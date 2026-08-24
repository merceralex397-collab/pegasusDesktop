# Research — FND-045: what the startup sequence must check, in what order, and what must fail closed

## Question

What are the exact platform calls, results and failure semantics of the four startup steps —
package update check, gateway compatibility gate, WebView2 runtime presence, session restore
— and what must the orchestrator do when each one cannot answer?

## Current behaviour

**No parity-matrix row covers this, and none should.** The matrix
(`docs/desktop/01-inventory-and-parity/parity-matrix.md`) holds `PAR-01`…`PAR-46`
(`grep -c '^| PAR-' …` → `46`), every row keyed to a Razor page model under
`src/Pegasus.Web/Pages/**`. A web application has no startup sequence to reach parity with:
the browser is the update mechanism, the tab is the session, and there is no client version to
gate. This whole capability is **new** under proposal § 9.

The closest existing repository mechanism is the version surface the desktop will *not* be
able to use as-is:

- `src/Pegasus.Web/Program.cs:954` — `app.MapGet("/diagnostics/version", …)` returns
  `{ version, sourceSha }` and is `AllowAnonymous()`. It is a **diagnostics** endpoint: it
  reports what the gateway is, not what a client must be. It carries no `minimumVersion`, no
  channel, no maintenance message and no TTL, so it cannot drive a gate.
- `docs/desktop/03-gateway-api-and-data/endpoint-map.md:34` records the endpoint that will:
  `GET /client-compatibility`, "— (new, §9.1)", anonymous, returning "minimum/current version,
  channel, maintenance, TTL", phase 2. Line `:35` keeps `/diagnostics/version` beside it as
  "(existing)" and points at `Program.cs:954`, so the two are deliberately distinct.

## Findings

- **The package update check has one correct call shape and one documented way to get it
  wrong.** `docs/desktop/09-release-update-and-distribution/appinstaller-template.md:156-160`
  § *Known behaviours*: "`Package.CheckUpdateAvailabilityAsync` works only for packages
  installed through an `.appinstaller`; call it on the package from
  `PackageManager.FindPackageForUser`, **not `Package.Current`** (known access-denied issue);
  `Required` means the `.appinstaller` policy blocks activation." Plan 04 § 2 records the same
  and adds the five results: `NoUpdates | Available | Required | Unknown | Error`.
  - Consequence: a side-loaded development MSIX returns `Unknown`, so the head-less tests
    cannot prove this path and the operator's real-feed run (body step 13) is the only
    evidence that can.
- **App Installer fails open, and that is the entire reason the gateway gate exists.**
  `appinstaller-template.md:148-150`: "if the feed is unreachable the check is skipped and the
  app launches; the gateway minimum-version gate (area 04) is the fail-closed layer." Plan 04
  § 3 decision 6 fixes the fail-closed rule at **24 hours** and § 7 says the cache "must not be
  extended 'for convenience'".
- **`ms-appinstaller:` is disabled by default and must not be used.**
  `appinstaller-template.md:142-143` (since December 2023). The update is started from code
  with `PackageManager.RequestAddPackageByAppInstallerFileAsync` against the channel's
  `.appinstaller` path, which the template writes as `Uri="<feed>/<channel>/Pegasus.appinstaller"`
  (`:22`) — under **D-003** that resolves to a UNC path
  `\\<host>\<share>\<channel>\Pegasus.appinstaller`, so an update check needs the office
  network or VPN.
- **The `.appinstaller` policy that produces `Required` is already written down.**
  `appinstaller-template.md:40` — `<OnLaunch HoursBetweenUpdateChecks="0" …>` with
  `ShowPrompt="true"` and `UpdateBlocksActivation="true"`; `:144-147` explains the behaviour
  pair, and `:69` records that the 2021 schema is required and that Windows 11 supports it on
  every build.
