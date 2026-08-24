---
id: FEAT-013
type: ticket
title: 'DSK-05-13 · S13 Uploads (manual, status, groups)'
status: backlog
area: desktop-features
assignee: ''
profile: feature
labels:
  - desktop-conversion
  - plan-05
  - phase-5
  - tier-5
  - tier-7
groups:
  - EPIC-006
  - HZN-006
links: []
blocks:
  - FEAT-022
  - FEAT-025
refs:
  - docs/frd/frd-02-intake-and-source-identity.md
docs_todo: true
archived: false
created: '2026-08-24T07:54:27.538Z'
updated: '2026-08-24T12:25:56.385Z'
---

## What

Deliver native manual upload: drag-and-drop or file picker within the server's real limits, an upload queue with progress and cancel, honest status polling, and upload-group register and attach — replacing `Pages/Upload`, `Pages/UploadStatus` and `Pages/UploadGroupStatus` for staff.

## Why

Proposal §13.4 and §13.7 require manual intake with a truthful status, and upstream INTK-001 asks for honest queued upload status rather than an implied completion. Today the surface is `src/Pegasus.Web/Pages/Upload.cshtml.cs` (183 lines, `OnPostAsync` over `IGroupedIntakeSubmission`), `Pages/UploadStatus.cshtml.cs` (83 lines) and `Pages/UploadGroupStatus.cshtml.cs` (225 lines, `OnPostRegisterGroupAsync` at `:64` and `OnPostAttachGroupAsync` at `:130`). Limits come from `src/Pegasus.Core/Intake/IntakeContracts.cs` — `MaximumContentLength` 10 MiB per file, `MaximumBatchFileCount` 20, `MaximumBatchContentLength` (20 × 10 MiB + `MultipartOverhead` 64 KiB) — and are enforced server-side before Core. INTK-001 names two specific dishonesty defects the desktop would otherwise re-specify: a `retry_scheduled` work item reads as **Received** while its retry is 30 minutes to 2 hours away, and the status page links only through `CaseIntakeLinks` so a receipt auto-associated to an existing case offers "Open receipt" rather than "Open case". Siblings: [[DSK-05-09]] supplies the received-item screen a receipt opens into, [[DSK-03-11]] the upload-session and status endpoints and the status payload that carries both fixes.

## Source of truth

