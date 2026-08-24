# Plan — DUI-014 Keyboard map and access keys

## Governing documents

This ticket remains docs_todo: true; use the authority and area plan now, then link the appropriate FRD-13 section once authored. Do not create a competing PRD, FRD or ADR from this ticket.

## Chosen approach

How can the whole authority keyboard map be implemented once, discoverable and script-verifiable across shell and slices?

## Steps

1. Read the complete keyboard map and current shell scope before registering accelerators.
2. Wire global, shell and contextual commands with priority/focus rules.
3. Add access-key discoverability and diagnostics keyboard list.
4. Automate each mapped shortcut and manually review visible focus and keyboard-only completion.

## Verification

- Every listed shortcut has an automated pass/fail result.
- Ctrl+K targets Cases search rather than global search.
- Focus is visible and restored across dialog and navigation transitions.

## Risks and dependencies

DUI-004 supplies the shell; TEST-007 is the downstream end-to-end consumer.

The implementation must record its simplification pass and independent pegasus-desktop-reviewer assessment before merge.
