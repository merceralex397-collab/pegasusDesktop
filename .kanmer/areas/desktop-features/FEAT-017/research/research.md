# Research — FEAT-017: S17 Assessment workbench

Repository revision read: `bbd1c549` (`git rev-parse --short HEAD`, 2026-08-24). Every line
number below was produced by `grep -n` or `sed -n` at that revision.

## Question

Which assessment behaviours live in `src/Pegasus.Web/Pages/Cases/Assessment/Index.cshtml.cs`
rather than in `src/Pegasus.Core/Assessment/`, what each in-scope handler requires on the wire,
where the mileage/source prefill comes from — and, because two of the seven handlers on that page
turn out not to be assessment handlers at all, which of them is a real characterization source for
this slice.

## Current behaviour

The web does this through one page model: `src/Pegasus.Web/Pages/Cases/Assessment/Index.cshtml.cs`,
740 lines (`wc -l`). Its seven handlers, with the line each starts on:

| Handler | Line | Owner |
| --- | --- | --- |
| `OnPostSaveDamageAsync` | `:184` | this ticket (S17a) |
| `OnGetAsync` | `:246` | this ticket (read model) |
| `OnPostGenerateReportDraftAsync` | `:277` | [[FEAT-018]] (plan handle `DSK-05-18`) |
| `OnPostImportEstimateAsync` | `:330` | this ticket (S17b) |
| `OnPostAcceptSpecificationAsync` | `:476` | this ticket (S17b) |
| `OnPostSendAsync` | `:583` | **not a report send — see Findings** |
| `OnPostReconcileAsync` | `:628` | **not an assessment reconcile — see Findings** |

Core policy: `src/Pegasus.Core/Assessment/AssessmentPolicy.cs` (499 lines), beside
`AssessmentContracts.cs` (297), `RepairSpecifications.cs` (232), `EstimateImport.cs` (42) and
`AssessmentOperations.cs` (37) — 1,107 lines in the folder.

Estimate parsing is server-side only: `src/Pegasus.Infrastructure/Assessment/AudatexEstimatePdfParser.cs`
(628 lines), reached through the `IEstimateDocumentParser` port.

Parity-matrix row: **`PAR-15`** at `docs/desktop/01-inventory-and-parity/parity-matrix.md:60` —
"13.9 Assessment and reporting", FRD-11 + FRD-06, current status `inventoried`. That single row
covers both this slice and [[FEAT-018]], which is why both ticket bodies scope themselves to a
*portion* of it. The matrix holds 46 `PAR-` rows (`grep -c '^| PAR-' … → 46`), all keyed to page
models under `src/Pegasus.Web/Pages/**`.

## Findings

- **The web assessment entry forms are not wired.** The class documentation comment at
  `Index.cshtml.cs:16-27` states that this model binds only "the case identity header, the Send to
  Claude panel, the report-draft panel, and the PAV slider's recorded-evidence data; the section
  forms themselves stay unbound design markup until the UI-15 activation task wires the staff save
  paths."
  - Consequence for parity: the comparison baseline for S17 is **the handlers plus
    `AssessmentPolicy`**, exercised through the integration tests, not the rendered page. An agent
    who tries to reach visual/behavioural parity with the current markup will be matching markup
    that does nothing. Upstream UI-15 stays backlog (ticket Guardrails) and must not be pulled in.
- **`OnPostSendAsync` (`:583`) is Send to Claude, not "send the report."** It resolves
  `ISendCaseToAi` (`Index.cshtml.cs:593`) and composes the prompt "Work the assessment for case
  {reference} in Pegasus…" (`:611-614`). `grep -rn "OnPostSend" src/Pegasus.Web/Pages/` returns
  exactly one hit — this one. **There is no implemented report-send path anywhere in the web
  application.**
