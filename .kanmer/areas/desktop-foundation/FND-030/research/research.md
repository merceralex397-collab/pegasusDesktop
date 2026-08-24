# Research — FND-030: scaffolding `src/Pegasus.Desktop` as a packaged WinUI 3 project

## Question

What exactly must the scaffolded WinUI 3 project declare, what does the repository already fix that
constrains it, and which of the vendored toolchain's stated behaviours are true as vendored rather
than as described?

## Current behaviour

**No parity-matrix row covers this ticket, and none should.** The matrix at
`docs/desktop/01-inventory-and-parity/parity-matrix.md` holds `PAR-01`…`PAR-46` — counted with
`grep -c '^| PAR-'`, which returns **46** — and every row is "keyed by the Razor page model and
handler group that implements it today" (`parity-matrix.md:3-5`). Creating a project is build and
tooling work with no operator-observable capability, so it is outside the matrix by construction.

The closest existing repository mechanisms — what does this job today:

- **The only desktop project in the repository is
  `scripts/email-eval-desktop/Pegasus.EmailEvaluation.Desktop.csproj`** (19 lines): `WinExe`,
  `net10.0-windows`, `UseWindowsForms=true`, referencing `Pegasus.Core` and `Pegasus.Infrastructure`.
  It is deliberately **outside** `Pegasus.slnx` (`docs/adr/0016-standalone-desktop-email-evaluator.md`
  is present in `docs/adr/`) and stays there. It is the precedent for "a Windows-target project in
  this repository", and also the counter-example: it references `Pegasus.Infrastructure`, which the
  new desktop projects are forbidden to do.
- **The build entry point for every existing project** is `Pegasus.slnx` (14 lines), restored and
  built by the composite action `.github/actions/dotnet-build/action.yml:22-27`
  (`dotnet restore ./Pegasus.slnx --locked-mode`, then a Release build).
- **The vendored WinUI toolchain** is `.codex/skills/winui-setup/`, `.codex/skills/winui-dev-workflow/`
  (`SKILL.md`, `BuildAndRun.ps1`, `analyzer/`), `.codex/skills/winui-design/` (with
  `winui-search.exe` and three reference files) and `.codex/skills/winui-code-review/`. All exist.

## Findings

### Facts

Verified by reading the repository at fork `main`, 2026-08-24. Each carries its source.

- **`src/Pegasus.Desktop` does not exist.** `ls src` returns exactly `Pegasus.Core`,
  `Pegasus.Infrastructure`, `Pegasus.Web`, `Pegasus.Worker`. No `Microsoft.WindowsAppSDK` reference,
  MSIX manifest or `WebView2` reference exists anywhere under `src/` or `tests/`.
- **`global.json` pins the SDK**: `{"sdk":{"version":"10.0.302","rollForward":"latestFeature","allowPrerelease":false}}`.
  `allowPrerelease: false` matters — a Windows App SDK preview that needs a preview .NET SDK cannot
  be adopted without changing this file, which the ticket Guardrails forbid.
- **`Directory.Build.props` is 19 lines** and sets, for every project: `Nullable=enable`,
  `ImplicitUsings=enable`, `LangVersion=latest`, `Deterministic=true`,
  `AnalysisLevel=latest-recommended`, `TreatWarningsAsErrors=true`, `Version=0.1.0-alpha.1`, and
  `PlaywrightVersion=1.61.0`. The comment at `:8-19` records that `PlaywrightVersion` is shared with
  `src/Pegasus.Web/Pegasus.Web.csproj`'s `ContainerBaseImage`.
- **`Directory.Packages.props` does not exist** (`ls Directory.Packages.props` → *No such file or
  directory*), so central package management is genuinely introduced by [[FND-027]] (plan handle
  `DSK-02-02`), not merely extended. Step 6 of this ticket writes `PackageVersion` entries into a
  file [[FND-027]] creates.
