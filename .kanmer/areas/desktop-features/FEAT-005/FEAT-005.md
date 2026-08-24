---
id: FEAT-005
type: ticket
title: 'DSK-05-05 · S5 Case edit with lease, version and completeness'
status: backlog
area: desktop-features
assignee: ''
profile: feature
labels:
  - desktop-conversion
  - plan-05
  - phase-4
  - tier-4
  - tier-5
  - tier-7
  - tier-12
  - needs-operator
groups:
  - EPIC-006
  - HZN-005
links: []
blocks:
  - FEAT-006
  - FEAT-008
  - FEAT-009
  - FEAT-014
  - FEAT-015
  - FEAT-017
  - FEAT-022
  - FEAT-024
  - FEAT-025
  - TEST-007
  - PLAT-017
refs:
  - docs/frd/frd-01-case-identity-and-lifecycle.md
docs_todo: true
archived: false
created: '2026-08-24T07:46:33.843Z'
updated: '2026-08-24T08:51:13.290Z'
---

## What

Deliver safe native case editing: claim, renew and release the edit lease, save with the expected version and lease token, confirm completeness, show an explicit dirty state and warn before discarding — so a second writer is told about the conflict and nothing is silently overwritten.

## Why

Proposal §10.4, §13.3 and §14.5 require deliberate saves, explicit dirty state and detected concurrent edits. The rules already exist transport-neutrally in Core: `CaseMutationRequest` carries `CaseId`, `ExpectedVersion`, `ActionActor`, `OperationKey`, `Reason` and `EditLeaseToken` (`src/Pegasus.Core/Workflow/CaseWorkflowContracts.cs:182`), leases are `IAcquireCaseEditLease` / `IRenewCaseEditLease` / `IReleaseCaseEditLease` (`src/Pegasus.Core/Workflow/CaseCommandContracts.cs:77-95`) and a stale write throws `CaseVersionConflictException`. What must not travel is the web's state machine — `src/Pegasus.Web/Pages/Cases/CaseMutationPageModel.cs:36-80` retains proposed values in cookie `TempData`. This slice is the Phase 4 gate: a two-user conflict test must pass. Siblings: [[DSK-05-03]] provides the workspace, [[DSK-03-08]] the lease and save endpoints, [[DSK-05-08]] builds the recovery UX on the problems this slice surfaces.

## Source of truth

- Plan row: `docs/desktop/05-implementation-and-migration/README.md` § 5 — `DSK-05-05`
- Plan detail: `docs/desktop/05-implementation-and-migration/vertical-slices.md` § `S5 · Case edit with lease, version and completeness (DSK-05-05)` and § `Common to every slice` (concurrency and idempotency)
- Endpoint map: `docs/desktop/03-gateway-api-and-data/endpoint-map.md` § `Cases` (`POST /cases/{id}/lease/claim|renew|release`, `PUT /cases/{id}`, `POST /cases/{id}/confirm-completeness`)
- Screen spec: `docs/desktop/06-ui-design/screen-specs.md` § `§13.3 Case lifecycle` → `Case workspace`
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 10.4 Concurrency, § 13.3, § 14.5 Case workspace
- Repository evidence: `src/Pegasus.Web/Pages/Cases/Details.cshtml.cs:156` (`OnPostClaimLeaseAsync`), `:203` (`OnPostRenewLeaseAsync`), `:250` (`OnPostReleaseLeaseAsync`), `:293` (`OnPostConfirmCompletenessAsync`), `:324` (`OnPostSaveAsync`); `src/Pegasus.Web/Pages/Cases/CaseMutationPageModel.cs:36-80`; `src/Pegasus.Core/Workflow/CaseWorkflowContracts.cs:182` and `:322-334`; `src/Pegasus.Core/Workflow/CaseCommandContracts.cs:77-95`; `src/Pegasus.Core/Workflow/CaseEditAuthority.cs` (64-hex lease tokens); `tests/Pegasus.IntegrationTests/CaseWorkflowPersistenceTests.cs` (2,194 lines); `tests/Pegasus.IntegrationTests/CaseDetailsWebTests.cs`
- Binding decisions: L-01 the gateway owns lease, version and audit; L-02 the two-user test runs against LocalDB in the local Test/UAT stack; L-04 routing named on the ticket
- Depends on: `DSK-05-03` the case workspace shell; `DSK-03-08` the lease claim/renew/release, save and confirm-completeness endpoints with typed 409 problems

