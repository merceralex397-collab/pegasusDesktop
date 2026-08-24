---
id: PLAT-010
type: ticket
title: >-
  DSK-10-10 · Performance baseline: record the lowest-spec workstation, the data
  sizes, the web timings and the budget table
status: preparing
area: platform-operations
assignee: ''
profile: chore
stageEntered:
  preparing: '2026-08-24T21:21:15.010Z'
labels:
  - desktop-conversion
  - plan-10
  - phase-0
  - tier-10
  - needs-operator
groups:
  - EPIC-011
  - HZN-001
links: []
blocks:
  - TEST-015
  - PLAT-011
docs_todo: true
archived: false
created: '2026-08-24T08:10:26.604Z'
updated: '2026-08-24T21:21:15.010Z'
---

## What

Publish `docs/desktop/10-security-observability-performance/performance-baseline.md`: the exact specification of the lowest-spec supported office workstation, the tier-10 data sizes the measurements use, the current web-application timings for the same workflows, and the proposal §15.1 budget table adopted verbatim as the starting targets.

## Why

Proposal §15.1 `:1074` states plainly that the budgets are starting acceptance targets and that **baseline hardware and data sizes must be recorded** before adjustments are argued. The plan's own assumption list records that the lowest-spec office workstation is not yet known, and its risk table names "performance budgets judged on a fast developer machine" as the failure mode. Until this file exists, no measurement in [[DSK-10-11]] or [[DSK-10-13]] means pass or fail, and the programme exit checklist item "startup, navigation and memory budgets met on baseline hardware" cannot be evaluated. Operator-visible consequence: the desktop is declared fast on a developer laptop and feels slower than the web app on the machine that actually runs it.

## Source of truth

- Plan row: `docs/desktop/10-security-observability-performance/README.md` § 5 — `DSK-10-10`
- Plan detail: same file § 2 (Assumptions — "The lowest-spec office workstation is not yet recorded"), § 4 (target state), § 7 ("Performance budgets judged on a fast developer machine")
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 15.1 Provisional performance budgets `:1057-1074`; § 15.3 Profiling `:1097-1111`; § 22.2 Performance tests `:1623-1634`; § 24 Phase 0 "Record baseline performance and critical business fixtures" `:1733`
- Repository evidence:
  - `docs/engineering.md:85` — tier 10 data shape: **eight concurrent operators, 2,000 cases per month, 2–20+ files per case, the one-file 10 MiB limit and the 10 MiB-plus-64-KiB multipart envelope, 48,000–480,000+ annual asset-metadata shapes**; and the standing rule "do not invent a release latency threshold without an explicit decision"
  - `src/Pegasus.Core/Intake/IntakeContracts.cs:7-57` — the file-size constants the data set must respect
  - `tests/Pegasus.IntegrationTests/Browser/OperatorJourneyTests.cs` — the existing browser journey whose steps the web-side timings should mirror
  - `scripts/Invoke-LocalDevelopment.ps1` — the local stack entry point that `DSK-08-17` extends with a `TestStack` mode
- Binding decisions:
  - **L-02** — measurements run on the local production-mimicking stack and, for the web comparison, on production only where a read-only observation is already permitted; no Azure test environment exists.
  - **C-01** — the estate is ten users; the baseline machine is one of the real office workstations, not a hypothetical minimum.
- Depends on: `DSK-01-11` — "Record baseline performance and critical business fixtures", the Phase 0 spike this ticket publishes the budget side of.

## Routing

- **Subagent**: `pegasus-ui-verifier` — `.codex/agents/pegasus-ui-verifier.toml`
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `analyzing-dotnet-performance` (dotnet/skills `98f84851`, plugin `dotnet-diag`) → `dotnet-trace-collect` (same pin)
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`). No Azure MCP write; a read-only `monitor` query may be used to read existing web timings.
- **Kanmer pipeline** for profile `chore`: `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout`
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Orientation. Read the plan row, proposal `:1057-1074` and `:1623-1634`, and `docs/engineering.md:85`. Call Kanmer `get_doc_gates` with this ticket's board id, then `take_ticket`.
2. Branch `task/dsk-10-10-performance-baseline` from `dev`.
3. **Operator step** — identify the lowest-spec office workstation actually in use and hand back: CPU model and core/thread count, installed RAM, storage type and free space, GPU, display resolution and scaling, Windows edition and build number, whether the machine is domain-joined, and the typical network path to the gateway (wired/wireless, nominal bandwidth, observed latency). A specification sheet or `msinfo32` export is the evidence; a guess is not acceptable.
4. Create `docs/desktop/10-security-observability-performance/performance-baseline.md` with sections `## Baseline workstation`, `## Data set`, `## Web-application timings (before)`, `## Budgets`, `## Method` and `## Change log`.
5. Fill `## Baseline workstation` from step 3, with the capture date and who captured it. Add the sentence that every budget in this file is judged on this machine in a **release** build, and that measuring on any other machine must be labelled as such.
6. Fill `## Data set` from the tier-10 shape: eight concurrent operators, 2,000 cases per month, 2–20+ files per case, one-file limit 10 MiB, multipart envelope 10 MiB + 64 KiB, 48,000–480,000+ annual asset-metadata shapes. State the concrete fixtures the measurements use: the case count in the list under test, the document-heavy case, the image-heavy case, and the report used for generation timing. Fixtures must be synthetic or approved non-corpus material — corpus is prohibited (`docs/runbook.md` § Corpus safety and evaluation).
7. Fill `## Web-application timings (before)` by measuring the same workflows in the current web application on the baseline machine: sign-in to dashboard, dashboard load, case list first page, case detail open, save, report generation. Record p50 and p95 over at least ten runs each, plus the method (browser, cache state, network path). These are the "materially faster" comparison the proposal asks for, not a target.
8. Fill `## Budgets` with the proposal §15.1 table copied verbatim:
   | Operation | Initial budget |
   | --- | --- |
   | Cold launch to usable shell | ≤ 3 seconds at p95 |
   | Warm launch | ≤ 1.5 seconds at p95 |
   | Cached page navigation | ≤ 200 ms perceived |
   | First page of ordinary server results | ≤ 1 second excluding provider outage |
   | Ordinary save | ≤ 1 second excluding external side effects |
   | List scrolling | Sustained smooth interaction without visible blocking |
   | Idle CPU | Normally below 1% |
   | Typical steady memory | Target below 500 MB; investigate sustained growth |
   | User cancellation feedback | Immediate |
   | Thumbnail display | Progressive; never blocks case navigation |
   Add the proposal's own sentence: these are starting acceptance targets and adjustments require evidence, not convenience.
