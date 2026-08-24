# 02 · Architecture and foundation (Phase 1)

Owner of the solution shape, central build, desktop host composition,
single-instance lifecycle, diagnostics bundle, and the dependency rules that
every later area inherits. It is the second work package after
[01 · inventory and parity](../01-inventory-and-parity/README.md) and the
prerequisite for [04](../04-auth-session-update-and-startup/README.md) and
every slice in [05](../05-implementation-and-migration/README.md).

## 1. Purpose and proposal coverage

Deliver the proposal's Phase 1 exit gate: a clean Windows 11 machine launches
the native Pegasus shell from a dev-signed MSIX, with no WebView, passing
foundation tests, with install/uninstall proven and the architecture
boundaries enforced by tests.

| Proposal section | Covered here |
| --- | --- |
| §5.2 deployment units, §5.3 native desktop layers, §5.4 solution structure | Solution evolution, dependency direction, project list |
| §7.1 runtime, §7.2 application composition, §7.3 single instance | TFM, Windows App SDK pin, packaging, Host/DI/logging, `AppInstance` |
| §11.1 what may be cached locally | Local cache and credential-store placement (implementation in 04) |
| §16.3 crash recovery (diagnostics), §18.1 desktop diagnostics | Unhandled-exception path and diagnostics bundle |
| §21.1 build properties | `Directory.Build.props`, CPM, lock files, RID, signing references |
| §24 Phase 1 | Work breakdown and exit gate |

Out of scope here: endpoints and contracts ([03](../03-gateway-api-and-data/README.md)),
token flow and startup gate ([04](../04-auth-session-update-and-startup/README.md)),
visual design tokens ([06](../06-ui-design/README.md)), packaging channels and
signing ([09](../09-release-update-and-distribution/README.md)).

## 2. Evidence base

### Facts

Repository (fork `main` at `191ddf33`, read 2026-08-23):

- `Pegasus.slnx` lists exactly four production projects and three test
  projects (`src/Pegasus.Core`, `src/Pegasus.Infrastructure`,
  `src/Pegasus.Web`, `src/Pegasus.Worker`; `tests/Pegasus.ArchitectureTests`,
  `tests/Pegasus.Core.Tests`, `tests/Pegasus.IntegrationTests`).
- `Directory.Build.props` (root, 19 lines) sets `Nullable`, `ImplicitUsings`,
  `LangVersion=latest`, `Deterministic=true`, `AnalysisLevel=latest-recommended`,
  `TreatWarningsAsErrors=true`, `Version=0.1.0-alpha.1`, and
  `PlaywrightVersion=1.61.0` (shared by the Infrastructure package reference and
  `src/Pegasus.Web/Pegasus.Web.csproj:28` `ContainerBaseImage`).
- There is no `Directory.Packages.props` (no central package management) and no
  `nuget.config`; `packages.lock.json` exists for all seven projects but
  `RestorePackagesWithLockFile=true` is set only in the three test projects;
  `global.json` pins SDK `10.0.302` (`rollForward: latestFeature`,
  `allowPrerelease: false`); `.config/dotnet-tools.json` pins `dotnet-ef`
  `10.0.10`.
- `src/Pegasus.Core/Pegasus.Core.csproj` targets `net10.0`, has zero package
  references and no project references; Core already separates transport from
  policy: `src/Pegasus.Core/Identity/IdentityContracts.cs` (`ActorKind`,
  `ActionActor`), `src/Pegasus.Core/Identity/StaffAuthorization.cs`
  (`StaffAccessRight`, fail-closed matrix), `src/Pegasus.Core/Actors/StaffActorFactory.cs:8`
  (`TryCreate`), `src/Pegasus.Core/Actors/StaffSessionPolicy.cs:9-13`.
- Core's mutation envelope is already API-shaped:
  `src/Pegasus.Core/Workflow/CaseWorkflowContracts.cs:182` `CaseMutationRequest`
  (`CaseId`, `ExpectedVersion`, `Actor`, `OperationKey`, `Reason`,
  `EditLeaseToken`); `src/Pegasus.Core/Workflow/CaseEditAuthority.cs` owns lease
  validation.
