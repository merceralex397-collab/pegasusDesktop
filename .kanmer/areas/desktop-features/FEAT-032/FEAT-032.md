---
id: FEAT-032
type: ticket
title: >-
  DSK-07-06 · Desktop document browser, transfer queue, preview pane and bounded
  working cache
status: preparing
area: desktop-features
assignee: ''
profile: feature
stageEntered:
  preparing: '2026-08-24T21:31:41.309Z'
labels:
  - desktop-conversion
  - plan-07
  - phase-6
  - tier-7
groups:
  - EPIC-008
  - HZN-007
links: []
blocks:
  - FEAT-034
refs:
  - docs/frd/frd-05-documents-extraction-and-custody.md
docs_todo: true
archived: false
created: '2026-08-24T08:18:48.690Z'
updated: '2026-08-24T21:31:41.309Z'
---

## What

Build the desktop half of Box-backed documents: a native folder/file browser for the case, a transfer queue with per-item progress, cancel and retry, a preview pane for safe types — the single document-preview surface every screen uses, including [[DSK-05-16]]'s gallery — and a bounded local working cache with per-user ACLs and retention, with the local working copy always visibly distinct from the canonical Box copy.

## Why

Proposal § 12.2 assigns the desktop browsing, drag-and-drop upload, preview, a bounded local working cache, transfer progress and cancellation, and conflict communication; § 13.7 adds "evidence that the canonical copy was saved"; § 16.3 requires temporary document files to use per-user access controls and bounded retention. The Phase 6 exit gate is explicit: *large and failed transfers recover safely*. This ticket also carries the document half of the preview surface for the whole desktop: it owns the preview pane, the `Case.Documents.Preview` AutomationId, the safe-type list and the single binding to [[DSK-07-14]]'s isolated document-render path, so that [[DSK-05-16]]'s gallery — which owns images and the image viewer — calls this surface for a document rather than becoming a second renderer of the same bytes. Siblings: [[DSK-07-05]] supplies the broker endpoints, [[DSK-07-07]] decides the transfer mode, [[DSK-07-08]] owns version conflict, [[DSK-05-14]] is the case-workspace slice that hosts this tab and extends the view model and transfer service this ticket owns, and [[DSK-05-16]] is the caller of the preview surface this ticket owns.

## Source of truth

- Plan row: `docs/desktop/07-integrations/README.md` § 5 — `DSK-07-06`
- Screen spec: `docs/desktop/06-ui-design/screen-specs.md` § `§13.7 Documents and evidence — Case workspace › Documents tab` (AutomationIds `Case.Documents.Table`, `Case.Documents.Upload`, `Case.Documents.Queue`, `Case.Documents.Preview`, `Case.Documents.OpenExternally`, `Case.Documents.UploadLink.Create`)
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 12.2 Box, § 13.7 Documents and evidence, § 14.6 Documents, § 16.3 Crash recovery
- Repository evidence: `src/Pegasus.Core/Documents/DocumentContracts.cs:32-60` (the metadata the UI renders), `:145-160` (`LogicallyRemoveDocumentCommand` reason requirement); `src/Pegasus.Web/Pages/Cases/Documents/Download.cshtml.cs:16` (safe-filename and no-sniff behaviour to preserve); `src/Pegasus.Core/Intake/IntakeContracts.cs:7` (`IntakeEnvelopeLimits`)
- Binding decisions: L-01 — all bytes and metadata arrive through `/api/v1`. **ADR-0107** — no Box credential in the package; the desktop never talks to Box directly unless [[DSK-07-07]] proves a short-lived file-scoped token and a follow-up ticket enables it. L-02 — injected-failure evidence is produced on the local Test/UAT stack.
- Depends on: `DSK-07-05` the broker endpoints; `DSK-07-07` the transfer-mode decision; `DSK-06-13` the adopted screen specs; `DSK-02-06` the desktop infrastructure project and bounded cache. `DSK-05-16` is a **caller**, not a dependency: its gallery hands a non-image item to the preview surface this ticket owns, so step 7's entry point must exist and be named before that slice can adopt it.

