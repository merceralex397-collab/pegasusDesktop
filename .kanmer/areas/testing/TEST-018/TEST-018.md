---
id: TEST-018
type: ticket
title: >-
  DSK-08-18 · Golden-file report parity lane: run area 07's fixtures on the
  stack and compare WebView2 output with the gateway renderer
status: backlog
area: testing
assignee: ''
profile: feature
labels:
  - desktop-conversion
  - plan-08
  - phase-7
  - tier-8
groups:
  - EPIC-009
  - HZN-008
links: []
refs:
  - docs/frd/frd-11-reports-correspondence-and-reviewed-proposals.md
docs_todo: true
archived: false
created: '2026-08-24T07:57:18.345Z'
updated: '2026-08-24T07:57:18.345Z'
---

## What

Stand up the lane that executes area 07's golden-file fixtures on the Test/UAT stack: for each fixture, render through the desktop's isolated WebView2 HTML→PDF path and through the retained gateway Playwright renderer, compare text, values, page count and key element positions within tolerance, and report every difference as explained or unresolved.

## Why

Locked decision L-03 moves report rendering to the desktop and keeps the gateway renderer **only until golden-file parity passes**; ADR-0108 records the parity lane as its verification. Proposal §24 Phase 7 makes it an exit gate: approved fixtures must match expected values and content, and no required report may depend on the web renderer unless explicitly retained. Without a lane that runs both renderers over the same fixtures on the same data, "the PDF looks right" is the only evidence for retiring a renderer that produces the firm's outward-facing documents. Executes the fixtures authored by [[DSK-07-15]] against the renderer of [[DSK-07-14]].

## Source of truth

- Plan row: `docs/desktop/08-testing/README.md` § 5 — `DSK-08-18`
- Plan detail: `docs/desktop/08-testing/README.md` § 3 (L-03: golden-file parity tests are a desktop-side test concern owned by 07 and executed in the lanes defined here) and `docs/desktop/08-testing/test-uat-stack.md` § Components (gateway-side rendering by in-process Playwright Chromium; desktop-side rendering by the WebView2 runtime)
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 12.5 documents, PDFs and reports; § 23.2 native verification; § 24 Phase 7 exit gate
- Repository evidence:
  - `src/Pegasus.Infrastructure/Reports/PlaywrightAssessmentReportRenderer.cs` — the retained gateway renderer that produces the comparison side
  - `tests/Pegasus.IntegrationTests/Reports/AssessmentReportRendererTests.cs` and `AssessmentReportDraftWebTests.cs` — the existing renderer tests whose fixtures and helpers this lane reuses rather than duplicates
  - `Directory.Build.props:14-20` — `PlaywrightVersion` 1.61.0 is the single source of truth for the pinned Playwright package and the Web `ContainerBaseImage`; the two must not desynchronise (ADR-0028, DELIV-012)
  - `docs/desktop/07-integrations/README.md` § 5 rows `DSK-07-13` (templates embedded once, hash-checked), `DSK-07-14` (desktop renderer), `DSK-07-15` (golden-file parity suite)
- Binding decisions:
  - L-03 — reports render locally through an isolated non-UI WebView2 path; the gateway renderer is retained until this lane passes, and ADR-0108 records this lane as its verification.
  - L-02 — the lane runs on the local stack; no Azure resource is involved.
- Depends on: `DSK-07-14` — the desktop WebView2 renderer. `DSK-07-15` — the fixtures and the comparison rules. `DSK-08-17` — the stack the lane runs on.

## Routing

