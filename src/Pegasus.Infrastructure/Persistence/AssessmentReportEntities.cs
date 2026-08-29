using Pegasus.Core.Reports;

namespace Pegasus.Infrastructure.Persistence;

internal sealed class AssessmentReportVersionEntity
{
    public Guid Id { get; set; }
    public Guid CaseId { get; set; }
    public CaseEntity Case { get; set; } = null!;
    public int Version { get; set; }
    public required string AssessmentFamily { get; set; }
    public required string AcceptedPayloadSha256 { get; set; }
    public required string TemplateVersion { get; set; }
    public required string LogicalKey { get; set; }
    public required string State { get; set; }
    public required string AcceptedPayloadJson { get; set; }
    public Guid? PredecessorId { get; set; }
    public AssessmentReportVersionEntity? Predecessor { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public string? FailureReason { get; set; }
    public int AttemptCount { get; set; }
    public DateTimeOffset? NextAttemptAtUtc { get; set; }
    public string? LeaseId { get; set; }
    public DateTimeOffset? LeaseExpiresAtUtc { get; set; }
    public List<AssessmentReportArtifactEntity> Artifacts { get; set; } = [];
}

internal sealed class AssessmentReportArtifactEntity
{
    public Guid Id { get; set; }
    public Guid ReportVersionId { get; set; }
    public AssessmentReportVersionEntity ReportVersion { get; set; } = null!;
    public required string Kind { get; set; }
    public Guid OccurrenceId { get; set; }
    public Guid DocumentId { get; set; }
    public Guid DocumentVersionId { get; set; }
    public int DocumentVersion { get; set; }
    public int DocumentOrdinal { get; set; }
    public required string FileName { get; set; }
    public required string MediaType { get; set; }
    public long ContentLength { get; set; }
    public required string Sha256 { get; set; }
    public int PageCount { get; set; }
    public required string TemplateVersion { get; set; }
    public required string EngineVersion { get; set; }
}
