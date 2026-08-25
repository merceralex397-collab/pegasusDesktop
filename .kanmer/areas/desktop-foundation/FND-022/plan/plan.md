# Plan — FND-022: triage the upstream board and audit the carried-over tickets on the fork board

**Diff estimate: ~1 repository file, ~40 lines (about +31 / −9).**

`docs/engineering.md` § Plan sizing requires the estimate first, and profile `chore`
owes neither `research` nor `files`, so this plan carries the surface-area burden
alone. The estimate is derived from the measured inventory below, not asserted.

### Measured file-and-line inventory (read 2026-08-24 at `bbd1c549`, branch `task/desktop-plan-segmentation`)

The **only** repository file this ticket may edit is
`docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md` — Guardrails
§ Scope boundary. Measured with `wc -l`, `grep -n` and `awk`:

| Measurement | Command | Value today |
| --- | --- | --- |
| File length | `wc -l docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md` | **224 lines** |
| Section starts | `grep -n '^#\{1,2\} ' <file>` | `:1` title, `:12` Upstream board shape, `:58` Disposition categories, `:77` Triage table, `:197` Code drift and the first sync |
| Disposition-category table | (rows between `:59` and `:65`) | 4 dispositions, header at `:61`, rows `:63`–`:66` |
| Recreation rule | `grep -n 'refs' <file>` → single hit at `:69` | paragraph `:67`–`:75`; the wrong clause is `:69`–`:70` (`with \`refs\` containing the upstream ID (\`upstream:<ID>\`)`) |
| Triage table data rows | `sed -n '80,196p' <file> \| grep -c '^| '` → 110 (1 separator + data) | header `:79`, separator `:80`, **109 data rows** at `:81`–`:189` |
| `TICK-054` row | `grep -n 'TICK-054' <file>` | exactly one hit, `:165`, currently `gateway-worker-ticket (provider port exists, unavailable by default) \| 07 (mail)` |
| Disposition totals paragraph | (after the table) | `:191`–`:195`, states `desktop-screen-spec` 18 / `gateway-worker-ticket` 26 / `report-decision` 13 / `unchanged-backlog` 53 and the sentence "none is dropped outright" |
| `unchanged-backlog` restriction sentence | `grep -c 'docs/capabilities.md row' <file>` | **0** — the sentence step 15(e) owns does not exist yet, so this ticket writes it for the first time |

Edit-by-edit line budget, which is where the estimate comes from:

| Edit (body step 15) | Anchor | Added | Removed |
| --- | --- | --- | --- |
| (a) dated "Recreated on the fork board" line with the 19 / 21 / 75 figures, the upstream head and group `EPIC-014` | after `:195` | ~8 | 0 |
| (b) § Recreation rule corrected — upstream id in the ticket **title and labels**, not `refs` | `:69`–`:70` | ~4 | 3 |
| (c) five post-triage upstream rows (`DOCS-013`, `ENG-014`, `ENG-015`, `INTK-034`, `INTK-035`) plus the 109-rows-against-114-open note; heading `:77` and the totals paragraph `:191`–`:195` re-stated | `:77`, after `:189`, `:191`–`:195` | ~13 | 5 |
| (d) `TICK-054` re-filed to `unchanged-backlog \| —` | `:165` | 1 | 1 |
| (e) § Disposition categories sentence restricting `unchanged-backlog` to rows that have a `docs/capabilities.md` row | after `:66` | ~5 | 0 |
| **Total** | | **~31** | **~9** |

No `src/`, `tests/`, `scripts/` or `infra/` file is touched, so the diff is
documentation-only and the `## Simplification pass` records `n/a — docs-only`.
Board writes (`update_item`, `create_items`, `set_ticket_doc`) do not appear in the
git diff at all; they are verified by tool output instead (see Verification).

## Approach

Execute the ticket body as an **audit-plus-delta**, not as a creation run: the 19
imports the 2026-08-24 coverage pass produced already exist — `get_group EPIC-014`
returns exactly 19 members, all at `backlog`, titled `upstream:<ID> · …` — so the
work is to verify each against the join table, confirm the 21 amendments, record the
75 drops in this document, re-run the classification against the upstream head that
is current on the day, and create only what that delta opens. The alternative —
running step 11's creation recipe over the whole import list as though the board were
empty — was rejected because `create_items` is ungated: it duplicates rather than
fails, so a second creating pass would produce 19 duplicate tickets competing for the
same code under a second acceptance criterion, which is the exact defect the ticket
body says it was rewritten to prevent. The second rejected alternative is the older
"flat recreation of the triage table" (57 tickets from 56 upstream ids): fifteen of
those ids are already delivered by named seeded tickets, two are already merged into
the fork's `main` (proved below), one is moot with the Razor front end, and two are
decisions [[FEAT-043]] owns.

