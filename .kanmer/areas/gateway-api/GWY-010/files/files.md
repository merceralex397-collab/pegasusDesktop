# Files — GWY-010: Intake (received items) endpoints

Paths marked **(created by …)** do not exist on disk today (`ls src` returns `Pegasus.Core`,
`Pegasus.Infrastructure`, `Pegasus.Web`, `Pegasus.Worker` and nothing else). Confirm the named
ticket has merged before starting.

## Where the change lands

| Path | New/edit | What happens here |
| --- | --- | --- |
| `src/Pegasus.Contracts/Intake/IntakeResponses.cs` | **new** (project created by [[GWY-001]], plan handle `DSK-03-01`) | `ReceivedItemDto` projecting `IntakeReceipt` (`src/Pegasus.Core/Intake/IntakeContracts.cs:366-436`) — receipt, evidence, review fields, instruction draft, missing fields, assets, OCR candidates, `version`, allocation state, and the four **derived** members projected rather than recomputed (`CurrentCaseId`, `AssociationWasStaffDecision`, `UnlinkCancelsCase`, `CurrentCaseReference`). Plus `ReceivedItemSummaryDto` over `IntakeReceiptSummary` (`:511-521`) and `ReceivedItemPageDto` over `IntakeListPage` (`:744-752`) |
| `src/Pegasus.Contracts/Intake/IntakeCommands.cs` | **new** (same project) | Nine command DTOs: `retry-allocation`, `block`, `reevaluate`, `correct-draft`, `dismiss-suggestion`, `register-image-intake` (each with `operationKey`, receipt `expectedVersion`, and `reason` where Core requires it), and the three case-association commands, which carry the receipt version **and** the case `expectedVersion` **and** `editLeaseToken` |
| `src/Pegasus.Web/Api/IntakeEndpoints.cs` | **new** (folder created by [[GWY-002]], plan handle `DSK-03-02`) | A `received` sub-group with the `PerformCasework` filter from [[GWY-003]] (plan handle `DSK-03-03`), carrying two reads, nine commands and three byte routes — fourteen named routes |
| the `/api/v1` problem-details mapper | **edit** (created by [[GWY-002]]) | An `IntakeArtifactIntegrityException` arm, so the byte routes return a problem document where `Source.cshtml.cs:43-48` returns 409 `text/plain` today |
| `tests/Pegasus.IntegrationTests/DesktopGatewayIntakeTests.cs` | **new** | Seven-case matrix per command, paging bounds, detail `version`/`ETag`, and six byte-safety facts per byte route |
| `openapi/pegasus-v1.json` | **edit** (created by [[GWY-004]], plan handle `DSK-03-04`) | **The freeze.** Fourteen paths and the intake vocabulary. Not committed until [[INTK-001]], [[INTK-004]] and [[INTK-006]] are each recorded as landed or explicitly deferred |
| `src/Pegasus.Desktop.Infrastructure/Api/Generated/**` | **edit** (created by [[FND-031]], plan handle `DSK-02-06`; generator by [[GWY-005]], plan handle `DSK-03-05`) | Kiota output regenerated and committed; CI fails if regeneration is not a no-op |

## Context files

