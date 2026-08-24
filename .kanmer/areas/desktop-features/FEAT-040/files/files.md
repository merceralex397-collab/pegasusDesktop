# Files — FEAT-040

## 2026-08-25 correction — documented invisible host

Microsoft Learn documents `HWND_MESSAGE` as the valid parent for an invisible `CoreWebView2Controller` on Windows 8 and later; the WebView will never become visible. The fixed design is `CoreWebView2Environment.CreateCoreWebView2ControllerAsync(HWND_MESSAGE)`. This supersedes every earlier collapsed-XAML/hidden-HWND host-selection instruction below. Phase 7 validates packaged-app initialisation, PDF output and no-window behaviour; it does not select a host.


Measured on 2026-08-24. Paths that do not exist yet carry the ticket that creates them.

## Where the change lands

| Path | Why |
|---|---|
| `src/Pegasus.Desktop.Infrastructure/Reports/WebView2AssessmentReportRenderer.cs` *(new; project created by [[FND-031]] (plan handle `DSK-02-06`))* | The `Pegasus.Core.Reports.IAssessmentReportRenderer` implementation: two Scriban contexts from one `AssessmentReportSnapshot`, template parse, unresolved-placeholder rejection, print via `CoreWebView2.PrintToPdfStreamAsync`, PDFsharp page count, `RenderedReportArtifact`. Breakage risk: any divergence from `PlaywrightAssessmentReportRenderer` shows up as a [[FEAT-041]] (plan handle `DSK-07-15`) golden-file failure that reads as a renderer bug and is not. |
| `src/Pegasus.Desktop.Infrastructure/Reports/AuthorisedEngineerIdentity.cs` *(new)* | Maps **one accepted** name/qualification/signature tuple to the byte-identified governed assets [[FEAT-039]] (plan handle `DSK-07-13`) embeds, and fails closed on missing, unknown, mismatched or substituted values. Chooses no identity of its own. |
| `src/Pegasus.Desktop.Infrastructure/Pegasus.Desktop.Infrastructure.csproj` | The WebView2 / Windows App SDK reference the documented `HWND_MESSAGE` controller needs, plus `Scriban` and `PDFsharp`. Versions are inline per project today (there is no `Directory.Packages.props`; [[FND-027]] (plan handle `DSK-02-02`) introduces one), so match the versions already pinned at `src/Pegasus.Infrastructure/Pegasus.Infrastructure.csproj:24-25` — `PDFsharp` 6.2.4, `Scriban` 7.2.6 — rather than picking newer ones. |
| The desktop host's DI registration (`src/Pegasus.Desktop`, created by [[FND-030]] (plan handle `DSK-02-05`); composition shape from [[FND-032]] (plan handle `DSK-02-07`)) | Register the desktop renderer as the `IAssessmentReportRenderer` **when the WebView2 runtime is present and the parity flag allows it**, leaving the gateway draft endpoint reachable as the fallback. Record the flag name in `plan`. |
| `tests/Pegasus.Desktop.ViewModelTests/Reports/WebView2RendererTests.cs` *(new; project created by [[TEST-004]] (plan handle `DSK-08-04`) / [[FND-038]] (plan handle `DSK-02-13`))* | Placeholder rejection, cancellation, provenance, concurrency, and every step-5 engineer-tuple fail-closed case. |
| `tests/Pegasus.ArchitectureTests` (extending [[FND-037]] (plan handle `DSK-02-12`)'s no-WebView rule) | The single-permitted-`WebView2`-usage fact: only this renderer type may reference `WebView2`, and it never navigates to an http/https Pegasus URL nor hosts application XAML. |
| `docs/adr/0108-desktop-webview2-report-rendering.md` | The fixed documented `HWND_MESSAGE` controller recorded by ADR-0108; this ticket supplies packaged-app validation evidence, never a second ADR. |
| `docs/current-architecture.md` | The renderer composition — desktop plus retained gateway fallback — after the slice ships. |
| `docs/frd/frd-11-reports-correspondence-and-reviewed-proposals.md` | The local-render behaviour clause, including the fail-closed engineer-identity rule. |

## Context files

| Path | What it tells the implementer |
|---|---|
| `src/Pegasus.Infrastructure/Reports/PlaywrightAssessmentReportRenderer.cs` (all 326 lines) | The thing being reproduced. Specifically: `:19` the `SemaphoreSlim(1,1)` taken at `:26` before any browser work; `:23-80` the assessment context assembled from one snapshot; `:105-114` template parse, `HasErrors` check, and the placeholder rejection on `"{{"` **or** `'«'` — two conditions, both needed; `:118-128` the exact page options; `:131-141` `PdfReader.Open(…, PdfDocumentOpenMode.Import)` for the page count and the six-member artifact; `:140+` `CommonContext` including `ResourceText("templates.report.css")` at `:146` and the logo as a base64 data URI. |
| `src/Pegasus.Core/Reports/AssessmentReportRendering.cs:272-312` | The contract and the trap: `RenderedReportArtifact`'s six members at `:272`, `IAssessmentReportRenderer` at `:284`, and `GenerateAssessmentReportDraft` at `:291` which **re-hashes both artifacts** and throws at `:305` on mismatch. Hash the exact bytes you return, lowercase hex. Also that `ReportRenderRejectedException` (`:312`) is the house refusal type — used sixteen times in this one file — so do not add another. |
| `src/Pegasus.Infrastructure/Reports/PlaywrightAssessmentReportRenderer.cs:309-314` | `ResourceStream`'s naming shape, `Pegasus.<assembly>.Reports.Assets.{suffix}`, and its runtime throw when a resource is missing. The desktop equivalent resolves the **same suffixes** from the same shared props file ([[FEAT-039]]) with a different prefix. |
| `src/Pegasus.Infrastructure/DependencyInjection.cs:446-453` | `AddPegasusReportRendering` registers the gateway renderer as a **singleton** — consistent with a serialising gate — plus the three Core report services. This must not be removed from the Web host; the Guardrails say so and L-03 requires it. |
| `docs/desktop/07-integrations/README.md` § 2 (WebView2 documentation facts, fetched 2026-08-23) and § 7 (risk rows) | The documented facts: `PrintToPdfStreamAsync` returns a rewound stream; `CoreWebView2PrintSettings` covers margins/page size/backgrounds/header-footer/scale; one print per WebView at a time; and `HWND_MESSAGE` is the invisible controller parent. The remaining traps are print concurrency, runtime missing, and Chromium drift against a Playwright pinned to 1.61.0. |
| `docs/desktop/06-ui-design/screen-specs.md:378-386` | That Generate is a local render with **progress in the status bar and cancel**, that Preview is a document viewer and never Pegasus UI in a WebView, and the AutomationIds. Cancellation is a product requirement here, not just a `CancellationToken` courtesy. |
| `tests/Pegasus.IntegrationTests/Reports/AssessmentReportRendererTests.cs` | The 158-line browser baseline: the four `AssessmentReportOutcome` values, the exact asserted strings, and the `PEGASUS_RENDER_EVIDENCE` capture mechanism [[FEAT-041]] reuses. This is what "same output" means concretely. |
| `Directory.Build.props` | `TreatWarningsAsErrors` at `:8`, `Nullable`, `AnalysisLevel latest-recommended`, and the `PlaywrightVersion` single-source precedent at `:9-18`. A Release build with a warning is a failed build. |
| `docs/design/brand/signatures/` | All three authorised signatures present (3,972 / 80,989 / 30,418 bytes) against only `andy_patterson.png` embedded at `Pegasus.Infrastructure.csproj:52-53`. The tuple resolver must not assume one signature exists. |
| `src/Pegasus.Web/Pegasus.Web.csproj:28` and `infra/modules/platform.bicep:436-444` | The central hosting cost this placement removes: the Playwright base image, and `cpu: json('1.0')` / `memory: '2Gi'` with the comment that headless Chromium shares the app's CPU and memory. Useful in the ADR consequences; not something this ticket changes. |
| `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 23.2 | The isolated-WebView2 exception sentence — the authority for the dependency existing at all, conditional on ADR-0108 recording it and the WebView2 never hosting Pegasus UI. |

## Ripple effects

- **[[FEAT-041]] (plan handle `DSK-07-15`)** — its whole suite renders through this type. A change
  to the context building or page options after that suite is baselined is a fixture-review event,
  not a quiet edit.
- **[[FEAT-042]] (plan handle `DSK-07-16`)** — consumes `RenderedReportArtifact`'s `Sha256`,
  `PageCount`, `TemplateVersion` and `EngineVersion` in the register call. If `EngineVersion` does
  not name WebView2 and its runtime version, that ticket cannot record which engine produced a
  stored report.
- **[[FEAT-018]] (plan handle `DSK-05-18`)** and [[TEST-018]] (plan handle `DSK-08-18`) are blocked by this ticket. [[FND-007]] blocks this ticket until its proposed ADR has merged; the renderer is no longer a blocker for FND-007.
- **`tests/Pegasus.ArchitectureTests`** — [[FND-037]]'s no-WebView rule gains its single exception;
  the rule must still fail for any other `WebView2` reference in the solution.
- **`docs/adr/0108-desktop-webview2-report-rendering.md`** — the fixed documented `HWND_MESSAGE` controller is authored by [[FND-007]] as `proposed`. This ticket supplies packaged-app evidence to [[FEAT-038]]; it does not edit the ADR body, frontmatter, or index.
- **The MSIX** — the WebView2 / Windows App SDK reference and the embedded signature set change
  package contents; [[TEST-010]] (plan handle `DSK-08-10`)'s packaging tests and [[TEST-011]] (plan
  handle `DSK-08-11`)'s secret scan of the package both see this ticket's output.
- **No OpenAPI or generated-client ripple.** This ticket adds no route. The draft endpoint stays
  exactly as it is.
- **No Worker change, no migration, no Azure write.**

## Out of scope

- **`src/Pegasus.Infrastructure/Reports/PlaywrightAssessmentReportRenderer.cs`** — not modified.
- **Removing `AddPegasusReportRendering` from the Web host** — forbidden; the gateway renderer stays
  registered until [[FEAT-041]] signs parity off (L-03).
- **Editing a template, `report.css`, the brand logo or a signature asset** — the governed source is
  read-only here, and which assets are embedded is [[FEAT-039]]'s.
- **Deciding which engineer identities are authorised** — [[FEAT-043]] (plan handle `DSK-07-17`)
  records that, adopted from the accepted upstream `TICK-216` contract. This renderer maps an
  accepted tuple and chooses nothing.
- **Storing, uploading or registering the PDF** — [[FEAT-042]].
- **The parity fixtures and tolerances** — [[FEAT-041]].
- **A second ADR-0108** — [[FND-007]] owns the proposed file and [[FEAT-038]] owns only its later acceptance flip.
- **Pixel equality with Playwright's Chromium** — explicitly not the target; the WebView2 runtime
  updates itself while `Directory.Build.props:18` pins Playwright to 1.61.0.
