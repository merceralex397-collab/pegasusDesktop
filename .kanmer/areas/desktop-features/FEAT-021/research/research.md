# Research — FEAT-021: S21 Password change and account lifecycle

Repository revision read: `bbd1c549` (`git rev-parse --short HEAD`, 2026-08-24). Line numbers from
`grep -n` / `sed -n` at that revision.

## Question

What validation the Core password-change use case actually applies, exactly what the
`MustChangePassword` middleware allows while a change is required, what the settled failure
messages are, and where the current disabled-account text lives — so a native flow can be built
from typed problems instead of a redirect.

## Current behaviour

`src/Pegasus.Web/Pages/Account/PasswordChange.cshtml.cs` (189 lines), `SignOut.cshtml.cs`
(21 lines), and the `MustChangePassword` middleware in `src/Pegasus.Web/Program.cs:875-899`.

**The middleware, read in full (`Program.cs:875-899`):** it runs after `app.UseAuthentication()`
(`:874`) and before `app.UseAuthorization()` (`:900`). For an authenticated request whose endpoint
carries no `IAllowAnonymous` metadata it loads the user and, if `user?.MustChangePassword == true`,
redirects to `/Account/PasswordChange` unless the path is in a **six-entry allow-list**
(`:883-889`): `/Account/PasswordChange`, `/Account/SignOut`, `/css`, `/js`, `/lib`, `/favicon.ico`.
Three of those six exist only because the browser needs stylesheets, scripts and a favicon — the
desktop's equivalent allow-list is therefore two entries, not six.

**The page's own rules (`PasswordChange.cshtml.cs`)**:

- Three bound fields with explicit messages, and a comment at `:15-18` saying why: "the framework
  defaults print bind-property names and CLR type talk at the operator". Required messages at
  `:21`, `:27`, `:34`; `MinLength(8, "The new password must be at least 8 characters.")` at `:28`;
  `Compare(… "The passwords do not match.")` at `:35`.
- An `OperationKey` bound property defaulting to `NewOperationKey()` (`:38-39`), parsed as
  `Guid.TryParseExact(OperationKey, "N", …)` (`:68`) — an invalid key produces "The form has
  expired. Retry the password change." (`:70`).
- A **`Forced` flag** (`:41-50`) distinguishing the gated arrival from a voluntary change, with the
  reasoning written down: "The two are not the same screen. Under the gate every other destination
  is already locked, so the page renders without navigation and states the consequence; a voluntary
  change keeps the application around it." That distinction carries directly into the desktop.
- The Core call is `changeStaffPassword.ExecuteAsync(new(actor, staffId, CurrentPassword, NewPassword, OperationKey))`
  (`:79-81`).
- **Five typed failures**, each with a settled message (`:94-120`) — the comment at `:91-93`
  records why they are not collapsed ("saying 'check the current password and the new password
  requirements' for all of them made the operator guess which one failed"):

  | `StaffPasswordChangeError` | Settled message | Field |
  | --- | --- | --- |
  | `CurrentPasswordInvalid` | "The current password is incorrect." | `CurrentPassword` |
  | `PasswordUnchanged` | "The new password must be different from the current one." | `NewPassword` |
  | `PasswordRejected` | "The new password must be at least 8 characters." | `NewPassword` |
  | `OperationConflict` | "This password-change form was already used. Retry from the current page." | form |
  | *(default)* | "The password could not be changed." | form |

- `StaffAccountNotFound` is handled **before** the switch and returns `Forbid()` (`:86-89`) — it
  never produces a message, so the screen cannot be used to discover whether an account exists.
- `ResetSensitiveInput()` (`:163`ff) clears the password fields on every failure path.
- On success: `signInManager.SignOutAsync()` then an immediate
  `PasswordSignInAsync(userName, NewPassword, isPersistent: false, lockoutOnFailure: false)`
  (`:135-140`), a confirmation in `TempData` and a redirect. The comment at `:146` records that "A
  silent redirect left the operator unsure whether anything happened."

**Sign out (`SignOut.cshtml.cs`)**: `OnGet` redirects to `/Index` (`:10`); `OnPostAsync` calls
`signInManager.SignOutAsync()` and redirects to `/Account/SignIn?signedOut=true` (`:14-20`), with a
comment recording that the confirmation is "a one-time state of the sign-in page, not a page of its
own: a bookmarked confirmation URL would assert that a session had just ended when nothing had
happened."

**Session policy**: `src/Pegasus.Core/Actors/StaffSessionPolicy.cs` — `IdleLifetime` 2 hours (`:9`),
`AbsoluteLifetime` 8 hours (`:10`), `SignInAttemptsPerClientPerMinute` 10 (`:12`),
`SignInAttemptsGlobalPerMinute` 100 (`:13`).

**Parity-matrix rows**: `PAR-02` (`docs/desktop/01-inventory-and-parity/parity-matrix.md:47`,
sign out) and `PAR-03` (`:48`, password change), both `not inventoried`. `PAR-03` names
`~POST /api/v1/session/password` while the endpoint map names
`POST /session/password-change` — a small inconsistency to reconcile when the row is updated; the
endpoint map is authoritative for exact paths.

## Findings

- **The desktop's forced-change gate is a two-entry allow-list, not six.** Three of the web's six
  entries (`/css`, `/js`, `/lib`) and a fourth (`/favicon.ico`) are browser asset paths with no
  desktop equivalent. What survives is: the change-password screen itself, and sign out.
- **The `Forced` distinction is already a settled design decision, with its reasoning in the code**
  (`PasswordChange.cshtml.cs:41-50`). The desktop must reproduce the *behaviour* — no navigation
  under the gate, the consequence stated — not the redirect.
- **The five typed errors are the desktop's problem-type map.** They exist in Core as
  `StaffPasswordChangeError`, so the gateway can translate each to a distinct problem type and the
  desktop can map each to the message already settled. Collapsing them would undo a recorded
  decision (`:91-93`).
- **"No password-policy prose" and the `PasswordRejected` message are compatible.** The eight-
  character rule appears only as a *validation outcome*, never as hint text before the operator
  types. `docs/design/README.md:432-445` bans the hint sentence, not the failure message.
- **The web's success path is a sign-out-then-sign-in-again**, which is exactly what the desktop
  must do differently: it discards its cached tokens and re-authenticates, and other devices fail
  their next refresh with `invalid_grant`. The revocation itself is [[GWY-022]]'s (plan handle
  `DSK-04-05`).
