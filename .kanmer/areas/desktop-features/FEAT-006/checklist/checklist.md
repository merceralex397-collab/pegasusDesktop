# Checklist — FEAT-006: S6 Workflow, closure and tasks commands

One box per plan step, in plan order. Tick with `set_ticket_doc`; append
progress notes below rather than rewriting.

- [ ] Read the plan row, `vertical-slices.md` § S6, `AGENTS.md` § Product invariants and `docs/design/README.md:400-409`; run `get_doc_gates FEAT-006`; `take_ticket` on branch `task/dsk-05-06-case-commands`, worktree `../pegasus-worktrees/dsk-05-06-case-commands`, from `origin/dev`
- [ ] Re-check parity drift: `git diff --stat bbd1c549..HEAD -- src/Pegasus.Web/Pages/Cases/Workflow.cshtml.cs src/Pegasus.Web/Pages/Cases/Closure.cshtml.cs src/Pegasus.Web/Pages/Cases/Tasks.cshtml.cs src/Pegasus.Core/Lifecycle src/Pegasus.Core/Tasks` is empty, or re-read and update `research` with the new SHA
- [ ] Extend the nineteen-row inventory in `research` with the exception types each command can throw, before writing any contract theory
- [ ] Confirm each of the nineteen has its own named endpoint in [[GWY-008]] / [[GWY-009]] — never a dispatcher taking an action string — and add any missing route against the same Core command the Razor handler calls
- [ ] Confirm the Engineer-role refusal on Record engineer finding (`src/Pegasus.Core/Cases/CaseContracts.cs:309-316`, an `InvalidOperationException`) is translated to **403 `not-authorized`**, not the default 400 shape
- [ ] Confirm commands 14–16 carry `expectedTaskVersion` and that `CaseTaskVersionConflictException` (`CaseTaskContracts.cs:21-31`) is mapped distinctly from the case version conflict
- [ ] Add nineteen request DTOs to `src/Pegasus.Contracts`, each naming the fields it needs — **no shared "command" bag**
- [ ] Include `reason` on eighteen of the nineteen (`ValidateMutation`, `CaseLifecycle.cs:420`); Add note is the one exception (`Tasks.cshtml.cs:28-32`, CASE-017)
- [ ] Make Reopen's readiness nullable and send it only for destination `Review` (`Closure.cshtml.cs:98-105`)
- [ ] Make Create linked replacement's response carry `isDuplicate` **and** the new reference (`Workflow.cshtml.cs:207-211`)
- [ ] Implement `CaseCommandsViewModel` with one command object per row and its own `CanExecute` from case state, edit-mode state ([[FEAT-005]]) and the actor's rights **and roles** ([[FND-046]])
- [ ] Derive terminal state from `CaseLifecycleRules.IsTerminal` (`CaseLifecycle.cs:393-399`), never from a restated list
- [ ] Allow a command whose state precondition fails but whose operation key has already been applied, mirroring `CaseLifecycle.cs:16` and `:34`, so a legitimate retry is not disabled
- [ ] Build the command bar with a named verb per command, **never a generic "Close"**, one primary action = the next permitted action, and `AutomationId` on every control
- [ ] Show permanent consequences without hover using only approved copy — for this surface, "Created in error cannot be reopened. Create and link the replacement case."
- [ ] Confirm a case in `Created in error` shows both references and **no reopen control**
- [ ] Wire the [[DUI-009]] `ReasonDialog` to all eighteen reason-required commands: named requirement, labelled reason field, verb-labelled primary plus Cancel, initial focus on the reason field
- [ ] Build the Tasks tab: add note, create / assign / complete / cancel task, record manual chase, link and unlink report evidence — each with its own operation key and the task-level `expectedVersion` where Core uses one
- [ ] Give Record manual chase the five fields the FRD names: `attemptedAtUtc`, `channel`, `targetPartyOrAddress`, `outcome`, `note?`
- [ ] Confirm no success message on any command claims external delivery
- [ ] Write contract tests running the [[TEST-002]] seven-case matrix over all nineteen commands, with `Features:DesktopGateway` enabled explicitly
- [ ] Add the stale-**task**-version 409 variant for commands 14–16
- [ ] Add the role-based 403 variant for Record engineer finding
- [ ] Record Add note's two inapplicable cases as documented exemptions in the test file and coordinate them with [[TEST-002]]'s template rather than silently skipping coverage
- [ ] Write view-model tests: `CanExecute` per state and per right/role, the replay allowance, reason-required refusal on an empty reason, no reopen control on `Created in error`, and **no delete command exists at all**
- [ ] **Operator step** — run the UAT script across the eleven lifecycle commands (hold / release, return to review, assign engineer, start work, record finding, create linked replacement, record report approval, close, reopen with reason, archive) and capture the sign-off text and date in the ticket proof
- [ ] Update `docs/desktop/01-inventory-and-parity/parity-matrix.md` rows `PAR-10` (workflow), `PAR-11` (tasks) and `PAR-12` (closure)
- [ ] Add the command sections to `docs/frd/frd-13-desktop-operator-experience.md` and `DSK` rows to `docs/capabilities.md`
- [ ] Run the simplification pass over the branch diff and record it under a dated `## Simplification pass` heading in the `plan` document
- [ ] Verification run — `dotnet build ./Pegasus.slnx -c Release --no-restore`; `dotnet test` for Api.ContractTests, Desktop.ViewModelTests and IntegrationTests (`--filter "Category!=Corpus&Category!=Browser"`); then write `proof` with the command log and the operator sign-off
- [ ] Open the PR into `dev`

## Progress notes

(append with `set_ticket_doc(doc: "checklist", append: true)`)
