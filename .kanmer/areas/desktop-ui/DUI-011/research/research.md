# Research — DUI-011 ProvenanceGlyph

## Question

How can a value origin remain intelligible to mouse, keyboard and assistive-technology users without a second provenance vocabulary?

## Verified findings

- OperatorLabels.Provenance owns the existing word/glyph pairs and the web partial proves tooltip-on-hover-and-focus behaviour.
- The glyph is supplementary: the value must still make sense without it.
- The ticket consumes DUI-003 icons and DUI-005 shared-label relocation.

## Implication

Use a narrow shared WinUI 3 control or test-lane extension. The desktop remains native, online-required and gateway-backed; no WebView shell, direct database/provider access, Azure write, or second policy owner is justified.

## Dependencies

DUI-003 and DUI-005 must land their resource/map foundations first.
