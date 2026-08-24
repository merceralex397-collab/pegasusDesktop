# Plan — FND-012: Register the parity matrix as one canonical path and decide whether it later moves to `docs/features/`

**Diff estimate: ~4 files, ~16 lines.** `docs/engineering.md` § plan sizing requires the
estimate first. This profile is `chore` — it owes no `research` or `files` document, so the
measured inventory below carries the surface area alone. No file is moved and no script is
changed, which is what keeps the number this small.

## Measured file-and-line inventory

Measured at `bbd1c549` on 2026-08-24.

| Path | Current size and the exact anchor | Change | Est. lines |
| --- | --- | --- | --- |
| `docs/index.md` | 59 lines. The question→file table header is `:7-8`; its rows run `:9-28`; the desktop plan-set row is the last one, `:28`. `## Authority` starts `:30`. | One new row appended after `:28` | 1 |
| `docs/capabilities.md` | 392 lines. `## Capabilities` at `:69`; the capability table header at `:71-72`; the first data row (`OPS-10`) at `:73`. The paragraph above the heading ends `:67`. | A short note between `:69` and `:71`, before the table header | ~4 |
| `docs/desktop/01-inventory-and-parity/parity-matrix.md` | 105 lines. Title `:1`; the header paragraph `:3-7` (it already names the 2026-08-23 pre-population from `main` `191ddf33` and the tickets that complete it); `## Legend` at `:8`. | One canonical-path line added to the `:3-7` header paragraph | ~4 |
| `docs/desktop/00-governance-and-workflow/README.md` | 431 lines. `## 8. Documentation changes` at `:419`; its two-column table header `:421-422`; rows from `:423`. | One row (or a short paragraph under the table) recording the `docs/features/` decision and its cost | ~3 |
| `scripts/Test-MarkdownPlacement.ps1` | 128-line sibling test; the allowed-roots regex is `:31` | **Unchanged** — asserted, not edited | 0 |

Facts measured for the steps, not guesses:

- **`git ls-files '*parity*'` returns six paths, not one.** The directory
  `docs/desktop/01-inventory-and-parity/` matches the glob, so the five files inside it plus
  `.codex/agents/pegasus-parity-researcher.toml` all come back. The command that answers the
  ticket's actual question is **`git ls-files '*parity-matrix*'`**, which returns exactly
  `docs/desktop/01-inventory-and-parity/parity-matrix.md`. Both are recorded in Verification
  below; the body's expectation ("exactly one matrix file") is correct, its glob is not.
- **`git ls-files '*desktop-parity*'` returns nothing** — the proposal's
  `docs/features/desktop-parity-matrix.md` has never been created, so there is no second
  matrix to reconcile.
- **`grep -rn 'parity-matrix' docs/ --include='*.md'` gives 23 hits across 9 files**:
  `00-governance-and-workflow/README.md`, `01-inventory-and-parity/README.md`,
  `01-inventory-and-parity/upstream-kanmer-carryover.md`, `03-gateway-api-and-data/endpoint-map.md`,
  `05-implementation-and-migration/README.md`, `05-implementation-and-migration/vertical-slices.md`,
  `06-ui-design/screen-specs.md`, `12-agent-tooling/subagents.md` and the proposal itself.
  Those 23 are the inbound set step 8 re-checks; each sits at a different relative depth.
- The allowed-roots regex at `scripts/Test-MarkdownPlacement.ps1:31` is
  `^((docs/(prd|frd|adr|design|desktop))|workspaces/document-extraction|\.agents/skills|\.design-sync|\.grok|\.stitch|design/planning-and-old-designs)/.+\.md$`.
  `docs/features/` is **not** in it. The `documentation` job at `.github/workflows/ci.yml:71-88`
  runs `Test-TestMarkdownPlacement.ps1` and `Test-DocumentationLinks.ps1` on every change set,
  so a naive rename fails CI.

## Approach

