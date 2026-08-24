---
id: FEAT-028
type: ticket
title: >-
  DSK-07-02 · Human retry commands through the gateway with operation keys and
  audit
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
  - docs/frd/frd-05-documents-extraction-and-custody.md
  - docs/frd/frd-02-intake-and-source-identity.md
docs_todo: true
archived: false
created: '2026-08-24T08:18:48.620Z'
updated: '2026-08-24T08:18:48.620Z'
---

## What

Expose the three existing **human-only** retry use cases as explicit `/api/v1` commands — retry external work, retry case custody, retry intake allocation, plus retry mailbox processing — each carrying an `operationKey`, each mapping one-to-one to the Core use case the Razor handler calls today, each audited, and each refused for an actor without the right.

## Why

Proposal § 12.1 requires "parsing failures enter a visible retry/failure table" and § 13.10 lists failed-work and retry screens as parity. `docs/current-architecture.md:571` records the rule that matters here: for Box custody an initial failed operation stays terminal and visible for authorised staff to retry — **no automatic business retry is permitted**. If the desktop cannot reach these commands, an operator has to keep the web app open to clear a failure, which breaks proposal § 27 item 2. Siblings: [[DSK-07-01]] supplies the reads that mark a row retryable, [[DSK-07-04]] binds the desktop surface.

## Source of truth

- Plan row: `docs/desktop/07-integrations/README.md` § 5 — `DSK-07-02`
- Plan context: `docs/desktop/07-integrations/README.md` § 7 Risks and traps (row "Custody retry automated 'for convenience'")
- Endpoint map: `docs/desktop/03-gateway-api-and-data/endpoint-map.md` § `Triage, Unidentified, Operations` (`POST /operations/external-work/{wid}/retry`), § `Cases` (`POST /cases/{id}/custody/retry`), § `Intake (received items), uploads, image intake` (`POST /received/{id}/retry-allocation`)
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 12.1 Microsoft Graph intake, § 13.10 Administration and operations, § 16.1 Operation model
- Repository evidence: `src/Pegasus.Web/Pages/Operations/Index.cshtml.cs:72-110` (`OnPostRetryExternalAsync`, its `expectedAttemptCount` + `operationKey` contract and its replay message); `src/Pegasus.Web/Pages/Cases/Custody.cshtml.cs:18,28` (`IRetryCaseCustody`, `OnPostRetryCustodyAsync`); `src/Pegasus.Core/Operations/RequestOperations.cs:157-201` (`RetryExternalWorkCommand`, `RetryExternalWork`); `src/Pegasus.Core/Custody/CustodyContracts.cs:288-410` (`RetryCaseCustodyRequest`, `RetryCaseCustodyOutcome`, `CustodyRetryDecision.Decide`, `IRetryCaseCustody`); `src/Pegasus.Core/Intake/IntakeAllocation.cs:161-320` (`RetryIntakeAllocationRequest`); `src/Pegasus.Core/Operations/EmailOperations.cs:106-168` (`RetryMailboxProcessingCommand`, `RetryMailboxProcessing`); `docs/current-architecture.md:526,571`
- Binding decisions: L-01 — commands are `/api/v1` route groups inside `Pegasus.Web`. ADR-0106 — the desktop never retries by calling Graph itself; it asks the gateway, which asks the same Core use case. L-02 — replay and authorization evidence is produced on the local stack.
- Depends on: `DSK-07-01` the reads that publish `canRetry` and the expected attempt count; `DSK-03-02` route-group skeleton; `DSK-03-03` right filter

## Routing

