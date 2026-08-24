# Research — GWY-011: Upload sessions, case documents, custody, export, EVA handoff — and the upload-status payload

## Question

Three questions, because this ticket carries three kinds of work. (1) What shape must the
three-step upload session take so it enforces the real limits and leaves nothing behind when
abandoned? (2) What exactly is wrong with `GET /uploads/{receiptId}/status` today, and what is the
smallest honest correction? (3) Which of the promised routes are plumbing over capabilities that do
not exist, and how does an endpoint say so without lying to an operator?

## Current behaviour

Eight page models, 1,354 lines together:

| Page model | lines | Handlers |
| --- | --- | --- |
| `src/Pegasus.Web/Pages/Upload.cshtml.cs` | 183 | `OnGet` `:43`, `OnPostAsync` `:48` (`IFormFile`, one request) |
| `src/Pegasus.Web/Pages/UploadStatus.cshtml.cs` | 83 | `OnGetAsync` `:56` |
| `src/Pegasus.Web/Pages/UploadGroupStatus.cshtml.cs` | 225 | `OnGetAsync` `:61`, `OnPostRegisterGroupAsync` `:64`, `OnPostAttachGroupAsync` `:130` |
| `src/Pegasus.Web/Pages/Cases/Custody.cshtml.cs` | 270 | `OnPostRetryCustodyAsync` `:28`, `OnPostUploadDocumentAsync` `:74`, `OnPostRemoveDocumentAsync` `:138`, `OnPostConfirmThirdPartyVehicleEvidenceAsync` `:162`, `OnPostCreateRequestUploadLinkAsync` `:186`, `OnPostRevokeRequestUploadLinkAsync` `:237` |
| `src/Pegasus.Web/Pages/Cases/Documents/Download.cshtml.cs` | 112 | `OnGetAsync` |
| `src/Pegasus.Web/Pages/Cases/Documents/Export.cshtml.cs` | 160 | `OnPostAsync` |
| `src/Pegasus.Web/Pages/Cases/Eva/Download.cshtml.cs` | 99 | `OnPostAsync` `:21` |
| `src/Pegasus.Web/Pages/Uploads/Request.cshtml.cs` | 222 | **not projected** — anonymous external audience, stays a Razor page |

Parity matrix rows (`docs/desktop/01-inventory-and-parity/parity-matrix.md`, 46 rows total by
`grep -c '^| PAR-'`): **`PAR-13`** (`:58`) for `Cases/Custody.cshtml.cs` and its six handlers;
**`PAR-16`** (`:61`) for document download; **`PAR-17`** (`:62`) for export, which records that
upstream `CASE-019` added the proof "Prove the case export produces a real archive" (upstream
`efbb2a9`); **`PAR-18`** (`:63`) for the EVA bundle download; **`PAR-28`** (`:73`) for `Upload`;
**`PAR-29`** (`:74`) for `UploadStatus`, whose own summary repeats the four-state list this ticket
corrects; **`PAR-30`** (`:75`) for `UploadGroupStatus`.

## Findings

### The upload-status payload (upstream INTK-001, absorbed)

- Every line reference in the ticket body checks out exactly.
  `src/Pegasus.Core/Intake/DurableIntake.cs`: `IntakeWorkItem` at `:35-46` with `DueAtUtc` at
  `:41`; `QueuedIntakeStatusKind` at `:79-85` with exactly four members
  (`Received = 0`, `Processing = 1`, `Complete = 2`, `Failed = 3`); `QueuedIntakeStatus` at
  `:87-94` with `CaseId` at `:93` and **no** due time; `QueuedIntakeStatusKinds.FromWorkState` at
  `:96-114`; `IQueuedIntakeStatusQueries` at `:116-121`.
- `FromWorkState` (`:103-114`) maps **four** work states to `Received` —
  `Pending`, `Dispatching`, `Dispatched` **and `RetryScheduled`** — under a doc comment that states
  the intent it now overshoots: "Everything before a lease is held reads as Received: staff are
  told the file is safe and waiting, not which internal queue step it is on." `Pending`,
  `Dispatching` and `Dispatched` genuinely are "safe and waiting"; a retry scheduled 30 minutes out
  is a different fact wearing the same word.