- **`PegasusIdentityUser` is one of only three page-model touches into `Pegasus.Infrastructure`.**
  Plan 05 § 2 records that "50 of the 53 page models import no `Pegasus.Infrastructure` type; the
  three `Pages/Account/*` models reference `PegasusIdentityUser`". `PasswordChange.cshtml.cs:6`
  confirms the `using Pegasus.Infrastructure.Persistence`. The desktop must not inherit that
  coupling — it talks to `/api/v1/session/*` and never to an Identity type.
- **The disabled-account message is not on this page.** `PasswordChange.cshtml.cs` handles
  `StaffAccountNotFound` with `Forbid()` and nothing else; the disabled-account text belongs to the
  session-failure matrix owned by [[FND-044]] (plan handle `DSK-04-08`). This slice renders it; it
  does not invent it.
- **Endpoint shapes are settled** in `docs/desktop/03-gateway-api-and-data/endpoint-map.md`
  § `Session, compatibility, diagnostics`: `POST /session/password-change` — right
  `AccessStaffApplication`, "yes (key)" idempotent, returns "204, refresh tokens revoked";
  `POST /session/logout` — right `AccessStaffApplication`, idempotent, 204;
  `GET /session/me` — returns "actor id, roles, rights, must-change-password flag".
- **`AccessStaffApplication` is the right, and it is the loosest one.**
  `src/Pegasus.Core/Identity/StaffAuthorization.cs:35` resolves it to `actor.Kind == ActorKind.Staff`
  — no role required. That is correct for a password change and worth stating, because a reviewer
  may expect an administrator right here.

### Facts

- `PasswordChange.cshtml.cs` 189 lines, `SignOut.cshtml.cs` 21 lines, `AccessDenied.cshtml.cs`
  7 lines, `SignIn.cshtml.cs` 106 lines (`wc -l`).
- The middleware allow-list has six entries at `Program.cs:883-889`.
- Five `StaffPasswordChangeError` cases plus a default are handled at `:94-120`.
- `StaffSessionPolicy` lifetimes and throttles are as tabulated above.
- `src/Pegasus.Desktop`, `src/Pegasus.Desktop.Infrastructure`, `src/Pegasus.Contracts`,
  `tests/Pegasus.Api.ContractTests` and `tests/Pegasus.Desktop.ViewModelTests` do not exist yet.

### Assumptions

- `A-05-21-1` — the Core `IChangeStaffPassword` use case owns every validation rule, so the desktop
  can mirror the field rules for immediate feedback without becoming a second owner. *Confirm:* read
  `src/Pegasus.Core/Identity/`'s password-change use case at step 2 and record which rules are
  Core's and which are the page's `DataAnnotations`. *If wrong* — if the eight-character minimum
  lives only in the page's `[MinLength(8)]` — it is a page-model rule that must move into Core with
  a characterization test before the desktop relies on it.
- `A-05-21-2` — `password-change-required` and the disabled-account problem exist as **typed**
  problem types, not prose. *Confirm:* read [[GWY-021]]'s (plan handle `DSK-04-04`) merged bearer
  authentication contract and [[FND-044]]'s session-failure matrix. *If wrong:* the desktop cannot
  route the forced state deterministically and the ticket blocks on those siblings.
