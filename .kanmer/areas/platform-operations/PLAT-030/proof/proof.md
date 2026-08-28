# Proof — PLAT-030

## Merged revision

- Verified checkout: detached `origin/main` at `28ba13a4fcdb51270b24a48725d53b1de5bcae87`.
- Ticket PR: [#37](https://github.com/merceralex397-collab/pegasusDesktop/pull/37), merged into `dev` at `acc715c2f3779b360897455aaa8397e97e0ee870` on 2026-08-28. That merge commit is an ancestor of the verified `origin/main` revision.
- No cloud write, deployment, credential, corpus, or upstream operation was performed.

## Merged-main verification

All commands below ran in `C:\Users\PC\Documents\GitHub\pegasus-worktrees\verify-plat030-main-20260828b`.

- `dotnet restore ./Pegasus.slnx --locked-mode` — passed.
- `dotnet build ./Pegasus.slnx --configuration Release --no-restore -nr:false -p:UseSharedCompilation=false -p:BuildInParallel=false -p:NodeReuse=false` — passed; 0 warnings, 0 errors.
- `dotnet test ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj --configuration Release --no-build --no-restore -p:UseSharedCompilation=false -p:BuildInParallel=false -p:NodeReuse=false --filter "FullyQualifiedName~RuntimeGrantCompositionTests" --verbosity minimal` — passed; 10/10.
- Full architecture suite with the same Release/no-build/no-restore settings — passed; 121/121.
- `pwsh ./scripts/Test-MigrationGrants.ps1` — passed; 73 migration files checked.
- `pwsh ./scripts/Test-AzureDeploymentPlan.ps1 -Mode Local` — passed; Worker Disabled settings rendered `true`. This is local validation only, not an Azure operation.
- `git diff --check` — passed; verification checkout remained clean.
- Merged-main source inspection confirmed the migration grants exactly Web UPDATE on `dbo.ApprovedSentPollOutcomes`, revokes that same grant in `Down`, and the migration census and bootstrap matrix contain the migration/permission entry.

## Exact-head CI and review

- Exact PR head `c599a42b1f964c4e5a1dc13894f28f8300152984` passed repository-check run [33146340008](https://github.com/merceralex397-collab/pegasusDesktop/actions/runs/33146340008), including the retry that made SQL shard 3 and coverage green.
- Independent reviewer Boyle returned PASS on exact PR head after the migration census correction.
- PR #37 was merged only after that review and green CI.
- Exact merged-main repository-check run [33207170876](https://github.com/merceralex397-collab/pegasusDesktop/actions/runs/33207170876) also completed successfully at `28ba13a4`; all applicable repository checks passed, including SQL shards 1/2/3 and SQL integration coverage. Infrastructure was skipped by design.

## Acceptance result

The merged migration is additive and grant-only. SQL Server `Up` validates the managed Web runtime role and grants only UPDATE on `ApprovedSentPollOutcomes`; `Down` revokes exactly that permission. Non-SQL providers are no-ops. The existing EvaHandoffDownloadOperations grant remains the sole owner of that table's existing Web SELECT/INSERT permissions. No runtime, model, unrelated migration, deployment, or cloud state was changed.
