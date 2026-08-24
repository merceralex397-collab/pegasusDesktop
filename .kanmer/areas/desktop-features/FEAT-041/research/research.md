# Research — FEAT-041: Golden-file parity suite for the two report renderers

## Question

What already exists in this repository that a golden-file parity suite can be built **on** rather
than beside — which assertions, which fixture capture mechanism, which PDF libraries, which CI lane
— and what properties must the comparison assert so that it is a real gate on ADR-0108's acceptance
rather than a green run that proves nothing?

## Current behaviour

The web application does not do this today: there is one renderer, so there is nothing to compare it
with. What exists is the **baseline** the fixtures are captured from.

- `src/Pegasus.Infrastructure/Reports/PlaywrightAssessmentReportRenderer.cs` (326 lines,
  `wc -l`) is the sole `IAssessmentReportRenderer` implementation (`:13`). It renders Scriban
  templates to HTML, checks for unresolved placeholders (`:105-114`), drives Chromium through
  `page.PdfAsync` with A4 / `PrintBackground` / `DisplayHeaderFooter` / margins
  `8mm 12mm 22mm 12mm` (`:120-127`), and post-processes with PDFsharp `PdfReader.Open` for the page
  count (`:133`).
- `src/Pegasus.Core/Reports/AssessmentReportRendering.cs:272-278` defines
  `RenderedReportArtifact(SuggestedFileName, Pdf, PageCount, Sha256, TemplateVersion,
  EngineVersion)`. `GenerateAssessmentReportDraft` (`:291`) re-hashes both artifacts and throws
  `ReportRenderRejectedException` (`:312`) on mismatch — so provenance is already enforced by Core
  and this suite does not need to re-prove it.
- `tests/Pegasus.IntegrationTests/Reports/AssessmentReportRendererTests.cs` (158 lines) is the
  existing renderer suite and the direct ancestor of this work. See Findings.
- `tests/Pegasus.IntegrationTests/Reports/AssessmentReportDraftWebTests.cs` (259 lines) exercises
  the same renderer through the web caller.

**Parity-matrix row.** `docs/desktop/01-inventory-and-parity/parity-matrix.md:60` — **`PAR-15`**,
"13.9 Assessment and reporting", FRD-11/FRD-06, entry point
`Cases/Assessment/Index.cshtml.cs` (740), whose test-evidence column already names
`tests/Pegasus.IntegrationTests/Reports/AssessmentReportDraftWebTests.cs` and
`AssessmentReportRendererTests.cs`. That row is at status `inventoried` and this ticket's
documentation change moves the report rows to `automated verification passed`. The matrix holds
**46** rows (`grep -c '^| PAR-' docs/desktop/01-inventory-and-parity/parity-matrix.md` → 46), all
keyed to page models under `src/Pegasus.Web/Pages/**`; `PAR-15` is the only one this suite proves.

## Findings

- **The existing suite already asserts most of what parity needs, on the gateway renderer.**
  `AssessmentReportRendererTests.cs:14-20` is a `[Theory]` over the four `AssessmentReportOutcome`
  values (`TotalLoss`, `Repairable`, `CashInLieu`, `ContractRepair`) carrying
  `[Trait("Category", "Browser")]` (`:15`).
  - Text assertions are extracted with `UglyToad.PdfPig` through a `PdfText` helper (`:131-135`)
    that concatenates `document.GetPages().Select(page => page.Text)`.
  - The per-outcome title assertion is a `switch` at `:30-37` (`"TOTAL LOSS REPORT"`,
    `"REPAIRABLE REPORT"`, `"CASH IN LIEU REPORT"`, `"CONTRACT REPAIR REPORT"`), followed by
    `"Vehicle Images"`, `"Statement of Truth"`, `"Front bumper"` and the outcome phrase
    (`:38-41`).
  - The fee note is asserted separately at `:43-51`: `"FEE NOTE"`, `"Subtotal (Net)"`,
    `"VAT @ 20%"`, `"TOTAL DUE"`, `"Lloyds Bank"`, `"30-12-80"`, `"50858868"` and
    `AssessmentReportContract.VatNumber`.
  - `AssertArtifact` (`:112-119`) checks the `%PDF` magic bytes, `PageCount >= 1`, a 64-character
    SHA-256, `TemplateVersion` equal to the contract constant, and that `EngineVersion` contains
    `"Playwright"`. **That last assertion is engine-specific** and is exactly the one a shared
    helper must not copy verbatim to the desktop side.
