# Plan — GWY-019: DSK-04-02 · OpenIddict client `pegasus-desktop`: password + refresh grants, Data Protection, staff lifetimes

## Governing documents

- No canonical document is linked yet. Retain the ticket's existing `docs_todo` state; do not invent or link a proposed desktop ADR.

## Chosen approach

Register a first-party **public** OpenIddict client `pegasus-desktop` in `Pegasus.Web` with the resource-owner password and refresh-token grants, replace the OpenIddict server's ephemeral keys with `UseDataProtection()`, and issue staff tokens with a 10-minute access lifetime, a rolling 2-hour refresh lifetime and an 8-hour absolute cap carried in a `pegasus:original-issued-at` claim.

## Routing and constraints

- Future owner: `pegasus-gateway-dev`; tests: `pegasus-test-engineer`; independent review: `pegasus-desktop-reviewer`.
- Use `dotnet-webapi`, `optimizing-ef-core-queries` where the ticket changes a query, and `run-tests` for the actual runner profile. The project decision overrides generic “service per endpoint” advice: route handlers translate to existing `Pegasus.Core` ports; no second policy/service layer is introduced.
- The shared EPIC context binds this to versioned `/api/v1` route groups in the existing `Pegasus.Web`, the existing rate-limiter mechanism, an OpenAPI snapshot, and no Azure write.

