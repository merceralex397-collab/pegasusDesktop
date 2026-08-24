# Research — FEAT-027: what the gateway can already say about intake status without touching the Worker

## Question

Which existing read models carry per-mailbox poll health, external-work failure
and poison visibility; can `GET /api/v1/operations/intake-status` and
`GET /api/v1/operations/external-work` be composed from them with **no** Worker
change and **no** new Core use case; and where does the poison count actually
come from?

## Current behaviour

Read at fork `main` `191ddf33` on 2026-08-24. The implementer re-reads after the
latest upstream sync ([[FND-023]], plan handle `DSK-01-10`) and records the SHA,
because upstream PLAT-039 and the MAIL fixes arrive with it.

| Surface | `path:line` | What it does today |
| --- | --- | --- |
| Operations page | `src/Pegasus.Web/Pages/Operations/Index.cshtml.cs:57` `OnGetAsync` | 236-line page model; calls `GetRequestOperations.ExecuteAsync` and nothing else for the read |
| Freshness honesty | `…/Operations/Index.cshtml.cs:41-45`, `:67` | `LoadedAtUtc` is documented "Set only after the query returns, so a failed load never claims to be fresh (FRD-12)" and is assigned at `:67`, *after* the await at `:64` |
| Retry external work | `…/Operations/Index.cshtml.cs:71` `OnPostRetryExternalAsync` | `RetryExternalWork` with `expectedAttemptCount` + `operationKey`; [[FEAT-028]] (plan handle `DSK-07-02`) owns the command half |
| Revoke upload link | `…/Operations/Index.cshtml.cs:112` `OnPostRevokeLinkAsync` | Acquires and releases a case edit lease around `IRevokeRequestUploadLink` |
| Mail freshness banner | `src/Pegasus.Web/Pages/Mail/Index.cshtml.cs:144` | `GetRetainedMailFreshness.ExecuteAsync`; `:253` `FreshnessStatus` maps the three states to `current` / `stale` / `unavailable` |

Parity-matrix row: **`PAR-27`** — "13.10 Administration and operations · FRD-12 ·
`Operations/Index.cshtml.cs` (236) … `~GET /api/v1/operations`", status
`not inventoried`
(`docs/desktop/01-inventory-and-parity/parity-matrix.md`). No row covers a
*per-mailbox intake-status* surface, because none exists in the web app today —
the mailbox half is only the Inbox banner (`PAR-21`). The matrix holds
`PAR-01`…`PAR-46` (`grep -c '^| PAR-' …/parity-matrix.md` → 46).

## Findings

### Facts

- **Three read ports, and only three, are needed.** `GetRequestOperations`
  (`src/Pegasus.Core/Operations/RequestOperations.cs:71`, `MaximumItems = 100`
  at `:73`), `GetEmailOperations`
  (`src/Pegasus.Core/Operations/EmailOperations.cs:61`,
  `MaximumItemsPerDirection = 50` at `:63`) and
  `IRetainedMailQueries.ListPollHealthAsync`
  (`src/Pegasus.Core/Intake/RetainedMail.cs:382`). All three already require
  `StaffAccessRight.PerformCasework` inside Core
  (`RequestOperations.cs:84`, `EmailOperations.cs:76`,
  `RetainedMail.cs:667`), so the endpoint filter is defence in depth, not the
  only check.
- **Poison is a failure *code*, not a state — and it is already queryable.**
  `IntakeWorkState` (`src/Pegasus.Core/Intake/DurableIntake.cs:12-22`) has
  seven members and **no** `Poisoned`. `EfIntakeWorkStore.MarkPoisonedAsync`
  (`src/Pegasus.Infrastructure/Persistence/EfIntakeWorkStore.cs:393-413`) sets
  `State = Failed`, `DueAtUtc = failedAtUtc` and
  `FailureCode = "queue_poisoned"` at `:410`.
  `EfExternalWorkStore.MarkPoisonedAsync`
  (`src/Pegasus.Infrastructure/Persistence/EfExternalWorkStore.cs:400`) writes
  the same literal at `:442`, `:475`, `:506`, `:524` and `:532`. The poison
  count that ticket step 7 requires is therefore
  `count(failureCode == "queue_poisoned")` over data the two projections
  already return — **no new table, no `Grant*` migration, no Worker change**.
