# 01 · Inventory and parity (Phase 0)

Phase 0 of the conversion: discover, inventory, and decide before any native
code is written. This folder holds the plan and four working documents that
the Phase 0 tickets fill in:

| File | What it is |
| --- | --- |
| [parity-matrix.md](parity-matrix.md) | The repository-derived feature-parity matrix, pre-populated with every Razor page model and handler in `src/Pegasus.Web` |
| [flow-records.md](flow-records.md) | Six current-flow records (authentication, database/migrations, Graph intake, Box custody, DVLA/DVSA, report rendering) pre-filled from the code |
| [azure-resource-register.md](azure-resource-register.md) | The Azure resource register pre-filled from `infra/` and `docs/operations.md`, with read-only verification commands |
| [upstream-kanmer-carryover.md](upstream-kanmer-carryover.md) | Historical triage of the original board and the current in-repository disposition; upstream sync instructions are superseded |

## 1. Purpose and proposal coverage

Phase 0 turns the proposal's capability groups into a row-per-capability
inventory with evidence, records the current authentication, database, Graph,
Box, DVLA/DVSA, and report flows, inventories every Azure resource and its
caller, and pins the baseline that parity will be measured against. Nothing in
Phase 0 changes runtime behaviour, Azure state, or the web application.

Proposal sections implemented here:

- §13 Current and desired Pegasus functionality (13.1–13.11) — the inventory
  groups and the rule that future channels are not "parity".
- §23 Verification and feature parity — the matrix columns, status ladder, and
  required conversion evidence (§23.1); §23.2 is verified by areas 08/09.
- §24 Phase 0 — discovery, inventory and decisions, including its exit gate.
- §29 Immediate next actions items 2 (parity matrix), 3 (Azure inventory
  without removing anything), 4 (record the auth/database/Graph/Box/DVLA
  flows), and 5 (pin the skill revisions — executed in area 12, recorded
  here as a dependency).
- §4 / §4.1 placement decisions — each inventory row carries the placement
  column that area 11 turns into a cloud-dependency record.
- §19 first action: "inventory and tag every current resource and identify
  which code path uses it".

## 2. Evidence base

### Facts

Verified on 2026-08-23 by read-only inspection of the fork at `main`
`191ddf33`. The historical upstream comparison is retained as provenance only;
the operator boundary below prohibits any new upstream synchronization.

Web surface (what the parity matrix enumerates):

- `src/Pegasus.Web/Pages/**` — 53 page models, 76 `.cshtml`, ~10,800 lines;
  base classes `Pages/Shared/StaffPageModel` (18 lines, 18 pages),
  `Pages/Administration/AdministrationPageModel.cs` (7 lines, 16 pages),
  `Pages/Cases/CaseMutationPageModel.cs` (339 lines, 7 case pages),
  `Pages/UploadConfirmationPageModel.cs` (82 lines, 2 pages).
- Largest page models: `Pages/Mail/Message.cshtml.cs` 1,025 lines;
  `Pages/Cases/Assessment/Index.cshtml.cs` 740; `Pages/Cases/Create.cshtml.cs`
  689; `Pages/Cases/Details.cshtml.cs` 654; `Pages/Intake/Details.cshtml.cs`
  613; `Pages/Triage/Details.cshtml.cs` 496.
- Non-Razor HTTP surface: `/health/live`, `/health/ready`
  (`src/Pegasus.Web/Program.cs:939-950`), `GET /diagnostics/version`
  (`Program.cs:954`), OpenIddict `/connect/token` and `/authorize`
  (`src/Pegasus.Web/Mcp/AutomationMcpExtensions.cs:134`), `/mcp`
  (`AutomationMcpExtensions.cs:137`, 35 `pegasus_*` tools). There is no
  OpenAPI document and no client-version concept.
- Core (`src/Pegasus.Core`, 107 files, 227 port interfaces, zero package
  references): the transport-neutral seam is `Actors/StaffActorFactory.cs`
  (`TryCreate(subjectId, roleNames, out ActionActor)`) with
  `Identity/StaffAuthorization.cs` (12 `StaffAccessRight` values, fail-closed
  switch) and the universal mutation envelope `CaseMutationRequest`
  (`Workflow/CaseWorkflowContracts.cs:182`: `CaseId`, `ExpectedVersion`,
  `Actor`, `OperationKey`, `Reason`, `EditLeaseToken`).
