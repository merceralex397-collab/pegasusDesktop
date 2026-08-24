# Plan — FND-033: Shell — NavigationView rail, title bar, status bar, navigation and dialog services

**Diff estimate: ~10 files, ~620 lines.**

`docs/engineering.md` § Plan sizing (`:201`) requires the estimate first. Derived from the files
document: `Shell/ShellPage.xaml` ~170 (rail, selection-indicator restyle, content host);
`Shell/ShellPage.xaml.cs` ~60; `Shell/ShellViewModel.cs` ~150 (rail visibility, badge, connection,
status values, six states); `Shell/TitleBar.xaml(.cs)` ~90; `Shell/StatusBar.xaml(.cs)` ~70;
`Services/INavigationService.cs` + implementation ~55; `Services/IDialogService.cs` + implementation
~45; `Hosting/PegasusHost.cs` +6 (two registrations). The four view-model tests land in
`tests/Pegasus.Desktop.ViewModelTests` and are counted against that project.

## Approach

Build the shell as a **scaffold plus two services** and leave every visual value to a
`{ThemeResource}` key, so that [[DUI-004]] (plan handle `DSK-06-04`) can dress it in place without
either ticket rewriting the other. That split is not this plan's invention — it is settled by
[[DUI-004]]'s own body, whose § Source of truth lists "`DSK-02-08` — the shell scaffold, navigation
and dialog services **this ticket dresses**". The rejected alternative is building the fully dressed
shell here and reducing [[DUI-004]] to a review: it would put token binding, count binding, the
checksummed logo and the `winapp ui` scripts in a ticket that depends on none of the area-06 work
those need, and it would leave [[DUI-004]] with nothing to do but re-open the same file.

The one decision this plan must actually take is the **path**, because the two bodies disagree:
this ticket says `src/Pegasus.Desktop/Shell/ShellPage.xaml`, [[DUI-004]] step 3 says
`src/Pegasus.Desktop/Views/ShellPage.xaml`. **This ticket creates the file, so `Shell/` is the path**,
and [[DUI-004]] dresses it there. Recorded in § Risks so the sibling's agent sees the agreement rather
than creating a second page.

## Governing docs

`refs` carries one entry, `docs/frd/frd-12-operator-experience.md`, and `get_doc_gates FND-033`
reports `docs_todo: true`.

| Governing doc | How this plan meets it |
| --- | --- |
| `docs/frd/frd-12-operator-experience.md` (`refs`) | **Meets.** The shell is the operator's frame: steps 3–6 deliver the rail, title bar and status bar the operator experience requires; step 10 delivers the state set; step 9 the keyboard subset; step 8 the automation identifiers that make the experience testable. `docs/desktop/06-ui-design/screen-specs.md:10` records that "FRD-13 adopts these blocks as its sections", so the screen spec is the operative statement of the FRD's shell requirement until [[FND-008]] (plan handle `DSK-00-08`) writes FRD-13. |

> **New ADR** — ADR-0104 (online-required; no offline replication; bounded local cache only) is why
> the shell **shows connection state** rather than pretending to work offline. It is authored by
> [[FND-005]] (plan handle `DSK-00-05`) and also claimed by [[FND-026]] (plan handle `DSK-02-01`) —
> see [[FND-026]]'s plan for the ownership reconciliation.
> This plan is written to the decision as recorded in
> `docs/desktop/00-governance-and-workflow/README.md` § 3 (ADR set table); if the ADR lands
> differently this plan is revised before implementation.

Because `refs` carries only one entry, these are the other authorities that bind today:

