# Files — FEAT-021: S21 Password change and account lifecycle

Surveyed at `bbd1c549` (2026-08-24). Paths marked *(created by …)* do not exist yet.

## Where the change lands

| Path | Why |
| --- | --- |
| `src/Pegasus.Contracts/` *(created by [[FND-029]] (plan handle `DSK-02-04`))* | The session DTOs: password-change request (current, new, `operationKey`), the `me` response carrying the must-change-password flag, and the logout request. **No password value is ever logged, cached or included in a diagnostics bundle** — the DTO is marked and the redaction is tested, not assumed. |
| `src/Pegasus.Desktop/` *(created by [[FND-030]] (plan handle `DSK-02-05`))* | `PasswordChangeViewModel` with immediate field validation, a deliberate submit, and a map from each typed problem to its settled message. Plus the disabled-account state and the sign-out command. A `Forced` state mirrors `PasswordChange.cshtml.cs:41-50`: under the gate there is no navigation and the consequence is stated. |
| `src/Pegasus.Desktop.Infrastructure/` *(created by [[FND-031]] (plan handle `DSK-02-06`))* | The session client, the in-memory access-token cache and the DPAPI refresh store's clear operation that sign-out calls. |
| `src/Pegasus.Web/` — the `/api/v1` session group | `POST /session/password-change`, `POST /session/logout`, `GET /session/me` from `docs/desktop/03-gateway-api-and-data/endpoint-map.md` § `Session, compatibility, diagnostics`. Behind `Features:DesktopGateway`. |
| `tests/Pegasus.Api.ContractTests/` *(created by [[TEST-001]] (plan handle `DSK-08-01`))* | Successful change revokes refresh tokens; a wrong current password returns a problem without revealing whether the account exists; replay of the same operation key is safe; a disabled account is refused on the next request; logout revokes the refresh token. |
| `tests/Pegasus.Desktop.ViewModelTests/` *(created by [[FND-038]] (plan handle `DSK-02-13`))* | Validation, forced-change routing before the shell, the disabled-account state, and sign-out clearing **both** caches. |
| The security test set *(owned by [[TEST-011]] (plan handle `DSK-08-11`))* | This slice contributes the credential-path cases: no password or token in any log or diagnostics bundle; restrictive ACLs on the DPAPI store file; a revoked refresh token unusable; changing the password on one session logs out the other. |
| `docs/desktop/01-inventory-and-parity/parity-matrix.md` | Rows `PAR-02` (`:47`, sign out) and `PAR-03` (`:48`, password change), both currently `not inventoried`. Reconcile `PAR-03`'s `~POST /api/v1/session/password` with the endpoint map's `POST /session/password-change` when the row is updated. |
| `docs/frd/frd-13-desktop-operator-experience.md` *(created by area 00)* | The account-lifecycle section, citing FRD-04. |
| `docs/capabilities.md` | `DSK` rows for password change and sign out. |

## Context files

