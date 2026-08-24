# Plan — FND-022: triage the upstream board and audit the carried-over tickets

**Diff estimate: ~1 file, ~30 lines** (≈22 added, ≈8 modified, 0 deleted) in the
repository, plus board writes that are not a repository diff.

`docs/engineering.md` § Plan sizing requires the estimate first. This is a `chore`,
so it owes no `research` and no `files` document and this plan carries the surface
area alone. The inventory the estimate is derived from is measured below, not
asserted.

### Measured surface-area inventory

The only repository file this ticket may edit is
`docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md` — **224 lines**
today (`wc -l`), at `bbd1c549`, 2026-08-24. Step 15's five edits land at these
measured positions:

| Edit | Target `path:line` | Measured current value | Change |
| --- | --- | --- | --- |
| 15(e) § Disposition categories restriction | `upstream-kanmer-carryover.md:58` heading; table rows `:60-65`; `unchanged-backlog` row at `:65` | four disposition rows, `unchanged-backlog` described only as "Post-alpha capabilities outside conversion scope" | +4 lines after `:65` |
| 15(b) § Recreation rule correction | `:67-75`; the defective sentence is `:69-70` — "with `refs` containing the upstream ID (`upstream:<ID>`)" | 9-line paragraph | −2 / +3 lines |
| 15(c) five post-triage rows | table rows `:81-190` (109 rows exactly: `sed -n '81,192p' … \| grep -c '^\| '` → `109`); `TICK-054` at `:165` | 109 rows against 114 open upstream tickets at head `a5b28111` | +5 rows, +2 lines of note |
| 15(d) re-file `TICK-054` | `:165` | `… \| gateway-worker-ticket (provider port exists, unavailable by default) \| 07 (mail) \| mail-communications \|` | 1 line modified |
| 15(a) dated recreation line | the totals paragraph at `:191-195` — "`desktop-screen-spec` 18 …, `gateway-worker-ticket` 26, `report-decision` 13, `unchanged-backlog` 53 … none is dropped outright" | totals dated 2026-08-23, now superseded by the 19/21/75 classification | +6 lines, and the 2026-08-23 totals annotated as superseded in the same edit |

Total: **1 file, ~30 lines**. No `.cs`, `.csproj`, `.bicep`, `.ps1` or workflow file
is touched, and no new `.md` is created anywhere.

### Measured board-state inventory (not a repository diff)

`list_items --group EPIC-014`, run 2026-08-24, returns **exactly 19 tickets**, and
their per-area distribution already matches the body's step 3 figure for figure:

| Fork area | Count | Board ids |
| --- | --- | --- |
| `automation-integrations` | 2 | `AUTO-001`, `AUTO-002` |
| `case-reference-workflow` | 2 | `CASE-001`, `CASE-002` |
| `documents-reports` | 3 | `DOCS-001`, `DOCS-002`, `DOCS-003` |
| `desktop-ui` | 1 | `DUI-017` |
| `engineering-assessment` | 2 | `ENG-001`, `ENG-002` |
| `intake-processing` | 7 | `INTK-001` … `INTK-007` |
| `platform-operations` | 2 | `PLAT-028`, `PLAT-029` |
| `delivery-repository`, `mail-communications`, `pr-review` | 0 | — |

Every one of the 19 already carries `EPIC-014` as its **only** group, `status`
`backlog`, and labels that include `upstream-carryover` plus `upstream-<ID>`; none
carries a `desktop-conversion`, `plan-NN`, `phase-N` or `tier-n` label. `blocks` is
not returned by `list_items` summaries and is therefore **not** yet verified — that
is step 11's `get_item` pass, and it is the one part of the audit no shortcut covers.

## Approach

