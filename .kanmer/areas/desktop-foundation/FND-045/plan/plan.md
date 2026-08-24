# Plan — FND-045: Startup orchestrator — update check, compatibility gate with fail-closed cache, session restore

**Diff estimate: ~10 files, ~700 lines.** Derived from the files document file by file:
`Startup/StartupOrchestrator.cs` ~200 (the ordered state machine, the 24-hour rule and the
per-step diagnostics line); `Startup/StartupState.cs` ~25 (eight values);
`Startup/IPackageUpdateProbe.cs` + implementation ~90 (`FindPackageForUser`,
`CheckUpdateAvailabilityAsync`, the five-result mapping and
`RequestAddPackageByAppInstallerFileAsync`); `Startup/ICompatibilityClient.cs` +
implementation ~110 (the anonymous GET, the header, the response and the cache write);
`Startup/IRuntimePresenceProbe.cs` + implementation ~60 (two registry reads and the
`> 0.0.0.0` comparison); `src/Pegasus.Desktop/App.xaml.cs` +~25 (the call site and the
state-to-screen binding); and ~190 lines of tests across
`tests/Pegasus.Desktop.ViewModelTests/Startup/` — six named cases at roughly 30 lines each.
The two documentation edits add ~4 lines **only if** their gating tickets have landed.
`docs/engineering.md:201` § Plan sizing requires the estimate first.

## Approach

**Put the whole sequence in one head-less state machine with every collaborator behind an
interface and the clock behind `TimeProvider`, so the two cases that actually matter — a
23-hour-old cache proceeding and a 25-hour-old cache blocking — are ordinary unit tests.** The
alternative rejected is **writing the steps inline in `App.xaml.cs`**, which is where they
naturally accrete: the ticket's own *Why* says it — the steps "end up scattered across
`App.xaml.cs` and untestable", and the fail-closed rule is then only as good as somebody's
memory. The second alternative rejected is **detecting WebView2 through
`CoreWebView2Environment.GetAvailableBrowserVersionString`**, which is the documented API
approach and would be the nicer code: it needs the `Microsoft.Web.WebView2` package, and
[[FND-037]]'s (plan handle `DSK-02-12`) `ForbiddenDesktopDependencyPrefixes` fails the build on
that reference while ADR-0108 does not exist (`ls docs/adr/010*` → nothing, 2026-08-24). The
third alternative, **treating `Unknown`/`Error` from the update probe as a blocking condition**,
is rejected because a side-loaded development MSIX always returns `Unknown` — it would make
every developer machine unusable while proving nothing.

The property this ticket exists to guarantee is **refusal**: proposal § 9.3 requires that
"prolonged inability to check compatibility should eventually prevent work rather than allow
indefinite offline use". App Installer cannot deliver that — it fails **open** when the feed is
unreachable (`appinstaller-template.md:148-150`) — so the gateway gate plus this local 24-hour
ceiling is the only layer that can. That is why there is no bypass switch, and why the
acceptance criteria make its absence checkable by `grep`.

## Governing docs

The ticket's `refs` list is **not** empty — it carries
`docs/frd/frd-12-operator-experience.md` — and its frontmatter also carries `docs_todo: true`,
so both halves of this section apply.

**Meets** — for the one entry in `refs`:

| FRD-12 requirement | Where it says so | Met by |
| --- | --- | --- |
| "loading, empty, current, stale, unavailable, partial, failed, validation, conflict, and access-denied states" | `docs/frd/frd-12-operator-experience.md:22-23` | Steps 3–9. The startup surface owes four of them and each is a distinct state: loading (`CheckingForUpdate`, `CheckingCompatibility`, `RestoringSession`), **stale** (the cached compatibility response inside its 24-hour window), unavailable (`Blocked` when the window has expired) and access-denied (`Blocked` with the disabled reason). No state is a shared "something went wrong". |
| "clear counts that link to their exact filtered work and do not render stale zero placeholders" | `:13-14` | Not applicable and stated so rather than skipped: the startup surface has no counts. The related obligation it *does* inherit is the ban on presenting stale data as current — met by step 7, which distinguishes a cached-and-valid response from an expired one and blocks on the latter. |
| "exact state labels mapped to Core decisions" | `:21` | Steps 5–9. Each state maps to one platform or gateway result: `Required` from `CheckUpdateAvailabilityAsync`, `urn:pegasus:problem:client-unsupported` from the gate, `RefreshRevoked` / `AccountDisabled` from [[FND-043]]'s `SessionFailure`. No label is invented locally. |
| "keyboard, pointer, screen-reader, 200% zoom, forced-colour, and reduced-motion support" | `:24-25` | Inherited by the screens this orchestrator routes to, which are [[FND-044]]'s (plan handle `DSK-04-08`) and carry that obligation in their own ticket. The tier-7 verification here drives them; it does not re-specify them. |

