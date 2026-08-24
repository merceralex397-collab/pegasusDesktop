---
id: FEAT-001
type: ticket
title: DSK-05-01 · S1 Dashboard and work queue
status: preparing
area: desktop-features
assignee: ''
profile: feature
stageEntered:
  preparing: '2026-08-24T21:31:32.542Z'
labels:
  - desktop-conversion
  - plan-05
  - phase-3
  - tier-5
  - tier-7
  - tier-12
groups:
  - EPIC-006
  - HZN-004
links: []
blocks:
  - FEAT-002
  - FEAT-020
  - FEAT-022
  - FEAT-025
  - TEST-007
refs:
  - docs/frd/frd-12-operator-experience.md
docs_todo: true
archived: false
created: '2026-08-24T07:46:33.781Z'
updated: '2026-08-24T21:31:32.542Z'
---

## What

Deliver the native WinUI 3 Dashboard screen, its view model and the shell rail counts, driven by `GET /api/v1/dashboard` and `GET /api/v1/dashboard/rail-counts`, so that on launch the operator sees assigned work, new/unassigned items, overdue work, integration failures and recent cases and can open any of them in one action.

## Why

Proposal §13.2 and §14.3 require the first screen to answer five questions from live data with actionable lists, not vanity charts. Today the dashboard is `src/Pegasus.Web/Pages/Index.cshtml.cs` (43 lines, one `OnGetAsync` over `IGetOperationsSnapshot`) and the rail badges come from a side channel — `src/Pegasus.Web/Presentation/RailCountsPageFilter.cs:29` writes `ViewData["RailCounts"]` on every authenticated request. The desktop cannot reproduce an `IAsyncPageFilter`, so counts and lists must become one query contract. This is the first slice of Phase 3: it proves shell, session, gateway, contracts and tests end to end. Siblings: [[DSK-03-06]] supplies the endpoints, [[DSK-06-04]] the shell rail, [[DSK-05-02]] follows with the case list.

## Source of truth

- Plan row: `docs/desktop/05-implementation-and-migration/README.md` § 5 — `DSK-05-01`
- Plan detail: `docs/desktop/05-implementation-and-migration/vertical-slices.md` § `Common to every slice` and § `S1 · Dashboard and work queue (DSK-05-01)`
- Reuse map: `docs/desktop/05-implementation-and-migration/reuse-map.md` § `Pegasus.Web — REPLACE pages, KEEP the host` (row `Presentation/RailCountsPageFilter.cs` → REPLACE by a gateway endpoint)
- Endpoint map: `docs/desktop/03-gateway-api-and-data/endpoint-map.md` § `Dashboard and rail counts`
- Screen spec: `docs/desktop/06-ui-design/screen-specs.md` § `§13.2 Dashboard and work queues` → `Dashboard`
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 13.2 Dashboard and work queues, § 14.3 Dashboard, § 14.2 Main shell
- Repository evidence: `src/Pegasus.Web/Pages/Index.cshtml.cs:26` (`OnGetAsync` over `IGetOperationsSnapshot`), `src/Pegasus.Web/Presentation/RailCountsPageFilter.cs:29-51`, `src/Pegasus.Core/Operations/` (`IDashboardQueries`, `IGetOperationsSnapshot`), `tests/Pegasus.IntegrationTests/Browser/OperatorJourneyTests.cs`
- Binding decisions: L-01 the gateway is `Pegasus.Web` evolved in place — the dashboard endpoints are a versioned `/api/v1` route group beside the Razor Pages, same Container App; L-02 Test/UAT is the local production-mimicking stack, never an Azure test resource; L-04 every ticket names its subagent, skills and MCP tools
- Depends on: `DSK-02-08` shell, navigation and status-bar services; `DSK-02-13` the `tests/Pegasus.Desktop.ViewModelTests` project; `DSK-03-06` the dashboard and rail-count endpoints; `DSK-04-07` the desktop session client that supplies the bearer token; `DSK-06-04` the NavigationView rail and its count slots; `DSK-06-13` the screen spec adopted as an FRD-13 section

## Routing

