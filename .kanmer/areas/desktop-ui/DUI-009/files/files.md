# Files — DUI-009 ReasonDialog

| Path or module | Intended change | Reuse / risk |
| --- | --- | --- |
| src/Pegasus.Desktop/Controls/ReasonDialog* | Add one dialog contract and invocation result. | No per-command modal variants. |
| src/Pegasus.Desktop/Services/DialogService* | Reuse the foundation dialog-service seam. | Do not introduce a general event system. |
| src/Pegasus.Desktop/Styles/Controls.Dialog.xaml | Apply existing dialog/focus resources. | No raw styling. |
| tests/Pegasus.Desktop.UITests | Test focus, Escape where safe, Cancel and return focus. | Do not automate domain command behaviour here. |

## Context

Read the ticket body, docs/desktop/06-ui-design/README.md, the named UI-design detail, docs/design/README.md, then pegasus-desktop, winui-design, and the relevant test/UI-testing skill. Keep code and canonical-document edits in the eventual ticket worktree only.

## Out of scope

No product-slice implementation, service/framework layer, source-of-truth rewrite, Azure change, or compatibility path.