- Tests: xunit 2.9.3 only; `tests/Pegasus.Core.Tests` 69 files (~494 facts,
  72 theories), `tests/Pegasus.IntegrationTests` 136 files (~716 facts,
  47 theories, `WebApplicationFactory<Program>` in 59 files, LocalDB, three
  CI shards), `tests/Pegasus.ArchitectureTests` 11 files (custom reflection,
  `DependencyDirectionTests.cs` 520 lines). Browser lane:
  `tests/Pegasus.IntegrationTests/Browser/` (9 files, 20 facts, Playwright +
  `Deque.AxeCore.Playwright`).
- Azure estate: `infra/main.bicep` (subscription scope, RG `rg-pegasus-prod`
  at line 71, default region `uksouth` line 32) and
  `infra/modules/platform.bicep` (34 declared resources/assignments, see the
  register). Current production release: release 20, 2026-08-22
  (`docs/operations.md:311-332`); `docs/operations.md:295` still says
  "release 14" — drift, not state.
- Upstream board: 456 tickets, 109 open and non-archived, 10 areas, 8 epics,
  3 horizons (`collisionengineers/pegasus@4694067`, branch `kanmer-board`).
  Upstream `main` `7d6a948a` is 32 commits ahead of the fork's `main`, and
  the fork head is an ancestor of it.
- Capacity targets the baseline must be sized against:
  `docs/engineering.md:72-89` tier 10 — eight concurrent operators, 2,000
  cases per month, 10 MiB single-file limit.
- Governance: new Markdown under `docs/desktop/` is permitted by the
  placement gate (`scripts/Test-MarkdownPlacement.ps1`, allowed roots now
  include `docs/desktop`); `corpus/` is ignored and immutable and domain data
  must never be fabricated (`AGENTS.md` safety rails).
- Proposal §23 status ladder and §23.1 evidence list (restated in the
  matrix legend).

### Assumptions

- The 53 page models and their handlers are the complete staff surface; a
  Phase 0 ticket re-derives the list from `git ls-files 'src/Pegasus.Web/**/*.cshtml.cs'`
  before marking any row `inventoried`.
- The operator will name one UAT owner per capability group; the matrix
  leaves that column blank rather than guessing.
- Baseline performance numbers will be captured on the lowest-spec supported
  office workstation (proposal §15.1) against the production-like local stack
  defined in area 08; no Azure load testing is assumed.
- The carry-over triage is historical evidence. No upstream board or remote is
  read during this refactor; any work needed from that history is recreated or
  amended as an in-repository ticket.

## 3. Decisions and assumptions

Locked decisions this area depends on (see the index):

- L-01 gateway = `Pegasus.Web` evolved in place — the matrix's "API/data
  dependency" column names `/api/v1` endpoints that live in the existing
  project.
- L-02 Test/UAT is a local production-mimicking stack — the baseline
  performance capture and all characterization runs happen there or on the
  production pilot ring, never in an Azure test environment.
- L-05 Kanmer is seeded by the implementing agent from these plans — the
  carry-over document is the seed list for carried-over upstream work.
- Operator boundary (2026-08-25): all work remains in this repository on the
  configured `pegasusDesktop` remote. No upstream remote operation, cloud write,
  deployment, credential change, or external environment change is permitted
  until the full refactor is complete.

Decisions taken inside this area:

- The parity matrix is keyed by **page model + handler group**, not by URL,
  because handler names are the stable evidence of what the web app does
  today (`OnPost*Async` names are the command inventory).
- Rows are grouped by proposal §13 capability group and carry the owning FRD
  so that behaviour questions route to the FRD, not to the old page.
- Two page models are explicitly **not** desktop surfaces and stay server
  side: `Pages/Uploads/Request.cshtml.cs` (anonymous external request-link
  upload) and `Pages/Connect/Authorize.cshtml.cs` (OpenIddict consent for
  external MCP connectors, ADR-0027). They are inventoried with status
  `legacy path retained`, a status added to the ladder — Deviation: proposal
  §23 has no "retained" state; without it these rows would never close.
