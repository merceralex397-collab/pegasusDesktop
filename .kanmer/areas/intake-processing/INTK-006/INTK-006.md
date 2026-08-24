---
id: INTK-006
type: ticket
title: >-
  upstream:INTK-032 · Fall back safely when a third-party report format cannot
  be read
status: backlog
area: intake-processing
assignee: ''
profile: feature
labels:
  - qdos26009
  - extraction
  - audits
  - upstream-carryover
  - upstream-INTK-032
  - needs-operator
groups:
  - EPIC-014
links: []
blocks:
  - FEAT-009
  - GWY-010
docs_todo: true
archived: false
created: '2026-08-24T11:50:33.940Z'
updated: '2026-08-24T13:36:34.709Z'
---

## What

Record and then implement the fail-closed rule for an audit whose accompanying third-party report cannot be read: an unrecognised report format must produce a clear, actionable operator state rather than a silent partial extraction or an invented value. The exact operator-visible outcome is **deliberately deferred to the operator** and must be recorded before implementation starts.

## Why

The fact this rule guards is immutable. `AuditIdentity.Create` (`src/Pegasus.Core/Cases/CaseContracts.cs:93-108`) turns `AuditAssessment.Repairable` into the reference prefix `a.` and `AuditAssessment.TotalLoss` into `ap.`, and a case reference cannot be corrected once allocated. So a report whose outcome cannot be read does not degrade a field — it blocks allocation, and guessing is unrecoverable.

Today the failure is silent by construction. `QdosMailClassificationPolicy.EvaluateStandaloneAuditReport` (`src/Pegasus.Core/Intake/DirectProviders/Qdos/QdosMailClassificationPolicy.cs:180-215`) returns `null` when it cannot resolve exactly one outcome from exactly one non-instruction attachment, and the caller cannot distinguish that from "this message is not a standalone audit". An unreadable report and an ordinary non-audit e-mail therefore look identical.

The desktop conversion is where that becomes visible and permanent. [[DSK-05-09]] renders "blocked and withheld states" with approved necessary copy and [[DSK-03-10]] step 3 publishes the received-item detail DTO with "receipt, evidence, suggestions, drafts, OCR-required state", then step 11 commits the OpenAPI snapshot and the generated client. If the new state is not in that DTO before the snapshot is pinned, the desktop has no vocabulary for it and [[DSK-05-04]]'s create path has nothing to show the operator when a reference cannot be allocated. No board ticket covers this: searches across the 208 seeded bodies for `extraction` and `issuer` return nothing.

And no register holds it. The carry-over disposition is `unchanged-backlog`, which `docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md` § Disposition categories justifies because "their capability rows stay in `docs/capabilities.md`" — there is **no** `docs/capabilities.md` row for upstream INTK-032, and no `capability`, `post-alpha` or `blocked` label on it. Under **L-05** the fork board is the single work register, so it is imported at Backlog with `docs_todo` true and its open operator question recorded, exactly as the import decision directs.

## Source of truth

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

### Upstream ticket INTK-032 (verbatim)

Provenance — upstream area `intake-processing`; upstream status `backlog`; upstream profile `feature`; upstream labels `qdos26009`, `extraction`, `audits`; upstream `docs_todo` true; upstream `deployment` `not-deployed`. Read from the read-only clone of `collisionengineers/pegasus` branch `kanmer-board` at commit `a5b28111`, read date **2026-08-24**. Copied unedited.

````
## Why — operator direction (2026-08-22)

> "add a new ticket accounting for situations where we cannot extract details from the third party engineer report e.g. we've not had that format in before — will plan in more detail the outcome on this specific ticket at a later time."

The original report accompanying an audit instruction is written by a different engineering firm each time. [[INTK-031]] builds the issuer corpus so known layouts are recognised; this ticket owns what happens for the ones that are **not**.

## Why it matters more than it looks

The audit reference prefix depends on whether the report says Repairable or Total Loss. If that fact cannot be read, the case cannot be given its reference — so an unreadable report is not a cosmetic gap, it blocks allocation. Fail closed rather than guessing a prefix that is immutable once allocated.

