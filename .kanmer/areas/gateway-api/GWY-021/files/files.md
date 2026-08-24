# Files — GWY-021: DSK-04-04 · Bearer authentication for `/api/v1`: claims to actor, per-request enabled and stamp check

## Change surface

| Path | Purpose / risk |
| --- | --- |
| `docs/desktop/04-auth-session-update-and-startup/README.md` | Authoritative context; update only if the ticket's accepted scope explicitly calls for it. |
| `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` | Authoritative context; update only if the ticket's accepted scope explicitly calls for it. |
| `src/Pegasus.Core/Actors/StaffActorFactory.cs` | Core policy or contract; reuse the existing business owner rather than placing policy in a host. |
| `src/Pegasus.Core/Identity/StaffAuthorization.cs` | Core policy or contract; reuse the existing business owner rather than placing policy in a host. |
| `src/Pegasus.Web/Program.cs` | Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core. |
| `src/Pegasus.Web/Mcp/AutomationActorResolver.cs` | Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core. |
| `src/Pegasus.Web/Mcp/AutomationMcpErrors.cs` | Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core. |
| `src/Pegasus.Core/Identity/IdentityContracts.cs` | Core policy or contract; reuse the existing business owner rather than placing policy in a host. |
| `docs/desktop/03-gateway-api-and-data/README.md` | Authoritative context; update only if the ticket's accepted scope explicitly calls for it. |
| `docs/frd/frd-04-parties-accounts-and-access.md` | Authoritative context; update only if the ticket's accepted scope explicitly calls for it. |
| `src/Pegasus.Web/Desktop/` | Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core. |
| `src/Pegasus.Web/Desktop/DesktopActorResolver.cs` | Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core. |
| `src/Pegasus.Core/Identity/StaffPasswordChange.cs` | Core policy or contract; reuse the existing business owner rather than placing policy in a host. |
| `tests/Pegasus.IntegrationTests/DesktopApiAuthenticationTests.cs` | Focused verification surface; extend the stated success, failure and regression coverage. |
| `tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs` | Focused verification surface; extend the stated success, failure and regression coverage. |
| `tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj` | Focused verification surface; extend the stated success, failure and regression coverage. |
| `tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj` | Focused verification surface; extend the stated success, failure and regression coverage. |
| `src/Pegasus.Web/Pegasus.Web.csproj` | Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core. |

## Context files

- `docs/desktop/04-auth-session-update-and-startup/README.md` — Authoritative context; update only if the ticket's accepted scope explicitly calls for it.
- `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` — Authoritative context; update only if the ticket's accepted scope explicitly calls for it.
- `src/Pegasus.Core/Actors/StaffActorFactory.cs` — Core policy or contract; reuse the existing business owner rather than placing policy in a host.
- `src/Pegasus.Core/Identity/StaffAuthorization.cs` — Core policy or contract; reuse the existing business owner rather than placing policy in a host.
- `src/Pegasus.Web/Program.cs` — Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core.
- `src/Pegasus.Web/Mcp/AutomationActorResolver.cs` — Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core.
- `src/Pegasus.Web/Mcp/AutomationMcpErrors.cs` — Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core.
- `src/Pegasus.Core/Identity/IdentityContracts.cs` — Core policy or contract; reuse the existing business owner rather than placing policy in a host.
- `docs/desktop/03-gateway-api-and-data/README.md` — Authoritative context; update only if the ticket's accepted scope explicitly calls for it.
- `docs/frd/frd-04-parties-accounts-and-access.md` — Authoritative context; update only if the ticket's accepted scope explicitly calls for it.
- `src/Pegasus.Web/Desktop/` — Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core.
- `src/Pegasus.Web/Desktop/DesktopActorResolver.cs` — Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core.
- `src/Pegasus.Core/Identity/StaffPasswordChange.cs` — Core policy or contract; reuse the existing business owner rather than placing policy in a host.
- `tests/Pegasus.IntegrationTests/DesktopApiAuthenticationTests.cs` — Focused verification surface; extend the stated success, failure and regression coverage.

## Ripple and out-of-scope boundary

- **Azure**: no write.
- **Scope boundary**: may touch `src/Pegasus.Web/Desktop/`, the `/api/v1` composition from [[DSK-03-02]], `tests/Pegasus.IntegrationTests`, `tests/Pegasus.ArchitectureTests`. Must not change the cookie pipeline's behaviour, `StaffAuthorization`, `StaffActorFactory`, or anything under `src/Pegasus.Worker`.
- **Scope overlap**: [[DSK-03-03]] owns the per-group `StaffAccessRight` endpoint filter and [[DSK-04-04]] owns the scheme, the actor resolution and the account re-check. Agree the seam in the plan document before coding so the two tickets do not both write the filter.
- **Traps**: (a) do not cache the `IsEnabled`/stamp read — the plan accepts one indexed read per request at ten users and a cache silently reintroduces the "disabled account keeps working" defect; (b) the `MustChangePassword` block must exempt the password-change endpoint or the operator is locked out with no route forward; (c) the Razor path *redirects*, the API path must *return a problem* — copying the redirect is a defect.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.
