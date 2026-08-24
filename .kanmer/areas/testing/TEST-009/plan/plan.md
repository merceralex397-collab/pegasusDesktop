# Plan — TEST-009 Accessibility lane

## Governing documents

This ticket remains docs_todo: true until the planned desktop governing documents are authored. The local Test/UAT and locked-decision material is binding now; do not create a competing product document in this task.

## Chosen approach

Add AxeWindowsCLI scanning and the recorded-review checklist to the desktop accessibility lane.

## Steps

1. Reuse TEST-006 harness and DUI-015 AutomationId audit contract.
2. Run AxeWindowsCLI against the real caller, archive reports and fail critical findings.
3. Create a checklist for the ten named manual reviews with build/reviewer/evidence fields.
4. Keep automated and manual results distinct in the report.

## Verification

- Critical Axe result fails the script/lane.
- Automated scan report has build identity and output path.
- Manual keyboard, Narrator, 200% and High Contrast review fields remain required.

## Risks

Automation does not replace human review; no Azure test environment is added.

Use the detected runner/framework and record exact command output when implementation begins. Complete a simplification pass and independent review before merge.
