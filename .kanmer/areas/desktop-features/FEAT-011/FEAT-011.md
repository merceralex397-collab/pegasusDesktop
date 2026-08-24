---
id: FEAT-011
type: ticket
title: 'DSK-05-11 · S11 Triage list, detail and actions'
status: backlog
area: desktop-features
assignee: ''
profile: feature
labels:
  - desktop-conversion
  - plan-05
  - phase-5
  - tier-2
  - tier-5
  - tier-7
  - needs-operator
groups:
  - EPIC-006
  - HZN-006
links: []
refs:
  - docs/frd/frd-03-triage.md
docs_todo: true
archived: false
created: '2026-08-24T07:51:33.051Z'
updated: '2026-08-24T07:51:33.051Z'
---

## What

Deliver the native Triage queue: list, detail, source download and every triage action as its own explicit named command — replacing the single `OnPostActionAsync` dispatcher that today switches on an action string — with the action matrix first characterized in `Pegasus.Core`.

## Why

Proposal §13.4 requires the triage flow natively with evidence, and §10.2 forbids a generic action endpoint. Today `src/Pegasus.Web/Pages/Triage/Details.cshtml.cs:85` dispatches on `actionName` through a `switch` at `:114-210` whose `case` labels are `assign`, `unassign`, `await_information`, `record_finding`, `supersede_finding`, `link_response`, `unlink_response`, `complete`, `cancel`, `reopen`, `link_case`, `unlink_case`, with `Pages/Triage/Index.cshtml.cs` (449 lines) as the list. `Triage` keeps its settled business meaning (`docs/engineering.md` § Capability organization). Siblings: [[DSK-05-09]] supplies the received-item context, [[DSK-03-13]] supplies the endpoints, [[DSK-05-08]] the conflict pattern for `TriageVersionConflictException`.

## Source of truth

- Plan row: `docs/desktop/05-implementation-and-migration/README.md` § 5 — `DSK-05-11`
- Plan detail: `docs/desktop/05-implementation-and-migration/vertical-slices.md` § `S11 · Triage (DSK-05-11)`; § 2 of `README.md` records the open point that the MCP tool set names ten mutations and the remaining commands are enumerated during S11 research rather than assumed
- Endpoint map: `docs/desktop/03-gateway-api-and-data/endpoint-map.md` § `Triage, Unidentified, Operations` (the twelve named routes, with the note "verify the full set")
- Screen spec: `docs/desktop/06-ui-design/screen-specs.md` § `§13.4 Intake` → `Triage detail`
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 13.4 Intake, § 10.2 API style
- Repository evidence: `src/Pegasus.Web/Pages/Triage/Details.cshtml.cs:85` (`OnPostActionAsync`) and its `switch` at `:114-210`, `src/Pegasus.Web/Pages/Triage/Index.cshtml.cs`, `src/Pegasus.Core/Triage/` (lifecycle, 561 lines), `src/Pegasus.Web/Mcp/TriageMcpTools.cs` (names ten mutations)
- Binding decisions: L-01 the gateway owns the commands and audit; L-02 verification on the local Test/UAT stack; L-04 routing named on the ticket
- Depends on: `DSK-05-09` the received-item surface and evidence plumbing; `DSK-03-13` the triage list, detail, source and named command endpoints

## Routing

