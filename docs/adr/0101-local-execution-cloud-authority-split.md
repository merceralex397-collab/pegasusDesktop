---
id: ADR-0101
status: accepted
date: 2026-08-24
supersedes: []
superseded_by: []
related_capabilities: []
related_frd: []
tags: [desktop, cloud-boundary, architecture]
---

# ADR-0101: Local-execution / cloud-authority split

## Status

Accepted on 2026-08-24.

## Context

Changing the UI to desktop does not itself justify moving work to Azure, nor
does it justify moving every responsibility to each workstation. Pegasus has
shared authoritative case data, central authorization, service credentials, and
unattended intake, while most interactive work benefits from running beside the
operator. A repeatable placement rule is needed so the conversion does not grow
new cloud resources or an offline-sync estate by habit.

Test/UAT remains local under ADR-0014. This decision does not supersede
ADR-0014 and does not authorize a new Azure development, test, staging, or UAT
environment.

## Cloud-justification test

| Question | Answer | Evidence |
| --- | --- | --- |
| Shared authority — must several users see and update the same state? | No | The placement test is a design rule, not a shared runtime responsibility. |
| Unattended execution — must it run with every desktop closed? | No | The rule is applied during design and review. |
| Protected credentials — long-lived secret that must not sit on workstations? | No | The rule stores no service credential. |
| Public callback — must an external service call a stable public endpoint? | No | The rule is not an externally callable service. |
| Central enforcement — revocation, permissions, audit, invariant independent of the client? | No | Individual cloud responsibilities, not this document, provide those controls. |
| Measured operational advantage — measured evidence central is materially better? | No | There is no runtime component to centralize. |

All six answers are no: the test is local design and review guidance. It decides
where actual responsibilities belong.

## Decision

Every proposed cloud-hosted responsibility must answer yes to at least one of
the following questions, with ticket-specific evidence:

1. Must several users see and update the same authoritative state?
2. Must it continue when all desktops are closed?
3. Does it require a long-lived or powerful credential that must not be on a
   workstation?
4. Must an external service call a stable public endpoint?
5. Must revocation, permission, audit, or an invariant be enforced independently
   of the client?
6. Is there measured evidence that central execution is materially more reliable,
   faster, or cheaper?

When every answer is no, the responsibility belongs in the desktop client. The
statements “it is already in Azure”, “the web app does it”, and “it may scale
later” are not evidence. A positive answer must identify the smallest central
boundary that satisfies it; it does not authorize unrelated cloud infrastructure.

## Consequences

- Native UI, view state, immediate interaction, and suitable deterministic work
  default to local execution.
- Shared authority, protected service credentials, public callbacks, and
  unattended intake remain central only when the relevant question proves the
  need.
- A ticket that adds or retains a cloud dependency records the answers and
  evidence rather than relying on previous placement.
- No new Azure environment or component is implied by this ADR. Azure changes
  continue to require the repository's exact-target approval process.

## Options considered

- **Keep all existing web responsibilities in the cloud:** rejected because it
  makes current placement an unexamined requirement.
- **Move all work to the desktop:** rejected because it would expose secrets,
  weaken central enforcement, and stop unattended work.
- **Decide placement ticket by ticket without a rule:** rejected because similar
  responsibilities would drift into different boundaries.

## Links

- [Native desktop conversion proposal — cloud-justification test](../desktop/Pegasus_Native_Desktop_Design_Proposal.md)
- [Conversion governance and ADR set](../desktop/00-governance-and-workflow/README.md)
- [ADR-0002: .NET modular monolith](0002-dotnet-modular-monolith-on-azure.md)
- [ADR-0014: Local-to-production deployment](0014-local-to-production-deployment.md)