Execute the ticket as an **audit plus a bounded delta**, not as a creation pass. The
19 imports already exist (measured above); `create_items` is ungated, so a second
creating pass would duplicate the batch rather than fail, and a duplicate backlog row
is worse than a missing one because nothing flags it. The chosen approach is
therefore: read the corrected classification from group `EPIC-014` and the
authoritative join table from `HZN-001/board-conventions.md`, `get_item` all 19
imports against the six-point shape check of step 11, `get_item` the 21 amend-list
board tickets and *report* what is missing rather than compensating with a new ticket,
re-run the classification against the upstream head that is current at execution time,
and create **only** what that delta opens. Then make the five carry-over document
edits in one commit.

The rejected alternative is the one the earlier draft of this ticket carried: a flat
recreation of the triage table's `desktop-screen-spec`/`gateway-worker-ticket`/
`report-decision` rows — about 57 tickets. It is rejected because fifteen of those ids
are already delivered by named seeded tickets, two are already merged into the fork's
`main`, one is moot with the Razor front end, two are decisions another ticket owns,
and the remaining nineteen already exist. It would have produced roughly twenty
duplicates competing for the same code under different acceptance criteria.

## Governing docs

The ticket's `refs` is **empty** and `docs_todo: true` — confirmed by
`get_doc_gates FND-022`, which reports `refs: []` and `docs_todo: true`. Profile
`chore` has no `leave-backlog` boundary at all on this board, so `docs_todo` is not
satisfying a gate here; it is an honest statement that no `docs/(prd|frd|adr)`
document is implemented by this work.

> **New ADR — none, and that is the correct answer.** Board mechanics are not a
> durable technical/architectural product decision, and `AGENTS.md` § ADR conventions
> is explicit that documentation rules and process are **not** ADRs. No entry in the
> reserved block ADR-0100…ADR-0110 governs this ticket, and none will be authored for
> it. Nothing in this plan may claim to meet an ADR that does not exist.

Because there is no governing document, the programme-level authorities that **do**
bind today are listed here with the step that satisfies each. `kanmer-review` checks
this table against the diff.

| Authority | Requirement | Met by |
| --- | --- | --- |
| L-05 (`docs/desktop/README.md` § Locked decisions) | The Kanmer board is the single work register; the open upstream board is triaged in area 01 | Steps 1–5, 11–14 |
| D-001 (decided 2026-08-23) | The fork becomes the single release source; an upstream row not carried before the freeze is **lost**, not deferred | Steps 5, 12; the unarrived list handed to [[FND-051]] (plan handle `DSK-01-13`) |
| D-004 (operator, 2026-08-24) | `OPS-10` acceptance folds into the desktop pilot approval, so upstream `TICK-001` stays dropped and no ticket is imported for it | Step 5's drop list, one operator-decision row |
| Send-to-AI recorded exclusion (operator, 2026-08-24) | Upstream `TICK-102` is a recorded exclusion with a reactivation condition, **not** an open question; no `open-questions` document, no speculative ticket, no reopening of `reuse-map.md:38` | Step 12 |
| Group document `HZN-001/board-conventions.md` § *Upstream ids versus board ids* | A bare `<PREFIX>-<nnn>` is a fork board id; an upstream id is written `upstream <ID>` or `upstream <ID> (board [[<board-id>]])`; the 19-row join table is authoritative over step 3 | Steps 1, 3, 14 and every id written in this plan, its proof and its scratch |
| Group `EPIC-014` body | The corrected 2026-08-24 classification against upstream head `a5b28111` (114 open non-archived tickets) is the register of what is imported | Steps 1, 3, 8, 11 |
| Proposal § 13.11 | Post-alpha capability rows are not smuggled into parity scope | Step 5's 58 post-alpha drops |
| Proposal § 25 | Ticket structure — the provenance block on every import | Step 11's body check |
| `docs/desktop/00-governance-and-workflow/README.md` § Kanmer board shape | The sixteen fork areas and their prefixes | Step 1's `list_board` |
| `AGENTS.md` § Repository task workflow step 4 | A simplification pass over this branch's own diff, recorded under a dated heading in this plan | § Simplification pass below |
| `AGENTS.md` § Repository task workflow step 5 | Review by an agent that did not implement | Routing → reviewer |
| `AGENTS.md` § New Markdown placement | No new `.md` outside `docs/(prd\|frd\|adr\|design\|desktop)` | Guardrails; the one edited file is already under `docs/desktop/` |

