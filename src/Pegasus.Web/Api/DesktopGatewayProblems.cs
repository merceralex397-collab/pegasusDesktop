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

        var correlationId = httpContext.Items.TryGetValue(
                DesktopGatewayRequestContext.CorrelationIdKey,
                out var value)
            && value is string itemCorrelationId
            ? itemCorrelationId
            : httpContext.TraceIdentifier;

        var problem = CreateProblem(exception, correlationId);
        httpContext.Response.StatusCode = problem.Status;
        httpContext.Response.ContentType = "application/problem+json";
        httpContext.Response.Headers[PegasusHeaders.CorrelationId] = correlationId;
        await JsonSerializer.SerializeAsync(
            httpContext.Response.Body,
            problem,
            PegasusJson.Options,
            cancellationToken);
        return true;
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
