# Files — DUI-011 ProvenanceGlyph

| Path or module | Intended change | Reuse / risk |
| --- | --- | --- |
| src/Pegasus.Desktop/Controls/ProvenanceGlyph* | Render the shared glyph, one-word tooltip and matching accessible name. | No local word/glyph switch. |
| src/Pegasus.Desktop/ViewModels/* | Expose shared provenance values. | No raw source code reaches XAML. |
| tests/Pegasus.Desktop.UITests | Cover focus tooltip and accessible name. | Mouse hover alone is not sufficient. |
| src/Pegasus.Web/Presentation/OperatorLabels.cs | Read the existing source of truth. | Do not alter it in this UI ticket. |

## Context

Read the ticket body, docs/desktop/06-ui-design/README.md, the named UI-design detail, docs/design/README.md, then pegasus-desktop, winui-design, and the relevant test/UI-testing skill. Keep code and canonical-document edits in the eventual ticket worktree only.

## Out of scope

No product-slice implementation, service/framework layer, source-of-truth rewrite, Azure change, or compatibility path.
