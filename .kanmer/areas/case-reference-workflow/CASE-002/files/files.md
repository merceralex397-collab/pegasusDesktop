# Files — CASE-002: upstream:CASE-022 · Deliver public upload links (INT-31) to the operator's accepted limits

## Change surface

| Path | Purpose / risk |
| --- | --- |
| `docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md` | Authoritative context; update only if the ticket's accepted scope explicitly calls for it. |
| `src/Pegasus.Core/Documents/RequestUploadPolicy.cs` | Core policy or contract; reuse the existing business owner rather than placing policy in a host. |
| `src/Pegasus.Web/Program.cs` | Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core. |
| `src/Pegasus.Core/Intake/IntakeContracts.cs` | Core policy or contract; reuse the existing business owner rather than placing policy in a host. |
| `src/Pegasus.Infrastructure/DependencyInjection.cs` | Persistence or provider adapter; inspect data access, transaction and failure behaviour before changing callers. |
| `src/Pegasus.Infrastructure/Persistence/UnavailableDocumentRequestStore.cs` | Persistence or provider adapter; inspect data access, transaction and failure behaviour before changing callers. |
| `src/Pegasus.Infrastructure/Persistence/EfDocumentRequestStore.cs` | Persistence or provider adapter; inspect data access, transaction and failure behaviour before changing callers. |
| `tests/Pegasus.IntegrationTests/ProductionCompositionTests.cs` | Focused verification surface; extend the stated success, failure and regression coverage. |
| `src/Pegasus.Web/Pages/Cases/Custody.cshtml.cs` | Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core. |
| `src/Pegasus.Web/Pages/Cases/Shared/_CaseDocuments.cshtml` | Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core. |
| `src/Pegasus.Web/Pages/Uploads/Request.cshtml` | Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core. |
| `docs/open-decisions.md` | Authoritative context; update only if the ticket's accepted scope explicitly calls for it. |
| `docs/desktop/03-gateway-api-and-data/endpoint-map.md` | Authoritative context; update only if the ticket's accepted scope explicitly calls for it. |
| `docs/desktop/03-gateway-api-and-data/README.md` | Authoritative context; update only if the ticket's accepted scope explicitly calls for it. |
| `docs/desktop/06-ui-design/screen-specs.md` | Authoritative context; update only if the ticket's accepted scope explicitly calls for it. |
| `docs/frd/frd-05-documents-extraction-and-custody.md` | Authoritative context; update only if the ticket's accepted scope explicitly calls for it. |
| `tests/Pegasus.Core.Tests` | Focused verification surface; extend the stated success, failure and regression coverage. |
| `tests/Pegasus.IntegrationTests` | Focused verification surface; extend the stated success, failure and regression coverage. |
| `docs/runbook.md` | Authoritative context; update only if the ticket's accepted scope explicitly calls for it. |
| `docs/desktop/11-azure-disposition/README.md` | Authoritative context; update only if the ticket's accepted scope explicitly calls for it. |
| `tests/Pegasus.Core.Tests/Pegasus.Core.Tests.csproj` | Focused verification surface; extend the stated success, failure and regression coverage. |
| `tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj` | Focused verification surface; extend the stated success, failure and regression coverage. |
| `tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj` | Focused verification surface; extend the stated success, failure and regression coverage. |

## Context files

- `docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md` — Authoritative context; update only if the ticket's accepted scope explicitly calls for it.
- `src/Pegasus.Core/Documents/RequestUploadPolicy.cs` — Core policy or contract; reuse the existing business owner rather than placing policy in a host.
- `src/Pegasus.Web/Program.cs` — Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core.
- `src/Pegasus.Core/Intake/IntakeContracts.cs` — Core policy or contract; reuse the existing business owner rather than placing policy in a host.
- `src/Pegasus.Infrastructure/DependencyInjection.cs` — Persistence or provider adapter; inspect data access, transaction and failure behaviour before changing callers.
- `src/Pegasus.Infrastructure/Persistence/UnavailableDocumentRequestStore.cs` — Persistence or provider adapter; inspect data access, transaction and failure behaviour before changing callers.
- `src/Pegasus.Infrastructure/Persistence/EfDocumentRequestStore.cs` — Persistence or provider adapter; inspect data access, transaction and failure behaviour before changing callers.
- `tests/Pegasus.IntegrationTests/ProductionCompositionTests.cs` — Focused verification surface; extend the stated success, failure and regression coverage.
- `src/Pegasus.Web/Pages/Cases/Custody.cshtml.cs` — Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core.
- `src/Pegasus.Web/Pages/Cases/Shared/_CaseDocuments.cshtml` — Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core.
- `src/Pegasus.Web/Pages/Uploads/Request.cshtml` — Web/gateway composition or transport adapter; preserve the existing host conventions and keep policy in Core.
- `docs/open-decisions.md` — Authoritative context; update only if the ticket's accepted scope explicitly calls for it.
- `docs/desktop/03-gateway-api-and-data/endpoint-map.md` — Authoritative context; update only if the ticket's accepted scope explicitly calls for it.
- `docs/desktop/03-gateway-api-and-data/README.md` — Authoritative context; update only if the ticket's accepted scope explicitly calls for it.

