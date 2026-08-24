# Research — GWY-024: DSK-04-14 · Security tests for the token path: expiry, rotation, revocation, role bypass, version spoofing

## Question

Give every authentication item of proposal §22.2's security-test list a test or a recorded manual check: token expiry, refresh rotation, revocation, disabled account, role-bypass attempts, API version spoofing, and the file permissions of the desktop's DPAPI refresh-token store.

## Evidence examined

- Plan row: `docs/desktop/04-auth-session-update-and-startup/README.md` § 5 — `DSK-04-14`
- Plan detail: same file § 3 session failure matrix (the seven rows are the test matrix), § 4 target state and exit gate, § 7 risks
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 22.2 Security tests; § 17.1 Required controls; § 8.4 Session failure handling
- Repository evidence:
  - `tests/Pegasus.IntegrationTests/StaffSignInSecurityTests.cs` — the existing security-test shape (`[Trait("Category", "SqlServer")]`, `LocalDbTestDatabase`, `ConfiguredWebApplicationFactory`) to follow
  - `tests/Pegasus.IntegrationTests/IntakePersistenceIntegrationTests.cs:415` — `LocalDbTestDatabase`; `tests/Pegasus.IntegrationTests/ReadinessEndpointTests.cs:297` — `ConfiguredWebApplicationFactory`
  - `src/Pegasus.Core/Identity/StaffAuthorization.cs:23-60` — the fail-closed right matrix the role-bypass tests must probe
  - `src/Pegasus.Core/Identity/IdentityContracts.cs:100-145` — `SecurityEventType`, `SecurityEvent`, `ISecurityEventWriter`, the observable audit surface
  - `src/Pegasus.Core/Actors/StaffSessionPolicy.cs:9-13` — the 2-hour idle and 8-hour absolute bounds the expiry tests assert
  - `src/Pegasus.Web/appsettings.json` — `Bootstrap:VerificationAccount`, the plaintext verification account that must never be a desktop login
  - `docs/engineering.md:84` — tier 9 definition (role matrix, transient authentication throttling, denial before client construction, correlation, redaction)
- Binding decisions:
  - **L-02** — the whole suite runs against the local Test/UAT stack; asking for an Azure test resource is out of bounds
  - **L-04** — this ticket names its subagent, skills and MCP tools
  - **ADR-0013** clause 12 — transient throttling, no persistent lockout; **ADR-0102** (owed) — the token session under test
- Depends on: `DSK-04-05` — revocation must exist before it can be tested; `DSK-04-07` — the desktop session client and its DPAPI refresh store, which the storage checks inspect (that ticket is in the `desktop-foundation` area)

## Scope and constraints

Proposal §22.2 lists the security tests the conversion owes, and §27 item 5 makes automated and UAT parity evidence a programme exit condition. [[DSK-04-02]], [[DSK-04-03]], [[DSK-04-04]], [[DSK-04-05]] and [[DSK-04-06]] each test their own happy and unhappy paths; nothing yet proves the *set* is complete, and a gap here is invisible until it is exploited. This ticket is the Phase 2 half of the wider security suite [[DSK-08-11]] and supplies the "tokens/secrets pass storage review" row of the Phase 2 exit gate.

- Future owner: `pegasus-gateway-dev`; tests: `pegasus-test-engineer`; independent review: `pegasus-desktop-reviewer`.
- Use `dotnet-webapi`, `optimizing-ef-core-queries` where the ticket changes a query, and `run-tests` for the actual runner profile. The project decision overrides generic “service per endpoint” advice: route handlers translate to existing `Pegasus.Core` ports; no second policy/service layer is introduced.
- The shared EPIC context binds this to versioned `/api/v1` route groups in the existing `Pegasus.Web`, the existing rate-limiter mechanism, an OpenAPI snapshot, and no Azure write.

- Microsoft Learn (fetched 2026-08-24): [ASP.NET Core OpenAPI support](https://learn.microsoft.com/aspnet/core/fundamentals/openapi/overview?view=aspnetcore-10.0) confirms first-party OpenAPI generation. Use the repository’s planned committed snapshot and contract-test flow rather than adding a parallel API documentation path.

- **Azure**: no write. The whole suite runs on the local Test/UAT stack (L-02); ADR-0014 stands and there is no Azure dev/test environment to ask for.
- **Scope boundary**: may add files under `tests/Pegasus.IntegrationTests` and the desktop view-model test project from [[DSK-02-13]]. Must not change production code — a test that only passes after a source change means the defect belongs in a new `fix` ticket, filed and linked, not patched here.
- **Cross-area**: this row spans both halves of plan 04. The gateway facts belong here in `gateway-api`; the DPAPI store evidence depends on the desktop client [[DSK-04-07]] in `desktop-foundation`, so schedule step 9 after that ticket lands and do not block the gateway facts on it.
- **Traps**: (a) do not assert lockout — ADR-0013 clause 12 makes throttling transient, and a test expecting `LockoutEnd` would enshrine the wrong control; (b) `CheckUpdateAvailabilityAsync` returns `Unknown` for a side-loaded MSIX, so any packaging-adjacent check must run against a local `.appinstaller` feed ([[DSK-04-12]]) or be recorded as not applicable; (c) never put a real credential in a test fixture — the plaintext `Bootstrap:VerificationAccount` must not become the desktop test login.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.

## Governing documents

- `docs/frd/frd-04-parties-accounts-and-access.md`

## Planning implication

Reuse the cited boundaries and revalidate the named sources against current `origin/dev` after the ticket is taken. Do not create a compatibility path, duplicate policy, or an unapproved external write.
