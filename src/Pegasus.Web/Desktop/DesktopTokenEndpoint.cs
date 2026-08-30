using System.Security.Claims;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using Pegasus.Core.Identity;
using Pegasus.Infrastructure.Persistence;
using Pegasus.Web.Mcp;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Pegasus.Web.Desktop;

/// <summary>
/// Dispatches the shared token endpoint to the existing Automation handler or
/// the first-party Desktop staff-session handler.
/// </summary>
internal static class DesktopTokenEndpoint
{
    public static async Task<IResult> ExchangeAsync(
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var request = httpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException(
                "The OpenIddict server request is unavailable.");
        var services = httpContext.RequestServices;
        if (string.Equals(
                request.ClientId,
                DesktopSession.ClientId,
                StringComparison.Ordinal))
        {
            return await ExchangeDesktopAsync(
                httpContext,
                request,
                services.GetRequiredService<DesktopClientRegistry>(),
                services.GetRequiredService<UserManager<PegasusIdentityUser>>(),
                services.GetRequiredService<SignInManager<PegasusIdentityUser>>(),
                services.GetRequiredService<ISecurityEventWriter>(),
                services.GetRequiredService<TimeProvider>(),
                cancellationToken);
        }

        var automationRegistry = services.GetService<AutomationClientRegistry>();
        return automationRegistry is null
            ? Forbid(Errors.UnauthorizedClient, "The token client is unavailable.")
            : await AutomationTokenEndpoint.ExchangeAsync(
                httpContext,
                automationRegistry,
                services.GetRequiredService<ISecurityEventWriter>(),
                services.GetRequiredService<TimeProvider>(),
                cancellationToken);
    }

    private static async Task<IResult> ExchangeDesktopAsync(
        HttpContext httpContext,
        OpenIddictRequest request,
        DesktopClientRegistry registry,
        UserManager<PegasusIdentityUser> userManager,
        SignInManager<PegasusIdentityUser> signInManager,
        ISecurityEventWriter securityEvents,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        if (!await registry.IsEnabledAsync(cancellationToken))
        {
            await RecordDeniedAsync(
                httpContext,
                securityEvents,
                timeProvider,
                subjectId: "anonymous",
                reasonCode: "desktop_client_disabled",
                cancellationToken);
            return Forbid(Errors.UnauthorizedClient, "The Desktop client is disabled.");
        }

        if (request.IsPasswordGrantType())
        {
            return await ExchangePasswordAsync(
                httpContext,
                request,
                userManager,
                signInManager,
                securityEvents,
                timeProvider,
                cancellationToken);
        }

        if (request.IsRefreshTokenGrantType())
        {
            return await ExchangeRefreshAsync(
                httpContext,
                request,
                userManager,
                securityEvents,
                timeProvider,
                cancellationToken);
        }

        return Forbid(
            Errors.UnsupportedGrantType,
            "Only the password and refresh-token grants are supported.");
    }

