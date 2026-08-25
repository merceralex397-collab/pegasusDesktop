# Post-implementation report — INTK-004

## Outcome

Completed receipts can now be re-evaluated from their retained source. The existing artifact-store port reads and re-stages exactly one hash-verified source before the existing transaction queues work. Missing, corrupt, or ambiguous retained content fails closed before any receipt/work/history mutation. Active leases still refuse the command first, and replayed operation keys do not re-stage or append history again.

## Evidence by acceptance condition

- Re-evaluation success and replay: `IntakeReevaluationPersistenceTests.ReevaluateRestagesRetainedSourceBeforeQueueingAndReplaysWithoutDuplicateStage` passed. It proves one `StageAsync` call, pending queue state, version increment, and one mutation-history row across first call plus replay.
- Missing/corrupt source refusal and atomicity: `IntakeReevaluationPersistenceTests.ReevaluateWithMissingOrCorruptRetainedSourceLeavesReceiptWorkAndHistoryUntouched` passed for both theory cases. It proves the existing `IntakeArtifactIntegrityException`, unchanged version, completed work item, zero history rows, and zero staging calls.
- Active lease guard: `IntakeReevaluationPersistenceTests.ReevaluateWithActiveLeaseDoesNotRestageOrMutate` passed and proves no staging or mutation while a live processing lease exists.
- Existing association consumers: all 7 `CaseMatchIntegrationTests` passed after supplying the new required artifact-store dependency.
- FRD and carry-over ownership: updated `docs/frd/frd-02-intake-and-source-identity.md`, `docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md`, `docs/desktop/05-implementation-and-migration/vertical-slices.md`, and `docs/desktop/06-ui-design/screen-specs.md` in this repository only.

## Commands and results

- `dotnet build src/Pegasus.Infrastructure/Pegasus.Infrastructure.csproj --configuration Release` — passed, 0 warnings, 0 errors.
- `dotnet test tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --filter "FullyQualifiedName~IntakeReevaluationPersistenceTests" --no-restore` — passed, 4 tests.
- `dotnet test tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --filter "FullyQualifiedName~CaseMatchIntegrationTests" --no-restore` — passed, 7 tests.
- `dotnet build --configuration Release` — passed, 0 warnings, 0 errors.
- `dotnet test tests/Pegasus.Core.Tests/Pegasus.Core.Tests.csproj --configuration Release --no-build` — passed, 920 tests.
- `dotnet test tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --no-build --filter "Category!=Corpus&Category!=Browser"` — not a clean run. An unrelated existing `GroupedImageIntakeConcurrencyTests.ConcurrentGroupMembersNeverSplitAcrossRepeatedRuns` case failed with a SQL Server deadlock in `EfIntakeWorkStore.CompleteProcessingAsync` at line 338; the runner then produced no further output and was stopped. No code in that path was changed by this ticket. This is not claimed as passing evidence.
- `git diff --check` — passed before final document update; must be rerun after the final working-tree review.

## Scope and authority

All changes remain in the current repository and configured `pegasusDesktop` remote. No upstream remote was added or synchronized. No Azure, deployment, credential, mailbox, or external storage write was performed. The future API problem mapping remains with GWY-010; no API or desktop code was edited.

## Review update — 2026-08-25

Independent reviewer Turing (`01a03a6b-b65e-7d71-bf78-1740fde16235`) initially identified the dispatching-lease gap, ambiguous-source/stage-failure coverage gap, and the need to verify the new test file in the committed diff. Those findings were addressed before merge:

- the guard covers both `dispatching` and `processing` future leases;
- the focused suite now passes 7 tests, including both lease states, ambiguous source, and StageAsync failure;
- the new test file is part of the working-tree change set and will be included in the commit;
- the stage-before-commit/idempotent external-store disposition is recorded in the plan.

The reviewer found no API, desktop, packaging, cloud-placement, accessibility, or unrelated architecture concern. Exact-head CI has not yet been run because no PR exists until this reviewed local commit is created.
