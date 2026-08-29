using System.Net.Http;
using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Pegasus.Contracts.Vehicle;
using Pegasus.Core.Identity;
using Pegasus.Core.Vehicle;
using Pegasus.Core.Workflow;

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

internal static class CommandCoverageTable
{
    private static readonly Guid CaseId =
        Guid.Parse("9f45fbe5-2c58-4a92-bf72-df0f2f2e4d01");
    private static readonly Guid ObservationId =
        Guid.Parse("1d4d10f9-c8ac-4f83-8d2a-5e5bc4a9c9d0");

    public static IReadOnlyList<CommandCoverageRow> Rows { get; } =
    [
        new(
            $"/api/v1/cases/{{caseId:guid}}/vehicle/lookups",
            "POST",
            StaffAccessRight.PerformCasework,
            HasVersionToken: true,
            HasOperationKey: true,
            context => CreateRequest(
                context,
                $"/api/v1/cases/{CaseId:D}/vehicle/lookups",
                "lookup-operation"),
            context => CreateRequest(
                context,
                $"/api/v1/cases/{CaseId:D}/vehicle/lookups",
                "lookup-operation",
                "X-Contract-Unauthenticated"),
            context => CreateRequest(
                context,
                $"/api/v1/cases/{CaseId:D}/vehicle/lookups",
                "lookup-operation",
                "X-Contract-Wrong-Right"),
            context => CreateRequest(
                context,
                $"/api/v1/cases/{CaseId:D}/vehicle/lookups",
                "lookup-stale",
                null,
                expectedVersion: 6),
            context => CreateRequest(
                context,
                $"/api/v1/cases/{CaseId:D}/vehicle/lookups",
                string.Empty,
                null,
                registration: string.Empty,
                expectedVersion: -1,
                leaseToken: string.Empty),
            context =>
            {
                var first = CreateRequest(
                    context,
                    $"/api/v1/cases/{CaseId:D}/vehicle/lookups",
                    "lookup-replay");
                var replay = CreateRequest(
                    context,
                    $"/api/v1/cases/{CaseId:D}/vehicle/lookups",
                    "lookup-replay");
                return (first, replay);
            },
            context => Task.FromResult(Store(context).ReadEffect()),
            context => Task.FromResult(Store(context).CurrentCaseVersion.ToString(CultureInfo.InvariantCulture)),
            "pending"),
        new(
            $"/api/v1/cases/{{caseId:guid}}/vehicle/suggestions/{{suggestionId:guid}}/accept",
            "POST",
            StaffAccessRight.PerformCasework,
            HasVersionToken: true,
            HasOperationKey: true,
            context => CreateRequest(
                context,
                $"/api/v1/cases/{CaseId:D}/vehicle/suggestions/{ObservationId:D}/accept",
                "accept-operation"),
            context => CreateRequest(
                context,
                $"/api/v1/cases/{CaseId:D}/vehicle/suggestions/{ObservationId:D}/accept",
                "accept-operation",
                "X-Contract-Unauthenticated"),
            context => CreateRequest(
                context,
                $"/api/v1/cases/{CaseId:D}/vehicle/suggestions/{ObservationId:D}/accept",
                "accept-operation",
                "X-Contract-Wrong-Right"),
            context => CreateRequest(
                context,
                $"/api/v1/cases/{CaseId:D}/vehicle/suggestions/{ObservationId:D}/accept",
                "accept-stale",
                null,
                expectedVersion: 6),
            context => CreateRequest(
                context,
                $"/api/v1/cases/{CaseId:D}/vehicle/suggestions/{ObservationId:D}/accept",
                string.Empty,
                null,
                expectedVersion: -1,
                leaseToken: string.Empty,
                reason: string.Empty),
            context =>
            {
                var first = CreateRequest(
                    context,
                    $"/api/v1/cases/{CaseId:D}/vehicle/suggestions/{ObservationId:D}/accept",
                    "accept-replay");
                var replay = CreateRequest(
                    context,
                    $"/api/v1/cases/{CaseId:D}/vehicle/suggestions/{ObservationId:D}/accept",
                    "accept-replay");
                return (first, replay);
            },
            context => Task.FromResult(Store(context).ReadEffect()),
            context => Task.FromResult(Store(context).CurrentCaseVersion.ToString(CultureInfo.InvariantCulture)),
            "confirmed")
    ];

