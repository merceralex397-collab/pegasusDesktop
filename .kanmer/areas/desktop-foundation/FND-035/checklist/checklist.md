# Checklist — FND-035

One box per plan step, in plan order. Each is independently tickable: it names the file, value or
command whose completion makes the box true.

- [ ] Read `docs/desktop/02-architecture-and-foundation/README.md` § 3 decision 8 and § 4's exit-gate table; run `get_doc_gates FND-035`; `take_ticket` on branch `task/desktop-single-instance` from `origin/dev`.
- [ ] Re-confirm from Microsoft Learn that the redirect must run before any window and requires `DISABLE_XAML_GENERATED_MAIN` plus a custom `Main` (Windows App SDK 1.0 release notes § 3.3), and re-confirm the STA rule (§ 3.4); record the fetch date in the research document.
- [ ] Add `<DefineConstants>$(DefineConstants);DISABLE_XAML_GENERATED_MAIN</DefineConstants>` to `src/Pegasus.Desktop/Pegasus.Desktop.csproj`, **appending** to `$(DefineConstants)` rather than replacing it.
- [ ] Create `src/Pegasus.Desktop/Program.cs` with `[STAThread] static void Main(string[] args)`.
- [ ] In `Main`, call `AppInstance.GetCurrent().GetActivatedEventArgs()` then `AppInstance.FindOrRegisterForKey(<fixed application key>)` and test `IsCurrent`; confirm the key derives from no mutable value (no user id, window title, version or timestamp).
- [ ] Implement the redirect **off the STA** — `RedirectActivationToAsync` on another thread, signalling an event, waited with a non-blocking API — then terminate the process immediately; record in the plan which entry-point shape was used (non-async `Main` off-thread, or `async Task Main`) and why.
- [ ] Confirm the redirected process creates **no** window, **no** host and **no** log sink beyond a single redirect line.
- [ ] Subscribe to `AppInstance.Activated` in `src/Pegasus.Desktop/App.xaml.cs` and forward the redirected `Microsoft.Windows.AppLifecycle.AppActivationArguments` (not `Microsoft.UI.Xaml.LaunchActivatedEventArgs`) to the router, resolving the router through the host when the event fires rather than capturing services at subscription time.
- [ ] Create `src/Pegasus.Desktop/Services/IActivationRouter.cs` and its implementation; register it in `Hosting/PegasusHost.cs`; route through `INavigationService` only, and log-and-ignore any argument the router does not understand.
- [ ] Bring the existing window forward on redirect (restore if minimised, then activate), using the `AppWindow` call confirmed from documentation rather than a guessed Win32 interop.
- [ ] Log every activation and redirect with the per-launch session identifier from [[FND-032]] (plan handle `DSK-02-07`) in a **stable, redacted** line format, because [[FND-036]] (plan handle `DSK-02-11`) collects this log into the diagnostics bundle.
- [ ] Add the three routing tests to `tests/Pegasus.Desktop.ViewModelTests`: case deep link → case route with the right identifier; file activation → document route; unknown argument ignored and logged.
- [ ] Launch the packaged app **twice** via `winapp run` (never the packaged `.exe` directly); confirm exactly one window, a single Pegasus process in `Get-Process`, and the second launch's argument in the activation log.
- [ ] Add a `single-instance` batch to [[TEST-006]] (plan handle `DSK-08-06`)'s `winapp ui` harness if it exists; otherwise record a manual pass with a screenshot and name [[TEST-006]] as the automation follow-up.
- [ ] Record in the proof that instancing **across an App Installer upgrade** is not covered by the two-launch test (instance lists are per app version) and that [[FND-039]] (plan handle `DSK-02-14`) and area 08 own it.
- [ ] Confirm no multi-window capability was introduced (plan 02 § 3 decision 8: Phase 1 is single-window only).
- [ ] Add the single-instance line to `docs/current-architecture.md` § Failure and recovery boundaries (`:565`).
- [ ] Run the simplification pass over this branch's diff and record it under a dated `## Simplification pass` heading in the plan document.
- [ ] Verification run (this box produces `proof`): the two-launch `winapp run` demonstration with the process listing and activation log lines; `dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release --filter "FullyQualifiedName~Activation"`; `pwsh .codex/skills/winui-dev-workflow/BuildAndRun.ps1 src/Pegasus.Desktop/Pegasus.Desktop.csproj -SkipRun` (exit 0, zero warnings); `grep -n 'DISABLE_XAML_GENERATED_MAIN' src/Pegasus.Desktop/Pegasus.Desktop.csproj` (one appending line); `grep -rn 'WindowsPackageType' src/Pegasus.Desktop/` (no matches); and the negative check that the redirected process produced no second window and no second log file. Capture every output as tier-7 evidence.

## Progress notes

(append with `set_ticket_doc(doc: "checklist", append: true)`)
