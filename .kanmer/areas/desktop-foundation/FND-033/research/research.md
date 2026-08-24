# Research — FND-033: the desktop shell, its rail, and the navigation/dialog services

## Question

What exactly does the shell specification fix, what does the web application's existing rail tell us
about the route set and its conditional visibility, and who owns building the shell given that a
second ticket in area 06 names the same deliverable?

## Current behaviour

**No single parity-matrix row covers the shell, and the matrix says so itself.** The matrix at
`docs/desktop/01-inventory-and-parity/parity-matrix.md` holds `PAR-01`…`PAR-46` — counted with
`grep -c '^| PAR-'`, which returns **46** — and every row is keyed to a page model under
`src/Pegasus.Web/Pages/**`. The shell is not a page model; it is the frame those page models render
*inside*. What the shell must reach is the union of the routes those rows live at, which is why the
route set below is derived from the existing layout rather than from a matrix row.

The closest existing repository mechanism — what does this job today:

- **`src/Pegasus.Web/Pages/Shared/_Layout.cshtml` (135 lines)** is the shell today, and
  `_LayoutAuth.cshtml` and `_LayoutExternal.cshtml` are its two siblings. Its `<nav class="app-rail__nav" aria-label="Primary">`
  at `:56` holds exactly the seven routes the desktop rail must carry, in this order:
  `/Index` (`:57`), `/Mail/Index` (`:62`), `/Upload` (`:69`), `/Triage/Index` (`:73`),
  `/Cases/Index` (`:80`), `/Operations/Index` (`:89`), `/Administration/Index` (`:95`).
- **Conditional visibility already exists there**, and it is conditional *rendering*, not disabling.
  `_Layout.cshtml:6` carries the comment *"disabled nav span: a permanently inert item says the
  product is broken"* — a recorded lesson, not a style note, and the reason `Administration` and
  `Inbox` must be **absent** rather than greyed.
- **`aria-current` marks the current item** (`CurrentWhen("/Cases", "/Search")` at `:80`), which is
  the accessibility affordance the desktop's `AutomationProperties` and selection indicator replace.
- **The user menu already has its three items** at `:107-114`: Change password, Sign out, Sign in.

## Findings

### Facts

Verified by reading the repository at fork `main`, 2026-08-24. Each carries its source.

- **The shell specification is `docs/desktop/06-ui-design/screen-specs.md:41-81`**, and it is a
  specification, not a sketch. It fixes: `PaneDisplayMode=Left`, `OpenPaneLength=236`,
  `IsPaneToggleButtonVisible=False` (`:59-60`, with the reason — "the authority's rail never hides");
  the route order in the ASCII diagram at `:43-56`; the current-item treatment as "weight change plus
  the 2px Collision-red left marker (the NavigationView selection indicator restyled), **never colour
  alone**" (`:62-63`); rail counts "absent when the query has not returned; never a shell-level `0`"
  (`:64-66`); the title-bar contents (`:67-70`); the status-bar contents (`:71-72`); the connectivity
  string "Disconnected — reconnecting" with saves disabled and existing content visible (`:73-74`);
  the five shell states (`:75-77`); the keyboard contract (`:78-79`); and the five AutomationIds
  (`:80-81`).
- **The AutomationId convention is repository-wide, not shell-local.**
  `screen-specs.md:31-39`: `<Screen>.<Region>.<Element>[.<Key>]`, PascalCase segments, "stable across
  releases, unique per window", row-level elements appending the record key
  (`Cases.List.Row.576059`), and "Every interactive control has one; `pegasus-ui-verifier`'s coverage
  audit must report 100%". The shell's five names are instances of that convention, so inventing a
  sixth shape here would break [[DUI-015]] (plan handle `DSK-06-15`)'s coverage audit as well as
  [[TEST-006]] (plan handle `DSK-08-06`)'s harness.
