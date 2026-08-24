# Files — FEAT-018: S18 Report generation, preview, finalise, send

Surveyed at `bbd1c549` (2026-08-24). Paths marked *(created by …)* do not exist yet.

## Where the change lands

| Path | Why |
| --- | --- |
| `src/Pegasus.Desktop/` *(created by [[FND-030]] (plan handle `DSK-02-05`))* | `ReportViewModel` and its XAML: fetch the projection, render through the injected `IAssessmentReportRenderer`, preview, and offer **Finalise** and **Send** as two separate deliberate commands. Long rendering shows progress and stays cancellable (proposal §14.5). |
| `src/Pegasus.Desktop.Infrastructure/` *(created by [[FND-031]] (plan handle `DSK-02-06`))* | The report client (draft/register/content/send calls) and the renderer *host wiring* only. The renderer type itself is [[FEAT-040]]'s (plan handle `DSK-07-14`); this slice injects it and must not write a second one. |
| `src/Pegasus.Contracts/` *(created by [[FND-029]] (plan handle `DSK-02-04`))* | The report projection DTO, the `NotReady` reason DTO (`Requirement` + `WhyOutstanding`, mirroring `Index.cshtml.cs:313-317`), the register-final request/response, and the send request carrying the idempotency key. |
| `src/Pegasus.Web/` — the `/api/v1` report endpoints | `POST /cases/{id}/reports/draft` (**two response modes** — projection, or gateway-rendered bytes while the flag selects the retained renderer), `POST /cases/{id}/reports` (register the finalised PDF), `GET /cases/{id}/reports/{rid}/content`, `POST /cases/{id}/assessment/send`. Behind `Features:DesktopGateway`. |
| `src/Pegasus.Web/` — the renderer-selection flag | One flag name, one default, recorded in the plan and handed to [[FEAT-038]] for ADR-0108's Consequences. It selects both the renderer and the draft endpoint's response mode; two flags for one decision would be a second list. |
| `tests/Pegasus.Api.ContractTests/` *(created by [[TEST-001]] (plan handle `DSK-08-01`))* | Draft, register, content, send: success, 401, 403, 409 stale version, replay of the send idempotency key returning the original outcome, and a finalised report refusing a silent overwrite. |
| `tests/Pegasus.Desktop.ViewModelTests/` *(created by [[FND-038]] (plan handle `DSK-02-13`))* | Preview, finalise, send, cancellation, and the WebView2-absent path. |
| `tests/Pegasus.IntegrationTests/Reports/` | The golden-file facts from [[FEAT-041]] (plan handle `DSK-07-15`) run here, beside the two existing files. |
| `tests/Pegasus.ArchitectureTests/` | The no-WebView-hosting-Pegasus-UI fact and a fact that the desktop holds no second renderer. Extended by [[FND-037]] (plan handle `DSK-02-12`); this slice asserts against it. |
| `docs/desktop/01-inventory-and-parity/parity-matrix.md` | Row `PAR-15` at `:60`, **report portion only** — [[FEAT-017]] owns the assessment portion of the same row. |
| `docs/frd/frd-13-desktop-operator-experience.md` *(created by area 00)* | The report section, cross-referencing FRD-11. |
| `docs/capabilities.md` | `DSK` rows for report generation, finalise and send. |

## Context files

