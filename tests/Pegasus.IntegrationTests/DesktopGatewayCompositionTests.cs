using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Pegasus.Web.Api;

namespace Pegasus.IntegrationTests;

[Trait("Category", "SqlServer")]
public sealed class DesktopGatewayCompositionTests
{
    public static TheoryData<string?> ClosedGateConfigurations => new()
    {
        null,
        "false"
    };

    [Theory]
    [MemberData(nameof(ClosedGateConfigurations))]
    public void GatewayIsNotComposedWhenTheFeatureFlagIsClosed(string? flagValue)
    {
        if (flagValue is null)
        {
            using var factory = new IntakeWebApplicationFactory();
            AssertClosed(factory);
            return;
        }

        using var baseFactory = new IntakeWebApplicationFactory();
        using var configuredFactory = baseFactory.WithWebHostBuilder(builder =>
            builder.UseSetting(DesktopGateway.FeatureFlag, flagValue));
        AssertClosed(configuredFactory);
    }

    private static void AssertClosed(WebApplicationFactory<Program> factory)
    {
        Assert.Null(factory.Services.GetService<DesktopGatewayOptions>());

        Assert.DoesNotContain(
            factory.Services.GetRequiredService<EndpointDataSource>().Endpoints,
            endpoint => endpoint is RouteEndpoint routeEndpoint
                         && routeEndpoint.RoutePattern.RawText?.StartsWith(
                             DesktopGateway.BasePath,
                             StringComparison.Ordinal) == true);
    }

    [Fact]
    public async Task GatewayComposesOnlyWhenTheFeatureFlagIsEnabled()
    {
        using var baseFactory = new IntakeWebApplicationFactory();
        using var factory = baseFactory.WithWebHostBuilder(builder =>
            builder.UseSetting(DesktopGateway.FeatureFlag, "true"));

        Assert.NotNull(factory.Services.GetRequiredService<DesktopGatewayOptions>());

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost:7139")
        });
        using var response = await client.GetAsync($"{DesktopGateway.BasePath}/unknown");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("/status/404", body, StringComparison.Ordinal);
    }
}
