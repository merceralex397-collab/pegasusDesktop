# Files — INTK-002: upstream:INTK-003 · Recover dispatched intake work whose queue message never arrives

## Change surface

| Path | Purpose / risk |
| --- | --- |
| `docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md` | Authoritative context; update only if the ticket's accepted scope explicitly calls for it. |
| `docs/desktop/05-implementation-and-migration/reuse-map.md` | Authoritative context; update only if the ticket's accepted scope explicitly calls for it. |
| `docs/frd/frd-02-intake-and-source-identity.md` | Authoritative context; update only if the ticket's accepted scope explicitly calls for it. |
| `src/Pegasus.Core/Intake/DurableIntake.cs` | Core policy or contract; reuse the existing business owner rather than placing policy in a host. |
| `src/Pegasus.Infrastructure/Persistence/EfIntakeWorkStore.cs` | Persistence or provider adapter; inspect data access, transaction and failure behaviour before changing callers. |
| `src/Pegasus.Worker/AzureQueueIntakeWorkQueue.cs` | Unattended worker composition; retain central execution and protected credentials. |
| `src/Pegasus.Worker/host.json` | Unattended worker composition; retain central execution and protected credentials. |
| `src/Pegasus.Worker/IntakeFunctions.cs` | Unattended worker composition; retain central execution and protected credentials. |
| `src/Pegasus.Worker/WorkerDependencyInjection.cs` | Unattended worker composition; retain central execution and protected credentials. |
| `tests/Pegasus.IntegrationTests/RecoveryTests.cs` | Focused verification surface; extend the stated success, failure and regression coverage. |
| `docs/operations.md` | Authoritative context; update only if the ticket's accepted scope explicitly calls for it. |
| `docs/engineering.md` | Authoritative context; update only if the ticket's accepted scope explicitly calls for it. |

## Context files

- `docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md` — Authoritative context; update only if the ticket's accepted scope explicitly calls for it.
- `docs/desktop/05-implementation-and-migration/reuse-map.md` — Authoritative context; update only if the ticket's accepted scope explicitly calls for it.
- `docs/frd/frd-02-intake-and-source-identity.md` — Authoritative context; update only if the ticket's accepted scope explicitly calls for it.
- `src/Pegasus.Core/Intake/DurableIntake.cs` — Core policy or contract; reuse the existing business owner rather than placing policy in a host.
- `src/Pegasus.Infrastructure/Persistence/EfIntakeWorkStore.cs` — Persistence or provider adapter; inspect data access, transaction and failure behaviour before changing callers.
- `src/Pegasus.Worker/AzureQueueIntakeWorkQueue.cs` — Unattended worker composition; retain central execution and protected credentials.
- `src/Pegasus.Worker/host.json` — Unattended worker composition; retain central execution and protected credentials.
- `src/Pegasus.Worker/IntakeFunctions.cs` — Unattended worker composition; retain central execution and protected credentials.
- `src/Pegasus.Worker/WorkerDependencyInjection.cs` — Unattended worker composition; retain central execution and protected credentials.
- `tests/Pegasus.IntegrationTests/RecoveryTests.cs` — Focused verification surface; extend the stated success, failure and regression coverage.
- `docs/operations.md` — Authoritative context; update only if the ticket's accepted scope explicitly calls for it.
- `docs/engineering.md` — Authoritative context; update only if the ticket's accepted scope explicitly calls for it.

## Ripple and out-of-scope boundary

