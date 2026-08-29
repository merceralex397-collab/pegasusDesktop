---
id: FEAT-035
type: ticket
title: >-
  DSK-07-09 · DVLA/DVSA gateway endpoints: request lookup, accept suggestion,
  status, cache lifetime and provenance
status: review
area: desktop-features
assignee: codex-mcp-client
profile: feature
stageEntered:
  preparing: '2026-08-24T21:31:42.711Z'
  review: '2026-08-29T21:34:53.593Z'
taken_at: '2026-08-29T20:09:18.169Z'
branch: task/dsk-07-09-vehicle-endpoints
worktree: 'C:\Users\PC\Documents\GitHub\pegasus-worktrees\dsk-07-09-vehicle-endpoints'
labels:
  - desktop-conversion
  - plan-07
  - phase-6
  - tier-5
groups:
  - EPIC-008
  - HZN-007
links: []
blocks:
  - GWY-014
  - FEAT-015
  - FEAT-036
refs:
  - docs/frd/frd-06-vehicle-and-engineering-evidence.md
docs_todo: true
commits:
  - cfdd886a2b3b8dddadd550069290b707f33da96e
  - 4f9dfc1e06ea7ba947791b5e9d28f7ca2d9949a2
  - 3663cd779194e7f24fc59a99d724e12ba54261d6
prs:
  - 'https://github.com/merceralex397-collab/pegasusDesktop/pull/51'
archived: false
created: '2026-08-24T08:24:13.912Z'
updated: '2026-08-29T23:26:17.454Z'
---

## What

Project the vehicle-lookup workflow onto `/api/v1`: request a lookup for a case, accept a suggested value, and read lookup status and evidence — with the response contract carrying provenance (provider, provider version, retrieved-at, source-observed-at, cache age) and with provider failure kept distinguishable from "vehicle not found". DVLA and DVSA keys never leave Key Vault.

## Why

Proposal § 12.3 keeps lookup credentials and rate-limit coordination behind the gateway, requires responses mapped into Pegasus-owned contracts, requires a correlation identifier on every provider call, and requires that provider failures be distinguishable from "vehicle not found". § 16.2 adds that the client must show when data is cached and when it was obtained. Core already models this exactly — `VehicleLookupOutcome` has seven members including `NotFound`, `Throttled`, `Unavailable` and `Failed` — so the job is to carry that fidelity onto the wire rather than flatten it. Siblings: [[DSK-07-10]] is the desktop surface; [[DSK-03-14]] owns the same route group from the gateway plan and must land as one contract.

## Source of truth

- Plan row: `docs/desktop/07-integrations/README.md` § 5 — `DSK-07-09`
- Plan context: `docs/desktop/07-integrations/README.md` § 2 Evidence base (DVLA/DVSA paragraph and the Key Vault reference names), § 4 Target state (third bullet)
- Endpoint map: `docs/desktop/03-gateway-api-and-data/endpoint-map.md` § `Cases` rows `Vehicle` (`POST /cases/{id}/vehicle/lookups`, `POST /cases/{id}/vehicle/suggestions/{sid}/accept`)
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 12.3 DVLA/DVSA, § 13.5 Vehicle and inspection information, § 16.2 External provider resilience
- Repository evidence: `src/Pegasus.Core/Vehicle/LookupContracts.cs:3-12` (`VehicleLookupOutcome`), `:20-37` (`VehicleLookupRequest` normalisation rule — uppercase ASCII alphanumeric, max 20), `:39-80` (`VehicleDetails`, `MotTestObservation`, `VehicleLookupFailure`, `VehicleLookupResult` with `Provider`, `ProviderVersion`, `ResponseIdentity`, `RetrievedAtUtc`, `SourceObservedAtUtc`, `SourceAge`); `src/Pegasus.Core/Vehicle/VehicleWorkflow.cs:81-160` (`RequestVehicleLookupCommand`, `AcceptVehicleSuggestionCommand`, `VehicleLookupAvailability`, the interfaces), `:385-445` (the refusal exceptions); `src/Pegasus.Infrastructure/Vehicle/DvlaDvsaProductionAdapter.cs:12-30` (options and the required key names), `:129-233` (the DVLA and DVSA reads); `src/Pegasus.Infrastructure/Vehicle/DvlaDvsaAdapters.cs:7` (`DvlaDvsaReplayAdapter`); `src/Pegasus.Infrastructure/DependencyInjection.cs:553-598` (`AddProductionExternalAdapters`, `VehicleLookupAvailability.ProductionLive`), `src/Pegasus.Web/Program.cs:577` (`VehicleLookupAvailability.DevelopmentOfflineReplay`); `infra/modules/platform.bicep:558-563` (`Dvla__ApiKey`, `Dvsa__ClientId`, `Dvsa__ClientSecret`, `Dvsa__ApiKey` as Key Vault references); `docs/current-architecture.md:514`; `tests/Pegasus.IntegrationTests/ProductionVehicleLookupTests.cs`, `AutomaticVehicleLookupTests.cs`
- Binding decisions: L-01 — endpoints are `/api/v1` groups in `Pegasus.Web`. **ADR-0107** — DVLA/DVSA credentials stay behind the gateway; a step that puts a provider key in the desktop package is a defect. L-02 — evidence uses `DvlaDvsaReplayAdapter` under the `DevelopmentOffline` runtime profile; no Azure test resource.
- Depends on: `DSK-03-02` route-group skeleton; `DSK-03-03` right filter; `DSK-03-14` the same vehicle route group in the gateway plan — coordinate, do not duplicate

