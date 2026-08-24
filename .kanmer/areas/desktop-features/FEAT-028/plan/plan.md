# Plan — FEAT-028: DSK-07-02 Human retry commands through the gateway

**Diff estimate: ~7 files, ~1,010 lines.**

Derived from the `files` document, not asserted. `src/Pegasus.Contracts`: 1 new
file ~140 (four request records — custody carries seven members, allocation six,
external work four, mailbox six — plus three response records).
`src/Pegasus.Web`: 1 new endpoint file ~260 (four `POST` handlers, each with its
own exception→problem translation; custody alone needs a five-way outcome map)
plus ~8 lines of registration split across the operations, cases and received
groups. `tests/Pegasus.Api.ContractTests`: 1 file ~360 — four commands × (success,
replay-equals-first, 401, 403, Automation-token refusal) plus custody's five
outcome facts and allocation's three exception facts.
`tests/Pegasus.IntegrationTests`: 1 file ~210 seeded against LocalDB for the
audit-trail facts. `tests/Pegasus.ArchitectureTests/WorkerCompositionTests.cs`
(259 today): ~35 lines added. Documentation: ~4 lines in `endpoint-map.md`
(three rows amended, one row new) and ~2 in `docs/capabilities.md`.

## Approach

Compose four `POST` endpoints as thin argument-mappers onto the four Core use
cases the Razor handlers already call, and prove idempotency **at the boundary
rather than implementing it** — each Core use case already owns replay
(`CustodyRetryPolicy.Decide` at `CustodyContracts.cs:333-339`, the allocation
command hash at `IntakeAllocation.cs:320-333`, and the store's own
`IsReplay` decision at `EfOperationsStore.cs:364-386` and `:466-483`). The
alternative considered and rejected was a Web-side operation-key cache in front
of the four endpoints: it looks like one uniform mechanism, but it would be a
second replay authority over `CustodyRetryPolicy`, whose own remark calls itself
"The sole owner of custody-retry replay, conflict, and eligibility decisions"
(`CustodyContracts.cs:325-327`), and it would need a table and therefore a
`Grant*` migration the ticket's Guardrails forbid. The second design decision is
that the custody wire shape carries the Core-supplied **`Message`** alongside the
outcome: four distinct refusals collapse into `Refused` in the enum
(`:347-348`, `:355-356`, `:360-361`, `:365-366`), so an outcome-only response
cannot tell "custody is already confirmed" from "the case has no Audit
reference", and the acceptance criterion "every one of the five values has a
distinct, documented wire representation" would be unmeetable. The rejected
alternative there — inventing five gateway-side sentences — would create a second
operator vocabulary against `AGENTS.md` § Simplicity rails.

## Governing docs

The ticket carries
`refs: ["docs/frd/frd-05-documents-extraction-and-custody.md",
"docs/frd/frd-02-intake-and-source-identity.md"]` and `docs_todo: true` —
confirmed in `get_doc_gates FEAT-028`, which reports `governing-doc`
**satisfied** at `leave-backlog`.

**Meets — `docs/frd/frd-05-documents-extraction-and-custody.md`.** `:27`
requires that a Box failure after allocation "retains the Case as `Not ready`
with explicit failure and staff-initiated retry/recovery evidence… no background
or automatic business retry is permitted." Step 5 exposes the retry as a
staff-initiated command that reaches the one Core use case authorised to re-arm
custody; step 10's guard is what keeps "staff-initiated" true; step 9 asserts the
"recovery evidence" the transaction already writes. No FRD text is modified.

**Meets — `docs/frd/frd-02-intake-and-source-identity.md`.** The allocation
retry (step 6) must not roll back, reuse or reallocate a reference: the endpoint
passes `ExpectedReceiptVersion` and `ExpectedCurrentAttemptId` straight through
so Core's concurrency check (`IntakeAllocation.cs:318-333`) decides, and it
surfaces `IntakeAllocationState.SafeReason` rather than a raw failure, mirroring
`Pages/Intake/Details.cshtml.cs:137-138`.

> **New ADR** — ADR-0106 (Graph intake worker stays central: unattended
> execution, protected credentials), authored by [[FND-005]] (plan handle
> `DSK-00-05`).
> This plan is written to the decision as recorded in
> `docs/desktop/00-governance-and-workflow/README.md` § 3; if the ADR lands
> differently this plan is revised before implementation.

