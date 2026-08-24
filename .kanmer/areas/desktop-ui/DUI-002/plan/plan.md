# Plan — DUI-002 Developer gallery/debug page

## Governing documents

This ticket currently remains `docs_todo: true`; its existing source material is the authoritative design documentation and area-06 plan. Do not create or link a speculative canonical document here. When FRD-13 exists, link the relevant stable section before the ticket leaves Preparing; current related references, where present, stay unchanged.

## Chosen approach

How can one non-production page make every approved token and shared control state reviewable without becoming an operator-facing screen? The chosen implementation is a narrow native WinUI 3 shared component/surface, extending the existing desktop project and gateway contracts rather than adding a second framework or policy owner.

## Steps

1. Confirm DUI-001's resource keys and the existing developer-channel composition gate.
2. Implement the ordered gallery sections with resource-backed swatches, typography, spacing and control samples.
3. Gate the route to non-production Settings → Developer only and give interactive samples stable AutomationIds.
4. Run the gallery in Light, Dark and HighContrast and capture the review screenshots.

## Verification

- Developer route is absent from a production composition.
- Every required sample resolves an existing resource key.
- Three-theme screenshots and UI automation pass.

## Risks and dependencies

DUI-001 must supply the resource keys; a design-owner review of the Dark palette is recorded against the theme work, not invented in this ticket.

The implementation worktree must record its simplification pass and independent desktop review before merge.
