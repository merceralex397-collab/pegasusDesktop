# Plan — GWY-011: Upload-session endpoints and case document upload, removal, custody retry, export and EVA handoff

**Diff estimate: ~15 files, ~3,600 lines** (~1,500 of them generated).

Derived from the files document, measured rather than asserted:

| File | Lines | How the number was reached |
| --- | --- | --- |
| `src/Pegasus.Contracts/Uploads/UploadSessionContracts.cs` (new) | ~110 | Session-open request (4 members), `UploadSessionResponse` (3), completion command reusing the shared mutation fields (6), plus XML doc on the two-step contract |
| `src/Pegasus.Contracts/Uploads/UploadStatusContracts.cs` (new) | ~70 | The status DTO (7 members after the widening), the five-value state enum with its wire spellings, and the group-status DTO |
| `src/Pegasus.Web/Api/UploadEndpoints.cs` (new) | ~340 | 2 shared session routes (~60 each — the streaming `PUT` is the largest handler in the ticket), 5 upload-group routes (~30 each), plus the group builder |
| `src/Pegasus.Web/Api/CaseDocumentEndpoints.cs` (new) | ~330 | 10 routes: 6 commands at ~25 lines, 2 byte routes at ~50 (digest headers, range, filename), 2 link routes at ~20 |
| `src/Pegasus.Core/Intake/DurableIntake.cs` (edit `:79-121`) | ±18 | One appended enum member, one added record member, one `switch` arm split out of the four-state fold, plus doc-comment correction |
| `src/Pegasus.Infrastructure/Persistence/EfQueuedIntakeStatusQueries.cs` (edit) | ±30 | The file is 45 lines; the projection at `:17-29` gains `DueAtUtc` and swaps the `CaseIntakeLinks`-only subquery for resolution through `IntakeReceipt.CurrentCaseId`'s rule |
| `/api/v1` problem mapper (edit, from [[GWY-002]], plan handle `DSK-03-02`) | +12 | One `DocumentRequestUnavailableException` arm |
| `tests/Pegasus.IntegrationTests/DesktopGatewayUploadTests.cs` (new) | ~1,000 | 11 command endpoints × 7 cases = 77; session lifecycle 4 (full session, oversized `PUT`, abandoned session, replayed complete); export comparison 1; the three upstream INTK-001 facts (retry state + `dueAtUtc`, association-resolved `caseId`, linked `caseId` unchanged); the inert-link fact. ~86 facts at ~11 lines after helpers |
| `openapi/pegasus-v1.json` (regenerated, from [[GWY-004]], plan handle `DSK-03-04`) | ~+650 | 17 paths + ~15 schemas |
| `src/Pegasus.Desktop.Infrastructure/Api/Generated/**` (regenerated, from [[FND-031]] / [[GWY-005]]) | ~+850, ~7 files | Kiota request-builders per path segment plus models |
| `docs/desktop/03-gateway-api-and-data/endpoint-map.md` (edit) | ±2 | One table row |

**Not included, and it is the largest uncertainty**: if the upload session needs a table of its own
rather than reusing the existing intake staging path, add a migration plus a `Grant*` migration,
the `scripts/Invoke-AzureDatabaseBootstrap.ps1` mirror and the census entry in
`tests/Pegasus.IntegrationTests/IntakePersistenceIntegrationTests.cs` — roughly 4 more files and
~200 lines. Step 3 establishes which case applies before any endpoint is written.

## Approach

Build one shared upload-session mechanism (`open` → streaming `PUT` → `complete`) and hang both the
staff-upload and case-document paths off it, then project the remaining custody, export and EVA
routes; and take on the two honesty problems this endpoint set inherits — the upload-status payload
that hides a scheduled retry, and the request-upload-link routes that promise a link over a store
that throws.

The rejected alternative for the session was **an `IFormFile` multipart endpoint per upload path**,
which is what the Razor pages do today (`Upload.cshtml.cs:48`,
`Cases/Custody.cshtml.cs:74`) and would be a smaller diff. It was rejected because the whole reason
proposal § 10.2 names the upload session is that a single-request multipart post gives no progress
and no resume, and creates the receipt in the same request as the bytes — so an interruption leaves
a half-written receipt. The two-step shape is the acceptance criterion, not a style preference.

