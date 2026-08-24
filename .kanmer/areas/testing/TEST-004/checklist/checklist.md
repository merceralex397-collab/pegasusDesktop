# Checklist — TEST-004 Desktop ViewModelTests project

- [ ] Inspect existing test framework, target framework and package conventions.
- [ ] Create the desktop view-model test project with no XAML dispatcher or UI-thread dependency.
- [ ] Reuse one shared fake clock/date convention and generated/gateway fakes.
- [ ] Add it to the solution and run focused tests.
- [ ] Verify: The project targets the approved Windows TFM and runs headlessly.
- [ ] Verify: Tests do not require an installed MSIX or UI thread.
- [ ] Verify: Locked restore and Release build pass.
- [ ] Record exact test command/output, simplification pass and independent review.
