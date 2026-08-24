# Research — FEAT-040: reproducing the gateway report renderer on the desktop through an isolated WebView2

## Question

What exactly must a desktop `IAssessmentReportRenderer` reproduce from
`PlaywrightAssessmentReportRenderer` to produce the same two PDFs from one snapshot, and which of
the two candidate off-screen WebView2 hosts can be proven to initialise with no visible window?

## Current behaviour

Report rendering today is a singleton inside the Web Container App:

| Fact | `path:line` |
| --- | --- |
| The renderer type and the interface it implements | `src/Pegasus.Infrastructure/Reports/PlaywrightAssessmentReportRenderer.cs:13` — `internal sealed class PlaywrightAssessmentReportRenderer : IAssessmentReportRenderer, IAsyncDisposable` (326 lines) |
| Serialisation gate | `:19` — `private readonly SemaphoreSlim gate = new(1, 1);`, taken at `:26` before any browser work |
| One snapshot → two Scriban contexts | `:23-80` (`RenderAsync` builds the assessment context; `CommonContext` at `:140`) |
| Template parse and unresolved-placeholder rejection | `:105-114` — `Template.Parse(ResourceText($"templates.{name}"))`, `template.HasErrors` → `InvalidOperationException`, then `html.Contains("{{")` or `html.Contains('«')` → `ReportRenderRejectedException("The composed report contains an unresolved placeholder.")` |
| Page setup and PDF options | `:118-128` — `SetContentAsync(html, WaitUntilState.Load)` then `page.PdfAsync(new PagePdfOptions { Format = "A4", PrintBackground = true, DisplayHeaderFooter = true, HeaderTemplate = "<span></span>", FooterTemplate = footer, Margin = new Margin { Top = "8mm", Right = "12mm", Bottom = "22mm", Left = "12mm" } })` |
| Post-processing and provenance | `:131-141` — `PdfReader.Open(new MemoryStream(pdf), PdfDocumentOpenMode.Import)` for the page count, then `new RenderedReportArtifact(fileName, pdf, document.PageCount, Convert.ToHexStringLower(SHA256.HashData(pdf)), AssessmentReportContract.TemplateVersion, $"Playwright/{typeof(Playwright).Assembly.GetName().Version}; Chromium")` |
| Resource resolution | `:309-314` — `ResourceStream` composes `Pegasus.Infrastructure.Reports.Assets.{suffix}`; `:292-299` reads one as a base64 data URI |
| Composition | `src/Pegasus.Infrastructure/DependencyInjection.cs:446-453` — `AddPegasusReportRendering` registers it as a **singleton** alongside `GenerateAssessmentReportDraft`, `IAssessmentReportProjectionSource` and `GenerateCaseAssessmentReportDraft` |
| Hosting cost | `src/Pegasus.Web/Pegasus.Web.csproj:28` — `ContainerBaseImage` `mcr.microsoft.com/playwright/dotnet:v$(PlaywrightVersion)-noble`; `infra/modules/platform.bicep:443-444` — `cpu: json('1.0')`, `memory: '2Gi'`, with the comment at `:436-441` recording that headless Chromium shares the app's CPU and memory |

Parity row: **`PAR-15`** (§13.9 Assessment and reporting, FRD-11/FRD-06,
`docs/desktop/01-inventory-and-parity/parity-matrix.md:60`) — `Cases/Assessment/Index.cshtml.cs`
(740 lines) including `OnPostGenerateReportDraftAsync`. That row covers the *operator path* into
rendering; the renderer implementation itself sits behind it in `Pegasus.Infrastructure`. The
matrix holds 46 `PAR-` rows, all keyed to page models under `src/Pegasus.Web/Pages/**`
(`grep -c '^| PAR-'` → `46`).

## Findings

### Facts

Measured on 2026-08-24 at fork `main`, unless a fetch date is given.

