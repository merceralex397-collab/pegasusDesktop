# Files — FEAT-028

Surveyed 2026-08-24 against fork `main` `191ddf33`. Paths marked *(created by …)*
do not exist today: `ls src` returns only `Pegasus.Core`, `Pegasus.Infrastructure`,
`Pegasus.Web`, `Pegasus.Worker`; `ls tests` only `Pegasus.ArchitectureTests`,
`Pegasus.Core.Tests`, `Pegasus.IntegrationTests`; there is no `openapi/` and no
`eng/`.

## Where the change lands

| Path | Why |
|---|---|
| `src/Pegasus.Contracts/` *(created by [[FND-029]], plan handle `DSK-02-04`; conventions by [[GWY-001]], plan handle `DSK-03-01`)* | Four request records and three response records. Field names and **types** copied from the Core commands, not from the ticket body's illustrative parentheses: `WorkItemId` is a `Guid` and `ExpectedAttemptCount` an `int` (`RequestOperations.cs:157-161`), not the `long` the body's step 3 sketches. Plain records, no EF/ASP.NET/Core types — the architecture test from [[GWY-001]] enforces it. |
| `src/Pegasus.Web/` — the `/api/v1` **operations**, **cases** and **received** route groups | Four `POST` endpoints registered inside the groups from [[GWY-002]] (plan handle `DSK-03-02`), behind `Features:DesktopGateway` and the `PerformCasework` filter from [[GWY-003]] (plan handle `DSK-03-03`). Each is a thin argument-mapper onto one existing Core use case plus an exception→problem-type translation. The cases group is shared with [[GWY-008]] (plan handle `DSK-03-08`) and the received group with [[GWY-010]] (plan handle `DSK-03-10`) — extend those registrations, never create a second group. |
| `tests/Pegasus.Api.ContractTests/` *(created by [[TEST-001]], plan handle `DSK-08-01`; template from [[TEST-002]], plan handle `DSK-08-02`)* | Per command: authorised success, replay-equals-first, unauthenticated 401, wrong-right 403 `not-authorized`, Automation-token refusal, and the outcome/exception→problem mapping. Custody alone needs five outcome facts plus the `Message` passthrough. |
| `tests/Pegasus.IntegrationTests/` | New LocalDB facts for the audit trail each command actually leaves, beside `CaseCustodyWebTests.cs` (276), `CustodyOutboxIntegrationTests.cs` (1,796) and `OperationsWebTests.cs` (363), which stay green. |
| `tests/Pegasus.ArchitectureTests/WorkerCompositionTests.cs` (259) | The step-10 guard. Phrased about **constructor dependencies of Worker function types**, not about registration — see the Context row below for why the obvious phrasing is false. |
| `docs/desktop/03-gateway-api-and-data/endpoint-map.md` | Rows in three sections. § `Triage, Unidentified, Operations` (`:108-116`) already carries `POST /operations/external-work/{wid}/retry`; § `Cases` (`:46-80`) and § `Intake (received items), uploads, image intake` (`:81-95`) carry the custody and allocation retries. Mailbox-processing retry has **no** row today and is the one genuinely new route. |
| `docs/capabilities.md` (392 lines) | A `DSK` row for desktop-initiated retries. The file uses `FAMILY-NN` two-digit ids and **contains no `DSK` family today** (`grep -n 'DSK' docs/capabilities.md` → no matches); [[FND-011]] (plan handle `DSK-00-11`) creates it. Confirm before adding a row. |

## Context files

