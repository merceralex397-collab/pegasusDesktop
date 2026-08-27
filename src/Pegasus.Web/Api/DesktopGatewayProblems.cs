using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Pegasus.Contracts;
using Pegasus.Contracts.ProblemDetails;
using Pegasus.Core.Identity;
using Pegasus.Core.Workflow;

namespace Pegasus.Web.Api;

/// <summary>
/// Translates known Core refusals into safe RFC 9457 responses for the desktop
/// API. Unexpected exceptions collapse to a generic maintenance problem.
/// </summary>
internal sealed class DesktopGatewayExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException || httpContext.Response.HasStarted)
        {
            return false;
        }

        var correlationId = DesktopGatewayCorrelation.Apply(httpContext);
        var problem = CreateProblem(exception, correlationId);
        return await DesktopGatewayProblems.WriteAsync(
            httpContext,
            problem,
            cancellationToken);
    }

    private static PegasusProblem CreateProblem(Exception exception, string correlationId)
    {
        return exception switch
        {
            StaffAuthorizationException => new PegasusProblem(
                PegasusProblemTypes.NotAuthorized,
                "Not authorized",
                StatusCodes.Status403Forbidden,
                "The current staff actor is not authorized for this action.",
                null,
                correlationId),
            CaseVersionConflictException versionConflict => new PegasusProblem(
                PegasusProblemTypes.VersionConflict,
                "Version conflict",
                StatusCodes.Status409Conflict,
                "The case changed since it was read. Reload before retrying.",
                null,
                correlationId,
                new Dictionary<string, object?>
                {
                    ["currentVersion"] = versionConflict.ActualVersion.ToString(
                        CultureInfo.InvariantCulture)
                }),
            CaseEditLeaseConflictException leaseConflict => new PegasusProblem(
                PegasusProblemTypes.LeaseConflict,
                "Edit lease conflict",
                StatusCodes.Status409Conflict,
                "The case is currently being edited by another actor.",
                null,
                correlationId,
                new Dictionary<string, object?>
                {
                    ["currentVersion"] = leaseConflict.CaseVersion.ToString(
                        CultureInfo.InvariantCulture)
                }),
            CaseEditLeaseExpiredException leaseExpired => new PegasusProblem(
                PegasusProblemTypes.LeaseExpired,
                "Edit lease expired",
                StatusCodes.Status409Conflict,
                "The edit lease is no longer valid. Acquire edit authority again.",
                null,
                correlationId,
                new Dictionary<string, object?>
                {
                    ["currentVersion"] = leaseExpired.CaseVersion.ToString(
                        CultureInfo.InvariantCulture)
                }),
            CaseOperationConflictException => new PegasusProblem(
                PegasusProblemTypes.OperationConflict,
                "Operation conflict",
                StatusCodes.Status409Conflict,
                "The operation key was already used with different inputs.",
                null,
                correlationId),
            ArgumentException or InvalidOperationException or InvalidDataException =>
                new PegasusProblem(
                    PegasusProblemTypes.Validation,
                    "Validation failed",
                    StatusCodes.Status400BadRequest,
                    "The request was rejected by a validation rule.",
                    null,
                    correlationId),
            _ => new PegasusProblem(
                PegasusProblemTypes.Maintenance,
                "Maintenance",
                StatusCodes.Status500InternalServerError,
                "The server could not complete the request.",
                null,
                correlationId)
        };
    }
}

internal static class DesktopGatewayProblems
{
    public static Task WriteUnauthorizedAsync(
        HttpContext httpContext,
        CancellationToken cancellationToken = default) =>
        WriteAsync(
            httpContext,
            new PegasusProblem(
                PegasusProblemTypes.NotAuthorized,
                "Authentication required",
                StatusCodes.Status401Unauthorized,
                "A valid staff session is required.",
                null,
                DesktopGatewayCorrelation.Apply(httpContext)),
            cancellationToken);

    public static Task WriteForbiddenAsync(
        HttpContext httpContext,
        CancellationToken cancellationToken = default) =>
        WriteAsync(
            httpContext,
            new PegasusProblem(
                PegasusProblemTypes.NotAuthorized,
                "Not authorized",
                StatusCodes.Status403Forbidden,
                "The current staff actor is not authorized for this action.",
                null,
                DesktopGatewayCorrelation.Apply(httpContext)),
            cancellationToken);

    public static Task WriteValidationAsync(
        HttpContext httpContext,
        string detail,
        CancellationToken cancellationToken = default) =>
        WriteAsync(
            httpContext,
            new PegasusProblem(
                PegasusProblemTypes.Validation,
                "Validation failed",
                StatusCodes.Status400BadRequest,
                detail,
                null,
                DesktopGatewayCorrelation.Apply(httpContext)),
            cancellationToken);

    public static Task WriteRateLimitedAsync(
        HttpContext httpContext,
        string detail,
        CancellationToken cancellationToken = default) =>
        WriteAsync(
            httpContext,
            new PegasusProblem(
                PegasusProblemTypes.RateLimited,
                "Too many requests",
                StatusCodes.Status429TooManyRequests,
                detail,
                null,
                DesktopGatewayCorrelation.Apply(httpContext)),
            cancellationToken);

    public static async Task WriteNotFoundAsync(
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        if (httpContext.Response.StatusCode != StatusCodes.Status404NotFound ||
            httpContext.Response.HasStarted ||
            httpContext.Response.ContentLength is > 0)
        {
            return;
        }

        var correlationId = DesktopGatewayCorrelation.Apply(httpContext);
        await WriteAsync(
            httpContext,
            new PegasusProblem(
                PegasusProblemTypes.NotFound,
                "Not found",
                StatusCodes.Status404NotFound,
                "The requested resource was not found.",
                null,
                correlationId),
            cancellationToken);
    }

    public static async Task<bool> WriteAsync(
        HttpContext httpContext,
        PegasusProblem problem,
        CancellationToken cancellationToken)
    {
        if (httpContext.Response.HasStarted)
        {
            return false;
        }

        httpContext.Response.StatusCode = problem.Status;
        httpContext.Response.ContentType = "application/problem+json";
        DesktopGatewayCorrelation.Echo(httpContext, problem.CorrelationId);
        await JsonSerializer.SerializeAsync(
            httpContext.Response.Body,
            problem,
            PegasusJson.Options,
            cancellationToken);
        return true;
    }
}
