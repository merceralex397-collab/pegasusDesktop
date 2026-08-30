# Verification proof

## Merged revision

- PR #56 is merged into `dev`; merge commit: `8ccbf8dab15d01bed8e58bf509a4a1c27851bdc2`.
- The exact configured `origin` refs `main` and `dev` both resolve to `8ccbf8dab15d01bed8e58bf509a4a1c27851bdc2`.
- The ticket worktree was detached at that merged `main` commit and was clean for verification.
- PR #56 had independent review PASS and exact-head CI run `33303988632` passed at implementation SHA `4547ecce0f5898fd58f717b3cc9576d6dbfcf39c`; repository, unit, browser, all three SQL shards, and coverage checks passed. Infrastructure was correctly skipped because this ticket has no infrastructure change.

## Merged-main validation

Commands run from the TEST-003 worktree at `8ccbf8d`:

- `dotnet build ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --no-restore` — passed; 0 warnings, 0 errors.
- Focused TEST-003 persistence filter — passed; 4 passed, 0 failed, 0 skipped.
- `pwsh ./scripts/Invoke-TestShard.ps1 -Project ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj -Filter 'Category!=Corpus&Category!=Browser' -Shard 1 -ShardCount 3 -ArtifactRoot ./artifacts/test-shards-proof-8ccbf8d` — 330/330 passed.
- Same command with `-Shard 2` — 323 passed, 1 skipped, 0 failed, all 324 assigned tests ran. The skipped test is the pre-existing `CustodyOutboxIntegrationTests.AcceptedCaseRetainsEmbeddedPhotographsBesideTheSource`.
- Same command with `-Shard 3` — 324/324 passed.
- `pwsh ./scripts/Invoke-TestShard.ps1 -VerifyPartition -ArtifactRoot ./artifacts/test-shards-proof-8ccbf8d -ShardCount 3` — passed; all 978 enumerated tests were covered exactly once.
- The shard durations were 5m35s, 5m29s, and 7m57s, below the 20-minute CI timeout and the ticket's caution threshold.

## Database cleanup evidence

- Read-only SQL census after the run found no row for the actual TRX database `Pegasus_Test_f6eac510f9614eb29cb0c937df44472c`.
- No `Pegasus_Test_*.bak` files were found in the LocalDB data directory.
- Older attached `Pegasus_Test_*` databases were observed but not modified because their ownership could not be attributed to this run.

## Acceptance and scope

The merged tree contains the planned four persistence tests and the trait-only shard-filter correction for the corpus-only QDOS classes. The tests assert persisted rows, actor history, version increments, outbox/work-item effects, and the documented concurrency conflict. No production code, corpus data, Azure resource, deployment, or upstream remote was changed.

## Traceability

- PR URL: https://github.com/merceralex397-collab/pegasusDesktop/pull/56
- Merged at: 2026-08-30T10:17:15Z