- **The Core contract is small and its provenance check is a hard gate.**
  `src/Pegasus.Core/Reports/AssessmentReportRendering.cs:272-278` —
  `RenderedReportArtifact(string SuggestedFileName, byte[] Pdf, int PageCount, string Sha256,
  string TemplateVersion, string EngineVersion)`; `:280-282` — `AssessmentReportDraft(Assessment,
  FeeNote)`; `:284-289` — `IAssessmentReportRenderer.RenderAsync(AssessmentReportSnapshot,
  CancellationToken)`. `GenerateAssessmentReportDraft` at `:291` calls `snapshot.Validate()`, then
  **re-hashes both artifacts** and throws `ReportRenderRejectedException("The renderer returned an
  artifact with mismatched provenance.")` at `:305` on any mismatch. The SHA-256 must therefore be
  of the exact bytes returned, lowercase hex (`Convert.ToHexStringLower`).
- **`ReportRenderRejectedException` is the established refusal type**, used sixteen times across
  `AssessmentReportRendering.cs` (`:39`, `:47`, `:76`, `:81`, `:188`, `:193`, `:198`, `:203`,
  `:207`, `:220`, `:226`, `:232`, `:261`, `:305`), and declared at `:312`. The desktop renderer's
  placeholder rejection reuses it rather than inventing a type.
- **All three governed signatures exist; one is embedded today.**
  `docs/design/brand/signatures/` holds `andy_patterson.png` (3,972 bytes), `ed_mawdsley.png`
  (80,989) and `neil_oreilly.png` (30,418); `Pegasus.Infrastructure.csproj:52-53` embeds only the
  first. The set the desktop assembly embeds is [[FEAT-039]]'s (plan handle `DSK-07-13`) job,
  against [[FEAT-043]]'s (plan handle `DSK-07-17`) record of the accepted upstream `TICK-216`
  contract.
- **The baseline test suite that this renderer must match is 158 lines and browser-trait-gated.**
  `tests/Pegasus.IntegrationTests/Reports/AssessmentReportRendererTests.cs` — a `[Theory]` over the
  four `AssessmentReportOutcome` values (`TotalLoss`, `Repairable`, `CashInLieu`, `ContractRepair`)
  with `[Trait("Category", "Browser")]`, asserting real extracted text with `UglyToad.PdfPig`
  (`"TOTAL LOSS REPORT"`, `"Vehicle Images"`, `"Statement of Truth"`, `"Front bumper"`, and on the
  fee note `"FEE NOTE"`, `"Subtotal (Net)"`, `"VAT @ 20%"`, `"TOTAL DUE"`, `"Lloyds Bank"`,
  `"30-12-80"`, `"50858868"`, `AssessmentReportContract.VatNumber`), plus a `PEGASUS_RENDER_EVIDENCE`
  environment variable that writes the rendered PDFs to a directory, and a long-list/multi-photo
  density case as a second `[Fact]`.
- **WebView2 printing, from the official documentation** (recorded in
  `docs/desktop/07-integrations/README.md` § 2 from a **2026-08-23** fetch of
  <https://learn.microsoft.com/microsoft-edge/webview2/how-to/print> and the `CoreWebView2` WinRT
  reference; this ticket re-fetches and re-dates it in step 2):
  `PrintToPdfAsync` and `PrintToPdfStreamAsync` exist on `CoreWebView2`; `PrintToPdfStreamAsync`
  returns a **rewound** PDF stream; `CoreWebView2PrintSettings` covers margins, page size,
  backgrounds, header/footer and scale; and **one print operation per WebView at a time** is
  supported.
- **The `SemaphoreSlim(1, 1)` discipline is therefore not a copied habit but a documented
  requirement.** The gateway renderer already does it at `:19`; the documentation makes it
  mandatory for WebView2.
- **The never-UI rule has an existing enforcement point.** [[FND-037]] (plan handle `DSK-02-12`)
  extends `DependencyDirectionTests` for the desktop boundaries and the no-WebView rule; this
  ticket extends that rule with the single approved exception rather than writing a new test
  harness. `tests/Pegasus.ArchitectureTests` exists today (`ls tests/`).
