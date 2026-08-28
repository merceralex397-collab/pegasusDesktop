# Plan — FND-030: Scaffold `src/Pegasus.Desktop` (WinUI 3, x64, packaged, self-contained, pinned Windows App SDK 2.x)

**Diff estimate: ~20 files, ~700 hand-written and template lines, plus one generated `packages.lock.json` of several thousand lines.**

`docs/engineering.md` § Plan sizing (`:201`) requires the estimate first. It is derived from this
ticket's `files` document and re-measured on 2026-08-24 against the working tree, with the command
shown.

| Path | Measured current state | Change | Lines |
| --- | --- | --- | --- |
| `src/Pegasus.Desktop/**` | absent — `ls src` returns exactly `Pegasus.Core`, `Pegasus.Infrastructure`, `Pegasus.Web`, `Pegasus.Worker` | `dotnet new winui-mvvm` output: csproj, `App.xaml`(`.cs`), `MainWindow.xaml`(`.cs`), `Package.appxmanifest`, `app.manifest`, `Assets/**`, a View/ViewModel pair | ~13 files, ~550 lines (template-generated, not hand-written) |
| `src/Pegasus.Desktop/Pegasus.Desktop.csproj` | part of the above | hand edits: TFM, min platform, `Platforms`, RID, the self-contained pair, `PublishReadyToRun=false`, an explicit analyzer `PackageReference`, commented `NoWarn` entries, version literals stripped | ~+30 / ~-6 |
| `src/Pegasus.Desktop/Package.appxmanifest` | part of the above | `Identity/@Name`, `Identity/@Publisher`, `Identity/@Version`, two `DisplayName` values | ~6 changed lines |
| `src/Pegasus.Desktop/packages.lock.json` | absent | **generated** by `dotnet restore -r win-x64 --force-evaluate`, then committed. Contrast `src/Pegasus.Core/packages.lock.json` — 124 bytes, three empty entries (`net10.0`, `net10.0/linux-x64`, `net10.0/win-x64`). The desktop one carries the whole Windows App SDK graph and is RID-specific | +2,000–6,000 (generated) |
| `Directory.Packages.props` | **absent today** — `ls Directory.Packages.props` → *No such file or directory*. Created by [[FND-027]] (plan handle `DSK-02-02`) | five `<PackageVersion>` elements | +5 |
| `Pegasus.slnx` | 14 lines; four `/src/` and three `/tests/` `<Project Path=…/>` elements | one `<Project Path="src/Pegasus.Desktop/Pegasus.Desktop.csproj" />` under `/src/` | +1 |
| `tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs` | 520 lines (`wc -l`). `ApplicationSolutionExcludesSourceWorkspaces` at `:128`; the ordinal expected array at `:141-149`, its seven path literals at `:142-148`; helpers `ProjectReferences` `:493`, `FindRepositoryRoot` `:509` | one string inserted into the array, between the Core and Infrastructure entries | +1 |
| `docs/current-architecture.md` | 682 lines. § System shape at `:27`, § Components and dependency direction at `:55` | the desktop client added to both | ~+8 |
| `docs/runbook.md` | 1254 lines. § Supported platform `:19-40`, with "record the platform actually exercised" at `:38` | one line: the desktop build requires Windows — **conditional**, see step 14 | ~+3 or 0 |

Totals: **~20 files**; **~700 lines** of template output plus hand edits; **one generated lock
file** that dominates the raw diff and must not be counted as authored work. Nothing under
`src/Pegasus.Core`, `src/Pegasus.Infrastructure`, `src/Pegasus.Web` or `src/Pegasus.Worker` is
touched.

## Approach