- **Subagent**: `pegasus-gateway-dev` — `.codex/agents/pegasus-gateway-dev.toml`
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `dotnet-webapi` (dotnet/skills `98f84851`) → `code-testing-agent` (dotnet/skills `98f84851`, plugin `dotnet-test`) → `run-tests`
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search`)
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (call `get_doc_gates <id>` before every move; a move crosses at most one gated boundary)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Orient: read the plan row, this area's § 7 trap table, and the endpoint-map Conventions header (`operationKey` on every command, `expectedVersion` on case-scoped commands). Call `get_doc_gates <this ticket id>`, then `take_ticket` on branch `task/dsk-07-02-retry-commands`.
2. Read the four Core commands named under Repository evidence and write into `files` a table of: command record, required fields, the exception each throws on conflict, and the outcome enum. `RetryCaseCustodyOutcome` has five members (`Replay`, `Conflict`, `NotFound`, `Refused`, `Pending`) — every one of them needs a wire representation.
3. Add request/response records to `src/Pegasus.Contracts`: `RetryExternalWorkRequest(long expectedAttemptCount, string operationKey)`, `RetryCustodyRequest(long expectedVersion, string editLeaseToken, string operationKey)`, `RetryIntakeAllocationRequest(long expectedVersion, string operationKey)`, `RetryMailboxProcessingRequest(string mailboxId, string direction, string expectedFailureCode, DateTimeOffset expectedDueAtUtc, string operationKey)` — field names copied from the Core records, not invented.
4. Implement `POST /api/v1/operations/external-work/{workItemId}/retry` calling `RetryExternalWork.ExecuteAsync`, returning `200` with `{ "isReplay": bool }`. Translate `InvalidOperationException` (the failure changed before retry) to `urn:pegasus:problem:operation-conflict` and `StaffAuthorizationException` to `urn:pegasus:problem:not-authorized`, using the mapping ported in [[DSK-03-02]].
5. Implement `POST /api/v1/cases/{caseId}/custody/retry` calling `IRetryCaseCustody.ExecuteAsync`. Map `Conflict` → `409` `version-conflict` carrying `currentVersion`, `Refused` → `409` `operation-conflict` with the Core-supplied reason text, `NotFound` → `404`, `Replay` and `Pending` → `200` with the outcome named. The desktop must be able to tell those five apart.
6. Implement `POST /api/v1/received/{receiptId}/retry-allocation` and `POST /api/v1/operations/mailbox-processing/retry` against `RetryIntakeAllocation` and `RetryMailboxProcessing` respectively, with the same key and conflict discipline.
7. Prove idempotency at the boundary, not in new code: a second call with the same `operationKey` must return the same result the first returned, because each Core use case already decides replay. Add one test per command asserting the replayed response body equals the first.
8. Add authorization-failure tests per command in `tests/Pegasus.Api.ContractTests`: unauthenticated → 401; actor without `PerformCasework` → 403 `not-authorized`; an Automation-client token → rejected on `/api/v1` (the rule from [[DSK-03-03]]).
9. Add an audit assertion: after each successful command an action-history / security-event row exists naming the staff actor, mirroring what the Razor handler produces today. Follow the assertion style in `tests/Pegasus.IntegrationTests/OperationsWebTests.cs` and `tests/Pegasus.IntegrationTests/CaseCustodyWebTests.cs`.
10. Add an architecture-level guard: no scheduled, timer or queue caller may invoke the custody retry command. Assert it as a test in `tests/Pegasus.ArchitectureTests` or as an explicit fact in the contract tests, and record the rule (`docs/current-architecture.md:571`) beside it.
11. Add the new rows to `docs/desktop/03-gateway-api-and-data/endpoint-map.md` in the sections they belong to.
12. Run the simplification pass over the branch diff, record it under a dated `## Simplification pass` heading in the plan document, then open the PR into `dev`.

## Acceptance criteria

- [ ] Each retry command maps one-to-one to the existing Core use case; no second business implementation is added.
- [ ] Replay with the same `operationKey` returns the same result and performs no second effect.
- [ ] Every one of the five `RetryCaseCustodyOutcome` values has a distinct, documented wire representation.
- [ ] Each command is refused with `urn:pegasus:problem:not-authorized` for an actor without the right, and refused for an Automation token.
- [ ] Each successful command writes an audit record naming the staff actor.
- [ ] No automatic or scheduled caller can invoke custody retry — a test proves it.

## Verification

- [ ] `dotnet test ./tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj --configuration Release` — expected: success, replay, authorization and outcome-mapping facts pass for all four commands.
- [ ] `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --filter "Category!=Corpus&Category!=Browser"` — expected: the audit-row facts pass and the existing custody and operations tests stay green.
- [ ] `dotnet test ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj --configuration Release` — expected: the no-automatic-custody-retry fact passes.

## Evidence tier

Tier 5 — Web/API/MCP caller.
Tier 5 obliges observable route-level evidence of authentication, validation, idempotency, exception translation and the action-history actor for every command added here.

## Documentation changes

- `docs/desktop/03-gateway-api-and-data/endpoint-map.md` — retry command rows
- `docs/capabilities.md` — `DSK` row for desktop-initiated retries

## Guardrails

- **Azure**: no write.
- **Scope boundary**: may touch `src/Pegasus.Web` (`/api/v1` groups), `src/Pegasus.Contracts` and the test projects. Must not touch `src/Pegasus.Core` use-case bodies, `src/Pegasus.Infrastructure/Custody/` or `src/Pegasus.Worker` — a retry that needs new Core behaviour is a different ticket.
- **Traps**: custody retry is human-only (`docs/current-architecture.md:571`) — automating it "for convenience" is a defect; provider credentials stay behind the gateway (ADR-0107) and none of these commands may return provider tokens or raw provider payloads; a friendly rollup that hides which failure was retried loses poison visibility; no new table (and therefore no `Grant*` migration) is needed — if one appears, stop and raise it.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.

## Outcome

_Filled at closeout._
