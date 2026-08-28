# PLAT-031 files map

## Owning change

- `src/Pegasus.Infrastructure/Persistence/Migrations/<new timestamp>_GrantWorkerCaseReportVersionLedgerInsert.cs` — add the one missing SQL Server-guarded `INSERT` grant and matching `Down` revoke for `pegasus_worker_runtime_role` on `dbo.CaseReportVersionLedgers`. Do not edit the existing table-creation migration.

## Validation consumers

- `tests/Pegasus.ArchitectureTests` — existing PLAT-018 gate consumes the migration grant matrix; no test change is planned unless the current migration discovery requires it.
- `scripts/Test-MigrationGrants.ps1` — existing migration census; explicitly out of scope and must remain unchanged.
- `tests/Pegasus.IntegrationTests` — no change planned; the defect is permission-matrix evidence, not a new integration scenario.

## Documentation

- Ticket pipeline documents only: plan, checklist/progress as applicable, post-implementation report, scratch, and proof.
- No repository documentation change is required because PLAT-018 owns the gate documentation and this ticket supplies the missing grant.