    private static async Task<IResult> ExchangePasswordAsync(
        HttpContext httpContext,
        OpenIddictRequest request,
        UserManager<PegasusIdentityUser> userManager,
        SignInManager<PegasusIdentityUser> signInManager,
        ISecurityEventWriter securityEvents,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var user = string.IsNullOrWhiteSpace(request.Username)
            ? null
            : await userManager.FindByNameAsync(request.Username.Trim());
        if (user is null || !user.IsEnabled)
        {
            await RecordDeniedAsync(
                httpContext,
                securityEvents,
                timeProvider,
                user?.Id.ToString("D") ?? "unknown",
                "account_disabled",
                cancellationToken);
            return Forbid(Errors.InvalidGrant, "account-disabled");
        }

        var passwordResult = await signInManager.CheckPasswordSignInAsync(
            user,
            request.Password ?? string.Empty,
            lockoutOnFailure: false);
        if (!passwordResult.Succeeded)
        {
            await RecordDeniedAsync(
                httpContext,
                securityEvents,
                timeProvider,
                user.Id.ToString("D"),
                "invalid_credentials",
                cancellationToken);
            return Forbid(Errors.InvalidGrant, "invalid-credentials");
        }

        var issuedAt = timeProvider.GetUtcNow().ToUnixTimeSeconds();
        var roles = await userManager.GetRolesAsync(user);
        var principal = DesktopTokenPrincipal.Create(
            user,
            roles,
            request.GetScopes(),
            issuedAt.ToString(System.Globalization.CultureInfo.InvariantCulture));
        await securityEvents.AppendAsync(
            new SecurityEvent(
                Guid.NewGuid(),
                SecurityEventType.SignIn,
                SecurityEventOutcome.Succeeded,
                user.Id.ToString("D"),
                timeProvider.GetUtcNow(),
                httpContext.TraceIdentifier,
                null),
            cancellationToken);
        return Results.SignIn(
            principal,
            properties: null,
            OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private static async Task<IResult> ExchangeRefreshAsync(
        HttpContext httpContext,
        OpenIddictRequest request,
        UserManager<PegasusIdentityUser> userManager,
        ISecurityEventWriter securityEvents,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var authenticated = await httpContext.AuthenticateAsync(
            OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        var tokenPrincipal = authenticated.Principal;
        var subjectValue = tokenPrincipal?.GetClaim(Claims.Subject);
        if (tokenPrincipal is null || !Guid.TryParse(subjectValue, out var subjectId))
        {
            await RecordDeniedAsync(
                httpContext,
                securityEvents,
                timeProvider,
                subjectValue ?? "unknown",
                "invalid_refresh_principal",
                cancellationToken);
            return Forbid(Errors.InvalidGrant, "The refresh token is no longer valid.");
        }

        var originalIssue = tokenPrincipal.FindFirst(DesktopSession.OriginalIssueClaim)?.Value;
        var nowSeconds = timeProvider.GetUtcNow().ToUnixTimeSeconds();
        if (!long.TryParse(
                originalIssue,
                System.Globalization.NumberStyles.None,
                System.Globalization.CultureInfo.InvariantCulture,
                out var issuedSeconds)
            || issuedSeconds < 0
            || issuedSeconds > nowSeconds
            || nowSeconds - issuedSeconds >= (long)DesktopSession.AbsoluteSessionLifetime.TotalSeconds)
        {
            await RecordDeniedAsync(
                httpContext,
                securityEvents,
                timeProvider,
                subjectId.ToString("D"),
                "absolute_session_expired",
                cancellationToken);
            return Forbid(Errors.InvalidGrant, "absolute-session-expired");
        }

        var user = await userManager.FindByIdAsync(subjectId.ToString("D"));
        var stamp = tokenPrincipal.FindFirst(DesktopSession.SecurityStampClaim)?.Value;
        if (user is null || !user.IsEnabled)
        {
            await RecordDeniedAsync(
                httpContext,
                securityEvents,
                timeProvider,
                subjectId.ToString("D"),
                "account_disabled",
                cancellationToken);
            return Forbid(Errors.InvalidGrant, "account-disabled");
        }

        if (!string.Equals(stamp, user.SecurityStamp, StringComparison.Ordinal))
        {
            await RecordDeniedAsync(
                httpContext,
                securityEvents,
                timeProvider,
                subjectId.ToString("D"),
                "invalid_security_stamp",
                cancellationToken);
            return Forbid(Errors.InvalidGrant, "The refresh token is no longer valid.");
        }

        var roles = await userManager.GetRolesAsync(user);
        var principal = DesktopTokenPrincipal.Create(
            user,
            roles,
            tokenPrincipal.GetScopes(),
            originalIssue!);
        return Results.SignIn(
            principal,
            properties: null,
            OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private static Task RecordDeniedAsync(
        HttpContext httpContext,
        ISecurityEventWriter securityEvents,
        TimeProvider timeProvider,
        string subjectId,
        string reasonCode,
        CancellationToken cancellationToken) =>
        securityEvents.AppendAsync(
            new SecurityEvent(
                Guid.NewGuid(),
                SecurityEventType.Token,
                SecurityEventOutcome.Denied,
                subjectId,
                timeProvider.GetUtcNow(),
                httpContext.TraceIdentifier,
                reasonCode),
            cancellationToken);

    private static IResult Forbid(string error, string description) =>
        Results.Forbid(
            new AuthenticationProperties(new Dictionary<string, string?>
            {
                [OpenIddictServerAspNetCoreConstants.Properties.Error] = error,
                [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = description
            }),
            [OpenIddictServerAspNetCoreDefaults.AuthenticationScheme]);
}

internal static class DesktopTokenPrincipal
{
    public static ClaimsPrincipal Create(
        PegasusIdentityUser user,
        IEnumerable<string> roles,
        IEnumerable<string> scopes,
        string originalIssue)
    {
        var identity = new ClaimsIdentity(
            TokenValidationParameters.DefaultAuthenticationType,
            Claims.Name,
            Claims.Role);
        identity.SetClaim(Claims.Subject, user.Id.ToString("D"));
        identity.SetClaim(DesktopSession.SecurityStampClaim, user.SecurityStamp ?? string.Empty);
        identity.SetClaim(DesktopSession.OriginalIssueClaim, originalIssue);
        foreach (var role in roles)
        {
            identity.AddClaim(new Claim(Claims.Role, role));
        }

        identity.SetScopes(scopes);
        // OpenIddict has no refresh-token destination. Claims needed to
        // rehydrate a refresh request remain in the protected refresh-token
        // principal; destinations govern the access/identity token claims.
        identity.SetDestinations(claim => claim.Type is Claims.Subject
            or Claims.Role
            or DesktopSession.OriginalIssueClaim
            or DesktopSession.SecurityStampClaim
            ? [Destinations.AccessToken]
            : []);
        var principal = new ClaimsPrincipal(identity);
        principal.SetAccessTokenLifetime(DesktopSession.AccessTokenLifetime);
        principal.SetRefreshTokenLifetime(DesktopSession.RefreshTokenLifetime);
        return principal;
    }
}
