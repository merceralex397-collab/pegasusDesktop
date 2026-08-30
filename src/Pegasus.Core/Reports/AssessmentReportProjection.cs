using System.Globalization;
using Pegasus.Core.Assessment;
using Pegasus.Core.Identity;

namespace Pegasus.Core.Reports;

/// <summary>
/// The case-level facts <see cref="AssessmentReportSnapshot"/> needs beyond
/// the assessment record itself: the accepted case's own identity and
/// addressee, plus the custody-confirmed evidence a report draws on. Every
/// field here is loaded from an existing accepted source (<see
/// cref="Assessment.CaseAssessmentProjection"/>, the case-detail projection,
/// and confirmed case-document custody) — nothing is synthesized.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Photos"/> are confirmed <c>Image</c>-role case documents,
/// following the same custody query the EVA hand-off bundle already uses
/// (<see cref="Pegasus.Core.Eva.EvaBundleImage"/>): current, not logically
/// removed, custody-confirmed. UI-15's photograph curation (which photo, what
/// order) is explicitly deferred — see the "Report images" section of
/// <c>src/Pegasus.Web/Pages/Cases/Assessment/Index.cshtml</c> — so every
/// confirmed image on the case is offered in custody (occurrence) order
/// rather than an operator-curated subset.
/// </para>
/// <para>
/// <see cref="Sources"/> are every other confirmed case document (any
/// semantic role), reported by their own custody name, version and hash —
/// the same provenance triple the EVA bundle's accepted-source manifest
/// already carries. This is the closest real analogue to "accepted source
/// evidence" the domain has today.
/// </para>
/// <para>
/// <see cref="Costs"/> is supplied by the selected accepted repair estimate.
/// This projection never derives an internal rate-card value. The source
/// reference is carried separately so a report snapshot can retain both the
/// imported numbers and the evidence that supplied them.
/// </para>
/// </remarks>
public sealed record AssessmentReportProjectionInput(
    CaseAssessmentProjection Assessment,
    string? ClaimantName,
    string OurReference,
    string? YourReference,
    IReadOnlyList<string> ReportFor,
    DateOnly ReportDate,
    IReadOnlyList<ReportImageEvidence> Photos,
    IReadOnlyList<AcceptedReportSource> Sources,
    ReportRepairCosts? Costs,
    AcceptedReportSource? RepairCostSource = null,
    Guid? RepairSpecificationId = null,
    int? RepairSpecificationVersion = null);

/// <summary>
/// Either a snapshot ready to render, or the enumerated reasons it is not —
/// never both, and never a snapshot the caller has to re-validate.
/// </summary>
public sealed record AssessmentReportProjectionResult(
    AssessmentReportSnapshot? Snapshot,
    IReadOnlyList<AssessmentReadinessItem> Reasons)
{
    public bool IsReady => Snapshot is not null;
}

/// <summary>
/// Builds an <see cref="AssessmentReportSnapshot"/> from an accepted
/// assessment plus its case-report inputs, or names exactly what is
/// outstanding. Reuses <see cref="AssessmentPolicy.EvaluateReadiness"/> as
/// the single readiness rail for everything the assessment screen already
/// tracks, and adds only the report-specific requirements
/// <see cref="AssessmentReportSnapshot.Validate"/> layers on top (case
/// identity, addressee, photographs, source evidence, an accepted engineer
/// signature, and repair costs) — one readiness vocabulary
/// (<see cref="AssessmentReadinessItem"/>), not two.
/// </summary>
public static class AssessmentReportProjection
{
    public const string RepairCostRequirement = "Repair cost figures";

