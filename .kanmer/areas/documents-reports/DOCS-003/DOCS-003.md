---
id: DOCS-003
type: ticket
title: >-
  upstream:TICK-208 · Preserve final Sent evidence through post-report
  correction
status: review
area: documents-reports
assignee: codex-mcp-client
profile: feature
stageEntered:
  preparing: '2026-08-24T21:23:44.571Z'
  review: '2026-08-28T01:17:17.520Z'
taken_at: '2026-08-27T22:49:17.645Z'
branch: task/upstream-tick-208-issued-version-ledger
worktree: ../pegasus-worktrees/upstream-tick-208-issued-version-ledger
labels:
  - now
  - source-now
  - upstream-carryover
  - upstream-TICK-208
  - report-decision
groups:
  - EPIC-014
links: []
blocks:
  - FEAT-042
  - FEAT-018
refs:
  - docs/frd/frd-11-reports-correspondence-and-reviewed-proposals.md
docs_todo: true
commits:
  - 33f0017c
  - add9da25
prs:
  - '33'
archived: false
created: '2026-08-24T11:41:20.454Z'
updated: '2026-08-28T01:34:14.968Z'
---

## What

Replace the single-slot report-approval and Sent-evidence pointers with an **append-only issued-report-version ledger**: each issued version binds its own immutable artifact identity and hash, its approval, its correction reason and predecessor, and zero or one exact final Sent evidence item, with permanent reasoned association history. A correction creates a new **unsent** successor and never unlinks, repoints, recycles or inherits the predecessor's final Sent evidence.

## Why

This is a Core **data-model** defect the desktop inherits unchanged, and it makes a promise the conversion has already written down unimplementable. `docs/desktop/06-ui-design/screen-specs.md` §13.9 requires a "list of issued versions with custody and sent evidence shown separately", and [[DSK-07-16]] step 11 requires exactly that — but Core carries **one** `ReportApprovalId` and **one** `ReportSentEvidenceId` per case (`src/Pegasus.Infrastructure/Persistence/CaseWorkflowEntities.cs:10`, `:12`, both unique-indexed at `CaseWorkflowModelConfiguration.cs:39-40`). While that is true, that column pair cannot be honest: there is no relationship in the database that can answer *which immutable report version the final Sent item issued*. Worse, `UnlinkReportEvidenceAsync` clears the evidence row's case, link time and linking actor, so the row survives as an unlinked candidate having lost its former association — a correction modelled as an unlink erases the fact that a version was issued.

The operator-visible consequence: after a corrected report, the record of what was actually sent to the customer, and when, is gone or attached to the wrong version. FRD-11 already says an issued artifact is immutable and a correction retains every earlier artifact, actor, time and source — so this is enforcing an existing requirement, not adding one.

No seeded ticket is permitted to fix it. [[DSK-07-16]]'s scope boundary confines it to the `/api/v1` case reports group, `Pegasus.Contracts` and the desktop projects, and its own Traps say the separately shown issued versions "depend on the recreated `TICK-208` ledger and must not be faked over the single-slot `ReportApprovalId` / `ReportSentEvidenceId`". [[DSK-07-11]] builds the outbound seam and records sent evidence as an audit fact, not a version binding. Nothing on the board changes `src/Pegasus.Core/Workflow` or the workflow schema for this. Upstream will not close it either: TICK-208 sits at `preparing` with a complete plan and closed open questions, and under **D-001** upstream is frozen at the first production gateway change. Because nothing here needs deciding, [[DSK-07-17]] step 7's *conditional* recreation becomes **unconditional** — that change is recorded as an amendment to [[DSK-07-17]] and is not made by this ticket.

**Sequencing.** This ticket lands **after** the imported `upstream:DOCS-001`, whose durable report version identity and hash it consumes. Its own plan says so: step 1 is to re-read merged DOCS-001 and, if that ticket already satisfies every acceptance condition here, close this one from evidence with no repository change.

## Source of truth

