# Plan — INTK-001: upstream:INTK-002 · Intake duplication chores: adapter-wide fault naming, one decision-code table, Web-composition assertion, leftover port

## Governing documents

- No canonical document is linked yet. Retain the ticket's existing `docs_todo` state; do not invent or link a proposed desktop ADR.

## Chosen approach

Land the four upstream INTK-002 duplication chores server-side on the fork: adapters name their faults so the intake retry taxonomy matches intake types only; the persisted intake decision-code table exists once instead of three times; an architecture fact asserts that `Pegasus.Web` composes no queue client and cannot resolve `ProcessQueuedIntake`; and the leftover `IIntakeSubmission` port is deleted or its second concrete need is recorded.

## Routing and constraints

- Future owner: Core/Infrastructure intake work with focused Core and integration tests. The local Test/UAT stack is the verification environment; no production intake or Azure write is authorized.


## Ordered implementation steps

1. Orient. Read the verbatim upstream body above, `coverage-decision.md` § Import list row for upstream `INTK-002`, and `docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md:151`. Call `get_doc_gates <this ticket id>`, then `take_ticket` with branch `task/upstream-intk-002-intake-duplication` and worktree `../pegasus-worktrees/upstream-intk-002-intake-duplication` from `origin/dev`.
2. **Re-expressed for this tree, not the upstream one.** The upstream body names `ProcessQueuedIntake.IsTransientProcessingFailure`. On the fork that predicate is `IntakeExceptionPolicy.IsTransientFailure` at `src/Pegasus.Core/Intake/IntakeContracts.cs:593-601`, consulted exactly once at `src/Pegasus.Core/Intake/DurableIntake.cs:573`. Record the current name, file and line in the ticket `plan` before changing anything, and use them throughout. Do the same for any other renamed symbol you meet.
3. Sub-item (a), fault naming. Give `FileSystemIntakeArtifactStore` (`src/Pegasus.Infrastructure/Intake/FileSystemIntakeArtifactStore.cs`) the same fault translation `AzureBlobIntakeArtifactStore.DependencyUnavailable` (`:451`) performs, on **every** path rather than read/upload only, and replace the bare `InvalidOperationException` at `src/Pegasus.Infrastructure/Persistence/EfIntakeReceiptStore.cs:41` with a named concurrency-conflict exception from `src/Pegasus.Core/Intake/IntakeContracts.cs`. Done when `dotnet build --configuration Release` is clean and `tests/Pegasus.ArchitectureTests/AzureBlobIntakeArtifactStoreTests.cs` plus `tests/Pegasus.IntegrationTests/RecoveryTests.cs` are still green.
4. Remove `IOException`, `TimeoutException` and `DbException` from `IntakeExceptionPolicy.IsTransientFailure` once step 3 covers every adapter on the processor's path. If one BCL type must remain, do **not** delete it — record in the `plan` which adapter still leaks it and why, exactly as the upstream verification line allows.
5. Sub-item (b), one decision-code table. Make `EfIntakeReceiptStore.ParseDecision` (`:1241-1252`) the single enumeration and re-point `EfOperationsStore.MapIntakeState` (`:563-569`) and `src/Pegasus.Web/Mcp/IntakeMcpTools.cs` (`:62` description, `:82-84` filter parse, `:190-192` emit) at it. **Decide the fail-closed behaviour explicitly and record the decision in the `plan`**: `ParseDecision` throws `UnknownCode`, `MapIntakeState` returns `EmailOperationState.Unknown` and silently omits `blocked_intake` and `image_intake_registered`. One of those two behaviours survives; write down which and why.
6. **Desktop-world sequencing.** This ticket must land before [[DSK-03-10]] step 3 adds `src/Pegasus.Contracts/Intake/` DTOs and before its step 11 commits the OpenAPI snapshot and the generated client, or a fourth divergent copy becomes a published contract. Record in the `plan` that the shared table established here is the one the Contracts project created by [[DSK-02-04]] maps to, and add a line to the ticket `plan` naming [[DSK-03-04]] and [[DSK-03-05]] as the snapshot and client that would otherwise pin the divergence.
7. Sub-item (c), the composition fact. Add to `tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs` a fact that `Pegasus.Web`'s composition resolves no Azure Queue client and cannot resolve `ProcessQueuedIntake`/`IProcessQueuedIntake`, and remove the duplicate assertion from `tests/Pegasus.IntegrationTests/QdosIntakeWebTests.cs`. Done when deliberately registering a queue client in a scratch composition makes the new fact fail. Coordinate with [[DSK-02-12]], which extends the same file for the desktop boundaries and the no-WebView rule — the two must not both add the same fact.
8. Sub-item (d), the leftover port. **Re-expressed:** the upstream body says `IIntakeSubmission` has "two Web callers"; on this tree it has one Web caller (`src/Pegasus.Web/Mcp/IntakeMcpTools.cs:46`) and one Core caller (`src/Pegasus.Core/Intake/GroupedIntake.cs:148`), plus registrations at `src/Pegasus.Web/Program.cs:615-617` and `src/Pegasus.Worker/WorkerDependencyInjection.cs:94`. Record the caller set as it actually is, then either fold both callers onto `ReceiveIntake` and delete the interface and both registrations, or record the second concrete need in the `plan` and leave it. `AGENTS.md` § Simplicity rails ("No abstraction without a second concrete caller, an external boundary, or an accepted ADR") is the rule being applied.
9. Add or extend tests for each of the four sub-items: fault naming in `tests/Pegasus.IntegrationTests/RecoveryTests.cs`, the single code table in `tests/Pegasus.Core.Tests/Intake/` plus a fact that every persisted code round-trips through one place, the composition fact in `tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs`, and the port removal by compilation.
10. Run the simplification pass over the branch diff, record it under a dated `## Simplification pass` heading in the ticket `plan`, then open the PR into `dev`. Do **not** merge upstream straight into `main`.

