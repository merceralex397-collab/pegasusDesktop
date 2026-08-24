# Checklist — FEAT-015

One box per plan step, in plan order. Tick with
`set_ticket_doc(doc: "checklist")`; append progress notes below.

- [ ] Read plan row `DSK-05-15`, `vertical-slices.md:523-549`, `screen-specs.md:319-330`, `docs/frd/frd-06-vehicle-and-engineering-evidence.md` and `docs/frd/frd-07-eva-and-external-engineering-handoff.md`; call `get_doc_gates FEAT-015`; `take_ticket` on branch `task/dsk-05-15-vehicle-eva`, worktree `../pegasus-worktrees/dsk-05-15-vehicle-eva`, from `origin/dev`
- [ ] Append to `research` how the lookup request becomes a durable Worker work item, what accept writes, where the Core normalisation rule lives, the mileage policy inputs, and the EVA download's full parameter set including `expectedVersion` and `editLeaseToken`; record the SHA read
- [ ] Settle the bundle-read shape with [[FEAT-035]] so it carries `revision`, `expectedVersion`, `operationKey`, `reason` and `editLeaseToken` — the endpoint map's `GET` row cannot
- [ ] Confirm the lookup responses carry the cache lifetime and the provenance fields (source and obtained-at) the screen must show
- [ ] Confirm [[FEAT-045]]'s provider error taxonomy is on these endpoints so a provider failure is distinguishable from a genuine not-found in the contract
- [ ] Add the vehicle and EVA DTOs to `src/Pegasus.Contracts`, including the suggestion with its source and timestamp and the handoff revision identifier
- [ ] Implement registration normalisation by calling the existing Core rule in `src/Pegasus.Core/Vehicle/` — write no second normalizer
- [ ] Extend `CaseVehicleViewModel` in place with the lookup-status refresh and the EVA generate and download commands, or create it to [[FEAT-036]]'s pinned members — and record which case applied
- [ ] Render each provider state distinctly through the shared vocabulary, never one generic "failed", and show source and obtained-at beside an accepted value
- [ ] Show cached-lookup freshness using the [[DUI-012]] header control so it is readable without hovering
- [ ] Implement EVA generate and download as explicit commands, the download streamed through [[FEAT-014]]'s transfer service and carrying the reason, version and lease Core requires
- [ ] Generate a bundle on the local Test/UAT stack from the seeded case
- [ ] Add the `EvaBundleContent` assertion for the archive's entry list — thirteen-key JSON plus `Images/` and nothing else — and the JSON layout, two-space indentation with the same key set and order, diff clean against `reference/eva_information/AX_SP58WVO.json`
- [ ] Add the `EvaBundleContent` assertion for the thirteen field values against both known-good samples, with `Reference` carrying the work provider's claim number, `Inspection Address` carrying exactly six lines, and `Vehicle Model` carrying make and model
- [ ] If the content assertion fails, raise it on [[ENG-001]] (packaging, indentation) and [[ENG-002]] (field values) — write no second EVA mapping in the desktop or the gateway
- [ ] Add contract tests over `DvlaDvsaReplayAdapter`: success, not-found, each provider failure class, rate-limited, 401, 403, 409 stale version, replay — with `Features:DesktopGateway` enabled explicitly
- [ ] Add the contract assertion that no provider key appears in any response
- [ ] Add view-model tests for normalisation delegating to Core, each provider state rendering distinctly, freshness display, accept updating the case version, and EVA generate-then-download
- [ ] Run the replay-adapter integration check on the local Test/UAT stack and record in the proof that no live provider call was made
- [ ] Update `parity-matrix.md` row `PAR-14` and leave `PAR-18` to [[FND-018]], supplying the content evidence rather than editing the row
- [ ] Add the EVA handoff behaviour as a **sub-heading** under [[FEAT-036]]'s Vehicle tab section in `docs/frd/frd-13-desktop-operator-experience.md` citing FRD-06 and FRD-07 (or contribute it to [[DUI-013]]), and add the `DSK` rows to `docs/capabilities.md`
- [ ] Run the simplification pass over this branch's diff and record it under a dated `## Simplification pass` heading in the plan document
- [ ] Verification run — `dotnet test` for `Pegasus.Api.ContractTests` (full and the `EvaBundleContent` filter), `Pegasus.Desktop.ViewModelTests` and `Pegasus.ArchitectureTests`, plus the tier-7 keyboard/axe artefacts and the Test/UAT no-live-call record; this box produces `proof`
- [ ] Open the PR into `dev`

## Progress notes

(append with `set_ticket_doc(doc: "checklist", append: true)`)
