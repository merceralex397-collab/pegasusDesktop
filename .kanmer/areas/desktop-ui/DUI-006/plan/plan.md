# Plan — DUI-006 StatusChip

## Governing documents

This ticket currently remains `docs_todo: true`; its existing source material is the authoritative design documentation and area-06 plan. Do not create or link a speculative canonical document here. When FRD-13 exists, link the relevant stable section before the ticket leaves Preparing; current related references, where present, stay unchanged.

## Chosen approach

How can one shared control make a business state readable without relying on colour? The chosen implementation is a narrow native WinUI 3 shared component/surface, extending the existing desktop project and gateway contracts rather than adding a second framework or policy owner.

## Steps

1. Confirm the existing shared vocabulary owner and registered glyph names.
2. Implement one StatusChip with text, glyph and semantic accessible name for every supported state.
3. Reuse ThemeResources for tone and add tests for representative complete, review, pending, blocked and failure states.
4. Render the control in the gallery and verify Contrast mode.

## Verification

- StatusChip tests prove the text accompanies every tone.
- UIA exposes a meaningful name in each state.
- Contrast screenshot remains intelligible without colour.

## Risks and dependencies

DUI-001 and DUI-003 supply the shared resources; DUI-005 determines the label source.

The implementation worktree must record its simplification pass and independent desktop review before merge.
