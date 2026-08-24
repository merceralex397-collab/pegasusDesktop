# Files — FEAT-042

Surveyed on 2026-08-24 at fork `main`. Paths that do not exist today carry the named ticket that
creates them; every other path was confirmed with `ls`, `wc -l` or `grep`.

## Where the change lands

| Path | Why |
| --- | --- |
| `src/Pegasus.Web/Api/V1/Cases/CaseReportsEndpoints.cs` (or the file name [[GWY-002]] (plan handle `DSK-03-02`)'s route-group skeleton establishes) | **New.** The three routes: `POST /api/v1/cases/{caseId}/reports/draft` (retained, gateway-rendered until parity), `POST /api/v1/cases/{caseId}/reports` (register final), `GET /api/v1/cases/{caseId}/reports/{reportId}/content`. Group created by [[GWY-002]]; per-group `StaffAccessRight` filter (`PerformCasework`) by [[GWY-003]] (plan handle `DSK-03-03`). Breaks if the readiness re-check is placed anywhere but inside the register handler. |
| `src/Pegasus.Contracts` — report request/response DTOs | **New types in an existing (by then) assembly** created by [[GWY-001]] (plan handle `DSK-03-01`). The register request fields (`fileName`, `sha256`, `pageCount`, `templateVersion`, `engineVersion`, `expectedVersion`, `editLeaseToken`, `operationKey`), the report-section response with issued versions, and the structured readiness-refusal payload carrying each `AssessmentReadinessItem`'s `Requirement` and `WhyOutstanding`. Problem-type URNs are **[[GWY-001]]'s list**, not defined here. |
| `src/Pegasus.Desktop/Views` + `ViewModels` — Reports tab | **New.** Generate (local render via [[FEAT-040]] (plan handle `DSK-07-14`), progress in the status bar, cancel), Preview (in-app PDF **document viewer**, never a WebView), Finalise (reasoned, idempotent). AutomationIds fixed by the screen spec: `Case.Reports.Generate`, `Case.Reports.Preview`, `Case.Reports.Send` (`docs/desktop/06-ui-design/screen-specs.md:384-386`). Project created by [[FND-030]] (plan handle `DSK-02-05`). |
| `src/Pegasus.Desktop.Infrastructure` — report upload client call | **Edit** (project created by [[FND-031]], plan handle `DSK-02-06`). Streams the rendered PDF to the register endpoint through the existing HTTP pipeline. No Box credential, no direct Box call. |
| `docs/desktop/03-gateway-api-and-data/endpoint-map.md` | **Edit** rows `:77` and `:78` — confirm the final shapes and add the readiness refusal to **both** the draft row and the register row. `:78`'s Replaces column currently reads "— (new for L-03; today the web keeps the rendered draft server-side)". |
| `docs/frd/frd-11-reports-correspondence-and-reviewed-proposals.md` | **Edit.** Add the desktop preview and finalise **behaviour** clause including the server-side readiness gate on both paths. FRD-11 owns correction and finality; this adds behaviour, never mechanism, and must not restate or contradict what is already there. |
| `docs/current-architecture.md` | **Edit, after the slice ships.** The renderer composition (desktop plus retained gateway fallback) and the report-finalise path. |
| `docs/capabilities.md` | **Edit.** A `DSK` row for local report rendering and finalise. Existing DOC/RPT/MAIL rows keep their owners. |
| `tests/Pegasus.Api.ContractTests/Cases/CaseReportsContractTests.cs` | **New** (project created by [[TEST-001]] (plan handle `DSK-08-01`) and [[GWY-004]] (plan handle `DSK-03-04`)). The nine facts of body step 12. |
| `tests/Pegasus.IntegrationTests/CaseReportFinalisePersistenceTests.cs` | **New**, following `CaseReportApprovalWebTests.cs` (236 lines) and the custody durability tests: one document version, one report record, one approval row, one action-history entry — and an interrupted upload leaving none. |
| `tests/Pegasus.Desktop.ViewModelTests/Reports/ReportsViewModelTests.cs` | **New** (project created by [[TEST-004]] (plan handle `DSK-08-04`) and [[FND-038]] (plan handle `DSK-02-13`)). Generate/preview/finalise state, cancel during render, not-ready reasons rendered as named requirements, regeneration disabled with a named reason. |
| `tests/Pegasus.Desktop.UITests` — `reports` script | **Edit** (harness created by [[TEST-006]] (plan handle `DSK-08-06`); the reports script is [[TEST-008]] (plan handle `DSK-08-08`)'s). Add the finalise assertions. |

## Context files

What the implementer must **read** first, and the specific constraint each one holds.

| Path | What it tells the implementer |
| --- | --- |
| `src/Pegasus.Core/Workflow/CaseWorkflowContracts.cs` | The definition of "final", in three records. `:63` — **"A human approval of one immutable report artifact. It does not claim the report was sent."** `:65-70` `ReportApprovalEvidence`; `:73-79` `ReportApprovalSubmission`, whose summary says the actor and time "are assigned by the authenticated mutation boundary" — so a client may never supply them. **The trap: `ArtifactIdentity` and `ArtifactSha256` are the whole binding.** Approve bytes that were not stored and the identity points at nothing. `:229-236` shows `RecordCaseReportApprovalRequest` is a `CaseMutationRequest`, so it carries version, key, reason and lease like every other case command; `:365` and `:420` are the store method and the use-case interface. |
| `src/Pegasus.Core/Lifecycle/CaseLifecycle.cs` | `:160-180` — `RecordCaseReportApproval.ExecuteAsync` calls `ValidateReportApproval` (`:167`) and then **refuses unless the case is in `ReportPreparation` or the operation key is already known** (`:168-173`, message "A report can be approved only while report preparation is active."). This is the single most surprising constraint in the ticket: an idempotent replay after the case has moved on works *only* through the `HasOperationAsync` branch. Design the replay test to hit it deliberately. `:448` `ValidateReportApproval` requires a non-empty `ApprovalId`. |
| `src/Pegasus.Core/Reports/AssessmentReportProjection.cs` | The **only** readiness rule, and the hole L-03 opened. `:306-310` `AssessmentReportDraftPreparation.CanGenerate`; `:312-322` the outcome enum and result; `:331-362` `GenerateCaseAssessmentReportDraft`, whose `ExecuteAsync` returns `NotReady` with `projected.Reasons` unless `AssessmentReportProjection.Project(input).IsReady`. Its summary (`:323-330`) records that authorisation is inherited from `IAssessmentReportProjectionSource` — "nothing new is invented here", and the same must hold for the new endpoints. **Because rendering currently sits inside `ExecuteAsync`, moving rendering to the client removes the gate unless the register path re-runs this rule itself.** |
| `src/Pegasus.Web/Pages/Cases/Assessment/Index.cshtml.cs` | The behaviour being replaced, and the standard to match. `:270-276`'s summary states the readiness contract in the repository's own words. `:277` `OnPostGenerateReportDraftAsync`; `:286-290` operation-key validation before anything else; `:295-303` the exception set that is caught into one safe message (`ReportRenderRejectedException`, `InvalidOperationException`, `IOException`, `TimeoutException`); `:310-316` the `NotReady` branch that names **every** outstanding reason as a `Requirement`/`WhyOutstanding` pair — a generic refusal in the API would be a regression against this page; `:318-319` `File(...)` with **no storage at all**, which is exactly what this ticket changes. |
| `src/Pegasus.Core/Documents/DocumentContracts.cs` | `:66-84` — `AddCaseDocumentCommand`'s field list (including `ExpectedCaseVersion` and `EditLeaseToken`, which is why finalise is a case mutation and not a file upload) and `AddCaseDocumentResult(Occurrence, Version, IsReplay)`. **`IsReplay` is the idempotency mechanism to reuse** — do not build a second one. `DocumentSemanticRole.EngineerReport` and `DocumentSource.Generated` are the classification that makes the report a normal document version rather than a special-cased blob. |
| `src/Pegasus.Core/Reports/AssessmentReportRendering.cs` | `:272-278` `RenderedReportArtifact` — the provenance the register call carries (`PageCount`, `Sha256`, `TemplateVersion`, `EngineVersion`). `:8` `TemplateVersion = "rendererref1-v1"` — assert the constant, never the literal. `:291-307` shows Core already re-hashes and throws `ReportRenderRejectedException` (`:312`) — **but once rendering is local that check runs on the client side of the wire and proves nothing about the bytes that arrived**, so the server must recompute independently. |
| `docs/desktop/03-gateway-api-and-data/endpoint-map.md:77-78` | The two rows this ticket implements, with their settled `PerformCasework` right, `yes (key)` idempotency, `CaseMutationRequest` concurrency tokens and phase 7. `:77`'s Core column already records the switchover sentence: gateway-side "until L-03 parity; then the desktop renders and `POST /cases/{id}/reports` registers the final PDF". |
| `docs/desktop/03-gateway-api-and-data/README.md:167` | The **complete** problem-type list: `validation`, `not-authorized`, `version-conflict`, `lease-conflict`, `lease-expired`, `operation-conflict`, `client-unsupported`, `password-change-required`, `account-disabled`, `provider-unavailable`, `not-found`, `rate-limited`, `maintenance`. **There is no `not-ready`.** `grep -rn "urn:pegasus:problem" src/ tests/` returns nothing today, so the list is specification, not code. Pick from it or coordinate a addition with [[GWY-001]]; never invent a URN locally. |
| `src/Pegasus.Web/Mcp/AutomationMcpErrors.cs` | The behavioural precedent area 03 says the problem types are a "port of". `:7-16`'s summary states the rule: domain exceptions carry deliberately safe messages and pass through, anything unexpected collapses to a generic failure "so no infrastructure detail crosses the boundary", and the edit-guard refusals name the current case version so the caller can reload rather than retry blindly. `:30-67` shows the four exception types and their messages. Match this vocabulary; do not invent new codes. |
| `docs/desktop/06-ui-design/screen-specs.md:371-386` | § 13.9. The Reports bullet (`:379-383`) fixes four things this ticket cannot renegotiate: Generate is a command with status-bar progress and cancel; **Preview is "a document viewer, not Pegasus UI in a WebView"**; issued versions show custody and sent evidence **separately**; regeneration rules appear as "enabled/disabled named conditions" — named, not a greyed button with no reason. AutomationIds at `:384-386`. |
| `docs/desktop/07-integrations/README.md` § 4 and § 7 | § 4's fourth Target-state bullet is this ticket's outcome sentence: reports "are finalised by uploading the PDF through the gateway into Box with the report record and audit … the gateway renderer remains only as the recorded fallback until parity is signed off." § 7's trap rows include "Custody retry automated 'for convenience'" — forbidden, human-only per `docs/current-architecture.md:571` — and the runtime-role GRANT trap. |
| `tests/Pegasus.IntegrationTests/CaseReportApprovalWebTests.cs` | 236 lines. The approval-path precedent: how a case is driven into an approvable state, how the actor is established, and what the assertions look like. Follow its shape rather than inventing one. |
| `tests/Pegasus.IntegrationTests/Reports/AssessmentReportDraftWebTests.cs` | 259 lines. The draft-path precedent, including how a not-ready case is arranged — reuse that arrangement for the register-path refusal test rather than building a second one. |
| `docs/engineering.md:72-88` | Tier 5 is "Web/API/MCP caller — actual routes reach Core; authentication, antiforgery, validation, scope, **idempotency**, exception translation, and the **action-history actor** are observable". Both italicised words are acceptance criteria here, not nice-to-haves. |
| `HZN-001` / `board-conventions.md` § "Upstream ids versus board ids" | The join table. **This ticket sits on the board's worst id collision**: board `DOCS-003` is upstream TICK-208 (this ticket's ledger dependency), upstream `DOCS-003` is an unrelated post-alpha RPT-04 gate with no fork ticket, board `DOCS-002` is upstream TICK-018, and board `DOCS-001` matches its upstream id **by coincidence** — which the conventions document calls out as the trap in the table. Always write `upstream <ID> (board [[<board-id>]])`. |
| `scripts/Test-MigrationGrants.ps1` | Exists. Any new table needs a runtime-role `Grant*` migration that this script checks (PLAT-035). Assumption `A-07-16-1` says no new table is needed; if that fails, this script is the thing that will catch it, in CI, late. |

## Ripple effects

- **Contract and generated client.** New DTOs and routes mean `openapi/pegasus-v1.json` is
  regenerated by [[GWY-004]]'s snapshot job and the Kiota client by [[GWY-005]] (plan handle
  `DSK-03-05`). This is a real ripple on this board and must land in the same PR, with
  `git diff --exit-code openapi/pegasus-v1.json` clean after regeneration.
