# Plan — TEST-013 Desktop CI lanes

## Governing documents

This ticket remains docs_todo: true until the planned desktop governing documents are authored. The local Test/UAT and locked-decision material is binding now; do not create a competing product document in this task.

## Chosen approach

Add desktop-build, desktop-package, desktop-ui-smoke and packaging-tests CI lanes without regressing Linux jobs.

## Steps

1. Read current workflow/action contracts and TEST-012 feasibility output.
2. Add the four narrow desktop jobs with correct dependencies, cache and artifact handling.
3. Use detected test-runner syntax and exact locked restore/Release build commands.
4. Keep Linux/browser jobs intact and emit actionable artifacts on failure.

## Verification

- Workflow validation succeeds and jobs use pinned repository commands.
- Desktop jobs run only their intended subsets.
- Existing Linux jobs remain unaffected.

## Risks

Private Windows runner time is a cost constraint; do not add duplicate lanes or a new CI platform.

Use the detected runner/framework and record exact command output when implementation begins. Complete a simplification pass and independent review before merge.