- `Error` and `StatusCode` pages are inventoried as "web shell only" and map
  to the desktop error/empty-state catalogue in area 06, not to screens.
- ADR references use the reserved desktop block ADR-0100…ADR-0110 (area 00).

⚠ Azure writes: none. Every Azure action in Phase 0 is a read (Azure MCP
list/show tools, `az ... show/list`). Tagging resources ("inventory and tag",
proposal §19) would be a write and is deferred: the register carries the tag
values as a column; applying them needs exact-target approval and is listed
in area 11.

## 4. Target state and exit gate

Target state at the end of Phase 0:

- `parity-matrix.md` has one row per observable capability, each at least
  `inventoried` (entry point, behaviour evidence, test evidence, FRD owner,
  capability group filled); UAT owner column assigned by the operator.
- `flow-records.md` has all six flows completed and reviewed, with their
  open questions answered or moved to `docs/open-decisions.md`.
- `azure-resource-register.md` has an "Used by (code path)" and "target
  position" for every resource, verified by a read-only Azure MCP run whose
  output is attached to the ticket proof.
- `upstream-kanmer-carryover.md` dispositions are accepted and the
  carried-over tickets exist on the fork board. The historical sync step is
  superseded by the operator boundary; no upstream sync is a Phase 0 exit
  condition.
- Baseline performance and critical business fixtures are recorded (web app,
  production-like local stack, named workstation spec).
- Dependency rules are written as architecture-test targets for area 02
  (desktop projects must not reference `Pegasus.Infrastructure`, EF Core,
  Azure SDKs, or server adapters).

Exit gate (proposal §24 Phase 0, verbatim intent):

1. Every current production capability has an inventory row.
2. Every Azure resource has an owner/use statement.
3. No unresolved uncertainty exists around authentication, database, or
   Graph intake (flow records 1–3 closed).
4. Target dependency rules compile as architecture tests or documented checks.

Proof: the four documents updated in the same PR as the tickets that filled
them, plus the attached read-only command outputs (Azure MCP, `git ls-files`,
test enumeration).

## 5. Work breakdown

All tickets are documentation/evidence work on the fork board area
`desktop-foundation` (FND) unless stated; profile `spike` for pure
discovery, `chore` for board/doc mechanics.

