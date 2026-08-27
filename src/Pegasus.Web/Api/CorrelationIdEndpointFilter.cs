using Microsoft.AspNetCore.Http;
using Pegasus.Contracts;

namespace Pegasus.Web.Api;

/// <summary>
/// Accepts or generates one correlation identifier for each desktop API call,
/// echoes it to the caller, and exposes it to problem translation.
/// </summary>
internal sealed partial class CorrelationIdEndpointFilter : IEndpointFilter
{
    private readonly ILogger<CorrelationIdEndpointFilter> logger;

    public CorrelationIdEndpointFilter(ILogger<CorrelationIdEndpointFilter> logger)
    {
        this.logger = logger;
    }

    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var httpContext = context.HttpContext;
        var correlationId = DesktopGatewayCorrelation.Apply(httpContext);
        DesktopGatewayCorrelation.Echo(httpContext, correlationId);
        DesktopGatewayLogging.LogRequest(
            logger,
            correlationId,
            httpContext.Request.Method,
            httpContext.Request.Path);

        return await next(context);
    }

}

internal sealed class DesktopGatewayCorrelationMiddleware(
    RequestDelegate next,
    ILogger<DesktopGatewayCorrelationMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext httpContext)
    {
        var correlationId = DesktopGatewayCorrelation.Apply(httpContext);
        DesktopGatewayCorrelation.Echo(httpContext, correlationId);
        DesktopGatewayLogging.LogRequest(
            logger,
            correlationId,
            httpContext.Request.Method,
            httpContext.Request.Path);

        await next(httpContext);
    }
}

internal static class DesktopGatewayCorrelation
{
    public static string Apply(HttpContext context)
    {
        if (context.Items.TryGetValue(
                DesktopGatewayRequestContext.CorrelationIdKey,
                out var value)
            && value is string existing)
        {
            return existing;
        }

        var supplied = context.Request.Headers[PegasusHeaders.CorrelationId].FirstOrDefault();
        var correlationId = IsWellFormed(supplied)
            ? supplied!
            : context.TraceIdentifier;
        context.Items[DesktopGatewayRequestContext.CorrelationIdKey] = correlationId;
        return correlationId;
    }

    public static void Echo(HttpContext context, string correlationId) =>
        context.Response.Headers[PegasusHeaders.CorrelationId] = correlationId;

    private static bool IsWellFormed(string? value) =>
        !string.IsNullOrEmpty(value)
        && value.Length <= 200
        && value.All(character => !char.IsControl(character));
}

internal static partial class DesktopGatewayLogging
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Debug,
        Message = "Desktop gateway request {CorrelationId} {Method} {Path}")]
    public static partial void LogRequest(
        ILogger logger,
        string correlationId,
        string method,
        PathString path);
}

/// <summary>
/// Named extension point reserved for the client-version compatibility check.
/// It intentionally preserves the current request until GWY-023 supplies the
/// actual check.
/// </summary>
internal sealed class ClientVersionEndpointFilter : IEndpointFilter
{
    public ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next) =>
        next(context);
}

internal static class DesktopGatewayRequestContext
{
    internal static readonly object CorrelationIdKey = new();
}
