# 11 · Azure disposition

Area plan for the Azure estate during and after the desktop conversion: the
resource register, the target position of every resource, the complete list
of Azure writes the conversion *may* need (each ⚠, each conditional), the
cloud-dependency records, and the deprovisioning method for after cutover.
The rule of this area is simple: **read freely, write only with exact-target
approval, deprovision nothing before cutover, observed use and rollback
approval**.

## 1. Purpose and proposal coverage

- §4 the cloud-justification test and the placement decisions table, turned
  into per-capability cloud-dependency records (Appendix B).
- §19 the Azure service disposition table, instantiated against the real
  estate (which is smaller than the proposal's generic list).
- §19.1 "do not add by default" enforced as a checklist for every ticket
  that touches Azure.
- §19.2 the deprovisioning method as a post-cutover checklist.
- §27 acceptance items 15 ("runtime Azure dependencies match the approved
  cloud-boundary register") and 16 ("no Azure resource removed before
  dependency, backup and rollback verification").
- §24 Phase 10 cloud rationalisation (only its preparation; execution waits
  for cutover).

It does not own the gateway's deployment route (existing `pegasus-release`
skill) nor desktop packaging ([area 09](../09-release-update-and-distribution/README.md)).

## 2. Evidence base

### Facts

Estate declared in `infra/modules/platform.bicep` (subscription scope in
`infra/main.bicep`; resource group `rg-pegasus-prod`, region `uksouth`,
`infra/main.bicep:71` and `:32`; tags `app=pegasus`, `environment=prod`,
`managedBy=azd-bicep`, `release=0.1.0-alpha.1`, `:72-77`):

| # | Resource | Name / pattern | Notes | Bicep lines |
| --- | --- | --- | --- | --- |
| 1 | Resource group | `rg-pegasus-prod` | conditional create | `main.bicep:79` |
| 2 | Log Analytics workspace | `pegasus-prod-logs-<suffix>` | PerGB2018, 31-day retention, 0.1 GB/day cap (PLAT-034) | `platform.bicep:46` |
| 3 | Application Insights | `pegasus-prod-appi-252ow37gij` | workspace-based, local auth disabled | `:56` |
| 4 | Action group | `pegasus-prod-operations` | one email receiver | `:68` |
| 5 | Key Vault | `pegasusprodkv252ow37g` | Standard, RBAC, soft delete 90d, purge protection; secrets only | `:85` |
| 6 | Storage (transport) | `pegtrans<suffix>` | Standard_LRS, shared-key disabled; container `app-package`; queues `intake-work`, `intake-work-poison`, `external-work`, `external-work-poison` | `:100-152` |
| 7 | Storage (custody) | `pegcustody<suffix>` | Standard_LRS; containers `transient-intake`, `authentication-ring`, `box-links` | `:154-193` |
| 8 | Azure SQL server + database | `pegasus-prod-sql-252ow37gij` / `pegasus` | Entra-only auth, S0, 250 GB max, firewall `AllowAzureServices` | `:195-223` |
| 9 | Container registry | `pegasusprodacr252ow37gij` | Basic, admin user disabled | `:229` |
| 10 | Container Apps environment | `pegasus-prod-aca-env-<suffix>` | logs to Azure Monitor; diagnostic settings | `:241-252` |
| 11 | User-assigned identities | `pegasus-prod-web-id-*`, `pegasus-prod-worker-id-*` | | `:264-270` |
| 12 | Role assignments (10) | AcrPull, Monitoring Metrics Publisher, Storage Blob Data Owner/Contributor, Queue/Table Data Contributor, Key Vault Secrets User | container/queue/secret-scoped | `:276-352`, `docs/operations.md:784-802` |
| 13 | Container App (Web) | `pegasus-prod-web-252ow37gij` | single revision, external ingress 8080, min/max 1 replica, cpu 1.0 / 2 GiB (in-process Chromium, ADR-0028), Key Vault-backed secrets | `:354-478` |
| 14 | Functions plan + app (Worker) | `pegasus-prod-worker-plan-<suffix>` / `pegasus-prod-worker-252ow37gij` | FlexConsumption FC1, dotnet-isolated 10.0, nine functions gated by `AzureWebJobs.<fn>.Disabled` | `:480-563` |
| 15 | Alert rules | `pegasus-prod-web-http5xx`, `pegasus-prod-application-exceptions` | Sev1 | `:576-689` |
| 16 | Budget | `pegasus-prod-monthly` | 75, notify 50/80/100% and forecast | `main.bicep:114` |

What does **not** exist (verified against `infra/` and
`docs/operations.md:87`, `:902`, `:910-918`): Front Door/CDN, custom domain
(deferred seam), SignalR, Service Bus, Event Hubs, Redis, Cosmos DB,
PostgreSQL, Azure Files/ADLS, API Management, VNet/private endpoints, App
Service plan for Web (Container Apps per ADR-0015), deployment slots,
Document Intelligence, any dev/test/UAT/staging environment (ADR-0014).
`docs/operations.md:87` forbids adding Service Bus, Event Hubs, Cosmos,
Redis, PostgreSQL, Azure Files, ADLS without a new accepted need; `:910-918`
lists permanent "Not planned" boundaries (malware scanning, multi-region,
private networking, separate staging/QA/UAT/demo, S1 or slots).

Governance facts: activation gates fail closed (`platform.bicep:35-36`,
`docs/operations.md:829-842`); read-only Azure checks are permitted with no
per-target approval, every write needs explicit approval for the exact
target (`docs/runbook.md:776-788`); ADR-0014 two environments; ADR-0007
authorised-terminal deployment; `docs/operations.md` is the sole
current-state owner (its narrative line `:295` "release 14" is stale against
its own release table `:311-332` — treat the table as current). Current
observability gap: the workspace cap exhausts within hours so most of each
UK working day is blind (PLAT-034; `docs/current-architecture.md:160-175`).

Code paths that use each resource (from `src/Pegasus.Web/Program.cs:130-176`
required production settings and `platform.bicep` app settings): Web →
SQL, custody storage (`authentication-ring` Data Protection keys,
`transient-intake`, `box-links`), Key Vault (Box config/secret, automation
client secret), App Insights, ACR; Worker → SQL, transport storage (queues,
`app-package`), custody storage (`transient-intake`, `box-links`), Key Vault
(Box, DVLA, DVSA secrets), App Insights.

### Assumptions

- No Azure writes are needed for Phases 0–1; the first possible writes
  arrive with Phase 2 (desktop gateway flag/settings) and the first pilot
  feed (D-002 only; D-003 chose an in-house UNC share and needs no Azure).
- The desktop client will never call Azure directly — every dependency is
  reached through the gateway or the feed — so the register's "used by"
  column gains at most the feed host and the signing service.
- Costs stay within the existing 75/month budget; any new resource is
  priced before approval (Azure MCP `pricing`).

## 3. Decisions and assumptions

- **No deprovisioning during the conversion** (proposal §19, §27 item 16,
  user constraint). The disposition table below records target positions
  only.
- **Writes are conditional and enumerated.** The complete list is in §5's
  "Conditional Azure writes" table; anything not listed is a plan change.
- **Minimum client version is DB-backed, not config-backed** (recommended
  in [area 04](../04-auth-session-update-and-startup/README.md)) so that
  every release does not require a Container App settings write.
- **Feature flag `Features:DesktopGateway`** is a Container App app setting
  change ⚠ when it is first enabled in production (one write, exact-target
  approval), mirroring how `Features:AutomationMcp` was enabled
  (`platform.bicep:429`).
- **OpenIddict desktop client** is a database seed (not an Azure write); its
  configuration may add app settings ⚠ only if a secret is involved (a
  public client has none — preferred).
- **Telemetry**: the Log Analytics daily cap (PLAT-036) is only raised if
  measurements after the desktop pilot show it is needed ⚠.
- **The Web Container App** appears twice in the proposal's table: as
  "existing API hosting — retain, simplified" and as "web frontend host —
  deprovision candidate". It is one resource: cutover means shrinking the
  image (Razor Pages, Playwright base image once local rendering parity
  passes), not deleting the app.
- Deviation: the proposal's §19 table lists resources Pegasus does not have
  (Front Door, SignalR, Service Bus, Redis, slots); they are recorded as
  "does not exist — do not add" rather than as candidates.

## 4. Target state and exit gate

Target state: a register in `docs/desktop/01-inventory-and-parity/azure-resource-register.md`
(populated by [area 01](../01-inventory-and-parity/README.md) read-only
checks) kept current by each release; cloud-dependency records for every
capability; every Azure write ever performed for the conversion listed with
its approval text and rollback; a post-cutover deprovision checklist ready
but unexecuted.

Exit gate for this area (programme-level §27 items 15–16): runtime Azure
dependencies equal the approved register (proved by the Test/UAT stack run
with only the documented dependencies and by the pilot ring's telemetry and
logs), and no resource has been removed. The deprovision checklist executes
only after Phase 10's cutover, one full business cycle, and explicit
approval.

## 5. Work breakdown

| ID | Title | Profile | Depends on | Acceptance | Verification | Tier | Routing |
| --- | --- | --- | --- | --- | --- | --- | --- |
| DSK-11-01 | Populate the Azure resource register by read-only inventory | chore | DSK-01-05 | Every resource in `platform.bicep` and in the live RG listed with owner/use, target position, and verification command; differences between Bicep and live state recorded | Azure MCP `group_resource_list` output attached; register diff reviewed | 9 | pegasus-azure-auditor · azure-resource-lookup, azure-resource-visualizer · Azure MCP (read), Kanmer |
| DSK-11-02 | Cloud-dependency records (Appendix B) for every capability | chore | DSK-11-01 | Records for graph-intake, box-custody, dvla-dvsa-lookup, report-rendering, authentication-session, update-feed, telemetry, database, transport; each with the six cloud-justification answers | Records reviewed against §4.1 placement table | 1 | pegasus-azure-auditor · — · Kanmer |
| DSK-11-03 | Conditional Azure writes catalogue with approval templates and rollback | chore | DSK-09-10, DSK-04-05 | Every ⚠ item in areas 04, 09, 10 mirrored here with trigger, exact target, Bicep location, approval text, rollback | Cross-check grep of `⚠` across `docs/desktop/` | 1 | pegasus-azure-auditor, pegasus-release-packager · — · Kanmer |
| DSK-11-04 | Cost baseline and forecast | chore | DSK-11-01 | Current monthly cost by resource; projected delta for each D-002/D-003 option; budget impact | `azure-cost` skill output; Azure MCP `pricing` | 9 | pegasus-azure-auditor · azure-cost · Azure MCP (read) |
| DSK-11-05 | Resource-health and advisor read of the estate | chore | DSK-11-01 | Health, advisor recommendations and compliance findings recorded; none acted on without a ticket | Azure MCP `resourcehealth`, `advisor`; `azure-compliance` (azqr) read-only | 9 | pegasus-azure-auditor · azure-compliance, azure-diagnostics · Azure MCP (read) |
| DSK-11-06 | Feature-flag enablement write for `Features:DesktopGateway` (⚠) | chore | DSK-03-01, approval | Setting applied through Bicep + `azd provision` by the release route; operations.md refreshed in the same task | Smoke: `/api/v1/client-compatibility` answers; Razor Pages unaffected | 12 | pegasus-release-packager · pegasus-release, azure-validate (what-if) · Kanmer |
| DSK-11-07 | Register refresh rule per release | chore | DSK-11-01 | `pegasus-release` and desktop runbooks include "refresh register and dependency records"; first refresh done | Review of skill/runbook diff | 1 | pegasus-release-packager · pegasus-release · Kanmer |
| DSK-11-08 | Post-cutover deprovision checklist (prepared, not executed) | chore | DSK-11-02 | §19.2 steps 1–9 as a checklist with candidate list, evidence fields, and approval lines; marked "do not execute before Phase 10 exit" | Review | 1 | pegasus-azure-auditor · — · Kanmer |
| DSK-11-09 | Telemetry cap decision input (PLAT-036) after pilot | spike | DSK-10-07 | Measured desktop-era ingestion volume; recommendation to raise cap or not; ⚠ write only on approval | Azure MCP `monitor`/`applicationinsights` read; note | 9 | pegasus-azure-auditor · azure-diagnostics, appinsights-instrumentation · Azure MCP (read) |

### Resource disposition register

| Resource | §19 conversion phase | Target position | Justification / removal condition | Desktop-conversion impact | Conditional Azure write? |
| --- | --- | --- | --- | --- | --- |
| Azure SQL `pegasus` | Retain | Retain | Shared source of truth for concurrent users | New tables (OpenIddict client row exists; minimum-version setting table) via migrations with runtime-role grants | No (migrations are release-owned, not resource writes) |
| Container App `pegasus-prod-web-*` (gateway + web) | Retain | Retain, simplified | Authentication, authorization, central writes, integration broker | Hosts `/api/v1` and the token flow; web UI retired from the image after cutover; Playwright base image removable after local rendering parity | ⚠ app setting `Features:DesktopGateway` (once); ⚠ image/cpu changes at cutover |
| Functions Worker `pegasus-prod-worker-*` + FC1 plan | Retain | Retain | Must run with all desktops closed (Graph intake, queues, sweeps) | None | No |
| Key Vault `pegasusprodkv252ow37g` | Retain | Retain | Server-held provider credentials | Unchanged unless D-002 chooses an OV certificate | ⚠ certificate import (D-002 option D only) |
| Storage `pegtrans*` (queues, app-package) | Retain | Retain | Worker transport and deployment package | None | No |
| Storage `pegcustody*` (transient-intake, authentication-ring, box-links) | Retain | Retain | Transient custody, Data Protection keys, Box links | None unless D-003 option A chooses a container here | ⚠ container + public access (D-003 A only) |
| Update-feed storage (does not exist, and will not) | — | Not applicable | Mandatory package distribution | **D-003 decided 2026-08-23: an in-house UNC file share.** No Azure resource hosts the feed | **None** — the conditional feed write is withdrawn |
| Log Analytics + App Insights | Retain | Reassess after stabilisation | Migration evidence; optional long-term desktop telemetry | Gateway adds client-version and blocked-client telemetry; cap may need raising | ⚠ daily cap change (PLAT-036) |
| Alert rules (2) + action group | Retain | Retain through cutover | Operational signal | Possible third rule for blocked-client spikes | ⚠ Bicep alert addition (optional) |
| ACR Basic | Retain | Retain | Web image store | None | No |
| Container Apps environment | Retain | Retain | Hosts the gateway | None | No |
| UAMIs + role assignments | Retain | Retain | Least privilege | No feed publisher identity is needed (D-003 chose a UNC share); a signing identity may be added if D-002 chooses Azure Artifact Signing | ⚠ RBAC only under D-002 A/B/D |
| Budget | Retain | Retain | Cost guard | Desktop-era costs reviewed in DSK-11-04 | No |
| Front Door/CDN, SignalR, Service Bus, Redis, APIM, slots, test env | — | Does not exist — do not add (§19.1) | n/a | n/a | n/a |
| Server-side report renderer (in the Web image) | Retain during parity | Candidate after native renderer passes (L-03) | Remove only after all report types match and no unattended use remains; would supersede ADR-0028 | Image shrink, cpu/memory reduction possible | ⚠ Container App resource change at that time |
| Legacy web-only monitoring alerts | Retain through cutover | Candidate | After web retirement and replacement runbooks | None until cutover | ⚠ Bicep change at that time |

### Conditional Azure writes (complete list, all ⚠)

| Write | Trigger | Exact target | Bicep location | Approval | Rollback |
| --- | --- | --- | --- | --- | --- |
| Enable `Features:DesktopGateway` (and any related non-secret settings) on the Web Container App | First production deployment of the gateway API (Phase 2) | `pegasus-prod-web-252ow37gij` env/app settings | `platform.bicep:354-478` (container app env block, by analogy with `:429`) | Exact-target approval; applied via `azd provision` by the `pegasus-release` route | Set the flag to `false` and re-provision |
| Minimum client version setting | Only if config-backed (not recommended) | same | same | same | same |
| ~~Update-feed container with anonymous read + RBAC for publisher~~ | **Withdrawn 2026-08-23** — D-003 chose an in-house UNC share, so no Azure resource, container, public-access setting or publisher RBAC is required for distribution | — | — | — |
| New storage account + container + RBAC | D-003 option B | new account in `rg-pegasus-prod` | new module/section in `platform.bicep` | D-003 + approval | Delete account |
| Artifact Signing account, identity validation, certificate profile, CI federated credential/app registration, `Trusted Signing Certificate Profile Signer` role | D-002 option A or B | new resources in `rg-pegasus-prod` (or a dedicated RG) | new module | D-002 + approval | Delete resources and role |
| Key Vault certificate import + RBAC for signer | D-002 option D | `pegasusprodkv252ow37g` | `:85` | D-002 + approval | Remove certificate and role |
| Log Analytics daily cap change | PLAT-036 after measurement (DSK-11-09) | `pegasus-prod-logs-<suffix>` | `:46` | Approval | Restore cap |
| Additional alert rule (blocked-client spike) | Area 10 decision | new `Microsoft.Insights` rule | after `:689` | Approval | Delete rule |
| Container App image/cpu changes at cutover (web UI and renderer removal) | Phase 10 | `pegasus-prod-web-252ow37gij` | `:354-478` | Approval after cutover | Redeploy previous image/resources |

Approval text template (fill the angle brackets; one request per write):

> Request `<create | change | assign>` of `<exact resource/setting>` in
> `rg-pegasus-prod` (subscription `e6076573-23a5-46a8-acef-7e22d264e5db`,
> tenant `858cf5b3-aa0a-47a6-9b40-4851fd0afa94`) because `<trigger>`;
> Bicep change at `<file:section>`; applied through `<route>`; rollback
> `<steps>`; nothing else changes.

### Cloud-dependency records (Appendix B, drafts)

```yaml
capability: graph-intake
current_resources: [pegasus-prod-worker-252ow37gij, pegasus-prod-sql-252ow37gij/pegasus, pegtrans*, pegcustody*/transient-intake, pegasusprodkv252ow37g]
desktop_components: [IntakeStatusView, InboxWorkspace (read through gateway)]
cloud_components: [InboxPollFunction, SentEvidencePollFunction, IntakeWorkFunction, poison reconciliation]
reason_cloud: {shared_authority: true, unattended_execution: true, protected_credentials: true, public_callback: false, central_enforcement: true, measured_operational_advantage: true}
data_owned: [approved-mailbox lease and cursor, retained-mail read model, receipts]
failure_mode: [intake-delayed, poison-visible]
monitoring: [last-successful-poll, failures-by-mailbox]
deprovision_candidate: false
```

```yaml
capability: box-custody
current_resources: [pegasus-prod-web-252ow37gij, pegasus-prod-worker-252ow37gij, pegasusprodkv252ow37g (Box secrets), pegcustody*/box-links]
desktop_components: [DocumentBrowser, TransferQueue, Preview]
cloud_components: [BoxCaseCustody, BoxDocumentContentStore, custody retry]
reason_cloud: {shared_authority: true, unattended_execution: true, protected_credentials: true, public_callback: false, central_enforcement: true, measured_operational_advantage: false}
data_owned: [case folder ids, document records]
failure_mode: [custody-terminal-visible, transfer-failed]
monitoring: [custody-failures, token-refresh]
deprovision_candidate: false
```

```yaml
capability: dvla-dvsa-lookup
current_resources: [pegasus-prod-worker-252ow37gij, pegasusprodkv252ow37g (Dvla/Dvsa secrets), pegasus-prod-sql-252ow37gij/pegasus]
desktop_components: [VehicleLookupView]
cloud_components: [DvlaDvsaProductionAdapter, reconciliation sweep]
reason_cloud: {shared_authority: true, unattended_execution: true, protected_credentials: true, public_callback: false, central_enforcement: false, measured_operational_advantage: false}
data_owned: [lookup requests, vehicle evidence]
failure_mode: [provider-unavailable-distinct-from-not-found]
monitoring: [lookup-failures-by-provider]
deprovision_candidate: false
```

```yaml
capability: report-rendering
current_resources: [pegasus-prod-web-252ow37gij (Playwright in image)]
desktop_components: [WebView2Renderer (L-03), ReportPreview]
cloud_components: [PlaywrightAssessmentReportRenderer (until parity), report record/final store through gateway]
reason_cloud: {shared_authority: true (final record), unattended_execution: false, protected_credentials: false, public_callback: false, central_enforcement: true (finality/audit), measured_operational_advantage: false}
data_owned: [report records, final PDFs in Box]
failure_mode: [render-failed-fallback-to-gateway]
monitoring: [render-failures]
deprovision_candidate: false   # renderer removable from the image after parity (supersedes ADR-0028 then)
```

```yaml
capability: authentication-session
current_resources: [pegasus-prod-web-252ow37gij, pegasus-prod-sql-252ow37gij/pegasus (Identity, OpenIddict), pegcustody*/authentication-ring]
desktop_components: [LoginView, SessionManager, CredentialStore]
cloud_components: [OpenIddict token endpoint, Identity, rate limiter, compatibility gate]
reason_cloud: {shared_authority: true, unattended_execution: false, protected_credentials: true, public_callback: false, central_enforcement: true, measured_operational_advantage: false}
data_owned: [accounts, roles, tokens, security events]
failure_mode: [login-unavailable, client-unsupported]
monitoring: [sign-in-rate-limited, blocked-clients-by-version]
deprovision_candidate: false
```

```yaml
capability: update-feed
current_resources: []   # D-003 open
desktop_components: [AppInstaller check, UpdateRequiredView]
cloud_components: [feed host (TBD)]
reason_cloud: {shared_authority: false, unattended_execution: false, protected_credentials: false, public_callback: false, central_enforcement: false, measured_operational_advantage: true}
data_owned: [signed packages, appinstaller files, release manifests]
failure_mode: [feed-unreachable-fail-open (gateway gate fails closed)]
monitoring: [feed-availability, update-success-by-version]
deprovision_candidate: false
```

```yaml
capability: telemetry
current_resources: [pegasus-prod-logs-<suffix>, pegasus-prod-appi-252ow37gij, action group, alert rules]
desktop_components: [DiagnosticsBundle]
cloud_components: [gateway and worker App Insights]
reason_cloud: {shared_authority: false, unattended_execution: true, protected_credentials: false, public_callback: false, central_enforcement: false, measured_operational_advantage: true}
data_owned: [traces, requests, exceptions]
failure_mode: [ingestion-capped (PLAT-034)]
monitoring: [workspaceCapping.dataIngestionStatus]
deprovision_candidate: false
```

```yaml
capability: database
current_resources: [pegasus-prod-sql-252ow37gij/pegasus]
desktop_components: [none — never direct]
cloud_components: [EF stores, runtime roles]
reason_cloud: {shared_authority: true, unattended_execution: true, protected_credentials: true, public_callback: false, central_enforcement: true, measured_operational_advantage: true}
data_owned: [all authoritative case data]
failure_mode: [gateway-ready-false]
monitoring: [/health/ready, sys.database_permissions checks]
deprovision_candidate: false
```

```yaml
capability: transport-queues-and-blobs
current_resources: [pegtrans* queues and app-package, pegcustody*/transient-intake]
desktop_components: [none]
cloud_components: [Worker triggers, AzureBlobIntakeArtifactStore]
reason_cloud: {shared_authority: true, unattended_execution: true, protected_credentials: true, public_callback: false, central_enforcement: false, measured_operational_advantage: true}
data_owned: [transient bytes, work items]
failure_mode: [poison-queues]
monitoring: [poison-depth]
deprovision_candidate: false
```

### Deprovisioning checklist (after cutover only — §19.2)

Do not start before: Phase 10 exit gate met (no user requires the web UI,
dependency map matches target, rollback window expired with approval).
For each candidate (web-only alerts, web UI code and assets in the image,
Playwright base image and cpu/memory once local rendering parity passes,
anything else that appears in the register with "candidate"):

1. Record traffic, dependencies and cost for the resource (Azure MCP read).
2. Confirm the native client passes the full cloud-dependency test with the
   candidate disabled on the Test/UAT stack and, where possible, in a
   non-production-like rehearsal.
3. Remove references in code, IaC, DNS, CI, secrets and monitoring.
4. Back up data/configuration and document restoration.
5. Disable or scale to zero before deleting where the service permits.
6. Observe at least one normal business cycle.
7. Obtain explicit approval (exact target).
8. Delete through infrastructure-as-code (`infra/`) or a recorded change.
9. Verify no orphaned secrets, DNS, storage or billing items remain; refresh
   `docs/operations.md` and `docs/current-architecture.md` in the same task.

## 6. Routing table

| Kind | Name | Use in this area | Source |
| --- | --- | --- | --- |
| Subagent | `pegasus-azure-auditor` | All read-only inventory, cost, health, compliance work; never writes | `.codex/agents/pegasus-azure-auditor.toml` |
| Subagent | `pegasus-release-packager` | Executes approved writes through the existing release route | `.codex/agents/pegasus-release-packager.toml` |
| Subagent | `pegasus-desktop-reviewer` | Reviews the register and writes catalogue | `.codex/agents/pegasus-desktop-reviewer.toml` |
| Skill | `azure-resource-lookup`, `azure-resource-visualizer` | Inventory and diagram of `rg-pegasus-prod` | `microsoft/azure-skills` `1a03acfb` |
| Skill | `azure-cost` | Cost baseline and forecast | same |
| Skill | `azure-compliance`, `azure-diagnostics` | azqr/resource-health/AppLens reads | same |
| Skill | `appinsights-instrumentation` | Guidance for the telemetry-cap decision | same |
| Skill | `azure-validate` | What-if/Bicep validation **only** when a write is approved | same |
| Skill (do not use) | `azure-deploy`, `azure-prepare`, `azure-app-onboard`, `azure-enterprise-infra-planner` | They provision/deploy; the only deployment route is `pegasus-release` | same |
| MCP | Azure MCP read-only tools: `group_resource_list`, `storage`, `keyvault`, `monitor`, `applicationinsights`, `sql`, `containerapps`, `functionapp`, `pricing`, `advisor`, `resourcehealth`, `role`, `subscription_list` | Evidence | azure-skills MCP |
| MCP | Microsoft Learn | Service semantics (storage public access, Artifact Signing, alerts) | `.codex/config.toml` |
| MCP | Kanmer | Tickets and proof | `.codex/config.toml` |

## 7. Risks and traps

- **A write without approval** is the single disqualifying failure of this
  area; the auditor agent is read-only by sandbox and the packager executes
  writes only with the approval text attached to the ticket.
- **Out-of-band resources**: anything created outside `infra/` is invisible
  to `azd provision` and will not be removed by it; all writes go through
  Bicep + the release route.
- **Public-read container beside private data** (D-003 option A) — prefer
  a dedicated account or keep container-scoped access and no shared keys.
- **Telemetry blind spots** (PLAT-034) make "is it still used?" questions
  unanswerable from App Insights for most of the day; use gateway logs,
  action history and the desktop diagnostics bundle before declaring a
  resource unused ("a service is not unused merely because no developer
  remembers it", §19.2).
- **Stale current-state docs** (`docs/operations.md:295`, `CHANGELOG.md`):
  refresh in the same task as any write.
- **Runtime-role grants** travel with migrations, not Azure writes, but a
  missing grant has shipped three times (PLAT-035) — every new table in the
  desktop work (OpenIddict client config, minimum version) needs its
  `Grant*` migration and the expected-matrix update in
  `scripts/Invoke-AzureDatabaseBootstrap.ps1`.

## 8. Documentation changes

- `docs/desktop/01-inventory-and-parity/azure-resource-register.md` is the
  living register; this README owns the disposition and the writes catalogue.
- `docs/operations.md § Production environment` records the in-house UNC
  feed host as a non-Azure dependency (D-003, decided), gains the signing
  identity when D-002 is executed, and gains a "desktop releases" table
  (area 09).
- `docs/current-architecture.md` deployment boundary updated at the first
  production gateway enablement and again at cutover.
- ADR-0101 (cloud split and justification test) and, later, an ADR
  superseding ADR-0028 when the renderer leaves the Web image.
- `docs/capabilities.md`: `DSK` rows for the register and deprovision
  checklist; `docs/open-decisions.md § Azure ownership and retirement
  targets` updated with D-002 and the post-cutover candidates (D-003 needs no
  entry there — it adds no Azure resource).
