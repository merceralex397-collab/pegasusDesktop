---
id: FEAT-043
type: ticket
title: >-
  DSK-07-17 · Reconcile the eleven upstream report-decision tickets against L-03
  and record each disposition
status: backlog
area: desktop-features
assignee: ''
profile: chore
labels:
  - desktop-conversion
  - plan-07
  - phase-7
  - tier-1
groups:
  - EPIC-008
  - HZN-008
links: []
docs_todo: true
archived: false
created: '2026-08-24T08:30:09.678Z'
updated: '2026-08-24T08:30:09.678Z'
---

## What

Give each of the eleven upstream `report-decision` tickets a recorded disposition against locked decision L-03: which renderer templates ship with the desktop, which retire, what stays gated, and for each ticket either a recreated fork ticket or an explicit `unchanged-backlog` entry.

## Why

The carry-over triage in `docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md` classified eleven open upstream tickets as `report-decision` — "renderer/report decisions folded into the WebView2 rendering plan (ADR-0108)" — and routed them to area 07. Until each has a disposition, [[DSK-07-13]] cannot say which templates are in scope (only three of the seven governed templates are embedded today) and the programme carries eleven open questions that block the Phase 7 exit gate's "no required report depends on the web renderer unless explicitly retained". This is a decision-recording ticket: it changes documents and creates tickets, and implements no renderer behaviour.

## Source of truth

- Plan row: `docs/desktop/07-integrations/README.md` § 5 — `DSK-07-17`
- Carry-over register: `docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md` § `Disposition categories` (the `report-decision` row and the DSK-01-09 recreation rule) and § `Triage table (109 open upstream tickets)` rows `DOCS-001` (line 118), `DOCS-003` (119), `DOCS-004` (120), `TICK-081` (125), `TICK-096` (126), `TICK-097` (127), `TICK-100` (128), `TICK-206` (129), `TICK-208` (130), `TICK-214` (131), `TICK-216` (132)
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 12.5 Documents, PDFs and reports, § 13.11 Future-compatible, not automatically in conversion scope, § 24 Phase 7
- Repository evidence: `docs/design/assets/report-renderer/templates/` — seven `.scriban` files (`advert_evidence_pack`, `assessment_fee_note`, `assessment_report`, `expert_report`, `fee_note`, `market_valuation_evidence`) plus `report.css`; `src/Pegasus.Infrastructure/Pegasus.Infrastructure.csproj` — only `assessment_report.scriban`, `assessment_fee_note.scriban` and `report.css` are embedded; `docs/capabilities.md` (the RPT and DOC family rows those tickets carry)
- Binding decisions: **L-03** — report rendering moves to the isolated WebView2 path and the gateway renderer is retained until parity; every disposition is judged against it. Proposal § 13.11 — post-alpha capabilities are not smuggled into "feature parity". D-001 — the fork becomes the single release source, so a ticket left upstream must be one nobody needs during the conversion.
- Depends on: `DSK-07-12` the ADR-0108 text each disposition cites; `DSK-01-09` the carry-over recreation rule and its `upstream:<ID>` `refs` convention

## Routing

