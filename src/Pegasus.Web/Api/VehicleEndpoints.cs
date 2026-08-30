using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Pegasus.Contracts;
using Pegasus.Contracts.ProblemDetails;
using Pegasus.Contracts.Vehicle;
using Pegasus.Core.Actors;
using Pegasus.Core.Identity;
using Pegasus.Core.Vehicle;

namespace Pegasus.Web.Api;

/// <summary>Maps the vehicle gateway routes to the existing Core vehicle ports.</summary>
public static class VehicleEndpoints
{
    public static RouteGroupBuilder MapVehicleEndpoints(this RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        var vehicle = group.MapGroup("/cases/{caseId:guid}/vehicle")
            .RequireAuthorization()
            .AddEndpointFilter<VehicleAuthorizationEndpointFilter>();

        vehicle.MapPost("/lookups", RequestLookupAsync)
            .WithName("RequestVehicleLookup")
            .WithSummary("Request a vehicle lookup")
            .WithDescription("Queues a provider lookup through the server-side vehicle workflow.")
            .WithMetadata(new VehicleAccessRightMetadata(StaffAccessRight.PerformCasework))
            .Produces<VehicleLookupResponse>(StatusCodes.Status202Accepted)
            .Produces<PegasusProblem>(StatusCodes.Status400BadRequest, "application/problem+json")
            .Produces<PegasusProblem>(StatusCodes.Status401Unauthorized, "application/problem+json")
            .Produces<PegasusProblem>(StatusCodes.Status403Forbidden, "application/problem+json")
            .Produces<PegasusProblem>(StatusCodes.Status409Conflict, "application/problem+json")
            .Produces<PegasusProblem>(StatusCodes.Status503ServiceUnavailable, "application/problem+json");

        vehicle.MapPost("/suggestions/{suggestionId:guid}/accept", AcceptSuggestionAsync)
            .WithName("AcceptVehicleSuggestion")
            .WithSummary("Accept a vehicle suggestion")
            .WithDescription("Accepts or corrects a provider vehicle suggestion through Core.")
            .WithMetadata(new VehicleAccessRightMetadata(StaffAccessRight.PerformCasework))
            .Produces<AcceptedVehicleSuggestionResponse>(StatusCodes.Status200OK)
            .Produces<PegasusProblem>(StatusCodes.Status400BadRequest, "application/problem+json")
            .Produces<PegasusProblem>(StatusCodes.Status401Unauthorized, "application/problem+json")
            .Produces<PegasusProblem>(StatusCodes.Status403Forbidden, "application/problem+json")
            .Produces<PegasusProblem>(StatusCodes.Status404NotFound, "application/problem+json")
            .Produces<PegasusProblem>(StatusCodes.Status409Conflict, "application/problem+json");

        vehicle.MapGet(string.Empty, GetEvidenceAsync)
            .WithName("GetVehicleEvidence")
            .WithSummary("Get vehicle evidence")
            .WithDescription("Returns confirmed vehicle evidence, lookup observations and provenance.")
            .WithMetadata(new VehicleAccessRightMetadata(StaffAccessRight.PerformCasework))
            .Produces<CaseVehicleResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status304NotModified)
            .Produces<PegasusProblem>(StatusCodes.Status401Unauthorized, "application/problem+json")
            .Produces<PegasusProblem>(StatusCodes.Status403Forbidden, "application/problem+json")
            .Produces<PegasusProblem>(StatusCodes.Status404NotFound, "application/problem+json");