- **Subagent**: `pegasus-test-engineer` — `.codex/agents/pegasus-test-engineer.toml`
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `run-tests` (`dotnet/skills` `98f84851`, plugin `dotnet-test`)
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search`) for `CoreWebView2.PrintToPdfStreamAsync` behaviour where a difference needs explaining
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (call `get_doc_gates <id>` before every move; a move crosses at most one gated boundary)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Read `docs/desktop/08-testing/README.md` § 5 row `DSK-08-18` and § 3, `docs/desktop/07-integrations/README.md` § 5 rows `DSK-07-13`–`DSK-07-16`, and the existing `tests/Pegasus.IntegrationTests/Reports/*.cs`. Call `get_doc_gates` on this ticket id, then `take_ticket`, and work in the ticket's own worktree and branch.
2. Confirm the ownership split with [[DSK-07-15]] before writing code: area 07 owns the fixtures and the comparison rules (what counts as a difference and what tolerance applies); this ticket owns the lane that runs them, files the artefacts and fails the build. Record the split in the ticket research document.
3. Load `pegasus-desktop`, then `run-tests`. Add `eng/reports/Invoke-ReportParity.ps1` taking `-FixtureSet` (default all), `-OutputPath` (default `artifacts/report-parity/`) and `-Tolerance`. For each fixture it drives the gateway renderer through the running stack and the desktop renderer through the installed package, and writes both PDFs side by side.
4. Implement the text comparison: extract text from both PDFs and compare normalised content — whitespace collapsed, line breaks normalised — reporting the first differing line with its context. A text difference is never a tolerance case; it either matches or it is a finding.
5. Implement the values comparison: extract the fixture's declared value fields (totals, dates, references) from both outputs and compare literally. This is the material business comparison, so it is a literal check against the fixture's expected values as well as against the other renderer — a green test written from the same mistaken interpretation as the implementation proves only self-consistency (`docs/engineering.md` § Evidence).
6. Implement the page-count and layout comparison: page count must be equal; key element positions are compared within the tolerance area 07 declared, and every position difference inside tolerance is still listed in the report so a drift is visible before it crosses the threshold.
7. Make the lane produce one report per run: fixture, text result, values result, page count, position deltas, and a verdict of `match`, `explained` (with the recorded explanation) or `unresolved`. Any `unresolved` fails the lane.
8. Add an explanations file listing accepted, deliberate differences with a reason and a date — a font substitution or a renderer-specific hyphenation, for instance. An explanation with no reason is not accepted, and the lane fails on an explanation whose fixture has since changed.
9. Wire the lane into the desktop lanes of [[DSK-08-13]] as a job that runs when the report templates, the renderer or the fixtures change, and upload the two PDFs plus the report as artifacts on failure so a reviewer can look at the actual documents.
10. Run the lane over the full fixture set on the Test/UAT stack. Done when every fixture is `match` or `explained` with a recorded reason, and the report is filed as ticket proof.
11. Record in ADR-0108's verification section ([[DSK-00-07]]) that this lane is the gate for retiring the gateway renderer, and state explicitly that the gateway renderer stays in place until the lane is green over the full set — do not remove `PlaywrightAssessmentReportRenderer` or its tests in this ticket.
12. Run the simplification pass over the branch diff and record it under a dated `## Simplification pass` heading in the plan document before opening the PR.

## Acceptance criteria

- [ ] Every area 07 fixture runs through both renderers on the stack and produces a comparison record.
- [ ] Text and value differences fail the lane; position differences are compared within the declared tolerance and always listed.
- [ ] Accepted differences are explained with a reason and a date, and an explanation expires when its fixture changes.
- [ ] The lane fails on any `unresolved` result and uploads both PDFs for inspection.
- [ ] The gateway renderer and its existing tests are untouched.

## Verification

- [ ] `pwsh ./eng/reports/Invoke-ReportParity.ps1 -FixtureSet All` — expected: exit 0, a report under `artifacts/report-parity/` with every fixture `match` or `explained`.
- [ ] `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --no-build --filter "Category!=Corpus&Category!=Browser"` — expected: the existing renderer tests still green.
- [ ] Alter one template value deliberately and rerun — expected: the lane fails naming the fixture and the differing value; revert and confirm green.

## Evidence tier

Tier 8 — Genuine corpus. The parity fixtures must be approved, reviewed report material handled under the repository's corpus rules: detailed evidence stays local and ignored under `artifacts/`, `corpus/` is never copied into the stack or the repository, and no domain document is fabricated to fill a fixture.

## Documentation changes

- `docs/adr/0108-*.md` — verification section: this lane is the gate for retiring the gateway renderer (authored by [[DSK-00-07]]).
- `docs/desktop/08-testing/README.md` § 4 — mark the golden-file parity lane as existing.
- `docs/operations.md` — note the parity report as release-candidate evidence.

## Guardrails

- **Azure**: no write.
- **Scope boundary**: may create `eng/reports/**` and add one CI job. Must not modify `src/Pegasus.Infrastructure/Reports/PlaywrightAssessmentReportRenderer.cs`, the report templates, or the fixtures — those belong to area 07 — and must not remove the gateway renderer or its tests.
- **Traps**: `PlaywrightVersion` in `Directory.Build.props` is the single source of truth for the package and the Web container base image; do not bump one without the other (ADR-0028, DELIV-012). A test written from the same interpretation as the implementation proves only self-consistency — the value comparison must also check the fixture's independently stated expected values. Never fabricate domain documents; `corpus/` is never used. `TreatWarningsAsErrors=true` applies to any test project touched.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.

## Outcome

_Filled at closeout._
