# Plan — DUI-012 Page header and manual refresh

## Governing documents

This ticket remains docs_todo: true; use the authority and area plan now, then link the appropriate FRD-13 section once authored. Do not create a competing PRD, FRD or ADR from this ticket.

## Chosen approach

How can each screen state what it shows and refresh the exact query safely, while preserving last-good data and avoiding a lede?

## Steps

1. Model supported query states and timestamp semantics from the authority and gateway envelope.
2. Implement header and refresh components with title, exact filter, last-good state and one primary action.
3. Protect refresh from double submission and keep last-good data visible during failed/stale states.
4. Add view-model and UI automation for same-filter rerun and keyboard access.

## Verification

- A refresh reuses current filter/page rather than resetting it.
- Stale/unavailable data is explicitly labelled and never rendered as zero.
- No introductory or lede copy appears beneath a page title.

## Risks and dependencies

DUI-001 provides text/brush resources; actual data queries remain in gateway contracts.

The implementation must record its simplification pass and independent pegasus-desktop-reviewer assessment before merge.
