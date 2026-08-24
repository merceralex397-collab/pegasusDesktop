# Research — GWY-023: DSK-04-06 · Audited minimum-client-version setting, `/api/v1/client-compatibility` and the version gate

## Question

Store the minimum supported desktop version as a database-backed Administrator setting with audit, expose the anonymous `GET /api/v1/client-compatibility` endpoint returning `minimumVersion`, `currentVersion`, `channel`, `maintenanceMessage` and `validForSeconds`, and reject every `/api/v1` request whose `X-Pegasus-Client-Version` header is below the minimum with `urn:pegasus:problem:client-unsupported`.

## Evidence examined

- Plan row: `docs/desktop/04-auth-session-update-and-startup/README.md` § 5 — `DSK-04-06`
- Plan detail: same file § 3 decision 5 (the setting is a DB-backed Administrator setting with audit, not a Container App app setting) and decision 6 (24-hour fail-closed client cache); § 7 risks "App Installer fail-open" and "Runtime-role GRANT trap"
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 9.1 Two-layer enforcement, § 9.2 Startup sequence, § 9.3 Operational controls
- Repository evidence:
  - `src/Pegasus.Web/Program.cs:954-958` — the existing anonymous `GET /diagnostics/version` and the `productVersion`/`sourceSha` values derived at `Program.cs:44-60` from `AssemblyInformationalVersionAttribute`
  - `docs/desktop/03-gateway-api-and-data/endpoint-map.md:34` — the contract row: `GET /client-compatibility`, anonymous, backed by an "admin setting (area 04)", tier 2
  - `docs/desktop/03-gateway-api-and-data/README.md:167` — problem-type catalogue including `client-unsupported` and `maintenance`
  - `src/Pegasus.Infrastructure/Persistence/EfWorkflowConfigurationStore.cs:14-110` — the exemplar DB-backed administrator setting: single-row policy key, `Version` optimistic-concurrency counter, serializable transaction, replay by `OperationKey` against `ActionHistory`, `before`/`after` snapshots
  - `src/Pegasus.Infrastructure/Persistence/AdministrationPolicyEntities.cs:3` — `WorkflowConfigurationEntity`, and `AdministrationPolicyModelConfiguration.WorkflowPolicyKey` for the single-row key pattern
  - `docs/adr/0018-provider-inspection-mode-database-setting.md` and `docs/runbook.md` § Provider inspection-mode setting (line 461) — the precedent for a setting that lives in the database rather than in app configuration
  - `infra/modules/platform.bicep:429` — `{ name: 'Features__AutomationMcp', value: 'true' }`: the only place a Container App app setting is declared, which is why any configuration fallback is an Azure write
  - `src/Pegasus.Infrastructure/Persistence/Migrations/20260729176000_AzureSqlRuntimeLeastPrivilege.cs` — the runtime roles; `scripts/Test-MigrationGrants.ps1` enforces a GRANT for every new table
  - `tests/Pegasus.IntegrationTests/IntakePersistenceIntegrationTests.cs:22-95` — the pinned migration census that a new migration must be appended to
- Binding decisions:
  - **L-01** — the endpoint and the gate live inside `Pegasus.Web`; no new deployment unit
  - **L-02** — Test/UAT is the local stack; the gate is exercised locally, never against an Azure test resource
  - **L-04** — this ticket names its subagent, skills and MCP tools
  - **ADR-0105** (owed, `docs_todo`) — MSIX/App Installer distribution with a gateway minimum-version gate
- Depends on: `DSK-04-04` — the `/api/v1` authentication pipeline the gate middleware sits in

## Scope and constraints

Proposal §9.1 requires two independent enforcement layers, because App Installer **fails open**: if the update feed is unreachable the operating system launches the old app anyway. The gateway gate is the fail-closed layer, and it is also the only way to reject a version with a serious defect before a package can be pushed. `Pegasus.Web` has no client-version header, no minimum-version gate and no compatibility endpoint today — `GET /diagnostics/version` (`src/Pegasus.Web/Program.cs:954-958`) returns only `{version, sourceSha}`. Without this, [[DSK-04-09]]'s startup gate has nothing to call and an obsolete desktop keeps writing to production.

- Future owner: `pegasus-gateway-dev`; tests: `pegasus-test-engineer`; independent review: `pegasus-desktop-reviewer`.
- Use `dotnet-webapi`, `optimizing-ef-core-queries` where the ticket changes a query, and `run-tests` for the actual runner profile. The project decision overrides generic “service per endpoint” advice: route handlers translate to existing `Pegasus.Core` ports; no second policy/service layer is introduced.
- The shared EPIC context binds this to versioned `/api/v1` route groups in the existing `Pegasus.Web`, the existing rate-limiter mechanism, an OpenAPI snapshot, and no Azure write.

- Microsoft Learn (fetched 2026-08-24): [ASP.NET Core OpenAPI support](https://learn.microsoft.com/aspnet/core/fundamentals/openapi/overview?view=aspnetcore-10.0) confirms first-party OpenAPI generation. Use the repository’s planned committed snapshot and contract-test flow rather than adding a parallel API documentation path.

- **⚠ Azure write** (conditional, step 13 only): a `Desktop__MinimumClientVersion` app setting on the Pegasus Web Container App, declared in `infra/modules/platform.bicep`. Needs exact-target approval per `docs/runbook.md` § Live-operation approval matrix, and must be mirrored in `docs/desktop/11-azure-disposition/README.md`. The database-backed setting is the intended control and needs no write; do not take the Azure route for convenience.
- **Scope boundary**: may touch `src/Pegasus.Core/Identity/`, `src/Pegasus.Infrastructure/Persistence/` (entity, configuration, store, migration), the `/api/v1` group, `scripts/Invoke-AzureDatabaseBootstrap.ps1`, and the three test projects. Must not touch `src/Pegasus.Worker`, the Razor pages, or the packaging/feed side, which [[DSK-04-12]] and area 09 own.
- **Traps**: (a) **runtime-role GRANT trap** (PLAT-035 class) — a new table without a GRANT fails on the first production save; migration, bootstrap script mirror and pinned census change together; (b) App Installer fails open, so the 24-hour client cache must not be extended "for convenience" — the gate is the fail-closed layer; (c) exempting the compatibility endpoint from the gate is mandatory, exempting anything else is a bypass; (d) compare versions as `System.Version`, never lexically, or `1.10.0` sorts below `1.9.0`.
- **Open question**: which `StaffAccessRight` guards the setting, and whether the refusal is HTTP 426 or 403 — steps 3 and 10 require both to be decided and recorded in the plan document, not invented in code.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.

## Governing documents

- No canonical document is linked yet. Retain the ticket's existing `docs_todo` state; do not invent or link a proposed desktop ADR.

## Planning implication

Reuse the cited boundaries and revalidate the named sources against current `origin/dev` after the ticket is taken. Do not create a compatibility path, duplicate policy, or an unapproved external write.
