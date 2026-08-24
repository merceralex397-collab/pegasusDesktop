# Files — FEAT-035 DVLA/DVSA gateway endpoints

| Path or module | Intended change | Reuse / risk |
| --- | --- | --- |
| Pegasus.Core existing vehicle lookup ports/use cases | Reuse validation, acceptance and provenance policy. | Do not reimplement rules in endpoint. |
| src/Pegasus.Web/Features/Vehicle/*; Contracts | Add minimal api-v1 routes and DTOs. | Map known provider errors to project problem details. |
| tests/Pegasus.Api.ContractTests | Authorization, status, accept and cache/provenance tests. | Use replay/fake provider. |
| docs/desktop/03-gateway-api-and-data/endpoint-map.md | Record endpoint names/contracts after the work. | No provider contract copied into canonical docs. |

## Context files

Read docs/desktop/07-integrations/README.md, docs/desktop/03-gateway-api-and-data/endpoint-map.md, relevant screen specs, the ticket body, pegasus-desktop, dotnet-webapi and winui-design as routed. Source changes later occur only in this ticket worktree.

## Out of scope

Azure changes, provider account/credential changes, direct desktop provider clients, a new deployment unit, and a second business-policy implementation.
