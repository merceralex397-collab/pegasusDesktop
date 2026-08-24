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
refs:
  - docs/frd/frd-02-intake-and-source-identity.md
docs_todo: true
archived: false
created: '2026-08-24T07:54:27.538Z'
updated: '2026-08-24T07:54:27.538Z'
---

## What

Deliver native manual upload: drag-and-drop or file picker within the server's real limits, an upload queue with progress and cancel, honest status polling, and upload-group register and attach — replacing `Pages/Upload`, `Pages/UploadStatus` and `Pages/UploadGroupStatus` for staff.

## Why

Proposal §13.4 and §13.7 require manual intake with a truthful status, and upstream INTK-001 asks for honest queued upload status rather than an implied completion. Today the surface is `src/Pegasus.Web/Pages/Upload.cshtml.cs` (183 lines, `OnPostAsync` over `IGroupedIntakeSubmission`), `Pages/UploadStatus.cshtml.cs` (83 lines) and `Pages/UploadGroupStatus.cshtml.cs` (225 lines, `OnPostRegisterGroupAsync` at `:64` and `OnPostAttachGroupAsync` at `:130`). Limits come from `src/Pegasus.Core/Intake/IntakeContracts.cs` — `MaximumContentLength` 10 MiB per file, `MaximumBatchFileCount` 20, `MaximumBatchContentLength` (20 × 10 MiB + `MultipartOverhead` 64 KiB) — and are enforced server-side before Core. Siblings: [[DSK-05-09]] supplies the received-item screen a receipt opens into, [[DSK-03-11]] the upload-session endpoints.

## Source of truth

- Plan row: `docs/desktop/05-implementation-and-migration/README.md` § 5 — `DSK-05-13`
- Plan detail: `docs/desktop/05-implementation-and-migration/vertical-slices.md` § `S13 · Uploads — manual, status, groups (DSK-05-13)`
- Endpoint map: `docs/desktop/03-gateway-api-and-data/endpoint-map.md` § `Intake (received items), uploads, image intake` (`/uploads/upload-session`, `/uploads/{receiptId}/status`, `/uploads/groups…`) and the `Uploads (external)` row that stays a Razor page
- Screen spec: `docs/desktop/06-ui-design/screen-specs.md` § `§13.4 Intake` → `Upload`
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 13.4 Intake, § 13.7 Documents and evidence
- Repository evidence: `src/Pegasus.Web/Pages/Upload.cshtml.cs:29-100` (`IGroupedIntakeSubmission`, `IFormFile[] Upload`, `ExternalReceiptToken` as the replay key), `src/Pegasus.Web/Pages/Upload.cshtml:36` (the accepted extension list `.eml,.pdf,.docx,.doc,.msg,.jpg,.jpeg,.png` with their MIME types), `src/Pegasus.Web/Pages/UploadStatus.cshtml.cs`, `src/Pegasus.Web/Pages/UploadGroupStatus.cshtml.cs:64` and `:130`, `src/Pegasus.Core/Intake/IntakeContracts.cs:7-56` (`IntakeEnvelopeLimits`), `src/Pegasus.Web/Program.cs:525-530` (`FormOptions.MultipartBodyLengthLimit` bound to `MaximumBatchContentLength`), `src/Pegasus.Web/Presentation/UploadOutcome.cs` (304 lines), `UploadCaseDecision.cs` (306 lines)
- Binding decisions: L-01 the gateway stages the bytes and owns the artifact store; L-02 verification on the local Test/UAT stack; L-04 routing named on the ticket
- Depends on: `DSK-05-09` the received-item screen and the shared transfer service; `DSK-03-11` the upload-session, status and group endpoints

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
5. Add the upload DTOs to `src/Pegasus.Contracts`, including a limits payload the client reads at startup so the client-side check mirrors the server rather than hard-coding a number.
6. Implement `UploadQueueService` in `src/Pegasus.Desktop.Infrastructure`: per-file streaming with progress, cancel, and per-file failure isolation (one rejected file does not abandon the batch). Nothing is buffered whole in memory.
7. Implement `UploadViewModel` in `src/Pegasus.Desktop` with drag-and-drop and a `FileOpenPicker`; use `microsoft_docs_search` for the packaged WinUI 3 window-handle initialization the picker requires. Apply the client-side extension and size checks from the limits payload and show a per-file rejection reason drawn from the shared vocabulary.
8. Implement the status view over `GET /api/v1/uploads/{receiptId}/status`: poll on an interval, render the honest state (Received / Processing / Complete / Failed) and never assume completion — this is upstream INTK-001, absorbed here and shared with [[DSK-05-09]]. A completed receipt offers navigation into the Received item screen.
9. Implement group register and attach as explicit commands with their own operation keys, mirroring `OnPostRegisterGroupAsync` and `OnPostAttachGroupAsync` behaviour.
10. Add contract tests in `tests/Pegasus.Api.ContractTests`: a file at exactly `MaximumContentLength` succeeds and one byte over is refused with a problem; a batch at `MaximumBatchFileCount` succeeds and one file more is refused; a request over `MaximumBatchContentLength` is refused before Core; replay of the same receipt token returns the existing receipt rather than a second one; 401 and 403 cases. Enable `Features:DesktopGateway` explicitly.
11. Add view-model tests in `tests/Pegasus.Desktop.ViewModelTests` for queue progress, cancel, per-file rejection, status polling states, and group register/attach.
12. Add a `winapp ui` script under `tests/Pegasus.Desktop.UITests` driving the file picker (`-w <HWND>` per the `winui-ui-testing` skill) end to end: pick a file, watch progress, reach a terminal status. Run the `axe-windows` scan on the screen and attach both artefacts.
13. Update `docs/desktop/01-inventory-and-parity/parity-matrix.md` for the upload, upload-status and upload-group rows, add the upload section to `docs/frd/frd-13-desktop-operator-experience.md`, run the simplification pass over the branch diff, record it under a dated `## Simplification pass` heading, then open the PR into `dev`.