The rejected alternative for the status payload was **to leave `QueuedIntakeStatus` alone and let
the desktop derive the waiting state from a due time it fetches separately**. Rejected because
there is no second endpoint that carries `DueAtUtc`, and because deriving a state client-side is
precisely the defect upstream `INTK-001` reports: two clients would derive it two ways. The named
consumer [[FEAT-013]] (plan handle `DSK-05-13`) is required by its own acceptance criteria to
consume both facts rather than infer them.

The rejected alternative for the request-upload links was **to omit the two routes until
[[CASE-002]] activates the capability**. Rejected because the endpoint map lists them and a missing
route is indistinguishable from an oversight, whereas a route that returns a named
`provider-unavailable` problem is a recorded state a client can render honestly. Composing a
working store here was never an option: it would activate INT-31 without the operator's accepted
limits and would require editing `ProductionCompositionTests`, which the guardrail forbids.

## Governing docs

The ticket's `refs` is **empty** and it carries `docs_todo: true` (confirmed in
`get_doc_gates GWY-011`: the `leave-backlog` `governing-doc` requirement shows `satisfied: true` on
that basis). So:

> **New ADR** — ADR-0103 (gateway, evolved `Pegasus.Web`, never direct database access from
> workstations), authored by [[FND-005]] (plan handle `DSK-00-05`).
> This plan is written to the decision as recorded in
> `docs/desktop/00-governance-and-workflow/README.md` § 3 (ADR table) and locked decision L-01 in
> `docs/desktop/README.md`; if the ADR lands differently this plan is revised before
> implementation.
> **ADR-0107** (Box and provider credentials stay behind the gateway; no long-lived provider secret
> in the package) also binds and is likewise authored by [[FND-005]]. It is what the research
> document's *Execution placement* answers "yes" against for question 3.

Because `refs` is empty, the programme-level authorities that bind today, with the step that
satisfies each:

| Authority | Requirement | Met by |
| --- | --- | --- |
| Proposal § 10.2 (API style) | Uploads use a two-step upload session | Steps 3, 5, 9 |
| Proposal § 12.2 (Box) | Provider credentials stay behind the gateway | Step 7 — the desktop uploads to `/api/v1`, never to Box |
| Proposal § 13.7 (Documents and evidence) | Documents and evidence are a primary operator workflow | Steps 7, 10 |
| L-01 | The upload surface lives in the existing `Pegasus.Web` | Steps 5, 7 |
| L-02 | Azurite and the local custody path stand in for Azure storage | Step 12 |
| Plan 03 § 3 row *Bytes & uploads* | Two-step session reusing `IntakeEnvelopeLimits`; `Uploads/Request` stays a Razor page | Steps 3, 5, 11 |
| Plan 03 § 3 row *Compression* | Byte responses excluded from compression | Step 10 |
| Plan 03 § 3 row *Idempotency* | `…/complete` idempotent by `operationKey` | Steps 4, 12 |
| Plan 03 § 7 trap *Runtime-role grants* | A new table needs its `Grant*` migration and census entry | Step 3 — establish first whether a new table is needed at all |
| Plan 03 § 7 trap *Two policy engines* | Rules stay in Core | Steps 6, 8 |
| Operator answers of 2026-08-24, quoted in upstream `CASE-022` (board [[CASE-002]]) | Per-link expiry; no rate limiting | **Not met here, by design** — step 8 records that both are refused by today's `RequestUploadPolicy` contract and are [[CASE-002]]'s Core policy change |
| Board decision recorded in [[FEAT-013]]'s acceptance criteria | Both the named waiting state and the derived poll interval | Step 6 — supplies both; does not reopen the choice |
| `docs/current-architecture.md` § Idempotency | Caller-supplied operation key, replay returns the same result | Steps 4, 12 |
| `AGENTS.md` § Repository task workflow step 4 | A recorded simplification pass over the branch diff | Step 15 |

## Routing

Copied from the ticket body's `## Routing` block.

- **Subagent**: `pegasus-gateway-dev` — `.codex/agents/pegasus-gateway-dev.toml`
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `minimal-api-file-upload` (`dotnet/skills`
  `98f84851`, plugin `dotnet-aspnetcore`) → `dotnet-webapi` (`dotnet/skills` `98f84851`, plugin
  `dotnet-aspnetcore`)
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`,
  `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search` for
  `ASP.NET Core minimal API large file upload streaming`, `microsoft_code_sample_search`)
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute`
  → `kanmer-review` → `kanmer-verify` → `kanmer-closeout`
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement
  (`AGENTS.md` § Repository task workflow step 5)

