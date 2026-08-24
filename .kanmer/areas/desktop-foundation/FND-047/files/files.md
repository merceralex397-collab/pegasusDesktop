# Files — FND-047

Surveyed 2026-08-24 against the fork working tree. **Nothing this ticket edits
exists yet**: every path under `src/Pegasus.Desktop*` and
`tests/Pegasus.Desktop.*` is created by a named earlier ticket, marked below.
Confirmed with `ls src` → `Pegasus.Core`, `Pegasus.Infrastructure`,
`Pegasus.Web`, `Pegasus.Worker`; `ls tests` → `Pegasus.ArchitectureTests`,
`Pegasus.Core.Tests`, `Pegasus.IntegrationTests`.

## Where the change lands

| Path | Why |
| --- | --- |
| `src/Pegasus.Desktop.Infrastructure/Connectivity/IConnectivityState.cs` — **new** (project created by [[FND-031]] (plan handle `DSK-02-06`)) | The contract: `Connected \| Disconnected`, the timestamp of the last successful server response, and a change event (body step 2). Interface lives beside its only implementation because the project references Core and Contracts only (`docs/desktop/02-architecture-and-foundation/README.md:210`); putting it in Core would import a client concern into the domain. |
| `src/Pegasus.Desktop.Infrastructure/Connectivity/ConnectivityState.cs` — **new** | The one state object. Holds the value, the last-success timestamp, raises the change event, and takes `TimeProvider` so the interval is testable (body step 2). Breaks if the change event is raised off a background thread without marshalling — every subscriber is a view model. |
| `src/Pegasus.Desktop.Infrastructure/Connectivity/ConnectivityRecheckService.cs` — **new** | The `Disconnected`-only poll of `GET /api/v1/client-compatibility` on the named-constant interval (body step 4). The constant lives here, in one place, and nowhere else. Breaks the app if it keeps polling after reconnection, or if it starts before sign-in. |
| `src/Pegasus.Desktop.Infrastructure/Http/` — the existing `DelegatingHandler` **edited** (created by [[FND-031]]) | Body step 3 is explicit that the state is set *from the pipeline*: mark disconnected on a transport exception or TLS failure, connected on any successful response. Editing the existing handler rather than adding a second one is what keeps "one state object, one signal source" (body Traps). Breaks if it swallows the exception it classifies — it must re-throw. |
| `src/Pegasus.Desktop/ViewModels/ShellViewModel.cs` — **edited** (created by [[FND-033]] (plan handle `DSK-02-08`)) | Exposes the connection text, the reconnecting indicator flag and the last-sync time to the status bar, and re-renders on the change event. Breaks the whole shell if it subscribes without unsubscribing on window close. |
| `src/Pegasus.Desktop/Views/ShellPage.xaml` — **edited** (created by [[FND-033]]) | The status-bar region binds the connection text under `AutomationProperties.AutomationId="Shell.Status.Connection"` and shows the thin indeterminate `ProgressBar` while reconnecting. Breaks the UI assertion if the id moves or if the text becomes colour-only. |
| `src/Pegasus.Desktop/Commands/ConnectivityAwareCommand.cs` — **new** | The single base command behaviour whose `CanExecute` is false while `Disconnected` (body step 6). Every authoritative save routes through it; nothing else re-implements the check. |
| `src/Pegasus.Desktop/App.xaml.cs` — **edited** (host and DI created by [[FND-032]] (plan handle `DSK-02-07`)) | Registers `IConnectivityState` as a singleton and the recheck service in the generic host. Two lines, but the singleton lifetime is load-bearing: a scoped registration silently gives each view model its own state. |
| `tests/Pegasus.Desktop.ViewModelTests/ConnectivityStateTests.cs` — **new** (project created by [[FND-038]] (plan handle `DSK-02-13`)) | Body step 10's first three cases: a transport exception flips within one handler pass; a successful recheck flips back; a transport failure never produces an invalid-credentials message. Uses the project's fake clock and fake API client. |
| `tests/Pegasus.Desktop.ViewModelTests/ConnectivityCommandGatingTests.cs` — **new** | Body step 10's remaining case plus step 8's audit: `CanExecute == false` while disconnected, `true` after the flip back, and a transport failure produces a failure state and never a success state with a local-only effect. |
| `tests/Pegasus.Desktop.UITests/ui-tests.ps1` — **edited, cases appended only** (file, `param([Parameter(Mandatory)][int]$AppPid)` signature and `Test-UI` helper owned by [[TEST-006]] (plan handle `DSK-08-06`)) | The disconnected and reconnect `Test-UI` cases (body step 11). Contributing cases is in scope; changing the signature or the helper is not — the body's Guardrails say so in as many words. |

