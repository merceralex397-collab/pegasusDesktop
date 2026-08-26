namespace Pegasus.Contracts.Paging;

/// <summary>
/// Shared upper bound for page sizes. Individual endpoints may impose a lower
/// cap; for example, <c>ListIntake</c> refuses a page size above 100
/// (<c>src/Pegasus.Core/Intake/IntakeQueryUseCases.cs:22-27</c>).
/// </summary>
public static class PagingLimits
{
    /// <summary>The maximum page size shared by the gateway contracts.</summary>
    public const int MaxPageSize = 200;
}
