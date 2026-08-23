# 08 · Testing strategy, Test/UAT stack, and CI lanes

## 1. Purpose and proposal coverage

This area turns the proposal's testing strategy into the concrete test
projects, scripts, CI lanes, and evidence rules the conversion uses, and it
defines the local Test/UAT stack that replaces the proposal's "production-like
Azure Test/UAT environment" (locked decision L-02). It owns:

| Proposal section | What this area does with it |
| --- | --- |
| §21.2 CI stages (15 stages) | Maps each stage to an existing or new `ci.yml` job |
| §22.1 Characterization before refactoring | Restricts it to the rules that actually move (almost none) |
| §22.2 Test pyramid (domain, application, contract, server integration, view-model, UI automation, accessibility, packaging/update, security, performance, end-to-end) | Maps every layer to a test project or script, existing or new |
| §22.3 Coverage policy | Restates it as merge rules, no global percentage |
| §23 Verification and feature parity, §23.2 native verification | Defines the parity evidence a slice ticket must produce and the release-gate checks |
| §24 exit gates (Phases 1–10) | Names the test lane that proves each gate |
| End-to-end business scenarios 1–14 | Becomes the release critical path, each mapped to the Test/UAT stack or the pilot ring |

The detailed Test/UAT stack definition lives in
[test-uat-stack.md](test-uat-stack.md). Packaging/update test *procedures*
and signing are owned by
[09 · release, update and distribution](../09-release-update-and-distribution/README.md);
this area owns that they run and where. Accessibility review content is owned
by [06 · UI design](../06-ui-design/keyboard-and-accessibility.md); this area
owns the lanes that execute it. Performance budgets are owned by
[10 · security, observability, performance](../10-security-observability-performance/README.md).

## 2. Evidence base

### Facts

Verified by read-only inspection of the fork at `main` `191ddf33` on
2026-08-23.

Test projects and framework:

- xunit 2.9.3 is the only framework — `xunit.runner.visualstudio` 3.1.4,
  `Microsoft.NET.Test.Sdk` 17.14.1, `coverlet.collector` 6.0.4 in every test
  csproj; no NUnit, TUnit, MSTest, Moq, or FluentAssertions — fakes are
  hand-rolled (`tests/*/*.csproj`).
- `tests/Pegasus.ArchitectureTests`: 11 files, 62 facts / 3 theories; custom
  reflection (`GetReferencedAssemblies()`, `GetTypes()`), no NetArchTest;
  `DependencyDirectionTests.cs` (520 lines) is the layering guard;
  `MainBranchHistoryGuardTests.cs` (203 lines) is a git-history guard;
  composition guards for Worker/activation/Azure clients.
- `tests/Pegasus.Core.Tests`: 69 files, 494 facts / 72 theories, Core only.
- `tests/Pegasus.IntegrationTests`: 136 files, 716 facts / 47 theories;
  `WebApplicationFactory<Program>` referenced in 59 files, shared factory
  `IntakeWebApplicationFactory` at `IntakeWebTestSupport.cs:26` (866 lines);
  persistence tests on SQL Server LocalDB (`Server=(localdb)\MSSQLLocalDB;`
  `Database=Pegasus_Test_*`), sharded three ways in CI by
  `scripts/Invoke-TestShard.ps1`, partition proved by `-VerifyPartition`;
  `xunit.runner.json` sets `maxParallelThreads: 4`.
- Browser lane: `tests/Pegasus.IntegrationTests/Browser/` — 9 files, 20 facts
  (`OperatorJourneyTests.cs` 612 lines, `AccessibilityTests.cs` 156 lines
  using `Deque.AxeCore.Playwright` 4.12.0, `BrowserTestSupport.cs` 209 lines);
  Playwright 1.61.0 pinned through `Directory.Build.props`
  `PlaywrightVersion` and the Web `ContainerBaseImage`.
- Renderer tests: `tests/Pegasus.IntegrationTests/Reports/`
  (`AssessmentReportRendererTests.cs`, `AssessmentReportDraftWebTests.cs`).
