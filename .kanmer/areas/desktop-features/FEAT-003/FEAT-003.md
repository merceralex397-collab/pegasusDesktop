---
id: FEAT-003
type: ticket
title: DSK-05-03 · S3 Case detail read-only and history
status: backlog
area: desktop-features
assignee: ''
profile: feature
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
  - FEAT-004
  - FEAT-005
  - FEAT-007
  - FEAT-022
  - FEAT-025
  - TEST-007
  - TEST-016
refs:
  - docs/frd/frd-01-case-identity-and-lifecycle.md
docs_todo: true
archived: false
created: '2026-08-24T07:46:33.815Z'
updated: '2026-08-24T08:51:09.966Z'
---

## What

Deliver the read-only case workspace: a stable case header (reference, status, assignee, priority, save state, commands) with lazily loaded sub-navigation — Overview · Vehicle · Assessment · Documents · Communications · Tasks · Reports · History — and an audit/history view whose rows match the web for the same case.

## Why

Proposal §13.3 and §14.5 require a stable case header and lazily loaded sections so that a case opens fast and only populated sections render. Today everything lives in `src/Pegasus.Web/Pages/Cases/Details.cshtml.cs` (654 lines) whose `OnGetAsync` at `:110` loads query, edit-lease state and completeness in one pass, with partials under `src/Pegasus.Web/Pages/Cases/Shared/`. This slice closes the Phase 3 read path and is the shell every Phase 4–7 slice hangs its tabs on. Siblings: [[DSK-05-02]] selects the case, [[DSK-03-07]] supplies the header and per-section endpoints, [[DSK-05-05]] adds editing on top of this shell.

## Source of truth

- Plan row: `docs/desktop/05-implementation-and-migration/README.md` § 5 — `DSK-05-03`
- Plan detail: `docs/desktop/05-implementation-and-migration/vertical-slices.md` § `S3 · Case detail read-only and history (DSK-05-03)` and § `Common to every slice`
- Endpoint map: `docs/desktop/03-gateway-api-and-data/endpoint-map.md` § `Cases` (`GET /cases/{id}`, `GET /cases/{id}/…` section endpoints, `GET /audit`)
- Screen spec: `docs/desktop/06-ui-design/screen-specs.md` § `§13.3 Case lifecycle` → `Case workspace`
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 13.3 Case lifecycle, § 14.5 Case workspace
- Repository evidence: `src/Pegasus.Web/Pages/Cases/Details.cshtml.cs:110` (`OnGetAsync`), `src/Pegasus.Web/Pages/Cases/Shared/` partials, `src/Pegasus.Core/Cases/` (`ICaseDataQueries`), `src/Pegasus.Core/Workflow/` (`ICaseWorkflowQueries`), `src/Pegasus.Core/Identity/` (`IActionHistoryWriter` and the history read ports), `tests/Pegasus.IntegrationTests/CaseDetailsWebTests.cs` (1,286 lines)
- Binding decisions: L-01 gateway evolves inside `Pegasus.Web`; L-02 verification runs on the local Test/UAT stack; L-04 routing named on the ticket
- Depends on: `DSK-05-02` the list that opens a case; `DSK-03-07` the case header, per-section and history/audit read endpoints with per-section `ETag`

## Routing

