# Plan — FND-030: Scaffold `src/Pegasus.Desktop` (WinUI 3, x64, packaged, self-contained, pinned Windows App SDK 2.x)

**Diff estimate: ~24 files, ~600 lines** — of which roughly 380 are template-generated and ~220 are
authored or edited.

`docs/engineering.md` § Plan sizing (`:201`) requires the estimate first. Derived from the files
document: the `winui-mvvm` template emits on the order of 18–20 files (csproj, `App.xaml`/`App.xaml.cs`,
`MainWindow.xaml`/`.cs`, a sample page and view model, `Package.appxmanifest`, `app.manifest`,
`Assets/*`, launch settings) — count them at step 4 and correct this line before the PR rather than
carrying an estimate the diff disproves. Authored on top: csproj edits ~40 lines,
`Package.appxmanifest` edits ~6, `Directory.Packages.props` +5 `PackageVersion` lines,
`Pegasus.slnx` +1, `DependencyDirectionTests.cs` +1, `docs/current-architecture.md` ~+6,
`docs/runbook.md` ~+3, plus a generated and committed `src/Pegasus.Desktop/packages.lock.json`
(large — hundreds of lines, RID-specific to `win-x64`; it dominates the raw line count and should be
reported separately in the PR description so the reviewable diff is visible).

## Approach

Scaffold from the vendored `winui-mvvm` template and then **retarget** it, rather than hand-writing a
csproj. The template is what `.codex/skills/winui-dev-workflow/SKILL.md` prescribes, it already pairs
CommunityToolkit.Mvvm with a `TitleBar`, `MicaBackdrop` and `Frame` navigation, and its
`Package.appxmanifest` and asset set are the parts most easily got wrong by hand. The rejected
alternative is `winapp init` over an empty `Microsoft.NET.Sdk` project: it produces the same
`net10.0-windows10.0.26100.0` target and the same three build-tools packages
(<https://learn.microsoft.com/windows/apps/dev-tools/winapp-cli/guides/dotnet>) but no MVVM wiring, so
[[FND-032]] (plan handle `DSK-02-07`) and [[FND-033]] (plan handle `DSK-02-08`) would each rebuild
what the template gives free — and `docs/engineering.md` § Abstractions forbids that kind of
hand-rolled scaffolding when a supported one exists.

Two things this plan does differently from a naive reading of the ticket, both from measurement:

- **The authoritative build gate is `dotnet build ./Pegasus.slnx --configuration Release`, not
  `BuildAndRun.ps1`.** Measured at `.codex/skills/winui-dev-workflow/BuildAndRun.ps1:146-157`, the
  script tests for `Directory.Build.props` **in the project directory only** and writes one there when
  it is absent. MSBuild stops at the first such file walking up, so the injected file shadows the
  repository-root props and that build runs *without* `TreatWarningsAsErrors=true`. The script is
  therefore the right tool for **launching** and the wrong tool for proving zero warnings.
- **The `Identity.Publisher` string is treated as the ticket's highest-consequence output.** It is
  permanent, the production certificate's subject must equal it exactly (D-002; plan 09 `:216`,
  `:329`), and [[REL-007]] (plan handle `DSK-09-08`) is blocked until it exists in writing.

## Governing docs

The ticket's `refs` array is empty and `get_doc_gates FND-030` reports `docs_todo: true`, so there is
no linked PRD/FRD/ADR to meet today.

> **New ADR** — ADR-0100 (native WinUI 3 / Windows 11 desktop client converted inside this fork, no
> WebView shell; it is what authorises this new top-level project under `AGENTS.md` § Product
> invariants), authored by [[FND-026]] (plan handle `DSK-02-01`); [[FND-005]] (plan handle
> `DSK-00-05`) also claims ADR-0100 in the reserved block — see [[FND-026]]'s plan for the ownership
> reconciliation. ADR-0105 (signed MSIX/App Installer distribution with a gateway minimum-version
> gate) is likewise claimed by [[REL-001]] (plan handle `DSK-09-01`), [[FND-005]] and [[FND-042]]
> (plan handle `DSK-04-01`) — see [[REL-001]]'s plan for that reconciliation.
> This plan is written to the decisions as recorded in
> `docs/desktop/00-governance-and-workflow/README.md` § 3 (ADR set table) and
> `docs/desktop/02-architecture-and-foundation/README.md` § 3 decisions 2, 3 and 9; if either ADR
> lands differently this plan is revised before implementation.

Because `refs` is empty, these are the authorities that actually bind today:

| Authority | Requirement | Met by |
| --- | --- | --- |
| Proposal § 7.1 Runtime | .NET 10, latest stable Windows App SDK pinned centrally, Windows 11 x64, self-contained signed MSIX, no AOT/trimming initially | Steps 5, 6 |
| Proposal § 7.2 Application composition | Packaged single-project MSIX with package identity | Steps 5, 8, 11 |
| Proposal § 5.4, § 24 Phase 1 | Solution structure and the Phase 1 project set | Steps 4, 9 |
| Plan 02 § 3 decision 2 | Four new source projects, each a boundary project; features are folders inside them | Step 4 creates the first of them and adds no feature assembly |
| Plan 02 § 3 decision 3 | The exact target properties: `net10.0-windows10.0.26100.0`, `Platforms x64`, min OS 10.0.22000, packaged single-project MSIX, `WindowsAppSDKSelfContained=true`, `SelfContained=true`, `RuntimeIdentifier win-x64`, `PublishReadyToRun=false`, no trimming/AOT, WinAppSDK pinned centrally | Step 5 |
| Plan 02 § 3 decision 4 | Central package management; major Windows App SDK / toolkit upgrades are reviewed PRs, never automatic | Step 6 |
| Plan 02 § 3 decision 9 | No desktop framework on top of WinUI | Step 4 deletes nothing but replaced sample pages and adds no framework |
| Plan 02 § 7 (five recorded traps) | XAML compiler silence (pin ≥ 2.1.3); `TreatWarningsAsErrors` + generated code; the `BuildAndRun.ps1` props behaviour; package identity churn; self-contained size measured for the release manifest | Steps 6, 7, 11, 12, 3, and § Verification item 5 |
| **D-002** (locked) | Production signing uses a self-managed certificate whose **subject must equal the manifest `Publisher` exactly** | Step 3, and the verbatim record it produces |
| **D-003** (locked) + **C-01** | The update feed is an in-house UNC share over SMB; no anonymous HTTPS feed | Cited only — this ticket adds no feed reference; [[FND-048]] (plan handle `DSK-04-12`) and area 09 own it |
| L-04 (locked) | Every ticket names its subagent, skills and MCP tools | § Routing below |
| `AGENTS.md` § Product invariants (`:235`) | A new top-level project requires an accepted ADR | The New-ADR paragraph above; the dependency on [[FND-026]] |
| `docs/engineering.md` § Required evidence tiers (`:72`), tier 1 | A compiling, launching project and an enforced solution shape — consistency only, no operator capability claimed | § Verification |

## Routing

Copied from the ticket body's `## Routing` block, as
`docs/desktop/00-governance-and-workflow/README.md` § Ticket template requires of the plan document
specifically.

- **Subagent**: `winui-dev` — `.codex/agents/winui-dev.toml` (verified present).
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `winui-setup`
  (`.codex/skills/winui-setup/SKILL.md`; its frontmatter carries `disable-model-invocation: true`, so
  it is **user-invoked**, prerequisites only) → `winui-dev-workflow`
  (`.codex/skills/winui-dev-workflow/SKILL.md`) → `winui-design`
  (`.codex/skills/winui-design/SKILL.md`), all vendored from `microsoft/win-dev-skills` v0.5.0
  `f1028dd5`.
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`,
  `move_item`); Microsoft Learn (`microsoft_docs_search`, `microsoft_docs_fetch` for the Windows App
  SDK 2.x release notes and single-project MSIX).
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute` →
  `kanmer-review` → `kanmer-verify` → `kanmer-closeout`. Call `get_doc_gates <id>` before every move;
  a move crosses at most one gated boundary.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement
  (`AGENTS.md` § Repository task workflow step 5).

## Steps

These refine the ticket body's thirteen steps: same order, same ownership, same paths.

1. **Orient.** Read `docs/desktop/02-architecture-and-foundation/README.md` § 3 decisions 2/3/9 and
   § 7, and `.codex/skills/winui-dev-workflow/SKILL.md` in full. Then `get_doc_gates FND-030` and
   `take_ticket` on branch `task/desktop-scaffold` in worktree
   `../pegasus-worktrees/desktop-scaffold` from `origin/dev`.
