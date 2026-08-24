# Plan — FND-035: Single instance per Windows user — `AppInstance.FindOrRegisterForKey` and activation redirection

**Diff estimate: ~7 files, ~230 lines.**

`docs/engineering.md` § Plan sizing (`:201`) requires the estimate first. Derived from the files
document: `Program.cs` ~70 (the entry point plus the non-blocking redirect pattern, which is longer
than the four-line sample); `Services/IActivationRouter.cs` + implementation ~85;
`App.xaml.cs` +25 (the `AppInstance.Activated` subscription and window activation);
`Pegasus.Desktop.csproj` +2 (`DefineConstants`); `Hosting/PegasusHost.cs` +2 (one registration);
`docs/current-architecture.md` +2. The three routing tests land in
`tests/Pegasus.Desktop.ViewModelTests` and are counted against that project.

## Approach

Put the whole decision in an explicit `Program.Main` and let the redirected process do **nothing** —
no host, no window, no log sink beyond one line. That is not a stylistic preference: Windows App SDK
1.0 release notes § 3.3 states that a WinUI app wanting to redirect "must do so as early as possible,
and **before initializing any windows**… the app must define `DISABLE_XAML_GENERATED_MAIN`, and write
a custom `Main` (C#)". So the ticket body's step 3 conditional — explicit `Main` versus
`App.OnLaunched` — is already answered by documentation, and this plan is written to the first branch
while step 2 still re-confirms it at kickoff, because the SDK moves.

The one thing this plan adds that the body does not name is **how** the redirect is awaited.
`RedirectActivationToAsync` is asynchronous; release notes § 3.4 says plainly "you should not wait on
an async call if your app is running in an STA", and the body specifies a non-async
`[STAThread] static void Main(string[] args)`. The documented fit for that signature is the
instancing guide's *Redirection without blocking* pattern — call it on another thread, set an event,
wait on that event with non-blocking APIs. A blocked STA does not error; the second process **hangs**,
which is indistinguishable from a broken build and will be blamed on the `winapp run` trap. That
pattern is therefore in step 5, not left to discovery.

## Governing docs

The ticket's `refs` array is empty and `get_doc_gates FND-035` reports `docs_todo: true`, so there is
no linked PRD/FRD/ADR to meet today.

> **New ADR** — ADR-0100 (native WinUI 3 client in the fork), which fixes the packaged single-project
> MSIX with **package identity** — the thing that makes `AppInstance` keys per-user meaningful and
> the reason `<WindowsPackageType>None</WindowsPackageType>` is forbidden here. Authored by
> [[FND-026]] (plan handle `DSK-02-01`); [[FND-005]] (plan handle `DSK-00-05`) also claims ADR-0100 in
> the reserved block — see [[FND-026]]'s plan for the ownership reconciliation.
> This plan is written to the decision as recorded in
> `docs/desktop/00-governance-and-workflow/README.md` § 3 (ADR set table) and
> `docs/desktop/02-architecture-and-foundation/README.md` § 3 decision 8; if the ADR lands differently
> this plan is revised before implementation.

Because `refs` is empty, these are the authorities that actually bind today:

| Authority | Requirement | Met by |
| --- | --- | --- |
| Proposal § 7.3 Single-instance behaviour | A second launch activates the existing window; deep links and file activations are redirected to the active process; unsaved work is never duplicated across processes | Steps 4–7 |
| Plan 02 § 3 decision 8 | `AppInstance.FindOrRegisterForKey` + `RedirectActivationToAsync` **before any window is created**; redirected activations carry deep-link/file arguments; **no multi-window in Phase 1** | Steps 3–6, 11 |
| Plan 02 § 4 exit-gate table | "Single instance — second launch activates the first window (UI test)" | Step 10, § Verification |
| Windows App SDK 1.0 release notes § 3.3 | `DISABLE_XAML_GENERATED_MAIN` plus a custom `Main` is **required** for a redirecting WinUI app | Steps 2, 3 |
| Windows App SDK 1.0 release notes § 3.4 and the instancing guide § Redirection without blocking | Do not await an async call on the STA; use another thread plus an event, waited with non-blocking APIs | Step 5 |
| Windows App SDK migration guide § Single-instanced apps (*Important*) | The sample code requires targeting **x64** | Already satisfied by [[FND-030]]'s `<Platforms>x64</Platforms>`; verified, not re-decided |
| `.codex/skills/winui-dev-workflow/SKILL.md` § Critical Rules / § Common Errors | Never run the packaged `.exe` directly — always `winapp run` | Step 10, § Verification |
| ADR-0103 / L-01 (via `src/Pegasus.Core/Workflow/CaseEditAuthority.cs`) | Concurrency is enforced server-side; the client does not own the invariant | § Approach and the research's placement table — this ticket claims a convenience, not a control |
| `docs/engineering.md` § Abstractions (`:113`) | An interface needs a real caller | Step 6 — `IActivationRouter` is registered and called in the same commit |
| `docs/engineering.md` § Required evidence tiers (`:72`), tier 7 | The two-launch scenario is **demonstrated on a real session**, not asserted from code | § Verification |

## Routing

Copied from the ticket body's `## Routing` block, as
`docs/desktop/00-governance-and-workflow/README.md` § Ticket template requires of the plan document
specifically.

- **Subagent**: `winui-dev` — `.codex/agents/winui-dev.toml` (verified present).
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
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement
  (`AGENTS.md` § Repository task workflow step 5).

## Steps

These refine the ticket body's eleven steps: same order, same ownership, same paths.

1. **Orient.** Read `docs/desktop/02-architecture-and-foundation/README.md` § 3 decision 8 and § 4's
   exit-gate table, then `get_doc_gates FND-035` and `take_ticket` on branch
   `task/desktop-single-instance` from `origin/dev`.
2. **Re-confirm the mechanism from official documentation before writing code.**
   `microsoft_docs_search` for `AppInstance.FindOrRegisterForKey` redirection semantics and for the
   Windows App SDK app-lifecycle single-instancing sample. The two facts the body asks for are already
   established in this ticket's `research` document with their URLs and a 2026-08-24 fetch date —
   **(a)** the redirect must run before `Application.Start` and **(b)** that requires
   `DISABLE_XAML_GENERATED_MAIN` and an explicit `Program.Main` (release notes § 3.3). Re-fetch to
   confirm they have not moved and record the fetch date in the research document. Do not spend the
   ticket re-deciding a settled point; do spend it confirming the version you are on.
3. **Define the symbol and add the entry point.** In `src/Pegasus.Desktop/Pegasus.Desktop.csproj` add
   `<DefineConstants>$(DefineConstants);DISABLE_XAML_GENERATED_MAIN</DefineConstants>` — **appending**
   to `$(DefineConstants)`, which Microsoft Learn calls out explicitly, so nothing the template or
   `Directory.Build.props` set is dropped. Create `src/Pegasus.Desktop/Program.cs` with
   `[STAThread] static void Main(string[] args)`. Note that from this commit the project will not
   build without `Program.cs`, because the generated entry point is gone.
4. **Register the key.** `AppInstance.GetCurrent().GetActivatedEventArgs()`, then
   `AppInstance.FindOrRegisterForKey(<key>)`. The key is a **fixed application string** — the
   instancing store already maintains "separate lists … for … instances of apps launched by different
   users", so per-user scoping is free and the key must not embed a user, a window title, a version or
   any other mutable value. Test `IsCurrent` on the returned instance; a key collision is the
   mechanism, not an error ("Attempting to register an existing key will result in
   `FindOrRegisterForKey` returning the app instance that has already registered that key").
5. **Redirect without blocking, then exit.** When the returned instance is not current, call
   `RedirectActivationToAsync(args)` **off the STA** — the documented pattern for a non-async `Main`
   is to call it on another thread, signal an event on completion, and wait on that event with a
   non-blocking API (instancing guide § Redirection without blocking; release notes § 3.4). Then
   terminate immediately: **no window, no host, no log sink beyond a single redirect line**. If the
   team prefers `async Task Main`, the release notes permit it for C# WinUI apps — record whichever
   was used and why. Do not `await` on the STA: it hangs rather than failing, and the symptom is
   indistinguishable from a broken build.
6. **Route the activation in the owning instance.** Subscribe to `AppInstance.Activated` in
   `App.xaml.cs` and forward the redirected `AppActivationArguments` — note the type: `OnLaunched`
   receives `Microsoft.UI.Xaml.LaunchActivatedEventArgs`, while `GetActivatedEventArgs` returns
   `Microsoft.Windows.AppLifecycle.AppActivationArguments` (release notes 1.3), and the router must be
   written against the latter — to an `IActivationRouter` registered in `Hosting/PegasusHost.cs`
   ([[FND-032]], plan handle `DSK-02-07`). The router parses deep-link and file arguments and asks
   `INavigationService` ([[FND-033]], plan handle `DSK-02-08`) to navigate; **`INavigationService` is
   the only navigation mechanism**, so no direct `Frame.Navigate` from the router. An argument it does
   not understand is **logged and ignored**, never crashed on. Resolve the router *through* the host
   when the event fires rather than capturing services at subscription time — `AppInstance.Activated`
   can fire after `OnLaunched` has built the host.
7. **Bring the window forward.** Restore it if minimised and activate it. Use `winui-design` /
   `microsoft_docs_search` for `AppWindow` activation to confirm the supported call rather than
   guessing at a Win32 interop.
8. **Log every activation and redirect** with the per-launch session identifier from [[FND-032]],
   into the single-instance/activation log. [[FND-036]] (plan handle `DSK-02-11`) step 3 collects that
   log into the diagnostics bundle, so the **line format must be stable and redacted from this
   commit** — a later format change breaks a consumer that has already shipped.
9. **Tests in `tests/Pegasus.Desktop.ViewModelTests`** ([[FND-038]], plan handle `DSK-02-13`) for
   argument parsing and routing: a case deep link routes to the case route with the right identifier;
   a file activation routes to the document route; an unknown argument is ignored and logged.
   Instancing itself cannot be unit-tested — that is step 10.
10. **Prove it end to end.** Install or run the packaged app, then launch it **twice**, using
    `winapp run` (or `BuildAndRun.ps1`, which wraps it) and **never the packaged `.exe` directly** —
    a directly-launched `.exe` exits silently and will misdiagnose this feature in both directions.
    Confirm exactly one window exists, `Get-Process` shows a single Pegasus process, and the second
    launch's arguments reached the first window via the activation log. If [[TEST-006]]'s (plan handle
    `DSK-08-06`) `winapp ui` harness exists, add a `single-instance` batch to it; otherwise record a
    manual pass with a screenshot and name [[TEST-006]] as the automation follow-up.
11. **Confirm no multi-window capability was added** — Phase 1 is single-window only (plan 02 § 3
    decision 8). Add the one line to `docs/current-architecture.md` § Failure and recovery boundaries
    (`:565`). Run the simplification pass, record it under a dated heading below, and open the PR into
    `dev`.

## Verification

Evidence tier **7 — Browser/accessibility** (`docs/engineering.md` § Required evidence tiers, `:72`),
applied to the desktop as the UI-behaviour tier: the two-launch scenario is **demonstrated on a real
session**, not asserted from code.

The `proof` document is produced from these:

1. **Launch the packaged app twice via `winapp run`** — expected: one window; `Get-Process` shows a
   single Pegasus process; the second launch's argument is visible in the activation log. Paste the
   process listing and the log lines, and state plainly that `winapp run` was used and the `.exe` was
   not.
2. `dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release --filter "FullyQualifiedName~Activation"`
   — expected: the routing and unknown-argument tests pass.
3. `pwsh .codex/skills/winui-dev-workflow/BuildAndRun.ps1 src/Pegasus.Desktop/Pegasus.Desktop.csproj -SkipRun`
   — expected: exit 0, zero warnings. (This also proves `Program.cs` is present and correct: with
   `DISABLE_XAML_GENERATED_MAIN` defined, a missing or wrong entry point is a build failure.)
4. Additionally, and not in the body — three checks that make acceptance criteria executable:
   - `grep -n 'DISABLE_XAML_GENERATED_MAIN' src/Pegasus.Desktop/Pegasus.Desktop.csproj` — expected:
     one line, **appending** to `$(DefineConstants)`, not replacing it.
   - `grep -rn 'WindowsPackageType' src/Pegasus.Desktop/` — expected: no matches. Package identity is
     what the instancing API depends on.
   - A **negative** check on the redirected process: after the second launch, confirm no second log
     file and no second window were created — the redirected process must do nothing beyond one
     redirect line. A flash of a second window is the observable failure.
5. Record which entry-point shape was used (non-async `Main` with the off-thread redirect, or
   `async Task Main`) and why, with the documentation URL.

## Risks / open questions

- **Risk — blocking the STA.** The single most likely failure. `RedirectActivationToAsync` is async,
  the body specifies a non-async `Main`, and release notes § 3.4 says not to wait on an async call on
  an STA. The symptom is a **hang**, not an error, and the `winapp run` misdiagnosis trap will be
  blamed for it. *Mitigation*: step 5's off-thread-plus-event pattern, taken from the instancing
  guide's *Redirection without blocking* section, and § Verification item 5 recording which shape was
  used.
- **Risk — running the packaged `.exe` directly.** It exits silently, which looks exactly like a
  successful redirect **and** exactly like a broken build. *Mitigation*: `winapp run` or
  `BuildAndRun.ps1` only, stated in the proof.
- **Risk — losing package identity to make testing easier.**
  `<WindowsPackageType>None</WindowsPackageType>` would remove the identity the instancing API depends
  on. *Mitigation*: forbidden by the Guardrails and checked by § Verification item 4.
- **Risk — the redirected process does more than it should.** Building a host, opening a log sink or
  creating a window in the redirected process defeats the point and is visible as a flash of a second
  window or a second log file. *Mitigation*: the negative check in § Verification item 4.
- **Risk — the router captures stale services.** `AppInstance.Activated` can fire after `OnLaunched`
  has built the host. *Mitigation*: step 6 resolves through the host when the event fires.
- **Risk — the activation log format changes later.** [[FND-036]] ships a consumer of it.
  *Mitigation*: step 8 fixes the format and the redaction now.
- **Untested case, recorded not resolved — instancing across an App Installer upgrade.** The
  instancing store maintains "separate lists … for different versions of the same app", so an old and
  a new version can each hold a key. Nothing in this ticket's two-launch test covers it. That belongs
  to [[FND-039]]'s (plan handle `DSK-02-14`) install/upgrade scenarios and area 08's packaging tests;
  say so in the proof rather than implying the two-launch test covered it.
- **Scope boundary, not an open question — file and protocol activation registration.** For a packaged
  app it is declared in `src/Pegasus.Desktop/Package.appxmanifest` (release notes § 2.2), which is
  [[FND-030]]'s (plan handle `DSK-02-05`) file. This ticket routes what arrives.
- **Scope boundary, not an open question — the diagnostics bundle, the update flow and deep-link
  target screens.** [[FND-036]], areas 04/09, and area 05 respectively.
- **No `open-questions` document is opened.** Both facts the body's step 2 asks for are settled in the
  research with URLs and a fetch date; re-confirming them is step 2 of this ticket's own work. Nothing
  requires an answer from outside the ticket before implementation begins.

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over this branch's own
diff before the PR, recorded here under a dated heading._
