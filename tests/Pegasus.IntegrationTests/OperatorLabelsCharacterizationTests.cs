using Pegasus.Core.Assessment;
using Pegasus.Core.Cases;
using Pegasus.Core.Documents;
using Pegasus.Core.Identity;
using Pegasus.Core.ImageIntake;
using Pegasus.Core.Intake;
using Pegasus.Core.Intake.Unidentified;
using Pegasus.Core.Tasks;
using Pegasus.Core.Vehicle;
using Pegasus.Core.Workflow;
using Pegasus.Web.Pages.Intake;
using Pegasus.Web.Presentation;

namespace Pegasus.IntegrationTests;

/// <summary>
/// Exact-output characterization for the public label surface. The same
/// assertions run against the base worktree and the extracted vocabulary.
/// </summary>
public sealed class OperatorLabelsCharacterizationTests
{
    [Fact]
    public void EveryMappedEnumRetainsItsBaseOutput()
    {
        Assert.Equal(
            [
                "Unreadable or corrupt content", "Unsupported content",
                "No usable identification", "Conflicting identification",
                "Ambiguous ownership or destination", "Technical processing failure"
            ],
            Enum.GetValues<UnidentifiedReasonCode>().Select(OperatorLabels.UnidentifiedReason));
        Assert.Equal(
            ["Unidentified", "Resolved Unidentified"],
            Enum.GetValues<UnidentifiedState>().Select(OperatorLabels.UnidentifiedState));
        Assert.Equal(
            ["Image", "E-mail", "Document"],
            Enum.GetValues<UnidentifiedMediaKind>().Select(OperatorLabels.UnidentifiedMediaKind));
        Assert.Equal(
            [
                "Not ready", "Held", "Review", "Report preparation", "Post report",
                "Post-report complete", "Provider cancelled", "Collision Engineers rejected",
                "Created in error", "Cancelled — email unlinked"
            ],
            Enum.GetValues<CaseLifecycleState>().Select(OperatorLabels.CaseStage));
        Assert.Equal(
            ["Inspection", "Audit", "Inspection and audit"],
            Enum.GetValues<CaseType>().Select(OperatorLabels.CaseTypeName));
        Assert.Equal(
            ["Chase due", "Chasing paused", "Chasing stopped"],
            Enum.GetValues<CaseDueWorkState>().Select(OperatorLabels.ChaseState));
        Assert.Equal(
            ["Receiving work", "Queries", "Detailed classification", "Other", "Unidentified", "Triage"],
            Enum.GetValues<MailOperationalDestination>().Select(OperatorLabels.MailOperationalDestinationLabel));
        Assert.Equal(
            [
                "recorded before source tracking", "entered by hand", "imported from Glass's",
                "imported from Audatex", "from an approved AI proposal"
            ],
            Enum.GetValues<RepairSpecificationSourceRoute>().Select(OperatorLabels.RepairSpecificationRoute));
        Assert.Equal(
            [
                "Original source", "Instruction", "Image", "Correspondence", "Engineer report",
                "Audit report", "Other"
            ],
            Enum.GetValues<DocumentSemanticRole>().Select(OperatorLabels.DocumentRole));
        Assert.Equal(
            ["E-mail", "Staff upload", "Upload link", "Correspondence", "Generated", "Automatic"],
            Enum.GetValues<DocumentSource>().Select(OperatorLabels.DocumentOrigin));
        Assert.Equal(
            [
                "Awaiting definitive instruction", "Merged into Instruction-initiated Case", "Staff-closed"
            ],
            Enum.GetValues<ImageInitiatedCaseState>().Select(OperatorLabels.ImageIntakeLifecycleState));
        Assert.Equal(
            ["Storing", "Stored", "Storage failed"],
            Enum.GetValues<DocumentCustodyStatus>().Select(OperatorLabels.CustodyState));
        Assert.Equal(
            ["Box case folder: preparing", "Box case folder: unavailable", "Box case folder: unavailable"],
            Enum.GetValues<CaseCustodyState>().Select(OperatorLabels.CustodyFolderState));
        Assert.Equal(
            ["Being created", "Active", "Expired", "No uploads left", "Withdrawn", "Failed"],
            Enum.GetValues<RequestUploadStatus>().Select(OperatorLabels.UploadRequestState));
        Assert.Equal(
            [
                "New instructions and Triage mail (Inbox)",
                "Exact report and Triage evidence (Sent Items)"
            ],
            Enum.GetValues<ApprovedMailboxRouteScope>().Select(OperatorLabels.RouteScope));
        Assert.Equal(
            ["Physical address", "Image Based Assessment"],
            Enum.GetValues<CaseInspectionMode>().Select(OperatorLabels.InspectionMode));
        Assert.Equal(
            ["Supplied", "External", "Estimated"],
            Enum.GetValues<VehicleMileageEvidenceClass>().Select(OperatorLabels.MileageEvidence));
        Assert.Equal(
            ["miles", "km"],
            Enum.GetValues<VehicleMileageUnit>().Select(OperatorLabels.MileageUnit));
        Assert.Equal(
            ["Manual upload", "E-mail", "Automation"],
            Enum.GetValues<IntakeSourceChannel>().Select(OperatorLabels.SourceChannel));
    }

