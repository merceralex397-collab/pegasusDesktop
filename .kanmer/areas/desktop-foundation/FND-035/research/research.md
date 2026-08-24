# Research — FND-035: single instance per Windows user and activation redirection

## Question

Where exactly must the redirect run, what does the Windows App SDK require to put it there, and what
does single-instancing actually prevent — as opposed to what it is often assumed to prevent?

## Current behaviour

**No parity-matrix row covers this ticket, and none should.** The matrix at
`docs/desktop/01-inventory-and-parity/parity-matrix.md` holds `PAR-01`…`PAR-46` — counted with
`grep -c '^| PAR-'`, which returns **46** — and every row is "keyed by the Razor page model and
handler group that implements it today" (`parity-matrix.md:3-5`). Process instancing has no page
model, and the web application has no equivalent concept at all: a browser tab is not an application
instance, and two tabs open on the same case is the ordinary state today.

The closest existing repository mechanism is therefore **not** a shell behaviour but the server-side
concurrency design that already handles two editors:
`src/Pegasus.Core/Workflow/CaseWorkflowContracts.cs:182-188` (`CaseMutationRequest` carrying
`ExpectedVersion`, `OperationKey` and `EditLeaseToken`) and
`src/Pegasus.Core/Workflow/CaseEditAuthority.cs`, which owns lease validation. That is what actually
prevents a silent overwrite; single-instancing keeps an operator from walking into it.

## Findings

### Facts

Verified by reading the repository, and from official documentation fetched **2026-08-24**. Each
carries its source.

**Repository:**

- **`src/Pegasus.Desktop` does not exist yet.** `ls src` returns exactly `Pegasus.Core`,
  `Pegasus.Infrastructure`, `Pegasus.Web`, `Pegasus.Worker`. `App.xaml.cs`,
  `Pegasus.Desktop.csproj` and `Package.appxmanifest` are created by [[FND-030]] (plan handle
  `DSK-02-05`); `Hosting/PegasusHost.cs` by [[FND-032]] (plan handle `DSK-02-07`);
  `Services/INavigationService.cs` by [[FND-033]] (plan handle `DSK-02-08`).
- **The packaged, x64 shape this ticket depends on is fixed by [[FND-030]]**: plan 02 § 3 decision 3
  specifies `<Platforms>x64</Platforms>`, `RuntimeIdentifier win-x64`, packaged single-project MSIX,
  and forbids `<WindowsPackageType>None</WindowsPackageType>`.
- **The skill's rule about running the packaged app is explicit.**
  `.codex/skills/winui-dev-workflow/SKILL.md` § Common Errors: "App silently exits → Use `winapp run`,
  never run the .exe directly." For this ticket that is not a convenience — running the `.exe`
  directly is precisely how a working single-instance implementation gets misdiagnosed as broken.
