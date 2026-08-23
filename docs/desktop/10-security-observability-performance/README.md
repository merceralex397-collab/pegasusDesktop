# 10 · Security, observability and performance

Cross-cutting area plan for the native desktop conversion: the threat model
and controls the desktop and gateway must meet, how the system is observed
and supported (desktop diagnostics, existing Application Insights, health),
and the performance budgets, measurement method and release regression
report. It is exercised by every slice in
[05 · implementation and migration](../05-implementation-and-migration/README.md)
and gated at Phase 8 (proposal §24). Packaging and signing controls are in
[09 · release, update and distribution](../09-release-update-and-distribution/README.md);
token storage and the compatibility gate in
[04 · auth, session, update and startup](../04-auth-session-update-and-startup/README.md).

## 1. Purpose and proposal coverage

| Proposal section | What this plan does with it |
| --- | --- |
| §15 Performance design (§15.1 budgets, §15.2 practices, §15.3 profiling) | Budgets adopted verbatim as starting targets; practices become a review checklist; profiling procedure and release regression report defined |
| §16 Reliability and error handling (§16.1 operation model, §16.2 provider resilience, §16.3 crash recovery) | Operation model and provider taxonomy carried into contracts (with 03/07); crash recovery and drafts scoped |
| §17 Security and privacy (§17.1 controls, §17.2 not prioritised, §17.3 threat focus) | Threat → control → test table; controls mapped to what already exists; explicit non-goals kept |
| §18 Observability and support (§18.1 desktop diagnostics, §18.2 central telemetry, §18.3 health) | Diagnostics bundle, App Insights reuse with the known quota gap, health surface |
| §22.2 Security tests, Performance tests | Test lists turned into tickets |
| §11.1 local cache list, §11.3 connectivity | The security side of what may be cached locally |
| §24 Phase 8 hardening gate | Exit gate restated in section 4 |

## 2. Evidence base

### Facts

Repository (fork `main` @ `191ddf33`, inspected 2026-08-23):

- Web composition already carries: managed identity only
  (`src/Pegasus.Web/Program.cs:158-171`), Data Protection keys in blob
  `authentication-ring/keys.xml` (`:172-176`), Application Insights
  registered when the connection string is present with Entra ingestion
  (`:193-199`), ASP.NET Core Identity with rate limiting instead of lockout
  (`:262-327`, ADR-0013; `Pages/Account/SignIn.cshtml.cs:63`
  `lockoutOnFailure: false`), cookie `__Host-Pegasus` with 2 h idle / 8 h
  absolute lifetime and per-request `IsEnabled` re-check (`:368-457`,
  `SecurityStampValidatorOptions.ValidationInterval = TimeSpan.Zero`,
  `:353`), a fallback authorization policy of `RequireAuthenticatedUser()`
  (`:517-522`), HSTS plus CSP `default-src 'self'; object-src 'none';
  base-uri 'self'; frame-ancestors 'none'` and `nosniff` (`:758-764`), health
  checks `/health/live` and `/health/ready` (`:523-524`, `:939-950`), and the
  build-identity endpoint `/diagnostics/version` (`:954`).
- Core security records: `SecurityEvent` / `ActionHistoryEntry` with
  `ISecurityEventWriter` / `IActionHistoryWriter`
  (`src/Pegasus.Core/Identity/IdentityContracts.cs:98-137`); staff rights
  matrix fails closed (`src/Pegasus.Core/Identity/StaffAuthorization.cs`).
- Database least privilege: `pegasus_web_runtime_role` and
  `pegasus_worker_runtime_role` with per-table `GRANT` migrations
  (`20260729176000_AzureSqlRuntimeLeastPrivilege.cs`, `Grant*` migrations);
  CI enforces `scripts/Test-MigrationGrants.ps1`; the untested gap is
  PLAT-035 — "the suite is green while the deployed estate refuses the
  write", shipped three times (`docs/current-architecture.md:176-183`).
- Secrets: Key Vault references and Container App secrets with
  per-secret `Key Vault Secrets User` grants (`infra/modules/platform.bicep:382-398`,
  `:555-563`; `docs/operations.md:784-802`). **A plaintext bootstrap
  verification account is committed**: `src/Pegasus.Web/appsettings.json:10-13`
  (`claudeuiverification`), documented as operator-requested and
  "must be removed before go-live" (`docs/operations.md:768-775`).