- **Neither target project exists yet.** `ls src/` → `Pegasus.Core`, `Pegasus.Infrastructure`,
  `Pegasus.Web`, `Pegasus.Worker`; `ls tests/` → `Pegasus.ArchitectureTests`, `Pegasus.Core.Tests`,
  `Pegasus.IntegrationTests`. `src/Pegasus.Desktop.Infrastructure` is [[FND-031]] (plan handle
  `DSK-02-06`), `src/Pegasus.Desktop` is [[FND-030]] (plan handle `DSK-02-05`), and
  `tests/Pegasus.Desktop.ViewModelTests` is [[TEST-004]] (plan handle `DSK-08-04`) /
  [[FND-038]] (plan handle `DSK-02-13`).
- **`TreatWarningsAsErrors` is on for every project.** `Directory.Build.props:8`, alongside
  `Nullable`, `ImplicitUsings`, `LangVersion latest`, `Deterministic` and
  `AnalysisLevel latest-recommended`. The verification's "succeeds with `TreatWarningsAsErrors`" is
  a real constraint, not a flourish.
- **The screen spec constrains the surface, not just the mechanism.**
  `docs/desktop/06-ui-design/screen-specs.md:378-383` — Generate report draft is a local WebView2
  render with progress in the status bar and cancel; **Preview is a document viewer, not Pegasus UI
  in a WebView**; AutomationIds at `:384-386`.

### Assumptions

