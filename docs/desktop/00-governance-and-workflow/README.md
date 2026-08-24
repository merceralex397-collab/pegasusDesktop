# 00 · Governance and workflow

Area plan for the rules every other desktop-conversion plan assumes: who
decides, where decisions are written, how the fork branches and syncs, how
the Kanmer board is shaped, what a ticket must contain, and which documents
(ADR/FRD/PRD/capabilities) the conversion adds. Read this before cutting a
ticket from any other area plan in [`docs/desktop/`](../README.md).

## 1. Purpose and proposal coverage

Delivers the governance scaffolding of the conversion programme: the
authority order, the decision-record set, the fork's branching and upstream
sync flow, the Kanmer board shape and ticket template, the programme exit
checklist, and the mapping from the proposal's phases to the owning plans.

Proposal sections owned here: §1 (executive decision), §2 (authority and
scope, locked constraints, non-goals), §3 (reconciliation of earlier
proposals), §6 (repository strategy: fork, not greenfield), §24 (phase map,
as the index to the content plans), §25 (ticket structure), §26
(documentation set), §27 (acceptance criteria), §28 (optimality — recorded,
not re-argued), §29 (immediate next actions), Appendix A (ADR template).
The cloud-justification *test* of §4 is defined here; its per-capability
answers live in [11-azure-disposition](../11-azure-disposition/README.md)
and in the integration plans.

## 2. Evidence base

### Facts

Repository (fork `merceralex397-collab/pegasusDesktop`, `main` @ `191ddf33`,
read 2026-08-23):

