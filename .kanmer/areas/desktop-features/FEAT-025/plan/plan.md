# Plan — FEAT-025: Parity evidence per slice (matrix maintenance)

**Diff estimate: ~2 files, ~420 lines, spread across the whole slice programme.** This is a
standing-discipline `chore`: it edits
`docs/desktop/01-inventory-and-parity/parity-matrix.md` (46 rows × up to five status advances and
their evidence links, plus the `UAT owner` column — the bulk of the ~420 lines, landed
incrementally, not in one PR) and the reviewer checklist maintained by [[FND-011]] (plan handle
`DSK-00-11`) (~20 lines). It changes **no code**. The first PR carries the maintenance rule, the
row→slice mapping and the reviewer check; subsequent edits ride with each slice.

**Chore inventory** — this profile owes no `research` or `files` document, so the measured surface
area is stated here (`docs/engineering.md` § plan sizing: a real inventory, not an assertion).
Measured at `bbd1c549` (`git rev-parse --short HEAD`, 2026-08-24).

| Path | Measured today | Role in this ticket |
| --- | --- | --- |
| `docs/desktop/01-inventory-and-parity/parity-matrix.md` | **46 rows** (`grep -c '^| PAR-'`). Sections: `## Legend` at **`:9`**, `## Matrix` at **`:42`**, `## Notes for the tickets that complete this matrix` at **`:93`**. Ten columns: ID, Capability group (§13.x), FRD owner, Current entry point, Current behaviour evidence, Native screen/use case, API/data dependency, Test evidence, **UAT owner**, Status (`:44`). | The single artefact this ticket maintains. |
| — status distribution | **23** rows at `not inventoried`, **21** at `inventoried`, **2** at `legacy path retained` (`PAR-31` at **`:76`**, `Uploads/Request.cshtml.cs`; `PAR-42` at **`:87`**, `Connect/Authorize.cshtml.cs`). **0** rows at `designed` or beyond. | The starting state the ladder rule is written against. |
| — the `UAT owner` column | **Blank on all 46 rows** (`grep -c "\|  \| "` → 46). The column note at `:41` says "left blank for the operator to assign"; the closing note at `:105-106` says the owner "is assigned by the operator per capability group **before any row may move past `automated verification passed`**". | Step 5's rule already has repository authority; this ticket enforces it rather than inventing it. |
| — the ladder | Nine states at `:13-22`: `not inventoried`, `inventoried`, `designed`, `implemented`, `automated verification passed`, `UAT passed`, `cut over`, `legacy path retired`, and `legacy path retained` ("Deviation from §23: the surface deliberately stays server-side"). | The vocabulary the maintenance rule uses verbatim. |
| — required evidence | Nine items at `:24-30` (proposal §23.1): current screenshot or behavioural description; current route/controller/service/entity references; approved native design; cloud-placement decision; automated test result; manual/UAT result; data comparison where applicable; known deliberate difference; rollback path. The line at `:29-30` records that "the evidence itself lives in the ticket proof" and the columns hold pointers. | What "linked evidence" means, precisely. |
| — the `~` convention | `:38-40`: endpoint names prefixed `~` are indicative, not decided; `:103-104` repeats "do not implement from this table". | A row may not claim `designed` on the strength of a `~` name. |
| `scripts/Test-DocumentationLinks.ps1` | Present (`ls scripts/*.ps1`) | Run after every matrix edit. |
| `scripts/Test-MarkdownPlacement.ps1` | Present | Run to keep new Markdown inside `docs/(prd\|frd\|adr\|design\|desktop)`. |
| Slice handles → board ids | Resolved for the whole `DSK-05-nn` range: `DSK-05-01`…`DSK-05-26` → `FEAT-001`…`FEAT-026` (one-to-one, in order). Confirmed by reading each ticket's title prefix. | Step 3's mapping input; no guessing required. |

## Approach

Make the ladder **rule-shaped and copyable**, map every row to exactly one owning slice **board
id**, and put the enforcement where a status advance actually happens: the reviewer's checklist.
Then run a reconciliation pass at each phase gate that re-reads every row, confirms its cited test
files still exist and its proof link still resolves, and confirms no row silently regressed. The
Phase 9 completeness report is the input to the cutover decision.

