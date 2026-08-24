# Checklist — FEAT-040

One box per plan step, in plan order. Tick with `set_ticket_doc`; append progress notes below.

- [ ] Read the plan row `DSK-07-14`, ADR-0108 from [[FEAT-038]] (plan handle `DSK-07-12`), area 07 § 2's WebView2 facts and § 7's four trap rows, [[FEAT-043]] (plan handle `DSK-07-17`)'s upstream TICK-216 record, and all 326 lines of `src/Pegasus.Infrastructure/Reports/PlaywrightAssessmentReportRenderer.cs`
- [ ] Call `get_doc_gates FEAT-040` and `take_ticket` on branch `task/dsk-07-14-desktop-renderer`
- [ ] Fetch <https://learn.microsoft.com/microsoft-edge/webview2/how-to/print> and the `CoreWebView2` reference with `microsoft_docs_fetch`, and record the facts in `research` with **today's** fetch date
- [ ] Build the timeboxed throwaway probe and render a trivial HTML document to PDF through a zero-size collapsed WinUI `WebView2` control in a XAML root
- [ ] Render the same document through `CoreWebView2Controller` on a hidden HWND via `CoreWebView2Environment.CreateAsync`
- [ ] Record in `research` which host initialises reliably with no visible window, the observed WebView2 runtime version, and the margin-unit shape `CoreWebView2PrintSettings` exposes; then delete the probe
- [ ] Create `WebView2AssessmentReportRenderer` in `src/Pegasus.Desktop.Infrastructure` implementing `Pegasus.Core.Reports.IAssessmentReportRenderer`, with the chosen host behind an internal `OffScreenWebViewHost` type
- [ ] Build the two `ScriptObject` contexts from one `AssessmentReportSnapshot` exactly as `PlaywrightAssessmentReportRenderer` does, parsing templates from the resources [[FEAT-039]] (plan handle `DSK-07-13`) shares
- [ ] Keep both halves of the placeholder rejection — `template.HasErrors` and `html.Contains("{{") || html.Contains('«')` — throwing the existing `ReportRenderRejectedException`
- [ ] Implement the authorised-engineer-identity resolver: exactly one name/qualification/signature tuple, failing closed on missing, unknown, mismatched or substituted values, with no fallback signature and no caller-supplied signature path
- [ ] Set `CoreWebView2PrintSettings` to A4, backgrounds printed, header and footer displayed with the same footer template and empty header, margins top 8 mm / right 12 mm / bottom 22 mm / left 12 mm, with the unit conversion recorded in a comment
- [ ] Call `PrintToPdfStreamAsync` and read the returned rewound stream fully
- [ ] Add the `SemaphoreSlim(1, 1)` gate taken before any host work, matching `PlaywrightAssessmentReportRenderer.cs:19`
- [ ] Post-process with `PdfReader.Open(…, PdfDocumentOpenMode.Import)` and return `RenderedReportArtifact` with the file name, bytes, page count, lowercase-hex SHA-256 of those exact bytes, `AssessmentReportContract.TemplateVersion` and a WebView2 engine-version string carrying the runtime version
- [ ] Detect a missing or outdated WebView2 runtime at composition and produce a distinct named failure that maps to "render unavailable — use the gateway renderer", logging the runtime version found and naming the install step from [[FND-045]] (plan handle `DSK-04-09`)'s startup check
- [ ] Register the renderer in the desktop host's DI so it is selected only when the runtime is present and the parity flag allows it, leaving the gateway draft endpoint reachable
- [ ] Record the parity flag's name under a dated heading in the plan document
- [ ] Extend [[FND-037]] (plan handle `DSK-02-12`)'s architecture test so this renderer type is the only permitted `WebView2` usage in the solution, and confirm the rule still fails when a second reference is introduced
- [ ] Run `winui-code-review`'s `WUI4xxx` checks for uninitialised-WebView2 defects
- [ ] Add the view-model tests: unresolved placeholder rejected; cancelled render throws `OperationCanceledException` with no partial artifact; page count matches PDFsharp's; SHA-256 matches the returned bytes; two concurrent renders serialise without corruption
- [ ] Add the six engineer-tuple negative tests — missing name, missing qualification, missing signature, unknown key, signature paired with another engineer's name, arbitrary substitution — each failing closed rather than rendering
- [ ] **Operator step** — render all four `AssessmentReportOutcome` values from the packaged app on the baseline Windows 11 workstation and hand back the four PDFs, the runtime version, per-render wall-clock times, and confirmation that no window appeared
- [ ] **Operator step** — render one report per authorised engineer identity where a fixture exists and confirm each artifact carries that person's matching name, qualification and signature
- [ ] Record the chosen off-screen host in ADR-0108's consequences via [[FEAT-038]]'s file, and add the local-render and fail-closed-identity clauses to `docs/frd/frd-11-reports-correspondence-and-reviewed-proposals.md` and the composition note to `docs/current-architecture.md`
- [ ] Run the simplification pass over this branch's diff and record it under a dated `## Simplification pass` heading in the plan document
- [ ] Verification run — the Release build of `Pegasus.Desktop.Infrastructure`, the desktop view-model test project, the architecture test project, and the attached operator render record — captured as `proof` at tier 3
- [ ] Open the PR into `dev`

## Progress notes

(append with `set_ticket_doc(doc: "checklist", append: true)`)
