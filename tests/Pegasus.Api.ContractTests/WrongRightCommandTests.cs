using System.Net;
using Pegasus.Api.ContractTests.CommandCoverage;

namespace Pegasus.Api.ContractTests;

[Trait("Category", "Contract")]
public sealed class WrongRightCommandTests
{
    public static IEnumerable<object[]> Cases => CommandCoverageTable.AllRows;

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task EveryCommandRejectsTheWrongRightWithoutAnEffect(CommandCoverageRow row)
    {
        if (row.IsPlaceholder)
        {
            return;
        }

        using var context = new CommandCoverageTestContext();
        var before = await row.ReadEffectAsync(context);
        using var request = row.CreateWrongRightRequest(context);
        using var response = await context.Client.SendAsync(request);

        await CommandCoverageAssertions.AssertProblemAsync(
            response,
            HttpStatusCode.Forbidden,
            Pegasus.Contracts.ProblemDetails.PegasusProblemTypes.NotAuthorized,
            "Not authorized");
        var after = await row.ReadEffectAsync(context);
        Assert.Equal(before, after);
    }
}
