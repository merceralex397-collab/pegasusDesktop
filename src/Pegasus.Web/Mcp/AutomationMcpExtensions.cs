using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol.AspNetCore.Authentication;
using OpenIddict.Abstractions;
using OpenIddict.Validation.AspNetCore;

namespace Pegasus.Web.Mcp;

/// <summary>
/// Composition for the configuration-gated Automation Actor MCP ingress.
/// Nothing here is registered unless <c>Features:AutomationMcp</c> enabled it
/// at startup; the application otherwise keeps failing closed by exposing no
/// such ingress.
/// </summary>
public static class AutomationMcpExtensions
{
    public static IServiceCollection AddPegasusAutomationMcp(
        this IServiceCollection services,
        AutomationMcpOptions options,
        string productVersion)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(productVersion);

        services.AddSingleton(options);
        services.AddMemoryCache();
        services.AddHttpContextAccessor();
        services.AddScoped<AutomationClientRegistry>();
        services.AddScoped<AutomationActorResolver>();
        services.AddScoped<AutomationMcpAuditor>();

        services.AddAuthentication()
            .AddMcp(
                AutomationMcp.AuthenticationScheme,
                displayName: "Pegasus Automation MCP",
                mcpOptions =>
                {
                    mcpOptions.ForwardAuthenticate =
                        OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
                    mcpOptions.ResourceMetadataUri = new Uri(
                        AutomationMcp.ResourceMetadataPath,
                        UriKind.Relative);
                    mcpOptions.ResourceMetadata = new()
                    {
                        Resource = options.ResourceUri.AbsoluteUri,
                        AuthorizationServers = { options.PublicOrigin.AbsoluteUri },
                        ScopesSupported = [.. AutomationMcp.Scopes],
                        ResourceName = "Pegasus Automation MCP"
                    };
                });
        services.AddAuthorizationBuilder()
            .AddPolicy(AutomationMcp.EndpointPolicy, policy =>
            {
                policy.AddAuthenticationSchemes(AutomationMcp.AuthenticationScheme);
                policy.RequireAuthenticatedUser();
                policy.RequireAssertion(context =>
                    context.User.GetAudiences().Contains(
                        AutomationMcp.Audience,
                        StringComparer.Ordinal)
                    && AutomationMcp.Scopes.Any(context.User.HasScope));
            });

        services.AddMcpServer(server => server.ServerInfo = new()
            {
                Name = "pegasus-automation",
                Version = productVersion
            })
            .WithHttpTransport(transport => transport.Stateless = true)
            .WithTools<CaseMcpTools>()
            .WithTools<IntakeMcpTools>()
            .WithTools<DocumentMcpTools>()
            .WithTools<AssessmentMcpTools>()
            .WithTools<MailMcpTools>()
            .WithTools<UnidentifiedMcpTools>()
            .WithTools<TriageMcpTools>();
        return services;
    }

    /// <summary>
    /// Maps the bearer-only automation surface: the streamable-HTTP MCP
    /// endpoint. The shared token endpoint (client credentials, authorization
    /// code, refresh, and Desktop staff sessions) is mapped by the shared
    /// OpenIddict composition. The Administrator consent page at <c>/authorize</c> is a
    /// Razor Page (staff cookie), not mapped here. A staff browser cookie is
    /// never accepted on <c>/mcp</c>: the endpoint policy authenticates
    /// exclusively with the automation bearer scheme, and an unauthenticated
    /// call receives 401 with WWW-Authenticate resource-metadata discovery.
    /// </summary>
    public static void MapPegasusAutomationMcp(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);
        app.MapMcp(AutomationMcp.McpEndpointPath)
            .RequireAuthorization(AutomationMcp.EndpointPolicy)
            .RequireRateLimiting(AutomationMcp.RateLimitPolicy);
    }
}
