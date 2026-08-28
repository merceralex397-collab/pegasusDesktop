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
