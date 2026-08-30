using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Pegasus.Contracts.Mail;
using Pegasus.Core.Identity;
using Pegasus.Core.Intake;
using Pegasus.Infrastructure.Persistence;
using Pegasus.Web.Api;

namespace Pegasus.IntegrationTests;

/// <summary>
/// Exercises the landed desktop mail gateway through HTTP and then reads the
/// SQL rows that the corresponding Core commands persist. The class stays as
/// one shard because all tests use the disposable LocalDB fixture.
/// </summary>
[Trait("Category", "SqlServer")]
public sealed class MailPersistenceTests
{
    private const string MailboxId = "instructions";
    private const string MailboxAddress = "instructions@collisionengineers.co.uk";
    private static readonly DateTimeOffset NowUtc =
        new(2031, 5, 6, 10, 30, 0, TimeSpan.Zero);
    private static readonly Guid ExpectedStaffId =
        Guid.Parse("d47fbbae-ea22-4ca6-b983-01e2ed1fbd13");

    [Fact]
    public async Task AssociationCommandsPersistTheirActorHistoryAndVersionIncrements()
    {
        using var baseFactory = new IntakeWebApplicationFactory(
            useIntegrationTestAuthentication: true);
        var messageId = await SeedRetainedMailAsync(baseFactory, "association");
        await StoreClassificationAsync(baseFactory, "association");
        var receiptId = await ReceiptIdAsync(baseFactory, "association");
        var caseId = await ImageIntakeTestData.SeedCaseAsync(
            baseFactory.Services,
            receiptId,
            "API31001",
            nameof(Pegasus.Core.Workflow.CaseLifecycleState.Review));
        var initialVersions = await ReadVersionsAsync(baseFactory, receiptId, caseId);

        using var factory = baseFactory.WithWebHostBuilder(builder =>
            builder.UseSetting(DesktopGateway.FeatureFlag, "true"));
        using var client = CreateClient(factory);

        using var prepareLink = await client.PostAsJsonAsync(
            $"/api/v1/mail/{messageId:D}/link-case/prepare",
            new MailCasePreparationRequest(
                caseId,
                initialVersions.IntakeVersion,
                initialVersions.CaseVersion,
                "association-lease"));
        Assert.Equal(HttpStatusCode.OK, prepareLink.StatusCode);
        var linkPreparation = await prepareLink.Content
            .ReadFromJsonAsync<MailCasePreparationResponse>();
        Assert.NotNull(linkPreparation);

        await using (var leaseContext = await baseFactory.Services
            .GetRequiredService<IDbContextFactory<PegasusDbContext>>()
            .CreateDbContextAsync())
        {
            var leaseOperation = await leaseContext.CaseEditLeaseOperations
                .AsNoTracking()
                .SingleAsync(item => item.CaseId == caseId
                    && item.OperationKey == "association-lease");
            Assert.Equal("claim", leaseOperation.OperationKind);
            Assert.Equal(nameof(ActorKind.Staff), leaseOperation.ActorKind);
            Assert.Equal(ExpectedStaffId.ToString("D"), leaseOperation.ActorSubjectId);
            Assert.Equal(initialVersions.CaseVersion, leaseOperation.ResultVersion);
            Assert.NotNull(leaseOperation.ResultTokenHash);
        }

        const string linkOperation = "api-association-link";
        const string linkReason = "The retained message names this exact Case/PO.";
        using var link = await client.PostAsJsonAsync(
            $"/api/v1/mail/{messageId:D}/link-case",
            new MailCaseAssociationRequest(
                caseId,
                linkPreparation.ExpectedIntakeVersion,
                linkPreparation.ExpectedCaseVersion,
                linkPreparation.LeaseToken,
                linkOperation,
                linkReason));
        Assert.Equal(HttpStatusCode.OK, link.StatusCode);
        var linked = await link.Content.ReadFromJsonAsync<MailCaseAssociationResponse>();
        Assert.NotNull(linked);
        Assert.Equal(initialVersions.IntakeVersion + 1, linked.Version);

        await using (var linkedContext = await baseFactory.Services
            .GetRequiredService<IDbContextFactory<PegasusDbContext>>()
            .CreateDbContextAsync())
        {
            var association = await linkedContext.IntakeManualAssociations
                .AsNoTracking()
                .SingleAsync(item => item.IntakeReceiptId == receiptId);
            Assert.True(association.IsActive);
            Assert.Equal(caseId, association.CaseId);
            Assert.Equal(nameof(ActorKind.Staff), association.ActorKind);
            Assert.Equal(ExpectedStaffId.ToString("D"), association.ActorSubjectId);
            Assert.Equal(linkReason, association.Reason);
            Assert.Equal(linkOperation, association.LastOperationKey);
            Assert.Equal(0, association.Version);

            var history = await linkedContext.IntakeMutationHistory
                .AsNoTracking()
                .SingleAsync(item => item.IntakeReceiptId == receiptId);
            Assert.Equal("intake_case_linked", history.EventType);
            Assert.Equal(nameof(ActorKind.Staff), history.ActorKind);
            Assert.Equal(ExpectedStaffId.ToString("D"), history.ActorSubjectId);
            Assert.Equal(initialVersions.IntakeVersion, history.BeforeIntakeVersion);
            Assert.Equal(initialVersions.IntakeVersion + 1, history.AfterIntakeVersion);
            Assert.Equal(initialVersions.CaseVersion, history.BeforeCaseVersion);
            Assert.Equal(initialVersions.CaseVersion + 1, history.AfterCaseVersion);

            var action = await linkedContext.ActionHistory
                .AsNoTracking()
                .SingleAsync(item => item.AggregateType == "mail_api"
                    && item.AggregateId == messageId.ToString("D")
                    && item.EventKind == "mail_case_link"
                    && item.CorrelationId == linkOperation);
            Assert.Equal(nameof(ActorKind.Staff), action.ActorKind);
            Assert.Equal(ExpectedStaffId.ToString("D"), action.ActorSubjectId);
            Assert.Equal("Succeeded", action.Outcome);
            Assert.Null(action.Reason);
        }

        using var prepareUnlink = await client.PostAsJsonAsync(
            $"/api/v1/mail/{messageId:D}/unlink-case/prepare",
            new MailCasePreparationRequest(
                caseId,
                initialVersions.IntakeVersion + 1,
                initialVersions.CaseVersion + 1,
                "association-unlink-lease"));
        Assert.Equal(HttpStatusCode.OK, prepareUnlink.StatusCode);
        var unlinkPreparation = await prepareUnlink.Content
            .ReadFromJsonAsync<MailCasePreparationResponse>();
        Assert.NotNull(unlinkPreparation);

        await using (var unlinkLeaseContext = await baseFactory.Services
            .GetRequiredService<IDbContextFactory<PegasusDbContext>>()
            .CreateDbContextAsync())
        {
            var unlinkLeaseOperation = await unlinkLeaseContext.CaseEditLeaseOperations
                .AsNoTracking()
                .SingleAsync(item => item.CaseId == caseId
                    && item.OperationKey == "association-unlink-lease");
            Assert.Equal("claim", unlinkLeaseOperation.OperationKind);
            Assert.Equal(nameof(ActorKind.Staff), unlinkLeaseOperation.ActorKind);
            Assert.Equal(ExpectedStaffId.ToString("D"), unlinkLeaseOperation.ActorSubjectId);
            Assert.Equal(initialVersions.CaseVersion + 1, unlinkLeaseOperation.ResultVersion);
            Assert.NotNull(unlinkLeaseOperation.ResultTokenHash);
        }

        const string unlinkOperation = "api-association-unlink";
        const string unlinkReason = "The message belongs to a different Case/PO.";
        using var unlink = await client.PostAsJsonAsync(
            $"/api/v1/mail/{messageId:D}/unlink-case",
            new MailCaseAssociationRequest(
                caseId,
                unlinkPreparation.ExpectedIntakeVersion,
                unlinkPreparation.ExpectedCaseVersion,
                unlinkPreparation.LeaseToken,
                unlinkOperation,
                unlinkReason));
        Assert.Equal(HttpStatusCode.OK, unlink.StatusCode);
        var unlinked = await unlink.Content.ReadFromJsonAsync<MailCaseAssociationResponse>();
        Assert.NotNull(unlinked);
        Assert.Equal(initialVersions.IntakeVersion + 2, unlinked.Version);

        await using var finalContext = await baseFactory.Services
            .GetRequiredService<IDbContextFactory<PegasusDbContext>>()
            .CreateDbContextAsync();
        var finalAssociation = await finalContext.IntakeManualAssociations
            .AsNoTracking()
            .SingleAsync(item => item.IntakeReceiptId == receiptId);
        Assert.False(finalAssociation.IsActive);
        Assert.Equal(1, finalAssociation.Version);
        Assert.Equal(nameof(ActorKind.Staff), finalAssociation.ActorKind);
        Assert.Equal(ExpectedStaffId.ToString("D"), finalAssociation.ActorSubjectId);
        Assert.Equal(unlinkReason, finalAssociation.Reason);
        Assert.Equal(unlinkOperation, finalAssociation.LastOperationKey);

        var historyRows = await finalContext.IntakeMutationHistory
            .AsNoTracking()
            .Where(item => item.IntakeReceiptId == receiptId)
            .OrderBy(item => item.AfterIntakeVersion)
            .ToArrayAsync();
        Assert.Equal(2, historyRows.Length);
        Assert.Equal("intake_case_link_reversed", historyRows[1].EventType);
        Assert.Equal(initialVersions.IntakeVersion + 1, historyRows[1].BeforeIntakeVersion);
        Assert.Equal(initialVersions.IntakeVersion + 2, historyRows[1].AfterIntakeVersion);
        Assert.Equal(ExpectedStaffId.ToString("D"), historyRows[1].ActorSubjectId);

        var unlinkAction = await finalContext.ActionHistory
            .AsNoTracking()
            .SingleAsync(item => item.AggregateType == "mail_api"
                && item.AggregateId == messageId.ToString("D")
                && item.EventKind == "mail_case_unlink"
                && item.CorrelationId == unlinkOperation);
        Assert.Equal(nameof(ActorKind.Staff), unlinkAction.ActorKind);
        Assert.Equal(ExpectedStaffId.ToString("D"), unlinkAction.ActorSubjectId);
        Assert.Equal("Succeeded", unlinkAction.Outcome);
        Assert.Null(unlinkAction.Reason);
    }

