# Post-implementation report

## Change

PR #37 targets `dev` at exact head `c599a42b1f964c4e5a1dc13894f28f8300152984`. The final diff contains exactly three files:

- `src/Pegasus.Infrastructure/Persistence/Migrations/20260828052825_GrantWebApprovedSentPollOutcomeUpdate.cs`
- `scripts/Invoke-AzureDatabaseBootstrap.ps1`
- `tests/Pegasus.IntegrationTests/IntakePersistenceIntegrationTests.cs`

The migration is discoverable by EF as `20260828052825_GrantWebApprovedPollOutcomeUpdate`. SQL Server `Up` checks the managed `pegasus_web_runtime_role` and grants only UPDATE on `dbo.ApprovedSentPollOutcomes`; `Down` performs the matching revoke. Non-SQL providers return before SQL execution. The existing `20260819180000_GrantEvaHandoffDownloadOperations.cs` remains the owner of Web SELECT/INSERT for `EvaHandoffDownloadOperations`; no duplicate was added. The bootstrap permission matrix and committed-migration census contain the new migration's required entries.

## Local validation

- `dotnet build --configuration Release --no-restore -p:UseSharedCompilation=false -p:BuildInParallel=false -p:NodeReuse=false --verbosity minimal` — passed, 0 warnings/errors.
- `dotnet build src/Pegasus.Web/Pegasus.Web.csproj --configuration Release --no-restore -p:UseSharedCompilation=false -p:BuildInParallel=false -p:NodeReuse=false --verbosity minimal` — passed, 0 warnings/errors.
- `dotnet test ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj --configuration Release --no-restore -p:UseSharedCompilation=false -p:BuildInParallel=false -p:NodeReuse=false --verbosity minimal` — passed, 111/111.
- `dotnet ef migrations list --project src/Pegasus.Infrastructure --startup-project src/Pegasus.Web --context PegasusDbContext --configuration Release --no-build` — passed; new migration listed Pending.
- `pwsh ./scripts/Test-MigrationGrants.ps1` — passed, 72 migration files.
- `pwsh ./scripts/Test-AzureDeploymentPlan.ps1 -Mode Local` — passed.
- Targeted `CommittedMigrationCreatesTheSqlServerSchema` integration test — passed, 1/1.
- `git diff --check` — passed.

The PLAT-018 focused composition test is not claimed on this branch: it belongs to PLAT-018's parser-correction branch and currently needs that correction. It remains an explicit dependency/acceptance condition rather than fabricated evidence.

## Review and CI

Independent reviewer Boyle reviewed exact head `c599a42b1f964c4e5a1dc13894f28f8300152984` and returned FAIL. Code scope, SQL guard, managed-role check, exact grant/revoke, EF discoverability, bootstrap matrix, migration census, and simplification passed review. The review remains blocking because the focused PLAT-018 coverage is unchecked and exact-head CI is not green.

Exact GitHub Actions run `33146340008` for `c599a42b`:

- Attempt 1: `sql-integration (3)` completed with 321/323 passed; two unrelated SQL resource failures were recorded: a post-login connection timeout in `AutomaticVehicleLookupTests.SweepSkipsTerminalCasesAndUnusableValues` and a command timeout in `MultiFormatIntakeWebTests.ConfirmingEmailWithDocxExtractedImagesOverTwentyFiveMbFailsClosedAndRetainsAttachment`.
- Attempt 2: the same SQL shard ran for the 20-minute job limit and was cancelled without an assertion result.
- Attempt 3: the same SQL shard again ran for the 20-minute job limit and was cancelled without an assertion result.

The latest exact-head CI state is therefore not green. No merge is claimed.

No cloud, deployment, credential, corpus, or upstream operation occurred.

## Cross-ticket focused validation — 2026-08-28

PLAT-018's parser correction and PLAT-030's grant migration were validated together in a temporary local worktree, without changing either task branch or pushing the temporary merge. Exact PLAT-018 HEAD `2d069f0a6f7ea01564b6fdf3fac7efedbfad1f8b` plus exact PLAT-030 HEAD `c599a42b1f964c4e5a1dc13894f28f8300152984` passed the focused `RuntimeGrantCompositionTests` 8/8 and the full architecture suite 119/119. The same tree passed `Test-MigrationGrants.ps1` (72 files), `Test-AzureDeploymentPlan.ps1 -Mode Local`, and `git diff --check`.

This supplies the previously unchecked PLAT-018 focused acceptance evidence. The temporary merge is validation evidence only; PR #37 remains the three-file PLAT-030 diff at exact head `c599a42b`.
