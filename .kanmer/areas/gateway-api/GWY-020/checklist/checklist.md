# Checklist — GWY-020: DSK-04-03 · Apply the `StaffSignIn` and global sign-in limiters to `/connect/token` password grants

- [x] Orient. Read the § 5 row `DSK-04-03` and § 7 risk "Rate limiter scope" in `docs/desktop/04-auth-session-update-and-startup/README.md`, and `docs/adr/0013-qdos-alpha-implementation-contract.md:41-43`. Call Kanmer `get_doc_gates` for this ticket's board id, then `take_ticket`, then load the skills under Routing.
- [x] Read the three existing mechanisms end to end so the new path reuses them rather than adding a fourth: `src/Pegasus.Web/Program.cs:275-327` (policies and `OnRejected`), `Program.cs:797-817` (the global-limiter middleware), `Program.cs:819` (`UseRateLimiter`). Note that `OnRejected` chooses its reason code purely from the request path.
- [x] Decide the partition key and record it. `StaffSignIn` partitions on `context.Connection.RemoteIpAddress`. Behind the Container Apps ingress this is the ingress address unless forwarded headers are applied first — confirm the forwarded-headers configuration in `src/Pegasus.Web/Program.cs` runs before `UseRateLimiter()` and note the finding in the ticket plan. If it does not, stop and raise it: without it every desktop shares one 10/minute bucket, which is a denial of service on the whole office.
- [x] Apply the per-client policy to the password grant only. In the token-endpoint mapping added by [[DSK-04-02]], require `StaffSignIn` for requests whose form `grant_type` is `password`, and leave the Automation grants on `AutomationMcp.RateLimitPolicy`. Because the policy must be chosen from the request body, implement it as a short middleware in front of the endpoint (mirroring `Program.cs:797-817`) rather than as a second `RequireRateLimiting` attribute; read `grant_type` through `HttpContext.Request.ReadFormAsync()` and re-enable buffering so the OpenIddict handler still sees the body.
- [x] Apply the global limiter. Extend the middleware at `src/Pegasus.Web/Program.cs:797-817` so its condition is `POST /Account/SignIn` or (`POST /connect/token` with `grant_type=password`). Keep the single `FixedWindowRateLimiter` singleton — one 100/minute budget shared by browser and desktop sign-ins is the intent of `StaffSessionPolicy.SignInAttemptsGlobalPerMinute`.
- [x] Emit the same reason code. Extend the path test in `OnRejected` (`Program.cs:281-295`) so a rejected password grant on `/connect/token` produces `sign_in_rate_limited`, not `automation_rate_limited`; the Automation grants on the same path must keep `automation_rate_limited`. Because `OnRejected` sees only the path, pass the discriminator through `HttpContext.Items` from the middleware in step 4.
- [x] Confirm the response shape: HTTP 429 with `Retry-After: 60`, no body that leaks whether the username exists. `Program.cs:277-279` already sets the header globally — assert it rather than re-setting it.
- [x] Test, mirroring the cookie test. Add `tests/Pegasus.IntegrationTests/DesktopTokenRateLimitTests.cs` with `[Trait("Category", "SqlServer")]`, built like `tests/Pegasus.IntegrationTests/StaffSignInSecurityTests.cs:20-60`. Facts: (a) the 11th password grant within one minute from one client returns 429 with `Retry-After: 60`; (b) a `SecurityEvent` row exists with `Type = RateLimited` and `ReasonCode = sign_in_rate_limited`; (c) an Automation client-credentials request on the same path is *not* charged to the sign-in bucket and still returns a token; (d) a successful password grant under the limit is unaffected.
- [x] Prove no lockout was introduced. Assert in the test that after the throttle window the same account signs in successfully and that `PegasusIdentityUser.LockoutEnd` is still null — ADR-0013 forbids persistent lockout.
- [x] Run `dotnet test tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --filter "FullyQualifiedName~DesktopTokenRateLimit|FullyQualifiedName~StaffSignInSecurity|FullyQualifiedName~Automation"` and record the output in the post-implementation report.
- [x] The 11th `grant_type=password` request within one minute from one client returns 429 with `Retry-After: 60`.
- [x] A `SecurityEvent` with type `RateLimited` and reason code `sign_in_rate_limited` is written for that rejection.
- [x] The global 100/minute sign-in budget is shared between `POST /Account/SignIn` and desktop password grants.
- [x] Automation client-credentials, authorization-code and refresh grants on `/connect/token` keep the `AutomationMcp` 120/minute policy and the `automation_rate_limited` reason code.
- [x] No ASP.NET Identity lockout state is introduced; `lockoutOnFailure: false` remains the only credential-check call.
- [x] `dotnet test tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --filter "FullyQualifiedName~DesktopTokenRateLimit"` — expected: all four facts pass.
- [x] `dotnet test tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --filter "FullyQualifiedName~StaffSignInSecurity"` — expected: the existing cookie-limiter behaviour is unchanged.
- [x] `dotnet test tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --filter "FullyQualifiedName~Automation"` — expected: green; the MCP budget was not narrowed.

## Progress notes

No implementation has started. This checklist is derived from the ticket’s accepted scope and is maintained by the ticket implementer.


## Progress notes

Implementation and parent-controlled validation completed on 2026-08-30. All acceptance and verification checklist items are evidenced by the committed implementation, the dedicated integration tests, the full Release build, and the focused/combined test runs recorded in the ticket plan and scratch. The time-window replenishment was not simulated with a clock advance and is not claimed as independently tested.
