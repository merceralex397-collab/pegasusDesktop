# Files — FND-030

Surveyed 2026-08-24 against fork `main`. Every existing path below was confirmed with `ls`/`sed`;
new files are marked.

## Where the change lands

| Path | Why |
| --- | --- |
| `src/Pegasus.Desktop/**` | **New**, created by `dotnet new winui-mvvm -n Pegasus.Desktop -o src/Pegasus.Desktop`. The template folder is created by `-n`; do **not** `mkdir` first (`.codex/skills/winui-dev-workflow/SKILL.md` § Create or Open a Project). Nothing may be deleted from it except sample pages that are replaced, and **never** `Package.appxmanifest`. |
| `src/Pegasus.Desktop/Pegasus.Desktop.csproj` | **New (template) then edited.** Must end at `net10.0-windows10.0.26100.0`, `TargetPlatformMinVersion 10.0.22000.0`, `<Platforms>x64</Platforms>`, `RuntimeIdentifier win-x64`, `SelfContained=true`, `WindowsAppSDKSelfContained=true`, `PublishReadyToRun=false`, no `PublishTrimmed`, no `PublishAot`, an explicit `Microsoft.WindowsAppSDK.Analyzers` reference, no version literals, and narrow commented `NoWarn` entries. Never `AnyCPU`; never `<WindowsPackageType>None</WindowsPackageType>`. |
| `src/Pegasus.Desktop/Package.appxmanifest` | **New (template) then edited.** `Identity/@Name` and `Identity/@Publisher` to the operator-confirmed permanent values; `Identity/@Version` to `0.1.0.0` as a placeholder ([[REL-002]], plan handle `DSK-09-02`, wires the CI version); `Properties/DisplayName` and `Application/uap:VisualElements/@DisplayName` to `Pegasus`. Which fields are permanent and which are placeholders must be documented in the csproj or a project `README` comment. |
| `src/Pegasus.Desktop/packages.lock.json` | **New, generated** by `dotnet restore … -r win-x64 --force-evaluate` and committed. Unlike `src/Pegasus.Core/packages.lock.json` (124 bytes, three empty entries) this one is large and RID-specific — plan 02 § 7 records that as a trap: "CI must restore with the same RID". |
| `Directory.Packages.props` (created by [[FND-027]], plan handle `DSK-02-02`) | Add `PackageVersion` entries for `Microsoft.WindowsAppSDK` (the chosen stable 2.x, ≥ 2.1.3), `Microsoft.Windows.SDK.BuildTools`, `Microsoft.Windows.SDK.BuildTools.WinApp`, `Microsoft.WindowsAppSDK.Analyzers` and `CommunityToolkit.Mvvm`. Confirmed absent today (`ls Directory.Packages.props` → *No such file*), so if [[FND-027]] has not landed this ticket cannot pin centrally and must record the sequencing rather than inlining versions permanently. |
| `Pegasus.slnx` | 14 lines. Add `<Project Path="src/Pegasus.Desktop/Pegasus.Desktop.csproj" />` under the `/src/` folder. |
| `tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs` | Extend the ordinal expected array at `:137-149` with `src/Pegasus.Desktop/Pegasus.Desktop.csproj` (it sorts between the Core and Infrastructure entries). No other change; the desktop dependency **rules** are [[FND-037]] (plan handle `DSK-02-12`). |
| `docs/current-architecture.md` | 682 lines. § System shape (`:27`) and § Components and dependency direction (`:55`) each gain the desktop client. |
| `docs/runbook.md` | 1254 lines. § Supported platform (`:19-40`) gains one line that the desktop build requires Windows — **only if** [[FND-039]] (plan handle `DSK-02-14`) has not already added the `winapp` CLI and Developer Mode prerequisites there. Record which case applied. |

**Not a file, but part of the change:** `Pegasus.Server.slnf` (created by [[FND-028]], plan handle
`DSK-02-03`) must **not** gain this project. That omission is the point — it is what
[[FND-028]]'s `ServerSolutionFilterExcludesWindowsTargetedProjects` fact asserts.

## Context files

What the implementer must **read** and what each one tells them.

