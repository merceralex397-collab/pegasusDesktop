using Microsoft.Extensions.Logging;
using Pegasus.Contracts;

namespace Pegasus.Desktop.Infrastructure.Api;

public interface IClientVersionProvider
{
    string GetVersion();
}

public sealed class PegasusRequestHandler(
    IClientVersionProvider clientVersionProvider,
    ILogger<PegasusRequestHandler> logger) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        request.Headers.Remove(PegasusHeaders.ClientVersion);
        request.Headers.TryAddWithoutValidation(
            PegasusHeaders.ClientVersion,
            clientVersionProvider.GetVersion());

        var correlationId = request.Headers.TryGetValues(PegasusHeaders.CorrelationId, out var values)
            ? values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))
            : null;

        if (string.IsNullOrWhiteSpace(correlationId))
        {
            correlationId = Guid.NewGuid().ToString("D");
            request.Headers.Remove(PegasusHeaders.CorrelationId);
            request.Headers.TryAddWithoutValidation(PegasusHeaders.CorrelationId, correlationId);
        }

        using var scope = logger.BeginScope(
            new Dictionary<string, object?>
            {
                [PegasusHeaders.CorrelationId] = correlationId
            });

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
