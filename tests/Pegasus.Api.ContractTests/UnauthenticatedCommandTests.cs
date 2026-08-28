using System.Net;
using Pegasus.Api.ContractTests.CommandCoverage;

namespace Pegasus.Api.ContractTests;

[Trait("Category", "Contract")]
public sealed class UnauthenticatedCommandTests
{
    public static IEnumerable<object[]> Cases => CommandCoverageTable.AllRows;

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task EveryCommandRejectsAnUnauthenticatedRequest(CommandCoverageRow row)
    {
        if (row.IsPlaceholder)
        {
            return;
        }

        using var context = new CommandCoverageTestContext();
        using var request = row.CreateUnauthenticatedRequest(context);
        using var response = await context.Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        CommandCoverageAssertions.AssertBearerChallengeOnly(response);
    }
}
