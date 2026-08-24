---
id: FEAT-027
type: ticket
title: >-
  DSK-07-01 · Gateway intake-status endpoints: per-mailbox cycles, failures,
  poison counts, retry eligibility
status: preparing
area: desktop-features
assignee: ''
profile: feature
stageEntered:
  preparing: '2026-08-24T21:31:39.549Z'
labels:
  - desktop-conversion
  - plan-07
  - phase-5
  - tier-5
groups:
  - EPIC-008
  - HZN-006
links: []
blocks:
  - FEAT-020
  - FEAT-028
  - FEAT-030
  - PLAT-015
refs:
  - docs/frd/frd-08-email-mailbox-and-background-processing.md
docs_todo: true
archived: false
created: '2026-08-24T08:18:48.602Z'
updated: '2026-08-24T21:31:39.549Z'
---

## What

Add two read endpoints to the `/api/v1` route group — `GET /api/v1/operations/intake-status` and `GET /api/v1/operations/external-work` — that project the **existing** Operations, retained-mail poll-health and email-operations read models into desktop contracts: per-mailbox last completed cycle, last failure code, next due time, failed and poison counts, and whether a human retry is currently eligible. No Worker code is written or changed.

## Why

Proposal § 12.1 keeps Graph polling central (ADR-0106) *and* requires that "the desktop shows ingestion status and failures through the gateway"; § 13.10 lists integration health and failed-work screens as parity. Today the only surface is `src/Pegasus.Web/Pages/Operations/Index.cshtml.cs` (236 lines) plus the freshness banner computed inside `Pages/Mail/Index.cshtml.cs`. Without these reads [[DSK-07-04]] has nothing to bind, and the Phase 5 exit gate — intake keeps arriving while every desktop is closed, and an operator can *see* that it did — cannot be demonstrated. Siblings: [[DSK-07-02]] adds the retry commands these reads mark eligible, [[DSK-07-19]] supplies the provider error vocabulary the failure fields use.

## Source of truth

