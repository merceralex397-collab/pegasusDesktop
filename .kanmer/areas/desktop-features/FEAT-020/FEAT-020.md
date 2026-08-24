---
id: FEAT-020
type: ticket
title: DSK-05-20 · S20 Operations and integration health
status: backlog
area: desktop-features
assignee: ''
profile: feature
labels:
  - desktop-conversion
  - plan-05
  - phase-8
  - tier-5
  - tier-7
groups:
  - EPIC-006
  - HZN-009
links: []
blocks:
  - FEAT-022
  - FEAT-025
  - TEST-016
refs:
  - docs/frd/frd-12-operator-experience.md
docs_todo: true
archived: false
created: '2026-08-24T07:59:40.211Z'
updated: '2026-08-24T13:33:25.980Z'
---

## What

Deliver the native Operations screen: retryable external work, active upload links, and integration health (Graph worker last cycle, Box, DVLA/DVSA, update feed, minimum client version) with explicit, audited retry and revoke commands.

## Why

Proposal §13.10 and §18.3 require failed-work and retry screens plus integration health appropriate to administrators, so a failure is visible and recoverable rather than discovered by a user. Today it is `src/Pegasus.Web/Pages/Operations/Index.cshtml.cs` (236 lines, `OnGetAsync` at `:57`, `OnPostRetryExternalAsync` at `:71`, `OnPostRevokeLinkAsync` at `:112`) over Core `src/Pegasus.Core/Operations/` and `src/Pegasus.Core/Custody/` external-work contracts, plus the existing `/health/live` and `/health/ready` endpoints. The health description must never expose a secret. Upstream INTK-004 records a second, quieter dishonesty on this surface: the as-built document claims the Operations row joins the actual Case link while the store hard-codes `CaseId: null`. Siblings: [[DSK-05-01]] shares the dashboard's failure counts, [[DSK-03-13]] supplies the operations endpoints, [[DSK-07-01]] and [[DSK-07-02]] supply the intake-status and retry surfaces, [[DSK-07-04]] owns the Operations screen itself.

## Source of truth

- Plan row: `docs/desktop/05-implementation-and-migration/README.md` § 5 — `DSK-05-20`
- Plan detail: `docs/desktop/05-implementation-and-migration/vertical-slices.md` § `S20 · Operations and integration health (DSK-05-20)`
- Endpoint map: `docs/desktop/03-gateway-api-and-data/endpoint-map.md` § `Triage, Unidentified, Operations` (`GET /operations`, retry-external, revoke-link) and § `Session, compatibility, diagnostics` (`GET /admin/health`)
- Screen spec: `docs/desktop/06-ui-design/screen-specs.md` § `§13.10 Administration and operations` → `Operations`
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 13.10 Administration and operations, § 18.3 Health
- Repository evidence: `src/Pegasus.Web/Pages/Operations/Index.cshtml.cs:57`, `:71`, `:112`; `src/Pegasus.Core/Operations/` (operations projection, `IDashboardQueries`), `src/Pegasus.Core/Custody/CustodyContracts.cs` (`IExternalWorkStore`, `IExternalWorkEnqueuer`), `src/Pegasus.Core/Documents/RequestUploadPolicy.cs` (link revoke); `src/Pegasus.Infrastructure/Persistence/EfOperationsStore.cs:159` (`CaseId: null` on the received-intake row) against the claim at `docs/current-architecture.md:291` ("Operations, retained Mail, Upload, MCP, and retry surfaces join the current allocation state and actual Case link"); `src/Pegasus.Web/Health/DatabaseReadinessHealthCheck.cs` and the `/health/*` and `/diagnostics/version` endpoints in `src/Pegasus.Web/Program.cs`
- Upstream evidence: upstream `PLAT-023` (redesign the Operations workspace — retryable work, upload links, integration health, no colour-only state); upstream `INTK-004` second half — the as-built document and the store disagree about the received-intake row's Case link, and one of them is wrong
- Binding decisions: L-01 the gateway owns the snapshot, the retries and the audit; L-02 verification on the local Test/UAT stack; L-04 routing named on the ticket
- Depends on: `DSK-05-01` the shell and the dashboard failure counts this screen drills into; `DSK-03-13` the operations snapshot, retry-external and revoke-link endpoints; `DSK-07-01` the intake-status and external-work health endpoints; `DSK-07-04` — owns `OperationsViewModel` and `OperationsPage.xaml`; this slice adds the audited retry and revoke commands to them

## Routing

