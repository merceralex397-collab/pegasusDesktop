# Checklist — GWY-015: DSK-03-15 · Administration endpoints: configuration, mailboxes, accounts, roles, automation, organizations, principals

- [ ] Orient. Read every row quoted above in `docs/desktop/03-gateway-api-and-data/endpoint-map.md` § Administration and audit, then `docs/desktop/10-security-observability-performance/README.md` for the health surface `GET /admin/health` must aggregate. Then `get_doc_gates <this ticket id>` and `take_ticket`.
- [ ] Read all sixteen page models under `src/Pegasus.Web/Pages/Administration/` and record, per handler, the Core interface, the version it carries and the audit record it writes.
- [ ] Add `src/Pegasus.Contracts/Admin/` DTOs, one file per administration area, each command carrying `operationKey`, the entity version where one exists, and `reason` where Core requires it (account disable, principal replace).
- [ ] Add `src/Pegasus.Web/Api/AdminEndpoints.cs` mapping an `admin` sub-group with no blanket right; each sub-group declares its own right with `.RequireStaffRight(...)`: configuration → `ManageWorkflowConfiguration`, mail categories → `ManageApprovedOutlookCategories`, mailboxes → `ManageApprovedMailboxes`, access review → `ReviewStaffAccess`, accounts → `ManageStaffAccounts`, roles → `AssignStaffRoles`, automation → `ManageAutomationClients`, organizations and principals → `ManageOrganizationsAndPrincipals`.
- [ ] Implement `POST /admin/accounts/{id}/disable` so it revokes the account's refresh tokens through the same path `DSK-04-05` establishes — do not add a second revocation implementation. If `DSK-04-05` has not landed, call the existing Identity path the Razor handler calls and record the dependency.
- [ ] Implement `GET /admin/health` as an aggregation over the existing health checks registered at `src/Pegasus.Web/Program.cs:939-950`, plus the worker's last-cycle state and provider state, plus the minimum client version from `DSK-04-06` and the update-feed state from area 09. Where a component is not yet available, return it as an explicitly unknown state rather than omitting it or inventing a value.
- [ ] Cap every administration `operationKey` at 100 characters, matching `src/Pegasus.Core/Identity/StaffAccountAdministration.cs:410` and `src/Pegasus.Core/Cases/OrganizationAdministration.cs:274`.
- [ ] Verify that every mutation writes a security-event or action-history record by asserting the record, not the status code: read it back through the same ports the Automation activity view uses (`src/Pegasus.Core/Identity/IdentityContracts.cs:98-137`).
- [ ] Add `GET /audit` full search behind `ManageStaffAccounts`, extending (not duplicating) the endpoint [[DSK-03-07]] added for case history — one route, two right levels, exactly as the endpoint-map row describes.
- [ ] Add `tests/Pegasus.IntegrationTests/DesktopGatewayAdminTests.cs`: for each administration command, the seven-case matrix plus an explicit non-administrator fact returning the `not-authorized` problem, and a fact asserting the audit record exists with the right actor. Add a fact that disabling an account invalidates its tokens.
- [ ] Regenerate and commit the OpenAPI snapshot and the generated client.
- [ ] Run `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj -c Release --filter "FullyQualifiedName~DesktopGatewayAdminTests"`, then run the simplification pass and record it under a dated `## Simplification pass` heading in the ticket plan.
- [ ] Each administration sub-group declares its own `StaffAccessRight`; there is no blanket administrator gate.
- [ ] Every mutation writes a security-event or action-history record, asserted by reading it back.
- [ ] A non-administrator receives the `not-authorized` problem for every administration route.
- [ ] Disabling an account revokes its tokens through the single existing revocation path.
- [ ] `GET /admin/health` reports dependency states, minimum client version and feed state, with unknown components explicitly unknown.
- [ ] `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj -c Release --filter "FullyQualifiedName~DesktopGatewayAdminTests"` — expected: all facts pass, including the audit-record and token-revocation facts.
- [ ] `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj -c Release --filter "FullyQualifiedName~AdministrationPolicyPersistenceTests"` — expected: the existing administration tests still pass unchanged.

## Progress notes

No implementation has started. This checklist is derived from the ticket’s accepted scope and is maintained by the ticket implementer.