- **`PEGASUS_RENDER_EVIDENCE` is the capture mechanism the body's step 3 refers to, and it already
  works.** `AssessmentReportRendererTests.cs:53-59`: when the environment variable is non-blank the
  test creates the directory and writes `{outcome}-{SuggestedFileName}` for both the assessment and
  the fee note. It is only wired into the `[Theory]`, **not** into the density test at `:62-98` —
  so capturing the density fixture needs the same six lines added there, which is a change to an
  existing test file and must be additive.
- **The density case is the fifth fixture and it is richer than the four outcomes.**
  `:64-98` builds `CE-STRESS-DENSITY` with 80 new parts, 80 repairs, 80 operations and 8 photos,
  then asserts `pages.Length >= 8` (`:88`), the reference on **every** page (`:89`), the 080th item
  of each list (`:90-92`), `"Statement of Truth"`, `"A Patterson"`, the absence of `{{` and `«`
  (`:95-96`), and `pages.Sum(page => page.GetImages().Count()) >= 8` (`:97`). Page count, per-page
  header text and embedded-image count are all already-proven properties and transfer directly to
  the tolerance set.
- **PdfPig and PDFsharp are both already available, and PdfPig can give word positions.**
  `src/Pegasus.Infrastructure/Pegasus.Infrastructure.csproj:23-24` references `PdfPig` 0.1.15 and
  `PDFsharp` 6.2.4; `Pegasus.IntegrationTests` gets both transitively through its project
  reference. PdfPig's `Page.GetWords()` exposes bounding boxes in PDF points, which is what the
  body's "key element positions … within a stated absolute tolerance in points" is measured with.
  PDFsharp gives `PageCount`.
- **Playwright is pinned; WebView2 is not.** `Directory.Build.props:17`
  `<PlaywrightVersion>1.61.0</PlaywrightVersion>`, single-sourced deliberately (`:10-16` explains
  that the package and the container base image "cannot silently desynchronise");
  `tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj:17` pins `Microsoft.Playwright`
  to `1.61.0`. The WebView2 Evergreen runtime updates itself on the workstation. This asymmetry is
  the whole reason the comparison is tolerant rather than exact, and it is recorded as a trap in
  `docs/desktop/07-integrations/README.md` § 7.
- **The `browser` CI lane exists and has headroom constraints already reasoned about.**
  `.github/workflows/ci.yml:207-234`: `runs-on: windows-latest`, `timeout-minutes: 25`, a
  Playwright-browser cache keyed on `tests/Pegasus.IntegrationTests/packages.lock.json`, an
  unconditional `playwright.ps1 install chromium` step whose comment says it is never gated on a
  cache hit so a poisoned cache is not trusted, and the test step's filter
  `Category=Browser&Category!=Corpus` with `xUnit.MaxParallelThreads=2` (`:230-234`). The inline
  comment records why parallelism is halved: each browser test starts a Chromium, a loopback
  Kestrel host and its own restored database.
- **Fixture bytes are safe to commit; corpus material is not.** `.gitignore:1-2` ignores `/corpus/`
  with the comment "Never commit operational emails or case files"; `:20-21` ignore
  `**/artifacts/` and `/artifacts/`. The fixtures this suite captures come from the synthetic
  `Snapshot(...)` helper (`AssessmentReportRendererTests.cs:137-150` — "Alex Example",
  `PK12 TMZ`, a screenshot from `reference/eva_information/…`), so they are not corpus.
  `.gitattributes:18` already marks `*.pdf binary`.