> **New ADR** — ADR-0107 (Box and DVLA/DVSA credentials stay behind the gateway;
> no long-lived provider secret in the package), authored by [[FND-005]]. Same
> condition. This is the ADR behind the guardrail that no response body may
> carry a provider token or a raw provider payload.

> **New ADR** — ADR-0103 (gateway; never direct database access from
> workstations), authored by [[FND-005]]. Same condition.

`refs` names two FRDs and no ADR, so the programme-level authorities that bind
today are tabulated for `kanmer-review` to check against the diff:

| Authority | Requirement | Met by |
| --- | --- | --- |
| `docs/current-architecture.md:571` | "For Box custody, an initial failed operation remains terminal and visible for authorised staff to retry; no automatic business retry is permitted." | Steps 5 and 10 |
| Proposal § 12.1 | Parsing failures enter a visible retry/failure table | Steps 4–6, over [[FEAT-027]]'s reads |
| Proposal § 13.10 | Failed-work review and retry screens are parity capabilities | Steps 4–6 |
| Proposal § 16.1 | One operation model: caller-supplied key, replay returns the first result | Step 7 |
| Proposal § 27 item 2 | An operator never needs the web app open to complete a task | The four commands together |
| L-01 | Gateway is `Pegasus.Web` evolved in place — route groups, no new deployment unit | Steps 4–6 |
| L-02 | Replay and authorization evidence is produced on the local stack, never an Azure test resource | Steps 7–9 |
| L-04 | Subagent, skills and MCP named on the ticket | § Routing below |
| ADR-0106 | The desktop never retries by calling Graph itself; it asks the gateway, which asks the same Core use case | Steps 4–6 |
| ADR-0107 | No provider token or raw provider payload in a response | Step 11's review of every response shape |
| `docs/desktop/03-gateway-api-and-data/README.md` § 3 "Projection style" | Endpoints are thin argument-mappers over Core ports; no business rule in Web; MCP and API stay two ingresses over one Core | Steps 4–6, and § Approach's rejection of a Web-side replay cache |
| `docs/desktop/03-gateway-api-and-data/README.md` § 3 "Idempotency" | `operationKey` is an explicit **body field**, not a header, because Core validates it per command | Step 3 |
| `docs/desktop/03-gateway-api-and-data/README.md:167` | Only the thirteen catalogued problem types; `correlationId` always present; no payload dumps | Steps 4–6 |
| `docs/desktop/03-gateway-api-and-data/README.md` § 3 "Audit & transactions" | Reuse the existing writers and store transactions; no event sourcing | Step 9, which reads rather than writes |
| `docs/desktop/07-integrations/README.md` § 7 (trap row) | "Custody retry automated 'for convenience'" | Step 10 |
| `docs/engineering.md` § Plan sizing | Diff estimate first; facts split from assumptions | This heading; `research` § Facts / Assumptions |
| `AGENTS.md` § Simplicity rails | One list per concept — one replay authority, one operator vocabulary | § Approach both rejections |
| `AGENTS.md` § Repository task workflow step 4 | Simplification pass over this branch's own diff before the PR | Step 12 |

## Routing

Copied from the ticket body's `## Routing` block.