- **The operator sentence for that code already exists.**
  `src/Pegasus.Web/Presentation/OperatorLabels.cs:340` maps
  `"queue_poisoned"` → `"Processing was attempted repeatedly without
  completing"`. That map is the one [[GWY-016]] (plan handle `DSK-03-16`) and
  [[FEAT-023]] (plan handle `DSK-05-23`) relocate to `Pegasus.Contracts`; this
  ticket consumes it and writes no second label list.
- **`maxDequeueCount` is 5.** `src/Pegasus.Worker/host.json` `extensions.queues`
  — `batchSize 4`, `newBatchThreshold 2`, `visibilityTimeout 00:05:00`,
  `maxDequeueCount 5`, `maxPollingInterval 00:00:02`. Exhaustion is what the
  poison queues (`intake-work-poison`, `external-work-poison`) consume, and the
  poison functions are `IntakePoisonFunction`
  (`src/Pegasus.Worker/IntakeFunctions.cs:48-50`) and `ExternalPoisonFunction`
  (`src/Pegasus.Worker/Functions/ExternalWorkFunctions.cs:24-27`).
- **`MailPollHealth` does not carry the mailbox address.**
  `src/Pegasus.Core/Intake/RetainedMail.cs:360-364` is
  `(string MailboxId, DateTimeOffset? LastCompletedAtUtc, string? LastFailureCode, DateTimeOffset DueAtUtc)`
  and `EfRetainedMailboxMessageStore.ListPollHealthAsync:347-359` projects
  exactly those four columns from `ApprovedInboxPollStates`. The address lives
  on `RetainedMailMailbox` (`RetainedMail.cs:339-342`:
  `MailboxId`, `MailboxAddress`, `IsPolled`) returned by
  `IRetainedMailQueries.ListMailboxesAsync` (`:380`). The DTO's
  `mailboxAddress` therefore needs a **join on `MailboxId`** between the two
  queries — the ticket's step 3 list of three ports is one short.
- **Per-mailbox freshness can reuse the Core policy verbatim.**
  `GetRetainedMailFreshness.Evaluate` (`RetainedMail.cs:681-711`) is a
  `public static` method over `IReadOnlyList<MailPollHealth>`. Calling it with a
  one-element list per mailbox reproduces step 6's rule exactly — count 0 →
  `Unavailable`; a recorded `LastFailureCode` with `DueAtUtc > now` →
  `Unavailable`; no completed poll → `Unavailable`; otherwise `Current` unless
  older than `StaleAfter` (`:661`, 15 minutes, documented **PROVISIONAL**).
  Writing a second per-mailbox rule would duplicate a policy Core owns.
- **The wire strings already exist.** `src/Pegasus.Web/Pages/Mail/Index.cshtml.cs:253-258`
  `FreshnessStatus` returns exactly `"current"`, `"stale"`, `"unavailable"` for
  the three `MailFreshnessState` members (`RetainedMail.cs:344-349`).
- **`EmailOperationProjection.CanRetry` is derived, not stored.**
  `src/Pegasus.Core/Operations/EmailOperations.cs:43` —
  `RetryMailboxId is not null && RetryExpectedDueAtUtc is not null`.
  `RequestOperationProjection.CanRetry` (`RequestOperations.cs:49`) is a stored
  field of the projection. The endpoint copies both; it computes neither.
- **`RequestOperationState` has eight members** (`RequestOperations.cs:13-22`:
  `Pending`, `Active`, `Expired`, `Exhausted`, `Revoked`, `Failed`, `Completed`,
  `UnknownExternal`) and `EmailOperationState` has four
  (`EmailOperations.cs:11-17`: `Pending`, `Succeeded`, `Failed`, `Unknown`).
  `UnknownExternal` and `Unknown` are the members that must **not** collapse
  into success — `docs/current-architecture.md:86-90` requires `terminal`,
  `transient` and `unknown` to stay distinct and "unknown outcomes remain
  unknown".
- **The projections are bounded and validated in Core.** `GetRequestOperations`
  throws `InvalidDataException` if the store returns more than `MaximumItems`
  (`RequestOperations.cs:96`) or an uninitialised collection (`:92`);
  `GetEmailOperations.Validate` does the same (`EmailOperations.cs:84-104`).
  The endpoint must surface `limitReached` rather than silently truncating
  again.
