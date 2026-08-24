---
id: FEAT-004
type: ticket
title: DSK-05-04 · S4 Case create
status: backlog
area: desktop-features
assignee: ''
profile: feature
labels:
  - desktop-conversion
  - plan-05
  - phase-4
  - tier-2
  - tier-5
  - tier-7
  - needs-operator
groups:
  - EPIC-006
  - HZN-005
links: []
refs:
  - docs/frd/frd-01-case-identity-and-lifecycle.md
  - docs/frd/frd-02-intake-and-source-identity.md
docs_todo: true
archived: false
created: '2026-08-24T07:46:33.829Z'
updated: '2026-08-24T07:46:33.829Z'
---

## What

Deliver the native case-create screen: create a case from a received instruction draft or from blank, settle the inspection address, show provenance beside every field, and obtain the allocated reference — with the draft-to-case mapping first characterized in `Pegasus.Core` so no business rule stays behind in a page model.

## Why

Proposal §13.3 and §13.4 make create the one place typed draft values are editable, with candidates and provenance beside each box and a keyed value becoming a staff-sourced candidate. Today that lives in `src/Pegasus.Web/Pages/Cases/Create.cshtml.cs` (689 lines, `OnGetAsync` and `OnPostCreateAsync`) and its state survives round-trips through cookie `TempData` in `src/Pegasus.Web/Pages/Cases/CaseMutationPageModel.cs:36-80` (8000/2000-character budgets and a `RetainableFormFields` allow-list) — web mechanics the desktop must not reproduce. Allocation outcomes must match the web exactly or the parity matrix cannot advance. Siblings: [[DSK-05-03]] provides the workspace this lands in, [[DSK-03-08]] the create endpoint, [[DSK-01-12]] the characterization gap list.

## Source of truth

- Plan row: `docs/desktop/05-implementation-and-migration/README.md` § 5 — `DSK-05-04`
- Plan detail: `docs/desktop/05-implementation-and-migration/vertical-slices.md` § `S4 · Case create (DSK-05-04)`; § 3 of `README.md` ("Characterization before moving any rule", create-screen draft-to-case mapping listed as a gap to close in S4)
- Endpoint map: `docs/desktop/03-gateway-api-and-data/endpoint-map.md` § `Cases` (`POST /cases`) and § `Intake (received items), uploads, image intake` (draft read)
- Screen spec: `docs/desktop/06-ui-design/screen-specs.md` § `§13.3 Case lifecycle` → `Case create`
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 13.3, § 13.4, § 11.1 local state, § 22.1 characterization before refactoring
- Repository evidence: `src/Pegasus.Web/Pages/Cases/Create.cshtml.cs` (689 lines), `src/Pegasus.Web/Pages/Cases/CaseMutationPageModel.cs:36-80`, `src/Pegasus.Web/Presentation/InstructionDraftFieldsView.cs` (64 lines), `src/Pegasus.Core/Intake/IntakeAllocation.cs:208` (`IAllocateIntake`), `src/Pegasus.Core/Address/` (`Ext18InspectionAddressPolicy`), `tests/Pegasus.IntegrationTests/QdosIntakeWebTests.cs`, `tests/Pegasus.IntegrationTests/QdosAllocationRecoveryTests.cs`
- Binding decisions: L-01 gateway evolves inside `Pegasus.Web` and allocates the reference; L-02 the genuine-corpus run is local only; L-04 routing named on the ticket
- Depends on: `DSK-05-03` the case workspace shell; `DSK-03-08` the idempotent `POST /api/v1/cases` create command; `DSK-01-12` the characterization-test gap list for Core policies

## Routing

