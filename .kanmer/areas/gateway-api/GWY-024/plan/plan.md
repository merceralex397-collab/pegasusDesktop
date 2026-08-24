# Plan — GWY-024: DSK-04-14 · Security tests for the token path: expiry, rotation, revocation, role bypass, version spoofing

## Governing documents

- `docs/frd/frd-04-parties-accounts-and-access.md`

## Chosen approach

Give every authentication item of proposal §22.2's security-test list a test or a recorded manual check: token expiry, refresh rotation, revocation, disabled account, role-bypass attempts, API version spoofing, and the file permissions of the desktop's DPAPI refresh-token store.

## Routing and constraints

- Future owner: `pegasus-gateway-dev`; tests: `pegasus-test-engineer`; independent review: `pegasus-desktop-reviewer`.
- Use `dotnet-webapi`, `optimizing-ef-core-queries` where the ticket changes a query, and `run-tests` for the actual runner profile. The project decision overrides generic “service per endpoint” advice: route handlers translate to existing `Pegasus.Core` ports; no second policy/service layer is introduced.
- The shared EPIC context binds this to versioned `/api/v1` route groups in the existing `Pegasus.Web`, the existing rate-limiter mechanism, an OpenAPI snapshot, and no Azure write.

- Microsoft Learn (fetched 2026-08-24): [ASP.NET Core OpenAPI support](https://learn.microsoft.com/aspnet/core/fundamentals/openapi/overview?view=aspnetcore-10.0) confirms first-party OpenAPI generation. Use the repository’s planned committed snapshot and contract-test flow rather than adding a parallel API documentation path.


## Ordered implementation steps

1. **Orient.** Read `docs/desktop/04-auth-session-update-and-startup/README.md` § 3 session failure matrix and § 4 exit gate, and proposal § 22.2 "Security tests" (`docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md`, the twelve-item list). Call Kanmer `get_doc_gates` for this ticket's board id, `take_ticket`, then load the skills under Routing.
2. **Write the coverage matrix first, in the ticket `research` document.** One row per §22.2 authentication item — login throttling, token expiry, token rotation, token revocation, disabled account, role bypass, API version spoofing, temporary-file permissions — against the test (file and fact name) or the recorded manual check that satisfies it. Rows already satisfied by [[DSK-04-03]] or [[DSK-04-05]] are cited, not duplicated: this ticket adds only what is missing.
3. **Run `test-gap-analysis` over the auth surface** — `src/Pegasus.Web/Desktop/`, `src/Pegasus.Web/Mcp/AutomationTokenEndpoint.cs`, `src/Pegasus.Infrastructure/Persistence/EfStaffAccountAdministration.cs`, `EfStaffPasswordChange.cs` — and reconcile its findings with the matrix from step 2. Any gap the skill reports that the matrix does not list is added or explicitly dismissed with a reason.
4. **Add the token-lifecycle facts** in `tests/Pegasus.IntegrationTests/DesktopTokenSecurityTests.cs`, `[Trait("Category", "SqlServer")]`, following `StaffSignInSecurityTests.cs:20-52`: an access token past 10 minutes is refused; a refresh token replayed after rotation is refused (`invalid_grant`) and the whole chain is revoked; a refresh past the 8-hour absolute cap is refused; a token minted for the Automation client is refused on `/api/v1`. Drive time with the injected `TimeProvider`, never `Thread.Sleep`.
5. **Add the role-bypass facts.** For each `StaffAccessRight` in `src/Pegasus.Core/Identity/StaffAuthorization.cs:7-21` that an `/api/v1` endpoint uses, assert one positive and one negative case, and add the forged-claim cases: a token whose role claim names a role the user does not hold in the store, a token with no role claim, and a token whose subject is not a `Guid`. All must be refused — `StaffActorFactory.TryCreate` (`src/Pegasus.Core/Actors/StaffActorFactory.cs:8-42`) fails closed and the tests must prove it end to end, not by calling the factory directly.
6. **Add the version-spoofing facts.** Assert that `X-Pegasus-Client-Version` values that are absent, empty, non-numeric, negative, absurdly high, or padded (`00001.0.0`) are handled by the [[DSK-04-06]] filter without ever bypassing the gate, and that the compatibility endpoint stays reachable in every case so a blocked client can still learn the minimum.
7. **Add the log-and-secret redaction facts.** Assert that no access token, refresh token, password or `Authorization` header value appears in the diagnostics log or in any problem body produced by the auth path, and that no problem body distinguishes an unknown username from a wrong password. Include an assertion that `Bootstrap:VerificationAccount` (`src/Pegasus.Web/appsettings.json`) is never accepted on the desktop token endpoint.
8. **Add the audit facts.** For each denial reason code emitted by [[DSK-04-02]], [[DSK-04-03]], [[DSK-04-04]] and [[DSK-04-05]], assert one `SecurityEvent` row exists with the expected `Type`, `Outcome`, `SubjectId` and a non-empty `CorrelationId` — tier 9 requires correlation, not payload dumps.
9. **Operator step — DPAPI store permissions.** On a Windows 11 workstation with the desktop from [[DSK-04-07]] installed, sign in, then record: the full path of the refresh-token store file, `icacls <path>` output showing access limited to the signed-in user, and proof that the file is unreadable by a second local account. Hand back the command transcript and screenshots for the ticket proof. If the store is registry- or credential-locker-backed rather than a file, record the equivalent ACL evidence and say so.
10. **Record any item that cannot be automated** as a dated manual check in the ticket `proof` document with its evidence, rather than leaving the matrix row empty. A row with neither a test nor a recorded check is a failure of this ticket.
11. **Run the suite**: `dotnet test tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --filter "FullyQualifiedName~DesktopToken|FullyQualifiedName~DesktopApiAuthentication|FullyQualifiedName~DesktopSessionRevocation|FullyQualifiedName~DesktopCompatibilityGate|FullyQualifiedName~StaffSignInSecurity"` and attach the output.
12. **Close the loop with the wider suite.** Post the completed matrix into [[DSK-08-11]]'s ticket scratch via Kanmer `append_scratch` so the Phase 8 security set extends this rather than restarting it, and note in the post-implementation report which rows are deferred to that ticket.

## Acceptance conditions

- [ ] Every §22.2 authentication item has either a named test fact or a dated, evidenced manual check in the coverage matrix.
- [ ] Token expiry, refresh rotation, refresh replay, absolute cap and revocation each have a failing-path fact.
- [ ] Each `/api/v1` `StaffAccessRight` has a positive and a negative fact, plus forged-claim cases that are all refused.
- [ ] Version-spoofing inputs never bypass the compatibility gate, and the compatibility endpoint stays reachable while blocked.
- [ ] No token, password or `Authorization` value appears in any log or problem body; no message distinguishes unknown user from wrong password.
- [ ] Every denial reason code has a matching `SecurityEvent` with a correlation id.
- [ ] The DPAPI refresh-store ACL evidence is attached to the ticket proof.

## Verification

- [ ] `dotnet test tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --filter "FullyQualifiedName~DesktopTokenSecurity"` — expected: every fact in the new file passes.
- [ ] `dotnet test tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --filter "FullyQualifiedName~Desktop"` — expected: the whole Phase 2 auth set is green in one run.
- [ ] `icacls <refresh-store-path>` on the test workstation — expected: only the signed-in user has access; transcript attached to proof.
- [ ] The coverage matrix in the ticket `research` document — expected: no §22.2 authentication row is empty.

## Risks and boundaries

- **Azure**: no write. The whole suite runs on the local Test/UAT stack (L-02); ADR-0014 stands and there is no Azure dev/test environment to ask for.
- **Scope boundary**: may add files under `tests/Pegasus.IntegrationTests` and the desktop view-model test project from [[DSK-02-13]]. Must not change production code — a test that only passes after a source change means the defect belongs in a new `fix` ticket, filed and linked, not patched here.
- **Cross-area**: this row spans both halves of plan 04. The gateway facts belong here in `gateway-api`; the DPAPI store evidence depends on the desktop client [[DSK-04-07]] in `desktop-foundation`, so schedule step 9 after that ticket lands and do not block the gateway facts on it.
- **Traps**: (a) do not assert lockout — ADR-0013 clause 12 makes throttling transient, and a test expecting `LockoutEnd` would enshrine the wrong control; (b) `CheckUpdateAvailabilityAsync` returns `Unknown` for a side-loaded MSIX, so any packaging-adjacent check must run against a local `.appinstaller` feed ([[DSK-04-12]]) or be recorded as not applicable; (c) never put a real credential in a test fixture — the plaintext `Bootstrap:VerificationAccount` must not become the desktop test login.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.
