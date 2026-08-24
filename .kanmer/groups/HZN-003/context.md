# Phase 2 — compatibility, update and authentication

## Delivers

The two things that make everything after it shippable (proposal §24 Phase 2): an obsolete client cannot proceed, and an existing Pegasus staff account is sufficient to sign in. Concretely — the gateway compatibility endpoint, the MSIX/App Installer pilot feed, the forced-update screen and flow, the desktop login and session client against the existing account store, credential storage and revocation handling, the role-aware shell, and the API generated-client pipeline.

## Plan folders and ticket-handle ranges

- `docs/desktop/03-gateway-api-and-data/` — DSK-03-01…DSK-03-18 → `gateway-api` (GWY); endpoint groups continue into Phase 4 as the slices need them
- `docs/desktop/04-auth-session-update-and-startup/` — DSK-04-01…DSK-04-15 → gateway rows `gateway-api` (GWY), desktop rows `desktop-foundation` (FND)
- `docs/desktop/09-release-update-and-distribution/` — DSK-09-01…DSK-09-18, the pilot-feed and packaging subset → `release-desktop` (REL). **`DSK-09-07` and `DSK-09-09` are withdrawn** — nothing may depend on them

## Entry condition and exit gate

Entry: the Phase 1 exit gate is met — a clean Windows 11 machine launches the native shell and the foundation tests pass.

Exit gate (proposal §24 Phase 2; **owner: plan 04**):

- Current user credentials work.
- Microsoft login is not required.
- An obsolete package is blocked and updates.
- A disabled account is rejected.
- Tokens and secrets pass storage review.

## Decisions and constraints that bind this phase

- **L-01** — versioned `/api/v1` route groups and the staff token flow live beside the Razor Pages in the same `Pegasus.Web` Container App; endpoints ship behind `Features:DesktopGateway` so `main` stays releasable for the live web app.
- **L-02** — Test/UAT is the local production-mimicking stack; ADR-0014 stands, and asking for an Azure test resource is out of bounds.
- **D-002** (2026-08-23) — production signing uses a self-managed certificate trusted per workstation in `LocalMachine\TrustedPeople`; no Azure signing service. Certificate issuance is an **operator step**.
- **D-003** (2026-08-23) — the update feed is a UNC file share on an always-on in-house Windows host, served to App Installer over SMB. Provisioning that share is an **operator step**.
- **C-01** — GitHub Releases and GitHub Pages are ruled out permanently as feed hosting; the repositories become private.

## Azure rule

Reads are free; every write is ⚠, needs exact-target approval (`docs/runbook.md` § Live operation approval matrix) and is mirrored in `docs/desktop/11-azure-disposition/README.md`; nothing is deprovisioned before cutover, observed use and rollback approval. The one live candidate here is enabling `Features:DesktopGateway` on the Web Container App (DSK-11-06) — applied through Bicep and the release route, never ad hoc.

## Read before starting

- `docs/desktop/README.md`
- `docs/desktop/00-governance-and-workflow/README.md` § Phase map
- `docs/desktop/03-gateway-api-and-data/README.md` and `endpoint-map.md`
- `docs/desktop/04-auth-session-update-and-startup/README.md`
- `docs/desktop/09-release-update-and-distribution/README.md`, with `appinstaller-template.md` and `signing-and-hosting-decision-matrix.md`
- `docs/desktop/11-azure-disposition/README.md`
- `docs/runbook.md` § Live operation approval matrix
- `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 8, § 9, § 10, § 24 Phase 2
- `.kanmer/groups/HZN-001/board-conventions.md`
