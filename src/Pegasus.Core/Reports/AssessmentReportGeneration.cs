using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Pegasus.Core.Identity;

namespace Pegasus.Core.Reports;

public enum AssessmentReportGenerationState
{
    Pending,
    Rendering,
    Generated,
    Failed
}

public enum AssessmentReportArtifactKind
{
    Assessment,
    FeeNote
}

public static class AssessmentReportFailureMessages
{
    public const string GenerationFailed =
        "The report draft could not be generated. Retry the operation.";
}

public sealed record AssessmentReportLogicalKey(
    Guid CaseId,
    string AssessmentFamily,
    string AcceptedPayloadSha256,
    string TemplateVersion)
{
    public string Value => string.Join(
        ":",
        CaseId.ToString("N"),
        AssessmentFamily,
        AcceptedPayloadSha256,
        TemplateVersion);

    public void Validate()
    {
        if (CaseId == Guid.Empty)
        {
            throw new ArgumentException("A case identifier is required.", nameof(CaseId));
        }

        Required(AssessmentFamily, nameof(AssessmentFamily));
        Required(TemplateVersion, nameof(TemplateVersion));
        if (AcceptedPayloadSha256.Length != SHA256.HashSizeInBytes * 2
            || !AcceptedPayloadSha256.All(Uri.IsHexDigit))
        {
            throw new ArgumentException("The accepted report payload requires a SHA-256 hash.", nameof(AcceptedPayloadSha256));
        }
    }

    private static void Required(string value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{name} is required.", name);
        }
    }
}

public static class AssessmentReportPayload
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    public static string Serialize(AssessmentReportSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        snapshot.Validate();
        return Encoding.UTF8.GetString(JsonSerializer.SerializeToUtf8Bytes(snapshot, Options));
    }

    public static string Hash(string payloadJson)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(payloadJson);
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(payloadJson)));
    }

    public static AssessmentReportSnapshot Deserialize(string payloadJson)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(payloadJson);
        var snapshot = JsonSerializer.Deserialize<AssessmentReportSnapshot>(payloadJson, Options)
            ?? throw new InvalidDataException("The stored report snapshot is empty.");
        snapshot.Validate();
        return snapshot;
    }

    public static AssessmentReportLogicalKey Key(AssessmentReportSnapshot snapshot)
    {
        return new(
            snapshot.CaseId,
            "accepted-assessment",
            // The report date and optimistic case-version token are request
            // metadata, not accepted report facts. Neither may turn an exact
            // replay into a new report after UTC midnight or an unrelated
            // case edit. The selected repair-specification identity remains
            // in the hash so a newly accepted estimate is a successor.
            Hash(Serialize(snapshot with
            {
                ReportDate = default,
                AssessmentCaseVersion = 0
            })),
            snapshot.PayloadVersion);
    }
}

public sealed record AssessmentReportArtifact(
    Guid Id,
    AssessmentReportArtifactKind Kind,
    string SuggestedFileName,
    string MediaType,
    long ContentLength,
    string Sha256,
    int PageCount,
    string TemplateVersion,
    string EngineVersion)
{
    public void Validate()
    {
        if (Id == Guid.Empty)
        {
            throw new ArgumentException("A report artifact identifier is required.", nameof(Id));
        }

        if (string.IsNullOrWhiteSpace(SuggestedFileName)
            || string.IsNullOrWhiteSpace(MediaType)
            || ContentLength < 0
            || PageCount < 0
            || Sha256.Length != SHA256.HashSizeInBytes * 2
            || !Sha256.All(Uri.IsHexDigit))
        {
            throw new ArgumentException("Report artifact metadata is incomplete.", nameof(SuggestedFileName));
        }
    }
}

public sealed record AssessmentReportVersion(
    Guid Id,
    Guid CaseId,
    int Version,
    AssessmentReportLogicalKey LogicalKey,
    AssessmentReportGenerationState State,
    string AcceptedPayloadJson,
    Guid? PredecessorId,
    IReadOnlyList<AssessmentReportArtifact> Artifacts,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    string? FailureReason,
    int AttemptCount = 0,
    DateTimeOffset? NextAttemptAtUtc = null,
    DateTimeOffset? LeaseExpiresAtUtc = null)
{
    public void Validate()
    {
        LogicalKey.Validate();
        if (Id == Guid.Empty || CaseId != LogicalKey.CaseId || Version <= 0)
        {
            throw new ArgumentException("Report version identity is incomplete.", nameof(Id));
        }

        if (State == AssessmentReportGenerationState.Generated)
        {
            if (Artifacts.Count != 2 || Artifacts.Select(item => item.Kind).Distinct().Count() != 2)
            {
                throw new ArgumentException("A generated report requires one assessment and one fee-note artifact.", nameof(Artifacts));
            }

            foreach (var artifact in Artifacts)
            {
                artifact.Validate();
            }
        }
    }
}

public static class AssessmentReportRetryPolicy
{
    public const int MaxAttempts = 3;

    public static bool CanRetry(int attemptCount) => attemptCount < MaxAttempts;

    public static DateTimeOffset NextAttemptAt(DateTimeOffset now, int attemptCount) =>
        now.AddSeconds(Math.Min(60, Math.Max(1, attemptCount * 5)));
}

public sealed record AssessmentReportGenerationRequest(
    Guid CaseId,
    AssessmentReportSnapshot Snapshot,
    ActionActor Actor);

public sealed record AssessmentReportGenerationReservation(
    AssessmentReportVersion Version,
    string LeaseId,
    bool ShouldRender)
{
    public bool IsReplay => !ShouldRender && Version.State == AssessmentReportGenerationState.Generated;
}

public interface IAssessmentReportStore
{
    Task<IReadOnlyList<AssessmentReportVersion>> ListAsync(
        Guid caseId,
        CancellationToken cancellationToken = default);

    Task<AssessmentReportGenerationReservation> BeginAsync(
        AssessmentReportGenerationRequest request,
        CancellationToken cancellationToken = default);

    Task<AssessmentReportDraft?> ReadDraftAsync(
        AssessmentReportVersion version,
        CancellationToken cancellationToken = default);

    Task<AssessmentReportVersion> CompleteAsync(
        AssessmentReportGenerationReservation reservation,
        AssessmentReportDraft draft,
        CancellationToken cancellationToken = default);

    Task FailAsync(
        AssessmentReportGenerationReservation reservation,
        string reason,
        CancellationToken cancellationToken = default);
}
