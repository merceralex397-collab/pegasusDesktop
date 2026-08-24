# Plan — GWY-022: DSK-04-05 · Revoke refresh tokens on account disable, password change, logout and sign-out-everywhere

## Governing documents

- `docs/frd/frd-04-parties-accounts-and-access.md`

## Chosen approach

Make account disable, password change, explicit desktop logout and an administrator "sign out everywhere" all revoke the subject's OpenIddict refresh tokens and authorizations through one shared path, fill in the `RevokedAuthorizations` / `RevokedTokens` counters that already exist on the result contracts, and write the audit entries through `ISecurityEventWriter`.

## Routing and constraints

- Future owner: `pegasus-gateway-dev`; tests: `pegasus-test-engineer`; independent review: `pegasus-desktop-reviewer`.
- Use `dotnet-webapi`, `optimizing-ef-core-queries` where the ticket changes a query, and `run-tests` for the actual runner profile. The project decision overrides generic “service per endpoint” advice: route handlers translate to existing `Pegasus.Core` ports; no second policy/service layer is introduced.
- The shared EPIC context binds this to versioned `/api/v1` route groups in the existing `Pegasus.Web`, the existing rate-limiter mechanism, an OpenAPI snapshot, and no Azure write.

- Microsoft Learn (fetched 2026-08-24): [ASP.NET Core OpenAPI support](https://learn.microsoft.com/aspnet/core/fundamentals/openapi/overview?view=aspnetcore-10.0) confirms first-party OpenAPI generation. Use the repository’s planned committed snapshot and contract-test flow rather than adding a parallel API documentation path.


## Ordered implementation steps

1. **Orient.** Read `docs/desktop/04-auth-session-update-and-startup/README.md` § 3 decision 3 and the § 5 row `DSK-04-05`, then `docs/frd/frd-04-parties-accounts-and-access.md` § Staff role access matrix and § Permanent action history. Call Kanmer `get_doc_gates` for this ticket's board id, `take_ticket`, then load the skills under Routing.
2. **Read the three call sites that already promise revocation counts**: `src/Pegasus.Infrastructure/Persistence/EfStaffAccountAdministration.cs:120` (disable), `:199` (role assignment), and `src/Pegasus.Infrastructure/Persistence/EfStaffPasswordChange.cs:78-79`. Note that both admin paths already run inside a **serializable** transaction with replay-by-`OperationKey`, and that the counters are persisted in the action-history `AfterJson` and re-read on replay by `ParseRevocationCounts` (`:461-482`).
3. **Define one Core port, not three implementations.** Add `ISessionRevocation` to `src/Pegasus.Core/Identity/` with a single method returning `(long Authorizations, long Tokens)` for a staff subject id plus a reason code — `Pegasus.Core` must not reference OpenIddict, so the port is a plain contract and the OpenIddict work lives in the adapter. This satisfies `docs/engineering.md` § One Core owner.
4. **Implement the adapter over OpenIddict's managers.** Add `src/Pegasus.Infrastructure/Persistence/OpenIddictSessionRevocation.cs` using `IOpenIddictTokenManager` and `IOpenIddictAuthorizationManager`: find every token and authorization whose subject is the staff `Guid` in `"D"` format and **revoke by status update** (`TryRevokeAsync`), counting each success. **Never delete rows** — the migration at `20260803151159_AutomationActorOpenIddict.cs:202-208` issues an explicit `DENY DELETE` to both runtime roles, so a prune call fails in production with a SQL permission error.
5. **Exclude the Automation client.** Filter the revocation to tokens whose client is `pegasus-desktop`, or equivalently exclude the Automation client id from `AutomationMcpOptions`; a staff disable must not revoke the MCP connector's authorization, which ADR-0027 governs separately.
6. **Wire the disable path.** In `EfStaffAccountAdministration.DisableAsync`, replace `var revoked = (Authorizations: 0L, Tokens: 0L);` at `:120` with a call to the port, keeping it **inside** the existing serializable transaction and **before** `AddHistory`, so `Snapshot(user, roles, revoked.Authorizations, revoked.Tokens)` records the real counts and a replay returns identical numbers through `ParseRevocationCounts`.
7. **Wire the password-change path.** In `EfStaffPasswordChange.ChangeAsync`, call the port after `userManager.ChangePasswordAsync` succeeds (which already rotates the security stamp) and return the real counts instead of the hard-coded zeros at `:78-79`; append a `SecurityEventType.Token` / `Succeeded` event with reason code `password_changed_tokens_revoked` alongside the existing `PasswordChanged` event, keeping both in the same transaction.
8. **Wire the role-assignment path** at `:199` the same way, since a role change already rotates the stamp and must not leave a token carrying stale role claims.
9. **Add logout and sign-out-everywhere.** Expose two `/api/v1` session endpoints through the group from [[DSK-03-02]]: `POST /api/v1/session/logout` revokes only the calling refresh token's own authorization, and `POST /api/v1/session/revoke-all` requires `StaffAccessRight.ManageStaffAccounts` (`src/Pegasus.Core/Identity/StaffAuthorization.cs:44-52`) and calls the same port for a named subject. Both write an audited `SecurityEvent`; both reuse the port — no second revocation implementation.
10. **Check the migration census and grants.** This ticket adds **no** table, so `pwsh ./scripts/Test-MigrationGrants.ps1` must stay green and the pinned migration list in `tests/Pegasus.IntegrationTests/IntakePersistenceIntegrationTests.cs:22-95` must be untouched. If the plan document concludes a table *is* needed, add the `Grant*` migration, mirror it in `scripts/Invoke-AzureDatabaseBootstrap.ps1:103-139`, and append the migration id to that pinned list in the same PR.
11. **Test.** Add `tests/Pegasus.IntegrationTests/DesktopSessionRevocationTests.cs`, `[Trait("Category", "SqlServer")]`, built like `StaffSignInSecurityTests.cs:20-52`. Facts: issue a token, disable the account, then the refresh exchange returns `invalid_grant`; the `DisableStaffAccountResult` reports non-zero `RevokedTokens`; **replaying the same `OperationKey` returns the identical counts**; password change revokes and reports counts; `revoke-all` by an administrator revokes another user's tokens and writes a `SecurityEvent`; a non-administrator calling `revoke-all` gets `urn:pegasus:problem:not-authorized`; the Automation client's own tokens survive a staff disable.
12. **Run** `dotnet test tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --filter "FullyQualifiedName~DesktopSessionRevocation|FullyQualifiedName~StaffAccount|FullyQualifiedName~Automation"` plus `pwsh ./scripts/Test-MigrationGrants.ps1`, and record both in the post-implementation report.

## Acceptance conditions

- [ ] Disabling an account revokes its desktop refresh tokens and authorizations; the next refresh exchange returns `invalid_grant`.
- [ ] Password change and role assignment revoke the subject's tokens through the same path.
- [ ] `DisableStaffAccountResult` and `ChangeStaffPasswordResult` report real, non-zero `RevokedAuthorizations`/`RevokedTokens`, and a replayed `OperationKey` returns identical counts.
- [ ] `POST /api/v1/session/logout` revokes only the caller's session; `POST /api/v1/session/revoke-all` is Administrator-only and audited.
- [ ] Every revocation writes a `SecurityEvent` with a correlation id and a stable reason code.
- [ ] The Automation MCP client's authorizations and tokens are untouched by any staff revocation.
- [ ] No OpenIddict row is deleted anywhere in the change.

## Verification

- [ ] `dotnet test tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --filter "FullyQualifiedName~DesktopSessionRevocation"` — expected: all facts pass, including the replay-count fact.
- [ ] `dotnet test tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --filter "FullyQualifiedName~Automation"` — expected: green; connector tokens survived.
- [ ] `pwsh ./scripts/Test-MigrationGrants.ps1` — expected: exits 0 with no ungranted table reported.
- [ ] `dotnet test tests/Pegasus.Core.Tests/Pegasus.Core.Tests.csproj` — expected: green; the new Core port added no behaviour to Core.

## Risks and boundaries

- **Azure**: no write.
- **Scope boundary**: may touch `src/Pegasus.Core/Identity/` (the new port only), `src/Pegasus.Infrastructure/Persistence/EfStaffAccountAdministration.cs`, `EfStaffPasswordChange.cs`, the new adapter, the `/api/v1` session endpoints, and the two test projects. Must not touch `src/Pegasus.Worker`, `infra/`, or the Automation client registry.
- **Traps**: (a) **`DENY DELETE`** on all four OpenIddict tables for both runtime roles — revoke by status, never prune; (b) the revocation counters are persisted in the action-history `AfterJson` and re-read on replay, so computing them outside the transaction breaks idempotency; (c) the runtime-role GRANT trap (PLAT-035 class) — if this ticket ever adds a table, the `Grant*` migration, the `Invoke-AzureDatabaseBootstrap.ps1` mirror and the pinned census in `IntakePersistenceIntegrationTests.cs` all change together; (d) `Pegasus.Core` must not reference OpenIddict — keep the port abstract or `tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs` fails.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.