- **Tests.** Contract tests (new project), integration tests following two existing precedents,
  view-model tests, and the `winapp ui` reports script owned by [[TEST-008]].
- **Documentation.** Four files: the endpoint map's two rows, FRD-11's behaviour clause,
  `docs/current-architecture.md` after the slice ships, and a `DSK` row in `docs/capabilities.md`.
- **Parity matrix.** `PAR-15` (`docs/desktop/01-inventory-and-parity/parity-matrix.md:60`) advances
  as the slice lands; the row's status ladder step is owned by [[FEAT-025]] (plan handle
  `DSK-05-25`) as matrix maintenance, so coordinate rather than editing the status unilaterally.
- **Custody.** The register path uses [[FEAT-031]] (plan handle `DSK-07-05`)'s Box broker upload;
  a Box-side failure must leave no half-registered report, which is what the interrupted-upload
  integration test asserts.
- **The gateway renderer stays registered.** `AddPegasusReportRendering`
  (`src/Pegasus.Infrastructure/DependencyInjection.cs:446`) is **not** removed; the draft route
  keeps returning gateway-rendered bytes until [[FEAT-041]] (plan handle `DSK-07-15`)'s results
  table signs parity off. Removing it is the Guardrail this ticket is most likely to trip.
- **Sequencing.** Body step 11's separately-shown issued versions and Sent evidence depend on
  **upstream TICK-208 (board [[DOCS-003]])**'s append-only ledger; that ticket is already imported
  and must be **found by board id, not created** ([[FEAT-043]] (plan handle `DSK-07-17`) step 7).
