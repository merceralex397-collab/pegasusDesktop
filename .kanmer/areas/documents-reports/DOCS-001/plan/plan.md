# Plan — DOCS-001: upstream:DOCS-001 · Trigger report generation from complete accepted assessments and retain immutable report references

## Governing documents

- `docs/frd/frd-11-reports-correspondence-and-reviewed-proposals.md`
- `docs/adr/0025-integrate-renderer-and-extractor-into-the-application.md`

## Chosen approach

Deliver the Core-owned **front half** of report generation that the 208 seeded conversion tickets have no owner for: a durable report request/version aggregate keyed on the case, the accepted assessment snapshot and a deterministic payload hash; a readiness gate that fails closed on missing, unaccepted or ambiguous required data on **both** the draft and the register path; idempotent generation, so one accepted input plus template version yields exactly one report version and a retry reconciles to it rather than duplicating it; and append-only correction lineage that preserves every earlier artifact with its provenance, hashes and custody state.

## Routing and constraints

- Future owner follows the ticket’s stated project boundary and repository task workflow. Reuse existing Core policy/ports before adding any abstraction.


## Ordered implementation steps

1. Orient. Read this body in full including the verbatim upstream ticket, then the three upstream pipeline documents copied onto this ticket (`research`, `files`, `open-questions`) — they are the requirement, not a summary of it. Read `docs/frd/frd-11-reports-correspondence-and-reviewed-proposals.md` and `docs/adr/0025-integrate-renderer-and-extractor-into-the-application.md`, then read [[DSK-07-16]] and [[DSK-03-14]] so you do not rebuild the registration endpoint. Call `get_doc_gates <this ticket id>`, then `take_ticket` on branch `task/upstream-docs-001-report-aggregate`.
2. **Re-scope against this repository, with no upstream synchronization.** The fork is the only release source under D-001 and the operator has prohibited upstream fetch, merge, and sync operations. Re-read the current local contracts on this branch. TICK-093's versioned repair-specification work is present; no dedicated TICK-092 accepted report-input snapshot or TICK-094 Engineer-decision component is present. DOCS-001 therefore owns the minimum accepted snapshot and one deterministic payload hash needed by this report aggregate, without copying a second business-policy owner or waiting for upstream work.

3. **Operator decision — 2026-08-26.** Report generation is initiated by the staff `Generate report draft` command. There is no automatic trigger and no hybrid. An exact replay returns or reconciles to the existing report version; it does not force a duplicate. A changed accepted payload or template identity creates a successor. Repair costs come from an external repair-estimate connection or an imported repair-estimate document. Multiple estimates remain separate tabs, and each tab's Generate action selects that estimate and its provenance; no internal rate-card formula or cross-estimate precedence is added. A missing or ambiguous selected estimate remains a named readiness blocker. This decision is recorded in `open-questions` and mirrored into board ticket [[FEAT-042]] (plan handle DSK-07-16).

