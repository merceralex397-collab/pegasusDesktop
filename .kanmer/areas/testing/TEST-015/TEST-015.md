---
id: TEST-015
type: ticket
title: >-
  DSK-08-15 · Performance scripts on the Test/UAT workstation: startup,
  navigation, large list, heavy case, memory, slow network, provider timeout,
  ten users, report generation
status: backlog
area: testing
assignee: ''
profile: feature
labels:
  - desktop-conversion
  - plan-08
  - phase-8
  - tier-10
  - needs-operator
groups:
  - EPIC-009
  - HZN-009
links: []
docs_todo: true
archived: false
created: '2026-08-24T07:53:33.960Z'
updated: '2026-08-24T07:53:33.960Z'
---

## What

Script the nine performance measurements of proposal §22.2 on the Test/UAT workstation and emit one table per run: measured value against the §15.1 budget, with the baseline hardware recorded beside it.

## Why

Proposal §15.1 sets provisional budgets and §24 makes them exit gates twice — Phase 3 ("paging/filtering/performance budgets pass") and Phase 7 ("performance target passes on baseline hardware"). The repository has no performance lane at all: `docs/operations.md` § Evidence profiles records that the `Performance` profile has no lane and that the nightly pressure probe was retired on 2026-08-18 as diagnostic-only CI that gated nothing. Without a scripted measurement against a named machine, "it feels fast on my laptop" is the only evidence a release candidate will have. Runs on the stack from [[DSK-08-17]] against the budgets published by [[DSK-10-10]].

## Source of truth

- Plan row: `docs/desktop/08-testing/README.md` § 5 — `DSK-08-15`
- Plan detail: `docs/desktop/08-testing/test-uat-stack.md` § "Evidence capture" (the §15.1 budget table with measured values and the baseline hardware description) and § "What the stack proves and what it does not" (tier 10 is only partly provable locally — ten desktops against the local gateway is realistic, Azure SQL latency is not)
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 15.1 provisional performance budgets, § 15.3 profiling, § 22.2 "Performance tests"
- Repository evidence:
  - `docs/operations.md` § Evidence profiles — `Performance`: "No lane", the retired probe (DELIV-007), the trait reserved for a future lane with an accepted capacity claim
  - `docs/engineering.md` § Required evidence tiers, tier 10 — eight concurrent operators, 2,000 cases a month, 2–20+ files per case, the 10 MiB single-file limit and the 10 MiB-plus-64-KiB multipart envelope; "Do not invent a release latency threshold without an explicit decision"
  - `scripts/Invoke-LocalDevelopment.ps1:1-13` — the existing failure-injection parameters `-FailureMode None|AfterWeb|StoragePressure` and `-StoragePressureMegabytes`, reused for the slow-network and provider-timeout scenarios
- Binding decisions:
  - L-02 — measurements are local; Azure SQL latency, Container App cold start and real provider round-trips are pilot-ring observations, and the report must say so rather than implying otherwise.
- Depends on: `DSK-08-17` — the Test/UAT stack and its failure-injection modes. `DSK-10-10` — the published §15.1 budget table and the recorded baseline workstation specification.

## Routing

