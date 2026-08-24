# Checklist — FEAT-010

One box per plan step, in plan order, grouped by the sub-slice whose PR carries
it. Tick with `set_ticket_doc(doc: "checklist")`; append progress notes below.

- [ ] Read plan row `DSK-05-10`, `vertical-slices.md:371-407`, `screen-specs.md:248-269`, the mockups under `docs/design/references/mockups/inbox-message-page/` (start with `Dialogs.dc.html`) and `docs/design/README.md:396-421`; call `get_doc_gates FEAT-010`; `take_ticket` on branch `task/dsk-05-10-mail-workspace`, worktree `../pegasus-worktrees/dsk-05-10-mail-workspace`, from `origin/dev`
- [ ] Record the S10a / S10b / S10c split and its per-slice checkpoint in the plan document before writing any code
- [ ] Append to `research` the seven message handlers with their Core calls, the versions each command carries — including the move's four version fields at `Message.cshtml.cs:525-533` — where `reason` is required, and how `UnavailableRetainedMailFolderMover` (`RetainedMailFolderMove.cs:134`) removes the move control; record the SHA characterized

**S10a — list, freshness, preview**

- [ ] Implement `MailListViewModel` over `GET /api/v1/mail?mailbox&folder&page&pageSize&q&deleted` with mailbox and folder scope as dropdowns, newest first, using the [[DUI-007]] data-table pattern
- [ ] Render freshness through the shared vocabulary map ([[DUI-005]]) in the [[DUI-012]] page header, with a coalesced manual refresh calling `POST /api/v1/mail/refresh`
- [ ] Show the Deleted Items 100-newest cap honestly on the surface rather than implying a complete search
- [ ] Implement the preview pane over `GET /api/v1/mail/{id}/preview` rendering inert text only — no remote HTML, no remote content
- [ ] Add S10a contract tests (list scoping, freshness, preview, the cap) with `Features:DesktopGateway` enabled explicitly
- [ ] Add S10a view-model tests (scoping, freshness, preview inertness)
- [ ] Run the S10a simplification pass, record it under its own dated heading, open the S10a PR into `dev`

**S10b — message detail, link and unlink**

- [ ] Implement `MailMessageViewModel` over `GET /api/v1/mail/{id}` (thread, attachments, classification, queue, outcome, association, move result, suggested move) to `screen-specs.md:248-269` — four tabs, Decision card, no Open case button
- [ ] Implement the prepare/link and prepare/unlink command pairs; the confirm step carries message and receipt versions, the case `expectedVersion` and the `editLeaseToken` from the [[FEAT-005]] session
- [ ] Surface `MailClassificationConcurrencyException` and every 409 through the shared [[FEAT-008]] conflict pattern
- [ ] Show exactly `Unlinking this email cancels case <reference>.` in the unlink confirmation, verbatim, with no surrounding explanatory text, on the [[DUI-009]] dialog contract
- [ ] Add S10b contract tests (prepare/confirm for link and unlink: success, 401, 403, 409 stale version, replay)
- [ ] Add S10b view-model tests (prepare-then-confirm flows and the exact unlink sentence)
- [ ] Add the `winapp ui` dialog scripts under `tests/Pegasus.Desktop.UITests` for the link and unlink confirmations, and run the `axe-windows` scan on the list and message screens; attach both artefacts
- [ ] Run the S10b simplification pass, record it under its own dated heading, open the S10b PR into `dev`

**S10c — classification correction and folder move**

- [ ] Implement classification correction over `POST /api/v1/mail/{id}/classification` carrying the classification version
- [ ] Implement the move over `POST /api/v1/mail/{id}/move-to-recommended-folder` carrying all four version fields plus a required `reason`
- [ ] Render the move's three outcomes distinctly and retry an uncertain move with the **same** operation key, never a fresh one
- [ ] Make the move control **absent** — not disabled with an explanation — when the provider port is unavailable
- [ ] Add S10c contract tests (classification versioning, the four-version move, provider-absent behaviour, replay)
- [ ] Add S10c view-model tests (classification version handling, stable operation key on uncertain-move retry, absent move control)
- [ ] Run the S10c simplification pass, record it under its own dated heading, open the S10c PR into `dev`

**Across the slice**

- [ ] Run the tier-12 parity comparison on the local Test/UAT stack against the `MailWorkspaceWebTests.cs` scenarios; record the table in the proof
- [ ] Update `parity-matrix.md` rows `PAR-21` and `PAR-22`
- [ ] Add the mail section to `docs/frd/frd-13-desktop-operator-experience.md` citing FRD-08 (or contribute it to [[DUI-013]] if that file has not landed) and add the `DSK` rows to `docs/capabilities.md`
- [ ] Verification run — `dotnet test` for `Pegasus.Api.ContractTests` and `Pegasus.Desktop.ViewModelTests`, `pwsh ./tests/Pegasus.Desktop.UITests/ui-tests.ps1 -Script mail-link-unlink`, and the tier-12 parity table; this box produces `proof`

## Progress notes

(append with `set_ticket_doc(doc: "checklist", append: true)`)
