# 04 · Authentication, session, forced update, and startup (Phase 2)

Owner of the staff token flow on the gateway, the desktop session client and
credential storage, the client-compatibility gate, the forced-update flow,
the application startup sequence, and first-run/initial-install onboarding.
Depends on [02](../02-architecture-and-foundation/README.md) (host, HTTP
pipeline, credential store abstraction) and on the endpoint conventions in
[03](../03-gateway-api-and-data/README.md); packaging and feed mechanics are
owned by [09](../09-release-update-and-distribution/README.md).

## 1. Purpose and proposal coverage

Deliver the proposal's Phase 2 exit gate: current Pegasus credentials work from
the desktop with no Microsoft login; an obsolete package is blocked and
updates; a disabled account is rejected; tokens and secrets pass the storage
review. Also specify the user-facing startup sequence and first install.

| Proposal section | Covered here |
| --- | --- |
| §8.1 user experience, §8.2 protocol, §8.3 authorization, §8.4 session failure handling | Token flow, claims→actor, failure matrix |
| §9.1 two-layer enforcement, §9.2 startup sequence, §9.3 operational controls (client side) | Compatibility endpoint + middleware, update-required flow, fail-closed cache |
| §11.1 local cache (tokens), §11.3 connectivity handling | Token placement, disconnected state |
| §13.1 access and session | Login, session restore, logout, role-aware navigation, availability status |
| §17.1 required controls (token storage, no secrets in package) | Storage review |
| §24 Phase 2, §29 item 7 (spike contents) | Work breakdown and exit gate |

## 2. Evidence base

### Facts

Gateway side (`src/Pegasus.Web`, fork `main` at `191ddf33`):

- ASP.NET Core Identity: `Program.cs:263` `AddIdentity<PegasusIdentityUser,
  IdentityRole<Guid>>` with password length 8, complexity off,
  `Lockout.AllowedForNewUsers=false`; `Pages/Account/SignIn.cshtml.cs:64`
  `CheckPasswordSignInAsync(user, Password, lockoutOnFailure: false)`;
  `SignIn.cshtml.cs:13` `[EnableRateLimiting("StaffSignIn")]`.
- Rate limiting: `Program.cs:275-327` `AddRateLimiter` with policy
  `StaffSignIn` (fixed window per remote IP,
  `StaffSessionPolicy.SignInAttemptsPerClientPerMinute` = 10) plus a global
  sign-in limiter (100/min) applied to `POST /Account/SignIn`; 429 with
  `Retry-After: 60` and `SecurityEvent` reason codes — ADR-0013 "transient
  request throttling rather than lockout".
- Cookie session: `Program.cs:328-457` policy scheme `Pegasus`, cookie
  `__Host-Pegasus` (`:370`), idle `StaffSessionPolicy.IdleLifetime` (2h,
  sliding), absolute 8h enforced in `OnValidatePrincipal` via a
  `pegasus:original-issued-at` claim, `SecurityStampValidatorOptions.
  ValidationInterval = TimeSpan.Zero` (`:353`) so `IsEnabled` is re-checked on
  every request; fallback authorization policy `RequireAuthenticatedUser`
  (`:518`); `MustChangePassword` redirect middleware (`:875-899`, check at
  `:891`).
- `src/Pegasus.Core/Actors/StaffSessionPolicy.cs:9-13`: `IdleLifetime` 2h,
  `AbsoluteLifetime` 8h, 10 sign-in attempts per client per minute, 100
  global; documented as transport-neutral. `src/Pegasus.Core/Actors/
  StaffActorFactory.cs:8` `TryCreate(subjectId, roleNames, out ActionActor)`
  is the claims→actor seam; `src/Pegasus.Core/Identity/StaffAuthorization.cs`
  fails closed for unknown combinations.
- OpenIddict 7.6 is already composed for the Automation MCP client:
  `src/Pegasus.Web/Mcp/AutomationMcpExtensions.cs:33-60` (`AddOpenIddict()`
  with EF Core stores on `PegasusDbContext`, token endpoint
  `/connect/token`, client-credentials + authorization-code/PKCE +
  refresh-token flows, scopes `automation.*`, access token 10 min, refresh 14
  days, `DisableSlidingRefreshTokenExpiration()`, **ephemeral** encryption and
  signing keys "sufficient for short-lived client-credentials tokens");
  constants in `src/Pegasus.Web/Mcp/AutomationMcp.cs:25-36`; consent page
  `Pages/Connect/Authorize.cshtml.cs`. ADR-0004, ADR-0011, ADR-0026,
  ADR-0027 govern that client; `ActorKind.Automation` has casework rights only.
