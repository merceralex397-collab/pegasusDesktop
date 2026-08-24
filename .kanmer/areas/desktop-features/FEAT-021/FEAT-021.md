---
id: FEAT-021
type: ticket
title: DSK-05-21 · S21 Password change and account lifecycle
status: backlog
area: desktop-features
assignee: ''
profile: feature
labels:
  - desktop-conversion
  - plan-05
  - phase-8
  - tier-5
  - tier-9
groups:
  - EPIC-006
  - HZN-009
links: []
blocks:
  - FEAT-022
  - FEAT-025
refs:
  - docs/frd/frd-04-parties-accounts-and-access.md
docs_todo: true
archived: false
created: '2026-08-24T07:59:40.227Z'
updated: '2026-08-24T08:51:44.837Z'
---

## What

Deliver the native account flow: change password (including the forced must-change-on-next-login path), an exact explanation for a disabled account, and sign out — driven by the session problem types, with a password change revoking every other session's refresh token.

## Why

Proposal §8.4 and §13.1 require session failure handling, password lifecycle and account disablement to work with existing Pegasus credentials and no Microsoft login. Today it is `src/Pegasus.Web/Pages/Account/PasswordChange.cshtml.cs` (189 lines), `Pages/Account/SignOut.cshtml.cs` (21 lines) and the `MustChangePassword` redirect middleware in `src/Pegasus.Web/Program.cs:875-899`, which routes an authenticated user to `/Account/PasswordChange` for every path except a small allow-list. The desktop cannot use a redirect, so the forced-change state must be routed before the shell from a typed problem. Siblings: [[DSK-04-05]] supplies revocation, [[DSK-04-08]] the login screen and session-failure matrix, [[DSK-05-19]] the administrator side of account disablement.

## Source of truth

- Plan row: `docs/desktop/05-implementation-and-migration/README.md` § 5 — `DSK-05-21`
- Plan detail: `docs/desktop/05-implementation-and-migration/vertical-slices.md` § `S21 · Password change and account lifecycle (DSK-05-21)`
- Endpoint map: `docs/desktop/03-gateway-api-and-data/endpoint-map.md` § `Session, compatibility, diagnostics` (`POST /session/password-change`, `POST /session/logout`, `GET /session/me`)
- Screen spec: `docs/desktop/06-ui-design/screen-specs.md` § `§13.1 Access and session` → `Change password`
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 8.4 Session failure handling, § 13.1 Access and session, § 17.1 Required controls
- Repository evidence: `src/Pegasus.Web/Pages/Account/PasswordChange.cshtml.cs`, `src/Pegasus.Web/Pages/Account/SignOut.cshtml.cs`, `src/Pegasus.Web/Program.cs:875-899` (the `MustChangePassword` middleware and its allow-list), `src/Pegasus.Core/Identity/` password-change use case, `src/Pegasus.Core/Actors/StaffSessionPolicy.cs`
- Binding decisions: L-01 the gateway owns credentials, revocation and audit; L-02 security verification on the local Test/UAT stack; L-04 routing named on the ticket
- Depends on: `DSK-04-05` refresh-token revocation on disable, password change and logout; `DSK-04-08` the login screen and the session-failure state matrix this flow plugs into

## Routing

