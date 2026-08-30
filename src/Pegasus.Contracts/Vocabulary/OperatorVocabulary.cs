using System.Text;

namespace Pegasus.Contracts.Vocabulary;

/// <summary>
/// The shared operator vocabulary for persisted codes and contract-shaped
/// values. Core types are deliberately not referenced here; host adapters pass
/// their stable names into this one map.
/// </summary>
/// <remarks>
/// Raw enum names, snake_case event codes and PascalCase compounds never reach
/// markup. Case lifecycle labels and the reserved meanings of Audit, Triage,
/// Unidentified and Blocked are settled vocabulary. Unknown readable codes
/// fall through to <see cref="Humanise"/> rather than being printed raw.
/// </remarks>
public static class OperatorVocabulary
{
    public static string AttachmentSearchability(bool isSearchable) =>
        isSearchable ? "Searchable content" : "Content unavailable for search";

    public static string UnidentifiedReason(string? reason) => Normalize(reason) switch
    {
        "unreadableorcorruptcontent" => "Unreadable or corrupt content",
        "unsupportedcontent" => "Unsupported content",
        "nousableidentification" => "No usable identification",
        "conflictingidentification" => "Conflicting identification",
        "ambiguousownershipordestination" => "Ambiguous ownership or destination",
        "technicalprocessingfailure" => "Technical processing failure",
        _ => Humanise(reason)
    };

    public static string UnidentifiedState(string? state) => Normalize(state) switch
    {
        "open" => "Unidentified",
        "resolved" => "Resolved Unidentified",
        _ => Humanise(state)
    };

    public static string UnidentifiedMediaKind(string? kind) => Normalize(kind) switch
    {
        "image" => "Image",
        "email" => "E-mail",
        "document" => "Document",
        _ => Humanise(kind)
    };

    public static string EmailHandle(string? subject, string? sender) => (subject, sender) switch
    {
        ({ } presentSubject, { } presentSender) => $"{presentSubject} — from {presentSender}",
        ({ } presentSubject, null) => presentSubject,
        (null, { } presentSender) => $"(No subject) — from {presentSender}",
        _ => "(No subject)"
    };

    public static string AssociatedWithCase(string? caseReference, bool byStaffDecision) =>
        (byStaffDecision, caseReference) switch
        {
            (true, null) => "This was added to a case.",
            (true, { } staffLinked) => $"This was added to case {staffLinked}.",
            (false, null) => "This was automatically associated with a case.",
            (false, { } matched) => $"This was automatically associated with case {matched}."
        };

    public static string CaseStage(string? state)
    {
        if (state is null || !state.All(char.IsLetterOrDigit))
        {
            return Humanise(state);
        }

        return Normalize(state) switch
        {
            "notready" => "Not ready",
            "held" => "Held",
            "review" => "Review",
            "reportpreparation" => "Report preparation",
            "postreport" => "Post report",
            "postreportcomplete" => "Post-report complete",
            "providercancelled" => "Provider cancelled",
            "collisionengineersrejected" => "Collision Engineers rejected",
            "createdinerror" => "Created in error",
            "sourceemailunlinked" => "Cancelled — email unlinked",
            _ => Humanise(state)
        };
    }

    public static string CaseTypeName(string? type) => Normalize(type) switch
    {
        "inspection" => "Inspection",
        "audit" => "Audit",
        "inspectionandaudit" => "Inspection and audit",
        _ => Humanise(type)
    };

    public static string AttemptedCaseTypeName(string? type) => Normalize(type) switch
    {
        "inspection" => "Inspection",
        "audit" => "Audit",
        "inspectionandaudit" => "Inspection and Audit",
        _ => "Not available"
    };

    public static string ChaseState(string? state) => Normalize(state) switch
    {
        "scheduled" => "Chase due",
        "held" => "Chasing paused",
        "stopped" => "Chasing stopped",
        _ => Humanise(state)
    };

    public static string ImageChaseState(bool chaseDue) => chaseDue ? "Chase due" : "Not yet due";

