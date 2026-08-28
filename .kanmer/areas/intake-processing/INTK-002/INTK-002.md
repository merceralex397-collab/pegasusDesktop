---
id: INTK-002
type: ticket
title: >-
  upstream:INTK-003 · Recover dispatched intake work whose queue message never
  arrives
status: review
area: intake-processing
order: 90
assignee: codex-mcp-client
profile: fix
stageEntered:
  preparing: '2026-08-24T21:23:32.235Z'
  review: '2026-08-26T19:42:08.024Z'
labels:
  - upstream-carryover
  - upstream-INTK-003
  - gateway-worker-ticket
groups:
  - EPIC-014
links: []
blocks:
  - FEAT-009
  - FEAT-013
  - FEAT-020
refs:
  - docs/frd/frd-02-intake-and-source-identity.md
docs_todo: true
archived: false
created: '2026-08-24T11:44:22.475Z'
updated: '2026-08-28T21:07:57.888Z'
---

## What

Give `dispatched` intake work items a reconciliation path. An unleased row in `dispatched` older than a chosen age returns to `pending` through the existing reconciliation timer, so a receipt whose queue message never reaches the Worker is re-dispatched instead of sitting as "Received" forever.

## Why

The desktop conversion does not change one line of this pipeline — `docs/desktop/05-implementation-and-migration/reuse-map.md` marks `src/Pegasus.Worker` REUSE unchanged and names the upstream Worker tickets (upstream INTK-003, this ticket; and upstream INTK-027, board [[INTK-004]]) as carried-over Worker tickets rather than desktop work — but it multiplies the number of places that lie about it. [[DSK-05-09]] renders the Received item's state, [[DSK-05-13]] renders upload status, and [[DSK-05-20]] with [[DSK-07-01]] render "whether a human retry is currently eligible". A stranded `dispatched` row is reported by all three as neither failed nor retryable: `RecoverExpiredLeasesAsync` only looks at leased `dispatching|processing` rows, and the dispatch scan only looks at `pending|retry_scheduled`, so nothing on the board or in the code ever sees it again. The operator is shown a truthful-looking "Received" for work that will never run.

**No seeded ticket may make the fix.** [[DSK-07-01]] states "No Worker code is written or changed" and its scope boundary reads "Must not touch `src/Pegasus.Worker`"; [[DSK-05-09]]'s reads "Must not touch `src/Pegasus.Infrastructure` (readers stay central), `src/Pegasus.Worker`"; [[DSK-03-10]]'s reads "Must not touch `src/Pegasus.Core/Intake/**`, the Worker". Under **D-001** upstream is merged once more and then frozen, so if the fork does not own this it is never done.

It is resilience, not repair: the upstream ticket records a read-only production count of **0** such rows on 2026-08-17. That is the reason it is small and separate, not a reason to drop it — under **L-02** the local production-mimicking stack is the only verification environment, and a long local Worker outage is exactly how the condition is reached.

## Source of truth

