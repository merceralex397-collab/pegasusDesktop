namespace Pegasus.Contracts.Requests;

public sealed record MutationEnvelope(
    long ExpectedVersion,
    string OperationKey,
    string Reason,
    string EditLeaseToken);

public static class OperationKeys
{
    public const int MaxLength = 100;
    public const string DesktopPrefix = "desk:";
}
