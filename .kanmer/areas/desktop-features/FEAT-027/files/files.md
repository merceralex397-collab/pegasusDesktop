# Files — FEAT-027

Surveyed 2026-08-24 against fork `main` `191ddf33`. Paths marked *(created by …)*
do not exist today: `ls src` returns only `Pegasus.Core`, `Pegasus.Infrastructure`,
`Pegasus.Web`, `Pegasus.Worker`; `ls tests` only `Pegasus.ArchitectureTests`,
`Pegasus.Core.Tests`, `Pegasus.IntegrationTests`; there is no `openapi/` and no
`eng/`.

## Where the change lands

| Path | Why |
|---|---|
| `src/Pegasus.Contracts/` *(created by [[FND-029]], plan handle `DSK-02-04`; conventions by [[GWY-001]], plan handle `DSK-03-01`)* | `IntakeStatusResponse` + `MailboxIntakeStatus` and `ExternalWorkResponse` + its row record, as plain records with no EF, ASP.NET or Core types. Also the `queue_poisoned` and `received-poison:` constants the two poison counts are derived from, so neither literal is spelled twice. |
| `src/Pegasus.Web/` — the `/api/v1` **operations** route group only | Two `GET` endpoints registered inside the group from [[GWY-002]] (plan handle `DSK-03-02`), behind `Features:DesktopGateway` and the `PerformCasework` filter from [[GWY-003]] (plan handle `DSK-03-03`). Thin argument-mappers over the four Core ports; no business rule. Coordinate with [[GWY-013]] (plan handle `DSK-03-13`), which owns `GET /operations` in the same group — extend that group, never create a second. |
| `tests/Pegasus.Api.ContractTests/` *(created by [[TEST-001]], plan handle `DSK-08-01`)* | Gate-off 404, 401, 403 `not-authorized`, healthy mailbox → `current`, failed mailbox with future due → `unavailable`, never-polled → `unavailable` with `isPolled` false, the two poison counts as separate fields, and the no-credential assertion. |
| `tests/Pegasus.IntegrationTests/` | New LocalDB facts seeded with a failed external work item, a failed mailbox poll and one `ApprovedInboxPoisonMessage`, beside the existing `OperationsWebTests.cs` (363) and `OperationsPersistenceTests.cs` (144) fixtures, which stay green. |
| `docs/desktop/03-gateway-api-and-data/endpoint-map.md` § `Triage, Unidentified, Operations` | Two new rows. The section currently holds five data rows (`:112`–`:116`) and carries `GET /operations` plus the two command rows, but no intake-status row. |
| `docs/capabilities.md` | A `DSK` row for desktop intake status with its canonical owner named. The file uses `FAMILY-NN` two-digit ids across eighteen families and **contains no `DSK` family today** (`grep -n 'DSK' docs/capabilities.md` → no matches), so confirm the family exists — [[FND-011]] (plan handle `DSK-00-11`) creates it — before adding a row. |

## Context files