## Routing

Copied from the ticket body's `## Routing` block, which
`docs/desktop/00-governance-and-workflow/README.md` § Ticket template makes mandatory
in the plan document.

- **Subagent**: `pegasus-parity-researcher` — `.codex/agents/pegasus-parity-researcher.toml`
  (evidence gathering; **the parent session performs the board writes**, because the
  researcher is read-only and must not call `create_item`).
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `kanmer-tickets`
  (`.grok/skills/kanmer-tickets/SKILL.md`) → `kanmer-groom`
  (`.grok/skills/kanmer-groom/SKILL.md`).
- **MCP**: Kanmer — `get_status`, `list_board`, `list_groups`, `get_group`,
  `get_group_doc`, `list_items`, `search_items`, `get_doc_gates`, `create_item`,
  `create_items`, `update_item`, `link_items`, `set_ticket_doc`, `append_scratch`,
  `take_ticket`, `move_item`. No Azure MCP. No Microsoft Learn.
- **Kanmer pipeline** for profile `chore`: `kanmer-plan` → `kanmer-execute` →
  `kanmer-verify` → `kanmer-closeout`. Gated boundaries confirmed by
  `get_doc_gates FND-022`: `leave-preparing` (`plan`, `questions-resolved`) and
  `enter-done` (`proof`, `questions-resolved`). Call `get_doc_gates FND-022` before
  every move and cross at most one gated boundary per move.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement
  (`AGENTS.md` § Repository task workflow step 5).

## Steps

These refine the ticket body's fifteen implementation steps: same order, same
ownership, same file paths. They add the *how* the body leaves out and the measured
current values a step must be checked against.

1. **Orient, then take.** Read `upstream-kanmer-carryover.md` in full — especially
   § Disposition categories (`:58-65`) and § Recreation rule (`:67-75`). Then
   `get_group EPIC-014` and read its body: that is the corrected classification this
   ticket executes. Then `get_group_doc HZN-001 board-conventions.md` and read
   § *Upstream ids versus board ids* — it is authoritative over step 3, and where the
   two disagree, step 3 is corrected to it. Then
   `docs/desktop/00-governance-and-workflow/README.md` § Kanmer board shape. Call
   `get_status`, then `list_board` (expect the sixteen fork areas), then
   `get_doc_gates FND-022`, then `take_ticket`.
2. **Establish the body source and record the head.** Clone the upstream board
   read-only **outside** this working tree:
   `git clone --branch kanmer-board --single-branch <upstream url> <temp dir>`, or read
   with `git --git-dir=<clone>/.git show <head>:.kanmer/...`. Record the SHA and its
   date. Two heads are already on record: `4694067` (2026-08-23 triage table) and
   `a5b28111` (2026-08-24 coverage pass and the 19 imports). Read the head that is
   current when this ticket runs. **Never** add upstream as a writable remote here and
   never push to it — note that `git remote -v` in this repository currently returns
   `origin` only, so nothing is configured that could accidentally push.
3. **Hold the join table, do not recompute it.** The 19 rows in the body's step 3 are
   the whole import set for head `a5b28111`, and the measured board state agrees
   (see the board-state inventory above). Every id before an arrow is an **upstream**
   id, every id after it is a **fork board** id, and the two spaces overlap without
   corresponding. The joins that bite: board [[CASE-001]] is upstream `CASE-021`, a
   live production defect, while upstream `CASE-001` is the step 7 drop with no fork
   ticket; board [[DOCS-001]] is upstream `DOCS-001` **by coincidence only** and still
   gets the full form every time; board [[INTK-001]]…[[INTK-007]] are upstream
   `INTK-002`, `INTK-003`, `INTK-026`, `INTK-027`, `INTK-031`, `INTK-032`, `INTK-033`
   — shifted by one for the first four and not at all for the last three, with no
   formula; board [[PLAT-028]] and [[PLAT-029]] are upstream `PLAT-032` and `PLAT-038`.
