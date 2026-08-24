# Files — FND-007

Surveyed 2026-08-24 against the working tree at `origin/main` `191ddf3342…`.
Every path was confirmed with `ls`, `wc -l`, `grep -n` or `sed -n`.

## Where the change lands

| Path | Why |
| --- | --- |
| `docs/adr/0108-desktop-webview2-report-rendering.md` | **New**, and the only file this ticket writes at first merge. `status: proposed`, `supersedes: []`, `superseded_by: []`, `related_frd: [frd-11]`. Confirmed absent: `ls docs/adr/0108*` returns nothing. **This exact path is also named by [[FEAT-038]]** (plan handle `DSK-07-12`) at its steps 2, 3 and Guardrails — one ADR ID, one file. If the file already exists, [[FEAT-038]] created it: edit in place, never create a second |
| `docs/adr/README.md` | **Not edited at this merge.** The accepted table (`:16`, header `:18-19`) has columns `ADR \| Title \| Related FRD` and no status column, so a row here would assert ADR-0108 as current architecture. [[FEAT-038]] adds the single row in the frontmatter-only acceptance PR |

## Context files

What the implementer must read to avoid a trap, and what each one tells them.

| Path | What it tells the implementer |
| --- | --- |
| `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md:60` | The locked § 2.1 constraint in its exact words: "It must not be a WebView/WebView2 shell around the current application." This is the sentence ADR-0108 has to be reconciled against, not paraphrased |
| `…Proposal.md:1715` | The § 23.2 exception, and the reason this ticket exists: an isolated WebView2 use "is not automatically a web wrapper, but it requires an ADR and must not host Pegasus UI" |
| `…Proposal.md:1701-1713` | The § 23.2 release-gate list — "no WebView renders the legacy Pegasus application", "no required workflow launches the legacy site". Write `## Decision` so a reviewer can check the renderer against these lines |
| `docs/adr/0028-run-integrated-renderer-in-web-container-app.md:13-16` | The exact wording precedent for ADR-0108's `## Status`: "Accepted on 2026-08-19. This decision refines ADR-0015 and ADR-0025; it supersedes neither." ADR-0108 relates the same two decisions and supersedes neither |
| `docs/adr/0028-*.md` § Context and § Decision | Why the renderer sits in the Web Container App at all — pinned Chromium, matching native Linux dependencies, fonts, writable temp space — and the sentence that fixes the split ADR-0108 records: readiness, accepted inputs, immutable identity and hash, correction, approval and failure behaviour "remain governed by FRD-11 and `Pegasus.Core` rather than by this ADR" |
| `docs/adr/0025-integrate-renderer-and-extractor-into-the-application.md` | 114 lines; the decision that folded CollisionRenderer in behind a `Pegasus.Core`-owned port. Immutable — cited, never edited |
| `docs/adr/0029-image-initiated-case-projection.md:1-30` | The current house form to copy: frontmatter, `# ADR-01NN: <title>`, `## Status` first, then `## Context`. Also that `related_frd` values are **lowercase stems** (`[frd-11]`, never `[FRD-11]`) — every `related_frd:` line in `docs/adr/*.md` follows this |
| `docs/adr/README.md:16-19` | Three-cell accepted table with **no status column** — the executable reason step 8 adds no row. Ignore the `AGENTS.md:114-117` sentence describing a five-column index; the file wins, and [[FND-005]] (plan handle `DSK-00-05`) owns that correction |
| `docs/desktop/07-integrations/README.md:255` | The risk row that ADR-0108 must not resolve: a WinUI `WebView2` control needs a XAML root, a zero-size collapsed control *may* still initialise but must be proven by the [[FEAT-040]] spike, and `CoreWebView2Controller` on a hidden HWND is the fallback. "Record the chosen host in ADR-0108; keep the renderer behind `IAssessmentReportRenderer` so the host can change" |
| `docs/desktop/07-integrations/README.md:227,229,230` | The § 5 rows for `DSK-07-12` ([[FEAT-038]]), `DSK-07-14` ([[FEAT-040]]) and `DSK-07-15` ([[FEAT-041]]) — what each owes, so the ADR defers to the right ticket by name |
| `docs/desktop/07-integrations/README.md:112-114` | The Microsoft Learn pages the plan already identified — the WebView2 print-to-PDF how-to and the `CoreWebView2.PrintToPdfAsync` / `PrintToPdfStreamAsync` / `CoreWebView2PrintSettings` reference. Re-fetch at ticket time and record the URL and date |
| `docs/desktop/01-inventory-and-parity/flow-records.md:362-433` (record 6) | The whole current-state picture: entry point, the 312-line Core contract, the 326-line Playwright implementation with its `SemaphoreSlim(1,1)`, the template set, the Playwright/container version pin, and § "What the desktop needs" at `:404-413` — which is the decision ADR-0108 records. Q6.1–Q6.4 at `:414-426` are what it defers |
| `src/Pegasus.Core/Reports/AssessmentReportRendering.cs` | 312 lines; defines `IAssessmentReportRenderer`, the port the desktop implementation must satisfy. The ADR's "behind `IAssessmentReportRenderer`" clause is only meaningful if this is read first |
| `src/Pegasus.Infrastructure/Reports/PlaywrightAssessmentReportRenderer.cs` | 326 lines; the behaviour the desktop renderer must match — snapshot → Scriban → HTML → `PdfAsync` → PDFsharp post-processing, single-flight, lazily created and cached browser. It is also the source of the golden-file baseline |
| `src/Pegasus.Infrastructure/DependencyInjection.cs:446` | `AddPegasusReportRendering()` — where the singleton is registered today, and what the desktop composition has to mirror |
| `docs/design/assets/report-renderer/templates/` | **Six** `.scriban` files (`advert_evidence_pack`, `assessment_fee_note`, `assessment_report`, `expert_report`, `fee_note`, `market_valuation_evidence`) plus `report.css`, LF-forced by `.gitattributes`. Which of them are in desktop scope is Q6.1 / upstream TICK-206, not this ADR's to decide |
| `tests/Pegasus.IntegrationTests/Reports/` | `AssessmentReportRendererTests.cs` and `AssessmentReportDraftWebTests.cs` — the existing suite [[FEAT-041]] reuses for the parity baseline. The ADR cites this evidence; it does not run it |
| `docs/frd/frd-11-reports-correspondence-and-reviewed-proposals.md` | 196 lines; the FRD that keeps authority over report finality. `related_frd: [frd-11]` must be true, not decorative |
| `docs/desktop/00-governance-and-workflow/README.md` § 3 | The ADR set table row for ADR-0108 and the cloud-justification table to paste into `## Context`. While the ADR is `proposed`, this table plus this ticket are its only discoverability |
| `.codex/agents/pegasus-desktop-reviewer.toml` | The read-only subagent that owns the judgement "this is not a web shell" |

