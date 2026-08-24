---
id: FEAT-017
type: ticket
title: DSK-05-17 · S17 Assessment workbench
status: backlog
area: desktop-features
assignee: ''
profile: feature
labels:
  - desktop-conversion
  - plan-05
  - phase-7
  - tier-2
  - tier-5
  - tier-7
  - needs-operator
groups:
  - EPIC-006
  - HZN-008
links: []
blocks:
  - FEAT-018
  - FEAT-022
  - FEAT-025
  - TEST-016
refs:
  - docs/frd/frd-06-vehicle-and-engineering-evidence.md
  - docs/frd/frd-11-reports-correspondence-and-reviewed-proposals.md
docs_todo: true
archived: false
created: '2026-08-24T07:57:08.732Z'
updated: '2026-08-24T08:51:35.327Z'
---

## What

Deliver the native assessment workbench in three sub-slices — S17a record damage, S17b import an estimate and accept the repair specification, S17c reconcile — with mileage and source prefilled from lookup evidence, deterministic calculations run locally through `Pegasus.Core` and re-checked by the gateway on every write.

## Why

Proposal §13.9 requires the current data-entry and calculation workflows, deterministic business rules and repair/valuation information to move across intact. Today they sit in the second-largest page model in the repository: `src/Pegasus.Web/Pages/Cases/Assessment/Index.cshtml.cs` (740 lines) with `OnPostSaveDamageAsync` at `:184`, `OnPostImportEstimateAsync` at `:330`, `OnPostAcceptSpecificationAsync` at `:476` and `OnPostReconcileAsync` at `:628` (the report handlers at `:277` and `:583` belong to [[DSK-05-18]]). The policy lives in `src/Pegasus.Core/Assessment/AssessmentPolicy.cs` (499 lines) and estimate parsing in `src/Pegasus.Infrastructure/Assessment/AudatexEstimatePdfParser.cs` — which stays server-side. The Phase 7 exit gate requires approved fixtures to match expected values. Siblings: [[DSK-05-05]] supplies the case session, [[DSK-03-14]] the endpoints, [[DSK-05-18]] follows with the report.

## Source of truth

- Plan row: `docs/desktop/05-implementation-and-migration/README.md` § 5 — `DSK-05-17`
- Plan detail: `docs/desktop/05-implementation-and-migration/vertical-slices.md` § `S17 · Assessment workbench (DSK-05-17)`; § 7 of `README.md` ("The two giants" — split into S17a damage, S17b estimate import/accept, S17c reconcile, never one PR)
- Endpoint map: `docs/desktop/03-gateway-api-and-data/endpoint-map.md` § `Cases` (Assessment rows)
- Screen spec: `docs/desktop/06-ui-design/screen-specs.md` § `§13.9 Assessment, valuation and reporting — Case workspace › Assessment and Reports tabs`
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 13.9 Assessment, valuation and reporting, § 4.1 placement decisions
- Repository evidence: `src/Pegasus.Web/Pages/Cases/Assessment/Index.cshtml.cs:184`, `:246`, `:330`, `:476`, `:628`; `src/Pegasus.Core/Assessment/AssessmentPolicy.cs` (499 lines), `ICaseAssessmentStore`, `IEstimateDocumentParser`; `src/Pegasus.Infrastructure/Assessment/AudatexEstimatePdfParser.cs` (628 lines, server-side); `tests/Pegasus.IntegrationTests/AssessmentDamageAndCopyWebTests.cs`, `AssessmentEstimateImportWebTests.cs`, `AssessmentPersistenceIntegrationTests.cs`, `AssessmentVehiclePrefillWebTests.cs`
- Binding decisions: L-01 the authoritative write and the estimate parse stay in the gateway; L-02 fixture comparison runs on the local Test/UAT stack; L-04 routing named on the ticket
- Depends on: `DSK-05-05` the case lease and version session; `DSK-03-14` the assessment get/save/import/accept/reconcile endpoints

## Routing

