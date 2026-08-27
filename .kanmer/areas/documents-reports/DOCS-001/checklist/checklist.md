# Checklist — DOCS-001: upstream:DOCS-001 · Trigger report generation from complete accepted assessments and retain immutable report references

- [x] Orient. Read this body in full including the verbatim upstream ticket, then the three upstream pipeline documents copied onto this ticket (`research`, `files`, `open-questions`) — they are the requirement, not a summary of it. Read `docs/frd/frd-11-reports-correspondence-and-reviewed-proposals.md` and `docs/adr/0025-integrate-renderer-and-extractor-into-the-application.md`, then read [[DSK-07-16]] and [[DSK-03-14]] so you do not rebuild the registration endpoint. Call `get_doc_gates <this ticket id>`, then `take_ticket` on branch `task/upstream-docs-001-report-aggregate`.
- [x] Re-scope against this repository without upstream synchronization. Confirmed TICK-093 is present locally; no dedicated TICK-092/TICK-094 contract is present, so DOCS-001 owns the minimum accepted snapshot and deterministic payload hash.
- [x] Operator decision recorded on 2026-08-26: staff invokes `Generate report draft`; automatic and hybrid triggers are excluded; exact replay is idempotent; changed accepted input/template creates a successor. Repair costs are externally supplied by a connected system or imported estimate document; multiple estimates remain separate tabs with an explicit Generate action per estimate and source provenance.
- [x] Mirror the trigger decision into board ticket [[FEAT-042]]'s `open-questions` document.
- [x] Define the readiness contract in `plan` over the one existing owner — `AssessmentReportProjection.Project` and `GenerateCaseAssessmentReportDraft` in `src/Pegasus.Core/Reports/AssessmentReportProjection.cs`. Enumerate the renderer inputs the copied research lists as *not* covered by `AssessmentPolicy.EvaluateReadiness` (principal/report addressee and external reference, incident date, inspection mode presence, selected ordered current images with content bytes and custody, canonical raw cost components and display sections, source identities/versions/hashes, the accepted engineer tuple) and add them to that owner. Never write a second readiness implementation; `AssessmentReadinessItem.Requirement` and `WhyOutstanding` stay the vocabulary.
- [x] Add the durable report aggregate in `src/Pegasus.Core/Reports/` as its own focused file(s): report request and report version states, the typed assessment-plus-fee-note artifact pair with identity and SHA-256, the deterministic logical key (case + active assessment family + accepted payload hash + template version), retry and terminal-failure policy, and predecessor/successor correction lineage. Reuse the conventions in `src/Pegasus.Core/Custody/ExternalWorkProcessing.cs` — do not overload `ExternalWorkItem` and do not invent a generic job framework for a single caller. Done looks like: `dotnet build ./Pegasus.slnx --configuration Release` succeeds with the new Core types and no Infrastructure reference from Core.
- [x] Persist it in `src/Pegasus.Infrastructure/Persistence/`: new report entities and a model configuration beside `CaseWorkflowEntities.cs` / `CaseWorkflowModelConfiguration.cs`, a migration under `src/Pegasus.Infrastructure/Persistence/Migrations/`, and the regenerated `PegasusDbContextModelSnapshot.cs`. The logical key gets a unique index so two callers cannot create two reports for one accepted input. Prior versions are never overwritten. Add the runtime-role grant in the same migration — `pwsh ./scripts/Test-MigrationGrants.ps1` must pass, and discovering this in CI instead is the trap upstream PLAT-035 records.
- [x] Attach generation to the committed accepted-snapshot boundary, not to the Razor page and not to the renderer adapter: enqueue from the transaction in `EfCaseAssessmentStore` that already persists under serializable isolation with the expected case version, edit lease and operation-key replay. Rendering itself runs *after* the durable request exists, under lease and retry protection, because the renderer cannot share the source-data transaction. If step 3 settled on an operator-initiated trigger, the enqueue is the gateway command instead and the same durability rules apply — record which in `plan`.
- [x] Store both artifacts through the existing content path — `IDocumentContentStore` with `DocumentSource.Generated`, `DocumentSemanticRole.EngineerReport` for the assessment PDF — so a generated report is a normal case document version with custody state. Do not force system-generated work through `AddCaseDocumentCommand`'s staff edit lease and expected-case-version requirement; give generation its own system-owned atomic result boundary, and name the fee note's semantic role rather than leaving it untyped.
- [x] Re-expressed the generation states, failure/retry ownership, and AutomationId handoff for the desktop in the plan; implementation remains explicitly owned by [[DSK-07-16]] and out of scope here.
- [x] Keep the three finality boundaries apart, as FRD-11 requires: generation is a draft; approval is a human act bound to a stored artifact identity and hash; sending is proved only by retained exact Sent evidence. A generated version is never rendered as approved, issued, sent or received. Version-specific approval and Sent association are not built here — they belong to the imported `upstream:TICK-208`, which sequences after this ticket.
- [x] Test in the projects that exist on the fork. `tests/Pegasus.Core.Tests` — readiness fails closed on each missing or unaccepted input with the named requirement; the logical key is deterministic; a changed accepted payload or template yields a successor version; correction never mutates a predecessor. `tests/Pegasus.IntegrationTests` — following `CaseWorkflowPersistenceTests.cs`, `DocumentCustodyDurabilityTests.cs` and `CustodyOutboxIntegrationTests.cs`: exact replay returns the same report and stores nothing new, two concurrent callers produce one version, a crash between database commit and content write leaves no half-report, and the migration preserves existing approvals.
- [ ] Verify on the local stack only (L-02) — no Azure and no Box write. Then run the simplification pass over this branch diff, record it under a dated `## Simplification pass` heading in this ticket's `plan` document, and open the PR into `dev`.
- [x] An incomplete, unaccepted or ambiguous assessment cannot produce a report on **either** the draft or the register path, and the refusal names each outstanding requirement rather than collapsing into one generic message.
- [x] One accepted input plus template version produces exactly one report version; an exact replay returns or reconciles to it and creates no second version.
- [x] The case retains an immutable report version identity, hash, template/payload versions, provenance and custody state for the assessment and fee-note artifacts as a fixed pair.
- [x] A correction or addendum appends a successor version and leaves every earlier artifact, its provenance and its approval untouched.
- [x] Generation is never rendered or recorded as approval, issue, sending or external receipt.
- [x] Readiness has exactly one owner in `src/Pegasus.Core`; no second required-field list exists in Web, Infrastructure or the desktop.
- [x] The new tables carry their runtime-role grants and `scripts/Test-MigrationGrants.ps1` passes.
- [x] The trigger question of step 3 is answered by the operator and recorded before the trigger is implemented.
- [x] A complete accepted assessment produces a deterministic report through the composed application path.
- [x] Incomplete or ambiguous assessment data cannot render.
- [x] The case retains immutable reference/version/hash/provenance and idempotent retry behavior.
- [x] Report generation does not count as approval, sending, or external receipt.

