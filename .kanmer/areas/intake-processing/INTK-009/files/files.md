# INTK-009 files map

## Evidence and governing owner

- The live blocker is the exact-head GitHub Actions run for PR #14, job `98652140460`, at head `bb263b20a49af1375d2823ce5c4a803dd66bdc39`. The SQL integration shard failed in `GroupedImageIntakeConcurrencyTests.ConcurrentGroupMembersNeverSplitAcrossRepeatedRuns` with SQL Server deadlock 1205 after the test's five-attempt helper retry.
- The governing behaviour is [FRD-02](../../../../docs/frd/frd-02-intake-and-source-identity.md): each dispatch is durable and idempotent; duplicate delivery must not duplicate evaluation or downstream side effects; grouped image routing is one group decision and must not split members.
- The current implementation owner is `EfIntakeWorkStore.CompleteProcessingAsync` in `src/Pegasus.Infrastructure/Persistence/EfIntakeWorkStore.cs`. It completes one leased work item and allocates the next per-staged-receipt evaluation revision inside a Serializable transaction.
- The caller is `ProcessQueuedIntake.ExecuteAsync` in `src/Pegasus.Core/Intake/DurableIntake.cs`, which completes the durable evaluation before post-completion association/allocation/image automation.
- The reproducer is `tests/Pegasus.IntegrationTests/GroupedImageIntakeConcurrencyTests.cs`; it runs two independent group-member deliveries concurrently and asserts both complete and converge on exactly one Image Intake registration.

## Owned change set

Implementation is limited to the completion transaction and its focused integration coverage. The change must preserve lease ownership, exactly-once durable completion/evaluation semantics, per-receipt revision uniqueness, and the post-completion grouped-image convergence contract. It must not weaken the race assertion, add a blanket retry policy, modify PR #14, alter upstream remotes, or perform cloud/deployment writes.

Expected files, subject to the verified repair:

- `src/Pegasus.Infrastructure/Persistence/EfIntakeWorkStore.cs` — completion transaction/concurrency repair.
- `tests/Pegasus.IntegrationTests/GroupedImageIntakeConcurrencyTests.cs` — only if focused coverage must assert the repaired completion path.
- `tests/Pegasus.IntegrationTests/IntakePersistenceIntegrationTests.cs` — only if a store-level completion invariant needs direct coverage.
- A migration is not expected; add one only if read-only investigation proves an existing schema/index boundary is insufficient and the plan is updated first.

Known non-owners:

- `src/Pegasus.Core/Intake/DurableIntake.cs` already handles a completed evaluation as a safe replay and defers pending grouped automation; it is not changed unless the completion contract cannot be repaired at the persistence owner.
- PR #14 remains an independent documentation PR and is not edited or merged by this ticket.
