# Plan — GWY-023: DSK-04-06 · Audited minimum-client-version setting, `/api/v1/client-compatibility` and the version gate

## Governing documents

- No canonical document is linked yet. Retain the ticket's existing `docs_todo` state; do not invent or link a proposed desktop ADR.

## Chosen approach

Store the minimum supported desktop version as a database-backed Administrator setting with audit, expose the anonymous `GET /api/v1/client-compatibility` endpoint returning `minimumVersion`, `currentVersion`, `channel`, `maintenanceMessage` and `validForSeconds`, and reject every `/api/v1` request whose `X-Pegasus-Client-Version` header is below the minimum with `urn:pegasus:problem:client-unsupported`.

## Routing and constraints

- Future owner: `pegasus-gateway-dev`; tests: `pegasus-test-engineer`; independent review: `pegasus-desktop-reviewer`.
- Use `dotnet-webapi`, `optimizing-ef-core-queries` where the ticket changes a query, and `run-tests` for the actual runner profile. The project decision overrides generic “service per endpoint” advice: route handlers translate to existing `Pegasus.Core` ports; no second policy/service layer is introduced.
- The shared EPIC context binds this to versioned `/api/v1` route groups in the existing `Pegasus.Web`, the existing rate-limiter mechanism, an OpenAPI snapshot, and no Azure write.

