# EPIC-007 — Area 06, UI design (board area `desktop-ui`, prefix `DUI`)

Read this once before working any `DSK-06-*` ticket. It carries what binds the whole
epic; the ticket carries the work.

## What the area delivers

The desktop presentation layer's rules and shared parts: theme resource dictionaries,
the Lucide/logo asset set, the shell (rail, title bar, status bar), the shared controls
(`StatusChip`, data table, form field, `ReasonDialog`, `ProblemInfoBar`, provenance
glyph, page header + freshness), the operator-vocabulary binding, the keyboard map, and
the accessibility automation plus the ten recorded reviews. It builds **no screen** —
screens are plan 05 slices that point at these specs and reuse these controls.

## Proposal coverage

§14.1 design character · §14.2 shell · §14.3–14.7 screens (via the specs) · §14.8
notifications and errors · §14.9 keyboard and accessibility · §14.10 theme system ·
§11.1 local preferences · §13.1–13.10 capability groups · §22.2 accessibility testing ·
§23.2 native verification (no WebView hosts Pegasus UI).

## What binds every ticket in this epic

- **`docs/design/README.md` is the authority, not proposal §14.** Where they differ, the
  authority wins and the difference is already recorded in the area plan §3 table. The
  recorded deviations are: solid surfaces (Mica only behind title bar/rail, contrast
  permitting, no acrylic); `Ctrl+K` focuses the Cases search box — there is no separate
  global-search screen; `InfoBar` + status bar + transfer pane instead of a notification
  centre or toasts; minimum window width 1280 with 1024–1279 reordering.
- **Operator copy rules.** A UI that explains itself is a defect (`AGENTS.md` §Simplicity
  rails, `docs/design/README.md:422-445`): a field is a label and a control; no
  how-it-works copy; only populated sections render; filters are dropdowns and tables
  sort newest first. The banned-words list (`:412-420`) and the closed necessary-copy
  list (`:400-409`) are closed sets — never extend either from a ticket.
- **L-03** WebView2 exists only as the isolated, never-visible report renderer
  (ADR-0108). No `WebView2` element may appear in any XAML view; the reviewer greps.
- **L-04** every ticket names its subagent, skills and MCP tools. **L-01** all data comes
  from the evolved `Pegasus.Web` gateway. **L-02** verification runs on the local
  Test/UAT stack or the pilot ring — asking for an Azure test resource is out of bounds.
- **Assumptions still open**: the Dark palette (the authority is light-only) and the 14px
  section heading standing in for 15/700 — both confirmed with the operator in DSK-06-02.
  The 2px vs 6px radius discrepancy with `site.css`/`.stitch/DESIGN.md` is flagged to the
  design owner; the desktop ships 2 regardless.
- **No Azure write arises anywhere in this area.**

## Exit gate and what proves it

Every shipped screen passes the design-authority review rules (four hard rules, banned
words, status vocabulary, one primary action per region, no colour-only state), recorded
by `pegasus-desktop-reviewer` in the ticket plan; the `winapp ui` AutomationId coverage
audit reports 100%; the `axe-windows` scan has no critical findings; the scripted
keyboard journey passes; and the ten recorded reviews exist per screen per release
candidate (`artifacts/a11y/<release>/<Screen>.md`). Tier 7 rule: automated results never
replace the manual keyboard and assistive-technology passes — both are recorded.

## Routing for this epic

| Work | Subagent | Skills (pinned source) | MCP |
| --- | --- | --- | --- |
| Theme, shell, controls, XAML + view models | `winui-dev` (`.codex/agents/winui-dev.toml`) | `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `winui-design` + `winui-search.exe` → `winui-dev-workflow` (`BuildAndRun.ps1`) — `.codex/skills/`, `microsoft/win-dev-skills` v0.5.0 `f1028dd5` | Microsoft Learn `microsoft_docs_search`, `microsoft_code_sample_search`; Kanmer `get_doc_gates`, `set_ticket_doc` |
| Independent review of every UI PR | `pegasus-desktop-reviewer` | `winui-code-review`, `winui-design`, `pegasus-desktop` | Microsoft Learn |
| UI automation, a11y scans, 200%/HC evidence | `pegasus-ui-verifier` | `winui-ui-testing` (`winapp ui` verbs, AutomationId audit, visual checklist); `AxeWindowsCLI` | — |
| View-model tests (vocabulary, problems, state) | `pegasus-test-engineer` | `code-testing-agent`, `run-tests`, `assertion-quality` — `dotnet/skills` `98f84851` | — |
| FRD-13 authoring and ticket linking | `pegasus-parity-researcher` | `kanmer-docs`, `kanmer-tickets` — `.grok/skills/` | Kanmer `link_doc`, `search_items` |

Never load `winui-wpf-migration`, `winui-session-report`, `entra-app-registration`, the
`dotnet-maui`/`dotnet-blazor` plugins, or any `azure-*` deployment skill
(`docs/desktop/12-agent-tooling/skill-routing.md` § Not applicable — do not load).

## Traps (area plan §7)

Re-creating the web layout (breadcrumb overload, card grids, full-page spinners) ·
design-authority rules block merges and have almost no CI enforcement · WinUI landmines:
`x:Bind` defaults to `OneTime`, `TextBox` two-way needs
`UpdateSourceTrigger=PropertyChanged`, `Converter={x:Null}` crashes at runtime, no
`SizeToContent`, theme dictionaries must name `Light`/`Dark`/`HighContrast` and never
`Default`, never `HighContrastAdjustment="None"` · colour-only state · ellipsis and
clipping at 200% (wrap, do not truncate) · vocabulary drift (two label maps) · a
shell-level `0` count is the stale placeholder the authority forbids · no motion tokens
exist — do not invent durations or easing · WebView2 creep.

## Read these before starting any ticket here

1. `docs/desktop/06-ui-design/README.md` (§3 reconciliation, §4 exit gate, §7 traps)
2. `docs/desktop/06-ui-design/tokens-and-theme.md`
3. `docs/desktop/06-ui-design/screen-specs.md` (rules preamble, AutomationId convention,
   cross-cutting state contract)
4. `docs/desktop/06-ui-design/keyboard-and-accessibility.md`
5. `docs/design/README.md` — `:160-180`, `:182-293`, `:296-376`, `:396-445`, `:461-504`,
   `:582-623`, `:764-808`
6. `AGENTS.md` §Simplicity rails and §Repository task workflow;
   `docs/engineering.md` §Required evidence tiers (tiers 1, 7, 10)
7. `docs/desktop/12-agent-tooling/skill-routing.md` and
   `.agents/skills/project/pegasus-desktop/SKILL.md`
