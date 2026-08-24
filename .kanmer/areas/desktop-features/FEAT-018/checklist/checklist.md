# Checklist — FEAT-018: S18 Report generation, preview, finalise, send

One box per plan step, in plan order. Tick with `set_ticket_doc`; append progress notes below.

## Orientation and preconditions

- [ ] Read plan 05 § S18, the `reuse-map.md` `Reports/` row, ADR-0108 as authored by [[FEAT-038]], and FRD-11 `:130-166`; call `get_doc_gates FEAT-018`; `take_ticket` with branch `task/dsk-05-18-reports` and worktree `../pegasus-worktrees/dsk-05-18-reports` from `origin/dev`
- [ ] Record in `research` the projection contract from `src/Pegasus.Core/Reports/AssessmentReportProjection.cs` and the renderer contract from `AssessmentReportRendering.cs:272-287`, with the SHA read
- [ ] Record the reproducible pipeline: the print options at `PlaywrightAssessmentReportRenderer.cs:120-128`, the unresolved-placeholder guard at `:114-117`, and the PDFsharp/SHA-256/`TemplateVersion` artifact steps at `:131-142`
- [ ] Record that only `assessment_report.scriban`, `assessment_fee_note.scriban` and `report.css` are embedded (`Pegasus.Infrastructure.csproj:42-47`) and that the other four `.scriban` files are unreachable
- [ ] Record that `Index.cshtml.cs:583` (`ISendCaseToAi`) is a Send-to-AI handler and is **not** the send this ticket implements
- [ ] Confirm [[FEAT-040]] has landed `IAssessmentReportRenderer` in `src/Pegasus.Desktop.Infrastructure`; if not, stop and leave the ticket in Preparing
- [ ] Confirm [[FEAT-039]] embeds the templates from one source with a hash check; if not, stop and leave the ticket in Preparing
- [ ] Re-verify `CoreWebView2.PrintToPdfStreamAsync(CoreWebView2PrintSettings)` → `Task<Stream>` with `microsoft_code_sample_search` / `microsoft_docs_fetch` and record the confirmed signature and fetch date in the plan
- [ ] Record the documented one-printing-operation-per-WebView constraint and confirm the renderer carries a serialising gate

## Gateway surface

- [ ] Confirm the four routes with [[GWY-014]] and [[FEAT-042]]: `POST …/reports/draft`, `POST …/reports`, `GET …/reports/{rid}/content`, `POST …/assessment/send`
- [ ] Implement the draft endpoint's two response modes — projection, or gateway-rendered bytes when the flag selects the retained renderer
- [ ] Implement the renderer-selection flag with one name and one recorded default, selecting both the renderer and the draft response mode
- [ ] Hand the flag name and default to [[FEAT-038]] for ADR-0108's Consequences while the ADR still reads `status: proposed`; make no edit to `docs/adr/0108-*.md`
- [ ] Add the report DTOs to `src/Pegasus.Contracts`, including the `NotReady` reason DTO (`Requirement` + `WhyOutstanding`)
- [ ] Regenerate `openapi/pegasus-v1.json` and the generated client in this change

## Desktop workflow

- [ ] Implement `ReportViewModel`: fetch the projection, render through the injected `IAssessmentReportRenderer`, show a preview
- [ ] Implement Finalise and Send as two separate deliberate commands
- [ ] Show progress and keep long rendering cancellable (proposal §14.5)
- [ ] Render a `NotReady` result as rows from `Requirement` / `WhyOutstanding` with no composed explanation (`docs/design/README.md:432-445`)
- [ ] Implement the WebView2-absent path: the guided message from [[FND-045]]'s startup check plus gateway-renderer fallback
- [ ] Implement Finalise: upload through [[FEAT-014]]'s transfer service and register with `POST /api/v1/cases/{id}/reports`; a finalised report is never silently replaced (FRD-11 `:130-134`)
- [ ] Implement Send: one stable idempotency key per user-initiated send, reused on retry; an uncertain outcome resolved by re-querying send status, never by resending
- [ ] Prove the send from the approved-mailbox Sent-item evidence (FRD-08 `:328`), not from the command's return, a queue result or a staff assertion (FRD-11 `:141-144`)
- [ ] Confirm no banned operator word and no how-it-works copy reaches the report screens

## Evidence

- [ ] Run the golden-file suite from [[FEAT-041]] against the two live templates and record the comparison, including any tolerance applied or any failing diff
- [ ] Add contract tests: draft, register, content and send — success, 401, 403, 409 stale version, send-key replay returning the original outcome, finalised report refusing a silent overwrite
- [ ] Enable `Features:DesktopGateway` explicitly in every new contract test
- [ ] Add view-model tests for preview, finalise, send, cancellation and the WebView2-absent path
- [ ] Confirm the architecture facts pass: WebView2 never hosts Pegasus UI, and the desktop holds no second renderer
- [ ] Operator step — measure generation on the baseline Test/UAT workstation against the target in `docs/desktop/10-security-observability-performance/README.md`; record the figures and the workstation specification
- [ ] Operator step — capture the operator's confirmation that the final document and its audit trail are correct, with the date
- [ ] Update `docs/desktop/01-inventory-and-parity/parity-matrix.md` row `PAR-15`, report portion only
- [ ] Cross-reference FRD-11 from `docs/frd/frd-13-desktop-operator-experience.md` and add the `DSK` rows to `docs/capabilities.md`
- [ ] Run the simplification pass over the branch diff and record it under a dated `## Simplification pass` heading in the plan
- [ ] **Verification run (this box produces `proof`)** — `dotnet test ./tests/Pegasus.IntegrationTests/… --filter "Category!=Corpus&Category!=Browser"`, `./tests/Pegasus.Api.ContractTests/…`, `./tests/Pegasus.Desktop.ViewModelTests/…` and `./tests/Pegasus.ArchitectureTests/…`, all `--configuration Release --no-build`; attach the four outputs, the golden-file report, the measured performance figures and the operator sign-off
- [ ] Open the PR into `dev`

## Progress notes

(append with `set_ticket_doc(doc: "checklist", append: true)`)