2. **Operator step — prerequisites.** Run the detection block from
   `.codex/skills/winui-setup/SKILL.md` verbatim: `dotnet --list-sdks`; `winapp --version` (must be
   ≥ 0.3 after stripping a `-prerelease.N` suffix); `dotnet new list winui | Select-String 'winui-mvvm'`;
   and `Get-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock' -Name AllowDevelopmentWithoutDevLicense`
   expecting `1`. Enabling Developer Mode and `winget install --id Microsoft.WinAppCLI` need a UAC
   elevation only the operator can accept. **Do not install anything yourself** — stop and ask
   (`winui-dev-workflow` § Prerequisites). Also record `$env:PROCESSOR_ARCHITECTURE`: on ARM64,
   `BuildAndRun.ps1:89` will append `/p:Platform=ARM64` and collide with `<Platforms>x64</Platforms>`,
   so every later `BuildAndRun.ps1` call must add `/p:Platform=x64`.
3. **Operator step — fix the package identity, permanently.** Confirm with the operator (a) the
   `Identity.Name` — `docs/desktop/09-release-update-and-distribution/README.md:156` assumes
   `CollisionEngineers.Pegasus`, one identity for both channels, and that is an *assumption* awaiting
   confirmation, not a decision; and (b) the exact `Identity.Publisher` distinguished name, which
   under D-002 must equal the subject of the self-managed production certificate issued by
   [[REL-007]] (plan handle `DSK-09-08`). Record both **verbatim** in this document under a dated
   heading. Neither is ever changed afterwards: plan 09 `:329` records "Publisher mismatch between
   certificate and `Identity.Publisher`" as a release trap, and to Windows a changed identity is a
   different application.
4. **Scaffold.** `dotnet new winui-mvvm -n Pegasus.Desktop -o src/Pegasus.Desktop`. Do **not** `mkdir`
   first. Count the emitted files and correct this plan's diff estimate. Delete nothing except sample
   pages actually replaced; never delete `Package.appxmanifest`.
5. **Retarget the csproj** to plan 02 § 3 decision 3: `<TargetFramework>net10.0-windows10.0.26100.0</TargetFramework>`,
   `<TargetPlatformMinVersion>10.0.22000.0</TargetPlatformMinVersion>`, `<Platforms>x64</Platforms>`,
   `<RuntimeIdentifier>win-x64</RuntimeIdentifier>`, `<SelfContained>true</SelfContained>`,
   `<WindowsAppSDKSelfContained>true</WindowsAppSDKSelfContained>`,
   `<PublishReadyToRun>false</PublishReadyToRun>`, no `PublishTrimmed`, no `PublishAot`. Never
   `<WindowsPackageType>None</WindowsPackageType>` and never `AnyCPU`
   (`winui-dev-workflow` § Critical Rules) — the packaged identity is what
   [[FND-035]] (plan handle `DSK-02-10`)'s `AppInstance` keys depend on.
6. **Pin centrally.** Add to `Directory.Packages.props` (created by [[FND-027]], plan handle
   `DSK-02-02`; confirmed absent today): `Microsoft.WindowsAppSDK` at the latest **stable** 2.x —
   2.4.0, released 2026-08-13, per plan 02 § 2; re-confirm with `microsoft_docs_fetch` on
   <https://learn.microsoft.com/windows/apps/windows-app-sdk/release-notes/windows-app-sdk-2-0> —
   plus `Microsoft.Windows.SDK.BuildTools`, `Microsoft.Windows.SDK.BuildTools.WinApp`,
   `Microsoft.WindowsAppSDK.Analyzers` and `CommunityToolkit.Mvvm`. Strip the version literals the
   template wrote into the csproj. The floor is **2.1.3** because earlier 2.x builds fail `MSB3073`
   with no XAML diagnostic (`winui-dev-workflow` § Common Errors). `global.json`'s
   `allowPrerelease: false` means a preview version is not an option. Record the exact chosen version
   and its release date here. If [[FND-027]] has not landed, record the sequencing rather than
   leaving permanent version literals in the csproj.
7. **Reference `Microsoft.WindowsAppSDK.Analyzers` explicitly** in
   `src/Pegasus.Desktop/Pegasus.Desktop.csproj`. The ticket body's instruction stands; its stated
   reason does not, and the corrected reason is stronger. Measured at `BuildAndRun.ps1:146-157`, the
   script tests only for `src/Pegasus.Desktop/Directory.Build.props` and **injects one when it is
   absent** — the repository-root props does not stop it. Because MSBuild's implicit
   `Directory.Build.props` import stops at the first file found walking up, the injected file
   *shadows* the root props for that build. So without the explicit package reference the `WUI*`
   diagnostics come only from the script, and script builds silently lose `TreatWarningsAsErrors`.
   Record this measured behaviour in the ticket so it is not rediscovered, and treat plain
   `dotnet build` as the authoritative gate (§ Verification).
