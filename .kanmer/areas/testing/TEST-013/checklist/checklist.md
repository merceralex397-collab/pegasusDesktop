# Checklist — TEST-013 Desktop CI lanes

- [ ] Read current workflow/action contracts and TEST-012 feasibility output.
- [ ] Add the four narrow desktop jobs with correct dependencies, cache and artifact handling.
- [ ] Use detected test-runner syntax and exact locked restore/Release build commands.
- [ ] Keep Linux/browser jobs intact and emit actionable artifacts on failure.
- [ ] Verify: Workflow validation succeeds and jobs use pinned repository commands.
- [ ] Verify: Desktop jobs run only their intended subsets.
- [ ] Verify: Existing Linux jobs remain unaffected.
- [ ] Record exact test command/output, simplification pass and independent review.
