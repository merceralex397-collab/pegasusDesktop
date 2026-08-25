---
id: FEAT-042
type: ticket
title: >-
  DSK-07-16 · Report finalise endpoint: register the desktop-rendered PDF into
  custody with the report record and audit
status: preparing
area: desktop-features
assignee: ''
profile: feature
stageEntered:
  preparing: '2026-08-24T21:31:45.963Z'
labels:
  - desktop-conversion
  - plan-07
  - phase-7
  - tier-5
groups:
  - EPIC-008
  - HZN-008
links: []
refs:
  - docs/frd/frd-11-reports-correspondence-and-reviewed-proposals.md
docs_todo: true
archived: false
created: '2026-08-24T08:30:09.663Z'
updated: '2026-08-25T00:29:28.381Z'
---

## What

Add `POST /api/v1/cases/{caseId}/reports` (register a finalised report) and `GET /api/v1/cases/{caseId}/reports/{reportId}/content`, so a PDF rendered on the desktop is uploaded once through the gateway into Box custody with its report record, approval evidence and audit — and build the desktop preview and finalise flow that calls them, with FRD-11's finality and regeneration rules enforced server-side.

## Why

Proposal § 12.5 ends the local-render story with "final output is uploaded to the canonical store and registered through the gateway"; § 13.9 requires PDF preview and finalisation, storage and retrieval of final reports, regeneration rules and audit. The endpoint map records this pair as **new for L-03** — today the web keeps the rendered draft server-side and `src/Pegasus.Web/Pages/Cases/Assessment/Index.cshtml.cs:277-320` streams the draft bytes straight back to the browser without storing them. Core already models the approval of an immutable artifact by identity and SHA-256, so the finalise path must bind to that rather than inventing a parallel notion of "final". L-03 also moved rendering to the client **without** moving the readiness gate with it, so without this ticket's server-side re-check a desktop can render and register a PDF for an incomplete, unaccepted assessment (upstream DOCS-001, board [[DOCS-001]]). Siblings: [[DSK-07-14]] renders the PDF, [[DSK-07-15]] gates the switchover, [[DSK-07-05]] owns the custody plumbing, [[DSK-05-18]] is the slice.

## Source of truth

- Plan row: `docs/desktop/07-integrations/README.md` § 5 — `DSK-07-16`
- Plan context: `docs/desktop/07-integrations/README.md` § 4 Target state (fourth bullet), § 8 Documentation changes
- Endpoint map: `docs/desktop/03-gateway-api-and-data/endpoint-map.md` § `Cases` — the `Assessment` rows `POST /cases/{id}/reports/draft` and `POST /cases/{id}/reports` (register final) / `GET /cases/{id}/reports/{rid}/content`
- Screen spec: `docs/desktop/06-ui-design/screen-specs.md` § `§13.9 Assessment, valuation and reporting` (Reports tab: Generate, Preview, Finalise/Send reasoned and idempotent, issued versions with custody and sent evidence shown separately, regeneration rules surfaced as named enabled/disabled conditions)
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 12.5 Documents, PDFs and reports, § 13.9 Assessment, valuation and reporting
- Repository evidence: `src/Pegasus.Web/Pages/Cases/Assessment/Index.cshtml.cs:277-320` (`OnPostGenerateReportDraftAsync` — operation-key validation, the `NotReady` reasons list, and `File(assessmentPdf.Pdf, "application/pdf", ...)` with no storage), `:583` (`OnPostSendAsync`); `src/Pegasus.Core/Reports/AssessmentReportProjection.cs:306-310` (`AssessmentReportDraftPreparation` and `CanGenerate`), `:312-322` (`GenerateCaseAssessmentReportDraftOutcome` and `GenerateCaseAssessmentReportDraftResult`), `:331-362` (`GenerateCaseAssessmentReportDraft.PrepareAsync` and `ExecuteAsync`, which refuse with `AssessmentReadinessItem` reasons unless `AssessmentReportProjection.Project(input).IsReady`); `src/Pegasus.Core/Workflow/CaseWorkflowContracts.cs:62-79` (`ReportApprovalEvidence` and `ReportApprovalSubmission` — caller supplies `ApprovalId`, `ArtifactIdentity`, `ArtifactSha256`; the boundary assigns actor and time), `:229-236` (`RecordCaseReportApprovalRequest`), `:365`, `:420` (`IRecordCaseReportApproval`); `src/Pegasus.Core/Lifecycle/CaseLifecycle.cs:160-180`, `:448` (`ValidateReportApproval`); `src/Pegasus.Core/Documents/DocumentContracts.cs:66-84` (`AddCaseDocumentCommand`, `AddCaseDocumentResult.IsReplay`), `DocumentSemanticRole.EngineerReport`, `DocumentSource.Generated`; `src/Pegasus.Core/Reports/AssessmentReportRendering.cs:272-282` (`RenderedReportArtifact` provenance the register call carries); `tests/Pegasus.IntegrationTests/CaseReportApprovalWebTests.cs`, `tests/Pegasus.IntegrationTests/Reports/AssessmentReportDraftWebTests.cs`
- Upstream evidence — **both are imported, and one of the two board ids does not match its upstream id; always cite them as written here:**
  - **upstream DOCS-001 (board [[DOCS-001]])** — readiness and idempotency are Core-owned and fail closed on missing, unaccepted or ambiguous required data; its own research records that no durable report request/version aggregate, payload-identity, generation-state, attempt, lease or failure tables exist. The board id happens to match the upstream id here; that is a coincidence, not a rule.
  - **upstream TICK-208 (board [[DOCS-003]])** — Core carries one `ReportApprovalId` and one `ReportSentEvidenceId` per case, so a corrected version currently risks replacing the earlier pointer; its plan makes the durable authority an append-only issued-version-to-Sent-evidence association with reasoned history.
  **Board `DOCS-003` is upstream TICK-208. It is not upstream `DOCS-003`, which is an unrelated post-alpha RPT-04 activation gate with no fork ticket at all** — writing `[[DOCS-003]]` when you mean that gate points a reader at this ticket's Sent-evidence dependency instead. Board `DOCS-002` is upstream TICK-018 and is not involved here. [[DSK-01-09]] step 3 holds the join table.
