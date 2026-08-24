# Files — FND-045 (plan handle `DSK-04-09`)

Surveyed 2026-08-24 against fork `main` with `ls`, `grep -n` and `cat -n`. Paths that do not
exist yet name the ticket that creates them.

## Where the change lands

| Path | Why |
| --- | --- |
| `src/Pegasus.Desktop.Infrastructure/Startup/StartupOrchestrator.cs` | **New** (project created by [[FND-031]], plan handle `DSK-02-06`). A plain state machine with **no WinUI types and no `DispatcherQueue`**, so it is testable head-less. `Startup/` is a capability folder — never `Common`, `Helpers`, `Utilities` or `Services` (`docs/engineering.md:106-111`). |
| `src/Pegasus.Desktop.Infrastructure/Startup/StartupState.cs` | **New.** One enum of exactly eight states: `CheckingForUpdate`, `UpdateRequired`, `CheckingCompatibility`, `Blocked`, `RuntimeWarning`, `RestoringSession`, `SignInRequired`, `Ready`. |
| `src/Pegasus.Desktop.Infrastructure/Startup/IPackageUpdateProbe.cs` + implementation | **New.** Wraps `new PackageManager().FindPackageForUser(string.Empty, Package.Current.Id.FullName)` then `CheckUpdateAvailabilityAsync()` on the **returned** package. **What could break:** calling it on `Package.Current` throws Access denied — the documented trap. |
| `src/Pegasus.Desktop.Infrastructure/Startup/ICompatibilityClient.cs` + implementation | **New.** `GET /api/v1/client-compatibility` (anonymous), sending `X-Pegasus-Client-Version` from `Package.Current.Id.Version`; persists the response and its retrieval timestamp through [[FND-031]]'s bounded cache. |
| `src/Pegasus.Desktop.Infrastructure/Startup/IRuntimePresenceProbe.cs` + implementation | **New.** Reads the `pv (REG_SZ)` value at `…\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}` under `HKLM\SOFTWARE\WOW6432Node` and `HKCU\Software`. **Must add no `Microsoft.Web.WebView2` package reference** — [[FND-037]]'s (plan handle `DSK-02-12`) `ForbiddenDesktopDependencyPrefixes` fails the build on it while ADR-0108 does not exist. |
| `src/Pegasus.Desktop.Infrastructure/Startup/` — the update launch | **New.** `PackageManager.RequestAddPackageByAppInstallerFileAsync` against the channel's `.appinstaller` path from the embedded channel configuration. **Never `ms-appinstaller:`** — disabled by default since December 2023. |
| `src/Pegasus.Desktop/App.xaml.cs` | **Wiring only.** Call the orchestrator **after** [[FND-035]]'s (plan handle `DSK-02-10`) single-instance redirection and **before** the main window is shown, then bind each state to its screen: `UpdateRequired` and `Blocked` to [[FND-044]]'s (plan handle `DSK-04-08`) full-window rail-less screens, `SignInRequired` to the sign-in page, `Ready` to the shell. |
| `tests/Pegasus.Desktop.ViewModelTests/Startup/StartupOrchestratorTests.cs` | **New**, in the project [[FND-038]] (plan handle `DSK-02-13`) creates: `Required` blocks; unreachable with a 23-hour-old cache proceeds; unreachable with a **25**-hour-old cache blocks; `client-unsupported` blocks carrying the minimum version; a missing WebView2 runtime warns and proceeds; a revoked refresh routes to sign-in. All with fakes and [[FND-038]]'s shared `FixedTimeProvider`. |
| `tests/Pegasus.Desktop.UITests/ui-tests.ps1` | **Invoked, never authored or forked.** `ls tests/` returns exactly `Pegasus.ArchitectureTests`, `Pegasus.Core.Tests`, `Pegasus.IntegrationTests` (2026-08-24). The harness belongs to [[TEST-006]] (plan handle `DSK-08-06`) — file, `param([Parameter(Mandatory)][int]$AppPid)` signature and `Test-UI` helper — and its update-required/blocked cases to [[FND-044]]. If neither has landed, **record the tier-7 UI check as deferred to [[TEST-006]] in the proof**. |
| `docs/frd/frd-13-desktop-operator-experience.md` | **Do not create.** `ls docs/frd/` returns FRD-01…FRD-12 only; FRD-13 is [[FND-008]]'s (plan handle `DSK-00-08`). Record the dependency instead. |
| `docs/runbook.md` | **Conditional.** A pointer to mandatory-update runbook R3 (`docs/desktop/09-release-update-and-distribution/runbooks.md:118`) is added **only once** [[REL-010]] (plan handle `DSK-09-12`) has proven that runbook; otherwise nothing is written and the dependency is recorded. |

## Context files

