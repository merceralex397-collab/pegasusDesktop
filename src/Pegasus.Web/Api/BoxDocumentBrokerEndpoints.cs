using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Net.Http.Headers;
using Pegasus.Contracts;
using Pegasus.Contracts.Paging;
using Pegasus.Contracts.ProblemDetails;
using Pegasus.Contracts.Requests;
using Pegasus.Contracts.Responses;
using Pegasus.Core.Actors;
using Pegasus.Core.Cases;
using Pegasus.Core.Documents;
using Pegasus.Core.Identity;
using Pegasus.Core.Workflow;

namespace Pegasus.Web.Api;

internal static class BoxDocumentBrokerEndpoints
{
    private const string DefaultSort = "createdAtUtcDesc";
    private const string FileNameSort = "fileNameAsc";
    private const string StaffUploadIdentityPrefix = "staff-upload:";
    private const int MaximumFileNameLength = 255;
    private const int MaximumMediaTypeLength = 200;
    private const int MaximumSourceIdentityLength = 512;

    public static void MapBoxDocumentBroker(this RouteGroupBuilder group)
    {
        var documents = group.MapGroup("/cases/{caseId:guid}/documents")
            .WithTags("Case documents");

        documents.MapGet("", ListAsync)
            .WithName("ListCaseDocuments")
            .WithSummary("List case document metadata")
            .Produces<DocumentListResponse>()
            .Produces(StatusCodes.Status304NotModified)
            .ProducesProblem(StatusCodes.Status404NotFound);

        documents.MapGet("/{occurrenceId:guid}", GetMetadataAsync)
            .WithName("GetCaseDocumentMetadata")
            .WithSummary("Get case document metadata")
            .Produces<DocumentMetadataResponse>()
            .Produces(StatusCodes.Status304NotModified)
            .ProducesProblem(StatusCodes.Status404NotFound);

        documents.MapGet("/{occurrenceId:guid}/content", DownloadAsync)
            .WithName("DownloadCaseDocumentContent")
            .WithSummary("Stream case document content")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status206PartialContent)
            .Produces(StatusCodes.Status304NotModified)
            .Produces(StatusCodes.Status416RangeNotSatisfiable)
            .ProducesProblem(StatusCodes.Status404NotFound);