## Acceptance criteria

- [ ] Files can be added by drag-and-drop or picker, within the limits actually enforced by `IntakeEnvelopeLimits`, mirrored client-side from a server-supplied limits payload.
- [ ] The upload queue shows per-file progress, supports cancel, and isolates a per-file failure.
- [ ] Status is polled and reported honestly; completion is never assumed.
- [ ] Replay of the same receipt token returns the existing receipt, never a second one.
- [ ] Group register and attach work as explicit idempotent commands.
- [ ] The external request-link upload page stays on the web and is untouched.

## Verification

- [ ] `dotnet test ./tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj --configuration Release --no-build` — expected: per-file, batch-count, batch-envelope, replay, 401 and 403 facts pass.
- [ ] `dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release --no-build` — expected: queue, cancel, rejection, status and group facts pass.
- [ ] `pwsh ./tests/Pegasus.Desktop.UITests/ui-tests.ps1 -Script upload` — expected: the file-picker script completes to a terminal status without sleeps.
- [ ] `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --no-build --filter "Category!=Corpus&Category!=Browser"` — expected: existing upload web tests stay green.

## Evidence tier

Tier 5 — Web/API/MCP caller. Tier 7 — Browser/accessibility.
Tier 5 obliges route-level evidence that the real upload endpoints enforce validation, limits and idempotency before Core and translate failures correctly; tier 7 obliges keyboard, focus, progress and error-state evidence from a real run, including the file picker.

## Documentation changes

- `docs/desktop/01-inventory-and-parity/parity-matrix.md` — upload, upload-status and upload-group rows
- `docs/frd/frd-13-desktop-operator-experience.md` — upload section
- `docs/capabilities.md` — `DSK` rows for manual upload and upload groups

## Guardrails

- **Azure**: no write. The artifact store is reached only through the gateway.
- **Scope boundary**: may touch `src/Pegasus.Desktop`, `src/Pegasus.Desktop.Infrastructure`, `src/Pegasus.Contracts`, the `/api/v1` uploads group in `src/Pegasus.Web` and the test projects. `src/Pegasus.Web/Pages/Uploads/Request.cshtml.cs` **stays on the web** — it serves an anonymous external audience with antiforgery and PRG and is not a desktop surface.
- **Traps**: limits are enforced server-side before Core and the client mirrors them from the server — never hard-code a size; the desktop streams, it does not buffer; the receipt token is the replay key and a malformed one is refused rather than regenerated; status must be honest (upstream INTK-001); `Features:DesktopGateway` must be enabled in tests; the plan prose and the code disagree on single-file versus batch — the code wins and the discrepancy is recorded, not silently resolved.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.

## Outcome

_Filled at closeout._
