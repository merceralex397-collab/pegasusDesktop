# Research — Triage authority reconciliation

## Question

What documentation changes are necessary to make the operator-approved Triage model authoritative without claiming an implementation that does not yet exist?

## Verified findings

| Finding | Evidence | Implication |
| --- | --- | --- |
| The operator explicitly chose Triage as a separate aggregate and a case in its own product right, with immutable `T-00001`-style identity, its own custody, conversion only after normal formal-instruction acceptance, and a non-duplicating immutable transfer record. | Direct operator decision, 2026-08-25, recorded in the ticket body. | This is the controlling target state for the protected operator authority and the linked requirements. |
| Stage 0 says Triage “does not technically count as a case,” is “pre-case,” and its later Case link is reference-only. | `docs/operator-notes.md` — Stage 0. | Replace those statements while retaining the non-definitive finding boundary and normal formal-Case gates. |
| FRD-03 says Triage is a separate pre-Case workflow, has no Case created by classification, and permits only an optional case link. | `docs/frd/frd-03-triage.md`. | Define the distinct Triage aggregate, its identity/custody, and conversion contract; retain the lifecycle and reply-evidence rules. |
| FRD-01 reserves Principal and Case/PO allocation for a normal Case after identity-critical acceptance. | `docs/frd/frd-01-case-identity-and-lifecycle.md` — Principal/reference identity. | Triage receives neither Principal nor Case/PO. Conversion must pass the normal Case acceptance/allocation gates and creates a separate linked normal Case. |
| The PRD calls Triage a staff workflow for a recorded matter but does not define its identifier, custody or conversion. | `docs/prd/pegasus-product.md` — Terminology and outcomes. | Add the product-level outcome without putting behaviour mechanics in the PRD. |
| The capability registry and UI authority repeat “separate pre-case record/entity.” | `docs/capabilities.md` TRI-01; `docs/design/README.md` Triage sections. | Update the directly affected downstream statements so planning is not built from obsolete terminology. |
| Existing code already has a separate `TriageRecord`/`TriageEntity`, its own lifecycle/history, and an optional `LinkedCaseId`; it has only an internal GUID, no T-reference, transfer record, or custody-transfer behaviour. | `src/Pegasus.Core/Triage/TriageContracts.cs`; `src/Pegasus.Infrastructure/Persistence/PegasusDbContext.cs`. | This is documentation-only: do not claim code, schema, migration, or caller delivery. A later implementation ticket must own those changes. |
| ADR-0029 establishes the analogous technical boundary for Image-initiated Case projection; it is not a Triage decision. | `docs/adr/0029-image-initiated-case-projection.md`. | The explicit “separate Core aggregate” decision needs its own thin ADR rather than overloading the image-intake ADR. |

## Chosen documentation boundary

Create ADR-0030 to record the durable Core boundary: Triage is a separate aggregate with its own immutable T-reference and custody, never a normal Case aggregate. It converts only by creating a linked standard Case after the standard formal-instruction acceptance, principal, and allocation gates; the transfer retains immutable source, time, actor/system, destination, and content/version identifiers without duplicate evidence copies.

The PRD will state the product outcome. FRD-03 will own lifecycle, identity, custody, and conversion behaviour. FRD-01 will own the normal Case allocation boundary. The protected operator note will preserve the historic workflow context while recording the new direct decision. The design and capability registry will be aligned as downstream documents.

## Deliberately out of scope

- Any Core, persistence, migration, gateway, desktop, mailbox, Box, Azure, release, or external write.
- Deciding transfer storage mechanics, database schema, or a new Box topology.
- Changing the substantive Triage state machine, finding rules, reply evidence, or definitive Engineer-report boundary.
- Closing or rewriting [[FEAT-011]] or [[INTK-007]]; this ticket unblocks their authoritative requirements only.

## Open questions

None. The operator supplied the identity, aggregate, custody, conversion, and transfer-record decisions directly on 2026-08-25.