    public static AssessmentReportProjectionResult Project(AssessmentReportProjectionInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        var assessment = input.Assessment;
        var reasons = new List<AssessmentReadinessItem>(AssessmentPolicy.EvaluateReadiness(assessment));

        void Require(bool ok, string requirement, string source, string whyOutstanding, string howToResolve)
        {
            if (!ok)
            {
                reasons.Add(new(requirement, source, whyOutstanding, howToResolve));
            }
        }

        Require(
            !string.IsNullOrWhiteSpace(input.ClaimantName),
            "Claimant name", "Case record",
            "No confirmed claimant is recorded.",
            "Confirm it on the case details.");
        Require(
            !string.IsNullOrWhiteSpace(input.YourReference),
            "Your reference", "Case record",
            "No confirmed claim number is recorded.",
            "Confirm it on the case details.");
        Require(
            input.ReportFor.Count > 0,
            "Report addressee", "Case record",
            "No confirmed instructing principal is recorded.",
            "Confirm the principal on the case details.");
        Require(
            assessment.CaseOwned.IncidentDate is not null,
            "Incident date", "Case record",
            "No confirmed incident date is recorded.",
            "Confirm it on the case details.");
        Require(
            input.Photos.Count > 0,
            "Report photographs", "Case documents",
            "No custody-confirmed photograph is attached to the case.",
            "Attach and confirm at least one image document on the case.");
        Require(
            input.Sources.Count > 0,
            "Accepted source evidence", "Case documents",
            "No custody-confirmed source document is attached to the case.",
            "Attach and confirm at least one document on the case.");
        Require(
            input.Sources.All(IsValidSource),
            "Accepted source evidence", "Case documents",
            "At least one accepted source has incomplete or invalid provenance.",
            "Retain a source name, version, and SHA-256 hash for every accepted source.");
        Require(
            input.Photos.All(IsValidPhoto),
            "Report photographs", "Case documents",
            "At least one report photograph has incomplete or invalid custody evidence.",
            "Use custody-confirmed image bytes with their matching SHA-256 hash.");

        var assessmentMethod = MapAssessmentMethod(assessment.CaseOwned.InspectionMode);
        Require(
            assessmentMethod is not null,
            "Assessment method", "Case record",
            "The case has no recognized inspection method recorded.",
            "Confirm the inspection method on the case details.");

        var engineerSignature = Field(assessment, AssessmentVocabulary.EngineerSignature);
        var engineerName = Field(assessment, AssessmentVocabulary.EngineerName);
        var engineerQualifications = Field(assessment, AssessmentVocabulary.EngineerQualifications);
        if (engineerSignature is not null)
        {
            var accepted = AssessmentReportSnapshot.TryResolveAcceptedEngineer(
                    engineerSignature, out var acceptedName, out var acceptedQualifications)
                && string.Equals(acceptedName, engineerName, StringComparison.Ordinal)
                && string.Equals(acceptedQualifications, engineerQualifications, StringComparison.Ordinal);
            Require(
                accepted,
                "Accepted engineer signature", "Assessment record",
                "The recorded engineer name, qualifications and signature do not match an accepted signatory.",
                "Record the exact accepted engineer name, qualifications and signature.");
        }

        Require(
            input.Costs is not null && input.RepairCostSource is not null,
            RepairCostRequirement, "Selected accepted repair estimate",
            "The selected repair estimate is missing, unaccepted, or has ambiguous source provenance.",
            "Select one accepted estimate with its external source and version evidence.");
        Require(
            input.RepairCostSource is not null && IsValidSource(input.RepairCostSource),
            "Repair cost source", "Selected accepted repair estimate",
            "The selected repair estimate does not carry valid source/version/hash evidence.",
            "Select an accepted estimate whose source evidence includes a SHA-256 hash.");
        Require(
            input.Costs is null || IsValidCosts(input.Costs),
            RepairCostRequirement, "Selected accepted repair estimate",
            "The selected repair estimate contains incomplete or invalid accepted amounts.",
            "Select one accepted estimate with a validated calculation basis.");
        Require(
            input.RepairSpecificationId is not null && input.RepairSpecificationVersion is > 0,
            "Selected repair estimate", "Selected accepted repair estimate",
            "The selected estimate identity or accepted version is missing.",
            "Select an accepted repair-estimate version explicitly.");

        if (reasons.Count > 0)
        {
            return new(null, reasons);
        }

        var sources = input.RepairCostSource is null
            ? input.Sources
            : input.Sources.Append(input.RepairCostSource).ToArray();

        var snapshot = new AssessmentReportSnapshot(
            OurReference: input.OurReference,
            YourReference: input.YourReference!,
            ReportDate: input.ReportDate,
            ClaimantName: input.ClaimantName!,
            IncidentDate: assessment.CaseOwned.IncidentDate!.Value,
            InstructionsReceived: assessment.CaseOwned.InstructionDate ?? default,
            Assessed: ParseDate(Field(assessment, AssessmentVocabulary.IncidentAssessed)) ?? default,
            ReportFor: input.ReportFor,
            Vehicle: BuildVehicle(assessment),
            Outcome: MapOutcome(Field(assessment, AssessmentVocabulary.Outcome)!),
            LegalStatus: Field(assessment, AssessmentVocabulary.LegalStatus)!,
            UnroadworthyReason: Field(assessment, AssessmentVocabulary.UnroadworthyReason),
            ImpactSeverity: Field(assessment, AssessmentVocabulary.ImpactSeverity)!,
            ImpactLocation: Field(assessment, AssessmentVocabulary.ImpactLocation)!,
            AssessmentMethod: assessmentMethod!,
            LocationAddress: assessment.CaseOwned.InspectionAddress,
            EngineerValue: ParseMoney(Field(assessment, AssessmentVocabulary.ValueEngineer)) ?? 0m,
            RetailValue: ParseMoney(Field(assessment, AssessmentVocabulary.ValueRetail)) ?? 0m,
            TradeValue: ParseMoney(Field(assessment, AssessmentVocabulary.ValueTrade)) ?? 0m,
            SalvageCategory: Field(assessment, AssessmentVocabulary.SalvageCategory),
            SalvageValue: ParseMoney(Field(assessment, AssessmentVocabulary.SalvageValue)),
            Costs: input.Costs!,
            NewParts: LinesOfType(assessment, "new_part"),
            Repairs: LinesOfType(assessment, "repair"),
            Operations: LinesOfType(
                assessment,
                "check_labour", "paint_new", "paint_repair", "paint_blend", "paint_prep",
                "specialist_fixed", "specialist_wu"),
            HistoryCheck: Field(assessment, AssessmentVocabulary.HistoryCheck)!,
            EngineerComments: Field(assessment, AssessmentVocabulary.EngineersComments),
            Engineer: new ReportEngineer(engineerName!, engineerQualifications!, engineerSignature!),
            AgreedFee: ParseMoney(Field(assessment, AssessmentVocabulary.AgreedFee)) ?? 0m,
            FeeDescriptionLines: SplitLines(Field(assessment, AssessmentVocabulary.FeeDescriptionLines)),
            Photos: input.Photos,
            Sources: sources,
            CaseId: assessment.CaseId,
            AssessmentCaseVersion: assessment.CaseVersion,
            RepairSpecificationId: input.RepairSpecificationId,
            RepairSpecificationVersion: input.RepairSpecificationVersion,
            RepairCostSource: input.RepairCostSource);

        return new(snapshot, []);
    }

