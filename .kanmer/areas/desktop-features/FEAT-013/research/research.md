# Research — FEAT-013: the real upload limits, and the two dishonesty defects the status view must not re-specify

## Question

What are the upload limits the code actually enforces — the plan prose says one
file, the code accepts a batch — and exactly which parts of upstream INTK-001's
two status-honesty defects are [[GWY-011]] (plan handle `DSK-03-11`)'s payload
work versus this slice's operator surface?

## Current behaviour

Read at fork `main` `191ddf33`. The implementer re-reads and records the SHA
(ticket step 3).

| Surface | `path:line` | What it does |
| --- | --- | --- |
| Upload | `src/Pegasus.Web/Pages/Upload.cshtml.cs:49` `OnPostAsync` (183 lines) | `IGroupedIntakeSubmission` over `IFormFile[] Upload`, with `ExternalReceiptToken` as the replay key |
| Upload status | `src/Pegasus.Web/Pages/UploadStatus.cshtml.cs:56` `OnGetAsync` (83 lines) | `IQueuedIntakeStatusQueries.GetAsync` |
| Upload group | `src/Pegasus.Web/Pages/UploadGroupStatus.cshtml.cs:61` `OnGetAsync`, `:64` `OnPostRegisterGroupAsync`, `:130` `OnPostAttachGroupAsync` (225 lines) | grouped upload use cases |
| External request-link upload | `src/Pegasus.Web/Pages/Uploads/Request.cshtml.cs` | **Stays a Razor page** — anonymous external audience, antiforgery, PRG (`parity-matrix.md` `PAR-31`, `legacy path retained`) |

Parity-matrix rows: **`PAR-28`** (upload), **`PAR-29`** (status), **`PAR-30`**
(group), plus **`PAR-31`** which is deliberately retained on the web. The matrix
holds `PAR-01`…`PAR-46` (`grep -c '^| PAR-' …/parity-matrix.md` → 46).

## Findings

### Facts

Every limit below was read from `src/Pegasus.Core/Intake/IntakeContracts.cs`.

