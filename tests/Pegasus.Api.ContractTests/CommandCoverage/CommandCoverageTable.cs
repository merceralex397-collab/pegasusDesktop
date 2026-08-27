using System.Net.Http;
using Pegasus.Core.Identity;

namespace Pegasus.Api.ContractTests.CommandCoverage;

public delegate HttpRequestMessage CommandRequestFactory(CommandCoverageTestContext context);

public delegate (HttpRequestMessage First, HttpRequestMessage Replay)
    CommandReplayRequestFactory(CommandCoverageTestContext context);

public delegate Task<CommandEffectSnapshot> CommandEffectReader(
    CommandCoverageTestContext context);

public delegate Task<string> CommandVersionReader(
    CommandCoverageTestContext context);

public sealed record CommandEffectSnapshot(string State, int ActionHistoryEntries);

/// <summary>
/// One literal contract row per command endpoint. Endpoint-specific setup and
/// effect snapshots stay with the row; the theory classes only enforce the
/// shared transport contract.
/// </summary>
public sealed record CommandCoverageRow(
    string RoutePattern,
    string Method,
    StaffAccessRight RequiredAccessRight,
    bool HasVersionToken,
    bool HasOperationKey,
    CommandRequestFactory CreateValidRequest,
    CommandRequestFactory CreateUnauthenticatedRequest,
    CommandRequestFactory CreateWrongRightRequest,
    CommandRequestFactory? CreateStaleVersionRequest,
    CommandRequestFactory CreateInvalidRequest,
    CommandReplayRequestFactory? CreateReplayRequests,
    CommandEffectReader ReadEffectAsync,
    CommandVersionReader? ReadExpectedCurrentVersionAsync,
    string ExpectedStateAfterReplay,
    string InvalidProblemType = Pegasus.Contracts.ProblemDetails.PegasusProblemTypes.Validation,
    string InvalidProblemTitle = "Validation failed",
    bool IsPlaceholder = false);

/// <summary>
/// The merged host currently has no command endpoint. Keeping this table
/// empty is intentional: the guard remains green now and turns red as soon as
/// a command is added without its explicit, reviewed row.
/// </summary>
internal static class CommandCoverageTable
{
    public static IReadOnlyList<CommandCoverageRow> Rows { get; } = [];

    public static IEnumerable<object[]> AllRows =>
        Rows.Count == 0
            ? [new object[] { EmptyTableRow }]
            : Rows.Select(row => new object[] { row });

    public static IEnumerable<object[]> VersionRows =>
        Rows.Count == 0
            ? [new object[] { EmptyTableRow }]
            : Rows.Where(row => row.HasVersionToken)
                .Select(row => new object[] { row });

    public static IEnumerable<object[]> OperationRows =>
        Rows.Count == 0
            ? [new object[] { EmptyTableRow }]
            : Rows.Where(row => row.HasOperationKey && row.CreateReplayRequests is not null)
                .Select(row => new object[] { row });

    private static CommandCoverageRow EmptyTableRow => new(
        "<no-command-endpoints>",
        "POST",
        StaffAccessRight.PerformCasework,
        false,
        false,
        _ => new HttpRequestMessage(),
        _ => new HttpRequestMessage(),
        _ => new HttpRequestMessage(),
        null,
        _ => new HttpRequestMessage(),
        null,
        _ => Task.FromResult(new CommandEffectSnapshot(string.Empty, 0)),
        null,
        string.Empty,
        IsPlaceholder: true);
}

internal static class CommandCoverageGuard
{
    public static IReadOnlyList<string> FindMismatches(
        IReadOnlyList<CommandEndpoint> endpoints,
        IReadOnlyList<CommandCoverageRow> rows)
    {
        var endpointByKey = endpoints
            .GroupBy(ToKey, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);
        var rowByKey = rows
            .GroupBy(ToKey, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);
        var mismatches = new List<string>();

        foreach (var endpoint in endpoints)
        {
            var key = ToKey(endpoint);
            if (!rowByKey.TryGetValue(key, out var matchingRows))
            {
                mismatches.Add($"Missing coverage row for {key}.");
                continue;
            }

            if (matchingRows.Length != 1)
            {
                mismatches.Add($"Expected exactly one coverage row for {key}, found {matchingRows.Length}.");
            }

            if (endpoint.DeclaredAccessRight is { } accessRight
                && matchingRows[0].RequiredAccessRight != accessRight)
            {
                mismatches.Add(
                    $"Coverage row for {key} declares {matchingRows[0].RequiredAccessRight}, "
                    + $"but the endpoint declares {accessRight}.");
            }
        }

        foreach (var row in rows)
        {
            var key = ToKey(row);
            if (row.HasOperationKey != (row.CreateReplayRequests is not null))
            {
                mismatches.Add(
                    $"Coverage row for {key} must provide replay requests exactly when it declares an operation key.");
            }

            if (row.HasVersionToken != (row.CreateStaleVersionRequest is not null
                                        && row.ReadExpectedCurrentVersionAsync is not null))
            {
                mismatches.Add(
                    $"Coverage row for {key} must provide stale-version and current-version evidence exactly when it declares a version token.");
            }

            if (!endpointByKey.TryGetValue(key, out var matchingEndpoints))
            {
                mismatches.Add($"Coverage row has no command endpoint for {key}.");
                continue;
            }

            if (matchingEndpoints.Length != 1)
            {
                mismatches.Add(
                    $"Expected exactly one command endpoint for {key}, found {matchingEndpoints.Length}.");
            }
        }

        return mismatches;
    }

    private static string ToKey(CommandEndpoint endpoint) =>
        $"{endpoint.Method.ToUpperInvariant()} {endpoint.RoutePattern}";

    private static string ToKey(CommandCoverageRow row) =>
        $"{row.Method.ToUpperInvariant()} {row.RoutePattern}";
}
