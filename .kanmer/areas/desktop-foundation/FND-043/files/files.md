# Files — FND-043 (plan handle `DSK-04-07`)

Surveyed 2026-08-24 against fork `main`. Paths that do not exist yet name the ticket that
creates them. Verified with `ls src/`, `ls tests/` and `grep -n`.

## Where the change lands

| Path | Why |
| --- | --- |
| `src/Pegasus.Desktop.Infrastructure/Session/ISessionClient.cs` | **New** (project created by [[FND-031]], plan handle `DSK-02-06`). `SignInAsync(userName, password, ct)`, `RefreshAsync(ct)`, `SignOutAsync(ct)` and a read-only `CurrentAccessToken` that is only ever an in-memory field. A `Session/` **capability** folder — never `Common`, `Helpers`, `Utilities` or `Services` (`docs/engineering.md:106-111`). |
| `src/Pegasus.Desktop.Infrastructure/Session/SessionClient.cs` | **New.** The `POST /connect/token` calls: `grant_type=password` with `username`, `password`, `client_id=pegasus-desktop`, `scope=pegasus.desktop offline_access`; and `grant_type=refresh_token` with the stored handle. Parses `access_token`, `refresh_token`, `expires_in`, and on failure `error` / `error_description`. **What could break:** a guessed form-field name silently produces `invalid_request` for every login. |
| `src/Pegasus.Desktop.Infrastructure/Session/SessionFailure.cs` | **New.** A closed set of exactly seven values, one per row of the area 04 session-failure matrix: `AccessTokenExpired`, `RefreshRevoked`, `AccountDisabled`, `PasswordChangeRequired`, `ClientUnsupported` (carrying `minimumVersion`), `Unreachable`, `RateLimited` (carrying `Retry-After` seconds). |
| `src/Pegasus.Desktop.Infrastructure/Session/SessionAuthorizationHandler.cs` | **New** `DelegatingHandler`. Attaches `Authorization: Bearer <access token>`; on a `401` carrying `WWW-Authenticate: Bearer error="invalid_token"` refreshes **once** and retries **once**. A second `401` is a session failure, never a loop. |
| `src/Pegasus.Desktop.Infrastructure/` — the DPAPI store | **Existing, consumed not created.** [[FND-031]] owns `ProtectedData` / `DataProtectionScope.CurrentUser` under `ApplicationData.Current.LocalFolder`. This ticket writes the refresh handle **through** it and adds no second store. |
| `src/Pegasus.Desktop.Infrastructure/` — the log redactor | **Existing, extended.** [[FND-032]] (plan handle `DSK-02-07`) owns the bounded rolling sink with redaction; add `access_token`, `refresh_token`, `password`, `Authorization` and `Set-Cookie` to its list. A second redactor is the third-copy rule applied to logging (`docs/engineering.md:194-199`). |
| `src/Pegasus.Desktop/App.xaml.cs` | **DI registration only.** Register `ISessionClient`, the DPAPI store and `SessionAuthorizationHandler`, wiring the handler into the named `HttpClient` that talks to the gateway. The generic host itself is [[FND-032]]'s. |
| `tests/Pegasus.Desktop.ViewModelTests/Session/*.cs` | **New tests** in the project [[FND-038]] (plan handle `DSK-02-13`) creates. Sign-in stores a handle and no password; expired access token → exactly one refresh and one retry; `invalid_grant` clears the store and reports `RefreshRevoked`; `429` reports `RateLimited` with the `Retry-After` value; transport exception reports `Unreachable` and **never** invalid credentials; plus the DPAPI round-trip and the log-redaction assertion. |
| `docs/current-architecture.md` § Authentication and authorization boundary | **Conditional.** Record the desktop token flow beside the cookie flow **only once** [[GWY-019]] (plan handle `DSK-04-02`) has landed the server half; otherwise note the dependency in the plan and write nothing. |
| `docs/capabilities.md` | **Conditional.** A `DSK-03` (desktop sign-in and session) row with canonical owner FRD-13/ADR-0102 — **only if** [[FND-008]] (plan handle `DSK-00-08`) has already created the `DSK` capability family; otherwise leave the row to that ticket. |

## Context files