| Path | What it tells the implementer |
|---|---|
| `src/Pegasus.Core/Custody/CustodyContracts.cs:288-296` | `RetryCaseCustodyRequest(CaseId, ExpectedCaseVersion, Actor, OperationKey, Reason, EditLeaseToken, TargetKind)` — **seven** members. The body's step-3 sketch omits `Reason` and `TargetKind`; Core rejects a request without them at `:433` and `:429`. `Actor` is server-derived and never on the wire. |
| `src/Pegasus.Core/Custody/CustodyContracts.cs:239-243` | `CustodyTargetKind` has exactly two values, `CaseSource` and `AuditReference`. The DTO carries it as a string and the endpoint parses it, so an unknown value is a `validation` problem rather than a silent default. |
| `src/Pegasus.Core/Custody/CustodyContracts.cs:297-309` | `RetryCaseCustodyOutcome` (`Pending`, `Replay`, `Conflict`, `Refused`, `NotFound`) and `RetryCaseCustodyResult(Outcome, CaseVersion, Message)`. `CaseVersion` is **nullable** — a `NotFound` carries none — and `Message` is Core-supplied prose. |
| `src/Pegasus.Core/Custody/CustodyContracts.cs:325-370` | `CustodyRetryPolicy.Decide` with its own remark: "The sole owner of custody-retry replay, conflict, and eligibility decisions." **Four different refusals collapse into `Refused`** — "No matching custody work exists" (`:347-348`), "Only failed custody work can be retried" (`:355-356`), "Confirmed custody cannot be retried" (`:360-361`), "The case has no immutable Audit reference to store" (`:365-366`). This is why the wire shape must carry `Message`, not only the enum. |
| `src/Pegasus.Core/Custody/CustodyContracts.cs:415-436` | `RetryCaseCustody.ExecuteAsync`. `request.Actor.Kind != ActorKind.Staff` throws `StaffAuthorizationException` at `:420-423` **before** the right check at `:424` — so the Automation-token refusal is already enforced in Core and the [[GWY-003]] filter is defence in depth. Field bounds live at `:433-435`: `Reason` ≤ 500, `OperationKey` ≤ 100, `EditLeaseToken` ≤ 200. |
| `src/Pegasus.Core/Operations/RequestOperations.cs:157-200` | `RetryExternalWorkCommand(Guid WorkItemId, int ExpectedAttemptCount, ActionActor Actor, string OperationKey)` and `RetryExternalWork.ExecuteAsync`: `PerformCasework` at `:185`, empty-Guid refusal at `:186-189`, negative attempt count at `:190`, `OperationKey` ≤ 100 at `:192-197`. Note there is **no** `Reason` here — do not add one. |
| `src/Pegasus.Core/Operations/EmailOperations.cs:106-155` | `RetryMailboxProcessingCommand(MailboxId, Direction, ExpectedFailureCode, ExpectedDueAtUtc, Actor, OperationKey)`, `OperationsRetryResult(bool IsReplay)` at `:114`, and the validation at `:139-152` — a defined `EmailOperationDirection`, three ≤ 100 strings, and a non-default `ExpectedDueAtUtc`. `{ "isReplay": bool }` in the body's step 4 matches this exactly. |
| `src/Pegasus.Core/Intake/IntakeAllocation.cs:161-172` | `RetryIntakeAllocationRequest(ReceiptId, ExpectedReceiptVersion, ExpectedCurrentAttemptId, Actor, OperationKey, Reason)` — **six** members; the body's sketch omits `ExpectedCurrentAttemptId` and `Reason`. It returns `IntakeAllocationResult(State, IsReplay, IsSuppressed)`, **three** fields, not `{ isReplay }`. `IsSuppressed` is set when the current attempt already `Succeeded` (`:334-338`) and is the difference between "your retry ran" and "it was already done"; dropping it loses that distinction on the wire. |
| `src/Pegasus.Core/Intake/IntakeAllocation.cs:83-98` | `IntakeAllocationState` with its derived `CanRetry` (`:95-98`): only `FailedRecoverable` with a `RetryAfterCorrection` or `ReloadThenRetry` disposition. The eligibility source the desktop enables the button from; the endpoint copies it and computes nothing. |
| `src/Pegasus.Core/Intake/IntakeAllocation.cs:189-197` | The three named exceptions — `IntakeAllocationOperationConflictException` ("The allocation operation key was already used for different command details."), `IntakeAllocationConcurrencyException` ("The receipt or allocation state changed after it was loaded.") and `PrincipalUnavailableException`. Three distinct problem mappings, not one 409. |
| `src/Pegasus.Core/Intake/IntakeAllocation.cs:308-340`, `:546-558` | Replay is decided by a **command hash** (`:320-333`): a same-key call with different details throws a conflict rather than replaying. `ValidateReasonAndOperation` bounds `Reason` ≤ 500 and `OperationKey` ≤ 100. |
| `src/Pegasus.Web/Pages/Operations/Index.cshtml.cs:71-110` | The precedent for step 4's mapping: `StaffAuthorizationException` → `Forbid` (`:96`), `ArgumentException` → "The external work retry request was invalid." (`:100-102`), `InvalidOperationException` → "The external work failure changed before retry. Refresh and try again." (`:104-106`). That third case is `operation-conflict`. |
| `src/Pegasus.Web/Pages/Cases/Custody.cshtml.cs:28-70` | `OnPostRetryCustodyAsync` — the exact parameter set the endpoint must accept (`id`, `expectedVersion`, `operationKey`, `reason`, `editLeaseToken`, `targetKind`) and the outcome split: `Pending`/`Replay` clear the lease and set `TempData["CaseStatus"] = result.Message`; the other three preserve the lease and set `TempData["CaseError"] = result.Message`. The lease is **preserved on failure** — the desktop must do the same or the operator loses the lease on a refusal. |
| `src/Pegasus.Web/Pages/Intake/Details.cshtml.cs:111-145` | `OnPostRetryAllocationAsync` — the parameter set (`id`, `expectedVersion`, `expectedAttemptId`, `operationKey`, `reason`) and the success rule: only `Succeeded` **with** a `CaseId` is a success; anything else surfaces `result.State.SafeReason`. `SafeReason` is the operator-safe text; never surface the raw failure. |
| `src/Pegasus.Infrastructure/Persistence/EfExternalWorkStore.cs:82-238` | **The only one of the four that writes audit.** `RetryAsync` opens one serializable transaction and adds a workflow-history row with `ResultJson` (`:194-208`), a `CaseHistory` row with `EventType = "custody_retry_requested"` (`:210-221`) and an `ActionHistory` row with `EventKind = "custody_retry_requested"`, `CorrelationId = request.OperationKey`, `Outcome = "Succeeded"`, `PolicyVersion = "custody-recovery-v1"` (`:223-238`). Step 9's assertion is a **read** of these rows. |
| `src/Pegasus.Infrastructure/Persistence/EfOperationsStore.cs:326-341`, `:343-409`, `:447-486` | **Measured, and it changes step 9.** `IMailboxProcessingRetryStore.RetryAsync` and `IExternalWorkRetryStore.RetryAsync` are single `ExecuteUpdateAsync` state transitions that write **no** `ActionHistory` and **no** `CaseHistory` row. They also decide replay themselves: a matching update → `IsReplay: false`; a cleared failure code or a higher attempt count → `IsReplay: true`; anything else → `InvalidOperationException` with a distinct sentence ("Mailbox processing is already leased.", "The mailbox processing failure changed before retry.", "The external work failure is unavailable.", "The external work failure changed before retry."). |
| `src/Pegasus.Infrastructure/Persistence/EfIntakeAllocationStore.cs` | `grep -n 'ActionHistory\|CaseHistory'` returns nothing: the allocation retry's durable record is the `IntakeAllocationAttempts` row itself, carrying actor, operation key, command hash and reason (`:31`, `:211`, `:268`). That row — not an `ActionHistory` row — is what a step-9 assertion can honestly read for this command. |
| `src/Pegasus.Infrastructure/DependencyInjection.cs:217`, `:236-243` | `IRetryCaseCustody` (`:217`), `IMailboxProcessingRetryStore` (`:236`), `IExternalWorkRetryStore` (`:238`), `RetryMailboxProcessing` (`:242`), `RetryExternalWork` (`:243`) are all registered in the **shared** composition both hosts call. A guard asserting "the Worker does not register custody retry" would fail today and would be wrong. |
| `tests/Pegasus.ArchitectureTests/WorkerCompositionTests.cs:19-45` | The harness for the step-10 guard: it builds the Worker's provider from `CreateWorkerServices(configuration, environment)` and asserts composition facts by resolving types. The provable statement is about **constructor dependencies of Worker function types** (`grep -rn 'RetryCaseCustody' src/Pegasus.Worker` → nothing), not about registration. |
| `tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs:383` | Already asserts `IRetryCaseCustody` and `RetryCaseCustody` share an assembly. The file pair to extend is known; do not start a third architecture-test file. |
| `tests/Pegasus.IntegrationTests/CaseCustodyWebTests.cs:22`, `:160-183` | Substitutes `IRetryCaseCustody` and records the `RetryCaseCustodyRequest`s it receives — the ready-made way to assert the endpoint composed the Core request correctly (all seven members, `Actor` server-derived) without touching Box. |
| `tests/Pegasus.IntegrationTests/CustodyOutboxIntegrationTests.cs:834`, `:980` | Exercises the **real** store, so it is where an audit-row assertion belongs rather than in the substituted-port test. |
| `tests/Pegasus.Core.Tests/Operations/OperationsUseCaseTests.cs:65` | The only existing exercise of `RetryMailboxProcessing`. Shows the fake-store shape the contract tests can borrow for the command that has no Razor precedent. |
| `docs/current-architecture.md:571` | The rule this ticket exists to protect, verbatim: "For Box custody, an initial failed operation remains terminal and visible for authorised staff to retry; no automatic business retry is permitted." |
| `docs/frd/frd-05-documents-extraction-and-custody.md:27` | The same rule in the FRD: "A Box failure after Case/PO allocation retains the Case as `Not ready` with explicit failure and staff-initiated retry/recovery evidence. It does not roll back, reuse, or reallocate the reference, and no background or automatic business retry is permitted." |
| `docs/desktop/03-gateway-api-and-data/README.md:167` | The thirteen `urn:pegasus:problem:<slug>` values — `validation`, `not-authorized`, `version-conflict`, `lease-conflict`, `lease-expired`, `operation-conflict`, `client-unsupported`, `password-change-required`, `account-disabled`, `provider-unavailable`, `not-found`, `rate-limited`, `maintenance` — plus the rules that the body never carries a payload dump and always carries `correlationId`. Every outcome and exception here maps into that list; add nothing to it. |
| `docs/desktop/03-gateway-api-and-data/endpoint-map.md:11-27` | The Conventions header: every command body carries `operationKey`; case-scoped commands also carry `expectedVersion` and, where Core requires it, `editLeaseToken`; `X-Pegasus-Client-Version` and `X-Correlation-Id` on every request. |
| `docs/desktop/07-integrations/README.md` § 7 | Two trap rows that bind here: "Custody retry automated 'for convenience'" (forbidden — the desktop only exposes the existing use case) and "Box token in the package, or a 'temporary' long-lived URL left in logs". |