- Plan row: `docs/desktop/05-implementation-and-migration/README.md` § 5 — `DSK-05-13`
- Plan detail: `docs/desktop/05-implementation-and-migration/vertical-slices.md` § `S13 · Uploads — manual, status, groups (DSK-05-13)`
- Endpoint map: `docs/desktop/03-gateway-api-and-data/endpoint-map.md` § `Intake (received items), uploads, image intake` (`/uploads/upload-session`, `/uploads/{receiptId}/status`, `/uploads/groups…`) and the `Uploads (external)` row that stays a Razor page
- Screen spec: `docs/desktop/06-ui-design/screen-specs.md` § `§13.4 Intake` → `Upload`
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 13.4 Intake, § 13.7 Documents and evidence
- Repository evidence: `src/Pegasus.Web/Pages/Upload.cshtml.cs:29-100` (`IGroupedIntakeSubmission`, `IFormFile[] Upload`, `ExternalReceiptToken` as the replay key), `src/Pegasus.Web/Pages/Upload.cshtml:36` (the accepted extension list `.eml,.pdf,.docx,.doc,.msg,.jpg,.jpeg,.png` with their MIME types), `src/Pegasus.Web/Pages/UploadStatus.cshtml.cs`, `src/Pegasus.Web/Pages/UploadGroupStatus.cshtml.cs:64` and `:130`, `src/Pegasus.Core/Intake/IntakeContracts.cs:7-56` (`IntakeEnvelopeLimits`), `:406-407` (`IntakeReceipt.CurrentCaseId`, the single association-or-link case-id rule), `src/Pegasus.Core/Intake/DurableIntake.cs:35-46` (`IntakeWorkItem` with `DueAtUtc` at `:41`), `:79-85` (`QueuedIntakeStatusKind` — only `Received`, `Processing`, `Complete`, `Failed`), `:87-94` (`QueuedIntakeStatus`, which carries `CaseId` at `:93` but no due time), `:96-114` (`QueuedIntakeStatusKinds.FromWorkState`, which collapses `IntakeWorkState.RetryScheduled` into `Received`), `:116-121` (`IQueuedIntakeStatusQueries`), `src/Pegasus.Infrastructure/Persistence/EfQueuedIntakeStatusQueries.cs:24-28` (the projection that reads `CaseId` from `CaseIntakeLinks` alone), `src/Pegasus.Infrastructure/Persistence/EfIntakeWorkStore.cs:722` (`IntakeWorkState.RetryScheduled` → `"retry_scheduled"`), `src/Pegasus.Web/Program.cs:525-530` (`FormOptions.MultipartBodyLengthLimit` bound to `MaximumBatchContentLength`), `src/Pegasus.Web/Presentation/UploadOutcome.cs` (304 lines), `UploadCaseDecision.cs` (306 lines)
- Upstream evidence: **upstream `INTK-001`** *Make queued upload status honest for retry-scheduled work and auto-associated receipts* — **absorbed here and in [[DSK-03-11]]; it was not imported, so there is no fork ticket for it, and the board's `INTK-001` is a different ticket (upstream INTK-002, the intake duplication chores). Never cite it as a bare board id.** Its approach: project `WorkItem.DueAtUtc` in `IQueuedIntakeStatusQueries` and derive the refresh interval from it, or add an explicit staff-visible retry-scheduled state; resolve the case id the way `IntakeReceipt.CurrentCaseId` does (link **or** association) rather than a third copy. This ticket's acceptance takes **both** options, so [[DSK-03-11]] supplies both on the wire (its step 6) and this ticket renders them.
- Binding decisions: L-01 the gateway stages the bytes and owns the artifact store; L-02 verification on the local Test/UAT stack; L-04 routing named on the ticket
- Depends on: `DSK-05-09` the received-item screen and the shared transfer service; `DSK-03-11` the upload-session, status and group endpoints, and the three status-payload facts step 5 names

## Routing