    private static VehicleCommandCoverageStore Store(CommandCoverageTestContext context) =>
        context.Services.GetRequiredService<VehicleCommandCoverageStore>();

    private static HttpRequestMessage CreateRequest(
        CommandCoverageTestContext context,
        string path,
        string operationKey,
        string? authHeader = null,
        int expectedVersion = 7,
        string registration = "AB12CDE",
        string leaseToken = "lease-token",
        string reason = "reviewed")
    {
        var isAcceptance = path.Contains("/suggestions/", StringComparison.Ordinal);
        var json = isAcceptance
            ? $$"""{"expectedVersion":{{expectedVersion}},"decision":"accept","operationKey":"{{operationKey}}","reason":"{{reason}}","editLeaseToken":"{{leaseToken}}"}"""
            : $$"""{"registration":"{{registration}}","expectedVersion":{{expectedVersion}},"operationKey":"{{operationKey}}","editLeaseToken":"{{leaseToken}}"}""";
        var request = CommandCoverageTestContext.CreateJsonRequest(
            "POST",
            path,
            json,
            correlationId: "command-coverage");
        if (authHeader is not null)
        {
            request.Headers.TryAddWithoutValidation(authHeader, "true");
        }

        return request;
    }

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

internal sealed class VehicleCommandCoverageStore :
    IRequestVehicleLookupStore,
    IAcceptVehicleSuggestionStore
{
    private readonly Dictionary<string, RequestedVehicleLookup> lookupOperations =
        new(StringComparer.Ordinal);
    private readonly Dictionary<string, AcceptedVehicleSuggestion> acceptanceOperations =
        new(StringComparer.Ordinal);
    private int actionHistoryEntries;
    private string state = "empty";

    public long CurrentCaseVersion { get; private set; } = 7;

    public Task<RequestedVehicleLookup> RequestAsync(
        RequestVehicleLookupCommand command,
        CancellationToken cancellationToken)
    {
        if (lookupOperations.TryGetValue(command.OperationKey, out var replay))
        {
            return Task.FromResult(replay with { IsReplay = true });
        }

        RequireCurrentVersion(command.CaseId, command.ExpectedCaseVersion);
        var result = new RequestedVehicleLookup(
            Guid.Parse("2d4d10f9-c8ac-4f83-8d2a-5e5bc4a9c9d0"),
            command.CaseId,
            command.Registration,
            VehicleLookupWorkState.Pending,
            ++CurrentCaseVersion,
            IsReplay: false);
        lookupOperations.Add(command.OperationKey, result);
        state = "pending";
        actionHistoryEntries++;
        return Task.FromResult(result);
    }

    public Task<AcceptedVehicleSuggestion> AcceptAsync(
        AcceptVehicleSuggestionCommand command,
        CancellationToken cancellationToken)
    {
        if (acceptanceOperations.TryGetValue(command.OperationKey, out var replay))
        {
            return Task.FromResult(replay with { IsReplay = true });
        }

        RequireCurrentVersion(command.CaseId, command.ExpectedCaseVersion);
        var result = new AcceptedVehicleSuggestion(
            Guid.Parse("3d4d10f9-c8ac-4f83-8d2a-5e5bc4a9c9d0"),
            command.CaseId,
            command.LookupObservationId,
            command.Decision,
            new VehicleConfirmationValues("AB12CDE", "Ford", "Focus", 12345, VehicleMileageUnit.Miles),
            new VehicleEvidenceProvenance(
                "dvla-dvsa-replay",
                "replay-v1",
                "response-123",
                new DateTimeOffset(2031, 5, 6, 12, 0, 0, TimeSpan.Zero),
                null,
                new DateTimeOffset(2031, 5, 6, 10, 0, 0, TimeSpan.Zero)),
            ++CurrentCaseVersion,
            IsReplay: false);
        acceptanceOperations.Add(command.OperationKey, result);
        state = "confirmed";
        actionHistoryEntries++;
        return Task.FromResult(result);
    }

    public CommandEffectSnapshot ReadEffect() =>
        new(state, actionHistoryEntries);

    private void RequireCurrentVersion(Guid caseId, long expectedVersion)
    {
        if (expectedVersion != CurrentCaseVersion)
        {
            throw new CaseVersionConflictException(caseId, expectedVersion, CurrentCaseVersion);
        }
    }
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
