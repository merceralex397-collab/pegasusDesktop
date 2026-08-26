# Checklist — FND-029

One box per plan step, in plan order. Each is independently tickable: it names the file or command
whose completion makes the box true.

- [x] Read `src/Pegasus.Core/Workflow/CaseWorkflowContracts.cs:120-200` and `src/Pegasus.Web/Mcp/AutomationMcpErrors.cs:76-89`; run `get_doc_gates FND-029`; `take_ticket` on branch `task/pegasus-contracts` created from `origin/dev`.
- [x] Create `src/Pegasus.Contracts/Pegasus.Contracts.csproj` copying `src/Pegasus.Core/Pegasus.Core.csproj`'s shape (`Microsoft.NET.Sdk`, `net10.0`, `RuntimeIdentifiers linux-x64;win-x64`, `ImplicitUsings`, `Nullable`) with zero `PackageReference`, zero `ProjectReference` and zero `FrameworkReference`.
- [x] Create `src/Pegasus.Contracts/Paging/PagedResult.cs` with exactly `public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, bool HasPreviousPage, bool HasNextPage)`, plus the comment citing `CaseQueries.cs:69-74` and `EfCaseQueryStore.cs:115-133` for why there is no total.
- [x] Create `src/Pegasus.Contracts/Paging/PagingLimits.cs` with `public const int MaxPageSize = 200;`.
- [x] Create `src/Pegasus.Contracts/ProblemDetails/PegasusProblemTypes.cs` with `Prefix = "urn:pegasus:problem:"` and exactly the thirteen slugs transcribed from `docs/desktop/03-gateway-api-and-data/README.md:167`.
- [x] Create `src/Pegasus.Contracts/ProblemDetails/PegasusProblem.cs` with the RFC 9457 members plus always-present `CorrelationId`, an optional `Extensions` dictionary, and typed `CurrentVersion` / `MinimumVersion` accessors; no payload dump, no exception text.
- [x] Create `src/Pegasus.Contracts/Requests/MutationEnvelope.cs` with `long ExpectedVersion`, `string OperationKey`, `string Reason`, `string EditLeaseToken` (no `ActionActor`, no `CaseId`), plus `OperationKeys.MaxLength = 100` and the `desk:` prefix constant.
- [x] Create `src/Pegasus.Contracts/PegasusHeaders.cs` with `ClientVersion = "X-Pegasus-Client-Version"` and `CorrelationId = "X-Correlation-Id"`.
- [x] Create `src/Pegasus.Contracts/Responses/ClientCompatibilityResponse.cs` with exactly `string MinimumVersion`, `string CurrentVersion`, `string Channel`, `string? MaintenanceMessage`, `int ValidForSeconds`.
- [x] Create `src/Pegasus.Contracts/PegasusJson.cs` exposing one `JsonSerializerOptions` (camelCase, `WhenWritingNull`), and apply `[JsonConverter(typeof(JsonStringEnumConverter))]` to every enum declared in the project (today: none — record that finding).
- [x] Add `<Project Path="src/Pegasus.Contracts/Pegasus.Contracts.csproj" />` to the `/src/` folder in `Pegasus.slnx`.
- [x] Add the same path to the server entry point created by [[FND-028]] (plan handle `DSK-02-03`). FND-028 is now on `origin/dev`; `Pegasus.Contracts` is present in `Pegasus.Server.slnf` and in the exact architecture expectation.
- [x] Insert `src/Pegasus.Contracts/Pegasus.Contracts.csproj` into the ordinal expected array in `DependencyDirectionTests.ApplicationSolutionExcludesSourceWorkspaces` (`tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs:137-149`), between the Core and Infrastructure entries.
- [x] Add the `ContractsProjectHasNoDependencies` fact to `DependencyDirectionTests.cs`, loading the csproj through `FindRepositoryRoot()` (`:509`) and asserting no `PackageReference`, `ProjectReference` or `FrameworkReference`, reusing `ProjectReferences` (`:493`) and the element-walk shape of `ForbiddenDirectDependencies` (`:480-491`).
- [x] Add a fifth `ProjectReference` to `..\..\src\Pegasus.Contracts\Pegasus.Contracts.csproj` in `tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj`.
- [x] Write `tests/Pegasus.ArchitectureTests/ContractSerializationTests.cs` covering: camelCase round-trip of `PagedResult<string>`, `PegasusProblem` and `ClientCompatibilityResponse`; the exact five serialised members of `PagedResult<string>` with no total-count property; null `MaintenanceMessage` omitted on write and tolerated as absent on read; an unknown enum value through a test-local enum; and `PegasusProblem` with no `Extensions` emitting no empty object.
- [x] Record in the plan that these serialization facts move to `tests/Pegasus.Api.ContractTests` when [[TEST-001]] (plan handle `DSK-08-01`) lands.
- [x] Run `dotnet restore ./src/Pegasus.Contracts/Pegasus.Contracts.csproj --force-evaluate` and commit the generated `src/Pegasus.Contracts/packages.lock.json`; confirm it matches the three-empty-entry shape of `src/Pegasus.Core/packages.lock.json`.
- [x] Add the `src/Pegasus.Contracts` row to `docs/current-architecture.md` § Components and dependency direction (`:55`) as a dependency-free shared DTO project.
- [x] Run the simplification pass over this branch's diff and record it under a dated `## Simplification pass` heading in the plan document.
- [x] Verification run (this box produces `proof`): `dotnet restore ./Pegasus.slnx --locked-mode`; `dotnet build ./Pegasus.slnx --configuration Release --no-restore` (exit 0, `0 Warning(s)`); `dotnet test ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj --configuration Release --no-build` filtered on `FullyQualifiedName~Contract` and then unfiltered; `grep -rn 'PackageReference\|ProjectReference' src/Pegasus.Contracts/Pegasus.Contracts.csproj` (no matches); `grep -n 'record PagedResult' src/Pegasus.Contracts/Paging/PagedResult.cs` (the exact five-member line); `grep -rn 'Total' src/Pegasus.Contracts/Paging/` (no matches); `grep -rn 'ActionActor' src/Pegasus.Contracts/` (no matches). Capture every output as tier-2 evidence.

## Progress notes

(append with `set_ticket_doc(doc: "checklist", append: true)`)


## Progress notes

- 2026-08-25: implementation and validation complete except the explicitly deferred FND-028 server-filter registration; full evidence is recorded in the plan.

- 2026-08-25 corrective review: RFC 9457 top-level extension serialization/readback was fixed with `PegasusProblemJsonConverter`; top-level version accessor tests pass. Commit `54ade310` is pushed and the fresh independent review is pending.

## Final independent review and CI — 2026-08-26

- [x] Independent review passes — fresh review PASS on exact head after evidence corrections.
- [x] Exact-head CI is green — run `33014659206`, including SQL shards 1–3 and coverage.
- [x] Current-head simplification pass covers the synchronized server-filter additions.
- [x] Commit metadata records the valid full SHA `0a3d23becc5a1038ab166effafd5203847bc3b5c`.
