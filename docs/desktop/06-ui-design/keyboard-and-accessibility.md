# Keyboard map and accessibility baseline

Reconciles proposal §14.9 with the design authority's accessibility
requirements (`docs/design/README.md:774-808`, `:1295-1304`) and the
vendored `winui-code-review` / `winui-ui-testing` skills. Every critical
workflow must be completable with the keyboard alone; every state must be
perceivable without colour; every control must be reachable by UI Automation.

## Keyboard map

Proposal §14.9 baseline, reconciled (deviations noted):

| Shortcut | Action | Scope | Note |
| --- | --- | --- | --- |
| `Ctrl+K` | Focus the Cases search box (navigates to Cases first if needed) | Global | Deviation from §14.7/§14.9 "global search": the authority merged search into Cases (`:461-478`); no separate search screen |
| `Ctrl+N` | Create the context-appropriate new item (Cases → New case; Upload → pick file; Administration lists → Create) | Screen | Disabled where the current user lacks the right (server-authorised anyway) |
| `Ctrl+S` | Save the current editable view | Case workspace in edit mode, forms | No-op with status-bar text when nothing is dirty |
| `Ctrl+W` | Close the current record view and return to its list | Record screens | Warns via dialog if unsaved changes exist |
| `F5` / `Ctrl+R` | Refresh the current screen (same filter, keeps last-good data) | Global | Mirrors the manual-refresh control |
| `Esc` | Close the transient pane, flyout or dialog; clear a search box when focused | Global | Never discards unsaved edits silently |
| `Ctrl+1` … `Ctrl+8` | Select tab n in the record workspace | Case, Received item, Message | Tab order = displayed order |
| `Alt+D/I/U/Q/C/O/A` | Rail access keys: Dashboard, Inbox, Upload, Queues, Cases, Operations, Administration | Global | Shown as KeyTips on `Alt`; Administration only when present |
| `Tab` / `Shift+Tab` | Move focus through the logical order (title bar → rail → page header → content → status bar) | Global | Focus order follows the visual hierarchy 1–5 of the authority's shared shell (`:1081-1087`) |
| `↑`/`↓`, `Home`/`End`, `PageUp`/`PageDown` | Move in lists and tables | Lists | Virtualized lists keep selection |
| `Enter` or double-click | Open the selected record | Lists | Enter on a focused button invokes it |
| `Space` | Toggle selection/check; invoke focused button | Lists, buttons | |
| `Shift+F10` / `Menu` | Open the row context menu (only genuinely useful commands, duplicating the command bar) | Tables | |
| `Ctrl+C` | Copy the selected reference/plate/Reference value | Record headers, problem Reference row | |
| `Alt+↑`/`Alt+↓` | Move between table header (sort) and first row | Tables | |
| `F6` | Cycle between shell regions (rail, header, content, status) | Global | Helps screen-reader users reach regions quickly |

Discoverability: every shortcut appears in the control's tooltip or
`KeyboardAccelerator` tooltip and in the Diagnostics screen's "Keyboard"
section (a list, not explanatory prose).

Conflicts: none with Windows system shortcuts (`Win+*`), with WinUI
`NavigationView` defaults, or with the reason dialog (`Enter` never confirms
a destructive action because `DefaultButton=Close`).

## Focus order and visible focus

- Focus visual: 3px Collision-red ring (`PegasusFocusBrush`, authority
  `:264`) on every focusable element in Light/Dark; forced-colours mode uses
  the system focus visual.
- Order: title bar controls → rail → page header (title, filter, primary
  action) → content (tables/forms top-to-bottom, left-to-right) → status
  bar. A dialog traps focus, returns it to the invoking control on close
  (`ReasonDialog` contract, authority `:622`).
- Initial focus: lists focus the first row (or the search box when empty);
  forms focus the first field; dialogs focus the reason box (or the first
  input); the Update-required screen focuses "Update now".
- Opening a record from a list and returning preserves the list position and
  any unsaved edits (authority `:623`).
- No focus on decorative elements (raster marks, icons paired with text).

## Accessibility checklist (per screen, enforced in review)

From `winui-code-review` (vendored) and the authority (`:774-808`):