- `EfQueuedIntakeStatusQueries.GetAsync` (`src/Pegasus.Infrastructure/Persistence/EfQueuedIntakeStatusQueries.cs`,
  45 lines) resolves `CaseId` at `:25-28` from `CaseIntakeLinks` alone —
  `context.CaseIntakeLinks.Where(link => link.IntakeReceiptId == item.WorkItem.ProcessedReceiptId)`.
  A receipt whose case came from an **association** rather than a link therefore reports `null`.
- The correct rule already exists once, in Core:
  `IntakeReceipt.CurrentCaseId` (`src/Pegasus.Core/Intake/IntakeContracts.cs:407-408`) —
  `ManualAssociationVersion is null ? AcceptedCaseId : ManualLinkedCaseId`. The surrounding members
  carry comments saying "no surface re-derives provenance from raw identity" and "Derived here
  beside the rest of the association rules so no surface works it out again from raw fields
  (INTK-029)".
- The wire spelling for the new state is already settled and must not be re-invented:
  `src/Pegasus.Infrastructure/Persistence/EfIntakeWorkStore.cs:722` persists
  `IntakeWorkState.RetryScheduled => "retry_scheduled"`.
- `QueuedIntakeStatus` has a second reader: the Razor `Pages/UploadStatus.cshtml.cs` (83 lines,
  `OnGetAsync` at `:56`). A nullable field and an **appended** enum value leave it compiling and
  behaving as it does today; renumbering the existing four would not.

### The request-upload-link routes (upstream CASE-022, board [[CASE-002]])

- The capability is composed closed in production. `src/Pegasus.Infrastructure/DependencyInjection.cs:433-441`
  registers `UnavailableDocumentRequestStore` for `ICreateRequestUploadLink`,
  `IRevokeRequestUploadLink`, `IUploadToRequest` and `IGetRequestUpload` in the `else` branch, and
  `src/Pegasus.Infrastructure/Persistence/UnavailableDocumentRequestStore.cs:13-29` checks
  `PerformCasework` and then throws `DocumentRequestUnavailableException` for both create and
  revoke.
- `tests/Pegasus.IntegrationTests/ProductionCompositionTests.cs` (229 lines) pins it: `:116` asserts
  `IsType<UnavailableDocumentRequestStore>` for `ICreateRequestUploadLink` under `BuildProduction()`,
  under the comment at `:111-112` — "INT-31 is not on the alpha path and its limits are an open
  decision, so composing document custody must not activate anonymous upload links" — and `:130`
  repeats the assertion for the no-durable-storage profile.
- The two operator answers upstream CASE-022 carries are inexpressible in today's contract, exactly
  as the body says.
  - **Per-link expiry.** `RequestUploadLimits` (`src/Pegasus.Core/Documents/RequestUploadPolicy.cs:28-80`)
    has one global `TimeSpan Lifetime`, and `HasAcceptedLifetime` (`:440-452`) returns false unless
    `link.ExpiresAtUtc == link.CreatedAtUtc.Add(limits.Lifetime)` exactly (`:448`), with UTC-offset
    checks at `:442-445`. It is called in the accept path at `:378`. A link with a chosen date, or
    open until cancelled, is refused by construction.
  - **No rate limiting.** The constructor calls
    `ArgumentOutOfRangeException.ThrowIfNegativeOrZero(rateLimit)` — **at `:47`, not `:46` as the
    ticket body states**; `:46` is the same guard for `maximumRequestBytes`. The substance is
    unaffected: `rateLimit` cannot be zero, so "no rate limiting" is unrepresentable. Recorded so a
    reader does not conclude the file has changed under them.

### Limits, and the ceiling that is not a constant

