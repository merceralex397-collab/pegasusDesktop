using Microsoft.Extensions.DependencyInjection;
using Pegasus.Web.Api;

namespace Pegasus.Api.ContractTests;

[Trait("Category", "Contract")]
public sealed class ContractTestHostTests
{
    [Fact]
    public void EnabledGatewayOptionsAreRegisteredByTheRealHost()
    {
        using var factory = new ContractTestWebApplicationFactory();

        Assert.NotNull(factory.Services.GetRequiredService<DesktopGatewayOptions>());
    }
}