| Path | What it tells you |
| --- | --- |
| `src/Pegasus.Core/Intake/IntakeQueryUseCases.cs:10-40` | That Core, not the endpoint, refuses a bad page: `Page is < 1 or > 10_000` (`:17-22`), **`PageSize is < 1 or > 100`** (`:23-28`, so the board's global 200 does not apply here), and an undefined `Decision` (`:29-34`). All three throw `ArgumentOutOfRangeException`, which becomes a 500 unless the endpoint maps it to a `validation` problem. Also `StaffAuthorization.Require(query.Actor, PerformCasework)` at `:16` — the endpoint filter is a fail-fast boundary, never the rule |
| `src/Pegasus.Core/Intake/IntakeContracts.cs:738-742` | `ListIntakeQuery` has **one** filter, `IntakeDecision? Decision` — no `queue`, no `state`. The endpoint-map's `?page&pageSize&queue&state` cannot be honoured as written without touching Core, which the guardrail forbids. Read this before designing the query string |
| `src/Pegasus.Core/Intake/IntakeContracts.cs:366-436` | The whole detail payload, including `Version` at `:391` (so `version` and the weak `ETag` need no new Core member) and four **derived** members with comments telling you not to re-derive them: `CurrentCaseId` `:407`, `AssociationWasStaffDecision` `:417`, `UnlinkCancelsCase` `:429` ("Derived here beside the rest of the association rules so no surface works it out again from raw fields (INTK-029)"), `CurrentCaseReference` `:432` |
| `src/Pegasus.Core/Intake/IntakeContracts.cs:77-86` | `IntakeDecision`, seven members — the vocabulary step 11 freezes, and the enum the three scattered code tables are supposed to collapse onto |
| `src/Pegasus.Infrastructure/Persistence/EfIntakeReceiptStore.cs:1241-1252` | Copy 1 of the persisted decision-code table: seven codes, **fail-closed** (`_ => throw UnknownCode("decision", value)`). This is the complete one |
| `src/Pegasus.Infrastructure/Persistence/EfOperationsStore.cs:563-569` | Copy 2: four codes, **fail-open** (`_ => EmailOperationState.Unknown`), silently omitting `blocked_intake` and `image_intake_registered`. Together with copy 1 this is the disagreement [[INTK-001]] resolves, and the reason step 3 must not add a fourth |
| `src/Pegasus.Web/Mcp/IntakeMcpTools.cs:62`, `:82-87`, `:190-195` | Copy 3: six codes (all but `image_intake_registered`), plus the sentence "processing decision and allocation state kept separate" and a documented MCP page cap of **50** — a third cap number, binding on MCP and not on this endpoint |
| `src/Pegasus.Web/Pages/Intake/Details.cshtml.cs` (613 lines) | The ten handlers to project, at `:95`, `:111`, `:157`, `:178`, `:192`, `:240`, `:274`, `:310`, `:513`, `:535` — each one's Core command, the version it expects, and where `reason` is required. **Do not edit it**; the guardrail forbids `src/Pegasus.Web/Pages/Intake/**` |
| `src/Pegasus.Web/Pages/Intake/Source.cshtml.cs` (78 lines) | What a byte handler does today: `nosniff` at `:30`, `File(..., "application/octet-stream", SafeFileName(...))` at `:31-34`, and — the part the endpoint-map does not mention — a `IntakeArtifactIntegrityException` catch at `:40-49` returning **409 with a `text/plain` body**. That is SHA-256 validation surfacing, and the API must return a problem document instead. `SafeFileName` at `:52-68` **sanitises**; the API's rule refuses |
| `src/Pegasus.Web/Pages/Intake/Asset.cshtml.cs:39-53` | The defence-in-depth media-type gate — "this endpoint accepts any asset id, so it gates on the parsed type", returning `NotFound()` for a non-`image/*` asset — plus `Cache-Control: private, no-store` and an `inline` `Content-Disposition`. Carry all of it across; dropping the type gate turns an asset route into an arbitrary-byte route |
| `src/Pegasus.Web/Mcp/AutomationMcpErrors.cs:127-140` | `RequireFileName`, the rule the ticket asks for: reject empty, over 255 characters, containing path components, or `.`/`..`. Note it **refuses** where the page **cleans** — a deliberate behaviour change with its own fact |
| `src/Pegasus.Core/Intake/IntakeContracts.cs:7-75` | `IntakeEnvelopeLimits` — 10 MiB single file (`:13`), 750 MiB mailbox envelope (`:33`, with the comment recording the 16.69 MB instruction rejected on 2026-08-05), 20-file batch (`:42`). Read-only here; [[GWY-011]] (plan handle `DSK-03-11`) owns the upload side |
| `src/Pegasus.Core/Intake/DurableIntake.cs:1106` | `LinkIntake`, the port behind `POST /received/{id}/link-case` (the body cites `:1109`, inside the same declaration) |
| `src/Pegasus.Core/Intake/IntakeAllocation.cs:205-208` | `AllocateIntake`'s constructor, behind `retry-allocation` |
| `tests/Pegasus.IntegrationTests/IntakeWebTestSupport.cs` (866 lines) | The shared intake `WebApplicationFactory` support — the arrange path your new test file reuses rather than rebuilding |
| `tests/Pegasus.IntegrationTests/MultiFormatIntakeWebTests.cs` (1,429 lines) | Real multi-format receipts to arrange against, and the existing behaviour that must stay green |
| `tests/Pegasus.IntegrationTests/IntakeWebNegativeTests.cs` (415 lines) | The negative-path shapes already in use — the model for the unauthorized and hostile-input facts |
| `tests/Pegasus.IntegrationTests/ImageViewingWebTests.cs` (224 lines) | The image byte-route scenarios, including the media-type gate |
| `docs/desktop/03-gateway-api-and-data/endpoint-map.md` § Intake | The five endpoint rows the acceptance criteria are checked against — **and** the `?queue&state` cell that Core cannot honour, so you recognise it |
| `docs/desktop/03-gateway-api-and-data/README.md` § 3 rows *Bytes & uploads*, *Compression* | That byte responses stream with `Content-Length`, range and `ETag`, and are **excluded** from response compression, which is JSON and problem responses only |
| `docs/desktop/01-inventory-and-parity/parity-matrix.md:64-65` | Rows `PAR-19` (the ten Details handlers) and `PAR-20` (the three byte pages, "validates length + SHA-256, no-sniff") — the parity claims this ticket satisfies |

## Ripple effects

- **`openapi/pegasus-v1.json` is where this ticket's real risk lives.** Committing it pins the
  intake vocabulary. [[GWY-004]] (plan handle `DSK-03-04`) snapshot-tests it and [[GWY-005]] (plan
  handle `DSK-03-05`) generates a client from it, so after this commit each of the three imported
  tickets below stops being an addition and becomes a versioned-contract change against a published
  client.
  - **[[INTK-001]]** (upstream `INTK-002`, *Intake duplication chores*) collapses the three
    decision-code tables onto one and decides the fail-closed behaviour for an unknown code. Step
    3's DTO is the fourth reader of that set.
  - **[[INTK-004]]** (upstream `INTK-027`, *Make policy re-evaluation work after transient staging
    cleanup*) settles the `reevaluate` failure contract — a named `validation` problem rather than
    a 200 followed by a silent `blocked_intake`. This ticket publishes that command but cannot make
    it work.
  - **[[INTK-006]]** (upstream `INTK-032`, *Fall back safely when a third-party report format
    cannot be read*) adds a state member beside the OCR-required state. Its own acceptance
    criterion pins it to *before* this snapshot is committed. It is labelled `needs-operator` and
    carries `docs_todo: true`, so "explicitly deferred and recorded" is a legitimate outcome.
- **The generated Kiota client** regenerates from that snapshot; CI runs `git diff --exit-code`
  after regeneration.
- **[[GWY-011]] (plan handle `DSK-03-11`) is blocked by this ticket** and reuses the `received`
  sub-group and the byte-route conventions established here. Its upload-status payload consumers
  depend on the receipt vocabulary this ticket freezes.
- **[[FEAT-009]] (plan handle `DSK-05-09`, "S9 Received items") is the desktop caller**; it is
  blocked by this ticket and consumes the detail DTO directly.
- **[[PLAT-006]] (plan handle `DSK-10-06`)** adds malformed-upload and unsafe-path tests over the
  upload-session endpoints and is blocked by this ticket; the filename and media-type rules
  established here are what it hardens.
- **[[GWY-017]] (plan handle `DSK-03-17`)** adds response compression; the three byte routes must
  be **exempt** (area README § 3, *Compression*). Record the requirement so it is not forgotten
  when that ticket lands. **[[GWY-018]] (plan handle `DSK-03-18`)** runs the authorization gap
  review over these nine commands.
- **[[FND-023]] (plan handle `DSK-01-10`) should land first.** Upstream `main` is ahead of the fork
  on intake paths (upstream `PLAT-039`, upstream `DOCS-010` — neither imported, both arriving by
  sync). Projecting code upstream has since changed is rework the area plan § 7 names.
- **Razor tests** — `MultiFormatIntakeWebTests.cs`, `IntakeWebNegativeTests.cs`,
  `ImageViewingWebTests.cs` and `LocalIntakeAccessTests.cs` must stay green. This ticket adds
  routes; it changes no page.
- **No schema change, no migration, no grant change.** The byte routes read through the storage
  adapters the pages already use and touch no new table.
- **`docs/` changes: none** beyond the regenerated snapshot.

## Out of scope

- `src/Pegasus.Core/Intake/**` — including adding a `queue` or allocation-state filter to
  `ListIntakeQuery`, which is what honouring the endpoint-map's query string literally would
  require. This is exactly why [[INTK-001]], [[INTK-004]] and [[INTK-006]] exist as their own
  tickets and why none of their work may be done here.
- The Worker and `src/Pegasus.Web/Pages/Intake/**`.
- The upload side: `POST /uploads/upload-session`, `GET /uploads/{receiptId}/status` and the group
  routes belong to [[GWY-011]] (plan handle `DSK-03-11`).
- `Pages/Uploads/Request.cshtml.cs` — anonymous external audience, stays a Razor page
  (`endpoint-map.md` § Stays web-only).
- Filtering a Core-paged result in Web to satisfy a parameter Core cannot honour. It would break
  `IntakeListPage.TotalCount` (`IntakeContracts.cs:748`) and make Web a second query engine.
- Re-deriving `CurrentCaseId`, `AssociationWasStaffDecision`, `UnlinkCancelsCase` or
  `CurrentCaseReference` in the DTO mapper — Core derives them and says so (INTK-029).
- Azure: no write. Azurite stands in for storage locally (L-02).
