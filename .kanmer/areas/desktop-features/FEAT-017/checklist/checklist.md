# Checklist — FEAT-017: S17 Assessment workbench

One box per plan step, in plan order. Tick with `set_ticket_doc`; append progress notes below
rather than rewriting.

## S17a — record damage (branch `task/dsk-05-17a-assessment-damage`)

- [ ] Read plan 05 § S17, the screen spec assessment section, FRD-06 § `Canonical repair specifications` and FRD-11 § `Report correction, finality, and post-report work`; call `get_doc_gates FEAT-017`; `take_ticket` with branch `task/dsk-05-17a-assessment-damage` and worktree `../pegasus-worktrees/dsk-05-17a-assessment-damage` from `origin/dev`
- [ ] Append the S17b and S17c branch names and the checkpoint order to the plan document under § Steps step 2
- [ ] Tabulate the four in-scope handlers (`Index.cshtml.cs:184`, `:246`, `:330`, `:476`) in `research` with their Core calls and required `expectedVersion` / `operationKey` / `editLeaseToken`, and record the SHA read
- [ ] Record in `research` that `:583` (`ISendCaseToAi`) and `:628` (`IReconcileAiWorkRequest`) are excluded as Send-to-AI surfaces per `reuse-map.md:38`
- [ ] Record the mileage/source prefill path read from `tests/Pegasus.IntegrationTests/AssessmentVehiclePrefillWebTests.cs` in `research`
- [ ] Enumerate the fixtures behind `AssessmentDamageAndCopyWebTests.cs` and `AssessmentEstimateImportWebTests.cs` and write the named list into the plan
- [ ] Write tier-2 characterization facts in `tests/Pegasus.Core.Tests` for all nine page-model rules at current behaviour (`:341`, `:45`/`:351`, `:356`, `:382-387`, `:388-394`, `:397`, `:494`, `:504`, `:509-514`)
- [ ] Move each characterized rule into `src/Pegasus.Core/Assessment/` and re-point `src/Pegasus.Web/Pages/Cases/Assessment/Index.cshtml.cs` at it in the same commit
- [ ] Verify `src/Pegasus.Web/Mcp/` against each moved use case and record what changed for the MCP surface
- [ ] Confirm the five assessment endpoints against [[GWY-014]] and `endpoint-map.md` § `Cases`, including the Engineer right on `specification/accept`
- [ ] Add the assessment DTOs to `src/Pegasus.Contracts` with `decimal` money and measurement fields
- [ ] Regenerate `openapi/pegasus-v1.json` and the generated client in this change
- [ ] Implement `AssessmentDamageViewModel` in `src/Pegasus.Desktop` with local `AssessmentPolicy` calculation and the server response as the authoritative figure
- [ ] Reproduce the lease-then-save write shape from `Index.cshtml.cs:213-228` in the desktop client
- [ ] Run the simplification pass over the S17a diff and record it under a dated `## Simplification pass` heading in the plan
- [ ] Open the S17a PR into `dev`

## S17b — estimate import and specification acceptance (branch `task/dsk-05-17b-assessment-estimate`)

- [ ] Implement estimate import as an upload session reusing the transfer service from [[FEAT-014]]; assert no PDF parsing in `src/Pegasus.Desktop*`
- [ ] Render the imported lines as a **draft** and implement Engineer acceptance per FRD-06 `:190-195`
- [ ] Enforce the Engineer gate server-side and reflect it in the UI without relying on hiding
- [ ] Add contract tests for import and accept: success, 401, 403 non-Engineer, 409 stale version, `operationKey` replay, malformed estimate rejected as a problem
- [ ] Assert an action-history record for each import and accept mutation (FRD-04 `:29`)
- [ ] Enable `Features:DesktopGateway` explicitly in every new contract test
- [ ] Run the simplification pass over the S17b diff and record it under a dated heading
- [ ] Open the S17b PR into `dev`

## S17c — reconcile (branch `task/dsk-05-17c-assessment-reconcile`)

- [ ] Confirm [[GWY-014]] has defined `POST /api/v1/cases/{id}/assessment/reconcile`; if not, stop and leave S17c in Preparing with the reason recorded
- [ ] Implement reconcile as an explicit command using the reason dialog from [[DUI-009]] where Core requires a reason
- [ ] Surface the shared 409 conflict pattern from [[FEAT-008]] on a stale version
- [ ] Add contract tests for reconcile: success, 401, 403, 409, replay
- [ ] Run the simplification pass over the S17c diff and record it under a dated heading
- [ ] Open the S17c PR into `dev`

## Cross-cutting: prefill, tests, evidence

- [ ] Prefill mileage and its source from [[FEAT-015]]'s accepted lookup evidence, with the provenance glyph and obtained-at value beside the figure
- [ ] Assert that a prefilled value is never presented as keyed by the operator and no range is defaulted into the case (FRD-06 `:214`)
- [ ] Add view-model tests in `tests/Pegasus.Desktop.ViewModelTests` for local-calculation equality, dirty state, Engineer gating, prefill provenance and reconcile
- [ ] Confirm no banned operator word (`artifact`, `lease`, `projection`, …) and no how-it-works copy reaches any assessment screen (`docs/design/README.md:412-445`)
- [ ] Produce the fixture comparison table: every enumerated fixture, desktop figure versus web figure, in the ticket proof
- [ ] Operator step — Engineer UAT on the local Test/UAT stack across damage, import, accept and reconcile; capture the sign-off text and date in the proof
- [ ] Update `docs/desktop/01-inventory-and-parity/parity-matrix.md` row `PAR-15`, assessment portion only
- [ ] Add the assessment section to `docs/frd/frd-13-desktop-operator-experience.md` citing FRD-06 and FRD-11, and the `DSK` rows to `docs/capabilities.md`
- [ ] **Verification run (this box produces `proof`)** — `dotnet test ./tests/Pegasus.Core.Tests/…`, `./tests/Pegasus.Api.ContractTests/…`, `./tests/Pegasus.Desktop.ViewModelTests/…`, and `./tests/Pegasus.IntegrationTests/… --filter "Category!=Corpus&Category!=Browser"`, all `--configuration Release --no-build`; attach the four outputs, the fixture table and the Engineer sign-off

## Progress notes

(append with `set_ticket_doc(doc: "checklist", append: true)`)
