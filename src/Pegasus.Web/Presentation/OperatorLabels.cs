using System.Globalization;
using Pegasus.Contracts.Vocabulary;
using Pegasus.Core.Assessment;
using Pegasus.Core.Cases;
using Pegasus.Core.Documents;
using Pegasus.Core.ImageIntake;
using Pegasus.Core.Intake;
using Pegasus.Core.Tasks;
using Pegasus.Core.Workflow;
using Pegasus.Core.Identity;
using Pegasus.Core.Vehicle;
using Pegasus.Core.Intake.Unidentified;
using Pegasus.Core.Reports;

namespace Pegasus.Web.Presentation;

/// <summary>
/// Web adapter for the shared operator vocabulary. Core-shaped values are
/// converted to their stable names here; the words themselves have one owner
/// in <see cref="OperatorVocabulary"/>.
/// </summary>
public static class OperatorLabels
{
    public static string AttachmentSearchability(bool isSearchable) =>
        OperatorVocabulary.AttachmentSearchability(isSearchable);

    public static string UnidentifiedReason(UnidentifiedReasonCode reason) =>
        OperatorVocabulary.UnidentifiedReason(reason.ToString());

    public static string UnidentifiedState(UnidentifiedState state) =>
        OperatorVocabulary.UnidentifiedState(state.ToString());

    public static string UnidentifiedMediaKind(UnidentifiedMediaKind kind) =>
        OperatorVocabulary.UnidentifiedMediaKind(kind.ToString());

    public static string EmailHandle(string? subject, string? sender) =>
        OperatorVocabulary.EmailHandle(subject, sender);

    public static string AssociatedWithCase(string? caseReference, bool byStaffDecision) =>
        OperatorVocabulary.AssociatedWithCase(caseReference, byStaffDecision);

    public static string CaseStage(CaseLifecycleState state) =>
        OperatorVocabulary.CaseStage(state.ToString());

    public static string CaseStage(string? state) =>
        Enum.TryParse<CaseLifecycleState>(state, ignoreCase: true, out var parsed)
            ? CaseStage(parsed)
            : OperatorVocabulary.CaseStage(state);

    public static string CaseTypeName(CaseType type) =>
        OperatorVocabulary.CaseTypeName(type.ToString());

    public static string CaseTypeName(string? type) =>
        Enum.TryParse<CaseType>(type, ignoreCase: true, out var parsed)
            ? CaseTypeName(parsed)
            : OperatorVocabulary.CaseTypeName(type);

    public static string AttemptedCaseTypeName(CaseType? type) => type is { } present
        ? OperatorVocabulary.AttemptedCaseTypeName(present.ToString())
        : "Not available";

    public static string ChaseState(CaseDueWorkState state) =>
        OperatorVocabulary.ChaseState(state.ToString());

    public static string ImageChaseState(bool chaseDue) =>
        OperatorVocabulary.ImageChaseState(chaseDue);

    public static string MailOperationalDestinationLabel(MailOperationalDestination destination) =>
        OperatorVocabulary.MailOperationalDestinationLabel(destination.ToString());

    public static string RepairSpecificationRoute(RepairSpecificationSourceRoute route) =>
        OperatorVocabulary.RepairSpecificationRoute(route.ToString());

    public static string EstimateLineType(string type) => OperatorVocabulary.EstimateLineType(type);

    public static string DocumentRole(DocumentSemanticRole role) =>
        OperatorVocabulary.DocumentRole(role.ToString());

    public static string ReportGenerationState(
        AssessmentReportGenerationState state,
        DateTimeOffset? retryAtUtc = null) =>
        state == AssessmentReportGenerationState.Pending && retryAtUtc is not null
            ? "Retry"
            : state switch
            {
                AssessmentReportGenerationState.Pending => "Pending",
                AssessmentReportGenerationState.Rendering => "Rendering",
                AssessmentReportGenerationState.Generated => "Generated",
                AssessmentReportGenerationState.Failed => "Failed",
                _ => Humanise(state.ToString())
            };


    public static string DocumentOrigin(DocumentSource source) =>
        OperatorVocabulary.DocumentOrigin(source.ToString());

    public static string ImageIntakeLifecycleState(ImageInitiatedCaseState state) =>
        OperatorVocabulary.ImageIntakeLifecycleState(state.ToString());

    public static string ImageIntakeLifecycleStateContinuation(ImageInitiatedCaseState state) =>
        OperatorVocabulary.ImageIntakeLifecycleStateContinuation(state.ToString());

    public static string CustodyState(DocumentCustodyStatus status) =>
        OperatorVocabulary.CustodyState(status.ToString());

    public static string CustodyFolderState(CaseCustodyState state) =>
        OperatorVocabulary.CustodyFolderState(state.ToString());

    public static string UploadRequestState(RequestUploadStatus status) =>
        OperatorVocabulary.UploadRequestState(status.ToString());

    public static string IntakeFailure(string? failureCode) =>
        OperatorVocabulary.IntakeFailure(failureCode);