Register the existing path and decide the move as a *recorded deviation* rather than doing
anything to the file: add one row to `docs/index.md`, one note to `docs/capabilities.md`, one
canonical-path line to the matrix header, and the decision with its cost to the area 00
documentation-changes section. Option (a) — keep the matrix under `docs/desktop/` — is the
one this plan recommends, because the placement gate already allows that root, because area 01
is still filling the rows ([[FND-014]] through [[FND-018]]), and because the matrix is
programme evidence rather than a governing document. The rejected alternative, option (b)
— move to `docs/features/desktop-parity-matrix.md` at cutover — costs a regex change at
`scripts/Test-MarkdownPlacement.ps1:31`, an update to its 128-line regression test
`scripts/Test-TestMarkdownPlacement.ps1`, and 23 inbound link edits across 9 files; if the
operator chooses it, the plan records the trigger and raises a follow-up ticket instead of
moving the file mid-fill.

## Governing docs

The ticket's `refs` is empty and it carries `docs_todo: true` — confirmed in
`get_doc_gates FND-012` (`"refs": []`, `"docs_todo": true`). `docs/index.md` and
`docs/capabilities.md` are current-state/registry documents, not PRD/FRD/ADR, so no governing
document is *met* by a step here.

> **New ADR** — none. This ticket records a **deviation from proposal § 23's path**, not a
> durable technical decision, so `AGENTS.md:91-92` ("one decision per ADR") argues against
> creating one; the deviation is recorded in
> `docs/desktop/00-governance-and-workflow/README.md` § 8 and in the matrix header, which is
> where `docs/desktop/01-inventory-and-parity/README.md` § 8 already records the same
> deviation in draft form. If the operator instead wants the move made binding, the decision
> belongs in ADR-0100 (native WinUI 3 desktop client in the fork), authored by [[FND-005]]
> (plan handle `DSK-00-05`); see [[FND-005]]'s plan for the ownership reconciliation with its
> co-claimant [[FND-026]] (plan handle `DSK-02-01`).

Programme-level authorities that bind today, with the step that satisfies each:

| Authority | Requirement | Met by |
| --- | --- | --- |
| Proposal § 23 (`docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md:1669-1686`) | "Create `docs/features/desktop-parity-matrix.md`" with one row per observable capability and the eight-status ladder | Step 5 records the deviation and its reason; steps 3–4 make the existing file findable |
| Proposal § 23.1 (`:1687-1699`) | Required conversion evidence per workflow | Step 7's header line keeps the evidence pointer on one path |
| **L-05** (`docs/desktop/README.md` § Locked decisions) | The plans, and the matrix they carry, are the source the board is seeded from — the matrix is planning evidence, not a governing document | Steps 3–4 (registered as evidence, not authority) |
| Plan 01 § 8 (`docs/desktop/01-inventory-and-parity/README.md:236-241`) | The matrix "becomes the canonical parity matrix … Deviation: kept inside the plan set because `docs/features/` does not exist and the placement gate allows `docs/desktop/`. Area 00 may relocate it by ticket." | Step 5 promotes that draft deviation into a recorded decision |
| `docs/index.md:12` | `docs/capabilities.md`'s *Canonical owner* column joins each capability ID to its PRD/FRD/ADR | Step 4 (the note must not claim to replace that column) |
| `scripts/Test-MarkdownPlacement.ps1:31` + `.github/workflows/ci.yml:71-88` | Markdown outside the allowed roots fails the `documentation` job | Step 6 (no move) and step 9 (both gates green) |
| Plan 00 § 7 (`docs/desktop/00-governance-and-workflow/README.md:396-398`) | Any `.md` outside `docs/(prd\|frd\|adr\|design\|desktop)` fails CI; ticket-transient documents go to Kanmer | Step 6 |

## Routing

Copied from the ticket body's `## Routing` block
(`docs/desktop/00-governance-and-workflow/README.md:272` makes this block mandatory in the
plan document specifically).