## Steps

These refine the ticket body's fourteen implementation steps in the same order and with the same
ownership; step 15 splits the body's step 14 so the test run and the simplification pass are
separately tickable.

1. **Orient.** Read the endpoint-map rows quoted in the ticket and the area README § 3 row
   *Bytes & uploads*, and the body of [[CASE-002]] (upstream `CASE-022`). Load
   `minimal-api-file-upload` and follow its streaming-upload section. Then `get_doc_gates GWY-011`
   and `take_ticket`. Confirm [[GWY-010]] (plan handle `DSK-03-10`) has merged — it supplies the
   `received` sub-group, the byte conventions and the intake fixtures — and that
   `src/Pegasus.Contracts/`, `src/Pegasus.Web/Api/`, `openapi/` and
   `src/Pegasus.Desktop.Infrastructure/Api/Generated/` exist.
2. **Read the seven projected page models in full and tabulate them**: `Upload.cshtml.cs`,
   `UploadStatus.cshtml.cs`, `UploadGroupStatus.cshtml.cs`, `Cases/Custody.cshtml.cs`,
   `Cases/Documents/Download.cshtml.cs`, `Cases/Documents/Export.cshtml.cs`,
   `Cases/Eva/Download.cshtml.cs`. Per handler: the Core use case, the version scope, and the exact
   response headers — including the two the endpoint map omits, `X-Content-SHA256`
   (`Documents/Download.cshtml.cs:54`) and `Content-Digest` (`Eva/Download.cshtml.cs:60-61`).
3. **Design the session against `IntakeEnvelopeLimits`, and settle the storage question first.**
   The session record carries declared content length, file name, media type and target; the `PUT`
   refuses bytes beyond `MaximumContentLength`; a batch cannot exceed `MaximumBatchFileCount` or
   `MaximumBatchContentLength`. **Read those from
   `src/Pegasus.Core/Intake/IntakeContracts.cs:7-75`; never copy the numbers** — [[CASE-002]] may
   raise them to 250 MB / 1 GB / 50 and a literal would pin the old ceiling. Record in the
   *Limits record* section below which values `IntakeEnvelopeLimits` held and whether [[CASE-002]]
   had landed. **Before writing any endpoint, determine whether the session can reuse the existing
   intake staging path or needs a table of its own.** If it needs a table: a migration, a `Grant*`
   migration, the `scripts/Invoke-AzureDatabaseBootstrap.ps1` mirror and the census entry in
   `tests/Pegasus.IntegrationTests/IntakePersistenceIntegrationTests.cs` all follow, enforced by
   `scripts/Test-MigrationGrants.ps1` in CI. This is the PLAT-035 "works locally, fails only in
   production" trap.
4. **Add `src/Pegasus.Contracts/Uploads/` DTOs** — session, completion command and status response
   — reusing the shared `operationKey` / `expectedVersion` / `editLeaseToken` fields from
   [[GWY-001]] (plan handle `DSK-03-01`) rather than per-endpoint variants.
5. **Add `src/Pegasus.Web/Api/UploadEndpoints.cs`** with the shared session routes
   `PUT /api/v1/upload-sessions/{sid}` and `POST /api/v1/upload-sessions/{sid}/complete`, and the
   staff upload group `POST /uploads/upload-session`, `GET /uploads/{receiptId}/status`,
   `GET /uploads/groups/{gid}`, `POST /uploads/groups`, `POST /uploads/groups/{gid}/attach`.
   **Stream the `PUT` body** — never buffer a whole file into memory.