## Routing

- **Subagent**: `winui-dev` — `.codex/agents/winui-dev.toml`; verification by `pegasus-ui-verifier` — `.codex/agents/pegasus-ui-verifier.toml`
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `winui-design` (`.codex/skills/winui-design/SKILL.md`) → `winui-dev-workflow` (`.codex/skills/winui-dev-workflow/SKILL.md`) → `winui-ui-testing` (`.codex/skills/winui-ui-testing/SKILL.md`) → `winui-code-review` (`.codex/skills/winui-code-review/SKILL.md`) at review
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search` for WinUI drag-and-drop `DataPackageView`, `FileOpenPicker` in packaged apps, and `Windows.Storage.ApplicationData` temporary folders)
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (call `get_doc_gates <id>` before every move; a move crosses at most one gated boundary)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Orient: read the plan row, the Documents screen spec, `docs/frd/frd-05-documents-extraction-and-custody.md`, and the [[DSK-07-07]] spike result. Call `get_doc_gates <this ticket id>`, then `take_ticket` on branch `task/dsk-07-06-document-browser`.
2. Record the transfer mode this ticket implements: **gateway streaming** (the default under this area's § 3 deviation) or direct transfer, only if [[DSK-07-07]] proved a short-lived, file-scoped downscoped token *and* a follow-up ticket enabled it. Do not decide it here; if the spike has not landed, the ticket stays in Preparing.
3. Implement `TransferQueueService` in `src/Pegasus.Desktop.Infrastructure`: a bounded queue of upload and download items, each with `notStarted`/`running`/`succeeded`/`failed`/`cancelled` state, a correlation id, progress in bytes, cancellation via `CancellationTokenSource`, and explicit retry of a failed item (proposal § 16.1). Uploads use the three-step session from [[DSK-07-05]]; a cancelled or failed upload never calls `complete`. This ticket owns that type: if [[DSK-05-14]] landed first it created it under exactly this shape, so extend it in place and add no second transfer service.
4. Implement the bounded working cache in the same project: files land under the packaged app's temporary folder, with per-user ACLs, a total-size cap and an age-based purge on startup and on case close. Every cached file records the `versionId` and `sha256` it came from so a stale copy can be detected. Nothing in the cache is ever presented as the canonical copy.
5. Build `CaseDocumentsViewModel` in `src/Pegasus.Desktop`: the file list from `GET /api/v1/cases/{id}/documents`, a queue pane bound to `TransferQueueService`, and commands for upload (picker and drag-and-drop), download, preview, open externally, reasoned removal and upload-link create/revoke. Follow `winui-code-review`'s MVVM checklist — `[ObservableProperty]` partial properties, `[RelayCommand]`, no UI types in the view model. This ticket owns that type and its view: if [[DSK-05-14]] landed first it created it under exactly these members, so extend it in place and add no second view model.
6. Build `CaseDocumentsView.xaml` to the screen spec with the six AutomationIds listed there, using the data-table pattern from [[DSK-06-07]]. Show name, type, size (MB, one decimal), source, uploader, time and custody state, plus an explicit canonical-copy indicator distinct from any local working copy.
7. **Implement the preview pane — the desktop's single document-preview surface.** Safe types only: images decoded to display size natively. For PDF, use the isolated document-render path from [[DSK-07-14]] — the preview surface is a document viewer, never a WebView hosting Pegasus UI (proposal § 23.2, ADR-0108). "Open externally" is an explicit user command, never automatic. Two things are owned here and nowhere else, and must be written so another slice can call them rather than copy them: (a) **the safe-type list** — one list, in one place, that decides whether a given document may be previewed at all; and (b) **the single binding to [[DSK-07-14]]'s isolated render path**. Expose the surface through a **named entry point** on this view model — one command taking a document record and previewing it in place, returning a refusal the caller can render when the type is outside the safe list — and record that entry point's exact name and signature in the plan document, because [[DSK-05-16]]'s gallery calls it for every non-image item. The interlock, stated in the same words in both bodies: *Document preview has one owner: [[DSK-07-06]]. It owns the Documents-tab preview pane, the `Case.Documents.Preview` AutomationId, the safe-type list, and the single binding to [[DSK-07-14]]'s isolated document-render path. [[DSK-05-16]] owns the image gallery and the image viewer — previous, next, download, close, `Escape` and click-outside — across every image-bearing screen. When a gallery item is a document rather than an image, [[DSK-05-16]] does not render it: it hands off to [[DSK-07-06]]'s preview surface through that ticket's named entry point, and where the type is not on [[DSK-07-06]]'s safe list it offers download through [[DSK-05-14]]'s transfer service instead of opening raw bytes. Two surfaces, one entry point per kind: a second document-render binding, a second safe-type list, or a PDF opened by the gallery itself is a stop condition.* Do not build an image pop-out viewer here — previous/next/close over an image set is [[DSK-05-16]]'s and duplicating it is the same defect in the other direction.
8. Make interruption safe and visible: a failed or cancelled transfer keeps its row with the failure sentence and a retry command; it never silently disappears and never leaves a partial file in the cache. Failed rows persist across navigation within the session.
9. Add view-model tests in `tests/Pegasus.Desktop.ViewModelTests` with a fake API client that injects failures: mid-transfer failure → row `failed` and retryable; cancel → `cancelled` with no `complete` call; a transfer completing while the case list is refreshed → no duplicate row; oversize file → rejected client-side with the same limit the gateway enforces; preview refused for an unsafe type; and the named preview entry point invoked with a document record previewing in place, and with a type outside the safe list returning the refusal a caller renders rather than throwing or opening raw bytes.
10. Add cache tests: the cache stays under its cap, purges by age, writes with restrictive ACLs, and a cached file whose `versionId` no longer matches the server is marked stale rather than shown as current.
11. Build and launch with `.\BuildAndRun.ps1` from the `winui-dev-workflow` skill (async mode, capture the PID), then write and run a `winapp ui` batch script per `winui-ui-testing` covering: picker upload driven through the Win32 file dialog, drag-and-drop upload, progress and cancel, retry of a failed row, preview open and close, keyboard-only traversal of the table and the queue.
12. Run the large-file and memory check on the Test/UAT workstation from [[DSK-08-15]]: a transfer in progress must not block navigation and memory must stay steady across repeated large transfers. Record method and figures in the proof.
13. Run the secret scan from [[DSK-08-11]] over the built package and the desktop log directory; expected: no Box token, JWT, Box URL or Box object id. Then run the simplification pass over the branch diff, record it under a dated `## Simplification pass` heading in the plan document, and open the PR into `dev`.