4. Define the readiness contract in `plan` over the **one existing owner** — `AssessmentReportProjection.Project` and `GenerateCaseAssessmentReportDraft` in `src/Pegasus.Core/Reports/AssessmentReportProjection.cs`. Enumerate the renderer inputs the copied research lists as *not* covered by `AssessmentPolicy.EvaluateReadiness` (principal/report addressee and external reference, incident date, inspection mode presence, selected ordered current images with content bytes and custody, canonical raw cost components and display sections, source identities/versions/hashes, the accepted engineer tuple) and add them to that owner. Never write a second readiness implementation; `AssessmentReadinessItem.Requirement` and `WhyOutstanding` stay the vocabulary.
5. Add the durable report aggregate in `src/Pegasus.Core/Reports/` as its own focused file(s): report request and report version states, the typed assessment-plus-fee-note artifact pair with identity and SHA-256, the deterministic logical key (case + active assessment family + accepted payload hash + template version), retry and terminal-failure policy, and predecessor/successor correction lineage. Reuse the conventions in `src/Pegasus.Core/Custody/ExternalWorkProcessing.cs` — do **not** overload `ExternalWorkItem` and do not invent a generic job framework for a single caller. Done looks like: `dotnet build ./Pegasus.slnx --configuration Release` succeeds with the new Core types and no Infrastructure reference from Core.
6. Persist it in `src/Pegasus.Infrastructure/Persistence/`: new report entities and a model configuration beside `CaseWorkflowEntities.cs` / `CaseWorkflowModelConfiguration.cs`, a migration under `src/Pegasus.Infrastructure/Persistence/Migrations/`, and the regenerated `PegasusDbContextModelSnapshot.cs`. The logical key gets a unique index so two callers cannot create two reports for one accepted input. Prior versions are never overwritten. Add the runtime-role grant in the same migration — `pwsh ./scripts/Test-MigrationGrants.ps1` must pass, and discovering this in CI instead is the trap upstream PLAT-035 records.
7. Attach generation to the **committed** accepted-snapshot boundary, not to the Razor page and not to the renderer adapter: enqueue from the transaction in `EfCaseAssessmentStore` that already persists under serializable isolation with the expected case version, edit lease and operation-key replay. Rendering itself runs *after* the durable request exists, under lease and retry protection, because the renderer cannot share the source-data transaction. The durable request is created by the operator command after the one Core readiness owner accepts the selected estimate and accepted assessment snapshot; the renderer runs only after that durable request exists under lease/retry protection.
8. Store both artifacts through the existing content path — `IDocumentContentStore` with `DocumentSource.Generated`, `DocumentSemanticRole.EngineerReport` for the assessment PDF — so a generated report is a normal case document version with custody state. Do **not** force system-generated work through `AddCaseDocumentCommand`'s staff edit lease and expected-case-version requirement; give generation its own system-owned atomic result boundary, and name the fee note's semantic role rather than leaving it untyped.
9. **Re-expressed for the desktop.** Upstream item 11 of the research puts generation state, failures, retry and artifact download on `src/Pegasus.Web/Pages/Cases/Assessment/Index.cshtml(.cs)`. That page is deleted by the conversion cut list, so keep the requirement and move it: expose Pending/Rendering/Generated/Failed/Retry, the actionable failure reason, and the version list as a **gateway projection** on the case reports section that [[DSK-07-16]] step 8 and [[DSK-03-14]] already own the route for, rendered by the desktop Reports tab under the existing AutomationIds `Case.Reports.Generate`, `Case.Reports.Preview`, `Case.Reports.Send`. The desktop renders named server states; it computes none of them. Stuck or failed generation appears in the Operations surface of [[DSK-05-20]] / [[DSK-07-04]] rather than inventing a second operational convention.
10. Keep the three finality boundaries apart, as FRD-11 requires: generation is a draft; approval is a human act bound to a stored artifact identity and hash; sending is proved only by retained exact Sent evidence. A generated version is never rendered as approved, issued, sent or received. Version-specific approval and Sent association are **not** built here — they belong to the imported `upstream:TICK-208`, which sequences after this ticket.
11. Test in the projects that exist on the fork. `tests/Pegasus.Core.Tests` — readiness fails closed on each missing or unaccepted input with the named requirement; the logical key is deterministic; a changed accepted payload or template yields a successor version; correction never mutates a predecessor. `tests/Pegasus.IntegrationTests` — following `CaseWorkflowPersistenceTests.cs`, `DocumentCustodyDurabilityTests.cs` and `CustodyOutboxIntegrationTests.cs`: exact replay returns the same report and stores nothing new, two concurrent callers produce one version, a crash between database commit and content write leaves no half-report, and the migration preserves existing approvals.
12. Verify on the local stack only (**L-02**) — no Azure and no Box write. Then run the simplification pass over this branch diff, record it under a dated `## Simplification pass` heading in this ticket's `plan` document, and open the PR into `dev`.

## Acceptance conditions

- [ ] An incomplete, unaccepted or ambiguous assessment cannot produce a report on **either** the draft or the register path, and the refusal names each outstanding requirement rather than collapsing into one generic message.
- [ ] One accepted input plus template version produces exactly one report version; an exact replay returns or reconciles to it and creates no second version.
- [ ] The case retains an immutable report version identity, hash, template/payload versions, provenance and custody state for the assessment and fee-note artifacts as a fixed pair.
- [ ] A correction or addendum appends a successor version and leaves every earlier artifact, its provenance and its approval untouched.
- [ ] Generation is never rendered or recorded as approval, issue, sending or external receipt.
- [ ] Readiness has exactly one owner in `src/Pegasus.Core`; no second required-field list exists in Web, Infrastructure or the desktop.
- [ ] The new tables carry their runtime-role grants and `scripts/Test-MigrationGrants.ps1` passes.
- [ ] The trigger question of step 3 is answered by the operator and recorded before the trigger is implemented.

## Verification

