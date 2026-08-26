# Open questions

No operator-only question blocks implementation. The following decisions were supplied by the operator on 2026-08-26:

- Report generation is an operator-initiated `Generate report draft` command for the desktop contract. The command is the only trigger; exact replay is idempotent and does not force a duplicate version. A changed accepted payload or template identity creates an append-only successor.
- Repair costs are externally supplied either by a connected repair-estimate system or by importing a repair-estimate document. Multiple estimates remain separate tabs, and each estimate has its own Generate action. The selected estimate and its source/provenance are part of the accepted report input; no internal rate-card derivation or cross-estimate precedence is inferred. A missing or ambiguous selected estimate remains not ready.
- Generation remains distinct from human approval, issue, sending, and receipt, and no separate renderer runtime is introduced.

- [x] **What human-readable reference appears on a generated report?** — Resolved by the operator on 2026-08-19: the report reference is the existing Case/PO number (`OurReference`). Do not create a separate outward report-number sequence. Pegasus retains a separate internal immutable report/version identity only for generation idempotency, custody, provenance, corrections, approvals, and Sent-evidence linkage.

The implementation plan must not be written until these evidence prerequisites merge and are re-read:

- [[TICK-093]] — accepted versioned canonical repair specification.
- [[TICK-094]] — accepted Engineer-decision component.
- [[TICK-092]] — one consistent accepted report-input snapshot/query and deterministic payload hash.

## Parked (explicitly deferred)

- Version-specific preservation of final Sent evidence through correction belongs to [[TICK-208]].
- Addendum identity/presentation belongs to [[TICK-100]].
- Deployment scheduling, runtime health and Azure proof belong to [[PLAT-007]].
- Audit-specific process inputs remain owned by RPT-03, but Audit reuses the same physical inspection/assessment report template.
- Diminution and other unsupported renderer families remain with their owning capabilities.
