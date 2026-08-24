# Files — FND-033

Surveyed 2026-08-24 against fork `main`. Existing paths were confirmed with `ls`/`sed`; new files are
marked; files created by a named earlier ticket say so.

## Where the change lands

| Path | Why |
| --- | --- |
| `src/Pegasus.Desktop/Shell/ShellPage.xaml` and `.xaml.cs` | **New.** The `NavigationView` frame: `PaneDisplayMode="Left"`, `OpenPaneLength="236"`, `IsPaneToggleButtonVisible="False"`, seven rail items in the authority order, the restyled selection indicator, and the content host. **Path note**: [[DUI-004]] (plan handle `DSK-06-04`) step 3 names `src/Pegasus.Desktop/Views/ShellPage.xaml` for the same file. This ticket creates it, so `Shell/` is the path and [[DUI-004]] dresses it there — see the plan's Risks section. |
| `src/Pegasus.Desktop/Shell/ShellViewModel.cs` | **New.** Rail visibility per role and per composed capability, environment badge state, connection state, status-bar values, and the shell state machine (authenticated / unauthenticated / update-required / blocked / disabled account / stale role). Bound with `Mode=OneWay` — `x:Bind` defaults to `OneTime`, which is [[DUI-004]]'s recorded trap. |
| `src/Pegasus.Desktop/Shell/TitleBar*.xaml` | **New.** Logo slot, environment badge (`Shell.Title.Environment`), connection glyph plus word, version and channel, and the user menu (`Shell.Title.User`) with Change password, Sign out, Diagnostics. The checksummed logo asset itself is [[DUI-003]] (plan handle `DSK-06-03`); this ticket places the slot. |
| `src/Pegasus.Desktop/Shell/StatusBar*.xaml` | **New.** `Shell.Status.Connection`, last sync in Europe/London, background-transfer summary, `Shell.Status.Update`. |
| `src/Pegasus.Desktop/Services/INavigationService.cs` and its implementation | **New.** The **only** navigation mechanism; every rail item routes through it. |
| `src/Pegasus.Desktop/Services/IDialogService.cs` and its implementation | **New.** The only prompt mechanism. |
| `src/Pegasus.Desktop/Hosting/PegasusHost.cs` (created by [[FND-032]], plan handle `DSK-02-07`) | Register the two services. [[FND-032]] deliberately did **not** create empty interfaces for them (`docs/engineering.md` § Abstractions, `:113`); this is the ticket that defines them, so this is where they enter the container. |
| `tests/Pegasus.Desktop.ViewModelTests/…` (created by [[FND-038]], plan handle `DSK-02-13`) | Rail visibility for administrator vs non-administrator; environment badge hidden in the production channel and shown otherwise; status-bar connection text for connected and disconnected; navigation service routing to each of the seven routes. |

## Context files

What the implementer must **read** and what each one tells them.

