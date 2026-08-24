# Research — FEAT-018: S18 Report generation, preview, finalise, send

Repository revision read: `bbd1c549` (`git rev-parse --short HEAD`, 2026-08-24). Line numbers were
produced by `grep -n` / `sed -n` at that revision. WebView2 documentation fetched from Microsoft
Learn on **2026-08-24**.

## Question

What exactly the gateway renderer does — templates, print options, post-processing, artifact
identity — so an isolated WebView2 path can reproduce it byte-comparably; which WebView2 print API
is the right one; and what the current web application actually does for *finalise* and *send*,
because the ticket names two handlers for it.

## Current behaviour

- **Draft generation**: `src/Pegasus.Web/Pages/Cases/Assessment/Index.cshtml.cs:277`
  `OnPostGenerateReportDraftAsync` → `GenerateCaseAssessmentReportDraft.ExecuteAsync(id, actor, ct)`
  (`:293`). Three outcomes (`:305-318`): `NotFound`; `NotReady`, carrying structured
  `result.Reasons` each with a `Requirement` and a `WhyOutstanding` string; otherwise the handler
  returns `File(assessmentPdf.Pdf, "application/pdf", assessmentPdf.SuggestedFileName)` at `:319`.
  **Today the draft endpoint returns rendered bytes, not a projection.**
- **Core contract**: `src/Pegasus.Core/Reports/AssessmentReportRendering.cs` (312 lines) —
  `IAssessmentReportRenderer.RenderAsync(snapshot, cancellationToken)` at `:284-287`, returning
  `AssessmentReportDraft` (`:280`), whose artifacts are `RenderedReportArtifact` (`:272`) carrying
  file name, bytes, page count, hash, `TemplateVersion` (`:277`) and a renderer identity string.
  `AssessmentReportContract.TemplateVersion = "rendererref1-v1"` (`:8`), and a payload-version guard
  refuses a mismatched payload at `:224`. `ReportRenderRejectedException` is declared at `:312`.
  The projection is `src/Pegasus.Core/Reports/AssessmentReportProjection.cs` (362 lines).
- **The renderer**: `src/Pegasus.Infrastructure/Reports/PlaywrightAssessmentReportRenderer.cs`
  (326 lines), `internal sealed class … : IAssessmentReportRenderer, IAsyncDisposable` at `:13`.
  Its pipeline, at `RenderPdfAsync` `:97-129`:
  1. parse the Scriban template (cached in a `ConcurrentDictionary`, `:15`, `:105-106`);
  2. render with `new TemplateContext { LimitToString = 0 }` (`:111`);
  3. **reject unresolved placeholders** — if the composed HTML contains `{{` or `«`, throw
     `ReportRenderRejectedException("The composed report contains an unresolved placeholder.")`
     (`:114-117`);
  4. `page.SetContentAsync(html, WaitUntil = Load)` (`:119`);
  5. `page.PdfAsync` with **`Format = "A4"`, `PrintBackground = true`,
     `DisplayHeaderFooter = true`, `HeaderTemplate = "<span></span>"`, `FooterTemplate = footer`,
     `Margin = Top 8mm / Right 12mm / Bottom 22mm / Left 12mm`** (`:120-128`).
  Then `Artifact` at `:131-142`: PDFsharp `PdfReader.Open(new MemoryStream(pdf), PdfDocumentOpenMode.Import)`
  to read `PageCount`, `Convert.ToHexStringLower(SHA256.HashData(pdf))` for the hash,
  `AssessmentReportContract.TemplateVersion`, and a renderer identity of
  `$"Playwright/{typeof(Playwright).Assembly.GetName().Version}; Chromium"`.
  Two documents are produced per draft: `assessment_report.scriban` and
  `assessment_fee_note.scriban` (`:74-75`).
- **Templates**: `docs/design/assets/report-renderer/templates/` holds seven files —
  `advert_evidence_pack.scriban`, `assessment_fee_note.scriban`, `assessment_report.scriban`,
  `expert_report.scriban`, `fee_note.scriban`, `market_valuation_evidence.scriban`, `report.css`.
  **Only three of them are embedded today**: `src/Pegasus.Infrastructure/Pegasus.Infrastructure.csproj:42-47`
  embeds `assessment_report.scriban`, `assessment_fee_note.scriban` and `report.css`, plus brand
  assets at `:48-52` (`docs/design/brand/logos/logo_no_margin.png`,
  `docs/design/brand/signatures/andy_patterson.png`).
