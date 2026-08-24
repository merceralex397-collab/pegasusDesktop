---
id: FEAT-018
type: ticket
title: 'DSK-05-18 · S18 Report generation, preview, finalise, send'
status: preparing
area: desktop-features
assignee: ''
profile: feature
stageEntered:
  preparing: '2026-08-24T21:31:37.169Z'
labels:
  - desktop-conversion
  - plan-05
  - phase-7
  - tier-2
  - tier-5
  - tier-7
  - tier-10
  - needs-operator
groups:
  - EPIC-006
  - HZN-008
links: []
blocks:
  - FEAT-022
  - FEAT-025
  - TEST-008
  - TEST-016
refs:
  - docs/frd/frd-11-reports-correspondence-and-reviewed-proposals.md
docs_todo: true
archived: false
created: '2026-08-24T07:57:08.747Z'
updated: '2026-08-24T21:31:37.169Z'
---

## What

Generate the assessment report and fee note on the desktop through the isolated non-UI WebView2 HTML→PDF path, preview it, finalise it (the canonical PDF uploaded and registered through the gateway) and send it with an idempotency key — keeping the gateway Playwright renderer available behind a flag until golden-file parity passes.

## Why

Proposal §12.5 and §13.9 and locked decision L-03 move report rendering to the desktop so generation is interactive, while canonical storage, audit and sending stay central. Today the draft is produced by `src/Pegasus.Web/Pages/Cases/Assessment/Index.cshtml.cs` `OnPostGenerateReportDraftAsync` (`:277`) and sent by `OnPostSendAsync` (`:583`), rendering through `Pegasus.Core.Reports.IAssessmentReportRenderer` → `src/Pegasus.Infrastructure/Reports/PlaywrightAssessmentReportRenderer.cs` (326 lines, Scriban templates, Playwright Chromium `PdfAsync`, PDFsharp post-processing; ADR-0025, ADR-0028). The Phase 7 exit gate requires approved fixtures to match and no required report to depend on the web renderer unless explicitly retained. Siblings: [[DSK-05-17]] supplies the assessment data, [[DSK-07-12]] the ADR-0108 decision, [[DSK-07-14]] the desktop renderer, [[DSK-07-15]] the golden-file suite.

## Source of truth

- Plan row: `docs/desktop/05-implementation-and-migration/README.md` § 5 — `DSK-05-18`
- Plan detail: `docs/desktop/05-implementation-and-migration/vertical-slices.md` § `S18 · Report generation, preview, finalise, send (DSK-05-18)`
- Reuse map: `docs/desktop/05-implementation-and-migration/reuse-map.md` § `Pegasus.Infrastructure` (the `Reports/` row — templates embedded by both Infrastructure and `Pegasus.Desktop.Infrastructure`, PDFsharp reusable as a package reference)
- Endpoint map: `docs/desktop/03-gateway-api-and-data/endpoint-map.md` § `Cases` (`POST /cases/{id}/reports/draft`, `POST /cases/{id}/reports`, `GET /cases/{id}/reports/{rid}/content`, `POST /cases/{id}/assessment/send`)
- Screen spec: `docs/desktop/06-ui-design/screen-specs.md` § `§13.9 Assessment, valuation and reporting — Case workspace › Assessment and Reports tabs`
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 12.5 Documents, PDFs and reports, § 13.9, § 23.2 Native verification
- Repository evidence: `src/Pegasus.Web/Pages/Cases/Assessment/Index.cshtml.cs:277` and `:583`; `src/Pegasus.Core/Reports/AssessmentReportProjection.cs` (362 lines), `AssessmentReportRendering.cs` (312 lines, `IAssessmentReportRenderer`); `src/Pegasus.Infrastructure/Reports/PlaywrightAssessmentReportRenderer.cs` (326 lines); templates and stylesheet at `docs/design/assets/report-renderer/templates/` (`assessment_report.scriban`, `assessment_fee_note.scriban`, `expert_report.scriban`, `fee_note.scriban`, `market_valuation_evidence.scriban`, `advert_evidence_pack.scriban`, `report.css`); `tests/Pegasus.IntegrationTests/Reports/AssessmentReportRendererTests.cs`, `tests/Pegasus.IntegrationTests/Reports/AssessmentReportDraftWebTests.cs`
- Binding decisions: L-03 report rendering moves to an isolated non-UI WebView2 HTML→PDF path and the gateway renderer is retained until golden-file parity passes (ADR-0108); L-01 the gateway registers the canonical copy and audits the send; L-02 verification and the performance run happen on the local Test/UAT workstation; L-04 routing named on the ticket
- Depends on: `DSK-05-17` the assessment data the projection reads; `DSK-07-12` accepted ADR-0108; `DSK-07-14` the `IAssessmentReportRenderer` implementation in `Pegasus.Desktop.Infrastructure`

