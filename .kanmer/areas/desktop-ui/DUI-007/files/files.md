# Files — DUI-007 Virtualized data table pattern

| Path or module | Intended change | Reuse / risk |
| --- | --- | --- |
| `src/Pegasus.Desktop/Controls/DataTable/*` | Add the shared header, row template and column-definition coordination. | Header/row widths must not drift. |
| `src/Pegasus.Desktop/ViewModels/*` | Translate sort/filter/page actions to gateway request parameters. | Never pull a whole dataset to sort locally. |
| `tests/Pegasus.Desktop.UITests/datatable-tests.ps1` | Automate sorting, filtering, keyboard navigation and persisted chooser scenarios. | Use AutomationIds from screen specs. |
| `docs/desktop/06-ui-design/screen-specs.md` | Record only final persistence-key/accessibility wording if needed. | No screen-specific table implementation. |

## Context to read first

- `docs/desktop/06-ui-design/README.md`, its relevant detail file, and `docs/design/README.md` — binding UI authority.
- `.agents/skills/project/pegasus-desktop/SKILL.md` then `.codex/skills/winui-design/SKILL.md` — placement, XAML and accessibility constraints.
- The ticket body — exact acceptance, routing and scope boundary.

## Deliberately out of scope

No product screen slice, new deployment unit, database access, provider credential, Azure write, broad design-authority rewrite, or compatibility layer belongs to this shared-control ticket.