- Data Protection keys are persisted to Azure Blob (`authentication-ring/
  keys.xml`, `Program.cs:172-176`) in production; `/diagnostics/version`
  (`Program.cs:954`) returns `{version, sourceSha}`; there is no client-version
  header, no minimum-version gate, no OpenAPI.
- Production config is managed-identity based; app settings live in
  `infra/modules/platform.bicep` (Container App) — any new app setting is an
  Azure write; database-backed administrator settings already exist as a
  pattern (ADR-0018 inspection mode, ADR-0022/0024 approved mailboxes).

Desktop side and platform (official docs, fetched 2026-08-23):

- Credential Locker (`PasswordVault`): passwords only, 20-credential limit in
  AppContainer, roaming semantics —
  https://learn.microsoft.com/windows/apps/develop/security/credential-locker.
  Area 02 decided on DPAPI (`ProtectedData`, current-user scope) for the
  refresh/session handle; access token in memory.
- App Installer update settings: `OnLaunch` with `HoursBetweenUpdateChecks`
  (0–255, default 24), `ShowPrompt`, `UpdateBlocksActivation` (requires
  `ShowPrompt="true"`; user can take the update or close the app),
  `ForceUpdateFromAnyVersion`; these need the **2021 schema**
  (`xmlns="http://schemas.microsoft.com/appx/appinstaller/2021"`) — Visual
  Studio emits 2017/2 which silently ignores them —
  https://learn.microsoft.com/windows/msix/app-installer/update-settings and
  https://learn.microsoft.com/windows/apps/package-and-deploy/distribution-feature-status.
- `Package.CheckUpdateAvailabilityAsync` returns `NoUpdates | Available |
  Required | Unknown | Error`; calling it on `Package.Current` fails with
  "Access denied" — use `new PackageManager().FindPackageForUser(string.Empty,
  Package.Current.Id.FullName)`; only works for packages installed through an
  `.appinstaller` file —
  https://learn.microsoft.com/uwp/api/windows.applicationmodel.package.checkupdateavailabilityasync.
  Updates can be started from code with
  `PackageManager.RequestAddPackageByAppInstallerFileAsync` /
  `AddPackageByAppInstallerFileAsync` —
  https://learn.microsoft.com/windows/msix/non-store-developer-updates.
- `ms-appinstaller:` protocol is disabled by default since December 2023;
  link to the `.appinstaller` file directly (download + open) —
  https://learn.microsoft.com/windows/apps/package-and-deploy/distribution-feature-status.
- OpenIddict supports the resource-owner password flow and the refresh-token
  flow (samples "Hollastin" and "Imynusoph"); refresh tokens need the
  `offline_access` scope and the client permission —
  https://documentation.openiddict.com/guides/choosing-the-right-flow.html.
- WebView2 runtime ships with Windows 11 (needed later for L-03 rendering);
  absence must still be detected (see 07).

### Assumptions

- A1. OpenIddict 7.6 still exposes `AllowPasswordFlow()` and per-principal
  `SetRefreshTokenLifetime`/`SetAccessTokenLifetime` overrides in the token
  handler (true for 5.x–6.x; verify against the 7.6 API with Microsoft Learn
  and the OpenIddict docs before DSK-04-01).
- A2. Enabling `UseDataProtection()` for OpenIddict tokens (so they survive a
  Container App restart through the blob-persisted Data Protection ring)
  does not disturb the existing Automation MCP client; verify with the
  existing MCP integration tests.
- A3. The production Container App ingress passes `Authorization: Bearer`
  headers untouched (it does today for `/mcp`).
- A4. Staff accounts keep `Guid` subject ids and role names compatible with
  `StaffActorFactory.TryCreate` when issued as token claims.

## 3. Decisions and assumptions

Locked decisions binding here: L-01 (gateway in `Pegasus.Web`), L-03
(WebView2 presence check at startup). Open decisions it touches: D-002 (trust
step only for a self-managed certificate), D-003 (feed URL per channel).
ADRs: **ADR-0102** (existing Pegasus credentials with a token session),
**ADR-0105** (MSIX/App Installer + minimum-version gate), ADR-0103 (gateway,
not direct DB) underpins everything.

