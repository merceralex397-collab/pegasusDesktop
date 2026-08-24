---
id: FEAT-015
type: ticket
title: DSK-05-15 · S15 Vehicle lookup and EVA handoff
status: backlog
area: desktop-features
assignee: ''
profile: feature
labels:
  - desktop-conversion
  - plan-05
  - phase-6
  - tier-5
  - tier-7
groups:
  - EPIC-006
  - HZN-007
links: []
blocks:
  - FEAT-022
  - FEAT-025
  - FEAT-044
  - TEST-008
  - TEST-016
refs:
  - docs/frd/frd-06-vehicle-and-engineering-evidence.md
  - docs/frd/frd-07-eva-and-external-engineering-handoff.md
docs_todo: true
archived: false
created: '2026-08-24T07:54:27.572Z'
updated: '2026-08-24T11:03:35.932Z'
---

## What

Deliver the case Vehicle tab: request a DVLA/DVSA lookup on a normalized registration, accept a suggestion with its source and timestamp, distinguish a provider failure from a not-found, show cached-lookup freshness, and generate and download the EVA handoff bundle as explicit commands.

## Why

Proposal §12.3 and §13.5 require vehicle identity, lookups, mileage history and the source and timestamp of external data, with provider secrets absent from the client. Today it is `src/Pegasus.Web/Pages/Cases/Vehicle.cshtml.cs` (149 lines, three handlers at `:24`, `:46`, `:87`) and `Pages/Cases/Eva/Download.cshtml.cs` (99 lines) over Core `src/Pegasus.Core/Vehicle/` and `src/Pegasus.Core/Eva/EvaBundleSchema.cs`, with the provider adapters and their replay variant in `src/Pegasus.Infrastructure/Vehicle/`. The Phase 6 exit gate requires provider rate and error handling to pass with no secret in the package. Siblings: [[DSK-05-05]] supplies the case session, [[DSK-07-09]] the gateway endpoints, [[DSK-07-19]] the provider error taxonomy, and [[DSK-07-10]] owns the Vehicle tab itself — `CaseVehicleViewModel` and `CaseVehicleView.xaml`.

## Source of truth