- **`IntakeEnvelopeLimits` spans `:7-56` and defines five constants**, not one:
  `MaximumContentLength = 10 * 1024 * 1024` (`:13`) — "One file uploaded through
  the staff form"; `MaximumMailboxContentLength = 750L * 1024 * 1024` (`:34`) — a
  received mailbox message and every attachment together;
  `MaximumBatchFileCount = 20` (`:41`) — "The most files one staff Upload
  submission may select as a single group"; `MaximumBatchContentLength =
  (MaximumBatchFileCount * (long)MaximumContentLength) + MultipartOverhead`
  (`:49-50`); and `MultipartOverhead = 64 * 1024` (`:56`).
- **The batch is not theoretical — the page enforces it.**
  `src/Pegasus.Web/Pages/Upload.cshtml.cs:38` binds `IFormFile[] Upload`;
  `:67-73` refuses a submission with more than `MaximumBatchFileCount` files
  ("You selected {n} files. Submit 20 or fewer at a time."); `:74-89` refuses each
  empty file and each file over `MaximumContentLength`. `:35` exposes
  `MaximumSizeLabel` and `:37` `MaximumFileCount`.
- **The request envelope is bounded before Core.**
  `src/Pegasus.Web/Program.cs:525-530` configures
  `FormOptions.MultipartBodyLengthLimit = IntakeEnvelopeLimits.MaximumBatchContentLength`
  with the comment "Bounded for a whole Upload batch, not one file".
- **The plan prose and the screen spec both say one file.**
  `docs/desktop/05-implementation-and-migration/vertical-slices.md:461-462` reads
  "upload one file (≤ 10 MiB; …)"; `docs/desktop/06-ui-design/screen-specs.md:311`
  reads "Drop target plus file picker (one file ≤ 10 MiB …)". **The code wins**
  (ticket Traps), and the discrepancy is recorded as an open question rather than
  silently resolved (ticket step 2).
- **The accepted extension list, verbatim from `src/Pegasus.Web/Pages/Upload.cshtml:36`**:
  `.eml,.pdf,.docx,.doc,.msg,.jpg,.jpeg,.png` together with their MIME types
  `message/rfc822`, `application/pdf`,
  `application/vnd.openxmlformats-officedocument.wordprocessingml.document`,
  `application/msword`, `application/vnd.ms-outlook`, `image/jpeg`, `image/png`.
  The page also states them in words at `:35` because "The accepted types are
  stated, not left in an `accept` attribute the operator cannot read."
- **The receipt token is the replay key and a malformed one is refused.**
  `src/Pegasus.Web/Pages/Upload.cshtml.cs:52-64`: `Guid.TryParseExact(token, "N")`
  succeeds and is re-canonicalised, or a model error is added. Its comment says
  why: a fresh key "would turn a replay into a second receipt".
- **Defect (a) is a one-line collapse.**
  `src/Pegasus.Core/Intake/DurableIntake.cs:96-114`
  (`QueuedIntakeStatusKinds.FromWorkState`) maps `IntakeWorkState.Pending`,
  `Dispatching`, `Dispatched` **and `RetryScheduled`** all to
  `QueuedIntakeStatusKind.Received` (`:104-107`). Its own summary at `:98-102`
  says "Everything before a lease is held reads as Received". Meanwhile
  `src/Pegasus.Infrastructure/Persistence/EfIntakeWorkStore.cs:722` already
  persists `IntakeWorkState.RetryScheduled => "retry_scheduled"`.
- **`QueuedIntakeStatusKind` has exactly four values today** —
  `Received = 0`, `Processing = 1`, `Complete = 2`, `Failed = 3`
  (`DurableIntake.cs:79-85`). `QueuedIntakeStatus` (`:87-94`) carries
  `StagedReceiptId`, `SourceFileName`, `ReceivedAtUtc`, `Status`,
  `ProcessedReceiptId`, `CaseId` (`:93`) and `FailureCode` — and **no due time**.
- **The due time exists, one level down.** `IntakeWorkItem` (`:35-46`) carries
  `DueAtUtc` at `:41`. It is simply not projected into the status.
- **Defect (b) is a projection that reads one table.**
  `src/Pegasus.Infrastructure/Persistence/EfQueuedIntakeStatusQueries.cs:24-28`
  resolves `CaseId` from `context.CaseIntakeLinks` alone. The single
  association-or-link rule already exists elsewhere:
  `src/Pegasus.Core/Intake/IntakeContracts.cs:406-407` defines
  `CurrentCaseId => ManualAssociationVersion is null ? AcceptedCaseId :
  ManualLinkedCaseId`. So a receipt auto-associated to an existing case without a
  `CaseIntakeLinks` row reports `caseId = null` today.
- **`IQueuedIntakeStatusQueries` has one member** — `GetAsync`
  (`DurableIntake.cs:116-121`).
- **The `retry_scheduled` wait is long.** The ticket records 30 minutes to 2
  hours; the wire spelling to use is already fixed by
  `EfIntakeWorkStore.cs:722`.
- **`Uploads/Request.cshtml.cs` stays web by decision, not omission.**
  `docs/desktop/03-gateway-api-and-data/endpoint-map.md` § `Stays web-only`
  records "Anonymous external audience (request-link actor), antiforgery + PRG;
  not a desktop surface (proposal §13.11 boundary)", and `parity-matrix.md`
  `PAR-31` marks it `legacy path retained`.
- **Presentation helpers are substantial and already exist.**
  `src/Pegasus.Web/Presentation/UploadOutcome.cs` (304 lines) and
  `UploadCaseDecision.cs` (306 lines).
- **The projects this slice writes into do not exist yet.** `ls src` returns only
  `Pegasus.Core Pegasus.Infrastructure Pegasus.Web Pegasus.Worker`; `ls tests`
  only `Pegasus.ArchitectureTests Pegasus.Core.Tests Pegasus.IntegrationTests`.
  Existing browser evidence: `tests/Pegasus.IntegrationTests/Browser/UploadDropzoneBrowserTests.cs`
  and `UploadRowsBrowserTests.cs` (named in `PAR-28`).

### Assumptions

- **A-05-13-1 — [[GWY-011]] lands all three status-payload facts**: `dueAtUtc`
  (from `IntakeWorkItem.DueAtUtc`, `DurableIntake.cs:41`, null only when the
  receipt has no work item); a fifth `QueuedIntakeStatusKind` value
  `retry_scheduled` appended as `4` with the existing four numeric assignments
  untouched and spelled as `EfIntakeWorkStore.cs:722` persists it; and a `caseId`
  resolved through `IntakeReceipt.CurrentCaseId` (`IntakeContracts.cs:406-407`)
  rather than from `CaseIntakeLinks` alone. Confirmed by: reading the generated
  client at step 5. Breaks if: any of the three is missing — then this slice
  **stops and raises it on [[GWY-011]]**; adding a second case-id resolution or
  inferring the waiting state from a due time is a stop condition.
- **A-05-13-2 — appending `retry_scheduled = 4` is non-breaking for existing
  clients.** The four existing numeric assignments are explicit
  (`DurableIntake.cs:79-85`), so appending does not renumber anything. Confirmed
  by: the existing `Pegasus.IntegrationTests` upload suite staying green. Breaks
  if: a consumer switches exhaustively on the enum without a default — which the
  gateway's own `FromWorkState` does (`:113`), so it must be updated in
  [[GWY-011]]'s change, not here.
- **A-05-13-3 — a settled operator word for the waiting state exists in
  `docs/design/README.md`.** The ticket says to take it from the settled
  vocabulary rather than inventing one. Confirmed by: finding it and reconciling
  it with FRD-02 at step 8. Breaks if: no settled word covers it — then the word
  is an operator question, which is why this ticket's open question is scoped to
  the limits and the word is flagged in the plan's Risks with FRD-02 as its home.
- **A-05-13-4 — `FileOpenPicker` in a packaged WinUI 3 app needs explicit
  window-handle initialization.** Confirmed by: `microsoft_docs_search` at step 7
  (the ticket routes that lookup explicitly). Breaks if: the pattern has changed —
  the doc search is the check, not an assumption carried into code.

## Execution placement

The six-question test from `docs/desktop/00-governance-and-workflow/README.md`
§ 3, answered.

| Question | Answer | Evidence |
| --- | --- | --- |
| Shared authority — must several users see and update the same state? | **yes** | The receipt and its group are shared: `ExternalReceiptToken` is a replay key across submissions (`Upload.cshtml.cs:52-64`) and group register/attach are shared operations. Lands in the gateway (L-01, ADR-0103). |
| Unattended execution — must it run with every desktop closed? | **yes** | The staged receipt is processed by `ProcessQueuedIntake` (`src/Pegasus.Core/Intake/DurableIntake.cs:418`) in `src/Pegasus.Worker`, and a `retry_scheduled` item is due 30 minutes to 2 hours later — long after the uploader has closed the app. Lands in the existing Worker (ADR-0106). This is the reason the status must be honest at all. |
| Protected credentials — long-lived secret that must not sit on workstations? | **yes** | The artifact-store credential behind staging. Lands behind the gateway (ADR-0107); the desktop streams bytes to `/api/v1` and holds no storage credential. |
| Public callback — must an external service call a stable public endpoint? | **no** — for this slice's surface | The one anonymous external upload path, `src/Pegasus.Web/Pages/Uploads/Request.cshtml.cs`, genuinely does need a stable public endpoint and it **stays a Razor page served by the gateway host** (`endpoint-map.md` § `Stays web-only`; `parity-matrix.md` `PAR-31`). It is out of this ticket's scope, so this slice places no public-callback responsibility anywhere. |
| Central enforcement — revocation, permissions, audit, invariant independent of the client? | **yes** | Every `IntakeEnvelopeLimits` bound is enforced server-side before Core (`Program.cs:525-530`, `Upload.cshtml.cs:67-89`), the receipt token's replay semantics are the gateway's, and `StaffAccessRight.PerformCasework` (`src/Pegasus.Core/Identity/StaffAuthorization.cs:10`) gates the whole surface. Lands in the gateway. |
| Measured operational advantage — measured evidence central is materially better? | **no** | No measurement supports staging bytes through a central UI. The opposite requirement is recorded: the desktop streams per file with progress and cancel and never buffers whole. |

Conclusion: four "yes" answers place the staging, the limits, the replay
semantics, the status payload and the audit in the gateway (L-01), and the
queued processing in the existing Worker (ADR-0106). The queue, the picker, the
per-file rejection reasons, the honest waiting state and the poll interval belong
in the desktop. No new Azure resource; no Azure write.

## Implications

- **The client mirrors the server's limits; it does not know them.** A limits
  payload read at startup is the only honest source — hard-coding 10 MiB or 20
  files would drift the first time `IntakeEnvelopeLimits` changed.
- **Per-file failure isolation follows from the batch.** Because the page already
  reports "File {n} is …" per file (`Upload.cshtml.cs:80-88`), one rejected file
  must not abandon the batch in the desktop either.
- **Two of upstream INTK-001's three parts are not this ticket's.** The
  `dueAtUtc` projection, the appended `retry_scheduled` value and the
  association-or-link `caseId` are all [[GWY-011]]'s (its step 6). This slice owns
  the operator-facing **word** for the waiting state, the derived-and-clamped poll
  interval, and the Open case / Open receipt choice. A client-side inference of
  the waiting state from a due time is exactly the second implementation the trap
  forbids.
- **INTK-001's `document.hidden` half is moot.** There is no background tab on the
  desktop; the ticket says to record that rather than inventing a
  window-visibility rule.
- **The poll interval is derived, not fixed.** A fixed two-second poll against a
  retry due in two hours is the waste the honest status removes; the clamp bounds
  are recorded in the plan.
- **The screen spec is wrong in three places and this ticket fixes one block.**
  `screen-specs.md:309-317` says one file, four states, and "polling every two
  seconds"; it also already says "completion links to the case or retained
  receipt", which the seeding dropped and step 8 restores. The matching
  `endpoint-map.md` § Intake row is [[GWY-011]]'s and is not edited here.

## Open questions

One, recorded in the `open-questions` document because the ticket body instructs
it: **the single-file-versus-batch discrepancy** between the plan prose
(`vertical-slices.md:461-462`), the screen spec (`screen-specs.md:311`) and the
code (`IntakeContracts.cs:7-56` plus `Upload.cshtml.cs:38,67-89`). Ticket step 2:
"Record the real limits with evidence in `research`, raise the discrepancy under
the ticket's open questions and get it resolved before leaving Preparing — do not
implement to the plan prose over the code."

Not open questions, recorded here so they are visibly decisions:

- The three status-payload facts are [[GWY-011]]'s to supply; if they are absent
  this slice stops and raises there. A scope boundary with a named owner.
- The operator word for the waiting state is taken from the settled vocabulary in
  `docs/design/README.md` and reconciled with FRD-02 at step 8; the wire value
  stays `retry_scheduled` and belongs to [[GWY-011]].
- upstream INTK-001 has **no fork ticket**. The board's `INTK-001` is upstream
  INTK-002 (intake duplication chores) and is unrelated.
