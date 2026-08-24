# Files — FEAT-013

Surveyed 2026-08-24 against fork `main` `191ddf33`. Paths marked *(created by …)*
do not exist today — `ls src` returns only `Pegasus.Core`,
`Pegasus.Infrastructure`, `Pegasus.Web`, `Pegasus.Worker`; `ls tests` only
`Pegasus.ArchitectureTests`, `Pegasus.Core.Tests`, `Pegasus.IntegrationTests`.

## Where the change lands

| Path | Why |
|---|---|
| `src/Pegasus.Contracts/` *(created by [[FND-029]], plan handle `DSK-02-04`)* | Upload DTOs, **including a limits payload the client reads at startup** so the client-side check mirrors the server rather than hard-coding a number; plus the upload-session triple, the status payload **consumed from [[GWY-011]] (plan handle `DSK-03-11`) without re-deriving any part of it**, and the group register/attach requests. |
| `src/Pegasus.Desktop.Infrastructure/` *(created by [[FND-031]], plan handle `DSK-02-06`)* | `UploadQueueService`: per-file streaming with progress, cancel, and per-file failure isolation — one rejected file does not abandon the batch. Nothing buffered whole in memory. |
| `src/Pegasus.Desktop/` *(created by [[FND-030]], plan handle `DSK-02-05`)* | `UploadViewModel` with drag-and-drop and a `FileOpenPicker` (packaged WinUI 3 needs explicit window-handle initialization — confirm with `microsoft_docs_search`), client-side extension and size checks driven by the limits payload, per-file rejection reasons from the shared vocabulary, the honest status view, and group register/attach. AutomationIds fixed by `docs/desktop/06-ui-design/screen-specs.md:316-317`: `Upload.Drop`, `Upload.Pick`, `Upload.Submit`, `Upload.Status.State`, `Upload.Group.Register`. |
| `src/Pegasus.Web/` — the `/api/v1` uploads group only | Only where [[GWY-011]] left a gap this slice must close to consume its own contract. Behind `Features:DesktopGateway` ([[GWY-002]], plan handle `DSK-03-02`). |
| `tests/Pegasus.Api.ContractTests/` *(created by [[TEST-001]], plan handle `DSK-08-01`)* | Boundary facts at exactly `MaximumContentLength` and one byte over; at `MaximumBatchFileCount` and one file more; over `MaximumBatchContentLength` refused **before Core**; receipt-token replay returning the existing receipt; a `retry_scheduled` work item's status carrying the `retry_scheduled` state **and** a non-null `dueAtUtc`, never `Received`; a receipt associated to a case with **no** `CaseIntakeLinks` row still returning the resolved case in `caseId`; 401 and 403. |
| `tests/Pegasus.Desktop.ViewModelTests/` *(created by [[TEST-004]], plan handle `DSK-08-04`)* | Queue progress, cancel, per-file rejection, status polling states, the poll interval **derived and clamped** from `dueAtUtc`, Open case versus Open receipt for the linked / associated / neither cases, and group register/attach. |
| `tests/Pegasus.Desktop.UITests/` *(created by [[TEST-006]], plan handle `DSK-08-06`)* | A `winapp ui` script driving the file picker (`-w <HWND>` per the `winui-ui-testing` skill) end to end, plus the `axe-windows` scan from [[TEST-009]] (plan handle `DSK-08-09`). |
| `docs/desktop/01-inventory-and-parity/parity-matrix.md` | Rows `PAR-28` (upload), `PAR-29` (status), `PAR-30` (group). `PAR-31` is **not** touched — it is `legacy path retained` by decision. |
| `docs/desktop/06-ui-design/screen-specs.md` § `Upload` (`:309-317`) | Correct the four-state list `Received, Processing, Complete, Failed` to carry the named retry-scheduled waiting state, and replace "polling every two seconds and manual refresh" with the interval derived from `dueAtUtc` and clamped, keeping manual refresh. The same block already says "completion links to the case or retained receipt", which the seeding dropped and step 8 restores. **This block is this ticket's**; the `endpoint-map.md` § Intake row is [[GWY-011]]'s. |
| `docs/frd/frd-02-intake-and-source-identity.md` | The named retry-scheduled staff-visible state, once step 8 has reconciled the word against the settled vocabulary. |
| `docs/frd/frd-13-desktop-operator-experience.md` *(created by [[DUI-013]], plan handle `DSK-06-13`)* | Upload section. The file does not exist today. |
| `docs/capabilities.md` | `DSK` rows for manual upload and upload groups. |

