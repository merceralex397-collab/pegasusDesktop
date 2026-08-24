# Plan — FND-004: Verify the 208 seeded conversion tickets, keep the plan-handle join accurate, and verify the carry-over batch

**Diff estimate: ~1 repository file, ~6 lines** — the § 3 board-shape rows in `docs/desktop/00-governance-and-workflow/README.md` — **plus board writes: 0 ticket creations expected, and up to 10 `update_item` area changes if the operator picks outcome (b).**

Derived from the measured inventory below, in which every count was actually
re-derived on 2026-08-24 rather than copied. The zero in "0 ticket creations
expected" is a measurement, not an assumption: the board already holds a ticket
for every one of the 208 plan rows.

## Measured board-and-file inventory

`chore` owes no `files` document, so the surface area is measured here.
Everything below was read on 2026-08-24 from the live board at
`.worktrees/kanmer/.kanmer` and from the working tree at `origin/main`
`191ddf3342…`. **Re-derive all of it at execution; do not copy these figures
into the proof.**

| Thing | Measured 2026-08-24 |
| --- | --- |
| Plan rows, `grep -c '^| DSK-' docs/desktop/*/README.md` | 13, 12, 16, 18, 15, 26, 16, 19, 19, 16, 18, 9, 11 for plan areas 00…12 — **total 208**, exactly the figure the body states |
| Tickets on the board | **229** (`get_status` → `counts.byStage.backlog: 229`, every other stage 0, `archived: 0`, `taken: 0`) |
| Tickets carrying a `DSK-<area>-<nn>` handle | **209**, every one unique — no handle appears twice |
| Plan handles present on the board | **all 208**; set-difference against the plan rows is empty in that direction |
| Board handles absent from the plans | exactly **one** — `DSK-01-13`, carried by [[FND-051]] "Standing later upstream syncs up to the D-001 freeze", created 2026-08-24T14:04:38Z. Not a plan row and **not a defect**: it is the standing-cadence follow-up that [[FND-023]] (plan handle `DSK-01-10`) specifies |
| Tickets with **no** handle | **20** — the 19 upstream carry-over imports plus [[FND-052]] ("BOARD · Groom the seeded board…", labelled `board-hygiene`, created 2026-08-24T14:04:38Z) |
| `label: desktop-conversion` | **210** = 208 plan rows + [[FND-051]] + [[FND-052]] |
| Withdrawn handles `DSK-09-07`, `DSK-09-09` | **no ticket carries either**, and neither appears in the plan rows (area 09 has 16 rows = `DSK-09-01`…`DSK-09-18` minus those two) |
| `plan-NN` label counts | 00:14, 01:13, 02:16, 03:18, 04:15, 05:26, 06:16, 07:19, 08:19, 09:16, 10:18, 11:9, 12:11 — the two surpluses are `plan-00`+[[FND-052]] and `plan-01`+[[FND-051]] |
| Per area | `desktop-foundation` plan-00 14 / plan-01 13 / plan-02 16 / plan-04 9; `gateway-api` plan-03 18 / plan-04 6; `desktop-features` plan-05 26 / plan-07 19; `platform-operations` plan-10 18 / plan-11 9; `desktop-ui` plan-06 16; `release-desktop` plan-09 16; `testing` plan-08 19; `agent-tooling` plan-12 11 |
| `EPIC-014` membership | **19** tickets, split `automation-integrations` 2, `engineering-assessment` 2, `platform-operations` 2, `intake-processing` 7, `documents-reports` 3, `case-reference-workflow` 2, `desktop-ui` 1 — and `delivery-repository`, `mail-communications`, `pr-review` **zero**. Exactly the split the body states. Each carries `EPIC-014` as its only group, with no conversion `EPIC` and no `HZN` |
| `docs/desktop/00-governance-and-workflow/README.md` | 431 lines; the § 3 board-shape area table is `:228-248` |
| `HZN-001/board-conventions.md` | holds § 1 *Upstream ids versus board ids* (the authoritative 19-row join). **§ 2 "Deviation to note" does not exist yet** — [[FND-003]] (plan handle `DSK-00-03`) step 7a writes it; this ticket cites it and must confirm it is there before step 9 relies on it |

