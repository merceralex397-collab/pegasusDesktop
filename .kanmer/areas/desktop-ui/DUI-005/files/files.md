# Files — DUI-005 Shared operator vocabulary consumption

| Path or module | Intended change | Reuse / risk |
| --- | --- | --- |
| `src/Pegasus.Contracts or shared label assembly` | Consume the single relocated OperatorLabels owner. | FEAT-023/GWY-016 ownership must be resolved before moving the implementation. |
| `src/Pegasus.Desktop/ViewModels/*` | Use labels and formatted display values at the view-model boundary. | No `ToString()` or raw identifiers reach XAML. |
| `tests/Pegasus.Desktop.ViewModelTests` | Add focused vocabulary/formatting guard tests. | Tests assert behaviour; they do not duplicate the label taxonomy. |
| `docs/design/README.md; docs/desktop/06-ui-design/README.md` | Use settled casing and design copy rules. | No new vocabulary table. |

## Context to read first

- `docs/desktop/06-ui-design/README.md`, its relevant detail file, and `docs/design/README.md` — binding UI authority.
- `.agents/skills/project/pegasus-desktop/SKILL.md` then `.codex/skills/winui-design/SKILL.md` — placement, XAML and accessibility constraints.
- The ticket body — exact acceptance, routing and scope boundary.

## Deliberately out of scope

No product screen slice, new deployment unit, database access, provider credential, Azure write, broad design-authority rewrite, or compatibility layer belongs to this shared-control ticket.
