using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using OpenIddict.Validation.AspNetCore;
using Pegasus.Web.Desktop;

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
        services.AddScoped<DesktopActorResolver>();
        services.AddProblemDetails();
        services.AddExceptionHandler<DesktopGatewayExceptionHandler>();
        services.AddSingleton<IAuthorizationMiddlewareResultHandler,
            DesktopGatewayAuthorizationMiddlewareResultHandler>();
        services.AddAuthorizationBuilder()
            .AddPolicy(DesktopGateway.AuthorizationPolicy, policy =>
            {
                policy.AddAuthenticationSchemes(
                    OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
                policy.RequireAuthenticatedUser();
                policy.RequireAssertion(context =>
                    context.User.HasScope(DesktopSession.Scope)
                    && Guid.TryParse(
                        context.User.GetClaim(OpenIddictConstants.Claims.Subject),
                        out var subjectId)
                    && subjectId != Guid.Empty);
            });
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
            .WithGroupName(OpenApiDocumentName)
            .RequireAuthorization(DesktopGateway.AuthorizationPolicy);
        group.AddEndpointFilter<CorrelationIdEndpointFilter>();
        group.AddEndpointFilter<ClientVersionEndpointFilter>();
        group.AddEndpointFilter<DesktopActorResolver>();
        group.MapVehicleEndpoints();
        group.MapMailEndpoints();
        app.MapOpenApi("/openapi/{documentName}.json")
            .AllowAnonymous();
        return group;
    }
}
