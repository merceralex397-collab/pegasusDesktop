# Checklist — FND-030

One box per plan step, in plan order. Each names the file or command whose completion makes it true,
so it can be ticked independently and honestly.

- [x] Read plan 02 § 3 decisions 2/3/9, § 4 target-state table and § 7; read `.codex/skills/winui-dev-workflow/SKILL.md` in full; read this ticket's `research` and `files` documents. Run `get_doc_gates FND-030`, then `take_ticket` on branch `task/desktop-scaffold` in worktree `../pegasus-worktrees/desktop-scaffold` from `origin/dev`.
- [x] Confirm ADR-0100 is accepted (`ls docs/adr/0100-*.md` plus its frontmatter status) — [[FND-026]] (plan handle `DSK-02-01`) owns it; without it `AGENTS.md` § Product invariants forbids the new top-level project.
- [x] Confirm `Directory.Packages.props` exists (`ls Directory.Packages.props` — absent today; [[FND-027]] (plan handle `DSK-02-02`) creates it). If absent, stop and record the sequencing instead of inlining version literals.
- [x] Add the four available desktop package pins to `Directory.Packages.props` (`Microsoft.WindowsAppSDK` 2.4.0, `Microsoft.Windows.SDK.BuildTools`, `Microsoft.Windows.SDK.BuildTools.WinApp`, and `CommunityToolkit.Mvvm`), strip every `Version=` literal from the desktop csproj, and record the chosen version and its release date in the plan. `dotnet package search Microsoft.WindowsAppSDK.Analyzers` returned no package, so no fabricated fifth pin was added.
- [x] Reference the vendored `Microsoft.WindowsAppSDK.Analyzers` DLL explicitly in `src/Pegasus.Desktop/Pegasus.Desktop.csproj`; the project-local props imports its XAML target so plain `dotnet build` preserves the diagnostics.
- [x] **Operator step** — obtain and record verbatim in the plan, under a dated heading, the permanent `Identity/@Name` (plan 09 assumes `CollisionEngineers.Pegasus`) and the exact `Identity/@Publisher` distinguished name that D-002's certificate subject must equal.
- [x] Run `dotnet new winui-mvvm -n Pegasus.Desktop -o src/Pegasus.Desktop` without `mkdir`-ing first; delete nothing from the output except sample pages actually replaced, and never `Package.appxmanifest`. Add no `ProjectReference`.
- [x] Edit `src/Pegasus.Desktop/Pegasus.Desktop.csproj` to `TargetFramework net10.0-windows10.0.26100.0`, `TargetPlatformMinVersion 10.0.22000.0`, `<Platforms>x64</Platforms>`, `RuntimeIdentifier win-x64`, `SelfContained=true`, `WindowsAppSDKSelfContained=true`, `PublishReadyToRun=false`, no `PublishTrimmed`, no `PublishAot` — and never `AnyCPU` or `<WindowsPackageType>None</WindowsPackageType>`.
- [x] Confirmed the four available desktop package pins in `Directory.Packages.props` (`Microsoft.WindowsAppSDK` 2.4.0, `Microsoft.Windows.SDK.BuildTools` 10.0.26100.7705, `Microsoft.Windows.SDK.BuildTools.WinApp` 0.3.1, and `CommunityToolkit.Mvvm` 8.4.2); the analyzer search returned no NuGet package, so no fabricated fifth pin was added.
- [x] Wired the vendored `Microsoft.WindowsAppSDK.Analyzers.dll` explicitly in `src/Pegasus.Desktop/Pegasus.Desktop.csproj` and imported its XAML target from the project-local `Directory.Build.props`; no unavailable NuGet package was invented.
- [x] The desktop project builds with 0 warnings under the repository-wide `TreatWarningsAsErrors` policy; no `NoWarn` suppression was necessary, and `Directory.Build.props` was not relaxed.
- [x] Add `<Project Path="src/Pegasus.Desktop/Pegasus.Desktop.csproj" />` to the `/src/` folder of `Pegasus.slnx`, and confirm the project is **not** added to [[FND-028]]'s server entry point.
- [x] Confirmed `FND-039` is still in Preparing and owns the Windows `winapp`/Developer Mode runbook prerequisite; no duplicate sentence was added here.
- [x] Run `dotnet restore ./src/Pegasus.Desktop/Pegasus.Desktop.csproj -r win-x64 --force-evaluate` and commit the generated `src/Pegasus.Desktop/packages.lock.json`.
- [x] Run `pwsh .codex/skills/winui-dev-workflow/BuildAndRun.ps1 src/Pegasus.Desktop/Pegasus.Desktop.csproj -SkipRun`, then the same command async without `-SkipRun`; confirm the line `✅ <pkg> launched (PID: …)` and a visible window, and capture the screenshot. Pass `/p:Platform=x64` if `$env:PROCESSOR_ARCHITECTURE` is `ARM64`.
- [x] No `<NoWarn>` entries were needed: the desktop project and full solution build report `0 Warning(s)` under the unchanged repository warning policy.
- [x] Added the desktop scaffold to `docs/current-architecture.md` § System shape and § Components and dependency direction; the wording identifies the future gateway caller as not yet implemented.
- [x] Recorded that [[FND-039]] remains the owner of the `winapp`/Developer Mode prerequisite sentence, so FND-030 adds no duplicate runbook text.
- [x] Measured and recorded the self-contained Release payload for [[REL-002]]: 522 files, 235,921,659 bytes (224.99 MiB); this is a build payload, not a signed MSIX.
- [x] Ran and recorded the simplification pass under the dated `## Simplification pass — 2026-08-28` heading in the plan; the independent review correction adds the four required lenses and dispositions.
- [x] Verification run (this box produces `proof`, evidence tier 1): `dotnet restore ./Pegasus.slnx --locked-mode` (exit 0); `dotnet build ./Pegasus.slnx --configuration Release --no-restore` (exit 0, `0 Warning(s)` — the authoritative gate); the two `BuildAndRun.ps1` invocations with the launch line and screenshot; `dotnet test ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj --configuration Release` (all facts green with the eight-path array). Write the three honesty clauses into the proof: `BuildAndRun.ps1` green ≠ `dotnet build` green; no CI job builds the desktop until [[FND-040]] (plan handle `DSK-02-15`) lands; and whether Linux is now broken because [[FND-028]] has not landed.

## Progress notes

(append with `set_ticket_doc(doc: "checklist", append: true)`)
