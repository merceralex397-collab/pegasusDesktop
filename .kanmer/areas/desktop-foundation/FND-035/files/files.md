# Files — FND-035

Surveyed 2026-08-24 against fork `main`. Every existing path was confirmed with `ls`/`sed`/`grep`;
paths created by an earlier ticket are marked with that ticket. Consistent with this ticket's
`research` document, which measured the same tree and settled the mechanism from official
documentation.

## Where the change lands

| Path | Why |
| --- | --- |
| `src/Pegasus.Desktop/Program.cs` | **New — and the research settles that it is required, not optional.** Windows App SDK 1.0 release notes § 3.3: a WinUI app that redirects an activation "must do so as early as possible, and before initializing any windows … To enable this, the app **must define `DISABLE_XAML_GENERATED_MAIN`, and write a custom `Main` (C#)**". Holds `[STAThread] static void Main(string[] args)`, the `FindOrRegisterForKey` test, the non-blocking redirect, and `Application.Start` for the owning instance. |
| `src/Pegasus.Desktop/Pegasus.Desktop.csproj` (created by [[FND-030]], plan handle `DSK-02-05`) | Add `<DefineConstants>$(DefineConstants);DISABLE_XAML_GENERATED_MAIN</DefineConstants>` — the exact XML from the Learn how-to. Nothing else; `<Platforms>x64</Platforms>` is already set by [[FND-030]] and the migration guide's *Important* note requires it for this sample code. |
| `src/Pegasus.Desktop/App.xaml.cs` (created by [[FND-030]], edited by [[FND-032]], plan handle `DSK-02-07`) | Subscribe to `AppInstance.Activated` in the **owning** instance and forward the redirected `AppActivationArguments` to the router. [[FND-032]] owns the host build in `OnLaunched` and left the pre-window region clean for exactly this; keep the two concerns separate. |
| `src/Pegasus.Desktop/Services/IActivationRouter.cs` + its implementation | **New.** Parses deep-link and file activation arguments and asks `INavigationService` ([[FND-033]], plan handle `DSK-02-08`) to navigate. An argument it does not understand is **logged and ignored, never crashed on**. It consumes `Microsoft.Windows.AppLifecycle.AppActivationArguments` — *not* `Microsoft.UI.Xaml.LaunchActivatedEventArgs`; the two are different types from different namespaces and writing it against the wrong one will not compile against the redirected path. |
| `src/Pegasus.Desktop/Hosting/PegasusHost.cs` (created by [[FND-032]]) | Register `IActivationRouter`. One registration; the router's only caller is the `Activated` handler. |
| `tests/Pegasus.Desktop.ViewModelTests/**` (created by [[FND-038]], plan handle `DSK-02-13`) | Argument-parsing and routing tests: a case deep link routes to the case route with the right identifier; a file activation routes to the document route; an unknown argument is ignored and logged. **Instancing itself cannot be unit-tested** — that is the two-launch pass. |
| `docs/current-architecture.md` | 682 lines. § Failure and recovery boundaries at `:565` gains one line: the desktop is single-instance per Windows user and redirects activations to the running instance. |

## Context files

What the implementer must **read**, and what each one tells them.

| Path | What it tells the implementer |
| --- | --- |
| This ticket's `research` document | **Read it before step 2 — it already answers step 2.** Both facts the body asks the implementer to establish are settled there with URLs and a 2026-08-24 fetch date: the redirect must run before any window and therefore needs `DISABLE_XAML_GENERATED_MAIN` plus a custom `Main`, so step 3's conditional resolves to its **first** branch. It also carries the four-line canonical single-instance body and the STA trap below. |
| Learn — *Windows App SDK 1.0 release notes* § 3.4 and *applifecycle-instancing* § Redirection without blocking | **The trap the ticket body does not name.** "`RedirectActivationToAsync` is an async call, and you should **not wait on an async call if your app is running in an STA**." The body specifies a **non-async** `[STAThread] static void Main`, so the documented non-blocking pattern applies: "call `RedirectActivationToAsync` in another thread, and set an event when the call completes. Then wait on that event using non-blocking APIs." Blocking the STA here does not fail loudly — **it hangs**, and the hang looks exactly like a broken build. |
| Learn — *applifecycle-instancing* § How the Windows App SDK instancing differs from UWP | Two facts that shape step 4. First, "Separate lists are maintained for different versions of the same app, **as well as instances of apps launched by different users**" — the per-user scoping the ticket's title claims is free, which is why the key needs no user identifier. Second, the same per-**version** scoping means an old and a new version can each hold an instance during an App Installer upgrade — a case nothing in this ticket tests. |
| Learn — *multi-instance-apps* | The canonical body — "To make your app single-instance, always use the same key": `FindOrRegisterForKey("single-instance")`, test `IsCurrent`, `RedirectActivationToAsync(AppInstance.GetCurrent().GetActivatedEventArgs())`, return. Also that "keys have no inherent meaning", that registration is removed automatically at shutdown, and that `AppInstance.Activated` is the owning instance's event. |
| Learn — *migrate-to-windows-app-sdk/guides/applifecycle* § Single-instanced apps | The *Important* note: "The code shown below works as expected **provided that you target the x64 architecture**." [[FND-030]]'s `<Platforms>x64</Platforms>` already satisfies it — corroboration for a decision already made, not a new constraint. |
| Learn — *Windows App SDK 1.0 release notes* § 2.2 | "*Packaged apps*: Not usable; use the app's MSIX manifest instead." File and protocol activations are **declared in `src/Pegasus.Desktop/Package.appxmanifest`**, which is [[FND-030]]'s file. This ticket routes what arrives; it cannot make anything arrive. |
| `.codex/skills/winui-dev-workflow/SKILL.md:76` § Common Errors | "App silently exits → Use `winapp run`, never run the .exe directly." For this ticket that is not a convenience — running the `.exe` directly is **precisely how a working single-instance implementation gets misdiagnosed as broken**, because the second process exiting is the intended behaviour. |
| `.codex/skills/winui-dev-workflow/SKILL.md:98` § Critical Rules | "NEVER add `<WindowsPackageType>None`". It removes package identity, which the instancing API depends on — so the one shortcut that would seem to "make instancing easier" is the one that breaks it. |
| `src/Pegasus.Core/Workflow/CaseWorkflowContracts.cs:182-188` | `public abstract record CaseMutationRequest(Guid CaseId, long ExpectedVersion, ActionActor Actor, string OperationKey, string Reason, string EditLeaseToken)` — **the machinery that actually prevents a silent overwrite**, and it already exists. Read it to understand the honest scope of this ticket: single-instancing keeps an operator from *walking into* a conflict on one machine; it is not the invariant, because two machines can always edit the same case. |
| `src/Pegasus.Web/Mcp/AutomationMcpErrors.cs:35-53` | The three refusals that machinery produces — `CaseEditLeaseExpiredException`, `CaseEditLeaseConflictException`, `CaseVersionConflictException` — each carrying the current case version with "reload and reacquire rather than retrying". This is what a second editor actually meets. |
| `src/Pegasus.Desktop/Hosting/PegasusHost.cs` (created by [[FND-032]]) | Where the router registers, and the source of the **per-launch session identifier** step 8 must stamp on every activation and redirect line. |
| `src/Pegasus.Desktop.Infrastructure/Diagnostics/IDiagnosticsWriter.cs` (created by [[FND-031]], plan handle `DSK-02-06`) | The sink and its redaction hook. The activation log's line format must be **stable**, because [[FND-036]] (plan handle `DSK-02-11`) includes that log in the diagnostics bundle and asserts a manifest against it. |
| `docs/current-architecture.md:565` § Failure and recovery boundaries | The section that gains the one-line statement, and the surrounding entries whose tone it must match. |