| ID | Title | Profile | Depends on | Acceptance | Verification | Tier | Routing |
| --- | --- | --- | --- | --- | --- | --- | --- |
| DSK-01-01 | Re-derive the page-model inventory and confirm the parity-matrix skeleton | spike | — | `git ls-files 'src/Pegasus.Web/**/*.cshtml.cs'` count equals matrix rows; every `OnGet*`/`OnPost*` handler appears once | Attach the file list and a grep of `OnGet*Async` and `OnPost*Async` handlers per file to the ticket | 1 | `pegasus-parity-researcher` · `kanmer-research` · Kanmer |
| DSK-01-02 | Populate parity rows for §13.1 Access and session and §13.2 Dashboard/work queues | spike | DSK-01-01 | Rows for Account/*, Index (dashboard), Search, rail counts at `inventoried` with test evidence | Row-by-row link check to cited files; reviewer confirms | 1 | `pegasus-parity-researcher` · `kanmer-research` · Kanmer |
| DSK-01-03 | Populate parity rows for §13.3 Case lifecycle and §13.6 Parties/reference data | spike | DSK-01-01 | Cases/* (12 page models, all handlers) and Administration/Organizations, Principals rows at `inventoried` | As above; cites `CaseDetailsWebTests.cs`, `CaseWorkflowPersistenceTests.cs` | 1 | `pegasus-parity-researcher` · `kanmer-research` · Kanmer |
| DSK-01-04 | Populate parity rows for §13.4 Intake, §13.7 Documents/evidence, §13.8 Communications | spike | DSK-01-01 | Intake/*, Mail/*, Triage/*, Unidentified/*, ImageIntake/*, Upload* rows at `inventoried` | Cites `QdosIntakeWebTests.cs`, `MailWorkspaceWebTests.cs`, `MultiFormatIntakeWebTests.cs`, `RetainedMailPersistenceTests.cs` | 1 | `pegasus-parity-researcher` · `kanmer-research` · Kanmer |
| DSK-01-05 | Populate parity rows for §13.5 Vehicle/inspection, §13.9 Assessment/reports, §13.10 Administration/operations | spike | DSK-01-01 | Cases/Vehicle, Cases/Assessment, Cases/Eva, Operations, Administration/* rows at `inventoried` | Cites renderer and assessment tests under `tests/Pegasus.IntegrationTests/Reports/` and `Browser/AssessmentReadinessSummaryBrowserTests.cs` | 1 | `pegasus-parity-researcher` · `kanmer-research` · Kanmer |
| DSK-01-06 | Complete flow records 1–3 (authentication, database/migrations, Graph intake) | spike | — | Each record's open questions answered with a code citation or moved to `docs/open-decisions.md`; Phase 0 exit gate item 3 satisfied | Reviewer re-runs the listed read-only commands | 1, 4 | `pegasus-parity-researcher` · `kanmer-research`, `microsoft-docs` · Kanmer, Microsoft Learn |
| DSK-01-07 | Complete flow records 4–6 (Box custody, DVLA/DVSA, report rendering) | spike | — | As DSK-01-06; report-rendering record feeds ADR-0108 context | As above | 1, 3 | `pegasus-parity-researcher` · `kanmer-research`, `microsoft-code-reference` · Kanmer, Microsoft Learn |
| DSK-01-08 | Verify the Azure resource register read-only and fill "Used by" and target position | spike | — | Every register row has a verified existence check, a code-path owner, and a §19 position; the "does not exist" list confirmed | Azure MCP outputs attached (`group_resource_list`, `storage`, `keyvault`, `monitor`, `applicationinsights`, `sql`, `containerapps`, `functionapp`); zero writes | 9 | `pegasus-azure-auditor` · `azure-resource-lookup`, `azure-resource-visualizer`, `azure-cost` · Azure MCP |
| DSK-01-09 | Triage the upstream board and recreate carried-over tickets on the fork board | chore | — | Every `desktop-screen-spec`, `gateway-worker-ticket`, and `report-decision` row recreated with `refs` to the upstream ID and the original body; `unchanged-backlog` rows listed, not recreated | `list_items` count matches the carry-over table; spot-check 5 tickets' bodies | 1 | `pegasus-parity-researcher` · `kanmer-tickets`, `kanmer-groom` · Kanmer |
| DSK-01-10 | First one-way upstream sync (`upstream/main` → fork `dev`) | chore | area 00 branch creation | 32 upstream commits merged via PR; CI green; `docs/operations.md` release table shows releases 21–24 | `git log --oneline dev..upstream/main` empty after merge | 1 | `pegasus-gateway-dev` · `run-tests` · — |
| DSK-01-11 | Record baseline performance and critical business fixtures | spike | area 08 Test/UAT stack ticket | Cold/warm page timings, list paging, report generation, and memory for the web app on the named workstation against the local stack; fixtures listed by path (no fabricated data) | Numbers recorded in `10-security-observability-performance` baseline table with commands | 10 | `pegasus-ui-verifier` · `analyzing-dotnet-performance`, `dotnet-trace-collect` · — |
| DSK-01-12 | Characterization-test gap list for Core policies and dependency-rule targets | spike | DSK-01-03, DSK-01-04 | Gap list per Core folder (Intake, Workflow, Lifecycle, Assessment, Vehicle, Triage, Reports) with the lowest reliable boundary named; architecture-test targets written for area 02 | Reviewer checks each gap against `tests/Pegasus.Core.Tests` | 1, 2 | `pegasus-test-engineer` · `test-gap-analysis`, `assertion-quality` · — |
| DSK-01-13 | Maintain the in-repository release boundary through the refactor | chore | none under the current operator boundary | Historical upstream-sync instructions are explicitly superseded; all work stays in this repository on `pegasusDesktop`, and cloud/deployment/external changes remain deferred | `git remote -v`; `git diff --check`; documentation and Markdown-placement gates; no upstream operation performed | 1 | parent session · `pegasus-desktop`, Kanmer · configured `pegasusDesktop` remote |

## 6. Routing table

| Work | Subagent | Skills (pinned source) | MCP tools |
| --- | --- | --- | --- |
| Page/handler/use-case inventory, parity rows, flow records | `pegasus-parity-researcher` (read-only) | `kanmer-research` (Kanmer 0.1.0); `microsoft-docs`, `microsoft-code-reference` (Microsoft Learn plugin) for API facts | Kanmer `get_status`, `list_items`, `set_ticket_doc`, `append_scratch`; Microsoft Learn `microsoft_docs_search` |
| Azure register verification | `pegasus-azure-auditor` (read-only) | `azure-resource-lookup`, `azure-resource-visualizer`, `azure-cost` (`microsoft/azure-skills` `1a03acfb`) | Azure MCP `group_resource_list`, `storage`, `keyvault`, `monitor`, `applicationinsights`, `sql`, `containerapps`, `functionapp`, `pricing` — list/show only |
| Board seeding and carry-over | `pegasus-parity-researcher` | `kanmer-tickets`, `kanmer-groom`, `kanmer-setup` | Kanmer `create_item`, `create_group`, `link_doc`, `link_items`, `update_item` |
| Upstream sync PR | `pegasus-gateway-dev` | `run-tests` (`dotnet/skills` `98f84851`) | — |
| Baseline performance | `pegasus-ui-verifier` | `analyzing-dotnet-performance`, `dotnet-trace-collect` (`dotnet/skills`) | — |
| Characterization gaps | `pegasus-test-engineer` | `test-gap-analysis`, `assertion-quality` (`dotnet/skills`) | — |
| Independent review of every ticket above | `pegasus-desktop-reviewer` (read-only) | project skill `pegasus-desktop` | Microsoft Learn |

## 7. Risks and traps

- **Inventory by page count misses behaviour.** Handlers such as
  `Pages/Triage/Details.cshtml.cs` `OnPostActionAsync` dispatch 13 commands
  behind one name; the matrix must list the command set, not the handler.
- **The web app keeps moving.** Upstream is 32 commits ahead today; a matrix
  row marked `inventoried` against the fork head can be stale after a sync.
  Each row records the commit it was inventoried at.
- **Documentation drift already exists** (`docs/operations.md:295` vs its own
  release table; `CHANGELOG.md` stopped at 2026-08-03). Treat the release
  table as authoritative and do not copy the stale line into any record.
- **Capability IDs vs ticket IDs collide in appearance** (`CASE-17` is a
  capability; `CASE-017` is a ticket). The matrix and carry-over tables use
  the full form and never abbreviate.
- **Azure tagging is a write.** The register records intended tags but does
  not apply them (runbook approval matrix).
- **Fabricated data.** Baseline fixtures must come from `reference/` or the
  ignored `corpus/`; never invent VRMs, names, or emails (safety rails).
- **App Insights is capped** (0.1 GB/day, PLAT-034): telemetry queries made
  in UK working hours return empty; do not conclude "no traffic" from them.
- **Runtime-role grants** (PLAT-035): any characterization run against
  production-shaped SQL needs the grant matrix; local full-privilege runs
  prove nothing about deployed permissions.

## 8. Documentation changes

- `docs/desktop/01-inventory-and-parity/parity-matrix.md` becomes the
  canonical parity matrix (proposal names it
  `docs/features/desktop-parity-matrix.md`; Deviation: kept inside the
  plan set because `docs/features/` does not exist and the placement gate
  allows `docs/desktop/`). Area 00 may relocate it by ticket.
- `docs/open-decisions.md`: one line per unresolved flow-record question.
- `docs/capabilities.md`: new `DSK` family rows are created in area 00; this
  area supplies the capability group per row.
- `docs/current-architecture.md` / `docs/operations.md`: not changed in
  Phase 0 (no runtime change). The drift at `docs/operations.md:295` is fixed
  by the first upstream sync PR if upstream already fixed it, otherwise by a
  one-line doc ticket.
- ADR-0100…ADR-0110 context sections cite the flow records by anchor.