- **The Worker functions are the only unattended callers**, and this ticket
  reads none of them: `PendingWorkDispatchFunction`
  (`src/Pegasus.Worker/IntakeFunctions.cs:9-13`), `IntakeWorkFunction`
  (`:31-33`), `IntakePoisonFunction` (`:48-50`),
  `StagedArtifactReconciliationFunction` (`:68-75`), `InboxPollFunction`
  (`src/Pegasus.Worker/MailboxFunctions.cs:8-15`), `SentEvidencePollFunction`
  (`src/Pegasus.Worker/EmailEvidenceFunctions.cs:9-16`),
  `DueWorkSweepFunction` (`:49-53`), `ExternalWorkFunction`
  (`src/Pegasus.Worker/Functions/ExternalWorkFunctions.cs:7-9`),
  `ExternalPoisonFunction` (`:24-27`).
- **The existing test evidence.**
  `tests/Pegasus.IntegrationTests/OperationsWebTests.cs` (363 lines — and `:345`
  already seeds `FailureCode: "queue_poisoned"` for a `Failed` row, so the
  fixture shape this ticket needs exists) and
  `OperationsPersistenceTests.cs` (144 lines).
- **The projects this ticket writes into do not exist yet.** `ls src` returns
  `Pegasus.Core`, `Pegasus.Infrastructure`, `Pegasus.Web`, `Pegasus.Worker`;
  `ls tests` returns `Pegasus.ArchitectureTests`, `Pegasus.Core.Tests`,
  `Pegasus.IntegrationTests`. There is no `src/Pegasus.Contracts`, no
  `tests/Pegasus.Api.ContractTests`, no `openapi/pegasus-v1.json` and no
  `eng/` directory.
- **The problem-type catalogue is fixed and this ticket adds nothing to it.**
  `docs/desktop/03-gateway-api-and-data/README.md:167` lists the thirteen
  `urn:pegasus:problem:<slug>` values; `not-authorized` is among them.

### Assumptions

- **A-07-01-1 — `ApprovedInboxPollStates` holds one row per approved mailbox,
  including a mailbox that has never polled.** Confirmed by: the LocalDB
  integration test at plan step 9 asserting a row count equal to the approved
  mailbox count. Breaks if: a never-polled mailbox has no row — the
  intake-status list would then silently omit the mailbox that is most likely
  to be broken, which is the exact failure the surface exists to prevent. The
  fix is a left join from `ListMailboxesAsync`, not a new table.
- **A-07-01-2 — `GetEmailOperations`' 50-per-direction bound and
  `GetRequestOperations`' 100 bound are acceptable for a fleet-wide operations
  read.** Confirmed by: the `limitReached` flags being surfaced and asserted.
  Breaks if: production routinely exceeds them — then the desktop shows a
  truncated failure list while claiming completeness, and raising the bound is
  a Core change and therefore a different ticket.
- **A-07-01-3 — `queue_poisoned` is the only failure code written by a poison
  path.** Confirmed by
  `grep -rn 'queue_poisoned' --include=*.cs src tests` → six write sites, all
  in the two poison stores, plus the label map and one test. Breaks if: an
  upstream sync adds a second poison code — the count would then under-report,
  so the count is derived from a named constant in `Pegasus.Contracts` and a
  test asserts the constant matches the store literal.
- **A-07-01-4 — [[GWY-002]] (plan handle `DSK-03-02`)'s route group returns
  `404` for the whole group when `Features:DesktopGateway` is off.** Confirmed
  by: the gate test at plan step 8. Breaks if: it returns `503` or `401`
  instead — the acceptance criterion "404 with the gate off" then belongs to
  [[GWY-002]] and this ticket asserts whatever that ticket settled.
- **A-07-01-5 — [[FEAT-045]] (plan handle `DSK-07-19`) has not yet fixed the
  wire vocabulary when this ticket lands.** Confirmed by: checking
  [[FEAT-045]]'s stage before step 6. Breaks if: it has landed — then this
  ticket consumes its taxonomy type rather than defining `failureCode` as a
  free string, and no second list is created either way.

## Execution placement

The six-question test from `docs/desktop/00-governance-and-workflow/README.md`
§ 3, answered. This ticket places a **read** responsibility; the work being
reported on is already placed by ADR-0106.

