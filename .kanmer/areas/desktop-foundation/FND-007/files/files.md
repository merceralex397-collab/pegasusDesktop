# Files — FND-007

## 2026-08-25 correction — documented invisible host

Microsoft Learn documents `HWND_MESSAGE` as the valid parent for an invisible `CoreWebView2Controller` on Windows 8 and later; the WebView will never become visible. The fixed design is `CoreWebView2Environment.CreateCoreWebView2ControllerAsync(HWND_MESSAGE)`. This supersedes every earlier collapsed-XAML/hidden-HWND host-selection instruction below. Phase 7 validates packaged-app initialisation, PDF output and no-window behaviour; it does not select a host. This user-directed correction also adds `docs/desktop/00-governance-and-workflow/README.md` and `docs/desktop/07-integrations/README.md` to FND-007's docs-only scope.


Surveyed 2026-08-24 against the working tree at `origin/main`
`191ddf334208b8966dc5e32f4f597e434a086233`. Every path was confirmed with `ls`
or `grep`. Exactly one file is created and **nothing existing is edited at this
merge** — including the ADR index.

## Where the change lands

| Path | Why |
| --- | --- |
| `docs/adr/0108-desktop-webview2-report-rendering.md` | **New; this correction also updates the Phase 0 and Phase 7 source plans.** `status: proposed` at first merge. Carries: the § 23.2 exception quoted from the proposal; the never-visible / never-hosts-Pegasus-UI constraint; the six-question cloud-justification table answered for report rendering; the decision to move rendering to `Pegasus.Desktop.Infrastructure` behind the existing `IAssessmentReportRenderer` port using the shared Scriban templates; the fixed documented `CoreWebView2Environment.CreateCoreWebView2ControllerAsync(HWND_MESSAGE)` host, with [[FEAT-040]] (plan handle `DSK-07-14`) supplying packaged-app validation; the retention gate on the gateway renderer; and a `## Reversal condition`. `ls docs/adr/010*` returned *No such file or directory* on 2026-08-24, so there is no existing file to extend |
| `docs/adr/README.md` | **NOT edited by this ticket** — listed here because the omission is a decision, not an oversight. The index has one accepted table (`:16`, header `| ADR \| Title \| Related FRD |` at `:18`) and **no status column**, and `:11-12` states that the current architecture *is* that table; a row would assert a `proposed` ADR as current architecture. [[FEAT-038]] (plan handle `DSK-07-12`) adds the row at acceptance |

## Context files

Read these before writing a line. Each says what it tells the implementer.