        return group;
    }

    private static async Task<IResult> RequestLookupAsync(
        Guid caseId,
        Pegasus.Contracts.Vehicle.VehicleLookupRequest request,
        HttpContext httpContext,
        IRequestVehicleLookup workflow,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await workflow.ExecuteAsync(
                new(
                    caseId,
                    RequireExpectedVersion(request.ExpectedVersion),
                    new Pegasus.Core.Vehicle.VehicleLookupRequest(request.Registration).Registration,
                    VehicleAuthorizationEndpointFilter.GetActor(httpContext),
                    request.OperationKey,
                    request.EditLeaseToken,
                    VehicleGatewayCorrelation(httpContext)),
                cancellationToken);
            var response = new VehicleLookupResponse(
                result.WorkItemId,
                result.CaseId,
                result.Registration,
                ToWireValue(result.State),
                result.ResultingCaseVersion,
                VehicleGatewayCorrelation(httpContext),
                result.CorrelationId);
            return TypedResults.Accepted($"/api/v1/cases/{caseId:D}/vehicle", response);
        }
        catch (Exception exception) when (VehicleGatewayProblems.IsKnownVehicleException(exception))
        {
            return VehicleGatewayProblems.Result(httpContext, exception);
        }
    }

    private static async Task<IResult> AcceptSuggestionAsync(
        Guid caseId,
        Guid suggestionId,
        AcceptVehicleSuggestionRequest request,
        HttpContext httpContext,
        IAcceptVehicleSuggestion workflow,
        CancellationToken cancellationToken)
    {
        try
        {
            var decision = ParseDecision(request.Decision);
            var correction = request.Correction is null
                ? null
                : new VehicleConfirmationValues(
                    new Pegasus.Core.Vehicle.VehicleLookupRequest(request.Correction.Registration).Registration,
                    request.Correction.Make,
                    request.Correction.Model,
                    request.Correction.Mileage,
                    ParseMileageUnit(request.Correction.MileageUnit));
            var result = await workflow.ExecuteAsync(
                new(
                    caseId,
                    RequireExpectedVersion(request.ExpectedVersion),
                    suggestionId,
                    decision,
                    correction,
                    VehicleAuthorizationEndpointFilter.GetActor(httpContext),
                    request.OperationKey,
                    request.Reason,
                    request.EditLeaseToken),
                cancellationToken);
            return TypedResults.Ok(new AcceptedVehicleSuggestionResponse(
                result.ConfirmationId,
                result.CaseId,
                result.LookupObservationId,
                ToWireValue(result.Decision),
                Map(result.Values),
                Map(result.Provenance),
                result.ResultingCaseVersion,
                VehicleGatewayCorrelation(httpContext),
                result.CorrelationId));
        }
        catch (Exception exception) when (VehicleGatewayProblems.IsKnownVehicleException(exception))
        {
            return VehicleGatewayProblems.Result(httpContext, exception);
        }
    }

    private static async Task<IResult> GetEvidenceAsync(
        Guid caseId,
        HttpContext httpContext,
        IVehicleEvidenceQueries queries,
        CancellationToken cancellationToken)
    {
        var evidence = await queries.GetAsync(caseId, cancellationToken);
        if (evidence is null)
        {
            return VehicleGatewayProblems.NotFound(httpContext);
        }

        var response = new CaseVehicleResponse(
            evidence.CaseId,
            evidence.Version,
            Map(evidence.Confirmed),
            Map(evidence.LatestObservation),
            evidence.Observations.Select(observation => Map(observation)!).ToArray(),
            evidence.ConfirmationHistory.Select(Map).ToArray(),
            VehicleGatewayCorrelation(httpContext));
        var etag = CreateWeakEtag(response);
        httpContext.Response.Headers.ETag = etag;
        if (httpContext.Request.Headers.TryGetValue("If-None-Match", out var ifNoneMatch)
            && ifNoneMatch
                .ToString()
                .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Any(value => string.Equals(value, etag, StringComparison.Ordinal)))
        {
            return TypedResults.StatusCode(StatusCodes.Status304NotModified);
        }

        return TypedResults.Ok(response);
    }

    private static VehicleSuggestionDecision ParseDecision(string value) =>
        Enum.TryParse<VehicleSuggestionDecision>(value, ignoreCase: true, out var decision)
            && Enum.IsDefined(decision)
            ? decision
            : throw new ArgumentException("The vehicle decision is invalid.", nameof(value));

    private static long RequireExpectedVersion(long? value) => value
        ?? throw new ArgumentException(
            "The expected case version is required.",
            nameof(value));

    private static VehicleMileageUnit? ParseMileageUnit(string? value)
    {
        if (value is null)
        {
            return null;
        }

        return Enum.TryParse<VehicleMileageUnit>(value, ignoreCase: true, out var unit)
               && Enum.IsDefined(unit)
            ? unit
            : throw new ArgumentException("The vehicle mileage unit is invalid.", nameof(value));
    }

    private static string CreateWeakEtag(CaseVehicleResponse response) => $"W/\"{response.Version}\"";

    internal static string VehicleGatewayCorrelation(HttpContext context) =>
        context.Response.Headers[PegasusHeaders.CorrelationId].FirstOrDefault()
        ?? context.TraceIdentifier;

    internal static string ToWireValue(VehicleLookupOutcome value) => value switch
    {
        VehicleLookupOutcome.Current => "current",
        VehicleLookupOutcome.Stale => "stale",
        VehicleLookupOutcome.Partial => "partial",
        VehicleLookupOutcome.NotFound => "notFound",
        VehicleLookupOutcome.Throttled => "throttled",
        VehicleLookupOutcome.Unavailable => "unavailable",
        VehicleLookupOutcome.Failed => "failed",
        _ => throw new ArgumentOutOfRangeException(nameof(value))
    };

    private static string ToWireValue(VehicleLookupWorkState value) => value switch
    {
        VehicleLookupWorkState.Pending => "pending",
        VehicleLookupWorkState.Processing => "processing",
        VehicleLookupWorkState.RetryScheduled => "retryScheduled",
        VehicleLookupWorkState.Completed => "completed",
        VehicleLookupWorkState.Failed => "failed",
        VehicleLookupWorkState.Poisoned => "poisoned",
        _ => throw new ArgumentOutOfRangeException(nameof(value))
    };

    private static string ToWireValue(VehicleSuggestionDecision value) => value switch
    {
        VehicleSuggestionDecision.Accept => "accept",
        VehicleSuggestionDecision.Correct => "correct",
        _ => throw new ArgumentOutOfRangeException(nameof(value))
    };

    private static string ToWireValue(VehicleMileageUnit value) => value switch
    {
        VehicleMileageUnit.Miles => "miles",
        VehicleMileageUnit.Kilometres => "kilometres",
        _ => throw new ArgumentOutOfRangeException(nameof(value))
    };

    private static VehicleEvidenceProvenanceResponse Map(VehicleEvidenceProvenance provenance) =>
        new(
            provenance.Provider,
            provenance.ProviderVersion,
            provenance.ResponseIdentity,
            provenance.RetrievedAtUtc,
            provenance.EffectiveAtUtc,
            provenance.SourceObservedAtUtc,
            SourceAgeSeconds(provenance.RetrievedAtUtc, provenance.SourceObservedAtUtc));

    private static VehicleLookupObservationResponse? Map(VehicleLookupObservation? observation) => observation is null
        ? null
        : new(
            observation.Id,
            observation.WorkItemId,
            observation.CaseId,
            observation.AttemptNumber,
            ToWireValue(observation.Outcome),
            observation.Registration,
            Map(observation.Provenance),
            Map(observation.Vehicle),
            observation.MotTests.Select(Map).ToArray(),
            Map(observation.Mileage),
            Map(observation.Failure),
            observation.RecordedAtUtc,
            observation.CorrelationId);

    private static VehicleDetailsResponse? Map(VehicleDetails? vehicle) => vehicle is null
        ? null
        : new(vehicle.Make, vehicle.Model, vehicle.ManufactureYear, vehicle.EngineCapacityCc, vehicle.FuelType);

    private static MotTestResponse Map(MotTestObservation observation) =>
        new(
            observation.TestDate,
            observation.TestStatus,
            observation.ExpiryDate,
            observation.Mileage,
            observation.MileageUnit is { } unit ? ToWireValue(unit) : null);

    private static VehicleMileageResponse? Map(VehicleMileageCalculation? mileage) => mileage is null
        ? null
        : new(
            mileage.Value,
            ToWireValue(mileage.Unit),
            mileage.ObservedOn,
            mileage.MethodKey,
            mileage.MethodVersion,
            mileage.SupportingObservationCount);

    private static VehicleLookupFailureResponse? Map(VehicleLookupFailure? failure) => failure is null
        ? null
        : new(failure.Code, failure.Retryable, RetryAfterSeconds(failure.RetryAfter));

    private static ConfirmedVehicleEvidenceResponse? Map(ConfirmedVehicleEvidence? evidence) => evidence is null
        ? null
        : new(
            MapRequired(evidence.Registration),
            MapOptional(evidence.Make),
            MapOptional(evidence.Model),
            Map(evidence.Mileage),
            Map(evidence.MileageUnit));

    private static ConfirmedVehicleTextFieldResponse MapRequired(ConfirmedVehicleField<string> field) =>
        new(
            field.Value,
            field.SourceKind,
            field.SourceIdentity,
            field.SourceLabel,
            field.PolicyKey,
            field.PolicyVersion,
            field.ConfirmedByActor,
            field.ConfirmedAtUtc,
            field.ExternalProvenance is { } provenance ? Map(provenance) : null);

    private static ConfirmedVehicleTextFieldResponse? MapOptional(ConfirmedVehicleField<string>? field) =>
        field is null ? null : MapRequired(field);

    private static ConfirmedVehicleMileageFieldResponse? Map(ConfirmedVehicleField<long>? field) => field is null
        ? null
        : new(
            field.Value,
            field.SourceKind,
            field.SourceIdentity,
            field.SourceLabel,
            field.PolicyKey,
            field.PolicyVersion,
            field.ConfirmedByActor,
            field.ConfirmedAtUtc,
            field.ExternalProvenance is { } provenance ? Map(provenance) : null);

    private static ConfirmedVehicleMileageUnitFieldResponse? Map(
        ConfirmedVehicleField<VehicleMileageUnit>? field) => field is null
        ? null
        : new(
            ToWireValue(field.Value),
            field.SourceKind,
            field.SourceIdentity,
            field.SourceLabel,
            field.PolicyKey,
            field.PolicyVersion,
            field.ConfirmedByActor,
            field.ConfirmedAtUtc,
            field.ExternalProvenance is { } provenance ? Map(provenance) : null);

    private static VehicleConfirmationHistoryResponse Map(VehicleConfirmationHistory history) =>
        new(
            history.Id,
            history.CaseId,
            history.LookupObservationId,
            ToWireValue(history.Decision),
            Map(history.Values),
            history.Actor.SubjectId,
            history.Reason,
            history.OccurredAtUtc,
            history.BeforeCaseVersion,
            history.AfterCaseVersion,
            history.PolicyKey,
            history.PolicyVersion);

    private static VehicleConfirmationValuesResponse Map(VehicleConfirmationValues values) =>
        new(
            values.Registration,
            values.Make,
            values.Model,
            values.Mileage,
            values.MileageUnit is { } unit ? ToWireValue(unit) : null);

    private static long? SourceAgeSeconds(DateTimeOffset retrievedAtUtc, DateTimeOffset? observedAtUtc) =>
        observedAtUtc is { } observed
            ? (long)(retrievedAtUtc - observed).TotalSeconds
            : null;

    private static long? RetryAfterSeconds(TimeSpan? retryAfter) =>
        retryAfter is { } value ? (long)value.TotalSeconds : null;
}

