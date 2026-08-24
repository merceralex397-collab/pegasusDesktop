# Files — FND-033

Surveyed 2026-08-24 against fork `main`. Every existing path was confirmed with `ls`/`sed`/`grep`;
paths created by an earlier ticket are marked with that ticket.

## Where the change lands

| Path | Why |
| --- | --- |
| `src/Pegasus.Desktop/Shell/ShellPage.xaml` | **New.** The `NavigationView`: `PaneDisplayMode="Left"`, `OpenPaneLength="236"`, `IsPaneToggleButtonVisible="False"`, seven `NavigationViewItem`s in the approved order (Dashboard, Inbox, Upload, Queues, Cases, Operations, Administration), the restyled selection indicator, the custom title bar and the status bar. Every `{ThemeResource}` key comes from [[FND-034]] (plan handle `DSK-02-09`); **no hex literal and no raw `FontSize`** may appear. |
| `src/Pegasus.Desktop/Shell/ShellPage.xaml.cs` | **New.** Code-behind only for what XAML cannot do: title-bar drag regions (`SetTitleBar` / `SetDragRectangles`) and hooking the `NavigationView` selection to the navigation service. No business logic. |
| `src/Pegasus.Desktop/Shell/ShellViewModel.cs` | **New.** The properties every negative requirement is tested against: rail-item visibility (`IsAdministrationVisible`, `IsInboxVisible`), rail counts as **nullable** so "absent" and "zero" are different values, `IsEnvironmentBadgeVisible` plus the badge text, connection state text, last-sync time, transfer summary, update availability, and the five shell states. Nullable counts are the mechanism that makes "never a shell-level `0`" enforceable rather than aspirational. |
| `src/Pegasus.Desktop/Services/INavigationService.cs` + its implementation | **New.** The only navigation mechanism in the application. Every rail item routes through it; a `Frame.Navigate` call anywhere else is a defect review must catch. |
| `src/Pegasus.Desktop/Services/IDialogService.cs` + its implementation | **New.** The only prompt mechanism, wrapping `ContentDialog`. The `Dialog.Reason.Text` / `Dialog.Reason.Confirm` / `Dialog.Reason.Cancel` AutomationIds from `screen-specs.md:36-37` belong to it. |
| `src/Pegasus.Desktop/Hosting/PegasusHost.cs` (created by [[FND-032]], plan handle `DSK-02-07`) | Register `INavigationService` and `IDialogService`. [[FND-032]] deliberately registered **no** placeholder for them (`docs/engineering.md` § Abstractions `:113`), so this is their first appearance — the registration and the real caller land together. |
| `src/Pegasus.Desktop/App.xaml.cs` (created by [[FND-030]], plan handle `DSK-02-05`, edited by [[FND-032]]) | Point the root window's content at `ShellPage`. Touch only that; the host build and the pre-window region belong to [[FND-032]] and [[FND-035]] (plan handle `DSK-02-10`). |
| `tests/Pegasus.Desktop.ViewModelTests/**` (created by [[FND-038]], plan handle `DSK-02-13`) | Four test classes against `ShellViewModel` and the navigation service — rail visibility, environment badge, status-bar connection text, and routing. They run without a dispatcher, which is why the state lives in a view model rather than in `ShellPage.xaml.cs`. |

## Context files

What the implementer must **read**, and what each one tells them.

