# Research — FND-043: what a desktop session client must call, store and never store

## Question

What exactly must the desktop session client post to the gateway, what may it keep on the
workstation, and how must each row of the area 04 session-failure matrix reach a view model —
given that nothing in this repository issues a bearer token to a staff user today?

## Current behaviour

**The parity matrix does cover this, and the rows are `PAR-01` and `PAR-02`.**
`docs/desktop/01-inventory-and-parity/parity-matrix.md` holds 46 rows
(`grep -c '^| PAR-' …` → `46`), each keyed to a page model under `src/Pegasus.Web/Pages/**`.

- **`PAR-01` (§13.1 Access and session, FRD-04)** — `Account/SignIn.cshtml.cs` (106 lines),
  `OnGet` / `OnPostAsync`. Its recorded "today" behaviour is Identity
  `CheckPasswordSignInAsync(lockoutOnFailure: false)`, the `StaffSignIn` rate-limit policy
  plus a global 100/min limiter on `POST /Account/SignIn`, and the `__Host-Pegasus` cookie
  with 2 h sliding / 8 h absolute lifetimes. Its recorded desktop target is the
  OpenIddict `/connect/token` password + `refresh_token` grants for client
  `pegasus-desktop`, `StaffActorFactory.TryCreate`, and `StaffSessionPolicy` lifetimes —
  which is exactly this ticket plus [[GWY-019]] (plan handle `DSK-04-02`).
- **`PAR-02` (§13.1, FRD-04)** — `Account/SignOut.cshtml.cs` (21 lines). Desktop target:
  "Sign out command in shell user menu; clears tokens and credential store", reaching
  `POST /connect/logout` or a `/api/v1/session/logout` that revokes the refresh token. Its
  test column reads `to locate` and the row is marked **not inventoried**, so the sign-out
  half has less recorded evidence behind it than sign-in does.
- `PAR-03` (password change) and `PAR-44` (deny-by-default authorization) touch the same
  area but belong to [[FEAT-021]] (plan handle `DSK-05-21`) and [[FND-046]] (plan handle
  `DSK-04-10`) respectively.

Today's mechanism, read directly:

| What | Where | Value |
| --- | --- | --- |
| Staff credential check | `src/Pegasus.Web/Pages/Account/SignIn.cshtml.cs:64` | `CheckPasswordSignInAsync(user, Password, lockoutOnFailure: false)`, guarded by a `user is null \|\| !user.IsEnabled` short-circuit on `:62-63` |
| Sign-in rate limit | `src/Pegasus.Web/Pages/Account/SignIn.cshtml.cs:13` | `[EnableRateLimiting("StaffSignIn")]` |
| The operator sentence for a bad credential | `SignIn.cshtml.cs:73-74` | "The username or password is incorrect. If your access has changed, contact an administrator." |
| Forced password change | `SignIn.cshtml.cs:83-85` | `MustChangePassword` → redirect to `/Account/PasswordChange` |
| Session cookie | `src/Pegasus.Web/Program.cs:370` | `__Host-Pegasus`, `HttpOnly`, `SameSite=Strict`, `SecurePolicy=Always` |
| Token endpoint that already exists | `src/Pegasus.Web/Mcp/AutomationMcp.cs:25` | `TokenEndpointPath = "/connect/token"` |
| Access-token lifetime constant family | `src/Pegasus.Web/Mcp/AutomationMcp.cs:35` | `AccessTokenLifetime = TimeSpan.FromMinutes(10)` |
| Automation refresh lifetime (**not** reused for staff) | `src/Pegasus.Web/Mcp/AutomationMcp.cs:36` | `RefreshTokenLifetime = TimeSpan.FromDays(14)` |
| Staff session contract | `src/Pegasus.Core/Actors/StaffSessionPolicy.cs:9-13` | `IdleLifetime` 2 h, `AbsoluteLifetime` 8 h, 10 sign-in attempts/client/min, 100 global |

## Findings

- **The desktop posts to an endpoint that already exists, for a client that does not.**
  `/connect/token` is live for the Automation MCP actor
  (`src/Pegasus.Web/Mcp/AutomationMcpExtensions.cs:33-60`), but the public client
  `pegasus-desktop` and the password grant are [[GWY-019]]'s work. This ticket is the caller
  half and must be written against the contract, not against a running server.
  - Consequence: every test in this ticket runs against a **fake `HttpMessageHandler`**, and
    the one live check (body step 13) is against the local Test/UAT stack, not Azure — L-02.
