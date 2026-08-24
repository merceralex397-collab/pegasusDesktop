# Research — GWY-019: the OpenIddict public client `pegasus-desktop`

## Question

What does `Pegasus.Web` already compose for OpenIddict, what exactly must be extracted so a
desktop token flow does not depend on the `Features:AutomationMcp` gate, and does the pinned
OpenIddict 7.6.0 actually expose the APIs plan 04 assumption **A1** says it should — before a
line of the handler is written?

## Current behaviour

Today the web application issues tokens for **one** caller: the machine Automation client.

- The whole OpenIddict server lives inside `AddPegasusAutomationMcp`
  (`src/Pegasus.Web/Mcp/AutomationMcpExtensions.cs:33-72`): `AddOpenIddict()` at `:33`,
  EF Core stores on `PegasusDbContext` (`:34-36`), token endpoint `/connect/token` (`:39`),
  authorization endpoint `/authorize` (`:40`), client-credentials (`:41`),
  authorization-code + PKCE (`:45`), refresh-token (`:47`), the `automation.*` scopes (`:48`),
  a 10-minute access token (`:52`), a 14-day refresh token (`:53`),
  `DisableSlidingRefreshTokenExpiration()` (`:55`), and **ephemeral** keys at `:58-59`.
- That extension is called only behind the gate: `Program.cs:246` builds
  `automationMcpOptions` from `Features:AutomationMcp`, `:625-627` calls
  `AddPegasusAutomationMcp` only `if (automationMcpOptions is not null)`, and `:961-964` calls
  `MapPegasusAutomationMcp` under the same condition. **With `Features:AutomationMcp` off there
  is no `/connect/token` at all**, which is precisely the trap the ticket's Guardrails name.
- Token issuance itself is a passthrough handler,
  `src/Pegasus.Web/Mcp/AutomationTokenEndpoint.cs:27-91`, whose principal shape is fixed by
  `AutomationPrincipal.Create` (`:112-123`): subject = client id, scopes, the fixed MCP
  audience, and `SetDestinations(_ => [Destinations.AccessToken])`.
- Client registration is seeded and kill-switched through `IOpenIddictApplicationManager` in
  `src/Pegasus.Web/Mcp/AutomationClientRegistry.cs` — `EnsureRegisteredAsync` at `:41-71`,
  the per-request kill-switch `IsEnabledAsync` at `:77-100`, and the canonical descriptor at
  `:185-227` with `ClientType = Confidential` (`:192`) and `ConsentType = Implicit` (`:193`).
- Staff sign-in is entirely separate and cookie-based: `AddIdentity<PegasusIdentityUser,
  IdentityRole<Guid>>` at `Program.cs:263-274`,
  `CheckPasswordSignInAsync(user, Password, lockoutOnFailure: false)` at
  `src/Pegasus.Web/Pages/Account/SignIn.cshtml.cs:64` guarded by an `IsEnabled` check at `:62`,
  and the cookie's absolute-lifetime enforcement in `OnValidatePrincipal`
  (`Program.cs:403-455`) using the `pegasus:original-issued-at` claim declared at
  `Program.cs:38`.

**Parity-matrix row: `PAR-01`** (`docs/desktop/01-inventory-and-parity/parity-matrix.md:46`,
"13.1 Access and session", FRD-04). Its **API/data dependency** column already names this
ticket's output in so many words: "OpenIddict `/connect/token` password + refresh_token grants
for client `pegasus-desktop`; `StaffActorFactory.TryCreate`; `StaffSessionPolicy` lifetimes".
Its **Current behaviour evidence** column names the cookie facts above. The matrix holds 46
rows in total (`grep -c '^| PAR-' docs/desktop/01-inventory-and-parity/parity-matrix.md`).

## Findings

### Facts

