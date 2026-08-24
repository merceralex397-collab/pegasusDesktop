# Plan — INTK-004: upstream:INTK-027 · Make policy re-evaluation work after transient staging cleanup

## Governing documents

- `docs/frd/frd-02-intake-and-source-identity.md`

## Chosen approach

Make "Re-evaluate with current policy" either work or refuse honestly. Re-evaluation of a completed receipt today queues work whose staged source blob has already been deleted by design, so it fails with `staged_artifact_integrity_failure` and strands the receipt in `blocked_intake` with `reevaluation_pending`. Either re-stage the source from the retained, hash-verified custody copy before dispatch, or refuse the command before any state change — with an operator-visible reason and no doomed queue entry.

## Routing and constraints

- Future owner: Core/Infrastructure intake work with focused Core and integration tests. The local Test/UAT stack is the verification environment; no production intake or Azure write is authorized.


## Ordered implementation steps

1. Orient. Read the verbatim upstream body above, `docs/frd/frd-02-intake-and-source-identity.md`, and `docs/desktop/03-gateway-api-and-data/endpoint-map.md:87`. Call `get_doc_gates <this ticket id>`, then `take_ticket` with branch `task/upstream-intk-027-reevaluation-after-cleanup` and worktree `../pegasus-worktrees/upstream-intk-027-reevaluation-after-cleanup` from `origin/dev`.
2. Reproduce before repairing. In `files`, trace the full path: `src/Pegasus.Web/Pages/Intake/Details.cshtml.cs:178-190` → `ReevaluateIntake.ExecuteAsync` (`src/Pegasus.Core/Intake/DurableIntake.cs:1084-1103`) → `EfIntakeMutationStore.ScheduleReevaluationAsync` (`src/Pegasus.Infrastructure/Persistence/EfIntakeMutationStore.cs:223-263`) → the Worker's re-processing → `DeleteCompletedStagedAsync` (`src/Pegasus.Core/Intake/DurableIntake.cs:843-848`). Write down the exact line at which the staged blob is assumed to exist.
3. **Verify the upstream body's attempt count against this tree rather than repeating it.** The upstream text says re-evaluation "fails after 2 attempts"; on the fork `IntakeArtifactIntegrityException` is a `TerminalInputFailureCode` (`src/Pegasus.Core/Intake/DurableIntake.cs:875-886`), which by its own comment fails on the first attempt under its own code. Record the observed count in the `plan` and use the observed one.
4. Confirm the retained durable source exists and is reachable: `src/Pegasus.Core/Intake/DownloadIntakeSource.cs:10-55` reads the receipt's `IntakeAssetKind.Source` / `IntakeAssetDisposition.Source` asset by its own `StorageKey` and validates it against both `sourceAsset.ContentHash` and `receipt.SourceHash`. Record in the `plan` that this, not the staging blob, is the hash-verified copy the upstream Direction refers to.
5. Take one of the two directions and record which, with its reason, in the `plan`. **(a) Re-stage**: before `ScheduleReevaluationAsync` requeues the work item, copy the retained source back to the staged storage key the Worker will read, preserving the hash so `IntakeArtifactIntegrityException` cannot fire for a legitimate re-evaluation. **(b) Refuse honestly**: check for the staged (or re-stageable) source *inside the same transaction*, before `workItem.State = "pending"`, and throw a named intake exception that becomes an operator-visible reason — no queue entry, no state change, no `blocked_intake` side effect. Fail-closed stays either way; the silent degradation is the defect.
6. Whichever direction is taken, add the check to `EfIntakeMutationStore.ScheduleReevaluationAsync` **before** the `workItem.State = "pending"` assignment at `:260`, inside the existing `ExecuteAsync` transaction, so a refusal leaves the receipt version and history untouched.
7. If direction (b) is chosen, give the refusal a named exception in `src/Pegasus.Core/Intake/IntakeContracts.cs` and a label in `src/Pegasus.Web/Presentation/OperatorLabels.cs` beside the existing `staged_artifact_integrity_failure` entry at `:332`. Use the approved necessary-copy style — an honest reason, no explanation of internals, and none of the banned operator words (`intake`, `artifact`, `staging`, `blob`).
8. **Re-expressed for the desktop world.** The upstream body describes a Razor button on `/Received/{id}`, which [[DSK-05-26]]'s cut list deletes. State the requirement against what replaces it and record it in the `plan`: `POST /api/v1/received/{id}/reevaluate` (endpoint-map `:87`) must return the refusal as a `validation`-shaped problem carrying the named reason, **not** a 200 followed by a silent `blocked_intake`; and [[DSK-05-09]]'s re-evaluate command must be able to disable itself or report the refusal without a second copy of the rule. Add that as a note to the `plan` for [[DSK-03-10]] and [[DSK-05-09]] to consume — do not edit those tickets.
9. Add tests: a completed receipt whose staged source is gone either completes under the current policy versions (draft re-resolved, history appended) or is refused before any state change; a receipt is never left in `blocked_intake` by a re-evaluation that could not run; a receipt currently leased for processing is still refused by the existing `:252-258` guard; and a replayed `operationKey` returns the same result.
10. Verify on the local stack under **L-02**: process a receipt to completion against Azurite so the staged blob is deleted, then re-evaluate it and observe the chosen behaviour end to end. Confirm the production symptom read-only if the operator makes it available — the `transient-intake` container's `staging/` prefix is empty, which is a **read** and needs no approval.
11. Update `docs/frd/frd-02-intake-and-source-identity.md` with the re-evaluation source rule, run the simplification pass over the branch diff, record it under a dated `## Simplification pass` heading in the ticket `plan`, then open the PR into `dev`.