    [Fact]
    public async Task ConcurrentLinkCommandsCommitExactlyOneMutationAndReturnConflictToLoser()
    {
        using var baseFactory = new IntakeWebApplicationFactory(
            useIntegrationTestAuthentication: true);
        var messageId = await SeedRetainedMailAsync(baseFactory, "association-concurrency");
        await StoreClassificationAsync(baseFactory, "association-concurrency");
        var receiptId = await ReceiptIdAsync(baseFactory, "association-concurrency");
        var caseId = await ImageIntakeTestData.SeedCaseAsync(
            baseFactory.Services,
            receiptId,
            "API31002",
            nameof(Pegasus.Core.Workflow.CaseLifecycleState.Review));
        var initialVersions = await ReadVersionsAsync(baseFactory, receiptId, caseId);

        using var factory = baseFactory.WithWebHostBuilder(builder =>
            builder.UseSetting(DesktopGateway.FeatureFlag, "true"));
        using var firstClient = CreateClient(factory);
        using var secondClient = CreateClient(factory);

        using var prepare = await firstClient.PostAsJsonAsync(
            $"/api/v1/mail/{messageId:D}/link-case/prepare",
            new MailCasePreparationRequest(
                caseId,
                initialVersions.IntakeVersion,
                initialVersions.CaseVersion,
                "association-concurrency-lease"));
        Assert.Equal(HttpStatusCode.OK, prepare.StatusCode);
        var preparation = await prepare.Content
            .ReadFromJsonAsync<MailCasePreparationResponse>();
        Assert.NotNull(preparation);

        var firstRequest = new MailCaseAssociationRequest(
            caseId,
            initialVersions.IntakeVersion,
            initialVersions.CaseVersion,
            preparation.LeaseToken,
            "api-association-concurrency-first",
            "The first concurrent caller owns the association.");
        var secondRequest = firstRequest with
        {
            // The second caller presents the post-commit snapshot. Whether it
            // reaches the endpoint before or after the first request commits,
            // the two callers must not produce two mutations: it is rejected
            // by the version/association precondition with 409.
            ExpectedIntakeVersion = initialVersions.IntakeVersion + 1,
            ExpectedCaseVersion = initialVersions.CaseVersion + 1,
            OperationKey = "api-association-concurrency-second",
            Reason = "The second concurrent caller must be rejected."
        };
        var responses = await Task.WhenAll(
            firstClient.PostAsJsonAsync($"/api/v1/mail/{messageId:D}/link-case", firstRequest),
            secondClient.PostAsJsonAsync($"/api/v1/mail/{messageId:D}/link-case", secondRequest));

        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.OK);
        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Conflict);
        var successfulOperationKey = responses[0].StatusCode == HttpStatusCode.OK
            ? firstRequest.OperationKey
            : secondRequest.OperationKey;

        await using var context = await baseFactory.Services
            .GetRequiredService<IDbContextFactory<PegasusDbContext>>()
            .CreateDbContextAsync();
        var association = await context.IntakeManualAssociations
            .AsNoTracking()
            .SingleAsync(item => item.IntakeReceiptId == receiptId);
        Assert.True(association.IsActive);
        Assert.Equal(caseId, association.CaseId);
        Assert.Equal(0, association.Version);

        var receipt = await context.IntakeReceipts
            .AsNoTracking()
            .SingleAsync(item => item.Id == receiptId);
        var workflow = await context.CaseWorkflows
            .AsNoTracking()
            .SingleAsync(item => item.CaseId == caseId);
        Assert.Equal(initialVersions.IntakeVersion + 1, receipt.Version);
        Assert.Equal(initialVersions.CaseVersion + 1, workflow.Version);

        Assert.Equal(
            1,
            await context.IntakeMutationHistory
                .AsNoTracking()
                .CountAsync(item => item.IntakeReceiptId == receiptId));
        var actions = await context.ActionHistory
            .AsNoTracking()
            .Where(item => item.AggregateType == "mail_api"
                && item.AggregateId == messageId.ToString("D")
                && item.EventKind == "mail_case_link")
            .ToArrayAsync();
        Assert.Contains(actions, item => item.Outcome == "Succeeded"
            && item.CorrelationId == successfulOperationKey
            && item.ActorSubjectId == ExpectedStaffId.ToString("D"));
        Assert.Single(actions, item => item.Outcome == "Succeeded");
    }

    [Fact]
    public async Task ClassificationCommandPersistsCorrectionHistoryAndIncrementsVersion()
    {
        using var baseFactory = new IntakeWebApplicationFactory(
            useIntegrationTestAuthentication: true);
        var messageId = await SeedRetainedMailAsync(baseFactory, "classification");
        await StoreClassificationAsync(baseFactory, "classification");

        using var factory = baseFactory.WithWebHostBuilder(builder =>
            builder.UseSetting(DesktopGateway.FeatureFlag, "true"));
        using var client = CreateClient(factory);

        const string reason = "The retained reply is an acknowledgement.";
        const string operationKey = "api-classification-correction";
        using var response = await client.PostAsJsonAsync(
            $"/api/v1/mail/{messageId:D}/classification",
            new MailClassificationCorrectionRequest(
                1,
                "received:General:acknowledgement",
                reason,
                operationKey));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<MailClassificationResponse>();
        Assert.NotNull(result);
        Assert.Equal(2, result.Version);
        var responseHistory = Assert.Single(result.History);
        Assert.Equal(reason, responseHistory.Reason);

        var receiptId = await ReceiptIdAsync(baseFactory, "classification");
        await using var context = await baseFactory.Services
            .GetRequiredService<IDbContextFactory<PegasusDbContext>>()
            .CreateDbContextAsync();
        var decision = await context.IntakeMailClassificationDecisions
            .AsNoTracking()
            .SingleAsync(item => item.IntakeReceiptId == receiptId);
        Assert.Equal(2, decision.Version);
        Assert.Equal("staff:" + ExpectedStaffId.ToString("D"), decision.DecidedByActor);

        var history = await context.IntakeMailClassificationHistory
            .AsNoTracking()
            .SingleAsync(item => item.IntakeReceiptId == receiptId);
        Assert.Equal(2, history.Version);
        Assert.Equal("staff:" + ExpectedStaffId.ToString("D"), history.Actor);
        Assert.Equal(reason, history.Reason);

        var action = await context.ActionHistory
            .AsNoTracking()
            .SingleAsync(item => item.CorrelationId == operationKey);
        Assert.Equal("mail_classification_correction", action.EventKind);
        Assert.Equal(nameof(ActorKind.Staff), action.ActorKind);
        Assert.Equal(ExpectedStaffId.ToString("D"), action.ActorSubjectId);
        Assert.Equal("Succeeded", action.Outcome);
    }

    [Fact]
    public async Task FolderMoveCommandPersistsTheMoveAndAuditActorAtTheExpectedVersions()
    {
        var mover = new RecordingFolderMover();
        using var baseFactory = new IntakeWebApplicationFactory(
            useIntegrationTestAuthentication: true);
        var messageId = await SeedRetainedMailAsync(baseFactory, "folder-move");
        await StoreClassifiedInstructionAsync(baseFactory, "folder-move");
        var mailboxVersion = await ConfigureFolderBindingAsync(
            baseFactory,
            MailLogicalFolderType.Instructions,
            "outlook-folder-instructions");

        using var factory = baseFactory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting(DesktopGateway.FeatureFlag, "true");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IRetainedMailFolderMover>();
                services.AddSingleton<IRetainedMailFolderMover>(mover);
            });
        });
        using var client = CreateClient(factory);

        const string operationKey = "4e5f2df8-91f0-4d6a-8b79-2ecf5f8d7f31";
        const string reason = "Confirmed after reviewing the retained message.";
        using var response = await client.PostAsJsonAsync(
            $"/api/v1/mail/{messageId:D}/move-to-recommended-folder",
            new MailMoveRequest(
                1,
                "mail_logical_folder",
                1,
                mailboxVersion,
                operationKey,
                reason));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<MailFolderMoveResponse>();
        Assert.NotNull(result);
        Assert.Equal("Succeeded", result.Outcome);
        Assert.Equal("Instructions", result.FolderType);
        Assert.Equal(operationKey, result.OperationKey);
        Assert.Equal(1, mover.MoveCalls);
        Assert.Equal("inbox", mover.Coordinates!.SourceFolderId);
        Assert.Equal("outlook-folder-instructions", mover.Coordinates.DestinationFolderId);

        await using var context = await baseFactory.Services
            .GetRequiredService<IDbContextFactory<PegasusDbContext>>()
            .CreateDbContextAsync();
        var operation = await context.RetainedMailFolderMoves
            .AsNoTracking()
            .SingleAsync(item => item.OperationKey == operationKey);
        Assert.Equal("succeeded", operation.Outcome);
        Assert.Equal(1, operation.ExpectedClassificationVersion);
        Assert.Equal("mail_logical_folder", operation.ExpectedRecommendationPolicyKey);
        Assert.Equal(1, operation.ExpectedRecommendationPolicyVersion);
        Assert.Equal(mailboxVersion, operation.ExpectedMailboxVersion);
        Assert.Equal("staff:" + ExpectedStaffId.ToString("D"), operation.Actor);
        Assert.Equal(reason, operation.Reason);

        var actions = await context.ActionHistory
            .AsNoTracking()
            .Where(item => item.CorrelationId == operationKey)
            .ToArrayAsync();
        Assert.Equal(2, actions.Length);
        Assert.Contains(actions, item => item.EventKind == "outlook-folder-move");
        Assert.Contains(actions, item => item.EventKind == "mail_folder_move");
        Assert.All(actions, action =>
        {
            Assert.Equal(
                action.EventKind == "outlook-folder-move" ? "succeeded" : "Succeeded",
                action.Outcome);
            Assert.Equal(
                action.EventKind == "outlook-folder-move" ? "staff" : nameof(ActorKind.Staff),
                action.ActorKind);
            Assert.Equal(ExpectedStaffId.ToString("D"), action.ActorSubjectId);
        });
    }

    private static HttpClient CreateClient(WebApplicationFactory<Program> factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost:7139")
        });

    private static async Task<Guid> SeedRetainedMailAsync(
        IntakeWebApplicationFactory factory,
        string immutableMessageId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var contextFactory = scope.ServiceProvider
            .GetRequiredService<IDbContextFactory<PegasusDbContext>>();
        await using (var context = await contextFactory.CreateDbContextAsync())
        {
            if (!await context.ApprovedInboxPollStates.AnyAsync(item => item.MailboxId == MailboxId))
            {
                context.ApprovedInboxPollStates.Add(new()
                {
                    MailboxId = MailboxId,
                    MailboxAddress = MailboxAddress,
                    DueAtUtc = NowUtc,
                    LastCompletedAtUtc = NowUtc.AddMinutes(-1)
                });
                await context.SaveChangesAsync();
            }
        }

        var store = scope.ServiceProvider.GetRequiredService<EfRetainedMailboxMessageStore>();
        await store.RetainAsync(
            new(
                MailboxId,
                MailboxAddress,
                immutableMessageId,
                $"{MailboxId.Length}:{MailboxId}{immutableMessageId}",
                NowUtc.AddMinutes(-1),
                1024,
                new string('A', 64),
                new(
                    "inbox",
                    "conversation-api",
                    $"<{immutableMessageId}@example.invalid>",
                    "sender@example.invalid",
                    "A Sender",
                    ["intake@collisionengineers.co.uk"],
                    [],
                    $"Message {immutableMessageId}",
                    "Please inspect the vehicle at the address supplied.",
                    [new("estimate.pdf", "application/pdf", 2048)],
                    IsRead: false),
                NowUtc),
            CancellationToken.None);

        await using var readContext = await contextFactory.CreateDbContextAsync();
        return await readContext.RetainedMailboxMessages
            .Where(item => item.MailboxId == MailboxId
                && item.ImmutableMessageId == immutableMessageId)
            .Select(item => item.Id)
            .SingleAsync();
    }

    private static async Task<Guid> ReceiptIdAsync(
        IntakeWebApplicationFactory factory,
        string immutableMessageId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<PegasusDbContext>>();
        await using var context = await contextFactory.CreateDbContextAsync();
        var token = $"{MailboxId.Length}:{MailboxId}{immutableMessageId}";
        return await context.IntakeReceipts
            .Where(item => item.SourceChannel == "mailbox"
                && item.ExternalReceiptToken == token)
            .Select(item => item.Id)
            .SingleAsync();
    }

    private static async Task<(long IntakeVersion, long CaseVersion)> ReadVersionsAsync(
        IntakeWebApplicationFactory factory,
        Guid receiptId,
        Guid caseId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var contextFactory = scope.ServiceProvider
            .GetRequiredService<IDbContextFactory<PegasusDbContext>>();
        await using var context = await contextFactory.CreateDbContextAsync();
        return (
            await context.IntakeReceipts
                .Where(item => item.Id == receiptId)
                .Select(item => item.Version)
                .SingleAsync(),
            await context.CaseWorkflows
                .Where(item => item.CaseId == caseId)
                .Select(item => item.Version)
                .SingleAsync());
    }

    private static async Task StoreClassificationAsync(
        IntakeWebApplicationFactory factory,
        string immutableMessageId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IIntakeReceiptStore>().StoreAsync(
            new(
                SourceFileName: "api-mail.eml",
                MediaType: "message/rfc822",
                SourceLength: 1,
                SourceHash: new string('D', 64),
                SourceIdentity: new(
                    IntakeSourceChannel.Mailbox,
                    $"{MailboxId.Length}:{MailboxId}{immutableMessageId}"),
                ReceivedAtUtc: NowUtc,
                ProcessedAtUtc: NowUtc,
                Actor: "system-worker:approved-inbox-poller",
                Decision: IntakeDecision.NeedsSorting,
                DecisionReason: "API persistence fixture.",
                Evidence: [],
                Fields: [],
                InstructionDraft: null,
                MissingFields: [],
                FailureCode: null,
                FailureReason: null,
                SourceReaderKey: "protocol_reader",
                SourceReaderVersion: "1",
                ExtractionPolicyKey: "protocol_policy",
                ExtractionPolicyVersion: 1,
                Assets: [],
                MailClassificationDecision: MailClassificationResult.Unclassified(
                    [new(
                        "sender-domain",
                        false,
                        "The sender domain is not recognized.")],
                    "No supported category matched.",
                    "shared-mail-policy",
                    3)),
            CancellationToken.None);
    }

    private static async Task StoreClassifiedInstructionAsync(
        IntakeWebApplicationFactory factory,
        string immutableMessageId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IIntakeReceiptStore>().StoreAsync(
            new(
                SourceFileName: "api-instruction.eml",
                MediaType: "message/rfc822",
                SourceLength: 1,
                SourceHash: new string('E', 64),
                SourceIdentity: new(
                    IntakeSourceChannel.Mailbox,
                    $"{MailboxId.Length}:{MailboxId}{immutableMessageId}"),
                ReceivedAtUtc: NowUtc,
                ProcessedAtUtc: NowUtc,
                Actor: "system-worker:approved-inbox-poller",
                Decision: IntakeDecision.NeedsSorting,
                DecisionReason: "API persistence fixture.",
                Evidence: [],
                Fields: [],
                InstructionDraft: null,
                MissingFields: [],
                FailureCode: null,
                FailureReason: null,
                SourceReaderKey: "protocol_reader",
                SourceReaderVersion: "1",
                ExtractionPolicyKey: "protocol_policy",
                ExtractionPolicyVersion: 1,
                Assets: [],
                MailClassificationDecision: MailClassificationResult.Classified(
                    MailCategory.Received(
                        ReceivedMailFamily.NewInstructionReceived,
                        "inspection"),
                    [new(
                        "attachment.engineer-notification",
                        true,
                        "An attached document contains the generated title.")],
                    "An accepted Inspection instruction was recognised.",
                    "qdos_mail_classification",
                    3)),
            CancellationToken.None);
    }

    private static async Task<int> ConfigureFolderBindingAsync(
        IntakeWebApplicationFactory factory,
        MailLogicalFolderType folderType,
        string folderIdentity)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<PegasusDbContext>>();
        await using var context = await contextFactory.CreateDbContextAsync();
        var mailbox = await context.ApprovedMailboxes
            .Include(item => item.FolderBindings)
            .SingleAsync(item => item.Address == MailboxAddress);
        mailbox.MailboxIdentity = MailboxId;
        mailbox.InboxFolderIdentity = "inbox";
        mailbox.SentFolderIdentity = "sent";
        mailbox.Version++;
        mailbox.FolderBindings.Add(new()
        {
            ApprovedMailboxId = mailbox.Id,
            ApprovedMailbox = mailbox,
            FolderType = folderType.ToString(),
            FolderIdentity = folderIdentity
        });
        await context.SaveChangesAsync();
        return mailbox.Version;
    }

    private sealed class RecordingFolderMover : IRetainedMailFolderMover
    {
        public bool IsAvailable => true;
        public int MoveCalls { get; private set; }
        public RetainedMailFolderMoveCoordinates? Coordinates { get; private set; }

        public Task MoveAsync(
            RetainedMailFolderMoveCoordinates coordinates,
            CancellationToken cancellationToken)
        {
            MoveCalls++;
            Coordinates = coordinates;
            return Task.CompletedTask;
        }

        public Task<string?> GetParentFolderIdAsync(
            string mailboxId,
            string immutableMessageId,
            CancellationToken cancellationToken) =>
            Task.FromResult<string?>("inbox");
    }
}