- `IntakeEnvelopeLimits` (`src/Pegasus.Core/Intake/IntakeContracts.cs:7-75`):
  `MaximumContentLength` 10 MiB (`:13`), `MaximumMailboxContentLength` 750 MiB (`:33`),
  `MaximumBatchFileCount` 20 (`:42`), `MaximumBatchContentLength` = 20 × 10 MiB + overhead
  (`:49-50`).
- `src/Pegasus.Web/Program.cs:525-530` sets `FormOptions.MultipartBodyLengthLimit =
  IntakeEnvelopeLimits.MaximumBatchContentLength`, under a comment saying it is "Bounded for a
  whole Upload batch, not one file".
- **`MaxRequestBodySize` is configured nowhere.** `grep -rn "MaxRequestBodySize" src/ infra/`
  returns no match, confirming the body's claim. Kestrel's default (~30 MB) therefore refuses an
  oversized request before `MultipartBodyLengthLimit` (≈200 MiB) is ever consulted. The effective
  ceiling is the smallest of Kestrel's default, any Container Apps ingress limit, and the
  constants — and only the last of those is readable from the code.

### The byte handlers carry headers the endpoint map does not mention

- `Cases/Documents/Download.cshtml.cs:52-56`: `Cache-Control: private, no-store`,
  `X-Content-Type-Options: nosniff`, and **`X-Content-SHA256`** at `:54` — a per-response content
  digest the endpoint-map row omits.
- `Cases/Eva/Download.cshtml.cs:58-63`: `nosniff`, `Cache-Control: private, no-store`, and
  **`Content-Digest: sha-256=:<base64>:`** at `:60-61`, plus an explicit `Response.ContentLength`
  at `:62`.
- `Cases/Documents/Export.cshtml.cs:73-75`: `Cache-Control: private, no-store`, `nosniff`,
  `File(export.Content, "application/zip", fileName)`.
  Dropping either digest header in the API version would be a silent reduction in what the client
  can verify.
- **The EVA bundle download is not a GET today, and cannot become one without losing four
  arguments.** `Cases/Eva/Download.cshtml.cs:21-28` is `OnPostAsync(Guid id, int revision, long
  expectedVersion, string operationKey, string reason, string editLeaseToken, …)`, and the Core
  call at `:41-42` passes all six; the result carries `DownloadEvaHandoffOutcome.NotFound`,
  `Conflict` and `Refused` arms (`:44-54`). The endpoint-map row says
  `GET /cases/{id}/eva-handoff/{revision}/bundle` and "download: GET", while parity row `PAR-18`
  (`parity-matrix.md:63`) says `~POST /api/v1/cases/{id}/eva-bundle` (bytes). The two planning
  documents disagree with each other, and the repository agrees with the parity matrix. Recorded in
  Implications with the resolution the ticket body forces.

### Fixtures

`tests/Pegasus.IntegrationTests/CaseCustodyWebTests.cs` (276 lines),
`UploadConfirmationWebTests.cs` (501), `DocumentCustodyDurabilityTests.cs` (500),
`EvaHandoffPersistenceTests.cs` (633), `ProductionCompositionTests.cs` (229).

The projects this ticket writes into do not exist yet: `ls src` returns `Pegasus.Core`,
`Pegasus.Infrastructure`, `Pegasus.Web`, `Pegasus.Worker` only.

### Facts

Read from the repository on 2026-08-24, branch `task/desktop-plan-segmentation`. Commands:

- `wc -l` over the eight page models → the table above, 1,354 total.
- `sed -n '30,125p' src/Pegasus.Core/Intake/DurableIntake.cs` → `IntakeWorkItem` `:35-46`
  (`DueAtUtc` `:41`), `QueuedIntakeStatusKind` `:79-85`, `QueuedIntakeStatus` `:87-94`,
  `FromWorkState` `:96-114`, `IQueuedIntakeStatusQueries` `:116-121`.
- `cat -n src/Pegasus.Infrastructure/Persistence/EfQueuedIntakeStatusQueries.cs` → 45 lines;
  the `CaseIntakeLinks`-only `CaseId` at `:25-28`.
