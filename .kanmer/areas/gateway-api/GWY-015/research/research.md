# Research — GWY-015: DSK-03-15 · Administration endpoints: configuration, mailboxes, accounts, roles, automation, organizations, principals

## Question

Project the sixteen administration page models onto `/api/v1/admin`: workflow configuration, approved mail categories, approved mailboxes and folder resolution, access review, staff accounts, roles, the automation client settings and activity, organizations and principals — each behind its own `StaffAccessRight` and each mutation writing a security or action-history record.

## Evidence examined

- Plan row: `docs/desktop/03-gateway-api-and-data/README.md` § 5 — `DSK-03-15`
- Plan detail: same file § 3 — rows *Idempotency*, *Concurrency*, *Audit & transactions*
- Plan detail: `docs/desktop/03-gateway-api-and-data/endpoint-map.md` § Administration and audit
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 13.10 Administration and operations, § 17.1 Required controls, § 18.3 Health
- Endpoint contracts quoted from `endpoint-map.md` § Administration and audit (route · replaces · right):
  - `GET /admin/configuration`, `PUT /admin/configuration` · `Administration/Configuration` `OnGetAsync`, `OnPostAsync` · `ManageWorkflowConfiguration` · idempotent by key · tokens configuration version + `operationKey` · phase 8.
  - `GET /admin/mail-categories`, `PUT /admin/mail-categories` · `Administration/MailCategories` `OnGetAsync`, `OnPostSaveAsync` · `ManageApprovedOutlookCategories` · version + `operationKey` · phase 8.
  - `GET /admin/mailboxes`, `PUT /admin/mailboxes/{id}`, `POST /admin/mailboxes/{id}/resolve-folders` · `Administration/Mailboxes` `OnGetAsync`, `OnPostUpdateAsync`, `OnPostResolveFoldersAsync` (362 lines) · `ManageApprovedMailboxes` · mailbox version + `operationKey` · phase 8.
  - `GET /admin/access-review`, `POST /admin/access-review` · `Administration/Access/Index` `OnGetAsync`, `OnPostReviewAsync` · `ReviewStaffAccess` · `operationKey` · phase 8.
  - `GET /admin/accounts`, `POST /admin/accounts`, `GET /admin/accounts/{id}`, `POST /admin/accounts/{id}/disable` · `Administration/Accounts/Index` `OnGetAsync`, `OnPostCreateAsync`; `Accounts/Edit` `OnGetAsync`, `OnPostDisableAsync` · `ManageStaffAccounts` · `operationKey`; disable requires `reason`; disabled → tokens revoked · phase 8.
  - `GET /admin/roles`, `POST /admin/roles/assign` · `Administration/Roles/Index` `OnGetAsync`, `OnPostAssignAsync` · `AssignStaffRoles` · `operationKey` · phase 8.
  - `GET /admin/automation`, `POST /admin/automation/enabled`, `POST /admin/automation/send-to-ai-enabled`, `PUT /admin/automation/connector`, `POST /admin/automation/channel-token/rotate`, `POST /admin/automation/channel-token/clear`, `GET /admin/automation/activity` · `Administration/Automation/Index` five handlers (260 lines) and `Automation/Activity` `OnGetAsync` · `ManageAutomationClients` · `operationKey` · phase 8.
  - `GET /admin/organizations`, `POST /admin/organizations`, `GET /admin/organizations/{id}`, `PUT /admin/organizations/{id}` · `Administration/Organizations/Index` `OnGetAsync`, `OnPostCreateAsync`; `Organizations/Edit` `OnGetAsync`, `OnPostUpdateAsync` · `ManageOrganizationsAndPrincipals` · organization version + `operationKey` (key ≤ 100) · phase 8.
  - `GET /admin/principals`, `POST /admin/principals`, `POST /admin/principals/{id}/replace` · `Administration/Principals/Index`, `Principals/Create` `OnPostCreateAsync`, `Principals/Replace` `OnPostReplaceAsync` · `ManageOrganizationsAndPrincipals` · `operationKey`, `reason` · phase 8.
  - `GET /audit?actor&case&from&to&page` · history partials and `Automation/Activity` · `ManageStaffAccounts` (full search) · phase 8 — the case-history half is delivered by [[DSK-03-07]].
  - `GET /admin/health` · new (§ 18.3) · aggregation over the existing checks plus the worker's last cycle and provider state · `ManageWorkflowConfiguration` · returns dependency states, minimum client version, feed state · phase 8.
