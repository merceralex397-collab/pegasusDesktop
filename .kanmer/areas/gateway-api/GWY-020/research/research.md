# Research — GWY-020: DSK-04-03 · Apply the `StaffSignIn` and global sign-in limiters to `/connect/token` password grants

## Question

Make a desktop password grant on `POST /connect/token` obey exactly the same throttling as a browser sign-in: the per-client `StaffSignIn` fixed-window policy (10/minute) plus the global 100/minute sign-in limiter, returning 429 with `Retry-After: 60` and writing the same `sign_in_rate_limited` `SecurityEvent`.

## Evidence examined

- Plan row: `docs/desktop/04-auth-session-update-and-startup/README.md` § 5 — `DSK-04-03`
- Plan detail: same file § 2 facts "Rate limiting", § 3 session failure matrix row "Rate limited", § 7 risk "Rate limiter scope"
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 8.4 Session failure handling; § 22.2 Security tests
- Repository evidence:
  - `src/Pegasus.Web/Program.cs:275-327` — `AddRateLimiter`, `OnRejected` (sets `Retry-After: 60`, chooses reason code `sign_in_rate_limited` / `automation_rate_limited` / `authentication_rate_limited` by path), policy `StaffSignIn` keyed on `context.Connection.RemoteIpAddress`, policy `AutomationMcp`
  - `src/Pegasus.Web/Program.cs:320-327` — the singleton global `FixedWindowRateLimiter` at `StaffSessionPolicy.SignInAttemptsGlobalPerMinute`
  - `src/Pegasus.Web/Program.cs:797-817` — the middleware that applies the global limiter to `POST /Account/SignIn` only
  - `src/Pegasus.Web/Program.cs:819` — `app.UseRateLimiter()`
  - `src/Pegasus.Core/Actors/StaffSessionPolicy.cs:11-12` — `SignInAttemptsPerClientPerMinute = 10`, `SignInAttemptsGlobalPerMinute = 100`
  - `src/Pegasus.Web/Mcp/AutomationMcpExtensions.cs:134-137` — `/connect/token` currently carries `RequireRateLimiting(AutomationMcp.RateLimitPolicy)` (120/min per client), which is a machine budget, not a sign-in budget
  - `tests/Pegasus.IntegrationTests/StaffSignInSecurityTests.cs` — the existing cookie-limiter test this one mirrors
  - `docs/adr/0013-qdos-alpha-implementation-contract.md:41-43,66-67` — transient throttling, no persistent lockout
- Binding decisions:
  - **L-01** — evolve `Pegasus.Web` in place; the limiter stays in the same process and pipeline
  - **L-04** — this ticket names its subagent, skills and MCP tools
  - **ADR-0013** clause 12 (exists, in `refs`) — login protection is transient throttling
- Depends on: `DSK-04-02` — the password grant this limiter must cover

## Scope and constraints

ADR-0013 clause 12 fixes login protection as *transient throttling, never persistent Identity lockout*, and `Pegasus.Web` implements that for the Razor sign-in only (`Pages/Account/SignIn.cshtml.cs:13`, `Program.cs:797-817`). Once [[DSK-04-02]] opens a password grant, an unthrottled `/connect/token` becomes a credential-stuffing bypass around the control the ADR mandates — the desktop would be the weakest door into the same account store. Proposal §17.3 names "lost or shared workstation session" and §22.2 names "login throttling/current lockout behaviour" as required security tests; [[DSK-04-14]] asserts this behaviour.

- Future owner: `pegasus-gateway-dev`; tests: `pegasus-test-engineer`; independent review: `pegasus-desktop-reviewer`.
- Use `dotnet-webapi`, `optimizing-ef-core-queries` where the ticket changes a query, and `run-tests` for the actual runner profile. The project decision overrides generic “service per endpoint” advice: route handlers translate to existing `Pegasus.Core` ports; no second policy/service layer is introduced.
- The shared EPIC context binds this to versioned `/api/v1` route groups in the existing `Pegasus.Web`, the existing rate-limiter mechanism, an OpenAPI snapshot, and no Azure write.

- Microsoft Learn (fetched 2026-08-24): [ASP.NET Core rate limiting middleware](https://learn.microsoft.com/aspnet/core/performance/rate-limit?view=aspnetcore-10.0) confirms policies are configured then attached to endpoints and should be load-tested. Reuse `Program.cs`’s existing limiter rather than adding another mechanism.

- **Azure**: no write.
- **Scope boundary**: may touch `src/Pegasus.Web/Program.cs` (limiter policy, `OnRejected`, the global-limiter middleware), the desktop token endpoint added by [[DSK-04-02]], and `tests/Pegasus.IntegrationTests`. Must not touch `src/Pegasus.Core/Actors/StaffSessionPolicy.cs` values, Identity lockout options (`Program.cs:270`), or any Worker or infra file.
- **Traps**: (a) the `StaffSignIn` policy keys on the raw remote IP — behind the Container Apps ingress every desktop collapses into one bucket unless forwarded headers are configured before `UseRateLimiter()`; (b) `OnRejected` derives the reason code from the path alone, so `/connect/token` needs an explicit discriminator or desktop throttles are mislabelled `automation_rate_limited`; (c) reading the form to find `grant_type` consumes the body — enable buffering or OpenIddict sees an empty request.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.

## Governing documents

- `docs/adr/0013-qdos-alpha-implementation-contract.md`

## Planning implication

Reuse the cited boundaries and revalidate the named sources against current `origin/dev` after the ticket is taken. Do not create a compatibility path, duplicate policy, or an unapproved external write.
