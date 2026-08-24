# Files — DUI-003 Lucide, marks and logo assets

| Path or module | Intended change | Reuse / risk |
| --- | --- | --- |
| `src/Pegasus.Desktop/Styles/Icons.Lucide.xaml` | Convert only registered glyph paths into reusable resources. | A new icon set or hand-drawn geometry violates the authority. |
| `src/Pegasus.Desktop/Assets/*` | Copy approved raster marks/logo with build action appropriate for packaging. | Do not recolour, redraw or extract assets from screenshots. |
| `tests/Pegasus.Desktop.*` | Verify the recorded SHA-256 for the approved logo/asset mapping. | Keep the expected hash in the authority or a single test fixture. |
| `docs/design/README.md` | Use the documented change route for a seventeenth glyph. | No silent registry expansion. |

## Context to read first

- `docs/desktop/06-ui-design/README.md`, its relevant detail file, and `docs/design/README.md` — binding UI authority.
- `.agents/skills/project/pegasus-desktop/SKILL.md` then `.codex/skills/winui-design/SKILL.md` — placement, XAML and accessibility constraints.
- The ticket body — exact acceptance, routing and scope boundary.

## Deliberately out of scope

No product screen slice, new deployment unit, database access, provider credential, Azure write, broad design-authority rewrite, or compatibility layer belongs to this shared-control ticket.