## Governing docs

`refs` on this ticket is **empty** and `docs_todo: true` is set — confirmed by
`get_doc_gates FND-022`, which reports `"refs": []` and `"docs_todo": true`, and by
the profile resolution `chore → leave-preparing: [plan, questions-resolved]`
(there is no `leave-backlog` `governing-doc` requirement for `chore`; only `feature`
carries one).

> **New ADR** — ADR-0100 (native WinUI 3 desktop client converted inside this fork;
> its consequences record D-001, the fork becoming the single release source),
> authored by [[FND-005]] (plan handle `DSK-00-05`); **ADR-0100 has more than one
> claimant** — [[FND-026]] (plan handle `DSK-02-01`) also names it — so see
> [[FND-005]]'s plan for the ownership reconciliation rather than assuming a single
> author. This plan is written to the decision as recorded in
> `docs/desktop/00-governance-and-workflow/README.md` § 3 (D-001, and the ADR table
> row for ADR-0100) and in `docs/desktop/README.md` § Locked decisions (L-05,
> D-001, D-004); if the ADR lands differently this plan is revised before
> implementation.

Because `refs` is empty, the New-ADR paragraph alone is not sufficient. The
programme-level authorities that bind **today**, each with the step that satisfies
it:

| Authority | Requirement | Met by |
| --- | --- | --- |
| `docs/desktop/README.md` § Locked decisions — **L-05** | The Kanmer board is seeded by the implementing agent from these plans, and the open upstream board is triaged in area 01 | Steps 1–5, 11 |
| `docs/desktop/README.md` § Locked decisions — **D-001** | The fork becomes the single release source; an upstream row not carried before the freeze is lost, not deferred | Steps 3, 11, 12 |
| `operator-decisions.md` — **D-004** (operator, 2026-08-24) | `OPS-10` acceptance folds into the desktop pilot approval; upstream `TICK-001` stays dropped and no ticket is imported for it | Step 5 (drop list), Verification row 5 |
| Send to AI recorded exclusion (operator, 2026-08-24) | Upstream `TICK-102` / capability `AI-09` is a **recorded exclusion with a reactivation condition**, not an open question: no speculative tickets, no `open-questions` document, no reopening of the `reuse-map.md` `AiWork/` row | Step 12 |
| Group document `HZN-001` / `board-conventions.md` § *Upstream ids versus board ids* | A bare `<PREFIX>-<nnn>` is a fork board id; an upstream id is written `upstream <ID>` or `upstream <ID> (board [[<board-id>]])`; the join table is read, never computed | Steps 3, 5, 7, 9; every id in this document |
| Kanmer group `EPIC-014` body | The corrected 2026-08-24 classification against upstream head `a5b28111` (114 open non-archived) and its five classes; `DSK-01-09` owns the re-run, `DSK-00-04` owns the counts | Steps 1, 3, 4, 5, 11 |
| Proposal § 13.11 (`docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md`) | Post-alpha capability rows are not smuggled into parity | Step 5 (58 post-alpha drops), step 12 (Send to AI) |
| Proposal § 25 / `docs/desktop/00-governance-and-workflow/README.md` § Ticket template | Every plan document carries a `## Routing` block and a dated `## Simplification pass` | This document's `## Routing` and `## Simplification pass` |
| `AGENTS.md` § Repository task workflow step 4 | Simplification pass over this branch's own diff before the PR, recorded under a dated heading; `n/a — docs-only` where the diff is documentation | `## Simplification pass` |
| `AGENTS.md` § Repository task workflow step 5 | Independent review by an agent that did not implement | Routing → reviewer `pegasus-desktop-reviewer` |
| `docs/engineering.md` § Required evidence tiers | Tier 1 — static/build/architecture; board state and documentation gates evidenced by tool output and counts | Verification |
| `docs/desktop/00-governance-and-workflow/README.md` § Kanmer board shape | The sixteen fork areas and their prefixes; imports land in the upstream domain area, not a conversion area | Steps 8, 14 |
| `docs/runbook.md` § Live-operation approval matrix | No Azure write; this ticket makes no Azure call at all | Guardrails; Risks |

