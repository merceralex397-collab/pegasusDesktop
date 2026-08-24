# Files — FEAT-029

Surveyed 2026-08-24 against fork `main` `191ddf33`. Paths marked *(created by …)*
do not exist today: `ls src` returns only `Pegasus.Core`, `Pegasus.Infrastructure`,
`Pegasus.Web`, `Pegasus.Worker`; `ls tests` only `Pegasus.ArchitectureTests`,
`Pegasus.Core.Tests`, `Pegasus.IntegrationTests`; there is no `openapi/` and no
`eng/`.

## Where the change lands

| Path | Why |
|---|---|
| `src/Pegasus.Contracts/` *(created by [[FND-029]], plan handle `DSK-02-04`; conventions by [[GWY-001]], plan handle `DSK-03-01`)* | The mail DTOs: a list page and summary, a **separate** deleted-search page and item (different Core shape entirely), a detail carrying the folder recommendation whole, a preview record, and six command requests. Plain records with no EF, ASP.NET or Core types. `MailCategory` is a validated record, not an enum — the wire form is the option key from `MailClassificationSelection`. |
| `src/Pegasus.Web/` — the `/api/v1` **mail** route group | Nine endpoints (three reads, one deleted-search read, four association commands, plus classification and move) registered behind `Features:DesktopGateway` ([[GWY-002]], plan handle `DSK-03-02`) and the `PerformCasework` filter ([[GWY-003]], plan handle `DSK-03-03`). **Coordinate with [[GWY-012]] (plan handle `DSK-03-12`), which owns this exact group in the gateway plan** — extend it if it landed first, create it and hand it over if not. The choice is recorded in `plan` before code. |
| `tests/Pegasus.Api.ContractTests/` *(created by [[TEST-001]], plan handle `DSK-08-01`; template from [[TEST-002]], plan handle `DSK-08-02`)* | Paging and freshness, preview inertness, prepare→confirm with version verification, classification correction and its version conflict, move with reason, and **both** folder-mover compositions asserted explicitly. Plus the gate-off 404, 401, 403 and the no-credential assertion. |
| `tests/Pegasus.IntegrationTests/` | The step-9 parity facts: endpoint and Razor handler produce the same Core effect for the same input. `MailWorkspaceWebTests.cs` (2,045) is the scenario catalogue to mirror and must stay green. |
| `docs/desktop/03-gateway-api-and-data/endpoint-map.md` § `Mail workspace` (`:96-107`) | Row corrections, not additions: the `pageSize` cap is **100** not 200; the classification row's "Idempotent?" is version-based, not `yes (key)`; the detail row's `Returns` gains the folder recommendation and capability fields; a separate deleted-search row per `PAR-21`'s `~GET /api/v1/mail/deleted?search`. |
| `docs/frd/frd-08-email-mailbox-and-background-processing.md` (338 lines) | The desktop behaviour clause — behaviour, not mechanism — per the ticket's Documentation changes. |

## Context files

