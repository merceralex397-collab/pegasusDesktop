# Post-implementation report — FND-030

## Result

Implemented the packaged WinUI 3 scaffold in the existing `task/desktop-scaffold` worktree using the operator-confirmed permanent identity:

- `Identity/@Name`: `CollisionEngineers.Pegasus`
- `Identity/@Publisher`: `CN=Collision Engineers`

## Changed scope

- Created `src/Pegasus.Desktop/**` from the `winui-mvvm` template.
- Added the four desktop package pins to `Directory.Packages.props`.
- Added the project to `Pegasus.slnx` and the application-solution architecture expectation.
- Added the desktop client and dependency boundary to `docs/current-architecture.md`.
- Recorded the fixed identity and Windows App SDK pin in `docs/desktop/02-architecture-and-foundation/README.md`.
- Did not modify server, Core, Worker, upstream, cloud, deployment, certificate, or runbook-owner files.

## Validation

- Toolchain detection: .NET SDK 10.0.204/10.0.303, WinApp CLI 0.3.1, `winui-mvvm` template present, Developer Mode registry value 1.
- `dotnet restore ./src/Pegasus.Desktop/Pegasus.Desktop.csproj -r win-x64 --force-evaluate --verbosity normal` — passed, 0 warnings/errors; generated the lock file.
- `pwsh .codex/skills/winui-dev-workflow/BuildAndRun.ps1 src/Pegasus.Desktop/Pegasus.Desktop.csproj -SkipRun` — passed, 0 warnings/errors.
- `pwsh .codex/skills/winui-dev-workflow/BuildAndRun.ps1 src/Pegasus.Desktop/Pegasus.Desktop.csproj` — passed; package identity launched as `CollisionEngineers.Pegasus_e6z0b4cw4baw`, PID 104480. Screenshot: `artifacts/fnd-030/desktop-launch.png`.
- `dotnet restore ./Pegasus.slnx --locked-mode` — passed.
- `dotnet build ./Pegasus.slnx --configuration Release --no-restore -nr:false -p:UseSharedCompilation=false -p:BuildInParallel=false -p:NodeReuse=false` — passed, 0 warnings/errors.
- `dotnet test ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj --configuration Release --no-build -nr:false -p:UseSharedCompilation=false -p:BuildInParallel=false -p:NodeReuse=false --verbosity minimal` — passed, 111/111.
- Direct desktop Release build with the same no-shared-compilation flags — passed, 0 warnings/errors.
- Manifest parse and invariant scan — identity values correct; no package version literals, AnyCPU, WindowsPackageType=None, PublishAot, or enabled PublishTrimmed in the desktop project.
- Release self-contained output measured at 224.99 MiB (522 files).

## Evidence limits

This proves the scaffold builds and launches with package identity on this workstation. It does not prove the future desktop shell, gateway capabilities, clean-machine installation, signing, CI desktop lane, or production deployment; those remain owned by their separate tickets.

## Independent review response — 2026-08-28

Applied the review corrections at the new task-branch head:

- Added unique automation IDs and accessible names to the increment and decrement buttons.
- Corrected the current-architecture wording to distinguish the launchable scaffold from the future gateway caller.
- Removed manifest trailing whitespace.
- Rebuilt the changed desktop project and full solution successfully with 0 warnings/errors; architecture tests remain 111/111.
- Updated the checklist and plan with honest package/analyzer, NoWarn, runbook, payload, and simplification dispositions.

The exact-head GitHub Actions run for this new commit and merged-main proof are still required; no proof document is being fabricated before merge.

## Independent review correction 2 — 2026-08-28

At exact task head `93ff2663364b05293f25832c6aa7fd5b10c90687`, `BuildAndRun.ps1` rebuilt with 0 warnings/errors and launched package identity `CollisionEngineers.Pegasus_e6z0b4cw4baw0` as PID `119016`; the responsive window was captured at `artifacts/fnd-030/desktop-launch-final.png` and closed cleanly. The architecture diagram now marks the absent scaffold project references as planned dependencies. The exact-head CI run is `33202445712`; fresh independent review remains required before merge.

## Independent review correction 3 — 2026-08-28

The exact `93ff2663` task head was relaunched with `BuildAndRun.ps1`: build passed with 0 warnings/errors, package identity `CollisionEngineers.Pegasus_e6z0b4cw4baw0` launched PID `119016`, the responsive window was captured at `artifacts/fnd-030/desktop-launch-final.png`, and the process was closed cleanly. The current-architecture diagram truthfully marks the absent project references as planned dependencies. Kanmer now records the exact commit sequence through `93ff2663`; exact-head CI run `33202445712` and fresh review remain required.
