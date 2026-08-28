using System.Net;
using Pegasus.Api.ContractTests.CommandCoverage;

namespace Pegasus.Api.ContractTests;

[Trait("Category", "Contract")]
public sealed class StaleVersionCommandTests
{
    public static IEnumerable<object[]> Cases => CommandCoverageTable.VersionRows;

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task VersionedCommandsRejectStaleVersionsWithoutAnEffect(CommandCoverageRow row)
    {
        if (row.IsPlaceholder)
        {
            return;
        }

        Assert.NotNull(row.CreateStaleVersionRequest);
        using var context = new CommandCoverageTestContext();
        var before = await row.ReadEffectAsync(context);
        using var request = row.CreateStaleVersionRequest!(context);
        using var response = await context.Client.SendAsync(request);

        await CommandCoverageAssertions.AssertProblemAsync(
            response,
            HttpStatusCode.Conflict,
            Pegasus.Contracts.ProblemDetails.PegasusProblemTypes.VersionConflict,
            "Version conflict",
            row.ReadExpectedCurrentVersionAsync is null
                ? null
                : await row.ReadExpectedCurrentVersionAsync(context));
        var after = await row.ReadEffectAsync(context);
        Assert.Equal(before, after);
    }
}