- **`A-07-14-1` — one of the two candidate off-screen hosts initialises reliably with no visible
  window.** The two candidates are (a) a zero-size, `Visibility.Collapsed` WinUI `WebView2` control
  inside a XAML root, and (b) a `CoreWebView2Controller` created on a hidden HWND via
  `CoreWebView2Environment.CreateAsync`. This is **unverified**, and the area plan's § 7 says so in
  as many words ("a zero-size collapsed control may still initialise, but behaviour must be
  proven"). Confirmed by step 2's timeboxed probe, which renders a trivial HTML document to PDF
  through both. If **neither** works, the ticket stops and the `IAssessmentReportRenderer` seam —
  which ADR-0108 records precisely as this mitigation — lets the host change without touching
  callers; the gateway renderer remains registered, so nothing is broken while the question is
  reopened.
- **`A-07-14-2` — `CoreWebView2PrintSettings`' margin units can express 8 / 12 / 22 / 12 mm
  exactly.** The WinRT settings expose margins as doubles in inches rather than a CSS-style string,
  so the conversion is arithmetic and must be recorded in a comment (body step 6 requires exactly
  that). Confirmed by reading the reference during step 2. If the conversion is lossy at the
  representable precision, [[FEAT-041]] (plan handle `DSK-07-15`) absorbs the difference through its
  documented position tolerance rather than through a changed margin.
- **`A-07-14-3` — the engineer identity tuple is resolvable from the snapshot or from an
  authorisation Core already owns**, not from a caller-supplied value. The renderer must *map* an
  accepted tuple to embedded assets and choose no identity of its own. Confirmed by reading
  `AssessmentReportSnapshot` and the [[FEAT-043]] record during step 5. If the accepted tuple is
  not reachable from the snapshot, the renderer fails closed on every render rather than inventing
  a default — which is the correct behaviour, and the finding is then raised against
  [[FEAT-043]]'s record rather than patched here.
- **`A-07-14-4` — the WebView2 runtime version is readable at composition time** (so the failure at
  step 9 can name it and the `EngineVersion` string at step 8 can carry it). Confirmed during
  step 2's probe.

## Execution placement

The six-question cloud-justification test from
`docs/desktop/00-governance-and-workflow/README.md:166-178`, answered for **producing the report
PDF bytes**:

| Question | Answer | Evidence |
| --- | --- | --- |
| Shared authority — must several users see and update the same state? | **no** | A draft render is one operator's view of one case at one moment. The *stored* report is shared state, and it stays central — [[FEAT-042]] (plan handle `DSK-07-16`) registers it through the gateway into Box custody. |
| Unattended execution — must it run with every desktop closed? | **no** | Rendering is operator-initiated from the Reports tab (`screen-specs.md:378-383`). Nothing renders a report while every desktop is closed today either: `OnPostGenerateReportDraftAsync` (`Index.cshtml.cs:277`) is a POST handler. |
| Protected credentials — long-lived secret that must not sit on workstations? | **no** | The inputs are the governed templates, the brand logo and the authorised signature images ([[FEAT-039]]), plus the case snapshot the operator is already authorised to read. No provider secret is involved; ADR-0107 keeps Box and DVLA/DVSA credentials behind the gateway and this path uses neither. |
| Public callback — must an external service call a stable public endpoint? | **no** | The render is local and offline to the network; the WebView2 is never navigated to any URL. |
| Central enforcement — revocation, permissions, audit, invariant independent of the client? | **no** — *for the render itself* | Readiness, finality and regeneration stay Core-owned and are re-checked server-side by [[FEAT-042]] step 3; the provenance hash is re-verified by `GenerateAssessmentReportDraft` (`AssessmentReportRendering.cs:291-307`) wherever it runs. Moving the bytes does **not** move the gate, which is why the register endpoint re-checks. |
| Measured operational advantage — measured evidence central is materially better? | **no** | The opposite is measured: the central placement costs a `mcr.microsoft.com/playwright/dotnet` base image (`Pegasus.Web.csproj:28`) and cpu 1.0 / 2Gi (`platform.bicep:443-444`) for in-process Chromium. [[FEAT-041]] measures parity, not advantage. |

All six **no** → the responsibility belongs in the desktop, which is what L-03 and ADR-0108 record.
No Azure write; the placement change *removes* a central cost rather than adding one.

## Implications

1. **This is a reproduction, not a reimplementation.** Every divergence in context building,
   placeholder rejection or page setup surfaces in [[FEAT-041]] as a golden-file failure that looks
   like a renderer bug and is not. Read `PlaywrightAssessmentReportRenderer.cs` end to end and match
   it structure for structure.
2. **The provenance check makes sloppiness fatal in a useful way.** `GenerateAssessmentReportDraft`
   re-hashes; a renderer that hashes anything other than the exact returned bytes fails at once.
   That is a feature — use it as the first test.
3. **The host question must be answered before any other work.** It changes the file layout, the
   disposal story and whether a XAML root is required at all, and it is the one thing the area plan
   explicitly refuses to assume.
4. **Fail-closed engineer identity is a professional-attribution rule, not a validation nicety.**
   Upstream `TICK-216`'s accepted contract requires name, qualification and signature to match as
   one tuple; a valid signature paired with another engineer's name is the defect, and moving the
   render to the client is where it would be easiest to introduce unnoticed. Negative tests in
   every direction are cheap; the defect is not.
5. **The concurrency rule is documented, not defensive.** One print operation per WebView at a
   time — so the `SemaphoreSlim(1, 1)` is required, and the test is that two concurrent
   `RenderAsync` calls both succeed and neither corrupts the other's output.
6. **Nothing here removes the gateway renderer.** `AddPegasusReportRendering`
   (`DependencyInjection.cs:446`) stays; L-03 keeps it until [[FEAT-041]] signs parity off.

## Open questions

- None recorded here. The off-screen host (`A-07-14-1`) and the margin-unit conversion
  (`A-07-14-2`) are resolved by this ticket's own timeboxed probe in step 2 — they are work items,
  not questions for anyone else. The operator render on baseline hardware (body step 13) is an
  operator **action** during implementation whose output becomes proof, not an unresolved decision.
  Which engineer identities are authorised is [[FEAT-043]]'s recorded disposition of an
  already-accepted upstream contract, and which signature assets are embedded is [[FEAT-039]]'s —
  both scope boundaries, recorded in the plan's Risks section.