| Path | What it tells the implementer |
| --- | --- |
| `docs/desktop/09-release-update-and-distribution/appinstaller-template.md:156-160` § *Known behaviours* | The whole update-probe contract in five lines: `CheckUpdateAvailabilityAsync` "works only for packages installed through an `.appinstaller`; call it on the package from `PackageManager.FindPackageForUser`, **not `Package.Current`** (known access-denied issue); `Required` means the `.appinstaller` policy blocks activation". Read this before writing the probe — it is the difference between a working check and a runtime exception. |
| `docs/desktop/09-release-update-and-distribution/appinstaller-template.md:148-150` | Why the gateway gate exists at all: "if the feed is unreachable the check is skipped and the app launches; the gateway minimum-version gate (area 04) is the fail-closed layer." The package layer cannot be made to fail closed, so nothing in this ticket should try. |
| `docs/desktop/09-release-update-and-distribution/appinstaller-template.md:142-143` | `ms-appinstaller:?source=` "does nothing on most devices since December 2023". This is why "Update now" calls `RequestAddPackageByAppInstallerFileAsync` rather than launching a protocol URI. |
| `docs/desktop/09-release-update-and-distribution/appinstaller-template.md:22,40` | The feed path shape (`Uri="<feed>/<channel>/Pegasus.appinstaller"`) and the policy that produces `Required` (`OnLaunch HoursBetweenUpdateChecks="0"` with `ShowPrompt` / `UpdateBlocksActivation`). Under **D-003** the `<feed>` is a UNC share, so the path is `\\<host>\<share>\<channel>\Pegasus.appinstaller` and a check needs the office network or VPN. |
| `docs/desktop/03-gateway-api-and-data/endpoint-map.md:34` | The endpoint this orchestrator calls: `GET /client-compatibility`, "— (new, §9.1)", **anonymous**, returning "minimum/current version, channel, maintenance, TTL", phase 2. It is [[GWY-023]]'s (plan handle `DSK-04-06`) to build. |
| `docs/desktop/03-gateway-api-and-data/endpoint-map.md:35` and `src/Pegasus.Web/Program.cs:954` | The endpoint that looks like it would do and does not: `/diagnostics/version` returns `{version, sourceSha}` only — no minimum, no channel, no TTL. Recorded here so nobody wires the gate to it. |
| `docs/desktop/04-auth-session-update-and-startup/README.md` § 3 decision 6 | The 24-hour fail-closed rule and the sentence that governs every temptation to soften it: the cache "must not be extended 'for convenience'". No bypass switch, environment variable or configuration key may exist. |
| `docs/desktop/04-auth-session-update-and-startup/README.md` § 3 decision 7 | The exact ordered sequence — App Installer `OnLaunch` (outside our control) → `CheckUpdateAvailabilityAsync` via `FindPackageForUser` → `Required`/`Available` handling → compatibility gate → WebView2 presence (non-blocking until Phase 7) → session restore or native login → shell — and the requirement that "every step has a user-visible state and a diagnostics log line with the correlation id". |
| `docs/desktop/04-auth-session-update-and-startup/README.md` § 7 | Four traps in one place: `Package.Current` access-denied; a side-loaded MSIX returning `Unknown`; App Installer failing open; and the 24-hour cache never being extended. |
| `docs/desktop/06-ui-design/screen-specs.md:99-106` | The two screens this orchestrator drives: full-window, no rail, "Update required" with current and minimum versions **as values**, `Update.Required.Now` and `Update.Required.SignOut`; the Blocked variant shows the operator sentence and "Sign out" only, with `Blocked.Reason`. [[FND-044]] builds them; this ticket routes to them. |
| Microsoft Learn — *Distribute your app and the WebView2 Runtime* (fetched 2026-08-24) | The detection contract: inspect `pv (REG_SZ)` at `…\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}` under `HKLM\SOFTWARE\WOW6432Node` (per-machine, 64-bit) and `HKCU\Software` (per-user); "at least one of these regkeys must be present and defined with a version greater than `0.0.0.0`", and absent/null/empty/`0.0.0.0` means not installed. The API alternative needs the `Microsoft.Web.WebView2` package, which is forbidden here. |
| `docs/desktop/02-architecture-and-foundation/README.md` § 3 decision 8 | The single-instance redirect happens "before any window is created", which fixes where the orchestrator call goes in `App.xaml.cs`: after the redirect, before the window. |
| `docs/desktop/02-architecture-and-foundation/README.md` § 4 target-state table | `tests/Pegasus.Desktop.ViewModelTests` is for "View-model behaviour **without the dispatcher**" — the reason every collaborator here is an interface and the clock is a `TimeProvider`. |
| `docs/desktop/09-release-update-and-distribution/runbooks.md:118` § *R3 · Mandatory-update enforcement* | The runbook the `docs/runbook.md` pointer would target — and the reason the pointer is conditional: [[REL-010]] (plan handle `DSK-09-12`) proves it first. |
| `docs/frd/frd-12-operator-experience.md:22-25` | The ticket's `refs` document: the required state vocabulary ("loading, empty, current, stale, unavailable, partial, failed, validation, conflict, and access-denied") and the accessibility floor the blocked and update-required screens are measured against. |
| `docs/engineering.md:106-111` § Capability organization | Why the new types live under `Startup/` rather than a `Services` or `Helpers` folder. |
| `Directory.Build.props:6-7` | `TreatWarningsAsErrors=true`, `AnalysisLevel=latest-recommended`. A registry read and a `PackageManager` interop call are both places a nullable warning appears first. |

