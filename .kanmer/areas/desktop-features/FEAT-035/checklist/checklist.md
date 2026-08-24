# Checklist — FEAT-035 DVLA/DVSA gateway endpoints

- [ ] Inventory existing vehicle provider port, cache and provenance owner.
- [ ] Design minimal request/status/accept contracts around the existing use cases.
- [ ] Implement gateway routes with auth/problem mapping and no secret/provider-internal projection.
- [ ] Test validation, known response, unavailable/timeout and provenance states using replay.
- [ ] Verify: Contract tests cover role denial, invalid VRM, provider failure and accepted suggestion.
- [ ] Verify: No provider credential/token appears in desktop contract or logs.
- [ ] Verify: Cache/provenance semantics are asserted by the single Core owner.
- [ ] Record simplification and independent review evidence.
