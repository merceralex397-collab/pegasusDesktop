# Plan — GWY-020: DSK-04-03 · Apply the `StaffSignIn` and global sign-in limiters to `/connect/token` password grants

## Governing documents

- `docs/adr/0013-qdos-alpha-implementation-contract.md`

## Chosen approach

Make a desktop password grant on `POST /connect/token` obey exactly the same throttling as a browser sign-in: the per-client `StaffSignIn` fixed-window policy (10/minute) plus the global 100/minute sign-in limiter, returning 429 with `Retry-After: 60` and writing the same `sign_in_rate_limited` `SecurityEvent`.

## Routing and constraints

- Future owner: `pegasus-gateway-dev`; tests: `pegasus-test-engineer`; independent review: `pegasus-desktop-reviewer`.
- Use `dotnet-webapi`, `optimizing-ef-core-queries` where the ticket changes a query, and `run-tests` for the actual runner profile. The project decision overrides generic “service per endpoint” advice: route handlers translate to existing `Pegasus.Core` ports; no second policy/service layer is introduced.
- The shared EPIC context binds this to versioned `/api/v1` route groups in the existing `Pegasus.Web`, the existing rate-limiter mechanism, an OpenAPI snapshot, and no Azure write.

- Microsoft Learn (fetched 2026-08-24): [ASP.NET Core rate limiting middleware](https://learn.microsoft.com/aspnet/core/performance/rate-limit?view=aspnetcore-10.0) confirms policies are configured then attached to endpoints and should be load-tested. Reuse `Program.cs`’s existing limiter rather than adding another mechanism.


## Ordered implementation steps

1. **Orient.** Read the § 5 row `DSK-04-03` and § 7 risk "Rate limiter scope" in `docs/desktop/04-auth-session-update-and-startup/README.md`, and `docs/adr/0013-qdos-alpha-implementation-contract.md:41-43`. Call Kanmer `get_doc_gates` for this ticket's board id, then `take_ticket`, then load the skills under Routing.
2. **Read the three existing mechanisms end to end** so the new path reuses them rather than adding a fourth: `src/Pegasus.Web/Program.cs:275-327` (policies and `OnRejected`), `Program.cs:797-817` (the global-limiter middleware), `Program.cs:819` (`UseRateLimiter`). Note that `OnRejected` chooses its reason code purely from the request path.
3. **Decide the partition key and record it.** `StaffSignIn` partitions on `context.Connection.RemoteIpAddress`. Behind the Container Apps ingress this is the ingress address unless forwarded headers are applied first — confirm the forwarded-headers configuration in `src/Pegasus.Web/Program.cs` runs before `UseRateLimiter()` and note the finding in the ticket plan. If it does not, **stop and raise it**: without it every desktop shares one 10/minute bucket, which is a denial of service on the whole office.
4. **Apply the per-client policy to the password grant only.** In the token-endpoint mapping added by [[DSK-04-02]], require `StaffSignIn` for requests whose form `grant_type` is `password`, and leave the Automation grants on `AutomationMcp.RateLimitPolicy`. Because the policy must be chosen from the request body, implement it as a short middleware in front of the endpoint (mirroring `Program.cs:797-817`) rather than as a second `RequireRateLimiting` attribute; read `grant_type` through `HttpContext.Request.ReadFormAsync()` and re-enable buffering so the OpenIddict handler still sees the body.
5. **Apply the global limiter.** Extend the middleware at `src/Pegasus.Web/Program.cs:797-817` so its condition is `POST /Account/SignIn` **or** (`POST /connect/token` with `grant_type=password`). Keep the single `FixedWindowRateLimiter` singleton — one 100/minute budget shared by browser and desktop sign-ins is the intent of `StaffSessionPolicy.SignInAttemptsGlobalPerMinute`.
6. **Emit the same reason code.** Extend the path test in `OnRejected` (`Program.cs:281-295`) so a rejected password grant on `/connect/token` produces `sign_in_rate_limited`, not `automation_rate_limited`; the Automation grants on the same path must keep `automation_rate_limited`. Because `OnRejected` sees only the path, pass the discriminator through `HttpContext.Items` from the middleware in step 4.
7. **Confirm the response shape**: HTTP 429 with `Retry-After: 60`, no body that leaks whether the username exists. `Program.cs:277-279` already sets the header globally — assert it rather than re-setting it.
8. **Test, mirroring the cookie test.** Add `tests/Pegasus.IntegrationTests/DesktopTokenRateLimitTests.cs` with `[Trait("Category", "SqlServer")]`, built like `tests/Pegasus.IntegrationTests/StaffSignInSecurityTests.cs:20-60`. Facts: (a) the 11th password grant within one minute from one client returns 429 with `Retry-After: 60`; (b) a `SecurityEvent` row exists with `Type = RateLimited` and `ReasonCode = sign_in_rate_limited`; (c) an Automation client-credentials request on the same path is *not* charged to the sign-in bucket and still returns a token; (d) a successful password grant under the limit is unaffected.
9. **Prove no lockout was introduced.** Assert in the test that after the throttle window the same account signs in successfully and that `PegasusIdentityUser.LockoutEnd` is still null — ADR-0013 forbids persistent lockout.
10. **Run** `dotnet test tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --filter "FullyQualifiedName~DesktopTokenRateLimit|FullyQualifiedName~StaffSignInSecurity|FullyQualifiedName~Automation"` and record the output in the post-implementation report.

## Acceptance conditions

- [ ] The 11th `grant_type=password` request within one minute from one client returns 429 with `Retry-After: 60`.
- [ ] A `SecurityEvent` with type `RateLimited` and reason code `sign_in_rate_limited` is written for that rejection.
- [ ] The global 100/minute sign-in budget is shared between `POST /Account/SignIn` and desktop password grants.
- [ ] Automation client-credentials, authorization-code and refresh grants on `/connect/token` keep the `AutomationMcp` 120/minute policy and the `automation_rate_limited` reason code.
- [ ] No ASP.NET Identity lockout state is introduced; `lockoutOnFailure: false` remains the only credential-check call.

## Verification

- [ ] `dotnet test tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --filter "FullyQualifiedName~DesktopTokenRateLimit"` — expected: all four facts pass.
- [ ] `dotnet test tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --filter "FullyQualifiedName~StaffSignInSecurity"` — expected: the existing cookie-limiter behaviour is unchanged.
- [ ] `dotnet test tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --filter "FullyQualifiedName~Automation"` — expected: green; the MCP budget was not narrowed.

## Risks and boundaries

- **Azure**: no write.
- **Scope boundary**: may touch `src/Pegasus.Web/Program.cs` (limiter policy, `OnRejected`, the global-limiter middleware), the desktop token endpoint added by [[DSK-04-02]], and `tests/Pegasus.IntegrationTests`. Must not touch `src/Pegasus.Core/Actors/StaffSessionPolicy.cs` values, Identity lockout options (`Program.cs:270`), or any Worker or infra file.
- **Traps**: (a) the `StaffSignIn` policy keys on the raw remote IP — behind the Container Apps ingress every desktop collapses into one bucket unless forwarded headers are configured before `UseRateLimiter()`; (b) `OnRejected` derives the reason code from the path alone, so `/connect/token` needs an explicit discriminator or desktop throttles are mislabelled `automation_rate_limited`; (c) reading the form to find `grant_type` consumes the body — enable buffering or OpenIddict sees an empty request.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.

## Implementation and validation — 2026-08-30

- The existing `MarkDesktopPasswordGrantAsync` now enables request buffering before `ReadFormAsync`, preserving the form for OpenIddict. It marks only POST `/connect/token` requests whose client is `pegasus-desktop` and whose grant type is `password`.
- The existing global `FixedWindowRateLimiter` middleware now charges browser POST `/Account/SignIn` and marked desktop password grants against the shared 100-per-minute budget. The existing `/connect/token` policy remains the single endpoint policy; its partition keeps automation grants at 120/minute and gives marked desktop password grants the 10-per-client sign-in limit.
- `OnRejected` recognizes the marker so desktop rejections record `sign_in_rate_limited`; automation requests remain `automation_rate_limited`. The existing `Retry-After: 60` response is retained.
- Forwarded headers are configured in the production profile before `UseRateLimiter()` (`UseForwardedHeaders` at the production middleware boundary precedes routing/rate limiting), so the limiter's remote-IP key receives the forwarded client address when the deployment supplies it. No Azure or deployment change was made.

## Simplification pass — 2026-08-30

- Reused the existing token endpoint, global limiter, automation limiter, `OnRejected` callback, marker convention, and security-event writer; no new service, policy owner, configuration, or compatibility path was introduced.
- The only implementation change required after review of the branch diff was request buffering before form inspection. The dedicated integration test file is limited to the ticket's three observable rate-limit/security cases. No unrelated cleanup or speculative time-control mechanism was added.
- No unapplied simplification finding remains. The one-minute replenishment path was not simulated by advancing wall time; the tests prove the fixed-window limits and response/event/lockout behavior without claiming a time-advanced result.

## Validation commands — 2026-08-30

- `dotnet restore ./Pegasus.slnx --locked-mode` — passed; all projects up to date.
- `dotnet build ./Pegasus.slnx --configuration Release --no-restore -nr:false -p:UseSharedCompilation=false` — passed; 0 warnings, 0 errors.
- `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~DesktopTokenRateLimit"` — passed; 3/3.
- `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~DesktopTokenRateLimit|FullyQualifiedName~StaffSignInSecurity|FullyQualifiedName~Automation"` — passed; 40/40.
- `git diff --check` — passed.

## Final validation refresh — 2026-08-30

- Final branch head: `58ce5c09a5994e9ae292a28c25a304342f10a34e`.
- Parent rerun after the review correction: `git diff --check` passed; `dotnet restore ./Pegasus.slnx --locked-mode` passed; `dotnet build ./Pegasus.slnx --configuration Release --no-restore -nr:false -p:UseSharedCompilation=false` passed with 0 warnings and 0 errors; focused `DesktopTokenRateLimit` passed 4/4; combined `DesktopTokenRateLimit|StaffSignInSecurity|Automation` passed 41/41.
- The final tests exercise a real browser POST through the global middleware (the test harness returns the expected antiforgery `400` after the permit is consumed), then real desktop password requests until the shared global limiter returns `429` and writes `sign_in_rate_limited`. They also issue 120 real Automation client-credentials requests, then assert the 121st returns `429`, `Retry-After: 60`, and `automation_rate_limited`.
- Independent re-review of this final head is pending. No merge or release action is authorized until it passes.

## Independent review — 2026-08-30

Fermat the 2nd independently reviewed exact head `58ce5c09a5994e9ae292a28c25a304342f10a34e` after the final parent validation. Verdict: PASS. The reviewer confirmed the real browser-to-desktop shared global-limiter exercise, Automation 120/minute rejection and `automation_rate_limited`, Retry-After/security-event/no-lockout assertions, report gate, locked restore, Release build 0/0, focused 4/4, regression 41/41, and diff check. No merge blocker remains.
