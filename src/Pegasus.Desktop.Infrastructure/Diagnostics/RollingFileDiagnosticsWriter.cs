using System.Text;
using System.Text.RegularExpressions;

namespace Pegasus.Desktop.Infrastructure.Diagnostics;

public sealed class RollingFileDiagnosticsWriter : IDiagnosticsWriter
{
    private const string CurrentFileName = "diagnostics.log";
    private static readonly Regex BearerToken = new(
        @"\bBearer\s+[A-Za-z0-9._~+/=-]+",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex SecretField = new(
        """(?<prefix>"?(?:access[_-]?token|refresh[_-]?token|password|passwd|secret)"?\s*[:=]\s*)(?<value>"[^"]*"|'[^']*'|[^,;\s]+)""",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private readonly object _gate = new();
    private readonly string _rootDirectory;
    private readonly long _maxTotalBytes;
    private readonly int _retentionCount;
    private readonly Func<string, string> _additionalRedactor;

    public RollingFileDiagnosticsWriter(
        string rootDirectory,
        long maxTotalBytes,
        int retentionCount,
        Func<string, string>? additionalRedactor = null)
    {
        if (string.IsNullOrWhiteSpace(rootDirectory))
        {
            throw new ArgumentException("A diagnostics directory is required.", nameof(rootDirectory));
        }

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxTotalBytes);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(retentionCount);

        _rootDirectory = Path.GetFullPath(rootDirectory);
        _maxTotalBytes = maxTotalBytes;
        _retentionCount = retentionCount;
        _additionalRedactor = additionalRedactor ?? (line => line);
    }

    public void Write(string line)
    {
        ArgumentNullException.ThrowIfNull(line);
        var redacted = _additionalRedactor(Redact(line));
        var content = Encoding.UTF8.GetBytes(redacted + Environment.NewLine);
        if (content.LongLength > _maxTotalBytes)
        {
            return;
        }

        lock (_gate)
        {
            Directory.CreateDirectory(_rootDirectory);
            var currentPath = Path.Combine(_rootDirectory, CurrentFileName);
            if (File.Exists(currentPath) && new FileInfo(currentPath).Length + content.LongLength > _maxTotalBytes)
            {
                File.Move(currentPath, Path.Combine(
                    _rootDirectory,
                    $"diagnostics-{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}.log"));
            }

            TrimFiles(content.LongLength);
            File.AppendAllText(currentPath, redacted + Environment.NewLine, Encoding.UTF8);
            TrimFiles();
        }
    }

    public IReadOnlyList<string> GetFiles()
    {
        lock (_gate)
        {
            if (!Directory.Exists(_rootDirectory))
            {
                return [];
            }

            return GetLogFiles()
                .Select(file => file.FullName)
                .ToArray();
        }
    }

    public static string Redact(string line)
    {
        ArgumentNullException.ThrowIfNull(line);
        var redactedBearer = BearerToken.Replace(line, "Bearer [REDACTED]");
        return SecretField.Replace(redactedBearer, "${prefix}[REDACTED]");
    }

    private List<FileInfo> GetLogFiles() =>
        Directory
            .EnumerateFiles(_rootDirectory, "diagnostics*.log")
            .Select(path => new FileInfo(path))
            .OrderBy(file => file.LastWriteTimeUtc)
            .ThenBy(file => file.Name, StringComparer.Ordinal)
            .ToList();

    private void TrimFiles(long upcomingBytes = 0)
    {
        var files = GetLogFiles();
        while (files.Count > _retentionCount)
        {
            var oldest = files.FirstOrDefault(file => !file.Name.Equals(CurrentFileName, StringComparison.Ordinal));
            if (oldest is null)
            {
                break;
            }

            oldest.Delete();
            files.Remove(oldest);
        }

        while (files.Sum(file => file.Length) + upcomingBytes > _maxTotalBytes)
        {
            var oldest = files.FirstOrDefault(file => !file.Name.Equals(CurrentFileName, StringComparison.Ordinal));
            if (oldest is null)
            {
                break;
            }

            oldest.Delete();
            files.Remove(oldest);
        }
    }
}