- Import decision: `coverage-decision.md` § Import list — the row for upstream `INTK-003` (this ticket; board `INTK-002`); § Plan gaps — "The 208-ticket set contains no owner for Worker and Core/Infrastructure intake defects"
- Carry-over register: `docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md:152` — the row for upstream `INTK-003`, quoted as it stands (its first cell is an upstream id): `INTK-003 | intake-processing | backlog | fix | — | … | gateway-worker-ticket | 07 (Graph/queue intake) | intake-processing`
- Reuse position: `docs/desktop/05-implementation-and-migration/reuse-map.md` § `Pegasus.Worker`
- Governing document: `docs/frd/frd-02-intake-and-source-identity.md`
- Repository evidence (fork `main`, read 2026-08-24):
  - `src/Pegasus.Core/Intake/DurableIntake.cs:216` — `IIntakeWorkStore.RecoverExpiredLeasesAsync`; `:174` `MarkDispatchedAsync`; `:186` `ClaimProcessingAsync`
  - `src/Pegasus.Core/Intake/DurableIntake.cs:369-400` — the dispatch loop that calls `MarkDispatchedAsync` at `:387` after the enqueue
  - `src/Pegasus.Core/Intake/DurableIntake.cs:935-950` — `ReconcileStagedArtifacts.ExecuteAsync`, the reconciliation timer that already calls `RecoverExpiredLeasesAsync`; this is the sibling the fix belongs beside
  - `src/Pegasus.Infrastructure/Persistence/EfIntakeWorkStore.cs:416-455` — the implementation: candidates are leased `dispatching|processing` only; `dispatching → pending`, `processing → retry_scheduled`, and `processing` with `AttemptCount >= 5` → `failed` with `processing_lease_expired`
  - `src/Pegasus.Infrastructure/Persistence/EfIntakeWorkStore.cs:214` and `:512` — the dispatch candidate filters, `pending` or `retry_scheduled` only; `:66` — the live-state set; `:722`/`:734` — the state code table
  - `src/Pegasus.Worker/AzureQueueIntakeWorkQueue.cs:19-24` — `SendMessageAsync` with **no** `timeToLive`, so the Azure Queue Storage default applies; `src/Pegasus.Worker/host.json:12-18` — `visibilityTimeout` `00:05:00`, `maxDequeueCount` 5, `maxPollingInterval` `00:00:02`
  - `src/Pegasus.Worker/IntakeFunctions.cs:69` and `src/Pegasus.Worker/WorkerDependencyInjection.cs:100` — where `ReconcileStagedArtifacts` is triggered and composed
  - `tests/Pegasus.IntegrationTests/RecoveryTests.cs` — the suite the new case joins
- Binding decisions: **L-02** the local production-mimicking stack (local Worker, Azurite, LocalDB/SQL container) is the verification environment — no Azure dev/test/staging; **L-05** the fork board is the single work register; **D-001** the fork is the single release source after the first production gateway change, so unmerged upstream work vanishes at the freeze
- Depends on: `DSK-01-10` — land the first one-way upstream sync before editing intake paths
- Not a dependency: upstream `SIMPLI-009` / `SIMPLI-010` are upstream simplification tickets with no fork equivalent; they are context for the finding, not work.

### Upstream ticket INTK-003 (verbatim)

Provenance — upstream area `intake-processing`; upstream status `backlog`; upstream profile `fix`; upstream labels: *(none)*; upstream groups `EPIC-002`; upstream links `SIMPLI-009`, `SIMPLI-010`. Read from the read-only clone of `collisionengineers/pegasus` branch `kanmer-board` at commit `a5b28111`, read date **2026-08-24**. Copied unedited.

````
## What

A work item in `dispatched` (message enqueued, lease cleared by `MarkDispatchedAsync`) has no reconciliation path if its queue message never reaches the Worker (message TTL expiry after a long Worker outage, manual queue clearing). `RecoverExpiredLeasesAsync` covers `dispatching|processing` leased rows; dispatch candidates are `pending|retry_scheduled`. Such a row stays "Received" forever.

## Why

Found in the PR #385 review of [[SIMPLI-009]] (T3). A 2026-08-17 read-only production count showed **0** such rows, so this is resilience, not repair — hence a small separate ticket rather than scope on [[SIMPLI-010]].

## Approach

Generalise `RecoverExpiredLeasesAsync` (or a sibling in the same reconciliation timer) to return unleased `dispatched` rows older than a chosen age (e.g. 1 h since `DueAtUtc`) to `pending`. Safe: a duplicate message no-ops because `ClaimProcessingAsync` refuses settled or leased work. Choose the age against the queue visibility timeout and message TTL in `docs/operations.md`; one `RecoveryTests` case; FRD-02 sentence if behaviour is stated there.

## Verification

- [ ] A `dispatched` row older than the threshold with no lease is re-dispatched by the reconciliation timer and processed once.
- [ ] A freshly dispatched row is left alone.

## Outcome
````

## Routing

