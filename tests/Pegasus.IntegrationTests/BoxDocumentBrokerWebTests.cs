using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Pegasus.Contracts;
using Pegasus.Contracts.ProblemDetails;
using Pegasus.Contracts.Responses;
using Pegasus.Core.Cases;
using Pegasus.Core.Documents;
using Pegasus.Core.Identity;
using Pegasus.Core.Workflow;
using Pegasus.Web.Api;

namespace Pegasus.IntegrationTests;

[Trait("Category", "SqlServer")]
public sealed class BoxDocumentBrokerWebTests
{
    private static readonly Guid CaseId = Guid.Parse("10213243-5465-7687-98a9-bacbdcedfe0f");
    private static readonly Guid DocumentId = Guid.Parse("20314253-6475-8697-a8b9-cadbecfd0e1f");
    private static readonly Guid OccurrenceId = Guid.Parse("30415263-7485-96a7-b8c9-daebfc0d1e2f");
    private static readonly Guid VersionId = Guid.Parse("40516273-8495-a6b7-c8d9-eafb0c1d2e3f");
    private const string CaseVersion = "7";

    [Fact]
    public async Task MetadataIsProjectedAndUsesCaseVersionEtag()
    {
        var getCase = new RecordingGetCase(CreateDetails());
        using var factory = CreateFactory(getCase);
        using var client = CreateClient(factory);

        using var response = await client.GetAsync(
            $"{DesktopGateway.BasePath}/cases/{CaseId:D}/documents");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("\"7\"", response.Headers.ETag?.Tag);
        var body = await DeserializeAsync<DocumentListResponse>(response);
        var document = Assert.Single(body.Items);
        Assert.Equal(OccurrenceId, document.OccurrenceId);
        Assert.Equal("image/jpeg", document.MediaType);
        Assert.Equal("Image", document.SemanticRole);
        Assert.Equal(CaseVersion, body.Version.ToString(System.Globalization.CultureInfo.InvariantCulture));

        using var cachedRequest = new HttpRequestMessage(
            HttpMethod.Get,
            $"{DesktopGateway.BasePath}/cases/{CaseId:D}/documents");
        cachedRequest.Headers.TryAddWithoutValidation("If-None-Match", "W/\"7\"");
        using var cachedResponse = await client.SendAsync(cachedRequest);
        Assert.Equal(HttpStatusCode.NotModified, cachedResponse.StatusCode);
    }

