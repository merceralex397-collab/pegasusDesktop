# Plan — FND-007: Author ADR-0108 (isolated WebView2 report rendering) as `proposed`

**Diff estimate: ~1 file, ~110 lines.**

`docs/engineering.md:201-207` § Plan sizing requires the estimate first, derived
from the `files` document. One new ADR and nothing else at this merge: no index
row (the accepted table has no status column, so a `proposed` ADR has no honest
row) and no code. The length is set by the measured house range for a
decision of this weight — ADR-0028 is 84 lines and ADR-0025 is 114, and ADR-0108
carries the same six sections plus the eight-row cloud-justification table and a
`## Reversal condition`, so ~110.

## Approach

Merge the decision `proposed`, with the parts that are settled written as
decisions and the one genuinely open part written as a named deferral. The
alternative — waiting for the Phase 7 spike and merging one `accepted` ADR — was
rejected on two grounds. First, plan 00 § 4 Target state makes ADR-0100…ADR-0110
part of the **Phase 0** governance exit gate and explicitly allows ADR-0108 to
stand `proposed` until the spike, which is why this ticket sits in `HZN-001`.
Second, and more practically, the constraint it reconciles is *already* live:
proposal `:60` forbids a WebView2 shell, and `:1715` permits an isolated use
only where an ADR exists. Until this file merges, any renderer work reads as a
breach of a locked constraint rather than as an exercise of a recorded
exception.

The second design choice is to write the off-screen host as a **recorded
parameter** rather than a decision. `docs/desktop/07-integrations/README.md:255`
says a collapsed WinUI `WebView2` control "may still initialise, but behaviour
must be proven", with `CoreWebView2Controller` on a hidden HWND as the fallback,
and instructs "keep the renderer behind `IAssessmentReportRenderer` so the host
can change". An ADR that guessed the host would be wrong on the day it merged
and would need a superseding ADR to fix — bodies are immutable
(`docs/adr/README.md:12-14`).

## Governing docs

`refs` is empty and `docs_todo: true` — confirm with `get_doc_gates FND-007`,
which for profile `feature` shows `leave-backlog: [governing-doc]` satisfied by
`docs_todo`, and `leave-preparing: [research, files, plan, checklist,
questions-resolved]`.

> **New ADR — this ticket authors it.** ADR-0108 (isolated, non-UI WebView2
> HTML→PDF report rendering on the desktop; gateway renderer retained until
> golden-file parity). It is co-claimed by [[FEAT-038]] (plan handle
> `DSK-07-12`), and both bodies state the same split, so this plan writes
> `authored by [[FND-007]] as `proposed`; accepted by [[FEAT-038]]'s
> frontmatter-only PR` rather than asserting a single owner across the whole
> life of the file. One filename,
> `docs/adr/0108-desktop-webview2-report-rendering.md`, and no other 0108 path
> anywhere on the board.
> This plan is written to **L-03** as recorded in `docs/desktop/README.md`
> § Locked decisions and to
> `docs/desktop/00-governance-and-workflow/README.md` § 3's ADR-0108 row; if the
> spike lands differently, the ADR receives the host choice by the route step 5
> describes and this plan is not revised for it — that is the point of merging
> `proposed`.

Because `refs` is empty, the authorities that bind today are these:

| Authority | Requirement | Met by |
| --- | --- | --- |
| Proposal § 2.1 (`:60`) | Not a WebView/WebView2 shell around the current application | Step 4 (why the constraint is not breached: never visible, never hosts Pegasus UI) |
| Proposal § 23.2 (`:1715`) | An isolated WebView2 use requires an ADR and must not host Pegasus UI | The whole ticket; stated in `## Decision` |
| Proposal § 23.2 (`:1701-1713`) | The release gate: no WebView renders the legacy application; no required workflow launches the legacy site | Step 5, written so a reviewer can check it |
| Proposal § 12.5 | Report rendering design | Steps 4–6 |
| `AGENTS.md:94-108` | The YAML frontmatter block, verbatim in shape | Step 3 |
| `AGENTS.md:109-110` | Heading set with Status first | Steps 4–7 |
| `AGENTS.md:84-90` | The reserved block ADR-0100–ADR-0110 is mandatory here; never "next free number" | Step 3 |
| `docs/adr/README.md:12-14` | Published bodies are immutable | The whole approach: everything the ADR must say is in it at first merge, except the two frontmatter fields step 10 changes |
| `docs/adr/README.md:18-19` | Three-cell accepted table with no status column | Step 8 (no row while `proposed`) |
| ADR-0025, ADR-0028 | Accepted; the integrated renderer runs in the Web Container App | Step 6's retention clause; ADR-0108 relates both and supersedes neither |
| L-03 | Rendering moves to an isolated non-UI WebView2 path; gateway renderer retained until golden-file parity | Steps 4–6 |
| L-02 | Parity evidence is produced on the local Test/UAT stack, not in an Azure environment | Step 6 (the gate names [[FEAT-041]], not an environment) |
| FRD-11 | Report finality, correction and approval stay centrally governed | Step 5's split; `related_frd: [frd-11]` |
| `scripts/Test-MarkdownPlacement.ps1:31` + `.github/workflows/ci.yml:71-87` | Placement and link gates on every change set | Step 9 |

## Routing

Copied from the ticket body's `## Routing` block, as
`docs/desktop/00-governance-and-workflow/README.md` § "Ticket template" requires
of the plan document specifically.

- **Subagent**: `pegasus-desktop-reviewer` —
  `.codex/agents/pegasus-desktop-reviewer.toml` (verified present). Read-only; it
  owns the judgement that the renderer is not a web shell.
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `kanmer-docs`
  (`.grok/skills/kanmer-docs/SKILL.md`) → `microsoft-docs` (Microsoft Learn
  plugin).
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`,
  `set_ticket_doc`, `link_doc`, `append_scratch`, `move_item`); Microsoft Learn
  (`microsoft_docs_search`, `microsoft_docs_fetch`).
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` →
  `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout`. Call
  `get_doc_gates FND-007` before every move; a move crosses at most one gated
  boundary.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement
  (`AGENTS.md` § Repository task workflow step 5).

## Steps

These refine the body's eleven implementation steps; order, ownership and file
paths are the body's.

1. **Orient, then check for an existing file.** Read the plan row, § 3's
   ADR-0108 line, `docs/desktop/07-integrations/README.md` § 5 rows `DSK-07-12`
   to `DSK-07-15` (`:227-230`) and § 7 (`:255`), and the two existing renderer
   ADRs 0025 and 0028. Call `get_doc_gates FND-007`, then `take_ticket`. Then:
   ```
   ls docs/adr/0108*
   ```
   **Measured 2026-08-24: no such file.** If it exists, [[FEAT-038]] created it —
   edit that file, never create a second.
2. **Establish the API facts from Microsoft Learn rather than assuming them.**
   `microsoft_docs_search` for `CoreWebView2.PrintToPdfStreamAsync` and for
   `CoreWebView2Controller` hosting on a window handle; `microsoft_docs_fetch`
   the print how-to. The plan already identified both pages at
   `docs/desktop/07-integrations/README.md:112-114` (fetched 2026-08-23) —
   re-fetch, and record the URLs **with the fetch date** in the ADR's `## Links`.
3. **Create `docs/adr/0108-desktop-webview2-report-rendering.md`** with the
   `AGENTS.md:94-108` frontmatter block: `id: ADR-0108`, `status: proposed`,
   `date`, `supersedes: []`, `superseded_by: []`, `related_capabilities: []`,
   **`related_frd: [frd-11]`** — lowercase stem; the display form `[FRD-11]`
   appears nowhere in `docs/adr/*.md` and is a silent house-style break.
   Use the heading set from `docs/adr/0029-*.md:11-20`: `# ADR-0108: …`, then
   `## Status`, `## Context`, `## Decision`, `## Consequences`, plus the
   `## Reversal condition` of step 7 and `## Links`.
