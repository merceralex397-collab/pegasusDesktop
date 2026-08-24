# Checklist — FEAT-005: S5 Case edit with lease, version and completeness

One box per plan step, in plan order. Tick with `set_ticket_doc`; append
progress notes below rather than rewriting.

- [ ] Read the plan row, `vertical-slices.md` § S5 and § `Common to every slice`, and `docs/frd/frd-01-case-identity-and-lifecycle.md:82-88`; run `get_doc_gates FEAT-005`; `take_ticket` on branch `task/dsk-05-05-case-edit`, worktree `../pegasus-worktrees/dsk-05-05-case-edit`, from `origin/dev`
- [ ] Re-check parity drift: `git diff --stat bbd1c549..HEAD -- src/Pegasus.Web/Pages/Cases/Details.cshtml.cs src/Pegasus.Web/Pages/Cases/CaseMutationPageModel.cs src/Pegasus.Core/Workflow src/Pegasus.Core/Lifecycle src/Pegasus.Core/Cases/CaseDataOperations.cs` is empty, or re-read and update `research` with the new SHA
- [ ] Confirm the wire carries all five things `CaseLifecycleRules.ValidateMutation` requires (`CaseLifecycle.cs:414-426`): `expectedVersion`, `operationKey` ≤ 100, **`reason` required ≤ 500**, `editLeaseToken` exactly 64, and an actor with `PerformCasework`
- [ ] Confirm a lease claim replayed with the same `operationKey` returns the same token and expiry (`ILeaseCaseForEdit`, `CaseWorkflowContracts.cs:323-336`)
- [ ] Confirm a stale write returns a 409 problem carrying the current version (`CaseVersionConflictException.ActualVersion`, `CaseWorkflowContracts.cs:129`)
- [ ] Confirm a lease conflict returns the holder's **display name** through `IDescribeCaseEditAuthorityHolder` (`CaseEditAuthority.cs:83-90`), never the subject id
- [ ] Confirm the claim and renew responses carry `expiresAtUtc`, and the completeness response carries both `Values` and `Evaluation` (`CaseDataContracts.cs:105-107`)
- [ ] Implement `CaseEditSession` in `src/Pegasus.Desktop.Infrastructure`: claim on entering edit, release on exit, `LeaseLost` on a failed renew
- [ ] Drive the renew timer from the returned `ExpiresAtUtc`, never from a hard-coded five minutes — `EditLeaseDuration` lives in `src/Pegasus.Infrastructure/Persistence/EfCaseWorkflowStore.cs:20` and the desktop must not reference Infrastructure
- [ ] Hold the lease token in memory only — never written to disk, never to a log — and raise no operator-visible event for a routine renewal
- [ ] Add edit state to `CaseWorkspaceViewModel`: explicit dirty indicator, deliberate `SaveCommand` (never autosave), navigation guard that warns before discarding
- [ ] Run immediate field validation against `CaseDataPolicy.Normalize` / `ValidateInspection` (`CaseDataOperations.cs:121-190`) referenced directly from `Pegasus.Core`
- [ ] Bind inspection address and inspection mode as one control group so a half-set pair cannot be submitted
- [ ] Bind `Ctrl+S` to `SaveCommand` and disable it while edit mode is not held or the session is offline — no silent queueing of saves ([[FND-047]])
- [ ] Send the save with the loaded `expectedVersion`, the held `editLeaseToken`, a reason collected through the [[DUI-009]] `ReasonDialog`, and a fresh `operationKey` per user-initiated attempt reused unchanged on transport retry
- [ ] Re-query the case on an uncertain outcome (timeout after send) rather than resending blind
- [ ] Render version conflict, edit mode lost and edit mode held by a named holder as three unambiguous states, reusing the settled sentences at `Details.cshtml.cs:196-197`, `:240-241`, `:280`
- [ ] Confirm the word `lease` reaches no operator surface (`docs/design/README.md:412-420`)
- [ ] Clear the held token on a stale-version refusal as well as on a lease failure, matching `RequiresReacquisition` (`CaseMutationPageModel.cs:313-314`)
- [ ] Implement completeness confirmation as an explicit reasoned command, rendering both that the confirmation was accepted and whether it satisfies the current policy
- [ ] Confirm no completeness rule was found only in the page model — and if one was, move it into `src/Pegasus.Core/Cases/` with a characterization test in `tests/Pegasus.Core.Tests` **first**
- [ ] Write view-model tests: dirty state, navigation guard, save disabled without edit mode and while offline, operation-key reuse, `LeaseLost`, 409 with current version captured, stale-version clearing the token, address/mode group refusal
- [ ] Write the two-user integration test against LocalDB: A claims edit mode and saves at version N; B saves at N and gets a 409 carrying N+1; A's write is intact
- [ ] Write contract tests for claim / renew / release replay, expiry, and release by a non-holder — with `Features:DesktopGateway` enabled explicitly
- [ ] Confirm the new integration facts land in exactly one shard: `pwsh ./scripts/Invoke-TestShard.ps1 -VerifyPartition`
- [ ] Add `tests/Pegasus.Desktop.UITests/ui-tests.ps1 -Script case-edit` asserting edit, save and the conflict message with `wait-for`, no sleeps
- [ ] **Operator step** — run the two-user UAT scenario on the local Test/UAT stack with two workstations or sessions, and capture the operator's sign-off text and date in the ticket proof
- [ ] Update `docs/desktop/01-inventory-and-parity/parity-matrix.md` row `PAR-08` for the **edit handlers**
- [ ] Add the edit and edit-mode section to `docs/frd/frd-13-desktop-operator-experience.md` and a `DSK` row to `docs/capabilities.md`
- [ ] Run the simplification pass over the branch diff and record it under a dated `## Simplification pass` heading in the `plan` document
- [ ] Verification run — `dotnet build ./Pegasus.slnx -c Release --no-restore`; `dotnet test` for IntegrationTests (`--filter "Category!=Corpus&Category!=Browser"`), Api.ContractTests and Desktop.ViewModelTests; `pwsh ./tests/Pegasus.Desktop.UITests/ui-tests.ps1 -Script case-edit`; then write `proof` with the command log, the two-user test output, the UI artefacts and the operator sign-off
- [ ] Open the PR into `dev`

## Progress notes

(append with `set_ticket_doc(doc: "checklist", append: true)`)