## Ripple effects

- **OpenAPI and the generated client.** Seven new DTOs change
  `openapi/pegasus-v1.json` (the committed snapshot from [[GWY-004]], plan
  handle `DSK-03-04`) and the Kiota client generated by
  `eng/api/Generate-ApiClient.ps1` ([[GWY-005]], plan handle `DSK-03-05`). CI
  fails if regeneration changes the tree, so regenerate and commit in the same
  PR.
- **[[FEAT-030]] (plan handle `DSK-07-04`) is blocked on this ticket** and binds
  these field names and the five custody outcomes directly into its
  confirmation dialogs. Freeze the names when the contract tests go green.
- **[[FEAT-027]] (plan handle `DSK-07-01`) supplies the eligibility fields**
  these commands consume — `canRetry`, the expected attempt count, the mailbox
  failure code and due time. A rename there breaks a command here.
- **[[GWY-008]] (plan handle `DSK-03-08`) owns the case route group and the
  lease endpoints**; [[GWY-010]] (plan handle `DSK-03-10`) owns the received
  group and the received-item read that must publish
  `expectedCurrentAttemptId`. Both are registration-site and contract overlaps,
  not just dependencies.
- **[[FEAT-045]] (plan handle `DSK-07-19`) will replace the echoed failure-code
  strings with a taxonomy.** Expect a follow-up edit; do not pre-empt it with a
  rival enum here.
