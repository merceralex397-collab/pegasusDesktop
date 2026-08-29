using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Pegasus.Api.ContractTests.CommandCoverage;
using Pegasus.Core.Vehicle;

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
            // Use deterministic staff claims and in-memory vehicle command stores.
            // This keeps the contract suite independent of SQL Server while still
            // exercising the real authentication, endpoint-filter and Core seams.
            services.RemoveAll<IAuthenticationService>();
            services.AddSingleton<IAuthenticationService, ContractAuthenticationService>();
            services.RemoveAll<IRequestVehicleLookupStore>();
            services.RemoveAll<IAcceptVehicleSuggestionStore>();
            services.AddSingleton<VehicleCommandCoverageStore>();
            services.AddSingleton<IRequestVehicleLookupStore>(
                provider => provider.GetRequiredService<VehicleCommandCoverageStore>());
            services.AddSingleton<IAcceptVehicleSuggestionStore>(
                provider => provider.GetRequiredService<VehicleCommandCoverageStore>());
        });
    }

    internal sealed class ContractAuthenticationService : IAuthenticationService
    {
        public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string? scheme)
        {
            if (context.Request.Headers.ContainsKey("X-Contract-Unauthenticated"))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var role = context.Request.Headers.ContainsKey("X-Contract-Wrong-Right")
                ? "Unknown"
                : Pegasus.Core.Identity.StaffRoleNames.User;
            var claims = new[]
            {
                new System.Security.Claims.Claim(
                    System.Security.Claims.ClaimTypes.NameIdentifier,
                    "4de7c7c0-6119-4b3e-a0ba-b5e8e042c4b0"),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, role)
            };
            var identity = new System.Security.Claims.ClaimsIdentity(claims, "ContractTest");
            return Task.FromResult(AuthenticateResult.Success(
                new AuthenticationTicket(
                    new System.Security.Claims.ClaimsPrincipal(identity),
                    scheme ?? "ContractTest")));
        }

        public Task ChallengeAsync(
            HttpContext context,
            string? scheme,
            AuthenticationProperties? properties)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.Headers["WWW-Authenticate"] = "Bearer";
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
