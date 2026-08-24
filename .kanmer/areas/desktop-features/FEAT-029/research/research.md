# Research — FEAT-029: what the mail workspace actually owns, and which of its rules a naive projection would break

## Question

Which Core owners do the two mail page models call, what exactly does each
handler verify before it mutates, where does "the folder-move provider is
unavailable" actually get decided, and which of the gateway's own conventions
(`operationKey` on every command, `pageSize ≤ 200`) do the mail Core use cases
refuse?

## Current behaviour

Read at fork `main` `191ddf33` on 2026-08-24. The implementer re-reads after the
latest upstream sync ([[FND-023]], plan handle `DSK-01-10`) and records the SHA,
because upstream MAIL-011/012 arrive with it.

| Handler | `path:line` | Core owner it calls |
| --- | --- | --- |
| List | `src/Pegasus.Web/Pages/Mail/Index.cshtml.cs:69` `OnGetAsync` | `ListRetainedMail.ExecuteAsync` (`:132`) **or** `SearchDeletedMail.ExecuteAsync` (`:122`), plus `ListMailboxesAsync` from whichever of the two owns the folder scope (`:114-116`), plus `GetRetainedMailFreshness.ExecuteAsync` (`:141`) |
| Preview | `…/Mail/Index.cshtml.cs:158` `OnGetPreviewAsync` | `GetRetainedMail.ExecuteAsync` (`:167`), projected to a `JsonResult` of nine fields (`:174-190`) |
| Detail | `src/Pegasus.Web/Pages/Mail/Message.cshtml.cs:157` `OnGetAsync` | `GetRetainedMail.ExecuteAsync(actor, id, searchTerm, …)` |
| Prepare link | `…/Mail/Message.cshtml.cs:199` `OnPostPrepareLinkCaseAsync` | `IGetCase` then `IAcquireCaseEditLease` (`:236`) |
| Prepare unlink | `…/Mail/Message.cshtml.cs:260` `OnPostPrepareUnlinkCaseAsync` | same pair, with the opposite association precondition (`:281-283`) |
| Confirm link | `…/Mail/Message.cshtml.cs:318` `OnPostLinkCaseAsync` | `ILinkIntake.ExecuteAsync` (`:355`) |
| Confirm unlink | `…/Mail/Message.cshtml.cs:383` `OnPostUnlinkCaseAsync` | `IReverseIntakeLink.ExecuteAsync` (`:420`) |
| Correct classification | `…/Mail/Message.cshtml.cs:448` `OnPostCorrectClassificationAsync` | `CorrectRetainedMailClassification.ExecuteAsync` (`:474`) |
| Move to recommended folder | `…/Mail/Message.cshtml.cs:511` `OnPostMoveToRecommendedFolderAsync` | `MoveRetainedMailFolder.ExecuteAsync` (`:524`) |

Parity-matrix rows: **`PAR-21`** (`Mail/Index.cshtml.cs` (428) — `OnGetAsync`,
`OnGetPreviewAsync`, status `inventoried`) and **`PAR-22`**
(`Mail/Message.cshtml.cs` (1,025) — all seven handlers, status `inventoried`),
`docs/desktop/01-inventory-and-parity/parity-matrix.md:66-67`. `PAR-22`'s own
note already records "folder move reserves `RetainedMailFolderMoves` (provider
unavailable by default)". The matrix holds `PAR-01`…`PAR-46`
(`grep -c '^| PAR-' …/parity-matrix.md` → 46).

## Findings

### Facts

- **There is a second non-Razor ingress over these exact Core owners already,
  and it is the precedent to copy.** `src/Pegasus.Web/Mcp/MailMcpTools.cs`
  (341 lines) exposes `pegasus_mail_list` (`:128`), `pegasus_mail_get` (`:189`)
  and `pegasus_mail_correct_classification` (`:239`) over `ListRetainedMail`,
  `GetRetainedMail` and `CorrectRetainedMailClassification`. It is the working
  proof of "MCP and API remain two ingresses over one Core"
  (`docs/desktop/03-gateway-api-and-data/README.md` § 3 Projection style). Its
  `Map(…)` projections (`:290`+) are the DTO shapes this ticket should mirror
  rather than invent.
