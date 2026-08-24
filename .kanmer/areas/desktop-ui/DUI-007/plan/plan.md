# Plan — DUI-007 Virtualized data table pattern

## Governing documents

This ticket currently remains `docs_todo: true`; its existing source material is the authoritative design documentation and area-06 plan. Do not create or link a speculative canonical document here. When FRD-13 exists, link the relevant stable section before the ticket leaves Preparing; current related references, where present, stay unchanged.

## Chosen approach

How can shared tabular screens remain accessible and server-paged without adopting a non-x:Bind DataGrid? The chosen implementation is a narrow native WinUI 3 shared component/surface, extending the existing desktop project and gateway contracts rather than adding a second framework or policy owner.

## Steps

1. Use the WinUI ListView guidance and existing gateway page envelope before designing the shared control.
2. Implement the Grid header/row contract, sort buttons, ComboBox filters and locally persisted column chooser.
3. Wire view-model events to the generated client and retain virtualisation by avoiding a wrapping ScrollViewer.
4. Add UI automation and a 2,000-row performance sample only to prove the shared pattern.

## Verification

- No CommunityToolkit DataGrid reference is added.
- Automation confirms sort-name updates, keyboard path and persisted columns.
- 2,000-row scroll evidence meets the recorded budget.

## Risks and dependencies

DUI-001 supplies table tokens; gateway list contracts are a prerequisite.

The implementation worktree must record its simplification pass and independent desktop review before merge.