## Routing

- **Subagent**: `pegasus-gateway-dev` — `.codex/agents/pegasus-gateway-dev.toml`; key-boundary evidence by `pegasus-azure-auditor` — `.codex/agents/pegasus-azure-auditor.toml` (read-only)
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `dotnet-webapi` (dotnet/skills `98f84851`) → `run-tests` → `assertion-quality` (dotnet/skills `98f84851`, plugin `dotnet-test`)
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search` for `HttpClient` resilience handlers and RFC 9457); Azure MCP read-only `keyvault` (names only, never values)
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (call `get_doc_gates <id>` before every move; a move crosses at most one gated boundary)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Orient: read the plan row, the endpoint map § `Cases` Vehicle rows, `docs/frd/frd-06-vehicle-and-engineering-evidence.md`, and `docs/current-architecture.md:514` (the release-15 reconciliation sweep that already enqueues automatic lookups). Call `get_doc_gates <this ticket id>`, then `take_ticket` on branch `task/dsk-07-09-vehicle-endpoints`.
2. Read `src/Pegasus.Web/Pages/Cases/Vehicle.cshtml.cs` and `src/Pegasus.Core/Vehicle/VehicleWorkflow.cs` in full. Tabulate in `files`: each command record, its required fields, the exception it throws on refusal (`VehicleLookupUnavailableException`, `VehicleOperationConflictException`, `VehicleSuggestionUnavailableException`, `ConfirmedVehicleRegistrationRequiredException`, `ConfirmedVehicleRegistrationConflictException`, `ConfirmedVehicleFieldConflictException`) and the Core owner it delegates to.
3. Coordinate with [[DSK-03-14]]: if the vehicle route group already exists, extend it here with the provenance and failure-taxonomy rules; if not, create it to the endpoint-map shape. Record the decision in `plan`.
4. Add the DTOs to `src/Pegasus.Contracts`, projecting `VehicleLookupResult` **without losing fidelity**: `outcome` (one of `current`, `stale`, `partial`, `notFound`, `throttled`, `unavailable`, `failed`), `provider`, `providerVersion`, `retrievedAtUtc`, `sourceObservedAtUtc`, `sourceAgeSeconds`, `vehicle`, `motTests`, and `failure` with `code` and `retryable`. Never expose raw provider JSON — proposal § 12.3 forbids client code depending on provider-specific shapes.
5. Implement `POST /api/v1/cases/{caseId}/vehicle/lookups` over `IRequestVehicleLookup` with `expectedVersion`, `editLeaseToken` and `operationKey`, returning the durable request id and status. When `VehicleLookupAvailability.RequestsEnabled` is false, return a `urn:pegasus:problem:provider-unavailable` naming the mode — never a generic failure.
6. Implement `POST /api/v1/cases/{caseId}/vehicle/suggestions/{suggestionId}/accept` over `IAcceptVehicleSuggestion` with the same concurrency fields, and map each of the six refusal exceptions from step 2 to a distinct problem type from the catalogue in `docs/desktop/03-gateway-api-and-data/README.md` § 3. A staff confirmation must never be overwritten by a refresh.
7. Implement `GET /api/v1/cases/{caseId}/vehicle` over `IVehicleEvidenceQueries`, returning the confirmed values, the provenance fields and the lookup status, with a weak `ETag`.
8. Enforce the normalisation rule in exactly one place. `VehicleLookupRequest`'s constructor (`LookupContracts.cs:22-33`) already rejects anything that is not uppercase ASCII alphanumeric within 20 characters; the endpoint normalises input to that shape and lets Core validate. Do not add a second regular expression — `AGENTS.md` § Simplicity rails, "one list per concept".
9. Attach a correlation identifier to every provider-bound request and carry it into the response and the problem detail, per proposal § 12.3. Assert its presence in a test.
10. Write contract tests in `tests/Pegasus.Api.ContractTests`: each of the seven outcomes renders distinctly; `notFound` and `unavailable` are different responses; `throttled` carries `retryable: true` and any `retryAfter`; unauthorised and stale-version cases behave per the command matrix; and no response, header or problem detail contains `x-api-key`, a DVSA client secret or a bearer token.
11. Write integration tests driven by `DvlaDvsaReplayAdapter` under the `DevelopmentOffline` runtime profile, following `tests/Pegasus.IntegrationTests/ProductionVehicleLookupTests.cs` and `AutomaticVehicleLookupTests.cs`: a replayed provider failure produces `failed`, not `notFound`; a replayed empty result produces `notFound`; the automatic sweep's rows and a staff-requested row remain distinguishable.
12. Record the credential boundary as evidence: the four key names appear only in `infra/modules/platform.bicep:558-563` and are read from Key Vault; use the Azure MCP `keyvault` tool read-only to list **names** and attach that to the proof. Then update the endpoint map rows, run the simplification pass over the branch diff, record it under a dated `## Simplification pass` heading, and open the PR into `dev`.

