---
id: FEAT-038
type: ticket
title: >-
  DSK-07-12 · Author ADR-0108: isolated WebView2 HTML→PDF rendering, never-UI
  rule, fallback and parity gate
status: backlog
area: desktop-features
assignee: ''
profile: chore
labels:
  - desktop-conversion
  - plan-07
  - phase-7
  - tier-1
groups:
  - EPIC-008
  - HZN-008
links: []
blocks:
  - FEAT-018
  - FEAT-040
  - FEAT-043
docs_todo: true
archived: false
created: '2026-08-24T08:24:13.959Z'
updated: '2026-08-24T09:37:23.763Z'
---

## What

Write `docs/adr/0108-desktop-webview2-report-rendering.md`: the decision that report rendering moves to the desktop through an **isolated, non-UI** WebView2 HTML→PDF path, with the proposal § 23.2 exception stated explicitly, the gateway renderer retained until golden-file parity passes, and a named reversal condition.

## Why

Locked decision L-03 requires this ADR, and proposal § 23.2 permits an isolated WebView2 for a specific document render **only** when an ADR records it and it never hosts Pegasus UI. Without the accepted record, [[DSK-07-14]] has no authority to add a WebView2 dependency to a native client whose release gate says "no WebView renders the legacy Pegasus application". The ADR also has to explain the deviation this area records in § 3: proposal § 12.5 leaned toward native rendering, and the operator chose WebView2 because the Scriban templates, `report.css`, brand logo and signatures already exist and are governed under `docs/design/assets/report-renderer/`. Sibling: [[DSK-00-07]] claims the same ADR — it authors and merges the file as `status: proposed` in Phase 0, and this ticket owns the Phase 7 content, the spike evidence and the acceptance flip; do not write two ADRs.

## Source of truth

- Plan row: `docs/desktop/07-integrations/README.md` § 5 — `DSK-07-12`
- Plan context: `docs/desktop/07-integrations/README.md` § 3 (Deviation L-03 and the ADR-0108 row), § 7 Risks and traps (the WebView2 hosting and print-concurrency rows)
- ADR conventions and the reserved block: `docs/desktop/00-governance-and-workflow/README.md` § 3 (ADR-0100…ADR-0110 table, the cloud-justification table used verbatim); `AGENTS.md` § ADR conventions; template at `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` Appendix A
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 12.5 Documents, PDFs and reports, § 23.2 Native verification (the isolated-WebView2 exception sentence), § 4 cloud-justification test
- Repository evidence: `src/Pegasus.Infrastructure/Reports/PlaywrightAssessmentReportRenderer.cs:13` (the interface it implements and the `SemaphoreSlim(1,1)` gate at `:19`), `:100-130` (Scriban render then `page.PdfAsync` with A4, background printing and the 8/12/22/12 mm margins), `:130-142` (PDFsharp post-processing and the `RenderedReportArtifact` provenance), `:305-315` (embedded-resource naming `Pegasus.Infrastructure.Reports.Assets.*`); `src/Pegasus.Infrastructure/DependencyInjection.cs:446-453` (`AddPegasusReportRendering`); `src/Pegasus.Core/Reports/AssessmentReportRendering.cs:272-310` (`RenderedReportArtifact`, `IAssessmentReportRenderer`, `GenerateAssessmentReportDraft` and its provenance check); `src/Pegasus.Web/Pegasus.Web.csproj` (`ContainerBaseImage` playwright image); `docs/adr/0025-integrate-renderer-and-extractor-into-the-application.md`, `docs/adr/0028-run-integrated-renderer-in-web-container-app.md`
- Binding decisions: **L-03** — rendering moves to an isolated non-UI WebView2 path; the gateway renderer is retained until golden-file parity passes. The reserved ADR block ADR-0100…ADR-0110 — never "next free number", because upstream keeps issuing ADRs and would collide.
- Depends on: `DSK-00-05` the reserved-block ADRs that establish the conversion decision set

## Routing

