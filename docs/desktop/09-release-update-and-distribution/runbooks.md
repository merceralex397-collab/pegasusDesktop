# 09 · Desktop release runbooks (drafts)

Draft runbooks for the desktop package. Each one states approvals, numbered
steps, evidence to capture, rollback, and what it does not prove. They
become authoritative only when proven on the Test/UAT stack
([area 08](../08-testing/test-uat-stack.md)) and then on the pilot ring, at
which point `docs/runbook.md` links to or absorbs them. Gateway releases keep
the existing `pegasus-release` skill; nothing here changes its steps.

Conventions used below:

- `<channel>` is `pilot` or `prod`; `<feed>` is the channel's base URL or UNC
  path decided by D-003; `<ver>` is the package version `1.<minor>.<build>.0`.
- "Approval" means the operator's explicit, written approval for the exact
  target, per
  [the live-operation approval matrix](../../runbook.md#live-operation-approval-matrix).
  The existing literal phrase `MERGE AUTH GRANTED` keeps its single meaning
  (the `dev` → `main` promotion). For publishing a signed package to the
  **production** feed this plan proposes the literal phrase
  `FEED PUBLISH GRANTED prod <ver>`; for pilot, `FEED PUBLISH GRANTED pilot
  <ver>` until pilot publication is automated. The implementing agent must
  confirm the phrase with the operator before first use.
- Evidence goes into the Kanmer ticket's proof (types `command-log`,
  `test-output`, `visual`) and the release row in `docs/operations.md`.

## R1 · Desktop pilot release

Purpose: ship a new package to the pilot ring (one or two internal users)
against the production gateway.

Preconditions:

1. The gateway release that the package needs is live and recorded in
   `docs/operations.md` (R8 ran or confirmed no gateway change).
2. `main` carries the commit; the `desktop/v<ver>` tag exists on it.
3. CI is green for that commit, including the desktop lanes and packaging
   tests.
4. D-002 and D-003 are decided; the signing route and `<feed>` are
   configured.
5. Test/UAT stack rehearsal of install → update → rollback passed for this
   package (evidence linked).

Steps:

1. On the authorised release terminal, clean checkout of the tagged commit;
   `git status` clean; record the SHA.
2. Run `scripts/Build-DesktopRelease.ps1 -Channel pilot -Version <ver>`
   (locked restore, x64 Release build, `winapp package`, manifest, SBOM,
   hashes). Record the SHA-256 of the `.msix`.
3. Sign by the D-002 route (Artifact Signing via the client tools/`signtool`
   with `/dlib`, AzureSignTool against Key Vault, or `winapp sign` with the
   self-managed certificate). Always timestamp (`/tr` or `--timestamp`).
   Verify: `signtool verify /pa /v <pkg>.msix` (chain and timestamp).
4. Generate `pilot/Pegasus.appinstaller` from the template with `Version`
   = previous + 1 revision and `MainPackage Version=<ver>`; run
   `eng/packaging/Test-AppInstaller.ps1` (schema, `Uri`, monotonic version,
   hash vs manifest).
