# Post-implementation report — FND-035

## Result

FND-035 implementation is present on branch `task/desktop-single-instance` at `18493d485f8eab5c9d1fd8c63af9b478d54e04d`, based on current `origin/dev` `d278de7ba0fd...`. The change adds the explicit WinUI entry point, constant per-user AppInstance registration, pre-window activation redirection, activation routing/logging, host registration, and focused routing tests. The worktree is clean.

## Acceptance validation

- `Program.Main` initializes COM, reads activated arguments, registers the constant key `pegasus-desktop-single-instance`, redirects a non-current instance before `Application.Start`, and awaits `RedirectActivationToAsync` without blocking the STA.
- The owning instance subscribes to `AppInstance.Activated`, queues pre-dispatch activations, routes on the UI dispatcher, and brings the existing window forward without Win32 interop.
- `ActivationRouter` maps supported `pegasus://case/<id>`, `pegasus://document/<id>`, launch, and file activations; unknown input is logged and ignored.
- The session identifier is passed to the diagnostics logger and redirect records contain a stable redacted argument hash.
- The instance key is constant and no multi-window capability was added.
- `INavigationService` is a single temporary shared contract for FND-033 to reuse; FND-033 owns its concrete shell navigation implementation. No duplicate navigation contract is permitted.

## Validation

- `dotnet test tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~Activation" --logger "console;verbosity=minimal" -nr:false -p:UseSharedCompilation=false` — 3 passed, 0 failed, 0 skipped.
- `dotnet build src/Pegasus.Desktop/Pegasus.Desktop.csproj --configuration Release --no-restore -nr:false -p:UseSharedCompilation=false` — passed, 0 warnings, 0 errors.
- `git diff --check` — passed.
- The initial implementation’s STA `ManualResetEventSlim.Wait()` was removed in `18493d4` after parent review; the final path uses `async Task Main` and `await`.

## Evidence boundary

The authoritative solution-wide build and the packaged two-launch `winapp run` demonstration remain pending. The real two-launch proof cannot be completed on this branch until FND-033 supplies the concrete `INavigationService` registration; claiming it now would be false. When that dependency lands, the remaining evidence must capture exactly one window/process, the redirected argument in the activation log, and whether the proof is manual or the TEST-006 batch. App Installer upgrade instancing remains FND-039/area-08 scope. No PR or merge is claimed by this report.

## Validation update — 2026-08-30

After generating the worktree assets with `dotnet restore ./Pegasus.slnx --locked-mode` (passed), the authoritative solution build was rerun:

- `dotnet build ./Pegasus.slnx --configuration Release --no-restore -nr:false -p:UseSharedCompilation=false` — passed, 0 warnings, 0 errors.

The remaining evidence boundary is unchanged: no packaged `winapp run` two-launch proof is claimed, and the concrete `INavigationService` registration from FND-033 is still required before that proof can be meaningful.
