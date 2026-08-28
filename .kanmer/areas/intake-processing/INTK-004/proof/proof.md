# Proof — INTK-004

## Merged revision

- Verified checkout: detached `origin/main` at `28ba13a4fcdb51270b24a48725d53b1de5bcae87`.
- Ticket PR: [#11](https://github.com/merceralex397-collab/pegasusDesktop/pull/11), merged into `dev` at `7656a65fff3e17d0c4bdada91acf72d5dc78b0b1` on 2026-08-25. The merge commit is an ancestor of the verified `origin/main` revision.
- No upstream synchronization, cloud write, deployment, mailbox mutation, Box mutation, or credential change was performed.

## Verification commands

All commands below ran in `C:\Users\PC\Documents\GitHub\pegasus-worktrees\verify-intk004-main-20260828` on the merged `origin/main` revision.

- `dotnet restore ./Pegasus.slnx --locked-mode` — passed.
- `dotnet build ./Pegasus.slnx --configuration Release --no-restore -nr:false -p:UseSharedCompilation=false -p:BuildInParallel=false -p:NodeReuse=false` — passed; 0 warnings, 0 errors.
- `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --no-build --no-restore --filter "FullyQualifiedName~IntakeReevaluationPersistenceTests"` — passed; 7/7 tests.
  - Covers retained-source re-staging and replay, missing/corrupt source atomic refusal, dispatching and processing lease refusal, ambiguous source, and staging failure.
  - The test exercised the local WebApplicationFactory/LocalDB path and confirmed no receipt/work/history mutation on refusal.
- `dotnet test ./tests/Pegasus.Core.Tests/Pegasus.Core.Tests.csproj --configuration Release --no-build --no-restore` — passed; 939/939 tests.
- `git diff --check` — passed; merged verification checkout remained clean.

## Exact-head CI

Repository-check run [33207170876](https://github.com/merceralex397-collab/pegasusDesktop/actions/runs/33207170876) completed successfully for exact head `28ba13a4fcdb51270b24a48725d53b1de5bcae87`. Reference-data, changes, documentation, local-development-scripts, unit, browser, SQL integration shards 1/2/3, and SQL integration coverage all passed. Infrastructure was skipped by the repository's designed condition.

A prior local broad integration attempt is recorded in the post-implementation report: it was stopped after an unrelated existing grouped-image concurrency SQL deadlock. It is not claimed as passing evidence; the exact-head CI SQL shards above are the merged-main repository validation.

## Acceptance result

The merged implementation re-stages the single retained, hash-verified source through the existing artifact-store port before assigning the work item to `pending`. Missing, corrupt, ambiguous, leased, or failed-staging cases fail closed before the receipt version, work item, or mutation history changes. This satisfies the ticket's acceptance conditions without changing the API or desktop consumer owned by GWY-010/FEAT-009.