    public static string MailOperationalDestinationLabel(string? destination) => Normalize(destination) switch
    {
        "receivingwork" => "Receiving work",
        "queries" => "Queries",
        "detailedclassification" => "Detailed classification",
        "other" => "Other",
        "triage" => "Triage",
        "unidentified" => "Unidentified",
        _ => Humanise(destination)
    };

    public static string RepairSpecificationRoute(string? route) => Normalize(route) switch
    {
        "manual" => "entered by hand",
        "glasses" => "imported from Glass's",
        "audatexpdf" => "imported from Audatex",
        "approvedaiproposal" => "from an approved AI proposal",
        _ => "recorded before source tracking"
    };

    public static string EstimateLineType(string type) => type switch
    {
        "rnr" => "Remove and refit",
        "repair" => "Repair",
        "new_part" => "New part",
        "check_labour" => "Check",
        "paint_new" => "Paint — new part",
        "paint_repair" => "Paint — repair",
        "paint_blend" => "Paint — blend",
        "paint_prep" => "Paint — preparation",
        "specialist_fixed" => "Specialist, fixed price",
        "specialist_wu" => "Specialist, by work units",
        _ => type
    };

    public static string DocumentRole(string? role) => Normalize(role) switch
    {
        "originalsource" => "Original source",
        "instruction" => "Instruction",
        "image" => "Image",
        "correspondence" => "Correspondence",
        "engineerreport" => "Engineer report",
        "auditreport" => "Audit report",
        "feenote" => "Fee note",
        "other" => "Other",
        _ => Humanise(role)
    };

    public static string DocumentOrigin(string? source) => Normalize(source) switch
    {
        "intake" => "E-mail",
        "staffupload" => "Staff upload",
        "requestupload" => "Upload link",
        "externalcorrespondence" => "Correspondence",
        "generated" => "Generated",
        "automation" => "Automatic",
        _ => Humanise(source)
    };

    public static string ImageIntakeLifecycleState(string? state) => Normalize(state) switch
    {
        "awaitinginstruction" => "Awaiting definitive instruction",
        "mergedintoinstructioncase" => "Merged into Instruction-initiated Case",
        "staffclosed" => "Staff-closed",
        _ => Humanise(state)
    };

    public static string ImageIntakeLifecycleStateContinuation(string? state)
    {
        var label = ImageIntakeLifecycleState(state);
        return string.Concat(char.ToLowerInvariant(label[0]).ToString(), label.AsSpan(1));
    }

    public static string CustodyState(string? status) => Normalize(status) switch
    {
        "pending" => "Storing",
        "confirmed" => "Stored",
        "failed" => "Storage failed",
        _ => Humanise(status)
    };

    public static string CustodyFolderState(string? state) =>
        Normalize(state) == "pending" ? "Box case folder: preparing" : "Box case folder: unavailable";

    public static string UploadRequestState(string? status) => Normalize(status) switch
    {
        "pending" => "Being created",
        "active" => "Active",
        "expired" => "Expired",
        "exhausted" => "No uploads left",
        "revoked" => "Withdrawn",
        "failed" => "Failed",
        _ => Humanise(status)
    };

    public static string IntakeFailure(string? failureCode) => failureCode switch
    {
        "unreadable_docx" => "The Word document could not be read",
        "unreadable_pdf" => "The PDF could not be read",
        "image_decode_failure" => "The image could not be read",
        "email_read_failure" => "The e-mail could not be read",
        "source_read_failure" or "source_reader_failure" => "The file could not be read",
        "empty_message" => "The message was empty",
        "message_too_large" => "The message was too large to process",
        "docx_limit_exceeded" => "The Word document is larger than the processing limit allows",
        "intake_limit_exceeded" => "The file is larger or more deeply nested than the processing limit allows",
        "unsupported_file_type" => "That file type is not supported",
        "deferred_file_type" => "That file type is not supported yet",
        "unsupported_source" => "That source is not supported",
        "artifact_retention_failure" or "not_run_retention_failure" => "The original file could not be retained",
        "artifact_read_failure" => "The retained file could not be read back",
        "artifact_integrity_failure" or "staged_artifact_integrity_failure" or "integrity_failure" =>
            "The retained file did not match what was received",
        "persistence_failure" => "The result could not be saved",
        "invalid_intake_data" => "The file's contents were not valid",
        "source_identity_conflict" => "The same receipt token was already used for a different file",
        "processing_lease_expired" => "Processing timed out and was not completed",
        "queue_poisoned" => "Processing was attempted repeatedly without completing",
        "intake_processing_failure" or "technical_failure" or "unexpected_intake_processing_failure" =>
            "Processing failed for a technical reason",
        null or "" => "Processing failed",
        _ => Humanise(failureCode)
    };