- **Subagent**: `winui-dev` — `.codex/agents/winui-dev.toml`; `pegasus-gateway-dev` — `.codex/agents/pegasus-gateway-dev.toml`; `pegasus-test-engineer` — `.codex/agents/pegasus-test-engineer.toml`; `pegasus-ui-verifier` — `.codex/agents/pegasus-ui-verifier.toml` (file-picker automation)
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `minimal-api-file-upload` (dotnet/skills `98f84851`, `plugins/dotnet-aspnetcore/skills/minimal-api-file-upload/SKILL.md`) → `winui-dev-workflow` (`.codex/skills/winui-dev-workflow/SKILL.md`) → `winui-design` (`.codex/skills/winui-design/SKILL.md`) → `winui-ui-testing` (`.codex/skills/winui-ui-testing/SKILL.md`) → `run-tests` → `winui-code-review` at review
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search` for `FileOpenPicker` window-handle initialization in a packaged WinUI 3 app)
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (call `get_doc_gates <id>` before every move; a move crosses at most one gated boundary)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Orient: read the plan row, `vertical-slices.md` § S13 and the screen spec `Upload` section. Call `get_doc_gates <this ticket id>`, then `take_ticket` with branch `task/dsk-05-13-uploads` and worktree `../pegasus-worktrees/dsk-05-13-uploads` from `origin/dev`.
2. **Resolve the limit discrepancy first.** Read `src/Pegasus.Core/Intake/IntakeContracts.cs:7-56` and `src/Pegasus.Web/Pages/Upload.cshtml.cs`. The plan text describes a one-file upload; the code accepts a batch of up to `MaximumBatchFileCount` (20) files, each bounded by `MaximumContentLength` (10 MiB), with the request bounded by `MaximumBatchContentLength` (20 × 10 MiB + 64 KiB `MultipartOverhead`). Record the real limits with evidence in `research`, raise the discrepancy under the ticket's open questions and get it resolved before leaving Preparing — do not implement to the plan prose over the code.
3. Record the accepted extension list verbatim from `src/Pegasus.Web/Pages/Upload.cshtml:36` — `.eml`, `.pdf`, `.docx`, `.doc`, `.msg`, `.jpg`, `.jpeg`, `.png` and their MIME types — and the replay semantics of `ExternalReceiptToken` (a malformed token is refused, never silently regenerated, so a replay never becomes a second receipt). Record the SHA read.
4. Confirm the endpoints from [[DSK-03-11]]: `POST /api/v1/uploads/upload-session` → `PUT` bytes → `POST …/complete` (complete is idempotent on the receipt token), `GET /api/v1/uploads/{receiptId}/status`, `POST /api/v1/uploads/groups`, `POST /api/v1/uploads/groups/{gid}/attach`, `GET /api/v1/uploads/groups/{gid}`. Load `minimal-api-file-upload` and confirm the server enforces every limit before Core is called.
5. Add the upload DTOs to `src/Pegasus.Contracts`, including a limits payload the client reads at startup so the client-side check mirrors the server rather than hard-coding a number. **Check first that [[DSK-03-11]] has landed the widened upload-status payload, and restate its shape from that ticket's step 6 before writing a line of client code — it owns `GET /api/v1/uploads/{receiptId}/status`, `QueuedIntakeStatus` and `IQueuedIntakeStatusQueries`, and this ticket consumes the payload without re-deriving any part of it.** Its three facts, which `QueuedIntakeStatus` does not carry today, are: (a) `dueAtUtc` — the work item's `DueAtUtc` (`DurableIntake.cs:41`) in UTC, null only when the receipt has no work item; (b) a fifth state value `retry_scheduled`, appended to `QueuedIntakeStatusKind` as `4` with the existing four numeric assignments untouched, spelled as `EfIntakeWorkStore.cs:722` already persists it, so a scheduled retry is no longer collapsed into `Received`; and (c) `caseId` — the same member as today (`DurableIntake.cs:93`) with corrected semantics: the value `IntakeReceipt.CurrentCaseId` yields (`IntakeContracts.cs:406-407`), covering a link **or** an association, resolved in `EfQueuedIntakeStatusQueries` rather than from `CaseIntakeLinks` alone. If any of the three is missing from the generated client, stop and raise it on [[DSK-03-11]] — do not add a second case-id resolution, a client-side inference of the waiting state, or a local copy of either rule here.
6. Implement `UploadQueueService` in `src/Pegasus.Desktop.Infrastructure`: per-file streaming with progress, cancel, and per-file failure isolation (one rejected file does not abandon the batch). Nothing is buffered whole in memory.
7. Implement `UploadViewModel` in `src/Pegasus.Desktop` with drag-and-drop and a `FileOpenPicker`; use `microsoft_docs_search` for the packaged WinUI 3 window-handle initialization the picker requires. Apply the client-side extension and size checks from the limits payload and show a per-file rejection reason drawn from the shared vocabulary.
8. Implement the status view over `GET /api/v1/uploads/{receiptId}/status` and make it honest — this is upstream INTK-001, absorbed here and shared with [[DSK-05-09]] and [[DSK-03-11]]. Two specific defects must not be re-specified. (a) `QueuedIntakeStatusKinds.FromWorkState` (`DurableIntake.cs:96-114`) collapses `IntakeWorkState.RetryScheduled` into `Received`, and the retry is due 30 minutes to 2 hours away: a receipt whose work item is `retry_scheduled` is shown as a **named waiting state**, never as Received, reading the payload's `retry_scheduled` state value directly — never inferring it from a due time — and the poll interval is derived from the payload's `dueAtUtc` (clamped — record the bounds in `plan`) rather than fixed at two seconds. **This ticket owns the operator-facing word only**: take the waiting word from the settled operator vocabulary in `docs/design/README.md` rather than inventing one, and reconcile it with FRD-02; the wire value stays `retry_scheduled` and belongs to [[DSK-03-11]]. (b) A completed receipt offers **Open case** whenever a case exists by link **or** by association — that is exactly what the payload's `caseId` now means, resolved once in [[DSK-03-11]] through `IntakeReceipt.CurrentCaseId` — offering **Open receipt** only when `caseId` is null. Do not re-resolve the case id here. INTK-001's `document.hidden` half is moot on the desktop, where there is no background tab; record that rather than inventing a window-visibility rule.
9. Implement group register and attach as explicit commands with their own operation keys, mirroring `OnPostRegisterGroupAsync` and `OnPostAttachGroupAsync` behaviour.
10. Add contract tests in `tests/Pegasus.Api.ContractTests`: a file at exactly `MaximumContentLength` succeeds and one byte over is refused with a problem; a batch at `MaximumBatchFileCount` succeeds and one file more is refused; a request over `MaximumBatchContentLength` is refused before Core; replay of the same receipt token returns the existing receipt rather than a second one; a `retry_scheduled` work item's status carries the `retry_scheduled` state and a non-null `dueAtUtc`, never `Received`; a receipt associated to a case without a `CaseIntakeLinks` row still returns the resolved case in `caseId`; 401 and 403 cases. Enable `Features:DesktopGateway` explicitly.
11. Add view-model tests in `tests/Pegasus.Desktop.ViewModelTests` for queue progress, cancel, per-file rejection, status polling states, the poll interval derived and clamped from `dueAtUtc`, Open case versus Open receipt for the linked, associated and neither cases, and group register/attach.
12. Add a `winapp ui` script under `tests/Pegasus.Desktop.UITests` driving the file picker (`-w <HWND>` per the `winui-ui-testing` skill) end to end: pick a file, watch progress, reach a terminal status. Run the `axe-windows` scan on the screen and attach both artefacts.
13. Update `docs/desktop/01-inventory-and-parity/parity-matrix.md` for the upload, upload-status and upload-group rows, add the upload section to `docs/frd/frd-13-desktop-operator-experience.md`, run the simplification pass over the branch diff, record it under a dated `## Simplification pass` heading, then open the PR into `dev`.

