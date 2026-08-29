# 06 · UI design — WinUI 3 experience reconciled to the design authority

Area plan for the native desktop user interface: shell, theme tokens, screen
specifications, keyboard map and accessibility baseline. The binding authority
for every UI decision is [`docs/design/README.md`](../../design/README.md) and
the review rule in [`AGENTS.md` § Simplicity rails](../../../AGENTS.md#simplicity-rails)
("Operator-facing explanation is a defect"). Where the proposal's §14 and the
authority differ, the authority wins and the difference is recorded below.

Supporting files in this folder:

- [tokens-and-theme.md](tokens-and-theme.md) — the WinUI `ResourceDictionary`
  plan derived from the authority's tokens.
- [screen-specs.md](screen-specs.md) — one specification block per screen,
  grouped by the proposal's capability groups (§13.1–13.10).
- [keyboard-and-accessibility.md](keyboard-and-accessibility.md) — keyboard
  map, focus rules, accessibility checklist, automated checks and the ten
  recorded reviews.

## 1. Purpose and proposal coverage

Delivers the desktop presentation layer's rules and specifications so that
every UI ticket in [05 · vertical slices](../05-implementation-and-migration/vertical-slices.md)
can be built, reviewed and verified against one source.

| Proposal section | Coverage here |
| --- | --- |
| §14.1 Design character | Reconciled to the authority's design principles (`docs/design/README.md:160-180`) |
| §14.2 Main shell | Shell specification: 236px left rail, route order, status bar |
| §14.3 Dashboard, §14.4 Work queue and case list, §14.5 Case workspace, §14.6 Documents, §14.7 Search | [screen-specs.md](screen-specs.md) |
| §14.8 Notifications and errors | InfoBar/problem presentation; no toasts, no spinners (authority § Motion) |
| §14.9 Keyboard and accessibility | [keyboard-and-accessibility.md](keyboard-and-accessibility.md) |
| §14.10 Theme system | [tokens-and-theme.md](tokens-and-theme.md) |
| §11.1 local preferences (window position, theme, grid columns) | Settings/Diagnostics screen spec |
| §13.1–13.10 capability groups | Screen specs grouped by capability |
| §22.2 accessibility testing | Automated checks and recorded reviews |
| §23.2 native verification (no WebView hosts Pegasus UI) | Enforced by the shell spec and the reviewer checklist |

FRD owner: [FRD-12 operator experience](../../frd/frd-12-operator-experience.md)
today; the desktop-specific behaviour goes into the new FRD-13 (desktop
operator experience) planned in [00 · governance](../00-governance-and-workflow/README.md).

## 2. Evidence base

### Facts

Repository evidence (read on the 2026-08-23 baseline):

- `docs/design/README.md` (1,317 lines) is the design authority. Relevant
  sections and line ranges: operator rail `:65-96`; design principles
  `:160-180`; tokens `:182-293` (colour table `:186-205`, typography
  `:239-256`, shape/borders/focus `:258-267`, spacing `:269-281`, motion
  `:283-293`); logo `:297`; icons `:334-376` (sixteen SHA-256-pinned Lucide
  glyphs); voice, labels and necessary copy `:396-420` (closed necessary-copy
  list, banned words); no explanatory copy and page economy `:422-445` (the
  four hard rules); operations-first shell and route order `:461-504`;
  component map `:582-623` (exercised components and planned contracts,
  including the reason-dialog and lease/conflict contracts); planned workflow
  patterns `:624-762` (Case `:672-723`); complete UI state contract
  `:764-772`; accessibility `:774-808`; change and verification rule `:982`;
  UI specification `:1077-1317` (shared shell and hierarchy `:1081-1109`,
  enforced presentation rules `:1149`, accessibility and acceptance
  `:1295-1304`).
- `AGENTS.md:171-178` binds the authority's "No explanatory copy and page
  economy" rules to every UI change.
- The approved `/Inbox/{id}` redesign
  (`docs/design/references/mockups/inbox-message-page/README.md`) is the
  worked example of the rules applied: one `.record` container, tabs, rows and
  actions render only when populated and available, a "What is removed" table
  citing the authority line by line, and one recorded deliberate departure (no
  action bar).

- The prototype-to-page join is preserved in the reference-only
  [screen map](../../design/references/screen-map.md); `docs/design/README.md`
  remains the authority.
- `src/Pegasus.Contracts/Vocabulary/OperatorVocabulary.cs` is the single
  code→operator-vocabulary map consumed by the gateway and desktop. The
  Core-typed `src/Pegasus.Web/Presentation/OperatorLabels.cs` adapter preserves
  the 24 existing `.cshtml` consumers (see [05 · reuse map](../05-implementation-and-migration/reuse-map.md)).
- `src/Pegasus.Web/wwwroot/css/site.css` (2,471 lines) is the implemented web
  stylesheet; `.design-sync/conventions.md` and `.stitch/DESIGN.md` restate it
  and record `--radius: 6px` / 5px controls, which conflicts with the
  authority's `2px` (`docs/design/README.md:262`, "There is no second approved
  radius").
- `.stitch/DESIGN.md` carries extra neutral values (surface `#f7f6f4`,
  record band `#1b1e23`, band text `#ededee`/`#a7a9ad`, strong hairline
  `#d8d5d1`, success container `#e8f3ec`, primary container `#fceeef`, VRM
  plate `#fcd116` on `#16191d` with border `#d9b012`) and the type scale
  (metric 28/700, page title 20/700, section 15/700, body 13.5/400 at 20
  line-height, caption 12.5, eyebrow 11/700 uppercase).
- The vendored `winui-design` skill (`.codex/skills/winui-design/SKILL.md`,
  from `microsoft/win-dev-skills` v0.5.0 `f1028dd5`) records the WinUI
  landmines this plan inherits: tabular data is `ListView` with a `Grid`
  item template and a header `Grid` because WinUI has no `DataGrid` and the
  CommunityToolkit `DataGrid` columns cannot use `x:Bind` (`:39`); no
  `SizeToContent` (`:47`); `x:Bind` defaults to `OneTime` (`:84-90`);
  `TextBox` two-way needs `UpdateSourceTrigger=PropertyChanged` (`:93-96`);
  `Converter={x:Null}` crashes at runtime (`:118-120`); prefer `x:Bind`
  static functions over `IValueConverter` (`:122-132`); theme dictionaries
  must cover `Light`, `Dark` and `HighContrast` explicitly, never `Default`
  (`:143`); never `HighContrastAdjustment="None"` (`:146`).
- The vendored `winui-code-review` skill requires `AutomationProperties.AutomationId`
  on every interactive control, `AutomationProperties.Name` on icon-only
  controls, semantic controls rather than clickable `Border`s, no information
  by colour alone, all colours through `{ThemeResource}`, typography through
  built-in text styles (no raw `FontSize`), spacing on a 4px grid, corner
  radius through `ControlCornerRadius`/`OverlayCornerRadius`.
- The vendored `winui-ui-testing` skill drives UI Automation through
  `winapp ui` and includes an AutomationId-coverage audit derived from
  `winapp ui inspect --interactive --json` plus a visual checklist that fails a
  run on clipping, ellipsis, overlap, dead zones or theming mismatch.
- `docs/engineering.md:72-89` tier 7 (browser/accessibility) states that
  automated axe results do not replace manual keyboard or assistive-technology
  review; `tests/Pegasus.IntegrationTests/Browser/AccessibilityTests.cs` uses
  `Deque.AxeCore.Playwright` for the web today.

Official documentation (fetched 2026-08-23):

- WinUI 3 design basics, controls and accessibility on Microsoft Learn:
  <https://learn.microsoft.com/windows/apps/design/> (NavigationView,
  ListView, CommandBar, InfoBar, ContentDialog, TeachingTip,
  `AutomationProperties`).