- **`BuildAndRun.ps1` launches through `winapp run --debug-output`**
  (`.codex/skills/winui-dev-workflow/SKILL.md` § Build & Run, step 6 of "What the script does
  automatically"), and success is the line `✅ <pkg> launched (PID: …)`.
- **The concurrency machinery that actually prevents overwrites already exists.**
  `src/Pegasus.Core/Workflow/CaseWorkflowContracts.cs:182-188` —
  `public abstract record CaseMutationRequest(Guid CaseId, long ExpectedVersion, ActionActor Actor, string OperationKey, string Reason, string EditLeaseToken)`;
  `src/Pegasus.Core/Workflow/CaseEditAuthority.cs` owns lease validation; and
  `src/Pegasus.Web/Mcp/AutomationMcpErrors.cs:33-51` shows the three refusals it produces
  (`CaseEditLeaseExpiredException`, `CaseEditLeaseConflictException`, `CaseVersionConflictException`),
  each carrying the current case version so the caller can reload rather than retry blindly.
- **`tests/Pegasus.Desktop.ViewModelTests` does not exist** (`ls tests` → three projects only);
  [[FND-038]] (plan handle `DSK-02-13`) creates it. Nor does the `winapp ui` harness — [[TEST-006]]
  (plan handle `DSK-08-06`) creates `tests/Pegasus.Desktop.UITests`.

**Official documentation (Microsoft Learn, fetched 2026-08-24):** these settle both facts the ticket's
step 2 asks the implementer to confirm, and add two more.

- **The redirect must run before any window, and that requires an explicit entry point.** Windows App
  SDK 1.0 release notes § 3.3 — "*WinUI apps*: If an app wants to detect other instances and redirect
  an activation, it must do so **as early as possible, and before initializing any windows**, etc. To
  enable this, the app **must define `DISABLE_XAML_GENERATED_MAIN`, and write a custom `Main` (C#)**"
  — <https://learn.microsoft.com/windows/apps/windows-app-sdk/release-notes/windows-app-sdk-1-0#version-10>.
  The how-to gives the exact project XML, identical to the ticket body's step 3:
  `<DefineConstants>$(DefineConstants);DISABLE_XAML_GENERATED_MAIN</DefineConstants>` —
  <https://learn.microsoft.com/windows/apps/windows-app-sdk/applifecycle/applifecycle-single-instance#disable-auto-generated-program-code>.
  **So the ticket's step 3 conditional resolves to the first branch**: an explicit `Program.cs` is
  required, and the `App.OnLaunched` alternative is not available for this purpose.
- **The canonical single-instance body is four lines.**
  <https://learn.microsoft.com/windows/apps/develop/launch/multi-instance-apps> — "To make your app
  single-instance, always use the same key":
  ```csharp
  var instance = AppInstance.FindOrRegisterForKey("single-instance");
  if (!instance.IsCurrent)
  {
      await instance.RedirectActivationToAsync(AppInstance.GetCurrent().GetActivatedEventArgs());
      return;
  }
  ```
- **`RedirectActivationToAsync` must not be awaited on the STA — and this is the trap the ticket body
  does not name.** Release notes § 3.4: "**RedirectActivationToAsync is an async call, and you should
  not wait on an async call if your app is running in an STA.** For Windows Forms and C# WinUI apps,
  you can declare `Main` to be async, if necessary." The instancing guide's *Redirection without
  blocking* section adds the alternative for a non-async entry point: "call **RedirectActivationToAsync**
  in another thread, and set an event when the call completes. Then wait on that event using
  non-blocking APIs" —
  <https://learn.microsoft.com/windows/apps/windows-app-sdk/applifecycle/applifecycle-instancing#examples>.
  The ticket body's step 3 specifies `[STAThread] static void Main(string[] args)` — a **non-async**
  signature — so the non-blocking pattern is the one that fits it. Blocking the STA here does not
  fail loudly; it hangs.
- **x64 is a stated requirement of the sample code.** The migration guide's single-instancing section
  carries an *Important*: "The code shown below works as expected **provided that you target the
  x64 architecture**. That applies to both C# and C++/WinRT" —
  <https://learn.microsoft.com/windows/apps/windows-app-sdk/migrate-to-windows-app-sdk/guides/applifecycle#single-instanced-apps>.
  [[FND-030]]'s `<Platforms>x64</Platforms>` already satisfies this; it is corroboration, not a new
  constraint.
- **Instance lists are scoped per user *and per app version*.**
  <https://learn.microsoft.com/windows/apps/windows-app-sdk/applifecycle/applifecycle-instancing#how-the-windows-app-sdk-instancing-differs-from-uwp-instancing>
  — "Separate lists are maintained for different versions of the same app, as well as instances of
  apps launched by different users." The per-user scoping is what the ticket's title claims and it is
  free; the **per-version** scoping is not mentioned in the ticket and matters during an App Installer
  upgrade, when an old and a new version can each hold an instance.
- **Keys have no inherent meaning and cannot collide across instances.** Same page — "Each instance of
  a multi-instanced app can register an arbitrary key… An instance of an app cannot set its key to the
  same value that another instance has already registered. Attempting to register an existing key will
  result in `FindOrRegisterForKey` returning the app instance that has already registered that key."
  That returned instance is what `IsCurrent` is tested against.
- **The activation arguments are a different type from `OnLaunched`'s.** Release notes 1.3 —
  "WinUI's `App.OnLaunched` is given a `Microsoft.UI.Xaml.LaunchActivatedEventArgs`, whereas … the
  WindowsAppSDK `GetActivatedEventArgs` returns a
  `Microsoft.Windows.AppLifecycle.AppActivationArguments`". The activation router in step 6 consumes
  the latter, not the former.
- **A packaged app registers file and protocol activation in its manifest, not in code.** Release
  notes § 2.2 — "*Packaged apps*: Not usable; use the app's MSIX manifest instead." So the deep-link
  and file activations this ticket routes are declared in
  `src/Pegasus.Desktop/Package.appxmanifest`, which is [[FND-030]]'s file.
- **`AppInstance.Activated` is the event the owning instance handles the redirect on**, and the
  registration is removed automatically at shutdown, with `AppInstance.GetCurrent().UnregisterKey()`
  available for an explicit unregister —
  <https://learn.microsoft.com/windows/apps/develop/launch/multi-instance-apps>.

### Assumptions

- **A-FND035-1 — the non-blocking redirect pattern (another thread plus an event, waited with a
  non-blocking API) works from a `[STAThread] static void Main`.** *Confirms it*: the two-launch test
  at step 10, where a blocked STA presents as the second process hanging rather than exiting.
  *If wrong*: the documented alternative is `async Task Main`, which the release notes permit for C#
  WinUI apps — record whichever is used and why.
- **A-FND035-2 — a package-family-scoped constant key is sufficient for per-user single instancing
  without embedding a user identifier.** The instancing guide states lists are already "maintained
  for … instances of apps launched by different users". *Confirms it*: two sessions on one machine,
  if that is testable; otherwise the documentation citation stands and the limitation is recorded.
  *If wrong*: two operators sharing a workstation collide, which is the opposite of the intent.
- **A-FND035-3 — an App Installer upgrade does not leave an old-version instance holding the key in a
  way that breaks the new version's launch.** The per-version list scoping means both can hold a key
  simultaneously. *Confirms it*: [[FND-039]]'s (plan handle `DSK-02-14`) install/upgrade scenarios and
  area 08's packaging tests. *If wrong*: an operator ends up with two windows across an upgrade —
  exactly the state this ticket exists to prevent, arriving through the one path nobody tests.
- **A-FND035-4 — a redirected activation reaches the owning instance's `AppInstance.Activated` handler
  quickly enough that the operator perceives the existing window activating rather than a launch
  failure.** *Confirms it*: the manual two-launch pass at step 10. *If wrong*, the symptom is a
  perceived "nothing happened" on the second launch, which is a UX failure rather than a correctness
  one and belongs in the proof honestly.

## Execution placement

The six-question cloud-justification test from
`docs/desktop/00-governance-and-workflow/README.md` § 3, answered.

| Question | Answer | Evidence |
| --- | --- | --- |
| Shared authority — must several users see and update the same state? | **No** | Instance registration is per Windows user and per app version and lives entirely in the local instancing store (<https://learn.microsoft.com/windows/apps/windows-app-sdk/applifecycle/applifecycle-instancing>). No state crosses a machine boundary. |
| Unattended execution — must it run with every desktop closed? | **No** | The whole mechanism exists to manage what happens when a *person* launches the app a second time. |
| Protected credentials — a long-lived secret that must not sit on workstations? | **No** | The instance key is a constant application string with no secret content — the documentation is explicit that "keys have no inherent meaning". The step-4 rule that the key must not derive from a mutable value is about stability, not secrecy. |
| Public callback — must an external service call a stable public endpoint? | **No** | Deep-link and file activations arrive from the local Windows shell, not from an external service. For a packaged app they are declared in `Package.appxmanifest` (release notes § 2.2) and delivered by the OS; nothing listens on a network. |
| Central enforcement — revocation, permissions, audit or an invariant independent of the client? | **Yes — and it is already placed on the existing evolved `Pegasus.Web` gateway; this ticket adds a local convenience on top of it, not the control.** | The ticket's § Why names the risk as "two Pegasus windows editing the same case … the silent-overwrite failure the concurrency design exists to prevent". The **actual** prevention is server-side and already exists: `CaseMutationRequest` carries `ExpectedVersion` and `EditLeaseToken` (`src/Pegasus.Core/Workflow/CaseWorkflowContracts.cs:182-188`), `CaseEditAuthority` validates the lease, and the three refusals in `src/Pegasus.Web/Mcp/AutomationMcpErrors.cs:33-51` each return the current version so the caller reloads. Two *machines* can always edit the same case, so single-instancing could never be the invariant — it reduces how often an operator meets it on one machine. Stating it the other way round would be the dishonest answer this test exists to catch. |
| Measured operational advantage — measured evidence that central is materially better? | **No** | None claimed. The placement follows from ADR-0103 and L-01, which already put mutation authority behind the gateway. |

**Conclusion.** Five "no" and one "yes"; the "yes" names an existing responsibility on the existing
gateway that this ticket does **not** move, and everything this ticket actually places is local to the
workstation. No Azure write arises.

## Implications

1. **Step 3's conditional is already resolved by documentation: the explicit `Program.cs` branch
   wins.** Release notes § 3.3 requires `DISABLE_XAML_GENERATED_MAIN` and a custom `Main` for a WinUI
   app that redirects. The plan should still make step 2 re-confirm at kickoff (the SDK moves), but it
   should not be written as an even choice.
2. **The STA blocking rule is the failure this ticket is most likely to hit, and the body does not
   name it.** With a non-async `Main`, `await`-ing `RedirectActivationToAsync` blocks the STA and the
   second process hangs instead of exiting — a symptom indistinguishable from a broken build, and one
   the `winapp run` misdiagnosis trap will be blamed for. The documented pattern (another thread plus
   an event, waited with non-blocking APIs) must be in the plan, not discovered.
3. **Package identity is load-bearing.** `<WindowsPackageType>None</WindowsPackageType>` removes it,
   and the ticket's Guardrails already forbid adding it "to make instancing easier". The x64
   requirement in the migration guide's *Important* note lands on the same csproj.
4. **The activation router consumes `AppActivationArguments`, not `LaunchActivatedEventArgs`.** These
   are different types from different namespaces (release notes 1.3); writing the router against the
   `OnLaunched` type will not compile against the redirected path.
5. **File and protocol registration is manifest work owned by another ticket.** For a packaged app the
   declaration lives in `Package.appxmanifest` ([[FND-030]]). This ticket routes what arrives; it
   cannot make anything arrive on its own.
6. **The upgrade case is untested by anything in this ticket.** Per-version instance lists mean an old
   and a new version can each hold a key. That belongs to [[FND-039]] and area 08, and saying so is
   better than a two-launch test that quietly does not cover it.
7. **The redirected process must do nothing else.** Step 5 permits exactly one log line. Building the
   host, opening a log sink or creating a window in the redirected process are all observable as a
   flash of a second window or a second log file, and all of them defeat the point.

## Open questions

- **None.** The two facts the ticket's step 2 asks the implementer to establish are settled above with
  their documentation URLs and a 2026-08-24 fetch date, and re-confirming them at kickoff is a step
  inside this ticket rather than a question for anyone else. The four assumptions each name the
  command or the sibling ticket that settles them, and the one genuinely untested case — instancing
  across an App Installer upgrade — is a scope boundary owned by [[FND-039]] and area 08, recorded in
  the plan's Risks section.
