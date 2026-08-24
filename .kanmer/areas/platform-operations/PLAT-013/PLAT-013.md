---
id: PLAT-013
type: ticket
title: >-
  DSK-10-13 · Release-candidate performance regression report: template, job,
  and the budget gate
status: preparing
area: platform-operations
assignee: ''
profile: feature
stageEntered:
  preparing: '2026-08-24T21:21:15.344Z'
labels:
  - desktop-conversion
  - plan-10
  - phase-8
  - tier-10
  - needs-operator
groups:
  - EPIC-011
  - HZN-009
links: []
docs_todo: true
archived: false
created: '2026-08-24T08:16:25.556Z'
updated: '2026-08-24T21:21:15.344Z'
---

## What

Produce a performance regression report for every release candidate: budgets versus measured, deltas versus the previous release, and a ten-operator-plus-Worker load run on the local Test/UAT stack — with a failing budget blocking the release unless waived with recorded evidence.

## Why

Proposal §15.3 `:1111` states plainly that "a performance regression report is required for release candidates", and the plan's exit gate (§ 4) makes the report an attachment to the release record. The programme exit checklist item 11 is "startup, navigation and memory budgets met on baseline hardware". Without a per-candidate report, regressions accumulate silently between releases and the budget table from [[DSK-10-10]] is aspirational. Operator-visible consequence: the desktop gets slower release by release and nobody can say which release did it. Siblings: [[DSK-10-10]] (baseline and budgets), [[DSK-10-11]] (how the numbers are collected), [[DSK-10-12]] (what prevents the regression at review time).

## Source of truth

- Plan row: `docs/desktop/10-security-observability-performance/README.md` § 5 — `DSK-10-13`
- Plan detail: same file § 4 (target state — "a regression report accompanies the release record"), § 7
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 15.1 budgets `:1057-1074`; § 15.3 Profiling `:1097-1111`; § 22.2 Performance tests `:1623-1634` ("ten concurrent users plus worker"); § 24 Phase 8 exit gate `:1885-1890`
- Repository evidence:
  - `docs/engineering.md:85` — tier 10: **eight concurrent operators, 2,000 cases per month, 2–20+ files per case, one-file 10 MiB limit, 10 MiB + 64 KiB multipart envelope**, burst/soak behaviour, 48,000–480,000+ annual asset-metadata shapes; and "do not invent a release latency threshold without an explicit decision"
  - `docs/desktop/10-security-observability-performance/performance-baseline.md` — the baseline machine, data set and budget table ([[DSK-10-10]])
  - `docs/desktop/10-security-observability-performance/profiling-runbook.md` and `eng/verification/*.ps1` — the collection scripts ([[DSK-10-11]])
  - `scripts/Invoke-LocalDevelopment.ps1` — the local stack entry point extended by `DSK-08-17` with a `TestStack` mode
  - `docs/desktop/09-release-update-and-distribution/README.md` § 5 rows `DSK-09-02`, `DSK-09-04`, `DSK-09-11` — the release manifest and runbook this report attaches to
- Binding decisions:
  - **L-02** — the load run happens on the local production-mimicking stack (local gateway and Worker, Azurite, LocalDB or a SQL container, replay adapters) plus the production pilot ring; there is no Azure test environment (ADR-0014).
  - **C-01** — private-repository Windows minutes bill at 2×; the report job runs on the release route or a self-hosted runner, not on every PR.
- Depends on: `DSK-10-11` (profiling procedure and scripts), `DSK-08-17` (Test/UAT stack lifecycle).

## Routing

