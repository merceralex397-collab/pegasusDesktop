# Historical Kanmer carry-over — in-repository disposition

The original repository `collisionengineers/pegasus` was not a finished
project at the planning baseline: its Kanmer board carried open work, and its
`main` kept moving. This document records that historical triage once so the
fork board could be seeded with what mattered. It is provenance, not a live
remote or synchronization instruction. The operator has prohibited all
upstream synchronization for the current refactor; the configured
`pegasusDesktop` remote and live Kanmer board are the only current sources.
Source: historical read-only clone of
`collisionengineers/pegasus` branch `kanmer-board` at `4694067`
("chore(kanmer): sync board 2026-08-23T15:51:00.775Z"), read 2026-08-23.
Ticket DSK-01-09 executed the dispositions. DSK-01-10's historical first-sync
step is superseded by the current operator boundary and must not be executed.

## Upstream board shape

| Area id | Name | Prefix | Open (non-archived, not done) |
| --- | --- | --- | --- |
| `mail-communications` | Mail & Communications | MAIL | 4 |
| `automation-integrations` | Automation & Integrations | AUTO | 22 |
| `documents-reports` | Documents & Reports | DOCS | 15 |
| `engineering-assessment` | Engineering & Assessment | ENG | 17 |
| `intake-processing` | Intake & Processing | INTK | 15 |
| `platform-operations` | Platform & Operations | PLAT | 19 |
| `delivery-repository` | Delivery & Repository | DELIV | 2 |
| `case-reference-workflow` | Case & Reference Workflow | CASE | 13 |
| `kanmer-meta` | Kanmer Meta | KANMER | 0 |
| `pr-review` | PR Review | PR | 2 |

Totals: 456 tickets; 233 done, 192 backlog, 24 preparing, 5 implementing,
2 verifying; 114 archived. **109 open and non-archived** tickets are
triaged below. Legacy `TICK-nnn` IDs (counter 221) predate the area
prefixes and sit inside the areas above. Counters of note: `PR` 54,
`PLAT` 41, `INTK` 33, `CASE` 22, `DELIV` 16, `SIMPLI` 15 (all archived or
done), `ENG` 13, `MAIL` 12, `DOCS` 12, `AUTO` 8, `KANMER` 5, `EPIC` 8,
`HZN` 3.

Groups:

| Group | Title | Members |
| --- | --- | --- |
| EPIC-001 | CI and repository safeguards independent of UI revamp | 5 |
| EPIC-002 | Simplification and boundary cleanup | 22 |
| EPIC-003 | Cross-domain UI revamp | 43 |
| EPIC-004 | CollisionRenderer monolith integration | 36 |
| EPIC-005 | AI and Automation Actor disposition | 49 |
| EPIC-006 | Email management workspace | 43 |
| EPIC-007 | Grouped upload, vehicle-image routing, and Unidentified intake | 4 |
| EPIC-008 | Pegasus administration and workspace redesign | 10 |
| HZN-001 | Retire stale NOW.md ticket references | 1 |
| HZN-002 | External integration roadmap | 25 |
| HZN-003 | QDOS alpha cutover | 62 |

Profiles: upstream `feature` gates are `governing-doc` (leave backlog),
`research, files, plan, checklist` (leave preparing),
`post-implementation-report` (enter review), `proof` (enter done); `fix`
= files, plan / proof; `chore` = plan / proof; `spike` = research. The
**fork board adds `questions-resolved`** to every gated move. Carried-over
tickets follow the **fork** profile set; `get_doc_gates` is authoritative.

## Disposition categories

| Disposition | Meaning | Where it lands |
| --- | --- | --- |
| `desktop-screen-spec` | A web UI redesign/fix whose intent becomes a desktop screen specification; it is **not** rebuilt in Razor | Area 06 screen specs (and area 05 slice that ships the screen); fork area `desktop-ui` (DUI) |
| `gateway-worker-ticket` | A defect or hygiene item in Core/Infrastructure/Web/Worker that matters regardless of UI | The named area plan; recreated in the same upstream domain area on the fork board |
| `report-decision` | Renderer/report decisions folded into the WebView2 rendering plan (ADR-0108) | Area 07 reports section; fork area `documents-reports` |
| `unchanged-backlog` | Post-alpha capabilities outside conversion scope (proposal §13.11) | Listed here; **not recreated** until their own horizon; their capability rows stay in `docs/capabilities.md` |

