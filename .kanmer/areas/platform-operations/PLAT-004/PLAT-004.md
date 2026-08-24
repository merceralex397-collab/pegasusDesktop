---
id: PLAT-004
type: ticket
title: >-
  DSK-10-04 · Token and session security tests: expiry, rotation, revocation,
  replay, storage
status: preparing
area: platform-operations
assignee: ''
profile: feature
stageEntered:
  preparing: '2026-08-24T21:21:14.247Z'
labels:
  - desktop-conversion
  - plan-10
  - phase-8
  - tier-9
groups:
  - EPIC-011
  - HZN-009
links: []
docs_todo: true
archived: false
created: '2026-08-24T08:05:04.717Z'
updated: '2026-08-24T21:21:14.247Z'
---

## What

Write the security test set for the desktop token session: access-token expiry, refresh rotation, revocation on account disable and on password change, a replayed refresh token, proof that the access token is never persisted, and proof that the DPAPI-protected refresh blob is bound to the current Windows user.

## Why

Proposal §8 puts the desktop session on a short-lived access token plus a rotated refresh token against the existing Pegasus identity store, §17.1 `:1160-1163` requires the refresh token to be protected by Windows credential storage, the access token to stay in memory, and account revocation to work — and §22.2 `:1610-1621` lists exactly these cases as security tests. The Phase 2 ticket `DSK-04-14` covers the auth path at the time it is built; this ticket is the Phase 8 completion that proves every listed case has a test or a recorded manual check, and that each negative path produces the documented problem type rather than a generic failure. Operator-visible consequence: a disabled leaver keeps working until their access token expires, or a stolen refresh blob works on another machine. Siblings: [[DSK-10-05]], [[DSK-10-07]], [[DSK-10-01]].

## Source of truth

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

## Routing

- **Subagent**: `pegasus-test-engineer` — `.codex/agents/pegasus-test-engineer.toml`
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `code-testing-agent` (dotnet/skills `98f84851`, plugin `dotnet-test`) → `run-tests` (same pin) → `assertion-quality` (same pin) when grading the finished set
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search`) for `System.Security.Cryptography.ProtectedData` `CurrentUser` scope semantics and OpenIddict refresh-token rotation
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout`
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Orientation. Read the plan row, `docs/desktop/04-auth-session-update-and-startup/README.md` § 5 rows `DSK-04-02`, `DSK-04-05`, `DSK-04-07`, `DSK-04-14`, and proposal `:417-466`. Call Kanmer `get_doc_gates` with this ticket's board id, then `take_ticket`.
2. Branch `task/dsk-10-04-token-session-security-tests` from `dev`. Before writing tests, load `test-gap-analysis` output from `DSK-04-14` if it exists and list only the cases it does **not** already cover — duplicating an existing test is a defect under `AGENTS.md` Simplicity rails.
3. In `tests/Pegasus.Api.ContractTests`, add a class `DesktopTokenSecurityTests` with one fact per case: (a) an access token past its lifetime is rejected on `/api/v1` with the documented problem type; (b) a refresh grant returns a *new* refresh token and the old one is then rejected; (c) a replayed (already-rotated) refresh token returns `invalid_grant`; (d) after `IsEnabled` is set false the next `/api/v1` request is rejected on the same request, not after expiry (mirrors `Program.cs:353`); (e) after a password change every outstanding refresh token is rejected; (f) an Automation client token is rejected on `/api/v1`.
4. Assert the audit side effect for each negative case: a `SecurityEvent` with the expected `Type` and `Outcome` was written through `ISecurityEventWriter` (`src/Pegasus.Core/Identity/IdentityContracts.cs:98-137`). A rejection with no security event is a finding, not a pass.
5. Assert the rate-limit path: eleven password-grant attempts in one minute from one client produce `429` with `Retry-After` and a `RateLimited` security event — matching the cookie sign-in limiter behaviour rather than an account lockout (ADR-0013, `SignIn.cshtml.cs:63`).
6. In `tests/Pegasus.Desktop.ViewModelTests` (or the Desktop.Infrastructure test project established by `DSK-02-06`), add `RefreshTokenStoreTests`: round-trip through the DPAPI store; assert the persisted bytes do not contain the plaintext token; assert that a blob protected under a different entropy/scope fails to unprotect and surfaces the named failure rather than an unhandled exception.
7. Add the negative-persistence test: drive a full login through the fake token endpoint, then assert that no file under the app's local data folder and no registry value contains the access token, and that the process's own log fixture contains neither token (this reuses the redaction fixture from [[DSK-10-09]]).
8. Add the "no stored password" assertion demanded by the plan's traps table: assert the credential store exposes no API that persists a password and that the login view model never writes the password field anywhere but the request body.
9. Use `microsoft_docs_search` for `ProtectedData.Protect CurrentUser scope` to confirm what "bound to the user" can and cannot be asserted in-process; where a claim can only be proved by logging on as a second Windows user, write it as a recorded manual check in the ticket's `post-implementation-report` with the exact steps, and say so in the register rather than asserting something the test does not prove.
10. Run `dotnet test ./tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj --configuration Release` and the desktop view-model/infrastructure test project. All new tests green; no existing test changed to make them pass.
11. Load `assertion-quality` and grade the new file: every test must fail for the right reason. Temporarily break one production line per case (for example return the same refresh token instead of rotating) and confirm the matching test goes red, then revert.
12. Update the threat register row "lost or shared workstation session" and "leaked service credential" with this ticket's test names ([[DSK-10-01]]).
13. Record `## Simplification pass` with today's date over the branch diff in the ticket's `plan` document, open the PR into `dev`, and hand review to `pegasus-desktop-reviewer`.

