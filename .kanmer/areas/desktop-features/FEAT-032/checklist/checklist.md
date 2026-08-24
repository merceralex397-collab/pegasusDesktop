# Checklist — FEAT-032 Desktop document browser and transfer queue

- [ ] Read document/custody screen specs and FEAT-031 contract before selecting view-model seams.
- [ ] Implement browser/list/preview against gateway contracts and reuse shared UI controls.
- [ ] Implement bounded temporary cache plus explicit queued/running/failed/cancelled states.
- [ ] Add view-model/UI tests for large/failing transfers, cancellation and retry handoff.
- [ ] Verify: No desktop provider secret, SDK or reusable provider URL appears.
- [ ] Verify: Queue tests prove failure/cancel state remains explicit.
- [ ] Verify: Bounded cache cannot become an offline authoritative store.
- [ ] Record simplification and independent review evidence.
