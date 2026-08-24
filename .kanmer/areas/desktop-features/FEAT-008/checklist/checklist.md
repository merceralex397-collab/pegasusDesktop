# Checklist — FEAT-008: S8 Concurrency UX (conflict, lease lost, replay)

One box per plan step, in plan order. Each is independently tickable. The
verification box is the one that produces `proof`.

- [ ] Read `vertical-slices.md` § S8 and § Common to every slice, `screen-specs.md:193-197` and `:417-427`, `docs/frd/frd-01-case-identity-and-lifecycle.md:82-88`, and `docs/design/README.md:622`, `:722`, `:769-772`
- [ ] `get_doc_gates FEAT-008`, then `take_ticket` with branch `task/dsk-05-08-concurrency-ux` and worktree `../pegasus-worktrees/dsk-05-08-concurrency-ux` from `origin/dev`
- [ ] Enumerate the four Core exceptions and their asymmetric payloads in `research` (`CaseWorkflowContracts.cs:125-158`), plus replay-as-success (`:322-334`), and record the SHA read
- [ ] Read `src/Pegasus.Web/Mcp/AutomationMcpErrors.cs` in full and record that its class docstring (`:7-16`) deliberately lets **no holder material cross** — so a faithful port is the wrong answer for the staff-facing gateway boundary
- [ ] Read [[GWY-002]]'s (plan handle `DSK-03-02`) delivered `/api/v1` problem-details mapping and `openapi/pegasus-v1.json`
- [ ] Confirm or add `currentVersion` on the `version-conflict` problem (`docs/desktop/03-gateway-api-and-data/README.md:166`)
- [ ] Confirm or add the **named holder** plus an `isAutomation` flag on `lease-conflict`, resolved through `IDescribeCaseEditAuthorityHolder` (`CaseEditAuthority.cs:83-127`) — not `ActorDisplayNames` directly — and confirm no lease token is on the wire
- [ ] Confirm `lease-expired` carries the case version and **no holder** (Core establishes none on that path, `CaseEditAuthority.cs:51-58`)
- [ ] Add the explicit `CaseOperationConflictException` branch to the mapping, emitting `operation-conflict` with the operation key and no interpolated case id (today it falls into the pass-through at `AutomationMcpErrors.cs:54-60`)
- [ ] Confirm the gateway marks a **replay** on the success response; if it is absent, raise it on [[GWY-002]] and [[GWY-008]] rather than attempting client-side detection
- [ ] Add the four problem DTOs to `src/Pegasus.Contracts` as a discriminated set keyed by the catalogue slugs at `README.md:167` — four distinct shapes, not one nullable bag
- [ ] Implement `ConflictRecoveryService` in `src/Pegasus.Desktop.Infrastructure`: re-query, field-level comparison, reapply plan — and confirm it **never resends the original body unchanged**
- [ ] Confirm the comparison covers the **editorial projection only** — no `version`, `id`, `operationKey` or lease-token rows — following the `RetainableFormFields` selection rule (`CaseMutationPageModel.cs:41-91`)
- [ ] Confirm no TempData equivalent was introduced (no 8000/2000 budget, no chunking, no drop/shorten flags) **and** that proposed values are preserved in memory (`screen-specs.md:195`, `docs/design/README.md:622`)
- [ ] Implement `ConflictRecoveryView` in `src/Pegasus.Desktop`: the [[DUI-010]] (plan handle `DSK-06-10`) `InfoBar`, a compare pane listing only differing fields with both columns in the same vocabulary, and Reload / Keep mine / Cancel
- [ ] Confirm **Keep mine** reloads and reacquires first, then re-populates the fresh editor, so the save that follows carries the **new** `expectedVersion` and the **new** lease token — never a write over the newer record
- [ ] Confirm operator copy uses "edit mode" and contains none of `lease`, `caller` or `correlation identifier`, and that the InfoBar's copyable field is labelled "Reference" (`docs/design/README.md:412-420`; nothing in CI checks this)
- [ ] Implement the retry rule in code: same `operationKey` for an idempotent retry; a fresh key only by operator decision for a non-idempotent one; re-query — never resend — after a timeout
- [ ] **Check [[GWY-008]]'s (plan handle `DSK-03-08`) two cross-actor lease facts and its acceptance criterion have landed and pass.** If either fails, **stop and raise it on [[GWY-008]]** — no client-side guard, no modelled takeover, no parity claim
- [ ] Implement the lease-lost path: editor read-only immediately, holder named, re-claim re-queries first and never silently re-acquires
- [ ] Distinguish `lease-expired` (no holder; the case is available to re-enter) from `lease-conflict` (holder named, including an Automation Actor named as itself) — do not copy the web's `IsLeaseLoss` collapse (`CaseMutationPageModel.cs:292-294`)
- [ ] Render the `RequiresReacquisition` truth (`CaseMutationPageModel.cs:296-304`): after a **version** conflict the operator still holds the server-side authority and the screen must not imply the lease was taken away
- [ ] Implement the replayed path so the original outcome is shown and never presented as a new success
- [ ] Add contract tests in `tests/Pegasus.Api.ContractTests` for each of the four problem types — shape, status code, and the presence of `currentVersion` or the named holder — with `Features:DesktopGateway` enabled explicitly
- [ ] Add the contract facts for the replay marker on the success path and for the negative: **no lease token appears in any problem body**
- [ ] Add view-model tests for each problem type mapped to its state, the comparison producing only differing fields and no identifier row, the retry-rule refusal, and re-query on timeout
- [ ] Add the view-model facts for holder naming: an Automation Actor holder named as itself without substituting the operator's own identity, and `lease-expired` naming no holder
- [ ] Add the reapply guard fact: a reapply never carries the stale version or the old lease token
- [ ] Add `tests/Pegasus.Desktop.UITests/ui-tests.ps1 -Script concurrency` covering the scripted two-user conflict against the gateway fixture **without sleeps**
- [ ] **Operator step** — run the two-user UAT scenario on the local Test/UAT stack (`test-uat-stack.md:22`, LocalDB, `Features:DesktopGateway=true`): two operators edit the same case, the second compares and reapplies deliberately, no value is lost; capture the named sign-off with date and screenshots of all four states, keeping it consistent with [[TEST-016]]'s (plan handle `DSK-08-16`) scenario 11
- [ ] Add the conflict-and-recovery section to `docs/frd/frd-13-desktop-operator-experience.md`, including the retry rule written as a requirement
- [ ] Add a **note** (not a row) to `docs/desktop/01-inventory-and-parity/parity-matrix.md` recording the shared recovery pattern across `PAR-08`–`PAR-12`, and a `DSK` row to `docs/capabilities.md`
- [ ] Run the simplification pass over the branch diff and record it under a dated `## Simplification pass` heading in the `plan` document
- [ ] Verification run — `dotnet build ./Pegasus.slnx -c Release --no-restore`; `dotnet test` for Api.ContractTests and Desktop.ViewModelTests; `dotnet test ./tests/Pegasus.IntegrationTests/... --filter "FullyQualifiedName~DesktopGatewayCaseCommandTests"` as the [[GWY-008]] dependency gate; `pwsh ./tests/Pegasus.Desktop.UITests/ui-tests.ps1 -Script concurrency`; then write `proof` with the command log, the tier-7 UI artefacts and the tier-12 UAT record with the operator's sign-off and the four state screenshots
- [ ] Open the PR into `dev`

## Progress notes

(append with `set_ticket_doc(doc: "checklist", append: true)`)
