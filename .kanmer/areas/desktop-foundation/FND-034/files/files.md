# Files — FND-034

Surveyed 2026-08-24 against fork `main`. Existing paths were confirmed with `ls`/`sed`; new files are
marked; files created by a named earlier ticket say so.

## Where the change lands

| Path | Why |
| --- | --- |
| `src/Pegasus.Desktop/Styles/Tokens.Colors.xaml` | **New.** `ResourceDictionary.ThemeDictionaries` with keys `Light`, `Dark` and `HighContrast` — and **no `Default`**. Every key and value transcribed verbatim from `docs/desktop/06-ui-design/tokens-and-theme.md` § Colour tokens (24 rows), never paraphrased or re-derived. |
| `src/Pegasus.Desktop/Styles/Tokens.Typography.xaml` | **New.** The eight text styles, each `BasedOn` its named built-in WinUI style, with `Typography.NumeralAlignment="Tabular"` on the numeric ones. No raw `FontSize` anywhere, here or in any view. |
| `src/Pegasus.Desktop/Styles/Tokens.Spacing.xaml` | **New.** `PegasusSpace1`…`PegasusSpace9` as `x:Double` 4, 8, 12, 14, 18, 24, 32, 40, 64, plus `PegasusGutter` = 24. (The dictionary may also carry `PegasusTableRowHeight` 32, `PegasusFactRowHeight` 28, `PegasusContentMaxWidth` 1280 and `PegasusRailWidth` 236 from the same table — [[FND-033]] (plan handle `DSK-02-08`) and [[DUI-004]] (plan handle `DSK-06-04`) both reference the last two by key.) |
| `src/Pegasus.Desktop/Styles/Tokens.Shape.xaml` | **New.** `ControlCornerRadius` and `OverlayCornerRadius` at `2`, and border thickness at `1`. `docs/design/README.md:268` — "There is no second approved radius"; the 6px/5px in `site.css` is a flagged discrepancy and is **not** adopted. |
| `src/Pegasus.Desktop/Styles/Tokens.Focus.xaml` | **New.** The focus visual overridden to the `PegasusFocusBrush` 3 px ring (`docs/design/README.md:264`), with `FocusVisualPrimaryBrush` / `FocusVisualSecondaryBrush` set per `tokens-and-theme.md` § Shape, borders, focus, depth. |
| `src/Pegasus.Desktop/Styles/Icons.Lucide.xaml` | **New (position reserved, contents not authored here).** [[DUI-003]] (plan handle `DSK-06-03`) converts the sixteen registered glyphs from the SHA-256-pinned sprite. This ticket creates its slot in the load order. |
| `src/Pegasus.Desktop/Styles/Controls.*.xaml` | **New (positions reserved, contents not authored here).** StatusChip, ReasonDialog, ProblemInfoBar, DataTable header and field styles come from [[DUI-006]] (plan handle `DSK-06-06`), [[DUI-008]] (plan handle `DSK-06-08`), [[DUI-009]] (plan handle `DSK-06-09`) and [[DUI-010]] (plan handle `DSK-06-10`). |
| `src/Pegasus.Desktop/Styles/Pegasus.Theme.xaml` | **New.** Merges the above **in the load order** of `tokens-and-theme.md` § Files and load order. Referenced exactly once in the whole application. |
| `src/Pegasus.Desktop/App.xaml` (created by [[FND-030]], plan handle `DSK-02-05`) | Merge `XamlControlsResources` **first**, then `Pegasus.Theme.xaml`, so the project's overrides win. This merge is owned here; [[DUI-001]] (plan handle `DSK-06-01`) verifies it rather than adding a second one. |
| The guard test — `tests/Pegasus.ArchitectureTests/StylesAreTheOnlySourceOfColourAndTypeTests.cs` **or** `tests/Pegasus.Desktop.ViewModelTests/…` | **New.** A fact named exactly `StylesAreTheOnlySourceOfColourAndType` that scans `src/Pegasus.Desktop/**/*.xaml` **excluding `Styles/`** and fails on a hex colour literal (`#` followed by 3, 4, 6 or 8 hex digits), a raw `FontSize=` attribute, or a numeric `CornerRadius=`. The body allows an architecture fact "if the check is pure text" — it is. There is **one** scanner in the repository and it is this one. |

