using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Pegasus.Api.ContractTests.CommandCoverage;

namespace Pegasus.Api.ContractTests;

[Trait("Category", "Contract")]
public sealed class CommandCoverageGuardTests
{
    [Fact]
    public void CurrentHostHasNoUncoveredCommandEndpoints()
    {
        using var factory = new ContractTestWebApplicationFactory();
        var mismatches = CommandCoverageGuard.FindMismatches(
            CommandEndpointCatalogue.Read(factory.Services),
            CommandCoverageTable.Rows);

        Assert.Empty(mismatches);
    }

    [Fact]
    public void ACommandWithoutARowNamesItsRouteAndMethod()
    {
        var endpointBuilder = new RouteEndpointBuilder(
            _ => Task.CompletedTask,
            RoutePatternFactory.Parse("/api/v1/__probe"),
            order: 0);
        endpointBuilder.Metadata.Add(new HttpMethodMetadata(["POST"]));
        var endpoint = endpointBuilder.Build();
        var mismatches = CommandCoverageGuard.FindMismatches(
            CommandEndpointCatalogue.Read([endpoint]),
            CommandCoverageTable.Rows);

        Assert.Contains(
            mismatches,
            mismatch => mismatch.Contains("POST /api/v1/__probe", StringComparison.Ordinal));
    }
}
