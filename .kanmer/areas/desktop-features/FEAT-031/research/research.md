# Research — FEAT-031: the real shape of the document commands, and why the download route as drawn cannot address a version

## Question

What do the four document commands and the download query actually require,
what does the existing `Documents/Download` page really guarantee (as opposed to
what the endpoint-map convention claims), where exactly does the PLAT-041 call
multiplication come from, and which of this ticket's own steps are blocked by an
upstream commit that has not arrived?

## Current behaviour

Read at fork `main` `191ddf33` on 2026-08-24. The implementer re-reads after the
latest upstream sync ([[FND-023]], plan handle `DSK-01-10`) and records the SHA,
because upstream PLAT-039 (commits `79db11f`, `282ba44`) arrives with it and
PLAT-041's status must be re-checked at the same moment.

| Handler | `path:line` | Core owner it calls |
| --- | --- | --- |
| Retry custody | `src/Pegasus.Web/Pages/Cases/Custody.cshtml.cs:28` `OnPostRetryCustodyAsync` | `IRetryCaseCustody` — **[[FEAT-028]]'s (plan handle `DSK-07-02`) command, not this ticket's** |
| Upload document | `…/Cases/Custody.cshtml.cs` `OnPostUploadDocumentAsync` | `IAddCaseDocument` (`src/Pegasus.Core/Documents/DocumentContracts.cs:169`) |
| Remove document | `…/Cases/Custody.cshtml.cs` `OnPostRemoveDocumentAsync` | `ILogicallyRemoveDocument` (`DocumentContracts.cs:206`) |
| Confirm third-party evidence | `…/Cases/Custody.cshtml.cs` `OnPostConfirmThirdPartyVehicleEvidenceAsync` | `IConfirmThirdPartyVehicleEvidence` (`DocumentContracts.cs:213`) |
| Download content | `src/Pegasus.Web/Pages/Cases/Documents/Download.cshtml.cs:16` `OnGetAsync` | `IDownloadCaseDocument` (`DocumentContracts.cs:176`) |
| Export archive | `src/Pegasus.Web/Pages/Cases/Documents/Export.cshtml.cs:18` `OnPostAsync` | `IExportCaseDocuments` (`DocumentContracts.cs:199`) |

