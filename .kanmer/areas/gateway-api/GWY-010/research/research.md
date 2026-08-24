# Research — GWY-010: Intake (received items) endpoints and the frozen intake vocabulary

## Question

What do the ten `Intake/*` page handlers actually call, what does Core already refuse on its own,
and — the question that makes this ticket different from its neighbours — **what exactly does step
11 freeze**, so the three imported intake tickets that change the vocabulary can be checked
against something concrete rather than a feeling?

## Current behaviour

Four page models, 850 lines together:

| Page model | lines | Handlers |
| --- | --- | --- |
| `src/Pegasus.Web/Pages/Intake/Details.cshtml.cs` | 613 | `OnGetAsync` `:95`; `OnPostRetryAllocationAsync` `:111`; `OnPostBlockAsync` `:157`; `OnPostReevaluateAsync` `:178`; `OnPostCorrectDraftAsync` `:192`; `OnPostClaimCaseLeaseAsync` `:240`; `OnPostLinkCaseAsync` `:274`; `OnPostReverseCaseLinkAsync` `:310`; `OnPostRegisterImageIntakeAsync` `:513`; `OnPostDismissSuggestionAsync` `:535` |
| `src/Pegasus.Web/Pages/Intake/Source.cshtml.cs` | 78 | `OnGetAsync` `:11` — `IDownloadIntakeSource` |
| `src/Pegasus.Web/Pages/Intake/Asset.cshtml.cs` | 80 | `OnGetAsync` — `DownloadIntakeAssetQuery` |
| `src/Pegasus.Web/Pages/Intake/Image.cshtml.cs` | 79 | `OnGetAsync` |

Parity matrix rows: **`PAR-19`** (`docs/desktop/01-inventory-and-parity/parity-matrix.md:64`) for
`Intake/Details.cshtml.cs` and its ten handlers, and **`PAR-20`** (`:65`) for the three byte pages
— "`DownloadIntakeSource` validates length + SHA-256, no-sniff; assets/images served per receipt".
Both are status `inventoried`. The list half (`GET /received`) is projected from
`Operations/Index` queue lists and `UploadStatus`, whose matrix row is `PAR-27` (`:72`, status
`not inventoried`). The matrix holds 46 rows
(`grep -c '^| PAR-' docs/desktop/01-inventory-and-parity/parity-matrix.md` → `46`).

Core already owns the query side: `ListIntake` (`src/Pegasus.Core/Intake/IntakeQueryUseCases.cs:5`)
and `GetIntake` (`:43`), both over `IIntakeReceiptQueries`.

## Findings

- **`ListIntake` refuses out-of-range paging itself, and its page-size cap is 100, not the board's
  200.** `IntakeQueryUseCases.cs:17-28`: `Page is < 1 or > 10_000` and `PageSize is < 1 or > 100`
  each throw `ArgumentOutOfRangeException`. `:29-34` also throws when `Decision` is not a defined
  enum value. The endpoint's job is to turn those into `validation` problems before they surface as
  unhandled exceptions — not to re-implement the bounds.
- **`ListIntake` authorizes itself**: `StaffAuthorization.Require(query.Actor,
  StaffAccessRight.PerformCasework)` at `:16`, and `GetIntake` does the same at `:54`. The endpoint
  filter is a fail-fast boundary only; a rule written into the filter would be the "two policy
  engines" defect the area plan § 7 names.
- **The endpoint-map's `?queue&state` query string does not correspond to what Core accepts.**
  `ListIntakeQuery` (`IntakeContracts.cs:738-742`) is `(ActionActor Actor, IntakeDecision? Decision,
  int Page, int PageSize)` — one filter, and it is the **decision**, not a queue and not an
  allocation state. `IntakeReceiptSummary` (`:511-521`) carries `Decision` **and** a separate
  `AllocationState`, and `src/Pegasus.Web/Mcp/IntakeMcpTools.cs:62` states the rule in its own
  words: "processing decision and allocation state kept separate". Since the guardrail forbids
  touching `src/Pegasus.Core/Intake/**`, the endpoint can expose only the decision filter; a
  `state` or `queue` parameter that Core cannot honour would have to be filtered in Web, which is
  a second query engine. Recorded in Implications with the resolution.
