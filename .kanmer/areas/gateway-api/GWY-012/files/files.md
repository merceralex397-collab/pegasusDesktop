# Files — GWY-012: DSK-03-12 · Mail workspace endpoints: list, preview, message detail, case link/unlink, classify, folder move

## Change surface

| Path | Purpose / risk |
| --- | --- |
| `docs/desktop/03-gateway-api-and-data/README.md` | Authoritative context; update only if the ticket's accepted scope explicitly calls for it. |
| `docs/desktop/03-gateway-api-and-data/endpoint-map.md` | Authoritative context; update only if the ticket's accepted scope explicitly calls for it. |
| `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` | Authoritative context; update only if the ticket's accepted scope explicitly calls for it. |
| `src/Pegasus.Core/Intake/RetainedMail.cs` | Core policy or contract; reuse the existing business owner rather than placing policy in a host. |
| `src/Pegasus.Web/Pages/Mail/Index.cshtml.cs` | Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core. |
| `src/Pegasus.Web/Pages/Mail/Message.cshtml.cs` | Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core. |
| `tests/Pegasus.IntegrationTests/MailWorkspaceWebTests.cs` | Focused verification surface; extend the stated success, failure and regression coverage. |
| `src/Pegasus.Web/Presentation/OperatorLabels.cs` | Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core. |
| `docs/desktop/07-integrations/README.md` | Authoritative context; update only if the ticket's accepted scope explicitly calls for it. |
| `src/Pegasus.Contracts/Mail/` | Named by the ticket as an implementation or verification dependency. |
| `src/Pegasus.Web/Api/MailEndpoints.cs` | Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core. |
| `tests/Pegasus.IntegrationTests/DesktopGatewayMailTests.cs` | Focused verification surface; extend the stated success, failure and regression coverage. |
| `tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj` | Focused verification surface; extend the stated success, failure and regression coverage. |

## Context files

- `docs/desktop/03-gateway-api-and-data/README.md` — Authoritative context; update only if the ticket's accepted scope explicitly calls for it.
- `docs/desktop/03-gateway-api-and-data/endpoint-map.md` — Authoritative context; update only if the ticket's accepted scope explicitly calls for it.
- `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` — Authoritative context; update only if the ticket's accepted scope explicitly calls for it.
- `src/Pegasus.Core/Intake/RetainedMail.cs` — Core policy or contract; reuse the existing business owner rather than placing policy in a host.
- `src/Pegasus.Web/Pages/Mail/Index.cshtml.cs` — Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core.
- `src/Pegasus.Web/Pages/Mail/Message.cshtml.cs` — Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core.
- `tests/Pegasus.IntegrationTests/MailWorkspaceWebTests.cs` — Focused verification surface; extend the stated success, failure and regression coverage.
- `src/Pegasus.Web/Presentation/OperatorLabels.cs` — Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core.
- `docs/desktop/07-integrations/README.md` — Authoritative context; update only if the ticket's accepted scope explicitly calls for it.
- `src/Pegasus.Contracts/Mail/` — Named by the ticket as an implementation or verification dependency.
- `src/Pegasus.Web/Api/MailEndpoints.cs` — Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core.
- `tests/Pegasus.IntegrationTests/DesktopGatewayMailTests.cs` — Focused verification surface; extend the stated success, failure and regression coverage.
- `tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj` — Focused verification surface; extend the stated success, failure and regression coverage.

## Ripple and out-of-scope boundary

- **Azure**: no write. Graph is reached through the existing adapters; a replay adapter stands in locally (L-02).
- **Scope boundary**: may touch `src/Pegasus.Web/Api/**`, `src/Pegasus.Contracts/Mail/**`, `openapi/`, the generated client and the test projects. Must not touch `src/Pegasus.Core/Intake/RetainedMail.cs`, the Graph adapters in `src/Pegasus.Infrastructure`, or `src/Pegasus.Web/Pages/Mail/**`.
- **Traps**: **upstream drift** — upstream `main` is 32 commits ahead with MAIL-011/012 among them; start this ticket only after the first upstream sync (`DSK-00-02`) or you will project code that has since changed. One vocabulary list: labels come from `OperatorLabels`, never a second map. Two policy engines: the provider-availability rule stays in Core/the provider port. Operator copy is governed by `docs/design/README.md` — a sentence that explains rather than states is a defect.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.
