# Files — FEAT-032 Desktop document browser and transfer queue

| Path or module | Intended change | Reuse / risk |
| --- | --- | --- |
| src/Pegasus.Desktop/Views/Documents/*; ViewModels/Documents/* | Native browser, queue, preview and operation-state presentation. | No WebView shell or provider client. |
| src/Pegasus.Desktop.Infrastructure/Gateway/* | Use generated Box broker client and bounded cache service. | Cache must be size/lifetime bounded and non-authoritative. |
| tests/Pegasus.Desktop.ViewModelTests; UITests | Queue state, cancellation, retry and preview UI. | No fabricated custody data. |
| tests/Pegasus.Api.ContractTests | Consume FEAT-031 contract fixtures. | Keep client/server contracts aligned. |

## Context files

Read docs/desktop/07-integrations/README.md, docs/desktop/03-gateway-api-and-data/endpoint-map.md, relevant screen specs, the ticket body, pegasus-desktop, dotnet-webapi and winui-design as routed. Source changes later occur only in this ticket worktree.

## Out of scope

Azure changes, provider account/credential changes, direct desktop provider clients, a new deployment unit, and a second business-policy implementation.
