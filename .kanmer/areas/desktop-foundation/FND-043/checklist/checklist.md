# Checklist — FND-043: Desktop session client

One box per plan step, in plan order. Tick a box only when the thing it names is true in the
worktree.

- [ ] **Orient.** Read `docs/desktop/04-auth-session-update-and-startup/README.md` § 3 (decisions 1–3 and 8, and the session-failure matrix) and § 7 in full; load `pegasus-desktop`, then `winui-dev-workflow`.
- [ ] **Take the ticket.** `get_doc_gates FND-043`, `take_ticket FND-043`, branch `task/desktop-session-client` created from `origin/dev`.
- [ ] **Read the two prerequisite projects and record the real type names.** Find the credential-store interface and the existing header `DelegatingHandler` in `src/Pegasus.Desktop.Infrastructure` ([[FND-031]]) and the problem-details envelope in `src/Pegasus.Contracts` ([[FND-029]]); write the actual names into the plan under a dated note (research assumption A-04-07-1).
- [ ] **Add `Session/ISessionClient.cs`** with `SignInAsync`, `RefreshAsync`, `SignOutAsync` and a read-only `CurrentAccessToken` backed only by an in-memory field — under a `Session/` capability folder, not `Common`/`Helpers`/`Utilities`/`Services`.
- [ ] **Confirm the OAuth form-field names** with `microsoft_docs_search` for the resource-owner password grant before writing them, and record the source in the plan.
- [ ] **Implement the password grant** in `Session/SessionClient.cs`: `POST /connect/token`, `application/x-www-form-urlencoded`, `grant_type=password`, `username`, `password`, `client_id=pegasus-desktop`, `scope=pegasus.desktop offline_access`; parse `access_token`, `refresh_token`, `expires_in`, `error`, `error_description`. No browser round trip anywhere in the path.
- [ ] **Implement `RefreshAsync`** as `grant_type=refresh_token`, persisting the **new** handle on every refresh, with no client-side 8-hour timer and no password persisted under any name.
- [ ] **Persist the handle only through [[FND-031]]'s DPAPI store** (`ProtectedData`, `DataProtectionScope.CurrentUser`, under `ApplicationData.Current.LocalFolder`); no second store is added.
- [ ] **Write the DPAPI test that reads the file back** and asserts the on-disk bytes do **not** contain the plaintext handle — not merely that the round trip returns the same value.
- [ ] **Add `Session/SessionAuthorizationHandler.cs`**: attaches `Authorization: Bearer` only; on `401` with `WWW-Authenticate: Bearer error="invalid_token"` refreshes once and retries once; a second `401` is a session failure. `WWW-Authenticate` is parsed directly — no `Microsoft.AspNetCore.*` reference.
- [ ] **Add `Session/SessionFailure.cs`** with exactly seven values — `AccessTokenExpired`, `RefreshRevoked`, `AccountDisabled`, `PasswordChangeRequired`, `ClientUnsupported` (with `minimumVersion`), `Unreachable`, `RateLimited` (with `Retry-After` seconds) — and no eighth.
- [ ] **Extend [[FND-032]]'s log redactor** with `access_token`, `refresh_token`, `password`, `Authorization` and `Set-Cookie`; confirm no second redactor was created.
- [ ] **Add the log-redaction test**: a full sign-in against a stubbed handler leaves none of those five literals in the rolling log file.
- [ ] **Register in the host.** `ISessionClient`, the DPAPI store and `SessionAuthorizationHandler` registered in `src/Pegasus.Desktop/App.xaml.cs` and wired into the named gateway `HttpClient`; [[FND-038]]'s host fixture resolves all three.
- [ ] **Write the success test**: sign-in stores a refresh handle and no password.
- [ ] **Write the refresh test**: an expired access token triggers **exactly one** refresh and **exactly one** retry — assert the counts, not just the outcome.
- [ ] **Write the revocation test**: `invalid_grant` clears the store and reports `RefreshRevoked`.
- [ ] **Write the rate-limit test**: `429` reports `RateLimited` carrying the `Retry-After` value.
- [ ] **Write the transport test**: a transport exception reports `Unreachable` and **never** an invalid-credential state.
- [ ] **Reuse [[FND-038]]'s fakes** — `FixedTimeProvider`, `FakeGatewayClient`, `InMemoryCredentialStore` — with no fourth fake added; confirm with a `grep` over the new test folder.
- [ ] **Prove it against the local stack.** `pwsh ./scripts/Invoke-LocalDevelopment.ps1 -Action Start`, point the desktop `local` channel at the printed Web readiness URL, launch with `.codex/skills/winui-dev-workflow/BuildAndRun.ps1`, sign in with the local Administrator, and capture the correlation id from the rolling log — or record plainly that [[GWY-019]] has not landed and the live check could not run.
- [ ] **Check the two conditional documentation edits** and record which gate was met: `docs/current-architecture.md` § Authentication and authorization boundary (waits on [[GWY-019]]) and the `docs/capabilities.md` `DSK-03` row (waits on [[FND-008]]).
- [ ] **Run the simplification pass** over this branch's own diff and record it under a dated `## Simplification pass` heading in the plan document.
- [ ] **Verification / proof.** Run `dotnet test tests/Pegasus.Desktop.ViewModelTests` (all green, zero skipped), `dotnet build Pegasus.slnx -c Release` on Windows (`0 Warning(s)`), and `dotnet test tests/Pegasus.ArchitectureTests` ([[FND-037]]'s desktop boundary facts still green); capture every output as `test-output` and `command-log` proof; state in it that the fake-handler tests prove the client's branches and **not** the flow against the gateway, that `PAR-02` (sign-out) is marked *not inventoried* in the parity matrix, and whether the [[FND-031]] type names matched. Open the PR into `dev`.

## Progress notes
