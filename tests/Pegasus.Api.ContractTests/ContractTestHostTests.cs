using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Pegasus.Web.Api;

namespace Pegasus.Api.ContractTests;

[Trait("Category", "Contract")]
public sealed class ContractTestHostTests
{
    [Fact]
    public async Task EnabledGatewayIsComposedAtTheVersionedApiBoundary()
    {
        using var factory = new ContractTestWebApplicationFactory();

        Assert.NotNull(factory.Services.GetRequiredService<DesktopGatewayOptions>());

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost:7139")
        });
        using var response = await client.GetAsync("/api/v1/__contract-test-probe");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
