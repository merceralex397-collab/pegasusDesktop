# Checklist — INTK-004

- [ ] Reproduce and record the current missing-staging path and first-attempt terminal behavior.
- [x] Re-stage the retained, hash-verified source in `EfIntakeMutationStore.ScheduleReevaluationAsync` before changing the work item to pending.
- [x] Add focused positive, missing/corrupt-source, lease, replay, and atomicity regression coverage.
- [x] Update the FRD, carry-over record, and explicitly named in-repository documentation without touching upstream or deployment state.
- [x] Run the required simplification pass and record honest findings/dispositions in the plan.
- [ ] Run Release build and focused Core/integration validation, then write the post-implementation report with exact results.

The focused integration run has already passed the positive/replay and missing-source cases; the final rerun must include the added corrupt-source and active-lease cases before the implementation report is written.
