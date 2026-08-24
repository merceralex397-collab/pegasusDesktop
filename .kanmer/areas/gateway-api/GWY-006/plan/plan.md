# Plan — GWY-006: DSK-03-06 · Compatibility, dashboard and rail-count endpoints with parity tests against the Razor sources

## Governing documents

- No canonical document is linked yet. Retain the ticket's existing `docs_todo` state; do not invent or link a proposed desktop ADR.

## Chosen approach

Add `GET /api/v1/dashboard` and `GET /api/v1/dashboard/rail-counts` projecting the same Core snapshot the Razor dashboard and rail filter use, and add the contract test proving the `GET /api/v1/client-compatibility` payload (implemented by [[DSK-04-06]]) matches the area 04 contract.

## Routing and constraints

- Future owner: `pegasus-gateway-dev`; tests: `pegasus-test-engineer`; independent review: `pegasus-desktop-reviewer`.
- Use `dotnet-webapi`, `optimizing-ef-core-queries` where the ticket changes a query, and `run-tests` for the actual runner profile. The project decision overrides generic “service per endpoint” advice: route handlers translate to existing `Pegasus.Core` ports; no second policy/service layer is introduced.
- The shared EPIC context binds this to versioned `/api/v1` route groups in the existing `Pegasus.Web`, the existing rate-limiter mechanism, an OpenAPI snapshot, and no Azure write.

- Microsoft Learn (fetched 2026-08-24): [ASP.NET Core OpenAPI support](https://learn.microsoft.com/aspnet/core/fundamentals/openapi/overview?view=aspnetcore-10.0) confirms first-party OpenAPI generation. Use the repository’s planned committed snapshot and contract-test flow rather than adding a parallel API documentation path.


## Ordered implementation steps

1. Orient. Read the three endpoint rows quoted above in `docs/desktop/03-gateway-api-and-data/endpoint-map.md`, then `docs/desktop/06-ui-design/README.md` for the dashboard screen spec so the payload carries what the screen needs and nothing more. Then `get_doc_gates <this ticket id>` and `take_ticket`.
2. Read `src/Pegasus.Web/Pages/Index.cshtml.cs` and `src/Pegasus.Core/Operations/OperationsSnapshot.cs` in full. The endpoint must call `IGetOperationsSnapshot.ExecuteAsync(actor, cancellationToken)` — the same use case — not `IDashboardQueries` directly, so there is one implementation of the dashboard rule.
3. Add `src/Pegasus.Contracts/Dashboard/DashboardResponse.cs` and `RailCountsResponse.cs` mirroring the page model's exposed members (`Counts`, `DueWork`, `CaseStages`, `CaseActivity`, `MailActivity`, `LoadedAtUtc`). Do not expose Core records directly — they carry `ActionActor` and server-only members (§ 3 row *Contracts*).
4. Add `src/Pegasus.Web/Api/DashboardEndpoints.cs` mapping a `dashboard` sub-group on the root group from [[DSK-03-02]], with `.RequireStaffRight(StaffAccessRight.AccessStaffApplication)` from [[DSK-03-03]]. Map `GET /` and `GET /rail-counts`. Handlers map arguments and shape only; no business rule enters `Pegasus.Web`.
5. Populate rail counts from `IDashboardQueries.GetCaseStageCountsAsync` and compute the `Queues` figure as `NotReady + Review + Held`, exactly as `RailCountsPageFilter.cs:44-46` does. Emit only keys with a real figure — an absent key means "nothing to show", never zero (the filter's own documented rule).
6. Add a weak `ETag` (`W/"<hash-of-payload>"`) and honour `If-None-Match` with 304 on both endpoints, per `endpoint-map.md` § Conventions ("Reads return `version` and a weak `ETag`"). Use the `Microsoft.AspNetCore.Http` primitives; do not add a caching library.
7. Propagate `HttpContext.RequestAborted` into every Core call so a cancelled desktop request releases the database connection (§ 10.2 cancellation support).
8. Add `tests/Pegasus.IntegrationTests/DesktopGatewayDashboardTests.cs`: seed the same fixture `DashboardCountersWebTests.cs` uses, call both the Razor page and the API for the same data, and assert the figures are equal. Add a rail-count fact asserting equality with `RailCountsPageFilter` output and a fact that an absent figure is absent from the payload rather than zero.
9. Add `ETag`/`If-None-Match` facts: a first request returns 200 with an `ETag`; the same request with `If-None-Match` returns 304 with an empty body.
10. Add the compatibility contract test to `tests/Pegasus.Api.ContractTests`: assert `GET /api/v1/client-compatibility` is anonymous, returns the fields the endpoint-map row names (minimum version, current version, channel, maintenance flag, TTL), and that its schema appears in `openapi/pegasus-v1.json`. Do **not** re-implement the endpoint or the minimum-version setting — [[DSK-04-06]] owns both; if that ticket has not landed, record the blocker and land the dashboard half only.
11. Regenerate the OpenAPI snapshot with `pwsh ./eng/api/Export-OpenApiDocument.ps1` and commit the updated `openapi/pegasus-v1.json`; regenerate the client with `pwsh ./eng/api/Generate-ApiClient.ps1` and commit the result.
12. Run `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj -c Release --filter "FullyQualifiedName~DesktopGatewayDashboard"` and the contract tests. Then run the simplification pass and record it under a dated `## Simplification pass` heading in the ticket plan.

## Acceptance conditions

- [ ] `GET /api/v1/dashboard` returns the same figures the Razor dashboard shows for the same seeded data.
- [ ] `GET /api/v1/dashboard/rail-counts` equals `RailCountsPageFilter` output; figures with no established query are absent, not zero.
- [ ] Both endpoints require `AccessStaffApplication`, return a weak `ETag` and honour `If-None-Match` with 304.
- [ ] The compatibility payload matches the area 04 contract and appears in the OpenAPI snapshot.
- [ ] The OpenAPI snapshot and generated client are regenerated and committed in the same change.

## Verification

- [ ] `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj -c Release --filter "FullyQualifiedName~DesktopGatewayDashboard"` — expected: all facts pass, including the parity comparison against the Razor page.
- [ ] `pwsh ./eng/api/Export-OpenApiDocument.ps1 && git diff --exit-code openapi/` — expected: exit 0 after the snapshot is committed.

## Risks and boundaries

- **Azure**: no write.
- **Scope boundary**: may touch `src/Pegasus.Web/Api/**`, `src/Pegasus.Contracts/Dashboard/**`, `openapi/`, the generated client output and the two test projects. Must not modify `src/Pegasus.Web/Pages/Index.cshtml.cs` or `Presentation/RailCountsPageFilter.cs` — the web app stays working unchanged through coexistence.
- **Traps**: two policy engines — the dashboard rule stays in `IGetOperationsSnapshot`; do not recompute it in the endpoint. The rail badge must never carry a shell-invented number (`RailCountsPageFilter.cs` remarks). Design authority: filters are dropdowns and tables sort newest first (`docs/design/README.md` § No explanatory copy and page economy). Upstream `main` is ahead of the fork; if the first upstream sync (`DSK-00-02`) has not landed, check `Pages/Index.cshtml.cs` for drift before projecting it.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.
