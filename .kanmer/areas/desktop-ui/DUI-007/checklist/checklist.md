# Checklist — DUI-007 Virtualized data table pattern

- [ ] Use the WinUI ListView guidance and existing gateway page envelope before designing the shared control.
- [ ] Implement the Grid header/row contract, sort buttons, ComboBox filters and locally persisted column chooser.
- [ ] Wire view-model events to the generated client and retain virtualisation by avoiding a wrapping ScrollViewer.
- [ ] Add UI automation and a 2,000-row performance sample only to prove the shared pattern.
- [ ] Verify: No CommunityToolkit DataGrid reference is added.
- [ ] Verify: Automation confirms sort-name updates, keyboard path and persisted columns.
- [ ] Verify: 2,000-row scroll evidence meets the recorded budget.
- [ ] Record the simplification pass and independent review in the plan before merge.
