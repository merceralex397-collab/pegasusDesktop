# Plan — FEAT-041: Golden-file parity suite — gateway-renderer fixtures compared with WebView2 output within documented tolerances

**Diff estimate: ~19 files, ~640 lines of text plus 10 binary fixtures.**

Derived from the files document: `ReportParityAssertions.cs` ~180 lines (four tolerance families,
each with a diagnosable failure message), `ReportFixtureCatalogue.cs` ~140 (five cases with purpose,
snapshot builder and expected token/anchor set), `ReportFixtureCaptureTests.cs` ~110,
`ReportParityTests.cs` in the desktop project ~130, `manifest.md` ~90, an additive ~8-line edit to
`AssessmentReportRendererTests.cs` (158 lines today), ~12 lines across the desktop `.csproj`
(a `PdfPig` reference plus one linked `<Compile>`), and ~25 across the two documentation files.
Ten fixture PDFs are binary and contribute files, not lines. No route, no DTO, no migration, no
Azure write, no new CI lane.

## Approach

Turn the assertions that `tests/Pegasus.IntegrationTests/Reports/AssessmentReportRendererTests.cs`
already makes about a correct report into **one** named, parameterised property set, capture the
gateway renderer's output as committed fixtures, and hold both renderers to that same set. The
rejected alternative was a byte- or image-level comparison of the two PDFs — visually obvious, and
wrong: `Directory.Build.props:17` pins Playwright to `1.61.0` while the WebView2 Evergreen runtime
updates itself, so an exact comparison fails on Chromium changes that no operator can see, which is
the trap `docs/desktop/07-integrations/README.md` § 7 records and mitigates with "tolerant
comparisons … not pixel equality". It is also *weaker* than it looks: a byte diff tells you two
files differ, never which value moved. The chosen shape trades an easy assertion for a diagnosable
one, and the shared-helper rule is what makes it a real gate — two similar-but-different assertion
sets would let a genuine difference through, which is the failure body step 5 names.

## Governing docs

`refs`: `docs/frd/frd-11-reports-correspondence-and-reviewed-proposals.md`.

| Ref | Requirement | Meets |
| --- | --- | --- |
| FRD-11 (reports, correspondence and reviewed proposals) | What a rendered report must contain, and that nothing may be substituted — the properties a parity suite must therefore hold constant across a change of rendering engine | Steps 2–4 define the catalogue and tolerances against FRD-11's required content; steps 5–6 assert both renderers against them with one helper. This ticket adds no report **behaviour**; correction and finality stay FRD-11's and [[FEAT-042]]'s (plan handle `DSK-07-16`) |

The ticket carries **`docs_todo: true`**, so no conversion ADR governs it yet:

> **New ADR** — ADR-0108 (isolated, non-UI WebView2 HTML→PDF rendering; gateway renderer retained
> until golden-file parity), authored by [[FND-007]] (plan handle `DSK-00-07`); ADR-0108 has two
> claimants, so see [[FND-007]]'s plan for the ownership reconciliation — [[FEAT-038]] (plan handle
> `DSK-07-12`) owns the Phase 7 content and the `proposed` → `accepted` flip, and **this ticket
> produces the evidence that flip depends on**.
> This plan is written to the decision as recorded in
> `docs/desktop/00-governance-and-workflow/README.md` § 3 (the ADR-0108 row) and in
> `docs/desktop/README.md` § Locked decisions (L-03); if the ADR lands differently this plan is
> revised before implementation.

`refs` carries no ADR, so the programme-level authorities that bind today, with the step that
satisfies each:

| Authority | Requirement | Met by |
| --- | --- | --- |
| L-03 (index § Locked decisions) | The gateway renderer is retained **until golden-file parity passes** — this suite is that gate | Steps 2–6, and step 10's results table |
| Proposal § 12.5 | Deterministic tests comparing key text, values and layout against **approved** fixtures | Steps 3–5; "approved" is what step 8's review procedure makes true |
| Proposal § 22.2 (test pyramid) | Adapter-level determinism sits below integrated workflow, not instead of it | Step 9's lane placement; [[TEST-018]] (plan handle `DSK-08-18`) owns the integrated run |
| Proposal § 23.1 (required conversion evidence) | Automated test result recorded per parity row | Step 10 and the parity-matrix edit |
| Area 07 § 4, Phase 7 exit gate | "Approved fixtures match expected values/content" | Step 10's results table is the gate artefact |
| Area 07 § 7, Chromium-drift trap | Tolerant comparison, fixture review on failure, **not** pixel equality | Steps 4 and 8; the manifest states it in as many words |
| L-02 (index § Locked decisions) | Evidence is produced on the local production-mimicking stack | Verification; [[TEST-018]] runs it there |
| C-01 (index § Constraints) | Private-repository Windows runner minutes bill at 2× | Step 9 — reuse the existing `browser` filter and the desktop lane; no third lane |
| `docs/engineering.md:72-88` tier 3 | Deterministic adapter evidence — an intermittent fixture fails this tier | Steps 4, 7 and 11 |
| `docs/engineering.md:201-207` § Plan sizing | Diff estimate first, facts split from assumptions | This plan's first line; `research` § Facts / § Assumptions |
| `AGENTS.md` § Repository task workflow step 5 | Review by an agent that did not implement | Step 8 adopts this as the re-baseline approver default |
| `AGENTS.md` § Simplicity rails, "One list per concept" | A property set lives in exactly one place | Steps 5 and 6 — one helper, two callers |
| `HZN-001` / `board-conventions.md` § Upstream ids versus board ids | Upstream ids are never written bare | Every cross-reference in these four documents |

## Routing

Copied from the ticket body's `## Routing` block.

- **Subagent**: `pegasus-test-engineer` — `.codex/agents/pegasus-test-engineer.toml`
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `code-testing-agent` (dotnet/skills
  `98f84851`, plugin `dotnet-test`) → `run-tests` → `assertion-quality` → `test-gap-analysis`
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`,
  `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search` only if a PDF API
  question arises)
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute`
  → `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (call `get_doc_gates <id>` before every
  move; a move crosses at most one gated boundary)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md`
  § Repository task workflow step 5)

## Steps

Refines the body's twelve steps in the same order, with the same ownership. Nothing is renumbered.

1. **Orient and take.** Read the plan row (`docs/desktop/07-integrations/README.md` § 5,
   `DSK-07-15`), that area's § 4 Phase 7 exit-gate row and its § 7 Chromium-drift trap row, and
   `tests/Pegasus.IntegrationTests/Reports/AssessmentReportRendererTests.cs` **end to end** (158
   lines — every assertion in it is a candidate tolerance). Call `get_doc_gates FEAT-041`, then
   `take_ticket` on branch `task/dsk-07-15-golden-file-parity`.
2. **Define the catalogue in this document before capturing anything**, under a dated heading. Five
   cases, each with the sentence that says what it is *for*:
   - `TotalLoss`, `Repairable`, `CashInLieu`, `ContractRepair` — the four
     `AssessmentReportOutcome` values the existing `[Theory]` already covers
     (`AssessmentReportRendererTests.cs:16-19`). Purpose: each selects a different report title
     (`:30-37`) and a different money/salvage shape (`Snapshot(...)` at `:145` gives `TotalLoss`
     alone a `SalvageCategory` of `"S"` and a `SalvageValue` of `500m`), so the four together prove
     outcome-dependent content and not just one happy path.
   - `Density` — the `CE-STRESS-DENSITY` case at `:64-98`: 80 new parts, 80 repairs, 80 operations
     and 8 photos. Purpose: this is the only case that exercises **pagination**, and pagination is
     where two Chromium builds actually diverge.
   Each case yields two artifacts — the assessment report and its fee note — so ten fixtures.
   A sixth case is a scope decision recorded in the manifest, not a convenience.