| Path | What it tells the implementer |
|---|---|
| `src/Pegasus.Web/Mcp/MailMcpTools.cs` (341) | **The template.** A second non-Razor ingress over these exact Core owners already exists: `pegasus_mail_list` (`:128`), `pegasus_mail_get` (`:189`), `pegasus_mail_correct_classification` (`:239`), with `Map(…)` projections from `:290`. It is the working proof of "MCP and API remain two ingresses over one Core". Read its DTO shapes before inventing any. |
| `src/Pegasus.Web/Mcp/MailMcpTools.cs:252`, `:258-262` | What an `operationKey` is *for* on the classification command: it is required with an `mcp:` prefix and passed to `AutomationMcpAuditor.RecordAsync` (`src/Pegasus.Web/Mcp/AutomationActorResolver.cs:117`) — the **audit ledger** — never to Core, which has no such field. The API's `desk:` key follows the same precedent. |
| `src/Pegasus.Web/Presentation/MailClassificationSelection.cs:13-40` | The one correction vocabulary, with its own remark: "The mail message page and the Automation MCP mail tools both consume this single list, so a corrected taxonomy entry appears — or disappears — for both callers at once." Option keys look like `received:<Family>:<subtype>`, `sent:<Family>`, `other-received`, `other-sent`. A third parser breaks that sentence. [[GWY-016]] (plan handle `DSK-03-16`) relocates it to `Pegasus.Contracts`. |
| `src/Pegasus.Core/Intake/RetainedMail.cs:109-118` | `RetainedMailFolderRecommendation(FolderType, PolicyKey, PolicyVersion, Reason, MailboxVersion, CanMove)` with `IsAvailable => FolderType is not null`. **Core already computes the capability the ticket's step 7 asks for.** Project this record whole; do not resolve `IRetainedMailFolderMover` in Web. |
| `src/Pegasus.Core/Intake/RetainedMail.cs:568-615` | `RecommendFolderAsync` — where `CanMove` is set (`:613`, `folderMover?.IsAvailable == true && !isCurrentLocation`) and where "unavailable" acquires **five distinct operator sentences**: no classification decision (`:574-576`), policy maps to no folder (`:580-582`), mailbox not approved (`:589-592`), folder not configured (`:596-602`), and `CanMove: false`. One boolean would collapse all five. |
| `src/Pegasus.Core/Intake/RetainedMail.cs:531-539` | `SuggestedMove` is non-null only when `CanMove` is true **and** the latest move outcome is not `Uncertain`. The advisory the design authority renders; the endpoint carries it rather than re-deriving it. |
| `src/Pegasus.Core/Intake/RetainedMailFolderMove.cs:40-52`, `:130-143` | `IRetainedMailFolderMover.IsAvailable` (`:42`) is the real availability signal — **not** `EmptyRetainedMailFolderMoveStore`, which the ticket body names. `UnavailableRetainedMailFolderMover.IsAvailable => false` (`:135`) is the production default. |
| `src/Pegasus.Infrastructure/DependencyInjection.cs:82-92` | The composition that settles it: `TryAddSingleton<IRetainedMailFolderMover, UnavailableRetainedMailFolderMover>()` (`:85`) always wins because `AddProductionApprovedMailboxResolver` (`:602-623`, called from `src/Pegasus.Web/Program.cs:184`) registers `GraphDeletedMailSearchSource` (`:621`) and no mover. The store, by contrast, is **always** the real `EfRetainedMailFolderMoveStore` (`:87-88`). |
| `src/Pegasus.Core/Intake/RetainedMailFolderMove.cs:88-127` | `MoveRetainedMailFolder.ExecuteAsync` bounds: three `int` versions all ≥ 1 (`:104-108`), non-empty `ExpectedRecommendationPolicyKey` (`:109-112`), **`OperationKey` must parse as a `Guid`** (`:114-117`, normalised to `"D"` at `:126`), `Reason` 1–500 (`:118-121`). The `desk:<guid>` gateway key format is rejected here — the only command on the board where that is true. |
| `src/Pegasus.Core/Intake/RetainedMailFolderMove.cs:6-11`, `:22-32` | `RetainedMailFolderMoveOutcome` is `Succeeded` / `Failed` / `Uncertain`, and `RetainedMailFolderMoveResult` carries `IsReplay`, `OperationKey`, `FailureReason` and the four echoed expected versions. `Uncertain` is the one outcome whose correct next action is "replay this same key" — the opposite of `Failed`'s "retry with a new confirmation". |
| `src/Pegasus.Web/Pages/Mail/Message.cshtml.cs:536-541` | The three approved operator sentences for those outcomes, verbatim. Reuse them; do not write a fourth. |
| `src/Pegasus.Core/Intake/RetainedMail.cs:172-176`, `:195-196`, `:267-320` | `CorrectMailClassificationRequest(MessageId, ExpectedVersion, Category, Reason)` — **no `OperationKey`**. `ExpectedVersion` is an `int` ≥ 1 (`:290`), `Reason` 1–500 (`:295`), and a stale version throws `MailClassificationConcurrencyException` ("The classification changed after this message was opened. Reload it before correcting it."). Idempotency here is version-based, not key-based. |
| `src/Pegasus.Core/Intake/Classification/MailClassificationContracts.cs:100-140` | `MailCategory` is a validated record with `Direction`, `ReceivedFamily`, `SentFamily`, `Subtype`, `IsReplyContext`, `OtherName` (≤ 200), `OtherReasoning` (≤ 1000) and `ValidateCanonical()`. Never serialise it as a bare enum. |
| `src/Pegasus.Core/Intake/RetainedMail.cs:391-452` | `ListRetainedMail.ExecuteAsync` bounds: `page` 1–10,000 (`:400`), **`pageSize` 1–100** (`:406`) — the gateway convention says 200, so mail must cap at 100 or a legal request becomes a 500 — a defined folder scope (`:412`), and `Destination` and `DetailedClassification` **mutually exclusive** (`:417-421`). Search term 1–200 (`:455-467`), mailbox identity ≤ 100 (`:441-447`). |
| `src/Pegasus.Core/Intake/DeletedMailSearch.cs:11-51`, `:54-104` | A **different Core owner returning different records**: `DeletedMailSearchPage(Items, Page, PageSize, TotalCount, IsTruncated, State)` of `DeletedMailSearchItem`, `MaximumMessages = 100` (`:55`), its own `ListMailboxesAsync` (`:57`). Not a filter on the retained list. |
| `src/Pegasus.Core/Intake/DeletedMailSearch.cs:106-124` | `UnavailableDeletedMailSearchSource` returns an **empty list with `State = Unavailable`**. An endpoint that returns only items reports "no matches" when the truth is "not composed here". `state` and `isTruncated` are the honesty fields. |
| `src/Pegasus.Web/Pages/Mail/Index.cshtml.cs:12-16`, `:23`, `:25-29`, `:36-42` | Four rules in the code's own words: the workspace is "A viewer and nothing else… opening a message here does not mark it read in the mailbox"; `PageSize = 25`; "a fresh visit resets to the default all-mailboxes view, so a TempData or cookie memory of the last filter would be a defect"; and `pageNumber` is a **Razor-only** workaround because `page` is a reserved route key — do not carry it onto `/api/v1`. |
| `src/Pegasus.Web/Pages/Mail/Index.cshtml.cs:76-141` | The exact read composition: folder parse → queue parse (with `NotFound` when a queue filter meets the deleted scope, `:87`) → mailbox trim → page clamp → search validation → **the mailbox list comes from whichever use case owns the folder** (`:114-116`) → list or deleted search → freshness last (`:141`). |
| `src/Pegasus.Web/Pages/Mail/Index.cshtml.cs:158-190` | `OnGetPreviewAsync` calls the **same** `GetRetainedMail` the detail uses (`:167`) and projects nine fields. There is no cheaper Core port: preview's value is inertness, not cost. |
| `src/Pegasus.Web/Pages/Mail/Message.cshtml.cs:199-258`, `:260-317` | The prepare phase, with four checks each: prepare-link requires `binding.Version == expectedIntakeVersion`, `CurrentCaseId is null`, `Workflow.Version == expectedCaseVersion`, `Archive is null`, `!CaseLifecycleRules.IsTerminal(state)`; prepare-unlink requires `CurrentCaseId == caseId` instead. Both then acquire a case edit lease (`:236`, `:292`). |
| `src/Pegasus.Web/Pages/Mail/Message.cshtml.cs:318-382`, `:383-447` | The confirm phase: `RequireAssociationConfirmation(operationKey, editLeaseToken, Reason)` then `RequirePreparedAssociation(…)` over seven values, **before** `ILinkIntake` / `IReverseIntakeLink`. An endpoint that accepts a confirm without a matching prepare is a different concurrency model, not a projection. Note the parameter is spelled `Reason` with a capital R (`:325`, `:390`) — a Razor binding detail that must not leak into a JSON DTO. |
| `src/Pegasus.Web/Pages/Mail/Message.cshtml.cs:499` | The approved consequence sentence for unlink lives beside the handler; the endpoint map records it as "Unlinking this email cancels case &lt;ref&gt;". It is contract text, carried on the wire, not desktop copy. |
| `docs/current-architecture.md:104` | The densest constraint line in this ticket: Deleted Items is "GET-only Graph reads… neither retained nor backfilled"; the move handler "accepts only the internal message id, current classification/recommendation/mailbox versions, operation key and required reason"; "The provider is unavailable by default and the control is absent in that composition… no production writer, Graph permission, deployment or live mailbox mutation is active." It also records the runtime-role grants — Web holds `SELECT` alone on retained-mail tables and `UPDATE, DELETE` are **denied** on `IntakeMailClassificationHistory`. |
| `docs/desktop/03-gateway-api-and-data/README.md:167` | The thirteen `urn:pegasus:problem:<slug>` values. `version-conflict`, `lease-conflict`, `lease-expired`, `validation`, `not-authorized`, `not-found` and `provider-unavailable` are the ones this ticket uses; add nothing. |
| `docs/desktop/03-gateway-api-and-data/README.md` § 3 (Paging, Idempotency) | The two conventions mail bends: `pageSize ≤ 200` (mail caps at 100) and `desk:<guid>` operation keys (the move command needs a bare GUID). Both are recorded in the endpoint map rather than silently broken. |
| `docs/desktop/06-ui-design/screen-specs.md:248-269` | The Inbox screen spec [[FEAT-010]] (plan handle `DSK-05-10`) builds: four tabs, the Decision card with *Correct classification* and *Move to folder* shown "only when populated and available", the unlink consequence sentence in its dialog, and deleted-items search as a scope option. The contract must supply exactly what those affordances need. |
| `docs/frd/frd-08-email-mailbox-and-background-processing.md:198`, `:205-213`, `:243-247` | Preview "never changes classification, association, read state, Case state, or source custody"; "Sent mail and read-only Deleted Items search remain separate folder scopes"; "Classification, linking and folder-move actions are available only from opened messages" and UI-10 "provides no bulk classification, linking or folder-move action". No batch endpoint. |
| `tests/Pegasus.IntegrationTests/MailWorkspaceWebTests.cs` (2,045) | The scenario catalogue step 8 mirrors. It is the largest single test file touching this surface; read it before writing a new harness. |
| `tests/Pegasus.IntegrationTests/ProductionGraphSourceTests.cs:26`, `:51` | The **only** places a real `GraphRetainedMailFolderMover` is constructed. Proof that no composition registers one, and the pattern for supplying a mover in the "move present" contract test. |