- Telemetry: Web instrumented since release 19; Worker
  `AddApplicationInsightsTelemetryWorkerService()`
  (`src/Pegasus.Worker/Program.cs:12-43`); the Log Analytics workspace runs a
  **0.1 GB/day cap resetting 03:00Z** that the estate exhausts within hours,
  so most of each UK working day is blind and the two alert rules cannot
  fire in that window (`docs/operations.md:363-369`, `:637-638`,
  `docs/current-architecture.md:160-175`, PLAT-034; PLAT-036 proposes
  raising or earning back the quota). Adaptive sampling is on; the Worker
  produces most volume. Alert rules: `pegasus-prod-web-http5xx` and
  `pegasus-prod-application-exceptions` (`infra/modules/platform.bicep:576-689`).
- Evidence tiers and sizes: `docs/engineering.md:72-89` (tier 7 browser/
  accessibility, tier 9 security/observability, tier 10 performance —
  **eight concurrent operators, 2,000 cases/month, 10 MiB single-file
  limit**, tier 11 migration/recovery).
- Existing browser-lane accessibility scan: `Deque.AxeCore.Playwright`
  in `tests/Pegasus.IntegrationTests/Browser/AccessibilityTests.cs`.
- Reader/resource limits enforced before Core (`docs/current-architecture.md:222-236`),
  relevant to malformed-attachment handling.

Official documentation and tooling (fetched 2026-08-23):

- `axe-windows` CLI (`AxeWindowsCLI.exe`) — automated UIA accessibility scans
  for any Windows app, the engine behind Accessibility Insights for Windows:
  <https://github.com/microsoft/axe-windows>.
- WinUI UI automation harness `winapp ui` and the desktop performance tooling
  are described in the vendored `winui-ui-testing` skill
  (`.codex/skills/winui-ui-testing/SKILL.md`) and the dotnet diagnostics
  skills (`dotnet-trace-collect`, `analyzing-dotnet-performance`,
  `dump-collect`; dotnet/skills `98f84851`).
