# Research — FND-038: a Windows-target home for desktop unit tests, and one shared clock

## Question

What shape must `tests/Pegasus.Desktop.ViewModelTests` take so that it matches this
repository's existing test-project conventions, runs on the Windows target framework without
a `DispatcherQueue`, and provides the one shared set of fakes that every later desktop ticket
consumes — and who owns creating it, given that area 08 names the same project?

## Current behaviour

No parity-matrix row covers this, **and none should**. The matrix
(`docs/desktop/01-inventory-and-parity/parity-matrix.md`) holds `PAR-01`…`PAR-46`
(`grep -c '^| PAR-' …` → `46`), every row keyed to a Razor page model under
`src/Pegasus.Web/Pages/**`. A test project delivers no operator-visible surface.

The closest existing repository mechanism is the three-project test layout the new project
joins:

| Project | TFM | Packages | Project references |
| --- | --- | --- | --- |
| `tests/Pegasus.Core.Tests` | `net10.0` (`:4`) | coverlet.collector 6.0.4, Microsoft.NET.Test.Sdk 17.14.1, xunit 2.9.3, xunit.runner.visualstudio 3.1.4 (`:12-15`) | Core only (`:23`) |
| `tests/Pegasus.ArchitectureTests` | `net10.0` (`:4`) | the same four (`:12-15`) | Core, Infrastructure, Web, Worker (`:23-26`) |
| `tests/Pegasus.IntegrationTests` | `net10.0` (`:4`) | the four plus Deque.AxeCore.Playwright, DocumentFormat.OpenXml, Microsoft.AspNetCore.Mvc.Testing, Microsoft.Playwright | Web, Infrastructure |

All three set `ImplicitUsings`, `Nullable`, `IsPackable=false`,
`RestorePackagesWithLockFile=true` and `<Using Include="Xunit" />`. CI runs the two unit
projects in `.github/workflows/ci.yml:131-147` (`unit`), chained with `&&` — the comment at
`:143-144` records why: pwsh reports only the last command's exit code, so two separate lines
would hide a failing first project.

## Findings