    [Theory]
    [InlineData("unreadable_docx", "The Word document could not be read")]
    [InlineData("unreadable_pdf", "The PDF could not be read")]
    [InlineData("image_decode_failure", "The image could not be read")]
    [InlineData("email_read_failure", "The e-mail could not be read")]
    [InlineData("source_read_failure", "The file could not be read")]
    [InlineData("source_reader_failure", "The file could not be read")]
    [InlineData("empty_message", "The message was empty")]
    [InlineData("message_too_large", "The message was too large to process")]
    [InlineData("docx_limit_exceeded", "The Word document is larger than the processing limit allows")]
    [InlineData("intake_limit_exceeded", "The file is larger or more deeply nested than the processing limit allows")]
    [InlineData("unsupported_file_type", "That file type is not supported")]
    [InlineData("deferred_file_type", "That file type is not supported yet")]
    [InlineData("unsupported_source", "That source is not supported")]
    [InlineData("artifact_retention_failure", "The original file could not be retained")]
    [InlineData("not_run_retention_failure", "The original file could not be retained")]
    [InlineData("artifact_read_failure", "The retained file could not be read back")]
    [InlineData("artifact_integrity_failure", "The retained file did not match what was received")]
    [InlineData("staged_artifact_integrity_failure", "The retained file did not match what was received")]
    [InlineData("integrity_failure", "The retained file did not match what was received")]
    [InlineData("persistence_failure", "The result could not be saved")]
    [InlineData("invalid_intake_data", "The file's contents were not valid")]
    [InlineData("source_identity_conflict", "The same receipt token was already used for a different file")]
    [InlineData("processing_lease_expired", "Processing timed out and was not completed")]
    [InlineData("queue_poisoned", "Processing was attempted repeatedly without completing")]
    [InlineData("intake_processing_failure", "Processing failed for a technical reason")]
    [InlineData("technical_failure", "Processing failed for a technical reason")]
    [InlineData("unexpected_intake_processing_failure", "Processing failed for a technical reason")]
    public void EveryIntakeFailureCodeRetainsItsBaseOutput(string code, string expected)
    {
        Assert.Equal(expected, OperatorLabels.IntakeFailure(code));
    }

