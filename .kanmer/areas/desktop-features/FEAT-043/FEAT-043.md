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
updated: '2026-08-24T11:08:22.394Z'
---

## What

Give each of the eleven upstream `report-decision` tickets a recorded disposition against locked decision L-03: which renderer templates ship with the desktop, which retire, what stays gated, and for each ticket either a recreated fork ticket or an explicit `unchanged-backlog` entry.

## Why

The carry-over triage in `docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md` classified eleven open upstream tickets as `report-decision` — "renderer/report decisions folded into the WebView2 rendering plan (ADR-0108)" — and routed them to area 07. Until each has a disposition, [[DSK-07-13]] cannot say which templates are in scope (only three of the seven governed templates are embedded today) and the programme carries eleven open questions that block the Phase 7 exit gate's "no required report depends on the web renderer unless explicitly retained". Three of the eleven are **not** open questions at all: `TICK-206`, `TICK-214` and `TICK-216` each carry a written plan and a closed `open-questions` document recording an operator decision already taken, so this ticket adopts and records them rather than re-deciding them — re-deciding a settled question is where uncertainty re-enters. This is a decision-recording ticket: it changes documents and creates tickets, and implements no renderer behaviour.

## Source of truth

- Plan row: `docs/desktop/07-integrations/README.md` § 5 — `DSK-07-17`
- Carry-over register: `docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md` § `Disposition categories` (the `report-decision` row and the DSK-01-09 recreation rule) and § `Triage table (109 open upstream tickets)` rows `DOCS-001` (line 118), `DOCS-003` (119), `DOCS-004` (120), `TICK-081` (125), `TICK-096` (126), `TICK-097` (127), `TICK-100` (128), `TICK-206` (129), `TICK-208` (130), `TICK-214` (131), `TICK-216` (132)
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 12.5 Documents, PDFs and reports, § 13.11 Future-compatible, not automatically in conversion scope, § 24 Phase 7
- Repository evidence: `docs/design/assets/report-renderer/templates/` — seven `.scriban` files (`advert_evidence_pack`, `assessment_fee_note`, `assessment_report`, `expert_report`, `fee_note`, `market_valuation_evidence`) plus `report.css`; `src/Pegasus.Infrastructure/Pegasus.Infrastructure.csproj` — only `assessment_report.scriban`, `assessment_fee_note.scriban` and `report.css` are embedded; `docs/capabilities.md` (the RPT and DOC family rows those tickets carry)
- Upstream evidence, read from the read-only upstream clone rather than summarised: `TICK-206` `plan` and `open-questions` (only the `rendererref1` assessment-report and fee-note families are activated; the twelve-entry legacy negative list; "no unresolved questions"), `TICK-214` `plan` and `open-questions` ("no surviving renderer MCPB host or distribution boundary"; ADR-0025's conditional MCPB possibility resolves to none), `TICK-216` `open-questions` (the operator's 2026-08-19 "all yes" on the exact `reference/rendererref1/` wording, named qualifications and the three bundled engineer signatures), `TICK-208` `plan` and `open-questions` (the append-only issued-version to Sent-evidence association; "no unresolved question for this ticket's preservation invariant"; CASE-23 parked to `TICK-055`)
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
2. Read each upstream ticket in full from the read-only upstream board clone referenced by the carry-over document — the body **and** its `plan`, `research` and `open-questions` documents, because for four of the eleven the decision lives in those documents and not in the body — and copy title, labels and the relevant text into the ticket's working notes. Do not disposition a ticket from its one-line summary.
3. Build the template scope table first, because four of the eleven turn on it. List all seven `.scriban` files under `docs/design/assets/report-renderer/templates/` and mark each: **embedded today** (`assessment_report`, `assessment_fee_note`, plus `report.css`), or **present but not embedded** (`advert_evidence_pack`, `expert_report`, `fee_note`, `market_valuation_evidence`). For each not embedded, record whether any code path references it.
4. For each of the seven templates record a disposition against L-03: **ships with the desktop renderer** (embedded by [[DSK-07-13]] and covered by [[DSK-07-15]] fixtures), **retires** (no capability needs it), or **stays gated** (retained in the governed source, not embedded, activated only when its own ticket lands). Name the capability id from `docs/capabilities.md` behind each.
5. Record `TICK-206` as **adopted, not re-decided** — its `open-questions` document reads "no unresolved questions" and its plan carries the operator's answer, so this step cites that answer rather than reaching a fresh one. The recorded decision: activate only the `rendererref1` assessment-report family — one closed typed operation over the four Core-owned outcomes `total_loss`, `repairable`, `cash_in_lieu`, `contract_repair` — plus its accepted fee-note artifact; RPT-01 carries the shared deterministic rendering mechanics, RPT-02 the assessment outcomes, fee note and itemised repair specification, EXT-08 the accepted-data activation with CASE-31/ENG-02 upstream; every other workspace catalogue entry stays inactive and non-discoverable, and no caller supplies or discovers a template identifier. Use TICK-206's twelve-entry negative list verbatim as the input to step 4's table — `market-valuation-evidence`, `advert-evidence-pack`, `fee-note` as a raw selector, `expert-report`, `blank-letterhead`, `repairable-contract-repair-report`, `total-loss-report`, `addendum-report`, `diminution-rebuttal`, `roadworthy-criminal-report`, `part-35-response`, `response-letter`. Then record the desktop-era addition upstream had no reason to state: none of the twelve is dispatchable from `Pegasus.Desktop.Infrastructure` either — an identifier being unavailable in the retained gateway renderer proves nothing about the client one. Write that negative assertion into the table as a named acceptance obligation on [[DSK-07-13]], which embeds the templates, and [[DSK-07-15]], which compares the two renderers; this ticket records the obligation and writes no test. Recreate `TICK-206` on the fork board only if work remains after the table is written.
6. Record `TICK-216` as **adopted, not re-decided**: the Collision Engineers operator answered it on 2026-08-19 ("all yes"), and its `open-questions` document carries that answer ticked. The accepted contract is the exact `reference/rendererref1/` assessment-report wording, its named qualifications, and all three bundled engineer signatures — Andy Patterson, Ed Mawdsley and Neil O'Reilly — for active draft generation, provided the selected engineer's name, qualification and signature match as one tuple, a missing, unknown, mismatched or substituted value fails closed, and human approval is still required before issue; wording absent from the supplied evidence stays unavailable and must not be invented, and Audit, diminution and addendum wording stay outside the acceptance until their own templates are approved. Then state the consequence the desktop adds, which upstream did not have to weigh: an asset embedded in a desktop assembly ships to **every workstation** inside the MSIX, a different exposure from an asset inside a server container. Record which signature assets the acceptance authorises for embedding — all three — and hand that list to [[DSK-07-13]], which today embeds only `andy_patterson.png`. If the exposure difference itself needs a fresh operator answer, raise that as an open question; do not re-ask the 2026-08-19 question.
7. Recreate `TICK-208` (preserve final Sent evidence through post-report correction) on the fork board **unconditionally**, in fork area `documents-reports` under the step 10 rule. The defect exists regardless of what the desktop finalise path does — Core carries one `ReportApprovalId` and one `ReportSentEvidenceId` per case, so a correction risks replacing the earlier pointer and an unlink clears the evidence row's Case and link metadata — and under D-001 nobody upstream will fix it after the freeze. Its plan is written and its `open-questions` document is closed (CASE-23 is explicitly parked to `TICK-055`), so nothing here needs deciding. Sequence it after the recreated `DOCS-001`, whose report-version identity types it reuses, and record on the new ticket that [[DSK-07-16]] step 11 — issued versions with custody state and sent evidence as separate columns — is not implementable until this ledger exists. Do not make the recreation conditional on the desktop path changing something.
8. Record `TICK-214` as **answered, not open**: its plan and `open-questions` document carry the operator's binding direction — no renderer MCPB host or distribution boundary survives, resolving ADR-0025's conditional MCPB possibility to "none". For the desktop era ADR-0108 supersedes the question outright: rendering is an isolated WebView2 path inside the desktop assembly, not a packaged renderer product; and the gateway's `/mcp` Automation surface is unchanged (parity row `PAR-46`). Record it as `unchanged-backlog` with that answer written out, rather than as a routing question for area 09 or area 12. Then add the one check that genuinely remains: after [[DSK-01-10]]'s sync, verify that no `CollisionRenderer.Mcp` project, MCPB manifest, stdio renderer host or browser bootstrap has arrived in the fork tree and that `src/Pegasus.Web/Mcp/` has gained no renderer tool or route; record the result. A future report-status Automation tool is explicitly parked and needs its own caller-backed ticket.
9. Disposition `DOCS-001`, `DOCS-003`, `DOCS-004`, `TICK-081`, `TICK-096`, `TICK-097` and `TICK-100` as either `report-decision` work now recreated on the fork board, or `unchanged-backlog` under proposal § 13.11. Each of the blocked/post-alpha ones needs one sentence saying what would unblock it — "blocked" without a condition is not a disposition.
10. Apply the recreation rule from DSK-01-09 for every ticket that becomes fork work: create it in fork area `documents-reports`, `refs` containing `upstream:<ID>`, the original body copied verbatim, upstream labels kept, and a link to this area plan. Do not silently rewrite an upstream body.
11. Update `docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md` so each of the eleven rows carries its recorded disposition and, where applicable, the fork ticket id. Update `docs/desktop/07-integrations/README.md` § 8 if the template scope changed what that section promises.
12. Run `pwsh ./scripts/Test-DocumentationLinks.ps1` and `pwsh ./scripts/Test-MarkdownPlacement.ps1`; both must pass. List every open question that survived in the ticket's `open-questions` document — an unticked item blocks the move, which is correct here. Then open the PR into `dev`.

