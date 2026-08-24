# Plan — FEAT-043: Reconcile the eleven upstream report-decision tickets against L-03 and record each disposition

**Diff estimate: ~2 repository files, ~55 lines (about +44 / −11).**

Profile `chore` owes neither `research` nor `files`, so this plan carries the surface-area burden
itself. Every number below was measured on 2026-08-24 at fork `main`, branch
`task/desktop-plan-segmentation`, with the command that produced it. Kanmer board writes
(`update_item`, `create_item`, `set_ticket_doc`) do not appear in a git diff at all and are
verified by tool output instead — see Verification.

## Measured file-and-line inventory

| Path | Measured state today | Command | This ticket's change |
| --- | --- | --- | --- |
| `docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md` | **224 lines**. `## Upstream board shape` `:12`; `## Disposition categories` `:58`, whose `report-decision` row is `:64`; the recreation-rule paragraph `:67`–`:75`; `## Triage table (109 open upstream tickets)` `:77` with **109 data rows** at `:81`–`:189`; the disposition-totals paragraph `:191`–`:195`; `## Code drift and the first sync` `:197` | `wc -l` → 224; `grep -n '^## '` | **Eleven row edits only** — the disposition column and, where one exists, the fork ticket id. ≈ +11 / −11 |
| — the eleven `report-decision` rows | `:118` upstream DOCS-001, `:119` upstream DOCS-003, `:120` upstream DOCS-004, `:125` upstream TICK-081, `:126` upstream TICK-096, `:127` upstream TICK-097, `:128` upstream TICK-100, `:129` upstream TICK-206, `:130` upstream TICK-208, `:131` upstream TICK-214, `:132` upstream TICK-216 | `grep -n 'report-decision' <file>` | the eleven dispositions |
| — `report-decision` occurrence count | **14 total**: the eleven rows, the category row `:64`, the recreation-rule sentence `:68`, and the totals line `:192` | `grep -o 'report-decision' <file> \| wc -l` → 14; `grep -c '^\| [A-Z].*report-decision' <file>` → **11** | unchanged by this ticket except within the eleven rows |
| `docs/desktop/07-integrations/README.md` | **286 lines**; § 8 Documentation changes is the final section | `wc -l` → 286 | **+~30**: the seven-template scope table, upstream TICK-206's twelve-entry negative list, and the `Pegasus.Desktop.Infrastructure` non-dispatch requirement |
| `docs/capabilities.md` | **392 lines**. `EXT-08` `:248`; `RPT-01` `:263`, `RPT-02` `:264`, `RPT-03` `:265`, `RPT-04` `:266`, `RPT-05` `:267`; the `EXT-08` and `RPT-01`–`RPT-05` rendering mention at `:354` | `wc -l` → 392; `grep -n 'RPT-0\|EXT-08'` | **0 lines expected.** Body: "only if a capability's canonical owner changes as a consequence". Budget ~+2 if one does |
| `docs/design/assets/report-renderer/templates/` | **seven governed files**: six `.scriban` (`advert_evidence_pack`, `assessment_fee_note`, `assessment_report`, `expert_report`, `fee_note`, `market_valuation_evidence`) plus `report.css` | `ls` | **read-only.** The scope table is written *about* them; Guardrails forbid editing one |
| `src/Pegasus.Infrastructure/Pegasus.Infrastructure.csproj:42-53` | **five** embedded report assets: `assessment_report.scriban`, `assessment_fee_note.scriban`, `report.css`, `logo_no_margin.png` (linked to `Reports\Assets\brand\logo.png`), `andy_patterson.png` | `sed -n '42,53p'` | **read-only.** Guardrails forbid editing a `.csproj` |
| `scripts/Test-DocumentationLinks.ps1` | present; **takes no parameters** (`param()`), and CI runs it bare at `.github/workflows/ci.yml:87` | `sed -n '1,12p'` | run, not edited |
| `scripts/Test-MarkdownPlacement.ps1` | present, but it is the **validator** and its `-Base` and `-Head` parameters are **`[Parameter(Mandatory)]`** — a bare invocation prompts and fails non-interactively. CI does not call it directly; the `documentation` job runs its regression suite `./scripts/Test-TestMarkdownPlacement.ps1` at `.github/workflows/ci.yml:84` | `sed -n '1,10p'`; `grep -rn MarkdownPlacement .github/` | run **with** `-Base`/`-Head`; not edited |
| Git remotes | **only `origin`** (`https://github.com/merceralex397-collab/pegasusDesktop.git`) — **there is no `upstream` remote in this working tree** | `git remote -v` | none; see step 2 |