- Binding decisions: **L-03 / ADR-0108** — the desktop renders, the gateway stores; the gateway renderer stays behind a flag until [[DSK-07-15]] signs off. L-01 — the endpoint lives in `Pegasus.Web`. **ADR-0107** — the PDF reaches Box through the gateway; no Box credential is used by the desktop.
- Depends on: `DSK-07-05` the custody upload path; `DSK-07-14` the desktop renderer producing the artifact and its provenance; **upstream TICK-208 (board [[DOCS-003]])** — already imported onto this board, unconditionally, so it needs finding by that board id rather than creating (see [[DSK-07-17]] step 7) — the append-only issued-version to Sent-evidence ledger, without which step 11's separately shown issued versions are not implementable

## Routing

- **Subagent**: `pegasus-gateway-dev` — `.codex/agents/pegasus-gateway-dev.toml`; `winui-dev` — `.codex/agents/winui-dev.toml` for the preview and finalise surface
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `dotnet-webapi` (dotnet/skills `98f84851`) → `minimal-api-file-upload` (dotnet/skills `98f84851`) → `winui-design` (`.codex/skills/winui-design/SKILL.md`) → `run-tests`
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search` for streamed request bodies and `IFormFile` alternatives in minimal APIs)
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (call `get_doc_gates <id>` before every move; a move crosses at most one gated boundary)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Orient: read the plan row, the endpoint map's two Assessment report rows, the Reports paragraph of the §13.9 screen spec, and `docs/frd/frd-11-reports-correspondence-and-reviewed-proposals.md` in full — FRD-11 owns correction and finality, and this ticket must not restate or contradict it. Read the two imported upstream tickets named under Source of truth by their board ids, [[DOCS-001]] and [[DOCS-003]]. Call `get_doc_gates <this ticket id>`, then `take_ticket` on branch `task/dsk-07-16-report-finalise`.
2. Record in `research` exactly what "final" means today: `ReportApprovalSubmission` binds an approval to an `ArtifactIdentity` and an `ArtifactSha256`, and the authenticated boundary assigns the actor and time (`CaseWorkflowContracts.cs:72-79`). The finalise endpoint must therefore store the artifact **first**, then approve *that* identity — never approve bytes that were not stored.
3. Close the readiness hole L-03 opened, on **both** paths, over the one existing Core rule (`GenerateCaseAssessmentReportDraft` and `AssessmentReportProjection.Project`, `src/Pegasus.Core/Reports/AssessmentReportProjection.cs:306-362`) — never a second readiness implementation and never a client-side check. `POST /api/v1/cases/{caseId}/reports/draft` returns a projection only for a complete, accepted assessment and otherwise returns the named `NotReady` reasons: each `AssessmentReadinessItem`'s `Requirement` and `WhyOutstanding` enumerated in the problem response, not collapsed into one generic refusal. `POST /api/v1/cases/{caseId}/reports` re-checks that readiness server-side before storing — a desktop-rendered PDF for a not-ready assessment is refused, not registered. The register path re-runs the check itself rather than trusting that the client called draft first; a case that became not-ready between render and finalise is refused with the same named reasons. Record in `plan` which problem type carries them.
4. Implement `POST /api/v1/cases/{caseId}/reports` accepting the rendered PDF as a streamed body plus `fileName`, `sha256`, `pageCount`, `templateVersion`, `engineVersion`, `expectedVersion`, `editLeaseToken` and `operationKey`. Verify the server-computed SHA-256 of the received bytes equals the client-declared `sha256`; on mismatch return `urn:pegasus:problem:validation` and store nothing.
5. Store through the existing custody path from [[DSK-07-05]] — `IAddCaseDocument` with `DocumentSemanticRole.EngineerReport` and `DocumentSource.Generated` — so the report becomes a normal case document version with custody status, not a special-cased blob. Reuse `AddCaseDocumentResult.IsReplay` for idempotency; a replayed `operationKey` returns the original report id and stores nothing new.
6. Record the report record and approval in the same operation: call `IRecordCaseReportApproval` with the stored artifact's identity and hash, letting `CaseLifecycleRules.ValidateReportApproval` enforce the rules. Do not add a second finality concept; FRD-11 owns it.
7. Implement `GET /api/v1/cases/{caseId}/reports/{reportId}/content` over the document content store with `Content-Length`, `ETag`, range support, `nosniff` and a safe filename — the same guarantees as the document download endpoint.
8. Surface regeneration rules as named conditions rather than a disabled button: the response for the case's reports section states, per FRD-11, whether regeneration is permitted and why not when it is not. The desktop renders those names; it does not compute them.
9. Keep the gateway renderer reachable behind its flag: `POST /api/v1/cases/{caseId}/reports/draft` continues to return gateway-rendered bytes until [[DSK-07-15]]'s results table signs parity off. Record the flag name and who may flip it in `plan`; do not remove `AddPegasusReportRendering` in this ticket.
10. Build the desktop Reports flow in `src/Pegasus.Desktop`: Generate (local render via [[DSK-07-14]] with progress in the status bar and cancel), Preview in an in-app PDF document viewer (a document viewer, never Pegasus UI in a WebView), and Finalise as a reasoned, idempotent command that uploads and registers. Use the AutomationIds from the screen spec — `Case.Reports.Generate`, `Case.Reports.Preview`, `Case.Reports.Send`. Render the server's named `NotReady` reasons; the desktop never decides readiness for itself.
11. Show issued versions with custody state and sent evidence as **separate** columns, reading the append-only issued-version-to-Sent-evidence association from **upstream TICK-208 (board [[DOCS-003]])**. An approved report is not a sent report — `ReportApprovalEvidence`'s own summary says so, and only retained Sent evidence proves a send ([[DSK-07-11]]). While Core still carries one `ReportApprovalId` and one `ReportSentEvidenceId` per case this column pair cannot be honest, so it does not ship ahead of that ticket; record the sequencing in `plan`.
12. Write contract tests in `tests/Pegasus.Api.ContractTests`: success stores exactly one document version and one approval; a not-ready assessment is refused by both the draft and the register endpoint with the named reasons and stores nothing; hash mismatch stores nothing; replayed key returns the original id with no second version; unauthorised actor refused; stale `expectedVersion` → `409`; regeneration refused when FRD-11 forbids it, with the named condition returned; the content endpoint honours range and `ETag`.
13. Write an integration test following `tests/Pegasus.IntegrationTests/CaseReportApprovalWebTests.cs` and the custody durability tests: a finalise on the local stack produces one Box-custody document version, one report record, one approval row and one action-history entry — and an interrupted upload produces none of them.
14. Add view-model tests for generate/preview/finalise state, cancel during render, the not-ready reasons rendering as named requirements, and the disabled-with-named-reason regeneration case; add the finalise assertions to the `winapp ui` reports script from [[DSK-08-08]]. Then run the simplification pass over the branch diff, record it under a dated `## Simplification pass` heading in the plan document, and open the PR into `dev`.

