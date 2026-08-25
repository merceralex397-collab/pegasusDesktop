---
id: INTK-003
type: ticket
title: 'upstream:INTK-026 · Normalize kilometre case mileage to canonical miles'
status: implementing
area: intake-processing
assignee: codex-mcp-client
profile: feature
stageEntered:
  preparing: '2026-08-24T21:23:32.455Z'
taken_at: '2026-08-25T20:14:25.093Z'
branch: task/upstream-intk-026-canonical-miles
worktree: ../pegasus-worktrees/upstream-intk-026-canonical-miles
labels:
  - vehicle
  - mileage
  - normalisation
  - case-data
  - upstream-carryover
  - upstream-INTK-026
groups:
  - EPIC-014
links: []
blocks:
  - FEAT-004
  - FEAT-005
  - FEAT-036
refs:
  - docs/frd/frd-06-vehicle-and-engineering-evidence.md
docs_todo: true
commits:
  - b8970c51
prs:
  - 'https://github.com/merceralex397-collab/pegasusDesktop/pull/12'
archived: false
created: '2026-08-24T11:47:12.089Z'
updated: '2026-08-25T20:35:47.229Z'
---

## What

Convert documented kilometre mileage to canonical miles at new-case creation and at every later case-data write, using 1 km = 0.6213711922 miles rounded to the nearest whole mile with midpoints away from zero; retain the typed original-kilometre value as provenance and show it as a compact marker beside the canonical miles figure; treat a missing documented unit as miles; and transform no existing persisted case.

## Why

This is a Core case-data rule with an operator-visible provenance marker, and the desktop conversion is the moment it becomes load-bearing. `src/Pegasus.Core/Cases/CaseDataContracts.cs:75-79` already carries `CaseField<long> Mileage` beside `CaseField<string> MileageUnit`, and `CaseDataOperations.Normalize` (`src/Pegasus.Core/Cases/CaseDataOperations.cs:121-160`) — the single place both create and later writes pass through — does nothing with the unit except refuse a negative mileage and truncate the unit string to 40 characters. Today the consequence is hidden by the Razor front end: `src/Pegasus.Web/Pages/Cases/Assessment/Index.cshtml.cs:100-121` prefills mileage only when `IsMiles` (`:165-166`) says so, and `IsMiles` treats a null unit as miles — so a kilometre case silently produces **no** mileage prefill rather than a wrong one. [[DSK-05-17]] converts that exact page model and [[DSK-07-10]] rebuilds the vehicle workflow with provenance display; neither is told about the unit, so the desktop would either inherit the silent drop or, worse, show a kilometre number in a miles-labelled field.

It is also the point where the number leaves the building: `src/Pegasus.Core/Eva/CaseEvaMapping.cs:52-53`, `:152-153` and `:191` export `Mileage` and `Mileage Unit` into the EVA bundle that `docs/desktop/05-implementation-and-migration/reuse-map.md` marks REUSE, so whatever this rule decides is what an external engineering firm receives.

No board ticket covers it and no register holds it. The carry-over disposition is `unchanged-backlog`, which `docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md` § Disposition categories justifies on the grounds that "their capability rows stay in `docs/capabilities.md`" — and a grep of `docs/capabilities.md` returns **no row** for upstream INTK-026. It carries no `capability`, `post-alpha` or `blocked` label upstream, so filing it as unchanged backlog would silently drop a live requirement. Under **L-05** the fork board is the single work register, which is why it is imported rather than left in a table.

## Source of truth

