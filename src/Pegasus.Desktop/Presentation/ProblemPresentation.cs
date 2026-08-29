using Pegasus.Contracts.ProblemDetails;

namespace Pegasus.Desktop.Presentation;

public enum ProblemSeverity
{
    Informational,
    Warning,
    Error
}

public sealed record ProblemPresentation(
    string ProblemType,
    ProblemSeverity Severity,
    string Sentence,
    string? Reference)
{
    public const string ReferenceLabel = "Reference";
    public const string CopyReferenceLabel = "Copy reference";
    public const string CopyButtonLabel = "Copy";

    private static readonly IReadOnlyDictionary<string, ProblemDefinition> Definitions =
        new Dictionary<string, ProblemDefinition>(StringComparer.Ordinal)
        {
            [PegasusProblemTypes.Validation] = new(
                ProblemSeverity.Warning,
                "No case or reference was created; review the missing or conflicting evidence."),
            [PegasusProblemTypes.NotAuthorized] = new(
                ProblemSeverity.Error,
                "You are not authorized for this action."),
            [PegasusProblemTypes.VersionConflict] = new(
                ProblemSeverity.Warning,
                "The case changed since it was read. Reload before trying again."),
            [PegasusProblemTypes.LeaseConflict] = new(
                ProblemSeverity.Warning,
                "The case is being edited by another person. Try again when it is available."),
            [PegasusProblemTypes.LeaseExpired] = new(
                ProblemSeverity.Warning,
                "Your edit access has expired. Reload before trying again."),
            [PegasusProblemTypes.OperationConflict] = new(
                ProblemSeverity.Warning,
                "This action conflicts with an earlier action. Review the current result before trying again."),
            [PegasusProblemTypes.ClientUnsupported] = new(
                ProblemSeverity.Error,
                "This version of Pegasus is no longer supported. Update Pegasus before continuing."),
            [PegasusProblemTypes.PasswordChangeRequired] = new(
                ProblemSeverity.Warning,
                "Change your password before continuing."),
            [PegasusProblemTypes.AccountDisabled] = new(
                ProblemSeverity.Error,
                "This account is disabled. Contact an administrator."),
            [PegasusProblemTypes.ProviderUnavailable] = new(
                ProblemSeverity.Error,
                "The external service is unavailable. Try again later."),
            [PegasusProblemTypes.NotFound] = new(
                ProblemSeverity.Informational,
                "The requested record could not be found."),
            [PegasusProblemTypes.RateLimited] = new(
                ProblemSeverity.Warning,
                "Too many requests were made. Wait and try again."),
            [PegasusProblemTypes.Maintenance] = new(
                ProblemSeverity.Error,
                "The service could not complete the request. Try again later.")
        };

    public static IReadOnlyList<string> OperatorStrings { get; } =
        Definitions.Values
            .Select(definition => definition.Sentence)
            .Append(ReferenceLabel)
            .Append(CopyReferenceLabel)
            .Append(CopyButtonLabel)
            .ToArray();

    public static ProblemPresentation FromProblem(PegasusProblem problem)
    {
        ArgumentNullException.ThrowIfNull(problem);

        if (!Definitions.TryGetValue(problem.Type, out var definition))
        {
            throw new InvalidOperationException(
                $"The gateway problem type '{problem.Type}' has no desktop presentation mapping.");
        }

        return new ProblemPresentation(
            problem.Type,
            definition.Severity,
            definition.Sentence,
            string.IsNullOrWhiteSpace(problem.CorrelationId) ? null : problem.CorrelationId);
    }

    private sealed record ProblemDefinition(ProblemSeverity Severity, string Sentence);
}