- **Subagent**: `pegasus-ui-verifier` — `.codex/agents/pegasus-ui-verifier.toml`
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `analyzing-dotnet-performance` (`dotnet/skills` `98f84851`, plugin `dotnet-diag`) → `dotnet-trace-collect` (same pin)
- **MCP**: Microsoft Learn (`microsoft_docs_search`, `microsoft_docs_fetch`) for `dotnet-counters`/`dotnet-trace` usage on a packaged app; Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`)
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (call `get_doc_gates <id>` before every move; a move crosses at most one gated boundary)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Read `docs/desktop/08-testing/README.md` § 5 row `DSK-08-15`, `docs/desktop/08-testing/test-uat-stack.md` § "Evidence capture", `docs/desktop/10-security-observability-performance/README.md` § 5 rows `DSK-10-10`, `DSK-10-11` and `DSK-10-13`, and proposal §15.1 for the budget list. Call `get_doc_gates` on this ticket id, then `take_ticket`, and work in the ticket's own worktree and branch.
2. **Operator step**: record the baseline hardware — the lowest-specification office workstation model, CPU, memory, disk type, display scaling and Windows build. Hand back the specification exactly as it will appear in every report. A measurement without a named machine is not evidence.
3. Load `pegasus-desktop`, then `analyzing-dotnet-performance`. Create `eng/performance/Measure-Desktop.ps1` with `-Scenario` (default `All`), `-Iterations` (default 5), `-OutputPath` (default `artifacts/performance/`) and `-BaselineFile`. Every scenario reports median and 95th percentile over the iterations, never a single sample.
4. Implement `ColdStart` and `WarmStart`: measure from process launch to the shell being interactive, using the app's own startup diagnostic timestamps ([[DSK-02-11]]) rather than a wall-clock guess, and confirm the two are distinguished by clearing the standby list or restarting between cold runs.
5. Implement `RepeatedNavigation`: navigate a fixed route sequence N times, recording per-navigation duration; and `LargeList`: open the case list at the tier 10 data size (2,000 cases a month scale) and record time to first row and time to fully rendered page.
6. Implement `DocumentHeavyCase` and `ImageHeavyCase` at the tier 10 shapes (2–20+ files per case, 10 MiB single-file limit) and record open time and peak working set.
7. Implement `MemoryAfterProlongedUse`: run the navigation sequence for a sustained period and record working set and managed heap at start, midpoint and end, with `dotnet-counters`. Follow `dotnet-trace-collect` for attaching to a packaged process — verify the exact procedure with `microsoft_docs_search` before scripting it.
8. Implement `SlowNetwork` and `ProviderTimeout` by reusing the existing failure-injection modes of `scripts/Invoke-LocalDevelopment.ps1` (`-FailureMode AfterWeb`, `-FailureMode StoragePressure`) rather than adding a new mechanism, and record the operator-visible behaviour as well as the timing — a timeout that is fast but silent is a defect.
9. Implement `TenConcurrentUsersPlusWorker`: drive ten concurrent API clients against the local gateway with the Worker running, and record gateway response times and desktop responsiveness during the load. The stack makes this realistic; Azure SQL latency does not appear here and the report must say so.
10. Implement `ReportGeneration`: measure the desktop WebView2 render-to-PDF path end to end ([[DSK-05-18]]), reporting median and 95th percentile.
11. Emit one Markdown table per run: scenario, budget from §15.1, measured median, measured p95, pass/fail, and a footer naming the baseline hardware, the stack version and the package version. Emit the same data as JSON so [[DSK-10-13]] can diff releases.
12. **Operator step**: run `-Scenario All` on the baseline workstation and hand back the report. File it as ticket proof; `artifacts/` is ignored and nothing is committed.
13. Add a line to the report and to `docs/operations.md` stating exactly what the local measurement does not prove (Azure SQL latency, Container App behaviour, real provider round-trips), then run the simplification pass over the branch diff before opening the PR.

## Acceptance criteria

- [ ] All nine scenarios are scripted and report median and p95 over at least five iterations.
- [ ] Every run's output names the baseline hardware, stack version and package version.
- [ ] Budgets come from the §15.1 table published by [[DSK-10-10]]; no threshold is invented here.
- [ ] Slow-network and provider-timeout scenarios reuse the existing failure-injection modes.
- [ ] The report states what the local measurement cannot prove.

## Verification

- [ ] `pwsh ./eng/performance/Measure-Desktop.ps1 -Scenario All -Iterations 5` — expected: exit 0, a Markdown table and a JSON file under `artifacts/performance/`, every scenario populated.
- [ ] Rerun `-Scenario ColdStart` — expected: median within a stated tolerance of the first run; a wildly different value means the cold/warm distinction is not being enforced.
- [ ] The filed report shows a budget column matching proposal §15.1 exactly.

## Evidence tier

Tier 10 — Performance/concurrency. It obliges the recorded sizes (eight to ten concurrent operators, 2,000 cases a month, 2–20+ files per case, the 10 MiB file limit) to be exercised on named hardware, and forbids inventing a release latency threshold without an explicit decision.

## Documentation changes

- `docs/operations.md` § Evidence profiles — replace the `Performance` "No lane" entry with this lane and what it proves and does not prove.
- `docs/desktop/08-testing/README.md` § 4 — mark the performance row as scripted.

## Guardrails

- **Azure**: no write, and no measurement against a production resource.
- **Scope boundary**: may create `eng/performance/**` and write under `artifacts/`. Must not change application code to improve a number — a missed budget is a finding for `winui-dev` and area 10, and must not add a new failure-injection mechanism to `Invoke-LocalDevelopment.ps1`.
- **Traps**: `docs/engineering.md` tier 10 forbids inventing a release latency threshold without an explicit decision — use the published budgets or record the gap. Ten desktops against a local gateway is realistic; Azure SQL latency is not, and the report must not imply it. Never fabricate domain data for the large-list and heavy-case fixtures. A single sample is not a measurement.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.

## Outcome

_Filled at closeout._
