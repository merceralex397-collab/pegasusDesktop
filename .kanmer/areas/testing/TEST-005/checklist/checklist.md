# Checklist — TEST-005 View-model test catalogue

- [ ] Map each named state/command to its existing view-model and gateway contract.
- [ ] Add deterministic tests for state transitions, cancellation, dirty/validation and navigation.
- [ ] Cover stale session and mandatory-update failures without a UI-thread test.
- [ ] Run targeted mutation-style reasoning on nontrivial guards before declaring gaps closed.
- [ ] Verify: Tests exercise positive and failure states for each catalogue row.
- [ ] Verify: Cancellation and stale-session tests assert observable state, not implementation details.
- [ ] Verify: Focused project tests pass.
- [ ] Record exact test command/output, simplification pass and independent review.
