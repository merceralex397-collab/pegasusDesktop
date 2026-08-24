# Files — DOCS-002: upstream:TICK-018 · DOC-02 — Store source emails, instruction documents, images, correspondence, and reports in Box

## Change surface

| Path | Purpose / risk |
| --- | --- |
| `src/Pegasus.Core/Custody/ExternalWorkProcessing.cs` | Core policy or contract; reuse the existing business owner rather than placing policy in a host. |
| `src/Pegasus.Core/Custody/CustodyContracts.cs` | Core policy or contract; reuse the existing business owner rather than placing policy in a host. |
| `src/Pegasus.Infrastructure/Persistence/EfCaseAcceptanceStore.cs` | Persistence or provider adapter; inspect data access, transaction and failure behaviour before changing callers. |
| `src/Pegasus.Infrastructure/Persistence/EfLinkedCaseReplacementStore.cs` | Persistence or provider adapter; inspect data access, transaction and failure behaviour before changing callers. |
| `src/Pegasus.Infrastructure/Persistence/EfIntakeMutationStore.cs` | Persistence or provider adapter; inspect data access, transaction and failure behaviour before changing callers. |
| `src/Pegasus.Infrastructure/Persistence/PegasusDbContext.cs` | Persistence or provider adapter; inspect data access, transaction and failure behaviour before changing callers. |
| `src/Pegasus.Infrastructure/Persistence/EfQueuedCustodyProcessor.cs` | Persistence or provider adapter; inspect data access, transaction and failure behaviour before changing callers. |
| `src/Pegasus.Infrastructure/Custody/BoxCaseCustody.cs` | Persistence or provider adapter; inspect data access, transaction and failure behaviour before changing callers. |
| `tests/Pegasus.IntegrationTests/CustodyOutboxIntegrationTests.cs` | Focused verification surface; extend the stated success, failure and regression coverage. |
| `docs/operations.md` | Authoritative context; update only if the ticket's accepted scope explicitly calls for it. |
| `docs/frd/frd-05-documents-extraction-and-custody.md` | Authoritative context; update only if the ticket's accepted scope explicitly calls for it. |
| `tests/Pegasus.Core.Tests` | Focused verification surface; extend the stated success, failure and regression coverage. |
| `docs/capabilities.md` | Authoritative context; update only if the ticket's accepted scope explicitly calls for it. |

## Context files

- `src/Pegasus.Core/Custody/ExternalWorkProcessing.cs` — Core policy or contract; reuse the existing business owner rather than placing policy in a host.
- `src/Pegasus.Core/Custody/CustodyContracts.cs` — Core policy or contract; reuse the existing business owner rather than placing policy in a host.
- `src/Pegasus.Infrastructure/Persistence/EfCaseAcceptanceStore.cs` — Persistence or provider adapter; inspect data access, transaction and failure behaviour before changing callers.
- `src/Pegasus.Infrastructure/Persistence/EfLinkedCaseReplacementStore.cs` — Persistence or provider adapter; inspect data access, transaction and failure behaviour before changing callers.
- `src/Pegasus.Infrastructure/Persistence/EfIntakeMutationStore.cs` — Persistence or provider adapter; inspect data access, transaction and failure behaviour before changing callers.
- `src/Pegasus.Infrastructure/Persistence/PegasusDbContext.cs` — Persistence or provider adapter; inspect data access, transaction and failure behaviour before changing callers.
- `src/Pegasus.Infrastructure/Persistence/EfQueuedCustodyProcessor.cs` — Persistence or provider adapter; inspect data access, transaction and failure behaviour before changing callers.
- `src/Pegasus.Infrastructure/Custody/BoxCaseCustody.cs` — Persistence or provider adapter; inspect data access, transaction and failure behaviour before changing callers.
- `tests/Pegasus.IntegrationTests/CustodyOutboxIntegrationTests.cs` — Focused verification surface; extend the stated success, failure and regression coverage.
- `docs/operations.md` — Authoritative context; update only if the ticket's accepted scope explicitly calls for it.
- `docs/frd/frd-05-documents-extraction-and-custody.md` — Authoritative context; update only if the ticket's accepted scope explicitly calls for it.
- `tests/Pegasus.Core.Tests` — Focused verification surface; extend the stated success, failure and regression coverage.
- `docs/capabilities.md` — Authoritative context; update only if the ticket's accepted scope explicitly calls for it.

## Ripple and out-of-scope boundary

- **Azure**: no write. ⚠ **Box write** (not Azure): the live verification of step 11 writes only inside the approved disposable subtree at `docs/operations.md#approved-box-integration-test-target` and needs exact-target operator approval per `docs/runbook.md` § Live operation approval matrix. The `requires-live-approval` label stands.
- **Scope boundary**: may touch `src/Pegasus.Core/Custody/**`, `src/Pegasus.Infrastructure/Custody/**`, `src/Pegasus.Infrastructure/Persistence/EfIntakeMutationStore.cs`, `EfQueuedCustodyProcessor.cs`, the sent-evidence store, any migration this needs, and the two test projects. Must **not** re-implement report retention (that is [[DSK-07-16]]), must **not** change `src/Pegasus.Worker`, must **not** widen into DOC-02 as a whole, and must **not** give any desktop client a Box credential — retention is server-side under **L-01**.
- **Blocks / blocked by**: this ticket **blocks** [[DSK-07-11]] (its outbound seam records sent evidence as an audit record and would sign off with no Box retention behind it) and [[DSK-05-14]] (the documents-and-custody slice would claim DOC-02 parity while case correspondence never reaches Box). It is **not** blocked by [[DSK-07-05]]; the Box broker endpoints serve the desktop browser, while this retention path is server-side and already has its adapters.
- **Traps**: blob is hot staging only, so "the message is in `IntakeAssets`" is not custody; the image-custody re-arm policy in `ImageCustodyRetryPolicy` is deliberately automatic and must **not** be copied onto case-scoped correspondence custody, which FRD-05 requires to fail explicitly with a staff retry; an unknown persisted work kind must keep failing closed; and a closed case stays read-only, so a late association to a closed case must be refused rather than written.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.