## Acceptance criteria

- [ ] `POST /api/v1/cases/{caseId}/reports/draft` returns a projection only for a complete, accepted assessment and otherwise returns the named `NotReady` reasons, and `POST /api/v1/cases/{caseId}/reports` re-checks that readiness server-side before storing — a desktop-rendered PDF for a not-ready assessment is refused, not registered.
- [ ] A finalised report is stored exactly once as a case document version with `EngineerReport` / `Generated` classification, with custody state visible.
- [ ] The approval binds to the stored artifact's identity and server-verified SHA-256; a hash mismatch stores nothing.
- [ ] Replay of the same `operationKey` returns the original report id and creates no second version or approval.
- [ ] Regeneration rules come from FRD-11 as named conditions; the desktop renders them and does not compute them.
- [ ] Approved and sent are shown separately; the desktop cannot assert that a report was sent.
- [ ] The gateway renderer remains reachable behind its flag until golden-file parity is signed off.
- [ ] No Box credential is used by the desktop; the PDF reaches custody only through the gateway.

## Verification

- [ ] `dotnet test ./tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj --configuration Release` — expected: store-once, not-ready-on-both-paths, hash-mismatch, replay, authorization, conflict and regeneration facts pass.
- [ ] `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --filter "Category!=Corpus&Category!=Browser"` — expected: one version, one report record, one approval, one audit row; the interrupted-upload fact leaves none; a not-ready case leaves none.
- [ ] `dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release` — expected: generate, preview, cancel, not-ready-reasons and finalise state facts pass.
- [ ] `pwsh ./tests/Pegasus.Desktop.UITests/ui-tests.ps1 -AppPid <pid> -Script reports` — expected: generate, preview and finalise assertions pass; screenshots attached.

