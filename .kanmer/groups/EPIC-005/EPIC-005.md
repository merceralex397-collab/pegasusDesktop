---
id: EPIC-005
kind: epic
title: 'Area 04 - auth, session, update and startup'
archived: false
created: '2026-08-24T07:25:48.730Z'
updated: '2026-08-24T07:25:48.730Z'
---
Seeds from `docs/desktop/04-auth-session-update-and-startup/README.md` (handles DSK-04-01…DSK-04-15). Delivers the staff token flow on the gateway (OpenIddict public client, bearer auth, rate limiting, revocation), the desktop session client and DPAPI credential storage, the client-compatibility gate, the forced-update flow and the observable startup and first-run sequence. Split across board areas: gateway work in `gateway-api` (GWY), desktop work in `desktop-foundation` (FND).
