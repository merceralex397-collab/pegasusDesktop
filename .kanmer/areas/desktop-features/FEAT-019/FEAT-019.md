---
id: FEAT-019
type: ticket
title: DSK-05-19 · S19 Administration
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
  - needs-operator
groups:
  - EPIC-006
  - HZN-009
links: []
refs:
  - docs/frd/frd-04-parties-accounts-and-access.md
docs_todo: true
archived: false
created: '2026-08-24T07:59:40.195Z'
updated: '2026-08-24T07:59:40.195Z'
---

## What

Deliver the administrator-only native Administration area: workflow configuration, approved mail categories, approved mailboxes (update and resolve folders), access review, staff accounts (create and disable), role assignment, automation clients (enable, Send-to-AI, connector, channel-token rotate and clear) and automation activity — every mutation audited and refused for non-administrators by the gateway.

## Why

Proposal §13.10 requires users, roles, reference-data administration, integration health, diagnostics and audit search on the desktop. Today it is the sixteen page models under `src/Pegasus.Web/Pages/Administration/` sharing `AdministrationPageModel.cs`; this slice owns all of them except the organizations and principals screens, which are [[DSK-05-07]]. Each area is gated by its own `StaffAccessRight` from `src/Pegasus.Core/Identity/StaffAuthorization.cs`: `ManageWorkflowConfiguration`, `ManageApprovedOutlookCategories`, `ManageApprovedMailboxes`, `ReviewStaffAccess`, `ManageStaffAccounts`, `AssignStaffRoles`, `ManageAutomationClients`. The Phase 8 exit gate requires the full automated suite to pass with no unresolved high-risk security finding. Siblings: [[DSK-05-07]] establishes the administration screen patterns, [[DSK-03-15]] supplies the endpoints, [[DSK-05-20]] adds Operations.

## Source of truth

- Plan row: `docs/desktop/05-implementation-and-migration/README.md` § 5 — `DSK-05-19`
- Plan detail: `docs/desktop/05-implementation-and-migration/vertical-slices.md` § `S19 · Administration (DSK-05-19)`
- Endpoint map: `docs/desktop/03-gateway-api-and-data/endpoint-map.md` § `Administration and audit`
- Screen spec: `docs/desktop/06-ui-design/screen-specs.md` § `§13.10 Administration and operations` → `Administration`
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 13.10 Administration and operations, § 17 Security and privacy
- Repository evidence: `src/Pegasus.Web/Pages/Administration/Configuration.cshtml.cs:40`, `:52`; `MailCategories.cshtml.cs:24`, `:32` (`OnPostSaveAsync`); `Mailboxes.cshtml.cs:45`, `:58` (`OnPostUpdateAsync`), `:167` (`OnPostResolveFoldersAsync`); `Access/Index.cshtml.cs:26`, `:37` (`OnPostReviewAsync`); `Accounts/Index.cshtml.cs:32`, `:43` (`OnPostCreateAsync`); `Accounts/Edit.cshtml.cs:22`, `:34` (`OnPostDisableAsync`); `Roles/Index.cshtml.cs:48`, `:59` (`OnPostAssignAsync`); `Automation/Index.cshtml.cs:45`, `:57` (`OnPostSetEnabledAsync`), `:95` (`OnPostSetSendToAiEnabledAsync`), `:128` (`OnPostUpdateConnectorAsync`), `:168` (`OnPostRotateChannelTokenAsync`), `:207` (`OnPostClearChannelTokenAsync`); `Automation/Activity.cshtml.cs:23`; `src/Pegasus.Core/Identity/StaffAuthorization.cs` (the twelve rights and the fail-closed switch), `src/Pegasus.Core/Identity/StaffAccountAdministration.cs`, `src/Pegasus.Core/Workflow/` (`ICaseWorkflowConfiguration`); ADR-0022 and ADR-0024 for approved mailbox identity
- Binding decisions: L-01 the gateway owns authorization and audit; L-02 verification on the local Test/UAT stack; L-04 routing named on the ticket
- Depends on: `DSK-05-07` the administration screen patterns and role-aware navigation; `DSK-03-15` the administration endpoints

## Routing

