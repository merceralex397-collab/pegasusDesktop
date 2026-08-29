# Checklist — FEAT-035 DVLA/DVSA gateway endpoints

- [x] Inventory existing vehicle provider port, cache and provenance owner — verified against `src/Pegasus.Core/Vehicle/LookupContracts.cs`, `VehicleWorkflow.cs`, the existing infrastructure adapter/store, and the linked FRD.
- [x] Design minimal request/status/accept contracts around the existing use cases — typed DTOs and route contracts are in `src/Pegasus.Contracts/VehicleContracts.cs`; Core remains the policy owner.
- [x] Implement gateway routes with auth/problem mapping and no secret/provider-internal projection — `src/Pegasus.Web/Api/VehicleEndpoints.cs`, composed by `DesktopGatewayExtensions.cs`; all seven outcomes and six refusal types remain distinct.
- [x] Test validation, known response, unavailable/timeout and provenance states using replay — API contract tests cover the seven outcomes/provenance/ETag and replay tests cover failed versus not-found.
- [x] Verify: Contract tests cover role denial, invalid VRM, provider failure and accepted suggestion — `Pegasus.Api.ContractTests`: 27 passed.
- [x] Verify: No provider credential/token appears in desktop contract or logs — exact repository scan found only the existing redaction regex; no credential value/key was introduced or exposed.
- [x] Verify: Cache/provenance semantics are asserted by the single Core owner — Core vehicle tests 36 passed; API tests assert provider/version/retrieved/source-observed/source-age and weak ETag.
- [x] Record simplification and independent review evidence — simplification is recorded above; independent review is the next Kanmer stage and remains outstanding until performed.
