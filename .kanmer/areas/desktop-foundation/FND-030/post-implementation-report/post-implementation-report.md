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

## Exact-head correction 4 — 2026-08-28

- Removed the explicit PublishTrimmed property from the desktop project.
- Windows App SDK's transitive WebView2 runtime payload was verified from the package targets and excluded only for this scaffold, which has no WebView2 caller. After a clean generated-output build, the package output scan found no Microsoft.Web.WebView2.Core.dll, Microsoft.Web.WebView2.Core.Projection.dll, Microsoft.Web.WebView2.Core.winmd, or WebView2Loader.dll. The lock graph remains transparently transitive; no ADR-0108 renderer use is claimed.
- BuildAndRun.ps1 was rerun from the corrected task worktree with 0 warnings and 0 errors; it launched package identity CollisionEngineers.Pegasus_e6z0b4cw4baw0 as PID 38852, with a responding Pegasus.Desktop window. The screenshot is now retained as the committed artifact artifacts/fnd-030/desktop-launch-desktop.png and as ticket asset documentation assets/desktop-launch.md (SHA-256 A822FC53563317FA4851096FC3CC640483A2A96A5B8838DB74660417277F79B1). The process was closed cleanly.
- The exact branch inventory is 10 Pegasus.slnx projects; the measured current branch diff is 29 paths with 649 text insertions and 3 deletions plus generated/binary assets. The prior estimate is superseded.
- Exact-head CI run 33202445712 was for the preceding head 93ff2663; a new run is required for 1c651eb4 before review/merge.

## Exact-head correction 5 — 2026-08-28

Final implementation head: d2fe4bd08f8e63c3655097e4f850a0f086072176.

- Removed the explicit PublishTrimmed property.
- Replaced the earlier custom output-filter target with the smaller NuGet-supported asset exclusion: Microsoft.Web.WebView2 1.0.3719.77 is centrally pinned and referenced with ExcludeAssets=all and PrivateAssets=all. Windows App SDK remains usable, but no WebView2 compile/runtime/native/content/build asset is emitted by this scaffold.
- dotnet restore ./Pegasus.slnx --locked-mode — passed.
- dotnet build ./Pegasus.slnx --configuration Release --no-restore -nr:false -p:UseSharedCompilation=false -p:BuildInParallel=false -p:NodeReuse=false — passed, 0 warnings/errors.
- dotnet test ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj --configuration Release --no-build -nr:false -p:UseSharedCompilation=false -p:BuildInParallel=false -p:NodeReuse=false — passed 111/111.
- BuildAndRun.ps1 -SkipRun — passed, 0 warnings/errors. BuildAndRun.ps1 packaged launch — passed; package identity CollisionEngineers.Pegasus_e6z0b4cw4baw0 launched PID 3576, with a responding Pegasus.Desktop window. The post-winapp-run output scan found no WebView2/loader payload. The process was closed cleanly.
- Durable visual evidence: artifacts/fnd-030/desktop-launch-desktop.png, SHA-256 C7838380DE4EACF053DFB0C9F7969F529DFAB76B11A8E4D1F5E1D5970A9B6159, indexed by ticket asset documentation assets/desktop-launch.md.
- Exact solution inventory is 10 projects. Final branch diff at d2fe4bd0 is 29 paths, 649 text insertions and 3 deletions plus generated/binary assets. Filtered Release output is 519 files / 234279635 bytes (223.45 MiB).
- The WebView2 package remains visible in packages.lock.json because it is required transitively by Microsoft.WindowsAppSDK.WinUI; this is transparent dependency evidence, not a renderer implementation or deployment claim.
- The new exact-head CI run and fresh independent review are required before merge.
