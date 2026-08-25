namespace Pegasus.Core.Intake;

/// <summary>
/// The single persisted-code vocabulary for <see cref="IntakeDecision"/>.
/// Readers that are projecting possibly-corrupt persisted data should use
/// <see cref="TryParse"/> and fail closed; command validation may use
/// <see cref="Parse"/> to reject an unknown code.
/// </summary>
public static class IntakeDecisionCodes
{
    private static readonly IReadOnlyDictionary<IntakeDecision, string> Codes =
        new Dictionary<IntakeDecision, string>
        {
            [IntakeDecision.CaseCreated] = "case_created",
            [IntakeDecision.NeedsSorting] = "needs_sorting",
            [IntakeDecision.BlockedIntake] = "blocked_intake",
            [IntakeDecision.Unsupported] = "unsupported",
            [IntakeDecision.OcrRequired] = "ocr_required",
            [IntakeDecision.TechnicalFailure] = "technical_failure",
            [IntakeDecision.ImageIntakeRegistered] = "image_intake_registered"
        };

    private static readonly Dictionary<string, IntakeDecision> Decisions =
        Codes.ToDictionary(item => item.Value, item => item.Key, StringComparer.Ordinal);

    public static IReadOnlyList<string> All { get; } = Codes.Values.ToArray();

    public static string ToCode(IntakeDecision value) =>
        Codes.TryGetValue(value, out var code)
            ? code
            : throw new ArgumentOutOfRangeException(
                nameof(value),
                value,
                "Unknown intake decision.");

    public static IntakeDecision Parse(string value) =>
        TryParse(value, out var decision)
            ? decision
            : throw new InvalidDataException($"Unknown persisted intake decision code '{value}'.");

    public static bool TryParse(string? value, out IntakeDecision decision)
    {
        decision = default;
        return value is not null && Decisions.TryGetValue(value, out decision);
    }
}