- **Three lifetime numbers exist and only two are this ticket's.** The staff access token is
  10 minutes (the `AutomationMcp.AccessTokenLifetime` constant family) and the staff refresh
  handle is 2 h idle / 8 h absolute (`StaffSessionPolicy:9-10`). The Automation client's
  14-day refresh (`AutomationMcp.cs:36`) is explicitly **not** reused — plan 04 § 3 decision 2
  and § 7's second trap both say the server-wide
  `DisableSlidingRefreshTokenExpiration()` must not be flipped, because ADR-0027 governs the
  MCP connectors' fortnightly cap.
  - Consequence for the client: it does not enforce the absolute cap itself. It stores the
    handle, refreshes when told to, and treats `invalid_grant` as the end of the session.
    Duplicating the 8-hour rule client-side would be a second policy owner.
- **The header contract is fixed and is not this ticket's to invent.**
  `docs/desktop/03-gateway-api-and-data/README.md:168` requires `X-Correlation-Id`
  (accepted or generated, echoed, logged) and `X-Pegasus-Client-Version` on **every**
  `/api/v1` request, with absence mapping to `client-unsupported`. The `DelegatingHandler`
  that adds them belongs to [[FND-031]] (plan handle `DSK-02-06`); this ticket adds a second
  handler for `Authorization` and must not duplicate the first.
- **The problem-slug list is closed at thirteen.**
  `docs/desktop/03-gateway-api-and-data/README.md:167` names them: `validation`,
  `not-authorized`, `version-conflict`, `lease-conflict`, `lease-expired`,
  `operation-conflict`, `client-unsupported`, `password-change-required`,
  `account-disabled`, `provider-unavailable`, `not-found`, `rate-limited`, `maintenance`.
  The ticket body's `SessionFailure` enum maps **seven** matrix rows, and its instruction "do
  not add a value the matrix does not list" is what keeps the client from inventing a
  fourteenth state.
- **DPAPI, not `PasswordVault`, and the reason is recorded.** Plan 02 § 3 decision 6:
  `System.Security.Cryptography.ProtectedData` with `DataProtectionScope.CurrentUser`,
  file-backed under `ApplicationData.Current.LocalFolder`, because the refresh handle may
  exceed what Microsoft's Credential Locker guidance calls a "password" and DPAPI has no
  count or size limit (Credential Locker documents a 20-credential AppContainer limit).
- **The password is never stored — there is no "remember me".** Proposal § 8.2 and plan 04
  § 7's last trap both state it. This is a design property the tests must assert, not a
  convention to be trusted: the ticket body's step 6 asks for a test that reads the on-disk
  bytes and confirms the plaintext handle is absent, and the same shape catches a password
  written by accident.
- **The redactor is an existing thing to extend, not a new thing to build.** [[FND-032]]
  (plan handle `DSK-02-07`) owns the bounded rolling log sink with redaction; this ticket
  adds `access_token`, `refresh_token`, `password`, `Authorization` and `Set-Cookie` to its
  list and adds the assertion. A second redactor would be the third-copy rule applied to
  logging.
- **The retry rule is "once", and the failure mode it prevents is a loop.** Body step 7:
  on `401` with `WWW-Authenticate: Bearer error="invalid_token"`, refresh **once**, retry
  **once**; a second `401` surfaces as a session failure. The area plan's matrix row says the
  same. Without the counter, a revoked refresh token and a 401 produce an infinite
  refresh/retry cycle that looks like a hang.
- **A transport failure must never be reported as a bad credential.** The matrix row is
  explicit: "Disconnected state in status bar; periodic recheck; **never shown as bad
  credentials**". This is the single most likely operator-visible defect in the ticket, and it
  is why `Unreachable` is a distinct `SessionFailure` value rather than a fallback.
- **`tests/Pegasus.Desktop.ViewModelTests` is where the tests live**, created by [[FND-038]]
  (plan handle `DSK-02-13`) with a shared `FixedTimeProvider`, a `FakeGatewayClient` that can
  return any of the thirteen problem slugs, and an `InMemoryCredentialStore`. That fake set
  was designed for exactly this ticket; using it rather than adding new fakes is the
  "one fake per concept" rule (`docs/engineering.md:194-199`).
- **Two `docs/` changes are conditional, not owed.** The ticket's Documentation-changes
  section makes `docs/current-architecture.md` § Authentication and authorization boundary
  conditional on [[GWY-019]] having landed the server half, and the `docs/capabilities.md`
  `DSK-03` row conditional on [[FND-008]] (plan handle `DSK-00-08`) having created the `DSK`
  family. Both must be checked, not assumed.

### Facts