| Path | What it tells the implementer |
| --- | --- |
| `src/Pegasus.Infrastructure/Reports/PlaywrightAssessmentReportRenderer.cs:97-129` | The exact pipeline to reproduce: Scriban parse with `TemplateContext { LimitToString = 0 }` (`:111`); the **unresolved-placeholder guard** that throws `ReportRenderRejectedException` when the composed HTML still contains `{{` or `«` (`:114-117`); `SetContentAsync(html, WaitUntil = Load)` (`:119`); and the print options — `Format = "A4"`, `PrintBackground = true`, `DisplayHeaderFooter = true`, `HeaderTemplate = "<span></span>"`, `FooterTemplate = footer`, `Margin` Top 8mm / Right 12mm / Bottom 22mm / Left 12mm (`:120-128`). Skip the guard and an unrendered `{{placeholder}}` reaches a client's report. |
| `src/Pegasus.Infrastructure/Reports/PlaywrightAssessmentReportRenderer.cs:131-142` | The artifact contract after rendering: PDFsharp `PdfReader.Open(…, PdfDocumentOpenMode.Import)` for `PageCount`, `Convert.ToHexStringLower(SHA256.HashData(pdf))` for the hash, `AssessmentReportContract.TemplateVersion`, and a renderer identity string (`Playwright/{version}; Chromium`). The desktop's identity string must still name its engine — FRD-11 requires preserved provenance. |
| `src/Pegasus.Infrastructure/Reports/PlaywrightAssessmentReportRenderer.cs:19` | `private readonly SemaphoreSlim gate = new(1, 1);` — the existing renderer already serialises. WebView2 documents the same constraint ("only one `Printing` operation can be in progress at a time"), so the desktop renderer needs its own gate, not an assumption of thread safety. |
| `src/Pegasus.Core/Reports/AssessmentReportRendering.cs:8`, `:224`, `:272-287`, `:312` | `TemplateVersion = "rendererref1-v1"`; the payload-version guard that refuses a mismatched payload; the `RenderedReportArtifact` / `AssessmentReportDraft` / `IAssessmentReportRenderer` shapes; and `ReportRenderRejectedException`. These are the contract both renderers implement — the desktop does not get its own. |
| `src/Pegasus.Web/Pages/Cases/Assessment/Index.cshtml.cs:277-319` | What the draft path does today, and the three outcomes: `NotFound`; `NotReady` with **structured** `Reasons` (`Requirement`, `WhyOutstanding`) at `:313-317`; otherwise `File(pdf, "application/pdf", SuggestedFileName)` at `:319`. Note the last one: today the draft endpoint returns **bytes**, so returning a projection is a new response mode, not a rename. |
| `src/Pegasus.Web/Pages/Cases/Assessment/Index.cshtml.cs:583-627` | The trap. `OnPostSendAsync` resolves `ISendCaseToAi` (`:593`) — it is **Send to Claude (AI-09)**, not a report send. `grep -rn "OnPostSend" src/Pegasus.Web/Pages/` returns only this hit, so **no report-send path exists in the web application** and there is nothing to characterize. Send to AI is a recorded exclusion (`reuse-map.md:38`, `docs/capabilities.md:269`); this slice ships no Send-to-AI affordance. |
| `docs/frd/frd-11-reports-correspondence-and-reviewed-proposals.md:130-144` | Finality, in enforceable words: immutable artifact/version identity and hash; correction/addendum creates a new reasoned version and retains every earlier artifact; a closed case must be reasonedly reopened first; and the report-**sent** business event is the approved-mailbox Sent-item evidence, with Outlook `sentDateTime` as the business time. Also the sentence that decides the acceptance test: "A Box report PDF, file upload, generated artifact, draft, queue result, or staff assertion alone proves neither sending nor external receipt." |
| `docs/frd/frd-11-…md:167-173` | The send contract: "An allocated targeted report-send transaction is idempotent and records approved destinations, immutable artifact/version, Box filing, exact send evidence, completion outcome, and partial-failure recovery." This is the specification the send steps are written from. |
| `docs/frd/frd-08-email-mailbox-and-background-processing.md:328` | § `Outbound correspondence evidence` — where the Sent-item evidence contract actually lives. |
| `src/Pegasus.Infrastructure/Pegasus.Infrastructure.csproj:42-52` | Which templates are really live: only `assessment_report.scriban`, `assessment_fee_note.scriban` and `report.css` are embedded, plus two brand PNGs. The folder holds seven `.scriban`/`.css` files; four are not reachable from the renderer. Golden-file parity covers the two live report templates, not seven. |
| `Directory.Build.props:10-17` and `src/Pegasus.Web/Pegasus.Web.csproj:23-28` | `PlaywrightVersion 1.61.0` is the single source of truth and the container base image tag must match it exactly. Retiring the renderer touches both, and that removal is [[FEAT-026]]'s (plan handle `DSK-05-26`), coupled to this ticket's parity outcome — not to its schedule. |
| `docs/design/README.md:432-445` | § `No explanatory copy and page economy`: only populated, relevant sections render, and a page never describes its own mechanics. The `NotReady` reasons are rendered as rows; the screen must not compose a sentence explaining how readiness works. |
| `docs/desktop/03-gateway-api-and-data/endpoint-map.md` § `Cases` (the two report rows) | The authoritative routes, and the note that `POST /cases/{id}/reports` and `GET …/reports/{rid}/content` are "new for L-03; today the web keeps the rendered draft server-side". |

## Ripple effects

- **`openapi/pegasus-v1.json` and the generated client** — four endpoints, one of them with two
  response modes. Regenerated in this change; a contract addition missing from the snapshot is
  invisible downstream.
- **`tests/Pegasus.IntegrationTests/Reports/AssessmentReportRendererTests.cs` and
  `AssessmentReportDraftWebTests.cs`** must stay green: the gateway renderer is retained, so its
  tests are not superseded by this slice.
- **`tests/Pegasus.ArchitectureTests`** — the no-WebView-hosting-Pegasus-UI fact and the
  single-renderer fact both run against the new desktop assemblies.
- **[[FEAT-038]] (plan handle `DSK-07-12`)** receives the flag name and the parity outcome for
  ADR-0108's Consequences while the ADR still reads `status: proposed`. After acceptance the body
  is immutable and a change would need a superseding ADR.
- **[[FEAT-017]]** shares row `PAR-15` and the same page-model file; the two slices must not edit
  `Index.cshtml.cs` concurrently.
- **[[TEST-008]] (plan handle `DSK-08-08`)** owns the "report preview and finalize" UI script and
  [[TEST-016]] (plan handle `DSK-08-16`) the end-to-end scenarios; both consume this slice's
  `AutomationId`s.
- **`docs/capabilities.md`, `frd-13`, the parity matrix** — updated in the same slice.

## Out of scope

- `src/Pegasus.Infrastructure/Reports/PlaywrightAssessmentReportRenderer.cs` — retained until
  parity is signed off; **not modified** by this ticket.
- `docs/adr/0108-desktop-webview2-report-rendering.md` — [[FEAT-038]] owns the file. This ticket
  supplies inputs and edits nothing; an accepted ADR body is immutable.
- The renderer implementation itself — [[FEAT-040]] (plan handle `DSK-07-14`); the template
  embedding and hash check — [[FEAT-039]] (plan handle `DSK-07-13`); the golden-file suite —
  [[FEAT-041]] (plan handle `DSK-07-15`). This slice consumes all three.
- The eleven upstream report-decision tickets (upstream DOCS-001, TICK-206/208/216,
  TICK-081/096/097/100) — [[FEAT-043]] (plan handle `DSK-07-17`) reconciles them. Not resolved here.
- **Any Send-to-AI affordance** — a recorded exclusion; `OnPostSendAsync` (`:583`) and
  `OnPostReconcileAsync` (`:628`) are AI-09 surfaces and are not carried over.
- Retiring the Playwright renderer from the Web container, the `AddPegasusReportRendering`
  registration and the ADR-0028 Container App CPU/memory uplift — ⚠ Azure setting change owned by
  plan 11 ([[PLAT-026]], plan handle `DSK-11-08`); the code-side pin removal is [[FEAT-026]]'s.
- Azure: no write.
