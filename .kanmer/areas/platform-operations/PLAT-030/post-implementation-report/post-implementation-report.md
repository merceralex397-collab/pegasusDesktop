# Post-implementation report

## Change

PR #37 targets `dev` at exact head `76f5558b9b8c2e2c47d52617f7c08cfe19dd7679`. It adds only `src/Pegasus.Infrastructure/Persistence/Migrations/20260828052825_GrantWebApprovedSentPollOutcomeUpdate.cs`.

The migration is discoverable by EF as `20260828052825_GrantWebApprovedSentPollOutcomeUpdate`. SQL Server `Up` checks the managed `pegasus_web_runtime_role` and grants only UPDATE on `dbo.ApprovedSentPollOutcomes`; `Down` performs the matching revoke. Non-SQL providers return before SQL execution. The existing `20260819180000_GrantEvaHandoffDownloadOperations.cs` remains the owner of Web SELECT/INSERT for EvaHandoffDownloadOperations; no duplicate was added.

## Local validation

- `dotnet build --configuration Release --no-restore -p:UseSharedCompilation=false -p:BuildInParallel=false -p:NodeReuse=false --verbosity minimal` — passed, 0 warnings/errors.
- `dotnet test ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj --configuration Release --no-restore -p:UseSharedCompilation=false -p:BuildInParallel=false -p:NodeReuse=false --verbosity minimal` — passed, 111/111.
- `dotnet ef migrations list --project src/Pegasus.Infrastructure --startup-project src/Pegasus.Web --context PegasusDbContext --configuration Release --no-build` — passed; new migration listed Pending.
- `pwsh ./scripts/Test-MigrationGrants.ps1` — passed, 72 migration files.
- `git diff --check` — passed.

## Review and CI

Independent review of exact head and GitHub Actions `changes` and `documentation` checks are pending. `local-development-scripts` and `reference-data` checks have passed. No merge is claimed until the independent review passes and all applicable exact-head checks are green.

No cloud, deployment, credential, corpus, or upstream operation occurred.

## CI correction in progress — 2026-08-28

The first PR head failed the repository `changes` job because the new grant-carrying migration was not represented in `scripts/Invoke-AzureDatabaseBootstrap.ps1`. The exact expected matrix entry has now been added locally; `Test-AzureDeploymentPlan.ps1 -Mode Local` passes. The first `sql-integration-coverage` failure was consequential because `changes` failed before its shard matrix ran, leaving no `listed-1.txt`; it will be re-evaluated on the corrected head.

The corrected final PR head and its CI/review state will replace the provisional values above before any merge.

## Exact-head review correction — 2026-08-28

Independent reviewer Boyle returned FAIL on exact PR head `e87e30aa819b0ac7753ae8e95b5a5cc97b7a474f`: the committed migration census in `tests/Pegasus.IntegrationTests/IntakePersistenceIntegrationTests.cs` omitted `20260828052825_GrantWebApprovedSentPollOutcomeUpdate`. Exact-head run `33145461491` failed `sql-integration (3)` in `CommittedMigrationCreatesTheSqlServerSchema` (322 passed, 1 failed). The expected migration name has now been added to the census in the PLAT-030 worktree. Review remains failed pending local confirmation and a new exact-head CI run. The reviewer confirmed the SQL guard, managed-role check, exact grant/revoke, EF discoverability, bootstrap matrix entry, and scope/simplification were otherwise correct.
