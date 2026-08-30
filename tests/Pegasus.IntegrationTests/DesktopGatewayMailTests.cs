using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pegasus.Contracts.Mail;
using Pegasus.Core.Intake;
using Pegasus.Infrastructure.Persistence;
using Pegasus.Web.Api;

namespace Pegasus.IntegrationTests;

[Trait("Category", "SqlServer")]
public sealed class DesktopGatewayMailTests
{
    private const string MailboxId = "parity-mailbox";
    private const string MailboxAddress = "parity-mailbox@collisionengineers.co.uk";
    private const string LinkReason = "The retained message names this exact Case/PO.";

    private static readonly DateTimeOffset NowUtc =
        new(2031, 5, 6, 10, 30, 0, TimeSpan.Zero);

    [Fact]
    public async Task ApiLinkConfirmHasTheSameCoreAssociationEffectAsTheRazorHandler()
    {
        using var baseFactory = new IntakeWebApplicationFactory(
            useIntegrationTestAuthentication: true);
        using var factory = baseFactory.WithWebHostBuilder(builder =>
            builder.UseSetting(DesktopGateway.FeatureFlag, "true"));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost:7139")
        });

        var razorMessageId = await SeedRetainedMailAsync(
            baseFactory,
            "razor-message");
        await StoreClassificationAsync(baseFactory, "razor-message");
        var razorReceiptId = await ReceiptIdAsync(baseFactory, "razor-message");
        var razorCaseId = await ImageIntakeTestData.SeedCaseAsync(
            baseFactory.Services,
            razorReceiptId,
            "PARITY31001",
            nameof(Pegasus.Core.Workflow.CaseLifecycleState.Review));

        var apiMessageId = await SeedRetainedMailAsync(
            baseFactory,
            "api-message");
        await StoreClassificationAsync(baseFactory, "api-message");
        var apiReceiptId = await ReceiptIdAsync(baseFactory, "api-message");
        var apiCaseId = await ImageIntakeTestData.SeedCaseAsync(
            baseFactory.Services,
            apiReceiptId,
            "PARITY31002",
            nameof(Pegasus.Core.Workflow.CaseLifecycleState.Review));

        await LinkWithRazorAsync(client, razorMessageId, razorCaseId, "PARITY31001");
        await LinkWithApiAsync(baseFactory, client, apiMessageId, apiReceiptId, apiCaseId);

        var razorEffect = await ReadAssociationEffectAsync(baseFactory, razorReceiptId);
        var apiEffect = await ReadAssociationEffectAsync(baseFactory, apiReceiptId);

        Assert.Equal(razorCaseId, razorEffect.CaseId);
        Assert.Equal(apiCaseId, apiEffect.CaseId);
        Assert.Equal(razorEffect.EventType, apiEffect.EventType);
        Assert.Equal("intake_case_linked", razorEffect.EventType);
        Assert.Equal(LinkReason, razorEffect.Reason);
        Assert.Equal(razorEffect.Reason, apiEffect.Reason);
        Assert.Equal(razorEffect.ExpectedIntakeVersion, apiEffect.ExpectedIntakeVersion);
        Assert.Equal(razorEffect.BeforeIntakeVersion, apiEffect.BeforeIntakeVersion);
        Assert.Equal(razorEffect.AfterIntakeVersion, apiEffect.AfterIntakeVersion);
        Assert.Equal(razorEffect.ExpectedCaseVersion, apiEffect.ExpectedCaseVersion);
        Assert.Equal(razorEffect.BeforeCaseVersion, apiEffect.BeforeCaseVersion);
        Assert.Equal(razorEffect.AfterCaseVersion, apiEffect.AfterCaseVersion);
        Assert.Equal(1, razorEffect.AfterIntakeVersion);
        Assert.Equal(1, razorEffect.AfterCaseVersion);
    }

    private static async Task LinkWithRazorAsync(
        HttpClient client,
        Guid messageId,
        Guid caseId,
        string caseReference)
    {
        var target = await GetPageAsync(
            client,
            $"/Inbox/{messageId:D}?caseQuery={caseReference}&targetCaseId={caseId:D}");
        var prepareForm = FindForm(target, "PrepareLinkCase");
        using var prepared = await client.PostAsync(
            FormAction(prepareForm),
            new FormUrlEncodedContent(HiddenFields(prepareForm)));
        Assert.Equal(HttpStatusCode.Redirect, prepared.StatusCode);
        Assert.NotNull(prepared.Headers.Location);

        var confirmation = await GetPageAsync(client, prepared.Headers.Location!.ToString());
        var linkForm = FindForm(confirmation, "LinkCase");
        var fields = HiddenFields(linkForm);
        fields["Reason"] = LinkReason;
        using var response = await client.PostAsync(
            FormAction(linkForm),
            new FormUrlEncodedContent(fields));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }

    private static async Task LinkWithApiAsync(
        IntakeWebApplicationFactory factory,
        HttpClient client,
        Guid messageId,
        Guid receiptId,
        Guid caseId)
    {
        var versions = await ReadVersionsAsync(factory, receiptId, caseId);
        using var prepareResponse = await client.PostAsJsonAsync(
            $"/api/v1/mail/{messageId:D}/link-case/prepare",
            new MailCasePreparationRequest(
                caseId,
                versions.IntakeVersion,
                versions.CaseVersion,
                "parity-api-lease"));
        Assert.Equal(HttpStatusCode.OK, prepareResponse.StatusCode);
        var preparation = await prepareResponse.Content
            .ReadFromJsonAsync<MailCasePreparationResponse>();
        Assert.NotNull(preparation);

        using var confirmResponse = await client.PostAsJsonAsync(
            $"/api/v1/mail/{messageId:D}/link-case",
            new MailCaseAssociationRequest(
                caseId,
                preparation.ExpectedIntakeVersion,
                preparation.ExpectedCaseVersion,
                preparation.LeaseToken,
                "parity-api-confirm",
                LinkReason));
        Assert.Equal(HttpStatusCode.OK, confirmResponse.StatusCode);
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
        var intakeVersion = await context.IntakeReceipts
            .Where(item => item.Id == receiptId)
            .Select(item => item.Version)
            .SingleAsync();
        var caseVersion = await context.CaseWorkflows
            .Where(item => item.CaseId == caseId)
            .Select(item => item.Version)
            .SingleAsync();
        return (intakeVersion, caseVersion);
    }

    private static async Task<AssociationEffect> ReadAssociationEffectAsync(
        IntakeWebApplicationFactory factory,
        Guid receiptId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var contextFactory = scope.ServiceProvider
            .GetRequiredService<IDbContextFactory<PegasusDbContext>>();
        await using var context = await contextFactory.CreateDbContextAsync();
        var association = await context.IntakeManualAssociations
            .AsNoTracking()
            .Where(item => item.IntakeReceiptId == receiptId && item.IsActive)
            .Select(item => new { item.CaseId })
            .SingleAsync();
        var history = await context.IntakeMutationHistory
            .AsNoTracking()
            .Where(item => item.IntakeReceiptId == receiptId)
            .SingleAsync();
        return new(
            association.CaseId,
            history.EventType,
            history.Reason,
            history.ExpectedIntakeVersion,
            history.BeforeIntakeVersion,
            history.AfterIntakeVersion,
            history.ExpectedCaseVersion,
            history.BeforeCaseVersion,
            history.AfterCaseVersion);
    }

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
        var externalToken = $"{MailboxId.Length}:{MailboxId}{immutableMessageId}";
        await store.RetainAsync(
            new(
                MailboxId,
                MailboxAddress,
                immutableMessageId,
                externalToken,
                NowUtc.AddMinutes(-1),
                1024,
                new string('A', 64),
                new(
                    "inbox",
                    "conversation-parity",
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
        var contextFactory = scope.ServiceProvider
            .GetRequiredService<IDbContextFactory<PegasusDbContext>>();
        await using var context = await contextFactory.CreateDbContextAsync();
        var externalToken = $"{MailboxId.Length}:{MailboxId}{immutableMessageId}";
        return await context.IntakeReceipts
            .Where(item => item.SourceChannel == "mailbox"
                           && item.ExternalReceiptToken == externalToken)
            .Select(item => item.Id)
            .SingleAsync();
    }

    private static async Task StoreClassificationAsync(
        IntakeWebApplicationFactory factory,
        string immutableMessageId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IIntakeReceiptStore>().StoreAsync(
            new(
                SourceFileName: "parity.eml",
                MediaType: "message/rfc822",
                SourceLength: 1,
                SourceHash: new string('D', 64),
                SourceIdentity: new(
                    IntakeSourceChannel.Mailbox,
                    MailboxId.Length + ":" + MailboxId + immutableMessageId),
                ReceivedAtUtc: NowUtc,
                ProcessedAtUtc: NowUtc,
                Actor: "system-worker:approved-inbox-poller",
                Decision: IntakeDecision.NeedsSorting,
                DecisionReason: "Parity fixture.",
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

    private static async Task<string> GetPageAsync(HttpClient client, string route)
    {
        using var response = await client.GetAsync(route);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadAsStringAsync();
    }

    private static string FindForm(string html, string handler)
    {
        var match = Regex.Match(
            html,
            $"<form method=\"post\" action=\"[^\"]*handler={Regex.Escape(handler)}[^\"]*\"[^>]*>.*?</form>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant);
        Assert.True(match.Success, $"The {handler} form was not rendered.");
        return match.Value;
    }

    private static string FormAction(string form)
    {
        var match = Regex.Match(
            form,
            "<form method=\"post\" action=\"([^\"]+)\"",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        Assert.True(match.Success, "The form action was not rendered.");
        return WebUtility.HtmlDecode(match.Groups[1].Value);
    }

    private static Dictionary<string, string> HiddenFields(string form) => Regex.Matches(
            form,
            "<input[^>]*name=\"([^\"]+)\"[^>]*value=\"([^\"]*)\"[^>]*>",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)
        .Cast<Match>()
        .ToDictionary(
            match => WebUtility.HtmlDecode(match.Groups[1].Value),
            match => WebUtility.HtmlDecode(match.Groups[2].Value),
            StringComparer.Ordinal);

    private sealed record AssociationEffect(
        Guid CaseId,
        string EventType,
        string Reason,
        long ExpectedIntakeVersion,
        long BeforeIntakeVersion,
        long AfterIntakeVersion,
        long? ExpectedCaseVersion,
        long? BeforeCaseVersion,
        long? AfterCaseVersion);
}
