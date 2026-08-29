using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Pegasus.Desktop.Infrastructure.Api;

public static class PegasusHttpClientRegistration
{
    public static IServiceCollection AddPegasusApiClient(
        this IServiceCollection services,
        Action<GatewayOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new GatewayOptions();
        configure(options);
        if (options.BaseAddress is null)
        {
            throw new InvalidOperationException("The Pegasus gateway base address is required.");
        }

        services.TryAddSingleton<IClientVersionProvider, Windows.PackageClientVersionProvider>();
        services.AddTransient<PegasusRequestHandler>();
        services.AddTransient<GetRetryHandler>();
        services
            .AddHttpClient("pegasus", client => client.BaseAddress = options.BaseAddress)
            .AddHttpMessageHandler<PegasusRequestHandler>()
            .AddHttpMessageHandler<GetRetryHandler>();

        return services;
    }
}
