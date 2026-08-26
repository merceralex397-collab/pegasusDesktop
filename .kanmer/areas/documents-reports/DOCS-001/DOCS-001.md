---
id: DOCS-001
type: ticket
title: >-
  upstream:DOCS-001 · Trigger report generation from complete accepted
  assessments and retain immutable report references
status: implementing
area: documents-reports
assignee: codex-mcp-client
profile: feature
stageEntered:
  preparing: '2026-08-24T21:22:05.347Z'
taken_at: '2026-08-26T07:45:42.803Z'
branch: task/upstream-docs-001-report-aggregate
worktree: ../pegasus-worktrees/upstream-docs-001-report-aggregate
labels:
  - now
  - renderer-integration
  - upstream-carryover
  - upstream-DOCS-001
  - report-decision
  - needs-operator
groups:
  - EPIC-014
links: []
blocks:
  - FEAT-042
  - GWY-014
  - FEAT-018
refs:
  - docs/frd/frd-11-reports-correspondence-and-reviewed-proposals.md
  - docs/adr/0025-integrate-renderer-and-extractor-into-the-application.md
docs_todo: true
archived: false
created: '2026-08-24T11:41:20.415Z'
updated: '2026-08-26T07:45:42.803Z'
---

## What

Deliver the Core-owned **front half** of report generation that the 208 seeded conversion tickets have no owner for: a durable report request/version aggregate keyed on the case, the accepted assessment snapshot and a deterministic payload hash; a readiness gate that fails closed on missing, unaccepted or ambiguous required data on **both** the draft and the register path; idempotent generation, so one accepted input plus template version yields exactly one report version and a retry reconciles to it rather than duplicating it; and append-only correction lineage that preserves every earlier artifact with its provenance, hashes and custody state.

## Why

The conversion delivers only report *registration*. [[DSK-07-16]] stores a desktop-rendered PDF and then approves that stored identity, and its own scope boundary forbids it from writing the missing rule — "must not write a second readiness rule — `AssessmentReportProjection` in `src/Pegasus.Core` is the only one". [[DSK-03-14]]'s single readiness mention is "returns the assessment model and readiness summary" on the GET, with no acceptance criterion behind it. Locked decision **L-03** moved rendering onto the client *without moving the readiness gate with it*, so on the seeded set as it stands a desktop can render a PDF for an incomplete, unaccepted assessment and hand it to the gateway to register.

Nothing on the board owns the durable aggregate that makes generation idempotent or a correction append-only. the imported upstream DOCS-001 (board [[DOCS-001]]) ticket's own research states it in terms: "There are no report request, report version, assessment/fee-note artifact, payload identity, source provenance, generation state, attempt, lease, or failure tables/ports", and reusing `CaseReportApproval` "would collapse generation into human approval and lose the fee-note pair and correction history". The operator-visible consequence is a report registered against a case that was never complete, and a correction that silently replaces the artifact that was actually issued.

Upstream will not close it. DOCS-001 is `preparing` upstream and blocked on TICK-092/093/094; under **D-001** the fork becomes the single release source at the first production gateway change and upstream is then frozen, so anything unmerged upstream vanishes. This is the front half of the report path and the desktop inherits the hole whole.

**Sequencing.** This ticket lands *before* the imported `upstream:TICK-208`, which binds each issued version to its own final Sent evidence and consumes the immutable report-version identity this ticket creates. It blocks [[DSK-07-16]], [[DSK-03-14]] and the slice [[DSK-05-18]]; [[DSK-07-17]] step 7 records this import as unconditional.

## Source of truth

