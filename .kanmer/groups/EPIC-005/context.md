# EPIC-005 — Area 04: auth, session, update and startup (Phase 2)

Read this once before working any ticket in this epic. It carries what binds
every row; it deliberately does not repeat what belongs in a ticket body.

## What this area delivers

The Phase 2 exit gate of the conversion: existing Pegasus credentials work from
the native desktop with no Microsoft login, an obsolete package is blocked and
updates itself, a disabled account stops within one request, and tokens and
secrets pass a storage review. It owns the staff token flow on the gateway, the
desktop session client and credential storage, the client-compatibility gate,
the forced-update flow, the startup sequence and first-run onboarding.

## Where the rows live — this epic spans two board areas

| Board area | Plan handles |
| --- | --- |
| `gateway-api` (GWY) | DSK-04-02, DSK-04-03, DSK-04-04, DSK-04-05, DSK-04-06, DSK-04-14 |
| `desktop-foundation` (FND) | DSK-04-01, DSK-04-07 … DSK-04-13, DSK-04-15 |

Every row is horizon HZN-003 (Phase 2 — compatibility, update and
authentication). DSK-04-14 spans both halves: its gateway facts are in
`gateway-api`, its DPAPI-store ACL evidence depends on DSK-04-07 in
`desktop-foundation`.

## Proposal coverage

`docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` §8.1–8.4
(authentication, protocol, authorization, session failure handling), §9.1–9.3
(two-layer enforcement, startup sequence, operational controls), §11.1 and
§11.3 (token placement, connectivity), §13.1 (access and session), §17.1
(token storage, no secrets in the package), §24 Phase 2 and §29 item 7.

## Decisions, assumptions and deviations that bind every ticket

- **L-01** the gateway is `Pegasus.Web` evolved in place — no new deployment
  unit. **L-02** Test/UAT is the local stack; ADR-0014 stands and an Azure test
  resource is out of bounds. **L-03** the WebView2 presence check belongs in the
  startup sequence. **L-04** every ticket names its subagent, skills and MCP.
- **D-002** signing uses a self-managed certificate, so first install always
  includes a per-workstation trust step. **D-003** the update feed is a UNC
  share, so the baked feed path is `\\<host>\<share>\<channel>\Pegasus.appinstaller`
  and update checks need the office network or VPN.
- ADRs: **ADR-0102** (existing credentials + token session) and **ADR-0105**
  (MSIX/App Installer + minimum-version gate) are owed — every ticket here
  carries `docs_todo: true` until DSK-04-01 lands. **ADR-0103** underpins all of
  it: gateway only, never direct database access from a workstation.
- Token flow is OpenIddict **password + refresh** for a first-party *public*
  client `pegasus-desktop` (scopes `pegasus.desktop`, `offline_access`), no
  browser round trip. Access 10 minutes; refresh rolling at 2 hours with an
  8-hour absolute cap carried as `pegasus:original-issued-at`.
- **Deviations, recorded:** the Automation client's 14-day refresh cap and the
  server-wide `DisableSlidingRefreshTokenExpiration()` are *not* reused for
  staff — implement the idle/absolute pair in the token handler. The current
  ephemeral OpenIddict keys are replaced by `UseDataProtection()`; the earlier
  choice was made for short-lived machine tokens only.
- **Choice, not a deviation:** the minimum client version is a database-backed
  Administrator setting with audit (ADR-0018/0024 pattern), not a Container App
  app setting. The `Desktop:MinimumClientVersion` configuration fallback exists
  for bootstrap only and is a ⚠ Azure write if ever set in production.
- Open assumptions to verify, not to assume: A1 the OpenIddict 7.6 API surface
  (`AllowPasswordFlow`, per-principal lifetimes); A2 `UseDataProtection()` does
  not disturb the Automation MCP client; A3 the Container Apps ingress passes
  `Authorization: Bearer` untouched; A4 staff `Guid` subjects and role names
  stay compatible with `StaffActorFactory.TryCreate`.

## Exit gate and what proves it

Credentials work (contract test on the password grant, tier 5) · no MSAL/Entra
package and no browser launch in the login path (architecture + UI test) ·
obsolete package blocked and updated (packaging test plus a below-minimum
`X-Pegasus-Client-Version` refusal, tiers 5 and 11) · disabled account rejected
on the *next* request (integration test) · tokens and secrets pass the storage
review (access token never written, refresh handle only in the DPAPI store, MSIX
content scan, tier 9) · startup sequence observable in the diagnostics log with
correlation ids. DSK-04-15 collects the evidence for all six rows.

## Routing for this area

