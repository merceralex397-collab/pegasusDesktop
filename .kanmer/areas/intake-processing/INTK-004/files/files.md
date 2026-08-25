# Files — INTK-004: upstream:INTK-027 · Make policy re-evaluation work after transient staging cleanup

## Change surface

| Path | Purpose / risk |
| --- | --- |
| `docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md` | Authoritative context; update only if the ticket's accepted scope explicitly calls for it. |
| `docs/frd/frd-02-intake-and-source-identity.md` | Authoritative context; update only if the ticket's accepted scope explicitly calls for it. |
| `docs/desktop/03-gateway-api-and-data/endpoint-map.md` | Authoritative context; update only if the ticket's accepted scope explicitly calls for it. |
| `src/Pegasus.Core/Intake/DurableIntake.cs` | Core policy or contract; reuse the existing business owner rather than placing policy in a host. |
| `src/Pegasus.Core/Intake/IntakeContracts.cs` | Core policy or contract; reuse the existing business owner rather than placing policy in a host. |
| `src/Pegasus.Infrastructure/Persistence/EfIntakeMutationStore.cs` | Persistence or provider adapter; inspect data access, transaction and failure behaviour before changing callers. |
| `src/Pegasus.Web/Presentation/OperatorLabels.cs` | Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core. |
| `src/Pegasus.Core/Intake/DownloadIntakeSource.cs` | Core policy or contract; reuse the existing business owner rather than placing policy in a host. |
| `src/Pegasus.Web/Program.cs` | Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core. |
| `src/Pegasus.Worker/WorkerAzureClientFactory.cs` | Unattended worker composition; retain central execution and protected credentials. |
| `src/Pegasus.Web/Pages/Intake/Details.cshtml.cs` | Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core. |
| `tests/Pegasus.Core.Tests/Pegasus.Core.Tests.csproj` | Focused verification surface; extend the stated success, failure and regression coverage. |
| `tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj` | Focused verification surface; extend the stated success, failure and regression coverage. |

## Context files

- `docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md` — Authoritative context; update only if the ticket's accepted scope explicitly calls for it.
- `docs/frd/frd-02-intake-and-source-identity.md` — Authoritative context; update only if the ticket's accepted scope explicitly calls for it.
- `docs/desktop/03-gateway-api-and-data/endpoint-map.md` — Authoritative context; update only if the ticket's accepted scope explicitly calls for it.
- `src/Pegasus.Core/Intake/DurableIntake.cs` — Core policy or contract; reuse the existing business owner rather than placing policy in a host.
- `src/Pegasus.Core/Intake/IntakeContracts.cs` — Core policy or contract; reuse the existing business owner rather than placing policy in a host.
- `src/Pegasus.Infrastructure/Persistence/EfIntakeMutationStore.cs` — Persistence or provider adapter; inspect data access, transaction and failure behaviour before changing callers.
- `src/Pegasus.Web/Presentation/OperatorLabels.cs` — Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core.
- `src/Pegasus.Core/Intake/DownloadIntakeSource.cs` — Core policy or contract; reuse the existing business owner rather than placing policy in a host.
- `src/Pegasus.Web/Program.cs` — Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core.
- `src/Pegasus.Worker/WorkerAzureClientFactory.cs` — Unattended worker composition; retain central execution and protected credentials.
- `src/Pegasus.Web/Pages/Intake/Details.cshtml.cs` — Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core.
- `tests/Pegasus.Core.Tests/Pegasus.Core.Tests.csproj` — Focused verification surface; extend the stated success, failure and regression coverage.
- `tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj` — Focused verification surface; extend the stated success, failure and regression coverage.

## Ripple and out-of-scope boundary

- **Azure**: no write. Reading the `transient-intake` container to confirm the empty `staging/` prefix is a read and is fully permitted with no per-target approval (`docs/runbook.md` § Live-operation approval matrix; mirrored in `docs/desktop/11-azure-disposition/README.md`). Re-staging a blob **in production** would be a write and is explicitly **not** part of this ticket — the code change is; any live remediation of already-stranded receipts is a separate approved operation.
- **Scope boundary**: may touch `src/Pegasus.Core/Intake/DurableIntake.cs`, `src/Pegasus.Core/Intake/IntakeContracts.cs`, `src/Pegasus.Infrastructure/Persistence/EfIntakeMutationStore.cs`, `src/Pegasus.Web/Presentation/OperatorLabels.cs`, the three test projects and the named documents. Must **not** touch `src/Pegasus.Web/Api/**` (that is [[DSK-03-10]]'s), any desktop project, or `src/Pegasus.Web/Pages/Intake/**` beyond reading it.
- **Unblocks / blocked by**: this ticket **blocks** [[DSK-05-09]] and [[DSK-03-10]] — they publish and render a command that cannot succeed today, and both are forbidden by their own scope boundaries from repairing it. It is **blocked by** [[DSK-01-10]], the first one-way upstream sync. [[DSK-05-23]] and [[DSK-03-16]] carry the operator label vocabulary any new refusal reason joins.
- **Traps**: **upstream ids and fork board ids do not match.** This ticket is board `INTK-004` and it is upstream INTK-027; upstream INTK-004 is a different ticket again — the received-intake Case-link and label defect absorbed into [[DSK-05-20]] and [[DSK-05-23]] — and it has **no fork ticket**, so never read a bare `INTK-004` as it. The join table is `HZN-001/board-conventions.md` § Upstream ids versus board ids: read it, never compute the mapping, and write `upstream <ID>`, or `upstream <ID> (board [[<board-id>]])` where both are meant. The fix is in `Pegasus.Infrastructure`, which [[DSK-05-09]] may not touch and [[DSK-03-10]] may not touch — that is precisely why this ticket exists; do not let it drift into either. Do not weaken `IntakeArtifactIntegrityException`: a genuinely corrupt source must still fail closed. Do not delete or change `DeleteCompletedStagedAsync` — deleting the staged copy on completion is deliberate and the retained source is the durable one. `IntakeWorkItems` state strings are persisted values.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the ticket `plan` document.

## Research refresh — 2026-08-25

Verified against the current tree: the mutation adapter owns the re-evaluation queue state, the existing `IIntakeArtifactStore` owns both durable reads and canonical staging, and `IntakeReceiptEntity.Assets` carries the retained source metadata needed for validation. The implementation must not add `src/Pegasus.Web/Api/**`, desktop files, or a second source-storage port.