## Context files

Read these; do not edit them.

| Path | What it tells the implementer |
| --- | --- |
| `docs/desktop/04-auth-session-update-and-startup/README.md:230` | The exact row this ticket implements: "Server unreachable / TLS failure → **transport exception** → Disconnected state in the status bar; periodic recheck; never shown as bad credentials". The signal is an exception, **not** an HTTP status code — the five rows above it own `401`, `429`, `invalid_grant` and the problem types, and taking one of those as "disconnected" collides with a ticket that already owns it. |
| `docs/desktop/04-auth-session-update-and-startup/README.md:178-188` | Decision 5: `GET /api/v1/client-compatibility` is **anonymous**, has no rate-limit bypass, and returns `minimumVersion`, `currentVersion`, `channel`, `maintenanceMessage`, `validForSeconds`. Anonymity is why it is safe to poll while the session may itself be dead — do not attach a bearer token to the recheck. |
| `docs/desktop/06-ui-design/screen-specs.md:74-78` | The settled wording and behaviour, verbatim: status bar carries "connection state, last sync time (Europe/London)…"; "Connectivity state: 'Disconnected — reconnecting' in the status bar; saves disabled; existing content visible". Do not invent alternative copy — `docs/design/README.md` binds every UI string. |
| `docs/desktop/06-ui-design/screen-specs.md:31-39` | The AutomationId convention `<Screen>.<Region>.<Element>` and the rule "State is never colour alone" that makes a glyph-only indicator a defect. `:85-86` fixes `Shell.Status.Connection` and `Shell.Status.Update` as the shell's ids. |
| `docs/desktop/06-ui-design/tokens-and-theme.md:184` | The only permitted progress affordance: "thin indeterminate `ProgressBar` … no ring spinners; honours `UISettings.AnimationsEnabled` with a static 'Working' text equivalent". `docs/desktop/06-ui-design/README.md:165` repeats it and adds "no full-page spinners; no animated transitions". This closes the design question before it opens. |
| `src/Pegasus.Web/wwwroot/js/site.js:298-318`, `:437-447`, `:644-664` | What "today" actually is, and why it is not a precedent to copy: `:317` and `:446` catch a failed `fetch` and call `form.submit()`, handing the operator to the browser's error page; `:663` is the only Pegasus-worded failure and it is per-panel and disables nothing. The desktop cannot delegate to a browser, which is the whole reason this ticket exists. |
| `src/Pegasus.Web/Program.cs:939-950`, `:954` | The health and version endpoints behind parity row `PAR-45` (`/health/live`, `/health/ready`, `GET /diagnostics/version`). They are `.AllowAnonymous().ShortCircuit()` — useful as the shape the compatibility endpoint follows, and a reminder that the recheck target is **not** one of these three: `PAR-45`'s own API column names `GET /api/v1/client-compatibility`. |
| `docs/desktop/01-inventory-and-parity/parity-matrix.md` row `PAR-45` | The row this ticket advances, already carrying "Status bar health (§18.3), About/version" as the native design and status `inventoried`. `PAR-01` is adjacent but scopes connectivity to the **login screen**, owned by [[FND-044]] (plan handle `DSK-04-08`) — do not claim `PAR-01`. |
| `src/Pegasus.Core/Documents/RequestUploadPolicy.cs:341-343` and `src/Pegasus.Core/Cases/CaseNotes.cs:33` | The repository's established `TimeProvider` injection pattern, constructor-injected and stored in a readonly field. Follow it rather than inventing an `IClock`; `grep -rn "interface IClock" src` returns nothing, so an `IClock` would be a second concept. |
| `Directory.Build.props:3-8` | `Nullable=enable`, `AnalysisLevel=latest-recommended` and **`TreatWarningsAsErrors=true`** apply solution-wide. An `async void` handler, an unawaited poll task or a nullable slip in the recheck loop fails the build rather than warning. |
| `scripts/Invoke-LocalDevelopment.ps1:3` | `[ValidateSet('Start', 'Status', 'Smoke', 'Stop', 'Reset')]` — `Stop` and `Start` are real values, so the body's "stop the gateway mid-run" step is runnable exactly as written. This is the L-02 way of producing "offline"; there is no Azure outage to arrange. |
| `.codex/skills/winui-ui-testing/SKILL.md:47`, `:57`, `:74`, `:138` | The harness contract to conform to, not to redesign: the `param([Parameter(Mandatory)][int]$AppPid)` signature (`$Pid` is read-only in PowerShell), the `Test-UI` helper that counts pass/fail, `wait-for … -a $AppPid -t <ms>`, and — the one this ticket needs — `wait-for "StatusBar" --value "words" --contains` for "substring match for dynamic content". The status-bar text carries a timestamp, so `--contains` is the only assertion that can work. |
| `.codex/skills/winui-ui-testing/SKILL.md:115` | `winapp ui screenshot -a $AppPid -o "screenshots/01-initial.png"` — the exact form for body step 12's two screenshots. |
| `docs/engineering.md:72-84` § Required evidence tiers | Tier 7 is "Browser/accessibility … keyboard, focus and error behavior, semantic labels, **text-plus-colour states**". The body's tier is 7, and "text-plus-colour" is the clause that makes a colour-only dot a tier failure, not a style preference. |

