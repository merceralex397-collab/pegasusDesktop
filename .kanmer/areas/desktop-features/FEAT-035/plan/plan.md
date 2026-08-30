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

## Review-fix completion (2026-08-30)

The first independent review returned BLOCK. Its findings were substantive and are now resolved:

- Provider correlation was present at the HTTP boundary but was not durable through the queued work item and provider calls. Added the correlation field to the persisted vehicle lookup request, carried it through the Core work item and worker, sent it on DVLA, DVSA token, and DVSA vehicle requests, and recorded it in action history.
- The previous integration coverage did not prove the complete route → durable store → worker → replay path. Added `VehicleGatewayReplayIntegrationTests`, which drives the real HTTP route and Core/SQL stores, runs the automatic sweep and worker with private replay fixtures, and verifies staff/automation attribution, correlations, distinct `error` versus `not_found` outcomes, and the read route.
- Missing vehicle observations now use the typed `VehicleSuggestionUnavailableException` path rather than an accidental `KeyNotFoundException`.
- `expectedVersion` is enforced as required at the request boundary, with validation for omission and matching OpenAPI required/schema metadata.
- The OpenAPI output now declares the closed outcome/state/decision enums, conditional `If-None-Match` input, and `ETag` response headers.

### Correction to the 2026-08-29 simplification note

The earlier statement that provider-bound correlation required no persistence field or migration is superseded by the independent review finding. Durable provider correlation is required by this ticket's acceptance criteria and is within scope. The corrected implementation adds the minimal `CorrelationId` column and migration to the existing vehicle lookup request table; no new store, provider client, compatibility path, or cloud integration was introduced. After this review fix, the simplification pass was re-evaluated and has no unapplied findings.

### Final validation

- `dotnet restore ./Pegasus.slnx --locked-mode` — succeeded; packages up to date.
- `dotnet build ./Pegasus.slnx --configuration Release --no-restore /nr:false` — succeeded; 0 warnings, 0 errors.
- Full API contract suite — 29 passed, 0 failed.
- Focused vehicle Core suite — 36 passed, 0 failed.
- Focused vehicle/replay/production integration suite — 27 passed, 0 failed.
- Architecture suite — 121 passed, 0 failed.
- Required `Category!=Corpus&Category!=Browser` integration suite — 972 passed, 2 skipped, 0 failed, 974 total. The skips are the existing QDOS mapped-instruction and custody embedded-photograph tests.
- Committed migration schema guard — 1 passed.
- `git diff --check` — passed; only normal line-ending conversion warnings.
- Secret scan — no provider credential values or keys introduced or exposed; only the existing bearer/token redaction regex matched.
- No live provider call, cloud write, deployment, upstream sync, or corpus mutation was performed.

## Hosted CI correction (2026-08-30)

PR #51's first exact-head run `33280183638` exposed that the contract suite was not actually SQL-independent on a clean runner: authenticated requests traversed the shared password-change middleware, which queried the absent `PegasusDevelopment` database. The hosted log showed Core 941/941 and architecture 121/121 passing; the filtered API contract run failed 7/18, with vehicle authorization/version cases becoming 400s and the disabled-gateway check seeing a database-login failure.

The test-only correction adds a scoped in-memory `IUserStore<PegasusIdentityUser>` to both contract web factories. It removes the hidden SQL dependency without changing product authentication or vehicle behavior. Exact local `Category=Contract` validation after the correction is 18 passed, 0 failed. The correction is commit `3663cd779194e7f24fc59a99d724e12ba54261d6`, pushed to PR #51; hosted rerun is required before merge.

## Fresh review-fix correction (2026-08-30)

The independent review identified three concrete issues on the earlier head 4f9dfc1e; all are addressed on the current branch before requesting a fresh review:

- Automatic reconciliation no longer strips punctuation or non-ASCII characters in infrastructure. It passes the stored value unchanged to the Core VehicleLookupRequest, so invalid registrations are rejected by the sole normalization owner. AutomaticVehicleLookupTests.SweepDoesNotRepairInvalidRegistrationBeforeCoreValidation proves this.
- Durable provider correlation is now exposed separately from the current HTTP correlation. Queue/replay responses expose providerCorrelationId; evidence observations expose their persisted provider correlation; current request/read correlation remains the response/header correlation for that HTTP call. VehicleGatewayReplayIntegrationTests proves initial request, idempotent replay with a different HTTP correlation, worker persistence, and read projection remain distinguishable.
- VehicleLookupCorrelation now adds the column nullable, backfills each legacy row as vehicle-lookup:migrated:<WorkItemId>, then makes it non-null without a shared default. Existing direct SQL fixtures were updated to supply explicit correlations.

The review was against 4f9dfc1e, so its BLOCK is not treated as a final-head review. A fresh independent reviewer must inspect the pushed final head.

### Corrected local validation

- dotnet build ./Pegasus.slnx --configuration Release --no-restore /nr:false — succeeded; 0 warnings, 0 errors.
- dotnet test ./tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj --configuration Release --no-build --filter "Category=Contract" — 18 passed, 0 failed.
- dotnet test ./tests/Pegasus.Core.Tests/Pegasus.Core.Tests.csproj --configuration Release --no-build — 941 passed, 0 failed.
- Focused vehicle/automatic/replay/terminal/production integration filter — 31 passed, 0 failed.
- dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --no-build --filter "Category!=Corpus&Category!=Browser" — 973 passed, 2 skipped, 0 failed, 975 total. Skips remain the existing QDOS mapped-instruction and custody embedded-photograph tests.
- Architecture tests — 121 passed, 0 failed.
- Committed migration schema guard — 1 passed, 0 failed.
- git diff --check — passed; only normal LF/CRLF conversion warnings.

No merge, proof, hosted-green claim, or Kanmer finalization is made by this correction. The next action is commit/push, then fresh independent review and exact-head hosted CI.

## Exact-head CI failure and correction (2026-08-30)

Hosted run `33282640860` at head `e2e9a2f5cfa4ba2827d73afb934d2da4bed025b9` failed in `unit` only: `OpenApiSnapshotTests.DisabledGatewayDoesNotExposeOpenApiDocument` received HTTP 500 because the authenticated contract host's status-page rail query opened the absent `PegasusDevelopment` database. Core 941/941, architecture 121/121, SQL shard 1, and all non-test jobs passed; remaining SQL/browser jobs were still running at diagnosis.

The test now sends `X-Contract-Unauthenticated` for this unauthenticated disabled-gateway probe, preventing the unrelated authenticated status-page rail query while retaining the expected 404 assertion. Focused test and full `Category=Contract` suite pass locally (18/18). A new commit and exact-head CI run are required; no merge or finalization is claimed.

## Final-head hosted CI success (2026-08-30)

Run `33283250011` passed at exact head `cc91137a4a9e95b99021fe652d367677e3f2c574` (`cc91137a`). All required jobs passed: unit, SQL integration shards 1–3, SQL integration coverage, browser, changes, documentation, local-development-scripts, and reference-data. Infrastructure was skipped by the workflow. PR #51 reports `CLEAN`. The disabled-gateway test correction is therefore validated on the hosted runner; independent review remains the only pre-merge gate.

## Independent review PASS (2026-08-30)

Helmholtz the 2nd (`01a0500d-f4e9-7eb0-9b7d-e9cdae9ba3c8`), an independent `pegasus-desktop-reviewer` that did not implement this ticket, reviewed PR #51 at exact head `cc91137a4a9e95b99021fe652d367677e3f2c574` and returned PASS with no findings. The review rechecked all prior blockers, route/Core/contract coverage, migration ordering, security boundary, simplification pass, exact-head CI, and merge readiness. PR #51 is eligible for merge to `dev`.
