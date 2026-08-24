# Plan — TEST-006 Desktop UI-test harness

## Governing documents

This ticket remains docs_todo: true until the planned desktop governing documents are authored. The local Test/UAT and locked-decision material is binding now; do not create a competing product document in this task.

## Chosen approach

Create the ui-tests.ps1 harness around winapp ui and the AutomationId contract for the native desktop app.

## Steps

1. Read winui-ui-testing guidance and the screen-spec AutomationId convention.
2. Create the harness that builds/launches through the approved workflow and captures PID/build identity.
3. Provide wait-for based UIA helpers, screenshots/records and no Start-Sleep polling.
4. Add a smoke route and AutomationId audit hook for downstream suites.

## Verification

- Harness launches through winapp tooling, never direct exe execution.
- A deterministic smoke script finds named controls and captures evidence.
- Missing AutomationId is detectable by the downstream audit.

## Risks

UI tests mutate installed packages; run only on a dedicated workstation/runner and retain manual a11y review.

Use the detected runner/framework and record exact command output when implementation begins. Complete a simplification pass and independent review before merge.