- Import decision: coverage decision § Plan gaps — "The report FRONT half is missing: there is no durable report request/version aggregate, no committed-accepted-snapshot trigger, no payload-hash idempotency, no readiness gate on the draft or finalise path, and no append-only issued-version to Sent-evidence ledger"; § Import list row `DOCS-001`.
- Fork board neighbours: [[DSK-07-16]] (registration plus the server-side readiness re-check), [[DSK-03-14]] (assessment and report endpoints), [[DSK-05-18]] (the slice), [[DSK-07-14]] (the desktop renderer), [[DSK-07-17]] step 7 (disposition), [[DSK-01-09]] (the carry-over pass that files this).
- Repository evidence, fork `main`, read 2026-08-24:
  - `src/Pegasus.Core/Reports/AssessmentReportRendering.cs` — the merged, caller-supplied draft boundary (`AssessmentReportSnapshot`, `GenerateAssessmentReportDraft`, `IAssessmentReportRenderer`); no case or report identity, no persistence.
  - `src/Pegasus.Core/Reports/AssessmentReportProjection.cs` — `AssessmentReportProjection.Project`, `AssessmentReportDraftPreparation`, `GenerateCaseAssessmentReportDraft`: the one readiness owner.
  - `src/Pegasus.Core/Assessment/AssessmentPolicy.cs` — `EvaluateReadiness`, which checks presence rather than deriving an immutable snapshot.
  - `src/Pegasus.Core/Assessment/AssessmentOperations.cs` and `src/Pegasus.Infrastructure/Persistence/EfCaseAssessmentStore.cs` — the serializable committed accepted-data transaction that is the real trigger seam.
  - `src/Pegasus.Core/Workflow/CaseWorkflowContracts.cs:62-79` (`ReportApprovalEvidence`, `ReportApprovalSubmission`), `src/Pegasus.Infrastructure/Persistence/CaseWorkflowEntities.cs:10` and `:12` (`ReportApprovalId`, `ReportSentEvidenceId` — one slot each), `CaseWorkflowModelConfiguration.cs:39-47` (both unique).
  - `src/Pegasus.Core/Documents/DocumentContracts.cs` and `src/Pegasus.Infrastructure/Persistence/EfDocumentCustodyStore.cs` — immutable content addresses with SHA-256/length verification to reuse; `DocumentSource.Generated` and `DocumentSemanticRole.EngineerReport` already exist.
  - `src/Pegasus.Core/Custody/ExternalWorkProcessing.cs:8-13` — `ExternalWorkKinds` and the pending/claim/lease/attempt/terminal-failure convention to imitate rather than overload.
  - `src/Pegasus.Web/Pages/Cases/Assessment/Index.cshtml.cs` — the Razor surface the upstream ticket assumed for status and failures, which the conversion cut list deletes.
  - `scripts/Test-MigrationGrants.ps1` — any new table needs its runtime-role grant or CI fails.
- Governing documents that exist: `docs/frd/frd-11-reports-correspondence-and-reviewed-proposals.md`, `docs/adr/0025-integrate-renderer-and-extractor-into-the-application.md`. ADR-0108 (the desktop renderer decision, reserved block ADR-0100…ADR-0110) is not written yet — that is what `docs_todo` records.
- Binding decisions: **L-01** the gateway is `Pegasus.Web` evolved in place, so the durable caller and processor compose there. **L-02** the local DevelopmentOffline stack is the only verification environment. **L-03 / ADR-0108** the desktop renders and the gateway stores; the gateway renderer is retained until golden-file parity passes. **D-001** upstream is frozen at the first production gateway change, so the upstream prerequisites TICK-092/093/094 may never merge and this ticket must be able to proceed without them.
- Provenance of the copy below: upstream area `documents-reports`, upstream status `preparing`, upstream profile `feature`, upstream labels `now, renderer-integration`, upstream groups `EPIC-004`; read from the read-only clone of `collisionengineers/pegasus` branch `kanmer-board` at clone commit **`a5b28111`**, read date **2026-08-24**.

### Upstream ticket DOCS-001 (verbatim)

```markdown
## What

Add the Core-owned workflow that detects a complete, accepted assessment, invokes the integrated renderer, and records the generated report's immutable reference, version, hash, template/payload versions, provenance, and custody state against the case.

## Why

A renderer library is not an integrated product capability until a real Pegasus assessment caller produces and retains a report. `reference/rendererref1/` supplies the key assessment template/schema evidence.

## Approach

- Define readiness and idempotency in Core; fail closed on missing, unaccepted, or ambiguous required data.
- Map accepted case/assessment data to the renderer contract without a second business-policy implementation.
- Generate once per accepted input/version; retries return or reconcile the same durable job/result.
- Preserve earlier artifacts; corrections and addenda create new immutable versions.
- Surface generation state and actionable failures to staff without implying issue or delivery.

## Verification

- [ ] A complete accepted assessment produces a deterministic report through the composed application path.
- [ ] Incomplete or ambiguous assessment data cannot render.
- [ ] The case retains immutable reference/version/hash/provenance and idempotent retry behavior.
- [ ] Report generation does not count as approval, sending, or external receipt.

## Outcome
```