**New documents this ticket is written to**, because `docs_todo: true`:

> **New FRD** — FRD-13 "Desktop operator experience" (the startup sequence and the
> blocked/update-required states), authored by [[FND-008]] (plan handle `DSK-00-08`).
> `ls docs/frd/` returns FRD-01…FRD-12 only (2026-08-24), so this ticket **records the
> dependency and creates nothing**.
> **New ADR** — ADR-0105 (MSIX/App Installer distribution and the minimum-version gate — the
> decision this orchestrator implements the client half of), **three claimants**: [[REL-001]]
> (plan handle `DSK-09-01`), [[FND-005]] (`DSK-00-05`) and [[FND-042]] (`DSK-04-01`) — see
> [[REL-001]]'s plan for the ownership reconciliation.
> **New ADR** — ADR-0104 (online-required, bounded local cache — the decision that bounds this
> ticket's compatibility cache), authored by [[FND-026]] (plan handle `DSK-02-01`);
> [[FND-005]] also claims it — see [[FND-026]]'s plan for the ownership reconciliation.
> **New ADR** — ADR-0108 (isolated non-UI WebView2 rendering), authored by [[FEAT-038]] (plan
> handle `DSK-07-12`); [[FND-007]] (plan handle `DSK-00-07`) also claims it — see
> [[FEAT-038]]'s plan for the ownership reconciliation. Named here for the opposite reason: it
> does **not** exist, which is why step 8 uses the registry rather than the WebView2 SDK.
> This plan is written to the decisions as recorded in
> `docs/desktop/04-auth-session-update-and-startup/README.md` § 3 decisions 6–7 and
> `docs/desktop/README.md` § Locked decisions (L-03, D-003); if any lands differently this plan
> is revised before implementation.

The programme-level authorities that also bind:

| Authority | Requirement | Met by |
| --- | --- | --- |
| Proposal § 9.2 Startup sequence | The ordered sequence itself | Steps 3–11 |
| Proposal § 9.3 Operational controls | "Prolonged inability to check compatibility should eventually prevent work rather than allow indefinite offline use" | Step 7 |
| Proposal § 11.3 Connectivity handling | Unreachable is a state, not an error dialog | Steps 6–7 |
| Plan 04 § 3 decision 6 | 24-hour fail-closed cache, **no bypass**, never extended "for convenience" | Step 7, and the `grep` in Verification |
| Plan 04 § 3 decision 7 | The ordered orchestrator, one class, one state machine, testable without the dispatcher; every step has a user-visible state and a log line with the correlation id | Steps 3 and 10 |
| Plan 04 § 3 decision 5 | The minimum version is a **database-backed** Administrator setting, so raising it is an administrative action and **not an Azure write** | Step 6 — this ticket reads it and never writes it |
| Plan 04 § 7 traps | `Package.Current` access-denied; side-loaded MSIX returns `Unknown`; App Installer fails open; no WebView2 package reference before ADR-0108 | Steps 4, 5, 7, 8 |
| Plan 06 `screen-specs.md:99-106` | The update-required and blocked screens and their three AutomationIds | Step 11's state-to-screen binding |
| Plan 09 `appinstaller-template.md:142-143` | `ms-appinstaller:` is disabled by default | Step 5 uses `RequestAddPackageByAppInstallerFileAsync` |
| Plan 09 `appinstaller-template.md:156-160` | Call `CheckUpdateAvailabilityAsync` on the package from `FindPackageForUser` | Step 4 |
| **L-03** | WebView2 only through the isolated non-UI path ADR-0108 authorises — hence a presence check now, blocking later | Step 8 |
| **D-003** | The feed is a UNC share, so the `.appinstaller` path is `\\<host>\<share>\<channel>\Pegasus.appinstaller` and a check needs the office network or VPN | Step 5 |
| Plan 02 § 3 decision 8 | The single-instance redirect happens before any window is created | Step 11's call site |
| `docs/engineering.md:76` tiers 2 and 7 | Positive, contradictory, ambiguous and failure cases; plus real-UI evidence | Steps 12–13 |
| `docs/engineering.md:106-111` | No `Common`/`Helpers`/`Utilities`/`Services` folder | Step 3's `Startup/` folder |
| `AGENTS.md` § Repository task workflow steps 4–5 | Simplification pass; review by an agent that did not implement | Step 14; Routing |

## Routing

Copied from the ticket body's `## Routing` block, which
`docs/desktop/00-governance-and-workflow/README.md` § Ticket template makes mandatory in the
plan document.

- **Subagent**: `winui-dev` — `.codex/agents/winui-dev.toml`.
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `winui-dev-workflow`
  (`.codex/skills/winui-dev-workflow/SKILL.md`, `microsoft/win-dev-skills` v0.5.0 `f1028dd5`)
  → `winui-design` (`.codex/skills/winui-design/SKILL.md`) for the blocked-screen layout. All
  three vendored and verified present 2026-08-24.
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`,
  `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search`,
  `microsoft_docs_fetch`) for `PackageManager.FindPackageForUser`,
  `Package.CheckUpdateAvailabilityAsync` and
  `PackageManager.RequestAddPackageByAppInstallerFileAsync`.
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` →
  `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout`. Call
  `get_doc_gates FND-045` before every move; a move crosses at most one gated boundary.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement
  (`AGENTS.md` § Repository task workflow step 5).

## Steps

These refine the body's fourteen implementation steps in the same order, with the same
ownership and the same file paths.

1. **Orient and take.** Read `docs/desktop/04-auth-session-update-and-startup/README.md` § 3
   decisions 6–7 and § 7 in full, and
   `docs/desktop/09-release-update-and-distribution/appinstaller-template.md:140-164`
   § *Known behaviours*. Confirm the prerequisites: `ls src/Pegasus.Desktop.Infrastructure`
   ([[FND-031]], plan handle `DSK-02-06`, for the bounded cache and the diagnostics writer) and
   `ls src/Pegasus.Desktop.Infrastructure/Session` ([[FND-043]], `DSK-04-07`, for
   `ISessionClient`). Call `get_doc_gates FND-045`, then `take_ticket FND-045`, and branch
   `task/desktop-startup-orchestrator` from `origin/dev`. Load `pegasus-desktop`, then
   `winui-dev-workflow`.
2. **Confirm the platform contract before coding, and record the fetch date.**
   `microsoft_docs_search` for `Package.CheckUpdateAvailabilityAsync` — the five results
   `NoUpdates | Available | Required | Unknown | Error` and the documented "Access denied" when
   called on `Package.Current` — and for `PackageManager.RequestAddPackageByAppInstallerFileAsync`.
   The research document already carries the 2026-08-24 fetch for the WebView2 half; add these
   two beside it. This is not ceremony: the whole probe is three calls, and every one of them
   has a documented failure mode.
3. **Add `StartupOrchestrator` under `src/Pegasus.Desktop.Infrastructure/Startup/`** — a
   capability folder, never `Common`/`Helpers`/`Utilities`/`Services`
   (`docs/engineering.md:106-111`) — as a plain state machine with **no WinUI types and no
   `DispatcherQueue`**. One enum of eight states: `CheckingForUpdate`, `UpdateRequired`,
   `CheckingCompatibility`, `Blocked`, `RuntimeWarning`, `RestoringSession`, `SignInRequired`,
   `Ready`. Inject `IPackageUpdateProbe`, `ICompatibilityClient`, `IRuntimePresenceProbe`,
   `ISessionClient`, `TimeProvider` and the diagnostics writer — **every one behind an
   interface**, because a single concrete `PackageManager` reference makes the whole class
   untestable.
4. **Implement the package update probe exactly as the trap requires.**
   `new PackageManager().FindPackageForUser(string.Empty, Package.Current.Id.FullName)`, then
   `CheckUpdateAvailabilityAsync()` **on the returned package** — never on `Package.Current`,
   which throws Access denied (`appinstaller-template.md:157-159`). Treat `Unknown` and `Error`
   as "not installed from an `.appinstaller`": log and continue, **do not block**, and comment
   that this is deliberate — a side-loaded development MSIX always lands there, and blocking
   would make every developer machine unusable.
5. **Handle the five results.** `Required` → state `UpdateRequired`, and **no further work**;
   `Available` → log and continue to the compatibility gate; `NoUpdates` / `Unknown` / `Error`
   → continue. The "Update now" command on the update-required screen calls
   `PackageManager.RequestAddPackageByAppInstallerFileAsync` against the channel's
   `.appinstaller` path from the embedded channel configuration — under **D-003** a UNC path
   `\\<host>\<share>\<channel>\Pegasus.appinstaller` (`appinstaller-template.md:22`), so the
   call needs the office network or VPN and its failure must be a state, not an exception
   dialog. **Never use `ms-appinstaller:`** — disabled by default since December 2023
   (`:142-143`).
6. **Implement the compatibility gate.** `GET /api/v1/client-compatibility` — anonymous
   (`endpoint-map.md:34`) — sending `X-Pegasus-Client-Version` from `Package.Current.Id.Version`.
   On success, persist the response **and its retrieval timestamp** through [[FND-031]]'s
   bounded cache; the timestamp is what step 7 reads, so a cache write that drops it makes the
   fail-closed rule unimplementable. On a `urn:pegasus:problem:client-unsupported` response, go
   to `UpdateRequired` carrying the returned `minimumVersion` so the screen can show it. Do
   **not** wire this to `/diagnostics/version` (`src/Pegasus.Web/Program.cs:954`): it returns
   `{version, sourceSha}` only and carries no minimum, channel or TTL.
7. **Implement the 24-hour fail-closed rule with `TimeProvider`.** If the endpoint is
   unreachable, use the cached response **only while it is younger than 24 hours**; at 24 hours
   or older, go to `Blocked` and perform no work. If the response carries a shorter TTL, honour
   it — but a longer TTL never extends the ceiling. There must be **no** bypass switch,
   environment variable, configuration key or debug-only branch: plan 04 § 3 decision 6 says
   the cache must not be extended "for convenience", and § 7 repeats it. This is the step
   proposal § 9.3 exists for, and the one a reviewer should read first.
8. **Implement the WebView2 runtime presence probe without a WebView2 package reference.** Read
   the `pv (REG_SZ)` value at
   `HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}`
   (per-machine, 64-bit — and the desktop is x64-only) and at
   `HKEY_CURRENT_USER\Software\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}`
   (per-user). Present means **at least one exists with a version greater than `0.0.0.0`**;
   absent, `null`, an empty string or `0.0.0.0` means not installed. Source: Microsoft Learn,
   *Distribute your app and the WebView2 Runtime* § "The Evergreen Runtime distribution mode" →
   "Detect if a WebView2 Runtime is already installed", fetched **2026-08-24**
   (<https://learn.microsoft.com/microsoft-edge/webview2/concepts/distribution>). The
   documented API alternative (`CoreWebView2Environment.GetAvailableBrowserVersionString`)
   needs the `Microsoft.Web.WebView2` package, which [[FND-037]]'s architecture fact fails the
   build on until ADR-0108 lands — so the registry is not a workaround, it is the only
   available mechanism. **A missing runtime produces `RuntimeWarning` and a log line only; it
   never blocks startup in Phase 2** (L-03: blocking arrives with Phase 7).
9. **Implement session restore.** If [[FND-031]]'s DPAPI store holds a refresh handle, call
   `ISessionClient.RefreshAsync`: success → `Ready`; `RefreshRevoked` → clear the store and go
   to `SignInRequired`; `AccountDisabled` → `Blocked` with the disabled reason. No handle at
   all → `SignInRequired` without an error state — a first launch is not a failure.
10. **Emit one structured diagnostics line per step** through [[FND-031]]'s writer — step name,
    outcome, elapsed milliseconds, correlation id — and **reuse a single correlation id for the
    whole startup sequence**, so [[FND-036]]'s (plan handle `DSK-02-11`) support bundle shows
    the ordered steps as one story rather than five unrelated lines.
11. **Call the orchestrator from `src/Pegasus.Desktop/App.xaml.cs`** **after** [[FND-035]]'s
    (plan handle `DSK-02-10`) single-instance redirection and **before** the main window is
    shown — plan 02 § 3 decision 8 requires the redirect to happen before any window is
    created, so an orchestrator that ran first would draw a window in a process about to
    redirect and exit. Bind each state to its screen: `UpdateRequired` and `Blocked` to
    [[FND-044]]'s (plan handle `DSK-04-08`) full-window rail-less screens, `SignInRequired` to
    the sign-in page, `Ready` to the shell.
12. **Write the head-less tests** in `tests/Pegasus.Desktop.ViewModelTests/Startup/` with fakes
    for the package probe, the compatibility client and the runtime probe, and [[FND-038]]'s
    (plan handle `DSK-02-13`) shared `FixedTimeProvider`: `Required` blocks; unreachable with a
    **23**-hour-old cache proceeds; unreachable with a **25**-hour-old cache blocks;
    `client-unsupported` blocks **carrying the minimum version**; missing WebView2 warns but
    proceeds; revoked refresh routes to sign-in. Run
    `dotnet test tests/Pegasus.Desktop.ViewModelTests`. The 23/25-hour pair is the ticket's
    spine — without both, "fails closed" is an assertion rather than a fact.
13. **Operator step — the real `Required` path.** On a Windows 11 workstation with the
    development certificate trusted ([[FND-039]], plan handle `DSK-02-14`), install the package
    from the local Test/UAT `.appinstaller` feed built by [[FND-048]] (plan handle
    `DSK-04-12`), publish a higher version to that feed, relaunch, and confirm the real
    `Required` path end to end. The operator hands back
    `Get-AppxPackage CollisionEngineers.Pegasus | Select-Object Version` **before and after**, a
    screenshot of the update-required screen, and the rolling-log excerpt showing the ordered
    startup lines with one correlation id. **A side-loaded MSIX not installed from an
    `.appinstaller` returns `Unknown` and cannot prove this path** — if [[FND-048]] has not
    landed, record the step as unrun rather than substituting a side-load.
14. **Documentation, simplification pass, PR.** Do **not** create
    `docs/frd/frd-13-desktop-operator-experience.md` — `ls docs/frd/` returns FRD-01…FRD-12
    (2026-08-24) and FRD-13 is [[FND-008]]'s (plan handle `DSK-00-08`). Add the
    `docs/runbook.md` pointer to mandatory-update runbook R3
    (`docs/desktop/09-release-update-and-distribution/runbooks.md:118`) **only once**
    [[REL-010]] (plan handle `DSK-09-12`) has proven that runbook; otherwise record the
    dependency here and write nothing. Run the simplification pass over this branch's own diff,
    record it under a dated `## Simplification pass` heading below, and open the PR into `dev`.

## Verification

Evidence tiers from the body: **Tier 2 — Core/domain** (the state machine, proved head-less
with fakes) **and Tier 7 — Browser/accessibility** (the blocked and update-required screens
driven through the real UI with screenshots). Both are owed and neither substitutes for the
other — `docs/engineering.md:76` tier 7 says automated results do not replace the real-session
review, and the head-less tests cannot exercise `Required` at all because a side-loaded MSIX
returns `Unknown`. Proof types: `test-output`, `command-log` and `visual`.

| Command / observation | Expected evidence |
| --- | --- |
| `dotnet test tests/Pegasus.Desktop.ViewModelTests` | `Passed!`, zero skipped, including `Required`-blocks, 23-hour-proceeds, 25-hour-blocks, `client-unsupported`-with-minimum, missing-runtime-warns and revoked-refresh-routes-to-sign-in |
| `dotnet test tests/Pegasus.ArchitectureTests` | `Passed!` — [[FND-037]]'s no-WebView fact and forbidden-prefix fact both stay green |
| `grep -rn 'Microsoft.Web.WebView2' src/Pegasus.Desktop.Infrastructure src/Pegasus.Desktop` | no match — the probe is a registry read |
| `grep -rni 'bypass\|skipCompat\|ignoreVersion\|DEBUG_SKIP' src/Pegasus.Desktop.Infrastructure/Startup` | no match — there is no way to extend or skip the 24-hour rule |
| `grep -rn 'ms-appinstaller' src/Pegasus.Desktop.Infrastructure src/Pegasus.Desktop` | no match — the update is launched with `RequestAddPackageByAppInstallerFileAsync` |
| `grep -rn 'Package.Current.CheckUpdateAvailabilityAsync' src/` | no match — the call goes through `FindPackageForUser` |
| `pwsh ./tests/Pegasus.Desktop.UITests/ui-tests.ps1 -AppPid <pid>` with the compatibility endpoint returning a minimum above the client version | the update-required screen asserted via `winapp ui wait-for Update.Required.Now`, and a screenshot written. The switch is `-AppPid`, the signature `param([Parameter(Mandatory)][int]$AppPid)` pinned by [[TEST-006]] (plan handle `DSK-08-06`); `$Pid` is read-only in PowerShell, which is why the harness never uses that name. **If neither [[TEST-006]] nor [[FND-044]] has landed, record this check as deferred to [[TEST-006]] rather than writing a second harness.** |
| Operator run: `Get-AppxPackage CollisionEngineers.Pegasus \| Select-Object Version` before and after the forced update | the version increases and the app reaches the shell **only after** the update |
| Operator run: the rolling-log excerpt | the ordered startup lines, each with step name, outcome, elapsed ms and **one shared correlation id** |
| `dotnet build Pegasus.slnx -c Release` on Windows | `Build succeeded` with `0 Warning(s)` |
| Observations stated rather than inferred | whether [[FND-048]] had landed and so whether step 13 ran at all; whether the tier-7 UI check ran or was deferred; whether the registry probe read HKLM, HKCU or neither on the test machine |

## Risks / open questions

- **Risk — a guard that never fires.** If the cache write drops the retrieval timestamp, the
  24-hour rule silently becomes "never blocks". Mitigation: step 6 names the timestamp as part
  of the cache contract and step 12's 25-hour test fails immediately if it is missing.
- **Risk — `Package.Current` access-denied at runtime.** The documented trap
  (`appinstaller-template.md:157-159`). Mitigation: step 4 uses `FindPackageForUser`, and the
  `grep` in Verification proves the wrong call is not present anywhere under `src/`.
- **Risk — `Unknown` treated as a block.** Would make every developer machine unusable while
  proving nothing. Mitigation: step 4 maps `Unknown` and `Error` to continue, with a comment
  saying why.
- **Risk — a bypass added later "for testing".** Mitigation: step 7 forbids it, and the
  Verification `grep` is a check a reviewer can run in one line rather than a claim they must
  believe.
- **Risk — a `Microsoft.Web.WebView2` reference sneaking in with the nicer detection API.**
  Mitigation: step 8 uses the registry and cites the Learn page; [[FND-037]]'s architecture
  fact fails the build if not.
- **Risk — the orchestrator running before the single-instance redirect.** Would draw a window
  in a process about to exit. Mitigation: step 11 fixes the call site relative to [[FND-035]],
  and plan 02 § 3 decision 8 is the authority.
- **Risk — step 13 cannot run.** The real `Required` path needs a package installed from an
  `.appinstaller`, which needs [[FND-048]]'s (plan handle `DSK-04-12`) local feed. Mitigation:
  record the step as unrun rather than substituting a side-load, which returns `Unknown` and
  proves nothing.
- **Settled, not open — the WebView2 detection mechanism.** The ticket's Guardrails say to
  decide it from Microsoft Learn evidence and to raise an open question **only if the
  documentation is ambiguous**. It is not: the Learn page fetched 2026-08-24 gives the exact key
  `{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}`, the value name `pv (REG_SZ)`, both hive locations
  and the "greater than `0.0.0.0`" threshold. The choice and its source are recorded in step 8
  and in the research document, and **no `open-questions` document is created**.
- **Scope boundary, not an open question — the compatibility endpoint and the minimum-version
  setting.** [[GWY-023]] (plan handle `DSK-04-06`). This ticket calls and caches; it never
  writes the setting, which is why no Azure write arises.
- **Scope boundary, not an open question — the screens.** [[FND-044]] (plan handle `DSK-04-08`)
  owns the update-required and blocked XAML and their AutomationIds.
- **Scope boundary, not an open question — the UI harness.** [[TEST-006]] (plan handle
  `DSK-08-06`) owns `ui-tests.ps1`; [[FND-044]] contributes its cases; this ticket only invokes
  it.
- **Scope boundary, not an open question — runbook R3 and FRD-13.** [[REL-010]] (plan handle
  `DSK-09-12`) and [[FND-008]] (`DSK-00-08`).
- **Open questions**: none. No `open-questions` document is created.

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over this
branch's own diff before the PR, recorded here under a dated heading. This branch adds C# and
tests, so `n/a — docs-only` does not apply._
