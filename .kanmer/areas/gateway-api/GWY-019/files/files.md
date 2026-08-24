# Files — GWY-019: DSK-04-02 · OpenIddict client `pegasus-desktop`: password + refresh grants, Data Protection, staff lifetimes

## Change surface

| Path | Purpose / risk |
| --- | --- |
| `docs/desktop/04-auth-session-update-and-startup/README.md` | Authoritative context; update only if the ticket's accepted scope explicitly calls for it. |
| `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` | Authoritative context; update only if the ticket's accepted scope explicitly calls for it. |
| `src/Pegasus.Web/Mcp/AutomationMcpExtensions.cs` | Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core. |
| `src/Pegasus.Web/Mcp/AutomationMcp.cs` | Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core. |
| `src/Pegasus.Web/Mcp/AutomationTokenEndpoint.cs` | Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core. |
| `src/Pegasus.Web/Mcp/AutomationClientRegistry.cs` | Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core. |
| `src/Pegasus.Core/Actors/StaffSessionPolicy.cs` | Core policy or contract; reuse the existing business owner rather than placing policy in a host. |
| `src/Pegasus.Web/Program.cs` | Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core. |
| `src/Pegasus.Web/Pages/Account/SignIn.cshtml.cs` | Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core. |
| `src/Pegasus.Web/Pegasus.Web.csproj` | Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core. |
| `src/Pegasus.Infrastructure/Persistence/Migrations/20260803151159_AutomationActorOpenIddict.cs` | Persistence or provider adapter; inspect data access, transaction and failure behaviour before changing callers. |
| `src/Pegasus.Contracts` | Named by the ticket as an implementation or verification dependency. |
| `src/Pegasus.Web/packages.lock.json` | Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core. |
| `src/Pegasus.Web/Desktop/DesktopSessionExtensions.cs` | Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core. |
| `tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj` | Focused verification surface; extend the stated success, failure and regression coverage. |
| `src/Pegasus.Web/Desktop/DesktopSession.cs` | Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core. |
| `src/Pegasus.Web/Desktop/DesktopClientRegistry.cs` | Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core. |
| `src/Pegasus.Web/Desktop/DesktopTokenEndpoint.cs` | Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core. |
| `src/Pegasus.Core/Identity/IdentityContracts.cs` | Core policy or contract; reuse the existing business owner rather than placing policy in a host. |
| `tests/Pegasus.IntegrationTests/DesktopTokenIssuanceTests.cs` | Focused verification surface; extend the stated success, failure and regression coverage. |
| `scripts/Test-MigrationGrants.ps1` | Repository verification or operational automation; preserve its checked-in workflow. |

## Context files

- `docs/desktop/04-auth-session-update-and-startup/README.md` — Authoritative context; update only if the ticket's accepted scope explicitly calls for it.
- `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` — Authoritative context; update only if the ticket's accepted scope explicitly calls for it.
- `src/Pegasus.Web/Mcp/AutomationMcpExtensions.cs` — Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core.
- `src/Pegasus.Web/Mcp/AutomationMcp.cs` — Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core.
- `src/Pegasus.Web/Mcp/AutomationTokenEndpoint.cs` — Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core.
- `src/Pegasus.Web/Mcp/AutomationClientRegistry.cs` — Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core.
- `src/Pegasus.Core/Actors/StaffSessionPolicy.cs` — Core policy or contract; reuse the existing business owner rather than placing policy in a host.
- `src/Pegasus.Web/Program.cs` — Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core.
- `src/Pegasus.Web/Pages/Account/SignIn.cshtml.cs` — Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core.
- `src/Pegasus.Web/Pegasus.Web.csproj` — Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core.
- `src/Pegasus.Infrastructure/Persistence/Migrations/20260803151159_AutomationActorOpenIddict.cs` — Persistence or provider adapter; inspect data access, transaction and failure behaviour before changing callers.
- `src/Pegasus.Contracts` — Named by the ticket as an implementation or verification dependency.
- `src/Pegasus.Web/packages.lock.json` — Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core.
- `src/Pegasus.Web/Desktop/DesktopSessionExtensions.cs` — Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core.

## Ripple and out-of-scope boundary

- **Azure**: no write. Data Protection reuses the already-provisioned blob ring; no app setting, no new resource.
- **Scope boundary**: may touch `src/Pegasus.Web` (new `Desktop/` folder and the `Mcp/` extraction), `src/Pegasus.Core/Actors/StaffSessionPolicy.cs` (read only), `tests/Pegasus.IntegrationTests`. Must not touch `src/Pegasus.Worker`, `infra/`, the Razor cookie pipeline's behaviour, or any `Mcp/*McpTools.cs`.
- **Traps**: (a) the OpenIddict server is composed **inside** the `Features:AutomationMcp` gate today — extract it or the desktop flow silently disappears in any deployment with MCP off; (b) `DisableSlidingRefreshTokenExpiration()` is server-wide — implement the staff idle/absolute pair in the handler, never by flipping it; (c) ephemeral keys invalidate every session on restart — the Data Protection switch is not optional; (d) the OpenIddict tables carry **DENY DELETE** for both runtime roles (`20260803151159_AutomationActorOpenIddict.cs:202-208`), so any cleanup must be a status update, never a row delete.
- **Open question**: the plan does not state whether a `MustChangePassword` account may obtain a token. Step 10 chooses "yes, with the block enforced per request" and records the reading; if review disagrees, the change is one branch in `DesktopTokenEndpoint`.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.
