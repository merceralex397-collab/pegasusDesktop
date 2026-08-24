---
id: FEAT-026
type: ticket
title: DSK-05-26 · Cut-list execution after cutover
status: preparing
area: desktop-features
assignee: ''
profile: chore
stageEntered:
  preparing: '2026-08-24T21:31:39.339Z'
labels:
  - desktop-conversion
  - plan-05
  - phase-10
  - tier-1
  - tier-5
  - needs-operator
groups:
  - EPIC-006
  - HZN-011
links: []
docs_todo: true
archived: false
created: '2026-08-24T08:04:08.622Z'
updated: '2026-08-24T21:31:39.339Z'
---

## What

After cutover approval, execute the reuse-map cut list: remove the replaced staff Razor pages, their partials, `site.css`, `site.js` and the browser test lane, keeping every web-only route that is deliberately retained — and leave the Azure-side removals to plan 11.

## Why

Proposal §24 Phase 10 and §19.2 allow code and infrastructure dependencies to be removed only after the mandatory production desktop release, a monitored business cycle and explicit approval. `docs/desktop/05-implementation-and-migration/reuse-map.md` § `Cut list after cutover (Phase 10 only)` names exactly what goes and § `Never cut before parity` names what stays. Leaving the dead surface in place would keep the browser lane, the Playwright pin and 10,800 lines of page models alive in every build; removing it early would strand a workflow with no desktop replacement. Siblings: [[DSK-05-25]] proves every row reached `cut over`, [[DSK-11-08]] owns the prepared Azure deprovision checklist, [[DSK-05-24]] already stopped the desktop from depending on the state machine this ticket deletes.

## Source of truth

- Plan row: `docs/desktop/05-implementation-and-migration/README.md` § 5 — `DSK-05-26`
- Plan detail: `docs/desktop/05-implementation-and-migration/reuse-map.md` § `Cut list after cutover (Phase 10 only)` (the five numbered items) and § `Never cut before parity`
- Endpoint map: `docs/desktop/03-gateway-api-and-data/endpoint-map.md` § `Stays web-only (not projected)` — `Pages/Uploads/Request.cshtml.cs`, `Pages/Connect/Authorize.cshtml.cs`, `Pages/Error.cshtml.cs`, `Pages/StatusCode.cshtml.cs`, `Pages/Account/AccessDenied.cshtml.cs`
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 24 Phase 10, § 19.2 Deprovisioning method after cutover
- Repository evidence: 53 Razor page models (~10,800 LOC) and 76 `.cshtml` under `src/Pegasus.Web/Pages/`; `src/Pegasus.Web/Pages/Shared/` (15 partials); `src/Pegasus.Web/wwwroot/css/site.css` (2,471 lines), `wwwroot/js/site.js` (786 lines); `src/Pegasus.Web/Pages/Cases/CaseMutationPageModel.cs` (339 lines), `src/Pegasus.Web/Presentation/RailCountsPageFilter.cs` (51 lines) and the `Presentation/*View.cs` view models; `tests/Pegasus.IntegrationTests/Browser/` (9 files, 20 facts); the Playwright base-image pin in `src/Pegasus.Web/Pegasus.Web.csproj` / `Directory.Build.props`; `tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs`
- Binding decisions: L-01 the gateway host stays — only the Razor staff surface goes; L-03 the Playwright renderer is retired only once ADR-0108 golden-file parity is signed off ([[DSK-05-18]]); D-001 the fork is the single release source by this point; L-04 routing named on the ticket
- Depends on: `DSK-05-25` the parity matrix showing every replaceable row at `cut over`; `DSK-11-08` the prepared Azure deprovision checklist that owns the Azure-side items. "Phase 10 approval" is an operator gate rather than a ticket — it is captured as an operator step below.

## Routing

