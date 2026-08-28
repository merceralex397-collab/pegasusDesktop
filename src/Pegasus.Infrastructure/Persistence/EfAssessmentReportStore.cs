using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Pegasus.Core.Assessment;
using Pegasus.Core.Documents;
using Pegasus.Core.Identity;
using Pegasus.Core.Reports;

namespace Pegasus.Infrastructure.Persistence;

/// <summary>
/// Owns the report-generation transaction boundary. The request row is
/// committed before rendering, while the two rendered PDFs and their normal
/// generated case-document custody rows are committed together afterwards.
/// </summary>
internal sealed partial class EfAssessmentReportStore(
    IDbContextFactory<PegasusDbContext> contextFactory,
    IDocumentContentStore contentStore,
    TimeProvider timeProvider,
    ILogger<EfAssessmentReportStore>? logger = null) : IAssessmentReportStore
{
    private static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(5);

    public async Task<IReadOnlyList<AssessmentReportVersion>> ListAsync(
        Guid caseId,
        CancellationToken cancellationToken = default)
    {
        if (caseId == Guid.Empty)
        {
            throw new ArgumentException("A case identifier is required.", nameof(caseId));
        }

        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var entities = await context.AssessmentReportVersions
            .AsNoTracking()
            .Include(item => item.Artifacts)
            .Where(item => item.CaseId == caseId)
            .OrderByDescending(item => item.Version)
            .ToListAsync(cancellationToken);
        return entities.Select(Map).ToArray();
    }

    public async Task<AssessmentReportGenerationReservation> BeginAsync(
        AssessmentReportGenerationRequest request,
        CancellationToken cancellationToken = default)
    {
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                return await BeginOnceAsync(request, cancellationToken);
            }
            catch (Exception exception) when (attempt < 2 && IsDeadlock(exception))
            {
                await Task.Delay(TimeSpan.FromMilliseconds(50 * (attempt + 1)), cancellationToken);
            }
        }
    }

    private async Task<AssessmentReportGenerationReservation> BeginOnceAsync(
        AssessmentReportGenerationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Snapshot);
        request.Snapshot.Validate();
        if (request.CaseId == Guid.Empty || request.Snapshot.CaseId != request.CaseId)
        {
            throw new ArgumentException("The report request and snapshot must name the same case.", nameof(request));
        }
        if (request.Snapshot.RepairSpecificationId is null
            || request.Snapshot.RepairSpecificationVersion is not > 0
            || request.Snapshot.RepairCostSource is null
            || request.Snapshot.Costs is not { IsImported: true })
        {
            throw new InvalidOperationException(
                "A report request must name one selected accepted repair estimate and its imported calculation basis.");
        }

        var payload = AssessmentReportPayload.Serialize(request.Snapshot);
        var key = AssessmentReportPayload.Key(request.Snapshot);
        key.Validate();
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await context.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        var now = timeProvider.GetUtcNow();
        var currentCaseVersion = await context.CaseWorkflows
            .Where(item => item.CaseId == request.CaseId)
            .Select(item => (long?)item.Version)
            .SingleOrDefaultAsync(cancellationToken);
        if (currentCaseVersion is null)
        {
            throw new KeyNotFoundException($"Case '{request.CaseId:D}' was not found.");
        }

        var existing = await context.AssessmentReportVersions
            .Include(item => item.Artifacts)
            .SingleOrDefaultAsync(
                item => item.CaseId == key.CaseId
                    && item.AssessmentFamily == key.AssessmentFamily
                    && item.AcceptedPayloadSha256 == key.AcceptedPayloadSha256
                    && item.TemplateVersion == key.TemplateVersion,
                cancellationToken);
        if (existing is not null)
        {
            if (existing.State == AssessmentReportGenerationState.Generated.ToString())
            {
                await transaction.CommitAsync(cancellationToken);
                return new(Map(existing), string.Empty, ShouldRender: false);
            }

            if (existing.State == AssessmentReportGenerationState.Rendering.ToString()
                && existing.LeaseExpiresAtUtc > now)
            {
                await transaction.CommitAsync(cancellationToken);
                return new(Map(existing), string.Empty, ShouldRender: false);
            }

            if (!AssessmentReportRetryPolicy.CanRetry(existing.AttemptCount))
            {
                throw new InvalidOperationException(
                    "The report draft has reached its retry limit and requires operator review.");
            }

            if (existing.NextAttemptAtUtc is { } nextAttemptAt && nextAttemptAt > now)
            {
                throw new InvalidOperationException(
                    $"The report draft retry is scheduled for {nextAttemptAt:O}.");
            }

            existing.State = AssessmentReportGenerationState.Rendering.ToString();
            existing.AttemptCount++;
            existing.LeaseId = NewLeaseId();
            existing.LeaseExpiresAtUtc = now.Add(LeaseDuration);
            existing.FailureReason = null;
            existing.NextAttemptAtUtc = null;
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new(Map(existing), existing.LeaseId, ShouldRender: true);
        }

        if (request.Snapshot.AssessmentCaseVersion != currentCaseVersion.Value)
        {
            throw new InvalidOperationException(
                "The report snapshot is stale; reload the accepted case before generating a draft.");
        }

        var specificationId = request.Snapshot.RepairSpecificationId.Value;
        var specification = await context.CaseRepairSpecifications
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.CaseId == request.CaseId && item.Id == specificationId,
                cancellationToken);
        if (specification is null
            || specification.State != nameof(RepairSpecificationState.Accepted)
            || specification.Version != request.Snapshot.RepairSpecificationVersion)
        {
            throw new InvalidOperationException(
                "The selected repair estimate is stale or no longer accepted; reload the case and select it again.");
        }

        var actualSource = request.Snapshot.RepairCostSource;
        if (specification.SourceArtifactReference is null
            || specification.SourceVersion is null
            || specification.SourceSha256 is null
            || !string.Equals(actualSource.Name, specification.SourceArtifactReference, StringComparison.Ordinal)
            || !string.Equals(actualSource.Version, specification.SourceVersion, StringComparison.Ordinal)
            || !string.Equals(actualSource.Sha256, specification.SourceSha256, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "The report snapshot source does not match the selected accepted repair estimate.");
        }

        if (specification.CalculationLabour is not { } labour
            || specification.CalculationParts is not { } parts
            || specification.CalculationPaintMaterials is not { } paintMaterials
            || specification.CalculationSpecialistOther is not { } specialistOther
            || specification.RepairerVatRegistered is not { } vatRegistered
            || specification.CalculationVat is not { } vat
            || specification.CalculationTotal is not { } total
            || specification.CalculationPolicyVersion is not { } policyVersion)
        {
            throw new InvalidOperationException(
                "The selected accepted repair estimate has no complete calculation basis.");
        }

        var expectedCosts = ReportRepairCosts.FromAcceptedBasis(
            new RepairCalculationBasis(
                labour,
                parts,
                paintMaterials,
                specialistOther,
                vatRegistered,
                vat,
                total,
                policyVersion));
        if (!CostsMatch(request.Snapshot.Costs, expectedCosts))
        {
            throw new InvalidOperationException(
                "The report snapshot costs do not match the selected accepted repair estimate.");
        }

        var predecessor = await context.AssessmentReportVersions
            .Where(item => item.CaseId == key.CaseId)
            .OrderByDescending(item => item.Version)
            .FirstOrDefaultAsync(cancellationToken);
        var nextVersion = (predecessor?.Version ?? 0) + 1;
        var entity = new AssessmentReportVersionEntity
        {
            Id = Guid.NewGuid(),
            CaseId = key.CaseId,
            Version = nextVersion,
            AssessmentFamily = key.AssessmentFamily,
            AcceptedPayloadSha256 = key.AcceptedPayloadSha256,
            TemplateVersion = key.TemplateVersion,
            LogicalKey = key.Value,
            State = AssessmentReportGenerationState.Rendering.ToString(),
            AcceptedPayloadJson = payload,
            PredecessorId = predecessor?.Id,
            CreatedAtUtc = now,
            LeaseId = NewLeaseId(),
            LeaseExpiresAtUtc = now.Add(LeaseDuration),
            AttemptCount = 1
        };
        context.AssessmentReportVersions.Add(entity);
        context.CaseReportVersionLedgers.Add(new CaseReportVersionLedgerEntity
        {
            ReportVersionId = entity.Id,
            CaseId = entity.CaseId,
            Version = 0
        });
        try
        {
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new(Map(entity), entity.LeaseId!, ShouldRender: true);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            context.ChangeTracker.Clear();
            var concurrent = await context.AssessmentReportVersions
                .AsNoTracking()
                .Include(item => item.Artifacts)
                .SingleOrDefaultAsync(
                    item => item.CaseId == key.CaseId
                        && item.AssessmentFamily == key.AssessmentFamily
                        && item.AcceptedPayloadSha256 == key.AcceptedPayloadSha256
                        && item.TemplateVersion == key.TemplateVersion,
                    cancellationToken);
            if (concurrent is null)
            {
                throw;
            }

            return new(Map(concurrent), string.Empty, ShouldRender: false);
        }
    }

    public async Task<AssessmentReportDraft?> ReadDraftAsync(
        AssessmentReportVersion version,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(version);
        if (version.State != AssessmentReportGenerationState.Generated)
        {
            return null;
        }

        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await context.AssessmentReportVersions
            .AsNoTracking()
            .Include(item => item.Artifacts)
            .SingleOrDefaultAsync(item => item.Id == version.Id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        var caseReference = await context.Cases
            .Where(item => item.Id == entity.CaseId)
            .Select(item => item.Reference)
            .SingleAsync(cancellationToken);
        var artifacts = new Dictionary<AssessmentReportArtifactKind, RenderedReportArtifact>();
        foreach (var artifact in entity.Artifacts)
        {
            var documentVersion = await context.Set<DocumentVersionEntity>()
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    item => item.Id == artifact.DocumentVersionId
                        && item.DocumentId == artifact.DocumentId,
                    cancellationToken);
            if (documentVersion is null
                || documentVersion.CustodyStatus != DocumentCustodyStatus.Confirmed
                || !documentVersion.IsCurrent
                || documentVersion.IsLogicallyRemoved)
            {
                return null;
            }

            var kind = ParseKind(artifact.Kind);
            var role = kind == AssessmentReportArtifactKind.Assessment
                ? DocumentSemanticRole.EngineerReport
                : DocumentSemanticRole.FeeNote;
            await using var content = await contentStore.OpenReadVersionAsync(
                new ManagedDocumentContentAddress(
                    entity.CaseId,
                    caseReference,
                    artifact.OccurrenceId,
                    artifact.DocumentOrdinal,
                    artifact.DocumentId,
                    artifact.DocumentVersionId,
                    artifact.DocumentVersion,
                    role,
                    artifact.FileName,
                    artifact.MediaType),
                artifact.Sha256,
                artifact.ContentLength,
                cancellationToken);
            var bytes = GC.AllocateUninitializedArray<byte>(checked((int)artifact.ContentLength));
            await content.ReadExactlyAsync(bytes, cancellationToken);
            artifacts[kind] = new(
                artifact.FileName,
                bytes,
                artifact.PageCount,
                artifact.Sha256,
                artifact.TemplateVersion,
                artifact.EngineVersion);
        }

        return artifacts.TryGetValue(AssessmentReportArtifactKind.Assessment, out var assessment)
            && artifacts.TryGetValue(AssessmentReportArtifactKind.FeeNote, out var feeNote)
            ? new AssessmentReportDraft(assessment, feeNote)
            : null;
    }

    public async Task<AssessmentReportVersion> CompleteAsync(
        AssessmentReportGenerationReservation reservation,
        AssessmentReportDraft draft,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reservation);
        ArgumentNullException.ThrowIfNull(draft);
        var candidates = new[]
        {
            (Kind: AssessmentReportArtifactKind.Assessment, Artifact: draft.Assessment),
            (Kind: AssessmentReportArtifactKind.FeeNote, Artifact: draft.FeeNote)
        };
        ValidateCandidateTemplates(candidates, reservation.Version.LogicalKey.TemplateVersion);

        string caseReference;
        IReadOnlyList<AssessmentReportArtifactEntity> persistedArtifacts;
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using (var transaction = await context.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken))
        {
            var entity = await context.AssessmentReportVersions
                .Include(item => item.Artifacts)
                .SingleAsync(item => item.Id == reservation.Version.Id, cancellationToken);
            RequireLease(entity, reservation.LeaseId);
            if (entity.State == AssessmentReportGenerationState.Generated.ToString())
            {
                await transaction.CommitAsync(cancellationToken);
                return Map(entity);
            }

            caseReference = await context.Cases
                .Where(item => item.Id == entity.CaseId)
                .Select(item => item.Reference)
                .SingleAsync(cancellationToken);
            if (entity.Artifacts.Count == 0)
            {
                var nextOrdinal = (await context.Set<CaseDocumentEntity>()
                    .Where(item => item.CaseId == entity.CaseId)
                    .Select(item => (int?)item.Ordinal)
                    .MaxAsync(cancellationToken) ?? 0) + 1;
                foreach (var candidate in candidates)
                {
                    var documentId = Guid.NewGuid();
                    var versionId = Guid.NewGuid();
                    var occurrenceId = Guid.NewGuid();
                    var ordinal = nextOrdinal++;
                    var sourceIdentity = $"report:{entity.Id:N}:{candidate.Kind.ToString().ToLowerInvariant()}";
                    var contentHash = candidate.Artifact.Sha256;
                    var semanticRole = RoleFor(candidate.Kind);
                    context.Add(new CaseDocumentEntity
                    {
                        Id = documentId,
                        CaseId = entity.CaseId,
                        Ordinal = ordinal,
                        SourceOccurrenceIdentity = sourceIdentity
                    });
                    context.Add(new DocumentVersionEntity
                    {
                        Id = versionId,
                        DocumentId = documentId,
                        Version = 1,
                        FileName = candidate.Artifact.SuggestedFileName,
                        MediaType = "application/pdf",
                        ContentLength = candidate.Artifact.Pdf.LongLength,
                        Sha256 = contentHash,
                        CustodyStatus = DocumentCustodyStatus.Pending,
                        CreatedAtUtc = entity.CreatedAtUtc,
                        CreatedBy = "GeneratedReport",
                        IsCurrent = true
                    });
                    context.Add(new DocumentOccurrenceEntity
                    {
                        Id = occurrenceId,
                        CaseId = entity.CaseId,
                        DocumentId = documentId,
                        VersionId = versionId,
                        Ordinal = ordinal,
                        SemanticRole = semanticRole,
                        Source = DocumentSource.Generated,
                        SourceOccurrenceIdentity = sourceIdentity,
                        RecordedAtUtc = entity.CreatedAtUtc,
                        OperationKey = $"report-{entity.Id:N}-{candidate.Kind.ToString().ToLowerInvariant()}"
                    });
                    context.Add(new AssessmentReportArtifactEntity
                    {
                        Id = Guid.NewGuid(),
                        ReportVersionId = entity.Id,
                        Kind = candidate.Kind.ToString(),
                        OccurrenceId = occurrenceId,
                        DocumentId = documentId,
                        DocumentVersionId = versionId,
                        DocumentVersion = 1,
                        DocumentOrdinal = ordinal,
                        FileName = candidate.Artifact.SuggestedFileName,
                        MediaType = "application/pdf",
                        ContentLength = candidate.Artifact.Pdf.LongLength,
                        Sha256 = contentHash,
                        PageCount = candidate.Artifact.PageCount,
                        TemplateVersion = candidate.Artifact.TemplateVersion,
                        EngineVersion = candidate.Artifact.EngineVersion
                    });
                }

                await context.SaveChangesAsync(cancellationToken);
            }
            else
            {
                ValidatePersistedArtifacts(entity.Artifacts, candidates, reservation.Version.LogicalKey.TemplateVersion);
            }

            persistedArtifacts = entity.Artifacts
                .Select(item => item)
                .ToArray();
            await transaction.CommitAsync(cancellationToken);
        }

        foreach (var candidate in candidates)
        {
            var persisted = persistedArtifacts.Single(item => ParseKind(item.Kind) == candidate.Kind);
            var address = new ManagedDocumentContentAddress(
                reservation.Version.CaseId,
                caseReference,
                persisted.OccurrenceId,
                persisted.DocumentOrdinal,
                persisted.DocumentId,
                persisted.DocumentVersionId,
                persisted.DocumentVersion,
                RoleFor(candidate.Kind),
                persisted.FileName,
                persisted.MediaType);
            await contentStore.StoreVersionAsync(
                address,
                candidate.Artifact.Pdf,
                persisted.Sha256,
                cancellationToken);
        }

        await using var finalContext = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var finalTransaction = await finalContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);
        var finalized = await finalContext.AssessmentReportVersions
            .Include(item => item.Artifacts)
            .SingleAsync(item => item.Id == reservation.Version.Id, cancellationToken);
        RequireLease(finalized, reservation.LeaseId);
        if (finalized.State == AssessmentReportGenerationState.Generated.ToString())
        {
            await finalTransaction.CommitAsync(cancellationToken);
            return Map(finalized);
        }

        ValidatePersistedArtifacts(
            finalized.Artifacts,
            candidates,
            reservation.Version.LogicalKey.TemplateVersion);
        foreach (var artifact in finalized.Artifacts)
        {
            var documentVersion = await finalContext.Set<DocumentVersionEntity>()
                .SingleAsync(item => item.Id == artifact.DocumentVersionId, cancellationToken);
            documentVersion.CustodyStatus = DocumentCustodyStatus.Confirmed;
        }

        finalized.State = AssessmentReportGenerationState.Generated.ToString();
        finalized.CompletedAtUtc = timeProvider.GetUtcNow();
        finalized.NextAttemptAtUtc = null;
        finalized.LeaseId = null;
        finalized.LeaseExpiresAtUtc = null;
        await finalContext.SaveChangesAsync(cancellationToken);
        await finalTransaction.CommitAsync(cancellationToken);
        return Map(finalized);
    }

    public async Task FailAsync(
        AssessmentReportGenerationReservation reservation,
        string reason,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reservation);
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        if (logger is not null)
        {
            LogGenerationFailed(logger, reservation.Version.Id, reason);
        }
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                await FailOnceAsync(reservation, reason, cancellationToken);
                return;
            }
            catch (Exception exception) when (attempt < 2 && IsDeadlock(exception))
            {
                await Task.Delay(TimeSpan.FromMilliseconds(50 * (attempt + 1)), cancellationToken);
            }
        }
    }

    private async Task FailOnceAsync(
        AssessmentReportGenerationReservation reservation,
        string reason,
        CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await context.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);
        var entity = await context.AssessmentReportVersions
            .Include(item => item.Artifacts)
            .SingleOrDefaultAsync(item => item.Id == reservation.Version.Id, cancellationToken);
        if (entity is null
            || entity.State != AssessmentReportGenerationState.Rendering.ToString()
            || entity.LeaseId != reservation.LeaseId)
        {
            await transaction.CommitAsync(cancellationToken);
            return;
        }

        entity.FailureReason = AssessmentReportFailureMessages.GenerationFailed;
        var canRetry = AssessmentReportRetryPolicy.CanRetry(entity.AttemptCount);
        entity.State = canRetry
            ? AssessmentReportGenerationState.Pending.ToString()
            : AssessmentReportGenerationState.Failed.ToString();
        entity.NextAttemptAtUtc = canRetry
            ? AssessmentReportRetryPolicy.NextAttemptAt(timeProvider.GetUtcNow(), entity.AttemptCount)
            : null;
        if (!canRetry && entity.Artifacts.Count > 0)
        {
            var versionIds = entity.Artifacts.Select(item => item.DocumentVersionId).ToArray();
            var pendingVersions = await context.Set<DocumentVersionEntity>()
                .Where(item => versionIds.Contains(item.Id)
                    && item.CustodyStatus == DocumentCustodyStatus.Pending)
                .ToListAsync(cancellationToken);
            foreach (var pendingVersion in pendingVersions)
            {
                pendingVersion.CustodyStatus = DocumentCustodyStatus.Failed;
            }
        }
        entity.LeaseId = null;
        entity.LeaseExpiresAtUtc = null;
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private static DocumentSemanticRole RoleFor(AssessmentReportArtifactKind kind) => kind switch
    {
        AssessmentReportArtifactKind.Assessment => DocumentSemanticRole.EngineerReport,
        AssessmentReportArtifactKind.FeeNote => DocumentSemanticRole.FeeNote,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
    };

    private static bool IsDeadlock(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is Microsoft.Data.SqlClient.SqlException { Number: 1205 })
            {
                return true;
            }
        }

        return false;
    }

    private static bool CostsMatch(ReportRepairCosts actual, ReportRepairCosts expected) =>
        actual.IsImported
        && actual.ImportedLabour == expected.ImportedLabour
        && actual.ImportedVat == expected.ImportedVat
        && string.Equals(actual.ImportedPolicyVersion, expected.ImportedPolicyVersion, StringComparison.Ordinal)
        && actual.Parts == expected.Parts
        && actual.PaintMaterials == expected.PaintMaterials
        && actual.SpecialistOther == expected.SpecialistOther
        && actual.RepairerVatRegistered == expected.RepairerVatRegistered
        && actual.Total == expected.Total;

    private static void ValidatePersistedArtifacts(
        List<AssessmentReportArtifactEntity> persisted,
        (AssessmentReportArtifactKind Kind, RenderedReportArtifact Artifact)[] candidates,
        string expectedTemplateVersion)
    {
        if (persisted.Count != candidates.Length)
        {
            throw new InvalidOperationException(
                "The persisted report artifact set is incomplete and cannot be recovered safely.");
        }

        foreach (var candidate in candidates)
        {
            var artifact = persisted.SingleOrDefault(item => ParseKind(item.Kind) == candidate.Kind);
            if (artifact is null
                || !string.Equals(artifact.FileName, candidate.Artifact.SuggestedFileName, StringComparison.Ordinal)
                || !string.Equals(artifact.MediaType, "application/pdf", StringComparison.Ordinal)
                || artifact.ContentLength != candidate.Artifact.Pdf.LongLength
                || !string.Equals(artifact.Sha256, candidate.Artifact.Sha256, StringComparison.Ordinal)
                || artifact.PageCount != candidate.Artifact.PageCount
                || !string.Equals(candidate.Artifact.TemplateVersion, expectedTemplateVersion, StringComparison.Ordinal)
                || !string.Equals(artifact.TemplateVersion, candidate.Artifact.TemplateVersion, StringComparison.Ordinal)
                || !string.Equals(artifact.EngineVersion, candidate.Artifact.EngineVersion, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"The rendered {candidate.Kind} artifact does not match its persisted report metadata.");
            }
        }
    }

    private static void ValidateCandidateTemplates(
        (AssessmentReportArtifactKind Kind, RenderedReportArtifact Artifact)[] candidates,
        string expectedTemplateVersion)
    {
        if (candidates.Any(candidate => !string.Equals(
                candidate.Artifact.TemplateVersion,
                expectedTemplateVersion,
                StringComparison.Ordinal)))
        {
            throw new ReportRenderRejectedException(
                "The renderer returned an artifact for a template version different from the report request.");
        }
    }

    private static void RequireLease(AssessmentReportVersionEntity entity, string leaseId)
    {
        if (entity.State != AssessmentReportGenerationState.Rendering.ToString()
            || string.IsNullOrWhiteSpace(leaseId)
            || !string.Equals(entity.LeaseId, leaseId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The report generation lease is no longer active.");
        }
    }

    private static string NewLeaseId() => Guid.NewGuid().ToString("N");

    private static AssessmentReportGenerationState ParseState(string value) =>
        Enum.Parse<AssessmentReportGenerationState>(value, ignoreCase: false);

    private static AssessmentReportArtifactKind ParseKind(string value) =>
        Enum.Parse<AssessmentReportArtifactKind>(value, ignoreCase: false);

    private static AssessmentReportVersion Map(AssessmentReportVersionEntity entity)
    {
        var key = new AssessmentReportLogicalKey(
            entity.CaseId,
            entity.AssessmentFamily,
            entity.AcceptedPayloadSha256,
            entity.TemplateVersion);
        var artifacts = entity.Artifacts
            .Select(item => new AssessmentReportArtifact(
                item.Id,
                ParseKind(item.Kind),
                item.FileName,
                item.MediaType,
                item.ContentLength,
                item.Sha256,
                item.PageCount,
                item.TemplateVersion,
                item.EngineVersion))
            .ToArray();
        var result = new AssessmentReportVersion(
            entity.Id,
            entity.CaseId,
            entity.Version,
            key,
            ParseState(entity.State),
            entity.AcceptedPayloadJson,
            entity.PredecessorId,
            artifacts,
            entity.CreatedAtUtc,
            entity.CompletedAtUtc,
            entity.FailureReason,
            entity.AttemptCount,
            entity.NextAttemptAtUtc,
            entity.LeaseExpiresAtUtc);
        result.Validate();
        return result;
    }

    [LoggerMessage(
        EventId = 6101,
        Level = LogLevel.Error,
        Message = "Assessment report generation failed for report version {ReportVersionId}: {DiagnosticReason}")]
    private static partial void LogGenerationFailed(
        ILogger logger,
        Guid reportVersionId,
        string diagnosticReason);
}
