using System.Security.Claims;
using OpenIddict.Abstractions;
using Pegasus.Core.Actors;
using Pegasus.Core.Identity;
using Pegasus.Web.Mcp;

namespace Pegasus.Web.Api;

/// <summary>
/// Resolves the already-authenticated desktop principal through the single Core
/// claims-to-actor factory. Account and session validity remain owned by
/// <see cref="Desktop.DesktopActorResolver"/>.
/// </summary>
internal sealed class StaffActorAccessor(
    IHttpContextAccessor httpContextAccessor,
    ISecurityEventWriter securityEvents,
    TimeProvider timeProvider)
{
    public async Task<ActionActor?> ResolveAsync(
        CancellationToken cancellationToken = default)
    {
        var httpContext = httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException(
                "The desktop request context is unavailable.");
        var principal = httpContext.User;
        var subject = principal.GetClaim(OpenIddictConstants.Claims.Subject)
            ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (principal.Identity?.IsAuthenticated != true
            || string.IsNullOrWhiteSpace(subject))
        {
            await DenyAsync(
                httpContext,
                "anonymous",
                "desktop_token_rejected",
                cancellationToken);
            return null;
        }

        if (principal.GetAudiences().Contains(
                AutomationMcp.Audience,
                StringComparer.Ordinal))
        {
            await DenyAsync(
                httpContext,
                subject,
                "desktop_token_rejected",
                cancellationToken);
            return null;
        }

        var roles = principal
            .FindAll(OpenIddictConstants.Claims.Role)
            .Concat(principal.FindAll(ClaimTypes.Role))
            .Select(claim => claim.Value)
            .Distinct(StringComparer.Ordinal);
        if (!StaffActorFactory.TryCreate(subject, roles, out var actor)
            || actor is null)
        {
            await DenyAsync(
                httpContext,
                subject,
                "desktop_token_rejected",
                cancellationToken);
            return null;
        }

        if (actor.Kind != ActorKind.Staff)
        {
            await DenyAsync(
                httpContext,
                subject,
                "desktop_actor_not_staff",
                cancellationToken);
            return null;
        }

        return actor;
    }

    public static ActionActor GetActor(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        return httpContext.Items.TryGetValue(
                DesktopGateway.ActorItemKey,
                out var value)
            && value is ActionActor actor
            ? actor
            : throw new StaffAuthorizationException(
                StaffAccessRight.AccessStaffApplication);
    }

    private Task DenyAsync(
        HttpContext httpContext,
        string subject,
        string reasonCode,
        CancellationToken cancellationToken) =>
        securityEvents.AppendAsync(
            new SecurityEvent(
                Guid.NewGuid(),
                SecurityEventType.Token,
                SecurityEventOutcome.Denied,
                subject,
                timeProvider.GetUtcNow(),
                DesktopGatewayCorrelation.Apply(httpContext),
                reasonCode),
            cancellationToken);
}
