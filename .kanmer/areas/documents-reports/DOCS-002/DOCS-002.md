---
id: DOCS-002
type: ticket
title: >-
  upstream:TICK-018 · DOC-02 — Store source emails, instruction documents,
  images, correspondence, and reports in Box
status: backlog
area: documents-reports
assignee: ''
profile: feature
labels:
  - capability
  - DOC-02
  - now
  - requires-live-approval
  - upstream-carryover
  - upstream-TICK-018
  - gateway-worker-ticket
  - needs-operator
groups:
  - EPIC-014
links: []
blocks:
  - FEAT-037
  - FEAT-014
refs:
  - docs/frd/frd-05-documents-extraction-and-custody.md
docs_todo: true
archived: false
created: '2026-08-24T11:41:20.436Z'
updated: '2026-08-24T11:57:50.778Z'
---

## What

Close the two remaining DOC-02 gaps, and only those two: **automatic Box retention of case correspondence when a later inbound e-mail is associated to a case**, and **automatic Box retention of outbound sent evidence**. Both are delivered as new durable custody work kinds keyed on the association row and the sent-evidence row, reusing the existing outbox convention, `BoxCaseCustody` and `BoxDocumentContentStore`. The ticket also settles one FRD-05 interpretation: whether DOC-02 requires per-attachment files in Box or the retained `.eml` satisfies it.

## Why

DOC-02 is allocated `Now / 0.1.0-alpha.1` and its report half is now closed by [[DSK-07-16]], which stores the finalised PDF through the existing custody path as a normal case document version with `DocumentSemanticRole.EngineerReport` and `DocumentSource.Generated`. The **correspondence** half is closed by nothing. A grep across the whole `docs/desktop/` plan set returns the word "correspondence" only inside the carry-over table row itself, and [[DSK-07-11]]'s outbound seam treats sent evidence as an **audit record**, not as Box retention. The upstream research is precise about the defect: later inbound e-mails associated to a case are retained in SQL and blob (`IntakeAssets`, `RetainedMailboxMessages`) but "no custody work is enqueued on association" — only acceptance and replacement enqueue case custody — and outbound `SentEmailEvidence` is SQL-only.

The operator-visible consequence is a Box case folder that is missing exactly the material an operator goes to Box to find: the correspondence trail after the instruction, and the evidence of what was sent. Blob is temporary hot staging only under FRD-05, so "it is in blob" is not custody.

This is a real implementation — a new durable work kind plus sent-evidence rows on the existing outbox convention — not a sentence that can be added to another ticket's acceptance. The seeded tickets that own the neighbouring surfaces cannot deliver it: [[DSK-05-14]] and [[DSK-07-05]] own the *browser and broker* over case documents, not the automatic retention trigger, and [[DSK-07-11]] explicitly scopes sent evidence as an audit record. So without this ticket the slices sign off DOC-02 parity while half the capability is absent.

## Source of truth

- Import decision: coverage decision § Plan gaps — "Automatic Box retention of case correspondence and outbound sent evidence — half of the DOC-02 capability that is allocated `now` — is delivered by nothing"; § Import list row `TICK-018` ("scoped to the two named gaps only… linked to FEAT-042 so the report half is not rebuilt; keep requires-live-approval").
- Fork board neighbours: [[DSK-07-16]] (closes the report half — do not rebuild it), [[DSK-07-11]] (outbound sent-evidence seam), [[DSK-05-14]] and [[DSK-07-05]] (documents and Box broker), [[DSK-05-20]] / [[DSK-07-04]] (where a failed custody item and its staff retry appear), [[DSK-01-09]] (the carry-over pass that files this).
- Repository evidence, fork `main`, read 2026-08-24:
  - `src/Pegasus.Core/Custody/ExternalWorkProcessing.cs:6-13` — `ExternalWorkKinds` today holds `create_case_custody`, `create_audit_reference_custody`, `create_image_case_custody`, `merge_image_case_custody`, `vehicle_lookup`; `:84-90` is the fail-closed kind dispatch in `ProcessQueuedExternalWork`.
  - `src/Pegasus.Core/Custody/CustodyContracts.cs:8-13` — `CustodyWorkKind` is `CreateCaseRoot`, `RetainAcceptedIntakeSource`, `CreateAuditReferenceFolder`; there is no correspondence or sent-evidence member.
  - `src/Pegasus.Infrastructure/Persistence/EfCaseAcceptanceStore.cs:384-397` — the only acceptance enqueue of `create_case_custody`, with its `OperationKey` shape `case-custody:{caseId:N}`; `src/Pegasus.Infrastructure/Persistence/EfLinkedCaseReplacementStore.cs:212` — the replacement path.
  - `src/Pegasus.Infrastructure/Persistence/EfIntakeMutationStore.cs:273` (`LinkAsync`), `:434` (`AutoLinkAsync`) — the association transactions that enqueue **no** custody work today.
  - `src/Pegasus.Infrastructure/Persistence/PegasusDbContext.cs:46` and `:1241` — `SentEmailEvidence` / `SentEmailEvidenceEntity`; `:83` `IntakeAssets`; `:106` `RetainedMailboxMessages`; `:27` `ExternalWorkItems`.
  - `src/Pegasus.Infrastructure/Persistence/EfQueuedCustodyProcessor.cs`, `src/Pegasus.Infrastructure/Custody/BoxCaseCustody.cs`, `BoxDocumentContentStore.cs`, `CustodyNames.cs`, `LocalCaseCustody.cs`, `LocalDocumentContentStore.cs` — the adapters to reuse, and the local pair the DevelopmentOffline stack uses.
  - `tests/Pegasus.IntegrationTests/CustodyOutboxIntegrationTests.cs`, `DocumentCustodyDurabilityTests.cs`, `CaseCustodyWebTests.cs`, `ImageCaseCustodyIntegrationTests.cs`, `LocalCaseCustodyAtomicWriteTests.cs` — the existing durability patterns to extend.
  - `docs/operations.md#approved-box-integration-test-target` — the only Box subtree a live check may write to.
