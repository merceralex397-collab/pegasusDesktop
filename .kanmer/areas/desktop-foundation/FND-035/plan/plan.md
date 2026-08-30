# Plan — FND-035: Single instance per Windows user — `AppInstance.FindOrRegisterForKey` and activation redirection

**Diff estimate: ~6 files, ~230 lines.**

`docs/engineering.md` § Plan sizing (`:201`) requires the estimate first. Derived from the `files`
document, file by file, measured 2026-08-24:
`src/Pegasus.Desktop/Program.cs` ~75 (the entry point, the key test, the non-blocking redirect,
`Application.Start`);
`src/Pegasus.Desktop/Services/IActivationRouter.cs` ~15 and its implementation ~70;
`src/Pegasus.Desktop/App.xaml.cs` ~+25 (the `AppInstance.Activated` subscription and forwarding);
`src/Pegasus.Desktop/Pegasus.Desktop.csproj` +1 (`DISABLE_XAML_GENERATED_MAIN`);
`src/Pegasus.Desktop/Hosting/PegasusHost.cs` +3 (one registration);
`docs/current-architecture.md` ~+3 at § Failure and recovery boundaries (`:565`).
The routing tests land in `tests/Pegasus.Desktop.ViewModelTests` (~70 lines) and are counted against
that project. Nothing under `src/Pegasus.Core`, `src/Pegasus.Infrastructure`, `src/Pegasus.Web` or
`src/Pegasus.Worker` is touched.

## Approach

Take the **explicit entry point** branch of the ticket body's step 3, and write the redirect with the
documented **non-blocking** pattern rather than an `await`.

