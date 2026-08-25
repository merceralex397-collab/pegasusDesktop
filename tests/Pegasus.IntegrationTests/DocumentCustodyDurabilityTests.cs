using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Pegasus.Core.Custody;
using Pegasus.Core.Documents;
using Pegasus.Core.Intake;
using Pegasus.Core.Identity;
using Pegasus.Core.Workflow;
using Pegasus.Infrastructure.Custody;
using Pegasus.Infrastructure.Persistence;

namespace Pegasus.IntegrationTests;

[Trait("Category", "SqlServer")]
public sealed class DocumentCustodyDurabilityTests
{
    [Fact]
    public async Task OpenReadVersionAsyncReadsContentRetainedByLocalCaseCustody()
    {
        var content = "retained intake source"u8.ToArray();
        var hash = Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();
        var artifactStore = new FixedArtifactStore(content);
        using var factory = new IntakeWebApplicationFactory(
            "Development",
            localIntakeEnabled: true,
            artifactStore: artifactStore);
        await using var scope = factory.Services.CreateAsyncScope();
        var custody = scope.ServiceProvider.GetRequiredService<ICaseCustody>();
        var caseId = Guid.NewGuid();
        var receiptId = Guid.NewGuid();
        var root = await custody.CreateCaseRootAsync(
            caseId,
            "QDOS31010",
            "custody-root:document-read",
            CancellationToken.None);
        await custody.RetainAcceptedIntakeSourceAsync(
            root,
            new(
                receiptId,
                "source.eml",
                "message/rfc822",
                hash,
                FixedArtifactStore.SourceObjectKey,
                content.LongLength),
            "custody-content:document-read",
            CancellationToken.None);

        IDocumentContentStore store = new LocalDocumentContentStore(
            Path.Combine(factory.ArtifactDirectory, "custody"));
        var address = new ManagedDocumentContentAddress(
            caseId,
            root.Reference,
            Guid.NewGuid(),
            1,
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            DocumentSemanticRole.OriginalSource,
            "source.eml",
            "message/rfc822");

        await using var retained = await store.OpenReadVersionAsync(
            address,
            hash,
            content.LongLength,
            CancellationToken.None);
        using var copy = new MemoryStream();
        await retained.CopyToAsync(copy);

        Assert.Equal(content, copy.ToArray());
    }

    [Fact]
    public async Task OpenReadVersionAsyncReadsAttachmentAndFoldedImageCustodyLayouts()
    {
        var attachment = "retained instruction attachment"u8.ToArray();
        var image = "retained image-case asset"u8.ToArray();
        var artifactStore = new MappingArtifactStore(new Dictionary<string, ReadOnlyMemory<byte>>
        {
            ["attachment"] = attachment,
            ["image"] = image
        });
        using var factory = new IntakeWebApplicationFactory(
            "Development",
            localIntakeEnabled: true,
            artifactStore: artifactStore);
        await using var scope = factory.Services.CreateAsyncScope();
        var custody = scope.ServiceProvider.GetRequiredService<ICaseCustody>();
        var caseId = Guid.NewGuid();
        var root = await custody.CreateCaseRootAsync(
            caseId,
            "QDOS31011",
            "custody-root:document-shapes",
            CancellationToken.None);
        var attachmentHash = Convert.ToHexString(SHA256.HashData(attachment)).ToLowerInvariant();
        await custody.RetainAcceptedIntakeAttachmentAsync(
            root,
            new(
                Guid.NewGuid(),
                "instruction.pdf",
                "application/pdf",
                attachmentHash,
                "attachment",
                attachment.LongLength),
            2,
            "custody-content:attachment",
            CancellationToken.None);

        IDocumentContentStore store = new LocalDocumentContentStore(
            Path.Combine(factory.ArtifactDirectory, "custody"));
        var attachmentAddress = new ManagedDocumentContentAddress(
            caseId,
            root.Reference,
            Guid.NewGuid(),
            2,
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            DocumentSemanticRole.Instruction,
            "instruction.pdf",
            "application/pdf");
        await using (var attachmentStream = await store.OpenReadVersionAsync(
                         attachmentAddress,
                         attachmentHash,
                         attachment.LongLength,
                         CancellationToken.None))
        {
            using var copy = new MemoryStream();
            await attachmentStream.CopyToAsync(copy);
            Assert.Equal(attachment, copy.ToArray());
        }

        var imageCaseId = Guid.NewGuid();
        var imageRoot = await custody.CreateCaseRootAsync(
            imageCaseId,
            "IMG31011",
            "custody-root:image-shape",
            CancellationToken.None);
        var imageReceiptId = Guid.NewGuid();
        var imageHash = Convert.ToHexString(SHA256.HashData(image)).ToLowerInvariant();
        await custody.RetainImageCaseAssetAsync(
            imageRoot,
            new(
                imageReceiptId,
                "damage.jpg",
                "image/jpeg",
                imageHash,
                "image",
                image.LongLength),
            1,
            "custody-content:image",
            CancellationToken.None);
        await custody.MergeImageCaseContentsAsync(
            imageRoot,
            root,
            "custody-content:image-fold",
            CancellationToken.None);

        var imageAddress = new ManagedDocumentContentAddress(
            caseId,
            root.Reference,
            Guid.NewGuid(),
            1,
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            DocumentSemanticRole.Image,
            "damage.jpg",
            "image/jpeg");
        await using var imageStream = await store.OpenReadVersionAsync(
            imageAddress,
            imageHash,
            image.LongLength,
            CancellationToken.None);
        using var imageCopy = new MemoryStream();
        await imageStream.CopyToAsync(imageCopy);
        Assert.Equal(image, imageCopy.ToArray());
    }