**Two measured findings that are *not* this ticket's to fix**, recorded so the implementer does not
"helpfully" fix them and collide with another ticket:

1. **The totals paragraph disagrees with its own table.** `:192` states `report-decision` **13**,
   while exactly **11** rows carry that disposition
   (`grep -c '^\| [A-Z].*report-decision'` → 11), and the ticket body names eleven. The four totals
   also sum to **110** (`18 + 26 + 13 + 53`) against the heading's stated **109** rows at `:77`.
   The totals paragraph `:191`–`:195` is **[[FND-022]] (plan handle `DSK-01-09`) step 15(c)'s** to
   restate — its own plan's inventory names those exact lines. Report the measurement to that
   ticket; do not edit the paragraph here.
2. **The recreation rule still states the withdrawn `refs` clause.** `:69`–`:70` reads "with `refs`
   containing the upstream ID (`upstream:<ID>`)", which this ticket's Guardrails record as withdrawn
   outright — `refs` accepts only repository-relative paths that exist, so an entry of
   `upstream:TICK-208` is not a path and fails the whole `create_items` entry. Correcting `:69`–`:70`
   is **[[FND-022]] step 15(b)'s**, explicitly. Follow the corrected rule (title prefix plus
   `upstream-<ID>` label) when creating anything under step 10, and leave the paragraph alone.

Size basis for the ~30 added lines in area 07 § 8: a seven-row table with a header and separator is
9 lines; upstream TICK-206's twelve-entry negative list is 12 plus a lead-in; the non-dispatch
requirement naming its two owners is ~4; the rest is connective prose.

## Approach

Treat this as a **decision-recording** ticket that adopts what upstream already settled and decides
only what the desktop era genuinely adds. Four of the eleven — upstream `TICK-206`, `TICK-208`,
`TICK-214` and `TICK-216` — already carry written plans and closed `open-questions` documents, so
the work is to read those documents, cite the answers, and add the one consequence upstream had no
reason to weigh (an asset embedded in a desktop assembly ships to every workstation inside the MSIX;
an identifier being unreachable in the retained gateway renderer says nothing about the client one).
The rejected alternative was to disposition all eleven from the carry-over table's one-line
summaries — faster, and the stop condition the body names, because for four of them **the answer
lives in their pipeline documents and not in their bodies**. Re-deciding a settled question is how
uncertainty re-enters a programme that has already paid to remove it. The second rejected
alternative was to make the upstream `TICK-208` disposition conditional on what the desktop finalise
path does; rejected because the defect exists regardless — Core carries one `ReportApprovalId` and
one `ReportSentEvidenceId` per case — and under D-001 nobody upstream will fix it after the freeze.

## Governing docs

The ticket's `refs` is **empty** and it carries **`docs_todo: true`**.

> **New ADR** — ADR-0108 (isolated, non-UI WebView2 HTML→PDF rendering; gateway renderer retained
> until golden-file parity), which every disposition in this ticket is judged against. ADR-0108 has
> two claimants — [[FND-007]] (plan handle `DSK-00-07`) and [[FEAT-038]] (plan handle `DSK-07-12`);
> authored by [[FND-007]], see [[FND-007]]'s plan for the ownership reconciliation, with
> [[FEAT-038]] owning the Phase 7 content and the acceptance flip.
> This plan is written to the decision as recorded in
> `docs/desktop/00-governance-and-workflow/README.md` § 3 (the ADR-0108 row) and in
> `docs/desktop/README.md` § Locked decisions (L-03); if the ADR lands differently this plan is
> revised before implementation. **This ticket writes no ADR**; it cites one.

Because `refs` is empty, the programme-level authorities that bind today, with the step that
satisfies each:

| Authority | Requirement | Met by |
| --- | --- | --- |
| L-03 (index § Locked decisions) | Rendering moves to the isolated WebView2 path; the gateway renderer is retained until parity — every disposition is judged against it | Steps 4–9 |
| D-001 (index § Locked decisions) | The fork becomes the single release source; a ticket left upstream must be one nobody needs during the conversion | Steps 7 and 9's `unchanged-backlog` conditions |
| Proposal § 13.11 | Post-alpha capabilities are not smuggled into feature parity | Step 9 |
| Proposal § 12.5 | Documents, PDFs and reports | Steps 3–4 |
| Proposal § 24 Phase 7 | Exit gate: "no required report depends on the web renderer unless explicitly retained" | Step 4's ships / retires / stays-gated table |
| Carry-over register § Disposition categories `:64` | `report-decision` = renderer/report decisions folded into the ADR-0108 plan; lands in area 07 and fork area `documents-reports` | Steps 10–11 |
| Carry-over register recreation rule `:67`–`:75`, **as corrected by [[FND-022]] step 15(b)** | Provenance lives in the title prefix and the `upstream-<ID>` label — **never in `refs`** | Step 10 |
| `HZN-001` / `board-conventions.md` § Upstream ids versus board ids | Upstream ids are never written bare — and this set holds the board's worst collision | Every citation in this document |
| `AGENTS.md` § New Markdown placement | A **new** `.md` outside the allowed roots fails the CI `documentation` job — the validator's own regex is `^((docs/(prd\|frd\|adr\|design\|desktop))\|workspaces/document-extraction\|\.agents/skills\|\.design-sync\|\.grok\|\.stitch\|design/planning-and-old-designs)/.+\.md$` and it checks only added, copied and renamed files | Step 12; ticket-transient notes live in Kanmer |
| `docs/engineering.md:72-88` tier 1 | Static/documentation consistency only | Verification |
| `docs/engineering.md:201-207` § Plan sizing | Diff estimate first, derived from a measured inventory | This plan's first line and the inventory above |
| `AGENTS.md` § Repository task workflow step 5 | Review by an agent that did not implement | Routing |

## Routing

Copied from the ticket body's `## Routing` block.

- **Subagent**: `pegasus-parity-researcher` — `.codex/agents/pegasus-parity-researcher.toml`
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `kanmer-tickets`
  (`.grok/skills/kanmer-tickets/SKILL.md`) → `kanmer-docs` (`.grok/skills/kanmer-docs/SKILL.md`)
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `search_items`, `get_item`,
  `create_item`, `link_doc`, `set_ticket_doc`, `append_scratch`, `move_item`)
- **Kanmer pipeline** for profile `chore`: `kanmer-plan` → `kanmer-execute` → `kanmer-verify` →
  `kanmer-closeout`. Gates are `leave-preparing` (plan plus questions-resolved) and `enter-done`
  (proof plus questions-resolved); call `get_doc_gates <id>` before every move.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md`
  § Repository task workflow step 5)

## Steps

Refines the body's twelve steps in the same order and with the same ownership.

1. **Orient and take.** Read the plan row (`docs/desktop/07-integrations/README.md` § 5,
   `DSK-07-17`), the carry-over register's § Disposition categories (`:58`, `report-decision` at
   `:64`), the recreation-rule paragraph (`:67`–`:75`) **and the note above that its `refs` clause
   is withdrawn and owned by [[FND-022]]**, the eleven rows at `:118`–`:120` and `:125`–`:132`, the
   upstream-to-board join in the ticket body, and ADR-0108 from [[FEAT-038]]. Call
   `get_doc_gates FEAT-043`, then `take_ticket` on branch
   `task/dsk-07-17-report-decision-dispositions`.
2. **Pin the upstream source before reading anything from it.** `git remote -v` in this tree returns
   **only `origin`** — there is no `upstream` remote here, so the carry-over document's "upstream
   board" is not reachable from this checkout. Clone read-only **outside** the working tree exactly
   as [[FND-022]] (plan handle `DSK-01-09`) step 2 records:
   `git clone --branch kanmer-board --single-branch https://github.com/collisionengineers/pegasus <temp>`,
   or read a pinned head with `git --git-dir=<temp>/.git show <head>:.kanmer/…`. Two heads are on
   record — `4694067` (2026-08-23, the 109-row triage table) and `a5b28111` (2026-08-24, the head
   the nineteen imports were copied from). **Never add `upstream` as a writable remote and never
   push to it.** Record the head and its date in this document under a dated heading.
   Then read each of the eleven **in full** — the body **and** its `plan`, `research` and
   `open-questions` documents, because for four of them the decision lives only in those documents.
   Copy title, labels and the relevant text into the ticket's working notes.
   **Do not disposition a ticket from its one-line summary; the body calls that a stop condition.**
