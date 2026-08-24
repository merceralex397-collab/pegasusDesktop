---
id: FEAT-007
type: ticket
title: 'DSK-05-07 · S7 Parties and reference data (organizations, principals)'
status: backlog
area: desktop-features
assignee: ''
profile: feature
labels:
  - desktop-conversion
  - plan-05
  - phase-4
  - tier-5
  - tier-7
groups:
  - EPIC-006
  - HZN-005
links: []
refs:
  - docs/frd/frd-04-parties-accounts-and-access.md
docs_todo: true
archived: false
created: '2026-08-24T07:49:10.201Z'
updated: '2026-08-24T07:49:10.201Z'
---

## What

Deliver the administrator-only native screens for organizations and principals — list, create, update and the explicit principal-replace command — plus read-only provider reference data, all behind the `ManageOrganizationsAndPrincipals` right on the gateway.

## Why

Proposal §13.6 requires party and reference-data maintenance according to permissions. Today it is five page models: `src/Pegasus.Web/Pages/Administration/Organizations/Index.cshtml.cs` (126 lines), `Organizations/Edit` (146), `Principals/Index` (31), `Principals/Create` (137) and `Principals/Replace` (199), over Core organization and principal administration (`src/Pegasus.Core/Cases/OrganizationAdministration.cs`, operation key ≤ 100) and the `src/Pegasus.Core/ReferenceData/` catalogue. Principal replacement is a consequential command — a principal is immutable once allocated — so it needs explicit consequence copy rather than an ordinary edit form. Siblings: [[DSK-05-03]] establishes the workspace patterns, [[DSK-03-15]] supplies the endpoints, [[DSK-05-19]] adds the rest of Administration.

## Source of truth

- Plan row: `docs/desktop/05-implementation-and-migration/README.md` § 5 — `DSK-05-07`
- Plan detail: `docs/desktop/05-implementation-and-migration/vertical-slices.md` § `S7 · Parties and reference data (DSK-05-07)`
- Endpoint map: `docs/desktop/03-gateway-api-and-data/endpoint-map.md` § `Administration and audit` (`/admin/organizations`, `/admin/principals`, `/admin/principals/{id}/replace`) and § `Reference data`
- Screen spec: `docs/desktop/06-ui-design/screen-specs.md` § `§13.6 Parties and reference data — Administration › Organizations, Principals`
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 13.6 Parties and reference data
- Repository evidence: `src/Pegasus.Web/Pages/Administration/Organizations/`, `src/Pegasus.Web/Pages/Administration/Principals/`, `src/Pegasus.Web/Pages/Administration/AdministrationPageModel.cs`, `src/Pegasus.Core/Cases/OrganizationAdministration.cs`, `src/Pegasus.Core/ReferenceData/` (`IProviderReferenceCatalog`), `src/Pegasus.Core/Identity/StaffAuthorization.cs` (`ManageOrganizationsAndPrincipals`)
- Binding decisions: L-01 the gateway owns authorization and audit; L-02 verification on the local Test/UAT stack; L-04 routing named on the ticket
- Depends on: `DSK-05-03` the read-only workspace patterns and shared controls; `DSK-03-15` the administration endpoints for organizations and principals

## Routing

