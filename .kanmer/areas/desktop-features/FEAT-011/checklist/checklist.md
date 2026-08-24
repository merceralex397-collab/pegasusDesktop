# Checklist — FEAT-011

One box per plan step, in plan order. Tick with
`set_ticket_doc(doc: "checklist")`; append progress notes below.

Two boxes in `open-questions` block `leave-preparing` until they are answered.
That is intended: the ticket body instructs both.

- [ ] Read plan row `DSK-05-11`, `vertical-slices.md:408-433`, `screen-specs.md:287-296` and `docs/frd/frd-03-triage.md`; call `get_doc_gates FEAT-011`; `take_ticket` on branch `task/dsk-05-11-triage`, worktree `../pegasus-worktrees/dsk-05-11-triage`, from `origin/dev`
- [ ] Enumerate every `case` label at `Triage/Details.cshtml.cs:114-210` with its Core command, required parameters and failure paths, and append the table to `research`; record the SHA characterized
- [ ] Get open question 1 (the action count: twelve measured, ten MCP mutations, thirteen in the plan text) answered and recorded before leaving Preparing
- [ ] Write action-matrix characterization facts in `tests/Pegasus.Core.Tests/Triage/` — legal action per `TriageState`, reason-required, payload-required — before moving any rule
- [ ] Move any page-model precondition that is business logic into `src/Pegasus.Core/Triage/` and re-point `Triage/Details.cshtml.cs`, leaving no second implementation
- [ ] Confirm with [[GWY-013]] that every enumerated action has its own route carrying `expectedVersion` and `operationKey`, and that `TriageVersionConflictException` maps to a 409 carrying the current version — stop and raise there if a route is folded
- [ ] Add the triage DTOs to `src/Pegasus.Contracts`: list item, detail (evidence images, reply evidence, response candidates), finding payloads, and one request record per command, with `link_response` taking the poll-outcome and sent-evidence **pair**
- [ ] Implement `TriageListViewModel` over `GET /api/v1/triage?page&state` on the [[DUI-007]] data-table pattern, state as a dropdown, newest first
- [ ] Implement `TriageDetailViewModel` with one command object per action, `CanExecute` from the loaded state and the actor's rights, and no dispatcher string anywhere
- [ ] Wire a [[DUI-009]] reason dialog to every command Core requires a reason for, and the [[FEAT-008]] conflict pattern to every 409; never a generic Close
- [ ] Replace "Assign to me" with Engineer selection (upstream INTK-019) so the assignment command carries the selected engineer's identity
- [ ] Implement `GET /api/v1/triage/{id}/source` as a streamed transfer with progress and cancel, reusing [[FEAT-009]]'s streaming service rather than copying it
- [ ] Get open question 2 (operator: is a Triage evidence surface wanted, and does FRD-03 record it) answered before the evidence section ships
- [ ] Check whether [[FEAT-016]]'s gallery and viewer control has landed; bind to it if so, otherwise define the section, its `Triage.Evidence.*` AutomationIds and its view-model shape and leave rendering to [[FEAT-016]] — and record which case applied in the plan document
- [ ] Confirm the evidence section reads the origin receipt's retained assets over the existing byte endpoints and writes no new custody record
- [ ] Confirm a reader without `StaffAccessRight.PerformCasework` sees the evidence section **absent**, not an error
- [ ] Add contract tests in `tests/Pegasus.Api.ContractTests` applying the [[TEST-002]] seven-case matrix to every enumerated action, with `Features:DesktopGateway` enabled explicitly
- [ ] Add view-model tests in `tests/Pegasus.Desktop.ViewModelTests` for `CanExecute` per state, reason-required commands, finding and supersede payload validation, response candidate selection, and the absent evidence section
- [ ] Run the operator UAT script over the full enumerated action set on the local Test/UAT stack, confirming each outcome and its audit row, and capture the sign-off text and date
- [ ] Update `parity-matrix.md` rows `PAR-23` and `PAR-24`, including `PAR-24`'s command count
- [ ] Add the evidence-images section and the `Triage.Evidence.*` AutomationId to `screen-specs.md:287-296` — this ticket's block only
- [ ] Write `docs/frd/frd-03-triage.md`'s evidence-surface requirement **only if** open question 2 was answered yes
- [ ] Add the triage section to `docs/frd/frd-13-desktop-operator-experience.md` citing FRD-03 (or contribute it to [[DUI-013]]) and add the `DSK` rows to `docs/capabilities.md`
- [ ] Run the simplification pass over this branch's diff and record it under a dated `## Simplification pass` heading in the plan document
- [ ] Verification run — `dotnet test` for `Pegasus.Core.Tests`, `Pegasus.Api.ContractTests` and `Pegasus.Desktop.ViewModelTests`, plus the tier-7 keyboard/axe artefacts and the operator UAT record; this box produces `proof`
- [ ] Open the PR into `dev`

## Progress notes

(append with `set_ticket_doc(doc: "checklist", append: true)`)
