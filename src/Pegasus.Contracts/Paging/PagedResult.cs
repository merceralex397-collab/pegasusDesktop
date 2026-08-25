namespace Pegasus.Contracts.Paging;

// Mirrors CaseQueries.cs:69-74 and EfCaseQueryStore.cs:115-133: the existing ports
// fetch one extra item for the next-page flag and do not produce a total count.
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    bool HasPreviousPage,
    bool HasNextPage);
