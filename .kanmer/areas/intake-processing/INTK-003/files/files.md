# Files — INTK-003: upstream:INTK-026 · Normalize kilometre case mileage to canonical miles

## Change surface

| Path | Purpose / risk |
| --- | --- |
| `docs/capabilities.md` | Authoritative context; update only if the ticket's accepted scope explicitly calls for it. |
| `docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md` | Authoritative context; update only if the ticket's accepted scope explicitly calls for it. |
| `docs/frd/frd-06-vehicle-and-engineering-evidence.md` | Authoritative context; update only if the ticket's accepted scope explicitly calls for it. |
| `src/Pegasus.Core/Cases/CaseDataContracts.cs` | Core policy or contract; reuse the existing business owner rather than placing policy in a host. |
| `src/Pegasus.Core/Cases/CaseDataOperations.cs` | Core policy or contract; reuse the existing business owner rather than placing policy in a host. |
| `src/Pegasus.Core/Vehicle/LookupContracts.cs` | Core policy or contract; reuse the existing business owner rather than placing policy in a host. |
| `src/Pegasus.Web/Pages/Cases/Assessment/Index.cshtml.cs` | Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core. |
| `src/Pegasus.Web/Pages/Cases/Details.cshtml.cs` | Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core. |
| `src/Pegasus.Web/Pages/Cases/CaseMutationPageModel.cs` | Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core. |
| `src/Pegasus.Core/Eva/CaseEvaMapping.cs` | Core policy or contract; reuse the existing business owner rather than placing policy in a host. |
| `src/Pegasus.Core/Assessment/AssessmentContracts.cs` | Core policy or contract; reuse the existing business owner rather than placing policy in a host. |
| `src/Pegasus.Core/Assessment/AssessmentPolicy.cs` | Core policy or contract; reuse the existing business owner rather than placing policy in a host. |
| `tests/Pegasus.Core.Tests/` | Focused verification surface; extend the stated success, failure and regression coverage. |
| `src/Pegasus.Web/Mcp/AssessmentMcpTools.cs` | Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core. |
| `scripts/Test-MigrationGrants.ps1` | Repository verification or operational automation; preserve its checked-in workflow. |

## Context files

- `docs/capabilities.md` — Authoritative context; update only if the ticket's accepted scope explicitly calls for it.
- `docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md` — Authoritative context; update only if the ticket's accepted scope explicitly calls for it.
- `docs/frd/frd-06-vehicle-and-engineering-evidence.md` — Authoritative context; update only if the ticket's accepted scope explicitly calls for it.
- `src/Pegasus.Core/Cases/CaseDataContracts.cs` — Core policy or contract; reuse the existing business owner rather than placing policy in a host.
- `src/Pegasus.Core/Cases/CaseDataOperations.cs` — Core policy or contract; reuse the existing business owner rather than placing policy in a host.
- `src/Pegasus.Core/Vehicle/LookupContracts.cs` — Core policy or contract; reuse the existing business owner rather than placing policy in a host.
- `src/Pegasus.Web/Pages/Cases/Assessment/Index.cshtml.cs` — Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core.
- `src/Pegasus.Web/Pages/Cases/Details.cshtml.cs` — Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core.
- `src/Pegasus.Web/Pages/Cases/CaseMutationPageModel.cs` — Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core.
- `src/Pegasus.Core/Eva/CaseEvaMapping.cs` — Core policy or contract; reuse the existing business owner rather than placing policy in a host.
- `src/Pegasus.Core/Assessment/AssessmentContracts.cs` — Core policy or contract; reuse the existing business owner rather than placing policy in a host.
- `src/Pegasus.Core/Assessment/AssessmentPolicy.cs` — Core policy or contract; reuse the existing business owner rather than placing policy in a host.
- `tests/Pegasus.Core.Tests/` — Focused verification surface; extend the stated success, failure and regression coverage.
- `src/Pegasus.Web/Mcp/AssessmentMcpTools.cs` — Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core.

## Ripple and out-of-scope boundary

- **Azure**: no write.
- **Scope boundary**: may touch `src/Pegasus.Core/Cases/`, `src/Pegasus.Core/Vehicle/LookupContracts.cs` (read only — reuse the enum, do not redefine it), `tests/Pegasus.Core.Tests/`, `tests/Pegasus.IntegrationTests/`, `docs/frd/frd-06-vehicle-and-engineering-evidence.md` and `docs/capabilities.md`. Must **not** touch `src/Pegasus.Core/Eva/` (bundle content belongs to the imported `upstream:ENG-014` and `upstream:ENG-015`), any Razor page under `src/Pegasus.Web/Pages/**` beyond reading it, or any desktop project.
- **Unblocks / blocked by**: this ticket **blocks** [[DSK-05-04]] and [[DSK-05-05]] (both write `CaseEditableData` and would persist an unconverted kilometre value) and [[DSK-07-10]] (which displays vehicle provenance and would show a kilometre number in a miles field). It is **blocked by** [[DSK-01-10]], the first one-way upstream sync. [[DSK-05-03]] displays the same value read-only — coordinate the marker's presentation with it and with [[DSK-06-16]]'s screen specification rather than inventing a second rendering.
- **Traps**: the conversion belongs on the write path in one place — a read-time conversion would be a second owner of the rule and is a stop condition. Do not add a legacy batch or fallback; the upstream Scope forbids it and a silent retro-transformation is unrecoverable. `MileageUnit` is a persisted string today and `VehicleMileageUnit` is an enum — do not conflate them without recording the mapping. `docs/capabilities.md` has no upstream INTK-026 row today; adding one is part of this ticket, not optional. **Upstream ids and fork board ids do not match**: this ticket is board `INTK-003` and it is upstream INTK-026, while upstream INTK-003 is board [[INTK-002]], a different ticket. Read the join table in `HZN-001/board-conventions.md` § Upstream ids versus board ids; never compute the mapping.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the ticket `plan` document.

## Current-repository refresh — 2026-08-25

- src/Pegasus.Core/Cases/CaseDataContracts.cs — add the optional original-kilometres projection and normalization-transfer value.
- src/Pegasus.Core/Cases/CaseDataOperations.cs — parse the existing unit vocabulary, convert and round kilometre input, preserve/clear the computed provenance, and reject unknown units.
- src/Pegasus.Infrastructure/Persistence/CaseDataEntities.cs — register the EAV field name; no relational schema change.
- src/Pegasus.Infrastructure/Persistence/CaseDataSnapshotFactory.cs — preserve the source suggestion shape and map the new field into the projection.
- src/Pegasus.Infrastructure/Persistence/EfCaseDataStore.cs — persist the normalized provenance field and round-trip it for later saves.
- tests/Pegasus.Core.Tests/Cases/CaseDataOperationsTests.cs — focused normalization, rounding, unit, provenance, and no-migration facts.
- docs/capabilities.md — register the missing canonical-mileage capability requested by this ticket.
- docs/frd/frd-06-vehicle-and-engineering-evidence.md — record the canonical-mileage and provenance rule.
- docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md — annotate upstream INTK-026 with board [[INTK-003]].

The earlier list’s infrastructure omission was stale against the actual EAV implementation and is amended here; no Web page, EVA mapping, desktop project, migration, or upstream repository is touched.

- `src/Pegasus.Infrastructure/Persistence/Migrations/` — generated check-constraint-only migration for the EAV field whitelist; no data backfill and no runtime grant.
