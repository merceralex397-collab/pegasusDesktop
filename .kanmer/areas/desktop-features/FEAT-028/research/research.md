# Research — FEAT-028: the four human-only retry use cases, their real shapes, and where the audit already comes from

## Question

What exactly do the four retry use cases require on the wire, what does each
return or throw, which of them has a Razor caller today, and where does the
audit record that step 9 must assert actually get written?

## Current behaviour

Read at fork `main` `191ddf33` on 2026-08-24. Re-read after the latest upstream
sync ([[FND-023]], plan handle `DSK-01-10`) and record the SHA.

| Command | Razor caller today | Core owner |
| --- | --- | --- |
| Retry external work | `src/Pegasus.Web/Pages/Operations/Index.cshtml.cs:71` `OnPostRetryExternalAsync` | `RetryExternalWork` (`src/Pegasus.Core/Operations/RequestOperations.cs:171`) |
| Retry case custody | `src/Pegasus.Web/Pages/Cases/Custody.cshtml.cs:28` `OnPostRetryCustodyAsync` | `RetryCaseCustody` (`src/Pegasus.Core/Custody/CustodyContracts.cs:413`) behind `IRetryCaseCustody` (`:402`) |
| Retry intake allocation | `src/Pegasus.Web/Pages/Intake/Details.cshtml.cs:111` `OnPostRetryAllocationAsync` | `AllocateIntake.RetryAsync` (`src/Pegasus.Core/Intake/IntakeAllocation.cs:308`) behind `IAllocateIntake` (`:174`, member at `:185`) |
| Retry mailbox processing | **none** | `RetryMailboxProcessing` (`src/Pegasus.Core/Operations/EmailOperations.cs:124`) |

Parity-matrix rows: **`PAR-27`** (Operations, `not inventoried`) carries
`~POST /api/v1/operations/external-work/{id}/retry`; **`PAR-13`**
(`Cases/Custody.cshtml.cs`, `inventoried`) carries `~POST .../custody/retry`;
**`PAR-19`** (`Intake/Details.cshtml.cs`, `inventoried`) carries
`~POST /api/v1/received/{id}/retry-allocation`. No row covers mailbox-processing
retry, because no web surface calls it. The matrix holds `PAR-01`…`PAR-46`
(`grep -c '^| PAR-' …/parity-matrix.md` → 46).

## Findings

### Facts

- **The four Core command records, with their real members** — the DTO field
  lists in the ticket body's step 3 are illustrative and three of the four are
  short. The body's own instruction settles it: "field names copied from the
  Core records, not invented."

  | Core record | `path:line` | Members |
  | --- | --- | --- |
  | `RetryExternalWorkCommand` | `RequestOperations.cs:157-161` | `WorkItemId`, `ExpectedAttemptCount`, `Actor`, `OperationKey` |
  | `RetryCaseCustodyRequest` | `CustodyContracts.cs:288-296` | `CaseId`, `ExpectedCaseVersion`, `Actor`, `OperationKey`, **`Reason`**, `EditLeaseToken`, **`TargetKind`** |
  | `RetryIntakeAllocationRequest` | `IntakeAllocation.cs:161-167` | `ReceiptId`, `ExpectedReceiptVersion`, **`ExpectedCurrentAttemptId`**, `Actor`, `OperationKey`, **`Reason`** |
  | `RetryMailboxProcessingCommand` | `EmailOperations.cs:106-112` | `MailboxId`, `Direction`, `ExpectedFailureCode`, `ExpectedDueAtUtc`, `Actor`, `OperationKey` |

  `Actor` is server-derived and never on the wire. The bolded members are the
  ones the body's illustrative list omits and the DTOs must carry.
  `CustodyTargetKind` has two values, `CaseSource` and `AuditReference`
  (`CustodyContracts.cs:239-243`).