- Credential Locker guidance: passwords only, not larger blobs; limit of 20
  credentials per app in an AppContainer
  (<https://learn.microsoft.com/windows/apps/develop/security/credential-locker>).
  DPAPI (`System.Security.Cryptography.ProtectedData`, CurrentUser scope) is
  the alternative chosen in 02/04.

### Assumptions

- The lowest-spec office workstation is not yet recorded; the baseline
  capture (DSK-10-10) records it before any budget is treated as pass/fail.
- Central desktop telemetry is optional after stabilisation (proposal §18.2);
  the plan assumes **no** desktop App Insights SDK initially — gateway-side
  telemetry plus on-demand diagnostic bundles — unless pilot evidence shows
  a gap.
- Raising the Log Analytics quota is desirable but not assumed; the plan
  measures desktop-era volume first (PLAT-036 carry-over).

## 3. Decisions and assumptions

| Decision | Source | Effect here |
| --- | --- | --- |
| ADR-0109 diagnostics and telemetry retention | 00 | Local rolling redacted logs, bounded retention, diagnostics bundle, gateway telemetry; no OpenTelemetry collector fleet |
| ADR-0102 existing credentials with a token session; ADR-0103 gateway not DB | 00, 04 | Access token in memory, refresh token DPAPI-protected; no DB or provider secret in the package |
| ADR-0105 MSIX/App Installer + minimum-version gate | 04, 09 | Signed package and trusted manifest are the tamper controls; tests here |
| L-02 Test/UAT is a local stack | index, 08 | Security and performance tests that need "production-like" run on the local stack plus the production pilot ring |
| D-002 signing route | 09 | Certificate protection and renewal runbook depend on the route chosen; the control is listed, the mechanism is 09's |

Deviations and Azure notes:

- **Deviation (telemetry SDK):** proposal §18.2 retains App Insights; the
  `configuring-opentelemetry-dotnet` skill is **not** used — the estate uses
  the Application Insights SDK and switching exporters is out of scope.
- ⚠ Raising the Log Analytics daily cap (PLAT-036) is an Azure write,
  conditional on exact-target approval after the volume measurement.
- ⚠ A new alert rule for blocked-client spikes or compatibility-gate
  failures is a Bicep change (`infra/modules/platform.bicep`), conditional
  on approval; the plan records it, does not assume it.
- Everything else in this area is code, tests and local tooling; no Azure
  writes.

## 4. Target state and exit gate

Target state:

- Every threat in §17.3 has a named control and a test that exercises it;
  the package contains no database, Box, Graph, DVLA/DVSA or Azure secret;
  the committed verification account is gone before desktop go-live.
- The desktop writes structured, redacted, bounded logs with a per-launch
  session id and API correlation ids, and can export a diagnostics bundle
  on demand; the gateway and Worker keep reporting to the existing App
  Insights with client-version and blocked-client dimensions; an
  administrator health view describes dependencies without secrets.
- The budgets in §15.1 are measured on the recorded baseline workstation
  for every release candidate and a regression report accompanies the
  release record.

Exit gate (proposal §24 Phase 8): full automated suite passes; accessibility
critical issues resolved (06/08); security review has no unresolved
high-risk item; production-like package tested on the local stack and the
pilot ring; performance regression report attached to the release.

## 5. Work breakdown

Tier numbers follow `docs/engineering.md` § Required evidence tiers.
Routing = subagent · skills · MCP.

| ID | Title | Profile | Depends on | Acceptance | Verification | Tier | Routing |
| --- | --- | --- | --- | --- | --- | --- | --- |
| DSK-10-01 | Threat → control → test register (§17.3 rows: lost/shared workstation session, leaked service credential, over-permission, malicious attachment, duplicate/conflicting writes, compromised update package/feed, sensitive data in logs/temp, provider outage, administrator error) as a living table in this folder | chore | — | Every row names an existing or planned control and a test ticket; §17.2 non-goals listed | Docs review by `pegasus-desktop-reviewer` | 9 | pegasus-desktop-reviewer · winui-code-review (security checklist) · Kanmer |
| DSK-10-02 | Retire the committed bootstrap verification account before desktop go-live (`appsettings.json` `Bootstrap:VerificationAccount` → `{ "Removed": ... }`, deployment, confirm deletion) | fix | operator decision | Account absent in production; secret no longer in source; operations.md updated | Production smoke (`Invoke-ProductionSmoke.ps1` extension); `git grep` shows no password | 9 | pegasus-gateway-dev · pegasus-release · Kanmer |
| DSK-10-03 | Package secret scan: CI step proving the MSIX, desktop config and logs contain no connection string, Key Vault URI value, API key or token (pattern list maintained with the threat register) | feature | 09 CI lane | Scan fails the build on a match; negative test with a planted secret | CI run with planted secret (then removed) | 9 | pegasus-release-packager · winui-packaging, authoring-github-workflows · Learn |
| DSK-10-04 | Token and session security tests: expiry, rotation, revocation on disable and password change, replayed refresh token, access token never persisted, DPAPI blob bound to user | feature | 04 | All `§22.2` security list items for tokens covered; negative paths produce the documented problem types | `dotnet test` in `Pegasus.Api.ContractTests` + desktop VM/infra tests | 9 | pegasus-test-engineer · code-testing-agent, run-tests · Learn |
| DSK-10-05 | Authorization and direct-object tests for every `/api/v1` command: role bypass, foreign case/document ids, version spoofing of `X-Pegasus-Client-Version`, update-manifest tampering rejected by signature | feature | 03 endpoints | Each endpoint has an allow and a deny test; tampered `.appinstaller`/package fails install | Contract tests; packaging test from 08 | 9 | pegasus-test-engineer, pegasus-release-packager · test-gap-analysis, winui-packaging · Learn |
| DSK-10-06 | Malformed upload and unsafe path tests on upload-session endpoints (limits from `IntakeEnvelopeLimits`, reader limits, path traversal, content sniffing) | feature | 03 upload endpoints | Limits enforced before Core; safe filenames; no-sniff | Integration tests with fixtures | 3 | pegasus-test-engineer · code-testing-agent · Learn |
| DSK-10-07 | Desktop temp files and cache: per-user ACLs, bounded retention, secure delete on logout/uninstall where feasible, no PII in file names | feature | 02 diagnostics/cache | ACL and retention verified on a clean Windows 11 machine | `Pegasus.Packaging.Tests` + manual check | 9 | winui-dev · winui-dev-workflow, winui-code-review · Learn |
| DSK-10-08 | Dependency and vulnerability scanning: `dotnet list package --vulnerable --include-transitive` in CI, NuGet audit, SBOM artifact (09), reviewed update PRs; no automatic major Windows App SDK bumps | chore | 09 CI lane | CI fails on known-high vulnerabilities; SBOM published with each package | CI run | 1 | pegasus-release-packager · directory-build-organization, authoring-github-workflows · Learn |
| DSK-10-09 | Desktop diagnostics: structured rolling logs (bounded size, retention), redaction by default (no PII, no attachment content, no tokens), per-launch session id, correlation ids, "Export diagnostic bundle" (logs, app/Windows/package/dependency versions, last compatibility response) | feature | 02 foundation | Bundle reproduces a reported failure without secrets; redaction unit-tested | Unit tests; manual bundle review | 9 | winui-dev · winui-dev-workflow, winui-code-review · Learn |
| DSK-10-10 | Performance baseline: record the lowest-spec office workstation, data sizes (tier 10), web-app timings for the same workflows; publish the budgets table (§15.1) as targets | chore | 01 baseline capture | Baseline file committed with hardware, data set, method | Measurement log | 10 | pegasus-ui-verifier · analyzing-dotnet-performance, dotnet-trace-collect · Learn |
| DSK-10-11 | Profiling procedure and tooling: release builds, WPR/WPA traces, `dotnet-counters`/`dotnet-trace`, memory snapshots before/after repeated navigation, API and provider timings, cold/warm start, update launch, constrained network | chore | DSK-10-10 | Runbook in this folder; scripts under `eng/verification/` | Dry run on the local stack | 10 | pegasus-ui-verifier · dotnet-trace-collect, dump-collect, analyzing-dotnet-performance · Learn |
| DSK-10-12 | Performance review checklist (§15.2) wired into `pegasus-desktop-reviewer` and the PR template: x:Bind, virtualization, paging, lazy sections, decode-to-size, dispose, off-UI-thread, cancellation, coalesced refresh, no duplicate subscriptions, single `IHttpClientFactory` pipeline, JSON compression only, bounded async logging | chore | 02 | Checklist referenced by the reviewer agent instructions; first three slices reviewed against it | Review records | 1 | pegasus-desktop-reviewer · winui-code-review, analyzing-dotnet-performance · Kanmer |
| DSK-10-13 | Release-candidate performance regression report template and CI/local job: budgets vs measured, deltas vs previous release, ten-operator-plus-worker load on the local stack | feature | DSK-10-11, 08 stack | Report produced for each release candidate; failing budget blocks release unless waived with evidence | Report artifact | 10 | pegasus-ui-verifier · analyzing-dotnet-performance, run-tests · Learn |
| DSK-10-14 | Gateway telemetry dimensions: client version, channel, blocked-client count, update-required responses, provider dependency timings, correlation id propagation; verify against the quota window; measure desktop-era volume before any quota request | feature | 03, 04 | Dimensions visible in App Insights during an uncapped window; volume report written | KQL checks via Azure MCP `monitor` read-only | 9 | pegasus-gateway-dev, pegasus-azure-auditor · appinsights-instrumentation, azure-diagnostics · Azure MCP `monitor`, `applicationinsights`; Learn |
| DSK-10-15 | Administrator health surface: authenticated `/api/v1/admin/health` describing gateway, database, Worker last successful cycle per function, Box, DVLA/DVSA, update-feed reachability, minimum client version — no secrets; desktop Operations/Settings shows it | feature | 03, 07 | Every dependency has a state and an "obtained at"; secrets absent (test) | Contract tests; `winapp ui` script | 9 | pegasus-gateway-dev, winui-dev · dotnet-webapi, winui-design · Learn |
| DSK-10-16 | ⚠ Alerting follow-ups (conditional): blocked-client spike rule and quota decision (PLAT-036 carry-over); Bicep change and cap change only with exact-target approval | chore | DSK-10-14 | Decision recorded with volume evidence; if approved, `platform.bicep` change reviewed with `azure-validate` what-if | Docs + what-if output | 9 | pegasus-azure-auditor · azure-diagnostics, azure-validate · Azure MCP read-only |
| DSK-10-17 | Reliability: operation model in the desktop (correlation id, explicit state not-started/running/succeeded/failed/cancelled/uncertain, cancellation, idempotency key, recovery advice) and crash recovery (encrypted local drafts only for approved long forms, recovery offer after abnormal exit, cleared on save, never continue corrupted) | feature | 02, 05 case edit slice | VM tests for each state; draft recovery test; unhandled exception path writes bundle and exits | VM tests; manual crash injection | 7 | winui-dev, pegasus-test-engineer · winui-dev-workflow, code-testing-agent · Learn |
| DSK-10-18 | PLAT-035 carry-over: automated check that every table a composition root writes has the runtime-role grant (build or test gate), so gateway tables added for the desktop cannot ship ungranted | fix | 03 | Test fails on an ungranted write; the three shipped regressions are covered | CI run | 11 | pegasus-gateway-dev, pegasus-test-engineer · optimizing-ef-core-queries, code-testing-agent · Kanmer |

## 6. Routing table

| Work type | Subagent | Skills (pinned source) | MCP tools |
| --- | --- | --- | --- |
| Security review and checklists | `pegasus-desktop-reviewer` (read-only) | `winui-code-review` security/performance checklists (win-dev-skills v0.5.0 `f1028dd5`); project skill `pegasus-desktop` | Microsoft Learn for API verification |
| Security and authorization tests | `pegasus-test-engineer` | `code-testing-agent`, `run-tests`, `test-gap-analysis`, `assertion-quality` (dotnet/skills `98f84851`, plugin dotnet-test) | Kanmer |
| Package/CI controls (secret scan, SBOM, vulnerability audit) | `pegasus-release-packager` | `winui-packaging`, `authoring-github-workflows`, `directory-build-organization` | Microsoft Learn |
| Desktop diagnostics, temp/cache hygiene, reliability model | `winui-dev` | `winui-dev-workflow`, `winui-code-review` | Microsoft Learn (`ProtectedData`, file ACL APIs) |
| Performance baseline, profiling, regression report | `pegasus-ui-verifier` | `analyzing-dotnet-performance`, `dotnet-trace-collect`, `dump-collect` (dotnet/skills, plugin dotnet-diag); `winui-ui-testing` for scripted runs | — |
| Telemetry dimensions and health | `pegasus-gateway-dev` | `dotnet-webapi`, `appinsights-instrumentation` (azure-skills `1a03acfb`) | Microsoft Learn |
| Telemetry verification, quota and alert decisions (read-only unless approved) | `pegasus-azure-auditor` | `azure-diagnostics`, `azure-validate` (what-if only when a write is approved), `azure-cost` for quota cost | Azure MCP read-only `monitor`, `applicationinsights`, `group_resource_list` |

Not used: `configuring-opentelemetry-dotnet` (App Insights SDK retained),
`azure-compliance`/`azqr` beyond an optional read-only posture scan at
Phase 8, `entra-*` skills.

## 7. Risks and traps

| Risk / trap | Mitigation |
| --- | --- |
| App Insights blind window (0.1 GB/day cap) hides production errors for most of each working day (PLAT-034) | Desktop diagnostics bundle is the primary support tool; gateway structured logs; measure volume, then decide the quota (DSK-10-14/16) |
| Runtime-role grants missing on new tables (shipped three times) | DSK-10-18 gate; `Test-MigrationGrants.ps1` stays in CI |
| Plaintext verification account shipped to go-live | DSK-10-02 is a Phase 8 exit-gate item |
| Secrets leaking through desktop logs or the diagnostics bundle | Redaction by default with unit tests; bundle review step; secret scan of bundles in the packaging tests |
| "Remember me" convenience turning into stored passwords | Forbidden: only the refresh token is stored (04); tests assert no password persistence |
| Performance budgets judged on a fast developer machine | Baseline workstation recorded first (DSK-10-10); measurements in release builds only |
| Memory growth from image/document views and event subscriptions | Review checklist (DSK-10-12); repeated-navigation snapshots in the profiling procedure |
| Crash handling that swallows exceptions and continues | Unhandled exception path writes the bundle and exits; tested |
| Alert/quota changes made casually | ⚠ items require exact-target approval and `azure-validate` what-if |
| Scope creep into obfuscation, anti-tamper, licensing | §17.2 non-goals restated in the threat register |

## 8. Documentation changes

- ADR-0109 (diagnostics and telemetry retention) authored per 00; ADR-0102
  and ADR-0105 cite the controls here.
- `docs/operations.md`: remove the verification-account clause once
  DSK-10-02 ships; add the desktop diagnostics-bundle collection procedure
  and the performance regression report location; alert/quota state if
  changed.
- `docs/runbook.md`: diagnostics collection for desktop support; profiling
  runbook pointer (`eng/verification/`).
- `docs/current-architecture.md`: health surface, telemetry dimensions,
  retained gateway facts after each release.
- `docs/engineering.md`: tier 9/10 evidence examples for desktop artefacts
  (bundle, regression report); performance review checklist reference.
- `docs/capabilities.md`: `DSK` rows for diagnostics bundle and admin
  health; PLAT-035/PLAT-036 remain upstream-carried platform items
  ([01 · carry-over](../01-inventory-and-parity/upstream-kanmer-carryover.md)).
