---
id: PLAT-011
type: ticket
title: DSK-10-11 · Profiling procedure and tooling for the desktop client
status: backlog
area: platform-operations
assignee: ''
profile: chore
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
blocks:
  - PLAT-013
docs_todo: true
archived: false
created: '2026-08-24T08:10:26.626Z'
updated: '2026-08-24T08:51:40.714Z'
---

## What

Write the profiling runbook for the desktop client and the scripts that execute it: release-build measurement, Windows Performance Recorder/Analyzer traces, `dotnet-counters` and `dotnet-trace` collection, memory snapshots before and after repeated navigation, API and provider timings, cold and warm start, update launch, and constrained-network behaviour.

## Why

Proposal §15.3 `:1097-1111` sets out exactly what must be captured and states that a performance regression report is required for release candidates. Without a written, rehearsed procedure the numbers in [[DSK-10-13]] are not comparable between releases, and the plan's risk "memory growth from image/document views and event subscriptions" has no detection method. The budgets in [[DSK-10-10]] are only enforceable if there is one agreed way to measure them on the baseline workstation. Operator-visible consequence: a release ships with a regression nobody can quantify, and the argument about whether it is real cannot be settled.

## Source of truth

- Plan row: `docs/desktop/10-security-observability-performance/README.md` § 5 — `DSK-10-11`
- Plan detail: same file § 2 (Facts — desktop performance tooling in the vendored skills), § 4 (target state), § 7 ("Memory growth from image/document views and event subscriptions")
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 15.3 Profiling `:1097-1111`; § 15.1 budgets `:1057-1074`; § 22.2 Performance tests `:1623-1634`
- Repository evidence:
  - `docs/desktop/10-security-observability-performance/performance-baseline.md` — the baseline machine, data set and budget table this procedure measures against ([[DSK-10-10]])
  - `.codex/skills/winui-ui-testing/SKILL.md` — the `winapp ui` harness used to drive scripted runs
  - `scripts/Invoke-LocalDevelopment.ps1` — the local stack entry point extended by `DSK-08-17` with a `TestStack` mode
  - `docs/desktop/08-testing/README.md` § 5 row `DSK-08-15` — the performance script list this procedure standardises
  - New: `eng/verification/` does not exist yet — this ticket creates it (`eng/` is absent from the repository root today)
- Binding decisions:
  - **L-02** — profiling runs on the local production-mimicking stack and on the pilot ring; there is no Azure test environment.
  - **ADR-0109** — measurement evidence is local artefacts, not a telemetry fleet.
- Depends on: `DSK-10-10` — the baseline workstation, data set and budgets this procedure measures against.

## Routing

