# Azure resource register

Every resource the conversion touches, reads, or deliberately leaves alone.
Pre-filled on 2026-08-23 from `infra/main.bicep`, `infra/modules/platform.bicep`,
and `docs/operations.md` (release table lines 311–332; `.azure/deployment-plan.md`
for subscription/tenant). Ticket DSK-01-08 verifies every row with read-only
Azure MCP calls and attaches the output. **No write is performed by any
command in this file.** Intended tags are recorded, not applied (applying a
tag is a write that needs exact-target approval; listed in area 11).

Scope facts:

- Subscription `e6076573-23a5-46a8-acef-7e22d264e5db`, tenant
  `858cf5b3-aa0a-47a6-9b40-4851fd0afa94` (`.azure/deployment-plan.md:24-27`).
- Resource group `rg-pegasus-prod`, region `uksouth` (`infra/main.bicep:71`,
  `:32`); common tags `app=pegasus`, `environment=prod`,
  `managedBy=azd-bicep`, `release=0.1.0-alpha.1` (`main.bicep:72-77`).
- Activation gates are fail-closed: the web app materialises only with
  `webActivation == 'approved'` plus a valid digest and revision suffix
  (`platform.bicep:35`); Worker functions are disabled unless
  `workerActivation == 'approved-live-worker'` (`platform.bicep:36`,
  `:531-539`).
- Current production release: release 20 (2026-08-22), migration head
  `20260822044425_GrantWorkerCaseDocuments`; the "release 14" sentence at
  `docs/operations.md:295` is drift.

Proposal §19 target-position vocabulary: *Retain*, *Retain, simplified*,
*Consolidate into gateway*, *Retain or repurpose*, *Reassess after
stabilization*, *Deprovision candidate* (only after cutover and §19.2).

## Register