5. Obtain `FEED PUBLISH GRANTED pilot <ver>` (until automated).
6. Publish: upload `.msix`, `.appinstaller`, `desktop-release-manifest.json`
   and SBOM to `<feed>/pilot/` with the MIME types in
   [appinstaller-template § hosting requirements](appinstaller-template.md#hosting-requirements);
   keep the previous package in place (R9).
7. Verify the feed from a workstation network position: `curl -I` shows
   `Content-Type` and `Content-Length`; a ranged `GET` returns `206`.
8. On a pilot workstation: launch Pegasus; App Installer prompt appears
   (or `winapp`-driven relaunch in the Test/UAT rehearsal); confirm version
   in Settings/Diagnostics; confirm the gateway accepts the version.
9. Record the release row in `docs/operations.md` (version, date, commit,
   package hash, signer, channel `pilot`, compat range) and the ticket
   proof in the same task.

Evidence: build log, hashes, `signtool verify` output, validator output,
`curl -I` output, screenshot of the version screen, operations row.

Rollback: R4 with the previous pilot package.

Does not prove: production-ring behaviour on every workstation; telemetry
(App Insights quota, PLAT-034); anything about the gateway's own release.

## R2 · Desktop production release

Purpose: publish a pilot-proven package to the production feed and, when
agreed, make it mandatory.

Preconditions:

1. R1 completed for the same `<ver>` and pilot users have run it through
   the normal workflows for the agreed soak period (Phase 9 exit gate items:
   no unexplained data divergence, update and rollback exercised).
2. The gateway compatibility range for `<ver>` is live.
3. Approval `FEED PUBLISH GRANTED prod <ver>` obtained.

Steps:

1. Use the same signed `.msix` and manifest from R1 (no rebuild — same
   hash).
2. Generate `prod/Pegasus.appinstaller` with `Version` incremented relative
   to the current production `.appinstaller`; validate.
3. Publish to `<feed>/prod/` keeping the previous production package; verify
   headers and ranges (R9).
4. Announce to users (one line: "Pegasus will update on next launch").
5. Watch the first day: blocked-client counts and update failures from the
   gateway (area 10), diagnostics bundles from any failing workstation
   (R10).
6. If the release is to become mandatory, run R3 after all production users
   have updated (or after the agreed grace period).
7. Record the production row in `docs/operations.md`; refresh
   `docs/current-architecture.md` if the deployment boundary changed.

Rollback: R4. Does not prove: that every workstation updated (verify via
the gateway's client-version distribution) or that the minimum version was
raised (that is R3).

## R3 · Mandatory-update enforcement

Purpose: make versions below `<ver>` unable to work (§9.1 gateway gate).

Preconditions: R2 done; all pilot and production users observed on `<ver>`
or the grace period elapsed; gateway supports `<ver>`.

Steps:

1. In the gateway administration surface (area 04 admin setting, DB-backed,
   audited), raise the minimum client version to `<ver>` with a reason.
2. From a test machine still on the previous version: launch; expect App
   Installer to update (if the feed is reachable) or, if the feed check is
   bypassed/unreachable, expect the gateway's update-required screen with
   the correlation id; confirm no work is possible.
3. Confirm a current client logs in normally.
4. Record the minimum-version change (who, when, reason) in the release row.

Rollback: lower the minimum version to the previous value (same admin
setting). Does not prove: App Installer behaviour on a machine whose
policy overrides update settings (see R7).

## R4 · Rollback

Purpose: return a channel to the previous known-good package.

Steps:

1. Decide scope (pilot or prod) and approval (`FEED PUBLISH GRANTED
   <channel> <prev-ver>`).
2. If the minimum client version was raised to the defective version, lower
   it first (R3 rollback) so downgraded clients are accepted.
3. Publish the previous signed `.msix` (already on the feed) under a **new**
   `.appinstaller` `Version` (higher than the defective one) with
   `ForceUpdateFromAnyVersion="true"` and `MainPackage Version=<prev-ver>`;
   validate; publish.
4. On a test workstation: launch; App Installer applies the downgrade;
   confirm the version.
5. If App Installer cannot downgrade on a particular machine, run R7's
   uninstall/reinstall steps for that machine.
6. Record the rollback row; open a ticket for the defect with the
   diagnostics bundle.

Does not prove: data written by the defective version is correct — check
audit/history for the window (area 10).

## R5 · Code-signing certificate or identity renewal

Purpose: keep releases signable and installable; steps depend on D-002.

Common:

1. Calendar the expiry/renewal date in `docs/operations.md` with a 60-day
   warning.
2. Sign a test package with the renewed credential; verify chain and
   timestamp; install on a Test/UAT machine.

Per route:

- Artifact Signing: renewal is automatic (daily certificates); renew the
  identity validation when Azure requires; rotate the CI federated
  credential/app registration secret ⚠ (Azure write, exact target).
- Self-managed certificate: issue the new certificate, update the Publisher
  if the subject changes (it must not — keep the subject stable), roll the
  new trust to every workstation **before** signing a release with it,
  keep the old certificate valid until every machine trusts the new one.
- OV certificate: renew with the CA, import into Key Vault ⚠, update the
  AzureSignTool reference, test-sign.

Rollback: keep signing with the still-valid old credential. Does not
prove: existing installations remain valid — they do, provided signatures
were timestamped.

## R6 · Emergency block of a defective client version

Purpose: stop a version with a serious defect from doing work within
minutes, independent of the feed.

Steps:

1. Raise the minimum client version above the defective version (R3 step 1)
   — or, when the fix is not yet built, to the last good version and
   publish the last good package as a rollback (R4) so clients can still
   work.
2. Confirm a defective client is refused (`client-unsupported` problem,
   update-required screen).
3. Communicate to users; collect diagnostics bundles (R10).
4. Record in the release row and the security/action history.

Does not prove: the defect's data impact (audit query needed).

## R7 · First-install onboarding per workstation

Purpose: put Pegasus on a new or reset workstation and reach the login
screen.

Prerequisites checklist:

1. Windows 11 x64 (24H2 recommended), current updates; signed in as the
   user who will use Pegasus (per-user MSIX install, no administrator needed
   once the signing chain is trusted).
2. Microsoft Edge WebView2 runtime present (default on Windows 11; verify in
   Settings → Apps); required for report rendering.
3. Network access to `<feed>` and the gateway.
4. Only if D-002 chose a self-managed certificate or Artifact Signing
   Private Trust: install the trust (root/leaf) by the scripted step from
   R5; verify with `certutil -verifystore`.
5. Managed device policy: `ms-appinstaller:` remains disabled (not needed);
   App Installer auto-update must not be disabled by CSP/PowerShell
   (`Get-AppxPackageAutoUpdateSettings` after install).

Steps:

1. Open the channel's `.appinstaller` URL in a browser (download) and open
   the file; App Installer shows the package and publisher; choose Install.
2. First launch: the app checks for updates, then the gateway compatibility
   gate, then shows Login (area 04 startup sequence). Sign in with the
   existing Pegasus account.
3. Verify: Settings/Diagnostics shows version `<ver>`, channel, gateway
   URL; `Get-AppxPackage CollisionEngineers.Pegasus` lists the version;
   `Get-AppxPackageAutoUpdateSettings` shows on-launch checks.
4. Record the workstation in the support register (user, machine, channel,
   date).

Uninstall/reinstall: Settings → Apps → Pegasus → Uninstall (or
`Remove-AppxPackage`), then repeat steps 1–3; local preferences live in the
package's `ApplicationData` and are removed with the package; the refresh
token is removed on uninstall (credential store cleanup is part of
uninstall behaviour to verify in area 04).

Switching channel: uninstall, then install from the other channel's
`.appinstaller`.

One-page guide skeleton for operators (operator vocabulary only, no
how-it-works copy): Install · Sign in · Pegasus updates itself on launch ·
If you see "Update required", close and reopen · If you see "Cannot reach
Pegasus", check the network and retry · Export diagnostics from Settings
when asked by support.

## R8 · Gateway release coordination

Purpose: keep the gateway ahead of the desktop and the compatibility range
true.

Steps:

1. Before a gateway release (`pegasus-release` skill), check which desktop
   versions are in use (gateway client-version distribution) and that the
   release is backward compatible with the minimum client version; expand
   before contract (API fields, database migrations with runtime-role
   grants — trap from DELIV-016).
2. After the gateway release is smoked, update the desktop release
   manifest's compatibility range for the next desktop build and the
   operations row.
3. Never release a desktop package that requires a gateway change not yet
   live.

## R9 · Feed hosting operations

Purpose: publish files so App Installer accepts them.

Steps:

The feed is the **UNC share decided in D-003** (2026-08-23):
`\\<host>\<share>\<channel>` with `<channel>` = `prod` or `pilot`. SMB carries
Windows authentication, so there is no anonymous endpoint and no MIME,
`Content-Length` or byte-range configuration to get right.

Steps:

1. Copy the new package **first** and the `.appinstaller` **last** — a client
   that reads the manifest mid-publish must never find a package that is not
   there yet:
   `robocopy <staging> \\<host>\<share>\<channel> Pegasus_<ver>_x64.msix /Z /R:2 /W:5`,
   then `desktop-release-manifest.json`, then `Pegasus.appinstaller`.
2. Keep at least the previous package per channel; never overwrite a
   published `.msix` (a new version always means a new file name). Only
   `Pegasus.appinstaller` is replaced in place, and its `Version` attribute
   must increase every time.
3. ACLs: staff group = read and execute; publisher account = modify; nobody
   else writes. The signing certificate never lives on the share.
4. Verify from a workstation that is **not** the publisher, signed in as an
   ordinary staff user: the path resolves (`Test-Path`); the manifest shows
   the expected `Version` and `Uri` (`Select-Xml -XPath /*`); the package
   hash matches `desktop-release-manifest.json` (`Get-FileHash`); the staff
   group has no write permission (`Get-Acl`).
5. Confirm the `Uri` attribute inside the `.appinstaller` is byte-identical
   to the path clients installed from. It is baked into every installed
   client: changing the host name, share name or channel folder breaks
   updates for every existing installation and forces a reinstall.
6. Availability: the share is the single point of failure for updates (a UNC
   fallback in `UpdateUris` is not documented as supported — see the decision
   matrix). If the host is down, clients keep running and simply do not
   update; the gateway minimum-version gate remains the safety line. Restore
   from the host's backup or republish from the CI artifacts.
7. Off-network clients do not check for updates until they are back on the
   LAN or VPN. That is expected. Do not raise the gateway minimum version
   while a pilot user is known to be away, or they are locked out of work
   until they return.

## R10 · Diagnostics collection from a desktop

Purpose: get evidence from a workstation without exposing secrets or case
content.

Steps:

1. User: Settings → Export diagnostics bundle → save; the bundle contains
   redacted rolling logs, session ids, correlation ids, app/Windows/package
   versions, last compatibility response, update check results; no tokens,
   no attachment content.
2. Attach to the Kanmer ticket (proof type `command-log`), never to chat or
   email in clear where avoidable.
3. Correlate with gateway telemetry by correlation id (if App Insights has
   the window — PLAT-034) or with the gateway's action history.
4. If the app cannot start: collect `%LOCALAPPDATA%\Packages\<pfn>\LocalState\logs`
   manually and `Get-AppxLog -ActivityId` output for install/update
   failures.

Does not prove: the root cause — it is input to a ticket.
