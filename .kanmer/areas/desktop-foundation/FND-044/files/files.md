# Files — FND-044 (plan handle `DSK-04-08`)

Surveyed 2026-08-24 against fork `main` with `ls`, `wc -l` and `grep -n`. Paths that do not
exist yet name the ticket that creates them.

## Where the change lands

| Path | Why |
| --- | --- |
| `src/Pegasus.Desktop/Session/SignInPage.xaml` + `.xaml.cs` | **New** (project created by [[FND-030]], plan handle `DSK-02-05`). Navless frame with the logo and the **company** name — not the product (`_LayoutAuth` convention, `docs/desktop/06-ui-design/screen-specs.md:87-88`). Fields User name and Password, label plus control only; primary **Sign in**. No "forgot password" — the spec says it is "not a current capability" (`:89`). AutomationIds exactly `SignIn.UserName`, `SignIn.Password`, `SignIn.Submit`, `SignIn.Problem`. A `Session/` **capability** folder — never `Common`, `Helpers`, `Utilities` or `Services` (`docs/engineering.md:106-111`). |
| `src/Pegasus.Desktop/Session/SignInViewModel.cs` | **New.** The five inline states as view-model state, not dialogs: idle; signing in (submit disabled, thin indeterminate progress); invalid credentials; rate limited; unreachable. Switches on [[FND-043]]'s (plan handle `DSK-04-07`) seven-value `SessionFailure` and adds none. |
| `src/Pegasus.Desktop/Session/UpdateRequiredPage.xaml` + `.xaml.cs` + view model | **New.** Full-window, rail-less. Title "Update required"; current and minimum versions rendered **as values**; primary "Update now"; secondary "Sign out". The **Blocked** variant (account disabled, or the compatibility fail-closed state) shows the operator sentence and "Sign out" only. AutomationIds `Update.Required.Now`, `Update.Required.SignOut`, `Blocked.Reason` (`screen-specs.md:99-106`). |
| `src/Pegasus.Desktop/Session/ChangePasswordPage.xaml` + `.xaml.cs` (navigation target only) | **New, minimal.** This ticket **routes** `PasswordChangeRequired` here and establishes the four AutomationIds `Password.Current`, `Password.New`, `Password.Confirm`, `Password.Save` (`screen-specs.md:108-113`). The change-password behaviour itself is [[FEAT-021]]'s (plan handle `DSK-05-21`). |
| `src/Pegasus.Desktop/App.xaml.cs` or the navigation service | **Registration only** — the three pages join the route table [[FND-033]] (plan handle `DSK-02-08`) owns. Do not re-implement navigation here. |
| `tests/Pegasus.Desktop.ViewModelTests/Session/SignInViewModelTests.cs` | **New**, in the project [[FND-038]] (plan handle `DSK-02-13`) creates. One test per session-failure matrix row, asserting the resulting state, the message identity and whether submit is enabled — all without a `DispatcherQueue`. |
| `tests/Pegasus.Desktop.UITests/ui-tests.ps1` | **Contribute seven cases.** `ls tests/` returns exactly `Pegasus.ArchitectureTests`, `Pegasus.Core.Tests`, `Pegasus.IntegrationTests` (2026-08-24), so neither the folder nor the file exists yet. The skeleton is **[[TEST-006]]'s** (plan handle `DSK-08-06`): `param([Parameter(Mandatory)][int]$AppPid)` — never `$Pid`, which is read-only in PowerShell — and a `Test-UI` pass/fail helper. If [[TEST-006]] has not landed, create the file to **exactly** that signature and helper so the two cannot fork, and record that [[TEST-006]] takes ownership of the skeleton. |
| `artifacts/ui-tests/` screenshots | **New output, not tracked.** `.gitignore:23-24` already ignores `**/artifacts/` and `/artifacts/`, so the seven state screenshots are proof attachments rather than repository files. |
| `docs/frd/frd-13-desktop-operator-experience.md` | **Do not create.** `ls docs/frd/` shows FRD-01…FRD-12 only; FRD-13 is [[FND-008]]'s (plan handle `DSK-00-08`). Record the dependency in the plan instead. |
| `docs/desktop/06-ui-design/screen-specs.md` | **Only if an implemented state differs from the spec**, and then as a correction carrying its reason. The spec is binding, not a starting point. |

