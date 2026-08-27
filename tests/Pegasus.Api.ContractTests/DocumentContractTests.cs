using System.Text.Json;
using Pegasus.Contracts;
using Pegasus.Contracts.Responses;

namespace Pegasus.Api.ContractTests;

public sealed class DocumentContractTests
{
    [Fact]
    public void MetadataContractContainsOnlyBrokerSafeFields()
    {
        var response = new DocumentMetadataResponse(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "photo.jpg",
            "image/jpeg",
            12,
            "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef",
            "Image",
            "StaffUpload",
            "Confirmed",
            DateTimeOffset.UnixEpoch,
            "Staff:operator",
            true,
            false,
            null,
            "staff-upload:one",
            DateTimeOffset.UnixEpoch,
            null,
            null,
            1);

        var json = JsonSerializer.Serialize(response, PegasusJson.Options);

        Assert.Contains("fileName", json, StringComparison.Ordinal);
        Assert.Contains("mediaType", json, StringComparison.Ordinal);
        Assert.DoesNotContain("box", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("credential", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("remoteId", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("objectId", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("url", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void UploadSessionContractExposesOnlyOpaqueSessionMetadata()
    {
        var response = new DocumentUploadSessionResponse(
            Guid.NewGuid(),
            DateTimeOffset.UnixEpoch.AddMinutes(30),
            10 * 1024 * 1024);

        var json = JsonSerializer.Serialize(response, PegasusJson.Options);

        Assert.Contains("sessionId", json, StringComparison.Ordinal);
        Assert.Contains("maximumContentLength", json, StringComparison.Ordinal);
        Assert.DoesNotContain("box", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("credential", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("remoteId", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("objectId", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("url", json, StringComparison.OrdinalIgnoreCase);
    }
}
