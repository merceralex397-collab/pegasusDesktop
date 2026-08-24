# Files — FEAT-001

Surface area of `DSK-05-01 · S1 Dashboard and work queue`. Paths that do not
exist at `HEAD` `bbd1c549` are marked with the ticket that creates them; every
other path was confirmed with `ls` or `wc -l`.

## Where the change lands

| Path | Why |
| --- | --- |
| `src/Pegasus.Contracts/` *(created by [[FND-029]] (plan handle `DSK-02-04`))* | New `DashboardResponse` and `RailCountsResponse` DTOs plus their nested count records. Must reference nothing but the BCL — `tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs` is extended by [[FND-037]] (plan handle `DSK-02-12`) to fail on an ASP.NET, EF or WinUI type here. Rail-count members must be **nullable** so an absent count omits rather than zeroes (research finding 1). |
| `src/Pegasus.Web/` — the `/api/v1` route group only *(group created by [[GWY-002]] (plan handle `DSK-03-02`); the dashboard routes by [[GWY-006]] (plan handle `DSK-03-06`))* | `GET /api/v1/dashboard` and `GET /api/v1/dashboard/rail-counts`, both calling the **same** `IGetOperationsSnapshot` / `IDashboardQueries` the Razor page calls. Gaps in [[GWY-006]]'s shape are closed here. Risk: a second query implementation is a stop condition (`AGENTS.md` § Product invariants). |
| `src/Pegasus.Desktop/` *(created by [[FND-030]] (plan handle `DSK-02-05`))* | `DashboardViewModel` (Loading / Empty / Error / Loaded, coalesced `RefreshCommand`, `LastLoadedAt`, cancellation on navigate-away) and the Dashboard XAML page. Risk: `TreatWarningsAsErrors=true` plus WinUI `WUI*` analyzers — fix, do not blanket-suppress. |
| `src/Pegasus.Desktop.Infrastructure/` *(created by [[FND-031]] (plan handle `DSK-02-06`))* | The typed call into the Kiota client from [[GWY-005]] (plan handle `DSK-03-05`). The view model never touches `HttpClient` directly. |
| `src/Pegasus.Desktop/` shell view model *(shell from [[FND-033]] (plan handle `DSK-02-08`), rail from [[DUI-004]] (plan handle `DSK-06-04`))* | Binds the rail badge slots to `GET /api/v1/dashboard/rail-counts`. Risk: a count the gateway omits must render **nothing**, never `0`. |
| `tests/Pegasus.Desktop.ViewModelTests/` *(created by [[FND-038]] (plan handle `DSK-02-13`))* | State facts, refresh coalescing, cancellation, error→`InfoBar` mapping. |
| `tests/Pegasus.Api.ContractTests/` *(created by [[TEST-001]] (plan handle `DSK-08-01`))* | Gate-off 404, 401, 200-with-every-field, `If-None-Match` 304, for both routes. |
| `tests/Pegasus.Desktop.UITests/` *(created by [[TEST-006]] (plan handle `DSK-08-06`))* | `ui-tests.ps1 -Script dashboard`: launch, wait on AutomationIds, keyboard-only traversal, open one item. |
| `docs/desktop/01-inventory-and-parity/parity-matrix.md` | Row `PAR-05` (`:50`) moves `inventoried` → `implemented` → `automated verification passed`, with evidence pointers. |
| `docs/frd/frd-13-desktop-operator-experience.md` *(skeleton created by [[FND-008]] (plan handle `DSK-00-08`))* | New Dashboard section. |
| `docs/capabilities.md` | One `DSK` family row for the dashboard, canonical owner FRD-13. |

## Context files

Read these before writing code. Each is here for one specific trap.

