# Plan — DUI-005 Shared operator vocabulary consumption

## Governing documents

This ticket currently remains `docs_todo: true`; its existing source material is the authoritative design documentation and area-06 plan. Do not create or link a speculative canonical document here. When FRD-13 exists, link the relevant stable section before the ticket leaves Preparing; current related references, where present, stay unchanged.

## Chosen approach

How can every desktop-facing state, time, size and identifier be presented through one shared label map? The chosen implementation is a narrow native WinUI 3 shared component/surface, extending the existing desktop project and gateway contracts rather than adding a second framework or policy owner.

## Steps

1. Resolve the current owner of the shared-label relocation by reading FEAT-023's recorded decision before code work.
2. Route all desktop display values through the shared map and one Europe/London formatter.
3. Replace raw-key display/input paths with named picker/display models.
4. Add view-model tests for unmapped values, raw identifiers and formatting regressions.

## Verification

- Focused view-model tests fail for raw enum/GUID/hash display and pass for approved labels.
- Dates display with the stated Europe/London/UTC fallback semantics.
- No second desktop label table exists.

## Risks and dependencies

FEAT-023's unresolved ownership split must remain unresolved here; this plan consumes its eventual decision rather than duplicating the question.

The implementation worktree must record its simplification pass and independent desktop review before merge.
