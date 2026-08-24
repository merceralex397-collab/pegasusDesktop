---
id: PLAT-020
type: ticket
title: DSK-11-02 · Cloud-dependency records (Appendix B) for every capability
status: preparing
area: platform-operations
assignee: ''
profile: chore
stageEntered:
  preparing: '2026-08-24T21:21:16.154Z'
labels:
  - desktop-conversion
  - plan-11
  - phase-0
  - tier-1
groups:
  - EPIC-012
  - HZN-001
links: []
blocks:
  - PLAT-026
docs_todo: true
archived: false
created: '2026-08-24T08:26:57.618Z'
updated: '2026-08-24T21:21:16.154Z'
---

## What

Complete the nine cloud-dependency records (proposal Appendix B) — `graph-intake`, `box-custody`, `dvla-dvsa-lookup`, `report-rendering`, `authentication-session`, `update-feed`, `telemetry`, `database`, `transport-queues-and-blobs` — so each carries the six cloud-justification answers with evidence, its current resources, its desktop and cloud components, the data it owns, its failure mode, its monitoring signal and its deprovision-candidate answer.

## Why

Proposal §4 makes the six-question cloud-justification test mandatory for every cloud-hosted responsibility and §4.1 turns it into a placement table; Appendix B is where those answers become durable records. Without them, ADR-0101 (cloud split and justification test) has no evidence section, and the Phase 10 deprovision checklist ([[DSK-11-08]]) has no per-capability basis for saying a resource may go. Operator-visible consequence: "the web app does it" and "it is already in Azure" — explicitly rejected by §4 — become the default answers again.

Siblings: [[DSK-11-01]] supplies the verified resource names each record cites; [[DSK-11-08]] consumes the `deprovision_candidate` field; the ADR-0101 authoring ticket in plan 00 ([[DSK-00-05]]) copies the completed table.

## Source of truth

- Plan row: `docs/desktop/11-azure-disposition/README.md` § 5 — `DSK-11-02`
- Plan detail: `docs/desktop/11-azure-disposition/README.md` § 5 "Cloud-dependency records (Appendix B, drafts)" — the nine YAML blocks to complete, verbatim
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 4 The mandatory cloud-justification test, § 4.1 Placement decisions (the 20-row table each record must agree with), Appendix B Cloud dependency record
- Cloud-justification table shape: `docs/desktop/00-governance-and-workflow/README.md` § 3 — the six questions with an Answer and an Evidence column, used verbatim in every ADR and every record
- Repository evidence:
  - `infra/modules/platform.bicep:100`/`:154` the two storage accounts, `:85` Key Vault, `:195`/`:214` SQL, `:354` Web container app, `:489` Worker Function App, `:46`/`:56` Log Analytics and App Insights — the resource names each record cites
  - `src/Pegasus.Web/Program.cs:130-176` required production settings (which capability needs which resource)
  - `docs/desktop/01-inventory-and-parity/flow-records.md` — the authentication, database, Graph intake, Box custody, DVLA/DVSA and report-rendering flow records that supply the evidence sentences
  - `docs/desktop/01-inventory-and-parity/azure-resource-register.md` — the verified `current_resources` values
- Binding decisions:
  - **L-01** — the gateway is `Pegasus.Web` evolved in place, so no record may name a new deployment unit.
  - **L-03** — report rendering moves to an isolated non-UI WebView2 path and the gateway renderer is retained until golden-file parity passes; the `report-rendering` record must say exactly that.
  - **D-002 / D-003** — the `update-feed` record's `current_resources` is **no longer open**: the certificate is self-managed in-house and the feed is an in-house UNC share, so the record names no Azure resource at all.
- Depends on: `DSK-11-01` — the records cite resource names and "used by" code paths the register verifies.

## Routing

