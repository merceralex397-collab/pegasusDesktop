# Research — FND-034: the `Styles/` dictionary set, the `App.xaml` merge, and the literal ban

## Question

What exactly must `src/Pegasus.Desktop/Styles/` contain, where do its values come from, and how is
"no hex literal in any view" turned from a review convention into something that fails a build?

## Current behaviour

**No parity-matrix row covers this ticket, and none should.** The matrix at
`docs/desktop/01-inventory-and-parity/parity-matrix.md` holds `PAR-01`…`PAR-46` — counted with
`grep -c '^| PAR-'`, which returns **46** — and every row is "keyed by the Razor page model and
handler group that implements it today" (`parity-matrix.md:3-5`). A resource dictionary has no page
model.

The closest existing repository mechanism is the **web implementation of the same authority**:
`src/Pegasus.Web/wwwroot/css/site.css`, named at `docs/desktop/06-ui-design/tokens-and-theme.md`
§ Change rule — "the desktop never carries a second token source; `site.css` remains the web's
implementation of the same authority". `docs/design/README.md:268` records that `site.css` "now uses
the approved 2px radius throughout", so the web side has already been reconciled to the authority
once. The desktop is the second implementation of the same values, not a second source of them.

## Findings

### Facts

Verified by reading the repository at fork `main`, 2026-08-24. Each carries its source.

- **`src/Pegasus.Desktop/Styles/` does not exist**, nor does `src/Pegasus.Desktop`. `ls src` returns
  exactly `Pegasus.Core`, `Pegasus.Infrastructure`, `Pegasus.Web`, `Pegasus.Worker`.
  `src/Pegasus.Desktop/App.xaml` is created by [[FND-030]] (plan handle `DSK-02-05`).
- **The file set and load order are written down and are exactly eight entries.**
  `docs/desktop/06-ui-design/tokens-and-theme.md` § Files and load order shows the tree:
  `Tokens.Colors.xaml`, `Tokens.Typography.xaml`, `Tokens.Spacing.xaml`, `Tokens.Shape.xaml`,
  `Tokens.Focus.xaml`, `Icons.Lucide.xaml`, `Controls.*.xaml` (one entry naming a set — StatusChip,
  ReasonDialog, ProblemInfoBar, DataTable header, field), and `Pegasus.Theme.xaml` "which merges the
  above in order; referenced once from App.xaml". Five `Tokens.*` + Icons + the `Controls.*` set +
  `Pegasus.Theme` = the eight the ticket body names, and the body's own step 3 reconciles the count
  the same way.
- **The merge rule is stated in the same file**: "`App.xaml` merges `Pegasus.Theme.xaml` **after** the
  WinUI `XamlControlsResources` so overrides win. Theme dictionaries cover `Light`, `Dark` **and**
  `HighContrast` explicitly — never `Default` (`winui-design` skill `:143`)."
- **That skill line is exact.** `.codex/skills/winui-design/SKILL.md:143` reads: "Custom theme
  dictionaries cover `Light`, `Dark`, **and** `HighContrast` explicitly — never `Default`." The
  citation in `tokens-and-theme.md` is correct to the line.
- **The colour table is complete and its provenance is split.**
  `tokens-and-theme.md` § Colour tokens carries 24 key rows with Light, Dark and HighContrast columns.
  Its preamble states the split that matters: "Light values are the authority's. Dark values are an
  **assumption** (the authority is light-only)… HighContrast maps every key to a system colour so
  forced-colours mode governs", naming the eight system resources `SystemColorWindowColor`,
  `SystemColorWindowTextColor`, `SystemColorHighlightColor`, `SystemColorHighlightTextColor`,
  `SystemColorButtonFaceColor`, `SystemColorButtonTextColor`, `SystemColorGrayTextColor`,
  `SystemColorHotlightColor`. Its § Contrast paragraph says the Dark values "are starting points to be
  adjusted by that review, **not authority**".