| Resource | Type | Declared at | Used by (code path) | Proposal §19 target position | Desktop-conversion impact | Deprovision candidate? | Read-only verification command |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `rg-pegasus-prod` | Resource group | `infra/main.bicep:79` | Everything below | Retain | None; ⚠ any new resource (D-002/D-003) would land here by infra/ change | No | Azure MCP `group_resource_list` (rg `rg-pegasus-prod`) |
| `pegasus-prod-logs-<suffix>` | Log Analytics workspace (PerGB2018, 31 d, 0.1 GB/day cap) | `platform.bicep:46` | App Insights backing store; ACA env diagnostics | Retain; reassess after stabilization | Telemetry for gateway and blocked-client counts; cap blinds working-hour queries (PLAT-034) | No | Azure MCP `monitor` workspace show; check `workspaceCapping.dataIngestionStatus` |
| `pegasus-prod-appi-252ow37gij` | Application Insights (workspace-based, `DisableLocalAuth`) | `platform.bicep:56` | `src/Pegasus.Web/Program.cs:193-199` (`AddApplicationInsightsTelemetry`, Entra ingestion), `src/Pegasus.Worker/Program.cs:14` | Retain; reassess for desktop telemetry later | Client version distribution and compatibility rejections logged here | No | Azure MCP `applicationinsights` show |
| `pegasus-prod-operations` | Action group (email receiver) | `platform.bicep:68` | Alert rules below | Retain | None | No | `az monitor action-group show` |
| `pegasusprodkv252ow37g` | Key Vault (Standard, RBAC, soft delete 90 d, purge protection); secrets only, no certificates | `platform.bicep:85` | Box/DVLA/DVSA/Automation secrets via references (`platform.bicep:382-398`, `:555-563`) | Retain | ⚠ D-002 OV-cert option would add a certificate; otherwise read-only | No | Azure MCP `keyvault` list secrets (names only) |
| `pegtrans<suffix>` | Storage account (transport; Standard_LRS; shared key disabled) | `platform.bicep:100` | Worker queues/blobs (`WorkerAzureClientFactory.cs`), Functions package container | Retain | ⚠ D-003 option "existing account, new container" could use this account or custody; not both | No | Azure MCP `storage` account show; container/queue list |
| `app-package` container | Blob container on transport account | `platform.bicep:117-127` | Functions deployment package (`platform.bicep:489+`) | Retain | None | No | Azure MCP `storage` container show |
| `intake-work`, `intake-work-poison`, `external-work`, `external-work-poison` | Storage queues | `platform.bicep:129-152` | Worker `IntakeWorkFunction`, `IntakePoisonFunction`, `ExternalWorkFunction`, `ExternalPoisonFunction`; dispatcher | Retain (cloud required: unattended) | None | No | Azure MCP `storage` queue list |
| `pegcustody<suffix>` | Storage account (custody; Standard_LRS) | `platform.bicep:154` | Web + Worker intake artifacts, auth ring, box links | Retain | ⚠ D-003 candidate host for a `desktop-releases` container (anonymous read) | No | Azure MCP `storage` account show; `allowBlobPublicAccess` value |
| `transient-intake` container | Blob container | `platform.bicep:177-193` | `AzureBlobIntakeArtifactStore.cs` (Web + Worker, Storage Blob Data Owner scoped) | Retain | None | No | Azure MCP `storage` container show |
| `authentication-ring` container | Blob container | `platform.bicep:177-193` | Data Protection key ring `keys.xml` (`Program.cs:172-176`) | Retain | Token issuance for the desktop depends on the same ring (OpenIddict + DP) | No | Azure MCP `storage` container show |
| `box-links` container | Blob container | `platform.bicep:177-193` | Box link bindings (Web + Worker Blob Data Contributor) | Retain | None | No | Azure MCP `storage` container show |
| `pegasus-prod-sql-252ow37gij` | Azure SQL logical server (Entra-only auth, TLS 1.2, public network, `AllowAzureServices` rule) | `platform.bicep:195`, `:223` | Web + Worker via managed identity, runtime roles | Retain | Desktop never connects (ADR-0103); gateway only | No | Azure MCP `sql` server show, firewall rules list |
| `pegasus` database | Azure SQL database (S0, 250 GB, not zone redundant) | `platform.bicep:214` | `PegasusDbContext`, 64 migrations | Retain | Expand/contract migrations for any gateway change | No | Azure MCP `sql` db show |
| `pegasusprodacr252ow37gij` | Container Registry (Basic, admin disabled) | `platform.bicep:229` | Web image pushed by `oras` in the release skill | Retain, simplified | Gateway image continues to ship here | No | Azure MCP `acr` registry show |
| `pegasus-prod-aca-env-<suffix>` | Container Apps managed environment (+ diagnostics to LA) | `platform.bicep:241`, `:252` | Hosts the Web container app | Retain | None | No | Azure MCP `containerapps` environment show |
| `pegasus-prod-web-id-*`, `pegasus-prod-worker-id-*` | User-assigned managed identities | `platform.bicep:264`, `:270` | `AzureIdentity:WebClientId` / `WorkerClientId` pinned `DefaultAzureCredential` (`Program.cs:158-171`, Worker `Program.cs`) | Retain | ⚠ D-002 Artifact Signing would add a signing identity/role, not reuse these | No | `az identity show` |
| 10 role assignments | RBAC (AcrPull web; Monitoring Metrics Publisher web+worker; Storage Blob Data Owner/Contributor; Queue + Table Data Contributor; KV Secrets User per secret) | `platform.bicep:276-352`, `docs/operations.md:784-802` | Least-privilege access for the two identities | Retain | New desktop-related settings need no new roles unless D-002/D-003 choose Azure | No | Azure MCP `role` assignment list (scope rg) |
| `pegasus-prod-web-252ow37gij` | Container App (single revision, external ingress 8080, 1–1 replicas, cpu 1.0 / 2 Gi, 3 KV-backed secrets, probes on `/health/*`) | `platform.bicep:354-478` | `src/Pegasus.Web` — Razor Pages + Identity + OpenIddict + MCP; will host `/api/v1` (L-01) | Retain, simplified (gateway) | ⚠ New app settings/secrets for the compatibility gate and desktop client (Azure write via infra/); Razor Pages retired only after cutover | No | Azure MCP `containerapps` app show (revision, image digest, env vars names) |
| `pegasus-prod-worker-plan-<suffix>` | App Service plan FC1 FlexConsumption | `platform.bicep:480` | Functions host | Retain | None | No | `az functionapp plan show` |
| `pegasus-prod-worker-252ow37gij` | Function App (`functionapp,linux`, dotnet-isolated 10.0, deploy from blob with UAMI, max 20 instances, 2048 MB) | `platform.bicep:489` | `src/Pegasus.Worker` 9 functions (Graph poll, queues, sweeps) | Retain (cloud required) | Unchanged by the conversion; status surfaced via gateway | No | Azure MCP `functionapp` show; settings names incl. `AzureWebJobs.*.Disabled` |
| `pegasus-prod-web-http5xx` | Metric alert (Sev1, PT5M, conditional) | `platform.bicep:576` | Web 5xx | Retain through cutover | Gateway errors from desktop calls surface here | Later candidate (web-only monitoring) | `az monitor metrics alert show` |
| `pegasus-prod-application-exceptions` | Scheduled query rule (Sev1, PT5M/PT15M, dedup KQL) | `platform.bicep:617` | Application exceptions | Retain through cutover | Same | No | `az monitor scheduled-query show` |
| `pegasus-prod-monthly` | Consumption budget (75, monthly, 2026-08-01 → 2036-08-01, alerts 50/80/100 % + forecast) | `infra/main.bicep:114` | Cost guard | Retain | ⚠ D-002/D-003 choices add small monthly cost (≈$10 signing; pennies storage) | No | Azure MCP `pricing`/`cost` read; `az consumption budget show` |