- **`RetryCaseCustodyOutcome` has five members** (`CustodyContracts.cs:297-305`,
  declaration order `Pending`, `Replay`, `Conflict`, `Refused`, `NotFound`) and
  each arrives with a **Core-supplied `Message`** on `RetryCaseCustodyResult`
  (`:306-309`: `Outcome`, `CaseVersion`, `Message`). The decision is taken by
  `CustodyRetryPolicy.Decide` (`:328-370`) over `CustodyRetryDecisionState`
  (`:311-323`), whose XML remark calls it "The sole owner of custody-retry
  replay, conflict, and eligibility decisions."
- **The body names `CustodyRetryDecision.Decide`; the code has
  `CustodyRetryPolicy.Decide`** (`CustodyContracts.cs:328`, with
  `CustodyRetryPolicyAuthority` at `:373` wrapping it). No type named
  `CustodyRetryDecision` exists —
  `grep -rn 'CustodyRetryDecision' --include=*.cs src` returns only
  `CustodyRetryDecisionState`. The body's intent is unambiguous; these documents
  use the real names.
- **Four different refusals share one outcome value.** The exact Core sentences
  (`CustodyContracts.cs:330-369`): `Replay` "The original custody retry request
  is already pending." (`:336-337`); `Conflict` "The custody retry operation key
  was already used for another request." (`:338-339`); `NotFound` "The case was
  not found." (`:343`); `Refused` "No matching custody work exists."
  (`:347-348`); `Conflict` "Another authorized retry already re-armed this
  custody work with a different operation key." (`:353-354`); `Refused` "Only
  failed custody work can be retried." (`:355-356`); `Refused` "Confirmed
  custody cannot be retried." (`:360-361`); `Refused` "The case has no immutable
  Audit reference to store." (`:365-366`); `Pending` "Custody retry queued."
  (`:368-369`). The wire representation must therefore carry the `Message`, not
  only the enum, or an operator cannot tell "already confirmed" from "no Audit
  reference".
- **The Automation-token refusal is already in Core.**
  `RetryCaseCustody.ExecuteAsync` (`CustodyContracts.cs:415`) throws
  `StaffAuthorizationException` when `request.Actor.Kind != ActorKind.Staff`
  (`:420-423`) *before* the right check at `:424`. Step 8's "an Automation-client
  token → rejected on `/api/v1`" is therefore enforced twice — at the
  [[GWY-003]] (plan handle `DSK-03-03`) filter and inside Core — and the test
  proves both.
- **Core's own field bounds.** Custody (`CustodyContracts.cs:434-436`):
  `Reason` ≤ 500, `OperationKey` ≤ 100, `EditLeaseToken` ≤ 200.
  External work (`RequestOperations.cs:186-197`): non-empty `WorkItemId`,
  non-negative `ExpectedAttemptCount` (`:190`), `OperationKey` ≤ 100
  (`:192-197`). Mailbox processing (`EmailOperations.cs:139-152`): a defined
  `Direction`, `MailboxId` / `ExpectedFailureCode` / `OperationKey` ≤ 100 each
  (`:145-147`), and a non-default `ExpectedDueAtUtc` (`:148`).
- **The audit rows already exist inside the persistence adapter, not the page.**
  `EfExternalWorkStore.RetryAsync`
  (`src/Pegasus.Infrastructure/Persistence/EfExternalWorkStore.cs:82`) writes, in
  one serializable transaction: a workflow-history row with `ResultJson`
  (`:204-208`), a `CaseHistory` row with `EventType = "custody_retry_requested"`
  (`:210-221`) and an `ActionHistory` row with
  `EventKind = "custody_retry_requested"`,
  `CorrelationId = request.OperationKey`, `Outcome = "Succeeded"` and
  `PolicyVersion = "custody-recovery-v1"` (`:223-238`). The gateway command
  therefore inherits the audit for free — step 9 asserts the row exists, it does
  not add a writer.
- **`OperationsRetryResult` is `(bool IsReplay)`** (`EmailOperations.cs:114`) and
  is what both `RetryExternalWork` and `RetryMailboxProcessing` return. The
  `{ "isReplay": bool }` body in the ticket's step 4 matches it exactly.
