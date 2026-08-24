# Files — DUI-015 Accessibility automation lane

| Path or module | Intended change | Reuse / risk |
| --- | --- | --- |
| tests/Pegasus.Desktop.UITests/ui-tests.ps1 | Add AutomationId audit and AxeWindowsCLI invocation/report capture. | Filter only app-owned interactive elements. |
| tests/Pegasus.Desktop.UITests/* | Provide predictable launch/build identity and scan fixtures. | UI tests mutate installed packages; use dedicated runner/workstation. |
| .github/workflows/ci.yml | Connect results after TEST-012/TEST-013 establish viable Windows lanes. | Private runner cost is measured, not assumed. |
| artifacts/a11y/* | Produce ignored evidence at execution time. | Do not commit generated evidence. |

## Context

Read the ticket body, docs/desktop/06-ui-design/README.md, the named UI-design detail, docs/design/README.md, then pegasus-desktop, winui-design, and the relevant test/UI-testing skill. Keep code and canonical-document edits in the eventual ticket worktree only.

## Out of scope

No product-slice implementation, service/framework layer, source-of-truth rewrite, Azure change, or compatibility path.
