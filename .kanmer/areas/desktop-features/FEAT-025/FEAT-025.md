---
id: FEAT-025
type: ticket
title: DSK-05-25 · Parity evidence per slice (matrix maintenance)
status: backlog
area: desktop-features
assignee: ''
profile: chore
labels:
  - desktop-conversion
  - plan-05
  - phase-3
  - tier-12
  - needs-operator
groups:
  - EPIC-006
  - HZN-004
links: []
docs_todo: true
archived: false
created: '2026-08-24T08:04:08.606Z'
updated: '2026-08-24T08:04:08.606Z'
---

## What

Keep `docs/desktop/01-inventory-and-parity/parity-matrix.md` honest across the whole slice programme: every row advances through `designed` → `implemented` → `automated verification passed` → `UAT passed` with linked proof, no row advances without evidence, and each slice PR carries a reviewed matrix diff.

## Why

Proposal §23 makes a repository-derived parity matrix the conversion's evidence of completeness, and the programme exit checklist requires every critical workflow to have automated and UAT parity evidence. The matrix already exists with a nine-state ladder and one row per observable capability (`PAR-01` onward), pre-populated on 2026-08-23 and completed by [[DSK-01-01]] to [[DSK-01-05]]. Without a standing owner, rows drift: a slice merges and its row stays `designed`, or a row is advanced to `UAT passed` with no named signer. That would make the Phase 9 and Phase 10 gates unprovable. Siblings: every slice [[DSK-05-01]] to [[DSK-05-21]] updates its own rows; this ticket owns the discipline, the review and the reconciliation.

## Source of truth

- Plan row: `docs/desktop/05-implementation-and-migration/README.md` § 5 — `DSK-05-25`
- Plan detail: `docs/desktop/05-implementation-and-migration/README.md` § 4 (target state and the phase exit-gate table) and § 8 (documentation changes: row status per slice)
- Matrix: `docs/desktop/01-inventory-and-parity/parity-matrix.md` § `Legend` (the status ladder and the nine required evidence items from proposal §23.1) and § `Matrix`
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 23 Verification and feature parity, § 23.1 Required conversion evidence, § 27 Acceptance criteria
- Repository evidence: `docs/desktop/01-inventory-and-parity/parity-matrix.md` (rows `PAR-01`… with columns Current entry point, Current behaviour evidence, Native screen/use case, API/data dependency, Test evidence, UAT owner, Status), `scripts/Test-DocumentationLinks.ps1`, `scripts/Test-MarkdownPlacement.ps1` (the CI `documentation` job runs both)
- Binding decisions: L-05 the board is seeded from these plans, so a matrix row and its ticket must stay joined; L-02 UAT happens on the local Test/UAT stack or the production pilot ring, never an Azure test environment; L-04 routing named on the ticket
- Depends on: `DSK-01-01` the confirmed matrix skeleton; and every slice whose rows it maintains — `DSK-05-01`, `DSK-05-02`, `DSK-05-03`, `DSK-05-04`, `DSK-05-05`, `DSK-05-06`, `DSK-05-07`, `DSK-05-08`, `DSK-05-09`, `DSK-05-10`, `DSK-05-11`, `DSK-05-12`, `DSK-05-13`, `DSK-05-14`, `DSK-05-15`, `DSK-05-16`, `DSK-05-17`, `DSK-05-18`, `DSK-05-19`, `DSK-05-20`, `DSK-05-21`

## Routing

