---
id: ADR-0031
status: accepted
date: 2026-08-26
supersedes: [ADR-0105]
superseded_by: []
related_capabilities: []
related_frd: []
tags: [desktop, msix, app-installer, release]
---

# ADR-0031: Desktop release distribution contract

## Status

Accepted on 2026-08-26. This ADR supersedes ADR-0105 because the accepted
record did not include the complete Area 09 package-version, channel, and
rollback contract. The body of ADR-0105 remains unchanged.

## Context

Pegasus permits mandatory desktop updates. The release path must prevent a
known-bad or obsolete client from continuing to perform work, while keeping
package installation within the Windows platform rather than adding a bespoke
updater. ADR-0105 established the two-layer choice, but did not record every
release detail already settled by Area 09 §3.

The project has three settled operational constraints:

- D-002: production packages use a self-managed signing certificate whose
  trust is established per workstation in `LocalMachine\TrustedPeople`;
- D-003: the update feed is an in-house UNC share over SMB, not an Azure
  resource;
- C-01: repositories are private, so GitHub Releases and GitHub Pages are not
  a distribution channel.

## Current evidence

- `docs/desktop/09-release-update-and-distribution/README.md` §3 records the
  2021 schema, package version, channel, release-order, rollback, signing,
  and feed-hosting decisions.
- `docs/desktop/09-release-update-and-distribution/appinstaller-template.md`
  records the canonical XML shape, including the
  `<ForceUpdateFromAnyVersion>true</ForceUpdateFromAnyVersion>` element.
- `docs/desktop/09-release-update-and-distribution/signing-and-hosting-decision-matrix.md`
  records that D-002 and D-003 were settled on 2026-08-23.
- `docs/desktop/03-gateway-api-and-data/README.md:179` and
  `docs/desktop/04-auth-session-update-and-startup/README.md:175-186`
  establish that the minimum client version is an audited database-backed
  Administrator setting. `Desktop:MinimumClientVersion` is bootstrap-only.

## Cloud-justification test

The feed and the gateway gate are separate scopes. The feed itself answers
No to each placement question; the gateway answers Yes only for central
enforcement, because that invariant must hold independently of the client.

| Question | Feed | Gateway minimum-version gate | Evidence |
| --- | --- | --- | --- |
| Shared authority — must several users see and update the same state? | No | No | The feed is a delivery location; compatibility policy is owned by the gateway's audited setting, not by the feed. |
| Unattended execution — must it run with every desktop closed? | No | No | Neither scope is an unattended business process; the always-on host is an availability arrangement, not a cloud execution requirement. |
| Protected credentials — long-lived secret that must not sit on workstations? | No | No | The signing private key stays on the release host and is outside the feed; the gateway gate does not place a new long-lived secret in the desktop. |
| Public callback — must an external service call a stable public endpoint? | No | No | The UNC/SMB feed and the gateway compatibility request do not require a public callback. |
| Central enforcement — revocation, permissions, audit, invariant independent of the client? | No | Yes | `Pegasus.Web` rejects a client below the centrally configured minimum even when App Installer cannot update or a launch path bypasses package settings. |
| Measured operational advantage — measured evidence central is materially better? | No | No | The gateway placement is required by the client-independent invariant; no separate measured advantage is claimed. |

## Decision

Use two complementary enforcement layers:

1. Signed self-contained MSIX packages are distributed through a direct
   `.appinstaller` file on the approved UNC share. The App Installer file uses
   the 2021 schema
   (`http://schemas.microsoft.com/appx/appinstaller/2021`) with
   `OnLaunch HoursBetweenUpdateChecks="0"`, `ShowPrompt="true"`,
   `UpdateBlocksActivation="true"`, and `AutomaticBackgroundTask`. These
   settings remain subject to the platform and launch-path limits recorded in
   ADR-0105; they are not universal activation enforcement.
2. The gateway exposes a pre-session client-compatibility result and requires
   a client version on authenticated requests. Its minimum-version value is
   an audited database-backed Administrator setting. The gateway gate fails
   closed after a short cached window; the package update check fails open
   when the feed is unreachable. Both layers are required, and the gateway
   gate is authoritative.