Recreation rule (DSK-01-09): each `desktop-screen-spec`,
`gateway-worker-ticket`, and `report-decision` ticket is recreated on the
fork board in the fork area named below, with `refs` containing the
upstream ID (`upstream:<ID>`), the original body copied verbatim into the
ticket body, the upstream labels kept, and a link to the owning area plan.
The 233 done and 114 archived historical tickets are **not** recreated — their
history remains in this document as provenance. `unchanged-backlog` tickets are
not recreated either; the table is their historical register until an
in-repository decision activates or archives them. No upstream remote is
consulted to interpret this record.

## Triage table (109 open upstream tickets)

| Upstream ID | Upstream area | Status | Profile | Labels | Title | Disposition | Target area plan | Fork area |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| AUTO-003 | automation-integrations | preparing | feature | follow-up, MCP-05, mail-workspace, post-alpha | Expose the completed email-workspace actions through the Automation Actor | gateway-worker-ticket | 03 (MCP/API parity), 07 | automation-integrations |
| AUTO-006 | automation-integrations | backlog | feature | ui, redesign, automation, operator-requested | Redesign the Automation workspace | desktop-screen-spec | 06 (Administration › Automation) | desktop-ui |
| AUTO-007 | automation-integrations | backlog | feature | ui, redesign, ai, settings, operator-requested | Redesign AI Settings | desktop-screen-spec | 06 (Administration › AI settings) | desktop-ui |
| AUTO-008 | automation-integrations | preparing | spike | performance, intake, provider-api, research | Measure and reduce durable intake processing latency | gateway-worker-ticket | 10 (performance baseline) | automation-integrations |
| KANMER-005 | automation-integrations | backlog | fix | bug, lease, concurrency | Enforce exclusive editing leases between staff and Automation Actors | gateway-worker-ticket | 03 (concurrency) | automation-integrations |
| TICK-058 | automation-integrations | preparing | feature | capability, API-01, next, post-alpha, blocked | API-01 — Principal-scoped provider submission API | unchanged-backlog | — | automation-integrations |
| TICK-060 | automation-integrations | preparing | feature | capability, API-03, next, post-alpha, blocked | API-03 — Return the provider's resulting Case/PO or fail | unchanged-backlog | — | automation-integrations |
| TICK-061 | automation-integrations | preparing | feature | capability, API-04, next, post-alpha, blocked | API-04 — Issue, reset, revoke, pause, and resume provider credentials | unchanged-backlog | — | automation-integrations |
| TICK-063 | automation-integrations | backlog | feature | capability, AI-05, later, post-alpha, blocked | AI-05 — Automatic AI-assisted image readiness assessment of the current Case image set | unchanged-backlog | — | automation-integrations |
| TICK-069 | automation-integrations | preparing | feature | capability, EXT-15, later, post-alpha, blocked | EXT-15 — Automated WhatsApp ingestion and coexistence | unchanged-backlog | — | automation-integrations |
| TICK-070 | automation-integrations | backlog | feature | capability, AI-01, later, post-alpha, blocked | AI-01 — In-app staff AI assistant | unchanged-backlog | — | automation-integrations |
| TICK-071 | automation-integrations | backlog | feature | capability, AI-02, later, post-alpha, blocked | AI-02 — AI-assisted email identification/classification | unchanged-backlog | — | automation-integrations |
| TICK-072 | automation-integrations | backlog | feature | capability, AI-03, later, post-alpha, blocked | AI-03 — AI-assisted suggested email actions | unchanged-backlog | — | automation-integrations |
| TICK-073 | automation-integrations | backlog | feature | capability, AI-04, later, post-alpha, blocked | AI-04 — AI-assisted document extraction and operator review | unchanged-backlog | — | automation-integrations |
| TICK-074 | automation-integrations | backlog | feature | capability, AI-06, later, post-alpha, blocked | AI-06 — AI-assisted inspection-address suggestions | unchanged-backlog | — | automation-integrations |
| TICK-087 | automation-integrations | backlog | feature | capability, AI-07, later, post-alpha, blocked | AI-07 — Staff-selected AI Assessor Engineer option in the post-EVA-replacement assignment workflow | unchanged-backlog | — | automation-integrations |
| TICK-089 | automation-integrations | backlog | feature | capability, EXT-16, later, post-alpha, blocked | EXT-16 — Collision Engineers guided mobile image capture | unchanged-backlog | — | automation-integrations |
| TICK-090 | automation-integrations | backlog | feature | capability, EXT-17, later, post-alpha, blocked | EXT-17 — Tractable or Ravin guided-capture integration | unchanged-backlog | — | automation-integrations |
| TICK-091 | automation-integrations | backlog | feature | capability, EXT-19, later, post-alpha, blocked | EXT-19 — Collision Engineers custom application domain | unchanged-backlog | — | automation-integrations |
| TICK-101 | automation-integrations | backlog | feature | capability, AI-08, later, post-alpha, blocked | AI-08 — Intended Microsoft Foundry candidate proposes a case-grounded query response in approved house style | unchanged-backlog | — | automation-integrations |
| TICK-102 | automation-integrations | verifying | feature | capability, AI-09, now, requires-live-approval | AI-09 — Staff Send to AI creates one durable idempotent capability-scoped work request bound to an immutable case reference | unchanged-backlog (verifying upstream; re-check at sync) | — | automation-integrations |
| TICK-103 | automation-integrations | backlog | feature | capability, AI-10, later, post-alpha, blocked | AI-10 — Extensible named AI job catalogue beginning with Case assessment | unchanged-backlog | — | automation-integrations |
| CASE-001 | case-reference-workflow | backlog | chore | simplify, web, follow-up | Show or drop the unread TempData["CaseDetailsStatus"] written by Create and Intake/Details | gateway-worker-ticket (moot after Razor retirement; keep as web hygiene until cutover) | 05 (reuse/cut map) | case-reference-workflow |
| CASE-002 | case-reference-workflow | backlog | feature | design, future-capability, post-report | Design post-report queries raised to Engineers | unchanged-backlog | — | case-reference-workflow |
| CASE-004 | case-reference-workflow | backlog | feature | design, future-capability, case-notes | Deliver case notes as a separate future capability | unchanged-backlog | — | case-reference-workflow |
| CASE-009 | case-reference-workflow | preparing | fix | ui, case-detail, queries, operator-reported | Show auto-attached Query emails on Case Details and remove manual query creation | gateway-worker-ticket (data) + desktop-screen-spec (Communications tab) | 03, 06 | case-reference-workflow |
| CASE-011 | case-reference-workflow | backlog | feature | ui, images, gallery, operator-requested | Provide a reusable image gallery viewer across image-bearing pages | desktop-screen-spec | 06 (shared gallery control) | desktop-ui |
| CASE-012 | case-reference-workflow | backlog | feature | ui, redesign, case, operator-requested | Redesign the Case page workspace | desktop-screen-spec | 06 (case workspace §14.5) | desktop-ui |
| CASE-020 | case-reference-workflow | backlog | fix | qdos26011, found-during-qa | Read the case header and list from the case, not the intake draft | gateway-worker-ticket | 03 (case read model) | case-reference-workflow |
| CASE-021 | case-reference-workflow | backlog | fix | qdos26013, production-defect, found-during-qa | Refuse Review for a case with no images instead of asserting its images are complete | gateway-worker-ticket | 03 (Core lifecycle rule) | case-reference-workflow |
| CASE-022 | case-reference-workflow | backlog | fix | found-during-qa, ui, design | Make creating a public upload link findable | desktop-screen-spec | 06 (Documents tab commands) | desktop-ui |
| TICK-034 | case-reference-workflow | backlog | feature | capability, DATA-02, next, post-alpha, blocked | DATA-02 — Prepare inspection-address / repairer reference data from separately approved spreadsheets | unchanged-backlog | — | case-reference-workflow |
| TICK-067 | case-reference-workflow | backlog | feature | capability, CASE-05, later, post-alpha, blocked | CASE-05 — Diminution cases | unchanged-backlog | — | case-reference-workflow |
| TICK-068 | case-reference-workflow | backlog | feature | capability, CASE-06, later, post-alpha, blocked | CASE-06 — Commercial cases | unchanged-backlog | — | case-reference-workflow |
| UICASE-001 | case-reference-workflow | backlog | feature | requires-live-approval | UI Improvement - Case Screen | desktop-screen-spec | 06 (case workspace) | desktop-ui |
| DELIV-006 | delivery-repository | backlog | chore | design, documentation | Capture the Claude Design github.md screen map in the repository | gateway-worker-ticket (documentation) | 06 (screen map input), 00 | delivery-repository |
| DELIV-010 | delivery-repository | backlog | fix | ci, source-now | Stop full-history CI checkouts timing out on the 700 MB repository | gateway-worker-ticket | 09 (CI lanes) | delivery-repository |
| DOCS-001 | documents-reports | preparing | feature | now, renderer-integration | Trigger report generation from complete accepted assessments and retain immutable report references | report-decision | 07 (reports) | documents-reports |
| DOCS-003 | documents-reports | backlog | feature | RPT-04, later, post-alpha, blocked, evidence | Activate diminution report rendering when an approved template is supplied | report-decision (template scope) | 07 | documents-reports |
| DOCS-004 | documents-reports | backlog | feature | RPT-05, later, post-alpha, blocked, evidence | Activate addendum report rendering when an approved template and workflow are supplied | report-decision (template scope) | 07 | documents-reports |
| DOCS-011 | documents-reports | backlog | feature | found-during-qa, ui, design | Preview evidence images and documents in the case, with paging and a download | desktop-screen-spec | 06 (Documents/Evidence preview pane) | desktop-ui |
| DOCS-012 | documents-reports | backlog | fix | found-during-qa, ui, design | Show case evidence on the Evidence tab, not the document custody ledger | desktop-screen-spec | 06 (Evidence tab) | desktop-ui |
| TICK-018 | documents-reports | preparing | feature | capability, DOC-02, now, requires-live-approval | DOC-02 — Store source emails, instruction documents, images, correspondence, and reports in Box | gateway-worker-ticket | 07 (Box) | documents-reports |
| TICK-055 | documents-reports | backlog | feature | capability, CASE-23, next, post-alpha, blocked | CASE-23 — Post-report query and dispute work on the existing case with retained report/reply-chain evidence | unchanged-backlog | — | documents-reports |
| TICK-081 | documents-reports | preparing | feature | capability, EXT-08, later, post-alpha, blocked | EXT-08 — Activate deterministic report generation from accepted Core-owned data through the approved renderer | report-decision | 07 | documents-reports |
| TICK-096 | documents-reports | preparing | feature | capability, RPT-01, later, post-alpha, blocked | RPT-01 — Deterministic renderer validates accepted data, computes each figure once, and applies the fixed Collision Engineers layout | report-decision | 07 | documents-reports |
| TICK-097 | documents-reports | preparing | feature | capability, RPT-02, later, post-alpha, blocked | RPT-02 — Assessment rendering covers four outcome variants and emits the fee note plus itemised repair specification | report-decision | 07 | documents-reports |
| TICK-100 | documents-reports | preparing | feature | capability, RPT-05, later, post-alpha, blocked | RPT-05 — Addenda render from accepted case data plus a versioned amendment without retyping the case | report-decision | 07 | documents-reports |
| TICK-206 | documents-reports | preparing | feature | now, source-now, decision-required | Map renderer templates to capabilities and decide proposed retirements | report-decision (template scope input to ADR-0108) | 07 | documents-reports |
| TICK-208 | documents-reports | preparing | feature | now, source-now | Preserve final Sent evidence through post-report correction | report-decision | 07 | documents-reports |
| TICK-214 | documents-reports | preparing | feature | now, source-now, decision-required | Decide the long-term MCPB host and distribution boundary | report-decision (decision register; MCP boundary also concerns 03/12) | 07 | documents-reports |
| TICK-216 | documents-reports | preparing | feature | now, source-now, decision-required | Decide whether unaccepted wording and signature assets may ship behind a closed gate | report-decision | 07 | documents-reports |
| ENG-001 | engineering-assessment | backlog | feature | design, external, future-capability, evidence | Deliver Experian AutoCheck as a future vehicle-history capability | unchanged-backlog | — | engineering-assessment |
| ENG-008 | engineering-assessment | backlog | feature | cazana, valuation, external-integration | Implement Cazana valuation worker for case data | unchanged-backlog | — | engineering-assessment |
| ENG-009 | engineering-assessment | preparing | feature | cazana, valuation, ui, case-workbench | Initiate Cazana valuation from the case workbench | unchanged-backlog | — | engineering-assessment |
| ENG-011 | engineering-assessment | backlog | spike | qdos26008 | Read the odometer from evidence photographs | unchanged-backlog | — | engineering-assessment |
| TICK-076 | engineering-assessment | backlog | feature | capability, CASE-22, later, post-alpha, blocked | CASE-22 — Replace EVA inspection and report-preparation work inside Pegasus | unchanged-backlog | — | engineering-assessment |
| TICK-077 | engineering-assessment | backlog | feature | capability, EXT-04, later, post-alpha, blocked | EXT-04 — Direct EVA API integration | unchanged-backlog | — | engineering-assessment |
| TICK-078 | engineering-assessment | backlog | feature | capability, EXT-05, later, post-alpha, blocked | EXT-05 — Replace EVA Engineer assignment | unchanged-backlog | — | engineering-assessment |
| TICK-079 | engineering-assessment | backlog | feature | capability, EXT-06, later, post-alpha, blocked | EXT-06 — Replace EVA estimating without moving repair-specification authority out of Pegasus Core | unchanged-backlog | — | engineering-assessment |
| TICK-080 | engineering-assessment | backlog | feature | capability, EXT-07, later, post-alpha, blocked | EXT-07 — Replace EVA valuation while preserving separate dated/versioned source evidence | unchanged-backlog | — | engineering-assessment |
| TICK-082 | engineering-assessment | backlog | feature | capability, EXT-09, later, post-alpha, blocked | EXT-09 — Versioned repair-estimate lines, source versions, approvals, original-versus-assessed comparison | unchanged-backlog | — | engineering-assessment |
| TICK-083 | engineering-assessment | backlog | feature | capability, EXT-10, later, post-alpha, blocked | EXT-10 — Versioned vehicle-valuation evidence, explicit Engineer acceptance/adjustments/rationale | unchanged-backlog | — | engineering-assessment |
| TICK-084 | engineering-assessment | backlog | feature | capability, EXT-11, later, post-alpha, blocked | EXT-11 — Versioned fee/invoice and Engineer cost/payment inputs, accounting status, role-restricted visibility | unchanged-backlog | — | engineering-assessment |
| TICK-085 | engineering-assessment | backlog | feature | capability, EXT-12, later, post-alpha, blocked | EXT-12 — Audatex/PDF repair-estimate ingestion with retained source artifact, mapped version, and variant proof | unchanged-backlog | — | engineering-assessment |
| TICK-086 | engineering-assessment | backlog | feature | capability, EXT-13, later, post-alpha, blocked | EXT-13 — Independently licensed valuation-source adapters that preserve each source observation and version | unchanged-backlog | — | engineering-assessment |
| TICK-092 | engineering-assessment | preparing | feature | capability, CASE-31, later, post-alpha, blocked | CASE-31 — One accepted structured case/engineering record is the source for every deterministic report and fee note | unchanged-backlog | — | engineering-assessment |
| TICK-094 | engineering-assessment | preparing | feature | capability, ENG-02, later, post-alpha, blocked | ENG-02 — Engineer-owned final value/deductions, outcome, salvage category/value, and roadworthiness/reason | unchanged-backlog | — | engineering-assessment |
| TICK-095 | engineering-assessment | backlog | feature | capability, UI-15, later, post-alpha, blocked | UI-15 — One case-centred progressive Engineer workbench for inspection, vehicle/damage, valuation, estimate/report | unchanged-backlog (future desktop workbench; 06 notes it as a later screen) | — | engineering-assessment |
| INTK-001 | intake-processing | backlog | feature | — | Make queued upload status honest for retry-scheduled work and auto-associated receipts | gateway-worker-ticket | 03 (upload status endpoint) | intake-processing |
| INTK-002 | intake-processing | backlog | chore | — | Intake duplication chores: adapter-wide fault naming, one decision-code table, Web-composition assertion | gateway-worker-ticket | 03, 10 | intake-processing |
| INTK-003 | intake-processing | backlog | fix | — | Recover dispatched intake work whose queue message never arrives | gateway-worker-ticket | 07 (Graph/queue intake) | intake-processing |
| INTK-004 | intake-processing | backlog | chore | — | Reconcile intake decision labels and the Operations case-link claim with the code | gateway-worker-ticket | 06 (operator vocabulary), 03 | intake-processing |
| INTK-019 | intake-processing | backlog | feature | triage, assignment, ui, operator-reported | Replace Triage "Assign to me" with Engineer selection | desktop-screen-spec | 06 (Triage detail) | desktop-ui |
| INTK-026 | intake-processing | backlog | feature | vehicle, mileage, normalisation, case-data | Normalize kilometre case mileage to canonical miles | fork implementation [[INTK-003]] | — | intake-processing |
| INTK-027 | intake-processing | backlog | fix | defect, intake, reevaluation, live-found | Make policy re-evaluation work after transient staging cleanup | gateway-worker-ticket; fork implementation [[INTK-004]] | 07 | intake-processing |
| INTK-031 | intake-processing | backlog | feature | extraction, audits, corpus | Identify the third-party engineer behind an audit's original report | unchanged-backlog | — | intake-processing |
| INTK-032 | intake-processing | backlog | feature | qdos26009, extraction, audits | Fall back safely when a third-party report format cannot be read | unchanged-backlog | — | intake-processing |
| INTK-033 (board INTK-007) | intake-processing | in-repository implementation | fix | production-defect, found-during-qa, triage, closed-composition-gate | A triage-request email creates no Triage and no Unidentified item — it is stranded | in-repository implementation [[INTK-007]]; no upstream sync | 07 | intake-processing |
| TICK-035 | intake-processing | backlog | feature | capability, INT-04, next, post-alpha, blocked | INT-04 — Activate additional providers through the shared intake/case workflow | unchanged-backlog | — | intake-processing |
| TICK-036 | intake-processing | backlog | feature | capability, INT-05, next, post-alpha, blocked | INT-05 — Automatic ingestion from desk@collisionengineers.co.uk | unchanged-backlog | — | intake-processing |
| TICK-037 | intake-processing | backlog | feature | capability, INT-06, next, post-alpha, blocked | INT-06 — Automatic ingestion from engineers@collisionengineers.co.uk | unchanged-backlog | — | intake-processing |
| TICK-038 | intake-processing | backlog | feature | capability, INT-07, next, post-alpha, blocked | INT-07 — Automatic ingestion from info@collisionengineers.co.uk | unchanged-backlog | — | intake-processing |
| TICK-041 | intake-processing | backlog | feature | capability, INT-16, next, post-alpha, blocked | INT-16 — OCR for scan-like PDF instruction pages | unchanged-backlog | — | intake-processing |
| TICK-054 | mail-communications | preparing | feature | capability, MAIL-13, next, post-alpha, blocked | MAIL-13 — Change read state, Outlook categories, flags, or delete messages in the app | gateway-worker-ticket (provider port exists, unavailable by default) | 07 (mail) | mail-communications |
| TICK-066 | mail-communications | backlog | feature | capability, MAIL-19, later, post-alpha, blocked | MAIL-19 — Automatically send chasers or other outbound messages | unchanged-backlog | — | mail-communications |
| TICK-075 | mail-communications | backlog | feature | capability, MAIL-17, later, post-alpha, blocked | MAIL-17 — Idempotent report/fee-note send on the original Outlook thread or provider API | unchanged-backlog | — | mail-communications |
| TICK-088 | mail-communications | preparing | feature | capability, MAIL-12, later, post-alpha, blocked | MAIL-12 — Authenticated staff compose, reply, forward, and send email in Pegasus | unchanged-backlog | — | mail-communications |
| PLAT-005 | platform-operations | implementing | chore | ui, design | Capture visual screenshots from a local DevelopmentOffline run | desktop-screen-spec (screenshots become parity-matrix evidence) | 01 (§23.1 evidence), 06 | desktop-ui |
| PLAT-015 | platform-operations | backlog | fix | ui, design, copy, follow-up | Bring pre-existing operator copy in line with the design authority (identifiers, placeholders, narration) | desktop-screen-spec (copy rules apply to every desktop screen) | 06 | desktop-ui |
| PLAT-022 | platform-operations | backlog | feature | cazana, external-integration, requires-live-approval | Prepare consent-gated Cazana provider activation | unchanged-backlog | — | platform-operations |
| PLAT-023 | platform-operations | backlog | feature | ui, redesign, operations, operator-requested | Redesign the Operations workspace | desktop-screen-spec | 06 (Operations) | desktop-ui |
| PLAT-025 | platform-operations | backlog | feature | ui, redesign, administration, workflow | Redesign workflow configurations | desktop-screen-spec | 06 (Administration › Configuration) | desktop-ui |
| PLAT-026 | platform-operations | backlog | feature | ui, redesign, administration, mailboxes | Redesign Approved Mailboxes administration | desktop-screen-spec | 06 (Administration › Mailboxes) | desktop-ui |
| PLAT-027 | platform-operations | backlog | feature | ui, redesign, administration, staff, access | Consolidate Staff accounts, roles, and access review administration | desktop-screen-spec | 06 (Administration › Staff) | desktop-ui |
| PLAT-028 | platform-operations | preparing | feature | ui, redesign, administration, organizations | Redesign Organizations and Principals with provider API controls | desktop-screen-spec | 06 (Administration › Organizations/Principals) | desktop-ui |
| PLAT-029 | platform-operations | backlog | feature | ui, redesign, information-architecture | Define and deliver the broad Pegasus information-architecture restructure | desktop-screen-spec (IA input) | 06 (shell and route order) | desktop-ui |
| PLAT-032 | platform-operations | backlog | chore | simplification | Simplification and duplicate-route sweep across the codebase | gateway-worker-ticket (hygiene; overlaps the reuse/cut map) | 05 | platform-operations |
| PLAT-035 | platform-operations | backlog | feature | testing, least-privilege, regression-class | Fail the build when a runtime role writes a table it has no grant on | gateway-worker-ticket (prerequisite for any gateway schema change) | 03, 08 | platform-operations |
| PLAT-036 | platform-operations | backlog | chore | observability, needs-operator-decision, cost | Raise or earn back the Application Insights daily ingestion quota | gateway-worker-ticket (⚠ raising the quota is an Azure write) | 10, 11 | platform-operations |
| PLAT-038 | platform-operations | backlog | fix | found-during-qa, developer-experience | Serve intake-retained document content in the local profile | gateway-worker-ticket (needed by the Test/UAT stack) | 08 | platform-operations |
| PLAT-039 | platform-operations | verifying | fix | qdos26012, production-defect, found-during-qa | Refresh the Box access token instead of minting it once per process | gateway-worker-ticket (already in upstream `main` `282ba44`/`79db11f`; arrives with the first sync — recreate as verify-only) | 07 | platform-operations |
| PLAT-041 | platform-operations | backlog | fix | qdos26014, found-during-qa, performance, box | Resolve the Box case folder once per export, not once per image | gateway-worker-ticket | 07 | platform-operations |
| TICK-001 | platform-operations | backlog | feature | capability, OPS-10, now, requires-live-approval | Complete the QDOS alpha production release | unchanged-backlog (web release programme; superseded by desktop cutover plan) | — | platform-operations |
| TICK-105 | platform-operations | backlog | feature | capability, MI-01, later, post-alpha, blocked | MI-01 — Per-Engineer throughput and query rate/types | unchanged-backlog | — | platform-operations |
| TICK-106 | platform-operations | backlog | feature | capability, MI-02, later, post-alpha, blocked | MI-02 — Per-principal report counts, types, and periods feeding invoice generation | unchanged-backlog | — | platform-operations |
| TICK-107 | platform-operations | backlog | feature | capability, MI-03, later, post-alpha, blocked | MI-03 — Holding-pen age and instruction-to-images, ready-to-sent, and overall turnaround measures | unchanged-backlog | — | platform-operations |
| PR-003 | pr-review | backlog | fix | — | Correct Contract repair to use the computed repair total | gateway-worker-ticket (Core assessment rule) | 03 | pr-review |
| PR-026 | pr-review | implementing | fix | pr-review, blocking, MAIL-004, governing-doc | Reconcile Outlook category administration with deferred UI design approval | gateway-worker-ticket | 06, 07 | pr-review |