- **Subagent**: `pegasus-parity-researcher` — `.codex/agents/pegasus-parity-researcher.toml`
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `kanmer-tickets` (`.grok/skills/kanmer-tickets/SKILL.md`) → `kanmer-docs` (`.grok/skills/kanmer-docs/SKILL.md`)
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `search_items`, `get_item`, `create_item`, `link_doc`, `set_ticket_doc`, `append_scratch`, `move_item`)
- **Kanmer pipeline** for profile `chore`: `kanmer-plan` → `kanmer-execute` → `kanmer-verify` → `kanmer-closeout` (gates are `leave-preparing` — plan plus questions-resolved — and `enter-done` — proof plus questions-resolved; call `get_doc_gates <id>` before every move)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Orient: read the plan row, the carry-over register's disposition categories and the eleven rows named above, and ADR-0108 from [[DSK-07-12]]. Call `get_doc_gates <this ticket id>`, then `take_ticket` on branch `task/dsk-07-17-report-decision-dispositions`.
2. Read each upstream ticket in full from the read-only upstream board clone referenced by the carry-over document, and copy its title, labels and body into the ticket's working notes. Do not disposition a ticket from its one-line summary.
3. Build the template scope table first, because four of the eleven turn on it. List all seven `.scriban` files under `docs/design/assets/report-renderer/templates/` and mark each: **embedded today** (`assessment_report`, `assessment_fee_note`, plus `report.css`), or **present but not embedded** (`advert_evidence_pack`, `expert_report`, `fee_note`, `market_valuation_evidence`). For each not embedded, record whether any code path references it.
4. For each of the seven templates record a disposition against L-03: **ships with the desktop renderer** (embedded by [[DSK-07-13]] and covered by [[DSK-07-15]] fixtures), **retires** (no capability needs it), or **stays gated** (retained in the governed source, not embedded, activated only when its own ticket lands). Name the capability id from `docs/capabilities.md` behind each.
5. Disposition `TICK-206` (map renderer templates to capabilities and decide proposed retirements) directly from step 4's table — it *is* that decision. Record the outcome and recreate it on the fork board only if work remains after the table is written.
6. Disposition `TICK-216` (whether unaccepted wording and signature assets may ship behind a closed gate) explicitly against the desktop package: an asset embedded in the desktop assembly ships to every workstation, which is a different exposure from an asset inside a server container. Record the decision and the reasoning; if it needs the operator, raise it as an open question rather than assuming.
7. Disposition `TICK-208` (preserve final Sent evidence through post-report correction) against FRD-11 and [[DSK-07-16]]: state whether the desktop finalise path changes anything about it, and recreate it as a fork ticket in `documents-reports` if it does.
8. Disposition `TICK-214` (long-term MCPB host and distribution boundary) — check whether it is genuinely a report decision or belongs to area 12 / area 09; if it is misfiled, correct the carry-over row's target and say so rather than forcing a report disposition on it.
9. Disposition `DOCS-001`, `DOCS-003`, `DOCS-004`, `TICK-081`, `TICK-096`, `TICK-097` and `TICK-100` as either `report-decision` work now recreated on the fork board, or `unchanged-backlog` under proposal § 13.11. Each of the blocked/post-alpha ones needs one sentence saying what would unblock it — "blocked" without a condition is not a disposition.
10. Apply the recreation rule from DSK-01-09 for every ticket that becomes fork work: create it in fork area `documents-reports`, `refs` containing `upstream:<ID>`, the original body copied verbatim, upstream labels kept, and a link to this area plan. Do not silently rewrite an upstream body.
11. Update `docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md` so each of the eleven rows carries its recorded disposition and, where applicable, the fork ticket id. Update `docs/desktop/07-integrations/README.md` § 8 if the template scope changed what that section promises.
12. Run `pwsh ./scripts/Test-DocumentationLinks.ps1` and `pwsh ./scripts/Test-MarkdownPlacement.ps1`; both must pass. List every open question that survived in the ticket's `open-questions` document — an unticked item blocks the move, which is correct here. Then open the PR into `dev`.

## Acceptance criteria

- [ ] All eleven upstream tickets have a recorded disposition naming L-03 and, where relevant, the capability id.
- [ ] A template scope table exists covering all seven `.scriban` files plus `report.css`, each marked ships / retires / stays gated.
- [ ] Every ticket that becomes fork work is recreated under the DSK-01-09 rule with `upstream:<ID>` in `refs` and its body copied verbatim.
- [ ] Every `unchanged-backlog` disposition states the condition that would activate it.
- [ ] The carry-over register shows each disposition and any fork ticket id.
- [ ] No renderer code or template file is changed by this ticket.

## Verification

- [ ] `pwsh ./scripts/Test-DocumentationLinks.ps1` — expected: exit 0.
- [ ] `pwsh ./scripts/Test-MarkdownPlacement.ps1` — expected: exit 0.
- [ ] `grep -c "report-decision" docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md` — expected: every one of the eleven rows still classified and now annotated with its disposition.
- [ ] Kanmer `search_items` for `upstream:` — expected: each recreated ticket is findable by its upstream id.
- [ ] `git diff --stat origin/dev -- src docs/design` — expected: empty output.

## Evidence tier

Tier 1 — Static/build/architecture.
Tier 1 obliges documentation-consistency evidence only: the register is complete, links resolve, placement passes, and no source or governed asset changed.

## Documentation changes

- `docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md` — dispositions and fork ticket ids for the eleven rows
- `docs/desktop/07-integrations/README.md` § 8 — template scope, if step 4 changed it
- `docs/capabilities.md` — only if a capability's canonical owner changes as a consequence

## Guardrails

- **Azure**: no write.
- **Scope boundary**: documentation and Kanmer tickets only. Must not edit a `.scriban` template, `report.css`, a `.csproj`, or any renderer source. Must not archive or modify an upstream ticket — the upstream remote is read-only and the fork never pushes to it.
- **Traps**: proposal § 13.11 — do not smuggle post-alpha report capabilities into parity; a body rewritten during recreation loses the upstream evidence trail; "blocked" without a named unblocking condition is not a disposition; any new `.md` outside `docs/(prd|frd|adr|design|desktop)` fails the CI `documentation` job, so ticket-transient notes live in Kanmer; check `docs/adr/README.md` after every upstream sync for ADR collisions.
- **Simplification pass** (`AGENTS.md` step 4): `n/a — docs-only`.

## Outcome

_Filled at closeout._