- **F1 — plan 04 assumption A1 is now verified against the pinned package, not assumed.** The
  restored packages are in the local NuGet cache and their XML documentation names every API
  step 3 asks for:
  - `Microsoft.Extensions.DependencyInjection.OpenIddictServerBuilder.AllowPasswordFlow` —
    present (`~/.nuget/packages/openiddict.server/7.6.0/lib/net10.0/OpenIddict.Server.xml`).
  - `Microsoft.Extensions.DependencyInjection.OpenIddictServerDataProtectionExtensions.UseDataProtection(OpenIddictServerBuilder)`
    — present (`openiddict.server.dataprotection/7.6.0/.../OpenIddict.Server.DataProtection.xml`).
  - `…OpenIddictValidationDataProtectionExtensions.UseDataProtection(OpenIddictValidationBuilder)`
    — present (`openiddict.validation.dataprotection/7.6.0/…`).
  - `OpenIddict.Abstractions.OpenIddictExtensions.SetAccessTokenLifetime(ClaimsPrincipal, TimeSpan?)`
    and `SetRefreshTokenLifetime(ClaimsPrincipal, TimeSpan?)` — both present, and both also have
    `ClaimsIdentity` overloads (`openiddict.abstractions/7.6.0/…`).
  - Also present and needed by the handler: `OpenIddictRequest.Username`, `.Password`,
    `.GrantType`, `.ClientId`, and `OpenIddictExtensions.IsPasswordGrantType(OpenIddictRequest)`.

  Command used: ``grep -oE 'name="M:[^"]*\.(AllowPasswordFlow|UseDataProtection|SetAccessTokenLifetime|SetRefreshTokenLifetime)[^"]*"' <package>/lib/net10.0/*.xml``.
  **A1 therefore holds.** Step 3 remains worth running as a compile check — the XML documents
  members, it does not prove the overloads bind under this project's `LangVersion` — but it is
  now a confirmation, not an investigation.
- **F2 — `UseDataProtection()` needs no new `PackageReference`.**
  `src/Pegasus.Web/Pegasus.Web.csproj:40` references `OpenIddict.AspNetCore 7.6.0`, and
  `src/Pegasus.Web/packages.lock.json:65-77` shows that package already resolves
  `OpenIddict.Server.DataProtection 7.6.0` (`:75`) and
  `OpenIddict.Validation.DataProtection 7.6.0` (`:77`) transitively. The lock file needs no
  change, so the locked-restore CI lane is unaffected.
- **F3 — the Data Protection ring is already persisted, in production only.**
  `src/Pegasus.Web/Program.cs:172-176`: `AddDataProtection().SetApplicationName("Pegasus")
  .PersistKeysToAzureBlobStorage(new Uri(custodyServiceUri, "authentication-ring/keys.xml"),
  credential)`. It sits inside a production-only branch, so `UseDataProtection()` gives
  restart-surviving tokens in production and **process-lifetime** keys elsewhere — which is what
  the local Test/UAT stack and the integration tests will see. The step 5 evidence run must
  therefore not claim "survives restart" from a test that never restarts.
- **F4 — the OpenIddict tables carry `DENY DELETE` for both runtime roles.**
  `src/Pegasus.Infrastructure/Persistence/Migrations/20260803151159_AutomationActorOpenIddict.cs:195-201`
  grants `SELECT, INSERT, UPDATE` on `OpenIddictApplications`, `OpenIddictAuthorizations` and
  `OpenIddictTokens` and `SELECT` on `OpenIddictScopes` to the Web role; `:202-208` then loops
  all four tables issuing `DENY DELETE` to **both** `pegasus_web_runtime_role` and
  `pegasus_worker_runtime_role`. The expectation is mirrored in
  `scripts/Invoke-AzureDatabaseBootstrap.ps1:103-104` and `:133-139`, and the migration id is
  in the pinned census at
  `tests/Pegasus.IntegrationTests/IntakePersistenceIntegrationTests.cs:64`. **Consequence:**
  every token operation this ticket adds must be an `INSERT` or an `UPDATE`. OpenIddict's
  background pruning (`PruneAsync`) deletes rows and would fail in production while passing on
  a LocalDB test database that has no such deny.
- **F5 — this ticket adds no table, so it adds no migration.** Both the desktop client
  registration and its tokens are rows in the four existing OpenIddict tables. The existing
  Web-role grants at `:195-201` already cover `INSERT`/`UPDATE`, so
  `pwsh ./scripts/Test-MigrationGrants.ps1` (99 lines; run by CI at
  `.github/workflows/ci.yml:58-60`) must stay green with no new entry.
- **F6 — the sign-in reason-code vocabulary already exists and must be reused, not invented.**
  `invalid_credentials` at `src/Pegasus.Web/Pages/Account/SignIn.cshtml.cs:70`;
  `invalid_security_stamp` at `Program.cs:415`; `absolute_session_expired` at `:439`;
  `disabled_or_missing_staff` at `:454`; the automation codes `automation_token_rejected` /
  `automation_access_denied` at `:494` and `automation_client_disabled` at
  `AutomationTokenEndpoint.cs:59`. `SecurityEventType` has exactly seven values
  (`src/Pegasus.Core/Identity/IdentityContracts.cs:98-107`) — `SignIn`, `PasswordChanged`,
  `Token`, `Client`, `RateLimited`, `SecurityStampChanged`, `SecurityConfigurationChanged` —
  and `ISecurityEventWriter` is at `:139-142`.
- **F7 — `StaffActorFactory.TryCreate` fixes the claim shape this ticket must emit.**
  `src/Pegasus.Core/Actors/StaffActorFactory.cs:8-39`: the subject must parse as a non-empty
  `Guid` (`:15`), **every** role name must parse as a defined `StaffRole` with
  `ignoreCase: false` (`:23-27`), and an actor with **zero** roles is rejected (`:32-35`). So the
  token must carry the staff id as a `Guid` string and one exactly-cased role claim per
  assigned role — `Administrator`, `Engineer` or `User`
  (`src/Pegasus.Core/Identity/IdentityContracts.cs:5-20`) — or [[GWY-021]] (plan handle
  `DSK-04-04`) cannot build an actor from it.
- **F8 — the lifetimes are constants that already exist.**
  `src/Pegasus.Core/Actors/StaffSessionPolicy.cs:9-13`: `IdleLifetime = 2h`,
  `AbsoluteLifetime = 8h`, `SignInAttemptsPerClientPerMinute = 10`,
  `SignInAttemptsGlobalPerMinute = 100`. The file's own summary calls the contract
  "transport-neutral", which is the licence to reuse it for a token session.
  `AutomationMcp.AccessTokenLifetime` is `TimeSpan.FromMinutes(10)`
  (`src/Pegasus.Web/Mcp/AutomationMcp.cs:35`) — the same 10 minutes plan 04 decision 2 wants for
  the desktop access token, from "the same constant family".
- **F9 — the security-stamp guarantee the desktop must match is already set to zero interval.**
  `Program.cs:351-353` configures `SecurityStampValidatorOptions.ValidationInterval =
  TimeSpan.Zero`, so the cookie re-checks `IsEnabled` on every request (`:443-455`). Plan 04
  decision 3 requires the token path to give the same guarantee. This ticket owns only the
  refresh-time half of it; the per-request half is [[GWY-021]]'s.
- **F10 — disabling an account already rotates the security stamp.**
  `src/Pegasus.Infrastructure/Persistence/EfStaffAccountAdministration.cs:123`
  `UpdateSecurityStampAsync(user)` on disable, and `:208` on a role change, each followed by a
  `SecurityEventType.SecurityStampChanged` event (`:137`, `:224`). That is the signal step 12's
  refresh-time stamp comparison reads.
- **F11 — `AcceptAnonymousClients()` exists but must not be called.** The 7.6.0 server builder
  exposes `Microsoft.Extensions.DependencyInjection.OpenIddictServerBuilder.AcceptAnonymousClients`.
  A **public** client still sends `client_id`, so the desktop flow does not need it; enabling it
  would let a token request arrive with no client identity at all, weakening the Automation
  client's confidential requirement on the shared `/connect/token`.
- **F12 — the test scaffolding this ticket needs already exists.** `LocalDbTestDatabase`
  (used at `tests/Pegasus.IntegrationTests/IntakePersistenceIntegrationTests.cs:23`),
  `ConfiguredWebApplicationFactory` (used at
  `tests/Pegasus.IntegrationTests/ReadinessEndpointTests.cs:38`), the
  `[Trait("Category", "SqlServer")]` shard trait and the user-seeding block at
  `tests/Pegasus.IntegrationTests/StaffSignInSecurityTests.cs:11`, `:20-52`, and the gated-host
  pattern `factory.WithWebHostBuilder(...)` + `builder.UseSetting("Features:…", "true")` at
  `tests/Pegasus.IntegrationTests/AutomationMcpTestSupport.cs:32-42` with a token-request helper
  at `:44-62`. Nothing new is needed to write the five facts.

### Assumptions

- **A-GWY019-1 — `OpenIddictConstants.ClientTypes.Public` and
  `Permissions.GrantTypes.Password` exist under these exact names.** Their siblings are already
  compiled in this repository — `ClientTypes.Confidential` at
  `AutomationClientRegistry.cs:192`, `Permissions.GrantTypes.ClientCredentials` at `:203`,
  `Permissions.GrantTypes.RefreshToken` at `:213` — so the nested classes exist; the specific
  members are undocumented constant fields and do not appear in the XML documentation.
  *Confirmed by*: step 3's compile check. *Breaks if wrong*: the descriptor uses the literal
  string values instead, which is a two-line change and no design change.
- **A-GWY019-2 — one `/connect/token` route can serve both clients.** The endpoint is mapped
  once (`AutomationMcpExtensions.cs:134-136`) with `.AllowAnonymous()` and
  `.RequireRateLimiting(AutomationMcp.RateLimitPolicy)`. Step 8 dispatches inside the handler on
  grant type and client id. *Confirmed by*: the desktop facts and the unchanged `Automation`
  suite both passing against a single mapping. *Breaks if wrong*: a second route
  (`/connect/desktop-token`) is needed, which changes the endpoint map's Session row
  (`endpoint-map.md:31`) and the desktop client — a contract change, so it must be caught here,
  not later.
- **A-GWY019-3 — the desktop client's rate-limit policy is the automation one until
  [[GWY-020]] changes it.** The single mapping carries
  `.RequireRateLimiting(AutomationMcp.RateLimitPolicy)` — 120 requests per client per minute
  (`AutomationMcp.cs:37`), keyed on remote IP (`Program.cs:309-318`) — not the `StaffSignIn`
  policy's 10 per minute. *Confirmed by*: reading the mapping after step 8.
  *Breaks if wrong*: nothing here; applying the sign-in limiters to password grants is
  [[GWY-020]]'s (plan handle `DSK-04-03`) entire subject and this ticket must not pre-empt it.
- **A-GWY019-4 — moving `OriginalIssueClaim` out of `Program.cs:38` does not disturb the cookie
  path.** It is a top-level `const string` used by the cookie's `OnSigningIn` (`:381-385`) and
  `OnValidatePrincipal` (`:424`) and by `SecurityStampValidatorOptions.OnRefreshingPrincipal`
  (`:354-366`). *Confirmed by*: the existing cookie sign-in tests staying green.
  *Breaks if wrong*: keep the constant where it is and have the desktop file reference it
  instead — the rule the body states is one owner, not one particular owner.
- **A-GWY019-5 — extracting the OpenIddict block leaves the Automation client byte-identical in
  behaviour.** The extraction moves `:33-72` wholesale and has each feature contribute only its
  own flows, scopes, resources and registration. *Confirmed by*:
  `dotnet test --filter "FullyQualifiedName~Automation"` green with no source change under
  `Mcp/*McpTools.cs`. *Breaks if wrong*: this is the single highest-risk change in the ticket
  and the Automation suite is the only thing standing between it and a silent regression of
  ADR-0011/0026/0027 behaviour.

## Execution placement

The six-question cloud-justification test from
`docs/desktop/00-governance-and-workflow/README.md` § 3, answered.

| Question | Answer | Evidence |
| --- | --- | --- |
| Shared authority — must several users see and update the same state? | **yes** — lands in the existing `Pegasus.Web` Container App | Identity is one account store shared by every operator (`Program.cs:263-274`, `AddEntityFrameworkStores<PegasusDbContext>`), and a token session must be the same authority the cookie session already is. L-01 places that authority in `Pegasus.Web` evolved in place; no new deployment unit |
| Unattended execution — must it run with every desktop closed? | **no** | Token issuance is request-driven. The one machine-to-machine flow on this endpoint — the Automation client's client-credentials grant (`AutomationMcpExtensions.cs:41`) — already exists under ADR-0011/0026 and is explicitly unchanged by this ticket |
| Protected credentials — long-lived secret that must not sit on workstations? | **yes** — lands on the same Container App plus the **already-provisioned** Data Protection ring | The token signing and encryption material and the Identity password hashes must never reach a workstation. `Program.cs:172-176` already persists the ring to `authentication-ring/keys.xml` in blob storage, so step 5 consumes an existing resource: **no new Azure resource, no new secret, no ⚠ Azure write** |
| Public callback — must an external service call a stable public endpoint? | **no** | Plan 04 § 3 decision 1: "No authorization-code/browser round trip: the login screen stays native (§8.1)." The client is registered with no redirect URI; the only redirect surface on this endpoint is the Automation connector's (`AutomationClientRegistry.cs:215-219`), untouched |
| Central enforcement — revocation, permissions, audit, invariant independent of the client? | **yes** — lands in the gateway | `IsEnabled` (`SignIn.cshtml.cs:62`), the security stamp (`EfStaffAccountAdministration.cs:123`, `:208`), role assignment and the `StaffAuthorization` matrix (`StaffAuthorization.cs:29-58`, fail-closed `_ => false` at `:56`) are all server-side and must stay so. This ticket's refresh grant re-checks `IsEnabled` and the stamp; [[GWY-021]] does the per-request half and [[GWY-022]] (plan handle `DSK-04-05`) the revocation half |
| Measured operational advantage — measured evidence central is materially better? | **no** | No measurement is offered or needed. The placement follows from questions 1, 3 and 5; claiming a measured advantage without one is the dishonesty this section exists to catch |

Three "yes" answers, and every one lands on infrastructure that already exists: the
`Pegasus.Web` Container App (L-01) and the blob-persisted Data Protection ring
(`Program.cs:172-176`). Consistent with the ticket's Guardrails, "Azure: no write."

## Implications

1. **The extraction in step 4 is the ticket's real risk, not the password grant.** The password
   grant is ~60 lines of well-trodden OpenIddict; moving a working `AddOpenIddict()` block out
   from behind a feature gate is where ADR-0011/0026/0027 behaviour can silently change. That is
   why the `Automation` filter run appears twice in the body (steps 4 and 5) and why the plan
   sequences the extraction before anything desktop-specific is added.
2. **A1 is answered, so step 3 becomes a confirmation.** F1 removes the ticket's largest
   unknown before implementation starts. The plan keeps step 3 — a compile check proves binding
   in a way an XML doc cannot — but the *stop and record an open question* branch is now
   unlikely to fire, and the plan says so rather than leaving the implementer expecting trouble.
3. **`DENY DELETE` shapes the design of every later revocation ticket.** F4 means "revocation"
   on this board is always a status update. [[GWY-022]] owns revocation; this ticket must not
   introduce any code path that deletes an OpenIddict row, and must not enable OpenIddict's
   background pruning.
4. **The claim shape is not free.** F7 makes the destination settings load-bearing: subject,
   roles and `pegasus:original-issued-at` must reach **both** the access token and the refresh
   token, unlike `AutomationPrincipal.Create`'s `SetDestinations(_ => [Destinations.AccessToken])`
   (`AutomationTokenEndpoint.cs:121`). Copying the automation principal wholesale would produce
   a refresh token that cannot carry the absolute cap forward — the exact defect step 12 tests
   for.
5. **The `MustChangePassword` question is decided by the body, not by me.** Step 10 chooses
   "issue the token, enforce the block per request" and orders the reading recorded under a
   `## Decision` heading in the plan. The plan does that. No `open-questions` document is
   created: the body took the default and named where to record it, and plan 04 § 3 decision 3
   independently places `password-change-required` in the per-request path.
6. **Production and test differ in one way that matters.** F3: the persisted ring is inside a
   production-only branch. Any claim that sessions survive a restart is a production claim; the
   test evidence proves only that Data Protection is in use and the Automation client is
   unaffected. The plan says so explicitly so the post-implementation report does not overclaim.

## Open questions

- None open. Two items are settled elsewhere and recorded in the plan's *Risks / open questions*
  rather than as blocking questions: the `MustChangePassword` reading is decided by the body's
  step 10 and recorded under `## Decision` in the plan (implication 5); the rate-limit policy on
  password grants is [[GWY-020]]'s (plan handle `DSK-04-03`) whole subject and is a scope
  boundary, not a question.
- The body's step 3 keeps a conditional instruction: *if* an OpenIddict 7.6.0 API turns out to be
  absent at compile time, stop and record it in `open-questions`. F1 makes that unlikely but the
  instruction stands unchanged; it is a runtime branch for the implementer, not a question open
  today.