## Acceptance criteria

- [ ] The local working copy is always visibly distinct from the canonical Box copy, and evidence that the canonical copy was saved is shown.
- [ ] The transfer queue shows progress and supports cancel and retry; a transfer in progress does not block navigation.
- [ ] An interrupted or cancelled transfer leaves no partial canonical document, no orphan session and no partial cache file, and is retryable.
- [ ] The working cache is bounded, purged, per-user ACL'd, and marks stale copies rather than presenting them as current.
- [ ] Preview handles safe types only; the PDF preview uses the isolated document-render path and never hosts Pegasus UI in a WebView.
- [ ] This ticket is the **single owner** of the document-preview path: exactly one safe-type list and exactly one binding to [[DSK-07-14]]'s isolated render path exist in the desktop, both here, and the preview surface is reachable by a named entry point whose name and signature are recorded in the plan document so [[DSK-05-16]]'s gallery calls it for a non-image item instead of rendering one. This ticket builds no image pop-out viewer — previous/next/close over an image set is [[DSK-05-16]]'s.
- [ ] No Box token, URL or object id appears in the package, in a response the desktop stores, or in a log.

## Verification

- [ ] `dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release` — expected: queue state, injected-failure, cancel, oversize, cache and named-preview-entry-point facts pass.
- [ ] `pwsh ./tests/Pegasus.Desktop.UITests/ui-tests.ps1 -AppPid <pid> -Script documents` — expected: upload, drag-and-drop, cancel, retry, preview and keyboard assertions pass; screenshots attached.
- [ ] `dotnet test ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj --configuration Release` — expected: the no-WebView-hosting-Pegasus-UI and no-Box-SDK-in-desktop facts pass, and exactly one desktop reference to the isolated document-render path exists — this ticket's.
- [ ] Large-file run and secret scan recorded in the ticket proof — expected: navigation unblocked, steady memory, clean scan.