3. **Build the template scope table first**, because four of the eleven turn on it. All seven
   governed files under `docs/design/assets/report-renderer/templates/`, each marked **embedded
   today** — `assessment_report.scriban`, `assessment_fee_note.scriban` and `report.css`, which is
   what `Pegasus.Infrastructure.csproj:42-53` actually embeds — or **present but not embedded**:
   `advert_evidence_pack.scriban`, `expert_report.scriban`, `fee_note.scriban`,
   `market_valuation_evidence.scriban`. For each not embedded, record whether **any** code path
   references it (`grep -rn '<name>' src/ tests/`), because "present but unreferenced" and "present
   and referenced" are different dispositions.
   Note for accuracy: the plan set's prose says "seven `.scriban` files"; the measured count is
   **six `.scriban` plus `report.css`**. Write the measured count, not the prose.
4. **Disposition each of the seven templates against L-03** as **ships with the desktop renderer**
   (embedded by [[FEAT-039]] (plan handle `DSK-07-13`), covered by [[FEAT-041]] (plan handle
   `DSK-07-15`) fixtures), **retires** (no capability needs it), or **stays gated** (retained in the
   governed source, not embedded, activated only when its own ticket lands). Name the capability id
   from `docs/capabilities.md` behind each — the candidates are `EXT-08` (`:248`), `RPT-01` (`:263`),
   `RPT-02` (`:264`), `RPT-03` (`:265`), `RPT-04` (`:266`) and `RPT-05` (`:267`). `RPT-02` is the
   only one at `Now` / `0.1.0-alpha.1`; the rest are `Later` / `1.1.0`, which is itself evidence for
   "stays gated".
5. **Record upstream `TICK-206` as adopted, not re-decided.** Its `open-questions` document reads
   "no unresolved questions" and its plan carries the operator's answer, so cite that answer rather
   than reaching a fresh one. The recorded decision: activate only the `rendererref1`
   assessment-report family — one closed typed operation over the four Core-owned outcomes
   `total_loss`, `repairable`, `cash_in_lieu`, `contract_repair` — plus its accepted fee-note
   artifact; `RPT-01` carries the shared deterministic rendering mechanics, `RPT-02` the assessment
   outcomes, fee note and itemised repair specification, `EXT-08` the accepted-data activation; every
   other workspace catalogue entry stays inactive and non-discoverable, and no caller supplies or
   discovers a template identifier. Use the twelve-entry negative list **verbatim** as the input to
   step 4's table: `market-valuation-evidence`, `advert-evidence-pack`, `fee-note` as a raw selector,
   `expert-report`, `blank-letterhead`, `repairable-contract-repair-report`, `total-loss-report`,
   `addendum-report`, `diminution-rebuttal`, `roadworthy-criminal-report`, `part-35-response`,
   `response-letter`.
   Then add the desktop-era consequence upstream had no reason to state: **none of the twelve is
   dispatchable from `Pegasus.Desktop.Infrastructure` either** — an identifier being unavailable in
   the retained gateway renderer proves nothing about the client one. Write that as a **named
   acceptance obligation on [[FEAT-039]]** (which embeds) and **[[FEAT-041]]** (which compares the
   two renderers). **This ticket records the obligation and writes no test** — Guardrails forbid
   touching a `.csproj` or renderer source. Upstream `TICK-206` has **no fork ticket** today; create
   one under step 10 only if work remains after the table is written.
6. **Record upstream `TICK-216` as adopted, not re-decided.** The Collision Engineers operator
   answered it on 2026-08-19 ("all yes") and its `open-questions` document carries that answer
   ticked. The accepted contract: the exact `reference/rendererref1/` assessment-report wording, its
   named qualifications, and all three bundled engineer signatures — Andy Patterson, Ed Mawdsley and
   Neil O'Reilly — for active draft generation, **provided** the selected engineer's name,
   qualification and signature match as one tuple; a missing, unknown, mismatched or substituted
   value fails closed; human approval is still required before issue; wording absent from the
   supplied evidence stays unavailable and must not be invented; and Audit, diminution and addendum
   wording stay outside the acceptance until their own templates are approved.
   Then state the consequence the desktop adds: **an asset embedded in a desktop assembly ships to
   every workstation inside the MSIX**, a materially different exposure from an asset inside a
   server container. Record which signature assets the acceptance authorises for embedding — all
   three — and hand that list to [[FEAT-039]], whose `Pegasus.Infrastructure.csproj:52-53` embeds
   only `andy_patterson.png` today. **[[FEAT-039]]'s own plan states it will not leave Preparing
   until this record exists**, so this step is on that ticket's critical path.
   **If the exposure difference itself needs a fresh operator answer, raise it as an open question
   on this ticket** — that is the body's instruction and it is binding. **Do not re-ask the
   2026-08-19 question.**
