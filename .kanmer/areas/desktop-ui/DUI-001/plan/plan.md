# Plan — DUI-001 Theme resource dictionaries

## Governing documents

This ticket currently remains `docs_todo: true`; its existing source material is the authoritative design documentation and area-06 plan. Do not create or link a speculative canonical document here. When FRD-13 exists, link the relevant stable section before the ticket leaves Preparing; current related references, where present, stay unchanged.

## Chosen approach

How can the desktop consume the authority tokens once, in all three themes, without creating a second palette or bypassing High Contrast? The chosen implementation is a narrow native WinUI 3 shared component/surface, extending the existing desktop project and gateway contracts rather than adding a second framework or policy owner.

## Steps

1. Read DSK-02-09's delivered file set and the token tables; reuse its merge order and guard test rather than creating new infrastructure.
2. Transcribe each approved token into the existing Light, Dark and HighContrast dictionaries, using system colours only for HighContrast.
3. Fill typography, spacing, shape and focus resources; keep the approved 2px radii and 3px focus treatment.
4. Build and run the shell, exercise all three themes, and add the guard-test and screenshot evidence.

## Verification

- `dotnet build --configuration Release` is warning-free.
- The existing `StylesAreTheOnlySourceOfColourAndType` test passes.
- Light, Dark and Contrast screenshots show legible panels and visible focus.

## Risks and dependencies

DSK-02-09 owns the Styles set, App.xaml merge and guard. The two design-owner confirmations named in the ticket body remain external review inputs; do not silently choose a different palette or radius.

The implementation worktree must record its simplification pass and independent desktop review before merge.