8. **Set the identity** in `src/Pegasus.Desktop/Package.appxmanifest`: `Identity/@Name` and
   `Identity/@Publisher` to the step-3 values; `Identity/@Version` to `0.1.0.0` as a placeholder
   ([[REL-002]], plan handle `DSK-09-02`, wires the version from the CI run);
   `Properties/DisplayName` and `Application/uap:VisualElements/@DisplayName` to `Pegasus`. Add a
   comment in the csproj or a project `README` recording which manifest fields are **permanent**
   (`Name`, `Publisher`) and which are **placeholders** (`Version`).
9. **Register the project.** Add `<Project Path="src/Pegasus.Desktop/Pegasus.Desktop.csproj" />` to
   the `/src/` folder in `Pegasus.slnx`; keep it **out** of the server entry point from [[FND-028]]
   (plan handle `DSK-02-03`); and extend the ordinal expected array in
   `DependencyDirectionTests.ApplicationSolutionExcludesSourceWorkspaces`
   (`tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs:137-149`), where the desktop path
   sorts between the Core and Infrastructure entries. If [[FND-028]] has not landed, note in the proof
   that this commit makes `dotnet build ./Pegasus.slnx` fail on Linux until it does.
10. **Restore with the lock file.**
    `dotnet restore ./src/Pegasus.Desktop/Pegasus.Desktop.csproj -r win-x64 --force-evaluate`, then
    commit the generated `src/Pegasus.Desktop/packages.lock.json`. Then
    `dotnet restore ./Pegasus.slnx --locked-mode` must pass on Windows — the composite CI action runs
    exactly that (`.github/actions/dotnet-build/action.yml:22`) on every lane, so a missing or stale
    lock file breaks the whole workflow rather than one job.
11. **Build and run.** `pwsh .codex/skills/winui-dev-workflow/BuildAndRun.ps1 src/Pegasus.Desktop/Pegasus.Desktop.csproj -SkipRun`
    (safe synchronously), then the same command **without** `-SkipRun`, invoked **asynchronously** —
    the script stays attached for the app's lifetime. Done looks like the line
    `✅ <pkg> launched (PID: …)` and a visible window; capture a screenshot for the proof. On ARM64
    add `/p:Platform=x64`. If the build fails with `MSB3073` / `XamlCompiler.exe … exited with code 1`
    naming no `.xaml`, raise the `Microsoft.WindowsAppSDK` pin. Also run a **plain**
    `dotnet build ./src/Pegasus.Desktop/Pegasus.Desktop.csproj -c Release -p:Platform=x64` and compare
    the warning count with the script's — a difference is the props-shadowing effect from step 7 and
    belongs in the proof.
12. **Resolve analyzer noise honestly.** With `TreatWarningsAsErrors=true` and
    `AnalysisLevel=latest-recommended` inherited from `Directory.Build.props`, template and
    XAML-generated code will trip the build. Add narrowly-scoped `<NoWarn>` entries **in the desktop
    csproj**, each with a comment naming the rule and why it is suppressed, or exclude generated files
    by path. Never relax `Directory.Build.props`.
13. **Test and close.** `dotnet test ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj --configuration Release`
    — expected green with the extended solution list. Add the desktop client to
    `docs/current-architecture.md` § System shape (`:27`) and § Components and dependency direction
    (`:55`). Touch `docs/runbook.md` § Supported platform (`:19-40`) **only if** [[FND-039]] (plan
    handle `DSK-02-14`) has not already added the prerequisites, and record which case applied. Run
    the simplification pass, record it under a dated heading below, and open the PR into `dev`.

## Verification

Evidence tier **1 — Static/build/architecture** (`docs/engineering.md` § Required evidence tiers,
`:72`), as the ticket body states: a compiling, launching project and the enforced solution shape. It
proves consistency, not any operator capability.

The `proof` document is produced from these:

1. `pwsh .codex/skills/winui-dev-workflow/BuildAndRun.ps1 src/Pegasus.Desktop/Pegasus.Desktop.csproj -SkipRun`
   — expected: `BUILD SUCCEEDED`, zero warnings. Note in the proof whether the script printed
   `Microsoft.WindowsAppSDK.Analyzers: enabled` (it injected a props file) or `skipped (existing …)`,
   because that single line tells the reader whether the root props applied to that build.
2. `pwsh .codex/skills/winui-dev-workflow/BuildAndRun.ps1 src/Pegasus.Desktop/Pegasus.Desktop.csproj`
   invoked **async** — expected: `✅ <pkg> launched (PID: …)` and a visible window. Attach the
   screenshot. Never run the packaged `.exe` directly; it exits silently and misdiagnoses everything.