4. **Confirm the 21 amendments; import none of them.** `get_item` each of the 21
   board tickets the body names by plan handle (`DSK-01-09` — this ticket — through
   `DSK-08-13`) and confirm the added acceptance line **and** its delivering
   implementation step are both present. Record any that is missing either as a
   **finding for the board owner in this plan's Risks section**; do not import the
   upstream row to compensate, and do not edit another agent's ticket from here. Five
   upstream ids are both imported and named in an amendment — upstream `DOCS-001`
   (board [[DOCS-001]]), upstream `TICK-208` (board [[DOCS-003]]), upstream `ENG-014`
   (board [[ENG-001]]), upstream `ENG-015` (board [[ENG-002]]) and upstream `CASE-022`
   (board [[CASE-002]]) — and are counted once, in the import list.
5. **Record the 75 drops in this document; recreate none.** The breakdown reconciles:
   58 post-alpha capability rows (proposal § 13.11 — their register stays
   `upstream-kanmer-carryover.md` and their capability rows stay in
   `docs/capabilities.md`) + 13 covered by a named seeded ticket + 2 already merged
   into the fork's `main` (upstream `PR-003`, upstream `PR-026` — step 6) + 1 moot with
   the Razor front end (upstream `CASE-001` — step 7) + 1 operator decision with no
   fork ticket either way (upstream `TICK-001`, D-004) = **75**. The 13 coverage
   mappings, every left-hand id an **upstream** id and every right-hand handle a
   seeded fork ticket: upstream `AUTO-006` and upstream `AUTO-007` → `DSK-05-19` plus
   `DSK-03-15`; upstream `PLAT-005` → `DSK-05-22`; upstream `PLAT-023` → `DSK-05-20`;
   upstream `PLAT-025`, `PLAT-026`, `PLAT-027` → `DSK-05-19`; upstream `PLAT-029` →
   `DSK-06-04` (**not** board `PLAT-029`, which is the import of upstream `PLAT-038`);
   upstream `PLAT-035` → `DSK-10-18`, which is board [[PLAT-018]]; upstream `PLAT-036`
   → `DSK-10-14` and `DSK-10-16`, mirrored by `DSK-11-09`; upstream `INTK-019` →
   `DSK-05-11`; upstream `CASE-012` and upstream `UICASE-001` → `DSK-05-02`,
   `DSK-05-03`, `DSK-05-05`, `DSK-05-06`. Resolve each handle to its board id with
   `search_items` for the handle string before writing it — never guess the mapping.
   Then add `upstream:<ID>` as a **provenance line** to each covering board ticket
   (for example `upstream:CASE-012` on the board ticket whose title begins
   `DSK-05-03 ·`) with `update_item`, so the coverage is discoverable from the covering
   ticket. Note the distinction that is easy to get wrong: these provenance writes are
   permitted, while the 21 amend-list tickets of step 4 are **read only**.
6. **Resolve every recorded commit or PR against the fork's `main`.** For each
   `gateway-worker-ticket` or `report-decision` row whose body names a commit or PR,
   run `git merge-base --is-ancestor <commit> main; echo $?`. Exit 0 means the upstream
   board status is lagging, not that work is outstanding: **annotate the carry-over row
   instead of holding a ticket**. Two are already confirmed — upstream `PR-003` at
   `8124ae2a` (its required sentence is at `docs/frd/frd-11-*.md:26`) and upstream
   `PR-026` at `4d00c3b7` (the design re-entry record at `docs/design/README.md:840-851`).
   This is a **general rule**: upstream `PLAT-039`'s Box token-renewal fix is one
   instance of it and arrives with the first sync ([[FND-023]], plan handle
   `DSK-01-10`), not an exception to it. Record every commit checked and its result
   here.