- Governing document that exists: `docs/frd/frd-05-documents-extraction-and-custody.md` (Box is the accepted case-file custody system; blob is temporary hot staging only; a Box failure keeps the case `Not ready` with staff-initiated retry; closed cases are read-only).
- Binding decisions: **L-01** the gateway is `Pegasus.Web` evolved in place — the retention work is server-side and no desktop holds a Box credential. **L-02** the local DevelopmentOffline stack with `LocalCaseCustody` / `LocalDocumentContentStore` is the verification environment; a live Box check is a separately approved operator step. **D-001** upstream freezes at the first production gateway change, so DOC-02 does not close upstream.
- Provenance of the copy below: upstream area `documents-reports`, upstream status `preparing`, upstream profile `feature`, upstream labels `capability, DOC-02, now, requires-live-approval`, upstream groups `HZN-003`; read from the read-only clone of `collisionengineers/pegasus` branch `kanmer-board` at clone commit **`a5b28111`**, read date **2026-08-24**.

### Upstream ticket TICK-018 (verbatim)

```markdown
## What

Plan and research **DOC-02**: Store source emails, instruction documents, images, correspondence, and reports in Box

## Why

The capability inventory allocates this outcome to **Now / 0.1.0-alpha.1**. This is a current allocation with incomplete evidence or activation work; plan the remaining caller, contract, and acceptance proof before implementation.

## Approach

- Establish the current Core policy owner, real caller, persistence/infrastructure boundary, and acceptance evidence before proposing implementation.
- Recover and resolve the stated activation boundary without treating allocation, registration, or a build as deployment or acceptance.

## Verification

- [ ] A task-level plan records the exact feature contract, caller, failure behavior, and required tests.
- [ ] The activation criteria have been satisfied or explicitly accepted before implementation begins.

## Notes

- Source: `docs/capabilities.md` — DOC-02.
- Canonical owner: [Owning FRD](docs/frd/frd-05-documents-extraction-and-custody.md#documents-extraction-and-custody)
- Activation/boundary: Day-one accepted Case custody requirement. Blob is temporary hot staging only; preserve the approved test-target scope for local and non-production deployment evidence.
```

## Routing