- **Subagent**: `pegasus-ui-verifier` — `.codex/agents/pegasus-ui-verifier.toml`
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `analyzing-dotnet-performance` (dotnet/skills `98f84851`, plugin `dotnet-diag`) → `run-tests` (same pin) → `winui-ui-testing` (`.codex/skills/winui-ui-testing/SKILL.md`, win-dev-skills v0.5.0 `f1028dd5`) for the scripted runs
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`)
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout`
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Orientation. Read the plan row, `performance-baseline.md` and `profiling-runbook.md`, and the release-manifest fields from `DSK-09-02`. Call Kanmer `get_doc_gates` with this ticket's board id, then `take_ticket`.
2. Branch `task/dsk-10-13-performance-regression-report` from `dev`.
3. Define the report as data first: `eng/verification/report-schema.json` describing one JSON object per candidate — package version, commit SHA, baseline machine id, data-set id, run date, and an array of measurements `{ budgetRow, unit, p50, p95, budget, status, previousP95, deltaPercent }`. Every script from [[DSK-10-11]] already emits JSON; this schema is their union.
4. Create `eng/verification/New-PerformanceReport.ps1` that runs the measurement scripts in order (`Measure-Startup.ps1`, `Measure-Navigation.ps1`, `Measure-Memory.ps1`, plus the report-generation and load scenarios), merges their JSON into the schema, loads the previous candidate's JSON for the delta, and writes both `performance-report.json` and a human-readable `performance-report.md`.
5. Implement the gate inside that script: `status` is `pass`, `fail` or `waived`; the script exits 1 when any row is `fail` and no matching waiver is present. A waiver is an entry in `docs/desktop/10-security-observability-performance/performance-waivers.md` naming the budget row, the candidate version, the measured value, the reason, the evidence and an expiry version. Evidence is required; convenience is not (§15.1 `:1074`).
6. Build the load scenario: ten concurrent operator sessions plus the Worker against the local stack. Drive the operator sessions through the gateway API rather than ten desktop instances (the desktop measurement is single-instance; the load run measures whether the gateway and database hold up under the tier-10 shape). Record the mapping explicitly: proposal §22.2 asks for "ten concurrent users plus worker"; `docs/engineering.md:85` fixes the tier-10 shape at eight concurrent operators and 2,000 cases per month. Run ten, and report against both figures rather than silently picking one.
7. Add the soak element: keep the load running long enough to show whether memory and connection counts are flat, and report start/end values. Use the memory thresholds from the runbook, referencing the ≤ 500 MB steady-memory budget and "investigate sustained growth".
8. Write the report template `docs/desktop/10-security-observability-performance/performance-report-template.md` with the fixed sections: candidate identity; baseline machine and data set; the ten §15.1 budget rows with measured p50/p95, budget, status; deltas versus the previous candidate; load-run summary; waivers in force; artefacts retained. Every row must show a number — a budget row reported without a measured value is a failed report, not a pass.
9. Wire it into the release route: add the report generation and the gate to `scripts/Build-DesktopRelease.ps1` (`DSK-09-04`) or to the release runbook R1 (`DSK-09-11`), so a candidate cannot be published without a report. Do not add it to the per-PR CI lanes (C-01).
10. **Operator step** — produce the first real report on the baseline workstation against the Test/UAT stack for the current candidate. Hand back `performance-report.json`, `performance-report.md` and the artefact folder. Confirm each of the ten budget rows carries a number.
11. Attach the report to the release record: add a row reference in `docs/operations.md` § desktop release table (`DSK-09-18`) so the report location is discoverable from the release history.
12. Prove the gate: temporarily set one budget threshold below the measured value, re-run, and confirm the script exits 1 and names the row; then add a waiver and confirm it exits 0 with `waived` recorded. Revert the threshold. Capture both runs.
13. Update `docs/engineering.md` § Required evidence tiers with the regression report as the tier-10 desktop artefact example, as the plan's documentation-changes list requires.
14. Record `## Simplification pass` with today's date over the branch diff in the ticket's `plan` document, open the PR into `dev`, and hand review to `pegasus-desktop-reviewer`.

## Acceptance criteria

- [ ] A machine-readable `performance-report.json` and a human-readable `performance-report.md` are produced for a candidate by one command.
- [ ] All ten §15.1 budget rows appear with measured p50/p95, the numeric budget and a status — no row is blank.
- [ ] Deltas versus the previous candidate are computed and shown as a percentage.
- [ ] A failing budget exits the script non-zero and blocks the release unless a waiver with evidence and an expiry version exists.
- [ ] The load run covers ten concurrent operator sessions plus the Worker on the local stack, and reports against both the ten-user figure and the tier-10 eight-operator shape.
- [ ] The first real report exists for the current candidate and is referenced from the release record.

## Verification

- [ ] `pwsh ./eng/verification/New-PerformanceReport.ps1 -PackageVersion <M.m.b>` — expected: exit 0, both report files written, every budget row populated.
- [ ] Same command with one budget deliberately tightened — expected: exit 1 naming the failing row.
- [ ] Same command with a matching waiver present — expected: exit 0 and the row reported as `waived`.

## Evidence tier

Tier 10 — Performance/concurrency. Here that obliges measurement on the recorded baseline machine against the tier-10 data shape, including burst and soak behaviour, with the numbers retained as the release artefact — and forbids inventing a threshold without an explicit decision, which is why every budget traces to §15.1 or to a recorded decision in [[DSK-10-10]].

## Documentation changes

- `docs/desktop/10-security-observability-performance/performance-report-template.md` and `performance-waivers.md` — new files.
- `docs/operations.md` — where the performance regression report lives and its link from the desktop release table.
- `docs/engineering.md` § Required evidence tiers — the regression report as a tier-10 example.

## Guardrails

- **Azure**: no write. The load run is entirely local (L-02); no Azure test resource may be requested (ADR-0014).
- **Scope boundary**: may create `eng/verification/*`, documentation under `docs/desktop/`, and edit the desktop release script/runbook. Must not change application code to meet a budget — a remediation is its own ticket. Board placement note: plan 00 § Kanmer board shape assigns no board area to plan 10, so this epic seeds into `platform-operations` (prefix `PLAT`).
- **Traps**: a waiver without evidence and an expiry becomes permanent — the schema requires both; running the report on a developer machine invalidates the delta series; comparing against a candidate measured with a different data set is meaningless, so the data-set id is part of the report identity; CI cost under C-01 keeps this off the per-PR lanes.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the ticket's `plan` document.

## Outcome

_Filled at closeout._
