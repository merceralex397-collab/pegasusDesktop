# Files — FEAT-031 Box broker endpoints

| Path or module | Intended change | Reuse / risk |
| --- | --- | --- |
| src/Pegasus.Web/Features/Box/* | Add versioned route group/DTO translation around existing Core/Infrastructure ports. | No provider policy in endpoint handlers. |
| src/Pegasus.Contracts/* | Add only caller-backed request/response contracts. | Do not expose secret/token/provider object detail. |
| tests/Pegasus.Api.ContractTests | Cover authorization, problem details, upload/download/session expiry and audit behaviour. | Use WebApplicationFactory/fakes, not live Box. |
| docs/desktop/07-integrations/* | Record endpoint-map/policy clarification if required. | No canonical credentials decision change. |

## Context files

Read docs/desktop/07-integrations/README.md, docs/desktop/03-gateway-api-and-data/endpoint-map.md, relevant screen specs, the ticket body, pegasus-desktop, dotnet-webapi and winui-design as routed. Source changes later occur only in this ticket worktree.

## Out of scope

Azure changes, provider account/credential changes, direct desktop provider clients, a new deployment unit, and a second business-policy implementation.