- **Subagent**: `winui-dev` — `.codex/agents/winui-dev.toml`; `pegasus-gateway-dev` — `.codex/agents/pegasus-gateway-dev.toml`; `pegasus-test-engineer` — `.codex/agents/pegasus-test-engineer.toml`
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `dotnet-webapi` (dotnet/skills `98f84851`, `plugins/dotnet-aspnetcore/skills/dotnet-webapi/SKILL.md`) → `winui-dev-workflow` (`.codex/skills/winui-dev-workflow/SKILL.md`) → `winui-design` (`.codex/skills/winui-design/SKILL.md`) → `run-tests` (dotnet/skills `98f84851`) → `winui-code-review` at review
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search`)
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (call `get_doc_gates <id>` before every move; a move crosses at most one gated boundary)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Orient: read the plan row, `vertical-slices.md` § S7, the screen spec section and `docs/frd/frd-04-parties-accounts-and-access.md` § staff role access matrix. Call `get_doc_gates <this ticket id>`, then `take_ticket` with branch `task/dsk-05-07-parties` and worktree `../pegasus-worktrees/dsk-05-07-parties` from `origin/dev`.
2. Read the five page models under `src/Pegasus.Web/Pages/Administration/Organizations/` and `Principals/` in full. Record in `research` the validation rules, the operation-key length bound (≤ 100 characters, `src/Pegasus.Core/Cases/OrganizationAdministration.cs`), the immutability rule for an allocated principal, and what `Principals/Replace` does to existing references. Record the SHA read.
3. Confirm the endpoints from [[DSK-03-15]]: `GET/POST /api/v1/admin/organizations`, `GET/PUT /api/v1/admin/organizations/{id}`, `GET/POST /api/v1/admin/principals`, `POST /api/v1/admin/principals/{id}/replace`, and the read-only `GET /api/v1/reference/providers`. Every mutation is gated on `ManageOrganizationsAndPrincipals` and carries `operationKey`; replace additionally carries `reason`.
4. Add the DTOs to `src/Pegasus.Contracts`, keeping the organization and principal versions on the wire so a stale update returns 409 rather than overwriting.
5. Implement `OrganizationsViewModel` and `PrincipalsViewModel` in `src/Pegasus.Desktop` using the data-table pattern from [[DSK-06-07]] for the lists and the form pattern from [[DSK-06-08]] for create and edit. Reference data is read-only — render it without any edit affordance.
6. Implement principal replacement as its own explicit command with the reason dialog from [[DSK-06-09]] and a consequence sentence drawn from the approved list; it is never an inline field edit. The replacement must never reuse an existing reference — assert this in the contract test rather than in the UI.
7. Apply role awareness from [[DSK-04-10]]: the Administration rail entry and these screens are absent for a non-administrator, and the gateway still refuses a forged call with a `not-authorized` problem.
8. Add contract tests in `tests/Pegasus.Api.ContractTests` for each endpoint: 200 for an administrator, 403 `not-authorized` for `PerformCasework`-only and for the Automation Actor, 401 without a token, 409 on a stale version, replay of the same `operationKey` returning the same result, and a replace that proves no reference is reused. Enable `Features:DesktopGateway` explicitly.
9. Add view-model tests in `tests/Pegasus.Desktop.ViewModelTests` for list paging, create validation, edit dirty state, and replace requiring a reason.
10. Add a `winapp ui` script under `tests/Pegasus.Desktop.UITests` covering create-organization and replace-principal by keyboard, and run the `axe-windows` scan on both screens; attach the artefacts to the ticket proof.
11. Update `docs/desktop/01-inventory-and-parity/parity-matrix.md` for the organizations and principals rows, add the parties section to `docs/frd/frd-13-desktop-operator-experience.md`, run the simplification pass over the branch diff, record it under a dated `## Simplification pass` heading, then open the PR into `dev`.

## Acceptance criteria

- [ ] Organizations and principals can be listed, created and updated by an administrator only.
- [ ] Principal replacement is an explicit reasoned command with visible consequence copy, and never reuses a reference.
- [ ] A principal that has been allocated stays immutable through the desktop path.
- [ ] Non-administrators receive a 403 `not-authorized` problem from the gateway even when the UI is bypassed.
- [ ] Provider reference data renders read-only, with no edit affordance.

## Verification

- [ ] `dotnet test ./tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj --configuration Release --no-build` — expected: 200/401/403/409/replay and no-reference-reuse facts pass for every endpoint.
- [ ] `dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release --no-build` — expected: list, create, edit and replace facts pass.
- [ ] `pwsh ./tests/Pegasus.Desktop.UITests/ui-tests.ps1 -Script parties` — expected: keyboard create and replace pass; axe report attached.

## Evidence tier

Tier 5 — Web/API/MCP caller. Tier 7 — Browser/accessibility.
Tier 5 obliges route-level evidence that each administration endpoint reaches Core with the right authorization boundary, idempotency and exception translation; tier 7 obliges keyboard, focus, semantic-label and validation-summary evidence from a real run of both screens.

## Documentation changes

- `docs/desktop/01-inventory-and-parity/parity-matrix.md` — organization and principal rows
- `docs/frd/frd-13-desktop-operator-experience.md` — parties and reference-data section
- `docs/capabilities.md` — `DSK` rows for organizations and principals

## Guardrails

- **Azure**: no write.
- **Scope boundary**: may touch `src/Pegasus.Desktop`, `src/Pegasus.Contracts`, the `/api/v1` administration group in `src/Pegasus.Web` and the test projects. Must not modify the Razor administration pages or `AdministrationPageModel.cs`.
- **Traps**: the desktop hides or disables for usability only — the gateway is the enforcement point; upstream PLAT-028 (redesign Organizations and Principals) is absorbed by this screen spec, but upstream TICK-034 (DATA-02) stays backlog and must not be pulled in (§13.11 scope creep); operation keys are bounded at 100 characters here, unlike the 200-character intake bound; `Features:DesktopGateway` must be enabled in tests.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.

## Outcome

_Filled at closeout._