- **Subagent**: `winui-dev` — `.codex/agents/winui-dev.toml`; `pegasus-gateway-dev` — `.codex/agents/pegasus-gateway-dev.toml`; `pegasus-test-engineer` — `.codex/agents/pegasus-test-engineer.toml`
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `dotnet-webapi` (dotnet/skills `98f84851`) → `winui-design` (`.codex/skills/winui-design/SKILL.md`, dialog and focus rules) → `winui-dev-workflow` (`.codex/skills/winui-dev-workflow/SKILL.md`) → `run-tests` → `winui-code-review` at review
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search` for OpenIddict token revocation semantics)
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (call `get_doc_gates <id>` before every move; a move crosses at most one gated boundary)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Orient: read the plan row, `vertical-slices.md` § S21, the screen spec Change-password section and `docs/desktop/04-auth-session-update-and-startup/README.md` for the session-failure matrix. Call `get_doc_gates <this ticket id>`, then `take_ticket` with branch `task/dsk-05-21-account-lifecycle` and worktree `../pegasus-worktrees/dsk-05-21-account-lifecycle` from `origin/dev`.
2. Read `src/Pegasus.Web/Pages/Account/PasswordChange.cshtml.cs` and `src/Pegasus.Web/Program.cs:875-899`. Record in `research` the password validation rules the Core use case applies, exactly what the middleware allows while a change is required, and the current disabled-account message text. Record the SHA read.
3. Confirm the endpoints from [[DSK-04-04]] and the endpoint map: `POST /api/v1/session/password-change` (idempotent by operation key, revoking refresh tokens on success), `POST /api/v1/session/logout`, and `GET /api/v1/session/me` carrying the must-change-password flag. Confirm the `password-change-required` and disabled-account problem types exist and are typed, not prose.
4. Add the session DTOs to `src/Pegasus.Contracts`. No password value is ever logged, cached, or included in a diagnostics bundle.
5. Implement `PasswordChangeViewModel` in `src/Pegasus.Desktop` with immediate field validation using the Core rules, a deliberate submit, and mapping of each failure to its typed problem. There is no hint text and no password-policy prose on the screen (`docs/design/README.md`).
6. Route the forced-change state before the shell: when `GET /api/v1/session/me` or any `/api/v1` call returns `password-change-required`, the startup orchestrator from [[DSK-04-09]] shows the change screen and no other navigation is possible. This replaces the web's redirect middleware; do not attempt to reproduce a redirect.
7. Implement the disabled-account state with the exact settled message and no further navigation, and implement sign out to call the logout endpoint, clear the in-memory token cache and clear the DPAPI refresh store from [[DSK-02-06]].
8. Ensure a successful password change invalidates the other sessions: the desktop discards its cached tokens and re-authenticates, and other devices fail their next refresh with `invalid_grant`.
9. Add contract tests in `tests/Pegasus.Api.ContractTests`: successful change revokes refresh tokens, a wrong current password returns a problem without revealing whether the account exists, replay of the same operation key is safe, a disabled account is refused on the next request, and logout revokes the refresh token.
10. Add security tests with [[DSK-08-11]]: no password or token appears in any log file or diagnostics bundle; the DPAPI store file has restrictive ACLs; a revoked refresh token cannot be reused; changing the password on one session logs out the other.
11. Add view-model tests in `tests/Pegasus.Desktop.ViewModelTests` for validation, forced-change routing before the shell, disabled-account state, and sign-out clearing both caches.
12. Update `docs/desktop/01-inventory-and-parity/parity-matrix.md` rows `PAR-02` and `PAR-03`, add the account-lifecycle section to `docs/frd/frd-13-desktop-operator-experience.md`, run the simplification pass over the branch diff, record it under a dated `## Simplification pass` heading, then open the PR into `dev`.

## Acceptance criteria

- [ ] A password can be changed from the desktop and the change invalidates every other session.
- [ ] The must-change-password state is routed before the shell and blocks all other navigation.
- [ ] The disabled-account message is exact and offers no further navigation.
- [ ] Sign out revokes the refresh token and clears both the in-memory cache and the DPAPI store.
- [ ] No password, token or credential appears in a log or a diagnostics bundle.
- [ ] The screen carries no hint text and no password-policy prose.

## Verification

- [ ] `dotnet test ./tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj --configuration Release --no-build` — expected: change, revoke, replay, disabled-account and logout facts pass.
- [ ] `dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release --no-build` — expected: validation, forced-change routing, disabled state and sign-out facts pass.
- [ ] `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --no-build --filter "Category!=Corpus&Category!=Browser"` — expected: existing account web tests stay green.
- [ ] Security-test record in the ticket proof — expected: clean secret scan over logs and diagnostics bundle, restrictive DPAPI store ACLs, revoked token unusable.

## Evidence tier

Tier 5 — Web/API/MCP caller. Tier 9 — Security/observability.
Tier 5 obliges route-level evidence that the session endpoints reach Core with validation, idempotency and exception translation; tier 9 obliges the role-matrix, transient-authentication-throttling, denial-before-client-construction, redaction and bounded-failure evidence for the credential path.

## Documentation changes

- `docs/desktop/01-inventory-and-parity/parity-matrix.md` — rows `PAR-02` (sign out) and `PAR-03` (password change)
- `docs/frd/frd-13-desktop-operator-experience.md` — account-lifecycle section, citing FRD-04
- `docs/capabilities.md` — `DSK` rows for password change and sign out

## Guardrails

- **Azure**: no write.
- **Scope boundary**: may touch `src/Pegasus.Desktop`, `src/Pegasus.Desktop.Infrastructure`, `src/Pegasus.Contracts`, the `/api/v1` session group in `src/Pegasus.Web` and the test projects. Must not modify the `MustChangePassword` middleware or the Razor account pages — the web keeps its redirect until cutover.
- **Traps**: the desktop has no redirect — the forced-change state is a typed problem routed before the shell; a failure message must not disclose whether an account exists; no password-policy prose on the screen (`docs/design/README.md`); Pegasus credentials only — `entra-app-registration` and `entra-agent-id` are on the do-not-load list in `docs/desktop/12-agent-tooling/skill-routing.md`; `Features:DesktopGateway` must be enabled in tests.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.

## Outcome

_Filled at closeout._
