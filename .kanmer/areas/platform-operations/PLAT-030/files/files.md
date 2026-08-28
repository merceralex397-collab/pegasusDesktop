# Files map

## Owned changes

- `src/Pegasus.Infrastructure/Persistence/Migrations/20260828052825_GrantWebApprovedSentPollOutcomeUpdate.cs` — the additive SQL Server grant-only migration.
- `scripts/Invoke-AzureDatabaseBootstrap.ps1` — the existing release bootstrap permission matrix, extended by one exact Web UPDATE entry so the grant-carrying migration is accounted for.

## Validation-only files

- `tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj` and `RuntimeGrantCompositionTests` are read-only validation owners; PLAT-018 owns its analyzer/parser correction.
- `src/Pegasus.Infrastructure/Persistence/EfTriageStore.cs`, `20260729199000_RuntimeRoleReconciliation.cs`, and `20260819180000_GrantEvaHandoffDownloadOperations.cs` are read-only evidence sources.
- `scripts/Test-AzureDeploymentPlan.ps1` is a read-only validator; its Local mode is run as evidence.

## Explicit non-scope

No runtime source, model snapshot, unrelated migration, CI workflow, deployment execution, cloud state, credential, Azure resource, upstream remote, or corpus file changes. No EvaHandoffDownloadOperations duplicate grant is added because its existing migration already grants Web SELECT and INSERT.

## Exact-head review correction — 2026-08-28

The independent review identified one required owned change: update `tests/Pegasus.IntegrationTests/IntakePersistenceIntegrationTests.cs` so `CommittedMigrationCreatesTheSqlServerSchema` includes `20260828052825_GrantWebApprovedSentPollOutcomeUpdate` in the applied-migration census. This is a direct acceptance/CI consumer of the migration, not unrelated cleanup.
