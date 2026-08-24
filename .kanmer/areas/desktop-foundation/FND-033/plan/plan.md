# Plan — FND-033: Shell — NavigationView rail, title bar, status bar, navigation and dialog services

**Diff estimate: ~9 files, ~640 lines** (XAML dominates; excluding tests).

`docs/engineering.md` § Plan sizing (`:201`) requires the estimate first. Derived from the `files`
document, file by file, measured 2026-08-24:
`Shell/ShellPage.xaml` ~260 (the `NavigationView` with seven items, the restyled selection
indicator, the custom title bar, the status bar);
`Shell/ShellPage.xaml.cs` ~70 (drag regions and the selection-to-navigation hook only);
`Shell/ShellViewModel.cs` ~150;
`Services/INavigationService.cs` ~20 and its implementation ~55;
`Services/IDialogService.cs` ~20 and its implementation ~45;
`Hosting/PegasusHost.cs` +6 (two registrations);
`App.xaml.cs` +8 (root content). The four view-model tests land in
`tests/Pegasus.Desktop.ViewModelTests` (~120 lines) and are counted against that project.
No file under `src/Pegasus.Core`, `src/Pegasus.Infrastructure`, `src/Pegasus.Web` or
`src/Pegasus.Worker` is touched, and `docs/desktop/06-ui-design/screen-specs.md` is read, never
edited.

> **This ticket has one blocking open question.** See `open-questions`: which channel drives which
> environment-badge label. It blocks `leave-preparing`, `enter-review` and `enter-done` — correctly,
> because step 5 cannot write the badge without the answer. It does **not** block `leave-backlog`.
> Everything else in this plan is ready to execute the moment it is answered.

## Approach

Put every value the shell displays on a **`ShellViewModel`**, and put the three negative requirements
into the *type system* wherever possible rather than into review discipline. Specifically: rail
counts are `int?`, so "absent" and "zero" are different values and "never a shell-level `0`" becomes
a binding condition rather than a promise; the current-item treatment is a weight change **plus** a
2 px marker, so removing the marker leaves a visible difference; and every colour is a
`{ThemeResource}` key, so [[FND-034]] (plan handle `DSK-02-09`)'s ban on hard-coded colours has
something to enforce against.

The rejected alternative is holding shell state in `ShellPage.xaml.cs` and binding directly to it —
which is what the `winui-mvvm` template's `MainWindow` nudges toward. It would work and be shorter,
but every assertion in step 11 would then need a dispatcher and a launched window, turning four fast
unit tests into a UI-automation dependency on [[TEST-006]] (plan handle `DSK-08-06`), which does not
exist yet. Testability is the reason for the view model, and it is the reason [[FND-032]] (plan
handle `DSK-02-07`) built the host in a separate file too.

**The ownership reconciliation the Guardrails require, settled here before any XAML is written.**
[[DUI-004]] (plan handle `DSK-06-04`) is titled "Shell: NavigationView rail (236px), route order,
counts, title bar, environment badge, status bar" and names the same deliverable. It sits in area
`desktop-ui`, group `EPIC-007`, and has no documents yet (`docs: {}`). The split recorded here:

> **[[FND-033]] builds it; [[DUI-004]] verifies and dresses it.** This ticket owns
> `src/Pegasus.Desktop/Shell/**` and `src/Pegasus.Desktop/Services/**` — the XAML, the view model and
> the two services. [[DUI-004]] owns the design-side conformance pass over that same XAML against
> `docs/desktop/06-ui-design/screen-specs.md` and `tokens-and-theme.md`, and any refinement it finds;
> its own § Source of truth casts it as dressing "the shell scaffold … this ticket dresses", which
> agrees.
>
> The reason is the dependency direction already recorded in the plans, not a preference: plan 02 § 5
> row `DSK-02-08` states the acceptance as "**Routes from 06 navigable**" — 06 specifies, 02
> implements — and [[FND-041]] (plan handle `DSK-02-16`), the Phase 1 exit review, requires a
> launching, navigable native shell as a Phase 1 gate row. A shell that arrived only in area 06 would
> miss that gate.
>
> **The file path follows from that split**: this ticket creates the file, so it is
> `src/Pegasus.Desktop/Shell/ShellPage.xaml`, not `src/Pegasus.Desktop/Views/ShellPage.xaml` as
> [[DUI-004]] step 3 assumes. [[DUI-004]] dresses it where it is.
>
> **This is a two-sided agreement and one side is written here.** Before writing XAML, confirm
> [[DUI-004]] has not been taken, and record the same split in [[DUI-004]]'s plan document. If
> [[DUI-004]] has already been taken and started, **stop** and reconcile with its holder rather than
> building a second shell — the Guardrails say "do not build it twice", and a merge conflict is the
> good outcome; two divergent shells is the bad one.