## Acceptance criteria

- [ ] Files can be added by drag-and-drop or picker, within the limits actually enforced by `IntakeEnvelopeLimits`, mirrored client-side from a server-supplied limits payload.
- [ ] The upload queue shows per-file progress, supports cancel, and isolates a per-file failure.
- [ ] Status is polled and reported honestly; completion is never assumed.
- [ ] A receipt whose work item is `retry_scheduled` is shown as a named waiting state, never as Received, read from the payload's `retry_scheduled` state value rather than inferred, and the poll interval is derived from the payload's `dueAtUtc` (clamped) rather than fixed at two seconds; and a completed receipt offers Open case whenever the payload's `caseId` is present — which [[DSK-03-11]] resolves through the single `IntakeReceipt.CurrentCaseId` path, covering a link **or** an association — offering Open receipt only when `caseId` is null. No second case-id resolution and no client-side state inference exist anywhere in this slice.
- [ ] Replay of the same receipt token returns the existing receipt, never a second one.
- [ ] Group register and attach work as explicit idempotent commands.
- [ ] The external request-link upload page stays on the web and is untouched.

## Verification

- [ ] `dotnet test ./tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj --configuration Release --no-build` — expected: per-file, batch-count, batch-envelope, replay, `retry_scheduled`-state, resolved-`caseId`, 401 and 403 facts pass.
- [ ] `dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release --no-build` — expected: queue, cancel, rejection, status, derived-poll-interval, Open case / Open receipt and group facts pass.
- [ ] `pwsh ./tests/Pegasus.Desktop.UITests/ui-tests.ps1 -Script upload` — expected: the file-picker script completes to a terminal status without sleeps.
- [ ] `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --no-build --filter "Category!=Corpus&Category!=Browser"` — expected: existing upload web tests stay green.