## Routing

Copied from the ticket body's `## Routing` block, which
`docs/desktop/00-governance-and-workflow/README.md` § Ticket template makes
mandatory in the plan document specifically.

- **Subagent**: `pegasus-parity-researcher` — `.codex/agents/pegasus-parity-researcher.toml`
  (verified present). It gathers evidence only; **the parent session performs the
  board writes**, because the researcher runs read-only and must not call
  `create_item`.
- **Skills**, loaded in this order:
  1. `pegasus-desktop` — `.agents/skills/project/pegasus-desktop/SKILL.md` (verified present)
  2. `kanmer-tickets` — `.grok/skills/kanmer-tickets/SKILL.md` (verified present)
  3. `kanmer-groom` — `.grok/skills/kanmer-groom/SKILL.md` (verified present)
- **MCP**: Kanmer only — `get_status`, `list_board`, `list_groups`, `get_group`,
  `get_group_doc`, `list_items`, `search_items`, `get_doc_gates`, `create_item`,
  `create_items`, `update_item`, `link_items`, `set_ticket_doc`, `append_scratch`,
  `take_ticket`, `move_item`. **No Azure MCP tool is called on this ticket.**
- **Kanmer pipeline** for profile `chore`: `kanmer-plan` → `kanmer-execute` →
  `kanmer-verify` → `kanmer-closeout`. Gated boundaries resolved by
  `get_doc_gates FND-022`: `leave-preparing` needs `plan` + `questions-resolved`;
  `enter-done` needs `proof` + `questions-resolved`. Call `get_doc_gates` before
  every move and cross at most one gated boundary per move.
- **Reviewer**: `pegasus-desktop-reviewer` —
  `.codex/agents/pegasus-desktop-reviewer.toml` (verified present); an agent that did
  not implement, per `AGENTS.md` § Repository task workflow step 5.

## Steps

These refine the ticket body's fifteen implementation steps — same order, same
ownership, same file paths — adding the *how* the body leaves out. They do not
renumber or contradict it.

1. **Orientation and take.** Read
   `docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md` in full
   (224 lines) — especially § Disposition categories at `:58` and § Recreation rule
   at `:67`–`:75`. Then `get_group EPIC-014` and read its body (the corrected
   classification, 5 classes, head `a5b28111`, 114 open). Then
   `get_group_doc HZN-001 board-conventions.md` and read § *Upstream ids versus
   board ids* — that document is authoritative over step 3's table wherever the two
   disagree. Then `docs/desktop/00-governance-and-workflow/README.md` § Kanmer board
   shape. Finally `get_status`, `list_board` (expect the sixteen areas),
   `get_doc_gates FND-022`, `take_ticket FND-022`.
2. **Pin the upstream source.** Clone read-only **outside** this working tree:
   `git clone --branch kanmer-board --single-branch https://github.com/collisionengineers/pegasus <temp>`,
   or read a pinned head with `git --git-dir=<temp>/.git show <head>:.kanmer/...`.
   Record the head SHA and its date in this plan. Two heads are already on record:
   `4694067` (2026-08-23, the 109-row triage table) and `a5b28111` (2026-08-24, the
   head the 19 imports were copied from). **Never** add `upstream` as a writable
   remote here and never push to it — this repository has only `origin`
   (`git remote -v` → `origin` fetch/push, verified 2026-08-24).
3. **Read the join table; do not compute it.** The 19 rows are in the ticket body
   step 3 and in `HZN-001/board-conventions.md`. Verify the two agree row for row and
   record any disagreement as resolved in favour of the group document. Per-area
   totals to hold against: `automation-integrations` 2, `engineering-assessment` 2,
   `platform-operations` 2, `intake-processing` 7, `documents-reports` 3,
   `case-reference-workflow` 2, `desktop-ui` 1; `delivery-repository`,
   `mail-communications` and `pr-review` **zero**. The three joins that bite:
   board [[CASE-001]] is upstream `CASE-021` (a live defect blocking [[FEAT-005]],
   [[GWY-006]], [[FEAT-001]] and [[DUI-006]]) while upstream `CASE-001` has no fork
   ticket; board [[DOCS-001]] is upstream `DOCS-001` **by coincidence only**, so it
   still gets the full form; the seven `INTK` rows shift by one for the first four
   and not at all for the last three, and there is no formula.
