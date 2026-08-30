using System.Globalization;
using Pegasus.Contracts.Vocabulary;

namespace Pegasus.Desktop.Presentation;

/// <summary>
/// The desktop presentation boundary for operator-readable values.
/// Business vocabulary is delegated to the one shared contract map; this class
/// only adapts contract strings and formats office dates, times and sizes.
/// </summary>
public static class OperatorText
{
    public static string AttachmentSearchability(bool isSearchable) =>
        OperatorVocabulary.AttachmentSearchability(isSearchable);

    public static string UnidentifiedReason(string? reason) =>
        OperatorVocabulary.UnidentifiedReason(reason);

    public static string UnidentifiedState(string? state) =>
        OperatorVocabulary.UnidentifiedState(state);

    public static string UnidentifiedMediaKind(string? kind) =>
        OperatorVocabulary.UnidentifiedMediaKind(kind);

    public static string AssociatedWithCase(string? caseReference, bool byStaffDecision) =>
        OperatorVocabulary.AssociatedWithCase(caseReference, byStaffDecision);

    public static string CaseStage(string? state) => OperatorVocabulary.CaseStage(state);

    public static string CaseTypeName(string? type) => OperatorVocabulary.CaseTypeName(type);

    public static string DocumentRole(string? role) => OperatorVocabulary.DocumentRole(role);

    public static string DocumentOrigin(string? source) => OperatorVocabulary.DocumentOrigin(source);

    public static string ImageIntakeLifecycleState(string? state) =>
        OperatorVocabulary.ImageIntakeLifecycleState(state);

    public static string CustodyState(string? status) => OperatorVocabulary.CustodyState(status);

    public static string UploadRequestState(string? status) =>
        OperatorVocabulary.UploadRequestState(status);

    public static string IntakeFailure(string? failureCode) =>
        OperatorVocabulary.IntakeFailure(failureCode);

    public static string IntakeDecisionLabel(string? decision) =>
        OperatorVocabulary.IntakeDecisionLabel(decision);

    public static string HistoryEvent(string? eventType) =>
        OperatorVocabulary.HistoryEvent(eventType);

    public static string MailOperationalDestinationLabel(string? destination) =>
        OperatorVocabulary.MailOperationalDestinationLabel(destination);

    public static string RouteScope(string? routeScope) => OperatorVocabulary.RouteScope(routeScope);

    public static string InspectionMode(string? value) => OperatorVocabulary.InspectionMode(value);

    public static string MileageEvidence(string? value) => OperatorVocabulary.MileageEvidence(value);

    public static string MileageUnit(string? value) => OperatorVocabulary.MileageUnit(value);

    public static string SourceChannel(string? code) => OperatorVocabulary.SourceChannel(code);

    public static string Humanise(string? code) => OperatorVocabulary.Humanise(code);

    public static (string Word, string Icon) Provenance(string? kind, bool isAiReader) =>
        OperatorVocabulary.Provenance(kind, isAiReader);

    public static string VrmRecognitionOutcomeLabel(
        string? outcome,
        string? suggestedRegistration,
        string formattedConfidence) =>
        OperatorVocabulary.VrmRecognitionOutcomeLabel(
            outcome,
            suggestedRegistration,
            formattedConfidence);

    /// <summary>Formats a persisted instant in the office time zone.</summary>
    public static string OfficeTime(DateTimeOffset value) =>
        ConvertToOfficeTime(value).ToString("dd MMM yyyy HH:mm", CultureInfo.InvariantCulture);

    /// <summary>Formats a persisted instant's office date.</summary>
    public static string OfficeDate(DateTimeOffset value) =>
        ConvertToOfficeTime(value).ToString("dd MMM yyyy", CultureInfo.InvariantCulture);

    /// <summary>Formats a persisted instant's office clock time.</summary>
    public static string OfficeClock(DateTimeOffset value) =>
        ConvertToOfficeTime(value).ToString("HH:mm", CultureInfo.InvariantCulture);

    /// <summary>
    /// Returns the zone name used by the formatting methods. If the host cannot
    /// resolve the IANA office zone, the fallback is explicitly labelled UTC.
    /// </summary>
    public static string OfficeTimeZoneLabel => ResolveOfficeZone().IsLondon ? "London" : "UTC";

    /// <summary>Formats an operator-visible count without exposing a raw number object to XAML.</summary>
    public static string Count(int value) => value.ToString("N0", CultureInfo.InvariantCulture);

    /// <summary>Formats an operator-visible content size in megabytes, never bytes.</summary>
    public static string FileSize(long bytes)
    {
        var megabytes = bytes / 1024d / 1024d;
        return megabytes < 0.1d
            ? "under 0.1 MB"
            : string.Create(CultureInfo.InvariantCulture, $"{megabytes:0.0} MB");
    }

    private static DateTimeOffset ConvertToOfficeTime(DateTimeOffset value) =>
        TimeZoneInfo.ConvertTime(value, ResolveOfficeZone().Zone);

    private static (TimeZoneInfo Zone, bool IsLondon) ResolveOfficeZone()
    {
        try
        {
            return (TimeZoneInfo.FindSystemTimeZoneById("Europe/London"), true);
        }
        catch (TimeZoneNotFoundException)
        {
            return (TimeZoneInfo.Utc, false);
        }
        catch (InvalidTimeZoneException)
        {
            return (TimeZoneInfo.Utc, false);
        }
    }
}