    [Fact]
    public void HistoryEventsRetainTheirBaseOutput()
    {
        var expected = new Dictionary<string, string>
        {
            ["operator_note"] = "Note",
            ["case_accepted"] = "Case created",
            ["case_created_as_replacement"] = "Created as a replacement case",
            ["intake_case_association_seeded"] = "Linked to the e-mail that started it",
            ["intake_case_linked_automatic"] = "E-mail linked automatically",
            ["intake_receipt_recorded"] = "E-mail received",
            ["intake_receipt_reevaluated"] = "E-mail reprocessed",
            ["image_intake_registered"] = "Vehicle images registered",
            ["image_intake_registration_reasserted"] = "Vehicle images re-registered",
            ["merged_into_instruction_case"] = "Merged into Instruction-initiated Case",
            ["staff_closed"] = "Staff-closed",
            ["image_initiated_case_merged"] = "Image-initiated Case merged in",
            ["engineer_finding_recorded"] = "Engineer finding recorded",
            ["report_evidence_auto_linked"] = "Sent report linked automatically",
            ["standalone_audit_evidence_confirmed"] = "Audit evidence confirmed",
            ["audit_custody_confirmed"] = "Audit evidence stored",
            ["audit_custody_failed"] = "Audit evidence storage failed",
            ["custody_confirmed"] = "Document stored",
            ["custody_failed"] = "Document storage failed",
            ["provider_inspection_mode_applied"] = "Inspection mode taken from the principal",
            ["triage_response_linked"] = "Reply linked"
        };

        foreach (var pair in expected)
        {
            Assert.Equal(pair.Value, OperatorLabels.HistoryEvent(pair.Key));
        }
    }

    [Fact]
    public void RemainingPureMapsRetainTheirBaseOutput()
    {
        Assert.Equal("This item was blocked, with the reason recorded. It cannot become a case until it is corrected on the received item.",
            OperatorLabels.IntakeCannotBecomeCaseReason(IntakeDecision.BlockedIntake));
        Assert.Equal("This item was registered as vehicle images. Image material never becomes a case on its own.",
            OperatorLabels.IntakeCannotBecomeCaseReason(IntakeDecision.ImageIntakeRegistered));
        Assert.Equal("This file failed while it was being processed, so there is nothing to create a case from.",
            OperatorLabels.IntakeCannotBecomeCaseReason(IntakeDecision.CaseCreated));
        Assert.Equal("awaiting definitive instruction",
            OperatorLabels.ImageIntakeLifecycleStateContinuation(ImageInitiatedCaseState.AwaitingInstruction));
        Assert.Equal("Manual upload", OperatorLabels.SourceChannel("manual_upload"));
        Assert.Equal("E-mail", OperatorLabels.SourceChannel("mailbox"));
        Assert.Equal("Automation", OperatorLabels.SourceChannel("automation"));
        Assert.Equal("Unrecognized value", OperatorLabels.SourceChannel("unrecognized_value"));

        var expectedEstimateLabels = new Dictionary<string, string>
        {
            ["rnr"] = "Remove and refit",
            ["repair"] = "Repair",
            ["new_part"] = "New part",
            ["check_labour"] = "Check",
            ["paint_new"] = "Paint — new part",
            ["paint_repair"] = "Paint — repair",
            ["paint_blend"] = "Paint — blend",
            ["paint_prep"] = "Paint — preparation",
            ["specialist_fixed"] = "Specialist, fixed price",
            ["specialist_wu"] = "Specialist, by work units"
        };
        foreach (var pair in expectedEstimateLabels)
        {
            Assert.Equal(pair.Value, OperatorLabels.EstimateLineType(pair.Key));
        }

        Assert.Equal(("Unknown", "icon-info"), OperatorLabels.Provenance(null));
        Assert.Equal(("Staff", "icon-user"), OperatorLabels.Provenance(new(
            CaseDataSourceKind.StaffCorrection, "id", "Staff", "staff", 1)));
        Assert.Equal(("AI", "icon-filter"), OperatorLabels.Provenance(new(
            CaseDataSourceKind.IntakeEvidence, "id", "AI reader", "reader", 1)));
        Assert.Equal(("Extracted", "icon-file-text"), OperatorLabels.Provenance(new(
            CaseDataSourceKind.IntakeEvidence, "id", "Reader", "reader", 1)));
        Assert.Equal(("E-mail", "icon-arrow-right"), OperatorLabels.Provenance(new(
            CaseDataSourceKind.MailRoute, "id", "Mail", "mail", 1)));
        Assert.Equal(("Lookup", "icon-search"), OperatorLabels.Provenance(new(
            CaseDataSourceKind.VehicleLookup, "id", "Lookup", "lookup", 1)));
        Assert.Equal(("Principal", "icon-shield"), OperatorLabels.Provenance(new(
            CaseDataSourceKind.ProviderSetting, "id", "Principal", "provider", 1)));
        Assert.Equal(("Automatic", "icon-refresh-cw"), OperatorLabels.Provenance(new(
            CaseDataSourceKind.CaseAcceptance, "id", "Acceptance", "acceptance", 1)));
    }