## Governing docs

This ticket's `refs` array is **not** empty — unusually for this board it carries
`docs/frd/frd-12-operator-experience.md`, which genuinely binds. `get_doc_gates FND-033` also reports
`docs_todo: true`, so the conversion ADRs below are still to be authored.

**Meets — `docs/frd/frd-12-operator-experience.md` (the ticket's `ref`):**

| FRD-12 requirement | Where it says so | Met by |
| --- | --- | --- |
| "clear counts that link to their exact filtered work and **do not render stale zero placeholders**" | § Operator experience `:13-14` | Steps 6 and 11 — rail counts are `int?`; absent until the query returns; the view-model test asserts a null count renders nothing, not `0` |
| "actionable receiving, requests, Triage, case, query, and exception queues" reachable | § Operator experience `:9-10` | Step 3 — the seven-route rail, and step 7's navigation service as the only route to them |
| "administration for authorised accounts, roles, access, organisations, principals, configuration, and mailboxes" | § Operator experience `:18-20` | Step 3 — `Administration` present for the Administrator role, absent otherwise; the authority itself stays server-side ([[FND-046]], plan handle `DSK-04-10`) |
| "loading, empty, current, stale, unavailable, partial, failed, validation, conflict, and access-denied states" | § Operator experience `:21-23` | Step 10 — the five shell states as view-model states with placeholder content where area 04 owns the real screens |
| "keyboard, pointer, screen-reader, 200% zoom, forced-colour, and reduced-motion support" | § Operator experience `:24-25` | Steps 8, 9 and 12 — AutomationIds on every interactive control, the access-key contract, and the tier-7 manual keyboard pass |
| "One semantic action or state has one consistent icon across Pegasus; no decorative or generated replacement icon is used" | `:28` | Step 4 — glyphs come from [[DUI-003]] (plan handle `DSK-06-03`)'s `PathIcon` resource set; none is invented here |

> **New ADR** — ADR-0104 (online-required; no offline replication; bounded local cache only) is why
> the shell **shows connection state rather than pretending to work offline**: the status bar's
> "Disconnected — reconnecting" with saves disabled and existing content visible
> (`screen-specs.md:73-74`) is the online-required decision made visible. ADR-0104 has two claimants —
> [[FND-005]] (plan handle `DSK-00-05`) and [[FND-026]] (plan handle `DSK-02-01`); see [[FND-026]]'s
> plan for the ownership reconciliation.
> This plan is written to the decision as recorded in
> `docs/desktop/00-governance-and-workflow/README.md` § 3 (ADR set table) and
> `docs/desktop/02-architecture-and-foundation/README.md` § 3 decision 9; if the ADR lands
> differently this plan is revised before implementation.

The programme-level authorities that also bind today:

| Authority | Requirement | Met by |
| --- | --- | --- |
| Proposal § 14.2 Main shell | The frame every screen renders inside | Steps 3–6 |
| Proposal § 14.8 Notifications and errors | Prompts go through one mechanism | Step 7 (`IDialogService`) |
| Proposal § 14.9 Keyboard and accessibility | Keyboard reach and semantic labels | Steps 8, 9 |
| Proposal § 11.3 Connectivity handling | Disconnected is shown, not hidden; existing content stays visible | Step 6, rendering only — [[FND-047]] (plan handle `DSK-04-11`) owns the state machine |
| `docs/design/README.md:31-38` | The canonical authenticated route list, Operations as route 6, "Operations-first … selected on 2026-07-27" | Step 3 — see the research addendum for why `:474-475` omits Operations and does not contradict this |
| `screen-specs.md:59-60` | `PaneDisplayMode=Left`, `OpenPaneLength=236`, `IsPaneToggleButtonVisible=False` — "the authority's rail never hides" | Step 3 |
| `screen-specs.md:62-63` | Current item is a weight change **plus** a 2 px Collision-red left marker, never colour alone | Step 4 |
| `screen-specs.md:64-66` | Counts absent until the query returns; **never a shell-level `0`** | Steps 6, 11 |
| `screen-specs.md:27-30` | Deferred capabilities are **absent**, not disabled (first half of the rule; the visible-and-disabled half does not apply to rail items) | Step 3 |
| `screen-specs.md:31-39` | The AutomationId convention, 100% coverage, stable across releases | Step 8 |
| `screen-specs.md:80-81` | The five shell AutomationIds, verbatim | Step 8 |
| Plan 02 § 3 decision 9 | No framework on top of WinUI — a shell service, a navigation service, a dialog service and a handful of controls are the whole permitted surface | Step 7 and nothing beyond it |
| Plan 02 § 7 | "Do not recreate the web shell — the shell is a `NavigationView`, not a port of `_Layout.cshtml`" | § Approach; `_Layout.cshtml` is read for the route inventory only |
| `docs/design/README.md` § No explanatory copy and page economy (`:422`) | Labels, values, and at most one consequence sentence on a destructive action | Steps 5, 6, 13 — a shell that "explains" is a defect |
| **L-04** (locked) | Every ticket names its subagent, skills and MCP tools | § Routing below |
| `docs/engineering.md` § Plan sizing (`:201`) | Diff estimate first, from a measured inventory | The estimate above |
| `docs/engineering.md` § Required evidence tiers (`:72`), tier 7 | Keyboard, focus and error behaviour, semantic labels, text-plus-colour states; **"Automated axe results do not replace manual keyboard or assistive-technology review"** | § Verification, V3 and its honesty clause |

## Routing

Copied from the ticket body's `## Routing` block, as
`docs/desktop/00-governance-and-workflow/README.md` § Ticket template requires of the plan document
specifically.

- **Subagent**: `winui-dev` — `.codex/agents/winui-dev.toml`.
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `winui-design`
  (`.codex/skills/winui-design/SKILL.md`, with `winui-search.exe` for control lookup) →
  `winui-dev-workflow` (`.codex/skills/winui-dev-workflow/SKILL.md`) → `winui-code-review`
  (`.codex/skills/winui-code-review/SKILL.md`), all win-dev-skills v0.5.0 `f1028dd5`.
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`,
  `move_item`); Microsoft Learn (`microsoft_docs_search` for `NavigationView PaneDisplayMode`,
  `AppWindow TitleBar SetDragRectangles`, `AutomationProperties.AutomationId`, `ContentDialog`).
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute` →
  `kanmer-review` → `kanmer-verify` → `kanmer-closeout`. Call `get_doc_gates <id>` before every move;
  a move crosses at most one gated boundary.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md`
  § Repository task workflow step 5).

## Steps

These refine the ticket body's thirteen implementation steps: same order, same ownership, same file
paths, adding the *how* the body leaves out.

1. **Orient, and settle ownership first.** Read `docs/desktop/06-ui-design/screen-specs.md:41-81`
   § Shell **in full** — it is the specification, not a summary — plus `:27-39` (absent-vs-disabled
   and the AutomationId convention), and `docs/design/README.md` § No explanatory copy and page
   economy (`:422`). Read `src/Pegasus.Web/Pages/Shared/_Layout.cshtml:56-114` for the **route
   inventory only** and then close it. Confirm [[DUI-004]] has not been taken, apply the
   reconciliation recorded in § Approach, and record the same split in [[DUI-004]]'s plan. Then
   `get_doc_gates FND-033` and `take_ticket` on branch `task/desktop-shell` from `origin/dev`.
2. **Confirm the control API before writing XAML.** Load `winui-design` and run `winui-search.exe`
   for `NavigationView` — its properties **and its selection-indicator template parts**, which step 4
   needs. Do not guess property names; the vendored skill exists precisely because the WinUI surface
   is large and changes between releases.
3. **Write the rail.** `src/Pegasus.Desktop/Shell/ShellPage.xaml` with a `NavigationView`:
   `PaneDisplayMode="Left"`, `OpenPaneLength="236"`, `IsPaneToggleButtonVisible="False"`, and seven
   `NavigationViewItem`s in exactly this order — Dashboard, Inbox, Upload, Queues, Cases, Operations,
   Administration. (The seven-route list is settled: `docs/design/README.md:31-38` is canonical and
   includes Operations as route 6; the abbreviated restatement at `:474-475` lists *shipped* routes,
   which `:1089-1091` reconciles. See the research addendum — do not re-open it.) Bind
   `Administration`'s and `Inbox`'s **visibility** to `ShellViewModel` properties; do not hard-code
   them visible and do not render them **disabled**.
   `src/Pegasus.Web/Pages/Shared/_Layout.cshtml:6` records why in the web application's own words:
   *"disabled nav span: a permanently inert item says the product is broken"*, and
   `screen-specs.md:27-28` agrees. The role signal behind the binding is placeholder state until
   [[FND-046]] supplies the real `StaffAccessRight`.
4. **Restyle the selection indicator.** A weight change on the current item **plus** a 2 px
   Collision-red left marker. Colour alone must never be the only signal — that is
   `screen-specs.md:62-63`, `docs/design/README.md`, and the `winui-code-review` theming checklist,
   three independent sources. Every colour and size comes from a `{ThemeResource}` key defined by
   [[FND-034]] and valued by [[DUI-001]] (plan handle `DSK-06-01`); **no hex literal and no raw
   `FontSize`** may appear in any view in this ticket. Glyphs come from [[DUI-003]]'s `PathIcon` set —
   FRD-12 `:28` requires one consistent icon per semantic action across Pegasus, so none is invented
   here.
5. **Build the title bar.** ⚠ **Blocked on the open question until it is answered** — see
   `open-questions`. Everything except the badge's label mapping can be built: logo asset, connection
   glyph **plus word**, version and channel, and a user menu with Change password, Sign out,
   Diagnostics. The **environment badge** is shown only outside production and reads the channel from
   the option [[FND-032]] registered (one read, bound to a view-model property, never a second literal
   read of `Channel`) — but *which label each channel renders* is the open question:
   `screen-specs.md:67-69` names three labels ("Pilot", "Test/UAT", "Development") while plan 02 § 3
   decision 7 defines three channels (`pilot` | `production` | `local`), and with `production` hiding
   the badge, two labels compete for `local`. Build the badge control and its binding; leave the
   label map to the answer. Run `microsoft_docs_search` for `AppWindow TitleBar` drag-region semantics
   **before** implementing the custom title bar: it needs explicit drag rectangles or the operator
   cannot move the window (A-FND033-2).
6. **Build the status bar**: connection state, last sync time rendered in **Europe/London**,
   background transfer summary that opens the transfer pane, and update availability. Rail counts are
   `int?` on the view model; when null the count element is **absent**, not `0`. This is FRD-12
   `:13-14` ("do not render stale zero placeholders") as much as it is `screen-specs.md:64-66`, and
   the nullable type is what makes it a binding condition rather than a promise. The connectivity
   string is exactly "Disconnected — reconnecting" (`screen-specs.md:73-74`); [[FND-047]] decides
   when it is true.
7. **Create the two services.** `src/Pegasus.Desktop/Services/INavigationService.cs` and
   `IDialogService.cs` with their implementations, registered in `Hosting/PegasusHost.cs`. Route
   **every** rail item through the navigation service; a `Frame.Navigate` call anywhere else is a
   defect review must catch, because it will not fail any build. [[FND-032]] deliberately registered
   no placeholder for these (`docs/engineering.md` § Abstractions `:113` — no dormant scaffolding),
   so the interface and its first real caller land together here. Plan 02 § 3 decision 9 bounds the
   surface: a shell service, a navigation service, a dialog service and a handful of controls —
   nothing more.
8. **Set the AutomationIds.** A unique `AutomationProperties.AutomationId` on **every** interactive
   control, using exactly the spec names for the shell elements: `Shell.Rail.<Route>` (one per rail
   item, e.g. `Shell.Rail.Cases`), `Shell.Title.Environment`, `Shell.Title.User`,
   `Shell.Status.Connection`, `Shell.Status.Update`. Dialog elements follow the same convention
   (`Dialog.Reason.Text`, `Dialog.Reason.Confirm`, `Dialog.Reason.Cancel`, `screen-specs.md:36-37`).
   These are a **contract with two other tickets** — [[TEST-006]]'s harness and [[DUI-015]] (plan
   handle `DSK-06-15`)'s 100%-coverage audit both read them — so a renamed id here is a silent break
   in another area's lane.
9. **Wire the shell's keyboard subset.** Rail access keys `Alt+D`, `Alt+I`, `Alt+U`, `Alt+Q`,
   `Alt+C`, `Alt+O`, `Alt+A`; `Ctrl+K` navigates to Cases search; `F5` refreshes the current screen.
   Verify tab order reaches every rail item **and** the user menu. That is the whole subset
   `screen-specs.md:78-79` assigns to the shell; `Ctrl+N`, `Ctrl+S`, `Ctrl+W`, `Esc` and the rest
   belong to [[DUI-014]] (plan handle `DSK-06-14`) and
   `docs/desktop/06-ui-design/keyboard-and-accessibility.md`. A subset is not a conflict. If two
   access keys collide, record it and raise it with [[DUI-014]] — do not silently pick a different
   letter.
10. **Implement the five shell states** as view-model states with placeholder content:
    authenticated; unauthenticated (login replaces the shell); update-required and blocked
    (full-window, **no rail**); disabled account; stale role. Area 04 owns the real screens
    ([[FND-044]] plan handle `DSK-04-08`, [[FND-045]] plan handle `DSK-04-09`) — **do not implement
    authentication here.** The value this step delivers is that the shell can *be* in each state and
    a test can assert it.
11. **Write the view-model tests** in `tests/Pegasus.Desktop.ViewModelTests` ([[FND-038]], plan
    handle `DSK-02-13`): rail visibility for administrator vs non-administrator; status-bar
    connection text for connected and disconnected; the navigation service routing to each of the
    seven routes; and — the one the negative requirement needs — a **null** rail count rendering
    nothing rather than `0`. The **environment-badge test** ("hidden in the production channel and
    shown otherwise") can be written for the hidden/shown behaviour now, but its *label* assertions
    wait on the open question. If [[FND-038]] has not landed, sequence it first and record the
    sequencing.
12. **Verify visually and by keyboard.** Run
    `pwsh .codex/skills/winui-dev-workflow/BuildAndRun.ps1 src/Pegasus.Desktop/Pegasus.Desktop.csproj`
    asynchronously, navigate every rail item, press each access key, drag the window by its custom
    title bar, and capture screenshots for the proof. If [[TEST-006]]'s `winapp ui` harness exists,
    run its shell smoke batch **as well**; if it does not — and it does not today (`ls tests` returns
    only the three existing projects) — record in the proof that the evidence is a **manual** pass
    and name [[TEST-006]] as the automation follow-up. Tier 7 says automated checks do not replace a
    manual keyboard review in either case.
13. **Review, simplify, open the PR.** Run the `winui-code-review` checklist over the new XAML —
    theming, no raw `FontSize`, no hex literals, AutomationIds present — then
    `dotnet build ./Pegasus.slnx --configuration Release` for the authoritative zero-warning gate.
    Run the simplification pass over this branch's own diff, record it under a dated
    `## Simplification pass` heading in this document, and open the PR into `dev`.

## Verification

Evidence tier **7 — Browser/accessibility** (`docs/engineering.md` § Required evidence tiers, `:72`),
read as its desktop equivalent, as the ticket body states: keyboard, focus and error behaviour,
semantic labels, and text-plus-colour states must be **demonstrated**. That tier's own sentence
governs the proof: *"Automated axe results do not replace manual keyboard or assistive-technology
review."*

The `proof` document is produced from these five outputs.

- **V1.** `dotnet build ./Pegasus.slnx --configuration Release` — expected exit 0 and
  `0 Warning(s)`. The authoritative gate: it is what `.github/actions/dotnet-build/action.yml:22-27`
  runs and, unlike `BuildAndRun.ps1`, it sees the repository-root `Directory.Build.props`.
- **V2.** `dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release`
  — expected to cover, at minimum:
  - **Rail visibility**: `Administration` absent for a non-administrator, present for an
    administrator; `Inbox` absent when its capability is not composed. Assert **absent**, not
    disabled.
  - **Environment badge**: hidden when the channel is `production`, shown otherwise — with the label
    assertions added once the open question is answered.
  - **Status bar**: connection text for connected and disconnected, the disconnected case matching
    the exact spec string.
  - **Counts**: a `null` count renders no count element; a count of `0` from the query is a separate
    case and must be distinguishable in the test from `null`. A test that only checks `null` passes
    even if both render `0`.
  - **Navigation**: each of the seven routes reached through `INavigationService`.
- **V3.** A **manual** keyboard and navigation pass, recorded step by step: every rail item reached
  by `Tab`; each of `Alt+D/I/U/Q/C/O/A` selecting its item; `Ctrl+K` reaching Cases search; `F5`
  refreshing; the user menu reachable; the window draggable by its custom title bar. Screenshots of
  the shell in at least the authenticated and blocked states. If [[TEST-006]]'s harness exists, its
  shell smoke batch runs **in addition** and its log is attached.
- **V4.** The `winui-code-review` checklist output over the new XAML — expected: no hex literal, no
  raw `FontSize`, `{ThemeResource}` used throughout, an AutomationId on every interactive control.
- **V5.** `grep -rniE '#[0-9a-f]{6}|FontSize="[0-9]' src/Pegasus.Desktop/Shell/` — expected **no
  matches**. This is the executable form of two of the ticket's negative requirements, and it is the
  check [[FND-034]] later turns into a permanent ban.

**Honesty clauses for the proof.**

- Record the answer the open question received and which label map was implemented, so a reader can
  see the badge text was decided rather than assumed.
- Say plainly whether the keyboard evidence is manual or automated, and name [[TEST-006]] as the
  follow-up if manual. `docs/runbook.md:38` ("record the platform actually exercised") and tier 7's
  own sentence both require this.
- A green `BuildAndRun.ps1` is **not** the same claim as a green `dotnet build`: the script injects a
  project-level `Directory.Build.props` (`.codex/skills/winui-dev-workflow/BuildAndRun.ps1:142-172`,
  existence test at `:152` against the project directory only) that shadows the root one and drops
  `TreatWarningsAsErrors`. V1 is authoritative.
- No CI job builds a desktop project until [[FND-040]] (plan handle `DSK-02-15`) lands, so a green
  `repository-check` run says nothing about this ticket.
- State which shell states were rendered with placeholder content and which were not rendered at
  all. "Implemented as a view-model state" and "shown to a human" are different claims.

## Risks / open questions

- **One blocking open question exists on this ticket, and it is recorded in `open-questions`:
  which channel drives which environment-badge label.** `screen-specs.md:67-69` names three
  non-production labels ("Pilot", "Test/UAT", "Development") while plan 02 § 3 decision 7 defines
  three channels (`pilot` | `production` | `local`) and plan 04 § 3 item 8 (`:198-199`) confirms the
  package carries only the channel name — so with `production` hiding the badge, **two labels compete
  for `local` and one label has no channel at all**. This ticket's `## Documentation changes` binds
  the author to record a spec ambiguity "as an open question in the ticket, not as an edit", and the
  badge's text is operator-facing copy governed by `docs/design/README.md`, which the authority order
  in `docs/desktop/00-governance-and-workflow/README.md` § 3 places above these plans. It is written
  as an **unticked** item, which blocks `leave-preparing`, `enter-review` and `enter-done` — correctly,
  because step 5 cannot write the badge without it. It never blocks `leave-backlog`. The default that
  would otherwise be taken (`local` → "Test/UAT", "Development" retired) is recorded in
  `open-questions` along with the three candidate resolutions, so answering it is a single decision.
- **Risk — two shells get built.** [[DUI-004]] names the same deliverable and has no documents yet.
  *Mitigation*: the reconciliation in § Approach, applied at step 1 **before** any XAML, and recorded
  in [[DUI-004]]'s plan as well. If [[DUI-004]] is already taken and started, stop and reconcile with
  its holder. This is a scope boundary with a named sibling ticket that the ticket body directs to be
  settled in this plan — settled here, not opened as a question. The file path follows from it:
  `Shell/ShellPage.xaml`, not `Views/ShellPage.xaml`.
- **Risk — A-FND033-1: the selection indicator may not restyle without a full template override.**
  *Mitigation*: `winui-search.exe` on the template parts at step 2 settles it before XAML is written.
  *If wrong*: a larger override, its size recorded — but "weight change plus marker, never colour
  alone" does not soften.
- **Risk — A-FND033-2: a custom title bar can leave the window undraggable.** *Mitigation*:
  `microsoft_docs_search` for `AppWindow TitleBar` drag-region semantics at step 5, and dragging the
  window in V3. *If wrong*: badge and menu move below the title bar and the deviation is recorded —
  never a window the operator cannot move.
- **Risk — A-FND033-3: the seven `Alt+` access keys may collide.** *Mitigation*: press each in V3.
  *If wrong*: record it and raise it with [[DUI-014]], which owns the keyboard map; do not silently
  substitute a letter, because [[DUI-014]] and [[TEST-007]] (plan handle `DSK-08-07`) both assume the
  specified set.
- **Risk — a rail count of `0` and an absent count become the same thing.** The requirement is
  negative and a rendering test passes trivially if both render as nothing. *Mitigation*: `int?` on
  the view model, and V2's explicit requirement that `null` and `0` be **distinguishable in the
  test**.
- **Risk — a second navigation mechanism appears later.** A `Frame.Navigate` in a page's code-behind
  will not fail any build. *Mitigation*: step 7 makes the service the only mechanism, and the risk is
  recorded here so [[FEAT-001]] (plan handle `DSK-05-01`) onward and the reviewer both know to look.
- **Risk — a `{ThemeResource}` key referenced here does not exist yet.** [[FND-034]] wires the
  dictionaries and [[DUI-001]] owns the values; a missing key is a **runtime** XAML failure, not a
  compile error. *Mitigation*: V5's grep plus the launch in V3 — a shell that loads is evidence the
  keys resolve. If [[FND-034]] has not landed, record which keys are provisional.
- **Sequencing, recorded not resolved — [[FND-038]] must land before step 11.**
  `tests/Pegasus.Desktop.ViewModelTests` does not exist yet and `tests/Pegasus.ArchitectureTests`
  targets `net10.0`, so it cannot host these tests. Sequence it first; do not duplicate the scaffold.
- **Sequencing, recorded not resolved — [[FND-030]] (plan handle `DSK-02-05`) and [[FND-032]] must
  both have landed.** The plan arrow names only [[FND-032]], but the project itself comes from
  [[FND-030]].
- **Scope boundary, not an open question — the rail-route list.** Settled on inspection and recorded
  in the research addendum: `docs/design/README.md:31-38` is canonical and includes Operations as
  route 6; `:474-475` lists *shipped* routes, which `:1089-1091` reconciles; and
  `src/Pegasus.Web/Pages/Operations/Index.cshtml` exists. Do not re-open it.
- **Scope boundary, not an open question — authentication, the real role, the real connectivity
  state, the full keyboard map, the counts query, and the token values.** [[FND-044]], [[FND-045]],
  [[FND-046]], [[FND-047]], [[DUI-014]], [[FEAT-001]], [[FND-034]] and [[DUI-001]] respectively.
- **No settled operator decision is reopened.** D-002, D-003, D-004 and the Send to AI (AI-09)
  recorded exclusion all stand untouched by this ticket.

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over this branch's own
diff before the PR, recorded here under a dated heading._