- **Subagent**: `winui-dev` — `.codex/agents/winui-dev.toml` (screen and view model); `pegasus-gateway-dev` — `.codex/agents/pegasus-gateway-dev.toml` (any gap in the `/api/v1` dashboard group); `pegasus-test-engineer` — `.codex/agents/pegasus-test-engineer.toml` (view-model and contract tests); `pegasus-ui-verifier` — `.codex/agents/pegasus-ui-verifier.toml` (UI script and axe scan)
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `winui-dev-workflow` (`.codex/skills/winui-dev-workflow/SKILL.md`, win-dev-skills v0.5.0 `f1028dd5`) → `winui-design` (`.codex/skills/winui-design/SKILL.md`) → `dotnet-webapi` (dotnet/skills `98f84851`, `plugins/dotnet-aspnetcore/skills/dotnet-webapi/SKILL.md`) → `code-testing-agent` and `run-tests` (dotnet/skills `98f84851`, `plugins/dotnet-test/skills/`) → `winui-ui-testing` (`.codex/skills/winui-ui-testing/SKILL.md`) → `winui-code-review` (`.codex/skills/winui-code-review/SKILL.md`) at review
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search`, `microsoft_docs_fetch`, `microsoft_code_sample_search`)
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (call `get_doc_gates <id>` before every move; a move crosses at most one gated boundary)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Orient: read the plan row and the plan sections listed under Source of truth, plus `docs/design/README.md` § `No explanatory copy and page economy` and its banned-words list. Then call `get_doc_gates <this ticket id>` and `take_ticket` with branch `task/dsk-05-01-dashboard` and worktree `../pegasus-worktrees/dsk-05-01-dashboard` created from `origin/dev`.
2. Load `pegasus-desktop`, then `winui-dev-workflow` and `winui-design`. Use `.codex/skills/winui-dev-workflow/BuildAndRun.ps1` for every local run of the packaged app (it launches with package identity, which a plain `dotnet run` does not), and `winui-search.exe` under `.codex/skills/winui-design/` for control API lookups.
3. Record current behaviour in the ticket `research` document: read `src/Pegasus.Web/Pages/Index.cshtml.cs` in full and list the six projection members it surfaces — `Counts` (`IntakeQueueCounts`), `DueWork` (`IReadOnlyList<CaseDueWork>`), `CaseStages` (`CaseStageCounts`), `CaseActivity` (`CaseActivityCounts`), `MailActivity` (`MailActivityCounts`), `LoadedAtUtc` — and read `src/Pegasus.Web/Presentation/RailCountsPageFilter.cs`, noting that only `Queues` gets a real figure (`counts.NotReady + counts.Review + counts.Held`) and absent keys render nothing. Record the exact commit SHA you read them at (parity-drift trap: upstream keeps changing these files).
4. Verify the gateway contract from [[DSK-03-06]] covers those six members plus `asOfUtc` and a weak `ETag`. Where a field is missing, extend the endpoint in `src/Pegasus.Web` inside the `/api/v1` route group gated by `Features:DesktopGateway`, calling the **same** `IGetOperationsSnapshot` / `IDashboardQueries` use case the Razor page calls — never a second query implementation (`docs/engineering.md` § One Core owner). Done when a fact in `tests/Pegasus.Api.ContractTests` asserts every field with the gate enabled.
5. Add the response DTOs to `src/Pegasus.Contracts` following the conventions established by [[DSK-02-04]] / [[DSK-03-01]] (paging envelope, no enum `ToString()` on the wire, `asOfUtc` as `DateTimeOffset`). No ASP.NET, EF or WinUI type may appear in this project — `tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs` enforces it.
6. Implement `DashboardViewModel` in `src/Pegasus.Desktop` with explicit `Loading` / `Empty` / `Error` / `Loaded` states, a single coalesced `RefreshCommand` (a second refresh while one is in flight joins the first, it does not queue), a `LastLoadedAt` shown as Europe/London through the shared vocabulary map, and cancellation on navigation away. It calls only the generated client from [[DSK-03-05]] through `Pegasus.Desktop.Infrastructure`; it never references `Pegasus.Infrastructure`.
7. Build the XAML page for the five §14.3 questions as actionable lists and counts — what needs attention now, what is assigned to me, what is new or overdue, did any intake or integration fail, which cases did I recently use. Every interactive control carries `AutomationProperties.AutomationId` per the convention in `docs/desktop/06-ui-design/screen-specs.md` § `AutomationId convention`. Status values render through the shared vocabulary list with text, never colour alone; no field hints and no how-it-works copy (`docs/design/README.md` — this is a merge rule).
8. Wire the shell rail counts to `GET /api/v1/dashboard/rail-counts` through the shell view model from [[DSK-06-04]], preserving the existing semantics exactly: a count that the gateway omits renders nothing, never a zero. Do not invent a figure for Inbox or Cases.
9. Bind `F5` and `Ctrl+R` to `RefreshCommand` per proposal §14.9, and show the freshness state (current / stale / unavailable) in the page header control from [[DSK-06-12]].
10. Write view-model tests in `tests/Pegasus.Desktop.ViewModelTests` (project from [[DSK-02-13]]) covering each state, refresh coalescing, cancellation, and an error response mapped to the `InfoBar` problem presentation. Run `dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release --no-build`.
11. Add contract tests in `tests/Pegasus.Api.ContractTests` for both endpoints: gate off → 404, gate on + no token → 401, gate on + staff token → 200 with every field, `If-None-Match` with the returned `ETag` → 304. Enable `Features:DesktopGateway` explicitly in the factory — a registered but gated-off endpoint returns 404 and will otherwise look like a routing bug.
12. Add a `winapp ui` script under `tests/Pegasus.Desktop.UITests` (harness from [[DSK-08-06]]) that launches the app, waits for the dashboard AutomationIds, traverses every list by keyboard only and opens one item; then run the `axe-windows` scan from [[DSK-06-15]] on the screen and attach both artefacts to the ticket proof.
13. Produce the parity comparison table: sign into the local Test/UAT stack, load the web dashboard and the desktop dashboard against the same database, and record web counts vs desktop counts per figure in the ticket proof. Any difference is a defect in this slice, not an accepted deviation.
14. Update `docs/desktop/01-inventory-and-parity/parity-matrix.md` row `PAR-05` to `implemented` (and to `automated verification passed` once step 12 is green), add the Dashboard section to `docs/frd/frd-13-desktop-operator-experience.md` and a `DSK` row to `docs/capabilities.md`. Run the simplification pass over the branch diff (`AGENTS.md` step 4), record it under a dated `## Simplification pass` heading in the ticket plan, then open the PR into `dev`.