- Repository evidence:
  - The sixteen page models under `src/Pegasus.Web/Pages/Administration/` and their handler names: `Configuration` (`OnGetAsync`, `OnPostAsync`), `MailCategories` (`OnPostSaveAsync`), `Mailboxes` (`OnPostUpdateAsync`, `OnPostResolveFoldersAsync`), `Access/Index` (`OnPostReviewAsync`), `Accounts/Index` (`OnPostCreateAsync`), `Accounts/Edit` (`OnPostDisableAsync`), `Roles/Index` (`OnPostAssignAsync`), `Automation/Index` (`OnPostSetEnabledAsync`, `OnPostSetSendToAiEnabledAsync`, `OnPostUpdateConnectorAsync`, `OnPostRotateChannelTokenAsync`, `OnPostClearChannelTokenAsync`), `Automation/Activity`, `Organizations/Index` (`OnPostCreateAsync`), `Organizations/Edit` (`OnPostUpdateAsync`), `Principals/Create` (`OnPostCreateAsync`), `Principals/Replace` (`OnPostReplaceAsync`)
  - `src/Pegasus.Core/Identity/StaffAuthorization.cs:7-21` — the twelve rights, including all eight administration rights used above
  - `src/Pegasus.Core/Identity/StaffAccountAdministration.cs:410` — the 100-character operation-key limit
  - `src/Pegasus.Core/Cases/OrganizationAdministration.cs:274` — the same limit for organizations
  - `src/Pegasus.Web/Program.cs:939-950` — the existing `/health/live` and `/health/ready` checks the `admin/health` aggregation reads
  - `tests/Pegasus.IntegrationTests/AdministrationPolicyPersistenceTests.cs`, `AdministrationSearchAccountWebTests.cs`, `ApprovedMailboxAdministrationWebTests.cs`, `ApprovedOutlookCategoryAdministrationWebTests.cs`, `OrganizationAdministrationWebTests.cs`, `SendToAiConnectorAdministrationTests.cs` — the scenarios the new tests mirror
- Binding decisions:
  - L-01 — endpoints evolve inside `Pegasus.Web`.
  - L-02 — evidence from the local stack; the Graph folder resolver is a Web-only read with a replay adapter.
- Depends on: `DSK-03-03` for the right filter and actor resolution.

## Scope and constraints

Proposal § 13.10 requires administration and operations to work natively, and § 17.1 requires central enforcement of permissions, revocation and audit. Operator-visible consequence: an administrator manages accounts, roles, mailboxes and the automation client from the desktop, and every change is attributable — disabling an account revokes its tokens, and a non-administrator gets a `not-authorized` problem instead of a silently ignored request. This is the last endpoint group before the Phase 8 hardening gate.

- Future owner: `pegasus-gateway-dev`; tests: `pegasus-test-engineer`; independent review: `pegasus-desktop-reviewer`.
- Use `dotnet-webapi`, `optimizing-ef-core-queries` where the ticket changes a query, and `run-tests` for the actual runner profile. The project decision overrides generic “service per endpoint” advice: route handlers translate to existing `Pegasus.Core` ports; no second policy/service layer is introduced.
- The shared EPIC context binds this to versioned `/api/v1` route groups in the existing `Pegasus.Web`, the existing rate-limiter mechanism, an OpenAPI snapshot, and no Azure write.

- Microsoft Learn (fetched 2026-08-24): [ASP.NET Core OpenAPI support](https://learn.microsoft.com/aspnet/core/fundamentals/openapi/overview?view=aspnetcore-10.0) confirms first-party OpenAPI generation. Use the repository’s planned committed snapshot and contract-test flow rather than adding a parallel API documentation path.

- **Azure**: no write.
- **Scope boundary**: may touch `src/Pegasus.Web/Api/**`, `src/Pegasus.Contracts/Admin/**`, `openapi/`, the generated client and the test projects. Must not touch `src/Pegasus.Core/Identity/**`, `src/Pegasus.Web/Pages/Administration/**`, or the OpenIddict client registry.
- **Traps**: two policy engines — administration rules live in the Core administration use cases; the endpoint filter only fails fast. Automation holds `PerformCasework` only (ADR-0011), so an Automation token must never reach an administration route — the audience rejection from [[DSK-03-03]] carries that. The observability blind spot is real: App Insights ingestion is capped at 0.1 GB/day (PLAT-034), so problem details with correlation ids are the compensating evidence for administration failures in production.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.

## Governing documents

- No canonical document is linked yet. Retain the ticket's existing `docs_todo` state; do not invent or link a proposed desktop ADR.

## Planning implication

Reuse the cited boundaries and revalidate the named sources against current `origin/dev` after the ticket is taken. Do not create a compatibility path, duplicate policy, or an unapproved external write.