    public static string IntakeDecisionLabel(string? decision) => Normalize(decision) switch
    {
        "casecreated" => "Ready for case allocation",
        "needssorting" => "Unidentified",
        "blockedintake" => "Blocked",
        "unsupported" => "Unsupported",
        "ocrrequired" => "Needs text extraction",
        "technicalfailure" => "Failed",
        "imageintakeregistered" => "Vehicle images registered",
        _ => throw new InvalidOperationException($"Unknown intake decision value '{decision}'.")
    };

    public static string IntakeCannotBecomeCaseReason(string? decision) => Normalize(decision) switch
    {
        "blockedintake" => "This item was blocked, with the reason recorded. It cannot become a case until it is corrected on the received item.",
        "imageintakeregistered" => "This item was registered as vehicle images. Image material never becomes a case on its own.",
        "unsupported" => "This file could not be read, so there is nothing to create a case from.",
        _ => "This file failed while it was being processed, so there is nothing to create a case from."
    };

    public static string HistoryEvent(string? eventType) => eventType switch
    {
        "operator_note" => "Note",
        "case_accepted" => "Case created",
        "case_created_as_replacement" => "Created as a replacement case",
        "intake_case_association_seeded" => "Linked to the e-mail that started it",
        "intake_case_linked_automatic" => "E-mail linked automatically",
        "intake_receipt_recorded" => "E-mail received",
        "intake_receipt_reevaluated" => "E-mail reprocessed",
        "image_intake_registered" => "Vehicle images registered",
        "image_intake_registration_reasserted" => "Vehicle images re-registered",
        "merged_into_instruction_case" => "Merged into Instruction-initiated Case",
        "staff_closed" => "Staff-closed",
        "image_initiated_case_merged" => "Image-initiated Case merged in",
        "engineer_finding_recorded" => "Engineer finding recorded",
        "report_evidence_auto_linked" => "Sent report linked automatically",
        "standalone_audit_evidence_confirmed" => "Audit evidence confirmed",
        "audit_custody_confirmed" => "Audit evidence stored",
        "audit_custody_failed" => "Audit evidence storage failed",
        "custody_confirmed" => "Document stored",
        "custody_failed" => "Document storage failed",
        "provider_inspection_mode_applied" => "Inspection mode taken from the principal",
        "triage_response_linked" => "Reply linked",
        _ => Humanise(eventType)
    };

    public static string RouteScope(string? routeScope) => Normalize(routeScope) switch
    {
        "inboundintake" => "New instructions and Triage mail (Inbox)",
        "sentevidence" => "Exact report and Triage evidence (Sent Items)",
        _ => Humanise(routeScope)
    };

    public static string ChaseReason(string? reason) =>
        reason == "Accepted intake is incomplete" ? "Details are incomplete" : reason ?? string.Empty;

    public static string InspectionMode(string? value) => Normalize(value) switch
    {
        "physicaladdress" => "Physical address",
        "imagebasedassessment" => "Image Based Assessment",
        _ => Humanise(value)
    };

    public static string AutomationActorLabel(string subjectId, string? configuredClientId, string clientDisplayName) =>
        configuredClientId is { Length: > 0 } && string.Equals(subjectId, configuredClientId, StringComparison.Ordinal)
            ? clientDisplayName
            : Guid.TryParse(subjectId, out _)
                ? "Unknown automation client"
                : subjectId;