| Path | What it tells the implementer |
| --- | --- |
| `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md:1715` (§ 23.2, heading `:1701`) | **The single sentence this whole ADR exists to satisfy**: an isolated WebView2 "is not automatically a web wrapper, but it requires an ADR and must not host Pegasus UI". Quote it verbatim — a paraphrase is not a citation |
| `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md:1351` (§ 2.1, heading `:54`) | The locked constraint being excepted: "no WebView shell". The ADR must show why the exception does not swallow the rule |
| `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md:751` § 12.5 | The rendering design the ADR records |
| `docs/desktop/07-integrations/README.md:260` (§ 7) | The documented `HWND_MESSAGE` controller host, its packaged-app validation, and the retained existing `IAssessmentReportRenderer` port — not a host-selection wrapper |
| `docs/desktop/07-integrations/README.md:257-258` | The two failure modes the ADR must pre-empt: runtime missing or outdated → named failure plus gateway fallback; and golden-file drift, because "WebView2 runtime updates itself; Playwright is pinned to 1.61.0" → tolerant comparison, **not pixel equality** |
| `docs/desktop/07-integrations/README.md:112-115` | The Microsoft Learn URLs already chosen for the print how-to and the `CoreWebView2` WinRT reference — the starting point for step 2, not a substitute for fetching them |
| `docs/desktop/07-integrations/README.md:227,229,230` | The three rows that own the successor work: `DSK-07-12` → [[FEAT-038]] (the acceptance flip, profile `chore`), `DSK-07-14` → [[FEAT-040]] (renderer plus packaged-controller validation), `DSK-07-15` → [[FEAT-041]] (golden-file parity suite). Read them to see exactly what this ADR is promising on their behalf |
| `docs/adr/0028-run-integrated-renderer-in-web-container-app.md` | **The model to copy, and the decision most at risk of looking contradicted.** Its `## Status` `:13-16` shows the house form for a refining decision ("refines ADR-0015 and ADR-0025; it supersedes neither"); `:22-27` records what central rendering costs the Web image (pinned Chromium, native Linux dependencies, fonts, writable temp); `:33-36` states that FRD-11 report behaviour "remain[s] governed by FRD-11 and `Pegasus.Core` rather than by this ADR"; and `:57-60` already requires "measured evidence… and a new accepted ADR" before the renderer changes host — which is what ADR-0108 plus the parity gate supply |
| `docs/adr/0025-integrate-renderer-and-extractor-into-the-application.md:30-36` | Why the templates are product behaviour and must co-version with the FRDs and Core policy that feed them — the reason the desktop renderer must consume the same governed source rather than a copy |
| `docs/adr/README.md:11-14` | The two facts that shape the whole ticket: the current architecture **is** the accepted table, and **published bodies are immutable**. Together they mean no index row now, and no body edit later |
| `docs/adr/README.md:16-19` | The accepted table's real shape — heading `:16`, three-column header `:18`, separator `:19`. What [[FEAT-038]] will add a row to, and what this ticket must leave alone |
| `docs/adr/0029-image-initiated-case-projection.md:11-20` | The newest house heading form, opening at `## Status` — the model, in preference to `docs/adr/0015-…`, which has no `## Status` section at all |
| `AGENTS.md:107-110` | The template and the reason Status comes first: "so a body-only read is never mistaken for current when it is superseded". For a `proposed` ADR this is the most load-bearing line in the conventions |
| `AGENTS.md:114-116` | **Read it knowing it is wrong.** It describes a five-column index (`ID \| Title \| Status \| Superseded-by \| Owner capability`) that would appear to give a `proposed` ADR a row. `docs/adr/README.md:18` contradicts it and the file wins. **[[FND-005]] owns the correction — do not make it here** |
| `src/Pegasus.Core/Reports/AssessmentReportRendering.cs:284` | The `IAssessmentReportRenderer` port the desktop implementation plugs into, and `:291` the `GenerateAssessmentReportDraft` use case that consumes it. The ADR adds a second implementation of an existing port — that is the whole architectural claim |
| `src/Pegasus.Infrastructure/Reports/PlaywrightAssessmentReportRenderer.cs:5-13,92,140` | The renderer being kept in parallel: `Microsoft.Playwright` + `Scriban` + `PdfSharp.Pdf.IO`, 326 lines, stamping a producer string that names the Playwright version and Chromium. What "retained until parity" concretely retains |
| `src/Pegasus.Infrastructure/DependencyInjection.cs:448` | `services.AddSingleton<IAssessmentReportRenderer, PlaywrightAssessmentReportRenderer>()` — the single registration a desktop composition root will parallel, never replace |
| `docs/design/assets/report-renderer/templates/` | The seven governed template files (`assessment_report.scriban`, `assessment_fee_note.scriban`, `expert_report.scriban`, `fee_note.scriban`, `advert_evidence_pack.scriban`, `market_valuation_evidence.scriban`, `report.css`). The ADR says "the same templates"; [[FEAT-039]] (`DSK-07-13`) embeds them hash-checked into both assemblies |
| `tests/Pegasus.IntegrationTests/Reports/` | `AssessmentReportDraftWebTests.cs` and `AssessmentReportRendererTests.cs` — the existing baseline the parity fixtures come from, and the evidence `PAR-15` already records |
| `docs/desktop/01-inventory-and-parity/parity-matrix.md:60` | `PAR-15`, the row this decision moves: current entry `Cases/Assessment/Index.cshtml.cs` with `OnPostGenerateReportDraftAsync`, target "rendering local via WebView2 per L-03". Do not edit it — the matrix is owned by the area 01 tickets |
| `docs/frd/frd-11-reports-correspondence-and-reviewed-proposals.md` | The FRD that keeps report readiness, finality, provenance and approval — everything the desktop does **not** get to decide. `related_frd` points here |

