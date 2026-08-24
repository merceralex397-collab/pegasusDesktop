# Plan — FEAT-038: Author ADR-0108 — isolated WebView2 HTML→PDF rendering, never-UI rule, fallback and parity gate

**Diff estimate: ~2 files, ~140 lines** — case A below, which is what the tree shows today. If
[[FND-007]] (plan handle `DSK-00-07`) has already merged ADR-0108 as `status: proposed` by the time
this ticket runs, the diff is **~2 files, ~25 lines** (case B: the frontmatter-only acceptance flip,
the spike-evidence paragraphs, and the one index row).

## Measured file-and-line inventory

`chore` owes neither `research` nor `files`, so this plan carries the surface area itself. Every
number below was measured on 2026-08-24 at fork `main`, with the command that produced it.

| Path | Measured state today | Command | This ticket's change |
| --- | --- | --- | --- |
| `docs/adr/0108-desktop-webview2-report-rendering.md` | **does not exist** | `ls docs/adr/0108*` → no match | Create (case A) or edit (case B). ~135 lines, sized against the comparable ADRs below |
| `docs/adr/README.md` | 59 lines; **28** rows in one table `## Current architecture decisions (\`status: accepted\`)` with columns `ADR \| Title \| Related FRD`; **no status column** | `wc -l docs/adr/README.md` → 59; `grep -c '^\| \[' docs/adr/README.md` → 28 | **+1 row, and only at acceptance** |
| `docs/adr/` as a whole | 29 `.md` files = 28 ADRs plus `README.md`; every ADR on disk is in the accepted table | `ls docs/adr/*.md \| wc -l` → 29 | unchanged |
| `docs/index.md` | ADR listing | — | touched only if the listing requires it (body's Documentation changes) |

Size basis for the ~135 lines: `wc -l` gives `docs/adr/0025-…md` **114**,
`docs/adr/0019-…md` **97**, `docs/adr/0028-…md` **84**. ADR-0028's section set
(`## Status` `:13`, `## Context` `:18`, `## Decision` `:38`, `## Consequences` `:50`,
`## Options considered` `:67`, `## Links` `:77`) is the template to match; ADR-0025 uses the same
set without a separate `## Status` heading. ADR-0108 adds the six-row cloud-justification table
(8 lines with header), the never-UI rule, the concurrency consequence, the retained-fallback and
parity gate, and the reversal condition — hence a little above ADR-0025's 114.

## Approach

Write **one** ADR at the reserved number 0108, at the one agreed path, structured on ADR-0028's
section set, with every Context fact read out of the repository rather than remembered. The
rejected alternative was to write a fresh ADR here and let [[FND-007]] write its own Phase 0
version — rejected because two ADRs at one ID is the stop condition the ticket's Guardrails name,
and because `AGENTS.md` § ADR conventions (`AGENTS.md:77-82`) makes IDs stable and append-only:
the fix for a wrong ADR is a superseding ADR, never a second file at the same number. The agreed
division is therefore the one the body records — [[FND-007]] authors and merges ADR-0108 as
`status: proposed` in Phase 0; this ticket supplies the Phase 7 content and spike evidence and
performs the frontmatter-only acceptance flip, adding the index row in that same PR. Step 2 of the
implementation records which case actually applies.

## Governing docs

The ticket's `refs` is **empty** and it carries **`docs_todo: true`**.

> **New ADR** — ADR-0108 (isolated, non-UI WebView2 HTML→PDF report rendering; gateway renderer
> retained until golden-file parity), which **this ticket writes**. ADR-0108 has two claimants —
> [[FND-007]] (plan handle `DSK-00-07`) and this ticket; authored by [[FND-007]], see
> [[FND-007]]'s plan for the ownership reconciliation, which this ticket's Guardrails restate as
> "[[FND-007]] merges it `proposed`, FEAT-038 flips it `accepted`".
> This plan is written to the decision as recorded in
> `docs/desktop/00-governance-and-workflow/README.md` § 3 (the ADR-0108 row: "Report rendering in
> the desktop through an isolated, non-UI WebView2 HTML→PDF path; gateway renderer retained until
> golden-file parity … Relates ADR-0025, ADR-0028") and in `docs/desktop/README.md` § Locked
> decisions (L-03); if the ADR lands differently this plan is revised before implementation.

Because `refs` is empty, the programme-level authorities that bind today, with the step that
satisfies each:

| Authority | Requirement | Met by |
| --- | --- | --- |
| L-03 (index § Locked decisions) | Rendering moves to an isolated non-UI WebView2 path; the gateway renderer is retained until golden-file parity passes | Steps 6, 9, 12 |
| Proposal § 23.2 | An isolated WebView2 for a specific document render is permitted **only** when an ADR records it and it never hosts Pegasus UI | Step 7 |
| Proposal § 12.5 | Documents, PDFs and reports; the deviation toward native rendering that L-03 overrode | Step 4's Context and the `## Options considered` section |
| Proposal § 4 / 00 § 3 | The six-question cloud-justification table used **verbatim**, one row per question with evidence | Step 6 |
| 00 § 3 "Deviation: reserved ADR block" | ADR-0100…ADR-0110, operator-confirmed 2026-08-23; never "next free number" | Step 3, and the `ls docs/adr/0108*` verification |
| `AGENTS.md` § ADR conventions (`:77-82`) | Stable IDs; never renumber, reuse or delete; supersede by a new ADR | Steps 3 and 10 |
| `docs/adr/README.md` header | The index's one table **is** the set with `status: accepted`; it has no status column | Step 11 — no row while `proposed` |
| ADR-0014 | Not superseded; Test/UAT stays local (L-02) | Stated in the ADR's Links section; nothing in ADR-0108 touches it |
| D-002 / D-003 (index) | The distribution path is in-house and touches no Azure resource | Nothing here places a responsibility in Azure; recorded in the cloud-justification table's evidence column |

## Routing

Copied from the ticket body's `## Routing` block.

- **Subagent**: `pegasus-desktop-reviewer` — `.codex/agents/pegasus-desktop-reviewer.toml`
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `kanmer-docs`
  (`.grok/skills/kanmer-docs/SKILL.md`) → `microsoft-docs` (Microsoft Learn plugin)
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `link_doc`,
  `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_fetch` on
  <https://learn.microsoft.com/microsoft-edge/webview2/how-to/print> and the `CoreWebView2` WinRT
  reference)
- **Kanmer pipeline** for profile `chore`: `kanmer-plan` → `kanmer-execute` → `kanmer-verify` →
  `kanmer-closeout`. Gates are `leave-preparing` (plan plus questions-resolved) and `enter-done`
  (proof plus questions-resolved); call `get_doc_gates <id>` before every move.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md`
  § Repository task workflow step 5)

## Steps

Refines the body's twelve steps in the same order.

1. **Orient and take.** Read the plan row (`docs/desktop/07-integrations/README.md` § 5,
   `DSK-07-12`), that area's § 3 deviation paragraph and its ADR-0108 row, `AGENTS.md`
   § ADR conventions (`:77` onward), `docs/desktop/00-governance-and-workflow/README.md` § 3
   (`:124-180`, containing the reserved-block paragraph, the ADR table and the verbatim
   cloud-justification table), and the Appendix A template. Call `get_doc_gates FEAT-038`, then
   `take_ticket` on branch `task/dsk-07-12-adr-0108`.
2. **Establish case A or case B first.** Run `ls docs/adr/0108*`. Today it returns **no match**
   (measured above), so case A applies and this ticket creates the file. If [[FND-007]] has landed
   it as `proposed`, case B applies and this ticket **edits that file**. Record the observed result
   and the case under a dated heading in this document. Creating a second ADR-0108 under any
   filename is a stop condition.
3. **Frontmatter.** Match the field set the existing ADRs use, read from
   `docs/adr/0028-run-integrated-renderer-in-web-container-app.md:1-10`: `id`, `status`, `date`,
   `supersedes`, `superseded_by`, `related_capabilities`, `related_frd`, `tags`. `id: ADR-0108`,
   `status: proposed`, `related_frd: [frd-11]`, `related_capabilities` naming the RPT/EXT rows the
   renderer carries, `supersedes: []` — ADR-0025 and ADR-0028 are **related**, not superseded.
4. **Write Context from measured evidence, not memory.** Every one of these was read on
   2026-08-24 and must be restated with its `path:line`:
   - `docs/design/assets/report-renderer/templates/` holds **six `.scriban` files**
     (`advert_evidence_pack`, `assessment_fee_note`, `assessment_report`, `expert_report`,
     `fee_note`, `market_valuation_evidence`) **plus `report.css`** — seven governed files
     (`ls docs/design/assets/report-renderer/templates/`). Where the plan set says "seven
     `.scriban` files" it names six; write the measured count and say which files.
   - `src/Pegasus.Infrastructure/Pegasus.Infrastructure.csproj:42-53` embeds **five** report assets:
     `assessment_report.scriban`, `assessment_fee_note.scriban`, `report.css`,
     `logo_no_margin.png` (linked to `Reports\Assets\brand\logo.png`) and
     `andy_patterson.png` — each with an explicit `LogicalName` under
     `Pegasus.Infrastructure.Reports.Assets.*`.
   - `docs/design/brand/signatures/` holds **all three** governed signatures
     (`andy_patterson.png`, `ed_mawdsley.png`, `neil_oreilly.png`).
   - `src/Pegasus.Infrastructure/Reports/PlaywrightAssessmentReportRenderer.cs` is **326 lines**;
     `:13` declares it as the `IAssessmentReportRenderer` implementation; `:19` is the
     `SemaphoreSlim(1, 1)` gate; `:120-127` is `page.PdfAsync` with `Format = "A4"`,
     `PrintBackground = true`, `DisplayHeaderFooter = true`, an empty header template and margins
     top `8mm`, right `12mm`, bottom `22mm`, left `12mm`; `:133` is
     `PdfReader.Open(…, PdfDocumentOpenMode.Import)`; `:309-314` is `ResourceStream` composing
     `Pegasus.Infrastructure.Reports.Assets.{suffix}`.
   - `src/Pegasus.Infrastructure/DependencyInjection.cs:446-453` — `AddPegasusReportRendering`
     registers the renderer as a **singleton** plus `GenerateAssessmentReportDraft`,
     `IAssessmentReportProjectionSource` and `GenerateCaseAssessmentReportDraft`.
   - `src/Pegasus.Core/Reports/AssessmentReportRendering.cs:272-312` —
     `RenderedReportArtifact(SuggestedFileName, Pdf, PageCount, Sha256, TemplateVersion,
     EngineVersion)` at `:272`, `IAssessmentReportRenderer` at `:284`, and
     `GenerateAssessmentReportDraft` at `:291` re-hashing both artifacts and throwing
     `ReportRenderRejectedException` on mismatch at `:305`.
   - `src/Pegasus.Web/Pegasus.Web.csproj` `ContainerBaseImage` is the Playwright image, and the
     container is sized cpu 1.0 / 2Gi for in-process Chromium
     (`infra/modules/platform.bicep:354-478`, per the area plan's § 2).
5. **Fetch the WebView2 printing documentation and record it with the fetch date.** Use
   `microsoft_docs_fetch` on <https://learn.microsoft.com/microsoft-edge/webview2/how-to/print> and
   the `CoreWebView2` WinRT reference. Record: `PrintToPdfAsync` and `PrintToPdfStreamAsync` exist
   on `CoreWebView2`; `PrintToPdfStreamAsync` returns a **rewound** PDF stream;
   `CoreWebView2PrintSettings` covers margins, page size, backgrounds, header/footer and scale; and
   **one print operation per WebView at a time** is supported. Do not restate the area plan's
   2026-08-23 fetch as if it were this ticket's own — fetch again and date it.
6. **Answer the six-question table verbatim.** Copy the table from
   `docs/desktop/00-governance-and-workflow/README.md` § 3 (`:170-178`) — the six questions in
   that order, one row each, with evidence. For interactive report rendering all six are **no**,
   with canonical storage remaining central through the gateway
   ([[FEAT-042]], plan handle `DSK-07-16`) — which is what puts rendering on the desktop. State the
   evidence per row rather than a bare "no": no shared authority because a draft render is one
   operator's view of one case; no unattended execution because rendering is operator-initiated
   (the *storage* is not, and it stays central); no protected credential because the templates are
   governed repository assets and no provider secret is involved; no public callback; no central
   enforcement because readiness and finality stay Core-owned and server-enforced regardless of
   where the bytes are produced; no measured operational advantage because the measurement
   ([[FEAT-041]], plan handle `DSK-07-15`) is the parity gate rather than a placement argument.
7. **State the never-UI rule as a testable constraint.** The WebView2 instance is off-screen,
   renders only a locally composed report document, is never navigated to a Pegasus URL, and never
   hosts application UI. Name the enforcement: [[FND-037]] (plan handle `DSK-02-12`)'s
   `DependencyDirectionTests` no-WebView rule, extended with the single approved exception, and
   `winui-code-review`'s `WUI4xxx` interop rules for an uninitialised WebView2. A promise is not a
   constraint; the ADR names the test.
8. **Record the concurrency consequence.** Renders are serialised with the same
   `SemaphoreSlim(1, 1)` discipline the gateway renderer already uses
   (`PlaywrightAssessmentReportRenderer.cs:19`), because the documentation permits only one print
   operation per WebView at a time.
9. **Record the retained fallback and the parity gate.** `AddPegasusReportRendering`
   (`DependencyInjection.cs:446`) keeps registering the gateway renderer, and the desktop path is
   not the only path until [[FEAT-041]]'s golden-file fixtures pass. State the flag or composition
   switch that selects between them and **who may flip it** — the ADR records the authority, the
   flag name is settled by [[FEAT-040]] (plan handle `DSK-07-14`) step 10 and [[FEAT-042]] step 9;
   cite whichever has landed and name the ticket if neither has.
10. **Record the reversal condition explicitly.** What evidence would make this decision wrong —
    for example golden-file drift that documented tolerances cannot absorb after a WebView2 runtime
    update, or a workstation where the runtime cannot be present — and what happens then: the
    gateway renderer resumes and a **superseding** ADR is written. ADR bodies are immutable.
11. **Do not touch `docs/adr/README.md` while the file reads `status: proposed`.** The index's one
    table is `## Current architecture decisions (\`status: accepted\`)` with columns
    `ADR | Title | Related FRD` and **no status column** (measured: 28 rows, 59 lines), so a row
    there would assert ADR-0108 as current architecture. The row is added only in the
    frontmatter-only acceptance PR after [[FEAT-041]]'s golden-file parity passes. While it is
    `proposed`, ADR-0108 is discoverable from
    `docs/desktop/00-governance-and-workflow/README.md` § 3 and from this ticket. The relation
    notes to ADR-0025 and ADR-0028 go in ADR-0108's own `## Links` section — do **not** edit those
    two ADRs' bodies.
12. **Verify, link and open the PR.** Run `pwsh ./scripts/Test-DocumentationLinks.ps1` and
    `pwsh ./scripts/Test-MarkdownPlacement.ps1` (both present in `scripts/`); both must pass. Link
    the ADR to this ticket with Kanmer `link_doc`, then open the PR into `dev`. Record in the ADR's
    Status section that acceptance from `proposed` to `accepted` happens after [[FEAT-041]] passes.

## Verification

Evidence tier from the body: **Tier 1 — Static/build/architecture**
(`docs/engineering.md` § Required evidence tiers item 1: consistency only). `proof` is the captured
output of:

- `pwsh ./scripts/Test-DocumentationLinks.ps1` — expected exit 0, no broken link.
- `pwsh ./scripts/Test-MarkdownPlacement.ps1` — expected exit 0. (Any `.md` outside
  `docs/(prd|frd|adr|design|desktop)` fails the CI `documentation` job.)
- `ls docs/adr/0108*` — expected **exactly one** file,
  `docs/adr/0108-desktop-webview2-report-rendering.md`.
- `grep -c '^| \[' docs/adr/README.md` — expected **28** while the ADR reads `proposed`, and
  **29** after the acceptance PR. This is the observable form of body step 11.
- `get_doc_gates FEAT-038` after `link_doc` — expected: the governing-doc requirement satisfied by
  the new ADR.

Tier 1 proves nothing about the renderer itself; that is [[FEAT-040]] and [[FEAT-041]].

## Risks / open questions

- **Risk — two ADR-0108 files.** Mitigation: step 2 runs `ls docs/adr/0108*` before writing
  anything and records the case; the verification asserts exactly one file.
- **Risk — the index row is added too early.** Mitigation: the measured row count (28) is asserted
  in verification while the file reads `proposed`.
- **Risk — a "next free number" ADR.** Mitigation: the reserved block ADR-0100…ADR-0110 is
  operator-confirmed (00 § 3) and `docs/adr/README.md` is re-checked after every upstream sync
  ([[FND-023]], plan handle `DSK-01-10`), because upstream keeps issuing ADRs.
- **Risk — the plan set's "seven `.scriban` files" is repeated into the ADR.** Mitigation: step 4
  writes the measured count (six `.scriban` plus `report.css`) with the `ls` that produced it.
- **Scope boundary, not an open question** — the off-screen host choice (collapsed WinUI
  `WebView2` control versus `CoreWebView2Controller` on a hidden HWND) is decided by [[FEAT-040]]
  (plan handle `DSK-07-14`) step 2 and recorded back into this ADR's consequences by that ticket.
  This ticket does not choose it.
- **Scope boundary, not an open question** — the parity evidence that flips acceptance is
  [[FEAT-041]] (plan handle `DSK-07-15`)'s results table.
- **No open question is opened.** The body instructs none, L-03 and the reserved block are
  operator-decided, and nothing here is unsettled.

## Simplification pass

_`n/a — docs-only`._
