# Research — FEAT-037: the outbound command seam and the case communications read

## Question

Can the desktop's outbound actions be served by **one** gateway command seam layered over the
send/reconcile use cases that already exist behind `Pages/Cases/Assessment/Index.cshtml.cs`, and
what must the case communications read carry so that a `Queries`-classified e-mail is
distinguishable from ordinary correspondence without inventing a query lifecycle?

## Current behaviour

The web application's only outbound action today is the assessment **send**, with a **reconcile**
companion, both on the assessment page model:

| Web surface | `path:line` | Core owner |
| --- | --- | --- |
| `POST /Cases/Assessment/Index?handler=Send` | `src/Pegasus.Web/Pages/Cases/Assessment/Index.cshtml.cs:583` (`OnPostSendAsync`) | send command behind the assessment/workflow use cases |
| `POST …?handler=Reconcile` | `src/Pegasus.Web/Pages/Cases/Assessment/Index.cshtml.cs:628` (`OnPostReconcileAsync`) | reconcile command; guards `requestId == Guid.Empty` at `:644` |
| operation-key validation for both | `src/Pegasus.Web/Pages/Cases/Assessment/Index.cshtml.cs:738` (`IsOperationKeyValid`), called at `:598` and `:644` | — |

Parity rows that cover this:

- **`PAR-15`** (§13.9 Assessment and reporting, FRD-11/FRD-06) — `Cases/Assessment/Index.cshtml.cs`
  (740 lines), naming `OnPostSendAsync` among its handlers
  (`docs/desktop/01-inventory-and-parity/parity-matrix.md:60`). This is the row that covers the
  send half of this ticket.
- **`PAR-21`** and **`PAR-22`** (§13.8 Communications, FRD-08) — `Mail/Index.cshtml.cs` (428) and
  `Mail/Message.cshtml.cs` (1,025), the mail-workspace surfaces that own classification correction
  and case link/unlink (`parity-matrix.md:66-67`).
- **`PAR-08`** (§13.3 Case lifecycle, FRD-01) — `Cases/Details.cshtml.cs` (654), the case workspace
  whose sections the desktop Communications tab replaces (`parity-matrix.md:53`).

**No parity row covers a case-scoped communications *read* as its own surface**, because there is
no such route today: the case's linked mail is a section of `Cases/Details` and its classification
lives on the mail-workspace side. The endpoint map already anticipates the read as part of the
sectioned case detail — `docs/desktop/03-gateway-api-and-data/endpoint-map.md:52` lists
`GET /cases/{id}/communications` among the case sections, sourced from `IRetainedMailQueries`
(case association). This ticket gives that section its classification field.

The matrix holds **46** `PAR-` rows (`grep -c '^| PAR-' docs/desktop/01-inventory-and-parity/parity-matrix.md`
→ `46`), all keyed to page models under `src/Pegasus.Web/Pages/**`.

## Findings

### Facts

Verified by reading the repository on 2026-08-24 at fork `main`.

- **The outbound state vocabulary already exists in Core, with five values, and `Unknown` is one
  of them.** `src/Pegasus.Core/Operations/EmailOperations.cs:12-18` defines
  `EmailOperationState { Pending, Succeeded, Failed, Unknown }`; `:6-10` defines
  `EmailOperationDirection { Received, Sent }`; `EmailOperationProjection` (from `:20`) carries
  `OperationId`, `Direction`, `State`, `MailboxIdentity`, `LastActivityAtUtc`, the intake/triage/case
  ids, `CaseReference`, `PrincipalCode`, `FailureCode`, `RetryMailboxId`, `RetryExpectedDueAtUtc`
  and `SourceLength`, with a derived `CanRetry` when a retry mailbox and due time are both present.
  Core has four states, not five — the contract's `draft` has **no** Core counterpart, because a
  draft is a client-side state before any command is issued.
  - Consequence: the `src/Pegasus.Contracts` vocabulary is `draft | queued | sent | failed |
    unknown` and the map is `draft` → (no Core state; client-only, never returned by the gateway),
    `Pending` → `queued`, `Succeeded` → `sent`, `Failed` → `failed`, `Unknown` → `unknown`. Write
    the map once and state the `draft` asymmetry in the contract's own XML documentation.
- **Only retained Sent evidence proves a send, and the record says so in its own summary.**
  `src/Pegasus.Core/Workflow/CaseWorkflowContracts.cs:82-84` carries the comment "Exact retained
  approved-mailbox Sent evidence. A caller cannot substitute a draft, manual assertion, queue
  result, prepared text, or a report file for this evidence"; `ApprovedMailboxReportSentEvidence`
  is declared at `:85` with sixteen members — `EvidenceId`, `MailboxIdentity`,
  `SentFolderIdentity`, `ImmutableItemIdentity`, `InternetMessageIdentity`, `ConversationIdentity`,
  `ReplyChainIdentity`, `SourceOccurrenceIdentity`, `SourceSha256`, `MimeSha256`, `SentAtUtc`,
  `DiscoveredAtUtc`, `DiscoveredBy`, `LinkedAtUtc`, `LinkedBy`. (The ticket body cites `:82-95`;
  the comment begins at `:82` and the record itself spans `:85-100`.)
