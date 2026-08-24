# Plan — TEST-007 UI critical-path scripts

## Governing documents

This ticket remains docs_todo: true until the planned desktop governing documents are authored. The local Test/UAT and locked-decision material is binding now; do not create a competing product document in this task.

## Chosen approach

Script launch/update/login, case open/edit/save, concurrency message, logout and keyboard navigation journeys.

## Steps

1. Map each journey to an approved local Test/UAT fixture and stable AutomationIds.
2. Implement wait-for UIA scripts for launch/session, read/write and conflict paths.
3. Drive keyboard navigation explicitly and assert visible result/state after each command.
4. Capture screenshot/recording and report step-level failure evidence.

## Verification

- Every journey can run without sleep-based timing.
- Concurrency path asserts no silent overwrite.
- Keyboard journey covers the authority map relevant to the flow.

## Risks

Requires deterministic local data and must not mutate Outlook/Box; use replay/local copies only.

Use the detected runner/framework and record exact command output when implementation begins. Complete a simplification pass and independent review before merge.