    [Fact]
    public async Task DownloadAuthorizesCaseBeforeCallingContentPortAndSupportsRange()
    {
        var events = new List<string>();
        var getCase = new RecordingGetCase(CreateDetails(), events);
        var download = new RecordingDownload(events);
        using var factory = CreateFactory(getCase, download);
        using var client = CreateClient(factory);
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"{DesktopGateway.BasePath}/cases/{CaseId:D}/documents/{OccurrenceId:D}/content");
        request.Headers.Range = new RangeHeaderValue(1, 3);

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.PartialContent, response.StatusCode);
        Assert.Equal("bytes 1-3/10", response.Content.Headers.ContentRange?.ToString());
        Assert.Equal("123", await response.Content.ReadAsStringAsync());
        Assert.Equal(1, getCase.Calls);
        Assert.True(download.CalledAfter(getCase));
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        AssertPrivateNoStore(response);
        Assert.DoesNotContain("box.com", response.Headers.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task MetadataIfNoneMatchUsesWeakCommaSeparatedAndWildcardMatching()
    {
        using var factory = CreateFactory(new RecordingGetCase(CreateDetails()));
        using var client = CreateClient(factory);

        foreach (var header in new[] { "W/\"other\", \"7\"", "*" })
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"{DesktopGateway.BasePath}/cases/{CaseId:D}/documents");
            request.Headers.TryAddWithoutValidation("If-None-Match", header);

            using var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.NotModified, response.StatusCode);
        }
    }

    [Fact]
    public async Task DownloadIfNoneMatchAndInvalidRangeRetainPrivateNoStoreCachePolicy()
    {
        using var factory = CreateFactory(
            new RecordingGetCase(CreateDetails()),
            new RecordingDownload([]));
        using var client = CreateClient(factory);

        using var notModifiedRequest = new HttpRequestMessage(
            HttpMethod.Get,
            $"{DesktopGateway.BasePath}/cases/{CaseId:D}/documents/{OccurrenceId:D}/content");
        notModifiedRequest.Headers.TryAddWithoutValidation(
            "If-None-Match",
            "W/\"0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef\"");
        using var notModified = await client.SendAsync(notModifiedRequest);
        Assert.Equal(HttpStatusCode.NotModified, notModified.StatusCode);
        AssertPrivateNoStore(notModified);

        using var rangeRequest = new HttpRequestMessage(
            HttpMethod.Get,
            $"{DesktopGateway.BasePath}/cases/{CaseId:D}/documents/{OccurrenceId:D}/content");
        rangeRequest.Headers.Range = new RangeHeaderValue(99, null);
        using var range = await client.SendAsync(rangeRequest);
        Assert.Equal(HttpStatusCode.RequestedRangeNotSatisfiable, range.StatusCode);
        AssertPrivateNoStore(range);
    }

    [Fact]
    public async Task UnauthenticatedRequestsReturnApi401InsteadOfCookieRedirect()
    {
        using var factory = CreateFactory(new RecordingGetCase(CreateDetails()));
        using var client = CreateClient(factory);
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"{DesktopGateway.BasePath}/cases/{CaseId:D}/documents");
        request.Headers.Add("X-Test-Anonymous", "true");

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("Bearer", response.Headers.WwwAuthenticate.Single().Scheme);
        var problem = await DeserializeAsync<PegasusProblem>(response);
        Assert.Equal(PegasusProblemTypes.NotAuthorized, problem.Type);
    }

    [Fact]
    public async Task CaseAuthorizationFailureReturns403BeforeContentPort()
    {
        var getCase = new RecordingGetCase(
            new StaffAuthorizationException(StaffAccessRight.PerformCasework));
        var download = new RecordingDownload([]);
        using var factory = CreateFactory(getCase, download);
        using var client = CreateClient(factory);

        using var response = await client.GetAsync(
            $"{DesktopGateway.BasePath}/cases/{CaseId:D}/documents/{OccurrenceId:D}/content");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.False(download.WasCalled);
        var problem = await DeserializeAsync<PegasusProblem>(response);
        Assert.Equal(PegasusProblemTypes.NotAuthorized, problem.Type);
    }

    [Fact]
    public async Task UploadSessionEnforcesTenMiBAndCompletesThroughCorePort()
    {
        var getCase = new RecordingGetCase(CreateDetails());
        var addDocument = new RecordingAddDocument();
        using var factory = CreateFactory(getCase, addDocument: addDocument);
        using var client = CreateClient(factory);
        var start = await client.PostAsJsonAsync(
            $"{DesktopGateway.BasePath}/cases/{CaseId:D}/documents/upload-session",
            new CreateDocumentUploadSessionRequest
            {
                FileName = "upload.txt",
                MediaType = "text/plain",
                SemanticRole = "Other"
            });
        Assert.Equal(HttpStatusCode.Created, start.StatusCode);
        var session = await DeserializeAsync<DocumentUploadSessionResponse>(start);

        var content = Encoding.UTF8.GetBytes("upload content");
        using var putContent = new ByteArrayContent(content);
        putContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        using var put = await client.PutAsync(
            $"{DesktopGateway.BasePath}/upload-sessions/{session.SessionId:D}",
            putContent);
        Assert.Equal(HttpStatusCode.NoContent, put.StatusCode);

        using var complete = await client.PostAsJsonAsync(
            $"{DesktopGateway.BasePath}/upload-sessions/{session.SessionId:D}/complete",
            new CompleteDocumentUploadRequest
            {
                ExpectedVersion = 7,
                OperationKey = "desk:upload-1",
                EditLeaseToken = "lease"
            });
        Assert.Equal(HttpStatusCode.Created, complete.StatusCode);
        Assert.Equal(content, addDocument.Content);
        Assert.Equal("desk:upload-1", addDocument.Command?.OperationKey);

        var response = await DeserializeAsync<DocumentUploadCompletionResponse>(complete);
        Assert.Equal(OccurrenceId, response.Document.OccurrenceId);
        Assert.False(response.IsReplay);

        using var replay = await client.PostAsJsonAsync(
            $"{DesktopGateway.BasePath}/upload-sessions/{session.SessionId:D}/complete",
            new CompleteDocumentUploadRequest
            {
                ExpectedVersion = 7,
                OperationKey = "desk:upload-1",
                EditLeaseToken = "lease"
            });
        Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        Assert.Equal(1, addDocument.Calls);
    }

    [Fact]
    public async Task ConcurrentSameKeyCompletionsCallCoreOnceAndReplayTheStoredResponse()
    {
        var getCase = new RecordingGetCase(CreateDetails());
        var addDocument = new BlockingAddDocument();
        using var factory = CreateFactory(getCase, addDocument: addDocument);
        using var client = CreateClient(factory);
        var session = await CreateUploadSessionAsync(client, "concurrent-same-key.txt");
        await PutUploadContentAsync(client, session.SessionId, "concurrent content");

        var first = CompleteUploadAsync(client, session.SessionId, "desk:concurrent-same");
        await addDocument.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        var second = CompleteUploadAsync(client, session.SessionId, "desk:concurrent-same");
        await Task.Delay(100);
        addDocument.Release.TrySetResult();

        using var firstResponse = await first;
        using var secondResponse = await second;
        var statuses = new[] { firstResponse.StatusCode, secondResponse.StatusCode };
        Assert.Equal(1, statuses.Count(status => status == HttpStatusCode.Created));
        Assert.Equal(1, statuses.Count(status => status == HttpStatusCode.OK));
        Assert.Equal(1, addDocument.Calls);
    }

    [Fact]
    public async Task ConcurrentDifferentKeyCompletionsCallCoreOnceAndConflictTheSecondKey()
    {
        var getCase = new RecordingGetCase(CreateDetails());
        var addDocument = new BlockingAddDocument();
        using var factory = CreateFactory(getCase, addDocument: addDocument);
        using var client = CreateClient(factory);
        var session = await CreateUploadSessionAsync(client, "concurrent-different-key.txt");
        await PutUploadContentAsync(client, session.SessionId, "concurrent content");

        var first = CompleteUploadAsync(client, session.SessionId, "desk:concurrent-first");
        await addDocument.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        var second = CompleteUploadAsync(client, session.SessionId, "desk:concurrent-second");
        await Task.Delay(100);
        addDocument.Release.TrySetResult();

        using var firstResponse = await first;
        using var secondResponse = await second;
        var statuses = new[] { firstResponse.StatusCode, secondResponse.StatusCode };
        Assert.Contains(HttpStatusCode.Created, statuses);
        Assert.Contains(HttpStatusCode.Conflict, statuses);
        Assert.Equal(1, addDocument.Calls);
        var problemResponse = firstResponse.StatusCode == HttpStatusCode.Conflict
            ? firstResponse
            : secondResponse;
        var problem = await DeserializeAsync<PegasusProblem>(problemResponse);
        Assert.Equal(PegasusProblemTypes.OperationConflict, problem.Type);
    }

    [Fact]
    public async Task LogicalRemovalRequiresReasonAndDelegatesIdempotencyToCore()
    {
        var getCase = new RecordingGetCase(CreateDetails());
        var remove = new RecordingRemoveDocument();
        using var factory = CreateFactory(getCase, remove: remove);
        using var client = CreateClient(factory);
        var payload = new RemoveDocumentRequest
        {
            ExpectedVersion = 7,
            OperationKey = "desk:remove-1",
            EditLeaseToken = "lease",
            Reason = "Duplicate upload"
        };

        using var response = await client.SendAsync(new HttpRequestMessage(
            HttpMethod.Delete,
            $"{DesktopGateway.BasePath}/cases/{CaseId:D}/documents/{OccurrenceId:D}")
        {
            Content = JsonContent.Create(payload)
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Duplicate upload", remove.Command?.Reason);
        Assert.Equal(1, remove.Calls);
    }

    [Fact]
    public async Task LogicalRemovalLeaseFailureIsReturnedAsProblemDetails()
    {
        var getCase = new RecordingGetCase(CreateDetails());
        var remove = new RecordingRemoveDocument(
            new CaseEditLeaseConflictException(CaseId, 7));
        using var factory = CreateFactory(getCase, remove: remove);
        using var client = CreateClient(factory);
        using var response = await client.SendAsync(new HttpRequestMessage(
            HttpMethod.Delete,
            $"{DesktopGateway.BasePath}/cases/{CaseId:D}/documents/{OccurrenceId:D}")
        {
            Content = JsonContent.Create(new RemoveDocumentRequest
            {
                ExpectedVersion = 7,
                OperationKey = "desk:remove-lease",
                EditLeaseToken = "lease",
                Reason = "Duplicate upload"
            })
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await DeserializeAsync<PegasusProblem>(response);
        Assert.Equal(PegasusProblemTypes.LeaseConflict, problem.Type);
        Assert.Equal(1, remove.Calls);
    }

    [Fact]
    public async Task ThirdPartyEvidenceConfirmationDelegatesReasonAndConcurrencyFields()
    {
        var getCase = new RecordingGetCase(CreateDetails());
        var confirm = new RecordingConfirmEvidence();
        using var factory = CreateFactory(getCase, confirm: confirm);
        using var client = CreateClient(factory);
        using var response = await client.PostAsJsonAsync(
            $"{DesktopGateway.BasePath}/cases/{CaseId:D}/third-party-vehicle-evidence/confirm",
            new ConfirmThirdPartyEvidenceRequest
            {
                OccurrenceId = OccurrenceId,
                ExpectedVersion = 7,
                OperationKey = "desk:confirm-1",
                EditLeaseToken = "lease",
                Reason = "Vehicle evidence confirmed"
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, confirm.Calls);
        Assert.Equal(OccurrenceId, confirm.Command?.OccurrenceId);
        Assert.Equal("desk:confirm-1", confirm.Command?.OperationKey);
        Assert.Equal("Vehicle evidence confirmed", confirm.Command?.Reason);
    }

    [Fact]
    public async Task UploadSessionQuotaReturnsRateLimitedProblemAfterActiveLimit()
    {
        using var factory = CreateFactory(new RecordingGetCase(CreateDetails()));
        using var client = CreateClient(factory);

        for (var index = 0; index < DesktopDocumentUploadSessions.MaximumActiveSessions; index++)
        {
            using var response = await client.PostAsJsonAsync(
                $"{DesktopGateway.BasePath}/cases/{CaseId:D}/documents/upload-session",
                new CreateDocumentUploadSessionRequest
                {
                    FileName = $"upload-{index}.txt",
                    MediaType = "text/plain",
                    SemanticRole = "Other"
                });
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        using var overLimit = await client.PostAsJsonAsync(
            $"{DesktopGateway.BasePath}/cases/{CaseId:D}/documents/upload-session",
            new CreateDocumentUploadSessionRequest
            {
                FileName = "over-limit.txt",
                MediaType = "text/plain",
                SemanticRole = "Other"
            });

        Assert.Equal(HttpStatusCode.TooManyRequests, overLimit.StatusCode);
        var problem = await DeserializeAsync<PegasusProblem>(overLimit);
        Assert.Equal(PegasusProblemTypes.RateLimited, problem.Type);
    }

    [Fact]
    public void ExpiredSessionCleanupReleasesActiveAndBufferedQuotas()
    {
        var clock = new TestTimeProvider(DateTimeOffset.UnixEpoch);
        var sessions = new DesktopDocumentUploadSessions(clock);
        var actor = ActionActor.Staff(
            Guid.Parse("50617283-94a5-b6c7-d8e9-fafb0c1d2e3f"),
            [StaffRole.Administrator]);

        Assert.True(sessions.TryCreate(
            CaseId,
            actor,
            "bounded.bin",
            "application/octet-stream",
            DocumentSemanticRole.Other,
            out var session));
        Assert.NotNull(session);
        Assert.True(session!.TrySetContent(
            new byte[(int)DesktopDocumentUploadSessions.MaximumBufferedBytes]));

        Assert.True(sessions.TryCreate(
            CaseId,
            actor,
            "another.bin",
            "application/octet-stream",
            DocumentSemanticRole.Other,
            out var another));
        Assert.NotNull(another);
        Assert.False(another!.TrySetContent([1]));

        clock.Advance(TimeSpan.FromMinutes(31));
        Assert.True(sessions.TryCreate(
            CaseId,
            actor,
            "after-expiry.bin",
            "application/octet-stream",
            DocumentSemanticRole.Other,
            out var afterExpiry));
        Assert.NotNull(afterExpiry);
        Assert.True(afterExpiry!.TrySetContent(
            new byte[(int)DesktopDocumentUploadSessions.MaximumBufferedBytes]));
    }

    [Fact]
    public void NonOwnerLookupDoesNotDiscardSessionOrLeakBufferedQuota()
    {
        var clock = new TestTimeProvider(DateTimeOffset.UnixEpoch);
        var sessions = new DesktopDocumentUploadSessions(clock);
        var owner = ActionActor.Staff(
            Guid.Parse("50617283-94a5-b6c7-d8e9-fafb0c1d2e3f"),
            [StaffRole.Administrator]);
        var otherActor = ActionActor.Staff(
            Guid.Parse("61728394-a5b6-c7d8-e9fa-bc0d1e2f3a4b"),
            [StaffRole.Administrator]);

        Assert.True(sessions.TryCreate(
            CaseId,
            owner,
            "bounded.bin",
            "application/octet-stream",
            DocumentSemanticRole.Other,
            out var session));
        Assert.NotNull(session);
        Assert.True(session!.TrySetContent(
            new byte[(int)DesktopDocumentUploadSessions.MaximumBufferedBytes]));

        Assert.Null(sessions.Find(session.Id, otherActor));
        Assert.Same(session, sessions.Find(session.Id, owner));

        clock.Advance(TimeSpan.FromMinutes(31));
        Assert.True(sessions.TryCreate(
            CaseId,
            owner,
            "after-expiry.bin",
            "application/octet-stream",
            DocumentSemanticRole.Other,
            out var afterExpiry));
        Assert.NotNull(afterExpiry);
        Assert.True(afterExpiry!.TrySetContent(
            new byte[(int)DesktopDocumentUploadSessions.MaximumBufferedBytes]));
    }

    [Fact]
    public async Task OversizedUploadIsRejectedWithoutCallingCore()
    {
        var getCase = new RecordingGetCase(CreateDetails());
        var addDocument = new RecordingAddDocument();
        using var factory = CreateFactory(getCase, addDocument: addDocument);
        using var client = CreateClient(factory);
        using var start = await client.PostAsJsonAsync(
            $"{DesktopGateway.BasePath}/cases/{CaseId:D}/documents/upload-session",
            new CreateDocumentUploadSessionRequest
            {
                FileName = "large.bin",
                MediaType = "application/octet-stream",
                SemanticRole = "Other"
            });
        var session = await DeserializeAsync<DocumentUploadSessionResponse>(start);

        using var oversized = new ByteArrayContent(new byte[10 * 1024 * 1024 + 1]);
        using var response = await client.PutAsync(
            $"{DesktopGateway.BasePath}/upload-sessions/{session.SessionId:D}",
            oversized);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, addDocument.Calls);
        var problem = await DeserializeAsync<PegasusProblem>(response);
        Assert.Equal(PegasusProblemTypes.Validation, problem.Type);
    }

    private static async Task<DocumentUploadSessionResponse> CreateUploadSessionAsync(
        HttpClient client,
        string fileName)
    {
        using var response = await client.PostAsJsonAsync(
            $"{DesktopGateway.BasePath}/cases/{CaseId:D}/documents/upload-session",
            new CreateDocumentUploadSessionRequest
            {
                FileName = fileName,
                MediaType = "text/plain",
                SemanticRole = "Other"
            });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await DeserializeAsync<DocumentUploadSessionResponse>(response);
    }

    private static async Task PutUploadContentAsync(
        HttpClient client,
        Guid sessionId,
        string content)
    {
        using var requestContent = new StringContent(content, Encoding.UTF8, "text/plain");
        using var response = await client.PutAsync(
            $"{DesktopGateway.BasePath}/upload-sessions/{sessionId:D}",
            requestContent);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    private static Task<HttpResponseMessage> CompleteUploadAsync(
        HttpClient client,
        Guid sessionId,
        string operationKey) =>
        client.PostAsJsonAsync(
            $"{DesktopGateway.BasePath}/upload-sessions/{sessionId:D}/complete",
            new CompleteDocumentUploadRequest
            {
                ExpectedVersion = 7,
                OperationKey = operationKey,
                EditLeaseToken = "lease"
            });

    private static WebApplicationFactory<Program> CreateFactory(
        RecordingGetCase getCase,
        RecordingDownload? download = null,
        IAddCaseDocument? addDocument = null,
        RecordingRemoveDocument? remove = null,
        RecordingConfirmEvidence? confirm = null)
    {
        var baseFactory = new IntakeWebApplicationFactory(useIntegrationTestAuthentication: true);
        return baseFactory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting(DesktopGateway.FeatureFlag, "true");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IGetCase>();
                services.AddSingleton<IGetCase>(getCase);
                if (download is not null)
                {
                    services.RemoveAll<IDownloadCaseDocument>();
                    services.AddSingleton<IDownloadCaseDocument>(download);
                }
                if (addDocument is not null)
                {
                    services.RemoveAll<IAddCaseDocument>();
                    services.AddSingleton<IAddCaseDocument>(addDocument);
                }
                if (remove is not null)
                {
                    services.RemoveAll<ILogicallyRemoveDocument>();
                    services.AddSingleton<ILogicallyRemoveDocument>(remove);
                }
                if (confirm is not null)
                {
                    services.RemoveAll<IConfirmThirdPartyVehicleEvidence>();
                    services.AddSingleton<IConfirmThirdPartyVehicleEvidence>(confirm);
                }
                services.RemoveAll<ICaseDocumentStateQueries>();
                services.AddSingleton<ICaseDocumentStateQueries>(
                    new RecordingDocumentStateQueries());
            });
        });
    }

    private static HttpClient CreateClient(WebApplicationFactory<Program> factory) =>
        factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost:7139")
        });

    private static CaseDetails CreateDetails()
    {
        var version = new DocumentVersion(
            VersionId,
            DocumentId,
            1,
            "photo.jpg",
            "image/jpeg",
            10,
            "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef",
            DocumentCustodyStatus.Confirmed,
            new DateTimeOffset(2031, 5, 6, 10, 0, 0, TimeSpan.Zero),
            "Staff:operator",
            true,
            false,
            null);
        var occurrence = new DocumentOccurrence(
            OccurrenceId,
            CaseId,
            DocumentId,
            VersionId,
            DocumentSemanticRole.Image,
            DocumentSource.StaffUpload,
            "staff-upload:one",
            version.CreatedAtUtc,
            null,
            null,
            1);
        var document = new CaseDocument(DocumentId, CaseId, [occurrence], [version]);
        var identity = new CaseIdentity(CaseId, "P", 2031, 1, "CASE-1");
        var workflow = new CaseWorkflowRecord(
            CaseId,
            identity,
            CaseLifecycleState.Review,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            7);
        var summary = new CaseSearchItem(
            CaseId,
            "CASE-1",
            null,
            CaseType.Inspection,
            "P",
            CaseLifecycleState.Review,
            null,
            null,
            null,
            null,
            version.CreatedAtUtc,
            null,
            "test",
            version.CreatedAtUtc);
        return new(
            summary,
            workflow,
            null,
            [document],
            null,
            CaseCustodyState.Pending,
            [],
            [],
            []);
    }

    private static async Task<T> DeserializeAsync<T>(HttpResponseMessage response)
    {
        var value = await response.Content.ReadFromJsonAsync<T>(PegasusJson.Options);
        Assert.NotNull(value);
        return value!;
    }

    private static void AssertPrivateNoStore(HttpResponseMessage response)
    {
        var cacheControl = response.Headers.CacheControl;
        Assert.NotNull(cacheControl);
        Assert.True(cacheControl!.Private);
        Assert.True(cacheControl.NoStore);
    }

    private sealed class RecordingGetCase : IGetCase
    {
        private readonly CaseDetails? details;
        private readonly Exception? exception;
        private readonly List<string> events;

        public RecordingGetCase(CaseDetails details)
            : this(details, [])
        {
        }

        public RecordingGetCase(CaseDetails details, List<string> events)
        {
            this.details = details;
            this.events = events;
        }

        public RecordingGetCase(Exception exception)
            : this(exception, [])
        {
        }

        private RecordingGetCase(Exception exception, List<string> events)
        {
            this.exception = exception;
            this.events = events;
        }

        public int Calls { get; private set; }
        public IReadOnlyList<string> Events => events;

        public Task<CaseDetails?> ExecuteAsync(
            GetCaseQuery query,
            CancellationToken cancellationToken)
        {
            Calls++;
            events.Add("case");
            if (exception is not null)
            {
                throw exception;
            }

            return Task.FromResult(details);
        }
    }

    private sealed class RecordingDownload : IDownloadCaseDocument
    {
        private readonly List<string> events;

        public RecordingDownload(List<string> events) => this.events = events;

        public bool WasCalled { get; private set; }

        public bool CalledAfter(RecordingGetCase expected) =>
            WasCalled &&
            events.IndexOf("case") >= 0 &&
            events.IndexOf("download") > events.IndexOf("case");

        public Task<DocumentDownload?> ExecuteAsync(
            DownloadCaseDocumentQuery query,
            CancellationToken cancellationToken)
        {
            WasCalled = true;
            events.Add("download");
            return Task.FromResult<DocumentDownload?>(new(
                new MemoryStream(Encoding.UTF8.GetBytes("0123456789")),
                "photo.jpg",
                "image/jpeg",
                10,
                "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef"));
        }
    }

    private sealed class RecordingAddDocument : IAddCaseDocument
    {
        private readonly Exception? exception;

        public RecordingAddDocument()
        {
        }

        public RecordingAddDocument(Exception exception) => this.exception = exception;

        public int Calls { get; private set; }
        public AddCaseDocumentCommand? Command { get; private set; }
        public byte[]? Content => Command?.Content.ToArray();

        public Task<AddCaseDocumentResult> ExecuteAsync(
            AddCaseDocumentCommand command,
            CancellationToken cancellationToken)
        {
            Calls++;
            Command = command;
            if (exception is not null)
            {
                throw exception;
            }

            var version = new DocumentVersion(
                VersionId,
                DocumentId,
                1,
                command.FileName,
                command.MediaType,
                command.Content.Length,
                Convert.ToHexString(SHA256.HashData(command.Content.Span)).ToLowerInvariant(),
                DocumentCustodyStatus.Confirmed,
                new DateTimeOffset(2031, 5, 6, 10, 0, 0, TimeSpan.Zero),
                "Staff:operator",
                true,
                false,
                null);
            var occurrence = new DocumentOccurrence(
                OccurrenceId,
                command.CaseId,
                DocumentId,
                version.Id,
                command.SemanticRole,
                command.Source,
                command.SourceOccurrenceIdentity,
                version.CreatedAtUtc,
                null,
                null,
                1);
            return Task.FromResult(new AddCaseDocumentResult(occurrence, version, false));
        }
    }

    private sealed class BlockingAddDocument : IAddCaseDocument
    {
        private readonly RecordingAddDocument inner = new();

        public TaskCompletionSource Entered { get; } = new(
            TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(
            TaskCreationOptions.RunContinuationsAsynchronously);
        public int Calls => inner.Calls;

        public async Task<AddCaseDocumentResult> ExecuteAsync(
            AddCaseDocumentCommand command,
            CancellationToken cancellationToken)
        {
            Entered.TrySetResult();
            await Release.Task.WaitAsync(cancellationToken);
            return await inner.ExecuteAsync(command, cancellationToken);
        }
    }

    private sealed class RecordingRemoveDocument : ILogicallyRemoveDocument
    {
        private readonly Exception? exception;

        public RecordingRemoveDocument()
        {
        }

        public RecordingRemoveDocument(Exception exception) => this.exception = exception;

        public int Calls { get; private set; }
        public LogicallyRemoveDocumentCommand? Command { get; private set; }

        public Task ExecuteAsync(
            LogicallyRemoveDocumentCommand command,
            CancellationToken cancellationToken)
        {
            Calls++;
            Command = command;
            if (exception is not null)
            {
                throw exception;
            }

            return Task.CompletedTask;
        }
    }

    private sealed class RecordingConfirmEvidence : IConfirmThirdPartyVehicleEvidence
    {
        public int Calls { get; private set; }
        public ConfirmThirdPartyVehicleEvidenceCommand? Command { get; private set; }

        public Task ExecuteAsync(
            ConfirmThirdPartyVehicleEvidenceCommand command,
            CancellationToken cancellationToken)
        {
            Calls++;
            Command = command;
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingDocumentStateQueries : ICaseDocumentStateQueries
    {
        public Task<CaseDocumentState?> GetAsync(
            Guid caseId,
            CancellationToken cancellationToken) =>
            Task.FromResult<CaseDocumentState?>(new(caseId, 8));
    }

    private sealed class TestTimeProvider(DateTimeOffset initialUtcNow) : TimeProvider
    {
        private DateTimeOffset utcNow = initialUtcNow;

        public override DateTimeOffset GetUtcNow() => utcNow;

        public void Advance(TimeSpan duration) => utcNow = utcNow.Add(duration);
    }
}
