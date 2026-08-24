# EPIC-009 · Area 08 — testing. Shared context

Read this once before working any TEST ticket in this epic. It carries what binds
every ticket here; what is specific to one ticket is in that ticket's body.

## What the area delivers

The test projects, scripts and CI lanes for every layer of the proposal's pyramid, and
the **local** Test/UAT stack that stands in for an Azure test environment. New homes it
creates: `tests/Pegasus.Api.ContractTests`, `tests/Pegasus.Desktop.ViewModelTests`,
`tests/Pegasus.Desktop.UITests`, `eng/packaging/Test-Package.ps1`, `eng/security/*`,
`eng/performance/*`, `eng/reports/*`, the `TestStack` mode of
`scripts/Invoke-LocalDevelopment.ps1`, four `ci.yml` lanes, and the fourteen UAT
scenario scripts under `docs/desktop/08-testing/uat-scenarios/`. Existing homes it
extends: `tests/Pegasus.IntegrationTests` (shards) and `scripts/Invoke-Doctor.ps1`.

## Proposal coverage

§21.2 (fifteen CI stages mapped to jobs), §22.1 (characterization — deliberately
narrow, see deviations), §22.2 (the whole pyramid: domain, application, contract,
server integration, view-model, UI automation, accessibility, packaging/update,
security, performance, end-to-end scenarios 1–14), §22.3 (coverage policy as merge
rules, no global percentage), §23 and §23.2 (parity evidence and the release gate),
§24 (the test lane that proves each phase gate).

## Decisions that bind every ticket here

- **L-02** — Test/UAT is a local production-mimicking stack: local gateway and Worker,
  Azurite 3.36.0, LocalDB, replay adapters, a local file-share feed. **ADR-0014 stands;
  there is no Azure dev/test/staging.** A ticket that asks for an Azure test resource is
  out of bounds. Real Azure is proved only on the production pilot ring.
- **L-03** — reports render on the desktop through an isolated non-UI WebView2 path; the
  gateway renderer is retained until the golden-file parity lane passes (ADR-0108).
- **D-002 / D-003** — distribution is fully decided: sign in-house with a self-managed
  certificate trusted in `LocalMachine\TrustedPeople`, serve from a UNC share over SMB.
  The stack therefore rehearses the real mechanism, with a development certificate.
- **C-01** — the repositories go private on completion; private Windows runner minutes
  bill at 2×. Every added Windows lane has a cost. See DSK-08-19.
- **L-04** — every ticket names its subagent, skills and MCP tools.

## Deviations, stated once

1. **Environments** — proposal §21.3's Azure Test/UAT is replaced by the local stack.
   What it cannot prove is listed in `test-uat-stack.md` § "What the stack proves and
   what it does not"; those rows are pilot-ring checks.
2. **Runtime profile** — no `TestStack` profile is added. `Runtime:Profile` accepts only
   `DevelopmentOffline` and `Production` (`src/Pegasus.Web/Program.cs:101-122`); the
   stack runs under `DevelopmentOffline` with `Features:LocalIntake` and
   `Features:LocalDocumentCustody`.
3. **UI driver** — `winapp ui` (UIA) only. No WinAppDriver, Appium or FlaUI, and no
   driver dependency in the application.
4. **Characterization** — no business rule moves (Core stays the single owner), so §22.1
   is limited to read-model shapes and the `OperatorLabels` vocabulary, as contract
   snapshots.
5. **Coverage** — no coverage gate; `coverlet` stays for reports only.
6. **Test clock** — desktop test projects adopt **one** shared fake clock and one date
   convention; do not add a ninth private `FixedTimeProvider` copy.

## Exit gate for the area, and what proves it

1. The new test projects exist, are in `Pegasus.slnx`, build with
   `TreatWarningsAsErrors` and run in CI on `windows-latest`.
2. `ci.yml` has the desktop lanes and the Linux jobs stay green.
3. The stack starts from one script on a clean Windows 11 machine and scenarios 1–14
   exist with pass/fail recording.
4. The release critical path has run once end to end, with evidence filed under
   `artifacts/` (ignored) and summarised in the release ticket's proof.

## Routing for this area

