# Checklist — INTK-004

- [ ] Reproduce and record the current missing-staging path and first-attempt terminal behavior.
- [x] Re-stage the retained, hash-verified source in `EfIntakeMutationStore.ScheduleReevaluationAsync` before changing the work item to pending.
- [x] Add focused positive, missing/corrupt-source, lease, replay, ambiguous-source, stage-failure, and atomicity regression coverage.
- [x] Update the FRD, carry-over record, and explicitly named in-repository documentation without touching upstream or deployment state.
- [x] Run the required simplification pass and record honest findings/dispositions in the plan.
- [x] Run Release build and focused Core/integration validation, then write the post-implementation report with exact results.

The final focused reevaluation suite passed 7 tests; the affected CaseMatch suite passed 7 tests; the full Core suite passed 920 tests; and the Release build passed with 0 warnings and 0 errors. The broader non-Corpus/non-Browser integration run remains non-green only because of the separately recorded SQL deadlock in existing grouped-image concurrency code.

# Closeout checklist

---

## Closeout — INTK-004

- [x] PR merge verified (`gh pr view --json state,mergedAt`)
- [x] proof.md finalised (PR URL + merge date)
- [x] Moved to final stage
- [ ] Outcome recorded in ticket body (PR link, follow-ups)
- [ ] cd out of worktree; `git worktree remove`
- [ ] `git branch -d` (squash/rebase merge requires `-D`)
- [ ] `git fetch --prune` + `git worktree prune`
- [ ] `take_ticket action: "release"`
