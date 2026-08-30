using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Pegasus.Contracts;
using Pegasus.Contracts.Mail;
using Pegasus.Contracts.ProblemDetails;
using Pegasus.Core.Cases;
using Pegasus.Core.Identity;
using Pegasus.Core.Intake;
using Pegasus.Core.Workflow;

namespace Pegasus.Api.ContractTests;

public sealed class MailGatewayContractTests
{
    [Fact]
    public async Task ListAndDetailExposeCoreDataAndSupportConditionalReads()
    {
        using var factory = new MailContractTestWebApplicationFactory();
        using var client = factory.CreateClient();

        using var listResponse = await client.GetAsync("/api/v1/mail?mailbox=mailbox-1&folder=inbox");

        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var page = await listResponse.Content.ReadFromJsonAsync<MailPageResponse>();
        Assert.NotNull(page);
        Assert.Equal("mailbox-1", page.Items[0].MailboxId);
        Assert.Equal(1, page.Items[0].IntakeVersion);
        Assert.Null(page.Items[0].CaseVersion);
        Assert.Equal("Current", page.Freshness.State);
        Assert.Equal(MailFolderScope.Inbox, factory.State.Queries.LastScope!.Folder);

        var messageId = MailContractTestState.MessageId;
        using var detailResponse = await client.GetAsync($"/api/v1/mail/{messageId:D}");

        Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);
        Assert.True(detailResponse.Headers.TryGetValues("ETag", out var etags));
        var etag = Assert.Single(etags);
        Assert.Contains("W/\"", etag, StringComparison.Ordinal);
        var detail = await detailResponse.Content.ReadFromJsonAsync<MailDetailResponse>();
        Assert.NotNull(detail);
        Assert.False(detail.FolderRecommendation!.CanMove);
        Assert.Null(detail.SuggestedMove);
        Assert.Equal(1, detail.Summary.IntakeVersion);
        Assert.Null(detail.Summary.CaseVersion);

        using var conditionalRequest = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/v1/mail/{messageId:D}");
        conditionalRequest.Headers.TryAddWithoutValidation("If-None-Match", etag);
        using var conditionalResponse = await client.SendAsync(conditionalRequest);

