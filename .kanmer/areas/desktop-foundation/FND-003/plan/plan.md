# Plan — FND-003: Verify the seeded Kanmer board shape

**Diff estimate: ~1 repository file, ~6 lines — plus 4 Kanmer board writes that produce no repository diff.**

Derived from the measured inventory below, in which the gaps were actually
counted rather than assumed. Three of the four board writes are repairs of
things measured missing on 2026-08-24; the repository line is conditional on
step 10.

## Measured board-and-file inventory

`chore` owes no `files` document, so the surface area is measured here. Every
figure was read on 2026-08-24 from the live board at
`C:\Users\PC\Documents\GitHub\pegasusDesktop\.worktrees\kanmer\.kanmer`.

| Thing | Measured today | What this ticket does |
| --- | --- | --- |
| Areas (`list_board`) | **16**, exactly the ids and prefixes the plan names: `pr-review`/PR, `mail-communications`/MAIL, `automation-integrations`/AUTO, `documents-reports`/DOCS, `engineering-assessment`/ENG, `intake-processing`/INTK, `platform-operations`/PLAT, `delivery-repository`/DELIV, `case-reference-workflow`/CASE, `desktop-foundation`/FND, `gateway-api`/GWY, `desktop-ui`/DUI, `desktop-features`/FEAT, `release-desktop`/REL, `testing`/TEST, `agent-tooling`/TOOL | verify only — no gap found |
| Groups (`list_groups`) | **25**: `HZN-001`…`HZN-011` (11) and `EPIC-001`…`EPIC-014` (14) | verify; record `EPIC-014` as the deliberate addition (step 5a) |
| Group `context.md` | **24 of 25 present**; `EPIC-014` has **none** (`.kanmer/groups/EPIC-014/` holds `EPIC-014.md` only) | **write one** with `set_group_doc` (step 6) |
| `HZN-001/board-conventions.md` | exists; **one heading only** — `## Upstream ids versus board ids — read this before writing any id`. There is **no § 2 "Deviation to note"** anywhere under `.kanmer/groups/` (`grep -rn "Deviation to note"` returns nothing) | **add § 2** with `set_group_doc` (step 7a) |
| `EPIC-011` body `:9`, `EPIC-012` body `:9` | both end "Board area: `platform-operations` (PLAT) - see the deviation note in `board-conventions.md`" — an unqualified filename resolving to a document that does not hold such a note | **repoint** with `update_group` after § 2 exists (step 7b) |
| `docs/desktop/12-agent-tooling/board-conventions.md` | **does not exist** — that folder holds `README.md`, `skill-routing.md`, `skills.lock.draft.json`, `subagents.md`. No group body cites that repository path today | nothing; the body's example is illustrative, and the real dangling reference is the one above |
| Ticket stages (`get_status`) | `backlog: 229`, every other stage `0`; `archived: 0`, `taken: 0`, `warningsCount: 0`, `format: 3`, `boardSource: "file"` | verify only |
| `docs/desktop/00-governance-and-workflow/README.md` | 431 lines; § 3 "Kanmer board shape for the fork" area table at `:228-248` | at most +1 `Deviation:` line about the **group set**; the table's **rows** belong to [[FND-004]] (plan handle `DSK-00-04`) step 9 |

## Approach

Verify with the MCP read tools and repair only what is measurably missing —
never re-seed. `AGENTS.md:14` records that gates constrain `move_item` and
nothing else: creation in any stage is ungated, so a second `create_group` pass
would silently duplicate the group set rather than fail. The measured gaps are
therefore fixed with the narrowest write that closes each one: `set_group_doc`
for the two missing documents and `update_group` for the two dangling body
references. The rejected alternative was running `kanmer-setup` to "reconcile":
it is the right tool for a missing **area** (areas live in `board.yml`, not
behind a create call), but here no area is missing, and running it to fix a
missing `context.md` would touch board configuration this ticket has no mandate
over.

