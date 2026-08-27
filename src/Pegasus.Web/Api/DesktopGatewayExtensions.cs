using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Pegasus.Web.Api;

/// <summary>
/// Composes and maps the native desktop API surface.
/// </summary>
public static class DesktopGatewayExtensions
{
    public static IServiceCollection AddPegasusDesktopGateway(
        this IServiceCollection services,
        DesktopGatewayOptions options)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);

        services.AddSingleton(options);
        services.AddProblemDetails();
        services.AddExceptionHandler<DesktopGatewayExceptionHandler>();
        return services;
    }

    /// <summary>
    /// Maps the empty versioned desktop API group. Authentication and endpoint
    /// authorization are added by the endpoint tickets that attach routes to
    /// this group; this ticket only composes the shared filters and returns the
    /// group for those callers.
    /// </summary>
    public static RouteGroupBuilder MapPegasusDesktopGateway(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup(DesktopGateway.BasePath);
        group.AddEndpointFilter<CorrelationIdEndpointFilter>();
        group.AddEndpointFilter<ClientVersionEndpointFilter>();
        return group;
    }
}
