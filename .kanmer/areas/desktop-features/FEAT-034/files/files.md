# Files — FEAT-034 Box conflict and version handling

| Path or module | Intended change | Reuse / risk |
| --- | --- | --- |
| Pegasus.Core existing custody/version use case | Reuse or extend the single owner for compare-and-act semantics. | No duplicate version rule in desktop. |
| src/Pegasus.Web/Features/Box/*; Contracts | Expose conflict/precondition result through api-v1. | Do not leak provider-specific internals. |
| src/Pegasus.Desktop/ViewModels/Documents/* | Present current/conflict/reload action. | No automatic overwrite. |
| tests/Pegasus.Api.ContractTests; Desktop.ViewModelTests | Cover stale version, refresh and authorised resolution. | One fixture establishes truth. |

## Context files

Read docs/desktop/07-integrations/README.md, docs/desktop/03-gateway-api-and-data/endpoint-map.md, relevant screen specs, the ticket body, pegasus-desktop, dotnet-webapi and winui-design as routed. Source changes later occur only in this ticket worktree.

## Out of scope

Azure changes, provider account/credential changes, direct desktop provider clients, a new deployment unit, and a second business-policy implementation.
