---
id: ADR-0030
status: accepted
date: 2026-08-25
supersedes: []
superseded_by: []
related_capabilities: [TRI-01, TRI-07]
related_frd: [frd-01, frd-03]
tags: [triage, aggregate, custody]
---
# ADR-0030: Triage as a separate aggregate

## Status

Accepted.

## Context

Triage is business work in its own right, but normal Case identity requires a
formal instruction to pass the existing acceptance, Principal, and Case/PO
allocation gates. Treating Triage as a normal Case weakens those gates;
treating it only as pre-case intake loses its independently identifiable work,
custody, and history.

## Decision

Pegasus.Core models Triage as a separate aggregate with its own immutable
T-reference, custody, and history. It is not a normal Case aggregate, Case
state, Principal allocation, or Case/PO allocation. Shared services may be
used where they already fit, but neither aggregate owns duplicate business
policy.

Only a later formal instruction that passes the normal Case acceptance and
allocation gates creates a linked standard Case. Conversion moves Triage
evidence into that Case's custody and records the immutable transfer required
by FRD-03. It does not retain duplicate evidence copies or make a Triage
finding a normal Case or Engineer decision.

## Consequences

- Triage and normal Case retain separate identities, lifecycles, and custody
  until conversion.
- Normal Principal and Case/PO allocation rules remain unchanged.
- A later implementation owns any schema, migration, use-case, caller, and
  proof work necessary to supply the T-reference and transfer behaviour; this
  ADR claims none of it is delivered.

## Links

- [FRD-01](../frd/frd-01-case-identity-and-lifecycle.md)
- [FRD-03](../frd/frd-03-triage.md)
