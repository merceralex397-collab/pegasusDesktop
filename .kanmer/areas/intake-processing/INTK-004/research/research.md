# Research — INTK-004: upstream:INTK-027 · Make policy re-evaluation work after transient staging cleanup

## Question

Why can re-evaluation queue work whose temporary source has already been deleted, and which in-repository change satisfies the accepted fail-closed behavior without changing the desktop/API tickets that consume this command?

## Verified facts

1. `EfIntakeMutationStore.ScheduleReevaluationAsync` loads the latest `IntakeEvaluations.StagedReceiptId`, loads the matching `IntakeWorkItems` row, checks only the active processing lease, and then sets `State = "pending"` before clearing failure fields. It does not inspect the staged source or retained source before queueing. Source: `src/Pegasus.Infrastructure/Persistence/EfIntakeMutationStore.cs:223-263`.
2. Completed processing deliberately deletes the staged object through `DeleteCompletedStagedAsync`; the Worker later reads the staged key and maps a missing object to `IntakeArtifactIntegrityException`, which is terminal on its first processing attempt. Sources: `src/Pegasus.Core/Intake/DurableIntake.cs:527-570, 800-852`.
3. The processed receipt retains exactly one durable source asset with kind/disposition `source`, its own storage key, content hash and length. `DownloadIntakeSource` reads that asset through `IIntakeArtifactStore.ReadAsync` and validates the bytes against both the asset and receipt hashes. Sources: `src/Pegasus.Core/Intake/DownloadIntakeSource.cs:10-55`, `src/Pegasus.Core/Intake/IntakeContracts.cs:316-324, 366-402`.
4. `IIntakeArtifactStore.StageAsync` already writes the canonical `staging/{stagedReceiptId}/{hash}` key and verifies immutable content on replay for both Azure Blob and local filesystem implementations. Sources: `src/Pegasus.Core/Intake/IntakeContracts.cs:638-686`, `src/Pegasus.Infrastructure/Intake/AzureBlobIntakeArtifactStore.cs:167-197, 400-437`, `src/Pegasus.Infrastructure/Intake/FileSystemIntakeArtifactStore.cs:180-227, 400-465`.
5. The existing persisted mutation wrapper starts a serializable transaction, invokes the mutation callback, increments the receipt version, writes mutation history, saves, and commits only after the callback succeeds. Therefore a missing or corrupt retained source can fail before `State = "pending"`, leaving receipt state/version/history unchanged. Source: `src/Pegasus.Infrastructure/Persistence/EfIntakeMutationStore.cs:649-785`.
6. The desktop conversion publishes re-evaluation as a named received-item command but the gateway/API and desktop slices are not owned by this ticket. Sources: `docs/desktop/03-gateway-api-and-data/endpoint-map.md:85-90`, `docs/desktop/05-implementation-and-migration/vertical-slices.md:333-369`, `docs/desktop/06-ui-design/screen-specs.md:273-285`.

## Decision for planning

Choose direction (a): re-stage the retained, hash-verified source before changing the work item to pending. This preserves the existing re-evaluate behavior for valid completed receipts and avoids turning a supported operator command into a blanket refusal. The re-stage uses the existing artifact-store port and canonical key; it does not alter completion cleanup or add a second storage implementation.

The mutation must:

- select exactly one retained source asset and validate its hash/length against the receipt;
- read the retained bytes through the existing artifact store;
- call `StageAsync(stagedReceiptId, receipt.SourceHash, content, occurredAtUtc, token)` before `workItem.State = "pending"`;
- allow integrity/dependency exceptions to abort the existing transaction;
- leave the existing lease guard, operation-key replay, and terminal corruption handling intact.

The external blob/file write is deliberately limited to the local/repository implementation path. No production re-staging or cloud write is performed by this ticket.

## Scope and ripple

The implementation owns the Core contract usage and Infrastructure persistence adapter, plus focused Core/Infrastructure/integration tests and the named FRD/carry-over documentation. It does not own `src/Pegasus.Web/Api/**`, the desktop project, or the existing Razor page. GWY-010 and FEAT-009 consume the same Core outcome later without duplicating the source rule.

## Validation targets

Positive: a completed receipt with its durable source and deleted staging is re-staged and queued, with the receipt moving to the existing pending/re-evaluation state.

Contradictory: a receipt with more than one or no retained source asset, a hash/length mismatch, or a missing/corrupt retained object fails closed before queue state or mutation history changes.

Regression: a currently leased work item remains refused; replayed operation keys remain idempotent; genuinely corrupt staged content still maps to the existing terminal integrity failure.
