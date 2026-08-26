# Post-implementation report — DOCS-001

## Scope delivered

- Implemented the explicit staff-triggered report-draft path in Core.
- Added one shared fail-closed readiness owner covering the selected accepted repair specification, source provenance, required assessment inputs, ordered image custody, and renderer inputs.
- Added durable immutable report versions with deterministic payload/logical identity, exact replay, correction lineage, retry/backoff state, and terminal failure state.
- Persisted assessment and fee-note artifacts through existing generated case-document custody, including pending metadata recovery and terminal cleanup.
- Added infrastructure entities, configuration, migration, runtime grants, and model snapshot.
- Added gateway/report-state projection for Pending/Rendering/Generated/Failed and retry/version visibility in the existing web report surface.
- Updated FRD-11 and the open decision record. No desktop/XAML, Worker, approval, sending, cloud, deployment, or upstream-sync changes were made.

## Evidence and validation

- `dotnet build ./Pegasus.slnx --configuration Release --no-restore -nr:false -p:UseSharedCompilation=false` — passed with 0 warnings and 0 errors.
- Core report-focused tests — 26/26 passed.
- Focused integration/web/renderer/migration tests — 13/13 passed.
- Full Core suite — 930/930 passed.
- Architecture suite — 101/101 passed.
- `scripts/Test-MigrationGrants.ps1` — 68/68 migration files passed.
- Full local integration command:
  `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --no-build --no-restore --filter "Category!=Corpus&Category!=Browser"`
  completed with 883 passed, 2 skipped, and 1 failed in 12m 8s.
- The sole full-suite failure is unrelated to DOCS-001:
  `GroupedImageIntakeConcurrencyTests.ConcurrentGroupMembersNeverSplitAcrossRepeatedRuns`
  failed with SQL Server deadlock 1205 at `EfIntakeWorkStore.CompleteProcessingAsync` line 338, reached from `DurableIntake.ProcessQueuedIntake`. The exact test was rerun independently with detailed logging and reproduced in 46s. No DOCS-001 report path appears in that stack.
- `corpus/` remained local and immutable; no cloud, deployment, mailbox, Box, credential, or upstream operation was performed.

## Simplification pass

The pass reused the existing Core readiness policy/ports, existing document-custody path, existing renderer, and one focused report store. No generic job framework, second readiness owner, rate-card calculation, compatibility path, or speculative desktop implementation was added. The independent findings applied before this report covered selected-estimate identity/source/version validation, UTC-midnight replay, canonical snapshot rendering, pending metadata recovery, terminal cleanup, explicit state projection, and honest validation recording.

## Review and delivery state

- Fresh independent review of the final diff: pending.
- PR and merge: pending.
- Kanmer verification proof: pending and will be written only after merge on the required target branch.
- Known blocker: the unrelated full integration deadlock above prevents claiming a green repository-wide integration suite. DOCS-001 targeted validation is green; the intake test must be owned and resolved separately or explicitly accepted by its owning ticket before the full-stack checkbox can be closed.