## Routing

- **Subagent**: `pegasus-gateway-dev` — `.codex/agents/pegasus-gateway-dev.toml`
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `dotnet-webapi` (dotnet/skills `98f84851`) → `run-tests`
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search` for EF Core owned entities, unique filtered indexes and serializable-transaction retry on SQL Server)
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (call `get_doc_gates <id>` before every move; a move crosses at most one gated boundary)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Orient. Read this body in full including the verbatim upstream ticket, then the three upstream pipeline documents copied onto this ticket (`research`, `files`, `open-questions`) — they are the requirement, not a summary of it. Read `docs/frd/frd-11-reports-correspondence-and-reviewed-proposals.md` and `docs/adr/0025-integrate-renderer-and-extractor-into-the-application.md`, then read [[DSK-07-16]] and [[DSK-03-14]] so you do not rebuild the registration endpoint. Call `get_doc_gates <this ticket id>`, then `take_ticket` on branch `task/upstream-docs-001-report-aggregate`.
2. **Re-scope against the fork, not upstream `dev`.** The copied research was taken against upstream `origin/dev` at `b548b674` and holds this ticket in Preparing until TICK-092, TICK-093 and TICK-094 merge. On the fork that instruction cannot stand unqualified: under **D-001** upstream freezes. After [[DSK-01-10]]'s sync, re-derive their state against the fork's `main` by reading `src/Pegasus.Core/Reports/AssessmentReportProjection.cs`, `src/Pegasus.Core/Assessment/AssessmentPolicy.cs` and `src/Pegasus.Core/Assessment/RepairSpecifications.cs` as they actually stand, and record in `research` which of the three landed. If any did not, this ticket owns the minimum accepted-snapshot projection itself — one query, one deterministic payload hash — and says so in `plan`; it does not wait for a repository that is frozen.
3. **Operator step** — resolve the trigger question before any of steps 5-8 is written. Upstream DOCS-001 records generation as **automatic** ("detects a complete, accepted assessment, invokes the integrated renderer") while `docs/desktop/06-ui-design/screen-specs.md` §13.9 and [[DSK-07-16]] make Generate an **operator-initiated** `Case.Reports.Generate` command. The operator must say which is the desktop contract. Record the answer in this ticket's `open-questions` document and mirror it into [[DSK-07-16]]'s. Do not invent a hybrid and do not implement automatic generation on the strength of the upstream wording alone. Evidence the operator hands back: one sentence naming the trigger and whether a staff command may also force a regeneration.
4. Define the readiness contract in `plan` over the **one existing owner** — `AssessmentReportProjection.Project` and `GenerateCaseAssessmentReportDraft` in `src/Pegasus.Core/Reports/AssessmentReportProjection.cs`. Enumerate the renderer inputs the copied research lists as *not* covered by `AssessmentPolicy.EvaluateReadiness` (principal/report addressee and external reference, incident date, inspection mode presence, selected ordered current images with content bytes and custody, canonical raw cost components and display sections, source identities/versions/hashes, the accepted engineer tuple) and add them to that owner. Never write a second readiness implementation; `AssessmentReadinessItem.Requirement` and `WhyOutstanding` stay the vocabulary.
5. Add the durable report aggregate in `src/Pegasus.Core/Reports/` as its own focused file(s): report request and report version states, the typed assessment-plus-fee-note artifact pair with identity and SHA-256, the deterministic logical key (case + active assessment family + accepted payload hash + template version), retry and terminal-failure policy, and predecessor/successor correction lineage. Reuse the conventions in `src/Pegasus.Core/Custody/ExternalWorkProcessing.cs` — do **not** overload `ExternalWorkItem` and do not invent a generic job framework for a single caller. Done looks like: `dotnet build ./Pegasus.slnx --configuration Release` succeeds with the new Core types and no Infrastructure reference from Core.
6. Persist it in `src/Pegasus.Infrastructure/Persistence/`: new report entities and a model configuration beside `CaseWorkflowEntities.cs` / `CaseWorkflowModelConfiguration.cs`, a migration under `src/Pegasus.Infrastructure/Persistence/Migrations/`, and the regenerated `PegasusDbContextModelSnapshot.cs`. The logical key gets a unique index so two callers cannot create two reports for one accepted input. Prior versions are never overwritten. Add the runtime-role grant in the same migration — `pwsh ./scripts/Test-MigrationGrants.ps1` must pass, and discovering this in CI instead is the trap upstream PLAT-035 records.
7. Attach generation to the **committed** accepted-snapshot boundary, not to the Razor page and not to the renderer adapter: enqueue from the transaction in `EfCaseAssessmentStore` that already persists under serializable isolation with the expected case version, edit lease and operation-key replay. Rendering itself runs *after* the durable request exists, under lease and retry protection, because the renderer cannot share the source-data transaction. If step 3 settled on an operator-initiated trigger, the enqueue is the gateway command instead and the same durability rules apply — record which in `plan`.
8. Store both artifacts through the existing content path — `IDocumentContentStore` with `DocumentSource.Generated`, `DocumentSemanticRole.EngineerReport` for the assessment PDF — so a generated report is a normal case document version with custody state. Do **not** force system-generated work through `AddCaseDocumentCommand`'s staff edit lease and expected-case-version requirement; give generation its own system-owned atomic result boundary, and name the fee note's semantic role rather than leaving it untyped.
9. **Re-expressed for the desktop.** Upstream item 11 of the research puts generation state, failures, retry and artifact download on `src/Pegasus.Web/Pages/Cases/Assessment/Index.cshtml(.cs)`. That page is deleted by the conversion cut list, so keep the requirement and move it: expose Pending/Rendering/Generated/Failed/Retry, the actionable failure reason, and the version list as a **gateway projection** on the case reports section that [[DSK-07-16]] step 8 and [[DSK-03-14]] already own the route for, rendered by the desktop Reports tab under the existing AutomationIds `Case.Reports.Generate`, `Case.Reports.Preview`, `Case.Reports.Send`. The desktop renders named server states; it computes none of them. Stuck or failed generation appears in the Operations surface of [[DSK-05-20]] / [[DSK-07-04]] rather than inventing a second operational convention.
10. Keep the three finality boundaries apart, as FRD-11 requires: generation is a draft; approval is a human act bound to a stored artifact identity and hash; sending is proved only by retained exact Sent evidence. A generated version is never rendered as approved, issued, sent or received. Version-specific approval and Sent association are **not** built here — they belong to the imported `upstream:TICK-208`, which sequences after this ticket.
11. Test in the projects that exist on the fork. `tests/Pegasus.Core.Tests` — readiness fails closed on each missing or unaccepted input with the named requirement; the logical key is deterministic; a changed accepted payload or template yields a successor version; correction never mutates a predecessor. `tests/Pegasus.IntegrationTests` — following `CaseWorkflowPersistenceTests.cs`, `DocumentCustodyDurabilityTests.cs` and `CustodyOutboxIntegrationTests.cs`: exact replay returns the same report and stores nothing new, two concurrent callers produce one version, a crash between database commit and content write leaves no half-report, and the migration preserves existing approvals.
12. Verify on the local stack only (**L-02**) — no Azure and no Box write. Then run the simplification pass over this branch diff, record it under a dated `## Simplification pass` heading in this ticket's `plan` document, and open the PR into `dev`.

