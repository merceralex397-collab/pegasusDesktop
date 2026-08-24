---
id: ADR-0103
status: accepted
date: 2026-08-24
supersedes: []
superseded_by: []
related_capabilities: []
related_frd: []
tags: [desktop, gateway, security]
---

# ADR-0103: Gateway, not direct workstation database access

## Status

Accepted on 2026-08-24.

## Context

The desktop conversion retains a multi-user system with shared case data,
authorization, audit, concurrency rules, and integration credentials. Giving
each workstation database connectivity would expose operational credentials,
couple clients to schema evolution, and allow client code to bypass
authorization, transactions, audit, and invariant checks.

The existing `Pegasus.Web` boundary is evolved in place into the gateway. This
is a narrow trusted boundary, not a second implementation of business policy or
a new microservice estate.

## Cloud-justification test

| Question | Answer | Evidence |
| --- | --- | --- |
| Shared authority — must several users see and update the same state? | Yes | Case, document, and account state has one authoritative shared database. |
| Unattended execution — must it run with every desktop closed? | No | An interactive gateway request is initiated by a client; workers are addressed separately. |
| Protected credentials — long-lived secret that must not sit on workstations? | Yes | Database and organization integration credentials remain server-held. |
| Public callback — must an external service call a stable public endpoint? | No | A public callback is not required to justify normal desktop-to-gateway requests. |
| Central enforcement — revocation, permissions, audit, invariant independent of the client? | Yes | Authorization, concurrency, transactions, audit, and client-version rejection must survive a compromised or stale client. |
| Measured operational advantage — measured evidence central is materially better? | No | The required central controls already justify the boundary. |

## Decision

Workstations never connect directly to the Pegasus database. The desktop talks to
the gateway over versioned HTTPS APIs. `Pegasus.Web` evolves into that gateway
and remains a composition root over the existing Core and Infrastructure
boundaries; a separate gateway product or deployment unit is not introduced.

The gateway independently authenticates and authorizes requests, rechecks
authoritative invariants and concurrency tokens, applies shared data changes in a
transaction, records audit data, holds or brokers protected integrations, and
can reject an unsafe client version. Desktop validation improves interaction but
is not authority.

## Consequences

- The desktop uses explicit API contracts and does not carry database connection
  strings, SQL credentials, ORM mappings, or database-write shortcuts.
- The gateway must expose only the routes and contracts a real desktop caller
  needs; generic repository or database-shaped endpoints are not introduced.
- Shared Core policy remains the one business-policy owner. Gateway and desktop
  callers must not drift into duplicate domain implementation.
- ADR-0014 remains in force: local Test/UAT does not become an Azure environment
  merely because a gateway boundary exists.

## Options considered

- **Direct database access from every desktop:** rejected for credentials,
  authorization bypass, schema coupling, audit, and concurrency risks.
- **A new standalone gateway service:** rejected because `Pegasus.Web` can carry
  the boundary without another deployable or duplicated composition root.
- **Trust desktop validation alone:** rejected because it cannot enforce shared
  authority independently of the client.

## Links

- [Native desktop conversion proposal — gateway boundary](../desktop/Pegasus_Native_Desktop_Design_Proposal.md)
- [Conversion governance and ADR set](../desktop/00-governance-and-workflow/README.md)
- [ADR-0002: .NET modular monolith](0002-dotnet-modular-monolith-on-azure.md)
- [ADR-0015: Pegasus Web hosting](0015-host-web-on-container-apps-consumption.md)