## Acceptance criteria

- [ ] The five §14.3 questions are answered on the Dashboard from live gateway data, with no vanity chart.
- [ ] Rail counts equal the web rail counts for the same dataset; an omitted count renders nothing, never a zero.
- [ ] Every list is reachable and openable by keyboard alone; every interactive control has an `AutomationId`.
- [ ] Freshness time is visible and refresh is coalesced under `F5` / `Ctrl+R`.
- [ ] No banned operator word and no explanatory copy reaches the screen.
- [ ] The desktop project references neither `Pegasus.Infrastructure` nor EF Core.

## Verification

- [ ] `dotnet build ./Pegasus.slnx --configuration Release --no-restore` — expected: succeeds with `TreatWarningsAsErrors=true` and no `WUI*` suppression.
- [ ] `dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release --no-build` — expected: all dashboard view-model facts pass.
- [ ] `dotnet test ./tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj --configuration Release --no-build` — expected: dashboard gate-off 404, 401, 200 and 304 facts pass.
- [ ] `dotnet test ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj --configuration Release --no-build` — expected: dependency-direction facts stay green.
- [ ] `pwsh ./tests/Pegasus.Desktop.UITests/ui-tests.ps1 -Script dashboard` — expected: script passes with no sleep-based waits; screenshot and axe report attached.
- [ ] Parity table in the ticket proof — expected: every desktop count equals the web count for the same database.

## Evidence tier

Tier 5 — Web/API/MCP caller. Tier 7 — Browser/accessibility. Tier 12 — Integrated workflow.
Tier 5 obliges observable route-level evidence that the real `/api/v1/dashboard` endpoints reach the same Core queries with authentication and exception translation; tier 7 obliges the keyboard, focus, semantic-label and text-plus-colour evidence from a real run of the app; tier 12 obliges the end-to-end comparison against the live web dashboard on the same data — a mocked path does not satisfy it.

## Documentation changes

- `docs/desktop/01-inventory-and-parity/parity-matrix.md` — row `PAR-05` status and evidence pointers
- `docs/frd/frd-13-desktop-operator-experience.md` — Dashboard section (skeleton owned by [[DSK-00-08]])
- `docs/capabilities.md` — `DSK` family row for the dashboard, canonical owner FRD-13

## Guardrails

- **Azure**: no write.
- **Scope boundary**: may touch `src/Pegasus.Desktop`, `src/Pegasus.Desktop.Infrastructure`, `src/Pegasus.Contracts`, the `/api/v1` group in `src/Pegasus.Web` and the test projects. Must not touch `src/Pegasus.Infrastructure`, `src/Pegasus.Worker`, any Razor page or `RailCountsPageFilter.cs` — the web dashboard stays live and unchanged until cutover.
- **Traps**: do not reproduce web mechanics (`ViewData`, `TempData`, PRG, antiforgery) — desktop state lives in the view model; `/api/v1` behind `Features:DesktopGateway` returns 404 when gated off, so integration tests must enable the gate explicitly; the design authority is a merge rule (no field hints, no how-it-works copy, only populated sections render, filters are dropdowns, newest first); banned words (`intake`, `lease`, `artifact`, `projection`, `bytes`, …) never reach the UI; parity drift — re-read `Pages/Index.cshtml.cs` after the latest upstream sync and record the revision characterized; `TreatWarningsAsErrors=true` and `AnalysisLevel=latest-recommended` apply, so fix `WUI*` analyzer warnings rather than suppressing them wholesale.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.

## Outcome

_Filled at closeout._
