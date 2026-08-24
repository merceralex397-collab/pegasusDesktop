# Files — FEAT-017: S17 Assessment workbench

Surveyed at `bbd1c549` (2026-08-24). Paths marked *(created by …)* do not exist yet — `ls src/`
returns only `Pegasus.Core`, `Pegasus.Infrastructure`, `Pegasus.Web`, `Pegasus.Worker`, and
`ls tests/` only `Pegasus.ArchitectureTests`, `Pegasus.Core.Tests`, `Pegasus.IntegrationTests`.

## Where the change lands

| Path | Why |
| --- | --- |
| `src/Pegasus.Contracts/` *(created by [[FND-029]] (plan handle `DSK-02-04`))* | Assessment DTOs: read model, damage save, estimate-import session, specification accept, reconcile. Money and measurement fields must be `decimal`; the accept handler takes five decimals at `Pages/Cases/Assessment/Index.cshtml.cs:481-485` and FRD-06 § `Canonical repair specifications` requires the raw calculation basis to be retained. A `double` or a pre-formatted string here is a data defect that survives into every client. |
| `src/Pegasus.Desktop/` *(created by [[FND-030]] (plan handle `DSK-02-05`))* | `AssessmentDamageViewModel` (S17a), the estimate import/accept view model (S17b) and the reconcile command (S17c), plus their XAML. One view model per screen; the workbench is one screen with three sub-slices, so S17b and S17c extend what S17a creates rather than adding siblings. |
| `src/Pegasus.Desktop.Infrastructure/` *(created by [[FND-031]] (plan handle `DSK-02-06`))* | The typed assessment client over the shared HTTP pipeline, and the estimate upload-session call reusing the transfer service from [[FEAT-014]] (plan handle `DSK-05-14`). No parsing here — the PDF is uploaded, never read. |
| `src/Pegasus.Web/` — the `/api/v1` assessment group | The five endpoints from `docs/desktop/03-gateway-api-and-data/endpoint-map.md` § `Cases`. Behind `Features:DesktopGateway`; a gated-off endpoint returns 404, so tests must enable the gate explicitly. |
| `src/Pegasus.Core/Assessment/` | **Only** for rules moved in with a characterization test first: the six page-model rules in `OnPostImportEstimateAsync` (`Index.cshtml.cs:341`, `:351`, `:356`, `:382-387`, `:388-394`, `:397`) and the three in `OnPostAcceptSpecificationAsync` (`:494`, `:504`, `:509-514`). A second implementation of any of them is a stop condition (`docs/engineering.md` § One Core owner). |
| `src/Pegasus.Web/Pages/Cases/Assessment/Index.cshtml.cs` | Re-pointed at each moved rule so exactly one implementation exists. Behaviour must not change; the existing web tests are the proof. |
| `tests/Pegasus.Core.Tests/` | The characterization facts for the nine moved rules, written **before** the move. |
| `tests/Pegasus.Api.ContractTests/` *(created by [[TEST-001]] (plan handle `DSK-08-01`))* | Per-command route evidence: success, 401, 403 (including a non-Engineer attempting acceptance), 409 stale version, `operationKey` replay, malformed estimate rejected as a problem rather than a partial import. |
| `tests/Pegasus.Desktop.ViewModelTests/` *(created by [[FND-038]] (plan handle `DSK-02-13`); catalogue by [[TEST-004]] (plan handle `DSK-08-04`))* | Local calculation matching the server response, dirty state, Engineer gating, prefill provenance, reconcile. |
| `docs/desktop/01-inventory-and-parity/parity-matrix.md` | Row `PAR-15` at `:60`, assessment portion only — [[FEAT-018]] owns the report portion of the same row. |
| `docs/frd/frd-13-desktop-operator-experience.md` *(created by area 00)* | The assessment section, citing FRD-06 and FRD-11. |
| `docs/capabilities.md` | `DSK` rows for the assessment workbench. |

## Context files