- **Subagent**: `winui-dev` — `.codex/agents/winui-dev.toml`; `pegasus-gateway-dev` — `.codex/agents/pegasus-gateway-dev.toml`; `pegasus-test-engineer` — `.codex/agents/pegasus-test-engineer.toml`
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `dotnet-webapi` (dotnet/skills `98f84851`) → `winui-dev-workflow` (`.codex/skills/winui-dev-workflow/SKILL.md`) → `winui-design` (`.codex/skills/winui-design/SKILL.md`) → `run-tests` → `winui-code-review` at review
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search`)
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (call `get_doc_gates <id>` before every move; a move crosses at most one gated boundary)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Orient: read the plan row, `vertical-slices.md` § S20, the screen spec Operations section and `docs/desktop/10-security-observability-performance/README.md` for what the health surface may disclose. Call `get_doc_gates <this ticket id>`, then `take_ticket` with branch `task/dsk-05-20-operations` and worktree `../pegasus-worktrees/dsk-05-20-operations` from `origin/dev`.
2. Read `src/Pegasus.Web/Pages/Operations/Index.cshtml.cs` in full. Record in `research` what the snapshot projects, which external-work items are retry-eligible and why, what revoking an upload link does, and the reason each command requires. Record the SHA read.
3. Confirm the endpoints: `GET /api/v1/operations` (snapshot with `ETag`), `POST /api/v1/operations/external-work/{wid}/retry`, `POST /api/v1/operations/upload-links/{lid}/revoke` from [[DSK-03-13]], and the integration-health payload from [[DSK-07-01]] plus `GET /api/v1/admin/health` for dependency states, minimum client version and feed state. Then settle the honest-case-link question (upstream INTK-004): `src/Pegasus.Infrastructure/Persistence/EfOperationsStore.cs:159` hard-codes `CaseId: null` for the received-intake row, while `docs/current-architecture.md:291` claims these surfaces "join the current allocation state and actual Case link". **Either** the snapshot carries the real case link for a received-intake row — resolved through the single `IntakeReceipt.CurrentCaseId` path [[DSK-05-13]] uses, never a second copy — **or** the claim is removed from `docs/current-architecture.md`. A row must not report a link it does not join. Decide which with [[DSK-03-13]], who owns the projection, and record the decision and its evidence in the plan.
4. Add the operations and health DTOs to `src/Pegasus.Contracts`. The health payload names each dependency and its state and last-cycle time — it carries no connection string, endpoint credential, token or internal host name.
5. Check whether `OperationsViewModel` already exists from [[DSK-07-04]], which owns that type and its page. If it does, add the retry and revoke commands to it in place and change no existing member; if it has not landed, create it with exactly the members [[DSK-07-04]] step 3 pins (`ObservableObject`, `[RelayCommand]`, no UI type in the view model) and record in the plan document which case applied. Either way this slice's own additions are the same: retryable external work and active upload links as two lists on the data-table pattern from [[DSK-06-07]], plus an integration-health panel showing each dependency's state with text (never colour alone) and its last-cycle time in Europe/London through the shared vocabulary map. Never a second view model for the Operations screen.
6. Implement retry and revoke as explicit commands with an `operationKey` and the reason Core requires, showing the outcome inline. A retry is offered only when the gateway says the item is eligible — the client does not infer eligibility.
7. Show the update-feed state and the minimum client version from the compatibility surface built by [[DSK-04-06]], so an administrator can see why a workstation is being blocked.
8. Add contract tests in `tests/Pegasus.Api.ContractTests`: snapshot 200 with `ETag`, 401, 403, retry success and retry of an ineligible item refused with a problem, revoke success and replay returning the same result, an assertion that the health payload contains no secret-shaped value, and — per step 3's recorded decision — either a fact that a received-intake row for an associated receipt carries the resolved case link, or a fact that the row carries no link at all and the document no longer claims one. Enable `Features:DesktopGateway` explicitly.
9. Add view-model tests in `tests/Pegasus.Desktop.ViewModelTests` for list loading, eligibility-driven command enablement, retry and revoke outcomes, and health-state rendering including an unavailable dependency.
10. Exercise end-to-end business scenario 13 from `docs/desktop/08-testing/README.md` on the local Test/UAT stack: cause an external-work failure, see it on this screen, retry it, and see it clear. Record the run in the ticket proof.
11. Update `docs/desktop/01-inventory-and-parity/parity-matrix.md` for the operations rows, add the retry and revoke command behaviour inside the Operations screen section [[DSK-07-04]] creates in `docs/frd/frd-13-desktop-operator-experience.md` (a sub-heading under that section, not a second screen section), apply step 3's `docs/current-architecture.md` correction if that is the recorded decision, run the simplification pass over the branch diff, record it under a dated `## Simplification pass` heading, then open the PR into `dev`.

