# Plan — DUI-008 Form section and field pattern

## Governing documents

This ticket currently remains `docs_todo: true`; its existing source material is the authoritative design documentation and area-06 plan. Do not create or link a speculative canonical document here. When FRD-13 exists, link the relevant stable section before the ticket leaves Preparing; current related references, where present, stay unchanged.

## Chosen approach

How can reusable forms satisfy the authority's label/control-only rule while exposing required state and validation accessibly? The chosen implementation is a narrow native WinUI 3 shared component/surface, extending the existing desktop project and gateway contracts rather than adding a second framework or policy owner.

## Steps

1. Read the design authority and current web error-summary shape; validate attached-property syntax against the Microsoft Learn result.
2. Implement label/control/validation only, required marker and LabeledBy/DescribedBy associations.
3. Implement section omission and error-summary focus links without adding explanatory copy.
4. Exercise TextBox immediate updates, visible focus and Narrator/three-theme coverage.

## Verification

- Automated test proves entered text updates before focus changes.
- UIA exposes label, required state and validation association.
- Narrator smoke and Light/Dark/High Contrast screenshots pass.

## Risks and dependencies

DUI-001 tokens are consumed; DUI-010 provides page-level problem treatment, not a replacement for field validation.

The implementation worktree must record its simplification pass and independent desktop review before merge.