7. **Drop upstream `CASE-001` — and read the id twice.** Board [[CASE-001]] is a
   different ticket: it is the import of upstream `CASE-021`, a live production defect
   blocking `FEAT-005`, `GWY-006`, `FEAT-001` and `DUI-006`, and nothing in this step
   touches it. Upstream `CASE-001` has **no fork ticket** and is not to be given one:
   it is moot with the Razor front end (four dead `TempData` writes in page models the
   `DSK-05-26` cut list deletes; the operator-facing message survives as data via the
   Received item's Case tab). The drop still needs operator sign-off — obtain it and
   record it here. Confirm no ticket exists for upstream `CASE-001` with
   `search_items` for `upstream:CASE-001` and **read the `title` of every hit**; never
   `search_items` for a bare `CASE-001`, which matches the live import and would make a
   correct board look wrong.
8. **Take the area mapping from `EPIC-014` and step 3, not from the 2026-08-23 "Fork
   area" column**, which is wrong in at least two places: upstream `DELIV-006` (board
   [[DUI-017]]) belongs in `desktop-ui` beside its consumer, not in
   `delivery-repository`; and upstream `TICK-054` is not a `gateway-worker-ticket` at
   all (step 10). The measured board state already agrees with the corrected mapping.
9. **Duplicate-check before any write.** `search_items` for the **qualified** form of
   the upstream id (for example `upstream:INTK-027`, whose import is board
   [[INTK-004]]) and for a distinctive phrase from the title. A bare-id search is safe
   only where no board ticket holds that number, which is why a bare `CASE-001` or
   `DOCS-001` search must never be used as the duplicate check. Where a ticket already
   exists — which, for all 19, it does — `update_item` it; never create a second.
10. **Re-file the upstream `TICK-054` row.** `upstream-kanmer-carryover.md:165` reads
    `gateway-worker-ticket (provider port exists, unavailable by default) | 07 (mail)`.
    That is wrong: upstream `TICK-054` is post-alpha capability `MAIL-13`, the plan set
    excludes it three times (`docs/desktop/07-integrations/README.md:102`, `DSK-07-03`'s
    trap, `DSK-07-11` step 2), the Inbox spec and endpoint map carry no provider-state
    mutation, and production activation needs an Entra `Mail.ReadWrite` approval nobody
    holds. The folder-move port the parenthetical describes belongs to capability
    `MAIL-07` and is already carried by `DSK-03-12`, `DSK-07-03` and `DSK-05-10`.
    Re-file the row as `unchanged-backlog | —` in step 15(d) and confirm no ticket was
    imported for it.