    private static string? Field(CaseAssessmentProjection assessment, string path) =>
        assessment.Field(path)?.Value;

    private static ReportVehicle BuildVehicle(CaseAssessmentProjection assessment)
    {
        var mileageSource = Field(assessment, AssessmentVocabulary.VehicleMileageSource) ?? "tbc";
        var mileage = assessment.CaseOwned.Mileage;
        var mileageUnit = assessment.CaseOwned.MileageUnit ?? "miles";
        var mileageDescription = mileage is { } value
            ? $"{value:N0} {mileageUnit}"
            : "To be confirmed";

        return new ReportVehicle(
            Registration: assessment.CaseOwned.Registration ?? string.Empty,
            Make: assessment.CaseOwned.Make ?? string.Empty,
            Model: assessment.CaseOwned.Model ?? string.Empty,
            Year: Field(assessment, AssessmentVocabulary.VehicleYear) ?? string.Empty,
            VehicleType: Field(assessment, AssessmentVocabulary.VehicleType) ?? string.Empty,
            Condition: Field(assessment, AssessmentVocabulary.VehicleCondition) ?? string.Empty,
            MileageDescription: mileageDescription,
            MileageSource: mileageSource,
            Vin: Field(assessment, AssessmentVocabulary.VehicleVin),
            Engine: Field(assessment, AssessmentVocabulary.VehicleEngineCc),
            Fuel: Field(assessment, AssessmentVocabulary.VehicleFuel));
    }