| Path | What it tells the implementer |
| --- | --- |
| `src/Pegasus.Web/Pages/Index.cshtml.cs` (43 lines) | The entire parity surface, and its exact size. Six members at `:15`–`:25`; `OnGetAsync` at `:27-42`. It reads `snapshot.Intake/DueWork/CaseStages/CaseActivity/MailActivity/AsOfUtc` and **never** `snapshot.TriageCount`. Do not add a Triage tile because the query happens to compute one. |
| `src/Pegasus.Core/Operations/OperationsSnapshot.cs` (160 lines) | The record has **seven** members (`:45-52`), one more than the page shows. `GetOperationsSnapshot.ExecuteAsync` (`:88-124`) requires **`StaffAccessRight.PerformCasework`** at `:96` — not `AccessStaffApplication`, which is what `endpoint-map.md:43-44` says. Due work is capped at 20 (`:68`, `:107`). Day/week boundaries are Europe/London with a Monday week start and a documented UTC fallback (`:78`, `:137-159`). |
| `src/Pegasus.Core/Operations/DashboardCounts.cs` (67 lines) | The exact tile vocabulary: `CaseStageCounts(NotReady, Review, Held)` (`:18`), `CaseActivityCounts` five members (`:30-34`), `MailActivityCounts(ReceivedToday, NeedsSorting)` with an `Unidentified` alias property (`:45-51`) — put `unidentified` on the wire, never `needsSorting`. The interface comment at `:50-53` is the tile rule: a real number, or the tile is not rendered. No placeholder. |
| `src/Pegasus.Web/Presentation/RailCountsPageFilter.cs` (51 lines) | Why the rail exists as a side channel and why the desktop cannot copy it. `:43-46` writes exactly one key, `["Queues"] = NotReady + Review + Held` (`:45`). `:13-20` is the binding sentence: Inbox and Cases "have no established figure to reuse without inventing one, so they are left absent… the layout already renders nothing for a missing key, never a stale zero." |
| `src/Pegasus.Web/Program.cs:255-261` | The filter is registered **globally** on every Razor page, so the count runs once per authenticated request. That is the baseline the desktop's coalesced refresh must not be worse than. |
| `src/Pegasus.Core/Identity/StaffAuthorization.cs` (77 lines) | The twelve rights (`:8-20`) and the fail-closed matrix (`:33-56`). `AccessStaffApplication` is Staff-only; `PerformCasework` is Staff **or** Automation. Choosing the wrong one at the endpoint filter turns a 403 into an unhandled `StaffAuthorizationException`. |
| `src/Pegasus.Web/Mcp/AutomationMcpErrors.cs` (154 lines) | The existing Core-exception→transport-error map. `StaffAuthorizationException` is caught first (`:29-33`). [[GWY-002]] ports this to problem details; the desktop's `InfoBar` presentation must match whatever shape lands. |
| `src/Pegasus.Web/Presentation/OperatorLabels.cs` (685 lines) | The single code→operator-vocabulary map, and the source of `OfficeTime` (`:412`) / `OfficeDate` (`:426`), which resolve `Europe/London` at `:446`. Freshness time renders through this. It is being extracted to the shared assembly by [[FEAT-023]] (plan handle `DSK-05-23`) — coordinate rather than copying a second office-time helper. |
| `docs/design/README.md:396-445` | The merge rules: approved necessary-copy list (`:400-409`), banned words (`:412-420`, including `intake`, `projection`, `bytes`), the four hard rules (`:422-445`). `:417-420` says explicitly that CI does **not** enforce the ban — the reviewer is the only gate. |
| `docs/design/README.md:764-772` | The complete UI state contract. The four query states this screen owes (loading, current, stale-with-last-good-time, unavailable/failed) are named there, not invented here. |
| `docs/desktop/06-ui-design/screen-specs.md:31-39` | The AutomationId convention `<Screen>.<Region>.<Element>[.<Key>]`. This screen's ids are fixed at `:145-147`: `Dashboard.Tile.<Metric>`, `Dashboard.Refresh`, `Dashboard.Recent.Row.<Ref>`. Coverage must be 100%. |
| `docs/desktop/06-ui-design/screen-specs.md:129-147` | The tile list and the rule that each tile shows "its value or its unavailable state, its last-good time and current refresh state… `0` is a current result only", plus "Recent cases… renders only when there are entries" and "No charts." |
| `docs/frd/frd-12-operator-experience.md:93-113` | The FRD this ticket's `refs` names. `:95-99` is the freshness contract verbatim; `:107` fixes `New cases today` to the Europe/London calendar day and lists what it excludes. |
| `docs/desktop/03-gateway-api-and-data/endpoint-map.md:39-44` | The two dashboard rows, their `Replaces` column, and the `AccessStaffApplication` entry that contradicts Core (see above). |
| `tests/Pegasus.IntegrationTests/DashboardCountersWebTests.cs` (74 lines) | The existing counter oracle. Build the parity table from these fixtures so a disagreement is attributable to a figure rather than to a dataset. |
| `tests/Pegasus.IntegrationTests/RailCountsWebTests.cs` (121 lines) | The existing rail oracle, including the absent-key behaviour. This is the test that proves "renders nothing, never a zero" today. |
| `tests/Pegasus.Core.Tests/Operations/DashboardBoundaryTests.cs` (141 lines) | The Europe/London day and Monday-week boundary facts. If a desktop-side date calculation disagrees with these, the desktop is wrong. |
| `tests/Pegasus.IntegrationTests/IntakeWebTestSupport.cs:26` | The shared `WebApplicationFactory<Program>` the contract tests reuse. `Features:DesktopGateway` must be switched on explicitly there or every `/api/v1` route returns 404 and reads as a routing bug. |
| `docs/desktop/08-testing/test-uat-stack.md:22` | The exact Test/UAT configuration the parity run needs: `Runtime:Profile=DevelopmentOffline`, `Features:LocalIntake=true`, `Features:LocalDocumentCustody=true`, `Features:DesktopGateway=true`. |
| `docs/desktop/05-implementation-and-migration/reuse-map.md` § "Pegasus.Web — REPLACE pages, KEEP the host" | The row `Presentation/RailCountsPageFilter.cs (51)` → **REPLACE by a gateway endpoint**, target **S1**. This ticket is that row. |