Parity-matrix rows: **`PAR-13`** (`Cases/Custody.cshtml.cs` (270), six handlers,
status `inventoried`), **`PAR-16`** (`Cases/Documents/Download.cshtml.cs` (112),
`Verification` column `to locate`, status **`not inventoried`**) and **`PAR-17`**
(`Cases/Documents/Export.cshtml.cs` (160), verification "upstream CASE-019 test
(after sync)", status **`not inventoried`**), at
`docs/desktop/01-inventory-and-parity/parity-matrix.md:58`, `:61`, `:62`. The
matrix holds `PAR-01`…`PAR-46`
(`grep -c '^| PAR-' …/parity-matrix.md` → 46).

## Findings

### Facts

- **The download route as the ticket draws it cannot address the content.**
  `DownloadCaseDocumentQuery` (`DocumentContracts.cs:157-162`) is
  `(CaseId, OccurrenceId, VersionId, Actor, OperationKey)` — **five** members,
  including a `VersionId`. The Razor page takes three route ids
  (`Download.cshtml.cs:17-20`: `caseId`, `documentId`, `versionId`) and passes
  them through at `:34-38`. The ticket's step 6 route
  `GET /api/v1/cases/{caseId}/documents/{occurrenceId}/content` carries no
  version, so it either needs a `{versionId}` segment or a documented
  current-version resolution. Note also that the page's parameter is *named*
  `documentId` while its own log messages call the same value `occurrenceId`
  (`:96`, `:105`) and `DownloadCaseDocumentQuery`'s second member is
  `OccurrenceId` — the DTO must use the Core name.
- **A `GET` here carries an operation key.** The page passes
  `$"web-download:{Guid.NewGuid():N}"` (`Download.cshtml.cs:38`). The gateway
  equivalent is a `desk-download:` prefixed key; the field is required by Core
  and cannot be omitted because the route is a `GET`.
- **What `Documents/Download` actually guarantees is not what the endpoint-map
  convention claims.** Measured at `Download.cshtml.cs:52-56`: `Cache-Control:
  private, no-store`, `X-Content-Type-Options: nosniff`, a custom
  `X-Content-SHA256` header, `Response.ContentLength`, and `File(stream,
  mediaType, fileName)`. There is **no `ETag`** and **no
  `enableRangeProcessing: true`** — the page supports no range requests at all.
  `docs/desktop/03-gateway-api-and-data/README.md` § 3 "Bytes & uploads" says
  byte endpoints "stream with `Content-Length`, range support, `ETag`", and
  `no-store` is actively incompatible with `ETag` revalidation. The ticket's
  step 6 phrase "the same properties the existing page guarantees" therefore
  under-describes two of the three it names.
- **The safe-filename rule is a real algorithm, not a phrase.**
  `TryValidateResponse` (`Download.cshtml.cs:69-82`) and `IsSafeFileName`
  (`:84-91`) require: `Path.GetFileName` equal to the original **ordinally**,
  1–255 characters, not `.` or `..`, no `/` or `\`, no control characters, a
  parseable media type, `ContentLength >= 0`, and a 64-character lowercase hex
  SHA-256. A failure is logged as an **error** ("returned unsafe metadata",
  `:103-105`) and answered with `NotFound` — not a 500. Reuse the algorithm.
- **Every failure on the download path becomes `NotFound`.**
  `Download.cshtml.cs:59-66` catches `ArgumentException`,
  `InvalidOperationException`, `InvalidDataException`, `IOException` and
  `UnauthorizedAccessException` and returns `NotFound` with a warning log. That
  is deliberate non-disclosure: an unauthorised case must not be
  distinguishable from a missing one. A gateway that returns `403` here would
  leak case existence.
- **Export is a `POST` command, not a `GET`, and it is bounded twice.**
  `Export.cshtml.cs:15-16`: `MaximumSelections = 100`,
  `MaximumArchiveBytes = 100 MiB`. `OnPostAsync` (`:18-25`) requires
  `expectedVersion`, `operationKey` and `editLeaseToken`, and
  `ExportCaseDocumentsCommand` (`DocumentContracts.cs:113-120`) carries all
  three plus `MaximumArchiveBytes`. The endpoint is a mutation-shaped command
  because it takes a case edit lease.
- **The export operation key must be a `Guid` in `"N"` format.**
  `Guid.TryParseExact(operationKey, "N", out var operationId)`
  (`Export.cshtml.cs:35`), normalised to `operationId.ToString("N")` at `:47`.
  The gateway plan's `desk:<guid>` format
  (`docs/desktop/03-gateway-api-and-data/README.md` § 3 Idempotency) would be
  **rejected**, as would a hyphenated `"D"` GUID. This is the second command on
  the board with a format-constrained key.
- **Export re-validates the manifest against the request before streaming.**
  `Export.cshtml.cs:53-64`: the manifest count must equal the selection count,
  the `(OccurrenceId, VersionId)` pairs must be distinct and must all be in the
  requested set, every `ContentLength >= 0` and every SHA-256 a 64-character
  hex string. A failure is logged as an error and answered with a *safe*
  operator sentence, not a 500.
- **A case exports only in Review, and that is a Core condition with a recorded
  operator decision behind it.** `CaseNotInReviewException`
  (`DocumentContracts.cs:184-193`) carries the remark: "The operator's rule
  (2026-08-04) is that a case exports only in Review. A disabled button is
  presentation; this is the condition itself, so it holds for every caller
  rather than only for the one that renders the button." The gateway is a new
  caller and inherits it.
- **Uploads cannot stream into Core.** `AddCaseDocumentCommand`
  (`DocumentContracts.cs:66-77`) takes `ReadOnlyMemory<byte> Content` — the
  whole file materialised — as does `IDocumentContentStore.StoreAsync`
  (`:226-233`). The three-step upload session must therefore stage bytes
  somewhere bounded and hand Core a complete buffer at completion. That is
  precisely why `IntakeEnvelopeLimits.MaximumContentLength`
  (`src/Pegasus.Core/Intake/IntakeContracts.cs:13`, **10 MiB**, documented as
  "One file uploaded through the staff form, which arrives inside one bounded
  multipart HTTP request") must be enforced at the boundary, not discovered at
  completion.
- **The four mutation commands share one concurrency shape.**
  `AddCaseDocumentCommand` (`:66-77`), `LogicallyRemoveDocumentCommand`
  (`:145-152`), `ConfirmThirdPartyVehicleEvidenceCommand` (`:154-161`) and
  `ExportCaseDocumentsCommand` (`:113-120`) each carry `ActionActor`,
  `OperationKey`, `ExpectedCaseVersion` and `EditLeaseToken`. Removal and
  confirmation additionally require a `Reason`. **Every one of the four needs a
  case edit lease** — so the desktop must acquire one through [[GWY-008]] (plan
  handle `DSK-03-08`) before any of them, including the download's sibling
  export.
- **`AddCaseDocumentResult` carries `IsReplay`** (`:79-83`), so upload
  idempotency is decided in Core; the gateway asserts it rather than
  implementing it.
- **The metadata field list the ticket's step 4 names is spread across two
  records and one of its fields does not exist there.** `DocumentVersion`
  (`:33-46`) has `Id`, `DocumentId`, `Version`, `FileName`, `MediaType`,
  `ContentLength`, `Sha256`, `CustodyStatus`, `CreatedAtUtc`, `CreatedBy`,
  `IsCurrent`, `IsLogicallyRemoved`, **`RemovalReason`**; `DocumentOccurrence`
  (`:48-59`) has `Id`, `CaseId`, `DocumentId`, `VersionId`, `SemanticRole`,
  `Source`, `SourceOccurrenceIdentity`, `RecordedAtUtc`,
  `ThirdPartyVehicleConfirmedAtUtc`, `ThirdPartyVehicleConfirmationReason`,
  `Ordinal`. The step-4 list omits `RemovalReason`, `Ordinal`,
  `SourceOccurrenceIdentity`, `RecordedAtUtc` and the two third-party
  confirmation fields — and the last two are what the desktop needs to render
  the confirm affordance's current state. `DocumentCustodyStatus`
  (`:26-31`) is `Pending` / `Confirmed` / `Failed`.
- **`IDocumentContentStore` exposes no batch method — this is the PLAT-041 root
  cause in one sentence.** `DocumentContracts.cs:220-280`: `StoreAsync`,
  `OpenReadAsync`, `DeleteAsync`, plus the two address-based default-interface
  wrappers `StoreVersionAsync` and `OpenReadVersionAsync`. Every read is one
  call per version; there is nowhere to hang a batch.
- **The Box client is stateless and re-walks ancestry on every call.**
  `BoxContentClient` (`src/Pegasus.Infrastructure/Custody/BoxCaseCustody.cs:144`)
  caches no folder id, file id or ancestry, and `EnsureDescendantAsync` is
  awaited at **ten** call sites (`:171`, `:186`, `:234`, `:244`, `:252`, `:272`,
  `:294`, `:305`, `:316`, `:330`) — one GET per ancestry level, every time.
  `GetExistingCaseRootAsync` (`:547-560`) additionally calls
  `VerifyFolderIdentityAsync` on top of the `FindChildAsync` that already found
  the folder. Multiply by N images and PLAT-041's ~9 calls per image follows
  arithmetically rather than as a claim.
- **Box credentials are exactly where ADR-0107 requires, and the check is a
  read.** `infra/modules/platform.bicep:382-398` declares `box-config-json` and
  `box-client-secret` as Container App secrets sourced from Key Vault through
  `webIdentity`; `:555-556` sets `Box__ConfigJson` and `Box__ClientSecret` as
  `@Microsoft.KeyVault(SecretUri=…)` references. `:553-554` shows the two
  non-secret Box settings (`Box__UploadUri`, `Box__RootFolderId`), so the
  boundary is visible in one place.
- **The Box SDK is a JWT enterprise credential, which is why no downscoped
  token exists today.**
  `BoxJwtAuthorizationHeaderProvider` (`BoxCaseCustody.cs:116-142`) builds a
  `JwtConfig(ClientId, ClientSecret, JwtKeyId, PrivateKey, PrivateKeyPassphrase)`
  with an `EnterpriseId` and calls `BoxJwtAuth.RetrieveAuthorizationHeaderAsync`.
  Whether that can be exchanged for a file-scoped token is exactly what
  [[FEAT-033]] (plan handle `DSK-07-07`) is for.
- **The existing test evidence is substantial.**
  `tests/Pegasus.IntegrationTests/CustodyOutboxIntegrationTests.cs` (1,796),
  `BoxDocumentContentStoreTests.cs` (433), `ProductionBoxCustodyTests.cs` (872).
  The fake/local adapter harness this ticket needs already exists in all three.
- **Flow record Q4.3 is verbatim and binding.**
  `docs/desktop/01-inventory-and-parity/flow-records.md:301-302`: "PLAT-041
  (resolve folder once per export) must land before the export endpoint is
  exposed to avoid per-image Box calls from a desktop batch." Q4.1 (`:297-299`)
  is [[FEAT-033]]'s question and Q4.2 (`:300`) is the metadata projection this
  ticket's step 4 answers.
- **The projects this ticket writes into do not exist yet.** No
  `src/Pegasus.Contracts`, no `tests/Pegasus.Api.ContractTests`, no `openapi/`,
  no `eng/`, no `src/Pegasus.Desktop.Infrastructure`.

### Assumptions

- **A-07-05-1 — upstream PLAT-041 has *not* landed when this ticket starts.**
  The body records it as still at `review` upstream on 2026-08-24 and outside
  [[FND-023]] (plan handle `DSK-01-10`)'s 32-commit range. Confirmed by:
  re-checking upstream at sync time (plan step 12) and recording the result.
  Breaks if: it has landed — the export and evidence-gallery endpoints may then
  be exposed and the measured call count is taken against the fixed code. The
  ticket ships either way; only those two endpoints are gated.
- **A-07-05-2 — the local stack's Box adapter can be instrumented to count
  calls.** Required by step 12(a). Confirmed by: the fake adapter used in
  `BoxDocumentContentStoreTests.cs` (433) and `ProductionBoxCustodyTests.cs`
  (872). Breaks if: no counting seam exists — then the count is taken from the
  adapter's own request log rather than added to `BoxCaseCustody.cs`, which the
  Guardrails forbid modifying.
- **A-07-05-3 — a gateway revision can be kept running for more than an hour on
  the local stack to take step 12(b)'s token-age check.** PLAT-039's own proof
  records the proving export at ~15:00Z against a 14:35Z revision — inside the
  first hour — so the renewal is deployed but unproved. Confirmed by: recording
  the revision start time and both call times. Breaks if: the check can only be
  taken against production — it is then a read-only observation with no Azure
  write, still inside the Guardrails, but it needs scheduling rather than being
  a test run.
- **A-07-05-4 — [[GWY-008]] (plan handle `DSK-03-08`) publishes the case
  edit-lease endpoints before this ticket's commands can be tested end to end.**
  All four mutations require an `EditLeaseToken`
  (`DocumentContracts.cs:76`, `:151`, `:160`, `:119`). Confirmed by: the
  contract test acquiring a lease first. Breaks if: they have not landed — the
  commands wait rather than inventing a lease-free path.
- **A-07-05-5 — the download route will carry a `{versionId}` segment.** The
  ticket's step 6 route omits it and `DownloadCaseDocumentQuery` requires it.
  Confirmed by: the route shape agreed with [[GWY-011]] (plan handle
  `DSK-03-11`) at plan step 3. Breaks if: the endpoint map fixes a
  current-version-only route — then the endpoint resolves the current version
  from the occurrence and the resolution is documented, because Core will not
  accept an empty `VersionId`.
- **A-07-05-6 — no response field on any of these endpoints carries a Box
  object id.** `DocumentContentWriteResult` (`DocumentContracts.cs:302-304`)
  has a `RemoteId` member, and `docs/current-architecture.md:528` records that
  "durable folder identity is the stored remote folder id". Confirmed by: the
  step-9 contract assertion and a field-by-field DTO review. Breaks if: a
  metadata field the desktop needs is only expressible as a remote id — it is
  then omitted and raised, not passed through (ADR-0107).

## Execution placement

The six-question test from `docs/desktop/00-governance-and-workflow/README.md`
§ 3, answered. This ticket places **two read** and **four command**
responsibilities over Box-backed custody.

| Question | Answer | Evidence, and where a "yes" lands |
| --- | --- | --- |
| Shared authority — must several users see and update the same state? | **yes** | Case documents are one shared custody record several staff act on, and every mutation carries `ExpectedCaseVersion` **and** an `EditLeaseToken` (`DocumentContracts.cs:76`, `:119`, `:151`, `:160`) precisely because two staff can race a case. **Lands in the gateway** — `Pegasus.Web` evolved in place (L-01), no new deployment unit. |
| Unattended execution — must it run with every desktop closed? | **yes**, for the custody effects | The queued custody work these documents land in is drained by `ExternalWorkFunction` / `ExternalPoisonFunction` (`src/Pegasus.Worker/Functions/ExternalWorkFunctions.cs:7`, `:24`) with every desktop closed. **Lands in the existing `src/Pegasus.Worker`** (ADR-0106); this ticket writes no Worker code. Browsing and transfer themselves are a "no" — they are operator-driven, which is why the desktop owns them ([[FEAT-032]], plan handle `DSK-07-06`). |
| Protected credentials — long-lived secret that must not sit on workstations? | **yes**, and it is the whole point of the ticket | The Box JWT enterprise credential: `BoxJwtAuthorizationHeaderProvider` (`BoxCaseCustody.cs:116-142`) needs `ClientId`, `ClientSecret`, `JwtKeyId`, `PrivateKey`, `PrivateKeyPassphrase` and `EnterpriseId`, held as Container App secrets and Key Vault references (`infra/modules/platform.bicep:382-398`, `:555-556`). **Lands behind the gateway** (ADR-0107): bytes stream **through** `Pegasus.Web`, and no Box token, URL or object id reaches the client. Whether a downscoped token could ever change that is [[FEAT-033]] (plan handle `DSK-07-07`)'s question, not this ticket's. |
| Public callback — must an external service call a stable public endpoint? | **no** | Box is called outbound only. There is no Box webhook, no event subscription and no callback; the desktop pulls through the gateway and the gateway pulls from Box. |
| Central enforcement — revocation, permissions, audit, invariant independent of the client? | **yes** | Four the client cannot be trusted with: the Pegasus case/document right checked **before** any Box call (step 5's ordering test); the case edit lease; the "a case exports only in Review" condition, which Core states holds "for every caller rather than only for the one that renders the button" (`DocumentContracts.cs:184-193`); and the canonical metadata and action history written inside the store transaction. **Lands in the gateway.** |
| Measured operational advantage — measured evidence central is materially better? | **yes**, and it is measured against this ticket rather than for it | PLAT-041 measured ~45 sequential Box calls (~18 s) for a five-image export, ~9 per image, of which only 5 move bytes. The arithmetic is visible in the code: `IDocumentContentStore` has no batch method (`DocumentContracts.cs:220-280`), `BoxContentClient` caches nothing (`BoxCaseCustody.cs:144`), and `EnsureDescendantAsync` is awaited at ten sites. Centralising the folder resolve is materially better than N desktop round trips — **and lands in the gateway**, but only after upstream PLAT-041 arrives; until then the export and gallery endpoints stay unexposed (flow record Q4.3). |

**Conclusion.** Five "yes" answers, and every one lands somewhere that already
exists: the reads, commands and byte streaming in the gateway (L-01), the
custody effects in the Worker (ADR-0106), the Box JWT credential behind both
(ADR-0107). Browsing, the transfer queue, the preview pane and the bounded
working cache land in the desktop ([[FEAT-032]], plan handle `DSK-07-06`).
**No new Azure resource and no Azure write** — the credential-boundary evidence
is a name-only Key Vault read, which needs no approval.

## Implications

- **The download route needs a version.** `DownloadCaseDocumentQuery` requires
  `VersionId` (`DocumentContracts.cs:160`) and Core will not accept an empty
  one. Either the route carries `{versionId}` or the endpoint resolves the
  current version from the occurrence and documents that resolution. Settle it
  with [[GWY-011]] (plan handle `DSK-03-11`) at step 3, before the DTOs freeze.
- **Two of the three byte-endpoint conventions do not hold on this path, and
  pretending otherwise is the defect.** The existing page sets
  `Cache-Control: private, no-store` and offers no `ETag` and no range support
  (`Download.cshtml.cs:52-56`). `no-store` is incompatible with `ETag`
  revalidation, and custody evidence should not be cached by an intermediary.
  The honest resolution: keep `no-store`, add **range support** because the
  desktop's transfer queue needs resumable downloads, and use the existing
  `X-Content-SHA256` as the integrity token rather than inventing an `ETag`
  that contradicts the cache header. Record the deviation in the endpoint map.
- **A failure on the download path must stay a `NotFound`.** The page maps five
  exception types to `NotFound` (`:59-66`) so an unauthorised case is
  indistinguishable from a missing one. A gateway that returns
  `urn:pegasus:problem:not-authorized` here would leak case existence — the
  ordering test at step 5 proves the *check* happens first, but the *response*
  must still not disclose.
- **The upload session's whole design is forced by `ReadOnlyMemory<byte>`.**
  Core takes the complete buffer (`DocumentContracts.cs:69`), so the session
  stages bytes, enforces `IntakeEnvelopeLimits.MaximumContentLength` (10 MiB)
  as they arrive, and materialises once at completion. An interrupted session
  must leave no receipt and no partial canonical document — which is
  achievable precisely because Core is only called once, at the end.
- **Export is a lease-taking `POST` bounded at 100 selections and 100 MiB, with
  a `"N"`-format GUID key.** Modelling it as a `GET` would lose the lease, the
  version check and the reason the operator rule exists. Its key format is the
  second on the board that rejects `desk:<guid>`.
- **Step 4's field list is short by six.** `RemovalReason`, `Ordinal`,
  `SourceOccurrenceIdentity`, `RecordedAtUtc`,
  `ThirdPartyVehicleConfirmedAtUtc` and `ThirdPartyVehicleConfirmationReason`
  are all Core members the desktop needs — the last two to render the confirm
  affordance's current state at all. That refines the body rather than
  contradicting it: step 4's own instruction is to project `DocumentOccurrence`
  and `DocumentVersion`, and this is what projecting them yields.
- **The call-budget measurement is arithmetic, not a claim.** No batch method
  (`DocumentContracts.cs:220-280`) × a stateless client (`BoxCaseCustody.cs:144`)
  × ten `EnsureDescendantAsync` sites = per-image ancestry walks. The measurement
  at step 12(a) records the count; it does not discover the cause.
- **Two upstream facts gate parts of this ticket and nothing else on the board
  asserts them.** PLAT-039's renewal is deployed but unproved until a call is
  taken more than an hour after a revision starts; PLAT-041's fix has not
  arrived, so the export and evidence-gallery endpoints stay unexposed. Both
  are recorded as acceptance criteria in the body and both are re-checked at
  sync time.

## Open questions

None that block. Five points that could look like questions have named owners:

- Whether the download route carries `{versionId}` is [[GWY-011]] (plan handle
  `DSK-03-11`)'s endpoint shape, settled at plan step 3 and recorded there. A
  scope boundary, not a question.
- Whether bytes may ever bypass the gateway is [[FEAT-033]] (plan handle
  `DSK-07-07`)'s spike — flow record Q4.1. This ticket's default is streaming
  through the gateway and it adds no direct-transfer path.
- Conflict and version handling on overwrite is [[FEAT-034]] (plan handle
  `DSK-07-08`)'s.
- Custody retry is [[FEAT-028]] (plan handle `DSK-07-02`)'s command and stays
  human-only (`docs/current-architecture.md:571`); this ticket adds no automatic
  path.
- When upstream PLAT-041 lands is outside this board. The ticket's own
  acceptance criterion gates the two affected endpoints on it and records the
  sync check, which is the correct handling of an external dependency rather
  than an open question.