- Governance home: [`AGENTS.md`](../../../AGENTS.md) — documentation model
  (PRD/FRD/ADR), ADR conventions (stable IDs, YAML frontmatter, one decision
  per ADR, supersede by new ADR), New Markdown placement, Simplicity rails,
  Safety rails, Product invariants, Repository task workflow (steps 1–6:
  take, worktree, plan, work and PR with simplification pass, review and
  merge, release or abandon). Authority chain restated in
  [`docs/index.md` § Authority](../../index.md#authority).
- Placement gate: `scripts/Test-MarkdownPlacement.ps1` — allowed-roots regex
  (function `Test-AllowedMarkdownPath`) now reads
  `docs/(prd|frd|adr|design|desktop)` after this task; the CI
  `documentation` job (`.github/workflows/ci.yml`, `windows-latest`) runs it
  together with `scripts/Test-DocumentationLinks.ps1`.
- `AGENTS.md` § New Markdown placement and `docs/index.md` now register
  `docs/desktop/` as the planning area (this task). Programme planning only;
  ADR/FRD/PRD and ticket-transient documents keep their homes.
- ADR state: `docs/adr/README.md` lists ADR-0001…ADR-0029 (0017 never
  issued); accepted set includes ADR-0002 (modular monolith on Azure),
  ADR-0007 (direct authorised-terminal deployment), ADR-0009 (workspaces;
  its body says "The future desktop workbench remains deferred until the Web
  capability is complete", `docs/adr/0009-adopt-pegasus-monorepo-workspaces.md:74`),
  ADR-0014 (local + production only, no Azure dev/test/staging),
  ADR-0015 (Web on Container Apps Consumption), ADR-0016 (standalone WinForms
  email evaluator — the only desktop precedent), ADR-0025/0028 (integrated
  renderer, runs in the Web container).
- Branching rule: `docs/engineering.md:10-52` — task branches from `dev`, PR
  into `dev`, `main` is the deployed branch, promotion is an exact-SHA atomic
  fast-forward requiring the literal `MERGE AUTH GRANTED`; a GitHub
  merge/squash/rebase is not a promotion; `scripts/Test-MainBranchHistory.ps1`
  guards `main` history on push. The fork has **only `main`** (no `dev`, no
  tags; `git branch -a`, `git tag`, 2026-08-23).
- Upstream `collisionengineers/pegasus` (`git ls-remote`, 2026-08-23): heads
  `dev` `499b8885`, `main` `7d6a948a`, `kanmer-board` `4694067c`. Fork `main`
  `191ddf33` **is an ancestor** of upstream `main`; upstream is **32 commits
  ahead** (releases 21–24 recorded; PLAT-039, DOCS-010, MAIL-011/012,
  ENG-013, CASE-019 fixes).
- Kanmer (fork): board worktree `.worktrees/kanmer` on branch
  `kanmer-board`, `.kanmer/data/board.yml` — one area `pr-review` (prefix
  `PR`), profiles `feature`/`fix`/`chore`/`spike` with `questions-resolved`
  gates on every move, `defaultProfile: fix`, group kinds `epic` (`EPIC`)
  and `horizon` (`HZN`), proof types `visual`/`test-output`/`command-log`,
  `idPrefixes: ticket TICK, plan PLAN, research RES`. No tickets.
- Kanmer (upstream board, read-only clone 2026-08-23): ten areas with
  prefixes MAIL, AUTO, DOCS, ENG, INTK, PLAT, DELIV, CASE, KANMER, PR; 456
  tickets (342 non-archived, 233 done, 109 open); groups EPIC-001…008 and
  HZN-001…003; upstream profiles lack the `questions-resolved` gate. Triage of
  the 109 open tickets is in
  [01 · upstream carry-over](../01-inventory-and-parity/upstream-kanmer-carryover.md).
- Kanmer operating instructions: `AGENTS.md:1-22` (machine-managed block):
  `get_status` first, `get_doc_gates <id>` before every move, stages
  `backlog → preparing → implementing → review → verifying → done`, one gated
  boundary per move, unticked `open-questions/` items block a move, proof is
  written on merged `main`, skills run
  `kanmer-tickets → -research → -plan → -execute → -review → -verify → -closeout`.
- Ticket/plan rules: `AGENTS.md` § Repository task workflow step 4 (the
  simplification pass over the branch's own diff, recorded in the ticket plan
  under a dated "Simplification pass" heading; docs-only records "n/a —
  docs-only") and step 5 (review by an agent that did not implement);
  `docs/engineering.md` § Plan sizing ("A plan states its diff estimate
  first… Research separates verified facts from assumptions").
- Capability registry: `docs/capabilities.md` — `FAMILY-NN` two-digit IDs,
  231 IDs across 18 families (INT, CASE, OPS, MAIL, EXT, UI, DOC, ACC, BND,
  AI, TRI, MCP, RPT, EVAL, API, MI, ENG, DATA); no desktop family exists.
- FRD set: FRD-01…FRD-12 (`docs/frd/README.md`); FRD-12 is Operator
  experience (UI). PRD: one file, `docs/prd/pegasus-product.md`.
- Design authority: `docs/design/README.md` binds every UI change
  (`AGENTS.md` § Simplicity rails).

Official documentation (fetched 2026-08-23):

- Codex custom agents and `[agents]` config:
  <https://developers.openai.com/codex/subagents> (redirects to
  learn.chatgpt.com) — fields `name`, `description`,
  `developer_instructions`, optional `model`, `model_reasoning_effort`,
  `sandbox_mode`, `mcp_servers`, `skills.config`.
- Codex skill discovery: <https://developers.openai.com/codex/skills> —
  repository skills are discovered from `.agents/skills` (current directory
  up to the repository root), user skills from `~/.agents/skills`.

### Assumptions

- A-00-1 (resolved 2026-08-23, now a decision): D-001 was decided as
  Option A — the fork is the conversion line and becomes the single release
  source once a gateway change is needed in production (see D-001 below).
  Until that point upstream keeps releasing the web app.
- A-00-2 (resolved 2026-08-23, now a decision): the operator confirmed the
  reserved ADR block ADR-0100…ADR-0110; `AGENTS.md` § ADR conventions
  records it. The "next free number" fallback is retired.
- A-00-3: the Kanmer GUI/MCP in use supports `create_group`, `create_item`,
  and area creation through `kanmer-setup`; if areas can only be created in
  the GUI, the board-seeding ticket records that step as operator-performed.

## 3. Decisions and assumptions

Locked (from [the index](../README.md#locked-decisions-and-open-decisions)):
L-01 gateway in place, L-02 local Test/UAT stack (ADR-0014 stands), L-03
WebView2 rendering (ADR-0108), L-04 subagents exist, L-05 board seeded from
these plans.

Authority order (proposal §2, reconciled with `docs/index.md`): operator
notes > PRD > FRD > capabilities > ADRs > current-state docs
(`current-architecture.md`, `operations.md`) > working rules (`runbook.md`,
`engineering.md`, `design/README.md`) > these plans > the proposal's prior
documents > generic skill guidance. The proposal's three "prior documents"
(§2 item 5) are **not** in the repository; they are not an input to any
ticket. Where an upstream skill assumes a web app, Microsoft-account login,
cross-platform runtime, public distribution or enterprise scale, it does not
apply (proposal §2).

**Deviation: reserved ADR block.** `AGENTS.md` § ADR conventions says the
next free number. Upstream keeps issuing ADRs (29 issued, active), and the
one-way upstream sync in the branching flow would collide with any number
the fork takes next. The conversion therefore uses **ADR-0100…ADR-0110**, and
the first conversion ADR restates it. **The operator confirmed the block on
2026-08-23** and `AGENTS.md` § ADR conventions now records it (done in this
planning task); DSK-00-05 authors the ADRs themselves. ADR bodies stay
immutable; supersession is by new ADR as today. **ADR-0014 is not superseded** — Test/UAT is local (L-02).

ADR set (Appendix A template; each answers the cloud-justification test):

| ADR | Decision | Context in one line | Supersedes / relates |
| --- | --- | --- | --- |
| ADR-0100 | Native WinUI 3 / Windows 11 desktop client, converted inside this fork, no WebView shell | Proposal §1, §6, §7; desktop no longer deferred | Supersedes the deferral clause of ADR-0009 only; ADR-0016 unchanged |
| ADR-0101 | Local-execution / cloud-authority split and the six-question cloud-justification test | Proposal §3.1, §4 | Relates ADR-0002 |
| ADR-0102 | Existing Pegasus credentials and identity store; desktop session = short-lived access token + rotated refresh token | Proposal §8; Identity/OpenIddict already in `Pegasus.Web` | Relates ADR-0004, ADR-0011, ADR-0027 |
| ADR-0103 | Gateway (evolved `Pegasus.Web`), never direct database access from workstations | Proposal §10.1; L-01 | Relates ADR-0002, ADR-0015 |
| ADR-0104 | Online-required; no offline replication; bounded local cache only | Proposal §11 | — |
| ADR-0105 | Signed MSIX/App Installer distribution with a gateway minimum-version gate (two-layer enforcement) | Proposal §9 | Relates ADR-0007 (gateway release unchanged) |
| ADR-0106 | Graph intake worker stays central (unattended execution, protected credentials) | Proposal §12.1 | Relates ADR-0024 |
| ADR-0107 | Box and DVLA/DVSA credentials stay behind the gateway; no long-lived provider secret in the package | Proposal §12.2–12.3 | — |
| ADR-0108 | Report rendering in the desktop through an isolated, non-UI WebView2 HTML→PDF path; gateway renderer retained until golden-file parity | Proposal §12.5, §23.2; L-03 | Relates ADR-0025, ADR-0028 |
| ADR-0109 | Desktop diagnostics bundle + existing App Insights; no new telemetry fleet | Proposal §18 | Relates PLAT-034 |
| ADR-0110 | Agent-skill pinning (lockfile, vendored revisions) and invocation/review protocol | Proposal §20 | Relates `skills-lock.json` |

Cloud-justification test table (Appendix A, used verbatim in each ADR and in
every cloud-dependency record):

| Question | Answer (yes/no) | Evidence |
| --- | --- | --- |
| Shared authority — must several users see and update the same state? | | |
| Unattended execution — must it run with every desktop closed? | | |
| Protected credentials — long-lived secret that must not sit on workstations? | | |
| Public callback — must an external service call a stable public endpoint? | | |
| Central enforcement — revocation, permissions, audit, invariant independent of the client? | | |
| Measured operational advantage — measured evidence central is materially better? | | |

All six "no" → the responsibility belongs in the desktop. "It is already in
Azure", "the web app does it", "it may scale later" are not answers.

### Recommended branching flow (answer to decision 6; adopt unless objected)

Keep the `AGENTS.md` shape — the release skill, the CI history guard and the
review rules already assume it — with four fork-specific additions:

1. **Create `dev` from `main` now.** `task/<slug>` → PR → `dev` (CI green +
   independent review) → exact-SHA promotion to `main` with
   `MERGE AUTH GRANTED`. No long-lived `desktop-conversion` branch: the
   fork's `dev`/`main` *are* the conversion trunk (the proposal's isolation
   requirement, §6.3, is satisfied by the fork itself).
2. **Add `upstream` = `https://github.com/collisionengineers/pegasus` as a
   read-only remote and sync one-way** (`upstream/main` → fork `dev` through
   a merge PR, then promote) after each upstream release **until cutover**,
   because the still-live web app keeps receiving fixes the evolved gateway
   needs. Never push fork → upstream. First sync = the 32 commits pending on
   2026-08-23 (fast-forward; verify with
   `git merge-base --is-ancestor <fork-main> upstream/main`).
3. **Land vertical slices small and flag-gated**, not on long feature
   branches: new `/api/v1` endpoints and desktop features ship behind the
   existing `Features:*` composition-gate pattern (`src/Pegasus.Web/Program.cs`)
   so `main` stays releasable for the production web app throughout
   (expand/contract).
4. **Tag releases on `main`**: `gateway/r<N>` (N = the release number in
   `docs/operations.md` § Production environment) and `desktop/v<M.m.b>`
   (= MSIX version) so the compatibility range in the release manifest maps
   to commits. CI builds an unsigned MSIX on every PR and builds + signs on
   `main` tags only; publishing to the production feed stays a
   runbook-controlled step (same culture as the `pegasus-release` skill);
   pilot-feed publishing to the decided UNC share (D-003) may be automated
   once D-002 settles how packages are signed.

**D-001 (decided 2026-08-23) — release source of truth after Phase 2.**
The operator chose **Option A**: when the first gateway change is needed in
production (compatibility endpoint and staff token flow,
[04](../04-auth-session-update-and-startup/README.md)), the fork becomes the
**single release source for gateway, worker and desktop** — upstream
`collisionengineers/pegasus` is merged in one final time and then frozen
(read-only/archived), consistent with proposal §6.3 "no permanent second
Pegasus repository". The alternative (merging fork gateway changes back
upstream per release) was rejected for its double CI/review cost and two
current-state documents. Execution notes: until the freeze, the one-way
`upstream` sync above continues; the freeze itself is an action in the
upstream repository agreed with its owners; DSK-00-10 records the decision
in ADR-0100's consequences and `docs/operations.md` when they are written.

### Kanmer board shape for the fork

Recreate the nine upstream domain areas with the same prefixes so carried-over
tickets keep readable IDs, and add the desktop areas:

| Area id | Prefix | Holds |
| --- | --- | --- |
| mail-communications | MAIL | upstream carry-over (mail) |
| automation-integrations | AUTO | upstream carry-over (automation/MCP) |
| documents-reports | DOCS | upstream carry-over (documents/reports) |
| engineering-assessment | ENG | upstream carry-over (assessment) |
| intake-processing | INTK | upstream carry-over (intake) |
| platform-operations | PLAT | upstream carry-over (platform defects, hygiene) |
| delivery-repository | DELIV | releases, CI, repository hygiene (gateway release route) |
| case-reference-workflow | CASE | upstream carry-over (case) |
| pr-review | PR | exists already |
| desktop-foundation | FND | area plans 02 (and 00 governance tickets) |
| gateway-api | GWY | area plan 03, 04 (gateway side) |
| desktop-ui | DUI | area plan 06 |
| desktop-features | FEAT | area plan 05 slices, 07 desktop side |
| release-desktop | REL | area plan 09 |
| testing | TEST | area plan 08 |
| agent-tooling | TOOL | area plan 12 |

Groups: one `HZN` horizon per proposal phase — HZN "Phase 0 — discovery,
inventory and decisions", "Phase 1 — solution foundation", "Phase 2 —
compatibility, update and authentication", "Phase 3 — first vertical slice",
"Phase 4 — case editing and concurrency", "Phase 5 — intake and
communications", "Phase 6 — documents, Box and vehicle services", "Phase 7 —
assessment and reports", "Phase 8 — administration and hardening", "Phase 9 —
pilot and parallel operation", "Phase 10 — cutover and cloud rationalization"
(titles from proposal §24) — and one `EPIC` per area plan (00…12), each
group's `context.md` carrying the constraint that binds its batch. Profiles:
the fork board's `feature`/`fix`/`chore`/`spike` with the `questions-resolved`
gate; `get_doc_gates <id>` is authoritative, never `board.yml`. Every
conversion ticket is `feature` unless it is a pure defect (`fix`), hygiene
(`chore`) or a timeboxed investigation (`spike`).

### Ticket template (proposal §25 → Kanmer documents)

| Proposal §25 section | Kanmer document | Note |
| --- | --- | --- |
| 1 User outcome, 2 Current behaviour, 3 Target behaviour | `research/` | Current behaviour cites routes/page models/Core use cases and the parity-matrix row |
| 4 Execution placement (cloud test answered) | `research/` + the ADR it relies on | Six answers, not prose |
| 5 Data/API impact | `files` + `plan/` | Contracts, migration, concurrency, permissions |
| 6 UI specification | `plan/` (link to 06 screen spec) | States, commands, keyboard, accessibility |
| 7 Agent skills | `plan/` **`## Routing` block — required** | `subagent · skills (pinned path) · MCP tools` |
| 8 Implementation boundaries | `plan/` | Allowed projects, forbidden dependencies |
| 9 Acceptance criteria | `checklist` | Observable outcomes |
| 10 Verification | `post-implementation-report` (Appendix C shape) | Commands, unit/contract/UI/a11y/perf/packaging evidence |
| 11 Documentation changes | `plan/` + `post-implementation-report` | ADR/FRD/capabilities/operations touched |
| 12 Rollback/compatibility | `plan/` | Expand/contract, compat range, feature gate |
| — | `proof/` | Written on merged `main` (visual / test-output / command-log) |

Two rules from `AGENTS.md` apply unchanged: the **simplification pass** over
the branch's own diff before the PR (four lenses; findings and dispositions
under a dated "Simplification pass" heading in the plan; docs-only records
"n/a — docs-only"), and **independent review** by an agent that did not
implement — for the conversion that reviewer is `pegasus-desktop-reviewer`
(read-only sandbox, loads `winui-code-review` and the `pegasus-desktop`
project skill).

### Phase map (proposal §24 → owning plans)

| Phase | Content | Exit gate owner |
| --- | --- | --- |
| 0 Discovery, inventory, decisions | 00, 01, 11, 12 | 01 |
| 1 Solution foundation | 02 | 02 |
| 2 Compatibility, update, authentication | 03, 04, 09 | 04 |
| 3 First vertical slice | 05 (slice 1), 06, 08 | 05 |
| 4 Case editing and concurrency | 05 (slice 2), 03 | 05 |
| 5 Intake and communications | 05 (slice 3), 07 | 05 |
| 6 Documents, Box, vehicle services | 05 (slice 4), 07 | 05 |
| 7 Assessment and reports | 05 (slice 5), 07 (WebView2) | 05 |
| 8 Administration and hardening | 05 (slice 6), 10 | 10 |
| 9 Pilot and parallel operation | 09, 08, 01 (parity evidence) | 09 |
| 10 Cutover and cloud rationalization | 09, 11 | 11 |

### Programme exit checklist (proposal §27)

1. Signed native WinUI 3 app launches on supported Windows 11 machines.
2. No primary workflow embeds or depends on the web application.
3. Existing Pegasus credentials and permissions work; no Microsoft login.
4. Unsupported versions cannot proceed.
5. Every critical workflow has automated and UAT parity evidence (01).
6. Domain calculations and report generation run locally where approved.
7. Graph intake continues with every desktop closed.
8. Box and DVLA/DVSA work without long-lived secrets in the package.
9. Desktops never connect directly to the production database.
10. Concurrent edits are detected; nothing is silently overwritten.
11. Startup, navigation and memory budgets met on baseline hardware (10).
12. Critical workflows keyboard-accessible; Windows accessibility review passed.
13. Install, mandatory update and rollback proven (09).
14. Legacy web front end can be disabled in the Test/UAT stack without
    breaking desktop workflows.
15. Runtime Azure dependencies match the cloud-boundary register (11).
16. No Azure resource removed before dependency, backup and rollback proof.
17. Operational and support documentation complete (09, 10).
18. Every deviation from the proposal has a recorded justification (ADR or
    the plan's "Deviation:" line).

### §29 immediate next actions → tickets

| §29 item | Ticket |
| --- | --- |
| 1 Choose fork/branch, freeze baseline | DSK-00-01, DSK-00-02 |
| 2 Repository-derived parity matrix | 01 · DSK-01-01 |
| 3 Azure inventory without removing anything | 01 · DSK-01-03, 11 |
| 4 Record auth, DB, Graph, Box, DVLA flows | 01 · DSK-01-02 |
| 5 Pin skill revisions | 12 · DSK-12-01 |
| 6 Project skill + ADRs | DSK-00-04, DSK-00-05, 12 · DSK-12-02 |
| 7 Foundation spike (MSIX, update flow, login, compat check, shell, read-only case list) | 02, 04, 05 slice 1 |
| 8 Review the spike before converting further | `pegasus-desktop-reviewer` review ticket in 02 |
| 9 Vertical slices with parity + placement evidence | 05 |
| 10 No deprovisioning until after cutover | 11 |

## 4. Target state and exit gate

Target state: `dev` exists and tracks the workflow; `upstream` is a
configured read-only remote and the first sync has landed; the fork's Kanmer
board has the areas, horizons and epics above and every DSK ticket from the
twelve content plans created with a governing-doc reference
(`link_doc` to an ADR/FRD or `docs_todo: true`); ADR-0100…ADR-0110 are
accepted (ADR-0108 may be `proposed` until Phase 7 packaged-controller
validation and parity); FRD-13 and
the PRD update are merged; `docs/capabilities.md` carries the `DSK` family;
`AGENTS.md` § ADR conventions records the reserved block (done, operator
confirmation 2026-08-23); `docs/index.md` links everything.

Exit gate (Phase 0 governance part): `pwsh ./scripts/Test-DocumentationLinks.ps1`
and the placement gate pass on `dev`; `get_status` on the board shows the
areas and groups; `list_items` returns every DSK ticket with its `refs`;
no ticket can leave `backlog` without a governing doc (probe one with
`get_doc_gates`).

## 5. Work breakdown

| ID | Title | Profile | Depends on | Acceptance | Verification | Tier | Routing |
| --- | --- | --- | --- | --- | --- | --- | --- |
| DSK-00-01 | Create `dev` from `main`; record the baseline commit in `docs/desktop/README.md` | chore | — | `dev` exists at the baseline SHA; AGENTS.md workflow unchanged | `git branch --list dev`; `git rev-parse dev main` equal | 1 | (operator or `winui-dev`) · `pegasus-desktop` project skill · Kanmer `get_status` |
| DSK-00-02 | Add read-only `upstream` remote; first one-way sync (32 commits) via PR into `dev`; never push upstream | chore | DSK-00-01 | `upstream/main` merged into `dev` and promoted; CI green | `git merge-base --is-ancestor <fork-main> upstream/main`; `repository-check` green | 1 | `pegasus-desktop-reviewer` reviews the sync diff · — · Kanmer |
| DSK-00-03 | Seed the fork board: areas, HZN phases, EPIC per area plan, `context.md` per group | chore | — | `get_status` lists the 16 areas and 24 groups | Kanmer `list_board`, `list_groups` | 1 | — · `kanmer-setup`, `kanmer-tickets` · Kanmer `create_group`, `create_item` |
| DSK-00-04 | Create every DSK ticket from plans 01–12 with `refs`/`docs_todo`, profile, area, group and a `## Routing` block | chore | DSK-00-03 | Every ticket row in the plans exists on the board | `list_items` count equals the plan rows; `get_doc_gates` on a sample | 1 | — · `kanmer-tickets` · Kanmer `create_item`, `link_doc`, `set_ticket_doc` |
| DSK-00-05 | Author ADR-0100, ADR-0101, ADR-0103, ADR-0104, ADR-0105, ADR-0110 (the reserved block is already confirmed and recorded in AGENTS.md § ADR conventions) | feature | — | ADRs accepted; index table updated | `Test-DocumentationLinks.ps1`; ADR frontmatter valid | 1 | `pegasus-parity-researcher` (evidence) · `kanmer-docs` · Kanmer `link_doc` |
| DSK-00-06 | Author ADR-0102, ADR-0106, ADR-0107, ADR-0109 from the flow records of 01 | feature | 01 · DSK-01-02 | ADRs accepted with the cloud-justification table answered | Links pass; each table has six answers | 1 | `pegasus-parity-researcher` · `kanmer-docs` · Kanmer |
| DSK-00-07 | Author ADR-0108 (WebView2 rendering) as `proposed`; accept after Phase 7 packaged-controller validation and parity | feature | 07 validation | ADR names the documented `HWND_MESSAGE` host and cites the validation/parity evidence | Links pass | 1 | `pegasus-desktop-reviewer` · `kanmer-docs`, `microsoft-docs` · Microsoft Learn |
| DSK-00-08 | FRD-13 "Desktop operator experience" + PRD scope update + `DSK` family rows in `docs/capabilities.md` + `docs/frd/README.md`, `docs/index.md` links | feature | DSK-00-05 | FRD cites `docs/design/README.md`; capabilities rows have canonical owners | Links pass; `docs/capabilities.md` allocation summary updated | 1 | — · `kanmer-docs` · Kanmer `link_doc` |
| DSK-00-09 | Record the release-tag convention (`gateway/r<N>`, `desktop/v<M.m.b>`) in `docs/engineering.md` § Branches and delivery and the `pegasus-release` skill | chore | DSK-00-01 | Convention documented; first gateway tag applied on the next release | `git tag --list 'gateway/*'` | 1 | `pegasus-release-packager` · `pegasus-release` · — |
| DSK-00-10 | Record the decided D-001 (Option A, 2026-08-23 — fork is the single release source; upstream merged then frozen) in ADR-0100 consequences and `docs/operations.md`, and agree the upstream freeze with that repository's owners | chore | DSK-00-05 | Decision text with date in both files; freeze agreed and dated | Text present in both files | 1 | — · `kanmer-docs` · Kanmer |
| DSK-00-11 | Ticket-template enforcement: `kanmer-plan` plan docs for DSK tickets carry `## Routing` and `## Simplification pass`; add the check to the review checklist | chore | DSK-00-04 | Reviewer checklist updated; a sample ticket passes | `get_ticket_doc` on a sample | 1 | `pegasus-desktop-reviewer` · `kanmer-plan`, `kanmer-review` · Kanmer |
| DSK-00-12 | Parity-matrix home: register `docs/desktop/01-inventory-and-parity/parity-matrix.md` in `docs/index.md` and `docs/capabilities.md` notes; decide whether it later moves to `docs/features/` per proposal §23 | chore | 01 · DSK-01-01 | One canonical path; no duplicate matrix | Links pass | 1 | — · `kanmer-docs` · — |
| DSK-00-13 | Retire the proposal's unresolved "prior documents" reference: note in ADR-0100 that they are not in the repository and not an input | chore | DSK-00-05 | Sentence present | — | 1 | — · — · — |

All rows: evidence tier 1 (static/build/docs); none touches Azure.

## 6. Routing table

| Need | Subagent | Skills (exact name · pinned source) | MCP |
| --- | --- | --- | --- |
| Board setup, ticket creation | — (parent session) | `kanmer-setup`, `kanmer-tickets` · `.grok/skills/` Kanmer 0.1.0 | Kanmer: `get_status`, `list_board`, `create_group`, `create_item`, `link_doc`, `set_ticket_doc`, `get_doc_gates` |
| ADR/FRD/PRD authoring | `pegasus-parity-researcher` for evidence gathering | `kanmer-docs` · Kanmer 0.1.0; `microsoft-docs` for API claims | Kanmer `link_doc`; Microsoft Learn `microsoft_docs_search` |
| Independent review of any governance PR | `pegasus-desktop-reviewer` | `kanmer-review` | Kanmer `get_ticket_doc` |
| Release-tag convention, gateway release | `pegasus-release-packager` | `pegasus-release` · `.agents/skills/pegasus-release/SKILL.md` | — |
| Upstream sync review | `pegasus-desktop-reviewer` | — | — |

Every ticket's plan document must carry a `## Routing` block using these
names; the project skill `pegasus-desktop`
(`.agents/skills/project/pegasus-desktop/SKILL.md`, see
[12](../12-agent-tooling/README.md)) is loaded first by every agent.

## 7. Risks and traps

- **Placement gate**: any `.md` outside `docs/(prd|frd|adr|design|desktop)`
  fails the CI `documentation` job; ticket-transient documents go to Kanmer,
  not the tree.
- **ADR collision with upstream**: taking "next free number" in the fork
  while syncing upstream produces duplicate IDs; hence the reserved block.
  Check `docs/adr/README.md` after every sync.
- **`main`-history guard**: `scripts/Test-MainBranchHistory.ps1` fails a
  push to `main` whose history is not contained in `dev`; never merge
  upstream straight into `main`.
- **Upstream sync brings Razor/web changes** the conversion intends to
  retire; merge them anyway until cutover (the web app is live) and do not
  "fix forward" in the fork what upstream owns — raise it upstream.
- **Kanmer gates**: a move crosses one gated boundary; an unticked
  `open-questions/` item blocks; `board.yml` is not the effective gate set.
- **Capability vs ticket IDs**: `CASE-17` (capability, two digits) is not
  `CASE-017` (ticket); plans use `DSK-<area>-<nn>` handles to avoid the
  collision.
- **Operator copy rules** apply to every UI ticket; a plan that "explains"
  is a defect under `docs/design/README.md`.
- **Two environments only** (ADR-0014) — a ticket that asks for an Azure
  test resource is out of bounds without a new accepted decision.

## 8. Documentation changes

| Document | Change |
| --- | --- |
| `docs/adr/0100…0110-*.md`, `docs/adr/README.md` | New ADRs and index rows; ADR-0100 records the narrow ADR-0009 deferral-clause supersession in its Context, while ADR-0009 remains unchanged |
| `AGENTS.md` | § ADR conventions: reserved block recorded (done, 2026-08-23); § New Markdown placement: done in this task |
| `docs/index.md` | Desktop plan-set row (done); FRD-13 link |
| `docs/frd/frd-13-desktop-operator-experience.md`, `docs/frd/README.md` | New FRD |
| `docs/prd/pegasus-product.md` | Scope: native desktop client, web retirement after cutover |
| `docs/capabilities.md` | `DSK` family rows with canonical owners; allocation summary |
| `docs/engineering.md` | § Branches and delivery: `upstream` one-way sync, release tags |
| `docs/operations.md`, `docs/current-architecture.md` | Only when a deployment changes (gateway releases from the fork after D-001) |
| `docs/boundaries.md` | Web front end becomes a deprovision candidate after cutover (recorded, not executed) |