4. **`## Status` and `## Context`.** `## Status` opens "Proposed." and states
   plainly that a `proposed` ADR is **not settled authority**: until acceptance
   the gateway renderer remains the path in use and no other ticket may cite
   ADR-0108 as binding. Word the relation on ADR-0028's precedent
   (`docs/adr/0028-*.md:15-16`: "This decision refines ADR-0015 and ADR-0025; it
   supersedes neither"). `## Context` carries: the L-03 statement; why proposal
   § 2.1 (`:60`) is not breached — the control never hosts Pegasus UI and is
   never visible; the retained gateway renderer of ADR-0025/ADR-0028; and the
   six-question cloud-justification table from plan 00 § 3, **answered** for
   report rendering. The answers and their evidence are worked out in this
   ticket's `research` document under *Execution placement*: four honest **no**s,
   a **yes** on central enforcement limited to readiness and finality under
   FRD-11, and a **no** on measured operational advantage that cites the real
   measured costs — Chromium startup on first render, one render at a time
   behind a `SemaphoreSlim(1,1)`, and a Container App sized cpu 1.0 / 2 Gi to
   carry in-process Chromium. Every row gets a citation; no blank cells.
5. **`## Decision` — name what is decided now and what the spike still owes.**
   Decided: rendering moves to `Pegasus.Desktop.Infrastructure` behind
   `IAssessmentReportRenderer` (`src/Pegasus.Core/Reports/AssessmentReportRendering.cs`,
   312 lines) using the shared Scriban templates; the WebView2 instance is
   isolated, non-UI and single-flight; it never renders Pegasus UI and is never
   visible. Deferred and recorded here once [[FEAT-040]] (plan handle
   `DSK-07-14`) proves it: the off-screen host — a collapsed WinUI `WebView2`
   control versus `CoreWebView2Controller` on a hidden HWND. Write the deferral
   as a named parameter with its owner, not as an omission. Also state the split
   ADR-0028 already fixes: the desktop renders, but report readiness, accepted
   inputs, immutable identity and hash, correction and approval remain governed
   by FRD-11 and `Pegasus.Core`.
6. **`## Consequences` — the retention rule as a checkable gate.** The gateway
   renderer stays until the golden-file parity tests of [[FEAT-041]] (plan handle
   `DSK-07-15`) pass on approved fixtures, and after that no required report may
   depend on the web renderer unless ADR-0108 is amended by a superseding ADR.
   Phrase it so `kanmer-review` can check it against a diff, not as a sentiment.
7. **`## Reversal condition` — what would send rendering back to the gateway.**
   Name real conditions, each traceable to a measured question: the WebView2
   runtime absent or pinned below the required version on fleet workstations
   (Q6.3, `flow-records.md:423-425`, owned by [[FND-020]]); or a golden-file
   divergence that cannot be closed within documented tolerances (Q6.2). Neither
   is a failure of this ticket — an ADR that survives its own rejection is the
   one worth writing.
8. **Add no row to `docs/adr/README.md` at this merge.** The index has one
   accepted table (`:16`, header `:18-19`, `ADR | Title | Related FRD`) and **no
   status column**, so a row would assert ADR-0108 as current architecture.
   Ignore the `AGENTS.md:114-117` sentence describing a five-column index — the
   real file contradicts it at `:18-19` and **the file wins**; correcting that
   sentence is [[FND-005]]'s (plan handle `DSK-00-05`), not this ticket's. While
   `proposed`, the ADR is discoverable from
   `docs/desktop/00-governance-and-workflow/README.md` § 3's ADR set table and
   from this ticket — link it there, not in the index. **Write any forward
   reference to a document that does not exist yet as prose, never as a relative
   link**, or `scripts/Test-DocumentationLinks.ps1` fails the CI lane.
9. **Run the gates**, the same two the CI `documentation` job runs at
   `.github/workflows/ci.yml:84,87`:
   ```
   pwsh ./scripts/Test-DocumentationLinks.ps1
   pwsh ./scripts/Test-TestMarkdownPlacement.ps1
   ```
   Both exit 0. Open the PR against `dev` and merge with the independent review
   from `pegasus-desktop-reviewer`, whose specific judgement is that the
   described renderer is not a web shell under § 23.2.
10. **Acceptance, later, by another ticket.** After [[FEAT-040]] and [[FEAT-041]]
    are done, [[FEAT-038]] opens the **frontmatter-only** PR that sets
    `status: accepted`, sets the acceptance `date:`, fills in the recorded host
    choice and adds the `ADR | Title | Related FRD` row. **This ticket verifies
    that PR** against the reversal condition and the § 23.2 statement and closes
    on it; it performs **no edit to ADR-0108 after the first merge**.
11. **Proof** is the two PR references plus the golden-file test output that
    justified acceptance — cited from [[FEAT-041]], not re-run here.

## Verification

Evidence tier 1 — Static/build/architecture (`docs/engineering.md:72-90`) for
the document itself. The acceptance step rests on the **Tier 3** golden-file
evidence produced by [[FEAT-041]]; cite it, do not re-run it here.

| Command | Expected |
| --- | --- |
| `ls docs/adr/0108*` | exactly one file, `docs/adr/0108-desktop-webview2-report-rendering.md` |
| `grep -n '^status:' docs/adr/0108-*.md` | `proposed` at first merge; `accepted` after the spike |
| `grep -n '^related_frd:' docs/adr/0108-*.md` | `[frd-11]` — lowercase stem |
| `grep -n '0108' docs/adr/README.md` | **no row** while the file reads `status: proposed`; exactly one row in the `ADR \| Title \| Related FRD` table after acceptance |
| `grep -n 'PrintToPdf\|CoreWebView2Controller' docs/adr/0108-*.md` | each API claim accompanied by a Microsoft Learn URL and a fetch date |
| `pwsh ./scripts/Test-DocumentationLinks.ps1` | exits 0 |
| `pwsh ./scripts/Test-TestMarkdownPlacement.ps1` | exits 0 |
| `git diff --stat -- docs/adr/0025-*.md docs/adr/0028-*.md AGENTS.md` | empty |

Proof is written on merged `main`, after the merge.

## Risks / open questions

- **Merging an ADR that pretends the host is settled.** The specific defect this
  plan guards against: `docs/desktop/07-integrations/README.md:255` says the
  behaviour must be proven, and an immutable body cannot be corrected later.
  Mitigation: step 5 writes the host as a named deferred parameter with its
  owner.
- **Adding an index row while `proposed`.** Would assert ADR-0108 as current
  architecture in a table that has no status column to qualify it. Mitigation:
  step 8, and a proof grep that expects no row.
- **A relative link to a document that does not exist yet** (FRD-13, the desktop
  renderer files) fails `Test-DocumentationLinks.ps1`. Mitigation: step 8 says
  write forward references as prose.
- **Stale API claims.** `PrintToPdfStreamAsync` and `CoreWebView2Controller`
  hosting are the kind of claim that ages. Mitigation: step 2 re-fetches and
  records the date.
- **An isolated WebView2 that ever renders Pegasus UI breaches proposal § 2.1 and
  is a stop condition** — not a review comment. The reviewer subagent owns that
  judgement.
- **A `proposed` ADR cited as settled authority.** Mitigation: step 4 says so in
  `## Status`; [[FND-008]] (plan handle `DSK-00-08`) is told to mark it as
  `proposed` when FRD-13 cites it.
- **Scope boundaries owned by named tickets, not questions:** the off-screen host
  ([[FEAT-040]]); tolerances and fixtures ([[FEAT-041]]); the acceptance flip and
  the index row ([[FEAT-038]]); which templates are in desktop scope (Q6.1 /
  upstream TICK-206 — written `upstream TICK-206`, never bare, since a bare
  `TICK-<nnn>` reads as a fork board id); the `AGENTS.md` index sentence
  ([[FND-005]]); flow record 6's own contents ([[FND-020]], which also owns the
  six-versus-seven template count noted in `research`).
- **Not open, and not to be reopened:** L-03; L-02; the reserved ADR block
  (operator, 2026-08-23); and the ADR-0108 authorship split, which both this
  body and [[FEAT-038]]'s state identically.

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over
this branch's own diff before the PR, recorded here under a dated heading.
Expected outcome: `n/a — docs-only`._