        documents.MapPost("/upload-session", StartUploadSessionAsync)
            .WithName("StartCaseDocumentUploadSession")
            .WithSummary("Start a case document upload session")
            .Produces<DocumentUploadSessionResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        documents.MapDelete("/{occurrenceId:guid}", RemoveAsync)
            .WithName("RemoveCaseDocument")
            .WithSummary("Logically remove a case document")
            .Accepts<RemoveDocumentRequest>("application/json")
            .Produces<DocumentMutationResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/cases/{caseId:guid}/third-party-vehicle-evidence/confirm", ConfirmThirdPartyEvidenceAsync)
            .WithName("ConfirmThirdPartyVehicleEvidence")
            .WithSummary("Confirm third-party vehicle evidence")
            .Produces<DocumentMutationResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/upload-sessions/{sessionId:guid}", PutUploadBytesAsync)
            .WithName("PutCaseDocumentUploadBytes")
            .WithSummary("Upload case document bytes")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status413PayloadTooLarge);

        group.MapPost("/upload-sessions/{sessionId:guid}/complete", CompleteUploadSessionAsync)
            .WithName("CompleteCaseDocumentUploadSession")
            .WithSummary("Complete a case document upload session")
            .Produces<DocumentUploadCompletionResponse>(StatusCodes.Status200OK)
            .Produces<DocumentUploadCompletionResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> ListAsync(
        Guid caseId,
        IGetCase getCase,
        HttpContext httpContext,
        int page = 1,
        int pageSize = 25,
        string? sort = null,
        string? semanticRole = null,
        bool includeRemoved = false,
        CancellationToken cancellationToken = default)
    {
        var actor = DesktopGatewayActors.Get(httpContext);
        var details = await AuthorizeCaseAsync(getCase, caseId, actor, cancellationToken);
        if (details is null)
        {
            return TypedResults.NotFound();
        }

        ValidatePaging(page, pageSize, sort);
        var all = Flatten(details, includeRemoved, semanticRole);
        var ordered = sort?.Equals(FileNameSort, StringComparison.OrdinalIgnoreCase) == true
            ? all.OrderBy(item => item.FileName, StringComparer.OrdinalIgnoreCase)
                .ThenByDescending(item => item.CreatedAtUtc)
            : all.OrderByDescending(item => item.CreatedAtUtc)
                .ThenBy(item => item.OccurrenceId);
        var items = ordered.Skip((page - 1) * pageSize).Take(pageSize).ToArray();
        var hasNext = ordered.Skip(page * pageSize).Any();
        var response = new DocumentListResponse(
            items,
            details.Workflow.Version,
            page,
            pageSize,
            page > 1,
            hasNext);
        return WithNotModified(httpContext, response, details.Workflow.Version);
    }

    private static async Task<IResult> GetMetadataAsync(
        Guid caseId,
        Guid occurrenceId,
        IGetCase getCase,
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        var actor = DesktopGatewayActors.Get(httpContext);
        var details = await AuthorizeCaseAsync(getCase, caseId, actor, cancellationToken);
        var metadata = details is null
            ? null
            : FindMetadata(details, occurrenceId);
        if (metadata is null)
        {
            return TypedResults.NotFound();
        }

        return WithNotModified(httpContext, metadata, details!.Workflow.Version);
    }

    private static async Task<IResult> DownloadAsync(
        Guid caseId,
        Guid occurrenceId,
        IGetCase getCase,
        IDownloadCaseDocument downloadDocument,
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        var actor = DesktopGatewayActors.Get(httpContext);
        var details = await AuthorizeCaseAsync(getCase, caseId, actor, cancellationToken);
        var metadata = details is null
            ? null
            : FindMetadata(details, occurrenceId);
        if (metadata is null || metadata.IsLogicallyRemoved)
        {
            return TypedResults.NotFound();
        }

        var operationKey = $"desk:document-download:{Guid.NewGuid():N}";
        var download = await downloadDocument.ExecuteAsync(
            new(caseId, occurrenceId, metadata.VersionId, actor, operationKey),
            cancellationToken);
        if (download is null || !IsSafeFileName(download.FileName)
            || !IsSafeMediaType(download.MediaType)
            || download.ContentLength < 0
            || !IsSha256(download.Sha256))
        {
            if (download is not null)
            {
                await download.DisposeAsync();
            }

            return TypedResults.NotFound();
        }

        return new DocumentDownloadResult(download, httpContext.Request);
    }

    private static async Task<IResult> StartUploadSessionAsync(
        Guid caseId,
        CreateDocumentUploadSessionRequest request,
        IGetCase getCase,
        DesktopDocumentUploadSessions sessions,
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        var actor = DesktopGatewayActors.Get(httpContext);
        if (await AuthorizeCaseAsync(getCase, caseId, actor, cancellationToken) is null)
        {
            return TypedResults.NotFound();
        }

        var fileName = RequireFileName(request.FileName);
        var mediaType = RequireMediaType(request.MediaType);
        var semanticRole = ParseSemanticRole(request.SemanticRole);
        if (!sessions.TryCreate(caseId, actor, fileName, mediaType, semanticRole, out var session)
            || session is null)
        {
            return await RateLimitedAsync(
                httpContext,
                "The active upload-session quota has been reached. Retry after an existing session expires.");
        }

        var response = new DocumentUploadSessionResponse(
            session.Id,
            session.ExpiresAtUtc,
            Pegasus.Core.Intake.IntakeEnvelopeLimits.MaximumContentLength);
        return TypedResults.Created(
            $"{DesktopGateway.BasePath}/upload-sessions/{session.Id:D}",
            response);
    }

    private static async Task<IResult> PutUploadBytesAsync(
        Guid sessionId,
        DesktopDocumentUploadSessions sessions,
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        var actor = DesktopGatewayActors.Get(httpContext);
        var session = sessions.Find(sessionId, actor);
        if (session is null)
        {
            return TypedResults.NotFound();
        }

        var content = await ReadBoundedContentAsync(
            httpContext.Request.Body,
            Pegasus.Core.Intake.IntakeEnvelopeLimits.MaximumContentLength,
            cancellationToken);
        if (content is null)
        {
            return await ValidationAsync(
                httpContext,
                "The upload content must be between 1 byte and 10 MiB.");
        }

        if (!session.TrySetContent(content))
        {
            return await RateLimitedAsync(
                httpContext,
                "The buffered upload quota has been reached. Complete or wait for an existing session to expire.");
        }

        return TypedResults.NoContent();
    }

    private static async Task<IResult> CompleteUploadSessionAsync(
        Guid sessionId,
        CompleteDocumentUploadRequest request,
        IGetCase getCase,
        IAddCaseDocument addDocument,
        DesktopDocumentUploadSessions sessions,
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        var actor = DesktopGatewayActors.Get(httpContext);
        var session = sessions.Find(sessionId, actor);
        if (session is null)
        {
            return TypedResults.NotFound();
        }

        var details = await AuthorizeCaseAsync(getCase, session.CaseId, actor, cancellationToken);
        if (details is null)
        {
            return TypedResults.NotFound();
        }

        var operationKey = RequireOperationKey(request.OperationKey);
        if (request.ExpectedVersion < 0 || string.IsNullOrWhiteSpace(request.EditLeaseToken))
        {
            return await ValidationAsync(
                httpContext,
                "ExpectedVersion and EditLeaseToken are required.");
        }

        await session.WaitForCompletionAsync(cancellationToken);
        try
        {
            if (session.TryGetCompleted(operationKey, out var completed))
            {
                return TypedResults.Ok(completed);
            }

            if (!session.TryGetContent(out var content))
            {
                return await ValidationAsync(
                    httpContext,
                    "Upload bytes must be supplied before completion.");
            }

            var result = await addDocument.ExecuteAsync(
                new(
                    session.CaseId,
                    session.FileName,
                    session.MediaType,
                    content,
                    session.SemanticRole,
                    DocumentSource.StaffUpload,
                    StaffUploadIdentityPrefix + operationKey,
                    actor,
                    operationKey,
                    request.ExpectedVersion,
                    request.EditLeaseToken),
                cancellationToken);
            var response = new DocumentUploadCompletionResponse(
                ToMetadata(result.Occurrence, result.Version),
                result.IsReplay);
            session.SetCompleted(operationKey, response);
            return result.IsReplay
                ? TypedResults.Ok(response)
                : TypedResults.Created(
                    $"{DesktopGateway.BasePath}/cases/{session.CaseId:D}/documents/{result.Occurrence.Id:D}",
                    response);
        }
        finally
        {
            session.ReleaseCompletion();
        }
    }

    private static async Task<IResult> RemoveAsync(
        Guid caseId,
        Guid occurrenceId,
        IGetCase getCase,
        ILogicallyRemoveDocument removeDocument,
        ICaseDocumentStateQueries documentStateQueries,
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        var actor = DesktopGatewayActors.Get(httpContext);
        if (await AuthorizeCaseAsync(getCase, caseId, actor, cancellationToken) is null)
        {
            return TypedResults.NotFound();
        }

        var request = await JsonSerializer.DeserializeAsync<RemoveDocumentRequest>(
            httpContext.Request.Body,
            PegasusJson.Options,
            cancellationToken);
        if (request is null)
        {
            return await ValidationAsync(httpContext, "A removal request is required.");
        }

        ValidateMutation(request.ExpectedVersion, request.OperationKey, request.EditLeaseToken, request.Reason);
        await removeDocument.ExecuteAsync(
            new(
                caseId,
                occurrenceId,
                actor,
                request.Reason,
                RequireOperationKey(request.OperationKey),
                request.ExpectedVersion,
                request.EditLeaseToken),
            cancellationToken);
        var state = await documentStateQueries.GetAsync(caseId, cancellationToken)
            ?? throw new InvalidOperationException("The case document state is unavailable.");
        return TypedResults.Ok(new DocumentMutationResponse(caseId, occurrenceId, state.CaseVersion));
    }

    private static async Task<IResult> ConfirmThirdPartyEvidenceAsync(
        Guid caseId,
        ConfirmThirdPartyEvidenceRequest request,
        IGetCase getCase,
        IConfirmThirdPartyVehicleEvidence confirmEvidence,
        ICaseDocumentStateQueries documentStateQueries,
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        var actor = DesktopGatewayActors.Get(httpContext);
        if (await AuthorizeCaseAsync(getCase, caseId, actor, cancellationToken) is null)
        {
            return TypedResults.NotFound();
        }

        ValidateMutation(request.ExpectedVersion, request.OperationKey, request.EditLeaseToken, request.Reason);
        await confirmEvidence.ExecuteAsync(
            new(
                caseId,
                request.OccurrenceId,
                actor,
                request.Reason,
                RequireOperationKey(request.OperationKey),
                request.ExpectedVersion,
                request.EditLeaseToken),
            cancellationToken);
        var state = await documentStateQueries.GetAsync(caseId, cancellationToken)
            ?? throw new InvalidOperationException("The case document state is unavailable.");
        return TypedResults.Ok(new DocumentMutationResponse(caseId, request.OccurrenceId, state.CaseVersion));
    }

    private static async Task<CaseDetails?> AuthorizeCaseAsync(
        IGetCase getCase,
        Guid caseId,
        ActionActor actor,
        CancellationToken cancellationToken) =>
        await getCase.ExecuteAsync(new GetCaseQuery(caseId, actor), cancellationToken);

    private static DocumentMetadataResponse[] Flatten(
        CaseDetails details,
        bool includeRemoved,
        string? semanticRole)
    {
        DocumentSemanticRole? role = string.IsNullOrWhiteSpace(semanticRole)
            ? null
            : ParseSemanticRole(semanticRole);
        return details.Documents
            .SelectMany(document => document.Occurrences.Select(occurrence =>
            {
                var version = document.Versions.SingleOrDefault(item => item.Id == occurrence.VersionId)
                    ?? throw new InvalidDataException("The case document occurrence has no version.");
                return ToMetadata(occurrence, version);
            }))
            .Where(item => includeRemoved || !item.IsLogicallyRemoved)
            .Where(item => role is null || item.SemanticRole.Equals(role.ToString(), StringComparison.Ordinal))
            .ToArray();
    }

    private static DocumentMetadataResponse? FindMetadata(CaseDetails details, Guid occurrenceId) =>
        Flatten(details, includeRemoved: true, semanticRole: null)
            .SingleOrDefault(item => item.OccurrenceId == occurrenceId);

    private static DocumentMetadataResponse ToMetadata(
        DocumentOccurrence occurrence,
        DocumentVersion version) =>
        new(
            occurrence.CaseId,
            occurrence.DocumentId,
            occurrence.Id,
            version.Id,
            version.FileName,
            version.MediaType,
            version.ContentLength,
            version.Sha256,
            occurrence.SemanticRole.ToString(),
            occurrence.Source.ToString(),
            version.CustodyStatus.ToString(),
            version.CreatedAtUtc,
            version.CreatedBy,
            version.IsCurrent,
            version.IsLogicallyRemoved,
            version.RemovalReason,
            occurrence.SourceOccurrenceIdentity,
            occurrence.RecordedAtUtc,
            occurrence.ThirdPartyVehicleConfirmedAtUtc,
            occurrence.ThirdPartyVehicleConfirmationReason,
            occurrence.Ordinal);

    private static IResult WithNotModified<T>(HttpContext context, T response, long version)
    {
        var etag = $"W/\"{version}\"";
        context.Response.Headers.ETag = etag;
        if (WeakIfNoneMatchMatches(
                context.Request.Headers.IfNoneMatch.ToString(),
                EntityTagHeaderValue.Parse(etag)))
        {
            return TypedResults.StatusCode(StatusCodes.Status304NotModified);
        }

        return TypedResults.Ok(response);
    }

    private static bool WeakIfNoneMatchMatches(
        string header,
        EntityTagHeaderValue current)
    {
        if (string.IsNullOrWhiteSpace(header))
        {
            return false;
        }

        return EntityTagHeaderValue.TryParseList([header], out var values)
            && values.Any(value =>
                value == EntityTagHeaderValue.Any
                || value.Compare(current, useStrongComparison: false));
    }

    private static async Task<IResult> ValidationAsync(HttpContext context, string detail)
    {
        await DesktopGatewayProblems.WriteValidationAsync(
            context,
            detail,
            context.RequestAborted);
        return TypedResults.Empty;
    }

    private static async Task<IResult> RateLimitedAsync(HttpContext context, string detail)
    {
        await DesktopGatewayProblems.WriteRateLimitedAsync(
            context,
            detail,
            context.RequestAborted);
        return TypedResults.Empty;
    }

    private static void ValidatePaging(int page, int pageSize, string? sort)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(page, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(page, 10_000);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(pageSize, PagingLimits.MaxPageSize);

        if (!string.IsNullOrWhiteSpace(sort)
            && !sort.Equals(DefaultSort, StringComparison.OrdinalIgnoreCase)
            && !sort.Equals(FileNameSort, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("The document sort is not supported.", nameof(sort));
        }
    }

    private static void ValidateMutation(
        long expectedVersion,
        string operationKey,
        string editLeaseToken,
        string reason)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(expectedVersion);

        RequireOperationKey(operationKey);
        if (string.IsNullOrWhiteSpace(editLeaseToken))
        {
            throw new ArgumentException("An edit lease token is required.", nameof(editLeaseToken));
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("A reason is required.", nameof(reason));
        }
    }

    private static string RequireOperationKey(string operationKey)
    {
        var normalized = operationKey?.Trim();
        if (string.IsNullOrWhiteSpace(normalized)
            || !normalized.StartsWith(OperationKeys.DesktopPrefix, StringComparison.Ordinal)
            || normalized.Length is <= 5 or > OperationKeys.MaxLength
            || normalized.Any(char.IsWhiteSpace) || normalized.Any(char.IsControl))
        {
            throw new ArgumentException(
                "OperationKey must start with 'desk:' and be at most 100 characters.",
                nameof(operationKey));
        }

        return normalized;
    }

    private static string RequireFileName(string fileName)
    {
        var normalized = fileName?.Trim();
        if (string.IsNullOrWhiteSpace(normalized)
            || normalized.Length > MaximumFileNameLength
            || !Path.GetFileName(normalized).Equals(normalized, StringComparison.Ordinal)
            || normalized is "." or "..")
        {
            throw new ArgumentException("A leaf file name is required.", nameof(fileName));
        }

        return normalized;
    }

    private static string RequireMediaType(string mediaType)
    {
        var normalized = mediaType?.Trim();
        if (string.IsNullOrWhiteSpace(normalized)
            || normalized.Length > MaximumMediaTypeLength
            || normalized.Any(char.IsControl))
        {
            throw new ArgumentException("A valid media type is required.", nameof(mediaType));
        }

        return normalized;
    }

    private static DocumentSemanticRole ParseSemanticRole(string value) =>
        Enum.TryParse<DocumentSemanticRole>(value?.Trim(), true, out var role)
            && Enum.IsDefined(role)
                ? role
                : throw new ArgumentException("The document semantic role is not recognized.", nameof(value));

    private static bool IsSafeFileName(string value) =>
        !string.IsNullOrWhiteSpace(value)
        && value.Length <= MaximumFileNameLength
        && Path.GetFileName(value).Equals(value, StringComparison.Ordinal)
        && value is not "." and not ".."
        && value.All(character => !char.IsControl(character));

    private static bool IsSafeMediaType(string value) =>
        !string.IsNullOrWhiteSpace(value)
        && value.Length <= MaximumMediaTypeLength
        && value.All(character => !char.IsControl(character));

    private static bool IsSha256(string value) =>
        value.Length == 64 && value.All(Uri.IsHexDigit);

    private static async Task<byte[]?> ReadBoundedContentAsync(
        Stream body,
        long maximumLength,
        CancellationToken cancellationToken)
    {
        await using var content = new MemoryStream();
        var buffer = new byte[64 * 1024];
        var total = 0L;
        int read;
        while ((read = await body.ReadAsync(buffer, cancellationToken)) > 0)
        {
            total += read;
            if (total > maximumLength)
            {
                return null;
            }

            await content.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }

        return total == 0 ? null : content.ToArray();
    }
}

