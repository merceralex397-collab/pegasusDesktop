# Checklist — FEAT-019: S19 Administration

One box per plan step, in plan order. Tick with `set_ticket_doc`; append progress notes below.

## Orientation

- [ ] Read plan 05 § S19, the screen spec Administration section, FRD-04 § `Staff role access matrix`, ADR-0022 and ADR-0024; call `get_doc_gates FEAT-019`; `take_ticket` with branch `task/dsk-05-19-administration` and worktree `../pegasus-worktrees/dsk-05-19-administration` from `origin/dev`
- [ ] Read `docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md` for upstream PLAT-025, PLAT-026, PLAT-027, AUTO-006, AUTO-007 and PR-026 before duplicating any of them
- [ ] Record in the plan that board `PLAT-025`/`PLAT-026`/`PLAT-027` are `DSK-11-07`/`DSK-11-08`/`DSK-11-09` and are **not** the upstream tickets absorbed here
- [ ] Tabulate the ten in-scope screens in `research`: handlers, Core use case, exact `StaffAccessRight`, version/`operationKey`/`reason` requirements, and which writer (`ISecurityEventWriter` or `IActionHistoryWriter`) each mutation uses; record the SHA read
- [ ] Confirm the in-scope list excludes the five `Organizations/*` and `Principals/*` models owned by [[FEAT-007]]

## Gateway surface

- [ ] Confirm every route and right against [[GWY-015]] and `endpoint-map.md` § `Administration and audit`
- [ ] Confirm `POST /admin/accounts/{id}/disable` requires a `reason` and revokes refresh tokens ([[GWY-022]])
- [ ] Add the administration DTOs to `src/Pegasus.Contracts`, each carrying its resource version
- [ ] Make the channel-token rotate response carry the token exactly once and mark the DTO as never persisted or logged
- [ ] Regenerate `openapi/pegasus-v1.json` and the generated client in this change

## Screens

- [ ] Implement one view model per screen on [[DUI-007]]'s data-table pattern and [[DUI-008]]'s form pattern
- [ ] Consolidate accounts, roles and access review into one **Administration › People** area (upstream PLAT-027)
- [ ] Build the Activity screen from `Automation/Activity.cshtml.cs:23`'s read, resolving the Target column to the Case/PO reference or omitting it — never the raw `AggregateId` (`Automation/Activity.cshtml:67`)
- [ ] Implement every reason-required mutation through [[DUI-009]]'s dialog contract
- [ ] Show the consequence of disabling an account and of clearing a channel token without hover, using approved copy from `docs/design/README.md:398-409`
- [ ] Implement mailbox folder resolution as a distinct command calling the gateway endpoint; assert no Graph call from `src/Pegasus.Desktop*`
- [ ] Apply [[FND-046]]'s role-aware navigation: each Administration entry absent when the actor lacks its right
- [ ] Implement the rotated channel token as a one-time reveal: shown once, copyable, never cached, logged or bundled
- [ ] Confirm no `StaffAccessRight` switch or rights matrix exists anywhere in `src/Pegasus.Desktop*`
- [ ] Confirm no banned operator word and no how-it-works copy reaches any administration screen

## Evidence

- [ ] Add contract tests per endpoint: 200 with the correct right, 403 with any other right, 403 for the Automation Actor, 401 without a token, 409 stale version, `operationKey` replay
- [ ] Assert an audit record for each sensitive mutation, in the store the step-3 tabulation named (FRD-04 `:27-33`)
- [ ] Enable `Features:DesktopGateway` explicitly in every new contract test
- [ ] Add view-model tests per screen: load, validation, reason-required commands, token non-retention after the dialog closes
- [ ] Confirm the token does not appear in a diagnostics bundle produced by [[FND-036]]
- [ ] Operator step — run the administration UAT script on the local Test/UAT stack: configuration change, mailbox update and folder resolve, access review, account create and disable, role assignment, each automation control
- [ ] Capture the operator's sign-off text and date in the ticket proof
- [ ] Update the administration rows in `docs/desktop/01-inventory-and-parity/parity-matrix.md`
- [ ] Add the administration section to `docs/frd/frd-13-desktop-operator-experience.md` citing FRD-04, and the `DSK` rows to `docs/capabilities.md`
- [ ] Run the simplification pass over the branch diff and record it under a dated `## Simplification pass` heading in the plan
- [ ] **Verification run (this box produces `proof`)** — `dotnet test ./tests/Pegasus.Api.ContractTests/…`, `./tests/Pegasus.Desktop.ViewModelTests/…` and `./tests/Pegasus.IntegrationTests/… --filter "Category!=Corpus&Category!=Browser"`, all `--configuration Release --no-build`; attach the three outputs and the named UAT sign-off
- [ ] Open the PR into `dev`

## Progress notes

(append with `set_ticket_doc(doc: "checklist", append: true)`)
