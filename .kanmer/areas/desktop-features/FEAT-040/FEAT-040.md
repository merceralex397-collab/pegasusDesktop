---
id: FEAT-040
type: ticket
title: >-
  DSK-07-14 · Desktop report renderer: Scriban + isolated WebView2
  PrintToPdfStreamAsync + PDFsharp post-processing
status: backlog
area: desktop-features
assignee: ''
profile: feature
labels:
  - desktop-conversion
  - plan-07
  - phase-7
  - tier-3
  - needs-operator
groups:
  - EPIC-008
  - HZN-008
links: []
refs:
  - docs/frd/frd-11-reports-correspondence-and-reviewed-proposals.md
docs_todo: true
archived: false
created: '2026-08-24T08:30:09.634Z'
updated: '2026-08-24T08:30:09.634Z'
---

## What

Implement `IAssessmentReportRenderer` in `src/Pegasus.Desktop.Infrastructure` using the shared Scriban templates, an **isolated, off-screen, never-UI** WebView2 host calling `CoreWebView2.PrintToPdfStreamAsync`, and the same PDFsharp post-processing the gateway renderer performs — producing the assessment report and fee note from one snapshot, one render at a time, with a named failure and gateway fallback when the WebView2 runtime is missing.

## Why

Locked decision L-03 and ADR-0108 move report rendering to the desktop so a report can be produced and previewed without the web application (proposal § 12.5, § 27 item 6). The existing renderer, `src/Pegasus.Infrastructure/Reports/PlaywrightAssessmentReportRenderer.cs` (326 lines), runs Chromium inside the Web Container App and is the reason that container is sized cpu 1.0 / 2Gi. This ticket reproduces its output locally without reproducing its hosting cost. The plan flags an open question this ticket must answer first: whether a collapsed WinUI `WebView2` control or a `CoreWebView2Controller` on a hidden HWND is the cleaner off-screen host. Siblings: [[DSK-07-13]] supplies the identical templates, [[DSK-07-15]] proves parity, [[DSK-07-16]] stores the result, [[DSK-05-18]] is the case-workspace slice.

## Source of truth