- Import decision: `coverage-decision.md` § Import list — the row for upstream `INTK-026` (this ticket; board `INTK-003`); § Plan gaps — "Three server-side domain requirements have no register at all: `unchanged-backlog` is only safe for rows that have a `docs/capabilities.md` row, and these have none"
- Carry-over register: `docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md:155` — the row for upstream `INTK-026`, quoted as it stands (its first cell is an upstream id): `INTK-026 | intake-processing | backlog | feature | vehicle, mileage, normalisation, case-data | … | unchanged-backlog | — | intake-processing`
- Governing document: `docs/frd/frd-06-vehicle-and-engineering-evidence.md` (the upstream ticket's own `refs`)
- Repository evidence (fork `main`, read 2026-08-24):
  - `src/Pegasus.Core/Cases/CaseDataContracts.cs:75-79` — `CaseVehicleData` with `CaseField<long> Mileage` and `CaseField<string> MileageUnit`; `:131-132` — `CaseEditableData.VehicleMileage` / `VehicleMileageUnit`
  - `src/Pegasus.Core/Cases/CaseDataOperations.cs:121-160` — `Normalize`, the one place every write passes through; `:124-129` the negative-mileage refusal, `:150` the 40-character unit truncation. This is where the conversion belongs.
  - `src/Pegasus.Core/Vehicle/LookupContracts.cs:14-18` — `enum VehicleMileageUnit { Miles, Kilometres }`, the existing named vocabulary; do not invent a second
  - `src/Pegasus.Web/Pages/Cases/Assessment/Index.cshtml.cs:100-121` and `:165-166` — `MileagePrefill` and `IsMiles`, which already encode "a missing unit is miles" and silently drop a kilometre value
  - `src/Pegasus.Web/Pages/Cases/Details.cshtml.cs:336`, `:553`, `:573`, `:596` and `src/Pegasus.Web/Pages/Cases/CaseMutationPageModel.cs:54`, `:70` — the `vehicleMileageUnit` / `mileageUnit` field names and the "Mileage unit" operator label carried today
  - `src/Pegasus.Core/Eva/CaseEvaMapping.cs:52-53`, `:152-153`, `:191` — `Mileage` and `Mileage Unit` in the exported bundle
  - `src/Pegasus.Core/Assessment/AssessmentContracts.cs:40`, `:78` and `src/Pegasus.Core/Assessment/AssessmentPolicy.cs:192`, `:215-225` — `vehicle.mileage_source` and the completeness rule that refuses without a confirmed mileage unless the source is `tbc`
  - `tests/Pegasus.Core.Tests/` — where the rounding, boundary, missing-unit and provenance facts land
- Binding decisions: **L-01** the gateway is `Pegasus.Web` evolved in place, so the converted value reaches the desktop through `/api/v1` rather than a second rule; **L-02** verification is the local production-mimicking stack; **L-05** the fork board is the single work register; **D-001** upstream is frozen after the final sync, so nobody upstream will do this
- Depends on: `DSK-01-10` — the first one-way upstream sync, before editing case-data paths
- Upstream `blocks: ENG-008`: upstream ENG-008 is a post-alpha provider backend (Cazana) that `coverage-decision.md` § Drop list excludes from the conversion, and it has **no fork ticket** — board `ENG-001` and board `ENG-002` are upstream ENG-014 and upstream ENG-015, neither of which is this. The blocking relationship is recorded here for provenance and is **not** recreated on the fork board.

### Upstream ticket INTK-026 (verbatim)

Provenance — upstream area `intake-processing`; upstream status `backlog`; upstream profile `feature`; upstream labels `vehicle`, `mileage`, `normalisation`, `case-data`; upstream `blocks` `ENG-008`; upstream `refs` `docs/frd/frd-06-vehicle-and-engineering-evidence.md`. Read from the read-only clone of `collisionengineers/pegasus` branch `kanmer-board` at commit `a5b28111`, read date **2026-08-24**. Copied unedited.

````
## Why

Pegasus must retain documented kilometre mileage faithfully while presenting canonical miles for case work and downstream valuation.

## Scope

- Convert kilometre mileage at new-case creation and later case-data writes using (1 km = 0.6213711922 miles), rounded to the nearest whole mile with midpoint values away from zero.
- Preserve typed original-kilometre provenance and display it as a compact marker beside the canonical miles value.
- Treat a missing documented unit as miles.
- Do not add a legacy conversion, batch, or read fallback for existing cases.

## Verification

- Tests cover kilometre conversion, rounding boundaries, missing-unit miles, provenance, and miles-first rendering.
- No existing persisted case is transformed.
- Blocks [[ENG-008]] so Cazana receives canonical case mileage.
````

## Routing

- **Subagent**: `pegasus-gateway-dev` — `.codex/agents/pegasus-gateway-dev.toml` (Core rule and gateway projection); `winui-dev` — `.codex/agents/winui-dev.toml` (the provenance marker on the desktop side, once [[DSK-05-04]] and [[DSK-07-10]] exist); tests by `pegasus-test-engineer` — `.codex/agents/pegasus-test-engineer.toml`
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `code-testing-agent` (dotnet/skills `98f84851`, `plugins/dotnet-test/skills/code-testing-agent/SKILL.md`) → `run-tests` (dotnet/skills `98f84851`) → `test-gap-analysis` (dotnet/skills `98f84851`) → `winui-design` (`.codex/skills/winui-design/SKILL.md`) for the marker's placement only
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search` for `MidpointRounding.AwayFromZero` and `decimal`/`double` rounding semantics)
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (call `get_doc_gates <id>` before every move; a move crosses at most one gated boundary)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Orient. Read the verbatim upstream body above, `docs/frd/frd-06-vehicle-and-engineering-evidence.md`, and `coverage-decision.md` § Import list row for upstream `INTK-026`. Call `get_doc_gates <this ticket id>`, then `take_ticket` with branch `task/upstream-intk-026-canonical-miles` and worktree `../pegasus-worktrees/upstream-intk-026-canonical-miles` from `origin/dev`.
2. In `research`, enumerate every writer and reader of `VehicleMileage` / `VehicleMileageUnit`: `CaseDataOperations.Normalize` (`src/Pegasus.Core/Cases/CaseDataOperations.cs:121-160`), `CaseDataContracts.cs:75-79` and `:131-132`, the assessment prefill (`src/Pegasus.Web/Pages/Cases/Assessment/Index.cshtml.cs:100-121`, `:165-166`), the details field map (`src/Pegasus.Web/Pages/Cases/Details.cshtml.cs:336`, `:553`, `:573`, `:596`), the MCP tool (`src/Pegasus.Web/Mcp/AssessmentMcpTools.cs:280`, `:320`, `:513`) and the EVA mapping (`src/Pegasus.Core/Eva/CaseEvaMapping.cs:52-53`, `:152-153`, `:191`). Record which are writes and which are reads — the rule goes on the write path only.
3. Put the conversion in `CaseDataOperations.Normalize`, the single place both create and later writes pass through. Reuse the existing `VehicleMileageUnit` vocabulary (`src/Pegasus.Core/Vehicle/LookupContracts.cs:14-18`) rather than inventing a second unit list, and reuse the existing "a missing unit is miles" reading that `IsMiles` already encodes at `src/Pegasus.Web/Pages/Cases/Assessment/Index.cshtml.cs:165-166`. Use the constant `0.6213711922` and `MidpointRounding.AwayFromZero` exactly as the upstream Scope states — do not substitute a different factor or rounding mode.
4. Preserve the typed original-kilometre value as provenance on the case data rather than overwriting it. Decide **and record in the `plan`** whether that is a new `CaseField` on `CaseVehicleData` or a provenance record beside it; if it is a new persisted column, the migration needs a runtime-role `Grant*` entry checked by `scripts/Test-MigrationGrants.ps1` — say so in the `plan` before writing it.
5. Add the Core facts in `tests/Pegasus.Core.Tests/`: kilometre conversion, the rounding boundary in both directions with a midpoint value, a missing unit treated as miles, a unit already miles left untouched, the retained provenance, and a negative or absurd value still refused by the existing `:124-129` guard.
6. **Re-expressed for the desktop world.** The upstream Scope says the provenance marker is "displayed… beside the canonical miles value" — which upstream means on the Razor case-details page that [[DSK-05-26]]'s cut list deletes. Express the same requirement against the surfaces that replace it and record it in the `plan`: the miles value and its compact kilometre marker are carried in the case DTO the gateway returns, so that [[DSK-05-04]] (create), [[DSK-05-05]] (edit) and [[DSK-07-10]] (vehicle workflow provenance display) all render one value from one source and neither converts a second time. The marker is a value in the payload, never a client-side calculation.
7. Project the canonical miles and the marker through the gateway case contracts rather than a new endpoint: the case sections are [[DSK-03-07]]'s `GET /api/v1/cases/{id}/vehicle`, and the save path is [[DSK-03-08]]'s `PUT /api/v1/cases/{id}`. Record in the `plan` that no new route is added by this ticket.
8. Check the EVA consequence before changing anything: `src/Pegasus.Core/Eva/CaseEvaMapping.cs` exports both `Mileage` and `Mileage Unit`. Record in the `plan` what an EVA bundle carries after this change (a miles value with a miles unit, plus whatever the provenance marker does or does not add) and cross-reference the imported `upstream:ENG-014` and `upstream:ENG-015` tickets, which own bundle content — this ticket must not change the bundle's field list on its own.
9. Confirm the assessment completeness rule still holds: `src/Pegasus.Core/Assessment/AssessmentPolicy.cs:215-225` refuses without a confirmed mileage unless `vehicle.mileage_source` is `tbc`. A kilometre case that now yields a confirmed miles figure changes that outcome — add a fact for it and record the change in the `plan`.
10. Assert the no-migration rule explicitly: add a test or an architecture fact proving no existing persisted case is transformed, and confirm the branch adds no data migration, batch job or read-time fallback, per the upstream Scope's fourth bullet.
11. Add the FRD-06 sentence stating the canonical-miles rule and the retained kilometre provenance, and add a `docs/capabilities.md` row so the requirement finally has a register (its absence is why this ticket exists).
12. Run the simplification pass over the branch diff, record it under a dated `## Simplification pass` heading in the ticket `plan`, then open the PR into `dev`.

## Acceptance criteria

- [ ] A case created or saved with a kilometre mileage stores canonical miles, converted with `0.6213711922` and `MidpointRounding.AwayFromZero`, in `CaseDataOperations.Normalize` and nowhere else.
- [ ] The typed original kilometre value is retained as provenance and reaches the gateway case payload as a compact marker, so [[DSK-05-04]], [[DSK-05-05]] and [[DSK-07-10]] render one value from one source and none converts a second time.
- [ ] A missing documented unit is treated as miles, matching the reading `IsMiles` already encodes.
- [ ] No existing persisted case is transformed: no data migration, no batch, no read-time fallback.
- [ ] Rounding boundaries in both directions and a midpoint value are asserted in `tests/Pegasus.Core.Tests/`.
- [ ] The EVA bundle consequence is recorded in the `plan`, and this ticket changes no EVA field list on its own.

## Verification

- [ ] `dotnet build --configuration Release` — expected: clean.
- [ ] `dotnet test ./tests/Pegasus.Core.Tests/Pegasus.Core.Tests.csproj --configuration Release` — expected: conversion, rounding boundaries, midpoint, missing-unit-is-miles, provenance retention and the unchanged negative-value refusal all pass.
- [ ] `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --filter "Category!=Corpus&Category!=Browser"` — expected: existing case-data and assessment suites stay green, including the completeness rule.
- [ ] `pwsh ./scripts/Test-MigrationGrants.ps1` — expected: passes; run only if step 4 adds a persisted column, and record the result either way.

## Evidence tier

Tier 2 — Core/domain. Tier 4 — LocalDB persistence.
Tier 2 obliges positive, boundary, missing-value and failure cases for the conversion and the provenance rule; tier 4 obliges evidence that no existing persisted case is transformed and that any new column round-trips with its constraint and grant.

## Documentation changes

- `docs/frd/frd-06-vehicle-and-engineering-evidence.md` — the canonical-miles rule, the retained kilometre provenance and the missing-unit reading
- `docs/capabilities.md` — add the row this requirement has never had; its absence is what made `unchanged-backlog` unsafe for it
- `docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md` — annotate the upstream `INTK-026` row with this fork ticket id (`INTK-003`)

## Guardrails

- **Azure**: no write.
- **Scope boundary**: may touch `src/Pegasus.Core/Cases/`, `src/Pegasus.Core/Vehicle/LookupContracts.cs` (read only — reuse the enum, do not redefine it), `tests/Pegasus.Core.Tests/`, `tests/Pegasus.IntegrationTests/`, `docs/frd/frd-06-vehicle-and-engineering-evidence.md` and `docs/capabilities.md`. Must **not** touch `src/Pegasus.Core/Eva/` (bundle content belongs to the imported `upstream:ENG-014` and `upstream:ENG-015`), any Razor page under `src/Pegasus.Web/Pages/**` beyond reading it, or any desktop project.
- **Unblocks / blocked by**: this ticket **blocks** [[DSK-05-04]] and [[DSK-05-05]] (both write `CaseEditableData` and would persist an unconverted kilometre value) and [[DSK-07-10]] (which displays vehicle provenance and would show a kilometre number in a miles field). It is **blocked by** [[DSK-01-10]], the first one-way upstream sync. [[DSK-05-03]] displays the same value read-only — coordinate the marker's presentation with it and with [[DSK-06-16]]'s screen specification rather than inventing a second rendering.
- **Traps**: the conversion belongs on the write path in one place — a read-time conversion would be a second owner of the rule and is a stop condition. Do not add a legacy batch or fallback; the upstream Scope forbids it and a silent retro-transformation is unrecoverable. `MileageUnit` is a persisted string today and `VehicleMileageUnit` is an enum — do not conflate them without recording the mapping. `docs/capabilities.md` has no upstream INTK-026 row today; adding one is part of this ticket, not optional. **Upstream ids and fork board ids do not match**: this ticket is board `INTK-003` and it is upstream INTK-026, while upstream INTK-003 is board [[INTK-002]], a different ticket. Read the join table in `HZN-001/board-conventions.md` § Upstream ids versus board ids; never compute the mapping.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the ticket `plan` document.

## Outcome

_Filled at closeout._