- **Subagent**: `winui-dev` — `.codex/agents/winui-dev.toml`; `pegasus-gateway-dev` — `.codex/agents/pegasus-gateway-dev.toml`; `pegasus-test-engineer` — `.codex/agents/pegasus-test-engineer.toml`
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `dotnet-webapi` (dotnet/skills `98f84851`, `plugins/dotnet-aspnetcore/skills/dotnet-webapi/SKILL.md`) → `winui-dev-workflow` (`.codex/skills/winui-dev-workflow/SKILL.md`) → `winui-design` (`.codex/skills/winui-design/SKILL.md`) → `code-testing-agent` (dotnet/skills `98f84851`) → `run-tests` → `winui-code-review` at review
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search`)
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (call `get_doc_gates <id>` before every move; a move crosses at most one gated boundary)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Orient: read the plan row, `vertical-slices.md` § S19, the screen spec Administration section, `docs/frd/frd-04-parties-accounts-and-access.md` § staff role access matrix, and ADR-0022 and ADR-0024. Call `get_doc_gates <this ticket id>`, then `take_ticket` with branch `task/dsk-05-19-administration` and worktree `../pegasus-worktrees/dsk-05-19-administration` from `origin/dev`.
2. Read every in-scope page model under `src/Pegasus.Web/Pages/Administration/` and `AdministrationPageModel.cs`. Tabulate in `research`, per screen: the handlers listed under Source of truth, the Core use case each calls, the exact `StaffAccessRight` it requires, whether Core needs a version, an `operationKey` or a `reason`, and which mutations write a `ISecurityEventWriter` or `IActionHistoryWriter` record. Record the SHA read.
3. Confirm the endpoints from [[DSK-03-15]] cover every row, each with its own named route and its own right — configuration, mail categories, mailboxes (update and resolve-folders), access review, accounts (list, create, get, disable), roles (list, assign), automation (enable, send-to-ai-enabled, connector, channel-token rotate, channel-token clear, activity).
4. Add the administration DTOs to `src/Pegasus.Contracts`, keeping each resource's version on the wire. The channel-token rotate response must carry the token exactly once and the DTO must not be persisted or logged by the client.
5. Implement one view model per screen in `src/Pegasus.Desktop` on the list pattern from [[DSK-06-07]] and the form pattern from [[DSK-06-08]]. Consolidate accounts, roles and access review into one Administration › People area as upstream PLAT-027 asks, which this slice absorbs.
6. Implement each mutation as an explicit reasoned command where Core requires a reason, using the dialog contract from [[DSK-06-09]]; a destructive or consequential action (disable an account, clear a channel token) shows its consequence without hover.
7. Implement mailbox folder resolution as a distinct command that shows its result, remembering that the resolver is a Web-only Graph read — the desktop calls the gateway endpoint and never Graph.
8. Apply role-aware navigation from [[DSK-04-10]]: each Administration entry is absent when the actor lacks its right; the gateway still refuses a forged call with a `not-authorized` problem.
9. Handle the rotated channel token as a one-time reveal: shown once, copyable, never written to the local cache, a log or a diagnostics bundle. Add a view-model test asserting it is not retained after the dialog closes.
10. Add authorization contract tests in `tests/Pegasus.Api.ContractTests` for **every** endpoint: 200 with the correct right, 403 `not-authorized` with any other right and for the Automation Actor, 401 without a token, 409 on a stale version, replay of the same `operationKey`, and an assertion that each sensitive mutation produced an audit record. Enable `Features:DesktopGateway` explicitly.
11. Add view-model tests in `tests/Pegasus.Desktop.ViewModelTests` for each screen's load, validation, reason-required commands, and the token non-retention rule.
12. **Operator step** — run the administration UAT script on the local Test/UAT stack covering configuration change, mailbox update and folder resolve, access review, account create and disable, role assignment, and each automation control. Capture the operator's sign-off text and date in the ticket proof.
13. Update `docs/desktop/01-inventory-and-parity/parity-matrix.md` for the administration rows, add the administration section to `docs/frd/frd-13-desktop-operator-experience.md`, run the simplification pass over the branch diff, record it under a dated `## Simplification pass` heading, then open the PR into `dev`.

## Acceptance criteria

- [ ] Every in-scope administration screen exists natively and is reachable only by an actor holding its right.
- [ ] Every endpoint returns 403 `not-authorized` for a non-administrator and for the Automation Actor, even when the UI is bypassed.
- [ ] Every sensitive mutation writes an audit record, asserted by test.
- [ ] A rotated channel token is revealed once and never persisted to cache, log or diagnostics bundle.
- [ ] Accounts, roles and access review are consolidated into one People area (upstream PLAT-027).
- [ ] Consequential actions show their consequence without hover, using approved copy only.

## Verification

- [ ] `dotnet test ./tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj --configuration Release --no-build` — expected: the authorization matrix and audit-record facts pass for every administration endpoint.
- [ ] `dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release --no-build` — expected: per-screen load, validation, reason and token-non-retention facts pass.
- [ ] `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --no-build --filter "Category!=Corpus&Category!=Browser"` — expected: existing administration web tests stay green.
- [ ] UAT record in the ticket proof — expected: named operator sign-off with date across every administration screen.

## Evidence tier

Tier 5 — Web/API/MCP caller. Tier 7 — Browser/accessibility.
Tier 5 obliges route-level evidence per endpoint that the real route reaches Core with the correct right, idempotency, exception translation and the right action-history actor; tier 7 obliges keyboard, focus, validation-summary and semantic-label evidence from a real run of every screen.

## Documentation changes

- `docs/desktop/01-inventory-and-parity/parity-matrix.md` — administration rows for the in-scope page models
- `docs/frd/frd-13-desktop-operator-experience.md` — administration section, citing FRD-04
- `docs/capabilities.md` — `DSK` rows per administration capability

## Guardrails

- **Azure**: no write.
- **Scope boundary**: may touch `src/Pegasus.Desktop`, `src/Pegasus.Contracts`, the `/api/v1` administration group in `src/Pegasus.Web` and the test projects. Must not modify the Razor administration pages, and must not touch `src/Pegasus.Infrastructure/Email/` — mailbox folder resolution stays a gateway-side Graph read.
- **Traps**: fail-closed authorization is Core's rule (`StaffAuthorization.IsAuthorized`) — never re-implement the matrix in the desktop; a secret revealed once must not be retained anywhere the diagnostics bundle can reach; upstream PLAT-025, PLAT-026, PLAT-027, AUTO-006, AUTO-007 and PR-026 are absorbed here — check `docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md` before duplicating any of them, and do not pull in Send-to-AI capability work beyond the existing toggle (§13.11); `Features:DesktopGateway` must be enabled in tests.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.

## Outcome

_Filled at closeout._