- **Subagent**: `pegasus-gateway-dev` — `.codex/agents/pegasus-gateway-dev.toml`
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `dotnet-webapi`
  (dotnet/skills `98f84851`) → `code-testing-agent` (dotnet/skills `98f84851`,
  plugin `dotnet-test`) → `run-tests`
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`,
  `set_ticket_doc`, `append_scratch`, `move_item`); Microsoft Learn
  (`microsoft_docs_search`)
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` →
  `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (call
  `get_doc_gates <id>` before every move; a move crosses at most one gated
  boundary)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement
  (`AGENTS.md` § Repository task workflow step 5)

## Steps

These refine the ticket body's twelve implementation steps in the same order and
with the same ownership.

1. **Orient and take.** Read the plan row `DSK-07-02`
   (`docs/desktop/07-integrations/README.md` § 5), that plan's § 7 trap table,
   the endpoint-map Conventions header (`:11-27`) and the three sections the new
   rows land in (`:46-80`, `:81-95`, `:108-116`). Call `get_doc_gates FEAT-028`,
   then `take_ticket` with branch `task/dsk-07-02-retry-commands` and a worktree
   cut from `origin/dev`.
2. **Record the four command shapes in `files` — already done, verify it still
   holds.** The table is written; re-read the four Core records after the latest
   upstream sync ([[FND-023]], plan handle `DSK-01-10`) and **record the SHA**.
   The one correction to carry forward: the body names
   `CustodyRetryDecision.Decide`; the code has `CustodyRetryPolicy.Decide`
   (`CustodyContracts.cs:328`) and no type named `CustodyRetryDecision` exists.
   Use the real names.
3. **Add the DTOs to `src/Pegasus.Contracts`** *(created by [[FND-029]], plan
   handle `DSK-02-04`)*, field names **and types** copied from Core:
   `RetryExternalWorkRequest(Guid workItemId is the route parameter; int
   expectedAttemptCount, string operationKey)`;
   `RetryCustodyRequest(long expectedVersion, string operationKey, string reason,
   string editLeaseToken, string targetKind)`;
   `RetryIntakeAllocationRequest(long expectedVersion, Guid
   expectedCurrentAttemptId, string operationKey, string reason)`;
   `RetryMailboxProcessingRequest(string mailboxId, string direction, string
   expectedFailureCode, DateTimeOffset expectedDueAtUtc, string operationKey)`.
   Responses: `OperationsRetryResponse(bool isReplay)` for external work and
   mailbox processing; `CustodyRetryResponse(string outcome, long? caseVersion,
   string message)`; `IntakeAllocationRetryResponse(string status, bool isReplay,
   bool isSuppressed, long? version, string? caseReference, string? safeReason)`.
   `ActionActor` is server-derived and appears on no DTO. `expectedAttemptCount`
   is an `int` (`RequestOperations.cs:159`), not the `long` the body sketches;
   `reason` and `targetKind` are required by Core (`CustodyContracts.cs:433`,
   `:429`) and are not optional here.
4. **Implement `POST /api/v1/operations/external-work/{workItemId}/retry`**
   calling `RetryExternalWork.ExecuteAsync`, returning `200` with
   `{ "isReplay": bool }`. Translate `StaffAuthorizationException` →
   `urn:pegasus:problem:not-authorized`, `ArgumentException` /
   `ArgumentOutOfRangeException` → `validation`, and `InvalidOperationException`
   → `operation-conflict` — the Razor precedent is
   `Pages/Operations/Index.cshtml.cs:96`, `:100-102`, `:104-106`. Use the
   mapping ported in [[GWY-002]] (plan handle `DSK-03-02`); write no second
   translator.
5. **Implement `POST /api/v1/cases/{caseId}/custody/retry`** calling
   `IRetryCaseCustody.ExecuteAsync` with all seven members. Map
   `Conflict` → `409` `version-conflict` carrying `currentVersion` from
   `result.CaseVersion`, `Refused` → `409` `operation-conflict` with the
   Core-supplied `Message`, `NotFound` → `404` `not-found`, and `Replay` and
   `Pending` → `200` with `outcome` named and `message` carried. `CaseVersion`
   is nullable (`CustodyContracts.cs:307`) — a `NotFound` has none, so do not
   assume it. Register inside the cases group owned by [[GWY-008]] (plan handle
   `DSK-03-08`) and record which registration landed first.
6. **Implement `POST /api/v1/received/{receiptId}/retry-allocation` and
   `POST /api/v1/operations/mailbox-processing/retry.`** Allocation calls
   `IAllocateIntake.RetryAsync` and returns all three result fields —
   `state`, `isReplay` and `isSuppressed` — because `IsSuppressed` is the
   difference between "your retry ran" and "it was already done"
   (`IntakeAllocation.cs:334-338`). Map its three named exceptions distinctly:
   `IntakeAllocationOperationConflictException` → `operation-conflict`,
   `IntakeAllocationConcurrencyException` → `version-conflict`,
   `PrincipalUnavailableException` → `validation`. Mailbox processing calls
   `RetryMailboxProcessing.ExecuteAsync`, parses `direction` into
   `EmailOperationDirection` (an unparseable value is `validation`, never a
   default), and maps `InvalidOperationException` → `operation-conflict`,
   preserving the store's distinct sentences (`EfOperationsStore.cs:384`,
   `:388`, `:475`, `:485`) in the problem `detail`. Register in the received
   group owned by [[GWY-010]] (plan handle `DSK-03-10`) and the operations group
   respectively.
7. **Prove idempotency at the boundary, not in new code.** One test per command:
   issue the call twice with the same `operationKey` and assert the second
   response body equals the first. Expect `isReplay` to flip to `true` on the
   second call for external work, mailbox processing and allocation — that is
   the same result, correctly reported, not a different one; assert the
   *decision* fields are equal and that `isReplay` is `false` then `true`. For
   custody expect `Pending` then `Replay` (`CustodyContracts.cs:336-337`). Add a
   negative fact for allocation: the same key with **different** details throws
   `IntakeAllocationOperationConflictException` (`IntakeAllocation.cs:328-331`)
   and must surface as `operation-conflict`, not as a replay. Write no replay
   cache in Web.
8. **Authorization-failure tests per command** in
   `tests/Pegasus.Api.ContractTests` *(created by [[TEST-001]], plan handle
   `DSK-08-01`; template from [[TEST-002]], plan handle `DSK-08-02`)*:
   unauthenticated → 401; an actor without `PerformCasework` → 403
   `not-authorized`; an Automation-client token → rejected on `/api/v1` (the
   [[GWY-003]] rule, plan handle `DSK-03-03`). For custody assert **both**
   layers fire: Core's own `ActorKind.Staff` refusal at
   `CustodyContracts.cs:420-423` precedes the right check, so the refusal holds
   even if the filter is misconfigured. Enable `Features:DesktopGateway`
   explicitly in the positive tests, or a gated endpoint returns 404 and the test
   lies. Add the gate-off 404 fact for all four routes.
9. **Assert the audit each command actually leaves — and stop where there is
   none.** Custody: after a successful call, `CustodyOutboxIntegrationTests`-style
   LocalDB facts assert the `ActionHistory` row with
   `EventKind = "custody_retry_requested"`, `ActorSubjectId` = the staff actor
   and `CorrelationId` = the `operationKey`, plus the `CaseHistory` row
   (`EfExternalWorkStore.cs:210-238`). Allocation: assert the
   `IntakeAllocationAttempts` row carrying actor, operation key, command hash and
   reason (`EfIntakeAllocationStore.cs:31`, `:211`). External work and mailbox
   processing: `EfOperationsStore.RetryAsync` writes **no** `ActionHistory` row
   (`:343-409`, `:447-486`, measured) — assert the durable state transition those
   commands do produce, and **raise the missing audit row as a separate
   Core/Infrastructure ticket**. Do not add a Web-side audit writer to make the
   assertion pass; the `files` document's Out-of-scope section and the
   research's Implications both forbid it. See § Risks.
10. **Add the architecture guard, phrased about callers.** Registration is
    shared (`DependencyInjection.cs:217`, `:236`, `:238`, `:242`, `:243`), so
    "the Worker does not register custody retry" is **false** and would fail
    today. Assert instead in `tests/Pegasus.ArchitectureTests/WorkerCompositionTests.cs`
    that **no type in `Pegasus.Worker` takes `IRetryCaseCustody`,
    `RetryExternalWork`, `RetryMailboxProcessing` or `IAllocateIntake`'s retry
    path as a constructor dependency** — true today
    (`grep -rn 'RetryCaseCustody' src/Pegasus.Worker` returns nothing). Record
    `docs/current-architecture.md:571` and
    `docs/frd/frd-05-documents-extraction-and-custody.md:27` beside the
    assertion so a future reader knows the rule, not just the test.
11. **Endpoint map and capabilities.** Amend the three existing rows in
    `docs/desktop/03-gateway-api-and-data/endpoint-map.md` (§ `Cases`,
    § `Intake (received items), uploads, image intake`, § `Triage, Unidentified,
    Operations`) with the real concurrency tokens and returns, and add the one
    genuinely new row for `POST /operations/mailbox-processing/retry`. Review
    every response shape against ADR-0107 before committing: no provider token,
    no raw provider payload, no connection string. Add the `DSK` row to
    `docs/capabilities.md` — first confirming the `DSK` family exists
    (`grep -n 'DSK' docs/capabilities.md` returns nothing today; [[FND-011]],
    plan handle `DSK-00-11`, creates it).
12. **Simplification pass and PR.** Run the pass over this branch's own diff
    (`AGENTS.md` § Repository task workflow step 4), record it under a dated
    `## Simplification pass` heading below, then open the PR into `dev`.