## Acceptance conditions

- [ ] `IntakeExceptionPolicy.IsTransientFailure` lists no BCL exception type, or the `plan` records exactly which one remains and why.
- [ ] Exactly one place in the solution enumerates the persisted intake decision codes; `EfOperationsStore` and `IntakeMcpTools` read through it, and the fail-closed behaviour for an unknown code is recorded as a decision.
- [ ] `tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs` fails if `Pegasus.Web` registers a queue client or `ProcessQueuedIntake`; `QdosIntakeWebTests` no longer duplicates the assertion.
- [ ] `IIntakeSubmission` is removed with its registrations, or its second concrete need is recorded in the `plan`.
- [ ] No operator-facing word changes: this is a code-vocabulary change only, and `docs/design/README.md`'s label table is untouched.

## Verification

- [ ] `IsTransientProcessingFailure` lists no BCL exception types once every adapter in the processor's path names its faults (or the plan records why one remains).
- [ ] Exactly one place enumerates persisted intake decision codes; Operations and MCP read through it.
- [ ] Architecture test fails if Web registers a queue client or the processor.
- [ ] `IIntakeSubmission` removed or its second concrete need recorded.

## Risks and boundaries

- **Azure**: no write. Read-only checks of the `transient-intake` container or Application Insights are permitted with no per-target approval (`docs/runbook.md` § Live-operation approval matrix; mirrored in `docs/desktop/11-azure-disposition/README.md`).
- **Scope boundary**: may touch `src/Pegasus.Core/Intake/`, `src/Pegasus.Infrastructure/Intake/`, `src/Pegasus.Infrastructure/Persistence/`, `src/Pegasus.Web/Mcp/IntakeMcpTools.cs`, `src/Pegasus.Web/Program.cs` composition, `src/Pegasus.Worker/WorkerDependencyInjection.cs` and the three test projects. Must **not** touch `src/Pegasus.Web/Pages/Intake/**` (they die with the cut list, [[DSK-05-26]]), `src/Pegasus.Web/Api/**` (that is [[DSK-03-10]]'s), or any operator-facing label.
- **Unblocks / blocked by**: this ticket **blocks** [[DSK-03-10]] — the gateway must not publish a fourth decision-code reader into `openapi/pegasus-v1.json` and the generated client. It is **blocked by** [[DSK-01-10]], the first one-way upstream sync, because upstream `main` is ahead of the fork on intake paths. It touches the same file as [[DSK-02-12]] (`DependencyDirectionTests`) — sequence, do not collide. Its cleaned vocabulary is what [[DSK-05-09]] and [[DSK-05-20]] render, and [[DSK-05-23]]/[[DSK-03-16]] carry the operator labels over that vocabulary unchanged.
- **Traps**: **upstream ids and fork board ids do not match.** This ticket is board `INTK-001` and it is upstream INTK-002. Upstream INTK-001 is a different ticket entirely — make the upload status honest for retry-scheduled work and auto-associated receipts — which was absorbed into [[DSK-05-13]] and [[DSK-03-11]] and has **no fork ticket**, so never read a bare `INTK-001` as it. The join table is `HZN-001/board-conventions.md` § Upstream ids versus board ids: read it, never compute the mapping, and write `upstream <ID>`, or `upstream <ID> (board [[<board-id>]])` where both are meant. This is a Worker/Core/Infrastructure chore, not desktop work — do not add a desktop project reference, a WinUI type or a `/api/v1` route here. A new table would need a runtime-role `Grant*` migration checked by `scripts/Test-MigrationGrants.ps1`; this ticket must not add one. Any new `.md` outside `docs/(prd|frd|adr|design|desktop)` fails the CI `documentation` job — ticket-transient notes live in Kanmer.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the ticket `plan` document.
