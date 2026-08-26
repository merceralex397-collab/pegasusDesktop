# FRD-03: Triage

## Boundary with Unidentified

Triage is a distinct product-case aggregate, not a normal Case aggregate or a
Case state. A Triage request without a usable vehicle registration remains
Unidentified until that identity gate is met; it does not receive a Triage
reference merely because it is awaiting information. Material that is not
accepted as Triage, or a terminal unreadable/ambiguous source outside that
workflow, enters Unidentified with its canonical reason.
> Owner capabilities: TRI · Source PRD: [Pegasus product requirements](../prd/pegasus-product.md) · UI behaviour: docs/design/README.md

## Triage

### Identity, aggregate, and custody

Each Triage has the next immutable `T-00001`-style Triage reference and its
own aggregate, history, evidence, and custody. A Triage receives neither a
Principal nor a Case/PO and is never relabelled as a normal Case. Its finding
is not a definitive Engineer outcome.

### Normal workflow and completion evidence

Triage begins when the exact accepted route policy classifies a provider request as an assessment request or an authorised staff member manually classifies safely retained, attributable material as Triage. For an automatic QDOS intake, the classified `pre-instruction-emails/triage-request` result is recorded as exactly one strong accepted triage-match evidence entry and is never sent to normal Case allocation. Manual classification records the source, available route evidence, actor, time, reason, and policy version; it neither invents Principal identity nor creates a normal Case. Material whose route or category remains unaccepted stays Unidentified and never becomes Triage or a Case by fallback. A Triage request without a usable VRM remains Unidentified; with a VRM it receives its Triage reference and opens as `Open`, may move to `Awaiting information`, records an accepted finding as `Finding recorded`, and reaches `Completed` only after the required response evidence is confirmed. An acknowledgement, request for information, Draft, queue action, or other correspondence may be retained but is not itself a finding or completion evidence.

Triage records have the states `Open`, `Awaiting information`, `Finding recorded`, `Completed`, and `Cancelled`.

A recorded finding has two independently optional dimensions:

- Roadworthiness: `Roadworthy` or `Unroadworthy`;
- Assessment: `Repairable` or `Total loss`.

At least one dimension is required. A later correction creates a reasoned superseding finding; it never overwrites history. A pre-send correction replaces the current finding with a reason. A post-send correction creates a superseding finding, returns the Triage to `Finding recorded`, and requires a new response.

Every `Completed` Triage has one exact reply-chain Sent item from an approved mailbox. Subject, VRM, a manual “sent” assertion, a Draft, a queue result, an acknowledgement, or an unrelated Sent item is not completion evidence. `Cancelled` is the only terminal Triage outcome without a finding and reply; `Completed` and `Cancelled` close only that Triage workflow and never make its finding definitive for a later normal Case.

### Conversion to a normal Case

A Triage converts only when later formal instructions pass the normal acceptance,
Principal, and Case/PO allocation gates in [FRD-01](frd-01-case-identity-and-lifecycle.md#principal-reference-organisation-and-case-party-identity). Conversion creates a linked standard Case; the Triage identity remains permanent and is never reused as a Case/PO.

On conversion, Triage evidence moves into the linked Case's custody. The
immutable transfer record retains the source Triage reference, transfer time,
authenticated actor or named system, destination Case/PO, and the identity and
version of every transferred content item. The transfer does not retain duplicate
evidence copies in Triage custody. It preserves source provenance and does not
make a Triage finding a Case or Engineer decision.

Triage may have an optional assignee but no due date or chase schedule. A
Triage may have at most one converted normal Case, and a normal Case may retain
many converted Triage histories. The conversion link and transfer record remain
permanent history.

Cancellation and reopen require reasons. Reopen always returns to `Open` and never erases the prior finding, reply, actor, or chronology.