- **Subagent**: `pegasus-gateway-dev` — `.codex/agents/pegasus-gateway-dev.toml`
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `dotnet-webapi` (dotnet/skills `98f84851`) → `run-tests`
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search` for EF Core transactional outbox patterns and SQL Server unique filtered indexes)
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (call `get_doc_gates <id>` before every move; a move crosses at most one gated boundary)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Orient. Read this body in full including the verbatim upstream ticket, then the upstream `research` document copied onto this ticket — it is the requirement and it enumerates exactly what is live and exactly what is missing. Read `docs/frd/frd-05-documents-extraction-and-custody.md` in full. Read [[DSK-07-16]] and confirm for yourself that the report half is closed there. Call `get_doc_gates <this ticket id>`, then `take_ticket` on branch `task/upstream-tick-018-correspondence-custody`.
2. Re-verify the copied research's "live" claims against the **fork tree**, not against upstream production, and record the result in `research`: acceptance enqueues `create_case_custody` (`EfCaseAcceptanceStore.cs:384-397`), replacement enqueues it too (`EfLinkedCaseReplacementStore.cs:212`), `EfQueuedCustodyProcessor` dispatches to `BoxCaseCustody`, and `AddProductionBoxCustody` composes `IDocumentContentStore` → `BoxDocumentContentStore`. Confirm by reading `EfIntakeMutationStore.LinkAsync` (`:273`) and `AutoLinkAsync` (`:434`) that association enqueues nothing — that is the defect.
3. **State the scope in `plan` before writing code.** This fork ticket carries the two named gaps only. Explicitly out of scope and recorded as such: the report half (owned by [[DSK-07-16]]), case custody roots and source-email retention (already live), and the image-initiated custody slice (its remaining item is a deploy-stage production verification, not code). Do not re-plan DOC-02 as a whole.
4. Add one new durable work kind for correspondence retention. Extend `ExternalWorkKinds` in `src/Pegasus.Core/Custody/ExternalWorkProcessing.cs` and add the matching member to `CustodyWorkKind` in `src/Pegasus.Core/Custody/CustodyContracts.cs`, following the existing `create_case_custody` / `create_image_case_custody` naming — name it in `plan` rather than guessing here. Extend the fail-closed dispatch at `ExternalWorkProcessing.cs:84-90`; an unknown persisted kind must still fail closed and must never be treated as custody by default.
5. Enqueue it from the association transactions themselves — `EfIntakeMutationStore.LinkAsync` and `AutoLinkAsync` — in the same transaction that records the association, with a deterministic operation key derived from the association row so a replayed association enqueues nothing new. Follow the exact shape used at `EfCaseAcceptanceStore.cs:384-397`. Done looks like: an integration test that links a retained mailbox message to a case and finds exactly one pending `ExternalWorkItems` row for the new kind.
6. Do the same for outbound sent evidence: enqueue a retention work item from the transaction that writes the `SentEmailEvidence` row (`PegasusDbContext.cs:46`, `:1241`), keyed on that row. Reuse the same outbox convention; do not add a second scheduling mechanism.
7. Implement the handler over the existing adapters — `BoxCaseCustody` for the case-folder placement and `BoxDocumentContentStore` for the versioned file — and give `LocalCaseCustody` / `LocalDocumentContentStore` the same behaviour so the DevelopmentOffline stack can prove it under **L-02**. Failure behaviour follows FRD-05 for case-scoped custody: an explicit named failure plus staff-initiated retry, not a silent automatic business retry.
8. **Re-expressed for the desktop.** FRD-05's staff retry lives today on a Razor page the conversion deletes. Keep the requirement and move it: the failed retention item and its Retry command surface through the operations projection that [[DSK-05-20]] and [[DSK-07-04]] already own (`Operations.External.Table`, `Operations.External.Retry`), so this ticket adds the work kind and its named failure reason to that projection rather than building a screen. Say in `plan` that you have done so.
9. **Operator step** — settle the FRD-05 interpretation the upstream research parks: does DOC-02 require each attachment of the accepted source exploded into Box as its own file, or does the retained `.eml` (which contains the attachments) satisfy it? FRD-05's wording reads as satisfied by the retained source for day one, but the operator may expect visible per-file content. Record the answer in this ticket's `open-questions` document and, if the answer is per-file, raise it as its own follow-up ticket rather than widening this one. Evidence the operator hands back: one sentence, and the FRD-05 clause it amends if any.
10. Test in the projects that exist on the fork: extend `tests/Pegasus.IntegrationTests/CustodyOutboxIntegrationTests.cs` and `DocumentCustodyDurabilityTests.cs` for enqueue-on-association, enqueue-on-sent-evidence, replay creating no second item, a dependency failure producing a named failure with a staff-retryable state, and a closed case remaining read-only. Add Core policy tests in `tests/Pegasus.Core.Tests` for the new kind's dispatch and failure-code classification.
11. **Operator step** — the live Box verification. Any live check writes only to the approved disposable subtree recorded at `docs/operations.md#approved-box-integration-test-target`; the `requires-live-approval` label stands. The operator hands back: the approval, the subtree used, and the folder listing showing the retained correspondence item and the retained sent-evidence item. Nothing outside that subtree is written.
12. Run the simplification pass over this branch diff, record it under a dated `## Simplification pass` heading in this ticket's `plan` document, update `docs/capabilities.md`'s DOC-02 row to the tier actually proved, and open the PR into `dev`.

## Acceptance criteria