- **`BuildAndRun.ps1`'s analyzer injection does the opposite of what plan 02 § 7 describes, and the
  consequence is larger.** Measured at `.codex/skills/winui-dev-workflow/BuildAndRun.ps1:142-172`:
  - `:146-149` — `$projectDir = Split-Path (Resolve-Path $Project) -Parent`; `$tempBuildProps = Join-Path $projectDir "Directory.Build.props"`.
  - `:152-154` — `if (Test-Path $tempBuildProps) { $existingProps = Get-Content $tempBuildProps -Raw }`.
  - `:157` — `if (-not $existingProps)` then it **writes** a `Directory.Build.props` into the project
    directory containing only an `<Analyzer Include="…"/>` item and an `<Import Project="…targets"/>`.
  - `:198-205` — a `finally` block deletes the file it created.
  The existence test is against **the project directory only**, not up the tree. With
  `src/Pegasus.Desktop/Directory.Build.props` absent, the script therefore **injects**, and MSBuild's
  implicit `Directory.Build.props` discovery stops at the first file found walking up — so the
  injected file **shadows the repository-root props for the duration of that build**, silently
  dropping `TreatWarningsAsErrors`, `Nullable`, `ImplicitUsings`, `LangVersion` and `Version` for the
  desktop project. Neither file imports the one above it, so nothing restores them.
  - *Relationship to the ticket body*: the body's **instruction** at step 7 — reference
    `Microsoft.WindowsAppSDK.Analyzers` explicitly in the desktop csproj — is followed unchanged and
    is if anything more necessary than stated. Only the body's stated **reason** ("it will skip
    injection") is contradicted by the measured code; the corrected reason is recorded here and
    carried into the plan, and the disagreement is reported rather than silently applied.
- **The analyzer binaries are vendored**: `.codex/skills/winui-dev-workflow/analyzer/` holds
  `Microsoft.WindowsAppSDK.Analyzers.dll` and `Microsoft.WindowsAppSDK.Analyzers.targets`.
  `BuildAndRun.ps1:133-138` prefers those and falls back to a `tools/winui-analyzer/…` source tree
  that does **not** exist in this repository — so the vendored pair is the only path that resolves.