## Routing

- **Subagent**: `winui-dev` — `.codex/agents/winui-dev.toml`; `pegasus-gateway-dev` — `.codex/agents/pegasus-gateway-dev.toml`; `pegasus-test-engineer` — `.codex/agents/pegasus-test-engineer.toml`; `pegasus-desktop-reviewer` — `.codex/agents/pegasus-desktop-reviewer.toml` (independent review of the concurrency behaviour)
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `winui-dev-workflow` (`.codex/skills/winui-dev-workflow/SKILL.md`) → `winui-design` (`.codex/skills/winui-design/SKILL.md`) → `dotnet-webapi` (dotnet/skills `98f84851`) → `test-gap-analysis` (dotnet/skills `98f84851`, `plugins/dotnet-test/skills/test-gap-analysis/SKILL.md`) → `run-tests` → `winui-code-review` at review
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search`)
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (call `get_doc_gates <id>` before every move; a move crosses at most one gated boundary)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Orient: read the plan row, `vertical-slices.md` § S5 and § `Common to every slice`, and `docs/frd/frd-01-case-identity-and-lifecycle.md` § case edit authority. Call `get_doc_gates <this ticket id>`, then `take_ticket` with branch `task/dsk-05-05-case-edit` and worktree `../pegasus-worktrees/dsk-05-05-case-edit` from `origin/dev`.
2. Read the five handlers in `src/Pegasus.Web/Pages/Cases/Details.cshtml.cs` (`:156`, `:203`, `:250`, `:293`, `:324`) and `CaseMutationPageModel.cs` in full. In `research`, separate the **business** behaviour (lease acquisition, renewal window, version check, completeness precondition, reason requirements) from the **web mechanics** (TempData retention, PRG redirect, antiforgery) and record the SHA read. Only the first list is carried over.
3. Confirm from [[DSK-03-08]] that the endpoints accept and return exactly the Core shapes: `expectedVersion`, `operationKey`, `editLeaseToken` and `reason` where Core requires; that lease claim replay with the same `operationKey` returns the same token and expiry; and that a stale write returns a 409 problem carrying the **current version** and, for a lease conflict, the holder. Lease tokens are 64 hex characters (`src/Pegasus.Core/Workflow/CaseEditAuthority.cs`) and must never be rendered to the operator.
4. Implement `CaseEditSession` in `src/Pegasus.Desktop.Infrastructure`: claim on entering edit, renew on a timer inside the server's renewal window, release on exit, and raise a `LeaseLost` event when a renew fails. It holds the lease token in memory only; it is never written to disk or to a log.
5. Extend `CaseWorkspaceViewModel` from [[DSK-05-03]] with edit state: an explicit dirty indicator, a deliberate `SaveCommand` (never an autosave), a navigation guard that warns before discarding unsaved work, and field-level validation that runs immediately using the deterministic Core rules referenced from `Pegasus.Core`.
6. Wire `Ctrl+S` to `SaveCommand` per proposal §14.9 and disable it while the lease is not held or while the session is offline (see [[DSK-04-11]] — no silent queueing of saves).
7. Send every save as a `CaseMutationRequest`-shaped body with the `expectedVersion` the workspace loaded, the held `editLeaseToken`, a fresh `operationKey` per user-initiated attempt, and reuse of the same `operationKey` on a transport retry. On an uncertain outcome (timeout after send) re-query the case rather than resending blind.
8. Surface the three failure states with the shared vocabulary: version conflict (another user changed the case), lease lost, and lease taken by a named holder. The full reload-compare-reapply pattern is designed and implemented in [[DSK-05-08]]; this slice must make the states unambiguous and never silently overwrite.
9. Implement completeness confirmation as an explicit command with the reason dialog contract from [[DSK-06-09]]; the precondition rules stay in Core, and any rule found only in the page model moves into `src/Pegasus.Core/Cases/` with a characterization test in `tests/Pegasus.Core.Tests` first.
10. Write view-model tests in `tests/Pegasus.Desktop.ViewModelTests`: dirty state on edit, navigation guard, save disabled without a lease, operation-key reuse on retry, lease-lost handling, and 409 mapped to the conflict state with the current version captured.
11. Add a two-user integration test in `tests/Pegasus.IntegrationTests` (LocalDB) driving the gateway directly: user A claims the lease and saves at version N; user B saves at version N and receives a 409 carrying version N+1; A's write is intact. Add contract tests for claim/renew/release replay, expiry, and release by a non-holder.
12. **Operator step** — run the two-user UAT script on the local Test/UAT stack with two real workstations or two sessions, confirming the second writer sees the conflict, can reload and compare, and that no value was lost. Capture the operator's sign-off text and date in the ticket proof.
13. Update `docs/desktop/01-inventory-and-parity/parity-matrix.md` row `PAR-08` (edit handlers), add the edit section to `docs/frd/frd-13-desktop-operator-experience.md`, run the simplification pass over the branch diff, record it under a dated `## Simplification pass` heading, then open the PR into `dev`.