- **WebView2 runtime detection is unambiguous in the official documentation, and only one of
  the two documented approaches is available here.** Microsoft Learn, *Distribute your app and
  the WebView2 Runtime* § "The Evergreen Runtime distribution mode" → "Detect if a WebView2
  Runtime is already installed" (fetched 2026-08-24,
  <https://learn.microsoft.com/microsoft-edge/webview2/concepts/distribution>):
  - **Approach 1 — registry.** Inspect the `pv (REG_SZ)` value at the WebView2 Runtime client
    key. On 64-bit Windows the two locations are
    `HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}`
    and
    `HKEY_CURRENT_USER\Software\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}`
    (per-machine and per-user installs respectively). "At least one of these regkeys must be
    present and defined with a version greater than `0.0.0.0`. If neither regkey exists, or if
    only one exists but its value is `null`, an empty string, or `0.0.0.0`, this means that the
    WebView2 Runtime isn't installed."
  - **Approach 2 — API.** `GetAvailableCoreWebView2BrowserVersionString` (Win32) or
    `CoreWebView2Environment.GetAvailableBrowserVersionString` (.NET), which throws
    `WebView2RuntimeNotFoundException` when the runtime is missing. **This requires the
    `Microsoft.Web.WebView2` package**, which [[FND-037]]'s (plan handle `DSK-02-12`)
    `ForbiddenDesktopDependencyPrefixes` fact fails the build on until ADR-0108 lands
    (`ls docs/adr/010*` returns nothing, 2026-08-24).
  - **Therefore Approach 1 is the only viable mechanism, and the documentation is explicit
    rather than ambiguous** — so the ticket's Guardrail "raise it as an open question **if the
    documentation is ambiguous**" does not trigger. The key, the value name and the
    "greater than 0.0.0.0" rule are all quoted above; the choice is recorded in the plan, not
    asked.
  - The same page notes the Evergreen Runtime "will be included as part of the Windows 11
    operating system", which is why plan 04 § 2 calls a missing runtime a **warning** rather
    than a blocker in Phase 2.
- **The compatibility endpoint's shape is fixed and this ticket is a caller.**
  `endpoint-map.md:34` gives the route, the anonymous auth and the four returned values;
  plan 04 § 3 decision 5 adds the problem type `urn:pegasus:problem:client-unsupported` for a
  below-minimum request and records that the minimum is a **database-backed Administrator
  setting with audit**, not a Container App app setting — so raising the minimum is an
  administrative action rather than an Azure write. [[GWY-023]] (plan handle `DSK-04-06`) owns
  all of it.
- **The state machine must be head-less to be testable at all.** Plan 02 § 4's target-state row
  for `tests/Pegasus.Desktop.ViewModelTests` is "View-model behaviour **without the
  dispatcher**", and every one of this ticket's interesting cases — a 23-hour-old cache versus
  a 25-hour-old one — needs an injectable clock. `TimeProvider` is the repository's convention
  and [[FND-038]] (plan handle `DSK-02-13`) ships the single shared `FixedTimeProvider`.
- **Ordering against single instance matters.** Body step 11 places the orchestrator after
  [[FND-035]]'s (plan handle `DSK-02-10`) `AppInstance` redirection and **before** the main
  window is shown. Plan 02 § 3 decision 8 requires the redirect to happen "before any window is
  created", so an orchestrator that ran first would draw a window in a process that is about to
  redirect and exit.
- **The tier-7 half depends on a harness this ticket must not write.**
  `tests/Pegasus.Desktop.UITests/ui-tests.ps1` is [[TEST-006]]'s (plan handle `DSK-08-06`) —
  file, `param([Parameter(Mandatory)][int]$AppPid)` signature and `Test-UI` helper — and the
  update-required and blocked cases are [[FND-044]]'s (plan handle `DSK-04-08`) step 10.
  `ls tests/` returns three projects (2026-08-24), so neither exists yet. The body already
  resolves it: if neither has landed, record the tier-7 UI check as **deferred to
  [[TEST-006]]** in the proof rather than writing a second harness.
- **Runbook R3 exists as a plan document but is not yet proven.**
  `docs/desktop/09-release-update-and-distribution/runbooks.md:118` § *R3 · Mandatory-update
  enforcement*. The ticket's Documentation-changes section makes the `docs/runbook.md` pointer
  conditional on [[REL-010]] (plan handle `DSK-09-12`) having proven it — so this ticket
  records the dependency rather than pointing at an unproven runbook.

### Facts

