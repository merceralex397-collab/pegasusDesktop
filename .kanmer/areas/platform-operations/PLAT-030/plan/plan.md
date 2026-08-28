# Plan

## Decision and evidence

PLAT-018's focused gate identifies one genuine missing permission: Web UPDATE on `dbo.ApprovedSentPollOutcomes`, written by `EfTriageStore.LinkResponseEvidenceAsync`. Read-only inspection also found that `20260819180000_GrantEvaHandoffDownloadOperations.cs` already grants Web SELECT and INSERT on `dbo.EvaHandoffDownloadOperations`; that apparent second gap is a comment-sensitive parser defect owned by PLAT-018, not a second migration requirement.

Use one additive grant-only EF migration, matching the existing runtime-role migration convention. Its SQL Server `Up` path will verify `pegasus_web_runtime_role` is the managed database role, then grant only UPDATE on `ApprovedSentPollOutcomes`. Non-SQL providers return without SQL. `Down` will verify the same condition and revoke only that permission. No model change or unrelated privilege is introduced.

## Steps

1. Generate the migration metadata from the current `origin/dev`-based branch using the repository EF migration tooling; keep the model snapshot unchanged because this ticket changes permissions only.
2. Implement the grant-only `Up` and exact `Down` SQL with the existing SQL Server provider guard and managed-role check.
3. Inspect the diff to ensure the only product change is the new migration (and its required metadata), with no duplicate EvaHandoff grant.
4. Run Release build, the focused PLAT-018 architecture test once the PLAT-018 branch is available locally for validation, `pwsh ./scripts/Test-MigrationGrants.ps1`, and `git diff --check`.
5. Run a simplification pass over the branch diff. Record only behaviour-preserving dispositions here.
6. Commit and push this branch to the configured `origin` remote, open a PR to `dev`, and obtain independent review before merge.

## Acceptance checklist

- [ ] Web UPDATE on `dbo.ApprovedSentPollOutcomes` is granted in SQL Server `Up`.
- [ ] The managed Web runtime role is checked; non-SQL providers are a no-op.
- [ ] `Down` revokes exactly Web UPDATE and is a non-SQL no-op.
- [ ] No EvaHandoffDownloadOperations duplicate grant or unrelated file is added.
- [ ] PLAT-018 focused coverage passes after its parser correction.
- [ ] Release build, migration-grant script, and diff check pass.
- [ ] Independent review passes on the exact PR head.
- [ ] No cloud, deployment, credential, corpus, or upstream operation occurs.

## Verification commands

- `dotnet build tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj --configuration Release --no-restore -p:UseSharedCompilation=false -p:BuildInParallel=false -p:NodeReuse=false --verbosity minimal`
- `dotnet test tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj --configuration Release --no-restore -p:UseSharedCompilation=false -p:BuildInParallel=false -p:NodeReuse=false --filter FullyQualifiedName~RuntimeGrantCompositionTests --verbosity minimal` (requires the PLAT-018 analyzer/parser correction; run in that branch or after its merge)
- `pwsh ./scripts/Test-MigrationGrants.ps1`
- `git diff --check`

## Simplification pass

_To be completed over the final branch diff before review._

## Implementation and simplification pass — 2026-08-28

Implemented on `task/plat-030-runtime-permissions` with the single owned migration `20260828052825_GrantWebApprovedSentPollOutcomeUpdate.cs`. EF initially generated a designer and a model-snapshot line reorder; the simplification pass removed both unnecessary artifacts. The final migration follows the existing concise grant-only convention used by `20260801220500_GrantWebMigrationHistoryRead.cs`: explicit `DbContext` and `Migration` attributes, provider guard, managed-role check, one UPDATE grant in `Up`, and one matching REVOKE in `Down`.

- Reuse: retained the existing SQL Server provider string, runtime-role validation shape, and grant-only migration convention.
- Simplification: removed the 7,568-line generated designer and the unchanged model-snapshot churn; no model schema changed.
- Efficiency: one migration SQL grant and one role check; no new dependency, service, CI job, or runtime path.
- Altitude: final branch scope is one 61-line migration; no EvaHandoff duplicate grant, runtime source, script, CI, cloud, deployment, credential, corpus, or upstream file changed.

