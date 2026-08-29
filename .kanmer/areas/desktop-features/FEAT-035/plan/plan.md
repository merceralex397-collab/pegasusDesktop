# Plan — FEAT-035 DVLA/DVSA gateway endpoints

## Governing documents

This ticket is currently docs_todo: true. Its binding conversion decisions are the area plan and ADR-0107/0104 where applicable; link the final FRD/ADR path once the canonical documents are authored. Do not create a competing governing document in this task.

## Chosen approach

Expose request lookup, accept suggestion, status, cache lifetime and provenance through the gateway while retaining provider credentials and policy server-side.

## Steps

1. Inventory existing vehicle provider port, cache and provenance owner.
2. Design minimal request/status/accept contracts around the existing use cases.
3. Implement gateway routes with auth/problem mapping and no secret/provider-internal projection.
4. Test validation, known response, unavailable/timeout and provenance states using replay.

## Verification

- Contract tests cover role denial, invalid VRM, provider failure and accepted suggestion.
- No provider credential/token appears in desktop contract or logs.
- Cache/provenance semantics are asserted by the single Core owner.

## Risks and dependencies

FEAT-036 consumes these contracts; direct desktop provider calls are forbidden.

Implementation uses the named gateway/WinUI/test agents, records simplification, and receives independent review.

## Current-head coordination decision (2026-08-29)

origin/dev contains the shared desktop gateway composition and Core vehicle workflow but no vehicle route group. FEAT-035 therefore owns the first /api/v1 vehicle route group in Pegasus.Web/Api; it will invoke the existing Core ports and be structured so the later assessment routes in [[GWY-014]] extend the same group rather than registering a second vehicle group. The desktop workflow remains downstream in [[FEAT-036]].

The current operator boundary prohibits cloud writes/deployment and upstream synchronization. Tests will use the existing DevelopmentOffline profile and replay adapter; a live Key Vault names-only check is not required to implement this ticket and will not be represented as completed evidence unless actually run.

## Implementation evidence (2026-08-29)

Implemented the planned gateway slice on `task/dsk-07-09-vehicle-endpoints`:

- Added typed Pegasus-owned vehicle request/response DTOs in `src/Pegasus.Contracts/VehicleContracts.cs`; raw provider payloads and credential fields are not projected.
- Added `/api/v1/cases/{caseId}/vehicle` routes in `src/Pegasus.Web/Api/VehicleEndpoints.cs`, composed through the existing desktop gateway group and Core ports.
- Preserved all seven Core outcomes on the wire, including distinct `notFound`, `unavailable`, `throttled`, and `failed` responses and retry metadata.
- Mapped authentication, case authorization, expected-version, edit-lease, idempotency, and the six vehicle refusal exceptions through the existing gateway/problem conventions.
- Reused the Core `VehicleLookupRequest` as the sole registration-normalization owner; the Web adapter invokes it rather than adding another rule.
- Added the Core evidence version to the existing `CaseVehicleEvidence` projection and emitted weak `ETag` values from that version.
- Added contract coverage, replay-adapter integration coverage, OpenAPI output, endpoint-map/FRD updates, and the required exporter array-preservation fix.

Validation completed on the final compiled tree:

- `dotnet restore ./Pegasus.slnx --locked-mode` — succeeded; packages up to date.
- `dotnet build ./Pegasus.slnx --configuration Release --no-restore /nr:false` — succeeded; 0 warnings, 0 errors.
- `dotnet test ./tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj --configuration Release --no-build --logger "console;verbosity=minimal"` — 27 passed, 0 failed.
- `dotnet test ./tests/Pegasus.Core.Tests/Pegasus.Core.Tests.csproj --configuration Release --no-build --filter "FullyQualifiedName~Vehicle" --logger "console;verbosity=minimal"` — 36 passed, 0 failed.
- `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --no-build --filter "FullyQualifiedName~VehicleReplayAdapterTests" --logger "console;verbosity=minimal"` — 2 passed, 0 failed.
- `dotnet test ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj --configuration Release --no-build --logger "console;verbosity=minimal"` — 121 passed, 0 failed.
- `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --no-build --filter "Category!=Corpus&Category!=Browser" --logger "console;verbosity=minimal"` — 970 passed, 2 skipped, 0 failed, 972 total.
- `git diff --check` — passed; only normal LF/CRLF conversion warnings.
- Secret scan over `src/Pegasus.Contracts src/Pegasus.Desktop src/Pegasus.Desktop.Infrastructure` — no credential values or provider keys; matches are the existing bearer/token redaction implementation only.
- No live provider call, cloud write, deployment, upstream sync, corpus mutation, or direct desktop provider client was used.

## Simplification pass (2026-08-29)

- DTO projection remains explicit because the gateway boundary must expose Pegasus-owned contracts and exclude provider JSON/secrets; no generic mapper or second provider model was introduced.
- The endpoint filter and metadata reuse the existing gateway authorization/correlation mechanisms and are needed for the route’s access-right and correlation contracts.
- Registration normalization is centralized in Core; the Web layer does not duplicate the policy.
- The evidence version is carried by the existing Core projection because it is required for the response version and weak ETag; no new store or schema was added.
- The OpenAPI exporter one-item-list fix is a required behavior-preserving correction exposed by the new routes, not a feature addition.
- Replay fixtures are private temporary test data and do not alter the immutable `corpus/`.
- No speculative compatibility path, fallback implementation, cloud integration, provider credential handling, migration, or unrelated cleanup was added.
- No unapplied simplification findings remain. Provider-bound correlation is represented by the existing request correlation filter and durable operation key/action history; adding a new persistence field or migration would exceed this gateway ticket and is not required by the current Core/provider boundary.