| Authority | Requirement | Met by |
| --- | --- | --- |
| `docs/design/README.md:30-38` | The seven authenticated routes in order, settled by operator decision 2026-08-04 | Step 3 |
| `docs/design/README.md:586` | Current route signalled by weight **and** a 2 px Collision-red left marker, never colour alone; `Inbox` absent, never disabled, when not composed | Steps 3, 4 |
| `docs/design/README.md:172` | A not-composed capability is absent, never disabled or "Unavailable"; every operator-facing time renders Europe/London through the label map and `ToLocalTime()` is never correct | Steps 3, 6 |
| `docs/design/README.md:489-491` | `0` is a current result and never a substitute for not-yet-loaded data | Step 6 |
| `docs/design/README.md:169-170` | One title, no lede, guidance only beside a consequential control and then one sentence; the banned operator vocabulary | Steps 5, 6, 13 |
| `docs/design/README.md:764-772` | The complete UI state contract the shell states apply | Step 10 |
| `docs/desktop/06-ui-design/screen-specs.md` § Shell | The rail settings, badge, status bar, states, keyboard subset and five AutomationIds | Steps 3–10 |
| `docs/desktop/06-ui-design/screen-specs.md` § AutomationId convention | The naming grammar, and 100 % coverage as a hard audit | Step 8 |
| Proposal § 14.2, § 14.8, § 14.9, § 11.3 | Main shell, notifications and errors, keyboard and accessibility, connectivity handling | Steps 3–10 |
| Plan 02 § 3 decision 9 | No desktop framework on top of WinUI — a shell service, a navigation service, a dialog service and a few controls | Step 7 |
| Plan 02 § 7 | "Do not recreate the web shell" | § Approach and § Risks |
| L-01 (locked) | Rail counts come from the gateway, never a direct database read | Step 6 defers the binding to [[DUI-004]]; the shell reads no database |
| L-04 (locked) | Every ticket names its subagent, skills and MCP tools | § Routing |
| `docs/engineering.md` § Required evidence tiers (`:72`), tier 7 | Keyboard, focus and error behaviour, semantic labels, text-plus-colour states; automated checks do not replace a manual keyboard review | § Verification |

## Routing

Copied from the ticket body's `## Routing` block, as
`docs/desktop/00-governance-and-workflow/README.md` § Ticket template requires of the plan document
specifically.

- **Subagent**: `winui-dev` — `.codex/agents/winui-dev.toml` (verified present).
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `winui-design`
  (`.codex/skills/winui-design/SKILL.md`, with `.codex/skills/winui-design/winui-search.exe` for
  control lookup) → `winui-dev-workflow` (`.codex/skills/winui-dev-workflow/SKILL.md`) →
  `winui-code-review` (`.codex/skills/winui-code-review/SKILL.md`), all win-dev-skills v0.5.0
  `f1028dd5`.
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`,
  `move_item`); Microsoft Learn (`microsoft_docs_search` for `NavigationView PaneDisplayMode`,
  `AppWindow TitleBar SetDragRectangles`, `AutomationProperties.AutomationId`, `ContentDialog`).
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute` →
  `kanmer-review` → `kanmer-verify` → `kanmer-closeout`. Call `get_doc_gates <id>` before every move;
  a move crosses at most one gated boundary. **Note**: this ticket carries an unticked item in its
  `open-questions` document, which blocks `leave-preparing`, `enter-review` and `enter-done` — and
  nothing else. It does **not** block `leave-backlog`.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement
  (`AGENTS.md` § Repository task workflow step 5).

## Steps

These refine the ticket body's thirteen steps: same order, same ownership, same paths.

1. **Orient.** Read `docs/desktop/06-ui-design/screen-specs.md` § Shell **in full** — it is the
   specification, not a summary — plus `docs/design/README.md:30-46` (the canonical seven-route list),
   `:169-173`, `:489-491`, `:586` and `:764-772`. Then `get_doc_gates FND-033` and `take_ticket` on
   branch `task/desktop-shell` from `origin/dev`.
   **Before writing XAML**, settle the two overlaps in this document (see § Risks): the file path
   against [[DUI-004]], and the keyboard subset against [[DUI-014]] (plan handle `DSK-06-14`).
