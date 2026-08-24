# Plan — GWY-015: DSK-03-15 · Administration endpoints: configuration, mailboxes, accounts, roles, automation, organizations, principals

## Governing documents

- No canonical document is linked yet. Retain the ticket's existing `docs_todo` state; do not invent or link a proposed desktop ADR.

## Chosen approach

Project the sixteen administration page models onto `/api/v1/admin`: workflow configuration, approved mail categories, approved mailboxes and folder resolution, access review, staff accounts, roles, the automation client settings and activity, organizations and principals — each behind its own `StaffAccessRight` and each mutation writing a security or action-history record.

## Routing and constraints

- Future owner: `pegasus-gateway-dev`; tests: `pegasus-test-engineer`; independent review: `pegasus-desktop-reviewer`.
- Use `dotnet-webapi`, `optimizing-ef-core-queries` where the ticket changes a query, and `run-tests` for the actual runner profile. The project decision overrides generic “service per endpoint” advice: route handlers translate to existing `Pegasus.Core` ports; no second policy/service layer is introduced.
- The shared EPIC context binds this to versioned `/api/v1` route groups in the existing `Pegasus.Web`, the existing rate-limiter mechanism, an OpenAPI snapshot, and no Azure write.

- Microsoft Learn (fetched 2026-08-24): [ASP.NET Core OpenAPI support](https://learn.microsoft.com/aspnet/core/fundamentals/openapi/overview?view=aspnetcore-10.0) confirms first-party OpenAPI generation. Use the repository’s planned committed snapshot and contract-test flow rather than adding a parallel API documentation path.


## Ordered implementation steps

1. Orient. Read every row quoted above in `docs/desktop/03-gateway-api-and-data/endpoint-map.md` § Administration and audit, then `docs/desktop/10-security-observability-performance/README.md` for the health surface `GET /admin/health` must aggregate. Then `get_doc_gates <this ticket id>` and `take_ticket`.
2. Read all sixteen page models under `src/Pegasus.Web/Pages/Administration/` and record, per handler, the Core interface, the version it carries and the audit record it writes.
3. Add `src/Pegasus.Contracts/Admin/` DTOs, one file per administration area, each command carrying `operationKey`, the entity version where one exists, and `reason` where Core requires it (account disable, principal replace).
4. Add `src/Pegasus.Web/Api/AdminEndpoints.cs` mapping an `admin` sub-group with **no** blanket right; each sub-group declares its own right with `.RequireStaffRight(...)`: configuration → `ManageWorkflowConfiguration`, mail categories → `ManageApprovedOutlookCategories`, mailboxes → `ManageApprovedMailboxes`, access review → `ReviewStaffAccess`, accounts → `ManageStaffAccounts`, roles → `AssignStaffRoles`, automation → `ManageAutomationClients`, organizations and principals → `ManageOrganizationsAndPrincipals`.
5. Implement `POST /admin/accounts/{id}/disable` so it revokes the account's refresh tokens through the same path `DSK-04-05` establishes — do not add a second revocation implementation. If `DSK-04-05` has not landed, call the existing Identity path the Razor handler calls and record the dependency.
6. Implement `GET /admin/health` as an aggregation over the existing health checks registered at `src/Pegasus.Web/Program.cs:939-950`, plus the worker's last-cycle state and provider state, plus the minimum client version from `DSK-04-06` and the update-feed state from area 09. Where a component is not yet available, return it as an explicitly unknown state rather than omitting it or inventing a value.
7. Cap every administration `operationKey` at 100 characters, matching `src/Pegasus.Core/Identity/StaffAccountAdministration.cs:410` and `src/Pegasus.Core/Cases/OrganizationAdministration.cs:274`.
8. Verify that every mutation writes a security-event or action-history record by asserting the record, not the status code: read it back through the same ports the Automation activity view uses (`src/Pegasus.Core/Identity/IdentityContracts.cs:98-137`).
9. Add `GET /audit` full search behind `ManageStaffAccounts`, extending (not duplicating) the endpoint [[DSK-03-07]] added for case history — one route, two right levels, exactly as the endpoint-map row describes.
10. Add `tests/Pegasus.IntegrationTests/DesktopGatewayAdminTests.cs`: for each administration command, the seven-case matrix plus an explicit non-administrator fact returning the `not-authorized` problem, and a fact asserting the audit record exists with the right actor. Add a fact that disabling an account invalidates its tokens.
11. Regenerate and commit the OpenAPI snapshot and the generated client.
12. Run `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj -c Release --filter "FullyQualifiedName~DesktopGatewayAdminTests"`, then run the simplification pass and record it under a dated `## Simplification pass` heading in the ticket plan.

## Acceptance conditions

- [ ] Each administration sub-group declares its own `StaffAccessRight`; there is no blanket administrator gate.
- [ ] Every mutation writes a security-event or action-history record, asserted by reading it back.
- [ ] A non-administrator receives the `not-authorized` problem for every administration route.
- [ ] Disabling an account revokes its tokens through the single existing revocation path.
- [ ] `GET /admin/health` reports dependency states, minimum client version and feed state, with unknown components explicitly unknown.

## Verification

- [ ] `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj -c Release --filter "FullyQualifiedName~DesktopGatewayAdminTests"` — expected: all facts pass, including the audit-record and token-revocation facts.
- [ ] `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj -c Release --filter "FullyQualifiedName~AdministrationPolicyPersistenceTests"` — expected: the existing administration tests still pass unchanged.

## Risks and boundaries

- **Azure**: no write.
- **Scope boundary**: may touch `src/Pegasus.Web/Api/**`, `src/Pegasus.Contracts/Admin/**`, `openapi/`, the generated client and the test projects. Must not touch `src/Pegasus.Core/Identity/**`, `src/Pegasus.Web/Pages/Administration/**`, or the OpenIddict client registry.
- **Traps**: two policy engines — administration rules live in the Core administration use cases; the endpoint filter only fails fast. Automation holds `PerformCasework` only (ADR-0011), so an Automation token must never reach an administration route — the audience rejection from [[DSK-03-03]] carries that. The observability blind spot is real: App Insights ingestion is capped at 0.1 GB/day (PLAT-034), so problem details with correlation ids are the compensating evidence for administration failures in production.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.
