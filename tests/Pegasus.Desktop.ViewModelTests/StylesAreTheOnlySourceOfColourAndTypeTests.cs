using System.Text.RegularExpressions;

namespace Pegasus.Desktop.ViewModelTests;

public sealed class StylesAreTheOnlySourceOfColourAndTypeTests
{
    private static readonly Regex _hexColourLiteral = new(
        @"(?<![A-Za-z0-9])#(?:[0-9A-Fa-f]{3}|[0-9A-Fa-f]{4}|[0-9A-Fa-f]{6}|[0-9A-Fa-f]{8})(?![0-9A-Fa-f])",
        RegexOptions.Compiled);

    private static readonly Regex _rawFontSizeAttribute = new(
        @"\bFontSize\s*=",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex _numericCornerRadiusAttribute = new(
        @"\bCornerRadius\s*=\s*[""']\s*-?\d+(?:\.\d+)?\s*[""']",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    [Fact]
    [Trait("Category", "ThemeResources")]
    public void StylesAreTheOnlySourceOfColourAndType()
    {
        var repositoryRoot = FindRepositoryRoot();
        var desktopRoot = Path.Combine(repositoryRoot, "src", "Pegasus.Desktop");
        var violations = Directory
            .EnumerateFiles(desktopRoot, "*.xaml", SearchOption.AllDirectories)
            .Where(file => !IsStylesFile(repositoryRoot, file))
            .Where(file => !IsGeneratedFile(repositoryRoot, file))
            .SelectMany(FindViolations)
            .ToArray();

        Assert.Empty(violations);
    }

    private static IEnumerable<string> FindViolations(string file)
    {
        var relativePath = Path.GetRelativePath(FindRepositoryRoot(), file).Replace('\\', '/');
        var content = File.ReadAllText(file);

        foreach (Match match in _hexColourLiteral.Matches(content))
        {
            yield return $"{relativePath}: hex colour literal {match.Value}";
        }

        if (_rawFontSizeAttribute.IsMatch(content))
        {
            yield return $"{relativePath}: raw FontSize attribute";
        }

        if (_numericCornerRadiusAttribute.IsMatch(content))
        {
            yield return $"{relativePath}: numeric CornerRadius attribute";
        }
    }

    private static bool IsStylesFile(string repositoryRoot, string file)
    {
        var relativePath = Path.GetRelativePath(repositoryRoot, file).Replace('\\', '/');
        return relativePath.StartsWith("src/Pegasus.Desktop/Styles/", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsGeneratedFile(string repositoryRoot, string file)
    {
        var relativePath = Path.GetRelativePath(repositoryRoot, file).Replace('\\', '/');
        return relativePath.StartsWith("src/Pegasus.Desktop/bin/", StringComparison.OrdinalIgnoreCase)
            || relativePath.StartsWith("src/Pegasus.Desktop/obj/", StringComparison.OrdinalIgnoreCase);
    }

    private static string FindRepositoryRoot()
    {
        foreach (var startPath in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
        {
            var directory = new DirectoryInfo(startPath);
            while (directory is not null)
            {
                if (Directory.Exists(Path.Combine(directory.FullName, "src", "Pegasus.Desktop")))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }
        }

        throw new DirectoryNotFoundException("Could not locate the Pegasus repository root.");
    }
}