- [ ] Associating a later inbound e-mail to a case enqueues exactly one durable retention work item in the same transaction as the association, and a replayed association enqueues none.
- [ ] Recording outbound sent evidence enqueues exactly one durable retention work item keyed on that evidence row.
- [ ] The retained correspondence item and the retained sent-evidence item appear in the case's Box folder through `BoxCaseCustody` / `BoxDocumentContentStore`, and in the local artifact root through the local pair.
- [ ] A retention failure is explicit and named, is staff-retryable through the operations projection of [[DSK-05-20]], and does not silently auto-retry as if it were image custody.
- [ ] The report half is untouched: no second path stores the finalised PDF, and [[DSK-07-16]]'s registration remains the only one.
- [ ] The per-attachment interpretation of step 9 is answered by the operator and recorded before the ticket leaves Preparing.
- [ ] No Box write occurs outside the approved test subtree, and no desktop holds a Box credential.

## Verification

- [ ] `dotnet build ./Pegasus.slnx --configuration Release` — expected: build succeeds with the new work kind in Core and the handler in Infrastructure.
- [ ] `dotnet test ./tests/Pegasus.Core.Tests/Pegasus.Core.Tests.csproj --configuration Release` — expected: the new kind dispatches to exactly one handler, an unknown kind still fails closed, and the failure-code classification facts pass.
- [ ] `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --filter "Category!=Corpus&Category!=Browser"` — expected: enqueue-on-association, enqueue-on-sent-evidence, replay, named-failure-with-staff-retry and closed-case-read-only facts pass against the local custody adapters.
- [ ] `pwsh ./scripts/Test-MigrationGrants.ps1` — expected: exits 0 (run it even if no table is added, because a new work kind often brings one).
- [ ] **Operator step** — the approved-subtree Box run of step 11: expected observable result is the two retained items listed in the disposable subtree named in `docs/operations.md#approved-box-integration-test-target`, and nothing written outside it.

## Evidence tier

Tier 4 — LocalDB persistence, with Tier 12 (integrated workflow) for the operator-approved live run.
Tier 4 obliges proof that the enqueue is atomic with the association, that replay is idempotent, and that a failure leaves a retryable state rather than a lost item. Tier 12 obliges the authenticated source receipt through Core, SQL and outbox, the actual processor trigger, the adapter outcome and the persisted operator view — which only the approved live run can show, and only within the approved subtree.

## Documentation changes

- `docs/frd/frd-05-documents-extraction-and-custody.md` — the automatic correspondence and sent-evidence retention clause, and the per-attachment interpretation settled in step 9.
- `docs/capabilities.md` — the `DOC-02` row updated to the tier actually proved; do not claim deployment.
- `docs/current-architecture.md` — the new work kind in the custody outbox description, after it ships.
- `docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md` — the `TICK-018` row annotated with this fork ticket id and the narrowed scope.

## Guardrails

- **Azure**: no write. ⚠ **Box write** (not Azure): the live verification of step 11 writes only inside the approved disposable subtree at `docs/operations.md#approved-box-integration-test-target` and needs exact-target operator approval per `docs/runbook.md` § Live operation approval matrix. The `requires-live-approval` label stands.
- **Scope boundary**: may touch `src/Pegasus.Core/Custody/**`, `src/Pegasus.Infrastructure/Custody/**`, `src/Pegasus.Infrastructure/Persistence/EfIntakeMutationStore.cs`, `EfQueuedCustodyProcessor.cs`, the sent-evidence store, any migration this needs, and the two test projects. Must **not** re-implement report retention (that is [[DSK-07-16]]), must **not** change `src/Pegasus.Worker`, must **not** widen into DOC-02 as a whole, and must **not** give any desktop client a Box credential — retention is server-side under **L-01**.
- **Blocks / blocked by**: this ticket **blocks** [[DSK-07-11]] (its outbound seam records sent evidence as an audit record and would sign off with no Box retention behind it) and [[DSK-05-14]] (the documents-and-custody slice would claim DOC-02 parity while case correspondence never reaches Box). It is **not** blocked by [[DSK-07-05]]; the Box broker endpoints serve the desktop browser, while this retention path is server-side and already has its adapters.
- **Traps**: blob is hot staging only, so "the message is in `IntakeAssets`" is not custody; the image-custody re-arm policy in `ImageCustodyRetryPolicy` is deliberately automatic and must **not** be copied onto case-scoped correspondence custody, which FRD-05 requires to fail explicitly with a staff retry; an unknown persisted work kind must keep failing closed; and a closed case stays read-only, so a late association to a closed case must be refused rather than written.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.

## Outcome

_Filled at closeout._
