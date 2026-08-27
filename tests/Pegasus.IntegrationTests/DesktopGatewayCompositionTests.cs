using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Pegasus.Contracts;
using Pegasus.Contracts.ProblemDetails;
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
    public async Task GatewayIsNotComposedWhenTheFeatureFlagIsClosed(string? flagValue)
    {
        if (flagValue is null)
        {
            using var factory = new IntakeWebApplicationFactory();
            await AssertClosedAsync(factory);
            return;
        }

        using var baseFactory = new IntakeWebApplicationFactory();
        using var configuredFactory = baseFactory.WithWebHostBuilder(builder =>
            builder.UseSetting(DesktopGateway.FeatureFlag, flagValue));
        await AssertClosedAsync(configuredFactory);
    }

    private static async Task AssertClosedAsync(WebApplicationFactory<Program> factory)
    {
        Assert.Null(factory.Services.GetService<DesktopGatewayOptions>());

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost:7139")
        });
        using var response = await client.GetAsync($"{DesktopGateway.BasePath}/anything");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

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
        using var baseFactory = new IntakeWebApplicationFactory(
            useIntegrationTestAuthentication: true);
        using var factory = baseFactory.WithWebHostBuilder(builder =>
            builder.UseSetting(DesktopGateway.FeatureFlag, "true"));

        Assert.NotNull(factory.Services.GetRequiredService<DesktopGatewayOptions>());

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost:7139")
        });
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"{DesktopGateway.BasePath}/unknown");
        request.Headers.Add(PegasusHeaders.CorrelationId, "not-found-correlation");
        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await JsonSerializer.DeserializeAsync<PegasusProblem>(
            await response.Content.ReadAsStreamAsync(),
            PegasusJson.Options);
        Assert.NotNull(problem);
        Assert.Equal(PegasusProblemTypes.NotFound, problem!.Type);
        Assert.Equal("not-found-correlation", problem.CorrelationId);
    }
}