## Acceptance criteria

- [ ] Every §22.2 token/session item has a test or a recorded manual check with steps: expiry, rotation, revocation on disable, revocation on password change, replayed refresh token, disabled account, rate limiting.
- [ ] Each negative path asserts the documented problem type **and** the security-event record.
- [ ] The access token is proved absent from disk, registry and logs after a full login.
- [ ] The DPAPI blob round-trips, holds no plaintext, and a foreign blob fails with a named error rather than an exception.
- [ ] No test asserts a password is stored anywhere; the store exposes no API to do so.

## Verification

- [ ] `dotnet test ./tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj --configuration Release --filter "FullyQualifiedName~DesktopTokenSecurityTests"` — expected: all facts pass.
- [ ] `dotnet test` on the desktop view-model/infrastructure test project filtered to `RefreshTokenStoreTests` — expected: all facts pass.
- [ ] Mutation check log in the post-implementation report — expected: each deliberately broken production line turned exactly one test red.

## Evidence tier

Tier 9 — Security/observability. Here that obliges denial before use and an observable audit record for every refusal, plus a redaction/persistence assertion; a green suite that never exercises a refusal does not satisfy the tier.

## Documentation changes

- `docs/desktop/10-security-observability-performance/threat-register.md` — record the test names against the session and credential rows.
- `docs/engineering.md` § Required evidence tiers — add the desktop token tests as a tier-9 example only if the reviewer agrees it is needed; otherwise `None.`

## Guardrails

- **Azure**: no write. Tests run against the local stack (L-02); asking for an Azure test resource is out of bounds without a new accepted decision (ADR-0014).
- **Scope boundary**: may add tests in `tests/Pegasus.Api.ContractTests` and the desktop test projects, and fixtures they need. Must not change `src/Pegasus.Web` authentication behaviour — a defect found here is a new `fix` ticket, not a silent change under a test ticket. Board placement note: plan 00 § Kanmer board shape assigns no board area to plan 10, so this epic seeds into `platform-operations` (prefix `PLAT`).
- **Traps**: a green test written from the same mistaken reading as the implementation proves only self-consistency (`docs/engineering.md` § Evidence) — hence the mutation check in step 11; "remember me" convenience must never become a stored password; the per-request security-stamp re-check is the whole point of the disable case, so a test that waits for token expiry proves nothing.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the ticket's `plan` document.

## Outcome

_Filled at closeout._