- **The templates are LF-pinned and shared.** `.gitattributes:4-5` pin
  `docs/design/assets/report-renderer/**/*.css` and `**/*.scriban` to LF.
  `ls docs/design/assets/report-renderer/templates/` returns **six** `.scriban` files
  (`advert_evidence_pack`, `assessment_fee_note`, `assessment_report`, `expert_report`,
  `fee_note`, `market_valuation_evidence`) **plus `report.css`** — seven governed files, of which
  `Pegasus.Infrastructure` embeds only `assessment_report.scriban`, `assessment_fee_note.scriban`
  and `report.css`. The suite's fixture catalogue therefore covers the assessment report and its
  fee note and nothing else; the other four templates are not rendered by either renderer today.
- **The desktop test project does not exist yet.** `ls tests/` returns exactly
  `Pegasus.ArchitectureTests`, `Pegasus.Core.Tests`, `Pegasus.IntegrationTests`. `ls src/` returns
  exactly `Pegasus.Core`, `Pegasus.Infrastructure`, `Pegasus.Web`, `Pegasus.Worker`. Both halves of
  the desktop side are created by named earlier tickets — see the `files` document.

### Facts

Each verified by reading the repository at fork `main`, on 2026-08-24.

| Fact | Source |
| --- | --- |
| One renderer implementation exists; it is `internal sealed` | `src/Pegasus.Infrastructure/Reports/PlaywrightAssessmentReportRenderer.cs:13` |
| Renders are serialised by `SemaphoreSlim(1, 1)` | same file `:19` |
| Print setup: A4, backgrounds, header/footer, margins `8mm/12mm/22mm/12mm`, empty header `"<span></span>"` | same file `:120-127` |
| `EngineVersion` is `$"Playwright/{version}; Chromium"` | same file, `Artifact(...)` |
| `RenderedReportArtifact` carries `PageCount`, `Sha256`, `TemplateVersion`, `EngineVersion` | `src/Pegasus.Core/Reports/AssessmentReportRendering.cs:272-278` |
| `AssessmentReportContract.TemplateVersion` is `"rendererref1-v1"` | same file `:8` |
| `AssessmentReportContract.VatNumber` is `"262 0937 10"` | same file `:9` |
| Core re-hashes and rejects mismatched provenance | same file `:291-307`, exception at `:312` |
| Existing suite: 158 lines, `[Theory]` over four outcomes, `[Trait("Category","Browser")]` | `tests/Pegasus.IntegrationTests/Reports/AssessmentReportRendererTests.cs:14-20` |
| `PEGASUS_RENDER_EVIDENCE` writes both PDFs, only from the `[Theory]` | same file `:53-59` |
| Density case asserts ≥ 8 pages, per-page reference, ≥ 8 embedded images | same file `:88`, `:89`, `:97` |
| A separate non-Browser fact asserts only `andy_patterson.png` is embedded | same file `:100-110` |
| `PdfPig` 0.1.15, `PDFsharp` 6.2.4 | `src/Pegasus.Infrastructure/Pegasus.Infrastructure.csproj:23-24` |
| Playwright pinned to `1.61.0` in one place | `Directory.Build.props:17`; test project `:17` |
| `browser` lane, filter and parallelism cap | `.github/workflows/ci.yml:207-234`, filter at `:230-234` |
| `/corpus/` ignored; `**/artifacts/` ignored | `.gitignore:1-2`, `:20-21` |
| `*.pdf binary`; templates LF-pinned | `.gitattributes:18`, `:4-5` |
| Templates: six `.scriban` + `report.css` | `ls docs/design/assets/report-renderer/templates/` |
| Parity matrix has 46 `PAR-` rows; `PAR-15` is the report row | `grep -c '^| PAR-' …/parity-matrix.md` → 46; row at `:60` |
| Neither `src/Pegasus.Desktop.Infrastructure` nor `tests/Pegasus.Desktop.ViewModelTests` exists today | `ls src/`, `ls tests/` |