| Fact | Source |
| --- | --- |
| `/connect/token` is the existing token endpoint path | `src/Pegasus.Web/Mcp/AutomationMcp.cs:25` |
| Access-token lifetime constant is 10 minutes | `src/Pegasus.Web/Mcp/AutomationMcp.cs:35` |
| Automation refresh lifetime is 14 days (not reused for staff) | `src/Pegasus.Web/Mcp/AutomationMcp.cs:36` |
| Staff idle 2 h, absolute 8 h, 10/client/min, 100 global | `src/Pegasus.Core/Actors/StaffSessionPolicy.cs:9-13` |
| Credential check with `lockoutOnFailure: false`, guarded by `IsEnabled` | `src/Pegasus.Web/Pages/Account/SignIn.cshtml.cs:62-64` |
| Sign-in page carries `[EnableRateLimiting("StaffSignIn")]` | `src/Pegasus.Web/Pages/Account/SignIn.cshtml.cs:13` |
| The invalid-credential operator sentence | `src/Pegasus.Web/Pages/Account/SignIn.cshtml.cs:73-74` |
| Cookie is `__Host-Pegasus`, `SameSite=Strict`, `SecurePolicy=Always` | `src/Pegasus.Web/Program.cs:370-375` |
| Thirteen stable `urn:pegasus:problem:<slug>` values | `docs/desktop/03-gateway-api-and-data/README.md:167` |
| `X-Correlation-Id` and `X-Pegasus-Client-Version` required on every `/api/v1` request | `docs/desktop/03-gateway-api-and-data/README.md:168` |
| Parity rows `PAR-01` (sign-in) and `PAR-02` (sign-out, "not inventoried") | `docs/desktop/01-inventory-and-parity/parity-matrix.md:46-47`; `grep -c '^| PAR-'` → 46 |
| DPAPI `DataProtectionScope.CurrentUser` under `ApplicationData.Current.LocalFolder`, not `PasswordVault` | `docs/desktop/02-architecture-and-foundation/README.md` § 3 decision 6 |
| No `Common`/`Helpers`/`Utilities` folders; organise by capability | `docs/engineering.md:106-111` |
| One fake per concept, `internal`, in the shared driver | `docs/engineering.md:194-199` |
| `TreatWarningsAsErrors=true`, `AnalysisLevel=latest-recommended` | `Directory.Build.props:6-7` |
| The local stack starts with `Invoke-LocalDevelopment.ps1 -Action Start` (`ValidateSet 'Start','Status','Smoke','Stop','Reset'`) | `scripts/Invoke-LocalDevelopment.ps1:3-4` |
| FRD-04 requires Pegasus-managed usernames and passwords with non-reversible hashes, and fail-closed authorization at every caller boundary | `docs/frd/frd-04-parties-accounts-and-access.md:15,25` |
| FRD-04: sign-ins and authentication failures live in the security log, not business history | `docs/frd/frd-04-parties-accounts-and-access.md:31` |

### Assumptions

- **A-04-07-1 — [[FND-031]]'s `src/Pegasus.Desktop.Infrastructure` exposes a credential-store
  interface and a header `DelegatingHandler` under names close to `IDesktopCredentialStore`
  and the `X-Pegasus-Client-Version` handler.** Confirmed by: reading the project at
  implementation time (body step 2 requires it). *If wrong*, use the names that exist and
  record the difference in the plan — the ticket body says so explicitly. What breaks if it is
  assumed: a second credential-store abstraction, which is the defect
  `docs/engineering.md` § One Core owner exists to prevent.
- **A-04-07-2 — the OpenIddict password grant accepts the exact form fields
  `grant_type=password`, `username`, `password`, `client_id`, `scope`, and returns
  `access_token`, `refresh_token`, `expires_in`, `error`, `error_description`.** Confirmed by:
  `microsoft_docs_search` for the OAuth 2.0 resource-owner password grant, then the first live
  call against the local stack. *If wrong*, the field names come from the OpenIddict
  configuration [[GWY-019]] lands — read it, do not invent a field.
- **A-04-07-3 — the gateway distinguishes a disabled account from an invalid credential in the
  token response.** Plan 04's matrix says an account-disabled condition arrives as token
  `invalid_grant` with an `error_description` code `account-disabled`, or as problem
  `urn:pegasus:problem:account-disabled` on an API call. Confirmed by: [[GWY-019]] and
  [[GWY-021]]'s integration tests. *If wrong* — if the gateway returns a bare `invalid_grant`
  — the client reports `RefreshRevoked` and the operator sees a generic message; that is a
  gateway defect to file, not a reason to guess from the message text.
- **A-04-07-4 — a `429` from `/connect/token` carries `Retry-After` in seconds.** The cookie
  path does (`Program.cs:275-327` returns 429 with `Retry-After: 60`), and [[GWY-020]] (plan
  handle `DSK-04-03`) applies the same limiter to the token endpoint. Confirmed by:
  [[GWY-020]]'s test. *If wrong*, `RateLimited` carries no wait time and the login screen shows
  a generic "try again shortly" — a degradation, not a failure.

## Execution placement

The six-question cloud-justification test from
`docs/desktop/00-governance-and-workflow/README.md` § 3 (`:169-176`), answered. The
responsibility being placed is **holding a signed-in operator's session on the workstation:
the in-memory access token, the stored refresh handle, and the silent refresh**.