## Scope for now

The outcome design is **deliberately deferred** — the operator will plan the exact behaviour on this ticket later. What this ticket records now is the requirement: an unrecognised report format must produce a clear, actionable operator state rather than a silent partial extraction or an invented value.

Related: [[INTK-031]] (issuer corpus and identification).

## How to verify

To be defined with the operator before implementation starts.
````

## Routing

- **Subagent**: `pegasus-gateway-dev` — `.codex/agents/pegasus-gateway-dev.toml`; tests by `pegasus-test-engineer` — `.codex/agents/pegasus-test-engineer.toml`; the operator conversation is run by whichever agent holds the ticket
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `kanmer-research` (`.grok/skills/kanmer-research/SKILL.md`) → `code-testing-agent` (dotnet/skills `98f84851`) → `run-tests` (dotnet/skills `98f84851`)
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`)
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (call `get_doc_gates <id>` before every move; the `open-questions` document below will block the move out of Preparing until the operator's answer ticks it, and that is correct)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

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

## Acceptance criteria

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

## Evidence tier

Tier 2 — Core/domain. Tier 3 — Parser/adapter contracts.
Tier 2 obliges positive, contradictory, ambiguous and failure cases for the outcome rule and the allocation refusal; tier 3 obliges deterministic, stable contract codes for the unreadable case and evidence that the failure is deterministic rather than a best guess.

## Documentation changes

- `docs/capabilities.md` — add the row this requirement has never had; its absence is what made `unchanged-backlog` unsafe for it
- `docs/frd/frd-02-intake-and-source-identity.md` — the fail-closed rule and the new operator state
- `docs/frd/frd-09-provider-and-intermediary-routes.md` — only if the operator's answer is route-specific; decide and record
- `docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md` — annotate the upstream `INTK-032` row with this fork ticket id (`INTK-006`) and record the operator's answer with its date

## Guardrails

- **Azure**: no write.
- **Scope boundary**: may touch `src/Pegasus.Core/Intake/`, `src/Pegasus.Core/Cases/CaseContracts.cs` (read only — the prefix rule is not changed here), `src/Pegasus.Web/Presentation/OperatorLabels.cs`, `tests/Pegasus.Core.Tests/`, `tests/Pegasus.IntegrationTests/` and the named documents. Must **not** touch `src/Pegasus.Web/Api/**` (that is [[DSK-03-10]]'s), any desktop project, or `src/Pegasus.Web/Pages/**` beyond reading it.
- **Unblocks / blocked by**: this ticket **blocks** [[DSK-05-09]] (which renders the blocked and withheld states and would have no vocabulary for this one) and [[DSK-03-10]] (whose detail DTO and committed OpenAPI snapshot would freeze without it). It is **blocked by** the imported `upstream:INTK-031`, whose abstention contract this turns into an operator state, and by [[DSK-01-10]], the first one-way upstream sync, and by the operator's deferred decision at step 5. [[DSK-05-04]] shows the consequence when no reference can be allocated — coordinate the copy with it and with [[DSK-06-16]] rather than inventing a second wording.
- **Traps**: never invent a prefix — `a.` and `ap.` are immutable once allocated and there is no correction path. Do not design the operator-visible outcome without the operator; the deferral is deliberate and recorded. Do not add a fourth copy of the intake decision-code table — the imported `upstream:INTK-002` is collapsing the existing three. **Upstream ids and fork board ids do not match**: this ticket is board `INTK-006` and it is upstream INTK-032; upstream INTK-006 has **no fork ticket** and is not on this board, and the sibling survey is upstream INTK-031, board [[INTK-005]]. Read the join table in `HZN-001/board-conventions.md` § Upstream ids versus board ids; never compute the mapping. `qdos26009` is an upstream case label, not a fork concept; the rule is keyed by report readability, not by principal. Banned operator words (`intake`, `artifact`, `extraction`) must not reach the new state's copy.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the ticket `plan` document.

## Outcome

_Filled at closeout._
