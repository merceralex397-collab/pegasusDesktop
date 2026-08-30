# Reuse map — what is reused, extracted, replaced, cut

Dispositions: **REUSE** (as-is, referenced by the new code), **EXTRACT**
(moved to a shared home so one implementation serves web, gateway and
desktop), **REPLACE** (a native/gateway implementation takes over; the web
version stays until cutover), **KEEP** (stays exactly where it is, web-only
or server-only by design), **CUT** (removed after cutover). Line counts and
paths are from the fork at `main` `191ddf33` (2026-08-23).

## Pegasus.Core — REUSE as-is

`src/Pegasus.Core/` (107 files, 227 public interfaces, zero package
dependencies). It is already the proposal's Domain+Application layer. No
code moves out of it for the desktop; the gateway and the desktop both
depend on it (the desktop through `Pegasus.Contracts` DTOs and, where a
deterministic rule must run locally, through a direct project reference —
see the boundary note below).

| Folder (files) | What it owns | Consumed by slices | Ports / use cases the slices call |
| --- | --- | --- | --- |
| `Intake/` (32) | Receipts, durable queue intake, allocation, mail classification, case matching, QDOS policies, retained mail, unidentified | S9, S10, S11, S12, S13 | `IListIntake`, `IGetIntake` (`Intake/IntakeQueryUseCases.cs:5`, `:43`), `IAllocateIntake` (`Intake/IntakeAllocation.cs:208`), `ILinkIntake` (`Intake/DurableIntake.cs:1109`), `IReverseIntakeLink`, `ListRetainedMail`/`GetRetainedMail` (`Intake/RetainedMail.cs`), `SearchDeletedMail`, `IIntakeReceiptQueries`, `IRetainedMailQueries`, `IIntakeMutationStore`, `IUnidentifiedStore`, `DownloadIntakeSource` |
| `Workflow/` (8) | Case lifecycle contracts, edit lease/version authority, report-evidence links, sent-evidence polling, workflow configuration | S3, S5, S6, S8 | `CaseMutationRequest` (`Workflow/CaseWorkflowContracts.cs:182`), `ILeaseCaseForEdit`, `IAcquireCaseEditLease`/`IRenewCaseEditLease`/`IReleaseCaseEditLease` (`Workflow/CaseCommandContracts.cs:77-91`), `ICaseWorkflowStore`, `ICaseWorkflowQueries`, `ICaseWorkflowConfiguration` |
| `Identity/` (8) | `ActionActor`, `StaffRole`, `StaffAccessRight`, staff account admin, password change, approved mailboxes, security/action-history writers | every slice (authorization), S19, S21 | `StaffAuthorization`, `ISecurityEventWriter`, `IActionHistoryWriter`, `IStaffAccountQueries`, staff account administration use cases |
| `Cases/` (8) | Case contracts, data, notes, queries/search, linked replacement, organization/principal administration, provider inspection mode | S2, S3, S4, S5, S6, S7 | `ICaseQueryStore`, `ICaseDataQueries`, `ICaseDataStore`, linked-replacement, organization/principal administration |
| `ImageIntake/` (7) | Image-initiated case registration, automation, pairing, VRM recognition port, chase schedule | S12, S16 | `IImageIntakeQueries`, `IImageIntakeStore`, `IVrmRecognitionEngine` (port only) |
| `Tasks/` (5) | Case tasks, due-work scheduling, manual chase, `RunDueChasers` | S6 | `ICaseTaskQueries`, task commands |
| `Assessment/` (5) | `AssessmentPolicy` (499 lines), contracts, estimate import, repair specifications | S17 | `ICaseAssessmentStore`, `IEstimateDocumentParser` (server-side adapter) |
| `Vehicle/` (4) | Lookup contracts/work items, mileage policy, request→accept workflow | S15, S17 | `IVehicleLookupAdapter` (server-side), `IVehicleEvidenceQueries` |
| `Triage/` (4) | Triage lifecycle (561 lines), contracts, queries, email-evidence contracts | S11 | `ITriageQueries`, `ITriageStore`, triage commands |
| `Operations/` (4) | Dashboard counts, email operations, operations snapshot, request operations | S1, S20 | `IDashboardQueries`, operations projection |
| `ReferenceData/` (3) | Provider-domain catalogue and schema-version policy | S7 (read), S9 | `IProviderReferenceCatalog` |
| `Actors/` (3) | `StaffActorFactory`, `StaffSessionPolicy`, `ActorDisplayNames` | area 04, every slice | `StaffActorFactory.TryCreate`, `StaffSessionPolicy` |
| `Reports/` (2) | `AssessmentReportProjection` (362), `AssessmentReportRendering` (312) incl. `IAssessmentReportRenderer` | S18 | report draft generation use cases, renderer port |
| `Lifecycle/` (2) | `CaseLifecycle` (629), `CaseCommandSeams` (280) | S6 | lifecycle transitions |
| `Eva/` (2) | `EvaBundleSchema` (916), `CaseEvaMapping` | S15 | EVA bundle generation, `IEvaHandoffQueries`, `IEvaHandoffProxy` (server) |
| `Documents/` (2) | Document contracts, `RequestUploadPolicy` (469) | S13, S14 | `IDocumentContentStore` (server), request-upload policy |
| `Custody/` (2) | `CustodyContracts` (622), external work processing | S14, S20 | `ICaseCustody` (server adapter), `IExternalWorkStore`, `IExternalWorkEnqueuer` |
| `AiWork/` (2) | Send-to-AI hand-off contracts/operations | none (gated, out of parity scope) | — |
| `Address/` (2) | `Ext18InspectionAddressPolicy`, inspection address resolution | S4, S5 | address resolution |