## Context files

What the implementer must **read** and what each one tells them.

| Path | What it tells the implementer |
| --- | --- |
| `docs/desktop/06-ui-design/tokens-and-theme.md` § Files and load order | The eight-entry tree and the merge rule verbatim: `Pegasus.Theme.xaml` "merges the above in order; referenced once from App.xaml", and `App.xaml` merges it **after** `XamlControlsResources` "so overrides win". Also the sentence that settles the `Default` question: theme dictionaries cover Light, Dark **and** HighContrast, "never `Default`". |
| `docs/desktop/06-ui-design/tokens-and-theme.md` § Colour tokens | The 24-row key table with all three columns — the **only** place the desktop's values are written. Its preamble records the provenance split that governs step 11: Light values "are the authority's", Dark values are "an **assumption**", HighContrast maps to eight named `SystemColor*` resources "so forced-colours mode governs". |
| `docs/desktop/06-ui-design/tokens-and-theme.md` § Typography | The eight style keys and their built-in bases, tabular numerals on the numeric ones, and the one flagged assumption: `PegasusSectionTextStyle` is "15/700 (assumption: 14 acceptable; confirm in review)". Also that Tw Cen MT and Futura are never UI fonts and no brand-font bundle is loaded. |
| `docs/desktop/06-ui-design/tokens-and-theme.md` § Change rule | Why this ticket may not change a value: "Tokens here are derived, not owned. A proposed change (including the Dark values and the section-heading size) is raised against `docs/design/README.md` … reviewed on the gallery page in Light/Dark/HighContrast at 100% and 200%, and only then applied to `Styles/`." The gallery page belongs to [[DUI-002]] (plan handle `DSK-06-02`). |
| `docs/design/README.md` § Tokens § Colour (`:258`ff) | The Light column's source of truth — `#DB0816`, `#8F1422`, `rgba(219,8,22,.07)`, `#2C2A27`, `#16191D`, `#F5F4F2`, `#E6E4E1`, `#6B6B6B`, `#16833B`, the amber trio and the navy trio — plus the two rules that are absolute: green is "reserved for confirmed completion" and never means progress or availability, and the excluded marketing tokens (WhatsApp green/pills, large display scales, CTA shadows, document red, brand-font declarations). |
| `docs/design/README.md:260-268` § Shape, borders and focus | Radius `2px`, borders `1px`, focus ring `3px rgba(219,8,22,.38)`, depth "border-first; rare soft shadows", and the closing sentence "There is no second approved radius" — which is why the 6px/5px in `site.css` is not adopted. |
| `docs/design/README.md:270-277` § Spacing and layout | The approved steps `4, 8, 12, 14, 18, 24, 32, 40, 64px`, "Use only steps exercised by the selected UI", and "Primary gutters are 24px". |
| `.codex/skills/winui-design/SKILL.md:143` | The exact rule the acceptance criteria restate: "Custom theme dictionaries cover `Light`, `Dark`, **and** `HighContrast` explicitly — never `Default`." Two lines below it: "Light/Dark working ≠ High Contrast working. Test in a Contrast theme separately", and "Never set `HighContrastAdjustment="None"` unless your app already supplies system-aware brushes throughout." |
| `.codex/skills/winui-design/references/theme-accessibility.md` | The vendored theming and accessibility reference this ticket's routing loads; read it before writing the `ThemeDictionaries` block. |
| `.codex/skills/winui-design/references/brushes-and-icons.md` | Brush and icon conventions — relevant because the colour keys are named by **purpose, not hue** (`PegasusAccentBrush`, `PegasusDangerBrush`), which the skill states as a theming rule and the token table already follows. |
| `.codex/skills/winui-code-review/references/quality-rules.md` | The checklist run at step 12: theming, no raw `FontSize`, no hex literals, AutomationIds present. It is the human counterpart of the scanner. |
| `tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs:2`, `:509` | That the architecture-test project already imports `System.Text.RegularExpressions` and has `FindRepositoryRoot()` — a repository-rooted path with no configuration. A pure text scanner fits this project's existing shape, and this project already runs unfiltered on every PR (`.github/workflows/ci.yml:136-148`). |
| `src/Pegasus.Web/wwwroot/css/site.css` | The web's implementation of the **same** authority. Read it to see the values in their other form; never copy from it, and note that `docs/design/README.md:268` records it as already reconciled to the 2px radius while `tokens-and-theme.md` still flags a 6px/5px discrepancy elsewhere. |
| `src/Pegasus.Desktop/Shell/ShellPage.xaml` (created by [[FND-033]], plan handle `DSK-02-08`) | The first consumer of these keys and the surface the three theme screenshots capture. Its selection marker binds `PegasusAccentBrush`; if this ticket's keys are wrong, that is where it shows. |