## Acceptance criteria

- [ ] All eleven upstream tickets have a recorded disposition naming L-03 and, where relevant, the capability id.
- [ ] `TICK-206`, `TICK-214` and `TICK-216` are recorded as decisions **already taken upstream**, each cited from its own `plan` and `open-questions` document, and none of the three is re-decided here.
- [ ] `TICK-208` is recreated on the fork board unconditionally, sequenced after the recreated `DOCS-001`, and carries the note that [[DSK-07-16]]'s separate issued-version and sent-evidence columns depend on it.
- [ ] A template scope table exists covering all seven `.scriban` files plus `report.css`, each marked ships / retires / stays gated.
- [ ] The table carries TICK-206's twelve-entry legacy negative list and the recorded requirement that none of the twelve is dispatchable from `Pegasus.Desktop.Infrastructure` either, with [[DSK-07-13]] and [[DSK-07-15]] named as the owners of that assertion.
- [ ] The `TICK-216` record names all three authorised signature assets and states the desktop-package exposure consequence.
- [ ] The `TICK-214` record states the answer — no renderer MCPB host or distribution boundary survives, superseded for the desktop by ADR-0108 — and carries the post-sync check that none of its retired surfaces has returned.
- [ ] Every ticket that becomes fork work is recreated under the DSK-01-09 rule with `upstream:<ID>` in `refs` and its body copied verbatim.
- [ ] Every `unchanged-backlog` disposition states the condition that would activate it.
- [ ] The carry-over register shows each disposition and any fork ticket id.
- [ ] No renderer code or template file is changed by this ticket.

