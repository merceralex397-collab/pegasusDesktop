# Files — DUI-002 Developer gallery/debug page

| Path or module | Intended change | Reuse / risk |
| --- | --- | --- |
| `src/Pegasus.Desktop/Views/Developer/GalleryPage.xaml(.cs)` | Add the gated gallery surface and resource-backed samples. | It is developer-only; it must not leak into production navigation. |
| `src/Pegasus.Desktop/ViewModels/Developer/*` | Expose the sample state required to render control variants. | Keep samples local and non-domain. |
| `tests/Pegasus.Desktop.UITests` | Add the developer-channel reachability and three-theme smoke coverage. | Avoid coupling production navigation tests to a debug-only route. |
| `docs/desktop/06-ui-design/tokens-and-theme.md` | Use its exact gallery content list. | No new token list is authored here. |

## Context to read first

- `docs/desktop/06-ui-design/README.md`, its relevant detail file, and `docs/design/README.md` — binding UI authority.
- `.agents/skills/project/pegasus-desktop/SKILL.md` then `.codex/skills/winui-design/SKILL.md` — placement, XAML and accessibility constraints.
- The ticket body — exact acceptance, routing and scope boundary.

## Deliberately out of scope

No product screen slice, new deployment unit, database access, provider credential, Azure write, broad design-authority rewrite, or compatibility layer belongs to this shared-control ticket.
