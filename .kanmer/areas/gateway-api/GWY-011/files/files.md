# Files — GWY-011: Upload sessions, case documents, custody, export and EVA handoff

Paths marked **(created by …)** do not exist on disk today (`ls src` returns `Pegasus.Core`,
`Pegasus.Infrastructure`, `Pegasus.Web`, `Pegasus.Worker` and nothing else).

## Where the change lands

| Path | New/edit | What happens here |
| --- | --- | --- |
| `src/Pegasus.Contracts/Uploads/UploadSessionContracts.cs` | **new** (project created by [[GWY-001]], plan handle `DSK-03-01`) | `UploadSessionResponse` (session id, expiry, byte-target URL), the session-open request, and the completion command reusing the shared `operationKey` / `expectedVersion` / `editLeaseToken` fields from [[GWY-001]] rather than per-endpoint variants |
| `src/Pegasus.Contracts/Uploads/UploadStatusContracts.cs` | **new** (same project) | The status response DTO carrying the **five**-value state, `dueAtUtc`, and the association-or-link `caseId` — the three facts [[FEAT-013]] (plan handle `DSK-05-13`) consumes and must not re-derive |
| `src/Pegasus.Web/Api/UploadEndpoints.cs` | **new** (folder created by [[GWY-002]], plan handle `DSK-03-02`) | The shared session routes `PUT /api/v1/upload-sessions/{sid}` and `POST /api/v1/upload-sessions/{sid}/complete`, plus `POST /uploads/upload-session`, `GET /uploads/{receiptId}/status`, `GET /uploads/groups/{gid}`, `POST /uploads/groups`, `POST /uploads/groups/{gid}/attach`. The `PUT` **streams**; it never buffers a whole file |
| `src/Pegasus.Web/Api/CaseDocumentEndpoints.cs` | **new** (same folder) | Ten routes on the `cases` sub-group: custody retry, document upload-session, document delete, third-party-vehicle-evidence confirm, request-upload-link create and revoke, document content, export, EVA handoff generate and bundle |
| `src/Pegasus.Core/Intake/DurableIntake.cs` | **edit**, `:79-121` **only** (named exception in the guardrail) | `QueuedIntakeStatusKind` gains `RetryScheduled = 4` **appended**, leaving `0`–`3` untouched; `QueuedIntakeStatus` gains `DateTimeOffset? DueAtUtc`; `FromWorkState` stops folding `IntakeWorkState.RetryScheduled` into `Received`; `IQueuedIntakeStatusQueries` is unchanged in shape but its projection widens |
| `src/Pegasus.Infrastructure/Persistence/EfQueuedIntakeStatusQueries.cs` | **edit** (named exception) | Project `WorkItem.DueAtUtc`; resolve `CaseId` through `IntakeReceipt.CurrentCaseId`'s rule rather than the `CaseIntakeLinks`-only subquery at `:25-28` |
| the `/api/v1` problem-details mapper | **edit** (created by [[GWY-002]]) | A `DocumentRequestUnavailableException` arm → `urn:pegasus:problem:provider-unavailable` with a stable operator sentence, so the request-upload-link routes refuse rather than 500 |
| `tests/Pegasus.IntegrationTests/DesktopGatewayUploadTests.cs` | **new** | The session lifecycle, the limit refusals, the seven-case matrix per command, the export comparison, the three upstream-INTK-001 facts and the inert-link fact |
| `openapi/pegasus-v1.json` | **edit** (created by [[GWY-004]], plan handle `DSK-03-04`) | Regenerated and committed |
| `src/Pegasus.Desktop.Infrastructure/Api/Generated/**` | **edit** (created by [[FND-031]], plan handle `DSK-02-06`; generator by [[GWY-005]], plan handle `DSK-03-05`) | Kiota output regenerated; CI fails if regeneration is not a no-op |
| `docs/desktop/03-gateway-api-and-data/endpoint-map.md` | **edit**, the `GET /uploads/{receiptId}/status` row only | Replace `returns Received/Processing/Complete/Failed` with the five-state list plus `dueAtUtc` and the association-or-link `caseId`. The `screen-specs.md:314` half of the same correction is [[FEAT-013]]'s |

## Context files

