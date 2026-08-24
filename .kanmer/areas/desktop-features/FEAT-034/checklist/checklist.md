# Checklist — FEAT-034 Box conflict and version handling

- [ ] Locate existing document version/custody policy and establish expected concurrency contract.
- [ ] Expose a typed conflict result from the gateway without provider implementation detail.
- [ ] Present the conflict with explicit reload/return path; never auto-overwrite.
- [ ] Test stale upload/update, correct version and user cancellation paths.
- [ ] Verify: Stale version test receives an explicit conflict rather than overwrite.
- [ ] Verify: Desktop tests prove no automatic retry/overwrite occurs.
- [ ] Verify: Audit/custody behaviour remains in the existing Core owner.
- [ ] Record simplification and independent review evidence.