## Ripple effects

- **`Pegasus.slnx`** — no change. The two projects this ticket edits are added
  to the solution by [[FND-030]] (plan handle `DSK-02-05`) and [[FND-031]];
  today the file lists only the four `src` projects and three `tests`
  projects. Adding a file to an SDK-style project needs no solution edit.
- **`tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs`** — the
  desktop boundary facts are extended by [[FND-037]] (plan handle
  `DSK-02-12`), not here, but this ticket must not violate them: the
  connectivity types live in `Pegasus.Desktop.Infrastructure`, which may
  reference Core and Contracts and **must not** reference
  `Pegasus.Infrastructure`, EF Core or any Azure client
  (`docs/desktop/02-architecture-and-foundation/README.md:210, :216`). A
  reference added here turns [[FND-037]]'s test red.
- **`tests/Pegasus.Desktop.UITests/ui-tests.ps1`** — gains two cases. Its
  pass/fail count changes, so any CI lane that asserts a fixed case count
  ([[TEST-013]], plan handle `DSK-08-13`) reads the new total rather than a
  literal.
- **`docs/desktop/01-inventory-and-parity/parity-matrix.md` row `PAR-45`** —
  its status advances toward `implemented` once this ships. The row is edited
  by the area 01 tickets ([[FND-018]], plan handle `DSK-01-05`), not by this
  ticket; this ticket's proof is the evidence they cite.
- **FRD-13 "Desktop operator experience"** — gains the disconnected state and
  its effect on commands, as the body's Documentation changes section says.
  Authored by [[FND-008]] (plan handle `DSK-00-08`); this ticket writes no
  FRD text.
- **No contract ripple.** This ticket adds no endpoint, changes no request or
  response shape, and therefore does **not** touch `openapi/pegasus-v1.json`
  or the generated client — the usual ripple on this board. It only *calls* an
  endpoint that [[GWY-023]] (plan handle `DSK-04-06`) defines and snapshots.
- **No Azure ripple.** No app setting, no Bicep change in
  `infra/modules/platform.bicep`, no Azure read or write. L-02 makes the
  offline scenario a stopped local gateway.

## Out of scope

Recorded so the reviewer sees each was a decision, not an oversight. Every
item below is drawn from the ticket's own Guardrails.

- **`src/Pegasus.Web` — untouched.** The gateway side of connectivity is the
  compatibility endpoint, owned by [[GWY-023]].
- **Any local persistence of pending commands.** No queue, no outbox, no
  retry store. Proposal § 11.3 permits only an explicit draft, drafts belong
  to area 05, and ADR-0104 (online-required, authored by [[FND-005]] (plan
  handle `DSK-00-05`)) is the decision behind it. A queue would import the
  conflict resolution proposal § 11.2 rules out.
- **A second connectivity signal source.** No `NetworkInformation`
  availability-changed subscription, no independent ping loop as the primary
  signal, no per-view-model probe. One state object, set from the real HTTP
  pipeline (body step 3 and Traps).
- **The login screen's own connectivity sentence.** Owned by [[FND-044]]
  (plan handle `DSK-04-08`) against the same matrix row; this ticket owns the
  shell status bar.
- **The startup-time compatibility gate and its 24-hour fail-closed cache.**
  Owned by [[FND-045]] (plan handle `DSK-04-09`). This ticket reuses the same
  endpoint as a liveness probe and caches nothing.
- **`Shell.Status.Update`.** The update-availability half of the status bar
  belongs to [[FND-045]]; this ticket touches only
  `Shell.Status.Connection`.
- **The `ui-tests.ps1` skeleton** — the file, its
  `param([Parameter(Mandatory)][int]$AppPid)` signature and its `Test-UI`
  helper belong to [[TEST-006]]. This ticket contributes cases and nothing
  else in that folder.
- **Drafts and unsaved-work preservation across a disconnection.** Area 05.
  This ticket disables the save; it does not decide what happens to the text
  in the box.
