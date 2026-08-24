# Plan — DUI-003 Lucide, marks and logo assets

## Governing documents

This ticket currently remains `docs_todo: true`; its existing source material is the authoritative design documentation and area-06 plan. Do not create or link a speculative canonical document here. When FRD-13 exists, link the relevant stable section before the ticket leaves Preparing; current related references, where present, stay unchanged.

## Chosen approach

How can the desktop reuse the approved visual assets with integrity checks and no substitute icon system? The chosen implementation is a narrow native WinUI 3 shared component/surface, extending the existing desktop project and gateway contracts rather than adding a second framework or policy owner.

## Steps

1. Read the approved icon and logo hashes; locate the existing runtime asset mapping.
2. Add PathIcon resources for the registered glyphs and package the approved raster assets without transformation.
3. Add a focused integrity test for the mapped logo/asset hashes.
4. File the trash-glyph request through the authority process; only convert it if that process approves it.

## Verification

- Registered glyph resources resolve in the gallery.
- Asset/hash test passes.
- No substitute icon package or unregistered glyph appears in the desktop project.

## Risks and dependencies

DUI-001 provides styles and brushes; the trash glyph remains deferred until the design-owner process records approval.

The implementation worktree must record its simplification pass and independent desktop review before merge.