internal sealed record VehicleAccessRightMetadata(StaffAccessRight AccessRight);

internal sealed class VehicleAuthorizationEndpointFilter : IEndpointFilter
{
    private static readonly object ActorKey = new();

    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var actor = GetActor(context.HttpContext);
        context.HttpContext.Items[ActorKey] = actor;
        return await next(context);
    }

    public static ActionActor GetActor(HttpContext httpContext)
    {
        if (httpContext.Items.TryGetValue(ActorKey, out var value) && value is ActionActor cachedActor)
        {
            return cachedActor;
        }

        var subjectId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var roles = httpContext.User.FindAll(ClaimTypes.Role).Select(claim => claim.Value);
        if (!StaffActorFactory.TryCreate(subjectId, roles, out ActionActor? resolvedActor)
            || resolvedActor is null)
        {
            throw new StaffAuthorizationException(StaffAccessRight.PerformCasework);
        }

        StaffAuthorization.Require(resolvedActor, StaffAccessRight.PerformCasework);
        return resolvedActor;
    }
}

internal static class VehicleGatewayProblems
{
    public static bool IsKnownVehicleException(Exception exception) => exception is
        ArgumentException or
        VehicleLookupUnavailableException or
        VehicleOperationConflictException or
        VehicleSuggestionUnavailableException or
        ConfirmedVehicleRegistrationRequiredException or
        ConfirmedVehicleRegistrationConflictException or
        ConfirmedVehicleFieldConflictException;

