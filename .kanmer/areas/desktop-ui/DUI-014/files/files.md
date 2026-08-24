# Files — DUI-014 Keyboard map and access keys

| Path or module | Intended change | Reuse / risk |
| --- | --- | --- |
| src/Pegasus.Desktop/Views/Shell/*; Controls/* | Register KeyboardAccelerators and access keys at the correct scope. | Avoid conflicting per-page shortcuts. |
| src/Pegasus.Desktop/Views/Diagnostics/* | Show the approved shortcut list where authority permits it. | No how-it-works prose on ordinary pages. |
| tests/Pegasus.Desktop.UITests | Script shortcut routes/focus/command behaviour. | Use UIA actions, not timing sleeps. |
| docs/desktop/06-ui-design/keyboard-and-accessibility.md | Read the closed map and record only evidence. | Do not extend shortcut vocabulary. |

## Context

Read the ticket body, docs/desktop/06-ui-design/README.md, the named UI-design detail, docs/design/README.md, then pegasus-desktop, winui-design, and the relevant test/UI-testing skill. Keep code and canonical-document edits in the eventual ticket worktree only.

## Out of scope

No product-slice implementation, service/framework layer, source-of-truth rewrite, Azure change, or compatibility path.
