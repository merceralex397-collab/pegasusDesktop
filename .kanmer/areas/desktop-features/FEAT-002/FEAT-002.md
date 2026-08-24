---
id: FEAT-002
type: ticket
title: DSK-05-02 · S2 Case list and search
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
  - tier-10
  - needs-operator
groups:
  - EPIC-006
  - HZN-004
links: []
blocks:
  - FEAT-003
  - FEAT-022
  - FEAT-025
  - TEST-007
refs:
  - docs/frd/frd-01-case-identity-and-lifecycle.md
  - docs/frd/frd-12-operator-experience.md
docs_todo: true
archived: false
created: '2026-08-24T07:46:33.800Z'
updated: '2026-08-24T08:51:08.250Z'
---

## What

Deliver the native case list and global search: a virtualized, server-paged list over `GET /api/v1/cases` with sortable columns, dropdown filters, a persisted column layout, `Ctrl+K` global search and keyboard open, replacing `Pages/Cases/Index.cshtml.cs` and the `Pages/Search/Index.cshtml.cs` redirect for desktop users.

## Why

Proposal §13.3, §14.4 and §14.7 require finding and opening a case to be fast and keyboard-driven, with the list virtualized and paged by the server rather than reloaded whole. Today `src/Pegasus.Web/Pages/Cases/Index.cshtml.cs` (261 lines, one `OnGetAsync` over `ICaseQueryStore`) does a full page reload per filter change, and `src/Pegasus.Web/Pages/Search/Index.cshtml.cs` (29 lines) merely redirects into Cases. The Phase 3 exit gate requires paging, filtering and the performance budget to pass on the baseline workstation. Siblings: [[DSK-05-01]] provides the shell context, [[DSK-03-07]] the read endpoints, [[DSK-05-03]] opens what this list selects.

## Source of truth

- Plan row: `docs/desktop/05-implementation-and-migration/README.md` § 5 — `DSK-05-02`
- Plan detail: `docs/desktop/05-implementation-and-migration/vertical-slices.md` § `S2 · Case list and search (DSK-05-02)` and § `Common to every slice`
- Endpoint map: `docs/desktop/03-gateway-api-and-data/endpoint-map.md` § `Cases` (`GET /cases?page&pageSize&sort&stage&assignee&principal&q`)
- Screen spec: `docs/desktop/06-ui-design/screen-specs.md` § `§13.3 Case lifecycle` → `Cases list and search`
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 13.3 Case lifecycle, § 14.4 Work queue and case list, § 14.7 Search, § 15.1 performance budgets
- Repository evidence: `src/Pegasus.Web/Pages/Cases/Index.cshtml.cs` (261 lines, `OnGetAsync`), `src/Pegasus.Web/Pages/Search/Index.cshtml.cs` (29 lines, redirect), `src/Pegasus.Core/Cases/` (`ICaseQueryStore` search/list), `tests/Pegasus.IntegrationTests/Browser/UploadCaseSearchBrowserTests.cs`
- Binding decisions: L-01 gateway evolves inside `Pegasus.Web`; L-02 the performance budget is measured on the local Test/UAT workstation, never an Azure test environment; L-04 routing is named on the ticket
- Depends on: `DSK-05-01` the shell, session and first proven gateway call; `DSK-03-07` the paged cases list/search endpoint with sort and filter contracts; `DSK-06-07` the data-table pattern (32 px rows, sort toggles, filter dropdowns, column chooser, virtualization)

## Routing

