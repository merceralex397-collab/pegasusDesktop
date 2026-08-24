# File map — PLAT-013

## Direct change surface

- `docs/desktop/10-security-observability-performance/performance-report-template.md` and `performance-waivers.md` — new files.
- `docs/operations.md` — where the performance regression report lives and its link from the desktop release table.
- `docs/engineering.md` § Required evidence tiers — the regression report as a tier-10 example.

## Context files

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

## Ripple effects

- [ ] A machine-readable `performance-report.json` and a human-readable `performance-report.md` are produced for a candidate by one command.
- [ ] All ten §15.1 budget rows appear with measured p50/p95, the numeric budget and a status — no row is blank.
- [ ] Deltas versus the previous candidate are computed and shown as a percentage.
- [ ] A failing budget exits the script non-zero and blocks the release unless a waiver with evidence and an expiry version exists.
- [ ] The load run covers ten concurrent operator sessions plus the Worker on the local stack, and reports against both the ten-user figure and the tier-10 eight-operator shape.
- [ ] The first real report exists for the current candidate and is referenced from the release record.

## Out of scope

- **Azure**: no write. The load run is entirely local (L-02); no Azure test resource may be requested (ADR-0014).
- **Scope boundary**: may create `eng/verification/*`, documentation under `docs/desktop/`, and edit the desktop release script/runbook. Must not change application code to meet a budget — a remediation is its own ticket. Board placement note: plan 00 § Kanmer board shape assigns no board area to plan 10, so this epic seeds into `platform-operations` (prefix `PLAT`).
- **Traps**: a waiver without evidence and an expiry becomes permanent — the schema requires both; running the report on a developer machine invalidates the delta series; comparing against a candidate measured with a different data set is meaningless, so the data-set id is part of the report identity; CI cost under C-01 keeps this off the per-PR lanes.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the ticket's `plan` document.