- **The repository's `FixedTimeProvider` is copied per test file today — nine copies at
  least, and the shapes differ.** `grep -rn 'class.*TimeProvider' tests src --include=*.cs`
  (2026-08-24) finds private nested classes in
  `tests/Pegasus.ArchitectureTests/DueWorkSweepFunctionTests.cs:115`,
  `tests/Pegasus.Core.Tests/Custody/ExternalWorkDispatchTests.cs:140`,
  `tests/Pegasus.Core.Tests/Intake/CaseMatching/AutomaticMailCaseAssociationTests.cs:79`,
  `Intake/PollApprovedInboxTests.cs:798`, `Intake/ProcessIntakeTests.cs:1208`,
  `Intake/RetainedMailTests.cs:820`, `Operations/DashboardBoundaryTests.cs:95`,
  `Operations/OperationsUseCaseTests.cs:194`, `Qdos/QdosBoundaryContractTests.cs:229`, plus a
  differently-shaped `FakeTime(Func<DateTimeOffset> now)` at `AiWork/AiWorkTests.cs:253`.
  Constructor parameter names vary (`utcNow`, `nowUtc`, `now`).
  - This is why `docs/desktop/08-testing/README.md` § 5 row `DSK-08-04` calls one shared
    `FixedTimeProvider` an explicit **Deviation from per-file copies**: it deviates from what
    the repository currently *does*, while agreeing with what `docs/engineering.md` § Test
    support *says* ("One fake per concept, in the shared driver, `internal`… A fake or helper
    copied into a second test file is the third-copy rule applied to tests").
  - Practical consequence for this ticket: the shared clock must be `internal`, must live in
    one file, and the plan must say plainly that the Core-side copies are **not** in scope to
    consolidate — that is a different diff in a different area.
- **No mocking framework is referenced anywhere.** The four packages in each test csproj are
  the whole set; there is no Moq, NSubstitute or FakeItEasy, and no
  `Microsoft.Extensions.TimeProvider.Testing` (its only appearance in the tree is as a
  *permitted-package example inside a test fixture string*,
  `tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs:98`). Hand-written fakes are
  the convention, which is what the ticket's Guardrails require.
- **The `unit` CI job names its projects explicitly, so a third project is invisible to CI
  until someone edits the workflow.** `.github/workflows/ci.yml:145-147` lists exactly
  `Pegasus.Core.Tests` and `Pegasus.ArchitectureTests`. The body's step 12 is right to leave
  that edit to [[FND-040]] (plan handle `DSK-02-15`) — but it means this ticket's tests are
  **not enforced by CI when it merges**, and the plan must say so rather than implying
  coverage it does not have.
- **`scripts/Get-CiChangeFlags.ps1:11` `$buildPattern` already matches `^(src|tests)/`**, so
  adding this project sets `build=true` on every PR that touches it with no classifier
  change. The gate exists; the lane does not.
- **The composite build action restores and builds the whole solution.**
  `.github/actions/dotnet-build/action.yml` runs `dotnet restore ./Pegasus.slnx
  --locked-mode` then `dotnet build ./Pegasus.slnx --configuration Release --no-restore`,
  with the NuGet cache keyed on `global.json`, `src/**/packages.lock.json` and
  `tests/**/packages.lock.json`. Adding a Windows-target project to `Pegasus.slnx` therefore
  makes every current lane — including the ubuntu-only `changes` and
  `sql-integration-coverage` jobs, which do not build — part of the blast radius of a
  restore failure on the ubuntu lanes if the composite is ever used there. Today all seven
  building lanes are `windows-latest`, so this is a latent risk, not a live one; it is what
  the solution-filter decision in [[FND-028]] (plan handle `DSK-02-03`) exists to bound.
- **Seven `packages.lock.json` files exist today** (four under `src/`, three under `tests/`),
  and `RestorePackagesWithLockFile=true` is set in each test csproj at `:7`. A Windows-only
  package graph produces a RID/TFM-specific lock file, which is why the body's step 10
  restores with `-r win-x64 --force-evaluate` before `--locked-mode` is expected to pass.
- **Central package management does not exist yet.** There is no `Directory.Packages.props`
  and no `nuget.config` in the tree (2026-08-24). The body's step 2 requires package
  references "without version literals", which is only correct **after** [[FND-027]] (plan
  handle `DSK-02-02`) has landed; if it has not, version literals matching the other test
  projects are the correct interim and [[FND-027]] centralises them.
- **The same project is named by two tickets.** [[TEST-004]] (plan handle `DSK-08-04`,
  "Scaffold `tests/Pegasus.Desktop.ViewModelTests` (`net10.0-windows10.0.26100.0`, no UI
  thread)") is the area 08 row for the identical scaffold. Its recorded dependency is
  `DSK-02-03`, which is a **stale handle** for the desktop scaffold — `DSK-02-03` is now
  [[FND-028]] (the solution filter) and the desktop scaffold is [[FND-030]] (plan handle
  `DSK-02-05`). Both tickets are in `backlog` and neither is taken (2026-08-24).
- **Five later tickets depend on this project existing**: [[FND-031]], [[FND-032]],
  [[FND-033]], [[FND-035]] and [[FND-036]] all need a Windows-target test home because
  `ProtectedData`, `ApplicationData.Current.LocalFolder` and `Package.Current` cannot be
  exercised from the `net10.0` architecture-test project. The board records
  `blocks: [FND-041, FEAT-001]` on this ticket directly.

### Facts

| Fact | Source |
| --- | --- |
| Existing test csproj shape (7 properties, 4 packages, `<Using Include="Xunit" />`) | `tests/Pegasus.Core.Tests/Pegasus.Core.Tests.csproj` (whole file, 26 lines) |
| `FixedTimeProvider` is copied into at least nine test files with three parameter names | `grep -rn 'class.*TimeProvider' tests src --include=*.cs`, 2026-08-24 |
| No mocking framework, no `TimeProvider.Testing` package | the three test csproj files; `grep -rn 'TimeProvider.Testing' tests src` finds only `DependencyDirectionTests.cs:98` (a fixture string) |
| CI `unit` names its two projects literally and chains with `&&` | `.github/workflows/ci.yml:145-147`, with the reason at `:143-144` |
| `$buildPattern` already matches `^(src|tests)/` | `scripts/Get-CiChangeFlags.ps1:11` |
| Composite action restores/builds the whole `Pegasus.slnx` with `--locked-mode` | `.github/actions/dotnet-build/action.yml` |
| Seven lock files; `RestorePackagesWithLockFile` in each test csproj at `:7` | `find . -name packages.lock.json` (excluding `.worktrees/`, `workspaces/`); the three test csproj files |
| No `Directory.Packages.props`, no `nuget.config` | `ls` at repository root, 2026-08-24 |
| Solution architecture fact pins the project list | `tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs:127-153`, expected array `:141-149` |
| Repository-wide `TreatWarningsAsErrors=true`, `AnalysisLevel=latest-recommended` | `Directory.Build.props:6-7` |
| `docs/engineering.md` § Test support requires one fake per concept, `internal`, in the shared driver | `docs/engineering.md:194-199` |
| [[TEST-004]] is the area 08 twin, in `backlog`, untaken | `list_items area=testing`, 2026-08-24 |

### Assumptions

- **A-02-13-1 — a WinUI 3 view model can be constructed and exercised on the xunit thread
  with no `DispatcherQueue` present**, provided the view model itself takes an `IDispatcher`
  abstraction rather than calling `DispatcherQueue.GetForCurrentThread()`. Confirmed by:
  running the first navigation test once [[FND-032]] (plan handle `DSK-02-07`) and
  [[FND-033]] (plan handle `DSK-02-08`) have landed. *If wrong*, the view models must be
  refactored to inject the abstraction — which is [[FND-032]]'s and [[FND-033]]'s work, not a
  reason to start a UI thread in a unit test. The ticket body already mandates the
  abstraction in step 7.
- **A-02-13-2 — `xunit` 2.9.3 and `xunit.runner.visualstudio` 3.1.4 run unchanged on a
  `net10.0-windows10.0.26100.0` target.** Confirmed by: the first `dotnet test` run. *If
  wrong*, the fallback is the same package versions on a plain `net10.0-windows` moniker, and
  the difference is recorded — not a different test framework.
- **A-02-13-3 — `dotnet restore ./Pegasus.slnx --locked-mode` succeeds on Windows once a
  RID-specific lock file for this project is committed.** Confirmed by: body step 10's two
  commands. *If wrong*, the failure is a lock/RID mismatch and the answer is to regenerate
  with the same RID CI uses, never to drop `--locked-mode`.

## Execution placement

The six-question cloud-justification test from
`docs/desktop/00-governance-and-workflow/README.md` § 3, answered. The responsibility being
placed is **execution of desktop view-model and desktop-infrastructure unit tests**.

| Question | Answer | Evidence |
| --- | --- | --- |
| Shared authority — must several users see and update the same state? | **No** | The tests are source under version control and run against in-memory fakes; there is no shared runtime state. The ticket's Guardrails make this explicit: "Tests must not reach any network endpoint; the gateway is always a fake." |
| Unattended execution — must it run with every desktop closed? | **Yes — on the GitHub-hosted `windows-latest` runner, not in Azure.** | The suite must fail a pull request without a developer present. It cannot run on the ubuntu lanes because the project targets `net10.0-windows10.0.26100.0`. The lane is [[FND-040]]'s `desktop-build` job; until it lands, this responsibility is **unplaced**, which is why the plan says so rather than implying CI coverage. |
| Protected credentials — long-lived secret that must not sit on workstations? | **No** | No secret is needed. The DPAPI round-trip test uses `DataProtectionScope.CurrentUser` against a temporary directory created by the test; nothing long-lived is stored. |
| Public callback — must an external service call a stable public endpoint? | **No** | Nothing calls in. |
| Central enforcement — revocation, permissions, audit, invariant independent of the client? | **Yes — enforced by the repository's own test suite run by CI, which is the client-independent authority.** | Desktop behaviour must be proven regardless of which agent or workstation builds; proposal § 22.2 puts view-model tests in the pyramid for exactly that reason. No service outside the repository is involved and no Azure resource is placed. |
| Measured operational advantage — measured evidence central is materially better? | **No** | The opposite is measured policy: C-01 makes private-repository `windows-latest` minutes bill at 2×, so the suite is required to stay fast and start no process, server or database (body step 11 records the run time for [[FND-040]] to budget against). |

Two "yes" answers, both landing on the CI workflow already in this repository. Nothing lands
in Azure; L-02 keeps the only real stack local and it is explicitly **not** a unit-test
dependency.

## Implications

1. **Copy the csproj shape, change only what the target framework forces.** Seven properties
   plus `TargetPlatformMinVersion`, `Platforms` and `RuntimeIdentifier`; the same four
   packages; `<Using Include="Xunit" />`. Anything else added here is a new convention that
   the other three projects do not have, and needs a reason.
2. **The shared `FixedTimeProvider` is a deliberate deviation and must be labelled as one.**
   Nine per-file copies exist elsewhere; this project starts with one `internal` copy and the
   plan records that consolidating the Core-side copies is out of scope.
3. **Ownership with [[TEST-004]] must be settled by reading the board, not by assuming.**
   It is a named sibling ticket, so it is a scope boundary rather than an open question
   (`operator-decisions.md` and the board conventions both leave sibling ownership to the
   plan's Risks section). The check is two calls — `get_item TEST-004` and
   `ls tests/Pegasus.Desktop.ViewModelTests` — and its outcome is recorded in the plan.
4. **This ticket ships tests CI does not yet run.** That is not a defect — it is the ordering
   the board chose ([[FND-039]] → [[FND-040]]) — but the proof must state it, and the plan
   must hand the measured local run time to [[FND-040]] rather than claiming a green lane.
5. **Package version literals are correct until [[FND-027]] lands, and wrong after.** Read
   for `Directory.Packages.props` before writing the csproj; the body's step 2 assumes it
   exists.
6. **The host fixture is the highest-leverage file here.** Body step 8 asks for one file that
   builds [[FND-032]]'s generic host with fakes substituted, so every later ticket resolves
   real services without duplicating registration. A second copy of that registration in any
   later test file is the third-copy rule applied to tests.

## Open questions

None. The one genuinely undecided thing — whether this ticket or [[TEST-004]] creates the
project — is owned by a **named sibling ticket**, which makes it a scope boundary recorded in
the plan's *Risks / open questions* section, not an open question. It is settled at
implementation time by two read-only calls, and the ticket body already directs that the
answer be agreed "in the ticket plan". No `open-questions` document is created.