    [Fact]
    public async Task OpenReadVersionAsyncPreservesManagedFallbackAndFailureContracts()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            "Pegasus.IntegrationTests",
            Guid.NewGuid().ToString("N"));
        try
        {
            IDocumentContentStore store = new LocalDocumentContentStore(root);
            var caseId = Guid.NewGuid();
            var versionId = Guid.NewGuid();
            var content = "managed fallback content"u8.ToArray();
            var hash = Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();
            await store.StoreAsync(
                caseId,
                "QDOS31012",
                versionId,
                content,
                hash,
                CancellationToken.None);
            var address = new ManagedDocumentContentAddress(
                caseId,
                "QDOS31012",
                Guid.NewGuid(),
                0,
                Guid.NewGuid(),
                versionId,
                1,
                DocumentSemanticRole.Other,
                "upload.txt",
                "text/plain");

            await using var retained = await store.OpenReadVersionAsync(
                address,
                hash,
                content.LongLength,
                CancellationToken.None);
            using var copy = new MemoryStream();
            await retained.CopyToAsync(copy);
            Assert.Equal(content, copy.ToArray());

            var missing = address with { VersionId = Guid.NewGuid() };
            var exception = await Assert.ThrowsAsync<FileNotFoundException>(() =>
                store.OpenReadVersionAsync(
                    missing,
                    hash,
                    content.LongLength,
                    CancellationToken.None));
            Assert.Equal("The document content is unavailable.", exception.Message);

            await Assert.ThrowsAsync<InvalidDataException>(() =>
                store.OpenReadVersionAsync(
                    address,
                    new string('0', 64),
                    content.LongLength,
                    CancellationToken.None));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task StaffConfirmationOfThirdPartyVehicleEvidenceIsDurableAndExactlyReplayable()
    {
        var root = Path.Combine(Path.GetTempPath(), "Pegasus.IntegrationTests", Guid.NewGuid().ToString("N"));
        try
        {
            await using var database = await LocalDbTestDatabase.CreateAsync(
                localArtifactRootFactory: _ => root);
            var caseId = await SeedCaseAsync(database);
            var occurrenceId = await SeedCurrentImageAsync(database, caseId);
            await using var scope = database.CreateAsyncScope();
            var actor = ActionActor.Staff(Guid.NewGuid(), [StaffRole.Engineer]);
            var lease = await scope.ServiceProvider.GetRequiredService<ILeaseCaseForEdit>()
                .ClaimAsync(
                    new(caseId, 0, actor, $"third-party-image-lease:{Guid.NewGuid():N}"),
                    CancellationToken.None);
            var command = new ConfirmThirdPartyVehicleEvidenceCommand(
                caseId,
                occurrenceId,
                actor,
                "The retained image depicts the other vehicle.",
                $"third-party-image-confirmation:{Guid.NewGuid():N}",
                lease.Version,
                lease.Token);
            var confirmer = scope.ServiceProvider.GetRequiredService<IConfirmThirdPartyVehicleEvidence>();

            await confirmer.ExecuteAsync(command, CancellationToken.None);
            await confirmer.ExecuteAsync(command, CancellationToken.None);

            await using var verification = await database.CreateContextAsync();
            var occurrence = await verification.Set<DocumentOccurrenceEntity>()
                .SingleAsync(item => item.Id == occurrenceId);
            Assert.NotNull(occurrence.ThirdPartyVehicleConfirmedAtUtc);
            Assert.Equal(command.Reason, occurrence.ThirdPartyVehicleConfirmationReason);
            Assert.Equal(command.OperationKey, occurrence.ThirdPartyVehicleConfirmationOperationKey);
            var history = await verification.ActionHistory.SingleAsync(item =>
                item.CorrelationId == command.OperationKey);
            Assert.Equal("third_party_vehicle_evidence_confirmed", history.EventKind);
            Assert.Equal(command.Reason, history.Reason);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task CancelledContentWriteLeavesNoImmutableDestinationAndRetrySucceeds()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            "Pegasus.IntegrationTests",
            Guid.NewGuid().ToString("N"));
        try
        {
            var store = new LocalDocumentContentStore(root);
            var caseId = Guid.NewGuid();
            var versionId = Guid.NewGuid();
            var content = "complete managed document content"u8.ToArray();
            var sha256 = Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();
            using var cancellationSource = new CancellationTokenSource();
            cancellationSource.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                store.StoreAsync(
                    caseId,
                    "QDOS001",
                    versionId,
                    content,
                    sha256,
                    cancellationSource.Token));

            var directory = Path.Combine(
                root,
                "cases",
                "QDOS001",
                "managed",
                versionId.ToString("N"));
            Assert.False(File.Exists(Path.Combine(directory, "content")));
            Assert.Empty(Directory.EnumerateFiles(directory, "*.tmp"));

            await store.StoreAsync(
                caseId,
                "QDOS001",
                versionId,
                content,
                sha256,
                CancellationToken.None);

            await using var retained = await store.OpenReadAsync(
                caseId,
                "QDOS001",
                versionId,
                sha256,
                content.LongLength,
                CancellationToken.None);
            using var copy = new MemoryStream();
            await retained.CopyToAsync(copy);
            Assert.Equal(content, copy.ToArray());
            Assert.Empty(Directory.EnumerateFiles(directory, "*.tmp"));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task FailedDatabaseSaveRollsBackCaseAndRemovesUnreferencedContent()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            "Pegasus.IntegrationTests",
            Guid.NewGuid().ToString("N"));
        var interceptor = new FailNextDocumentSaveInterceptor();
        try
        {
            await using var database = await LocalDbTestDatabase.CreateAsync(
                configureDatabase: options => options.AddInterceptors(interceptor),
                localArtifactRootFactory: _ => root);
            var caseId = await SeedCaseAsync(database);
            await using var scope = database.CreateAsyncScope();
            var actor = ActionActor.Staff(Guid.NewGuid(), [StaffRole.Engineer]);
            var lease = await scope.ServiceProvider.GetRequiredService<ILeaseCaseForEdit>()
                .ClaimAsync(
                    new(
                        caseId,
                        ExpectedVersion: 0,
                        actor,
                        $"document-add-lease:{Guid.NewGuid():N}"),
                    CancellationToken.None);
            var command = new AddCaseDocumentCommand(
                caseId,
                "evidence.txt",
                "text/plain",
                "retained evidence"u8.ToArray(),
                DocumentSemanticRole.Other,
                DocumentSource.StaffUpload,
                $"durability:{Guid.NewGuid():N}",
                actor,
                $"document-add:{Guid.NewGuid():N}",
                lease.Version,
                lease.Token);
            var addDocument = scope.ServiceProvider.GetRequiredService<IAddCaseDocument>();
            interceptor.FailNextDocumentSave();

            await Assert.ThrowsAsync<DbUpdateException>(() =>
                addDocument.ExecuteAsync(command, CancellationToken.None));

            var managedDirectory = Path.Combine(
                root,
                "custody",
                "cases",
                "QDOS001",
                "managed");
            Assert.Empty(Directory.EnumerateFiles(
                managedDirectory,
                "content",
                SearchOption.AllDirectories));
            await using (var context = await database.CreateContextAsync())
            {
                Assert.Empty(await context.Set<DocumentVersionEntity>().ToArrayAsync());
                Assert.Equal(
                    3,
                    await context.Set<CaseEntity>()
                        .Where(value => value.Id == caseId)
                        .Select(value => value.Version)
                        .SingleAsync());
                Assert.Equal(
                    0,
                    await context.CaseWorkflows
                        .Where(value => value.CaseId == caseId)
                        .Select(value => value.Version)
                        .SingleAsync());
            }

            var added = await addDocument.ExecuteAsync(command, CancellationToken.None);

            Assert.False(added.IsReplay);
            Assert.Single(Directory.EnumerateFiles(
                managedDirectory,
                "content",
                SearchOption.AllDirectories));
            await using (var context = await database.CreateContextAsync())
            {
                Assert.Equal(
                    1,
                    await context.CaseWorkflows
                        .Where(value => value.CaseId == caseId)
                        .Select(value => value.Version)
                        .SingleAsync());
            }
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task FailedRequestUploadSaveRemovesUnreferencedContentBeforeSafeRetry()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            "Pegasus.IntegrationTests",
            Guid.NewGuid().ToString("N"));
        var interceptor = new FailNextDocumentSaveInterceptor();
        try
        {
            await using var database = await LocalDbTestDatabase.CreateAsync(
                configureDatabase: options => options.AddInterceptors(interceptor),
                localArtifactRootFactory: _ => root);
            var caseId = await SeedCaseAsync(database);
            var token = RequestUploadToken.Create();
            var requestId = Guid.NewGuid();
            var limits = new RequestUploadLimits(
                "durability-v1",
                TimeSpan.FromHours(1),
                2,
                1024,
                2048,
                ["text/plain"],
                10,
                TimeSpan.FromMinutes(1));
            var createdAtUtc = DateTimeOffset.UtcNow;
            await using (var context = await database.CreateContextAsync())
            {
                context.Add(new RequestUploadLinkEntity
                {
                    Id = requestId,
                    CaseId = caseId,
                    TokenDigest = token.TokenDigest,
                    Status = RequestUploadStatus.Active,
                    CreatedAtUtc = createdAtUtc,
                    ExpiresAtUtc = createdAtUtc.Add(limits.Lifetime),
                    LimitsVersion = limits.Version,
                    Version = 1,
                    CreateOperationKey = $"request-create:{Guid.NewGuid():N}"
                });
                await context.SaveChangesAsync();
            }

            await using var scope = database.CreateAsyncScope();
            var contextFactory = scope.ServiceProvider
                .GetRequiredService<IDbContextFactory<PegasusDbContext>>();
            var timeProvider = scope.ServiceProvider.GetRequiredService<TimeProvider>();
            IUploadToRequest upload = new EfDocumentRequestStore(
                contextFactory,
                scope.ServiceProvider.GetRequiredService<LocalDocumentContentStore>(),
                new RequestUploadPolicy(limits, timeProvider),
                limits,
                timeProvider);
            var command = new UploadToRequestCommand(
                token.Secret.Token,
                new(
                    "evidence.txt",
                    "text/plain",
                    "request upload evidence"u8.ToArray(),
                    $"request-file:{Guid.NewGuid():N}"),
                AttemptsInCurrentRateWindow: 1);
            interceptor.FailNextRequestUploadSave();

            await Assert.ThrowsAsync<DbUpdateException>(() =>
                upload.ExecuteAsync(command, CancellationToken.None));

            var managedDirectory = Path.Combine(
                root,
                "custody",
                "cases",
                "QDOS001",
                "managed");
            Assert.Empty(Directory.EnumerateFiles(
                managedDirectory,
                "content",
                SearchOption.AllDirectories));
            await using (var context = await database.CreateContextAsync())
            {
                Assert.Empty(await context.Set<RequestUploadReceiptEntity>().ToArrayAsync());
                Assert.Equal(
                    1,
                    await context.Set<RequestUploadLinkEntity>()
                        .Where(value => value.Id == requestId)
                        .Select(value => value.Version)
                        .SingleAsync());
                Assert.Equal(
                    0,
                    await context.CaseWorkflows
                        .Where(value => value.CaseId == caseId)
                        .Select(value => value.Version)
                        .SingleAsync());
            }

            var result = await upload.ExecuteAsync(command, CancellationToken.None);

            Assert.Equal(RequestUploadDecision.Accepted, result.Decision);
            Assert.Single(Directory.EnumerateFiles(
                managedDirectory,
                "content",
                SearchOption.AllDirectories));
            await using (var context = await database.CreateContextAsync())
            {
                Assert.Equal(
                    1,
                    await context.CaseWorkflows
                        .Where(value => value.CaseId == caseId)
                        .Select(value => value.Version)
                        .SingleAsync());
            }
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    private static async Task<Guid> SeedCaseAsync(LocalDbTestDatabase database)
    {
        await using var context = await database.CreateContextAsync();
        var organizationId = Guid.NewGuid();
        var sequenceLineageId = Guid.NewGuid();
        var principalId = Guid.NewGuid();
        var receiptId = Guid.NewGuid();
        var caseId = Guid.NewGuid();
        var occurredAtUtc = new DateTimeOffset(2031, 5, 6, 10, 30, 0, TimeSpan.Zero);
        context.AddRange(
            new OrganizationEntity
            {
                Id = organizationId,
                Name = "Durability test organization",
                Version = 0
            },
            new PrincipalSequenceLineageEntity
            {
                Id = sequenceLineageId,
                CreatedAtUtc = occurredAtUtc
            },
            new PrincipalEntity
            {
                Id = principalId,
                OrganizationId = organizationId,
                Code = "QDOS",
                SequenceLineageId = sequenceLineageId,
                IsActive = true,
                Version = 0
            },
            new IntakeReceiptEntity
            {
                Id = receiptId,
                SourceFileName = "durability.eml",
                MediaType = "message/rfc822",
                SourceLength = 1,
                SourceHash = new string('0', 64),
                SourceChannel = "manual_upload",
                ExternalReceiptToken = $"durability:{Guid.NewGuid():N}",
                ReceivedAtUtc = occurredAtUtc,
                ProcessedAtUtc = occurredAtUtc,
                SourceReaderKey = "durability-test",
                SourceReaderVersion = "1",
                Version = 0,
                Decision = "case_created",
                DecisionReason = "Durability test fixture.",
                EvidenceJson = "[]",
                FieldsJson = "[]",
                OcrCandidatesJson = "[]"
            },
            new CaseEntity
            {
                Id = caseId,
                PrincipalId = principalId,
                SequenceLineageId = sequenceLineageId,
                Year = 2031,
                Sequence = 1,
                Reference = "QDOS001",
                Type = "Inspection",
                InitialState = "NotReady",
                CustodyState = "Confirmed",
                OriginIntakeReceiptId = receiptId,
                CreatedAtUtc = occurredAtUtc,
                Version = 3,
                ConcurrencyToken = Guid.NewGuid()
            },
            new CaseWorkflowEntity
            {
                CaseId = caseId,
                State = "NotReady",
                Version = 0,
                ConcurrencyToken = Guid.NewGuid()
            });
        await context.SaveChangesAsync();
        return caseId;
    }

    private static async Task<Guid> SeedCurrentImageAsync(LocalDbTestDatabase database, Guid caseId)
    {
        await using var context = await database.CreateContextAsync();
        var documentId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var occurrenceId = Guid.NewGuid();
        context.AddRange(
            new CaseDocumentEntity
            {
                Id = documentId,
                CaseId = caseId,
                SourceOccurrenceIdentity = $"test-image:{occurrenceId:N}"
            },
            new DocumentVersionEntity
            {
                Id = versionId,
                DocumentId = documentId,
                Version = 1,
                FileName = "third-party.jpg",
                MediaType = "image/jpeg",
                ContentLength = 1,
                Sha256 = new string('a', 64),
                CustodyStatus = DocumentCustodyStatus.Confirmed,
                CreatedAtUtc = DateTimeOffset.UtcNow,
                CreatedBy = "Staff:test",
                IsCurrent = true
            },
            new DocumentOccurrenceEntity
            {
                Id = occurrenceId,
                CaseId = caseId,
                DocumentId = documentId,
                VersionId = versionId,
                SemanticRole = DocumentSemanticRole.Image,
                Source = DocumentSource.StaffUpload,
                SourceOccurrenceIdentity = $"test-image:{occurrenceId:N}",
                RecordedAtUtc = DateTimeOffset.UtcNow,
                OperationKey = $"seed-image:{occurrenceId:N}"
            });
        await context.SaveChangesAsync();
        return occurrenceId;
    }

    private sealed class FailNextDocumentSaveInterceptor : SaveChangesInterceptor
    {
        private int failNextDocumentSave;
        private int failNextRequestUploadSave;

        public void FailNextDocumentSave() =>
            Interlocked.Exchange(ref failNextDocumentSave, 1);

        public void FailNextRequestUploadSave() =>
            Interlocked.Exchange(ref failNextRequestUploadSave, 1);

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (Volatile.Read(ref failNextDocumentSave) == 1
                && eventData.Context is not null
                && eventData.Context.ChangeTracker.Entries<DocumentVersionEntity>()
                    .Any(entry => entry.State == EntityState.Added)
                && Interlocked.Exchange(ref failNextDocumentSave, 0) == 1)
            {
                throw new DbUpdateException("Injected document database failure.");
            }

            if (Volatile.Read(ref failNextRequestUploadSave) == 1
                && eventData.Context is not null
                && eventData.Context.ChangeTracker.Entries<RequestUploadReceiptEntity>()
                    .Any(entry => entry.State == EntityState.Added)
                && Interlocked.Exchange(ref failNextRequestUploadSave, 0) == 1)
            {
                throw new DbUpdateException("Injected request-upload database failure.");
            }

            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }

    private sealed class FixedArtifactStore(ReadOnlyMemory<byte> content) : IIntakeArtifactStore
    {
        public const string SourceObjectKey = "local-custody-source";

        public Task<string> StoreAsync(
            string contentHash,
            ReadOnlyMemory<byte> value,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ReadOnlyMemory<byte>?> ReadAsync(
            string storageKey,
            CancellationToken cancellationToken)
        {
            Assert.Equal(SourceObjectKey, storageKey);
            return Task.FromResult<ReadOnlyMemory<byte>?>(content);
        }
    }

    private sealed class MappingArtifactStore(
        IReadOnlyDictionary<string, ReadOnlyMemory<byte>> content) : IIntakeArtifactStore
    {
        public Task<string> StoreAsync(
            string contentHash,
            ReadOnlyMemory<byte> value,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ReadOnlyMemory<byte>?> ReadAsync(
            string storageKey,
            CancellationToken cancellationToken)
        {
            Assert.True(content.TryGetValue(storageKey, out var value));
            return Task.FromResult<ReadOnlyMemory<byte>?>(value);
        }
    }
}