- **Subagent**: `pegasus-desktop-reviewer` — `.codex/agents/pegasus-desktop-reviewer.toml`
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `kanmer-docs` (`.grok/skills/kanmer-docs/SKILL.md`) → `microsoft-docs` (Microsoft Learn plugin)
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `link_doc`, `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_fetch` on <https://learn.microsoft.com/microsoft-edge/webview2/how-to/print> and the `CoreWebView2` WinRT reference)
- **Kanmer pipeline** for profile `chore`: `kanmer-plan` → `kanmer-execute` → `kanmer-verify` → `kanmer-closeout` (gates are `leave-preparing` — plan plus questions-resolved — and `enter-done` — proof plus questions-resolved; call `get_doc_gates <id>` before every move)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Orient: read the plan row, this area's § 3 deviation paragraph, `docs/desktop/00-governance-and-workflow/README.md` § 3, `AGENTS.md` § ADR conventions, and the Appendix A template. Call `get_doc_gates <this ticket id>`, then `take_ticket` on branch `task/dsk-07-12-adr-0108`.
2. Check for duplication first: if [[DSK-00-07]] has already created `docs/adr/0108-*.md` as `proposed`, this ticket **edits that file** rather than creating a second ADR. Record which case applies in `plan`.
3. Create or open `docs/adr/0108-desktop-webview2-report-rendering.md` with valid YAML frontmatter matching the conventions used by the existing ADRs (compare `docs/adr/0025-integrate-renderer-and-extractor-into-the-application.md` and `docs/adr/0028-run-integrated-renderer-in-web-container-app.md` for the exact field set). Status starts `proposed`.
4. Write the Context section from evidence, not memory: the seven Scriban templates and `report.css` under `docs/design/assets/report-renderer/templates/`, the three of them plus two brand assets currently embedded by `src/Pegasus.Infrastructure/Pegasus.Infrastructure.csproj`, the Playwright Chromium renderer's exact page setup (A4, `PrintBackground = true`, margins top 8 mm, right 12 mm, bottom 22 mm, left 12 mm), the `SemaphoreSlim(1,1)` serialisation, the PDFsharp post-processing and the container's cpu 1.0 / 2Gi sizing.
5. Fetch the WebView2 printing documentation with `microsoft_docs_fetch` and record, with the fetch date: that `PrintToPdfAsync` / `PrintToPdfStreamAsync` exist on `CoreWebView2`, that `PrintToPdfStreamAsync` returns a rewound PDF stream, that `CoreWebView2PrintSettings` covers margins, page size, backgrounds, header/footer and scale, and that **one print operation per WebView at a time** is supported.
6. Answer the six-question cloud-justification test verbatim in the table from `docs/desktop/00-governance-and-workflow/README.md` § 3, one row per question with evidence. For interactive report rendering all six are "no" — with canonical storage remaining central through the gateway — which is what puts rendering on the desktop.
7. State the **never-UI rule** as a testable constraint, not a promise: the WebView2 instance is off-screen, renders only a locally composed report document, is never navigated to a Pegasus URL, and never hosts application UI. Name the architecture test that enforces it ([[DSK-02-12]]'s no-WebView rule, extended with the single approved exception) and the `winui-code-review` `WUI4xxx` interop rules that catch an uninitialised WebView2.
8. Record the concurrency consequence: renders are serialised with the same `SemaphoreSlim(1,1)` discipline the Playwright renderer already uses, because the documentation permits only one print operation per WebView at a time.
9. Record the retained fallback and the parity gate: `AddPegasusReportRendering` keeps registering the gateway renderer, and the desktop path is not the only path until [[DSK-07-15]]'s golden-file fixtures pass. State the flag or composition switch that selects between them and who may flip it.
10. Record the reversal condition explicitly: what evidence would make this decision wrong (for example golden-file drift that tolerances cannot absorb after a WebView2 runtime update, or a workstation where the runtime cannot be present), and what happens then — the gateway renderer resumes and a superseding ADR is written. ADR bodies are immutable; supersession is by new ADR.
11. Do **not** add a row to `docs/adr/README.md` while the file reads `status: proposed`. That index has one accepted table (`## Current architecture decisions (`status: accepted`)`, columns `ADR | Title | Related FRD`) and no status column, so a row there would assert ADR-0108 as current architecture — the same rule [[DSK-00-07]] step 8 states. The row is added only when the ADR flips to `accepted` after [[DSK-07-15]]'s golden-file parity passes, in that same frontmatter-only PR. While it is `proposed` the ADR is discoverable from `docs/desktop/00-governance-and-workflow/README.md` § 3 and from this ticket. The relation notes to ADR-0025 and ADR-0028 go in ADR-0108's own body; do not edit those ADRs' bodies.
12. Run `pwsh ./scripts/Test-DocumentationLinks.ps1` and `pwsh ./scripts/Test-MarkdownPlacement.ps1`; both must pass. Link the ADR to this ticket with Kanmer `link_doc`, then open the PR into `dev`. Acceptance from `proposed` to `accepted` happens after [[DSK-07-15]] passes — record that condition in the ADR's Status section.

## Acceptance criteria

- [ ] `docs/adr/0108-desktop-webview2-report-rendering.md` exists with valid frontmatter and the reserved number 0108 — not a "next free" number.
- [ ] The proposal § 23.2 exception is stated: isolated, off-screen, one purpose, never hosts Pegasus UI.
- [ ] The six cloud-justification answers are present with evidence.
- [ ] The retained gateway renderer and the golden-file parity gate are recorded as the condition for acceptance.
- [ ] A named reversal condition exists, with what happens if it is met.
- [ ] Exactly one ADR-0108 exists in the repository, at `docs/adr/0108-desktop-webview2-report-rendering.md`; `docs/adr/README.md` carries no ADR-0108 row while the file reads `status: proposed`, and exactly one row in the `ADR | Title | Related FRD` table after acceptance.

## Verification

- [ ] `pwsh ./scripts/Test-DocumentationLinks.ps1` — expected: exit 0, no broken link.
- [ ] `pwsh ./scripts/Test-MarkdownPlacement.ps1` — expected: exit 0.
- [ ] `ls docs/adr/0108*` — expected: exactly one file, `docs/adr/0108-desktop-webview2-report-rendering.md`.
- [ ] `get_doc_gates <this ticket id>` after `link_doc` — expected: the governing-doc requirement is satisfied by the new ADR.

## Evidence tier

Tier 1 — Static/build/architecture.
Tier 1 obliges consistency evidence only: the document exists in the allowed root, links resolve, frontmatter is valid and the index row is present once the ADR reads `status: accepted`. It proves nothing about the renderer, which is [[DSK-07-14]] and [[DSK-07-15]].

## Documentation changes

- `docs/adr/0108-desktop-webview2-report-rendering.md` — new ADR, carrying the relation notes to ADR-0025 and ADR-0028
- `docs/adr/README.md` — one index row, added only when the ADR flips to `accepted`; nothing is written there while the file reads `status: proposed`
- `docs/index.md` — only if the ADR index listing requires it

## Guardrails

- **Azure**: no write.
- **Scope boundary**: documentation only — `docs/adr/` and `docs/adr/README.md`. No source file, no project reference, no package addition. Adding the WebView2 dependency is [[DSK-07-14]].
- **Co-claimant**: ADR-0108 is also claimed by [[DSK-00-07]]. One agreed path — `docs/adr/0108-desktop-webview2-report-rendering.md` — and no other 0108 filename anywhere on the board. The rule: [[DSK-00-07]] authors and merges ADR-0108 as `status: proposed` in Phase 0; this ticket supplies the spike evidence from [[DSK-07-14]]/[[DSK-07-15]] and performs the frontmatter-only acceptance flip, adding the index row in that same PR. Two authors on one ADR ID is a stop condition: if the file already exists, edit it.
- **Traps**: use the reserved block ADR-0100…ADR-0110 — taking the next free number collides with upstream, which keeps issuing ADRs; check `docs/adr/README.md` after every upstream sync; ADR bodies are immutable, so supersede rather than rewrite; ADR-0014 is **not** superseded (Test/UAT stays local, L-02); any `.md` outside `docs/(prd|frd|adr|design|desktop)` fails the CI `documentation` job; do not author a second ADR if [[DSK-00-07]] already created one.
- **Simplification pass** (`AGENTS.md` step 4): `n/a — docs-only`.

## Outcome

_Filled at closeout._
