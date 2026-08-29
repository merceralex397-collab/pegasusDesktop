namespace Pegasus.Contracts.Responses;

/// <summary>Canonical metadata for one case-document occurrence and version.</summary>
public sealed record DocumentMetadataResponse(
    Guid CaseId,
    Guid DocumentId,
    Guid OccurrenceId,
    Guid VersionId,
    string FileName,
    string MediaType,
    long ContentLength,
    string Sha256,
    string SemanticRole,
    string Source,
    string CustodyStatus,
    DateTimeOffset CreatedAtUtc,
    string CreatedBy,
    bool IsCurrent,
    bool IsLogicallyRemoved,
    string? RemovalReason,
    string SourceOccurrenceIdentity,
    DateTimeOffset RecordedAtUtc,
    DateTimeOffset? ThirdPartyVehicleConfirmedAtUtc,
    string? ThirdPartyVehicleConfirmationReason,
    int Ordinal);

/// <summary>A paged case-document metadata response.</summary>
public sealed record DocumentListResponse(
    IReadOnlyList<DocumentMetadataResponse> Items,
    long Version,
    int Page,
    int PageSize,
    bool HasPreviousPage,
    bool HasNextPage);

/// <summary>Payload that starts one bounded document upload session.</summary>
public sealed record CreateDocumentUploadSessionRequest
{
    /// <summary>The leaf file name to retain in canonical custody.</summary>
    public required string FileName { get; init; }

    /// <summary>The declared media type of the document.</summary>
    public required string MediaType { get; init; }

    /// <summary>The canonical document semantic role.</summary>
    public required string SemanticRole { get; init; }
}

/// <summary>Details of an active bounded document upload session.</summary>
public sealed record DocumentUploadSessionResponse(
    Guid SessionId,
    DateTimeOffset ExpiresAtUtc,
    long MaximumContentLength);

/// <summary>Payload that completes a document upload session.</summary>
public sealed record CompleteDocumentUploadRequest
{
    /// <summary>The case version observed when edit authority was acquired.</summary>
    public long ExpectedVersion { get; init; }

    /// <summary>The caller-supplied idempotency key.</summary>
    public required string OperationKey { get; init; }

    /// <summary>The active case edit lease token.</summary>
    public required string EditLeaseToken { get; init; }
}

/// <summary>Result of completing a document upload session.</summary>
public sealed record DocumentUploadCompletionResponse(
    DocumentMetadataResponse Document,
    bool IsReplay);

/// <summary>Payload for a reasoned logical document removal.</summary>
public sealed record RemoveDocumentRequest
{
    /// <summary>The case version observed by the caller.</summary>
    public long ExpectedVersion { get; init; }

    /// <summary>The caller-supplied idempotency key.</summary>
    public required string OperationKey { get; init; }

    /// <summary>The active case edit lease token.</summary>
    public required string EditLeaseToken { get; init; }

    /// <summary>The operator's reason for logical removal.</summary>
    public required string Reason { get; init; }
}

/// <summary>Payload for confirming third-party vehicle evidence.</summary>
public sealed record ConfirmThirdPartyEvidenceRequest
{
    /// <summary>The case-document occurrence to confirm.</summary>
    public Guid OccurrenceId { get; init; }

    /// <summary>The case version observed by the caller.</summary>
    public long ExpectedVersion { get; init; }

    /// <summary>The caller-supplied idempotency key.</summary>
    public required string OperationKey { get; init; }

    /// <summary>The active case edit lease token.</summary>
    public required string EditLeaseToken { get; init; }

    /// <summary>The operator's reason for the confirmation.</summary>
    public required string Reason { get; init; }
}

/// <summary>Result of a document mutation.</summary>
public sealed record DocumentMutationResponse(
    Guid CaseId,
    Guid OccurrenceId,
    long Version);
