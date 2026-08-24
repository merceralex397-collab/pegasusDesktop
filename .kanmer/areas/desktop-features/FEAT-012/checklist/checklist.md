# Checklist — FEAT-012

One box per plan step, in plan order. Tick with
`set_ticket_doc(doc: "checklist")`; append progress notes below.

- [ ] Read plan row `DSK-05-12`, `vertical-slices.md:434-458`, `screen-specs.md:298-307` and the settled vocabulary at `docs/design/README.md:535-546`; call `get_doc_gates FEAT-012`; `take_ticket` on branch `task/dsk-05-12-unidentified-vehicle-images`, worktree `../pegasus-worktrees/dsk-05-12-unidentified-vehicle-images`, from `origin/dev`
- [ ] Append to `research` each of the four page models' list query, exclusion rule, command parameters (`expectedVersion`, `operationKey` ≤ 200, `reason`) and VRM/candidate-case fields; record the SHA read
- [ ] Record explicitly that `Unidentified/Index.cshtml.cs` is a redirect and the real queue is `Triage/Index.cshtml.cs:249-274`, and that the exclusion rule is `item.State == Open` at `EfUnidentifiedStore.cs:250,259`
- [ ] Confirm [[GWY-013]] has landed all seven endpoints and that the list counts apply the same `Open`-state exclusion from one query, not two that can disagree
- [ ] Add the DTOs to `src/Pegasus.Contracts`, with a queue-row shape that tolerates a **missing** origin receipt and a VRM suggestion carrying confidence-free presentation fields
- [ ] Bind the resolve request DTO to [[GWY-013]]'s widened shape without redesigning it
- [ ] Implement `UnidentifiedListViewModel` and `UnidentifiedDetailViewModel` on the [[DUI-007]] data-table pattern, with resolve as an explicit reasoned command using the [[DUI-009]] dialog contract
- [ ] Confirm [[GWY-013]] has landed the optional `registration` on `POST /api/v1/unidentified/{id}/resolve`, and restate its shape in the plan document — stop and raise there if it has not
- [ ] Add the promote control: sends the typed registration with `targetKind = Triage` and **no** `targetId`, and contains no registration normaliser, format check, Triage-creation call or origin-receipt lookup
- [ ] Render the promote path's outcome and its refusal from the gateway's problem, fabricating nothing
- [ ] Record in the plan document which case applied for `ITriageQueries.GetByOriginReceiptAsync` (present, or awaiting upstream INTK-033 / board [[INTK-007]] via [[GWY-013]] step 8)
- [ ] Implement `VehicleImagesListViewModel` and `VehicleImagesDetailViewModel` with close as an explicit reasoned command, VRM suggestions and candidate cases shown as data with no explanatory copy, and paging honoured
- [ ] Render every state through [[FEAT-023]]'s label list — `Unidentified`, `Vehicle images`, `Image reference`, exact and case-sensitive
- [ ] Reuse [[FEAT-009]]'s streaming download service for member source access rather than writing a second one
- [ ] Add both queues to the shell rail under Queues in the `screen-specs.md` § `Shell` route order, with an absent count rendering nothing
- [ ] Add contract tests for both queues' list, detail, resolve, close and source endpoints: success, 401, 403, 409 stale version, replay, reason required — with `Features:DesktopGateway` enabled explicitly
- [ ] Add the count-exclusion contract assertion proving receipts that produced a case are excluded from the counts
- [ ] Add the five promote-path contract facts: opens exactly one Triage from the originating receipt; an invalid registration opens nothing; a receipt that already has a Triage does not gain a second; `registration` with a non-`Triage` `targetKind` is a validation failure; an ordinary resolve is unchanged
- [ ] Add view-model tests for paging, reason-required resolve and close, the promote command's `CanExecute` and refusal path, the [[FEAT-008]] conflict pattern, and correct vocabulary on every state
- [ ] Update `parity-matrix.md` rows `PAR-25` and `PAR-26`
- [ ] Add the promote control and its AutomationId to `screen-specs.md:298-307` — this ticket's block only, not the `endpoint-map.md` resolve row
- [ ] Add the section to `docs/frd/frd-13-desktop-operator-experience.md` (or contribute it to [[DUI-013]]) and add the `DSK` rows to `docs/capabilities.md`
- [ ] Run the simplification pass over this branch's diff and record it under a dated `## Simplification pass` heading in the plan document
- [ ] Verification run — `dotnet test` for `Pegasus.Api.ContractTests`, `Pegasus.Desktop.ViewModelTests` and `Pegasus.ArchitectureTests`, plus the tier-7 keyboard/axe artefacts for both screens; this box produces `proof`
- [ ] Open the PR into `dev`

## Progress notes

(append with `set_ticket_doc(doc: "checklist", append: true)`)