- **The folder-move availability decision is already made in Core, and the
  ticket body names the wrong type for it.** Step 7 says the mover "resolves to
  `EmptyRetainedMailFolderMoveStore` (`RetainedMailFolderMove.cs:72`)". Measured:
  `EmptyRetainedMailFolderMoveStore` (`:70-83`) is only the **null-object
  default of an optional constructor parameter** on `GetRetainedMail`
  (`RetainedMail.cs:494`); the composition always registers the real
  `EfRetainedMailFolderMoveStore` (`src/Pegasus.Infrastructure/DependencyInjection.cs:87-88`).
  The actual availability signal is `IRetainedMailFolderMover.IsAvailable`
  (`RetainedMailFolderMove.cs:42`), whose production default is
  `UnavailableRetainedMailFolderMover.IsAvailable => false` (`:135`) registered
  by `TryAddSingleton` at `DependencyInjection.cs:85`. The body's intent is
  unambiguous; these documents use the real names.
- **Core already computes the capability flag the ticket asks for, so step 7
  must project it rather than compute a second one.**
  `RetainedMailFolderRecommendation.CanMove` (`RetainedMail.cs:115`) is set at
  `:613` to `folderMover?.IsAvailable == true && !isCurrentLocation`, and
  `RetainedMailDetail.SuggestedMove` (`:535-538`) is non-null only when
  `CanMove` is true **and** the latest move outcome is not `Uncertain`. A
  gateway-side boolean resolving `IRetainedMailFolderMover` itself would be a
  second availability authority over the one Core already owns.
- **"Unavailable" has five distinct operator sentences, not one boolean.**
  `RecommendFolderAsync` (`RetainedMail.cs:568-615`) returns `Unavailable(…)`
  for: no current classification decision (`:574-576`), the policy maps to no
  folder type (`:580-582`), the mailbox is not currently approved (`:589-592`),
  and the designated folder is not configured for this mailbox (`:596-602`);
  the fifth case is `CanMove: false` on an otherwise valid recommendation.
  A one-boolean DTO would collapse five reasons an operator needs.
- **No production writer exists — measured, not assumed.**
  `grep -rn 'GraphRetainedMailFolderMover' --include=*.cs src tests` returns the
  class itself (`src/Pegasus.Infrastructure/Email/GraphApprovedSources.cs:1077`)
  and two test constructions
  (`tests/Pegasus.IntegrationTests/ProductionGraphSourceTests.cs:26`, `:51`).
  `AddProductionApprovedMailboxResolver`
  (`src/Pegasus.Infrastructure/DependencyInjection.cs:602-623`, called from
  `src/Pegasus.Web/Program.cs:184`) registers `GraphDeletedMailSearchSource`
  (`:621`) and **never** an `IRetainedMailFolderMover`, so the `TryAddSingleton`
  fallback at `:85` always wins in production.
- **Deleted-Items search is a different Core owner returning a different
  record.** `SearchDeletedMail` (`src/Pegasus.Core/Intake/DeletedMailSearch.cs:54`)
  returns `DeletedMailSearchPage(Items, Page, PageSize, TotalCount, IsTruncated,
  State)` (`:40-51`) of `DeletedMailSearchItem` (`:11-22`) — **not**
  `RetainedMailPage`/`RetainedMailSummary`. `MaximumMessages = 100` (`:55`),
  term 1–200 characters (`:73-79`), `page` 1–10,000 (`:80-83`), `pageSize`
  1–100 (`:84-87`). It also has its own `ListMailboxesAsync` (`:57`), which the
  page switches to for the deleted scope (`Index.cshtml.cs:114-116`).
- **`DeletedMailSearchState` is an honesty signal that must survive the wire.**
  `UnavailableDeletedMailSearchSource` (`DeletedMailSearch.cs:106-124`) returns
  an **empty list** with `State = Unavailable`. An endpoint that returns only
  items would report "no deleted messages match" when the truth is "Deleted
  Items search is not composed here". `IsTruncated` carries the same weight for
  the 100-message cap.
- **The list and Deleted-Items scopes are mutually exclusive in the page and in
  Core.** `Index.cshtml.cs:87` returns `NotFound()` when a queue filter is
  combined with the deleted folder, and `ListRetainedMail` refuses a scope
  carrying **both** a `Destination` and a `DetailedClassification`
  (`RetainedMail.cs:417-421`). The `queue` query parameter maps to one or the
  other, never both.