4. **Confirm the 21 amendments; do not import them and do not edit them.**
   `get_item` each of `DSK-01-09` (this ticket), `DSK-01-10`, `DSK-03-07`,
   `DSK-03-08`, `DSK-03-09`, `DSK-03-11`, `DSK-05-07`, `DSK-05-11`, `DSK-05-12`,
   `DSK-05-13`, `DSK-05-15`, `DSK-05-16`, `DSK-05-22`, `DSK-05-23`, `DSK-06-03`,
   `DSK-07-05`, `DSK-07-11`, `DSK-07-13`, `DSK-07-16`, `DSK-07-17`, `DSK-08-13`
   (resolve each handle with `search_items` for the handle string — every seeded
   ticket carries it as its title prefix — never by guessing a board id). Confirm the
   added acceptance line **and** the implementation step behind it are present.
   Record any that are missing them as a finding in this plan for the board owner;
   **never** compensate by importing the upstream row, and never amend another
   agent's ticket from here. Five upstream ids appear on both lists — upstream
   `DOCS-001`, upstream `TICK-208`, upstream `ENG-014`, upstream `ENG-015`, upstream
   `CASE-022` — and are counted once, in the import list.
5. **Record the 75 drops here, in this plan, with the reason from `EPIC-014`.**
   58 post-alpha capability rows (proposal § 13.11 — their register stays the
   carry-over document and their capability rows stay in `docs/capabilities.md`);
   13 covered by a named seeded ticket (upstream `AUTO-006` and upstream `AUTO-007`
   → `DSK-05-19` + `DSK-03-15`; upstream `PLAT-005` → `DSK-05-22`; upstream
   `PLAT-023` → `DSK-05-20`; upstream `PLAT-025`/`PLAT-026`/`PLAT-027` →
   `DSK-05-19`; upstream `PLAT-029` → `DSK-06-04`, which is **not** board
   [[PLAT-029]], the import of upstream `PLAT-038`; upstream `PLAT-035` →
   `DSK-10-18`; upstream `PLAT-036` → `DSK-10-14` + `DSK-10-16`, mirrored by
   `DSK-11-09`; upstream `INTK-019` → `DSK-05-11`; upstream `CASE-012` and upstream
   `UICASE-001` → `DSK-05-02`/`DSK-05-03`/`DSK-05-05`/`DSK-05-06`); 2 already merged
   into the fork's `main` (upstream `PR-003`, upstream `PR-026` — step 6); 1 moot
   with the Razor front end (upstream `CASE-001` — step 7); 1 operator decision with
   no fork ticket either way (upstream `TICK-001`, dropped under **D-004**). Add an
   `upstream:<ID>` provenance line to the covering board ticket where the coverage
   decision names one, so the coverage is discoverable from the covering ticket.
6. **Resolve every recorded commit or PR against the fork's `main`.** For each
   `gateway-worker-ticket`/`report-decision` row naming a commit or PR, run
   `git merge-base --is-ancestor <commit> main; echo $?`. `0` means annotate the
   carry-over row rather than hold a ticket — the upstream board status lags, the
   fork does not. **Both known cases are already confirmed at this head
   (2026-08-24):** `8124ae2a` → `0` and `4d00c3b7` → `0`. This is the general rule,
   not two exceptions; upstream `PLAT-039` is one more instance, arriving with the
   first sync ([[FND-023]], plan handle `DSK-01-10`). Record every commit checked and
   its exit code in this plan.
7. **Drop upstream `CASE-001` — and read the id twice.** Board [[CASE-001]] is a
   different ticket (the import of upstream `CASE-021`) and nothing in this step
   touches it. Upstream `CASE-001` has no fork ticket and is not to be given one. It
   is moot with the Razor front end: four dead `TempData` writes in page models the
   `DSK-05-26` cut list deletes, with the operator-facing message surviving as data
   via the Received item's Case tab. Obtain the operator sign-off this step requires
   and record it here. Duplicate check: `search_items` for `upstream:CASE-001` and
   read the **titles** of the hits; never `search_items` for a bare `CASE-001`, which
   matches the live import and makes a correct board look wrong.
