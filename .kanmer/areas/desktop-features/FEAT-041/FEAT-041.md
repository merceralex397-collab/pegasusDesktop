---
id: FEAT-041
type: ticket
title: >-
  DSK-07-15 · Golden-file parity suite: gateway-renderer fixtures compared with
  WebView2 output within documented tolerances
status: preparing
area: desktop-features
assignee: ''
profile: feature
stageEntered:
  preparing: '2026-08-24T21:31:45.187Z'
labels:
  - desktop-conversion
  - plan-07
  - phase-7
  - tier-3
groups:
  - EPIC-008
  - HZN-008
links: []
blocks:
  - TEST-018
  - FEAT-038
refs:
  - docs/frd/frd-11-reports-correspondence-and-reviewed-proposals.md
docs_todo: true
archived: false
created: '2026-08-24T08:30:09.649Z'
updated: '2026-08-24T23:49:47.922Z'
---

## What

Build the fixture catalogue and comparison suite that decides whether the desktop renderer may replace the gateway renderer: approved fixtures captured from the Playwright renderer, compared with WebView2 output on text, values, page count and key element positions within documented tolerances — not pixel equality.

## Why

Proposal § 12.5 requires deterministic tests comparing key text, values and layout against approved fixtures, and the Phase 7 exit gate is "approved fixtures match expected values/content". L-03 keeps the gateway renderer until this suite passes, so this ticket is the gate itself — ADR-0108 moves from `proposed` to `accepted` on its evidence. This area's § 7 also records why the tolerance design matters: the WebView2 runtime updates itself while Playwright is pinned to `1.61.0`, so a pixel-equality suite would fail on a Chromium update that changed nothing an operator can see. Siblings: [[DSK-07-14]] produces the output under test, [[DSK-08-18]] runs this suite as a CI lane on the Test/UAT stack.

## Source of truth

- Plan row: `docs/desktop/07-integrations/README.md` § 5 — `DSK-07-15`
- Plan context: `docs/desktop/07-integrations/README.md` § 4 Exit gates (Phase 7 row), § 7 Risks and traps ("Golden-file drift between Chromium builds")
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 12.5 Documents, PDFs and reports, § 22.2 Test pyramid, § 23.1 Required conversion evidence
- Repository evidence: `tests/Pegasus.IntegrationTests/Reports/AssessmentReportRendererTests.cs` — the existing baseline: a `[Theory]` over the four `AssessmentReportOutcome` values with `[Trait("Category", "Browser")]`, text assertions extracted with `UglyToad.PdfPig` (`PdfText`), the `PEGASUS_RENDER_EVIDENCE` environment variable that writes the rendered PDFs to a directory, and the flow/density test at `:64`; `tests/Pegasus.IntegrationTests/Reports/AssessmentReportDraftWebTests.cs`; `src/Pegasus.Core/Reports/AssessmentReportRendering.cs:272-282` (`RenderedReportArtifact` carries `PageCount`, `Sha256`, `TemplateVersion`, `EngineVersion`); `src/Pegasus.Infrastructure/Pegasus.Infrastructure.csproj` (`PdfPig` 0.1.15, `PDFsharp` 6.2.4, `Microsoft.Playwright` pinned by `$(PlaywrightVersion)`); `.github/workflows/ci.yml:207-234` (the `browser` lane, its Playwright Chromium cache and its `Category=Browser&Category!=Corpus` filter)
- Binding decisions: **L-03 / ADR-0108** — the gateway renderer is retained *until golden-file parity passes*; this suite is that gate. L-02 — the comparison runs on the local Test/UAT stack. C-01 — private-repository Windows runner minutes bill at 2×, so the suite must reuse the existing browser lane rather than adding a third renderer lane.
- Depends on: `DSK-07-14` the desktop renderer whose output is compared

## Routing