Scaffold from the vendored `winui-mvvm` template and then **retarget** it, rather than hand-writing
a csproj from the Windows App SDK documentation. The template is the path the vendored toolchain
supports end to end — `.codex/skills/winui-dev-workflow/SKILL.md:10` names
`dotnet new winui-mvvm -n <AppName>` as the creation command, and `BuildAndRun.ps1` expects the
output layout `bin\<Platform>\<Config>\<tfm>\win-<rid>\` (`:228`) that the template produces. It
also brings CommunityToolkit.Mvvm, a TitleBar, MicaBackdrop and Frame navigation already wired,
which [[FND-032]] (plan handle `DSK-02-07`) and [[FND-033]] (plan handle `DSK-02-08`) then replace
deliberately rather than invent.

The rejected alternative is `winapp init` on an empty folder. It is documented
(<https://learn.microsoft.com/windows/apps/dev-tools/winapp-cli/guides/dotnet>, fetched 2026-08-23
by the plan-02 author) and sets the same `net10.0-windows10.0.26100.0` TFM, but it produces no MVVM
wiring, no `App.xaml` composition point and no sample View/ViewModel pair, so every one of those
would be hand-written here — work that [[FND-032]] and [[FND-033]] own. It is rejected for scope,
not for correctness.

Three properties of this repository make "scaffold then retarget" more than a formality, and the
steps below exist to handle them:

1. `global.json` pins SDK `10.0.302` with **`allowPrerelease: false`**, so only a *stable* Windows
   App SDK 2.x may be pinned, with a floor of **2.1.3** — below that, `MSB3073` /
   `XamlCompiler.exe … exited with code 1` names no `.xaml` file at all
   (`.codex/skills/winui-dev-workflow/SKILL.md:79`). 2.4.0 (2026-08-13) is the latest stable
   recorded in plan 02 § 2 and is the default choice.
2. `Directory.Build.props` (19 lines) applies `TreatWarningsAsErrors=true` and
   `AnalysisLevel=latest-recommended` to every project including this one, and template plus XAML
   generated code will trip it. That is absorbed with narrow, individually-commented `NoWarn`
   entries in the desktop csproj — never by relaxing the root props.
3. `BuildAndRun.ps1` **injects** a project-level `Directory.Build.props`, which shadows the root one
   for the duration of that build. See § Risks; the consequence for the approach is that plain
   `dotnet build ./Pegasus.slnx --configuration Release` is the authoritative zero-warning gate and
   `BuildAndRun.ps1` is the launch mechanism, not the gate.

## Governing docs

The ticket's `refs` array is empty and `get_doc_gates FND-030` reports `docs_todo: true`, so there
is no linked PRD/FRD/ADR to meet today.

> **New ADR** — ADR-0100 (native WinUI 3 / Windows 11 desktop client converted inside this fork, no
> WebView shell — the ADR that authorises this new top-level project), authored by [[FND-026]]
> (plan handle `DSK-02-01`); [[FND-005]] (plan handle `DSK-00-05`) also claims ADR-0100 in the
> reserved block ADR-0100…ADR-0110 — see [[FND-026]]'s plan for the ownership reconciliation.
> ADR-0104 (online-required, bounded local cache) has the same two claimants; it bounds anything the
> desktop later caches and therefore constrains [[FND-031]] (plan handle `DSK-02-06`), not this
> ticket, which adds no cache.
> This plan is written to the decision as recorded in
> `docs/desktop/00-governance-and-workflow/README.md` § 3 (ADR set table, ADR-0100 row) and
> `docs/desktop/02-architecture-and-foundation/README.md` § 3 decisions 2, 3 and 9; if the ADR lands
> differently this plan is revised before implementation.

Because `refs` is empty, the authorities that actually bind today are these:

| Authority | Requirement | Met by |
| --- | --- | --- |
| Proposal § 7.1 Runtime | .NET 10, latest **stable** Windows App SDK pinned centrally, Windows 11 x64, self-contained signed MSIX, no AOT/trimming initially | Steps 6, 7 |
| Proposal § 7.2 Application composition | A WinUI app composed with a generic host, not a framework built on top of WinUI | Step 5 — the template's composition is left in place; host composition is [[FND-032]]'s |
| Proposal § 5.4 solution structure | `src/Pegasus.Desktop` is one boundary project; features stay folders inside it | Steps 5, 10 |
| Plan 02 § 3 decision 3 | `net10.0-windows10.0.26100.0`, min OS `10.0.22000.0`, `Platforms` x64 only, packaged single-project MSIX, `WindowsAppSDKSelfContained=true`, `SelfContained=true`, `RuntimeIdentifier=win-x64`, `PublishReadyToRun=false`, no trimming/AOT | Step 6 |
| Plan 02 § 3 decision 4 | Central package management; `RestorePackagesWithLockFile=true` for every project | Steps 7, 11 |
| Plan 02 § 4 target-state table (`src/Pegasus.Desktop` row) | References Core, Contracts and Desktop.Infrastructure; never `Pegasus.Infrastructure`, EF, Azure SDKs, Box/Graph SDKs or `Microsoft.AspNetCore.*` | Step 5 — the scaffold adds **no** `ProjectReference` at all; the rules themselves are [[FND-037]]'s (plan handle `DSK-02-12`) |
| Plan 02 § 7 — package identity churn | `Identity.Name` and `Identity.Publisher` are permanent; the signing certificate subject must equal the `Publisher` exactly | Steps 4, 9 |
| Plan 02 § 7 — self-contained size | "acceptable for ten users but measure and record in 09's release manifest" | Verification V5 |
| Plan 02 § 7 — XAML compiler silence | Pin ≥ 2.1.3 on the 2.x line | Step 7 |
| Plan 02 § 7 — `TreatWarningsAsErrors` | Generated code needs explicit `NoWarn` / `GeneratedCodeAttribute` handling, never a relaxed repository policy | Step 13 |
| **D-002** (`docs/desktop/README.md` § Locked decisions, decided 2026-08-23) | Production signing uses a self-managed certificate whose subject is fixed to the manifest `Publisher` | Step 4 fixes the string; [[REL-007]] (plan handle `DSK-09-08`) issues the certificate against it |
| **L-04** (`docs/desktop/README.md` § Locked decisions) | Every ticket names its subagent, skills and MCP tools | § Routing below |
| `AGENTS.md` § Product invariants (`:235`) | A new top-level project requires an accepted ADR proving the existing boundary cannot carry it | The [[FND-026]] dependency; this plan does **not** proceed on an unaccepted ADR-0100 |
| `docs/engineering.md` § Plan sizing (`:201`) | A plan states its diff estimate first, derived from a measured inventory | The inventory table above |
| `docs/engineering.md` § Required evidence tiers, tier 1 (`:76`) | "compile the four approved projects, enforce dependency direction and one policy owner … This proves consistency only" | § Verification, and its honesty clauses |
| **C-01** (`docs/desktop/README.md` § Constraints) | The repositories become private; GitHub Actions Windows minutes stop being free | This ticket adds **no** CI job — [[FND-040]] (plan handle `DSK-02-15`) owns the `desktop-build` lane |

## Routing

Copied from the ticket body's `## Routing` block, as
`docs/desktop/00-governance-and-workflow/README.md` § Ticket template requires of the plan document
specifically.