2. **Confirm the API before writing it.** Load `winui-design` and use
   `.codex/skills/winui-design/winui-search.exe` to confirm the current `NavigationView` surface and
   its selection-indicator template parts; do not guess property names. Record in this document that
   the left rail is the authority's shape (`docs/design/README.md:30-38`, `:474`), not the reflexive
   choice the skill's anti-pattern table warns about.
3. **`src/Pegasus.Desktop/Shell/ShellPage.xaml`** — a `NavigationView` with `PaneDisplayMode="Left"`,
   `OpenPaneLength="236"`, `IsPaneToggleButtonVisible="False"`, and `NavigationViewItem`s in exactly
   this order: **Dashboard, Inbox, Upload, Queues, Cases, Operations, Administration**. This is the
   canonical list at `docs/design/README.md:30-38`; the abbreviated restatement at `:474-475` omits
   Operations and `:1089-1091` reconciles the two — do not "correct" the rail against `:474`.
   `Administration` is added only for the Administrator role and `Inbox` only when its capability is
   composed: bind visibility to view-model state, do **not** hard-code them visible, and make them
   **absent**, never disabled (`docs/design/README.md:172`, `:586`). Also set
   `IsSettingsVisible="False"` — Diagnostics/Settings is reached from the user menu, not the rail
   ([[DUI-004]] step 3).
4. **Selection signal.** Restyle the `NavigationView` selection indicator so the current item shows a
   weight change **plus** a 2 px Collision-red left marker. Colour alone is never the only signal
   (`docs/design/README.md:586`; `winui-code-review` theming checklist). Use `{ThemeResource}` keys
   from [[FND-034]] (plan handle `DSK-02-09`) — **no hex literal may appear in any view**. Confirm the
   accessible selection state survives the restyle; if it does not, hide the built-in indicator and
   add the marker inside the item template rather than losing the automation state.
5. **Title bar.** Logo slot (the checksummed asset itself is [[DUI-003]], plan handle `DSK-06-03`);
   environment badge shown only outside production, reading the channel option registered by
   [[FND-032]] (plan handle `DSK-02-07`); connection glyph plus word; version and channel; and a user
   menu with Change password, Sign out, Diagnostics. Use `microsoft_docs_search` for
   `AppWindow TitleBar` drag-region semantics before implementing the custom title bar, and keep a
   working drag region. **The badge's label values are the subject of this ticket's open question** —
   do not write them until it is answered.
6. **Status bar.** Connection state, last sync time rendered **Europe/London through the shared
   operator-label map** (`docs/design/README.md:172` — "`ToLocalTime()` is never correct"), background
   transfer summary that opens the transfer pane, and update availability. Rail counts come from the
   dashboard rail-counts query and are simply **absent** until it returns — never a shell-level `0`
   (`docs/design/README.md:489-491`). The count *binding* is [[DUI-004]] step 6 with its own test
   `RailCountIsAbsentUntilTheQueryReturns`; this ticket provides the absent-capable view-model shape
   and does not duplicate that test.
7. **`src/Pegasus.Desktop/Services/INavigationService.cs` and `IDialogService.cs`** plus their
   implementations, registered in `Hosting/PegasusHost.cs`. Every rail item routes through the
   navigation service; **no other navigation mechanism may exist**, and no other prompt mechanism than
   the dialog service. These are the two interfaces [[FND-032]] step 5 deliberately did not create
   empty — this is where they gain a real caller, which is what `docs/engineering.md` § Abstractions
   (`:113`) requires.
8. **AutomationIds.** A unique `AutomationProperties.AutomationId` on every interactive control, using
   exactly the spec names: `Shell.Rail.<Route>` per rail item (`Shell.Rail.Dashboard`,
   `Shell.Rail.Inbox`, `Shell.Rail.Upload`, `Shell.Rail.Queues`, `Shell.Rail.Cases`,
   `Shell.Rail.Operations`, `Shell.Rail.Administration`), `Shell.Title.Environment`,
   `Shell.Title.User`, `Shell.Status.Connection`, `Shell.Status.Update`. A missing id breaks the
   [[TEST-006]] (plan handle `DSK-08-06`) harness and the accessibility audit, which must report
   100 % (`screen-specs.md` § AutomationId convention).