| Path | What it tells the implementer |
| --- | --- |
| `src/Pegasus.Web/Mcp/AutomationMcp.cs:25,35,36` | The three constants that decide this client's shape: `TokenEndpointPath = "/connect/token"` (the URL to post to), `AccessTokenLifetime = 10 minutes` (the constant family the staff token reuses) and `RefreshTokenLifetime = 14 days` — which is the Automation client's and is **explicitly not** the staff value. Reading this file is how you avoid inheriting the 14 days by copy. |
| `src/Pegasus.Web/Mcp/AutomationMcpExtensions.cs:33-60` | The live OpenIddict composition — EF Core stores on `PegasusDbContext`, client-credentials + authorization-code/PKCE + refresh-token flows, scopes `automation.*`, and **ephemeral** encryption/signing keys. It tells you two things: the server this client talks to already exists, and the keys are the thing [[GWY-019]] must change to `UseDataProtection()` or every gateway release will sign your users out. |
| `src/Pegasus.Core/Actors/StaffSessionPolicy.cs:9-13` | The staff numbers, transport-neutral by design: `IdleLifetime` 2 h, `AbsoluteLifetime` 8 h, 10 sign-in attempts per client per minute, 100 global. It also tells you these are the **gateway's** to enforce — the class comment says transport middleware stays responsible for its own concerns — so the client must not re-implement the 8-hour cap. |
| `src/Pegasus.Web/Pages/Account/SignIn.cshtml.cs:62-64,73-74,83-85` | What "the same credentials" actually means today: an `IsEnabled` short-circuit **before** `CheckPasswordSignInAsync(…, lockoutOnFailure: false)`; the exact operator sentence for a bad credential ("The username or password is incorrect. If your access has changed, contact an administrator.") which the native login screen should not rewrite; and the `MustChangePassword` redirect that becomes the `password-change-required` problem type. |
| `src/Pegasus.Web/Program.cs:370-375` | The cookie the desktop replaces — `__Host-Pegasus`, `HttpOnly`, `SameSite=Strict`, `SecurePolicy=Always`. Useful as the security bar to match, not as a thing to call: the desktop path issues no cookie at all. |
| `docs/desktop/03-gateway-api-and-data/README.md:167` | The **thirteen** stable `urn:pegasus:problem:<slug>` values. Four of them map to `SessionFailure` values here (`client-unsupported`, `password-change-required`, `account-disabled`, `rate-limited`); the other nine are other tickets'. This is the list that makes "do not add a value the matrix does not list" checkable. |
| `docs/desktop/03-gateway-api-and-data/README.md:168` | `X-Correlation-Id` is accepted or generated, echoed and logged; `X-Pegasus-Client-Version` is **required on every `/api/v1` request** and its absence maps to `client-unsupported`. This is why the session handler must not also set headers — [[FND-031]]'s handler already does, on every call including unauthenticated ones. |
| `docs/desktop/04-auth-session-update-and-startup/README.md` § 3 *Session failure matrix* | The seven rows, each with its gateway signal and its required desktop behaviour. The row that matters most: a transport exception is the **disconnected** state and is "never shown as bad credentials". |
| `docs/desktop/04-auth-session-update-and-startup/README.md` § 3 decisions 1–2 | The grant, the client id, the scopes, and the reason there is **no browser round trip**: the login screen stays native (§ 8.1). If you find yourself launching a browser, you have taken the wrong flow. |
| `docs/desktop/04-auth-session-update-and-startup/README.md` § 7 | Two traps this ticket can trigger from the client side: the plaintext `Bootstrap:VerificationAccount` in `src/Pegasus.Web/appsettings.json` must never be the desktop test login in production, and there is **no password storage on the desktop, not even "remember me"**. |
| `docs/desktop/02-architecture-and-foundation/README.md` § 3 decision 6 | Why DPAPI and not `PasswordVault`: the refresh handle may exceed what Credential Locker guidance calls a "password", and DPAPI has no count or size limit. The access token stays in memory. |
| `docs/desktop/01-inventory-and-parity/parity-matrix.md:46-47` | `PAR-01` (sign-in) and `PAR-02` (sign-out). `PAR-02` is marked **not inventoried** with its test column reading `to locate`, so the sign-out half carries less recorded evidence than sign-in — worth stating in the proof rather than implying parity was fully mapped. |
| `docs/frd/frd-04-parties-accounts-and-access.md:15,25,31` | The ticket's `refs` document. `:15` — Pegasus-managed usernames and passwords with non-reversible hashes, "until a separately accepted identity change supersedes that route", which is the authority for *not* introducing Microsoft login. `:25` — authorization is enforced in Core and at every caller boundary and fails closed. `:31` — sign-ins and authentication failures belong in the **security log**, not business history. |
| `docs/engineering.md:106-111` § Capability organization | No `Common`/`Helpers`/`Utilities`/`Services` folder, and `V2`/`New`/`Manager`/`Helper`/`Util` do not justify a layer. This is why the new types go under `Session/`. |
| `docs/engineering.md:194-199` § Test support | One fake per concept, `internal`, in the shared driver. [[FND-038]] already ships `FakeGatewayClient`, `FixedTimeProvider` and `InMemoryCredentialStore`; use them rather than adding a fourth fake. |
| `Directory.Build.props:6-7` | `TreatWarningsAsErrors=true` and `AnalysisLevel=latest-recommended` apply to these new files. Nullable-clean or it does not compile. |
| `scripts/Invoke-LocalDevelopment.ps1:3-4` | `-Action` accepts `Start`, `Status`, `Smoke`, `Stop`, `Reset`. `Start` is the live check in step 13; the script prints the Web readiness URL the desktop `local` channel configuration must point at. |