| Need | Subagent | Skills (exact name · pinned source) | MCP |
| --- | --- | --- | --- |
| Run/filter tests, TRX, shards | `pegasus-test-engineer` | `run-tests` · `dotnet/skills` `98f84851`, plugin `dotnet-test` | Kanmer |
| Write tests, scaffold projects | `pegasus-test-engineer` | `code-testing-agent`, `scaffold-dotnet-test-project` · same pin | Microsoft Learn |
| Gaps and assertion grading | `pegasus-test-engineer` | `test-gap-analysis`, `assertion-quality` · same pin | — |
| `winapp ui`, AutomationId audit, screenshots | `pegasus-ui-verifier` | `winui-ui-testing` · `microsoft/win-dev-skills` v0.5.0 `f1028dd5`, `.codex/skills/winui-ui-testing/` | — |
| Performance traces | `pegasus-ui-verifier` | `analyzing-dotnet-performance`, `dotnet-trace-collect` · plugin `dotnet-diag` | Microsoft Learn |
| Packaging/update tests, MSIX dev cert | `pegasus-release-packager` | `winui-packaging` · win-dev-skills v0.5.0 | Microsoft Learn |
| CI workflow changes | `pegasus-release-packager` | `authoring-github-workflows` · `.agents/skills/authoring-github-workflows` | — |
| Endpoint fixtures and fakes | `pegasus-gateway-dev` | `dotnet-webapi` · plugin `dotnet-aspnetcore` | Microsoft Learn |
| Independent review | `pegasus-desktop-reviewer` | `winui-code-review` · win-dev-skills v0.5.0 | Kanmer |
| Automated accessibility | `pegasus-ui-verifier` | (tool, not a skill) `AxeWindowsCLI` — github.com/microsoft/axe-windows | — |

`pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) is loaded first by
every agent. Never load a skill from the "do not load" table in
`docs/desktop/12-agent-tooling/skill-routing.md`.

## Traps (plan § 7) — they apply to every ticket here

- **CI minutes**: private Windows runners bill at 2×; decide the runner strategy before
  the repositories flip (DSK-08-19), not after.
- **UI flakiness**: AutomationId contract, `wait-for` never `Start-Sleep`, two
  fix-and-rerun cycles maximum.
- **LocalDB is Windows-only**; the integration shards cannot move to Linux.
- **Shard partition drift**: every new integration test lands in exactly one shard;
  `dotnet test` exits 0 when a filter matches nothing, so `-VerifyPartition` is the gate.
- **Browser lane stays** until web retirement — do not remove Playwright/axe-core on
  desktop cutover day.
- **`Category` traits**: `SqlServer` / `Browser` / `ViewModel` / `Security` /
  `Performance` / `Packaging` / `Contract`, or the CI filters lose the tests.
- **`TreatWarningsAsErrors=true` applies to test projects** — suppress per file with a
  reason, never globally.
- **CI checkout timeouts** on the 700 MB repository (DELIV-010): shallow checkout where
  the history guard does not need depth.
- **UI tests mutate the installed package** — dedicated runner or workstation only.
- **Automated axe is not acceptance**: the ten recorded manual reviews are still due per
  release candidate.
- **Never fabricate domain data**; fixtures come from `reference/` and the existing
  builders. `corpus/` is never copied, uploaded or committed.
- **Two policy engines**: contract tests assert that the API and the MCP tools reach the
  same Core use cases through shared fixtures; they never re-implement a rule.

## Read before starting any ticket in this epic

- `docs/desktop/08-testing/README.md` — the area plan (§ 2 facts, § 3 decisions, § 4
  target state and exit gate, § 5 the ticket row, § 6 routing, § 7 traps)
- `docs/desktop/08-testing/test-uat-stack.md` — the stack definition, prerequisites,
  lifecycle verbs, evidence capture, scenarios 1–14, known gaps
- `docs/desktop/README.md` — decisions L-01…L-05, D-001…D-003, C-01, routing legend
- `docs/desktop/00-governance-and-workflow/README.md` — board shape, ticket template,
  ADR block, branching, traps
- `docs/desktop/12-agent-tooling/skill-routing.md` — exact skill names, pins, and the
  "do not load" table
- `AGENTS.md` — task workflow, simplification pass, Markdown placement, safety rails
- `docs/engineering.md` § Required evidence tiers · `docs/runbook.md` § Locked restore,
  build, and test · `docs/operations.md` § Evidence profiles
- `.github/workflows/ci.yml` and `.github/actions/dotnet-build/action.yml`