    public static string MileageEvidence(string? value) => Normalize(value) switch
    {
        "supplied" => "Supplied",
        "external" => "External",
        "estimated" => "Estimated",
        _ => Humanise(value)
    };

    public static string MileageUnit(string? value) => Normalize(value) switch
    {
        "miles" => "miles",
        "kilometres" => "km",
        _ => Humanise(value)
    };

    public static string SourceChannel(string? code) => Normalize(code) switch
    {
        "manualupload" => "Manual upload",
        "mailbox" => "E-mail",
        "automation" => "Automation",
        _ => Humanise(code)
    };

    public static string VrmRecognitionOutcomeLabel(
        string? outcome,
        string? suggestedRegistration,
        string formattedConfidence) => Normalize(outcome) switch
    {
        "suggested" => $"Suggested {suggestedRegistration} ({formattedConfidence} confidence)",
        "noreadableresult" => "No readable registration",
        "technicalfailure" => "Technical failure",
        "unavailable" => "Recognition unavailable",
        _ => throw new InvalidOperationException($"Unknown recognition outcome value '{outcome}'.")
    };

    public static (string Word, string Icon) Provenance(string? kind, bool isAiReader) => Normalize(kind) switch
    {
        null => ("Unknown", "icon-info"),
        "staffcorrection" => ("Staff", "icon-user"),
        "intakeevidence" when isAiReader => ("AI", "icon-filter"),
        "intakeevidence" => ("Extracted", "icon-file-text"),
        "mailroute" => ("E-mail", "icon-arrow-right"),
        "vehiclelookup" => ("Lookup", "icon-search"),
        "providersetting" => ("Principal", "icon-shield"),
        "caseacceptance" => ("Automatic", "icon-refresh-cw"),
        _ => ("Unknown", "icon-info")
    };

    public static string MailClassification(
        bool isOther,
        string? otherName,
        bool sent,
        string? family,
        string? subtype)
    {
        if (isOther)
        {
            return otherName!;
        }

        var normalizedFamily = Normalize(family);
        var familyLabel = sent
            ? normalizedFamily switch
            {
                "reportsent" => "Report sent",
                "caserejected" => "Case rejected",
                "querysent" => "Query sent",
                "additionalimagerequest" => "Additional image request",
                _ => throw new ArgumentOutOfRangeException(nameof(family))
            }
            : normalizedFamily switch
            {
                "general" => "General",
                "billing" => "Billing",
                "newinstructionreceived" => "New instruction",
                "nonclientrelated" => "Not client related",
                "inprogresscases" => "In-progress case",
                "postreportemails" => "Post-report",
                "preinstructionemails" => "Pre-instruction",
                "internalcc" => "Internal CC",
                _ => throw new ArgumentOutOfRangeException(nameof(family))
            };
        var prefixed = sent ? $"Sent · {familyLabel}" : familyLabel;
        return subtype is { } presentSubtype
            ? $"{prefixed} · {HumanizeSlug(presentSubtype)}"
            : prefixed;
    }

    public static string Humanise(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return "Unknown";
        }

        var spaced = new StringBuilder(code.Length + 8);
        for (var index = 0; index < code.Length; index++)
        {
            var character = code[index];
            if (character is '_' or '-' or '.')
            {
                spaced.Append(' ');
                continue;
            }

            if (char.IsUpper(character) && index > 0 && !char.IsUpper(code[index - 1]))
            {
                spaced.Append(' ');
            }

            spaced.Append(character);
        }

        var words = spaced.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
        {
            return "Unknown";
        }

        var sentence = string.Join(' ', words).ToLowerInvariant();
        return char.ToUpperInvariant(sentence[0]) + sentence[1..];
    }

    private static string Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : new string(value.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();

    private static string HumanizeSlug(string slug)
    {
        var words = slug.Replace('-', ' ').Replace('_', ' ');
        return words.Length == 0 ? words : char.ToUpperInvariant(words[0]) + words[1..];
    }
}