## Acceptance criteria

- [ ] An incomplete, unaccepted or ambiguous assessment cannot produce a report on **either** the draft or the register path, and the refusal names each outstanding requirement rather than collapsing into one generic message.
- [ ] One accepted input plus template version produces exactly one report version; an exact replay returns or reconciles to it and creates no second version.
- [ ] The case retains an immutable report version identity, hash, template/payload versions, provenance and custody state for the assessment and fee-note artifacts as a fixed pair.
- [ ] A correction or addendum appends a successor version and leaves every earlier artifact, its provenance and its approval untouched.
- [ ] Generation is never rendered or recorded as approval, issue, sending or external receipt.
- [ ] Readiness has exactly one owner in `src/Pegasus.Core`; no second required-field list exists in Web, Infrastructure or the desktop.
- [ ] The new tables carry their runtime-role grants and `scripts/Test-MigrationGrants.ps1` passes.
- [ ] The trigger question of step 3 is answered by the operator and recorded before the trigger is implemented.

## Verification

- [ ] `dotnet restore ./Pegasus.slnx --locked-mode` — expected: restore succeeds with no lock-file drift.
- [ ] `dotnet build ./Pegasus.slnx --configuration Release --no-restore` — expected: build succeeds; `dotnet test ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj --configuration Release` still passes, proving Core gained no Infrastructure dependency.
- [ ] `dotnet test ./tests/Pegasus.Core.Tests/Pegasus.Core.Tests.csproj --configuration Release` — expected: fail-closed readiness, deterministic key, successor-on-change and no-mutation-of-predecessor facts pass.
- [ ] `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --filter "Category!=Corpus&Category!=Browser"` — expected: replay, concurrency, partial-failure and migration-preservation facts pass.
- [ ] `pwsh ./scripts/Test-MigrationGrants.ps1` — expected: exits 0 with the new report tables listed as granted.