- Plan row: `docs/desktop/07-integrations/README.md` § 5 — `DSK-07-14`
- Plan context: `docs/desktop/07-integrations/README.md` § 2 (the WebView2 printing documentation facts, fetched 2026-08-23), § 3 (Deviation L-03), § 7 Risks and traps (off-screen hosting, one print per WebView, runtime missing, Chromium drift)
- Screen spec: `docs/desktop/06-ui-design/screen-specs.md` § `§13.9 Assessment, valuation and reporting` (Generate report draft = local WebView2 render, progress in the status bar, cancel; Preview is a document viewer, not Pegasus UI in a WebView)
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 12.5 Documents, PDFs and reports, § 23.2 Native verification (the isolated-WebView2 exception)
- Repository evidence: `src/Pegasus.Core/Reports/AssessmentReportRendering.cs:272-310` (`RenderedReportArtifact`, `AssessmentReportDraft`, `IAssessmentReportRenderer`, and `GenerateAssessmentReportDraft`'s SHA-256 provenance check that rejects a mismatched artifact); `src/Pegasus.Infrastructure/Reports/PlaywrightAssessmentReportRenderer.cs:19` (`SemaphoreSlim(1,1)`), `:23-80` (the two Scriban contexts built from one `AssessmentReportSnapshot`), `:100-129` (template parse, unresolved-placeholder rejection, `SetContentAsync`, `PdfAsync` with `Format = "A4"`, `PrintBackground = true`, `DisplayHeaderFooter = true`, footer template, margins top `8mm` right `12mm` bottom `22mm` left `12mm`), `:130-142` (`PdfReader.Open` and `RenderedReportArtifact` with page count, SHA-256, `TemplateVersion` and an engine-version string), `:305-315` (`ResourceStream` naming); `src/Pegasus.Core/Reports/AssessmentReportProjection.cs`; `tests/Pegasus.IntegrationTests/Reports/AssessmentReportRendererTests.cs` (the `[Trait("Category", "Browser")]` baseline suite)
- Binding decisions: **L-03 / ADR-0108** — isolated non-UI WebView2; the gateway renderer is retained until golden-file parity passes. L-01 — the canonical store stays behind the gateway. L-02 — all evidence is produced locally.
- Depends on: `DSK-07-12` the accepted-or-proposed ADR-0108; `DSK-07-13` the shared embedded templates; `DSK-02-06` the `Pegasus.Desktop.Infrastructure` project; `DSK-02-05` the packaged desktop app

## Routing

- **Subagent**: `winui-dev` — `.codex/agents/winui-dev.toml`, then `pegasus-desktop-reviewer` — `.codex/agents/pegasus-desktop-reviewer.toml`
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `winui-dev-workflow` (`.codex/skills/winui-dev-workflow/SKILL.md`) → `microsoft-code-reference` (Microsoft Learn plugin) → `winui-code-review` (`.codex/skills/winui-code-review/SKILL.md`, the `WUI4xxx` interop rules for WebView2 initialisation)
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_fetch` on <https://learn.microsoft.com/microsoft-edge/webview2/how-to/print> and the `CoreWebView2` reference; `microsoft_code_sample_search` for `PrintToPdfStreamAsync` and `CoreWebView2Environment.CreateAsync`)
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (call `get_doc_gates <id>` before every move; a move crosses at most one gated boundary)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Orient: read the plan row, ADR-0108 from [[DSK-07-12]], the plan's § 2 WebView2 facts and § 7 trap rows, and `src/Pegasus.Infrastructure/Reports/PlaywrightAssessmentReportRenderer.cs` end to end. Call `get_doc_gates <this ticket id>`, then `take_ticket` on branch `task/dsk-07-14-desktop-renderer`.
2. Resolve the host question **first**, timeboxed, and record the answer in `research`: build a throwaway probe that renders a trivial HTML document to PDF twice — once through a zero-size collapsed WinUI `WebView2` control in a XAML root, once through `CoreWebView2Controller` created on a hidden HWND via `CoreWebView2Environment.CreateAsync`. Use `microsoft_docs_fetch` on the print how-to and `microsoft_code_sample_search` for the exact API shapes; do not code from memory. Record which host initialises reliably with no visible window, and choose it.
3. Create `WebView2AssessmentReportRenderer` in `src/Pegasus.Desktop.Infrastructure` implementing `Pegasus.Core.Reports.IAssessmentReportRenderer`. Keep the interface as the seam so the host can change later without touching callers — that is the mitigation ADR-0108 records.
4. Reproduce the composition, not a reinterpretation of it: build the two `ScriptObject` contexts exactly as `PlaywrightAssessmentReportRenderer` does from one `AssessmentReportSnapshot`, parse the templates from the embedded resources shared by [[DSK-07-13]], and keep the unresolved-placeholder rejection (`html.Contains("{{")` or `'«'` → `ReportRenderRejectedException`). Any divergence here shows up as a golden-file failure in [[DSK-07-15]] that looks like a renderer bug but is not.
5. Set the print settings to match the Playwright options one for one via `CoreWebView2PrintSettings`: A4 page size, backgrounds printed, header and footer displayed with the same footer template and an empty header, and margins top 8 mm, right 12 mm, bottom 22 mm, left 12 mm (convert to the units the settings type expects and record the conversion in a comment). Call `PrintToPdfStreamAsync`, which returns a rewound stream, and read it fully.
6. Serialise renders with a `SemaphoreSlim(1, 1)` exactly as the gateway renderer does. The documentation permits **one print operation per WebView at a time**; a parallel render throws. Add a test that two concurrent `RenderAsync` calls both succeed and neither corrupts the other's output.
7. Post-process with PDFsharp as the gateway does: `PdfReader.Open(..., PdfDocumentOpenMode.Import)` to obtain the page count, then produce `RenderedReportArtifact` with the suggested file name, bytes, page count, lowercase hex SHA-256, `AssessmentReportContract.TemplateVersion` and an engine-version string naming WebView2 and its runtime version. `GenerateAssessmentReportDraft` re-hashes and rejects a mismatch, so the hash must be of the exact bytes returned.
8. Handle a missing or outdated WebView2 runtime as a **named** failure, not an exception dump: detect it at composition (the startup check from area 04 owns the user-facing prompt), throw or return a distinct failure the caller can map to "render unavailable — use the gateway renderer", and log the runtime version found. Record the install step name rather than inventing one.
9. Register the renderer in the desktop host's DI so that the gateway path remains available: composition selects the desktop renderer when the runtime is present and the parity flag allows it, and the gateway `POST /api/v1/cases/{id}/reports/draft` remains the fallback until [[DSK-07-15]] signs off. Record the flag name in `plan`.
10. Prove the never-UI rule mechanically: the WebView2 is never navigated to an http/https Pegasus URL, hosts no application XAML, and is created off-screen. Extend the architecture test from [[DSK-02-12]] so the only permitted `WebView2` usage in the solution is this renderer type, and run `winui-code-review`'s `WUI4xxx` checks for uninitialised-WebView2 defects.
11. Add unit and adapter tests in the desktop test project: an unresolved placeholder is rejected; a cancelled render throws `OperationCanceledException` and leaves no partial artifact; the returned page count matches PDFsharp's; the SHA-256 matches the returned bytes; two concurrent renders serialise.
12. **Operator step** — run one real render of each of the four assessment outcomes on the baseline Windows 11 workstation, from the packaged app, and hand back: the four PDFs, the WebView2 runtime version reported by the app, the wall-clock render time for each, and confirmation that no window appeared during the render. Attach them to the ticket proof; [[DSK-07-15]] compares them with the gateway fixtures.
13. Run the simplification pass over the branch diff, record it under a dated `## Simplification pass` heading in the plan document, then open the PR into `dev`.

## Acceptance criteria

- [ ] Both the assessment report and the fee note render from a single `AssessmentReportSnapshot` through the shared embedded templates.
- [ ] The chosen off-screen host is recorded with evidence, and no window appears during a render.
- [ ] Print settings match the gateway renderer's page size, background printing, header/footer and four margins.
- [ ] Renders are serialised one at a time; concurrent calls do not throw or interleave.
- [ ] The artifact carries page count, SHA-256, template version and a WebView2 engine-version string, and passes `GenerateAssessmentReportDraft`'s provenance check.
- [ ] A missing WebView2 runtime produces a named failure and the gateway renderer remains available as the fallback.
- [ ] No WebView2 in the solution hosts Pegasus UI or navigates to a Pegasus URL, proven by an architecture test.

## Verification

- [ ] `dotnet build ./src/Pegasus.Desktop.Infrastructure/Pegasus.Desktop.Infrastructure.csproj -c Release` — expected: succeeds with `TreatWarningsAsErrors`.
- [ ] `dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release` — expected: placeholder-rejection, cancellation, provenance and concurrency facts pass.
- [ ] `dotnet test ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj --configuration Release` — expected: the WebView2 single-permitted-usage fact passes.
- [ ] Operator render record attached to the ticket proof — expected: four PDFs, runtime version, timings, and "no window appeared".

## Evidence tier

Tier 3 — Parser/adapter contracts.
Tier 3 obliges adapter-contract evidence: deterministic external failure handling, cancellation, resource limits, stable contract codes and integrity — here the render's provenance, its cancellation behaviour and its named runtime-missing failure.

## Documentation changes

- `docs/adr/0108-isolated-webview2-report-rendering.md` — record the chosen off-screen host in the decision's consequences (via [[DSK-07-12]]'s file, not a second ADR)
- `docs/current-architecture.md` — the renderer composition (desktop plus retained gateway fallback), after the slice ships
- `docs/frd/frd-11-reports-correspondence-and-reviewed-proposals.md` — the local-render behaviour clause

## Guardrails

- **Azure**: no write.
- **Scope boundary**: may touch `src/Pegasus.Desktop.Infrastructure`, the desktop host's DI registration, `tests/Pegasus.Desktop.ViewModelTests` and `tests/Pegasus.ArchitectureTests`. Must not modify `PlaywrightAssessmentReportRenderer.cs`, must not remove `AddPegasusReportRendering` from the Web host, and must not edit a template.
- **Traps**: one print operation per WebView at a time — parallel renders throw; a WinUI `WebView2` control needs a XAML root and a zero-size collapsed control may still initialise, but that must be proven, not assumed; the WebView2 runtime updates itself while Playwright is pinned to 1.61.0, so exact pixel equality is not the target ([[DSK-07-15]] sets tolerances); a WebView2 that ever hosts Pegasus UI breaks proposal § 23.2 and ADR-0108; the gateway renderer must stay registered until parity sign-off.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.

## Outcome

_Filled at closeout._
