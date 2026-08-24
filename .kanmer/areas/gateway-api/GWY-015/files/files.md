# Files — GWY-015: DSK-03-15 · Administration endpoints: configuration, mailboxes, accounts, roles, automation, organizations, principals

## Change surface

| Path | Purpose / risk |
| --- | --- |
| `docs/desktop/03-gateway-api-and-data/README.md` | Authoritative context; update only if the ticket's accepted scope explicitly calls for it. |
| `docs/desktop/03-gateway-api-and-data/endpoint-map.md` | Authoritative context; update only if the ticket's accepted scope explicitly calls for it. |
| `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` | Authoritative context; update only if the ticket's accepted scope explicitly calls for it. |
| `src/Pegasus.Web/Pages/Administration/` | Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core. |
| `src/Pegasus.Core/Identity/StaffAuthorization.cs` | Core policy or contract; reuse the existing business owner rather than placing policy in a host. |
| `src/Pegasus.Core/Identity/StaffAccountAdministration.cs` | Core policy or contract; reuse the existing business owner rather than placing policy in a host. |
| `src/Pegasus.Core/Cases/OrganizationAdministration.cs` | Core policy or contract; reuse the existing business owner rather than placing policy in a host. |
| `src/Pegasus.Web/Program.cs` | Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core. |
| `tests/Pegasus.IntegrationTests/AdministrationPolicyPersistenceTests.cs` | Focused verification surface; extend the stated success, failure and regression coverage. |
| `docs/desktop/10-security-observability-performance/README.md` | Authoritative context; update only if the ticket's accepted scope explicitly calls for it. |
| `src/Pegasus.Contracts/Admin/` | Named by the ticket as an implementation or verification dependency. |
| `src/Pegasus.Web/Api/AdminEndpoints.cs` | Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core. |
| `src/Pegasus.Core/Identity/IdentityContracts.cs` | Core policy or contract; reuse the existing business owner rather than placing policy in a host. |
| `tests/Pegasus.IntegrationTests/DesktopGatewayAdminTests.cs` | Focused verification surface; extend the stated success, failure and regression coverage. |
| `tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj` | Focused verification surface; extend the stated success, failure and regression coverage. |

## Context files

- `docs/desktop/03-gateway-api-and-data/README.md` — Authoritative context; update only if the ticket's accepted scope explicitly calls for it.
- `docs/desktop/03-gateway-api-and-data/endpoint-map.md` — Authoritative context; update only if the ticket's accepted scope explicitly calls for it.
- `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` — Authoritative context; update only if the ticket's accepted scope explicitly calls for it.
- `src/Pegasus.Web/Pages/Administration/` — Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core.
- `src/Pegasus.Core/Identity/StaffAuthorization.cs` — Core policy or contract; reuse the existing business owner rather than placing policy in a host.
- `src/Pegasus.Core/Identity/StaffAccountAdministration.cs` — Core policy or contract; reuse the existing business owner rather than placing policy in a host.
- `src/Pegasus.Core/Cases/OrganizationAdministration.cs` — Core policy or contract; reuse the existing business owner rather than placing policy in a host.
- `src/Pegasus.Web/Program.cs` — Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core.
- `tests/Pegasus.IntegrationTests/AdministrationPolicyPersistenceTests.cs` — Focused verification surface; extend the stated success, failure and regression coverage.
- `docs/desktop/10-security-observability-performance/README.md` — Authoritative context; update only if the ticket's accepted scope explicitly calls for it.
- `src/Pegasus.Contracts/Admin/` — Named by the ticket as an implementation or verification dependency.
- `src/Pegasus.Web/Api/AdminEndpoints.cs` — Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core.
- `src/Pegasus.Core/Identity/IdentityContracts.cs` — Core policy or contract; reuse the existing business owner rather than placing policy in a host.
- `tests/Pegasus.IntegrationTests/DesktopGatewayAdminTests.cs` — Focused verification surface; extend the stated success, failure and regression coverage.

## Ripple and out-of-scope boundary

- **Azure**: no write.
- **Scope boundary**: may touch `src/Pegasus.Web/Api/**`, `src/Pegasus.Contracts/Admin/**`, `openapi/`, the generated client and the test projects. Must not touch `src/Pegasus.Core/Identity/**`, `src/Pegasus.Web/Pages/Administration/**`, or the OpenIddict client registry.
- **Traps**: two policy engines — administration rules live in the Core administration use cases; the endpoint filter only fails fast. Automation holds `PerformCasework` only (ADR-0011), so an Automation token must never reach an administration route — the audience rejection from [[DSK-03-03]] carries that. The observability blind spot is real: App Insights ingestion is capped at 0.1 GB/day (PLAT-034), so problem details with correlation ids are the compensating evidence for administration failures in production.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.