Decisions taken in this plan:

1. **Token flow = OpenIddict password + refresh-token grants for a first-party
   public client `pegasus-desktop`** (no secret, scopes `pegasus.desktop` +
   `offline_access`), reusing Identity's password verification and the
   existing sign-in rate limiter on `POST /connect/token` when
   `grant_type=password`. No authorization-code/browser round trip: the login
   screen stays native (§8.1). The Automation client's grants, scopes, and
   lifetimes are untouched.
2. **Lifetimes**: access token 10 minutes (same constant family as
   `AutomationMcp.AccessTokenLifetime`); refresh token re-issued on every
   refresh (rolling, OpenIddict default) with a 2-hour lifetime
   (`StaffSessionPolicy.IdleLifetime`) and an **absolute** 8-hour cap carried
   as an `original-issued-at` claim copied into every re-issued token
   (mirrors the cookie's `pegasus:original-issued-at`). *Deviation note:* the
   Automation client's 14-day refresh lifetime and the server-wide
   `DisableSlidingRefreshTokenExpiration()` are not reused for staff; the
   idle/absolute pair is implemented in the token handler rather than by
   flipping the global sliding switch, so MCP connectors keep their fortnightly
   cap.
3. **Revocation**: account disable, password change, and explicit logout
   revoke the subject's refresh tokens (OpenIddict token store); every `/api/v1`
   request re-checks `IsEnabled` and the security stamp (same guarantee as the
   cookie's `ValidationInterval = 0`) — cost is one indexed read per request,
   acceptable at ten users; a disabled account therefore stops within one
   request, not one access-token lifetime. `MustChangePassword` becomes
   problem type `password-change-required` and the desktop routes to the
   change-password flow (the existing Core `Identity/` password-change use
   case through a new endpoint in 03).
4. **Keys**: the desktop client's tokens use OpenIddict's Data Protection
   integration (`UseDataProtection()`) so tokens survive Container App
   restarts/releases through the already-persisted key ring; no new Azure
   secret. *Deviation note:* the current ephemeral keys were chosen for
   short-lived machine tokens; staff sessions must not be invalidated by every
   gateway release.
5. **Compatibility gate**: `GET /api/v1/client-compatibility` (anonymous,
   no rate-limit bypass, returns `minimumVersion`, `currentVersion`,
   `channel`, `maintenanceMessage`, `validForSeconds`); middleware on the
   `/api/v1` group rejects requests whose `X-Pegasus-Client-Version` is below
   the minimum with problem type `urn:pegasus:problem:client-unsupported`
   (HTTP 426 or 403 — 03 fixes the code). **Minimum version is a
   database-backed Administrator setting with audit** (pattern: ADR-0018/0024
   settings), not a Container App app setting — so raising the minimum is an
   authenticated administrative action, not an Azure write; a configuration
   fallback (`Desktop:MinimumClientVersion`) exists only for bootstrap and is
   ⚠ an Azure write if ever changed in production (conditional on exact-target
   approval). Proposal §9 allows either; this is a choice, not a deviation.
6. **Fail-closed cache**: the last successful compatibility response is cached
   locally for 24 hours; beyond that, or when the response says blocked, the
   app shows the update-required/connectivity screen and performs no work;
   no bypass exists (§9.3).
7. **Startup orchestrator** (one class, one state machine, testable without
   the dispatcher) runs: App Installer `OnLaunch` (outside our control) →
   `CheckUpdateAvailabilityAsync` via `PackageManager.FindPackageForUser` →
   `Required`/`Available` handling → compatibility gate → WebView2 runtime
   presence (non-blocking warning until Phase 7 makes it required) → session
   restore (refresh) or native login → shell. Every step has a
   user-visible state and a diagnostics log line with the correlation id.
8. **Secrets in the package**: none. The package carries only the gateway
   base URL, feed URL, and channel name per channel (02's embedded
   `appsettings.<channel>.json`).

Session failure matrix (proposal §8.4 → problem types and desktop behaviour):

| Condition | Gateway signal | Desktop behaviour |
| --- | --- | --- |
| Access token expired | `401` with `WWW-Authenticate: Bearer error="invalid_token"` | Silent refresh once; on success retry the request |
| Refresh token invalid/revoked | token endpoint `invalid_grant` | Clear store, return to login, keep unsaved drafts per 05 |
| Account disabled | `401`/token `invalid_grant` with `error_description` code `account-disabled` → problem `urn:pegasus:problem:account-disabled` | Explain access has been disabled; no retry loop |
| Password change required | problem `urn:pegasus:problem:password-change-required` | Route to change-password flow; block other work |
| Client unsupported | problem `urn:pegasus:problem:client-unsupported` (+ `minimumVersion`) | Update-required screen; launch `.appinstaller`; no work |
| Server unreachable / TLS failure | transport exception | Disconnected state in status bar; periodic recheck; never shown as bad credentials |
| Rate limited | `429` + `Retry-After` | Show wait time; disable submit until then |

## 4. Target state and exit gate

| Gate (proposal §24 Phase 2) | Evidence |
| --- | --- |
| Current user credentials work | Login with an existing Identity account against the local Test/UAT stack and, in pilot, production; contract test for `POST /connect/token` password grant (tier 5) |
| Microsoft login is not required | No MSAL/Entra package in Desktop; no browser launch in the login path (architecture test + UI test) |
| Obsolete package is blocked and updates | Packaging test: install v1, publish v2 `.appinstaller` with `UpdateBlocksActivation`, relaunch → prompt → updated; gateway test: old `X-Pegasus-Client-Version` → `client-unsupported` (tiers 5, 11) |
| Disabled account is rejected | Integration test: disable user → next API call 401/problem within one request; refresh refused |
| Tokens/secrets pass storage review | Review checklist: access token never written; refresh handle only in DPAPI store; no secrets in MSIX (package content scan) (tier 9) |
| Startup sequence observable | Diagnostics log shows the ordered steps with correlation ids; UI test drives update-required and disconnected states |

## 5. Work breakdown

Kanmer areas: gateway work in `gateway-api` (GWY), desktop work in
`desktop-foundation` (FND); horizon `HZN Phase 2`.

| ID | Title | Profile | Depends on | Acceptance | Verification | Tier | Routing |
| --- | --- | --- | --- | --- | --- | --- | --- |
| DSK-04-01 | Author ADR-0102 (existing credentials, token session) and ADR-0105 (MSIX/App Installer + minimum-version gate) | chore | 00 | Accepted ADRs with the cloud-justification test filled (shared authority + central enforcement = yes) | Link test | 1 | `pegasus-desktop-reviewer` · `kanmer-docs` · Kanmer |
| DSK-04-02 | Register OpenIddict public client `pegasus-desktop` (password + refresh grants, scopes), `UseDataProtection()` for tokens, token handler with idle/absolute lifetimes and `original-issued-at` claim | feature | DSK-04-01, DSK-02-04 | Token issued for a valid staff account; MCP client behaviour unchanged (existing MCP tests green) | New integration tests in `tests/Pegasus.IntegrationTests` (password grant, refresh rolling, absolute cap) | 5 | `pegasus-gateway-dev` · `dotnet-webapi`, `microsoft-code-reference` · Microsoft Learn, Kanmer |
| DSK-04-03 | Apply the `StaffSignIn` rate-limit policy and the global sign-in limiter to `POST /connect/token` password grants; emit the same `SecurityEvent` reason codes | feature | DSK-04-02 | 11th attempt/min from one client → 429 + `Retry-After`; security event written | Integration test mirrors the cookie sign-in limiter tests | 5/9 | `pegasus-gateway-dev` · `dotnet-webapi` · — |
| DSK-04-04 | Bearer authentication for `/api/v1`: claims → `StaffActorFactory.TryCreate`; per-request `IsEnabled`/security-stamp check; `password-change-required` problem | feature | DSK-04-02, 03 endpoint group | Disabled user rejected on the next request; must-change-password user receives the problem type | Integration tests | 5 | `pegasus-gateway-dev` · `dotnet-webapi` · Microsoft Learn |
| DSK-04-05 | Revocation: disable account, password change, logout revoke refresh tokens; admin "sign out everywhere" reuses the same path | feature | DSK-04-04 | Refresh after revocation → `invalid_grant`; audit entries via `ISecurityEventWriter` | Integration tests | 5/9 | `pegasus-gateway-dev` · `dotnet-webapi`, `code-testing-agent` · — |
| DSK-04-06 | Minimum client version as a DB-backed Administrator setting (+ audit) with config bootstrap fallback; `GET /api/v1/client-compatibility`; `/api/v1` middleware on `X-Pegasus-Client-Version` | feature | DSK-04-04 | Endpoint returns min/current/channel/maintenance; below-minimum requests get `client-unsupported`; setting change is audited | Integration tests; architecture test that the middleware covers the whole group | 5 | `pegasus-gateway-dev` · `dotnet-webapi`, `optimizing-ef-core-queries` · Microsoft Learn |
| DSK-04-07 | Desktop session client: login use case, token cache (memory), DPAPI refresh store (02 abstraction), automatic refresh, logout, correlation/version headers | feature | DSK-02-06, DSK-04-02 | Login/refresh/logout against the local stack; no token in logs | ViewModel tests with a fake token endpoint; log redaction test | 2 | `winui-dev` · `winui-dev-workflow`, `microsoft-code-reference` · Microsoft Learn |
| DSK-04-08 | Native login screen and session-failure handling per the matrix (disabled, password change, rate limited, disconnected) | feature | DSK-04-07 | Each row of the matrix reachable in a UI test; no modal for routine states (InfoBar) | `winapp ui` script + screenshots | 7 | `winui-dev` · `winui-design`, `winui-ui-testing` · — |
| DSK-04-09 | Startup orchestrator: update check (`FindPackageForUser` + `CheckUpdateAvailabilityAsync`), compatibility gate with 24h fail-closed cache, WebView2 presence check, session restore | feature | DSK-04-06, DSK-04-07 | Each state has a screen and a log line; `Required` → update-required screen launching the `.appinstaller` | ViewModel tests with fakes for package/compat/clock; UI test for blocked state | 2/7 | `winui-dev` · `winui-dev-workflow` · Microsoft Learn (`PackageManager`) |
| DSK-04-10 | Role-aware shell: hide/disable commands by `StaffAccessRight`; server still enforces | feature | DSK-04-04, DSK-02-08 | Administrator-only rail items absent for non-admins; a forged call is still refused by the gateway | VM test + integration test | 2/5 | `winui-dev` · `winui-design` · — |
| DSK-04-11 | Connectivity state: status-bar disconnected indicator, automatic recheck, saves disabled while offline (no silent queueing) | feature | DSK-04-07 | Pulling the network shows the state within one recheck interval; no command reports success without server confirmation | UI test with the gateway stopped | 7 | `winui-dev` · `winui-design` · — |
| DSK-04-12 | `.appinstaller` 2021-schema template and local feed for the Test/UAT stack (`OnLaunch HoursBetweenUpdateChecks="0" ShowPrompt="true" UpdateBlocksActivation="true"`) | chore | DSK-02-14 | Local feed serves v1→v2 with the blocking prompt | Packaging test (09's scenario list) | 11 | `pegasus-release-packager` · `winui-packaging` · Microsoft Learn |
| DSK-04-13 | First-run/initial-install guide for workstations (prereqs, optional trust step per D-002, open `.appinstaller`, first launch, blocked states, diagnostics location, channel switch) | chore | DSK-04-09 | Guide reproduced on a clean Windows 11 VM by someone other than the author | Proof: screenshots of each step | 7 | `pegasus-release-packager` · `winui-packaging`, `microsoft-docs` · Microsoft Learn |
| DSK-04-14 | Security tests for the token path (expiry/rotation/revocation, disabled account, role bypass, version spoofing, temp-file permissions of the DPAPI store) | feature | DSK-04-05, DSK-04-07 | Every §22 security-test item for auth has a test or a recorded manual check | `dotnet test` + checklist | 9 | `pegasus-test-engineer` · `code-testing-agent`, `test-gap-analysis` · — |
| DSK-04-15 | Phase 2 exit review and UAT script (login, blocked old version, update, disabled account) | chore | all above | Every gate row in §4 evidenced in the ticket proof | Proof doc | 5/7/11 | `pegasus-desktop-reviewer` · `winui-code-review` · Kanmer |

## 6. Routing table

| Work type | Subagent | Skills (pinned source) | MCP tools |
| --- | --- | --- | --- |
| OpenIddict client, token handler, bearer auth, compat gate, revocation | `pegasus-gateway-dev` | `dotnet-webapi`, `microsoft-code-reference`, `optimizing-ef-core-queries` — dotnet/skills `98f84851` | Microsoft Learn (`microsoft_docs_search` for OpenIddict/ASP.NET Core bearer, `microsoft_code_sample_search`), Kanmer |
| Desktop session client, login screen, startup orchestrator, connectivity | `winui-dev` | `winui-dev-workflow`, `winui-design`, `microsoft-code-reference` — win-dev-skills `f1028dd5` | Microsoft Learn (`PackageManager`, `Package.CheckUpdateAvailabilityAsync`, `ProtectedData`) |
| `.appinstaller` template, local feed, first-run guide | `pegasus-release-packager` | `winui-packaging`, `microsoft-docs` | Microsoft Learn (App Installer schema) |
| Tests | `pegasus-test-engineer` | `code-testing-agent`, `run-tests`, `test-gap-analysis` | — |
| Review | `pegasus-desktop-reviewer` (read-only) | `winui-code-review`, project skill `pegasus-desktop` | Microsoft Learn, Kanmer |
| Not applicable | — | `entra-app-registration`, `entra-agent-id` (no Microsoft/Entra login for users) | — |

## 7. Risks and traps

- **Ephemeral OpenIddict keys**: without `UseDataProtection()` (or persisted
  keys) every Container App restart invalidates all desktop sessions; decide
  in DSK-04-02, test with a host restart in the local stack.
- **Global sliding switch**: `DisableSlidingRefreshTokenExpiration()` is
  server-wide; implement staff idle/absolute in the handler, do not flip the
  switch (it would change MCP connector behaviour governed by ADR-0027).
- **Rate limiter scope**: the cookie limiter keys on remote IP; behind the
  Container Apps ingress the forwarded-headers configuration must be in place
  (it is, `Program.cs` forwarded headers in production) or all desktops share
  one bucket.
- **`CheckUpdateAvailabilityAsync` on `Package.Current`** throws Access
  denied; use `PackageManager.FindPackageForUser`. Returns `Unknown` for a
  side-loaded dev MSIX not installed from an `.appinstaller` — the Test/UAT
  stack must install from a local feed to exercise the path.
- **2017/2 schema silently ignores** `ShowPrompt`/`UpdateBlocksActivation`;
  the template must declare the 2021 namespace and the feed host must return
  correct MIME/Content-Length (09).
- **App Installer fail-open**: if the feed is unreachable the OS launches the
  app anyway — the gateway gate is the fail-closed layer; the 24h cache must
  not be extended "for convenience".
- **Runtime-role GRANT trap** (PLAT-035 class): new tables for the minimum
  version setting and OpenIddict token rows need `GRANT`s in the migration
  for `pegasus_web_runtime_role`; mirror in
  `scripts/Invoke-AzureDatabaseBootstrap.ps1` and add the migration id to the
  pinned census in `tests/Pegasus.IntegrationTests/IntakePersistenceIntegrationTests.cs`
  (release traps in `.agents/skills/pegasus-release/SKILL.md`).
- **Plaintext verification account** in `src/Pegasus.Web/appsettings.json`
  (`Bootstrap:VerificationAccount`) must be retired before desktop go-live
  (documented in `docs/operations.md`); it must never be the desktop test
  login in production.
- **No password storage on the desktop** — not even "remember me"; only the
  refresh handle (§8.2).

## 8. Documentation changes

- `docs/adr/0102-existing-pegasus-credentials-token-session.md`,
  `docs/adr/0105-msix-app-installer-and-minimum-version-gate.md` (+ index).
- FRD-13 (desktop operator experience): login, session restore, blocked
  states, disconnected state, update-required screen; FRD-04 cross-reference
  for account lifecycle.
- `docs/current-architecture.md § Authentication and authorization boundary`:
  token flow beside the cookie; compatibility gate; client version header.
- `docs/runbook.md`: mandatory-update runbook pointer (09) and the
  administrator procedure for raising the minimum client version.
- `docs/operations.md`: record the first production minimum-version setting
  and the pilot ring state when it exists.
- `docs/capabilities.md`: `DSK-03` (desktop sign-in and session), `DSK-04`
  (forced update and compatibility gate).