- **`BuildAndRun.ps1` auto-detects the platform** at `:89` (`ARM64` if
  `$env:PROCESSOR_ARCHITECTURE -eq "ARM64"`, else `x64`) and appends `/p:Platform=$detectedPlatform`
  at `:101` unless the caller passed one; `:228` expects output at
  `bin\<Platform>\<Config>\<tfm>\win-<rid>\`. A project restricted to `<Platforms>x64</Platforms>`
  therefore fails on an ARM64 workstation unless `/p:Platform=x64` is passed explicitly.
- **The `winui-setup` detection block is executable and specific**
  (`.codex/skills/winui-setup/SKILL.md`, `disable-model-invocation: true` in its frontmatter, so it is
  user-invoked): `dotnet --list-sdks` accepting any SDK ≥ 8; `winapp --version` requiring ≥ 0.3 after
  stripping a `-prerelease.N` suffix; `dotnet new list winui | Select-String 'winui-mvvm'`; and the
  registry read `HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock` →
  `AllowDevelopmentWithoutDevLicense -eq 1`. The skill states the WinApp CLI and templates should be
  upgraded to latest even when present.
- **`winui-dev-workflow` § Critical Rules and § Common Errors** (`.codex/skills/winui-dev-workflow/SKILL.md`)
  fix four things this ticket must honour: scaffold with `dotnet new winui-mvvm -n <AppName>` and do
  **not** `mkdir` first (the template creates the folder); invoke `BuildAndRun.ps1` **async** because
  it stays attached, with success looking like `✅ <pkg> launched (PID: …)`; never run the packaged
  `.exe` directly (`App silently exits` → "Use `winapp run`"); and
  `MSB3073 / XamlCompiler.exe … exited with code 1` naming no `.xaml` is the old XAML-compiler bug —
  "update `Microsoft.WindowsAppSDK` NuGet to latest (≥ 2.1.3, or ≥ 1.8 on the 1.x line)".
- **`Pegasus.slnx` and the pinned solution fact.**
  `tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs:128` declares
  `ApplicationSolutionExcludesSourceWorkspaces`; the expected seven-path array is `:137-149`, ordered
  `StringComparer.Ordinal`. `src/Pegasus.Desktop/Pegasus.Desktop.csproj` sorts after
  `src/Pegasus.Core/…` and before `src/Pegasus.Infrastructure/…`.
- **The lock-file shape for a zero-package project** is `src/Pegasus.Core/packages.lock.json`
  (124 bytes, entries `net10.0`, `net10.0/linux-x64`, `net10.0/win-x64`). The desktop project will
  produce a much larger one, RID-specific to `win-x64` — plan 02 § 7 records that as a trap.
- **CI is Windows-heavy but does not build a desktop project today.** `.github/workflows/ci.yml`
  defines nine jobs: `changes` (`:15`, ubuntu), `documentation` (`:76`), `local-development-scripts`
  (`:92`), `reference-data` (`:104`), `infrastructure` (`:120`), `unit` (`:136`), `sql-integration`
  (`:160`), `sql-integration-coverage` (`:194`, ubuntu), `browser` (`:210`) — seven on
  `windows-latest`. The desktop lane is [[FND-040]] (plan handle `DSK-02-15`); this ticket adds no
  job.
- **The package identity is already written down.**
  `docs/desktop/09-release-update-and-distribution/README.md:156-158`: "Package identity
  `CollisionEngineers.Pegasus`, one identity for both channels; the Publisher string is the subject of
  the self-managed certificate"; `:216` fixes "the subject fixed to the manifest `Publisher` and a
  ~3-year validity"; `:289` assigns issuing the production certificate to [[REL-007]] (plan handle
  `DSK-09-08`); `:329` lists "**Publisher mismatch** between certificate and `Identity.Publisher`" as
  a recorded trap. D-002 (`docs/desktop/README.md` § Locked decisions) is the decision behind all of
  it. `CollisionEngineers.Pegasus` is a plan **assumption**, not an operator confirmation — step 3 of
  this ticket is where it becomes fixed.
- **`docs/current-architecture.md` has both target sections**: § System shape at `:27` and
  § Components and dependency direction at `:55`.
- **`docs/runbook.md` § Supported platform is `:19-40`** and currently states that repository
  development supports Windows and Linux, that release operations are Windows-only (ADR-0007), and at
  `:38` "record the platform actually exercised". It lists `scripts/email-eval-desktop` under "What
  Windows gives this project that Linux does not" because it "targets `net10.0-windows` with Windows
  Forms, which has no Linux implementation".

Official documentation, as recorded in plan 02 § 2 (fetched 2026-08-23 by the plan author; re-confirm
at kickoff per step 6):

- Windows App SDK 2.x stable line: 2.0 (2026-04-29), 2.1.3 (2026-05-21), 2.2.0 (2026-06-09),
  2.3.1 (2026-07-16), **2.4.0 (2026-08-13)** —
  <https://learn.microsoft.com/windows/apps/windows-app-sdk/release-notes/windows-app-sdk-2-0>.
- `winapp init` sets `TargetFramework` to `net10.0-windows10.0.26100.0` and adds
  `Microsoft.WindowsAppSDK`, `Microsoft.Windows.SDK.BuildTools` and
  `Microsoft.Windows.SDK.BuildTools.WinApp` —
  <https://learn.microsoft.com/windows/apps/dev-tools/winapp-cli/guides/dotnet>.
- Single-project MSIX for WinUI 3 needs no separate packaging project —
  <https://learn.microsoft.com/windows/apps/windows-app-sdk/single-project-msix>.
- Self-contained single-file EXE applies only to *unpackaged* apps; packaged apps use MSIX —
  <https://learn.microsoft.com/windows/apps/package-and-deploy/unpackage-winui-app#single-file-exe>.

### Assumptions

- **A-FND030-1 — `Microsoft.WindowsAppSDK` 2.4.x compiles against SDK `10.0.302` with
  `allowPrerelease: false`.** (Plan 02 § 2 assumption A1.) *Confirms it*: the first scaffold build at
  step 11. *If wrong*: a `global.json` bump is required, which the Guardrails put in its own ticket —
  so the scaffold stalls until that ticket exists, and this must be recorded rather than worked
  around by editing `global.json` here.
- **A-FND030-2 — the `winui-mvvm` template's own package versions can be stripped to
  `Directory.Packages.props` without breaking the template's `x:Bind`/source-generator wiring.**
  (Plan 02 § 2 assumption A2.) *Confirms it*: the build at step 11 after step 6's version-literal
  strip. *If wrong*: the offending package keeps a literal version with a comment naming why, and the
  deviation from "no version literal remains" is recorded in the ticket rather than hidden.
- **A-FND030-3 — the template compiles clean under `TreatWarningsAsErrors=true` plus
  `AnalysisLevel=latest-recommended` after narrow `NoWarn` entries.** *Confirms it*: step 12's build
  with zero warnings. *If wrong*: the honest outcome is a longer, individually-commented `NoWarn`
  list in the desktop csproj — never a relaxation of `Directory.Build.props`.
- **A-FND030-4 — a `BuildAndRun.ps1` build and a plain `dotnet build` of the same project produce
  the same diagnostics.** Given the injection finding above, this is **probably false** unless
  `src/Pegasus.Desktop/Directory.Build.props` exists. *Confirms it*: build both ways at step 11 and
  compare warning counts. *If wrong*: the `BuildAndRun.ps1` path is the weaker gate and CI's plain
  `dotnet build` is authoritative — say so in the proof rather than reporting the script's green as
  the whole evidence.
- **A-FND030-5 — the developer workstation is x64.** `BuildAndRun.ps1:89` detects ARM64 and would
  pass `/p:Platform=ARM64` into a project declaring `<Platforms>x64</Platforms>`. *Confirms it*:
  `$env:PROCESSOR_ARCHITECTURE`. *If wrong*: pass `/p:Platform=x64` explicitly and record it.

## Execution placement

The six-question cloud-justification test from
`docs/desktop/00-governance-and-workflow/README.md` § 3, answered.

| Question | Answer | Evidence |
| --- | --- | --- |
| Shared authority — must several users see and update the same state? | **No** | The scaffold is a client shell with no state of its own; ADR-0104 (online-required, bounded local cache) governs anything it later holds, and this ticket adds no cache. |
| Unattended execution — must it run with every desktop closed? | **No** | The desktop runs when an operator runs it. Unattended work stays in `Pegasus.Worker` under ADR-0106 (Graph intake worker stays central), which this ticket does not touch. |
| Protected credentials — a long-lived secret that must not sit on workstations? | **No for this ticket, and it names where the one nearby credential does land.** | The package carries no secret: `docs/desktop/04-auth-session-update-and-startup/README.md` § 3 item 8 — "Secrets in the package: none". The production signing key is a real credential responsibility, and under **D-002** it lands on the **in-house signing host**, held by [[REL-007]] (plan handle `DSK-09-08`, "key on the signing host with a restricted ACL", plan 09 `:289`) — an in-house host, not Azure. This ticket only fixes the `Identity.Publisher` string that key's subject must equal. |
| Public callback — must an external service call a stable public endpoint? | **No** | No endpoint and no callback is introduced. Under **D-003** the update feed is a UNC share served over SMB, reached from the office network or VPN — deliberately not a public HTTPS endpoint (C-01 rules GitHub Releases and Pages out permanently). |
| Central enforcement — revocation, permissions, audit or an invariant independent of the client? | **Yes — and it lands on the already-existing evolved `Pegasus.Web` gateway plus the in-house feed host, not on any new Azure resource.** | Package identity is precisely what makes central enforcement possible: ADR-0105 (MSIX + gateway minimum-version gate, two-layer) is enforced server-side by the compatibility middleware on `/api/v1` (plan 04 § 3 item 5), and the minimum version is "a database-backed Administrator setting with audit … not a Container App app setting", so raising it is an authenticated administrative action and **not** an Azure write. The second layer, the App Installer feed, sits on the in-house UNC host under D-003. |
| Measured operational advantage — measured evidence that central is materially better? | **No** | None claimed. The placement follows from ADR-0105 and D-002/D-003, not from a benchmark. Plan 02 § 7 does require the self-contained MSIX size to be *measured* and recorded for [[REL-002]] (plan handle `DSK-09-02`), but that is a size figure, not an argument for central placement. |

**Conclusion.** Four "no" and one "yes"; the "yes" names the existing gateway and the in-house feed
host, and the one genuine credential nearby is placed on an in-house signing host by D-002 and owned
by a different ticket. Nothing this ticket does places any responsibility in Azure, and no Azure write
arises.

## Implications

1. **The `Publisher` string is the single highest-consequence value in the ticket.** It is permanent
   (changing it makes a different application to Windows), the certificate subject must equal it
   exactly (D-002, plan 09 `:216`, `:329`), and it is the only value here that a later ticket
   ([[REL-007]]) is blocked on. It is therefore an operator step (step 3), not a default to take.
   `CollisionEngineers.Pegasus` is the plan's `Identity.Name` assumption; the `Publisher`
   distinguished name is not written down anywhere in the repository and must come from the operator.
2. **The analyzer reference is load-bearing for a different reason than the body gives.** Because
   `BuildAndRun.ps1` will inject a project-level `Directory.Build.props` that shadows the root one, a
   build through the script is *weaker* than a plain `dotnet build`. Two consequences for the plan:
   reference the analyzer explicitly (as the body instructs), and treat plain
   `dotnet build ./Pegasus.slnx --configuration Release` — the command CI actually runs — as the
   authoritative zero-warning gate, with `BuildAndRun.ps1` used for launching.
3. **`<Platforms>x64</Platforms>` plus `BuildAndRun.ps1`'s auto-detection is a real interaction.** On
   an ARM64 workstation the script appends `/p:Platform=ARM64` and the build fails; the plan must say
   to pass `/p:Platform=x64` where that applies rather than widening `<Platforms>`.
4. **`allowPrerelease: false` narrows the Windows App SDK choice.** Only a **stable** 2.x may be
   pinned, and the floor is 2.1.3 because of the `MSB3073` XAML-compiler bug named in the vendored
   skill's error table. 2.4.0 (2026-08-13) is the plan's recorded latest.
5. **This ticket is where the desktop leaves the Linux build.** `Pegasus.slnx` gains the project and
   the server entry point from [[FND-028]] (plan handle `DSK-02-03`) must **not**, which is the
   moment [[FND-028]]'s `ServerSolutionFilterExcludesWindowsTargetedProjects` fact starts doing real
   work rather than asserting a tautology.
6. **The MSIX size figure is an obligation, not a nicety.** Plan 02 § 7 requires measuring the
   self-contained package size and recording it for [[REL-002]]'s release manifest; the plan's
   verification must capture it.

## Open questions

- None that must be answered before implementation. The two values that are not yet fixed — the
  permanent `Identity.Name` and `Identity.Publisher` — are assigned to the **operator** inside the
  ticket's own step 3, which is a `needs-operator` step this ticket already carries as a label; they
  are not questions this plan can or should resolve, and blocking `leave-preparing` on them would
  stop the ticket from reaching the step that asks. The five assumptions above each name the command
  inside the ticket that settles them.
- One finding is reported rather than opened as a question: `BuildAndRun.ps1`'s injection condition
  contradicts the reason plan 02 § 7 and this ticket's step 7 give for the explicit analyzer
  reference. The instruction is unchanged and is followed; the corrected mechanism is recorded in the
  plan's Risks section so the implementer is not surprised by a shadowed props file.