7. **Verify [[DOCS-003]]; do not create a second.** Upstream `TICK-208` is already on this board as
   board **[[DOCS-003]]** — the recreation was unconditional and has already happened.
   `get_item DOCS-003` and confirm its title begins `upstream:TICK-208 · `, it sits in fork area
   `documents-reports`, and its body is the verbatim upstream copy. If any of that is wrong,
   `update_item` rather than creating another. The defect exists regardless of what the desktop
   finalise path does — Core carries one `ReportApprovalId` and one `ReportSentEvidenceId` per case,
   so a correction risks replacing the earlier pointer — and under D-001 nobody upstream will fix it
   after the freeze, so **do not make the disposition conditional on the desktop path changing
   something**. Sequence it after **upstream DOCS-001 (board [[DOCS-001]])**, whose report-version
   identity types it reuses, and record on [[DOCS-003]] that **[[FEAT-042]] (plan handle
   `DSK-07-16`) step 11** — issued versions with custody state and sent evidence as separate
   columns — is not implementable until this ledger exists. Its plan is written and its
   `open-questions` document is closed (CASE-23 parked to upstream `TICK-055`), so nothing here
   needs deciding.
8. **Record upstream `TICK-214` as answered, not open.** Its plan and `open-questions` document
   carry the operator's binding direction: no renderer MCPB host or distribution boundary survives,
   resolving ADR-0025's conditional MCPB possibility to "none". For the desktop era **ADR-0108
   supersedes the question outright** — rendering is an isolated WebView2 path inside the desktop
   assembly, not a packaged renderer product — and the gateway's `/mcp` Automation surface is
   unchanged (parity row `PAR-46`). Record it as `unchanged-backlog` **with that answer written
   out**, not as a routing question for area 09 or 12. Then add the one check that genuinely
   remains: after [[FND-023]] (plan handle `DSK-01-10`)'s sync, verify that no `CollisionRenderer.Mcp`
   project, MCPB manifest, stdio renderer host or browser bootstrap has arrived in the fork tree and
   that `src/Pegasus.Web/Mcp/` has gained no renderer tool or route; record the result. A future
   report-status Automation tool is explicitly parked and needs its own caller-backed ticket.
9. **Disposition the remaining seven** — **upstream `DOCS-001`, upstream `DOCS-003`, upstream
   `DOCS-004`, upstream `TICK-081`, upstream `TICK-096`, upstream `TICK-097`, upstream `TICK-100`**
   — as either `report-decision` work now on the fork board or `unchanged-backlog` under proposal
   § 13.11.
   - **Upstream DOCS-001 is already imported as board [[DOCS-001]]**: verify it exactly as step 7
     verifies [[DOCS-003]], and **create nothing**. The board id matching the upstream id here is a
     coincidence, which `HZN-001` / `board-conventions.md` calls out as the trap in its join table.
   - **Upstream DOCS-003 and upstream DOCS-004 have no fork ticket** and are post-alpha `RPT-04` /
     `RPT-05` activation gates. **Do not write `[[DOCS-003]]` for either** — that wiki link resolves
     to the imported upstream TICK-208 ticket and would silently attach a post-alpha gate to a live
     defect. Their capability rows are `docs/capabilities.md:266` and `:267`, both `Later` /
     `1.1.0`, both "Allocation only".
   - Upstream `TICK-081`, `TICK-096`, `TICK-097` and `TICK-100` are the `EXT-08`, `RPT-01`,
     `RPT-02` and `RPT-05` capability rows at `:248`, `:263`, `:264` and `:267`, all labelled
     `later, post-alpha, blocked` in the register.
   **Each blocked or post-alpha disposition needs one sentence naming what would unblock it** —
   "blocked" without a condition is not a disposition.
