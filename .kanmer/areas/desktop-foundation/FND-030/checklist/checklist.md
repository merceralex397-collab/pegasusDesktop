# Checklist — FND-030

One box per plan step, in plan order. Each is independently tickable: it names the file, value or
command whose completion makes the box true.

- [ ] Read `docs/desktop/02-architecture-and-foundation/README.md` § 3 decisions 2/3/9 and § 7, and `.codex/skills/winui-dev-workflow/SKILL.md` in full; run `get_doc_gates FND-030`; `take_ticket` on branch `task/desktop-scaffold` in worktree `../pegasus-worktrees/desktop-scaffold` from `origin/dev`.
- [ ] **Operator step**: run the `winui-setup` detection block and record the results — `dotnet --list-sdks`, `winapp --version` (≥ 0.3), `dotnet new list winui | Select-String 'winui-mvvm'`, and `AllowDevelopmentWithoutDevLicense -eq 1`. Install nothing yourself; stop and ask if anything is missing.
- [ ] Record `$env:PROCESSOR_ARCHITECTURE`; if `ARM64`, note that every `BuildAndRun.ps1` call in this ticket must add `/p:Platform=x64`.
- [ ] **Operator step**: confirm and record **verbatim** in the plan the permanent `Identity.Name` (plan 09 `:156` assumes `CollisionEngineers.Pegasus`) and the exact `Identity.Publisher` distinguished name, which under D-002 must equal the subject of the certificate [[REL-007]] (plan handle `DSK-09-08`) will issue.
- [ ] Run `dotnet new winui-mvvm -n Pegasus.Desktop -o src/Pegasus.Desktop` (no `mkdir` first); count the emitted files and correct the diff estimate in the plan.
- [ ] Edit `src/Pegasus.Desktop/Pegasus.Desktop.csproj` to `net10.0-windows10.0.26100.0`, `TargetPlatformMinVersion 10.0.22000.0`, `<Platforms>x64</Platforms>`, `RuntimeIdentifier win-x64`, `SelfContained=true`, `WindowsAppSDKSelfContained=true`, `PublishReadyToRun=false`, no `PublishTrimmed`, no `PublishAot`; confirm no `AnyCPU` and no `<WindowsPackageType>None</WindowsPackageType>`.
- [ ] Add `PackageVersion` entries to `Directory.Packages.props` for `Microsoft.WindowsAppSDK` (latest **stable** 2.x, ≥ 2.1.3 — re-confirm 2.4.0 / 2026-08-13 with `microsoft_docs_fetch`), `Microsoft.Windows.SDK.BuildTools`, `Microsoft.Windows.SDK.BuildTools.WinApp`, `Microsoft.WindowsAppSDK.Analyzers` and `CommunityToolkit.Mvvm`; record the chosen version and its release date in the plan.
- [ ] Strip every version literal the template wrote into `src/Pegasus.Desktop/Pegasus.Desktop.csproj`; if any package genuinely cannot move, record the exception with its reason rather than leaving it undocumented.
- [ ] Add an explicit `Microsoft.WindowsAppSDK.Analyzers` package reference to the desktop csproj, and record in the plan the measured `BuildAndRun.ps1:146-157` injection behaviour (it injects into the project directory and shadows the root `Directory.Build.props`).
- [ ] Set `Identity/@Name` and `Identity/@Publisher` in `src/Pegasus.Desktop/Package.appxmanifest` to the step-3 values, `Identity/@Version` to `0.1.0.0`, and both `DisplayName` fields to `Pegasus`.
- [ ] Add a comment in the csproj or a project `README` recording which manifest fields are permanent (`Name`, `Publisher`) and which are placeholders (`Version`, wired later by [[REL-002]], plan handle `DSK-09-02`).
- [ ] Add `<Project Path="src/Pegasus.Desktop/Pegasus.Desktop.csproj" />` to the `/src/` folder in `Pegasus.slnx`.
- [ ] Confirm the project is **not** added to the server entry point from [[FND-028]] (plan handle `DSK-02-03`); if [[FND-028]] has not landed, record in the proof that `dotnet build ./Pegasus.slnx` now fails on Linux until it does.
- [ ] Extend the ordinal expected array in `DependencyDirectionTests.ApplicationSolutionExcludesSourceWorkspaces` (`tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs:137-149`) with the desktop path, between the Core and Infrastructure entries.
- [ ] Run `dotnet restore ./src/Pegasus.Desktop/Pegasus.Desktop.csproj -r win-x64 --force-evaluate` and commit the generated `src/Pegasus.Desktop/packages.lock.json`.
- [ ] Run `pwsh .codex/skills/winui-dev-workflow/BuildAndRun.ps1 src/Pegasus.Desktop/Pegasus.Desktop.csproj -SkipRun` (synchronous) and confirm `BUILD SUCCEEDED`; note whether it printed `Analyzers: enabled` or `skipped (existing Directory.Build.props)`.
- [ ] Run `pwsh .codex/skills/winui-dev-workflow/BuildAndRun.ps1 src/Pegasus.Desktop/Pegasus.Desktop.csproj` **asynchronously**; confirm `✅ <pkg> launched (PID: …)` and a visible window; capture the screenshot for the proof.
- [ ] Run a plain `dotnet build ./src/Pegasus.Desktop/Pegasus.Desktop.csproj -c Release -p:Platform=x64` and compare its warning count with the script build's; record any difference as the props-shadowing effect.
- [ ] Add narrowly-scoped `<NoWarn>` entries to the desktop csproj, each with a comment naming the rule and why; confirm `Directory.Build.props` is unchanged.
- [ ] Measure and record the self-contained package/output size for [[REL-002]]'s release manifest (plan 02 § 7).
- [ ] Add the desktop client to `docs/current-architecture.md` § System shape (`:27`) and § Components and dependency direction (`:55`).
- [ ] Add the Windows-only desktop-build line to `docs/runbook.md` § Supported platform (`:19-40`) **only if** [[FND-039]] (plan handle `DSK-02-14`) has not already added the `winapp` CLI and Developer Mode prerequisites; record which case applied.
- [ ] Run the simplification pass over this branch's diff and record it under a dated `## Simplification pass` heading in the plan document.
- [ ] Verification run (this box produces `proof`): `dotnet restore ./Pegasus.slnx --locked-mode` (exit 0 on Windows); `dotnet build ./Pegasus.slnx --configuration Release --no-restore` (exit 0, `0 Warning(s)` — the authoritative gate, matching `.github/actions/dotnet-build/action.yml:22-27`); `dotnet test ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj --configuration Release` (all facts pass, including the extended solution list). Attach the launch screenshot, the package size, the resolved Windows App SDK version and date, and the verbatim `Identity.Name` / `Identity.Publisher`.

## Progress notes

(append with `set_ticket_doc(doc: "checklist", append: true)`)