| Path | What it tells the implementer |
| --- | --- |
| `docs/desktop/06-ui-design/screen-specs.md:41-81` § Shell | **The specification, not a summary — read it in full before anything else.** It fixes the three `NavigationView` properties with their reason (`:59-60`, "the authority's rail never hides"); the route order in the ASCII diagram (`:43-56`); the current item as "weight change plus the 2px Collision-red left marker … **never colour alone**" (`:62-63`); counts "absent when the query has not returned; never a shell-level `0`" (`:64-66`); title bar and status bar contents (`:67-72`); the exact connectivity string "Disconnected — reconnecting" with saves disabled and existing content visible (`:73-74`); the five states (`:75-77`); the keyboard contract (`:78-79`); and the five AutomationIds (`:80-81`). |
| `docs/desktop/06-ui-design/screen-specs.md:31-39` § AutomationId convention | That the five shell names are **instances of a repository-wide convention** — `<Screen>.<Region>.<Element>[.<Key>]`, PascalCase, "stable across releases, unique per window", 100% coverage audited by `pegasus-ui-verifier`. Inventing a different shape here breaks [[TEST-006]] (plan handle `DSK-08-06`)'s harness and [[DUI-015]] (plan handle `DSK-06-15`)'s audit, not just this screen. |
| `docs/desktop/06-ui-design/screen-specs.md:27-30` | The absent-vs-disabled rule, which has **two halves** that are easy to conflate: "Deferred capabilities are **absent**, not disabled; an action the record will offer once a condition is met stays **visible and disabled with the condition named on the control** (\"Available in Review\")." `Administration` for a non-administrator is the first case (absent). Do not apply the second. |
| `src/Pegasus.Web/Pages/Shared/_Layout.cshtml` (135 lines) | **Read for the route inventory, then close it.** `:56-99` is the existing rail and names the seven routes and their order: `/Index`, `/Mail/Index`, `/Upload`, `/Triage/Index`, `/Cases/Index`, `/Operations/Index`, `/Administration/Index`, with the last two already conditionally rendered. `:107-114` is the user menu's three items. Plan 02 § 7 and this ticket's Guardrails forbid porting its structure — this is a `NavigationView`, not a port of `_Layout.cshtml`. |
| `src/Pegasus.Web/Pages/Shared/_Layout.cshtml:6` | The single strongest argument in this repository for absent-over-disabled, written by someone who had already made the mistake: *"disabled nav span: a permanently inert item says the product is broken"*. It agrees exactly with `screen-specs.md:27-28`. |
| `docs/design/README.md` | The binding design authority. The sections that bind this ticket: § Design principles (`:160`), § Tokens (`:182`), § Voice, labels and necessary copy (`:396`), **§ No explanatory copy and page economy (`:422`)** — labels, values, and at most one consequence sentence on a destructive action; a shell that "explains" is a defect — § Access and permissions (`:447`), § Operations-first shell (`:461`), § Complete UI state contract (`:764`), § Accessibility (`:774`), § Deferred and absent UI seams (`:810`). |
| `docs/desktop/06-ui-design/tokens-and-theme.md` | Where the `{ThemeResource}` keys this XAML consumes are **defined** — § Files and load order (`:11`), § Colour tokens (`:29`), § Typography (`:85`), § Spacing, density and layout (`:115`), § Shape, borders, focus, depth (`:132`), § Control styles (`:174`), § Change rule (`:197`). This ticket **consumes** keys; [[FND-034]] wires the dictionaries into `App.xaml` and [[DUI-001]] (plan handle `DSK-06-01`) owns the values. Do not define a token here. |
| `docs/frd/frd-12-operator-experience.md` § Operator experience (`:4-27`) | This ticket's one real `ref`. `:13-14` — "clear counts that link to their exact filtered work and **do not render stale zero placeholders**" — is the FRD-level origin of the spec's "never a shell-level `0`", so the nullable-count design satisfies an FRD requirement, not a style preference. `:24-25` requires "keyboard, pointer, screen-reader, 200% zoom, forced-colour, and reduced-motion support". `:28` requires one consistent icon per semantic action across Pegasus. |
| `src/Pegasus.Core/Identity/StaffAuthorization.cs` | The fail-closed `StaffAccessRight` matrix the **gateway** enforces. Read it to understand that hiding `Administration` in the rail is a convenience and not a control — `screen-specs.md:60-62` says the role is "derived from the role matrix and **server authorisation**". [[FND-046]] (plan handle `DSK-04-10`) supplies the real role signal; this ticket binds to a view-model property. |
| `src/Pegasus.Desktop/Hosting/PegasusHost.cs` (created by [[FND-032]]) | Where the two services register, and the options instance the environment badge reads its channel from. [[FND-032]] registered no navigation or dialog placeholder on purpose, so there is no dead interface waiting — the registration and the caller land together here. |
| `.codex/skills/winui-design/` (with `winui-search.exe`) | The control-lookup binary step 2 requires. Use it to confirm the `NavigationView` API surface and its selection-indicator template parts **before** writing XAML; do not guess property names. |
| `.codex/skills/winui-code-review/SKILL.md` | The theming checklist step 13 runs: no hex literals, no raw `FontSize`, AutomationIds present, theme resources used. This is the executable form of three of this ticket's negative requirements. |
| `.codex/skills/winui-dev-workflow/BuildAndRun.ps1:142-172` | That the script **injects** a project-level `Directory.Build.props` (the existence test at `:152` checks the project directory only, not up the tree) which **shadows** the root one, dropping `TreatWarningsAsErrors`. Use the script to launch and look at the shell; use `dotnet build ./Pegasus.slnx --configuration Release` to gate. |
| `Directory.Build.props` (19 lines) | `TreatWarningsAsErrors=true` and `AnalysisLevel=latest-recommended` apply to XAML-generated code too. Narrow, individually-commented `NoWarn` entries in the desktop csproj are the only permitted remedy. |

