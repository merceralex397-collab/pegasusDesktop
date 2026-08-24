---
id: FEAT-012
type: ticket
title: DSK-05-12 · S12 Unidentified and vehicle images
status: backlog
area: desktop-features
assignee: ''
profile: feature
labels:
  - desktop-conversion
  - plan-05
  - phase-5
  - tier-5
  - tier-7
groups:
  - EPIC-006
  - HZN-006
links: []
refs:
  - docs/frd/frd-02-intake-and-source-identity.md
docs_todo: true
archived: false
created: '2026-08-24T07:51:33.066Z'
updated: '2026-08-24T07:51:33.066Z'
---

## What

Deliver two small native queues: Unidentified (list, detail, reasoned resolve, source download) and Vehicle images (list, detail, reasoned close) with VRM suggestions and candidate cases shown, using the settled operator vocabulary `Unidentified`, `Vehicle images` and `Image reference`.

## Why

Proposal §13.4 and §13.5 require these queues to be workable natively so nothing sits unresolved after the Phase 5 cutover of intake. Today they are four small page models — `src/Pegasus.Web/Pages/Unidentified/Index.cshtml.cs` (19 lines), `Unidentified/Details.cshtml.cs` (180 lines, `OnPostResolveAsync`), `Pages/ImageIntake/Index.cshtml.cs` (85 lines) and `ImageIntake/Details.cshtml.cs` (89 lines, `OnPostCloseAsync`) — over Core `src/Pegasus.Core/Intake/Unidentified/` (operation key ≤ 200 characters) and `src/Pegasus.Core/ImageIntake/`. The counts must keep excluding receipts that produced a case, exactly as today. Siblings: [[DSK-05-09]] supplies the received-item surface these queues link into, [[DSK-03-13]] the endpoints.

## Source of truth

- Plan row: `docs/desktop/05-implementation-and-migration/README.md` § 5 — `DSK-05-12`
- Plan detail: `docs/desktop/05-implementation-and-migration/vertical-slices.md` § `S12 · Unidentified and vehicle images (DSK-05-12)`
- Endpoint map: `docs/desktop/03-gateway-api-and-data/endpoint-map.md` § `Triage, Unidentified, Operations` (unidentified routes) and § `Intake (received items), uploads, image intake` (image-intake routes)
- Screen spec: `docs/desktop/06-ui-design/screen-specs.md` § `§13.4 Intake` → `Unidentified and Vehicle images`
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 13.4 Intake, § 13.5 Vehicle and inspection information
- Repository evidence: `src/Pegasus.Web/Pages/Unidentified/Index.cshtml.cs`, `Unidentified/Details.cshtml.cs`, `src/Pegasus.Web/Pages/ImageIntake/Index.cshtml.cs`, `ImageIntake/Details.cshtml.cs`; `src/Pegasus.Core/Intake/Unidentified/` (`IUnidentifiedStore`, operation key ≤ 200), `src/Pegasus.Core/ImageIntake/` (`IImageIntakeQueries`, `IImageIntakeStore`, `IVrmRecognitionEngine` port)
- Binding decisions: L-01 the gateway owns the commands and audit; L-02 verification on the local Test/UAT stack; L-04 routing named on the ticket
- Depends on: `DSK-05-09` the received-item surface and streaming service; `DSK-03-13` the unidentified and image-intake endpoints

## Routing