- **The design authority's absent-vs-disabled rule has two halves and they are easy to conflate.**
  `screen-specs.md:27-30`: "Deferred capabilities are **absent**, not disabled; an action the record
  will offer once a condition is met stays **visible and disabled with the condition named on the
  control** (\"Available in Review\")." Rail items the operator's role never grants are the first
  case (absent); an action awaiting a case state is the second. `Administration` is the first case.
- **`docs/design/README.md` is the binding design authority** and has the sections this ticket must
  obey: § Design principles (`:160`), § Tokens (`:182`), § Voice, labels and necessary copy (`:396`),
  § No explanatory copy and page economy (`:422`), § Access and permissions (`:447`),
  § Operations-first shell (`:461`), § Complete UI state contract (`:764`), § Accessibility (`:774`),
  § Deferred and absent UI seams (`:810`).
- **`docs/desktop/06-ui-design/tokens-and-theme.md` owns the token values**, with § Files and load
  order (`:11`), § Colour tokens (`:29`), § Typography (`:85`), § Spacing, density and layout
  (`:115`), § Shape, borders, focus, depth (`:132`), § Control styles (shared) (`:174`) and a
  § Change rule (`:197`). This ticket consumes `{ThemeResource}` keys from it; [[FND-034]] (plan
  handle `DSK-02-09`) wires the dictionaries and [[DUI-001]] (plan handle `DSK-06-01`) owns the
  values.
- **A second board ticket names the same deliverable.** [[DUI-004]] (plan handle `DSK-06-04`) is
  titled "Shell: NavigationView rail (236px), route order, counts, title bar, environment badge,
  status bar", sits in area `desktop-ui`, group `EPIC-007` / `HZN-002`, carries the same
  `docs/frd/frd-12-operator-experience.md` ref, and has **no documents yet** (`docs: {}`). This
  ticket's own Guardrails instruct: "agree one owner in the ticket plan before writing XAML, and do
  not build it twice." The reconciliation is recorded in this ticket's plan.
- **`docs/frd/frd-12-operator-experience.md` is this ticket's one real `ref`** — unusually for this
  board, `refs` is **not** empty. Its § Operator experience (`:4-27`) states the requirements this
  shell partly satisfies, including "clear counts that link to their exact filtered work and **do not
  render stale zero placeholders**" (`:13-14`), which is the FRD-level origin of the spec's
  "never a shell-level `0`", and "keyboard, pointer, screen-reader, 200% zoom, forced-colour, and
  reduced-motion support" (`:24-25`). `:28` adds "One semantic action or state has one consistent
  icon across Pegasus; no decorative or generated replacement icon is used."
- **`src/Pegasus.Desktop` does not exist yet.** `ls src` returns exactly `Pegasus.Core`,
  `Pegasus.Infrastructure`, `Pegasus.Web`, `Pegasus.Worker`. The project comes from [[FND-030]] (plan
  handle `DSK-02-05`), the host and DI from [[FND-032]] (plan handle `DSK-02-07`). Both are hard
  prerequisites; the plan's dependency arrow names only [[FND-032]].
- **`Directory.Build.props` (19 lines) applies**: `TreatWarningsAsErrors=true`,
  `AnalysisLevel=latest-recommended`, `Nullable`, `ImplicitUsings`, `LangVersion=latest`.
  XAML-generated code will trip it; narrow commented `NoWarn` entries in the desktop csproj are the
  only permitted remedy.
- **The `winui-design` skill ships a control-lookup binary.** `.codex/skills/winui-design/` holds
  `winui-search.exe` and three reference files; step 2 requires using it rather than guessing
  `NavigationView` property names. `.codex/skills/winui-code-review/SKILL.md` supplies the theming
  checklist step 13 runs.

### Assumptions

- **A-FND033-1 — the `NavigationView` selection indicator can be restyled to a 2 px left marker
  without replacing the whole control template.** *Confirms it*: `winui-search.exe` on the
  `NavigationView` template parts at step 2, then the rendered result at step 12. *If wrong*: a
  larger template override is needed and its size must be recorded — but the requirement itself
  (weight change **plus** marker, never colour alone) does not soften.
- **A-FND033-2 — a custom title bar with an environment badge, connection glyph and user menu can
  keep a usable drag region.** WinUI custom title bars need explicit drag rectangles or
  `SetTitleBar`. *Confirms it*: `microsoft_docs_search` for `AppWindow TitleBar` drag-region
  semantics at step 5, then dragging the window at step 12. *If wrong*: the badge and menu move into
  the content region below the title bar and the deviation is recorded — never a window the operator
  cannot move.
- **A-FND033-3 — `Alt+<letter>` access keys on `NavigationViewItem`s do not collide with the system
  menu or with each other.** Seven keys are specified: `Alt+D/I/U/Q/C/O/A`. *Confirms it*: the
  keyboard pass at step 12, pressing each in turn. *If wrong*: record the collision and raise it with
  [[DUI-014]] (plan handle `DSK-06-14`), which owns the full keyboard map — do not silently pick a
  different letter.
- **A-FND033-4 — the `winapp ui` harness from [[TEST-006]] (plan handle `DSK-08-06`) does not exist
  yet**, so step 12's automation is unavailable. `ls tests` returns only the three existing projects.
  *Confirms it*: check at implementation time. *If it still does not exist*: the evidence is an
  explicitly recorded **manual** keyboard and navigation pass, with [[TEST-006]] named as the
  automation follow-up — the ticket body already permits exactly this, and tier 7 says automated
  checks do not replace a manual keyboard review anyway.
- **A-FND033-5 — the environment badge can read the channel from the options [[FND-032]] registered
  without a second configuration read.** *Confirms it*: the view-model test at step 11 that asserts
  the badge is hidden in the production channel. *If wrong*: the badge is bound to a view-model
  property fed by the same options instance — never to a second literal read of `Channel`, which
  would be the "one list per concept" failure.

## Execution placement

The six-question cloud-justification test from
`docs/desktop/00-governance-and-workflow/README.md` § 3, answered.

| Question | Answer | Evidence |
| --- | --- | --- |
| Shared authority — must several users see and update the same state? | **No** | The shell is per-window chrome. Every value it displays — rail counts, sync time, connection state — is *read* from the gateway; none is authored here. The state itself stays behind the gateway under L-01. |
| Unattended execution — must it run with every desktop closed? | **No** | The shell exists only while an operator has the application open. Unattended work stays in `Pegasus.Worker` under ADR-0106. |
| Protected credentials — a long-lived secret that must not sit on workstations? | **No** | The shell holds no credential. The user menu's Sign out calls the session client from area 04 ([[FND-043]], plan handle `DSK-04-07`); the refresh handle lives in the DPAPI store from [[FND-031]] (plan handle `DSK-02-06`) and the access token stays in memory. Plan 04 § 3 item 8 (`:198-199`): "Secrets in the package: none." |
| Public callback — must an external service call a stable public endpoint? | **No** | The shell listens for nothing. All traffic is outbound to the gateway. |
| Central enforcement — revocation, permissions, audit or an invariant independent of the client? | **Yes — and it lands on the already-existing evolved `Pegasus.Web` gateway, not on any new Azure resource.** | Hiding `Administration` in the rail is a **convenience, not a control**. `screen-specs.md:60-62` is explicit that the role is "derived from the role matrix and **server authorisation**", and `src/Pegasus.Core/Identity/StaffAuthorization.cs` holds the fail-closed `StaffAccessRight` matrix that the gateway enforces. A client that showed the item anyway must still be refused. [[FND-046]] (plan handle `DSK-04-10`) supplies the real role signal; this ticket binds visibility to a view-model property and asserts nothing about authority. |
| Measured operational advantage — measured evidence that central is materially better? | **No** | None claimed. Rendering chrome locally is the whole point of the conversion; no benchmark is offered or needed. |

**Conclusion.** Five "no" and one "yes"; the "yes" names the existing gateway and the Core
authorisation matrix that already enforces it, and explicitly declines to treat the rail as a
security boundary. Nothing here places any responsibility in Azure, and no Azure write arises.

## Implications

1. **The ownership overlap with [[DUI-004]] must be settled in writing before any XAML is typed, and
   this plan is the place the ticket body names.** Two agents building the same `ShellPage.xaml` is
   not a merge conflict — it is two shells. The reconciliation and its reasoning go in the plan's
   Governing docs and Risks sections.
2. **`_Layout.cshtml` is a *route inventory*, not a template.** It tells you the seven routes and
   that two of them are conditional; plan 02 § 7 ("Do not recreate the web shell") and this ticket's
   Guardrails forbid porting its structure. Read it for the route set, then close it.
3. **`_Layout.cshtml:6`'s comment is the strongest argument in the repository for absent-over-disabled**
   — "a permanently inert item says the product is broken" — and it was written by someone who had
   already made the mistake. It aligns exactly with `screen-specs.md:27-28`.
4. **Three of this ticket's requirements are negative, and negatives need explicit tests.** Never a
   shell-level `0`; never colour alone; never a hex literal in a view. A test that renders the shell
   and passes says nothing about any of them. Step 11's view-model tests and step 13's
   `winui-code-review` pass are what make them checkable.
5. **The five AutomationIds are a contract with two other tickets.** [[TEST-006]]'s harness and
   [[DUI-015]]'s 100%-coverage audit both read them. Getting a name wrong here is a silent break
   discovered much later.
6. **The `refs` array is non-empty, which is rare on this board.** `docs/frd/frd-12-operator-experience.md`
   genuinely binds, so the plan's Governing docs section can state **Meets** against real
   requirements rather than only naming a future ADR.

## Open questions

- None that must be answered before implementation. The ownership overlap with [[DUI-004]] is a
  scope boundary with a named sibling ticket, which the ticket body directs to be settled **in this
  plan**; it is recorded there, not opened as a blocking question.
- Two spec-reading points are recorded rather than opened: whether the seven `Alt+` access keys
  collide (A-FND033-3, settled by pressing them) and whether the selection indicator can be restyled
  without a full template override (A-FND033-1, settled by `winui-search.exe`). Both are answered
  inside the ticket by the implementing agent.
- `docs/desktop/06-ui-design/screen-specs.md` is the source and must not be edited by this ticket.
  If a genuine ambiguity is found, the ticket body directs recording it in the ticket rather than
  amending the spec — that instruction is followed.
