using System.Text.Json;
using Microsoft.Extensions.Logging;
using Pegasus.Contracts;
using Pegasus.Desktop.Infrastructure.Diagnostics;

namespace Pegasus.Desktop.Logging;

public sealed class DiagnosticsLoggerProvider : ILoggerProvider, ISupportExternalScope
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly IDiagnosticsWriter _writer;
    private readonly string _sessionId;
    private IExternalScopeProvider _scopeProvider = new LoggerExternalScopeProvider();

    public DiagnosticsLoggerProvider(IDiagnosticsWriter writer, string sessionId)
    {
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        _sessionId = string.IsNullOrWhiteSpace(sessionId)
            ? throw new ArgumentException("A logging session identifier is required.", nameof(sessionId))
            : sessionId;
    }

    public ILogger CreateLogger(string categoryName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(categoryName);
        return new DiagnosticsLogger(this, categoryName);
    }

    public void SetScopeProvider(IExternalScopeProvider scopeProvider)
    {
        _scopeProvider = scopeProvider ?? throw new ArgumentNullException(nameof(scopeProvider));
    }

    public void Dispose()
    {
    }

    private void Write(
        string categoryName,
        LogLevel logLevel,
        EventId eventId,
        object? state,
        Exception? exception,
        string message)
    {
        var scopes = new List<object?>();
        _scopeProvider.ForEachScope(static (scope, collected) => collected.Add(scope), scopes);

        var properties = new Dictionary<string, object?>(StringComparer.Ordinal);
        AddProperties(properties, state);
        foreach (var scope in scopes)
        {
            AddProperties(properties, scope);
        }

        properties.TryGetValue(PegasusHeaders.CorrelationId, out var correlationValue);
        var entry = new
        {
            Timestamp = DateTimeOffset.UtcNow,
            Level = logLevel.ToString(),
            Category = categoryName,
            EventId = eventId.Id,
            EventName = eventId.Name,
            SessionId = _sessionId,
            CorrelationId = correlationValue?.ToString(),
            Message = message,
            Exception = exception?.ToString(),
            Properties = properties
        };

        _writer.Write(JsonSerializer.Serialize(entry, SerializerOptions));
    }

    private static void AddProperties(Dictionary<string, object?> properties, object? state)
    {
        if (state is not IEnumerable<KeyValuePair<string, object?>> values)
        {
            return;
        }

        foreach (var pair in values)
        {
            properties[pair.Key] = pair.Value;
        }
    }

    private sealed class DiagnosticsLogger(
        DiagnosticsLoggerProvider provider,
        string categoryName) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull =>
            provider._scopeProvider.Push(state);

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            ArgumentNullException.ThrowIfNull(formatter);
            provider.Write(categoryName, logLevel, eventId, state, exception, formatter(state, exception));
        }
    }
}
