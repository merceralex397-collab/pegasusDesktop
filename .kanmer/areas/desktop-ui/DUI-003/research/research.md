# Research — DUI-003 Lucide, marks and logo assets

## Question

How can the desktop reuse the approved visual assets with integrity checks and no substitute icon system?

## Verified findings

- The design authority registers sixteen Lucide glyphs and hashes both the sprite and approved logo; the ticket body makes the asset registry closed.
- PathIcon resources belong in the shared Styles set and consume DUI-001 brushes.
- The requested trash glyph is a design-owner change request, not permission to substitute or draw an icon.

## Implications

Implement the smallest shared WinUI slice stated in the ticket body. Reuse the existing project, generated gateway client and authority documents; do not create a WebView shell, direct data access, a second vocabulary/resource list, or an Azure dependency. The implementation agent is `winui-dev`; independent review is by `pegasus-desktop-reviewer`.

## Dependency / decision handling

DUI-001 provides styles and brushes; the trash glyph remains deferred until the design-owner process records approval.
