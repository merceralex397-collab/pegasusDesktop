# Research — GWY-014: DSK-03-14 · Vehicle lookup and assessment endpoints: damage, estimate import, specification, report draft, send

## Question

Add the vehicle-lookup and assessment surfaces to `/api/v1`: request a lookup, accept a suggestion, read the assessment, save damage, import an estimate through an upload session, accept the repair specification, generate the report draft, register a final report, and send or reconcile the assessment.

## Evidence examined

- Plan row: `docs/desktop/03-gateway-api-and-data/README.md` § 5 — `DSK-03-14`
- Plan detail: same file § 3 — rows *Idempotency*, *Concurrency*, *Bytes & uploads*, *Retry*
- Plan detail: `docs/desktop/03-gateway-api-and-data/endpoint-map.md` § Cases (Vehicle, EVA and Assessment rows)
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 12.3 DVLA/DVSA, § 12.5 Documents, PDFs and reports, § 13.5 Vehicle and inspection information, § 13.9 Assessment, valuation and reporting, § 16.2 External provider resilience
- Endpoint contracts quoted from `endpoint-map.md`:
  - `POST /cases/{id}/vehicle/lookups` — replaces `Cases/Vehicle` `OnPostRequestVehicleLookupAsync`; Core `src/Pegasus.Core/Vehicle/` lookup request (durable request row; the Worker executes it); `PerformCasework`; `yes (key)`; `CaseMutationRequest` fields; returns request id + status; phase 6.
  - `POST /cases/{id}/vehicle/suggestions/{sid}/accept` — replaces `OnPostAcceptVehicleSuggestionAsync`; vehicle suggestion acceptance; `PerformCasework`; `yes (key)`; `CaseMutationRequest` fields; returns version; phase 6.
  - `GET /cases/{id}/assessment` — replaces `Cases/Assessment/Index` `OnGetAsync` (740 lines); Core `ICaseAssessmentStore` reads and `AssessmentPolicy`; `PerformCasework`; GET; `ETag` + `version`; returns the assessment model and readiness summary; phase 7.
  - `POST /cases/{id}/assessment/damage` — replaces `OnPostSaveDamageAsync`; Core `src/Pegasus.Core/Assessment/` save command; `PerformCasework`; `yes (key)`; `CaseMutationRequest` fields; returns version; phase 7.
  - `POST /cases/{id}/assessment/estimate-import` (upload session) — replaces `OnPostImportEstimateAsync` (`IFormFile`); `IEstimateDocumentParser` (`AudatexEstimatePdfParser`) via the Core import; `PerformCasework`; `yes (key)`; `CaseMutationRequest` fields; returns imported lines + version; phase 7.
  - `POST /cases/{id}/assessment/specification/accept` — replaces `OnPostAcceptSpecificationAsync`; repair specification acceptance; auth right **Engineer**; `yes (key)`; `CaseMutationRequest` fields; returns version; phase 7.
  - `POST /cases/{id}/reports/draft` — replaces `OnPostGenerateReportDraftAsync`; Core `GenerateCaseAssessmentReportDraft` → `IAssessmentReportRenderer` (gateway-side until L-03 parity; then the desktop renders and `POST /cases/{id}/reports` registers the final PDF); `PerformCasework`; `yes (key)`; `CaseMutationRequest` fields; returns report bytes or a report id + `ETag`; phase 7.
  - `POST /cases/{id}/reports` (register final), `GET /cases/{id}/reports/{rid}/content` — new for L-03; report registration + `IDocumentContentStore`; `PerformCasework`; `yes (key)`; `CaseMutationRequest` fields; returns report id + version, and bytes; phase 7.
  - `POST /cases/{id}/assessment/send`, `/reconcile` — replaces `OnPostSendAsync`, `OnPostReconcileAsync`; send/reconcile commands (`Assessment/`, `Workflow/`); `PerformCasework`; `yes (key)`; `CaseMutationRequest` fields; returns version + send status; phase 7.