- **Subagent**: `pegasus-gateway-dev` — `.codex/agents/pegasus-gateway-dev.toml` (performs the removals in `Pegasus.Web`); `pegasus-desktop-reviewer` — `.codex/agents/pegasus-desktop-reviewer.toml` (independent review that nothing retained was removed)
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `run-tests` (dotnet/skills `98f84851`, `plugins/dotnet-test/skills/run-tests/SKILL.md`) → `pegasus-release` (`.agents/skills/pegasus-release/SKILL.md`) for the release notes
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`)
- **Kanmer pipeline** for profile `chore`: `kanmer-plan` → `kanmer-execute` → `kanmer-verify` → `kanmer-closeout` (call `get_doc_gates <id>` before every move; `chore` needs `plan` and `questions-resolved` to leave Preparing and `proof` to enter Done)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Orient: read the plan row, `reuse-map.md` § `Cut list after cutover (Phase 10 only)` and § `Never cut before parity`, and the endpoint map's `Stays web-only` table. Call `get_doc_gates <this ticket id>`, then `take_ticket` with branch `task/dsk-05-26-cut-list` and worktree `../pegasus-worktrees/dsk-05-26-cut-list` from `origin/dev`.
2. **Operator step** — obtain and record the three Phase 10 preconditions before deleting anything: the mandatory production desktop release has shipped, at least one complete business cycle has been monitored, and the operator has given explicit cutover approval with a date. Paste the approval text into the ticket proof. Without all three the ticket stays in Preparing.
3. Verify the parity matrix from [[DSK-05-25]]: every replaceable row reads `cut over`, and the deliberate exceptions read `legacy path retained`. A row still at `UAT passed` or below blocks removal of the page it names — list any such row and stop rather than removing it.
4. Build the removal manifest in the plan: one line per file to delete, grouped by cut-list item, with the matrix row that authorises it. Explicitly list the KEEP set that must survive — `Pages/Uploads/Request.cshtml.cs`, `Pages/Connect/Authorize.cshtml.cs`, `Pages/Error.cshtml.cs`, `Pages/StatusCode.cshtml.cs`, `Pages/Account/AccessDenied.cshtml.cs`, the whole `Mcp/` folder, `Authentication/`, rate limiting, `Health/` and `/diagnostics/version`.
5. Cut-list item 1: delete the staff Razor page models and their `.cshtml` files and the four case partials, except the KEEP set. Remove any now-unused DI registration in `src/Pegasus.Web/Program.cs` in the same change — a registration for a deleted type is a defect (`docs/engineering.md` § One Core owner: migrate or delete the replaced code, registrations, tests and documentation in the same slice).
6. Cut-list item 2: delete `src/Pegasus.Web/wwwroot/css/site.css`, `wwwroot/js/site.js`, `Pages/Shared/_LucideSprite.cshtml` and the shell layouts, keeping whatever the retained pages still need — verify by loading each KEEP page after the removal.
7. Cut-list item 3: delete `src/Pegasus.Web/Pages/Cases/CaseMutationPageModel.cs`, `src/Pegasus.Web/Presentation/RailCountsPageFilter.cs` (and its global page-filter registration) and every `Presentation/*View.cs` view model no longer referenced. `OperatorLabels` is **not** in this list — it moved to the shared assembly in [[DSK-05-23]] and stays.
8. Cut-list item 4: delete `tests/Pegasus.IntegrationTests/Browser/` and remove the Playwright base-image pin from `src/Pegasus.Web/Pegasus.Web.csproj` and `Directory.Build.props` — **only if** the Playwright renderer has also been retired under ADR-0108 golden-file parity ([[DSK-05-18]]). If the renderer is still retained, keep the pin and record why.
9. Cut-list item 5 is **out of scope**: the `AddPegasusReportRendering` registration removal and the Container App CPU/memory uplift reversal are an ⚠ Azure setting change owned by plan 11 ([[DSK-11-08]]). Raise or update that ticket rather than acting here.
10. Extend `tests/Pegasus.ArchitectureTests` so the retained web-only routes are asserted to exist, and so a future change cannot delete `Uploads/Request` or `Connect/Authorize` silently.
11. Run the canonical build and test commands and confirm the solution is green with the deletions in place; every remaining test must pass without an assertion being edited to accommodate a removal.
12. Update `docs/current-architecture.md`, `docs/boundaries.md` (the web front end as a deprovision candidate is now executed on the code side), `docs/operations.md` and the release notes through the `pegasus-release` skill, then update the parity matrix rows to `legacy path retired`.
13. Run the simplification pass over the branch diff, record it under a dated `## Simplification pass` heading, then open the PR into `dev` and promote to `main` by exact-SHA with the literal `MERGE AUTH GRANTED` — a GitHub merge is not a promotion.

## Acceptance criteria

- [ ] The three Phase 10 preconditions are recorded with the operator's approval text and date before any deletion.
- [ ] Every deleted file is authorised by a parity row reading `cut over`; nothing below that status was removed.
- [ ] The KEEP set survives and each retained page loads: request-link upload, MCP consent, error, status code, access denied.
- [ ] `Pegasus.Core`, `Pegasus.Infrastructure`, `Pegasus.Worker`, migrations, Identity, OpenIddict, MCP ingress, rate limiting and health endpoints are untouched.
- [ ] The browser lane and Playwright pin are removed only if the Playwright renderer is also retired; otherwise both stay with a recorded reason.
- [ ] No Azure resource or setting was changed; cut-list item 5 was handed to plan 11.
- [ ] Build and full test suite are green with no assertion edited to accommodate a removal.

## Verification

- [ ] `dotnet build ./Pegasus.slnx --configuration Release --no-restore` — expected: succeeds with no unresolved reference to a deleted type.
- [ ] `dotnet test ./Pegasus.slnx --configuration Release --no-build --filter "Category!=Corpus"` — expected: the full suite passes; the browser lane is absent only if item 4 applied.
- [ ] `dotnet test ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj --configuration Release --no-build` — expected: the retained-route facts pass and dependency-direction facts stay green.
- [ ] `pwsh ./scripts/Test-DocumentationLinks.ps1` — expected: passes after the documentation updates.
- [ ] Manual check recorded in the proof — expected: each retained web-only page loads correctly on the deployed gateway after promotion.

## Evidence tier

Tier 1 — Static/build/architecture. Tier 5 — Web/API/MCP caller.
Tier 1 obliges compiling the approved projects and enforcing dependency direction and one policy owner after the removals; tier 5 obliges observable evidence that the retained routes and the `/api/v1` surface still reach Core with authentication and validation intact once the Razor surface is gone.

## Documentation changes

- `docs/current-architecture.md` — remove the retired Razor surface from the implementation map
- `docs/boundaries.md` — record that the web front end's code-side removal is executed
- `docs/operations.md` and the release notes — the release that carries the removal
- `docs/desktop/01-inventory-and-parity/parity-matrix.md` — advance the removed rows to `legacy path retired`
- `docs/desktop/05-implementation-and-migration/reuse-map.md` — mark the executed cut-list items

## Guardrails

- **Azure**: no write. Cut-list item 5 (the `AddPegasusReportRendering` registration and the Container App CPU/memory uplift from ADR-0028) is an ⚠ Azure change owned by plan 11 and the runbook approval matrix (`docs/runbook.md` § Live-operation approval matrix, mirrored in `docs/desktop/11-azure-disposition/README.md`). Nothing is deprovisioned before cutover, observed use and rollback approval.
- **Scope boundary**: may delete from `src/Pegasus.Web/Pages/`, `src/Pegasus.Web/Presentation/`, `src/Pegasus.Web/wwwroot/` and `tests/Pegasus.IntegrationTests/Browser/`, and may edit `Program.cs` registrations, the `.csproj` pin and the documentation. Must not touch `src/Pegasus.Core`, `src/Pegasus.Infrastructure`, `src/Pegasus.Worker`, migrations, Identity, OpenIddict, `Mcp/`, rate limiting or the health endpoints.
- **Traps**: never cut before parity — a row not at `cut over` blocks its page; the `main`-history guard (`scripts/Test-MainBranchHistory.ps1`) fails a push to `main` whose history is not contained in `dev`, and promotion needs the literal `MERGE AUTH GRANTED`; deleting a type without its DI registration and tests leaves the build green but the codebase dishonest; the Playwright pin and the browser lane are coupled to the renderer's retirement, not to this ticket's schedule.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.

## Outcome

_Filled at closeout._