- Plan row: `docs/desktop/07-integrations/README.md` § 5 — `DSK-07-01`
- Plan context: `docs/desktop/07-integrations/README.md` § 2 Evidence base (Worker function inventory), § 4 Target state (first bullet)
- Endpoint map: `docs/desktop/03-gateway-api-and-data/endpoint-map.md` § `Triage, Unidentified, Operations` (rows `GET /operations`, `POST /operations/external-work/{wid}/retry`) — this ticket adds the intake-status row that map does not yet carry
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 12.1 Microsoft Graph intake, § 13.10 Administration and operations, § 16.2 External provider resilience
- Repository evidence: `src/Pegasus.Web/Pages/Operations/Index.cshtml.cs:18-70` (the page's Core dependencies and `LoadedAtUtc` honesty rule); `src/Pegasus.Core/Operations/RequestOperations.cs:32-70` (`RequestOperationProjection`, `RequestOperationsProjection`), `:72-100` (`GetRequestOperations`, `MaximumItems = 100`); `src/Pegasus.Core/Operations/EmailOperations.cs:19-60` (`EmailOperationProjection`, `CanRetry`), `:62-104` (`GetEmailOperations`, `MaximumItemsPerDirection = 50`); `src/Pegasus.Core/Intake/RetainedMail.cs:360-364` (`MailPollHealth`), `:382` (`IRetainedMailQueries.ListPollHealthAsync`), `:641-711` (`GetRetainedMailFreshness.Evaluate`); `src/Pegasus.Worker/MailboxFunctions.cs:15` (`InboxPollFunction`), `src/Pegasus.Worker/IntakeFunctions.cs:13,30,47,75`, `src/Pegasus.Worker/Functions/ExternalWorkFunctions.cs:9,27`, `src/Pegasus.Worker/host.json` (`maxDequeueCount 5`)
- Binding decisions: L-01 — the gateway is `Pegasus.Web` evolved in place, so these endpoints are route groups beside the Razor Pages, no new deployment unit. ADR-0106 — Graph intake stays central; the desktop never polls and never holds a Graph credential. L-02 — evidence is produced on the local Test/UAT stack, never an Azure test resource.
- Depends on: `DSK-03-02` the `/api/v1` route-group skeleton behind `Features:DesktopGateway` and the problem-details mapping; `DSK-03-03` the staff bearer actor resolution and the `StaffAccessRight` endpoint filter

## Routing

- **Subagent**: `pegasus-gateway-dev` — `.codex/agents/pegasus-gateway-dev.toml`
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `dotnet-webapi` (dotnet/skills `98f84851`, plugin `dotnet-aspnetcore`) → `microsoft-code-reference` (Microsoft Learn plugin) → `run-tests` (dotnet/skills `98f84851`, plugin `dotnet-test`)
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search`, `microsoft_code_sample_search`)
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (call `get_doc_gates <id>` before every move; a move crosses at most one gated boundary)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Orient: read the plan row and the plan sections named under Source of truth, then `docs/desktop/03-gateway-api-and-data/endpoint-map.md` § `Triage, Unidentified, Operations` and its Conventions header. Call `get_doc_gates <this ticket id>`, then `take_ticket`, working in a worktree on branch `task/dsk-07-01-intake-status-endpoints` cut from `origin/dev`.
2. Read `src/Pegasus.Web/Pages/Operations/Index.cshtml.cs` in full and tabulate in the `research` document: which Core use case each handler calls, the `LoadedAtUtc` rule at `:43-46` (a failed load never claims freshness), and the `RequestOperationProjection` fields the page renders. Do the same for the freshness path in `src/Pegasus.Core/Intake/RetainedMail.cs:641-711`.
3. Confirm — do not assume — the three read ports this endpoint composes: `GetRequestOperations` (`src/Pegasus.Core/Operations/RequestOperations.cs:72`), `GetEmailOperations` (`src/Pegasus.Core/Operations/EmailOperations.cs:62`) and `IRetainedMailQueries.ListPollHealthAsync` (`src/Pegasus.Core/Intake/RetainedMail.cs:382`). Record the exact type names in `files`.
4. Add the response DTOs to `src/Pegasus.Contracts` (the project created by [[DSK-02-04]]): `IntakeStatusResponse` with one `MailboxIntakeStatus` per mailbox carrying `mailboxId`, `mailboxAddress`, `lastCompletedAtUtc`, `lastFailureCode`, `dueAtUtc`, `freshness` (`current` | `stale` | `unavailable`), and `ExternalWorkResponse` with `kind`, `caseReference`, `attemptCount`, `failureCode`, `failureReason`, `canRetry`, `lastActivityAtUtc`. Every DTO is a plain record with no EF, ASP.NET or Core types — the architecture test from [[DSK-03-01]] enforces it.
5. Add the endpoints in the `/api/v1` operations route group inside `src/Pegasus.Web`, both `GET`, both behind the `Features:DesktopGateway` gate and the `PerformCasework` right filter from [[DSK-03-03]]. Return `version`-free reads with a weak `ETag` per the endpoint-map conventions, and always populate an `asOfUtc` taken *after* the query returns — never before.
6. Map failure fields through the taxonomy rather than inventing strings: a mailbox with a recorded `lastFailureCode` and a future `dueAtUtc` is `unavailable`, exactly as `GetRetainedMailFreshness.Evaluate` decides it. Do not collapse an `unknown` outcome into success — `docs/current-architecture.md:85-90` makes `terminal` / `transient` / `unknown` distinct, and [[DSK-07-19]] fixes the wire vocabulary.
7. Surface poison visibility: report the count of intake items that have exhausted `maxDequeueCount` (`src/Pegasus.Worker/host.json`) as its own field. A friendly rollup that hides poison counts is a defect against this area's trap list.
8. Write contract tests in `tests/Pegasus.Api.ContractTests` (scaffolded by [[DSK-08-01]]): gate off → 404; unauthenticated → 401; wrong right → 403 with the `urn:pegasus:problem:not-authorized` type; a healthy mailbox → `current`; a mailbox with a failure code and a future due time → `unavailable`; and an assertion that no response field contains a mailbox credential, Graph token, connection string or storage key.
9. Write a LocalDB integration test in `tests/Pegasus.IntegrationTests` seeded with a failed external work item and a failed mailbox poll, following the fixture patterns in `tests/Pegasus.IntegrationTests/OperationsWebTests.cs` and `tests/Pegasus.IntegrationTests/OperationsPersistenceTests.cs`. Expected: `canRetry` is true only when `RequestOperationProjection.CanRetry` / `EmailOperationProjection.CanRetry` are true for the same data.
10. Run `dotnet build ./src/Pegasus.Web/Pegasus.Web.csproj -c Release` and the two test commands under Verification. Confirm the existing `OperationsWebTests` remain green — the Razor page is untouched.
11. Add the two rows to `docs/desktop/03-gateway-api-and-data/endpoint-map.md` § `Triage, Unidentified, Operations` so the map stays the single endpoint list.
12. Run the simplification pass over the branch diff (`AGENTS.md` step 4), record it under a dated `## Simplification pass` heading in the ticket plan document, then open the PR into `dev`.

## Acceptance criteria

- [ ] `GET /api/v1/operations/intake-status` returns one row per approved mailbox with last completed cycle, last failure code, next due time and a freshness state computed by the existing Core policy.
- [ ] `GET /api/v1/operations/external-work` returns retryable external work with attempt count, failure code and `canRetry` matching the Core projection for the same data.
- [ ] Failure information is never collapsed into success; poison counts are reported as their own field.
- [ ] No Worker file is modified and no new Core use case is written — both endpoints compose existing read models.
- [ ] No response body, log line or problem detail carries a Graph, storage or Box credential.
- [ ] Both endpoints 404 with `Features:DesktopGateway` off and 403 for an actor without `PerformCasework`.

## Verification

- [ ] `dotnet test ./tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj --configuration Release` — expected: the gate, authorization, freshness and no-credential facts pass.
- [ ] `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --filter "Category!=Corpus&Category!=Browser"` — expected: the new seeded-failure facts pass and every existing `OperationsWebTests` / `OperationsPersistenceTests` fact stays green.
- [ ] `git diff --stat origin/dev -- src/Pegasus.Worker` — expected: empty output (no Worker change).

## Evidence tier

Tier 5 — Web/API/MCP caller.
Tier 5 obliges evidence that the actual `/api/v1` routes reach Core with authentication, right checks, exception translation and correlation ids observable — a registration or a green build does not satisfy it.

## Documentation changes

- `docs/desktop/03-gateway-api-and-data/endpoint-map.md` — two new rows in § `Triage, Unidentified, Operations`
- `docs/capabilities.md` — a `DSK` row for desktop intake status (canonical owner named)

## Guardrails

- **Azure**: no write. Reads of App Insights or storage for diagnosis are permitted without approval (`docs/runbook.md` § Live-operation approval matrix; mirrored in `docs/desktop/11-azure-disposition/README.md`).
- **Scope boundary**: may touch `src/Pegasus.Web` (the `/api/v1` operations group only), `src/Pegasus.Contracts`, `tests/Pegasus.Api.ContractTests`, `tests/Pegasus.IntegrationTests`. Must not touch `src/Pegasus.Worker`, `src/Pegasus.Infrastructure/Email/`, or any Razor page.
- **Traps**: poison-queue visibility must not disappear behind a friendly status; `unknown` never becomes success; the Worker keeps every Graph credential (ADR-0106) and a step that puts a provider secret in the desktop package or in a response body is a defect; a new table would need a runtime-role `Grant*` migration checked by `scripts/Test-MigrationGrants.ps1` — this ticket must not add one; run the upstream sync before relying on mailbox behaviour, because PLAT-039 and the mail fixes arrive with it (`docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md`).
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.

## Outcome

_Filled at closeout._
