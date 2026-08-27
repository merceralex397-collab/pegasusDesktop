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
        services.AddSingleton<DesktopDocumentUploadSessions>();
        return services;
    }

    /// <summary>
    /// Maps the versioned desktop API group, including its shared authentication
    /// and endpoint-authorization filters.
    /// </summary>
    public static RouteGroupBuilder MapPegasusDesktopGateway(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup(DesktopGateway.BasePath);
        group.AllowAnonymous();
        group.AddEndpointFilter<CorrelationIdEndpointFilter>();
        group.AddEndpointFilter<ClientVersionEndpointFilter>();
        group.AddEndpointFilter<DesktopGatewayAuthorizationEndpointFilter>();
        group.MapBoxDocumentBroker();
        return group;
    }
}
