# Post-implementation report — TEST-003

## Delivered

Added `tests/Pegasus.IntegrationTests/MailPersistenceTests.cs` on branch `task/dsk-08-03-integration-shard-persistence`. The single class covers the six retained-mail write routes currently present on `origin/dev` at `69a88bbdc0f9a223f8c7da60e24277b664d4b495`:

- link-case prepare and link-case;
- unlink-case prepare and unlink-case;
- classification correction;
- move to the recommended folder.

Each test owns an isolated `IntakeWebApplicationFactory`/LocalDB database and verifies the persisted domain result, actor, operation/reason, version increment, lease operation where applicable, and mutation/action history. No future route group or test-only endpoint was added.

## Validation

- Release build with `--no-restore`: passed, 0 warnings and 0 errors.
- Exact focused MailPersistenceTests filter: 3 passed, 0 skipped.
- Shard 1: 329 passed of 329 assigned, 3m58s.
- Shard 2: 323 passed, 1 pre-existing skip, 324 assigned, 4m57s.
- Shard 3: 325 passed, 1 pre-existing skip, 326 assigned, 6m35s.
- `-VerifyPartition -ShardCount 3`: passed; all 979 enumerated tests were covered exactly once.
- The two skips are pre-existing and unrelated to this change:
  `CustodyOutboxIntegrationTests.AcceptedCaseRetainsEmbeddedPhotographsBesideTheSource` and `QdosMappingExtractionTests.MappedInstructionEmailExtractsItsDocumentedFieldSet`.
- LocalDB cleanup: the eight exact disposable databases created by the final rerun were verified by exact name and creation time after testhost exit, dropped through the normal test cleanup SQL path, and then absent in a follow-up census. No `Pegasus_Test_*.bak` file was present.

## Scope and simplification

Only `tests/Pegasus.IntegrationTests/MailPersistenceTests.cs` is a product-tree change. No production code, shard runner, CI matrix, documentation, Azure resource, or corpus file changed. The simplification pass is recorded in the plan; no behaviour-preserving finding remained unapplied.

## Review handoff

The branch is ready for independent review. The PR must target `dev`; merge requires the independent review and exact-head CI to be green.
