---
id: ADR-0104
status: accepted
date: 2026-08-24
supersedes: []
superseded_by: []
related_capabilities: []
related_frd: []
tags: [desktop, offline, cache]
---

# ADR-0104: Online-required client with bounded local cache

## Status

Accepted on 2026-08-24.

## Context

Pegasus is a shared case-management system. An offline-first replicated case
database would add stale authorization, conflict resolution, sensitive-data
persistence, another migration stream, and ambiguous authority without a stated
operator need. Desktop responsiveness still needs small local working state and
safe handling of a temporary connectivity loss.

### Cloud-justification test

| Question | Answer | Evidence |
| --- | --- | --- |
| Shared authority — must several users see and update the same state? | No | The local cache is per-device and is never authoritative. |
| Unattended execution — must it run with every desktop closed? | No | Cached UI state exists only for the active desktop. |
| Protected credentials — long-lived secret that must not sit on workstations? | No | The cache holds no service credential; a refresh/session handle uses the Windows credential store. |
| Public callback — must an external service call a stable public endpoint? | No | The cache has no inbound endpoint. |
| Central enforcement — revocation, permissions, audit, invariant independent of the client? | No | Authority stays at the gateway; cache contents cannot authorize a write. |
| Measured operational advantage — measured evidence central is materially better? | No | Cache behavior is local and no central cache is justified. |

All six answers are no, so local transient state belongs in the desktop; the
authoritative data path remains central under ADR-0103.

## Decision

Pegasus is online-required and does not implement offline replication. The
desktop may retain bounded, non-authoritative state: access token in memory,
refresh/session handle in the Windows credential store, user preferences, small
reference-data snapshots, thumbnails, temporary document working copies, a
short-lived compatibility result, and rolling redacted diagnostic logs.

It must not create a full local case database, synchronization engine, or silent
command queue. Existing safe on-screen data may remain visible while disconnected;
new authoritative saves are disabled or are an explicitly labelled draft, and no
action is shown as complete until the gateway confirms it.

## Consequences

- The UI reports disconnected state clearly and rechecks connectivity, without
  misrepresenting it as invalid credentials.
- Temporary files are retained safely for an explicit retry path; they are not
  evidence of a completed server command.
- SQLite or another durable local store needs measured evidence that server
  queries and memory caching cannot meet the target, plus a later decision that
  specifies authority, security, lifecycle, and recovery.
- No offline-sync compatibility layer is retained merely because the application
  is native.

## Options considered

- **Offline-first replicated case database:** rejected for its conflict,
  authorization, data-protection, migration, and recovery surface.
- **No local state at all:** rejected because it would make ordinary desktop
  interaction and safe temporary work needlessly poor.
- **Distributed cache or cloud cache by default:** rejected without a measured
  operational need.

## Links

- [Native desktop conversion proposal — local state and offline behaviour](../desktop/Pegasus_Native_Desktop_Design_Proposal.md)
- [Conversion governance and ADR set](../desktop/00-governance-and-workflow/README.md)
- [ADR-0103: Gateway, not direct workstation database access](0103-gateway-not-direct-database-access.md)
