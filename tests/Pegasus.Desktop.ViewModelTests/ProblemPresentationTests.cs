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

        Assert.True(Enum.IsDefined(presentation.Severity));
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
        foreach (var operatorString in ProblemPresentation.OperatorStrings)
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

        foreach (var operatorString in ProblemPresentation.OperatorStrings)
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
}