| Work | Subagent | Skills (pinned source) | MCP |
| --- | --- | --- | --- |
| OpenIddict client, token handler, bearer auth, compat gate, revocation | `pegasus-gateway-dev` | `dotnet-webapi`, `microsoft-code-reference`, `optimizing-ef-core-queries` — `dotnet/skills` `98f84851` | Microsoft Learn (`microsoft_docs_search`, `microsoft_code_sample_search`), Kanmer |
| Desktop session client, login screen, startup orchestrator, connectivity | `winui-dev` | `winui-dev-workflow`, `winui-design`, `microsoft-code-reference` — win-dev-skills `f1028dd5` (`.codex/skills/` today) | Microsoft Learn (`PackageManager`, `CheckUpdateAvailabilityAsync`, `ProtectedData`) |
| `.appinstaller` template, local feed, first-run guide | `pegasus-release-packager` | `winui-packaging`, `microsoft-docs` | Microsoft Learn (App Installer 2021 schema) |
| Tests | `pegasus-test-engineer` | `code-testing-agent`, `run-tests`, `test-gap-analysis` | — |
| Review (read-only, did not implement) | `pegasus-desktop-reviewer` | `winui-code-review`, project skill `pegasus-desktop` | Microsoft Learn, Kanmer |

Load `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`)
first, always. **Do not load** `entra-app-registration` or `entra-agent-id` —
there is no Microsoft or Entra login for users (proposal §8).

## Traps (plan §7) — every ticket inherits these

1. **Ephemeral OpenIddict keys** invalidate every desktop session on each
   Container App restart; `UseDataProtection()` is mandatory, not an option.
2. **`DisableSlidingRefreshTokenExpiration()` is server-wide** — flipping it
   changes MCP connector behaviour governed by ADR-0027.
3. **Rate-limiter scope**: `StaffSignIn` partitions on the raw remote IP; behind
   the ingress every desktop shares one bucket unless forwarded headers are
   configured before `UseRateLimiter()`.
4. **`CheckUpdateAvailabilityAsync` on `Package.Current`** throws access denied
   — use `PackageManager.FindPackageForUser`; it returns `Unknown` for a
   side-loaded MSIX, so the local feed is required to exercise the path.
5. **App Installer schema 2017/2 silently ignores** `ShowPrompt` and
   `UpdateBlocksActivation`; the template must declare the 2021 namespace.
6. **App Installer fails open** — the gateway gate is the fail-closed layer and
   the 24-hour compatibility cache must never be extended for convenience.
7. **Runtime-role GRANT trap** (PLAT-035 class): a new table needs its `GRANT`
   in the migration, the mirror in `scripts/Invoke-AzureDatabaseBootstrap.ps1`
   and its id appended to the pinned census in
   `tests/Pegasus.IntegrationTests/IntakePersistenceIntegrationTests.cs`.
   OpenIddict tables additionally carry **`DENY DELETE`** for both runtime roles
   — revoke by status update, never prune.
8. **Plaintext `Bootstrap:VerificationAccount`** in `src/Pegasus.Web/appsettings.json`
   must never be the desktop test login and must be retired before go-live.
9. **No password storage on the desktop** — not even "remember me"; only the
   refresh handle (§8.2).

## Read these before starting any ticket in this epic

- `docs/desktop/04-auth-session-update-and-startup/README.md` (whole file)
- `docs/desktop/README.md` § Locked decisions, § Routing legend
- `docs/desktop/00-governance-and-workflow/README.md` § Kanmer board shape,
  § Ticket template, § Risks and traps
- `docs/desktop/03-gateway-api-and-data/README.md` § 2 facts and the problem-type
  catalogue at line 167; `docs/desktop/03-gateway-api-and-data/endpoint-map.md`
- `docs/desktop/12-agent-tooling/skill-routing.md` (pins and the do-not-load table)
- `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` §8, §9, §11, §13.1,
  §17.1, §22.2, §24 Phase 2
- `AGENTS.md` (Kanmer block, Repository task workflow, ADR conventions),
  `docs/engineering.md` § Required evidence tiers, `docs/runbook.md`
  § Live-operation approval matrix
- Gateway code: `src/Pegasus.Web/Program.cs`, `src/Pegasus.Web/Mcp/*.cs`,
  `src/Pegasus.Core/Actors/StaffSessionPolicy.cs`,
  `src/Pegasus.Core/Actors/StaffActorFactory.cs`,
  `src/Pegasus.Core/Identity/StaffAuthorization.cs`,
  `src/Pegasus.Core/Identity/IdentityContracts.cs`,
  `src/Pegasus.Infrastructure/Persistence/EfStaffAccountAdministration.cs`,
  `src/Pegasus.Infrastructure/Persistence/EfStaffPasswordChange.cs`,
  `src/Pegasus.Infrastructure/Persistence/EfWorkflowConfigurationStore.cs`
