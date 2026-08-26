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
        var correlationId = GetCorrelationId(httpContext);
        httpContext.Items[DesktopGatewayRequestContext.CorrelationIdKey] = correlationId;
        httpContext.Response.Headers[PegasusHeaders.CorrelationId] = correlationId;
        LogRequest(
            correlationId,
            httpContext.Request.Method,
            httpContext.Request.Path);

        return await next(context);
    }

    private static string GetCorrelationId(HttpContext context)
    {
        var supplied = context.Request.Headers[PegasusHeaders.CorrelationId].FirstOrDefault();
        return IsWellFormed(supplied)
            ? supplied!
            : context.TraceIdentifier;
    }

    private static bool IsWellFormed(string? value) =>
        !string.IsNullOrEmpty(value)
        && value.Length <= 200
        && value.All(character => !char.IsControl(character));

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Debug,
        Message = "Desktop gateway request {CorrelationId} {Method} {Path}")]
    private partial void LogRequest(string correlationId, string method, PathString path);
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
