using System.Text.Json;
using System.Text.Json.Serialization;
using Pegasus.Contracts;
using Pegasus.Contracts.Paging;
using Pegasus.Contracts.ProblemDetails;
using Pegasus.Contracts.Responses;

namespace Pegasus.ArchitectureTests;

public sealed class ContractSerializationTests
{
    [Fact]
    public void PagedResultRoundTripsWithExactlyFiveCamelCaseMembers()
    {
        var value = new PagedResult<string>(["one"], 2, 25, true, true);

        var json = JsonSerializer.Serialize(value, PegasusJson.Options);
        using var document = JsonDocument.Parse(json);
        var properties = document.RootElement.EnumerateObject().Select(property => property.Name).ToArray();

        Assert.Equal(["items", "page", "pageSize", "hasPreviousPage", "hasNextPage"], properties);
        Assert.DoesNotContain("total", json, StringComparison.OrdinalIgnoreCase);
        var roundTrip = JsonSerializer.Deserialize<PagedResult<string>>(json, PegasusJson.Options);

        Assert.NotNull(roundTrip);
        Assert.Equal(value.Items, roundTrip.Items);
        Assert.Equal(value.Page, roundTrip.Page);
        Assert.Equal(value.PageSize, roundTrip.PageSize);
        Assert.Equal(value.HasPreviousPage, roundTrip.HasPreviousPage);
        Assert.Equal(value.HasNextPage, roundTrip.HasNextPage);
    }

    [Fact]
    public void PegasusProblemRoundTripsWithoutAnEmptyExtensionsObject()
    {
        var value = new PegasusProblem(
            PegasusProblemTypes.Validation,
            "Validation failed",
            400,
            "The request is invalid.",
            null,
            "corr-123");

        var json = JsonSerializer.Serialize(value, PegasusJson.Options);
        var roundTrip = JsonSerializer.Deserialize<PegasusProblem>(json, PegasusJson.Options);

        Assert.DoesNotContain("extensions", json, StringComparison.Ordinal);
        Assert.NotNull(roundTrip);
        Assert.Equal(value.Type, roundTrip.Type);
        Assert.Equal(value.Title, roundTrip.Title);
        Assert.Equal(value.Status, roundTrip.Status);
        Assert.Equal(value.Detail, roundTrip.Detail);
        Assert.Equal(value.Instance, roundTrip.Instance);
        Assert.Equal(value.CorrelationId, roundTrip.CorrelationId);
        Assert.Empty(roundTrip.Extensions);
    }

    [Fact]
    public void PegasusProblemRoundTripsRFC9457TopLevelExtensions()
    {
        var value = new PegasusProblem(
            PegasusProblemTypes.VersionConflict,
            "Version conflict",
            409,
            null,
            null,
            "corr-123",
            new Dictionary<string, object?>
            {
                ["currentVersion"] = "7",
                ["minimumVersion"] = "6"
            });

        var json = JsonSerializer.Serialize(value, PegasusJson.Options);
        var roundTrip = JsonSerializer.Deserialize<PegasusProblem>(json, PegasusJson.Options);

        Assert.DoesNotContain("\"extensions\"", json, StringComparison.Ordinal);
        Assert.Contains("\"currentVersion\":\"7\"", json, StringComparison.Ordinal);
        Assert.Contains("\"minimumVersion\":\"6\"", json, StringComparison.Ordinal);
        Assert.Equal("7", roundTrip?.CurrentVersion);
        Assert.Equal("6", roundTrip?.MinimumVersion);
    }

    [Fact]
    public void PegasusProblemReadsRFC9457TopLevelVersionExtensions()
    {
        const string json = "{\"type\":\"urn:pegasus:problem:version-conflict\",\"title\":\"Version conflict\",\"status\":409,\"correlationId\":\"corr-123\",\"currentVersion\":\"7\",\"minimumVersion\":\"6\"}";

        var value = JsonSerializer.Deserialize<PegasusProblem>(json, PegasusJson.Options);

        Assert.NotNull(value);
        Assert.Equal("7", value.CurrentVersion);
        Assert.Equal("6", value.MinimumVersion);
    }

    [Fact]
    public void CompatibilityResponseOmitsNullMaintenanceMessageAndReadsItWhenAbsent()
    {
        var value = new ClientCompatibilityResponse("1.0.0", "1.2.0", "stable", null, 60);

        var json = JsonSerializer.Serialize(value, PegasusJson.Options);
        var roundTrip = JsonSerializer.Deserialize<ClientCompatibilityResponse>(json, PegasusJson.Options);

        Assert.DoesNotContain("maintenanceMessage", json, StringComparison.Ordinal);
        Assert.Equal(value, roundTrip);
    }

    [Fact]
    public void UnknownStringEnumValueFailsClosed()
    {
        const string json = "\"future\"";

        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<TestStatus>(json, PegasusJson.Options));
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    private enum TestStatus
    {
        Ready
    }
}
