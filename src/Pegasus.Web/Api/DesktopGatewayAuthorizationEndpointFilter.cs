using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Pegasus.Core.Actors;
using Pegasus.Core.Identity;

namespace Pegasus.Web.Api;

internal sealed class DesktopGatewayAuthorizationEndpointFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var httpContext = context.HttpContext;
        if (httpContext.User.Identity?.IsAuthenticated != true)
        {
            httpContext.Response.Headers.WWWAuthenticate = "Bearer";
            await DesktopGatewayProblems.WriteUnauthorizedAsync(
                httpContext,
                httpContext.RequestAborted);
            return TypedResults.Empty;
        }

        if (!StaffActorFactory.TryCreate(
                httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),
                httpContext.User.FindAll(ClaimTypes.Role).Select(claim => claim.Value),
                out var actor))
        {
            await DesktopGatewayProblems.WriteUnauthorizedAsync(
                httpContext,
                httpContext.RequestAborted);
            return TypedResults.Empty;
        }

        if (!StaffAuthorization.IsAuthorized(actor, StaffAccessRight.PerformCasework))
        {
            await DesktopGatewayProblems.WriteForbiddenAsync(
                httpContext,
                httpContext.RequestAborted);
            return TypedResults.Empty;
        }

        httpContext.Items[DesktopGatewayRequestContext.ActorKey] = actor;
        return await next(context);
    }
}

internal static class DesktopGatewayActors
{
    public static ActionActor Get(HttpContext context) =>
        context.Items[DesktopGatewayRequestContext.ActorKey] as ActionActor
        ?? throw new InvalidOperationException("The desktop gateway actor was not established.");
}