- **Subagent**: `winui-dev` — `.codex/agents/winui-dev.toml`; `pegasus-gateway-dev` — `.codex/agents/pegasus-gateway-dev.toml`; `pegasus-test-engineer` — `.codex/agents/pegasus-test-engineer.toml`
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `winui-dev-workflow` (`.codex/skills/winui-dev-workflow/SKILL.md`) → `winui-design` (`.codex/skills/winui-design/SKILL.md`) → `dotnet-webapi` (dotnet/skills `98f84851`, `plugins/dotnet-aspnetcore/skills/dotnet-webapi/SKILL.md`) → `run-tests` (dotnet/skills `98f84851`) → `winui-code-review` at review
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search`, `microsoft_code_sample_search`)
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (call `get_doc_gates <id>` before every move; a move crosses at most one gated boundary)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Orient: read the plan row and the sections under Source of truth, plus `docs/design/README.md` § `No explanatory copy and page economy` ("only populated, relevant sections render"). Call `get_doc_gates <this ticket id>`, then `take_ticket` with branch `task/dsk-05-03-case-detail` and worktree `../pegasus-worktrees/dsk-05-03-case-detail` from `origin/dev`.
2. Load `pegasus-desktop`, `winui-dev-workflow` and `winui-design`. Read `src/Pegasus.Web/Pages/Cases/Details.cshtml.cs` `OnGetAsync` (`:110-155`) and every partial in `src/Pegasus.Web/Pages/Cases/Shared/`; record in `research` which fields belong to the header, which to Overview, and which to each tab, plus the SHA read.
3. Confirm from [[DSK-03-07]] that `GET /api/v1/cases/{id}` returns the header plus the Overview section with a `version` and weak `ETag`, and that the section endpoints (`/vehicle`, `/assessment`, `/documents`, `/communications`, `/tasks`, `/reports`, `/history`) each carry their own `ETag` so they can load independently. History rows come from the action-history read ports in `src/Pegasus.Core/Identity/` — the same source the web uses.
4. Implement `CaseWorkspaceViewModel` in `src/Pegasus.Desktop` holding the header state and a child view model per tab. A tab's data loads on first activation, not on case open; each child exposes its own Loading/Empty/Error/Loaded state and can be refreshed independently. Cache section payloads by `ETag` for the lifetime of the open case and revalidate with `If-None-Match` on manual refresh.
5. Build the workspace XAML: a stable header showing reference, status, assignee, priority and save state with the command bar slot (commands themselves arrive in [[DSK-05-06]]), the eight-tab sub-navigation in the order given by the screen spec, and a collapsible right-side activity pane. Only populated sections render — a tab with nothing recorded and no available action shows no empty-state panel.
6. Implement the History tab over `GET /api/v1/cases/{id}/history`: newest first, paged, each row rendering actor, action, timestamp (Europe/London through the shared vocabulary map) and reason where recorded. No GUID, hash or version integer reaches the screen.
7. Ensure the whole workspace is reachable without horizontal scrolling at the minimum supported window size from `docs/desktop/06-ui-design/screen-specs.md` § `Shell`, and that focus order runs header → sub-navigation → content → activity pane.
8. Write view-model tests in `tests/Pegasus.Desktop.ViewModelTests` covering lazy tab activation (a tab not visited issues no request), per-tab error isolation (one failing section does not blank the workspace), `ETag` revalidation on refresh, and history paging.
9. Add contract tests in `tests/Pegasus.Api.ContractTests` for the header and every section endpoint: 200 with `version` and `ETag`, 304 on `If-None-Match`, 401 without a token, 403 without `PerformCasework`, 404 for an unknown case. Enable `Features:DesktopGateway` explicitly.
10. Run the parity comparison against `tests/Pegasus.IntegrationTests/CaseDetailsWebTests.cs` scenarios: for three fixture cases, compare the web Details page against the desktop workspace field by field, and compare history rows one to one. Record the table in the ticket proof.
11. Measure the navigation budget: first useful view ≤ 200 ms perceived after the header has loaded (cached navigation budget, proposal §15.1). Record the measurement method and figures in the proof.
12. Add a `winapp ui` script under `tests/Pegasus.Desktop.UITests` that opens a case from the list, cycles every tab by keyboard and asserts the header stays stable; run the `axe-windows` scan and attach both artefacts.
13. Update `docs/desktop/01-inventory-and-parity/parity-matrix.md` row `PAR-08` (read path only — the edit handlers stay with [[DSK-05-05]]), add the case-workspace section to `docs/frd/frd-13-desktop-operator-experience.md`, run the simplification pass and record it under a dated `## Simplification pass` heading, then open the PR into `dev`.

## Acceptance criteria

- [ ] The case header is stable across tab changes and shows reference, status, assignee, priority and save state.
- [ ] Sections load lazily; an unvisited tab issues no request; a failing section does not blank the workspace.
- [ ] Only populated, relevant sections render; no empty-state panels.
- [ ] History rows equal the web history for the same case, newest first.
- [ ] The workspace is reachable without horizontal scrolling at the minimum supported window size and is fully keyboard traversable.
- [ ] No GUID, hash, version integer or banned operator word reaches the screen.

## Verification

- [ ] `dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release --no-build` — expected: lazy-load, error-isolation, revalidation and paging facts pass.
- [ ] `dotnet test ./tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj --configuration Release --no-build` — expected: header and section 200/304/401/403/404 facts pass.
- [ ] `pwsh ./tests/Pegasus.Desktop.UITests/ui-tests.ps1 -Script case-detail` — expected: tab cycle by keyboard passes; axe report attached.
- [ ] Parity table in the ticket proof — expected: field-by-field and history-row equality against the web Details page for three fixture cases.

## Evidence tier

Tier 5 — Web/API/MCP caller. Tier 7 — Browser/accessibility. Tier 12 — Integrated workflow.
Tier 5 obliges observable evidence that the real section endpoints reach the same Core queries with authorization and exception translation; tier 7 obliges keyboard, focus and semantic-label evidence from a real run; tier 12 obliges the persisted-operator-view comparison against the live web page on the same data.

## Documentation changes

- `docs/desktop/01-inventory-and-parity/parity-matrix.md` — row `PAR-08` read path and history evidence pointers
- `docs/frd/frd-13-desktop-operator-experience.md` — case workspace section
- `docs/capabilities.md` — `DSK` row for the case workspace read path

## Guardrails

- **Azure**: no write.
- **Scope boundary**: may touch `src/Pegasus.Desktop`, `src/Pegasus.Desktop.Infrastructure`, `src/Pegasus.Contracts`, the `/api/v1` cases read group in `src/Pegasus.Web` and the test projects. Must not modify `Pages/Cases/Details.cshtml.cs`, its partials, or `CaseMutationPageModel.cs`.
- **Traps**: only populated sections render — an empty-state panel is a defect under `docs/design/README.md`; do not carry over `TempData["CaseDetailsStatus"]`-style status passing (upstream CASE-001 is dropped for the desktop); `Features:DesktopGateway` must be enabled in tests; parity drift — record the SHA of `Details.cshtml.cs` characterized; upstream CASE-020 (read the case header from the case, not the instruction draft) must be true before this row can reach parity — raise it rather than working around it.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.

## Outcome

_Filled at closeout._
