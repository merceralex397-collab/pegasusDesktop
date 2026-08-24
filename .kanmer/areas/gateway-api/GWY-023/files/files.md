# Files — GWY-023: DSK-04-06 · Audited minimum-client-version setting, `/api/v1/client-compatibility` and the version gate

## Change surface

| Path | Purpose / risk |
| --- | --- |
| `docs/desktop/04-auth-session-update-and-startup/README.md` | Authoritative context; update only if the ticket's accepted scope explicitly calls for it. |
| `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` | Authoritative context; update only if the ticket's accepted scope explicitly calls for it. |
| `src/Pegasus.Web/Program.cs` | Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core. |
| `docs/desktop/03-gateway-api-and-data/endpoint-map.md` | Authoritative context; update only if the ticket's accepted scope explicitly calls for it. |
| `docs/desktop/03-gateway-api-and-data/README.md` | Authoritative context; update only if the ticket's accepted scope explicitly calls for it. |
| `src/Pegasus.Infrastructure/Persistence/EfWorkflowConfigurationStore.cs` | Persistence or provider adapter; inspect data access, transaction and failure behaviour before changing callers. |
| `src/Pegasus.Infrastructure/Persistence/AdministrationPolicyEntities.cs` | Persistence or provider adapter; inspect data access, transaction and failure behaviour before changing callers. |
| `docs/adr/0018-provider-inspection-mode-database-setting.md` | Authoritative context; update only if the ticket's accepted scope explicitly calls for it. |
| `docs/runbook.md` | Authoritative context; update only if the ticket's accepted scope explicitly calls for it. |
| `infra/modules/platform.bicep` | Named by the ticket as an implementation or verification dependency. |
| `src/Pegasus.Infrastructure/Persistence/Migrations/20260729176000_AzureSqlRuntimeLeastPrivilege.cs` | Persistence or provider adapter; inspect data access, transaction and failure behaviour before changing callers. |
| `scripts/Test-MigrationGrants.ps1` | Repository verification or operational automation; preserve its checked-in workflow. |
| `tests/Pegasus.IntegrationTests/IntakePersistenceIntegrationTests.cs` | Focused verification surface; extend the stated success, failure and regression coverage. |
| `src/Pegasus.Core/Identity/` | Core policy or contract; reuse the existing business owner rather than placing policy in a host. |
| `src/Pegasus.Infrastructure` | Named by the ticket as an implementation or verification dependency. |
| `src/Pegasus.Web` | Named by the ticket as an implementation or verification dependency. |
| `scripts/Invoke-AzureDatabaseBootstrap.ps1` | Repository verification or operational automation; preserve its checked-in workflow. |
| `src/Pegasus.Infrastructure/Persistence/EfDesktopCompatibilityStore.cs` | Persistence or provider adapter; inspect data access, transaction and failure behaviour before changing callers. |
| `tests/Pegasus.IntegrationTests/DesktopCompatibilityGateTests.cs` | Focused verification surface; extend the stated success, failure and regression coverage. |
| `tests/Pegasus.ArchitectureTests` | Focused verification surface; extend the stated success, failure and regression coverage. |
| `tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj` | Focused verification surface; extend the stated success, failure and regression coverage. |
| `tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj` | Focused verification surface; extend the stated success, failure and regression coverage. |

## Context files

- `docs/desktop/04-auth-session-update-and-startup/README.md` — Authoritative context; update only if the ticket's accepted scope explicitly calls for it.
- `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` — Authoritative context; update only if the ticket's accepted scope explicitly calls for it.
- `src/Pegasus.Web/Program.cs` — Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core.
- `docs/desktop/03-gateway-api-and-data/endpoint-map.md` — Authoritative context; update only if the ticket's accepted scope explicitly calls for it.
- `docs/desktop/03-gateway-api-and-data/README.md` — Authoritative context; update only if the ticket's accepted scope explicitly calls for it.
- `src/Pegasus.Infrastructure/Persistence/EfWorkflowConfigurationStore.cs` — Persistence or provider adapter; inspect data access, transaction and failure behaviour before changing callers.
- `src/Pegasus.Infrastructure/Persistence/AdministrationPolicyEntities.cs` — Persistence or provider adapter; inspect data access, transaction and failure behaviour before changing callers.
- `docs/adr/0018-provider-inspection-mode-database-setting.md` — Authoritative context; update only if the ticket's accepted scope explicitly calls for it.
- `docs/runbook.md` — Authoritative context; update only if the ticket's accepted scope explicitly calls for it.
- `infra/modules/platform.bicep` — Named by the ticket as an implementation or verification dependency.
- `src/Pegasus.Infrastructure/Persistence/Migrations/20260729176000_AzureSqlRuntimeLeastPrivilege.cs` — Persistence or provider adapter; inspect data access, transaction and failure behaviour before changing callers.
- `scripts/Test-MigrationGrants.ps1` — Repository verification or operational automation; preserve its checked-in workflow.
- `tests/Pegasus.IntegrationTests/IntakePersistenceIntegrationTests.cs` — Focused verification surface; extend the stated success, failure and regression coverage.
- `src/Pegasus.Core/Identity/` — Core policy or contract; reuse the existing business owner rather than placing policy in a host.

## Ripple and out-of-scope boundary

- **⚠ Azure write** (conditional, step 13 only): a `Desktop__MinimumClientVersion` app setting on the Pegasus Web Container App, declared in `infra/modules/platform.bicep`. Needs exact-target approval per `docs/runbook.md` § Live-operation approval matrix, and must be mirrored in `docs/desktop/11-azure-disposition/README.md`. The database-backed setting is the intended control and needs no write; do not take the Azure route for convenience.
- **Scope boundary**: may touch `src/Pegasus.Core/Identity/`, `src/Pegasus.Infrastructure/Persistence/` (entity, configuration, store, migration), the `/api/v1` group, `scripts/Invoke-AzureDatabaseBootstrap.ps1`, and the three test projects. Must not touch `src/Pegasus.Worker`, the Razor pages, or the packaging/feed side, which [[DSK-04-12]] and area 09 own.
- **Traps**: (a) **runtime-role GRANT trap** (PLAT-035 class) — a new table without a GRANT fails on the first production save; migration, bootstrap script mirror and pinned census change together; (b) App Installer fails open, so the 24-hour client cache must not be extended "for convenience" — the gate is the fail-closed layer; (c) exempting the compatibility endpoint from the gate is mandatory, exempting anything else is a bypass; (d) compare versions as `System.Version`, never lexically, or `1.10.0` sorts below `1.9.0`.
- **Open question**: which `StaffAccessRight` guards the setting, and whether the refusal is HTTP 426 or 403 — steps 3 and 10 require both to be decided and recorded in the plan document, not invented in code.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.