Intended tags for the register (recorded, not applied): `desktop-conversion=phase0-inventory`,
`owner=<name>`, `codepath=<file>` — see area 11 before proposing the write.

## Declared absent (do not assume, do not add by default)

Confirmed absent from `infra/` and `docs/operations.md` on 2026-08-23; the
proposal §19.1 "do not add by default" list applies:

| Not present | Note |
| --- | --- |
| Front Door / CDN / custom domain | Custom domain is a deferred seam (`docs/operations.md:902`) |
| Azure SignalR | Poll/refresh is the design (proposal §4.1 notifications) |
| Service Bus / Event Hubs / Event Grid | Storage queues only (`docs/operations.md:87` forbids adding without a new accepted need) |
| Redis / distributed cache | Memory cache + short lifetimes (proposal §10.6) |
| API Management | Rejected for ten users (proposal §2.2) |
| Deployment slots / S1 / multi-region / private networking | `docs/operations.md:910-918` "Not planned" |
| Any dev/test/UAT/staging Azure environment | ADR-0014; L-02 keeps Test/UAT local |
| Document Intelligence / OCR service | Absent (`.azure/deployment-plan.md:69`) |
| Key Vault certificates, signing service, App Installer feed | None exist, and none will: D-002 chose a self-managed certificate and D-003 an in-house UNC share (both 2026-08-23) |

## Read-only verification procedure (DSK-01-08)

1. `pegasus-azure-auditor` runs, with the subscription pinned: Azure MCP
   `group_resource_list` for `rg-pegasus-prod`; then per type `storage`,
   `keyvault` (secret names only), `sql`, `containerapps`, `functionapp`,
   `monitor`, `applicationinsights`, `acr`, `role`; record the JSON.
2. Compare the returned set with this register; any resource present in
   Azure but not declared in `infra/` is recorded as drift (not removed).
3. Record `allowBlobPublicAccess` on both storage accounts and the Log
   Analytics `workspaceCapping` state — both feed decisions D-003 and
   PLAT-036.
4. Attach outputs to the ticket proof; update the "Used by" column where the
   code citation was approximate; never run `az ... create|update|delete`.