- **Parity-matrix row**: `PAR-15` at `docs/desktop/01-inventory-and-parity/parity-matrix.md:60`,
  shared with [[FEAT-017]] (plan handle `DSK-05-17`). The matrix holds 46 `PAR-` rows.

## Findings

- **There is no report-send path in the web application.** `grep -rn "OnPostSend" src/Pegasus.Web/Pages/`
  returns exactly one hit: `Pages/Cases/Assessment/Index.cshtml.cs:583`. That handler resolves
  `ISendCaseToAi` (`:593`) and composes the prompt "Work the assessment for case {reference} in
  Pegasus…" (`:611-614`) — it is **Send to Claude (AI-09)**, not a report send. The only `ISend*`
  interfaces in `src/Pegasus.Core/` are `ISendToAiControl` (`AiWork/AiWorkContracts.cs:138`) and
  `ISendCaseToAi` (`:261`).
  - Consequence: **`send` in this ticket is new capability, not parity**, and there is no web
    behaviour to characterize against. Its specification is FRD-11, not a page handler. Send to AI
    itself is a recorded exclusion (`reuse-map.md:38`, `docs/capabilities.md:269`,
    `AiWork/SendToAi.cs:12` and `:35-42`) and this slice ships no Send-to-AI affordance.
- **FRD-11 does govern the send, precisely.** `docs/frd/frd-11-…md` § `Targeted sending and
  reviewed AI proposals` `:167-173`: "An allocated targeted report-send transaction is idempotent
  and records approved destinations, immutable artifact/version, Box filing, exact send evidence,
  completion outcome, and partial-failure recovery." And § `Report correction, finality, and
  post-report work` `:138-144`: the report-sent business event **is the approved-mailbox Sent-item
  evidence** specified in FRD-08 § `Outbound correspondence evidence`
  (`docs/frd/frd-08-…md:328`); Outlook `sentDateTime` is the business time; "A Box report PDF, file
  upload, generated artifact, draft, queue result, or staff assertion alone proves neither sending
  nor external receipt."
- **FRD-11 finality is enforceable and specific** (`:130-137`): an issued report has an immutable
  artifact/version identity and hash; a correction or addendum creates a new reasoned version and
  retains every earlier artifact; a closed case must be reasonedly reopened before its report is
  revised.
- **The WebView2 API question in the ticket body is settled.** Microsoft Learn, fetched 2026-08-24
  (`learn.microsoft.com/dotnet/api/microsoft.web.webview2.core.corewebview2.printtopdfstreamasync`,
  package `Microsoft.Web.WebView2`, assembly `Microsoft.Web.WebView2.Core.dll`, namespace
  `Microsoft.Web.WebView2.Core`, doc version 1.0.4129.50). **Both methods exist and they differ:**
  - `Task<bool> PrintToPdfAsync(string resultFilePath, CoreWebView2PrintSettings printSettings)` —
    writes to a **file path**; the host must supply an absolute path including file name.
  - `Task<System.IO.Stream> PrintToPdfStreamAsync(CoreWebView2PrintSettings printSettings)` —
    returns the PDF **as a stream**, "rewound to the start of the pdf data"; passing `null` for
    `printSettings` uses defaults; settings come from
    `CoreWebView2Environment.CreatePrintSettings`.
  - Both docs state: "Only one `Printing` operation can be in progress at a time"; a concurrent
    `PrintToPdfStreamAsync` / `PrintToPdfAsync` / `PrintAsync` on the same WebView throws.
  - **`PrintToPdfStreamAsync` is the correct choice here**: PDFsharp post-processing already reads
    from a `MemoryStream` (`PlaywrightAssessmentReportRenderer.cs:133`), and the file-path variant
    would put an unencrypted report PDF on disk — a temporary-file ACL exposure the security
    checklist in [[TEST-011]] (plan handle `DSK-08-11`) tests for. The single-operation constraint
    means the renderer needs the same serialising gate the Playwright renderer already has
    (`SemaphoreSlim gate = new(1, 1)`, `:19`).
- **Only three templates are live.** The four unembedded `.scriban` files in the templates folder
  are not reachable from `IAssessmentReportRenderer` today. Whatever [[FEAT-039]] (plan handle
  `DSK-07-13`) embeds "from one source, hash-checked" must be scoped against that fact rather than
  against the folder listing.
- **The draft endpoint changes shape, and that is the real design decision here.** Today the web
  handler returns rendered bytes; the endpoint map row for `POST /cases/{id}/reports/draft`
  says "report bytes **or** report id + ETag", and this ticket's step 5 requires it to return the
  **projection** for local rendering while still being able to return gateway-rendered bytes when
  the flag selects the retained renderer. The endpoint therefore has two response modes selected by
  the same flag that selects the renderer.
- **`NotReady` is structured, and must stay structured.** `result.Reasons` carries `Requirement`
  and `WhyOutstanding` per outstanding item (`Index.cshtml.cs:313-317`). The desktop renders those
  as populated rows; it must not compose an explanatory sentence (`docs/design/README.md` § `No
  explanatory copy and page economy`, `:432-445`).
- The Playwright pin is coupled to the container base image: `Directory.Build.props:17`
  `<PlaywrightVersion>1.61.0</PlaywrightVersion>` and
  `src/Pegasus.Web/Pegasus.Web.csproj:28` `ContainerBaseImage = mcr.microsoft.com/playwright/dotnet:v$(PlaywrightVersion)-noble`,
  with comments at `:23-26` and `Directory.Build.props:10-16` recording that the two must not
  desynchronise. Retiring the renderer therefore touches both — and that removal belongs to
  [[FEAT-026]] (plan handle `DSK-05-26`) and, on the Azure side, to plan 11.

### Facts

- `AssessmentReportProjection.cs` 362 lines; `AssessmentReportRendering.cs` 312;
  `PlaywrightAssessmentReportRenderer.cs` 326 (`wc -l`).
- Seven files in `docs/design/assets/report-renderer/templates/`; three embedded by
  `Pegasus.Infrastructure.csproj:42-47`.
- `tests/Pegasus.IntegrationTests/Reports/` contains exactly `AssessmentReportRendererTests.cs` and
  `AssessmentReportDraftWebTests.cs` (`ls`).
- `src/Pegasus.Desktop`, `src/Pegasus.Desktop.Infrastructure`, `src/Pegasus.Contracts`,
  `tests/Pegasus.Api.ContractTests` and `tests/Pegasus.Desktop.ViewModelTests` do not exist yet
  (`ls src/`, `ls tests/`, `cat Pegasus.slnx`).
- `docs/adr/0108-desktop-webview2-report-rendering.md` does not exist yet; the ADR block
  ADR-0100…ADR-0110 is reserved and operator-confirmed
  (`docs/desktop/00-governance-and-workflow/README.md:141-148`), and ADR-0108's row is at `:163`.

### Assumptions

- `A-05-18-1` — WebView2's Chromium print engine reproduces the Playwright Chromium output closely
  enough that the golden-file tolerances in [[FEAT-041]] (plan handle `DSK-07-15`) are achievable
  for the two live templates. *Confirm:* run the suite at step 11 on the real fixtures. *If wrong:*
  the flag keeps the gateway renderer selected and L-03's parity gate simply does not open — the
  ticket still ships honestly, with the diff recorded.
- `A-05-18-2` — `CoreWebView2PrintSettings` can express A4, background printing, header/footer
  templates and the exact 8/12/22/12 mm margins the Playwright options set. *Confirm:* read
  `CoreWebView2PrintSettings` members before writing the renderer (`microsoft-code-reference`).
  *If wrong:* the margin or footer differences appear as golden-file diffs and are recorded as
  tolerances rather than hidden.
- `A-05-18-3` — [[FEAT-040]] (plan handle `DSK-07-14`) lands `IAssessmentReportRenderer` in
  `src/Pegasus.Desktop.Infrastructure` and [[FEAT-039]] embeds the templates hash-checked from one
  source. *Confirm:* step 3 checks both before any code is written; if either is missing the ticket
  stays in Preparing, per the body.
- `A-05-18-4` — the send transport is an approved-mailbox outbound path whose Sent-item evidence
  FRD-08 § `Outbound correspondence evidence` (`:328`) already specifies, executed gateway-side.
  *Confirm:* read that section and [[FEAT-042]] (plan handle `DSK-07-16`) before implementing send.
  *If wrong:* the "audited provider message id" the acceptance criterion names would be sourced
  from the wrong evidence and FRD-11's "staff assertion alone proves neither sending nor external
  receipt" would be breached.

## Execution placement

Six-question test from `docs/desktop/00-governance-and-workflow/README.md` § 3 (`:169-176`):

| Question | Answer | Evidence |
| --- | --- | --- |
| Shared authority — must several users see and update the same state? | **yes** | The canonical report record is case state; FRD-11 `:130-133` gives an issued report an immutable artifact/version identity every operator must see. Lands in the **gateway** (`Pegasus.Web`, L-01) for registration and retrieval. |
| Unattended execution — must it run with every desktop closed? | **no** | Generation, preview and finalise are operator-initiated. The send *evidence* is discovered by the existing Sent-item poller in `src/Pegasus.Worker` — that is FRD-08's surface, already central, and not moved by this ticket. |
| Protected credentials — long-lived secret that must not sit on workstations? | **yes, for the send only** | Outbound correspondence uses the approved-mailbox Graph path; `reuse-map.md` (`Email/` row) records that Graph credentials never reach the desktop (ADR-0106). The desktop confirms; the **gateway** authorises and executes. Rendering itself needs no credential. |
| Public callback — must an external service call a stable public endpoint? | **no** | Nothing calls back into this surface. |
| Central enforcement — revocation, permissions, audit, invariant independent of the client? | **yes** | FRD-11 `:130-137` finality, the immutable hash, and "a finalised report is never silently replaced" must hold whatever the client does. Lands in the **gateway**. |
| Measured operational advantage — measured evidence central is materially better? | **no — and this is the decision L-03 records** | Proposal §4.1 finds the measured advantage runs the other way for *generation*: rendering locally removes a round trip of report bytes and removes Chromium from the Container App (ADR-0028's CPU/memory uplift). Rendering therefore lands on the **desktop**; the gateway renderer is retained only until golden-file parity passes. |

Three "yes" answers, and each names the **gateway** — the existing `Pegasus.Web` Container App
under L-01, not a new Azure resource. No Azure write is implied by this ticket; the eventual
Container App uplift reversal is plan 11's (⚠, [[PLAT-026]], plan handle `DSK-11-08`).

## Implications

1. **Reproduce the pipeline, not just the output.** The five Playwright print options
   (`:120-128`), the unresolved-placeholder guard (`:114-117`), the PDFsharp page count, the
   lowercase-hex SHA-256 and `TemplateVersion = "rendererref1-v1"` are all part of the artifact
   contract. The desktop renderer's identity string replaces `Playwright/…; Chromium` — it must
   still record *which* engine produced the bytes, because FRD-11 requires preserved provenance.
2. **`PrintToPdfStreamAsync`, and serialise it.** The stream variant avoids a plaintext report on
   disk, and the documented "only one printing operation at a time" constraint means the desktop
   renderer needs its own gate exactly as `PlaywrightAssessmentReportRenderer.cs:19` has one.
3. **Send has no baseline, so it needs its specification cited step by step.** Every send
   requirement in this plan is traced to an FRD-11 or FRD-08 line, because there is no handler to
   point at. In particular: an idempotency key generated once per user-initiated send; an uncertain
   outcome resolved by re-query, never by resending; and the Sent-item evidence — not a queue
   result or a staff assertion — as proof of sending.
4. **The draft endpoint has two response modes.** The flag that selects the renderer also selects
   whether `POST …/reports/draft` returns the projection or the gateway-rendered bytes. Write it as
   one flag with one name and one default, recorded in this plan, and hand both to [[FEAT-038]]
   (plan handle `DSK-07-12`) for ADR-0108's Consequences **before** the acceptance flip. This
   ticket makes no edit to ADR-0108.
5. **Only two templates are in the golden-file scope.** `assessment_report.scriban` and
   `assessment_fee_note.scriban`. Claiming parity across all seven would be false.

## Open questions

None that belong in an `open-questions` document.

- The WebView2 print API — **settled by this research** against official documentation fetched
  2026-08-24: `PrintToPdfStreamAsync`. The ticket's step 4 remains as a re-verification at
  implementation time, and the verified signature is recorded in the plan.
- ADR-0108's content — owned by [[FEAT-038]] (plan handle `DSK-07-12`). A decision a named sibling
  ticket owns is a scope boundary, recorded in the plan's *Risks / open questions*.
- The renderer implementation and the golden-file suite — owned by [[FEAT-040]] and [[FEAT-041]];
  same treatment.
- Send to AI — a recorded exclusion with a reactivation condition, settled by the operator on
  2026-08-24. No question is opened for it on any ticket.
