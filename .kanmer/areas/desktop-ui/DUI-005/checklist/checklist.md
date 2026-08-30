# Checklist — DUI-005 Shared operator vocabulary consumption

- [x] Resolve the current owner of the shared-label relocation: [[GWY-016]] owns `Pegasus.Contracts/Vocabulary/OperatorVocabulary.cs`; [[FEAT-023]] is archived as covered/duplicate.
- [x] Route current desktop display values through the shared map and one Europe/London formatter; counts and sizes are formatted at the desktop boundary.
- [x] Confirm the current desktop scaffold has no identifier-entry path, Target/reference column, or raw-key display; add guard tests so a future typed identifier or raw aggregate fails.
- [x] Add view-model tests for unmapped enum values, raw identifiers and formatting regressions.
- [x] Verify focused view-model tests fail for raw enum/GUID/hash display and pass for approved labels; current suite passes.
- [x] Verify dates display with Europe/London/UTC-fallback semantics; summer and winter assertions pass.
- [x] Verify no second desktop label table exists; `OperatorText` delegates to the shared owner.
- [ ] Record the independent desktop review alongside the simplification pass before merge.