- Plan row: `docs/desktop/05-implementation-and-migration/README.md` § 5 — `DSK-05-15`
- Plan detail: `docs/desktop/05-implementation-and-migration/vertical-slices.md` § `S15 · Vehicle lookup and EVA handoff (DSK-05-15)`
- Endpoint map: `docs/desktop/03-gateway-api-and-data/endpoint-map.md` § `Cases` (Vehicle and EVA rows)
- Screen spec: `docs/desktop/06-ui-design/screen-specs.md` § `§13.5 Vehicle and inspection information — Case workspace › Vehicle tab`
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 12.3 DVLA/DVSA, § 13.5 Vehicle and inspection information, § 16.2 External provider resilience
- Repository evidence: `src/Pegasus.Web/Pages/Cases/Vehicle.cshtml.cs:24` (`OnPostRequestVehicleLookupAsync`), `:46` (`OnPostAcceptVehicleSuggestionAsync`), `:87` (`OnPostGenerateEvaHandoffAsync`); `src/Pegasus.Web/Pages/Cases/Eva/Download.cshtml.cs`; `src/Pegasus.Core/Vehicle/` (lookup contracts, work items, mileage policy, request→accept workflow), `src/Pegasus.Core/Eva/EvaBundleSchema.cs` (916 lines), `CaseEvaMapping`; `src/Pegasus.Infrastructure/Vehicle/DvlaDvsaProductionAdapter.cs` (412 lines) and `DvlaDvsaAdapters.cs` (222 lines, includes the replay adapter used by the Test/UAT stack)
- Known-good EVA corpus (operator-supplied, in the repository): `reference/eva_information/AX_SP58WVO.json` (the thirteen keys in order, 2-space indented, no companion files), `reference/eva_information/Final Format Example 02.json`, `reference/eva_information/eva_information.md:31-45` (`Case/Po` is our reference, `Claim no` is the work provider's)
- Upstream evidence: `ENG-014` (the invented `manifest.sha256` and `provenance.json` stop being produced and the JSON is indented) and `ENG-015` (four of the thirteen exported values are wrong: `Reference` must carry the work provider's claim number, `Inspection Address` must be a six-line block, `Vehicle Model` must carry the make, `Mileage Unit` casing) — both raised from the `ap.QDOS26015` export EVA refused on 2026-08-24
- Binding decisions: L-01 the gateway holds the provider keys and the shared lookup cache; L-02 the Test/UAT stack uses the replay adapter, never a live provider call; L-04 routing named on the ticket; ADR-0107 consumed
- Depends on: `DSK-05-05` the case lease and version session; `DSK-07-09` the DVLA/DVSA request, accept and status endpoints with cache lifetime and provenance fields; `DSK-07-10` — owns `CaseVehicleViewModel` and `CaseVehicleView.xaml`; this slice adds the lookup-status refresh and the EVA handoff generate and download commands to them; `DSK-01-09` recreates upstream `ENG-014` and `ENG-015` as fork tickets, which own the mapping fix this ticket's bundle-content gate detects

## Routing

- **Subagent**: `winui-dev` — `.codex/agents/winui-dev.toml`; `pegasus-gateway-dev` — `.codex/agents/pegasus-gateway-dev.toml`; `pegasus-test-engineer` — `.codex/agents/pegasus-test-engineer.toml`
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `dotnet-webapi` (dotnet/skills `98f84851`) → `winui-dev-workflow` (`.codex/skills/winui-dev-workflow/SKILL.md`) → `winui-design` (`.codex/skills/winui-design/SKILL.md`) → `run-tests` → `winui-code-review` at review
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search`)
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (call `get_doc_gates <id>` before every move; a move crosses at most one gated boundary)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Orient: read the plan row, `vertical-slices.md` § S15, the screen spec Vehicle section, `docs/frd/frd-06-vehicle-and-engineering-evidence.md` and `docs/frd/frd-07-eva-and-external-engineering-handoff.md`. Call `get_doc_gates <this ticket id>`, then `take_ticket` with branch `task/dsk-05-15-vehicle-eva` and worktree `../pegasus-worktrees/dsk-05-15-vehicle-eva` from `origin/dev`.
2. Read `src/Pegasus.Web/Pages/Cases/Vehicle.cshtml.cs` and `Pages/Cases/Eva/Download.cshtml.cs` in full. Record in `research`: how the lookup request becomes a durable work item the Worker executes, what the accept command writes, the registration normalization rule and where it lives in `src/Pegasus.Core/Vehicle/`, the mileage policy inputs, and the reason the EVA download requires. Record the SHA read.
3. Confirm the endpoints from [[DSK-07-09]] and the endpoint map: `POST /api/v1/cases/{id}/vehicle/lookups`, `POST /api/v1/cases/{id}/vehicle/suggestions/{sid}/accept`, `POST /api/v1/cases/{id}/eva-handoff` and `GET /api/v1/cases/{id}/eva-handoff/{revision}/bundle`. Confirm the response carries the cache lifetime and the provenance fields (source and obtained-at) the screen must show.
4. Confirm the provider error taxonomy from [[DSK-07-19]] is on these endpoints: `terminal` / `transient` / `unknown` alongside `not-found`, `invalid-request`, `not-authorized`, `rate-limited`, `unavailable`. A provider failure must be distinguishable from a genuine not-found in the contract, not inferred by the client.
5. Add the vehicle and EVA DTOs to `src/Pegasus.Contracts`, including the suggestion with its source and timestamp and the handoff revision identifier.
6. Implement registration normalization in the desktop by calling the **existing** Core rule from `src/Pegasus.Core/Vehicle/` — the boundary note in `reuse-map.md` permits a direct `Pegasus.Core` reference for deterministic validation. Do not write a second normalizer; the gateway re-checks on write.
7. Check whether `CaseVehicleViewModel` already exists from [[DSK-07-10]], which owns that type and its view. If it does, add the lookup-status refresh and the EVA handoff generate and download commands to it in place and change no existing member; if it has not landed, create it with exactly the members [[DSK-07-10]] step 5 pins (`ObservableObject`, `[ObservableProperty]` partial properties, `[RelayCommand]`, and the shared Core normalisation rule reused rather than a second copy) and record in the plan document which case applied. Either way this slice's own surface is the same: request lookup, poll or refresh status, accept a suggestion (showing source and obtained-at beside the value), and render each provider state distinctly using the shared vocabulary — never one generic "failed". Never a second view model for the Vehicle tab.
8. Show cached-lookup freshness explicitly using the header control from [[DSK-06-12]], so an operator can tell a fresh answer from a cached one without hovering.
9. Implement EVA handoff generate and download as explicit commands; the download is a streamed transfer reusing the service from [[DSK-05-14]] and carries the reason the Core download requires.
10. Assert the bundle's CONTENT, not only that the two commands run — the slice is otherwise able to sign off on a package EVA rejects, which is exactly what upstream hit on 2026-08-24 exporting `ap.QDOS26015`. Generate a bundle on the local Test/UAT stack from the seeded case and add a test in `tests/Pegasus.Api.ContractTests` that pins two things against the operator-supplied corpus: (a) the archive's entry list — the thirteen-key JSON plus `Images/` and nothing else — and the JSON's layout, two-space indentation with the same key set and key order, diff clean against `reference/eva_information/AX_SP58WVO.json`; and (b) the thirteen field values — `Work Provider`, `VRM`, `Vehicle Model`, `Claimant Name`, `Reference`, `Incident Date`, `Instruction Date`, `Inspection Date`, `Inspection Address`, `Accident Circumstances`, `VAT Status`, `Mileage`, `Mileage Unit` — match the known-good samples `reference/eva_information/AX_SP58WVO.json` and `reference/eva_information/Final Format Example 02.json`, with `Reference` carrying the work provider's claim number rather than our case reference, `Inspection Address` carrying exactly six lines, and `Vehicle Model` carrying make and model. Record the run in the proof. If the assertion fails, the fix belongs to upstream `ENG-014` (packaging and indentation) and `ENG-015` (field values) in `src/Pegasus.Core/Eva/` and `src/Pegasus.Infrastructure/Persistence/EvaHandoffStore.cs`, recreated as fork tickets by [[DSK-01-09]] and sequenced `ENG-014` then `ENG-015` so the archive bytes change once — raise it, do not write a second EVA mapping in the desktop or the gateway.
11. Add contract tests in `tests/Pegasus.Api.ContractTests` using the replay adapter from `src/Pegasus.Infrastructure/Vehicle/DvlaDvsaAdapters.cs`: success, not-found, each provider failure class, rate-limited, 401, 403, 409 stale version, replay of the same `operationKey`, and an assertion that no provider key appears in any response. Enable `Features:DesktopGateway` explicitly.
12. Add view-model tests in `tests/Pegasus.Desktop.ViewModelTests` for normalization, each provider state rendering distinctly, freshness display, accept updating the case version, and EVA generate-then-download.
13. Run the replay-adapter integration check on the local Test/UAT stack per `docs/desktop/08-testing/test-uat-stack.md` and record in the proof that no live provider call was made.
14. Update `docs/desktop/01-inventory-and-parity/parity-matrix.md` row `PAR-14`, add the EVA handoff behaviour inside the Vehicle tab section [[DSK-07-10]] creates in `docs/frd/frd-13-desktop-operator-experience.md` (a sub-heading under that section, not a second vehicle section), run the simplification pass over the branch diff, record it under a dated `## Simplification pass` heading, then open the PR into `dev`.

## Acceptance criteria

- [ ] A lookup can be requested on a normalized registration and its status followed; the normalization rule has one implementation, in Core.
- [ ] A provider failure is visibly distinct from a not-found, and rate-limited and unavailable states are distinguishable.
- [ ] An accepted suggestion shows its source and timestamp beside the value.
- [ ] Cached-lookup freshness is visible without hovering.
- [ ] The EVA bundle can be generated and downloaded as explicit commands, with the reason Core requires.
- [ ] The generated EVA bundle's entry list and JSON layout diff clean against `reference/eva_information/AX_SP58WVO.json`, and its thirteen field values match the known-good samples.
- [ ] No provider secret appears in the package, a response body or a log; the Test/UAT run uses the replay adapter only.

## Verification

- [ ] `dotnet test ./tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj --configuration Release --no-build` — expected: lookup, accept and EVA facts pass across the full provider error taxonomy with the replay adapter.
- [ ] `dotnet test ./tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj --configuration Release --no-build --filter "FullyQualifiedName~EvaBundleContent"` — expected: the generated bundle's entry list and JSON layout diff clean against `reference/eva_information/AX_SP58WVO.json` and the thirteen field values match the known-good samples.
- [ ] `dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release --no-build` — expected: normalization, provider-state, freshness and EVA facts pass.
- [ ] `dotnet test ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj --configuration Release --no-build` — expected: the desktop references no provider adapter and no second normalizer exists.
- [ ] Test/UAT record in the ticket proof — expected: replay adapter used, no live provider call, no key in package or logs.

## Evidence tier

Tier 5 — Web/API/MCP caller. Tier 7 — Browser/accessibility.
Tier 5 obliges route-level evidence that the lookup and handoff endpoints reach Core and the provider port with authorization, idempotency and deterministic external-failure translation; tier 7 obliges keyboard, focus, semantic-label and text-plus-colour evidence for the provider states from a real run.

## Documentation changes

- `docs/desktop/01-inventory-and-parity/parity-matrix.md` — row `PAR-14`. Row `PAR-18` must record that EVA parity covers the bundle's CONTENT — entry list, JSON layout and the thirteen field values — and not only the download command and frozen revisions; [[DSK-01-05]] owns that row and writes it, this ticket supplies the evidence and does not edit the row itself
- `docs/frd/frd-13-desktop-operator-experience.md` — the EVA handoff behaviour inside the Vehicle tab section [[DSK-07-10]] creates, citing FRD-06 and FRD-07; this ticket adds no second vehicle section
- `docs/capabilities.md` — `DSK` rows for vehicle lookup and EVA handoff

## Guardrails

- **Azure**: no write.
- **Scope boundary**: may extend `CaseVehicleViewModel` and `CaseVehicleView.xaml` in `src/Pegasus.Desktop` — [[DSK-07-10]] owns both and this slice adds members to them rather than creating its own — and may touch `src/Pegasus.Desktop.Infrastructure`, `src/Pegasus.Contracts`, the `/api/v1` vehicle and EVA groups in `src/Pegasus.Web` and the test projects. Must not reference `src/Pegasus.Infrastructure/Vehicle/` from the desktop; the desktop never calls a provider directly. Must not change `src/Pegasus.Core/Eva/EvaBundleSchema.cs`, `CaseEvaMapping` or `EvaHandoffStore.cs` — this ticket asserts the bundle's content, upstream `ENG-014` and `ENG-015` fix it.
- **Traps**: DVLA/DVSA keys stay behind the gateway (ADR-0107); the Test/UAT stack uses the replay adapter and asking for a live provider or an Azure test resource is out of bounds (L-02, ADR-0014); one normalization rule only — a second implementation is a stop condition; upstream ENG-013 arrives via upstream sync and ENG-009 (Cazana valuation) stays backlog and must not be pulled in; `Features:DesktopGateway` must be enabled in tests. One view model per screen: [[DSK-07-10]] owns `CaseVehicleViewModel`, this ticket extends it; a second view model for the same screen is a stop condition. `reuse-map.md` marks `Eva/` REUSE, so the desktop ships byte-identical output — a bundle that passes every other gate here and is still refused by EVA is the `ap.QDOS26015` failure repeating, which is why the content assertion is a gate rather than an assumption; neither `ENG-014` (upstream branch `task/eng-014-drop-manifest-indent-json` against `dev`, not in [[DSK-01-10]]'s 32-commit `main` range) nor `ENG-015` (no upstream branch at all) arrives by sync, so under D-001 they exist only if the fork board holds them.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.

## Outcome

_Filled at closeout._
