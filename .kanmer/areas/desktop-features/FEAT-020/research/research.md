# Research — FEAT-020: S20 Operations and integration health

Repository revision read: `bbd1c549` (`git rev-parse --short HEAD`, 2026-08-24). Line numbers from
`grep -n` / `sed -n` at that revision.

## Question

What the Operations snapshot actually projects today, what "retry eligible" means in Core, what the
health surface may disclose — and the ticket's step-3 question: whether the received-intake row
really reports a case link it does not join (upstream `INTK-004`), and which of the two disagreeing
statements is wrong.

## Current behaviour

`src/Pegasus.Web/Pages/Operations/Index.cshtml.cs`, 236 lines (`wc -l`):

- `OnGetAsync` `:57`, `OnPostRetryExternalAsync` `:71`, `OnPostRevokeLinkAsync` `:112` — all three
  line numbers in the ticket body confirmed.
- Injected use cases (`:17-23`): `GetRequestOperations`, `RetryExternalWork`,
  `IAcquireCaseEditLease`, `IReleaseCaseEditLease`, `IRevokeRequestUploadLink`, `TimeProvider`.
  The page is `[Authorize(Roles = Administrator, Engineer, User)]` (`:13-14`) and
  `[ValidateAntiForgeryToken]` (`:15`).
- `LoadedAtUtc` is set **only after the query returns**, with the comment "so a failed load never
  claims to be fresh (FRD-12)" (`:41-45`). That freshness rule carries straight across to the
  desktop.
- Retry: `retryExternalWork.ExecuteAsync(new(workItemId, expectedAttemptCount, actor, operationKey))`
  (`:88-90`), and the outcome distinguishes replay from first execution via `result.IsReplay`
  (`:91-93`). Three catch arms map to distinct operator messages: `StaffAuthorizationException` →
  `Forbid()`, `ArgumentException` → "invalid", `InvalidOperationException` → "The external work
  failure changed before retry" (`:95-107`).
- Revoke takes `requestId, caseId, expectedVersion, expectedCaseVersion, reason, operationKey`
  (`:112-119`) and brackets the call with a case edit lease acquire/release.

Core: `src/Pegasus.Core/Operations/` holds four files — `DashboardCounts.cs`, `EmailOperations.cs`,
`OperationsSnapshot.cs`, `RequestOperations.cs`.
`RequestOperationProjection` (`RequestOperations.cs:32-56`) carries a **non-nullable `CaseId` and
`CaseReference`**, plus `ExternalKind`, `AttemptCount`, `FailureCode`, `FailureReason`, `CanRetry`,
`CanRevoke`, `CaseVersion` and `CaseEditLeaseState`. `RetryExternalWork` is at `:171` and validates
`WorkItemId != Guid.Empty` (`:186`) and a non-negative `ExpectedAttemptCount` (`:190`).

Health: `src/Pegasus.Web/Health/DatabaseReadinessHealthCheck.cs` is the only file in `Health/`;
`Program.cs:939` and `:945` map `/health/live` and `/health/ready`, and `:954` maps
`/diagnostics/version`.

Parity-matrix: the operations rows sit among the 46 `PAR-` rows
(`grep -c '^| PAR-' docs/desktop/01-inventory-and-parity/parity-matrix.md → 46`).

## Findings

### The upstream `INTK-004` question, resolved as far as reading can resolve it

- `src/Pegasus.Infrastructure/Persistence/EfOperationsStore.cs:159` hard-codes `CaseId: null` on
  the **received-intake** `EmailOperationProjection` row. It is one of seven `CaseId: null`
  literals in that file (`:144`, `:159`, `:175`, `:192`, `:221`, `:511`, `:543`); `:159` is the
  received-intake one the ticket names.
- `docs/current-architecture.md:291` claims: "Operations, retained Mail, Upload, MCP, and retry
  surfaces join the current allocation state and actual Case link."
- **But the two projections are different, and only one of them is null.**
  `RequestOperationProjection` — what the Operations Razor page actually renders through
  `GetRequestOperations` — carries a real, non-nullable `CaseId` and `CaseReference`
  (`RequestOperations.cs:35-36`). It is `EmailOperationProjection`
  (`src/Pegasus.Core/Operations/EmailOperations.cs:20-46`) whose received-intake row is null.
- **And `EmailOperationProjection` has no caller.** `grep -rn "GetEmailOperations" src/ --include=*.cs`
  returns exactly two hits: the class declaration at
  `src/Pegasus.Core/Operations/EmailOperations.cs:62` and a DI registration at
  `src/Pegasus.Infrastructure/DependencyInjection.cs:240`. No page model, no MCP tool and no
  endpoint consumes it today.
- So the honest statement of the defect is narrower and more useful than "the Operations row lies":
  **the email-operations projection would report a null case link if it were surfaced, and
  `current-architecture.md:291` already claims it joins one.** The desktop Operations screen is the
  first surface that would make that claim visible. The decision at step 3 is therefore a real
  decision made *before* the row is rendered, not a repair of something on screen.