### Assumptions

- **`A-07-15-1` — PdfPig 0.1.15's `Page.GetWords()` returns bounding boxes stable enough to anchor
  on.** The existing suite uses only `page.Text` and `page.GetImages()`, so word-level geometry is
  unexercised in this repository. *Confirmed by*: extracting the five named anchors from one
  captured fixture twice and asserting identical coordinates, in plan step 4 before any tolerance
  number is written. *Breaks if wrong*: position tolerance cannot be asserted and the catalogue
  falls back to text, values, page count and image count — a weaker but still real gate; the
  fallback is recorded rather than the check dropped silently.
- **`A-07-15-2` — the desktop test project can reference `PdfPig` and consume a linked source file
  from `tests/Pegasus.IntegrationTests`.** The desktop project targets
  `net10.0-windows10.0.26100.0` ([[TEST-004]] (plan handle `DSK-08-04`) title) while
  `Pegasus.IntegrationTests` targets `net10.0`; PdfPig would need its own `PackageReference` there
  rather than arriving transitively. *Confirmed by*: adding the reference and compiling, plan
  step 6. *Breaks if wrong*: the shared helper moves into a small `tests/Pegasus.TestSupport`
  project instead of a linked file — more scaffolding, same single-assertion-set property.
- **`A-07-15-3` — a fixture PDF captured on one machine is byte-reproducible enough that committing
  it is useful.** It is *not* assumed to be byte-identical run to run — PDF creation dates and
  object ordering vary — which is precisely why the comparison is over extracted properties, never
  over file hashes. *Confirmed by*: capturing the same fixture twice and observing that the
  extracted property set matches while the SHA-256 does not. *Breaks if wrong*: nothing; the
  design already assumes the weaker property.
- **`A-07-15-4` — the desktop renderer's `EngineVersion` will name WebView2 and its runtime
  version.** [[FEAT-040]] (plan handle `DSK-07-14`) step 8 states this. *Confirmed by*: reading
  the landed renderer. *Breaks if wrong*: the manifest cannot record the runtime version from the
  artifact and must take it from the host instead.
- **`A-07-15-5` — capturing the density fixture needs only the six `PEGASUS_RENDER_EVIDENCE` lines
  added to the existing density test.** *Confirmed by*: running the capture with the variable set
  and observing the file appear. *Breaks if wrong*: the capture is driven from a new test rather
  than an edit to an existing one, which is the preferable shape anyway.

## Execution placement

The six-question cloud-justification test from
`docs/desktop/00-governance-and-workflow/README.md` § 3 (`:166-178`), answered for the
responsibility this ticket places: **running the two-renderer parity comparison and holding the
authority to re-baseline a fixture.**