- **`IntakeReceipt` already carries `Version`** (`IntakeContracts.cs:391`, defaulted to `0`) and
  `AllocationState` (`:397`), so the detail response's `version` and weak `ETag` need no new Core
  member. It also carries derived members the DTO should project rather than re-derive:
  `CurrentCaseId` (`:407-408`), `AssociationWasStaffDecision` (`:417-418`), `UnlinkCancelsCase`
  (`:429-430`, whose comment records INTK-029: "no surface works it out again from raw fields") and
  `CurrentCaseReference` (`:432-435`). Re-deriving any of these in the DTO mapper is the same
  defect those comments were written to prevent.
- **The three decision-code copies the body names are real, and they disagree exactly as the body
  says.**
  - `src/Pegasus.Infrastructure/Persistence/EfIntakeReceiptStore.cs:1241-1252` — `ParseDecision`
    knows **seven** codes (`case_created`, `needs_sorting`, `blocked_intake`, `unsupported`,
    `ocr_required`, `technical_failure`, `image_intake_registered`) and **throws** on anything else
    (`_ => throw UnknownCode("decision", value)`).
  - `src/Pegasus.Infrastructure/Persistence/EfOperationsStore.cs:563-569` — `MapIntakeState` knows
    **four** (`case_created`, `needs_sorting`, `unsupported`, `ocr_required` → `Succeeded`;
    `technical_failure` → `Failed`) and returns `EmailOperationState.Unknown` for the rest —
    silently swallowing `blocked_intake` and `image_intake_registered`.
  - `src/Pegasus.Web/Mcp/IntakeMcpTools.cs:82-87` — a third parse knowing **six** (all but
    `image_intake_registered`), with the same six re-emitted at `:190-195`, and a documented page
    cap of 50 at `:62`.
  - The enum itself, `IntakeDecision` (`IntakeContracts.cs:77-86`), has seven members. So one copy
    is complete and fail-closed, one is short by one and silent, one is short by two and silent.
    A DTO written by hand here would be the fourth.
- **The byte handlers already do most of what the endpoint-map row asks, and one thing it does not
  mention.** `Source.cshtml.cs:30` sets `XContentTypeOptions = "nosniff"` and `:31-34` returns
  `File(bytes, "application/octet-stream", SafeFileName(...))`; `SafeFileName` at `:52-68` strips
  control characters, `"`, `'`, `;` and `Path.GetInvalidFileNameChars()`, falling back to
  `intake-source.bin`. `:40-49` catches `IntakeArtifactIntegrityException` and returns **409** with
  a plain-text body — that is the SHA-256 validation surfacing, and the `/api/v1` version must
  return a problem document rather than `text/plain`.
  `Asset.cshtml.cs:39-44` adds a defence-in-depth media-type gate — "this endpoint accepts any
  asset id, so it gates on the parsed type" — returning `NotFound()` for a non-`image/*` asset, and
  sets `Cache-Control: private, no-store` plus an `inline` `Content-Disposition`. None of the three
  handlers sets an `ETag` or enables range processing today; both are new in the API version.
- **The filename rule the body points at is stricter than the page's own.**
  `src/Pegasus.Web/Mcp/AutomationMcpErrors.cs:127-140` (`RequireFileName`) **rejects** a name that
  is empty, over 255 characters, not equal to its own `Path.GetFileName`, or `.`/`..`; the page's
  `SafeFileName` **sanitises** instead. The body asks for the `RequireFileName` rule, which changes
  hostile input from "cleaned" to "refused". Worth stating, because the two behaviours produce
  different test expectations.
- `IntakeEnvelopeLimits` (`IntakeContracts.cs:7-75`) holds `MaximumContentLength` (10 MiB, one
  staff-form file, `:13`), `MaximumMailboxContentLength` (750 MiB, `:33`, with a comment recording
  that applying the 10 MiB figure to a whole envelope rejected a real 16.69 MB instruction on
  2026-08-05), `MaximumBatchFileCount` (20, `:42`) and `MaximumBatchContentLength` (`:49-50`).
  Read-only for this ticket; [[GWY-011]] (plan handle `DSK-03-11`) owns the upload side.
