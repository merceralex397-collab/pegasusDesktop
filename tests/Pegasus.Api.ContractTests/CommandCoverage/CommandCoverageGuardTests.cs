using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Pegasus.Api.ContractTests.CommandCoverage;

namespace Pegasus.Api.ContractTests;

[Trait("Category", "Contract")]
public sealed class CommandCoverageGuardTests
{
    [Fact]
    public void CurrentHostHasNoUncoveredCommandEndpoints()
    {
        using var factory = new ContractTestWebApplicationFactory();
        var mismatches = CommandCoverageGuard.FindMismatches(
            CommandEndpointCatalogue.Read(factory.Services),
            CommandCoverageTable.Rows);

        Assert.Empty(mismatches);
    }

    [Fact]
    public void ACommandWithoutARowNamesItsRouteAndMethod()
    {
        using var factory = new ProbeWebApplicationFactory();
        using var client = factory.CreateClient();
        var mismatches = CommandCoverageGuard.FindMismatches(
            CommandEndpointCatalogue.Read(factory.Services),
            CommandCoverageTable.Rows);

        Assert.Contains(
            mismatches,
            mismatch => mismatch.Contains("POST /api/v1/__probe", StringComparison.Ordinal));
    }

    private sealed class ProbeWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("Runtime:Profile", "DevelopmentOffline");
            builder.UseSetting("Features:DesktopGateway", "true");
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IAuthenticationService>();
                services.AddSingleton<IAuthenticationService,
                    ContractTestWebApplicationFactory.NoOpAuthenticationService>();
                services.AddTransient<IStartupFilter, ProbeStartupFilter>();
            });
        }
    }

    private sealed class ProbeStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) =>
            application =>
            {
                next(application);
                application.UseEndpoints(endpoints =>
                    endpoints.MapPost("/api/v1/__probe", () => Results.NoContent()));
            };
    }
}
