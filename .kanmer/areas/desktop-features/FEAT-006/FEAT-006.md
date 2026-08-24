---
id: FEAT-006
type: ticket
title: 'DSK-05-06 · S6 Workflow, closure and tasks commands'
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
  - needs-operator
groups:
  - EPIC-006
  - HZN-005
links: []
refs:
  - docs/frd/frd-01-case-identity-and-lifecycle.md
docs_todo: true
archived: false
created: '2026-08-24T07:46:33.876Z'
updated: '2026-08-24T07:46:33.876Z'
---

## What

Make all nineteen case commands available natively as explicit, audited actions with reason dialogs where Core requires a reason: the seven workflow commands, the four closure commands and the eight task/note/chase/report-evidence commands, each mapped to its own named `/api/v1` endpoint.

## Why

Proposal §13.3 requires the full case lifecycle on the desktop, and §10.2 forbids a generic execute endpoint. Today the commands are spread over three page models that all wrap `CaseMutationPageModel` PRG: `src/Pegasus.Web/Pages/Cases/Workflow.cshtml.cs` (227 lines, seven `OnPost*` handlers at `:26`, `:42`, `:64`, `:98`, `:133`, `:156`, `:180`), `Closure.cshtml.cs` (121 lines, four at `:23`, `:52`, `:69`, `:106`) and `Tasks.cshtml.cs` (248 lines, eight at `:33`, `:61`, `:89`, `:117`, `:143`, `:169`, `:201`, `:225`). The product invariants — never delete a case, reopen needs a reason, principal and reference are immutable — must hold on the new surface. Siblings: [[DSK-05-05]] supplies the lease and version session, [[DSK-03-08]] and [[DSK-03-09]] supply the endpoints.

## Source of truth

- Plan row: `docs/desktop/05-implementation-and-migration/README.md` § 5 — `DSK-05-06`
- Plan detail: `docs/desktop/05-implementation-and-migration/vertical-slices.md` § `S6 · Workflow, closure and tasks commands (DSK-05-06)`
- Endpoint map: `docs/desktop/03-gateway-api-and-data/endpoint-map.md` § `Cases` (`/hold`, `/release-hold`, `/return-to-review`, `/assign-engineer`, `/start-work`, `/record-engineer-finding`, `/linked-replacement`, `/report-approval`, `/close`, `/reopen`, `/archive`) and the Tasks rows (`/notes`, `/tasks`, `/tasks/{taskId}/assign|complete|cancel`, `/chases/manual`, `/report-evidence/link|unlink`)
- Screen spec: `docs/desktop/06-ui-design/screen-specs.md` § `§13.3 Case lifecycle` → `Case workspace` (command bar) and the reason-dialog contract
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 13.3 Case lifecycle, § 10.2 API style
- Repository evidence: `src/Pegasus.Web/Pages/Cases/Workflow.cshtml.cs:26-180`, `src/Pegasus.Web/Pages/Cases/Closure.cshtml.cs:23-106`, `src/Pegasus.Web/Pages/Cases/Tasks.cshtml.cs:33-225`, `src/Pegasus.Core/Lifecycle/CaseLifecycle.cs` (629 lines), `src/Pegasus.Core/Lifecycle/CaseCommandSeams.cs` (280 lines), `src/Pegasus.Core/Tasks/`, `AGENTS.md` § Product invariants
- Binding decisions: L-01 the gateway owns the commands, audit and authorization; L-02 verification on the local Test/UAT stack; L-04 routing named on the ticket
- Depends on: `DSK-05-05` the lease, version and save session; `DSK-03-08` the workflow and closure command endpoints; `DSK-03-09` the tasks, notes, chases and report-evidence endpoints

## Routing