9. **Keyboard — the shell subset only.** Rail access keys `Alt+D`, `Alt+I`, `Alt+U`, `Alt+Q`,
   `Alt+C`, `Alt+O`, `Alt+A`; `Ctrl+K` → Cases search; `F5` refresh. Verify tab order reaches every
   rail item and the user menu. The **full** map — `Ctrl+N`, `Ctrl+S`, `Ctrl+W`, `Esc`,
   `Ctrl+1`…`Ctrl+8`, `F6` region cycling and the five-region focus order — lives in
   `docs/desktop/06-ui-design/keyboard-and-accessibility.md` and is owned by [[DUI-014]]. Implementing
   more than the subset here is the duplication both tickets must avoid.
10. **The six shell states** from the spec, as view-model states with placeholder content where area
    04 owns the real screens: authenticated; unauthenticated (login replaces the shell);
    update-required and blocked (full-window, **no rail** — genuinely removed, not disabled); disabled
    account; stale role. Disconnected shows "Disconnected — reconnecting" in the status bar with saves
    disabled and existing content still visible (proposal § 11.3). Implement **no** authentication
    here.
11. **View-model tests** in `tests/Pegasus.Desktop.ViewModelTests` ([[FND-038]], plan handle
    `DSK-02-13`): rail visibility for administrator vs non-administrator (absent, not disabled);
    environment badge hidden in the production channel and shown otherwise; status-bar connection text
    for connected and disconnected; and navigation service routing to each of the seven routes.
12. **Verify visually and by automation.** Run
    `pwsh .codex/skills/winui-dev-workflow/BuildAndRun.ps1 src/Pegasus.Desktop/Pegasus.Desktop.csproj`
    **asynchronously**, navigate every rail item, and capture screenshots. If [[TEST-006]]'s
    `winapp ui` harness exists, run its shell smoke batch instead of manual navigation; if it does
    not, record in the proof that the evidence is a **manual pass** and name [[TEST-006]] as the
    automation follow-up. Tier 7 requires a manual keyboard review either way — automated checks do
    not replace it (`docs/engineering.md:74`).
13. **Review and close.** Run the `winui-code-review` checklist over the new XAML — theming, no raw
    `FontSize`, no hex literals, AutomationIds present — then the simplification pass, recorded under
    a dated heading below, then open the PR into `dev`.

## Verification

Evidence tier **7 — Browser/accessibility** (`docs/engineering.md` § Required evidence tiers, `:72`),
read as its desktop equivalent: keyboard, focus and error behaviour, semantic labels and
text-plus-colour states must be **demonstrated**, and `:74` states that automated results "do not
replace manual keyboard or assistive-technology review".

The `proof` document is produced from these:

1. `dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release`
   — expected: rail visibility, badge, status and navigation tests pass. Name them individually.
2. The `winapp ui` shell smoke batch, **or** a recorded manual pass — expected: every rail item
   navigates and its `Shell.Rail.<Route>` AutomationId is discoverable. If manual, say so plainly and
   name [[TEST-006]] as the follow-up; do not present a manual pass as automation.
3. `pwsh .codex/skills/winui-dev-workflow/BuildAndRun.ps1 src/Pegasus.Desktop/Pegasus.Desktop.csproj -SkipRun`
   — expected exit 0, zero warnings.
4. **A manual keyboard pass**, recorded as such: `Alt+D/I/U/Q/C/O/A` each reach their route;
   `Ctrl+K` reaches Cases search; `F5` refreshes; `Tab` reaches every rail item and the user menu;
   focus is visible at every stop.
