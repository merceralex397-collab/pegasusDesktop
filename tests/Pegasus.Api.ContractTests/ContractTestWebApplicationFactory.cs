using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Pegasus.Api.ContractTests;

public sealed class ContractTestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("Runtime:Profile", "DevelopmentOffline");
        builder.UseSetting("Features:DesktopGateway", "true");
        builder.ConfigureTestServices(services =>
        {
            // The contract endpoints are anonymous. Avoid the DevelopmentOffline
            // identity lookup so this unit-test host does not require SQL Server.
            services.RemoveAll<IAuthenticationService>();
            services.AddSingleton<IAuthenticationService, NoOpAuthenticationService>();
        });
    }

    private sealed class NoOpAuthenticationService : IAuthenticationService
    {
        public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string? scheme) =>
            Task.FromResult(AuthenticateResult.NoResult());

        public Task ChallengeAsync(
            HttpContext context,
            string? scheme,
            AuthenticationProperties? properties)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        }

        public Task ForbidAsync(
            HttpContext context,
            string? scheme,
            AuthenticationProperties? properties)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        }

        public Task SignInAsync(
            HttpContext context,
            string? scheme,
            System.Security.Claims.ClaimsPrincipal principal,
            AuthenticationProperties? properties) =>
            Task.CompletedTask;

        public Task SignOutAsync(
            HttpContext context,
            string? scheme,
            AuthenticationProperties? properties) =>
            Task.CompletedTask;
    }
}
