# Checklist — INTK-004

- [ ] Reproduce and record the current missing-staging path and first-attempt terminal behavior.
- [ ] Re-stage the retained, hash-verified source in `EfIntakeMutationStore.ScheduleReevaluationAsync` before changing the work item to pending.
- [ ] Add focused positive, missing/corrupt-source, lease, replay, and atomicity regression coverage.
- [ ] Update the FRD, carry-over record, and any explicitly named in-repository documentation without touching upstream or deployment state.
- [ ] Run the required simplification pass and record honest findings/dispositions in the plan.
- [ ] Run Release build and focused Core/integration validation, then write the post-implementation report with exact results.
