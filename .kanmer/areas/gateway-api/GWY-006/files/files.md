# Files — GWY-006: DSK-03-06 · Compatibility, dashboard and rail-count endpoints with parity tests against the Razor sources

## Change surface

| Path | Purpose / risk |
| --- | --- |
| `docs/desktop/03-gateway-api-and-data/README.md` | Authoritative context; update only if the ticket's accepted scope explicitly calls for it. |
| `docs/desktop/03-gateway-api-and-data/endpoint-map.md` | Authoritative context; update only if the ticket's accepted scope explicitly calls for it. |
| `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` | Authoritative context; update only if the ticket's accepted scope explicitly calls for it. |
| `src/Pegasus.Core/Operations/` | Core policy or contract; reuse the existing business owner rather than placing policy in a host. |
| `src/Pegasus.Web/Pages/Index.cshtml.cs` | Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core. |
| `src/Pegasus.Core/Operations/OperationsSnapshot.cs` | Core policy or contract; reuse the existing business owner rather than placing policy in a host. |
| `src/Pegasus.Core/Operations/DashboardCounts.cs` | Core policy or contract; reuse the existing business owner rather than placing policy in a host. |
| `src/Pegasus.Web/Presentation/RailCountsPageFilter.cs` | Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core. |
| `tests/Pegasus.IntegrationTests/DashboardCountersWebTests.cs` | Focused verification surface; extend the stated success, failure and regression coverage. |
| `tests/Pegasus.IntegrationTests/RailCountsWebTests.cs` | Focused verification surface; extend the stated success, failure and regression coverage. |
| `docs/desktop/06-ui-design/README.md` | Authoritative context; update only if the ticket's accepted scope explicitly calls for it. |
| `src/Pegasus.Contracts/Dashboard/DashboardResponse.cs` | Named by the ticket as an implementation or verification dependency. |
| `src/Pegasus.Web/Api/DashboardEndpoints.cs` | Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core. |
| `tests/Pegasus.IntegrationTests/DesktopGatewayDashboardTests.cs` | Focused verification surface; extend the stated success, failure and regression coverage. |
| `tests/Pegasus.Api.ContractTests` | Focused verification surface; extend the stated success, failure and regression coverage. |
| `openapi/pegasus-v1.json` | Versioned HTTP contract snapshot; change only with matching contract-test and client-generation evidence. |
| `eng/api/Export-OpenApiDocument.ps1` | Engineering tool or generation script; keep it deterministic and repository-owned. |
| `eng/api/Generate-ApiClient.ps1` | Engineering tool or generation script; keep it deterministic and repository-owned. |
| `tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj` | Focused verification surface; extend the stated success, failure and regression coverage. |

## Context files

- `docs/desktop/03-gateway-api-and-data/README.md` — Authoritative context; update only if the ticket's accepted scope explicitly calls for it.
- `docs/desktop/03-gateway-api-and-data/endpoint-map.md` — Authoritative context; update only if the ticket's accepted scope explicitly calls for it.
- `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` — Authoritative context; update only if the ticket's accepted scope explicitly calls for it.
- `src/Pegasus.Core/Operations/` — Core policy or contract; reuse the existing business owner rather than placing policy in a host.
- `src/Pegasus.Web/Pages/Index.cshtml.cs` — Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core.
- `src/Pegasus.Core/Operations/OperationsSnapshot.cs` — Core policy or contract; reuse the existing business owner rather than placing policy in a host.
- `src/Pegasus.Core/Operations/DashboardCounts.cs` — Core policy or contract; reuse the existing business owner rather than placing policy in a host.
- `src/Pegasus.Web/Presentation/RailCountsPageFilter.cs` — Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core.
- `tests/Pegasus.IntegrationTests/DashboardCountersWebTests.cs` — Focused verification surface; extend the stated success, failure and regression coverage.
- `tests/Pegasus.IntegrationTests/RailCountsWebTests.cs` — Focused verification surface; extend the stated success, failure and regression coverage.
- `docs/desktop/06-ui-design/README.md` — Authoritative context; update only if the ticket's accepted scope explicitly calls for it.
- `src/Pegasus.Contracts/Dashboard/DashboardResponse.cs` — Named by the ticket as an implementation or verification dependency.
- `src/Pegasus.Web/Api/DashboardEndpoints.cs` — Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core.
- `tests/Pegasus.IntegrationTests/DesktopGatewayDashboardTests.cs` — Focused verification surface; extend the stated success, failure and regression coverage.

## Ripple and out-of-scope boundary

- **Azure**: no write.
- **Scope boundary**: may touch `src/Pegasus.Web/Api/**`, `src/Pegasus.Contracts/Dashboard/**`, `openapi/`, the generated client output and the two test projects. Must not modify `src/Pegasus.Web/Pages/Index.cshtml.cs` or `Presentation/RailCountsPageFilter.cs` — the web app stays working unchanged through coexistence.
- **Traps**: two policy engines — the dashboard rule stays in `IGetOperationsSnapshot`; do not recompute it in the endpoint. The rail badge must never carry a shell-invented number (`RailCountsPageFilter.cs` remarks). Design authority: filters are dropdowns and tables sort newest first (`docs/design/README.md` § No explanatory copy and page economy). Upstream `main` is ahead of the fork; if the first upstream sync (`DSK-00-02`) has not landed, check `Pages/Index.cshtml.cs` for drift before projecting it.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.
