# Post-implementation report — TEST-003

## Delivered

Added `tests/Pegasus.IntegrationTests/MailPersistenceTests.cs` on branch `task/dsk-08-03-integration-shard-persistence`. The single class now contains five tests covering the six retained-mail write routes currently present on `origin/dev` at `69a88bbdc0f9a223f8c7da60e24277b664d4b495`:

- link-case prepare and link-case;
- unlink-case prepare and unlink-case;
- classification correction;
- move to the recommended folder;
- two concurrent API callers for link-case with one successful mutation and one deterministic stale-version `409`.

Each test owns an isolated `IntakeWebApplicationFactory`/LocalDB database and verifies the persisted domain result, actor, operation/reason, version increment, lease operation where applicable, and mutation/action history. No future route group or test-only endpoint was added.

## Validation

- `dotnet build ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --no-restore`: passed, 0 warnings and 0 errors.
- Exact focused filter for all four association/concurrency/classification/folder tests: 4 passed, 0 skipped.
- Final shard 1: 330 passed of 330 assigned, 4m23s.
- Final shard 2: 323 passed, 1 pre-existing skip, 324 assigned, 3m56s.
- Final shard 3: 325 passed, 1 pre-existing skip, 326 assigned, 8m02s.
- `pwsh ./scripts/Invoke-TestShard.ps1 -VerifyPartition -ArtifactRoot ./artifacts/test-shards -ShardCount 3`: passed; all 980 enumerated tests were covered exactly once.
- The two skips are pre-existing and unrelated to this change:
  `CustodyOutboxIntegrationTests.AcceptedCaseRetainsEmbeddedPhotographsBesideTheSource` and `QdosMappingExtractionTests.MappedInstructionEmailExtractsItsDocumentedFieldSet`.
- LocalDB cleanup: the eight exact disposable databases created by the final shard rerun, plus the one exact database left by the exploratory valid-concurrency run, were verified after testhost exit, dropped through the normal ALTER SINGLE_USER/DROP path, and then absent in a follow-up exact-name census. No `Pegasus_Test_*.bak` file was present.

## Concurrency disposition

A first exploratory two-valid-request race exposed an existing SQL transient/deadlock path, which the current API maps to a generic validation `400` rather than the required domain `409`. This tests-only ticket does not silently count that as a passing contract or change production transaction strategy. The committed test instead sends two API callers concurrently while the loser presents the post-commit version precondition; the observed result is exactly one `200`, one `409`, one persisted mutation, and final receipt/case versions incremented once.

## Scope and simplification

Product-tree changes are limited to `tests/Pegasus.IntegrationTests/MailPersistenceTests.cs` and the explicitly planned DSK-08-03 row in `docs/desktop/08-testing/README.md`. No production code, shard runner, CI matrix, Azure resource, or corpus file changed. The dated simplification pass and the first independent-review dispositions are recorded in the Kanmer plan; the reviewer’s action-history, concurrency, checklist, and reproduction findings were addressed before this handoff.

## Review handoff

Current branch commit: `f85e5236`. The branch is ready for a fresh independent review of this exact commit. The PR must target `dev`; merge requires that review and exact-head CI to be green.