5. Additionally, and not in the body — two greps that make acceptance criteria executable:
   `grep -rnE '#[0-9A-Fa-f]{3,8}\b' src/Pegasus.Desktop/Shell/ src/Pegasus.Desktop/Services/`
   (expected: no matches — no hex literal in any view) and
   `grep -c 'AutomationProperties.AutomationId' src/Pegasus.Desktop/Shell/*.xaml`
   (expected: at least the eleven spec ids). The 100 % coverage claim deserves a check, not an eyeball.
6. Screenshots of the shell in the authenticated state, and of one full-window state
   (update-required) showing the rail genuinely removed rather than disabled.

## Risks / open questions

- **Open question, recorded in this ticket's `open-questions` document** — which channel drives which
  environment-badge label. `screen-specs.md` § Shell gives three non-production labels ("Pilot",
  "Test/UAT", "Development"); plan 02 § 3 decision 7 gives three channels (`pilot`, `production`,
  `local`). Two labels compete for one channel and one label has no channel. This ticket's
  § Documentation changes instructs that a spec ambiguity is recorded as an open question rather than
  an edit, and the badge is the operator-facing control this ticket's § Why calls "what stops an
  operator doing pilot work believing they are in production". The unticked box blocks
  `leave-preparing`, `enter-review` and `enter-done` — **not** `leave-backlog` — and that is the
  correct behaviour, because step 5 cannot write the badge text without the answer. The default this
  plan would otherwise take is stated in that document.
- **Settled here, not an open question — the shell file path.** This ticket's body says
  `src/Pegasus.Desktop/Shell/ShellPage.xaml`; [[DUI-004]] step 3 says
  `src/Pegasus.Desktop/Views/ShellPage.xaml`. **This ticket creates the file, so `Shell/` is the
  path.** [[DUI-004]]'s own § Source of truth already casts it as dressing "the shell scaffold …
  this ticket dresses", so the ownership split needs no negotiation — only the path does, and it is
  fixed here. Building a second shell page under `Views/` is a stop condition for both tickets.
- **Settled here, not an open question — the keyboard boundary.** This ticket wires the
  `screen-specs.md` § Shell subset (`Alt+…`, `Ctrl+K`, `F5`). [[DUI-014]] owns the full map in
  `docs/desktop/06-ui-design/keyboard-and-accessibility.md`, including `F6`, `Ctrl+1`…`Ctrl+8` and
  the five-region focus order. A subset, not a conflict.
- **Risk — the seventh route looks wrong.** `docs/design/README.md:474-475` omits Operations and a
  reader who lands there first will "fix" the rail to six items. *Mitigation*: the reconciliation is
  recorded in this ticket's research and in step 3, citing `:30-38` as canonical and `:1089-1091` as
  the reconciliation.
- **Risk — restyling the selection indicator breaks the accessible selection state.** Assumption
  A-FND033-1. *Mitigation*: step 4's fallback, and [[DUI-004]] step 5's `winapp ui get-property`
  check.
- **Risk — recreating the web shell.** Plan 02 § 7 and this ticket's Guardrails both name it. The
  shell is a `NavigationView`, not a port of `src/Pegasus.Web/Pages/Shared/_Layout.cshtml`; read that
  file to know what is replaced, never to port markup.
- **Risk — explanatory copy.** `docs/design/README.md:169-170`: one title, no lede, guidance only
  beside a consequential control and then one sentence, and a banned vocabulary that includes the word
  "intake". A shell that "explains" is a defect, and step 13's checklist is where it is caught.
- **Risk — `x:Bind` defaults to `OneTime`.** [[DUI-004]]'s recorded trap. Every live shell value
  (connection, counts, badge, role-dependent visibility) needs `Mode=OneWay` or it will render once
  and never update — a failure that looks exactly like a broken query.
- **Sequencing, not an open question — [[FND-034]], [[FND-038]] and [[TEST-006]].** Theme keys, the
  view-model test project and the UI harness each have a named owner. Reference the keys, record a
  manual pass, and do not stub any of them here.

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over this branch's own
diff before the PR, recorded here under a dated heading._
