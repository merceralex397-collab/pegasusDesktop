# Research — INTK-003: upstream:INTK-026 · Normalize kilometre case mileage to canonical miles

## Question

Convert documented kilometre mileage to canonical miles at new-case creation and at every later case-data write, using 1 km = 0.6213711922 miles rounded to the nearest whole mile with midpoints away from zero; retain the typed original-kilometre value as provenance and show it as a compact marker beside the canonical miles figure; treat a missing documented unit as miles; and transform no existing persisted case.

## Evidence examined

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

## Scope and constraints

This is a Core case-data rule with an operator-visible provenance marker, and the desktop conversion is the moment it becomes load-bearing. `src/Pegasus.Core/Cases/CaseDataContracts.cs:75-79` already carries `CaseField<long> Mileage` beside `CaseField<string> MileageUnit`, and `CaseDataOperations.Normalize` (`src/Pegasus.Core/Cases/CaseDataOperations.cs:121-160`) — the single place both create and later writes pass through — does nothing with the unit except refuse a negative mileage and truncate the unit string to 40 characters. Today the consequence is hidden by the Razor front end: `src/Pegasus.Web/Pages/Cases/Assessment/Index.cshtml.cs:100-121` prefills mileage only when `IsMiles` (`:165-166`) says so, and `IsMiles` treats a null unit as miles — so a kilometre case silently produces **no** mileage prefill rather than a wrong one. [[DSK-05-17]] converts that exact page model and [[DSK-07-10]] rebuilds the vehicle workflow with provenance display; neither is told about the unit, so the desktop would either inherit the silent drop or, worse, show a kilometre number in a miles-labelled field.

It is also the point where the number leaves the building: `src/Pegasus.Core/Eva/CaseEvaMapping.cs:52-53`, `:152-153` and `:191` export `Mileage` and `Mileage Unit` into the EVA bundle that `docs/desktop/05-implementation-and-migration/reuse-map.md` marks REUSE, so whatever this rule decides is what an external engineering firm receives.

No board ticket covers it and no register holds it. The carry-over disposition is `unchanged-backlog`, which `docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md` § Disposition categories justifies on the grounds that "their capability rows stay in `docs/capabilities.md`" — and a grep of `docs/capabilities.md` returns **no row** for upstream INTK-026. It carries no `capability`, `post-alpha` or `blocked` label upstream, so filing it as unchanged backlog would silently drop a live requirement. Under **L-05** the fork board is the single work register, which is why it is imported rather than left in a table.

- Future owner: Core/Infrastructure intake work with focused Core and integration tests. The local Test/UAT stack is the verification environment; no production intake or Azure write is authorized.

- **Azure**: no write.
- **Scope boundary**: may touch `src/Pegasus.Core/Cases/`, `src/Pegasus.Core/Vehicle/LookupContracts.cs` (read only — reuse the enum, do not redefine it), `tests/Pegasus.Core.Tests/`, `tests/Pegasus.IntegrationTests/`, `docs/frd/frd-06-vehicle-and-engineering-evidence.md` and `docs/capabilities.md`. Must **not** touch `src/Pegasus.Core/Eva/` (bundle content belongs to the imported `upstream:ENG-014` and `upstream:ENG-015`), any Razor page under `src/Pegasus.Web/Pages/**` beyond reading it, or any desktop project.
- **Unblocks / blocked by**: this ticket **blocks** [[DSK-05-04]] and [[DSK-05-05]] (both write `CaseEditableData` and would persist an unconverted kilometre value) and [[DSK-07-10]] (which displays vehicle provenance and would show a kilometre number in a miles field). It is **blocked by** [[DSK-01-10]], the first one-way upstream sync. [[DSK-05-03]] displays the same value read-only — coordinate the marker's presentation with it and with [[DSK-06-16]]'s screen specification rather than inventing a second rendering.
- **Traps**: the conversion belongs on the write path in one place — a read-time conversion would be a second owner of the rule and is a stop condition. Do not add a legacy batch or fallback; the upstream Scope forbids it and a silent retro-transformation is unrecoverable. `MileageUnit` is a persisted string today and `VehicleMileageUnit` is an enum — do not conflate them without recording the mapping. `docs/capabilities.md` has no upstream INTK-026 row today; adding one is part of this ticket, not optional. **Upstream ids and fork board ids do not match**: this ticket is board `INTK-003` and it is upstream INTK-026, while upstream INTK-003 is board [[INTK-002]], a different ticket. Read the join table in `HZN-001/board-conventions.md` § Upstream ids versus board ids; never compute the mapping.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the ticket `plan` document.

## Governing documents

- `docs/frd/frd-06-vehicle-and-engineering-evidence.md`

## Planning implication

Reuse the cited boundaries and revalidate the named sources against current `origin/dev` after the ticket is taken. Do not create a compatibility path, duplicate policy, or an unapproved external write.