3. **Capture the baseline from the gateway renderer, never from a new implementation.** Add the six
   `PEGASUS_RENDER_EVIDENCE` lines from `:53-59` to the density test at `:62-98` (additive only —
   change no existing assertion), then run the browser filter with the variable pointed at a
   capture directory and collect the ten PDFs. Write `manifest.md` beside them recording per
   fixture: purpose, `PlaywrightVersion` read from `Directory.Build.props:17` (`1.61.0`),
   `AssessmentReportContract.TemplateVersion` read from
   `src/Pegasus.Core/Reports/AssessmentReportRendering.cs:8` (`"rendererref1-v1"`), the capture
   date, and the machine's Chromium build. Read those two versions from the source, do not type
   them.
4. **Decide and document the four tolerance families in the manifest, in these words.**
   - **Text** — every asserted string present, extracted with `PdfPig` through the `PdfText` shape
     at `:131-135`. The strings are the ones the existing suite already reviews: the outcome title
     (`:30-37`), `"Vehicle Images"`, `"Statement of Truth"`, `"Front bumper"`, the outcome phrase
     (`:38-41`), and for the fee note `"FEE NOTE"`, `"Subtotal (Net)"`, `"VAT @ 20%"`,
     `"TOTAL DUE"`, `"Lloyds Bank"`, `"30-12-80"`, `"50858868"` and
     `AssessmentReportContract.VatNumber` (`:43-51`). Plus the density case's `"Stress new part
     080"`, `"Stress repair 080"`, `"Stress operation 080"` and the per-page reference (`:89-92`).
   - **Values** — every money and date token **identical**, not merely present. `£` amounts and
     `dd/MM/yyyy` dates are extracted by regex from the page text and compared as ordered
     sequences. A rounding difference between engines is a defect, not a tolerance.
   - **Page count** — **exactly equal**, via PDFsharp `PdfReader.Open` as
     `PlaywrightAssessmentReportRenderer.cs:133` does. The density case additionally keeps the
     existing `>= 8` floor and the `>= 8` embedded-image count (`:88`, `:97`). A page-count
     difference is never cosmetic.
   - **Key element positions** — five named anchors, resolved with PdfPig `Page.GetWords()`
     bounding boxes: report title, settlement value, statement of truth, signature, fee total.
     **Settle assumption `A-07-15-1` first** by extracting the anchors from one captured fixture
     twice and asserting identical coordinates; only then write an absolute tolerance in PDF
     points into the manifest. If `GetWords()` geometry proves unusable, record the fallback in the
     manifest — text, values, page count and image count remain — rather than dropping the family
     silently.
   Write the sentence **"Pixel equality is explicitly not the target."** into the manifest verbatim.
   A later reader who does not know why will otherwise try to tighten it.
5. **Add the comparison harness under `tests/Pegasus.IntegrationTests/Reports/`.**
   `ReportParityAssertions.cs` holds the four families once; `ReportFixtureCatalogue.cs` holds the
   five cases and their expected sets; `ReportFixtureCaptureTests.cs` renders each case through
   `GenerateAssessmentReportDraft` and asserts it against its committed fixture. Carry
   `[Trait("Category", "Browser")]` on the capture tests — without it they never run in CI
   (`.github/workflows/ci.yml:232`). **Parameterise the engine name.** `AssertArtifact` at `:112-119`
   asserts `EngineVersion` contains `"Playwright"`; the shared helper takes the expected engine
   token as an argument so the desktop side can pass WebView2. Copying that line is the single most
   likely way to make every desktop fixture fail for the one property that is supposed to differ.
   Do **not** compare `Sha256` between renderers — the bytes legitimately differ, and Core already
   enforces each artifact's own provenance (`AssessmentReportRendering.cs:291-307`).
