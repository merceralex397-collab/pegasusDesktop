using System.Security.Cryptography;
using System.Text.Json;
using Pegasus.Core.Documents;

namespace Pegasus.Infrastructure.Custody;

public sealed class LocalDocumentContentStore(string rootPath) : IDocumentContentStore
{
    private readonly string rootPath = Path.GetFullPath(rootPath);

    public async Task StoreAsync(
        Guid caseId,
        string caseReference,
        Guid versionId,
        ReadOnlyMemory<byte> content,
        string expectedSha256,
        CancellationToken cancellationToken)
    {
        ValidateIdentifiers(caseId, caseReference, versionId);
        var normalizedHash = NormalizeSha256(expectedSha256);
        var actualHash = Convert.ToHexString(SHA256.HashData(content.Span)).ToLowerInvariant();
        if (!string.Equals(normalizedHash, actualHash, StringComparison.Ordinal))
        {
            throw new InvalidDataException("Document content does not match its custody hash.");
        }

        var path = Resolve(caseReference, versionId);
        var directory = Path.GetDirectoryName(path)!;
        Directory.CreateDirectory(directory);
        if (File.Exists(path))
        {
            await VerifyAsync(path, normalizedHash, content.Length, cancellationToken);
            return;
        }

        var temporaryPath = Path.Combine(
            directory,
            $".{Path.GetFileName(path)}.{Guid.NewGuid():N}.tmp");
        try
        {
            await using (var stream = new FileStream(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                64 * 1024,
                FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await stream.WriteAsync(content, cancellationToken);
                await stream.FlushAsync(cancellationToken);
                RandomAccess.FlushToDisk(stream.SafeFileHandle);
            }

            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                File.Move(temporaryPath, path, overwrite: false);
            }
            catch (IOException) when (File.Exists(path))
            {
                await VerifyAsync(path, normalizedHash, content.Length, cancellationToken);
            }
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    public Task DeleteAsync(
        Guid caseId,
        string caseReference,
        Guid versionId,
        CancellationToken cancellationToken)
    {
        ValidateIdentifiers(caseId, caseReference, versionId);
        cancellationToken.ThrowIfCancellationRequested();
        var path = Resolve(caseReference, versionId);
        File.Delete(path);
        return Task.CompletedTask;
    }

    public async Task<Stream> OpenReadAsync(
        Guid caseId,
        string caseReference,
        Guid versionId,
        string expectedSha256,
        long expectedLength,
        CancellationToken cancellationToken)
    {
        ValidateIdentifiers(caseId, caseReference, versionId);
        var path = Resolve(caseReference, versionId);
        return await OpenReadPathAsync(
            path,
            NormalizeSha256(expectedSha256),
            expectedLength,
            cancellationToken);
    }

    public async Task<Stream> OpenReadVersionAsync(
        ManagedDocumentContentAddress address,
        string expectedSha256,
        long expectedLength,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(address);
        ValidateIdentifiers(address.CaseId, address.CaseReference, address.VersionId);
        var normalizedHash = NormalizeSha256(expectedSha256);
        var managedPath = Resolve(address.CaseReference, address.VersionId);
        if (File.Exists(managedPath))
        {
            return await OpenReadPathAsync(
                managedPath,
                normalizedHash,
                expectedLength,
                cancellationToken);
        }

        var occurrencePath = await ResolveOccurrenceAsync(address, normalizedHash, cancellationToken);
        if (occurrencePath is null)
        {
            throw new FileNotFoundException("The document content is unavailable.");
        }

        return await OpenReadPathAsync(
            occurrencePath,
            normalizedHash,
            expectedLength,
            cancellationToken);
    }

    private static async Task<Stream> OpenReadPathAsync(
        string path,
        string expectedSha256,
        long expectedLength,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("The document content is unavailable.");
        }

        await VerifyAsync(path, expectedSha256, expectedLength, cancellationToken);
        return new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            64 * 1024,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
    }

    private async Task<string?> ResolveOccurrenceAsync(
        ManagedDocumentContentAddress address,
        string expectedSha256,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var caseDirectory = ResolveCaseDirectory(address.CaseId);
        var candidates = new List<(string Path, bool IsImageLayout)>();
        var documentsDirectory = Path.Combine(caseDirectory, "documents");
        if (Directory.Exists(documentsDirectory))
        {
            foreach (var receiptDirectory in Directory.EnumerateDirectories(documentsDirectory))
            {
                if (address.OccurrenceOrdinal == 1)
                {
                    candidates.Add((
                        Path.Combine(receiptDirectory, expectedSha256),
                        IsImageLayout: false));
                }
                else if (address.OccurrenceOrdinal >= 2)
                {
                    candidates.Add((
                        Path.Combine(
                            receiptDirectory,
                            "attachments",
                            $"{address.OccurrenceOrdinal:D3}-{expectedSha256}"),
                        IsImageLayout: false));
                }
            }
        }

        if (address.SemanticRole == DocumentSemanticRole.Image)
        {
            var imagesDirectory = Path.Combine(caseDirectory, "images");
            if (Directory.Exists(imagesDirectory))
            {
                var prefix = $"{address.OccurrenceOrdinal:D3}-";
                candidates.AddRange(
                    Directory.EnumerateDirectories(imagesDirectory)
                        .Where(path => Path.GetFileName(path).StartsWith(
                            prefix,
                            StringComparison.OrdinalIgnoreCase))
                        .Select(path => (path, IsImageLayout: true)));
            }
        }

        var matches = new List<string>();
        foreach (var (directory, isImageLayout) in candidates)
        {
            var contentPath = Path.Combine(directory, "content");
            if (!File.Exists(contentPath))
            {
                continue;
            }

            if (await MatchesMetadataAsync(
                    directory,
                    address,
                    expectedSha256,
                    isImageLayout,
                    cancellationToken))
            {
                matches.Add(contentPath);
            }
        }

        return matches.Count switch
        {
            0 => null,
            1 => matches[0],
            _ => throw new InvalidDataException(
                "Local custody content resolution is ambiguous.")
        };
    }

    private static async Task<bool> MatchesMetadataAsync(
        string directory,
        ManagedDocumentContentAddress address,
        string expectedSha256,
        bool isImageLayout,
        CancellationToken cancellationToken)
    {
        var metadataPath = Path.Combine(directory, "metadata.json");
        if (!File.Exists(metadataPath))
        {
            throw new InvalidDataException("Local custody content metadata is unavailable.");
        }

        await using var stream = new FileStream(
            metadataPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            4096,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        using var metadata = await JsonDocument.ParseAsync(
            stream,
            cancellationToken: cancellationToken);
        var root = metadata.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidDataException("Local custody content metadata is incomplete.");
        }

        var sha256 = RequiredString(root, "Sha256");
        var fileName = RequiredString(root, "FileName");
        var mediaType = RequiredString(root, "MediaType");
        if (!string.Equals(sha256, expectedSha256, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(fileName, address.FileName, StringComparison.Ordinal)
            || !string.Equals(mediaType, address.MediaType, StringComparison.Ordinal))
        {
            return false;
        }

        if (isImageLayout)
        {
            if (!root.TryGetProperty("Ordinal", out var ordinal)
                || ordinal.ValueKind != JsonValueKind.Number
                || !ordinal.TryGetInt32(out var metadataOrdinal))
            {
                throw new InvalidDataException("Local image custody metadata is incomplete.");
            }

            return metadataOrdinal == address.OccurrenceOrdinal;
        }

        return true;
    }

    private string ResolveCaseDirectory(Guid caseId)
    {
        var path = Path.GetFullPath(Path.Combine(rootPath, "cases", caseId.ToString("N")));
        var rootPrefix = rootPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        if (!path.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("The document content is outside the configured custody root.");
        }

        return path;
    }

    private static string RequiredString(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var property)
            || property.ValueKind != JsonValueKind.String
            || string.IsNullOrWhiteSpace(property.GetString()))
        {
            throw new InvalidDataException("Local custody content metadata is incomplete.");
        }

        return property.GetString()!;
    }

