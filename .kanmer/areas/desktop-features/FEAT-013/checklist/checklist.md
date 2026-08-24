# Checklist — FEAT-013

One box per plan step, in plan order. Tick with
`set_ticket_doc(doc: "checklist")`; append progress notes below.

One box in `open-questions` blocks `leave-preparing` until the limit discrepancy
is resolved. That is intended: the ticket body instructs it.

- [ ] Read plan row `DSK-05-13`, `vertical-slices.md:459-487` and `screen-specs.md:309-317`; call `get_doc_gates FEAT-013`; `take_ticket` on branch `task/dsk-05-13-uploads`, worktree `../pegasus-worktrees/dsk-05-13-uploads`, from `origin/dev`
- [ ] Record the five real limits from `IntakeContracts.cs:7-56` with evidence in `research`, noting that `MaximumMailboxContentLength` is a received-message bound and not an upload bound
- [ ] Get the open question (single file per the plan prose versus a batch of up to 20 per the code) resolved before leaving Preparing
- [ ] Record the accepted extension list verbatim from `Upload.cshtml:36` with its MIME types, and `ExternalReceiptToken`'s replay semantics from `Upload.cshtml.cs:52-64`; record the SHA read
- [ ] Confirm [[GWY-011]] has landed the upload-session triple, the status endpoint and the three group endpoints, and that every limit is enforced server-side before Core
- [ ] Add the upload DTOs to `src/Pegasus.Contracts`, including a limits payload the client reads at startup
- [ ] Confirm [[GWY-011]] has landed all three widened-status facts — `dueAtUtc`, the appended `retry_scheduled` state value, and the association-or-link `caseId` — and restate the shape in the plan document; stop and raise there if any is missing
- [ ] Implement `UploadQueueService` in `src/Pegasus.Desktop.Infrastructure` with per-file streaming, progress, cancel and per-file failure isolation — nothing buffered whole
- [ ] Implement `UploadViewModel` with drag-and-drop and a `FileOpenPicker`, using `microsoft_docs_search` for the packaged WinUI 3 window-handle initialization
- [ ] Apply client-side extension and size checks driven by the limits payload, with per-file rejection reasons from the shared vocabulary — no hard-coded number anywhere
- [ ] Show a receipt whose work item is `retry_scheduled` as a named waiting state, never as Received, reading the payload's state value directly rather than inferring it from a due time
- [ ] Derive and clamp the poll interval from `dueAtUtc` (minimum 2 s, maximum 60 s, null falls back to the minimum) instead of polling every two seconds, keeping manual refresh
- [ ] Take the waiting word from the settled vocabulary in `docs/design/README.md` and reconcile it with FRD-02; leave the wire value `retry_scheduled` to [[GWY-011]]
- [ ] Offer **Open case** whenever the payload's `caseId` is present and **Open receipt** only when it is null, adding no second case-id resolution
- [ ] Record in the plan document that INTK-001's `document.hidden` half is moot on the desktop, inventing no window-visibility rule
- [ ] Implement group register and attach as explicit commands with their own operation keys
- [ ] Add contract tests for the three limit boundaries: exactly `MaximumContentLength` and one byte over; exactly `MaximumBatchFileCount` and one file more; over `MaximumBatchContentLength` refused before Core
- [ ] Add contract tests for receipt-token replay returning the existing receipt, the `retry_scheduled` state with a non-null `dueAtUtc`, the resolved `caseId` for an associated receipt with no `CaseIntakeLinks` row, and 401/403 — with `Features:DesktopGateway` enabled explicitly
- [ ] Add view-model tests for queue progress, cancel, per-file rejection, polling states, the clamped poll interval at both bounds, Open case / Open receipt across linked / associated / neither, and group register/attach
- [ ] Add the `winapp ui` file-picker script under `tests/Pegasus.Desktop.UITests` driving pick → progress → terminal status without sleeps, and run the `axe-windows` scan; attach both artefacts
- [ ] Update `parity-matrix.md` rows `PAR-28`, `PAR-29` and `PAR-30`, leaving `PAR-31` untouched
- [ ] Correct the `screen-specs.md:309-317` Upload block — named waiting state in the state list, derived-and-clamped interval replacing "polling every two seconds", manual refresh kept — and edit no other block
- [ ] Add the named retry-scheduled staff-visible state to `docs/frd/frd-02-intake-and-source-identity.md`
- [ ] Add the upload section to `docs/frd/frd-13-desktop-operator-experience.md` (or contribute it to [[DUI-013]]) and add the `DSK` rows to `docs/capabilities.md`
- [ ] Run the simplification pass over this branch's diff and record it under a dated `## Simplification pass` heading in the plan document
- [ ] Verification run — `dotnet test` for `Pegasus.Api.ContractTests`, `Pegasus.Desktop.ViewModelTests` and the filtered `Pegasus.IntegrationTests`, plus `pwsh ./tests/Pegasus.Desktop.UITests/ui-tests.ps1 -Script upload` and the axe artefact; this box produces `proof`
- [ ] Open the PR into `dev`

## Progress notes

(append with `set_ticket_doc(doc: "checklist", append: true)`)
