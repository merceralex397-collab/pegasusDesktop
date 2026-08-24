# Files — ENG-002: upstream:ENG-015 · Export the field values EVA expects

## Change surface

| Path | Purpose / risk |
| --- | --- |
| `docs/frd/frd-07-eva-and-external-engineering-handoff.md` | Authoritative context; update only if the ticket's accepted scope explicitly calls for it. |
| `docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md` | Authoritative context; update only if the ticket's accepted scope explicitly calls for it. |
| `src/Pegasus.Infrastructure/Persistence/EvaHandoffStore.cs` | Persistence or provider adapter; inspect data access, transaction and failure behaviour before changing callers. |
| `tests/Pegasus.IntegrationTests/QdosMappingExtractionTests.cs` | Focused verification surface; extend the stated success, failure and regression coverage. |
| `src/Pegasus.Core/Eva/CaseEvaMapping.cs` | Core policy or contract; reuse the existing business owner rather than placing policy in a host. |
| `src/Pegasus.Core/Address/Ext18InspectionAddressPolicy.cs` | Core policy or contract; reuse the existing business owner rather than placing policy in a host. |
| `tests/Pegasus.Core.Tests/Qdos/` | Focused verification surface; extend the stated success, failure and regression coverage. |
| `tests/Pegasus.IntegrationTests/EvaHandoffPersistenceTests.cs` | Focused verification surface; extend the stated success, failure and regression coverage. |
| `src/Pegasus.Core/Cases/CaseDataContracts.cs` | Core policy or contract; reuse the existing business owner rather than placing policy in a host. |
| `src/Pegasus.Core/Intake/DirectProviders/Qdos/QdosInstructionExtractionPolicy.cs` | Core policy or contract; reuse the existing business owner rather than placing policy in a host. |
| `docs/json-extraction-parity/ap.QDOS26015/old-extraction-working/QDOS_NX14AXY.json` | Authoritative context; update only if the ticket's accepted scope explicitly calls for it. |

## Context files

- `docs/frd/frd-07-eva-and-external-engineering-handoff.md` — Authoritative context; update only if the ticket's accepted scope explicitly calls for it.
- `docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md` — Authoritative context; update only if the ticket's accepted scope explicitly calls for it.
- `src/Pegasus.Infrastructure/Persistence/EvaHandoffStore.cs` — Persistence or provider adapter; inspect data access, transaction and failure behaviour before changing callers.
- `tests/Pegasus.IntegrationTests/QdosMappingExtractionTests.cs` — Focused verification surface; extend the stated success, failure and regression coverage.
- `src/Pegasus.Core/Eva/CaseEvaMapping.cs` — Core policy or contract; reuse the existing business owner rather than placing policy in a host.
- `src/Pegasus.Core/Address/Ext18InspectionAddressPolicy.cs` — Core policy or contract; reuse the existing business owner rather than placing policy in a host.
- `tests/Pegasus.Core.Tests/Qdos/` — Focused verification surface; extend the stated success, failure and regression coverage.
- `tests/Pegasus.IntegrationTests/EvaHandoffPersistenceTests.cs` — Focused verification surface; extend the stated success, failure and regression coverage.
- `src/Pegasus.Core/Cases/CaseDataContracts.cs` — Core policy or contract; reuse the existing business owner rather than placing policy in a host.
- `src/Pegasus.Core/Intake/DirectProviders/Qdos/QdosInstructionExtractionPolicy.cs` — Core policy or contract; reuse the existing business owner rather than placing policy in a host.
- `docs/json-extraction-parity/ap.QDOS26015/old-extraction-working/QDOS_NX14AXY.json` — Authoritative context; update only if the ticket's accepted scope explicitly calls for it.

## Ripple and out-of-scope boundary

- **Azure**: no write, and no Azure read — this ticket has no cloud surface. Verification is the local stack and the retained corpus under **L-02**.
- **Scope boundary**: may touch `src/Pegasus.Infrastructure/Persistence/EvaHandoffStore.cs` (the mapping only), `src/Pegasus.Core/Eva/CaseEvaMapping.cs`, the EVA and QDOS test files, and `docs/frd/frd-07-eva-and-external-engineering-handoff.md`. Must **not** change `CaseEvaMapping.ImageBasedAssessment` or `src/Pegasus.Core/Address/Ext18InspectionAddressPolicy.cs:12` (both are comparison targets for the case's stored value), the thirteen keys or their order, `EvaBundleSchema`'s packaging (the imported `upstream:ENG-014` owns it), any Razor page model, or any desktop project.
- **Blocking**: this **unblocks** [[DSK-05-15]] (FEAT-015), whose thirteen-field acceptance criterion cannot pass while four values are wrong, and through it [[DSK-05-22]], [[DSK-05-25]], [[DSK-07-18]], [[DSK-08-08]] and [[DSK-08-16]]. It is **blocked by** the imported `upstream:ENG-014` (sequence ENG-014 then ENG-015 so the archive bytes change once) and follows [[DSK-01-10]]'s sync. [[DSK-01-09]] (FND-022) assigns its phase; do not invent one here.
- **Open questions carried from upstream** (recorded here, not answered in code): (a) **Mileage Unit casing and CRLF** — the samples show `Miles`/`Km` and CRLF but may be predecessor artefacts; step 8 settles it by an actual EVA import or the operator's answer. (b) **`Reference` fail-closed behaviour** — step 4's operator decision; this one is new to the fork and must not be skipped. (c) **Accident Circumstances** — whether damage-area text should feed the key when no circumstances prose exists (`QdosInstructionExtractionPolicy.cs:317`/`:340`) is a business rule and is **out of scope**; raise its own ticket. (d) **Instruction Date** — the bare `Date:` label absent from `QdosInstructionExtractionPolicy.cs:49-51` makes every QDOS case default to the receipt date; **out of scope**, likely an intake ticket. (e) **VAT Status** — confirm with the operator that it is meant to be staff-entered rather than derived; recording the answer is enough, no code follows.
- **Traps**: the fork is behind upstream, so every line number in the upstream body is upstream's — re-derive them. The `Reference` change makes a previously-always-present value optional; that is the one place this ticket can break generation for a real case. `NormalizeValue`'s `.Trim()` will silently eat the address padding unless the exemption is explicit. The samples are evidence of shape, not a specification — the casing question is settled by an import, not by copying.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.