- **Subagent**: `winui-dev` — `.codex/agents/winui-dev.toml`.
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `winui-setup`
  (`.codex/skills/winui-setup/SKILL.md` — **user-invoked**; its frontmatter carries
  `disable-model-invocation: true`; prerequisites only) → `winui-dev-workflow`
  (`.codex/skills/winui-dev-workflow/SKILL.md`) → `winui-design`
  (`.codex/skills/winui-design/SKILL.md`), all vendored from `microsoft/win-dev-skills` v0.5.0
  `f1028dd5`.
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`,
  `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search`, `microsoft_docs_fetch`
  for the Windows App SDK 2.x release notes and single-project MSIX).
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute` →
  `kanmer-review` → `kanmer-verify` → `kanmer-closeout`. Call `get_doc_gates <id>` before every
  move; a move crosses at most one gated boundary. `get_doc_gates FND-030` reports the owed set as
  `governing-doc` at `leave-backlog` (satisfied by `docs_todo: true`); `research`, `files`, `plan`,
  `checklist` and `questions-resolved` at `leave-preparing`; `post-implementation-report` at
  `enter-review`; `proof` at `enter-done`.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md`
  § Repository task workflow step 5).

## Steps

These refine the ticket body's thirteen implementation steps: same order, same ownership, same file
paths, adding the *how* the body leaves out. The body's steps 2–3 are split into steps 2–4 here
because prerequisites and package identity are two separate operator interactions, and the first
gates the second.

1. **Orient.** Read `docs/desktop/02-architecture-and-foundation/README.md` § 3 decisions 2, 3 and
   9, § 4 target-state table and § 7 in full; read `.codex/skills/winui-dev-workflow/SKILL.md` in
   full (its § Critical Rules matter more than the rest); read this ticket's `research` and `files`
   documents. Call `get_doc_gates FND-030`, then `take_ticket` on branch `task/desktop-scaffold` in
   worktree `../pegasus-worktrees/desktop-scaffold` created from `origin/dev`.
2. **Confirm the two hard prerequisites have landed.** [[FND-026]] must have ADR-0100 accepted
   (`ls docs/adr/0100-*.md`, then confirm its frontmatter status) — without it `AGENTS.md`
   § Product invariants forbids creating this top-level project at all. [[FND-027]] must have
   created `Directory.Packages.props` (`ls Directory.Packages.props`; it is absent today). If
   [[FND-027]] has not landed, **stop and record the sequencing** rather than inlining version
   literals that step 7 would then have to strip again — see § Risks. Also check whether
   [[FND-028]] (plan handle `DSK-02-03`) has landed the server entry point; if it has not, this
   ticket makes the repository Windows-only for developers and the proof must say so.
3. **Operator step — toolchain prerequisites.** Run the detection block from
   `.codex/skills/winui-setup/SKILL.md`: `dotnet --list-sdks`; `winapp --version` (must be ≥ 0.3
   after stripping any `-prerelease.N` suffix); `dotnet new list winui | Select-String 'winui-mvvm'`;
   and the registry read `HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock` →
   `AllowDevelopmentWithoutDevLicense -eq 1`. Enabling Developer Mode and
   `winget install --id Microsoft.WinAppCLI` each need a UAC elevation only the operator can
   accept. **Do not attempt to install these yourself if they are missing — stop and ask**
   (`winui-dev-workflow` § Prerequisites). Paste the four outputs into this plan's progress notes.
4. **Operator step — fix the package identity, permanently.** Ask the operator for two strings and
   record them verbatim in this plan under a dated heading **before writing any file**:
   - `Identity/@Name` — plan 09 § 2
     (`docs/desktop/09-release-update-and-distribution/README.md:156-158`) assumes
     `CollisionEngineers.Pegasus`, one identity for both channels. That is a **plan assumption, not
     an operator confirmation**; this step is where it becomes fixed.
   - `Identity/@Publisher` — the exact distinguished name. It is written down nowhere in the
     repository and can only come from the operator. Under **D-002** the self-managed production
     certificate's subject must equal it character for character; plan 09 `:329` records
     "Publisher mismatch between certificate and `Identity.Publisher`" as a named trap, and
     [[REL-007]] (plan handle `DSK-09-08`) cannot issue the certificate until this string exists.

   Neither value is ever changed afterwards: to Windows a changed `Identity.Name` or `Publisher` is
   a *different application*, and every workstation that already installed the old one keeps it.
5. **Scaffold.** `dotnet new winui-mvvm -n Pegasus.Desktop -o src/Pegasus.Desktop`. **Do not
   `mkdir` first** — the template creates the folder (`winui-dev-workflow` § Create or Open a
   Project). Delete nothing from the output except sample pages you actually replace, and **never**
   `Package.appxmanifest`. Add no `ProjectReference` in this ticket: Core and Contracts arrive with
   [[FND-031]] and [[FND-032]], and adding them early would put an unenforced dependency edge in
   place before [[FND-037]] writes the rules that police it.
6. **Retarget the csproj** — `src/Pegasus.Desktop/Pegasus.Desktop.csproj`, to plan 02 § 3
   decision 3 exactly: `<TargetFramework>net10.0-windows10.0.26100.0</TargetFramework>`,
   `<TargetPlatformMinVersion>10.0.22000.0</TargetPlatformMinVersion>`,
   `<Platforms>x64</Platforms>`, `<RuntimeIdentifier>win-x64</RuntimeIdentifier>`,
   `<SelfContained>true</SelfContained>`,
   `<WindowsAppSDKSelfContained>true</WindowsAppSDKSelfContained>`,
   `<PublishReadyToRun>false</PublishReadyToRun>`, **no** `PublishTrimmed`, **no** `PublishAot`, and
   `<RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>` unless [[FND-027]] already sets
   it globally. **Never** `<WindowsPackageType>None</WindowsPackageType>` and **never** `AnyCPU`
   (`winui-dev-workflow` § Critical Rules `:97-100`; `:81` records `0x8007000B bad image format` as
   the symptom of the `AnyCPU` mistake).
7. **Pin centrally.** Add to `Directory.Packages.props`:
   `<PackageVersion Include="Microsoft.WindowsAppSDK" Version="2.4.0" />` — or the latest 2.x
   **stable** confirmed at kickoff with `microsoft_docs_fetch` on
   <https://learn.microsoft.com/windows/apps/windows-app-sdk/release-notes/windows-app-sdk-2-0>,
   floor **2.1.3** — plus `Microsoft.Windows.SDK.BuildTools`,
   `Microsoft.Windows.SDK.BuildTools.WinApp`, `Microsoft.WindowsAppSDK.Analyzers` and
   `CommunityToolkit.Mvvm`. Then strip every `Version=` literal the template wrote into the csproj.
   Record the exact chosen version **and its release date** in this plan. `allowPrerelease: false`
   in `global.json` is why a preview build is not an option here.
8. **Reference the analyzer explicitly** in `src/Pegasus.Desktop/Pegasus.Desktop.csproj`. The ticket
   body's instruction stands unchanged; its stated *reason* does not — see § Risks. The correct
   reason: `BuildAndRun.ps1` supplies the `WUI*` diagnostics only during its own builds, through a
   props file it injects and then deletes in a `finally` block (`:198-205`), so CI's plain
   `dotnet build` and every `dotnet build` a developer runs would carry no analyzer at all without
   the package reference.
9. **Set the manifest identity** in `src/Pegasus.Desktop/Package.appxmanifest`: `Identity/@Name`
   and `Identity/@Publisher` to the step-4 values; `Identity/@Version` to `0.1.0.0` as a placeholder
   — [[REL-002]] (plan handle `DSK-09-02`) wires the version from the CI run;
   `Properties/DisplayName` and `Application/uap:VisualElements/@DisplayName` to `Pegasus`. Add a
   comment block in the csproj (or a short `src/Pegasus.Desktop/README.md`) stating plainly which
   fields are **permanent** (`Name`, `Publisher`) and which are **placeholders** (`Version`), so the
   next agent cannot mistake one for the other.
10. **Register the project.** Add `<Project Path="src/Pegasus.Desktop/Pegasus.Desktop.csproj" />`
    to the `/src/` folder in `Pegasus.slnx`. Keep it **out** of the server entry point created by
    [[FND-028]] — that omission is the entire point of [[FND-028]]'s
    `ServerSolutionFilterExcludesWindowsTargetedProjects` fact, which starts doing real work at this
    moment instead of asserting a tautology. Then extend the ordinal expected array in
    `DependencyDirectionTests.ApplicationSolutionExcludesSourceWorkspaces`
    (`tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs:141-149`, the seven path literals
    at `:142-148`) with `"src/Pegasus.Desktop/Pegasus.Desktop.csproj"`. Ordinal order puts it
    **between** `src/Pegasus.Core/…` and `src/Pegasus.Infrastructure/…`, not at the end. Change
    nothing else in that file; the desktop dependency **rules** are [[FND-037]]'s.
11. **Restore and commit the lock file.**
    `dotnet restore ./src/Pegasus.Desktop/Pegasus.Desktop.csproj -r win-x64 --force-evaluate`, then
    commit the generated `src/Pegasus.Desktop/packages.lock.json`. Then
    `dotnet restore ./Pegasus.slnx --locked-mode` must pass on Windows. This is not optional
    housekeeping: `.github/actions/dotnet-build/action.yml:22-27` runs
    `dotnet restore ./Pegasus.slnx --locked-mode` on **every** lane and its SDK cache key already
    globs `src/**/packages.lock.json`, so a missing or stale desktop lock file fails all of CI, not
    just a desktop job.
12. **Build and launch.**
    `pwsh .codex/skills/winui-dev-workflow/BuildAndRun.ps1 src/Pegasus.Desktop/Pegasus.Desktop.csproj -SkipRun`
    first (safe synchronously), then the same command **without** `-SkipRun`, invoked
    asynchronously — the script stays attached. Success is the literal line
    `✅ <pkg> launched (PID: …)` (`winui-dev-workflow/SKILL.md:37`) **and** a visible window;
    capture a screenshot for the proof. Never run the packaged `.exe` directly — "App silently
    exits → use `winapp run`" (`SKILL.md:76`). On an ARM64 workstation pass `/p:Platform=x64`
    explicitly: `BuildAndRun.ps1:89` auto-detects ARM64 and would append `/p:Platform=ARM64` into a
    project declaring `<Platforms>x64</Platforms>`. Note the script also defaults to
    `Configuration=Debug` (`:90`) and auto-adds `/restore` (`:104`), so its green result is a
    **Debug** build unless you pass `/p:Configuration=Release`. If the build fails `MSB3073` /
    `XamlCompiler.exe … exited with code 1` naming no `.xaml` file, raise the
    `Microsoft.WindowsAppSDK` pin (`SKILL.md:79`).
13. **Absorb analyzer noise honestly.** `Directory.Build.props` sets `TreatWarningsAsErrors=true`
    and `AnalysisLevel=latest-recommended`. Add narrowly-scoped `<NoWarn>` entries in
    `src/Pegasus.Desktop/Pegasus.Desktop.csproj`, each with a comment naming the rule and why it is
    suppressed, or exclude generated files by path. **Do not** relax the repository-wide policy in
    `Directory.Build.props` — it governs all seven existing projects. The gate for this step is a
    plain `dotnet build ./Pegasus.slnx --configuration Release` reporting `0 Warning(s)`, not a
    green `BuildAndRun.ps1`.
14. **Documentation.** Add the desktop client to `docs/current-architecture.md` § System shape
    (`:27`) and § Components and dependency direction (`:55`). For `docs/runbook.md` § Supported
    platform (`:19-40`): add one line that the desktop build requires Windows **only if**
    [[FND-039]] (plan handle `DSK-02-14`) has not already added the `winapp` CLI and Developer Mode
    prerequisites there — that sentence is [[FND-039]]'s to own, cited here and never restated a
    second time. Record in the proof which case applied. The section already lists
    `scripts/email-eval-desktop` under "What Windows gives this project that Linux does not", so
    there is a precedent line to sit beside.
15. **Verify, simplify, open the PR.** Run the § Verification commands below. Run the
    simplification pass over this branch's own diff, record it under a dated
    `## Simplification pass` heading in this document, and open the PR into `dev`.