internal sealed class DesktopDocumentUploadSessions : IDisposable
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(30);
    internal const int MaximumActiveSessions = 32;
    internal const long MaximumBufferedBytes = 64L * 1024 * 1024;
    private readonly ConcurrentDictionary<Guid, Session> sessions = new();
    private readonly object sync = new();
    private readonly TimeProvider timeProvider;
    private long bufferedBytes;

    public DesktopDocumentUploadSessions(TimeProvider timeProvider) =>
        this.timeProvider = timeProvider;

    public bool TryCreate(
        Guid caseId,
        ActionActor actor,
        string fileName,
        string mediaType,
        DocumentSemanticRole semanticRole,
        out Session? session)
    {
        lock (sync)
        {
            CleanupExpiredUnsafe(timeProvider.GetUtcNow());
            if (sessions.Count >= MaximumActiveSessions)
            {
                session = null;
                return false;
            }

            session = new Session(
                this,
                Guid.NewGuid(),
                caseId,
                actor,
                fileName,
                mediaType,
                semanticRole,
                timeProvider.GetUtcNow().Add(Lifetime));
            sessions[session.Id] = session;
            return true;
        }
    }

    public Session? Find(Guid id, ActionActor actor)
    {
        lock (sync)
        {
            CleanupExpiredUnsafe(timeProvider.GetUtcNow());
            if (!sessions.TryGetValue(id, out var session)
                || !session.BelongsTo(actor))
            {
                sessions.TryRemove(id, out _);
                return null;
            }

            return session;
        }
    }

    private void CleanupExpiredUnsafe(DateTimeOffset now)
    {
        foreach (var pair in sessions)
        {
            if (pair.Value.ExpiresAtUtc <= now
                && sessions.TryRemove(pair.Key, out var expired))
            {
                bufferedBytes -= expired.ContentLength;
            }
        }
    }

    private bool TrySetContent(Session session, byte[] value)
    {
        lock (sync)
        {
            if (!sessions.ContainsKey(session.Id) || session.IsCompleted)
            {
                return false;
            }

            var nextBufferedBytes = bufferedBytes
                - session.ContentLength
                + value.LongLength;
            if (nextBufferedBytes > MaximumBufferedBytes)
            {
                return false;
            }

            bufferedBytes = nextBufferedBytes;
            session.SetContentUnsafe(value);
            return true;
        }
    }

    private bool TryGetContent(Session session, out byte[] value)
    {
        lock (sync)
        {
            value = session.Content ?? [];
            return sessions.ContainsKey(session.Id) && value.Length > 0;
        }
    }

    private bool TryGetCompleted(
        Session session,
        string operationKey,
        out DocumentUploadCompletionResponse value)
    {
        lock (sync)
        {
            if (session.Completed is not null)
            {
                if (!string.Equals(
                        session.CompletedOperationKey,
                        operationKey,
                        StringComparison.Ordinal))
                {
                    throw new CaseOperationConflictException(session.CaseId, operationKey);
                }

                value = session.Completed;
                return true;
            }

            value = null!;
            return false;
        }
    }

    private void SetCompleted(
        Session session,
        string operationKey,
        DocumentUploadCompletionResponse value)
    {
        lock (sync)
        {
            if (sessions.ContainsKey(session.Id))
            {
                bufferedBytes -= session.ContentLength;
            }

            session.SetCompletedUnsafe(operationKey, value);
        }
    }

    public void Dispose()
    {
        Session[] active;
        lock (sync)
        {
            active = sessions.Values.ToArray();
            sessions.Clear();
            bufferedBytes = 0;
        }

        foreach (var session in active)
        {
            session.Dispose();
        }
    }

    internal sealed class Session(
        DesktopDocumentUploadSessions owner,
        Guid id,
        Guid caseId,
        ActionActor actor,
        string fileName,
        string mediaType,
        DocumentSemanticRole semanticRole,
        DateTimeOffset expiresAtUtc) : IDisposable
    {
        private readonly SemaphoreSlim completionGate = new(1, 1);
        private byte[]? content;

        public Guid Id { get; } = id;
        public Guid CaseId { get; } = caseId;
        public string FileName { get; } = fileName;
        public string MediaType { get; } = mediaType;
        public DocumentSemanticRole SemanticRole { get; } = semanticRole;
        public DateTimeOffset ExpiresAtUtc { get; } = expiresAtUtc;
        public bool IsCompleted => Completed is not null;
        public long ContentLength => content?.LongLength ?? 0;
        internal byte[]? Content => content;
        internal string? CompletedOperationKey { get; private set; }
        internal DocumentUploadCompletionResponse? Completed { get; private set; }

        public bool BelongsTo(ActionActor candidate) =>
            actor.Kind == candidate.Kind
            && string.Equals(actor.SubjectId, candidate.SubjectId, StringComparison.Ordinal);

        public bool TrySetContent(byte[] value) => owner.TrySetContent(this, value);

        public bool TryGetContent(out byte[] value) => owner.TryGetContent(this, out value);

        public bool TryGetCompleted(
            string operationKey,
            out DocumentUploadCompletionResponse value) =>
            owner.TryGetCompleted(this, operationKey, out value);

        public void SetCompleted(
            string operationKey,
            DocumentUploadCompletionResponse value) =>
            owner.SetCompleted(this, operationKey, value);

        public Task WaitForCompletionAsync(CancellationToken cancellationToken) =>
            completionGate.WaitAsync(cancellationToken);

        public void ReleaseCompletion() => completionGate.Release();

        internal void SetContentUnsafe(byte[] value) => content = value;

        internal void SetCompletedUnsafe(
            string operationKey,
            DocumentUploadCompletionResponse value)
        {
            CompletedOperationKey = operationKey;
            Completed = value;
            content = null;
        }

        public void Dispose() => completionGate.Dispose();
    }
}

