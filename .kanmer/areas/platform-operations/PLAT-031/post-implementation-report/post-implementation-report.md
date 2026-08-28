# PLAT-031 post-implementation report

## Result

Implemented the narrow runtime permission correction for PLAT-018. The branch adds a new SQL Server-guarded migration that grants Worker `INSERT` on `dbo.CaseReportVersionLedgers`, and updates the two existing expected-state consumers that must know about the grant.

## Files changed

- `src/Pegasus.Infrastructure/Persistence/Migrations/20260828074800_GrantWorkerCaseReportVersionLedgerInsert.cs`
  - SQL Server provider guard.
  - Managed `pegasus_worker_runtime_role` validation.
  - `Up`: `GRANT INSERT` on `[dbo].[CaseReportVersionLedgers]`.
  - `Down`: matching `REVOKE INSERT`.
- `scripts/Invoke-AzureDatabaseBootstrap.ps1`
  - names the grant-carrying migration and adds the expected Worker `INSERT` matrix row; no Azure command was executed.
- `tests/Pegasus.IntegrationTests/AzureSqlRuntimeRoleMigrationTests.cs`
  - adds the Worker `CaseReportVersionLedgers:INSERT` expectation to the existing latest-ledger test.

No existing migration, production source, grant-census script, CI job, cloud resource, deployment, credential, upstream remote, or `corpus/` content changed.

## Evidence

The defect was independently exposed by PLAT-018's composition-root analysis: `EfCaseWorkflowStore` inserts the ledger, while `20260827231948_IssuedReportVersionEvidenceLedger` granted the Worker role only `SELECT, UPDATE`.

## Verification

- `dotnet restore Pegasus.slnx --locked-mode` — passed.
- `dotnet build Pegasus.slnx --configuration Release --no-restore -p:UseSharedCompilation=false -p:BuildInParallel=false -p:NodeReuse=false` — passed.
- `dotnet test tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj --configuration Release --no-restore -p:UseSharedCompilation=false -p:BuildInParallel=false -p:NodeReuse=false --verbosity minimal` — passed, 111 total / 111 succeeded / 0 failed / 0 skipped.
- `dotnet test tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --no-restore -p:UseSharedCompilation=false -p:BuildInParallel=false -p:NodeReuse=false --filter "FullyQualifiedName~AzureSqlRuntimeRoleMigrationTests.LatestMigrationGrantsIssuedReportVersionLedgerToItsRuntimeCallers" --verbosity minimal` — passed, 1 total / 1 succeeded / 0 failed / 0 skipped.
- `pwsh ./scripts/Test-MigrationGrants.ps1` — passed, 72 migration files checked.
- `pwsh ./scripts/Test-AzureDeploymentPlan.ps1 -Mode Local` — passed; Worker Disabled settings rendered `true`.
- `git diff --cached --check` — passed before commit.
- Independent simplification pass by Aristotle — pass; no changes recommended.
- Independent test analysis by Popper — initially found the omitted bootstrap/test expectations; both were corrected and the checks above were rerun successfully.

## Delivery

- Branch: `task/plat-031-worker-case-report-ledger-grant`.
- Commit: `59af1b21fa9a09cffc370d299f5c10363e7a4edb`.
- PR: #38, targeting `dev`, exact head `59af1b21fa9a09cffc370d299f5c10363e7a4edb`.
- GitHub CI: running at report time; merge remains gated on green exact-head CI and independent review.
- Combined PLAT-018 + PLAT-030 + PLAT-031 validation: pending until those exact heads are available together.
- No cloud or deployment write was performed.

## Review and combined validation update — 2026-08-28

- Boyle (`pegasus-desktop-reviewer`) independently reviewed exact PR head `59af1b21fa9a09cffc370d299f5c10363e7a4edb`. The migration, bootstrap matrix, integration expectation, plan coverage, simplification pass, and scope passed review. The review was held **not merge-ready at that time** only because exact-head CI was incomplete and combined PLAT-018/030/031 evidence was pending; no implementation defect was identified.
- Clean detached combined validation of exact PLAT-018 `fca04a917f2f2e3c3abc7eeb93e46c9ca9a00ea5`, PLAT-030 `c599a42b1f964c4e5a1dc13894f28f8300152984`, and PLAT-031 `59af1b21fa9a09cffc370d299f5c10363e7a4edb` passed: locked restore; focused runtime-grant tests 10/10; full ArchitectureTests 121/121; `Test-MigrationGrants.ps1` 73/73; `Test-AzureDeploymentPlan.ps1 -Mode Local`; and `git diff --check`.
- Exact-head GitHub Actions run `33153323761`: unit, browser, infrastructure, documentation, changes, local-development-scripts, and reference-data have passed; SQL shards 2 and 3 remain in progress. Merge remains pending their successful completion.

## Exact-head CI remediation — 2026-08-28

- Run `33153323761` exposed a genuine omission in the ticket's migration census consumer: SQL shard 3 job `98790328153` showed `CommittedMigrationCreatesTheSqlServerSchema` expected migrations ending at `20260827231948_IssuedReportVersionEvidenceLedger`, while the database correctly applied `20260828074800_GrantWorkerCaseReportVersionLedgerInsert`.
- Added that migration name to `tests/Pegasus.IntegrationTests/IntakePersistenceIntegrationTests.cs`. The focused test then passed 1/1; `git diff --check` passed; commit `0ab518e3` was pushed to PR #38.
- The previous exact-head run is superseded. Fresh exact-head CI and Boyle's independent re-review of `0ab518e3` remain required before merge. No cloud or deployment write was performed.

## Dev-base conflict resolution — 2026-08-28

PLAT-030's merge into `dev` caused PR #38 to conflict in the shared bootstrap matrix and committed-migration census. The PLAT-031 task branch merged current `origin/dev` and retained both required entries, producing exact head `c97e8e1db774b8b7d6c38ac2fcc24520d27a1150`, pushed to the configured remote. The branch diff against current `origin/dev` remains limited to the PLAT-031 migration and its two expected-state consumers. Post-resolution build passed with 0 warnings/errors, and `CommittedMigrationCreatesTheSqlServerSchema` passed 1/1. The prior green CI run for `0ab518e3` is superseded; fresh exact-head CI and independent review are required for `c97e8e1d`. No cloud, deployment, credential, corpus, or upstream operation occurred.
