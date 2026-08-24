# Files — DUI-006 StatusChip

| Path or module | Intended change | Reuse / risk |
| --- | --- | --- |
| `src/Pegasus.Desktop/Styles/Controls.StatusChip.xaml` | Create the one reusable visual style/control. | No per-screen status-badge variants. |
| `src/Pegasus.Desktop/Controls/StatusChip.cs` | Map already-humanised state to tone/glyph/text. | Do not reimplement business state rules. |
| `tests/Pegasus.Desktop.ViewModelTests; tests/Pegasus.Desktop.UITests` | Cover state→label/tone/glyph and accessibility name. | Colour alone is insufficient proof. |
| `src/Pegasus.Web/Pages/Shared/_StatusChip.cshtml` | Read-only reference for the existing mapping. | Do not copy a second policy switch if shared labels can own it. |

## Context to read first

- `docs/desktop/06-ui-design/README.md`, its relevant detail file, and `docs/design/README.md` — binding UI authority.
- `.agents/skills/project/pegasus-desktop/SKILL.md` then `.codex/skills/winui-design/SKILL.md` — placement, XAML and accessibility constraints.
- The ticket body — exact acceptance, routing and scope boundary.

## Deliberately out of scope

No product screen slice, new deployment unit, database access, provider credential, Azure write, broad design-authority rewrite, or compatibility layer belongs to this shared-control ticket.