## Ripple effects

- **The one deliberate non-ripple is the index.** `docs/adr/README.md` gains no
  row until acceptance. That is the ripple a reviewer will look for and must not
  find.
- **Successor work this ADR authorises, each with a named owner** — nothing here
  is unowned: [[FEAT-039]] (`DSK-07-13`) embeds the templates hash-checked;
  [[FEAT-040]] (`DSK-07-14`) writes the renderer and validates the documented
  `HWND_MESSAGE` controller from the packaged app; [[FEAT-041]]
  (`DSK-07-15`) produces the golden-file parity evidence that opens the retention
  gate; [[FEAT-042]] (`DSK-07-16`) adds the finalise endpoint that keeps report
  *registration* on the gateway; [[TEST-018]] (`DSK-08-18`) runs the parity lane
  on the local Test/UAT stack; [[FEAT-043]] (`DSK-07-17`) reconciles the upstream
  report tickets against L-03; and [[FEAT-038]] (`DSK-07-12`) performs the
  frontmatter-only acceptance flip and adds the index row.
- **No code, test or build ripple at this merge.** `src/`, `tests/`, `scripts/`
  and `.github/` are untouched; nothing regenerates. There is no `openapi/`
  directory in the repository today (`ls openapi` → *No such file or directory*),
  so the usual contract ripple does not apply.
- **No `docs/index.md` change.** It links the ADR *index* (`:21`, `:46`) and only
  ADR-0029 individually (`:56`), so a new ADR creates no dangling reference.
  Verified with `grep -n "adr" docs/index.md`.
- **Board ripple is limited and conditional.** A `proposed` ADR must **not** be
  cited by another ticket as settled authority, so `docs_todo` is *not* cleared
  on the report-rendering tickets at this merge. `link_doc` may attach the path
  to this ticket for traceability; clearing `docs_todo` waits for acceptance.

## Out of scope

Recorded so the reviewer sees each was a decision. The ticket's Guardrails
already forbid them.

- **Any renderer code** — `Pegasus.Desktop.Infrastructure`, the Scriban call, the
  `PrintToPdfStreamAsync` call, the PDFsharp post-processing. All [[FEAT-040]]'s.
- **Renderer host experimentation.** The ADR already fixes the documented `HWND_MESSAGE` host; [[FEAT-040]] validates it, but does not introduce a collapsed-XAML or arbitrary-hidden-HWND alternative.
- **Any edit to `docs/adr/0025-…` or `docs/adr/0028-…`.** Bodies are immutable
  (`docs/adr/README.md:12-14`); ADR-0108 *relates to* them and supersedes
  neither. `supersedes: []` and `superseded_by: []` stay empty on all three.
- **The `docs/adr/README.md` row** — [[FEAT-038]] adds it at acceptance.
- **The `AGENTS.md:114-116` index-shape correction** — [[FND-005]]'s
  (`DSK-00-05`), even though this ticket must read the wrong sentence to avoid
  following it.
- **The other ten ADRs of the reserved block** — ADR-0100/0101/0103/0104/0105/0110
  are [[FND-005]]'s and ADR-0102/0106/0107/0109 are [[FND-006]]'s. This ticket
  writes exactly one file.
- **`docs/desktop/01-inventory-and-parity/parity-matrix.md`** — `PAR-15` is cited,
  never edited; the matrix belongs to the area 01 tickets.
- **Golden-file fixtures and any `dotnet test` run.** Tier 3 evidence owned by
  [[FEAT-041]]; this ticket cites it at acceptance and never re-runs it.
- **Any Azure read or write.** Nothing is deprovisioned: ADR-0025 and ADR-0028
  keep the gateway renderer in the Web Container App until the parity gate passes.