11. **Audit the existing batch; create only the later head's delta.** Run
    `list_items --group EPIC-014` (expect the 19 measured above), then `get_item` on
    each and confirm all six points: `title` = `upstream:<ID> · <upstream title>`;
    `area` and `profile` as step 3 names; `labels` = the upstream labels verbatim **+**
    `upstream-carryover` **+** `upstream-<ID>` **+** the 2026-08-23 triage disposition
    where the table records one (14 of the 19 carry one — the five that carry none are
    upstream `ENG-014`, `ENG-015`, `INTK-026`, `INTK-031`, `INTK-032`) **+**
    `needs-operator` where an operator step is required, and **no** `desktop-conversion`,
    `plan-<NN>`, `phase-<N>` or `tier-<n>` label; `groups` = `["EPIC-014"]` and nothing
    else; `body` = the upstream body **verbatim** plus the provenance block (upstream
    id, upstream area, disposition, target area plan, link to that plan's `README.md`)
    plus the step 3 scope note where one is given (upstream `PLAT-032` roster items 1–5
    only; upstream `TICK-018` the two named gaps only; upstream `CASE-021` and upstream
    `INTK-033` verify-after-sync); `status` unset and `links` empty. **`blocks` is
    already wired and must not be removed** — 18 of the 19 block the seeded tickets they
    gate, and only board [[INTK-005]] is deliberately empty because a survey gates
    nothing. `blocks` is the one field `list_items` does not return, so this `get_item`
    pass is the only place it is checked. Never edit an id inside an imported ticket's
    `### Upstream ticket <ID> (verbatim)` block — that text is a quotation and its ids
    are upstream ids by definition. Then re-run the classification against the step 2
    head: triage every upstream ticket opened, reopened or un-archived since
    `a5b28111` into import, amend or drop by the rules of steps 3–5, and create **only**
    the imports that delta produces — `create_items`, **at most 6 entries per call**,
    checking each `{ok, item|error}` and retrying failures, with the same title, area,
    profile, label, group, body and provenance shape as the 19. The upstream id goes in
    the **title and labels only**: `refs` accepts nothing but repository-relative paths
    to documents that exist, so a `refs` entry of `upstream:INTK-027` fails the whole
    `create_items` entry. Extend step 3's join table with each new
    `upstream <ID> → board <board-id>` row and record the head, the delta and every id
    added.
12. **Re-check the in-flight rows, and record the standing exclusion.** At the step 2
    head, read and confirm scope for: upstream `INTK-033` (board [[INTK-007]]) — at
    `review` upstream on the unmerged branch `task/intk-033-triage-from-intake`, so
    [[FND-023]]'s pinned range does not bring it; verify at sync and carry the full fix
    if it has not merged. Upstream `CASE-021` (board [[CASE-001]]) — at `implementing`
    upstream on `task/case-021-observed-images` with a complete plan, so verify after
    sync; porting it fresh would duplicate and conflict. Upstream `TICK-102` /
    capability `AI-09` (Send to AI) — a **recorded exclusion with a reactivation
    condition**, verified against the code on 2026-08-24
    (`src/Pegasus.Web/AiWork/SendToAi.cs:12` defines `Features:SendToAi`; `:35-42`
    refuse to compose outside the `DevelopmentOffline` runtime profile;
    `src/Pegasus.Web/Program.cs:104-110` permits that profile only in Development —
    re-verified in this plan's own reading of `Program.cs`). Keep it in the re-check
    list with its reactivation condition (the separate non-preview transport decision
    at `docs/capabilities.md:269`); file **no** speculative ticket, do **not** reopen
    the `reuse-map.md:38` `AiWork/` row, and create **no** `open-questions` document
    for it on any ticket. Record the status of upstream `PLAT-041`, `DOCS-013`,
    `ENG-014` and `ENG-015` here so [[FND-051]] (plan handle `DSK-01-13`) picks them up.
13. **Group hygiene.** No conversion `EPIC-0xx` and no `HZN-0xx` belongs on an
    imported ticket; `EPIC-014` is the one group they carry, and the measured board
    state already satisfies this for all 19. Confirm `docs_todo: true` on any imported
    `feature` ticket with no existing governing document — `feature` is the only
    profile with a `leave-backlog` gate. Where an upstream ticket already names an
    existing `docs/adr/**` or `docs/frd/**` path, `refs` carries it and the path must
    exist; a non-existent `refs` path fails the whole entry.
14. **Verify and record.** `list_items --group EPIC-014` against step 3 plus the step
    11 delta; `list_items` per fork area against the per-area figures measured above
    (2 / 2 / 3 / 1 / 2 / 7 / 2, and **zero** in `delivery-repository`,
    `mail-communications` and `pr-review`); `get_item` on five imports to confirm the
    body matches upstream and the provenance block names the upstream id in full.
    Record the counts, the five spot-checks, the step 6 commit results, the step 12
    statuses and the step 11 audit result in the `proof` document.
15. **One commit to the carry-over document, carrying all five edits.** At the
    measured positions in the surface-area inventory above:
    (a) a dated "Recreated on the fork board" line — the date, the upstream board
    commit the bodies came from, the 19 imported / 21 amended / 75 dropped figures and
    the fork board group `EPIC-014` — placed with the 2026-08-23 totals paragraph at
    `:191-195` and annotating those totals as superseded;
    (b) replace `:69-70` so the upstream ID is carried in the ticket **title**
    (`upstream:<ID> · <title>`) and **labels**, never in `refs`, which holds only
    existing repository paths;
    (c) add the five post-triage rows — upstream `DOCS-013`, `ENG-014`, `ENG-015`,
    `INTK-034`, `INTK-035` — with their dispositions, and state that the table held 109
    rows against 114 open tickets at head `a5b28111`;
    (d) re-file `:165` as `unchanged-backlog | —` with the step 10 reason;
    (e) add the sentence to § Disposition categories restricting `unchanged-backlog` to
    rows that **have** a `docs/capabilities.md` row. **This step is the single owner of
    that sentence.** If it is already present when this step runs, leave it and record
    that it was — do not write a second copy. Then write `plan` and `proof` with
    `set_ticket_doc`, call `get_doc_gates FND-022`, and move the ticket on.

## Verification

`proof` is produced from the outputs below, in this order. Evidence tier: **Tier 1 —
Static/build/architecture** (`docs/engineering.md` § Required evidence tiers) — this
proves the register is complete and duplicate-free, and nothing about any carried-over
defect being fixed.

- `list_items --group EPIC-014` — expected: the 19 `upstream:` tickets of step 3,
  matching one for one, plus only whatever step 11's re-run added for a later head.
  Baseline measured 2026-08-24: exactly 19.
- `get_group_doc HZN-001 board-conventions.md` § *Upstream ids versus board ids*,
  compared line by line with step 3 — expected: the same 19 upstream↔board pairs; any
  disagreement resolved in favour of the group document.
- `list_items` per fork area — expected: `automation-integrations` 2,
  `engineering-assessment` 2, `platform-operations` 2, `intake-processing` 7,
  `documents-reports` 3, `case-reference-workflow` 2, `desktop-ui` 1 `upstream:`
  ticket; `delivery-repository`, `mail-communications`, `pr-review` **zero**.
- `get_item` on all 19 — expected: `upstream-carryover` and `upstream-<ID>` present;
  no `desktop-conversion`/`plan-NN`/`phase-N`/`tier-n` label; `EPIC-014` the only
  group; `status` unset; `links` empty; `blocks` non-empty except board [[INTK-005]].
- `search_items` for `upstream:CASE-001`, `upstream:TICK-001`, `upstream:TICK-054`,
  `upstream:PR-003`, `upstream:PR-026`, `upstream:AUTO-006`, `upstream:AUTO-007`,
  `upstream:CASE-012`, `upstream:UICASE-001`, `upstream:TICK-206`, `upstream:TICK-214`,
  `upstream:TICK-102` — expected: **no ticket whose title begins with any of those
  handles**. Read the `title` of every hit: `search_items` is full-text over id, title,
  body and labels, so this ticket's own body matches several of these strings and a
  non-empty result set is not a failure.
- `git merge-base --is-ancestor 8124ae2a main; echo $?` and
  `git merge-base --is-ancestor 4d00c3b7 main; echo $?` — expected `0` for both.
- `get_item` on five imports — expected: each body matches its upstream body and states
  the upstream id, disposition and target area plan.
- `get_doc_gates` on one imported `feature` ticket — expected: the `leave-backlog`
  `governing-doc` requirement satisfied by `docs_todo` or a real `refs` path.
- `grep -c "docs/capabilities.md row" docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md`
  — expected: exactly **1**.
- `pwsh ./scripts/Test-DocumentationLinks.ps1` — expected: exit code 0.
- `pwsh ./scripts/Test-MarkdownPlacement.ps1` — expected: exit code 0 (the edited file
  is already under `docs/desktop/`, an allowed root).

## Risks / open questions

- **Risk — a second creating pass duplicates the batch.** `create_items` is ungated
  and duplicates rather than fails, and the 19 imports **already exist** (measured).
  *Mitigation:* step 11 is an audit; the only creation is the step 2 head's delta, and
  step 9's qualified-form `search_items` runs before every write.
- **Risk — a bare id in the wrong namespace deletes real work.** Board [[CASE-001]] is
  upstream `CASE-021`, a live production defect blocking four tickets; upstream
  `CASE-001` is the step 7 drop. *Mitigation:* the absolute id rule of
  `HZN-001/board-conventions.md`, applied to this plan, the proof and the scratch;
  never `search_items` a bare `CASE-001` or `DOCS-001`.
- **Risk — the stale totals paragraph contradicts the new figures.**
  `upstream-kanmer-carryover.md:191-195` still reads
  "`desktop-screen-spec` 18 …, `gateway-worker-ticket` 26, `report-decision` 13,
  `unchanged-backlog` 53 … none is dropped outright". After edit 15(a) the document
  would carry two incompatible totals. *Mitigation:* annotate the 2026-08-23 totals as
  superseded in the same edit — step 15(c) already requires stating that the table held
  109 rows against 114 open tickets, so this is within scope, not new scope.
- **Risk — `refs` rejects `upstream:<ID>`.** The carry-over document's § Recreation
  rule is wrong on exactly this point and a `refs` entry that is not a repository path
  fails the whole `create_items` entry. *Mitigation:* edit 15(b) is the correction, and
  step 11 spells out that the upstream id lives in the title and labels only.
- **Risk — the upstream board keeps moving.** This triage is dated; a re-triage is a
  new ticket, never a rewrite. *Mitigation:* step 2 records the head; step 11 bounds
  creation to that head's delta.
- **Open question — do all 21 amend-list board tickets actually carry their added
  acceptance line?** Not verifiable before execution, because it needs 21 `get_item`
  calls against tickets other agents own. **Answered by:** the implementer at step 4,
  and reported here as a finding rather than compensated for with an imported ticket.
  This is a step, not a blocker, so it is deliberately **not** an `open-questions`
  item: gating `leave-preparing` on it would stop the very work that answers it.
- **Open question — operator sign-off for dropping upstream `CASE-001`.**
  **Answered by:** the operator, at step 7, during implementation. Recorded here rather
  than as an unticked `open-questions` box for the same reason: the sign-off is obtained
  *inside* the ticket, and blocking `leave-preparing` on it would make the ticket
  unstartable.
- **Not an open question — upstream `TICK-102` (Send to AI).** A recorded exclusion
  with a reactivation condition, settled by the operator on 2026-08-24. Do not re-open
  it, do not raise it again, and create no `open-questions` document for it on any
  ticket.
- **Not an open question — upstream `TICK-001` / `OPS-10`.** Settled by D-004: the
  acceptance folds into the desktop pilot approval, upstream `TICK-001` stays dropped,
  and no ticket is imported for it.
- **Scope boundary, not a question — the § Disposition categories sentence.** This
  ticket's step 15(e) writes it once. The imported `upstream:INTK-031` ticket (board
  [[INTK-005]]) annotates its own triage row and *cites* the sentence; it does not
  write it. Two tickets writing one sentence is a stop condition.

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over this
branch's own diff before the PR, recorded here under a dated heading. Expected result
for this ticket: `n/a — docs-only`, since the only repository diff is the single edit
to `docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md`. Board writes
are not a repository diff and are not in scope for the pass._
