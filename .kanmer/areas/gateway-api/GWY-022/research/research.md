# Research — GWY-022: DSK-04-05 · Revoke refresh tokens on account disable, password change, logout and sign-out-everywhere

## Question

Make account disable, password change, explicit desktop logout and an administrator "sign out everywhere" all revoke the subject's OpenIddict refresh tokens and authorizations through one shared path, fill in the `RevokedAuthorizations` / `RevokedTokens` counters that already exist on the result contracts, and write the audit entries through `ISecurityEventWriter`.

## Evidence examined

- Plan row: `docs/desktop/04-auth-session-update-and-startup/README.md` § 5 — `DSK-04-05`
- Plan detail: same file § 3 decision 3 (revocation), § 3 session failure matrix rows "Refresh token invalid/revoked" and "Account disabled"
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 8.2 Protocol (item 6), § 8.3 Authorization, § 17.1 Required controls ("account revocation", "audit records for sensitive operations")
- Repository evidence:
  - `src/Pegasus.Core/Identity/StaffAccountAdministration.cs:70-92` — `DisableStaffAccountRequest`/`Result` and the role-assignment result, both with `RevokedAuthorizations`/`RevokedTokens`
  - `src/Pegasus.Core/Identity/StaffPasswordChange.cs:3-20` — `ChangeStaffPasswordRequest`/`Result` and `IStaffPasswordChangeStore`
  - `src/Pegasus.Infrastructure/Persistence/EfStaffAccountAdministration.cs:77-152` — the disable path: serializable transaction, replay-by-`OperationKey`, `UpdateSecurityStampAsync`, `AddHistory`, `AddSecurityEvent(SecurityEventType.SecurityStampChanged, …, "staff_account_disabled")`; **`var revoked = (Authorizations: 0L, Tokens: 0L);` at `:120` is the seam to fill**; the same seam at `:199` on the role-assignment path
  - `src/Pegasus.Infrastructure/Persistence/EfStaffAccountAdministration.cs:423-436,461-482` — `Snapshot(...)` writes the counters into the action-history `AfterJson`, and `ParseRevocationCounts` reads them back on replay: the counters must be **stable across a replay**
  - `src/Pegasus.Infrastructure/Persistence/EfStaffPasswordChange.cs:54-80` — password change updates `MustChangePassword` and the stamp, then returns `RevokedAuthorizations: 0, RevokedTokens: 0`
  - `src/Pegasus.Infrastructure/Persistence/Migrations/20260803151159_AutomationActorOpenIddict.cs:195-208` — Web role has `SELECT, INSERT, UPDATE` on `OpenIddictTokens`/`Applications`/`Authorizations` and an explicit **`DENY DELETE`** on all four tables
  - `scripts/Invoke-AzureDatabaseBootstrap.ps1:103-139` — the mirrored grant expectations that must keep matching
  - `src/Pegasus.Core/Identity/IdentityContracts.cs:100-145` — `SecurityEventType` (`Token`, `SecurityStampChanged`, `PasswordChanged`), `SecurityEvent`, `ISecurityEventWriter`
- Binding decisions:
  - **L-01** — revocation happens inside `Pegasus.Web`/`Pegasus.Infrastructure`, no new service
  - **L-04** — this ticket names its subagent, skills and MCP tools
  - **ADR-0102** (owed, `docs_todo`) — the token session whose revocation this is; **ADR-0027** governs the MCP connector's own refresh tokens, which must not be revoked by a staff action
- Depends on: `DSK-04-04` — the bearer path and the per-request stamp check that makes revocation observable within one request

## Scope and constraints

Proposal §8.2 item 6 requires the gateway to support "logout, account disablement, refresh revocation and password-change invalidation", and §8.3 requires that a disabled account stops working without waiting for a desktop update. `Pegasus.Core` already declares the contract — `DisableStaffAccountResult` and `ChangeStaffPasswordResult` both carry `RevokedAuthorizations` and `RevokedTokens` (`src/Pegasus.Core/Identity/StaffAccountAdministration.cs:76-80`, `src/Pegasus.Core/Identity/StaffPasswordChange.cs:10-14`) — but both implementations hard-code zero today (`EfStaffAccountAdministration.cs:120`, `EfStaffPasswordChange.cs:78-79`) because there were no staff tokens to revoke. [[DSK-04-02]] creates them, and without this ticket a disabled operator keeps a working refresh token for up to two hours.

- Future owner: `pegasus-gateway-dev`; tests: `pegasus-test-engineer`; independent review: `pegasus-desktop-reviewer`.
- Use `dotnet-webapi`, `optimizing-ef-core-queries` where the ticket changes a query, and `run-tests` for the actual runner profile. The project decision overrides generic “service per endpoint” advice: route handlers translate to existing `Pegasus.Core` ports; no second policy/service layer is introduced.
- The shared EPIC context binds this to versioned `/api/v1` route groups in the existing `Pegasus.Web`, the existing rate-limiter mechanism, an OpenAPI snapshot, and no Azure write.

- Microsoft Learn (fetched 2026-08-24): [ASP.NET Core OpenAPI support](https://learn.microsoft.com/aspnet/core/fundamentals/openapi/overview?view=aspnetcore-10.0) confirms first-party OpenAPI generation. Use the repository’s planned committed snapshot and contract-test flow rather than adding a parallel API documentation path.

- **Azure**: no write.
- **Scope boundary**: may touch `src/Pegasus.Core/Identity/` (the new port only), `src/Pegasus.Infrastructure/Persistence/EfStaffAccountAdministration.cs`, `EfStaffPasswordChange.cs`, the new adapter, the `/api/v1` session endpoints, and the two test projects. Must not touch `src/Pegasus.Worker`, `infra/`, or the Automation client registry.
- **Traps**: (a) **`DENY DELETE`** on all four OpenIddict tables for both runtime roles — revoke by status, never prune; (b) the revocation counters are persisted in the action-history `AfterJson` and re-read on replay, so computing them outside the transaction breaks idempotency; (c) the runtime-role GRANT trap (PLAT-035 class) — if this ticket ever adds a table, the `Grant*` migration, the `Invoke-AzureDatabaseBootstrap.ps1` mirror and the pinned census in `IntakePersistenceIntegrationTests.cs` all change together; (d) `Pegasus.Core` must not reference OpenIddict — keep the port abstract or `tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs` fails.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.

## Governing documents

- `docs/frd/frd-04-parties-accounts-and-access.md`

## Planning implication

Reuse the cited boundaries and revalidate the named sources against current `origin/dev` after the ticket is taken. Do not create a compatibility path, duplicate policy, or an unapproved external write.
