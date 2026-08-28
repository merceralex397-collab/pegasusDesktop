using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pegasus.Core.Identity;
using Pegasus.Core.Intake;
using Pegasus.Infrastructure.Persistence;

namespace Pegasus.IntegrationTests;

[Trait("Category", "SqlServer")]
public sealed class IntakeReevaluationPersistenceTests
{
    private static readonly DateTimeOffset FixedUtcNow =
        new(2031, 5, 6, 10, 30, 0, TimeSpan.Zero);

    [Fact]
    public async Task ReevaluateRestagesRetainedSourceBeforeQueueingAndReplaysWithoutDuplicateStage()
    {
        var content = Encoding.UTF8.GetBytes("retained source");
        var sourceKey = "retained/source.eml";
        var artifactStore = new RecordingArtifactStore(sourceKey, content);
        using var factory = new IntakeWebApplicationFactory(
            environment: "Development",
            localIntakeEnabled: true,
            timeProvider: new FixedTimeProvider(FixedUtcNow),
            artifactStore: artifactStore);
        var receipt = await SeedCompletedEvaluationAsync(factory, content, sourceKey);
        var request = ReevaluateRequest(receipt.Id, receipt.Version);

        await using var scope = factory.Services.CreateAsyncScope();
        var command = scope.ServiceProvider.GetRequiredService<IReevaluateIntake>();

        var result = await command.ExecuteAsync(request);
        var replay = await command.ExecuteAsync(request);

        Assert.Equal(IntakeDecision.BlockedIntake, result.Decision);
        Assert.Equal("reevaluation_pending", result.FailureCode);
        Assert.Equal(result.Id, replay.Id);
        Assert.Equal(result.Version, replay.Version);
        Assert.Equal(result.Decision, replay.Decision);
        Assert.Equal(result.FailureCode, replay.FailureCode);
        var stage = Assert.Single(artifactStore.StageCalls);
        Assert.NotEqual(Guid.Empty, stage.StagedReceiptId);
        Assert.Equal(receipt.SourceHash, stage.ContentHash);
        Assert.Equal(content, stage.Content.ToArray());

        await using var context = await factory.Database.CreateContextAsync();
        var persistedReceipt = await context.IntakeReceipts.SingleAsync(item => item.Id == receipt.Id);
        var workItem = await context.IntakeWorkItems.SingleAsync(item => item.StagedReceiptId == stage.StagedReceiptId);
        var history = await context.IntakeMutationHistory
            .Where(item => item.IntakeReceiptId == receipt.Id)
            .ToListAsync();

        Assert.Equal(1, persistedReceipt.Version);
        Assert.Equal("blocked_intake", persistedReceipt.Decision);
        Assert.Equal("reevaluation_pending", persistedReceipt.FailureCode);
        Assert.Equal("pending", workItem.State);
        Assert.Null(workItem.LeaseToken);
        Assert.Single(history);
        Assert.Equal("intake_reevaluation_queued", history[0].EventType);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ReevaluateWithMissingOrCorruptRetainedSourceLeavesReceiptWorkAndHistoryUntouched(
        bool returnCorruptContent)
    {
        var content = Encoding.UTF8.GetBytes("retained source");
        var sourceKey = "retained/missing.eml";
        var artifactStore = new RecordingArtifactStore(
            sourceKey,
            returnCorruptContent ? Encoding.UTF8.GetBytes("corrupt source") : null);
        using var factory = new IntakeWebApplicationFactory(
            environment: "Development",
            localIntakeEnabled: true,
            timeProvider: new FixedTimeProvider(FixedUtcNow),
            artifactStore: artifactStore);
        var receipt = await SeedCompletedEvaluationAsync(factory, content, sourceKey);
        var request = ReevaluateRequest(receipt.Id, receipt.Version);

        await using var scope = factory.Services.CreateAsyncScope();
        var command = scope.ServiceProvider.GetRequiredService<IReevaluateIntake>();

        await Assert.ThrowsAsync<IntakeArtifactIntegrityException>(
            () => command.ExecuteAsync(request));

        await using var context = await factory.Database.CreateContextAsync();
        var persistedReceipt = await context.IntakeReceipts.SingleAsync(item => item.Id == receipt.Id);
        var workItem = await context.IntakeWorkItems.SingleAsync(item => item.StagedReceiptId != Guid.Empty);
        var historyCount = await context.IntakeMutationHistory
            .CountAsync(item => item.IntakeReceiptId == receipt.Id);

        Assert.Equal(0, persistedReceipt.Version);
        Assert.Equal("blocked_intake", persistedReceipt.Decision);
        Assert.Equal("completed", workItem.State);
        Assert.Equal(0, historyCount);
        Assert.Empty(artifactStore.StageCalls);
    }

    [Theory]
    [InlineData("processing")]
    [InlineData("dispatching")]
    public async Task ReevaluateWithActiveLeaseDoesNotRestageOrMutate(string activeState)
    {
        var content = Encoding.UTF8.GetBytes("retained source");
        var sourceKey = "retained/leased.eml";
        var artifactStore = new RecordingArtifactStore(sourceKey, content);
        using var factory = new IntakeWebApplicationFactory(
            environment: "Development",
            localIntakeEnabled: true,
            timeProvider: new FixedTimeProvider(FixedUtcNow),
            artifactStore: artifactStore);
        var receipt = await SeedCompletedEvaluationAsync(factory, content, sourceKey);

        await using (var context = await factory.Database.CreateContextAsync())
        {
            var workItem = await context.IntakeWorkItems.SingleAsync(item => item.ProcessedReceiptId == receipt.Id);
            workItem.State = activeState;
            workItem.LeaseToken = "active-lease";
            workItem.LeaseExpiresAtUtc = FixedUtcNow.AddMinutes(5);
            await context.SaveChangesAsync();
        }

        await using var scope = factory.Services.CreateAsyncScope();
        var command = scope.ServiceProvider.GetRequiredService<IReevaluateIntake>();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => command.ExecuteAsync(ReevaluateRequest(receipt.Id, receipt.Version)));

        await using var verification = await factory.Database.CreateContextAsync();
        var persistedReceipt = await verification.IntakeReceipts.SingleAsync(item => item.Id == receipt.Id);
        var workItemAfter = await verification.IntakeWorkItems.SingleAsync(item => item.ProcessedReceiptId == receipt.Id);
        Assert.Equal(0, persistedReceipt.Version);
        Assert.Equal(activeState, workItemAfter.State);
        Assert.Equal("active-lease", workItemAfter.LeaseToken);
        Assert.Empty(artifactStore.StageCalls);
    }

