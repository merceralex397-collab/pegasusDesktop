# Files — GWY-024: DSK-04-14 · Security tests for the token path: expiry, rotation, revocation, role bypass, version spoofing

## Change surface

| Path | Purpose / risk |
| --- | --- |
| `docs/desktop/04-auth-session-update-and-startup/README.md` | Authoritative context; update only if the ticket's accepted scope explicitly calls for it. |
| `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` | Authoritative context; update only if the ticket's accepted scope explicitly calls for it. |
| `tests/Pegasus.IntegrationTests/StaffSignInSecurityTests.cs` | Focused verification surface; extend the stated success, failure and regression coverage. |
| `tests/Pegasus.IntegrationTests/IntakePersistenceIntegrationTests.cs` | Focused verification surface; extend the stated success, failure and regression coverage. |
| `tests/Pegasus.IntegrationTests/ReadinessEndpointTests.cs` | Focused verification surface; extend the stated success, failure and regression coverage. |
| `src/Pegasus.Core/Identity/StaffAuthorization.cs` | Core policy or contract; reuse the existing business owner rather than placing policy in a host. |
| `src/Pegasus.Core/Identity/IdentityContracts.cs` | Core policy or contract; reuse the existing business owner rather than placing policy in a host. |
| `src/Pegasus.Core/Actors/StaffSessionPolicy.cs` | Core policy or contract; reuse the existing business owner rather than placing policy in a host. |
| `src/Pegasus.Web/appsettings.json` | Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core. |
| `docs/engineering.md` | Authoritative context; update only if the ticket's accepted scope explicitly calls for it. |
| `src/Pegasus.Web/Desktop/` | Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core. |
| `src/Pegasus.Web/Mcp/AutomationTokenEndpoint.cs` | Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core. |
| `src/Pegasus.Infrastructure/Persistence/EfStaffAccountAdministration.cs` | Persistence or provider adapter; inspect data access, transaction and failure behaviour before changing callers. |
| `tests/Pegasus.IntegrationTests/DesktopTokenSecurityTests.cs` | Focused verification surface; extend the stated success, failure and regression coverage. |
| `src/Pegasus.Core/Actors/StaffActorFactory.cs` | Core policy or contract; reuse the existing business owner rather than placing policy in a host. |
| `tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj` | Focused verification surface; extend the stated success, failure and regression coverage. |

## Context files

- `docs/desktop/04-auth-session-update-and-startup/README.md` — Authoritative context; update only if the ticket's accepted scope explicitly calls for it.
- `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` — Authoritative context; update only if the ticket's accepted scope explicitly calls for it.
- `tests/Pegasus.IntegrationTests/StaffSignInSecurityTests.cs` — Focused verification surface; extend the stated success, failure and regression coverage.
- `tests/Pegasus.IntegrationTests/IntakePersistenceIntegrationTests.cs` — Focused verification surface; extend the stated success, failure and regression coverage.
- `tests/Pegasus.IntegrationTests/ReadinessEndpointTests.cs` — Focused verification surface; extend the stated success, failure and regression coverage.
- `src/Pegasus.Core/Identity/StaffAuthorization.cs` — Core policy or contract; reuse the existing business owner rather than placing policy in a host.
- `src/Pegasus.Core/Identity/IdentityContracts.cs` — Core policy or contract; reuse the existing business owner rather than placing policy in a host.
- `src/Pegasus.Core/Actors/StaffSessionPolicy.cs` — Core policy or contract; reuse the existing business owner rather than placing policy in a host.
- `src/Pegasus.Web/appsettings.json` — Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core.
- `docs/engineering.md` — Authoritative context; update only if the ticket's accepted scope explicitly calls for it.
- `src/Pegasus.Web/Desktop/` — Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core.
- `src/Pegasus.Web/Mcp/AutomationTokenEndpoint.cs` — Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core.
- `src/Pegasus.Infrastructure/Persistence/EfStaffAccountAdministration.cs` — Persistence or provider adapter; inspect data access, transaction and failure behaviour before changing callers.
- `tests/Pegasus.IntegrationTests/DesktopTokenSecurityTests.cs` — Focused verification surface; extend the stated success, failure and regression coverage.

## Ripple and out-of-scope boundary

- **Azure**: no write. The whole suite runs on the local Test/UAT stack (L-02); ADR-0014 stands and there is no Azure dev/test environment to ask for.
- **Scope boundary**: may add files under `tests/Pegasus.IntegrationTests` and the desktop view-model test project from [[DSK-02-13]]. Must not change production code — a test that only passes after a source change means the defect belongs in a new `fix` ticket, filed and linked, not patched here.
- **Cross-area**: this row spans both halves of plan 04. The gateway facts belong here in `gateway-api`; the DPAPI store evidence depends on the desktop client [[DSK-04-07]] in `desktop-foundation`, so schedule step 9 after that ticket lands and do not block the gateway facts on it.
- **Traps**: (a) do not assert lockout — ADR-0013 clause 12 makes throttling transient, and a test expecting `LockoutEnd` would enshrine the wrong control; (b) `CheckUpdateAvailabilityAsync` returns `Unknown` for a side-loaded MSIX, so any packaging-adjacent check must run against a local `.appinstaller` feed ([[DSK-04-12]]) or be recorded as not applicable; (c) never put a real credential in a test fixture — the plaintext `Bootstrap:VerificationAccount` must not become the desktop test login.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.