6. **Add the desktop-side suite in the project that can host WinAppSDK dependencies** —
   `tests/Pegasus.Desktop.ViewModelTests` ([[TEST-004]] (plan handle `DSK-08-04`), [[FND-038]] (plan
   handle `DSK-02-13`)). `ReportParityTests.cs` renders the same catalogue cases through
   [[FEAT-040]] (plan handle `DSK-07-14`)'s `WebView2AssessmentReportRenderer` and asserts against
   the same fixtures through the **same** helper — shared by a linked `<Compile>` item plus a
   `PdfPig` `PackageReference`, not by copying the file. That settles assumption `A-07-15-2`; if the
   link does not work, promote the helper to a small `tests/Pegasus.TestSupport` project rather than
   duplicating it. Name the tests so `FullyQualifiedName~ReportParity` selects them, which is the
   body's verification filter.
7. **Make a failure diagnosable.** On mismatch, write both PDFs and a text diff to the test output
   directory, and name in the assertion message: the fixture, the tolerance family, the anchor or
   token, and the **measured delta**. An assertion that says only "expected true" costs an hour per
   failure — this is a tier-3 requirement, not a nicety.
8. **Write the drift-review procedure into the manifest.** When a fixture fails after a WebView2
   runtime update it is **reviewed, never silently re-baselined**. The procedure: the failure is
   triaged as a renderer defect ([[FEAT-040]]) or a reviewed tolerance change; a re-baseline
   requires a pull request approved by an agent that did not capture the new fixture
   (`AGENTS.md` § Repository task workflow step 5 — **taken as the default here rather than opened
   as a question**), and the new capture's WebView2 runtime version and capture date are written
   into the manifest in the same commit. A re-baseline with no manifest change is the failure mode
   the Guardrails name.
9. **Wire into the existing lanes; add none.** Gateway captures run inside the existing `browser`
   filter (`Category=Browser&Category!=Corpus`, `.github/workflows/ci.yml:230-234`, capped at
   `xUnit.MaxParallelThreads=2` within `timeout-minutes: 25`); the desktop comparison runs in the
   desktop test lane [[TEST-013]] (plan handle `DSK-08-13`) establishes. Re-measure the browser
   lane's duration after the five capture cases land — renders are serialised by
   `SemaphoreSlim(1, 1)` (`PlaywrightAssessmentReportRenderer.cs:19`) — and if the 25-minute cap is
   at risk, report it to [[TEST-019]] (plan handle `DSK-08-19`) rather than opening a third lane;
   C-01 makes that a real recurring cost. [[TEST-018]] (plan handle `DSK-08-18`), which this ticket
   blocks, owns running both halves together on the Test/UAT stack.
10. **Produce the sign-off results table** — one row per fixture, four columns for the four
    tolerance families, pass/fail in each, plus the Playwright version, the WebView2 runtime version
    and the template version the run used. Attach it to the ticket proof. This table is the evidence
    [[FEAT-038]] needs for the ADR-0108 acceptance flip and the condition [[FEAT-042]] (plan handle
    `DSK-07-16`) needs before the gateway renderer may be switched off behind its flag. It has two
    named readers, so write it for them.
11. **Run the deliberate negative test.** In a scratch working copy, alter one template value —
    a fee-note money token is the cleanest — confirm the suite fails and that the message names the
    fixture, the family and the differing token, capture that output, then revert. **A parity suite
    that cannot fail is not a gate**, and this is an acceptance criterion in its own right. Verify
    with `git status --porcelain -- docs/design` that the revert is complete.
12. **Simplification pass and PR.** Run the pass over this branch's own diff, record it under a
    dated `## Simplification pass` heading below, and open the PR into `dev`.

## Verification

Evidence tier from the body: **Tier 3 — Parser/adapter contracts**
(`docs/engineering.md:72-88` item 3: corruption, expansion/resource limits, cancellation,
path/integrity safety, stable contract codes and **deterministic** external failures — here proven
as fixture-level determinism across two rendering engines rather than a single green run).
`proof` is the captured output of:

- `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --filter "Category=Browser&Category!=Corpus" -- xUnit.MaxParallelThreads=2`
  — expected: the five gateway capture-and-compare cases pass **and** the pre-existing
  `AssessmentReportRendererTests` facts stay green, which is the guard that the additive edit to
  the density test disturbed nothing.