Boundary note (proposal §5.3): `Pegasus.Desktop` may reference
`Pegasus.Core` for deterministic local validation and calculations
(desktop-side execution is the default, §4.1), but never `Pegasus.Infrastructure`,
EF Core, Azure SDKs, Box or Graph SDKs; `tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs`
is extended in area 02 to enforce it. Any rule the desktop runs locally is
re-checked by the gateway on write (the existing Core stores already do
this inside the transaction).

## Pegasus.Infrastructure — server-side, REUSE behind the gateway

`src/Pegasus.Infrastructure/` (238 files). Nothing in it is referenced by
the desktop. Portable-to-desktop candidates were reviewed; the default is
"none", with two exceptions noted.

| Folder (files) | Disposition | Notes |
| --- | --- | --- |
| `Persistence/` (190; `PegasusDbContext.cs` 1,526; 64 migrations, latest `20260822044425_GrantWorkerCaseDocuments`) | REUSE unchanged behind the gateway | New tables (OpenIddict desktop client is a row in an existing table; a minimum-client-version setting may be new) need `Grant*` migrations and the `scripts/Test-MigrationGrants.ps1` gate |
| `Intake/` (27; `MimeKitPdfPigOpenXmlIntakeSourceReader.cs` 1,233 + `.DocMsg.cs` 289; `DocumentExtraction/`; `AzureBlobIntakeArtifactStore` 658; `FileSystemIntakeArtifactStore` 624; `LocalDurableApprovedInboxSource` 514; `LocalEmailDisplayReader` 247) | REUSE server-side | Readers stay central (no network client, bounded limits); the desktop never parses source documents |
| `Email/` (5; `GraphApprovedSources.cs` 1,125; `LocalDurableApprovedSentSource` 553) | REUSE server-side (Worker/gateway) | Graph credentials never reach the desktop (ADR-0106) |
| `Custody/` (5; `BoxCaseCustody.cs` 1,016; `BoxDocumentContentStore` 240; `LocalCaseCustody` 549; `LocalDocumentContentStore` 183) | REUSE server-side | Box tokens stay central (ADR-0107); upstream PLAT-039 (token refresh) and PLAT-041 (folder resolve once per export) arrive through the upstream sync |
| `Vision/` (4; `OnnxVrmRecognitionEngine` 263, models embedded) | REUSE server-side initially | Proposal §12.6 allows local preprocessing; a later spike may move the engine to the desktop, not in parity scope |
| `Vehicle/` (2; `DvlaDvsaProductionAdapter` 412; `DvlaDvsaAdapters` 222 incl. replay) | REUSE server-side | Replay adapter is the Test/UAT stack's provider (area 08) |
| `Reports/` (1; `PlaywrightAssessmentReportRenderer.cs` 326) | REUSE until S18 parity, then retire from the Web container (ADR-0028 superseded by ADR-0108) | The **templates** (`docs/design/assets/report-renderer/templates/*.scriban`, `report.css`, brand PNGs) are embedded today by Infrastructure and will ALSO be embedded by `Pegasus.Desktop` for WebView2 rendering; PDFsharp post-processing is reusable as a package reference in `Pegasus.Desktop.Infrastructure`; area 07 owns the renderer design |
| `Assessment/` (1; `AudatexEstimatePdfParser.cs` 628) | REUSE server-side | Estimate import stays a gateway upload + parse |
| `Eva/` (1; `LocalEvaHandoffProxy` 40) | REUSE server-side | |
| `DependencyInjection.cs` (624; `AddPegasusInfrastructure` `:38`, `AddPegasusReportRendering` `:446`, production adapter registrations) | REUSE; gateway composition unchanged | The desktop has its own composition root (area 02) |