| Path | What it tells the implementer |
| --- | --- |
| `.codex/skills/winui-dev-workflow/SKILL.md` § Critical Rules, § Build & Run, § Common Errors | Four rules that decide whether this ticket succeeds: scaffold with `dotnet new winui-mvvm -n <AppName>` and never `mkdir` first; invoke `BuildAndRun.ps1` **async** because it stays attached and success is the line `✅ <pkg> launched (PID: …)`; never run the packaged `.exe` directly ("App silently exits → use `winapp run`"); and `MSB3073 / XamlCompiler.exe … exited with code 1` naming no `.xaml` means raise the `Microsoft.WindowsAppSDK` pin (≥ 2.1.3 on the 2.x line). |
| `.codex/skills/winui-dev-workflow/BuildAndRun.ps1:142-172` | **The most important file in this list, and it does not behave as plan 02 § 7 describes.** `:146-149` builds `$tempBuildProps` from the **project directory only**; `:152-157` writes a `Directory.Build.props` there when that exact file is absent — it does not look up the tree. The repository-root props file therefore does **not** prevent injection, and because MSBuild stops at the first `Directory.Build.props` it finds walking up, the injected file **shadows** the root one for that build, dropping `TreatWarningsAsErrors`, `Nullable`, `ImplicitUsings`, `LangVersion` and `Version`. `:198-205` deletes only the file the script itself created. Consequence: a green `BuildAndRun.ps1` build is a weaker gate than a plain `dotnet build`, and the explicit analyzer reference (step 7) is needed for a stronger reason than the body states. |
| `.codex/skills/winui-dev-workflow/BuildAndRun.ps1:89-101`, `:223-228` | Platform auto-detection: ARM64 if `$env:PROCESSOR_ARCHITECTURE -eq "ARM64"`, else x64, appended as `/p:Platform=` unless the caller supplied one; output is expected at `bin\<Platform>\<Config>\<tfm>\win-<rid>\`. On an ARM64 workstation this collides with `<Platforms>x64</Platforms>` — pass `/p:Platform=x64` rather than widening `<Platforms>`. |
| `.codex/skills/winui-dev-workflow/analyzer/` | Holds the vendored `Microsoft.WindowsAppSDK.Analyzers.dll` and `.targets`. `BuildAndRun.ps1:133-138` falls back to a `tools/winui-analyzer/…` path that does **not** exist here, so the vendored pair is the only resolvable source — which is why the csproj must reference the analyzer package itself rather than rely on the script. |
| `.codex/skills/winui-setup/SKILL.md` (frontmatter `disable-model-invocation: true`) | The exact detection block for step 2 — `dotnet --list-sdks`, `winapp --version` (≥ 0.3 after stripping `-prerelease.N`), `dotnet new list winui \| Select-String 'winui-mvvm'`, and the registry read `HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock` → `AllowDevelopmentWithoutDevLicense -eq 1`. It also says to upgrade the WinApp CLI and templates even when present. The skill is user-invoked, and enabling Developer Mode needs a UAC elevation only the operator can accept. |
| `global.json` | `10.0.302`, `rollForward: latestFeature`, **`allowPrerelease: false`**. The last flag is why only a *stable* Windows App SDK 2.x may be pinned, and why a preview that demands a preview .NET SDK is a separate ticket rather than an edit here. |
| `Directory.Build.props` (19 lines) | What the desktop project inherits, and what the injected props file would shadow: `TreatWarningsAsErrors=true`, `AnalysisLevel=latest-recommended`, `Nullable`, `ImplicitUsings`, `LangVersion=latest`, `Deterministic=true`, `Version=0.1.0-alpha.1`. Also, from its `:8-19` comment, that this file is already a single-source-of-truth mechanism (`PlaywrightVersion`) — the precedent for putting the Windows App SDK pin in a props file rather than a csproj. |
| `src/Pegasus.Core/packages.lock.json` | What a *small* lock file looks like, for contrast: the desktop one will be large and RID-specific, and it must be committed or `dotnet restore ./Pegasus.slnx --locked-mode` fails on every CI lane. |
| `tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs:128-154` | The fact whose array must be extended, and the exact idiom: `XDocument.Load`, `.Order(StringComparer.Ordinal)`, `Assert.Equal` against a literal array. The ordering is why the desktop entry goes between Core and Infrastructure, not at the end. |
| `.github/actions/dotnet-build/action.yml:14-27` | CI restores `./Pegasus.slnx --locked-mode` and builds it Release, with the SDK cache keyed on `global.json`, `src/**/packages.lock.json` and `tests/**/packages.lock.json`. This is the command whose zero-warning result is the authoritative gate — not `BuildAndRun.ps1`. |
| `.github/workflows/ci.yml` (nine jobs; `unit` at `:136`) | That no CI job builds a desktop project today, so nothing in CI will catch a desktop regression until [[FND-040]] (plan handle `DSK-02-15`) adds the lane. The architecture test in `unit` is the only automatic guard this ticket gains. |
| `scripts/email-eval-desktop/Pegasus.EmailEvaluation.Desktop.csproj` (19 lines) | The repository's only existing desktop project and the shape **not** to copy: `net10.0-windows`, `UseWindowsForms=true`, referencing `Pegasus.Core` **and `Pegasus.Infrastructure`**. It is outside `Pegasus.slnx` under ADR-0016 (`docs/adr/0016-standalone-desktop-email-evaluator.md`) and stays there. It also tells you `docs/runbook.md` already records a Windows-only project, so the § Supported platform sentence has a precedent to sit beside. |
| `docs/desktop/09-release-update-and-distribution/README.md:156-158`, `:216`, `:289`, `:329` | The identity chain: `CollisionEngineers.Pegasus` as the plan's assumed `Identity.Name`, one identity for both channels; the certificate subject "fixed to the manifest `Publisher`" with ~3-year validity; [[REL-007]] (plan handle `DSK-09-08`) as the ticket that issues it; and "Publisher mismatch between certificate and `Identity.Publisher`" recorded as a trap. This is why step 3 is an operator step and not a default. |
| `docs/desktop/04-auth-session-update-and-startup/README.md` § 3 items 5 and 8 | That the package carries "none" in the way of secrets, and that the minimum-version gate is a database-backed Administrator setting rather than a Container App app setting — so nothing about package identity implies an Azure write. |
| `AGENTS.md` § Product invariants (`:235`) | A new top-level project needs an accepted ADR proving the existing boundary cannot carry it — the reason this ticket depends on [[FND-026]] (plan handle `DSK-02-01`) authoring ADR-0100 rather than simply creating the project. |

## Ripple effects

- **Architecture test.** `ApplicationSolutionExcludesSourceWorkspaces` fails the moment `Pegasus.slnx`
  gains the project and the array is not extended — the intended coupling. It runs unfiltered in the
  CI `unit` lane (`.github/workflows/ci.yml:136-148`), so the break is caught on the PR.
- **Restore graph.** `dotnet restore ./Pegasus.slnx --locked-mode` in the composite action fails on
  **every** lane if `src/Pegasus.Desktop/packages.lock.json` is missing or stale, because the cache
  key already globs `src/**/packages.lock.json`. It must be generated with `-r win-x64 --force-evaluate`
  and committed.
- **Linux builds.** Adding a `net10.0-windows10.0.26100.0` project to `Pegasus.slnx` is exactly what
  breaks `dotnet build ./Pegasus.slnx` on Linux. [[FND-028]] (plan handle `DSK-02-03`) is the
  mitigation and is a hard prerequisite in practice even though the plan's dependency arrow names only
  [[FND-026]] and [[FND-027]]; if [[FND-028]] has not landed, this ticket makes the repository
  Windows-only for developers and must say so in its proof.
- **Downstream tickets unblocked.** [[FND-031]] (plan handle `DSK-02-06`), [[FND-032]] (plan handle
  `DSK-02-07`), [[FND-037]], [[FND-039]], [[FND-041]] (plan handle `DSK-02-16`), plus [[DUI-001]]
  (plan handle `DSK-06-01`), [[TEST-004]], [[PLAT-012]], [[TOOL-009]] and [[FEAT-040]] all name this
  project. Every one of them assumes the csproj properties fixed here.
- **Release manifest.** The measured self-contained MSIX/package size is owed to [[REL-002]] (plan
  handle `DSK-09-02`) per plan 02 § 7 ("acceptable for ten users but measure and record"). Capture it
  in the proof or it will be re-measured later.
- **Certificate work.** [[REL-007]] (plan handle `DSK-09-08`) cannot issue the production certificate
  until the `Identity.Publisher` string exists. Recording it verbatim in this ticket's plan is what
  unblocks that.
- **Documentation.** `docs/current-architecture.md` and possibly `docs/runbook.md` change;
  `scripts/Test-DocumentationLinks.ps1` runs in the CI `documentation` lane
  (`.github/workflows/ci.yml:76-87`).

## Out of scope

Recorded so the reviewer sees each was a decision, matching the ticket's Guardrails.

- **`src/Pegasus.Core`, `src/Pegasus.Infrastructure`, `src/Pegasus.Web`, `src/Pegasus.Worker`** — not
  touched.
- **`scripts/email-eval-desktop/`** — not touched and not added to the solution; ADR-0016 keeps it
  outside.
- **`global.json`** — not edited. If the Windows App SDK genuinely needs a newer SDK feature band,
  that is its own ticket (plan 02 § 2 assumption A1).
- **The shell** — not built here. [[FND-033]] (plan handle `DSK-02-08`) owns it, and it must be a
  `NavigationView`, not a port of `src/Pegasus.Web/Pages/Shared/_Layout.cshtml`.
- **Any WebView2 reference** — refused. The only permitted WebView2 use is the isolated report
  renderer under ADR-0108, which is area 07.
- **Theme dictionaries, host composition, single instance, diagnostics** — [[FND-034]] (plan handle
  `DSK-02-09`), [[FND-032]], [[FND-035]] (plan handle `DSK-02-10`) and [[FND-036]] (plan handle
  `DSK-02-11`) respectively.
- **Desktop dependency-direction rules and the no-WebView test** — [[FND-037]]; this ticket only
  extends the *solution contents* array.
- **`.github/workflows/ci.yml`** — untouched; [[FND-040]] owns the `desktop-build` lane.
- **The `docs/runbook.md` § Supported platform prerequisite sentence** — owned by [[FND-039]]; cited
  here, never restated a second time.
- **Trimming, AOT and ReadyToRun** — explicitly off (proposal § 7.1 defers them until profiled).

## Operator confirmation — 2026-08-28

The prior handback is resolved. The operator confirmed verbatim:

- `Identity/@Name`: `CollisionEngineers.Pegasus`
- `Identity/@Publisher`: `CN=Collision Engineers`

Project-file creation may proceed with these exact values.
