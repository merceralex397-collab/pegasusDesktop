# Files — GWY-008: DSK-03-08 · Case command endpoints: create, save, lease, completeness, workflow and closure

## Change surface

| Path | Purpose / risk |
| --- | --- |
| `docs/desktop/03-gateway-api-and-data/README.md` | Authoritative context; update only if the ticket's accepted scope explicitly calls for it. |
| `docs/desktop/03-gateway-api-and-data/endpoint-map.md` | Authoritative context; update only if the ticket's accepted scope explicitly calls for it. |
| `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` | Authoritative context; update only if the ticket's accepted scope explicitly calls for it. |
| `src/Pegasus.Core/Lifecycle/CaseLifecycle.cs` | Core policy or contract; reuse the existing business owner rather than placing policy in a host. |
| `src/Pegasus.Web/Pages/Cases/Workflow.cshtml.cs` | Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core. |
| `src/Pegasus.Web/Pages/Cases/Closure.cshtml.cs` | Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core. |
| `src/Pegasus.Web/Pages/Cases/Details.cshtml.cs` | Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core. |
| `src/Pegasus.Core/Workflow/CaseWorkflowContracts.cs` | Core policy or contract; reuse the existing business owner rather than placing policy in a host. |
| `src/Pegasus.Core/Workflow/CaseCommandContracts.cs` | Core policy or contract; reuse the existing business owner rather than placing policy in a host. |
| `src/Pegasus.Core/Workflow/CaseEditAuthority.cs` | Core policy or contract; reuse the existing business owner rather than placing policy in a host. |
| `src/Pegasus.Infrastructure/Persistence/EfCaseWorkflowStore.cs` | Persistence or provider adapter; inspect data access, transaction and failure behaviour before changing callers. |
| `tests/Pegasus.IntegrationTests/CaseWorkflowPersistenceTests.cs` | Focused verification surface; extend the stated success, failure and regression coverage. |
| `src/Pegasus.Contracts/Cases/Commands/` | Named by the ticket as an implementation or verification dependency. |
| `src/Pegasus.Web/Api/CaseCommandEndpoints.cs` | Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core. |
| `src/Pegasus.Web/Mcp/AutomationMcpErrors.cs` | Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core. |
| `src/Pegasus.Web/Program.cs` | Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core. |
| `tests/Pegasus.IntegrationTests/DesktopGatewayCaseCommandTests.cs` | Focused verification surface; extend the stated success, failure and regression coverage. |
| `tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj` | Focused verification surface; extend the stated success, failure and regression coverage. |

## Context files

- `docs/desktop/03-gateway-api-and-data/README.md` — Authoritative context; update only if the ticket's accepted scope explicitly calls for it.
- `docs/desktop/03-gateway-api-and-data/endpoint-map.md` — Authoritative context; update only if the ticket's accepted scope explicitly calls for it.
- `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` — Authoritative context; update only if the ticket's accepted scope explicitly calls for it.
- `src/Pegasus.Core/Lifecycle/CaseLifecycle.cs` — Core policy or contract; reuse the existing business owner rather than placing policy in a host.
- `src/Pegasus.Web/Pages/Cases/Workflow.cshtml.cs` — Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core.
- `src/Pegasus.Web/Pages/Cases/Closure.cshtml.cs` — Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core.
- `src/Pegasus.Web/Pages/Cases/Details.cshtml.cs` — Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core.
- `src/Pegasus.Core/Workflow/CaseWorkflowContracts.cs` — Core policy or contract; reuse the existing business owner rather than placing policy in a host.
- `src/Pegasus.Core/Workflow/CaseCommandContracts.cs` — Core policy or contract; reuse the existing business owner rather than placing policy in a host.
- `src/Pegasus.Core/Workflow/CaseEditAuthority.cs` — Core policy or contract; reuse the existing business owner rather than placing policy in a host.
- `src/Pegasus.Infrastructure/Persistence/EfCaseWorkflowStore.cs` — Persistence or provider adapter; inspect data access, transaction and failure behaviour before changing callers.
- `tests/Pegasus.IntegrationTests/CaseWorkflowPersistenceTests.cs` — Focused verification surface; extend the stated success, failure and regression coverage.
- `src/Pegasus.Contracts/Cases/Commands/` — Named by the ticket as an implementation or verification dependency.
- `src/Pegasus.Web/Api/CaseCommandEndpoints.cs` — Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core.

## Ripple and out-of-scope boundary

- **Azure**: no write.
- **Scope boundary**: may touch `src/Pegasus.Web/Api/**`, `src/Pegasus.Contracts/Cases/**`, the rate-limiter configuration in `Program.cs`, `openapi/`, the generated client and the test projects. Must not touch `src/Pegasus.Core/Lifecycle/**` or any Razor page model — the business rules already exist and stay where they are. **Named conditional exception for upstream KANMER-005**: if and only if a step 12 fact fails, this ticket may change `src/Pegasus.Core/Workflow/CaseEditAuthority.cs` and `src/Pegasus.Infrastructure/Persistence/EfCaseWorkflowStore.cs` `ClaimAsync` to enforce the exclusion — recorded in the ticket plan with the failing fact quoted, and reviewed as a Core change. It is not a licence to refactor the lease code when the facts pass. [[DSK-05-08]] holds no part of this exception and may not make the fix.
- **Traps**: two policy engines — any rule that appears in an endpoint filter and not in Core is a defect. Do not reproduce `Pages/Cases/CaseMutationPageModel.cs`'s TempData proposed-values/lease chaining; the desktop sends explicit fields. Reuse the existing rate limiter rather than adding a second mechanism. This row covers eighteen routes and is the largest in the epic — it is deliberately not split, but sequence the work by group (lease → save/completeness → workflow → closure → create) and keep the checklist per group. **Cross-actor lease exclusion is proved here, not assumed, and this ticket is its single owner** — [[DSK-05-08]] restates the two step 12 facts verbatim under its own Source of truth and renders their outcome, asserting nothing itself; the two facts in step 12 are the evidence both tickets point at, and a lease matrix written with two staff actors only does not close upstream KANMER-005 because the failure was an Automation holder. [[DSK-05-08]]'s earlier "confirm it is implemented on the endpoint" wording was withdrawn precisely because it pointed at nothing named — do not reintroduce that shape anywhere. **Upstream ids and fork board ids do not match**: upstream KANMER-005 has no fork ticket at all, so never write it as a board wiki-link.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.