## Evidence tier

Tier 5 — Web/API/MCP caller. Tier 7 — Browser/accessibility.
Tier 5 obliges route-level evidence that the real upload endpoints enforce validation, limits and idempotency before Core and translate failures correctly; tier 7 obliges keyboard, focus, progress and error-state evidence from a real run, including the file picker.

## Documentation changes

- `docs/desktop/01-inventory-and-parity/parity-matrix.md` — upload, upload-status and upload-group rows
- `docs/desktop/06-ui-design/screen-specs.md` § `Upload — replaces Pages/Upload.cshtml.cs, UploadStatus, UploadGroupStatus` (line 314) — correct the four-state list `Received, Processing, Complete, Failed` to carry the named retry-scheduled waiting state, and replace "polling every two seconds and manual refresh" with the interval derived from `dueAtUtc` and clamped, keeping manual refresh; the same block already says "completion links to the case or retained receipt", which the seeding dropped and step 8 now restores (upstream INTK-001). This line is this ticket's; the matching `endpoint-map.md` § Intake row is [[DSK-03-11]]'s and is not edited here.
- `docs/frd/frd-02-intake-and-source-identity.md` — the named retry-scheduled staff-visible state, once step 8 has reconciled the word against the settled vocabulary
- `docs/frd/frd-13-desktop-operator-experience.md` — upload section
- `docs/capabilities.md` — `DSK` rows for manual upload and upload groups

## Guardrails

- **Azure**: no write. The artifact store is reached only through the gateway.
- **Scope boundary**: may touch `src/Pegasus.Desktop`, `src/Pegasus.Desktop.Infrastructure`, `src/Pegasus.Contracts`, the `/api/v1` uploads group in `src/Pegasus.Web` and the test projects. `src/Pegasus.Web/Pages/Uploads/Request.cshtml.cs` **stays on the web** — it serves an anonymous external audience with antiforgery and PRG and is not a desktop surface. The `QueuedIntakeStatus` / `IQueuedIntakeStatusQueries` / `EfQueuedIntakeStatusQueries` projection change belongs to [[DSK-03-11]], which owns the status payload; `src/Pegasus.Core/Intake/**` and `src/Pegasus.Infrastructure/**` are out of bounds here.
- **Traps**: limits are enforced server-side before Core and the client mirrors them from the server — never hard-code a size; the desktop streams, it does not buffer; the receipt token is the replay key and a malformed one is refused rather than regenerated; status must be honest (upstream INTK-001) — a `retry_scheduled` item shown as Received is the defect this slice exists to avoid re-specifying, and a fixed two-second poll against a retry due in two hours is the waste that goes with it. **[[DSK-03-11]] is the single owner of the upload-status payload**: it supplies `dueAtUtc`, the appended `retry_scheduled` state value and the association-or-link `caseId`, and this slice reads all three — one case-id resolution and it is `IntakeReceipt.CurrentCaseId`'s, resolved there; a third copy here, or a client-side guess at the waiting state from a due time, is a stop condition. This ticket owns only the operator-facing *word* for the waiting state. **Upstream ids and fork board ids do not match**: the board's `INTK-001` is upstream INTK-002 and is unrelated to this ticket — upstream INTK-001 has no fork ticket and is absorbed here and in [[DSK-03-11]], so never write a bare `INTK-001` link. `Features:DesktopGateway` must be enabled in tests; the plan prose and the code disagree on single-file versus batch — the code wins and the discrepancy is recorded, not silently resolved.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.

## Outcome

_Filled at closeout._