## Verification

Evidence tier from the body: **5** — Web/API/MCP caller. Tier 5 obliges
observable route-level evidence of authentication, validation, idempotency,
exception translation and the action-history actor for every command; a
registration or a green build does not satisfy it.

- `dotnet test ./tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj --configuration Release`
  — expected: success, replay-equals-first, the five custody outcomes,
  allocation's three exception mappings, the gate-off 404 and the three
  authorization facts pass for all four commands. This output is the tier-5
  evidence.
- `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --filter "Category!=Corpus&Category!=Browser"`
  — expected: the custody `ActionHistory` / `CaseHistory` facts and the
  allocation-attempt fact pass, and every existing `CaseCustodyWebTests`,
  `CustodyOutboxIntegrationTests` and `OperationsWebTests` fact stays green.
- `dotnet test ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj --configuration Release`
  — expected: the no-automatic-custody-retry caller fact passes alongside the
  existing composition facts.
- `git diff --stat origin/dev -- src/Pegasus.Core src/Pegasus.Infrastructure src/Pegasus.Worker`
  — expected: **empty output**. This single command is the proof of the
  "no second business implementation" acceptance criterion and of the Guardrails'
  scope boundary; it belongs in the proof verbatim.

## Risks / open questions

- **Three of the four commands write no `ActionHistory` row today, and the body's
  acceptance criterion asks for one.** Measured:
  `EfOperationsStore.RetryAsync` for external work (`:447-486`) and mailbox
  processing (`:343-409`) are single `ExecuteUpdateAsync` transitions with no
  audit write, and `EfIntakeAllocationStore.cs` contains no `ActionHistory`
  reference at all; only custody's `EfExternalWorkStore.RetryAsync:210-238`
  writes the row. The Razor handlers produce no such row either, so "mirroring
  what the Razor handler produces today" is satisfied by asserting what each
  command actually leaves. Mitigation, and it is the research's own instruction:
  assert the real trail per command (step 9), and raise the missing
  `ActionHistory` rows as a separate Core/Infrastructure ticket rather than
  adding a Web-side writer — which the Guardrails forbid and which would put a
  second audit authority beside the store transactions. **Flagged to the
  reviewer: this ticket cannot fully close its own fifth acceptance criterion
  without that follow-up ticket.**
