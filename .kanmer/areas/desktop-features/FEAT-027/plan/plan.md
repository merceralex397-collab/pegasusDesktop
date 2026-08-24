# Plan — FEAT-027: DSK-07-01 Gateway intake-status endpoints

**Diff estimate: ~9 files, ~740 lines.**

Derived from the `files` document, not asserted. `src/Pegasus.Contracts`: 3 files
~135 (`IntakeStatusResponse` + `MailboxIntakeStatus` ~55; `ExternalWorkResponse`
+ its row ~65; the `queue_poisoned` constant ~15). `src/Pegasus.Web`: 1 new
endpoint file ~180 plus ~5 lines of registration in [[GWY-002]]'s group file.
`tests/Pegasus.Api.ContractTests`: 1 file ~230 (six gate/authorization facts,
three freshness facts, the poison-count fact, the no-credential fact).
`tests/Pegasus.IntegrationTests`: 1 file ~180 seeded against the LocalDB
fixtures. Documentation: ~4 lines in `endpoint-map.md` (two rows) and ~2 in
`docs/capabilities.md`.

## Approach

Compose both endpoints as thin argument-mappers over four existing Core read
ports — `GetRequestOperations`, `GetEmailOperations`,
`IRetainedMailQueries.ListPollHealthAsync` and `ListMailboxesAsync` — and reuse
`GetRetainedMailFreshness.Evaluate` **per mailbox** by calling the existing
`public static` method with a one-element list. That is the whole design
decision: the alternative considered was computing the per-mailbox freshness
state in the endpoint from `LastFailureCode` and `DueAtUtc` directly, which
looks simpler and is a duplicate of a policy Core already owns
(`src/Pegasus.Core/Intake/RetainedMail.cs:356-364` says so in its own remark:
"turning them into a freshness state is policy and belongs to
`GetRetainedMailFreshness`"), so it was rejected under `AGENTS.md` § Simplicity
rails. The second alternative — adding a poison-count column or table — was
rejected once the measurement showed poison is already recorded as the failure
code `queue_poisoned` on rows the projections return
(`EfIntakeWorkStore.cs:410`, `EfExternalWorkStore.cs:442`), so the count is a
filter and needs no `Grant*` migration.

## Governing docs

The ticket carries
`refs: ["docs/frd/frd-08-email-mailbox-and-background-processing.md"]` and
`docs_todo: true` — confirmed in `get_doc_gates FEAT-027`, which reports
`governing-doc` **satisfied** at `leave-backlog`.

**Meets — `docs/frd/frd-08-email-mailbox-and-background-processing.md`.**
§ Inbound mailbox identity (`:16-45`) requires the durable Pegasus mailbox
identity and the mailbox address to be kept as *separate, explicitly named*
identities and forbids one substituting for the other: step 4's DTO therefore
carries `mailboxId` **and** `mailboxAddress` as distinct fields, and step 3's
join is what supplies the second without conflating them. Steps 5–6 report
poll state without changing what retention means; no FRD text is modified by
this ticket.

> **New ADR** — ADR-0106 (Graph intake worker stays central: unattended
> execution, protected credentials), authored by [[FND-005]] (plan handle
> `DSK-00-05`).
> This plan is written to the decision as recorded in
> `docs/desktop/00-governance-and-workflow/README.md` § 3; if the ADR lands
> differently this plan is revised before implementation.

> **New ADR** — ADR-0103 (gateway; never direct database access from
> workstations), authored by [[FND-005]]. Same condition.

> **New ADR** — ADR-0107 (Box and DVLA/DVSA credentials stay behind the
> gateway; no long-lived provider secret in the package), authored by
> [[FND-005]]. Same condition. It is cited here for the step-8 no-credential
> assertion, which applies the same rule to the Graph credential.

`refs` names one FRD and no ADR, so the programme-level authorities that bind
today are tabulated for `kanmer-review` to check against the diff:

| Authority | Requirement | Met by |
| --- | --- | --- |
| Proposal § 12.1 | Graph polling stays central; the desktop shows ingestion status and failures **through the gateway** | Steps 4–7; step 12's empty `src/Pegasus.Worker` diff |
| Proposal § 13.10 | Integration health and failed-work review are parity capabilities | Steps 5–7 |
| Proposal § 16.2 | Provider failure states are distinguishable and carry when the data was obtained | Steps 5–6 (`asOfUtc`, `freshness`, `lastFailureCode`) |
| Proposal § 10.2 (via `endpoint-map.md` Conventions) | Reads return `version`-free bodies with a weak `ETag`; explicit routes, no generic dispatcher | Step 5 |
| L-01 | Gateway is `Pegasus.Web` evolved in place — route groups, no new deployment unit | Step 5 |
| L-02 | Evidence on the local Test/UAT stack, never an Azure test resource | Steps 8–10 |
| L-04 | Subagent, skills and MCP named on the ticket | § Routing below |
| `docs/current-architecture.md:86-90` | `terminal` / `transient` / `unknown` stay distinct; unknown outcomes remain unknown | Step 6 and its contract test |
| `docs/current-architecture.md:104` | `GET /Operations` has no approval controls, receipt ledger or Box request caller | § Out of scope in `files` |
| `docs/desktop/03-gateway-api-and-data/README.md` § 3 "Projection style" | Endpoints are thin argument-mappers over Core ports; no business rule in Web | Steps 5–6 |
| `docs/desktop/03-gateway-api-and-data/README.md:167` | Only the thirteen catalogued problem types; `correlationId` always present | Step 8 |
| `docs/desktop/07-integrations/README.md` § 7 (trap row) | "Poison-queue visibility lost behind a friendly status" | Step 7 |
| `docs/engineering.md` § Plan sizing | Diff estimate first; facts split from assumptions | This heading; `research` § Facts / Assumptions |
| `AGENTS.md` § Simplicity rails | One list per concept — one freshness policy, one label map | Steps 6–7 and the § Approach rejection |
| `AGENTS.md` § Repository task workflow step 4 | Simplification pass over this branch's own diff before the PR | Step 12 |

## Routing

Copied from the ticket body's `## Routing` block.

- **Subagent**: `pegasus-gateway-dev` — `.codex/agents/pegasus-gateway-dev.toml`
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `dotnet-webapi`
  (dotnet/skills `98f84851`, plugin `dotnet-aspnetcore`) →
  `microsoft-code-reference` (Microsoft Learn plugin) → `run-tests`
  (dotnet/skills `98f84851`, plugin `dotnet-test`)
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`,
  `set_ticket_doc`, `append_scratch`, `move_item`); Microsoft Learn
  (`microsoft_docs_search`, `microsoft_code_sample_search`)
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` →
  `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (call
  `get_doc_gates <id>` before every move; a move crosses at most one gated
  boundary)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement
  (`AGENTS.md` § Repository task workflow step 5)

## Steps

These refine the ticket body's twelve implementation steps in the same order and
with the same ownership.

1. **Orient and take.** Read the plan row `DSK-07-01`
   (`docs/desktop/07-integrations/README.md` § 5), that plan's § 2 Evidence base
   and § 4 first bullet, `docs/desktop/03-gateway-api-and-data/endpoint-map.md`
   § `Triage, Unidentified, Operations` (`:108-116`) with its Conventions header
   (`:11-27`), and `docs/frd/frd-08-email-mailbox-and-background-processing.md`
   § Inbound mailbox identity. Call `get_doc_gates FEAT-027`, then `take_ticket`
   with branch `task/dsk-07-01-intake-status-endpoints` and a worktree cut from
   `origin/dev`.
2. **Characterise the Operations page and record it.** Append to `research` the
   handler table already begun there, adding: which Core use case each handler
   calls, the `LoadedAtUtc` rule at `:41-45` / `:67`, and the
   `RequestOperationProjection` fields the Razor view renders. Do the same for
   `GetRetainedMailFreshness.Evaluate` (`RetainedMail.cs:680-711`). **Record the
   SHA read** — upstream PLAT-039 and the MAIL fixes arrive with [[FND-023]]
   (plan handle `DSK-01-10`).
3. **Confirm the read ports — there are four, not three.** `GetRequestOperations`
   (`RequestOperations.cs:72`), `GetEmailOperations` (`EmailOperations.cs:62`),
   `IRetainedMailQueries.ListPollHealthAsync` (`RetainedMail.cs:382`) and
   `ListMailboxesAsync` (`:379`). The fourth is required because `MailPollHealth`
   (`:360-364`) carries no mailbox address and step 4's DTO does; join on
   `MailboxId`. Record the exact type names in `files` (done) and the join in
   this plan (done).
4. **Add the DTOs to `src/Pegasus.Contracts`** *(created by [[FND-029]], plan
   handle `DSK-02-04`)*. `IntakeStatusResponse` carries `asOfUtc`, a
   `mailboxes` list of `MailboxIntakeStatus(mailboxId, mailboxAddress,
   isPolled, lastCompletedAtUtc, lastFailureCode, dueAtUtc, freshness)` and the
   poison and failure counts from step 7. `ExternalWorkResponse` carries
   `asOfUtc`, `limitReached` and rows of `(kind, caseReference, attemptCount,
   failureCode, failureReason, canRetry, lastActivityAtUtc)`. Every DTO is a
   plain record with no EF, ASP.NET or Core type — the architecture test from
   [[GWY-001]] (plan handle `DSK-03-01`) enforces it. `isPolled` is added
   beyond the body's list because `RetainedMailMailbox.IsPolled`
   (`RetainedMail.cs:341`) is the only way to tell a configured-but-unpolled
   mailbox from a failing one; without it the surface reports a lie.
5. **Register the two `GET` endpoints in the `/api/v1` operations route group**
   inside `src/Pegasus.Web`, behind `Features:DesktopGateway` ([[GWY-002]], plan
   handle `DSK-03-02`) and the `PerformCasework` filter ([[GWY-003]], plan
   handle `DSK-03-03`). Reads carry a weak `ETag` per the endpoint-map
   conventions and no `version` field. Take `asOfUtc` from `TimeProvider`
   **after** the last await returns, reproducing
   `Operations/Index.cshtml.cs:67`; a failed query returns a problem, never a
   body with a fresh timestamp and an empty list. Coordinate with [[GWY-013]]
   (plan handle `DSK-03-13`), which owns `GET /operations` in the same group:
   extend that registration, add no second group, and record the choice here.
6. **Map freshness through the Core policy, not through an `if`.** For each
   mailbox call `GetRetainedMailFreshness.Evaluate(new[] { health }, nowUtc)`
   (`RetainedMail.cs:680`) and render its `MailFreshnessState` with the exact
   strings `Mail/Index.cshtml.cs:253-258` already uses — `current`, `stale`,
   `unavailable`. Carry `lastFailureCode` verbatim from Core; do not translate
   it, and do not collapse `RequestOperationState.UnknownExternal`
   (`RequestOperations.cs:22`) or `EmailOperationState.Unknown`
   (`EmailOperations.cs:17`) into success — `docs/current-architecture.md:86-90`
   makes the three distinct and [[FEAT-045]] (plan handle `DSK-07-19`) fixes the
   wire vocabulary later.
7. **Report poison as its own named figure.** Count rows whose `failureCode`
   equals the `queue_poisoned` constant added in step 4 — the literal written by
   `EfIntakeWorkStore.MarkPoisonedAsync` (`:410`) and
   `EfExternalWorkStore.MarkPoisonedAsync` (`:442`, `:475`, `:506`, `:524`,
   `:532`). Add a test asserting the constant equals the store literal, so an
   upstream sync that renames it fails the build rather than silently
   zeroing the count. Note that `CompletePoisonReplay`
   (`EfExternalWorkStore.cs:435`, `:468`, `:499`) **completes** a poisoned
   message whose effect already landed, so those rows are not counted. Add no
   column and no table.
8. **Contract tests** in `tests/Pegasus.Api.ContractTests` *(created by
   [[TEST-001]], plan handle `DSK-08-01`)*: gate off → 404; unauthenticated →
   401; wrong right → 403 with `urn:pegasus:problem:not-authorized`; a healthy
   mailbox → `current`; a mailbox with a failure code and a future `dueAtUtc` →
   `unavailable`; a never-polled mailbox → `unavailable` with `isPolled` false;
   the poison count present as its own field; and an assertion that no response
   field contains a mailbox credential, Graph token, connection string or
   storage key. Enable `Features:DesktopGateway` explicitly in the positive
   tests, or a gated endpoint returns 404 and the test lies.
9. **LocalDB integration test** in `tests/Pegasus.IntegrationTests`, seeded with
   a failed external work item and a failed mailbox poll, following the fixture
   patterns in `OperationsWebTests.cs` (which already seeds
   `FailureCode: "queue_poisoned"` at `:345`) and `OperationsPersistenceTests.cs`.
   Expected: `canRetry` is true only where `RequestOperationProjection.CanRetry`
   (`:51`) / `EmailOperationProjection.CanRetry` (`:45`) are true for the same
   data, and `limitReached` surfaces rather than a second truncation.
10. **Build and run.** `dotnet build ./src/Pegasus.Web/Pegasus.Web.csproj -c Release`
    and the two test commands under Verification. Confirm the existing
    `OperationsWebTests` and `OperationsPersistenceTests` stay green — the Razor
    page is untouched.
11. **Endpoint map and capabilities.** Add the two rows to
    `docs/desktop/03-gateway-api-and-data/endpoint-map.md`
    § `Triage, Unidentified, Operations` so the map stays the single endpoint
    list, and add the `DSK` row to `docs/capabilities.md` — first confirming the
    `DSK` family exists (`grep -n 'DSK' docs/capabilities.md` returns nothing
    today; [[FND-011]], plan handle `DSK-00-11`, creates it).
12. **Simplification pass and PR.** Run the pass over this branch's own diff
    (`AGENTS.md` § Repository task workflow step 4), record it under a dated
    `## Simplification pass` heading below, then open the PR into `dev`.

## Verification

Evidence tier from the body: **5** — Web/API/MCP caller. Tier 5 obliges evidence
that the actual `/api/v1` routes reach Core with authentication, right checks,
exception translation and correlation ids observable; a registration or a green
build does not satisfy it.

- `dotnet test ./tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj --configuration Release`
  — expected: the gate, authorization, freshness, poison-count and
  no-credential facts pass. This output is the tier-5 evidence.
- `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --filter "Category!=Corpus&Category!=Browser"`
  — expected: the new seeded-failure facts pass and every existing
  `OperationsWebTests` / `OperationsPersistenceTests` fact stays green.
- `git diff --stat origin/dev -- src/Pegasus.Worker` — expected: **empty
  output**. This single command is the proof of the "no Worker change"
  acceptance criterion and belongs in the proof verbatim.
- `dotnet build ./src/Pegasus.Web/Pegasus.Web.csproj -c Release` — expected:
  success with no new warnings.

## Risks / open questions

- **A never-polled mailbox may have no `ApprovedInboxPollStates` row**
  (assumption A-07-01-1). Mitigation: build the list from `ListMailboxesAsync`
  and left-join poll health onto it, so a missing row renders `unavailable` with
  `isPolled` false rather than vanishing. Asserted at step 8.
- **Truncation could imply completeness.** `GetRequestOperations` bounds at 100
  (`:76`) and `GetEmailOperations` at 50 per direction (`:66`). Mitigation:
  surface `limitReached`; raising a Core bound is a different ticket.
- **The wire vocabulary is not settled.** [[FEAT-045]] (plan handle
  `DSK-07-19`) owns `terminal` / `transient` / `unknown` and the five provider
  problem types. This ticket carries the Core failure codes verbatim and defines
  no rival list. Answered by: [[FEAT-045]].
- **Two tickets can register the same route group.** [[GWY-013]] (plan handle
  `DSK-03-13`) owns `GET /operations`. Step 5 records which one landed first and
  extends it. Answered by: [[GWY-013]].
- **`docs/capabilities.md` has no `DSK` family yet.** Step 11 confirms
  [[FND-011]] (plan handle `DSK-00-11`) has created it; if not, contribute the
  row there rather than inventing a family here. A scope boundary with an owner.
- **`StaleAfter` is provisional.** `RetainedMail.cs:652-662` records fifteen
  minutes as open in `docs/open-decisions.md`. The endpoint reuses the constant
  and neither re-argues nor hard-codes it, so a later change to the constant
  changes the endpoint with it.

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over
this branch's own diff before the PR, recorded here under a dated heading._