- **The Light values trace to the authority.** `docs/design/README.md` § Tokens § Colour
  (`:258`ff) lists Collision red `#DB0816`, pressed `#8F1422`, red tint `rgba(219,8,22,.07)`, warm
  charcoal `#2C2A27`, near-black ink `#16191D`, white `#FFFFFF`, light neutral `#F5F4F2`, border
  `#E6E4E1`, muted text `#6B6B6B`, confirmed-success green `#16833B`, amber `#7A3E00`/`#FFF4D6`/`#A15C00`
  and Review navy `#143A5E`/`#EAF1F8`/`#365F87` — each of which appears verbatim in the WinUI mapping.
- **The banned list and the green rule are the authority's, not the plan's.**
  `docs/design/README.md` § Tokens § Colour closing paragraphs: "Green must not represent progress,
  availability or a generic positive action; it is reserved for confirmed completion", and "Excluded
  marketing tokens include WhatsApp green/pills, large display scales, CTA shadows, document red and
  brand-font declarations." `:201` and the `.stitch` banned list add pure black `#000000` and cool
  slate greys.
- **Shape values, verified against the authority.** `docs/design/README.md:260-267` § Shape, borders
  and focus: primary radius `2px`, borders `1px`, keyboard focus ring `3px rgba(219,8,22,.38)`, depth
  "border-first; rare soft shadows", and `:268` — "There is no second approved radius." The WinUI
  mapping adds that the 6px/5px recorded in `site.css`/`.design-sync/conventions.md`/`.stitch/DESIGN.md`
  is "a discrepancy flagged to the design owner; not adopted".
- **Spacing values, verified against the authority.** `docs/design/README.md:270-277`: approved steps
  `4, 8, 12, 14, 18, 24, 32, 40, 64px`, "Use only steps exercised by the selected UI. Primary gutters
  are 24px." The WinUI mapping's `PegasusSpace1`…`PegasusSpace9` and `PegasusGutter` = 24 match
  exactly, and it adds `PegasusTableRowHeight` 32, `PegasusFactRowHeight` 28, `PegasusPanelPadding`
  12–16, `PegasusContentMaxWidth` 1280, `PegasusRailWidth` 236, `PegasusMinimumTargetSize` 44 and
  `PegasusMinimumWindowWidth` 1280.
- **Typography maps to built-in styles, and one entry is flagged as an assumption.**
  `tokens-and-theme.md` § Typography defines eight keys, each `BasedOn` a named built-in
  (`TitleTextBlockStyle`, `SubtitleTextBlockStyle`, `BodyStrongTextBlockStyle`, `BodyTextBlockStyle`,
  `CaptionTextBlockStyle`), with `Typography.NumeralAlignment="Tabular"` on the numeric styles, and
  records `PegasusSectionTextStyle` as "15/700 (**assumption**: 14 acceptable; confirm in review)".
  It also states "no raw `FontSize` in views" and that "Tw Cen MT and Futura are never UI fonts and no
  brand-font bundle is loaded".
- **The change rule is explicit and this ticket is downstream of it.**
  `tokens-and-theme.md` § Change rule: "Tokens here are derived, not owned. A proposed change
  (including the Dark values and the section-heading size) is raised against `docs/design/README.md`
  through its change and verification rule (`:982`), reviewed on the gallery page in
  Light/Dark/HighContrast at 100% and 200%, and only then applied to `Styles/`." The gallery page is
  `Pegasus.Desktop/Views/Developer/GalleryPage.xaml`, owned by [[DUI-002]] (plan handle `DSK-06-02`).
- **The split with [[DUI-001]] is settled in this ticket's body, in detail, and this research does not
  re-open it.** This ticket owns `src/Pegasus.Desktop/Styles/`, its file set and load order, the
  `App.xaml` merge and the `StylesAreTheOnlySourceOfColourAndType` guard test. [[DUI-001]] (plan handle
  `DSK-06-01`) owns the token **values** and fills them into these dictionaries in place: no new file,
  no second merge, no second guard test.
- **Two of the eight entries are filled by other area-06 tickets**, as the body's step 3 states:
  `Icons.Lucide.xaml` by [[DUI-003]] (plan handle `DSK-06-03`), and the `Controls.*.xaml` set by
  [[DUI-006]] (plan handle `DSK-06-06`), [[DUI-008]] (plan handle `DSK-06-08`), [[DUI-009]] (plan
  handle `DSK-06-09`) and [[DUI-010]] (plan handle `DSK-06-10`). They are merged into
  `Pegasus.Theme.xaml` when they land.