- `EmailOperationProjection` does carry `IntakeId` (`:26`) and `CaseReference` (`:29`), so a join
  is expressible; the ticket names `IntakeReceipt.CurrentCaseId` as the single resolution path and
  forbids a second copy.

### The rest

- **Retry eligibility is Core's, not the client's.** `RequestOperationProjection.CanRetry` /
  `CanRevoke` (`RequestOperations.cs:50-51`) are computed server-side, and
  `EmailOperationProjection` exposes its own `CanRetry => RetryMailboxId is not null &&
  RetryExpectedDueAtUtc is not null` (`EmailOperations.cs:45`). The desktop must offer retry only
  where the gateway says so — it does not infer eligibility from a failure code.
- **Replay is already distinguishable.** `RetryExternalWork` returns `IsReplay`
  (`Index.cshtml.cs:91`), which is what a "replay returns the same result" contract fact asserts
  against.
- **Freshness is a stated rule with an FRD behind it.** `Index.cshtml.cs:41-45` cites FRD-12; the
  desktop must not show a stale `LoadedAtUtc` after a failed refresh.
- **Health today is thin.** `Health/` contains one check
  (`DatabaseReadinessHealthCheck.cs`); the integration-health payload the ticket describes —
  Graph worker last cycle, Box, DVLA/DVSA, feed state, minimum client version — is **new**, built
  by [[FEAT-027]] (plan handle `DSK-07-01`) and `GET /api/v1/admin/health`
  (`docs/desktop/03-gateway-api-and-data/endpoint-map.md` § `Session, compatibility, diagnostics`,
  right `ManageWorkflowConfiguration`). It is not a rename of `/health/ready`.
- **The screen itself is not this ticket's to create.** [[FEAT-030]] (plan handle `DSK-07-04`) owns
  `OperationsViewModel` and `OperationsPage.xaml`; this slice adds the audited retry and revoke
  commands to them. One view model per screen; a second is a stop condition.
- **The namespace collisions are real and are worth stating twice.** Upstream `PLAT-023` (redesign
  the Operations workspace) and upstream `INTK-004` have **no fork ticket**. The board's own
  `PLAT-023` is `DSK-11-05` ("Resource-health, advisor and compliance read of the estate") and the
  board's own `INTK-004` is upstream `INTK-027` ("Make policy re-evaluation work after transient
  staging cleanup") — verified against the `HZN-001` group document `board-conventions.md`
  § `Upstream ids versus board ids`. This screen owns upstream INTK-004's Operations half;
  [[FEAT-023]] (plan handle `DSK-05-23`) owns its label half.
- **End-to-end scenario 13 is defined in the proposal, not in plan 08.**
  `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md:1652` — "13. An integration failure is
  visible and recoverable." Plan 08 references "End-to-end business scenarios 1–14" at
  `docs/desktop/08-testing/README.md:18`, `:229` and `:265`, and [[TEST-016]] (plan handle
  `DSK-08-16`) authors the UAT scripts. Reading plan 08 alone for scenario 13's text will not find
  it.

### Facts

- `Pages/Operations/Index.cshtml.cs` is 236 lines; handlers at `:57`, `:71`, `:112`.
- Seven `CaseId: null` literals in `EfOperationsStore.cs`; the received-intake row is `:159`.
- `GetEmailOperations` has no caller (`grep -rn "GetEmailOperations" src/ --include=*.cs`).
- `RequestOperationProjection.CaseId` is `Guid` (non-nullable) and `CaseReference` is `string`.
- `src/Pegasus.Web/Health/` contains one file.
- `src/Pegasus.Desktop`, `src/Pegasus.Contracts`, `tests/Pegasus.Api.ContractTests`,
  `tests/Pegasus.Desktop.ViewModelTests` and `tests/Pegasus.Desktop.UITests` do not exist yet.

### Assumptions

- `A-05-20-1` — the desktop Operations screen will surface the **email** operations projection as
  part of "intake status", which is why the null case link matters now. *Confirm:* read
  [[FEAT-027]]'s (plan handle `DSK-07-01`) intake-status payload and [[FEAT-030]]'s screen scope at
  step 3. *If wrong:* the INTK-004 decision reduces to the documentation correction alone, which is
  still a required outcome.
- `A-05-20-2` — `IntakeReceipt.CurrentCaseId` is the single case-id resolution path the ticket
  names and it is reachable from the email-operations query without a second copy. *Confirm:* with
  [[GWY-013]] (plan handle `DSK-03-13`), who owns the projection. *If wrong:* the decision must be
  the documentation correction, because inventing a second resolution is a stop condition.