10. **Apply the recreation rule for every ticket that becomes fork work and does not already
    exist.** Fork area `documents-reports`; title `upstream:<ID> · <upstream title>`; labels = the
    upstream labels plus `upstream-carryover` plus `upstream-<ID>`; body = the original copied
    **verbatim** plus a provenance block; a link to this area plan. **Never `upstream:<ID>` in
    `refs`** — it takes only existing repository-relative paths, so that entry fails the whole
    `create_items` entry; this is the rule [[FND-022]] step 15(b) corrects in the register itself.
    Before creating anything, `search_items` for the **qualified** form (`upstream:TICK-206`) and
    read every hit's **title** — `search_items` is full-text over id, title, body and labels, so
    this ticket's own documents match several of these strings and a non-empty result set is not a
    duplicate. `update_item` an existing ticket rather than creating a second. Do not silently
    rewrite an upstream body, and never edit an id inside an imported ticket's
    `### Upstream ticket <ID> (verbatim)` block.
11. **Write the two document edits.**
    - `docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md` — the **eleven rows only**
      (`:118`, `:119`, `:120`, `:125`–`:132`), each carrying its recorded disposition and, where
      applicable, the fork ticket id **by board id**, stating the upstream id beside it wherever the
      two differ. **Do not touch the totals paragraph `:191`–`:195` or the recreation rule
      `:69`–`:70`** — both are [[FND-022]] step 15's, and the measured discrepancies (`report-decision`
      13 stated against 11 rows; the totals summing to 110 against 109) are reported to that ticket
      rather than fixed here. If a disposition in step 4–9 changes a row's category, say so in the
      report so [[FND-022]]'s restatement lands on the right numbers.
    - `docs/desktop/07-integrations/README.md` § 8 — the template scope table, upstream TICK-206's
      twelve-entry negative list, and the `Pegasus.Desktop.Infrastructure` non-dispatch requirement
      naming [[FEAT-039]] and [[FEAT-041]] as its owners. 286 lines today.
    - `docs/capabilities.md` only if a capability's canonical owner changes as a consequence;
      the expectation is no change.
    **Add no new `.md` file anywhere.** Both edits are to files that already exist inside
    `docs/desktop/`, so the placement validator is satisfied either way; ticket-transient notes
    belong in Kanmer, not in a new document.
12. **Verify, record and open the PR.** Run `pwsh ./scripts/Test-DocumentationLinks.ps1` (it takes
    **no** parameters — `param()` — and CI invokes it bare at `.github/workflows/ci.yml:87`) and
    `pwsh ./scripts/Test-MarkdownPlacement.ps1 -Base origin/dev -Head HEAD` (**both parameters are
    `[Parameter(Mandatory)]`**; a bare invocation prompts and fails non-interactively — the body's
    shorthand omits them). Both must pass. Note that CI's `documentation` job does **not** call the
    validator directly; it runs the validator's own regression suite
    `./scripts/Test-TestMarkdownPlacement.ps1` at `.github/workflows/ci.yml:84`, so the local
    validator run is an extra check rather than a reproduction of the lane. List every open question
    that survived — realistically only step 6's, and only if the desktop-exposure difference needs a
    fresh operator answer — in this ticket's `open-questions` document. **An unticked item blocks the
    move, which is correct here.** Then open the PR into `dev`.

## Verification

Evidence tier from the body: **Tier 1 — Static/build/architecture**
(`docs/engineering.md:72-88` item 1: consistency only — the register is complete, links resolve,
placement passes, and no source or governed asset changed). `proof` is the captured output of:

- `pwsh ./scripts/Test-DocumentationLinks.ps1` — expected exit 0, no broken link. No parameters.
- `pwsh ./scripts/Test-MarkdownPlacement.ps1 -Base origin/dev -Head HEAD` — expected exit 0. The
  `-Base`/`-Head` parameters are mandatory. Expected to be trivially green here, because this ticket
  adds no `.md` file and the validator inspects only added, copied and renamed paths.
- `pwsh ./scripts/Test-TestMarkdownPlacement.ps1` — expected exit 0. This is what the CI
  `documentation` job actually runs (`.github/workflows/ci.yml:84`).
- `grep -c '^| [A-Z].*report-decision' docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md`
  — expected **11** before and after, unless a step 4–9 disposition deliberately re-categorises a
  row, in which case the new number is stated in the proof and reported to [[FND-022]].