## Ripple effects

- **OpenAPI and the generated client.** The mail DTOs change
  `openapi/pegasus-v1.json` (the committed snapshot from [[GWY-004]], plan
  handle `DSK-03-04`) and the Kiota client generated by
  `eng/api/Generate-ApiClient.ps1` ([[GWY-005]], plan handle `DSK-03-05`). CI
  fails if regeneration changes the tree, so regenerate and commit in the same
  PR.
- **[[GWY-012]] (plan handle `DSK-03-12`) owns this route group in the gateway
  plan.** This is a registration-site collision, not merely a dependency: the
  body's own step 2 says the two "must land as one contract, not two". Resolve
  it before writing code.
- **[[FEAT-010]] (plan handle `DSK-05-10`) is the screen** and binds these field
  names, the five folder-unavailability reasons, the three move outcomes and the
  unlink consequence sentence. Freeze the names when the contract tests go green.
- **`src/Pegasus.Web/Presentation/MailClassificationSelection.cs` and
  `OperatorLabels.cs` move to `Pegasus.Contracts`** under [[GWY-016]] (plan
  handle `DSK-03-16`) and [[FEAT-023]] (plan handle `DSK-05-23`). Consume them;
  do not fork them, and expect the namespace to change under you.
- **`src/Pegasus.Web/Mcp/MailMcpTools.cs` is the sibling ingress.** If this
  ticket discovers a projection field the MCP tool also needs, raise it there
  rather than letting the two drift.