- **Core caps `pageSize` at 100; the gateway convention says 200.**
  `ListRetainedMail.ExecuteAsync` throws `ArgumentOutOfRangeException` for
  `pageSize is < 1 or > 100` (`RetainedMail.cs:406-411`) and
  `SearchDeletedMail` for the same range (`DeletedMailSearch.cs:84-87`), while
  `docs/desktop/03-gateway-api-and-data/README.md` § 3 Paging says
  "`pageSize` ≤ 200". The mail endpoints must cap at **100** or a legal gateway
  request becomes a 500. The Razor page's own page size is `PageSize = 25`
  (`Index.cshtml.cs:23`).
- **`page` is bound as `pageNumber` in the Razor page for a Razor-only reason.**
  `Index.cshtml.cs:36-42` records it: "`page` is the reserved Razor Pages route
  key: an `asp-route-page` is overwritten by `asp-page`". That constraint does
  **not** apply to `/api/v1`, where the endpoint-map convention is plain `page`.
  Do not carry the Razor workaround onto the API.
- **`CorrectMailClassificationRequest` carries no `OperationKey`.**
  `RetainedMail.cs:172-176` is `(MessageId, ExpectedVersion, Category, Reason)`.
  Concurrency is by version alone: `CorrectRetainedMailClassification`
  (`:267`) throws `MailClassificationConcurrencyException` (`:195-196`, "The
  classification changed after this message was opened. Reload it before
  correcting it.") when `current.Version != request.ExpectedVersion` (`:305`).
  `ExpectedVersion` is an **`int`** and must be ≥ 1 (`:290`); `Reason` is 1–500
  characters (`:295`).
- **The MCP tool shows what an `operationKey` is *for* on that command.**
  `pegasus_mail_correct_classification` requires an `mcp:`-prefixed key
  (`MailMcpTools.cs:252`), and `:258-262` passes it to
  `AutomationMcpAuditor.RecordAsync` (`src/Pegasus.Web/Mcp/AutomationActorResolver.cs:117`)
  — the **audit ledger** — never to Core. So the gateway's "every command
  carries `operationKey`" convention is satisfiable here as an audit
  correlation id, and the endpoint-map "Idempotent?" column for this row must
  say version-based, not `yes (key)`.
- **`MailCategory` is a validated record, not an enum.**
  `src/Pegasus.Core/Intake/Classification/MailClassificationContracts.cs:100-140`
  — `Direction`, `ReceivedFamily`, `SentFamily`, `Subtype`, `IsReplyContext`,
  `OtherName` (≤ 200), `OtherReasoning` (≤ 1000), with `ValidateCanonical()`.
  The wire form is the **option key** parsed by
  `src/Pegasus.Web/Presentation/MailClassificationSelection.cs:13`, whose own
  remark says "The mail message page and the Automation MCP mail tools both
  consume this single list". A third parser would break that sentence.
- **The move command's `OperationKey` must be a GUID.**
  `MoveRetainedMailFolder.ExecuteAsync` (`RetainedMailFolderMove.cs:88`) does
  `Guid.TryParse(request.OperationKey, out var operationKey)` and throws
  otherwise (`:114-117`), then normalises it to `operationKey.ToString("D")`
  (`:126`). The gateway plan's `desk:<guid>` key format
  (`docs/desktop/03-gateway-api-and-data/README.md` § 3 Idempotency) would be
  **rejected** by this one command. Its other bounds: three `int` versions all
  ≥ 1 (`:104-108`), a non-empty `ExpectedRecommendationPolicyKey` (`:109-112`),
  and a `Reason` of 1–500 characters (`:118-121`).
- **The move outcome is three-valued and its third value is load-bearing.**
  `RetainedMailFolderMoveOutcome` is `Succeeded`, `Failed`, `Uncertain`
  (`RetainedMailFolderMove.cs:6-11`), and the page's sentences
  (`Message.cshtml.cs:536-541`) are: "Message moved to the recommended Outlook
  folder.", "The message was not moved. You can retry with a new
  confirmation.", and for `Uncertain` "The move result is uncertain. Retry this
  same confirmation to check its current location." The third instructs a
  **replay of the same key**, which is the opposite of the second. Collapsing
  them loses the operator's next action.
- **Link and unlink are a two-phase prepare→confirm with four version checks
  each.** Prepare-link (`Message.cshtml.cs:222-231`) requires
  `binding.Version == expectedIntakeVersion`, `binding.CurrentCaseId is null`,
  `selectedCase.Workflow.Version == expectedCaseVersion`,
  `Archive is null` and `!CaseLifecycleRules.IsTerminal(state)`; prepare-unlink
  (`:281-295`) requires the same version pair but `CurrentCaseId == caseId`.
  Both then acquire a case edit lease (`:236`, `:292`) and stash the prepared
  tuple. Confirm (`:340-352`, `:405-417`) re-resolves the binding and calls
  `RequirePreparedAssociation` with the same seven values before touching Core.
  Any endpoint that accepts a confirm without a matching prepare is a different
  concurrency model, not a projection.
- **The confirm handlers require confirmation material and Core requires a
  reason.** `RequireAssociationConfirmation(operationKey, editLeaseToken,
  Reason)` runs first (`:341`, `:406`), and the parameter is spelled `Reason`
  with a capital R in both signatures (`:325`, `:390`) — a Razor model-binding
  detail that must not leak into a JSON DTO.
- **The preview handler is not cheaper than the detail.**
  `OnGetPreviewAsync` calls the same `GetRetainedMail.ExecuteAsync` (`:167`) and
  then projects nine fields (`:174-190`), including
  `MessageModel.DecisionLabel` / `ClassificationLabel`. There is no separate
  lightweight Core port, so `GET /mail/{id}/preview` costs what `GET /mail/{id}`
  costs. Its value is inertness, not cheapness.
- **`GetRetainedMail` has two overloads and the search term matters.**
  `RetainedMail.cs:497` (no term) and `:503` (with term). The message page passes
  `SearchTerm` (`Message.cshtml.cs` `ReloadAsync`) so match highlighting works;
  the preview handler does not. The endpoint must expose the term on detail or
  the desktop loses match context the web has.
- **The whole workspace is read-only in the mailbox.** `Index.cshtml.cs:12-16`:
  "A viewer and nothing else… opening a message here does not mark it read in
  the mailbox." FRD-08 `:198` says the same of preview: "previewing never
  changes classification, association, read state, Case state, or source
  custody."
- **`docs/current-architecture.md:104` is the single densest constraint line in
  this ticket** and states, in the repository's own words: Deleted Items search
  "uses GET-only Graph reads against each exact approved mailbox and its
  resolved `deleteditems` folder; MIME is parsed once by the same intake reader
  and is neither retained nor backfilled"; the move handler "accepts only the
  internal message id, current classification/recommendation/mailbox versions,
  operation key and required reason"; and "The provider is unavailable by
  default and the control is absent in that composition… no production writer,
  Graph permission, deployment or live mailbox mutation is active." It also
  records the runtime-role grants: Web holds `SELECT` alone on the retained-mail
  tables, `SELECT, INSERT, UPDATE` on the move-operation table, `SELECT, UPDATE`
  on `IntakeMailClassificationDecisions` and `SELECT, INSERT` on
  `IntakeMailClassificationHistory` (`UPDATE, DELETE` denied).
- **The existing test evidence is large and specific.**
  `tests/Pegasus.IntegrationTests/MailWorkspaceWebTests.cs` is **2,045 lines**,
  and `PAR-21`/`PAR-22` also name `RetainedMailPersistenceTests.cs` and
  `Browser/MailWorkspaceBrowserTests.cs`. The ticket's step 8 says "mirroring
  the scenarios already proven" — the scenario catalogue is in that file, not to
  be reinvented.
- **The projects this ticket writes into do not exist yet.** No
  `src/Pegasus.Contracts`, no `tests/Pegasus.Api.ContractTests`, no `openapi/`,
  no `eng/`.

### Assumptions

- **A-07-03-1 — [[GWY-012]] (plan handle `DSK-03-12`) has not yet landed the
  mail route group when this ticket starts.** Confirmed by: `get_item GWY-012`
  and `get_doc_gates GWY-012` at plan step 2, before any code. Breaks if: it
  has landed — then this ticket **extends** that group with the availability
  and Deleted-Items rules and adds no second group, which is exactly what the
  body's step 2 already instructs. Either way the answer is recorded in `plan`
  before code.
- **A-07-03-2 — the `pageSize ≤ 100` cap is acceptable to the desktop Inbox
  screen.** The Razor page uses 25 (`Index.cshtml.cs:23`) and Core refuses more
  than 100. Confirmed by: [[FEAT-010]] (plan handle `DSK-05-10`) and the Inbox
  screen spec, which specify a paged table, not an infinite list. Breaks if: a
  screen needs more than 100 rows at once — that is a Core bound change and a
  different ticket.
- **A-07-03-3 — no test or local composition registers a real
  `IRetainedMailFolderMover`, so the "move present" contract test must supply
  one itself.** Confirmed by: the `grep` in Facts, which finds the Graph mover
  constructed only inside `ProductionGraphSourceTests.cs`. Breaks if: a fake
  mover is registered somewhere in the Web test host — then the "move absent"
  test would silently be testing the wrong composition, so **both**
  compositions are asserted explicitly (step 8) rather than one being assumed
  to be the default.
- **A-07-03-4 — the desktop can generate a bare GUID operation key for the move
  command while using `desk:<guid>` everywhere else.** Required by
  `RetainedMailFolderMove.cs:114-117`. Confirmed by: a contract test sending a
  `desk:`-prefixed key to the move endpoint and asserting a `validation`
  problem, and a bare GUID succeeding. Breaks if: [[GWY-001]] (plan handle
  `DSK-03-01`) mandates the prefix in the DTO type — then the exception is
  recorded there, not worked around here.
- **A-07-03-5 — `RetainedMailSummary`, `RetainedMailDetail` and
  `DeletedMailSearchItem` contain no field that is a provider secret or a raw
  provider payload.** Confirmed by: step 10's field-by-field review and the
  no-credential contract assertion. Breaks if: a field carries a Graph token,
  mailbox secret or raw JSON — it is then omitted from the DTO and raised, not
  passed through (ADR-0107).
- **A-07-03-6 — upstream MAIL-011/012 do not change these handler signatures.**
  Confirmed by: re-reading the two page models after the sync and recording the
  SHA (plan step 3). Breaks if: they do — the handler table in this document is
  re-derived before the DTOs are frozen.

## Execution placement

The six-question test from `docs/desktop/00-governance-and-workflow/README.md`
§ 3, answered. This ticket places **six read** and **six command**
responsibilities over the mail workspace.

| Question | Answer | Evidence, and where a "yes" lands |
| --- | --- | --- |
| Shared authority — must several users see and update the same state? | **yes** | Classification decisions, case associations and folder-move records are one shared state several staff act on. The prepare→confirm pair exists for exactly that: `Message.cshtml.cs:222-231` refuses a stale `binding.Version` or `Workflow.Version`, and `MailClassificationConcurrencyException` (`RetainedMail.cs:195`) exists because two operators can correct the same classification. **Lands in the gateway** — `Pegasus.Web` evolved in place (L-01), no new deployment unit. |
| Unattended execution — must it run with every desktop closed? | **yes** | The retained store these endpoints read is filled by `InboxPollFunction` (`src/Pegasus.Worker/MailboxFunctions.cs:8-15`) on a timer, per mailbox, with its own lease and cursor. **Lands in the existing `src/Pegasus.Worker`** (ADR-0106) — and this ticket writes no Worker code; the Guardrails forbid touching it. |
| Protected credentials — long-lived secret that must not sit on workstations? | **yes** | Two: the Graph credential behind Deleted-Items search, composed **in the Web host** (`AddProductionApprovedMailboxResolver`, `src/Pegasus.Infrastructure/DependencyInjection.cs:602-623`, called at `src/Pegasus.Web/Program.cs:184`), and the mailbox credential behind the poll. **Lands behind the gateway and Worker** (ADR-0106, ADR-0107); the desktop holds none and receives none — hence step 10's no-credential assertion. |
| Public callback — must an external service call a stable public endpoint? | **no** | Graph is polled on a timer and read GET-only on demand. No change-notification subscription exists and the ticket's step 5 forbids adding one; doing so would need a new accepted decision under proposal § 4. |
| Central enforcement — revocation, permissions, audit, invariant independent of the client? | **yes** | Four enforcements no client can be trusted with: `StaffAuthorization.Require(actor, PerformCasework)` inside every mail use case (`RetainedMail.cs:398`, `:509`, `DeletedMailSearch.cs:61`, `:71`, `RetainedMailFolderMove.cs:94`); the version checks above; the case edit lease acquired at `Message.cshtml.cs:236`; and the classification history that is append-only by grant (`docs/current-architecture.md:104` — `UPDATE, DELETE` denied on `IntakeMailClassificationHistory`). **Lands in the gateway.** |
| Measured operational advantage — measured evidence central is materially better? | **no** | No measurement in this repository supports rendering the mail workspace centrally. The relevant measured constraint points the other way: `MailWorkspaceWebTests.cs` proves the behaviour locally on the L-02 stack with no Azure resource. |

**Conclusion.** Four "yes" answers, and every one lands somewhere that already
exists: the reads and commands in the gateway (L-01), the polling in the Worker
(ADR-0106), the Graph credentials behind both (ADR-0107). List rendering,
preview presentation, scope memory and the confirmation dialogs belong to the
desktop ([[FEAT-010]], plan handle `DSK-05-10`). **No new Azure resource and no
Azure write.**

## Implications

- **Step 7 is a projection, not a computation.** The detail DTO carries
  `RetainedMailFolderRecommendation` whole — `folderType`, `policyKey`,
  `policyVersion`, `reason`, `mailboxVersion`, `canMove` — plus `suggestedMove`
  and `latestFolderMove`. Core already decided all of it
  (`RetainedMail.cs:531-539`, `:604-614`); the endpoint must not resolve
  `IRetainedMailFolderMover` and must not reduce five distinct unavailability
  reasons to one boolean.
- **Two of the gateway's own conventions bend for mail, and both must be
  recorded in the endpoint map rather than silently broken.** `pageSize` caps at
  **100**, not 200, because Core throws above it; and the classification command
  is **version-idempotent, not key-idempotent**, because
  `CorrectMailClassificationRequest` has no `OperationKey` — the key is an audit
  correlation id, exactly as `MailMcpTools.cs:258-262` already uses it.
- **The move command needs a bare GUID operation key.** `desk:<guid>` fails
  `Guid.TryParse` at `RetainedMailFolderMove.cs:114`. This is a contract detail
  the desktop must know before it sends, so it belongs in the DTO's
  documentation and in a contract test, not in a runtime surprise.
- **Deleted Items is a second read shape, not a filter.** A different Core use
  case, a different page record, a different item record and its own mailbox
  list. Modelling it as `folder=deleted` on the retained list would force one
  DTO to carry two shapes; the endpoint map's own third row
  (`~GET /api/v1/mail/deleted?search` in `PAR-21`) already anticipates a
  separate route.
- **`state` and `isTruncated` are the honesty fields.** An empty deleted-search
  result with `State = Unavailable` must not render as "no matches", and a
  truncated 100-message result must not render as "all matches".
- **Prepare→confirm must survive the projection.** The gateway cannot accept a
  confirm without the prepared tuple the page stashes; the four endpoints
  (`link-case/prepare`, `link-case`, `unlink-case/prepare`, `unlink-case`)
  already in the endpoint map are the right shape, and the lease token returned
  by prepare is what joins them.
- **`MailMcpTools.cs` is the template.** Same Core owners, same option-key
  vocabulary through `MailClassificationSelection`, same auditor use of the
  operation key. Following it keeps "one Core, two ingresses" true and keeps the
  desktop from acquiring a third classification vocabulary.
- **The move outcome's `Uncertain` value must reach the desktop.** It is the
  only outcome whose correct next action is "replay this same key", and
  [[FEAT-010]] cannot render that instruction from a boolean.

## Open questions

None that block. Four points that could look like questions have named owners:

- Whether [[GWY-012]] (plan handle `DSK-03-12`) has already created the mail
  route group is settled at plan step 2 by reading the board, and the body
  already prescribes both branches. A scope boundary, not a question.
- Whether the `desk:<guid>` key format is mandated in the DTO type is
  [[GWY-001]] (plan handle `DSK-03-01`)'s contract; the move command's GUID
  requirement is recorded there if so.
- The `terminal` / `transient` / `unknown` wire vocabulary for the provider
  failure states these endpoints surface is [[FEAT-045]] (plan handle
  `DSK-07-19`)'s. This ticket carries the Core outcome enums verbatim.
- Whether `OperatorLabels` and `MailClassificationSelection` have moved to
  `Pegasus.Contracts` is [[GWY-016]] (plan handle `DSK-03-16`) and [[FEAT-023]]
  (plan handle `DSK-05-23`)'s work. Until they have, the endpoint consumes them
  from `src/Pegasus.Web/Presentation/` and writes no second list.