## Ripple effects

- **`docs/adr/README.md` is deliberately untouched now and edited later** by
  [[FEAT-038]] in the acceptance PR. A reviewer expecting an index row with this
  merge is reading the wrong ticket; the `## Documentation changes` section of
  the body says so.
- **`scripts/Test-DocumentationLinks.ps1`** resolves every relative link in the
  new file. Any forward reference to a document that does not exist yet — FRD-13,
  a desktop renderer file — must be written as prose, not as a relative link, or
  the CI `documentation` job fails.
- **[[FEAT-038]], [[FEAT-040]], [[FEAT-041]] and [[FEAT-042]]** all build against
  this ADR. [[FEAT-040]] records the host choice into it; [[FEAT-041]] supplies
  the parity evidence; [[FEAT-038]] flips the status and adds the row;
  [[FEAT-042]] registers the finalised PDF into custody.
- **[[FND-008]]** (plan handle `DSK-00-08`) cites ADR-0108 from FRD-13 and must
  mark it as still `proposed` — a `proposed` ADR is not settled authority.
- **No code, no test, no contract.** `openapi/pegasus-v1.json`, the generated
  client and every `src/` project are unaffected. Say so in the
  post-implementation report so a reviewer does not go looking.

## Out of scope

- **Renderer code** — `Pegasus.Desktop.Infrastructure`, the WebView2 host, the
  Scriban wiring and the PDFsharp post-processing all belong to [[FEAT-040]].
- **Golden-file fixtures and tolerances** — [[FEAT-041]].
- **The acceptance flip and the index row** — [[FEAT-038]], as a frontmatter-only
  PR. This ticket verifies that PR against the reversal condition and the § 23.2
  statement, and performs **no edit to ADR-0108 after the first merge**.
- **ADR-0025 and ADR-0028 bodies** — immutable. Related, cited, never edited.
- **The `AGENTS.md:114-117` index-shape sentence** — [[FND-005]] owns that
  one-line correction; this ticket does not touch `AGENTS.md`.
- **Which templates are in desktop scope** — Q6.1 / upstream TICK-206, not this
  ADR's decision.
- **`docs/desktop/01-inventory-and-parity/flow-records.md`** — read and cited,
  never edited (including the six-versus-seven template count noted in
  `research`); [[FND-020]] owns it.
- **Azure** — no write, no read.