- **Subagent**: `winui-dev` — `.codex/agents/winui-dev.toml`; `pegasus-gateway-dev` — `.codex/agents/pegasus-gateway-dev.toml`; `pegasus-test-engineer` — `.codex/agents/pegasus-test-engineer.toml`
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `code-testing-agent` (dotnet/skills `98f84851`, `plugins/dotnet-test/skills/code-testing-agent/SKILL.md`) → `dotnet-webapi` (dotnet/skills `98f84851`) → `winui-dev-workflow` (`.codex/skills/winui-dev-workflow/SKILL.md`) → `winui-design` (`.codex/skills/winui-design/SKILL.md`) → `run-tests` → `winui-code-review` at review
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search`)
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (call `get_doc_gates <id>` before every move; a move crosses at most one gated boundary)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Orient: read the plan row, `vertical-slices.md` § S11, the screen spec `Triage detail` section and `docs/frd/frd-03-triage.md`. Call `get_doc_gates <this ticket id>`, then `take_ticket` with branch `task/dsk-05-11-triage` and worktree `../pegasus-worktrees/dsk-05-11-triage` from `origin/dev`.
2. **Enumerate the action matrix and resolve the open question.** Read `src/Pegasus.Web/Pages/Triage/Details.cshtml.cs:114-210` and list every `case` label with the Core command it calls, its required parameters (`expectedVersion`, `operationKey`, `reason`, `roadworthiness`, `assessment`, `supersedesFindingId`, `responseCandidate`, `sentEvidenceId`, `caseId`) and its failure paths. The plan text says thirteen actions and `src/Pegasus.Web/Mcp/TriageMcpTools.cs` names ten; the dispatcher read on 2026-08-24 has twelve. Record the actual count with evidence in `research`, note the discrepancy under the ticket's open questions, and get it resolved before leaving Preparing — do not assume a number.
3. Load `code-testing-agent`. Write characterization tests in `tests/Pegasus.Core.Tests` for the action matrix — which action is legal from which `TriageState`, which require a reason, which require a finding payload — before any rule moves. Where a precondition lives only in the page model, move it into `src/Pegasus.Core/Triage/` and re-point the Razor page; a second implementation is a stop condition.
4. Confirm with [[DSK-03-13]] that every enumerated action has its **own** route (for example `POST /api/v1/triage/{id}/await-information`, `…/findings`, `…/findings/{fid}/supersede`, `…/responses/link`, `…/responses/unlink`, `…/complete`, `…/cancel`, `…/reopen`, `…/case-link`, `…/case-unlink`, plus the assignment routes), each carrying the triage `expectedVersion` and an `operationKey`, and that `TriageVersionConflictException` maps to a 409 problem carrying the current version.
5. Add the triage DTOs to `src/Pegasus.Contracts`: list item, detail (including the evidence and response candidates), finding payloads, and one request record per command.
6. Implement `TriageListViewModel` over `GET /api/v1/triage?page&state` using the data-table pattern from [[DSK-06-07]] with state as a dropdown filter, newest first.
7. Implement `TriageDetailViewModel` with one command object per action — no dispatcher string anywhere in the desktop — each with `CanExecute` derived from the loaded state and the actor's rights, a reason dialog where Core requires a reason, and the shared conflict pattern from [[DSK-05-08]] on 409.
8. Replace "Assign to me" with an Engineer selection per upstream INTK-019, which this slice absorbs; the assignment command takes the selected engineer's identity rather than implying the current user.
9. Implement the source download over `GET /api/v1/triage/{id}/source` as a streamed transfer with progress and cancel, using the same streaming service as [[DSK-05-09]] — one implementation, not a copy.
10. Add contract tests in `tests/Pegasus.Api.ContractTests` covering, for every enumerated action, the seven-case matrix from [[DSK-08-02]]: success, 401, 403, 409 stale version, 400 bad input problem, replay of the same `operationKey`, and the Core-specific failure. Enable `Features:DesktopGateway` explicitly.
11. Add view-model tests in `tests/Pegasus.Desktop.ViewModelTests` for `CanExecute` per state, reason-required commands, finding and supersede payload validation, and response link/unlink candidate selection.
12. **Operator step** — run the triage UAT script covering the full enumerated action set on the local Test/UAT stack, confirming each outcome and its audit row. Capture the operator's sign-off text and date in the ticket proof.
13. Update `docs/desktop/01-inventory-and-parity/parity-matrix.md` for the triage rows, add the triage section to `docs/frd/frd-13-desktop-operator-experience.md`, run the simplification pass over the branch diff, record it under a dated `## Simplification pass` heading, then open the PR into `dev`.

## Acceptance criteria

- [ ] Every enumerated triage action exists as its own named command and endpoint; no dispatcher string in the desktop or the gateway.
- [ ] The actual action count is evidenced from the dispatcher and the discrepancy with the plan text is resolved and recorded.
- [ ] Every action has contract and authorization tests, and the action matrix has Core characterization tests.
- [ ] Assignment is by Engineer selection, not "Assign to me".
- [ ] Source download streams with progress and cancel, reusing the S9 streaming service.
- [ ] `Triage` keeps its settled business meaning in every operator string.

## Verification

- [ ] `dotnet test ./tests/Pegasus.Core.Tests/Pegasus.Core.Tests.csproj --configuration Release --no-build` — expected: triage action-matrix characterization facts pass.
- [ ] `dotnet test ./tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj --configuration Release --no-build` — expected: the seven-case matrix passes for every enumerated action.
- [ ] `dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release --no-build` — expected: state gating, reason and payload facts pass.
- [ ] UAT record in the ticket proof — expected: named operator sign-off with date across the full action set.

## Evidence tier

Tier 2 — Core/domain. Tier 5 — Web/API/MCP caller. Tier 7 — Browser/accessibility.
Tier 2 obliges positive, contradictory, ambiguous and failure cases for the triage lifecycle and its action matrix; tier 5 obliges route-level evidence per command including idempotency and exception translation; tier 7 obliges keyboard, focus and error-behaviour evidence from a real run.

## Documentation changes

- `docs/desktop/01-inventory-and-parity/parity-matrix.md` — triage list and detail rows
- `docs/frd/frd-13-desktop-operator-experience.md` — triage section, citing FRD-03
- `docs/capabilities.md` — `DSK` rows for the triage queue and actions

## Guardrails

- **Azure**: no write.
- **Scope boundary**: may touch `src/Pegasus.Desktop`, `src/Pegasus.Desktop.Infrastructure`, `src/Pegasus.Contracts`, the `/api/v1` triage group in `src/Pegasus.Web`, `src/Pegasus.Core/Triage/` only for rules moved in with a characterization test, and the test projects. Must not modify the Razor triage pages beyond re-pointing a moved rule.
- **Traps**: never a dispatcher string — one named command per action; do not assume the action count, enumerate it and record the discrepancy as an open question (an unticked open question blocks a Kanmer move); a page-model precondition that is business logic moves into Core with a test first; `Triage` is a reserved business word and keeps its settled meaning; `Features:DesktopGateway` must be enabled in tests; parity drift — record the SHA of `Details.cshtml.cs` characterized.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.

## Outcome

_Filled at closeout._