## Verification

Evidence tier **1 — Static/build/architecture** (`docs/engineering.md` § Required evidence tiers,
`:76`), as the ticket body states: this obliges a compiling, launching project and the enforced
solution shape, and **proves consistency only**. No operator capability is claimed — a launched
empty shell is not a delivered feature, and the proof must not imply that it is.

The `proof` document is produced from these five outputs.

- **V1.** `dotnet restore ./Pegasus.slnx --locked-mode` on Windows — expected exit 0. Run it
  *after* committing `src/Pegasus.Desktop/packages.lock.json`; this is the exact command the CI
  composite action runs.
- **V2.** `dotnet build ./Pegasus.slnx --configuration Release --no-restore` — expected exit 0 and
  `0 Warning(s)`. **This is the authoritative gate**, because it is what
  `.github/actions/dotnet-build/action.yml:22-27` runs and because it sees the repository-root
  `Directory.Build.props` (see § Risks). Paste the warning summary line, not just the exit code.
- **V3.** `pwsh .codex/skills/winui-dev-workflow/BuildAndRun.ps1 src/Pegasus.Desktop/Pegasus.Desktop.csproj -SkipRun`,
  then the same command async without `-SkipRun` — expected output containing
  `✅ <pkg> launched (PID: …)` and a visible window. Attach the **screenshot**; it is the only
  evidence that package identity actually worked.
