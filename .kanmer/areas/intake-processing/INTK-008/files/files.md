# Files — INTK-008

## Owned change

| Path | Current owner | Change |
| --- | --- | --- |
| `tests/Pegasus.IntegrationTests/GroupedImageIntakeConcurrencyTests.cs` | Integration test harness | Change only `ProcessWithDeadlockRetryAsync` so the existing bounded retry recognizes SQL Server deadlock 1205 through EF's exception wrapper. Preserve all assertions, iteration counts, transaction behavior, and production code. |

## Explicit non-scope

- No `src/**` production file.
- No EF global retry policy, schema, migration, persistence behavior, or API.
- No new abstraction or compatibility path.
- No upstream, cloud, deployment, credential, or external-environment operation.

## Evidence

The exact-head CI log for run `32959758190`, rerun job `98152798225`, reports `InvalidOperationException → DbUpdateException → SqlException(Number=1205)` from `EfIntakeWorkStore.CompleteProcessingAsync`. The current helper at `GroupedImageIntakeConcurrencyTests.cs:314-333` filters only direct `SqlException`.