    [Fact]
    public void DynamicAndFallbackLabelsRetainTheirBaseOutput()
    {
        Assert.Equal("Searchable content", OperatorLabels.AttachmentSearchability(true));
        Assert.Equal("Content unavailable for search", OperatorLabels.AttachmentSearchability(false));
        Assert.Equal("Subject — from sender", OperatorLabels.EmailHandle("Subject", "sender"));
        Assert.Equal("Subject", OperatorLabels.EmailHandle("Subject", null));
        Assert.Equal("(No subject) — from sender", OperatorLabels.EmailHandle(null, "sender"));
        Assert.Equal("(No subject)", OperatorLabels.EmailHandle(null, null));
        Assert.Equal("This was added to case C-1.", OperatorLabels.AssociatedWithCase("C-1", true));
        Assert.Equal("This was automatically associated with a case.", OperatorLabels.AssociatedWithCase(null, false));
        Assert.Equal("Chase due", OperatorLabels.ImageChaseState(true));
        Assert.Equal("Not yet due", OperatorLabels.ImageChaseState(false));
        Assert.Equal("Remove and refit", OperatorLabels.EstimateLineType("rnr"));
        Assert.Equal("Unlisted", OperatorLabels.EstimateLineType("Unlisted"));
        Assert.Equal("Needs text extraction", OperatorLabels.IntakeDecisionLabel(IntakeDecision.OcrRequired));
        Assert.Equal("Failed", OperatorLabels.IntakeDecisionLabel(IntakeDecision.TechnicalFailure));
        Assert.Equal("Inspection and Audit", DetailsModel.CaseTypeLabel(CaseType.InspectionAndAudit));
        Assert.Equal("Not available", DetailsModel.CaseTypeLabel(null));
        Assert.Equal("Source email unlinked", OperatorLabels.CaseStage("source_email_unlinked"));
        Assert.Equal("Post-report complete", OperatorLabels.CaseStage("PostReportComplete"));
        Assert.Equal("Unknown", OperatorLabels.Humanise(null));
        Assert.Equal("Case returned to review", OperatorLabels.Humanise("case_returned_to_review"));
        Assert.Equal("Details are incomplete", OperatorLabels.ChaseReason("Accepted intake is incomplete"));
        Assert.Equal("already honest", OperatorLabels.ChaseReason("already honest"));
        Assert.Equal("The Word document could not be read", OperatorLabels.IntakeFailure("unreadable_docx"));
        Assert.Equal("Processing failed", OperatorLabels.IntakeFailure(null));
        Assert.Equal("Unexpected code", OperatorLabels.HistoryEvent("unexpected_code"));
        Assert.Equal("under 0.1 MB", OperatorLabels.FileSize(1));
        Assert.Equal("1.0 MB", OperatorLabels.FileSize(1024 * 1024));
        Assert.Equal("31 Dec 2031 23:00", OperatorLabels.OfficeTime(new DateTimeOffset(2031, 12, 31, 23, 0, 0, TimeSpan.Zero)));
    }

    [Fact]
    public void StrictAndNullBoundariesRemainStrict()
    {
        Assert.Throws<InvalidOperationException>(() => OperatorLabels.SourceChannel((IntakeSourceChannel)999));
        Assert.Throws<InvalidOperationException>(() =>
            Pegasus.Contracts.Vocabulary.OperatorVocabulary.IntakeDecisionLabel("unknown"));
    }
}
