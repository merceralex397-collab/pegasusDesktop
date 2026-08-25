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

## Current-fork implementation record — 2026-08-25

- The current fork uses IntakeExceptionPolicy.IsTransientFailure (src/Pegasus.Core/Intake/IntakeContracts.cs) as the processor retry predicate. FileSystemIntakeArtifactStore now translates its public filesystem IOException paths to IntakeDependencyUnavailableException; EfIntakeReceiptStore already terminates its concurrency retry loop with IntakeVersionConflictException. No BCL IOException, TimeoutException, or DbException remains in the Core retry taxonomy. The Recovery fixture now supplies the named adapter fault that the contract requires.
- The fail-closed decision is explicit: domain/persistence parsing uses IntakeDecisionCodes.Parse and throws InvalidDataException for an unknown persisted code; the Operations projection uses TryParse and maps an unknown or BlockedIntake decision to EmailOperationState.Unknown; MCP rejects an unknown filter. This preserves the projection boundary without presenting corrupt data as a successful operation.
- IntakeDecisionCodes is the one persisted decision-code vocabulary. EfIntakeReceiptStore, EfOperationsStore, ProcessIntake, image/custody/mutation/dashboard persistence callers, and MCP use it. The MCP description no longer enumerates a second list. The decision table is the source that the future Contracts mapping for [[DSK-02-04]] / snapshot [[DSK-03-04]] / client [[DSK-03-05]] must consume; no gateway/OpenAPI copy is added here.
- The Web composition fact moved from QdosIntakeWebTests to DependencyDirectionTests: the Web assembly does not reference Pegasus.Worker and defines no IProcessQueuedIntake implementation. Worker composition remains separately covered.
- IIntakeSubmission is retained. The concrete callers are IntakeMcpTools (Web automation ingress) and GroupedIntake (Core grouped-intake orchestration), with ReceiveIntake as the sole implementation and the Web registration as the composition boundary. The interface also gives GroupedIntakeTests a focused fake; deleting it would widen this chore into a command-port refactor without a current requirement.
- Added focused tests: every IntakeDecision round-trips through the shared vocabulary; unknown codes fail closed; a local filesystem dependency failure is translated; the Web composition fact passes.

## Simplification pass — 2026-08-25

- Reused the existing IntakeDependencyUnavailableException, IntakeVersionConflictException, Core IntakeDecision enum, and existing Azure adapter translation shape. No new package, service, compatibility path, or persistent format was introduced.
- The shared code table is a small Core static owner because it has multiple Core, Infrastructure, and Web callers; the adapters do not retain duplicate decision switches. The retained IIntakeSubmission is justified by two production callers plus the existing grouped-intake fake.
- The filesystem catches are at the public adapter boundary and preserve integrity, argument, cancellation, and domain exceptions. No unrelated Web pages, API routes, labels, Azure state, or operator documents were changed.
- Unapplied finding: the architecture assertion proves the compiled Web assembly cannot reference or implement the Worker queue processor; it does not build a deliberately invalid queue-client registration because that would be a negative test composition rather than production behavior. The same boundary is enforced by the project-reference architecture test.

## Verification record — 2026-08-25

- dotnet build src/Pegasus.Core/Pegasus.Core.csproj --configuration Release --no-restore — passed.
- dotnet build src/Pegasus.Infrastructure/Pegasus.Infrastructure.csproj --configuration Release --no-restore — passed.
- dotnet test tests/Pegasus.Core.Tests/Pegasus.Core.Tests.csproj --configuration Release --no-build --filter "FullyQualifiedName~IntakeDecisionCodesTests" — 2 passed.
- dotnet test tests/Pegasus.Core.Tests/Pegasus.Core.Tests.csproj --configuration Release --no-build --filter "FullyQualifiedName~ProcessIntakeTests" — 43 passed.
- dotnet test tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj --configuration Release --filter "FullyQualifiedName~FileSystemIntakeArtifactStoreTests|FullyQualifiedName~DependencyDirectionTests.WebCompositionDoesNotOwnTheWorkerIntakeProcessor" — 2 passed.
- dotnet test tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --filter "FullyQualifiedName~RecoveryTests.TransientProcessingFailureSchedulesARetry" — 1 passed in 29 seconds.
- git diff --check — passed; warnings only noted that Git would normalize patched LF files to repository CRLF on a future touch.

## Review remediation — 2026-08-25

The independent review initially failed the branch and identified:
- EF retry exhaustion still throwing InvalidOperationException;
- the architecture check not proving live Web DI absence;
- one Operations failure-code comparison still spelling technical_failure;
- Recovery fixture coverage still naming the removed raw io fault;
- cleanup masking risk.

All required findings are resolved:
- EfIntakeReceiptStore now throws IntakeVersionConflictException after retry exhaustion.
- DependencyDirectionTests guards the Web assembly against Pegasus.Worker and Azure.Storage.Queues references/types, and QdosIntakeWebTests now exercises the real Web host and asserts both ProcessQueuedIntake and IProcessQueuedIntake are absent from its service provider.
- EfOperationsStore derives the technical-failure code from IntakeDecisionCodes.
- The Recovery exhaustion fixture uses the named dependency-unavailable exception; the policy test asserts named dependency/version faults are transient while raw IOException and TimeoutException are not.
- FileSystemIntakeArtifactStore cleanup is best-effort and cannot replace a primary operation exception.

## Final verification — 2026-08-25

- dotnet build Pegasus.slnx --configuration Release --no-restore -p:MSBuildNodeReuse=false — passed; Core, Infrastructure, Web, Worker, ArchitectureTests, IntegrationTests all built with 0 warnings and 0 errors.
- dotnet test tests/Pegasus.Core.Tests/Pegasus.Core.Tests.csproj --configuration Release --no-build --filter "FullyQualifiedName~ProcessIntakeTests|FullyQualifiedName~IntakeDecisionCodesTests" -p:MSBuildNodeReuse=false — 46 passed.
- dotnet test tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj --configuration Release --no-build -p:MSBuildNodeReuse=false — 101 passed.
- dotnet test tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --no-build --filter "FullyQualifiedName~RecoveryTests" -p:MSBuildNodeReuse=false — 27 passed in 2m17s.
- dotnet test tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --no-build --filter "FullyQualifiedName~QdosIntakeWebTests.ReadableManualUploadStagesPendingWorkAndOpensItsStatusPage" -p:MSBuildNodeReuse=false — 1 passed in 42s; live Web DI absence assertions passed.