## Pegasus.Web — REPLACE pages, KEEP the host

`src/Pegasus.Web/` (85 `.cs`, 76 `.cshtml`; `Program.cs` 1,216 lines). Per
L-01 the host stays and grows the `/api/v1` groups (area 03); the Razor
surfaces are replaced slice by slice.

| Component | Disposition | Evidence | Target |
| --- | --- | --- | --- |
| 53 Razor page models (~10,800 LOC) and 76 `.cshtml` | REPLACE by desktop screens + gateway endpoints; CUT after cutover | Largest: `Pages/Mail/Message.cshtml.cs` 1,025; `Pages/Cases/Assessment/Index.cshtml.cs` 740; `Pages/Cases/Create.cshtml.cs` 689; `Pages/Cases/Details.cshtml.cs` 654; `Pages/Intake/Details.cshtml.cs` 613; `Pages/Triage/Details.cshtml.cs` 496; `Pages/Triage/Index.cshtml.cs` 449; `Pages/Mail/Index.cshtml.cs` 428; `Pages/Administration/Mailboxes.cshtml.cs` 362; `Pages/Cases/CaseMutationPageModel.cs` 339 | Slices S1–S21 |
| `Presentation/OperatorLabels.cs` (thin adapter; 24 `.cshtml` consumers) | EXTRACT pure map to `Pegasus.Contracts` with a Core-typed Web adapter | `OperatorVocabulary` owns the pure code→vocabulary map; the adapter preserves existing Web signatures. The intake-decision and VRM-outcome page maps are folded into the same owner so vocabulary cannot drift. `Pegasus.Desktop.Presentation.OperatorText` is the second consumer and adds formatting only; it does not copy the map | GWY-016, DUI-005 |
| `Presentation/RailCountsPageFilter.cs` (51) | REPLACE by a gateway endpoint | `IAsyncPageFilter` writing `ViewData["RailCounts"]`; desktop queries the dashboard group | S1 |
| `Pages/Cases/CaseMutationPageModel.cs` (339) | REPLACE (web keeps it until cutover) | Cookie TempData state machine, 8000/2000-char budgets (`:36-80`), `RetainableFormFields` allow-list; the desktop holds edit state in memory and relies on server leases | DSK-05-24 |
| `Pages/UploadConfirmationPageModel.cs` (82), `Presentation/UploadOutcome.cs` (304), `Presentation/UploadCaseDecision.cs` (306) | EXTRACT the composition (read models over Core ports) into gateway read models; REPLACE the `JsonResult` shell | | S13 |
| `Presentation/InstructionDraftFieldsView.cs` (64), `MailBodyPresentation.cs` (43), `MailClassificationSelection.cs` (102), `GalleryImage.cs` (4) | REPLACE by contracts/view models | Small view models bound to partials | S4, S9, S10, S16 |
| `Pages/Shared/*` (15 partials: `_Layout`, `_LayoutAuth`, `_LayoutExternal`, `_ErrorSummary`, `_FreshnessBanner`, `_ImageGallery`, `_InstructionDraftFields`, `_LucideSprite`, `_MetricCard`, `_PageHeader`, `_Provenance`, `_ProvenancePanel`, `_ReasonDialog`, `_StatusChip`, `_UploadOutcome`) + 4 case partials | REPLACE by XAML controls (area 06), CUT after cutover | The design vocabulary they encode (status chip, provenance, reason dialog, freshness banner) is the control catalogue for area 06 | area 06 |
| `wwwroot/css/site.css` (2,471), `wwwroot/js/site.js` (786) | CUT after cutover | Tokens are re-expressed as a WinUI `ResourceDictionary` from `docs/design/README.md` (the authority), not from `site.css` | area 06 |
| `Pages/Uploads/Request.cshtml.cs` (222) | KEEP on web | Anonymous external request-link upload (`RequestLink` actor); not a staff desktop surface | — |
| `Pages/Connect/Authorize.cshtml.cs` (177) | KEEP on web | OpenIddict consent for external MCP connectors (ADR-0027) | — |
| `Pages/Error.cshtml.cs`, `Pages/StatusCode.cshtml.cs` | KEEP while any web page remains | | — |
| `Mcp/` (14 files, ~3,200 LOC, 35 tools; `AutomationActorResolver.cs`, `AutomationMcpErrors.cs`) | KEEP; the reference projection for the gateway | Shares Core with the new API; never a second policy engine; error normalisation ported to problem details (area 03) | area 03 |
| `Authentication/` (DevelopmentOffline handler), rate limiting, cookie auth in `Program.cs` | KEEP for web; token flow added beside it (area 04) | `Program.cs:262-457` | area 04 |
| `Health/DatabaseReadinessHealthCheck.cs`, `/health/*`, `/diagnostics/version` | REUSE; extend with integration health (area 10) | | S20 |