The § 2 repair is written into the **board** document, not the repository.
`AGENTS.md` § New Markdown placement and the body's own Guardrails both say
ticket- and board-transient material lives in Kanmer; and the body's step 7
forbids creating a repository file to satisfy a board reference. Writing § 2
into `HZN-001/board-conventions.md` also discharges an obligation two other
tickets already assume: [[FND-004]] step 9 is told to *cite* that section, and
`EPIC-011`/`EPIC-012` already point at it.

## Governing docs

`refs` is empty and `docs_todo: true` — confirm with `get_doc_gates FND-003`
before moving.

> **No ADR will govern this ticket, and that is a decision the repository has
> already taken.** `docs/adr/README.md:47,52` records ADR-0010 and ADR-0023 as
> relocated to `AGENTS.md` / `docs/index.md` because "governance is not an ADR".
> Board shape is governance. This plan is therefore written to **L-05** as
> recorded in `docs/desktop/README.md` § Locked decisions and to
> `docs/desktop/00-governance-and-workflow/README.md` § 3 "Kanmer board shape
> for the fork"; the conversion ADR block ADR-0100…ADR-0110 (authored by
> [[FND-005]] (plan handle `DSK-00-05`), [[FND-006]] and [[FND-007]]) contains
> nothing this ticket depends on, and no `link_doc` is owed. Do not invent a
> governing ADR to fill this section.

Because `refs` is empty, the authorities that bind today are these:

| Authority | Requirement | Met by |
| --- | --- | --- |
| `AGENTS.md:1-22` (machine-managed Kanmer block) | `get_status` first; `get_doc_gates <id>` before every move, never `board.yml`; a move crosses at most one gated boundary; if a ticket is in a group, its `context.md` is read before starting | Steps 2, 6, 8 |
| `AGENTS.md:14` | Gates constrain `move_item` only — creation in any stage is ungated | Step 1 (the no-re-seed rule) and the Guardrails |
| `AGENTS.md:6` | "never create, switch or push that branch yourself" — the board worktree and branch are managed by Kanmer | Guardrail: no hand-edit of `board.yml` |
| Plan 00 § 3 "Kanmer board shape for the fork" (`:228-262`) | 16 areas with those prefixes; one `HZN` per proposal phase; one `EPIC` per area plan; each group's `context.md` carries the constraint binding its batch | Steps 3, 5, 6 |
| Plan 00 § 4 Target state and exit gate | `get_status` shows the areas and groups; this is the Phase 0 governance exit-gate evidence | Steps 2–5, 11 |
| Proposal § 24 | The eleven phase titles the horizons carry | Step 5 |
| L-05 | The board is seeded by the implementing agent from the ticket tables in these plans | The whole ticket |
| L-04 | Every ticket names its subagent, skills and MCP tools — which is what the epic `context.md` routing table supplies | Step 6 |

## Routing

Copied from the ticket body's `## Routing` block.

- **Subagent**: — (parent session; the plan routes board setup to no subagent).
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `kanmer-setup`
  (`.grok/skills/kanmer-setup/SKILL.md`) → `kanmer-tickets`
  (`.grok/skills/kanmer-tickets/SKILL.md`).
- **MCP**: Kanmer (`get_status`, `list_board`, `list_groups`, `get_group`,
  `get_group_doc`, `set_group_doc`, `update_group`, `get_doc_gates`,
  `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`).
- **Kanmer pipeline** for profile `chore`: `kanmer-tickets` → `kanmer-plan` →
  `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout`. Call
  `get_doc_gates FND-003` before every move; a move crosses at most one gated
  boundary.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement
  (`AGENTS.md` § Repository task workflow step 5).

## Steps

These refine the body's eleven implementation steps; order and ownership are the
body's. Where a measured value is given below it was read on 2026-08-24 and is
the value to *compare against*, not to copy into the proof — re-derive every
figure at execution.

1. **Orient.** Read the plan row and § 3 "Kanmer board shape for the fork".
   Call `get_doc_gates FND-003` — expect `leave-preparing: [plan,
   questions-resolved]`, `enter-done: [proof, questions-resolved]`, no
   `leave-backlog` row. Then `take_ticket`. **Re-seed nothing**: creation is
   ungated, so a duplicate run produces duplicate groups rather than an error.