| Path | What it tells the implementer |
| --- | --- |
| `src/Pegasus.Web/Program.cs:875-899` | The whole forced-change gate, and the fact that decides the desktop's design: the allow-list has **six** entries (`:883-889`) — `/Account/PasswordChange`, `/Account/SignOut`, `/css`, `/js`, `/lib`, `/favicon.ico` — of which four are browser asset paths. The desktop's equivalent is two: the change screen and sign out. The middleware sits between `UseAuthentication()` (`:874`) and `UseAuthorization()` (`:900`), which is why it never sees an anonymous endpoint. |
| `src/Pegasus.Web/Pages/Account/PasswordChange.cshtml.cs:41-50` | The `Forced` flag and the reasoning behind it, written in the code: "The two are not the same screen. Under the gate every other destination is already locked, so the page renders without navigation and states the consequence; a voluntary change keeps the application around it." Reproduce the behaviour, not the redirect. |
| `src/Pegasus.Web/Pages/Account/PasswordChange.cshtml.cs:91-120` | The five settled failure messages and the comment saying why they were separated: "saying 'check the current password and the new password requirements' for all of them made the operator guess which one failed." `CurrentPasswordInvalid`, `PasswordUnchanged`, `PasswordRejected`, `OperationConflict` and a default — each with its field and its exact words. Carry them across verbatim. |
| `src/Pegasus.Web/Pages/Account/PasswordChange.cshtml.cs:86-89` | `StaffAccountNotFound` is handled **before** the switch and returns `Forbid()` with no message. This is how the screen avoids disclosing whether an account exists; the desktop must not add a friendlier message here. |
| `src/Pegasus.Web/Pages/Account/PasswordChange.cshtml.cs:15-18`, `:163`ff | The comment explaining why every message is explicit rather than framework-default, and `ResetSensitiveInput()`, which clears the password fields on every failure path — the precedent for the desktop's non-retention rule. |
| `src/Pegasus.Web/Pages/Account/PasswordChange.cshtml.cs:6` | `using Pegasus.Infrastructure.Persistence;` — one of only three page models that touch `Pegasus.Infrastructure` (plan 05 § 2: "50 of the 53 page models import no `Pegasus.Infrastructure` type"). The desktop must **not** inherit this coupling; it talks to `/api/v1/session/*`. |
| `src/Pegasus.Web/Pages/Account/SignOut.cshtml.cs:16-19` | Why the signed-out confirmation is a state of the sign-in screen and not a page: "a bookmarked confirmation URL would assert that a session had just ended when nothing had happened." The desktop equivalent is a one-time state, not a screen. |
| `src/Pegasus.Core/Actors/StaffSessionPolicy.cs:9-13` | `IdleLifetime` 2 h, `AbsoluteLifetime` 8 h, `SignInAttemptsPerClientPerMinute` 10, `SignInAttemptsGlobalPerMinute` 100. The throttles are central by design — a per-workstation limiter would not be a limit. |
| `src/Pegasus.Core/Identity/StaffAuthorization.cs:35` | `AccessStaffApplication => actor.Kind == ActorKind.Staff` — no role required. This is the right on all three session endpoints, and it is deliberately the loosest one: any staff actor may change their own password. |
| `docs/desktop/03-gateway-api-and-data/endpoint-map.md` § `Session, compatibility, diagnostics` | The authoritative routes and two details: `POST /session/password-change` returns "204, refresh tokens revoked", and `GET /session/me` returns "actor id, roles, rights, must-change-password flag". Note the matrix row `PAR-03` names a different path; the endpoint map wins. |
| `docs/frd/frd-04-parties-accounts-and-access.md:13-26` | Staff accounts use Pegasus-managed usernames and passwords with non-reversible hashes "until a separately accepted identity change supersedes that route", and authorization "fails closed without revealing case or source data". |
| `docs/frd/frd-04-parties-accounts-and-access.md:33` | Sign-ins and authentication failures stay in the **security log**, not the permanent action history. A test asserting a password change in the action history would be asserting against the wrong store. |
| `docs/design/README.md:432-445` | "A field is a label and a control, nothing more. No hint sentence under a field, no 'Required.' or 'Optional.' text, no format guidance." Required state is shown visually, never as prose. Note the compatible nuance: the eight-character rule appears only as a *validation outcome* (`PasswordChange.cshtml.cs:107-109`), never as hint text before the operator types. |
| `docs/desktop/12-agent-tooling/skill-routing.md` | `entra-app-registration` and `entra-agent-id` are on the do-not-load list. Pegasus credentials only; there is no Microsoft login in this flow. |

## Ripple effects

- **`openapi/pegasus-v1.json` and the generated client** — three session routes; regenerated in
  this change.
- **[[FND-045]] (plan handle `DSK-04-09`)'s startup orchestrator** gains the forced-change branch;
  a change to the problem type changes that orchestrator too.
- **[[GWY-022]] (plan handle `DSK-04-05`)** owns revocation on change, disable, logout and
  sign-out-everywhere. This slice's acceptance criterion "the change invalidates every other
  session" is only true once that ticket has landed.
- **[[FND-044]] (plan handle `DSK-04-08`)** owns the login screen and the session-failure matrix
  this flow plugs into, including the disabled-account message text.
- **[[FEAT-019]] (plan handle `DSK-05-19`)** is the administrator side of account disablement; the
  message the administrator sees when disabling and the message the disabled operator sees must
  agree.
- **[[TEST-011]] (plan handle `DSK-08-11`)** owns the security test set; this slice contributes the
  credential-path cases and the evidence is shared.
- **`tests/Pegasus.IntegrationTests`** — the existing account web tests must stay green; the Razor
  account pages and the middleware are untouched.
- **`docs/capabilities.md`, `frd-13`, `PAR-02` / `PAR-03`** — updated in the same slice.

## Out of scope

- `src/Pegasus.Web/Program.cs:875-899` — the `MustChangePassword` middleware is **not modified**;
  the web keeps its redirect until cutover, and the removal is [[FEAT-026]]'s (plan handle
  `DSK-05-26`).
- The Razor account pages — untouched.
- The login screen and the session-failure matrix — [[FND-044]] (plan handle `DSK-04-08`).
- Refresh-token revocation mechanics — [[GWY-022]] (plan handle `DSK-04-05`).
- Any Microsoft-account or Entra route — Pegasus credentials only; the Entra skills are on the
  do-not-load list.
- Password reset by an administrator, and account creation — [[FEAT-019]]'s administration surface.
- Azure: no write.