- **Subagent**: `pegasus-parity-researcher` — `.codex/agents/pegasus-parity-researcher.toml` (evidence gathering and row reconciliation); `pegasus-desktop-reviewer` — `.codex/agents/pegasus-desktop-reviewer.toml` (reviews every matrix diff)
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `kanmer-research` (`.grok/skills/kanmer-research/SKILL.md`) → `kanmer-verify` (`.grok/skills/kanmer-verify/SKILL.md`)
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `list_items`, `search_items`, `get_item`, `get_ticket_doc`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`)
- **Kanmer pipeline** for profile `chore`: `kanmer-plan` → `kanmer-execute` → `kanmer-verify` → `kanmer-closeout` (call `get_doc_gates <id>` before every move; `chore` needs `plan` and `questions-resolved` to leave Preparing and `proof` to enter Done)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Orient: read the plan row, `docs/desktop/01-inventory-and-parity/parity-matrix.md` in full — particularly § `Legend`, the nine required evidence items and the column notes — and § 4 of the area plan. Call `get_doc_gates <this ticket id>`, then `take_ticket` with branch `task/dsk-05-25-parity-evidence` and worktree `../pegasus-worktrees/dsk-05-25-parity-evidence` from `origin/dev`.
2. Write the maintenance rule in the ticket plan as a short, copyable checklist a slice author follows: which status a row may move to at each pipeline stage (`designed` when the screen spec and endpoint exist; `implemented` when the code merges; `automated verification passed` when the named tests are green on merged `main`; `UAT passed` when a named owner signs off with a date), and what must be linked at each step (test names, proof document, sign-off text).
3. Map every matrix row to its owning slice handle and record the mapping in the plan. Use `list_items` and `search_items` on the Kanmer board to resolve each `DSK-05-nn` handle to its board id so the mapping is usable by an agent working a slice. Flag any row with no owning slice and any slice with no row — both are defects to raise, not to paper over.
4. Add the reviewer check: every slice PR that claims a status change must include the matrix diff, and `pegasus-desktop-reviewer` refuses a status advance whose evidence is missing or unnamed. Record the check in the reviewer checklist maintained by [[DSK-00-11]].
5. Enforce that `UAT owner` is a named person, never blank, before a row reads `UAT passed`; a row advanced without a name is reverted and the slice ticket reopened.
6. Add a reconciliation pass, run once per phase gate: read every row, confirm its cited test files still exist and its proof link still resolves, and confirm no row silently regressed. Record the pass and its date in the ticket proof.
7. Handle the deliberate exceptions honestly: the `legacy path retained` state exists for surfaces that deliberately stay server-side — `Pages/Uploads/Request.cshtml.cs` and `Pages/Connect/Authorize.cshtml.cs` (see `docs/desktop/03-gateway-api-and-data/endpoint-map.md` § `Stays web-only`). Set those rows to `legacy path retained` with the reason, and never to `cut over`.
8. Run `pwsh ./scripts/Test-DocumentationLinks.ps1` after every matrix edit so a broken proof or test link fails locally rather than in the CI `documentation` job.
9. Keep the matrix in its one canonical location; [[DSK-00-12]] owns the question of whether it later moves to `docs/features/` per proposal §23 — do not move it here, and do not create a second copy anywhere.
10. **Operator step** — collect the UAT sign-offs: for each slice reaching UAT, obtain the named owner's confirmation text and date and file it in that slice's proof, then advance the row. The sign-off is the operator's, not an agent's assertion.
11. At the Phase 9 gate, produce the completeness report: every row's status, its evidence links and any row not at `UAT passed` or better with the reason and owning ticket. Attach it to this ticket's proof — it is the input to the cutover decision.
12. Run the simplification pass (`n/a — docs-only` applies to a matrix-only change), record it under a dated `## Simplification pass` heading, then open the PR into `dev`.

## Acceptance criteria

- [ ] Every matrix row maps to exactly one owning slice handle, and every slice has at least one row.
- [ ] The status ladder rule is written down and applied; no row advances without its named evidence.
- [ ] `UAT owner` is a named person on every row that reads `UAT passed`.
- [ ] Deliberate exceptions read `legacy path retained` with a recorded reason, never `cut over`.
- [ ] Each slice PR carries a reviewed matrix diff; the reviewer checklist includes the check.
- [ ] A per-phase reconciliation pass is recorded, with the completeness report produced at the Phase 9 gate.
- [ ] Only one parity matrix exists in the repository.

## Verification

- [ ] `pwsh ./scripts/Test-DocumentationLinks.ps1` — expected: passes after every matrix edit; no broken proof or test link.
- [ ] `pwsh ./scripts/Test-MarkdownPlacement.ps1` — expected: passes; no new Markdown outside `docs/(prd|frd|adr|design|desktop)`.
- [ ] Kanmer `list_items` for area `desktop-features` — expected: every `DSK-05-nn` slice handle resolves to a board ticket and appears in the row mapping.
- [ ] Completeness report in the ticket proof — expected: every row's status and evidence listed, exceptions named with owning tickets.

## Evidence tier

Tier 12 — Integrated workflow.
Tier 12 obliges evidence from the authenticated source receipt through Core, SQL and the outbox, the actual Worker trigger, the adapter outcome, the persisted operator view, telemetry and safe replay — registration or mock-only paths do not satisfy it. A parity row may only claim `automated verification passed` or better when evidence of that kind is linked.

## Documentation changes

- `docs/desktop/01-inventory-and-parity/parity-matrix.md` — status, evidence links and UAT owner per row, maintained continuously
- `docs/desktop/05-implementation-and-migration/README.md` — no change; the phase table there stays the authority for the gates

## Guardrails

- **Azure**: no write.
- **Scope boundary**: documentation and Kanmer only. This ticket changes no code and must not edit a slice's implementation to make a row pass.
- **Traps**: a status advanced without evidence is worse than a row left behind — the matrix is the conversion's honesty record; capability IDs and ticket IDs are different namespaces (`CASE-17` is a capability, `CASE-017` a ticket), so use the `DSK-05-nn` plan handles; any new Markdown outside `docs/(prd|frd|adr|design|desktop)` fails the CI `documentation` job, so ticket-transient notes live in Kanmer; do not create a second matrix, and do not move this one — that decision belongs to [[DSK-00-12]].
- **Simplification pass** (`AGENTS.md` step 4): `n/a — docs-only` for a matrix-only change, still recorded under a dated `## Simplification pass` heading in the plan document.

## Outcome

_Filled at closeout._
