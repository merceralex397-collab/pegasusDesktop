# Files — DUI-008 Form section and field pattern

| Path or module | Intended change | Reuse / risk |
| --- | --- | --- |
| `src/Pegasus.Desktop/Controls/FormField.cs; FormSection.cs; ErrorSummary.cs` | Add the narrow reusable field/section/error-summary pieces. | No future hint/description slot. |
| `src/Pegasus.Desktop/Styles/Controls.Field.xaml` | Style labels, marker, validation and focus from ThemeResources. | No raw colours or new typography. |
| `tests/Pegasus.Desktop.UITests; ViewModelTests` | Cover association, immediate updates, focus and error navigation. | Manual Narrator pass remains required. |
| `docs/desktop/06-ui-design/tokens-and-theme.md` | Record control names/association mechanics if implementation forces a clarification. | Design authority is not edited. |

## Context to read first

- `docs/desktop/06-ui-design/README.md`, its relevant detail file, and `docs/design/README.md` — binding UI authority.
- `.agents/skills/project/pegasus-desktop/SKILL.md` then `.codex/skills/winui-design/SKILL.md` — placement, XAML and accessibility constraints.
- The ticket body — exact acceptance, routing and scope boundary.

## Deliberately out of scope

No product screen slice, new deployment unit, database access, provider credential, Azure write, broad design-authority rewrite, or compatibility layer belongs to this shared-control ticket.
