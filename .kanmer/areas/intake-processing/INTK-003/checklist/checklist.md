# Checklist — INTK-003: upstream:INTK-026 · Normalize kilometre case mileage to canonical miles

- [x] Orient and take the ticket on `task/upstream-intk-026-canonical-miles` from `origin/dev`; current repository boundary supersedes the historical upstream-sync dependency.
- [x] Research the actual writers/readers: Core normalization is the write owner; Infrastructure EAV persistence/projection and existing Web/MCP case-data callers are the affected consumers; EVA field ownership remains unchanged.
- [x] Reuse the existing `VehicleMileageUnit` vocabulary and treat a missing unit as miles.
- [x] Convert kilometres in `CaseDataPolicy.Normalize` with `0.6213711922` and `MidpointRounding.AwayFromZero`; reject unknown named values and numeric enum text; preserve negative-value rejection.
- [x] Retain typed kilometre provenance in `CaseVehicleData.OriginalMileageKilometres`, backed by the EAV field `vehicle_mileage_kilometres`; update the existing whitelist constraint through the generated EF migration.
- [x] Preserve the marker through the existing case DTO and save callers; display it beside the canonical miles value in the existing case summary without client-side reconversion.
- [x] Leave EVA field names/bundle ownership unchanged and record the consequence in the plan.
- [x] Add FRD-06 and capability-register requirements; no new route, table, column, batch, read-time conversion, data backfill, Azure write, or cloud/deployment activity.
- [x] Validate corrected source: focused Core/VehicleWorkflow tests (36/36), full Core tests (927/927), case-data persistence tests (5/5), Release build (0 warnings/errors), migration grants (66 migrations checked), and diff hygiene.
- [ ] Independent review of corrected head, PR/CI, merge to `dev`, post-merge proof on `main`, and Kanmer closeout remain to be completed.

## Evidence notes

The first persistence run correctly exposed the existing EAV field-name check constraint; after generating `CanonicalCaseMileageProvenance`, the focused and full case-data persistence tests passed. The migration changes only that constraint and does not transform existing persisted cases.

The first independent review found duplicate conversion ownership and incomplete provenance validation. The corrected commit `52b00c52` delegates conversion to the existing `VehicleMileagePolicy.ToMiles`, rejects negative provenance, and clears provenance when mileage is cleared.

## Review correction — 2026-08-25

- [x] Correct the vehicle-suggestion acceptance writer to reuse canonical mileage normalization and persist/clear the existing kilometre provenance marker.
- [x] Add and pass the SQL-backed kilometre-correction integration test; rerun full Core tests, Release build, migration-grant validation, and diff hygiene.
- [ ] Obtain independent PASS review of corrected head, green exact-head CI, merge to `dev`, post-merge proof on `main`, and Kanmer closeout.

- [x] Diagnose exact-head CI failure: the migration-list assertion omitted the generated canonical-mileage provenance migration; update the owned test expectation and pass the focused local rerun (1/1).

- [ ] Exact-head CI remains blocked: run `32900431792` failed twice on the unrelated SQL Server deadlock in `GroupedImageIntakeConcurrencyTests.ConcurrentGroupMembersNeverSplitAcrossRepeatedRuns` (290/291 passed on the second attempt).

- [x] Exact-head CI run 32900431792 passed on the unchanged head 13ba7b41775ee83c1399eb84c17e008aa13d7a67 after the authorized failed-job rerun; sql-integration (3) and coverage are green. Review/verification, merge, proof, and closeout remain.