## Pegasus.Worker — REUSE unchanged

`src/Pegasus.Worker/` (nine functions: `PendingWorkDispatchFunction`,
`IntakeWorkFunction`, `IntakePoisonFunction`,
`StagedArtifactReconciliationFunction`, `InboxPollFunction`,
`SentEvidencePollFunction`, `DueWorkSweepFunction`, `ExternalWorkFunction`,
`ExternalPoisonFunction`). No desktop slice changes it. Worker defects from
the upstream board (INTK-003, INTK-027) are carried over as Worker tickets,
not desktop work.

## Standalone tools — KEEP

`scripts/email-eval-desktop/` (WinForms `net10.0-windows`, ADR-0016) stays
outside `Pegasus.slnx`; it is not merged into `Pegasus.Desktop` and not
referenced by it.

## Tests — REUSE and extend

| Suite | Disposition |
| --- | --- |
| `tests/Pegasus.Core.Tests` (494 facts, 72 theories) | REUSE; grows with every characterization test written before a rule moves |
| `tests/Pegasus.IntegrationTests` (716 facts; `WebApplicationFactory` via `IntakeWebTestSupport.cs:26`; LocalDB shards) | REUSE for the gateway `/api/v1` groups (same factory, gate enabled); Razor-page web tests stay green until their page is cut |
| `tests/Pegasus.IntegrationTests/Browser/` (20 facts, Playwright + axe) | KEEP until web retirement; the desktop equivalent is `tests/Pegasus.Desktop.UITests` (`winapp ui`) plus `axe-windows` (area 08) |
| `tests/Pegasus.ArchitectureTests` (62 facts, reflection-based) | EXTEND: desktop dependency rules, no-WebView-hosting-Pegasus-UI rule, shared-vocabulary single-owner rule |
| New: `tests/Pegasus.Desktop.ViewModelTests`, `tests/Pegasus.Api.ContractTests`, `tests/Pegasus.Desktop.UITests`, `tests/Pegasus.Packaging.Tests` | CREATE (area 02 scaffolds, area 08 specifies) |

## Cut list after cutover (Phase 10 only)

Removed only after the parity matrix shows every row at `cut over`, the
rollback window has expired and the operator has approved (proposal §24
Phase 10, §19.2):

1. All staff Razor pages and their `.cshtml`/partials except the KEEP rows
   above (`Uploads/Request`, `Connect/Authorize`, `Error`, `StatusCode`).
2. `wwwroot/css/site.css`, `wwwroot/js/site.js`, `_LucideSprite` and the
   shell layouts.
3. `Pages/Cases/CaseMutationPageModel.cs`, `Presentation/RailCountsPageFilter.cs`,
   `Presentation/*View.cs` view models no longer referenced.
4. Browser test lane (`tests/Pegasus.IntegrationTests/Browser/`) and the
   Playwright base image pin in `Pegasus.Web.csproj` / `Directory.Build.props`
   once the Playwright renderer is also retired (ADR-0108 parity).
5. The Playwright renderer registration (`AddPegasusReportRendering`) and its
   Container App CPU/memory uplift (ADR-0028) — an Azure setting change ⚠,
   owned by area 11.

## Never cut before parity

- `Pegasus.Core`, `Pegasus.Infrastructure`, `Pegasus.Worker`, migrations.
- Identity, OpenIddict, MCP ingress, rate limiting, health endpoints.
- Any Razor page whose parity row is not yet `cut over`.
- The web-only KEEP rows.
- Azure resources (area 11 owns the deprovision checklist).
