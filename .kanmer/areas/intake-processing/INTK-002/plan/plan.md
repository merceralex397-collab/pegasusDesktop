# Plan — INTK-002: upstream:INTK-003 · Recover dispatched intake work whose queue message never arrives

## Governing documents

- `docs/frd/frd-02-intake-and-source-identity.md`

## Chosen approach

Give `dispatched` intake work items a reconciliation path. An unleased row in `dispatched` older than a chosen age returns to `pending` through the existing reconciliation timer, so a receipt whose queue message never reaches the Worker is re-dispatched instead of sitting as "Received" forever.

## Routing and constraints

- Future owner: Core/Infrastructure intake work with focused Core and integration tests. The local Test/UAT stack is the verification environment; no production intake or Azure write is authorized.


## Ordered implementation steps

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

## Acceptance conditions

- [ ] An unleased `dispatched` row older than the chosen threshold is returned to `pending` by the existing reconciliation timer and is processed exactly once.
- [ ] A freshly dispatched row is untouched, and a leased row is untouched.
- [ ] The chosen threshold, the queue visibility timeout and the message time-to-live it was chosen against are recorded in the ticket `plan` with their sources.
- [ ] `AttemptCount` is preserved across recovery, so a recovered row still reaches `failed` at the existing limit.
- [ ] A duplicate queue message arriving after recovery is a no-op.
- [ ] No second reconciliation timer and no new table are introduced.

## Verification

- [ ] A `dispatched` row older than the threshold with no lease is re-dispatched by the reconciliation timer and processed once.
- [ ] A freshly dispatched row is left alone.

## Risks and boundaries

- **Azure**: no write. Read-only checks of the intake queue and Application Insights are permitted with no per-target approval (`docs/runbook.md` § Live-operation approval matrix; mirrored in `docs/desktop/11-azure-disposition/README.md`). The local run uses Azurite under **L-02**; asking for an Azure test resource is out of bounds (ADR-0014 stands).
- **Scope boundary**: may touch `src/Pegasus.Core/Intake/DurableIntake.cs`, `src/Pegasus.Infrastructure/Persistence/EfIntakeWorkStore.cs`, `tests/Pegasus.IntegrationTests/RecoveryTests.cs`, `docs/frd/frd-02-intake-and-source-identity.md` and `docs/operations.md`. Must **not** touch `src/Pegasus.Web/Pages/**`, `src/Pegasus.Web/Api/**`, any desktop project, or add a database table.
- **Unblocks / blocked by**: this ticket **blocks** [[DSK-05-09]], [[DSK-05-13]] and [[DSK-05-20]] — each renders a state that is dishonest while a `dispatched` row can strand, and [[DSK-07-01]]'s retry-eligibility field is computed over the same rows. It is **blocked by** [[DSK-01-10]], the first one-way upstream sync. [[DSK-08-17]]'s Test/UAT stack is where the tier-6 evidence is produced.
- **Traps**: **upstream ids and fork board ids do not match.** This ticket is board `INTK-002` and it is upstream INTK-003; upstream INTK-002 is the intake duplication chores, board [[INTK-001]]. The join table is `HZN-001/board-conventions.md` § Upstream ids versus board ids — read it, never compute the mapping, and write `upstream <ID>`, or `upstream <ID> (board [[<board-id>]])` where both are meant. Do not reset `AttemptCount` on recovery — that would make a poisoned item immortal. Do not add a second timer; the reconciliation timer already exists. A new table would need a runtime-role `Grant*` migration checked by `scripts/Test-MigrationGrants.ps1`; this ticket must not add one. `IntakeWorkItems` state strings are persisted values — changing one is a migration, not a rename.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the ticket `plan` document.