6. **Make the upload-status payload honest** (upstream `INTK-001`, absorbed here and in
   [[FEAT-013]]; there is no fork ticket for it, and the board's `INTK-001` is upstream `INTK-002`,
   a different ticket). Three changes in the Core port and its EF implementation, together, so no
   caller re-derives anything:
   - **Add the due time.** `DateTimeOffset? DueAtUtc` on `QueuedIntakeStatus`
     (`src/Pegasus.Core/Intake/DurableIntake.cs:87-94`) and on the `IQueuedIntakeStatusQueries`
     projection, carrying `IntakeWorkItem.DueAtUtc` (`:41`) through unchanged and in UTC; null only
     when the receipt has no work item. Serialises as `dueAtUtc`.
   - **Name the waiting state.** Append `RetryScheduled = 4` to `QueuedIntakeStatusKind` (`:79-85`)
     — **the existing `Received = 0`, `Processing = 1`, `Complete = 2`, `Failed = 3` assignments are
     explicit and must not move** — and stop `FromWorkState` (`:96-114`) folding
     `IntakeWorkState.RetryScheduled` into `Received`; the other six work states keep their current
     mapping. Serialise as `retry_scheduled`, the spelling
     `src/Pegasus.Infrastructure/Persistence/EfIntakeWorkStore.cs:722` already persists. Correct the
     doc comment at `:98-101` so it still describes what the method does. **The wire value only:**
     the operator-facing word is [[FEAT-013]] step 8's, from `docs/design/README.md`; no operator
     sentence goes in the payload.
   - **Resolve the case id.** Keep the member name `CaseId` (`:93`) and change its meaning to what
     `IntakeReceipt.CurrentCaseId` (`src/Pegasus.Core/Intake/IntakeContracts.cs:407-408`) yields —
     a link **or** an association — replacing the `CaseIntakeLinks`-only subquery at
     `src/Pegasus.Infrastructure/Persistence/EfQueuedIntakeStatusQueries.cs:25-28`. **Resolve
     through that one derived property; a LINQ re-expression of the same rule is still a second
     copy** (the neighbouring comments cite INTK-029).
   Surface all three on the status DTO from step 4.
7. **Add `src/Pegasus.Web/Api/CaseDocumentEndpoints.cs`** extending the `cases` sub-group with
   `POST /{id}/custody/retry`, `POST /{id}/documents/upload-session`,
   `DELETE /{id}/documents/{docId}`, `POST /{id}/third-party-vehicle-evidence/confirm`,
   `POST /{id}/request-upload-links`, `DELETE /{id}/request-upload-links/{linkId}`,
   `GET /{id}/documents/{docId}/content`, `POST /{id}/documents/export`, `POST /{id}/eva-handoff`
   and `GET /{id}/eva-handoff/{revision}/bundle`.
8. **Make the request-upload-link routes honest about being inert.** They will hit
   `UnavailableDocumentRequestStore` (`src/Pegasus.Infrastructure/Persistence/UnavailableDocumentRequestStore.cs:13-29`),
   which checks `PerformCasework` and then throws `DocumentRequestUnavailableException`; production
   composition is pinned closed by
   `tests/Pegasus.IntegrationTests/ProductionCompositionTests.cs:116` and `:130`. Map both routes to
   the same Core ports the Razor handlers call and add a `DocumentRequestUnavailableException` arm
   to the problem mapper from [[GWY-002]], returning `urn:pegasus:problem:provider-unavailable`
   with a stable operator sentence saying the upload-link capability is not active. **Do not**
   compose a different store, edit `ProductionCompositionTests`, or build a gateway-local issuer.
   Record in the *Inactive-capability record* below that the routes go live when [[CASE-002]]
   activates INT-31, and that the two operator answers it must resolve first — a per-link expiry,
   and no rate limiting — are refused by today's contract:
   `RequestUploadPolicy.HasAcceptedLifetime` (`src/Pegasus.Core/Documents/RequestUploadPolicy.cs:440-452`,
   the exact-match at `:448`) and `ArgumentOutOfRangeException.ThrowIfNegativeOrZero(rateLimit)`
   (`:47`). Mirror the inert-until-`CASE-022` statement into [[FEAT-014]]'s (plan handle
   `DSK-05-14`) traps so the desktop does not render a findable link command as though it worked.
9. **Make an abandoned session leave nothing.** The session stages bytes; **only** `…/complete`
   calls the Core use case that creates the receipt or document. Give the session an expiry so an
   abandoned one is collected. Proved by the step-12 fact.
10. **Apply [[GWY-010]]'s byte conventions** to `…/documents/{docId}/content` and
    `…/eva-handoff/{revision}/bundle`: `Content-Length`, weak `ETag`, range, `nosniff`, sanitised
    filename, exclusion from response compression — **and carry across the digest headers the
    endpoint map omits**, `X-Content-SHA256` (`Documents/Download.cshtml.cs:54`) and
    `Content-Digest` (`Eva/Download.cshtml.cs:60-61`). Note that the EVA bundle's Core call requires
    `expectedVersion`, `operationKey`, `reason` and `editLeaseToken`
    (`Eva/Download.cshtml.cs:21-28`); on a `GET` they can only travel as required query parameters.
    See *Risks* — this is a live contradiction between the endpoint map and parity row `PAR-18`,
    and the reviewer should see it flagged rather than silently resolved.