- **Subagent**: `winui-dev` — `.codex/agents/winui-dev.toml`; `pegasus-gateway-dev` — `.codex/agents/pegasus-gateway-dev.toml`; `pegasus-test-engineer` — `.codex/agents/pegasus-test-engineer.toml`
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `code-testing-agent` (dotnet/skills `98f84851`, `plugins/dotnet-test/skills/code-testing-agent/SKILL.md`) → `dotnet-webapi` (dotnet/skills `98f84851`) → `winui-dev-workflow` (`.codex/skills/winui-dev-workflow/SKILL.md`) → `winui-design` (`.codex/skills/winui-design/SKILL.md`) → `run-tests` → `winui-code-review` at review
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search`)
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (call `get_doc_gates <id>` before every move; a move crosses at most one gated boundary)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Orient: read the plan row, `vertical-slices.md` § S17, the screen spec assessment section, `docs/frd/frd-06-vehicle-and-engineering-evidence.md` and `docs/frd/frd-11-reports-correspondence-and-reviewed-proposals.md`. Call `get_doc_gates <this ticket id>`, then `take_ticket` with branch `task/dsk-05-17a-assessment-damage` and worktree `../pegasus-worktrees/dsk-05-17a-assessment-damage` from `origin/dev`.
2. Plan the split explicitly in the ticket plan: **S17a** record damage; **S17b** estimate import and specification acceptance; **S17c** reconcile. Each is its own branch, commit series and PR into `dev`; the plan records the order and the checkpoint after each. Never land the workbench as one PR.
3. Read `src/Pegasus.Web/Pages/Cases/Assessment/Index.cshtml.cs` in full and tabulate in `research` the four in-scope handlers with their Core calls, the required `expectedVersion` / `operationKey` / `editLeaseToken`, and — crucially — which calculations are performed in the page model versus in `src/Pegasus.Core/Assessment/AssessmentPolicy.cs`. Record the mileage and source prefill path from the lookup evidence (`AssessmentVehiclePrefillWebTests.cs`). Record the SHA read.
4. Load `code-testing-agent` and close the characterization gap the plan names: write tests in `tests/Pegasus.Core.Tests` for the save/import/reconcile rules against current behaviour **before** moving anything, then move any page-model calculation into `AssessmentPolicy` and re-point the Razor page. A second implementation of a calculation is a stop condition.
5. Confirm the endpoints from [[DSK-03-14]]: `GET /api/v1/cases/{id}/assessment`, `POST …/assessment/damage`, `POST …/assessment/estimate-import` (an upload session, since the parse is server-side), `POST …/assessment/specification/accept` (Engineer role), `POST …/assessment/reconcile`. Every write carries the case version and operation key and the gateway re-checks the calculation inside the transaction.
6. Add the assessment DTOs to `src/Pegasus.Contracts`, keeping monetary and measurement values in their exact representation — no lossy rounding on the wire.
7. **S17a** — implement `AssessmentDamageViewModel` in `src/Pegasus.Desktop`, running the deterministic `AssessmentPolicy` calculations locally through a direct `Pegasus.Core` reference (permitted by the boundary note in `reuse-map.md`) for immediate feedback, with the authoritative figure always coming back from the save response.
8. **S17b** — implement estimate import as an upload session reusing the transfer service from [[DSK-05-14]]; the desktop never parses the estimate PDF. Show the imported lines and let the Engineer accept the specification; the Engineer-only confirmation is enforced server-side and merely reflected in the UI.
9. **S17c** — implement reconcile as an explicit command with the reason dialog where Core requires one, surfacing the shared conflict pattern from [[DSK-05-08]] on 409.
10. Prefill mileage and its source from the accepted lookup evidence produced by [[DSK-05-15]], showing the provenance glyph and the obtained-at value beside the figure; never present a prefilled value as keyed by the operator.
11. Add contract tests in `tests/Pegasus.Api.ContractTests` for each command: success, 401, 403 (including a non-Engineer attempting acceptance), 409 stale version, replay of the same `operationKey`, and a malformed estimate rejected with a problem rather than a partial import. Enable `Features:DesktopGateway` explicitly.
12. Add view-model tests in `tests/Pegasus.Desktop.ViewModelTests` for local calculation matching the server response, dirty state, Engineer gating, prefill provenance and reconcile.
13. Run the fixture comparison: for the approved fixture set behind `AssessmentDamageAndCopyWebTests.cs` and `AssessmentEstimateImportWebTests.cs`, every figure produced on the desktop must equal the web figure. Record the table in the ticket proof.
14. **Operator step** — UAT by a qualified Engineer on the local Test/UAT stack across damage, import, accept and reconcile. Capture the Engineer's sign-off text and date in the ticket proof. Then update `docs/desktop/01-inventory-and-parity/parity-matrix.md` row `PAR-15` (assessment part), add the assessment section to `docs/frd/frd-13-desktop-operator-experience.md`, run the simplification pass over each sub-slice diff under a dated `## Simplification pass` heading, and open the PRs in S17a → S17b → S17c order.

