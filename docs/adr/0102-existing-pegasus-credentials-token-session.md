---
id: ADR-0102
status: accepted
date: 2026-08-25
supersedes: []
superseded_by: []
related_capabilities: []
related_frd: [frd-04]
tags: [desktop, authentication, session, openiddict]
---

# ADR-0102: Existing Pegasus credentials and desktop token session

## Status

Accepted on 2026-08-25. This decision records the desktop session boundary;
implementation and runtime acceptance belong to the Phase 2 gateway and desktop
tickets.

## Context

The native desktop must authenticate existing Pegasus staff without introducing
Microsoft or Entra identity, a second account store, or a browser round trip for
the login screen. The gateway is the existing `Pegasus.Web` deployment unit
(L-01), and the desktop must remain a public client: it cannot safely carry a
client secret.

The existing web application uses ASP.NET Core Identity for staff accounts and
the cookie session governed by `StaffSessionPolicy`. OpenIddict is already
composed for the Automation MCP client, but that client is a different actor,
has different scopes and lifetimes, and is governed by ADR-0027. The desktop
session must not change that Automation contract.

## Current evidence

- `src/Pegasus.Web/Program.cs:263-327` composes Identity and the staff sign-in
  rate limits; `:328-457` composes the cookie session, including the two-hour
  idle lifetime and eight-hour absolute lifetime.
- `src/Pegasus.Core/Actors/StaffSessionPolicy.cs:9-13` owns those transport-
  neutral lifetimes and the sign-in limits.
- `src/Pegasus.Web/Program.cs:353` sets
  `SecurityStampValidatorOptions.ValidationInterval = TimeSpan.Zero`, and
  `:875-899` handles the existing must-change-password path.
- `src/Pegasus.Web/Mcp/AutomationMcpExtensions.cs:33-60` composes OpenIddict
  for the Automation actor with client-credentials and authorization-code/PKCE
  flows, a ten-minute access token, a fourteen-day refresh token, and ephemeral
  keys. Those settings remain outside this decision.
- `src/Pegasus.Core/Actors/StaffActorFactory.cs:8` is the claims-to-actor seam;
  `src/Pegasus.Core/Identity/StaffAuthorization.cs` fails closed for unknown
  role combinations.
- `src/Pegasus.Web/Program.cs:172-176` persists the production Data Protection
  ring to the existing Azure Blob location, while `:954` exposes the current
  version surface. No desktop token endpoint or client-version gate exists yet.

## Options

- **Reuse the staff cookie in the desktop:** rejected. It couples the native
  client to a browser-cookie transport and does not provide the gateway token
  boundary required by the `/api/v1` contract.
- **Add Microsoft/Entra sign-in:** rejected. It adds an identity authority not
  required by the existing staff account store and violates the Phase 2
  requirement that current Pegasus credentials are sufficient.
- **Use OpenIddict password and refresh grants for a first-party public client:**
  accepted. It reuses Identity password verification and the gateway's token
  boundary without placing a secret in the MSIX package.

## Cloud-justification test

| Question | Answer (yes/no) | Evidence |
| --- | --- | --- |
| Shared authority — must several users see and update the same state? | Yes | Staff identities, roles, enabled state and security stamps remain in the existing Pegasus account store behind `Pegasus.Web`; `Program.cs:263-327` composes that authority. |
| Unattended execution — must it run with every desktop closed? | No | Staff sign-in and token refresh are request-driven; no new background execution or Azure resource is required by this decision. |
| Protected credentials — long-lived secret that must not sit on workstations? | Yes | The public desktop client has no secret; its refresh handle is protected locally and token protection uses the existing persisted Data Protection ring rather than a package secret. |
| Public callback — must an external service call a stable public endpoint? | No | The first-party public client uses the Pegasus token endpoint; no external identity provider or callback is introduced. |
| Central enforcement — revocation, permissions, audit, invariant independent of the client? | Yes | The gateway issues tokens, resolves claims through `StaffActorFactory`, re-checks account state/security stamps, and owns revocation and authorization. |
| Measured operational advantage — measured evidence central is materially better? | Yes | The existing Identity/OpenIddict composition and persisted Data Protection ring are the current authority; reusing them avoids a second account store, identity provider, or deployment unit. |

## Decision

Register a first-party public OpenIddict client named `pegasus-desktop` in the
existing Pegasus gateway. Use the password grant for native sign-in and the
refresh-token grant for session renewal, with scopes `pegasus.desktop` and
`offline_access`; the client has no secret and no browser authorization-code
round trip.

Use a ten-minute access token. Refresh tokens are rolling, with a two-hour idle
lifetime and an eight-hour absolute lifetime carried by an
`original-issued-at` claim that is copied into each re-issued token. Staff
tokens use OpenIddict's Data Protection integration backed by the existing
persisted Data Protection key ring.

Every `/api/v1` request re-checks the staff account's enabled state and security
stamp. Disable, password change and explicit logout revoke the subject's
refresh tokens. The claims-to-actor path remains `StaffActorFactory` and all
unknown role combinations fail closed. The Automation MCP client keeps its
existing actor, permissions, fourteen-day refresh lifetime and
`DisableSlidingRefreshTokenExpiration()` behaviour.

When an account has `MustChangePassword`, the gateway returns the stable
`urn:pegasus:problem:password-change-required` problem type and the desktop
routes to the change-password flow, blocking all other work until the existing
Identity password-change use case succeeds.

## Consequences

- Existing Pegasus credentials remain the account authority and the desktop
  receives a gateway token session rather than a second identity.
- A refresh handle may be stored in the protected local session store; access
  tokens remain in memory. No password, client secret or provider credential is
  placed in the MSIX package.
- The Automation client's fourteen-day refresh lifetime and the server-wide
  `DisableSlidingRefreshTokenExpiration()` are **not** reused for staff. The
  staff idle/absolute pair is implemented in the token handler so MCP
  connectors keep their ADR-0027 contract.
- The current ephemeral OpenIddict keys are not suitable for staff sessions.
  Data Protection replaces them for the desktop token path so a Container App
  restart or release does not invalidate every staff session.
- Gateway implementation must prove password grant, refresh rotation, the
  absolute cap, revocation, disabled-account handling, role resolution and
  unchanged Automation MCP behaviour. This ADR does not claim those proofs.

## Verification

The gateway tickets prove the token and revocation contract with integration
tests against the local Test/UAT stack. The desktop tickets prove storage,
refresh, logout and failure-state handling. The Phase 2 exit ticket collects
the evidence. A green documentation check or a merged PR alone does not prove
the session works.

## Reversal/deprovision condition

Do not issue desktop tokens if the existing Identity authority, claims-to-actor
mapping, token protection, or revocation contract cannot be preserved. Revoke
the `pegasus-desktop` client and remove its token permissions through a reviewed
gateway change if the desktop conversion is abandoned or if a security review
finds that the public-client flow cannot meet the stated storage and revocation
properties. Do not delete or migrate existing staff accounts as part of that
reversal.

## Links

- [FRD-04](../frd/frd-04-parties-accounts-and-access.md)
- [ADR-0004](0004-provider-api-and-staff-mcp-authentication.md)
- [ADR-0011](0011-restrict-mcp-to-automation-actor.md)
- [ADR-0027](0027-authorization-code-for-external-mcp-connectors.md)
- [Phase 2 authentication plan](../desktop/04-auth-session-update-and-startup/README.md)