Rejected: **a script that derives status from the board.** A row's status is a claim about evidence,
not about ticket state; a ticket can be Done with its matrix evidence never linked, and a derived
status would launder that into a green row. The whole value of the matrix is that it is the
conversion's honesty record — `docs/desktop/05-implementation-and-migration/README.md` § 4 makes it
the Phase 9 and Phase 10 gate. Also rejected: **letting each slice own its own rule**, which is how
twenty-one slices arrive at twenty-one interpretations of `automated verification passed`.

## Governing docs

The ticket's `refs` is **empty** (`get_doc_gates FEAT-025` reports `refs: null`), and
`docs_todo: true`. The New-ADR paragraph alone would give `kanmer-review` nothing to check, so the
authorities that bind today are tabled below.

> **New ADR** — ADR-0101 (local-execution / cloud-authority split and the six-question
> cloud-justification test), authored by [[FND-005]] (plan handle `DSK-00-05`).
> This plan is written to the decision as recorded in
> `docs/desktop/00-governance-and-workflow/README.md` § 3 (ADR set table, `:155`); if the ADR lands
> differently this plan is revised before implementation. The **cloud-placement decision** is one of
> the nine required evidence items per row (`parity-matrix.md:24-30`), which is why an ADR appears in
> a documentation-only ticket at all.

| Authority | Requirement | Met by |
| --- | --- | --- |
| Proposal § 23 Verification and feature parity | A repository-derived parity matrix is the conversion's evidence of completeness | Steps 2–6 |
| Proposal § 23.1 Required conversion evidence | Nine evidence items per row, restated at `parity-matrix.md:24-30` | Step 2's ladder rule, Step 6's reconciliation |
| Proposal § 27 Acceptance criteria | Every critical workflow has automated and UAT parity evidence | Step 11's completeness report |
| Plan 05 § 4 (phase exit-gate table) | The phase gates are the programme gates; the matrix proves them | Step 6, Step 11 |
| Plan 05 § 8 (documentation changes) | Row status per slice: `designed` → `implemented` → `automated verification passed` → `UAT passed` → `cut over` → `legacy path retired` | Step 2 |
| `parity-matrix.md:105-106` | The operator assigns a `UAT owner` per capability group **before any row may move past `automated verification passed`** | Step 5 |
| `parity-matrix.md:38-40`, `:103-104` | `~`-prefixed endpoint names are indicative; do not implement from the table | Step 2 (a `~` name cannot support a `designed` claim) |
| L-05 | The board is seeded from these plans, so a matrix row and its ticket stay joined | Step 3 |
| L-02 | UAT happens on the local Test/UAT stack or the production pilot ring, never an Azure test environment | Step 10 |
| L-04 | Routing named on the ticket | § Routing |
| `docs/engineering.md` § Required evidence tiers, **tier 12** | Evidence from the authenticated source receipt through Core, SQL and the outbox, the actual Worker trigger, the adapter outcome, the persisted operator view, telemetry and safe replay; **registration or mock-only paths do not satisfy it** | Step 2's rule for `automated verification passed` |
| `HZN-001` / `board-conventions.md` § Upstream ids versus board ids | A bare `<PREFIX>-<nnn>` is a fork board id; upstream ids are written `upstream <ID>` | Step 3 |
| CI `documentation` job | `scripts/Test-DocumentationLinks.ps1` and `scripts/Test-MarkdownPlacement.ps1` must pass | Step 8, § Verification |

## Routing

Copied from the ticket body's `## Routing` block, as
`docs/desktop/00-governance-and-workflow/README.md` § Ticket template requires in the plan.

- **Subagent**: `pegasus-parity-researcher` — `.codex/agents/pegasus-parity-researcher.toml`
  (evidence gathering and row reconciliation); `pegasus-desktop-reviewer` —
  `.codex/agents/pegasus-desktop-reviewer.toml` (reviews every matrix diff)
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `kanmer-research`
  (`.grok/skills/kanmer-research/SKILL.md`) → `kanmer-verify` (`.grok/skills/kanmer-verify/SKILL.md`)
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `list_items`, `search_items`, `get_item`,
  `get_ticket_doc`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`)
- **Kanmer pipeline** for profile `chore`: `kanmer-plan` → `kanmer-execute` → `kanmer-verify` →
  `kanmer-closeout` (call `get_doc_gates <id>` before every move; `chore` needs `plan` and
  `questions-resolved` to leave Preparing and `proof` to enter Done)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md`
  § Repository task workflow step 5)

