---
id: FEAT-037
type: ticket
title: >-
  DSK-07-11 · Outbound command pattern: desktop confirms, gateway authorises and
  executes with an idempotency key
status: backlog
area: desktop-features
assignee: ''
profile: feature
labels:
  - desktop-conversion
  - plan-07
  - phase-5
  - tier-5
groups:
  - EPIC-008
  - HZN-006
links: []
refs:
  - docs/frd/frd-11-reports-correspondence-and-reviewed-proposals.md
  - docs/frd/frd-08-email-mailbox-and-background-processing.md
docs_todo: true
archived: false
created: '2026-08-24T08:24:13.944Z'
updated: '2026-08-24T08:24:13.944Z'
---

## What

Establish the one outbound-action seam the conversion needs: the desktop builds and confirms a command, the gateway authorises and executes it exactly once under an idempotency key, and the provider message identifier and status are audited. Built against today's only outbound capability — exact sent-message evidence — with draft, queued, sent and failed as distinct states.

## Why

Proposal § 12.4 fixes the split: desktop creates and confirms, gateway authorises and queues or executes, the service credential stays central, duplicate sends are prevented by an idempotency key, and the final provider message identifier and status are audited. § 13.8 requires the draft/queued/sent/failed distinction to be explicit. This area's § 2 records that outbound mail today is limited to exact sent-message evidence and that MAIL-12/13/17/19 (compose, mailbox mutation, idempotent report send, automatic chasers) are **open upstream capabilities, not conversion scope** — so this ticket builds the seam and the state vocabulary, not a mail composer. Sibling: [[DSK-07-16]] consumes this seam for report finalise and send.

## Source of truth

- Plan row: `docs/desktop/07-integrations/README.md` § 5 — `DSK-07-11`
- Plan context: `docs/desktop/07-integrations/README.md` § 2 Evidence base (the outbound-mail paragraph), § 7 Risks and traps ("Scope creep into MAIL-12/13/17/19")
- Endpoint map: `docs/desktop/03-gateway-api-and-data/endpoint-map.md` § `Cases` (`POST /cases/{id}/assessment/send`, `/reconcile`) and the Conventions header's `operationKey` rule
- Screen spec: `docs/desktop/06-ui-design/screen-specs.md` § `§13.8 Communications — Case workspace › Communications tab and Inbox` (explicit draft / queued / sent / failed chips; AutomationIds `Case.Communications.Table`, `Case.Communications.Send`)
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 12.4 Email sending and other service actions, § 13.8 Communications, § 16.1 Operation model
- Repository evidence: `src/Pegasus.Web/Pages/Cases/Assessment/Index.cshtml.cs:583` (`OnPostSendAsync`), `:628` (`OnPostReconcileAsync`); `src/Pegasus.Core/Workflow/CaseWorkflowContracts.cs:82-95` (`ApprovedMailboxReportSentEvidence` — exact retained Sent evidence, no substitute permitted); `src/Pegasus.Core/Workflow/PollSentEvidence.cs:32-140`; `src/Pegasus.Worker/EmailEvidenceFunctions.cs:16` (`SentEvidencePollFunction`), `:53` (`DueWorkSweepFunction`); `src/Pegasus.Core/Operations/EmailOperations.cs:6-42` (`EmailOperationDirection`, `EmailOperationState`, `EmailOperationProjection`); `docs/current-architecture.md:453`
- Binding decisions: L-01 — the command endpoint lives in `Pegasus.Web`. ADR-0106 — the mail service credential stays central; the desktop never sends through Graph itself. L-02 — replay evidence is produced on the local stack.
- Depends on: `DSK-03-02` route-group skeleton; `DSK-03-03` right filter

## Routing