## Acceptance criteria

- [ ] Damage can be recorded, an estimate imported, a specification accepted and the assessment reconciled, natively.
- [ ] Figures equal the web for the approved fixture set.
- [ ] Deterministic calculations run locally through `AssessmentPolicy` and are re-checked by the gateway on write; there is no second calculation implementation.
- [ ] Engineer-only confirmations are enforced server-side, not merely hidden in the UI.
- [ ] Mileage and source are prefilled from lookup evidence with visible provenance.
- [ ] The workbench ships as three PRs (S17a, S17b, S17c), never one.

## Verification

- [ ] `dotnet test ./tests/Pegasus.Core.Tests/Pegasus.Core.Tests.csproj --configuration Release --no-build` — expected: the assessment characterization facts pass and existing policy facts stay green.
- [ ] `dotnet test ./tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj --configuration Release --no-build` — expected: damage, import, accept and reconcile facts pass including the non-Engineer 403.
- [ ] `dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release --no-build` — expected: calculation, gating and prefill facts pass.
- [ ] `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --no-build --filter "Category!=Corpus&Category!=Browser"` — expected: existing assessment web tests stay green after any rule moves into Core.
- [ ] Fixture table and UAT record in the ticket proof — expected: figure-for-figure equality with the web, and Engineer sign-off with date.

## Evidence tier

Tier 2 — Core/domain. Tier 5 — Web/API/MCP caller. Tier 7 — Browser/accessibility.
Tier 2 obliges positive, contradictory, ambiguous and failure cases for the assessment rules before any calculation moves; tier 5 obliges route-level evidence per command with authorization, idempotency and exception translation; tier 7 obliges keyboard, focus, validation-summary and semantic-label evidence from a real run.

## Documentation changes

- `docs/desktop/01-inventory-and-parity/parity-matrix.md` — row `PAR-15` assessment portion
- `docs/frd/frd-13-desktop-operator-experience.md` — assessment section, citing FRD-06 and FRD-11
- `docs/capabilities.md` — `DSK` rows for the assessment workbench

## Guardrails

- **Azure**: no write.
- **Scope boundary**: may touch `src/Pegasus.Desktop`, `src/Pegasus.Desktop.Infrastructure`, `src/Pegasus.Contracts`, the `/api/v1` assessment group in `src/Pegasus.Web`, `src/Pegasus.Core/Assessment/` only for calculations moved in with a characterization test, and the test projects. Must not reference `src/Pegasus.Infrastructure/Assessment/` — estimate parsing stays a gateway upload and parse.
- **Traps**: the two giants — `Assessment/Index.cshtml.cs` is 740 lines and is split into S17a/S17b/S17c, never one PR; a calculation found only in the page model moves into Core with a test first, and a second implementation is a stop condition; the desktop must not parse the estimate PDF; upstream UI-15 (workbench redesign) stays backlog and must not be pulled in; `Features:DesktopGateway` must be enabled in tests; the report handlers on the same page belong to [[DSK-05-18]] and are out of scope here.
- **Simplification pass** (`AGENTS.md` step 4): required over each sub-slice branch diff before its PR, recorded under a dated `## Simplification pass` heading in the plan document.

## Outcome

_Filled at closeout._
