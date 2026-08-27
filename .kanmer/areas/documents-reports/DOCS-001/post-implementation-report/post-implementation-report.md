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
- Focused integration/web/renderer/migration tests — 23/23 passed.
- Full Core suite — 930/930 passed.
- Architecture suite — 101/101 passed.
- `scripts/Test-MigrationGrants.ps1` — 69/69 migration files passed.
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

- Fresh independent review of the final diff: PASS by Bernoulli; no actionable findings.
- PR and merge: pending.
- Kanmer verification proof: pending and will be written only after merge on the required target branch.
- Known blocker: the unrelated full integration deadlock above prevents claiming a green repository-wide integration suite. DOCS-001 targeted validation is green; the intake test must be owned and resolved separately or explicitly accepted by its owning ticket before the full-stack checkbox can be closed.


## Final review checkpoint — 2026-08-26

Bernoulli independently reviewed the final diff and returned PASS with no actionable findings. The review verified exact report-version retry binding, canonical retry payloads, shared operator labels and office-time formatting, removal of duplicate explanatory/empty-state copy, and the report store's idempotency, concurrency, template, recovery, terminal, and grant controls.

## Exact-head CI blocker — 2026-08-26

PR #14 exact head `bb263b20a49af1375d2823ce5c4a803dd66bdc39` was validated by run `32959758190`. Browser passed 49/49; unit, infrastructure, changes, documentation, local-development-scripts, reference-data, SQL shards 1 and 3, and SQL coverage passed. SQL shard 2 failed, and the authorized failed-job rerun also failed, both on `GroupedImageIntakeConcurrencyTests.ConcurrentGroupMembersNeverSplitAcrossRepeatedRuns`: SQL Server deadlock 1205 at `EfIntakeWorkStore.CompleteProcessingAsync` line 338, through `DurableIntake.ProcessQueuedIntake`. This is the same unrelated intake concurrency failure reproduced in the local suite; no DOCS-001 report path appears in either stack. The PR remains unmerged because repository policy requires green required CI. The intake deadlock must be resolved by its owning work or explicitly accepted by the owning ticket before DOCS-001 can merge.

## Remediation checkpoint — 2026-08-27

The prior review was superseded by Plato's independent review of exact head `9beae42dcc787f8d1f199866b8a52aed22a3bade`, which initially returned FAIL. The review identified (1) missing actor-aware authorization before stored report-version retry, (2) an empty report-draft panel when neither versions nor generation is available, and (3) no UI retry path for an expired Rendering lease. The branch now re-authorizes the target case with `IGetCase`, hides the empty panel, projects lease expiry, and exposes expired-lease retry. New web coverage proves cross-case denial, empty-panel omission, and recovery from the stored canonical payload. Focused post-fix validation passed 35/35.

The FeeNote characterization assertion was also corrected after exact-head CI run `33112687249` exposed the missing expectation. Fresh exact-head CI for `9beae42d` is in progress; the earlier run is not used as merge evidence. The ticket remains unmerged and unverified until fresh CI and independent review pass.

## Review remediation follow-up — 2026-08-27

Plato's fresh review of exact head `7039bdf7` returned FAIL on the browser test's invalid absent-locator text lookup. That test was corrected and locally passed 1/1; the correction is pushed as exact head `8f60fc47`. The prior CI run `33115676879` remains non-green because it tested `7039bdf7`; a new exact-head CI run and independent review are required for `8f60fc47` before merge. No cloud, deployment, upstream, or corpus operation was performed.

## Exact-head CI checkpoint — 2026-08-27

Run `33116768838` passed fully for exact head `8f60fc47f97f9e6ca18078a3341f6b0795dcc77d`, including browser, all SQL integration shards, and coverage. This replaces the superseded non-green run. Fresh independent review remains the final pre-merge gate.

## Independent review follow-up — 2026-08-27

Curie's fresh exact-head review returned **FAIL**. The review found raw exception messages could be persisted and rendered directly from the report-version failure field, exposing internal renderer/database/path details; this will be replaced by stable operator wording with raw detail retained only as structured diagnostics. It also found that the operator-recorded multiple-estimate decision was not implemented: the current page exposes one accepted estimate and a disabled placeholder rather than separately selectable accepted estimates with one Generate action per estimate. Curie's non-blocking warning is that web fake fixtures use direct costs rather than the production accepted-basis conversion; the production persistence coverage and an explicit test disposition will be recorded after remediation. Merge remains prohibited until both blocking findings have fresh review and exact-head CI evidence.

## Final remediation checkpoint — 2026-08-27

Curie's blocking findings were addressed in the branch. Report failures now use stable operator language in persistence and the web projection, while the original failure reason is restricted to structured diagnostics. Accepted estimates are independently accepted/listed, shown as source-attributed selectable tabs, and the selected specification ID is passed to the existing report-generation/retry path. A production persistence test proves two independent accepted rows remain separate; the web fixture cost values are intentionally presentation-only, with accepted-basis conversion retained in the production projection/persistence coverage rather than duplicated in markup tests.

The remediation remained proportionate: existing stores, report projection, custody and operator-label conventions were reused; no rate-card formula, compatibility path, generic job framework, or desktop/cloud/upstream work was added. Local evidence is green for the Release solution build (0 warnings/errors), focused report/import/persistence integration tests (12/12), Core tests (938/938), architecture tests (111/111), and migration grants (70/70). Full integration validation is running separately. Fresh independent review and exact-head CI for the resulting commit remain required before merge.

## Final local correction — 2026-08-27

A full integration attempt exposed and corrected one obsolete single-current-estimate assertion; it was not a production failure. The migration catalog test was updated for `20260827214843_AllowMultipleAcceptedRepairSpecifications`. The affected integration set then passed 14/14 and the Release solution build passed with 0 warnings and 0 errors. The final pushed head is `fb13e943`; fresh independent review and exact-head CI remain required.

## Final independent review — 2026-08-27

Curie reviewed exact pushed head `fb13e94318116c6f39a5941278313c67ad1e324b` against `origin/dev` and found the prior implementation blockers fixed. The only merge blocker in the review is exact-head CI run `33121490469`, which was still running. Curie's low-risk note that the web tab fixture does not POST through a projection fake that records selected identity is dispositioned: the web test proves selected identity in rendered Generate/retry markup, and production projection/persistence tests prove selected accepted-basis/source composition.

## Exact-head CI completion — 2026-08-27

Run 33121490469 passed fully for exact head fb13e94318116c6f39a5941278313c67ad1e324b: changes, documentation, local-development-scripts, reference-data, infrastructure, unit, browser, SQL integration shards 1/2/3, and SQL integration coverage all passed. Curie's fresh independent review passed the implementation; its low-risk web-fixture limitation is dispositioned in the plan. PR #14 is ready for the reviewed merge into dev; main promotion, proof, and Kanmer closeout remain pending.
