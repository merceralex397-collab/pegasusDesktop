# Files — FEAT-002

Surface area of `DSK-05-02 · S2 Case list and search`. Paths that do not exist
at `HEAD` `bbd1c549` are marked with the ticket that creates them; every other
path was confirmed with `ls` or `wc -l`.

## Where the change lands

| Path | Why |
| --- | --- |
| `src/Pegasus.Contracts/` *(created by [[FND-029]] (plan handle `DSK-02-04`))* | `CaseListItem`, the paged envelope, the `sort` value set and the filter request. The row must mirror `CaseSearchItem` (`src/Pegasus.Core/Cases/CaseQueries.cs:52-67`) without leaking `CaseLifecycleState` or `CaseType` as enum names on the wire (`DSK-03-01` convention). Risk: adding a `total` member commits [[GWY-007]] to a second `COUNT(*)` per page — see the plan's risks. |
| `src/Pegasus.Web/` — the `/api/v1` cases **read** group only *(group by [[GWY-002]] (plan handle `DSK-03-02`); routes by [[GWY-007]] (plan handle `DSK-03-07`))* | `GET /api/v1/cases` with page, pageSize, sort and the twelve UI-07 filters, calling the **same** `ISearchCases` the Razor page calls. `ArgumentException` / `ArgumentOutOfRangeException` from `SearchCases.ExecuteAsync` must map to a 400 problem, not a 500. Risk: a second query implementation is a stop condition (`AGENTS.md` § Product invariants). |
| `src/Pegasus.Desktop/` *(created by [[FND-030]] (plan handle `DSK-02-05`))* | `CaseListViewModel` (incremental server paging, `SortDescriptor`, filter selections, cancellation on filter change) and the list XAML. Risk: `TreatWarningsAsErrors=true` plus WinUI `WUI*` analyzers — fix, do not blanket-suppress. |
| `src/Pegasus.Desktop/` shell search slot *(title-bar slot from [[DUI-004]] (plan handle `DSK-06-04`))* | `Ctrl+K` focuses the search box; results grouped and keyboard traversable. |
| `src/Pegasus.Desktop.Infrastructure/` *(created by [[FND-031]] (plan handle `DSK-02-06`))* | The typed call into the Kiota client from [[GWY-005]] (plan handle `DSK-03-05`), plus the per-user persisted column layout through the local settings/cache abstraction — layout only, never result data. |
| `tests/Pegasus.Desktop.ViewModelTests/` *(created by [[FND-038]] (plan handle `DSK-02-13`))* | Paging, sort toggling, filter-change-resets-to-page-1, cancellation of an in-flight page, empty and error states. |
| `tests/Pegasus.Api.ContractTests/` *(created by [[TEST-001]] (plan handle `DSK-08-01`))* | Paging, sort, each filter, 401, 403 without `PerformCasework`, `If-None-Match` → 304, and 400 problems for the six Core input bounds. |
| `tests/Pegasus.Desktop.UITests/` *(created by [[TEST-006]] (plan handle `DSK-08-06`))* | `ui-tests.ps1 -Script case-list`: keyboard traversal of results and open-by-Enter. |
| `docs/desktop/01-inventory-and-parity/parity-matrix.md` | Rows `PAR-06` (`:51`, search) and `PAR-07` (`:52`, case list). |
| `docs/frd/frd-13-desktop-operator-experience.md` *(skeleton by [[FND-008]] (plan handle `DSK-00-08`))* | List and search section. |
| `docs/capabilities.md` | One `DSK` row for case list and search. |

## Context files

Read these before writing code. Each is here for one specific trap.