- **Subagent**: `pegasus-gateway-dev` — `.codex/agents/pegasus-gateway-dev.toml`; tests by `pegasus-test-engineer` — `.codex/agents/pegasus-test-engineer.toml`
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `optimizing-ef-core-queries` (dotnet/skills `98f84851`, `plugins/dotnet-data/skills/optimizing-ef-core-queries/SKILL.md`) → `code-testing-agent` (dotnet/skills `98f84851`) → `run-tests` (dotnet/skills `98f84851`)
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search` for Azure Queue Storage message time-to-live defaults and `QueueClient.SendMessageAsync` overloads)
- **Kanmer pipeline** for profile `fix`: `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (call `get_doc_gates <id>` before every move; `fix` needs `files`, `plan` and `questions-resolved` to leave Preparing, `post-implementation-report` to enter Review, `proof` to enter Done)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Orient. Read the verbatim upstream body above, `coverage-decision.md` § Import list row for upstream `INTK-003`, and `docs/frd/frd-02-intake-and-source-identity.md`. Call `get_doc_gates <this ticket id>`, then `take_ticket` with branch `task/upstream-intk-003-recover-dispatched-work` and worktree `../pegasus-worktrees/upstream-intk-003-recover-dispatched-work` from `origin/dev`.
2. Record the current lifecycle in the ticket `files` document: every state code in `src/Pegasus.Infrastructure/Persistence/EfIntakeWorkStore.cs:722`/`:734`, which states `FindExpiredLeaseCandidatesAsync` selects (`:416-455`), and which states the dispatch scan selects (`:214`, `:512`). Name the exact gap: `dispatched` with a null `LeaseToken` is selected by neither.
3. **Choose the age against real numbers, not the upstream example.** The upstream Approach says "e.g. 1 h since `DueAtUtc`" and points at `docs/operations.md`. On this tree the numbers are: `src/Pegasus.Worker/host.json:12-18` — `visibilityTimeout` `00:05:00`, `maxDequeueCount` 5, `maxPollingInterval` `00:00:02`; and `src/Pegasus.Worker/AzureQueueIntakeWorkQueue.cs:19-24` sends with **no** `timeToLive`, so the Azure Queue Storage service default applies. Confirm that default with `microsoft_docs_search` for `QueueClient.SendMessageAsync` message time-to-live default, record the figure and its source in the `plan`, and pick a threshold safely above the visibility timeout and safely below the message TTL. Do not invent a latency threshold without recording the decision (`docs/engineering.md` § Required evidence tiers, tier 10).
4. Extend the reconciliation path. Either generalise `IIntakeWorkStore.RecoverExpiredLeasesAsync` (`src/Pegasus.Core/Intake/DurableIntake.cs:216`, implemented at `src/Pegasus.Infrastructure/Persistence/EfIntakeWorkStore.cs:416`) to include unleased `dispatched` rows older than the threshold, or add a sibling method called from the same place. Whichever you choose, it is driven from `ReconcileStagedArtifacts.ExecuteAsync` (`src/Pegasus.Core/Intake/DurableIntake.cs:935-950`) — do **not** add a second timer.
5. Return such a row to `pending` with its lease fields null, exactly as the `dispatching → pending` branch already does at `EfIntakeWorkStore.cs:436`, and preserve the optimistic-concurrency `Where` clause at `:446-450` so a concurrent claim wins. Do not reset `AttemptCount`; a recovered row must still reach `failed` at the existing limit.
6. Confirm the duplicate-message safety the upstream Approach asserts, rather than assuming it: read `ClaimProcessingAsync` (`src/Pegasus.Infrastructure/Persistence/EfIntakeWorkStore.cs:266`) and record in the `plan` the exact clause that makes a late-arriving duplicate a no-op against settled or leased work.
7. Add the two upstream verification cases to `tests/Pegasus.IntegrationTests/RecoveryTests.cs`: an unleased `dispatched` row older than the threshold is re-dispatched by the reconciliation timer and processed **once**; a freshly dispatched row is left alone. Add a third: a duplicate queue message arriving after recovery no-ops.
8. **Re-expressed for the desktop world.** The upstream body describes the operator symptom as the row staying "Received" forever on the Razor pages, which the cut list [[DSK-05-26]] deletes. State the same requirement against the surfaces that replace them and record it in the `plan`: after recovery, [[DSK-05-09]]'s Received item state, [[DSK-05-13]]'s upload status and [[DSK-07-01]]'s `GET /api/v1/operations/intake-status` retry-eligibility field must all move off "Received" without any of those tickets adding a second recovery mechanism.
9. Add the FRD-02 sentence if and only if the behaviour is stated there — read `docs/frd/frd-02-intake-and-source-identity.md` first and record in the `plan` whether it is, per the upstream Approach.
10. Verify on the local stack under **L-02**: run the Worker against Azurite, dispatch a receipt, clear the queue by hand, advance the clock past the threshold and confirm the reconciliation timer re-dispatches it. Capture the command log as `proof`.
11. Run the simplification pass over the branch diff, record it under a dated `## Simplification pass` heading in the ticket `plan`, then open the PR into `dev`.