## Context files

| Path | What it tells the implementer |
| --- | --- |
| `docs/desktop/06-ui-design/screen-specs.md:85-97` | The whole sign-in specification in thirteen lines: the navless frame and the `_LayoutAuth` convention (logo plus **company** name, not the product), "User name, Password (label + control only)", the deliberate absence of "forgot password", the eight states each already resolved to a behaviour, and the four AutomationIds. Read this before any XAML; nothing here is a suggestion. |
| `docs/desktop/06-ui-design/screen-specs.md:99-106` | The Update required / Blocked screen. Two details are easy to miss: the versions are rendered "as values" (a labelled value, not a sentence), and the **Blocked** variant drops "Update now" entirely — it offers only the operator sentence and "Sign out". |
| `docs/desktop/06-ui-design/screen-specs.md:108-113` | Change password. The rule that bites even on a routing-only stub: "minimum length is shown only as a validation outcome, **never as hint text**". |
| `src/Pegasus.Web/Pages/Account/SignIn.cshtml.cs:21-31` | The two required-field messages **and the recorded reason they are explicit**: the framework default names the bind property ("The UserName field is required."), "which is a C# identifier, not a word the operator has ever seen on this screen". Copy "Enter your username." and "Enter your password." verbatim; the comment is why. |
| `src/Pegasus.Web/Pages/Account/SignIn.cshtml.cs:73-74` | The invalid-credential sentence that must match word for word: "The username or password is incorrect. If your access has changed, contact an administrator." It is deliberately generic — it does not say whether the account exists. |
| `src/Pegasus.Web/Pages/Account/SignIn.cshtml.cs:13` and `src/Pegasus.Core/Actors/StaffSessionPolicy.cs:12` | Where the rate limit comes from and what the number is: `[EnableRateLimiting("StaffSignIn")]` and `SignInAttemptsPerClientPerMinute = 10`. The desktop does not enforce it — it renders the `429`'s `Retry-After` — but the number tells you the wait is seconds, not minutes of guesswork. |
| `docs/desktop/04-auth-session-update-and-startup/README.md` § 3 *Session failure matrix* | The seven rows, gateway signal → desktop behaviour. The two rows this screen most often gets wrong: a transport exception is the **disconnected** state and is "never shown as bad credentials"; `429` shows the wait time and disables submit until it elapses. |
| `docs/desktop/06-ui-design/keyboard-and-accessibility.md:1-7` | The three non-negotiables stated as the document's own purpose: keyboard-only completion, perceivable without colour, reachable by UI Automation. |
| `docs/desktop/06-ui-design/keyboard-and-accessibility.md:24,39-42` | The global `Tab` order (title bar → rail → page header → content → status bar), which the navless login frame reduces to User name → Password → Sign in; and the focus visual — a 3px `PegasusFocusBrush` ring, with forced-colours mode using the **system** focus visual rather than the brush. |
| `docs/design/README.md:424` | "Operator direction, 2026-08-20: stop explaining pages." This is the authority behind the ticket's trap "a message that 'explains' how the system works is a defect". One sentence, one action, no description of internals. |
| `docs/design/README.md:774-792` § Accessibility | The enumerated required behaviours. Four bite here: labelled navigation, **associated field errors and error summaries**, visible focus, and **non-colour state cues** — an `InfoBar` whose only difference between states is its colour fails this. |
| `docs/design/README.md:1300-1302` | The recording obligation, and the limit on automation: keyboard, screen-reader, focus/error, forced-colours, reduced-motion, 1280+ desktop, constrained desktop and 200%-zoom inspection "must be recorded", and "generated imagery or synthetic operational material cannot prove acceptance". This is why tier 7 needs artefacts, not a green log. |
| `docs/frd/frd-12-operator-experience.md:20-25` | The ticket's `refs` document. It requires "loading, empty, current, stale, unavailable, partial, failed, validation, conflict, and access-denied states" and "keyboard, pointer, screen-reader, 200% zoom, forced-colour, and reduced-motion support" — the state vocabulary and the accessibility floor this screen is measured against. |
| `docs/desktop/01-inventory-and-parity/parity-matrix.md:46,48,49` | `PAR-01` (sign-in, the row this screen replaces), `PAR-03` (password change, routed to) and `PAR-04` (access denied, the "denied" problem state). |
| `.codex/skills/winui-design/SKILL.md` | The skill the body requires **before** any XAML: its *Search samples before writing XAML* section (run `winui-search.exe` for the form controls) and its *XAML landmines* and *Theming rules* sections. Loading it after writing the page is loading it too late. |
| `docs/engineering.md:106-111` § Capability organization | Why the new pages live under `Session/` and not under `Views/`, `Pages/Common` or `Services`. |
| `Directory.Build.props:6-7` | `TreatWarningsAsErrors=true`, `AnalysisLevel=latest-recommended` — generated XAML partials included. A `WUI*` or nullable warning fails the build. |
| `.gitignore:23-24` | `**/artifacts/` and `/artifacts/` are ignored, so `artifacts/ui-tests/` screenshots are proof attachments and never appear in `git status`. |