- Microsoft Learn (fetched 2026-08-24): [ASP.NET Core OpenAPI support](https://learn.microsoft.com/aspnet/core/fundamentals/openapi/overview?view=aspnetcore-10.0) confirms first-party OpenAPI generation. Use the repository’s planned committed snapshot and contract-test flow rather than adding a parallel API documentation path.


## Ordered implementation steps

1. **Orient.** Read `docs/desktop/04-auth-session-update-and-startup/README.md` § 3 decisions 5 and 6 and § 7 risks, plus `docs/desktop/03-gateway-api-and-data/endpoint-map.md:34`. Call Kanmer `get_doc_gates` for this ticket's board id, `take_ticket`, then load the skills under Routing.
2. **Read the exemplar setting before writing a new one.** `src/Pegasus.Infrastructure/Persistence/EfWorkflowConfigurationStore.cs:14-110` is the shape to copy: one row identified by a fixed policy key, a `Version` counter checked against `ExpectedVersion`, a serializable transaction, replay by `OperationKey` against `ActionHistory`, and `before`/`after` JSON snapshots. Also read `docs/adr/0018-provider-inspection-mode-database-setting.md` for why the setting belongs in the database.
3. **Add the Core contract.** In `src/Pegasus.Core/Identity/` (or the desktop-facing Core folder the plan document names) add `DesktopCompatibilityPolicy` with `MinimumClientVersion`, `Channel`, `MaintenanceMessage`, `Version`, plus `UpdateDesktopCompatibilityRequest(ActionActor Actor, string MinimumClientVersion, string? MaintenanceMessage, long ExpectedVersion, string Reason, string OperationKey)` and the store interface. Guard the write with `StaffAuthorization.Require(actor, StaffAccessRight.ManageWorkflowConfiguration)` — or add a new right only if review agrees; record the choice in the plan document rather than inventing one silently.
4. **Add the entity, configuration and migration.** Create the single-row entity beside `WorkflowConfigurationEntity` in `src/Pegasus.Infrastructure/Persistence/AdministrationPolicyEntities.cs` and its model configuration, then `dotnet ef migrations add DesktopCompatibilityPolicy --project src/Pegasus.Infrastructure --startup-project src/Pegasus.Web` using the pinned tool (`.config/dotnet-tools.json`, `dotnet-ef 10.0.10`). Seed the initial row in the migration with a minimum version of `0.0.0` so an unconfigured deployment blocks nothing.
5. **Close the runtime-role GRANT trap in the same commit.** Add `GRANT SELECT, INSERT, UPDATE ON OBJECT::[dbo].[<NewTable>] TO [pegasus_web_runtime_role];` to the migration's `Up`, mirror the expectation in `scripts/Invoke-AzureDatabaseBootstrap.ps1` (the block at `:103-139` is the pattern), and append the new migration id to the pinned census array in `tests/Pegasus.IntegrationTests/IntakePersistenceIntegrationTests.cs:22-95`. Done when `pwsh ./scripts/Test-MigrationGrants.ps1` exits 0 and `CommittedMigrationCreatesTheSqlServerSchema` passes.
6. **Implement the store** as `src/Pegasus.Infrastructure/Persistence/EfDesktopCompatibilityStore.cs`, copying the transaction, replay and snapshot structure of `EfWorkflowConfigurationStore` exactly; every update appends an `ActionHistory` row and a `SecurityEventType.SecurityConfigurationChanged` event so raising the minimum version is auditable.
7. **Add the configuration bootstrap fallback.** Read `Desktop:MinimumClientVersion` from configuration **only** when the row is absent, so a fresh deployment can start. It is a bootstrap value, not an operating control: the endpoint must report the database value whenever a row exists.
8. **Map the endpoint.** Add `GET /api/v1/client-compatibility` to the `/api/v1` group from [[DSK-03-02]], `AllowAnonymous`, returning `minimumVersion`, `currentVersion` (the `productVersion` computed at `src/Pegasus.Web/Program.cs:44-58`), `channel`, `maintenanceMessage` and `validForSeconds`. Set `validForSeconds` to 86400 to match the desktop's 24-hour fail-closed cache in plan § 3 decision 6, and do **not** exempt the endpoint from rate limiting.
9. **Add the version gate as a group-wide filter.** Add an endpoint filter (not a per-endpoint attribute) on the whole `/api/v1` group that parses `X-Pegasus-Client-Version` and, when it is missing, unparseable or below `MinimumClientVersion`, short-circuits with `urn:pegasus:problem:client-unsupported` carrying `minimumVersion`. Exempt only `GET /api/v1/client-compatibility` itself, or an obsolete client can never learn why it was refused. Compare with `System.Version`, never as strings.
10. **Settle the status code and record it.** Plan § 3 decision 5 leaves HTTP 426 versus 403 to area 03 ("03 fixes the code"). Read the code chosen in [[DSK-03-02]]'s problem-details mapping and use it; if that ticket has not landed, choose one, write it under a `## Decision` heading in the plan document, and add it to the problem-type catalogue in `docs/desktop/03-gateway-api-and-data/README.md:167` — do not leave two codes in the tree.
11. **Test.** Add `tests/Pegasus.IntegrationTests/DesktopCompatibilityGateTests.cs`, `[Trait("Category", "SqlServer")]`, using `LocalDbTestDatabase` and `ConfiguredWebApplicationFactory`. Facts: the anonymous endpoint returns all five fields; a request with a below-minimum header gets `client-unsupported` with `minimumVersion` in the body; a request with an equal or higher version succeeds; a missing or malformed header is refused; a non-administrator update attempt is refused; an administrator update writes an `ActionHistory` row and a `SecurityEvent`; a replayed `OperationKey` returns the same result and does not bump `Version` twice.
12. **Add the coverage guard.** Add a fact in `tests/Pegasus.ArchitectureTests` that fails if any endpoint mapped under `/api/v1` is not covered by the version filter (except the compatibility endpoint) — the plan requires the gate to cover the whole group, and a per-endpoint attribute would silently miss new routes.
13. **Operator step — the configuration fallback in production.** If, and only if, the bootstrap fallback must be set in production, that is a Container App app setting (`Desktop__MinimumClientVersion`) added beside `Features__AutomationMcp` in `infra/modules/platform.bicep:429`, which is a **⚠ Azure write**. The operator must give exact-target approval per `docs/runbook.md` § Live-operation approval matrix and hand back the approval text and the resulting revision name for the ticket proof. The normal path — raising the minimum version through the audited administrator setting — needs no approval and no Azure write; prefer it.
14. **Run** `dotnet test tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --filter "FullyQualifiedName~DesktopCompatibilityGate|FullyQualifiedName~IntakePersistence"`, `dotnet test tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj`, and `pwsh ./scripts/Test-MigrationGrants.ps1`; record all three in the post-implementation report.

## Acceptance conditions

- [ ] `GET /api/v1/client-compatibility` is anonymous and returns `minimumVersion`, `currentVersion`, `channel`, `maintenanceMessage`, `validForSeconds`.
- [ ] A request carrying `X-Pegasus-Client-Version` below the minimum receives `urn:pegasus:problem:client-unsupported` with `minimumVersion`, and performs no work.
- [ ] A request with a missing or unparseable client-version header is refused the same way.
- [ ] The minimum version is a database-backed Administrator setting; changing it writes an `ActionHistory` row and a `SecurityEvent`, and a replayed `OperationKey` is idempotent.
- [ ] A non-administrator cannot change it.
- [ ] The gate covers the whole `/api/v1` group, proven by an architecture test, with only the compatibility endpoint exempt.
- [ ] `pwsh ./scripts/Test-MigrationGrants.ps1` passes and the new migration id is in the pinned census.

## Verification

- [ ] `dotnet test tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --filter "FullyQualifiedName~DesktopCompatibilityGate"` — expected: all facts pass.
- [ ] `dotnet test tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --filter "FullyQualifiedName~IntakePersistence"` — expected: `CommittedMigrationCreatesTheSqlServerSchema` passes with the new migration id appended.
- [ ] `pwsh ./scripts/Test-MigrationGrants.ps1` — expected: exits 0, no ungranted table.
- [ ] `dotnet test tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj` — expected: the group-coverage fact passes.

## Risks and boundaries

- **⚠ Azure write** (conditional, step 13 only): a `Desktop__MinimumClientVersion` app setting on the Pegasus Web Container App, declared in `infra/modules/platform.bicep`. Needs exact-target approval per `docs/runbook.md` § Live-operation approval matrix, and must be mirrored in `docs/desktop/11-azure-disposition/README.md`. The database-backed setting is the intended control and needs no write; do not take the Azure route for convenience.
- **Scope boundary**: may touch `src/Pegasus.Core/Identity/`, `src/Pegasus.Infrastructure/Persistence/` (entity, configuration, store, migration), the `/api/v1` group, `scripts/Invoke-AzureDatabaseBootstrap.ps1`, and the three test projects. Must not touch `src/Pegasus.Worker`, the Razor pages, or the packaging/feed side, which [[DSK-04-12]] and area 09 own.
- **Traps**: (a) **runtime-role GRANT trap** (PLAT-035 class) — a new table without a GRANT fails on the first production save; migration, bootstrap script mirror and pinned census change together; (b) App Installer fails open, so the 24-hour client cache must not be extended "for convenience" — the gate is the fail-closed layer; (c) exempting the compatibility endpoint from the gate is mandatory, exempting anything else is a bypass; (d) compare versions as `System.Version`, never lexically, or `1.10.0` sorts below `1.9.0`.
- **Open question**: which `StaffAccessRight` guards the setting, and whether the refusal is HTTP 426 or 403 — steps 3 and 10 require both to be decided and recorded in the plan document, not invented in code.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.