- `LinkIntake` is at `src/Pegasus.Core/Intake/DurableIntake.cs:1106` (the body cites `:1109`, which
  is inside the same declaration); `AllocateIntake`'s constructor is at
  `src/Pegasus.Core/Intake/IntakeAllocation.cs:205-208`.
- Test fixtures available: `tests/Pegasus.IntegrationTests/MultiFormatIntakeWebTests.cs` (1,429
  lines), `IntakeWebTestSupport.cs` (866 lines — the shared `WebApplicationFactory` support),
  `IntakeWebNegativeTests.cs` (415 lines), `ImageViewingWebTests.cs` (224 lines). `PAR-19` also
  names `QdosIntakeWebTests.cs` and `LocalIntakeAccessTests.cs`.
- The projects this ticket writes into do not exist yet: `ls src` returns `Pegasus.Core`,
  `Pegasus.Infrastructure`, `Pegasus.Web`, `Pegasus.Worker` only.

### Facts

Read from the repository on 2026-08-24, branch `task/desktop-plan-segmentation`. Commands:

- `wc -l src/Pegasus.Web/Pages/Intake/*.cshtml.cs` → `80` (Asset), `613` (Details), `79` (Image),
  `78` (Source), `850` total.
- `grep -n "OnPost\|OnGet" src/Pegasus.Web/Pages/Intake/Details.cshtml.cs` → the ten handler lines
  tabulated above.
- `sed -n '1,60p' src/Pegasus.Core/Intake/IntakeQueryUseCases.cs` → the authorization call at
  `:16` and the two range refusals at `:17-28`.
- `grep -rn "record ListIntakeQuery" src/Pegasus.Core/` → `IntakeContracts.cs:738`, four members.
- `sed -n '1235,1258p' src/Pegasus.Infrastructure/Persistence/EfIntakeReceiptStore.cs` and
  `sed -n '558,575p' src/Pegasus.Infrastructure/Persistence/EfOperationsStore.cs` → the seven-code
  and four-code tables.
- `grep -n "IntakeDecision\." src/Pegasus.Web/Mcp/IntakeMcpTools.cs` → the third copy at `:82-87`
  and `:190-195`.
- `wc -l tests/Pegasus.IntegrationTests/{MultiFormatIntakeWebTests,IntakeWebTestSupport,IntakeWebNegativeTests,ImageViewingWebTests}.cs`
  → `1429`, `866`, `415`, `224`.
- `grep -c '^| PAR-' docs/desktop/01-inventory-and-parity/parity-matrix.md` → `46`.

Documentation read: `docs/desktop/03-gateway-api-and-data/endpoint-map.md` § Intake (received
items), uploads, image intake; `docs/desktop/03-gateway-api-and-data/README.md` § 3 rows
*Bytes & uploads*, *Idempotency*, *Concurrency*, *Paging/filter/sort*, *Compression*;
`docs/desktop/00-governance-and-workflow/README.md` § 3.

Board facts, read with `get_item` / `search_items` on 2026-08-24 (the join table in the `HZN-001`
group document `board-conventions.md` is the authority for the mapping):

- upstream `INTK-002` is board [[INTK-001]] — *Intake duplication chores*, profile `chore`, status
  `backlog`, no pipeline documents yet.
- upstream `INTK-027` is board [[INTK-004]] — *Make policy re-evaluation work after transient
  staging cleanup*, profile `fix`, status `backlog`, labelled `defect`, `live-found`.
- upstream `INTK-032` is board [[INTK-006]] — *Fall back safely when a third-party report format
  cannot be read*, profile `feature`, status `backlog`, labelled `needs-operator`.

### Assumptions

- **A-GWY-1** — the `RequireStaffRight(StaffAccessRight.PerformCasework)` endpoint filter from
  [[GWY-003]] (plan handle `DSK-03-03`) is attachable to a sub-group in one call. *Confirmed by:*
  reading the filter [[GWY-003]] adds. *If wrong:* it is attached per route and the endpoint file
  grows by fourteen lines.