- [ ] A complete accepted assessment produces a deterministic report through the composed application path.
- [ ] Incomplete or ambiguous assessment data cannot render.
- [ ] The case retains immutable reference/version/hash/provenance and idempotent retry behavior.
- [ ] Report generation does not count as approval, sending, or external receipt.

## Risks and boundaries

- **Azure**: no write. Verification is the local DevelopmentOffline stack under **L-02**; no Azure test resource may be requested.
- **Scope boundary**: may touch `src/Pegasus.Core/Reports/**`, `src/Pegasus.Core/Assessment/**` (readiness inputs only), `src/Pegasus.Infrastructure/Persistence/**` (new report entities, configuration, migration, snapshot), `src/Pegasus.Infrastructure/DependencyInjection.cs`, `src/Pegasus.Web/Program.cs` composition, `tests/Pegasus.Core.Tests`, `tests/Pegasus.IntegrationTests`, `tests/Pegasus.ArchitectureTests`. Must **not** add a second readiness rule, must **not** build the `/api/v1` register endpoint or the desktop Reports UI (that is [[DSK-07-16]]), must **not** build version-specific Sent-evidence association (that is the imported `upstream:TICK-208`), must **not** touch `src/Pegasus.Worker`, and must **not** create a standalone renderer host, MCP tool or second editable report-data record.
- **Blocks / blocked by**: this ticket **blocks** [[DSK-07-16]] (its report record and idempotency have no aggregate without it), [[DSK-03-14]] (the readiness summary it returns has nothing behind it) and [[DSK-05-18]] (the slice cannot sign off on a report path that can render a not-ready assessment). It **is blocked by** [[DSK-01-10]]'s upstream sync only to the extent that step 2 needs the merged state of TICK-092/093/094 to be known; it is not blocked by their completion. It sequences **before** the imported `upstream:TICK-208`.
- **Traps**: a new table without a `Grant*` migration fails `scripts/Test-MigrationGrants.ps1` in CI (upstream PLAT-035); reusing `CaseReportApproval` as the report record collapses generation into approval and loses the fee-note pair; rendering inside the assessment transaction cannot work because the renderer is an out-of-transaction effect; a random operation key alone is not idempotency — two callers with different keys and the same accepted input must not create two reports; and the human-readable reference on a generated report is the existing Case/PO number (`OurReference`) by operator decision of 2026-08-19 recorded in the copied `open-questions` — do not create a second outward report-number sequence.
- **Open question carried from upstream**: the copied `open-questions` document ties the implementation plan to merged TICK-093, TICK-094 and TICK-092. Step 2 replaces "wait" with "re-derive and, if frozen, own the minimum snapshot"; record that deviation explicitly in `plan` rather than silently ignoring the upstream instruction.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.

## Current fork re-scope — 2026-08-26

The fork is the only implementation source. No upstream synchronization is permitted. The local fork contains TICK-093's versioned repair-specification work, but no dedicated TICK-092 snapshot/hash contract or TICK-094 Engineer-decision component. DOCS-001 owns the minimum accepted report-input snapshot and deterministic payload hash, while continuing to consume the existing Core readiness owner and TICK-093 repair-specification contract.

## Product decision record — 2026-08-26

The operator selected an explicit `Generate report draft` command. Automatic generation and hybrid triggering are excluded. The selected repair estimate is an explicit tab-level input. Estimates may originate from a connected repair-estimate system or an imported repair-estimate document; multiple estimates remain separately selectable and source-attributed. No internal rate-card derivation or precedence rule is added. Exact replay is idempotent; changed accepted input or template creates an immutable successor. Missing or ambiguous selected costs remain fail-closed.

## Implementation readiness

The prior trigger contradiction and rate-card blocker are resolved by the decision record above. Implementation must still prove the selected estimate's source/provenance is captured in the accepted snapshot and must not fabricate costs or silently choose among estimates. The ticket remains bounded to report generation, persistence, custody, and readiness; approval, sending, Sent-evidence correction, desktop UI, cloud writes, deployments, and upstream synchronization remain out of scope.

## Simplification pass

_To be completed against the branch diff before opening the PR._


## Implementation result — 2026-08-26

