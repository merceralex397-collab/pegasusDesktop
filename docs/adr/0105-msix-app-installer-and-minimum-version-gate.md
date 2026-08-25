---
id: ADR-0105
status: accepted
date: 2026-08-24
supersedes: []
superseded_by: []
related_capabilities: []
related_frd: []
tags: [desktop, msix, app-installer, release]
---

# ADR-0105: Signed MSIX/App Installer and minimum-version gate

## Status

Accepted on 2026-08-24.

## Context

Pegasus permits mandatory desktop updates. The release path must prevent a
known-bad or obsolete client from continuing to perform work, while keeping
package installation within the Windows platform rather than adding a bespoke
updater. The project has three settled operational constraints:

- D-002: production packages use a self-managed signing certificate whose trust
  is established per workstation in `LocalMachine\TrustedPeople`;
- D-003: the update feed is an in-house UNC share over SMB, not an Azure resource;
- C-01: repositories are private, so GitHub Releases and GitHub Pages are not a
  distribution channel.

### Cloud-justification test

| Question | Answer | Evidence |
| --- | --- | --- |
| Shared authority — must several users see and update the same state? | Yes | The approved UNC App Installer feed holds one signed channel manifest and compatibility policy for every client. |
| Unattended execution — must it run with every desktop closed? | Yes | The approved App Installer feed is maintained on an always-on in-house Windows host, so it remains available while every desktop client is closed. |
| Protected credentials — long-lived secret that must not sit on workstations? | Yes | The self-managed signing certificate private key and password stay on the in-house signing host; they are never installed on workstations. |
| Public callback — must an external service call a stable public endpoint? | No | The in-house SMB feed has no public callback. |
| Central enforcement — revocation, permissions, audit, invariant independent of the client? | Yes | The `Pegasus.Web` gateway rejects a client below the supported version even when package update configuration fails. |
| Measured operational advantage — measured evidence central is materially better? | No | Shared authority and central enforcement already justify the minimum boundary. |

## Decision

Use two complementary enforcement layers:

1. Signed self-contained MSIX packages distributed through an App Installer feed
   on the approved UNC share. The App Installer configuration performs an
   on-launch update check and configures `ShowPrompt="true"` with
   `UpdateBlocksActivation="true"` where the supported schema and launch path
   permit it. This is not universal activation enforcement: for packaged desktop
   applications `ShowPrompt` uses silent-update behaviour, and both attributes
   have no effect for desktop-shortcut or taskbar launches.
2. The gateway exposes a pre-session client-compatibility result and requires a
   client version on authenticated requests. It fails closed with a specific
   problem response when the client is below the centrally configured minimum.

The package mechanism performs trusted installation and best-effort on-launch
update delivery. The gateway minimum-version gate is the unconditional,
fail-closed protection for every launched obsolete client, including one reached
through a desktop shortcut or taskbar. The gateway's existing authorized release
route is unchanged. The decision does not add a new Azure feed, Microsoft Store
channel, or GitHub-based distribution channel.

Before an internally signed package is installed, the approved signing
certificate is trusted on the target workstation in `LocalMachine\TrustedPeople`.
Private keys and certificate passwords remain release-process secrets and are
never bundled with the desktop application or committed to the repository.

## Consequences

- App Installer update behaviour is limited by the supported schema, platform,
  and launch path; desktop-shortcut and taskbar launches are outside
  `ShowPrompt`/`UpdateBlocksActivation` enforcement. The compatible gateway
  still rejects obsolete clients independently of App Installer behavior.
- The client-compatibility response is a narrow contract: minimum version,
  current version, channel, and maintenance state. It is not a general update
  service.
- Packaging, feed access, certificate trust, rollback, and pilot/production
  publishing need their own operational evidence and exact-target approvals;
  this ADR records the architectural choice, not a completed rollout.
- App Installer's forced-update setting requires a schema and Windows version
  that support it; packaging verification must prove the actual generated file.

## Options considered

- **Bespoke updater:** rejected because signed MSIX/App Installer provides the
  required installation and update mechanism without another updater runtime.
- **App Installer enforcement only:** rejected because it cannot centrally stop
  a serious unsupported client when local update configuration fails.
- **Gateway enforcement only:** rejected because it does not provide trusted
  package installation and update delivery.

## Links

- [Native desktop conversion proposal — forced updates and compatibility](../desktop/Pegasus_Native_Desktop_Design_Proposal.md)
- [Conversion governance and ADR set](../desktop/00-governance-and-workflow/README.md)
- [ADR-0007: Direct authorised-terminal Azure deployment](0007-direct-terminal-azure-deployment.md)
- [Microsoft Learn: Configure update settings in the App Installer file](https://learn.microsoft.com/windows/msix/app-installer/update-settings)
- [Microsoft Learn: s2:OnLaunch remarks](https://learn.microsoft.com/uwp/schemas/appinstallerschema/element-s2-onlaunch#remarks) (fetched 2026-08-25)
- [Microsoft Learn: Create a certificate for package signing](https://learn.microsoft.com/windows/msix/package/create-certificate-package-signing)