- **No migration, if `A-07-16-1` holds.** If it does not, a runtime-role `Grant*` migration and
  `scripts/Test-MigrationGrants.ps1` enter scope and the diff estimate is wrong.

## Out of scope

Recorded because the ticket's Guardrails already forbid each one.

- **Removing the gateway renderer registration.** L-03 keeps it until [[FEAT-041]] passes; the flag
  and its authority are recorded, not exercised, here.
- **Changing FRD-11's finality or correction rules.** FRD-11 owns them; this ticket adds a
  behaviour clause and enforces the existing rules server-side.
- **A second readiness implementation.** `AssessmentReportProjection` in `src/Pegasus.Core` is the
  only one. A client-side check is explicitly not a gate.
- **A second finality concept.** The approval *is* finality; do not add a "final" flag to the
  document version.
- **Giving the desktop a Box credential.** ADR-0107 and the Guardrail. The PDF reaches custody only
  through the gateway.
- **Building the append-only issued-version ledger.** That is **upstream TICK-208 (board
  [[DOCS-003]])**'s, and faking the pair over the single-slot `ReportApprovalId` /
  `ReportSentEvidenceId` is named as a trap.
- **Asserting that a report was sent.** Only retained Sent evidence proves a send; the outbound
  seam is [[FEAT-037]] (plan handle `DSK-07-11`)'s.
- **Hosting Pegasus UI in a WebView for preview.** Proposal § 23.2 and ADR-0108 permit exactly one
  isolated, non-UI WebView2 — [[FEAT-040]]'s renderer. Preview is a document viewer.
- **Automating custody retry.** Human-only (`docs/current-architecture.md:571`); the area plan's
  § 7 names automating it "for convenience" as a trap.
- **Any Azure write.** Guardrail: "Azure: no write."
- **Deciding automatic versus operator-initiated generation.** That is the binding open question
  recorded in this ticket's `open-questions` document, and the body forbids resolving it by
  inventing a hybrid.