## Steps

Refining the ticket body's twelve steps. Body step numbers in brackets.

1. **[body 1] Orient and take.** Read `parity-matrix.md` in full — the `## Legend` ladder at
   `:13-22`, the nine required evidence items at `:24-30`, the column notes at `:34-41`, and the
   closing notes at `:93-106` — and plan 05 § 4. Call `get_doc_gates FEAT-025`, then `take_ticket`
   with branch `task/dsk-05-25-parity-evidence` and worktree
   `../pegasus-worktrees/dsk-05-25-parity-evidence` from `origin/dev`.
2. **[body 2] Write the maintenance rule into this plan, as a short copyable checklist** a slice
   author follows. Using the ladder's own words:
   - `designed` — the screen spec exists in area 06 **and** the endpoint is named in area 03's
     endpoint map with a **decided** (not `~`-prefixed) path. Link: the spec section and the
     endpoint-map row.
   - `implemented` — the code has **merged**, not merely exists on a branch. Link: the PR.
   - `automated verification passed` — the named unit, contract and UI-automation evidence is green
     on merged `main`. Link: the exact test **names**, not just project paths. Tier 12 applies: a
     registration-only or mock-only path does not satisfy it.
   - `UAT passed` — a **named** owner has signed off, with a date. Link: the sign-off text in the
     slice's proof document.
   - `cut over` — the desktop is the path in use and the web path is disabled in the Test/UAT stack.
   - `legacy path retired` — the web page is removed ([[FEAT-026]], plan handle `DSK-05-26`).
   A row may never skip a rung, and a row may never advance in the same PR that first creates its
   evidence without that evidence being linked in the same diff.
3. **[body 3] Map every row to its owning slice, by board id.** Use `list_items` and `search_items`
   to resolve each handle; the `DSK-05-nn` range resolves one-to-one and in order to
   `FEAT-001`…`FEAT-026`, already confirmed. Write both forms on first mention —
   ``[[FEAT-nnn]] (plan handle `DSK-05-nn`)`` — because `[[DSK-05-nn]]` does not resolve as a wiki
   link. **Flag any row with no owning slice and any slice with no row; both are defects to raise as
   tickets, not to paper over.** Start from the measured state: 23 rows `not inventoried`, 21
   `inventoried`, 2 `legacy path retained`, none beyond.
4. **[body 4] Add the reviewer check.** Every slice PR that claims a status change must include the
   matrix diff, and `pegasus-desktop-reviewer` refuses a status advance whose evidence is missing or
   unnamed. Record the check in the reviewer checklist maintained by [[FND-011]] (plan handle
   `DSK-00-11`).
5. **[body 5] Enforce the named `UAT owner`.** All 46 cells are blank today and
   `parity-matrix.md:105-106` already requires the operator to assign one per capability group
   before a row moves past `automated verification passed`. A row advanced to `UAT passed` without a
   name is reverted and the slice ticket reopened.
6. **[body 6] The per-phase reconciliation pass.** Read every row; confirm its cited test files
   still exist and its proof link still resolves; confirm no row silently regressed. Record the pass
   and its date in the ticket proof. Run it at each phase gate — Phases 3, 4, 5, 6, 7, 8, 9.