- **Subagent**: — (parent session)
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `kanmer-docs`
  (`.grok/skills/kanmer-docs/SKILL.md`)
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`,
  `append_scratch`, `move_item`)
- **Kanmer pipeline** for profile `chore`: `kanmer-tickets` → `kanmer-plan` →
  `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (call
  `get_doc_gates FND-012` before every move; a move crosses at most one gated boundary)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement
  (`AGENTS.md:298-305`)

## Steps

These refine the ticket body's eleven implementation steps — same order, same ownership, same
file paths.

1. **Orient.** Read `docs/desktop/01-inventory-and-parity/parity-matrix.md:1-38` (header and
   Legend), proposal § 23 (`:1669-1699`), `docs/index.md:1-30`, and
   `scripts/Test-MarkdownPlacement.ps1:27-32`. Call `get_doc_gates FND-012` — it reports
   `leave-preparing` needing `plan` and `enter-done` needing `proof` — then `take_ticket` onto
   `task/<slug>` in `../pegasus-worktrees/<slug>` from `origin/dev`.
2. **Prove there is only one matrix today.** Run **`git ls-files '*parity-matrix*'`** (returns
   the single file) and `git ls-files '*desktop-parity*'` (returns nothing). Record both.
   The body's `git ls-files '*parity*'` also runs, but expect six paths from it — the
   directory name matches the glob — and say so in the record rather than treating the five
   siblings and the researcher TOML as duplicate matrices. Any file that is genuinely a second
   matrix is a finding to resolve before a canonical path is advertised.
3. **Register it in `docs/index.md`.** Append one row after `:28`:
   "Which desktop workflows have parity evidence, and at what status?" →
   `docs/desktop/01-inventory-and-parity/parity-matrix.md`, with the clause that it is
   **programme evidence, not a governing document**. Match the surrounding rows' relative-link
   style (`desktop/01-inventory-and-parity/parity-matrix.md` from `docs/index.md`).
4. **Note it in `docs/capabilities.md`.** Insert a short paragraph between `## Capabilities`
   (`:69`) and the table header (`:71`): the desktop conversion's per-capability parity status
   is tracked in the matrix, and the matrix **does not replace** the *Canonical owner* column.
   Do not add a column and do not add a row — the capability registry's shape is not this
   ticket's to change.
5. **Decide the `docs/features/` question and record it.** Write the decision, its reason and
   its cost as a row (or short paragraph) in
   `docs/desktop/00-governance-and-workflow/README.md` § 8 (`:419-431`), and repeat the
   one-line conclusion in the matrix header. State both options with their measured costs:
   **(a)** keep under `docs/desktop/` — allowed by the `:31` regex today, keeps programme
   planning together, recorded as a deviation from proposal § 23's path; **(b)** move to
   `docs/features/desktop-parity-matrix.md` at cutover — needs the `:31` regex changed, the
   128-line `scripts/Test-TestMarkdownPlacement.ps1` updated, and 23 inbound links across 9
   files re-pointed.
6. **Do not perform the move under either option.** If (b) is chosen, record the trigger
   (cutover) and raise a follow-up ticket. Moving a document the area 01 tickets
   ([[FND-014]] plan handle `DSK-01-01`, and [[FND-015]]–[[FND-018]]) are still filling would
   invalidate their in-flight row edits.
7. **Add the canonical-path line to the matrix header** (`:3-7`): this path is canonical, and
   the proposal's `docs/features/` path is a recorded deviation, with a pointer to the area 00
   § 8 entry. One line, inside the existing header paragraph; the Legend at `:8` and the
   Matrix table are untouched.
8. **Re-check every inbound reference.** Run `grep -rn 'parity-matrix' docs/ --include='*.md'`
   — expect the same 23 hits across the same 9 files as before the change, plus the new
   `docs/index.md` hit. For each, confirm the relative depth is right *from its own file*
   (`docs/index.md` needs `desktop/01-…`, a sibling inside `01-inventory-and-parity/` needs a
   bare filename, area 03/05/06/12 files need `../01-inventory-and-parity/…`).
