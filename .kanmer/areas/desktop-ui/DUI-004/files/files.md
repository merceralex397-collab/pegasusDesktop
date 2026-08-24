# Files — DUI-004 Authenticated shell

| Path or module | Intended change | Reuse / risk |
| --- | --- | --- |
| `src/Pegasus.Desktop/Views/Shell/*; MainWindow` | Implement the fixed rail, title/status regions and route hosting. | Do not create a second app shell or WebView host. |
| `src/Pegasus.Desktop/ViewModels/Shell/*` | Bind route selection, gateway rail counts and shell state. | Never show stale data as a literal zero. |
| `src/Pegasus.Desktop.Infrastructure/Gateway/*` | Reuse generated gateway client/query contracts for counts. | No direct database or Azure access. |
| `tests/Pegasus.Desktop.UITests` | Verify route order, route accessibility and non-placeholder count states. | Environment badge wording consumes FND-033's decision. |

## Context to read first

- `docs/desktop/06-ui-design/README.md`, its relevant detail file, and `docs/design/README.md` — binding UI authority.
- `.agents/skills/project/pegasus-desktop/SKILL.md` then `.codex/skills/winui-design/SKILL.md` — placement, XAML and accessibility constraints.
- The ticket body — exact acceptance, routing and scope boundary.

## Deliberately out of scope

No product screen slice, new deployment unit, database access, provider credential, Azure write, broad design-authority rewrite, or compatibility layer belongs to this shared-control ticket.