7. **[body 7] Handle the deliberate exceptions honestly.** `PAR-31` (`:76`,
   `Uploads/Request.cshtml.cs`) and `PAR-42` (`:87`, `Connect/Authorize.cshtml.cs`) already read
   `legacy path retained` — they are the two surfaces the endpoint map's § `Stays web-only (not
   projected)` table keeps server-side. They must **never** be set to `cut over`. Confirm the
   endpoint map's web-only list against the matrix and record any surface on one list but not the
   other.
8. **[body 8] Link-check after every edit.** `pwsh ./scripts/Test-DocumentationLinks.ps1`, so a
   broken proof or test link fails locally rather than in the CI `documentation` job.
9. **[body 9] One canonical location.** The matrix stays where it is; [[FND-012]] (plan handle
   `DSK-00-12`) owns the question of whether it later moves to `docs/features/` per proposal §23.
   Do not move it here, and do not create a second copy anywhere.
10. **[body 10] Operator step — collect the sign-offs.** For each slice reaching UAT, obtain the
    named owner's confirmation text and date, file it in **that slice's** proof, then advance the
    row. The sign-off is the operator's, not an agent's assertion. L-02: UAT runs on the local
    Test/UAT stack or the production pilot ring, never an Azure test environment.
11. **[body 11] The Phase 9 completeness report.** Every row's status, its evidence links, and any
    row not at `UAT passed` or better with the reason and the owning ticket. Attach it to this
    ticket's proof — it is the input to the cutover decision and to [[FEAT-026]]'s step 3.
12. **[body 12] Simplification and PR.** `n/a — docs-only` applies to a matrix-only change, still
    recorded under a dated `## Simplification pass` heading; then open the PR into `dev`.

## Verification

Evidence tier from the body: **12** (Integrated workflow). Tier 12 obliges evidence from the
authenticated source receipt through Core, SQL and the outbox, the actual Worker trigger, the
adapter outcome, the persisted operator view, telemetry and safe replay; registration or mock-only
paths do not satisfy it. A row may claim `automated verification passed` or better **only** when
evidence of that kind is linked.

- `pwsh ./scripts/Test-DocumentationLinks.ps1` — passes after every matrix edit; no broken proof or
  test link.
- `pwsh ./scripts/Test-MarkdownPlacement.ps1` — passes; no new Markdown outside
  `docs/(prd|frd|adr|design|desktop)`.
- Kanmer `list_items` for area `desktop-features` — every `DSK-05-nn` slice handle resolves to a
  board ticket and appears in the row mapping.
- **Completeness report in the ticket proof** — every row's status and evidence listed; exceptions
  named with their owning tickets.

Evidence that becomes `proof`: the two script outputs (command-log tier), the row→slice mapping, the
dated reconciliation records, and the Phase 9 completeness report.

## Risks / open questions

- **A status advanced without evidence is worse than a row left behind.** The matrix is the
  conversion's honesty record and the Phase 9/10 gates read it. Mitigation: step 4's reviewer check
  refuses an unlinked advance, and step 6's reconciliation catches one that slipped through.
- **All 46 `UAT owner` cells are blank and only the operator can fill them.** Mitigation: step 10 is
  an operator step; step 5 reverts a row advanced without a name. Owner: the operator, per capability
  group, per `parity-matrix.md:105-106`.
- **Three namespaces, and two of them look alike.** `CASE-17` is a capability, `CASE-017` a board
  ticket, and `DSK-05-17` a plan handle that **does not resolve as a wiki link**. Mitigation: step 3
  writes ``[[FEAT-nnn]] (plan handle `DSK-05-nn`)`` on first mention and the bare board id
  thereafter; `HZN-001`'s `board-conventions.md` holds the upstream join table.
- **A `~`-prefixed endpoint name is not a decided endpoint** (`parity-matrix.md:38-40`). Mitigation:
  step 2's `designed` rule requires a decided path, so a row cannot advance on a placeholder.
- **Rows with no owning slice, and slices with no row.** Both are real possibilities given 46 rows
  and 21 slices. Mitigation: step 3 flags them as defects to raise. The matrix's own notes at
  `:95-96` already list rows needing test evidence located (`PAR-14`, `16–18`, `23–27`, `29–31`,
  `33–42`, `44`), fed by [[FND-025]] (plan handle `DSK-01-12`).
- **Whether the matrix later moves to `docs/features/`** — owned by [[FND-012]] (plan handle
  `DSK-00-12`). A scope boundary, not an open question; do not move it and do not create a second
  copy.
- **Ticket-transient notes must not become new Markdown.** Anything outside
  `docs/(prd|frd|adr|design|desktop)` fails the CI `documentation` job, so working notes live in
  Kanmer (`append_scratch`).

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over this branch's own
diff before the PR, recorded here under a dated heading — `n/a — docs-only` for a matrix-only
change._