| Path | What it tells the implementer |
| --- | --- |
| `src/Pegasus.Web/Pages/Cases/Index.cshtml.cs` (261 lines) | The whole parity surface. `ResultsPerPage = 25` is a **private constant** at `:19`, not a parameter. Thirteen bound filters at `:21-60` with their exact query-string names. `OnGetAsync` at `:71`; unrecognised `kind` → `NotFound()` (`:79-82`); the Core call at `:101-119` passes **no sort**; `ArgumentException` → model-state error (`:124`); anything else → `QueryFailed` and **HTTP 503** (`:129-130`). |
| `src/Pegasus.Core/Cases/CaseQueries.cs` (357 lines) | The contract this list must not diverge from. `CaseSearchFilters` (`:12-24`), `CaseSearchOrder` **ten members** (`:31-43`) — the sort the web never uses. `CaseSearchItem` fifteen members (`:52-67`). `SearchCasesResult` (`:69-74`) carries **no total count**. `SearchCases.ExecuteAsync` (`:175-227`) requires `PerformCasework` at `:184` and enforces page 1…10 000 (`:186`), pageSize 1…100 (`:190`), non-empty `EngineerId` (`:194`), defined `State` (`:198`), defined `Order` (`:202`), `FromDate <= ToDate` (`:206`). Normalization caps at `:212-226`: reference 100, claimant 300, claim number 100, principal 20 **upper-cased**, origin 100, query 300; registration 20 then compacted (`:246-259`). |
| `src/Pegasus.Web/Pages/Search/Index.cshtml.cs` (29 lines) | Why there is no second search screen, and the failure-state lesson: its remarks (`:10-19`) say Cases returned 503 on a query failure and Search returned nothing at all. The desktop must render "unavailable", not an empty list. `OnGet` is a permanent redirect carrying `query` through (`:27-28`). |
| `src/Pegasus.Core/Identity/StaffAuthorization.cs` (77 lines) | Twelve rights (`:8-20`); the fail-closed matrix (`:33-56`). `PerformCasework` admits `ActorKind.Staff` **or** `ActorKind.Automation` (`:39-41`) — so a 403 fact must use an actor that genuinely lacks the right, not merely a non-staff one. |
| `src/Pegasus.Web/Mcp/AutomationMcpErrors.cs` (154 lines) | The precedent for mapping Core refusals to a transport: `ArgumentException`, `InvalidOperationException` and `InvalidDataException` pass their message through as a caller error (`:53-59`). [[GWY-002]] ports this to problem details; the six input bounds above must land as 400, not 500. |
| `docs/design/README.md:741-757` | The authority's UI-07 field list — twelve fields, including **Image Intake Reference**. That settles which filters the pane offers and stops the list drifting toward the endpoint map's shorter six. |
| `docs/design/README.md:441-445` | "Filters are dropdowns; tables sort newest first… column headers are sort links that toggle direction." A merge rule, not a preference. Pill-tab filters do not merge. |
| `docs/design/README.md:412-420` | Banned operator words — `intake` among them, which matters here because the `kind=images` filter's operator label must be "Images", never "Image intake". `:417-420` says CI does **not** enforce the ban; the reviewer is the only gate. |
| `docs/design/README.md:764-772` | The complete UI state contract. Query states include "unavailable" and "failed/retry" as distinct outcomes — the 503 distinction above. |
| `docs/desktop/06-ui-design/screen-specs.md:163-177` | The screen: search box with `Ctrl+K`, the filter dropdown list, the seven table columns, "Column chooser persisted locally", "server-paged with accessible current-page context and keyboard-operable next/previous", `Ctrl+N` for New case, and the AutomationIds `Cases.Search`, `Cases.List.Table`, `Cases.List.Filter.<Name>`, `Cases.List.Row.<Ref>`, `Cases.New`. |
| `docs/desktop/06-ui-design/screen-specs.md:31-39` | The AutomationId convention, including the row-level rule "Row-level elements append the record key: `Cases.List.Row.576059`". Coverage must be 100%. |
| `docs/desktop/03-gateway-api-and-data/endpoint-map.md:49` | The `/cases` row: `GET /cases?page&pageSize&sort&stage&assignee&principal&q`, auth right `PerformCasework`, ETag, "paged list, newest first". Note it names six filters where the page has thirteen and the authority twelve. |
| `docs/desktop/01-inventory-and-parity/parity-matrix.md:51-52` | `PAR-06` and `PAR-07` as they stand — including `PAR-06`'s indicative `~GET /api/v1/search`, which the endpoint map does not have. One of the two is wrong; step 3 settles which. |
| `tests/Pegasus.IntegrationTests/CasesIndexWebTests.cs` (155 lines) | The existing route-level oracle for this page. Build the parity table from its fixtures so a disagreement is attributable to a filter rather than to a dataset. |
| `tests/Pegasus.Core.Tests/Cases/CaseSearchTests.cs` | The Core-level oracle for normalization and the six input bounds. If a gateway 400 disagrees with these, the gateway is wrong. |
| `tests/Pegasus.IntegrationTests/IntakeWebTestSupport.cs:26` | The shared `WebApplicationFactory<Program>` the contract tests reuse. `Features:DesktopGateway` must be enabled explicitly there, or `/api/v1/cases` returns 404 and reads as a routing bug. |
| `docs/desktop/08-testing/test-uat-stack.md:22` | The exact Test/UAT configuration for the perf and parity runs: `Runtime:Profile=DevelopmentOffline`, `Features:LocalIntake=true`, `Features:LocalDocumentCustody=true`, `Features:DesktopGateway=true`. |
| `docs/desktop/05-implementation-and-migration/reuse-map.md` § "Pegasus.Core — REUSE as-is" | The `Cases/` row names `ICaseQueryStore` as an S2 port, and the **boundary note** permits `Pegasus.Desktop` to reference `Pegasus.Core` for deterministic local validation only — never `Pegasus.Infrastructure`, EF Core or an Azure SDK. |
| `src/Pegasus.Web/Presentation/OperatorLabels.cs:101-137` | `CaseStage(CaseLifecycleState)` and `CaseTypeName(CaseType)` — the settled operator words for the Stage and Type columns. Do not render an enum name. Extraction to the shared assembly is [[FEAT-023]] (plan handle `DSK-05-23`); coordinate rather than writing a second map. |