## Acceptance criteria

- [ ] All seven `VehicleLookupOutcome` values reach the wire distinctly; provider failure is never reported as "not found".
- [ ] Every response carries provider, provider version, retrieved-at and source-observed-at so the client can show cache age.
- [ ] Every provider-bound call has a correlation id, carried into responses and problem details.
- [ ] Registration normalisation exists in exactly one place — the Core request type.
- [ ] No DVLA or DVSA key, secret or token appears in a response, a log or any desktop-consumed project.
- [ ] The replay adapter drives every test; no live provider call is made.

## Verification

- [ ] `dotnet test ./tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj --configuration Release` — expected: the seven-outcome, correlation-id and no-credential facts pass.
- [ ] `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --filter "Category!=Corpus&Category!=Browser"` — expected: replay-adapter facts pass and the existing vehicle tests stay green.
- [ ] `grep -rn "Dvla__\|Dvsa__\|x-api-key" src/Pegasus.Contracts src/Pegasus.Desktop src/Pegasus.Desktop.Infrastructure` — expected: no matches.

## Evidence tier

Tier 5 — Web/API/MCP caller.
Tier 5 obliges route-level evidence that the real endpoints reach Core and the adapter with authorization, validation, idempotency and exception translation observable — with the provider outcome taxonomy preserved end to end.

## Documentation changes

- `docs/desktop/03-gateway-api-and-data/endpoint-map.md` — vehicle rows amended with the provenance fields
- `docs/frd/frd-06-vehicle-and-engineering-evidence.md` — desktop behaviour clause for provenance and provider states

## Guardrails

- **Azure**: no write. Key Vault reads are name-only and need no approval (`docs/runbook.md` § Live-operation approval matrix; mirrored in `docs/desktop/11-azure-disposition/README.md`).
- **Scope boundary**: may touch `src/Pegasus.Web` (`/api/v1` vehicle group), `src/Pegasus.Contracts` and the test projects. Must not modify `DvlaDvsaProductionAdapter.cs` behaviour, must not add a provider HTTP client to any desktop project.
- **Traps**: ADR-0107 — a provider key in the desktop package or in a log is a defect; `unknown` and `failed` must never collapse into `notFound`; the release-15 automatic sweep already issues lookups, so a desktop request must stay idempotent per case and registration or it will duplicate provider calls; raw provider JSON must not reach the client; a new table would need a runtime-role `Grant*` migration checked by `scripts/Test-MigrationGrants.ps1`.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.

## Outcome

_Filled at closeout._