9. **Run both gates.** `pwsh ./scripts/Test-DocumentationLinks.ps1` and
   `pwsh ./scripts/Test-TestMarkdownPlacement.ps1` — both exit 0. These are the two steps of
   the CI `documentation` job (`.github/workflows/ci.yml:82-88`), so a green pair locally
   predicts a green lane.
10. **PR and review.** Open the PR against `dev`, take the independent
    `pegasus-desktop-reviewer` review, and record `n/a — docs-only` under a dated
    `## Simplification pass` heading in this plan (`AGENTS.md:289-297`).
11. **Write `proof`** as a `command-log`: the `git ls-files '*parity-matrix*'` output showing a
    single file, the `git ls-files '*parity*'` output with its six-path explanation, and the
    two green gate runs.

## Verification

Evidence tier from the ticket body: **Tier 1 — Static/build/architecture**. Documentation
placement and link integrity only. `proof` is a `command-log`.

| Command | Expected |
| --- | --- |
| `git ls-files '*parity-matrix*'` | exactly `docs/desktop/01-inventory-and-parity/parity-matrix.md` |
| `git ls-files '*parity*'` | six paths — the five files under `docs/desktop/01-inventory-and-parity/` plus `.codex/agents/pegasus-parity-researcher.toml`; the glob matches the **directory** name, and this is recorded as an explanation, not a duplicate-matrix finding |
| `git ls-files '*desktop-parity*'` | no output — the proposal's `docs/features/` path was never created |
| `pwsh ./scripts/Test-DocumentationLinks.ps1` | exits 0 |
| `pwsh ./scripts/Test-TestMarkdownPlacement.ps1` | exits 0 |
| `grep -n 'parity-matrix' docs/index.md` | exactly one row |
| `git diff --stat scripts/` | empty — the placement regex at `:31` is unchanged |
| `git diff --name-status -- docs/desktop/01-inventory-and-parity/parity-matrix.md` | `M`, never `R` — no rename |

## Risks / open questions

- **Risk: the body's `git ls-files '*parity*'` is read as "six matrices exist".** Mitigation:
  step 2 runs the narrowed glob first and records the six-path result with its explanation.
  This is recorded as a body-command imprecision, not a scope change — the body's intent
  ("exactly one parity matrix file exists") is met.
- **Risk: a link edit at the wrong relative depth.** Mitigation: step 8 checks each of the 23
  hits from its own file, and `Test-DocumentationLinks.ps1` catches the rest.
- **Risk: the `docs/capabilities.md` note drifts into a second capability registry.**
  Mitigation: step 4 forbids adding a column or a row, and states the note does not replace
  the *Canonical owner* column.
- **The `docs/features/` question is decided by this ticket** (step 5), with option (a)
  recommended and both costs measured. It is therefore **not** an `open-questions/` item: the
  body instructs the ticket to decide it, and the contract's "take trivial defaults rather
  than asking" applies — the default is the path the placement gate already allows. If the
  operator prefers (b), the trigger and the follow-up ticket are the recorded outcome, still
  without a blocking question.
- **Scope boundary, not an open question**: filling matrix rows belongs to [[FND-014]] (plan
  handle `DSK-01-01`) through [[FND-018]] (plan handle `DSK-01-05`), not here.
- **Dependency, not an open question**: the body makes this ticket depend on `DSK-01-01`
  ([[FND-014]]) — the skeleton is confirmed before the path is advertised as canonical. The
  board already records that edge (`FND-014` `blocks` `FND-012`).

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 (`AGENTS.md:289-297`) requires a
pass over this branch's own diff before the PR, recorded here under a dated heading. Record
`n/a — docs-only` for this documentation-only branch._