- **A-GWY-2** — `Results.File` / `TypedResults.File` with `enableRangeProcessing: true` satisfies
  the endpoint-map's range requirement without a custom handler, over the in-memory byte arrays
  the current handlers materialise (`Source.cshtml.cs:32` calls `source.Content.ToArray()`).
  *Confirmed by:* the byte facts in step 10 — a `Range` request returning `206` with the correct
  slice. *If wrong:* a stream overload is used instead, and the memory profile changes, which is
  [[GWY-017]]'s (plan handle `DSK-03-17`) concern rather than this ticket's.
- **A-GWY-3** — the intake decision vocabulary the DTO exposes is the `IntakeDecision` enum
  (`IntakeContracts.cs:77-86`), seven members, and [[INTK-001]] will collapse the three code tables
  onto that same enum rather than redefining it. *Confirmed by:* reading [[INTK-001]]'s plan when
  it exists. *If wrong:* step 3's mapping target changes, which is precisely why step 11 sequences
  the snapshot after it.
- **A-GWY-4** — nothing in the three byte paths needs a schema or grant change; they read through
  the existing storage adapters that the pages already use. *Confirmed by:* the pages call
  `IDownloadIntakeSource` / `DownloadIntakeAssetQuery` and touch no new table. *If wrong:* the area
  plan § 7 runtime-grant trap applies and a `Grant*` migration is needed — check before assuming.

## Execution placement

The six-question cloud-justification test from `docs/desktop/00-governance-and-workflow/README.md`
§ 3, answered for the responsibility this ticket places: *reading and acting on received items,
including handing retained source bytes to a client*.

| Question | Answer | Evidence |
| --- | --- | --- |
| Shared authority — must several users see and update the same state? | **yes** — lands in the gateway (`Pegasus.Web`), per L-01 | The received queue is one shared work list; `IntakeReceipt.Version` (`IntakeContracts.cs:391`) and the case-lease commands (`Details.cshtml.cs:240`, `:274`, `:310`) exist because two operators can act on the same receipt |
| Unattended execution — must it run with every desktop closed? | **yes** — lands on the existing Worker Container App; ADR-0106 keeps the Graph intake worker central and this ticket places nothing new there | Intake arrives and is decided by the Worker before any operator opens a desktop; this ticket only reads and acts on what the Worker already produced |
| Protected credentials — long-lived secret that must not sit on workstations? | **yes** — lands behind the gateway, as ADR-0107 already requires | The retained source and asset bytes live in the storage account the gateway reads through; putting the storage credential on a workstation is exactly what the byte endpoints exist to avoid |
| Public callback — must an external service call a stable public endpoint? | **no** | All fourteen routes are bearer-authenticated staff routes. The one anonymous external surface, `Uploads/Request`, stays a Razor page (`endpoint-map.md` § Stays web-only) and is not this ticket's |
| Central enforcement — revocation, permissions, audit, invariant independent of the client? | **yes** — lands in Core, invoked by the gateway | `ListIntake` and `GetIntake` call `StaffAuthorization.Require` themselves (`IntakeQueryUseCases.cs:16`, `:54`); `DownloadIntakeSource` validates length and SHA-256 and throws `IntakeArtifactIntegrityException`, which `Source.cshtml.cs:40-49` surfaces. A client that checked its own hashes would be trusting the bytes it was given |
| Measured operational advantage — measured evidence central is materially better? | **no** | No measurement exists. Area 10 owns the baseline; nothing is claimed here |

Four "yes", each naming a host that already carries the responsibility: `Pegasus.Web` for shared
authority and credential brokering (L-01, ADR-0107), the existing Worker Container App for
unattended execution (ADR-0106), Core for central enforcement. No new placement, no Azure resource,
no Azure write. Azurite stands in for storage locally (L-02).

## Implications

