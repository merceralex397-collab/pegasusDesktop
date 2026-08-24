# Research — PLAT-004

## Question

Write the security test set for the desktop token session: access-token expiry, refresh rotation, revocation on account disable and on password change, a replayed refresh token, proof that the access token is never persisted, and proof that the DPAPI-protected refresh blob is bound to the current Windows user.

## Findings

- Plan row: `docs/desktop/10-security-observability-performance/README.md` § 5 — `DSK-10-04`
- Plan detail: same file § 2 (Facts — Identity, rate limiting, security-stamp re-check), § 3 (ADR-0102 row), § 7 ("'Remember me' convenience turning into stored passwords")
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 8 Authentication and authorization `:417-466`; § 17.1 `:1153-1172`; § 22.2 Security tests `:1608-1621`
- Repository evidence:
  - `src/Pegasus.Web/Program.cs:262-327` — ASP.NET Core Identity registration and the sign-in rate limiter (ADR-0013: rate limiting, not lockout)
  - `src/Pegasus.Web/Program.cs:353` — `SecurityStampValidatorOptions.ValidationInterval = TimeSpan.Zero`, the per-request re-check the bearer path must match
  - `src/Pegasus.Web/Pages/Account/SignIn.cshtml.cs:63` — `lockoutOnFailure: false`
  - `src/Pegasus.Core/Identity/IdentityContracts.cs:98-137` — `SecurityEvent` (`SignIn`, `PasswordChanged`, `Token`, `RateLimited`, `SecurityStampChanged`) and `ISecurityEventWriter`, the audit the tests assert on
  - `tests/Pegasus.IntegrationTests` — the existing `WebApplicationFactory` test style to copy
  - New: `tests/Pegasus.Api.ContractTests` (created by `DSK-08-01`) and the desktop credential store in `src/Pegasus.Desktop.Infrastructure` (created by `DSK-02-06`)
- Binding decisions:
  - **ADR-0102** (to be authored) — existing Pegasus credentials and identity store; desktop session = short-lived access token + rotated refresh token.
  - **L-02** — tests run on the local production-mimicking stack; there is no Azure test environment.
- Depends on: `DSK-04-02` (OpenIddict `pegasus-desktop` client and token handler), `DSK-04-05` (revocation on disable, password change, logout), `DSK-04-07` (desktop session client and DPAPI refresh store).

## Implications for this ticket

Proposal §8 puts the desktop session on a short-lived access token plus a rotated refresh token against the existing Pegasus identity store, §17.1 `:1160-1163` requires the refresh token to be protected by Windows credential storage, the access token to stay in memory, and account revocation to work — and §22.2 `:1610-1621` lists exactly these cases as security tests. The Phase 2 ticket `DSK-04-14` covers the auth path at the time it is built; this ticket is the Phase 8 completion that proves every listed case has a test or a recorded manual check, and that each negative path produces the documented problem type rather than a generic failure. Operator-visible consequence: a disabled leaver keeps working until their access token expires, or a stolen refresh blob works on another machine. Siblings: [[DSK-10-05]], [[DSK-10-07]], [[DSK-10-01]].

## Boundaries and assumptions

- **Azure**: no write. Tests run against the local stack (L-02); asking for an Azure test resource is out of bounds without a new accepted decision (ADR-0014).
- **Scope boundary**: may add tests in `tests/Pegasus.Api.ContractTests` and the desktop test projects, and fixtures they need. Must not change `src/Pegasus.Web` authentication behaviour — a defect found here is a new `fix` ticket, not a silent change under a test ticket. Board placement note: plan 00 § Kanmer board shape assigns no board area to plan 10, so this epic seeds into `platform-operations` (prefix `PLAT`).
- **Traps**: a green test written from the same mistaken reading as the implementation proves only self-consistency (`docs/engineering.md` § Evidence) — hence the mutation check in step 11; "remember me" convenience must never become a stored password; the per-request security-stamp re-check is the whole point of the disable case, so a test that waits for token expiry proves nothing.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the ticket's `plan` document.

## Research conclusion

The existing ticket evidence identifies the implementation target, routing, and verification. This research does not create or link a planned canonical governing document; `docs_todo` remains accurate until such a document exists.