| Fact | Source |
| --- | --- |
| `/diagnostics/version` returns `{version, sourceSha}`, anonymous — and is **not** the compatibility endpoint | `src/Pegasus.Web/Program.cs:954-958` |
| `GET /client-compatibility` is new, anonymous, returns minimum/current version, channel, maintenance, TTL | `docs/desktop/03-gateway-api-and-data/endpoint-map.md:34` |
| `/diagnostics/version` is kept beside it as "(existing)" | `docs/desktop/03-gateway-api-and-data/endpoint-map.md:35` |
| Call `CheckUpdateAvailabilityAsync` on the package from `PackageManager.FindPackageForUser`, never `Package.Current`; works only for `.appinstaller` installs; `Required` means the policy blocks activation | `docs/desktop/09-release-update-and-distribution/appinstaller-template.md:156-160` |
| App Installer fails **open** when the feed is unreachable; the gateway gate is the fail-closed layer | `docs/desktop/09-release-update-and-distribution/appinstaller-template.md:148-150` |
| `ms-appinstaller:` does nothing on most devices since December 2023 | `docs/desktop/09-release-update-and-distribution/appinstaller-template.md:142-143` |
| The `.appinstaller` `Uri` is `<feed>/<channel>/Pegasus.appinstaller`; `OnLaunch HoursBetweenUpdateChecks="0"` | `docs/desktop/09-release-update-and-distribution/appinstaller-template.md:22,40` |
| Runbook R3 · Mandatory-update enforcement exists as a plan document | `docs/desktop/09-release-update-and-distribution/runbooks.md:118` |
| WebView2 detection: `pv (REG_SZ)` under `…\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}` in HKLM (`WOW6432Node` on 64-bit) and HKCU; absent/null/empty/`0.0.0.0` means not installed | Microsoft Learn, *Distribute your app and the WebView2 Runtime*, fetched 2026-08-24 — <https://learn.microsoft.com/microsoft-edge/webview2/concepts/distribution> |
| The API alternative `CoreWebView2Environment.GetAvailableBrowserVersionString` throws `WebView2RuntimeNotFoundException` — but lives in the `Microsoft.Web.WebView2` package | Microsoft Learn, `CoreWebView2Environment.GetAvailableBrowserVersionString`, fetched 2026-08-24 |
| ADR-0108 does not exist, so a `Microsoft.Web.WebView2` reference fails [[FND-037]]'s fact | `ls docs/adr/010*` → nothing, 2026-08-24 |
| Update-required / Blocked screen: full-window, no rail, `Update.Required.Now`, `Update.Required.SignOut`, `Blocked.Reason` | `docs/desktop/06-ui-design/screen-specs.md:99-106` |
| Desktop view-model tests run "without the dispatcher" | `docs/desktop/02-architecture-and-foundation/README.md` § 4 target-state table |
| The single-instance redirect happens before any window is created | `docs/desktop/02-architecture-and-foundation/README.md` § 3 decision 8 |
| `tests/` holds three projects; `Pegasus.Desktop.UITests` does not exist yet | `ls tests/`, 2026-08-24 |
| No parity row covers startup; the matrix is 46 page-model rows | `grep -c '^| PAR-' docs/desktop/01-inventory-and-parity/parity-matrix.md` → 46 |

### Assumptions

- **A-04-09-1 — a packaged WinUI 3 app can read
  `HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-…}` without
  elevation.** MSIX gives package identity, not a registry sandbox for reads under
  `HKLM\SOFTWARE`. Confirmed by: running the probe once on the packaged build (body step 13's
  operator run covers it). *If wrong*, the HKCU location alone still answers the per-user
  install case, and the probe degrades to "unknown" — which is a **warning** in Phase 2 and so
  blocks nothing.
- **A-04-09-2 — `PackageManager.FindPackageForUser(string.Empty, Package.Current.Id.FullName)`
  returns the running package for the current user in a packaged, non-elevated process.**
  This is the documented workaround plan 04 § 7 and `appinstaller-template.md:157-158` both
  name. Confirmed by: the operator run in body step 13. *If wrong*, the probe returns nothing
  and the orchestrator must treat it exactly as `Unknown` — log and continue — never as a
  reason to block.
- **A-04-09-3 — the compatibility response carries a TTL the client may honour, but the
  24-hour ceiling is absolute.** `endpoint-map.md:34` lists a TTL among the returned values and
  plan 04 § 3 decision 5 names `validForSeconds`. Confirmed by: [[GWY-023]]'s contract test.
  *If wrong or absent*, the client uses 24 hours. What must **not** happen either way: a TTL
  longer than 24 hours extending the fail-closed window — the ceiling is the plan's, not the
  server's.