9. Turn the two qualitative rows into measurable definitions and say so explicitly: "list scrolling" is measured as no frame interval above 50 ms during a scripted scroll of the large-list fixture; "user cancellation feedback" is measured as the interval between the cancel command and the visible state change, budgeted at ≤ 200 ms to match the navigation row. Mark both as **decided here** rather than taken from the proposal, so the deviation is recorded rather than invented.
10. Fill `## Method` with the measurement rules: release builds only, package-identity launch, three warm-up runs discarded, at least ten measured runs, p50/p95 reported, machine idle, no profiler attached for the headline numbers. Point forward to [[DSK-10-11]] for the tooling and to [[DSK-10-13]] for the release-candidate report.
11. Link the file from `docs/desktop/10-security-observability-performance/README.md` § 8, then run `pwsh ./scripts/Test-DocumentationLinks.ps1` and `pwsh ./scripts/Test-TestMarkdownPlacement.ps1`; both must exit 0.
12. Record `## Simplification pass` as `n/a — docs-only` with today's date in the ticket's `plan` document, open the PR into `dev`, and hand review to `pegasus-desktop-reviewer`.

## Acceptance criteria

- [ ] The baseline workstation is recorded with CPU, RAM, storage, GPU, display, Windows build, domain state and network path, with a capture date and evidence.
- [ ] The data set is stated concretely against the tier-10 shape (eight concurrent operators, 2,000 cases/month, 2–20+ files per case, 10 MiB single-file limit, 10 MiB + 64 KiB envelope) and uses no corpus material.
- [ ] Web-application p50 and p95 timings exist for the six comparison workflows on the baseline machine, with the method recorded.
- [ ] All ten §15.1 budget rows appear verbatim with their numeric targets.
- [ ] The two qualitative rows have measurable definitions, marked as decided in this ticket.
- [ ] The method section fixes release builds, warm-ups, run count and p50/p95 reporting.

## Verification

- [ ] `pwsh ./scripts/Test-DocumentationLinks.ps1` — expected: exit 0.
- [ ] `pwsh ./scripts/Test-TestMarkdownPlacement.ps1` — expected: exit 0.
- [ ] Measurement log attached to the ticket — expected: at least ten runs per web workflow with timestamps, showing p50 and p95 as published.

## Evidence tier

Tier 10 — Performance/concurrency. Here that obliges the recorded data shape (eight concurrent operators, 2,000 cases per month, the 10 MiB single-file and 10 MiB + 64 KiB envelope limits) and forbids inventing a latency threshold without an explicit decision — which is why the two qualitative rows are decided and recorded here rather than assumed.

## Documentation changes

- `docs/desktop/10-security-observability-performance/performance-baseline.md` — new file.
- `docs/desktop/10-security-observability-performance/README.md` § 8 — register the file.

## Guardrails

- **Azure**: no write. Reading existing production timings via Azure MCP `monitor`/`applicationinsights` is permitted and free — but note the 0.1 GB/day cap means working-hour data is usually absent, so the web timings must be measured directly, not inferred from telemetry.
- **Scope boundary**: documentation and measurement only; no code change. Board placement note: plan 00 § Kanmer board shape assigns no board area to plan 10, so this epic seeds into `platform-operations` (prefix `PLAT`).
- **Traps**: measuring on a developer machine invalidates every later comparison; corpus material must never be used as a performance fixture; "do not invent a release latency threshold without an explicit decision" (`docs/engineering.md:85`) — hence step 9 records its two definitions as decisions; the desktop does not exist yet at Phase 0, so this ticket measures the **web** baseline and publishes the desktop targets, it does not measure the desktop.
- **Simplification pass** (`AGENTS.md` step 4): `n/a — docs-only`, recorded under a dated `## Simplification pass` heading in the ticket's `plan` document.

## Outcome

_Filled at closeout._
