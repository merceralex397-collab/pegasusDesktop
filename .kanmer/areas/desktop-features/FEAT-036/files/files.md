# Files — FEAT-036 Desktop vehicle workflow

| Path or module | Intended change | Reuse / risk |
| --- | --- | --- |
| src/Pegasus.Desktop/Views/Vehicle/*; ViewModels/Vehicle/* | Native validation/request/result/accept workflow. | No provider SDK/credential. |
| src/Pegasus.Desktop.Infrastructure/Gateway/* | Generated FEAT-035 client use. | No duplicate validation policy. |
| tests/Pegasus.Desktop.ViewModelTests; UITests | VRM/input, result, error/provenance and keyboard tests. | Use replay contracts. |
| docs/desktop/07-integrations/README.md | Record provider-contract check outcome if evidence changes. | Do not modify provider credentials policy. |

## Context files

Read docs/desktop/07-integrations/README.md, docs/desktop/03-gateway-api-and-data/endpoint-map.md, relevant screen specs, the ticket body, pegasus-desktop, dotnet-webapi and winui-design as routed. Source changes later occur only in this ticket worktree.

## Out of scope

Azure changes, provider account/credential changes, direct desktop provider clients, a new deployment unit, and a second business-policy implementation.
