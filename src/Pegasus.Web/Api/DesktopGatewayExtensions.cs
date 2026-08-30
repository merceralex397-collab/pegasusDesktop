using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Pegasus.Web.Api;

/// <summary>
/// Composes and maps the native desktop API surface.
/// </summary>
public static class DesktopGatewayExtensions
{
    private const string OpenApiDocumentName = "v1";

    public static IServiceCollection AddPegasusDesktopGateway(
        this IServiceCollection services,
        DesktopGatewayOptions options)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);

        services.AddSingleton(options);
        services.AddProblemDetails();
        services.AddExceptionHandler<DesktopGatewayExceptionHandler>();
        services.AddOpenApi(OpenApiDocumentName, openApiOptions =>
        {
            openApiOptions.ShouldInclude = description => description.GroupName == OpenApiDocumentName;
            openApiOptions.AddDocumentTransformer<OpenApiDocumentTransformer>();
            openApiOptions.AddOperationTransformer<VehicleOpenApiOperationTransformer>();
        });
        return services;
    }

    /// <summary>
    /// Maps the versioned desktop API group and its currently composed endpoint
    /// slices. Each slice adds its own Core-backed routes to this group.
    /// </summary>
    public static RouteGroupBuilder MapPegasusDesktopGateway(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup(DesktopGateway.BasePath)
            .WithGroupName(OpenApiDocumentName);
        group.AddEndpointFilter<CorrelationIdEndpointFilter>();
        group.AddEndpointFilter<ClientVersionEndpointFilter>();
        group.MapVehicleEndpoints();
        app.MapOpenApi("/openapi/{documentName}.json")
            .AllowAnonymous();
        return group;
    }
}
