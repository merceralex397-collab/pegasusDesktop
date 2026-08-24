# Plan — FEAT-037: Outbound command pattern — desktop confirms, gateway authorises and executes with an idempotency key

**Diff estimate: ~11 files, ~640 lines.**

Derived from the files document: 4 new contract files (~150 lines), 2 endpoints in the existing
`/api/v1` case group (~170), 1 persistence projection widening (~60), 1 contract-test file
(~180 covering nine facts), 1 integration-test file (~50), 3 documentation edits (~30). No
migration, no Worker change, no new project.

## Approach

Layer **one** command seam over the two use cases that already exist behind
`OnPostSendAsync` (`src/Pegasus.Web/Pages/Cases/Assessment/Index.cshtml.cs:583`) and
`OnPostReconcileAsync` (`:628`), rather than modelling a general outbound-message pipeline. The
rejected alternative was a generic "outbound operation" resource with a queue, a dispatcher and a
provider abstraction — the shape MAIL-12/13/17 would eventually want. It is rejected because those
capabilities are out of conversion scope (proposal § 13.11, and this area's § 7 scope-creep trap),
because building a dispatcher with exactly one caller is the over-engineering `AGENTS.md`
§ Simplicity rails names, and because a pipeline invites exactly the defect the ticket forbids:
an optimistic `sent` written by the dispatcher instead of by retained Sent evidence. The seam is
therefore two routes, one five-value vocabulary defined once in `src/Pegasus.Contracts`, and a set
of refusals. The communications read is widened in the same ticket because a state vocabulary
without the classification beside it leaves upstream CASE-009's operator problem intact.

## Governing docs

The ticket's `refs` are `docs/frd/frd-11-reports-correspondence-and-reviewed-proposals.md` and
`docs/frd/frd-08-email-mailbox-and-background-processing.md`.

| Ref | Requirement | Meets |
| --- | --- | --- |
| FRD-11 (correspondence and reviewed proposals) | The outbound command seam's behaviour — what a send is, and that approval is not a send | Steps 5–8; documentation change adds the seam clause as behaviour, not mechanism |
| FRD-08 § classification rows (`:120-135`) | The `Queries` destination and what counts as one | Step 9 projects the destination and category the FRD defines; this plan decides nothing about classification itself |

The ticket also carries **`docs_todo: true`**, so no conversion ADR governs it yet:

> **New ADR** — ADR-0106 (Graph intake worker stays central; the mail service credential stays
> central), authored by [[FND-006]] (plan handle `DSK-00-06`).
> This plan is written to the decision as recorded in
> `docs/desktop/00-governance-and-workflow/README.md` § 3 (the ADR-0106 row) and in
> `docs/desktop/07-integrations/README.md` § 3; if the ADR lands differently this plan is revised
> before implementation.

`refs` carries no ADR, so the programme-level authorities that bind today:

| Authority | Requirement | Met by |
| --- | --- | --- |
| Proposal § 12.4 | Desktop creates and confirms; gateway authorises and executes; service credential stays central; duplicate sends prevented by an idempotency key; provider message id and status audited | Steps 4–6, 8 |
| Proposal § 13.8 | Draft / queued / sent / failed distinction explicit | Step 4, step 10's contract facts |
| Proposal § 16.1 | "Uncertain" is a real operation state | Step 7 |
| Proposal § 13.11 | Post-alpha capabilities are not smuggled into parity | Step 2's recorded boundary; Out of scope in `files` |
| L-01 (index, Locked decisions) | The gateway is `Pegasus.Web` evolved in place | Steps 5 and 9 add routes to the existing `/api/v1` groups |
| L-02 | Evidence is produced on the local stack | Verification |
| ADR-0106 (as recorded in 00 § 3) | Mail service credential central; no desktop Graph credential | Step 8, and the empty-Worker-diff check |
| `docs/current-architecture.md:86-90` | `terminal` / `transient` / `unknown`; unknown remains unknown | Step 7 |
| `AGENTS.md` § Simplicity rails ("One list per concept") | A state vocabulary lives in exactly one place | Step 4 |
| `docs/desktop/03-gateway-api-and-data/endpoint-map.md:16,22` | Every command body carries `operationKey`; replay returns the original result | Steps 5–6 |
| `HZN-001` / `board-conventions.md` § Upstream ids versus board ids | Upstream ids are never written bare | Step 2's recorded boundary and this plan throughout |

## Routing

Copied from the ticket body's `## Routing` block.

- **Subagent**: `pegasus-gateway-dev` — `.codex/agents/pegasus-gateway-dev.toml`
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `dotnet-webapi` (dotnet/skills `98f84851`)
  → `code-testing-agent` (dotnet/skills `98f84851`) → `run-tests`
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`,
  `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search` for idempotency-key
  patterns in ASP.NET Core minimal APIs)
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute`
  → `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (call `get_doc_gates <id>` before every
  move; a move crosses at most one gated boundary)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md`
  § Repository task workflow step 5)

