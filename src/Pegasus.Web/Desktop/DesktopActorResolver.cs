using System.Globalization;
using Microsoft.AspNetCore.Identity;
using OpenIddict.Abstractions;
using Pegasus.Core.Actors;
using Pegasus.Core.Identity;
using Pegasus.Infrastructure.Persistence;
using Pegasus.Web.Api;

namespace Pegasus.Web.Desktop;

/// <summary>
/// Resolves a validated desktop bearer principal to the single Core staff
/// actor and re-checks the account state for every API request.
/// </summary>
internal sealed class DesktopActorResolver(
    UserManager<PegasusIdentityUser> userManager,
    TimeProvider timeProvider,
    StaffActorAccessor staffActorAccessor,
    ISecurityEventWriter securityEvents) : IEndpointFilter
{
    private static readonly string[] PasswordChangeExemptPaths =
    [
        "/api/v1/session/password-change",
        "/api/v1/session/logout",
        "/api/v1/session/me"
    ];

    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var refusal = await ResolveAsync(context.HttpContext);
        if (refusal is not null)
        {
            return refusal;
        }

        return await next(context);
    }

    internal async Task<IResult?> ResolveAsync(
        HttpContext httpContext)
    {
        var principal = httpContext.User;
        var subject = principal.GetClaim(OpenIddictConstants.Claims.Subject);

        if (!Guid.TryParse(subject, out var subjectId) || subjectId == Guid.Empty)
        {
            return DesktopGatewayProblems.NotAuthorized(httpContext);
        }

        var originalIssue = principal.FindFirst(DesktopSession.OriginalIssueClaim)?.Value;
        var nowSeconds = timeProvider.GetUtcNow().ToUnixTimeSeconds();
        if (!long.TryParse(
                originalIssue,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var issuedSeconds)
            || issuedSeconds < 0
            || issuedSeconds > nowSeconds
            || nowSeconds - issuedSeconds
                >= (long)DesktopSession.AbsoluteSessionLifetime.TotalSeconds)
        {
            await RecordDeniedAsync(
                httpContext,
                subjectId.ToString("D"),
                "absolute_session_expired");
            return DesktopGatewayProblems.AccountDisabled(
                httpContext,
                "absolute_session_expired");
        }

        var user = await userManager.FindByIdAsync(subjectId.ToString("D"));
        if (user is null || !user.IsEnabled)
        {
            await RecordDeniedAsync(
                httpContext,
                subjectId.ToString("D"),
                "account_disabled");
            return DesktopGatewayProblems.AccountDisabled(httpContext);
        }

        var securityStamp = principal.FindFirst(DesktopSession.SecurityStampClaim)?.Value;
        if (!string.Equals(securityStamp, user.SecurityStamp, StringComparison.Ordinal))
        {
            await RecordDeniedAsync(
                httpContext,
                subjectId.ToString("D"),
                "invalid_security_stamp");
            return DesktopGatewayProblems.AccountDisabled(
                httpContext,
                "invalid_security_stamp");
        }

        if (user.MustChangePassword
            && !PasswordChangeExemptPaths.Any(path =>
                httpContext.Request.Path.StartsWithSegments(path)))
        {
            return DesktopGatewayProblems.PasswordChangeRequired(httpContext);
        }

        var actor = await staffActorAccessor.ResolveAsync(httpContext.RequestAborted);
        if (actor is null)
        {
            return DesktopGatewayProblems.NotAuthorized(httpContext);
        }

        httpContext.Items[DesktopGateway.ActorItemKey] = actor;
        return null;
    }

    private Task RecordDeniedAsync(
        HttpContext httpContext,
        string subjectId,
        string reasonCode) =>
        securityEvents.AppendAsync(
            new SecurityEvent(
                Guid.NewGuid(),
                SecurityEventType.Token,
                SecurityEventOutcome.Denied,
                subjectId,
                timeProvider.GetUtcNow(),
                DesktopGatewayCorrelation.Apply(httpContext),
                reasonCode),
            httpContext.RequestAborted);

    public static ActionActor GetActor(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        return StaffActorAccessor.GetActor(httpContext);
    }
}