## Acceptance criteria

- [ ] An unleased `dispatched` row older than the chosen threshold is returned to `pending` by the existing reconciliation timer and is processed exactly once.
- [ ] A freshly dispatched row is untouched, and a leased row is untouched.
- [ ] The chosen threshold, the queue visibility timeout and the message time-to-live it was chosen against are recorded in the ticket `plan` with their sources.
- [ ] `AttemptCount` is preserved across recovery, so a recovered row still reaches `failed` at the existing limit.
- [ ] A duplicate queue message arriving after recovery is a no-op.
- [ ] No second reconciliation timer and no new table are introduced.

## Verification

- [ ] `dotnet build --configuration Release` — expected: clean.
- [ ] `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --filter "FullyQualifiedName~RecoveryTests"` — expected: the stranded-dispatched case, the fresh-dispatch case and the duplicate-message case all pass.
- [ ] Local stack run (L-02) — expected: a hand-cleared queue message leaves a `dispatched` row that the reconciliation timer returns to `pending` and the Worker then processes once; command log captured as `proof`.

## Evidence tier

Tier 4 — LocalDB persistence. Tier 6 — Functions/Azurite caller.
Tier 4 obliges state/lease/concurrency evidence for the recovery update, including the stale-version case; tier 6 obliges the actual timer trigger against Azurite with duplicate, retry and restart behaviour observed, not mocked.

## Documentation changes

- `docs/frd/frd-02-intake-and-source-identity.md` — one sentence on the recovery of dispatched work, **only if** FRD-02 already states the surrounding behaviour (step 9 decides)
- `docs/operations.md` — record the chosen threshold beside the queue settings it was chosen against
- `docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md` — annotate the upstream `INTK-003` row with this fork ticket id (`INTK-002`)

## Guardrails

- **Azure**: no write. Read-only checks of the intake queue and Application Insights are permitted with no per-target approval (`docs/runbook.md` § Live-operation approval matrix; mirrored in `docs/desktop/11-azure-disposition/README.md`). The local run uses Azurite under **L-02**; asking for an Azure test resource is out of bounds (ADR-0014 stands).
- **Scope boundary**: may touch `src/Pegasus.Core/Intake/DurableIntake.cs`, `src/Pegasus.Infrastructure/Persistence/EfIntakeWorkStore.cs`, `tests/Pegasus.IntegrationTests/RecoveryTests.cs`, `docs/frd/frd-02-intake-and-source-identity.md` and `docs/operations.md`. Must **not** touch `src/Pegasus.Web/Pages/**`, `src/Pegasus.Web/Api/**`, any desktop project, or add a database table.
- **Unblocks / blocked by**: this ticket **blocks** [[DSK-05-09]], [[DSK-05-13]] and [[DSK-05-20]] — each renders a state that is dishonest while a `dispatched` row can strand, and [[DSK-07-01]]'s retry-eligibility field is computed over the same rows. It is **blocked by** [[DSK-01-10]], the first one-way upstream sync. [[DSK-08-17]]'s Test/UAT stack is where the tier-6 evidence is produced.
- **Traps**: **upstream ids and fork board ids do not match.** This ticket is board `INTK-002` and it is upstream INTK-003; upstream INTK-002 is the intake duplication chores, board [[INTK-001]]. The join table is `HZN-001/board-conventions.md` § Upstream ids versus board ids — read it, never compute the mapping, and write `upstream <ID>`, or `upstream <ID> (board <board-id>)` where both are meant. Do not reset `AttemptCount` on recovery — that would make a poisoned item immortal. Do not add a second timer; the reconciliation timer already exists. A new table would need a runtime-role `Grant*` migration checked by `scripts/Test-MigrationGrants.ps1`; this ticket must not add one. `IntakeWorkItems` state strings are persisted values — changing one is a migration, not a rename.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the ticket `plan` document.

## Outcome

_Filled at closeout._