- `sed -n '718,726p' src/Pegasus.Infrastructure/Persistence/EfIntakeWorkStore.cs` →
  `RetryScheduled => "retry_scheduled"` at `:722`.
- `grep -rn "MaxRequestBodySize" src/ infra/` → **no match**.
- `sed -n '25,85p' src/Pegasus.Core/Documents/RequestUploadPolicy.cs` → the constructor guards,
  with `ThrowIfNegativeOrZero(rateLimit)` at `:47`.
- `sed -n '110,135p' tests/Pegasus.IntegrationTests/ProductionCompositionTests.cs` → the two
  pinning assertions at `:116` and `:130`.
- `grep -n "nosniff\|File(\|Headers"` over the three byte pages → the header sets above.
- `grep -c '^| PAR-' docs/desktop/01-inventory-and-parity/parity-matrix.md` → `46`.

Board facts, read with `get_item` / `search_items` on 2026-08-24; the join table in the `HZN-001`
group document `board-conventions.md` is the authority:

- upstream `CASE-022` is board [[CASE-002]] — *Deliver public upload links (INT-31) to the accepted
  limits*, area `case-reference-workflow`, profile `feature`, status `backlog`.
- The board's `CASE-001` is upstream `CASE-021` — a different ticket entirely.
- upstream `INTK-001` was **absorbed, not imported**: no fork ticket exists for it. The board's
  `INTK-001` is upstream `INTK-002` (*Intake duplication chores*).

Documentation read: `docs/desktop/03-gateway-api-and-data/endpoint-map.md` § Cases (Custody,
Documents, EVA rows) and § Intake (Uploads rows); `docs/desktop/03-gateway-api-and-data/README.md`
§ 3 rows *Bytes & uploads*, *Compression*; `docs/desktop/00-governance-and-workflow/README.md` § 3.

### Assumptions

- **A-GWY-1** — the staged upload session can reuse the existing intake staging path
  (`ReceiveIntake` staging plus Worker dispatch) rather than needing a new table, so no `Grant*`
  migration is required. *Confirmed by:* reading `Upload.cshtml.cs:48` and the staging store it
  calls before designing the session record. *If wrong:* a new table means a `Grant*` migration
  mirrored in `scripts/Invoke-AzureDatabaseBootstrap.ps1` and the census in
  `tests/Pegasus.IntegrationTests/IntakePersistenceIntegrationTests.cs`, enforced by
  `scripts/Test-MigrationGrants.ps1` in CI — the PLAT-035 trap, and the single largest hidden cost
  in this ticket.
- **A-GWY-2** — appending `RetryScheduled = 4` to `QueuedIntakeStatusKind` and adding a nullable
  `DueAtUtc` to `QueuedIntakeStatus` leaves `Pages/UploadStatus.cshtml.cs` compiling unchanged.
  *Confirmed by:* `UploadConfirmationWebTests` (501 lines) passing unchanged, which the body makes
  a named verification. *If wrong:* the page needs a one-line arm, which is inside this ticket's
  boundary only if it is a compile fix, not a behaviour change.
- **A-GWY-3** — the streaming `PUT` can write to the staging store without materialising the whole
  file, using the `minimal-api-file-upload` skill's streaming section. *Confirmed by:* the
  oversized-`PUT` fact in step 12, and by a memory observation if [[GWY-017]] (plan handle
  `DSK-03-17`) asks for one. *If wrong:* the ticket still works but the "streams rather than
  buffers" acceptance criterion fails, and that is a stop condition, not a note.
- **A-GWY-4** — the export archive can be compared byte-for-byte (or entry-for-entry) against the
  `Cases/Documents/Export` output for the same case, using the upstream `CASE-019` proof fixture
  named in `PAR-17`. *Confirmed by:* locating that fixture before writing the test. *If wrong:* the
  comparison is entry-name-and-count rather than bytes, and the plan says which was used.

## Execution placement

The six-question cloud-justification test from `docs/desktop/00-governance-and-workflow/README.md`
§ 3, answered for the responsibility this ticket places: *accepting operator-supplied bytes,
placing them in custody, and serving them back*.

