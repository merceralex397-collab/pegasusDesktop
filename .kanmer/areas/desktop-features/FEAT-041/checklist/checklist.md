# Checklist — FEAT-041

One box per plan step, in plan order. The last box produces `proof`.

- [ ] Read `docs/desktop/07-integrations/README.md` § 5 row `DSK-07-15`, its § 4 Phase 7 exit-gate row and its § 7 Chromium-drift trap row, and `tests/Pegasus.IntegrationTests/Reports/AssessmentReportRendererTests.cs` end to end (158 lines).
- [ ] Call `get_doc_gates FEAT-041` and confirm `leave-preparing` is passable, then `take_ticket` on branch `task/dsk-07-15-golden-file-parity`.
- [ ] Record the five-case fixture catalogue under a dated heading in the `plan` document — the four `AssessmentReportOutcome` values and the `CE-STRESS-DENSITY` case — each with the one sentence saying what it is for.
- [ ] Add the six `PEGASUS_RENDER_EVIDENCE` lines (mirroring `AssessmentReportRendererTests.cs:53-59`) to the density test at `:62-98`, changing no existing assertion.
- [ ] Run the browser filter with `PEGASUS_RENDER_EVIDENCE` set to a capture directory and collect the ten baseline PDFs (assessment + fee note for each of the five cases).
- [ ] Create `tests/Pegasus.IntegrationTests/Reports/fixtures/manifest.md` recording per fixture: purpose, `PlaywrightVersion` read from `Directory.Build.props:17`, `AssessmentReportContract.TemplateVersion` read from `src/Pegasus.Core/Reports/AssessmentReportRendering.cs:8`, capture date and Chromium build.
- [ ] Settle assumption `A-07-15-1`: extract the five named anchors from one captured fixture twice with PdfPig `Page.GetWords()` and confirm identical coordinates; record the result (or the reduced-family fallback) in the manifest.
- [ ] Write the four tolerance families into the manifest — text (present), values (money and date tokens identical), page count (exactly equal), anchor positions (absolute tolerance in PDF points) — including the verbatim sentence "Pixel equality is explicitly not the target."
- [ ] Commit the ten fixture PDFs under `tests/Pegasus.IntegrationTests/Reports/fixtures/` and confirm they are tracked (not caught by `.gitignore`'s `**/artifacts/`).
- [ ] Write `tests/Pegasus.IntegrationTests/Reports/ReportParityAssertions.cs` holding the four tolerance families once, with the expected engine token as a **parameter** rather than the hard-coded `"Playwright"` of `AssessmentReportRendererTests.cs:118`.
- [ ] Write `tests/Pegasus.IntegrationTests/Reports/ReportFixtureCatalogue.cs` holding the five cases, their snapshot builders and their expected token and anchor sets.
- [ ] Write `tests/Pegasus.IntegrationTests/Reports/ReportFixtureCaptureTests.cs` rendering each case through `GenerateAssessmentReportDraft` and asserting it against its committed fixture, carrying `[Trait("Category", "Browser")]`.
- [ ] Confirm no `Sha256` comparison exists between the two renderers' artifacts anywhere in the harness.
- [ ] Add a `PdfPig` `PackageReference` and a linked `<Compile Include="..\Pegasus.IntegrationTests\Reports\ReportParityAssertions.cs" …/>` item to `tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj`, and compile it — settling assumption `A-07-15-2`.
- [ ] Write `tests/Pegasus.Desktop.ViewModelTests/Reports/ReportParityTests.cs` rendering the same catalogue cases through [[FEAT-040]]'s `WebView2AssessmentReportRenderer` and asserting through the same helper, named so `FullyQualifiedName~ReportParity` selects them.
- [ ] Make every mismatch write both PDFs and a text diff to the test output directory, and name the fixture, the tolerance family, the anchor or token and the measured delta in the assertion message.
- [ ] Write the drift-review procedure into the manifest: a re-baseline requires a pull request approved by an agent that did not capture the new fixture, and the new WebView2 runtime version and capture date land in the manifest in the same commit.
- [ ] Confirm the gateway capture tests are selected by the existing `browser` filter `Category=Browser&Category!=Corpus` (`.github/workflows/ci.yml:230-234`) and that no new CI lane was added.
- [ ] Re-measure the `browser` lane duration with the five capture cases included against its `timeout-minutes: 25` cap; if at risk, report it to [[TEST-019]] (plan handle `DSK-08-19`) rather than opening a third lane.
- [ ] Produce the sign-off results table — one row per fixture, four tolerance-family columns, plus the Playwright version, the WebView2 runtime version and the template version — and attach it to the ticket proof for [[FEAT-038]] and [[FEAT-042]].
- [ ] Run the deliberate negative test: alter one fee-note money token in a scratch copy, confirm the suite fails naming fixture, family and token, capture the output, revert, and confirm `git status --porcelain -- docs/design` is empty.
- [ ] Edit the Verification section of `docs/adr/0108-desktop-webview2-report-rendering.md` to cite this suite — **without** touching its frontmatter status or `docs/adr/README.md`, both of which are [[FEAT-038]]'s.
- [ ] Move the report rows of `docs/desktop/01-inventory-and-parity/parity-matrix.md` (`PAR-15` at `:60`) to `automated verification passed`, extending the evidence column rather than replacing it.
- [ ] Run the simplification pass over this branch's own diff and record it under a dated `## Simplification pass` heading in the `plan` document.
- [ ] Run the full verification set and capture its output as `proof`: the browser-filter run, the `FullyQualifiedName~ReportParity` run, the negative-test capture, `git diff --exit-code openapi/pegasus-v1.json`, `git diff --stat origin/dev -- src/` (expected empty), and the results table. Then open the PR into `dev`.

## Progress notes

(append with `set_ticket_doc(doc: "checklist", append: true)`)
