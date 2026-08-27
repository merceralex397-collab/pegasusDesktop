using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Pegasus.Api.ContractTests;

public sealed class ContractTestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("Runtime:Profile", "DevelopmentOffline");
        builder.UseSetting("Features:DesktopGateway", "true");
    }
}