## Progress notes

Implementation and independent review are complete on the local branch; PR, merge, proof, and Kanmer closeout remain tracked as separate gates.


## Validation checkpoint — 2026-08-26

- Targeted DOCS-001 validation passed: Release build (0 warnings/errors), 26/26 Core report tests, 23/23 focused integration/web/renderer/migration tests, 930/930 Core tests, 101/101 architecture tests, and migration grants for 69/69 migration files.
- Full local integration validation was run but is not marked green: 886 passed, 2 skipped, 1 failed. The sole failure is the unrelated `GroupedImageIntakeConcurrencyTests.ConcurrentGroupMembersNeverSplitAcrossRepeatedRuns` SQL deadlock 1205 at `EfIntakeWorkStore.CompleteProcessingAsync` line 338; it reproduced independently. The full-stack verification checkbox remains open until this repository-level failure is resolved or separately accepted by the owning ticket.


## Final review checkpoint — 2026-08-26

- [x] Fresh independent review by Bernoulli passed with no actionable findings.
- [x] Final targeted validation passed: Release build 0/0, 26/26 report Core tests, 23/23 focused integration/report/web/renderer/migration tests, 930/930 full Core tests, 101/101 architecture tests, and 69/69 migration grants.
- [ ] Full repository integration suite remains open because 886 passed, 2 skipped, and the unrelated `GroupedImageIntakeConcurrencyTests.ConcurrentGroupMembersNeverSplitAcrossRepeatedRuns` test failed with SQL deadlock 1205 at `EfIntakeWorkStore.CompleteProcessingAsync` line 338.

## Exact-head CI blocker — 2026-08-26

- [ ] PR #14 head `bb263b20` required CI is fully green. Browser 49/49, unit, infrastructure, repository checks, SQL shards 1 and 3, and coverage passed; SQL shard 2 failed and its authorized rerun failed identically on the unrelated intake deadlock at `EfIntakeWorkStore.CompleteProcessingAsync:338`.

## Remediation checkpoint — 2026-08-27

- [x] Corrected the FeeNote characterization expectation after exact-head CI identified the stale enum assertion.
- [x] Re-authorized stored report-version retry through actor-aware case lookup and added cross-case denial coverage.
- [x] Omitted the report-draft panel when it has neither versions nor an available generation action, with incomplete-case coverage.
- [x] Made expired Rendering leases retryable from the retained canonical payload, with recovery coverage.
- [ ] Fresh exact-head CI for `9beae42d` is fully green.
- [ ] Fresh independent review of the remediated head passes.
- [ ] Merge, proof, and Kanmer closeout remain pending.

## Review remediation follow-up — 2026-08-27

- [x] Plato independently reviewed exact head `7039bdf7` and identified the invalid absent-locator text lookup.
- [x] Replaced the invalid text lookup with a zero-count assertion; local browser test passed 1/1.
- [ ] Fresh exact-head CI for `8f60fc47` is fully green.
- [ ] Fresh independent review of exact head `8f60fc47` passes.
- [ ] Merge, proof, and Kanmer closeout remain pending.

## Exact-head CI checkpoint — 2026-08-27

- [x] Exact-head CI run `33116768838` for `8f60fc47` is fully green, including browser, SQL shards 1/2/3, and coverage.
- [ ] Fresh independent review of exact head `8f60fc47` passes.
- [ ] Merge, proof, and Kanmer closeout remain pending.

## Independent review follow-up — 2026-08-27

- [ ] Curie's blocking finding on raw exception detail is fixed: only stable operator wording is persisted/rendered, with raw detail limited to structured diagnostics.
- [ ] Curie's blocking finding on multiple accepted estimate tabs is fixed: accepted estimates are separately selectable and each has its own Generate action with source provenance.
- [ ] Curie's non-blocking production-cost-composition warning is addressed or honestly dispositioned.
- [ ] Fresh independent review and exact-head CI pass on the resulting commit.
- [ ] Merge, proof, and Kanmer closeout remain pending.