## Context files

| Path | What it tells the implementer |
|---|---|
| `src/Pegasus.Core/Intake/IntakeContracts.cs:7-56` | **The five real limits**, with the reasoning attached: `MaximumContentLength` 10 MiB per file (`:13`), `MaximumMailboxContentLength` 750 MiB for a whole received message (`:34`, and its remark explains a 16.69 MB forward was refused as `message_too_large` on 2026-08-05 by applying the one-file figure to an envelope), `MaximumBatchFileCount` 20 (`:41`), `MaximumBatchContentLength` = 20 × 10 MiB + overhead (`:49-50`), `MultipartOverhead` 64 KiB (`:56`). The mailbox bound is **not** an upload bound — do not surface it. |
| `src/Pegasus.Web/Pages/Upload.cshtml.cs:35-38,52-64,67-89` | `MaximumSizeLabel` and `MaximumFileCount` are exposed to the view; `IFormFile[] Upload` is a **batch**; the receipt token is re-canonicalised or refused, with the comment "a fresh key would turn a replay into a second receipt"; and the batch-count and per-file checks produce per-file operator sentences. This file is the evidence that the plan prose's "one file" is wrong. |
| `src/Pegasus.Web/Pages/Upload.cshtml:35-36` | The accepted extension list and its MIME types, verbatim — and the deliberate decision to state the accepted types in words because "the operator cannot read" an `accept` attribute. The desktop states them too. |
| `src/Pegasus.Web/Program.cs:525-530` | `FormOptions.MultipartBodyLengthLimit` bound to `MaximumBatchContentLength`, with the comment "Bounded for a whole Upload batch, not one file". This is the envelope check that happens **before** Core. |
| `src/Pegasus.Core/Intake/DurableIntake.cs:96-114` | `QueuedIntakeStatusKinds.FromWorkState` — the defect in one place: `Pending`, `Dispatching`, `Dispatched` **and `RetryScheduled`** all collapse to `Received` (`:104-107`). Its own summary (`:98-102`) states the intent that made it reasonable and that the honest status now overrides. Note the `default` at `:113` throws, so appending a fifth enum value means this switch is part of [[GWY-011]]'s change. |
| `src/Pegasus.Core/Intake/DurableIntake.cs:35-46,79-94,116-121` | `IntakeWorkItem` carries `DueAtUtc` at `:41`; `QueuedIntakeStatusKind` has four values (`:79-85`); `QueuedIntakeStatus` carries `CaseId` at `:93` and **no** due time; `IQueuedIntakeStatusQueries` has one member. The due time exists one level down and is simply not projected — that is the whole of defect (a)'s data half. |
| `src/Pegasus.Infrastructure/Persistence/EfQueuedIntakeStatusQueries.cs:24-28` | The projection reading `CaseId` from `context.CaseIntakeLinks` **alone** — defect (b). |
| `src/Pegasus.Core/Intake/IntakeContracts.cs:406-407` | `IntakeReceipt.CurrentCaseId => ManualAssociationVersion is null ? AcceptedCaseId : ManualLinkedCaseId` — the single association-or-link rule that already exists. [[GWY-011]] resolves `caseId` through it; a third copy here is a stop condition. |
| `src/Pegasus.Infrastructure/Persistence/EfIntakeWorkStore.cs:722` | `IntakeWorkState.RetryScheduled => "retry_scheduled"` — the wire spelling is already decided by the persistence layer, so the new enum value is spelled to match rather than invented. |
| `src/Pegasus.Web/Pages/UploadGroupStatus.cshtml.cs:61,64,130` | `OnGetAsync`, `OnPostRegisterGroupAsync`, `OnPostAttachGroupAsync` — the behaviour the two group commands mirror. |
| `src/Pegasus.Web/Presentation/UploadOutcome.cs` (304 lines), `UploadCaseDecision.cs` (306 lines) | How the web turns an outcome into operator-facing words today. Read before writing a rejection reason — the vocabulary is already settled here. |
| `docs/desktop/06-ui-design/screen-specs.md:309-317` | The Upload block. It says one file, four states, and "polling every two seconds" — all three wrong against the code — and it already says "completion links to the case or retained receipt", which the seeding dropped. |
| `docs/desktop/03-gateway-api-and-data/endpoint-map.md` § `Stays web-only` | Why `Pages/Uploads/Request.cshtml.cs` is not a desktop surface: "Anonymous external audience (request-link actor), antiforgery + PRG; not a desktop surface (proposal §13.11 boundary)". |
| `docs/design/README.md:412-421` | The banned-word list — `intake`, `artifact`, `durable`, `bytes` — and the file's own statement that nothing in CI enforces it. |
| `tests/Pegasus.IntegrationTests/Browser/UploadDropzoneBrowserTests.cs`, `UploadRowsBrowserTests.cs` | The existing web-side dropzone and row evidence named in `PAR-28`. The desktop's equivalent is the `winapp ui` script plus the axe artefact. |

