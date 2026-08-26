# Plan — INTK-006: upstream:INTK-032 · Fall back safely when a third-party report format cannot be read

## Governing documents

- No canonical document is linked yet. Retain the ticket's existing `docs_todo` state; do not invent or link a proposed desktop ADR.

## Operator-decision hold

Do not implement the fallback until the operator has chosen and recorded its exact visible outcome. This plan preserves the question rather than resolving it by inference; the next implementation agent must add the required unticked question gate before asking to move the ticket beyond Preparing.

## Chosen approach

Record and then implement the fail-closed rule for an audit whose accompanying third-party report cannot be read: an unrecognised report format must produce a clear, actionable operator state rather than a silent partial extraction or an invented value. The exact operator-visible outcome is **deliberately deferred to the operator** and must be recorded before implementation starts.

## Routing and constraints

- Future owner: Core/Infrastructure intake work with focused Core and integration tests. The local Test/UAT stack is the verification environment; no production intake or Azure write is authorized.


## Ordered implementation steps

1. Orient. Read the verbatim upstream body above, the imported `upstream:INTK-031` and its `research` once it exists, and `coverage-decision.md` § Import list row for upstream `INTK-032`. Call `get_doc_gates <this ticket id>`, then `take_ticket` with branch `task/upstream-intk-032-unreadable-report-fallback` and worktree `../pegasus-worktrees/upstream-intk-032-unreadable-report-fallback` from `origin/dev`.
2. **Do not start implementing.** The upstream § Scope for now says the outcome design is deliberately deferred and § How to verify says "To be defined with the operator before implementation starts". Write that into the ticket's `open-questions` document as an unticked item before anything else, so the stage gate holds the ticket until it is answered. An unticked open question blocking the move is the correct and honest state.
3. In `research`, establish the current behaviour precisely: `EvaluateStandaloneAuditReport` (`src/Pegasus.Core/Intake/DirectProviders/Qdos/QdosMailClassificationPolicy.cs:180-215`) returns `null` for **both** "this is not a standalone audit" and "the report's outcome could not be read", and record which callers consume that `null` and what each does with it.
4. In `research`, follow the consequence to allocation: `src/Pegasus.Core/Intake/AcceptIntake.cs:61-73` refuses an `Audit` case type without a `StandaloneAuditEvidenceId`, and `AuditIdentity.Create` (`src/Pegasus.Core/Cases/CaseContracts.cs:93-108`) allocates the immutable `a.` / `ap.` prefix. Show, with line references, why an invented outcome is unrecoverable and a partial extraction is worse than a refusal.
5. **Operator step.** Put the deferred decision to the operator as a small number of concrete options drawn from the code, not as an open essay — for example: (i) the receipt reaches a named blocked state with a reason and no case, (ii) the receipt reaches a named needs-attention state with the readable facts retained and the outcome absent, (iii) allocation proceeds only after a staff member confirms the outcome from the report. Record the operator's chosen behaviour, the exact operator-visible wording and the date, in `research` and in `docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md`. Evidence handed back: the chosen option and its wording. Then tick the `open-questions` item.
6. Implement the chosen behaviour fail-closed in Core. The new state joins the existing decision vocabulary (`src/Pegasus.Core/Intake/IntakeDecisionPolicy.cs`, `src/Pegasus.Core/Intake/ProcessIntake.cs`) rather than becoming a parallel flag, and its operator word joins `src/Pegasus.Web/Presentation/OperatorLabels.cs` — not a second label table. If a persisted decision code is added, coordinate with the imported `upstream:INTK-002`, which collapses the three existing copies of that code table onto one; adding a fourth copy here would undo it.
7. Distinguish the two cases the current `null` conflates: "not a standalone audit" continues to behave exactly as today, and "issuer known or unknown but outcome unreadable" reaches the new state. Use the abstention contract the imported `upstream:INTK-031` specifies rather than inventing a second one.
8. **Re-expressed for the desktop world.** The upstream body assumes the operator meets this on the Razor Received-item page that [[DSK-05-26]]'s cut list deletes. State the requirement against what replaces it and record it in the `plan`: the new state and its reason travel in [[DSK-03-10]]'s received-item detail DTO beside the existing OCR-required state, so [[DSK-05-09]] renders it with approved necessary copy and [[DSK-05-04]] can tell the operator why no reference can be allocated. It must be in the DTO **before** [[DSK-03-10]] step 11 commits `openapi/pegasus-v1.json` and the generated client.
9. Add tests: an audit whose report states neither outcome reaches the new state and allocates no reference; an audit whose report states both is still refused as ambiguous by the existing `:206-213` logic; a genuine non-audit message is unchanged; a readable audit still allocates the correct `a.` or `ap.` prefix; and the readable facts that could be extracted are retained rather than discarded.
10. Add the `docs/capabilities.md` row this requirement has never had, and record the chosen behaviour in `docs/frd/frd-02-intake-and-source-identity.md` (and `docs/frd/frd-09-provider-and-intermediary-routes.md` if the operator's answer is route-specific — decide, do not assume).
11. Run the simplification pass over the branch diff, record it under a dated `## Simplification pass` heading in the ticket `plan`, then open the PR into `dev`.

## Acceptance conditions

- [ ] The operator's chosen behaviour, its exact operator-visible wording and the date are recorded in `research` and in the carry-over register **before** any implementation, and the `open-questions` item is ticked by that answer rather than by assumption.
- [ ] An audit whose report's outcome cannot be read produces a clear, actionable operator state — never a silent partial extraction, never an invented prefix, never a case with a guessed reference.
- [ ] "Not a standalone audit" and "outcome unreadable" are distinguishable outcomes; the first behaves exactly as today.
- [ ] Whatever facts *could* be extracted are retained; failing closed on the outcome does not discard the rest.
- [ ] The new state travels in [[DSK-03-10]]'s received-item DTO before its OpenAPI snapshot is committed, and its operator word lives in `OperatorLabels` — one vocabulary, not two.
- [ ] A readable audit still allocates the correct `a.` or `ap.` prefix, unchanged.
- [ ] `docs/capabilities.md` carries a row for this requirement.

## Verification

- [ ] `dotnet build --configuration Release` — expected: clean.
- [ ] `dotnet test ./tests/Pegasus.Core.Tests/Pegasus.Core.Tests.csproj --configuration Release --filter "FullyQualifiedName~Qdos"` — expected: unreadable-outcome, both-outcomes-ambiguous, non-audit-unchanged and readable-audit-prefix facts all pass.
- [ ] `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --filter "Category!=Corpus&Category!=Browser"` — expected: no regression in allocation or acceptance.
- [ ] `get_doc_gates <this ticket id>` before the move out of Preparing — expected: blocked while the `open-questions` item is unticked; that is the intended behaviour, not a failure.

## Risks and boundaries

- **Azure**: no write.
- **Scope boundary**: may touch `src/Pegasus.Core/Intake/`, `src/Pegasus.Core/Cases/CaseContracts.cs` (read only — the prefix rule is not changed here), `src/Pegasus.Web/Presentation/OperatorLabels.cs`, `tests/Pegasus.Core.Tests/`, `tests/Pegasus.IntegrationTests/` and the named documents. Must **not** touch `src/Pegasus.Web/Api/**` (that is [[DSK-03-10]]'s), any desktop project, or `src/Pegasus.Web/Pages/**` beyond reading it.
- **Unblocks / blocked by**: this ticket **blocks** [[DSK-05-09]] (which renders the blocked and withheld states and would have no vocabulary for this one) and [[DSK-03-10]] (whose detail DTO and committed OpenAPI snapshot would freeze without it). It is **blocked by** the imported `upstream:INTK-031`, whose abstention contract this turns into an operator state, and by [[DSK-01-10]], the first one-way upstream sync, and by the operator's deferred decision at step 5. [[DSK-05-04]] shows the consequence when no reference can be allocated — coordinate the copy with it and with [[DSK-06-16]] rather than inventing a second wording.
- **Traps**: never invent a prefix — `a.` and `ap.` are immutable once allocated and there is no correction path. Do not design the operator-visible outcome without the operator; the deferral is deliberate and recorded. Do not add a fourth copy of the intake decision-code table — the imported `upstream:INTK-002` is collapsing the existing three. **Upstream ids and fork board ids do not match**: this ticket is board `INTK-006` and it is upstream INTK-032; upstream INTK-006 has **no fork ticket** and is not on this board, and the sibling survey is upstream INTK-031, board [[INTK-005]]. Read the join table in `HZN-001/board-conventions.md` § Upstream ids versus board ids; never compute the mapping. `qdos26009` is an upstream case label, not a fork concept; the rule is keyed by report readability, not by principal. Banned operator words (`intake`, `artifact`, `extraction`) must not reach the new state's copy.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the ticket `plan` document.

## Fork boundary amendment — 2026-08-26

The earlier plan language naming a future first one-way upstream sync as a dependency is superseded. The repository instruction is no upstream synchronization: this ticket is implemented only in the fork, from the current `origin/dev` head, with INTK-005's completed research used as read-only provenance. No upstream fetch, merge, push, or dependency handback is required. The operator-visible outcome remains unresolved and is the only product decision that may authorize implementation.
