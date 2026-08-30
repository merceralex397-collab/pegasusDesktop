using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using OpenIddict.Validation.AspNetCore;
using Pegasus.Infrastructure.Persistence;
using Pegasus.Web.Mcp;

namespace Pegasus.Web.Desktop;

/// <summary>
/// Composes the shared OpenIddict server once for the enabled first-party
/// clients. Automation and Desktop contribute only their own grants and
/// registrations; neither feature depends on the other feature's gate.
/// </summary>
public static class DesktopSessionExtensions
{
    public static IServiceCollection AddPegasusOpenIddict(
        this IServiceCollection services,
        AutomationMcpOptions? automationOptions,
        bool desktopGatewayEnabled)
    {
        ArgumentNullException.ThrowIfNull(services);
        if (automationOptions is null && !desktopGatewayEnabled)
        {
            throw new ArgumentException(
                "At least one first-party token client must be enabled.",
                nameof(desktopGatewayEnabled));
        }

        services.AddMemoryCache();
        services.AddScoped<DesktopClientRegistry>();
        services.AddOpenIddict()
            .AddCore(core => core
                .UseEntityFrameworkCore()
                .UseDbContext<PegasusDbContext>())
            .AddServer(server =>
            {
                server.SetTokenEndpointUris(DesktopSession.TokenEndpointPath);
                if (automationOptions is not null)
                {
                    server.SetAuthorizationEndpointUris(AutomationMcp.AuthorizationEndpointPath);
                    server.AllowClientCredentialsFlow();
                    server.AllowAuthorizationCodeFlow().RequireProofKeyForCodeExchange();
                }

                if (desktopGatewayEnabled)
                {
                    server.AllowPasswordFlow();
                }

                if (automationOptions is not null || desktopGatewayEnabled)
                {
                    server.AllowRefreshTokenFlow();
                }

                IReadOnlyList<string> scopes = automationOptions is null
                    ? [DesktopSession.Scope]
                    : [.. AutomationMcp.Scopes, DesktopSession.Scope];
                server.RegisterScopes([.. scopes]);
                if (automationOptions is not null)
                {
                    server.RegisterResources(automationOptions.ResourceUri.AbsoluteUri);
                    server.SetAccessTokenLifetime(AutomationMcp.AccessTokenLifetime);
                    server.SetRefreshTokenLifetime(AutomationMcp.RefreshTokenLifetime);
                    server.DisableSlidingRefreshTokenExpiration();
                }

                server.UseAspNetCore()
                    .EnableTokenEndpointPassthrough()
                    .EnableAuthorizationEndpointPassthrough()
                    .DisableTransportSecurityRequirement();
                server.UseDataProtection();
            })
            .AddValidation(validation =>
            {
                validation.UseLocalServer();
                validation.UseDataProtection();
                validation.UseAspNetCore();
                if (automationOptions is not null)
                {
                    validation.AddAudiences(AutomationMcp.Audience);
                }
            });

        return services;
    }

    /// <summary>
    /// Maps the single shared token endpoint. The dispatcher retains the
    /// existing Automation handler and sends only Desktop grant requests to
    /// the staff-session handler.
    /// </summary>
    public static void MapPegasusOpenIddictTokenEndpoint(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);
        app.MapPost(DesktopSession.TokenEndpointPath, DesktopTokenEndpoint.ExchangeAsync)
            .AllowAnonymous()
            .RequireRateLimiting(AutomationMcp.RateLimitPolicy);
    }
}
