namespace Pegasus.Desktop.ViewModelTests.Support;

/// <summary>
/// In-memory credential-store test support. It has the same narrow operations
/// as the desktop credential port without duplicating the production port.
/// </summary>
public sealed class FakeCredentialStore
{
    private readonly Dictionary<string, string> _values = new(StringComparer.Ordinal);

    public void Save(string key, string value) => _values[key] = value;

    public bool TryRead(string key, out string? value) => _values.TryGetValue(key, out value);

    public bool Clear(string key) => _values.Remove(key);
}
