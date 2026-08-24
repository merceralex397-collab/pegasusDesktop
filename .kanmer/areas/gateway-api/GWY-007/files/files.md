# Files — GWY-007: DSK-03-07 · Case read endpoints: paged list/search, sectioned detail, case history and audit

## Change surface

| Path | Purpose / risk |
| --- | --- |
| `docs/desktop/03-gateway-api-and-data/README.md` | Authoritative context; update only if the ticket's accepted scope explicitly calls for it. |
| `docs/desktop/03-gateway-api-and-data/endpoint-map.md` | Authoritative context; update only if the ticket's accepted scope explicitly calls for it. |
| `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` | Authoritative context; update only if the ticket's accepted scope explicitly calls for it. |
| `docs/desktop/06-ui-design/screen-specs.md` | Authoritative context; update only if the ticket's accepted scope explicitly calls for it. |
| `src/Pegasus.Core/Cases/CaseQueries.cs` | Core policy or contract; reuse the existing business owner rather than placing policy in a host. |
| `src/Pegasus.Infrastructure/Persistence/EfCaseQueryStore.cs` | Persistence or provider adapter; inspect data access, transaction and failure behaviour before changing callers. |
| `src/Pegasus.Core/Cases/CaseDataContracts.cs` | Core policy or contract; reuse the existing business owner rather than placing policy in a host. |
| `src/Pegasus.Infrastructure/Persistence/CaseDataModelConfiguration.cs` | Persistence or provider adapter; inspect data access, transaction and failure behaviour before changing callers. |
| `tests/Pegasus.IntegrationTests/CasesIndexWebTests.cs` | Focused verification surface; extend the stated success, failure and regression coverage. |
| `src/Pegasus.Web/Pages/Cases/Index.cshtml.cs` | Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core. |
| `src/Pegasus.Web/Pages/Cases/Details.cshtml.cs` | Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core. |
| `src/Pegasus.Contracts/Cases/` | Named by the ticket as an implementation or verification dependency. |
| `src/Pegasus.Web/Api/CaseReadEndpoints.cs` | Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core. |
| `src/Pegasus.Contracts` | Named by the ticket as an implementation or verification dependency. |
| `src/Pegasus.Core/Identity/IdentityContracts.cs` | Core policy or contract; reuse the existing business owner rather than placing policy in a host. |
| `src/Pegasus.Infrastructure/Persistence/` | Persistence or provider adapter; inspect data access, transaction and failure behaviour before changing callers. |
| `tests/Pegasus.IntegrationTests/DesktopGatewayCaseReadTests.cs` | Focused verification surface; extend the stated success, failure and regression coverage. |
| `eng/api/Export-OpenApiDocument.ps1` | Engineering tool or generation script; keep it deterministic and repository-owned. |
| `eng/api/Generate-ApiClient.ps1` | Engineering tool or generation script; keep it deterministic and repository-owned. |
| `tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj` | Focused verification surface; extend the stated success, failure and regression coverage. |

## Context files

- `docs/desktop/03-gateway-api-and-data/README.md` — Authoritative context; update only if the ticket's accepted scope explicitly calls for it.
- `docs/desktop/03-gateway-api-and-data/endpoint-map.md` — Authoritative context; update only if the ticket's accepted scope explicitly calls for it.
- `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` — Authoritative context; update only if the ticket's accepted scope explicitly calls for it.
- `docs/desktop/06-ui-design/screen-specs.md` — Authoritative context; update only if the ticket's accepted scope explicitly calls for it.
- `src/Pegasus.Core/Cases/CaseQueries.cs` — Core policy or contract; reuse the existing business owner rather than placing policy in a host.
- `src/Pegasus.Infrastructure/Persistence/EfCaseQueryStore.cs` — Persistence or provider adapter; inspect data access, transaction and failure behaviour before changing callers.
- `src/Pegasus.Core/Cases/CaseDataContracts.cs` — Core policy or contract; reuse the existing business owner rather than placing policy in a host.
- `src/Pegasus.Infrastructure/Persistence/CaseDataModelConfiguration.cs` — Persistence or provider adapter; inspect data access, transaction and failure behaviour before changing callers.
- `tests/Pegasus.IntegrationTests/CasesIndexWebTests.cs` — Focused verification surface; extend the stated success, failure and regression coverage.
- `src/Pegasus.Web/Pages/Cases/Index.cshtml.cs` — Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core.
- `src/Pegasus.Web/Pages/Cases/Details.cshtml.cs` — Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core.
- `src/Pegasus.Contracts/Cases/` — Named by the ticket as an implementation or verification dependency.
- `src/Pegasus.Web/Api/CaseReadEndpoints.cs` — Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core.
- `src/Pegasus.Contracts` — Named by the ticket as an implementation or verification dependency.

## Ripple and out-of-scope boundary

- **Azure**: no write.
- **Scope boundary**: may touch `src/Pegasus.Web/Api/**`, `src/Pegasus.Contracts/Cases/**`, `openapi/`, the generated client and the test projects. **Named exception for upstream CASE-020**: this ticket may also edit the `SearchRows` projection in `src/Pegasus.Infrastructure/Persistence/EfCaseQueryStore.cs` (`:224-252`) and nothing else in `src/Pegasus.Infrastructure` — the fix is a read projection, it is the one place the defect lives, and no other seeded ticket is permitted to make it. Must not modify `src/Pegasus.Web/Pages/Cases/**`, must not change any write path or the intake draft itself, and must not add a new Core query port without a recorded decision.
- **Traps**: do not port `Pages/Cases/CaseMutationPageModel.cs`'s TempData proposed-values/lease chaining — that is web-only state the desktop keeps in memory. Design authority: filters are dropdowns and tables sort newest first (`docs/design/README.md` § No explanatory copy and page economy). Contract changes must stay additive once the pilot ring exists. Upstream `main` is ahead of the fork; check for drift in the case pages after the first upstream sync ([[DSK-01-10]]). Upstream CASE-020 is **latent, not live** — all three production cases currently carry draft rows matching their case fields — so a "nothing changes in production" observation is expected and is not evidence the fix is unnecessary; the failure appears the first time staff correct a case. Whether the header should repeat registration and claimant at all is explicitly out of upstream CASE-020's scope (an open operator decision under **upstream CASE-018**, which has no fork ticket) — do not remove or restructure the header band here. **Upstream ids and fork board ids do not match, and every `CASE-` id in this body collides**: upstream CASE-020 was absorbed here and has **no fork ticket**; upstream CASE-021 is board [[CASE-001]] and upstream CASE-022 is board [[CASE-002]]; upstream `CASE-001` and upstream `CASE-002` are different tickets again and were both dropped, so board `CASE-001`/`CASE-002` never mean them; and upstream CASE-011, CASE-012, CASE-018, CASE-019 and ENG-013 have no fork tickets at all. Always write `upstream <ID>` or `upstream <ID> (board [[<board-id>]])`, never a bare `CASE-0nn`.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.
