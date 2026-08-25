# Files — Triage authority reconciliation

## Files changed

| Path | Change | Risk / validation |
| --- | --- | --- |
| `docs/operator-notes.md` | Amend Stage 0 under the ticket’s explicit operator authority: Triage is a separate product-case aggregate with `T-` identity and custody; it is not a normal Case/PO/principal allocation. | Protected business authority. Preserve the non-definitive finding, reply evidence, and normal Case boundaries; independent review compares the decision to the ticket evidence. |
| `docs/prd/pegasus-product.md` | State the product outcome: Triage is a separate recorded matter with immutable T-reference and a governed conversion to a normal Case. | PRD must state intent, not data-transfer mechanics. |
| `docs/frd/frd-03-triage.md` | Own the Triage identifier, separate aggregate/custody boundary, normal-acceptance conversion, and immutable non-duplicating transfer record. | Must retain the existing state/finding/reply-evidence rules and avoid implementation mechanics. |
| `docs/frd/frd-01-case-identity-and-lifecycle.md` | State the normal Case side of conversion: it is the first point at which Principal and Case/PO allocation can occur. | Must not weaken normal Case identity-critical gates. |
| `docs/adr/0030-triage-as-separate-aggregate.md` | New thin ADR for the explicit Core aggregate boundary. | One decision only; no schema/runtime design or implementation claim. |
| `docs/adr/README.md` | Index ADR-0030 as accepted. | Index must match ADR front matter. |
| `docs/capabilities.md` | Align TRI-01 and TRI-07 descriptions with the new authority. | Registry remains non-normative and links to FRD-03. |
| `docs/design/README.md` | Replace direct “pre-case entity/record” wording with the separate Triage aggregate boundary, preserving UI scope and no Case/PO allocation. | Design remains downstream of FRD-03; no new screen or UI behaviour is designed here. |

## Context files read

| Path | Why it matters |
| --- | --- |
| `docs/operator-notes.md` | Binding business truth and the conflicting Stage 0 text. |
| `docs/prd/pegasus-product.md` | Product intent and allocation boundary. |
| `docs/frd/frd-01-case-identity-and-lifecycle.md` | Normal Case identity, principal, and Case/PO rules. |
| `docs/frd/frd-03-triage.md` | Current Triage workflow/behaviour owner. |
| `docs/adr/0029-image-initiated-case-projection.md` | Existing analogous separate-record architecture decision; not reused for Triage. |
| `docs/adr/README.md` | ADR numbering/index conventions. |
| `docs/capabilities.md` | TRI capability descriptions that repeat the obsolete boundary. |
| `docs/design/README.md` | Downstream UI authority that repeats the obsolete boundary. |
| `src/Pegasus.Core/Triage/TriageContracts.cs` | Existing separate Triage model and the absence of a T-reference/transfer contract. |
| `src/Pegasus.Infrastructure/Persistence/PegasusDbContext.cs` | Existing separate persisted Triage entity and optional Case link. |

## Ripple effects

- [[FEAT-011]] and [[INTK-007]] must plan against the corrected FRD-03. This ticket changes neither ticket body nor implementation.
- A later, separately scoped implementation ticket must add the T-reference, custody-transfer representation, and conversion behaviour with migrations/tests/callers as appropriate.
- No source, project, migration, tests, deployment artifacts, or external systems change here.

## Validation

Run the repository documentation link check and Markdown placement check against `origin/dev` and `HEAD`; inspect the ADR index/front matter and the branch diff.

## Scope adjustment — 2026-08-25

Read-only contradiction search found two additional directly affected downstream documents, so this ticket also updates `docs/runbook.md` (Triage registration gate verification) and `docs/desktop/01-inventory-and-parity/parity-matrix.md` (the Triage Core boundary). Both changes only align their existing references with FRD-03; no workflow, code, or UI design is added.