- **Subagent**: `winui-dev` — `.codex/agents/winui-dev.toml`; `pegasus-gateway-dev` — `.codex/agents/pegasus-gateway-dev.toml`; `pegasus-test-engineer` — `.codex/agents/pegasus-test-engineer.toml`
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `dotnet-webapi` (dotnet/skills `98f84851`) → `winui-dev-workflow` (`.codex/skills/winui-dev-workflow/SKILL.md`) → `winui-design` (`.codex/skills/winui-design/SKILL.md`) → `run-tests` → `winui-code-review` at review
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search`)
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (call `get_doc_gates <id>` before every move; a move crosses at most one gated boundary)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Orient: read the plan row, `vertical-slices.md` § S12, the screen spec section and the status vocabulary in `docs/design/README.md` (`Unidentified` is the settled word — never "Needs sorting"). Call `get_doc_gates <this ticket id>`, then `take_ticket` with branch `task/dsk-05-12-unidentified-vehicle-images` and worktree `../pegasus-worktrees/dsk-05-12-unidentified-vehicle-images` from `origin/dev`.
2. Read the four page models in full. In `research`, record for each: the query it lists, the exclusion rule that keeps receipts which produced a case out of the counts, the resolve and close command parameters (`expectedVersion`, `operationKey` ≤ 200 characters, `reason`), and the VRM suggestion and candidate-case fields the detail shows. Record the SHA read.
3. Confirm the endpoints from [[DSK-03-13]]: `GET /api/v1/unidentified?page`, `GET /api/v1/unidentified/{id}`, `GET /api/v1/unidentified/{id}/members/{mid}/source`, `POST /api/v1/unidentified/{id}/resolve`; and `GET /api/v1/image-intake?page`, `GET /api/v1/image-intake/{id}`, `POST /api/v1/image-intake/{id}/close`. Verify the list counts apply the same exclusion the Razor lists apply.
4. Add the DTOs to `src/Pegasus.Contracts`, including the VRM suggestion with its confidence-free presentation fields and the candidate case list with reference and status.
5. Implement `UnidentifiedListViewModel` and `UnidentifiedDetailViewModel` in `src/Pegasus.Desktop` using the data-table pattern from [[DSK-06-07]]; resolve is an explicit reasoned command using the dialog contract from [[DSK-06-09]].
6. Implement `VehicleImagesListViewModel` and `VehicleImagesDetailViewModel` the same way; close is an explicit reasoned command. Show VRM suggestions and candidate cases as data, without explanatory copy about how they were derived.
7. Reuse the streaming download service from [[DSK-05-09]] for member source access — one implementation, not a copy.
8. Add both queues to the shell rail under Queues in the route order from `docs/desktop/06-ui-design/screen-specs.md` § `Shell`, with counts sourced from the rail-counts endpoint (an absent count renders nothing).
9. Add contract tests in `tests/Pegasus.Api.ContractTests` for both list, detail, resolve, close and source endpoints: success, 401, 403, 409 stale version, replay of the same `operationKey`, reason required, and a list assertion proving receipts that produced a case are excluded from the counts. Enable `Features:DesktopGateway` explicitly.
10. Add view-model tests in `tests/Pegasus.Desktop.ViewModelTests` for list paging, resolve and close requiring a reason, conflict handling through the shared pattern, and correct vocabulary on every state.
11. Update `docs/desktop/01-inventory-and-parity/parity-matrix.md` for the unidentified and image-intake rows, add the section to `docs/frd/frd-13-desktop-operator-experience.md`, run the simplification pass over the branch diff, record it under a dated `## Simplification pass` heading, then open the PR into `dev`.

## Acceptance criteria

- [ ] Unidentified items can be listed, opened, their source downloaded, and resolved with a reason.
- [ ] Vehicle image registrations can be listed, opened and closed with a reason, with VRM suggestions and candidate cases visible.
- [ ] Counts exclude receipts that produced a case, exactly as today.
- [ ] The settled vocabulary is used throughout (`Unidentified`, `Vehicle images`, `Image reference`).
- [ ] The streaming download service from S9 is reused, not duplicated.

## Verification

- [ ] `dotnet test ./tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj --configuration Release --no-build` — expected: both queues' list/detail/command facts pass, including the count-exclusion assertion.
- [ ] `dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release --no-build` — expected: paging, reason-required and conflict facts pass.
- [ ] `dotnet test ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj --configuration Release --no-build` — expected: no second streaming implementation is introduced; dependency-direction facts stay green.

## Evidence tier

Tier 5 — Web/API/MCP caller. Tier 7 — Browser/accessibility.
Tier 5 obliges route-level evidence that both queues reach Core with authorization, idempotency and exception translation; tier 7 obliges keyboard, focus, semantic-label and text-plus-colour evidence from a real run of both screens.

## Documentation changes

- `docs/desktop/01-inventory-and-parity/parity-matrix.md` — unidentified and image-intake rows
- `docs/frd/frd-13-desktop-operator-experience.md` — unidentified and vehicle-images section
- `docs/capabilities.md` — `DSK` rows for both queues

## Guardrails

- **Azure**: no write.
- **Scope boundary**: may touch `src/Pegasus.Desktop`, `src/Pegasus.Desktop.Infrastructure`, `src/Pegasus.Contracts`, the `/api/v1` unidentified and image-intake groups in `src/Pegasus.Web` and the test projects. Must not touch `src/Pegasus.Infrastructure/Vision/` — the ONNX VRM engine stays server-side (a desktop move is the [[DSK-07-18]] spike, not this slice).
- **Traps**: operation keys here are bounded at 200 characters, unlike the 100-character administration bound; the settled status vocabulary is exact and case-sensitive; only populated sections render; `Features:DesktopGateway` must be enabled in tests; no second implementation of the streaming download.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.

## Outcome

_Filled at closeout._
