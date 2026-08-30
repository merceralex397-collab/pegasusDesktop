using System.Runtime.InteropServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pegasus.Desktop.Infrastructure.Api;
using Pegasus.Desktop.Infrastructure.Authentication;
using Pegasus.Desktop.Infrastructure.Caching;
using Pegasus.Desktop.Infrastructure.Diagnostics;
using Pegasus.Desktop.Logging;
using Pegasus.Desktop.Options;
using Windows.Storage;

namespace Pegasus.Desktop.Hosting;

public static class PegasusHost
{
    internal const string BaseConfigurationResourceName =
        "Pegasus.Desktop.Configuration.appsettings.json";

    internal const string ChannelConfigurationResourceName =
        "Pegasus.Desktop.Configuration.appsettings.channel.json";

    private const long DiagnosticsMaximumBytes = 10 * 1024 * 1024;
    private const int DiagnosticsRetentionCount = 5;
    private const int SnapshotCacheMaximumEntries = 256;

    public static IHost Build(string[]? args = null)
    {
        return CreateBuilder(args).Build();
    }

    public static HostApplicationBuilder CreateBuilder(string[]? args = null)
    {
        var builder = Host.CreateApplicationBuilder(args ?? []);
        builder.Configuration.Sources.Clear();
        builder.Configuration
            .AddJsonStream(ReadEmbeddedConfiguration(BaseConfigurationResourceName))
            .AddJsonStream(ReadEmbeddedConfiguration(ChannelConfigurationResourceName));

        var configuration = builder.Configuration;
        builder.Services
            .AddOptions<GatewayOptions>()
            .Bind(configuration.GetSection("Gateway"))
            .ValidateDataAnnotations()
            .Validate(
                options => options.BaseAddress is { IsAbsoluteUri: true } &&
                    (options.BaseAddress.Scheme == Uri.UriSchemeHttp ||
                        options.BaseAddress.Scheme == Uri.UriSchemeHttps),
                "Gateway:BaseAddress must be an absolute HTTP or HTTPS URI.")
            .ValidateOnStart();

        builder.Services
            .AddOptions<UpdateOptions>()
            .Bind(configuration.GetSection(UpdateOptions.ConfigurationSectionName))
            .ValidateDataAnnotations()
            .Validate(options => options.FeedUri is { IsAbsoluteUri: true },
                "Update:FeedUri must be an absolute URI.")
            .ValidateOnStart();

        builder.Services
            .AddOptions<ChannelOptions>()
            .Configure(options => options.Channel = configuration[ChannelOptions.ConfigurationKey])
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var gatewayOptions = new GatewayOptions();
        configuration.GetSection("Gateway").Bind(gatewayOptions);
        builder.Services.AddPegasusApiClient(options => options.BaseAddress = gatewayOptions.BaseAddress);

        var localDataPath = GetLocalDataPath();
        var diagnosticsWriter = new RollingFileDiagnosticsWriter(
            localDataPath,
            DiagnosticsMaximumBytes,
            DiagnosticsRetentionCount);
        builder.Services.AddSingleton<IDiagnosticsWriter>(diagnosticsWriter);
        builder.Services.AddSingleton<IDesktopCredentialStore>(
            new DpapiCredentialStore(localDataPath));
        builder.Services.AddSingleton(new BoundedSnapshotCache(SnapshotCacheMaximumEntries));

        builder.Logging.ClearProviders();
        builder.Logging.AddProvider(
            new DiagnosticsLoggerProvider(
                diagnosticsWriter,
                Guid.NewGuid().ToString("N")));

        return builder;
    }

    private static MemoryStream ReadEmbeddedConfiguration(string resourceName)
    {
        using var resource = typeof(PegasusHost).Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded configuration resource '{resourceName}' was not found.");
        var content = new MemoryStream();
        resource.CopyTo(content);
        content.Position = 0;
        return content;
    }

    private static string GetLocalDataPath()
    {
        try
        {
            return ApplicationData.Current.LocalFolder.Path;
        }
        catch (Exception exception) when (exception is COMException or InvalidOperationException)
        {
            return Path.Combine(
                Path.GetTempPath(),
                "Pegasus.Desktop",
                Environment.ProcessId.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }
    }
}
