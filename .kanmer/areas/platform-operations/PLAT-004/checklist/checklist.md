# Checklist — PLAT-004

## Implementation

- [ ] 1. Orientation. Read the plan row, `docs/desktop/04-auth-session-update-and-startup/README.md` § 5 rows `DSK-04-02`, `DSK-04-05`, `DSK-04-07`, `DSK-04-14`, and proposal `:417-466`. Call Kanmer `get_doc_gates` with this ticket's board id, then `take_ticket`.

- [ ] 2. Branch `task/dsk-10-04-token-session-security-tests` from `dev`. Before writing tests, load `test-gap-analysis` output from `DSK-04-14` if it exists and list only the cases it does **not** already cover — duplicating an existing test is a defect under `AGENTS.md` Simplicity rails.

- [ ] 3. In `tests/Pegasus.Api.ContractTests`, add a class `DesktopTokenSecurityTests` with one fact per case: (a) an access token past its lifetime is rejected on `/api/v1` with the documented problem type; (b) a refresh grant returns a *new* refresh token and the old one is then rejected; (c) a replayed (already-rotated) refresh token returns `invalid_grant`; (d) after `IsEnabled` is set false the next `/api/v1` request is rejected on the same request, not after expiry (mirrors `Program.cs:353`); (e) after a password change every outstanding refresh token is rejected; (f) an Automation client token is rejected on `/api/v1`.

- [ ] 4. Assert the audit side effect for each negative case: a `SecurityEvent` with the expected `Type` and `Outcome` was written through `ISecurityEventWriter` (`src/Pegasus.Core/Identity/IdentityContracts.cs:98-137`). A rejection with no security event is a finding, not a pass.

- [ ] 5. Assert the rate-limit path: eleven password-grant attempts in one minute from one client produce `429` with `Retry-After` and a `RateLimited` security event — matching the cookie sign-in limiter behaviour rather than an account lockout (ADR-0013, `SignIn.cshtml.cs:63`).

- [ ] 6. In `tests/Pegasus.Desktop.ViewModelTests` (or the Desktop.Infrastructure test project established by `DSK-02-06`), add `RefreshTokenStoreTests`: round-trip through the DPAPI store; assert the persisted bytes do not contain the plaintext token; assert that a blob protected under a different entropy/scope fails to unprotect and surfaces the named failure rather than an unhandled exception.

- [ ] 7. Add the negative-persistence test: drive a full login through the fake token endpoint, then assert that no file under the app's local data folder and no registry value contains the access token, and that the process's own log fixture contains neither token (this reuses the redaction fixture from [[DSK-10-09]]).

- [ ] 8. Add the "no stored password" assertion demanded by the plan's traps table: assert the credential store exposes no API that persists a password and that the login view model never writes the password field anywhere but the request body.

- [ ] 9. Use `microsoft_docs_search` for `ProtectedData.Protect CurrentUser scope` to confirm what "bound to the user" can and cannot be asserted in-process; where a claim can only be proved by logging on as a second Windows user, write it as a recorded manual check in the ticket's `post-implementation-report` with the exact steps, and say so in the register rather than asserting something the test does not prove.

- [ ] 10. Run `dotnet test ./tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj --configuration Release` and the desktop view-model/infrastructure test project. All new tests green; no existing test changed to make them pass.

- [ ] 11. Load `assertion-quality` and grade the new file: every test must fail for the right reason. Temporarily break one production line per case (for example return the same refresh token instead of rotating) and confirm the matching test goes red, then revert.

- [ ] 12. Update the threat register row "lost or shared workstation session" and "leaked service credential" with this ticket's test names ([[DSK-10-01]]).

- [ ] 13. Record `## Simplification pass` with today's date over the branch diff in the ticket's `plan` document, open the PR into `dev`, and hand review to `pegasus-desktop-reviewer`.

## Verification

- [ ] `dotnet test ./tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj --configuration Release --filter "FullyQualifiedName~DesktopTokenSecurityTests"` — expected: all facts pass.
- [ ] `dotnet test` on the desktop view-model/infrastructure test project filtered to `RefreshTokenStoreTests` — expected: all facts pass.
- [ ] Mutation check log in the post-implementation report — expected: each deliberately broken production line turned exactly one test red.

## Progress notes

Record only factual progress here; unresolved decisions remain in `open-questions`.
