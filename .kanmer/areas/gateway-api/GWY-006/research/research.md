# Research — GWY-006: DSK-03-06 · Compatibility, dashboard and rail-count endpoints with parity tests against the Razor sources

## Question

Add `GET /api/v1/dashboard` and `GET /api/v1/dashboard/rail-counts` projecting the same Core snapshot the Razor dashboard and rail filter use, and add the contract test proving the `GET /api/v1/client-compatibility` payload (implemented by [[DSK-04-06]]) matches the area 04 contract.

## Evidence examined

- Plan row: `docs/desktop/03-gateway-api-and-data/README.md` § 5 — `DSK-03-06`
- Plan detail: `docs/desktop/03-gateway-api-and-data/endpoint-map.md` § Dashboard and rail counts, and § Session, compatibility, diagnostics (compatibility row)
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 9.1 Two-layer enforcement, § 10.6 Query strategy, § 14.3 Dashboard
- Endpoint contracts quoted from `endpoint-map.md`:
  - `GET /dashboard` — replaces `Index.cshtml.cs` `OnGetAsync`; Core port `IDashboardQueries` (`src/Pegasus.Core/Operations/`); auth right `AccessStaffApplication`; GET, idempotent; concurrency token `ETag`; returns assigned/new/overdue/recent/integration-failure counts and lists; phase 3.
  - `GET /dashboard/rail-counts` — replaces `Presentation/RailCountsPageFilter.cs`; Core port `IDashboardQueries.GetCaseStageCountsAsync`; auth right `AccessStaffApplication`; GET; `ETag`; returns counts per rail entry (only figures already queried; absent = nothing); phase 3.
  - `GET /client-compatibility` — new (§ 9.1); Core: admin setting (area 04); auth **anonymous**; GET; no concurrency token; returns minimum/current version, channel, maintenance, TTL; phase 2.
- Repository evidence:
  - `src/Pegasus.Web/Pages/Index.cshtml.cs:13-42` — the dashboard page model calls `IGetOperationsSnapshot.ExecuteAsync(actor, ct)` and exposes `Counts`, `DueWork`, `CaseStages`, `CaseActivity`, `MailActivity`, `LoadedAtUtc`
  - `src/Pegasus.Core/Operations/OperationsSnapshot.cs:54` — `IGetOperationsSnapshot`, the single Core use case behind the dashboard
  - `src/Pegasus.Core/Operations/DashboardCounts.cs:55-67` — `IDashboardQueries` with `GetCaseStageCountsAsync`, `GetCaseActivityCountsAsync`, `GetMailActivityCountsAsync`
  - `src/Pegasus.Web/Presentation/RailCountsPageFilter.cs:40-47` — the rail figure is exactly `counts.NotReady + counts.Review + counts.Held`, and only the `Queues` key is populated
  - `tests/Pegasus.IntegrationTests/DashboardCountersWebTests.cs`, `tests/Pegasus.IntegrationTests/RailCountsWebTests.cs` — the existing web assertions to compare against
- Binding decisions:
  - L-01 — endpoints live in the existing `Pegasus.Web` process beside the Razor pages they mirror.
  - L-02 — parity is proven in the local stack; there is no Azure test environment.
- Depends on: `DSK-03-03` for the `StaffAccessRight` filter; `DSK-04-06` implements the compatibility endpoint and the minimum-version setting this ticket tests against.

## Scope and constraints

Proposal § 10.6 requires server-side query strategy and § 14.3 makes the dashboard the first native screen; the endpoint map gives the dashboard and rail counts Phase 3. Operator-visible consequence: the desktop's first screen shows the same figures the web dashboard shows for the same data — a divergence here would be the first thing an operator sees and the fastest way to lose trust in the conversion. The rail badge in particular must carry only figures that are already queried; the layout renders nothing for a missing key rather than a stale zero.

- Future owner: `pegasus-gateway-dev`; tests: `pegasus-test-engineer`; independent review: `pegasus-desktop-reviewer`.
- Use `dotnet-webapi`, `optimizing-ef-core-queries` where the ticket changes a query, and `run-tests` for the actual runner profile. The project decision overrides generic “service per endpoint” advice: route handlers translate to existing `Pegasus.Core` ports; no second policy/service layer is introduced.
- The shared EPIC context binds this to versioned `/api/v1` route groups in the existing `Pegasus.Web`, the existing rate-limiter mechanism, an OpenAPI snapshot, and no Azure write.

- Microsoft Learn (fetched 2026-08-24): [ASP.NET Core OpenAPI support](https://learn.microsoft.com/aspnet/core/fundamentals/openapi/overview?view=aspnetcore-10.0) confirms first-party OpenAPI generation. Use the repository’s planned committed snapshot and contract-test flow rather than adding a parallel API documentation path.

- **Azure**: no write.
- **Scope boundary**: may touch `src/Pegasus.Web/Api/**`, `src/Pegasus.Contracts/Dashboard/**`, `openapi/`, the generated client output and the two test projects. Must not modify `src/Pegasus.Web/Pages/Index.cshtml.cs` or `Presentation/RailCountsPageFilter.cs` — the web app stays working unchanged through coexistence.
- **Traps**: two policy engines — the dashboard rule stays in `IGetOperationsSnapshot`; do not recompute it in the endpoint. The rail badge must never carry a shell-invented number (`RailCountsPageFilter.cs` remarks). Design authority: filters are dropdowns and tables sort newest first (`docs/design/README.md` § No explanatory copy and page economy). Upstream `main` is ahead of the fork; if the first upstream sync (`DSK-00-02`) has not landed, check `Pages/Index.cshtml.cs` for drift before projecting it.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.

## Governing documents

- No canonical document is linked yet. Retain the ticket's existing `docs_todo` state; do not invent or link a proposed desktop ADR.

## Planning implication

Reuse the cited boundaries and revalidate the named sources against current `origin/dev` after the ticket is taken. Do not create a compatibility path, duplicate policy, or an unapproved external write.
