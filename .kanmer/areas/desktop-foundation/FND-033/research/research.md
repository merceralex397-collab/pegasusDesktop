# Research — FND-033: the desktop shell, its rail, and the navigation and dialog services

## Question

What exactly does the shell owe — rail, title bar, status bar, states, keyboard, AutomationIds — where
does the design authority actually say it, and which parts of it are owned by a sibling ticket rather
than by this one?

## Current behaviour

**No parity-matrix row covers this ticket, and none should — but the pages it replaces are covered by
rows this shell must not break.** The matrix at
`docs/desktop/01-inventory-and-parity/parity-matrix.md` holds `PAR-01`…`PAR-46` — counted with
`grep -c '^| PAR-'`, which returns **46** — and every row is "keyed by the Razor page model and
handler group that implements it today" (`parity-matrix.md:3-5`). A shell is a frame, not a page
model, so it has no row of its own; the screen spec records it as replacing two shared layouts rather
than any page.

What does this job today:

- `src/Pegasus.Web/Pages/Shared/_Layout.cshtml` (6,948 bytes) — the authenticated shell, named as the
  runtime owner of the "Development shell/navigation" component at `docs/design/README.md:586`.
- `src/Pegasus.Web/Pages/Shared/_LayoutAuth.cshtml` (1,061 bytes) — the navless shell for sign in,
  signed-out confirmation, access denied and the error/not-found family (`docs/design/README.md:587`).
- `src/Pegasus.Web/Presentation/RailCountsPageFilter.cs` — how rail counts are obtained today (named
  by [[DUI-004]] (plan handle `DSK-06-04`) as the mechanism the desktop replaces with a gateway
  query).

Both layouts stay live until cutover; nothing in this ticket touches them.

## Findings

### Facts

Verified by reading the repository at fork `main`, 2026-08-24. Each carries its source.