## Ripple effects

- **[[DUI-001]] fills the values in place.** Its scope is the token *values* in the six files this
  ticket creates (`Tokens.*.xaml` × 5 plus `Pegasus.Theme.xaml`); it creates no new file, performs no
  second `App.xaml` merge and adds no second guard test. If it has already landed, its values stay
  and no existing key is changed.
- **[[DUI-003]], [[DUI-006]], [[DUI-008]], [[DUI-009]], [[DUI-010]]** fill `Icons.Lucide.xaml` and the
  `Controls.*.xaml` set and are merged into `Pegasus.Theme.xaml` when they land. Their positions in
  the load order are reserved here.
- **Every desktop view becomes subject to the scanner.** From the moment
  `StylesAreTheOnlySourceOfColourAndType` is green, any later ticket that writes a hex literal, a raw
  `FontSize` or a numeric `CornerRadius` outside `Styles/` fails the build — including
  [[FND-033]]'s shell, [[DUI-004]]'s dressing and every area-05 slice.
- **CI.** If the guard lands in `tests/Pegasus.ArchitectureTests`, it runs on every PR immediately
  (`.github/workflows/ci.yml:136-148`, the `unit` lane, chained and unfiltered). If it lands in
  `tests/Pegasus.Desktop.ViewModelTests`, it runs only once [[FND-040]] (plan handle `DSK-02-15`) adds
  the desktop lane.
- **`App.xaml` changes once, permanently.** A second `Pegasus.Theme.xaml` reference anywhere is a
  defect the acceptance criteria name.
- **No solution, package, restore or documentation change.** This ticket adds no project and no
  package; `Pegasus.slnx`, `DependencyDirectionTests.ApplicationSolutionExcludesSourceWorkspaces` and
  every `packages.lock.json` are untouched. `docs/design/README.md` is the **authority** and is not
  edited; `docs/desktop/06-ui-design/tokens-and-theme.md` is edited only if step 11 produces a
  contrast finding, and then as an open question, not a silent value change.

## Out of scope

Recorded so the reviewer sees each was a decision, matching the ticket's Guardrails.

- **Token *values*** — [[DUI-001]] owns them. This ticket transcribes the table verbatim where
  [[DUI-001]] has not landed, and changes nothing where it has.
- **A second `Styles/` directory, a second `App.xaml` merge, or a second scanner** — refused. One
  directory, one merge, one guard test. Duplicating them with [[DUI-001]] is the failure mode this
  ticket exists to avoid.
- **The shell's layout** — [[FND-033]]. This ticket restyles nothing structural.
- **New tokens the authority does not define** — refused. A needed value starts in
  `docs/design/README.md` through its change and verification rule, in its own ticket.
- **Editing `docs/design/README.md`** — refused. It is the authority.
- **Adjusting a Light value to make a contrast pair pass** — refused. The Light column is authority;
  the Dark column is the assumption that may move, and a failing pair is recorded as an open question.
- **`Icons.Lucide.xaml` and `Controls.*.xaml` contents** — owned by the area-06 tickets named above.
- **The gallery page and the 100 %/200 % token review** — [[DUI-002]].
- **A brand-font bundle, Tw Cen MT or Futura as UI fonts** — refused
  (`tokens-and-theme.md` § Typography).
- **Anything on the banned list** — WhatsApp green, large display scales, CTA shadows, gradients,
  neon/glow, purple/blue "AI" aesthetics, pure black `#000000`, cool slate greys.