| Path | What it tells the implementer |
| --- | --- |
| `docs/desktop/06-ui-design/screen-specs.md` § Shell | **The specification, not a summary** — read it in full. The ASCII frame, the three `NavigationView` settings, the absence rules for `Administration` and `Inbox`, the "never a shell-level `0`" count rule, the title-bar and status-bar contents, the six shell states, the keyboard subset and the five AutomationIds. |
| `docs/desktop/06-ui-design/screen-specs.md` § AutomationId convention (`:31-40`) | The naming grammar `<Screen>.<Region>.<Element>[.<Key>]`, PascalCase, stable across releases, unique per window — and that `pegasus-ui-verifier`'s coverage audit "must report 100%". A missing id is a harness failure, not a style nit. |
| `docs/design/README.md:30-46` | The **canonical** authenticated route list — seven routes including **6. Operations** — "settled by the operator on 2026-08-04 and shipped in releases 6 and 7", and what it superseded. This is the list the rail implements. |
| `docs/design/README.md:474-475` | The abbreviated restatement that **omits Operations**. Read it knowing `:30-38` is canonical and `:1089-1091` reconciles them ("Operations is a scoped staff workspace in the implementation; its documentation does not prove a deployed or released route"). Without this note the next reader opens a question that has an answer. |
| `docs/design/README.md:586` | The exercised shell component: the current route carries "a weight change **and a 2px Collision-red left border** so it is not signalled by colour alone; the Inbox item is conditional and is **absent**, never a disabled span, where the capability is not composed". Both halves are acceptance criteria. |
| `docs/design/README.md:172-173` | Two rules that decide shell behaviour: a not-composed capability is **absent**, never disabled or "Unavailable" (`:172`); but an *action* the record will offer once a condition is met stays visible and disabled with the condition named (`:173`). Capability ≠ condition, and confusing them produces exactly the wrong rail. |
| `docs/design/README.md:172` (the Europe/London paragraph) | "`ToLocalTime()` is never correct: it resolves against the server clock… so it looks right exactly where it is tested and is wrong through British Summer Time where it runs." The status bar's last-sync time must go through the shared operator-label map. |
| `docs/design/README.md:489-491` | The zero rule in full: "`0` is a current result, never a substitute for stale, partial, unavailable, failed, or not-yet-loaded data, and no shipped tile may render a placeholder for a query that does not exist." This is why a rail count is **absent**, not `0`, before the query returns. |
| `docs/design/README.md:169-170` | The operator-copy rules: one H1, no lede or subtitle, guidance only beside a control with a consequence and then one sentence; and the banned vocabulary (Azure, OCR, AI, queue mechanics, extraction, deployment, adapter, lease/version, projection, ingress, artifact — and the word "intake" never in operator-facing text). A shell that explains is a defect against this. |
| `docs/design/README.md:764-772` § Complete UI state contract | The repository-level required-state list the shell's six states are an application of — Queries must cover loading, empty, current, stale with last-good time, partial, unavailable, failed/retry, unauthenticated, disabled, stale-role and denied. |
| `docs/desktop/06-ui-design/keyboard-and-accessibility.md` § Keyboard map | The **full** map, of which this ticket implements only the shell subset. It also carries the focus order ("title bar → rail → page header → content → status bar"), the `F6` region cycle, and the claim that there are no conflicts with `NavigationView` defaults — which step 12's keyboard pass tests rather than trusts. [[DUI-014]] (plan handle `DSK-06-14`) owns the map. |
| `docs/desktop/02-architecture-and-foundation/README.md` § 3 decision 9 | "No desktop framework on top of WinUI": a shell service, a navigation service, a dialog service and a handful of project controls are the whole permitted surface. It bounds what this ticket may add. |
| `docs/desktop/02-architecture-and-foundation/README.md` § 7 | "Do not recreate the web shell — the shell is a `NavigationView`, not a port of `_Layout.cshtml`; 06 owns the rules." |
| `src/Pegasus.Web/Pages/Shared/_Layout.cshtml` (6,948 bytes), `_LayoutAuth.cshtml` (1,061 bytes) | The two files this shell replaces, and the two files this ticket must **not** touch — the web front end stays live until cutover. Read them to know what is being replaced, never to port markup. |
| `src/Pegasus.Web/Presentation/RailCountsPageFilter.cs` | How rail counts are obtained today. The desktop replaces it with `GET /api/v1/dashboard/rail-counts` ([[DUI-004]]'s binding decision under L-01) — never a direct database read. |
| `src/Pegasus.Core/Identity/StaffAuthorization.cs` | The fail-closed `StaffAccessRight` matrix. It is why hiding `Administration` in the rail is a convenience rather than a control: the server authorises every request regardless of what the rail shows. |
| `src/Pegasus.Desktop/Hosting/PegasusHost.cs` (created by [[FND-032]]) | Where the two new services are registered, and the reason they did not exist before: [[FND-032]] step 5 deliberately deferred creating empty interfaces. |
| `.codex/skills/winui-design/SKILL.md` and `winui-search.exe` | The control-choice guidance and the API-lookup binary. Its anti-pattern table warns against "reflexively" choosing `NavigationView` Left — here it is not reflexive, it is the authority's rail, and recording that reasoning is what satisfies the skill. |
| `.codex/skills/winui-code-review/references/quality-rules.md` | The checklist step 13 runs: theming, no raw `FontSize`, no hex literals, AutomationIds present. |

## Ripple effects

- **[[DUI-004]] dresses this file.** Its § Source of truth names `DSK-02-08` as "the shell scaffold,
  navigation and dialog services **this ticket dresses**", and its steps then set the rail width from
  `PegasusRailWidth`, restyle the indicator, bind the counts, add landmarks, place the checksummed
  logo, cap the content region at `PegasusContentMaxWidth`, decide the backdrop and write
  `tests/Pegasus.Desktop.UITests/shell-tests.ps1`. Every one of those lands **in the file this ticket
  creates**, so the path must be agreed here.
- **Theme keys.** Every brush this shell uses is a `{ThemeResource}` from [[FND-034]] (plan handle
  `DSK-02-09`); no hex literal may appear. If [[FND-034]] has not landed, the keys are referenced and
  the shell renders unstyled rather than being hard-coded — recorded, not worked around.
- **Host registrations.** `PegasusHost.cs` gains two registrations, which is the first time
  [[FND-032]]'s "register only services with a real caller" rule is exercised.
- **Tests.** `tests/Pegasus.Desktop.ViewModelTests` gains four view-model tests;
  `tests/Pegasus.Desktop.UITests` (created by [[TEST-006]], plan handle `DSK-08-06`) gains the shell
  smoke batch — and if it does not exist, the evidence is an explicitly-recorded manual pass naming
  [[TEST-006]] as the automation follow-up.
- **Downstream.** [[FND-034]], [[FND-041]] (plan handle `DSK-02-16`), [[FND-046]] (plan handle
  `DSK-04-10`, role-aware shell), [[FEAT-001]] (plan handle `DSK-05-01`) and [[DUI-004]] all name this
  shell. [[FND-035]] (plan handle `DSK-02-10`) routes redirected activations through
  `INavigationService`.
- **No solution, architecture-test, package or restore change.** This ticket adds no project and no
  package, so `Pegasus.slnx`, `DependencyDirectionTests.cs` and every `packages.lock.json` are
  untouched.
- **Documentation.** None owed. `docs/desktop/06-ui-design/screen-specs.md` is the **source** and is
  not edited; the `DSK-01` capability row is [[FND-008]]'s (plan handle `DSK-00-08`).

## Out of scope

Recorded so the reviewer sees each was a decision, matching the ticket's Guardrails.

- **Theme token values and the `Styles/` dictionaries** — [[FND-034]] wires them and [[DUI-001]] (plan
  handle `DSK-06-01`) owns the values. This ticket defines no colour.
- **Sign-in, the update-required screen's behaviour and the compatibility gate** — area 04
  ([[FND-044]], [[FND-045]]). The states exist here as view-model states with placeholder content.
- **The rail-counts query itself** — `GET /api/v1/dashboard/rail-counts` is `DSK-03-06`'s; [[DUI-004]]
  binds the counts.
- **The full keyboard map** — [[DUI-014]]. Only the screen spec's shell subset is wired here.
- **`src/Pegasus.Web/Pages/Shared/_Layout.cshtml` and any Razor view** — untouched; the web front end
  stays live until cutover.
- **Any `WebView2` element** — refused in this or any view; the only permitted use is the isolated
  report renderer under ADR-0108, in area 07.
- **A second shell page** — refused. One file, at the path this plan fixes.
- **Explanatory copy** — refused under `docs/design/README.md:169-170`; labels, values, and at most
  one consequence sentence beside a destructive action.
- **A shell-level `0` for an unqueried count** — refused under `docs/design/README.md:489-491`.