## Acceptance criteria

- [ ] Retryable external work and active upload links are listed with their state and age.
- [ ] Integration health names each dependency's state and last cycle without exposing any secret or internal host name.
- [ ] The operations snapshot either carries the real case link for a received-intake row or the claim is removed from `docs/current-architecture.md`; a row must not report a link it does not join (upstream INTK-004).
- [ ] Retry and revoke are explicit, audited commands; retry is offered only for gateway-declared eligible items.
- [ ] The update-feed state and minimum client version are visible to an administrator.
- [ ] A failure raised in end-to-end scenario 13 is visible here and recoverable from here.
- [ ] No colour-only state anywhere on the screen.

## Verification

- [ ] `dotnet test ./tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj --configuration Release --no-build` — expected: snapshot, retry, revoke, no-secret-in-health and honest-case-link facts pass.
- [ ] `dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release --no-build` — expected: eligibility, outcome and health-state facts pass.
- [ ] `pwsh ./tests/Pegasus.Desktop.UITests/ui-tests.ps1 -Script operations` — expected: keyboard traversal and retry command pass; axe report attached.
- [ ] Scenario-13 record in the ticket proof — expected: an induced failure is visible and recoverable on the Test/UAT stack.

## Evidence tier

Tier 5 — Web/API/MCP caller. Tier 7 — Browser/accessibility.
Tier 5 obliges route-level evidence that the snapshot and both commands reach Core with authorization, idempotency and exception translation; tier 7 obliges keyboard, focus, semantic-label and text-plus-colour evidence for the health panel from a real run.

## Documentation changes

- `docs/desktop/01-inventory-and-parity/parity-matrix.md` — operations rows
- `docs/current-architecture.md:291` — if step 3's decision is to leave the row unlinked, correct the sentence so it describes what the Operations received-intake row actually joins; if the decision is to carry the link, the sentence becomes true and is left as it stands, with the evidence recorded (upstream INTK-004)
- `docs/frd/frd-13-desktop-operator-experience.md` — the retry and revoke command behaviour inside the Operations screen section [[DSK-07-04]] creates; this ticket adds no second screen section
- `docs/capabilities.md` — `DSK` rows for operations and integration health

## Guardrails

- **Azure**: no write. Application Insights and Azure resource state are read-only inputs owned by plan 10 and plan 11; this screen shows only what the gateway health endpoint returns.
- **Scope boundary**: may extend `OperationsViewModel` and `OperationsPage.xaml` in `src/Pegasus.Desktop` — [[DSK-07-04]] owns both and this slice adds members to them rather than creating its own — and may touch `src/Pegasus.Contracts`, the `/api/v1` operations group in `src/Pegasus.Web` and the test projects. Must not modify `src/Pegasus.Web/Health/DatabaseReadinessHealthCheck.cs` beyond extension agreed with plan 10, and must not touch `src/Pegasus.Worker`. The `EfOperationsStore` projection change, if step 3 decides on one, belongs to [[DSK-03-13]].
- **Traps**: health must be described without secrets (proposal §18.3); no colour-only state; a row that reports a link it does not join is the same class of dishonesty as a colour-only state and is not fixed by rendering — one case-id resolution, `IntakeReceipt.CurrentCaseId`'s, and a second copy is a stop condition; App Insights quota can hide failures (recorded trap PLAT-034), so the pilot evidence is the desktop diagnostics bundle rather than a telemetry query; upstream PLAT-023 (redesign the Operations workspace) is absorbed by this screen spec — **note the collisions: neither upstream PLAT-023 nor upstream INTK-004 has a fork ticket, the board's `PLAT-023` is `DSK-11-05` (the resource-health and advisor read) and the board's `INTK-004` is upstream INTK-027, a different ticket entirely; this screen owns upstream INTK-004's Operations half and [[DSK-05-23]] owns its label half** (`HZN-001` group document `board-conventions.md` § Upstream ids versus board ids holds the join table); `Features:DesktopGateway` must be enabled in tests. One view model per screen: [[DSK-07-04]] owns `OperationsViewModel`, this ticket extends it; a second view model for the same screen is a stop condition.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.

## Outcome

_Filled at closeout._