    [Fact]
    public async Task ReevaluateWithAmbiguousRetainedSourceFailsClosedBeforeMutation()
    {
        var content = Encoding.UTF8.GetBytes("retained source");
        var sourceKey = "retained/ambiguous.eml";
        var artifactStore = new RecordingArtifactStore(sourceKey, content);
        using var factory = new IntakeWebApplicationFactory(
            environment: "Development",
            localIntakeEnabled: true,
            timeProvider: new FixedTimeProvider(FixedUtcNow),
            artifactStore: artifactStore);
        var receipt = await SeedCompletedEvaluationAsync(factory, content, sourceKey);

        await using (var context = await factory.Database.CreateContextAsync())
        {
            var sourceAsset = await context.IntakeAssets.SingleAsync(item => item.IntakeReceiptId == receipt.Id);
            context.IntakeAssets.Add(new IntakeAssetEntity
            {
                Id = Guid.NewGuid(),
                IntakeReceiptId = receipt.Id,
                SourceLabel = sourceAsset.SourceLabel,
                FileName = sourceAsset.FileName,
                MediaType = sourceAsset.MediaType,
                Kind = sourceAsset.Kind,
                Disposition = sourceAsset.Disposition,
                ContentLength = sourceAsset.ContentLength,
                ContentHash = sourceAsset.ContentHash,
                StorageKey = sourceAsset.StorageKey
            });
            await context.SaveChangesAsync();
        }

        await using var scope = factory.Services.CreateAsyncScope();
        var command = scope.ServiceProvider.GetRequiredService<IReevaluateIntake>();

        await Assert.ThrowsAsync<IntakeArtifactIntegrityException>(
            () => command.ExecuteAsync(ReevaluateRequest(receipt.Id, receipt.Version)));

        await AssertUnchangedAsync(factory, receipt.Id, artifactStore, "completed");
    }

