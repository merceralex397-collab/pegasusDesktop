# Checklist — FEAT-021: S21 Password change and account lifecycle

One box per plan step, in plan order. Tick with `set_ticket_doc`; append progress notes below.

## Orientation and evidence gathering

- [ ] Read plan 05 § S21, the screen spec Change-password section and `docs/desktop/04-auth-session-update-and-startup/README.md` for the session-failure matrix; call `get_doc_gates FEAT-021`; `take_ticket` with branch `task/dsk-05-21-account-lifecycle` and worktree `../pegasus-worktrees/dsk-05-21-account-lifecycle` from `origin/dev`
- [ ] Confirm `entra-app-registration` and `entra-agent-id` are not loaded (do-not-load list, `docs/desktop/12-agent-tooling/skill-routing.md`)
- [ ] Record in `research` which password rules the Core use case applies and which live only in the page's `DataAnnotations`, settling whether `[MinLength(8)]` (`PasswordChange.cshtml.cs:28`) is Core's rule
- [ ] If the eight-character minimum is a page-model rule, move it into Core with a characterization test before the desktop mirrors it
- [ ] Record the middleware allow-list exactly (`Program.cs:883-889`, six entries, four of them browser asset paths) and the desktop's two-entry equivalent
- [ ] Record the five settled `StaffPasswordChangeError` messages and their fields (`PasswordChange.cshtml.cs:94-120`), and the SHA read

## Contracts and gateway

- [ ] Confirm `POST /api/v1/session/password-change`, `POST /api/v1/session/logout` and `GET /api/v1/session/me` with [[GWY-021]]
- [ ] Confirm `password-change-required` and the disabled-account problem exist as **typed** problem types, not prose
- [ ] Add the session DTOs to `src/Pegasus.Contracts` and mark that no password value is logged, cached or bundled
- [ ] Regenerate `openapi/pegasus-v1.json` and the generated client in this change

## Desktop flow

- [ ] Implement `PasswordChangeViewModel` with immediate field validation and a deliberate submit
- [ ] Map each typed problem to its settled message, attached to the same field the web attaches it to
- [ ] Clear the password fields on every failure path (the `ResetSensitiveInput()` precedent, `PasswordChange.cshtml.cs:163`)
- [ ] Confirm the screen carries no hint text and no password-policy prose; the eight-character rule appears only as a validation outcome
- [ ] Route the forced-change state before the shell through [[FND-045]]'s startup orchestrator, with no other navigation possible
- [ ] Carry the `Forced` distinction as a view-model state (`PasswordChange.cshtml.cs:41-50`) — under the gate, no navigation and the consequence stated
- [ ] Confirm no redirect-style navigation was introduced anywhere in the flow
- [ ] Render the disabled-account state with the exact message from [[FND-044]]'s session-failure matrix and no further navigation
- [ ] Implement sign out: call the logout endpoint, clear the in-memory token cache, clear the DPAPI refresh store from [[FND-031]]
- [ ] Make the signed-out confirmation a one-time state rather than a screen (`SignOut.cshtml.cs:16-19`)
- [ ] Confirm [[GWY-022]] has landed refresh-token revocation before claiming that a change invalidates other sessions
- [ ] Implement the post-change behaviour: discard cached tokens and re-authenticate; other devices fail their next refresh with `invalid_grant`
- [ ] Confirm `src/Pegasus.Desktop*` references no `Pegasus.Infrastructure` Identity type

## Evidence

- [ ] Add contract tests: successful change revokes refresh tokens; wrong current password returns a problem without revealing whether the account exists; operation-key replay is safe; a disabled account is refused on the next request; logout revokes the refresh token
- [ ] Assert the security-log record rather than an action-history record (FRD-04 `:33`)
- [ ] Enable `Features:DesktopGateway` explicitly in every new contract test
- [ ] Add security tests with [[TEST-011]]: no password or token in any log or diagnostics bundle
- [ ] Add security tests: restrictive ACLs on the DPAPI store file; a revoked refresh token cannot be reused; changing the password on one session logs out the other
- [ ] Add view-model tests for validation, forced-change routing before the shell, the disabled-account state and sign-out clearing both caches
- [ ] Update `docs/desktop/01-inventory-and-parity/parity-matrix.md` rows `PAR-02` (`:47`) and `PAR-03` (`:48`), reconciling `PAR-03`'s path with the endpoint map
- [ ] Add the account-lifecycle section to `docs/frd/frd-13-desktop-operator-experience.md` citing FRD-04, and the `DSK` rows to `docs/capabilities.md`
- [ ] Run the simplification pass over the branch diff and record it under a dated `## Simplification pass` heading in the plan
- [ ] **Verification run (this box produces `proof`)** — `dotnet test ./tests/Pegasus.Api.ContractTests/…`, `./tests/Pegasus.Desktop.ViewModelTests/…` and `./tests/Pegasus.IntegrationTests/… --filter "Category!=Corpus&Category!=Browser"`, all `--configuration Release --no-build`; attach the three outputs and the security-test record
- [ ] Open the PR into `dev`

## Progress notes

(append with `set_ticket_doc(doc: "checklist", append: true)`)