    public static string IntakeDecisionLabel(IntakeDecision decision) =>
        OperatorVocabulary.IntakeDecisionLabel(decision.ToString());

    public static string IntakeCannotBecomeCaseReason(IntakeDecision decision) =>
        OperatorVocabulary.IntakeCannotBecomeCaseReason(decision.ToString());

    public static string HistoryEvent(string? eventType) =>
        OperatorVocabulary.HistoryEvent(eventType);

    public static string OfficeTime(DateTimeOffset value) =>
        InOffice(value).ToString("dd MMM yyyy HH:mm", CultureInfo.InvariantCulture);

    public static string OfficeTime(DateTimeOffset? value, string absent) =>
        value is { } present ? OfficeTime(present) : absent;

    public static string OfficeDate(DateTimeOffset value) =>
        InOffice(value).ToString("dd MMM yyyy", CultureInfo.InvariantCulture);

    public static string OfficeClock(DateTimeOffset value) =>
        InOffice(value).ToString("HH:mm", CultureInfo.InvariantCulture);

    private static DateTimeOffset InOffice(DateTimeOffset value)
    {
        TimeZoneInfo office;
        try
        {
            office = TimeZoneInfo.FindSystemTimeZoneById("Europe/London");
        }
        catch (Exception exception) when (
            exception is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            office = TimeZoneInfo.Utc;
        }

        return TimeZoneInfo.ConvertTime(value, office);
    }

    public static string FileSize(long bytes)
    {
        var megabytes = bytes / 1024d / 1024d;
        return megabytes < 0.1d
            ? "under 0.1 MB"
            : string.Create(CultureInfo.InvariantCulture, $"{megabytes:0.0} MB");
    }

    public static string RouteScope(ApprovedMailboxRouteScope routeScope) =>
        OperatorVocabulary.RouteScope(routeScope.ToString());

    public static string ChaseReason(string? reason) => OperatorVocabulary.ChaseReason(reason);

    public static string InspectionMode(CaseInspectionMode value) =>
        OperatorVocabulary.InspectionMode(value.ToString());

    public static string Humanise(string? code) => OperatorVocabulary.Humanise(code);

    public static string AutomationActorLabel(string subjectId, string? configuredClientId) =>
        OperatorVocabulary.AutomationActorLabel(
            subjectId,
            configuredClientId,
            Pegasus.Web.Mcp.AutomationMcp.ClientDisplayName);

    public static string MileageEvidence(VehicleMileageEvidenceClass value) =>
        OperatorVocabulary.MileageEvidence(value.ToString());

    public static string MileageUnit(VehicleMileageUnit value) =>
        OperatorVocabulary.MileageUnit(value.ToString());

    public static string SourceChannel(IntakeSourceChannel channel) => channel switch
    {
        IntakeSourceChannel.ManualUpload => OperatorVocabulary.SourceChannel(channel.ToString()),
        IntakeSourceChannel.Mailbox => OperatorVocabulary.SourceChannel(channel.ToString()),
        IntakeSourceChannel.Automation => OperatorVocabulary.SourceChannel(channel.ToString()),
        _ => throw new InvalidOperationException(
            $"Unknown intake source channel value '{(int)channel}'.")
    };

    public static string SourceChannel(string? code) => OperatorVocabulary.SourceChannel(code);

    public static (string Word, string Icon) Provenance(CaseDataSource? source)
    {
        var isAiReader = source is not null
            && source.Kind == CaseDataSourceKind.IntakeEvidence
            && (source.Label.Contains("ai", StringComparison.OrdinalIgnoreCase)
                || source.PolicyKey.Contains("ai", StringComparison.OrdinalIgnoreCase));

        return OperatorVocabulary.Provenance(source?.Kind.ToString(), isAiReader);
    }

    public static string MailClassification(MailCategory category)
    {
        if (category.IsOther)
        {
            return OperatorVocabulary.MailClassification(
                true,
                category.OtherName,
                sent: false,
                family: null,
                category.Subtype);
        }

        string family;
        if (category.ReceivedFamily is { } received)
        {
            if (!Enum.IsDefined(received))
            {
                throw new ArgumentOutOfRangeException(nameof(category));
            }

            family = received.ToString();
        }
        else if (category.SentFamily is { } sent)
        {
            if (!Enum.IsDefined(sent))
            {
                throw new ArgumentOutOfRangeException(nameof(category));
            }

            family = sent.ToString();
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(category));
        }

        return OperatorVocabulary.MailClassification(
            false,
            otherName: null,
            category.Direction == MailDirection.Sent,
            family,
            category.Subtype);
    }

    public static string SuggestionOutcomeLabel(ImageVrmSuggestion suggestion) =>
        OperatorVocabulary.VrmRecognitionOutcomeLabel(
            suggestion.Outcome.ToString(),
            suggestion.SuggestedRegistration,
            $"{suggestion.Confidence:P0}");
}