- **Subagent**: `pegasus-ui-verifier` — `.codex/agents/pegasus-ui-verifier.toml`
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `dotnet-trace-collect` (dotnet/skills `98f84851`, plugin `dotnet-diag`) → `dump-collect` (same pin) → `analyzing-dotnet-performance` (same pin) → `winui-ui-testing` (`.codex/skills/winui-ui-testing/SKILL.md`, win-dev-skills v0.5.0 `f1028dd5`) for scripted runs
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search`) for `wpr.exe` profile names and `dotnet-counters` provider names on a packaged app
- **Kanmer pipeline** for profile `chore`: `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout`
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Orientation. Read the plan row, proposal `:1097-1111`, and `performance-baseline.md` from [[DSK-10-10]] so the budgets and data set are fixed before any script is written. Call Kanmer `get_doc_gates` with this ticket's board id, then `take_ticket`.
2. Branch `task/dsk-10-11-profiling-procedure` from `dev`.
3. Create the directory `eng/verification/` (new; the repository has no `eng/` root today — `DSK-03-05` creates `eng/api/` and `DSK-08-10` creates `eng/packaging/`, so follow the same layout).
4. Write `docs/desktop/10-security-observability-performance/profiling-runbook.md` with one section per §15.3 capture: WPR/WPA traces; .NET counters and traces; UI-thread stalls; API and external dependency timings; memory snapshots before and after repeated navigation; image and document workflows; cold and warm startup; update launch; network-constrained behaviour. Each section states the command, the artefact it produces, where the artefact is filed, and which budget row it evidences.
5. Create `eng/verification/Measure-Startup.ps1`: launches the installed package (cold and warm), measures time to a usable shell using the `winapp ui` `wait-for` primitive against a known `AutomationId` from `DSK-02-08`, discards three warm-ups, runs at least ten iterations, and emits p50/p95 as JSON. Cold launch must clear the standby list or restart between runs — state exactly how, so "cold" means the same thing every time.
6. Create `eng/verification/Measure-Navigation.ps1`: drives a scripted navigation loop across the rail items and back, records per-navigation perceived time, and reports the maximum frame interval during a scripted scroll of the large-list fixture (the definition [[DSK-10-10]] fixed for the "list scrolling" row).
7. Create `eng/verification/Measure-Memory.ps1`: takes a working-set and managed-heap snapshot at start, repeats a navigation/document-open loop N times, snapshots again, and reports the delta. Use `dotnet-counters` for the live series and `dump-collect` guidance for a heap dump when the delta exceeds the threshold. Record the threshold in the runbook, referencing the ≤ 500 MB steady-memory budget and "investigate sustained growth".
8. Create `eng/verification/Collect-Trace.ps1`: wraps `wpr -start <profile>` / `wpr -stop <etl>` and `dotnet-trace collect` for a named scenario, writing artefacts to `artifacts/performance/<date>-<scenario>/`. Use `microsoft_docs_search` to confirm the WPR profile names before hard-coding them; a wrong profile silently collects the wrong events.
9. Add the constrained-network procedure: how to apply a bandwidth/latency constraint on the test machine (name the exact mechanism used and its settings), which scenarios to re-run under it, and what "acceptable degradation" means — the budgets exclude provider outage, so record what is excluded rather than relaxing the number.
10. Add the update-launch procedure: measure the first launch after an App Installer update from the Test/UAT feed (`DSK-04-12`), since that path does extra work and is the launch users notice.
11. **Operator step** — dry-run the whole runbook once on the baseline workstation against the local Test/UAT stack (`DSK-08-17`). Hand back the artefact folder and a note of every step that did not work as written. Fix the runbook until a second person can execute it without asking questions.
12. Record the artefact retention rule: performance artefacts are large, so state what is kept (the JSON summaries, always) and what is discarded (raw `.etl`/`.nettrace` after the release record is written), and confirm `artifacts/` is git-ignored.
13. Link the runbook from `docs/desktop/10-security-observability-performance/README.md` § 8 and add the pointer in `docs/runbook.md` required by the plan's documentation-changes list. Run `pwsh ./scripts/Test-DocumentationLinks.ps1`.
14. Record `## Simplification pass` with today's date over the branch diff in the ticket's `plan` document, open the PR into `dev`, and hand review to `pegasus-desktop-reviewer`.

## Acceptance criteria

- [ ] The runbook covers every §15.3 capture, each naming its command, artefact and the budget row it evidences.
- [ ] Scripts exist under `eng/verification/` for startup, navigation, memory and trace collection, and emit machine-readable JSON summaries.
- [ ] Cold versus warm launch is defined operationally, not left to interpretation.
- [ ] The constrained-network mechanism and its exact settings are recorded.
- [ ] The update-launch measurement is included.
- [ ] A dry run on the baseline workstation completed and every correction is folded back into the runbook.
- [ ] Artefact retention is stated and `artifacts/` is git-ignored.

## Verification

- [ ] `pwsh ./eng/verification/Measure-Startup.ps1 -Iterations 10` — expected: a JSON summary with p50 and p95 for cold and warm launch.
- [ ] `pwsh ./eng/verification/Measure-Memory.ps1 -Iterations 20` — expected: a JSON summary with the start/end working set and managed heap and the delta.
- [ ] `pwsh ./scripts/Test-DocumentationLinks.ps1` — expected: exit 0.

## Evidence tier

Tier 10 — Performance/concurrency. Here that obliges measurement against the recorded tier-10 data shape on the recorded baseline machine in release builds, with burst and soak behaviour covered by the repeated-navigation and memory procedures.

## Documentation changes

- `docs/desktop/10-security-observability-performance/profiling-runbook.md` — new file.
- `docs/runbook.md` — pointer to the profiling runbook and to `eng/verification/`.
- `docs/desktop/10-security-observability-performance/README.md` § 8 — register both.

## Guardrails

- **Azure**: no write.
- **Scope boundary**: may create `eng/verification/` and documentation under `docs/desktop/` and `docs/runbook.md`. Must not change application code to make a measurement look better — a fix is its own ticket. Board placement note: plan 00 § Kanmer board shape assigns no board area to plan 10, so this epic seeds into `platform-operations` (prefix `PLAT`).
- **Traps**: measurements in debug builds or with a profiler attached are not comparable — the headline numbers are release-build and unattached; an undefined "cold launch" makes every later comparison arguable; large `.etl` artefacts committed to the repository would bloat it — keep them in git-ignored `artifacts/`; any new `.md` must live under `docs/(prd|frd|adr|design|desktop)` or the CI `documentation` job fails, so the runbook belongs in the plan folder and only a pointer goes in `docs/runbook.md`.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the ticket's `plan` document.

## Outcome

_Filled at closeout._