    [Fact]
    public async Task ReevaluateWhenRestageFailsLeavesReceiptWorkAndHistoryUntouched()
    {
        var content = Encoding.UTF8.GetBytes("retained source");
        var sourceKey = "retained/restage-failure.eml";
        var artifactStore = new RecordingArtifactStore(sourceKey, content)
        {
            StageFailure = new InvalidOperationException("test staging failure")
        };
        using var factory = new IntakeWebApplicationFactory(
            environment: "Development",
            localIntakeEnabled: true,
            timeProvider: new FixedTimeProvider(FixedUtcNow),
            artifactStore: artifactStore);
        var receipt = await SeedCompletedEvaluationAsync(factory, content, sourceKey);

        await using var scope = factory.Services.CreateAsyncScope();
        var command = scope.ServiceProvider.GetRequiredService<IReevaluateIntake>();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => command.ExecuteAsync(ReevaluateRequest(receipt.Id, receipt.Version)));

        await AssertUnchangedAsync(factory, receipt.Id, artifactStore, "completed");
    }

    private static async Task AssertUnchangedAsync(
        IntakeWebApplicationFactory factory,
        Guid receiptId,
        RecordingArtifactStore artifactStore,
        string expectedWorkState)
    {
        await using var context = await factory.Database.CreateContextAsync();
        var persistedReceipt = await context.IntakeReceipts.SingleAsync(item => item.Id == receiptId);
        var workItem = await context.IntakeWorkItems.SingleAsync(item => item.ProcessedReceiptId == receiptId);
        var historyCount = await context.IntakeMutationHistory
            .CountAsync(item => item.IntakeReceiptId == receiptId);

        Assert.Equal(0, persistedReceipt.Version);
        Assert.Equal(expectedWorkState, workItem.State);
        Assert.Equal(0, historyCount);
        Assert.Empty(artifactStore.StageCalls);
    }

    private static ReevaluateIntakeRequest ReevaluateRequest(Guid receiptId, long version) =>
        new(
            receiptId,
            version,
            ActionActor.Staff(Guid.NewGuid(), [StaffRole.User]),
            "reevaluate-operation",
            "Re-evaluate after policy change");

    private static async Task<IntakeReceipt> SeedCompletedEvaluationAsync(
        IntakeWebApplicationFactory factory,
        byte[] content,
        string sourceKey)
    {
        var sourceHash = Convert.ToHexString(SHA256.HashData(content));
        var externalToken = "reevaluate-source-token";
        await using var scope = factory.Services.CreateAsyncScope();
        var receipt = await scope.ServiceProvider
            .GetRequiredService<IIntakeReceiptStore>()
            .StoreAsync(
                new IntakeReceiptDraft(
                    SourceFileName: "source.eml",
                    MediaType: "message/rfc822",
                    SourceLength: content.Length,
                    SourceHash: sourceHash,
                    SourceIdentity: new(IntakeSourceChannel.Mailbox, externalToken),
                    ReceivedAtUtc: FixedUtcNow.AddMinutes(-5),
                    ProcessedAtUtc: FixedUtcNow.AddMinutes(-4),
                    Actor: "test-actor",
                    Decision: IntakeDecision.BlockedIntake,
                    DecisionReason: "Initial completed evaluation",
                    Evidence: [],
                    Fields: [],
                    InstructionDraft: null,
                    MissingFields: [],
                    FailureCode: null,
                    FailureReason: null,
                    SourceReaderKey: "test-reader",
                    SourceReaderVersion: "1",
                    ExtractionPolicyKey: null,
                    ExtractionPolicyVersion: null,
                    Assets:
                    [
                        new(
                            Guid.NewGuid(),
                            "Original source",
                            "source.eml",
                            "message/rfc822",
                            IntakeAssetKind.Source,
                            IntakeAssetDisposition.Source,
                            content.Length,
                            sourceHash,
                            sourceKey,
                            null,
                            null,
                            null,
                            null)
                    ]),
                CancellationToken.None);

        var stagedReceiptId = Guid.NewGuid();
        await using var context = await factory.Database.CreateContextAsync();
        context.IntakeStagedReceipts.Add(new IntakeStagedReceiptEntity
        {
            Id = stagedReceiptId,
            SourceFileName = "source.eml",
            MediaType = "message/rfc822",
            SourceLength = content.Length,
            SourceHash = sourceHash,
            SourceChannel = "mailbox",
            ExternalReceiptToken = externalToken,
            ReceivedAtUtc = FixedUtcNow.AddMinutes(-5),
            Actor = "test-actor",
            StorageKey = "staging/source.eml",
            StagedAtUtc = FixedUtcNow.AddMinutes(-5)
        });
        context.IntakeWorkItems.Add(new IntakeWorkItemEntity
        {
            Id = Guid.NewGuid(),
            StagedReceiptId = stagedReceiptId,
            OperationKey = "initial-processing-operation",
            State = "completed",
            AttemptCount = 1,
            DueAtUtc = FixedUtcNow.AddMinutes(-4),
            ProcessedReceiptId = receipt.Id,
            CompletedAtUtc = FixedUtcNow.AddMinutes(-4)
        });
        context.IntakeEvaluations.Add(new IntakeEvaluationEntity
        {
            Id = Guid.NewGuid(),
            StagedReceiptId = stagedReceiptId,
            ProcessedReceiptId = receipt.Id,
            Revision = 1,
            EvaluatedAtUtc = FixedUtcNow.AddMinutes(-4)
        });
        await context.SaveChangesAsync();
        return receipt;
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private sealed class RecordingArtifactStore(string sourceKey, byte[]? sourceContent)
        : IIntakeArtifactStore
    {
        private readonly byte[]? durableSource = sourceContent?.ToArray();

        public List<StageCall> StageCalls { get; } = [];

        public Exception? StageFailure { get; init; }

        public Task<string> StoreAsync(
            string contentHash,
            ReadOnlyMemory<byte> content,
            CancellationToken cancellationToken) =>
            Task.FromResult("stored/" + contentHash);

        public Task<ReadOnlyMemory<byte>?> ReadAsync(
            string storageKey,
            CancellationToken cancellationToken) =>
            Task.FromResult<ReadOnlyMemory<byte>?>(
                string.Equals(storageKey, sourceKey, StringComparison.Ordinal)
                    ? durableSource
                    : null);

        public Task<StagedArtifactInventoryItem> StageAsync(
            Guid stagedReceiptId,
            string contentHash,
            ReadOnlyMemory<byte> content,
            DateTimeOffset firstSeenAtUtc,
            CancellationToken cancellationToken)
        {
            if (StageFailure is { } exception)
            {
                throw exception;
            }

            StageCalls.Add(new(stagedReceiptId, contentHash, content.ToArray()));
            return Task.FromResult(new StagedArtifactInventoryItem(
                "staging/" + stagedReceiptId.ToString("D"),
                contentHash,
                content.Length,
                firstSeenAtUtc,
                StagedArtifactDisposition.Pending,
                "test-token"));
        }

    }

    private sealed record StageCall(
        Guid StagedReceiptId,
        string ContentHash,
        byte[] Content);
}