## Acceptance conditions

- [ ] Re-evaluating a completed receipt either completes under the current policy versions with the draft re-resolved and history appended, or is refused with an honest operator-visible reason **before any state change**.
- [ ] No receipt is ever left in `blocked_intake` with `reevaluation_pending` by a re-evaluation that could not run.
- [ ] The check happens inside the existing `ScheduleReevaluationAsync` transaction, before `State = "pending"`; a refusal leaves the receipt version and its history untouched.
- [ ] `POST /api/v1/received/{id}/reevaluate` surfaces the refusal as a problem response carrying the named reason, not a 200 followed by a failed background state.
- [ ] Fail-closed behaviour is preserved: no path silently degrades, and `IntakeArtifactIntegrityException` still fires for a genuinely corrupt source.
- [ ] The refusal copy contains no banned operator word and no internal detail.

## Verification

- [ ] `dotnet build --configuration Release` — expected: clean.
- [ ] `dotnet test ./tests/Pegasus.Core.Tests/Pegasus.Core.Tests.csproj --configuration Release` — expected: the re-evaluation precondition facts pass.
- [ ] `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --filter "Category!=Corpus&Category!=Browser"` — expected: a completed receipt re-evaluated after staged cleanup reaches the chosen outcome, and no receipt reaches `blocked_intake` by that route.
- [ ] Local stack run (L-02) — expected: process to completion against Azurite, re-evaluate, observe either a successful re-resolution or an honest refusal with the receipt untouched; command log captured as `proof`.

## Risks and boundaries

- **Azure**: no write. Reading the `transient-intake` container to confirm the empty `staging/` prefix is a read and is fully permitted with no per-target approval (`docs/runbook.md` § Live-operation approval matrix; mirrored in `docs/desktop/11-azure-disposition/README.md`). Re-staging a blob **in production** would be a write and is explicitly **not** part of this ticket — the code change is; any live remediation of already-stranded receipts is a separate approved operation.
- **Scope boundary**: may touch `src/Pegasus.Core/Intake/DurableIntake.cs`, `src/Pegasus.Core/Intake/IntakeContracts.cs`, `src/Pegasus.Infrastructure/Persistence/EfIntakeMutationStore.cs`, `src/Pegasus.Web/Presentation/OperatorLabels.cs`, the three test projects and the named documents. Must **not** touch `src/Pegasus.Web/Api/**` (that is [[DSK-03-10]]'s), any desktop project, or `src/Pegasus.Web/Pages/Intake/**` beyond reading it.
- **Unblocks / blocked by**: this ticket **blocks** [[DSK-05-09]] and [[DSK-03-10]] — they publish and render a command that cannot succeed today, and both are forbidden by their own scope boundaries from repairing it. It is **blocked by** [[DSK-01-10]], the first one-way upstream sync. [[DSK-05-23]] and [[DSK-03-16]] carry the operator label vocabulary any new refusal reason joins.
- **Traps**: **upstream ids and fork board ids do not match.** This ticket is board `INTK-004` and it is upstream INTK-027; upstream INTK-004 is a different ticket again — the received-intake Case-link and label defect absorbed into [[DSK-05-20]] and [[DSK-05-23]] — and it has **no fork ticket**, so never read a bare `INTK-004` as it. The join table is `HZN-001/board-conventions.md` § Upstream ids versus board ids: read it, never compute the mapping, and write `upstream <ID>`, or `upstream <ID> (board [[<board-id>]])` where both are meant. The fix is in `Pegasus.Infrastructure`, which [[DSK-05-09]] may not touch and [[DSK-03-10]] may not touch — that is precisely why this ticket exists; do not let it drift into either. Do not weaken `IntakeArtifactIntegrityException`: a genuinely corrupt source must still fail closed. Do not delete or change `DeleteCompletedStagedAsync` — deleting the staged copy on completion is deliberate and the retained source is the durable one. `IntakeWorkItems` state strings are persisted values.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the ticket `plan` document.