- `A-05-20-3` — `GET /api/v1/admin/health` is gated by `ManageWorkflowConfiguration` as the
  endpoint map states, so the health panel is administrator-only while the rest of the screen is
  `PerformCasework`. *Confirm:* read [[GWY-013]]'s and plan 10's merged contracts. *If wrong:* the
  panel's visibility rule changes, not its content rule.
- `A-05-20-4` — the update-feed state and minimum client version are exposed through the
  compatibility surface built by [[GWY-023]] (plan handle `DSK-04-06`). *Confirm:* read that
  ticket's endpoint before writing step 7. *If wrong:* the two values come from `admin/health`
  instead and the panel is unchanged.

## Execution placement

Six-question test from `docs/desktop/00-governance-and-workflow/README.md` § 3 (`:169-176`):

| Question | Answer | Evidence |
| --- | --- | --- |
| Shared authority — must several users see and update the same state? | **yes** | A retry claims a work item; two operators must not both retry it. `RetryExternalWork` takes `ExpectedAttemptCount` (`RequestOperations.cs:159`) precisely so a second retry against a changed attempt count fails. Lands in the **gateway**. |
| Unattended execution — must it run with every desktop closed? | **yes** | The work being retried is executed by `src/Pegasus.Worker`'s `ExternalWorkFunction` with every desktop closed (`reuse-map.md` § `Pegasus.Worker`). The desktop schedules; the **Worker**, already central, executes. |
| Protected credentials — long-lived secret that must not sit on workstations? | **yes, and this is the health panel's whole constraint** | The dependencies whose health is shown — Graph, Box, DVLA/DVSA — hold credentials that never reach the desktop (ADR-0106, ADR-0107, `reuse-map.md` `Email/` and `Custody/` rows). The **gateway** composes the health payload and emits states and last-cycle times only; the desktop displays what it is given and can disclose nothing more. |
| Public callback — must an external service call a stable public endpoint? | **no** | Nothing calls back into this surface; `/health/*` are read endpoints. |
| Central enforcement — revocation, permissions, audit, invariant independent of the client? | **yes** | Retry eligibility (`CanRetry`), revoke authority and the audit record are server-side; `RevokeRequestUploadLink` is bracketed by a case edit lease (`Index.cshtml.cs:112-119`). Lands in the **gateway**. |
| Measured operational advantage — measured evidence central is materially better? | **yes** | The snapshot aggregates across cases, mailboxes and work items in one query with an `ETag`; assembling it from per-item desktop calls would multiply round trips for a screen an administrator refreshes often. |

Five "yes" answers, and each names **the gateway** (or the existing Worker) — the `Pegasus.Web`
Container App under L-01, not a new Azure resource. The desktop keeps list presentation, local
sorting and the command affordances. No Azure write: Application Insights and Azure resource state
are read-only inputs owned by plan 10 and plan 11, and this screen shows only what the gateway
health endpoint returns.

## Implications

1. **Step 3's decision is sharper than the ticket could state.** The Razor page's rows already
   carry a real case link; the null one is in `EmailOperationProjection`, which nothing consumes.
   The choice is therefore: resolve the link through `IntakeReceipt.CurrentCaseId` **before** the
   desktop first surfaces the row, or correct `docs/current-architecture.md:291` so it stops
   claiming a join for the email-operations surface. Either is honest; rendering the null row while
   the sentence stands is not.
2. **Do not create a second view model.** [[FEAT-030]] owns `OperationsViewModel` and
   `OperationsPage.xaml`. If it has landed, add the retry and revoke commands in place and change no
   existing member; if it has not, create the type with exactly the members [[FEAT-030]] step 3
   pins (`ObservableObject`, `[RelayCommand]`, no UI type in the view model) and record in the plan
   which case applied.
3. **Eligibility comes from the server, and the test must prove it.** A contract fact for "retry of
   an ineligible item is refused with a problem" is the counterpart of the view-model fact that the
   command is disabled — the second without the first proves nothing.
4. **The health payload needs a negative test, not a review.** "Contains no secret-shaped value" is
   assertable: no connection-string fragment, no bearer token shape, no internal host name. Write it
   as a fact rather than trusting the review.
5. **Freshness and colour are both honesty rules.** `LoadedAtUtc` set only after success
   (`Index.cshtml.cs:41-45`, FRD-12) and "no colour-only state" (`docs/design/README.md`) are the
   same class of requirement as the case-link question: the screen must not assert more than it
   knows.

## Open questions

None that belong in an `open-questions` document.

- **The honest-case-link decision** — the ticket body directs it to the plan: "Decide which with
  [[GWY-013]], who owns the projection, and record the decision and its evidence in the plan." It is
  recorded in the plan's *Risks / open questions* with [[GWY-013]] named as owner. A decision a
  named sibling ticket owns is a scope boundary, not an open question.
- The `EfOperationsStore` projection change, if that is the decision — explicitly [[GWY-013]]'s per
  the ticket's Guardrails.
- The Operations screen type itself — [[FEAT-030]] (plan handle `DSK-07-04`); same treatment.
