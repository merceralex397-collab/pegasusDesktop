using Pegasus.Api.ContractTests.CommandCoverage;

namespace Pegasus.Api.ContractTests;

[Trait("Category", "Contract")]
public sealed class IdempotentReplayCommandTests
{
    public static IEnumerable<object[]> Cases => CommandCoverageTable.OperationRows;

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task OperationKeyReplayHasOneEffectAndTheSameResponse(CommandCoverageRow row)
    {
        if (row.IsPlaceholder)
        {
            return;
        }

        Assert.NotNull(row.CreateReplayRequests);
        using var context = new CommandCoverageTestContext();
        var before = await row.ReadEffectAsync(context);
        var (firstRequest, replayRequest) = row.CreateReplayRequests!(context);
        using (firstRequest)
        using (replayRequest)
        {
            using var firstResponse = await context.Client.SendAsync(firstRequest);
            using var replayResponse = await context.Client.SendAsync(replayRequest);

            await CommandCoverageAssertions.AssertResponseBodiesEqualAsync(
                firstResponse,
                replayResponse);
        }
        var after = await row.ReadEffectAsync(context);
        Assert.Equal(before.ActionHistoryEntries + 1, after.ActionHistoryEntries);
    }
}