- All production projects declare `RuntimeIdentifiers` `linux-x64;win-x64`
  (`src/Pegasus.Web/Pegasus.Web.csproj:15`, `src/Pegasus.Core/Pegasus.Core.csproj:5`);
  Web and Worker are published `linux-x64` by `scripts/Build-ReleaseArtifacts.ps1`;
  the migration bundle is built `win-x64` (`scripts/Build-ReleaseArtifacts.ps1:70`).
- `tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs` (520 lines)
  guards dependency direction with custom reflection and csproj parsing:
  `CoreHasNoInfrastructureOrHostDependencies` (line 43),
  `ProjectReferencesFollowTheModularMonolithDirection` (line 111),
  `ApplicationSolutionExcludesSourceWorkspaces` (line 128),
  `ApplicationProjectsDoNotReferenceSourceWorkspaces` (line 156). No
  NetArchTest or Mono.Cecil is used.
- CI (`.github/workflows/ci.yml`, one workflow `repository-check`) already runs
  seven of nine jobs on `windows-latest` through the composite
  `.github/actions/dotnet-build/action.yml` (`actions/setup-dotnet@v6`,
  `dotnet-version: 10.0.x`, `dotnet restore --locked-mode`, Release build).
- No `Microsoft.WindowsAppSDK`, MSIX manifest, `WebView2`, WPF/MAUI/Avalonia
  reference exists under `src/` or `tests/`. The only desktop code is the
  WinForms evaluator under `scripts/email-eval-desktop/` (ADR-0016), which is
  deliberately outside `Pegasus.slnx` and stays that way.
- `AGENTS.md § Product invariants`: a new top-level project, runtime,
  deployment unit, or migration stream requires an accepted ADR proving the
  existing boundary cannot carry it; `Pegasus.Core` owns business policy; no
  `Common`/`Helpers`/`Utilities` packages (`docs/current-architecture.md`
  § Architecture invariants).
- Vendored WinUI skills (`.codex/skills/winui-dev-workflow/SKILL.md`,
  `BuildAndRun.ps1`, `.codex/skills/winui-setup/SKILL.md`) prescribe the
  CLI-only loop: `dotnet new winui-mvvm`, `winapp` CLI ≥ 0.3, Developer Mode,
  `.NET SDK ≥ 8 (recommended 10)`, never `<WindowsPackageType>None`, never
  `AnyCPU`, and "do not install Visual Studio".

Official documentation (fetched 2026-08-23):

- Windows App SDK 2.x stable line: 2.0 (2026-04-29, first SemVer release),
  2.1.3 (2026-05-21), 2.2.0 (2026-06-09), 2.3.1 (2026-07-16), **2.4.0
  (2026-08-13)** — https://learn.microsoft.com/windows/apps/windows-app-sdk/release-notes/windows-app-sdk-2-0.
  Package family name aligns with the major version; breaking changes only
  across majors.
- WinUI 3 get started (command-line path): .NET 10 SDK +
  `dotnet new winui -n MyApp`, `dotnet run` builds and launches with package
  identity (Developer Mode required); Visual Studio 2026 is the IDE path, not a
  requirement — https://learn.microsoft.com/windows/apps/get-started/winui-get-started-overview.
- `winapp init` sets `TargetFramework` to `net10.0-windows10.0.26100.0` and adds
  `Microsoft.WindowsAppSDK`, `Microsoft.Windows.SDK.BuildTools`,
  `Microsoft.Windows.SDK.BuildTools.WinApp` —
  https://learn.microsoft.com/windows/apps/dev-tools/winapp-cli/guides/dotnet.
- Single-project MSIX for WinUI 3 (no separate packaging project) —
  https://learn.microsoft.com/windows/apps/windows-app-sdk/single-project-msix.
- Trimming of Windows App SDK apps is possible since 1.2 but must be tested
  thoroughly (reflection against trimmable types) — release notes archive
  1.2; the proposal §7.1 defers AOT/trimming, and this plan keeps that.