3. **The authoritative zero-warning gate**: `dotnet restore ./Pegasus.slnx --locked-mode` then
   `dotnet build ./Pegasus.slnx --configuration Release --no-restore` on Windows — expected exit 0
   with `0 Warning(s)`. This is the command CI runs
   (`.github/actions/dotnet-build/action.yml:22-27`) and it is not subject to the props-shadowing
   effect. Paste the summary line.
4. `dotnet test ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj --configuration Release`
   — expected: all facts pass, including the extended solution list.
5. **Measure and record the package size** (plan 02 § 7): the size of the produced package or of the
   self-contained output directory, owed to [[REL-002]]'s release manifest. A number, in the proof.
6. Record the resolved `Microsoft.WindowsAppSDK` version and its release date, the confirmed
   `Identity.Name` and `Identity.Publisher` verbatim, and whether [[FND-028]] had landed.

## Risks / open questions

- **Risk — `BuildAndRun.ps1` shadows the repository props.** Measured, not hypothetical:
  `BuildAndRun.ps1:146-157` injects `src/Pegasus.Desktop/Directory.Build.props` whenever that exact
  file is absent, and MSBuild stops at the first one it finds walking up, so
  `TreatWarningsAsErrors`, `Nullable`, `ImplicitUsings`, `LangVersion` and `Version` do not apply to
  that build. *Mitigation*: the explicit analyzer reference (step 7), and treating plain
  `dotnet build` as the gate (§ Verification item 3). **This corrects the reason given in plan 02 § 7
  and in this ticket's step 7 — the instruction is unchanged and is followed; only the stated
  mechanism is wrong.** Do not "fix" it by committing a permanent
  `src/Pegasus.Desktop/Directory.Build.props`: that would shadow the root props for *every* build,
  which is strictly worse, and the root props is the single source of truth for those settings.
- **Risk — package identity churn.** `Identity.Name` and `Identity.Publisher` are permanent, and the
  certificate subject must equal `Publisher` exactly (D-002; plan 09 `:216`, `:329`). *Mitigation*:
  step 3 is an operator step and its outputs are recorded verbatim before any other work.
- **Risk — Windows App SDK / SDK band mismatch.** Plan 02 § 2 assumption A1.
  `global.json`'s `allowPrerelease: false` narrows the choice to stable 2.x. *Mitigation*: step 6
  re-confirms the version and step 11 proves the build; a `global.json` bump is its own ticket and is
  recorded here rather than performed.
- **Risk — ARM64 workstation.** `BuildAndRun.ps1:89` would pass `/p:Platform=ARM64` into a project
  declaring `<Platforms>x64</Platforms>`. *Mitigation*: step 2 records the architecture and step 11
  passes `/p:Platform=x64` where needed. Do **not** widen `<Platforms>` — x64-only is plan 02 § 3
  decision 3.
- **Risk — the lock file.** `dotnet restore ./Pegasus.slnx --locked-mode` runs on every CI lane and
  the cache key already globs `src/**/packages.lock.json`, so a missing or stale desktop lock file
  breaks all of CI. *Mitigation*: step 10 generates it with `-r win-x64 --force-evaluate` and commits
  it before the solution restore.
- **Sequencing, not an open question — [[FND-028]].** The plan's dependency arrow names only
  [[FND-026]] and [[FND-027]], but adding a Windows-target project to `Pegasus.slnx` is exactly what
  breaks Linux builds. If [[FND-028]] has not landed, this commit makes the repository Windows-only
  for developers; say so in the proof rather than adding the server entry point here (it is
  [[FND-028]]'s file).
- **Scope boundary, not an open question — the shell.** [[FND-033]] (plan handle `DSK-02-08`) owns
  it, and it must be a `NavigationView`, never a port of
  `src/Pegasus.Web/Pages/Shared/_Layout.cshtml`.
- **Scope boundary, not an open question — WebView2.** No reference is added; the only permitted use
  is the isolated report renderer under ADR-0108, in area 07. [[FND-037]] (plan handle `DSK-02-12`)
  adds the test that enforces this.
- **Scope boundary, not an open question — the runbook prerequisites sentence.** Owned by
  [[FND-039]]; cited here, never restated.
- **No `open-questions` document is opened.** The two unfixed values are assigned to the operator
  inside step 3 — a `needs-operator` step this ticket already carries as a label — and blocking
  `leave-preparing` would prevent the ticket reaching the step that asks. Everything else is settled
  by a command inside the ticket's own steps.

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over this branch's own
diff before the PR, recorded here under a dated heading._