## Verification

- [ ] `pwsh ./scripts/Test-DocumentationLinks.ps1` — expected: exit 0.
- [ ] `pwsh ./scripts/Test-MarkdownPlacement.ps1` — expected: exit 0.
- [ ] `grep -c "report-decision" docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md` — expected: every one of the eleven rows still classified and now annotated with its disposition.
- [ ] `git ls-files | grep -Ei "collisionrenderer\.mcp|\.mcpb$|mcpb"` — expected: no output; upstream `TICK-214`'s retired renderer host, manifest and bundle surfaces are absent from the fork tree after the sync.
- [ ] Kanmer `search_items` for `upstream:TICK-208` — expected: exactly one recreated fork ticket in `documents-reports`, created unconditionally.
- [ ] Kanmer `search_items` for `upstream:` — expected: each recreated ticket is findable by its upstream id.
- [ ] `git diff --stat origin/dev -- src docs/design` — expected: empty output.

## Evidence tier

Tier 1 — Static/build/architecture.
Tier 1 obliges documentation-consistency evidence only: the register is complete, links resolve, placement passes, and no source or governed asset changed.

## Documentation changes

- `docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md` — dispositions and fork ticket ids for the eleven rows, including the three recorded-upstream-decision entries and the unconditional `TICK-208` recreation
- `docs/desktop/07-integrations/README.md` § 8 — template scope, if step 4 changed it, including TICK-206's twelve-entry negative list and the `Pegasus.Desktop.Infrastructure` non-dispatch requirement
- `docs/capabilities.md` — only if a capability's canonical owner changes as a consequence

## Guardrails

- **Azure**: no write.
- **Scope boundary**: documentation and Kanmer tickets only. Must not edit a `.scriban` template, `report.css`, a `.csproj`, or any renderer source — the negative-dispatch assertion recorded in step 5 is written as an obligation on [[DSK-07-13]] and [[DSK-07-15]], not as a test here. Must not archive or modify an upstream ticket — the upstream remote is read-only and the fork never pushes to it.
- **Traps**: proposal § 13.11 — do not smuggle post-alpha report capabilities into parity; a body rewritten during recreation loses the upstream evidence trail; "blocked" without a named unblocking condition is not a disposition; a decision already recorded upstream is adopted and cited, never re-taken — re-deciding `TICK-206`, `TICK-214` or `TICK-216` from their one-line bodies is a stop condition, because the answer lives in their pipeline documents; any new `.md` outside `docs/(prd|frd|adr|design|desktop)` fails the CI `documentation` job, so ticket-transient notes live in Kanmer; check `docs/adr/README.md` after every upstream sync for ADR collisions.
- **Simplification pass** (`AGENTS.md` step 4): `n/a — docs-only`.

## Outcome

_Filled at closeout._
