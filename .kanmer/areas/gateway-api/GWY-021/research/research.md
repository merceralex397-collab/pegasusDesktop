# Research — GWY-021: DSK-04-04 · Bearer authentication for `/api/v1`: claims to actor, per-request enabled and stamp check

## Question

Authenticate every `/api/v1` request with the desktop bearer token: translate its claims through `StaffActorFactory.TryCreate` into an `ActionActor`, re-check `IsEnabled` and the Identity security stamp on **every** request, and return `urn:pegasus:problem:password-change-required` for a user whose `MustChangePassword` flag is set.

## Evidence examined

- Plan row: `docs/desktop/04-auth-session-update-and-startup/README.md` § 5 — `DSK-04-04`
- Plan detail: same file § 3 decision 3 (revocation and per-request re-check), § 3 session failure matrix (rows "Account disabled", "Password change required")
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 8.3 Authorization, § 8.4 Session failure handling
- Repository evidence:
  - `src/Pegasus.Core/Actors/StaffActorFactory.cs:8-42` — `TryCreate(subjectId, roleNames, out ActionActor)`; fails closed on a non-`Guid` subject, an unknown role name, or an empty role set
  - `src/Pegasus.Core/Identity/StaffAuthorization.cs:23-60` — `IsAuthorized`/`Require` over the 12 `StaffAccessRight` values, fail-closed `switch`
  - `src/Pegasus.Web/Program.cs:351-353` — `SecurityStampValidatorOptions.ValidationInterval = TimeSpan.Zero`, the guarantee to match
  - `src/Pegasus.Web/Program.cs:404-455` — `OnValidatePrincipal`: absolute-lifetime check, `UserManager.GetUserAsync`, `!user.IsEnabled` → reject + `disabled_or_missing_staff` security event
  - `src/Pegasus.Web/Program.cs:875-899` — the `MustChangePassword` redirect middleware (check at `:891`) whose API equivalent is a problem response, not a redirect
  - `src/Pegasus.Web/Mcp/AutomationActorResolver.cs` and `src/Pegasus.Web/Mcp/AutomationMcpErrors.cs:17-70` — the existing claims → actor and exception → transport mapping to model
  - `src/Pegasus.Core/Identity/IdentityContracts.cs:139-145` — `ISecurityEventWriter`
  - `docs/desktop/03-gateway-api-and-data/README.md:167` — the problem-type catalogue: `urn:pegasus:problem:<slug>` with `not-authorized`, `password-change-required`, `account-disabled`, `client-unsupported`
  - `docs/frd/frd-04-parties-accounts-and-access.md` § Staff role access matrix — the authority for which role may do what
- Binding decisions:
  - **L-01** — the bearer scheme lives beside the cookie scheme in the same `Pegasus.Web` process
  - **L-04** — this ticket names its subagent, skills and MCP tools
  - **ADR-0102** (owed, `docs_todo`) — token session over the existing credential store; **ADR-0103** — gateway only, never direct database access from workstations
- Depends on: `DSK-04-02` — issues the token whose claims are read here; `DSK-03-02` — creates the `/api/v1` route group and the problem-details mapping this middleware plugs into

## Scope and constraints

Proposal §8.3 requires the gateway to enforce authorization independently of the client and states that "a disabled account must stop working without waiting for a desktop update". The cookie pipeline already achieves this with `SecurityStampValidatorOptions.ValidationInterval = TimeSpan.Zero` (`src/Pegasus.Web/Program.cs:351-353`), so a bearer path that trusts a 10-minute access token would be a *weaker* guarantee than the web app it replaces. Without this ticket the `/api/v1` group from [[DSK-03-02]] has no actor, and [[DSK-04-05]], [[DSK-04-06]] and [[DSK-04-10]] have nothing to build on.

- Future owner: `pegasus-gateway-dev`; tests: `pegasus-test-engineer`; independent review: `pegasus-desktop-reviewer`.
- Use `dotnet-webapi`, `optimizing-ef-core-queries` where the ticket changes a query, and `run-tests` for the actual runner profile. The project decision overrides generic “service per endpoint” advice: route handlers translate to existing `Pegasus.Core` ports; no second policy/service layer is introduced.
- The shared EPIC context binds this to versioned `/api/v1` route groups in the existing `Pegasus.Web`, the existing rate-limiter mechanism, an OpenAPI snapshot, and no Azure write.

- Microsoft Learn (fetched 2026-08-24): [ASP.NET Core OpenAPI support](https://learn.microsoft.com/aspnet/core/fundamentals/openapi/overview?view=aspnetcore-10.0) confirms first-party OpenAPI generation. Use the repository’s planned committed snapshot and contract-test flow rather than adding a parallel API documentation path.

- **Azure**: no write.
- **Scope boundary**: may touch `src/Pegasus.Web/Desktop/`, the `/api/v1` composition from [[DSK-03-02]], `tests/Pegasus.IntegrationTests`, `tests/Pegasus.ArchitectureTests`. Must not change the cookie pipeline's behaviour, `StaffAuthorization`, `StaffActorFactory`, or anything under `src/Pegasus.Worker`.
- **Scope overlap**: [[DSK-03-03]] owns the per-group `StaffAccessRight` endpoint filter and [[DSK-04-04]] owns the scheme, the actor resolution and the account re-check. Agree the seam in the plan document before coding so the two tickets do not both write the filter.
- **Traps**: (a) do not cache the `IsEnabled`/stamp read — the plan accepts one indexed read per request at ten users and a cache silently reintroduces the "disabled account keeps working" defect; (b) the `MustChangePassword` block must exempt the password-change endpoint or the operator is locked out with no route forward; (c) the Razor path *redirects*, the API path must *return a problem* — copying the redirect is a defect.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.

## Governing documents

- `docs/frd/frd-04-parties-accounts-and-access.md`

## Planning implication

Reuse the cited boundaries and revalidate the named sources against current `origin/dev` after the ticket is taken. Do not create a compatibility path, duplicate policy, or an unapproved external write.