| Path | What it tells you |
| --- | --- |
| `src/Pegasus.Core/Intake/DurableIntake.cs:79-121` | The whole upload-status defect in 43 lines. `QueuedIntakeStatusKind` `:79-85` has four members with **explicit numeric assignments `0`–`3`** — which is why the new member is appended as `4` and nothing is renumbered. `QueuedIntakeStatus` `:87-94` has `CaseId` at `:93` and no due time. `FromWorkState` `:103-114` folds `Pending`, `Dispatching`, `Dispatched` **and `RetryScheduled`** into `Received`, under a doc comment (`:98-101`) stating the intent it overshoots: "staff are told the file is safe and waiting, not which internal queue step it is on" — true of the first three, false of a retry 30 minutes out |
| `src/Pegasus.Core/Intake/DurableIntake.cs:35-46` | `IntakeWorkItem`, with `DueAtUtc` at `:41` — the value that already exists and is simply never projected |
| `src/Pegasus.Infrastructure/Persistence/EfQueuedIntakeStatusQueries.cs` (45 lines) | The other half: `CaseId` resolved at `:25-28` from `CaseIntakeLinks` alone, so an associated-but-unlinked receipt reports `null` and the desktop can only offer "Open receipt". Read the whole file — it is short, and it is the only place the projection lives |
| `src/Pegasus.Core/Intake/IntakeContracts.cs:406-408` | `IntakeReceipt.CurrentCaseId` — the one correct case-id rule (`ManualAssociationVersion is null ? AcceptedCaseId : ManualLinkedCaseId`). Resolve through it; a LINQ re-expression of the same rule is still a second copy, and the neighbouring comments cite INTK-029 saying so |
| `src/Pegasus.Infrastructure/Persistence/EfIntakeWorkStore.cs:722` | `IntakeWorkState.RetryScheduled => "retry_scheduled"` — the wire spelling already settled. Do not invent a second one |
| `src/Pegasus.Web/Pages/UploadStatus.cshtml.cs` (83 lines, `OnGetAsync` `:56`) | The **second reader** of `QueuedIntakeStatus`. It must keep compiling and behaving identically after the field and enum member are added — which is what `UploadConfirmationWebTests` proves and why the enum value is appended rather than inserted |
| `src/Pegasus.Core/Intake/IntakeContracts.cs:7-75` | `IntakeEnvelopeLimits`: 10 MiB per file (`:13`), 750 MiB mailbox envelope (`:33`), 20 files (`:42`), batch total (`:49-50`). **Read these; never copy the numbers.** [[CASE-002]] may raise them to 250 MB / 1 GB / 50, and a literal in an endpoint would silently pin the old ceiling |
| `src/Pegasus.Web/Program.cs:525-530` | `FormOptions.MultipartBodyLengthLimit = IntakeEnvelopeLimits.MaximumBatchContentLength` (≈200 MiB), with the comment saying it bounds a whole batch and not one file. This is the limit you can see — not the one that bites first |
| *(absence)* `grep -rn "MaxRequestBodySize" src/ infra/` | **No match.** Kestrel's ~30 MB default therefore refuses an oversized request long before `MultipartBodyLengthLimit` is consulted, and Container Apps ingress may cut lower still. The effective ceiling has to be measured; it cannot be read |
| `src/Pegasus.Core/Documents/RequestUploadPolicy.cs:28-80` | `RequestUploadLimits`: one **global** `TimeSpan Lifetime`, and `ArgumentOutOfRangeException.ThrowIfNegativeOrZero(rateLimit)` at **`:47`** (the ticket body says `:46`, which is the same guard for `maximumRequestBytes` — the substance is unchanged). Together these make the operator's two accepted answers — a per-link expiry, and no rate limiting — unrepresentable in today's contract |
| `src/Pegasus.Core/Documents/RequestUploadPolicy.cs:440-452` | `HasAcceptedLifetime`: returns false unless `link.ExpiresAtUtc == link.CreatedAtUtc.Add(limits.Lifetime)` exactly (`:448`), with UTC-offset checks at `:442-445`. Called in the accept path at `:378`. A chosen date, or open-until-cancelled, is refused by construction — which is why the change is [[CASE-002]]'s Core policy work and not an endpoint parameter |
| `src/Pegasus.Infrastructure/Persistence/UnavailableDocumentRequestStore.cs:13-29` | Both `ICreateRequestUploadLink` and `IRevokeRequestUploadLink` check `PerformCasework` and then throw `DocumentRequestUnavailableException`. This is what your endpoint will actually hit in production |
| `src/Pegasus.Infrastructure/DependencyInjection.cs:433-441` | The `else` branch that composes that throwing store for all four request-upload ports. Do not add a fifth registration or a different store |
| `tests/Pegasus.IntegrationTests/ProductionCompositionTests.cs:111-116`, `:130` | The test that pins the capability closed, and the comment that says why: "INT-31 is not on the alpha path and its limits are an open decision, so composing document custody must not activate anonymous upload links." **Do not edit this file** — it is the evidence, and the guardrail names it |
| `src/Pegasus.Web/Pages/Cases/Custody.cshtml.cs` (270 lines) | The six handlers to project, at `:28`, `:74`, `:138`, `:162`, `:186`, `:237` — each one's Core port, version scope and whether `reason` is required |
| `src/Pegasus.Web/Pages/Cases/Documents/Download.cshtml.cs:52-56` | The byte headers to carry across, including **`X-Content-SHA256`** at `:54` — a content digest the endpoint-map row does not mention. Dropping it reduces what a client can verify |
| `src/Pegasus.Web/Pages/Cases/Eva/Download.cshtml.cs:21-63` | Two things. (1) The bundle download is a **POST** taking `expectedVersion`, `operationKey`, `reason` and `editLeaseToken` (`:21-28`) with `NotFound`/`Conflict`/`Refused` outcomes (`:44-54`) — the endpoint-map's `GET` cannot carry those four unless they become query parameters. (2) The headers at `:58-62`: `nosniff`, `private, no-store`, **`Content-Digest: sha-256=:<base64>:`** and an explicit `Response.ContentLength` |
| `src/Pegasus.Web/Pages/Cases/Documents/Export.cshtml.cs:73-75` | The export's headers and `application/zip` media type; the archive whose bytes the new endpoint must match |
| `src/Pegasus.Web/Pages/Upload.cshtml.cs` (183 lines, `OnPostAsync` `:48`) | The one-request `IFormFile` shape being replaced — no progress, no resume, and a receipt created in the same request as the bytes |
| `src/Pegasus.Web/Pages/UploadGroupStatus.cshtml.cs` (225 lines) | `OnGetAsync` `:61`, `OnPostRegisterGroupAsync` `:64`, `OnPostAttachGroupAsync` `:130` — the grouped-upload use cases behind three of the routes |
| `src/Pegasus.Web/Pages/Uploads/Request.cshtml.cs` (222 lines) | The page that **stays web-only**: anonymous request-link actor, antiforgery, PRG. Read it once so you recognise what an API equivalent would duplicate, then leave it alone |
| `tests/Pegasus.IntegrationTests/UploadConfirmationWebTests.cs` (501 lines) | The Razor upload-status behaviour that must survive the widened projection unchanged — the named verification for the enum-append decision |
| `tests/Pegasus.IntegrationTests/CaseCustodyWebTests.cs` (276 lines) | The custody web behaviour that must stay green |
| `tests/Pegasus.IntegrationTests/DocumentCustodyDurabilityTests.cs` (500 lines) | The durability scenarios — the closest model for "an abandoned session leaves nothing" |
| `tests/Pegasus.IntegrationTests/EvaHandoffPersistenceTests.cs` (633 lines) | The EVA revision fixtures for the handoff generate/download facts |
| `docs/desktop/01-inventory-and-parity/parity-matrix.md:58`, `:61-63`, `:73-75` | Rows `PAR-13` (custody), `PAR-16` (download), `PAR-17` (export, recording upstream `CASE-019`'s "Prove the case export produces a real archive" proof at upstream `efbb2a9`), `PAR-18` (**which says `~POST … /eva-bundle`, disagreeing with the endpoint map's GET**), `PAR-28` (upload), `PAR-29` (upload status — its own summary repeats the four-state list this ticket corrects), `PAR-30` (upload groups) |
| `docs/desktop/03-gateway-api-and-data/README.md` § 3 rows *Bytes & uploads*, *Compression* | The two-step session shape, the `Uploads/Request` exclusion, and the rule that byte responses are excluded from compression |