- **Subagent**: `pegasus-test-engineer` — `.codex/agents/pegasus-test-engineer.toml`
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `code-testing-agent` (dotnet/skills `98f84851`, plugin `dotnet-test`) → `run-tests` → `assertion-quality` → `test-gap-analysis`
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search` only if a PDF API question arises)
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (call `get_doc_gates <id>` before every move; a move crosses at most one gated boundary)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Orient: read the plan row, the Phase 7 exit-gate row, the Chromium-drift trap, and `tests/Pegasus.IntegrationTests/Reports/AssessmentReportRendererTests.cs` in full. Call `get_doc_gates <this ticket id>`, then `take_ticket` on branch `task/dsk-07-15-golden-file-parity`.
2. Define the fixture catalogue in `plan` before capturing anything. Start from the cases the existing suite already exercises — the four `AssessmentReportOutcome` values (`TotalLoss`, `Repairable`, `CashInLieu`, `ContractRepair`) and the long-list/multi-photo density case at `:64` — and add the fee note for each. Record what each fixture is *for*; a fixture nobody can explain is a fixture nobody will maintain.
3. Capture the baseline from the **gateway** renderer, not from a new implementation: run the existing browser tests with `PEGASUS_RENDER_EVIDENCE` set to a capture directory and collect the PDFs, then record for each the Playwright version, the template version and the capture date in a manifest file beside the fixtures.
4. Decide and document the tolerances explicitly in the fixture manifest: **text** — every asserted string present, extracted with `PdfPig`; **values** — every money and date token identical; **page count** — exactly equal; **key element positions** — named anchors (report title, settlement value, statement of truth, signature, fee total) within a stated absolute tolerance in points. Pixel equality is explicitly **not** the target; write that sentence into the manifest.
5. Add the comparison harness to `tests/Pegasus.IntegrationTests/Reports/`, reusing `PdfPig` for text and word positions and PDFsharp for page count. Extract a shared assertion helper so the gateway and desktop suites assert the *same* properties — two similar-but-different assertion sets would let a real difference through.
6. Add the desktop-side suite that renders the same snapshots through [[DSK-07-14]]'s renderer and compares against the fixtures with the same helper. Keep it in the test project that can host WinAppSDK dependencies; if that means the desktop test project rather than `Pegasus.IntegrationTests`, put it there and share the helper through a small test-support type rather than copying it.
7. Make a failure diagnosable: on mismatch, write both PDFs and a text diff to the test output directory and name the fixture, the anchor and the measured delta in the assertion message. An assertion that says only "expected true" costs an hour per failure.
8. Add a drift-review procedure to the manifest: when a fixture fails after a WebView2 runtime update, the fixture is **reviewed**, not silently re-baselined. Record who may approve a re-baseline and require the new capture's runtime version to be written into the manifest.
9. Wire the suite into the existing lanes rather than adding one: the gateway captures stay in the `browser` lane filter (`Category=Browser&Category!=Corpus`, `.github/workflows/ci.yml:230-234`), and the desktop comparison runs in the desktop test lane [[DSK-08-13]] establishes. [[DSK-08-18]] owns running both together on the Test/UAT stack.
10. Record the sign-off artefact: a single results table listing every fixture, its four tolerance checks and pass/fail. That table is the evidence ADR-0108 needs to move from `proposed` to `accepted`, and the condition [[DSK-07-16]] needs before the gateway renderer may be switched off behind its flag.
11. Run a deliberate negative test: alter one template value in a scratch copy, confirm the suite fails and names the fixture and the differing token, then revert. A parity suite that cannot fail is not a gate.
12. Run the simplification pass over the branch diff, record it under a dated `## Simplification pass` heading in the plan document, then open the PR into `dev`.

## Acceptance criteria

- [ ] A fixture catalogue exists covering the four assessment outcomes, the density case and the fee note, each with a recorded purpose.
- [ ] Baseline fixtures were captured from the gateway renderer with the Playwright version, template version and date recorded.
- [ ] Tolerances for text, values, page count and named anchor positions are documented; pixel equality is explicitly excluded.
- [ ] Both renderers are asserted with the same shared helper.
- [ ] A failure names the fixture, the anchor and the measured delta, and writes both PDFs for inspection.
- [ ] A re-baseline requires a recorded review and the new runtime version; it is never silent.
- [ ] The suite demonstrably fails when an input changes (negative test recorded).

## Verification

- [ ] `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --filter "Category=Browser&Category!=Corpus" -- xUnit.MaxParallelThreads=2` — expected: the gateway baseline captures pass.
- [ ] `dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release --filter "FullyQualifiedName~ReportParity"` — expected: every fixture passes all four tolerance checks.
- [ ] Results table attached to the ticket proof — expected: one row per fixture, all four checks green, sufficient for ADR-0108 acceptance.

## Evidence tier

Tier 3 — Parser/adapter contracts.
Tier 3 obliges deterministic adapter evidence: stable outputs, resource and cancellation behaviour, and integrity — here proven as fixture-level determinism across two rendering engines rather than a single green run.

## Documentation changes

- `docs/adr/0108-desktop-webview2-report-rendering.md` — Verification section cites this suite; status moves to `accepted` on its evidence (edited via [[DSK-07-12]]'s file)
- `docs/desktop/01-inventory-and-parity/parity-matrix.md` — the report rows reach `automated verification passed`

## Guardrails

- **Azure**: no write.
- **Scope boundary**: may touch `tests/Pegasus.IntegrationTests/Reports/`, the desktop test project and the fixture directory. Must not change either renderer to make a fixture pass — a failing fixture is either a renderer defect ([[DSK-07-14]]) or a reviewed tolerance change, never a quiet edit.
- **Traps**: the WebView2 runtime updates itself while Playwright is pinned, so tolerant comparison is the design, not a compromise; a silent re-baseline destroys the gate; fixtures are large binaries — keep the catalogue small enough to review and never commit corpus material (`docs/engineering.md` tier 8 keeps detailed corpus evidence local and ignored); C-01 means reusing existing CI lanes rather than adding one.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.

## Dependency correction — 2026-08-25

The results table produced here is the formal acceptance evidence for [[FEAT-038]]. [[FND-007]] owns the merged proposed ADR and [[FEAT-040]] provides packaged-controller evidence. This ticket does not edit ADR-0108's body, frontmatter, or index.

## Outcome

_Filled at closeout._
