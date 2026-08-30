using Microsoft.Extensions.Logging;
using Pegasus.Desktop.Services;

namespace Pegasus.Desktop.ViewModelTests;

public sealed class ActivationRouterTests
{
    [Fact]
    [Trait("Category", "Activation")]
    public void CaseDeepLinkRoutesToCaseWithIdentifier()
    {
        var navigation = new RecordingNavigationService();
        var logger = new RecordingLogger<ActivationRouter>();
        var router = new ActivationRouter(navigation, logger);

        router.Route("pegasus://case/CE-2026-01432");

        Assert.Equal("/Cases/CE-2026-01432", navigation.CurrentRoute);
    }

    [Fact]
    [Trait("Category", "Activation")]
    public void FileActivationRoutesToDocument()
    {
        var navigation = new RecordingNavigationService();
        var logger = new RecordingLogger<ActivationRouter>();
        var router = new ActivationRouter(navigation, logger);

        router.RouteFile("C:\\Cases\\CE-2026-01432\\estimate.pdf");

        Assert.Equal(
            "/Documents/C:\\Cases\\CE-2026-01432\\estimate.pdf",
            navigation.CurrentRoute);
    }

    [Fact]
    [Trait("Category", "Activation")]
    public void UnknownArgumentIsIgnoredAndLogged()
    {
        var navigation = new RecordingNavigationService();
        var logger = new RecordingLogger<ActivationRouter>();
        var router = new ActivationRouter(navigation, logger);

        router.Route("not-a-pegasus-activation");

        Assert.Empty(navigation.History);
        var entry = Assert.Single(logger.Entries, item => item.EventId.Id == 2002);
        Assert.Equal(LogLevel.Warning, entry.Level);
    }

    private sealed class RecordingNavigationService : INavigationService
    {
        public List<string> History { get; } = [];

        public string? CurrentRoute => History.Count == 0 ? null : History[^1];

        public void Navigate(string route) => History.Add(route);
    }

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public List<LogEntry> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Entries.Add(new LogEntry(logLevel, eventId, formatter(state, exception)));
        }

        public sealed record LogEntry(LogLevel Level, EventId EventId, string Message);

        private sealed class NullScope : IDisposable
        {
            public static NullScope Instance { get; } = new();

            public void Dispose()
            {
            }
        }
    }
}