- **The evidence is produced by the Worker, not by any client.**
  `src/Pegasus.Worker/EmailEvidenceFunctions.cs:16` (`SentEvidencePollFunction`) and `:53`
  (`DueWorkSweepFunction`) are the unattended producers; `src/Pegasus.Core/Workflow/PollSentEvidence.cs`
  holds the use case. `docs/current-architecture.md:453` records that sent-evidence polling is
  configuration-driven for one mailbox.
- **`ReportApprovalEvidence` explicitly does not claim a send.**
  `src/Pegasus.Core/Workflow/CaseWorkflowContracts.cs:62-64` — "A human approval of one immutable
  report artifact. It does not claim the report was sent." The record is at `:65-70`;
  `ReportApprovalSubmission` at `:76-79`. `CaseWorkflowState` (from `:107`) carries
  `ReportApproval` and `ReportSentEvidence` as two separate single-slot members.
- **The canonical classification exists and the case-scoped read does not project it.**
  `src/Pegasus.Core/Intake/Classification/MailOperationalDestinationPolicy.cs:7-15` defines
  `MailOperationalDestination { ReceivingWork, Queries, DetailedClassification, Other,
  Unidentified, Triage }` — with the summary "Unidentified is an abstention, never a category";
  `:17-22` defines `MailOperationalDestinationResult(Destination, MailCategory? Classification,
  string PolicyKey, int PolicyVersion, string Reason)`.
  `src/Pegasus.Core/Intake/RetainedMail.cs:366-381` declares `IRetainedMailQueries` with
  `ListAsync`, `GetAsync`, `ListMailboxesAsync` and `ListPollHealthAsync` — none of which projects
  a destination or category alongside case association.
  - Consequence: the classification has to be joined into the communications projection here, and
    `PolicyKey` / `PolicyVersion` must be dropped at the boundary.
- **`terminal` / `transient` / `unknown` is an already-enforced repository rule.**
  `docs/current-architecture.md:86-90`: "External clients and catch paths distinguish `terminal`,
  `transient`, and `unknown`; terminal outcomes stop retries, unknown outcomes remain unknown, and
  metrics count successful effects rather than attempts."
- **The screen spec already names the four chips and the two AutomationIds.**
  `docs/desktop/06-ui-design/screen-specs.md:362-369` — explicit draft / queued / sent / failed
  chips, exact Outlook Sent evidence with separate discovery, link and sent times, correlation to
  case and actor; AutomationIds `Case.Communications.Table` and `Case.Communications.Send`. It says
  nothing about classification today — which is the documentation change this ticket owes.
- **The endpoint map already reserves both routes.** `endpoint-map.md:52` lists
  `GET /cases/{id}/communications` as a case section fed by `IRetainedMailQueries`;
  `endpoint-map.md:79` lists `POST /cases/{id}/assessment/send`, `/reconcile` mapped to
  `OnPostSendAsync` / `OnPostReconcileAsync`, `PerformCasework`, idempotent `yes (key)`, tier 7.
  `endpoint-map.md:16` and `:22` state the convention: every command body carries `operationKey`,
  and replay of the same key returns the original result.
- **The FRD-08 classification rows exist.**
  `docs/frd/frd-08-email-mailbox-and-background-processing.md:120-135` is the classification table
  whose `Queries` destination this read must carry.
- **Neither test project this ticket writes into exists yet.** `ls tests/` returns
  `Pegasus.ArchitectureTests`, `Pegasus.Core.Tests`, `Pegasus.IntegrationTests` only.
  `tests/Pegasus.Api.ContractTests` is created by [[TEST-001]] (plan handle `DSK-08-01`), and
  `src/Pegasus.Contracts` by [[FND-029]] (plan handle `DSK-02-04`), confirmed by
  [[GWY-001]] (plan handle `DSK-03-01`). `ls src/` returns `Pegasus.Core`, `Pegasus.Infrastructure`,
  `Pegasus.Web`, `Pegasus.Worker`.
- **`tests/Pegasus.IntegrationTests/SentEvidencePollPersistenceTests.cs` is the pattern named by the
  body for step 11.** It sits beside the other persistence suites in that project (the browser and
  corpus suites are trait-filtered out of the default lane by
  `--filter "Category!=Corpus&Category!=Browser"`, `.github/workflows/ci.yml:230-234`).

### Assumptions

- **`A-07-11-1` — the send use case behind `OnPostSendAsync` can be called with the same
  `expectedVersion` / `editLeaseToken` / `operationKey` triple the endpoint map's Conventions
  header prescribes, without a new Core overload.** Confirmed by reading the use case's signature
  during step 3 of the plan. If wrong, the ticket needs a Core adapter and the diff estimate grows
  by roughly one file and forty lines; it does **not** authorise changing Core semantics
  (Guardrails forbid it).