The first half is not a choice this plan is making — the research already settled it from official
documentation. Windows App SDK 1.0 release notes § 3.3: a WinUI app that wants to "detect other
instances and redirect an activation … must do so as early as possible, and **before initializing any
windows** … To enable this, the app **must define `DISABLE_XAML_GENERATED_MAIN`, and write a custom
`Main` (C#)**". The body's `App.OnLaunched` alternative is therefore not available for this purpose,
and step 2 re-confirms rather than decides. The rejected alternative — redirecting from `OnLaunched` —
is rejected because by the time `OnLaunched` runs, XAML has already initialised, which is exactly what
the documentation forbids.

The second half is the part the ticket body does not name, and it is the failure this ticket is most
likely to hit. Release notes § 3.4: "**`RedirectActivationToAsync` is an async call, and you should not
wait on an async call if your app is running in an STA.**" The body specifies
`[STAThread] static void Main(string[] args)` — a **non-async** signature — so the documented
non-blocking pattern applies: call `RedirectActivationToAsync` on another thread, set an event when it
completes, and wait on that event with non-blocking APIs
(*applifecycle-instancing* § Redirection without blocking). Blocking the STA here does **not** fail
loudly; it hangs, and a hung second process looks exactly like a broken build — which the "never run
the `.exe` directly" trap will then be blamed for. The documented alternative, `async Task Main`, is
permitted by the same release notes for C# WinUI apps; whichever is used must be recorded with its
reason.

**One honest framing carried from the research, because it changes what the proof may claim.** The
ticket's § Why says two windows editing the same case is "the silent-overwrite failure the concurrency
design exists to prevent". The actual prevention already exists and is server-side:
`src/Pegasus.Core/Workflow/CaseWorkflowContracts.cs:182-188` —
`CaseMutationRequest(Guid CaseId, long ExpectedVersion, ActionActor Actor, string OperationKey, string Reason, string EditLeaseToken)`
— with `CaseEditAuthority` validating the lease and three refusals in
`src/Pegasus.Web/Mcp/AutomationMcpErrors.cs:35-53` each returning the current version and saying
"reload and reacquire rather than retrying". Two *machines* can always edit the same case, so
single-instancing could never be the invariant. It reduces how often an operator meets the conflict on
one machine. The plan is written to that scope and the proof must not overclaim it.

## Governing docs

The ticket's `refs` array is empty and `get_doc_gates FND-035` reports `docs_todo: true`, so there is
no linked PRD/FRD/ADR to meet today.

> **New ADR** — ADR-0100 (native WinUI 3 / Windows 11 desktop client converted inside this fork) is
> what fixes the **packaged single-project MSIX with package identity**, and package identity is what
> makes `AppInstance` keys meaningful at all — remove it and the mechanism does not exist. ADR-0100 is
> authored by [[FND-026]] (plan handle `DSK-02-01`); [[FND-005]] (plan handle `DSK-00-05`) also claims
> it in the reserved block ADR-0100…ADR-0110 — see [[FND-026]]'s plan for the ownership
> reconciliation. **ADR-0103** (gateway, never direct database access from workstations) is the ADR
> that actually owns the concurrency invariant this ticket is often mistaken for enforcing; it is
> claimed by [[FND-005]].
> This plan is written to the decisions as recorded in
> `docs/desktop/00-governance-and-workflow/README.md` § 3 (ADR set table) and
> `docs/desktop/02-architecture-and-foundation/README.md` § 3 decision 8; if either lands differently
> this plan is revised before implementation.

Because `refs` is empty, the authorities that actually bind today are these:

| Authority | Requirement | Met by |
| --- | --- | --- |
| Proposal § 7.3 Single-instance behaviour | A second launch activates the existing window; deep links and file activations are redirected to the active process; unsaved work is never duplicated across processes | Steps 4–7 |
| Plan 02 § 3 decision 8 | `AppInstance.FindOrRegisterForKey` + `RedirectActivationToAsync` **before any window is created**; redirected activations carry deep-link/file arguments; **no multi-window in Phase 1** | Steps 3–6, and step 11's explicit check |
| Plan 02 § 4 exit-gate table | "Single instance — second launch activates the first window (UI test)" | Step 10; [[FND-041]] (plan handle `DSK-02-16`) consumes the evidence |
| Windows App SDK 1.0 release notes § 3.3 (fetched 2026-08-24) | Redirect before initialising any windows; requires `DISABLE_XAML_GENERATED_MAIN` and a custom `Main` | Steps 2, 3 |
| Windows App SDK 1.0 release notes § 3.4 + *applifecycle-instancing* § Redirection without blocking | Do not wait on the async redirect from an STA; use the other-thread-plus-event pattern or `async Task Main` | Step 5 |
| *migrate-to-windows-app-sdk/guides/applifecycle* § Single-instanced apps | The sample code requires targeting **x64** | Already satisfied by [[FND-030]] (plan handle `DSK-02-05`)'s `<Platforms>x64</Platforms>`; corroborated, not re-decided |
| Windows App SDK 1.0 release notes § 2.2 | For packaged apps, file and protocol activation is declared in the MSIX manifest, not in code | § Out of scope — `Package.appxmanifest` is [[FND-030]]'s file |
| `.codex/skills/winui-dev-workflow/SKILL.md:98` | Never `<WindowsPackageType>None</WindowsPackageType>` | § Out of scope — it removes the package identity the API depends on |
| `.codex/skills/winui-dev-workflow/SKILL.md:76` | "App silently exits → Use `winapp run`, never run the .exe directly" | Step 10 and § Verification |
| **L-01** (locked) | The desktop talks only to the evolved `Pegasus.Web` gateway | Nothing here changes where mutation authority lives; § Approach records that explicitly |
| **L-04** (locked) | Every ticket names its subagent, skills and MCP tools | § Routing below |
| `docs/engineering.md` § Plan sizing (`:201`) | Diff estimate first, from a measured inventory | The estimate above |
| `docs/engineering.md` § Required evidence tiers (`:72`), tier 7 | Behaviour demonstrated on a real session, not asserted from code | § Verification V3 |
| **C-01** (constraint) | The repositories become private; Actions minutes stop being free | This ticket adds no CI job — [[FND-040]] (plan handle `DSK-02-15`) owns the lane |

## Routing

Copied from the ticket body's `## Routing` block, as
`docs/desktop/00-governance-and-workflow/README.md` § Ticket template requires of the plan document
specifically.

- **Subagent**: `winui-dev` — `.codex/agents/winui-dev.toml`.
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `winui-dev-workflow`
  (`.codex/skills/winui-dev-workflow/SKILL.md`) → `winui-design`
  (`.codex/skills/winui-design/SKILL.md`), win-dev-skills v0.5.0 `f1028dd5`.
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`,
  `move_item`); Microsoft Learn — `microsoft_docs_search` for `AppInstance.FindOrRegisterForKey`
  redirection semantics, `AppInstance.GetActivatedEventArgs`, `RedirectActivationToAsync`, and
  `DISABLE_XAML_GENERATED_MAIN`.
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute` →
  `kanmer-review` → `kanmer-verify` → `kanmer-closeout`. Call `get_doc_gates <id>` before every move;
  a move crosses at most one gated boundary.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md`
  § Repository task workflow step 5).

## Steps

These refine the ticket body's eleven implementation steps: same order, same ownership, same file
paths, adding the *how* the body leaves out.

1. **Orient.** Read plan 02 § 3 decision 8 and § 4's exit-gate table, and **this ticket's `research`
   document**, which already answers step 2. Confirm [[FND-030]], [[FND-032]] (plan handle
   `DSK-02-07`) and [[FND-033]] (plan handle `DSK-02-08`) have landed — the project, the host, and
   `INavigationService` respectively. Then `get_doc_gates FND-035` and `take_ticket` on branch
   `task/desktop-single-instance` from `origin/dev`.
2. **Re-confirm the mechanism, do not re-decide it.** Run `microsoft_docs_search` for
   `AppInstance.FindOrRegisterForKey` redirection semantics and for the Windows App SDK app-lifecycle
   single-instancing sample, and check the two facts the research recorded on 2026-08-24 have not
   moved: (a) the redirect must run before any window, and (b) that requires
   `DISABLE_XAML_GENERATED_MAIN` plus a custom `Main`. Record the re-confirmation date and URLs in the
   proof. The SDK moves; the answer is not expected to.
3. **Add the explicit entry point.** Put
   `<DefineConstants>$(DefineConstants);DISABLE_XAML_GENERATED_MAIN</DefineConstants>` in
   `src/Pegasus.Desktop/Pegasus.Desktop.csproj` — the exact XML from the Learn how-to — and create
   `src/Pegasus.Desktop/Program.cs` with `[STAThread] static void Main(string[] args)`. Note in the PR
   that this file now owns application startup: every future pre-window change lands here rather than
   in generated code, including [[FND-036]] (plan handle `DSK-02-11`)'s crash path.
4. **Register the key.** Call `AppInstance.GetCurrent().GetActivatedEventArgs()`, then
   `AppInstance.FindOrRegisterForKey(<key>)` with a **constant application key string**. Do not build
   it from a mutable value — no window title, no version number, no timestamp. A constant is
   sufficient because the instancing API already maintains "separate lists … for … instances of apps
   launched by different users" (*applifecycle-instancing*), which is exactly the per-user scoping
   this ticket's title claims. Note the same sentence also scopes lists **per app version**, which is
   the untested upgrade case in § Risks.
5. **Redirect without blocking the STA.** When `FindOrRegisterForKey` returns an instance whose
   `IsCurrent` is false, call `RedirectActivationToAsync(args)` **on another thread, set an event when
   it completes, and wait on that event with non-blocking APIs** — the documented pattern for a
   non-async entry point (release notes § 3.4; *applifecycle-instancing* § Redirection without
   blocking). If `async Task Main` is used instead, record that choice and its reason. Then terminate
   the process immediately: **no window, no host, no view model, and no log file beyond one redirect
   line.** Anything else is observable as a flash of a second window or a second log file, and defeats
   the point.
6. **Handle the redirect in the owning instance.** Subscribe to `AppInstance.Activated` in
   `App.xaml.cs` and forward the `AppActivationArguments` to `IActivationRouter`, registered in
   `Hosting/PegasusHost.cs`. The router parses deep-link and file arguments and asks
   `INavigationService` ([[FND-033]]) to navigate to the requested case or document; an argument it
   does not understand is **logged and ignored, never crashed on**. Write the router against
   `Microsoft.Windows.AppLifecycle.AppActivationArguments` — **not**
   `Microsoft.UI.Xaml.LaunchActivatedEventArgs`. They are different types from different namespaces
   (release notes 1.3), and the wrong one will not compile against the redirected path.
7. **Bring the window forward.** Restore it if minimised and activate it. Use `winui-design` or
   `microsoft_docs_search` for `AppWindow` activation to confirm the supported call rather than
   guessing at a Win32 interop — a hand-rolled `SetForegroundWindow` is the shape to avoid.
8. **Log every activation and redirect** with the per-launch session identifier from [[FND-032]],
   through [[FND-031]] (plan handle `DSK-02-06`)'s `IDiagnosticsWriter` so the redaction hook applies.
   The line format must be **stable**: [[FND-036]] includes this log in the diagnostics bundle and
   asserts a manifest against it, and [[FND-049]] (plan handle `DSK-04-13`) tells an operator where to
   find it. Fix the format once.
9. **Write the routing tests** in `tests/Pegasus.Desktop.ViewModelTests` ([[FND-038]], plan handle
   `DSK-02-13`): a case deep link routes to the case route with the right identifier; a file
   activation routes to the document route; an unknown argument is ignored **and logged** — assert
   both halves, because silently ignoring is a different behaviour from logging and ignoring.
   **Instancing itself cannot be unit-tested**; that is step 10. If [[FND-038]] has not landed,
   sequence it first and record the sequencing.
10. **Prove it end to end.** Install or run the packaged app, then launch it **twice** using
    `winapp run` — **never the packaged `.exe` directly**. Running the `.exe` is precisely how a
    *correct* single-instance implementation gets misdiagnosed as broken, because the second process
    exiting is the intended behaviour and the direct-launch path makes it look like a silent crash
    (`.codex/skills/winui-dev-workflow/SKILL.md:76`). Confirm exactly one window exists, that
    `Get-Process` shows a single Pegasus process, and that the second launch's argument is visible in
    the activation log. If [[TEST-006]] (plan handle `DSK-08-06`)'s `winapp ui` harness exists, add a
    `single-instance` batch to it; otherwise record a **manual** pass with a screenshot and name
    [[TEST-006]] as the automation follow-up.
11. **Confirm no multi-window capability was added** — Phase 1 is single-window only (plan 02 § 3
    decision 8), and this is an acceptance criterion, so check it rather than assume it. Add the
    one-line statement to `docs/current-architecture.md` § Failure and recovery boundaries (`:565`).
    Then run the simplification pass over this branch's own diff, record it under a dated
    `## Simplification pass` heading in this document, and open the PR into `dev`.

## Verification

Evidence tier **7 — Browser/accessibility** (`docs/engineering.md` § Required evidence tiers, `:72`),
applied to the desktop as the UI-behaviour tier, as the ticket body states: the two-launch scenario is
**demonstrated on a real session, not asserted from code**.

The `proof` document is produced from these four outputs.

- **V1.** `dotnet build ./Pegasus.slnx --configuration Release` — expected exit 0 and
  `0 Warning(s)`. The authoritative gate: it is what `.github/actions/dotnet-build/action.yml:22-27`
  runs and, unlike `BuildAndRun.ps1`, it sees the repository-root `Directory.Build.props`.
- **V2.** `dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release --filter "FullyQualifiedName~Activation"`
  — expected: the case-deep-link, file-activation and unknown-argument tests pass, with the
  unknown-argument case asserting **both** that it was ignored and that it was logged.
- **V3.** **The two-launch demonstration**, which is the heart of the proof. Launch the packaged app
  twice via `winapp run`, then capture: a screenshot showing exactly one window; `Get-Process` output
  showing a single Pegasus process; and the activation-log lines showing the second launch's argument
  arriving at the first instance with the session identifier. State whether the second launch was
  given a deep-link argument or a bare launch — they are different cases and only one of them proves
  routing.
- **V4.** The re-confirmation record from step 2: the two documentation URLs, the date they were
  re-fetched, and whether the answer changed. Also record which redirect pattern was used —
  other-thread-plus-event, or `async Task Main` — and why.

**Honesty clauses for the proof.**

- **Do not claim this prevents concurrent editing.** It reduces how often an operator meets a
  conflict on one machine. The invariant lives server-side in `CaseMutationRequest`'s
  `ExpectedVersion` and `EditLeaseToken` (`src/Pegasus.Core/Workflow/CaseWorkflowContracts.cs:182-188`)
  and is enforced by `CaseEditAuthority`; two machines can always edit the same case. Overclaiming
  here would be the dishonest answer the cloud-justification test exists to catch, restated in the
  proof.
- Say whether the evidence is a **manual** two-launch pass or a [[TEST-006]] batch, and name
  [[TEST-006]] as the follow-up if manual. Tier 7 requires a demonstration either way.
- Record that **instancing across an App Installer upgrade was not exercised** (A-FND035-3) and name
  [[FND-039]] (plan handle `DSK-02-14`) and area 08 as its owners. A two-launch test on one version
  quietly does not cover it.
- A green `BuildAndRun.ps1` is **not** the same claim as a green `dotnet build`: the script injects a
  project-level `Directory.Build.props` (`.codex/skills/winui-dev-workflow/BuildAndRun.ps1:142-172`,
  its existence test at `:152` against the project directory only) that shadows the root one and
  drops `TreatWarningsAsErrors`. V1 is authoritative.
- No CI job builds a desktop project until [[FND-040]] lands, so a green `repository-check` run says
  nothing about this ticket.

## Risks / open questions

- **Risk — A-FND035-1: blocking the STA hangs instead of failing.** `await`-ing
  `RedirectActivationToAsync` from a non-async `[STAThread] Main` blocks, and the second process hangs
  rather than exiting — a symptom indistinguishable from a broken build, and one the "never run the
  `.exe` directly" trap will be blamed for. *Mitigation*: step 5 uses the documented non-blocking
  pattern, and V3's `Get-Process` check catches a lingering second process that a screenshot alone
  would miss. *If wrong*: `async Task Main` is the documented alternative for C# WinUI apps; record
  which was used.
- **Risk — misdiagnosis by running the packaged `.exe` directly.** The second process exiting is the
  **intended** behaviour, so the direct-launch path makes a correct implementation look like a silent
  crash. *Mitigation*: `winapp run` only, stated in step 10, in § Verification and in the Guardrails.
  This is the single most likely way this ticket gets "fixed" into being broken.
- **Risk — A-FND035-3: instancing across an App Installer upgrade is untested here.**
  *applifecycle-instancing* states instance lists are scoped per app **version** as well as per user,
  so an old and a new version can each hold a key and the operator ends up with two windows — exactly
  the state this ticket exists to prevent, arriving through the one path nobody tests. *Mitigation*:
  recorded as a scope boundary owned by [[FND-039]] and area 08's packaging tests, and stated in the
  proof rather than left implied.
- **Risk — A-FND035-2: two operators sharing a workstation.** The per-user scoping is documented, not
  measured here. *Mitigation*: if two sessions on one machine are testable, test it; otherwise the
  documentation citation stands and the limitation is recorded. Do **not** "fix" it by embedding a
  user identifier in the key — that would contradict the documented behaviour and make the key
  mutable.
- **Risk — the router is written against the wrong argument type.**
  `Microsoft.UI.Xaml.LaunchActivatedEventArgs` (what `OnLaunched` receives) and
  `Microsoft.Windows.AppLifecycle.AppActivationArguments` (what `GetActivatedEventArgs` returns) are
  different types. *Mitigation*: named explicitly in step 6; the wrong one fails at compile time, so
  the cost is time, not a shipped defect.
- **Risk — the activation-log format changes after [[FND-036]] depends on it.** *Mitigation*: step 8
  fixes it once and records it; [[FND-036]] asserts a manifest against it.
- **Risk — `Program.cs` becomes a contested file.** [[FND-036]]'s unhandled-exception path also wants
  the pre-window region. *Mitigation*: this ticket keeps `Program.cs` to instancing only and leaves
  the seam clean, exactly as [[FND-032]] did for this ticket. The Guardrails already say crash
  handling must not swallow an exception and continue.
- **Sequencing, recorded not resolved — [[FND-033]] must have landed for step 6.** The router calls
  `INavigationService`, which [[FND-033]] creates. The plan's dependency arrow names only [[FND-032]].
- **Sequencing, recorded not resolved — [[FND-038]] must land before step 9.**
  `tests/Pegasus.Desktop.ViewModelTests` does not exist yet.
- **Scope boundary, not an open question — the diagnostics bundle, the update flow, deep-link target
  screens, and the manifest activation declarations.** [[FND-036]], area 04/09, area 05, and
  [[FND-030]]'s `Package.appxmanifest` respectively.
- **No `open-questions` document is opened on this ticket.** The body does not instruct one; the two
  facts its step 2 asks to be established are settled in the `research` document with URLs and a
  fetch date, and re-confirming them is a step inside this ticket. Every assumption names the command
  or the sibling ticket that settles it, and no settled operator decision (D-002, D-003, D-004, the
  Send-to-AI exclusion) is reopened.

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over this branch's own
diff before the PR, recorded here under a dated heading._

## Implementation and simplification pass — 2026-08-30

Implementation commit `afb2341783d96a3de43c4f9fc3c9cf8d69948af7` adds the explicit startup entry point, constant AppInstance key, activation routing/logging, host registration, architecture note, and three focused activation tests within the planned scope. The manifest's existing operator-confirmed identity (`CollisionEngineers.Pegasus`, publisher `CN=Collision Engineers`) was not changed. A parent review found the initial `ManualResetEventSlim.Wait()` on the STA contradicted the WinUI performance rule and could deadlock activation; commit `18493d4825d4609ba8dbfcb29960023839a98cc6` removes that machinery and uses the documented `async Task Main`/`await RedirectActivationToAsync` path.

Parent validation after the correction:

- `dotnet test tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~Activation" --logger "console;verbosity=minimal" -nr:false -p:UseSharedCompilation=false` — 3 passed, 0 failed, 0 skipped.
- `dotnet build src/Pegasus.Desktop/Pegasus.Desktop.csproj --configuration Release --no-restore -nr:false -p:UseSharedCompilation=false` — succeeded, 0 warnings, 0 errors.
- `git diff --check` — passed.

Simplification findings: removed the unnecessary event/thread/exception-dispatch machinery rather than retaining it as a defensive path; no Win32 foreground wrapper, no mutable instance-key inputs, no package-identity change, and no new abstraction beyond the ticket's activation router and the navigation seam needed by [[FND-033]]. The temporary-compatible `INavigationService` declaration is deliberately a single shared contract for FND-033 to reuse; FND-033 must not create a duplicate contract. No unrelated files were changed.

The authoritative solution-wide build and real two-launch packaged-app proof remain pending. The two-launch proof also needs FND-033's concrete navigation implementation; no claim of runtime completion is made yet. No PR or push has been made for this ticket.

## Validation update — 2026-08-30

The first solution-wide build attempt was not a source failure: the worktree had not yet generated assets for several projects under `--no-restore`, and the WinUI compiler also encountered a transient access-denied lock while another .NET host held generated output. A locked restore then completed successfully:

- `dotnet restore ./Pegasus.slnx --locked-mode` — passed; all 13 projects restored.
- `dotnet build ./Pegasus.slnx --configuration Release --no-restore -nr:false -p:UseSharedCompilation=false` — passed, 0 warnings, 0 errors.

The build was rerun after restore and completed successfully. This validates the requested solution-wide compile; it does not replace the missing packaged two-launch evidence.

## Independent review correction — 2026-08-30

The first review request used a mistyped SHA. The actual implementation-fix commit is `18493d4825d4609ba8dbfcb29960023839a98cc6`, not the nonexistent `18493d485...`. The review also found that `AppActivationArguments.Data` must be matched through the Windows activation launch interface, not the WinUI `OnLaunched` class. The implementation now uses `Windows.ApplicationModel.Activation.ILaunchActivatedEventArgs`. This is the projected interface documented for the Launch payload; using the concrete Windows class triggers the repository's WUI1001 analyzer because WinUI desktop launch handling uses the `Microsoft.UI.Xaml` type for `OnLaunched`. A null guard was also added because `GetActivatedEventArgs()` may return no activation payload on an ordinary launch.

Correction commit: `fa29f6f42dde60c7b5e3908dc3fcae60629a4d87`.

Validation after the correction:

- `dotnet test tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~Activation" --logger "console;verbosity=minimal" -nr:false -p:UseSharedCompilation=false` — 3 passed, 0 failed, 0 skipped.
- `dotnet build ./Pegasus.slnx --configuration Release --no-restore -nr:false -p:UseSharedCompilation=false` — passed, 0 warnings, 0 errors.
- `git diff --check` — passed before commit.

The reviewer's required DI boundary remains unresolved: FND-033 must provide and register the concrete navigation service; FND-035 does not duplicate it. The packaged two-launch proof and protocol/file manifest declarations remain pending their owning tickets and cannot be claimed here.


## Independent review correction — 2026-08-30

The independent reviewer confirmed the actual correction sequence and found the earlier stale SHA references were evidence defects. All occurrences of the nonexistent `18493d485f8eab5c9d1fd8c63af9b478d54e04d` have been corrected to the actual parent `18493d4825d4609ba8dbfcb29960023839a98cc6`. The final code head is `fa29f6f42dde60c7b5e3908dc3fcae60629a4d87`.

The reviewer confirmed that `AppActivationArguments.Data` is correctly matched through `Windows.ApplicationModel.Activation.ILaunchActivatedEventArgs`, the absent initial activation payload is guarded, async STA startup is non-blocking, the constant key and window activation shape are correct, and the worktree is clean. Focused activation tests passed 3/3 and the serial solution Release build passed with 0 warnings/errors.

The review remains FAIL for two legitimate delivery blockers: FND-033 must land the owned `INavigationService` implementation and host registration before the activation router can resolve at runtime; then FND-035 must provide the packaged two-launch evidence after the owning manifest activation declarations are present. The public helper tests are a documented warning, not a merge blocker.
