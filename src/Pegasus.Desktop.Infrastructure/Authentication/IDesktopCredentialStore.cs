namespace Pegasus.Desktop.Infrastructure.Authentication;

public interface IDesktopCredentialStore
{
    void Save(string key, string value);

    bool TryRead(string key, out string? value);

    void Clear(string key);
}