- **Subagent**: `pegasus-azure-auditor` — `.codex/agents/pegasus-azure-auditor.toml` (read-only)
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `kanmer-plan` (`.grok/skills/kanmer-plan/SKILL.md`) → `kanmer-docs` (`.grok/skills/kanmer-docs/SKILL.md`, only if the records move to their own file)
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`); Azure MCP **read-only** (`group_resource_list`, `storage`, `keyvault`, `sql`, `containerapps`, `functionapp`) only to confirm a resource name; Microsoft Learn (`microsoft_docs_search`) for service semantics
- **Kanmer pipeline** for profile `chore`: `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout`; gates are `plan` + `questions-resolved` to leave `preparing` and `proof` + `questions-resolved` to enter `done`. Call `get_doc_gates <this ticket id>` before every move.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Orientation. Read `docs/desktop/11-azure-disposition/README.md` § 5 "Cloud-dependency records", proposal § 4 and § 4.1, and `docs/desktop/00-governance-and-workflow/README.md` § 3 cloud-justification table. Then `get_doc_gates <this ticket id>` and `take_ticket <this ticket id>`.
2. **Decide and record the home** for the finished records: either complete them in place under `docs/desktop/11-azure-disposition/README.md` § "Cloud-dependency records", or move them to a new `docs/desktop/11-azure-disposition/cloud-dependency-records.md` and link it from that README section. Both paths are inside the allowed markdown root `docs/(prd|frd|adr|design|desktop)`; anything outside it fails the CI `documentation` job. Record the choice in the ticket plan document — the plan set leaves it open.
3. Copy the nine YAML blocks from the plan **verbatim** as the starting text. Do not paraphrase them and do not renumber the keys: `capability`, `current_resources`, `desktop_components`, `cloud_components`, `reason_cloud` (six booleans), `data_owned`, `failure_mode`, `monitoring`, `deprovision_candidate`.
4. For each record, replace every `reason_cloud` boolean with the boolean **plus a one-line evidence citation** (`file:line`, a flow record heading, or a proposal §4.1 row). The six keys map to the §4 questions in this order: `shared_authority`, `unattended_execution`, `protected_credentials`, `public_callback`, `central_enforcement`, `measured_operational_advantage`. A record where all six are false belongs in the desktop and must say so.
5. Reconcile every record against the §4.1 placement table (proposal lines 140–165). Where a record and the table disagree, the record wins only with evidence, and the disagreement is written down as a `Deviation:` line — never silently.
6. Replace the `current_resources` of each record with the verified names from `docs/desktop/01-inventory-and-parity/azure-resource-register.md` (for example `pegasus-prod-worker-252ow37gij`, `pegasus-prod-sql-252ow37gij/pegasus`, `pegasusprodkv252ow37g`), not the `<suffix>` patterns.
7. Correct the `update-feed` record, which is the one stale draft: it still reads `current_resources: []   # D-003 open` and `cloud_components: [feed host (TBD)]`. D-003 was decided on 2026-08-23 in favour of an in-house UNC file share served to App Installer over SMB, and D-002 chose a self-managed certificate — so the record must state `current_resources: []   # no Azure resource — in-house UNC share (D-003, 2026-08-23)`, name the UNC host as a non-Azure dependency, and keep `failure_mode: [feed-unreachable-fail-open (gateway gate fails closed)]`.
8. Complete the `report-rendering` record against L-03/ADR-0108: desktop `WebView2Renderer`, cloud `PlaywrightAssessmentReportRenderer` retained **until golden-file parity passes**, and `deprovision_candidate: false` with the comment that the renderer becomes removable from the Web image after parity — which would supersede ADR-0028 (`infra/modules/platform.bicep:436-445` records why the container is sized for in-process Chromium).
9. Give every record a `monitoring` value that names a signal somebody can actually query today, and mark the ones that cannot be observed during the capped Log Analytics window (PLAT-034, `docs/current-architecture.md:160-175`) so [[DSK-11-08]] does not rely on them.
10. Cross-check completeness: `grep -c "^capability:" <the records file>` must return `9`, and every capability named in `docs/desktop/11-azure-disposition/README.md` § 5 must appear exactly once.
11. Run `pwsh ./scripts/Test-DocumentationLinks.ps1` and `pwsh ./scripts/Test-TestMarkdownPlacement.ps1`; both must exit 0.
12. Simplification pass (`AGENTS.md` step 4, `n/a — docs-only` is the expected entry), then write `proof` and hand to `pegasus-desktop-reviewer`, who checks each of the 54 answers (nine records × six questions) against its cited evidence.

## Acceptance criteria

- [ ] Nine records exist, one per capability listed in the plan, each with all nine keys present.
- [ ] Every `reason_cloud` answer carries an evidence citation; no answer is justified by "already in Azure", "the web app does it" or "it may scale later".
- [ ] Every `current_resources` entry names a resource verified in the register, or explicitly records that the capability uses none.
- [ ] The `update-feed` record reflects D-003 (in-house UNC share, no Azure resource) and D-002 (self-managed certificate); no "TBD" remains anywhere in the nine records.
- [ ] Every record's `deprovision_candidate` is `false`, or `candidate` with the removal condition and the words "not before cutover".
- [ ] Any disagreement with proposal §4.1 is written as a `Deviation:` line with its reason.

## Verification

- [ ] `grep -c "^capability:" docs/desktop/11-azure-disposition/<records file>` — expected: `9`.
- [ ] `grep -rn "TBD\|D-003 open" docs/desktop/11-azure-disposition/` — expected: no match in the records.
- [ ] `pwsh ./scripts/Test-DocumentationLinks.ps1` — expected: exits 0.
- [ ] `pwsh ./scripts/Test-TestMarkdownPlacement.ps1` — expected: exits 0.
- [ ] Reviewer walkthrough recorded in the ticket — expected: each of the six answers per record traced to its cited file, line or flow record.

## Evidence tier

Tier 1 — Static/build/architecture. The obligation is documentary consistency only: the records compile against the register, the placement table and the flow records, and the documentation gates pass. No runtime behaviour is proved here.

## Documentation changes

- `docs/desktop/11-azure-disposition/README.md` § Cloud-dependency records — completed in place, or replaced by a link to `docs/desktop/11-azure-disposition/cloud-dependency-records.md`.
- ADR-0101 (cloud split and the justification test) is the governing document these records feed; it does not exist yet, hence `docs_todo`.

## Guardrails

- **Azure**: no write. Reads are free and need no per-target approval (`docs/runbook.md` § Live-operation approval matrix); any write is a marked ⚠ Azure write needing exact-target approval and is mirrored in `docs/desktop/11-azure-disposition/README.md` § Conditional Azure writes. **Nothing is deprovisioned before cutover, observed non-use and rollback approval** — a record may say "candidate", never "remove".
- **Scope boundary**: documentation under `docs/desktop/11-azure-disposition/` only. Do not edit `infra/`, `src/`, ADR files (ADR authoring is [[DSK-00-05]]'s job) or `docs/operations.md`.
- **Open question to carry, not invent**: the plan does not state whether the records live in the area README or in their own file — step 2 decides and records it rather than assuming.
- **Traps** (plan § 7): telemetry blind spots (PLAT-034) make `monitoring` values unreliable for most of the working day, so mark them; a service is not "unused" merely because no developer remembers it (proposal §19.2).
- **Board placement**: this plan area seeds into `platform-operations` because the board-shape table in `docs/desktop/00-governance-and-workflow/README.md` § Kanmer board shape assigns no area to plan 11.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document (`n/a — docs-only` for documentation-only tickets).

## Outcome

_Filled at closeout._