2. **`get_status`.** Expect `projectRoot` ending `\.worktrees\kanmer`,
   `repoRoot` the repository root, `exists: true`, `format: 3`,
   `boardSource: "file"`, `warningsCount: 0`, and `counts.byStage` showing every
   ticket in `backlog` (**229** on 2026-08-24, with `archived: 0` and
   `taken: 0`). Record `server.version` and `server.sha256Short` in the ticket —
   two hosts can run different Kanmer builds against one board; the values read
   on 2026-08-24 were `0.3.3` and `03196057`. Note also the `repo.stale` entry
   `board-config` in state **`compensated`** ("board.yml predates repoDocs; core
   falls back to the shipped defaults, so behaviour is current and only the file
   is old"). `compensated` is informational — `repo.upToDate` is still `true`,
   and this is **not** a gap for this ticket to fix.
3. **`list_board`.** Expect the 16 areas listed in the inventory above,
   row-for-row against § 3. Measured 2026-08-24: all 16 present, ids and
   prefixes exact. Report any difference; never hand-edit
   `.worktrees/kanmer/.kanmer/data/board.yml`.
4. **If an area is missing** — none was on 2026-08-24 — record the gap and run
   `kanmer-setup` (which reconciles), or hand the step to the operator through
   the Kanmer GUI. Assumption A-00-3 in plan 00 § 2 anticipates exactly this.
5. **`list_groups`.** Expect `HZN-001`…`HZN-011` titled with the proposal § 24
   phase names ("Phase 0 - discovery, inventory and decisions" … "Phase 10 -
   cutover and cloud rationalization") and `EPIC-001`…`EPIC-013` titled "Area 00
   - governance and workflow" … "Area 12 - agent tooling". Measured 2026-08-24:
   all 24 present and correctly titled.
   **5a. The board holds a twenty-fifth group, and it is not a defect.**
   `EPIC-014` — "Upstream carry-over — the subset the desktop conversion still
   needs" — was created 2026-08-24T10:00:58Z, after the initial seed, by the
   carry-over classification pass that [[FND-022]] (plan handle `DSK-01-09`)
   owns. The body's "24 groups" figure counts the plan-derived groups and
   predates it. Report the realised count as **25 (11 HZN + 14 EPIC)**, name
   `EPIC-014` as the deliberate addition, and carry it into step 10 rather than
   raising it as a duplicate.
6. **`get_group_doc … path: "context.md"` for every group.** Measured
   2026-08-24: 24 of 25 return content; **`EPIC-014` returns none**. Write it
   with `set_group_doc` from the constraint that binds that batch — for
   `EPIC-014` that is not a plan folder but the classification recorded in the
   group's own body: the source head (`collisionengineers/pegasus` branch
   `kanmer-board` at `a5b28111`, read 2026-08-24), the five disposition
   categories, the rule that a Razor UI ticket is not automatically moot, and
   the two owning tickets ([[FND-022]] re-runs the classification before Phase 3;
   [[FND-004]] verifies the counts). For any plan-derived group that is missing
   one, source it from the owning plan folder's § 1 purpose, § 3 decisions, § 4
   exit gate, § 6 routing and § 7 traps — written once, in the group, never
   repeated per ticket.
7. **Fix the dangling group references.** Measured 2026-08-24: `EPIC-011:9` and
   `EPIC-012:9` both end "see the deviation note in `board-conventions.md`", and
   `HZN-001/board-conventions.md` holds exactly one section
   (`## Upstream ids versus board ids`) with no deviation note anywhere under
   `.kanmer/groups/`. Two writes, in this order:
   **7a.** `set_group_doc HZN-001 board-conventions.md`, appending a second
   section — literally `## 2. Deviation to note` — recording that plan folders
   10 and 11 seed into `platform-operations` (PLAT) although the § 3 board-shape
   table assigns them no area, with the counts as measured (`plan-10` and
   `plan-11` ticket counts from `list_items`) and the date. Preserve § 1
   verbatim; it is the authoritative upstream↔board join and must not be
   disturbed.
   **7b.** `update_group EPIC-011` and `update_group EPIC-012` so each cites the
   section by its full address — the Kanmer group document
   `HZN-001/board-conventions.md` § 2 "Deviation to note", readable with
   `get_group_doc HZN-001 board-conventions.md` — instead of a bare filename.
   Do **not** repoint them at
   `docs/desktop/00-governance-and-workflow/README.md` § 3: that section's table
   assigns no area to plan folders 10 and 11 and carries no deviation note, so
   it would be a second dead reference. Do **not** create
   `docs/desktop/12-agent-tooling/board-conventions.md` or any other repository
   file to satisfy a board reference — that path does not exist today and must
   not be created for this.
8. **Probe the gates rather than trusting `board.yml`.** `get_doc_gates` on one
   `feature` ticket and one `chore` ticket. Expected for `feature`:
   `leave-backlog: [governing-doc]`, satisfied by `docs_todo: true`;
   `leave-preparing: [research, files, plan, checklist, questions-resolved]`.
   Expected for `chore`: **no** `leave-backlog` boundary at all, and
   `leave-preparing: [plan, questions-resolved]`. Record both outputs verbatim.
9. **Confirm nothing was moved off Backlog during seeding.** `list_items` with
   `status: "backlog"` and `list_items` with `label: "desktop-conversion"`.
   Measured 2026-08-24: 229 tickets in total, all `backlog`; 210 carry the
   `desktop-conversion` label (208 plan rows plus [[FND-051]] and [[FND-052]],
   both created later) and 19 are the upstream carry-over batch. State what you
   measure; the equality the body asks for is "every conversion ticket is in
   backlog", not "the two counts are equal".
10. **Record deliberate deviations** in
    `docs/desktop/00-governance-and-workflow/README.md` § 3 as a `Deviation:`
    line — one line covering the **group set** (the twenty-fifth group
    `EPIC-014`, created for the carry-over batch and holding no plan rows).
    **Scope boundary:** the § 3 *area table* rows at `:228-248` — the
    `desktop-foundation`, `gateway-api` and `desktop-features` rows, and plans 10
    and 11 — belong to [[FND-004]] step 9, which records the operator's chosen
    outcome. Do not edit those rows here; the § 2 note written in step 7a is
    precisely what [[FND-004]] is told to cite.
11. **Write the proof** as a `command-log` holding the `get_status`,
    `list_board` and `list_groups` outputs, the per-group `context.md` results,
    the two `get_doc_gates` probes, and the before/after of the two
    `update_group` calls. If step 10 changed a repository file, run
    `pwsh ./scripts/Test-DocumentationLinks.ps1` — the same script the CI
    `documentation` job runs at `.github/workflows/ci.yml:87` — and include its
    exit code.

## Verification

Evidence tier 1 — Static/build/architecture (`docs/engineering.md:72-90`). The
evidence is the board's own MCP output; nothing here proves application
behaviour.

| Check | Expected |
| --- | --- |
| `get_status` | `exists: true`, `format: 3`, `boardSource: "file"`, `warningsCount: 0`; `server.version` and `server.sha256Short` recorded |
| `list_board` | 16 area entries whose ids and prefixes match § 3 exactly |
| `list_groups` | 25 entries, none archived — 11 `HZN`, 14 `EPIC`, with `EPIC-014` named as the deliberate addition |
| `get_group_doc` `context.md` for all 25 | content returned for all 25 **after** step 6 (24 of 25 before it) |
| `get_group_doc HZN-001 board-conventions.md` | § 1 unchanged; § 2 "Deviation to note" present |
| `get_group EPIC-011`, `get_group EPIC-012` | body cites `HZN-001/board-conventions.md` § 2 by its full address |
| `get_doc_gates <feature id>` / `<chore id>` | as step 8; recorded verbatim |

## Risks / open questions

- **Re-seeding is the destructive failure here.** `create_group` and
  `create_item` are ungated (`AGENTS.md:14`), so a well-meaning "make sure it
  exists" run duplicates the group set. Mitigation: step 1 forbids it and every
  repair in this plan is a `set_group_doc` or `update_group` on an existing
  object.
- **The original body's "24 groups" was stale, not wrong.** It counts the plan-derived
  groups; `EPIC-014` postdates it. Mitigation: step 5a reports 25 and names the
  addition rather than reconciling the number down.
- **The § 2 the body tells you to cite does not exist yet.** Measured: no
  "Deviation to note" section anywhere under `.kanmer/groups/`. This is not an
  open question — this ticket owns group documents and step 7a writes it. Two
  other tickets ([[FND-004]] step 9, and the `EPIC-011`/`EPIC-012` bodies)
  already assume it, so writing it removes a dangling reference from three
  places at once.
- **Overlap with [[FND-004]] on the § 3 table.** A scope boundary owned by a
  named ticket, not a question: [[FND-004]] step 9 corrects the area rows and
  records the operator's chosen outcome; this ticket writes only the group-set
  deviation line.
- **`board.yml` is not the effective gate set.** `list_board`'s `profiles:`
  block is informational; `get_doc_gates` is authoritative. The `compensated`
  `board-config` staleness in `get_status` is the same point in another form and
  needs no action.
- **Never hand-edit the board worktree.** `AGENTS.md:6` — the board branch is
  managed by Kanmer.

## Simplification pass

2026-08-25 — `n/a — docs-only`. The only repository change is the six-line canonical `Deviation:` note required by the live board shape; no code or architecture was added.

## Live board verification recheck — 2026-08-26

Fresh Kanmer MCP evidence (server `0.3.3`, `sha256Short=03196057`) reports the effective board root `C:\\Users\\PC\\Documents\\GitHub\\pegasusDesktop\\.worktrees\\kanmer`, repository root `C:\\Users\\PC\\Documents\\GitHub\\pegasusDesktop`, format `3`, source `file`, and no listing warnings. The repository check is `upToDate=true`; its only stale entry is informational `board-config=compensated` because the runtime falls back to current shipped defaults.

- `list_board`: exactly 16 expected areas and prefixes: `pr-review/PR`, `mail-communications/MAIL`, `automation-integrations/AUTO`, `documents-reports/DOCS`, `engineering-assessment/ENG`, `intake-processing/INTK`, `platform-operations/PLAT`, `delivery-repository/DELIV`, `case-reference-workflow/CASE`, `desktop-foundation/FND`, `gateway-api/GWY`, `desktop-ui/DUI`, `desktop-features/FEAT`, `release-desktop/REL`, `testing/TEST`, `agent-tooling/TOOL`.
- `list_groups`: exactly 25 non-archived groups (`EPIC-001`…`EPIC-014`, `HZN-001`…`HZN-011)); `EPIC-014` is the deliberate carry-over group.
- `get_group_doc(context.md)` returned content for all 25 groups. Repository paths cited by group bodies were checked present; EPIC-011 and EPIC-012 cite `HZN-001/board-conventions.md § 2 "Deviation to note"`.
- Gate probes: chore `FND-003` requires `plan` and `questions-resolved` before done proof; feature `AUTO-001` requires governing-doc, research, files, plan, checklist and questions-resolved before implementation, and post-implementation-report before review.
- Current board movement has left backlog at 0 and the `desktop-conversion` label on 209 active tickets. This is a live post-seed board, not a re-seed; no group or ticket was created during this verification.

PR #19 (`fnd-003-board-shape`) is merged into `dev` at merge commit `fff7e14178f1be6e3d4f2fbc5a5401799ba69409`; repository-check run `32981200637` passed its applicable documentation/changes lanes at the exact PR head. The ticket's proof remains intentionally unwritten until the reviewed `dev` history is promoted to `main`, as required by the repository Kanmer workflow.