8. **Take the area mapping from `EPIC-014` and step 3, not from the table's "Fork
   area" column**, which is wrong in at least two places: upstream `DELIV-006` (board
   [[DUI-017]]) belongs in `desktop-ui` beside its consumer, not in
   `delivery-repository`; and upstream `TICK-054` is not a `gateway-worker-ticket`
   at all (step 10).
9. **Duplicate check before any write.** `search_items` for the qualified form
   (`upstream:INTK-027`, whose import is board [[INTK-004]]) and for a distinctive
   phrase from the title. A bare-id search is safe only where no board ticket holds
   that number — which excludes `CASE-001`, `DOCS-001` and every `INTK`/`PLAT`
   number in the join table. Where a ticket exists — which, for all 19 of step 3, it
   does — `update_item` it; never create a second.
10. **Re-file the upstream `TICK-054` row.** It is at line `:165` of the carry-over
    document, classified `gateway-worker-ticket (provider port exists, unavailable by
    default) | 07 (mail)`. That is wrong: upstream `TICK-054` is post-alpha capability
    `MAIL-13`, excluded three times in the plan set
    (`docs/desktop/07-integrations/README.md:102`, `DSK-07-03`'s trap, `DSK-07-11`
    step 2), and its production activation needs an Entra `Mail.ReadWrite` approval
    nobody holds. The folder-move port the parenthetical describes is capability
    `MAIL-07`'s and is already carried by `DSK-03-12`, `DSK-07-03` and `DSK-05-10`.
    Re-file as `unchanged-backlog | —` (step 15(d)) and confirm no ticket was imported
    for it.
11. **Audit the 19, then create only the delta.** `list_items group: EPIC-014` —
    expect exactly 19 members, all `backlog` (confirmed 2026-08-24:
    `progress.backlog = 19`, `total = 19`). `get_item` each and confirm, per ticket:
    title `upstream:<ID> · <upstream title>`; area and profile as step 3 names;
    labels = the upstream labels verbatim **plus** `upstream-carryover` **plus**
    `upstream-<ID>` **plus** the 2026-08-23 triage disposition where the table records
    one (14 of the 19; the five without are upstream `ENG-014`, `ENG-015`, `INTK-026`,
    `INTK-031`, `INTK-032`) **plus** `needs-operator` where an operator step is
    required — and **no** `desktop-conversion`, `plan-<NN>`, `phase-<N>` or `tier-<n>`
    label, all of which are seeding labels; `groups` = `["EPIC-014"]` and nothing
    else; body = the upstream body verbatim plus the provenance block plus the step 3
    scope note where one is given; `status` unset; `links` empty; **`blocks` intact —
    18 of the 19 block the seeded tickets they gate and only board [[INTK-005]] is
    deliberately empty, because a survey gates nothing. Removing a `blocks` entry lets
    a slice ship around a defect it must not.** The upstream id lives in the title and
    the labels only: `refs` accepts nothing but repository-relative paths to documents
    that exist, so a `refs` entry of `upstream:INTK-027` fails the whole `create_items`
    entry. **Never edit an id inside an imported ticket's
    `### Upstream ticket <ID> (verbatim)` block** — it is a quotation and its ids are
    upstream ids by definition. Then re-run the classification against the step 2
    head: triage every upstream ticket opened, reopened or un-archived since
    `a5b28111` into import / amend / drop by the rules of steps 3–5, and create **only**
    the imports that delta produces — `create_items`, at most 6 entries per call,
    checking each `{ok, item|error}` and retrying failures, with the same title, area,
    profile, label, group, body and provenance shape as the 19. Each new import
    extends step 3's join table with its own `upstream <ID> → board <board-id>` row.
    Record the head, the delta and every id created here.