| Path | What it tells the implementer |
| --- | --- |
| `src/Pegasus.Web/Pages/Cases/Assessment/Index.cshtml.cs:16-27` | The class comment that changes the whole approach: the section forms "stay unbound design markup until the UI-15 activation task wires the staff save paths." Only the identity header, the Send-to-Claude panel, the report-draft panel and the PAV slider are bound. Do not try to reach parity with markup that does nothing; characterize the handlers. |
| `src/Pegasus.Web/Pages/Cases/Assessment/Index.cshtml.cs:583` and `:628` | The trap on this page. `OnPostSendAsync` resolves `ISendCaseToAi` (`:593`) and `OnPostReconcileAsync` resolves `IReconcileAiWorkRequest` (`:639`) — both are Send to AI (AI-09), which `reuse-map.md:38` marks "gated, out of parity scope". Neither is a characterization source. `grep -rn "OnPostSend" src/Pegasus.Web/Pages/` returns exactly one hit, so **no report-send path exists in the web app at all**. |
| `src/Pegasus.Web/Pages/Cases/Assessment/Index.cshtml.cs:184-243` | The exact write shape to reproduce: acquire a lease with a fresh operation key (`:213-215`), then call the save use case with `(id, version, actor, operationKey, reason, lease.Token, values)` where `values` is `Dictionary<string, string?>` keyed by `AssessmentVocabulary.ImpactLocation`. Also shows what *not* to carry over: `TempData["AssessmentError"]` plus `RedirectToPage` on every branch. |
| `src/Pegasus.Core/Assessment/AssessmentPolicy.cs` (499 lines) | The deterministic calculations the desktop may run locally for immediate feedback. Whatever is not here today is a page-model rule that must move here first. |
| `src/Pegasus.Core/Workflow/CaseWorkflowContracts.cs:182` | `CaseMutationRequest` — `CaseId`, `ExpectedVersion`, `ActionActor`, `OperationKey`, `Reason`, `EditLeaseToken`. Every assessment write carries this shape; the DTOs mirror it rather than inventing a second one. `CaseVersionConflictException` at `:125` is what a 409 translates from. |
| `docs/frd/frd-06-vehicle-and-engineering-evidence.md:182-205` | The binding rule for S17b: one current accepted canonical version per case; imported material stays a **draft** until an authorised Engineer accepts the exact source, mapping, ordered lines and calculation basis; corrections create a new reasoned version and never edit accepted rows in place; a case with no unambiguous accepted version **fails closed**. Also states that the three report lists are a names-only projection of the ordered lines, not a second repair specification. |
| `docs/frd/frd-04-parties-accounts-and-access.md:13-26` | The staff role access matrix and, at `:27-35`, that a permanent history write "is part of the mutable business transaction; a failed write cannot leave an unrecorded successful mutation." That is why the audit assertion belongs in the contract test, not in a follow-up. |
| `docs/desktop/03-gateway-api-and-data/endpoint-map.md` § `Cases` (Assessment rows) | Authoritative paths, the ETag/`version` concurrency tokens, and the one row whose Auth right is **Engineer** rather than `PerformCasework` — `POST /cases/{id}/assessment/specification/accept`. |
| `docs/desktop/05-implementation-and-migration/reuse-map.md` (Boundary note under `Pegasus.Core`) | The desktop may reference `Pegasus.Core` for deterministic local validation and calculation, but never `Pegasus.Infrastructure`, EF Core, or the Azure/Box/Graph SDKs — and any locally run rule is re-checked by the gateway on write. This is the licence for step 7 and the limit on it. |
| `docs/desktop/05-implementation-and-migration/README.md` § 7 ("The two giants") | Why the split into S17a/S17b/S17c exists and that it is never one PR. |
| `docs/design/README.md:412-445` | Banned operator words (`artifact`, `lease`, `projection`, … ) and the four hard rules — a field is a label and a control; no how-it-works copy; only populated sections render; filters are dropdowns. `docs/design/README.md:398-409` is the closed list of approved copy. These are merge rules with no CI enforcement. |
| `tests/Pegasus.IntegrationTests/AssessmentVehiclePrefillWebTests.cs` | The prefill path the mileage/source provenance must reproduce. Read before writing step 10. |
| `tests/Pegasus.IntegrationTests/AssessmentEstimateImportWebTests.cs`, `AssessmentDamageAndCopyWebTests.cs` | The fixtures that become the figure-for-figure comparison table, and the tests that must stay green after any rule moves into Core. |

## Ripple effects

- **`openapi/pegasus-v1.json` and the generated client.** The five new endpoints change the OpenAPI
  document; the snapshot and the generated client are regenerated in the same change, not later.
  A contract addition that is not in the snapshot is invisible to every downstream consumer.
- **`tests/Pegasus.IntegrationTests`** — the four existing assessment web tests must stay green
  after each rule moves into Core. An edited assertion there is evidence the move changed
  behaviour, not evidence the test was stale.
- **`tests/Pegasus.ArchitectureTests`** — the dependency-direction facts extended by [[FND-037]]
  (plan handle `DSK-02-12`) must stay green once `src/Pegasus.Desktop` takes a direct
  `Pegasus.Core` reference for the local calculation.
- **`src/Pegasus.Core/Assessment/`'s existing callers** — the MCP tool surface
  (`src/Pegasus.Web/Mcp/`) and the Razor page both consume these use cases. Moving a rule into Core
  changes what the MCP surface enforces too; that is intended (one policy owner) and must be
  checked, not assumed.
- **`docs/capabilities.md`, `docs/frd/frd-13-desktop-operator-experience.md`, the parity matrix** —
  documentation follows in the same slice (`docs/engineering.md` § One Core owner).
- **[[FEAT-018]]** consumes the assessment data this slice writes and shares row `PAR-15`; its
  report handlers sit in the same file, so the two slices must not both edit
  `Index.cshtml.cs` concurrently.

## Out of scope

Recorded because the ticket's Guardrails already forbid it, so the reviewer sees a decision:

- `src/Pegasus.Infrastructure/Assessment/AudatexEstimatePdfParser.cs` and everything under
  `src/Pegasus.Infrastructure/Assessment/` — the estimate parse stays a gateway upload and parse;
  the desktop never opens the PDF.
- The report handlers on the same page, `OnPostGenerateReportDraftAsync` (`:277`) and the report
  workflow generally — [[FEAT-018]].
- **Any Send-to-AI affordance.** `OnPostSendAsync` (`:583`) and `OnPostReconcileAsync` (`:628`) are
  AI-09 surfaces; `reuse-map.md:38` marks `AiWork/` out of parity scope and
  `docs/capabilities.md:269` records the reactivation condition. Nothing here reopens it.
- Upstream UI-15 (assessment workbench redesign) — stays backlog; it is the reason the current
  forms are unbound, and pulling it in would replace parity work with new design work.
- The Razor assessment page's presentation. It is re-pointed at moved rules and otherwise untouched;
  its removal belongs to [[FEAT-026]] (plan handle `DSK-05-26`) after cutover.
- Azure: no write of any kind.
