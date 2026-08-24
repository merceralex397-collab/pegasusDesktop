# Plan — DUI-009 ReasonDialog

## Governing documents

This ticket remains docs_todo: true; use the authority and area plan now, then link the appropriate FRD-13 section once authored. Do not create a competing PRD, FRD or ADR from this ticket.

## Chosen approach

How can every reasoned or destructive desktop action use one accessible ContentDialog that names its consequence and cannot confirm accidentally?

## Steps

1. Read the authority reason-dialog contract and established dialog-service API.
2. Implement the narrow model: requirement, identity, optional approved consequence, reason and verb.
3. Enforce initial reason focus, explicit Cancel, safe Escape, focus containment and invoking-control restoration.
4. Exercise a representative Hold, Block or Unlink flow without inventing new consequence copy.

## Verification

- UI automation proves the primary action is verb-labelled and does not fire on an accidental Enter.
- Focus starts in the reason input and returns to the invoker after Cancel or complete.
- No dialog body contains unapproved explanatory prose.

## Risks and dependencies

DUI-001 supplies theme/focus resources and DSK-02-08 owns dialog-service registration.

The implementation must record its simplification pass and independent pegasus-desktop-reviewer assessment before merge.