## Evidence tier

Tier 7 — Browser/accessibility (desktop equivalent: real authenticated workflow, keyboard, focus and error behaviour, semantic labels, text-plus-colour states).
Tier 7 obliges a real run on a real package; an automated scan does not replace the keyboard and assistive-technology walk.

## Documentation changes

- `docs/frd/frd-13-desktop-operator-experience.md` — documents and transfer-queue section (this ticket authors the section; [[DSK-05-14]] adds the export, custody-retry and permission-checked removal behaviour as a sub-heading inside it, and [[DSK-05-16]]'s viewer contract reaches FRD-13 through [[DSK-06-13]]'s adoption of `screen-specs.md` § `§13.7 Documents and evidence`, not through a second section here)
- `docs/frd/frd-05-documents-extraction-and-custody.md` — desktop behaviour clause (local working copy vs canonical, transfer states)
- `docs/desktop/01-inventory-and-parity/parity-matrix.md` — document rows advance to `implemented`

## Guardrails

- **Azure**: no write.
- **Scope boundary**: may touch `src/Pegasus.Desktop`, `src/Pegasus.Desktop.Infrastructure`, `tests/Pegasus.Desktop.ViewModelTests`, `tests/Pegasus.Desktop.UITests`. Must not reference `src/Pegasus.Infrastructure/Custody/` or `Box.Sdk.Gen` from any desktop project, and must not add gateway endpoints (that is [[DSK-07-05]]). The `ImageGallery`, `ImageThumbnailService` and `ImageViewer` types are **[[DSK-05-16]]'s**; this ticket consumes the gallery where the tab shows images and builds none of them.
- **Traps**: ADR-0107 — a Box credential in the package is a defect, and so is a "temporary" long-lived URL left in a log; the working cache must never masquerade as custody; no hidden overwrite — a collision is a decision, and version conflict is [[DSK-07-08]]; custody retry stays human-only; the PDF preview must not become a WebView hosting Pegasus UI (proposal § 23.2). One view model per screen and one transfer service: this ticket owns `CaseDocumentsViewModel`, `CaseDocumentsView.xaml` and `TransferQueueService`, [[DSK-05-14]] extends them; a second view model for the same screen, or a second transfer service, is a stop condition. **Document preview has one owner: [[DSK-07-06]].** It owns the Documents-tab preview pane, the `Case.Documents.Preview` AutomationId, the safe-type list, and the single binding to [[DSK-07-14]]'s isolated document-render path. [[DSK-05-16]] owns the image gallery and the image viewer — previous, next, download, close, `Escape` and click-outside — across every image-bearing screen. When a gallery item is a document rather than an image, [[DSK-05-16]] does not render it: it hands off to [[DSK-07-06]]'s preview surface through that ticket's named entry point, and where the type is not on [[DSK-07-06]]'s safe list it offers download through [[DSK-05-14]]'s transfer service instead of opening raw bytes. **Two surfaces, one entry point per kind: a second document-render binding, a second safe-type list, or a PDF opened by the gallery itself is a stop condition.**
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.

## Outcome

_Filled at closeout._
