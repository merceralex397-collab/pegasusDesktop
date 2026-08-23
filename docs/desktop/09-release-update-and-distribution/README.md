# 09 · Release, update and distribution

Area plan for building, signing, publishing, installing and updating the
Pegasus desktop package, for coordinating desktop and gateway releases, and
for the first install on each workstation. Companion files:
[runbooks](runbooks.md), the
[`.appinstaller` template](appinstaller-template.md), and the
[signing and hosting decision matrix](signing-and-hosting-decision-matrix.md).

## 1. Purpose and proposal coverage

The desktop conversion makes the update path critical infrastructure: the
proposal accepts mandatory updates and places more logic in the client on the
strength of them (§9). This area delivers:

- the two-layer enforcement of §9.1 on the package side (App Installer) —
  the gateway side is [area 04](../04-auth-session-update-and-startup/README.md);
- the startup/update sequence of §9.2 as seen from the package;
- the operational controls of §9.3 (known-good package, backward-compatible
  gateway first, expand/contract, pilot, interrupted-update tests, emergency
  path, short-lived compatibility cache);
- build properties, CI stages and environments of §21 (§21.1–21.3), with the
  Test/UAT stack defined in [area 08](../08-testing/test-uat-stack.md);
- the release aspects of §24 Phase 2 (pilot feed), Phase 9 (pilot ring,
  parallel run, update and rollback exercised) and Phase 10 (mandatory
  production release);
- the operations documents of §26 (build and signing runbook, pilot and
  production release runbook, mandatory-update runbook, rollback runbook,
  code-signing renewal, cloud resource register pointer);
- §29 item 7, the foundation spike that must include a signed development
  MSIX and the mandatory-update flow;
- initial startup and first install on a workstation, which the operator
  asked to be covered explicitly.

Out of scope here: the gateway's own deployment procedure, which stays the
existing `pegasus-release` skill (referenced, not rewritten), and the
Microsoft Store (private in-house software, §2.1).

## 2. Evidence base

### Facts

Repository (verified by reading, 2026-08-23):

- Release procedure today: `.agents/skills/pegasus-release/SKILL.md`
  (byte-identical copy at `.codex/skills/pegasus-release/SKILL.md`), eleven
  steps: preflight → exact-SHA atomic fast-forward of `dev` to `main`
  requiring the literal words `MERGE AUTH GRANTED` → `scripts/Build-ReleaseArtifacts.ps1`
  → `oras cp` of the OCI image to ACR (no Docker on the workstation) → set
  azd inputs → `azd provision --no-prompt` → Worker via `config-zip` only →
  `efbundle` migrations → `scripts/Invoke-ProductionSmoke.ps1` → behavioural
  verification → refresh `docs/current-architecture.md` and
  `docs/operations.md` in the same task. Its traps table (lines 252–266)
  ends with the four DELIV-016 traps (App Insights quota, grant-carrying
  migrations, migration census, runtime-role grants).
- `scripts/Build-ReleaseArtifacts.ps1` (130 lines): clean-HEAD, locked
  restore, `linux-x64` publish of Web and Worker, OCI container archive,
  `dotnet ef migrations bundle --self-contained -r win-x64` (line 70),
  zips, `release-manifest.json` (schemaVersion 2 with `migrationIdentity`,
  `webImage.digest`, per-artifact SHA-256).
- Release identity: sequential release number + date + 40-char source SHA
  + image digest + Container App revision + migration ids; newest row is
  release 20 on 2026-08-22 (`docs/operations.md:311-332`); the narrative
  line `docs/operations.md:295` still says "release 14" — the table is
  authoritative. Product version is fixed at `0.1.0-alpha.1`
  (`Directory.Build.props:9`, `azure.yaml:3`, `package.json:3`).
- CI: one workflow, `.github/workflows/ci.yml` (`repository-check`),
  jobs `changes`, `documentation`, `local-development-scripts`,
  `reference-data`, `infrastructure`, `unit`, `sql-integration` ×3,
  `sql-integration-coverage`, `browser`; seven of nine on `windows-latest`;
  composite action `.github/actions/dotnet-build/action.yml` pins .NET SDK
  `10.0.x` with locked restore. No publish, sign or deploy lane. GitHub
  Actions Azure deployment is recorded as `Not planned`
  (`docs/runbook.md:903`).