internal sealed class DocumentDownloadResult(
    DocumentDownload download,
    HttpRequest request) : IResult
{
    public async Task ExecuteAsync(HttpContext httpContext)
    {
        var etag = $"W/\"{download.Sha256.ToLowerInvariant()}\"";
        httpContext.Response.Headers.ETag = etag;
        httpContext.Response.Headers.CacheControl = "private, no-store";
        httpContext.Response.Headers.AcceptRanges = "bytes";
        httpContext.Response.Headers.XContentTypeOptions = "nosniff";
        httpContext.Response.ContentType = download.MediaType;
        httpContext.Response.Headers.ContentDisposition =
            new ContentDispositionHeaderValue("attachment")
            {
                FileNameStar = download.FileName
            }.ToString();

        try
        {
            if (EntityTagHeaderValue.TryParseList(
                    [request.Headers.IfNoneMatch.ToString()],
                    out var values)
                && values.Any(value =>
                    value == EntityTagHeaderValue.Any
                    || value.Compare(
                        EntityTagHeaderValue.Parse(etag),
                        useStrongComparison: false)))
            {
                httpContext.Response.StatusCode = StatusCodes.Status304NotModified;
                return;
            }

            var rangeHeader = request.Headers.Range.ToString();
            var range = ParseRange(rangeHeader, download.ContentLength);
            if (range is null && rangeHeader.Length > 0)
            {
                httpContext.Response.StatusCode = StatusCodes.Status416RangeNotSatisfiable;
                httpContext.Response.Headers.ContentRange = $"bytes */{download.ContentLength}";
                return;
            }

            var (start, length, partial) = range ?? (0L, download.ContentLength, false);
            httpContext.Response.StatusCode = partial
                ? StatusCodes.Status206PartialContent
                : StatusCodes.Status200OK;
            httpContext.Response.ContentLength = length;
            if (partial)
            {
                httpContext.Response.Headers.ContentRange =
                    $"bytes {start}-{start + length - 1}/{download.ContentLength}";
            }

            await CopyRangeAsync(
                download.Content,
                httpContext.Response.Body,
                start,
                length,
                httpContext.RequestAborted);
        }
        finally
        {
            await download.DisposeAsync();
        }
    }

    private static (long Start, long Length, bool Partial)? ParseRange(
        string? header,
        long totalLength)
    {
        if (string.IsNullOrWhiteSpace(header))
        {
            return null;
        }

        if (!header.StartsWith("bytes=", StringComparison.OrdinalIgnoreCase)
            || header[6..].Contains(',', StringComparison.Ordinal))
        {
            return null;
        }

        var value = header[6..].Trim();
        var separator = value.IndexOf('-');
        if (separator < 0 || totalLength <= 0)
        {
            return null;
        }

        var startText = value[..separator].Trim();
        var endText = value[(separator + 1)..].Trim();
        long start;
        long end;
        if (startText.Length == 0)
        {
            if (!long.TryParse(endText, out var suffixLength) || suffixLength <= 0)
            {
                return null;
            }

            start = Math.Max(0, totalLength - suffixLength);
            end = totalLength - 1;
        }
        else if (!long.TryParse(startText, out start) || start < 0 || start >= totalLength)
        {
            return null;
        }
        else
        {
            if (endText.Length == 0)
            {
                end = totalLength - 1;
            }
            else if (!long.TryParse(endText, out end) || end < start)
            {
                return null;
            }

            end = Math.Min(end, totalLength - 1);
        }

        return (start, checked(end - start + 1), true);
    }

    private static async Task CopyRangeAsync(
        Stream source,
        Stream destination,
        long start,
        long length,
        CancellationToken cancellationToken)
    {
        var buffer = new byte[64 * 1024];
        var skipped = 0L;
        while (skipped < start)
        {
            var read = await source.ReadAsync(
                buffer.AsMemory(0, (int)Math.Min(buffer.Length, start - skipped)),
                cancellationToken);
            if (read == 0)
            {
                throw new InvalidDataException("The document content ended before the requested range.");
            }

            skipped += read;
        }

        var copied = 0L;
        while (copied < length)
        {
            var read = await source.ReadAsync(
                buffer.AsMemory(0, (int)Math.Min(buffer.Length, length - copied)),
                cancellationToken);
            if (read == 0)
            {
                throw new InvalidDataException("The document content ended before its declared length.");
            }

            await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            copied += read;
        }
    }
}