## Ripple effects

- **The activation-log line format becomes an interface the moment it is written.** [[FND-036]]
  packages that log into the diagnostics bundle and asserts a bundle manifest; [[FND-049]] (plan
  handle `DSK-04-13`) tells an operator where to find it. Changing the format later breaks both.
- **`Program.cs` takes over the entry point from the XAML-generated `Main`.** Once
  `DISABLE_XAML_GENERATED_MAIN` is defined, **every** future change to application startup lands in
  this file rather than in generated code — including anything [[FND-036]] wants to do before the
  first window for crash handling. Say so in the PR.
- **[[FND-036]] must not swallow an exception and continue.** Its unhandled-exception path runs in the
  same startup region this ticket now owns; the Guardrails already name that boundary, and the two
  tickets meet in `Program.cs`.
- **[[DUI-004]] (plan handle `DSK-06-04`) and [[REL-006]] are blocked on this ticket** per the board.
  [[REL-006]] is release work whose install/upgrade scenarios are where the per-version instancing
  case (A-FND035-3) would actually surface.
- **[[FND-041]] (plan handle `DSK-02-16`), the Phase 1 exit review**, has a dedicated "Single
  instance" gate row: "second launch activates the first window (UI test)". This ticket is what makes
  that row answerable.
- **[[TEST-006]] (plan handle `DSK-08-06`) gains a candidate batch.** If its `winapp ui` harness
  exists, a `single-instance` batch belongs in it; if not, this ticket's evidence is a recorded manual
  pass and [[TEST-006]] is the named follow-up.
- **No OpenAPI, generated-client or contract ripple.** This ticket adds no contract type and calls no
  endpoint. Say so in the PR rather than leaving the reviewer to check `openapi/pegasus-v1.json`.
- **Documentation.** `docs/current-architecture.md` changes, and `scripts/Test-DocumentationLinks.ps1`
  runs in the CI `documentation` lane (`.github/workflows/ci.yml:76-87`).

## Out of scope

Recorded so the reviewer sees each was a decision, matching the ticket's Guardrails.

- **The diagnostics bundle and the unhandled-exception path** — [[FND-036]]. This ticket writes the
  activation log lines; that one packages them. Crash handling must not swallow an exception and
  continue in a corrupted state, and this ticket does not add any handler that could.
- **The update flow** — area 04 and area 09. Instancing across an App Installer upgrade
  (A-FND035-3) belongs to [[FND-039]] (plan handle `DSK-02-14`) and area 08's packaging tests, and is
  recorded in the plan's Risks rather than half-covered here.
- **Any deep-link target screen** — area 05. The router navigates to a route; the route's content is
  someone else's.
- **Declaring file or protocol activations** — `src/Pegasus.Desktop/Package.appxmanifest` is
  [[FND-030]]'s file, and for a packaged app that declaration is manifest work, not code (release
  notes § 2.2). This ticket routes what arrives.
- **Multi-window** — refused. Plan 02 § 3 decision 8 rules it out of Phase 1, and the acceptance
  criteria make its absence explicit.
- **`<WindowsPackageType>None</WindowsPackageType>`** — refused absolutely. It removes package
  identity, which the instancing API depends on; the Guardrails name it as the tempting shortcut that
  breaks the feature.
- **Running the packaged `.exe` directly** — refused. Always `winapp run` or `BuildAndRun.ps1`; the
  `.exe` path makes a *correct* implementation look broken.
- **Building a key from a mutable value** — refused. No window title, no version number, no
  timestamp. A constant application key is sufficient because the instance lists are already scoped
  per user.
- **Doing anything else in the redirected process** — refused. One log line, then exit: no host, no
  window, no view model, no second log file.