## Routing

- **Subagent**: `winui-dev` — `.codex/agents/winui-dev.toml`; `pegasus-gateway-dev` — `.codex/agents/pegasus-gateway-dev.toml`; `pegasus-test-engineer` — `.codex/agents/pegasus-test-engineer.toml`; `pegasus-desktop-reviewer` — `.codex/agents/pegasus-desktop-reviewer.toml`
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `microsoft-code-reference` (Microsoft Learn plugin — verify the WebView2 print-to-PDF API before writing it) → `winui-dev-workflow` (`.codex/skills/winui-dev-workflow/SKILL.md`) → `winui-design` (`.codex/skills/winui-design/SKILL.md`) → `dotnet-webapi` (dotnet/skills `98f84851`) → `run-tests` → `winui-code-review` at review
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search`, `microsoft_code_sample_search`, `microsoft_docs_fetch`)
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (call `get_doc_gates <id>` before every move; a move crosses at most one gated boundary)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Orient: read the plan row, `vertical-slices.md` § S18, the reuse-map `Reports/` row, ADR-0108 as authored by [[DSK-07-12]], and `docs/frd/frd-11-reports-correspondence-and-reviewed-proposals.md` for the finality and regeneration rules. Call `get_doc_gates <this ticket id>`, then `take_ticket` with branch `task/dsk-05-18-reports` and worktree `../pegasus-worktrees/dsk-05-18-reports` from `origin/dev`.
2. Read `src/Pegasus.Core/Reports/AssessmentReportProjection.cs` and `AssessmentReportRendering.cs`, and `src/Pegasus.Infrastructure/Reports/PlaywrightAssessmentReportRenderer.cs`. Record in `research` the exact projection contract, the template names it selects from `docs/design/assets/report-renderer/templates/`, and the PDFsharp post-processing steps that must be reproduced. Record the SHA read.
3. Confirm [[DSK-07-14]] has landed an `IAssessmentReportRenderer` implementation in `src/Pegasus.Desktop.Infrastructure` and [[DSK-07-13]] embeds the same templates from one source with a hash check. If either is missing the ticket stays in Preparing — do not write a second renderer here.
4. Use `microsoft_code_sample_search` and `microsoft_docs_fetch` to confirm the current WebView2 print-to-PDF API surface and its exact method name before calling it (the plan set cites both `PrintToPdfAsync` and `PrintToPdfStreamAsync` — verify against official documentation rather than choosing one). Record the verified signature in the plan.
5. Confirm the endpoints from [[DSK-03-14]] and [[DSK-07-16]]: `POST /api/v1/cases/{id}/reports/draft` returns the **projection** for local rendering (and the gateway-rendered bytes while the flag selects the retained renderer), `POST /api/v1/cases/{id}/reports` registers the finalised PDF, `GET /api/v1/cases/{id}/reports/{rid}/content` serves it back, and `POST /api/v1/cases/{id}/assessment/send` carries an idempotency key and audits the provider message id.
6. Implement `ReportViewModel` in `src/Pegasus.Desktop`: fetch the projection, render locally through the injected `IAssessmentReportRenderer`, show a preview, and offer Finalise and Send as separate deliberate commands. Long rendering shows progress and stays cancellable (proposal §14.5).
7. Implement the renderer selection flag: while golden-file parity is unproven the gateway renderer remains selectable, and the flag — not a code change — chooses which path a given deployment uses. Record the flag name and its default in the plan document and hand them to [[DSK-07-12]] for ADR-0108's Consequences **before** the acceptance flip; this ticket makes no edit to ADR-0108.
8. Implement the WebView2-absent path: when the runtime is missing, show the guided message from [[DSK-04-09]]'s startup check and fall back to the gateway renderer rather than failing the workflow.
9. Implement Finalise: upload the rendered PDF through the transfer service from [[DSK-05-14]] and register it with `POST /api/v1/cases/{id}/reports`, so the canonical copy is stored once and its registration is audited. Regeneration follows the FRD-11 finality rules — a finalised report is never silently replaced.
10. Implement Send with a stable idempotency key generated once per user-initiated send and reused on retry; an uncertain outcome is resolved by re-querying the send status, never by resending.
11. Run the golden-file suite from [[DSK-07-15]]: for every approved fixture, compare the WebView2 output against the Playwright output on text, values, page count and key element positions within the documented tolerances. A failure blocks the parity claim, not the ticket's honesty — record the diff.
12. Add contract tests in `tests/Pegasus.Api.ContractTests` for draft, register, content and send: success, 401, 403, 409 stale version, replay of the send idempotency key returning the original outcome, and a finalised report refusing a silent overwrite. Enable `Features:DesktopGateway` explicitly.
13. **Operator step** — measure report generation on the baseline Test/UAT workstation and confirm the target from `docs/desktop/10-security-observability-performance/README.md`; have the operator confirm the final document and its audit trail are correct. Record figures, the workstation specification and the sign-off in the ticket proof.
14. Update `docs/desktop/01-inventory-and-parity/parity-matrix.md` row `PAR-15` (report portion), cross-reference FRD-11 from `docs/frd/frd-13-desktop-operator-experience.md`, run the simplification pass over the branch diff under a dated `## Simplification pass` heading, then open the PR into `dev`.