- **Subagent**: `pegasus-gateway-dev` — `.codex/agents/pegasus-gateway-dev.toml`
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `dotnet-webapi` (dotnet/skills `98f84851`) → `code-testing-agent` (dotnet/skills `98f84851`) → `run-tests`
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search` for idempotency-key patterns in ASP.NET Core minimal APIs)
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (call `get_doc_gates <id>` before every move; a move crosses at most one gated boundary)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Orient: read the plan row, this area's outbound-mail evidence paragraph and its scope-creep trap, plus `docs/frd/frd-11-reports-correspondence-and-reviewed-proposals.md`. Call `get_doc_gates <this ticket id>`, then `take_ticket` on branch `task/dsk-07-11-outbound-command-seam`.
2. Establish the boundary in `plan` before writing code: this ticket implements the **command seam and the state vocabulary** over the existing send/reconcile use cases. It does **not** build compose, mailbox mutation, automatic chasers or a new send channel — MAIL-12/13/17/19 stay upstream backlog (`docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md`).
3. Read `src/Pegasus.Web/Pages/Cases/Assessment/Index.cshtml.cs:583-660` and the Core send and reconcile use cases behind them. Record in `files` the required fields, the operation-key validation (`IsOperationKeyValid`), and what the reconcile path does when the provider result is unknown.
4. Define the outbound state vocabulary in exactly one place — `src/Pegasus.Contracts` — as `draft`, `queued`, `sent`, `failed`, `unknown`, mapped from `EmailOperationState` (`src/Pegasus.Core/Operations/EmailOperations.cs:12-18`). Do not create a second enum in the desktop; `AGENTS.md` § Simplicity rails forbids a second copy of a state vocabulary.
5. Implement `POST /api/v1/cases/{caseId}/assessment/send` over the existing send use case with `expectedVersion`, `editLeaseToken` and `operationKey`, returning the resulting state and, where the provider has answered, the audited provider message identifier. Authorisation is the gateway's, never the client's.
6. Guarantee single execution by key: a replay of the same `operationKey` returns the original result and performs no second provider effect. Prove it with a test that replays after success and asserts exactly one audit row and one provider identifier.
7. Represent `unknown` honestly. Proposal § 16.1 lists "uncertain" as a real operation state and `docs/current-architecture.md:85-90` keeps `unknown` distinct from success. An outbound command whose provider outcome is not yet known returns `unknown` with the reconcile path named — never an optimistic `sent`.
8. Keep sent evidence exact: the contract must not allow a client to assert that something was sent. Only `ApprovedMailboxReportSentEvidence` (`src/Pegasus.Core/Workflow/CaseWorkflowContracts.cs:82-95`) proves a send, and it is produced by the Worker's `SentEvidencePollFunction`, not by the desktop. Add a test that a client-supplied "sent" claim is refused.
9. Add `GET /api/v1/cases/{caseId}/communications` returning the outbound and inbound history with those five states, the discovery, link and sent times, and the correlating actor — the data the Communications tab renders.
10. Write contract tests in `tests/Pegasus.Api.ContractTests`: success; replay with the same key; missing or malformed key → `validation`; unauthorised actor → `not-authorized`; stale `expectedVersion` → `version-conflict`; a client-asserted send → refused; `unknown` rendered distinctly from `sent`.
11. Write an integration test following `tests/Pegasus.IntegrationTests/SentEvidencePollPersistenceTests.cs`: after a send whose evidence has not yet arrived, the state is `unknown`; after the evidence poll runs, the same operation reports `sent` with the provider identifier audited. Nothing in that path is driven by the desktop.
12. Update `docs/desktop/03-gateway-api-and-data/endpoint-map.md` with the communications read row and the send row's returned state. Then run the simplification pass over the branch diff, record it under a dated `## Simplification pass` heading in the plan document, and open the PR into `dev`.

## Acceptance criteria

- [ ] A duplicate send is impossible by key: replay returns the original result and produces no second provider effect.
- [ ] `draft`, `queued`, `sent`, `failed` and `unknown` are distinct on the wire, defined once in `src/Pegasus.Contracts`.
- [ ] `unknown` is never optimistically reported as `sent`; the reconcile path is named in the response.
- [ ] A client cannot assert that a message was sent; only retained Sent evidence proves it.
- [ ] The provider message identifier and status are audited for every completed send.
- [ ] No compose, mailbox-mutation or chaser capability is added — the seam only.

## Verification

- [ ] `dotnet test ./tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj --configuration Release` — expected: replay, validation, authorization, refused-client-claim and distinct-`unknown` facts pass.
- [ ] `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --filter "Category!=Corpus&Category!=Browser"` — expected: the unknown-then-sent evidence fact passes and existing sent-evidence tests stay green.
- [ ] `git diff --stat origin/dev -- src/Pegasus.Worker` — expected: empty output.

## Evidence tier

Tier 5 — Web/API/MCP caller.
Tier 5 obliges observable route-level evidence of authentication, validation, idempotency, exception translation and the action-history actor for the outbound command.

## Documentation changes

- `docs/desktop/03-gateway-api-and-data/endpoint-map.md` — communications read row and send state
- `docs/frd/frd-11-reports-correspondence-and-reviewed-proposals.md` — the outbound command seam clause (behaviour, not mechanism)

## Guardrails

- **Azure**: no write.
- **Scope boundary**: may touch `src/Pegasus.Web` (`/api/v1` case and communications groups), `src/Pegasus.Contracts` and the test projects. Must not touch `src/Pegasus.Worker`, `src/Pegasus.Infrastructure/Email/`, or add any outbound provider client.
- **Traps**: scope creep into MAIL-12/13/17/19 is out of conversion scope (proposal § 13.11) — only the seam is built; ADR-0106 keeps the mail service credential central and a Graph credential in the desktop package is a defect; `unknown` must never collapse into success; a second copy of the outbound state vocabulary is duplication; a new table for idempotency bookkeeping would need a runtime-role `Grant*` migration (`scripts/Test-MigrationGrants.ps1`) — reuse the existing operation-key mechanics instead.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.

## Outcome

_Filled at closeout._
