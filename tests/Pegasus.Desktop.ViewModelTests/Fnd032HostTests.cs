using System.Globalization;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pegasus.Contracts;
using Pegasus.Desktop.Hosting;
using Pegasus.Desktop.Infrastructure.Api;
using Pegasus.Desktop.Infrastructure.Authentication;
using Pegasus.Desktop.Infrastructure.Caching;
using Pegasus.Desktop.Infrastructure.Diagnostics;
using Pegasus.Desktop.Options;

namespace Pegasus.Desktop.ViewModelTests;

public sealed class Fnd032HostTests
{
    [Fact]
    [Trait("Category", "ViewModel")]
    public async Task HostBuildResolvesConfiguredServicesInAnUnpackagedTestProcess()
    {
        using var host = PegasusHost.Build();
        await host.StartAsync();

        var services = host.Services;
        var gateway = services.GetRequiredService<IOptions<GatewayOptions>>().Value;
        var update = services.GetRequiredService<IOptions<UpdateOptions>>().Value;
        var channel = services.GetRequiredService<IOptions<ChannelOptions>>().Value;
        using var client = services.GetRequiredService<IHttpClientFactory>().CreateClient("pegasus");

        Assert.Equal("local", channel.Channel);
        Assert.NotNull(gateway.BaseAddress);
        Assert.NotNull(update.FeedUri);
        Assert.Equal(gateway.BaseAddress, client.BaseAddress);
        Assert.IsAssignableFrom<IDesktopCredentialStore>(
            services.GetRequiredService<IDesktopCredentialStore>());
        Assert.IsType<BoundedSnapshotCache>(services.GetRequiredService<BoundedSnapshotCache>());
        var writer = Assert.IsType<RollingFileDiagnosticsWriter>(
            services.GetRequiredService<IDiagnosticsWriter>());
        Assert.Equal(
            10 * 1024 * 1024,
            Assert.IsType<long>(typeof(RollingFileDiagnosticsWriter)
                .GetField("_maxTotalBytes", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.GetValue(writer)));
        Assert.Equal(
            5,
            Assert.IsType<int>(typeof(RollingFileDiagnosticsWriter)
                .GetField("_retentionCount", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.GetValue(writer)));

        const string marker = "fnd032-host-logger-marker";
        var loggerFactory = services.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("Pegasus.Desktop.ViewModelTests");
        using (logger.BeginScope(new Dictionary<string, object?>
        {
            [PegasusHeaders.CorrelationId] = "correlation-123"
        }))
        {
            var state = new[]
            {
                new KeyValuePair<string, object?>(
                    "AccessToken",
                    "Bearer fake-access-token")
            };
            logger.Log(
                LogLevel.Information,
                new EventId(1, "HostTest"),
                state,
                exception: null,
                static (_, _) => marker);
        }

        var logPath = Assert.Single(
            writer.GetFiles(),
            path => File.ReadAllText(path).Contains(marker));
        var logLine = Assert.Single(File.ReadLines(logPath), line => line.Contains(marker));
        using var logEntry = JsonDocument.Parse(logLine);
        var logRoot = logEntry.RootElement;
        Assert.False(string.IsNullOrWhiteSpace(logRoot.GetProperty("sessionId").GetString()));
        Assert.Equal("correlation-123", logRoot.GetProperty("correlationId").GetString());
        Assert.Equal(marker, logRoot.GetProperty("message").GetString());
        Assert.Contains("[REDACTED]", logLine);
        Assert.DoesNotContain("fake-access-token", logLine);

        var fallbackRoot = Path.GetFullPath(Path.Combine(
            Path.GetTempPath(),
            "Pegasus.Desktop",
            Environment.ProcessId.ToString(CultureInfo.InvariantCulture)));
        Assert.StartsWith(
            fallbackRoot + Path.DirectorySeparatorChar,
            Path.GetFullPath(logPath),
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Category", "ViewModel")]
    public async Task HostStartFailsWhenGatewayBaseAddressIsMissing()
    {
        var builder = PegasusHost.CreateBuilder();
        builder.Configuration["Gateway:BaseAddress"] = null;
        using var host = builder.Build();

        var exception = await Assert.ThrowsAnyAsync<Exception>(() => host.StartAsync());

        Assert.Contains("Gateway:BaseAddress", exception.ToString(), StringComparison.OrdinalIgnoreCase);
    }

}