- Nothing in the repository mentions MSIX, App Installer, `signtool`,
  Azure Trusted/Artifact Signing, or a code-signing certificate; Key Vault
  (`pegasusprodkv252ow37g`, `infra/modules/platform.bicep:85`) holds
  secrets only — no certificate resource.
- Release operations are Windows-only by construction (migration bundle is
  `win-x64`, `docs/runbook.md:19-75`); Web/Worker packages are `linux-x64`.
- Vendored `winui-packaging` skill (`.codex/skills/winui-packaging/SKILL.md`,
  from `microsoft/win-dev-skills` v0.5.0, commit `f1028dd5`): `winapp cert
  generate --manifest .`, `winapp cert install ./devcert.pfx` (admin),
  `winapp package <dir> --cert ./devcert.pfx` (preferred over separate
  `winapp sign`), `--self-contained` bundles the Windows App SDK runtime,
  `--timestamp` is critical for production, Publisher must match
  `Identity.Publisher`; CI sample uses `microsoft/setup-WinAppCli@v0.1` on
  `windows-latest` with `--if-exists skip --quiet`. Its
  `references/sourcegen-patterns.md` covers AOT/trimming readiness (not
  used initially, §7.1).
- Design authority and repository rules that bind this area:
  `docs/runbook.md#live-operation-approval-matrix` (read-only Azure free,
  every write needs exact-target approval), ADR-0014 (two environments),
  ADR-0007 (authorised-terminal deployment), `AGENTS.md § Safety rails`
  (refresh current-state docs in the same task after any release).

Official documentation (all fetched 2026-08-23):

- App Installer update settings — `OnLaunch` (`HoursBetweenUpdateChecks`
  0–255, default 24), `ShowPrompt`, `UpdateBlocksActivation` (requires
  `ShowPrompt="true"`), `AutomaticBackgroundTask` (every 8 hours,
  no UI), `ForceUpdateFromAnyVersion` (allows downgrade):
  <https://learn.microsoft.com/windows/msix/app-installer/update-settings>.
- The `.appinstaller` 2021 schema is required for `ShowPrompt`,
  `UpdateBlocksActivation` and `HoursBetweenUpdateChecks`; Visual Studio
  emits the 2017/2 schema by default and silently ignores those settings;
  the `ms-appinstaller:` protocol has been disabled by default since
  December 2023 (link directly to the `.appinstaller` file; enterprise
  re-enable only via Group Policy `EnableMSAppInstallerProtocol`):
  <https://learn.microsoft.com/windows/apps/package-and-deploy/distribution-feature-status>.
- Auto-update and repair, `UpdateUris` fallbacks (maximum 10), settings
  precedence (CSP > PowerShell/App Installer file > embedded file),
  `Get-/Set-AppxPackageAutoUpdateSettings`:
  <https://learn.microsoft.com/windows/msix/app-installer/auto-update-and-repair--overview>.
- Manual `.appinstaller` authoring, including the `Uri` attribute and
  `Version` rules:
  <https://learn.microsoft.com/windows/msix/app-installer/how-to-create-appinstaller-file>.
- Web delivery requirements (apply to an **HTTP** host only; the decided
  UNC share carries none of them, because SMB is not HTTP) — MIME types
  `application/msix`,
  `application/msixbundle`, `application/appinstaller`; every response
  must carry `Content-Length`; byte-range support (HTTP/1.1):
  <https://learn.microsoft.com/windows/msix/msix-troubleshooting-guide> and
  <https://learn.microsoft.com/windows/msix/app-installer/installing-windows10-apps-web>.
- In-app update APIs — `Package.CheckUpdateAvailabilityAsync` (call on the
  package returned by `PackageManager.FindPackageForUser`, not on
  `Package.Current`, which fails with access denied; `Required` vs
  `Available`), `PackageManager.RequestAddPackageByAppInstallerFileAsync`:
  <https://learn.microsoft.com/uwp/api/windows.applicationmodel.package.checkupdateavailabilityasync>,
  <https://learn.microsoft.com/windows/msix/non-store-developer-updates>.