## Ripple effects

- **[[GWY-011]] must land first for three specific facts.** `dueAtUtc`, the
  appended `retry_scheduled` value and the association-or-link `caseId`. If any is
  missing from the generated client, this slice stops and raises it there.
- **Appending a fifth `QueuedIntakeStatusKind` value ripples into
  `FromWorkState`'s `default` branch (`DurableIntake.cs:113`)** and into any
  exhaustive switch on the enum — [[GWY-011]]'s change, not this one, but this
  slice's contract tests will be the ones that notice if it was missed.
- **OpenAPI and the generated client.** The limits payload and the widened status
  change `openapi/pegasus-v1.json`.
- **`tests/Pegasus.IntegrationTests`** — the existing upload web tests must stay
  green; this slice changes no Razor page.
- **[[FEAT-009]] (plan handle `DSK-05-09`)** owns the received-item screen a
  completed receipt opens into and the shared transfer service.
- **`docs/frd/frd-02-intake-and-source-identity.md`** gains the named
  retry-scheduled staff-visible state — a real FRD change, made only after the
  word is reconciled against the settled vocabulary.
- **`docs/frd/frd-13-desktop-operator-experience.md` does not exist yet** — it is
  authored by [[DUI-013]]; contribute the upload section there if it has not
  landed.

## Out of scope

- **`src/Pegasus.Web/Pages/Uploads/Request.cshtml.cs`** — the external
  request-link upload page **stays on the web**. Anonymous external audience,
  antiforgery, PRG; `parity-matrix.md` `PAR-31` records it as
  `legacy path retained`.
- **`src/Pegasus.Core/Intake/**` and `src/Pegasus.Infrastructure/**`.** The
  `QueuedIntakeStatus` / `IQueuedIntakeStatusQueries` / `EfQueuedIntakeStatusQueries`
  change belongs to [[GWY-011]], which owns the status payload.
- **A second case-id resolution.** One rule and it is
  `IntakeReceipt.CurrentCaseId` (`src/Pegasus.Core/Intake/IntakeContracts.cs:406-407`),
  resolved in [[GWY-011]].
- **A client-side inference of the waiting state from a due time.** The desktop
  reads the payload's `retry_scheduled` state value directly.
- **Hard-coded limits.** Every bound comes from the server-supplied limits
  payload.
- **INTK-001's `document.hidden` half.** Moot on the desktop — there is no
  background tab. Recorded rather than reimplemented as a window-visibility rule.
- **The `endpoint-map.md` § Intake row.** [[GWY-011]]'s to write.
- **Any Azure write.** The artifact store is reached only through the gateway.
- **upstream INTK-001 as a fork ticket** — it was not imported and has **no fork
  ticket**; it is absorbed here and in [[GWY-011]]. The board's `INTK-001` is
  upstream INTK-002 and is unrelated, so never write a bare `INTK-001` link.