## Ripple effects

- **[[FEAT-013]] (plan handle `DSK-05-13`, "S13 Uploads") is the named consumer of the status
  payload.** Its acceptance criteria require **both** the named waiting state and a poll interval
  derived from the due time, so this ticket must supply both `retry_scheduled` and `dueAtUtc`. If
  that slice ends up resolving a case id or inferring a state for itself, this ticket under-delivered.
- **[[CASE-002]] (upstream `CASE-022`) owns the limits and the activation.** When it lands, the
  request-upload-link routes go live and the byte ceilings change — without any edit to this
  ticket's endpoint code, provided every check reads `IntakeEnvelopeLimits`. It also owns the Core
  policy change that a per-link expiry and a zero rate limit require
  (`RequestUploadPolicy.cs:47`, `:440-452`).
- **[[FEAT-014]] (plan handle `DSK-05-14`, "S14 Documents and custody") must be told the link
  routes are inert** so the desktop does not render a findable command as though it worked. The
  ticket body instructs mirroring the statement into that ticket's traps.
- **[[PLAT-006]] (plan handle `DSK-10-06`)** adds malformed-upload and unsafe-path tests over these
  upload-session endpoints and is blocked by this ticket.
- **[[GWY-017]] (plan handle `DSK-03-17`)** adds response compression — the byte routes must be
  exempt; **[[GWY-018]] (plan handle `DSK-03-18`)** runs the authorization gap review over these
  commands. Both are listed as blocked by this ticket.