- Credential Locker: 20-credential limit applies to AppContainer apps; "only
  use the credential locker for passwords and not for larger data blobs" —
  https://learn.microsoft.com/windows/apps/develop/security/credential-locker.
- Self-contained single-file EXE is only for *unpackaged* apps; packaged apps
  use MSIX — https://learn.microsoft.com/windows/apps/package-and-deploy/unpackage-winui-app#single-file-exe.

### Assumptions

- A1. `Microsoft.WindowsAppSDK` 2.4.x (or the latest 2.x stable at kickoff)
  compiles with SDK 10.0.302 without a `global.json` change. Verify with the
  first scaffold build; if a newer SDK feature band is required, bump
  `global.json` in its own ticket.
- A2. CommunityToolkit.Mvvm and Microsoft.Extensions.Hosting versions current
  at kickoff have no conflicts with the Windows App SDK's `WinRT.Runtime`;
  verify at scaffold time (the `winui-mvvm` template already pairs them).
- A3. GitHub-hosted `windows-latest` can restore the Windows SDK build tools
  packages and run `winapp` (installed with `winget`/`microsoft/setup-WinAppCli`)
  — to be proven by the CI lane ticket, not assumed green.
- A4. A dev-signed MSIX can be installed on a clean Windows 11 test machine
  after trusting the dev certificate (Windows 11 has sideloading enabled by
  default).

## 3. Decisions and assumptions

Locked decisions that bind this area: L-01 (gateway is `Pegasus.Web` evolved in
place), L-02 (Test/UAT is a local stack), L-04 (subagents exist). Relevant ADRs
to write (see [00](../00-governance-and-workflow/README.md)): **ADR-0100**
native WinUI 3 client inside the fork (authorises the new top-level projects),
ADR-0104 online-required (bounds the local cache), ADR-0109 diagnostics and
telemetry retention (bounds the log/bundle design).

Decisions taken in this plan:

1. **Keep `Pegasus.Core` as Domain + Application.** *Deviation* from proposal
   §5.4, which names `Pegasus.Domain` and `Pegasus.Application` as separate
   projects. Reason: the repository invariant is one Core owner of business
   policy (`AGENTS.md § Product invariants`); Core already has zero package
   dependencies and transport-neutral actors, so the split would be a rename
   without a second concrete need, which `docs/engineering.md § Abstractions`
   forbids. Revisit only if a Core type must be excluded from the desktop for
   size or secrecy reasons (none identified in the inventory).
