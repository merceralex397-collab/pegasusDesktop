# Proof — PLAT-031

## Merged revision

- Verified checkout: detached `origin/main` at `28ba13a4fcdb51270b24a48725d53b1de5bcae87`.
- Ticket PR: [#38](https://github.com/merceralex397-collab/pegasusDesktop/pull/38), merged into `dev` at `6d5ee8e4fb14b711fe8f00f2936bf1ce4fc2dc52` on 2026-08-28. The merge commit is an ancestor of the verified `origin/main` revision.
- No cloud write, deployment, credential, corpus, or upstream operation was performed.

## Merged-main verification

All commands below ran in `C:\Users\PC\Documents\GitHub\pegasus-worktrees\verify-plat031-main-20260828`.

- `dotnet restore ./Pegasus.slnx --locked-mode` — passed.
- `dotnet build ./Pegasus.slnx --configuration Release --no-restore -nr:false -p:UseSharedCompilation=false -p:BuildInParallel=false -p:NodeReuse=false` — passed; 0 warnings, 0 errors.
- `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --no-build --no-restore -nr:false -p:UseSharedCompilation=false -p:BuildInParallel=false -p:NodeReuse=false --filter "FullyQualifiedName~AzureSqlRuntimeRoleMigrationTests.LatestMigrationGrantsIssuedReportVersionLedgerToItsRuntimeCallers" --verbosity minimal` — passed; 1/1.
- `dotnet test ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj --configuration Release --no-build --no-restore -p:UseSharedCompilation=false -p:BuildInParallel=false -p:NodeReuse=false --verbosity minimal` — passed; 121/121.
- `pwsh ./scripts/Test-MigrationGrants.ps1` — passed; 73 migration files checked.
- `pwsh ./scripts/Test-AzureDeploymentPlan.ps1 -Mode Local` — passed; Worker Disabled settings rendered `true`. This is local validation only, not an Azure operation.
- `git diff --check` — passed; verification checkout remained clean.
- Merged-main source inspection confirmed `Up` grants only Worker `INSERT` on `dbo.CaseReportVersionLedgers`, `Down` revokes that exact grant, the bootstrap matrix contains the expected Worker entry, and the existing runtime-role expectation contains the Worker INSERT permission.

## Review and CI

- Independent reviewer Ramanujan reviewed exact PR head `c97e8e1db774b8b7d6c38ac2fcc24520d27a1150` and returned PASS.
- Exact-head CI run [33195926358](https://github.com/merceralex397-collab/pegasusDesktop/actions/runs/33195926358) completed successfully, including all SQL shards and coverage.
- PR #38 merged only after the independent PASS and green exact-head CI.
- Exact merged-main repository-check run [33207170876](https://github.com/merceralex397-collab/pegasusDesktop/actions/runs/33207170876) also completed successfully at `28ba13a4`; all applicable repository checks passed, including SQL shards 1/2/3 and SQL integration coverage. Infrastructure was skipped by design.

## Acceptance result

The merged change is limited to the missing Worker runtime-role permission and its two existing expected-state consumers. It is SQL Server guarded, validates the managed Worker role, grants/revokes exactly `INSERT`, preserves non-SQL no-op behavior, and changes no production code or cloud state.
