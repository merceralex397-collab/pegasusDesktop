# Files — FND-035

Surveyed 2026-08-24 against fork `main`. Existing paths were confirmed with `ls`/`sed`; new files are
marked; files created by a named earlier ticket say so.

## Where the change lands

| Path | Why |
| --- | --- |
| `src/Pegasus.Desktop/Program.cs` | **New.** The explicit entry point: `[STAThread] static void Main(string[] args)` containing `AppInstance.GetCurrent().GetActivatedEventArgs()`, `AppInstance.FindOrRegisterForKey(<constant key>)`, the `IsCurrent` test, the **non-blocking** `RedirectActivationToAsync` call, and the process exit. Nothing else may live here. |
| `src/Pegasus.Desktop/Pegasus.Desktop.csproj` (created by [[FND-030]], plan handle `DSK-02-05`) | Add `<DefineConstants>$(DefineConstants);DISABLE_XAML_GENERATED_MAIN</DefineConstants>` — the exact XML Microsoft Learn gives. Appending to `$(DefineConstants)` rather than replacing it is not stylistic: the docs call it out, and replacing would drop whatever the template or `Directory.Build.props` set. |
| `src/Pegasus.Desktop/App.xaml.cs` (created by [[FND-030]], edited by [[FND-032]], plan handle `DSK-02-07`) | Subscribe to `AppInstance.Activated` in the owning instance and forward the redirected `AppActivationArguments` to the activation router. The host build from [[FND-032]] stays where it is; this adds a subscription, it does not move composition. |
| `src/Pegasus.Desktop/Services/IActivationRouter.cs` and its implementation | **New.** Parses deep-link and file arguments out of `AppActivationArguments` and asks `INavigationService` ([[FND-033]], plan handle `DSK-02-08`) to navigate. An argument it does not understand is **logged and ignored**, never thrown on. |
| `src/Pegasus.Desktop/Hosting/PegasusHost.cs` (created by [[FND-032]]) | Register `IActivationRouter`. It has a real caller from the moment it is registered, which is what `docs/engineering.md` § Abstractions (`:113`) requires. |
| `tests/Pegasus.Desktop.ViewModelTests/…` (created by [[FND-038]], plan handle `DSK-02-13`) | Argument-parsing and routing tests: a case deep link routes to the case route with the right identifier; a file activation routes to the document route; an unknown argument is ignored and logged. Instancing itself cannot be unit-tested. |
| `tests/Pegasus.Desktop.UITests/…` (created by [[TEST-006]], plan handle `DSK-08-06`) | A `single-instance` batch, **if that harness exists**. If it does not, the evidence is an explicitly recorded manual pass naming [[TEST-006]] as the automation follow-up. |
| `docs/current-architecture.md` | 682 lines; § Failure and recovery boundaries at `:565`. One line: the desktop is single-instance per Windows user and redirects activations. |

**Not this ticket's file, but part of the story:** `src/Pegasus.Desktop/Package.appxmanifest`
([[FND-030]]). For a packaged app, file and protocol activation are declared in the manifest, not
registered in code (Windows App SDK 1.0 release notes § 2.2). This ticket routes what arrives; it
cannot make anything arrive.

## Context files

What the implementer must **read** and what each one tells them.