- **Subagent**: `winui-dev` — `.codex/agents/winui-dev.toml`; `pegasus-gateway-dev` — `.codex/agents/pegasus-gateway-dev.toml`; `pegasus-test-engineer` — `.codex/agents/pegasus-test-engineer.toml`
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `dotnet-webapi` (dotnet/skills `98f84851`, `plugins/dotnet-aspnetcore/skills/dotnet-webapi/SKILL.md`) → `winui-dev-workflow` (`.codex/skills/winui-dev-workflow/SKILL.md`) → `winui-design` (`.codex/skills/winui-design/SKILL.md`) → `code-testing-agent` (dotnet/skills `98f84851`) → `run-tests` → `winui-code-review` at review
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search`)
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (call `get_doc_gates <id>` before every move; a move crosses at most one gated boundary)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Orient: read the plan row, `vertical-slices.md` § S6, `AGENTS.md` § Product invariants and `docs/design/README.md` § `Voice, labels and necessary copy` (the closed consequence list). Call `get_doc_gates <this ticket id>`, then `take_ticket` with branch `task/dsk-05-06-case-commands` and worktree `../pegasus-worktrees/dsk-05-06-case-commands` from `origin/dev`.
2. Build the command inventory in `research`: one row per handler across the three page models (nineteen rows), each recording the Core use case called, whether Core requires a `reason`, whether the lease token is required, which `StaffAccessRight` or role gates it (engineer finding requires the Engineer role), and the exception types it can throw. Record the SHA read.
3. Confirm each row has its own named endpoint in [[DSK-03-08]] / [[DSK-03-09]] — never a dispatcher taking an action string. Where an endpoint is missing, add it to the `/api/v1` group calling the same `src/Pegasus.Core/Lifecycle/` or `src/Pegasus.Core/Tasks/` command the Razor handler calls.
4. Add one request DTO per command to `src/Pegasus.Contracts`, each carrying `operationKey`, `expectedVersion`, `editLeaseToken` where Core requires it, and `reason` where Core requires it. Do not introduce a shared "command" bag that hides which fields a given command needs.
5. Implement a `CaseCommandsViewModel` in `src/Pegasus.Desktop` exposing one command object per row, each with its own `CanExecute` derived from the loaded case state and the actor's rights from [[DSK-04-10]] — the desktop hides or disables for usability only; the gateway remains the enforcement point.
6. Build the command bar in the case header from [[DSK-05-03]] using the design authority's rules: a named verb per command, never a generic "Close"; permanent consequences visible without hover, using only the approved sentences, for example `Created in error cannot be reopened. Create and link the replacement case.` Every command control carries an `AutomationId`.
7. Implement the reason dialog with the `ReasonDialog` contract from [[DSK-06-09]] for every command Core requires a reason for (reopen among them): named requirement, labelled reason field, verb-labelled primary button plus Cancel, initial focus on the reason field.
8. Implement the Tasks tab: add note, create/assign/complete/cancel task, record manual chase, link and unlink report evidence — each an explicit action with its own operation key and the task-level `expectedVersion` where Core uses one (`CaseTaskVersionConflictException`).
9. Add contract tests in `tests/Pegasus.Api.ContractTests` covering, for every one of the nineteen commands, the seven-case matrix from [[DSK-08-02]]: success, unauthenticated 401, wrong right 403, stale version 409, bad input 400 problem, replayed operation key returns the same result, and the Core-specific failure path. Enable `Features:DesktopGateway` explicitly.
10. Add view-model tests in `tests/Pegasus.Desktop.ViewModelTests` for `CanExecute` gating per case state and per right, reason-required commands refusing to execute with an empty reason, and the product invariants holding on the surface (no delete command exists at all).
11. **Operator step** — run the UAT script for the primary case workflow: hold/release, return to review, assign engineer, start work, record finding, create linked replacement, record report approval, close, reopen with reason, archive. Capture the operator's sign-off text and date in the ticket proof.
12. Update `docs/desktop/01-inventory-and-parity/parity-matrix.md` rows `PAR-10`, `PAR-11` and `PAR-12`, add the command sections to `docs/frd/frd-13-desktop-operator-experience.md`, run the simplification pass over the branch diff, record it under a dated `## Simplification pass` heading, then open the PR into `dev`.

## Acceptance criteria

- [ ] All nineteen commands are available as explicit, named, audited actions; no dispatcher string and no generic execute endpoint.
- [ ] Every command that Core requires a reason for shows the reason dialog and refuses an empty reason.
- [ ] Permanent consequences are visible without hover, using only approved copy.
- [ ] Every command has authorization and failure-path tests, including the Engineer-only finding command.
- [ ] Product invariants hold: no case can be deleted, reopen requires a reason, principal and reference remain immutable.

## Verification

- [ ] `dotnet test ./tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj --configuration Release --no-build` — expected: the seven-case matrix passes for each of the nineteen commands.
- [ ] `dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release --no-build` — expected: `CanExecute`, reason-required and no-delete facts pass.
- [ ] `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --no-build --filter "Category!=Corpus&Category!=Browser"` — expected: existing lifecycle persistence tests stay green.
- [ ] UAT record in the ticket proof — expected: named operator sign-off with date across the eleven lifecycle commands.

## Evidence tier

Tier 5 — Web/API/MCP caller. Tier 7 — Browser/accessibility.
Tier 5 obliges route-level evidence per command that the real endpoint reaches Core with authentication, validation, idempotency, exception translation and the correct action-history actor; tier 7 obliges keyboard, focus and error-behaviour evidence for the command bar and reason dialogs from a real run.

## Documentation changes

- `docs/desktop/01-inventory-and-parity/parity-matrix.md` — rows `PAR-10` (workflow), `PAR-11` (tasks), `PAR-12` (closure)
- `docs/frd/frd-13-desktop-operator-experience.md` — command sections
- `docs/capabilities.md` — `DSK` rows for the workflow, closure and task commands

## Guardrails

- **Azure**: no write.
- **Scope boundary**: may touch `src/Pegasus.Desktop`, `src/Pegasus.Contracts`, the `/api/v1` cases command group in `src/Pegasus.Web` and the test projects. Must not modify `Pages/Cases/Workflow.cshtml.cs`, `Closure.cshtml.cs` or `Tasks.cshtml.cs`.
- **Traps**: never a generic execute endpoint (`docs/desktop/03-gateway-api-and-data/README.md` § 3); the design authority forbids explanatory copy — consequence sentences come from the closed approved list only; a rule found only in a page model moves into Core with a test first; `Features:DesktopGateway` must be enabled in tests; upstream CASE-002 and CASE-004 are future capabilities and are **not** absorbed here — a slice that needs one stops and raises a ticket.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.

## Outcome

_Filled at closeout._