The measured position is therefore: **the seed is complete and correct.** Steps
2–8 confirm it; the real work of this ticket is the join (step 4) and the
board-shape reconciliation (step 9).

## Approach

Verify by re-derivation, never by comparison against a written figure — including
the figures in this plan. Every number in the ticket body and in the carry-over
document has a stale ancestor somewhere on this board, and the failure mode this
ticket exists to prevent is a proof that reproduces a stale count with
confidence. So each step names the MCP call that produces its number.

Absence is asserted on **titles**, never on hit counts. `search_items` is
full-text over id, title, body, labels and assignee, so searching `DSK-09-07`
legitimately returns this ticket and [[REL-007]] (plan handle `DSK-09-08`),
both of which name the withdrawn handle in prose. The check is "no hit whose
title begins `DSK-09-07 ·`", and every absence check below is written that way.

The rejected alternative for step 7 was a blanket `create_items` pass "to be
safe". `create_item`/`create_items` are ungated (`AGENTS.md:14`), so a second
pass duplicates every handle instead of failing — and the measured gap is zero,
so the safe pass would be pure damage.

## Governing docs

`refs` is empty and `docs_todo: true` — confirm with `get_doc_gates FND-004`
before moving.

> **No ADR will govern this ticket, and the repository has already decided
> that.** `docs/adr/README.md:47,52` relocates ADR-0010 and ADR-0023 to
> `AGENTS.md` / `docs/index.md` because "governance is not an ADR"; board
> bookkeeping is governance. This plan is written to **L-05** as recorded in
> `docs/desktop/README.md` § Locked decisions, to
> `docs/desktop/00-governance-and-workflow/README.md` § 3, and to the Kanmer
> group document `HZN-001/board-conventions.md`. The conversion ADR block
> ADR-0100…ADR-0110 (authored by [[FND-005]] (plan handle `DSK-00-05`),
> [[FND-006]] and [[FND-007]]) contains nothing this ticket depends on, and no
> `link_doc` is owed. Do not invent a governing ADR to fill this section.

| Authority | Requirement | Met by |
| --- | --- | --- |
| L-05 (`docs/desktop/README.md`) | The board is the executable form of the plan set — a plan row with no ticket is work that silently disappears | Steps 2–4, 7 |
| L-04 | Every ticket names its subagent, skills and MCP tools | Step 5 (the `## Routing` block check) |
| Plan 00 § 3 "Ticket template (proposal §25 → Kanmer documents)" | Which §25 section lands in which Kanmer document; `## Routing` is required in the plan document specifically | Step 5 |
| `docs/desktop/README.md` § "Ticket IDs in these plans are planning handles" | The plan handle goes into the ticket so plan and board stay joined | Step 4 |
| Proposal § 25 Ticket structure | The twelve-section ticket shape | Step 5 |
| `AGENTS.md:1-22` | `get_doc_gates` before every move, never `board.yml`; one gated boundary per move; unticked `open-questions/` items block | Steps 6, 11 |
| `AGENTS.md:14` | Creation in any stage is ungated | Steps 7, 8 (the no-duplicate rule) |
| `HZN-001/board-conventions.md` § 1 | A bare `<PREFIX>-<nnn>` is a fork board id; an upstream id is always `upstream <ID>`; the 19-row join is read, never computed | Steps 4, 8 |
| `scripts/Test-MarkdownPlacement.ps1:31` | New Markdown only under `docs/(prd|frd|adr|design|desktop)` and the other allowed roots | Step 4 (the join stays in Kanmer) |
| D-004 and the Send-to-AI exclusion (operator decisions, 2026-08-24) | Settled; not to be reopened as questions on this board | Step 8 (they are not carry-over defects) |

## Routing

Copied from the ticket body's `## Routing` block.