Validation on the final working tree:

- `dotnet build --configuration Release --no-restore -p:UseSharedCompilation=false -p:BuildInParallel=false -p:NodeReuse=false --verbosity minimal` — passed, 0 warnings/errors.
- `dotnet build src/Pegasus.Web/Pegasus.Web.csproj --configuration Release --no-restore -p:UseSharedCompilation=false -p:BuildInParallel=false -p:NodeReuse=false --verbosity minimal` — passed, 0 warnings/errors.
- `dotnet ef migrations list --project src/Pegasus.Infrastructure --startup-project src/Pegasus.Web --context PegasusDbContext --configuration Release --no-build` — passed; `20260828052825_GrantWebApprovedSentPollOutcomeUpdate` is listed Pending.
- `dotnet test ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj --configuration Release --no-restore -p:UseSharedCompilation=false -p:BuildInParallel=false -p:NodeReuse=false --verbosity minimal` — passed, 111/111.
- `pwsh ./scripts/Test-MigrationGrants.ps1` — passed, 72 migration files.
- `git diff --check` — passed.

The PLAT-018 focused composition test is intentionally not claimed on this branch: it belongs to PLAT-018's test-only branch and currently needs its parser correction. It remains an explicit downstream acceptance condition for PLAT-018, not evidence to fabricate for PLAT-030.

## CI-required matrix correction — 2026-08-28

The first PR run showed that the repository's `Test-AzureDeploymentPlan.ps1 -Mode Local` treats every post-reconciliation grant-carrying migration as requiring a matching expected permission in `scripts/Invoke-AzureDatabaseBootstrap.ps1`. This is the existing bootstrap script's direct matrix consumer, not a cloud write or unrelated deployment change. The plan and files map are corrected to include exactly one `pegasus_web_runtime_role|G|UPDATE|ApprovedSentPollOutcomes` entry.

After this correction, local `pwsh ./scripts/Test-AzureDeploymentPlan.ps1 -Mode Local` passed. The first PR's `sql-integration-coverage` failure was consequential: the `changes` job failed before the SQL shard matrix ran, so `listed-1.txt` was absent. It is not treated as a passing check; CI must be rerun on the corrected exact head.

## Exact-head review correction — 2026-08-28

Independent reviewer Boyle reviewed PR #37 at exact head `e87e30aa819b0ac7753ae8e95b5a5cc97b7a474f` and returned FAIL with one concrete issue: `tests/Pegasus.IntegrationTests/IntakePersistenceIntegrationTests.cs:28-101` pins the applied migration list and omitted `20260828052825_GrantWebApprovedSentPollOutcomeUpdate`. Exact-head CI run `33145461491`, `sql-integration (3)`, failed `CommittedMigrationCreatesTheSqlServerSchema` (322 passed, 1 failed). The migration entry was added after `20260827231948_IssuedReportVersionEvidenceLedger`; no production code or unrelated migration was changed. Rerun the affected local integration test and exact-head CI before merge. The review also notes that the previously attempted focused `RuntimeGrantCompositionTests` filter matched no tests on this branch; it is not counted as passing evidence, and PLAT-018 owns that downstream focused gate.

## Review correction implementation — 2026-08-28

Added the missing `20260828052825_GrantWebApprovedSentPollOutcomeUpdate` entry to the applied-migration census in `tests/Pegasus.IntegrationTests/IntakePersistenceIntegrationTests.cs`, as required by Boyle's independent review. Targeted local validation passed:

- `dotnet test tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --no-restore -p:UseSharedCompilation=false -p:BuildInParallel=false -p:NodeReuse=false --filter "FullyQualifiedName~IntakePersistenceIntegrationTests.CommittedMigrationCreatesTheSqlServerSchema" --verbosity minimal` — passed, 1/1.
- `git diff --check` — passed.

Committed and pushed as `c599a42b` to the configured `origin`; PR #37 now requires a fresh exact-head review and CI run.