- `dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release --filter "FullyQualifiedName~ReportParity"`
  — expected: every fixture passes all four tolerance families, named individually. The assertion
  names are the evidence, not the summary line.
- **The negative-test capture from step 11** — expected: a red run whose message names the fixture,
  the tolerance family and the differing token, followed by
  `git status --porcelain -- docs/design` returning **empty**.
- `git diff --exit-code openapi/pegasus-v1.json` — expected clean. This ticket adds no contract, so
  a diff here means something leaked out of scope.
- `git diff --stat origin/dev -- src/` — expected: **empty output**. The observable form of the
  Guardrail "must not change either renderer to make a fixture pass".
- **Results table** attached to the proof — one row per fixture, all four families green, plus the
  three version strings. Sufficient for ADR-0108 acceptance.

Behaviour to observe on the local stack (L-02): running the same capture twice produces fixtures
whose extracted property sets match while their SHA-256 values differ — the concrete demonstration
that assumption `A-07-15-3` holds and that a byte comparison would have been the wrong design.

## Risks / open questions

- **Risk — the shared helper copies `AssertArtifact`'s `"Playwright"` assertion**
  (`AssessmentReportRendererTests.cs:118`) and every desktop fixture fails for the one property
  meant to differ. Mitigation: step 5 parameterises the expected engine token, and this is called
  out in the `files` Context table against the file it lives in.
- **Risk — PdfPig word geometry is not stable enough to anchor on** (`A-07-15-1`). Mitigation:
  step 4 measures it twice before any tolerance number is written, and records the reduced family
  set in the manifest if it fails rather than quietly dropping the check.
- **Risk — the browser lane exceeds `timeout-minutes: 25`.** Five serialised renders join a lane
  already capped at `MaxParallelThreads=2` for good reason. Mitigation: step 9 re-measures and
  escalates to [[TEST-019]]; a third lane is a C-01 cost, not a free fix.
- **Risk — a silent re-baseline.** The gate's whole value dies the first time someone re-captures a
  fixture to turn a build green. Mitigation: step 8's recorded procedure plus the requirement that
  the manifest change lands in the same commit, so a re-baseline with no manifest diff is visible
  in review.
- **Risk — tolerances set from the first failure rather than from the design.** Mitigation: every
  tolerance is written into the manifest in step 4, *before* the desktop renderer's output is ever
  compared; widening one afterwards is a reviewed change with a recorded reason.
- **Risk — a CRLF checkout changes every fixture at once.** `.gitattributes:4-5` pin the `.scriban`
  and `.css` sources to LF. Mitigation: a whole-catalogue failure with no code change is diagnosed
  here first; the `files` Context table records it.
- **Risk — fixtures grow into a repository-size problem.** Mitigation: the catalogue is capped at
  five cases and any addition is recorded in the manifest with its purpose; corpus material is
  excluded by `.gitignore:1` and by Guardrail.
- **Scope boundary, not an open question** — the ADR-0108 acceptance flip and its index row are
  [[FEAT-038]]'s; the renderer under test is [[FEAT-040]]'s; the shared embedded templates are
  [[FEAT-039]]'s (plan handle `DSK-07-13`); the flag that switches the gateway renderer off is
  [[FEAT-042]]'s; the Test/UAT run is [[TEST-018]]'s; the desktop test project is [[TEST-004]] /
  [[FND-038]]'s; the CI cost decision is [[TEST-019]]'s.
- **A default was taken rather than a question opened** — the re-baseline approver is the
  repository's existing review rule (`AGENTS.md` § Repository task workflow step 5: an agent that
  did not implement), recorded in the manifest by step 8. If the operator wants a named human
  instead, that is a one-line manifest edit and does not block this ticket.
- **No open question is opened.** The body instructs none, every assumption is settled by a check
  inside these steps, and nothing here is unsettled.

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over this branch's own
diff before the PR, recorded here under a dated heading._
