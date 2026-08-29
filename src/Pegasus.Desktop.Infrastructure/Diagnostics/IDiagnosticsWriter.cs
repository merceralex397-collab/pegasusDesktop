namespace Pegasus.Desktop.Infrastructure.Diagnostics;

public interface IDiagnosticsWriter
{
    void Write(string line);

    IReadOnlyList<string> GetFiles();
}