- **[[GWY-010]] (plan handle `DSK-03-10`) supplies the `received` sub-group, the byte-endpoint
  conventions and the intake fixtures**, and is the prerequisite named in the body.
- **`openapi/pegasus-v1.json` and the generated client** regenerate; CI runs `git diff --exit-code`.
- **Razor tests** — `UploadConfirmationWebTests`, `CaseCustodyWebTests`,
  `DocumentCustodyDurabilityTests`, `EvaHandoffPersistenceTests` and `ProductionCompositionTests`
  must all stay green **unchanged**. `ProductionCompositionTests` staying green is the proof that
  the closed capability was not quietly opened.
- **`docs/desktop/03-gateway-api-and-data/endpoint-map.md`** — one row corrected (the status row).
  Named in the body's *Documentation changes*.
- **Possible migration.** If the upload session needs a table of its own rather than reusing the
  existing staging path, a `Grant*` migration is required, mirrored in
  `scripts/Invoke-AzureDatabaseBootstrap.ps1` and the census in
  `tests/Pegasus.IntegrationTests/IntakePersistenceIntegrationTests.cs`, enforced by
  `scripts/Test-MigrationGrants.ps1` in CI (`.github/workflows/ci.yml:58-60`). Establish this
  before designing the session record — it is the PLAT-035 trap and the largest hidden cost here.

## Out of scope

- `src/Pegasus.Web/Pages/Uploads/Request.cshtml.cs` — out of **bounds**, not merely out of scope.
  Anonymous external audience; an API equivalent would create a second anonymous ingress.
- `tests/Pegasus.IntegrationTests/ProductionCompositionTests.cs` — the guardrail names it. It is
  the evidence that the capability is closed; editing it to make a route pass inverts the test's
  purpose.
- Composing a second document-request store, or issuing a gateway-local upload link. The capability
  is inactive by a recorded decision; making it work here would activate INT-31 without the
  operator's accepted limits.
- `src/Pegasus.Core/Documents/RequestUploadPolicy.cs` and
  `src/Pegasus.Core/Intake/IntakeContracts.cs` — read-only here; both belong to [[CASE-002]] for the
  accepted-limits and per-link-expiry change.
- Anything in `src/Pegasus.Core/Intake/DurableIntake.cs` outside `:79-121`, and anything else in
  `src/Pegasus.Core/Intake/**`.
- The Worker.
- Raising any limit. This ticket reads constants and records their values; it does not change them.
- Putting an operator-facing sentence in the status payload. The wire value is `retry_scheduled`;
  the word an operator reads is [[FEAT-013]] step 8's, from `docs/design/README.md`.
- Azure: no write. Blob staging runs against Azurite locally (L-02).