- `AutomationProperties.AutomationId` on every interactive control, unique
  per window, following [the convention](screen-specs.md#automationid-convention).
- `AutomationProperties.Name` on icon-only controls and on every image that
  conveys meaning; decorative images set
  `AutomationProperties.AccessibilityView="Raw"`.
- Semantic controls only: buttons are `Button`s, not clickable `Border`s;
  links are `HyperlinkButton`; tables expose header/row/cell structure
  (ListView with header Grid plus `AutomationProperties.HeadingLevel` and
  sort state announced through the header control's name, e.g. "Received,
  sorted descending").
- Landmarks: `AutomationProperties.LandmarkType` on rail (Navigation),
  content (Main), status bar (Custom "Status"); headings via
  `HeadingLevel` on the screen title and section titles.
- Labels: every field has a visible label associated through
  `AutomationProperties.LabeledBy`; required state via the label's required
  marker with accessible name "required" — never prose.
- Errors: validation text associated through
  `AutomationProperties.DescribedBy`; an error summary `InfoBar` at the top of
  a form lists fields with focusable links; announced once via a `LiveSetting`
  = Polite region.
- Live regions are restrained: status-bar connection changes and refresh
  completion are Polite; nothing is Assertive except a blocking dialog.
- No information by colour alone: every chip carries text and glyph; table
  row states carry a text cell.
- Practical 44px pointer targets while keeping 32px visual rows (padding and
  hit-test area); touch is not required but must not break.
- 200% scale: no clipping, truncation or lost actions; long values wrap;
  secondary content reorders into tabs/drawers at 1024–1279 effective width.
- High contrast (forced colours): all brushes map to system colours; focus
  visible; chips remain distinguishable by text/glyph.
- Reduced motion: `UISettings.AnimationsEnabled == false` disables the only
  animation (indeterminate progress) in favour of static "Working" text; no
  other motion exists (no motion tokens, authority `:283-293`).
- Narrator smoke test per screen: title announced on navigation, first
  control reachable, table navigation reads header + cell, dialog title and
  consequence read, problem Reference readable.
- Permanent consequences visible without hover or colour (authority `:409`).
- Server authorisation regardless of what the UI hides (`:802`).

## Automated checks

| Check | Tool | Source | Gate |
| --- | --- | --- | --- |
| AutomationId coverage audit (every interactive element has a non-empty AutomationId) | `winapp ui inspect -a <PID> --interactive --json` parsed by the generated `ui-tests.ps1` | vendored `winui-ui-testing` skill (`.codex/skills/winui-ui-testing/SKILL.md`) | 100% on shipped pages; fails the UI lane otherwise |
| Scripted keyboard journeys (login → open case → edit → save → logout; sign-in rate limit; update-required) | `winapp ui send-keys`, `wait-for`, `get-value`, `screenshot` | `winui-ui-testing` | Pass/fail per journey; screenshots per state |
| Accessibility rules scan (name/role/value, keyboard focusable, contrast heuristics, landmark/heading structure) | `AxeWindowsCLI.exe --processid <PID> --outputdirectory artifacts/a11y --verbosity default` | `axe-windows` — <https://github.com/microsoft/axe-windows> (fetched 2026-08-23); the engine behind Accessibility Insights for Windows | No critical findings; report archived as a CI artifact |
| Visual checklist (clipping, ellipsis, overlap, dead zones, theme mismatch) at 100% and 200%, Light/Dark/HC | `winapp ui screenshot` per state + reviewer pass | `winui-ui-testing` visual checklist | Reviewer sign-off in the ticket proof |
| Banned-words and raw-code lint over operator strings | View-model unit tests over the label map and problem-type table | `pegasus-test-engineer` (`code-testing-agent`) | Build fails on a banned word or unmapped value |
| Analyzer rules (missing AutomationId `WUI2xxx`, x:Bind mode, converter null, MVVM `WUI3xxx`) | `Microsoft.WindowsAppSDK.Analyzers` injected by `BuildAndRun.ps1` (`winui-dev-workflow`) | vendored skill | Warnings treated as errors in CI (`TreatWarningsAsErrors=true` repo-wide) |

The web's `Deque.AxeCore.Playwright` lane
(`tests/Pegasus.IntegrationTests/Browser/AccessibilityTests.cs`) stays until
the web is retired; the desktop lane is additive and lives in
[08 · testing](../08-testing/README.md).

## The ten recorded reviews (per release candidate, per shipped screen)

Authority `:798-808`; evidence goes into the release ticket's proof
(`proofTypes` visual / test-output / command-log on the fork board):

1. Keyboard-only traversal (script + manual pass; screenshot of focus at
   each stop).
2. Screen-reader and semantic inspection (Narrator script; `winapp ui
   inspect` tree export).
3. Focus and error behaviour (dialog trap/return; error summary; field
   association).
4. 1280px-and-wider desktop review (screenshots).
5. 1024–1279px constrained-desktop review (reordering verified; nothing
   hidden).
6. 200% zoom review (`winapp ui screenshot` at 200%; no clipping/ellipsis).
7. Forced-colours review (High Contrast theme screenshots).
8. Reduced-motion review (animations disabled; static equivalents present).
9. Contrast review (token pairs measured; Dark values adjusted if needed).
10. Automated accessibility scan through the real app (`axe-windows` report).

Evidence format (one file per screen per release candidate):

```text
artifacts/a11y/<release>/<Screen>.md
  - screen, build version, channel, date, reviewer
  - table: review # | result (pass/fail) | evidence path | notes
  - findings: id, severity, rule, element AutomationId, disposition (fixed in
    <ticket> | accepted with reason | deferred to <ticket>)
```

`docs/engineering.md` tier 7 rule applies: automated results do not replace
the manual keyboard and assistive-technology passes; both are recorded.

## Acceptance

A UI ticket is accepted only when: the AutomationId audit is 100%; the
`axe-windows` report has no critical findings; the scripted keyboard journey
for the screen passes; the screen's ten-review evidence file exists (or the
release-candidate file covers it); and `pegasus-desktop-reviewer` has
recorded the design-authority review (four hard rules, banned words, status
vocabulary, one primary action, no colour-only state) in the ticket plan.
