# Plan — INTK-003: upstream:INTK-026 · Normalize kilometre case mileage to canonical miles

## Governing documents

- `docs/frd/frd-06-vehicle-and-engineering-evidence.md`

## Chosen approach

Convert documented kilometre mileage to canonical miles at new-case creation and at every later case-data write, using 1 km = 0.6213711922 miles rounded to the nearest whole mile with midpoints away from zero; retain the typed original-kilometre value as provenance and show it as a compact marker beside the canonical miles figure; treat a missing documented unit as miles; and transform no existing persisted case.

## Routing and constraints

- Future owner: Core/Infrastructure intake work with focused Core and integration tests. The local Test/UAT stack is the verification environment; no production intake or Azure write is authorized.


## Ordered implementation steps

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

## Acceptance conditions

- [ ] A case created or saved with a kilometre mileage stores canonical miles, converted with `0.6213711922` and `MidpointRounding.AwayFromZero`, in `CaseDataOperations.Normalize` and nowhere else.
- [ ] The typed original kilometre value is retained as provenance and reaches the gateway case payload as a compact marker, so [[DSK-05-04]], [[DSK-05-05]] and [[DSK-07-10]] render one value from one source and none converts a second time.
- [ ] A missing documented unit is treated as miles, matching the reading `IsMiles` already encodes.
- [ ] No existing persisted case is transformed: no data migration, no batch, no read-time fallback.
- [ ] Rounding boundaries in both directions and a midpoint value are asserted in `tests/Pegasus.Core.Tests/`.
- [ ] The EVA bundle consequence is recorded in the `plan`, and this ticket changes no EVA field list on its own.

## Verification

- Tests cover kilometre conversion, rounding boundaries, missing-unit miles, provenance, and miles-first rendering.
- No existing persisted case is transformed.
- Blocks [[ENG-008]] so Cazana receives canonical case mileage.
````

## Risks and boundaries

- **Azure**: no write.
- **Scope boundary**: may touch `src/Pegasus.Core/Cases/`, `src/Pegasus.Core/Vehicle/LookupContracts.cs` (read only — reuse the enum, do not redefine it), `tests/Pegasus.Core.Tests/`, `tests/Pegasus.IntegrationTests/`, `docs/frd/frd-06-vehicle-and-engineering-evidence.md` and `docs/capabilities.md`. Must **not** touch `src/Pegasus.Core/Eva/` (bundle content belongs to the imported `upstream:ENG-014` and `upstream:ENG-015`), any Razor page under `src/Pegasus.Web/Pages/**` beyond reading it, or any desktop project.
- **Unblocks / blocked by**: this ticket **blocks** [[DSK-05-04]] and [[DSK-05-05]] (both write `CaseEditableData` and would persist an unconverted kilometre value) and [[DSK-07-10]] (which displays vehicle provenance and would show a kilometre number in a miles field). It is **blocked by** [[DSK-01-10]], the first one-way upstream sync. [[DSK-05-03]] displays the same value read-only — coordinate the marker's presentation with it and with [[DSK-06-16]]'s screen specification rather than inventing a second rendering.
- **Traps**: the conversion belongs on the write path in one place — a read-time conversion would be a second owner of the rule and is a stop condition. Do not add a legacy batch or fallback; the upstream Scope forbids it and a silent retro-transformation is unrecoverable. `MileageUnit` is a persisted string today and `VehicleMileageUnit` is an enum — do not conflate them without recording the mapping. `docs/capabilities.md` has no upstream INTK-026 row today; adding one is part of this ticket, not optional. **Upstream ids and fork board ids do not match**: this ticket is board `INTK-003` and it is upstream INTK-026, while upstream INTK-003 is board [[INTK-002]], a different ticket. Read the join table in `HZN-001/board-conventions.md` § Upstream ids versus board ids; never compute the mapping.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the ticket `plan` document.

## Implementation decision — 2026-08-25