- Import decision: coverage decision § Plan gaps — "The report FRONT half is missing" (the `TICK-208` half: "Core carries one ReportApprovalId and one ReportSentEvidenceId per case"); § Import list row `TICK-208`; § Amend list `FEAT-042 (DSK-07-16) ← DOCS-001, TICK-208` and `FEAT-043 (DSK-07-17) ← … step 7 … from conditional to unconditional`.
- Fork board neighbours: the imported `upstream:DOCS-001` (prerequisite), [[DSK-07-16]] (step 11's separate columns read this ledger), [[DSK-05-18]] (the slice), [[DSK-07-11]] (outbound sent evidence), [[DSK-07-17]] (records the disposition), [[DSK-01-09]] (the carry-over pass that files this).
- Repository evidence, fork `main`, read 2026-08-24:
  - `src/Pegasus.Core/Workflow/CaseWorkflowContracts.cs:62-79` (`ReportApprovalEvidence`, `ReportApprovalSubmission`), `:85-104` (`ApprovedMailboxReportSentEvidence` and its exact retained fields), `:107-108` (`CaseWorkflowRecord.ReportApproval` / `.ReportSentEvidence` — one each), `:365` and `:373` (`RecordReportApprovalAsync`, `UnlinkReportEvidenceAsync`), `:420-445` (`IRecordCaseReportApproval`, `ILinkReportEvidence`, `IAutoLinkReportEvidence`, `IUnlinkReportEvidence`).
  - `src/Pegasus.Infrastructure/Persistence/CaseWorkflowEntities.cs:10`, `:12` — the two nullable single-slot foreign keys; `CaseWorkflowModelConfiguration.cs:39-40` (unique indexes) and `:46-47` (the restrict-delete relationships).
  - `src/Pegasus.Infrastructure/Persistence/EfCaseWorkflowStore.cs:441` (`RecordReportApprovalAsync` — old approval rows survive pointer replacement but the projection returns only the current one), `:475` and `:891` (the two callers of the link evaluation), `:529` (`UnlinkReportEvidenceAsync` — clears case, link time and linking actor), `:926` (`EvaluateReportEvidenceLinkAsync` — checks chronology against the current approval only, and never binds the Sent item to the approved artifact).
  - `src/Pegasus.Core/Workflow/ApprovedMailboxReportSentEvidence.cs`, `src/Pegasus.Core/Workflow/PollSentEvidence.cs`, `src/Pegasus.Infrastructure/Persistence/EfCaseReportSentEvidenceStore.cs` — the exact-source retention and guarded auto-link flow to reuse; the defect is the association model, not discovery.
  - Tests to extend: `tests/Pegasus.IntegrationTests/CaseWorkflowPersistenceTests.cs`, `CaseWorkflowMigrationTests.cs`, `LocalDurableApprovedSentSourceTests.cs`, `SentEvidencePollPersistenceTests.cs`; `tests/Pegasus.Core.Tests/Workflow/ApprovedMailboxReportSentEvidenceTests.cs`, `PollSentEvidenceTests.cs`.
  - `src/Pegasus.Infrastructure/Persistence/Migrations/` and `PegasusDbContextModelSnapshot.cs`; `scripts/Test-MigrationGrants.ps1`.
  - `docs/desktop/06-ui-design/screen-specs.md:371-386` — §13.9's issued-version list and the `Case.Reports.*` AutomationIds.
- Governing documents that exist: `docs/frd/frd-11-reports-correspondence-and-reviewed-proposals.md` (immutability, correction, the draft/approval/Sent boundary), `docs/frd/frd-08-email-mailbox-and-background-processing.md` (exact Sent evidence fields, finality and proof limits), `docs/frd/frd-01-case-identity-and-lifecycle.md` (PostReport and reasoned reopen).
- Binding decisions: **L-01** the change is server-side inside `Pegasus.Web`'s solution, no new deployment unit. **L-02** the local stack is the verification environment; no mailbox, Graph, Box or Azure write is needed or authorised. **D-001** upstream freezes, so this defect does not close upstream.
- Provenance of the copy below: upstream area `documents-reports`, upstream status `preparing`, upstream profile `feature`, upstream labels `now, source-now`, upstream groups `EPIC-004`; read from the read-only clone of `collisionengineers/pegasus` branch `kanmer-board` at clone commit **`a5b28111`**, read date **2026-08-24**.

### Upstream ticket TICK-208 (verbatim)

```markdown
## What

Preserve final Sent evidence through post-report correction.

## Why

This remains an unresolved current-work item in the canonical Kanmer board; it is a planning/research unit until taken.

## Approach

- At activation, re-check the exact current source, caller, and evidence state before choosing an implementation path.
- Write the task-level plan first; do not infer authority for live, credential, mailbox, Box, Azure, or other external operations.

## Verification

- [ ] The task plan defines the owned change, failure behavior, tests, and acceptance evidence.
- [ ] Completion is recorded only at the evidence tier actually proved.

## Notes

- Source: the retired pre-Kanmer tracker — Next — renderer lifecycle defect.
- Related capability: CASE-23 ([[TICK-055]]).


## Tracker migration

Authority references were retargeted by [[KANMER-001]] after the legacy tracker was retired.
```

## Routing

- **Subagent**: `pegasus-gateway-dev` — `.codex/agents/pegasus-gateway-dev.toml`
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `dotnet-webapi` (dotnet/skills `98f84851`) → `run-tests`
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search` for EF Core migrations that drop a unique constraint while preserving data, and filtered unique indexes on SQL Server)
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (call `get_doc_gates <id>` before every move; a move crosses at most one gated boundary)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

The five upstream pipeline documents copied onto this ticket (`research`, `files`, `plan`, `checklist`, `open-questions`) are the requirement. The steps below are the upstream plan's eight steps re-expressed for the fork, where the prerequisite is the imported `upstream:DOCS-001` rather than upstream SIMPLI-014 plus DOCS-001, and where the operator-facing half lands on the desktop rather than on Razor pages.

1. Orient. Read this body in full including the verbatim upstream ticket, then the copied `research`, `files`, `plan`, `checklist` and `open-questions` documents. Read `docs/frd/frd-11-…md`, `docs/frd/frd-08-…md` and `docs/frd/frd-01-…md`. Call `get_doc_gates <this ticket id>`, then `take_ticket` on branch `task/upstream-tick-208-issued-version-ledger`.
2. **Reconcile against the prerequisite, and be willing to close no-code.** After the imported `upstream:DOCS-001` merges, re-read its diff, its plan, its proof and the exact Core report-version identity types and stores it created, and name them in `research`. If DOCS-001 already satisfies every acceptance criterion below, stop repository implementation and close this ticket from evidence — that outcome is a success, not a failure. Otherwise narrow the file map to the missing association slice before going further. Note the fork deviation from the upstream plan: upstream sequences after SIMPLI-014 *and* DOCS-001; on the fork the renderer half is [[DSK-07-14]] / [[DSK-07-16]] under **L-03**, so only DOCS-001 is a hard prerequisite.
3. Add the smallest Core version-specific association contract in `src/Pegasus.Core/Workflow/CaseWorkflowContracts.cs`: approval and link/reassociate requests name one immutable report version, and the projection exposes an ordered issued-version history carrying each version's artifact identity and hash, its approval, its predecessor and correction reason, and zero or one current exact Sent evidence plus permanent association history. Reuse DOCS-001's identity types and the existing `ApprovedMailboxReportSentEvidence` record; create **no** second report aggregate and **no** CASE-23 state model. Keep `CaseWorkflowRecord.ReportApproval` / `.ReportSentEvidence` as a current-version convenience if it is genuinely useful, but it stops being the durable authority.
4. Persist it append-only and migrate conservatively. Add the joining entity and configuration beside `CaseWorkflowEntities.cs` / `CaseWorkflowModelConfiguration.cs`, with uniqueness, chronology and concurrency constraints and non-destructive relationships; add the migration and regenerate `PegasusDbContextModelSnapshot.cs`. Every existing `CaseReportApprovals` and `CaseReportSentEvidence` row is preserved with its current case association. **Never fabricate an artifact/version match for a legacy row** — retain it with explicit legacy/unresolved provenance until a reasoned authoritative reconciliation can name the version. Add the runtime-role grant in the same migration; `pwsh ./scripts/Test-MigrationGrants.ps1` must pass.
5. Make staff linking and Worker auto-linking version-aware and fail closed, in `EfCaseWorkflowStore.cs` (`EvaluateReportEvidenceLinkAsync:926`, `:475`, `:891`) and `PollSentEvidence`: require an existing approved version, reject evidence predating its approval, reject duplicates and mismatched case/version/hash, preserve idempotent replay, and auto-link **only** on an authoritative artifact/version match — otherwise retain the evidence as unlinked for staff review. Reassociation appends actor, time, reason and before/after history and never clears the source item or the former association record. `UnlinkReportEvidenceAsync:529` stops erasing link metadata.
6. Preserve correction semantics without touching the lifecycle: a corrected or addendum version starts with no Sent status, its predecessor keeps its original final Sent item and time, and a new exact Sent item binds only to the successor. Keep the existing ReportPreparation / PostReport / reasoned-reopen checks as they are unless version identity mechanically requires a guard. Introduce no query, dispute, due, chaser, response, completion or closure behaviour — CASE-23 stays deferred, as the copied `open-questions` records.
7. **Re-expressed for the desktop.** Upstream step 6 adapts "existing staff mutation surfaces", which on the web are `Pages/Cases/**` — those pages are deleted by the conversion cut list. Keep the requirement and move it: expose the ordered issued-version history and its per-version Sent evidence through the **gateway** case reports projection that [[DSK-03-14]] and [[DSK-07-16]] own the routes for, and render it in the desktop Reports tab as the two separate columns §13.9 requires, with legacy or unresolved association shown explicitly as such. Add no send operation, no Outlook mutation and no delivery or read claim. Say in `plan` that you have re-expressed it.
8. Test migration, policy, persistence and concurrency in the fork's projects. `tests/Pegasus.Core.Tests/Workflow/ApprovedMailboxReportSentEvidenceTests.cs` and `PollSentEvidenceTests.cs` — version-specific binding while retaining exact-source validation; ambiguous or hash-mismatched auto-link stays unlinked and cannot overwrite prior final evidence. `tests/Pegasus.IntegrationTests/CaseWorkflowPersistenceTests.cs` — the original version and its Sent evidence survive a correction, the successor starts unsent, second evidence binds only to the successor, prior rows stay queryable, reasoned reassociation appends history, and a concurrent staff and Worker link yields one valid association. `tests/Pegasus.IntegrationTests/CaseWorkflowMigrationTests.cs` — legacy rows preserved with no fabricated version identity.
9. Verify locally under **L-02**, run the simplification pass over this branch diff, record it under a dated `## Simplification pass` heading in this ticket's `plan` document, update current-state documentation only to the tier actually proved, and open the PR into `dev` with a post-implementation report recording the exact migration and backfill behaviour and any deviation from the copied upstream plan.

## Acceptance criteria

- [ ] A corrected or addendum report creates a new issued version that starts **unsent**, and the predecessor retains its own final exact Sent evidence, time, approval and artifact hash unchanged.
- [ ] A new exact Sent item binds only to the version it issued; a generated artifact, Box upload, queue result, staff assertion or the prior version's Sent item can never mark a new version sent.
- [ ] The database can answer which immutable report version the final Sent item issued, for every version, not only the current one.
- [ ] Reasoned unlink and relink **append** association history and retain the former metadata and source identity; a correction is never modelled as an unlink.
- [ ] Auto-link is fail closed: without an authoritative artifact/version match the evidence stays unlinked for staff review.
- [ ] The migration preserves every existing approval and evidence row and its current case association, and fabricates no artifact/version match for legacy rows — they carry explicit unresolved provenance.
- [ ] CASE-23's post-report query and dispute lifecycle is unchanged: no query, dispute, due, chaser, response, completion or closure behaviour is introduced.
- [ ] The desktop Reports tab can show custody state and sent evidence as separate columns over real data, satisfying `screen-specs.md` §13.9 and [[DSK-07-16]] step 11.

## Verification

- [ ] `dotnet restore ./Pegasus.slnx --locked-mode` — expected: restore succeeds with no lock-file drift.
- [ ] `dotnet build ./Pegasus.slnx --configuration Release --no-restore` — expected: build succeeds.
- [ ] `dotnet test ./tests/Pegasus.Core.Tests/Pegasus.Core.Tests.csproj --configuration Release` — expected: version-specific approval, link, reassociation, chronology, idempotency, mismatch and ambiguity facts pass, and the unchanged-CASE-23 fact passes.
- [ ] `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --filter "Category!=Corpus&Category!=Browser"` — expected: append-only persistence, correction lineage, per-version Sent finality, migration backfill, projection commands, concurrency and retained exact-source facts pass.
- [ ] `pwsh ./scripts/Test-MigrationGrants.ps1` — expected: exits 0 with the new association table listed as granted.
- [ ] Migration script inspection — expected: existing rows preserved, no artifact/version relationship fabricated, rollback follows repository conventions; record the inspection in the post-implementation report.

## Evidence tier

Tier 4 — LocalDB persistence, with Tier 11 (migration/recovery) alongside it.
Tier 4 obliges state and action-history atomicity, constraints, leases, stale versions and concurrency on the new append-only association. Tier 11 obliges that every supported prior schema migrates idempotently, that previous artifacts stay compatible, and that reconciliation works by stable Outlook and Box identities — which is precisely the risk in replacing two unique single-slot foreign keys.

## Documentation changes

- `docs/frd/frd-11-reports-correspondence-and-reviewed-proposals.md` — clarify the per-issued-version binding between immutable artifact, approval and exact final Sent evidence, and distinguish correction from reassociation.
- `docs/current-architecture.md` — the issued-version ledger, after it ships.
- `docs/desktop/06-ui-design/screen-specs.md` — §13.9's issued-version list confirmed as implementable, with the legacy/unresolved state named.
- `docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md` — the `TICK-208` row annotated with this fork ticket id.

## Guardrails

- **Azure**: no write. No mailbox, Graph, Outlook or Box write either — the copied research is explicit that none is required, and discovery already works; the defect is the association model.
- **Scope boundary**: may touch `src/Pegasus.Core/Workflow/**`, `src/Pegasus.Infrastructure/Persistence/CaseWorkflowEntities.cs`, `CaseWorkflowModelConfiguration.cs`, `EfCaseWorkflowStore.cs`, `EfCaseReportSentEvidenceStore.cs`, a new migration plus the model snapshot, and `tests/Pegasus.Core.Tests` / `tests/Pegasus.IntegrationTests`. Must **not** create a second report aggregate (that is the imported `upstream:DOCS-001`), must **not** build the `/api/v1` routes or the desktop Reports UI (that is [[DSK-03-14]] and [[DSK-07-16]]), must **not** touch `src/Pegasus.Worker`'s discovery behaviour beyond making its auto-link version-aware, and must **not** settle CASE-23.
- **Blocks / blocked by**: this ticket **blocks** [[DSK-07-16]] (step 11's separately shown issued versions and sent evidence are not implementable over the single-slot columns, and its own Traps say so) and [[DSK-05-18]] (the slice cannot sign off a report path where a correction destroys the record of what was sent). It **is blocked by** the imported `upstream:DOCS-001`, whose immutable report-version identity it consumes. [[DSK-07-17]] step 7 must be amended from conditional to unconditional recreation — that amendment belongs to [[DSK-07-17]], not to this ticket.
- **Traps**: dropping the two unique single-slot foreign keys is a migration with real data behind it — preserve and never fabricate; `UnlinkReportEvidenceAsync` currently clears the evidence row's case, link time and linking actor, and that erasure is the defect, so appending history is the fix rather than a nicety; an e-mail may mention a case without authoritatively identifying the artifact version, so auto-link must fail closed; correction tempts lifecycle design — every query, dispute, due, chaser and closure question stays with CASE-23; and a new table without its `Grant*` migration fails `scripts/Test-MigrationGrants.ps1` in CI (upstream PLAT-035).
- **Open questions**: none for the preservation invariant — the copied `open-questions` document records CASE-23 as explicitly parked, not unresolved. Do not manufacture an operator question here.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.

## Outcome

_Filled at closeout._