- **The icon source is checksum-pinned.** `tokens-and-theme.md` § Icons: the sixteen registered
  glyphs are converted from `src/Pegasus.Web/wwwroot/images/lucide-sprite.svg` (SHA-256 `C81F0677…22BF1`),
  and the logo master is `docs/design/brand/logos/logo_no_margin.png` (SHA-256 `E7247BE4…63A2`). Both
  belong to [[DUI-003]], not here.
- **`tests/Pegasus.Desktop.ViewModelTests` does not exist** (`ls tests` → `Pegasus.ArchitectureTests`,
  `Pegasus.Core.Tests`, `Pegasus.IntegrationTests`); [[FND-038]] (plan handle `DSK-02-13`) creates it.
  `tests/Pegasus.ArchitectureTests` targets `net10.0` and is a viable home **if** the guard is a pure
  text scan, which it is — the body's step 9 allows exactly that alternative.
- **The architecture-test project already contains a text-scanning precedent.**
  `tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs:2` imports
  `System.Text.RegularExpressions`, and `FindRepositoryRoot()` (`:509`) gives any new scanner a
  repository-rooted path without configuration. A file-globbing regex fact fits the project's existing
  shape.
- **`Directory.Build.props` (19 lines) applies**: `TreatWarningsAsErrors=true`,
  `AnalysisLevel=latest-recommended`. A XAML resource dictionary compiles under those settings like
  any other file.
- **The `winui-code-review` checklist is vendored** at
  `.codex/skills/winui-code-review/references/quality-rules.md`, and `winui-design` carries
  `references/theme-accessibility.md` and `references/brushes-and-icons.md` — all three exist and are
  named in this ticket's routing.

### Assumptions

- **A-FND034-1 — a `ResourceDictionary.ThemeDictionaries` block with keys `Light`, `Dark` and
  `HighContrast` and no `Default` resolves correctly in all three modes.** The skill states the rule
  (`SKILL.md:143`) but the failure mode of getting it wrong is a resource-not-found at runtime, not a
  compile error. *Confirms it*: step 10's three-theme visual sweep, which is the only thing that
  exercises all three code paths. *If wrong*: the app throws on theme switch, which a single-theme
  screenshot would never reveal.
- **A-FND034-2 — HighContrast entries mapped to `SystemColor*` resources are picked up by
  forced-colours mode without `HighContrastAdjustment` changes.** `winui-design` § Theming rules warns
  "Never set `HighContrastAdjustment="None"` unless your app already supplies system-aware brushes
  throughout". *Confirms it*: the high-contrast screenshot at step 10. *If wrong*, forced colours are
  half-applied, which is worse than not supporting them.
- **A-FND034-3 — the literal scanner can distinguish a hex colour from other `#`-prefixed XAML text.**
  The regex must match `#` followed by 3, 4, 6 or 8 hex digits, and must not fire on, say, a `#`
  inside a comment or a `{Binding}` path. *Confirms it*: proving it red with a planted literal
  **and** green on the real tree, which the body's step 9 already requires in that order. *If wrong*
  in the false-positive direction it blocks legitimate work; in the false-negative direction it is a
  guard that has never fired, which `docs/engineering.md` § Lessons deletes.
- **A-FND034-4 — the Dark column passes 4.5:1 for body text and 3:1 for large text and UI
  boundaries.** `tokens-and-theme.md` § Contrast states the requirement and simultaneously says the
  Dark values "are starting points to be adjusted by that review, not authority". *Confirms it*: step
  11's contrast check. *If wrong*: the failing pair becomes an open question for the design authority,
  which is what step 11 and this ticket's § Documentation changes require — the Light column is
  authority and is **not** adjusted to make a pair pass.

## Execution placement

The six-question cloud-justification test from
`docs/desktop/00-governance-and-workflow/README.md` § 3, answered. This ticket is close to the
"places nothing" case, but the section is answered rather than omitted because the ticket ships code
into a distributed package.