## Ripple effects

- **Blocked tickets unblock.** The board records this ticket blocking [[FND-049]] (plan handle
  `DSK-04-13`, the workstation first-install guide, which documents the blocked states this
  orchestrator produces), [[FND-050]] (`DSK-04-15`, the Phase 2 exit review, whose UAT script
  drives "blocked old version" and "update") and [[TEST-005]] (`DSK-08-05`, the view-model test
  catalogue, which lists "stale session" and "mandatory update" as required cases).
- **`src/Pegasus.Desktop/App.xaml.cs` gains a startup call and a state-to-screen binding**,
  sitting between [[FND-035]]'s single-instance redirect and [[FND-032]]'s host. Three tickets
  now touch that file's startup path, so it is where a merge conflict is most likely.
- **The diagnostics bundle gains its first ordered evidence.** [[FND-036]]'s (plan handle
  `DSK-02-11`) bundle already promises "the last compatibility response"; this ticket is what
  writes it, and the one-correlation-id-per-startup rule is what makes the bundle readable.
- **The bounded cache gains its first real consumer.** [[FND-031]] built it; the compatibility
  response and its retrieval timestamp are what it now holds.
- **No contract ripple, recorded because it was checked.** This ticket defines no endpoint, no
  DTO and no serialized shape — it *calls* `GET /api/v1/client-compatibility`, which
  [[GWY-023]] defines — so `openapi/pegasus-v1.json` and the generated client are untouched.
- **The architecture facts must stay green.** [[FND-037]]'s `ForbiddenDesktopDependencyPrefixes`
  includes `Microsoft.Web.WebView2`; the registry probe is precisely how this ticket satisfies
  the WebView2 presence requirement without turning that fact red.
- **A `docs/runbook.md` pointer is owed later, not now** — gated on [[REL-010]].

## Out of scope

Recording what the ticket's Guardrails already forbid, so the reviewer sees each as a
decision:

- **`src/Pegasus.Web`.** The compatibility endpoint, the minimum-version Administrator setting
  and the `/api/v1` version middleware are [[GWY-023]]'s (plan handle `DSK-04-06`). This
  ticket is a caller and edits no gateway file.
- **The session client.** [[FND-043]] (plan handle `DSK-04-07`) owns `ISessionClient`, the
  DPAPI store and the seven `SessionFailure` values; session restore here calls
  `RefreshAsync` and interprets the result.
- **The update-required and blocked screens themselves.** [[FND-044]] (plan handle
  `DSK-04-08`) owns the XAML, the copy and the AutomationIds; this ticket routes states to
  them.
- **`tests/Pegasus.Desktop.UITests/ui-tests.ps1` — the file, its `$AppPid` signature and its
  `Test-UI` helper.** [[TEST-006]] (plan handle `DSK-08-06`) owns the harness and [[FND-044]]
  contributes its cases; this ticket only invokes it, and records the check as deferred if it
  does not exist.
- **Any `Microsoft.Web.WebView2` package reference.** ADR-0108 does not exist
  (`ls docs/adr/010*` → nothing, 2026-08-24) and [[FEAT-038]] (plan handle `DSK-07-12`)
  authors it; the registry probe is the alternative.
- **Any bypass of the 24-hour fail-closed cache** — no configuration key, no environment
  variable, no debug-only branch. Plan 04 § 3 decision 6.
- **The `.appinstaller` template, the local Test/UAT feed and the UNC feed host.**
  [[REL-003]] (plan handle `DSK-09-03`), [[FND-048]] (`DSK-04-12`) and [[REL-008]]
  (`DSK-09-10`).
- **`docs/frd/frd-13-desktop-operator-experience.md`.** [[FND-008]] (plan handle `DSK-00-08`)
  authors it.
- **Any Azure write.** Plan 04 § 3 decision 5 deliberately puts the minimum version in the
  database so raising it is an administrative action rather than a Container App app-setting
  change.
