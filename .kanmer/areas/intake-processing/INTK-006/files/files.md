# Files — INTK-006: upstream:INTK-032 · Fall back safely when a third-party report format cannot be read

## Change surface

| Path | Purpose / risk |
| --- | --- |
| `docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md` | Authoritative context; update only if the ticket's accepted scope explicitly calls for it. |
| `src/Pegasus.Core/Intake/DirectProviders/Qdos/QdosMailClassificationPolicy.cs` | Core policy or contract; reuse the existing business owner rather than placing policy in a host. |
| `src/Pegasus.Core/Intake/Classification/MailClassificationContracts.cs` | Core policy or contract; reuse the existing business owner rather than placing policy in a host. |
| `src/Pegasus.Core/Cases/CaseContracts.cs` | Core policy or contract; reuse the existing business owner rather than placing policy in a host. |
| `src/Pegasus.Core/Intake/AcceptIntake.cs` | Core policy or contract; reuse the existing business owner rather than placing policy in a host. |
| `src/Pegasus.Core/Intake/IntakeAllocation.cs` | Core policy or contract; reuse the existing business owner rather than placing policy in a host. |
| `src/Pegasus.Web/Presentation/OperatorLabels.cs` | Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core. |
| `src/Pegasus.Core/Intake/IntakeDecisionPolicy.cs` | Core policy or contract; reuse the existing business owner rather than placing policy in a host. |
| `src/Pegasus.Core/Intake/ProcessIntake.cs` | Core policy or contract; reuse the existing business owner rather than placing policy in a host. |
| `openapi/pegasus-v1.json` | Versioned HTTP contract snapshot; change only with matching contract-test and client-generation evidence. |
| `docs/capabilities.md` | Authoritative context; update only if the ticket's accepted scope explicitly calls for it. |
| `docs/frd/frd-02-intake-and-source-identity.md` | Authoritative context; update only if the ticket's accepted scope explicitly calls for it. |
| `docs/frd/frd-09-provider-and-intermediary-routes.md` | Authoritative context; update only if the ticket's accepted scope explicitly calls for it. |
| `tests/Pegasus.Core.Tests/Pegasus.Core.Tests.csproj` | Focused verification surface; extend the stated success, failure and regression coverage. |
| `tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj` | Focused verification surface; extend the stated success, failure and regression coverage. |

## Context files

- `docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md` — Authoritative context; update only if the ticket's accepted scope explicitly calls for it.
- `src/Pegasus.Core/Intake/DirectProviders/Qdos/QdosMailClassificationPolicy.cs` — Core policy or contract; reuse the existing business owner rather than placing policy in a host.
- `src/Pegasus.Core/Intake/Classification/MailClassificationContracts.cs` — Core policy or contract; reuse the existing business owner rather than placing policy in a host.
- `src/Pegasus.Core/Cases/CaseContracts.cs` — Core policy or contract; reuse the existing business owner rather than placing policy in a host.
- `src/Pegasus.Core/Intake/AcceptIntake.cs` — Core policy or contract; reuse the existing business owner rather than placing policy in a host.
- `src/Pegasus.Core/Intake/IntakeAllocation.cs` — Core policy or contract; reuse the existing business owner rather than placing policy in a host.
- `src/Pegasus.Web/Presentation/OperatorLabels.cs` — Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core.
- `src/Pegasus.Core/Intake/IntakeDecisionPolicy.cs` — Core policy or contract; reuse the existing business owner rather than placing policy in a host.
- `src/Pegasus.Core/Intake/ProcessIntake.cs` — Core policy or contract; reuse the existing business owner rather than placing policy in a host.
- `openapi/pegasus-v1.json` — Versioned HTTP contract snapshot; change only with matching contract-test and client-generation evidence.
- `docs/capabilities.md` — Authoritative context; update only if the ticket's accepted scope explicitly calls for it.
- `docs/frd/frd-02-intake-and-source-identity.md` — Authoritative context; update only if the ticket's accepted scope explicitly calls for it.
- `docs/frd/frd-09-provider-and-intermediary-routes.md` — Authoritative context; update only if the ticket's accepted scope explicitly calls for it.
- `tests/Pegasus.Core.Tests/Pegasus.Core.Tests.csproj` — Focused verification surface; extend the stated success, failure and regression coverage.

## Ripple and out-of-scope boundary

- **Azure**: no write.
- **Scope boundary**: may touch `src/Pegasus.Core/Intake/`, `src/Pegasus.Core/Cases/CaseContracts.cs` (read only — the prefix rule is not changed here), `src/Pegasus.Web/Presentation/OperatorLabels.cs`, `tests/Pegasus.Core.Tests/`, `tests/Pegasus.IntegrationTests/` and the named documents. Must **not** touch `src/Pegasus.Web/Api/**` (that is [[DSK-03-10]]'s), any desktop project, or `src/Pegasus.Web/Pages/**` beyond reading it.
- **Unblocks / blocked by**: this ticket **blocks** [[DSK-05-09]] (which renders the blocked and withheld states and would have no vocabulary for this one) and [[DSK-03-10]] (whose detail DTO and committed OpenAPI snapshot would freeze without it). It is **blocked by** the imported `upstream:INTK-031`, whose abstention contract this turns into an operator state, and by [[DSK-01-10]], the first one-way upstream sync, and by the operator's deferred decision at step 5. [[DSK-05-04]] shows the consequence when no reference can be allocated — coordinate the copy with it and with [[DSK-06-16]] rather than inventing a second wording.
- **Traps**: never invent a prefix — `a.` and `ap.` are immutable once allocated and there is no correction path. Do not design the operator-visible outcome without the operator; the deferral is deliberate and recorded. Do not add a fourth copy of the intake decision-code table — the imported `upstream:INTK-002` is collapsing the existing three. **Upstream ids and fork board ids do not match**: this ticket is board `INTK-006` and it is upstream INTK-032; upstream INTK-006 has **no fork ticket** and is not on this board, and the sibling survey is upstream INTK-031, board [[INTK-005]]. Read the join table in `HZN-001/board-conventions.md` § Upstream ids versus board ids; never compute the mapping. `qdos26009` is an upstream case label, not a fork concept; the rule is keyed by report readability, not by principal. Banned operator words (`intake`, `artifact`, `extraction`) must not reach the new state's copy.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the ticket `plan` document.