Disposition totals: `desktop-screen-spec` 18 (including CASE-009's screen
half), `gateway-worker-ticket` 26, `report-decision` 13,
`unchanged-backlog` 53. Every row above is recreated, listed, or both per the
recreation rule; none is dropped outright — a "drop" needs the operator, and
the only candidate is CASE-001 once Razor Pages are retired.

## Operator boundary — current refactor

The upstream board and remote comparison below are historical provenance from
the planning baseline. The operator has prohibited all synchronization with
the upstream Pegasus repository for the current refactor. No ticket may add,
fetch, compare, merge, or push an upstream remote. All implementation and
history work stays in this repository and uses the configured `pegasusDesktop`
remote only. Cloud writes, deployments, credentials, and external environment
changes are deferred until the full refactor is complete.

The carry-over rows remain useful as evidence of why fork tickets exist. They
are not instructions to perform an external sync. A ticket that previously
required one must be amended through Kanmer to an honest in-repository scope;
if no such scope exists, it remains blocked rather than importing external
work.

## Historical code drift and superseded first-sync plan

Upstream `main` `7d6a948a` ("Merge pull request #523 from
collisionengineers/task/qdos26012-regressions") is 32 commits ahead of the
fork's `main` `191ddf33`; the fork head is an ancestor, so the first sync is
a fast-forward. Notable upstream changes in that range (read 2026-08-23):

- Release records 23 and 24 with their provision traps and "how to diagnose
  without telemetry" (`d8cc85e`, `a8855ba`).
- PLAT-039 Box token renewal and single token/expiry value (`79db11f`,
  `282ba44`) — removes a production defect the desktop Box flow would
  otherwise inherit.
- DOCS-010 Box read fixes and evidence gallery occurrence targeting
  (`e1d40a6`, `ef5f1b6`).
- MAIL-011 forwarded-header recipient, MAIL-012 QDOS triage template
  classification (`9c39c1d`, `51da3a3`, `1828a4b`).
- ENG-013 backfill of lookup values; PLAT-037 accepted EVA mapping declared
  in infra (`42a3a84`, `5dd8694`).
- CASE-019 case export proof (`efbb2a9`); INTK-029 unlink dialog proof
  (`e035e3b`); QDOS26009 end-to-end proof (`5e52b13`).

Historical first-sync plan (DSK-01-10, area 00 flow): the paragraph above
describes the plan that was recorded before the operator boundary changed. It
is retained for provenance and is not executable. The current disposition is
in-repository only: no upstream remote is added or read, no external commits
are imported, and no sync or freeze proof is required. Recreate or amend any
needed work as a fork ticket and deliver it through the configured remote.
