# Research — DUI-006 StatusChip

## Question

How can one shared control make a business state readable without relying on colour?

## Verified findings

- The existing web StatusChip is the complete tone/glyph vocabulary evidence; the ticket requires exact casing through OperatorLabels.
- The design authority requires tone, glyph and text as one component and reserves green for confirmed completion.
- DUI-001 supplies brushes and DUI-003 supplies glyph resources.

## Implications

Implement the smallest shared WinUI slice stated in the ticket body. Reuse the existing project, generated gateway client and authority documents; do not create a WebView shell, direct data access, a second vocabulary/resource list, or an Azure dependency. The implementation agent is `winui-dev`; independent review is by `pegasus-desktop-reviewer`.

## Dependency / decision handling

DUI-001 and DUI-003 supply the shared resources; DUI-005 determines the label source.