- **The Razor page's exception mapping is the precedent for step 4.**
  `Operations/Index.cshtml.cs` catches `StaffAuthorizationException` → `Forbid`
  (`:96`), `ArgumentException` → "The external work retry request was invalid."
  (`:100-102`) and `InvalidOperationException` → "The external work failure
  changed before retry. Refresh and try again." (`:104-106`). That third case is
  the one step 4 maps to `urn:pegasus:problem:operation-conflict`. The custody
  page's equivalent is `TempData["CaseStatus"] = result.Message` for
  `Pending`/`Replay` (`Cases/Custody.cshtml.cs:50`) and
  `TempData["CaseError"] = result.Message` for the other three (`:55`).
- **Intake allocation throws three named exceptions**:
  `IntakeAllocationOperationConflictException` (`IntakeAllocation.cs:190`, "The
  allocation operation key was already used for different command details."),
  `IntakeAllocationConcurrencyException` (`:193`, "The receipt or allocation
  state changed after it was loaded.") and `PrincipalUnavailableException`
  (`:196`). Replay is decided inside `AllocateIntake.RetryAsync` by comparing a
  command hash (`:320-333`) — a same-key call with *different* details is a
  conflict, not a replay.
- **`RetryMailboxProcessing` has no caller in `src/Pegasus.Web`.**
  `grep -rn 'RetryMailboxProcessing' --include=*.cs src tests` returns the Core
  declaration, the DI registration
  (`src/Pegasus.Infrastructure/DependencyInjection.cs:242`), the EF store
  (`EfOperationsStore.cs:327`, `:344`, `:392`) and one Core test
  (`tests/Pegasus.Core.Tests/Operations/OperationsUseCaseTests.cs:65`). The
  gateway command is its **first shipped caller**, so there is no Razor parity
  baseline for it and its evidence is the contract and integration facts alone.
- **The registrations are shared, so an architecture guard must be about
  callers, not registration.**
  `src/Pegasus.Infrastructure/DependencyInjection.cs:217` registers
  `IRetryCaseCustody`, `:242` `RetryMailboxProcessing`, `:243`
  `RetryExternalWork` — all inside the shared composition both hosts call. A test
  asserting "the Worker does not register custody retry" would fail today and
  would be wrong. `grep -rn 'RetryCaseCustody' src/Pegasus.Worker` returns
  nothing, so the true, provable statement is **no Worker function type takes
  `IRetryCaseCustody` (or the other three) as a constructor dependency**.
- **`WorkerCompositionTests.cs` (259 lines) is the right home for that guard.**
  It already builds the Worker's service provider from
  `CreateWorkerServices(configuration, environment)` and asserts composition
  facts (`:19-45`). `DependencyDirectionTests.cs:383` separately asserts
  `IRetryCaseCustody` and `RetryCaseCustody` share an assembly, so the file pair
  to extend is known.
- **Existing test harnesses to reuse.**
  `tests/Pegasus.IntegrationTests/CaseCustodyWebTests.cs` (276) already
  substitutes `IRetryCaseCustody` (`:22`, `:160-183`) and records
  `RetryCaseCustodyRequest`s;
  `tests/Pegasus.IntegrationTests/CustodyOutboxIntegrationTests.cs` exercises the
  real store (`:834`, `:980`);
  `tests/Pegasus.Core.Tests/Custody/CustodyRecoveryPolicyTests.cs` covers the
  policy; `tests/Pegasus.IntegrationTests/OperationsWebTests.cs` (363) covers the
  external-work page.
- **The rule this ticket exists to protect, in the repository's own words.**
  `docs/current-architecture.md:571`: "For Box custody, an initial failed
  operation remains terminal and visible for authorised staff to retry; no
  automatic business retry is permitted."
  `docs/frd/frd-05-documents-extraction-and-custody.md:27`: "A Box failure after
  Case/PO allocation retains the Case as `Not ready` with explicit failure and
  staff-initiated retry/recovery evidence… no background or automatic business
  retry is permitted."
- **The problem-type catalogue is fixed.**
  `docs/desktop/03-gateway-api-and-data/README.md:167` — the thirteen
  `urn:pegasus:problem:<slug>` values include `not-authorized`,
  `version-conflict`, `operation-conflict`, `lease-conflict`, `lease-expired`,
  `not-found` and `validation`. All five custody outcomes and every exception in
  this ticket map into that list; nothing new is needed.
- **The projects this ticket writes into do not exist yet.** No
  `src/Pegasus.Contracts`, no `tests/Pegasus.Api.ContractTests`, no `openapi/`,
  no `eng/`.

### Assumptions

- **A-07-02-1 — the desktop can obtain a case edit lease before calling custody
  retry.** `RetryCaseCustodyRequest` requires an `EditLeaseToken`
  (`CustodyContracts.cs:294`) and the Razor page receives one from the case
  workspace. Confirmed by: the contract test acquiring a lease through
  [[GWY-008]] (plan handle `DSK-03-08`)'s lease endpoints first. Breaks if:
  those endpoints have not landed — then this command cannot be tested end to
  end and the ticket waits rather than inventing a lease-free path.
- **A-07-02-2 — `ExpectedCurrentAttemptId` is available to the desktop from the
  received-item read.** It is required by `RetryIntakeAllocationRequest`
  (`IntakeAllocation.cs:164`). Confirmed by: checking the received-item DTO from
  [[GWY-010]] (plan handle `DSK-03-10`) carries the current attempt id. Breaks
  if: it does not — the retry cannot be composed, and the missing field is
  raised on [[GWY-010]], not added here.
- **A-07-02-3 — `RetryMailboxProcessing`'s `ExpectedFailureCode` and
  `ExpectedDueAtUtc` are exactly the `FailureCode` and `RetryExpectedDueAtUtc`
  that `EmailOperationProjection` publishes** (`EmailOperations.cs:31`, `:33`,
  with `CanRetry` derived from `RetryMailboxId` and `RetryExpectedDueAtUtc` at
  `:45`). Confirmed by: an integration fact that a row marked `canRetry` can be
  retried with the values the same read returned. Breaks if: they diverge — the
  desktop would offer a retry that always conflicts.
- **A-07-02-4 — no Worker function will need a retry use case.** Confirmed by
  the guard at plan step 10. Breaks if: a future ticket legitimately needs an
  automatic re-arm — that is an ADR-level change against
  `docs/current-architecture.md:571` and FRD-05 `:27`, not a test relaxation.
- **A-07-02-5 — [[FEAT-027]] (plan handle `DSK-07-01`) publishes `canRetry` and
  the expected attempt count before this ticket is implemented.** Stated in the
  body's "Depends on". Breaks if: it has not landed — the commands can still be
  built and tested, but the desktop has nothing to enable them from, so
  [[FEAT-030]] (plan handle `DSK-07-04`) stalls rather than this ticket.

## Execution placement

The six-question test from `docs/desktop/00-governance-and-workflow/README.md`
§ 3, answered. This ticket places **four command** responsibilities.

| Question | Answer | Evidence, and where a "yes" lands |
| --- | --- | --- |
| Shared authority — must several users see and update the same state? | **yes** | Two staff can race the same retry. `CustodyRetryPolicy.Decide` has a dedicated branch for it — "Another authorized retry already re-armed this custody work with a different operation key" (`CustodyContracts.cs:353-354`) — and `RetryExternalWorkCommand.ExpectedAttemptCount` (`RequestOperations.cs:159`) exists for the same reason. **Lands in the gateway** (L-01), where the Core use case already decides it. |
| Unattended execution — must it run with every desktop closed? | **no**, and deliberately so | These are the four commands that must **not** run unattended. `docs/current-architecture.md:571` and FRD-05 `:27` forbid an automatic business retry. The desktop initiates; nothing schedules. This is the one "no" on this ticket that is load-bearing rather than incidental, and step 10's guard is what keeps it true. |
| Protected credentials — long-lived secret that must not sit on workstations? | **yes** | Executing a retry re-arms work that calls Box, Graph and DVLA/DVSA with credentials held as Key Vault references and Container App secrets (`infra/modules/platform.bicep:382-398`, `:555-563`). **Lands behind the gateway and Worker** (ADR-0107): the desktop sends an intent, the gateway authorises, the Worker executes with the credential. No provider token or raw provider payload reaches a response. |
| Public callback — must an external service call a stable public endpoint? | **no** | Nothing external calls a retry. The re-armed work is drained by the existing queue triggers (`ExternalWorkFunction`, `src/Pegasus.Worker/Functions/ExternalWorkFunctions.cs:7-9`). |
| Central enforcement — revocation, permissions, audit, invariant independent of the client? | **yes** | Three enforcements the client cannot be trusted with: the `PerformCasework` right (`CustodyContracts.cs:424`, `RequestOperations.cs:185`, `EmailOperations.cs:138`), the `ActorKind.Staff` refusal that rejects an Automation token (`CustodyContracts.cs:420-423`), and the audit rows written inside the transaction (`EfExternalWorkStore.cs:210-238`). **Lands in the gateway.** |
| Measured operational advantage — measured evidence central is materially better? | **no** | No measurement supports executing a retry anywhere else. The relevant measured facts (PLAT-041's Box call count, PLAT-034's blind hours) argue about where bytes and telemetry go, not about where a retry decision is taken. |

**Conclusion.** Three "yes" answers, all landing in the gateway (L-01) with the
credential behind it (ADR-0107) and the work drained by the existing Worker
(ADR-0106). The two "no" answers are the point of the ticket: unattended
execution is **forbidden** here, not merely unnecessary. Confirmation dialogs,
reason capture and the enable/disable state belong to the desktop
([[FEAT-030]], plan handle `DSK-07-04`). **No new Azure resource and no Azure
write.**

## Implications

- **Four DTOs, four field lists taken from Core.** `RetryCustodyRequest` needs
  `reason` and `targetKind`; `RetryIntakeAllocationRequest` needs
  `expectedCurrentAttemptId` and `reason`. Building them from the body's
  illustrative parentheses would produce commands Core rejects at the first
  argument check.
- **The custody wire shape must carry the `Message`, not only the outcome.**
  Four different refusals share `Refused`. The acceptance criterion "every one of
  the five values has a distinct, documented wire representation" is met by
  outcome **plus** the Core-supplied reason text, which the Razor page already
  surfaces (`Cases/Custody.cshtml.cs:50`, `:55`).
- **Idempotency is proved at the boundary, not implemented.** Each Core use case
  already decides replay — custody by `OperationExists && OperationMatches`
  (`CustodyContracts.cs:333-339`), allocation by command hash
  (`IntakeAllocation.cs:320-333`), external work and mailbox processing by the
  store returning `OperationsRetryResult(IsReplay: true)`. The tests assert the
  second response equals the first; no replay cache is written in Web.
- **The audit assertion is a read, not a write.** The rows come from
  `EfExternalWorkStore.RetryAsync` (`:210-238`). If a command produces no
  `ActionHistory` row, that is a Core/Infrastructure gap to raise, not something
  to paper over with a Web-side writer.
- **The architecture guard must be phrased about callers.** Registration is
  shared (`DependencyInjection.cs:217`, `:242`, `:243`), so "not registered in
  the Worker" is false. "No Worker function type takes a retry use case as a
  constructor dependency" is true today and is what `WorkerCompositionTests.cs`
  can assert.
- **Mailbox-processing retry is new surface.** It has no Razor precedent, so its
  operator sentences, its problem mapping and its enablement rule are decided
  here for the first time — and `EmailOperationProjection.CanRetry` (`:45`) is
  the only eligibility source.

## Open questions

None that block. Three points that could look like questions have named owners:

- Whether the received-item read publishes `expectedCurrentAttemptId` is
  [[GWY-010]] (plan handle `DSK-03-10`)'s contract. If it does not, stop and
  raise it there — a scope boundary, not a question.
- Whether the case-lease endpoints exist for the custody retry's
  `editLeaseToken` is [[GWY-008]] (plan handle `DSK-03-08`)'s. Same treatment.
- The `terminal` / `transient` / `unknown` wire vocabulary for the failure codes
  these commands echo is [[FEAT-045]] (plan handle `DSK-07-19`)'s. This ticket
  carries the Core strings verbatim.