## Evidence tier

Tier 5 — Web/API/MCP caller.
Tier 5 obliges route-level evidence that the real endpoints reach Core and custody with authentication, validation, idempotency, exception translation and the action-history actor observable.

## Documentation changes

- `docs/desktop/03-gateway-api-and-data/endpoint-map.md` — the register and content rows confirmed with their final shapes, including the readiness refusal on both the draft and register rows
- `docs/frd/frd-11-reports-correspondence-and-reviewed-proposals.md` — the desktop preview and finalise behaviour clause, including the server-side readiness gate on both paths
- `docs/current-architecture.md` — the renderer composition and the report-finalise path, after the slice ships
- `docs/capabilities.md` — `DSK` row for local report rendering and finalise

## Guardrails

- **Azure**: no write.
- **Scope boundary**: may touch `src/Pegasus.Web` (`/api/v1` case reports group), `src/Pegasus.Contracts`, `src/Pegasus.Desktop`, `src/Pegasus.Desktop.Infrastructure` and the test projects. Must not remove the gateway renderer registration, must not change FRD-11's finality rules, must not give the desktop a Box credential, and must not write a second readiness rule — `AssessmentReportProjection` in `src/Pegasus.Core` is the only one. The append-only issued-version ledger is **upstream TICK-208 (board [[DOCS-003]])**'s and is not built here.
- **Open question (operator), to be resolved and recorded in this ticket's `open-questions` document before step 3 is implemented**: **upstream DOCS-001 (board [[DOCS-001]])** records report generation as **automatic** — "detects a complete, accepted assessment, invokes the integrated renderer" — while `screen-specs.md` §13.9 and this ticket make Generate an **operator-initiated** `Case.Reports.Generate` command. Decide which is the desktop contract and record the answer; do not invent a hybrid, and do not implement automatic generation on the strength of the upstream wording alone.
- **Traps**: a new report-record table needs a runtime-role `Grant*` migration checked by `scripts/Test-MigrationGrants.ps1` (PLAT-035) — plan for it rather than discovering it in CI; approving bytes that were not stored breaks the artifact-identity binding; approved is not sent; readiness is enforced server-side on **both** the draft and the register path — a client-side check is not a gate, because L-03 moved the rendering and not the gate; the PDF preview must be a document viewer, never a WebView hosting Pegasus UI (proposal § 23.2, ADR-0108); the gateway renderer stays until [[DSK-07-15]] passes (L-03); separately shown issued versions and sent evidence depend on **upstream TICK-208 (board [[DOCS-003]])**'s ledger and must not be faked over the single-slot `ReportApprovalId` / `ReportSentEvidenceId`. **Upstream ids and fork board ids do not match, and this area holds the board's worst collision**: board `DOCS-003` is upstream TICK-208 (this ticket's ledger dependency), while upstream `DOCS-003` is an unrelated post-alpha RPT-04 activation gate with no fork ticket; board `DOCS-002` is upstream TICK-018; board `DOCS-001` is upstream DOCS-001 by coincidence. Always write `upstream <ID> (board <board-id>)`, never a bare `DOCS-0nn` or `TICK-nnn`.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.

## Outcome

_Filled at closeout._
