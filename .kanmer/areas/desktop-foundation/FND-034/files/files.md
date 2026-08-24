# Files — FND-034

Surveyed 2026-08-24 against fork `main`. Every existing path was confirmed with `ls`/`sed`/`grep`;
paths created by an earlier ticket are marked with that ticket. Consistent with this ticket's
`research` document, which measured the same tree.

## Where the change lands

**This ticket is the single owner of `src/Pegasus.Desktop/Styles/`, its file set and load order, the
`App.xaml` merge, and the `StylesAreTheOnlySourceOfColourAndType` guard test.** [[DUI-001]] (plan
handle `DSK-06-01`) fills the token **values** into these dictionaries in place and creates no second
file, no second merge and no second scanner. That split is settled in this ticket's body and is not
re-opened here.

| Path | Why |
| --- | --- |
| `src/Pegasus.Desktop/Styles/Tokens.Colors.xaml` | **New.** A `ResourceDictionary.ThemeDictionaries` block with keys `Light`, `Dark` and `HighContrast` — and **no `Default`**. The 24 key rows come verbatim from `docs/desktop/06-ui-design/tokens-and-theme.md` § Colour tokens; every HighContrast entry is a `SystemColor*` resource so forced-colours mode governs. |
| `src/Pegasus.Desktop/Styles/Tokens.Typography.xaml` | **New.** The eight text styles, each `BasedOn` its named built-in WinUI style, with `Typography.NumeralAlignment="Tabular"` on the numeric ones. No raw `FontSize` anywhere. |
| `src/Pegasus.Desktop/Styles/Tokens.Spacing.xaml` | **New.** `PegasusSpace1`…`PegasusSpace9` as `x:Double` 4, 8, 12, 14, 18, 24, 32, 40, 64; `PegasusGutter` 24; plus `PegasusTableRowHeight` 32, `PegasusFactRowHeight` 28, `PegasusPanelPadding`, `PegasusContentMaxWidth` 1280, `PegasusRailWidth` 236, `PegasusMinimumTargetSize` 44, `PegasusMinimumWindowWidth` 1280. |
| `src/Pegasus.Desktop/Styles/Tokens.Shape.xaml` | **New.** `ControlCornerRadius` and `OverlayCornerRadius` = `2` (there is no second approved radius — `docs/design/README.md:268`), `PegasusBorderThickness` = `1`. |
| `src/Pegasus.Desktop/Styles/Tokens.Focus.xaml` | **New.** `FocusVisualPrimaryBrush` → `PegasusFocusBrush`, `FocusVisualSecondaryBrush` → `PegasusPanelBrush`, `PegasusFocusVisualThickness` = `3`. |
| `src/Pegasus.Desktop/Styles/Icons.Lucide.xaml` | **New, position reserved.** [[DUI-003]] (plan handle `DSK-06-03`) supplies the sixteen `PathIcon` geometries from the checksum-pinned sprite. This ticket creates the file and its merge slot; it does not invent glyph data. |
| `src/Pegasus.Desktop/Styles/Controls.*.xaml` | **New, positions reserved.** StatusChip, ReasonDialog, ProblemInfoBar, DataTable header, field — filled by [[DUI-006]] (plan handle `DSK-06-06`), [[DUI-008]] (`DSK-06-08`), [[DUI-009]] (`DSK-06-09`) and [[DUI-010]] (`DSK-06-10`). Same rule: the load order reserves their place, this ticket does not author their contents. |
| `src/Pegasus.Desktop/Styles/Pegasus.Theme.xaml` | **New.** Merges the above **in the order above**. Referenced exactly once in the whole application. |
| `src/Pegasus.Desktop/App.xaml` (created by [[FND-030]], plan handle `DSK-02-05`) | Merge `XamlControlsResources` **first**, then `Pegasus.Theme.xaml`, so the project's overrides win. This merge is owned here; [[DUI-001]] verifies it rather than adding a second. |
| `tests/Pegasus.ArchitectureTests/StyleLiteralTests.cs` | **New — and this is the file the ticket really turns on.** Holds the fact `StylesAreTheOnlySourceOfColourAndType`, that exact name, scanning `src/Pegasus.Desktop/**/*.xaml` **excluding `Styles/`** and failing on a hex colour literal, a raw `FontSize=` attribute or a numeric `CornerRadius=`. The body permits either test project; the plan chooses this one because it already runs unfiltered on every PR. |

## Context files

What the implementer must **read**, and what each one tells them.

