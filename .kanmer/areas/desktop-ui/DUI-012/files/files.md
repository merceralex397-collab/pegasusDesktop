# Files — DUI-012 Page header and manual refresh

| Path or module | Intended change | Reuse / risk |
| --- | --- | --- |
| src/Pegasus.Desktop/Controls/PageHeader*; RefreshControl* | Add shared header, freshness and refresh components. | At most one primary action. |
| src/Pegasus.Desktop/ViewModels/* | Carry filter identity, last-good timestamp and refresh state. | No local fake zero data. |
| tests/Pegasus.Desktop.ViewModelTests; UITests | Test same-filter refresh, double-submit guard and stale/unavailable display. | No domain changes. |
| src/Pegasus.Web/Pages/Shared/_FreshnessBanner.cshtml | Read-only contract evidence. | Do not retain web hidden-field mechanics. |

## Context

Read the ticket body, docs/desktop/06-ui-design/README.md, the named UI-design detail, docs/design/README.md, then pegasus-desktop, winui-design, and the relevant test/UI-testing skill. Keep code and canonical-document edits in the eventual ticket worktree only.

## Out of scope

No product-slice implementation, service/framework layer, source-of-truth rewrite, Azure change, or compatibility path.
