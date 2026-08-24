# Plan — DUI-011 ProvenanceGlyph

## Governing documents

This ticket remains docs_todo: true; use the authority and area plan now, then link the appropriate FRD-13 section once authored. Do not create a competing PRD, FRD or ADR from this ticket.

## Chosen approach

How can a value origin remain intelligible to mouse, keyboard and assistive-technology users without a second provenance vocabulary?

## Steps

1. Confirm the shared provenance API and registered glyph resources.
2. Implement one compact glyph control driven by the shared map.
3. Show the exact one-word tooltip on hover and keyboard focus and mirror it in UIA.
4. Add tests for each supported provenance type and the Unknown fallback.

## Verification

- Accessible name and tooltip agree with the shared label map.
- Keyboard focus reveals the same word as hover.
- No raw provenance key or local vocabulary table exists.

## Risks and dependencies

DUI-003 and DUI-005 must land their resource/map foundations first.

The implementation must record its simplification pass and independent pegasus-desktop-reviewer assessment before merge.
