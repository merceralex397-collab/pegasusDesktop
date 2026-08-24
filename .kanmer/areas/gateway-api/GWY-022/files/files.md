# Files — GWY-022: DSK-04-05 · Revoke refresh tokens on account disable, password change, logout and sign-out-everywhere

## Change surface

| Path | Purpose / risk |
| --- | --- |
| `docs/desktop/04-auth-session-update-and-startup/README.md` | Authoritative context; update only if the ticket's accepted scope explicitly calls for it. |
| `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` | Authoritative context; update only if the ticket's accepted scope explicitly calls for it. |
| `src/Pegasus.Core/Identity/StaffAccountAdministration.cs` | Core policy or contract; reuse the existing business owner rather than placing policy in a host. |
| `src/Pegasus.Core/Identity/StaffPasswordChange.cs` | Core policy or contract; reuse the existing business owner rather than placing policy in a host. |
| `src/Pegasus.Infrastructure/Persistence/EfStaffAccountAdministration.cs` | Persistence or provider adapter; inspect data access, transaction and failure behaviour before changing callers. |
| `src/Pegasus.Infrastructure/Persistence/EfStaffPasswordChange.cs` | Persistence or provider adapter; inspect data access, transaction and failure behaviour before changing callers. |
| `src/Pegasus.Infrastructure/Persistence/Migrations/20260803151159_AutomationActorOpenIddict.cs` | Persistence or provider adapter; inspect data access, transaction and failure behaviour before changing callers. |
| `scripts/Invoke-AzureDatabaseBootstrap.ps1` | Repository verification or operational automation; preserve its checked-in workflow. |
| `src/Pegasus.Core/Identity/IdentityContracts.cs` | Core policy or contract; reuse the existing business owner rather than placing policy in a host. |
| `docs/frd/frd-04-parties-accounts-and-access.md` | Authoritative context; update only if the ticket's accepted scope explicitly calls for it. |
| `src/Pegasus.Core/Identity/` | Core policy or contract; reuse the existing business owner rather than placing policy in a host. |
| `docs/engineering.md` | Authoritative context; update only if the ticket's accepted scope explicitly calls for it. |
| `src/Pegasus.Infrastructure/Persistence/OpenIddictSessionRevocation.cs` | Persistence or provider adapter; inspect data access, transaction and failure behaviour before changing callers. |
| `src/Pegasus.Core/Identity/StaffAuthorization.cs` | Core policy or contract; reuse the existing business owner rather than placing policy in a host. |
| `scripts/Test-MigrationGrants.ps1` | Repository verification or operational automation; preserve its checked-in workflow. |
| `tests/Pegasus.IntegrationTests/IntakePersistenceIntegrationTests.cs` | Focused verification surface; extend the stated success, failure and regression coverage. |
| `tests/Pegasus.IntegrationTests/DesktopSessionRevocationTests.cs` | Focused verification surface; extend the stated success, failure and regression coverage. |
| `tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj` | Focused verification surface; extend the stated success, failure and regression coverage. |
| `tests/Pegasus.Core.Tests/Pegasus.Core.Tests.csproj` | Focused verification surface; extend the stated success, failure and regression coverage. |

## Context files

- `docs/desktop/04-auth-session-update-and-startup/README.md` — Authoritative context; update only if the ticket's accepted scope explicitly calls for it.
- `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` — Authoritative context; update only if the ticket's accepted scope explicitly calls for it.
- `src/Pegasus.Core/Identity/StaffAccountAdministration.cs` — Core policy or contract; reuse the existing business owner rather than placing policy in a host.
- `src/Pegasus.Core/Identity/StaffPasswordChange.cs` — Core policy or contract; reuse the existing business owner rather than placing policy in a host.
- `src/Pegasus.Infrastructure/Persistence/EfStaffAccountAdministration.cs` — Persistence or provider adapter; inspect data access, transaction and failure behaviour before changing callers.
- `src/Pegasus.Infrastructure/Persistence/EfStaffPasswordChange.cs` — Persistence or provider adapter; inspect data access, transaction and failure behaviour before changing callers.
- `src/Pegasus.Infrastructure/Persistence/Migrations/20260803151159_AutomationActorOpenIddict.cs` — Persistence or provider adapter; inspect data access, transaction and failure behaviour before changing callers.
- `scripts/Invoke-AzureDatabaseBootstrap.ps1` — Repository verification or operational automation; preserve its checked-in workflow.
- `src/Pegasus.Core/Identity/IdentityContracts.cs` — Core policy or contract; reuse the existing business owner rather than placing policy in a host.
- `docs/frd/frd-04-parties-accounts-and-access.md` — Authoritative context; update only if the ticket's accepted scope explicitly calls for it.
- `src/Pegasus.Core/Identity/` — Core policy or contract; reuse the existing business owner rather than placing policy in a host.
- `docs/engineering.md` — Authoritative context; update only if the ticket's accepted scope explicitly calls for it.
- `src/Pegasus.Infrastructure/Persistence/OpenIddictSessionRevocation.cs` — Persistence or provider adapter; inspect data access, transaction and failure behaviour before changing callers.
- `src/Pegasus.Core/Identity/StaffAuthorization.cs` — Core policy or contract; reuse the existing business owner rather than placing policy in a host.

## Ripple and out-of-scope boundary

- **Azure**: no write.
- **Scope boundary**: may touch `src/Pegasus.Core/Identity/` (the new port only), `src/Pegasus.Infrastructure/Persistence/EfStaffAccountAdministration.cs`, `EfStaffPasswordChange.cs`, the new adapter, the `/api/v1` session endpoints, and the two test projects. Must not touch `src/Pegasus.Worker`, `infra/`, or the Automation client registry.
- **Traps**: (a) **`DENY DELETE`** on all four OpenIddict tables for both runtime roles — revoke by status, never prune; (b) the revocation counters are persisted in the action-history `AfterJson` and re-read on replay, so computing them outside the transaction breaks idempotency; (c) the runtime-role GRANT trap (PLAT-035 class) — if this ticket ever adds a table, the `Grant*` migration, the `Invoke-AzureDatabaseBootstrap.ps1` mirror and the pinned census in `IntakePersistenceIntegrationTests.cs` all change together; (d) `Pegasus.Core` must not reference OpenIddict — keep the port abstract or `tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs` fails.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.
