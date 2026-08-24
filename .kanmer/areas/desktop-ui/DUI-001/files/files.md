# Files — DUI-001 Theme resource dictionaries

| Path or module | Intended change | Reuse / risk |
| --- | --- | --- |
| `src/Pegasus.Desktop/Styles/Tokens.*.xaml` | Fill the existing token dictionaries owned by DSK-02-09; do not introduce a parallel Styles tree. | One canonical resource list and theme merge. |
| `src/Pegasus.Desktop/Styles/Pegasus.Theme.xaml; App.xaml` | Use the established merge order once. | Duplicate merges cause inconsistent resource resolution. |
| `tests/Pegasus.Desktop.ViewModelTests` | Reuse the single guard that rejects literal colours, raw FontSize and numeric CornerRadius outside Styles. | The guard must be shared with DSK-02-09, not copied. |
| `docs/desktop/06-ui-design/tokens-and-theme.md` | Record only an unavoidable WinUI-key mapping or resolved authority question. | Design authority remains `docs/design/README.md`. |

## Context to read first

- `docs/desktop/06-ui-design/README.md`, its relevant detail file, and `docs/design/README.md` — binding UI authority.
- `.agents/skills/project/pegasus-desktop/SKILL.md` then `.codex/skills/winui-design/SKILL.md` — placement, XAML and accessibility constraints.
- The ticket body — exact acceptance, routing and scope boundary.

## Deliberately out of scope

No product screen slice, new deployment unit, database access, provider credential, Azure write, broad design-authority rewrite, or compatibility layer belongs to this shared-control ticket.