- `git ls-files | grep -Ei "collisionrenderer\.mcp|\.mcpb$|mcpb"` — expected **no output**; upstream
  `TICK-214`'s retired renderer host, manifest and bundle surfaces are absent from the fork tree.
- Kanmer `search_items` for `upstream:TICK-208` — expected **exactly one** ticket, board id
  `DOCS-003`, in `documents-reports`. A second result is a duplicate to reconcile, not to leave.
- Kanmer `search_items` for `upstream:DOCS-001` — expected **exactly one** ticket, board id
  `DOCS-001`, in `documents-reports`.
- Kanmer `search_items` for `upstream:` — expected: each fork ticket findable by its upstream id
  through its title prefix and its `upstream-<ID>` label, and **no** ticket carrying an
  `upstream:<ID>` entry in `refs`.
- `git diff --stat origin/dev -- src docs/design` — expected **empty output**. This is the
  observable form of the Guardrail "must not edit a `.scriban` template, `report.css`, a `.csproj`,
  or any renderer source".
- `git diff --stat origin/dev -- docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md`
  — expected: changes confined to the eleven row lines; **no** hunk touching `:69`–`:70` or
  `:191`–`:195`.

Tier 1 proves the register is coherent; it proves nothing about any carried-over defect being
fixed, and nothing about the renderer.

## Risks / open questions

- **Risk — a settled question is re-decided from a one-line body.** For upstream `TICK-206`,
  `TICK-214`, `TICK-216` and `TICK-208` the answer lives in their `plan` and `open-questions`
  documents, not their bodies. Mitigation: step 2 requires reading all four documents per ticket
  from the pinned clone, and the body names re-deciding as a stop condition.
- **Risk — the upstream clone is unreachable.** `git remote -v` shows only `origin`; there is no
  `upstream` remote here. Mitigation: step 2 carries the exact read-only clone command and two
  pinned heads from [[FND-022]]'s plan, and records the head used.
- **Risk — colliding with [[FND-022]] on the same file.** Both tickets edit
  `upstream-kanmer-carryover.md`. Mitigation: the boundary is stated line by line — this ticket
  owns the eleven rows, [[FND-022]] owns `:69`–`:70`, `:165` and `:191`–`:195` — and the final
  verification asserts no hunk crosses it.
- **Risk — the id namespace.** `upstream DOCS-003` is a post-alpha RPT-04 activation gate with
  **no** fork ticket; board `DOCS-003` is upstream TICK-208, a live Sent-evidence defect. Writing
  `[[DOCS-003]]` for the activation gate points a reader at the defect. Board `DOCS-001` matches its
  upstream id by coincidence; board `DOCS-002` is upstream TICK-018 and is not in this set.
  Mitigation: every citation here is `upstream <ID>` or `upstream <ID> (board [[<board-id>]])`.
- **Risk — `create_items` duplicates rather than fails.** Mitigation: step 10's qualified
  duplicate check reading hit **titles**, not counts, before any write.
- **Risk — provenance written into `refs`.** It fails the whole entry. Mitigation: step 10 states
  the corrected rule inline rather than relying on the register, whose `:69`–`:70` is still stale.
- **Risk — a new `.md` is added for the working notes and fails the placement lane.** Mitigation:
  step 11's closing instruction, and the allowed-path regex recorded in the Governing docs table.
- **Risk — "blocked" recorded without a condition.** Mitigation: step 9 makes the unblocking
  sentence an acceptance criterion.
- **Scope boundary, not an open question** — the ADR-0108 text is [[FEAT-038]]'s; the template
  embedding is [[FEAT-039]]'s; the two-renderer comparison and the non-dispatch test are
  [[FEAT-041]]'s; the finalise path and its ledger dependency are [[FEAT-042]]'s; the register's
  totals, recreation rule and `TICK-054` row are [[FND-022]]'s; the post-sync ADR and code-drift
  re-check is [[FND-023]]'s.
- **No open question is opened at planning time.** The body instructs none unconditionally, and
  every decision in the eleven is either already taken upstream or determinable from the repository.
  **Step 6 may open exactly one during implementation** — whether the desktop-package exposure of all
  three engineer signatures needs a fresh operator answer — and the body explicitly authorises that.
  If it is opened it will hold `leave-preparing`, `enter-review` and `enter-done` shut, which is the
  contract working, not a defect. It will **not** gate `leave-backlog`.

## Simplification pass

_`n/a — docs-only`._