12. **Re-check the in-flight rows at the step 2 head, and record the standing
    exclusion.** Upstream `INTK-033` (board [[INTK-007]]) — at `review` upstream on the
    unmerged branch `task/intk-033-triage-from-intake`, so [[FND-023]]'s pinned range
    does not bring it; verify at sync and carry the full fix if it has not merged.
    Upstream `CASE-021` (board [[CASE-001]]) — at `implementing` upstream on
    `task/case-021-observed-images` with a complete plan, so verify-after-sync;
    porting it fresh would duplicate and conflict. Upstream `TICK-102` / capability
    `AI-09` (Send to AI) — **a recorded exclusion with a reactivation condition, not
    an open question**, settled by the operator on 2026-08-24 against the code
    (`src/Pegasus.Web/AiWork/SendToAi.cs:12` defines `Features:SendToAi`; lines 35–42
    refuse to compose it outside the `DevelopmentOffline` runtime profile;
    `src/Pegasus.Web/Program.cs:104-110` permits that profile only in Development).
    Record the exclusion with its reactivation condition — the separate non-preview
    transport decision named at `docs/capabilities.md:269`. Do **not** file the two
    speculative tickets, do **not** reopen the `reuse-map.md` `AiWork/` row, and do
    **not** create an `open-questions` document for it on any ticket. Also record the
    status of upstream `PLAT-041`, upstream `DOCS-013`, upstream `ENG-014` and
    upstream `ENG-015`, which are outside the first sync's range, so the standing
    later-sync ticket named in [[FND-023]] § Follow-up ticket to create picks them up.
13. **Groups and governing docs on imports.** `EPIC-014` is the only group an import
    carries: a conversion `EPIC-0xx` would corrupt the epic counts and an `HZN-0xx`
    would imply a phase commitment nobody has made. Confirm `docs_todo: true` on any
    imported `feature` ticket with no existing governing document — `feature` is the
    only profile with a `leave-backlog` `governing-doc` gate, and an honest
    `docs_todo` is what satisfies it. Where the upstream ticket names an existing
    `docs/adr/**` or `docs/frd/**` path, `refs` carries it **and the path must
    exist**; a non-existent `refs` path fails the whole entry.
14. **Verify the board.** `list_items group: EPIC-014` against the 19 plus the step 11
    delta; `list_items` per fork area against the step 3 per-area totals; `get_item`
    on five imports to confirm the body matches upstream and the provenance block
    names the upstream id in full. Record the counts, the five spot-checks, the step 6
    commits, the step 12 statuses and the step 11 audit result in the ticket `proof`.
15. **One edit to the carry-over document, carrying all five changes.** Anchors and
    line budget are in the inventory above: (a) the dated "Recreated on the fork
    board" line after `:195`; (b) § Recreation rule at `:69`–`:70` corrected to put
    the upstream ID in the ticket title (`upstream:<ID> · <title>`) and labels rather
    than `refs`; (c) the five post-triage rows after `:189` plus the heading at `:77`
    and the totals paragraph `:191`–`:195` re-stated as 109 rows against 114 open at
    `a5b28111`; (d) the `TICK-054` row at `:165` re-filed; (e) the § Disposition
    categories sentence after `:66` restricting `unchanged-backlog` to rows that
    **have** a `docs/capabilities.md` row — this ticket is its **single owner**, and
    `grep -c 'docs/capabilities.md row' <file>` returns **0** today, so it is being
    written for the first time; if it is already present when this step runs, leave it
    and record that it was. Then write `plan` and `proof` with `set_ticket_doc`, tick
    every `open-questions/` item if any exist, call `get_doc_gates FND-022`, and move.

## Verification

Evidence tier **1 — static/build/architecture** (`docs/engineering.md` § Required
evidence tiers), as the ticket body states. It proves the register is complete and
free of duplicates; it does not prove any carried-over defect is fixed. The `proof`
document is a `command-log` built from the following, each output pasted raw:

| # | Command / call | Expected |
| --- | --- | --- |
| 1 | Kanmer `list_items group: EPIC-014` | the 19 `upstream:` tickets of step 3, one for one, plus only what step 11's re-run added for a later head |
| 2 | Kanmer `get_group_doc HZN-001 board-conventions.md`, § *Upstream ids versus board ids* compared line by line with step 3 | the same 19 upstream↔board pairs; any disagreement resolved in favour of the group document |
| 3 | Kanmer `list_items` per fork area | `automation-integrations` 2, `engineering-assessment` 2, `platform-operations` 2, `intake-processing` 7, `documents-reports` 3, `case-reference-workflow` 2, `desktop-ui` 1; `delivery-repository`, `mail-communications`, `pr-review` **zero** |
| 4 | Kanmer `get_item` on all 19 | each carries `upstream-carryover` and `upstream-<ID>`, no `desktop-conversion`/`plan-NN`/`phase-N`/`tier-n`, `EPIC-014` as its only group, `status` unset, `links` empty, `blocks` non-empty except board [[INTK-005]] |
| 5 | Kanmer `search_items` for `upstream:CASE-001`, `upstream:TICK-001`, `upstream:TICK-054`, `upstream:PR-003`, `upstream:PR-026`, `upstream:AUTO-006`, `upstream:AUTO-007`, `upstream:CASE-012`, `upstream:UICASE-001`, `upstream:TICK-206`, `upstream:TICK-214`, `upstream:TICK-102` | **no ticket whose title begins with any of those handles.** Read every hit's `title`: `search_items` is full-text over id, title, body and labels, so this ticket's own body matches several of these strings and a non-empty result set is not a failure |
| 6 | `git merge-base --is-ancestor 8124ae2a main; echo $?` and `git merge-base --is-ancestor 4d00c3b7 main; echo $?` | `0` for both — **already observed on 2026-08-24 at `bbd1c549`**; re-run at the implementing head and paste the output |
| 7 | Kanmer `get_item` on five imports | each body matches its upstream body and states the upstream id, disposition and target area plan |
| 8 | Kanmer `get_doc_gates` on one imported `feature` ticket | `leave-backlog` `governing-doc` satisfied by `docs_todo` or a real `refs` path |
| 9 | `grep -c "docs/capabilities.md row" docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md` | **exactly 1** after the edit (it is `0` today) — written by this ticket and by nothing else |
| 10 | `pwsh ./scripts/Test-DocumentationLinks.ps1` | exit code 0 |
| 11 | `pwsh ./scripts/Test-MarkdownPlacement.ps1` | exit code 0 (no new `.md` outside `docs/(prd\|frd\|adr\|design\|desktop)`) |

## Risks / open questions

- **`create_items` is ungated and duplicates rather than fails.** The 19 imports
  already exist. *Mitigation:* step 9's qualified duplicate check before every write,
  and step 11 framed as an audit; the only creation permitted is the step 2 delta.
- **The upstream board keeps moving.** The triage table is dated 2026-08-23 (109 rows
  at `:81`–`:189`) and `EPIC-014` is dated 2026-08-24 (114 open at `a5b28111`).
  *Mitigation:* record the head in step 2 and re-derive every count from it; a
  re-triage is a new ticket, never a rewrite of this one.
- **The table's own totals are stale.** `:191`–`:195` states `desktop-screen-spec` 18
  / `gateway-worker-ticket` 26 / `report-decision` 13 / `unchanged-backlog` 53 and
  claims "none is dropped outright". *Mitigation:* never restate a figure from the
  table without checking it; step 15(c) re-states the paragraph.
- **Two id namespaces collide in appearance.** 45 ids exist on both boards.
  *Mitigation:* `HZN-001/board-conventions.md` is authoritative, the full form is
  used everywhere, and the one place a bare upstream id is correct is inside an
  imported ticket's `### Upstream ticket <ID> (verbatim)` block.
- **Ownership boundary, not an open question:** the 21 amend-list tickets are owned by
  their own agents. This ticket reads them and reports what is missing; it does not
  edit them. Likewise `DSK-00-04` ([[FND-004]], plan handle `DSK-00-04`) owns the
  count verification and creates nothing.
- **Ownership boundary, not an open question:** the § Disposition categories sentence
  of step 15(e) has exactly one owner — this ticket. The imported
  `upstream:INTK-031` ticket (board [[INTK-005]]) annotates its own triage row and
  cites the sentence; it does not write it. Two tickets writing one sentence produces
  a duplicate or a conflict, and either is a stop condition.
- **Operator step, already scheduled:** step 7's sign-off to drop upstream
  `CASE-001`. Answered by the operator at execution time; recorded in this plan.
- **Settled, not open:** D-004 (upstream `TICK-001` stays dropped, no ticket imported)
  and the Send-to-AI recorded exclusion (step 12). Neither is to be re-raised, and no
  `open-questions` document is created for either.

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over this
branch's own diff before the PR, recorded here under a dated heading. Expected result
for this ticket: `n/a — docs-only`, because the only repository diff is the single
edit to `docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md`._

## Execution update — 2026-08-25

### Upstream source and delta

A read-only clone of `collisionengineers/pegasus` branch `kanmer-board` was refreshed outside this repository at:

- head: `8566c18d59481df740abc8ea784e629f91ede6cf`
- commit: `chore(kanmer): sync board 2026-08-25T01:28:04.859Z`
- source date: 2026-08-25

The historical coverage head `a5b28111` exists in the clone. The current head has 477 ticket folders, 245 done, 118 open/non-archived, and no archived delta in the five newly added open findings. The five newly added open tickets since `a5b28111` are upstream `CASE-023`, `DOCS-014`, `ENG-017`, `INTK-036` and `PLAT-043`. An independent `pegasus-parity-researcher` classified all five as amendments to existing owners, not imports and not drops:

| Upstream finding | Fork owner | Reason |
| --- | --- | --- |
| `CASE-023` | [[GWY-009]] / DSK-03-09 | case notes and automatic workflow events |
| `DOCS-014` | [[FEAT-016]] / DSK-05-16, with [[FEAT-031]] gateway coordination | shared viewer preview/download intent and custody event |
| `ENG-017` | [[FEAT-015]] / DSK-05-15 | vehicle/EVA image eligibility and bundle content |
| `INTK-036` | [[ENG-002]] / upstream ENG-015 | QDOS instruction-date extraction owner |
| `PLAT-043` | [[GWY-013]] / DSK-03-13 | Triage mutation routes and Core lifecycle boundary |

No later-head import or legitimate drop was identified.

### Amendment audit and board updates

All 21 recorded amendment handles were resolved with qualified Kanmer search and title matching. Each owner was read with `get_item`; the acceptance and implementation obligations were present. The 21 handles and resolved board owners are recorded in the verification output for this run.

The five later-head amendments were applied individually through Kanmer `update_item` to the five previously unclaimed owners, using their fresh `updated` values. No worktree, branch, claim, or in-progress PR was touched. The amendments add explicit acceptance and implementation obligations for the five findings; the INTK-036 update also replaces the prior statement that bare `Date:` was out of scope, avoiding contradictory requirements.

### Board audit

Kanmer `get_group EPIC-014` and `list_items group:EPIC-014` still show the original 19 imports, one-for-one against the HZN-001 join table. The per-area counts remain automation-integrations 2, engineering-assessment 2, platform-operations 2, intake-processing 7, documents-reports 3, case-reference-workflow 2, desktop-ui 1, with delivery-repository, mail-communications and pr-review at zero. No duplicate import was created.

The carry-over document was updated as the sole repository diff: current upstream source/head, corrected title/label recreation rule, TICK-054 re-filed as unchanged-backlog, the capability-row restriction, the 19/21/75 coverage totals, the five post-triage rows, and the five later-head amendments.

## Verification update — 2026-08-25

- Read-only upstream clone: `8566c18d59481df740abc8ea784e629f91ede6cf`; current open/non-archived count: 118.
- `git merge-base --is-ancestor 8124ae2a main` → exit 0.
- `git merge-base --is-ancestor 4d00c3b7 main` → exit 0.
- `pwsh ./scripts/Test-DocumentationLinks.ps1` → `All relative Markdown links resolve (232 files checked).`
- `pwsh ./scripts/Test-MarkdownPlacement.ps1 -Base origin/dev -Head HEAD` → `Markdown placement passed for origin/dev..HEAD.`
- `git diff --check` → exit 0; the only repository diff is the owned carry-over document.
- `grep -c 'docs/capabilities.md row' docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md` → exactly 1.

The 21 amendment handles were title-matched after full-text search and each owner body was read with `get_item`; every owner contained both acceptance and implementation sections. The five later-head owner bodies now contain their explicit `## Upstream delta` acceptance and implementation sections.

## Simplification pass — 2026-08-25

n/a — docs-only. The branch changes one existing canonical carry-over document; no code, test, abstraction, dependency, or runtime surface exists to simplify. The document edit was kept to the measured five corrections plus the current-head evidence and did not create a transient repository planning file.

## Delivery blocker — 2026-08-25

The branch is pushed at `e38939e7` on `fnd-022-upstream-triage`. PR creation was attempted with `gh pr create --base dev --head fnd-022-upstream-triage` and failed with the exact GitHub response `GraphQL: must be a collaborator (createPullRequest)`. The ticket remains **implementing**; no PR, review gate, merge, or Kanmer stage move is claimed. An independent `pegasus-desktop-reviewer` review is running against the pushed branch, but it cannot substitute for the missing PR permission/authorized PR creation.
