---
id: INTK-001
type: ticket
title: >-
  upstream:INTK-002 · Intake duplication chores: adapter-wide fault naming, one
  decision-code table, Web-composition assertion, leftover port
status: preparing
area: intake-processing
assignee: ''
profile: chore
stageEntered:
  preparing: '2026-08-24T21:23:32.015Z'
labels:
  - upstream-carryover
  - upstream-INTK-002
  - gateway-worker-ticket
groups:
  - EPIC-014
links: []
blocks:
  - GWY-010
docs_todo: true
archived: false
created: '2026-08-24T11:44:22.452Z'
updated: '2026-08-24T21:23:32.015Z'
---

## What

Land the four upstream INTK-002 duplication chores server-side on the fork: adapters name their faults so the intake retry taxonomy matches intake types only; the persisted intake decision-code table exists once instead of three times; an architecture fact asserts that `Pegasus.Web` composes no queue client and cannot resolve `ProcessQueuedIntake`; and the leftover `IIntakeSubmission` port is deleted or its second concrete need is recorded.

## Why

The desktop conversion inherits all four defects unchanged and makes the second one **worse**. `docs/desktop/05-implementation-and-migration/reuse-map.md` marks `src/Pegasus.Worker` and the intake stores REUSE, so every copy of the decision-code table ships into the desktop era untouched. [[DSK-03-10]] step 3 adds `src/Pegasus.Contracts/Intake/` DTOs carrying "receipt, evidence, suggestions, drafts and the OCR-required state" — a **fourth** reader of the same code set — and its step 11 commits the OpenAPI snapshot and the generated client, which turns the divergence into a published contract that [[DSK-03-04]] and [[DSK-03-05]] then pin. The divergence is already operator-visible: `EfOperationsStore.MapIntakeState` (`src/Pegasus.Infrastructure/Persistence/EfOperationsStore.cs:563-569`) knows four codes and returns `EmailOperationState.Unknown` for the rest, omitting `blocked_intake` and `image_intake_registered`, while `EfIntakeReceiptStore.ParseDecision` (`:1241-1252`) knows seven and throws on an unknown one — so the Operations snapshot [[DSK-05-20]] renders and the Received item screen [[DSK-05-09]] shows are already two vocabularies for one fact.

**No seeded ticket is permitted to make this fix.** [[DSK-03-10]]'s scope boundary reads "Must not touch `src/Pegasus.Core/Intake/**`, the Worker, or `src/Pegasus.Web/Pages/Intake/**`"; [[DSK-05-09]]'s reads "Must not touch `src/Pegasus.Infrastructure` (readers stay central), `src/Pegasus.Worker`"; [[DSK-07-01]] states in its own § What "No Worker code is written or changed". `docs/desktop/05-implementation-and-migration/reuse-map.md` § `Pegasus.Worker` names the upstream Worker tickets as carried-over Worker tickets rather than desktop work, and the seeded guardrails prove that reading right. Without this ticket the work has no owner on the 208-ticket board.

## Source of truth

- Import decision: `coverage-decision.md` § Import list — the row for upstream `INTK-002` (this ticket; board `INTK-001`); § Plan gaps — "The 208-ticket set contains no owner for Worker and Core/Infrastructure intake defects, yet the desktop inherits all of them unchanged"
- Carry-over register: `docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md:151` — the row for upstream `INTK-002`, quoted as it stands (its first cell is an upstream id): `INTK-002 | intake-processing | backlog | chore | — | Intake duplication chores… | gateway-worker-ticket | 03, 10 | intake-processing`
- Reuse position: `docs/desktop/05-implementation-and-migration/reuse-map.md` § `Pegasus.Worker` (Worker REUSE unchanged; Worker defects carried over as Worker tickets)
- Repository evidence (fork `main`, read 2026-08-24):
  - `src/Pegasus.Core/Intake/IntakeContracts.cs:593-601` — `IntakeExceptionPolicy.IsTransientFailure`, which lists three BCL types (`IOException`, `TimeoutException`, `DbException`) plus the inner-exception walk
  - `src/Pegasus.Core/Intake/DurableIntake.cs:573` — the only `catch … when` that consults it; `:875-892` — `TerminalInputFailureCode` and `TransientFailureCode`
  - `src/Pegasus.Infrastructure/Intake/AzureBlobIntakeArtifactStore.cs:451` — the `DependencyUnavailable` factory (read/upload paths only)
  - `src/Pegasus.Infrastructure/Intake/FileSystemIntakeArtifactStore.cs:9` — no fault translation at all
  - `src/Pegasus.Infrastructure/Persistence/EfIntakeReceiptStore.cs:41` — the bare `InvalidOperationException("The intake receipt could not be stored after the concurrency retry limit.")`
  - `src/Pegasus.Infrastructure/Persistence/EfIntakeReceiptStore.cs:1241-1252` — `ParseDecision` (seven codes, throws `UnknownCode`)
  - `src/Pegasus.Infrastructure/Persistence/EfOperationsStore.cs:154`, `:563-569` — `MapIntakeState` (four codes, `Unknown` otherwise)
  - `src/Pegasus.Web/Mcp/IntakeMcpTools.cs:62`, `:82-84`, `:190-192`, `:202` — the third copy, in a tool `Description` string, a filter parse and an emit
  - `src/Pegasus.Infrastructure/Persistence/EfCaseAcceptanceStore.cs:200` and `EfRetainedMailboxMessageStore.cs:767` — readers that already go through `ParseDecision`, the shape to collapse onto
  - `tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs:21` — where the Web-composition fact belongs
  - `tests/Pegasus.IntegrationTests/QdosIntakeWebTests.cs:9` — where it is asserted today
  - `src/Pegasus.Core/Intake/DurableIntake.cs:142` (`IIntakeSubmission`), `:247` (`ReceiveIntake`); callers `src/Pegasus.Web/Mcp/IntakeMcpTools.cs:46` and `src/Pegasus.Core/Intake/GroupedIntake.cs:148`; registrations `src/Pegasus.Web/Program.cs:615-617` and `src/Pegasus.Worker/WorkerDependencyInjection.cs:94`
