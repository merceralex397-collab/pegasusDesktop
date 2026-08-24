# Plan — TEST-003 Integration shard persistence paths

## Governing documents

Use the existing integration-test and local-stack conventions; this fix remains docs_todo until canonical desktop ownership is linked.

## Steps

1. Inventory current api-v1 persistence coverage and map each missing case to one existing shard.
2. Extend the shard using current LocalDB fixtures and shared endpoint contracts.
3. Run each shard plus VerifyPartition with the detected runner syntax.
4. Confirm no filter returns a false green zero-test result and record exact output.

## Verification

- [ ] All affected shards pass.
- [ ] VerifyPartition passes and each test is assigned once.
- [ ] No live Azure dependency is introduced.