- **`RetryCaseCustodyResult.CaseVersion` is nullable.** A `NotFound` carries
  none. Mitigation: the `409 version-conflict` body carries `currentVersion`
  only where Core supplied it; a null is a `404` path, asserted at step 5.
- **The custody lease must be preserved on failure.**
  `Pages/Cases/Custody.cshtml.cs:52` calls `PreserveLeaseState` for
  `Conflict` / `Refused` / `NotFound` and `ClearLeaseState` for
  `Pending` / `Replay`. A gateway that always releases would cost the operator
  the lease on every refusal. Mitigation: the endpoint returns the outcome and
  leaves lease lifecycle to [[GWY-008]]'s lease endpoints; [[FEAT-030]] (plan
  handle `DSK-07-04`) mirrors the Razor split in the desktop.
- **Whether the received-item read publishes `expectedCurrentAttemptId`** is
  [[GWY-010]] (plan handle `DSK-03-10`)'s contract (assumption A-07-02-2). If it
  does not, the command cannot be composed and the field is raised there, not
  added here. A scope boundary with a named owner. Answered by: [[GWY-010]].
- **Whether the case-lease endpoints exist for the custody retry's
  `editLeaseToken`** is [[GWY-008]] (plan handle `DSK-03-08`)'s (assumption
  A-07-02-1). If they have not landed, this command waits rather than inventing
  a lease-free path. Answered by: [[GWY-008]].
- **The wire vocabulary for the failure codes these commands echo is not
  settled.** [[FEAT-045]] (plan handle `DSK-07-19`) owns
  `terminal` / `transient` / `unknown` and the five provider problem types. This
  ticket carries the Core strings verbatim and defines no rival list.
  Answered by: [[FEAT-045]].
- **`ExpectedFailureCode` / `ExpectedDueAtUtc` may diverge from what
  `EmailOperationProjection` publishes** (assumption A-07-02-3), which would
  make the desktop offer a retry that always conflicts. Mitigation: an
  integration fact that a row marked `canRetry` can be retried with exactly the
  values the same read returned.
- **Mailbox-processing retry has no Razor precedent.** Its operator sentences,
  problem mapping and enablement rule are decided here for the first time, and
  `EmailOperationProjection.CanRetry` (`EmailOperations.cs:45`) is the only
  eligibility source. Expect its Core validation to fire during development;
  that is a statement about the data, not a reason to relax the check.
- **A future ticket may legitimately want an automatic re-arm** (assumption
  A-07-02-4). That is an ADR-level change against
  `docs/current-architecture.md:571` and FRD-05 `:27`, not a relaxation of
  step 10's test.

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over
this branch's own diff before the PR, recorded here under a dated heading._