## Evidence tier

Tier 4 — LocalDB persistence, with Tier 2 (Core/domain) underneath it.
Tier 4 obliges committed SQL Server migrations proven on a fresh and an existing schema, state and action-history atomicity, unique-constraint and concurrency behaviour, stale versions and leases — which is exactly what an idempotent, append-only report aggregate has to demonstrate. Tier 2 obliges the positive, contradictory, ambiguous and failure readiness cases.

## Documentation changes

- `docs/frd/frd-11-reports-correspondence-and-reviewed-proposals.md` — the durable report request/version aggregate, the idempotency key and the correction-lineage clause, once implemented.
- `docs/current-architecture.md` — the report aggregate and the generation path, after the slice ships.
- `docs/desktop/03-gateway-api-and-data/endpoint-map.md` — the readiness refusal recorded on both the `POST /cases/{id}/reports/draft` and `POST /cases/{id}/reports` rows (mirrors [[DSK-07-16]]).
- `docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md` — the `DOCS-001` row annotated with this fork ticket id.

## Guardrails

- **Azure**: no write. Verification is the local DevelopmentOffline stack under **L-02**; no Azure test resource may be requested.
- **Scope boundary**: may touch `src/Pegasus.Core/Reports/**`, `src/Pegasus.Core/Assessment/**` (readiness inputs only), `src/Pegasus.Infrastructure/Persistence/**` (new report entities, configuration, migration, snapshot), `src/Pegasus.Infrastructure/DependencyInjection.cs`, `src/Pegasus.Web/Program.cs` composition, `tests/Pegasus.Core.Tests`, `tests/Pegasus.IntegrationTests`, `tests/Pegasus.ArchitectureTests`. Must **not** add a second readiness rule, must **not** build the `/api/v1` register endpoint or the desktop Reports UI (that is [[DSK-07-16]]), must **not** build version-specific Sent-evidence association (that is the imported `upstream:TICK-208`), must **not** touch `src/Pegasus.Worker`, and must **not** create a standalone renderer host, MCP tool or second editable report-data record.
- **Blocks / blocked by**: this ticket **blocks** [[DSK-07-16]] (its report record and idempotency have no aggregate without it), [[DSK-03-14]] (the readiness summary it returns has nothing behind it) and [[DSK-05-18]] (the slice cannot sign off on a report path that can render a not-ready assessment). It **is blocked by** [[DSK-01-10]]'s upstream sync only to the extent that step 2 needs the merged state of TICK-092/093/094 to be known; it is not blocked by their completion. It sequences **before** the imported `upstream:TICK-208`.
- **Traps**: a new table without a `Grant*` migration fails `scripts/Test-MigrationGrants.ps1` in CI (upstream PLAT-035); reusing `CaseReportApproval` as the report record collapses generation into approval and loses the fee-note pair; rendering inside the assessment transaction cannot work because the renderer is an out-of-transaction effect; a random operation key alone is not idempotency — two callers with different keys and the same accepted input must not create two reports; and the human-readable reference on a generated report is the existing Case/PO number (`OurReference`) by operator decision of 2026-08-19 recorded in the copied `open-questions` — do not create a second outward report-number sequence.
- **Open question carried from upstream**: the copied `open-questions` document ties the implementation plan to merged TICK-093, TICK-094 and TICK-092. Step 2 replaces "wait" with "re-derive and, if frozen, own the minimum snapshot"; record that deviation explicitly in `plan` rather than silently ignoring the upstream instruction.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.

## Outcome

_Filled at closeout._