| Question | Answer | Evidence |
| --- | --- | --- |
| Shared authority — must several users see and update the same state? | **yes** — lands in the gateway (`Pegasus.Web`), per L-01 | Case documents are shared case state: every mutation here carries `CaseMutationRequest` fields and an edit lease (`Custody.cshtml.cs:138`, `:162`), and the custody root is one per case |
| Unattended execution — must it run with every desktop closed? | **yes** — lands on the existing Worker Container App; nothing new is placed | The upload session only stages; the receipt and the custody transfer are completed by the Worker's queue path, which is the whole reason `IntakeWorkItem` has a `DueAtUtc` and a retry state at all (`DurableIntake.cs:41`, `:83`) |
| Protected credentials — long-lived secret that must not sit on workstations? | **yes** — lands behind the gateway, as ADR-0107 requires | Box is the custody destination and its credential is brokered by the gateway; the desktop uploads to `/api/v1` and never holds a Box token |
| Public callback — must an external service call a stable public endpoint? | **no** for this ticket | The one anonymous external surface is `Pages/Uploads/Request.cshtml.cs`, which **stays a Razor page** (`endpoint-map.md` § Stays web-only) and is explicitly out of bounds here. When upstream `CASE-022` (board [[CASE-002]]) activates INT-31 the answer becomes yes for that ticket, not this one |
| Central enforcement — revocation, permissions, audit, invariant independent of the client? | **yes** — lands in Core, invoked by the gateway | `UnavailableDocumentRequestStore` checks `PerformCasework` before refusing (`:18`, `:26`); `RequestUploadPolicy.HasAcceptedLifetime` (`:440`) refuses a link whose expiry does not match the accepted limits; `DownloadEvaHandoff` refuses on version conflict and returns a `Refused` outcome (`Eva/Download.cshtml.cs:47-54`). A client enforcing any of these would be a second policy engine |
| Measured operational advantage — measured evidence central is materially better? | **no** | No measurement exists. The effective request-body ceiling is not even known today (see Facts); establishing it is a measurement this ticket records, not a claim it makes |

Four "yes", each naming a host that already carries it: `Pegasus.Web` for shared authority and
credential brokering (L-01, ADR-0107), the existing Worker Container App for unattended completion,
Core for central enforcement. No new placement, no Azure resource, no Azure write; Azurite stands
in for blob staging locally (L-02).

## Implications

1. **The upload-status correction is three changes that must land together.** `dueAtUtc`,
   `retry_scheduled` and the association-or-link `caseId`. Any one alone leaves a caller inferring
   the others — and inference client-side is exactly the defect upstream INTK-001 reports. The
   named consumer [[FEAT-013]] (plan handle `DSK-05-13`) is required by its own acceptance criteria
   to consume both the named state and the derived poll interval, so the decision the upstream
   ticket left open ("or add an explicit state — decide in plan") is **already taken on the board**
   and must not be reopened here.
2. **Append the enum value; never renumber.** `QueuedIntakeStatusKind` has explicit numeric
   assignments `0`–`3` (`DurableIntake.cs:81-84`) and is persisted and serialised. `RetryScheduled
   = 4` is additive; inserting it in alphabetical or logical position would silently change every
   stored and transmitted value.
3. **Resolve `caseId` through `IntakeReceipt.CurrentCaseId`, not by copying its expression.** The
   rule exists once, in Core, with a comment citing INTK-029 saying no surface should re-derive it.
   A LINQ translation of the same rule inside `EfQueuedIntakeStatusQueries` would be a second copy
   even though it produced the same answer.
4. **The request-upload-link routes must fail honestly, and the temptation is to make them work.**
   `UnavailableDocumentRequestStore` throws; the endpoint-map row promises "link id + expiry". The
   only correct move is to let the closed composition surface as a single named problem
   (`urn:pegasus:problem:provider-unavailable`) and record that the routes go live when
   [[CASE-002]] activates INT-31. Composing a different store, editing
   `ProductionCompositionTests`, or issuing a gateway-local link would each turn a recorded
   inactive capability into an undocumented active one.