11. **Leave `src/Pegasus.Web/Pages/Uploads/Request.cshtml.cs` untouched.** It stays a Razor page
    (anonymous request-link actor, antiforgery, PRG). No API equivalent.
12. **Add `tests/Pegasus.IntegrationTests/DesktopGatewayUploadTests.cs`** covering: a full
    three-step session succeeds; a `PUT` exceeding `IntakeEnvelopeLimits.MaximumContentLength` is
    refused, **asserted against the constant and never a literal byte count**; an abandoned session
    creates no receipt and no document; `…/complete` replayed with the same `operationKey` returns
    the same result; the seven-case matrix for each command endpoint; the export archive matching
    the `Cases/Documents/Export` output for the same case (against the upstream `CASE-019` proof
    fixture named in `PAR-17`); one fact that a receipt whose work item is `retry_scheduled`
    returns the `retry_scheduled` state and a non-null `dueAtUtc` equal to `IntakeWorkItem.DueAtUtc`
    and **never** `Received`; one fact that a receipt associated to a case with no `CaseIntakeLinks`
    row still returns that case in `caseId`; one fact that a **linked** receipt returns the same
    value it did before this change; and one fact that
    `POST /cases/{id}/request-upload-links` under the production composition returns the named
    `provider-unavailable` problem rather than a 500 or a fabricated link.
13. **Correct the `endpoint-map.md` status row** — replace the four-state list with the five-state
    list plus `dueAtUtc` and the association-or-link `caseId`. The `screen-specs.md:314` half of the
    same correction is [[FEAT-013]]'s and is not made here.
14. **Regenerate and commit** `openapi/pegasus-v1.json` and the Kiota client via
    `eng/api/Generate-ApiClient.ps1`.
15. **Run the verification commands, then the simplification pass**, recorded under a dated
    `## Simplification pass` heading here. Look specifically for: a literal byte limit, a second
    case-id rule, a renumbered enum, an operator sentence in the payload, a receipt created on
    `PUT`, a buffered upload, a dropped digest header, and any change to
    `ProductionCompositionTests`.

## Verification

Evidence tier from the body: **Tier 5 — Web/API/MCP caller.** It obliges evidence that the real
upload and byte routes reach Core with limits, idempotency and exception translation observable on
the wire. Local stack only (L-02); Azurite stands in for blob staging.

1. `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj -c Release --filter "FullyQualifiedName~DesktopGatewayUploadTests"`
   — expected: all facts pass, including the abandoned-session fact, the `retry_scheduled` +
   `dueAtUtc` fact, the association-resolved `caseId` fact and the inert request-upload-link fact.
   **This output is the `proof` document.**
2. `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj -c Release --filter "FullyQualifiedName~CaseCustodyWebTests"`
   — expected: the existing custody web tests pass unchanged.
3. `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj -c Release --filter "FullyQualifiedName~UploadConfirmationWebTests"`
   — expected: still passing. This is the proof that the appended enum value and the added nullable
   field left the Razor `Pages/UploadStatus.cshtml.cs` behaving as it does today.
4. `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj -c Release --filter "FullyQualifiedName~ProductionCompositionTests"`
   — expected: **unchanged and passing**. This is the proof that the closed capability was not
   quietly opened.

Observable behaviour to record: the effective request-body ceiling, established by sending
progressively larger bodies — `MaxRequestBodySize` is set nowhere in `src/` or `infra/`, so
Kestrel's ~30 MB default bites before the ≈200 MiB `MultipartBodyLengthLimit`. That measurement is
an input [[CASE-002]] needs and cannot be read from a constant.

## Limits record

_Written at implementation time, per step 3._

- `IntakeEnvelopeLimits` values in force when the endpoints were written — _not yet recorded._
- Had [[CASE-002]] (upstream `CASE-022`) landed? — _not yet recorded._
- Measured effective request-body ceiling — _not yet recorded._
- Did the upload session need a new table (and therefore a `Grant*` migration)? — _not yet
  recorded._

## Inactive-capability record

_Written at implementation time, per step 8._