1. **`?queue&state` cannot be implemented as written.** Core accepts one filter — `IntakeDecision?`
   — and the guardrail forbids touching `src/Pegasus.Core/Intake/**`. The endpoint therefore
   exposes the decision filter (whatever `state` is named in the DTO must map onto it) and does
   **not** invent a `queue` parameter it would have to satisfy by filtering in Web. Filtering a
   paged Core result in Web also breaks the paging contract, since `IntakeListPage.TotalCount`
   (`IntakeContracts.cs:748`) would no longer match what was returned. Record the decision in the
   plan; do not silently drop the parameter.
2. **`pageSize` is capped at 100, not 200.** The board convention in the area plan § 3 says
   "≤ 200"; `ListIntake` refuses anything over 100. The lower cap wins, and the ticket says so.
   Note also that `IntakeMcpTools` documents a cap of 50 for the MCP surface (`:62`) — three
   numbers in play, of which only Core's is binding on this endpoint.
3. **The four `ArgumentOutOfRangeException` throws in `ListIntake` are the validation contract.**
   Left unmapped they become 500s. Mapping them to `urn:pegasus:problem:validation` is a step, not
   an afterthought.
4. **Do not re-derive `IntakeReceipt`'s computed members.** `CurrentCaseId`,
   `AssociationWasStaffDecision`, `UnlinkCancelsCase` and `CurrentCaseReference` are derived in
   Core with comments saying "no surface re-derives" and citing INTK-029. Project them; do not
   recompute them from `AcceptedCaseId` / `ManualLinkedCaseId` in the DTO mapper.
5. **The byte endpoints change two behaviours, not zero.** `IntakeArtifactIntegrityException`
   currently produces a 409 `text/plain` body; the API must produce a problem document. And the
   filename rule the body specifies (`RequireFileName`, refuse) differs from the page's
   (`SafeFileName`, sanitise) — so the API refuses a hostile name where the page cleaned it. Both
   are deliberate and both need a fact.
6. **Step 11 is the whole reason this ticket is different, and what it freezes is specific**: the
   `IntakeDecision` vocabulary as exposed on the detail DTO, and the `reevaluate` command's failure
   shape. The three imported tickets each change one of those:
   [[INTK-001]] decides which codes exist and what an unknown one does; [[INTK-004]] decides whether
   `reevaluate` refuses with a named `validation` problem or returns 200 and a silent
   `blocked_intake`; [[INTK-006]] adds a state member. After [[GWY-004]] and [[GWY-005]] pin the
   snapshot and the generated client, each of those becomes a versioned-contract change rather than
   an addition.
7. **A deferral is a legitimate outcome and must be written down.** [[INTK-006]] carries
   `docs_todo: true` and the `needs-operator` label because its operator-visible wording is still
   an open operator decision. "Settled" therefore includes "explicitly deferred and recorded with
   who decided" — the failure mode step 11 guards against is silence, not deferral.
8. **The interlock ran one way until this ticket's body was written.** All three imported tickets
   name `DSK-03-10`'s steps 3 and 11; this ticket named none of them. The plan carries the
   sequencing so the reference is now mutual.
9. **Start after the first upstream sync.** Upstream `main` is ahead of the fork on intake paths
   (upstream `PLAT-039`, upstream `DOCS-010` — neither imported, both arriving by sync).
   [[FND-023]] (plan handle `DSK-01-10`) lands that sync. Projecting code that upstream has since
   changed is rework, and the area plan § 7 names it.

## Open questions

- None opened. The ticket body does not instruct one, and every candidate resolves to a named
  sibling ticket rather than to an unknown:
  - Which intake decision codes exist, and what an unknown one does — owned by [[INTK-001]]
    (upstream `INTK-002`). A scope boundary, recorded in the plan's *Risks / open questions*.
  - The `reevaluate` refusal contract — owned by [[INTK-004]] (upstream `INTK-027`).
  - The unreadable-third-party-report state and its operator wording — owned by [[INTK-006]]
    (upstream `INTK-032`), which is where the operator decision sits.
  - The `?queue&state` mismatch — settled here by the guardrail (Core is not touched) and recorded
    as a decision in the plan, not asked.
