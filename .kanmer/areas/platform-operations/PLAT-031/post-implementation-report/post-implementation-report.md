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
