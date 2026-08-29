namespace Pegasus.Desktop.ViewModelTests.Support;

public sealed class FakeNavigationService
{
    private readonly List<string> _history = [];

    public IReadOnlyList<string> History => _history;

    public string? CurrentRoute => _history.Count == 0 ? null : _history[^1];

    public void Navigate(string route)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(route);
        _history.Add(route);
    }
}