## Steps

These refine the ticket body's twelve implementation steps in the same order and with the same
ownership; nothing is renumbered.

1. **Orient and take.** Read the plan row (`docs/desktop/07-integrations/README.md` § 5,
   `DSK-07-11`), that area's outbound-mail evidence paragraph and its scope-creep trap row, FRD-11
   in full, and FRD-08 `:120-135`. Call `get_doc_gates FEAT-037`, then `take_ticket` on branch
   `task/dsk-07-11-outbound-command-seam` with a worktree at
   `../pegasus-worktrees/dsk-07-11-outbound-command-seam` from `origin/dev`.
2. **Record the boundary in this plan before any code.** Write, under a dated heading here: this
   ticket implements the command seam, the state vocabulary and the classification field on the
   communications read — and *not* compose, mailbox mutation, automatic chasers, a new send channel
   or any query lifecycle. Cite upstream MAIL-12/13/17/19 as upstream backlog and upstream CASE-002
   (not imported; board [[CASE-002]] is upstream CASE-022 and unrelated) as the owner of the query
   lifecycle, per `docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md`.
3. **Read the two handlers and record the field list.** `Index.cshtml.cs:583-660`, plus the Core
   send and reconcile use cases behind them. Record in `files` (append) the required fields, the
   `IsOperationKeyValid` contract (`:738`) and what reconcile does when the provider result is
   unknown (`:644` guards an empty `requestId` in the same expression as the key check). This step
   settles assumptions `A-07-11-1` and `A-07-11-3`.
4. **Define the vocabulary once.** Add `OutboundOperationState` to `src/Pegasus.Contracts` with
   `draft`, `queued`, `sent`, `failed`, `unknown` and an XML-documented map from
   `EmailOperationState` (`src/Pegasus.Core/Operations/EmailOperations.cs:12-18`):
   `Pending`→`queued`, `Succeeded`→`sent`, `Failed`→`failed`, `Unknown`→`unknown`, and `draft`
   **client-only, never returned by the gateway** — Core has four states, not five. Do not add a
   second enum in the desktop.
5. **Implement `POST /api/v1/cases/{caseId}/assessment/send`** over the existing send use case,
   taking `expectedVersion`, `editLeaseToken` and `operationKey`, validating the key *first* in the
   order `Index.cshtml.cs:598` establishes, and returning the resulting state plus, where the
   provider has answered, the audited provider message identifier. Authorisation is the per-group
   `StaffAccessRight` filter from [[GWY-003]] (plan handle `DSK-03-03`) — `PerformCasework` per
   `endpoint-map.md:79` — never a client claim.
6. **Guarantee single execution by key.** A replay of the same `operationKey` returns the original
   result and performs no second provider effect. Reuse the existing operation-key mechanics; do
   **not** add an idempotency table (a new table needs a runtime-role `Grant*` migration checked by
   `scripts/Test-MigrationGrants.ps1`). Prove it with a test that replays after success and asserts
   exactly one audit row and one provider identifier.
7. **Represent `unknown` honestly.** An outbound command whose provider outcome is not yet known
   returns `unknown` with the reconcile path named in the response — never an optimistic `sent`.
   This is `docs/current-architecture.md:86-90` applied at the boundary and proposal § 16.1's
   "uncertain" state.
8. **Keep sent evidence exact.** The request contract has no field by which a client can assert a
   send. Only `ApprovedMailboxReportSentEvidence`
   (`src/Pegasus.Core/Workflow/CaseWorkflowContracts.cs:85`, summary at `:82-84`) proves one, and
   it is produced by `SentEvidencePollFunction` (`src/Pegasus.Worker/EmailEvidenceFunctions.cs:16`).
   Add a test that a client-supplied "sent" claim is refused.
9. **Add `GET /api/v1/cases/{caseId}/communications`** returning outbound and inbound history with
   the five states, the discovery / link / sent times, the correlating actor, **and each linked
   e-mail's canonical classification** — `MailOperationalDestination` and `MailCategory?` from
   `MailOperationalDestinationPolicy.cs:7-22`, projected through the shared vocabulary map — so a
   `Queries`-destination e-mail is distinguishable. Widen the `IRetainedMailQueries` case-scoped
   projection (`RetainedMail.cs:366-381`) by **joining** the existing classification row, following
   the precedent of `MailOperationalDestinationQuery`; if a new table appears to be needed, stop
   (assumption `A-07-11-2` has failed). Do **not** expose `PolicyKey` or `PolicyVersion`.
