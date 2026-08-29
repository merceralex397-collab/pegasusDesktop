using System.Reflection;
using System.Text.RegularExpressions;
using Pegasus.Contracts.ProblemDetails;
using Pegasus.Desktop.Presentation;

namespace Pegasus.Desktop.ViewModelTests;

public sealed class ProblemPresentationTests
{
    private static readonly string[] BannedWords =
    [
        "intake",
        "bounded",
        "projection",
        "lease",
        "opaque",
        "ingress",
        "composed",
        "artifact",
        "durable",
        "aggregate",
        "caller",
        "correlation identifier",
        "bytes"
    ];

    private static readonly Dictionary<string, ProblemSeverity> ExpectedSeverities =
        new Dictionary<string, ProblemSeverity>(StringComparer.Ordinal)
        {
            [PegasusProblemTypes.Validation] = ProblemSeverity.Warning,
            [PegasusProblemTypes.NotAuthorized] = ProblemSeverity.Error,
            [PegasusProblemTypes.VersionConflict] = ProblemSeverity.Warning,
            [PegasusProblemTypes.LeaseConflict] = ProblemSeverity.Warning,
            [PegasusProblemTypes.LeaseExpired] = ProblemSeverity.Warning,
            [PegasusProblemTypes.OperationConflict] = ProblemSeverity.Warning,
            [PegasusProblemTypes.ClientUnsupported] = ProblemSeverity.Error,
            [PegasusProblemTypes.PasswordChangeRequired] = ProblemSeverity.Warning,
            [PegasusProblemTypes.AccountDisabled] = ProblemSeverity.Error,
            [PegasusProblemTypes.ProviderUnavailable] = ProblemSeverity.Error,
            [PegasusProblemTypes.NotFound] = ProblemSeverity.Informational,
            [PegasusProblemTypes.RateLimited] = ProblemSeverity.Warning,
            [PegasusProblemTypes.Maintenance] = ProblemSeverity.Error
        };

    private static readonly Regex XamlOperatorAttribute = new(
        "(?:Text|Header|Content|AutomationProperties\\.Name|ToolTip)\\s*=\\s*\\\"([^\\\"]+)\\\"",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static IEnumerable<object[]> GatewayProblemTypes =>
        typeof(PegasusProblemTypes)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(member => member.FieldType == typeof(string))
            .Where(member => member.Name != nameof(PegasusProblemTypes.Prefix))
            .Select(member => new object[] { member.GetValue(null)! });

    [Theory]
    [MemberData(nameof(GatewayProblemTypes))]
    [Trait("Category", "ViewModel")]
    public void EveryGatewayProblemTypeHasASeverityAndSentence(string problemType)
    {
        var presentation = ProblemPresentation.FromProblem(CreateProblem(problemType));

        Assert.Equal(ExpectedSeverities[problemType], presentation.Severity);
        Assert.False(string.IsNullOrWhiteSpace(presentation.Sentence));
    }

    [Fact]
    [Trait("Category", "ViewModel")]
    public void UnmappedProblemTypeFails()
    {
        var problem = CreateProblem(PegasusProblemTypes.Prefix + "unmapped");

        Assert.Throws<InvalidOperationException>(() => ProblemPresentation.FromProblem(problem));
    }

    [Fact]
    [Trait("Category", "ViewModel")]
    public void NoBannedWordAppearsInAnyProblemSentence()
    {
        foreach (var operatorString in DesktopOperatorStrings())
        {
            foreach (var bannedWord in BannedWords)
            {
                Assert.DoesNotMatch(
                    new Regex(
                        $"(?<!\\w){Regex.Escape(bannedWord)}(?!\\w)",
                        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
                    operatorString);
            }
        }
    }

    [Fact]
    [Trait("Category", "ViewModel")]
    public void NoRawCodeInAProblemSentence()
    {
        var rawCodePatterns = new[]
        {
            new Regex(@"\b[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
            new Regex(@"\b[a-z][a-z0-9]*_[a-z0-9_]+\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
            new Regex(@"\b[A-Z][a-z]+(?:[A-Z][a-z]+)+\b", RegexOptions.CultureInvariant),
            new Regex(@"\b[1-5][0-9]{2}\b", RegexOptions.CultureInvariant)
        };

        foreach (var operatorString in DesktopOperatorStrings())
        {
            foreach (var rawCodePattern in rawCodePatterns)
            {
                Assert.False(
                    rawCodePattern.IsMatch(operatorString),
                    $"Raw code pattern '{rawCodePattern}' appeared in '{operatorString}'.");
            }
        }
    }

    private static PegasusProblem CreateProblem(string type) =>
        new(type, "Test problem", 400, null, null, "test-reference");

    private static IEnumerable<string> DesktopOperatorStrings()
    {
        var strings = ProblemPresentation.OperatorStrings.ToList();
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null && !Directory.Exists(Path.Combine(current.FullName, "src", "Pegasus.Desktop")))
        {
            current = current.Parent;
        }

        Assert.NotNull(current);

        var desktopRoot = Path.Combine(current!.FullName, "src", "Pegasus.Desktop");
        foreach (var path in Directory.EnumerateFiles(desktopRoot, "*.xaml", SearchOption.AllDirectories))
        {
            var markup = File.ReadAllText(path);
            strings.AddRange(
                XamlOperatorAttribute.Matches(markup)
                    .Select(match => match.Groups[1].Value)
                    .Where(value => !value.StartsWith('{')));
        }

        return strings.Distinct(StringComparer.Ordinal);
    }
}