- Request-upload-link routes are inert until [[CASE-002]] (upstream `CASE-022`) activates INT-31 to
  the operator's accepted limits. The two accepted answers — a per-link expiry, and no rate
  limiting — are refused by today's `RequestUploadPolicy` / `RequestUploadLimits` contract
  (`HasAcceptedLifetime` exact-match at `RequestUploadPolicy.cs:448`;
  `ThrowIfNegativeOrZero(rateLimit)` at `:47`) and are therefore a Core policy change owned by that
  ticket. Mirrored into [[FEAT-014]]'s traps on _(date not yet recorded)_.

## Risks / open questions

- **The EVA bundle route is a documented contradiction.** The repository has a reasoned,
  lease-bearing, version-checked **POST** (`Cases/Eva/Download.cshtml.cs:21-28`, with
  `NotFound`/`Conflict`/`Refused` outcomes); parity row `PAR-18`
  (`docs/desktop/01-inventory-and-parity/parity-matrix.md:63`) says `~POST … /eva-bundle`; the
  endpoint map and this ticket's step 7 say **GET**. *Mitigation:* the body outranks this plan, so
  the route is mapped as a `GET` and the four required arguments travel as required query
  parameters — with the consequence that an operator's reason text lands in access logs. Flagged
  here for the reviewer rather than resolved silently; if the reviewer prefers the POST, it is a
  one-line route change and an `endpoint-map.md` correction.
- **A new session table would pull in the PLAT-035 grant trap.** *Mitigation:* step 3 settles the
  storage question before any endpoint is written, and the *Limits record* captures the answer.
- **Buffering the `PUT` would pass every functional test and fail the acceptance criterion.**
  *Mitigation:* step 5 states the requirement, the `minimal-api-file-upload` skill's streaming
  section is loaded in step 1, and step 15's pass looks for it.
- **The real byte ceiling is not the constant.** `MaxRequestBodySize` is unset; Kestrel's ~30 MB
  default and Container Apps ingress both sit below `MultipartBodyLengthLimit`. *Mitigation:*
  measure and record it in the *Limits record*.
- **A literal limit would silently pin the old ceiling** when [[CASE-002]] raises it.
  *Mitigation:* step 3 and the step-12 fact assert against the constant.
- **Renumbering `QueuedIntakeStatusKind` would change every persisted and transmitted value.**
  *Mitigation:* step 6 appends `= 4`; `UploadConfirmationWebTests` is a named verification.
- **A second case-id rule anywhere is a stop condition.** *Mitigation:* step 6 resolves through
  `IntakeReceipt.CurrentCaseId`; step 15's pass looks for a re-expression in SQL, in the endpoint or
  in the client.
- **Making the link routes "work" is the tempting wrong move.** *Mitigation:* step 8, plus
  `ProductionCompositionTests` unchanged as a named verification.
- **Scope boundaries owned by named sibling tickets** — not open questions: the accepted limits,
  the per-link expiry and INT-31 activation belong to [[CASE-002]] (upstream `CASE-022`); the
  operator-facing word for the waiting state and the desktop upload screen to [[FEAT-013]] (plan
  handle `DSK-05-13`); the desktop document surface to [[FEAT-014]] (plan handle `DSK-05-14`); the
  malformed-upload hardening to [[PLAT-006]] (plan handle `DSK-10-06`); compression to [[GWY-017]]
  (plan handle `DSK-03-17`); the authorization sweep to [[GWY-018]] (plan handle `DSK-03-18`).
- **Phase span, by design.** The area README § 5 lists this row in both "10–12 (Phase 5)" and
  "11, 14 (Phase 6–7)", and `endpoint-map.md` gives the Uploads rows Phase 5 and the
  Custody/Documents/EVA rows Phase 6. The horizon is set to the earliest phase that needs any of
  it. If the reviewer prefers endpoints to land with their callers, split the Phase 6 rows into a
  follow-up rather than delaying the Phase 5 uploads slice.
- **One line reference in the ticket body is off by one**: it cites
  `RequestUploadPolicy.cs:46` for `ThrowIfNegativeOrZero(rateLimit)`; the guard is at `:47` and
  `:46` is the same guard for `maximumRequestBytes`. The substance — that "no rate limit" is
  unrepresentable — is unaffected. Recorded so a reader does not conclude the file changed.

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over this branch's own
diff before the PR, recorded here under a dated heading._