- Microsoft Learn (fetched 2026-08-24): [Data Protection key storage providers](https://learn.microsoft.com/aspnet/core/security/data-protection/implementation/key-storage-providers?view=aspnetcore-10.0) confirms explicit key persistence also needs at-rest protection. The project decision overrides generic Azure setup: consume the already-provisioned ring; do not create or change Azure resources.


## Ordered implementation steps

1. **Orient.** Read `docs/desktop/04-auth-session-update-and-startup/README.md` § 3 decisions 1, 2 and 4, § 4 exit gate and § 7 risks, and the § 5 row `DSK-04-02`. Then call Kanmer `get_doc_gates` for this ticket's board id, `take_ticket`, and load the skills in the order given under Routing.
2. **Read the existing composition before changing it**: `src/Pegasus.Web/Mcp/AutomationMcpExtensions.cs`, `AutomationMcp.cs`, `AutomationTokenEndpoint.cs`, `AutomationClientRegistry.cs`. The whole OpenIddict server is composed inside `AddPegasusAutomationMcp`, which `src/Pegasus.Web/Program.cs:625-628` calls only when `Features:AutomationMcp` is on, and `/connect/token` is mapped only by `MapPegasusAutomationMcp` (`Program.cs:961-964`). The desktop token flow must not depend on that gate.
3. **Confirm the OpenIddict 7.6.0 API (plan assumption A1)** before writing the handler. Verify that `OpenIddictServerBuilder.AllowPasswordFlow()`, `OpenIddictServerBuilder.UseDataProtection()`, `OpenIddictValidationBuilder.UseDataProtection()` and the per-principal `ClaimsPrincipal.SetAccessTokenLifetime` / `SetRefreshTokenLifetime` extensions exist on the pinned `OpenIddict.AspNetCore 7.6.0` (`src/Pegasus.Web/packages.lock.json:65-77`) by compiling a call to each. If any is absent, **stop**: record it in the ticket's `open-questions/` document and raise it, do not substitute another flow. OpenIddict's own site (`documentation.openiddict.com`, "Choosing the right flow") is the API authority quoted in plan § 2; use `microsoft_docs_search` only for ASP.NET Core Data Protection key-ring semantics.
4. **Extract the shared OpenIddict composition.** Create `src/Pegasus.Web/Desktop/DesktopSessionExtensions.cs` and move the `AddOpenIddict().AddCore(...)`/`.AddServer(...)`/`.AddValidation(...)` block out of `AddPegasusAutomationMcp` into one `AddPegasusOpenIddict(...)` call that runs when **either** `Features:AutomationMcp` or `Features:DesktopGateway` (the gate [[DSK-03-02]] introduces) is enabled; each feature then contributes only its own flows, scopes, resources and client registration. Done when `dotnet build src/Pegasus.Web/Pegasus.Web.csproj` succeeds and `dotnet test tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --filter "FullyQualifiedName~Automation"` is still green with no source change in `Mcp/*McpTools.cs`.
5. **Switch token protection to Data Protection.** In the extracted server builder replace `AddEphemeralEncryptionKey()` and `AddEphemeralSigningKey()` with `server.UseDataProtection()`, and add `validation.UseDataProtection()` in `AddValidation`. The ring is already persisted to blob storage in production (`Program.cs:172-176`), so no new Azure resource and no new secret. Re-run the Automation filter from step 4 — that run is the evidence for plan assumption A2.
6. **Add the desktop session constants.** Create `src/Pegasus.Web/Desktop/DesktopSession.cs` with `ClientId = "pegasus-desktop"`, `Scope = "pegasus.desktop"`, `TokenEndpointPath` reusing `AutomationMcp.TokenEndpointPath` (`/connect/token`), `AccessTokenLifetime = TimeSpan.FromMinutes(10)`, `RefreshTokenLifetime = StaffSessionPolicy.IdleLifetime`, `AbsoluteSessionLifetime = StaffSessionPolicy.AbsoluteLifetime`, and `OriginalIssueClaim = "pegasus:original-issued-at"` (the same literal as `Program.cs:38` — move it to this one owner and have `Program.cs` reference it, do not create a second copy). If [[DSK-03-02]] has already created a folder for the desktop gateway surface, put the file there instead and say so in the plan document.
7. **Register the public client.** Add `src/Pegasus.Web/Desktop/DesktopClientRegistry.cs` modelled on `AutomationClientRegistry.EnsureRegisteredAsync` (`AutomationClientRegistry.cs:39-70`): `ClientId = pegasus-desktop`, **`ClientType = Public`, no secret**, permissions `Endpoints.Token`, `GrantTypes.Password`, `GrantTypes.RefreshToken`, `Scopes.Prefix + "pegasus.desktop"` and `Permissions.Scopes.Prefix + "offline_access"`; idempotent, preserving an administrator-set disabled state exactly as the Automation registry does. Register `pegasus.desktop` as an OpenIddict scope alongside `AutomationMcp.Scopes`.
8. **Split the token endpoint by grant.** Add `src/Pegasus.Web/Desktop/DesktopTokenEndpoint.cs` and make the single `POST /connect/token` mapping dispatch on grant type and client id: client-credentials / authorization-code / refresh for the Automation client keep going to `AutomationTokenEndpoint.ExchangeAsync` unchanged; `grant_type=password` and refresh for `pegasus-desktop` go to the new handler. Map the endpoint from the shared composition so it exists when `Features:DesktopGateway` is on and `Features:AutomationMcp` is off.
9. **Implement the password grant.** Resolve the user with `UserManager<PegasusIdentityUser>.FindByNameAsync(request.Username.Trim())`; when the user is null or `!user.IsEnabled` return `Errors.InvalidGrant` with `error_description` code `account-disabled`; verify the password with `SignInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: false)` — the same call and the same `lockoutOnFailure: false` as `Pages/Account/SignIn.cshtml.cs:64`, because ADR-0013 clause 12 keeps login protection transient. Build the principal: `Claims.Subject` = `user.Id.ToString("D")`, one `Claims.Role` per `UserManager.GetRolesAsync(user)` value (so [[DSK-04-04]] can feed `StaffActorFactory.TryCreate`), the granted scopes, and `pegasus:original-issued-at` = `TimeProvider.GetUtcNow().ToUnixTimeSeconds()`; set destinations so subject, roles and the original-issue claim reach **both** the access token and the refresh token.
10. **Issue a token even when `MustChangePassword` is true**, and record that reading under a `## Decision` heading in the ticket plan document: plan § 3 decision 3 routes the desktop to the change-password flow, which itself needs a token, so the block belongs in the per-request middleware of [[DSK-04-04]] (`urn:pegasus:problem:password-change-required`), not here.
11. **Set lifetimes per principal, never server-wide.** Call `principal.SetAccessTokenLifetime(DesktopSession.AccessTokenLifetime)` and `principal.SetRefreshTokenLifetime(DesktopSession.RefreshTokenLifetime)` on the desktop principal only. Do **not** change `SetAccessTokenLifetime`/`SetRefreshTokenLifetime` on the server builder and do **not** remove `DisableSlidingRefreshTokenExpiration()` — that switch is server-wide and would change the fortnightly MCP connector cap governed by ADR-0027.
12. **Implement the refresh grant.** Authenticate the incoming refresh principal via `httpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)`; reload the user and reject with `invalid_grant` when the account is missing, `!IsEnabled`, or the principal's security stamp no longer matches `user.SecurityStamp`; copy `pegasus:original-issued-at` forward unchanged; reject with `invalid_grant` and `error_description` code `absolute-session-expired` when `now - originalIssuedAt >= StaffSessionPolicy.AbsoluteLifetime`. OpenIddict's rolling refresh re-issues the refresh token on every exchange — assert that in the test, do not implement rotation by hand.
13. **Write security events.** Through `ISecurityEventWriter` (`src/Pegasus.Core/Identity/IdentityContracts.cs:139`) append `SecurityEventType.SignIn` / `Succeeded` on a successful password grant and `SecurityEventType.Token` / `Denied` with reason codes `invalid_credentials`, `account_disabled`, `absolute_session_expired` on the refusals, mirroring the reason-code vocabulary already used at `src/Pegasus.Web/Program.cs:456-470`.
14. **Test.** Add `tests/Pegasus.IntegrationTests/DesktopTokenIssuanceTests.cs` with `[Trait("Category", "SqlServer")]`, using `LocalDbTestDatabase` (`IntakePersistenceIntegrationTests.cs:415`) and `ConfiguredWebApplicationFactory` (`ReadinessEndpointTests.cs:297`) exactly as `StaffSignInSecurityTests.cs:20-52` does. Cases, each its own `[Fact]`: password grant returns `access_token` + `refresh_token`; refresh returns a **different** refresh token; refresh past the 8-hour cap returns `invalid_grant`; disabled account returns `invalid_grant`; the Automation client-credentials grant is unchanged. Run `dotnet test tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --filter "FullyQualifiedName~DesktopTokenIssuance|FullyQualifiedName~Automation"`.

## Acceptance conditions

- [ ] `POST /connect/token` with `grant_type=password&client_id=pegasus-desktop&scope=pegasus.desktop offline_access` and a valid enabled staff account returns a 10-minute access token and a refresh token.
- [ ] The access token carries the subject as the staff `Guid`, one role claim per assigned `StaffRole`, and `pegasus:original-issued-at`.
- [ ] Refreshing returns a new refresh token (rolling) and refuses once 8 hours have passed since the original issue.
- [ ] The Automation MCP client's grants, scopes, 10-minute access token and 14-day refresh cap are unchanged, and every existing `Automation*` integration test is green.
- [ ] OpenIddict tokens are protected by the persisted Data Protection ring; no ephemeral key call remains in the server builder.
- [ ] No client secret exists anywhere for `pegasus-desktop`; the registration is `Public`.

## Verification

- [ ] `dotnet build src/Pegasus.Web/Pegasus.Web.csproj` — expected: succeeds with zero warnings (`Directory.Build.props` sets `TreatWarningsAsErrors=true`).
- [ ] `dotnet test tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --filter "FullyQualifiedName~DesktopTokenIssuance"` — expected: all new facts pass.
- [ ] `dotnet test tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --filter "FullyQualifiedName~Automation"` — expected: the pre-existing Automation suite passes unchanged (evidence for plan assumption A2).
- [ ] `pwsh ./scripts/Test-MigrationGrants.ps1` — expected: exits 0 (no new table is added by this ticket; the check must stay green).

## Risks and boundaries

- **Azure**: no write. Data Protection reuses the already-provisioned blob ring; no app setting, no new resource.
- **Scope boundary**: may touch `src/Pegasus.Web` (new `Desktop/` folder and the `Mcp/` extraction), `src/Pegasus.Core/Actors/StaffSessionPolicy.cs` (read only), `tests/Pegasus.IntegrationTests`. Must not touch `src/Pegasus.Worker`, `infra/`, the Razor cookie pipeline's behaviour, or any `Mcp/*McpTools.cs`.
- **Traps**: (a) the OpenIddict server is composed **inside** the `Features:AutomationMcp` gate today — extract it or the desktop flow silently disappears in any deployment with MCP off; (b) `DisableSlidingRefreshTokenExpiration()` is server-wide — implement the staff idle/absolute pair in the handler, never by flipping it; (c) ephemeral keys invalidate every session on restart — the Data Protection switch is not optional; (d) the OpenIddict tables carry **DENY DELETE** for both runtime roles (`20260803151159_AutomationActorOpenIddict.cs:202-208`), so any cleanup must be a status update, never a row delete.
- **Open question**: the plan does not state whether a `MustChangePassword` account may obtain a token. Step 10 chooses "yes, with the block enforced per request" and records the reading; if review disagrees, the change is one branch in `DesktopTokenEndpoint`.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.

## Implementation finding — 2026-08-27

- The extracted composition compiles, but the first exact Automation ingress run fails during OpenIddict startup with: `InvalidOperationException: At least one encryption key must be registered in the OpenIddict server options.`
- The pinned OpenIddict 7.6.0 XML and source require at least one encryption credential and one asymmetric signing credential unconditionally during server-options validation (OpenIddictServerConfiguration, validation of `EncryptionCredentials` and `SigningCredentials`). The Data Protection integration replaces formats for access, authorization-code, refresh, device, user and request tokens; it does not remove the JWT identity-token key requirement.
- The current Automation composition enables authorization-code + PKCE, so retaining that existing capability requires a signing/encryption credential. The repository contains no approved durable certificate/configuration source for this branch. Development or ephemeral credentials would contradict this ticket's acceptance and the repository's security boundary; no cloud, credential, or external-environment write is authorized.
- Do not proceed to desktop issuance tests or PR while the existing Automation path is broken. The smallest unblock is an approved in-repository/runtime credential source for the retained authorization-code capability, or an explicit product decision to remove/defer that capability; neither is being guessed here.

## Decision — 2026-08-30

The desktop password-grant token is issued even when `MustChangePassword` is true. The later desktop request gate owns the `password-change-required` response; withholding the session here would prevent that flow from being reached.

## Implementation finding resolution — 2026-08-30

The 2026-08-27 startup finding is resolved in-repository. OpenIddict 7.6 still validates signing and encryption credentials when the retained Automation authorization-code + PKCE capability is composed, even when token payload protection uses Data Protection. The shared composition now registers user-scoped Development signing/encryption certificates with the settled publisher subject `CN=Collision Engineers` for local/test hosts. Non-Development hosts fail closed unless `OpenIddict:CertificatePath` is supplied; the loaded certificate subject must exactly match `CN=Collision Engineers`, and its private key is held outside the repository.

This does not create or change cloud state, add a certificate or secret to the repository, or alter the persisted production Data Protection ring. The package identity remains `CollisionEngineers.Pegasus` with Publisher `CN=Collision Engineers`; the same subject is the only accepted runtime token-certificate subject. Production certificate issuance/trust rollout remains owned by the release/distribution work and is not claimed by this ticket.

Evidence: `dotnet build tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --no-restore` succeeded with 0 warnings and 0 errors; `DesktopTokenIssuance` passed 3/3; `Automation` passed 33/33; `pwsh ./scripts/Test-MigrationGrants.ps1` checked 74 migration files with exit 0; `rg` found no `AddEphemeralEncryptionKey` or `AddEphemeralSigningKey` in `src/Pegasus.Web`.

## Simplification pass — 2026-08-30

- Reuse: the shared `AutomationMcp.TokenEndpointPath`, existing OpenIddict application manager, Identity `UserManager`/ `SignInManager`, `StaffSessionPolicy`, rate limiter, security-event writer, and existing test factory were reused. No new business-policy or persistence layer was introduced.
- Simplification: Automation remains in its existing handler; only the OpenIddict composition and token route were consolidated because Desktop needs the same endpoint. Desktop-specific grants, registration, lifetimes, claims, and account checks remain in the Desktop folder.
- Security: no client secret, cloud write, certificate file, or private key was added to the repository. Missing/mismatched non-Development certificate configuration fails closed.
- Test support: the claim probe is test-only middleware in `DesktopTokenIssuanceTests.cs`, needed to validate the actual bearer validation pipeline; it is not an application endpoint.
- Disposition: no behaviour-preserving simplification remained after the pass. The earlier invalid `Destinations.RefreshToken` assumption was removed because OpenIddict has no such destination; refresh principal preservation is proved by the rolling refresh and absolute-cap tests.

## Independent review resolution — 2026-08-30

The first independent review identified three merge-blocking combined-composition defects. The branch now resolves them as follows:

- Removed the shared DisableSlidingRefreshTokenExpiration() switch. Desktop refresh tokens now use OpenIddict's rolling behavior. Automation retains its 14-day absolute cap through an internal original-issue claim, per-principal remaining refresh lifetime, and refresh-time age validation; this supersedes the earlier step 11 instruction to retain the global switch.
- Desktop password grants now share the existing global sign-in limiter and use the staff 10-attempts-per-client-per-minute policy on the token endpoint, while Automation remains at 120 requests per client per minute. The request marker is an in-memory pipeline detail, not a second policy vocabulary.
- Added combined-mode integration facts for Desktop rolling refresh, Automation client-credentials issuance, the public no-secret registration, security-stamp invalidation, and rejection of the Desktop client using the Automation grant.

Post-fix validation: Web Release build and IntegrationTests Release build both passed with 0 warnings and 0 errors; DesktopTokenIssuance passed 6/6; Automation passed 35/35; the prior full integration run passed 1031/1031 executed tests with 16 corpus-dependent skips; migration-grant validation passed for 74 migration files; git diff --check passed. A fresh independent review is required for this changed branch before PR merge.

## Final simplification disposition — 2026-08-30

The independent simplification pass found and the branch applied three behavior-preserving cleanups: the shared OpenIddict composition is now the sole AddMemoryCache registration; DesktopClientRegistry is registered only when the Desktop gate is enabled; and the two deterministic integration suites reuse one MutableTimeProvider in AutomationMcpTestSupport. No other abstraction, duplication, or scope issue remained. The pass is complete.

Final post-cleanup validation: Web Release build and IntegrationTests Release build passed with 0 warnings and 0 errors; DesktopTokenIssuance plus Automation passed 42/42 with 0 failed and 0 skipped; migration-grant validation passed for 74 migration files; git diff --check passed. Fresh independent review of the exact committed head is still required before PR merge.