## Ripple effects

- **The five AutomationIds become a contract with two other tickets the moment they are written.**
  [[TEST-006]] (plan handle `DSK-08-06`) builds its `winapp ui` harness around them and [[DUI-015]]
  audits 100% coverage. A renamed id later is a silent break discovered in another area's lane.
- **[[FND-034]] is unblocked and immediately constrained.** It wires the Light/Dark/HighContrast
  dictionaries into `App.xaml` and bans hard-coded colours — every `{ThemeResource}` key this XAML
  references must exist by the time that ticket's ban is enforced, or the shell fails to load rather
  than merely looking wrong.
- **[[FND-046]] (plan handle `DSK-04-10`) replaces this ticket's placeholder role signal** with the
  real `StaffAccessRight`. It already carries all four documents and a 28-item checklist, so the
  view-model property this ticket introduces is the seam it plugs into — name it deliberately.
- **[[FND-047]] (plan handle `DSK-04-11`) owns the real connectivity state machine.** This ticket
  renders "Disconnected — reconnecting"; that one decides when it is true and disables saves. The
  status-bar binding is the shared seam.
- **Every area 05 slice navigates through `INavigationService`.** [[FEAT-001]] (plan handle
  `DSK-05-01`) onward assume it exists and that no other navigation mechanism does. A second
  navigation path added later is a duplication that will not fail any build.
- **[[FND-041]] (plan handle `DSK-02-16`), the Phase 1 exit review**, requires a clean Windows 11
  machine to launch the native shell and navigate. This ticket is what makes that gate row
  answerable.
- **No OpenAPI or generated-client ripple.** This ticket introduces no contract type and calls no
  endpoint — rail counts come from a query [[FEAT-001]] owns. Say so in the PR rather than leaving
  the reviewer to check `openapi/pegasus-v1.json`.
- **No documentation ripple in this ticket.** `docs/desktop/06-ui-design/screen-specs.md` is the
  **source** and is not edited; the `docs/capabilities.md` `DSK-01` row belongs to [[FND-008]] (plan
  handle `DSK-00-08`).

## Out of scope

Recorded so the reviewer sees each was a decision, matching the ticket's Guardrails.

- **Theme token values and the theme dictionaries themselves** — [[FND-034]] wires them, [[DUI-001]]
  owns the values. This ticket consumes `{ThemeResource}` keys and defines none.
- **Sign-in, the update-required screen's behaviour, the compatibility gate, session restore** —
  area 04: [[FND-043]] (plan handle `DSK-04-07`), [[FND-044]] (plan handle `DSK-04-08`),
  [[FND-045]] (plan handle `DSK-04-09`). This ticket implements the five shell **states** as
  view-model states with placeholder content only.
- **The real role signal** — [[FND-046]]. Rail visibility binds to a view-model property here.
- **The real connectivity state machine and save disabling** — [[FND-047]].
- **The full keyboard map** — [[DUI-014]] (plan handle `DSK-06-14`) owns `Ctrl+N`, `Ctrl+S`,
  `Ctrl+W`, `Esc` and the rest. This ticket wires only the shell subset the spec names at
  `screen-specs.md:78-79`: the seven rail access keys, `Ctrl+K` and `F5`.
- **The dashboard rail-counts query** — [[FEAT-001]]. This ticket renders a nullable count and shows
  nothing when it is null.
- **Building the shell twice** — [[DUI-004]] (plan handle `DSK-06-04`) names the same deliverable
  and has no documents yet. The ownership reconciliation is recorded in this ticket's plan, as the
  Guardrails require, before any XAML is written.
- **Porting `_Layout.cshtml`** — refused. It is read for the route inventory and nothing else.
- **Editing `docs/desktop/06-ui-design/screen-specs.md`** — refused. Any ambiguity found is recorded
  in the ticket, not resolved by amending the source.
- **Relaxing `Directory.Build.props`** — never. Narrow, commented `NoWarn` entries in the desktop
  csproj only.
