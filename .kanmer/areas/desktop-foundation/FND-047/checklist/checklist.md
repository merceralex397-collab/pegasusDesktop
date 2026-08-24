# Checklist — FND-047

One box per plan step, in plan order. Tick with `set_ticket_doc` as you go;
append progress notes below rather than rewriting.

- [ ] Read `docs/desktop/04-auth-session-update-and-startup/README.md:214-231` (session failure matrix) and `docs/desktop/06-ui-design/screen-specs.md:41-86` (§ Shell)
- [ ] Call `get_doc_gates FND-047`, then `take_ticket`; create branch `task/<slug>` from `origin/dev` and its worktree under `../pegasus-worktrees/<slug>`
- [ ] Load `pegasus-desktop`, then `winui-design`
- [ ] Add `src/Pegasus.Desktop.Infrastructure/Connectivity/IConnectivityState.cs` exposing the `Connected | Disconnected` value, the last-successful-response timestamp and a change event
- [ ] Add `ConnectivityState.cs` with `TimeProvider` constructor-injected into a `readonly` field (pattern: `src/Pegasus.Core/Documents/RequestUploadPolicy.cs:341-343`); no `IClock` introduced
- [ ] Raise the change event only on an actual transition, never on a repeat of the same value
- [ ] Edit the existing `DelegatingHandler` from [[FND-031]] so any response object sets `Connected` and records the timestamp
- [ ] In the same handler, set `Disconnected` on `HttpRequestException`, transport `TaskCanceledException` and an `AuthenticationException` inner — and **re-throw** the exception
- [ ] Confirm no status code anywhere is classified as disconnected (`401`, `429`, `503` are reachable)
- [ ] Add `ConnectivityRecheckService.cs` whose loop starts on the transition to `Disconnected` and is cancelled on the transition to `Connected`
- [ ] Make the recheck call `GET /api/v1/client-compatibility` with **no bearer token** (`README.md:178-181`)
- [ ] Declare the interval once as `internal const int RecheckIntervalSeconds = 15;` on that class and reference it nowhere else
- [ ] Register `IConnectivityState` as a **singleton** and the recheck service in the host in `src/Pegasus.Desktop/App.xaml.cs`
- [ ] Bind the connected status-bar form: connection word plus last sync time in Europe/London
- [ ] Bind the disconnected form to the literal string "Disconnected — reconnecting", with the last-good sync time still shown beside it
- [ ] Set `AutomationProperties.AutomationId="Shell.Status.Connection"` on the status-bar connection element
- [ ] Confirm both forms are text plus glyph, never colour alone (`screen-specs.md:38`, `frd-12-operator-experience.md:112`)
- [ ] Marshal the change event onto the UI thread and unsubscribe when the shell window closes
- [ ] Add `src/Pegasus.Desktop/Commands/ConnectivityAwareCommand.cs` whose `CanExecute` is false while `Disconnected` and which raises `CanExecuteChanged` on the state's change event
- [ ] Route every authoritative save/command view model through that one behaviour; confirm no per-view-model connectivity `if` exists anywhere
- [ ] Confirm no queue, outbox or pending-command store was added (proposal § 11.3, ADR-0104)
- [ ] Confirm no page clears itself, no navigation is blocked and read-only content stays readable while disconnected
- [ ] Confirm sign-out and token clearing are **not** gated by `ConnectivityAwareCommand` and remain available
- [ ] Sweep every existing `src/Pegasus.Desktop` command path for a success reported without a server response; route any offender through the base behaviour; record the swept list for the post-implementation report
- [ ] Make the reconnecting indicator a thin indeterminate `ProgressBar` in the status bar; confirm no ring spinner, no full-page spinner, no animated transition
- [ ] Add the static "Working" text equivalent for `UISettings.AnimationsEnabled == false`, reading the value once on the UI thread at shell construction
- [ ] Test (a): a transport exception flips the state to `Disconnected` within one handler pass — `tests/Pegasus.Desktop.ViewModelTests/ConnectivityStateTests.cs`
- [ ] Test (b): save commands report `CanExecute == false` while disconnected — `ConnectivityCommandGatingTests.cs`
- [ ] Test (c): a successful recheck flips back to `Connected` and re-enables the commands, driven by advancing the fake `TimeProvider` past 15 s, and issues no further recheck afterwards
- [ ] Test (d): a transport failure surfaces the disconnected message and **never** an invalid-credentials message — assert on the surfaced message, not only the state
- [ ] Load `winui-ui-testing` and add the disconnected case to `tests/Pegasus.Desktop.UITests/ui-tests.ps1` as a `Test-UI` block, leaving the file's `param([Parameter(Mandatory)][int]$AppPid)` signature and `Test-UI` helper untouched
- [ ] In that case, stop the gateway with `pwsh ./scripts/Invoke-LocalDevelopment.ps1 -Action Stop` and assert `winapp ui wait-for "Shell.Status.Connection" -a $AppPid --value "Disconnected" --contains -t 20000`
- [ ] In that case, assert the save control reports disabled
- [ ] Add the reconnect case: `-Action Start`, then assert `Shell.Status.Connection` returns to the connected form without restarting the app
- [ ] Confirm `grep -rn "Start-Sleep" tests/Pegasus.Desktop.UITests` returns no matches
- [ ] Capture `winapp ui screenshot -a $AppPid -o "screenshots/connectivity-connected.png"` and `…-disconnected.png`
- [ ] Check keyboard reachability of the status area with `winapp ui get-focused` after tabbing to it, and record the result
- [ ] Run the simplification pass over this branch's diff (four lenses) and record it under a dated `## Simplification pass` heading in the plan document
- [ ] Verification run — `dotnet test tests/Pegasus.Desktop.ViewModelTests`, the UI script with the gateway stopped mid-run, the `Start-Sleep` grep and both screenshots; this box produces `proof`
- [ ] Open the PR into `dev`

## Progress notes

(append with `set_ticket_doc(doc: "checklist", append: true)`)