| Question | Answer | Evidence |
| --- | --- | --- |
| Shared authority — must several users see and update the same state? | **No** | A resource dictionary is compiled into the package; nothing about it is shared state. The *values* are shared between the web and the desktop, but through one **document** (`docs/design/README.md`) rather than through a runtime service — `tokens-and-theme.md` § Change rule: "the desktop never carries a second token source; `site.css` remains the web's implementation of the same authority". |
| Unattended execution — must it run with every desktop closed? | **No** | Styling resolves only while a window is rendering. |
| Protected credentials — a long-lived secret that must not sit on workstations? | **No** | The dictionaries carry colour, type, spacing and shape values, all of which are already public in `docs/design/README.md`. |
| Public callback — must an external service call a stable public endpoint? | **No** | No network surface of any kind. |
| Central enforcement — revocation, permissions, audit or an invariant independent of the client? | **No — and one adjacent responsibility belongs to Windows, not to any host.** | There is nothing to revoke or authorise. The one enforcement that matters here is **forced-colours mode**, and it is enforced by the operating system on the workstation: mapping every HighContrast entry to a `SystemColor*` resource is precisely how the app yields that decision to Windows rather than keeping it. That is a local placement, deliberately. |
| Measured operational advantage — measured evidence that central is materially better? | **No** | None claimed; none would be meaningful for a resource dictionary. |

**Conclusion.** All six "no" — the responsibility belongs in the desktop, which is the expected and
honest answer for local rendering. No Azure write arises and nothing is placed on any host.

## Implications

1. **The ticket's real deliverable is the guard, not the palette.** [[DUI-001]] fills the values; what
   only this ticket can give is a single directory, a single merge, and a single executable check that
   makes "no hex literal in any view" a failing test rather than a review habit.
   `docs/engineering.md` § Lessons ("A guard that has never fired is deleted") is why step 9's
   prove-it-red-first ordering matters.
2. **Two of the eight entries will be empty for a while, and that is deliberate.**
   `Icons.Lucide.xaml` and the `Controls.*.xaml` set have their own owners. The load order must
   reserve their position in `Pegasus.Theme.xaml` without this ticket inventing their contents — the
   opposite of the dormant-scaffolding problem, because their callers are named tickets with dates.
3. **The Light column is authority and the Dark column is not.** If a contrast pair fails, the correct
   action is to adjust the **Dark** value or raise the question — never to "fix" a Light value, which
   would be an edit to the authority made from a downstream ticket.
4. **`Default` is the trap.** A `Default` theme dictionary silently works in light mode and is the
   most likely accidental shape, since it is what most WinUI samples show. `SKILL.md:143` names it and
   the acceptance criteria make its absence explicit.
5. **The scanner's home is a real choice, not a formality.** A pure text scan can live in
   `tests/Pegasus.ArchitectureTests` (`net10.0`, already imports `System.Text.RegularExpressions`,
   already runs unfiltered in the CI `unit` lane) and would then run on **every PR** without waiting
   for [[FND-038]]. A scan in `tests/Pegasus.Desktop.ViewModelTests` would not run until the desktop
   test project and its lane exist. The body permits either; the plan takes the one that fires
   soonest.
6. **The 200 % and gallery review are not this ticket's.** `tokens-and-theme.md` § Change rule routes
   token review through the gallery page at Light/Dark/HighContrast, 100 % and 200 % — that page is
   [[DUI-002]]'s. This ticket's visual evidence is the shell from [[FND-033]] (plan handle
   `DSK-02-08`) in three themes.

## Open questions

- **None opened.** Every value this ticket writes is transcribed verbatim from
  `docs/desktop/06-ui-design/tokens-and-theme.md` § Colour tokens, § Typography, § Spacing and § Shape,
  which are themselves derived from `docs/design/README.md`. The split with [[DUI-001]] is settled in
  this ticket's body ("do not re-open it") and is honoured.
- **One question may be opened during implementation, and the ticket already says so.** Step 11's
  contrast check may find a foreground/background pair below 4.5:1 (body text) or 3:1 (large text and
  UI boundaries). This ticket's § Documentation changes requires that such a pair is "record[ed] …
  as an open question in the ticket, not as a silent edit". No pair can be recorded now because the
  check has not run — it is step 11 of this ticket's own work — so the obligation is carried in the
  plan's Risks section for the implementer to discharge, rather than pre-opening an empty box.