| Path | What it tells the implementer |
|---|---|
| `src/Pegasus.Web/Pages/Operations/Index.cshtml.cs:41-45`, `:67` | The freshness honesty rule in the code's own words — "Set only after the query returns, so a failed load never claims to be fresh (FRD-12)" — with the assignment at `:67` placed *after* the await at `:64`. `asOfUtc` must be taken in the same place, or the endpoint lies on a failed read. |
| `src/Pegasus.Web/Pages/Operations/Index.cshtml.cs:176-187` | `StateLabel` — the eight `RequestOperationState` members with their current operator words. Shows that `UnknownExternal` renders as "Unknown external" and is never folded into a success word. |
| `src/Pegasus.Core/Operations/RequestOperations.cs:13-23`, `:32-56` | The eight-member state enum and `RequestOperationProjection`'s members. `CanRetry` (`:51`) and `CanRevoke` (`:52`) are **fields of the projection** — the endpoint copies them and never recomputes eligibility. |
| `src/Pegasus.Core/Operations/RequestOperations.cs:72-153` | `GetRequestOperations`: `PerformCasework` at `:87`, `MaximumItems = 100` at `:76`, and the `InvalidDataException` invariants at `:96`–`:149`. Two matter here — "Only versioned file requests may expose revocation" (`:142`) and "Only a versioned durable external-work failure may expose retry" (`:149`) — because they are why `canRetry` can be trusted verbatim. |
| `src/Pegasus.Core/Operations/EmailOperations.cs:20-45` | `EmailOperationProjection`. `CanRetry` at `:45` is **derived** (`RetryMailboxId is not null && RetryExpectedDueAtUtc is not null`), not stored, and `SourceLength` (`:43`) carries an XML remark explaining why the refused-message byte length is the whole answer for the one production refusal — do not drop it from the DTO. |
| `src/Pegasus.Infrastructure/Persistence/EfOperationsStore.cs:133-220` | **The projection this endpoint is the first shipped caller of.** Received rows come from four sources: mailbox poll states (`MapInboxState`, `:489`), **poison messages** (`:136-149`, operation ids prefixed `received-poison:`, `State = Failed`, `RetryMailboxId: null` so `CanRetry` is false), intake receipts scoped to `SourceChannel == "mailbox"` (`:150-165`) and response evidence (`:166-180`). Sent rows come from three more. `OrderEmailOperations` (`:728`) is where the per-direction bound is applied. Read this before assuming what a "received failure" is. |
| `src/Pegasus.Core/Intake/MailboxIntake.cs:124-134` | `ApprovedInboxPoisonMessage` — the mailbox-poison record (`OccurrenceKey`, `ImmutableMessageId`, `FileName`, `SourceLength`, `ReceivedAtUtc`, `FailureCode`). This is a **different** poison from `queue_poisoned`: a message the provider or reader refused, not a queue message that exhausted its dequeue count. Summing the two into one figure is the defect step 7 exists to prevent. |
| `src/Pegasus.Core/Intake/RetainedMail.cs:356-364` | `MailPollHealth` with its own remark: "Raw facts only: turning them into a freshness state is policy and belongs to `GetRetainedMailFreshness`." That sentence is why the endpoint calls `Evaluate` rather than writing an `if`. |
| `src/Pegasus.Core/Intake/RetainedMail.cs:366-384` | `IRetainedMailQueries`. `ListPollHealthAsync` (`:382`) returns no mailbox address; `ListMailboxesAsync` (`:379`) returns `RetainedMailMailbox(MailboxId, MailboxAddress, IsPolled)` (`:339-342`). The join is on `MailboxId`, and `IsPolled` is how a configured-but-unpolled mailbox is told apart from a failing one. |
| `src/Pegasus.Core/Intake/RetainedMail.cs:641-711` | `GetRetainedMailFreshness`. `StaleAfter` (`:662`) is fifteen minutes and its remark (`:652-661`) says the number is **PROVISIONAL** and recorded as open in `docs/open-decisions.md`. `Evaluate` (`:680`) is `public static` over a list, so a one-element call gives the per-mailbox rule with no duplicated policy. |
| `src/Pegasus.Web/Pages/Mail/Index.cshtml.cs:253-258` | `FreshnessStatus` — the exact lowercase wire strings `current` / `stale` / `unavailable`. Reuse them; do not invent `ok` / `degraded`. |
| `src/Pegasus.Infrastructure/Persistence/EfIntakeWorkStore.cs:393-414` | Where queue poison lands on the intake side: `State = Failed`, `FailureCode = "queue_poisoned"` (`:410`). There is no `Poisoned` member in `IntakeWorkState` (`src/Pegasus.Core/Intake/DurableIntake.cs:12-22`), so the count is a filter on this code — and no new table is needed. |
| `src/Pegasus.Infrastructure/Persistence/EfExternalWorkStore.cs:400-535` | The external half of the same rule: `queue_poisoned` written at `:442`, `:475`, `:506`, `:524`, `:532`, with `CompletePoisonReplay` called at `:435`, `:468` and `:499` — a poisoned message whose effect already landed **completes** rather than failing. A count that ignores that would over-report. |
| `src/Pegasus.Web/Presentation/OperatorLabels.cs:340` | `"queue_poisoned"` → "Processing was attempted repeatedly without completing". The one label map; [[GWY-016]] (plan handle `DSK-03-16`) and [[FEAT-023]] (plan handle `DSK-05-23`) move it to `Pegasus.Contracts`. Do not write a second sentence for this code. |
| `src/Pegasus.Worker/host.json` | `maxDequeueCount 5`, `visibilityTimeout 00:05:00`, `maxPollingInterval 00:00:02`. The number behind "exhausted `maxDequeueCount`" in the ticket's step 7. |
| `docs/current-architecture.md:86-90` | "External clients and catch paths distinguish `terminal`, `transient`, and `unknown`; terminal outcomes stop retries, unknown outcomes remain unknown, and metrics count successful effects rather than attempts." The rule step 6 must not break. |
| `docs/current-architecture.md:104` | `GET /Operations` today: "no approval controls, general receipt ledger, manual/email/Automation receipt display, or Box request caller." A boundary statement — the new reads must not quietly grow one. |
| `docs/desktop/03-gateway-api-and-data/README.md:167` | The thirteen `urn:pegasus:problem:<slug>` values and the rule that the body never carries payload dumps and always carries `correlationId`. `not-authorized` is the one this ticket uses; add nothing. |
| `docs/desktop/03-gateway-api-and-data/endpoint-map.md:11-27` | The Conventions header: weak `ETag` on reads, `X-Pegasus-Client-Version` and `X-Correlation-Id` on every request, `pageSize ≤ 200`, newest first. |
| `tests/Pegasus.IntegrationTests/OperationsWebTests.cs:345` | Already seeds `FailureCode: "queue_poisoned"` for a `Failed` row. The seeded-failure fixture this ticket needs exists — extend it rather than building a new harness. |
| `tests/Pegasus.Core.Tests/Operations/OperationsUseCaseTests.cs` | The only existing exercise of `GetEmailOperations` (`:16`, `:29`) and `RetryMailboxProcessing` (`:65`). It shows the fake-store shape the contract tests can borrow. |
| `docs/frd/frd-12-operator-experience.md:93-99` | "Every count and query exposes its last successful update time and current refresh state. `0`, loading, current, stale-with-last-good-time, partial, unavailable, and failed are distinct outcomes. A refresh never replaces a last-good value with a false zero…". The FRD sentence the `asOfUtc` and freshness fields satisfy. |