        Assert.Equal(HttpStatusCode.NotModified, conditionalResponse.StatusCode);
    }

    [Fact]
    public async Task DeletedSearchIsReadOnlyAndPreservesSearchState()
    {
        using var factory = new MailContractTestWebApplicationFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(
            "/api/v1/mail/deleted?mailbox=mailbox-1&search=old-message");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<DeletedMailPageResponse>();
        Assert.NotNull(page);
        Assert.Equal("Available", page.State);
        Assert.Equal("old-message", factory.State.DeletedSource.LastSearchTerm);
        Assert.Empty(factory.State.ActionHistory.Entries);
    }

    [Fact]
    public async Task MailResponsesDoNotExposeProviderCredentialsOrRawPayloads()
    {
        using var factory = new MailContractTestWebApplicationFactory();
        using var client = factory.CreateClient();

        using var detail = await client.GetAsync(
            $"/api/v1/mail/{MailContractTestState.MessageId:D}");
        using var deleted = await client.GetAsync(
            "/api/v1/mail/deleted?search=provider-check");
        var combined = (await detail.Content.ReadAsStringAsync())
            + (await deleted.Content.ReadAsStringAsync());

        foreach (var forbidden in new[]
        {
            "access_token",
            "client_secret",
            "connection_string",
            "raw_provider",
            "graph_token",
            "mailbox_secret"
        })
        {
            Assert.DoesNotContain(forbidden, combined, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task ClassificationCorrectionUsesCanonicalSelectionAndRecordsAudit()
    {
        using var factory = new MailContractTestWebApplicationFactory();
        using var client = factory.CreateClient();
        var operationKey = Guid.NewGuid().ToString("D");
        var request = new MailClassificationCorrectionRequest(
            1,
            "received:General",
            "Operator correction",
            operationKey,
            null,
            null);

        using var response = await client.PostAsJsonAsync(
            $"/api/v1/mail/{MailContractTestState.MessageId:D}/classification",
            request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<MailClassificationResponse>();
        Assert.NotNull(result);
        Assert.Equal(2, result.Version);
        Assert.Equal(1, factory.State.ClassificationStore.LastExpectedVersion);
        var audit = Assert.Single(factory.State.ActionHistory.Entries);
        Assert.Equal("mail_classification_correction", audit.EventKind);
        Assert.Equal(operationKey, audit.CorrelationId);
    }

    [Fact]
    public async Task UnavailableFolderMoveIsReportedAsProviderUnavailable()
    {
        using var factory = new MailContractTestWebApplicationFactory();
        using var client = factory.CreateClient();
        var request = new MailMoveRequest(
            1,
            "mail-folder-policy",
            1,
            1,
            Guid.NewGuid().ToString("D"),
            "Move requested");

        using var response = await client.PostAsJsonAsync(
            $"/api/v1/mail/{MailContractTestState.MessageId:D}/move-to-recommended-folder",
            request);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<PegasusProblem>();
        Assert.NotNull(problem);
        Assert.Equal(PegasusProblemTypes.ProviderUnavailable, problem.Type);
    }

    [Fact]
    public async Task FolderMoveRequiresABareGuidOperationKey()
    {
        using var factory = new MailContractTestWebApplicationFactory(successfulMove: true);
        using var client = factory.CreateClient();
        var request = new MailMoveRequest(
            1,
            "mail-folder-policy",
            1,
            1,
            $"desk:{Guid.NewGuid():D}",
            "Move requested");

        using var rejected = await client.PostAsJsonAsync(
            $"/api/v1/mail/{MailContractTestState.MessageId:D}/move-to-recommended-folder",
            request);

        Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
        var problem = await rejected.Content.ReadFromJsonAsync<PegasusProblem>();
        Assert.NotNull(problem);
        Assert.Equal(PegasusProblemTypes.Validation, problem.Type);

        var acceptedRequest = request with { OperationKey = Guid.NewGuid().ToString("D") };
        using var accepted = await client.PostAsJsonAsync(
            $"/api/v1/mail/{MailContractTestState.MessageId:D}/move-to-recommended-folder",
            acceptedRequest);

        Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
        var result = await accepted.Content.ReadFromJsonAsync<MailFolderMoveResponse>();
        Assert.NotNull(result);
        Assert.Equal("Succeeded", result.Outcome);
        Assert.Equal("Message moved to the recommended Outlook folder.", result.OperatorMessage);
    }

    [Fact]
    public async Task ClassificationVersionConflictsAreProblemDetails()
    {
        using var factory = new MailContractTestWebApplicationFactory();
        using var client = factory.CreateClient();
        var request = new MailClassificationCorrectionRequest(
            2,
            "received:General",
            "Operator correction",
            Guid.NewGuid().ToString("D"),
            null,
            null);

        using var response = await client.PostAsJsonAsync(
            $"/api/v1/mail/{MailContractTestState.MessageId:D}/classification",
            request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<PegasusProblem>();
        Assert.NotNull(problem);
        Assert.Equal(PegasusProblemTypes.VersionConflict, problem.Type);
    }

    [Fact]
    public async Task ComposedFolderMoverExposesMoveCapability()
    {
        using var factory = new MailContractTestWebApplicationFactory(useAvailableMover: true);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(
            $"/api/v1/mail/{MailContractTestState.MessageId:D}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var detail = await response.Content.ReadFromJsonAsync<MailDetailResponse>();
        Assert.NotNull(detail);
        Assert.True(detail.FolderRecommendation!.CanMove);
        Assert.NotNull(detail.SuggestedMove);
    }

    [Fact]
    public async Task LinkCaseRequiresPreparationAndCarriesVersionsThroughCore()
    {
        using var factory = new MailContractTestWebApplicationFactory();
        using var client = factory.CreateClient();
        var prepareRequest = new MailCasePreparationRequest(
            MailContractTestState.CaseId,
            1,
            1,
            "lease-operation");

        using var prepareResponse = await client.PostAsJsonAsync(
            $"/api/v1/mail/{MailContractTestState.MessageId:D}/link-case/prepare",
            prepareRequest);

        Assert.Equal(HttpStatusCode.OK, prepareResponse.StatusCode);
        var prepared = await prepareResponse.Content.ReadFromJsonAsync<MailCasePreparationResponse>();
        Assert.NotNull(prepared);
        Assert.Equal("lease-token", prepared.LeaseToken);

        var operationKey = Guid.NewGuid().ToString("D");
        var confirmRequest = new MailCaseAssociationRequest(
            MailContractTestState.CaseId,
            prepared.ExpectedIntakeVersion,
            prepared.ExpectedCaseVersion,
            prepared.LeaseToken,
            operationKey,
            "Link the message");
        using var confirmResponse = await client.PostAsJsonAsync(
            $"/api/v1/mail/{MailContractTestState.MessageId:D}/link-case",
            confirmRequest);

        Assert.Equal(HttpStatusCode.OK, confirmResponse.StatusCode);
        Assert.True(factory.State.LinkIntake.WasCalled);
        Assert.Equal(1, factory.State.LinkIntake.LastRequest!.ExpectedIntakeVersion);
        Assert.Equal(1, factory.State.LinkIntake.LastRequest.ExpectedCaseVersion);
        Assert.Equal(operationKey, factory.State.ActionHistory.Entries.Single().CorrelationId);

        using var replayResponse = await client.PostAsJsonAsync(
            $"/api/v1/mail/{MailContractTestState.MessageId:D}/link-case",
            confirmRequest);

        Assert.Equal(HttpStatusCode.OK, replayResponse.StatusCode);
        Assert.Single(factory.State.ActionHistory.Entries);
    }

    [Fact]
    public async Task UnlinkCaseCarriesTheCaseCancellationConsequence()
    {
        using var factory = new MailContractTestWebApplicationFactory(associated: true);
        using var client = factory.CreateClient();
        var prepareRequest = new MailCasePreparationRequest(
            MailContractTestState.CaseId,
            1,
            1,
            "unlink-lease-operation");

        using var prepareResponse = await client.PostAsJsonAsync(
            $"/api/v1/mail/{MailContractTestState.MessageId:D}/unlink-case/prepare",
            prepareRequest);

        Assert.Equal(HttpStatusCode.OK, prepareResponse.StatusCode);
        var prepared = await prepareResponse.Content.ReadFromJsonAsync<MailCasePreparationResponse>();
        Assert.NotNull(prepared);
        Assert.Equal(
            "Unlinking this email cancels case CASE-001",
            prepared.Consequence);

        var confirmRequest = new MailCaseAssociationRequest(
            MailContractTestState.CaseId,
            prepared.ExpectedIntakeVersion,
            prepared.ExpectedCaseVersion,
            prepared.LeaseToken,
            Guid.NewGuid().ToString("D"),
            "Unlink the message");
        using var confirmResponse = await client.PostAsJsonAsync(
            $"/api/v1/mail/{MailContractTestState.MessageId:D}/unlink-case",
            confirmRequest);

        Assert.Equal(HttpStatusCode.OK, confirmResponse.StatusCode);
        var result = await confirmResponse.Content.ReadFromJsonAsync<MailCaseAssociationResponse>();
        Assert.NotNull(result);
        Assert.Equal("Unlinking this email cancels case CASE-001", result.Consequence);
        Assert.True(factory.State.ReverseIntakeLink.WasCalled);

        using var replayResponse = await client.PostAsJsonAsync(
            $"/api/v1/mail/{MailContractTestState.MessageId:D}/unlink-case",
            confirmRequest);

        Assert.Equal(HttpStatusCode.OK, replayResponse.StatusCode);
        Assert.Single(factory.State.ActionHistory.Entries);
    }

    [Fact]
    public async Task MailListRejectsPageSizesAboveCoreCap()
    {
        using var factory = new MailContractTestWebApplicationFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/v1/mail?pageSize=101");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<PegasusProblem>();
        Assert.NotNull(problem);
        Assert.Equal(PegasusProblemTypes.Validation, problem.Type);
    }

    [Fact]
    public async Task MailListRejectsAnOperationalAndDetailedViewTogether()
    {
        using var factory = new MailContractTestWebApplicationFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(
            "/api/v1/mail?destination=receiving-work&classification=received:General");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<PegasusProblem>();
        Assert.NotNull(problem);
        Assert.Equal(PegasusProblemTypes.Validation, problem.Type);
    }

    [Fact]
    public async Task EveryMailRouteIsUnauthorizedWithoutAnAuthenticatedActor()
    {
        using var factory = new MailContractTestWebApplicationFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Contract-Unauthenticated", "true");

        foreach (var (method, path) in MailRoutes())
        {
            using var request = new HttpRequestMessage(method, path);
            if (method == HttpMethod.Post)
            {
                request.Content = JsonContent.Create(new { });
            }
            using var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }

    [Fact]
    public async Task EveryMailRouteIsAbsentWhenTheGatewayCompositionIsOff()
    {
        using var factory = new MailContractTestWebApplicationFactory(desktopGatewayEnabled: false);
        using var client = factory.CreateClient();

        foreach (var path in MailRoutes().Select(route => route.Path).Distinct(StringComparer.Ordinal))
        {
            using var response = await client.GetAsync(path);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }

    private static IEnumerable<(HttpMethod Method, string Path)> MailRoutes() =>
    [
        (HttpMethod.Get, "/api/v1/mail"),
        (HttpMethod.Get, "/api/v1/mail/deleted"),
        (HttpMethod.Get, $"/api/v1/mail/{MailContractTestState.MessageId:D}/preview"),
        (HttpMethod.Get, $"/api/v1/mail/{MailContractTestState.MessageId:D}"),
        (HttpMethod.Post, $"/api/v1/mail/{MailContractTestState.MessageId:D}/link-case/prepare"),
        (HttpMethod.Post, $"/api/v1/mail/{MailContractTestState.MessageId:D}/unlink-case/prepare"),
        (HttpMethod.Post, $"/api/v1/mail/{MailContractTestState.MessageId:D}/link-case"),
        (HttpMethod.Post, $"/api/v1/mail/{MailContractTestState.MessageId:D}/unlink-case"),
        (HttpMethod.Post, $"/api/v1/mail/{MailContractTestState.MessageId:D}/classification"),
        (HttpMethod.Post, $"/api/v1/mail/{MailContractTestState.MessageId:D}/move-to-recommended-folder")
    ];

    [Fact]
    public async Task MailGatewayRequiresCaseworkAuthorization()
    {
        using var factory = new MailContractTestWebApplicationFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Contract-Wrong-Right", "true");

        foreach (var (method, path) in MailRoutes())
        {
            using var request = new HttpRequestMessage(method, path);
            if (method == HttpMethod.Post)
            {
                request.Content = JsonContent.Create(new { });
            }
            using var response = await client.SendAsync(request);
            Assert.True(
                response.StatusCode == HttpStatusCode.Forbidden,
                $"{method} {path} returned {response.StatusCode}");
        }
    }
}

internal sealed class MailContractTestWebApplicationFactory : ContractTestWebApplicationFactory
{
    private readonly bool useAvailableMover;
    private readonly bool successfulMove;
    private readonly bool desktopGatewayEnabled;

    public MailContractTestWebApplicationFactory(
        bool useAvailableMover = false,
        bool associated = false,
        bool successfulMove = false,
        bool desktopGatewayEnabled = true)
    {
        this.useAvailableMover = useAvailableMover;
        this.successfulMove = successfulMove;
        this.desktopGatewayEnabled = desktopGatewayEnabled;
        State = new MailContractTestState(associated);
        if (useAvailableMover)
        {
            State.ApprovedMailboxStore.Mailboxes =
            [
                new ApprovedMailbox(
                    Guid.NewGuid(),
                    "mailbox@example.test",
                    [ApprovedMailboxRouteScope.InboundIntake],
                    ApprovedMailboxState.Approved,
                    "mailbox-1",
                    "inbox-folder",
                    "sent-folder",
                    true,
                    1,
                    [new(MailLogicalFolderType.NoAction, "no-action-folder")])
            ];
        }
    }

    public MailContractTestState State { get; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        if (!desktopGatewayEnabled)
        {
            builder.UseSetting("Features:DesktopGateway", "false");
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Features:DesktopGateway"] = "false"
                }));
        }
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IRetainedMailQueries>();
            services.AddSingleton<IRetainedMailQueries>(State.Queries);
            services.RemoveAll<IDeletedMailSearchSource>();
            services.AddSingleton<IDeletedMailSearchSource>(State.DeletedSource);
            services.RemoveAll<IRetainedMailClassificationStore>();
            services.AddSingleton<IRetainedMailClassificationStore>(State.ClassificationStore);
            services.RemoveAll<IRetainedMailFolderMoveStore>();
            services.AddSingleton<IRetainedMailFolderMoveStore>(State.FolderMoveStore);
            services.RemoveAll<IApprovedMailboxStore>();
            services.AddSingleton<IApprovedMailboxStore>(State.ApprovedMailboxStore);
            services.RemoveAll<IActionHistoryWriter>();
            services.AddSingleton<IActionHistoryWriter>(State.ActionHistory);
            services.RemoveAll<IGetIntake>();
            services.AddSingleton<IGetIntake>(State.Intake);
            services.RemoveAll<IGetCase>();
            services.AddSingleton<IGetCase>(State.CaseQueries);
            services.RemoveAll<IAcquireCaseEditLease>();
            services.AddSingleton<IAcquireCaseEditLease>(State.Lease);
            services.RemoveAll<ILinkIntake>();
            services.AddSingleton<ILinkIntake>(State.LinkIntake);
            services.RemoveAll<IReverseIntakeLink>();
            services.AddSingleton<IReverseIntakeLink>(State.ReverseIntakeLink);
            State.FolderMoveStore.Succeed = successfulMove;
            if (useAvailableMover)
            {
                services.RemoveAll<IRetainedMailFolderMover>();
                services.AddSingleton<IRetainedMailFolderMover, AvailableFolderMover>();
            }
        });
    }
}

internal sealed class MailContractTestState
{
    public static readonly Guid MessageId =
        Guid.Parse("c1a5c8ef-2ac2-4c37-b0d8-26f9e2d0dd18");
    public static readonly Guid ReceiptId =
        Guid.Parse("ba8d10fa-0f2e-44d4-a9aa-90f383207f90");
    public static readonly Guid CaseId =
        Guid.Parse("2e4b6a6b-7f4b-4c9e-8f4c-36ecaa19a6b2");

    public MailContractTestState(bool associated)
    {
        Queries = new(associated);
        DeletedSource = new();
        ClassificationStore = new();
        FolderMoveStore = new();
        ApprovedMailboxStore = new();
        ActionHistory = new();
        Intake = new(associated);
        CaseQueries = new();
        Lease = new();
        LinkIntake = new(Intake);
        ReverseIntakeLink = new(Intake);
    }

    public FakeRetainedMailQueries Queries { get; }
    public FakeDeletedMailSearchSource DeletedSource { get; }
    public FakeClassificationStore ClassificationStore { get; }
    public FakeFolderMoveStore FolderMoveStore { get; }
    public FakeApprovedMailboxStore ApprovedMailboxStore { get; }
    public FakeActionHistoryWriter ActionHistory { get; }
    public FakeIntakeQuery Intake { get; }
    public FakeCaseQuery CaseQueries { get; }
    public FakeLeaseAcquirer Lease { get; }
    public FakeLinkIntake LinkIntake { get; }
    public FakeReverseIntakeLink ReverseIntakeLink { get; }
}

internal sealed class FakeRetainedMailQueries : IRetainedMailQueries
{
    private static readonly DateTimeOffset ReceivedAt =
        new(2026, 8, 30, 8, 0, 0, TimeSpan.Zero);

    public FakeRetainedMailQueries(bool associated)
    {
        var summary = new RetainedMailSummary(
            MailContractTestState.MessageId,
            "mailbox-1",
            "mailbox@example.test",
            true,
            "sender@example.test",
            "Sender",
            "sender@example.test",
            "Retained message",
            "Message excerpt",
            ReceivedAt,
            true,
            1,
            IntakeDecision.CaseCreated,
            MailContractTestState.ReceiptId,
            associated ? MailContractTestState.CaseId : null,
            associated ? "CASE-001" : null,
            IntakeVersion: 1,
            CaseVersion: associated ? 1 : null);
        var classification = new MailClassificationDossier(
            1,
            MailClassificationResult.Classified(
                MailCategory.Received(ReceivedMailFamily.General),
                [],
                "Initial classification",
                "mail-policy",
                1),
            "system-worker:contract-test",
            ReceivedAt,
            []);
        Detail = new RetainedMailDetail(
            summary,
            ["recipient@example.test"],
            [],
            "Message body",
            [new RetainedMailAttachment("evidence.pdf", "application/pdf", 12, true)],
            [],
            MailFolderScope.Inbox,
            null,
            null,
            classification);
        Page = new RetainedMailPage([summary], 1, 25, 1, false);
    }

    public RetainedMailDetail Detail { get; }
    public RetainedMailPage Page { get; }
    public MailWorkspaceScope? LastScope { get; private set; }

    public Task<RetainedMailPage> ListAsync(
        MailWorkspaceScope scope,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        LastScope = scope;
        return Task.FromResult(Page with { Page = page, PageSize = pageSize });
    }

    public Task<RetainedMailDetail?> GetAsync(
        Guid id,
        CancellationToken cancellationToken,
        string? searchTerm = null) =>
        Task.FromResult<RetainedMailDetail?>(id == MailContractTestState.MessageId ? Detail : null);

    public Task<IReadOnlyList<RetainedMailMailbox>> ListMailboxesAsync(
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<RetainedMailMailbox>>(
            [new("mailbox-1", "mailbox@example.test", true)]);

    public Task<IReadOnlyList<MailPollHealth>> ListPollHealthAsync(
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<MailPollHealth>>(
            [new("mailbox-1", DateTimeOffset.UtcNow.AddMinutes(-1), null, DateTimeOffset.UtcNow.AddMinutes(1))]);
}

internal sealed class FakeDeletedMailSearchSource : IDeletedMailSearchSource
{
    public string? LastSearchTerm { get; private set; }

    public Task<IReadOnlyList<RetainedMailMailbox>> ListMailboxesAsync(
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<RetainedMailMailbox>>(
            [new("mailbox-1", "mailbox@example.test", true)]);

    public Task<DeletedMailSourceResult> SearchAsync(
        string? mailboxId,
        string searchTerm,
        int maximumMessages,
        CancellationToken cancellationToken)
    {
        LastSearchTerm = searchTerm;
        return Task.FromResult(new DeletedMailSourceResult(
            [new DeletedMailSearchItem(
                "mailbox-1",
                "mailbox@example.test",
                "immutable-message-1",
                "sender@example.test",
                "Sender",
                "Old message",
                "Deleted body",
                DateTimeOffset.UtcNow.AddDays(-1),
                false,
                [],
                [])],
            false));
    }
}

internal sealed class FakeClassificationStore : IRetainedMailClassificationStore
{
    private static readonly MailCategory InitialCategory =
        MailCategory.Received(ReceivedMailFamily.General);

    private MailClassificationDossier dossier = new MailClassificationDossier(
        1,
        MailClassificationResult.Classified(
            InitialCategory,
            [],
            "Initial classification",
            "mail-policy",
            1),
        "staff:4de7c7c0-6119-4b3e-a0ba-b5e8e042c4b0",
        DateTimeOffset.UtcNow,
        []) with
    {
        CurrentActorDisplayName = "Test Staff"
    };

    public int LastExpectedVersion { get; private set; }

    public Task<MailClassificationDossier?> GetClassificationAsync(
        Guid messageId,
        CancellationToken cancellationToken) =>
        Task.FromResult<MailClassificationDossier?>(
            messageId == MailContractTestState.MessageId ? dossier : null);

    public Task<MailClassificationDossier> AppendCorrectionAsync(
        Guid messageId,
        int expectedVersion,
        MailClassificationResult before,
        MailClassificationResult after,
        string actor,
        string reason,
        DateTimeOffset correctedAtUtc,
        CancellationToken cancellationToken)
    {
        LastExpectedVersion = expectedVersion;
        dossier = new MailClassificationDossier(
            expectedVersion + 1,
            after,
            actor,
            correctedAtUtc,
            [new(expectedVersion, before, after, actor, reason, correctedAtUtc)]) with
        {
            CurrentActorDisplayName = "Test Staff"
        };
        return Task.FromResult(dossier);
    }
}

internal sealed class FakeFolderMoveStore : IRetainedMailFolderMoveStore
{
    public bool Succeed { get; set; }

    public Task<RetainedMailFolderMoveResult?> MoveAsync(
        ActionActor actor,
        MoveRetainedMailFolderRequest request,
        CancellationToken cancellationToken)
    {
        if (!Succeed)
        {
            throw new RetainedMailFolderMoveException("Move unavailable in contract tests.");
        }

        return Task.FromResult<RetainedMailFolderMoveResult?>(new(
            RetainedMailFolderMoveOutcome.Succeeded,
            MailLogicalFolderType.NoAction,
            request.Reason,
            DateTimeOffset.UtcNow,
            false,
            request.OperationKey,
            null,
            request.ExpectedClassificationVersion,
            request.ExpectedRecommendationPolicyKey,
            request.ExpectedRecommendationPolicyVersion,
            request.ExpectedMailboxVersion));
    }

    public Task<RetainedMailFolderMoveResult?> GetLatestAsync(
        Guid messageId,
        CancellationToken cancellationToken) =>
        Task.FromResult<RetainedMailFolderMoveResult?>(null);

    public Task<bool> IsCurrentLocationAsync(
        Guid messageId,
        string folderIdentity,
        CancellationToken cancellationToken) =>
        Task.FromResult(false);
}

internal sealed class FakeApprovedMailboxStore : IApprovedMailboxStore
{
    public IReadOnlyList<ApprovedMailbox> Mailboxes { get; set; } = [];

    public Task<bool> IsApprovedAsync(
        string mailboxAddress,
        ApprovedMailboxRouteScope routeScope,
        CancellationToken cancellationToken) =>
        Task.FromResult(true);

    public Task<IReadOnlyList<ApprovedMailbox>> ListAsync(
        CancellationToken cancellationToken) =>
        Task.FromResult(Mailboxes);

    public Task<ApprovedMailbox> UpdateAsync(
        UpdateApprovedMailboxRequest request,
        CancellationToken cancellationToken) =>
        throw new NotSupportedException();
}

internal sealed class FakeIntakeQuery : IGetIntake
{
    public FakeIntakeQuery(bool associated)
    {
        var now = DateTimeOffset.UtcNow;
        Receipt = new IntakeReceipt(
            MailContractTestState.ReceiptId,
            "message.eml",
            "message/rfc822",
            10,
            "source-hash",
            new(IntakeSourceChannel.Mailbox, "external-message-1"),
            now,
            now,
            IntakeDecision.CaseCreated,
            "Accepted",
            [],
            [],
            null,
            [],
            null,
            null,
            false,
            "reader",
            "1",
            null,
            null,
            Version: 1,
            AcceptedCaseId: associated ? MailContractTestState.CaseId : null,
            AcceptedCaseReference: associated ? "CASE-001" : null,
            ManualAssociationActorKind: associated ? ActorKind.Staff : null);
    }

    public IntakeReceipt Receipt { get; private set; }

    public Task<IntakeReceipt?> ExecuteAsync(
        GetIntakeQuery query,
        CancellationToken cancellationToken) =>
        Task.FromResult<IntakeReceipt?>(
            query.ReceiptId == MailContractTestState.ReceiptId ? Receipt : null);

    public void ApplyLink(LinkIntakeRequest request) =>
        Receipt = Receipt with
        {
            ManualLinkedCaseId = request.CaseId,
            ManualLinkedCaseReference = "CASE-001",
            ManualAssociationVersion = request.ExpectedIntakeVersion + 1,
            ManualAssociationActorKind = ActorKind.Staff,
            ManualAssociationOperationKey = request.OperationKey,
            Version = request.ExpectedIntakeVersion + 1
        };

    public void ApplyUnlink(ReverseIntakeLinkRequest request) =>
        Receipt = Receipt with
        {
            ManualLinkedCaseId = null,
            ManualLinkedCaseReference = null,
            ManualAssociationVersion = request.ExpectedIntakeVersion + 1,
            ManualAssociationOperationKey = request.OperationKey,
            Version = request.ExpectedIntakeVersion + 1
        };
}

internal sealed class FakeCaseQuery : IGetCase
{
    private readonly CaseDetails details = CreateDetails();

    public Task<CaseDetails?> ExecuteAsync(
        GetCaseQuery query,
        CancellationToken cancellationToken) =>
        Task.FromResult<CaseDetails?>(query.CaseId == MailContractTestState.CaseId ? details : null);

    private static CaseDetails CreateDetails()
    {
        var identity = new CaseIdentity(
            MailContractTestState.CaseId,
            "CE",
            2026,
            1,
            "CASE-001");
        var summary = new CaseSearchItem(
            MailContractTestState.CaseId,
            "CASE-001",
            null,
            CaseType.Inspection,
            "CE",
            CaseLifecycleState.Review,
            null,
            "AB12CDE",
            "Claimant",
            "CLM-001",
            DateTimeOffset.UtcNow,
            null,
            "contract-test",
            DateTimeOffset.UtcNow);
        var workflow = new CaseWorkflowRecord(
            MailContractTestState.CaseId,
            identity,
            CaseLifecycleState.Review,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            1);
        return new CaseDetails(
            summary,
            workflow,
            null,
            [],
            null,
            CaseCustodyState.Pending,
            [],
            [],
            []);
    }
}

internal sealed class FakeLeaseAcquirer : IAcquireCaseEditLease
{
    public ClaimCaseEditLeaseRequest? LastRequest { get; private set; }

    public Task<CaseEditLease> ExecuteAsync(
        ClaimCaseEditLeaseRequest request,
        CancellationToken cancellationToken)
    {
        LastRequest = request;
        return Task.FromResult(new CaseEditLease(
            request.CaseId,
            "lease-token",
            "contract-test",
            request.ExpectedVersion,
            DateTimeOffset.UtcNow.AddMinutes(5)));
    }
}

internal sealed class FakeLinkIntake(FakeIntakeQuery intake) : ILinkIntake
{
    public bool WasCalled { get; private set; }
    public LinkIntakeRequest? LastRequest { get; private set; }

    public Task ExecuteAsync(
        LinkIntakeRequest request,
        CancellationToken cancellationToken)
    {
        WasCalled = true;
        LastRequest = request;
        intake.ApplyLink(request);
        return Task.CompletedTask;
    }
}

internal sealed class FakeReverseIntakeLink(FakeIntakeQuery intake) : IReverseIntakeLink
{
    public bool WasCalled { get; private set; }

    public Task ExecuteAsync(
        ReverseIntakeLinkRequest request,
        CancellationToken cancellationToken)
    {
        WasCalled = true;
        intake.ApplyUnlink(request);
        return Task.CompletedTask;
    }
}

internal sealed class AvailableFolderMover : IRetainedMailFolderMover
{
    public bool IsAvailable => true;

    public Task MoveAsync(
        RetainedMailFolderMoveCoordinates coordinates,
        CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public Task<string?> GetParentFolderIdAsync(
        string mailboxId,
        string immutableMessageId,
        CancellationToken cancellationToken) =>
        Task.FromResult<string?>("parent-folder");
}

internal sealed class FakeActionHistoryWriter : IActionHistoryWriter
{
    public List<ActionHistoryEntry> Entries { get; } = [];

    public Task AppendAsync(ActionHistoryEntry entry, CancellationToken cancellationToken)
    {
        Entries.Add(entry);
        return Task.CompletedTask;
    }
}