| Question | Answer | Evidence |
| --- | --- | --- |
| Shared authority — must several users see and update the same state? | **No** | The fixtures and the manifest are committed repository files changed through pull request; a comparison run produces per-run test output only. There is no shared mutable state between two people running the suite. |
| Unattended execution — must it run with every desktop closed? | **Yes** — and it lands on the GitHub-hosted `windows-latest` runner that already carries the `browser` lane (`.github/workflows/ci.yml:207-234`), plus the desktop lane [[TEST-013]] (plan handle `DSK-08-13`) adds. **Not Azure**; no Azure resource is involved, and which runner hosts it after C-01 makes minutes billable is [[TEST-019]] (plan handle `DSK-08-19`)'s decision. | The existing lane runs on push with no operator present. |
| Protected credentials — long-lived secret that must not sit on workstations? | **No** | The suite drives Chromium through Playwright and WebView2 through the local runtime. No provider key, no Key Vault reference, no token. Nothing in `infra/modules/platform.bicep` is read or written. |
| Public callback — must an external service call a stable public endpoint? | **No** | Nothing calls in. Both renderers are driven in-process from a synthetic `AssessmentReportSnapshot`. |
| Central enforcement — revocation, permissions, audit, invariant independent of the client? | **Yes** — the re-baseline authority. A fixture must not be re-captured by whoever is looking at a red test. That responsibility lands on the **fork repository's pull-request review** (`AGENTS.md` § Repository task workflow step 5: an agent that did not implement) and the named approver recorded in the fixture manifest — an in-house authority, not a cloud service. | Body step 8 requires the review; Guardrails call a silent re-baseline the failure. |
| Measured operational advantage — measured evidence that central is materially better? | **No** | L-02 puts Test/UAT on a local production-mimicking stack, and [[TEST-018]] (plan handle `DSK-08-18`) runs this suite there. There is no measurement suggesting a central run is better, and "it may scale later" is not an answer. |

Two "yes" answers, neither of which places anything in Azure: unattended execution lands on the CI
runner this repository already uses, and central enforcement lands on human review of the fork
repository. Nothing in this ticket needs an Azure resource, and § 3 of the area plan records that
this whole area requires no ⚠ Azure write.

## Implications

1. **Build on `AssessmentReportRendererTests.cs`, do not replace it.** Its assertions are the
   already-reviewed definition of a correct report. The parity helper's job is to turn that
   implicit definition into an explicit, named, reusable property set that *both* renderers are
   held to — which is the body's step 5 requirement that "two similar-but-different assertion sets
   would let a real difference through".
2. **`AssertArtifact`'s `EngineVersion` assertion must be parameterised, not copied.** It asserts
   `"Playwright"`; the desktop artifact will say WebView2. A shared helper that copied it would
   fail the desktop side for the one property that is *supposed* to differ. This is the single
   clearest place where a careless share creates a false failure.
3. **Comparison is over extracted properties, never over PDF bytes or hashes.** `A-07-15-3`, and
   the pinned-versus-evergreen Chromium asymmetry, both point the same way. The manifest must say
   "pixel equality is explicitly not the target" in as many words, because a later reader who does
   not know why will try to tighten it.
4. **Five fixtures, ten artifacts.** Four outcomes plus the density case, each yielding an
   assessment PDF and a fee note PDF — the body's step 2 list, and the exact set the existing suite
   already exercises. Adding a sixth case is a scope decision, not a convenience.
5. **The density case is the most valuable fixture and the most likely to differ.** 80-item lists
   flowed across ≥ 8 pages with 8 embedded images is where two Chromium builds diverge on
   pagination. Its page count must be an exact-equality check, because a page-count difference is
   never cosmetic.
6. **The suite must be able to fail.** Body step 11's deliberate negative test is the difference
   between a gate and a decoration, and it is an acceptance criterion in its own right.
7. **Two lanes, no third.** C-01 makes a third Windows lane a real recurring cost; the gateway
   captures belong in the existing `browser` filter and the desktop comparison in the desktop lane
   [[TEST-013]] establishes.
8. **This suite is the evidence for two downstream decisions** — ADR-0108's move from `proposed` to
   `accepted` ([[FEAT-038]], plan handle `DSK-07-12`) and the condition under which the gateway
   renderer may be switched off behind its flag ([[FEAT-042]], plan handle `DSK-07-16`). The
   results table is therefore a deliverable, not a by-product.

## Open questions

None that this ticket must not silently assume. The five assumptions above are each settled by a
check inside the plan's own steps rather than by asking anyone, and the one judgement the body
requires — who may approve a re-baseline — is answered by the repository's existing review rule
(`AGENTS.md` § Repository task workflow step 5) taken as the default and recorded in the manifest,
rather than opened as a question. No `open-questions` document is created: the body instructs none,
and nothing here is unsettled.
