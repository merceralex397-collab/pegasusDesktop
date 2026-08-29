using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pegasus.Contracts;
using Pegasus.Desktop.Hosting;
using Pegasus.Desktop.Infrastructure.Api;
using Pegasus.Desktop.Infrastructure.Authentication;
using Pegasus.Desktop.Infrastructure.Caching;
using Pegasus.Desktop.Infrastructure.Diagnostics;
using Pegasus.Desktop.Logging;
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
        Assert.NotEmpty(services.GetServices<ILoggerProvider>());
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

    [Fact]
    [Trait("Category", "ViewModel")]
    public void DiagnosticsProviderWritesSessionAndCorrelationAndRedactsSensitiveProperties()
    {
        var root = Directory.CreateTempSubdirectory("pegasus-desktop-host-tests-").FullName;

        try
        {
            var writer = new RollingFileDiagnosticsWriter(root, 4096, 2);
            using var provider = new DiagnosticsLoggerProvider(writer, "session-123");
            var logger = provider.CreateLogger("Pegasus.Desktop.ViewModelTests");

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
                    static (_, _) => "operation complete");
            }

            var content = File.ReadAllText(Assert.Single(writer.GetFiles()));
            Assert.Contains("session-123", content);
            Assert.Contains("correlation-123", content);
            Assert.Contains("operation complete", content);
            Assert.Contains("[REDACTED]", content);
            Assert.DoesNotContain("fake-access-token", content);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }
}
