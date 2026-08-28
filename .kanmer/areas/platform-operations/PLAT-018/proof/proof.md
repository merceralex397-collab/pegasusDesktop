# Proof — PLAT-018

## Merged change

- PR #36 merged into `dev` with reviewed head `5061f226cd3d8c795ccbb6c9e440ef9878d014fc`.
- Exact `dev` SHA `6606771c15ed5b71262ae0debb2e1981313bf217` was promoted non-force to `main`.
- Exact-head CI run `33197299822` passed, including the rerun of SQL shard 2.
- Independent re-review of `5061f226` returned PASS.

## Merged-main validation

Executed in detached worktree `verify-plat018-main-20260828` at exact `main` SHA `6606771c15ed5b71262ae0debb2e1981313bf217`:

- `dotnet restore ./Pegasus.slnx --locked-mode` — passed.
- `dotnet build ./Pegasus.slnx --configuration Release --no-restore -nr:false -p:UseSharedCompilation=false -p:BuildInParallel=false -p:NodeReuse=false` — passed, 0 warnings/errors.
- `dotnet test ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj --configuration Release --no-build -nr:false -p:UseSharedCompilation=false -p:BuildInParallel=false -p:NodeReuse=false --verbosity minimal` — passed, 121/121.
- `pwsh ./scripts/Test-MigrationGrants.ps1` — passed, 73 migration files.
- `pwsh ./scripts/Test-AzureDeploymentPlan.ps1 -Mode Local` — passed; Worker Disabled settings rendered `true`.
- `git diff --check` — passed.

## Acceptance evidence

The merged evaluator derives composition-root writes through registration closure and EF model table metadata, compares role-specific verbs against the migration grant shapes, and covers the historical regressions, forward ungranted-table fixture, and reasoned create-file-only opt-outs. The existing migration-grant script and CI path remain unchanged. The implementation diff is test/package/fixture/documentation-only; no cloud write, deployment, credential, corpus, or upstream operation was performed.

This proof establishes repository and merged-main evidence. It does not claim a production permission write or deployment runtime exercise.
