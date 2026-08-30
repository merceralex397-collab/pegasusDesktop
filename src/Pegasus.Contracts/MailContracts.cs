namespace Pegasus.Contracts.Mail;

public sealed record MailboxResponse(
    string MailboxId,
    string MailboxAddress,
    bool IsPolled);

public sealed record MailFreshnessResponse(
    string State,
    DateTimeOffset? LastSuccessfulUpdateAtUtc);

public sealed record MailSearchMatchResponse(
    string Kind,
    string? AttachmentFileName,
    int? AttachmentOrdinal);

public sealed record MailCategoryResponse(
    string Direction,
    string Name,
    string? ReceivedFamily,
    string? SentFamily,
    string? Subtype,
    bool IsReplyContext,
    bool IsOther,
    string? OtherName,
    string? OtherReasoning);

public sealed record MailPredicateResponse(
    string Key,
    bool Matched,
    string Detail);

public sealed record MailStandaloneAuditResponse(
    string AssetSourceLabel,
    string Assessment);

public sealed record MailClassificationResultResponse(
    string Outcome,
    MailCategoryResponse? Category,
    IReadOnlyList<string> AmbiguousCandidates,
    IReadOnlyList<MailPredicateResponse> Predicates,
    string Reason,
    string PolicyKey,
    int PolicyVersion,
    string? CaseType,
    MailStandaloneAuditResponse? StandaloneAuditReport);

public sealed record MailClassificationHistoryResponse(
    int Version,
    MailClassificationResultResponse Before,
    MailClassificationResultResponse After,
    string ActorDisplayName,
    string Reason,
    DateTimeOffset CorrectedAtUtc);

public sealed record MailClassificationResponse(
    int Version,
    MailClassificationResultResponse Current,
    string CurrentActorDisplayName,
    DateTimeOffset CurrentDecidedAtUtc,
    string OperationalDestination,
    IReadOnlyList<MailClassificationHistoryResponse> History,
    IReadOnlyList<MailClassificationOptionResponse> CorrectionOptions);

public sealed record MailClassificationOptionResponse(
    string Value,
    string Label);

public sealed record MailFolderRecommendationResponse(
    string? FolderType,
    string PolicyKey,
    int PolicyVersion,
    string Reason,
    int? MailboxVersion,
    bool CanMove);

public sealed record MailSuggestedMoveResponse(
    string FolderType,
    string Reason);

public sealed record MailFolderMoveResponse(
    string Outcome,
    string FolderType,
    string Reason,
    DateTimeOffset RecordedAtUtc,
    bool IsReplay,
    string? OperationKey,
    string? FailureReason,
    int? ExpectedClassificationVersion,
    string? ExpectedRecommendationPolicyKey,
    int? ExpectedRecommendationPolicyVersion,
    int? ExpectedMailboxVersion,
    string OperatorMessage);

public sealed record MailSummaryResponse(
    Guid Id,
    string MailboxId,
    string MailboxAddress,
    bool MailboxIsPolled,
    string? SenderAddress,
    string? SenderDisplayName,
    string? EffectiveSenderAddress,
    string? Subject,
    string? BodyExcerpt,
    DateTimeOffset ReceivedAtUtc,
    bool IsRead,
    int AttachmentCount,
    string? ProcessingOutcome,
    Guid? IntakeReceiptId,
    Guid? CaseId,
    string? CaseReference,
    string? AllocationState,
    IReadOnlyList<MailSearchMatchResponse> SearchMatches,
    string? CurrentFolderType,
    MailClassificationResultResponse? Classification,
    string? OperationalDestination,
    long? IntakeVersion,
    long? CaseVersion);

public sealed record MailPageResponse(
    IReadOnlyList<MailSummaryResponse> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages,
    bool HasUnretainedHistory,
    IReadOnlyList<MailboxResponse> Mailboxes,
    MailFreshnessResponse Freshness,
    string Version);

public sealed record MailAttachmentResponse(
    string FileName,
    string MediaType,
    long ContentLength,
    bool IsSearchable);

public sealed record MailThreadEntryResponse(
    Guid Id,
    string? SenderDisplayName,
    string? SenderAddress,
    string? Subject,
    DateTimeOffset ReceivedAtUtc);

public sealed record MailDetailResponse(
    MailSummaryResponse Summary,
    IReadOnlyList<string> ToAddresses,
    IReadOnlyList<string> CcAddresses,
    string? BodyPlainText,
    IReadOnlyList<MailAttachmentResponse> Attachments,
    IReadOnlyList<MailThreadEntryResponse> Thread,
    string Folder,
    string? ClassificationOutcome,
    string? RouteDisposition,
    MailClassificationResponse? Classification,
    MailFolderRecommendationResponse? FolderRecommendation,
    MailFolderMoveResponse? LatestFolderMove,
    MailSuggestedMoveResponse? SuggestedMove,
    string Version);

public sealed record DeletedMailItemResponse(
    string MailboxId,
    string MailboxAddress,
    string ImmutableMessageId,
    string? SenderAddress,
    string? SenderDisplayName,
    string? Subject,
    string? BodyPlainText,
    DateTimeOffset ReceivedAtUtc,
    bool IsRead,
    IReadOnlyList<MailAttachmentResponse> Attachments,
    IReadOnlyList<MailSearchMatchResponse> Matches);

public sealed record DeletedMailPageResponse(
    IReadOnlyList<DeletedMailItemResponse> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages,
    bool IsTruncated,
    string State,
    IReadOnlyList<MailboxResponse> Mailboxes,
    string Version);

public sealed record MailPreviewResponse(
    Guid Id,
    string Sender,
    string Subject,
    DateTimeOffset ReceivedAtUtc,
    string Received,
    string Excerpt,
    string Classification,
    string Association,
    IReadOnlyList<string> Attachments,
    string Version);

public sealed record MailCasePreparationRequest(
    Guid CaseId,
    long ExpectedIntakeVersion,
    long ExpectedCaseVersion,
    string LeaseOperationKey);

public sealed record MailCasePreparationResponse(
    Guid MessageId,
    Guid ReceiptId,
    string Action,
    Guid CaseId,
    long ExpectedIntakeVersion,
    long ExpectedCaseVersion,
    string LeaseToken,
    DateTimeOffset ExpiresAtUtc,
    string? Consequence = null);

public sealed record MailCaseAssociationRequest(
    Guid CaseId,
    long ExpectedIntakeVersion,
    long ExpectedCaseVersion,
    string EditLeaseToken,
    string OperationKey,
    string Reason);

public sealed record MailCaseAssociationResponse(
    Guid MessageId,
    Guid ReceiptId,
    string Action,
    Guid CaseId,
    long Version,
    string? Consequence = null);

public sealed record MailClassificationCorrectionRequest(
    int ExpectedClassificationVersion,
    string ClassificationKey,
    string Reason,
    string OperationKey,
    string? OtherName = null,
    string? OtherReasoning = null);

public sealed record MailMoveRequest(
    int ExpectedClassificationVersion,
    string ExpectedRecommendationPolicyKey,
    int ExpectedRecommendationPolicyVersion,
    int ExpectedMailboxVersion,
    string OperationKey,
    string Reason);
