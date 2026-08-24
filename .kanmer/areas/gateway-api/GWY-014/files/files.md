# Files — GWY-014: DSK-03-14 · Vehicle lookup and assessment endpoints: damage, estimate import, specification, report draft, send

## Change surface

| Path | Purpose / risk |
| --- | --- |
| `docs/desktop/03-gateway-api-and-data/README.md` | Authoritative context; update only if the ticket's accepted scope explicitly calls for it. |
| `docs/desktop/03-gateway-api-and-data/endpoint-map.md` | Authoritative context; update only if the ticket's accepted scope explicitly calls for it. |
| `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` | Authoritative context; update only if the ticket's accepted scope explicitly calls for it. |
| `src/Pegasus.Core/Vehicle/` | Core policy or contract; reuse the existing business owner rather than placing policy in a host. |
| `src/Pegasus.Core/Assessment/` | Core policy or contract; reuse the existing business owner rather than placing policy in a host. |
| `src/Pegasus.Web/Pages/Cases/Vehicle.cshtml.cs` | Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core. |
| `src/Pegasus.Web/Pages/Cases/Assessment/Index.cshtml.cs` | Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core. |
| `tests/Pegasus.IntegrationTests/AssessmentDamageAndCopyWebTests.cs` | Focused verification surface; extend the stated success, failure and regression coverage. |
| `docs/desktop/07-integrations/README.md` | Authoritative context; update only if the ticket's accepted scope explicitly calls for it. |
| `src/Pegasus.Contracts/Assessment/` | Named by the ticket as an implementation or verification dependency. |
| `src/Pegasus.Contracts/Vehicle/` | Named by the ticket as an implementation or verification dependency. |
| `src/Pegasus.Web/Api/VehicleEndpoints.cs` | Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core. |
| `src/Pegasus.Web/Api/AssessmentEndpoints.cs` | Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core. |
| `tests/Pegasus.IntegrationTests/DesktopGatewayAssessmentTests.cs` | Focused verification surface; extend the stated success, failure and regression coverage. |
| `tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj` | Focused verification surface; extend the stated success, failure and regression coverage. |

## Context files

- `docs/desktop/03-gateway-api-and-data/README.md` — Authoritative context; update only if the ticket's accepted scope explicitly calls for it.
- `docs/desktop/03-gateway-api-and-data/endpoint-map.md` — Authoritative context; update only if the ticket's accepted scope explicitly calls for it.
- `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` — Authoritative context; update only if the ticket's accepted scope explicitly calls for it.
- `src/Pegasus.Core/Vehicle/` — Core policy or contract; reuse the existing business owner rather than placing policy in a host.
- `src/Pegasus.Core/Assessment/` — Core policy or contract; reuse the existing business owner rather than placing policy in a host.
- `src/Pegasus.Web/Pages/Cases/Vehicle.cshtml.cs` — Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core.
- `src/Pegasus.Web/Pages/Cases/Assessment/Index.cshtml.cs` — Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core.
- `tests/Pegasus.IntegrationTests/AssessmentDamageAndCopyWebTests.cs` — Focused verification surface; extend the stated success, failure and regression coverage.
- `docs/desktop/07-integrations/README.md` — Authoritative context; update only if the ticket's accepted scope explicitly calls for it.
- `src/Pegasus.Contracts/Assessment/` — Named by the ticket as an implementation or verification dependency.
- `src/Pegasus.Contracts/Vehicle/` — Named by the ticket as an implementation or verification dependency.
- `src/Pegasus.Web/Api/VehicleEndpoints.cs` — Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core.
- `src/Pegasus.Web/Api/AssessmentEndpoints.cs` — Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core.
- `tests/Pegasus.IntegrationTests/DesktopGatewayAssessmentTests.cs` — Focused verification surface; extend the stated success, failure and regression coverage.

## Ripple and out-of-scope boundary

- **Azure**: no write. DVLA/DVSA and the renderer are reached through the existing adapters; replay adapters stand in locally (L-02).
- **Scope boundary**: may touch `src/Pegasus.Web/Api/**`, `src/Pegasus.Contracts/{Assessment,Vehicle}/**`, `openapi/`, the generated client and the test projects. Must not touch `src/Pegasus.Core/Assessment/**`, `src/Pegasus.Core/Vehicle/**`, the renderer in `src/Pegasus.Infrastructure`, or the Razor assessment page.
- **Traps**: L-03 says the gateway renderer is retained **until golden-file parity passes** — removing or bypassing it here is out of bounds. Do not retry commands automatically; only idempotent `GET`s. `Pegasus.Web` still publishes `linux-x64` for the Playwright renderer base image, so no Windows-only package may enter it. **Phase span**: `README.md` § 5 sequencing lists this row as "11, 14 (Phase 6–7)", and `endpoint-map.md` gives the two vehicle rows Phase 6 and the assessment rows Phase 7; the horizon is set to the earliest phase that needs any of it. If the reviewer prefers endpoints to land with their callers, split the assessment rows into a Phase 7 follow-up rather than delaying the Phase 6 vehicle slice.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.
