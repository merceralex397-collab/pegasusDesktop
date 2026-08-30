using System.Globalization;
using Microsoft.AspNetCore.Identity;
using OpenIddict.Abstractions;
using Pegasus.Contracts.ProblemDetails;
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
    TimeProvider timeProvider) : IEndpointFilter
{
    internal static readonly object ActorKey = new();

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
            return DesktopGatewayProblems.AccountDisabled(
                httpContext,
                "absolute_session_expired");
        }

        var user = await userManager.FindByIdAsync(subjectId.ToString("D"));
        if (user is null || !user.IsEnabled)
        {
            return DesktopGatewayProblems.AccountDisabled(httpContext);
        }

        var securityStamp = principal.FindFirst(DesktopSession.SecurityStampClaim)?.Value;
        if (!string.Equals(securityStamp, user.SecurityStamp, StringComparison.Ordinal))
        {
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

        var roles = principal
            .FindAll(OpenIddictConstants.Claims.Role)
            .Select(claim => claim.Value);
        if (!StaffActorFactory.TryCreate(subject, roles, out var actor)
            || actor is null)
        {
            return DesktopGatewayProblems.NotAuthorized(httpContext);
        }

        httpContext.Items[ActorKey] = actor;
        return null;
    }

    public static ActionActor GetActor(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        return httpContext.Items.TryGetValue(
                ActorKey,
                out var value)
            && value is ActionActor actor
            ? actor
            : throw new StaffAuthorizationException(StaffAccessRight.AccessStaffApplication);
    }
}
