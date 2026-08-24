# Checklist — FND-035

One box per plan step, in plan order. Each names the file, call or command whose completion makes it
true, so it can be ticked independently and honestly.

- [ ] Read plan 02 § 3 decision 8 and § 4's exit-gate table, and this ticket's `research` document — it already answers step 2 with URLs and a 2026-08-24 fetch date.
- [ ] Confirm [[FND-030]] (plan handle `DSK-02-05`), [[FND-032]] (plan handle `DSK-02-07`) and [[FND-033]] (plan handle `DSK-02-08`) have landed — the project, the host, and `INavigationService`. Then `get_doc_gates FND-035` and `take_ticket` on branch `task/desktop-single-instance` from `origin/dev`.
- [ ] Re-confirm with `microsoft_docs_search` that (a) the redirect must run before any window and (b) that requires `DISABLE_XAML_GENERATED_MAIN` plus a custom `Main`; record the re-confirmation date and URLs in the proof.
- [ ] Add `<DefineConstants>$(DefineConstants);DISABLE_XAML_GENERATED_MAIN</DefineConstants>` to `src/Pegasus.Desktop/Pegasus.Desktop.csproj`.
- [ ] Create `src/Pegasus.Desktop/Program.cs` with `[STAThread] static void Main(string[] args)`, and note in the PR that this file now owns application startup for every future pre-window change.
- [ ] In `Main`, call `AppInstance.GetCurrent().GetActivatedEventArgs()` then `AppInstance.FindOrRegisterForKey(<key>)` with a **constant** application key string — no window title, no version number, no timestamp.
- [ ] When `IsCurrent` is false, call `RedirectActivationToAsync(args)` using the documented **non-blocking** pattern (another thread plus an event, waited with non-blocking APIs) — or `async Task Main`, recording which was used and why. Never `await` it from a non-async STA `Main`.
- [ ] Confirm the redirected process creates **no window, no host, no view model and no log file beyond one redirect line** before terminating.
- [ ] Subscribe to `AppInstance.Activated` in `src/Pegasus.Desktop/App.xaml.cs` and forward the `AppActivationArguments` to the router, keeping the host build [[FND-032]] owns untouched.
- [ ] Create `src/Pegasus.Desktop/Services/IActivationRouter.cs` and its implementation against `Microsoft.Windows.AppLifecycle.AppActivationArguments` — **not** `Microsoft.UI.Xaml.LaunchActivatedEventArgs` — parsing deep-link and file arguments and calling `INavigationService`.
- [ ] Make an unrecognised argument **logged and ignored**, never a crash.
- [ ] Register `IActivationRouter` in `src/Pegasus.Desktop/Hosting/PegasusHost.cs`.
- [ ] Bring the existing window forward on redirect: restore if minimised and activate, using the supported `AppWindow` call confirmed via `winui-design` or `microsoft_docs_search` rather than a hand-rolled Win32 interop.
- [ ] Log every activation and redirect with the per-launch session identifier from [[FND-032]], written through [[FND-031]] (plan handle `DSK-02-06`)'s `IDiagnosticsWriter` so redaction applies, in a **stable** line format [[FND-036]] (plan handle `DSK-02-11`) can assert against.
- [ ] Write the routing tests in `tests/Pegasus.Desktop.ViewModelTests` ([[FND-038]], plan handle `DSK-02-13`): case deep link → case route with the right identifier; file activation → document route; unknown argument ignored **and** logged (assert both halves). Sequence [[FND-038]] first if it has not landed and record the sequencing.
- [ ] Launch the packaged app **twice** via `winapp run` — never the packaged `.exe` directly — and confirm exactly one window, a single Pegasus process in `Get-Process`, and the second launch's argument in the activation log.
- [ ] Record whether the two-launch evidence is a manual pass or a [[TEST-006]] (plan handle `DSK-08-06`) `single-instance` batch, naming [[TEST-006]] as the automation follow-up if manual.
- [ ] Confirm no multi-window capability was introduced — Phase 1 is single-window only (plan 02 § 3 decision 8).
- [ ] Add the one-line statement to `docs/current-architecture.md` § Failure and recovery boundaries (`:565`): the desktop is single-instance per Windows user and redirects activations to the running instance.
- [ ] Run the simplification pass over this branch's diff and record it under a dated `## Simplification pass` heading in the plan document.
- [ ] Verification run (this box produces `proof`, evidence tier 7): `dotnet build ./Pegasus.slnx --configuration Release` (exit 0, `0 Warning(s)` — the authoritative gate); `dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release --filter "FullyQualifiedName~Activation"`; the two-launch demonstration with its screenshot, `Get-Process` output and activation-log lines; and the step-2 re-confirmation record. Write the honesty clauses into the proof: **do not claim this prevents concurrent editing** — the invariant is server-side in `CaseMutationRequest`'s `ExpectedVersion` and `EditLeaseToken` (`src/Pegasus.Core/Workflow/CaseWorkflowContracts.cs:182-188`) and two machines can always edit the same case; state whether the second launch carried a deep-link argument or was a bare launch; record that instancing **across an App Installer upgrade was not exercised** and name [[FND-039]] (plan handle `DSK-02-14`) and area 08 as its owners; and note that `BuildAndRun.ps1` green ≠ `dotnet build` green.

## Progress notes

(append with `set_ticket_doc(doc: "checklist", append: true)`)
