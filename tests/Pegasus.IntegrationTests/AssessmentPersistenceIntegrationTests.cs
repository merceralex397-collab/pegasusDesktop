using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Pegasus.Core.AiWork;
using Pegasus.Core.Assessment;
using Pegasus.Core.Cases;
using Pegasus.Core.Documents;
using Pegasus.Core.Identity;
using Pegasus.Core.Intake;
using Pegasus.Core.Lifecycle;
using Pegasus.Core.Reports;
using Pegasus.Core.Workflow;
using Pegasus.Infrastructure.Custody;
using Pegasus.Infrastructure.Persistence;

namespace Pegasus.IntegrationTests;

[Trait("Category", "SqlServer")]
public sealed class AssessmentPersistenceIntegrationTests
{
    private static readonly DateTimeOffset StartUtc =
        new(2031, 5, 6, 10, 30, 0, TimeSpan.Zero);

    private static AssessmentReportDraft ReportDraft(string? templateVersion = null)
    {
        templateVersion ??= AssessmentReportContract.TemplateVersion;
        var assessment = new RenderedReportArtifact(
            "assessment.pdf", [1, 2, 3], 1,
            Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData([1, 2, 3])),
            templateVersion, "test");
        var feeNote = new RenderedReportArtifact(
            "fee-note.pdf", [4, 5, 6], 1,
            Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData([4, 5, 6])),
            templateVersion, "test");
        return new(assessment, feeNote);
    }

    private static AssessmentReportSnapshot ReportSnapshot(
        Guid caseId,
        RepairSpecificationVersion specification)
    {
        var basis = specification.CalculationBasis!;
        var source = new AcceptedReportSource(
            specification.Source.ArtifactReference!,
            specification.Source.SourceVersion!,
            specification.Source.Sha256!);
        var snapshot = Reports.AssessmentReportRendererTests.Snapshot(
            AssessmentReportOutcome.Repairable) with
        {
            CaseId = caseId,
            AssessmentCaseVersion = 2,
            RepairSpecificationId = specification.SpecificationId,
            RepairSpecificationVersion = specification.Version,
            Costs = ReportRepairCosts.FromAcceptedBasis(basis),
            RepairCostSource = source
        };
        return snapshot with { Sources = snapshot.Sources.Append(source).ToArray() };
    }

    [Fact]
    public async Task AutomationSaveIsUnconfirmedAttributedAndParityLoggedWithAStaffSave()
    {
        await using var harness = await Harness.CreateAsync();
        var outcome = await harness.AcceptAsync("assessment-accept-1");
        var caseId = outcome.Identity.CaseId;

        // The Automation actor writes under the same lease and version
        // guards as a staff save; its values land unconfirmed.
        var automationLease = await harness.AcquireLeaseAsync(
            caseId,
            0,
            harness.AutomationActor,
            "assessment-lease-automation");
        var saved = await harness.SaveAssessment.ExecuteAsync(
            new(
                caseId,
                automationLease.Version,
                harness.AutomationActor,
                "mcp:assessment-save-1",
                "Automation recorded the assessment draft.",
                automationLease.Token,
                new Dictionary<string, string?>(StringComparer.Ordinal)
                {
                    ["vehicle.condition"] = "good",
                    ["assessment.outcome"] = "total_loss",
                    ["assessment.category"] = "S",
                    ["assessment.salvage_value"] = "1500.00",
                    ["assessment.values.retail"] = "12000",
                    ["assessment.values.trade"] = "10500",
                    ["assessment.values.engineer"] = "12000"
                },
                [
                    new("repair", null, "Repair nearside door", 3.5m, null, false, null, null,
                        "estimated", "judgement", "Visible panel damage"),
                    new("new_part", null, "Door skin", null, 220.40m, false, "P-1234", null,
                        "confirmed", "official", "Distorted beyond repair")
                ]),
            CancellationToken.None);

        Assert.Equal(1, saved.CaseVersion);
        Assert.All(saved.Fields, field =>
        {
            Assert.Equal(ActorKind.Automation, field.RecordedByKind);
            Assert.False(field.IsConfirmed);
        });
        Assert.Equal(2, saved.EstimateLines.Count);
        Assert.All(saved.EstimateLines, line => Assert.False(line.IsConfirmed));
        Assert.Contains(
            saved.Readiness,
            item => item.Requirement == "vehicle.condition awaits review"
                && item.Source.Contains("Automation", StringComparison.Ordinal));
        Assert.Contains(
            saved.Readiness,
            item => item.Requirement == "Estimate line 1 (repair) awaits review");

        // A staff Engineer re-saves one finding with the same value: the
        // value flips to confirmed, and both saves left exactly the same
        // shape of permanent evidence (logging parity, side by side). The
        // clock advances so the two history rows order deterministically.
        harness.Advance(TimeSpan.FromMinutes(1));
        var staffLease = await harness.AcquireLeaseAsync(
            caseId,
            saved.CaseVersion,
            harness.EngineerActor,
            "assessment-lease-staff");
        var confirmed = await harness.SaveAssessment.ExecuteAsync(
            new(
                caseId,
                staffLease.Version,
                harness.EngineerActor,
                "staff-assessment-save-1",
                "Engineer confirmed the recorded outcome.",
                staffLease.Token,
                new Dictionary<string, string?>(StringComparer.Ordinal)
                {
                    ["assessment.outcome"] = "total_loss"
                }),
            CancellationToken.None);
        var confirmedOutcome = confirmed.Field("assessment.outcome");
        Assert.NotNull(confirmedOutcome);
        Assert.True(confirmedOutcome!.IsConfirmed);
        Assert.Equal(ActorKind.Staff, confirmedOutcome.RecordedByKind);

        await using var context = await harness.Factory.CreateDbContextAsync();
        var history = await context.ActionHistory.AsNoTracking()
            .Where(item => item.EventKind == "case_assessment_saved")
            .OrderBy(item => item.OccurredAtUtc)
            .ToArrayAsync();
        Assert.Equal(2, history.Length);
        Assert.Equal(nameof(ActorKind.Automation), history[0].ActorKind);
        Assert.Equal("pegasus-automation", history[0].ActorSubjectId);
        Assert.Equal(nameof(ActorKind.Staff), history[1].ActorKind);
        Assert.All(history, entry =>
        {
            Assert.Equal("case", entry.AggregateType);
            Assert.Equal(caseId.ToString("D"), entry.AggregateId);
            Assert.Equal("Succeeded", entry.Outcome);
            Assert.False(string.IsNullOrWhiteSpace(entry.BeforeJson));
            Assert.False(string.IsNullOrWhiteSpace(entry.AfterJson));
            Assert.False(string.IsNullOrWhiteSpace(entry.Reason));
            Assert.Equal("case-assessment-edit/v1", entry.PolicyVersion);
        });
        Assert.Equal(2, await context.CaseWorkflowEvents.AsNoTracking()
            .CountAsync(item => item.CaseId == caseId
                && item.EventType == "case_assessment_saved"));
        Assert.Equal(2, await context.CaseHistory.AsNoTracking()
            .CountAsync(item => item.CaseId == caseId
                && item.EventType == "case_assessment_saved"));
    }

    [Fact]
    public async Task OperationKeyReplayReturnsTheOriginalResultAndConflictsOnDifferentMaterial()
    {
        await using var harness = await Harness.CreateAsync();
        var outcome = await harness.AcceptAsync("assessment-accept-2");
        var caseId = outcome.Identity.CaseId;
        var lease = await harness.AcquireLeaseAsync(
            caseId,
            0,
            harness.AutomationActor,
            "assessment-lease-replay");
        SaveAssessmentRequest Request(string value) => new(
            caseId,
            lease.Version,
            harness.AutomationActor,
            "mcp:assessment-replay",
            "Automation recorded the assessment draft.",
            lease.Token,
            new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["vehicle.condition"] = value
            });

        var first = await harness.SaveAssessment.ExecuteAsync(
            Request("good"),
            CancellationToken.None);
        Assert.Equal(1, first.CaseVersion);

        var replay = await harness.SaveAssessment.ExecuteAsync(
            Request("good"),
            CancellationToken.None);
        Assert.Equal(1, replay.CaseVersion);
        await using (var context = await harness.Factory.CreateDbContextAsync())
        {
            Assert.Equal(1, await context.CaseWorkflowEvents.AsNoTracking()
                .CountAsync(item => item.CaseId == caseId
                    && item.EventType == "case_assessment_saved"));
        }

        await Assert.ThrowsAsync<CaseOperationConflictException>(() =>
            harness.SaveAssessment.ExecuteAsync(Request("poor"), CancellationToken.None));
    }

    [Fact]
    public async Task ReportStoreReplaysAnExactInputAndAppendsACorrectionVersion()
    {
        await using var harness = await Harness.CreateAsync();
        var outcome = await harness.AcceptAsync("assessment-report-accept");
        var caseId = outcome.Identity.CaseId;
        var specification = await harness.AcceptReportSpecificationAsync(caseId);
        var snapshot = ReportSnapshot(caseId, specification);
        var request = new AssessmentReportGenerationRequest(caseId, snapshot, harness.EngineerActor);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.ReportStore.BeginAsync(
                request with
                {
                    Snapshot = snapshot with
                    {
                        RepairSpecificationId = null,
                        RepairSpecificationVersion = null,
                        RepairCostSource = null
                    }
                }));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.ReportStore.BeginAsync(
                request with
                {
                    Snapshot = snapshot with
                    {
                        RepairCostSource = new AcceptedReportSource(
                            "case://repair-spec/wrong-source", "source-v1", new string('b', 64))
                    }
                }));

        var first = await harness.ReportStore.BeginAsync(request);
        Assert.True(first.ShouldRender);
        var completed = await harness.ReportStore.CompleteAsync(first, ReportDraft());

        var replay = await harness.ReportStore.BeginAsync(request);
        Assert.True(replay.IsReplay);
        Assert.Equal(completed.Id, replay.Version.Id);
        var replayDraft = await harness.ReportStore.ReadDraftAsync(replay.Version);
        Assert.NotNull(replayDraft);
        Assert.Equal(ReportDraft().Assessment.Sha256, replayDraft!.Assessment.Sha256);

        var replayAfterUnrelatedCaseEdit = await harness.ReportStore.BeginAsync(
            request with { Snapshot = snapshot with { AssessmentCaseVersion = 1 } });
        Assert.True(replayAfterUnrelatedCaseEdit.IsReplay);
        Assert.Equal(completed.Id, replayAfterUnrelatedCaseEdit.Version.Id);

        var correction = await harness.ReportStore.BeginAsync(
            request with { Snapshot = snapshot with { EngineerComments = "Correction retained" } });
        Assert.True(correction.ShouldRender);
        Assert.Equal(2, correction.Version.Version);
        Assert.Equal(completed.Id, correction.Version.PredecessorId);
        await harness.ReportStore.CompleteAsync(correction, ReportDraft());

        await using var context = await harness.Factory.CreateDbContextAsync();
        Assert.Equal(2, await context.AssessmentReportVersions.AsNoTracking()
            .CountAsync(item => item.CaseId == caseId));
        Assert.Equal(4, await context.AssessmentReportArtifacts.AsNoTracking()
            .CountAsync(item => item.ReportVersion.CaseId == caseId));
        Assert.Equal(4, await context.Set<DocumentOccurrenceEntity>().AsNoTracking()
            .CountAsync(item => item.CaseId == caseId && item.Source == Pegasus.Core.Documents.DocumentSource.Generated));

    }

    [Fact]
    public async Task ReportStoreRejectsAnArtifactFromTheWrongTemplateVersion()
    {
        await using var harness = await Harness.CreateAsync();
        var outcome = await harness.AcceptAsync("assessment-report-template");
        var caseId = outcome.Identity.CaseId;
        var specification = await harness.AcceptReportSpecificationAsync(caseId);
        var snapshot = ReportSnapshot(caseId, specification);
        var reservation = await harness.ReportStore.BeginAsync(
            new AssessmentReportGenerationRequest(caseId, snapshot, harness.EngineerActor));

        await Assert.ThrowsAsync<ReportRenderRejectedException>(() =>
            harness.ReportStore.CompleteAsync(reservation, ReportDraft("report-template/wrong")));
    }

    [Fact]
    public async Task ConcurrentFailureReportsOnlyTheActiveLeaseOwner()
    {
        await using var harness = await Harness.CreateAsync();
        var outcome = await harness.AcceptAsync("assessment-report-failure-race");
        var caseId = outcome.Identity.CaseId;
        var specification = await harness.AcceptReportSpecificationAsync(caseId);
        var snapshot = ReportSnapshot(caseId, specification);
        var reservation = await harness.ReportStore.BeginAsync(
            new AssessmentReportGenerationRequest(caseId, snapshot, harness.EngineerActor));

        await Task.WhenAll(
            harness.ReportStore.FailAsync(reservation, "first failure"),
            harness.ReportStore.FailAsync(reservation, "second failure"));

        await using var context = await harness.Factory.CreateDbContextAsync();
        var entity = await context.AssessmentReportVersions.AsNoTracking()
            .SingleAsync(item => item.Id == reservation.Version.Id);
        Assert.Equal(AssessmentReportGenerationState.Pending.ToString(), entity.State);
        Assert.Null(entity.LeaseId);
        Assert.Equal(1, entity.AttemptCount);
        Assert.Equal(AssessmentReportFailureMessages.GenerationFailed, entity.FailureReason);
    }

    [Fact]
    public async Task ConcurrentIdenticalReportRequestsReserveOnlyOneRenderer()
    {
        await using var harness = await Harness.CreateAsync();
        var outcome = await harness.AcceptAsync("assessment-report-concurrent");
        var caseId = outcome.Identity.CaseId;
        var specification = await harness.AcceptReportSpecificationAsync(caseId);
        var snapshot = ReportSnapshot(caseId, specification);
        var request = new AssessmentReportGenerationRequest(caseId, snapshot, harness.EngineerActor);

        var reservations = await Task.WhenAll(
            Task.Run(() => harness.ReportStore.BeginAsync(request)),
            Task.Run(() => harness.ReportStore.BeginAsync(request)));

        Assert.Single(reservations, item => item.ShouldRender);
        Assert.Single(reservations, item => !item.ShouldRender);
        await using var context = await harness.Factory.CreateDbContextAsync();
        Assert.Equal(
            1,
            await context.AssessmentReportVersions
                .AsNoTracking()
                .CountAsync(item => item.CaseId == caseId));
    }

    [Fact]
    public async Task ReportGenerationRetriesWithBackoffAndStopsAtTheTerminalAttempt()
    {
        await using var harness = await Harness.CreateAsync();
        var outcome = await harness.AcceptAsync("assessment-report-retries");
        var caseId = outcome.Identity.CaseId;
        var specification = await harness.AcceptReportSpecificationAsync(caseId);
        var snapshot = ReportSnapshot(caseId, specification);
        var request = new AssessmentReportGenerationRequest(caseId, snapshot, harness.EngineerActor);

        var first = await harness.ReportStore.BeginAsync(request);
        Assert.Equal(1, first.Version.AttemptCount);
        await harness.ReportStore.FailAsync(first, "renderer failed");
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.ReportStore.BeginAsync(request));

        harness.Advance(TimeSpan.FromSeconds(5));
        var second = await harness.ReportStore.BeginAsync(request);
        Assert.Equal(2, second.Version.AttemptCount);
        await harness.ReportStore.FailAsync(second, "renderer failed again");

        harness.Advance(TimeSpan.FromSeconds(10));
        var third = await harness.ReportStore.BeginAsync(request);
        Assert.Equal(3, third.Version.AttemptCount);
        await harness.ReportStore.FailAsync(third, "renderer failed finally");

        harness.Advance(TimeSpan.FromSeconds(15));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.ReportStore.BeginAsync(request));
        await using var context = await harness.Factory.CreateDbContextAsync();
        var reportId = await context.AssessmentReportVersions
            .Where(item => item.CaseId == caseId)
            .Select(item => item.Id)
            .SingleAsync();
        Assert.All(
            await context.Set<DocumentVersionEntity>()
                .Where(item => context.AssessmentReportArtifacts
                    .Where(artifact => artifact.ReportVersionId == reportId)
                    .Select(artifact => artifact.DocumentVersionId)
                    .Contains(item.Id))
                .ToArrayAsync(),
            item => Assert.Equal(DocumentCustodyStatus.Failed, item.CustodyStatus));
    }

    [Fact]
    public async Task RecoveryReconcilesMetadataCommittedBeforeContentWrite()
    {
        await using var harness = await Harness.CreateAsync();
        var outcome = await harness.AcceptAsync("assessment-report-recovery");
        var caseId = outcome.Identity.CaseId;
        var specification = await harness.AcceptReportSpecificationAsync(caseId);
        var snapshot = ReportSnapshot(caseId, specification);
        var request = new AssessmentReportGenerationRequest(caseId, snapshot, harness.EngineerActor);
        var failingStore = harness.CreateReportStore(
            new FailFirstDocumentContentStore(harness.NewLocalContentStore()));

        var reservation = await failingStore.BeginAsync(request);
        await Assert.ThrowsAsync<IOException>(() =>
            failingStore.CompleteAsync(reservation, ReportDraft()));

        await using (var context = await harness.Factory.CreateDbContextAsync())
        {
            Assert.Equal(
                AssessmentReportGenerationState.Rendering.ToString(),
                await context.AssessmentReportVersions
                    .Where(item => item.Id == reservation.Version.Id)
                    .Select(item => item.State)
                    .SingleAsync());
            Assert.Equal(
                2,
                await context.AssessmentReportArtifacts
                    .Where(item => item.ReportVersionId == reservation.Version.Id)
                    .CountAsync());
            Assert.All(
                await context.Set<DocumentVersionEntity>()
                    .Where(item => context.AssessmentReportArtifacts
                        .Where(artifact => artifact.ReportVersionId == reservation.Version.Id)
                        .Select(artifact => artifact.DocumentVersionId)
                        .Contains(item.Id))
                    .ToArrayAsync(),
                item => Assert.Equal(DocumentCustodyStatus.Pending, item.CustodyStatus));
        }

        harness.Advance(TimeSpan.FromMinutes(6));
        var recovery = await harness.ReportStore.BeginAsync(request);
        Assert.True(recovery.ShouldRender);
        Assert.Equal(reservation.Version.Id, recovery.Version.Id);
        await harness.ReportStore.CompleteAsync(recovery, ReportDraft());
        Assert.True((await harness.ReportStore.BeginAsync(request)).IsReplay);
    }

    [Fact]
    public async Task TerminalFailureMarksPendingGeneratedDocumentsFailed()
    {
        await using var harness = await Harness.CreateAsync();
        var outcome = await harness.AcceptAsync("assessment-report-terminal");
        var caseId = outcome.Identity.CaseId;
        var specification = await harness.AcceptReportSpecificationAsync(caseId);
        var snapshot = ReportSnapshot(caseId, specification);
        var request = new AssessmentReportGenerationRequest(caseId, snapshot, harness.EngineerActor);
        var failingStore = harness.CreateReportStore(
            new FailFirstDocumentContentStore(harness.NewLocalContentStore()));

        var first = await failingStore.BeginAsync(request);
        await Assert.ThrowsAsync<IOException>(() =>
            failingStore.CompleteAsync(first, ReportDraft()));

        harness.Advance(TimeSpan.FromMinutes(6));
        var second = await harness.ReportStore.BeginAsync(request);
        await harness.ReportStore.FailAsync(second, "second renderer failure");
        harness.Advance(TimeSpan.FromSeconds(10));
        var third = await harness.ReportStore.BeginAsync(request);
        await harness.ReportStore.FailAsync(third, "terminal renderer failure");
        harness.Advance(TimeSpan.FromSeconds(15));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.ReportStore.BeginAsync(request));

        await using var context = await harness.Factory.CreateDbContextAsync();
        var reportId = await context.AssessmentReportVersions
            .Where(item => item.CaseId == caseId)
            .Select(item => item.Id)
            .SingleAsync();
        Assert.All(
            await context.Set<DocumentVersionEntity>()
                .Where(item => context.AssessmentReportArtifacts
                    .Where(artifact => artifact.ReportVersionId == reportId)
                    .Select(artifact => artifact.DocumentVersionId)
                    .Contains(item.Id))
                .ToArrayAsync(),
            item => Assert.Equal(DocumentCustodyStatus.Failed, item.CustodyStatus));
    }

    [Fact]
    public async Task StaleVersionsAndMissingLeasesFailClosed()
    {
        await using var harness = await Harness.CreateAsync();
        var outcome = await harness.AcceptAsync("assessment-accept-3");
        var caseId = outcome.Identity.CaseId;

        await Assert.ThrowsAsync<CaseEditLeaseExpiredException>(() =>
            harness.SaveAssessment.ExecuteAsync(
                new(
                    caseId,
                    0,
                    harness.AutomationActor,
                    "mcp:assessment-noleased",
                    "Automation recorded the assessment draft.",
                    "not-a-lease",
                    new Dictionary<string, string?>(StringComparer.Ordinal)
                    {
                        ["vehicle.condition"] = "good"
                    }),
                CancellationToken.None));

        var lease = await harness.AcquireLeaseAsync(
            caseId,
            0,
            harness.AutomationActor,
            "assessment-lease-stale");
        await Assert.ThrowsAsync<CaseVersionConflictException>(() =>
            harness.SaveAssessment.ExecuteAsync(
                new(
                    caseId,
                    lease.Version + 5,
                    harness.AutomationActor,
                    "mcp:assessment-stale",
                    "Automation recorded the assessment draft.",
                    lease.Token,
                    new Dictionary<string, string?>(StringComparer.Ordinal)
                    {
                        ["vehicle.condition"] = "good"
                    }),
                CancellationToken.None));
    }

    [Fact]
    public async Task AnUnknownWorkRequestBindingFailsClosed()
    {
        await using var harness = await Harness.CreateAsync();
        var outcome = await harness.AcceptAsync("assessment-accept-4");
        var caseId = outcome.Identity.CaseId;
        var lease = await harness.AcquireLeaseAsync(
            caseId,
            0,
            harness.AutomationActor,
            "assessment-lease-binding");
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.SaveAssessment.ExecuteAsync(
                new(
                    caseId,
                    lease.Version,
                    harness.AutomationActor,
                    "mcp:assessment-binding",
                    "Automation recorded the assessment draft.",
                    lease.Token,
                    new Dictionary<string, string?>(StringComparer.Ordinal)
                    {
                        ["vehicle.condition"] = "good"
                    },
                    AiWorkRequestId: Guid.NewGuid()),
                CancellationToken.None));
    }

    [Fact]
    public async Task RepairSpecificationAcceptanceCorrectionAndExactVersionPersist()
    {
        await using var harness = await Harness.CreateAsync();
        var outcome = await harness.AcceptAsync("repair-spec-accept-case");
        var caseId = outcome.Identity.CaseId;
        var source = new RepairSpecificationSource(
            RepairSpecificationSourceRoute.Manual,
            "case://repair-spec/source-1",
            "source-v1",
            new string('a', 64));
        var basis = new RepairCalculationBasis(100m, 20m, 10m, 0m, true, 26m, 156m, "calc/v1");
        var lines = new EstimateLineInput[]
        {
            new("new_part", null, "Door skin", null, 20m, false, null, null,
                "confirmed", "case", "Engineer mapping"),
            new("repair", null, "Repair door", 2m, null, false, null, null,
                "confirmed", "judgement", "Engineer mapping"),
        };

        var draftLease = await harness.AcquireLeaseAsync(
            caseId, 0, harness.EngineerActor, "repair-spec-draft-lease");
        var draftRequest = new StartRepairSpecificationDraftRequest(
            caseId, draftLease.Version, source, harness.EngineerActor,
            "repair-spec-draft", "Create the canonical repair specification.",
            draftLease.Token, Lines: lines);
        var draft = await harness.RepairSpecifications.StartDraftAsync(draftRequest, CancellationToken.None);
        var replayedDraft = await harness.RepairSpecifications.StartDraftAsync(draftRequest, CancellationToken.None);
        Assert.Equal(draft.SpecificationId, replayedDraft.SpecificationId);

        var acceptLease = await harness.AcquireLeaseAsync(
            caseId, 1, harness.EngineerActor, "repair-spec-accept-lease");
        var accepted = await harness.RepairSpecifications.AcceptAsync(
            new(caseId, acceptLease.Version, draft.SpecificationId, draft.Version, source, basis,
                harness.EngineerActor, "repair-spec-accept", "Engineer accepted the source and mapping.",
                acceptLease.Token), CancellationToken.None);
        Assert.Equal(RepairSpecificationState.Accepted, accepted.State);
        Assert.Equal(["Door skin"], RepairSpecificationPolicy.ToDisplayLists(accepted).NewParts);

        var correctionLease = await harness.AcquireLeaseAsync(
            caseId, 2, harness.EngineerActor, "repair-spec-correct-lease");
        var correction = await harness.RepairSpecifications.StartDraftAsync(
            new(caseId, correctionLease.Version, source with { SourceVersion = "source-v2" },
                harness.EngineerActor, "repair-spec-correct", "Correct the accepted mapping.",
                correctionLease.Token, accepted.SpecificationId), CancellationToken.None);
        Assert.Equal(2, correction.Version);
        Assert.Equal(accepted.SpecificationId, correction.SupersedesSpecificationId);

        var correctionAcceptLease = await harness.AcquireLeaseAsync(
            caseId, 3, harness.EngineerActor, "repair-spec-correct-accept-lease");
        var corrected = await harness.RepairSpecifications.AcceptAsync(
            new(caseId, correctionAcceptLease.Version, correction.SpecificationId, correction.Version,
                source with { SourceVersion = "source-v2" }, basis, harness.EngineerActor,
                "repair-spec-correct-accept", "Engineer accepted the corrected mapping.",
                correctionAcceptLease.Token), CancellationToken.None);
        Assert.Equal(corrected.SpecificationId,
            (await harness.RepairSpecifications.GetCurrentAcceptedAsync(
                caseId, CancellationToken.None))!.SpecificationId);
        Assert.Equal(RepairSpecificationState.Superseded,
            (await harness.RepairSpecifications.GetVersionAsync(
                caseId, accepted.SpecificationId, CancellationToken.None))!.State);

        Assert.Equal(corrected.SpecificationId,
            (await harness.RepairSpecifications.GetVersionAsync(
                caseId, corrected.SpecificationId, CancellationToken.None))!.SpecificationId);
    }

    [Fact]
    public async Task IndependentAcceptedEstimatesRemainSeparateAndListable()
    {
        await using var harness = await Harness.CreateAsync();
        var first = await harness.AcceptReportSpecificationAsync(
            (await harness.AcceptAsync("repair-estimate-first-case")).Identity.CaseId);
        var caseId = first.CaseId;
        var source = new RepairSpecificationSource(
            RepairSpecificationSourceRoute.Manual,
            "case://repair-spec/second-source",
            "source-v2",
            new string('b', 64));
        var basis = new RepairCalculationBasis(200m, 40m, 10m, 5m, true, 51m, 306m, "external-estimate/v1");
        var lines = new EstimateLineInput[]
        {
            new("new_part", null, "Second bumper", null, 40m, false, null, null,
                "confirmed", "case", "Independent estimate"),
        };

        var draftLease = await harness.AcquireLeaseAsync(
            caseId, 2, harness.EngineerActor, "repair-spec-second-draft-lease");
        var draft = await harness.RepairSpecifications.StartDraftAsync(
            new(
                caseId,
                draftLease.Version,
                source,
                harness.EngineerActor,
                "repair-spec-second-draft",
                "Record an independent repair estimate.",
                draftLease.Token,
                Lines: lines),
            CancellationToken.None);
        var acceptLease = await harness.AcquireLeaseAsync(
            caseId, 3, harness.EngineerActor, "repair-spec-second-accept-lease");
        var second = await harness.RepairSpecifications.AcceptAsync(
            new(
                caseId,
                acceptLease.Version,
                draft.SpecificationId,
                draft.Version,
                source,
                basis,
                harness.EngineerActor,
                "repair-spec-second-accept",
                "Accept the independent repair estimate.",
                acceptLease.Token),
            CancellationToken.None);

        var accepted = await harness.RepairSpecifications.ListAcceptedAsync(
            caseId, CancellationToken.None);
        Assert.Equal(2, accepted.Count);
        Assert.Contains(accepted, item => item.SpecificationId == first.SpecificationId);
        Assert.Contains(accepted, item => item.SpecificationId == second.SpecificationId);
        Assert.All(accepted, item => Assert.Equal(RepairSpecificationState.Accepted, item.State));
    }

    [Fact]
    public async Task TheAiWorkRequestLifecyclePersistsWithCorrelatedHistory()
    {
        await using var harness = await Harness.CreateAsync();
        var outcome = await harness.AcceptAsync("assessment-accept-5");
        var caseId = outcome.Identity.CaseId;
        var staff = harness.EngineerActor;

        var created = await harness.WorkRequests.CreateAsync(
            new(
                caseId,
                outcome.Identity.Reference,
                0,
                staff,
                "send-op-1",
                "Work the assessment.",
                TimeSpan.FromHours(24)),
            CancellationToken.None);
        Assert.Equal(AiWorkRequestState.Created, created.State);

        // Creation replays idempotently on the same operation key and
        // conflicts on different material.
        var replay = await harness.WorkRequests.CreateAsync(
            new(
                caseId,
                outcome.Identity.Reference,
                0,
                staff,
                "send-op-1",
                "Work the assessment.",
                TimeSpan.FromHours(24)),
            CancellationToken.None);
        Assert.Equal(created.RequestId, replay.RequestId);
        await Assert.ThrowsAsync<CaseOperationConflictException>(() =>
            harness.WorkRequests.CreateAsync(
                new(
                    caseId,
                    outcome.Identity.Reference,
                    0,
                    staff,
                    "send-op-1",
                    "A different instruction.",
                    TimeSpan.FromHours(24)),
                CancellationToken.None));

        var handedOff = await harness.WorkRequests.TransitionAsync(
            new(created.RequestId, created.Version, AiWorkRequestState.HandedOff, staff, "t-1"),
            CancellationToken.None);
        Assert.Equal(AiWorkRequestState.HandedOff, handedOff.State);
        Assert.NotNull(handedOff.HandedOffAtUtc);

        var completed = await harness.WorkRequests.TransitionAsync(
            new(
                created.RequestId,
                handedOff.Version,
                AiWorkRequestState.Completed,
                staff,
                "t-2",
                ReplyStatus: "done",
                ReplyMessage: "Assessment recorded."),
            CancellationToken.None);
        Assert.Equal(AiWorkRequestState.Completed, completed.State);
        Assert.Equal("Assessment recorded.", completed.ReplyMessage);

        // Completed is terminal: reopening it is an illegal transition, and
        // an exact repeat of the terminal transition replays inertly.
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.WorkRequests.TransitionAsync(
                new(
                    created.RequestId,
                    completed.Version,
                    AiWorkRequestState.HandedOff,
                    staff,
                    "t-3"),
                CancellationToken.None));
        var repeat = await harness.WorkRequests.TransitionAsync(
            new(
                created.RequestId,
                completed.Version,
                AiWorkRequestState.Completed,
                staff,
                "t-2"),
            CancellationToken.None);
        Assert.Equal(completed.Version, repeat.Version);

        await using var context = await harness.Factory.CreateDbContextAsync();
        var history = await context.ActionHistory.AsNoTracking()
            .Where(item => item.AggregateType == "ai_work_request")
            .ToArrayAsync();
        Assert.Equal(3, history.Length);
        Assert.All(history, entry =>
            Assert.Equal(created.RequestId.ToString("D"), entry.CorrelationId));
        Assert.Contains(history, entry => entry.EventKind == "ai_work_request_created");
        Assert.Contains(history, entry => entry.EventKind == "ai_work_request_handedoff");
        Assert.Contains(history, entry => entry.EventKind == "ai_work_request_completed");
    }

    private sealed class Harness : IAsyncDisposable
    {
        private readonly LocalDbTestDatabase database;
        private readonly string contentRoot;
        private readonly AcquireCaseEditLease acquireLease;
        private readonly AcceptIntake acceptIntake;
        private readonly CaseDataCompletenessPersistenceTests.MutableTimeProvider timeProvider;

        private Harness(
            LocalDbTestDatabase database,
            PooledDbContextFactory<PegasusDbContext> factory,
            Guid receiptId,
            AcceptIntake acceptIntake,
            AcquireCaseEditLease acquireLease,
            SaveAssessment saveAssessment,
            EfAiWorkRequestStore workRequests,
            EfRepairSpecificationStore repairSpecifications,
            CaseDataCompletenessPersistenceTests.MutableTimeProvider timeProvider,
            string contentRoot,
            EfAssessmentReportStore reportStore)
        {
            this.database = database;
            Factory = factory;
            ReceiptId = receiptId;
            this.acceptIntake = acceptIntake;
            this.acquireLease = acquireLease;
            SaveAssessment = saveAssessment;
            WorkRequests = workRequests;
            RepairSpecifications = repairSpecifications;
            this.timeProvider = timeProvider;
            this.contentRoot = contentRoot;
            ReportStore = reportStore;
        }

        public PooledDbContextFactory<PegasusDbContext> Factory { get; }
        public Guid ReceiptId { get; }
        public SaveAssessment SaveAssessment { get; }
        public EfAiWorkRequestStore WorkRequests { get; }
        public EfRepairSpecificationStore RepairSpecifications { get; }
        public EfAssessmentReportStore ReportStore { get; }
        public ActionActor AutomationActor { get; } = ActionActor.Automation("pegasus-automation");
        public ActionActor EngineerActor { get; } =
            ActionActor.Staff(Guid.NewGuid(), [StaffRole.Engineer]);

        public static async Task<Harness> CreateAsync()
        {
            var database = await LocalDbTestDatabase.CreateAsync();
            try
            {
                var options = new DbContextOptionsBuilder<PegasusDbContext>()
                    .UseSqlServer(database.ConnectionString)
                    .Options;
                var factory = new PooledDbContextFactory<PegasusDbContext>(options);
                var timeProvider = new CaseDataCompletenessPersistenceTests.MutableTimeProvider(
                    StartUtc);
                var receiptId = Guid.NewGuid();
                await SeedAsync(factory, receiptId);
                var acceptanceStore = new EfCaseAcceptanceStore(factory, timeProvider, []);
                var workflowStore = new EfCaseWorkflowStore(factory, timeProvider);
                var repairSpecifications = new EfRepairSpecificationStore(factory, timeProvider);
                var contentRoot = Path.Combine(Path.GetTempPath(), $"pegasus-report-{Guid.NewGuid():N}");
                var reportStore = new EfAssessmentReportStore(
                    factory,
                    new LocalDocumentContentStore(contentRoot),
                    timeProvider);
                return new(
                    database,
                    factory,
                    receiptId,
                    new AcceptIntake(
                        acceptanceStore,
                        new FixedConfiguration(),
                        new EfProviderInspectionModeStore(factory)),
                    new AcquireCaseEditLease(workflowStore),
                    new SaveAssessment(
                        new EfCaseAssessmentStore(factory, timeProvider, repairSpecifications)),
                    new EfAiWorkRequestStore(factory, timeProvider),
                    repairSpecifications,
                    timeProvider,
                    contentRoot,
                    reportStore);
            }
            catch
            {
                await database.DisposeAsync();
                throw;
            }
        }

        public void Advance(TimeSpan interval) => timeProvider.Advance(interval);

        public EfAssessmentReportStore CreateReportStore(IDocumentContentStore contentStore) =>
            new(Factory, contentStore, timeProvider);

        public LocalDocumentContentStore NewLocalContentStore() =>
            new(contentRoot);

        public Task<CaseAcceptanceOutcome> AcceptAsync(string operationKey) =>
            acceptIntake.ExecuteAsync(
                new(
                    ReceiptId,
                    0,
                    ActionActor.Staff(Guid.NewGuid(), [StaffRole.User]),
                    operationKey,
                    "Accepted assessment fixture case",
                    CaseType.Inspection,
                    "QDOS",
                    new(true, true, false, false),
                    AcceptedInspectionDeadline: new DateOnly(2031, 5, 20)),
                CancellationToken.None);

        public async Task<RepairSpecificationVersion> AcceptReportSpecificationAsync(Guid caseId)
        {
            var source = new RepairSpecificationSource(
                RepairSpecificationSourceRoute.Manual,
                "case://repair-spec/report-source",
                "source-v1",
                new string('a', 64));
            var basis = new RepairCalculationBasis(
                150m, 50m, 20m, 5m, true, 45m, 270m, "external-estimate/v1");
            var lines = new EstimateLineInput[]
            {
                new("new_part", null, "Front bumper", null, 50m, false, null, null,
                    "confirmed", "case", "Report fixture"),
            };
            var draftLease = await AcquireLeaseAsync(
                caseId, 0, EngineerActor, "report-spec-draft-lease");
            var draft = await RepairSpecifications.StartDraftAsync(
                new(
                    caseId,
                    draftLease.Version,
                    source,
                    EngineerActor,
                    "report-spec-draft",
                    "Create the selected report estimate.",
                    draftLease.Token,
                    Lines: lines),
                CancellationToken.None);
            var acceptLease = await AcquireLeaseAsync(
                caseId, 1, EngineerActor, "report-spec-accept-lease");
            return await RepairSpecifications.AcceptAsync(
                new(
                    caseId,
                    acceptLease.Version,
                    draft.SpecificationId,
                    draft.Version,
                    source,
                    basis,
                    EngineerActor,
                    "report-spec-accept",
                    "Accept the selected report estimate.",
                    acceptLease.Token),
                CancellationToken.None);
        }

        public Task<CaseEditLease> AcquireLeaseAsync(
            Guid caseId,
            long version,
            ActionActor actor,
            string operationKey) => acquireLease.ExecuteAsync(
            new(caseId, version, actor, operationKey),
            CancellationToken.None);

        public async ValueTask DisposeAsync()
        {
            await database.DisposeAsync();
            if (Directory.Exists(contentRoot))
            {
                Directory.Delete(contentRoot, recursive: true);
            }
        }

        private static async Task SeedAsync(
            IDbContextFactory<PegasusDbContext> factory,
            Guid receiptId)
        {
            await using var context = await factory.CreateDbContextAsync();
            var organizationId = Guid.NewGuid();
            var lineageId = Guid.NewGuid();
            var principalId = Guid.NewGuid();
            var sourceHash = new string('d', 64);
            var fieldsJson =
                """{"version":1,"data":[{"name":"Claimant name","suggestedValue":"Mrs Jane Example","candidates":[{"value":"Mrs Jane Example","source":"pdf_content","sourceLabel":"instructions.pdf"}],"isDefaulted":false,"hasConflict":false},{"name":"Claim number","suggestedValue":"ABC/DEF/12345/1","candidates":[{"value":"ABC/DEF/12345/1","source":"pdf_content","sourceLabel":"instructions.pdf"}],"isDefaulted":false,"hasConflict":false},{"name":"Vehicle registration","suggestedValue":"AB12 CDE","candidates":[{"value":"AB12 CDE","source":"pdf_content","sourceLabel":"instructions.pdf"}],"isDefaulted":false,"hasConflict":false},{"name":"Date of incident","suggestedValue":"2031-04-01","candidates":[{"value":"2031-04-01","source":"pdf_content","sourceLabel":"instructions.pdf"}],"isDefaulted":false,"hasConflict":false},{"name":"Inspection address","suggestedValue":"1 Test Street, London","candidates":[{"value":"1 Test Street, London","source":"pdf_content","sourceLabel":"instructions.pdf"}],"isDefaulted":false,"hasConflict":false},{"name":"Inspection date","suggestedValue":"2031-05-20","candidates":[{"value":"2031-05-20","source":"pdf_content","sourceLabel":"instructions.pdf"}],"isDefaulted":false,"hasConflict":false}]}""";
            var emptyEnvelope = """{"version":1,"data":[]}""";

            await context.Database.ExecuteSqlInterpolatedAsync(
                $"INSERT INTO Organizations (Id, Name, Version) VALUES ({organizationId}, {"Assessment fixture provider"}, {0L})");
            await context.Database.ExecuteSqlInterpolatedAsync(
                $"INSERT INTO PrincipalSequenceLineages (Id, CreatedAtUtc) VALUES ({lineageId}, {StartUtc})");
            await context.Database.ExecuteSqlInterpolatedAsync(
                $"INSERT INTO Principals (Id, OrganizationId, Code, SequenceLineageId, IsActive, InspectionMode, Version) VALUES ({principalId}, {organizationId}, {"QDOS"}, {lineageId}, {true}, {"image_based_assessment"}, {0L})");
            await context.Database.ExecuteSqlInterpolatedAsync(
                $"INSERT INTO IntakeReceipts (Id, SourceFileName, MediaType, SourceLength, SourceHash, SourceChannel, ExternalReceiptToken, ReceivedAtUtc, ProcessedAtUtc, SourceReaderKey, SourceReaderVersion, ExtractionPolicyKey, ExtractionPolicyVersion, Version, Decision, DecisionReason, EvidenceJson, FieldsJson, OcrCandidatesJson) VALUES ({receiptId}, {"assessment.eml"}, {"message/rfc822"}, {100L}, {sourceHash}, {"mailbox"}, {"assessment-item-1"}, {StartUtc}, {StartUtc}, {"fixture-reader"}, {"1"}, {"qdos_instruction"}, {1}, {0L}, {"case_created"}, {"Ready fixture"}, {emptyEnvelope}, {fieldsJson}, {emptyEnvelope})");
            await context.Database.ExecuteSqlInterpolatedAsync(
                $"INSERT INTO InstructionDrafts (IntakeReceiptId, SuggestedPrincipalCode, ClaimantName, ClaimNumber, VehicleRegistration, DateOfIncident, InspectionAddress, InspectionDate) VALUES ({receiptId}, {"QDOS"}, {"Mrs Jane Example"}, {"ABC/DEF/12345/1"}, {"AB12CDE"}, {new DateOnly(2031, 4, 1)}, {"1 Test Street, London"}, {new DateOnly(2031, 5, 20)})");
            await context.Database.ExecuteSqlInterpolatedAsync(
                $"INSERT INTO IntakeMailRouteDecisions (IntakeReceiptId, Disposition, RouteOwnerCode, RouteKind, WorkProviderCode, PredicatesJson, Reason, PolicyKey, PolicyVersion, TransportIdentitiesJson, OriginalIdentitiesJson) VALUES ({receiptId}, {"accepted"}, {"QDOS"}, {"direct_work_provider"}, {"QDOS"}, {emptyEnvelope}, {"Accepted QDOS route"}, {"qdos_mail_route"}, {3}, {emptyEnvelope}, {emptyEnvelope})");
        }
    }

    private sealed class FixedConfiguration : ICaseWorkflowConfiguration
    {
        private static readonly CaseWorkflowConfiguration Configuration = new(
            true,
            true,
            true,
            true,
            "case-workflow",
            1);

        public Task<CaseWorkflowConfiguration> GetCurrentAsync(
            CancellationToken cancellationToken) => Task.FromResult(Configuration);
    }

    private sealed class FailFirstDocumentContentStore(IDocumentContentStore inner) : IDocumentContentStore
    {
        private int writes;

        public Task StoreAsync(
            Guid caseId,
            string caseReference,
            Guid versionId,
            ReadOnlyMemory<byte> content,
            string expectedSha256,
            CancellationToken cancellationToken)
        {
            if (Interlocked.Increment(ref writes) == 1)
            {
                throw new IOException("Simulated content-store failure after metadata commit.");
            }

            return inner.StoreAsync(
                caseId,
                caseReference,
                versionId,
                content,
                expectedSha256,
                cancellationToken);
        }

        public Task<Stream> OpenReadAsync(
            Guid caseId,
            string caseReference,
            Guid versionId,
            string expectedSha256,
            long expectedLength,
            CancellationToken cancellationToken) =>
            inner.OpenReadAsync(
                caseId,
                caseReference,
                versionId,
                expectedSha256,
                expectedLength,
                cancellationToken);

        public Task DeleteAsync(
            Guid caseId,
            string caseReference,
            Guid versionId,
            CancellationToken cancellationToken) =>
            inner.DeleteAsync(caseId, caseReference, versionId, cancellationToken);
    }
}
