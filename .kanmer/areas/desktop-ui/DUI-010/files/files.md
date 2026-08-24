# Files — DUI-010 ProblemInfoBar

| Path or module | Intended change | Reuse / risk |
| --- | --- | --- |
| src/Pegasus.Desktop/Controls/ProblemInfoBar* | Map gateway problem types to severity, approved sentence and copyable Reference row. | No desktop error taxonomy. |
| src/Pegasus.Desktop/ViewModels/* | Expose a transport-neutral problem presentation model. | Do not surface raw type, code or exception text. |
| tests/Pegasus.Desktop.ViewModelTests | Test mappings and banned-word/raw-code guard. | Test the presentation boundary, not Core policy. |
| src/Pegasus.Web/Features/* | Read only the existing problem-details contract. | Gateway changes belong to its owning ticket. |

## Context

Read the ticket body, docs/desktop/06-ui-design/README.md, the named UI-design detail, docs/design/README.md, then pegasus-desktop, winui-design, and the relevant test/UI-testing skill. Keep code and canonical-document edits in the eventual ticket worktree only.

## Out of scope

No product-slice implementation, service/framework layer, source-of-truth rewrite, Azure change, or compatibility path.