- **Subagent**: — (parent session); `pegasus-parity-researcher`
  (`.codex/agents/pegasus-parity-researcher.toml`, verified present) for the
  read-only cross-check of plan rows against board tickets.
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `kanmer-tickets`
  (`.grok/skills/kanmer-tickets/SKILL.md`) → `kanmer-groom`
  (`.grok/skills/kanmer-groom/SKILL.md`).
- **MCP**: Kanmer (`get_status`, `list_board`, `list_items`, `search_items`,
  `get_item`, `get_group`, `get_group_doc`, `create_items`, `update_item`,
  `link_doc`, `set_ticket_doc`, `get_doc_gates`, `take_ticket`,
  `append_scratch`, `move_item`).
- **Kanmer pipeline** for profile `chore`: `kanmer-tickets` → `kanmer-plan` →
  `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout`. Call
  `get_doc_gates FND-004` before every move; a move crosses at most one gated
  boundary.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement
  (`AGENTS.md` § Repository task workflow step 5).

## Steps

These refine the body's twelve implementation steps; order and ownership are the
body's.

1. **Orient.** Read the plan row and § 3 "Ticket template", then
   `docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md` (224
   lines; § "Triage table (109 open upstream tickets)" at `:77` and § "Code
   drift and the first sync" at `:197` — the 109 figure is the withdrawn one).
   Call `get_doc_gates FND-004`, then `take_ticket`. Confirm [[FND-003]] is done
   and that `HZN-001/board-conventions.md` § 2 exists before step 9 relies on it.
   **Never create a handle that already exists** — `search_items` for it first.
2. **Count the source of truth.** `grep -c '^| DSK-' docs/desktop/*/README.md`
   — expect 13, 12, 16, 18, 15, 26, 16, 19, 19, 16, 18, 9, 11 for plan areas
   00…12, total **208**. `DSK-09-07` and `DSK-09-09` are withdrawn: **no ticket
   carries either handle**. Both strings still appear in prose — in this step,
   and in [[REL-007]] (plan handle `DSK-09-08`) step 1, which records the
   withdrawal — and those two references are deliberate and correct. The check
   is that no *ticket exists under* either handle, not that the string appears
   nowhere.
3. **Count the board.** `list_items` with `label: "desktop-conversion"` — the
   measured value on 2026-08-24 is **210**, not 208, and the two extras are
   [[FND-051]] (`DSK-01-13`, the standing-sync follow-up specified by
   [[FND-023]]) and [[FND-052]] (`BOARD ·`, board hygiene, no handle). Report
   210 with its two-line explanation rather than reporting a 2-ticket surplus as
   a defect. Then per plan area, `list_items` with `label: "plan-00"` …
   `"plan-12"` and compare with step 2: measured 14, 13, 16, 18, 15, 26, 16, 19,
   19, 16, 18, 9, 11 — the two surpluses fall in `plan-00` and `plan-01` and are
   the same two tickets.
4. **Build the join.** For each plan area run `search_items` with the handle
   prefix (`DSK-05-`) and record `handle → board id → area → profile → groups`.
   Confirmed anchors for spot-checking the result:
   `DSK-00-03`→`FND-003`, `DSK-00-04`→`FND-004`, `DSK-00-05`→`FND-005`,
   `DSK-00-11`→`FND-011`, `DSK-01-09`→`FND-022`, `DSK-01-10`→`FND-023`,
   `DSK-02-01`→`FND-026`, `DSK-02-05`→`FND-030`, `DSK-04-01`→`FND-042`,
   `DSK-04-02`→`GWY-019`, `DSK-05-03`→`FEAT-003`, `DSK-07-11`→`FEAT-037`,
   `DSK-07-12`→`FEAT-038`, `DSK-07-14`→`FEAT-040`, `DSK-07-15`→`FEAT-041`,
   `DSK-09-01`→`REL-001`, `DSK-09-08`→`REL-007`, `DSK-09-17`→`REL-015`,
   `DSK-12-02`→`TOOL-002`, `DSK-12-04`→`TOOL-004`, `DSK-12-08`→`TOOL-008`.
   Write the table into this ticket with `set_ticket_doc`. **Practical note:**
   `set_ticket_doc` accepts the area's configured doc ids
   (`research`, `files`, `plan`, `checklist`, `open-questions`,
   `post-implementation-report`, `proof`) and rejects an unknown id *with the
   list of valid ids*; if `reference` is not accepted on this build, use
   `append_scratch`, which is never gated. Either way the join stays on the
   board: a new `.md` outside the allowed roots fails the CI `documentation` job
   (`scripts/Test-MarkdownPlacement.ps1:31`).
5. **Spot-check ten tickets** spread across areas with `get_item`. Each must
   have: the plan handle at the front of the title; the board area from § 2 of
   the seeding plan; the profile from its plan row; exactly one `EPIC-0xx` and
   one `HZN-0xx` group; labels `desktop-conversion`, `plan-<NN>`, `phase-<N>`
   and its tier label; `docs_todo: true` where the governing document is a
   conversion ADR/FRD that does not exist yet; and a body carrying `## Routing`
   plus numbered `## Implementation steps`. Note that the 19 `EPIC-014` imports
   deliberately break the "one EPIC and one HZN" rule — do not spot-check them
   here; step 8 covers them.
6. **Probe one gate.** `get_doc_gates` on any `feature` ticket — expect
   `leave-backlog: [governing-doc]`, satisfied by `docs_todo: true`, and
   `leave-preparing: [research, files, plan, checklist, questions-resolved]`.
   Record the output verbatim.
7. **Fill gaps only.** Measured gap on 2026-08-24: **zero** — every one of the
   208 plan rows has a ticket. If a later run finds one missing, create it with
   `create_items` in **batches of at most 6**, using the body template in plan 00
   § 3 and the row's own acceptance, verification, tier and routing columns.
   Check each entry's `{ok, item|error}` result and retry failures — a `refs`
   path that does not exist fails the whole entry. This is the only creation this
   ticket performs, and it covers conversion plan rows only.
8. **Carry-over count — verification only; create nothing.** Read
   `upstream-kanmer-carryover.md` **together with** `get_group EPIC-014`, whose
   corrected classification supersedes the 2026-08-23 triage table's
   dispositions and totals. That 2026-08-24 coverage pass reconciled the **114**
   open, non-archived upstream tickets at head `a5b28111` as **19 imported / 21
   amended / 75 dropped**: 19 + 20 amend-only upstream ids + 75 = 114, because
   `upstream DOCS-001`, `upstream TICK-208`, `upstream ENG-014`,
   `upstream ENG-015` and `upstream CASE-022` are each both imported and named in
   an amendment, and are counted once, in the import list. Confirm on the board
   with `list_items group: EPIC-014` then one call per fork area. Measured
   2026-08-24 and matching the body exactly: **19** `upstream:` tickets, each
   carrying `EPIC-014` as its only group with no conversion `EPIC` and no `HZN`,
   split `automation-integrations` 2, `engineering-assessment` 2,
   `platform-operations` 2, `intake-processing` 7, `documents-reports` 3,
   `case-reference-workflow` 2, `desktop-ui` 1, with `delivery-repository`,
   `mail-communications` and `pr-review` at zero. Confirm no upstream id appears
   twice, that the 233 `done` and 114 archived upstream tickets were not
   recreated, and that no amend-list or drop-list row was imported.
   `unchanged-backlog` is **not** a blanket exclusion — it is safe only for rows
   with a `docs/capabilities.md` row, and there is none for `upstream INTK-026`,
   `upstream INTK-031` or `upstream INTK-032`, so all three are **expected**
   imports rather than defects. The withdrawn figures are wrong and must never be
   restated: there are not 18 `desktop-screen-spec` tickets in `desktop-ui`, not
   39 across the domain areas, not 57 from 56 ids, and `upstream CASE-009` is
   **not** recreated twice — it is an amendment to [[FEAT-037]] (plan handle
   `DSK-07-11`) and [[FEAT-003]] (plan handle `DSK-05-03`), not two tickets.
   Re-derive every number from `list_items` before writing it down; do not copy a
   figure from any document, this plan included. Any mismatch is reported as a
   finding on [[FND-022]] — do **not** create, relabel or retitle a carry-over
   ticket here.
9. **Reconcile the plan-00 board-shape table with what was seeded — the whole
   table.** `sed -n '228,248p' docs/desktop/00-governance-and-workflow/README.md`
   says `desktop-foundation` holds "area plans 02 (and 00 governance tickets)",
   `gateway-api` "area plan 03, 04 (gateway side)", `desktop-features` "area
   plan 05 slices, 07 desktop side", and gives plan folders 10 and 11 no area at
   all. The seeded board says otherwise; re-establish each line with
   `list_items` before writing anything down. Measured 2026-08-24:
   - `desktop-foundation` holds plans **00, 01, 02 and the desktop half of 04**
     (`plan-00` 14 — 13 rows plus [[FND-052]]; `plan-01` 13 — 12 rows plus
     [[FND-051]]; `plan-02` 16; `plan-04` 9 = FND-042…FND-050);
   - `gateway-api` holds **plan 03 and the gateway half of 04** (`plan-03` 18;
     `plan-04` 6 = GWY-019…GWY-024);
   - `desktop-features` holds **plans 05 and 07 in full** (`plan-05` 26,
     `plan-07` 19 = FEAT-027…FEAT-045), of which ten are gateway- or build-side
     work: FEAT-027, FEAT-028, FEAT-029, FEAT-031, FEAT-034, FEAT-035, FEAT-037,
     FEAT-039, FEAT-042, FEAT-045;
   - `platform-operations` holds **plans 10 and 11** (`plan-10` 18, `plan-11` 9),
     a deliberate deviation recorded in the Kanmer group document
     `HZN-001/board-conventions.md` § 2 "Deviation to note", written by
     [[FND-003]] step 7a — cite that record; do not re-decide it, and confirm it
     is present before citing it.
   Two outcomes are honest, and **the choice is the operator's to make and this
   ticket's to record — never to guess**:
   **(a) amend the plan-00 table to match the seed** — `desktop-foundation` =
   "area plans 00, 01, 02 and the desktop half of 04", `gateway-api` = "area
   plan 03, 04 (gateway side)", `desktop-features` = "area plan 05 slices, area
   plan 07"; or
   **(b) move the ten gateway-side plan-07 tickets** to `gateway-api` with
   `update_item(area: "gateway-api")` on each — their `## Routing` blocks name
   `pegasus-gateway-dev` or `pegasus-release-packager` and their scope
   boundaries touch `src/Pegasus.Web`, `src/Pegasus.Contracts` and `eng/build/`,
   not `src/Pegasus.Desktop` — and amend the `gateway-api` row to "area plan 03,
   04 (gateway side), 07 (gateway side)", leaving the `desktop-features` row as
   it reads.
   Under **both** outcomes the `desktop-foundation` row is wrong as written and
   must be corrected. Record the chosen outcome, who chose it, its reason and its
   date here in this plan document and apply exactly that outcome; an unrecorded
   or assumed choice is a defect. Nothing is unworkable while it is open —
   `get_doc_gates` gives `desktop-features` and `gateway-api` identical gate
   rules — but an area-scoped sweep over `gateway-api` silently misses ten
   tickets until it is settled. `update_item(area: …)` moves the ticket's folder
   and keeps its id; it is **not** `move_item` and must not be attempted with
   one.
10. **Re-run steps 3 and 4** after any creation or area change so the counts and
    the join stay true, and append the new totals with `append_scratch`.
11. **Leave every ticket in `backlog`.** Seeding sets no status and this ticket
    calls no `move_item` on another ticket.
12. **Write the proof** as a `command-log` containing the final counts, the ten
    spot-checks, the carry-over count from step 8, the recorded board-shape
    outcome from step 9 and the `get_doc_gates` probe.

## Verification

Evidence tier 1 — Static/build/architecture (`docs/engineering.md:72-90`). Board
state and plan-row counts only; no application behaviour is claimed.

| Check | Expected |
| --- | --- |
| `list_items label: "desktop-conversion"` | 210 summaries, all `backlog` — 208 plan rows plus [[FND-051]] and [[FND-052]], each named |
| `grep -c '^| DSK-' docs/desktop/*/README.md` | 13 12 16 18 15 26 16 19 19 16 18 9 11 |
| `get_doc_gates <a feature ticket id>` | `leave-backlog` names `governing-doc`, satisfied by `docs_todo` |
| `search_items "DSK-09-07"`, then `"DSK-09-09"` | **no hit whose title begins `DSK-09-07 ·` or `DSK-09-09 ·`**. Read the `title` of every hit, never the count: both searches legitimately return results (this ticket, and [[REL-007]]) |
| `list_items group: "EPIC-014"` | exactly 19 `upstream:` tickets, each with `EPIC-014` as its only group, none created by this ticket |
| `list_items` per fork carry-over area | `automation-integrations` 2, `engineering-assessment` 2, `platform-operations` 2, `intake-processing` 7, `documents-reports` 3, `case-reference-workflow` 2, `desktop-ui` 1; `delivery-repository`, `mail-communications`, `pr-review` zero |
| `search_items "upstream:CASE-009"` | **no hit whose title begins `upstream:CASE-009 ·`** — same title-not-count shape as above |
| `sed -n '228,248p' …/00-governance-and-workflow/README.md` vs `list_items` per `plan-NN` | every row describes the areas the board actually holds, under the outcome recorded in step 9 |
| `pwsh ./scripts/Test-DocumentationLinks.ps1` | exits 0 (step 9 edits a repository file) |

## Risks / open questions

- **Restating a withdrawn figure.** The single likeliest defect. The 2026-08-23
  triage's 109 rows and its 57 / 56 / 18 / 39 / 53 counts are all withdrawn;
  `EPIC-014` carries the corrected 19 / 21 / 75. Mitigation: every step names
  the call that produces its number, and the acceptance criteria make
  re-derivation itself checkable.
- **Asserting absence from a hit count.** `search_items` is full-text, so "no
  results" can never prove a ticket does not exist. Mitigation: every absence
  check in this plan asserts on the `title` of each hit.
- **The board-shape outcome is the operator's** (step 9). It is recorded here,
  not decided by whichever agent starts first — and it does not block: gate
  rules are identical across the two candidate areas, so the ticket is workable
  while it is open. This is a recorded decision awaiting an owner, held in this
  plan; it is not written as an `open-questions` item, because nothing about it
  blocks `leave-preparing` and the body directs the record to the plan document.
- **The § 2 this step cites may not exist yet.** Measured 2026-08-24:
  `HZN-001/board-conventions.md` holds § 1 only. [[FND-003]] step 7a writes § 2;
  this ticket depends on it and step 1 checks for it. A scope boundary owned by a
  named ticket, not a question.
- **Three jobs plus a reconciliation in one row.** The plan is followed rather
  than split, per the seeding contract. A shortfall found in step 8 is raised as
  a finding on [[FND-022]] and never fixed by creating tickets here.
- **Upstream ids are not fork board ids.** `HZN-001/board-conventions.md` § 1
  holds the 19-row join; board `DOCS-001` and upstream `DOCS-001` are different
  tickets that happen to share a number. Read the table; never compute the
  mapping.
- **Two board-hygiene tickets already exist.** [[FND-052]] holds the grooming
  sweep for unrunnable verification commands and dangling links. Findings of that
  kind belong there, not in a widened FND-004.

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over
this branch's own diff before the PR, recorded here under a dated heading.
Expected outcome: `n/a — docs-only`._