- **`A-07-11-2` — `IRetainedMailQueries`'s persistence adapter can join the classification row for
  a case-linked message without a new table.** `MailOperationalDestinationQuery`
  (`MailOperationalDestinationPolicy.cs:26+`) exists precisely so the persistence adapter can
  translate destinations against the classification row rather than owning a second table, which is
  strong evidence the join is available. If wrong, a new table would require a runtime-role
  `Grant*` migration (`scripts/Test-MigrationGrants.ps1`) — the trap the ticket's Guardrails name —
  and the honest response is to stop and raise it, not to add the table quietly.
- **`A-07-11-3` — `Pending` is the state a queued-but-unconfirmed send reports.** Read from the
  enum's ordering and from the reconcile path's existence rather than from a state-transition test.
  Settled during step 3 by reading the send use case's return; if `Pending` is instead used for
  "accepted but not dispatched", the contract map gains one row and nothing else changes.

## Execution placement

The six-question cloud-justification test from
`docs/desktop/00-governance-and-workflow/README.md` § 3, answered for **the outbound command and
its communications read**:

| Question | Answer | Evidence |
| --- | --- | --- |
| Shared authority — must several users see and update the same state? | **yes** | The case's communications history and its send state are case state read by every operator on that case; `CaseWorkflowState` (`CaseWorkflowContracts.cs:107-108`) carries the approval and sent-evidence slots on the case, not per client. Lands in the gateway (`Pegasus.Web`, L-01). |
| Unattended execution — must it run with every desktop closed? | **yes** | The provider outcome is confirmed by `SentEvidencePollFunction` (`src/Pegasus.Worker/EmailEvidenceFunctions.cs:16`) and `DueWorkSweepFunction` (`:53`), which run with every desktop closed. Lands in the **existing Worker Function App** — already there, no new host, no Azure write. |
| Protected credentials — long-lived secret that must not sit on workstations? | **yes** | The mail service credential is the Graph credential ADR-0106 keeps central; the desktop never composes a Graph client. Lands in the **existing Web Container App and Worker** with their existing Key Vault references. |
| Public callback — must an external service call a stable public endpoint? | **no** | Nothing in this seam is called back by a provider; sent evidence is discovered by polling, not by a webhook (`docs/current-architecture.md:453`). |
| Central enforcement — revocation, permissions, audit, invariant independent of the client? | **yes** | Single-execution-by-key, `PerformCasework` authorisation and the provider-message-id audit are exactly the invariants a client must not be trusted with; the refused-client-claim rule (step 8) is the same point. Lands in the gateway. |
| Measured operational advantage — measured evidence central is materially better? | **no** | No measurement exists, and none is claimed. The placement is decided by the four "yes" answers above, not by this one. |

Four "yes" answers, and **none of them means "in Azure" beyond the two hosts that already run**:
the gateway is `Pegasus.Web` evolved in place (L-01) and the unattended half is the Worker that
already polls. The desktop keeps only what a client may hold — composing and confirming the
command, and rendering the states.

## Implications

1. **The seam is thin, and the ticket's value is the vocabulary and the refusals, not new
   capability.** Both underlying use cases exist; what does not exist is one place where the five
   states are named, one refusal for a client-asserted send, and one honest `unknown`.
2. **`draft` is client-only and must be documented as such**, or the first reader will look for a
   Core state that is not there and invent one.
3. **The communications read is the half with real work in it.** `IRetainedMailQueries` projects
   case association and no classification, so the projection has to be widened — and widened
   *carefully*, because `MailOperationalDestinationResult` carries `PolicyKey` and `PolicyVersion`
   that must not reach an operator surface (the upstream PLAT-015 breach the Guardrails name; note
   the collision recorded there — board `PLAT-015` is plan handle `DSK-10-15` and is a different
   ticket from upstream PLAT-015, which has no fork ticket).
4. **The report-send half of this seam is consumed, not extended, by
   [[FEAT-042]] (plan handle `DSK-07-16`)** — that ticket registers a finalised PDF and shows
   approved and sent separately; it does not get its own send vocabulary.
5. **Everything this ticket must not build is already decided elsewhere.** Compose, mailbox
   mutation, chasers (upstream MAIL-12/13/17/19) stay upstream backlog under proposal § 13.11;
   raising, replying to and resolving a query stay with **upstream CASE-002** — which is *not*
   board [[CASE-002]] (that is upstream CASE-022, public upload links) and was not imported. The
   operator problem this ticket does solve is **upstream CASE-009**'s: making Query classification
   visible on the case surface. Upstream CASE-009 was not imported either.

## Open questions

- None. The classification field, the state vocabulary, the refusal rules and the scope boundary
  are all settled by the ticket body, the endpoint map and FRD-08. The three assumptions above are
  resolved by reading during plan steps 3 and 9, not by asking anyone; if `A-07-11-2` fails the
  correct action is to stop and raise a new question then, not to pre-open one now.
