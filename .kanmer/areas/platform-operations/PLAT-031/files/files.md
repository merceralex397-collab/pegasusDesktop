# PLAT-031 files map

## Owning change

- `src/Pegasus.Infrastructure/Persistence/Migrations/20260828074800_GrantWorkerCaseReportVersionLedgerInsert.cs` — add the one missing SQL Server-guarded `INSERT` grant and matching `Down` revoke for `pegasus_worker_runtime_role` on `dbo.CaseReportVersionLedgers`. Do not edit the existing table-creation migration.

## Required validation consumers

- `scripts/Invoke-AzureDatabaseBootstrap.ps1` — extend the existing local/effective permission matrix with the new Worker `INSERT` entry. This is a required expected-state consumer of the grant, not an Azure write and not a change to the grant-census script.
- `tests/Pegasus.IntegrationTests/AzureSqlRuntimeRoleMigrationTests.cs` — update the existing latest-ledger permission expectation to include Worker `INSERT`; no new test infrastructure is planned.
- `tests/Pegasus.ArchitectureTests` — existing PLAT-018 gate consumes the migration grant matrix; no change planned in this ticket.
- `scripts/Test-MigrationGrants.ps1` — existing migration census; explicitly out of scope and must remain unchanged.

## Documentation

- Ticket pipeline documents only: plan, checklist/progress as applicable, post-implementation report, scratch, and proof.
- No repository documentation change is required because PLAT-018 owns the gate documentation and this ticket supplies the missing grant plus its existing expected-state consumers.