## Acceptance criteria

- [ ] Editing requires a claimed lease; the lease renews automatically while the editor is open and is released on exit.
- [ ] Save is deliberate, `Ctrl+S`-bound and disabled without a lease or while offline.
- [ ] Dirty state is explicit and navigation warns before discarding unsaved work.
- [ ] A stale write returns and renders a conflict carrying the current version; nothing is silently overwritten.
- [ ] Lease loss surfaces immediately and the lease token never appears in the UI or in a log.
- [ ] Unsaved values live in the view model (or an encrypted local draft), never in a `TempData` equivalent.

## Verification

- [ ] `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --no-build --filter "Category!=Corpus&Category!=Browser"` — expected: the new two-user conflict facts pass and `CaseWorkflowPersistenceTests` stays green.
- [ ] `dotnet test ./tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj --configuration Release --no-build` — expected: lease claim/renew/release replay, expiry, non-holder release and 409-with-current-version facts pass.
- [ ] `dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release --no-build` — expected: dirty state, navigation guard, operation-key and lease-lost facts pass.
- [ ] `pwsh ./tests/Pegasus.Desktop.UITests/ui-tests.ps1 -Script case-edit` — expected: edit, save and conflict message assertions pass without sleeps.
- [ ] UAT record in the ticket proof — expected: named operator sign-off with date for the two-user conflict scenario.

## Evidence tier

Tier 4 — LocalDB persistence. Tier 5 — Web/API/MCP caller. Tier 7 — Browser/accessibility. Tier 12 — Integrated workflow.
Tier 4 obliges lease, stale-version and concurrency evidence against a real LocalDB with action-history atomicity; tier 5 obliges route-level idempotency and exception-translation evidence; tier 7 obliges the two-session editing, keyboard and error-behaviour evidence; tier 12 obliges the end-to-end run through Core, SQL and the persisted operator view rather than a mocked path.

## Documentation changes

- `docs/desktop/01-inventory-and-parity/parity-matrix.md` — row `PAR-08` edit handlers
- `docs/frd/frd-13-desktop-operator-experience.md` — edit and lease section
- `docs/capabilities.md` — `DSK` row for case edit

## Guardrails

- **Azure**: no write.
- **Scope boundary**: may touch `src/Pegasus.Desktop`, `src/Pegasus.Desktop.Infrastructure`, `src/Pegasus.Contracts`, the `/api/v1` cases command group in `src/Pegasus.Web`, `src/Pegasus.Core` only for rules moved in with a characterization test, and the test projects. `CaseMutationPageModel.cs` stays untouched — its retirement for desktop paths is [[DSK-05-24]].
- **Traps**: no TempData-retained proposed values, no PRG, no antiforgery in the desktop path; the lease token is a secret-shaped value and is banned from operator copy (`lease` is a banned word — use the settled operator vocabulary); a rule found only in the page model must move into Core with a test first; `Features:DesktopGateway` must be enabled in tests; upstream CASE-021 (refuse Review for a case with no images) is a gateway rule that must be true before this row reaches parity.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.

## Outcome

_Filled at closeout._
