# Plan — DUI-015 Accessibility automation lane

## Governing documents

This ticket remains docs_todo: true; use the authority and area plan now, then link the appropriate FRD-13 section once authored. Do not create a competing PRD, FRD or ADR from this ticket.

## Chosen approach

How can the UI test lane continuously reject interactive controls without AutomationIds or accessibility-critical defects while retaining mandatory human review?

## Steps

1. Confirm TEST-012/TEST-013 runner decision and existing UI harness contract.
2. Implement AutomationId audit from winapp UI inspection with documented OS-chrome exclusions.
3. Run AxeWindowsCLI against the real launched app, archive the report and fail critical findings.
4. Feed results to the UI test lane while preserving manual-review handoff.

## Verification

- An intentionally missing interactive AutomationId fails the audit.
- A critical Axe finding fails the lane and produces a report path.
- Evidence records app build identity, exact commands and scan output.

## Risks and dependencies

TEST-006 supplies the harness; TEST-012 determines CI feasibility; TEST-013 owns workflow lanes; DUI-016 retains human-review ownership.

The implementation must record its simplification pass and independent pegasus-desktop-reviewer assessment before merge.