- Fixed clock: no shared helper; each file declares its own private
  `FixedTimeProvider(DateTimeOffset) : TimeProvider` (eight or more copies,
  e.g. `Pegasus.Core.Tests/Intake/ProcessIntakeTests.cs:1208`,
  `.../RetainedMailTests.cs:820`). The 2031 data convention is not uniform
  (`Pegasus.ArchitectureTests/DueWorkSweepFunctionTests.cs:72` uses 2026).
- Traits in use: `SqlServer`, `Browser`, `Corpus`, `QdosAlphaAcceptance`
  (`docs/operations.md:66-104`); runbook locked profile:
  `dotnet restore ./Pegasus.slnx --locked-mode`, `dotnet build
  --configuration Release --no-restore`, `dotnet test --no-build --filter
  "Category!=Corpus"` (`docs/runbook.md:298-370`).
- `corpus/` is ignored and immutable (`.gitignore:1-2`, `AGENTS.md` safety
  rails); `reference/` holds supplied evidence; repository rule: never
  fabricate domain emails, images, documents, or data.

CI today (`.github/workflows/ci.yml`, single workflow `repository-check`):

- Jobs: `changes` (ubuntu, history guard, path flags, shard/migration-grant
  tests), `documentation` (windows), `local-development-scripts` (windows),
  `reference-data` (windows, Python), `infrastructure` (windows, conditional),
  `unit` (windows, Core.Tests + ArchitectureTests), `sql-integration`
  (windows, matrix shard 1–3, `Category!=Corpus&Category!=Browser`),
  `sql-integration-coverage` (ubuntu, `-VerifyPartition`), `browser`
  (windows, `Category=Browser`, pinned Chromium, `xUnit.MaxParallelThreads=2`).
- Composite action `.github/actions/dotnet-build/action.yml`:
  `actions/setup-dotnet@v6` 10.0.x, NuGet cache keyed on `global.json` and
  every `packages.lock.json`, locked restore, Release build.
- No publish, sign, deploy, or artifact-release lane; no Dependabot.
- Known trap: CI dying in `actions/checkout` at about five minutes on the
  700 MB repository (upstream ticket DELIV-010; release-skill trap table).

Local tooling that the Test/UAT stack reuses:

- `scripts/Invoke-LocalDevelopment.ps1` (1,583 lines): `Start`, `Status`,
  `Smoke`, `Stop`, `Reset`, failure-injection modes;
  `scripts/Initialize-LocalDevelopment.ps1` (locked restore, Playwright
  Chromium, dev certs, LocalDB); `scripts/Invoke-Doctor.ps1`
  (`-Profile Offline|Cloud`, never installs or signs in).
- Azurite 3.36.0 is the only `package.json` devDependency; Worker
  `local.settings.example.json` uses `AzureWebJobsStorage=UseDevelopmentStorage=true`
  and `Runtime__Profile=DevelopmentOffline`.
- `DevelopmentOffline` replay/local adapters exist in Infrastructure:
  `LocalDurableApprovedInboxSource` (514 lines), `LocalDurableApprovedSentSource`
  (553), `Vehicle/DvlaDvsaAdapters.cs` (replay adapter), `Custody/LocalCaseCustody`
  (549), `Intake/FileSystemIntakeArtifactStore` (624).
- `Runtime:Profile` accepts only `DevelopmentOffline` (Development
  environment) or `Production` (`src/Pegasus.Web/Program.cs:101-122`); any
  other value throws.

Evidence tiers and rules:

- Twelve evidence tiers, `docs/engineering.md:72-89`; tier 7 (browser and
  accessibility) states that automated axe results do not replace manual
  keyboard or assistive-technology review; tier 10 sizes: eight concurrent
  operators, 2,000 cases a month, 10 MiB single-file limit.
- `docs/design/README.md` requires ten recorded accessibility reviews
  (keyboard-only, screen reader, focus/error, 1280+ desktop, 1024–1279,
  200% zoom, forced colours, reduced motion, contrast, automated scan
  through the real caller) — see
  [06 · keyboard and accessibility](../06-ui-design/keyboard-and-accessibility.md).