| Question | Answer | Evidence, and where a "yes" lands |
| --- | --- | --- |
| Shared authority — must several users see and update the same state? | **yes** | Retryable external work and mailbox poll state are one shared queue every operator acts on; `RequestOperationProjection.CanRetry` (`RequestOperations.cs:49`) and the `expectedAttemptCount` guard exist because two staff can race a retry. **Lands in the gateway** — `Pegasus.Web` evolved in place (L-01), no new deployment unit. |
| Unattended execution — must it run with every desktop closed? | **yes** | The polling and queue work being reported on runs unattended: `InboxPollFunction` (`MailboxFunctions.cs:8-15`), `IntakeWorkFunction`/`IntakePoisonFunction` (`IntakeFunctions.cs:31,48`), `ExternalWorkFunction`/`ExternalPoisonFunction` (`ExternalWorkFunctions.cs:7,24`). **Lands in the existing `src/Pegasus.Worker`** (ADR-0106) — and this ticket writes no Worker code; it only reads what the Worker recorded. |
| Protected credentials — long-lived secret that must not sit on workstations? | **yes** | The Microsoft Graph credential behind every mailbox cycle. **Lands behind the gateway and Worker** (ADR-0106, ADR-0107); the desktop holds none and receives none — hence the step-8 assertion that no response field carries a mailbox credential, Graph token, connection string or storage key. |
| Public callback — must an external service call a stable public endpoint? | **no** | Graph is polled on a timer, not called back. No external service calls this surface, and step 5 of the body adds no subscription. |
| Central enforcement — revocation, permissions, audit, invariant independent of the client? | **yes** | `StaffAuthorization.Require(actor, StaffAccessRight.PerformCasework)` is inside the Core reads (`RequestOperations.cs:84`, `EmailOperations.cs:76`, `RetainedMail.cs:667`), and the `MaximumItems`/`MaximumItemsPerDirection` bounds are Core invariants that must hold whatever the client is. **Lands in the gateway.** |
| Measured operational advantage — measured evidence central is materially better? | **no** | No measurement in this repository supports rendering the operations view centrally. The one measured constraint points the other way — App Insights blind hours (PLAT-034, `docs/desktop/07-integrations/README.md` § 7) are why an operator needs a client-side surface at all. |

**Conclusion.** Four "yes" answers, and every one of them lands somewhere that
already exists: the reads in the gateway (L-01), the polling and queue work in
the Worker (ADR-0106), the Graph credential behind both (ADR-0107). Rendering,
scoping and the freshness presentation belong to the desktop ([[FEAT-030]],
plan handle `DSK-07-04`). **No new Azure resource and no Azure write.**

## Implications

- **Nothing new is computed; two things are joined.** The intake-status row is
  `MailPollHealth` joined to `RetainedMailMailbox` on `MailboxId`, with
  `GetRetainedMailFreshness.Evaluate` applied to the single-element list. That
  keeps the freshness policy in its one Core owner and keeps the endpoint a
  thin argument-mapper, which is the projection style the gateway plan requires
  (`docs/desktop/03-gateway-api-and-data/README.md` § 3, "Projection style").
- **The poison count is a filter, not a feature.** Because `queue_poisoned` is
  a failure code on rows the projections already return, "report the count of
  intake items that have exhausted `maxDequeueCount`" is a `Count(…)` over the
  projection — and the ticket's guardrail "this ticket must not add a table" is
  satisfiable without argument.
- **`asOfUtc` is the whole honesty contract.** `Index.cshtml.cs:41-45` states
  it in the code's own words; the endpoint reproduces it by taking the
  timestamp *after* the last await, and a failed query returns a problem, never
  a body with a fresh timestamp and an empty list.
- **`unknown` must survive the DTO.** `RequestOperationState.UnknownExternal`
  and `EmailOperationState.Unknown` are the two members that a friendly rollup
  would erase. They map to their own wire values, and the contract test asserts
  a row in either state is not reported as success.
- **The ticket's step 3 is one port short.** It names three; the address field
  in its own step-4 DTO needs `ListMailboxesAsync` as a fourth. That is a
  refinement of the body, not a contradiction — the body's step 4 fixes the
  field list, and this is how it is satisfied.

## Open questions

None that block. The two points that could look like questions have named
owners:

- The wire vocabulary for `failureCode` / `terminal` / `transient` / `unknown`
  is [[FEAT-045]] (plan handle `DSK-07-19`)'s contract to fix. Until it lands
  this ticket carries the Core codes verbatim and defines no rival list — a
  scope boundary, not a question.
- Whether `GetRetainedMailFreshness.StaleAfter` should stay at fifteen minutes
  is recorded as open in `docs/open-decisions.md` by the code's own remark
  (`RetainedMail.cs:651-661`). This ticket reuses the constant and neither
  re-argues nor hard-codes it.
