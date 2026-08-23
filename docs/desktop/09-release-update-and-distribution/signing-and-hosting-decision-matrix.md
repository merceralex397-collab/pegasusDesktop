# 09 · Signing and hosting decision matrix (D-002, D-003)

Both decisions are **open**. This page gives the operator the options with
their consequences, the read-only checks an agent may run now, the ⚠ Azure
writes each option would need (none executed without exact-target
approval), and a recommendation. The plans in this folder are written so
that either decision can be taken without rewriting them: the
`.appinstaller` template, validator, runbooks and CI lanes are the same; only
the signing step and the upload target differ.

Sources (fetched 2026-08-23):
[Sign an MSIX package — signing options](https://learn.microsoft.com/windows/msix/package/signing-package-overview),
[Sign your MSIX package: end-to-end guide](https://learn.microsoft.com/windows/msix/package/sign-msix-package-guide),
[Artifact Signing resources and roles](https://learn.microsoft.com/azure/artifact-signing/concept-resources-roles),
[Artifact Signing trust models](https://learn.microsoft.com/azure/artifact-signing/concept-trust-models),
[MSIX and CI/CD pipeline signing with Azure Key Vault](https://learn.microsoft.com/windows/msix/desktop/cicd-keyvault),
[Current status of Windows app distribution features](https://learn.microsoft.com/windows/apps/package-and-deploy/distribution-feature-status),
[Installing Windows apps from a web page](https://learn.microsoft.com/windows/msix/app-installer/installing-windows10-apps-web),
[MSIX troubleshooting guide](https://learn.microsoft.com/windows/msix/msix-troubleshooting-guide).

## D-002 · Production code signing

Repository facts: no certificate, signing tool or signing step exists
today; Key Vault `pegasusprodkv252ow37g` holds secrets only
(`infra/modules/platform.bicep:85`); CI has no protected job; release
operations run from an authorised Windows terminal.

| Criterion | A · Azure Artifact Signing, Public Trust | B · Azure Artifact Signing, Private Trust | C · Self-managed certificate | D · Purchased OV certificate |
| --- | --- | --- | --- | --- |
| Trust on the ten workstations | Publicly trusted chain (Microsoft Identity Verification Root CA 2020); nothing to deploy | Root is **not** publicly trusted; deploy the chain to each workstation (script or Intune/GPO) | Deploy your root/leaf to each workstation (script or GPO); keep the subject stable | Publicly trusted CA chain; nothing to deploy |
| Eligibility and lead time | Organisation in UK/EU/US/CA with ≥3 years verifiable tax history; identity validation takes days | Minimal validation tied to the Azure tenant; fastest Azure route | Immediate | CA vetting (days to weeks); hardware token or Key Vault-stored key |
| Cost | ~$10/month (Basic) | ~$10/month (Basic) | Nil (plus staff time) | ~$150–500/year |
| ⚠ Azure writes | Artifact Signing account + identity validation + certificate profile; Entra app registration or federated credential for CI; `Trusted Signing Certificate Profile Signer` role | Same as A | **None** | Key Vault certificate import (existing vault) + RBAC for the signing identity; optional app registration for CI |
| CI integration | `azure/trusted-signing-action` (GitHub Actions) or Artifact Signing Client Tools + `signtool /dlib … /dmdf metadata.json` on the terminal | Same as A | `winapp package --cert` / `winapp sign` / `signtool sign /f` with the PFX from a protected secret or the terminal | `AzureSignTool` (dotnet tool) against Key Vault from CI or terminal |
| Key protection | Managed by Azure; daily-issued short-lived certificates (≈3-day validity) | Same | You protect the PFX (hardware token, or encrypted store on the release terminal); highest operational risk | Key never leaves Key Vault (or HSM token) |
| Revocation | Time-precise via the service | Same | Manual: distribute a new root and remove the old | CA revocation |
| Renewal burden | Automatic; identity validation renewal when Azure requires; rotate CI credential | Same | Re-issue, re-trust on every workstation before use, keep overlap | Annual renewal and re-import |
| SmartScreen | Reputation builds over time (hash-based) | Internal only; SmartScreen prompts possible on first download | Prompts possible; irrelevant inside a trusted estate | Reputation builds over time |
| What breaks when it lapses | New signatures impossible until renewed; installed packages with timestamped signatures keep working | Same | Same, plus new machines fail until trust is restored | Same |
| Runbook deltas | R5 = rotate CI credential; sign step uses dlib/action | R5 also re-pushes the root when the chain changes | R5 = full re-trust rollout; R7 adds the trust step | R5 = CA renewal + Key Vault import |
| Proposal fit | §17.1 "code-signing certificate protection and renewal runbook", §9.1 signed package — strongest | Strong, but adds the trust rollout of C without C's zero-Azure property | Meets §9.1 with no Azure change; weakest renewal story | Meets §9.1; minor Azure write |

Recommendation (status stays **Open** until the operator decides): **A**
if the organisation passes the Public Trust identity validation — it is
Microsoft's recommended route for non-Store distribution, has no per-machine
trust step, and its cost is negligible; the ⚠ Azure writes are small and
additive. If A is not eligible, **D** (OV certificate in the existing Key
Vault, AzureSignTool) is the next best; **C** only if the operator wants
zero Azure change and accepts the trust-rollout and renewal burden; **B**
sits between B-for-speed and C-for-trust-burden and is rarely the best fit
for ten workstations.

Spikes that settle the choice: DSK-09-07 (A/B eligibility dry run, no
resource created), DSK-09-08 (C trust rollout on two test machines),
DSK-09-09 (D procurement and Key Vault signing dry run).

Read-only checks an agent may run now (no approval needed):

- Azure MCP `role` / `subscription_list` / `group_resource_list` to confirm
  the subscription and that no Artifact Signing account exists yet.
- Azure MCP `keyvault` to list certificates in `pegasusprodkv252ow37g`
  (expect none).
- `winapp cert generate --if-exists skip` locally to exercise the dev
  route; `signtool verify` on a dev-signed package.

Approval text template for the ⚠ writes (to be filled per option):

> Request: create `<resource type>` named `<name>` in `rg-pegasus-prod`
> (`e6076573-23a5-46a8-acef-7e22d264e5db`, tenant
> `858cf5b3-aa0a-47a6-9b40-4851fd0afa94`) for desktop package signing;
> role assignment `<role>` to `<identity>`; Bicep change in
> `infra/modules/platform.bicep` section `<name>`; rollback = delete the
> resource/role; no other resource is touched.

## D-003 · Update-feed hosting

Repository facts: two storage accounts exist (`pegtrans<suffix>` for
transport, `pegcustody<suffix>` for custody; Standard_LRS; shared-key access
disabled; `infra/modules/platform.bicep:100` and `:154`); no CDN/Front Door,
no Static Web App; the runbook forbids Azure writes without exact-target
approval; App Installer needs unauthenticated HTTPS or UNC with byte ranges,
correct MIME types and `Content-Length`.

| Criterion | A · New container in an existing storage account | B · New dedicated storage account | C · Non-Azure host (UNC share or own HTTPS host) | D · Azure Static Web Apps / Front Door |
| --- | --- | --- | --- | --- |
| Requirements compliance | Blob supports ranges and `Content-Length`; set `Content-Type` per blob on upload; anonymous read at container level | Same as A | UNC: App Installer supports `\\server\share\Pegasus.appinstaller`; own HTTPS host: you configure MIME/ranges | Needs custom MIME configuration; CDN caching complicates `.appinstaller` freshness |
| Authentication | None (anonymous read on that container only); shared keys stay disabled; uploads via managed/CI identity | Same | Share ACLs or none; you own TLS | None |
| ⚠ Azure writes | Container creation; `allowBlobPublicAccess`/container public access setting on an account that currently serves custody or transport data; RBAC `Storage Blob Data Contributor` scoped to the container for the publisher identity; Bicep change | New account + container + public access + RBAC + Bicep change; isolates public-read blobs from custody/transport data | **None** | New resources explicitly against §19.1 ("do not add … a new public web front end"/CDN) |
| Availability and cost | Azure SLA; pennies per month | Same; a few pennies more | Your server/VPN availability; cost already sunk or new | Higher cost and complexity |
| Blast radius | Public-read container inside an account holding private data (mitigated by container-scoped access, but a configuration error has consequences) | Cleanest: nothing else in the account | Depends on the host; no Azure exposure | n/a |
| Runbook | R9 with Azure Blob semantics (`az storage blob upload --content-type`, identity auth) | Same | R9 with file copy / web server config | Not recommended |
| Rollback | Delete container/blobs; remove RBAC | Delete account | Remove files | n/a |
| Proposal fit | §19 "Storage used for MSIX/update feed: retain or repurpose" — closest to "repurpose" | §19 allows it if justified; new resource | §4 test: no cloud requirement for a static feed if an in-house host exists | Fails §19.1 |

Recommendation (status **Open**): **B** (new, dedicated storage account
with one public-read container per channel) if any Azure write is
acceptable — it keeps public-read content out of the custody/transport
accounts and is a single small Bicep module; **A** if the operator prefers
zero new resources and accepts a public-read container alongside private
data (container-scoped access, no shared keys); **C** if the operator
wants no Azure change at all and already runs a reachable server or share
(VPN-only access is fine for ten users). **D** is not recommended.

Spike that settles the choice: DSK-09-10 (read-only checks below, then a
local feed on the Test/UAT stack to prove the client side; Azure writes only
after approval).

Read-only checks an agent may run now:

- Azure MCP `storage` (list accounts, containers, and properties) and
  `group_resource_list` for `rg-pegasus-prod`.
- `az storage account show -n <account> -g rg-pegasus-prod --query
  "{publicAccess:allowBlobPublicAccess, sharedKey:allowSharedKeyAccess,
  sku:sku.name}"` for both accounts.
- `az role assignment list --scope <account-id>` to see existing
  data-plane grants (expect the worker/web identities only).
- Pricing: Azure MCP `pricing` for Standard_LRS blob storage in uksouth.
- Client side (no Azure): host the `.appinstaller` and `.msix` from a local
  static server with the MIME map and run R9's `curl -I` / ranged `GET`
  checks; install on a Test/UAT machine.

Approval text template for the ⚠ writes:

> Request: `<create container `desktop-releases` with anonymous blob read in
> storage account `<name>` | create storage account `<name>` (Standard_LRS,
> uksouth, shared-key disabled, public blob access enabled on container
> `desktop-releases` only)>` in `rg-pegasus-prod`; assign `Storage Blob Data
> Contributor` on that container to `<publisher identity>`; Bicep change in
> `infra/modules/platform.bicep` (storage section) applied through the
> existing `azd provision` route; rollback = remove the container/account and
> the role assignment; no other resource or setting changes.

## How the decisions interact

- A self-managed certificate (D-002 C) with a non-Azure host (D-003 C) is
  the only combination with **no Azure write at all**; it costs the most
  operational care (trust rollout, renewal, your own host).
- Artifact Signing (D-002 A) with a dedicated storage account (D-003 B) is
  the combination with the least operational care and the smallest, most
  additive set of Azure writes.
- Whatever the choice, the gateway minimum-version gate, the `.appinstaller`
  template, the validator, and runbooks R1–R10 do not change.
