namespace Pegasus.Contracts.Vehicle;

/// <summary>Requests a vehicle lookup for a case.</summary>
public sealed record VehicleLookupRequest
{
    /// <summary>The registration in the Core-normalized uppercase form.</summary>
    public required string Registration { get; init; }

    /// <summary>The case version the caller read before requesting the lookup.</summary>
    public long ExpectedVersion { get; init; }

    /// <summary>The caller-supplied idempotency key.</summary>
    public required string OperationKey { get; init; }

    /// <summary>The active case edit lease token.</summary>
    public required string EditLeaseToken { get; init; }
}

/// <summary>Corrected values supplied when accepting a vehicle suggestion.</summary>
public sealed record VehicleCorrectionRequest
{
    /// <summary>The corrected normalized registration.</summary>
    public required string Registration { get; init; }

    /// <summary>The corrected make.</summary>
    public string? Make { get; init; }

    /// <summary>The corrected model.</summary>
    public string? Model { get; init; }

    /// <summary>The corrected mileage.</summary>
    public long? Mileage { get; init; }

    /// <summary>The unit for the corrected mileage.</summary>
    public string? MileageUnit { get; init; }
}

/// <summary>Accepts or corrects a vehicle lookup suggestion.</summary>
public sealed record AcceptVehicleSuggestionRequest
{
    /// <summary>The case version the caller read before accepting the suggestion.</summary>
    public long ExpectedVersion { get; init; }

    /// <summary>The lookup decision, either <c>accept</c> or <c>correct</c>.</summary>
    public required string Decision { get; init; }

    /// <summary>The corrected values when the decision is <c>correct</c>.</summary>
    public VehicleCorrectionRequest? Correction { get; init; }

    /// <summary>The caller-supplied idempotency key.</summary>
    public required string OperationKey { get; init; }

    /// <summary>The reason for the decision.</summary>
    public required string Reason { get; init; }

    /// <summary>The active case edit lease token.</summary>
    public required string EditLeaseToken { get; init; }
}

/// <summary>Reports a queued vehicle lookup.</summary>
public sealed record VehicleLookupResponse(
    Guid WorkItemId,
    Guid CaseId,
    string Registration,
    string State,
    long ResultingCaseVersion,
    string CorrelationId);

/// <summary>Reports an accepted vehicle suggestion and its provenance.</summary>
public sealed record AcceptedVehicleSuggestionResponse(
    Guid ConfirmationId,
    Guid CaseId,
    Guid LookupObservationId,
    string Decision,
    VehicleConfirmationValuesResponse Values,
    VehicleEvidenceProvenanceResponse Provenance,
    long ResultingCaseVersion,
    string CorrelationId);

/// <summary>Confirmed vehicle values written to a case.</summary>
public sealed record VehicleConfirmationValuesResponse(
    string Registration,
    string? Make,
    string? Model,
    long? Mileage,
    string? MileageUnit);

/// <summary>Vehicle provider provenance carried with an observation or confirmation.</summary>
public sealed record VehicleEvidenceProvenanceResponse(
    string Provider,
    string ProviderVersion,
    string ResponseIdentity,
    DateTimeOffset RetrievedAtUtc,
    DateTimeOffset? EffectiveAtUtc,
    DateTimeOffset? SourceObservedAtUtc,
    long? SourceAgeSeconds);

/// <summary>Safe failure metadata for a vehicle lookup.</summary>
public sealed record VehicleLookupFailureResponse(
    string Code,
    bool Retryable,
    long? RetryAfterSeconds);

/// <summary>Vehicle details returned by a provider lookup.</summary>
public sealed record VehicleDetailsResponse(
    string? Make,
    string? Model,
    int? ManufactureYear,
    int? EngineCapacityCc,
    string? FuelType);

/// <summary>An MOT observation returned by a vehicle lookup.</summary>
public sealed record MotTestResponse(
    DateOnly TestDate,
    string TestStatus,
    DateOnly? ExpiryDate,
    long? Mileage,
    string? MileageUnit);

/// <summary>Derived mileage calculated from MOT observations.</summary>
public sealed record VehicleMileageResponse(
    long Value,
    string Unit,
    DateOnly ObservedOn,
    string MethodKey,
    int MethodVersion,
    int SupportingObservationCount);

/// <summary>A provider lookup observation for a case.</summary>
public sealed record VehicleLookupObservationResponse(
    Guid Id,
    Guid WorkItemId,
    Guid CaseId,
    int AttemptNumber,
    string Outcome,
    string Registration,
    VehicleEvidenceProvenanceResponse Provenance,
    VehicleDetailsResponse? Vehicle,
    IReadOnlyList<MotTestResponse> MotTests,
    VehicleMileageResponse? Mileage,
    VehicleLookupFailureResponse? Failure,
    DateTimeOffset RecordedAtUtc);

/// <summary>A confirmed text field and its audit provenance.</summary>
public sealed record ConfirmedVehicleTextFieldResponse(
    string Value,
    string SourceKind,
    string SourceIdentity,
    string SourceLabel,
    string PolicyKey,
    int PolicyVersion,
    string ConfirmedByActor,
    DateTimeOffset ConfirmedAtUtc,
    VehicleEvidenceProvenanceResponse? ExternalProvenance);

/// <summary>A confirmed mileage field and its audit provenance.</summary>
public sealed record ConfirmedVehicleMileageFieldResponse(
    long Value,
    string SourceKind,
    string SourceIdentity,
    string SourceLabel,
    string PolicyKey,
    int PolicyVersion,
    string ConfirmedByActor,
    DateTimeOffset ConfirmedAtUtc,
    VehicleEvidenceProvenanceResponse? ExternalProvenance);

/// <summary>A confirmed mileage-unit field and its audit provenance.</summary>
public sealed record ConfirmedVehicleMileageUnitFieldResponse(
    string Value,
    string SourceKind,
    string SourceIdentity,
    string SourceLabel,
    string PolicyKey,
    int PolicyVersion,
    string ConfirmedByActor,
    DateTimeOffset ConfirmedAtUtc,
    VehicleEvidenceProvenanceResponse? ExternalProvenance);

/// <summary>The confirmed vehicle evidence projection.</summary>
public sealed record ConfirmedVehicleEvidenceResponse(
    ConfirmedVehicleTextFieldResponse Registration,
    ConfirmedVehicleTextFieldResponse? Make,
    ConfirmedVehicleTextFieldResponse? Model,
    ConfirmedVehicleMileageFieldResponse? Mileage,
    ConfirmedVehicleMileageUnitFieldResponse? MileageUnit);

/// <summary>A recorded vehicle confirmation decision.</summary>
public sealed record VehicleConfirmationHistoryResponse(
    Guid Id,
    Guid CaseId,
    Guid LookupObservationId,
    string Decision,
    VehicleConfirmationValuesResponse Values,
    string ActorSubjectId,
    string Reason,
    DateTimeOffset OccurredAtUtc,
    long BeforeCaseVersion,
    long AfterCaseVersion,
    string PolicyKey,
    int PolicyVersion);

/// <summary>The vehicle evidence projection for a case.</summary>
public sealed record CaseVehicleResponse(
    Guid CaseId,
    long Version,
    ConfirmedVehicleEvidenceResponse? Confirmed,
    VehicleLookupObservationResponse? LatestObservation,
    IReadOnlyList<VehicleLookupObservationResponse> Observations,
    IReadOnlyList<VehicleConfirmationHistoryResponse> ConfirmationHistory,
    string CorrelationId);
