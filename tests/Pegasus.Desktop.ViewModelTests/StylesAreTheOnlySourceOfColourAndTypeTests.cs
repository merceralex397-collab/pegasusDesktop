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
        @"\bCornerRadius\s*=\s*[""']\s*-?\d+(?:\.\d+)?(?:\s*,\s*-?\d+(?:\.\d+)?){0,3}\s*[""']",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex _colourAttribute = new(
        @"\b(?:Color|Background|Foreground|BorderBrush|Fill|Stroke)\s*=\s*[""']\s*([^""']+?)\s*[""']",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex _themeColourReference = new(
        @"^\{ThemeResource\s+[^}]+\}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex _colourSetter = new(
        @"<Setter\b(?=[^>]*\bProperty\s*=\s*[""'](?:Color|Background|Foreground|BorderBrush|Fill|Stroke)[""'])(?=[^>]*\bValue\s*=\s*[""'](?<value>[^""']+)[""'])[^>]*/?>",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex _colourPropertyElement = new(
        @"<[\w.:-]+\.(?:Color|Background|Foreground|BorderBrush|Fill|Stroke)\s*>\s*(?<value>[^<]+?)\s*</[\w.:-]+\.(?:Color|Background|Foreground|BorderBrush|Fill|Stroke)\s*>",
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

    [Theory]
    [InlineData("<TextBlock Foreground=\"Blue\" />", "named colour literal Blue")]
    [InlineData("<Border CornerRadius=\"1,1,1,1\" />", "numeric CornerRadius attribute")]
    [InlineData("<TextBlock Foreground=\"{StaticResource BlueBrush}\" />", "non-theme colour resource")]
    [InlineData("<Setter Property=\"Foreground\" Value=\"Blue\" />", "named colour literal Blue")]
    [InlineData("<TextBlock><TextBlock.Foreground>Blue</TextBlock.Foreground></TextBlock>", "named colour literal Blue")]
    public void GuardRejectsAdditionalAuthoredLiterals(string xaml, string expectedViolation)
    {
        var violations = FindViolations("probe.xaml", xaml).ToArray();

        Assert.Contains(violations, violation => violation.Contains(expectedViolation, StringComparison.Ordinal));
    }

    private static IEnumerable<string> FindViolations(string file)
    {
        var relativePath = Path.GetRelativePath(FindRepositoryRoot(), file).Replace('\\', '/');
        return FindViolations(relativePath, File.ReadAllText(file));
    }

    private static IEnumerable<string> FindViolations(string relativePath, string content)
    {

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

        foreach (Match match in _colourAttribute.Matches(content))
        {
            var violation = FormatColourViolation(relativePath, match.Groups[1].Value);
            if (violation is not null)
            {
                yield return violation;
            }
        }

        foreach (Match match in _colourSetter.Matches(content))
        {
            var violation = FormatColourViolation(relativePath, match.Groups["value"].Value);
            if (violation is not null)
            {
                yield return violation;
            }
        }

        foreach (Match match in _colourPropertyElement.Matches(content))
        {
            var violation = FormatColourViolation(relativePath, match.Groups["value"].Value);
            if (violation is not null)
            {
                yield return violation;
            }
        }
    }

    private static string? FormatColourViolation(string relativePath, string rawValue)
    {
        var value = rawValue.Trim();
        return value.StartsWith('{') && _themeColourReference.IsMatch(value)
            ? null
            : value.StartsWith('{')
                ? $"{relativePath}: non-theme colour resource {value}"
                : $"{relativePath}: named colour literal {value}";
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
