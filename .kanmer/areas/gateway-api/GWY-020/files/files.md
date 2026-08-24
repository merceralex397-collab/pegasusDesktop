# Files — GWY-020: DSK-04-03 · Apply the `StaffSignIn` and global sign-in limiters to `/connect/token` password grants

## Change surface

| Path | Purpose / risk |
| --- | --- |
| `docs/desktop/04-auth-session-update-and-startup/README.md` | Authoritative context; update only if the ticket's accepted scope explicitly calls for it. |
| `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` | Authoritative context; update only if the ticket's accepted scope explicitly calls for it. |
| `src/Pegasus.Web/Program.cs` | Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core. |
| `src/Pegasus.Core/Actors/StaffSessionPolicy.cs` | Core policy or contract; reuse the existing business owner rather than placing policy in a host. |
| `src/Pegasus.Web/Mcp/AutomationMcpExtensions.cs` | Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core. |
| `tests/Pegasus.IntegrationTests/StaffSignInSecurityTests.cs` | Focused verification surface; extend the stated success, failure and regression coverage. |
| `docs/adr/0013-qdos-alpha-implementation-contract.md` | Authoritative context; update only if the ticket's accepted scope explicitly calls for it. |
| `tests/Pegasus.IntegrationTests/DesktopTokenRateLimitTests.cs` | Focused verification surface; extend the stated success, failure and regression coverage. |
| `tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj` | Focused verification surface; extend the stated success, failure and regression coverage. |

## Context files

- `docs/desktop/04-auth-session-update-and-startup/README.md` — Authoritative context; update only if the ticket's accepted scope explicitly calls for it.
- `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` — Authoritative context; update only if the ticket's accepted scope explicitly calls for it.
- `src/Pegasus.Web/Program.cs` — Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core.
- `src/Pegasus.Core/Actors/StaffSessionPolicy.cs` — Core policy or contract; reuse the existing business owner rather than placing policy in a host.
- `src/Pegasus.Web/Mcp/AutomationMcpExtensions.cs` — Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core.
- `tests/Pegasus.IntegrationTests/StaffSignInSecurityTests.cs` — Focused verification surface; extend the stated success, failure and regression coverage.
- `docs/adr/0013-qdos-alpha-implementation-contract.md` — Authoritative context; update only if the ticket's accepted scope explicitly calls for it.
- `tests/Pegasus.IntegrationTests/DesktopTokenRateLimitTests.cs` — Focused verification surface; extend the stated success, failure and regression coverage.
- `tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj` — Focused verification surface; extend the stated success, failure and regression coverage.

## Ripple and out-of-scope boundary

- **Azure**: no write.
- **Scope boundary**: may touch `src/Pegasus.Web/Program.cs` (limiter policy, `OnRejected`, the global-limiter middleware), the desktop token endpoint added by [[DSK-04-02]], and `tests/Pegasus.IntegrationTests`. Must not touch `src/Pegasus.Core/Actors/StaffSessionPolicy.cs` values, Identity lockout options (`Program.cs:270`), or any Worker or infra file.
- **Traps**: (a) the `StaffSignIn` policy keys on the raw remote IP — behind the Container Apps ingress every desktop collapses into one bucket unless forwarded headers are configured before `UseRateLimiter()`; (b) `OnRejected` derives the reason code from the path alone, so `/connect/token` needs an explicit discriminator or desktop throttles are mislabelled `automation_rate_limited`; (c) reading the form to find `grant_type` consumes the body — enable buffering or OpenIddict sees an empty request.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.
