using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenIddict.Abstractions;
using OpenIddict.Validation.AspNetCore;
using Pegasus.Infrastructure.Persistence;
using Pegasus.Web.Mcp;
using System.Security.Cryptography.X509Certificates;

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
        bool desktopGatewayEnabled,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);
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
                AddTokenSigningCredentials(server, configuration, environment);
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

    private static void AddTokenSigningCredentials(
        OpenIddictServerBuilder server,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        if (environment.IsDevelopment())
        {
            // The local/Test stack has no external certificate authority. These
            // user-scoped development certificates are durable on the workstation
            // and are intentionally separate from the Data Protection token ring.
            // The production path below requires an operator-provided certificate.
            var subject = new X500DistinguishedName(DesktopSession.CertificateSubject);
            server.AddDevelopmentEncryptionCertificate(subject);
            server.AddDevelopmentSigningCertificate(subject);
            return;
        }

        var certificatePath = configuration["OpenIddict:CertificatePath"];
        var certificatePassword = configuration["OpenIddict:CertificatePassword"];
        if (string.IsNullOrWhiteSpace(certificatePath))
        {
            throw new InvalidOperationException(
                "OpenIddict:CertificatePath is required outside Development when the "
                + "Automation or Desktop token client is enabled.");
        }

        var certificate = X509CertificateLoader.LoadPkcs12FromFile(
            certificatePath,
            certificatePassword,
            X509KeyStorageFlags.EphemeralKeySet);
        if (!string.Equals(
                certificate.Subject,
                DesktopSession.CertificateSubject,
                StringComparison.Ordinal))
        {
            certificate.Dispose();
            throw new InvalidOperationException(
                $"OpenIddict certificate subject must be '{DesktopSession.CertificateSubject}'.");
        }

        server.AddEncryptionCertificate(certificate);
        server.AddSigningCertificate(certificate);
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