- **Subagent**: `winui-dev` — `.codex/agents/winui-dev.toml`; `pegasus-gateway-dev` — `.codex/agents/pegasus-gateway-dev.toml`; `pegasus-test-engineer` — `.codex/agents/pegasus-test-engineer.toml` (characterization tests first)
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `code-testing-agent` (dotnet/skills `98f84851`, `plugins/dotnet-test/skills/code-testing-agent/SKILL.md`) → `winui-dev-workflow` (`.codex/skills/winui-dev-workflow/SKILL.md`) → `winui-design` (`.codex/skills/winui-design/SKILL.md`) → `dotnet-webapi` (dotnet/skills `98f84851`) → `run-tests` → `winui-code-review` at review
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search`)
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (call `get_doc_gates <id>` before every move; a move crosses at most one gated boundary)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Orient: read the plan row, `vertical-slices.md` § S4, and `README.md` § 3 of plan 05 (characterization rule). Call `get_doc_gates <this ticket id>`, then `take_ticket` with branch `task/dsk-05-04-case-create` and worktree `../pegasus-worktrees/dsk-05-04-case-create` from `origin/dev`.
2. Read `src/Pegasus.Web/Pages/Cases/Create.cshtml.cs` in full and enumerate, in `research`, every rule it applies between the typed draft and the create command: candidate selection, staff-sourced candidate promotion, inspection-address resolution (`src/Pegasus.Core/Address/Ext18InspectionAddressPolicy`), principal/organization allocation, and the withheld/failed outcomes. Mark each rule as **already in Core** or **only in the page model**, with file and line, and record the SHA read.
3. Load `code-testing-agent`. For every rule marked "only in the page model", write a characterization test in `tests/Pegasus.Core.Tests` **first** against the current behaviour, then move the rule into the owning `src/Pegasus.Core/Cases/` or `src/Pegasus.Core/Address/` use case and re-point the Razor page at it. A second implementation is a stop condition (`docs/engineering.md` § One Core owner) — stop and consolidate rather than duplicating.
4. Confirm `POST /api/v1/cases` from [[DSK-03-08]] is idempotent by `operationKey` and returns 201 with the case id and version, and that the outcome vocabulary distinguishes created / withheld / failed exactly as the Core allocation path does. Add a contract fact that replaying the same `operationKey` returns the same result rather than allocating a second reference.
5. Add the create request and draft DTOs to `src/Pegasus.Contracts`, including a provenance value per field (`Staff · Extracted · AI · E-mail · Lookup · Principal · Automatic` — the closed list from `docs/design/README.md`).
6. Implement `CaseCreateViewModel` in `src/Pegasus.Desktop`: immediate field-level validation using the deterministic Core rules referenced directly from `Pegasus.Core` (permitted by the boundary note in `reuse-map.md`), server validation surfaced next to the owning section, a deliberate Save command, and a stable `operationKey` generated once per create attempt and reused on retry.
7. Hold unsaved state in the view model only. Where a local draft is justified (proposal §11.1) persist it encrypted through the credential/cache abstraction from [[DSK-02-06]] — never a `TempData` equivalent, never the `RetainableFormFields` allow-list, and never the 8000/2000-character budgets.
8. Build the create XAML on the form pattern from [[DSK-06-08]]: label and control only, no hint text, no "Required."/"Optional." prose, required state shown visually; a provenance glyph with its one-word tooltip beside each populated field per [[DSK-06-11]]; `AutomationId` on every control.
9. Write view-model tests in `tests/Pegasus.Desktop.ViewModelTests` covering validation, dirty state, the deliberate-save gate, operation-key reuse on retry, and each of the three allocation outcomes rendered with the approved copy (`No case or reference was created; review the missing or conflicting evidence.`).
10. Add contract tests in `tests/Pegasus.Api.ContractTests` for create success, replay, validation failure as a problem document, 401, and 403 without `PerformCasework`, with `Features:DesktopGateway` enabled.
11. Run the fixture comparison: for the QDOS fixture set used by `tests/Pegasus.IntegrationTests/QdosIntakeWebTests.cs` and `QdosAllocationRecoveryTests.cs`, create through the web page and through the desktop and confirm the allocation outcome and reference behaviour are identical. Record the table in the proof.
12. **Operator step** — run the UAT script for case create against the genuine corpus on the local Test/UAT stack (tier 8, local only; corpus material is never committed). The operator confirms the outcomes and signs the parity row; capture their sign-off text and date in the ticket proof.
13. Update `docs/desktop/01-inventory-and-parity/parity-matrix.md` row `PAR-09`, add the create section to `docs/frd/frd-13-desktop-operator-experience.md`, run the simplification pass over the branch diff, record it under a dated `## Simplification pass` heading, then open the PR into `dev`.

## Acceptance criteria

- [ ] A case can be created from an instruction draft or from blank, with the inspection address settled on the screen.
- [ ] Allocation outcomes (created / withheld / failed) match the web for the fixture set.
- [ ] Replay of the same operation key returns the same result and never allocates a second reference.
- [ ] Provenance is shown beside every populated field using the closed seven-value list.
- [ ] No field hints, no `TempData` equivalent, no `RetainableFormFields` allow-list, no retained-character budgets.
- [ ] Every rule the desktop relies on lives in `Pegasus.Core` with a characterization test; no rule has two implementations.

## Verification

- [ ] `dotnet test ./tests/Pegasus.Core.Tests/Pegasus.Core.Tests.csproj --configuration Release --no-build` — expected: the new draft-to-case characterization facts pass and the pre-existing Core facts stay green.
- [ ] `dotnet test ./tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj --configuration Release --no-build` — expected: create, replay, validation-problem, 401 and 403 facts pass.
- [ ] `dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release --no-build` — expected: validation, dirty-state, operation-key and outcome facts pass.
- [ ] `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --no-build --filter "Category!=Corpus&Category!=Browser"` — expected: existing QDOS web tests remain green after the rules move into Core.
- [ ] UAT record in the ticket proof — expected: named operator sign-off with date for the create workflow.

## Evidence tier

Tier 2 — Core/domain. Tier 5 — Web/API/MCP caller. Tier 7 — Browser/accessibility.
Tier 2 obliges positive, contradictory, ambiguous and failure cases for the draft-to-case mapping and reference allocation before any rule moves; tier 5 obliges route-level evidence including idempotency and exception translation on the real create endpoint; tier 7 obliges keyboard, focus, validation-summary and semantic-label evidence from a real run.

## Documentation changes

- `docs/desktop/01-inventory-and-parity/parity-matrix.md` — row `PAR-09`
- `docs/frd/frd-13-desktop-operator-experience.md` — create section
- `docs/capabilities.md` — `DSK` row for case create

## Guardrails

- **Azure**: no write.
- **Scope boundary**: may touch `src/Pegasus.Desktop`, `src/Pegasus.Desktop.Infrastructure`, `src/Pegasus.Contracts`, the `/api/v1` cases command group in `src/Pegasus.Web`, `src/Pegasus.Core` **only** for rules moved in with a characterization test, and the test projects. The Razor create page may be re-pointed at a moved Core rule but must keep its behaviour; it is not removed.
- **Traps**: page-model logic that is really business logic must move into Core with a test before the slice consumes it, and a second implementation is a stop condition; do not reproduce `TempData`, PRG or antiforgery; the design authority forbids hint text and how-it-works copy; genuine-corpus evidence stays local and is never committed; `Features:DesktopGateway` must be enabled in tests; parity drift — record the SHA of `Create.cshtml.cs` characterized.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.

## Outcome

_Filled at closeout._