    public static IResult Result(HttpContext context, Exception exception)
    {
        var correlationId = VehicleEndpoints.VehicleGatewayCorrelation(context);
        var (type, title, status, detail, extensions) = exception switch
        {
            VehicleLookupUnavailableException unavailable => (
                PegasusProblemTypes.ProviderUnavailable,
                "Provider unavailable",
                StatusCodes.Status503ServiceUnavailable,
                "Vehicle lookup is unavailable in the current runtime profile.",
                new Dictionary<string, object?> { ["mode"] = unavailable.Mode }),
            VehicleOperationConflictException => (
                PegasusProblemTypes.OperationConflict,
                "Operation conflict",
                StatusCodes.Status409Conflict,
                "The vehicle operation key was already used with different inputs.",
                null),
            VehicleSuggestionUnavailableException suggestion => (
                PegasusProblemTypes.VehicleSuggestionUnavailable,
                "Suggestion not available",
                StatusCodes.Status404NotFound,
                "The vehicle suggestion is not available for acceptance.",
                new Dictionary<string, object?>
                {
                    ["observationId"] = suggestion.ObservationId,
                    ["outcome"] = VehicleEndpoints.ToWireValue(suggestion.Outcome)
                }),
            ConfirmedVehicleRegistrationRequiredException => (
                PegasusProblemTypes.VehicleRegistrationRequired,
                "Validation failed",
                StatusCodes.Status400BadRequest,
                "Exactly one confirmed vehicle registration is required before lookup.",
                null),
            ConfirmedVehicleRegistrationConflictException => (
                PegasusProblemTypes.VehicleRegistrationConflict,
                "Version conflict",
                StatusCodes.Status409Conflict,
                "The case has a different confirmed vehicle registration.",
                null),
            ConfirmedVehicleFieldConflictException field => (
                PegasusProblemTypes.VehicleFieldConflict,
                "Version conflict",
                StatusCodes.Status409Conflict,
                "The case has a different confirmed vehicle value.",
                new Dictionary<string, object?> { ["field"] = field.FieldName }),
            ArgumentException => (
                PegasusProblemTypes.Validation,
                "Validation failed",
                StatusCodes.Status400BadRequest,
                "The vehicle request was rejected by a validation rule.",
                null),
            _ => throw new ArgumentOutOfRangeException(nameof(exception))
        };

        return new VehicleProblemResult(new(
            type,
            title,
            status,
            detail,
            null,
            correlationId,
            extensions));
    }

    public static IResult NotFound(HttpContext context) => new VehicleProblemResult(new(
        PegasusProblemTypes.NotFound,
        "Not found",
        StatusCodes.Status404NotFound,
        "The requested vehicle evidence was not found.",
        null,
        VehicleEndpoints.VehicleGatewayCorrelation(context)));

    private sealed class VehicleProblemResult(PegasusProblem problem) : IResult
    {
        public Task ExecuteAsync(HttpContext httpContext) =>
            DesktopGatewayProblems.WriteAsync(httpContext, problem, httpContext.RequestAborted);
    }
}