    private static async Task VerifyAsync(
        string path,
        string expectedSha256,
        long expectedLength,
        CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            64 * 1024,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        if (stream.Length != expectedLength)
        {
            throw new InvalidDataException("Document custody length verification failed.");
        }

        var actualHash = Convert.ToHexString(await SHA256.HashDataAsync(stream, cancellationToken))
            .ToLowerInvariant();
        if (!string.Equals(expectedSha256, actualHash, StringComparison.Ordinal))
        {
            throw new InvalidDataException("Document custody hash verification failed.");
        }
    }

    private string Resolve(string caseReference, Guid versionId)
    {
        var path = Path.GetFullPath(Path.Combine(
            rootPath,
            "cases",
            SafeCaseFolderName(caseReference),
            "managed",
            versionId.ToString("N"),
            "content"));
        var rootPrefix = rootPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        if (!path.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("The document content is outside the configured custody root.");
        }

        return path;
    }

    private static void ValidateIdentifiers(Guid caseId, string caseReference, Guid versionId)
    {
        if (caseId == Guid.Empty
            || versionId == Guid.Empty
            || string.IsNullOrWhiteSpace(caseReference)
            || caseReference.Any(char.IsControl))
        {
            throw new ArgumentException("Case, Case/PO, and document version identifiers are required.");
        }
    }

    private static string SafeCaseFolderName(string value)
    {
        var result = CustodyNames.SafeName(value);

        return result;
    }

    private static string NormalizeSha256(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        if (value.Length != SHA256.HashSizeInBytes * 2 || !value.All(Uri.IsHexDigit))
        {
            throw new ArgumentException("A SHA-256 hash is required.", nameof(value));
        }

        return value.ToLowerInvariant();
    }
}