## Ripple effects

- **OpenAPI and the generated client.** New DTOs in `src/Pegasus.Contracts`
  change `openapi/pegasus-v1.json` (the committed snapshot from [[GWY-004]],
  plan handle `DSK-03-04`) and the Kiota client generated by
  `eng/api/Generate-ApiClient.ps1` ([[GWY-005]], plan handle `DSK-03-05`). CI
  fails if regeneration changes the tree, so regenerate and commit in the same
  PR.
- **[[FEAT-030]] (plan handle `DSK-07-04`) binds these fields directly.** Any
  rename after it starts is a breaking change to a screen; freeze the field
  names when the contract tests go green.
- **[[GWY-013]] (plan handle `DSK-03-13`) owns `GET /operations` in the same
  route group.** Two rows in one group, one registration site. Raise overlap
  there rather than registering a second group.
- **[[FEAT-045]] (plan handle `DSK-07-19`) will replace the `failureCode`
  string with a taxonomy.** Expect a follow-up edit to these DTOs; do not
  pre-empt it with a rival enum.
- **[[PLAT-015]] (plan handle `DSK-10-15`) adds `/api/v1/admin/health`** over
  the same worker-cycle facts. Its aggregate reads these Core ports, not this
  endpoint — coordinate so one projection exists.
- **`tests/Pegasus.IntegrationTests/OperationsWebTests.cs` and
  `OperationsPersistenceTests.cs` stay green** — no Razor page and no Core file
  changes.
- **Documentation** — the two endpoint-map rows and the `docs/capabilities.md`
  `DSK` row.

## Out of scope

- **`src/Pegasus.Worker`** — every file. The ticket's own verification runs
  `git diff --stat origin/dev -- src/Pegasus.Worker` and expects empty output.
- **`src/Pegasus.Infrastructure/Email/`** — the Graph adapters. Credentials stay
  central (ADR-0106).
- **`src/Pegasus.Core`** — no new use case, no new projection field. If a field
  the DTO needs does not exist in Core, stop and raise it rather than adding it
  here.
- **`src/Pegasus.Infrastructure/Persistence/EfOperationsStore.cs`** — read it,
  do not change it. A projection change is a Core-contract change and belongs to
  a ticket that owns the projection.
- **Any Razor page**, including `Pages/Operations/Index.cshtml.cs`. It stays
  deployable until `PAR-27` reaches `UAT passed`.
- **The retry commands.** [[FEAT-028]] (plan handle `DSK-07-02`) owns every
  `POST`; this ticket publishes only the `canRetry` flag those commands honour.
- **A new table of any kind**, and therefore any `Grant*` migration
  (`scripts/Test-MigrationGrants.ps1`, PLAT-035). If session or cache state
  seems necessary, that is a signal the design is wrong here.
- **Any Azure write.** Reads of App Insights or storage for diagnosis need no
  approval (`docs/runbook.md` § Live-operation approval matrix).
