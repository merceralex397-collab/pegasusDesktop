# Checklist — FEAT-014

One box per plan step, in plan order. Tick with
`set_ticket_doc(doc: "checklist")`; append progress notes below.

- [ ] Read plan row `DSK-05-14`, `vertical-slices.md:488-522`, `screen-specs.md:343-361`, `docs/frd/frd-05-documents-extraction-and-custody.md`, the [[FEAT-033]] spike outcome and the upstream CASE-022 (board [[CASE-002]]) body; call `get_doc_gates FEAT-014`; `take_ticket` on branch `task/dsk-05-14-documents-custody`, worktree `../pegasus-worktrees/dsk-05-14-documents-custody`, from `origin/dev`
- [ ] Append to `research` the six custody handlers with their Core calls, the removal permission rules, the reason requirements, the `RequestUploadPolicy.cs` bounds and how the export builds its archive; record that removal is logical (`Custody.cshtml.cs:160`) and record the SHA read
- [ ] Read the [[FEAT-033]] spike result and record in the plan document which transfer mode this slice implements — gateway streaming or direct downscoped-token transfer; if the spike has not landed, leave the ticket in Preparing
- [ ] Confirm [[FEAT-031]] and [[GWY-011]] have landed the document list, content download with `ETag` and range, the upload-session triple, the soft reasoned delete, custody retry, the request-upload-link pair and export
- [ ] Add the document DTOs to `src/Pegasus.Contracts` carrying file type, size, source, uploader, timestamp, custody state and a canonical-copy indicator
- [ ] Extend `TransferQueueService` in place if [[FEAT-032]] has landed it, or create it to that ticket's pinned shape — bounded queue, five item states, correlation id, byte progress, `CancellationTokenSource`, explicit retry, and a cancelled or failed upload never calling `complete` — and record which case applied
- [ ] Write temporary files to a per-user path with restrictive ACLs and bounded retention, deleted when the transfer completes or is abandoned
- [ ] Extend `CaseDocumentsViewModel` in place with the export, custody-retry and permission-checked removal commands, or create it to [[FEAT-032]]'s pinned members — and record which case applied
- [ ] Build the tab's surface: folder and file list, transfer queue with per-item state, preview pane for safe types only, explicit "open externally", export, permission-gated removal, custody retry, and request-link create and revoke as reasoned commands
- [ ] Render the request-link commands as present, discoverable and **honestly inert**: their unavailability stated in words from [[GWY-011]]'s named `provider-unavailable` problem, with no link, expiry, QR code or copyable URL fabricated anywhere
- [ ] Record in the plan document that the request-link commands become live when [[CASE-002]] activates INT-31, and that this ticket's acceptance is met by the honest inert state
- [ ] Make the canonical-versus-local distinction explicit and show evidence the canonical copy was saved; surface a name collision as a decision with no hidden overwrite
- [ ] Add contract tests per endpoint: success, 401, 403, 409 stale version, replay, reason required on removal, range download — with `Features:DesktopGateway` enabled explicitly
- [ ] Add the contract assertion that no Box credential or token appears in any response
- [ ] Add the contract fact that `POST /api/v1/cases/{id}/request-upload-links` under the production composition returns the named `provider-unavailable` problem, not a 500 and not a link
- [ ] Add transfer-failure tests extending `CustodyOutboxIntegrationTests.cs`: an interrupted large transfer leaves no partial canonical document and is retryable; a cancelled upload leaves no orphan; a failed custody item retries through the human-only command
- [ ] Add view-model tests for queue state transitions, cancel, retry, permission-gated removal, preview-type gating, the canonical indicator, and the request-link commands surfacing the unavailable state with no fabricated link value
- [ ] Measure and record (tier 10) that a transfer in progress does not block navigation and that memory stays steady across repeated large transfers, with the method stated
- [ ] Run the [[TEST-011]] secret scan over the built package and the desktop logs and record the clean result
- [ ] Update `parity-matrix.md` row `PAR-13` and the document rows `PAR-16`/`PAR-17`, recording that the request-upload-link capability is inert until [[CASE-002]] activates it
- [ ] Add the export, custody-retry and permission-checked removal behaviour as a **sub-heading** under [[FEAT-032]]'s documents and transfer-queue section in `docs/frd/frd-13-desktop-operator-experience.md` (or contribute it to [[DUI-013]]), and add the `DSK` rows to `docs/capabilities.md`
- [ ] Run the simplification pass over this branch's diff and record it under a dated `## Simplification pass` heading in the plan document
- [ ] Verification run — `dotnet test` for `Pegasus.Api.ContractTests`, the filtered `Pegasus.IntegrationTests` (with `ProductionCompositionTests` green and unchanged) and `Pegasus.Desktop.ViewModelTests`, plus `pwsh ./tests/Pegasus.Desktop.UITests/ui-tests.ps1 -Script documents`, the axe report, and the performance and secret-scan records; this box produces `proof`
- [ ] Open the PR into `dev`

## Progress notes

(append with `set_ticket_doc(doc: "checklist", append: true)`)