Official documentation (fetched 2026-08-23):

- `winapp ui` UI Automation harness (verbs, `ui-tests.ps1` pattern,
  AutomationId audit): vendored skill `.codex/skills/winui-ui-testing/SKILL.md`
  from `microsoft/win-dev-skills` v0.5.0.
- Automated accessibility engine for Windows desktop apps: `AxeWindowsCLI`
  from https://github.com/microsoft/axe-windows (CLI README under
  `src/CLI/README.MD`).
- App Installer update behaviour used by the packaging tests
  (`OnLaunch`, `ShowPrompt`, `UpdateBlocksActivation`,
  `ForceUpdateFromAnyVersion`):
  https://learn.microsoft.com/windows/msix/app-installer/update-settings and
  https://learn.microsoft.com/windows/msix/app-installer/auto-update-and-repair--overview.
- In-app update check (`Package.CheckUpdateAvailabilityAsync` through
  `PackageManager.FindPackageForUser`, not `Package.Current`):
  https://learn.microsoft.com/uwp/api/windows.applicationmodel.package.checkupdateavailabilityasync.
- MIME types and `Content-Length` required of any feed host (including the
  local one): https://learn.microsoft.com/windows/msix/msix-troubleshooting-guide.

### Assumptions

- GitHub-hosted `windows-latest` runners run an interactive desktop session
  sufficient for `winapp ui` and `AxeWindowsCLI` against an installed MSIX.
  To be verified by the first CI spike ticket (DSK-08-12); if false, the UI
  and axe lanes run on a self-hosted Windows 11 runner or only on the Test/UAT
  workstation.
- `Add-AppxPackage` with a dev certificate trusted in `LocalMachine\TrustedPeople`
  works on the runner without Developer Mode (sideloading is on by default on
  Windows 11). To be verified in the same spike.
- The ViewModel test project can target `net10.0-windows10.0.26100.0` and run
  on `windows-latest` without a packaged identity because view models stay
  free of `DispatcherQueue` and WinRT UI types (design rule from
  [02 · architecture](../02-architecture-and-foundation/README.md)).
- The Test/UAT stack under `DevelopmentOffline` is acceptable for UAT of
  desktop workflows; tiers 9–12 are proved only on the pilot ring. This is a
  consequence of L-02, recorded as a deviation below.

## 3. Decisions and assumptions