| Path | What it tells the implementer |
| --- | --- |
| `docs/desktop/06-ui-design/tokens-and-theme.md` | **Read it in full before anything else — it is the WinUI mapping, and every value this ticket writes is transcribed from it.** § Files and load order gives the eight-entry tree and the sentence "`App.xaml` merges `Pegasus.Theme.xaml` **after** the WinUI `XamlControlsResources` so overrides win. Theme dictionaries cover `Light`, `Dark` **and** `HighContrast` explicitly — never `Default`." § Colour tokens gives 24 key rows in three columns. § Typography gives eight styles with their built-in bases. § Spacing gives the nine steps. § Shape gives radius 2, border 1, focus 3. |
| `docs/desktop/06-ui-design/tokens-and-theme.md` § Colour tokens preamble | **The provenance split, which decides what may be changed.** "Light values are the authority's. Dark values are an **assumption** (the authority is light-only)… HighContrast maps every key to a system colour so forced-colours mode governs." Its § Contrast paragraph closes it: the Dark values "are starting points to be adjusted by that review, **not authority**." If a contrast pair fails, adjust **Dark** or raise the question — never a Light value. |
| `docs/desktop/06-ui-design/tokens-and-theme.md` § Change rule (`:197`ff) | Why this ticket may not invent a value: "Tokens here are derived, not owned. A proposed change (including the Dark values and the section-heading size) is raised against `docs/design/README.md` through its change and verification rule (`:982`), reviewed on the gallery page in Light/Dark/HighContrast at 100% and 200%, and only then applied to `Styles/`. **The desktop never carries a second token source**; `site.css` remains the web's implementation of the same authority." |
| `docs/design/README.md` § Shape, borders and focus (`:258-268`) | The authority's own table — primary radius `2px`, borders `1px`, keyboard focus ring `3px rgba(219,8,22,.38)`, depth "Border-first; rare soft shadows" — closing with `:268`: "`site.css` now uses the approved 2px radius throughout. **There is no second approved radius.**" This is why the 6px/5px in `.design-sync/conventions.md` and `.stitch/DESIGN.md` is a flagged discrepancy and **not adopted**. |
| `docs/design/README.md` § Spacing and layout (`:270-277`) | The nine approved steps `4, 8, 12, 14, 18, 24, 32, 40, 64px`, "Use only steps exercised by the selected UI. Primary gutters are 24px." The WinUI mapping matches exactly, so a mismatch means a transcription error, not a judgement call. |
| `docs/design/README.md` § Tokens § Colour | The Light values traced to source — Collision red `#DB0816`, pressed `#8F1422`, warm charcoal `#2C2A27`, ink `#16191D`, light neutral `#F5F4F2`, border `#E6E4E1`, muted `#6B6B6B`, success `#16833B`, amber `#7A3E00`/`#FFF4D6`/`#A15C00`, Review navy `#143A5E`/`#EAF1F8`/`#365F87` — plus the two rules that are the authority's and not the plan's: green is reserved for **confirmed completion** and never means progress or availability, and the excluded marketing tokens (WhatsApp green/pills, large display scales, CTA shadows, gradients, brand-font declarations, pure black `#000000`, cool slate greys). |
| `docs/design/README.md` § Change and verification rule (`:982`) | Where a value change actually starts. This ticket is downstream of it and edits nothing here. |
| `.codex/skills/winui-design/SKILL.md:143` | The exact rule, verified to the line: "Custom theme dictionaries cover `Light`, `Dark`, **and** `HighContrast` explicitly — never `Default`." `:142` adds the binding rule (`{ThemeResource}` at usage sites; `{StaticResource}` inside `ThemeDictionaries`; `SystemAccentColor` / `SystemColor*` stay `{ThemeResource}`), and `:146` warns "Never set `HighContrastAdjustment="None"` unless your app already supplies system-aware brushes throughout." |
| `.codex/skills/winui-design/references/theme-accessibility.md` and `references/brushes-and-icons.md` | The two vendored references this ticket's routing names. Read them for the brush/theme mechanics before writing `Tokens.Colors.xaml`. |
| `.codex/skills/winui-code-review/references/quality-rules.md` | The theming checklist step 12 runs, and the source of "no raw `FontSize` in views". |
| `tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs` (520 lines) | The **shape** the new scanner follows. `:2` already imports `System.Text.RegularExpressions`; `FindRepositoryRoot()` (`:509`) gives a repository-rooted path with no configuration; the `[Fact]` + `Assert` idiom is established. A file-globbing regex fact fits this project without adding a dependency. |
| `.github/workflows/ci.yml` (nine jobs; `unit` at `:136`) | Why the scanner's home matters. The `unit` lane runs the architecture tests unfiltered on every PR **today**, while no lane runs a desktop test project until [[FND-040]] (plan handle `DSK-02-15`) adds one. A scanner in `tests/Pegasus.ArchitectureTests` starts guarding immediately; one in `tests/Pegasus.Desktop.ViewModelTests` waits. |
| `docs/engineering.md` § Lessons from the predecessor (`:217`) | Why step 9's order — prove it **red** on a planted literal, then green — is not ceremony: a guard that has never fired is indistinguishable from a guard that does not work. |
| `Directory.Build.props` (19 lines) | `TreatWarningsAsErrors=true` and `AnalysisLevel=latest-recommended` apply to a XAML resource dictionary like any other file. |

