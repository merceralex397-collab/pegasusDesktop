using Microsoft.AspNetCore.Builder;
using Pegasus.Core.Identity;

namespace Pegasus.Web.Api;

/// <summary>
/// Applies one Core staff right as a fail-fast transport boundary. Business
/// state preconditions remain owned by the Core use cases after this boundary.
/// </summary>
/// <remarks>
/// The <see cref="StaffAccessRight"/> contract states that business-state
/// preconditions remain owned by their feature use cases and are evaluated
/// after this actor boundary succeeds. This filter adds no such precondition.
/// </remarks>
internal sealed class RequireStaffRightFilter(StaffAccessRight right) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var actor = StaffActorAccessor.GetActor(context.HttpContext);
        StaffAuthorization.Require(actor, right);
        return await next(context);
    }
}

internal static class StaffRightRouteGroupExtensions
{
    public static RouteGroupBuilder RequireStaffRight(
        this RouteGroupBuilder group,
        StaffAccessRight right)
    {
        ArgumentNullException.ThrowIfNull(group);
        group.AddEndpointFilter(new RequireStaffRightFilter(right));
        return group;
    }
}