| Question | Answer | Evidence |
| --- | --- | --- |
| Shared authority — must several users see and update the same state? | **No** | A session is one operator on one workstation. The shared state — the account, its roles, its enabled flag — is the gateway's and is not touched here; `docs/frd/frd-04-parties-accounts-and-access.md:25` places authorization in Core and at every caller boundary, which is [[GWY-021]]'s ticket, not this one. |
| Unattended execution — must it run with every desktop closed? | **No** | The session exists only while the operator is signed in; nothing refreshes when the application is closed. Plan 04 § 3 decision 2 caps the refresh handle at 8 hours absolute precisely so a closed workstation holds nothing useful. |
| Protected credentials — long-lived secret that must not sit on workstations? | **No — and the design is what makes the answer no.** | The password is never stored at all (proposal § 8.2; plan 04 § 7). The access token is memory-only and lives 10 minutes (`AutomationMcp.cs:35`). The refresh handle is 2 h idle / 8 h absolute (`StaffSessionPolicy.cs:9-10`) and sits under DPAPI `DataProtectionScope.CurrentUser`. Nothing long-lived reaches the disk, so there is no credential to move off the workstation. |
| Public callback — must an external service call a stable public endpoint? | **No** | The desktop calls out; nothing calls in. Plan 04 § 3 decision 1 is explicit that there is **no** authorization-code/browser round trip — the login screen stays native — so no redirect URI and no listener exist. |
| Central enforcement — revocation, permissions, audit, invariant independent of the client? | **Yes — and it lands on the existing `Pegasus.Web` gateway, not in Azure.** | Revocation on disable, password change and logout, plus the per-request `IsEnabled`/security-stamp re-check, must hold no matter what the client does (plan 04 § 3 decision 3; `Program.cs:353` is today's equivalent for the cookie). **L-01** places that on `Pegasus.Web` evolved in place — no new deployment unit — and the owning tickets are [[GWY-021]] (plan handle `DSK-04-04`) and [[GWY-022]] (plan handle `DSK-04-05`). This ticket is the caller half and places nothing new anywhere. |
| Measured operational advantage — measured evidence central is materially better? | **No** | The opposite is the recorded reason for the design: keeping the access token in memory and only a short-lived handle on disk is what makes a stolen or imaged workstation useless. Moving session state server-side is the cookie model the conversion is leaving (proposal § 8.2). No measurement supports a central alternative. |

One "yes", and it names the **existing gateway** under L-01. Nothing lands in Azure; the only
real stack this ticket touches is the local Test/UAT one (L-02), which is not a unit-test
dependency at all.

## Implications

1. **Write the client to the contract, not to a running server.** [[GWY-019]] has not landed;
   every test uses a fake `HttpMessageHandler`, and the one live check is against the local
   stack. Say so in the proof rather than implying the flow was proven end to end against
   production.
2. **Do not re-implement the absolute cap client-side.** The 8-hour rule is the gateway's; the
   client stores, refreshes, and treats `invalid_grant` as the end. A client-side timer would
   be a second policy owner and would drift.
3. **Two handlers, not one, and not a merged one.** [[FND-031]] owns the header handler;
   this ticket adds the `Authorization` handler. Merging them would put the version/correlation
   contract inside the session concern and break [[FND-047]]'s (plan handle `DSK-04-11`)
   connectivity work, which needs headers on unauthenticated calls too.
4. **The `SessionFailure` enum is a closed set of seven, derived from the matrix.** Adding an
   eighth value is how a view model ends up switching on a state no gateway ever sends.
5. **Two assertions are the ticket's real evidence**: the on-disk bytes do not contain the
   plaintext handle, and the rolling log contains none of the five redacted literals. Both are
   cheap, both fail loudly, and both catch the class of defect that is otherwise invisible
   until a security review.
6. **`Unreachable` must never be reported as bad credentials.** This is a one-line branch and
   the matrix names it; it is also the defect most likely to survive review, because both
   states show "you cannot sign in".
7. **Check the two conditional documentation changes before writing either.**
   `docs/current-architecture.md` waits for [[GWY-019]]; `docs/capabilities.md` waits for
   [[FND-008]]. Writing either early creates a claim the repository cannot support.

## Open questions

None. Every genuinely undecided item here is owned by a **named sibling ticket** — the server
half by [[GWY-019]], [[GWY-021]] and [[GWY-022]]; the credential-store and header-handler
shapes by [[FND-031]]; the log redactor by [[FND-032]] — which makes each a scope boundary
recorded in the plan's *Risks / open questions* section rather than an open question. The four
assumptions above are settled by reading the projects at implementation time or by a single
documentation query, not by asking anyone. No `open-questions` document is created.
