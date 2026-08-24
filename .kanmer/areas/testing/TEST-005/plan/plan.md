# Plan — TEST-005 View-model test catalogue

## Governing documents

This ticket remains docs_todo: true until the planned desktop governing documents are authored. The local Test/UAT and locked-decision material is binding now; do not create a competing product document in this task.

## Chosen approach

Create the view-model coverage catalogue for states, commands, cancellation, dirty state, validation, navigation, stale session and mandatory update.

## Steps

1. Map each named state/command to its existing view-model and gateway contract.
2. Add deterministic tests for state transitions, cancellation, dirty/validation and navigation.
3. Cover stale session and mandatory-update failures without a UI-thread test.
4. Run targeted mutation-style reasoning on nontrivial guards before declaring gaps closed.

## Verification

- Tests exercise positive and failure states for each catalogue row.
- Cancellation and stale-session tests assert observable state, not implementation details.
- Focused project tests pass.

## Risks

Core policy is asserted through contracts/view-model outputs only; test code must not become a second rule owner.

Use the detected runner/framework and record exact command output when implementation begins. Complete a simplification pass and independent review before merge.
