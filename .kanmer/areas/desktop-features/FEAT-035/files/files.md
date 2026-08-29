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

## Verified Core owners and refusal boundaries (2026-08-29)

- `RequestVehicleLookupCommand` is owned by `RequestVehicleLookup`, which validates case/version/actor/operation/lease, rejects disabled `VehicleLookupAvailability`, and normalizes the registration through the single `VehicleLookupRequest` constructor before calling `IRequestVehicleLookupStore.RequestAsync`.
- `AcceptVehicleSuggestionCommand` is owned by `AcceptVehicleSuggestion`, which validates the decision, observation id, reason, correction values, and calls `IAcceptVehicleSuggestionStore.AcceptAsync` after Core normalization.
- Core refusal types observed in `VehicleWorkflow.cs` are `VehicleLookupUnavailableException`, `VehicleOperationConflictException`, `VehicleSuggestionUnavailableException`, `ConfirmedVehicleRegistrationRequiredException`, `ConfirmedVehicleRegistrationConflictException`, and `ConfirmedVehicleFieldConflictException`; route translation must preserve their distinctions.
- `IVehicleEvidenceQueries.GetAsync` is the read owner for confirmed evidence, observations, and confirmation history; infrastructure already registers `EfVehicleWorkflowStore` for all three gateway-facing ports.
- `Program.cs` composes `VehicleLookupAvailability.DevelopmentOfflineReplay` for the offline profile and `ProductionLive` for the production profile; no live provider call belongs in this gateway ticket.
- No vehicle gateway route group exists on `origin/dev`; this ticket must establish the shared group in `Pegasus.Web/Api` without a duplicate Core policy owner.
