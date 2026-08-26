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

## 2026-08-25 research decision

The repository facts above were re-checked on the implementation branch. FRD-02 already states the surrounding durable intake behavior: acknowledgement means durable receipt/dispatch, the Worker is the sole processing owner, it dispatches pending work, claims queue deliveries idempotently, recovers expired leases, and duplicate delivery must not duplicate evaluation or downstream side effects (FRD-02 Source occurrence and dispatch identity). The implementation therefore adds one sentence to that same paragraph for unleased dispatched rows recovered by the existing reconciliation sweep; no new FRD section is needed.

The threshold is one hour since DueAtUtc. Source values are visibilityTimeout=00:05:00 in src/Pegasus.Worker/host.json:16, and the omitted timeToLive on AzureQueueIntakeWorkQueue.SendMessageAsync uses Azure Queue Storage's documented seven-day default. One hour is safely above the visibility timeout and safely below the message TTL. The existing ClaimProcessingAsync guard is the duplicate-safety basis: completed/failed rows return null; a live processing lease returns null; only dispatching|dispatched|processing without that live lease can claim.

The branch implementation keeps one reconciliation timer, extends the existing recovery store contract with the explicit age, includes only unleased dispatched rows older than the cutoff, preserves AttemptCount, clears lease fields, and retains a state/attempt/lease/due optimistic-concurrency predicate.

## 2026-08-25 implementation and simplification pass

Implementation completed on branch `intk-002-recover-dispatched-work` in `.worktrees/intk-002`.

- `IIntakeWorkStore.RecoverExpiredLeasesAsync` now receives the chosen one-hour dispatched recovery age from the existing `ReconcileStagedArtifacts` timer.
- Infrastructure selects only unleased `dispatched` rows whose `DueAtUtc` is at least one hour old, returns them to `pending`, clears lease/failure fields, preserves `AttemptCount`, and retains state/attempt/lease/due optimistic concurrency checks.
- Existing leased `dispatching`/ `processing` recovery remains unchanged in behavior. No second timer, table, migration, Web/API route, desktop project, or Azure write was added.
- `RecoveryTests` covers stale dispatched re-dispatch and one-time processing, fresh dispatched preservation, and a late duplicate queue message no-op. Existing recovery/lease/retry tests remain green.
- FRD-02 and `docs/operations.md` now state the bounded recovery behavior and its one-hour threshold against the five-minute visibility timeout and seven-day default Azure Queue TTL.

Simplification review over the branch diff:

- Reused the existing reconciliation timer, store contract, dispatch path, processor, and test harness; no new abstraction or timer was introduced.
- Made the recovery candidate predicate explicitly parenthesized so the leased-state and unleased-dispatched branches are readable without changing behavior.
- Reused the existing `StageAndDispatchAsync` setup and added one test-only `CreateReconciler` helper for the three new cases; no production helper or compatibility path was added.
- Kept the existing method name `RecoverExpiredLeasesAsync` because it is the established port used by the reconciliation timer and still owns both lease and bounded dispatched recovery; renaming it would expand the diff without reducing duplication.
- No further behavior-preserving simplification was identified. The remaining repeated assertions are deliberately acceptance-specific (state, attempt count, lease clearing, evaluation revision, and receipt cardinality).

Validation completed:

- `dotnet restore Pegasus.slnx --locked-mode` — passed before implementation.
- `dotnet build tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --no-restore --configuration Debug --nologo` — passed.
- `dotnet test tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --no-restore --configuration Debug --filter "FullyQualifiedName~RecoveryTests" --logger "console;verbosity=minimal"` — 32 passed, 0 failed, 0 skipped.

The repository's scripted L-02 local stack remains unavailable for this ticket's required caller proof: `Invoke-LocalDevelopment.ps1 -Action Start` fails before Web/Functions readiness in the existing launcher at line 1482 because `Process.Path` is empty for the PowerShell-owned launcher. The failure is recorded in AUTO-002's evidence; this ticket does not modify that launcher outside its scope. Integration evidence above is LocalDB-backed and does not claim Azurite/Functions-host execution.

## Independent review — 2026-08-25

The independent `pegasus-desktop-reviewer` review of commit `65a10183` passed the static implementation, threshold, scope, cloud-placement, and simplification lenses, but failed the delivery gate for four findings: missing L-02 caller proof, missing stale-version/concurrent-claim coverage, missing upstream-to-board carry-over annotation, and no PR/CI because GitHub rejected PR creation with `GraphQL: must be a collaborator (createPullRequest)`.

Follow-up completed on this branch:

- Added `ConcurrentRecoveryClaimsOnlyOneStaleDispatchedRow`, which runs two store recoveries against the same stale row and asserts exactly one update wins, the final state is pending, attempt count is unchanged, and lease fields are clear.
- Extended `QueuedStatusProjectsAnActiveProcessingLease` to assert a duplicate while a live processing lease exists returns `NoOp`.
- Annotated upstream `INTK-003` in `docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md` with fork board `[[INTK-002]]`.

The L-02 proof and PR/CI findings remain open. The reviewer correctly notes that the current LocalDB tests do not prove Azurite, the Functions timer, queue loss, restart, or real duplicate delivery. No claim of those tiers is made.

## Acceptance and review disposition — 2026-08-26

- The exact PR head `56fb9b05c9609e08bf14a2e26f71e6d9b8ed5e1f` now has completed-success CI run `33006548735`; the required repository checks are green.
- The independent review passed the static implementation, scope, threshold, and simplification lenses. Its delivery gate remains open because the required L-02 Azurite/Functions-host caller journey has not been demonstrated.
- The scripted local-stack attempt still fails before readiness because the existing PowerShell launcher observes an empty `Process.Path`. A direct exact-worktree attempt reached Functions metadata discovery only after a normal build, then failed in the Worker language process because `EfStaffAccountAdministration` requires unregistered `UserManager<PegasusIdentityUser>` services. This is an existing Worker composition defect outside this ticket's allowed file scope.
- The L-02 acceptance item remains intentionally unresolved. LocalDB recovery tests and green CI do not substitute for queue loss, timer redispatch, and one-time processing proof against Azurite and the Functions host.
- Non-blocking review suggestions (direct active-lease untouched coverage and nonzero AttemptCount preservation) are recorded for follow-up; the existing tests already cover the core optimistic concurrency behavior and zero-attempt preservation required by this ticket.

Next action: resolve the Worker/local-stack composition failure under its owning ticket or a narrowly scoped follow-up, then rerun the exact L-02 journey against this branch before merge.
