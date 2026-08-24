# Research — INTK-006: upstream:INTK-032 · Fall back safely when a third-party report format cannot be read

## Question

Record and then implement the fail-closed rule for an audit whose accompanying third-party report cannot be read: an unrecognised report format must produce a clear, actionable operator state rather than a silent partial extraction or an invented value. The exact operator-visible outcome is **deliberately deferred to the operator** and must be recorded before implementation starts.

## Evidence examined

- Import decision: `coverage-decision.md` § Import list — the row for upstream `INTK-032` (this ticket; board `INTK-006`) ("enter at Backlog with `docs_todo: true` since its operator-visible state is still undefined"); § Plan gaps — "Three server-side domain requirements have no register at all"
- Carry-over register: `docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md:158` — the row for upstream `INTK-032`, quoted as it stands (its first cell is an upstream id): `INTK-032 | intake-processing | backlog | feature | qdos26009, extraction, audits | … | unchanged-backlog | — | intake-processing`
- Repository evidence (fork `main`, read 2026-08-24):
  - `src/Pegasus.Core/Intake/DirectProviders/Qdos/QdosMailClassificationPolicy.cs:180-215` — `EvaluateStandaloneAuditReport`, the `null` return that conflates "not an audit" with "outcome unreadable"; `:223-229` — `ContainsRepairable` / `ContainsTotalLoss` and their negation guards
  - `src/Pegasus.Core/Intake/Classification/MailClassificationContracts.cs:240` — the classification record carrying `AuditAssessment`
  - `src/Pegasus.Core/Cases/CaseContracts.cs:37-41` and `:93-108` — `AuditAssessment` and the immutable `a.` / `ap.` prefix allocation
  - `src/Pegasus.Core/Intake/AcceptIntake.cs:61-73` and `src/Pegasus.Core/Intake/IntakeAllocation.cs:61`, `:270`, `:296`, `:444` — `StandaloneAuditEvidenceId` flowing through acceptance and allocation; this is the path that must fail closed
  - `src/Pegasus.Web/Presentation/OperatorLabels.cs` — where any new operator-visible state gets its word
  - `src/Pegasus.Core/Intake/IntakeDecisionPolicy.cs` and `src/Pegasus.Core/Intake/ProcessIntake.cs` — the decision vocabulary a new state would join
- Binding decisions: **L-01** the gateway is `Pegasus.Web` evolved in place, so one new state serves both the Razor era and the desktop; **L-02** verification is the local production-mimicking stack; **L-05** the fork board is the single work register; **D-001** upstream is frozen after the final sync
- Depends on: the imported `upstream:INTK-031` — the issuer survey defines the abstention this ticket turns into an operator state; and `DSK-01-10`, the first one-way upstream sync
- Sequencing against the board: this must land before [[DSK-03-10]] step 11 commits the OpenAPI snapshot, or the new state has no place in the published contract.

## Known unresolved operator decision

The fail-closed outcome for an unreadable third-party audit report is deliberately not chosen. Preserve that uncertainty: no state name, operator wording, or allocation behaviour may be assumed. Before implementation, the operator’s selected behaviour and wording must be recorded; this ticket remains in Preparing until then.

## Scope and constraints

The fact this rule guards is immutable. `AuditIdentity.Create` (`src/Pegasus.Core/Cases/CaseContracts.cs:93-108`) turns `AuditAssessment.Repairable` into the reference prefix `a.` and `AuditAssessment.TotalLoss` into `ap.`, and a case reference cannot be corrected once allocated. So a report whose outcome cannot be read does not degrade a field — it blocks allocation, and guessing is unrecoverable.

Today the failure is silent by construction. `QdosMailClassificationPolicy.EvaluateStandaloneAuditReport` (`src/Pegasus.Core/Intake/DirectProviders/Qdos/QdosMailClassificationPolicy.cs:180-215`) returns `null` when it cannot resolve exactly one outcome from exactly one non-instruction attachment, and the caller cannot distinguish that from "this message is not a standalone audit". An unreadable report and an ordinary non-audit e-mail therefore look identical.

The desktop conversion is where that becomes visible and permanent. [[DSK-05-09]] renders "blocked and withheld states" with approved necessary copy and [[DSK-03-10]] step 3 publishes the received-item detail DTO with "receipt, evidence, suggestions, drafts, OCR-required state", then step 11 commits the OpenAPI snapshot and the generated client. If the new state is not in that DTO before the snapshot is pinned, the desktop has no vocabulary for it and [[DSK-05-04]]'s create path has nothing to show the operator when a reference cannot be allocated. No board ticket covers this: searches across the 208 seeded bodies for `extraction` and `issuer` return nothing.

And no register holds it. The carry-over disposition is `unchanged-backlog`, which `docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md` § Disposition categories justifies because "their capability rows stay in `docs/capabilities.md`" — there is **no** `docs/capabilities.md` row for upstream INTK-032, and no `capability`, `post-alpha` or `blocked` label on it. Under **L-05** the fork board is the single work register, so it is imported at Backlog with `docs_todo` true and its open operator question recorded, exactly as the import decision directs.

- Future owner: Core/Infrastructure intake work with focused Core and integration tests. The local Test/UAT stack is the verification environment; no production intake or Azure write is authorized.

- **Azure**: no write.
- **Scope boundary**: may touch `src/Pegasus.Core/Intake/`, `src/Pegasus.Core/Cases/CaseContracts.cs` (read only — the prefix rule is not changed here), `src/Pegasus.Web/Presentation/OperatorLabels.cs`, `tests/Pegasus.Core.Tests/`, `tests/Pegasus.IntegrationTests/` and the named documents. Must **not** touch `src/Pegasus.Web/Api/**` (that is [[DSK-03-10]]'s), any desktop project, or `src/Pegasus.Web/Pages/**` beyond reading it.
- **Unblocks / blocked by**: this ticket **blocks** [[DSK-05-09]] (which renders the blocked and withheld states and would have no vocabulary for this one) and [[DSK-03-10]] (whose detail DTO and committed OpenAPI snapshot would freeze without it). It is **blocked by** the imported `upstream:INTK-031`, whose abstention contract this turns into an operator state, and by [[DSK-01-10]], the first one-way upstream sync, and by the operator's deferred decision at step 5. [[DSK-05-04]] shows the consequence when no reference can be allocated — coordinate the copy with it and with [[DSK-06-16]] rather than inventing a second wording.
- **Traps**: never invent a prefix — `a.` and `ap.` are immutable once allocated and there is no correction path. Do not design the operator-visible outcome without the operator; the deferral is deliberate and recorded. Do not add a fourth copy of the intake decision-code table — the imported `upstream:INTK-002` is collapsing the existing three. **Upstream ids and fork board ids do not match**: this ticket is board `INTK-006` and it is upstream INTK-032; upstream INTK-006 has **no fork ticket** and is not on this board, and the sibling survey is upstream INTK-031, board [[INTK-005]]. Read the join table in `HZN-001/board-conventions.md` § Upstream ids versus board ids; never compute the mapping. `qdos26009` is an upstream case label, not a fork concept; the rule is keyed by report readability, not by principal. Banned operator words (`intake`, `artifact`, `extraction`) must not reach the new state's copy.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the ticket `plan` document.

## Governing documents

- No canonical document is linked yet. Retain the ticket's existing `docs_todo` state; do not invent or link a proposed desktop ADR.

## Planning implication

Reuse the cited boundaries and revalidate the named sources against current `origin/dev` after the ticket is taken. Do not create a compatibility path, duplicate policy, or an unapproved external write.
