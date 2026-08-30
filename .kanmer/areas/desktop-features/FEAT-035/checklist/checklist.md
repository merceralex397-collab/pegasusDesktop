# Checklist — FEAT-035 DVLA/DVSA gateway endpoints

- [x] Inventory existing vehicle provider port, cache and provenance owner — verified against `src/Pegasus.Core/Vehicle/LookupContracts.cs`, `VehicleWorkflow.cs`, the existing infrastructure adapter/store, and the linked FRD.
- [x] Design minimal request/status/accept contracts around the existing use cases — typed DTOs and route contracts are in `src/Pegasus.Contracts/VehicleContracts.cs`; Core remains the policy owner.
- [x] Implement gateway routes with auth/problem mapping and no secret/provider-internal projection — `src/Pegasus.Web/Api/VehicleEndpoints.cs`, composed by `DesktopGatewayExtensions.cs`; all seven outcomes and six refusal types remain distinct.
- [x] Test validation, known response, unavailable/timeout and provenance states using replay — API contract tests cover the seven outcomes/provenance/ETag and replay tests cover failed versus not-found.
- [x] Verify: Contract tests cover role denial, invalid VRM, provider failure and accepted suggestion — `Pegasus.Api.ContractTests`: 27 passed.
- [x] Verify: No provider credential/token appears in desktop contract or logs — exact repository scan found only the existing redaction regex; no credential value/key was introduced or exposed.
- [x] Verify: Cache/provenance semantics are asserted by the single Core owner — Core vehicle tests 36 passed; API tests assert provider/version/retrieved/source-observed/source-age and weak ETag.
- [x] Record simplification and independent review evidence — simplification is recorded above; independent review is the next Kanmer stage and remains outstanding until performed.

## Review-fix verification (2026-08-30)

- [x] Independent review findings were addressed: durable provider correlation, real route-to-worker replay proof, typed missing-observation refusal, required `expectedVersion`, and OpenAPI conditional-read/enum metadata.
- [x] Full API contract suite — 29 passed, 0 failed.
- [x] Focused vehicle Core suite — 36 passed, 0 failed.
- [x] Focused vehicle/replay/production integration suite — 27 passed, 0 failed.
- [x] Architecture suite — 121 passed, 0 failed.
- [x] Required filtered integration suite — 972 passed, 2 skipped, 0 failed, 974 total; skips are the existing QDOS mapped-instruction and custody embedded-photograph tests.
- [x] Migration schema guard — 1 passed.
- [x] Review-fix simplification correction recorded; no unapplied findings remain.
- [ ] Fresh independent reviewer PASS — pending; no merge or final Kanmer closeout is claimed.

## Hosted CI correction (2026-08-30)

- [x] Diagnose exact-head CI run `33280183638`: Core 941/941 and architecture 121/121 passed; the API contract filter failed because authenticated contract hosts still resolved SQL-backed `UserManager` on a clean runner.
- [x] Add scoped in-memory identity-store isolation to both contract fixtures — commit `3663cd779194e7f24fc59a99d724e12ba54261d6`.
- [x] Re-run exact filtered contract suite locally — 18 passed, 0 failed.
- [ ] Hosted CI rerun green at the new head — pending.

## Fresh review-fix correction (2026-08-30)

- [x] Remove automatic-sweep pre-normalization; invalid stored registrations reach Core unchanged and are refused there.
- [x] Expose durable provider correlation separately from per-request HTTP correlation in queued/replay/evidence responses.
- [x] Backfill legacy correlation values uniquely from each WorkItemId before enforcing non-null storage.
- [x] Add regression coverage for invalid automatic input and replay/read correlation separation.
- [x] Corrected local validation — build 0/0; Core 941/941; Architecture 121/121; contract filter 18/18; focused vehicle/SQL set 31/31; full filtered integration 973 passed, 2 skipped, 0 failed, 975 total; migration guard 1/1.
- [ ] Fresh independent reviewer PASS — pending against the final pushed head.
- [ ] Hosted CI green at the final pushed head — pending.