Locked and open decisions this area depends on
([index](../README.md#locked-decisions-and-open-decisions)):

- **L-02** — Test/UAT is the local production-mimicking stack; no Azure test
  environment; ADR-0014 stands. The pilot ring on production is the only
  real-Azure validation.
- **L-03** — reports render locally through WebView2; golden-file parity tests
  are therefore a desktop-side test concern (owned by 07, executed in the
  lanes defined here).
- **D-003 is decided** (UNC share), so the Test/UAT stack rehearses the real
  transport: packaging tests run against a file share, not an HTTP
  substitute. **D-002 (signing) is open**, so they run with a dev
  certificate; the production signing variant is added when D-002 closes.

Deviations from the proposal, stated explicitly:

- **Deviation (environments):** proposal §21.3 lists a Test/UAT environment
  with production-like Azure dependencies. Under L-02 the Test/UAT surface is
  local (Azurite, LocalDB or SQL container, replay adapters, local feed). What
  it cannot prove — Azure SQL locking, Blob and Key Vault behaviour,
  Container App probes, App Insights, real Box/Graph/DVLA/DVSA, the real
  update feed — is listed in
  [test-uat-stack.md](test-uat-stack.md#what-the-stack-proves-and-what-it-does-not)
  and is proved on the pilot ring.
- **Deviation (runtime profile):** no new `TestStack` runtime profile is
  added. `Runtime:Profile` accepts only `DevelopmentOffline` and
  `Production` (`Program.cs:101-122`); adding a third profile would be a new
  composition root to maintain and review. The stack runs under
  `DevelopmentOffline` with `Features:LocalIntake` and
  `Features:LocalDocumentCustody` and the existing replay adapters.
- **Deviation (UI automation driver):** proposal §22.2 leaves the driver
  open ("current supported Windows UI Automation/Appium-compatible route").
  The vendored toolchain provides `winapp ui` (UIA); no WinAppDriver, Appium,
  or FlaUI is introduced. If `winapp ui` proves insufficient for a scenario,
  the fallback is a small UIA harness using the same AutomationId contract,
  not a driver dependency in the application.
- **Deviation (characterization tests):** §22.1 asks for characterization
  tests before moving any business rule. No business rule moves — Core stays
  the single owner and the desktop reaches it through the gateway — so
  characterization is limited to (a) the read-model shapes the Razor pages
  compose today and (b) the `OperatorLabels` vocabulary map, both captured as
  contract snapshots rather than behaviour tests. Page-level behaviours
  (TempData proposed values, PRG redirects, antiforgery) are transport
  mechanics, not behaviour to preserve.
- **Deviation (coverage):** no coverage gate is added; the `coverlet`
  collector stays for reports only (§22.3).

No Azure writes in this area. Everything runs locally or on CI runners.

## 4. Target state and exit gate

Target state when this area is complete:

| Layer (§22.2) | Home | State |
| --- | --- | --- |
| Domain/application unit tests | `tests/Pegasus.Core.Tests` (reused) | Unchanged; gaps listed by `test-gap-analysis` are closed as slices touch a rule |
| API contract tests | NEW `tests/Pegasus.Api.ContractTests` | OpenAPI snapshot, generated-client compile, serialization, problem responses, authn/authz per endpoint, version compatibility, concurrency conflicts, paging/filter/sort, backward compatibility during rollout |
| Server integration | `tests/Pegasus.IntegrationTests` (extended) | Every `/api/v1` command has authorization and failure-path tests; LocalDB shards still partition-verified |
| View-model tests | NEW `tests/Pegasus.Desktop.ViewModelTests` | Commands and availability, loading/empty/error/success, cancellation, dirty state, validation, navigation, stale session, mandatory update |
| WinUI UI automation | NEW `tests/Pegasus.Desktop.UITests` (script-driven `winapp ui`) | Small high-value suite: launch/update/login, open case, edit/save, concurrency message, document upload, vehicle lookup, report preview/finalize, logout, keyboard navigation, core accessibility properties |
| Accessibility | `AxeWindowsCLI` scan + the ten recorded manual reviews | Recorded per release candidate |
| Packaging and update | NEW `eng/packaging/Test-Package.ps1` (+ `tests/Pegasus.Packaging.Tests` thin xunit wrapper if useful) | Clean install, upgrade from each supported previous version, mandatory update, blocked obsolete client, signature failure, interrupted update, rollback, uninstall/reinstall, no admin requirement, trusted certificate deployment |
| Security | Contract/integration tests + scripted scans | Token lifecycle, disabled account, role bypass, direct-object access, malformed uploads, unsafe paths, secret/log scanning, dependency scanning, manifest tampering, version spoofing, temp-file permissions |
| Performance | Scripts + traces on the Test/UAT workstation | §15.1 budgets measured on baseline hardware; regression report per release candidate |
| End-to-end business scenarios 1–14 | UAT scripts on the Test/UAT stack; 2, 7, 12 repeated on the pilot ring | Release critical path |

Exit gate for the area (proves proposal §24 gates for the test side):

1. The new test projects exist, are in `Pegasus.slnx`, build with
   `TreatWarningsAsErrors`, and run in CI on `windows-latest`.
2. `ci.yml` has the desktop lanes below and the Linux publish of Web/Worker
   stays green.
3. The Test/UAT stack starts from one script on a clean Windows 11 machine and
   the E2E scenario scripts 1–14 exist with pass/fail recording.
4. The release critical path (scenarios 1–14) has run once end to end and its
   evidence is filed under `artifacts/` (ignored) with a summary in the
   release ticket's proof.

## 5. Work breakdown

Tier numbers are the `docs/engineering.md` evidence tiers. Routing is
"subagent · skills · MCP". Profiles are the fork board's Kanmer profiles.

| ID | Title | Profile | Depends on | Acceptance | Verification | Tier | Routing |
| --- | --- | --- | --- | --- | --- | --- | --- |
| DSK-08-01 | Scaffold `tests/Pegasus.Api.ContractTests` (xunit 2.9.3, WebApplicationFactory, locked restore) | feature | 03 route-group skeleton (DSK-03-01) | Project in `Pegasus.slnx`; OpenAPI snapshot test fails on undeclared change; generated client compiles | `dotnet test tests/Pegasus.Api.ContractTests` green locally and in CI | 5 | `pegasus-test-engineer` · `scaffold-dotnet-test-project`, `run-tests` · Microsoft Learn, Kanmer |
| DSK-08-02 | Authorization and failure-path test template for every `/api/v1` command | feature | DSK-08-01 | One theory per command: unauthenticated 401, wrong right 403, stale version 409, bad input 400 problem, replayed operation key idempotent | Tests enumerate the endpoint map and fail when a command lacks coverage | 5 | `pegasus-test-engineer` · `code-testing-agent`, `test-gap-analysis` · Kanmer |
| DSK-08-03 | Extend `Pegasus.IntegrationTests` shards with `/api/v1` persistence paths; keep `-VerifyPartition` green | fix | DSK-08-02 | New tests appear in exactly one shard; LocalDB template backup still used | `scripts/Invoke-TestShard.ps1 -VerifyPartition` | 4, 5 | `pegasus-test-engineer` · `run-tests` · Kanmer |
| DSK-08-04 | Scaffold `tests/Pegasus.Desktop.ViewModelTests` (`net10.0-windows10.0.26100.0`, no UI thread) | feature | 02 desktop scaffold (DSK-02-03) | VMs testable without `DispatcherQueue`; fake gateway client; fake clock (one shared `FixedTimeProvider` for desktop tests — Deviation from per-file copies) | `dotnet test` on `windows-latest` | 2 (desktop-side) | `pegasus-test-engineer` · `scaffold-dotnet-test-project`, `code-testing-agent` · Microsoft Learn, Kanmer |
| DSK-08-05 | VM test catalogue: states, commands, cancellation, dirty state, validation, navigation, stale session, mandatory update | feature | DSK-08-04, 04 startup orchestrator | Each slice VM has state-machine tests; mandatory-update and stale-session VMs covered | `dotnet test --filter Category=ViewModel` | 2 | `pegasus-test-engineer` · `code-testing-agent`, `assertion-quality` · Kanmer |
| DSK-08-06 | `tests/Pegasus.Desktop.UITests`: `ui-tests.ps1` harness around `winapp ui` with the AutomationId contract | feature | 06 AutomationId convention, 02 MSIX dev build | Script launches installed package, runs batch, writes results JSON and screenshots; AutomationId coverage audit passes | Run against the Test/UAT stack; results filed under `artifacts/ui-tests/` | 7 | `pegasus-ui-verifier` · `winui-ui-testing` · Kanmer |
| DSK-08-07 | UI critical-path scripts: launch/update/login, open case, edit/save, concurrency message, logout, keyboard navigation | feature | DSK-08-06, slices S1–S5 | Each script asserts via `wait-for`/`get-value`; no sleeps; two-user conflict scripted with the gateway fixture | Pass on Test/UAT stack | 7 | `pegasus-ui-verifier` · `winui-ui-testing` · Kanmer |
| DSK-08-08 | UI scripts: document upload, vehicle lookup, report preview/finalize | feature | DSK-08-06, slices S14, S15, S18 | File picker driven through `winapp ui` (`-w <HWND>`); report PDF produced and registered | Pass on Test/UAT stack | 7 | `pegasus-ui-verifier` · `winui-ui-testing` · Kanmer |
| DSK-08-09 | Accessibility lane: `AxeWindowsCLI` scan script + the ten recorded reviews checklist | feature | DSK-08-06 | Scan runs per screen; results attached; manual review record template filled per release candidate | Scan artefacts + signed checklist | 7 | `pegasus-ui-verifier` · `winui-ui-testing` · Microsoft Learn |
| DSK-08-10 | `eng/packaging/Test-Package.ps1`: clean install, upgrade from each supported previous version, mandatory update, blocked client, signature failure, interrupted update, rollback, uninstall/reinstall, no-admin, cert trust | feature | 09 appinstaller template, local feed in Test/UAT stack | Every scenario scripted with expected `Get-AppxPackage` state; interrupted update simulated by feed cut-off | Run on Test/UAT workstation per release candidate | 11 | `pegasus-release-packager` · `winui-packaging` · Microsoft Learn |
| DSK-08-11 | Security test set: token lifecycle, disabled account, role bypass, direct-object access, malformed uploads, unsafe paths, manifest tampering, version spoofing, temp-file ACLs, secret/log scan | feature | DSK-08-02, 04 token flow | Each item has a failing-then-passing test or scripted check; secret scan over logs and package | `dotnet test --filter Category=Security` + scripts | 9 | `pegasus-test-engineer` · `code-testing-agent` · Kanmer |
| DSK-08-12 | CI spike: can `windows-latest` install a dev-signed MSIX, run `winapp ui`, and run `AxeWindowsCLI`? | spike | DSK-08-06 | Written answer with the run log; decision recorded (hosted vs self-hosted runner) | Workflow run link in the ticket research doc | 1 | `pegasus-release-packager` · `authoring-github-workflows`, `winui-ui-testing` · Microsoft Learn |
| DSK-08-13 | `ci.yml` lanes: `desktop-build` (build + VM tests + contract tests), `desktop-package` (MSIX dev-cert artifact), `desktop-ui-smoke` (install + `winapp ui` + axe, per DSK-08-12 outcome), `packaging-tests` | feature | DSK-08-12, 02 solution filter | Lanes green on PR; Linux Web/Worker publish unaffected; artifacts uploaded | Workflow run green; `git diff` limited to `ci.yml` and composite action | 1 | `pegasus-release-packager` · `authoring-github-workflows` · Kanmer |
| DSK-08-14 | Vulnerability and SBOM step (`dotnet list package --vulnerable --include-transitive`; optional Syft SBOM) | chore | DSK-08-13 | Lane fails on known-vulnerable packages; SBOM attached to package artifact | Workflow run | 9 | `pegasus-release-packager` · `authoring-github-workflows` · Microsoft Learn |
| DSK-08-15 | Performance scripts on the Test/UAT workstation: startup (cold/warm), repeated navigation, large list, document/image-heavy case, memory after prolonged use, slow network, provider timeout, ten concurrent users + worker, report generation | feature | Test/UAT stack, 10 budgets | Scripts emit a table against §15.1 budgets; baseline hardware recorded | Report filed per release candidate | 10 | `pegasus-ui-verifier` · `analyzing-dotnet-performance`, `dotnet-trace-collect` · Microsoft Learn |
| DSK-08-16 | E2E business scenarios 1–14 as UAT scripts; map each to Test/UAT stack or pilot ring | feature | Test/UAT stack, slices | Each scenario has steps, expected results, evidence to capture, and the tier it proves | Dry run once on the stack | 12 | `pegasus-test-engineer` · `kanmer-verify` · Kanmer |
| DSK-08-17 | Build the Test/UAT stack lifecycle (extend `scripts/Invoke-LocalDevelopment.ps1` with a `TestStack` mode) | feature | 02, 04, local feed | `Start`, `Status`, `Smoke`, `Reset`, `Stop` bring up gateway + Worker + Azurite + DB + feed; `Invoke-Doctor.ps1` reports prerequisites | Clean Windows 11 machine walkthrough recorded | 6, 12 | `pegasus-test-engineer` · `run-tests` · Microsoft Learn |
| DSK-08-18 | Golden-file report parity lane (executes 07's fixtures on the stack; WebView2 vs gateway output) | feature | 07 renderer tickets | Text/values/layout fixtures compared; differences explained or fixed | Lane green | 8 | `pegasus-test-engineer` · `run-tests` · Kanmer |
| DSK-08-19 | CI cost and runner plan for the private-repository era (C-01): measure current Windows-minute consumption, price the added desktop lanes, decide self-hosted runner vs paid plan vs lane trimming, and record it in `docs/engineering.md` | spike | DSK-08-13, DSK-08-14 | A written recommendation with measured minutes per PR run and per month, the chosen option, and the migration steps; if self-hosted, the host is the D-003 share host and its isolation/permissions are specified | Actions usage report for the last 30 days; a dry-run costing of the new lanes | 1 | `pegasus-release-packager` · `authoring-github-workflows` · Kanmer |

## 6. Routing table

| Capability needed | Subagent | Skills (exact names) | Source / pin | MCP |
| --- | --- | --- | --- | --- |
| Run or filter any .NET test, TRX, shards | `pegasus-test-engineer` | `run-tests` | `dotnet/skills` `98f84851`, plugin `dotnet-test` | Kanmer |
| Write unit/VM/contract tests, scaffold new test projects | `pegasus-test-engineer` | `code-testing-agent`, `scaffold-dotnet-test-project` | `dotnet/skills` `98f84851`, plugin `dotnet-test` | Microsoft Learn |
| Find test gaps, audit assertions | `pegasus-test-engineer` | `test-gap-analysis`, `assertion-quality` | `dotnet/skills` `98f84851`, plugin `dotnet-test` | — |
| Script `winapp ui` batches, AutomationId audit, screenshots, video | `pegasus-ui-verifier` | `winui-ui-testing` | `microsoft/win-dev-skills` v0.5.0 `f1028dd5`, vendored `.codex/skills/winui-ui-testing/` | — |
| Performance anti-pattern scan and trace capture | `pegasus-ui-verifier` | `analyzing-dotnet-performance`, `dotnet-trace-collect` | `dotnet/skills` `98f84851`, plugin `dotnet-diag` | Microsoft Learn |
| Packaging/update tests, MSIX dev cert | `pegasus-release-packager` | `winui-packaging` | `microsoft/win-dev-skills` v0.5.0 | Microsoft Learn |
| CI workflow changes | `pegasus-release-packager` | `authoring-github-workflows` | `dotnet/skills` `.agents/skills/authoring-github-workflows` | — |
| Build/launch the app under test | `winui-dev` | `winui-dev-workflow` (`BuildAndRun.ps1`) | `microsoft/win-dev-skills` v0.5.0 | — |
| Endpoint fixtures and fakes for contract tests | `pegasus-gateway-dev` | `dotnet-webapi` | `dotnet/skills` `98f84851`, plugin `dotnet-aspnetcore` | Microsoft Learn |
| Independent review of test evidence | `pegasus-desktop-reviewer` | `winui-code-review` | `microsoft/win-dev-skills` v0.5.0 | — |
| Ticket pipeline | any | `kanmer-tickets`, `kanmer-plan`, `kanmer-execute`, `kanmer-verify` | `.grok/skills/` | Kanmer (`get_doc_gates`, `set_ticket_doc`, `move_item`) |
| Automated accessibility | `pegasus-ui-verifier` | (tool, not a skill) `AxeWindowsCLI` | https://github.com/microsoft/axe-windows (fetched 2026-08-23) | — |

## 7. Risks and traps

- **CI minutes stop being free when the repositories go private (constraint
  C-01).** GitHub Actions bills private-repository **Windows** runners at a
  2× multiplier against a monthly included-minutes allowance, and this
  repository already runs most of `ci.yml` on `windows-latest` — with the
  desktop build, MSIX packaging, `winapp ui` and packaging lanes still to be
  added on top. Verify the current allowance and per-minute rates for the
  account's plan at decision time. Mitigations, in order of fit: a
  **self-hosted Windows runner** on the same always-on host that serves the
  D-003 UNC share (self-hosted minutes are not billed, the machine is
  already required, and it is the natural custodian of the signing
  certificate if D-002 lands on a self-managed cert); a paid plan; or
  trimming Windows lanes (for example running contract and view-model tests
  on the cheapest lane that can host them). Decide before the repositories
  flip, not after — see DSK-08-19.
- **UI automation flakiness on hosted runners.** Mitigation: AutomationId
  contract, `wait-for` instead of sleeps, two fix-and-rerun cycles maximum
  (from the skill), and the DSK-08-12 spike decides hosted vs self-hosted.
- **LocalDB is Windows-only; Linux CI cannot run the integration shards.**
  The existing design (shards on `windows-latest`, coverage check on ubuntu)
  is kept; desktop lanes are Windows-only by nature.
- **Shard partition drift.** Every new integration test must land in exactly
  one shard; `-VerifyPartition` stays a required job.
- **Browser lane retained until web retirement.** The Playwright lane and
  `Deque.AxeCore.Playwright` keep running while Razor pages serve production;
  do not remove them on desktop cutover day — remove with the web retirement
  ticket (area 05 cut list).
- **`Category` traits.** New tests must carry `SqlServer`/`Browser`/
  `ViewModel`/`Security`/`Performance`/`Packaging` traits so the filters in
  `ci.yml` and the runbook keep meaning.
- **Test clock inconsistency.** 2031 vs 2026 fixed dates already coexist; the
  desktop test projects adopt one shared fake clock and one date convention
  (documented in the project README) — do not add a ninth `FixedTimeProvider`
  copy.
- **Never fabricate domain data.** UAT datasets come from `reference/` and
  deliberately generic fixtures; `corpus/` is never copied, uploaded, or
  committed.
- **`TreatWarningsAsErrors=true` applies to test projects** (`Directory.Build.props`);
  analyzer warnings in generated client or test fakes break the build —
  suppress per file with a reason, never globally.
- **CI checkout timeouts on the 700 MB repository** (DELIV-010, release-skill
  trap): use shallow checkout where the history guard does not need depth;
  re-run by closing/reopening the PR is a workaround, not a fix.
- **UI tests mutate the installed package** (`Add-AppxPackage`,
  `Remove-AppxPackage`): run them only on dedicated runners/workstations,
  never on a developer's machine with a pilot install.
- **Automated axe is not acceptance** (tier 7): the ten manual reviews are
  still required per release candidate.
- **Two policy engines risk in tests**: contract tests must assert that the
  API and the MCP tools reach the same Core use cases (shared fixtures), not
  re-implement rules in test code.

## 8. Documentation changes

- `docs/engineering.md § Required evidence tiers`: add the desktop-side
  interpretation of tiers 2 (view-model), 5 (`/api/v1` caller), 7
  (`winapp ui` + `AxeWindowsCLI` + manual reviews), 11 (packaging/update
  tests), 12 (scenarios 1–14 on the Test/UAT stack and pilot ring).
- `docs/runbook.md § Locked restore, build and test`: add the desktop test
  commands, the `TestStack` mode of `Invoke-LocalDevelopment.ps1`, and the
  Windows-only note for the desktop lanes.
- `docs/operations.md § Evidence profiles`: add `ViewModel`, `DesktopUI`,
  `Packaging`, `Security`, `Performance` traits and what each proves.
- `docs/capabilities.md`: `DSK` rows for "desktop test lanes" and "Test/UAT
  stack" with canonical owner = this plan until an FRD/ADR owns them.
- `docs/desktop/README.md` status table: mark 08 as "lanes defined" then
  "lanes green" as DSK-08-13 lands.
- ADR-0105 (MSIX/App Installer + minimum-version gate) records that the
  packaging/update tests are the release gate for the update path; ADR-0108
  (WebView2 rendering) records the golden-file parity lane as its
  verification.
