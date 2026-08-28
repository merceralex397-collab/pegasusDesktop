using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Pegasus.Core.Cases;
using Pegasus.Core.Custody;
using Pegasus.Core.Identity;
using Pegasus.Core.Lifecycle;
using Pegasus.Core.Reports;
using Pegasus.Core.Tasks;
using Pegasus.Core.Workflow;

namespace Pegasus.Infrastructure.Persistence;

public sealed class EfCaseWorkflowStore(
    IDbContextFactory<PegasusDbContext> contextFactory,
    TimeProvider timeProvider) : ICaseWorkflowStore, IAutoLinkReportEvidenceStore, ICaseDueWorkStore, ICaseArchiveStore, ICaseArchiveReadinessQueries
{
    private static readonly TimeSpan EditLeaseDuration = TimeSpan.FromMinutes(5);
    private const string ClaimLeaseOperationKind = "claim";
    private const string RenewLeaseOperationKind = "renew";
    private const string ReleaseLeaseOperationKind = "release";

    public async Task<CaseWorkflowRecord?> GetAsync(Guid caseId, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await WorkflowQuery(context, tracking: false)
            .SingleOrDefaultAsync(item => item.CaseId == caseId, cancellationToken);
        return entity is null ? null : Map(entity);
    }
    public async Task<bool> HasCaseMutationOperationAsync(
        Guid caseId,
        string operationKey,
        CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var normalizedOperationKey = operationKey.Trim();
        return await context.CaseWorkflowEvents
            .AsNoTracking()
            .AnyAsync(
                item => item.CaseId == caseId
                    && item.OperationKey == normalizedOperationKey,
                cancellationToken);
    }

    public async Task<CaseArchiveReadiness> GetArchiveReadinessAsync(
        Guid caseId,
        CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var caseEntity = await context.Cases
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == caseId, cancellationToken)
            ?? throw new KeyNotFoundException($"Case '{caseId}' was not found.");
        return await LoadArchiveReadinessAsync(context, caseEntity, cancellationToken);
    }


    public async Task<bool> HasOperationAsync(
        Guid caseId,
        string operationKey,
        CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var normalizedOperationKey = operationKey.Trim();
        return await context.CaseWorkflowEvents.AsNoTracking().AnyAsync(
                item => item.CaseId == caseId
                    && item.OperationKey == normalizedOperationKey,
                cancellationToken)
            || await context.CaseEditLeaseOperations.AsNoTracking().AnyAsync(
                item => item.CaseId == caseId
                    && item.OperationKey == normalizedOperationKey,
                cancellationToken);
    }

    async Task<CaseDueWork?> ICaseDueWorkQueries.GetAsync(Guid caseId, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await context.CaseDueWork.AsNoTracking()
            .Include(item => item.Workflow)
            .ThenInclude(workflow => workflow.Case)
            .SingleOrDefaultAsync(item => item.CaseId == caseId, cancellationToken);
        return entity is null ? null : Map(entity);
    }

    public async Task<IReadOnlyList<CaseDueWork>> GetDueAsync(
        DateTimeOffset asOfUtc,
        int maximumResults,
        CancellationToken cancellationToken)
    {
        if (maximumResults is < 1 or > 500)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumResults));
        }

        var asOfUtcTicks = asOfUtc.ToUniversalTime().UtcDateTime.Ticks;
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var entities = await context.CaseDueWork.AsNoTracking()
            .Include(item => item.Workflow)
            .ThenInclude(workflow => workflow.Case)
            .Where(item => item.Workflow.ArchivedAtUtc == null
                && item.State == nameof(CaseDueWorkState.Scheduled)
                && item.NextChaseAtUtc != null
                && item.NextChaseAtUtcTicks != null
                && item.NextChaseAtUtcTicks <= asOfUtcTicks)
            .OrderBy(item => item.NextChaseAtUtcTicks)
            .ThenBy(item => item.CaseId)
            .Take(maximumResults)
            .ToArrayAsync(cancellationToken);
        return entities.Select(Map).ToArray();
    }

    public async Task<CaseEditLease> ClaimAsync(
        ClaimCaseEditLeaseRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await context.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);
        await AcquireWorkflowMutationLockAsync(
            context,
            request.CaseId,
            cancellationToken);
        var workflow = await context.CaseWorkflows.SingleOrDefaultAsync(
                item => item.CaseId == request.CaseId,
                cancellationToken)
            ?? throw new KeyNotFoundException($"Case '{request.CaseId}' was not found.");
        StaffAuthorization.Require(request.Actor, StaffAccessRight.PerformCasework);

        var operationKey = request.OperationKey.Trim();
        var requestHash = LeaseOperationRequestHash(
            ClaimLeaseOperationKind,
            request.CaseId,
            request.ExpectedVersion,
            request.Actor,
            operationKey,
            leaseToken: null);
        var now = timeProvider.GetUtcNow();
        var replay = await FindLeaseOperationAsync(
            context,
            request.CaseId,
            operationKey,
            cancellationToken);
        if (replay is not null)
        {
            EnsureLeaseReplay(
                replay,
                ClaimLeaseOperationKind,
                requestHash,
                request.CaseId,
                operationKey);
            return ReadLeaseReplayOrThrow(
                workflow,
                replay,
                request.Actor,
                operationKey,
                now);
        }

        ArchivedCaseGuard.RequireNotArchived(workflow);
        RequireVersion(workflow, request.ExpectedVersion);
        if (CaseEditAuthority.IsHeld(workflow.EditLeaseExpiresAtUtc, now))
        {
            throw new CaseEditLeaseConflictException(request.CaseId, workflow.Version);
        }

        ClearLease(workflow);
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        var tokenHash = Hash(token);
        var expiresAtUtc = now + EditLeaseDuration;
        workflow.EditLeaseToken = token;
        workflow.EditLeaseTokenHash = tokenHash;
        workflow.EditLeaseRequestHash = requestHash;
        workflow.EditLeaseHolder = request.Actor.SubjectId;
        workflow.EditLeaseOperationKey = operationKey;
        workflow.EditLeaseExpiresAtUtc = expiresAtUtc;
        AddLeaseOperation(
            context,
            workflow,
            request.Actor,
            operationKey,
            ClaimLeaseOperationKind,
            requestHash,
            now,
            workflow.Version,
            expiresAtUtc,
            tokenHash);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new(
            request.CaseId,
            token,
            request.Actor.SubjectId,
            workflow.Version,
            expiresAtUtc);
    }

    public async Task<CaseEditLease> RenewAsync(
        RenewCaseEditLeaseRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await context.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);
        await AcquireWorkflowMutationLockAsync(
            context,
            request.CaseId,
            cancellationToken);
        var workflow = await context.CaseWorkflows.SingleOrDefaultAsync(
                item => item.CaseId == request.CaseId,
                cancellationToken)
            ?? throw new KeyNotFoundException($"Case '{request.CaseId}' was not found.");
        StaffAuthorization.Require(request.Actor, StaffAccessRight.PerformCasework);

        var operationKey = request.OperationKey.Trim();
        var requestHash = LeaseOperationRequestHash(
            RenewLeaseOperationKind,
            request.CaseId,
            request.ExpectedVersion,
            request.Actor,
            operationKey,
            request.LeaseToken);
        var now = timeProvider.GetUtcNow();
        var replay = await FindLeaseOperationAsync(
            context,
            request.CaseId,
            operationKey,
            cancellationToken);
        if (replay is not null)
        {
            EnsureLeaseReplay(
                replay,
                RenewLeaseOperationKind,
                requestHash,
                request.CaseId,
                operationKey);
            return ReadLeaseReplayOrThrow(
                workflow,
                replay,
                request.Actor,
                operationKey,
                now);
        }

        ArchivedCaseGuard.RequireNotArchived(workflow);
        RequireVersion(workflow, request.ExpectedVersion);
        RequireLease(workflow, request.Actor, request.LeaseToken, now);
        var tokenHash = Hash(request.LeaseToken);
        var expiresAtUtc = now + EditLeaseDuration;
        workflow.EditLeaseToken = request.LeaseToken;
        workflow.EditLeaseTokenHash = tokenHash;
        workflow.EditLeaseRequestHash = requestHash;
        workflow.EditLeaseOperationKey = operationKey;
        workflow.EditLeaseExpiresAtUtc = expiresAtUtc;
        AddLeaseOperation(
            context,
            workflow,
            request.Actor,
            operationKey,
            RenewLeaseOperationKind,
            requestHash,
            now,
            workflow.Version,
            expiresAtUtc,
            tokenHash);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new(
            request.CaseId,
            request.LeaseToken,
            request.Actor.SubjectId,
            workflow.Version,
            expiresAtUtc);
    }

    public async Task ReleaseAsync(
        ReleaseCaseEditLeaseRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await context.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);
        await AcquireWorkflowMutationLockAsync(
            context,
            request.CaseId,
            cancellationToken);
        var workflow = await context.CaseWorkflows.SingleOrDefaultAsync(
                item => item.CaseId == request.CaseId,
                cancellationToken)
            ?? throw new KeyNotFoundException($"Case '{request.CaseId}' was not found.");
        StaffAuthorization.Require(request.Actor, StaffAccessRight.PerformCasework);

        var operationKey = request.OperationKey.Trim();
        var requestHash = LeaseOperationRequestHash(
            ReleaseLeaseOperationKind,
            request.CaseId,
            expectedVersion: null,
            request.Actor,
            operationKey,
            request.LeaseToken);
        var replay = await FindLeaseOperationAsync(
            context,
            request.CaseId,
            operationKey,
            cancellationToken);
        if (replay is not null)
        {
            EnsureLeaseReplay(
                replay,
                ReleaseLeaseOperationKind,
                requestHash,
                request.CaseId,
                operationKey);
            return;
        }

        ArchivedCaseGuard.RequireNotArchived(workflow);
        var now = timeProvider.GetUtcNow();
        RequireLease(workflow, request.Actor, request.LeaseToken, now);
        var resultVersion = workflow.Version;
        ClearLease(workflow);
        AddLeaseOperation(
            context,
            workflow,
            request.Actor,
            operationKey,
            ReleaseLeaseOperationKind,
            requestHash,
            now,
            resultVersion,
            resultExpiresAtUtc: null,
            resultTokenHash: null);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public Task<CaseWorkflowRecord> ChangeStateAsync(
        CaseMutationRequest request,
        CaseLifecycleState targetState,
        CancellationToken cancellationToken) =>
        MutateAsync(request, $"state_{targetState}", (context, workflow, now) =>
        {
            workflow.State = targetState.ToString();
            return Task.CompletedTask;
        }, cancellationToken, targetState.ToString());

    public Task<CaseWorkflowRecord> HoldAsync(PutCaseOnHoldRequest request, CancellationToken cancellationToken) =>
        MutateAsync(request, "case_held", (context, workflow, now) =>
        {
            workflow.PreHoldState = workflow.State;
            var due = workflow.DueWork;
            if (workflow.State == nameof(CaseLifecycleState.NotReady))
            {
                if (due is null
                    || due.State != nameof(CaseDueWorkState.Scheduled)
                    || due.NextChaseAtUtc is null)
                {
                    throw new InvalidOperationException(
                        "A Not ready case must have scheduled due work before it can be held.");
                }

                due.State = nameof(CaseDueWorkState.Held);
                due.HeldAtUtc = now;
                due.RemainingChaseIntervalTicks =
                    CaseChaseSchedule.RemainingInterval(due.NextChaseAtUtc.Value, now).Ticks;
                due.NextChaseAtUtc = null;
                due.Version++;
            }

            workflow.State = nameof(CaseLifecycleState.Held);
            return Task.CompletedTask;
        }, cancellationToken);

    public Task<CaseWorkflowRecord> ReleaseHoldAsync(CaseMutationRequest request, CancellationToken cancellationToken) =>
        MutateAsync(request, "case_hold_released", (context, workflow, now) =>
        {
            if (!Enum.TryParse<CaseLifecycleState>(workflow.PreHoldState, out var previousState)
                || previousState is not (
                    CaseLifecycleState.NotReady
                    or CaseLifecycleState.Review
                    or CaseLifecycleState.ReportPreparation
                    or CaseLifecycleState.PostReport))
            {
                throw new InvalidOperationException(
                    "The case hold does not retain a valid previous lifecycle state.");
            }

            var due = workflow.DueWork;
            if (previousState == CaseLifecycleState.NotReady)
            {
                if (due is null
                    || due.State != nameof(CaseDueWorkState.Held)
                    || due.HeldAtUtc is null
                    || due.RemainingChaseIntervalTicks is not { } remainingTicks
                    || remainingTicks < 0)
                {
                    throw new InvalidOperationException(
                        "The held case does not retain a valid chase interval.");
                }

                due.State = nameof(CaseDueWorkState.Scheduled);
                due.NextChaseAtUtc =
                    CaseChaseSchedule.ResumeAt(now, TimeSpan.FromTicks(remainingTicks));
                due.HeldAtUtc = null;
                due.RemainingChaseIntervalTicks = null;
                due.Version++;
            }
            else if (due?.State == nameof(CaseDueWorkState.Held))
            {
                throw new InvalidOperationException(
                    "Held due work is inconsistent with the retained lifecycle state.");
            }

            workflow.State = previousState.ToString();
            workflow.PreHoldState = null;
            return Task.CompletedTask;
        }, cancellationToken);

    public Task<CaseWorkflowRecord> ReturnToReviewAsync(ReturnCaseToReviewRequest request, CancellationToken cancellationToken) =>
        MutateAsync(request, "case_returned_to_review", (context, workflow, now) =>
        {
            workflow.State = nameof(CaseLifecycleState.Review);
            CaseChaseState.Stop(workflow);
            return Task.CompletedTask;
        }, cancellationToken);

    public Task<CaseWorkflowRecord> AssignEngineerAsync(AssignCaseEngineerRequest request, CancellationToken cancellationToken) =>
        MutateAsync(request, "case_engineer_assigned", (context, workflow, now) =>
        {
            workflow.AssignedEngineerId = request.EngineerId;
            return Task.CompletedTask;
        }, cancellationToken);

    public Task<CaseWorkflowRecord> RecordReportApprovalAsync(
        RecordCaseReportApprovalRequest request,
        CancellationToken cancellationToken) =>
        MutateAsync(request, "case_report_approved", async (context, workflow, now) =>
        {
            if (workflow.State != nameof(CaseLifecycleState.ReportPreparation))
            {
                throw new InvalidOperationException(
                    "A report can be approved only while Report preparation is active.");
            }

            var approval = request.Approval;
            var reportVersionId = approval.ReportVersionId
                ?? throw new InvalidOperationException(
                    "An immutable report version identity is required for report approval.");
            var ledger = await context.CaseReportVersionLedgers
                .Include(item => item.ReportVersion)
                .ThenInclude(item => item.Artifacts)
                .Include(item => item.Approval)
                .SingleOrDefaultAsync(
                    item => item.ReportVersionId == reportVersionId
                        && item.CaseId == workflow.CaseId,
                    cancellationToken);
            if (ledger is null)
            {
                throw new InvalidOperationException(
                    "The selected report version does not belong to this case.");
            }
            if (ledger.ReportVersion.State != nameof(AssessmentReportGenerationState.Generated))
            {
                throw new InvalidOperationException(
                    "Only a generated report version can be approved.");
            }
            if (ledger.ApprovalId is not null)
            {
                throw new InvalidOperationException(
                    "The selected report version already has an approval.");
            }
            var matchingArtifacts = ledger.ReportVersion.Artifacts
                .Where(item => ArtifactMatches(item, approval.ArtifactIdentity, approval.ArtifactSha256))
                .ToArray();
            if (matchingArtifacts.Length != 1)
            {
                throw new InvalidOperationException(
                    "The approved artifact does not match exactly one artifact of the selected report version.");
            }

            var entity = new CaseReportApprovalEntity
            {
                Id = approval.ApprovalId,
                CaseId = workflow.CaseId,
                ArtifactIdentity = approval.ArtifactIdentity,
                ArtifactSha256 = approval.ArtifactSha256.ToLowerInvariant(),
                ApprovedByKind = request.Actor.Kind.ToString(),
                ApprovedBySubjectId = request.Actor.SubjectId,
                ApprovedByRolesJson = RolesJson(request.Actor),
                ApprovedAtUtc = now,
                AssociationStatus = "Authoritative",
                AssociationStatusReason = "Approval is bound to the selected immutable report version."
            };
            context.CaseReportApprovals.Add(entity);
            workflow.ReportApprovalId = approval.ApprovalId;
            workflow.ReportApproval = entity;
            ledger.ApprovalId = entity.Id;
            ledger.Approval = entity;
            ledger.CorrectionReason = ledger.ReportVersion.PredecessorId is null
                ? null
                : request.Reason.Trim();
            ledger.Version++;
            AddAssociationHistory(
                context,
                ledger,
                evidenceId: null,
                approvalId: entity.Id,
                beforeReportVersionId: null,
                afterReportVersionId: ledger.ReportVersionId,
                action: "approved",
                request.Actor,
                request.Reason.Trim(),
                request.OperationKey.Trim(),
                now);
            return;
        }, cancellationToken);

    public Task<CaseWorkflowRecord> LinkReportEvidenceAsync(
        LinkReportEvidenceRequest request,
        CancellationToken cancellationToken) =>
        MutateAsync(request, "report_evidence_linked", async (context, workflow, now) =>
        {
            var evaluation = await EvaluateReportEvidenceLinkAsync(
                context,
                workflow,
                request.EvidenceId,
                request.ReportVersionId,
                now,
                cancellationToken);
            if (evaluation.Evidence is null)
            {
                throw new InvalidOperationException(evaluation.Message);
            }

            ApplyReportEvidenceLink(workflow, evaluation.Evidence, request.Actor, now);
            if (evaluation.Ledger is not null)
            {
                evaluation.Ledger.CurrentEvidenceId = evaluation.Evidence.Id;
                evaluation.Ledger.CurrentEvidence = evaluation.Evidence;
                evaluation.Ledger.Version++;
                AddAssociationHistory(
                    context,
                    evaluation.Ledger,
                    evaluation.Evidence.Id,
                    approvalId: evaluation.Ledger.ApprovalId,
                    beforeReportVersionId: null,
                    afterReportVersionId: evaluation.Ledger.ReportVersionId,
                    "linked",
                    request.Actor,
                    request.Reason.Trim(),
                    request.OperationKey.Trim(),
                    now);
            }
        }, cancellationToken);

    public async Task<AutoLinkReportEvidenceResult> TryAutoLinkAsync(
        AutoLinkReportEvidenceRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Actor);
        if (request.CaseId == Guid.Empty)
        {
            throw new ArgumentException("A case identifier is required.", nameof(request));
        }
        if (request.EvidenceId == Guid.Empty)
        {
            throw new ArgumentException("A retained Sent-evidence identifier is required.", nameof(request));
        }
        ArgumentException.ThrowIfNullOrWhiteSpace(request.OperationKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Reason);
        if (request.OperationKey.Length > 100)
        {
            throw new ArgumentException(
                "The auto-link operation key cannot exceed 100 characters.",
                nameof(request));
        }
        if (request.Reason.Length > 500)
        {
            throw new ArgumentException(
                "The auto-link reason cannot exceed 500 characters.",
                nameof(request));
        }
        StaffAuthorization.Require(request.Actor, StaffAccessRight.ExecuteSystemWork);
        if (request.ReportVersionId is null)
        {
            return AutoLinkNotLinked("report_version_required");
        }

        try
        {
            return await TryAutoLinkOnceAsync(request, cancellationToken);
        }
        catch (Exception exception) when (IsConcurrencyFailure(exception))
        {
            return AutoLinkNotLinked("concurrency_conflict");
        }
    }

    public Task<CaseWorkflowRecord> UnlinkReportEvidenceAsync(
        UnlinkReportEvidenceRequest request,
        CancellationToken cancellationToken) =>
        MutateAsync(request, "report_evidence_unlinked", (context, workflow, now) =>
        {
            if (request.ReportVersionId is null)
            {
                throw new InvalidOperationException(
                    "An immutable report version identity is required to unlink report-Sent evidence.");
            }

            var ledger = workflow.ReportVersionLedgers.SingleOrDefault(
                item => item.ReportVersionId == request.ReportVersionId.Value);
            if (ledger is null || ledger.CurrentEvidenceId != request.EvidenceId)
            {
                throw new InvalidOperationException(
                    "The selected report version is not currently associated with the selected evidence.");
            }

            var evidence = ledger.CurrentEvidence;
            if (evidence is null
                || evidence.Id != request.EvidenceId
                || evidence.CaseId != workflow.CaseId)
            {
                throw new InvalidOperationException(
                    "The selected report-Sent evidence is not the case's current association.");
            }

            var formerCaseId = evidence.CaseId;
            var formerLinkedAtUtc = evidence.LinkedAtUtc;
            var formerLinkedByKind = evidence.LinkedByKind;
            var formerLinkedBySubjectId = evidence.LinkedBySubjectId;
            var formerLinkedByRolesJson = evidence.LinkedByRolesJson;
            // The evidence row's association fields describe its mutable current candidate
            // state. The append-only history row below is the authoritative former-association
            // record and is projected from the ledger for audit and later relink.
            evidence.CaseId = null;
            evidence.LinkedAtUtc = null;
            evidence.LinkedByKind = null;
            evidence.LinkedBySubjectId = null;
            evidence.LinkedByRolesJson = null;
            evidence.AssociationStatus = "Unresolved";
            evidence.AssociationStatusReason = "The previous report-version association was explicitly unlinked; source evidence is retained.";
            ledger.CurrentEvidenceId = null;
            ledger.CurrentEvidence = null;
            ledger.Version++;
            AddAssociationHistory(
                context,
                ledger,
                evidence.Id,
                ledger.ApprovalId,
                ledger.ReportVersionId,
                afterReportVersionId: null,
                "unlinked",
                request.Actor,
                request.Reason.Trim(),
                request.OperationKey.Trim(),
                now,
                formerCaseId,
                formerLinkedAtUtc,
                formerLinkedByKind,
                formerLinkedBySubjectId,
                formerLinkedByRolesJson);
            if (workflow.ReportSentEvidenceId == request.EvidenceId)
            {
                workflow.ReportSentEvidenceId = null;
                workflow.ReportSentEvidence = null;
                if (workflow.State == nameof(CaseLifecycleState.PostReport))
                {
                    workflow.State = nameof(CaseLifecycleState.ReportPreparation);
                }
            }

            return Task.CompletedTask;
        }, cancellationToken);

    public Task<CaseWorkflowRecord> CloseAsync(
        CloseCaseRequest request,
        CancellationToken cancellationToken) =>
        MutateAsync(request, $"case_closed_{request.Outcome}", async (context, workflow, now) =>
        {
            await CaseTerminalReadinessGuard.RequireNoOpenTasksAsync(
                context,
                workflow.CaseId,
                cancellationToken);
            if (request.Outcome == CaseClosureOutcome.CreatedInError)
            {
                throw new InvalidOperationException(
                    "Created in error requires the atomic corrected-principal replacement action.");
            }
            if (request.Outcome == CaseClosureOutcome.SourceEmailUnlinked)
            {
                throw new InvalidOperationException(
                    "Cancelling on unlink requires unlinking the email that created the case.");
            }
            workflow.State = request.Outcome.ToString();
            workflow.ClosureOutcome = request.Outcome.ToString();
            CaseChaseState.Stop(workflow);
        }, cancellationToken);

    public Task<CaseWorkflowRecord> ReopenAsync(ReopenCaseRequest request, CancellationToken cancellationToken) =>
        MutateAsync(request, $"case_reopened_{request.Destination}", (context, workflow, now) =>
        {
            workflow.State = request.Destination.ToString();
            workflow.ClosureOutcome = null;
            if (request.Destination == CaseReopenDestination.NotReady)
            {
                var due = workflow.DueWork;
                if (due is null)
                {
                    context.CaseDueWork.Add(new()
                    {
                        CaseId = workflow.CaseId,
                        Workflow = workflow,
                        MissingMaterialReason = request.Reason,
                        State = nameof(CaseDueWorkState.Scheduled),
                        NextChaseAtUtc = CaseChaseSchedule.FirstChaseAt(now),
                        Version = 0
                    });
                }
                else
                {
                    due.State = nameof(CaseDueWorkState.Scheduled);
                    due.NextChaseAtUtc = CaseChaseSchedule.FirstChaseAt(now);
                    due.Version++;
                }
            }
            else
            {
                CaseChaseState.Stop(workflow);
            }
            return Task.CompletedTask;
        }, cancellationToken);

    public Task<CaseWorkflowRecord> ArchiveAsync(
        ArchiveCaseRequest request,
        CancellationToken cancellationToken) =>
        MutateAsync(request, "case_archived", async (context, workflow, now) =>
        {
            if (!CaseLifecycleRules.IsTerminal(
                    Enum.Parse<CaseLifecycleState>(workflow.State)))
            {
                throw new InvalidOperationException(
                    "Only a terminal case can be archived.");
            }
            await CaseTerminalReadinessGuard.RequireNoOpenTasksAsync(
                context,
                workflow.CaseId,
                cancellationToken);
            var readiness = await LoadArchiveReadinessAsync(
                context,
                workflow.Case,
                cancellationToken);
            if (!readiness.IsCustodyConfirmed)
            {
                throw new InvalidOperationException(
                    "A case can be archived only after its required custody is confirmed.");
            }
            if (readiness.HasBlockingExternalWork)
            {
                throw new InvalidOperationException(
                    "A case cannot be archived while required durable work is incomplete or unrecognized work exists.");
            }

            workflow.ArchivedAtUtc = now;
            workflow.ArchivedByKind = request.Actor.Kind.ToString();
            workflow.ArchivedBySubjectId = request.Actor.SubjectId;
            workflow.ArchivedByRolesJson = RolesJson(request.Actor);
            workflow.ArchiveReason = request.Reason.Trim();
        }, cancellationToken);

    public async Task<CaseDueWork> RecordManualChaseAsync(
        ManualChaseRecord request,
        CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        await AcquireWorkflowMutationLockAsync(
            context,
            request.CaseId,
            cancellationToken);
        if (await context.CaseEditLeaseOperations.AsNoTracking().AnyAsync(
                item => item.CaseId == request.CaseId
                    && item.OperationKey == request.OperationKey.Trim(),
                cancellationToken))
        {
            throw new CaseOperationConflictException(
                request.CaseId,
                request.OperationKey.Trim());
        }

        var hash = RequestHash(request);
        var replay = await context.CaseManualChases.AsNoTracking()
            .SingleOrDefaultAsync(item => item.CaseId == request.CaseId && item.OperationKey == request.OperationKey, cancellationToken);
        if (replay is not null)
        {
            if (!CryptographicOperations.FixedTimeEquals(Convert.FromHexString(replay.RequestHash), Convert.FromHexString(hash)))
            {
                throw new CaseOperationConflictException(request.CaseId, request.OperationKey);
            }
            var replayedDue = await context.CaseDueWork.AsNoTracking()
                .Include(item => item.Workflow)
                .ThenInclude(workflow => workflow.Case)
                .SingleAsync(item => item.CaseId == request.CaseId, cancellationToken);
            return Map(replayedDue);
        }

        var workflow = await context.CaseWorkflows.SingleOrDefaultAsync(item => item.CaseId == request.CaseId, cancellationToken)
            ?? throw new KeyNotFoundException($"Case '{request.CaseId}' was not found.");
        StaffAuthorization.Require(request.Actor, StaffAccessRight.PerformCasework);
        ArchivedCaseGuard.RequireMutable(workflow);
        RequireVersion(workflow, request.ExpectedCaseVersion);
        RequireLease(workflow, request.Actor, request.EditLeaseToken, timeProvider.GetUtcNow());
        var due = await context.CaseDueWork
                .Include(item => item.Workflow)
                .ThenInclude(workflow => workflow.Case)
                .SingleOrDefaultAsync(item => item.CaseId == request.CaseId, cancellationToken)
            ?? throw new InvalidOperationException("The case has no due work to chase.");
        if (due.State != nameof(CaseDueWorkState.Scheduled))
        {
            throw new InvalidOperationException("Only scheduled due work can be chased.");
        }
        due.MostRecentChannel = request.Channel;
        due.MostRecentOutcome = request.Outcome;
        due.MostRecentNote = request.Note;
        due.NextChaseAtUtc = CaseChaseSchedule.NextChaseAt(request.AttemptedAtUtc);
        due.Version++;
        workflow.Version++;
        ClearLease(workflow);
        context.CaseManualChases.Add(new()
        {
            Id = Guid.NewGuid(),
            CaseId = request.CaseId,
            OperationKey = request.OperationKey,
            RequestHash = hash,
            ActorKind = request.Actor.Kind.ToString(),
            ActorSubjectId = request.Actor.SubjectId,
            ActorRolesJson = RolesJson(request.Actor),
            Reason = request.Reason,
            Channel = request.Channel,
            TargetPartyOrAddress = request.TargetPartyOrAddress,
            AttemptedAtUtc = request.AttemptedAtUtc,
            Outcome = request.Outcome,
            Note = request.Note,
            ResultingVersion = workflow.Version
        });
        AddEvent(
            context,
            workflow,
            request.Actor,
            request.OperationKey.Trim(),
            request.Reason.Trim(),
            hash,
            "manual_chase_recorded",
            workflow.Version - 1,
            workflow.Version,
            timeProvider.GetUtcNow(),
            beforeJson: null,
            afterJson: JsonSerializer.Serialize(Map(due)));
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Map(due);
    }

    private async Task<CaseWorkflowRecord> MutateAsync(
        CaseMutationRequest request,
        string eventType,
        Func<PegasusDbContext, CaseWorkflowEntity, DateTimeOffset, Task> apply,
        CancellationToken cancellationToken,
        string? discriminator = null)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        await AcquireWorkflowMutationLockAsync(
            context,
            request.CaseId,
            cancellationToken);
        var operationKey = request.OperationKey.Trim();
        var hash = Hash($"{request.GetType().FullName}|{JsonSerializer.Serialize(request, request.GetType())}|{discriminator}");
        if (await context.CaseEditLeaseOperations.AsNoTracking().AnyAsync(
                item => item.CaseId == request.CaseId
                    && item.OperationKey == operationKey,
                cancellationToken))
        {
            throw new CaseOperationConflictException(request.CaseId, operationKey);
        }

        var replay = await context.CaseWorkflowEvents.AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.CaseId == request.CaseId
                    && item.OperationKey == operationKey,
                cancellationToken);
        if (replay is not null)
        {
            if (!string.Equals(replay.RequestHash, hash, StringComparison.Ordinal))
            {
                throw new CaseOperationConflictException(request.CaseId, request.OperationKey);
            }
            var replayWorkflow = await WorkflowQuery(context, tracking: false).SingleAsync(item => item.CaseId == request.CaseId, cancellationToken);
            return Map(replayWorkflow);
        }

        var workflow = await WorkflowQuery(context, tracking: true)
            .SingleOrDefaultAsync(item => item.CaseId == request.CaseId, cancellationToken)
            ?? throw new KeyNotFoundException($"Case '{request.CaseId}' was not found.");
        StaffAuthorization.Require(request.Actor, StaffAccessRight.PerformCasework);
        if (request is ReopenCaseRequest or ArchiveCaseRequest)
        {
            ArchivedCaseGuard.RequireNotArchived(workflow);
        }
        else
        {
            ArchivedCaseGuard.RequireMutable(workflow);
        }
        RequireVersion(workflow, request.ExpectedVersion);
        var now = timeProvider.GetUtcNow();
        RequireLease(workflow, request.Actor, request.EditLeaseToken, now);
        var beforeJson = JsonSerializer.Serialize(HistoryValue(workflow));
        var beforeVersion = workflow.Version;
        await apply(context, workflow, now);
        workflow.Version = checked(workflow.Version + 1);
        ClearLease(workflow);
        var afterJson = JsonSerializer.Serialize(HistoryValue(workflow));
        AddEvent(
            context,
            workflow,
            request.Actor,
            operationKey,
            request.Reason.Trim(),
            hash,
            eventType,
            beforeVersion,
            workflow.Version,
            now,
            beforeJson,
            afterJson);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Map(workflow);
    }

    private async Task<AutoLinkReportEvidenceResult> TryAutoLinkOnceAsync(
        AutoLinkReportEvidenceRequest request,
        CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await context.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);
        await AcquireWorkflowMutationLockAsync(
            context,
            request.CaseId,
            cancellationToken);
        var operationKey = request.OperationKey.Trim();
        var requestHash = Hash(
            $"{request.GetType().FullName}|{JsonSerializer.Serialize(request, request.GetType())}");
        if (await context.CaseEditLeaseOperations
                .AsNoTracking()
                .AnyAsync(
                    item => item.CaseId == request.CaseId
                        && item.OperationKey == operationKey,
                    cancellationToken))
        {
            throw new CaseOperationConflictException(request.CaseId, operationKey);
        }

        var replay = await context.CaseWorkflowEvents
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.CaseId == request.CaseId
                    && item.OperationKey == operationKey,
                cancellationToken);
        if (replay is not null)
        {
            if (!string.Equals(replay.RequestHash, requestHash, StringComparison.Ordinal))
            {
                throw new CaseOperationConflictException(request.CaseId, operationKey);
            }

            var replayWorkflow = await AutoLinkWorkflowQuery(context, tracking: false)
                .SingleOrDefaultAsync(item => item.CaseId == request.CaseId, cancellationToken);
            if (replay.EventType == "report_evidence_auto_linked"
                && replayWorkflow is not null
                && replayWorkflow.State == nameof(CaseLifecycleState.PostReport)
                && replayWorkflow.ReportSentEvidenceId == request.EvidenceId
                && replayWorkflow.ReportSentEvidence?.CaseId == request.CaseId
                && replayWorkflow.ReportVersionLedgers
                    .SingleOrDefault(item => item.ReportVersionId == request.ReportVersionId!.Value)
                    ?.CurrentEvidenceId == request.EvidenceId)
            {
                return AutoLinkLinked(replayWorkflow);
            }

            return AutoLinkNotLinked("concurrency_conflict");
        }

        var workflow = await AutoLinkWorkflowQuery(context, tracking: true)
            .SingleOrDefaultAsync(item => item.CaseId == request.CaseId, cancellationToken);
        if (workflow is null)
        {
            return AutoLinkNotLinked("case_not_found");
        }
        if (workflow.ArchivedAtUtc is not null
            || !Enum.TryParse<CaseLifecycleState>(
                workflow.State,
                ignoreCase: false,
                out var lifecycleState)
            || CaseLifecycleRules.IsTerminal(lifecycleState))
        {
            return AutoLinkNotLinked("case_not_mutable");
        }

        var now = timeProvider.GetUtcNow();
        var evaluation = await EvaluateReportEvidenceLinkAsync(
            context,
            workflow,
            request.EvidenceId,
            request.ReportVersionId,
            now,
            cancellationToken);
        if (evaluation.Evidence is null)
        {
            return AutoLinkNotLinked(evaluation.ReasonCode!);
        }

        var beforeJson = JsonSerializer.Serialize(HistoryValue(workflow));
        var beforeVersion = workflow.Version;
        ApplyReportEvidenceLink(workflow, evaluation.Evidence, request.Actor, now);
        if (evaluation.Ledger is not null)
        {
            evaluation.Ledger.CurrentEvidenceId = evaluation.Evidence!.Id;
            evaluation.Ledger.CurrentEvidence = evaluation.Evidence;
            evaluation.Ledger.Version++;
            AddAssociationHistory(
                context,
                evaluation.Ledger,
                evaluation.Evidence.Id,
                approvalId: evaluation.Ledger.ApprovalId,
                beforeReportVersionId: null,
                afterReportVersionId: evaluation.Ledger.ReportVersionId,
                "linked",
                request.Actor,
                request.Reason.Trim(),
                request.OperationKey.Trim(),
                now);
        }
        workflow.Version = checked(workflow.Version + 1);
        ClearLease(workflow);
        var afterJson = JsonSerializer.Serialize(HistoryValue(workflow));
        AddEvent(
            context,
            workflow,
            request.Actor,
            operationKey,
            request.Reason.Trim(),
            requestHash,
            "report_evidence_auto_linked",
            beforeVersion,
            workflow.Version,
            now,
            beforeJson,
            afterJson);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return AutoLinkLinked(workflow);
    }

    private static async Task<ReportEvidenceLinkEvaluation> EvaluateReportEvidenceLinkAsync(
        PegasusDbContext context,
        CaseWorkflowEntity workflow,
        Guid evidenceId,
        Guid? reportVersionId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (workflow.State != nameof(CaseLifecycleState.ReportPreparation))
        {
            return new(
                null,
                null,
                "case_not_report_preparation",
                "Report-Sent evidence can be linked only from Report preparation.");
        }

        if (reportVersionId is null)
        {
            return new(null, null, "report_version_required", "An immutable report version identity is required for this association.");
        }

        var ledger = await context.CaseReportVersionLedgers
            .Include(item => item.ReportVersion)
            .ThenInclude(item => item.Artifacts)
            .Include(item => item.Approval)
            .SingleOrDefaultAsync(
                item => item.ReportVersionId == reportVersionId.Value
                    && item.CaseId == workflow.CaseId,
                cancellationToken);
        if (ledger is null)
        {
            return new(null, null, "report_version_not_found", "The selected report version does not belong to this case.");
        }
        if (ledger.ReportVersion.State != nameof(AssessmentReportGenerationState.Generated)
            || ledger.Approval is null)
        {
            return new(null, null, "report_version_not_approved", "The selected report version has no authoritative approval.");
        }

        var evidence = await context.CaseReportSentEvidence
            .SingleOrDefaultAsync(item => item.Id == evidenceId, cancellationToken);
        if (evidence is null)
        {
            return new(
                null,
                null,
                "evidence_not_found",
                "The retained approved-mailbox Sent evidence does not exist.");
        }

        var evidenceAggregateId = evidence.Id.ToString("D");
        var hasAuthoritativeRetention =
            evidence.DiscoveredByKind == nameof(ActorKind.SystemWorker)
            && await context.ActionHistory
                .AsNoTracking()
                .AnyAsync(
                    item => item.AggregateType == "report_sent_evidence"
                        && item.AggregateId == evidenceAggregateId
                        && item.EventKind == "report_sent_evidence_retained"
                        && item.ActorKind == evidence.DiscoveredByKind
                        && item.ActorSubjectId == evidence.DiscoveredBySubjectId
                        && item.CorrelationId == evidence.RetentionOperationKey
                        && item.Outcome == "Succeeded",
                    cancellationToken);
        if (!hasAuthoritativeRetention)
        {
            return new(
                null,
                null,
                "evidence_not_authoritatively_retained",
                "The report-Sent evidence has no authoritative approved-mailbox retention record.");
        }

        if (workflow.ReportSentEvidenceId is not null
            && (ledger is null || ledger.CurrentEvidenceId is not null))
        {
            return new(
                null,
                null,
                "case_already_has_report_evidence",
                "The case already has current report-Sent evidence.");
        }
        if (evidence.CaseId is not null)
        {
            return new(
                null,
                null,
                "evidence_already_linked",
                "The retained approved-mailbox Sent evidence is already linked to a case.");
        }
        if (evidence.SentAtUtc > now || evidence.DiscoveredAtUtc > now)
        {
            return new(
                null,
                null,
                "evidence_future_dated",
                "Future-dated retained Sent evidence cannot enter post-report work.");
        }
        var approval = ledger.Approval;
        if (evidence.SourceReportVersionId != ledger.ReportVersionId
            || !ArtifactMatches(
                ledger.ReportVersion,
                evidence.SourceArtifactIdentity,
                evidence.SourceArtifactSha256,
                approval.ArtifactIdentity,
                approval.ArtifactSha256))
        {
            return new(
                null,
                null,
                "evidence_report_version_mismatch",
                "The retained Sent evidence does not carry the selected report version and artifact identity.");
        }
        if (ledger.CurrentEvidenceId is not null)
        {
            return new(
                null,
                null,
                "report_version_already_has_evidence",
                "The selected report version already has final Sent evidence.");
        }
        if (evidence.SentAtUtc < approval.ApprovedAtUtc)
        {
            return new(
                null,
                null,
                "evidence_predates_report_approval",
                "Retained Sent evidence cannot predate the current report approval.");
        }

        var followsReportPreparation = await context.CaseWorkflowEvents
            .AsNoTracking()
            .AnyAsync(
                item => item.CaseId == workflow.CaseId
                    && item.OccurredAtUtc <= evidence.SentAtUtc
                    && (item.EventType == "state_ReportPreparation"
                        || item.EventType == "case_reopened_ReportPreparation"),
                cancellationToken);
        if (!followsReportPreparation)
        {
            return new(
                null,
                null,
                "evidence_predates_report_preparation",
                "Retained Sent evidence must follow an authoritative Report preparation transition.");
        }

        return new(evidence, ledger, null, null);
    }

    private static void ApplyReportEvidenceLink(
        CaseWorkflowEntity workflow,
        CaseReportSentEvidenceEntity evidence,
        ActionActor actor,
        DateTimeOffset linkedAtUtc)
    {
        evidence.CaseId = workflow.CaseId;
        evidence.LinkedAtUtc = linkedAtUtc;
        evidence.LinkedByKind = actor.Kind.ToString();
        evidence.LinkedBySubjectId = actor.SubjectId;
        evidence.LinkedByRolesJson = RolesJson(actor);
        evidence.AssociationStatus = evidence.SourceReportVersionId is null
            ? "Unresolved"
            : "Authoritative";
        evidence.AssociationStatusReason = evidence.SourceReportVersionId is null
            ? "The source did not name an immutable report version."
            : "The retained Sent evidence is bound to its immutable report version.";
        workflow.ReportSentEvidenceId = evidence.Id;
        workflow.ReportSentEvidence = evidence;
        workflow.State = nameof(CaseLifecycleState.PostReport);
    }

    private static bool ArtifactMatches(
        AssessmentReportArtifactEntity artifact,
        string identity,
        string sha256) =>
        (string.Equals(artifact.FileName, identity, StringComparison.Ordinal)
            || string.Equals(artifact.Id.ToString("D"), identity, StringComparison.OrdinalIgnoreCase))
        && string.Equals(artifact.Sha256, sha256, StringComparison.OrdinalIgnoreCase);

    private static bool ArtifactMatches(
        AssessmentReportVersionEntity version,
        string? sourceIdentity,
        string? sourceSha256,
        string approvalIdentity,
        string approvalSha256) =>
        sourceIdentity is not null
        && sourceSha256 is not null
        && string.Equals(sourceIdentity, approvalIdentity, StringComparison.Ordinal)
        && string.Equals(sourceSha256, approvalSha256, StringComparison.OrdinalIgnoreCase)
        && version.Artifacts.Any(item => ArtifactMatches(item, approvalIdentity, approvalSha256));

    private static void AddAssociationHistory(
        PegasusDbContext context,
        CaseReportVersionLedgerEntity ledger,
        Guid? evidenceId,
        Guid? approvalId,
        Guid? beforeReportVersionId,
        Guid? afterReportVersionId,
        string action,
        ActionActor actor,
        string reason,
        string operationKey,
        DateTimeOffset occurredAtUtc,
        Guid? formerCaseId = null,
        DateTimeOffset? formerLinkedAtUtc = null,
        string? formerLinkedByKind = null,
        string? formerLinkedBySubjectId = null,
        string? formerLinkedByRolesJson = null)
    {
        var history = new CaseReportAssociationHistoryEntity
        {
            Id = Guid.NewGuid(),
            LedgerReportVersionId = ledger.ReportVersionId,
            EvidenceId = evidenceId,
            ApprovalId = approvalId,
            BeforeReportVersionId = beforeReportVersionId,
            AfterReportVersionId = afterReportVersionId,
            Action = action,
            ActorKind = actor.Kind.ToString(),
            ActorSubjectId = actor.SubjectId,
            ActorRolesJson = RolesJson(actor),
            Reason = reason,
            OperationKey = operationKey,
            LedgerVersion = ledger.Version,
            OccurredAtUtc = occurredAtUtc,
            FormerCaseId = formerCaseId,
            FormerLinkedAtUtc = formerLinkedAtUtc,
            FormerLinkedByKind = formerLinkedByKind,
            FormerLinkedBySubjectId = formerLinkedBySubjectId,
            FormerLinkedByRolesJson = formerLinkedByRolesJson
        };
        ledger.AssociationHistory.Add(history);
        context.CaseReportAssociationHistory.Add(history);
    }

    private static AutoLinkReportEvidenceResult AutoLinkLinked(CaseWorkflowEntity workflow)
    {
        if (workflow.State != nameof(CaseLifecycleState.PostReport)
            || workflow.ReportSentEvidenceId is not { } evidenceId
            || workflow.ReportSentEvidence is not { } evidence
            || evidence.Id != evidenceId
            || evidence.CaseId != workflow.CaseId)
        {
            throw new InvalidDataException(
                "The automatic report-evidence link did not produce a canonical association.");
        }

        return new(
            AutoLinkReportEvidenceDisposition.Linked,
            new(
                workflow.CaseId,
                evidenceId,
                CaseLifecycleState.PostReport,
                workflow.Version),
            null);
    }

    private static IQueryable<CaseWorkflowEntity> AutoLinkWorkflowQuery(
        PegasusDbContext context,
        bool tracking)
    {
        var query = context.CaseWorkflows
            .Include(item => item.ReportApproval)
            .Include(item => item.ReportSentEvidence)
            .Include(item => item.ReportVersionLedgers)
                .ThenInclude(item => item.ReportVersion)
                    .ThenInclude(item => item.Artifacts)
            .Include(item => item.ReportVersionLedgers)
                .ThenInclude(item => item.Approval)
            .Include(item => item.ReportVersionLedgers)
                .ThenInclude(item => item.CurrentEvidence)
            .Include(item => item.ReportVersionLedgers)
                .ThenInclude(item => item.AssociationHistory);
        return tracking ? query : query.AsNoTracking();
    }


    private static async Task AcquireWorkflowMutationLockAsync(
        PegasusDbContext context,
        Guid caseId,
        CancellationToken cancellationToken)
    {
        if (!context.Database.IsSqlServer())
        {
            return;
        }

        _ = await context.CaseWorkflows
            .FromSqlInterpolated($"""
                SELECT *
                FROM [CaseWorkflows] WITH (UPDLOCK, HOLDLOCK)
                WHERE [CaseId] = {caseId}
                """)
            .AsNoTracking()
            .Select(item => item.CaseId)
            .SingleOrDefaultAsync(cancellationToken);
    }

    private static AutoLinkReportEvidenceResult AutoLinkNotLinked(string reasonCode) =>
        new(AutoLinkReportEvidenceDisposition.NotLinked, null, reasonCode);

    private static bool IsConcurrencyFailure(Exception exception) => exception switch
    {
        DbUpdateConcurrencyException => true,
        SqlException { Number: 1205 or 2601 or 2627 } => true,
        Exception { InnerException: { } innerException } =>
            IsConcurrencyFailure(innerException),
        _ => false
    };

    private readonly record struct ReportEvidenceLinkEvaluation(
        CaseReportSentEvidenceEntity? Evidence,
        CaseReportVersionLedgerEntity? Ledger,
        string? ReasonCode,
        string? Message);

    private static IQueryable<CaseWorkflowEntity> WorkflowQuery(PegasusDbContext context, bool tracking)
    {
        var query = context.CaseWorkflows
            .Include(item => item.Case).ThenInclude(item => item.Principal)
            .Include(item => item.ReportApproval)
            .Include(item => item.ReportSentEvidence)
            .Include(item => item.ReportVersionLedgers)
                .ThenInclude(item => item.ReportVersion)
                    .ThenInclude(item => item.Artifacts)
            .Include(item => item.ReportVersionLedgers)
                .ThenInclude(item => item.Approval)
            .Include(item => item.ReportVersionLedgers)
                .ThenInclude(item => item.CurrentEvidence)
            .Include(item => item.ReportVersionLedgers)
                .ThenInclude(item => item.AssociationHistory)
            .Include(item => item.DueWork);
        return tracking ? query : query.AsNoTracking();
    }


    private static async Task<CaseArchiveReadiness> LoadArchiveReadinessAsync(
        PegasusDbContext context,
        CaseEntity caseEntity,
        CancellationToken cancellationToken)
    {
        var isCustodyConfirmed =
            string.Equals(caseEntity.CustodyState, "confirmed", StringComparison.Ordinal)
            && (!string.Equals(caseEntity.Type, "audit", StringComparison.Ordinal)
                || (!string.IsNullOrWhiteSpace(caseEntity.AuditCustodyRemoteId)
                    && caseEntity.AuditCustodyConfirmedAtUtc is not null));
        var hasBlockingExternalWork = await context.ExternalWorkItems
            .AsNoTracking()
            .AnyAsync(
                item => item.CaseId == caseEntity.Id
                    && ((item.Kind == ExternalWorkKinds.CreateCaseCustody
                            && item.State != "completed")
                        || (item.Kind == ExternalWorkKinds.CreateAuditReferenceCustody
                            && item.State != "completed")
                        || (item.Kind == ExternalWorkKinds.VehicleLookup
                            && item.State != "completed"
                            && item.State != "failed")
                        || (item.Kind != ExternalWorkKinds.CreateCaseCustody
                            && item.Kind != ExternalWorkKinds.CreateAuditReferenceCustody
                            && item.Kind != ExternalWorkKinds.VehicleLookup)),
                cancellationToken);
        return new(isCustodyConfirmed, hasBlockingExternalWork);
    }


    private static async Task<CaseEditLeaseOperationEntity?> FindLeaseOperationAsync(
        PegasusDbContext context,
        Guid caseId,
        string operationKey,
        CancellationToken cancellationToken)
    {
        var replay = await context.CaseEditLeaseOperations.AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.CaseId == caseId
                    && item.OperationKey == operationKey,
                cancellationToken);
        if (replay is null
            && await context.CaseWorkflowEvents.AsNoTracking().AnyAsync(
                item => item.CaseId == caseId
                    && item.OperationKey == operationKey,
                cancellationToken))
        {
            throw new CaseOperationConflictException(caseId, operationKey);
        }

        return replay;
    }

    private static void EnsureLeaseReplay(
        CaseEditLeaseOperationEntity replay,
        string operationKind,
        string requestHash,
        Guid caseId,
        string operationKey)
    {
        if (!string.Equals(replay.OperationKind, operationKind, StringComparison.Ordinal)
            || !HashesMatch(replay.RequestHash, requestHash))
        {
            throw new CaseOperationConflictException(caseId, operationKey);
        }
    }

    private static CaseEditLease ReadLeaseReplayOrThrow(
        CaseWorkflowEntity workflow,
        CaseEditLeaseOperationEntity replay,
        ActionActor actor,
        string operationKey,
        DateTimeOffset now)
    {
        // The replay legitimately returns the retained plaintext token, but whether the lease is
        // still held is the one owner's question here as everywhere else.
        if (!CaseEditAuthority.IsHeld(workflow.EditLeaseExpiresAtUtc, now)
            || workflow.EditLeaseToken is not
                { Length: CaseEditAuthority.LeaseTokenLength } token
            || workflow.EditLeaseTokenHash is not { } tokenHash)
        {
            throw new CaseEditLeaseExpiredException(workflow.CaseId, workflow.Version);
        }
        if (!string.Equals(
                workflow.EditLeaseHolder,
                actor.SubjectId,
                StringComparison.Ordinal))
        {
            throw new CaseEditLeaseConflictException(workflow.CaseId, workflow.Version);
        }
        if (replay.ResultExpiresAtUtc is not { } resultExpiresAtUtc
            || resultExpiresAtUtc <= now
            || !HashesMatch(replay.ResultTokenHash, tokenHash)
            || !HashesMatch(tokenHash, Hash(token)))
        {
            throw new CaseOperationConflictException(workflow.CaseId, operationKey);
        }

        return new(
            workflow.CaseId,
            token,
            actor.SubjectId,
            replay.ResultVersion,
            resultExpiresAtUtc);
    }

    private static string LeaseOperationRequestHash(
        string operationKind,
        Guid caseId,
        long? expectedVersion,
        ActionActor actor,
        string operationKey,
        string? leaseToken) =>
        Hash(JsonSerializer.Serialize(new
        {
            SchemaVersion = 1,
            OperationKind = operationKind,
            CaseId = caseId,
            ExpectedVersion = expectedVersion,
            ActorKind = actor.Kind.ToString(),
            ActorSubjectId = actor.SubjectId,
            ActorRolesJson = RolesJson(actor),
            OperationKey = operationKey,
            LeaseTokenHash = leaseToken is null ? null : Hash(leaseToken)
        }));

    private static void AddLeaseOperation(
        PegasusDbContext context,
        CaseWorkflowEntity workflow,
        ActionActor actor,
        string operationKey,
        string operationKind,
        string requestHash,
        DateTimeOffset completedAtUtc,
        long resultVersion,
        DateTimeOffset? resultExpiresAtUtc,
        string? resultTokenHash)
    {
        context.CaseEditLeaseOperations.Add(new()
        {
            CaseId = workflow.CaseId,
            Workflow = workflow,
            OperationKey = operationKey,
            OperationKind = operationKind,
            RequestHash = requestHash,
            ActorKind = actor.Kind.ToString(),
            ActorSubjectId = actor.SubjectId,
            ActorRolesJson = RolesJson(actor),
            CompletedAtUtc = completedAtUtc,
            ResultVersion = resultVersion,
            ResultExpiresAtUtc = resultExpiresAtUtc,
            ResultTokenHash = resultTokenHash
        });
    }

    private static bool HashesMatch(string? storedHash, string expectedHash)
    {
        if (storedHash is null || storedHash.Length != expectedHash.Length)
        {
            return false;
        }

        try
        {
            return CryptographicOperations.FixedTimeEquals(
                Convert.FromHexString(storedHash),
                Convert.FromHexString(expectedHash));
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static void RequireVersion(CaseWorkflowEntity workflow, long expectedVersion) =>
        CaseMutationGuard.RequireVersion(workflow, expectedVersion);

    private static void RequireLease(CaseWorkflowEntity workflow, ActionActor actor, string token, DateTimeOffset now) =>
        CaseMutationGuard.RequireLease(workflow, actor, token, now);

    private static void ClearLease(CaseWorkflowEntity workflow) =>
        CaseMutationGuard.ClearLease(workflow);

    private static void AddEvent(
        PegasusDbContext context,
        CaseWorkflowEntity workflow,
        ActionActor actor,
        string operationKey,
        string reason,
        string requestHash,
        string eventType,
        long beforeVersion,
        long afterVersion,
        DateTimeOffset occurredAtUtc,
        string? beforeJson,
        string? afterJson)
    {
        context.CaseWorkflowEvents.Add(new()
        {
            Id = Guid.NewGuid(),
            CaseId = workflow.CaseId,
            Workflow = workflow,
            EventType = eventType,
            OperationKey = operationKey,
            RequestHash = requestHash,
            ActorKind = actor.Kind.ToString(),
            ActorSubjectId = actor.SubjectId,
            ActorRolesJson = RolesJson(actor),
            Reason = reason,
            OccurredAtUtc = occurredAtUtc,
            BeforeVersion = beforeVersion,
            AfterVersion = afterVersion,
            ResultJson = afterJson
        });
        context.ActionHistory.Add(new()
        {
            Id = Guid.NewGuid(),
            AggregateType = "case",
            AggregateId = workflow.CaseId.ToString("D"),
            EventKind = eventType,
            ActorKind = actor.Kind.ToString(),
            ActorSubjectId = actor.SubjectId,
            ActorRolesJson = JsonSerializer.Serialize(
                actor.Roles.OrderBy(role => role).Select(role => role.ToString())),
            OccurredAtUtc = occurredAtUtc,
            Outcome = "Succeeded",
            CorrelationId = operationKey,
            Reason = reason,
            BeforeJson = beforeJson,
            AfterJson = afterJson,
            PolicyVersion = "case-lifecycle-v1"
        });
    }

    private static CaseWorkflowRecord Map(CaseWorkflowEntity entity)
    {
        var currentApprovalLedger = entity.ReportVersionLedgers
            .FirstOrDefault(item => item.ApprovalId == entity.ReportApprovalId);
        var result = new CaseWorkflowRecord(
            entity.CaseId,
            new CaseIdentity(
                entity.CaseId,
                entity.Case.Principal.Code,
                entity.Case.Year,
                entity.Case.Sequence,
                entity.Case.Reference,
                entity.Case.AuditReference),
            Enum.Parse<CaseLifecycleState>(entity.State),
            entity.AssignedEngineerId,
            entity.ReportApproval is null ? null : MapApproval(entity.ReportApproval, currentApprovalLedger?.ReportVersionId),
            entity.ReportSentEvidence is null
                ? null
                : MapReportSentEvidence(entity.ReportSentEvidence),
            entity.DueWork is null ? null : Map(entity.DueWork, entity.Case.Reference),
            entity.ClosureOutcome is null
                ? null
                : Enum.Parse<CaseClosureOutcome>(entity.ClosureOutcome),
            entity.OriginalCaseId,
            entity.ReplacementCaseId,
            entity.Version)
        {
            Archive = MapArchive(entity),
            IssuedReportVersions = MapIssuedReportVersions(entity.ReportVersionLedgers)
        };
        return result;
    }

    private static ReportApprovalEvidence MapApproval(
        CaseReportApprovalEntity entity,
        Guid? reportVersionId) => new(
        entity.Id,
        entity.ArtifactIdentity,
        entity.ArtifactSha256,
        Actor(
            entity.ApprovedByKind,
            entity.ApprovedBySubjectId,
            entity.ApprovedByRolesJson),
        entity.ApprovedAtUtc,
        reportVersionId,
        entity.AssociationStatus ?? (reportVersionId is null ? "Unresolved" : "Authoritative"),
        entity.AssociationStatusReason);

    private static IssuedReportVersion[] MapIssuedReportVersions(
        IEnumerable<CaseReportVersionLedgerEntity> ledgers) => ledgers
        .OrderBy(item => item.ReportVersion.Version)
        .ThenBy(item => item.ReportVersionId)
        .Select(item => new IssuedReportVersion(
            item.ReportVersionId,
            item.ReportVersion.Version,
            item.Approval?.ArtifactIdentity,
            item.Approval?.ArtifactSha256,
            item.ReportVersion.PredecessorId,
            item.CorrectionReason,
            item.Approval is null ? null : MapApproval(item.Approval, item.ReportVersionId),
            item.CurrentEvidence is null ? null : MapReportSentEvidence(item.CurrentEvidence),
            item.AssociationHistory
                .OrderBy(history => history.LedgerVersion)
                .ThenBy(history => history.OccurredAtUtc)
                .ThenBy(history => history.Id)
                .Select(history => new ReportEvidenceAssociationHistory(
                    history.Id,
                    history.EvidenceId,
                    history.ApprovalId,
                    history.BeforeReportVersionId,
                    history.AfterReportVersionId,
                    history.Action,
                    LinkActor(history.ActorKind, history.ActorSubjectId, history.ActorRolesJson),
                    history.Reason,
                    history.OccurredAtUtc,
                    history.FormerCaseId,
                    history.FormerLinkedAtUtc,
                    OptionalActor(
                        history.FormerLinkedByKind,
                        history.FormerLinkedBySubjectId,
                        history.FormerLinkedByRolesJson)))
                .ToArray()))
        .ToArray();
    private static CaseArchive? MapArchive(CaseWorkflowEntity entity)
    {
        if (entity.ArchivedAtUtc is not { } archivedAtUtc)
        {
            return null;
        }
        if (entity.ArchivedByKind is null
            || entity.ArchivedBySubjectId is null
            || entity.ArchivedByRolesJson is null
            || entity.ArchiveReason is null)
        {
            throw new InvalidDataException(
                "The archived case is missing its attributable archive metadata.");
        }

        return new(
            archivedAtUtc,
            Actor(
                entity.ArchivedByKind,
                entity.ArchivedBySubjectId,
                entity.ArchivedByRolesJson),
            entity.ArchiveReason);
    }

    private static CaseWorkflowHistoryValue HistoryValue(
        CaseWorkflowEntity entity) => new(
            entity.State,
            entity.PreHoldState,
            entity.AssignedEngineerId,
            entity.ReportApprovalId,
            entity.ReportSentEvidenceId,
            entity.ClosureOutcome,
            entity.ArchivedAtUtc,
            entity.ArchivedBySubjectId,
            entity.ArchiveReason,
            entity.Version);

    private static ApprovedMailboxReportSentEvidence? MapReportSentEvidence(
        CaseReportSentEvidenceEntity entity)
    {
        if (string.Equals(
                entity.DiscoveredByKind,
                "LegacyUnverified",
                StringComparison.Ordinal))
        {
            return null;
        }

        if (entity.LinkedAtUtc is not { } linkedAtUtc
            || entity.LinkedByKind is null
            || entity.LinkedBySubjectId is null
            || entity.LinkedByRolesJson is null)
        {
            throw new InvalidDataException(
                "Case report-sent evidence is missing its authoritative link metadata.");
        }

        return new(
            entity.Id,
            entity.MailboxIdentity,
            entity.SentFolderIdentity,
            entity.ImmutableItemIdentity,
            entity.InternetMessageIdentity,
            entity.ConversationIdentity,
            entity.ReplyChainIdentity,
            entity.SourceOccurrenceIdentity,
            entity.SourceSha256,
            entity.MimeSha256,
            entity.SentAtUtc,
            entity.DiscoveredAtUtc,
            DiscoveryActor(entity.DiscoveredByKind, entity.DiscoveredBySubjectId),
            linkedAtUtc,
            LinkActor(entity.LinkedByKind, entity.LinkedBySubjectId, entity.LinkedByRolesJson),
            entity.SourceReportVersionId,
            entity.SourceArtifactIdentity,
            entity.SourceArtifactSha256,
            entity.AssociationStatus ?? (entity.SourceReportVersionId is null ? "Unresolved" : "Authoritative"),
            entity.AssociationStatusReason);
    }

    /// <summary>
    /// Maps due work when the query loaded the case behind it.
    /// </summary>
    private static CaseDueWork Map(CaseDueWorkEntity entity) =>
        Map(entity, entity.Workflow.Case.Reference);

    private static CaseDueWork Map(CaseDueWorkEntity entity, string reference) => new(
        entity.CaseId,
        reference,
        entity.MissingMaterialReason,
        entity.DueBy,
        Enum.Parse<CaseDueWorkState>(entity.State),
        entity.NextChaseAtUtc,
        entity.HeldAtUtc,
        entity.RemainingChaseIntervalTicks is null ? null : TimeSpan.FromTicks(entity.RemainingChaseIntervalTicks.Value),
        entity.MostRecentChannel,
        entity.MostRecentOutcome,
        entity.MostRecentNote,
        entity.Version);

    private static ActionActor DiscoveryActor(string kind, string subjectId) => kind switch
    {
        nameof(ActorKind.SystemWorker) => ActionActor.SystemWorker(subjectId),
        _ => throw new InvalidDataException(
            "Case report-sent evidence contains an unsupported discovery actor.")
    };

    private static ActionActor LinkActor(string kind, string subjectId, string rolesJson)
    {
        if (kind == nameof(ActorKind.SystemWorker))
        {
            var roles = JsonSerializer.Deserialize<StaffRole[]>(rolesJson) ?? [];
            if (roles.Length != 0)
            {
                throw new InvalidDataException(
                    "System-worker report-evidence linkage cannot contain staff roles.");
            }

            return ActionActor.SystemWorker(subjectId);
        }

        return Actor(kind, subjectId, rolesJson);
    }

    private static ActionActor Actor(string kind, string subjectId, string rolesJson)
    {
        if (kind != nameof(ActorKind.Staff) || !Guid.TryParse(subjectId, out var staffId))
        {
            throw new InvalidOperationException("Workflow evidence contains an unsupported actor identity.");
        }
        return ActionActor.Staff(staffId, JsonSerializer.Deserialize<StaffRole[]>(rolesJson) ?? []);
    }

    private static ActionActor? OptionalActor(
        string? kind,
        string? subjectId,
        string? rolesJson)
    {
        if (kind is null && subjectId is null && rolesJson is null)
        {
            return null;
        }

        if (kind is null || subjectId is null || rolesJson is null)
        {
            throw new InvalidDataException(
                "Report-evidence association history contains incomplete former-link actor metadata.");
        }

        return LinkActor(kind, subjectId, rolesJson);
    }

    private sealed record CaseWorkflowHistoryValue(
        string State,
        string? PreHoldState,
        Guid? AssignedEngineerId,
        Guid? ReportApprovalId,
        Guid? ReportSentEvidenceId,
        string? ClosureOutcome,
        DateTimeOffset? ArchivedAtUtc,
        string? ArchivedBySubjectId,
        string? ArchiveReason,
        long Version);

    private static string RolesJson(ActionActor actor) => JsonSerializer.Serialize(actor.Roles.OrderBy(role => role));
    private static string RequestHash<T>(T request) => Hash(JsonSerializer.Serialize(request));
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
