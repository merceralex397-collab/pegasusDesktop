# Plan — GWY-021: DSK-04-04 · Bearer authentication for `/api/v1`: claims to actor, per-request enabled and stamp check

## Governing documents

- `docs/frd/frd-04-parties-accounts-and-access.md`

## Chosen approach

Authenticate every `/api/v1` request with the desktop bearer token: translate its claims through `StaffActorFactory.TryCreate` into an `ActionActor`, re-check `IsEnabled` and the Identity security stamp on **every** request, and return `urn:pegasus:problem:password-change-required` for a user whose `MustChangePassword` flag is set.

## Routing and constraints

- Future owner: `pegasus-gateway-dev`; tests: `pegasus-test-engineer`; independent review: `pegasus-desktop-reviewer`.
- Use `dotnet-webapi`, `optimizing-ef-core-queries` where the ticket changes a query, and `run-tests` for the actual runner profile. The project decision overrides generic “service per endpoint” advice: route handlers translate to existing `Pegasus.Core` ports; no second policy/service layer is introduced.
- The shared EPIC context binds this to versioned `/api/v1` route groups in the existing `Pegasus.Web`, the existing rate-limiter mechanism, an OpenAPI snapshot, and no Azure write.

- Microsoft Learn (fetched 2026-08-24): [ASP.NET Core OpenAPI support](https://learn.microsoft.com/aspnet/core/fundamentals/openapi/overview?view=aspnetcore-10.0) confirms first-party OpenAPI generation. Use the repository’s planned committed snapshot and contract-test flow rather than adding a parallel API documentation path.


## Ordered implementation steps

1. **Orient.** Read `docs/desktop/04-auth-session-update-and-startup/README.md` § 3 decision 3 and the session failure matrix, and `docs/desktop/03-gateway-api-and-data/README.md:167` for the problem-type catalogue. Call Kanmer `get_doc_gates` for this ticket's board id, `take_ticket`, then load the skills under Routing.
2. **Read the guarantee you must match**: `src/Pegasus.Web/Program.cs:351-353` and `:404-455`. Write down in the ticket `research` document the four rejection reasons the cookie path already implements — invalid security stamp, absolute session expiry, disabled/missing user, must-change-password — because the bearer path owes the same four.
3. **Add the bearer scheme for `/api/v1`.** Register an OpenIddict validation authentication scheme (already composed by [[DSK-04-02]]) as the scheme the `/api/v1` group authenticates with, and add an authorization policy `DesktopApi` in `src/Pegasus.Web/Desktop/` that requires an authenticated user, the `pegasus.desktop` scope, and **rejects** a principal whose subject is the Automation client id — an Automation token must never reach `/api/v1`.
4. **Translate claims to the actor.** Add `src/Pegasus.Web/Desktop/DesktopActorResolver.cs` modelled on `src/Pegasus.Web/Mcp/AutomationActorResolver.cs`: read `Claims.Subject` and every `Claims.Role` value, call `StaffActorFactory.TryCreate(subject, roles, out var actor)`, and on `false` return the `urn:pegasus:problem:not-authorized` problem. Do not construct `ActionActor` any other way — `StaffActorFactory` is the single claims→actor seam.
5. **Re-check the account on every request.** In the same resolver, load the user with `UserManager<PegasusIdentityUser>.FindByIdAsync(subject)` and reject when: the user is missing or `!user.IsEnabled` → `urn:pegasus:problem:account-disabled` (HTTP 401); the principal's security-stamp claim differs from `user.SecurityStamp` → `urn:pegasus:problem:account-disabled` with reason code `invalid_security_stamp`. This is one indexed read per request, which plan § 3 decision 3 accepts explicitly for ten users — do not add a cache to "optimise" it, and do not raise `ValidationInterval` above zero.
6. **Enforce the absolute session cap.** Reject with `urn:pegasus:problem:account-disabled` reason `absolute_session_expired` when `now - pegasus:original-issued-at >= StaffSessionPolicy.AbsoluteLifetime`, mirroring `Program.cs:427-441`. The refresh path in [[DSK-04-02]] enforces the same bound; both are required because an access token issued just before the cap would otherwise outlive it.
7. **Return the password-change problem instead of a redirect.** When `user.MustChangePassword` is true, return `urn:pegasus:problem:password-change-required` (HTTP 403) for every `/api/v1` endpoint **except** the password-change endpoint that [[DSK-03-15]] exposes over the existing `IStaffPasswordChangeStore` (`src/Pegasus.Core/Identity/StaffPasswordChange.cs:16-20`) and the session/logout endpoints. Keep the allow-list in one named array beside the middleware, the way `Program.cs:884-890` keeps the Razor allow-list.
8. **Expose the actor to endpoints.** Store the resolved `ActionActor` in `HttpContext.Items` under a single named key owned by `src/Pegasus.Web/Desktop/`, and provide the endpoint filter that [[DSK-03-03]] consumes to call `StaffAuthorization.Require(actor, right)`. Do not duplicate the right-checking logic — the fail-closed switch in `src/Pegasus.Core/Identity/StaffAuthorization.cs` stays the only owner.
9. **Map failures to problems, not exceptions.** Reuse the mapping [[DSK-03-02]] ports from `src/Pegasus.Web/Mcp/AutomationMcpErrors.cs:17-70`; every problem body carries `correlationId` and never a payload dump (proposal §17.1, §18.1).
10. **Test.** Add `tests/Pegasus.IntegrationTests/DesktopApiAuthenticationTests.cs`, `[Trait("Category", "SqlServer")]`, using `LocalDbTestDatabase` and `ConfiguredWebApplicationFactory` as `StaffSignInSecurityTests.cs:20-52` does, with `Features:DesktopGateway=true`. Facts: valid token reaches an endpoint and the recorded actor is the staff `Guid` with the right roles; **disable the user through `UserManager` and the very next request with the same still-valid access token returns 401 `account-disabled`**; a changed security stamp is rejected; `MustChangePassword` returns 403 `password-change-required` on a normal endpoint but not on the password-change endpoint; an Automation MCP token is rejected on `/api/v1`; a token with an unknown role name is rejected.
11. **Add the architecture guard.** Extend `tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs` (or a sibling fact in that project) so a fact fails if any `/api/v1` endpoint is mapped without the `DesktopApi` policy — the group must be covered as a whole, not endpoint by endpoint.
12. **Run** `dotnet test tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --filter "FullyQualifiedName~DesktopApiAuthentication"` and `dotnet test tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj`, and record both in the post-implementation report.

## Acceptance conditions

- [ ] A valid desktop access token yields an `ActionActor` built by `StaffActorFactory.TryCreate` with the staff `Guid` and every assigned role.
- [ ] Disabling an account causes the **next** `/api/v1` request to fail with `urn:pegasus:problem:account-disabled` — not after the access token expires.
- [ ] A security-stamp change (password change, role change, disable) invalidates the token on the next request.
- [ ] `MustChangePassword` yields `urn:pegasus:problem:password-change-required` on every `/api/v1` endpoint except the password-change and session endpoints.
- [ ] An Automation MCP token is refused on `/api/v1`; a cookie is neither required nor accepted there.
- [ ] Every problem body carries `correlationId` and no payload content.

## Verification

- [ ] `dotnet test tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --filter "FullyQualifiedName~DesktopApiAuthentication"` — expected: all facts pass, including the disable-then-next-request fact.
- [ ] `dotnet test tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj` — expected: the new coverage fact passes and no dependency-direction fact regresses.
- [ ] `dotnet build src/Pegasus.Web/Pegasus.Web.csproj` — expected: succeeds with zero warnings.

## Risks and boundaries

- **Azure**: no write.
- **Scope boundary**: may touch `src/Pegasus.Web/Desktop/`, the `/api/v1` composition from [[DSK-03-02]], `tests/Pegasus.IntegrationTests`, `tests/Pegasus.ArchitectureTests`. Must not change the cookie pipeline's behaviour, `StaffAuthorization`, `StaffActorFactory`, or anything under `src/Pegasus.Worker`.
- **Scope overlap**: [[DSK-03-03]] owns the per-group `StaffAccessRight` endpoint filter and [[DSK-04-04]] owns the scheme, the actor resolution and the account re-check. Agree the seam in the plan document before coding so the two tickets do not both write the filter.
- **Traps**: (a) do not cache the `IsEnabled`/stamp read — the plan accepts one indexed read per request at ten users and a cache silently reintroduces the "disabled account keeps working" defect; (b) the `MustChangePassword` block must exempt the password-change endpoint or the operator is locked out with no route forward; (c) the Razor path *redirects*, the API path must *return a problem* — copying the redirect is a defect.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.

## Implementation and validation — 2026-08-30

- Final implementation head is `481d29c84d27efbdd78f0e23b13ad6ce2cc2a1d8`; documentation follow-up head is `20cd20bc29d035e1bb7689fdd71ce71394191cb` and changes only `docs/current-architecture.md`.
- The `/api/v1` group now requires the OpenIddict validation scheme plus the `pegasus.desktop` scope and a non-empty staff subject; the shared desktop resolver calls `StaffActorFactory.TryCreate`, performs an uncached user enabled/security-stamp check on every request, enforces the absolute session cap, returns the API password-change problem with the documented session/password-change exceptions, and stores the actor for endpoint authorization. GWY-003 remains the owner of per-right endpoint filtering.
- Parent validation: `git diff --check` passed; `dotnet restore ./Pegasus.slnx --locked-mode` passed; `dotnet build ./Pegasus.slnx --configuration Release --no-restore -nr:false -p:UseSharedCompilation=false` passed with 0 warnings and 0 errors; `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~DesktopApiAuthenticationTests"` passed 7/7; `dotnet test ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~EveryDesktopGatewayEndpointInheritsTheDesktopApiPolicy"` passed 1/1.
- Specialist also reported locked restore, Release build 0/0, focused auth 7/7, architecture guard 1/1, and diff check. No cloud/upstream/deployment write was made.
- The required independent review of the final code/documentation head is pending; no merge or release action has been taken.

## Final correction and parent validation — 2026-08-30

- Final branch head is `5cbe7033ad477895634fb8a8d769cc3943109b3c` (implementation `481d29c84d27efbdd78f0e23b13ad6ce2cc2a1d8`, documentation `20cd20bc29d035e1bb7689fdd71ce71394191cb`, production-route test/structural-guard correction `5cbe7033ad477895634fb8a8d769cc3943109b3c`).
- The authentication tests now call the real production `/api/v1/mail` route; the test-only startup filter and manual authentication/resolver endpoints were removed. The architecture fact parses the group composition structurally and verifies vehicle/mail slices are mapped through the protected group. The password-change endpoint remains owned by DSK-03-15 and is not yet composed; the current production-route test proves the required block while the resolver keeps the documented exemption for that future route.
- Parent rerun on the final head: `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~DesktopApiAuthenticationTests"` passed 7/7; `dotnet test ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~EveryDesktopGatewayEndpointInheritsTheDesktopApiPolicy"` passed 1/1; `git diff --check` passed. The prior parent locked restore and full Release solution build also passed 0 warnings/0 errors on the same production implementation; the final correction changes tests/architecture guard only.