| Path | What it tells the implementer |
| --- | --- |
| Windows App SDK 1.0 release notes § 3.3 — <https://learn.microsoft.com/windows/apps/windows-app-sdk/release-notes/windows-app-sdk-1-0#version-10> | That step 3's conditional has one answer: a WinUI app that redirects "must do so as early as possible, and before initializing any windows… the app **must define `DISABLE_XAML_GENERATED_MAIN`, and write a custom `Main` (C#)**". Do not spend the ticket re-deciding it; re-confirm and move on. |
| Same page, § 3.4 | **The trap this ticket is most likely to hit.** "RedirectActivationToAsync is an async call, and you should not wait on an async call if your app is running in an STA. For Windows Forms and C# WinUI apps, you can declare `Main` to be async, if necessary." A blocked STA hangs; it does not error. |
| <https://learn.microsoft.com/windows/apps/windows-app-sdk/applifecycle/applifecycle-instancing#examples> § Redirection without blocking | The pattern that fits the body's non-async `Main`: "call **RedirectActivationToAsync** in another thread, and set an event when the call completes. Then wait on that event using non-blocking APIs." |
| <https://learn.microsoft.com/windows/apps/develop/launch/multi-instance-apps> | The canonical four-line single-instance body (`FindOrRegisterForKey("single-instance")`, `IsCurrent`, `RedirectActivationToAsync`, `return`), that the custom `Main` "goes in `Program.cs`, before any XAML initialization", that registration is removed automatically at shutdown, and that `AppInstance.Activated` is where the owning instance receives the redirect. |
| <https://learn.microsoft.com/windows/apps/windows-app-sdk/applifecycle/applifecycle-instancing#how-the-windows-app-sdk-instancing-differs-from-uwp-instancing> | Two facts that shape the key: keys "have no inherent meaning", and an instance "cannot set its key to the same value that another instance has already registered" — the collision *is* the mechanism. And the scoping: "Separate lists are maintained for different versions of the same app, as well as instances of apps launched by different users" — per-user comes free; **per-version** is the upgrade case nothing in this ticket tests. |
| <https://learn.microsoft.com/windows/apps/windows-app-sdk/migrate-to-windows-app-sdk/guides/applifecycle#single-instanced-apps> | The *Important* note: the sample code "works as expected provided that you target the **x64** architecture". [[FND-030]]'s `<Platforms>x64</Platforms>` already satisfies it — corroboration for a property that might otherwise look like an arbitrary restriction. |
| Windows App SDK 1.0 release notes § 1.3 | That `OnLaunched` receives `Microsoft.UI.Xaml.LaunchActivatedEventArgs` while `GetActivatedEventArgs` returns `Microsoft.Windows.AppLifecycle.AppActivationArguments`. The router must be written against the latter or it will not compile on the redirected path. |
| Windows App SDK 1.0 release notes § 2.2 | "*Packaged apps*: Not usable; use the app's MSIX manifest instead" for rich-activation registration. Deep-link and file activations are declared in `Package.appxmanifest`, which is [[FND-030]]'s file, not this ticket's. |
| `.codex/skills/winui-dev-workflow/SKILL.md` § Common Errors | "App silently exits → Use `winapp run`, never run the .exe directly." For this ticket that line is the difference between proving the feature and misdiagnosing it: a directly-launched `.exe` exits silently, which looks exactly like a redirect that worked and exactly like a build that is broken. |
| `.codex/skills/winui-dev-workflow/SKILL.md` § Build & Run | That `BuildAndRun.ps1` launches through `winapp run --debug-output` and that success is the line `✅ <pkg> launched (PID: …)` — the string to look for when counting processes. |
| `src/Pegasus.Core/Workflow/CaseWorkflowContracts.cs:182-188` | `CaseMutationRequest(Guid CaseId, long ExpectedVersion, ActionActor Actor, string OperationKey, string Reason, string EditLeaseToken)` — what **actually** prevents a silent overwrite. Read it to understand that single-instancing is a convenience on top of a server-side invariant, not the invariant. |
| `src/Pegasus.Web/Mcp/AutomationMcpErrors.cs:33-51` | The three concurrency refusals the gateway already produces — lease expired, lease held by another actor, version conflict — each carrying the current case version "so the … Actor can reload and reacquire rather than retry blindly". Two machines can always reach this state; one machine should not have to. |
| `src/Pegasus.Desktop/Hosting/PegasusHost.cs` (created by [[FND-032]]) | Where `IActivationRouter` is registered, and the per-launch session identifier the activation log lines must carry. |
| `src/Pegasus.Desktop/Services/INavigationService.cs` (created by [[FND-033]]) | The **only** navigation mechanism. The router must go through it; a direct `Frame.Navigate` from the router would be the second navigation mechanism [[FND-033]] forbids. |
| `docs/desktop/02-architecture-and-foundation/README.md` § 3 decision 8 | The mechanism, the ordering constraint ("redirect before window creation") and the scope limit: "No multi-window in Phase 1." |
| `docs/desktop/02-architecture-and-foundation/README.md` § 4 exit-gate table | The row this ticket satisfies: "Single instance — second launch activates the first window (UI test)". |

## Ripple effects

- **The entry point changes for the whole application.** Defining `DISABLE_XAML_GENERATED_MAIN` means
  the build no longer generates `Main`; if `Program.cs` is wrong or missing, the project does not
  build at all. That is a whole-project effect from a small edit, and it lands on the file
  [[FND-030]] created.
- **`App.xaml.cs` gains a subscription** on top of [[FND-032]]'s host build. Ordering matters: the
  host is built in `OnLaunched`, and `AppInstance.Activated` can fire after that — the router must
  resolve through the host rather than capture services at subscription time.
- **`PegasusHost.cs` gains one registration**, `IActivationRouter`.
- **The activation log becomes bundle input.** [[FND-036]] (plan handle `DSK-02-11`) step 3 collects
  "the single-instance/activation log from [[FND-035]]" into the diagnostics bundle, so the **line
  format must be stable and redacted** from the first commit — a later format change breaks a
  consumer that has already shipped.
- **Downstream tickets.** [[FND-041]] (plan handle `DSK-02-16`) has a Phase 1 exit-gate row for this;
  [[DUI-004]] (plan handle `DSK-06-04`) lists `DSK-02-10` as a dependency "so a second launch
  reactivates this window rather than opening a second shell"; [[REL-006]] (plan handle `DSK-09-06`)
  is blocked by it.
- **Tests.** `tests/Pegasus.Desktop.ViewModelTests` gains three routing tests;
  `tests/Pegasus.Desktop.UITests` gains a `single-instance` batch if [[TEST-006]] has landed.
- **No solution, package, restore or architecture-test change.** No project and no package is added;
  `Pegasus.slnx`, `DependencyDirectionTests.cs` and every `packages.lock.json` are untouched.
- **Documentation.** One line in `docs/current-architecture.md` § Failure and recovery boundaries
  (`:565`); `scripts/Test-DocumentationLinks.ps1` runs in the CI `documentation` lane.

## Out of scope

Recorded so the reviewer sees each was a decision, matching the ticket's Guardrails.

- **The diagnostics bundle** — [[FND-036]]. This ticket writes activation log lines in a stable
  format; it does not package them.
- **The update flow** — areas 04 and 09.
- **Any deep-link target screen** — area 05. The router navigates to a route; the route's content is
  someone else's.
- **File and protocol activation *registration*** — `Package.appxmanifest`, owned by [[FND-030]].
- **Multi-window support** — refused. Plan 02 § 3 decision 8: "No multi-window in Phase 1."
- **`<WindowsPackageType>None</WindowsPackageType>`** — refused, even though it would make local
  testing easier. It removes package identity, which the instancing API depends on.
- **Running the packaged `.exe` directly** — refused as a verification method; `winapp run` or
  `BuildAndRun.ps1` only.
- **Swallowing an exception and continuing** — refused; [[FND-036]] owns the crash path and the rule
  is that a corrupted state is never continued in.
- **Deriving the instance key from a mutable value** (window title, version, timestamp) — refused.
  The key must be a fixed application string; the instancing store already scopes per user.