- The retained provenance shape is a new optional CaseField<long> named OriginalMileageKilometres on CaseVehicleData, backed by the existing EAV case-data store field vehicle_mileage_kilometres. It is projected through the existing case DTO; no new endpoint or relational column is added.
- CaseEditableData.VehicleMileageKilometres is an internal normalization/store-transfer value, not caller authority. CaseDataPolicy.Normalize overwrites it from the submitted mileage when the documented unit parses as VehicleMileageUnit.Kilometres; miles and a missing unit clear it. The normalized canonical mileage unit is Miles.
- A nonblank unit must parse case-insensitively as the existing VehicleMileageUnit vocabulary; an unknown unit is rejected rather than silently treated as miles. Missing unit alone means miles.
- Because the case-data store is EAV, the implementation must update the Core contracts/policy plus CaseDataFieldNames, CaseDataSnapshotFactory, and EfCaseDataStore; no EF migration or migration-grant change is required. The field is persisted only for newly saved kilometre input; existing snapshots are not transformed.
- The gateway already serializes CaseDataProjection, so the marker reaches the existing case payload without a route or desktop change. EVA field names remain unchanged and this ticket does not edit CaseEvaMapping.
- The prior “blocked by first upstream sync” text is superseded by the operator’s no-upstream boundary: this branch is based on the configured origin/dev and all work remains in this repository.

## Persistence correction — 2026-08-25

The EAV field registry is enforced by `CK_CaseDataFields_FieldName`, so the earlier no-migration statement was incomplete. This ticket will add one generated EF migration that replaces that check constraint to include `vehicle_mileage_kilometres`; it does not add a table/column, transform existing rows, or require a runtime grant. `Test-MigrationGrants.ps1` remains a required validation and should pass unchanged.

## Execution reconciliation — 2026-08-25

- The implementation follows the repository's actual EAV case-data path: `CaseDataPolicy.Normalize` is the only conversion owner; `CaseDataFieldNames` and `EfCaseDataStore` persist/project the marker; the existing CaseDataProjection is the payload path. The current repository has no separate desktop gateway contract or `CaseDataSnapshotFactory` change required for this field.
- The marker is carried by `CaseVehicleData.OriginalMileageKilometres`, and the existing Web case-details/MCP callers pass it through the existing `CaseEditableData` path. The Razor summary shows the marker for the current value; no client-side conversion or new route was added.
- EVA mapping remains unchanged: its existing `Mileage` and `MileageUnit` fields receive the normalized canonical miles representation; the provenance marker is not added to the EVA bundle. The owning imported ENG-014/ENG-015 bundle contract is therefore untouched.
- The assessment completeness rule is unchanged and still sees a confirmed mileage after a kilometre save because the write path stores the normalized miles value.
- The EAV whitelist required the generated `CanonicalCaseMileageProvenance` migration. It only replaces `CK_CaseDataFields_FieldName` to admit `vehicle_mileage_kilometres`; it adds no table or column, performs no data transformation, and requires no `Grant*` entry.

## Simplification pass — 2026-08-25

- Reuse: retained the existing `VehicleMileageUnit` enum, `CaseField<T>`/EAV projection, `SetConfirmed` history path, existing case DTO, and existing details/MCP save route. No new endpoint, service, abstraction, or parallel conversion path was introduced.
- Scope: the change is limited to canonical write normalization, typed provenance persistence/projection, the existing case-details display/preservation fields, the FRD/capability register, migration constraint, and focused tests. EVA fields and external/cloud paths remain unchanged.
- Correctness simplification: replaced permissive enum parsing with explicit named-value matching so numeric enum text cannot bypass the unknown-unit fail-closed rule. Added a regression assertion for `"0"`.
- Disposition: no further behaviour-preserving simplification was identified. The required EAV migration is proportional because the existing SQL field-name check constraint otherwise rejects the new provenance field.