- Binding decisions: **L-01** the gateway is `Pegasus.Web` evolved in place, so the fourth code reader lands inside the same assembly this ticket is cleaning; **L-02** verification is the local production-mimicking stack — no Azure test resource; **L-05** the fork board is the single work register, so an upstream chore with no fork ticket is unowned work; **D-001** the fork becomes the single release source and upstream is frozen, so nobody upstream will do this after the freeze
- Depends on: `DSK-01-10` — land the first one-way upstream sync before editing intake paths, so this does not re-project code that has since changed upstream (the same trap [[DSK-03-10]] records)

### Upstream ticket INTK-002 (verbatim)

Provenance — upstream area `intake-processing`; upstream status `backlog`; upstream profile `chore`; upstream labels: *(none)*; upstream groups `EPIC-002`; upstream links `SIMPLI-009`, `SIMPLI-010`. Read from the read-only clone of `collisionengineers/pegasus` branch `kanmer-board` at commit `a5b28111`, read date **2026-08-24**. Copied unedited. Where the body names a symbol that has since been renamed in this tree, the correction is in the Implementation steps below and never in this block.

````
## What

Carry-forward from the simplification passes on [[SIMPLI-009]] (PR #385) and [[SIMPLI-010]]:

- Adapter-wide fault naming: `AzureBlobIntakeArtifactStore` translates only its read/upload paths; `FileSystemIntakeArtifactStore` throws raw `IOException`; EF stores surface raw SQL faults; `EfIntakeReceiptStore.StoreAsync` throws a bare `InvalidOperationException` after three consecutive deadlocks (terminal under the new taxonomy). Adapters should name faults (`IntakeDependencyUnavailableException` / a named concurrency conflict) so `ProcessQueuedIntake.IsTransientProcessingFailure` matches intake types only.
- One decision-code table: `EfOperationsStore.MapIntakeState` is a second hand-kept copy of `EfIntakeReceiptStore.ParseDecision`'s string set, and `IntakeMcpTools` a third — SIMPLI-010 had to edit two of them to remove one code. Collapse onto `ParseDecision` (same assembly) or a shared code table; note `ParseDecision` throws on unknown codes where `MapIntakeState` returns `Unknown` (and omits `blocked_intake`/`image_intake_registered`), so decide the fail-closed behaviour explicitly.
- `DependencyDirectionTests`: a fact that `Pegasus.Web` composes no queue client and cannot resolve `ProcessQueuedIntake` (today asserted only inside `QdosIntakeWebTests`).
- `IIntakeSubmission` has one implementation (`ReceiveIntake`) and two Web callers — fold the callers onto `ReceiveIntake` and delete the interface and its registration unless a test double is genuinely wanted.

## Why

One place per taxonomy and per code table; adapters name faults, Core matches its own types; invariants asserted where architecture tests live; no leftover abstraction.

## Verification

- [ ] `IsTransientProcessingFailure` lists no BCL exception types once every adapter in the processor's path names its faults (or the plan records why one remains).
- [ ] Exactly one place enumerates persisted intake decision codes; Operations and MCP read through it.
- [ ] Architecture test fails if Web registers a queue client or the processor.
- [ ] `IIntakeSubmission` removed or its second concrete need recorded.

## Outcome
````

## Routing

- **Subagent**: `pegasus-gateway-dev` — `.codex/agents/pegasus-gateway-dev.toml`; tests by `pegasus-test-engineer` — `.codex/agents/pegasus-test-engineer.toml`
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `code-testing-agent` (dotnet/skills `98f84851`, `plugins/dotnet-test/skills/code-testing-agent/SKILL.md`) → `run-tests` (dotnet/skills `98f84851`) → `test-gap-analysis` (dotnet/skills `98f84851`) → `microsoft-code-reference` (Microsoft Learn plugin)
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search`, `microsoft_code_sample_search`)
- **Kanmer pipeline** for profile `chore`: `kanmer-plan` → `kanmer-execute` → `kanmer-verify` → `kanmer-closeout` (call `get_doc_gates <id>` before every move; `chore` needs `plan` and `questions-resolved` to leave Preparing and `proof` to enter Done)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

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

## Acceptance criteria

- [ ] `IntakeExceptionPolicy.IsTransientFailure` lists no BCL exception type, or the `plan` records exactly which one remains and why.
- [ ] Exactly one place in the solution enumerates the persisted intake decision codes; `EfOperationsStore` and `IntakeMcpTools` read through it, and the fail-closed behaviour for an unknown code is recorded as a decision.
- [ ] `tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs` fails if `Pegasus.Web` registers a queue client or `ProcessQueuedIntake`; `QdosIntakeWebTests` no longer duplicates the assertion.
- [ ] `IIntakeSubmission` is removed with its registrations, or its second concrete need is recorded in the `plan`.
- [ ] No operator-facing word changes: this is a code-vocabulary change only, and `docs/design/README.md`'s label table is untouched.

## Verification

- [ ] `dotnet build --configuration Release` — expected: clean build across `Pegasus.Core`, `Pegasus.Infrastructure`, `Pegasus.Web`, `Pegasus.Worker`.
- [ ] `dotnet test ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj --configuration Release` — expected: the new Web-composition fact passes and fails when a queue client is registered.
- [ ] `dotnet test ./tests/Pegasus.Core.Tests/Pegasus.Core.Tests.csproj --configuration Release` — expected: the single decision-code table round-trips every persisted code.
- [ ] `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --filter "FullyQualifiedName~RecoveryTests"` — expected: named adapter faults still classify as retryable where they did before.

## Evidence tier

Tier 1 — Static/build/architecture. Tier 2 — Core/domain. Tier 5 — Web/API/MCP caller.
Tier 1 obliges the dependency-direction fact and a clean Release build of the four projects; tier 2 obliges positive and unknown-code cases for the collapsed decision table with the fail-closed behaviour asserted; tier 5 obliges evidence that the MCP intake tool reads the same table through the real caller.

## Documentation changes

- `docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md` — annotate the upstream `INTK-002` row with this fork ticket id (`INTK-001`), so the register joins to the board (`DSK-00-04` verifies the join)
- `docs/frd/frd-02-intake-and-source-identity.md` — only if the fail-closed decision in step 5 changes stated behaviour; otherwise `None.`

## Guardrails

- **Azure**: no write. Read-only checks of the `transient-intake` container or Application Insights are permitted with no per-target approval (`docs/runbook.md` § Live-operation approval matrix; mirrored in `docs/desktop/11-azure-disposition/README.md`).
- **Scope boundary**: may touch `src/Pegasus.Core/Intake/`, `src/Pegasus.Infrastructure/Intake/`, `src/Pegasus.Infrastructure/Persistence/`, `src/Pegasus.Web/Mcp/IntakeMcpTools.cs`, `src/Pegasus.Web/Program.cs` composition, `src/Pegasus.Worker/WorkerDependencyInjection.cs` and the three test projects. Must **not** touch `src/Pegasus.Web/Pages/Intake/**` (they die with the cut list, [[DSK-05-26]]), `src/Pegasus.Web/Api/**` (that is [[DSK-03-10]]'s), or any operator-facing label.
- **Unblocks / blocked by**: this ticket **blocks** [[DSK-03-10]] — the gateway must not publish a fourth decision-code reader into `openapi/pegasus-v1.json` and the generated client. It is **blocked by** [[DSK-01-10]], the first one-way upstream sync, because upstream `main` is ahead of the fork on intake paths. It touches the same file as [[DSK-02-12]] (`DependencyDirectionTests`) — sequence, do not collide. Its cleaned vocabulary is what [[DSK-05-09]] and [[DSK-05-20]] render, and [[DSK-05-23]]/[[DSK-03-16]] carry the operator labels over that vocabulary unchanged.
- **Traps**: **upstream ids and fork board ids do not match.** This ticket is board `INTK-001` and it is upstream INTK-002. Upstream INTK-001 is a different ticket entirely — make the upload status honest for retry-scheduled work and auto-associated receipts — which was absorbed into [[DSK-05-13]] and [[DSK-03-11]] and has **no fork ticket**, so never read a bare `INTK-001` as it. The join table is `HZN-001/board-conventions.md` § Upstream ids versus board ids: read it, never compute the mapping, and write `upstream <ID>`, or `upstream <ID> (board [[<board-id>]])` where both are meant. This is a Worker/Core/Infrastructure chore, not desktop work — do not add a desktop project reference, a WinUI type or a `/api/v1` route here. A new table would need a runtime-role `Grant*` migration checked by `scripts/Test-MigrationGrants.ps1`; this ticket must not add one. Any new `.md` outside `docs/(prd|frd|adr|design|desktop)` fails the CI `documentation` job — ticket-transient notes live in Kanmer.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the ticket `plan` document.

## Outcome

_Filled at closeout._