## Ripple effects

- **Blocked tickets unblock.** The board records this ticket blocking [[FND-044]] (plan handle
  `DSK-04-08`, the login screen), [[FND-045]] (`DSK-04-09`, the startup orchestrator),
  [[FND-047]] (`DSK-04-11`, connectivity), [[FND-050]] (`DSK-04-15`, the Phase 2 exit review),
  [[GWY-024]] (`DSK-04-14`, token-path security tests), [[FEAT-001]] and [[PLAT-004]]
  (`DSK-10-04`, token and session security tests). Every one of them consumes `ISessionClient`
  or `SessionFailure`, so the enum's seven values are a contract, not an implementation
  detail.
- **The log redactor's list grows** — [[FND-032]] owns the sink; adding five literals to its
  redaction list touches its tests as well as this ticket's.
- **The generic host's registration grows** — `src/Pegasus.Desktop/App.xaml.cs` gains three
  registrations and one named-client handler wiring. [[FND-038]]'s single host fixture
  resolves them in tests, so that fixture is the place a missing registration shows up first.
- **No contract ripple, and it is worth recording that it was checked.** This ticket adds no
  endpoint, no DTO and no serialized shape of its own — it *calls* an endpoint [[GWY-019]]
  defines — so `openapi/pegasus-v1.json` and the generated client, the usual ripple on this
  board, are untouched.
- **The architecture facts must stay green.** [[FND-037]]'s (plan handle `DSK-02-12`)
  `ForbiddenDesktopDependencyPrefixes` forbids `Pegasus.Infrastructure`, `Pegasus.Web`,
  Entity Framework, Azure SDKs and `Microsoft.AspNetCore.*` in this project. A session client
  that reaches for `Microsoft.AspNetCore.Authentication` to parse `WWW-Authenticate` would
  turn that fact red — parse the header directly instead.
- **Two conditional documentation edits**, both gated on another ticket having landed:
  `docs/current-architecture.md` on [[GWY-019]], `docs/capabilities.md` on [[FND-008]].

## Out of scope

Recording what the ticket's Guardrails already forbid, so the reviewer sees each as a
decision:

- **`src/Pegasus.Web`, `src/Pegasus.Infrastructure`, `src/Pegasus.Worker` and
  `src/Pegasus.Core`.** The server half of the token flow is [[GWY-019]] (plan handle
  `DSK-04-02`) and [[GWY-021]] (`DSK-04-04`); revocation is [[GWY-022]] (`DSK-04-05`); the
  rate limiter on `/connect/token` is [[GWY-020]] (`DSK-04-03`).
- **The native login screen and its states.** [[FND-044]] (plan handle `DSK-04-08`) owns the
  UI; this ticket owns the seven `SessionFailure` values it switches on.
- **The startup orchestrator and the compatibility gate.** [[FND-045]] (plan handle
  `DSK-04-09`) and [[GWY-023]] (`DSK-04-06`). `ClientUnsupported` is produced here; the
  screen and the 24-hour fail-closed cache are not.
- **Any password storage, including "remember me".** Proposal § 8.2 and plan 04 § 7 forbid it
  outright; only the refresh handle is persisted.
- **A client-side absolute-session timer.** The 8-hour cap is the gateway's
  (`StaffSessionPolicy.cs:10`, enforced in [[GWY-019]]'s token handler). Duplicating it here
  would be a second policy owner.
- **A `Common`/`Helpers`/`Utilities` folder** (`docs/engineering.md:106-111`), and a second
  credential store, log redactor or header handler — all owned by [[FND-031]] and
  [[FND-032]].
- **Any Azure resource or any network endpoint in a unit test.** L-02 keeps the only real
  stack local, and step 13's live check runs against it, not against production.
