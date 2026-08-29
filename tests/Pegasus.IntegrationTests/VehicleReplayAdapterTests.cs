using Pegasus.Core.Vehicle;
using Pegasus.Infrastructure.Vehicle;

namespace Pegasus.IntegrationTests;

/// <summary>
/// The offline worker adapter is the provider boundary used by development
/// and integration validation. These tests never contact DVLA or DVSA.
/// </summary>
public sealed class VehicleReplayAdapterTests
{
    private static readonly DateTimeOffset RetrievedAtUtc =
        new(2031, 5, 6, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ReplayedProviderFailureStaysFailedRatherThanNotFound()
    {
        const string registration = "AB12CDE";
        const string fixture = """
            {
              "schemaVersion": 1,
              "registration": "AB12CDE",
              "outcome": "failed",
              "provider": "dvla-dvsa-replay",
              "providerVersion": "replay-v1",
              "responseIdentity": "response-failed",
              "sourceObservedAtUtc": "2031-05-06T10:00:00+00:00",
              "vehicle": null,
              "motTests": [],
              "failure": {
                "code": "provider-timeout",
                "retryable": true,
                "retryAfterSeconds": 30
              }
            }
            """;

        var result = await ReadFixtureAsync(registration, fixture);

        Assert.Equal(VehicleLookupOutcome.Failed, result.Outcome);
        Assert.NotEqual(VehicleLookupOutcome.NotFound, result.Outcome);
        Assert.Equal("provider-timeout", result.Failure?.Code);
        Assert.True(result.Failure?.Retryable);
        Assert.Equal(TimeSpan.FromSeconds(30), result.Failure?.RetryAfter);
        Assert.Equal("dvla-dvsa-replay", result.Provider);
        Assert.Equal(TimeSpan.FromHours(2), result.SourceAge);
    }

    [Fact]
    public async Task ReplayedEmptyResultIsNotFound()
    {
        const string registration = "XY34ZAB";
        const string fixture = """
            {
              "schemaVersion": 1,
              "registration": "XY34ZAB",
              "outcome": "notFound",
              "provider": "dvla-dvsa-replay",
              "providerVersion": "replay-v1",
              "responseIdentity": "response-not-found",
              "sourceObservedAtUtc": null,
              "vehicle": null,
              "motTests": [],
              "failure": null
            }
            """;

        var result = await ReadFixtureAsync(registration, fixture);

        Assert.Equal(VehicleLookupOutcome.NotFound, result.Outcome);
        Assert.Null(result.Vehicle);
        Assert.Empty(result.MotTests);
        Assert.Null(result.Failure);
    }

    private static async Task<VehicleLookupResult> ReadFixtureAsync(
        string registration,
        string fixture)
    {
        var root = Path.Combine(Path.GetTempPath(), $"pegasus-vehicle-replay-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            await File.WriteAllTextAsync(
                Path.Combine(root, $"{registration}.vehicle-lookup.json"),
                fixture);
            var adapter = new DvlaDvsaReplayAdapter(root, new FixedTimeProvider(RetrievedAtUtc));
            return await adapter.LookupAsync(
                new VehicleLookupRequest(registration),
                "replay-test-correlation",
                CancellationToken.None);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
