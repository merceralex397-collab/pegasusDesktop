using System.Net;

namespace Pegasus.Desktop.Infrastructure.Api;

internal sealed class GetRetryHandler : DelegatingHandler
{
    private const int MaxAttempts = 3;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Commands are deliberately never retried automatically; only idempotent GETs are eligible.
        if (request.Method != HttpMethod.Get)
        {
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }

        for (var attempt = 1; ; attempt++)
        {
            HttpResponseMessage response;
            try
            {
                response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            }
            catch (HttpRequestException) when (attempt < MaxAttempts)
            {
                await DelayBeforeRetryAsync(attempt, cancellationToken).ConfigureAwait(false);
                continue;
            }

            if (attempt >= MaxAttempts || !IsTransient(response.StatusCode))
            {
                return response;
            }

            response.Dispose();
            await DelayBeforeRetryAsync(attempt, cancellationToken).ConfigureAwait(false);
        }
    }

    private static bool IsTransient(HttpStatusCode statusCode) =>
        statusCode is HttpStatusCode.RequestTimeout
            or HttpStatusCode.TooManyRequests
            or >= HttpStatusCode.InternalServerError;

    private static Task DelayBeforeRetryAsync(int attempt, CancellationToken cancellationToken)
    {
        var milliseconds = (attempt * 50) + Random.Shared.Next(0, 51);
        return Task.Delay(TimeSpan.FromMilliseconds(milliseconds), cancellationToken);
    }
}