- **The seven-route rail is authority-backed, and the authority states it twice with different
  wording.** This looks like a contradiction and is not:
  - `docs/design/README.md:30-38` — "The authenticated routes are: 1. Dashboard 2. Inbox 3. Upload
    4. Queues 5. Cases **6. Operations** 7. Administration, visible only to authorised Administrators
    8. authenticated user/sign-out controls", "settled by the operator on 2026-08-04 and shipped in
    releases 6 and 7". Seven routes plus user controls.
  - `docs/design/README.md:474-475` — the abbreviated restatement, "`Dashboard → Inbox → Upload →
    Queues → Cases → Administration (admin-only) + user controls`", which **omits Operations**.
  - `docs/design/README.md:1089-1091` reconciles them: "The routes shipped in releases 6 and 7 are
    Dashboard, Inbox, Upload, Queues, Cases and authorised Administration… **Operations is a scoped
    staff workspace in the implementation; its documentation does not prove a deployed or released
    route.**"
  - It does exist in the implementation: `src/Pegasus.Web/Pages/Operations/Index.cshtml` and
    `Index.cshtml.cs`, and `docs/design/README.md:49-54` describes its three sections.
  - **Conclusion**: the canonical list at `:30-38` includes Operations, `docs/desktop/06-ui-design/screen-specs.md`
    § Shell's seven-item order matches it, [[DUI-004]] step 4 lists `Shell.Rail.Operations`, and this
    ticket's body lists the same seven. The `:474` restatement is the abbreviated one. **No question
    is opened on the route count**, and the reconciliation is recorded here so the next reader who
    lands on `:474` does not open one.
- **The rail settings are exact**: `docs/desktop/06-ui-design/screen-specs.md` § Shell —
  `PaneDisplayMode="Left"`, `OpenPaneLength="236"`, `IsPaneToggleButtonVisible="False"` ("the
  authority's rail never hides"); `Administration` present only for the Administrator role; `Inbox`
  present only when the capability is composed.
- **Absence, not disablement, is the rule** — `docs/design/README.md:586`: the current route carries
  "a weight change **and a 2px Collision-red left border** so it is not signalled by colour alone; the
  Inbox item is conditional and is **absent**, never a disabled span, where the capability is not
  composed". `:172` states the general form: "A capability that is not composed in a deployment is
  absent from the interface — never a disabled item, inert card, or 'Unavailable' placeholder", with
  the carve-out at `:173` for *conditions* ("Available in Review"), which is a different thing.
- **The zero rule is explicit and repeated.** `docs/design/README.md:172` — "A composed query that
  returns zero renders `0`"; `:489-491` — "`0` is a current result, never a substitute for stale,
  partial, unavailable, failed, or not-yet-loaded data, and no shipped tile may render a placeholder
  for a query that does not exist". The shell consequence in the screen spec: rail counts "come from
  the dashboard rail-counts query; absent when the query has not returned; never a shell-level `0`".
- **The five shell AutomationIds are named**: `Shell.Rail.<Route>`, `Shell.Title.Environment`,
  `Shell.Title.User`, `Shell.Status.Connection`, `Shell.Status.Update`
  (`screen-specs.md` § Shell), under the convention `<Screen>.<Region>.<Element>[.<Key>]`, PascalCase,
  stable across releases, unique per window (`screen-specs.md` § AutomationId convention), where
  "`pegasus-ui-verifier`'s coverage audit must report 100%".
- **The keyboard contract is owned twice, and the second owner is broader.**
  `screen-specs.md` § Shell lists the rail access keys `Alt+D/I/U/Q/C/O/A`, `Ctrl+K` → Cases search
  and `F5` refresh. `docs/desktop/06-ui-design/keyboard-and-accessibility.md` § Keyboard map holds the
  **full** map — those plus `Ctrl+N`, `Ctrl+S`, `Ctrl+W`, `Esc`, `Ctrl+1`…`Ctrl+8`, `Tab`/`Shift+Tab`
  with the focus order "title bar → rail → page header → content → status bar", and `F6` to cycle
  shell regions — and [[DUI-014]] (plan handle `DSK-06-14`, horizon Phase 1) is the ticket titled
  "Keyboard map and access keys implemented". The shell subset is this ticket's; the map is
  [[DUI-014]]'s.
- **The shell is owned by two tickets and they are complementary, not duplicative — settled by the
  sibling's own body.** [[DUI-004]]'s § Source of truth states its dependencies include
  "`DSK-02-08` — the shell scaffold, navigation and dialog services **this ticket dresses**". So this
  ticket builds the scaffold and the services; [[DUI-004]] dresses it to the design spec (token-bound
  rail width, selection-indicator restyle, landmarks, count binding to
  `GET /api/v1/dashboard/rail-counts`, the checksummed logo from [[DUI-003]] (plan handle
  `DSK-06-03`), the 1280 content cap, the backdrop decision, and the `winapp ui` scripts).
- **The two tickets name different paths for the same file.** This ticket's body says
  `src/Pegasus.Desktop/Shell/ShellPage.xaml`; [[DUI-004]] step 3 says
  `src/Pegasus.Desktop/Views/ShellPage.xaml`. One file, two paths — a concrete thing the "agree one
  owner" instruction must settle, and it is settled in this ticket's plan.
- **[[DUI-004]] adds two settings this ticket's body does not name**: `IsSettingsVisible="False"`
  (Diagnostics/Settings is reached from the user menu, not the rail) and
  `AutomationProperties.LandmarkType="Navigation"` on the pane. Neither contradicts anything here.
- **The state contract is a repository authority, not a plan invention.**
  `docs/design/README.md:764-772` § Complete UI state contract fixes the required states per scope —
  Queries: "Loading; empty; current success; stale with last-good time; partial; unavailable;
  failed/retry; unauthenticated; disabled; stale-role; denied". The screen spec's shell states
  (authenticated; unauthenticated; update-required and blocked; disabled account; stale role) are that
  contract applied to the frame.
- **Operator copy rules bind the shell.** `docs/design/README.md:169` — "Controls communicate purpose
  without narrating obvious actions. Screens carry no lede or subtitle: one H1 and the content.
  Guidance appears only beside a control whose action has a consequence the operator must understand,
  and is one sentence." `:170` bans exposing Azure, OCR, AI, queue, extraction, deployment, adapter,
  lease/version, projection, ingress or artifact terminology in operator copy, and bans the word
  "intake" in operator-facing text.
- **Europe/London is mandatory and `ToLocalTime()` is named as wrong.** `docs/design/README.md:172`
  (the paragraph beginning "Every date and time an operator reads renders Europe/London through that
  same map. `ToLocalTime()` is never correct: it resolves against the server clock… so it looks right
  exactly where it is tested and is wrong through British Summer Time where it runs."). The status
  bar's last-sync time is exactly such a value.
- **`winui-design` supplies a control lookup binary**: `.codex/skills/winui-design/winui-search.exe`,
  beside `SKILL.md` and `references/{brushes-and-icons,layout-review,theme-accessibility}.md`. Its
  theming rule at `SKILL.md:143` — "Custom theme dictionaries cover `Light`, `Dark`, **and**
  `HighContrast` explicitly — never `Default`" — belongs to [[FND-034]] (plan handle `DSK-02-09`), not
  here; what binds here is its anti-pattern table's first row, "Reflexively build every app as
  `NavigationView` Left", which in this case is not reflexive: the authority's rail *is* a left rail.
- **`tests/Pegasus.Desktop.ViewModelTests` does not exist** (`ls tests` → three projects only);
  [[FND-038]] (plan handle `DSK-02-13`) creates it. Nor does the `winapp ui` harness — [[TEST-006]]
  (plan handle `DSK-08-06`) creates `tests/Pegasus.Desktop.UITests`.
- **The host this shell resolves through does not exist yet either.**
  `src/Pegasus.Desktop/Hosting/PegasusHost.cs` and `App.xaml.cs`'s host build are [[FND-032]] (plan
  handle `DSK-02-07`), which is the named dependency.

### Assumptions

- **A-FND033-1 — the `NavigationView` selection indicator can be restyled to a 2 px left marker while
  keeping the accessible selection state correct.** *Confirms it*: [[DUI-004]] step 5's
  `winapp ui get-property` check on the selected item; before that, a `winui-search.exe` lookup of the
  selection-indicator template parts. *If wrong*: the marker becomes a separate visual element inside
  the item template and the built-in indicator is hidden — which must not remove the automation
  state.
- **A-FND033-2 — a custom title bar can host the environment badge, connection state, version and
  user menu while keeping a working drag region.** *Confirms it*: `microsoft_docs_search` for
  `AppWindow TitleBar` drag-region semantics (this ticket's step 5) and the manual pass at step 12.
  *If wrong*: the badge moves into the page header region and the deviation is recorded against the
  screen spec.
- **A-FND033-3 — rail access keys (`Alt+…`) and `Ctrl+K`/`F5` can be declared on the shell without
  colliding with `NavigationView`'s built-in accelerators.**
  `keyboard-and-accessibility.md` § Keyboard map asserts "Conflicts: none with Windows system
  shortcuts (`Win+*`), with WinUI `NavigationView` defaults, or with the reason dialog". *Confirms
  it*: the keyboard pass at step 12 exercising every key. *If wrong*: [[DUI-014]] owns the resolution,
  since it owns the map.
- **A-FND033-4 — the shell's role-dependent items can be driven from view-model state without the
  session flow existing.** Area 04 owns authentication; this ticket needs only a boolean-ish role
  input. *Confirms it*: the rail-visibility view-model tests at step 11 passing against a fake.
  *If wrong*: the states become placeholders with a recorded follow-up rather than fabricated
  authentication.

## Execution placement

The six-question cloud-justification test from
`docs/desktop/00-governance-and-workflow/README.md` § 3, answered.

| Question | Answer | Evidence |
| --- | --- | --- |
| Shared authority — must several users see and update the same state? | **Yes for what the rail *displays*, no for the shell itself — and the display already lands on the existing gateway.** | The rail counts are shared office-wide state: `docs/design/README.md:512` records "strongest shared-office awareness and truthful day/week visibility" as the selection rationale. But the shell does not own that state — [[DUI-004]]'s binding decision is explicit: "the rail counts come from the evolved `Pegasus.Web` gateway (`GET /api/v1/dashboard/rail-counts`, `DSK-03-06`), never from a direct database read" (L-01, ADR-0103). The shell renders; the gateway is authority. No new resource. |
| Unattended execution — must it run with every desktop closed? | **No** | A shell exists only while an operator is looking at it. |
| Protected credentials — a long-lived secret that must not sit on workstations? | **No** | The shell holds no secret. The user menu's Sign out and Change password route to area 04 flows over [[FND-031]]'s (plan handle `DSK-02-06`) DPAPI store, which holds only a short-lived refresh handle. |
| Public callback — must an external service call a stable public endpoint? | **No** | The shell makes no outbound call of its own; it renders state fetched through the gateway client. |
| Central enforcement — revocation, permissions, audit or an invariant independent of the client? | **Yes — and it lands on the already-existing evolved `Pegasus.Web` gateway, not on any new Azure resource.** | Hiding `Administration` in the rail is a **convenience, not a control**. [[DUI-004]] states the rule directly: `Administration` is "present only for the Administrator role (derived from the role matrix and **server authorisation**)". `src/Pegasus.Core/Identity/StaffAuthorization.cs` holds the fail-closed `StaffAccessRight` matrix and the gateway enforces it per request; plan 04 § 3 item 3 re-checks `IsEnabled` and the security stamp on **every** `/api/v1` request. A shell that hid nothing would still be safe; a shell that hid everything would still not be authorisation. |
| Measured operational advantage — measured evidence that central is materially better? | **No** | None claimed. The placement follows from L-01 and ADR-0103. |

**Conclusion.** Four "no" and two "yes"; both "yes" answers name the **existing** gateway process —
one for the shared counts, one for authorisation — and neither places anything new, in Azure or
anywhere else. No Azure write arises.

## Implications

1. **Two tickets, one file, and the split is already settled by the sibling.** [[DUI-004]]'s own body
   says it "dresses" this ticket's scaffold. The remaining decision is the **path**, because the two
   bodies name `Shell/ShellPage.xaml` and `Views/ShellPage.xaml`. This ticket creates the file, so its
   path wins and the plan records it; building two shell pages is the failure both bodies forbid.
2. **The rail count rule needs a test, not a convention.** "Absent until the query returns, never a
   shell-level `0`" is invisible in a screenshot of a populated system and only appears when the
   gateway is slow. [[DUI-004]] names the test (`RailCountIsAbsentUntilTheQueryReturns`); this
   ticket's view-model tests should not duplicate it.
3. **The keyboard boundary is a subset relationship, not a conflict.** This ticket wires the shell
   subset the screen spec names; [[DUI-014]] owns the full map including `F6` region cycling and the
   `Ctrl+1`…`Ctrl+8` tab keys. Implementing more than the subset here would be the duplication.
4. **The environment badge's values do not map cleanly onto the channel names, and that is a real
   spec ambiguity.** `screen-specs.md` § Shell says the badge is non-production only and shows
   "Pilot", "Test/UAT" or "Development" — three labels. Plan 02 § 3 decision 7 and plan 04 § 3 item 8
   define exactly three channels: `pilot`, `production`, `local`. Two of the three labels compete for
   one channel and one label has no channel at all. The badge is the control the ticket's own § Why
   names as "what stops an operator doing pilot work believing they are in production", and its text
   is operator copy owned by the design authority — so it is recorded as an open question, as this
   ticket's § Documentation changes instructs, with the default this plan would otherwise take stated
   alongside it.
5. **Europe/London is a correctness rule with a named wrong answer.** `docs/design/README.md:172`
   says `ToLocalTime()` "is never correct" and explains why it passes on a UK workstation and fails in
   the container. The status bar's last-sync time must go through the shared label map.
6. **Operator copy is a review gate for a shell.** `docs/design/README.md:169` — one H1, no lede, no
   how-it-works copy; `:170` bans the vocabulary. "A shell that explains is a defect" is not rhetoric,
   it is the authority.

## Open questions

- **One, and it is recorded in this ticket's `open-questions` document**: which channel value drives
  which environment-badge label, given three labels ("Pilot", "Test/UAT", "Development") and three
  channels (`pilot`, `production`, `local`). It must be answered before step 5 writes the badge,
  because the badge is operator-facing copy owned by `docs/design/README.md` and is the safety control
  this shell exists to provide. The default this plan would take is stated there.
- Everything else that looked like a question resolved on inspection: the seven-route order is
  authority-backed at `docs/design/README.md:30-38` (the `:474` restatement is the abbreviated one),
  and the [[DUI-004]] / [[DUI-014]] overlaps are scope boundaries with named owners, recorded in the
  plan's Risks section.