    /// <summary>
    /// Groups confirmed line descriptions for the report's parts/repairs/
    /// operations lists. Every estimate line is already confirmed by the
    /// time this runs — <see cref="AssessmentPolicy.EvaluateReadiness"/>
    /// blocks the whole draft on the first unconfirmed line, of any type —
    /// so this only has to group by type and drop blank descriptions.
    /// </summary>
    private static string[] LinesOfType(
        CaseAssessmentProjection assessment, params ReadOnlySpan<string> types)
    {
        var typeSet = new HashSet<string>(types.ToArray(), StringComparer.Ordinal);
        return assessment.EstimateLines
            .Where(line => typeSet.Contains(line.Type) && !string.IsNullOrWhiteSpace(line.Description))
            .OrderBy(line => line.Position)
            .Select(line => line.Description!)
            .ToArray();
    }

    private static string? MapAssessmentMethod(string? inspectionMode) => inspectionMode switch
    {
        "PhysicalAddress" => "physical",
        "ImageBasedAssessment" => "image_based",
        _ => null,
    };

    private static AssessmentReportOutcome MapOutcome(string value) => value switch
    {
        "total_loss" => AssessmentReportOutcome.TotalLoss,
        "repairable" => AssessmentReportOutcome.Repairable,
        "cash_in_lieu" => AssessmentReportOutcome.CashInLieu,
        "contract_repair" => AssessmentReportOutcome.ContractRepair,
        _ => throw new InvalidOperationException($"Unrecognized assessment outcome '{value}'."),
    };

    private static decimal? ParseMoney(string? value) =>
        value is not null
            && decimal.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;

    private static DateOnly? ParseDate(string? value) =>
        value is not null && DateOnly.TryParseExact(value, "yyyy-MM-dd", out var parsed)
            ? parsed
            : null;

    private static string[] SplitLines(string? value) =>
        value is null
            ? []
            : value.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static bool IsValidSource(AcceptedReportSource source)
    {
        try
        {
            source.Validate();
            return true;
        }
        catch (ReportRenderRejectedException)
        {
            return false;
        }
    }

    private static bool IsValidPhoto(ReportImageEvidence photo)
    {
        try
        {
            photo.Validate();
            return true;
        }
        catch (ReportRenderRejectedException)
        {
            return false;
        }
    }

    private static bool IsValidCosts(ReportRepairCosts costs)
    {
        if (costs.IsImported)
        {
            return costs.ImportedLabour is >= 0
                && costs.ImportedVat is >= 0
                && !string.IsNullOrWhiteSpace(costs.ImportedPolicyVersion)
                && costs.Parts >= 0
                && costs.PaintMaterials >= 0
                && costs.SpecialistOther >= 0
                && costs.Total >= 0;
        }

        return costs.LabourHours >= 0
            && costs.HourlyRate > 0
            && costs.Parts >= 0
            && costs.PaintMaterials >= 0
            && costs.SpecialistOther >= 0;
    }
}

