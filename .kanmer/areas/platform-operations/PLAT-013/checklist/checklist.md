# Checklist — PLAT-013

## Implementation

- [ ] 1. Orientation. Read the plan row, `performance-baseline.md` and `profiling-runbook.md`, and the release-manifest fields from `DSK-09-02`. Call Kanmer `get_doc_gates` with this ticket's board id, then `take_ticket`.

- [ ] 2. Branch `task/dsk-10-13-performance-regression-report` from `dev`.

- [ ] 3. Define the report as data first: `eng/verification/report-schema.json` describing one JSON object per candidate — package version, commit SHA, baseline machine id, data-set id, run date, and an array of measurements `{ budgetRow, unit, p50, p95, budget, status, previousP95, deltaPercent }`. Every script from [[DSK-10-11]] already emits JSON; this schema is their union.

- [ ] 4. Create `eng/verification/New-PerformanceReport.ps1` that runs the measurement scripts in order (`Measure-Startup.ps1`, `Measure-Navigation.ps1`, `Measure-Memory.ps1`, plus the report-generation and load scenarios), merges their JSON into the schema, loads the previous candidate's JSON for the delta, and writes both `performance-report.json` and a human-readable `performance-report.md`.

- [ ] 5. Implement the gate inside that script: `status` is `pass`, `fail` or `waived`; the script exits 1 when any row is `fail` and no matching waiver is present. A waiver is an entry in `docs/desktop/10-security-observability-performance/performance-waivers.md` naming the budget row, the candidate version, the measured value, the reason, the evidence and an expiry version. Evidence is required; convenience is not (§15.1 `:1074`).

- [ ] 6. Build the load scenario: ten concurrent operator sessions plus the Worker against the local stack. Drive the operator sessions through the gateway API rather than ten desktop instances (the desktop measurement is single-instance; the load run measures whether the gateway and database hold up under the tier-10 shape). Record the mapping explicitly: proposal §22.2 asks for "ten concurrent users plus worker"; `docs/engineering.md:85` fixes the tier-10 shape at eight concurrent operators and 2,000 cases per month. Run ten, and report against both figures rather than silently picking one.

- [ ] 7. Add the soak element: keep the load running long enough to show whether memory and connection counts are flat, and report start/end values. Use the memory thresholds from the runbook, referencing the ≤ 500 MB steady-memory budget and "investigate sustained growth".

- [ ] 8. Write the report template `docs/desktop/10-security-observability-performance/performance-report-template.md` with the fixed sections: candidate identity; baseline machine and data set; the ten §15.1 budget rows with measured p50/p95, budget, status; deltas versus the previous candidate; load-run summary; waivers in force; artefacts retained. Every row must show a number — a budget row reported without a measured value is a failed report, not a pass.

- [ ] 9. Wire it into the release route: add the report generation and the gate to `scripts/Build-DesktopRelease.ps1` (`DSK-09-04`) or to the release runbook R1 (`DSK-09-11`), so a candidate cannot be published without a report. Do not add it to the per-PR CI lanes (C-01).

- [ ] 10. **Operator step** — produce the first real report on the baseline workstation against the Test/UAT stack for the current candidate. Hand back `performance-report.json`, `performance-report.md` and the artefact folder. Confirm each of the ten budget rows carries a number.

- [ ] 11. Attach the report to the release record: add a row reference in `docs/operations.md` § desktop release table (`DSK-09-18`) so the report location is discoverable from the release history.

- [ ] 12. Prove the gate: temporarily set one budget threshold below the measured value, re-run, and confirm the script exits 1 and names the row; then add a waiver and confirm it exits 0 with `waived` recorded. Revert the threshold. Capture both runs.

- [ ] 13. Update `docs/engineering.md` § Required evidence tiers with the regression report as the tier-10 desktop artefact example, as the plan's documentation-changes list requires.

- [ ] 14. Record `## Simplification pass` with today's date over the branch diff in the ticket's `plan` document, open the PR into `dev`, and hand review to `pegasus-desktop-reviewer`.

## Verification

- [ ] `pwsh ./eng/verification/New-PerformanceReport.ps1 -PackageVersion <M.m.b>` — expected: exit 0, both report files written, every budget row populated.
- [ ] Same command with one budget deliberately tightened — expected: exit 1 naming the failing row.
- [ ] Same command with a matching waiver present — expected: exit 0 and the row reported as `waived`.

## Progress notes

Record factual progress here.