- **`OnPostReconcileAsync` (`:628`) is the Send-to-AI work reconcile, not an assessment reconcile.**
  It resolves `IReconcileAiWorkRequest` (`:639`), takes a `requestId`, and reports
  `AiWorkRequestState.Completed / Failed / Expired` (`:655-664`).
- Send to AI is a **recorded exclusion with a reactivation condition**, not open scope:
  `docs/desktop/05-implementation-and-migration/reuse-map.md:38` marks `AiWork/` "gated, out of
  parity scope"; `src/Pegasus.Web/AiWork/SendToAi.cs:12` defines `Features:SendToAi` and `:35-42`
  refuses composition outside the `DevelopmentOffline` runtime profile; `docs/capabilities.md:269`
  records that production activation needs a separate non-preview transport decision. Neither
  handler is therefore a parity source, and this slice ships no Send-to-AI affordance.
- **Real business rules live in the page model, not in Core**, and step 4 of the ticket exists to
  move them. Measured in `OnPostImportEstimateAsync`:
  - Engineer-only gate — `if (!actor.IsInRole(StaffRole.Engineer))` at `:341`;
  - upload ceiling — `MaximumEstimateUploadBytes = 10 * 1024 * 1024` at `:45`, enforced at `:351`;
  - PDF-only gate — `estimateParser.CanParse(fileName, contentType)` at `:356`;
  - "a draft repair specification already exists → refuse another import" at `:382-387`;
  - "an accepted specification exists → a reason is required for a correcting import" at `:388-394`;
  - artifact identity minted as `$"estimate-import:{operationKey}"` at `:397`.
  And in `OnPostAcceptSpecificationAsync`: Engineer-only at `:494`; a
  `repairerVatRegistered is not ("true" or "false")` validation at `:504`; and a re-read of the
  current draft with a `draft.SpecificationId != specificationId` staleness check at `:509-514`.
  The money fields (`labour`, `parts`, `paintMaterials`, `specialistOther`, `vat`) arrive as
  `decimal` handler parameters (`:481-485`).
- **Every write already carries the remote-API shape.** `OnPostSaveDamageAsync` acquires a lease
  (`acquireLease.ExecuteAsync(new(id, details.Workflow.Version, actor, NewOperationKey())` at
  `:213-215`) and then calls the save use case with
  `(id, version, actor, operationKey, reason, lease.Token, values)` at `:216-228`, where `values`
  is a `Dictionary<string, string?>` keyed by `AssessmentVocabulary.ImpactLocation`.
  `CaseMutationRequest` (`src/Pegasus.Core/Workflow/CaseWorkflowContracts.cs:182`) is the same
  shape the gateway will use.
- **PRG and `TempData` carry every outcome today** — `TempData["AssessmentError"]` /
  `TempData["AssessmentStatus"]` then `RedirectToPage(...)` on all paths. These are web mechanics
  and are explicitly not preserved (`docs/desktop/05-implementation-and-migration/README.md` § 3);
  [[FEAT-024]] (plan handle `DSK-05-24`) enforces their absence from the desktop.
- Endpoint shapes are already settled by
  `docs/desktop/03-gateway-api-and-data/endpoint-map.md` § `Cases` (Assessment rows):
  `GET /cases/{id}/assessment` (ETag + `version`), `POST …/assessment/damage`,
  `POST …/assessment/estimate-import` (upload session), `POST …/assessment/specification/accept`
  (**auth right: Engineer**, the only Engineer-gated row in that table), and
  `POST …/assessment/send`, `/reconcile`. All but the read are "yes (key)" idempotent and carry
  `CaseMutationRequest` fields.
- FRD-06 § `Canonical repair specifications` (`docs/frd/frd-06-vehicle-and-engineering-evidence.md:182-205`)
  is the binding rule for S17b: every accepted specification is an immutable versioned Core
  aggregate, one current accepted version per case; imported or automated material "remains a draft
  until an authorised Engineer accepts the exact source, mapping, ordered lines, and calculation
  basis"; corrections create a new reasoned version and never edit accepted rows in place; a case
  with no unambiguous current accepted version fails closed.