5. **The two accepted operator answers are a Core policy change, not an endpoint change.** A
   per-link expiry contradicts `HasAcceptedLifetime`'s exact-match rule (`:448`) and "no rate
   limiting" contradicts `ThrowIfNegativeOrZero(rateLimit)` (`:47`). Both live in
   `src/Pegasus.Core/Documents/RequestUploadPolicy.cs`, which the guardrail assigns to
   [[CASE-002]]. Recording that here is the useful contribution; changing it would be scope theft.
6. **Read the limits, do not copy them.** [[CASE-002]] proposes 250 MB per file, 1 GB per request
   and 50 files. An endpoint with `10 * 1024 * 1024` written into it would silently pin the old
   ceiling when those constants change. Every limit check reads `IntakeEnvelopeLimits`.
7. **The real ceiling must be measured, not read.** With `MaxRequestBodySize` unset anywhere in
   `src/` or `infra/`, Kestrel's ~30 MB default bites long before the ≈200 MiB
   `MultipartBodyLengthLimit`. An endpoint that validates against `MaximumBatchContentLength` will
   still see the request refused upstream of its own check. Establish the effective ceiling by
   sending progressively larger bodies and record it — it is an input [[CASE-002]] needs.
8. **The byte routes must keep their digest headers.** `X-Content-SHA256` on document download
   (`Download.cshtml.cs:54`) and `Content-Digest` on the EVA bundle (`Eva/Download.cshtml.cs:60-61`)
   are not in the endpoint-map rows. They are how a client verifies what it received; dropping them
   is a reduction the endpoint map would not catch.
9. **The EVA bundle route is a documented contradiction and the plan must state which side it
   takes.** The repository has a reasoned, lease-bearing, version-checked **POST**
   (`Eva/Download.cshtml.cs:21-28`, with `Conflict`/`Refused` outcomes); parity row `PAR-18`
   (`parity-matrix.md:63`) says `~POST`; the endpoint-map row and the ticket body's step 7 say
   **GET**. The body outranks this document, so the route is mapped as the body names it — and the
   four arguments Core requires (`expectedVersion`, `operationKey`, `reason`, `editLeaseToken`)
   must then travel as required query parameters, with the consequence that an operator's reason
   text lands in URLs and therefore in access logs. Recorded as a risk in the plan and reported
   upward, not silently resolved either way.
10. **An abandoned session must leave nothing, and only `…/complete` may call Core.** The staging
    step writes bytes; the completion step creates the receipt or document. Anything that creates a
    receipt on `PUT` reintroduces the half-written receipt the two-step shape exists to remove.
11. **`Pages/Uploads/Request.cshtml.cs` is out of bounds, not merely out of scope.** It is the
    anonymous request-link surface with antiforgery and PRG; an API equivalent would create a
    second anonymous ingress.

## Open questions

- None opened. The ticket body does not instruct one, and every candidate is owned by a named
  sibling ticket or settled by the body:
  - The accepted upload limits, the per-link expiry and the no-rate-limit answer — owned by
    [[CASE-002]] (upstream `CASE-022`). A scope boundary, recorded in the plan's *Risks*.
  - The operator-facing word for the `retry_scheduled` state — owned by [[FEAT-013]] (plan handle
    `DSK-05-13`) step 8, from the settled vocabulary in `docs/design/README.md`. This ticket sets
    the wire value only.
  - Whether the state or the poll interval is the answer to upstream INTK-001 — **already decided
    on the board**: [[FEAT-013]]'s acceptance criteria require both, so this ticket supplies both.
    Reopening it would contradict a settled decision.
  - The EVA GET-versus-POST contradiction — resolved by the body, which names the GET. Taken as a
    default rather than asked, and reported upward as a disagreement.
- `docs/open-decisions.md` item 1 under *QDOS alpha activation details* is closed by [[CASE-002]],
  not here, and is not reopened.
