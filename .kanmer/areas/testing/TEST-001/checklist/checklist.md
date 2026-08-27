# Checklist — TEST-001 API contract-test project

- [x] Inspect existing test platform, global.json and package-management conventions before adding the project.
- [x] Create the minimal xUnit/WebApplicationFactory project and add it to the solution.
- [x] Reuse gateway fixture/auth test helpers where they exist; do not reproduce Core business policy.
- [x] Run locked restore, Release build and the focused project test.
- [x] Verify: The project appears once in Pegasus.slnx and builds with warnings-as-errors.
- [x] Verify: Focused API contract test command passes using the detected test runner.
- [x] Verify: Fixture does not require live Azure, Box or Graph.
- [x] Record exact test command/output, simplification pass and independent review.

# Closeout checklist

## Closeout — TEST-001

- [x] PR merge verified (PR #29 state MERGED, mergedAt 2026-08-27)
- [x] proof.md finalised (PR URL and merge date recorded)
- [x] Moved to final stage (`done`)
- [x] Outcome recorded in ticket body (PR link, follow-ups)
- [x] cd out of worktree; remove this ticket worktree
- [x] Delete this ticket branch after merged PR
- [x] fetch --prune origin and worktree prune
- [x] take_ticket action: release