- Windows App SDK 2.x stable line (2.4.0 released 2026-08-13):
  <https://learn.microsoft.com/windows/apps/windows-app-sdk/release-notes/windows-app-sdk-2-0>.
- `axe-windows` automated accessibility engine and CLI:
  <https://github.com/microsoft/axe-windows>.

### Assumptions

- The authority is light-themed only; a Dark theme is not specified anywhere.
  Providing Dark and HighContrast dictionaries is a desktop necessity (WinUI
  follows the system theme) — treated as an assumption to confirm with the
  operator, see [tokens-and-theme.md](tokens-and-theme.md).
- The sixteen Lucide glyph SVGs can be converted to `PathIcon` data without
  visual change (same 24×24 geometry, 2px stroke is a `Stroke` on a `Path`,
  not a filled glyph — verification ticket DSK-06-03).
- WinUI's built-in type ramp (Caption 12, Body 14, BodyStrong 14, Subtitle 20,
  Title 28) approximates the authority's 13.5/14/15/20/28 closely enough that
  no raw `FontSize` is needed; the 15px section heading maps to `BodyStrong`
  (14) unless the operator review finds it too small.

## 3. Decisions and assumptions

Locked decisions this area depends on (from [../README.md](../README.md)):
L-03 (report rendering in an isolated, non-UI WebView2 — the only WebView2
in the product; it never hosts Pegasus UI), L-04 (subagents), and the
governance rule that ADR-0100 (native WinUI 3 client) and FRD-13 are written
before the first slice ships. No Azure writes arise from this area.

