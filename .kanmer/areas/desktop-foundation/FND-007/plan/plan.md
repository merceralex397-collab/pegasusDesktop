# Plan — FND-007: Author ADR-0108 (isolated WebView2 report rendering) as `proposed`

## 2026-08-25 correction — documented invisible host

Microsoft Learn documents `HWND_MESSAGE` as the valid parent for an invisible `CoreWebView2Controller` on Windows 8 and later; the WebView will never become visible. The fixed design is `CoreWebView2Environment.CreateCoreWebView2ControllerAsync(HWND_MESSAGE)`. This supersedes every earlier collapsed-XAML/hidden-HWND host-selection instruction below. Phase 7 validates packaged-app initialisation, PDF output and no-window behaviour; it does not select a host. This user-directed correction also adds `docs/desktop/00-governance-and-workflow/README.md` and `docs/desktop/07-integrations/README.md` to FND-007's docs-only scope.


**Diff estimate: ~1 file, ~130 lines.**

Derived from the `files` document, not asserted. One new ADR and nothing else —
no index row at this merge, no code, no edit to any existing file. The 130 lines
are budgeted against the measured house lengths: ADR-0028 is 84 lines and
ADR-0025 is the fullest recent comparator; ADR-0108 carries everything ADR-0028
does **plus** the ten-line six-question table, two verbatim proposal quotations,
a named blank for the off-screen host, and a `## Reversal condition` section the
house form does not usually have. `git diff --stat` on the branch should show
`1 file changed, ~130 insertions(+)` and **zero deletions** — a deletion means
something was edited that should not have been.

## Approach

Write ADR-0108 once, merge it `proposed`, and never touch it again from this
ticket. The whole design of the document is aimed at one property: **the
acceptance flip must be a frontmatter-and-named-blank change, not a body
rewrite.** `docs/adr/README.md:12-14` makes a published body immutable, so if the
Decision section is written as though the off-screen host were already known,
[[FEAT-040]]'s (plan handle `DSK-07-14`) spike result cannot be recorded without
a whole superseding ADR. The body therefore states what is decided now — port,
project, templates, isolation, single-flight, fallback, retention gate — and
carries an explicitly named blank for the host, which [[FEAT-038]] (plan handle
`DSK-07-12`) fills at acceptance.

The rejected alternative was waiting for the Phase 7 spike and writing one
`accepted` ADR. It is tidier and it is wrong twice over: plan 00 § 4 makes
ADR-0100…ADR-0110 part of the **Phase 0** governance exit gate and explicitly
allows ADR-0108 to stand `proposed` until the spike, so waiting would hold that
gate open; and, more importantly, the first desktop renderer commit would then
land with no recorded exception to proposal § 2.1's "no WebView shell"
(`…Design_Proposal.md:1351`), which is precisely the reading § 23.2 (`:1715`)
requires an ADR to prevent.

The second rejected alternative was adding an index row now and marking it
"proposed" in the Title cell. `docs/adr/README.md:11-12` says the current
architecture **is** the accepted table; there is no status column
(`:18` — `| ADR | Title | Related FRD |`), so any row there is a claim of
currency. The row waits.

## Governing docs

`refs` is empty and `docs_todo: true` — confirmed by `get_doc_gates FND-007`,
whose `leave-backlog` requirement `governing-doc` reads `satisfied: true` on the
strength of `docs_todo`. No repository ADR governs this work today.