## Ripple effects

- **[[FND-033]] (plan handle `DSK-02-08`) becomes loadable or unloadable.** Every `{ThemeResource}`
  key the shell references must exist in these dictionaries. A missing key is a **runtime** XAML
  failure, not a compile error, so the shell either launches or dies — there is no partial state.
  This ticket's step 10 uses that shell as its visual subject, so the two are proven together.
- **The guard test starts failing other people's work, by design.** Once
  `StylesAreTheOnlySourceOfColourAndType` is green, every area 05 slice and every area 06 control
  ticket inherits the ban. If it lands in `tests/Pegasus.ArchitectureTests`, it fires in the CI
  `unit` lane on the very next PR from any area — which is the point, and worth saying in the PR so
  nobody is surprised.
- **[[DUI-001]] is unblocked and constrained in the same act.** It fills values into these files in
  place. If this ticket's file names or load order differ by even one entry from
  `tokens-and-theme.md` § Files and load order, [[DUI-001]] will either create a second set or edit
  the wrong file.
- **[[DUI-003]], [[DUI-006]], [[DUI-008]], [[DUI-009]] and [[DUI-010]]** each merge their content into
  the slots reserved here. Their tickets assume the slot exists and the merge order is fixed.
- **[[DUI-002]] (plan handle `DSK-06-02`), the gallery page**, is the surface the change rule routes
  token review through at Light/Dark/HighContrast, 100 % and 200 %. It reads every key defined here.
- **[[FND-041]] (plan handle `DSK-02-16`), the Phase 1 exit review**, is the only ticket this one
  blocks on the board. Its high-contrast evidence depends on step 5's `SystemColor*` mapping being
  right.
- **No OpenAPI, generated-client or contract ripple.** This ticket adds no type and calls no endpoint;
  `openapi/pegasus-v1.json` and the generated client are untouched. Say so in the PR rather than
  leaving the reviewer to check.
- **No documentation ripple in this ticket.** `docs/design/README.md` is the authority and is not
  edited; `docs/desktop/06-ui-design/tokens-and-theme.md` is the mapping and is not silently edited —
  a contrast finding is recorded as an open question on this ticket instead.

## Out of scope

Recorded so the reviewer sees each was a decision, matching the ticket's Guardrails.

- **The token values themselves** — [[DUI-001]] owns them and fills them in place. If [[DUI-001]] has
  already landed, keep what it wrote and change no existing key; if it has not, transcribe
  `tokens-and-theme.md` verbatim. **Do not produce two copies of the palette.**
- **A second `Styles/` directory, a second `App.xaml` merge, or a second literal scanner** — refused.
  One directory, one merge, one guard test; the Guardrails name this as the failure mode to avoid.
- **The Lucide glyph geometries, the raster marks and the logo asset** — [[DUI-003]], with the
  checksum pins on `src/Pegasus.Web/wwwroot/images/lucide-sprite.svg` and
  `docs/design/brand/logos/logo_no_margin.png`.
- **The `Controls.*.xaml` contents** — [[DUI-006]], [[DUI-008]], [[DUI-009]], [[DUI-010]].
- **The shell's layout** — [[FND-033]]. This ticket restyles nothing structural; it supplies the
  resources the shell already references.
- **The gallery/debug page and the 100 %/200 % token review** — [[DUI-002]]. This ticket's visual
  evidence is three screenshots of the shell, not a token gallery.
- **Editing `docs/design/README.md` or `tokens-and-theme.md`** — refused. A value change starts in the
  authority through its own change and verification rule (`:982`), in its own ticket.
- **Adjusting a Light value to make a contrast pair pass** — refused explicitly. The Light column is
  authority; the Dark column is the assumption that may move.
- **Adopting the 6px/5px radius** from `site.css` / `.design-sync/conventions.md` / `.stitch/DESIGN.md` —
  refused. `docs/design/README.md:268`: "There is no second approved radius." The discrepancy is
  already flagged to the design owner.
- **Setting `HighContrastAdjustment="None"`** — refused unless every brush is system-aware, per
  `.codex/skills/winui-design/SKILL.md:146`.
- **Loading a brand-font bundle** — refused. Tw Cen MT and Futura are never UI fonts; the report
  renderer's fonts are area 07's concern.