### Reconciliation: proposal §14 against the design authority

| Topic | Proposal §14 | Authority | Decision |
| --- | --- | --- | --- |
| Surfaces and backdrop | Mica or comparable system backdrop for the shell; solid surfaces for grids/forms | "Border-led rather than decorative", white or light-neutral ground, white panels, warm-charcoal navigation (`:160-163`) | Solid surfaces everywhere; Mica is permitted only behind the title bar and rail, and only if it passes contrast review. No acrylic. Deviation from §14.1 recorded. |
| Global search | `Ctrl+K` focuses a title-area global search returning grouped results | Search merged into Cases with the identical backing query; no separate search route (`:461-478`) | `Ctrl+K` focuses the Cases search box (navigating to Cases first). No grouped cross-entity search screen in the conversion. Deviation from §14.7 recorded; a grouped search is a later capability. |
| Notifications | "Small notification centre for background outcomes", non-blocking success confirmation | No toasts, no spinners; refresh feedback only; state is shown on the record (`:283-293`, `.stitch` banned list) | `InfoBar` per page for errors/warnings, a status-bar area for connection/transfer state, and the transfer queue pane for background outcomes. No notification centre, no toast. Deviation from §14.8 recorded. |
| Theme system | Semantic colours, typography roles, spacing, density, badge, form and header styles in a small resource dictionary plus gallery page | Tokens `:182-293`, exact hex values, 2px radius, 1px borders, 3px red focus ring, spacing steps, no motion tokens | Adopt the authority's values verbatim; add Dark/HighContrast as an assumption; gallery/debug page as the proposal suggests. See [tokens-and-theme.md](tokens-and-theme.md). |
| Corner radius | not stated | `2px` primary radius, no second approved radius (`:262`); `site.css`/`.stitch` record 6px/5px | `ControlCornerRadius = 2`, `OverlayCornerRadius = 2`. The 6px discrepancy is flagged to the design owner; the desktop does not adopt it. |
| Typography | Fluent typography | System UI text stack; 13.5–14px body; Tw Cen MT/Futura never UI fonts; no brand-font bundle (`:239-256`) | Segoe UI Variable via the built-in WinUI text styles only (no raw `FontSize`); tabular numerals for counts, references, dates. |
| Icons | Fluent icons implied | Lucide only, sixteen registered glyphs, SHA-pinned (`:334-376`); fourteen commissioned raster marks, decorative only; logo never redrawn (`:297`) | Lucide glyphs as `PathIcon` resources converted from the pinned SVGs; no Segoe Fluent Icons substitution without registering a new glyph in the authority; raster marks as decorative assets; logo PNG with checksum rule. |
| Copy | "Human-readable problem messages with an expandable correlation identifier" | Banned words include `correlation identifier`, `artifact`, `lease`, `intake`, `bytes` (`:412-420`); closed necessary-copy list (`:400-409`); four hard rules (`:422-445`) | Problem presentation shows the operator sentence plus a copyable *Reference* value (the correlation id) under that label; every label passes through the operator-label map; no hint text, no how-it-works copy, no empty-state panels in read-only view. |
| Density | "Compact information density" | 32px table rows, 12–16px panel padding, 13.5–14px body, 24px gutters, 1280px-and-wider dense multi-pane, 1024–1279px reordering (`:176`, `:269-281`) | Adopt verbatim; minimum supported window width 1280 (content region capped at 1280 as the authority's `main`), 1024–1279 reorders secondary content into tabs/drawers. Deviation from §14.2's "define minimum size from testing": the authority already defines it. |
| Shell | Left `NavigationView`, title area with global search, command bar, status bar, environment name | 236px left rail, route order Dashboard → Inbox → Upload → Queues → Cases → Operations (scoped staff workspace) → Administration (admin-only) → user/sign-out; rail counts only from queried figures; current route signalled by weight + 2px red left border, never colour alone (`:65-96`, `:461-504`) | `NavigationView` in left-expanded mode at a fixed 236px pane width with the authority's order; counts from the dashboard query; environment badge in the title bar for non-production; status bar with connection/integration/version. |
| Case workspace | Stable header + sub-navigation Overview, Vehicle, Assessment, Documents, Communications, Tasks, Reports, History; lazy sections; collapsible right pane | "A screen about a single record is one container — header, action bar, tabs — and the operator reaches its identity, state, available actions and main content without scrolling" (`:176`); Case patterns `:672-723`; lease/conflict contract `:622` | One container with identity header, action bar and tabs; tabs as in the proposal but each tab renders only populated sections; lease state on the header; reason dialogs per contract; right pane only where a tab's content genuinely needs a secondary column (Decision-card pattern from the approved Inbox mockup). |
| Dialogs | `ContentDialog` only for decisions requiring interruption | Reason dialog contract: named requirement and consequence, labelled reason, confirmation/cancel, initial focus, focus containment, Escape where safe, focus return (`:622`); destructive action confirmed with verb-labelled primary action and identity in the body (winui-design anti-patterns) | `ContentDialog` implements the reason-dialog contract; primary button is the verb ("Block", "Unlink"), secondary is "Cancel". |
| Status badges | "Text, not colour alone" | Status chip pairs tone, Lucide glyph and text label (`:594`); amber = incomplete/pending, navy = Review, green = confirmed completion only (`:164-165`) | One `StatusChip` control; tone selected from the status vocabulary; never colour alone. |
| Primary actions | not stated | One primary button per view region; Collision red is sparse (`:163`, `.stitch` banned list) | Enforced in screen specs and the reviewer checklist. |
| Dates/times | not stated | Every date and time renders Europe/London through the one map; `ToLocalTime()` is never correct (`:171`) | Desktop formats through the shared operator-label/time map with an explicit `Europe/London` zone, never the workstation's local zone. |
| Motion | Progress indicators for longer operations | No motion system, no duration or easing tokens; refresh/loading animation only if understandable without motion and with a reduced-motion static equivalent (`:283-293`) | Indeterminate `ProgressBar` (thin, in the command bar or status bar) for operations longer than a brief interaction; no full-page spinners; no animated transitions; respect `UISettings.AnimationsEnabled`. |

### Additional decisions

- AutomationIds are mandatory on every interactive control and follow the
  naming convention in [screen-specs.md § AutomationId convention](screen-specs.md#automationid-convention).
- The desktop consumes the same operator vocabulary as the web through the
  relocated label map (one list per concept); a value with no mapping is a
  build-time failure in the view-model tests, not a silent `ToString()`.
- WebView2 is used only by the report renderer (L-03) and is never parented
  into a visible page; the reviewer checklist verifies no `WebView2` element
  appears in any XAML view.
- Sample data in mockups and gallery pages follows the authority's rule:
  plausible UK casework, VRMs like `EJ17 NBZ`, references like `576059`,
  dates DD/MM/YYYY, irregular counts — never fabricated real-looking records
  in tests (the repository's `corpus/` is immutable and never uploaded).

## 4. Target state and exit gate

Target state:

- A `Pegasus.Desktop/Styles/` resource set implementing every token in
  [tokens-and-theme.md](tokens-and-theme.md) with Light, Dark and
  HighContrast dictionaries and a gallery/debug page that renders every token
  and every control state.
- A shell that matches the authority's rail, route order and bounded content
  region, with status bar and environment badge.
- Shared controls: `StatusChip`, `ProvenanceGlyph`, `ReasonDialog`,
  `ProblemInfoBar`, `DataTable` pattern (ListView + header Grid), form
  section/field pattern, page header (title + primary action, no lede).
- Screen specifications for every capability group that a slice ticket can
  point to, with states, commands, keyboard and AutomationIds.
- Keyboard map and accessibility baseline automated in CI (`winapp ui`
  AutomationId audit, `axe-windows` scan) and the ten recorded reviews
  performed per release candidate.

Exit gate (programme-level, proposal §27 items 11–12 and Phase 3/8 gates):

- Every shipped screen passes the design-authority review rules (four hard
  rules, banned words, status vocabulary, one primary action) — reviewed by
  `pegasus-desktop-reviewer`, findings recorded in the ticket plan.
- Every interactive control has a unique AutomationId; the
  `winapp ui` coverage audit reports 100% on every shipped page.
- `axe-windows` scan has no critical findings on every shipped page; the ten
  recorded reviews exist for each screen in the release candidate.
- Keyboard-only completion of every critical workflow is demonstrated
  (scripted `winapp ui send-keys` journeys in
  [08 · testing](../08-testing/README.md)).
- 200% scale and forced-colours reviews pass with no clipping, ellipsis or
  lost actions.

## 5. Work breakdown

Profiles are Kanmer profiles on the fork board; tiers follow
[engineering § evidence tiers](../../engineering.md#required-evidence-tiers)
(1 static/build/architecture, 5 Web/API caller, 7 browser/accessibility →
read here as desktop UI/accessibility). Kanmer area: `desktop-ui` (DUI).

| ID | Title | Profile | Depends on | Acceptance | Verification | Tier | Routing |
| --- | --- | --- | --- | --- | --- | --- | --- |
| DSK-06-01 | Theme resource dictionaries (Light/Dark/HighContrast) from the authority tokens | feature | DSK-02 shell scaffold; ADR-0100 | Every token in tokens-and-theme.md has a `ThemeResource` key; HighContrast maps to system colours; no hex literal outside `Styles/`; radius 2; focus ring 3px red | `winui-code-review` theming checklist; gallery page screenshot in Light/Dark/HC; `winapp ui screenshot` per theme | 1, 7 | `winui-dev` · winui-design, winui-dev-workflow · Microsoft Learn (`microsoft_docs_search` ThemeResource, HighContrast) |
| DSK-06-02 | Gallery/debug page listing every token and control state | feature | DSK-06-01 | Page renders all brushes, text styles, spacing, chips, buttons (all states), fields, dialog sample; hidden from the rail in production builds | Screenshots at 100%/200%, Light/Dark/HC; reviewer sign-off | 7 | `winui-dev` · winui-design · — |
| DSK-06-03 | Lucide glyph set as `PathIcon` resources + raster marks + logo asset with checksum check | feature | DSK-06-01 | Sixteen glyphs converted from the SHA-pinned SVGs with identical geometry; an architecture/unit test verifies the logo PNG SHA-256 against `docs/design/README.md`; no other icon font used | Unit test on checksums; visual compare against web sprite | 1, 7 | `winui-dev` · winui-design · — |
| DSK-06-04 | Shell: NavigationView rail (236px), route order, counts, title bar, environment badge, status bar | feature | DSK-06-01; DSK-02 single-instance | Rail order Dashboard → Inbox → Upload → Queues → Cases → Operations → Administration (admin-only) → user; Administration absent (not disabled) for non-admins; counts only from the dashboard query, absent when unqueried; current item shows weight + 2px red marker; environment badge in non-production; content region capped at 1280 and centred | `winapp ui inspect` tree; AutomationId audit; keyboard traversal script; screenshots | 7 | `winui-dev` · winui-design, winui-ui-testing · Microsoft Learn (NavigationView) |
| DSK-06-05 | Operator vocabulary consumption: desktop binds every state/date through the shared label map; missing mapping fails VM tests | feature | 05 reuse-map ticket relocating `OperatorLabels`; DSK-03 contracts | No `enum.ToString()`, GUID, hash, version integer or byte count reaches XAML; Europe/London formatting verified with a BST date | ViewModel tests; reviewer grep for `.ToString()` in views | 1 | `winui-dev` + `pegasus-test-engineer` · code-testing-agent, run-tests · — |
| DSK-06-06 | `StatusChip` control (tone + glyph + text; status vocabulary exact casing) | feature | DSK-06-01, DSK-06-03 | Amber/navy/green/neutral tones only as the authority allows; text always present; AutomationId per chip | Gallery page; unit test over the vocabulary table | 1, 7 | `winui-dev` · winui-design · — |
| DSK-06-07 | Data table pattern: `ListView` + `Grid` item template + header `Grid`, sort toggles (newest-first default), filter dropdowns, column chooser persisted locally, virtualization | feature | DSK-06-01 | 32px rows; header is a sort control with accessible sort state; filters are `ComboBox`es not pill rows; keyboard navigation; no CommunityToolkit DataGrid | `winapp ui` script (sort, filter, keyboard); perf check on 2,000-row list | 7, 10 | `winui-dev` · winui-design (`winui-search.exe` ListView header patterns), winui-ui-testing · Microsoft Learn (ListView virtualization) |
| DSK-06-08 | Form section and field pattern (label + control only; required marker visual; inline validation placement) | feature | DSK-06-01 | No hint text, no "Required."/"Optional." prose, no format guidance; validation message associated with the field and announced; section renders only in edit context where edit-only | Reviewer checklist; Narrator smoke | 7 | `winui-dev` · winui-design, winui-code-review · — |
| DSK-06-09 | `ReasonDialog` (`ContentDialog`) implementing the reason-dialog contract | feature | DSK-06-01 | Named requirement/consequence (closed copy list only), labelled reason, verb-labelled primary + Cancel, initial focus, focus containment, Esc where safe, focus return | `winapp ui` ContentDialog test (Primary/Secondary/Close); Narrator | 7 | `winui-dev` · winui-design, winui-ui-testing · — |
| DSK-06-10 | Problem presentation: `InfoBar` per page with operator sentence + copyable Reference (correlation id); banned words lint in view-model tests | feature | DSK-03 problem details | No banned word in any operator string; Reference copy works; severity mapping from problem type | Unit test over problem-type → message table; reviewer | 1, 7 | `winui-dev` + `pegasus-test-engineer` · code-testing-agent · — |
| DSK-06-11 | Provenance glyph: icon + one-word tooltip on hover AND keyboard focus with matching accessible name | feature | DSK-06-03 | Staff · Extracted · AI · E-mail · Lookup · Principal · Automatic only; tooltip on focus | `winapp ui hover` + focus test; Narrator | 7 | `winui-dev` · winui-design · — |
| DSK-06-12 | Page header (title + exact queue/filter + freshness + one primary action; no lede) and freshness/manual-refresh control | feature | DSK-06-01 | One H1-equivalent per screen; last-good Europe/London time; refresh state words current/stale/partial/unavailable/failed; double-submit protection | Reviewer; `winapp ui` refresh test | 7 | `winui-dev` · winui-design · — |
| DSK-06-13 | Screen specs adopted as FRD-13 sections and linked from each slice ticket | chore | DSK-00 FRD-13 | Every screen in screen-specs.md has an FRD-13 anchor; every slice ticket links it | `Test-DocumentationLinks.ps1` | 1 | `pegasus-parity-researcher` · kanmer-docs · Kanmer (`link_doc`) |
| DSK-06-14 | Keyboard map and access keys implemented (Ctrl+K, Ctrl+N, Ctrl+S, Ctrl+W, F5/Ctrl+R, Esc, rail access keys) | feature | DSK-06-04 | Every shortcut from keyboard-and-accessibility.md works and is discoverable (KeyTips/tooltips); no conflict with system shortcuts | `winapp ui send-keys` journeys | 7 | `winui-dev` · winui-ui-testing · — |
| DSK-06-15 | Accessibility automation: AutomationId coverage audit + `axe-windows` CLI scan wired into the UI test lane | feature | DSK-08 CI lanes | 100% AutomationId coverage on shipped pages; axe-windows run produces an artifact; critical findings fail the lane | CI artifact; `AxeWindowsCLI` report | 7 | `pegasus-ui-verifier` · winui-ui-testing · — |
| DSK-06-16 | Ten recorded reviews per release candidate (keyboard-only, screen reader, focus/error, 1280+, 1024–1279, 200% zoom, forced colours, reduced motion, contrast, automated scan) — checklist + evidence template | chore | DSK-06-15 | Evidence files per screen in the release ticket proof | Reviewer sign-off | 7 | `pegasus-ui-verifier` + `pegasus-desktop-reviewer` · winui-ui-testing, winui-code-review · Kanmer (`set_ticket_doc` proof) |

## 6. Routing table

| Work | Subagent | Skills (pinned source) | MCP tools |
| --- | --- | --- | --- |
| Theme, shell, controls, screens (XAML + view models) | `winui-dev` | `winui-design` (+ bundled `winui-search.exe` for grounded control lookup), `winui-dev-workflow` (`BuildAndRun.ps1`) — `microsoft/win-dev-skills` v0.5.0 `f1028dd5` | Microsoft Learn `microsoft_docs_search` / `microsoft_code_sample_search` for NavigationView, ListView, CommandBar, InfoBar, ContentDialog, AutomationProperties, theme resources; Kanmer `get_doc_gates`, `set_ticket_doc` |
| Independent review of every UI PR | `pegasus-desktop-reviewer` | `winui-code-review` (analyzer families WUI2xxx/WUI3xxx, accessibility/theming checklists), `winui-design`, project skill `pegasus-desktop` | Microsoft Learn for API verification |
| UI automation, accessibility scans, screenshots, 200%/HC evidence | `pegasus-ui-verifier` | `winui-ui-testing` (`winapp ui` verbs, AutomationId audit, visual checklist); `axe-windows` CLI | — |
| View-model tests for vocabulary, problem mapping, state contract | `pegasus-test-engineer` | `code-testing-agent`, `run-tests`, `assertion-quality` — `dotnet/skills` `98f84851` | — |
| FRD-13 authoring and ticket linking | `pegasus-parity-researcher` | `kanmer-docs`, `kanmer-tickets` | Kanmer `link_doc`, `create_item` |

## 7. Risks and traps

- **Re-creating the web layout.** The proposal forbids preserving accidental
  web layout; the authority forbids marketing patterns. Mitigation: slice
  tickets reference the screen spec, not the Razor page; the reviewer checks
  for web idioms (breadcrumb overload, card grids, full-page spinners).
- **Design-authority review rules block merges.** Banned words, the four hard
  rules and "one primary button per region" are review rules without CI
  enforcement. Mitigation: DSK-06-10 adds a banned-words unit test over
  operator strings; the reviewer checklist is mandatory.
- **WinUI landmines** (vendored `winui-design`): `x:Bind` `OneTime` default;
  `TextBox` two-way needs `UpdateSourceTrigger=PropertyChanged`;
  `Converter={x:Null}` crashes; no `SizeToContent`; `AppWindow.Resize` takes
  physical pixels; attached properties need static setters; never
  `HighContrastAdjustment="None"`. Mitigation: analyzer from
  `winui-dev-workflow` (`Microsoft.WindowsAppSDK.Analyzers`) plus review.
- **Colour-only state.** Every status needs text and glyph; Dark/HC theme
  review must confirm contrast for amber/navy/green on their backgrounds.
- **Ellipsis and clipping at 200%.** The approved Inbox mockup records that
  a truncated subject "read as clipping"; the visual checklist fails runs on
  ellipsis. Mitigation: wrap rather than truncate in record headers; 200%
  review per screen.
- **Vocabulary drift.** Two label maps (web and desktop) would violate the
  one-list rule. Mitigation: relocation ticket in 05 before DSK-06-05.
- **Dark theme is unspecified.** Shipping Dark without operator confirmation
  risks an unreviewed palette. Mitigation: system-following Dark behind the
  same semantic roles, reviewed in DSK-06-02 with the operator before Phase 3
  exit.
- **Rail counts.** A shell-level zero is the stale placeholder the authority
  forbids; counts render only when the dashboard query returned them.
- **No motion tokens.** Do not invent durations or easing; only the
  indeterminate progress bar and reduced-motion static equivalent.
- **WebView2 creep.** Any WebView2 other than the report renderer is a
  violation of §23.2; the reviewer greps views for `WebView2`.

## 8. Documentation changes

- New FRD-13 (desktop operator experience) carrying the screen specifications
  and the desktop-specific state/keyboard/accessibility behaviour; FRD-12
  gains a cross-reference. Owner rows in `docs/capabilities.md` (new `DSK`
  family) per [00 · governance](../00-governance-and-workflow/README.md).
- `docs/design/README.md` amendments proposed through the design owner (not
  edited by UI tickets directly): (a) record the desktop's adoption of the
  tokens and the Dark/HighContrast decision; (b) resolve the 2px vs 6px radius
  discrepancy with `site.css`/`.stitch/DESIGN.md`; (c) register any new Lucide
  glyph the desktop needs (none expected); (d) note that the operator-label map
  relocates to a shared assembly and remains the single map.
- ADR-0100 (native WinUI 3 client) cites this area for the UI conventions;
  ADR-0108 (WebView2 report rendering) cites the "no WebView hosts Pegasus UI"
  rule restated here.
- `docs/current-architecture.md` gains the desktop presentation component row
  once the shell ships; `docs/operations.md` records the recorded-review
  evidence per release.
