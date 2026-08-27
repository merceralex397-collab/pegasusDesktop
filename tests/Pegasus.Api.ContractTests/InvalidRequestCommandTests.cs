using System.Net;
using Pegasus.Api.ContractTests.CommandCoverage;

namespace Pegasus.Api.ContractTests;

[Trait("Category", "Contract")]
public sealed class InvalidRequestCommandTests
{
    public static IEnumerable<object[]> Cases => CommandCoverageTable.AllRows;

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task EveryCommandReturnsTheMappedProblemForInvalidInput(CommandCoverageRow row)
    {
        if (row.IsPlaceholder)
        {
            return;
        }

        using var context = new CommandCoverageTestContext();
        using var request = row.CreateInvalidRequest(context);
        using var response = await context.Client.SendAsync(request);

        await CommandCoverageAssertions.AssertProblemAsync(
            response,
            HttpStatusCode.BadRequest,
            row.InvalidProblemType,
            row.InvalidProblemTitle);
    }
}
