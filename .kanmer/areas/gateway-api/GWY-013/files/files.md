# Files — GWY-013: DSK-03-13 · Triage, Unidentified, image-intake and Operations endpoints as explicit named commands

## Change surface

| Path | Purpose / risk |
| --- | --- |
| `docs/desktop/03-gateway-api-and-data/README.md` | Authoritative context; update only if the ticket's accepted scope explicitly calls for it. |
| `docs/desktop/03-gateway-api-and-data/endpoint-map.md` | Authoritative context; update only if the ticket's accepted scope explicitly calls for it. |
| `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` | Authoritative context; update only if the ticket's accepted scope explicitly calls for it. |
| `src/Pegasus.Core/Triage/TriageContracts.cs` | Core policy or contract; reuse the existing business owner rather than placing policy in a host. |
| `src/Pegasus.Core/Intake/DurableIntake.cs` | Core policy or contract; reuse the existing business owner rather than placing policy in a host. |
| `src/Pegasus.Web/Pages/Triage/Details.cshtml.cs` | Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core. |
| `src/Pegasus.Web/Pages/Operations/Index.cshtml.cs` | Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core. |
| `src/Pegasus.Core/Intake/Unidentified/UnidentifiedContracts.cs` | Core policy or contract; reuse the existing business owner rather than placing policy in a host. |
| `src/Pegasus.Core/Triage/TriageLifecycle.cs` | Core policy or contract; reuse the existing business owner rather than placing policy in a host. |
| `src/Pegasus.Core/ImageIntake/ImageIntakeLifecycle.cs` | Core policy or contract; reuse the existing business owner rather than placing policy in a host. |
| `tests/Pegasus.IntegrationTests/TriageQueuesWebTests.cs` | Focused verification surface; extend the stated success, failure and regression coverage. |
| `src/Pegasus.Web/Pages/Unidentified/Details.cshtml.cs` | Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core. |
| `src/Pegasus.Contracts/Triage/` | Named by the ticket as an implementation or verification dependency. |
| `src/Pegasus.Web/Api/TriageEndpoints.cs` | Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core. |
| `src/Pegasus.Web/Api/UnidentifiedEndpoints.cs` | Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core. |
| `tests/Pegasus.IntegrationTests/DesktopGatewayTriageTests.cs` | Focused verification surface; extend the stated success, failure and regression coverage. |
| `tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj` | Focused verification surface; extend the stated success, failure and regression coverage. |

## Context files

- `docs/desktop/03-gateway-api-and-data/README.md` — Authoritative context; update only if the ticket's accepted scope explicitly calls for it.
- `docs/desktop/03-gateway-api-and-data/endpoint-map.md` — Authoritative context; update only if the ticket's accepted scope explicitly calls for it.
- `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` — Authoritative context; update only if the ticket's accepted scope explicitly calls for it.
- `src/Pegasus.Core/Triage/TriageContracts.cs` — Core policy or contract; reuse the existing business owner rather than placing policy in a host.
- `src/Pegasus.Core/Intake/DurableIntake.cs` — Core policy or contract; reuse the existing business owner rather than placing policy in a host.
- `src/Pegasus.Web/Pages/Triage/Details.cshtml.cs` — Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core.
- `src/Pegasus.Web/Pages/Operations/Index.cshtml.cs` — Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core.
- `src/Pegasus.Core/Intake/Unidentified/UnidentifiedContracts.cs` — Core policy or contract; reuse the existing business owner rather than placing policy in a host.
- `src/Pegasus.Core/Triage/TriageLifecycle.cs` — Core policy or contract; reuse the existing business owner rather than placing policy in a host.
- `src/Pegasus.Core/ImageIntake/ImageIntakeLifecycle.cs` — Core policy or contract; reuse the existing business owner rather than placing policy in a host.
- `tests/Pegasus.IntegrationTests/TriageQueuesWebTests.cs` — Focused verification surface; extend the stated success, failure and regression coverage.
- `src/Pegasus.Web/Pages/Unidentified/Details.cshtml.cs` — Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core.
- `src/Pegasus.Contracts/Triage/` — Named by the ticket as an implementation or verification dependency.
- `src/Pegasus.Web/Api/TriageEndpoints.cs` — Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core.

## Ripple and out-of-scope boundary

- **Azure**: no write.
- **Scope boundary**: may touch `src/Pegasus.Web/Api/**`, `src/Pegasus.Contracts/{Triage,Unidentified,ImageIntake,Operations}/**`, `openapi/`, the generated client and the test projects. Must not touch `src/Pegasus.Core/Intake/Unidentified/**` or the matching Razor page models. **One named conditional exception for step 8**: if the sync does not bring `ITriageQueries.GetByOriginReceiptAsync`, this ticket may add exactly that one read-only query member to `src/Pegasus.Core/Triage/TriageContracts.cs` and its EF implementation — nothing else in `src/Pegasus.Core/Triage/**`, and no change to `TriageLifecycle.cs` or `TriageLifecycleRules`. The promote path composes existing Core commands; it does not write a new one.
- **Traps**: two policy engines — triage lifecycle rules already exist in `Triage/TriageLifecycle.cs` and are shared with the MCP `pegasus_triage_*` tools; the API is a third ingress over the same Core, never a third rule set. **The promote path is composition, not new policy** (upstream INTK-035): one registration normaliser and it is `ImageIntakeLifecycle.NormalizeRegistrationInput`; one judge of validity and it is `TriageLifecycleRules.ValidateCreate`; one Triage-creation command and it is `ICreateTriageFromIntake`, which gains a second **caller** and not a second implementation — a gateway-side registration regex, a gateway-side Triage insert, or a second origin-receipt lookup in [[DSK-05-12]] is a stop condition. **This ticket is the single owner of the resolve contract and of the origin-receipt lookup**; [[DSK-05-12]] consumes both and adds neither. Operation-key limits differ per area (100 vs 200) — do not normalise them. **Upstream ids and fork board ids do not match**: upstream INTK-035 has no fork ticket, upstream INTK-033 is board [[INTK-007]], and the board's `INTK-001`…`INTK-007` are upstream INTK-002, INTK-003, INTK-026, INTK-027, INTK-031, INTK-032 and INTK-033 — never cite a bare intake id in this body; use `upstream <ID> (board [[<board-id>]])`, or say "absorbed, no fork ticket". **Phase note**: `README.md` § 5 sequencing does not list this row; the horizon is taken from `endpoint-map.md`, which puts triage, unidentified and image intake at Phase 5 and the two Operations rows at Phase 3 — land the Operations group first if the Phase 3 slice needs it.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.
