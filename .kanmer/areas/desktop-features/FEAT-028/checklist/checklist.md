# Checklist — FEAT-028

One box per plan step, in plan order. The last box produces `proof`.

- [ ] Read the plan row `DSK-07-02` (`docs/desktop/07-integrations/README.md` § 5), that plan's § 7 trap table, and `endpoint-map.md:11-27`, `:46-80`, `:81-95`, `:108-116`; call `get_doc_gates FEAT-028`; `take_ticket` on branch `task/dsk-07-02-retry-commands` from a worktree cut off `origin/dev`
- [ ] Re-read the four Core command records after the latest upstream sync ([[FND-023]], plan handle `DSK-01-10`), record the SHA in `research`, and confirm the `files` command table still holds — including that the type is `CustodyRetryPolicy`, not the body's `CustodyRetryDecision`
- [ ] Add the four request records to `src/Pegasus.Contracts` with field names and types copied from Core — custody's seven members including `reason` and `targetKind`, allocation's `expectedCurrentAttemptId` and `reason`, and `expectedAttemptCount` as `int`
- [ ] Add the three response records to `src/Pegasus.Contracts`: `OperationsRetryResponse(isReplay)`, `CustodyRetryResponse(outcome, caseVersion, message)`, `IntakeAllocationRetryResponse(status, isReplay, isSuppressed, version, caseReference, safeReason)` — no `ActionActor` on any DTO
- [ ] Implement `POST /api/v1/operations/external-work/{workItemId}/retry` over `RetryExternalWork.ExecuteAsync`, returning `{ "isReplay": bool }`, with `StaffAuthorizationException` → `not-authorized`, `Argument*Exception` → `validation`, `InvalidOperationException` → `operation-conflict`
- [ ] Implement `POST /api/v1/cases/{caseId}/custody/retry` over `IRetryCaseCustody.ExecuteAsync` with all seven members, mapping `Conflict` → 409 `version-conflict` (nullable `currentVersion`), `Refused` → 409 `operation-conflict`, `NotFound` → 404, `Replay`/`Pending` → 200, every response carrying the Core-supplied `message`
- [ ] Implement `POST /api/v1/received/{receiptId}/retry-allocation` over `IAllocateIntake.RetryAsync`, returning `state`, `isReplay` **and** `isSuppressed`, with the three named allocation exceptions mapped to `operation-conflict`, `version-conflict` and `validation` respectively
- [ ] Implement `POST /api/v1/operations/mailbox-processing/retry` over `RetryMailboxProcessing.ExecuteAsync`, parsing `direction` into `EmailOperationDirection` (unparseable → `validation`, never a default) and mapping `InvalidOperationException` → `operation-conflict` with the store's distinct sentence in `detail`
- [ ] Register all four endpoints inside the existing `/api/v1` groups from [[GWY-002]] (plan handle `DSK-03-02`), [[GWY-008]] (plan handle `DSK-03-08`) and [[GWY-010]] (plan handle `DSK-03-10`) behind `Features:DesktopGateway` and the `PerformCasework` filter from [[GWY-003]] (plan handle `DSK-03-03`) — no second group, and record which registration landed first
- [ ] Add one replay test per command asserting the second same-key response equals the first (`isReplay` false then true; custody `Pending` then `Replay`), and a negative allocation fact that a same-key call with different details surfaces `operation-conflict`
- [ ] Add per-command authorization tests in `tests/Pegasus.Api.ContractTests`: 401 unauthenticated, 403 `not-authorized` without `PerformCasework`, Automation-token refused, and the gate-off 404 for all four routes — with `Features:DesktopGateway` enabled explicitly in the positive tests
- [ ] Add the custody double-refusal fact: Core's `ActorKind.Staff` check (`CustodyContracts.cs:420-423`) refuses an Automation token even with the endpoint filter bypassed
- [ ] Add the LocalDB audit facts: custody's `ActionHistory` (`EventKind = "custody_retry_requested"`, actor, `CorrelationId` = `operationKey`) and `CaseHistory` rows, and allocation's `IntakeAllocationAttempts` row carrying actor, key, hash and reason
- [ ] Add the durable-state-transition facts for external work and mailbox processing, and raise the missing `ActionHistory` rows for those two commands as a separate Core/Infrastructure ticket — adding no Web-side audit writer
- [ ] Add the architecture guard to `tests/Pegasus.ArchitectureTests/WorkerCompositionTests.cs` asserting no `Pegasus.Worker` type takes a retry use case as a constructor dependency, with `docs/current-architecture.md:571` and FRD-05 `:27` recorded beside it
- [ ] Amend the three existing `endpoint-map.md` rows with real concurrency tokens and returns, and add the new row for `POST /operations/mailbox-processing/retry`
- [ ] Review every response shape against ADR-0107 — no provider token, no raw provider payload, no connection string — and record the check
- [ ] Confirm the `DSK` family exists in `docs/capabilities.md` ([[FND-011]], plan handle `DSK-00-11`) and add the desktop-initiated-retry row
- [ ] Regenerate `openapi/pegasus-v1.json` and the Kiota client via `eng/api/Generate-ApiClient.ps1` and commit the result in this PR
- [ ] Run the simplification pass over this branch's own diff and record it under a dated `## Simplification pass` heading in the `plan` document
- [ ] Run the verification suite and capture its output as `proof`: `dotnet test ./tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj --configuration Release`, `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --filter "Category!=Corpus&Category!=Browser"`, `dotnet test ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj --configuration Release`, and `git diff --stat origin/dev -- src/Pegasus.Core src/Pegasus.Infrastructure src/Pegasus.Worker` (expected: empty)

## Progress notes

(append with `set_ticket_doc(doc: "checklist", append: true)`)
