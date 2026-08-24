---
id: ADR-0100
status: accepted
date: 2026-08-24
supersedes: []
superseded_by: []
related_capabilities: []
related_frd: []
tags: [desktop, winui, windows]
---

# ADR-0100: Native WinUI 3 client in the Pegasus fork

## Status

Accepted on 2026-08-24.

## Context

Pegasus needs a native operator client, not a desktop wrapper around the existing
web UI. The operator has chosen the active Pegasus fork as the conversion line;
the client is a clean WinUI 3 implementation in that repository rather than a
permanent second Pegasus repository. Windows 11 x64 is the supported platform.

This decision supersedes only the sentence in
[ADR-0009](0009-adopt-pegasus-monorepo-workspaces.md) that deferred a future
desktop workbench. It does not supersede ADR-0009's workspace boundary, and it
does not change ADR-0016. The clause-level relation is deliberately recorded
here: `supersedes` stays empty because ADR-0009 is not wholly superseded.

The earlier desktop-conversion documents named by the proposal are not present
in this repository and are not implementation input. The current conversion
brief, repository evidence, and accepted ADRs are the authority for this work.

## Cloud-justification test

| Question | Answer | Evidence |
| --- | --- | --- |
| Shared authority — must several users see and update the same state? | No | Native presentation and local interaction state are per workstation; shared case state remains behind the gateway. |
| Unattended execution — must it run with every desktop closed? | No | An operator client runs only while its user is present. |
| Protected credentials — long-lived secret that must not sit on workstations? | No | The desktop holds no service credential; gateway-held integrations remain central. |
| Public callback — must an external service call a stable public endpoint? | No | The client is not an inbound endpoint. |
| Central enforcement — revocation, permissions, audit, invariant independent of the client? | No | Those controls are enforced by the gateway, not by the presentation client. |
| Measured operational advantage — measured evidence central is materially better? | No | No evidence justifies hosting the native UI remotely. |

All six answers are no, so the native client belongs on the operator workstation.

## Decision

Build Pegasus as a genuine WinUI 3 Windows 11 x64 application inside this fork.
It uses native XAML screens and Windows controls; a WebView/WebView2 shell around
the current web UI is prohibited. Interactive presentation, view state, and
user-triggered local work belong in the desktop client. Shared source may be
reused where it respects the Core ownership boundary, but the desktop must not
duplicate business policy in a second implementation.

The existing web and worker components remain only while their actual callers
remain. Their existence does not dictate desktop navigation, UI structure, or a
compatibility wrapper. Removal follows parity and cutover evidence, not this ADR.

The conversion uses the reserved ADR-0100–ADR-0110 block confirmed by the
operator on 2026-08-23. The block prevents collisions with the still-active
upstream ADR sequence; it is not a new numbering convention for other work.

## Consequences

- D-001 is recorded: when the first production gateway change is needed, this
  fork becomes the single Pegasus release source; the upstream repository is
  merged and then frozen only after agreement with its owners. This ADR does not
  claim that the freeze has already occurred.
- The desktop is native by construction, so web pages, browser state, and
  browser performance are not carried forward as a default implementation.
- The gateway, database, workers, and document stores remain separate
  responsibilities justified by their own ADRs and actual callers.
- A new desktop project is an implementation consequence, not an independent
  product or deployment boundary; it must remain within this repository's
  documented dependency and release rules.

## Options considered

- **Wrap the current web application in WebView2:** rejected because it retains
  web layout and browser behaviour instead of delivering a native client.
- **Create a permanent second desktop repository:** rejected because it would
  split contracts, migrations, CI, and parity ownership.
- **Keep the desktop workbench deferred:** rejected by the operator's approved
  conversion direction.

## Links

- [Native desktop conversion proposal](../desktop/Pegasus_Native_Desktop_Design_Proposal.md)
- [Conversion governance and ADR set](../desktop/00-governance-and-workflow/README.md)
- [ADR-0009: Pegasus monorepo workspaces](0009-adopt-pegasus-monorepo-workspaces.md)
- [Repository ADR conventions](../../AGENTS.md#adr-conventions)