- Repository evidence:
  - `src/Pegasus.Web/Pages/Cases/Vehicle.cshtml.cs` — handlers `OnPostRequestVehicleLookupAsync`, `OnPostAcceptVehicleSuggestionAsync`, `OnPostGenerateEvaHandoffAsync`
  - `src/Pegasus.Web/Pages/Cases/Assessment/Index.cshtml.cs` — handlers `OnGetAsync`, `OnPostSaveDamageAsync`, `OnPostImportEstimateAsync`, `OnPostAcceptSpecificationAsync`, `OnPostGenerateReportDraftAsync`, `OnPostSendAsync`, `OnPostReconcileAsync`
  - `tests/Pegasus.IntegrationTests/AssessmentDamageAndCopyWebTests.cs`, `AssessmentEstimateImportWebTests.cs`, `AssessmentVehiclePrefillWebTests.cs`, `AssessmentPersistenceIntegrationTests.cs`, `AutomaticVehicleLookupTests.cs`, `ProductionVehicleLookupTests.cs`, `VehicleWorkflowTerminalTests.cs` — the scenarios and replay adapters the new tests reuse
- Binding decisions:
  - L-01 — endpoints evolve inside `Pegasus.Web`; provider credentials stay there.
  - L-02 — replay adapters stand in for DVLA/DVSA; there is no Azure test environment.
  - L-03 — report rendering moves to an isolated non-UI WebView2 HTML→PDF path on the desktop; the gateway renderer is retained only until golden-file parity passes (ADR-0108). This ticket keeps the gateway draft endpoint and adds the registration endpoint the desktop renderer will call.
- Depends on: `DSK-03-08` for the case command conventions; `DSK-07-09` owns the DVLA/DVSA provider endpoints and cache/provenance contract; `DSK-07-19` owns the provider error taxonomy (`terminal`/`transient`/`unknown` plus `not-found`, `invalid-request`, `not-authorized`, `rate-limited`).

## Scope and constraints

Proposal § 13.5 and § 13.9 make vehicle evidence and assessment the engineer's core work; § 12.3 keeps DVLA/DVSA credentials behind the gateway so no long-lived provider secret ships in the package. Operator-visible consequence: an engineer completes an assessment natively, and a provider outage is distinguishable from "no record found" instead of both looking like a failure. The report-draft endpoint keeps the gateway renderer until golden-file parity passes under L-03/ADR-0108, after which the desktop renders and registers the final PDF.

- Future owner: `pegasus-gateway-dev`; tests: `pegasus-test-engineer`; independent review: `pegasus-desktop-reviewer`.
- Use `dotnet-webapi`, `optimizing-ef-core-queries` where the ticket changes a query, and `run-tests` for the actual runner profile. The project decision overrides generic “service per endpoint” advice: route handlers translate to existing `Pegasus.Core` ports; no second policy/service layer is introduced.
- The shared EPIC context binds this to versioned `/api/v1` route groups in the existing `Pegasus.Web`, the existing rate-limiter mechanism, an OpenAPI snapshot, and no Azure write.

- Microsoft Learn (fetched 2026-08-24): [ASP.NET Core OpenAPI support](https://learn.microsoft.com/aspnet/core/fundamentals/openapi/overview?view=aspnetcore-10.0) confirms first-party OpenAPI generation. Use the repository’s planned committed snapshot and contract-test flow rather than adding a parallel API documentation path.

- **Azure**: no write. DVLA/DVSA and the renderer are reached through the existing adapters; replay adapters stand in locally (L-02).
- **Scope boundary**: may touch `src/Pegasus.Web/Api/**`, `src/Pegasus.Contracts/{Assessment,Vehicle}/**`, `openapi/`, the generated client and the test projects. Must not touch `src/Pegasus.Core/Assessment/**`, `src/Pegasus.Core/Vehicle/**`, the renderer in `src/Pegasus.Infrastructure`, or the Razor assessment page.
- **Traps**: L-03 says the gateway renderer is retained **until golden-file parity passes** — removing or bypassing it here is out of bounds. Do not retry commands automatically; only idempotent `GET`s. `Pegasus.Web` still publishes `linux-x64` for the Playwright renderer base image, so no Windows-only package may enter it. **Phase span**: `README.md` § 5 sequencing lists this row as "11, 14 (Phase 6–7)", and `endpoint-map.md` gives the two vehicle rows Phase 6 and the assessment rows Phase 7; the horizon is set to the earliest phase that needs any of it. If the reviewer prefers endpoints to land with their callers, split the assessment rows into a Phase 7 follow-up rather than delaying the Phase 6 vehicle slice.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.

## Governing documents

- No canonical document is linked yet. Retain the ticket's existing `docs_todo` state; do not invent or link a proposed desktop ADR.

## Planning implication

Reuse the cited boundaries and revalidate the named sources against current `origin/dev` after the ticket is taken. Do not create a compatibility path, duplicate policy, or an unapproved external write.