## Acceptance criteria

- [ ] The report and fee note render locally through the isolated non-UI WebView2 path using the same Scriban templates and `report.css` as the gateway renderer.
- [ ] Golden-file comparison against the Playwright output passes for the approved fixture catalogue, within documented tolerances.
- [ ] Preview is available and long rendering shows progress and remains cancellable.
- [ ] Finalise stores the canonical copy once through the gateway and audits its registration; a finalised report is never silently replaced.
- [ ] Send is idempotent and an uncertain outcome is resolved by re-query.
- [ ] WebView2 absent → guided message plus gateway-renderer fallback; the gateway renderer stays selectable until parity is signed off.
- [ ] WebView2 never hosts Pegasus UI — the architecture test proves it.

## Verification

- [ ] `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --no-build --filter "Category!=Corpus&Category!=Browser"` — expected: the golden-file report facts pass alongside the existing renderer tests in `tests/Pegasus.IntegrationTests/Reports/`.
- [ ] `dotnet test ./tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj --configuration Release --no-build` — expected: draft, register, content and send facts pass including idempotent replay.
- [ ] `dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release --no-build` — expected: preview, finalise, send and WebView2-absent facts pass.
- [ ] `dotnet test ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj --configuration Release --no-build` — expected: the no-WebView-hosting-Pegasus-UI fact passes and the desktop holds no second renderer.
- [ ] Performance and operator records in the ticket proof — expected: generation within the baseline-hardware target, with the final document and audit confirmed correct.

## Evidence tier

Tier 2 — Core/domain. Tier 5 — Web/API/MCP caller. Tier 7 — Browser/accessibility. Tier 10 — Performance/concurrency.
Tier 2 obliges deterministic evidence for the report projection fixtures; tier 5 obliges route-level evidence for draft, register, content and send including idempotency and audit actor; tier 7 obliges keyboard, focus and progress evidence for the preview and finalise flow; tier 10 obliges a measured generation time on baseline hardware rather than an asserted one.

## Documentation changes

- `docs/adr/0108-desktop-webview2-report-rendering.md` — nothing is written by this ticket. The renderer-selection flag and the parity outcome are supplied to [[DSK-07-12]] while the ADR still reads `status: proposed`; after acceptance the body is immutable and a change would need a superseding ADR.
- `docs/desktop/01-inventory-and-parity/parity-matrix.md` — row `PAR-15` report portion
- `docs/frd/frd-13-desktop-operator-experience.md` — report section cross-referencing FRD-11
- `docs/capabilities.md` — `DSK` rows for report generation, finalise and send

## Guardrails

- **Azure**: no write. Retiring the Playwright renderer from the Web container and its Container App CPU/memory uplift (ADR-0028) is an ⚠ Azure setting change owned by plan 11 — out of scope here.
- **Scope boundary**: the renderer implementation lives in `src/Pegasus.Desktop.Infrastructure` ([[DSK-07-14]]); this slice owns the desktop workflow, the `/api/v1` report endpoints and the tests. Must not modify `src/Pegasus.Infrastructure/Reports/PlaywrightAssessmentReportRenderer.cs` — it is retained until parity is signed off. Must not edit `docs/adr/0108-desktop-webview2-report-rendering.md`; [[DSK-07-12]] owns that file.
- **Traps**: WebView2 never hosts Pegasus UI (architecture test from [[DSK-02-12]]); templates come from one source and are hash-checked ([[DSK-07-13]]) — never copy a `.scriban` file; verify the WebView2 print API against official documentation before calling it rather than trusting either name in the plan set; a finalised report is governed by FRD-11 finality and regeneration rules; upstream DOCS-001 and TICK-206/208/216 and TICK-081/096/097/100 are report-decision inputs owned by [[DSK-07-17]] — do not resolve them here; `Features:DesktopGateway` must be enabled in tests. ADR bodies are immutable once accepted — this ticket depends on an already-accepted ADR-0108 and must never edit it.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.

## Outcome

_Filled at closeout._
