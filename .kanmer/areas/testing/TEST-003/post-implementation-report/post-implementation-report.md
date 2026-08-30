# Post-implementation report — TEST-003

## Delivered

Added `tests/Pegasus.IntegrationTests/MailPersistenceTests.cs` on branch `task/dsk-08-03-integration-shard-persistence`; the CI filter fix is also in the same tests project. The single class contains four tests covering the six retained-mail write routes currently present on `origin/dev` at `69a88bbdc0f9a223f8c7da60e24277b664d4b495`:

- link-case prepare and link-case;
- unlink-case prepare and unlink-case;
- classification correction;
- move to the recommended folder;
- two concurrent API callers for link-case with one successful mutation and one deterministic stale-version `409`.

Each test owns an isolated `IntakeWebApplicationFactory`/LocalDB database and verifies the persisted domain result, actor, operation/reason, version increment, lease operation where applicable, and mutation/action history. No future route group or test-only endpoint was added.

## Exact-head validation

The final local evidence below was produced from commit `4547ecce0f5898fd58f717b3cc9576d6dbfcf39c`:

- `dotnet build ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --no-restore`: passed, 0 warnings and 0 errors.
- Exact focused filter for the four association/concurrency/classification/folder tests: 4 passed, 0 skipped.
- Shard 1: 330 passed of 330 assigned, 4m09s.
- Shard 2: 323 passed, 1 pre-existing skip, 324 assigned, 4m30s.
- Shard 3: 324 passed of 324 assigned, 6m40s.
- `pwsh ./scripts/Invoke-TestShard.ps1 -VerifyPartition -ArtifactRoot ./artifacts/test-shards -ShardCount 3`: passed; all 978 filtered tests were covered exactly once.
- The only skip in the fixed-head local filtered artifacts is the pre-existing unrelated test `CustodyOutboxIntegrationTests.AcceptedCaseRetainsEmbeddedPhotographsBesideTheSource`. The two QDOS classes are now excluded by their `Category=Corpus` traits; they are mentioned below only as part of the prior timeout diagnosis.
- LocalDB cleanup: the ten exact disposable databases created by this post-fix run were censused after testhost exit and are absent. Two databases created by the unrelated active `uiimp-005` test process were excluded from cleanup. No `Pegasus_Test_*.bak` file was present.

## Concurrency disposition

A first exploratory two-valid-request race exposed an existing SQL transient/deadlock path, which the current API maps to a generic validation `400` rather than the required domain `409`. This tests-only ticket does not silently count that as a passing contract or change production transaction strategy. The committed test instead sends two API callers concurrently while the loser presents the post-commit version precondition; the observed result is exactly one `200`, one `409`, one persisted mutation, and final receipt/case versions incremented once. This limitation is explicit in the plan.

## Scope and simplification

Product-tree changes are limited to `tests/Pegasus.IntegrationTests/MailPersistenceTests.cs`, the trait-only CI filter fixes in `tests/Pegasus.IntegrationTests/QdosExtractionCoverageTests.cs` and `tests/Pegasus.IntegrationTests/QdosMappingExtractionTests.cs`, and the explicitly planned DSK-08-03 row in `docs/desktop/08-testing/README.md`. No production code, shard runner, CI matrix, Azure resource, or corpus file changed. The dated simplification pass and review-finding dispositions are recorded in the Kanmer plan.

## Independent review

The fresh independent review of exact head `4547ecce0f5898fd58f717b3cc9576d6dbfcf39c` passed after this report reconciliation. It confirmed the scope, acceptance criteria, persistence assertions, simplification pass, local validation, and CI evidence are sufficient for merge.

## CI evidence

PR #56 at the prior exact head `f85e5236a27bfbda91278569ef46743776fc3160` passed every check except `sql-integration (3)`, which timed out at 20m10s. Its completed log showed the corpus-only QDOS classes being discovered and skipped after multi-minute corpus-root scans because they lacked the `Category=Corpus` trait required by the CI filter.

Commit `4547ecce0f5898fd58f717b3cc9576d6dbfcf39c` adds that trait to both classes. PR #56 exact-head CI run [33303988632, attempt 3](https://github.com/merceralex397-collab/pegasusDesktop/actions/runs/33303988632) completed successfully at that SHA: repository checks, unit, browser, all three SQL shards, and SQL-shard coverage passed; infrastructure was correctly skipped. The fixed-head local suite also completed all 978 filtered tests with the single pre-existing skip stated above.
