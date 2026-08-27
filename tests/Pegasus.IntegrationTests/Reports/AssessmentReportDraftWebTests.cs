using System.Net;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Pegasus.Core.Actors;
using Pegasus.Core.Assessment;
using Pegasus.Core.Cases;
using Pegasus.Core.Identity;
using Pegasus.Core.Lifecycle;
using Pegasus.Core.Reports;
using Pegasus.Core.Workflow;

namespace Pegasus.IntegrationTests.Reports;

/// <summary>
/// Proves the DELIV-012 report-draft entry point is actually reachable from
/// the web: a complete case renders and returns a PDF, and an incomplete
/// case fails closed with its readiness reasons named instead of throwing.
/// <see cref="IAssessmentReportRenderer"/> is substituted with a fast fake so
/// this suite does not need a Chromium install — the real Playwright
/// renderer already has its own coverage in
/// <c>tests/Pegasus.IntegrationTests/Reports/AssessmentReportRendererTests.cs</c>.
/// Everything upstream of the renderer (the projection, the readiness gate,
/// the page wiring, authorisation) is exercised for real.
/// </summary>
[Trait("Category", "SqlServer")]
public sealed partial class AssessmentReportDraftWebTests
{
    [Fact]
    public async Task CompleteCaseRendersAndReturnsThePdf()
    {
        using var baseFactory = new IntakeWebApplicationFactory();
        var caseId = Guid.NewGuid();
        var pdfBytes = new byte[] { 1, 2, 3, 4 };
        using var factory = Compose(
            baseFactory,
            new FakeGetCase(caseId),
            new FakeGetCaseAssessment(FullAssessmentProjection(caseId)),
            new FakeProjectionSource(ReadyInput(caseId)),
            new FakeRenderer(pdfBytes));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        var html = await GetHtmlAsync(client, $"/Cases/{caseId:D}/Assessment");
        Assert.Contains("Generate report draft", html, StringComparison.Ordinal);

        using var response = await client.PostAsync(
            $"/Cases/{caseId:D}/Assessment?handler=GenerateReportDraft",
            Form(AntiforgeryValue(html), ("id", caseId.ToString("D")), ("operationKey", NewOperationKey())));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(pdfBytes, await response.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task IncompleteCaseFailsClosedNamingWhatIsMissingInsteadOfThrowing()
    {
        using var baseFactory = new IntakeWebApplicationFactory();
        var caseId = Guid.NewGuid();
        using var factory = Compose(
            baseFactory,
            new FakeGetCase(caseId),
            new FakeGetCaseAssessment(FullAssessmentProjection(caseId)),
            new FakeProjectionSource(ReadyInput(caseId) with { Costs = null }),
            new FakeRenderer([1]));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        var html = await GetHtmlAsync(client, $"/Cases/{caseId:D}/Assessment");
        Assert.Contains(AssessmentReportProjection.RepairCostRequirement, html, StringComparison.Ordinal);
        Assert.DoesNotContain("Generate report draft", html, StringComparison.Ordinal);
        Assert.DoesNotContain("report-draft-title", html, StringComparison.Ordinal);

        using var response = await client.PostAsync(
            $"/Cases/{caseId:D}/Assessment?handler=GenerateReportDraft",
            Form(AntiforgeryValue(html), ("id", caseId.ToString("D")), ("operationKey", NewOperationKey())));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal($"/Cases/{caseId:D}/Assessment", response.Headers.Location?.OriginalString);

        var afterHtml = await GetHtmlAsync(client, $"/Cases/{caseId:D}/Assessment");
        Assert.Contains(AssessmentReportProjection.RepairCostRequirement, afterHtml, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RetryStateUsesOperatorLabelsAndAnActionableRetryControl()
    {
        using var baseFactory = new IntakeWebApplicationFactory();
        var caseId = Guid.NewGuid();
        var store = new FakeAssessmentReportStore();
        var snapshot = AssessmentReportProjection.Project(ReadyInput(caseId)).Snapshot!;
        store.Seed(new AssessmentReportVersion(
            Guid.NewGuid(),
            caseId,
            1,
            AssessmentReportPayload.Key(snapshot),
            AssessmentReportGenerationState.Pending,
            AssessmentReportPayload.Serialize(snapshot),
            null,
            [],
            DateTimeOffset.UtcNow,
            null,
            "Renderer unavailable",
            1,
            DateTimeOffset.UtcNow.AddMinutes(-1)));
        using var factory = Compose(
            baseFactory,
            new FakeGetCase(caseId),
            new FakeGetCaseAssessment(FullAssessmentProjection(caseId)),
            new FakeProjectionSource(ReadyInput(caseId)),
            new FakeRenderer([1]),
            store);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var html = await GetHtmlAsync(client, $"/Cases/{caseId:D}/Assessment");

        Assert.Contains("Current report: Retry", html, StringComparison.Ordinal);
        Assert.Contains("Retry report draft", html, StringComparison.Ordinal);
        Assert.Contains("Renderer unavailable", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Report drafts are not approved or sent.", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RetryReauthorizesTheTargetCaseBeforeUsingItsStoredVersion()
    {
        using var baseFactory = new IntakeWebApplicationFactory();
        var visibleCaseId = Guid.NewGuid();
        var hiddenCaseId = Guid.NewGuid();
        var store = new FakeAssessmentReportStore();
        var hiddenVersion = StoredVersion(
            hiddenCaseId,
            AssessmentReportGenerationState.Pending,
            nextAttemptAtUtc: DateTimeOffset.UtcNow.AddMinutes(-1));
        store.Seed(hiddenVersion);
        using var factory = Compose(
            baseFactory,
            new FakeGetCase(visibleCaseId),
            new FakeGetCaseAssessment(FullAssessmentProjection(visibleCaseId)),
            new FakeProjectionSource(ReadyInput(visibleCaseId)),
            new FakeRenderer([1]),
            store);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var html = await GetHtmlAsync(client, $"/Cases/{visibleCaseId:D}/Assessment");
        using var response = await client.PostAsync(
            $"/Cases/{hiddenCaseId:D}/Assessment?handler=GenerateReportDraft",
            Form(
                AntiforgeryValue(html),
                ("id", hiddenCaseId.ToString("D")),
                ("operationKey", NewOperationKey()),
                ("reportVersionId", hiddenVersion.Id.ToString("D"))));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(0, store.BeginCount);
    }

    [Fact]
    public async Task ExpiredRenderingVersionIsRetryableFromItsStoredCanonicalPayload()
    {
        using var baseFactory = new IntakeWebApplicationFactory();
        var caseId = Guid.NewGuid();
        var store = new FakeAssessmentReportStore();
        var version = StoredVersion(
            caseId,
            AssessmentReportGenerationState.Rendering,
            leaseExpiresAtUtc: DateTimeOffset.UtcNow.AddMinutes(-1));
        store.Seed(version);
        using var factory = Compose(
            baseFactory,
            new FakeGetCase(caseId),
            new FakeGetCaseAssessment(FullAssessmentProjection(caseId)),
            new FakeProjectionSource(ReadyInput(caseId)),
            new FakeRenderer([4, 5, 6]),
            store);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        var html = await GetHtmlAsync(client, $"/Cases/{caseId:D}/Assessment");
        Assert.Contains("Current report: Rendering", html, StringComparison.Ordinal);
        Assert.Contains("Retry report draft", html, StringComparison.Ordinal);

        using var response = await client.PostAsync(
            $"/Cases/{caseId:D}/Assessment?handler=GenerateReportDraft",
            Form(
                AntiforgeryValue(html),
                ("id", caseId.ToString("D")),
                ("operationKey", NewOperationKey()),
                ("reportVersionId", version.Id.ToString("D"))));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal([4, 5, 6], await response.Content.ReadAsByteArrayAsync());
        Assert.Equal(1, store.BeginCount);
    }

    private static AssessmentReportVersion StoredVersion(
        Guid caseId,
        AssessmentReportGenerationState state,
        DateTimeOffset? nextAttemptAtUtc = null,
        DateTimeOffset? leaseExpiresAtUtc = null)
    {
        var snapshot = AssessmentReportProjection.Project(ReadyInput(caseId)).Snapshot!;
        return new(
            Guid.NewGuid(),
            caseId,
            1,
            AssessmentReportPayload.Key(snapshot),
            state,
            AssessmentReportPayload.Serialize(snapshot),
            null,
            [],
            DateTimeOffset.UtcNow,
            null,
            "Renderer unavailable",
            1,
            nextAttemptAtUtc,
            leaseExpiresAtUtc);
    }

    private static WebApplicationFactory<Program> Compose(
        IntakeWebApplicationFactory baseFactory,
        IGetCase getCase,
        IGetCaseAssessment getCaseAssessment,
        IAssessmentReportProjectionSource projectionSource,
        IAssessmentReportRenderer renderer,
        FakeAssessmentReportStore? reportStore = null) =>
        baseFactory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IGetCase>();
                services.RemoveAll<IGetCaseAssessment>();
                services.RemoveAll<IAssessmentReportProjectionSource>();
                services.RemoveAll<IAssessmentReportRenderer>();
                services.RemoveAll<IAssessmentReportStore>();
                services.AddSingleton(getCase);
                services.AddSingleton(getCaseAssessment);
                services.AddSingleton(projectionSource);
                services.AddSingleton(renderer);
                services.AddSingleton<IAssessmentReportStore>(reportStore ?? new FakeAssessmentReportStore());
            }));

    private static AssessmentReportProjectionInput ReadyInput(Guid caseId)
    {
        var image = new byte[] { 137, 80, 78, 71, 1, 2, 3, 4 };
        var photo = new ReportImageEvidence(
            "site.jpg", "image/jpeg", image, Convert.ToHexStringLower(SHA256.HashData(image)));
        var source = new AcceptedReportSource("instruction.pdf", "1", new string('a', 64));
        var repairCostSource = new AcceptedReportSource("estimate.pdf", "2", new string('b', 64));
        return new AssessmentReportProjectionInput(
            FullAssessmentProjection(caseId),
            ClaimantName: "Alex Example",
            OurReference: "CE-100",
            YourReference: "P-100",
            ReportFor: ["Approved Principal"],
            ReportDate: new DateOnly(2026, 8, 19),
            Photos: [photo],
            Sources: [source],
            Costs: new ReportRepairCosts(5m, 30m, 50m, 20m, 5m, true),
            RepairCostSource: repairCostSource,
            RepairSpecificationId: Guid.NewGuid(),
            RepairSpecificationVersion: 2);
    }

    /// <summary>
    /// Every assessment field <see cref="AssessmentPolicy.EvaluateReadiness"/>
    /// requires, confirmed — the same fixture shape as the Core projection
    /// tests (<c>tests/Pegasus.Core.Tests/Reports/AssessmentReportProjectionTests.cs</c>),
    /// so a "ready" web test genuinely reaches the renderer rather than
    /// tripping over the shared readiness rail.
    /// </summary>
    private static CaseAssessmentProjection FullAssessmentProjection(Guid caseId)
    {
        var confirmedAt = DateTimeOffset.UtcNow;
        AssessmentFieldValue Field(string path, string value) => new(
            path, value, ActorKind.Staff, "engineer-1", confirmedAt, "engineer-1", confirmedAt);

        var fields = new[]
        {
            Field(AssessmentVocabulary.VehicleType, "car"),
            Field(AssessmentVocabulary.VehicleYear, "2012"),
            Field(AssessmentVocabulary.VehicleMileageSource, "online_data"),
            Field(AssessmentVocabulary.VehicleCondition, "good"),
            Field(AssessmentVocabulary.IncidentAssessed, "2026-08-03"),
            Field(AssessmentVocabulary.ImpactSeverity, "moderate"),
            Field(AssessmentVocabulary.ImpactLocation, "right_rear"),
            Field(AssessmentVocabulary.ValueRetail, "5000.00"),
            Field(AssessmentVocabulary.ValueTrade, "4000.00"),
            Field(AssessmentVocabulary.ValueEngineer, "5000.00"),
            Field(AssessmentVocabulary.CostRepairerVatRegistered, "true"),
            Field(AssessmentVocabulary.Outcome, "repairable"),
            Field(AssessmentVocabulary.LegalStatus, "roadworthy"),
            Field(AssessmentVocabulary.HistoryCheck, "History clear"),
            Field(AssessmentVocabulary.EngineerName, "A Patterson"),
            Field(AssessmentVocabulary.EngineerQualifications, "M.Inst.IAEA"),
            Field(AssessmentVocabulary.EngineerSignature, "andy_patterson"),
            Field(AssessmentVocabulary.AgreedFee, "120.00"),
        };
        var caseOwned = new AssessmentCaseOwnedData(
            Registration: "PK12TMZ",
            Make: "Ford",
            Model: "Focus",
            Mileage: 80_000,
            MileageUnit: "miles",
            IncidentDate: new DateOnly(2026, 8, 1),
            InstructionDate: new DateOnly(2026, 8, 2),
            InspectionMode: "ImageBasedAssessment",
            InspectionAddress: null);
        return new CaseAssessmentProjection(
            caseId, "CE-100", 0, CaseLifecycleState.Review, Guid.NewGuid(), fields, [], caseOwned);
    }

    private static string NewOperationKey() => Guid.NewGuid().ToString("N");

    private static async Task<string> GetHtmlAsync(HttpClient client, string path)
    {
        using var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadAsStringAsync();
    }

    private static FormUrlEncodedContent Form(
        string antiforgeryToken, params (string Name, string Value)[] values)
    {
        var fields = values.ToDictionary(item => item.Name, item => item.Value, StringComparer.Ordinal);
        fields["__RequestVerificationToken"] = antiforgeryToken;
        return new(fields);
    }

    private static string AntiforgeryValue(string html)
    {
        var tag = AntiforgeryTagRegex().Match(html);
        Assert.True(tag.Success, "The case action must render an antiforgery token.");
        var value = ValueRegex().Match(tag.Value);
        Assert.True(value.Success, "The case antiforgery token must have a value.");
        return WebUtility.HtmlDecode(value.Groups["value"].Value);
    }

    [GeneratedRegex("<input[^>]*name=\"__RequestVerificationToken\"[^>]*>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex AntiforgeryTagRegex();

    [GeneratedRegex("value=\"(?<value>[^\"]+)\"", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ValueRegex();

    private sealed class FakeGetCase(Guid caseId) : IGetCase
    {
        public Task<CaseDetails?> ExecuteAsync(GetCaseQuery query, CancellationToken cancellationToken)
        {
            if (query.CaseId != caseId)
            {
                return Task.FromResult<CaseDetails?>(null);
            }

            var identity = new CaseIdentity(caseId, "QDOS", 2026, 42, "QDOS-2026-00042");
            var workflow = new CaseWorkflowRecord(
                caseId, identity, CaseLifecycleState.ReportPreparation, null, null,
                null, null, null, null, null, 0);
            var summary = new CaseSearchItem(
                caseId, identity.Reference, null, CaseType.Inspection, "Approved Principal",
                workflow.State, null, "AB12CDE", "Alex Example", "P-100",
                DateTimeOffset.UtcNow, new DateOnly(2026, 8, 1), "Email", DateTimeOffset.UtcNow);
            CaseDetails details = new(
                summary, workflow, null, [], null, CaseCustodyState.Pending, [], [], []);
            return Task.FromResult<CaseDetails?>(details);
        }
    }

    private sealed class FakeGetCaseAssessment(CaseAssessmentProjection projection) : IGetCaseAssessment
    {
        public Task<CaseAssessmentProjection?> ExecuteAsync(Guid caseId, CancellationToken cancellationToken) =>
            Task.FromResult<CaseAssessmentProjection?>(projection);
    }

    private sealed class FakeProjectionSource(AssessmentReportProjectionInput input)
        : IAssessmentReportProjectionSource
    {
        public Task<AssessmentReportProjectionInput?> GetAsync(
            Guid caseId,
            ActionActor actor,
            Guid? selectedRepairSpecificationId = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<AssessmentReportProjectionInput?>(input);
    }

    private sealed class FakeRenderer(byte[] pdfBytes) : IAssessmentReportRenderer
    {
        public Task<AssessmentReportDraft> RenderAsync(
            AssessmentReportSnapshot snapshot, CancellationToken cancellationToken = default) =>
            Task.FromResult(new AssessmentReportDraft(Artifact("assessment"), Artifact("fee-note")));

        private RenderedReportArtifact Artifact(string family) => new(
            $"{family}.pdf", pdfBytes, 1,
            Convert.ToHexStringLower(SHA256.HashData(pdfBytes)),
            AssessmentReportContract.TemplateVersion, "fake");
    }

    private sealed class FakeAssessmentReportStore : IAssessmentReportStore
    {
        private readonly Dictionary<string, (AssessmentReportVersion Version, AssessmentReportDraft Draft)> versions = [];

        public int BeginCount { get; private set; }

        public void Seed(AssessmentReportVersion version)
        {
            var assessment = new RenderedReportArtifact(
                "assessment.pdf",
                [1],
                1,
                Convert.ToHexStringLower(SHA256.HashData([1])),
                AssessmentReportContract.TemplateVersion,
                "seed");
            var feeNote = new RenderedReportArtifact(
                "fee-note.pdf",
                [2],
                1,
                Convert.ToHexStringLower(SHA256.HashData([2])),
                AssessmentReportContract.TemplateVersion,
                "seed");
            versions[version.LogicalKey.Value] = (version, new(assessment, feeNote));
        }

        public Task<IReadOnlyList<AssessmentReportVersion>> ListAsync(
            Guid caseId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AssessmentReportVersion>>(
                versions.Values
                    .Where(item => item.Version.CaseId == caseId)
                    .Select(item => item.Version)
                    .OrderByDescending(item => item.Version)
                    .ToArray());

        public Task<AssessmentReportGenerationReservation> BeginAsync(
            AssessmentReportGenerationRequest request,
            CancellationToken cancellationToken = default)
        {
            BeginCount++;
            var payload = AssessmentReportPayload.Serialize(request.Snapshot);
            var key = AssessmentReportPayload.Key(request.Snapshot);
            if (versions.TryGetValue(key.Value, out var existing))
            {
                if (existing.Version.State == AssessmentReportGenerationState.Rendering
                    && existing.Version.LeaseExpiresAtUtc is { } leaseExpiresAtUtc
                    && leaseExpiresAtUtc <= DateTimeOffset.UtcNow)
                {
                    var reclaimed = existing.Version with
                    {
                        AttemptCount = existing.Version.AttemptCount + 1,
                        LeaseExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(5)
                    };
                    versions[key.Value] = (reclaimed, existing.Draft);
                    return Task.FromResult(new AssessmentReportGenerationReservation(
                        reclaimed,
                        "reclaimed",
                        ShouldRender: true));
                }

                return Task.FromResult(new AssessmentReportGenerationReservation(
                    existing.Version,
                    string.Empty,
                    ShouldRender: false));
            }

            var version = new AssessmentReportVersion(
                Guid.NewGuid(),
                request.CaseId,
                1,
                key,
                AssessmentReportGenerationState.Rendering,
                payload,
                null,
                [],
                DateTimeOffset.UtcNow,
                null,
                null);
            return Task.FromResult(new AssessmentReportGenerationReservation(version, "fake", ShouldRender: true));
        }

        public Task<AssessmentReportDraft?> ReadDraftAsync(
            AssessmentReportVersion version,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                versions.TryGetValue(version.LogicalKey.Value, out var value)
                    ? (AssessmentReportDraft?)value.Draft
                    : null);

        public Task<AssessmentReportVersion> CompleteAsync(
            AssessmentReportGenerationReservation reservation,
            AssessmentReportDraft draft,
            CancellationToken cancellationToken = default)
        {
            var artifacts = new[]
            {
                ToArtifact(AssessmentReportArtifactKind.Assessment, draft.Assessment),
                ToArtifact(AssessmentReportArtifactKind.FeeNote, draft.FeeNote)
            };
            var completed = reservation.Version with
            {
                State = AssessmentReportGenerationState.Generated,
                Artifacts = artifacts,
                CompletedAtUtc = DateTimeOffset.UtcNow
            };
            versions[completed.LogicalKey.Value] = (completed, draft);
            return Task.FromResult(completed);
        }

        public Task FailAsync(
            AssessmentReportGenerationReservation reservation,
            string reason,
            CancellationToken cancellationToken = default) => Task.CompletedTask;

        private static AssessmentReportArtifact ToArtifact(
            AssessmentReportArtifactKind kind,
            RenderedReportArtifact artifact) => new(
                Guid.NewGuid(),
                kind,
                artifact.SuggestedFileName,
                "application/pdf",
                artifact.Pdf.LongLength,
                artifact.Sha256,
                artifact.PageCount,
                artifact.TemplateVersion,
                artifact.EngineVersion);
    }
}
