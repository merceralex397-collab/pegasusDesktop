# Files — TEST-003 Integration shard persistence paths

| File or area | Intended change | Risk / reuse |
| --- | --- | --- |
| tests/Pegasus.IntegrationTests | Extend the existing api-v1 persistence shards. | Each new test belongs to exactly one shard. |
| shard runner and VerifyPartition support | Preserve current partition verification. | dotnet test can pass when a filter matches nothing. |
| existing LocalDB fixtures | Reuse real persistence fixtures. | Windows-only; no Azure test environment. |

## Context

Read the current shard topology, test runner configuration and docs/desktop/08-testing test-stack rules. Do not scaffold a second integration harness.