## Ripple effects

- **Generated client.** [[GWY-005]] (plan handle `DSK-03-05`) commits Kiota
  output with a CI no-op check. Adding or changing a dashboard DTO regenerates
  it; the regenerated files are part of this ticket's diff and the no-op check
  fails if they are not committed.
- **OpenAPI snapshot.** [[TEST-001]] (plan handle `DSK-08-01`) makes the snapshot
  test fail on an undeclared change. Both new routes and their schemas land in
  the snapshot in the same commit as the endpoints.
- **Architecture tests.** `tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs`
  (520 lines) gains desktop rules in [[FND-037]] (plan handle `DSK-02-12`). A
  `Pegasus.Contracts` DTO that pulls in `Microsoft.AspNetCore.*` fails it, as
  does any `Pegasus.Infrastructure` reference from the desktop.
- **Solution and CI.** New projects are already in `Pegasus.slnx` by their
  creating tickets, but the `desktop-build` lane ([[FND-040]], plan handle
  `DSK-02-15`) runs the ViewModel and Architecture tests — a new failing fact
  here turns that lane red.
- **Existing web tests must stay green.** Nothing in this ticket touches
  `Pages/Index.cshtml.cs` or `RailCountsPageFilter.cs`, so
  `DashboardCountersWebTests.cs`, `RailCountsWebTests.cs` and
  `Browser/OperatorJourneyTests.cs` must be unchanged and passing after the
  change. A diff touching them is a scope breach.
- **Parity matrix and documentation.** `PAR-05` status plus evidence pointers;
  a new FRD-13 section; a `docs/capabilities.md` `DSK` row. `scripts/Test-DocumentationLinks.ps1`
  runs over the repository's documentation links, so a broken relative link in
  the new FRD section fails CI.
- **Endpoint map correction.** The `AccessStaffApplication` entry at
  `endpoint-map.md:43-44` is wrong against `OperationsSnapshot.cs:96`. Correcting
  it is [[GWY-006]]'s documentation change, raised from here.
- **Downstream tickets.** `FEAT-001` blocks `FEAT-002`, `FEAT-020`, `FEAT-022`,
  `FEAT-025` and `TEST-007`; the shell rail binding it lands is what
  [[FEAT-020]] (plan handle `DSK-05-20`) extends with integration health.

## Out of scope

Recorded so the reviewer sees each was a decision, not an oversight. These are
the ticket's own Guardrails made explicit.

- **`src/Pegasus.Web/Pages/Index.cshtml.cs` and `Presentation/RailCountsPageFilter.cs`
  are not modified.** The web dashboard stays live and unchanged until `PAR-05`
  reaches `cut over`; the cut is `Phase 10` work owned by [[FEAT-026]] (plan
  handle `DSK-05-26`).
- **`src/Pegasus.Infrastructure` and `src/Pegasus.Worker` are not touched.**
  `EfDashboardQueries` is reused behind the gateway exactly as registered at
  `src/Pegasus.Infrastructure/DependencyInjection.cs:244-245`.
- **No Triage tile.** `snapshot.TriageCount` exists and is unused by the web
  page; surfacing it would be new scope, not parity.
- **No vanity charts.** Screen spec `:141` — "No charts."
- **No Azure write of any kind.** Enabling `Features:DesktopGateway` on the
  production Container App is [[PLAT-024]] (plan handle `DSK-11-06`).
- **No operations retry or revoke commands.** The dashboard links to
  Operations; the commands themselves are [[FEAT-020]] (plan handle `DSK-05-20`).
- **No `ViewData`, `TempData`, PRG or antiforgery equivalent** anywhere in the
  desktop path — desktop state lives in the view model.
- **`OperatorLabels` is not extracted here.** That is [[FEAT-023]] (plan handle
  `DSK-05-23`); this ticket consumes whatever shared vocabulary exists when it
  runs and does not create a second office-time helper.