2. **Add four source projects and four test projects** (table below). Each is a
   boundary project; features stay as folders inside them (§5.4 "do not split
   every feature into a separate assembly").
3. **Desktop target**: `net10.0-windows10.0.26100.0`, `Platforms` `x64` only,
   minimum OS 10.0.22000 (Windows 11), packaged single-project MSIX,
   `WindowsAppSDKSelfContained=true`, .NET `SelfContained=true`,
   `RuntimeIdentifier=win-x64`, `PublishReadyToRun=false` and no trimming/AOT
   initially (profile before enabling, §7.1), `Microsoft.WindowsAppSDK` pinned
   centrally to the 2.x stable chosen at kickoff (2.4.0 on 2026-08-13).
4. **Central package management**: introduce `Directory.Packages.props` and set
   `RestorePackagesWithLockFile=true` for every project (today only tests).
   Major Windows App SDK / toolkit upgrades are reviewed PRs, never automatic.
5. **Server projects stay Linux-publishable.** *Deviation (additive)*: the
   proposal does not discuss build matrices. A Windows-TFM project in
   `Pegasus.slnx` breaks `dotnet build Pegasus.slnx` on Linux (the repository
   supports Linux workstations, `docs/runbook.md § Supported platform`).
   Decision: add the desktop projects to `Pegasus.slnx` **and** add a solution
   filter `Pegasus.Server.slnf` (Core, Infrastructure, Web, Worker, their
   tests) used by Linux builds and by the existing Linux-x64 release script;
   Windows CI builds the full `slnx`. The architecture test that pins the
   solution contents is extended rather than bypassed.
6. **Credential store**: DPAPI (`System.Security.Cryptography.ProtectedData`,
   `DataProtectionScope.CurrentUser`) file-backed under the packaged app's
   `ApplicationData.Current.LocalFolder`, not `PasswordVault`. Reason: the
   refresh/session handle may exceed what the Credential Locker guidance calls
   a "password", and DPAPI has no count or size limits. Access token stays in
   memory (§8.2). Implemented in 04; the abstraction lives here.
7. **Host composition**: `Microsoft.Extensions.Hosting` generic host inside
   `App.xaml.cs`, `IHttpClientFactory` single pipeline, structured logging to a
   bounded rolling file sink with redaction, configuration layered as embedded
   `appsettings.json` + `appsettings.<channel>.json` selected by an MSBuild
   property at package time (channel = `pilot` | `production` | `local`).
8. **Single instance per Windows user** via `AppInstance.FindOrRegisterForKey`
   and `RedirectActivationToAsync` before any window is created; redirected
   activations carry deep-link/file arguments to the running instance (§7.3).
   No multi-window in Phase 1.
9. **No desktop framework on top of WinUI**: a shell service, a navigation
   service, a dialog service, and a handful of project controls (§7.2).
10. **Diagnostics bundle** is a foundation feature, not a Phase 8 afterthought:
    export of redacted rolling logs, app/package/Windows/dependency versions,
    last compatibility response, and the single-instance/activation log.

No Azure writes arise in this area.

## 4. Target state and exit gate

Target solution shape after Phase 1:

| Project | Kind | References | Purpose |
| --- | --- | --- | --- |
| `src/Pegasus.Core` | unchanged | none | Domain + application use cases and ports (no split) |
| `src/Pegasus.Contracts` | new, `net10.0` | none (System.Text.Json only) | Request/response/problem-details DTOs, enums-as-strings, paging and concurrency envelopes shared by gateway and desktop; no EF/ASP.NET/WinUI types |
| `src/Pegasus.Infrastructure` | unchanged | Core | Server-side adapters only |
| `src/Pegasus.Web` | evolves (L-01) | Core, Infrastructure, Contracts | Razor Pages + `/api/v1` gateway + token flow |
| `src/Pegasus.Worker` | unchanged | Core, Infrastructure | Unattended work |
| `src/Pegasus.Desktop` | new, `net10.0-windows10.0.26100.0`, x64, MSIX | Core, Contracts, Desktop.Infrastructure | WinUI 3 shell, views, view models, theme, navigation/dialog services |
| `src/Pegasus.Desktop.Infrastructure` | new, `net10.0-windows10.0.26100.0` | Core, Contracts | Generated API client + HTTP pipeline, credential store, bounded cache, diagnostics, Windows integration |
| `tests/Pegasus.Desktop.ViewModelTests` | new, xunit | Desktop, Contracts | View-model behaviour without the dispatcher |
| `tests/Pegasus.Api.ContractTests` | new, xunit | Web, Contracts | OpenAPI snapshot, generated-client compile, serialization |
| `tests/Pegasus.Desktop.UITests` | new, script-driven | — | `winapp ui` batch scripts + harness (see 08) |
| `tests/Pegasus.Packaging.Tests` | new, PowerShell + xunit shim | — | Install/upgrade/blocked/rollback scenarios (see 08/09) |

Dependency direction (enforced by tests): Desktop and Desktop.Infrastructure
must not reference `Pegasus.Infrastructure`, Entity Framework, Azure SDKs,
Box/Graph SDKs, or `Microsoft.AspNetCore.*`; Contracts references nothing
but the BCL/System.Text.Json; Web may reference Contracts; Core unchanged.

Exit gate (proposal §24 Phase 1, made testable):

| Gate | Evidence |
| --- | --- |
| Clean Windows 11 machine launches the native shell from a dev-signed MSIX | `tests/Pegasus.Packaging.Tests` install log + screenshot of the shell (tier 7) |
| No WebView/web dependency in the package | Package content scan: no `Microsoft.Web.WebView2` assembly referenced until ADR-0108 lands; no `WebView2` XAML element (architecture test) |
| Foundation tests pass | `dotnet test` on ViewModelTests + ArchitectureTests (tier 1/2) |
| Install/uninstall works and leaves only intended user settings | Packaging test: install, run, uninstall, `%LOCALAPPDATA%\Packages\<pfn>` removed, DPAPI store removed |
| Architecture boundaries enforced | New `DependencyDirectionTests` facts red on a forbidden reference (prove by a temporary failing fixture) |
| Single instance | Second launch activates the first window (UI test) |
| Diagnostics bundle exports | Bundle zip contains the documented manifest (tier 9) |

## 5. Work breakdown

Profiles are the fork board's (`feature`, `fix`, `chore`, `spike`). Tier =
`docs/engineering.md § Required evidence tiers`. All rows belong to Kanmer
area `desktop-foundation` (prefix `FND`) unless noted; horizon group
`HZN Phase 1`.

| ID | Title | Profile | Depends on | Acceptance | Verification | Tier | Routing |
| --- | --- | --- | --- | --- | --- | --- | --- |
| DSK-02-01 | Author ADR-0100 (native WinUI 3 client in the fork; new projects) and ADR-0104 (online-required) | chore | 00 ADR block agreed | ADRs `proposed`→`accepted`, frontmatter per AGENTS.md, linked from `docs/adr/README.md` | `scripts/Test-DocumentationLinks.ps1` | 1 | `pegasus-desktop-reviewer` · `kanmer-docs` · Kanmer `link_doc` |
| DSK-02-02 | Introduce `Directory.Packages.props` (CPM) and enforce lock files for all projects | chore | — | All `PackageReference` versions centralised; `RestorePackagesWithLockFile` everywhere; `dotnet restore --locked-mode` green on Windows and Linux | CI `repository-check` green; `git diff` shows no version literals left in csproj | 1 | `pegasus-release-packager` · `convert-to-cpm`, `directory-build-organization` · Microsoft Learn |
| DSK-02-03 | Add `Pegasus.Server.slnf` and extend the solution architecture test | chore | DSK-02-02 | Linux `dotnet build Pegasus.Server.slnf` green; `ApplicationSolutionExcludesSourceWorkspaces` updated to the new project set | CI ubuntu job uses the slnf; Windows job uses slnx | 1 | `pegasus-release-packager` · `directory-build-organization`, `binlog-failure-analysis` · — |
| DSK-02-04 | Create `src/Pegasus.Contracts` with envelope types (paging, problem details, concurrency token, operation key) | feature | DSK-02-01 | Project builds with zero non-BCL references; first DTOs are those 03's compatibility endpoint needs | `Pegasus.Api.ContractTests` serialization round-trip | 2 | `pegasus-gateway-dev` · `dotnet-webapi`, `microsoft-code-reference` · Microsoft Learn |
| DSK-02-05 | Scaffold `src/Pegasus.Desktop` (`dotnet new winui-mvvm`), x64, packaged, self-contained, pinned WinAppSDK 2.x | feature | DSK-02-01, DSK-02-02 | Builds with `BuildAndRun.ps1`; launches with package identity; no `AnyCPU`; `Package.appxmanifest` identity placeholders documented | `BuildAndRun.ps1 -SkipRun` then `winapp run` log; screenshot | 1 | `winui-dev` · `winui-setup`, `winui-dev-workflow`, `winui-design` · Microsoft Learn |
| DSK-02-06 | Create `src/Pegasus.Desktop.Infrastructure` (HTTP pipeline via `IHttpClientFactory`, headers `X-Pegasus-Client-Version`/`X-Correlation-Id`, DPAPI credential store, bounded cache, diagnostics writer) | feature | DSK-02-04, DSK-02-05 | Interfaces live in Desktop.Infrastructure or Core; no Infrastructure/EF/Azure references | Architecture test + unit tests for credential store round-trip | 2 | `winui-dev` · `winui-dev-workflow`, `microsoft-code-reference` · Microsoft Learn (`ProtectedData`) |
| DSK-02-07 | Generic Host, DI, options, logging (rolling file, bounded, redacted), channel-selected configuration | feature | DSK-02-05 | `App.xaml.cs` builds the host; services resolvable in VM tests; logs rotate and redact tokens/PII | ViewModelTests with a fake host; log fixture asserts redaction | 2 | `winui-dev` · `winui-dev-workflow` · Microsoft Learn (`Microsoft.Extensions.Hosting`) |
| DSK-02-08 | Shell: `NavigationView` rail, title bar search slot, status bar (connection · integration · version · environment), navigation + dialog services | feature | DSK-02-07 | Routes from 06 navigable; environment badge shows outside production; every interactive control has `AutomationProperties.AutomationId` | `winapp ui` smoke script navigates all rail items | 7 | `winui-dev` · `winui-design`, `winui-code-review` · Microsoft Learn |
| DSK-02-09 | Theme resource dictionary wired to the design authority tokens (details in 06) incl. Light/Dark/HighContrast | feature | DSK-02-08 | No hard-coded colour literals; typography via built-in text styles; 4 px grid | `winui-code-review` checklist; high-contrast screenshot | 7 | `winui-dev` · `winui-design` · — |
| DSK-02-10 | Single instance: `AppInstance.FindOrRegisterForKey` + activation redirection carrying arguments | feature | DSK-02-07 | Second launch activates first window and forwards arguments; redirect happens before window creation | UI test: launch twice, assert one window | 7 | `winui-dev` · `winui-dev-workflow` · Microsoft Learn (`AppInstance`) |
| DSK-02-11 | Unhandled-exception handler + diagnostics bundle export (zip manifest: versions, redacted logs, last compatibility response) | feature | DSK-02-07 | Crash writes a bundle then exits (never continues corrupted); user/admin "Export diagnostics" command | Fault-injection test; bundle schema test | 9 | `winui-dev` · `winui-dev-workflow` · — |
| DSK-02-12 | Extend `DependencyDirectionTests` for the desktop boundaries and the no-WebView rule | feature | DSK-02-05, DSK-02-06 | Facts fail on forbidden references and on a `WebView2` XAML element (until ADR-0108 allows the isolated renderer) | Temporary failing fixture proves red; then green | 1 | `pegasus-test-engineer` · `code-testing-agent`, `run-tests` · — |
| DSK-02-13 | `tests/Pegasus.Desktop.ViewModelTests` project (xunit, fakes for API client/clock/credential store) | feature | DSK-02-06 | First tests cover shell navigation and status-bar state | `dotnet test` | 2 | `pegasus-test-engineer` · `scaffold-dotnet-test-project`, `code-testing-agent` · — |
| DSK-02-14 | Dev-cert MSIX build: `winapp cert generate --manifest`, `winapp package --cert devcert.pfx --self-contained` | chore | DSK-02-05 | Signed dev MSIX installs on a clean Windows 11 VM after trust step | `tests/Pegasus.Packaging.Tests` install/uninstall script | 11 | `pegasus-release-packager` · `winui-packaging` · — |
| DSK-02-15 | CI lane `desktop-build` on `windows-latest`: restore (locked), build x64 Release, run ViewModel/Architecture tests, produce unsigned MSIX artifact | chore | DSK-02-14 | Job green on PR; artifact uploaded; Linux jobs unaffected (slnf) | `repository-check` run link | 1 | `pegasus-release-packager` · `authoring-github-workflows`, `winui-packaging` · — |
| DSK-02-16 | Phase 1 exit review on a clean Windows 11 test machine (install, launch, navigate, export diagnostics, uninstall) | chore | all above | Every gate row in §4 has evidence attached to the ticket proof | Proof doc with screenshots/logs | 7/11 | `pegasus-desktop-reviewer` · `winui-code-review` · Kanmer `set_ticket_doc` |

## 6. Routing table

| Work type | Subagent | Skills (pinned source) | MCP tools |
| --- | --- | --- | --- |
| Desktop scaffold, host, shell, services | `winui-dev` | `winui-setup`, `winui-dev-workflow` (BuildAndRun.ps1), `winui-design` (`winui-search.exe` for control lookup) — win-dev-skills v0.5.0 `f1028dd5` | Microsoft Learn `microsoft_docs_search` / `microsoft_code_sample_search` (AppInstance, AppWindow, ProtectedData, Hosting) |
| Contracts project, envelope types | `pegasus-gateway-dev` | `dotnet-webapi`, `microsoft-code-reference` — dotnet/skills `98f84851` | Microsoft Learn |
| Build props, CPM, slnf, CI lane, dev MSIX | `pegasus-release-packager` | `convert-to-cpm`, `directory-build-organization`, `binlog-failure-analysis`, `authoring-github-workflows`, `winui-packaging` | — |
| Tests (VM, architecture) | `pegasus-test-engineer` | `scaffold-dotnet-test-project`, `code-testing-agent`, `run-tests` | — |
| Review and exit gate | `pegasus-desktop-reviewer` (read-only) | `winui-code-review`, `winui-design`, project skill `pegasus-desktop` | Microsoft Learn, Kanmer |
| Ticket lifecycle | any | `kanmer-research` → `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` | Kanmer `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `move_item` |

## 7. Risks and traps

- **XAML compiler silence**: older Windows App SDK builds failed `MSB3073` with
  no XAML diagnostic; fixed in ≥ 2.1.3 (2.x) / ≥ 1.8 (1.x). Pin ≥ 2.1.3 and
  keep `BuildAndRun.ps1` (`.codex/skills/winui-dev-workflow/SKILL.md`).
- **`TreatWarningsAsErrors=true` + `AnalysisLevel=latest-recommended`** apply
  to the new projects; generated code (API client, XAML) needs explicit
  `NoWarn`/`GeneratedCodeAttribute` handling rather than relaxing the repo
  policy.
- **Linux build break**: a Windows TFM in `Pegasus.slnx` fails Linux
  restores; the slnf decision above is the mitigation; the Linux release
  script must switch to the slnf in the same ticket.
- **Lock files with Windows-only packages**: `packages.lock.json` for the
  desktop projects is RID/TFM specific; CI must restore with the same RID.
- **`BuildAndRun.ps1` injects a temporary `Directory.Build.props`** into the
  project directory only when none exists up the tree; with the repo-root
  props present it skips injection, so the `Microsoft.WindowsAppSDK.Analyzers`
  must be referenced explicitly in the desktop csproj to keep the `WUI*`
  diagnostics.
- **Package identity churn**: `Package.appxmanifest` `Identity.Name` and
  `Publisher` must be settled before any user installs (changing them later is
  a different app). D-002 chose a self-managed certificate whose **subject
  must equal this Publisher exactly**, so fix the value once here and never
  change it; use a stable CN in
  development only.
- **Self-contained size**: .NET + Windows App SDK self-contained MSIX is large;
  acceptable for ten users but measure and record in 09's release manifest.
- **Do not recreate the web shell**: the shell is a `NavigationView`, not a
  port of `_Layout.cshtml`; 06 owns the rules.
- **One Core owner**: temptation to copy `OperatorLabels` or policies into the
  desktop; 05's reuse map decides where such code moves, once.

## 8. Documentation changes

- `docs/adr/0100-native-winui-3-client-in-the-fork.md` and
  `docs/adr/0104-online-required-bounded-local-cache.md` (+ index rows).
- `docs/current-architecture.md`: system shape gains the desktop client and
  Contracts; dependency direction table extended; implementation map rows for
  `src/Pegasus.Desktop*`.
- `docs/runbook.md § Supported platform`: Windows-only desktop build, `winapp`
  CLI prerequisite, slnf usage on Linux.
- `docs/engineering.md`: evidence tiers mention `winapp ui` and packaging
  proofs (coordinate with 08).
- `docs/capabilities.md`: `DSK-01` (native shell) and `DSK-02` (diagnostics
  bundle) rows with canonical owner FRD-13/ADR-0100.
- `docs/desktop/README.md` status row for area 02 when tickets start.