- `A-05-21-3` — the DPAPI refresh store from [[FND-031]] (plan handle `DSK-02-06`) exposes a clear
  operation the sign-out command can call. *Confirm:* read that ticket's merged interface.
  *If wrong:* sign-out clears the in-memory cache only and the DPAPI clear becomes a defect ticket.
- `A-05-21-4` — `PAR-03`'s `~POST /api/v1/session/password` is a drafting slip and the endpoint map's
  `POST /session/password-change` is the intended path. *Confirm:* with [[GWY-021]] when the row is
  updated. *If wrong:* only the matrix row's text changes.

## Execution placement

Six-question test from `docs/desktop/00-governance-and-workflow/README.md` § 3 (`:169-176`):

| Question | Answer | Evidence |
| --- | --- | --- |
| Shared authority — must several users see and update the same state? | **yes** | A password change invalidates sessions on the operator's *other* devices; the credential and its refresh tokens are one shared state. Lands in the **gateway** (`Pegasus.Web`, L-01), which already hosts Identity and OpenIddict (`reuse-map.md` § `Pegasus.Web`, `Authentication/` row). |
| Unattended execution — must it run with every desktop closed? | **no** | Every action here is operator-initiated. |
| Protected credentials — long-lived secret that must not sit on workstations? | **yes** | Password hashes and refresh-token state are credentials. The **gateway** holds them; the desktop holds only a short-lived access token in memory and a rotated refresh token in the DPAPI store from [[FND-031]]. No password value is ever logged, cached or bundled. |
| Public callback — must an external service call a stable public endpoint? | **no** | Pegasus credentials only; no external identity provider. `entra-app-registration` and `entra-agent-id` are on the do-not-load list in `docs/desktop/12-agent-tooling/skill-routing.md`. |
| Central enforcement — revocation, permissions, audit, invariant independent of the client? | **yes** | Revocation on change, disable and logout must hold regardless of the client (ADR-0102); `StaffAuthorization.IsAuthorized` (`StaffAuthorization.cs:29-57`) fails closed. Lands in the **gateway**. |
| Measured operational advantage — measured evidence central is materially better? | **yes** | Sign-in throttling is measured and central by design — `StaffSessionPolicy.SignInAttemptsPerClientPerMinute = 10` and `SignInAttemptsGlobalPerMinute = 100` (`:12-13`) are enforced across all clients; a per-workstation limiter would not be a limit at all. |

Four "yes" answers, each naming **the gateway** — the existing `Pegasus.Web` Container App under
L-01, not a new Azure resource. The desktop keeps field validation, the deliberate submit and the
forced-state routing. No Azure write.

## Implications

1. **Route the forced state, do not redirect.** `GET /api/v1/session/me` carries the
   must-change-password flag and any `/api/v1` call can return `password-change-required`; the
   startup orchestrator from [[FND-045]] (plan handle `DSK-04-09`) shows the change screen and no
   other navigation is possible. The web's `Forced` distinction (`PasswordChange.cshtml.cs:41-50`)
   becomes a view-model state, and the desktop's allow-list is two entries — this screen, and sign
   out.
2. **Carry the five settled messages across verbatim.** They are a recorded decision with its
   reasoning in the code; re-wording them is a regression, and collapsing them re-creates the
   defect the comment at `:91-93` describes.
3. **Never let the screen disclose account existence.** `StaffAccountNotFound` → `Forbid()` with no
   message (`:86-89`) is the pattern; a wrong current password returns
   `CurrentPasswordInvalid`, which says nothing about whether the account exists.
4. **Prove non-retention, do not assert it.** A tier-9 obligation: no password, token or credential
   in any log file or diagnostics bundle; restrictive ACLs on the DPAPI store file; a revoked
   refresh token unusable. `ResetSensitiveInput()` (`:163`) is the web's precedent for clearing the
   fields; the desktop's equivalent is a view-model fact.
5. **Do not inherit the Identity coupling.** `PasswordChange.cshtml.cs:6` imports
   `Pegasus.Infrastructure.Persistence` for `PegasusIdentityUser`. The desktop talks to
   `/api/v1/session/*` and to nothing else; the dependency-direction facts from [[FND-037]] (plan
   handle `DSK-02-12`) enforce it.

## Open questions

None that belong in an `open-questions` document.

- The typed session problem types and the disabled-account message text — owned by [[GWY-021]]
  (plan handle `DSK-04-04`) and [[FND-044]] (plan handle `DSK-04-08`). Scope boundaries, recorded
  in the plan's *Risks / open questions*.
- Refresh-token revocation on disable, password change, logout and sign-out-everywhere — owned by
  [[GWY-022]] (plan handle `DSK-04-05`); same treatment.
- Whether the eight-character minimum is Core's rule or the page's `DataAnnotations` — resolved by
  reading, at implementation step 2, not by asking. If it is the page's, it moves into Core with a
  characterization test, which is the standing rule in plan 05 § 3, not a new decision.