The desktop package version is `1.<minor>.<build>.0`: the release owner bumps
`minor` when the gateway compatibility range changes, the CI run number is the
`build` component, and the revision is always `0`. The gateway product version
remains `0.1.0-alpha.1`; the desktop version is carried by
`Package.appxmanifest` and the desktop release manifest.

There is one package identity, `CollisionEngineers.Pegasus`, with two channel
feeds: `pilot/Pegasus.appinstaller` and `prod/Pegasus.appinstaller`. A
workstation belongs to the channel from which its `.appinstaller` was
installed; changing rings is a reinstall from the other feed.

The feed retains the known-good previous package for every channel. A rollback
publishes that package with a higher `.appinstaller` `Version` and the XML
element `<ForceUpdateFromAnyVersion>true</ForceUpdateFromAnyVersion>`.

The gateway is deployed first and remains backward compatible, the desktop
package follows, and the minimum client version is raised only after the pilot
ring has run the new package. The gateway's existing authorised-terminal
release route is unchanged.

## Consequences

- D-002 (self-managed certificate) and D-003 (UNC feed) were decided on
  2026-08-23. Their combined distribution path has no Azure resource and no
  recurring service cost.
- The accepted trade-offs are per-machine certificate trust rollout, a
  rehearsed renewal, and update checks that work only on the office network or
  VPN.
- Private keys and certificate passwords remain release-process secrets and
  are never bundled with the desktop application or committed to the
  repository. The manifest `Publisher` must match the signing certificate
  subject exactly.
- Test/UAT remains local under ADR-0014; this decision creates no Azure test
  feed or environment.
- Packaging, feed access, certificate trust, rollback, and pilot/production
  publishing need their own operational evidence and exact-target approvals.
  This ADR records the design, not a completed rollout.

## Verification

The release and packaging tickets prove the generated 2021-schema
`.appinstaller` file, exact `Publisher`/certificate-subject match, canonical
UNC `Uri`, SMB access from the office network or VPN, and the two-layer
minimum-version behavior. The gateway tickets prove the audited setting and
fail-closed rejection. This ADR records the design and repository evidence;
it does not claim a package release, feed publication, Azure change,
deployment, or runtime acceptance.

## Reversal/deprovision condition

Replace this decision through a new ADR if the in-house share cannot meet the
network/VPN availability or access-control requirement, if the exact
Publisher/trust rollout cannot be maintained, or if measured runtime evidence
shows that the gateway minimum-version gate cannot remain fail-closed. Remove
feed or signing assets only through the release runbook after the installed
client population and rollback path are accounted for.

## Options

- **Bespoke updater:** rejected because signed MSIX/App Installer provides the
  required installation and update mechanism without another updater runtime.
- **App Installer enforcement only:** rejected because it cannot centrally stop
  a serious unsupported client when local update configuration fails.
- **Gateway enforcement only:** rejected because it does not provide trusted
  package installation and update delivery.

## Relates

- ADR-0007 — the gateway's existing authorised-terminal release route is
  unchanged.
- ADR-0014 — Test/UAT remains local; this decision does not create an Azure
  test feed or environment.
- ADR-0105 — superseded by this complete release contract; its body remains
  unchanged as the historical decision record.
- FRD-13 — the future desktop gateway compatibility contract, to be authored
  by [[DSK-00-08]] (FND-008); this ADR does not claim that FRD-13 exists yet.

## Links

- [Native desktop conversion proposal — forced updates and compatibility](../desktop/Pegasus_Native_Desktop_Design_Proposal.md)
- [Release, update and distribution plan](../desktop/09-release-update-and-distribution/README.md)
- [App Installer template](../desktop/09-release-update-and-distribution/appinstaller-template.md)
- [Conversion governance and ADR set](../desktop/00-governance-and-workflow/README.md)
- [ADR-0007: Direct authorised-terminal Azure deployment](0007-direct-terminal-azure-deployment.md)
- [ADR-0014: Local-to-production deployment](0014-local-to-production-deployment.md)
- [ADR-0105: Original signed MSIX/App Installer decision](0105-msix-app-installer-and-minimum-version-gate.md)
- [Microsoft Learn: Configure update settings in the App Installer file](https://learn.microsoft.com/windows/msix/app-installer/update-settings)
- [Microsoft Learn: Create a certificate for package signing](https://learn.microsoft.com/windows/msix/package/create-certificate-package-signing)