- The explicit staff `Generate report draft` command is the only trigger. `GenerateCaseAssessmentReportDraft` now uses the shared Core readiness service, reserves a durable report version, renders outside the database transaction, and returns an exact replay from retained artifacts.
- The accepted repair-specification version, source provenance, case version, payload hash, template version, retry attempt/backoff state, and predecessor lineage are retained. A selected estimate must be accepted, source-attributed, and versioned; no internal rate-card value is derived.
- `EfAssessmentReportStore` uses a unique logical key and reconciles concurrent insert/deadlock races. It commits pending generated-document metadata before content writes, then confirms custody only after both PDF bytes are verified. A later lease reconciles the same persisted artifact identities.
- The generated assessment and fee note use normal generated case-document custody with distinct semantic roles. No approval, issue, send, receipt, cloud write, deployment, upstream synchronization, desktop Reports UI, or version-specific Sent association is implemented here; those boundaries remain with the linked downstream tickets.

## Simplification pass — 2026-08-26

- Reused `AssessmentPolicy`, `IDocumentContentStore`, existing case-document custody entities, and the existing renderer instead of adding a generic job framework or duplicate policy owner.
- Kept report persistence in one focused store and one focused Core aggregate file; the only new retry state is the three-attempt policy required by the ticket.
- Removed the redundant imported/non-imported total-labour renderer branch and excluded only request metadata (report date and optimistic case-version token) from the logical-key hash. Selected repair-specification identity remains part of the accepted payload.
- The independent review findings about date-boundary replay, selected estimate identity, version guards, concurrency, pending metadata recovery, validation, retry terminal state, shared readiness, and misleading UI text were applied. No known behaviour-preserving simplification finding remains unapplied.

## Validation checkpoint — 2026-08-26

- `dotnet build ./Pegasus.slnx --configuration Release --no-restore -nr:false -p:UseSharedCompilation=false` — passed with 0 warnings and 0 errors.
- Targeted report evidence — 26/26 Core report tests and 23/23 focused integration/web/renderer/migration tests passed; the full Core suite passed 930/930, architecture tests passed 101/101, and `scripts/Test-MigrationGrants.ps1` passed for all 68 migration files.
- The full local integration command `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --no-build --no-restore --filter "Category!=Corpus&Category!=Browser"` completed with 886 passed, 2 skipped, and 1 failure in 12m 54s. The sole failure is unrelated to DOCS-001: `GroupedImageIntakeConcurrencyTests.ConcurrentGroupMembersNeverSplitAcrossRepeatedRuns` fails with SQL Server deadlock 1205 at `EfIntakeWorkStore.CompleteProcessingAsync` line 338. The exact test reproduced independently in 46s with detailed logging. No DOCS-001 report path appears in that failure stack.
- This is not claimed as a green full-suite result. Task-specific validation is green; the unrelated intake deadlock remains a repository-level validation blocker to record in the post-implementation report.

## Final review disposition — 2026-08-26

- Bernoulli completed a fresh independent read-only review of the final diff and returned **PASS** with no actionable findings.
- The review specifically verified exact retry binding to the displayed stored report version, canonical-payload retry, shared `OperatorLabels`/`OfficeTime` presentation, no raw internal report state/version/UTC output, and the earlier store fixes for idempotency, concurrency, template identity, recovery, terminal cleanup, and grants.
- The simplification pass is complete. The final UI additions are limited to the required report-version retry control and the existing presentation vocabulary; no generic retry framework, second state owner, or unrelated UI was introduced.

## Exact-head CI blocker — 2026-08-26

PR #14 exact head `bb263b20a49af1375d2823ce5c4a803dd66bdc39` was validated by run `32959758190`. Browser passed 49/49; unit, infrastructure, changes, documentation, local-development-scripts, reference-data, SQL shards 1 and 3, and SQL coverage passed. SQL shard 2 failed, and the authorized failed-job rerun also failed, both on `GroupedImageIntakeConcurrencyTests.ConcurrentGroupMembersNeverSplitAcrossRepeatedRuns`: SQL Server deadlock 1205 at `EfIntakeWorkStore.CompleteProcessingAsync` line 338, through `DurableIntake.ProcessQueuedIntake`. This is the same unrelated intake concurrency failure reproduced in the local suite; no DOCS-001 report path appears in either stack. The PR remains unmerged because repository policy requires green required CI. The intake deadlock must be resolved by its owning work or explicitly accepted by the owning ticket before DOCS-001 can merge.

## Review remediation — 2026-08-27

Plato's independent review of exact head `9beae42dcc787f8d1f199866b8a52aed22a3bade` found three actionable issues, all fixed on this branch:

- The report-draft POST now re-authorizes the target case through the existing actor-aware `IGetCase` path before a stored `reportVersionId` can be read or retried. Cross-case denial coverage was added.
- The report-draft panel now renders only when stored versions exist or generation is currently available; the incomplete-case web test proves the empty panel is absent while readiness remains visible.
- `AssessmentReportVersion` now carries the rendering lease expiry from the persistence mapper, and the page exposes retry for an expired Rendering lease when the retry limit permits. The recovery test posts the stored version and proves the canonical payload reaches the renderer.

The one-line FeeNote characterization correction and these behaviour-preserving fixes do not change the simplification disposition. Focused validation after remediation: `AssessmentReportDraftWebTests|OperatorLabelsCharacterizationTests` — 35 passed, 0 failed, 0 skipped. The branch was then pushed as `9beae42d`; fresh exact-head CI and a fresh independent review remain required before merge.

## Review remediation follow-up — 2026-08-27

Plato independently reviewed exact head `7039bdf7fe24c8d9d94f21db3721a5918ec148f7` against `origin/dev` `67109b45066648b3256eff8d4bc3491a18bfeb7d` and returned FAIL because `AssessmentReadinessSummaryBrowserTests` asserted the report panel was absent and then called `InnerTextAsync()` on that absent locator. The review also required current exact-head evidence. The production remediation itself had no new finding. The test was corrected to assert the locator count is zero, committed as `8f60fc47f97f9e6ca18078a3341f6b0795dcc77d`, and pushed to the configured `origin` remote. Local browser validation passed 1/1. Fresh exact-head CI and fresh independent review of `8f60fc47` are pending; merge remains prohibited until both pass.

## Exact-head CI checkpoint — 2026-08-27

GitHub Actions run `33116768838` completed green for exact head `8f60fc47f97f9e6ca18078a3341f6b0795dcc77d`: changes, documentation, local-development-scripts, reference-data, infrastructure, unit, browser, SQL integration shards 1/2/3, and `sql-integration-coverage` all passed. Fresh independent review of this exact head remains pending before merge.

## Independent review follow-up — 2026-08-27

Curie reviewed exact head `8f60fc47f97f9e6ca18078a3341f6b0795dcc77d` against `origin/dev` `67109b45066648b3256eff8d4bc3491a18bfeb7d` and returned **FAIL**. Two blocking findings are accepted as implementation work: raw exception detail must not be persisted or rendered as operator-facing failure text; raw diagnostics may be logged in structured form, while the page uses the approved stable retry message. The recorded product decision that multiple accepted estimates remain separate tabs with one Generate action per estimate is not implemented by the current single-current-estimate/disabled-tab surface, so the accepted-estimate list and per-estimate report action must be wired in the owned page/store path. Curie's warning that the web fake does not exercise `ReportRepairCosts.FromAcceptedBasis` will be covered by the production persistence path or documented as a non-blocking test-gap disposition after the blocking fixes.

## Final remediation and validation — 2026-08-27

- Curie's two blocking findings were implemented in the ticket-owned branch. Report-generation failures now persist stable operator wording; the original diagnostic reason is sent only to structured logging. The page also maps any legacy stored failure reason to the same stable wording and never renders internal paths or exception text.
- Accepted repair specifications are now listable per case without collapsing independent estimates. The assessment page selects one accepted specification by query-string identity, renders one source-attributed tab per estimate, and posts that selected identity through the existing report-generation action. Explicit correction still requires a predecessor and supersedes only that predecessor; independent estimates remain Accepted. No internal rate-card or precedence rule was added.
- Added production persistence coverage proving two independent accepted estimates remain separate and are returned by `ListAcceptedAsync`; the web test covers selection, tab state, and the selected identity carried by Generate/retry markup. The web fixture's direct costs are intentionally limited to selection/presentation coverage; accepted-basis conversion remains covered by the production projection/persistence path and is not duplicated in a markup test.
- Simplification pass completed: reused the existing accepted-specification store, report projection, operator-facing vocabulary, document custody, and renderer; removed an unused tab wrapper; added no compatibility path, generic job abstraction, rate-card policy, or speculative desktop implementation.
- Local validation after remediation: `dotnet build .\\Pegasus.slnx --configuration Release -nr:false -p:UseSharedCompilation=false --no-restore` passed with 0 warnings and 0 errors; focused report/import/persistence integration tests passed 12/12; Core tests passed 938/938; architecture tests passed 111/111; `pwsh -NoProfile -File .\\scripts\\Test-MigrationGrants.ps1` passed 70/70 migration files. Full integration validation is running separately and retains the previously evidenced unrelated intake deadlock disposition if it recurs.
- Fresh independent review and exact-head CI are still required for the resulting commit before merge.