- **Subagent**: `winui-dev` — `.codex/agents/winui-dev.toml`; `pegasus-gateway-dev` — `.codex/agents/pegasus-gateway-dev.toml` (query path and paging); `pegasus-test-engineer` — `.codex/agents/pegasus-test-engineer.toml`
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `winui-dev-workflow` (`.codex/skills/winui-dev-workflow/SKILL.md`) → `winui-design` (`.codex/skills/winui-design/SKILL.md`) → `dotnet-webapi` (dotnet/skills `98f84851`, `plugins/dotnet-aspnetcore/skills/dotnet-webapi/SKILL.md`) → `optimizing-ef-core-queries` (dotnet/skills `98f84851`, `plugins/dotnet-data/skills/optimizing-ef-core-queries/SKILL.md`) → `run-tests` → `winui-code-review` at review
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search`, `microsoft_code_sample_search`)
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (call `get_doc_gates <id>` before every move; a move crosses at most one gated boundary)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Orient: read the plan row and the sections under Source of truth, plus `docs/design/README.md` § `No explanatory copy and page economy` ("filters are dropdowns; tables sort newest first"). Call `get_doc_gates <this ticket id>`, then `take_ticket` with branch `task/dsk-05-02-case-list` and worktree `../pegasus-worktrees/dsk-05-02-case-list` from `origin/dev`.
2. Load `pegasus-desktop`, `winui-dev-workflow` and `winui-design`. Read `src/Pegasus.Web/Pages/Cases/Index.cshtml.cs` in full and record in `research` the exact filter parameters, the default sort, the page size and the `ICaseQueryStore` method it calls, together with the commit SHA read.
3. Confirm the `GET /api/v1/cases` contract from [[DSK-03-07]]: page, pageSize, sort, and the `stage` / `assignee` / `principal` / `q` filters, newest first by default, weak `ETag`, and a paging envelope carrying total count and continuation. Where the contract is short of what the Razor page supports, extend the endpoint against the same `ICaseQueryStore` call — one implementation only.
4. Load `optimizing-ef-core-queries` and review the gateway query path for N+1 and unbounded projections; the list must project only the columns the table renders. Record the review outcome in the plan.
5. Implement `CaseListViewModel` in `src/Pegasus.Desktop` with incremental server paging, a `SortDescriptor`, filter selections bound to `ComboBox` sources, and local sorting of **only the loaded page** — never a client-side sort that implies the whole set is present.
6. Build the list XAML on the data-table pattern from [[DSK-06-07]]: 32 px rows, header cells that are sort controls exposing an accessible sort state, filters as `ComboBox`es (not pill rows), a column chooser whose layout is persisted locally per user, `ListView` virtualization, and Enter or double-click to open the selected case. Every control carries an `AutomationId`.
7. Implement global search: `Ctrl+K` focuses the title-area search slot from [[DSK-06-04]]; results are grouped by case / party / vehicle / document metadata as the gateway supports and are keyboard traversable. Search queries the gateway — it never downloads the dataset. Recent items are a local convenience only and are not presented as search authority.
8. Add clear loading, empty and error states per proposal §14.4; the error state uses the `InfoBar` problem presentation from [[DSK-06-10]] with a copyable Reference, not a modal.
9. Write view-model tests in `tests/Pegasus.Desktop.ViewModelTests` for paging (first page, next page, end of set), sort toggling, filter change resetting to page 1, cancellation of an in-flight page when the filter changes, and the empty and error states.
10. Add contract tests in `tests/Pegasus.Api.ContractTests` for paging, sort and each filter, plus 401 without a token, 403 without `PerformCasework`, and `If-None-Match` → 304. Enable `Features:DesktopGateway` explicitly in the test factory.
11. **Operator step** — measure the performance budget on the baseline Test/UAT workstation from `docs/desktop/08-testing/test-uat-stack.md`: time to first page of ordinary results must be ≤ 1 s excluding provider outage. Record cold and warm figures and the hardware description in the ticket proof; the operator or the `pegasus-ui-verifier` run on the real workstation is the only acceptable source.
12. Run a parity comparison: the same filters applied on the web Cases page and on the desktop list must return identical result sets and ordering for the same database. Record the comparison in the proof.
13. Update `docs/desktop/01-inventory-and-parity/parity-matrix.md` rows `PAR-06` and `PAR-07`, add the list/search section to `docs/frd/frd-13-desktop-operator-experience.md`, run the simplification pass over the branch diff and record it under a dated `## Simplification pass` heading, then open the PR into `dev`.

## Acceptance criteria

- [ ] The list is server-paged, sortable, filtered by dropdowns and defaults to newest first.
- [ ] `Ctrl+K` focuses search; results are grouped and keyboard traversable; no full-dataset download.
- [ ] Column chooser layout persists locally per user; Enter and double-click open the selected case.
- [ ] First page of ordinary results ≤ 1 s on the baseline workstation (provider outage excluded).
- [ ] Result sets equal the web page for the same filters.
- [ ] Loading, empty and error states are explicit; no full-page spinner and no modal for a routine error.

## Verification

- [ ] `dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release --no-build` — expected: paging, sort, filter, cancellation and state facts pass.
- [ ] `dotnet test ./tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj --configuration Release --no-build` — expected: paging/sort/filter/authorization/304 facts pass.
- [ ] `pwsh ./tests/Pegasus.Desktop.UITests/ui-tests.ps1 -Script case-list` — expected: keyboard traversal and open-by-Enter pass without sleeps.
- [ ] Performance record in the ticket proof — expected: documented first-page latency ≤ 1 s with the workstation specification stated.
- [ ] Parity table in the ticket proof — expected: identical result sets and ordering versus the web page.

## Evidence tier

Tier 5 — Web/API/MCP caller. Tier 7 — Browser/accessibility. Tier 10 — Performance/concurrency.
Tier 5 obliges route-level evidence for paging, filtering, validation and authorization on the real endpoint; tier 7 obliges keyboard, focus and semantic-label evidence from a real run; tier 10 obliges a measured budget against the stated concurrency and volume assumptions rather than an invented threshold.

## Documentation changes

- `docs/desktop/01-inventory-and-parity/parity-matrix.md` — rows `PAR-06` (search) and `PAR-07` (case list)
- `docs/frd/frd-13-desktop-operator-experience.md` — list and search section
- `docs/capabilities.md` — `DSK` row for case list and search

## Guardrails

- **Azure**: no write.
- **Scope boundary**: may touch `src/Pegasus.Desktop`, `src/Pegasus.Desktop.Infrastructure`, `src/Pegasus.Contracts`, the `/api/v1` cases read group in `src/Pegasus.Web` and the test projects. Must not modify `Pages/Cases/Index.cshtml.cs` or `Pages/Search/Index.cshtml.cs` — they stay live until their parity rows reach `cut over`.
- **Traps**: filters must be dropdowns and tables newest-first (`docs/design/README.md`, a merge rule); local sorting applies to the loaded page only; the gate `Features:DesktopGateway` must be enabled in tests or the endpoint 404s; parity drift — record the SHA of the page model characterized; no colour-only status, every badge carries text.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.

## Outcome

_Filled at closeout._