- **[[FEAT-045]] (plan handle `DSK-07-19`) will fix the provider failure
  vocabulary** these endpoints surface. Do not pre-empt it with a rival enum.
- **[[GWY-018]] (plan handle `DSK-03-18`) re-reviews every `/api/v1` command**
  for contract and authorization gaps and will read what this ticket writes.
- **`MailWorkspaceWebTests.cs`, `RetainedMailPersistenceTests.cs` and
  `Browser/MailWorkspaceBrowserTests.cs` stay green** — no Razor page, no Core
  and no Infrastructure file changes.
- **Documentation** — the `Mail workspace` endpoint-map rows and the FRD-08
  behaviour clause.

## Out of scope

- **`src/Pegasus.Infrastructure/Email/GraphApprovedSources.cs`** (1,125 lines) —
  named explicitly in the ticket's Guardrails. No Graph adapter change, no new
  Graph permission, no change-notification subscription.
- **`src/Pegasus.Worker`** — every file. The poller stays central (ADR-0106).
- **The Razor mail pages** — `Pages/Mail/Index.cshtml.cs` and
  `Pages/Mail/Message.cshtml.cs`. They stay deployable until `PAR-21` and
  `PAR-22` reach `UAT passed`.
- **`src/Pegasus.Core`** — no new use case, no new projection field. If the DTO
  needs a field Core does not expose, stop and raise it.
- **Registering a real `IRetainedMailFolderMover` anywhere outside a test.**
  That would activate a live mailbox mutation with no Graph permission,
  deployment or production writer behind it
  (`docs/current-architecture.md:104`).
- **MAIL-12/13/17/19** — compose, mailbox mutation beyond the existing folder
  move, idempotent report send, automatic chasers. Out of conversion scope
  (proposal § 13.11); only the seam is built.
- **Any bulk or batch endpoint** over classification, linking or folder move —
  FRD-08 `:247` forbids it (UI-10).
- **A new table of any kind**, and therefore any `Grant*` migration
  (`scripts/Test-MigrationGrants.ps1`, PLAT-035).
- **Any Azure write.**