## Ripple effects

- **Generated client.** [[GWY-005]] (plan handle `DSK-03-05`) commits Kiota
  output with a CI no-op check; a new or changed cases DTO regenerates it and
  the regenerated files belong in this diff.
- **OpenAPI snapshot.** [[TEST-001]] (plan handle `DSK-08-01`) fails the
  snapshot test on an undeclared change; the `/api/v1/cases` schema lands in the
  snapshot in the same commit as the route.
- **Architecture tests.** `tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs`
  (520 lines), extended by [[FND-037]] (plan handle `DSK-02-12`), fails on an
  ASP.NET/EF/WinUI type inside `Pegasus.Contracts` and on any
  `Pegasus.Infrastructure` reference from the desktop.
- **Existing web tests must stay green.** Nothing here touches
  `Pages/Cases/Index.cshtml.cs` or `Pages/Search/Index.cshtml.cs`, so
  `CasesIndexWebTests.cs`, `CaseSearchTests.cs` and
  `Browser/UploadCaseSearchBrowserTests.cs` must pass unchanged. A diff touching
  them is a scope breach.
- **Downstream tickets.** `FEAT-002` blocks `FEAT-003`, `FEAT-022`, `FEAT-025`
  and `TEST-007`. [[FEAT-003]] (plan handle `DSK-05-03`) opens what this list
  selects, so the row's `CaseId` and the open gesture (Enter / double-click) are
  its entry contract.
- **Endpoint map and parity matrix may need correcting.** If step 3 finds
  [[GWY-007]] serving global search through `/cases?q=` rather than a `/search`
  route, `parity-matrix.md:51`'s `~GET /api/v1/search` is wrong and the
  correction is raised on [[GWY-007]].
- **Documentation link check.** `scripts/Test-DocumentationLinks.ps1` runs over
  repository documentation, so a broken relative link in the new FRD-13 section
  fails CI.
- **Performance record.** The ≤ 1 s figure and the workstation specification
  become part of the area 10 performance baseline that [[FEAT-022]] (plan handle
  `DSK-05-22`) sweeps at Phase 8.

## Out of scope

Recorded so the reviewer sees each was a decision.

- **`Pages/Cases/Index.cshtml.cs` and `Pages/Search/Index.cshtml.cs` are not
  modified.** Both stay live until their parity rows reach `cut over`; the cut is
  [[FEAT-026]] (plan handle `DSK-05-26`).
- **No client-side filtering or sorting of the whole set.** Local sort applies to
  the loaded page only; a header click issues a server sort and resets to page 1.
- **No total-count member** unless [[GWY-007]] already provides one. The screen
  spec asks for "accessible current-page context", not "page n of m".
- **No `Due by` computed on the desktop.** `CaseSearchItem` does not carry a due
  date; deriving one client-side would be a second business implementation and a
  stop condition. Either [[GWY-007]] projects it or the column is omitted with a
  recorded reason.
- **No vehicle-images workspace.** The `kind=images` **filter** and its outcome
  label are in scope; the Unidentified and Vehicle-images list/detail screens are
  [[FEAT-012]] (plan handle `DSK-05-12`).
- **No New-case flow.** `Ctrl+N` may exist as the primary command slot, but
  create itself is [[FEAT-004]] (plan handle `DSK-05-04`).
- **No bulk actions or multi-select.** `screen-specs.md:174` — "Multi-select only
  if a bulk action is approved (none in scope)."
- **No `ViewData`, `TempData`, PRG or antiforgery equivalent**; no full-page
  reload per filter change — that is the mechanic being removed.
- **No Azure write.** Enabling `Features:DesktopGateway` in production is
  [[PLAT-024]] (plan handle `DSK-11-06`).
