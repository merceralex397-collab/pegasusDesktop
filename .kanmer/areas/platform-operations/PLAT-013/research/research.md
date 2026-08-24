# Research — PLAT-013

## Question

Produce a performance regression report for every release candidate: budgets versus measured, deltas versus the previous release, and a ten-operator-plus-Worker load run on the local Test/UAT stack — with a failing budget blocking the release unless waived with recorded evidence.

## Findings

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

## Implications

Proposal §15.3 `:1111` states plainly that "a performance regression report is required for release candidates", and the plan's exit gate (§ 4) makes the report an attachment to the release record. The programme exit checklist item 11 is "startup, navigation and memory budgets met on baseline hardware". Without a per-candidate report, regressions accumulate silently between releases and the budget table from [[DSK-10-10]] is aspirational. Operator-visible consequence: the desktop gets slower release by release and nobody can say which release did it. Siblings: [[DSK-10-10]] (baseline and budgets), [[DSK-10-11]] (how the numbers are collected), [[DSK-10-12]] (what prevents the regression at review time).

## Constraints

- **Azure**: no write. The load run is entirely local (L-02); no Azure test resource may be requested (ADR-0014).
- **Scope boundary**: may create `eng/verification/*`, documentation under `docs/desktop/`, and edit the desktop release script/runbook. Must not change application code to meet a budget — a remediation is its own ticket. Board placement note: plan 00 § Kanmer board shape assigns no board area to plan 10, so this epic seeds into `platform-operations` (prefix `PLAT`).
- **Traps**: a waiver without evidence and an expiry becomes permanent — the schema requires both; running the report on a developer machine invalidates the delta series; comparing against a candidate measured with a different data set is meaningless, so the data-set id is part of the report identity; CI cost under C-01 keeps this off the per-PR lanes.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the ticket's `plan` document.

## Conclusion

The ticket's cited evidence is sufficient to plan the bounded change. No planned canonical document is linked or claimed to exist.