- **V4.** `dotnet test ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj --configuration Release`
  — expected: every fact green, including `ApplicationSolutionExcludesSourceWorkspaces` with the
  extended eight-path array. Run it once *before* extending the array to show it red if that is
  cheap; a demonstrated red-then-green is stronger evidence that the coupling is real than a green
  alone.
- **V5.** **Measure the package size.** Report the size of the produced MSIX (or of
  `bin\x64\Release\net10.0-windows10.0.26100.0\win-x64\`) in MB. Plan 02 § 7 requires it
  ("acceptable for ten users but measure and record in 09's release manifest") and [[REL-002]] is
  owed the figure. If it is not captured here it will simply be re-measured later.

**Honesty clauses for the proof.**

- A green `BuildAndRun.ps1` is **not** the same claim as a green `dotnet build` — record both, and
  where their warning counts differ, say so and treat V2 as authoritative.
- No CI job builds a desktop project until [[FND-040]] lands, so a green `repository-check` run
  proves only that the architecture test and the locked restore still pass — not that the desktop
  builds. Say that, rather than letting a green badge imply it.
- If [[FND-028]] has not landed, state plainly that `dotnet build ./Pegasus.slnx` now fails on Linux
  and that the repository is Windows-only for developers until it does (`docs/runbook.md:38` —
  "record the platform actually exercised").

## Risks / open questions

- **Risk — `BuildAndRun.ps1` shadows the repository-root `Directory.Build.props`, and the ticket
  body's stated reason for step 8 is the inverse of the measured behaviour.** Plan 02 § 7 and the
  body's step 7 say the script injects "only when none exists up the tree", concluding "it will skip
  injection". Measured at `.codex/skills/winui-dev-workflow/BuildAndRun.ps1:142-172`: `:146-149`
  builds `$tempBuildProps` from **the project directory only**; `:152-154` tests `Test-Path` against
  that exact path; `:157` writes the file when it is absent. It does **not** look up the tree. With
  `src/Pegasus.Desktop/Directory.Build.props` absent the script therefore *does* inject, and MSBuild
  stops at the first `Directory.Build.props` it finds walking up — so the injected file shadows the
  root one for that build, silently dropping `TreatWarningsAsErrors`, `Nullable`, `ImplicitUsings`,
  `LangVersion` and `Version`. `:198-205` deletes only the file the script itself created.
  *Mitigation*: the instruction is followed unchanged (step 8), and V2 rather than V3 is the
  authoritative gate. The disagreement is **reported, not silently applied** — the body outranks
  this plan on what to do; only the stated reason is corrected here. Committing a
  `src/Pegasus.Desktop/Directory.Build.props` that imports the root one would make the script log
  "skipped (existing Directory.Build.props)" (`:170`) and end the shadowing permanently — that is a
  suggestion for [[FND-032]] or [[FND-040]], not a change this ticket takes on its own authority.
- **Risk — A-FND030-1: Windows App SDK 2.4.x may not compile against SDK `10.0.302` with
  `allowPrerelease: false`** (plan 02 § 2 assumption A1). *Settled by*: the first build at step 12.
  *If wrong*: a `global.json` bump is needed, which this ticket's Guardrails put in **its own
  ticket**. The scaffold then stalls, and that is recorded — never worked around by editing
  `global.json` here.
- **Risk — A-FND030-2: stripping the template's version literals may break its `x:Bind` /
  source-generator wiring** (plan 02 § 2 assumption A2). *Settled by*: the build at step 12 after
  step 7's strip. *If wrong*: the offending package keeps a literal version **with a comment naming
  why**, and the deviation from "no version literal remains" is recorded in the ticket rather than
  hidden.
- **Risk — A-FND030-3: the template may not compile clean under `TreatWarningsAsErrors=true` plus
  `AnalysisLevel=latest-recommended`.** *Settled by*: step 13's zero-warning build. *If wrong*: the
  honest outcome is a longer, individually-commented `NoWarn` list — never a relaxation of the root
  props.
- **Risk — A-FND030-5: an ARM64 workstation.** `BuildAndRun.ps1:89` would append
  `/p:Platform=ARM64` into a project declaring `<Platforms>x64</Platforms>`. *Settled by*:
  `$env:PROCESSOR_ARCHITECTURE`. *Mitigation*: pass `/p:Platform=x64` explicitly and record it. Do
  **not** widen `<Platforms>` — plan 02 § 3 decision 3 fixes x64 only.
- **Risk — the desktop `packages.lock.json` breaks every CI lane if stale.** It is RID-specific
  (plan 02 § 7: "CI must restore with the same RID") and the cache key in
  `.github/actions/dotnet-build/action.yml` already globs `src/**/packages.lock.json`.
  *Mitigation*: step 11 generates it with `-r win-x64 --force-evaluate`, and V1 proves the locked
  restore before the PR opens.
- **Risk — [[FND-027]] has not landed and `Directory.Packages.props` does not exist.** Confirmed
  absent today. *Mitigation*: step 2 checks first. If it is still absent, record the sequencing and
  wait; do not inline version literals that step 7 would then have to strip again.
- **Scope boundary, not an open question — the package identity strings.** They are assigned to the
  **operator** inside this ticket's own step 4, which is why the ticket carries the `needs-operator`
  label. Blocking `leave-preparing` on them would stop the ticket ever reaching the step that asks.
  No `open-questions` document is opened for them.
- **Scope boundary, not an open question — the shell.** [[FND-033]] (plan handle `DSK-02-08`) owns
  it, and it must be a `NavigationView`, not a port of
  `src/Pegasus.Web/Pages/Shared/_Layout.cshtml`.
- **Scope boundary, not an open question — the desktop dependency rules and the no-WebView test.**
  [[FND-037]] owns them; this ticket only extends the *solution contents* array. No WebView2
  reference is added here under any circumstances; the sole permitted WebView2 use is the isolated
  report renderer under ADR-0108, which is area 07.
- **Scope boundary, not an open question — the CI lane and the runbook prerequisite sentence.**
  [[FND-040]] owns `.github/workflows/ci.yml`; [[FND-039]] owns the `winapp` CLI and Developer Mode
  sentence in `docs/runbook.md` § Supported platform.
- **No open question is opened on this ticket.** Nothing here is unsettled in a way that must be
  answered before implementation begins. Every assumption above names the command inside the ticket
  that settles it, and the two unfixed strings are an operator step, not a research gap.

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over this branch's own
diff before the PR, recorded here under a dated heading._

## Toolchain and prerequisite detection — 2026-08-26

Read-only detection was run in the FND-030 worktree before any product file was written:

- `dotnet --list-sdks` — `10.0.204`, `10.0.303`; SDK requirement satisfied.
- `winapp --version` — `0.3.1`; minimum `0.3) satisfied.
- `dotnet new list winui | Select-String 'winui-mvvm'` — WinUI MVVM template found.
- `HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\AppModelUnlock\AllowDevelopmentWithoutDevLicense` — `1); Developer Mode enabled.
- ADR-0100 exists and is accepted; `Directory.Packages.props` exists from FND-027.
- `Pegasus.Server.slnf` is absent on `origin/dev` because FND-028 is not merged yet; the later proof must state that Linux remains Windows-only until FND-028 lands.

No installation, UAC elevation, certificate operation, cloud write, deployment, or upstream operation was attempted.

## Operator handback — pending before file creation

FND-030 requires the operator to confirm the permanent package identity before writing the manifest:

- `Identity/@Name`: the current area assumption is `CollisionEngineers.Pegasus`, but the ticket requires explicit confirmation.
- `Identity/@Publisher`: the exact distinguished name matching the subject of the self-managed production certificate is not present in the repository and cannot be inferred.

No project files have been written until both values are confirmed verbatim.

## Operator confirmation — 2026-08-28

The operator explicitly confirmed the permanent package identity values before project-file creation:

- `Identity/@Name`: `CollisionEngineers.Pegasus`
- `Identity/@Publisher`: `CN=Collision Engineers`

Use these exact strings in `Package.appxmanifest`. The Publisher value is the required manifest/certificate-subject match under D-002; no certificate was created, changed, or uploaded by this task.

## Implementation checkpoint — 2026-08-28

- Operator confirmation is fixed verbatim: `Identity.Name=CollisionEngineers.Pegasus`; `Identity.Publisher=CN=Collision Engineers`.
- `dotnet new winui-mvvm -n Pegasus.Desktop -o src/Pegasus.Desktop` created the project in the existing task worktree. The generated underscore namespace was changed to `Pegasus.Desktop` so the repository's warnings-as-errors policy remains clean.
- The project now targets `net10.0-windows10.0.26100.0`, x64, `win-x64`, self-contained .NET and Windows App SDK, with ReadyToRun and trimming disabled. Windows App SDK is centrally pinned to stable `2.4.0` (release date recorded in the area plan); BuildTools is `10.0.26100.7705`, WinApp support is `0.3.1`, and CommunityToolkit.Mvvm is `8.4.2`.
- `dotnet package search Microsoft.WindowsAppSDK.Analyzers` returned no NuGet package. The explicit analyzer reference therefore uses the vendored DLL in the desktop project; a project-local `Directory.Build.props` imports the vendored XAML-file target while importing the repository root props, so plain builds preserve the repository policy and BuildAndRun does not shadow it. This is the necessary implementation of the ticket's analyzer requirement, not a fabricated package dependency.
- Manifest identity is `CollisionEngineers.Pegasus` / `CN=Collision Engineers`, version `0.1.0.0`; the version is a scaffold placeholder for the later release-version ticket.
- Added the desktop project to `Pegasus.slnx` and the architecture solution-content expectation. Added the desktop client to current-architecture documentation. The runbook prerequisite sentence remains owned by [[FND-039]] and was not duplicated.
- Packaged launch evidence: `BuildAndRun.ps1 ...` reported `CollisionEngineers.Pegasus_e6z0b4cw4baw launched (PID: 104480)`; screenshot captured at `artifacts/fnd-030/desktop-launch.png`. The app responded and was closed cleanly after capture.
- Self-contained Release payload measurement: 522 files, 235,921,659 bytes (224.99 MiB) at `src/Pegasus.Desktop/bin/Release/net10.0-windows10.0.26100.0/win-x64`.

## Simplification pass — 2026-08-28

- Removed template inline package versions in favour of the repository's existing central package-management file.
- Kept the analyzer wiring to one explicit project reference plus one local target import; no NuGet package or duplicate architecture layer was added.
- Removed template platform alternatives, trimming, and ReadyToRun defaults that contradicted the ticket's fixed x64/self-contained target.
- Replaced the generated underscore namespace instead of suppressing CA1707. No unrelated source, server, cloud, upstream, or certificate changes were made.

## Independent review correction — 2026-08-28

The independent review of exact head `dc6bd81c0c9e5f8f73d3dd0642c72ddedf338ad7` found two implementation/evidence corrections, which were applied in the task worktree:

- Added `AutomationProperties.AutomationId` and `AutomationProperties.Name` to both starter counter buttons in `src/Pegasus.Desktop/MainPage.xaml`.
- Removed trailing whitespace from `src/Pegasus.Desktop/app.manifest`.
- Corrected `docs/current-architecture.md` to describe a packaged desktop scaffold with local launch evidence; the gateway caller remains future work owned by the desktop feature tickets.
- Rebuilt the desktop and full solution after the correction: both Release builds passed with 0 warnings and 0 errors; architecture tests passed 111/111; `git diff --check` passed.

The simplification evidence is now explicit by required lens:

- Reuse: retained the vendored `winui-mvvm` composition and repository central package management; no second client framework, package source, or business-policy owner was added.
- Simplification: kept x64, self-contained, no-trimming/no-AOT scope; removed the generated underscore namespace issue instead of suppressing CA1707; no NoWarn entries were required.
- Efficiency: used one project-local props import to preserve the root warning policy and analyzer target without adding an analyzer package that NuGet does not provide; no cache, service, or compatibility layer was introduced.
- Altitude: kept shell, gateway, UI test lane, signing, and runbook prerequisites in their owning tickets; corrected the as-built sentence rather than implying future gateway behavior is already implemented.
- Unapplied finding: exact-head CI was still running at the time of this correction and must complete on the new commit before merge. Proof remains intentionally deferred until merged-main verification.

## Independent review correction 2 — 2026-08-28

The fresh review's evidence and truthfulness findings were addressed:

- The corrected XAML was rebuilt and relaunched from exact task head `93ff2663364b05293f25832c6aa7fd5b10c90687` using `BuildAndRun.ps1`. It reported `0 Warning(s)` and `0 Error(s)`, launched package identity `CollisionEngineers.Pegasus_e6z0b4cw4baw0` as PID `119016`, and the responsive `Pegasus.Desktop` window was captured at `artifacts/fnd-030/desktop-launch-final.png`. The process was closed cleanly after capture.
- The current-architecture diagram now marks Desktop → Core and Desktop → Contracts as planned dependencies because the scaffold intentionally has no `ProjectReference`; the component row remains the future allowed boundary.
- Corrected the evidence statement: exact-head `unit` CI compiles the full `Pegasus.slnx`, including the desktop scaffold; [[FND-040]] still owns the dedicated desktop build/package/UI lanes.
- Exact task-head PR CI run `33202445712` is the run for this commit and must be green before merge.