## Ripple effects

- **Blocked tickets unblock.** The board records this ticket blocking [[FND-050]] (plan handle
  `DSK-04-15`, the Phase 2 exit review, which drives every one of these states in its UAT
  script) and [[FEAT-021]] (`DSK-05-21`, password change, which fills in the screen this
  ticket only routes to).
- **The AutomationId contract propagates.** Eleven ids are established here and consumed by
  [[TEST-006]]'s harness and by [[DUI-015]]'s (plan handle `DSK-06-15`) AutomationId coverage
  audit. A renamed id breaks both.
- **`tests/Pegasus.Desktop.UITests/ui-tests.ps1` gains seven cases** — and possibly the file
  itself. Either way its signature and helper are [[TEST-006]]'s to own; this ticket must not
  introduce a second harness or a second helper.
- **The navigation route table grows** by three routes, owned by [[FND-033]] (plan handle
  `DSK-02-08`).
- **No contract ripple, recorded because it was checked.** This ticket adds no endpoint, no
  DTO and no serialized shape, so `openapi/pegasus-v1.json` and the generated client — the
  usual ripple on this board — are untouched.
- **The architecture facts must stay green.** [[FND-037]]'s (plan handle `DSK-02-12`)
  `DesktopXamlContainsNoWebView` scans `src/Pegasus.Desktop/**/*.xaml`; three new XAML files
  enter its scope, and none may contain a `WebView2` element — ADR-0108 does not exist
  (`ls docs/adr/010*` returns nothing, 2026-08-24).
- **FRD-13 is owed but not by this ticket.** [[FND-008]] writes it; this ticket records the
  dependency so the reviewer sees the gap was deliberate.

## Out of scope

Recording what the ticket's Guardrails already forbid, so the reviewer sees each as a
decision:

- **`src/Pegasus.Web`, including `Pages/Account/`.** The Razor sign-in page stays live until
  cutover; this ticket reads it for copy and edits nothing. `src/Pegasus.Core` and every
  gateway project are likewise read-only here.
- **The session client itself.** [[FND-043]] (plan handle `DSK-04-07`) owns `ISessionClient`,
  the DPAPI store and the seven `SessionFailure` values; this screen consumes them.
- **The startup orchestrator, the update check and the 24-hour fail-closed cache.**
  [[FND-045]] (plan handle `DSK-04-09`). "Update now" binds to the entry point that ticket
  exposes; if it has not landed, the command logs and disables itself and the plan says so.
- **The change-password behaviour.** [[FEAT-021]] (plan handle `DSK-05-21`) owns it; this
  ticket adds the route and the four AutomationIds.
- **The `ui-tests.ps1` skeleton, its `$AppPid` signature and its `Test-UI` helper.**
  [[TEST-006]] (plan handle `DSK-08-06`). This ticket contributes the seven session-failure
  cases and nothing else in that folder.
- **The theme dictionaries.** [[FND-034]] (plan handle `DSK-02-09`) owns Light/Dark/HighContrast;
  this screen consumes theme resources and writes **no colour literal**.
- **Any modal dialog for a routine state**, any "remember me" or password persistence, and any
  port of `_LayoutAuth.cshtml` — the frame is a navless WinUI frame, not a translated web
  layout.
- **`docs/frd/frd-13-desktop-operator-experience.md`.** Owned by [[FND-008]]; creating it here
  would take a document out of its author's hands.