- **Azure**: no write. Read-only checks of the intake queue and Application Insights are permitted with no per-target approval (`docs/runbook.md` § Live-operation approval matrix; mirrored in `docs/desktop/11-azure-disposition/README.md`). The local run uses Azurite under **L-02**; asking for an Azure test resource is out of bounds (ADR-0014 stands).
- **Scope boundary**: may touch `src/Pegasus.Core/Intake/DurableIntake.cs`, `src/Pegasus.Infrastructure/Persistence/EfIntakeWorkStore.cs`, `tests/Pegasus.IntegrationTests/RecoveryTests.cs`, `docs/frd/frd-02-intake-and-source-identity.md` and `docs/operations.md`. Must **not** touch `src/Pegasus.Web/Pages/**`, `src/Pegasus.Web/Api/**`, any desktop project, or add a database table.
- **Unblocks / blocked by**: this ticket **blocks** [[DSK-05-09]], [[DSK-05-13]] and [[DSK-05-20]] — each renders a state that is dishonest while a `dispatched` row can strand, and [[DSK-07-01]]'s retry-eligibility field is computed over the same rows. It is **blocked by** [[DSK-01-10]], the first one-way upstream sync. [[DSK-08-17]]'s Test/UAT stack is where the tier-6 evidence is produced.
- **Traps**: **upstream ids and fork board ids do not match.** This ticket is board `INTK-002` and it is upstream INTK-003; upstream INTK-002 is the intake duplication chores, board [[INTK-001]]. The join table is `HZN-001/board-conventions.md` § Upstream ids versus board ids — read it, never compute the mapping, and write `upstream <ID>`, or `upstream <ID> (board [[<board-id>]])` where both are meant. Do not reset `AttemptCount` on recovery — that would make a poisoned item immortal. Do not add a second timer; the reconciliation timer already exists. A new table would need a runtime-role `Grant*` migration checked by `scripts/Test-MigrationGrants.ps1`; this ticket must not add one. `IntakeWorkItems` state strings are persisted values — changing one is a migration, not a rename.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the ticket `plan` document.

## 2026-08-25 verified lifecycle and threshold

Read-only source verification on branch intk-002-recover-dispatched-work (origin/dev base):

- src/Pegasus.Infrastructure/Persistence/EfIntakeWorkStore.cs:197-227: ClaimDispatchAsync selects only persisted pending or retry_scheduled rows whose DueAtUtc <= nowUtc, then claims them as dispatching.
- src/Pegasus.Infrastructure/Persistence/EfIntakeWorkStore.cs:229-240: MarkDispatchedAsync transitions the owned dispatch lease to dispatched, sets DueAtUtc=nowUtc, clears lease fields, and clears failure.
- src/Pegasus.Infrastructure/Persistence/EfIntakeWorkStore.cs:266-301: ClaimProcessingAsync accepts dispatching|dispatched|processing; completed/failed rows and an unexpired processing lease return null. This makes a late duplicate after a recovered row safe once the first processing attempt has claimed it.
- src/Pegasus.Infrastructure/Persistence/EfIntakeWorkStore.cs:416-470 and :543-600: existing recovery selected only leased dispatching|processing; the implementation now carries a one-hour cutoff for unleased dispatched rows and preserves the existing state/attempt/lease/due optimistic update predicate.
- src/Pegasus.Core/Intake/DurableIntake.cs:935-950: ReconcileStagedArtifacts.ExecuteAsync is the existing timer-owned reconciliation path; no second timer is introduced.
- src/Pegasus.Worker/IntakeFunctions.cs:68-83 and WorkerDependencyInjection.cs:100: the existing staged-artifact reconciliation timer and composition root own this path.
- src/Pegasus.Worker/host.json:12-19: queue visibility timeout is 5 minutes, max polling interval 2 seconds, max dequeue count 5.
- src/Pegasus.Worker/AzureQueueIntakeWorkQueue.cs:19-24: SendMessageAsync omits timeToLive.
- Microsoft Learn primary evidence: https://learn.microsoft.com/azure/storage/queues/storage-quickstart-queues-dotnet#object-model states that an omitted Azure Queue message TTL defaults to seven days; https://learn.microsoft.com/dotnet/api/azure.storage.queues.queueclient.sendmessageasync?view=azure-dotnet confirms the overload and optional TTL parameter.
- Threshold decision: one hour since DueAtUtc. It is above the five-minute visibility timeout, below the seven-day omitted message TTL, and is the upstream example retained as a bounded fork decision. It is passed by the existing Core reconciliation service to the store; no persisted state vocabulary or new table is added.