> **New ADR** — ADR-0108 (report rendering in the desktop through an isolated,
> non-UI WebView2 HTML→PDF path; the gateway renderer retained until golden-file
> parity), **authored by this ticket** and merged `proposed`. It has no
> co-author: [[FEAT-038]] (plan handle `DSK-07-12`) performs only the later
> frontmatter-only acceptance flip and the index row, and this ticket verifies
> that PR rather than editing the ADR again.
> This plan is written to **L-03** as recorded in `docs/desktop/README.md`
> § Locked decisions ("Report rendering moves to the desktop through an isolated,
> non-UI WebView2 HTML→PDF path; the gateway renderer is retained only until
> golden-file parity passes; needs ADR-0108") and to
> `docs/desktop/00-governance-and-workflow/README.md` § 3's ADR set table row for
> ADR-0108. If the Phase 7 spike lands differently the ADR is accepted with the
> host it actually proves — which is why the blank is named rather than guessed.

Because `refs` is empty, the authorities that bind today are these:

| Authority | Requirement | Met by |
| --- | --- | --- |
| Proposal § 23.2 (`:1715`) | An isolated WebView2 "requires an ADR and must not host Pegasus UI" | Steps 3–4 (the ADR itself, and the never-UI clause) |
| Proposal § 2.1 (`:1351`) | "no WebView shell" — the locked constraint being excepted | Step 4 (quoted, with the argument that the exception does not swallow it) |
| Proposal § 12.5 (`:751`) | Documents, PDFs and reports — the rendering design | Step 5 |
| L-03 (`docs/desktop/README.md` § Locked decisions) | Isolated non-UI WebView2 HTML→PDF; gateway renderer retained until golden-file parity | Steps 4–6 |
| L-02 | Parity evidence is produced on the local Test/UAT stack, not in an Azure environment | Step 6 (the gate names [[FEAT-041]] and [[TEST-018]], not a cloud run) |
| Plan 00 § 4 Target state and exit gate | ADR-0100…ADR-0110 are the Phase 0 governance exit gate; "ADR-0108 may be `proposed` until the Phase 7 spike" | The whole ticket, and why it is `HZN-001` |
| `AGENTS.md:81-89` | Stable IDs; the operator-confirmed reserved block ADR-0100–ADR-0110 | Step 3 |
| `AGENTS.md:94-108` | The eight-key YAML frontmatter block | Step 3 |
| `AGENTS.md:107-110` | Heading set with **Status first**, "so a body-only read is never mistaken for current when it is superseded" | Step 3 |
| `docs/adr/README.md:11-14` | The current architecture **is** the accepted table; published bodies are immutable | Steps 5 and 8, and the whole Approach |
| `docs/adr/README.md:18-19` | Three-column accepted table, no status column | Step 8 |
| ADR-0028 `:57-60` | A move to another renderer host requires measured evidence and a **new accepted ADR** | Steps 6–7 (this ADR plus the parity gate are that evidence route) |
| ADR-0028 `:33-36` | FRD-11 report behaviour stays governed by FRD-11 and `Pegasus.Core` | Step 5 (the desktop produces bytes; the gateway registers reports) |
| ADR-0025 `:30-36` | Templates are product behaviour and must co-version with the FRDs and Core policy | Step 5 (the same governed template source, embedded by [[FEAT-039]]) |
| `docs/desktop/07-integrations/README.md:255` | Record the chosen off-screen host **in ADR-0108**; keep the renderer behind `IAssessmentReportRenderer` so the host can change | Step 5's named blank |
| `docs/desktop/07-integrations/README.md:257` | Runtime missing or outdated → named failure and gateway fallback | Step 5 |
| `scripts/Test-MarkdownPlacement.ps1:31` + `.github/workflows/ci.yml:70-87` | New Markdown only under the allowed roots; the `documentation` job runs on every change set | Step 9 |

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

These refine the body's eleven implementation steps; the order, the ownership
and the file paths are the body's. Measured values were read on 2026-08-24.

1. **Orient.** Read the plan row, § 3's ADR-0108 line,
   `docs/desktop/07-integrations/README.md:227-231` (rows `DSK-07-12` to
   `DSK-07-17`) and `:251-259` (§ 7 Risks and traps), then the two existing
   renderer ADRs `docs/adr/0025-…` and `docs/adr/0028-…`. Call
   `get_doc_gates FND-007` — expect `leave-backlog: [governing-doc]` satisfied by
   `docs_todo` and `leave-preparing: [research, files, plan, checklist,
   questions-resolved]` — then `take_ticket`.
   Run `ls docs/adr/010*` and record the output. Measured 2026-08-24:
   `No such file or directory`. ADR-0108 has no co-author, so unlike
   [[FND-005]] and [[FND-006]] there is no extend-in-place branch here — but run
   the check anyway, because a file appearing means someone else wrote this ADR.
2. **Establish the API facts before asserting them.** `microsoft_docs_search`
   for `CoreWebView2.PrintToPdfStreamAsync`, then again for
   `CoreWebView2Controller` hosting on a window handle; `microsoft_docs_fetch`
   the print how-to for the detail. `docs/desktop/07-integrations/README.md:112-115`
   already names the two starting URLs — the WebView2 print how-to and the
   `CoreWebView2` WinRT reference — but they are a starting point, not a
   substitute for fetching. **Record the fetched URLs and the fetch date in the
   ADR's `## Links`**: the body is immutable, so this is the only place a later
   reader can check whether the named API still exists.
3. **Create `docs/adr/0108-desktop-webview2-report-rendering.md`** with the
   `AGENTS.md:94-108` frontmatter block:
   ```yaml
   ---
   id: ADR-0108
   status: proposed
   date: <the date of this merge>
   supersedes: []
   superseded_by: []
   related_capabilities: []
   related_frd: [FRD-11]
   tags: []
   ---
   ```
   `supersedes: []` and `superseded_by: []` stay empty — ADR-0108 **relates to**
   ADR-0025 and ADR-0028 and supersedes neither, exactly as ADR-0028 itself
   relates to ADR-0015 and ADR-0025 (`docs/adr/0028-…:14-16`).
   > **Flag for the reviewer at the point of writing.** The ticket body
   > prescribes `related_frd: [FRD-11]`, and the body is settled, so that is what
   > this plan carries. But the measured house form across all 28 existing ADRs
   > is the **lowercase file stem** — `grep -h '^related_frd:' docs/adr/*.md`
   > returns only `[]` and values like `[frd-11]`, `[frd-10, frd-11]`, and
   > `grep -l '^related_frd: \[FRD' docs/adr/*.md` returns **no file**
   > (2026-08-24). Raise the one-token discrepancy with
   > `pegasus-desktop-reviewer` in the PR and take their answer; do not silently
   > switch it, and do not treat it as a blocker.
   Use the heading set `## Status · ## Context · ## Decision · ## Consequences ·
   ## Options considered · ## Links` with **Status first**
   (`AGENTS.md:107-110`), following the newest house form at
   `docs/adr/0029-…:11-20` rather than `docs/adr/0015-…`, which has no
   `## Status` section at all. In `## Status`, say `proposed` and say what would
   change it — the pattern ADR-0028 uses at `:13-16`.
4. **Write `## Context`** with four things, in this order:
   (a) the L-03 statement;
   (b) **verbatim quotations** of proposal § 23.2 `:1715` ("An isolated WebView2
   use for a third-party login consent page or a specific document preview is not
   automatically a web wrapper, but it requires an ADR and must not host Pegasus
   UI.") and § 2.1 `:1351` ("no WebView shell"), with the argument that the
   exception does not swallow the rule — the control never hosts Pegasus UI and
   is never visible;
   (c) the retained gateway renderer of ADR-0025 and ADR-0028, and what central
   rendering currently costs the Web image (`docs/adr/0028-…:22-27`: a pinned
   Chromium build, matching native Linux dependencies, fonts and writable
   temporary space);
   (d) the six-question cloud-justification table from plan 00 § 3, **every cell
   filled**, answered for report rendering. The worked answers and their evidence
   are in this ticket's `research` document under *Execution placement*; the
   shape of the conclusion is one "yes" — central enforcement, landing **on the
   gateway**, because FRD-11's readiness, identity, hash, correction, approval and
   finality rules stay `Pegasus.Core` policy (`docs/adr/0028-…:33-36`). Note
   honestly that "measured operational advantage" is **no, and not yet measured**;
   claiming a measurement that has not been taken is the failure this row exists
   to catch.
5. **Write `## Decision`** so it separates what is decided now from what the
   spike still owes.
   Decided now: rendering moves to `Pegasus.Desktop.Infrastructure` behind the
   **existing** `IAssessmentReportRenderer` port
   (`src/Pegasus.Core/Reports/AssessmentReportRendering.cs:284`, consumed by
   `GenerateAssessmentReportDraft` at `:291`) — a second implementation of an
   existing port, not a new port; it uses the same governed Scriban templates
   (`docs/design/assets/report-renderer/templates/`, seven files), embedded
   hash-checked by [[FEAT-039]] (plan handle `DSK-07-13`) rather than copied; the
   WebView2 instance is **isolated, non-UI, never visible and single-flight**; a
   missing or outdated WebView2 runtime produces a **named failure and falls back
   to the gateway renderer** (`docs/desktop/07-integrations/README.md:257`); and
   the desktop **produces the document bytes but never registers the report** —
   registration, custody and audit stay behind the gateway ([[FEAT-042]], plan
   handle `DSK-07-16`).
   Left open, as a **named blank**: the off-screen host — a collapsed WinUI
   `WebView2` control versus `CoreWebView2Controller` on a hidden HWND — "recorded
   here once [[FEAT-040]] proves it". Write that blank as a labelled sentence a
   later editor can fill without touching anything around it.
6. **Write `## Consequences`** with the retention rule stated as a gate with an
   owner, not a sentiment: the gateway renderer
   (`src/Pegasus.Infrastructure/Reports/PlaywrightAssessmentReportRenderer.cs:13`,
   registered at `src/Pegasus.Infrastructure/DependencyInjection.cs:448`) stays
   until [[FEAT-041]]'s (plan handle `DSK-07-15`) golden-file parity tests pass on
   approved fixtures, and after that **no required report may depend on the web
   renderer unless a superseding ADR says so**. Add the two consequences the
   evidence forces: the parity comparison is **tolerant, not pixel-equal**,
   because WebView2's Chromium self-updates while Playwright is pinned to 1.61.0
   (`docs/desktop/07-integrations/README.md:258`); and acceptance of this decision
   proves architecture only — it does not prove the renderer is implemented,
   installed, parity-passing or accepted (the disclaimer ADR-0028 uses at `:55`).
7. **Write `## Reversal condition`** — what evidence sends rendering back to the
   gateway. Write it now, before the evidence exists, because that is the only
   time it can be written honestly. At minimum: WebView2 runtime absent or
   unmaintainable across the workstation fleet (recorded as an *assumption* at
   `docs/desktop/07-integrations/README.md:125`, not a fact); a golden-file
   divergence that cannot be closed within documented tolerance; or neither
   off-screen host initialising without a visible window, which would put L-03
   itself back to the operator.
8. **Add no row to `docs/adr/README.md` at this merge.** The index has one
   accepted table (`:16`, header `:18`) and **no status column**, and `:11-12`
   states the current architecture *is* that table — a row would assert ADR-0108
   as current architecture. Ignore `AGENTS.md:114-116`, which describes a
   five-column index the file does not have; **the file wins**, and correcting
   that sentence is [[FND-005]]'s (plan handle `DSK-00-05`), not this ticket's.
   While it is `proposed`, ADR-0108 is discoverable from
   `docs/desktop/00-governance-and-workflow/README.md` § 3's ADR set table and
   from this ticket — link it there, not in the index.
9. **Run the gates**, the same two the CI `documentation` job runs at
   `.github/workflows/ci.yml:82-87`:
   ```
   pwsh ./scripts/Test-DocumentationLinks.ps1
   pwsh ./scripts/Test-TestMarkdownPlacement.ps1
   ```
   Both exit 0. `Test-DocumentationLinks.ps1` takes **no** parameters
   (`:8-9`) and **strips fenced and inline code before scanning** (`:4-7`) — so
   put the `## Links` entries outside fences, or the gate checks nothing. It also
   ignores external URLs entirely, which is why step 2's fetch date matters: the
   Microsoft Learn links are never verified by CI. Then open the PR against `dev`
   (`gh pr create --base dev`) and merge with the independent review from
   `pegasus-desktop-reviewer`, whose specific job here is to agree that the
   renderer is not a web shell.
10. **After [[FEAT-040]] and [[FEAT-041]] are done, [[FEAT-038]] opens the
    frontmatter-only acceptance PR** — `status: accepted`, the acceptance `date:`,
    the recorded host choice filled into the named blank, and the
    `ADR | Title | Related FRD` row added to `docs/adr/README.md`. **This ticket
    verifies that PR** against the reversal condition and the § 23.2 statement and
    closes on it; **it performs no edit to ADR-0108 after the first merge.** If
    [[FEAT-038]] is descoped, this ticket has an unowned successor — raise it
    rather than editing the ADR here.
11. **Record the proof** as the two PR references plus the golden-file test
    output that justified acceptance — cited from [[FEAT-041]], never re-run here.

## Verification

Evidence tier **1 — Static/build/architecture** for the document itself, as the
body states. The acceptance step rests on the **tier 3** golden-file evidence
produced by [[FEAT-041]] — cite it, do not re-run it.

| Check | Expected |
| --- | --- |
| `ls docs/adr/010*` — run **before** writing, output recorded | `No such file or directory` (2026-08-24); after, exactly one ADR-0108 file |
| `grep -n '^status:' docs/adr/0108-*.md` | `proposed` at first merge; `accepted` only after [[FEAT-038]]'s flip |
| `grep -n '^supersedes:\|^superseded_by:' docs/adr/0108-*.md` | both `[]` |
| `grep -n '0108' docs/adr/README.md` | **no match** while the file reads `status: proposed`; exactly one row in the `ADR \| Title \| Related FRD` table after acceptance |
| `grep -n '^## ' docs/adr/0108-*.md` | `## Status`, `## Context`, `## Decision`, `## Consequences`, `## Reversal condition`, `## Options considered`, `## Links` — Status first |
| `grep -c 'must not host Pegasus UI' docs/adr/0108-*.md` | at least 1 — the § 23.2 sentence is quoted, not paraphrased |
| `grep -n 'learn.microsoft.com' docs/adr/0108-*.md` | the fetched URLs, each with the fetch date, **outside** any fenced block |
| `git diff --stat` on the branch | `1 file changed`, ~130 insertions, **0 deletions** — a deletion means an existing file was edited |
| `git diff --stat -- docs/adr/0025-*.md docs/adr/0028-*.md docs/adr/README.md AGENTS.md` | **empty** |
| `pwsh ./scripts/Test-DocumentationLinks.ps1` | exits 0 |
| `pwsh ./scripts/Test-TestMarkdownPlacement.ps1` | exits 0 |

Proof is written on merged `main`, after review and merge — never before
(`AGENTS.md` § Kanmer operating instructions).

## Risks / open questions

- **Writing the host choice instead of a named blank.** The one failure that
  cannot be repaired: `docs/adr/README.md:12-14` makes the body immutable, so a
  Decision section written as though the host were known forces a superseding ADR
  when [[FEAT-040]]'s spike answers differently. Mitigation: step 5 makes the
  blank an explicitly labelled sentence, and the whole Approach is built around
  it.
- **Adding an index row "for discoverability".** It would assert a `proposed`
  decision as current architecture (`docs/adr/README.md:11-12`), and
  `AGENTS.md:115` actively encourages the mistake by describing a status column
  that does not exist. Mitigation: step 8, and a verification line that asserts
  **no** ADR-0108 match in the index.
- **Naming a Microsoft API that is renamed or removed.** An immutable body ages
  with it, and CI never checks external URLs
  (`scripts/Test-DocumentationLinks.ps1:1-3`). Mitigation: step 2 fetches and
  records the URL **and the date**, so a later reader knows what was true when.
- **`related_frd: [FRD-11]` versus the measured `[frd-11]`.** A one-token
  discrepancy between the settled ticket body and the form all 28 existing ADRs
  use. Mitigation: step 3 follows the body and flags it to the reviewer in the
  PR. It blocks nothing.
- **A reviewer reading the WebView2 as a shell.** Mitigation: the § 23.2
  quotation, the never-visible and never-hosts-Pegasus-UI clauses, and routing
  the review to `pegasus-desktop-reviewer`, whose named job this is. An isolated
  WebView2 that ever renders Pegasus UI is a **stop condition**, not a review
  comment.
- **Scope boundaries owned by named tickets, not open questions:** the off-screen
  host spike ([[FEAT-040]]); the acceptance flip and index row ([[FEAT-038]]);
  the golden-file fixtures ([[FEAT-041]]); the shared-template embedding
  ([[FEAT-039]]); the finalise endpoint that keeps report registration on the
  gateway ([[FEAT-042]]); the parity lane on the local stack ([[TEST-018]]); the
  `AGENTS.md` index sentence ([[FND-005]]). None of them gates this ticket's
  `leave-preparing`.
- **The successor could go unowned.** If [[FEAT-038]] is descoped, ADR-0108 stays
  `proposed` indefinitely and the report tickets never get settled authority.
  Mitigation: step 10 says raise it rather than editing the ADR here.

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over
this branch's own diff before the PR, recorded here under a dated heading.
Expected outcome for this ticket: `n/a — docs-only`._