- Signing options — self-signed for dev, Azure Artifact Signing (formerly
  Trusted Signing, Basic ~$10/month, organisations in USA/Canada/EU/UK,
  Public Trust identity validation needs three or more years of verifiable
  tax history, daily short-lived certificates, `azure/trusted-signing-action`,
  SignTool needs the Artifact Signing Client Tools dlib and `metadata.json`),
  OV certificate (~$300–500/year):
  <https://learn.microsoft.com/windows/msix/package/signing-package-overview>
  and <https://learn.microsoft.com/windows/msix/package/sign-msix-package-guide>;
  Private Trust certificate profiles for LOB apps:
  <https://learn.microsoft.com/azure/artifact-signing/concept-resources-roles>.
- OV certificate in Key Vault with AzureSignTool in GitHub Actions or
  Azure Pipelines:
  <https://learn.microsoft.com/windows/msix/desktop/cicd-keyvault>.
- SmartScreen reputation is hash-based and accumulates over time regardless
  of certificate type (EV no longer grants instant bypass):
  <https://learn.microsoft.com/windows/apps/package-and-deploy/distribution-feature-status>.
- Windows App SDK 2.x is on Semantic Versioning (package family name
  follows the major version); 2.4.0 released 2026-08-13:
  <https://learn.microsoft.com/windows/apps/windows-app-sdk/release-notes/windows-app-sdk-2-0>.
- WinApp CLI for .NET (`winapp init`, `winapp pack`, version increment
  needed to update an installed package):
  <https://learn.microsoft.com/windows/apps/dev-tools/winapp-cli/guides/dotnet>.

### Assumptions

- Package identity `CollisionEngineers.Pegasus`, one identity for both
  channels; the Publisher string is the subject of the self-managed
  certificate (D-002) and is fixed before the first package is built.
- The feed is reachable from every workstation over HTTPS (or UNC) without
  authentication; confidentiality comes from signed packages, unguessable
  paths, and the gateway's minimum-version gate, not from the feed.
- A CI run number is a monotonic, collision-free source for the package
  `Build` component; if the team moves CI providers the scheme is re-based.
- The ten workstations are Windows 11 x64 with the WebView2 runtime present
  (needed by [area 07](../07-integrations/README.md) for report rendering).
- Release operations for the desktop can run from the same authorised
  Windows release terminal that runs `pegasus-release`.

## 3. Decisions and assumptions