## Ripple and out-of-scope boundary

- **Azure**: ⚠ **Azure write** at step 11 only — setting `DocumentRequests__AcceptedLimitsVersion`, the `DocumentRequests` section and (if chosen globally) the request-body limit on the **deployed production Container App** hosting `Pegasus.Web`. Exact-target approval is required under `docs/runbook.md` § *Live-operation approval matrix* and the write is mirrored in `docs/desktop/11-azure-disposition/README.md`. It is an **operator step**: no agent performs it. Every other Azure interaction in this ticket is read-only (`containerapps` read for the ingress ceiling). Nothing is deprovisioned.
- **Scope boundary**: may touch `src/Pegasus.Core/Documents/RequestUploadPolicy.cs`, `src/Pegasus.Infrastructure/DependencyInjection.cs` (the four upload-link registrations only), `src/Pegasus.Infrastructure/Persistence/EfDocumentRequestStore.cs`, the Kestrel and `DocumentRequests` configuration in `src/Pegasus.Web/Program.cs`, `src/Pegasus.Core/Intake/IntakeContracts.cs` (only if step 3 proves the staff form and this route share a path, with the reason recorded), `tests/Pegasus.Core.Tests`, `tests/Pegasus.IntegrationTests` and the documents listed above. Must **not** touch `src/Pegasus.Web/Api/**` or `src/Pegasus.Contracts/**` — [[DSK-03-11]] owns the `/api/v1` routes; must not touch any desktop project — [[DSK-05-14]] owns the affordance; must not touch `src/Pegasus.Worker`; must not revive the dead controls in `src/Pegasus.Web/Pages/Cases/Shared/_CaseDocuments.cshtml`; must not edit `docs/adr/0003-pdfpig-for-first-qdos-slice.md`.
- **Blocks (this must land before these can correctly ship)**: [[DSK-03-11]] — its request-upload-link routes return the named `provider-unavailable` problem and are inert until this activates INT-31, and its scope boundary hands `RequestUploadPolicy.cs` and `IntakeContracts.cs` to this ticket; [[DSK-05-14]] — its acceptance criterion *"Request-upload links can be created and revoked"* cannot be true while the store throws, and its traps carry the inert-until-CASE-022 statement. A later pass wires these as `blocks` links; this ticket does not.
- **Blocked by**: nothing on the fork board. The two open questions in step 3 are the only gate, and one of them needs the operator.
- **Stale triage disposition**: the `desktop-screen-spec` label and the `upstream-kanmer-carryover.md:111` row ("Make creating a public upload link findable", plan area 06, fork area `desktop-ui`) both predate the 2026-08-24 retitle and rescope. They are carried for provenance only. This is a Core policy contract plus a Kestrel limit; it is not a screen specification and it is not `desktop-ui` work.
- **Traps**: do **not** "just supply eight numbers" — two of the accepted answers are refused by the built contract, and setting `DocumentRequests:AcceptedLimitsVersion` without the contract change activates a capability that then rejects every operator-chosen expiry. Do **not** raise `IntakeEnvelopeLimits.MaximumContentLength` expecting the ceiling to move; `MaxRequestBodySize` is the real refusal point and it is configured nowhere. Do **not** weaken the fail-closed guarantee: the capability must still be unavailable when no accepted limits version is set, and `ProfileWithoutDurableStorageStillFailsClosed` must stay untouched. Do **not** build a second custody path — Box, through the existing case-document path, is the operator's answer. Runtime-role grants: if any new table is introduced it needs a `Grant*` migration mirrored in `scripts/Invoke-AzureDatabaseBootstrap.ps1` and enforced by `scripts/Test-MigrationGrants.ps1` in CI — "works locally, fails only in production" has shipped three times (upstream PLAT-035). Anonymous surface: no case detail may be disclosed through the token.
- **Pipeline note**: upstream has **no** pipeline documents for this ticket, so `research`, `files`, `plan` and `checklist` are written from scratch and the `open-questions` document from step 3 will legitimately block the move out of Preparing until both questions are answered. That is correct, not a fault.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the ticket plan document.