- **A-04-09-4 — `Package.Current.Id.Version` is the value to send as
  `X-Pegasus-Client-Version`.** It is the package version plan 09 § 3 defines as
  `1.<minor>.<build>.0`. Confirmed by: the operator run's `Get-AppxPackage … | Select-Object
  Version` matching the header in the gateway log. *If wrong*, the gate compares the wrong
  number and either blocks a current client or admits an obsolete one — which is why the
  operator run captures both values.

## Execution placement

The six-question cloud-justification test from
`docs/desktop/00-governance-and-workflow/README.md` § 3 (`:169-176`), answered. The
responsibility being placed is **deciding, at each launch, whether this workstation may do
work — and refusing when it cannot find out**.

| Question | Answer | Evidence |
| --- | --- | --- |
| Shared authority — must several users see and update the same state? | **No** | The decision is per launch, per workstation. The *input* to it — the minimum client version — is shared and is a database-backed Administrator setting with audit (plan 04 § 3 decision 5), owned by [[GWY-023]] (plan handle `DSK-04-06`). This ticket reads it and caches it locally; it never writes it. |
| Unattended execution — must it run with every desktop closed? | **No** | It runs at launch, with a person waiting. Nothing here runs unattended: App Installer's own `AutomaticBackgroundTask` is the OS's, configured in the `.appinstaller` by [[REL-003]] (plan handle `DSK-09-03`), not code this ticket owns. |
| Protected credentials — long-lived secret that must not sit on workstations? | **No** | The compatibility endpoint is anonymous (`endpoint-map.md:34`). The only credential involved is the refresh handle the session-restore step reads through [[FND-031]]'s DPAPI store — short-lived (2 h idle / 8 h absolute) and already placed by [[FND-043]] (plan handle `DSK-04-07`). This ticket adds no new secret. |
| Public callback — must an external service call a stable public endpoint? | **No** | The desktop calls out to the gateway and reads a UNC feed over SMB (D-003). Nothing calls in, and `ms-appinstaller:` — the one protocol that would need a registered handler — is disabled by default and explicitly not used. |
| Central enforcement — revocation, permissions, audit, invariant independent of the client? | **Yes — and it lands on the existing `Pegasus.Web` gateway under L-01, plus the in-house UNC feed host under D-003. Neither is a new Azure resource.** | Proposal § 9.3 requires the block to hold no matter what the client does, which is exactly why the local cache is capped and has **no bypass**. The authority is the gateway's minimum-version setting ([[GWY-023]]); the package layer's authority is the `.appinstaller` on the in-house share ([[REL-008]], plan handle `DSK-09-10`). This ticket's contribution to central enforcement is to **obey it and to refuse when it cannot reach it** — the client half of a two-layer control. |
| Measured operational advantage — measured evidence central is materially better? | **No** | The opposite: the check must happen on the workstation because that is where the obsolete client is. A server-side check cannot stop a client that never calls, which is the failure the 24-hour fail-closed cache exists to close. |

One "yes", naming the **existing gateway** (L-01) and the **in-house UNC host** (D-003). No
Azure write arises: plan 04 § 3 decision 5 puts the minimum version in the database precisely
so raising it is an administrative action rather than a Container App app-setting change.

## Implications

1. **Two probes are wrappers around one documented call each, and both are behind interfaces.**
   `IPackageUpdateProbe` wraps `FindPackageForUser` + `CheckUpdateAvailabilityAsync`;
   `IRuntimePresenceProbe` wraps the `pv` registry read. Neither may touch WinUI types, or the
   state machine stops being head-less testable.
2. **The WebView2 mechanism is decided, not deferred.** Approach 1 (registry `pv`) is the only
   one available while ADR-0108 does not exist, and the documentation is explicit — so the plan
   records the key, the value name and the "> 0.0.0.0" rule, and no `open-questions` document
   is created. The body's conditional did not trigger.
3. **`Unknown` and `Error` are not failures.** They mean "not installed from an
   `.appinstaller`", which is the normal state of a development side-load. Treating them as a
   block would make every developer machine unusable; treating them as `NoUpdates` is correct
   and must be commented as deliberate.
4. **The two cache tests are the ticket's spine**: unreachable with a 23-hour-old cache
   proceeds; unreachable with a 25-hour-old cache blocks. They are cheap, they need only a
   `TimeProvider`, and they are the only thing standing between "fail closed" and "fails closed
   eventually, we think".
5. **No bypass may exist anywhere** — not a configuration key, not an environment variable, not
   a debug-only branch. Plan 04 § 3 decision 6 says so and the acceptance criteria make it
   checkable by `grep`.
6. **The head-less tests cannot prove the `Required` path.** A side-loaded MSIX returns
   `Unknown`. Only the operator's local-feed run (body step 13) can, and it depends on
   [[FND-048]] (plan handle `DSK-04-12`) having built the local Test/UAT feed.
7. **State the tier-7 gap rather than filling it with a second harness.** If [[TEST-006]] and
   [[FND-044]] have not landed, the UI check is deferred and recorded as such.

## Open questions

None. The one item the ticket body flagged as *possibly* open — the WebView2 detection
mechanism — is settled above from official documentation fetched 2026-08-24, and the body's
own condition ("raise it as an open question **if the documentation is ambiguous**") does not
apply because the documentation gives the exact key, value name and threshold. Every other
undecided item is owned by a **named sibling ticket** — the compatibility endpoint by
[[GWY-023]], the session client by [[FND-043]], the blocked screens by [[FND-044]], the local
feed by [[FND-048]], the UI harness by [[TEST-006]], runbook R3 by [[REL-010]], ADR-0108 by
[[FEAT-038]] — which makes each a scope boundary recorded in the plan's *Risks / open
questions* section. No `open-questions` document is created.
