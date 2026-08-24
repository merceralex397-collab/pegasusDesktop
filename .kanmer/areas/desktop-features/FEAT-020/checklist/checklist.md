# Checklist — FEAT-020: S20 Operations and integration health

One box per plan step, in plan order. Tick with `set_ticket_doc`; append progress notes below.

## Orientation and the case-link decision

- [ ] Read plan 05 § S20, the screen spec Operations section and `docs/desktop/10-security-observability-performance/README.md`; call `get_doc_gates FEAT-020`; `take_ticket` with branch `task/dsk-05-20-operations` and worktree `../pegasus-worktrees/dsk-05-20-operations` from `origin/dev`
- [ ] Read `src/Pegasus.Web/Pages/Operations/Index.cshtml.cs` in full and record in `research` what the snapshot projects, what makes an item retry-eligible, what revoking a link does, and the reason each command requires; record the SHA read
- [ ] Record in `research` that neither upstream `PLAT-023` nor upstream `INTK-004` has a fork ticket, that the board's `PLAT-023` is `DSK-11-05` and the board's `INTK-004` is upstream `INTK-027`
- [ ] Confirm `GET /api/v1/operations`, the retry and revoke routes with [[GWY-013]], the integration-health payload with [[FEAT-027]], and `GET /api/v1/admin/health`
- [ ] Settle the honest-case-link question with [[GWY-013]] and record the decision plus its evidence in the plan: resolve the link through the single `IntakeReceipt.CurrentCaseId` path, **or** remove the claim from `docs/current-architecture.md:291`
- [ ] Record that `RequestOperationProjection` already carries a non-nullable `CaseId`, that only `EmailOperationProjection`'s received-intake row is null (`EfOperationsStore.cs:159`), and that `GetEmailOperations` has no caller

## Contracts and gateway

- [ ] Add the operations snapshot DTO and the health DTO to `src/Pegasus.Contracts`
- [ ] Assert the health DTO carries no connection string, endpoint credential, token or internal host name
- [ ] Regenerate `openapi/pegasus-v1.json` and the generated client in this change

## Screen

- [ ] Check whether `OperationsViewModel` exists from [[FEAT-030]]; extend it in place, or create it with exactly the members [[FEAT-030]] step 3 pins — and record in the plan which case applied
- [ ] Confirm exactly one `OperationsViewModel` and one `OperationsPage.xaml` exist in `src/Pegasus.Desktop`
- [ ] Add retryable external work and active upload links as two lists on [[DUI-007]]'s data-table pattern
- [ ] Add the integration-health panel showing each dependency's state as **text** (never colour alone) and its last-cycle time in Europe/London through the shared vocabulary map
- [ ] Implement retry as an explicit command with an `operationKey`, offered only where the gateway says the item is eligible
- [ ] Implement revoke carrying the same six values as `Index.cshtml.cs:112-119` (`requestId`, `caseId`, `expectedVersion`, `expectedCaseVersion`, `reason`, `operationKey`)
- [ ] Distinguish the three retry failure outcomes the web already distinguishes (`Index.cshtml.cs:95-107`) rather than collapsing them into one error
- [ ] Carry the freshness rule: a failed refresh does not leave a stale "last read" on screen (`Index.cshtml.cs:41-45`, FRD-12)
- [ ] Show the update-feed state and minimum client version from [[GWY-023]]'s compatibility surface
- [ ] Confirm no banned operator word and no colour-only state anywhere on the screen

## Evidence

- [ ] Add contract tests: snapshot 200 with `ETag`, 401, 403
- [ ] Add a contract test that retry of an ineligible item is refused with a problem, and that retry success and revoke replay return the expected results
- [ ] Add a contract test that the health payload contains no secret-shaped value
- [ ] Add the step-3 case-link contract fact in whichever form the decision took
- [ ] Enable `Features:DesktopGateway` explicitly in every new contract test
- [ ] Add view-model tests for list loading, eligibility-driven command enablement, retry and revoke outcomes, health-state rendering with an unavailable dependency, and freshness after a failed refresh
- [ ] Run end-to-end scenario 13 (proposal `:1652`) on the local Test/UAT stack: induce an external-work failure, see it here, retry it, see it clear; record the run in the proof
- [ ] Update the operations rows in `docs/desktop/01-inventory-and-parity/parity-matrix.md`
- [ ] Add the retry and revoke behaviour as a sub-heading inside [[FEAT-030]]'s Operations screen section in `docs/frd/frd-13-desktop-operator-experience.md` — no second screen section
- [ ] Apply the `docs/current-architecture.md:291` correction if that was step 3's decision; otherwise record the evidence that the sentence is now true
- [ ] Add the `DSK` rows to `docs/capabilities.md`
- [ ] Run the simplification pass over the branch diff and record it under a dated `## Simplification pass` heading in the plan
- [ ] **Verification run (this box produces `proof`)** — `dotnet test ./tests/Pegasus.Api.ContractTests/…` and `./tests/Pegasus.Desktop.ViewModelTests/…` (`--configuration Release --no-build`), plus `pwsh ./tests/Pegasus.Desktop.UITests/ui-tests.ps1 -Script operations`; attach the outputs, the axe report and the scenario-13 record
- [ ] Open the PR into `dev`

## Progress notes

(append with `set_ticket_doc(doc: "checklist", append: true)`)