Locked and open decisions this area depends on are listed in
[the index](../README.md#locked-decisions-and-open-decisions). Decisions
made here:

- **Two-layer enforcement (§9.1).** The package layer uses the App Installer
  2021 schema with `OnLaunch HoursBetweenUpdateChecks="0" ShowPrompt="true"
  UpdateBlocksActivation="true"` plus `AutomaticBackgroundTask`; the
  application layer is the gateway compatibility gate of area 04. The
  package check fails open when the feed is unreachable; the gateway gate
  fails closed after a short cached window. Both are required.
- **Versioning.** Package version `1.<minor>.<build>.0`: `minor` is bumped
  by the release owner when a release changes the gateway compatibility
  range, `build` is the CI run number, revision is always `0`. The product
  `Version` property in `Directory.Build.props` (`0.1.0-alpha.1`) stays the
  gateway's identity; the desktop version lives in `Package.appxmanifest`
  and the desktop release manifest. Deviation: the proposal does not fix a
  scheme; this one is chosen for monotonicity and rollback simplicity.
- **Channels.** One package identity, two feeds: `pilot/Pegasus.appinstaller`
  and `prod/Pegasus.appinstaller`. A workstation belongs to the ring whose
  `.appinstaller` it was installed from; moving rings is a reinstall from
  the other feed. Assumption/Deviation: a separate pilot identity
  (side-by-side install) was considered and rejected for now because it
  doubles package configuration and the pilot users are the same people who
  will run production; revisit if pilot and production must coexist on one
  machine.
- **Desktop release manifest.** `desktop-release-manifest.json` next to the
  package: version, source commit, package SHA-256, `.appinstaller`
  version, channel, signer identity/thumbprint, minimum gateway release,
  maximum tested gateway release, Windows App SDK version, build run.
  Recorded in `docs/operations.md` in a new desktop release table and in the
  `desktop/v<M.m.b>` tag.
- **Order of deployment.** Gateway first, always backward compatible
  (expand/contract for API and database); desktop second; minimum client
  version raised last, only after the pilot ring has run the new package.
- **Known-good previous package** is retained on the feed for every channel
  and rollback republishes it with a higher `.appinstaller` `Version` and
  `ForceUpdateFromAnyVersion="true"`.
- **Pilot ring** is one or two internal users on the production gateway
  (L-02: no Azure test environment; the local Test/UAT stack rehearses
  install/update/rollback before the pilot).
- **Signing** happens only in a protected CI job (tag-triggered) or on the
  authorised release terminal; never on a PR build. Timestamping is
  mandatory. The signing route is **decided (D-002, 2026-08-23): a
  self-managed certificate**, self-signed, held in-house, trusted per
  workstation in `LocalMachine\TrustedPeople` (never `Trusted Root`), with
  the subject fixed to the manifest `Publisher` and a ~3-year validity. The
  `.pfx` never leaves the signing host and is not a GitHub secret. Trust
  always reaches a machine **before** a package signed with that certificate
  does. The [matrix](signing-and-hosting-decision-matrix.md) records the
  chosen shape and the rejected options.
- **Feed hosting** is **decided (D-003, 2026-08-23): a UNC file share** on
  an always-on in-house Windows host, one folder per channel, served to App
  Installer over SMB. It follows from constraint C-01 (the repositories
  become private on completion, so no GitHub-hosted anonymous feed can
  survive) and it needs **no Azure write and no recurring cost**. MIME,
  `Content-Length` and byte-range requirements do not apply over SMB;
  share ACLs and a permanently stable UNC path replace them. Accepted
  trade-off: update checks work on the office network or VPN only — the
  launch check fails open there, and the gateway minimum-version gate still
  fails closed, so an obsolete client cannot work.
- **Publication.** The pilot feed publish may be automated from CI once
  D-002 is decided and approved (the feed itself is settled: D-003, the UNC
  share); production feed publish stays a
  runbook-controlled terminal step with explicit operator approval
  (phrase proposed in [runbooks](runbooks.md)). This mirrors the existing
  `MERGE AUTH GRANTED` culture without extending that literal phrase's
  meaning.
- **Emergency path.** A defective client is blocked by raising the minimum
  client version (admin setting, area 04) and, where needed, republishing
  the previous package; there is no secret bypass.
- **SBOM and vulnerability report** are produced per release
  (`dotnet list package --vulnerable --include-transitive`, plus an SBOM
  generator chosen in DSK-09-16).
- **Self-contained** .NET and Windows App SDK in the MSIX (§7.1), no
  `Dependencies` element; package size is measured, ReadyToRun only after
  measurement.
- ⚠ Azure writes in this area are all conditional: feed container or
  account — **withdrawn, D-003 chose the UNC share**; signing
  account/identity/RBAC or Key Vault certificate — **withdrawn, D-002 chose a
  self-managed certificate**. **This area now requires no Azure write at
  all.** The withdrawals are mirrored
  in [area 11](../11-azure-disposition/README.md).

## 4. Target state and exit gate

Target state:

- `eng/packaging/` holds the `.appinstaller` templates, validator, and
  `Build-DesktopRelease.ps1`; `ci.yml` has desktop build, package, test and
  (tag-only) sign/publish lanes; the feed serves `pilot/` and `prod/`
  channels with the current and previous package for each.
- A workstation can be onboarded by opening the channel's `.appinstaller`
  URL, and an obsolete client is blocked by App Installer on launch and by
  the gateway thereafter.
- Release evidence per desktop release: manifest, hashes, signer, compat
  range, pilot result, `docs/operations.md` row, tag.

Exit gate (release side of §24 Phases 2, 9 and 10 and §27 items 4, 13):

- Obsolete package is blocked and updates (Phase 2).
- Update and rollback exercised on the pilot ring; support runbook proven
  (Phase 9).
- Mandatory production release shipped; no user requires the legacy web UI
  (Phase 10).
- Package install, mandatory update and rollback proven (§27 item 13);
  unsupported versions cannot proceed (§27 item 4).

## 5. Work breakdown

| ID | Title | Profile | Depends on | Acceptance | Verification | Tier | Routing |
| --- | --- | --- | --- | --- | --- | --- | --- |
| DSK-09-01 | ADR-0105 MSIX/App Installer distribution and minimum-version gate | chore | DSK-00-03 | ADR accepted; two-layer enforcement, fail-open/fail-closed split, channel model and versioning recorded | ADR in `docs/adr/`, index updated | 1 | pegasus-release-packager · kanmer-docs · Kanmer |
| DSK-09-02 | Versioning scheme and `desktop-release-manifest.json` | chore | DSK-02-03 | Version flows from CI run into `Package.appxmanifest` at build; manifest produced by the build with every field listed in §3 | Build output contains manifest; unit test on the generator script | 1 | pegasus-release-packager · directory-build-organization · Microsoft Learn |
| DSK-09-03 | `.appinstaller` templates (pilot/prod) and validator `eng/packaging/Test-AppInstaller.ps1` | feature | DSK-09-02 | 2021 schema; `Uri` equals hosted URL; version monotonic; package hash matches manifest; validator fails on each violation | Validator regression tests with fixture files | 1 | pegasus-release-packager · winui-packaging · Microsoft Learn |
| DSK-09-04 | `scripts/Build-DesktopRelease.ps1` (terminal route) | feature | DSK-09-02, DSK-09-03 | Clean-HEAD, locked restore, x64 Release build, `winapp package`, manifest, SBOM, hashes; no signing unless `-Sign` with a route parameter | Run on release terminal; artifacts hashed; re-run reproduces hashes for deterministic parts | 1 | pegasus-release-packager · winui-packaging, pegasus-release · — |
| DSK-09-05 | CI desktop lanes: build, dev-cert package, packaging tests, artifact upload | feature | DSK-02-12, DSK-08-10 | `windows-latest` job green on PRs; unsigned-for-prod artifact attached; Linux Web/Worker jobs unaffected | `ci.yml` run; artifact present; existing jobs green | 1 | pegasus-release-packager · authoring-github-workflows, winui-packaging · Microsoft Learn |
| DSK-09-06 | Development certificate pipeline (`winapp cert generate`, trust on Test/UAT machines) | chore | DSK-02-10 | Dev-signed MSIX installs on a clean Windows 11 test machine after `winapp cert install` | Install/uninstall smoke on the Test/UAT stack | 7 | pegasus-release-packager · winui-packaging · — |
| ~~DSK-09-07~~ | ~~D-002 spike A: Azure Artifact Signing eligibility and dry run~~ — **withdrawn 2026-08-23**, D-002 chose the self-managed certificate | — | — | — | — | — | — |
| DSK-09-08 | Issue the production self-managed certificate (subject = manifest `Publisher`, ~3 years, key on the signing host with a restricted ACL), export the `.cer`, prove the trust rollout on two machines, then roll it to the estate; record whether the mechanism is a scripted `Import-Certificate` or Group Policy Trusted People | feature | DSK-09-06 | Cert issued from a controlled key; trust installed on two test machines by script; renewal and revocation rehearsal written up | Test/UAT stack install from a self-managed-signed package | 7 | pegasus-release-packager · winui-packaging · Microsoft Learn |
| ~~DSK-09-09~~ | ~~D-002 spike C: OV certificate procurement and Key Vault signing dry run~~ — **withdrawn 2026-08-23** | — | — | — | — | — | — |
| DSK-09-10 | Stand up the decided UNC feed (D-003): stable path (DFS/CNAME), per-channel folders, ACLs, backup, publisher account, and the `UpdateUris` fallback check | feature | DSK-09-03 | Read-only checks done (`allowBlobPublicAccess`, RBAC, costs); MIME/Range/Content-Length verification procedure written; ⚠ writes enumerated with approval text | Spike result; local feed on the Test/UAT stack proves the client side | 1 | pegasus-azure-auditor, pegasus-release-packager · azure-storage, azure-resource-lookup · Azure MCP (read), Microsoft Learn |
| DSK-09-11 | Pilot-ring release runbook R1 and first pilot release | feature | DSK-09-04, DSK-09-05, D-002, D-003 | R1 executed once end to end with evidence; pilot users updated from the pilot feed | Evidence in ticket proof; `docs/operations.md` desktop release row | 12 | pegasus-release-packager · pegasus-release, winui-packaging · Kanmer |
| DSK-09-12 | Mandatory-update runbook R3 and test | feature | DSK-04-06, DSK-09-11 | Raising the minimum version blocks an old client with the update-required screen; new client proceeds | Test/UAT stack + pilot evidence | 7 | pegasus-release-packager, pegasus-test-engineer · winui-ui-testing · — |
| DSK-09-13 | Rollback runbook R4 and test | feature | DSK-09-11 | Previous package republished with higher `.appinstaller` version; client downgrades; minimum version lowered | Test/UAT stack rehearsal, pilot rehearsal | 7 | pegasus-release-packager · winui-packaging · — |
| DSK-09-14 | Certificate/identity renewal runbook R5 and emergency block runbook R6 | chore | D-002, DSK-04-06 | Both runbooks written for the chosen route; R6 rehearsed on the Test/UAT stack | Dry run recorded | 1 | pegasus-release-packager · pegasus-release · Kanmer |
| DSK-09-15 | First-install onboarding guide R7 (operator one-pager) | chore | DSK-09-11 | One page, operator vocabulary only (design authority: no how-it-works copy beyond necessary steps) | Review by `pegasus-desktop-reviewer`; used by pilot users | 1 | pegasus-release-packager · — · Kanmer |
| DSK-09-16 | SBOM and vulnerability report per release | chore | DSK-09-04 | SBOM generated; `dotnet list package --vulnerable --include-transitive` clean or triaged | CI artifact; release record | 9 | pegasus-release-packager · authoring-github-workflows · Microsoft Learn |
| DSK-09-17 | Tag-triggered sign and publish lane (pilot feed), production publish stays terminal | feature | DSK-09-05, D-002, D-003 | `desktop/v*` tag on `main` signs and publishes to pilot after approval; production publish by runbook R2 only | Dry run with a dev certificate to a local feed; then real | 9 | pegasus-release-packager · authoring-github-workflows, winui-packaging · Azure MCP (read) |
| DSK-09-18 | Desktop release table and compatibility range in `docs/operations.md` | chore | DSK-09-11 | Table exists with the first row; compat range joins gateway release number | Documentation link test; review | 1 | pegasus-release-packager · pegasus-release · Kanmer |

## 6. Routing table

| Kind | Name | Use in this area | Source |
| --- | --- | --- | --- |
| Subagent | `pegasus-release-packager` | Owns every ticket above except the Azure read-only checks | `.codex/agents/pegasus-release-packager.toml` |
| Subagent | `pegasus-azure-auditor` | Read-only storage/identity/cost checks for D-002/D-003 | `.codex/agents/pegasus-azure-auditor.toml` |
| Subagent | `pegasus-test-engineer` | Packaging and update tests (with [area 08](../08-testing/README.md)) | `.codex/agents/pegasus-test-engineer.toml` |
| Subagent | `pegasus-desktop-reviewer` | Independent review of runbooks, CI lanes, first-install guide | `.codex/agents/pegasus-desktop-reviewer.toml` |
| Skill | `winui-packaging` | MSIX packaging, `winapp cert/package/sign`, CI sample, self-contained | `microsoft/win-dev-skills` v0.5.0 (`f1028dd5`), vendored `.codex/skills/winui-packaging/` |
| Skill | `pegasus-release` | Gateway release steps, traps, `MERGE AUTH GRANTED` rule | `.agents/skills/pegasus-release/SKILL.md` |
| Skill | `authoring-github-workflows` | CI lane authoring | `dotnet/skills` `98f84851`, `.agents/skills/authoring-github-workflows/` |
| Skill | `directory-build-organization` | Version/sign properties in `Directory.Build.props` | `dotnet/skills` `98f84851`, plugin `dotnet-msbuild` |
| Skill | `binlog-failure-analysis` | Diagnosing MSIX/XAML build failures in CI | `dotnet/skills` `98f84851`, plugin `dotnet-msbuild` |
| Skill | `azure-storage`, `azure-resource-lookup` | Blob semantics and read-only inventory for D-003 | `microsoft/azure-skills` `1a03acfb` |
| MCP | Microsoft Learn (`microsoft_docs_search`, `microsoft_docs_fetch`) | App Installer schema, Artifact Signing, WinApp CLI, Package APIs | `.codex/config.toml` |
| MCP | Azure MCP (read-only: `storage`, `group_resource_list`, `role`, `pricing`) | D-002/D-003 facts without writes | azure-skills MCP |
| MCP | Kanmer | Tickets, proof, runbook evidence | `.codex/config.toml` |

## 7. Risks and traps

- **2017/2 schema silently ignores `ShowPrompt`/`UpdateBlocksActivation`**:
  the validator rejects any namespace other than 2021.
- **`ms-appinstaller:` protocol is disabled by default**: never publish a
  protocol link; publish the `.appinstaller` file URL.
- **Fail-open package check**: when the feed is unreachable App Installer
  launches the app; only the gateway gate (area 04) closes that door.
- **Certificate expiry without timestamping** invalidates every installed
  package's signature path for new installs; timestamp every signature.
- **Publisher mismatch** between certificate and `Identity.Publisher`
  breaks packaging; the validator checks the manifest against the signer.
- **Feed MIME/Content-Length/Range**: App Installer errors are generic
  (`0x80072F76`); runbook R9 verifies headers with `curl -I` and a ranged
  `GET` before announcing a release.
- **SmartScreen** will warn on first download of a new hash for a while;
  in-house users are told in R7; it is not a signing failure.
- **CI runner has no production certificate**: signing only in the
  protected tag job or the release terminal; PR builds use the dev cert.
- **Linux publish** of Web/Worker must stay green: desktop projects are
  excluded from the Linux solution filter ([area 02](../02-architecture-and-foundation/README.md)).
- **Release docs drift**: `docs/operations.md:295` contradicts its own
  table; `CHANGELOG.md` stopped at 2026-08-03 — do not treat either as
  current; the desktop release table must not repeat the pattern
  (refresh in the same task, per `AGENTS.md § Safety rails`).
- **App Insights 0.1 GB/day cap** (PLAT-034) can hide update and
  blocked-client telemetry for most of the day; rely on the desktop
  diagnostics bundle and feed-side evidence, not only on telemetry.
- **UpdateBlocksActivation needs ShowPrompt**; `HoursBetweenUpdateChecks="0"`
  checks every launch — acceptable for ten users; watch the launch-time
  budget (§15.1) on a slow network.
- **`Package.Current.CheckUpdateAvailabilityAsync` fails** with access
  denied; use `PackageManager.FindPackageForUser` (area 04 owns the code).
- **Group Policy/CSP** can override App Installer settings on managed
  devices; R7 records the expected policy state.

## 8. Documentation changes

- New ADR-0105 (this area) under `docs/adr/`, index row in
  `docs/adr/README.md`.
- `docs/operations.md`: new "Desktop releases" table (version, date,
  commit, package hash, signer, channel, compat range, pilot/production),
  refreshed in the same task as each release; first row at DSK-09-11.
- `docs/runbook.md`: links to [runbooks](runbooks.md) R1–R10 once they are
  proven (runbook content may move into `docs/runbook.md` at cutover; the
  drafts here are planning material).
- `docs/current-architecture.md`: deployment boundary paragraph gains the
  desktop package and feed once the first pilot release ships.
- `docs/capabilities.md`: `DSK` rows for packaging, update, rollback, first
  install.
- [Area 11](../11-azure-disposition/README.md) mirrors every ⚠ write
  listed here.