- **[[GWY-018]] (plan handle `DSK-03-18`) re-reviews every `/api/v1` command**
  for contract and authorization gaps, and [[PLAT-005]] (plan handle
  `DSK-10-05`) adds direct-object tests over the same commands. Both read what
  this ticket writes.
- **`tests/Pegasus.IntegrationTests/CaseCustodyWebTests.cs`,
  `CustodyOutboxIntegrationTests.cs` and `OperationsWebTests.cs` stay green** —
  no Razor page, no Core and no Infrastructure file changes.
- **Documentation** — endpoint-map rows in three sections and the
  `docs/capabilities.md` `DSK` row.

## Out of scope

- **`src/Pegasus.Core` use-case bodies** — every one of the four. A retry that
  needs new Core behaviour is a different ticket (the ticket's own Guardrails).
- **`src/Pegasus.Infrastructure/Custody/`** — `BoxCaseCustody.cs` (1,016) and
  `BoxDocumentContentStore.cs` (240). The endpoint asks the Core use case; it
  never reaches a Box adapter.
- **`src/Pegasus.Infrastructure/Persistence/`** — read `EfOperationsStore.cs`,
  `EfExternalWorkStore.cs` and `EfIntakeAllocationStore.cs`, change none of
  them. **In particular: do not add a Web-side audit writer** to make step 9's
  assertion pass for the three commands that write no `ActionHistory` row. A
  missing audit row is a Core/Infrastructure gap to raise — see the plan's
  Risks section.
- **`src/Pegasus.Worker`** — every file. Step 10's guard exists to keep it that
  way.
- **Any Razor page**, including `Pages/Operations/Index.cshtml.cs`,
  `Pages/Cases/Custody.cshtml.cs` and `Pages/Intake/Details.cshtml.cs`. They
  stay deployable until their parity rows reach `UAT passed`.
- **A new table of any kind**, and therefore any `Grant*` migration
  (`scripts/Test-MigrationGrants.ps1`, PLAT-035). No replay cache is written in
  Web — Core already decides replay for all four commands.
- **Provider tokens and raw provider payloads** in any response body (ADR-0107).
- **Any Azure write.**
