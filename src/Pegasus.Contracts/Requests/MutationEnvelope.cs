namespace Pegasus.Contracts.Requests;

/// <summary>
/// Common mutation body fields. Concurrency and idempotency values are body
/// fields, never headers. Desktop callers use <c>desk:&lt;guid&gt;</c>; the
/// gateway applies the <c>RequireOperationKey</c> rules of at most 100
/// characters, with no whitespace or control characters, and does not
/// duplicate that validation here. The 200-character exception is limited to
/// <c>UnidentifiedValidation.MaximumOperationKeyLength</c> at
/// <c>src/Pegasus.Core/Intake/Unidentified/UnidentifiedContracts.cs:398</c>.
/// </summary>
public sealed record MutationEnvelope(
    long ExpectedVersion,
    string OperationKey,
    string Reason,
    string EditLeaseToken);

public static class OperationKeys
{
    /// <summary>The maximum length accepted for the standard operation key.</summary>
    public const int MaxLength = 100;

    /// <summary>The operation-key prefix reserved for desktop callers.</summary>
    public const string DesktopPrefix = "desk:";
}