- Existing test evidence to keep green and to characterize against:
  `tests/Pegasus.IntegrationTests/AssessmentDamageAndCopyWebTests.cs`,
  `AssessmentEstimateImportWebTests.cs`, `AssessmentPersistenceIntegrationTests.cs`,
  `AssessmentVehiclePrefillWebTests.cs` (all present, `ls tests/Pegasus.IntegrationTests/`).

### Facts

Verified by reading the repository at `bbd1c549`.

- `Index.cshtml.cs` is 740 lines; the seven handler line numbers are as tabulated above (`grep -n`).
- `AssessmentPolicy.cs` is 499 lines; `AudatexEstimatePdfParser.cs` is 628 lines (`wc -l`).
- The page model's injected dependencies include `IGetCaseAssessment`, `IRepairSpecificationStore`,
  `IEstimateDocumentParser`, `IAddCaseDocument`, `IAcquireCaseEditLease`,
  `GenerateCaseAssessmentReportDraft`, `IAiWorkRequestStore`, `ISendToAiControl`
  (`Index.cshtml.cs:31-40`).
- The class is `[Authorize(Roles = Administrator, Engineer, User)]` at `:28-29`; the Engineer-only
  restrictions are per-handler, not class-level.
- `src/Pegasus.Desktop`, `src/Pegasus.Desktop.Infrastructure`, `src/Pegasus.Contracts`,
  `tests/Pegasus.Desktop.ViewModelTests` and `tests/Pegasus.Api.ContractTests` **do not exist yet**
  (`ls src/`, `ls tests/`, `cat Pegasus.slnx`). They are created by area 02 and area 08 tickets.

### Assumptions

- `A-05-17-1` — the mileage/source prefill path is the one exercised by
  `AssessmentVehiclePrefillWebTests.cs` and originates in accepted lookup evidence from
  [[FEAT-015]] (plan handle `DSK-05-15`). *Confirm:* read that test file and the vehicle-evidence
  query it drives at implementation step 3. *If wrong:* the provenance glyph would attribute a
  value to the wrong source, which the design authority treats as a defect, not a cosmetic issue.
- `A-05-17-2` — the deterministic parts of `AssessmentPolicy` can be executed in-process by
  `src/Pegasus.Desktop` through a direct `Pegasus.Core` project reference. *Confirm:* the boundary
  note in `reuse-map.md` ("Boundary note (proposal §5.3)") permits it; the dependency-direction
  facts extended by [[FND-037]] (plan handle `DSK-02-12`) are the enforcement. *If wrong:* local
  calculation is dropped and every figure round-trips to the gateway — slower, still correct.
- `A-05-17-3` — `GWY-014` publishes the assessment endpoints with the exact paths in the endpoint
  map. *Confirm:* read [[GWY-014]]'s merged contract before writing the client. *If wrong:* only
  the DTO/route names in `src/Pegasus.Contracts` change.
- `A-05-17-4` — the "approved fixture set" the acceptance criterion names is the fixture data
  behind `AssessmentDamageAndCopyWebTests.cs` and `AssessmentEstimateImportWebTests.cs`; the ticket
  body names those two files and no separate fixture catalogue exists in the repository.
  *Confirm:* enumerate the fixtures in those tests at step 13. *If wrong:* the comparison table has
  the wrong rows and the Phase 7 exit gate is not actually proven.

## Execution placement

The six-question cloud-justification test from
`docs/desktop/00-governance-and-workflow/README.md` § 3 (questions at `:169-176`), answered for
the assessment workbench:

| Question | Answer | Evidence |
| --- | --- | --- |
| Shared authority — must several users see and update the same state? | **yes** | An assessment is case state; `CaseMutationRequest` carries `ExpectedVersion` and `EditLeaseToken` (`CaseWorkflowContracts.cs:182`) precisely because two operators can reach it. Lands in the gateway (`Pegasus.Web`, L-01). |
| Unattended execution — must it run with every desktop closed? | **no** | Every in-scope handler is operator-initiated; nothing in `src/Pegasus.Worker` touches the assessment path. |
| Protected credentials — long-lived secret that must not sit on workstations? | **no** | The four in-scope handlers use no provider credential. (Estimate *parsing* is server-side for a different reason — see the last row.) |
| Public callback — must an external service call a stable public endpoint? | **no** | No inbound callback exists on this surface. |
| Central enforcement — revocation, permissions, audit, invariant independent of the client? | **yes** | Engineer-only acceptance (`Index.cshtml.cs:494`), FRD-06's "fails closed" rule for an ambiguous accepted version, and the permanent action history required by FRD-04 § `Permanent action history` are all client-independent. Lands in the gateway. |
| Measured operational advantage — measured evidence central is materially better? | **yes, for the estimate parse only** | `AudatexEstimatePdfParser.cs` is 628 lines of PDF parsing with bounded limits; it stays behind the `IEstimateDocumentParser` port in `Pegasus.Infrastructure`, which the desktop is forbidden to reference (`reuse-map.md` boundary note). The desktop uploads the PDF and never parses it. |

Two "yes" answers name **the gateway** as the responsibility's home, not Azure — the gateway is the
existing `Pegasus.Web` Container App under L-01 and no new Azure resource is implied. Everything
else — damage entry, immediate validation, the deterministic `AssessmentPolicy` figures shown while
the operator types — runs on the desktop, and every locally computed figure is re-checked by the
gateway inside the write transaction.

## Implications

1. **Characterize the handlers, not the page.** Because the section forms are unbound markup
   (`Index.cshtml.cs:16-27`), the tests written at step 4 must drive the handlers and
   `AssessmentPolicy` directly. "Compare with the rendered web screen" is not available evidence.
2. **The rules to move into Core are enumerable now** — the six in `OnPostImportEstimateAsync` and
   the three in `OnPostAcceptSpecificationAsync` listed under Findings. Each gets a characterization
   fact in `tests/Pegasus.Core.Tests` **before** it moves, and the Razor page is re-pointed at the
   moved rule so no second implementation exists (`docs/engineering.md` § One Core owner).
3. **S17c "reconcile" has no web implementation to copy.** `OnPostReconcileAsync` reconciles a
   Send-to-AI work request, which is out of parity scope. S17c must therefore be specified from the
   assessment domain and the endpoint-map row (`POST /cases/{id}/assessment/reconcile` → "send /
   reconcile commands (`Assessment/`, `Workflow/`)") with [[GWY-014]], not characterized from
   `:628`. This is recorded in the plan's *Risks / open questions* with its owner; it is not an open
   question on this ticket, because [[GWY-014]] owns the endpoint definition and a decision a named
   sibling owns is a scope boundary.
4. **The three-PR split is load-bearing, not ceremony.** S17a touches one field and one command;
   S17b carries the upload session, the Engineer gate and the FRD-06 acceptance aggregate; S17c is
   the smallest and the least defined. Landing them together would put an undefined command in the
   same PR as the acceptance aggregate.
5. **Money must not round on the wire.** The accept handler takes five `decimal` parameters
   (`:481-485`) and FRD-06 requires the "raw calculation basis and totals" to be retained; the
   contracts DTOs must carry `decimal`, never `double` or a formatted string.

## Open questions

None that belong in an `open-questions` document. The two items a reader might expect there are
both settled elsewhere and are recorded in the plan instead:

- The meaning of an assessment *reconcile* command — owned by [[GWY-014]] (plan handle
  `DSK-03-14`), which publishes the endpoint. A decision a named sibling ticket owns is a scope
  boundary, not an open question.
- Send to AI — a recorded exclusion with a reactivation condition (`docs/capabilities.md:269`),
  settled by the operator on 2026-08-24. No question is opened for it on any ticket.
