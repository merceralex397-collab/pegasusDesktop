using Microsoft.Extensions.Logging;
using Pegasus.Desktop.Infrastructure.Api;

namespace Pegasus.Desktop.ViewModelTests.Support;

internal sealed class FixedClientVersionProvider(string version) : IClientVersionProvider
{
    public string GetVersion() => version;
}

internal sealed class RecordingHttpMessageHandler(
    Func<HttpRequestMessage, int, HttpResponseMessage> responseFactory) : HttpMessageHandler
{
    public List<HttpRequestMessage> Requests { get; } = [];

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Requests.Add(request);
        return Task.FromResult(responseFactory(request, Requests.Count));
    }
}

internal sealed class RecordingLogger<T> : ILogger<T>
{
    public List<object?> Scopes { get; } = [];

    public IDisposable BeginScope<TState>(TState state)
        where TState : notnull
    {
        Scopes.Add(state);
        return NoopDisposable.Instance;
    }

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
    }

    private sealed class NoopDisposable : IDisposable
    {
        public static NoopDisposable Instance { get; } = new();

        public void Dispose()
        {
        }
    }
}