10. **Contract tests** in `tests/Pegasus.Api.ContractTests` — nine facts: success; replay with the
    same key; missing or malformed key → `validation`; unauthorised actor → `not-authorized`; stale
    `expectedVersion` → `version-conflict`; a client-asserted send → refused; `unknown` rendered
    distinctly from `sent`; a `Queries`-classified linked e-mail returned with its classification
    and distinguishable from an ordinary linked one; and no response body carrying a policy key or
    version. Match the refusal vocabulary of `src/Pegasus.Web/Mcp/AutomationMcpErrors.cs:18-70` as
    ported by [[GWY-002]] rather than inventing new codes.
11. **Integration test** following `tests/Pegasus.IntegrationTests/SentEvidencePollPersistenceTests.cs`:
    after a send whose evidence has not yet arrived the state is `unknown`; after the evidence poll
    runs, the same operation reports `sent` with the provider identifier audited. Nothing in that
    path is driven by the desktop.
12. **Documentation, simplification pass, PR.** Update `endpoint-map.md` (`:52` communications row
    with the classification field, `:79` send row with the returned state), FRD-11's seam clause,
    and `screen-specs.md` `:362-369` with the classification sentence for [[DUI-013]] (plan handle
    `DSK-06-13`). Regenerate `openapi/pegasus-v1.json` and the Kiota client per [[GWY-004]] /
    [[GWY-005]]. Run the simplification pass over this branch's diff, record it under a dated
    `## Simplification pass` heading below, and open the PR into `dev`.

## Verification

Evidence tier from the body: **Tier 5 — Web/API/MCP caller**
(`docs/engineering.md` § Required evidence tiers item 5: actual routes reach Core; authentication,
antiforgery, validation, scope, idempotency, exception translation and the action-history actor are
observable). `proof` is the captured output of:

- `dotnet test ./tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj --configuration Release`
  — expected: replay, validation, authorization, refused-client-claim, distinct-`unknown` and
  classification facts pass. The nine assertion names are the evidence, not the summary line.
- `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --filter "Category!=Corpus&Category!=Browser"`
  — expected: the unknown-then-sent evidence fact passes and the existing sent-evidence tests stay
  green (the guard that the projection widening did not disturb the poll).
- `git diff --stat origin/dev -- src/Pegasus.Worker` — expected: **empty output**. This is the
  observable form of ADR-0106's "no desktop Graph credential, no Worker change".
- `git diff --exit-code openapi/pegasus-v1.json` after regeneration — expected clean, proving the
  committed snapshot matches the generated document.

Behaviour to observe on the local stack (L-02): a send issued twice with one key produces one
provider effect and one audit row; a `Queries`-classified linked e-mail is visibly different in
the payload from an ordinary linked e-mail.

## Risks / open questions

- **Risk — the projection widening needs new schema.** Mitigation: the join precedent
  (`MailOperationalDestinationQuery`, "does not own another classification-to-destination table")
  is checked first in step 9; if it does not hold, stop rather than adding a table, because a new
  table drags a runtime-role `Grant*` migration (`scripts/Test-MigrationGrants.ps1`, PLAT-035) that
  this ticket's scope does not carry.
- **Risk — the taxonomy collides with [[FEAT-045]] (plan handle `DSK-07-19`).** Mitigation:
  whichever lands first owns the slugs; the second maps onto them. Two vocabularies is the failure.
- **Risk — a reviewer reads `draft` as a Core state.** Mitigation: the XML documentation on the
  contract enum says client-only in as many words (step 4).
- **Risk — scope creep into a mail composer.** Mitigation: step 2 records the boundary in this
  document before code exists, and `files` § Out of scope carries it into review.
- **Scope boundary, not an open question** — the query lifecycle (raise / reply / resolve) belongs
  to **upstream CASE-002**, which was not imported; board [[CASE-002]] is upstream CASE-022 and is
  a different ticket. Nothing here waits on it.
- **Scope boundary, not an open question** — report finalise and send is [[FEAT-042]] (plan handle
  `DSK-07-16`), which consumes this seam.
- **No open question is opened.** The ticket body instructs none, and every remaining unknown is
  settled by reading during steps 3 and 9.

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over this branch's own
diff before the PR, recorded here under a dated heading._
