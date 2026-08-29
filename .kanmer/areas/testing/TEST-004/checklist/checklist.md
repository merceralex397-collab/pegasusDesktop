# Checklist — TEST-004 Desktop ViewModelTests project

- [ ] Inspect existing test framework, target framework and package conventions.
- [ ] Create the desktop view-model test project with no XAML dispatcher or UI-thread dependency.
- [ ] Reuse one shared fake clock/date convention and generated/gateway fakes.
- [ ] Add it to the solution and run focused tests.
- [ ] Verify: The project targets the approved Windows TFM and runs headlessly.
- [ ] Verify: Tests do not require an installed MSIX or UI thread.
- [ ] Verify: Locked restore and Release build pass.
- [ ] Record exact test command/output, simplification pass and independent review.

- [x] Inspect existing test framework, target framework and package conventions.
- [x] Create the desktop view-model test project with no XAML dispatcher or UI-thread dependency.
- [x] Reuse one shared fake clock/date convention and generated/gateway fakes.
- [x] Add it to the solution and run focused tests.
- [x] Verify: The project targets the approved Windows TFM and runs headlessly.
- [x] Verify: Tests do not require an installed MSIX or UI thread.
- [x] Verify: Locked restore and Release build pass.
- [x] Record exact test command/output, simplification pass and independent review. Implementation and simplification are recorded above; independent review passed at exact head 5602d7f1 after the one documentation correction.

# Closeout checklist

---

## Closeout — TEST-004

- [x] PR merge verified (`gh pr view 40 --json state,mergedAt,url` → MERGED, 2026-08-28T22:51:56Z)
- [x] proof.md finalised (PR URL + merge date appended)
- [x] Moved to final stage
- [ ] Outcome recorded in ticket body (PR link, follow-ups)
- [ ] cd out of worktree; `git worktree remove ../pegasus-worktrees/desktop-viewmodel-tests`
- [ ] `git branch -d task/desktop-viewmodel-tests`
- [ ] `git fetch --prune origin` + `git worktree prune`
- [ ] `take_ticket action: "release"`