/// <summary>
/// The single Core-owned port for everything a report draft needs beyond the
/// assessment record: the case's own identity/addressee and its
/// custody-confirmed photograph and source evidence. Infrastructure supplies
/// it by composing the same accepted queries (case detail, assessment,
/// document custody) the rest of the app already uses — no new persistence.
/// </summary>
public interface IAssessmentReportProjectionSource
{
    Task<AssessmentReportProjectionInput?> GetAsync(
        Guid caseId,
        ActionActor actor,
        Guid? selectedRepairSpecificationId = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Shared readiness use case for both the operator draft action and a future
/// report-registration caller. Callers receive the same projection result;
/// neither Web nor Infrastructure owns a second required-field list.
/// </summary>
public sealed class AssessCaseReportReadiness(IAssessmentReportProjectionSource source)
{
    public async Task<AssessmentReportProjectionResult?> ExecuteAsync(
        Guid caseId,
        ActionActor actor,
        Guid? selectedRepairSpecificationId = null,
        CancellationToken cancellationToken = default)
    {
        var input = await source.GetAsync(
            caseId,
            actor,
            selectedRepairSpecificationId,
            cancellationToken);
        return input is null ? null : AssessmentReportProjection.Project(input);
    }
}

/// <summary>
/// The read-only preparation a control renders from: ready, or the exact
/// reasons it is not. Mirrors the existing *Preparation naming
/// (<see cref="Pegasus.Core.Eva.EvaHandoffPreparation"/>,
/// <see cref="Pegasus.Core.Custody.CaseCustodyPreparation"/>) rather than a
/// new shape.
/// </summary>
public sealed record AssessmentReportDraftPreparation(IReadOnlyList<AssessmentReadinessItem> Reasons)
{
    public bool CanGenerate => Reasons.Count == 0;
}

public enum GenerateCaseAssessmentReportDraftOutcome
{
    Generated,
    NotReady,
    NotFound,
}

public sealed record GenerateCaseAssessmentReportDraftResult(
    GenerateCaseAssessmentReportDraftOutcome Outcome,
    AssessmentReportDraft? Draft,
    IReadOnlyList<AssessmentReadinessItem> Reasons);

/// <summary>
/// The reachable operator entry point (DELIV-012): loads a case's report
/// inputs, projects them, and renders the draft only when every requirement
/// is met. Authorisation is inherited from the composed
/// <see cref="IAssessmentReportProjectionSource"/> (the same
/// <c>StaffAuthorization</c> check the case-detail query already performs) —
/// nothing new is invented here.
/// </summary>
public sealed class GenerateCaseAssessmentReportDraft(
    AssessCaseReportReadiness readiness,
    GenerateAssessmentReportDraft generate,
    IAssessmentReportStore reportStore)
{
    public Task<IReadOnlyList<AssessmentReportVersion>> GetVersionsAsync(
        Guid caseId,
        CancellationToken cancellationToken = default) =>
        reportStore.ListAsync(caseId, cancellationToken);

    public async Task<AssessmentReportDraftPreparation?> PrepareAsync(
        Guid caseId,
        ActionActor actor,
        Guid? selectedRepairSpecificationId = null,
        CancellationToken cancellationToken = default)
    {
        var projected = await readiness.ExecuteAsync(
            caseId,
            actor,
            selectedRepairSpecificationId,
            cancellationToken);
        return projected is null
            ? null
            : new AssessmentReportDraftPreparation(projected.Reasons);
    }

    public async Task<GenerateCaseAssessmentReportDraftResult> ExecuteAsync(
        Guid caseId,
        ActionActor actor,
        Guid? selectedRepairSpecificationId = null,
        Guid? reportVersionId = null,
        CancellationToken cancellationToken = default)
    {
        AssessmentReportSnapshot snapshot;
        if (reportVersionId is { } requestedVersionId)
        {
            var storedVersion = (await reportStore.ListAsync(caseId, cancellationToken))
                .SingleOrDefault(item => item.Id == requestedVersionId);
            if (storedVersion is null)
            {
                return new(GenerateCaseAssessmentReportDraftOutcome.NotFound, null, []);
            }

            snapshot = AssessmentReportPayload.Deserialize(storedVersion.AcceptedPayloadJson);
        }
        else
        {
            var projected = await readiness.ExecuteAsync(
                caseId,
                actor,
                selectedRepairSpecificationId,
                cancellationToken);
            if (projected is null)
            {
                return new(GenerateCaseAssessmentReportDraftOutcome.NotFound, null, []);
            }

            if (!projected.IsReady)
            {
                return new(GenerateCaseAssessmentReportDraftOutcome.NotReady, null, projected.Reasons);
            }

            snapshot = projected.Snapshot!;
        }

        var reservation = await reportStore.BeginAsync(
            new AssessmentReportGenerationRequest(caseId, snapshot, actor),
            cancellationToken);
        if (reservation.IsReplay)
        {
            var replay = await reportStore.ReadDraftAsync(reservation.Version, cancellationToken)
                ?? throw new InvalidOperationException("The stored report version has no complete artifact pair.");
            return new(GenerateCaseAssessmentReportDraftOutcome.Generated, replay, []);
        }

        if (!reservation.ShouldRender)
        {
            throw new InvalidOperationException(
                "A report draft for this accepted input is already being generated. Retry after it completes.");
        }

        try
        {
            var canonicalSnapshot = AssessmentReportPayload.Deserialize(
                reservation.Version.AcceptedPayloadJson);
            var draft = await generate.ExecuteAsync(canonicalSnapshot, cancellationToken);
            await reportStore.CompleteAsync(reservation, draft, cancellationToken);
            return new(GenerateCaseAssessmentReportDraftOutcome.Generated, draft, []);
        }
        catch (Exception exception)
        {
            await reportStore.FailAsync(reservation, exception.Message, CancellationToken.None);
            throw;
        }
    }
}
