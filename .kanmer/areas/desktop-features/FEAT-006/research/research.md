# Research — FEAT-006: the nineteen case commands, and what each one requires

## Question

What are the nineteen case commands exactly — their Core use case, their
required fields, their authorization gate and their failure types — and what
must the `/api/v1` surface look like so each is an explicit, audited, named
action with no generic execute endpoint?

## Current behaviour

Three page models, all deriving from `CaseMutationPageModel` and all ending in a
PRG redirect to the workspace:

- `src/Pegasus.Web/Pages/Cases/Workflow.cshtml.cs` (227 lines) — **seven**
  handlers at `:26`, `:42`, `:64`, `:98`, `:133`, `:156`, `:180`;
- `src/Pegasus.Web/Pages/Cases/Closure.cshtml.cs` (121 lines) — **four** at
  `:23`, `:52`, `:69`, `:106`;
- `src/Pegasus.Web/Pages/Cases/Tasks.cshtml.cs` (248 lines) — **eight** at
  `:33`, `:61`, `:89`, `:117`, `:143`, `:169`, `:201`, `:225`.

Seven plus four plus eight is nineteen, matching the ticket body. Sixteen of the
nineteen go through `CaseMutationPageModel.ExecuteCaseCommandAsync`
(`src/Pegasus.Web/Pages/Cases/CaseMutationPageModel.cs:110-121`); two —
`OnPostCreateLinkedReplacementAsync` (`Workflow.cshtml.cs:180-226`) and
`OnPostAddNoteAsync` (`Tasks.cshtml.cs:33-58`) — hand-roll their own try/catch
because they need a bespoke success message or a different envelope.

Parity matrix rows: **`PAR-10`** (workflow, seven handlers) at
`docs/desktop/01-inventory-and-parity/parity-matrix.md:55`, **`PAR-11`** (tasks,
eight handlers) at `:56`, **`PAR-12`** (closure, four handlers) at `:57` — all
`inventoried`. Note the matrix orders them workflow / tasks / closure, while the
ticket body's step 12 says "`PAR-10`, `PAR-11` and `PAR-12`" and its
Documentation-changes section correctly labels `PAR-11` tasks and `PAR-12`
closure. The matrix holds 46 `PAR-` rows (`grep -c '^| PAR-' …` → `46`), all
keyed to page models under `src/Pegasus.Web/Pages/**`.

## Findings

### Facts

Verified at `HEAD` `bbd1c549` (2026-08-24). `git diff --stat 191ddf33..HEAD -- src tests`
is empty, so the plan set's line references still hold. **`bbd1c549` is the
revision characterized.**

#### The nineteen-command inventory

Read from the three page models. "Extra fields" is beyond the five that
`CaseLifecycleRules.ValidateMutation` requires of every mutation
(`src/Pegasus.Core/Lifecycle/CaseLifecycle.cs:414-426`): `expectedVersion`,
`operationKey` (≤ 100), `reason` (**required**, ≤ 500), `editLeaseToken`
(exactly 64), and an actor with `PerformCasework`.

| # | Command | Handler | Core use case | Extra fields | Gate beyond `PerformCasework` |
| --- | --- | --- | --- | --- | --- |
| 1 | Hold | `Workflow:26` | `IHoldCase` → `PutCaseOnHoldRequest` | hold detail | State: only an open case (`CaseLifecycle.cs:15-19`) |
| 2 | Release hold | `Workflow:42` | `IReleaseCase` | — | State: only a held case (`:33-37`) |
| 3 | Return to review | `Workflow:64` | `ITransitionCase`, destination `Review` | four readiness booleans + `evidenceReference` | State: only from Not ready (`:53-57`); `ValidateReturnToReview` (`:429-433`) |
| 4 | Assign engineer | `Workflow:98` | `IAssignCaseEngineer` | `engineerId` + four readiness booleans + `evidenceReference` | `ValidateAssignment` (`:435-446`) — non-empty engineer id, readiness against the current `CaseWorkflowConfiguration` |
| 5 | Start work | `Workflow:133` | `ITransitionCase`, destination `ReportPreparation` | — | — |
| 6 | Record engineer finding | `Workflow:156` | `IRecordEngineerFinding` | `AuditAssessment assessment` | **Engineer role required.** `src/Pegasus.Core/Cases/CaseContracts.cs:300-319` — actor must be `ActorKind.Staff`, `IsInRole(StaffRole.Engineer)`, and its `SubjectId` a non-empty `Guid` |
| 7 | Create linked replacement | `Workflow:180` | `ICreateLinkedReplacement` | `replacementPrincipalCode` | Returns `outcome.IsDuplicate` and the replacement `Identity.Reference`; hand-rolled handler |
| 8 | Record report approval | `Closure:23` | `IRecordCaseReportApproval` | `approvalId`, `artifactIdentity`, artifact SHA-256 | `ValidateReportApproval` (`CaseLifecycle.cs:448-461`) — non-empty approval id, artifact identity ≤ 200, SHA-256 exactly 64 hex |
| 9 | Close | `Closure:52` | `ICloseCase` | `CaseClosureOutcome outcome` (five values, `CaseWorkflowContracts.cs:25-32`) | — |
| 10 | Reopen | `Closure:69` | `IReopenCase` | `CaseReopenDestination destination` (four values, `:34-39`) + four readiness booleans + optional `evidenceReference`; readiness supplied **only** when the destination is `Review` (`Closure.cshtml.cs:98-105`) | — |
| 11 | Archive | `Closure:106` | `IArchiveCase` | — | `CaseArchivedException` guards a re-archive (`CaseCommandContracts.cs:10-14`) |
| 12 | Add note | `Tasks:33` | `IAddCaseNote` | `note` | **No lease, no expected version** — deliberately (`Tasks.cshtml.cs:28-32`, CASE-017): "it adds to the case's record rather than changing the case, so it must not contend with an engineer editing the same case" |
| 13 | Create task | `Tasks:61` | `ICreateCaseTask` | `taskId`, `description`, `assigneeId?` | — |
| 14 | Assign task | `Tasks:89` | `IAssignCaseTask` | `taskId`, **`expectedTaskVersion`**, `assigneeId?` | Task-level optimistic concurrency |
| 15 | Complete task | `Tasks:117` | `ICompleteCaseTask` | `taskId`, `expectedTaskVersion` | Task-level optimistic concurrency |
| 16 | Cancel task | `Tasks:143` | `ICancelCaseTask` | `taskId`, `expectedTaskVersion` | Task-level optimistic concurrency |
| 17 | Record manual chase | `Tasks:169` | `IRecordManualCaseChase` | `attemptedAtUtc`, `channel`, `targetPartyOrAddress`, `outcome`, `note?` | — |
| 18 | Link report evidence | `Tasks:201` | `ILinkReportEvidence` | `evidenceId` | `ValidateReportEvidence` (`CaseLifecycle.cs:463-…`) — non-empty evidence id |
| 19 | Unlink report evidence | `Tasks:225` | `IUnlinkReportEvidence` | `evidenceId` | Same |

#### What follows from the inventory

- **Every command except Add note requires a reason.** `ValidateMutation`
  (`CaseLifecycle.cs:420`) makes `Reason` mandatory on the `CaseMutationRequest`
  base record (`src/Pegasus.Core/Workflow/CaseWorkflowContracts.cs:182-188`),
  and eighteen of the nineteen derive from it. `AddCaseNoteRequest` does not —
  the handler passes `(id, actor, operationKey, note)` (`Tasks.cshtml.cs:46`).
  So the ticket body's "reason dialogs where Core requires one" resolves to
  **eighteen of nineteen**, and the exception is Add note.
- **Three distinct version tokens are in play**, not one: the case
  `expectedVersion` on all eighteen mutations; `expectedTaskVersion` on commands
  14–16, whose conflict is `CaseTaskVersionConflictException`
  (`src/Pegasus.Core/Tasks/CaseTaskContracts.cs:21-31`, carrying `TaskId`,
  `ExpectedVersion`, `ActualVersion`); and the operation key, whose conflict is
  `CaseOperationConflictException` (`CaseWorkflowContracts.cs:150-157`). A DTO
  that flattens task version into case version silently loses command 14–16's
  concurrency.
- **One command is role-gated and it is not gated by a `StaffAccessRight`.**
  Record engineer finding checks `IsInRole(StaffRole.Engineer)` directly in
  `src/Pegasus.Core/Cases/CaseContracts.cs:309-316` and throws
  `InvalidOperationException`, **not** `StaffAuthorizationException`. An endpoint
  filter keyed on `StaffAccessRight` cannot express it, and the exception maps to
  a 400-shaped problem unless translated deliberately — a real trap for the
  "wrong right 403" theory in the seven-case matrix.
- **Reopen's readiness is conditional.** `Closure.cshtml.cs:98-105` supplies
  `Readiness(...)` **only** when `destination == CaseReopenDestination.Review`,
  and `null` otherwise. `ReopenCaseRequest.Readiness` is nullable
  (`CaseWorkflowContracts.cs:305-314`). A DTO that always sends readiness changes
  behaviour for the other three destinations.
- **Create linked replacement returns business content, not just a version.**
  `outcome.IsDuplicate` distinguishes "already allocated" from "allocated and
  linked", and `outcome.Identity.Reference` is the new reference
  (`Workflow.cshtml.cs:207-211`). The response DTO must carry both, or the
  operator cannot be told which happened.
- **The product invariants this surface must uphold** are at
  `AGENTS.md` § Product invariants: "Never delete a case. Reopening needs a
  reason and normal destination gates." and "Principal and reference are
  immutable after allocation. Wrong-principal work closes as `Created in error`
  with a reason and linked replacement; neither reference is reused and the
  original never reopens." `CaseLifecycleRules.IsTerminal`
  (`CaseLifecycle.cs:393-399`) names the five terminal states, and
  `TerminalStateNames()` (`:409-415`) derives the persisted names from
  `IsTerminal` "so the two cannot drift" — a restated copy elsewhere is the
  defect INTK-029 recorded.
- **The approved consequence sentences are a closed list.**
  `docs/design/README.md:400-409` holds four, of which two belong to this
  surface: "Created in error cannot be reopened. Create and link the replacement
  case." and "Blocked — a reason is required." Nothing else may be written; the
  design authority forbids explanatory copy (`:430-434`).
- **The screen spec already fixes the command vocabulary.**
  `docs/desktop/06-ui-design/screen-specs.md:198-205`: "Lifecycle actions use
  only the named Core actions (Hold, Release hold, Return to review, Assign
  engineer, Start work, Record engineer finding, Create linked replacement,
  Record report approval, Close with named outcome, Reopen with reason,
  Archive); **never a generic Close**; every reasoned action through the
  `ReasonDialog` contract; `Created in error` shows both references and no
  reopen control." That is eleven lifecycle commands; the remaining eight are the
  Tasks-tab set (`:214`).
- **The endpoint map already names every route.**
  `docs/desktop/03-gateway-api-and-data/endpoint-map.md:57` (workflow seven),
  `:58` (closure four, with "reopen requires `reason`"), `:59-62` (notes, tasks,
  chases, report-evidence). Auth right `PerformCasework` throughout, with
  "(engineer finding: Engineer role)" called out at `:57`. Phase 4 for most,
  Phase 5 for manual chase and report-evidence.
- Existing test evidence, located by `ls tests/Pegasus.IntegrationTests`:
  `CaseWorkflowWebTests.cs` (124 lines), `CaseClosureWebTests.cs` (121),
  `CaseTasksWebTests.cs` (181), `CaseReportApprovalWebTests.cs`,
  `CaseNotePersistenceTests.cs`, `CaseTaskArchivePersistenceTests.cs`,
  `CaseCapabilityPagesTestSupport.cs` (166 — the shared harness for exactly
  these three page models), `CaseWorkflowPersistenceTests.cs` (2,194),
  `VehicleWorkflowTerminalTests.cs`. `CaseCapabilityPagesTestSupport.cs` is the
  single most useful file for this ticket and is not named in the plan set.
- **Target projects do not exist yet.** `Pegasus.slnx` lists four production and
  three test projects. `grep -rn "DesktopGateway" src/ tests/` returns nothing —
  the gate is introduced by [[GWY-002]] (plan handle `DSK-03-02`).

### Assumptions

- **`A-05-20` — [[GWY-008]] (plan handle `DSK-03-08`) and [[GWY-009]] (plan
  handle `DSK-03-09`) will expose nineteen named routes, not a dispatcher.**
  `endpoint-map.md:57-62` lists them as named verbs and
  `docs/desktop/03-gateway-api-and-data/README.md` § 3 forbids a generic action
  endpoint. Confirmed by: reading their delivered route lists at step 3. Breaks
  if wrong: the desktop would have to send an action string, which proposal
  §10.2 forbids — the fix belongs to those tickets.
- **`A-05-21` — the Engineer-role refusal will be translated to a 403
  `not-authorized` problem.** It throws `InvalidOperationException`
  (`src/Pegasus.Core/Cases/CaseContracts.cs:314-316`), which
  `src/Pegasus.Web/Mcp/AutomationMcpErrors.cs:53-59` currently passes through as
  a caller error — i.e. a 400 shape. Confirmed by: a contract fact asserting 403
  for a non-Engineer on command 6. Breaks if wrong: the seven-case matrix's
  "wrong right 403" theory fails for that one command, and the desktop's
  `CanExecute` gating is the only thing stopping a non-Engineer — which the
  design authority says is for usability only.
- **`A-05-22` — task-level `expectedTaskVersion` survives onto the wire.**
  Commands 14–16 carry it and `CaseTaskVersionConflictException` is a distinct
  type. Confirmed by: reading [[GWY-009]]'s task DTOs. Breaks if wrong: two
  operators editing the same task overwrite each other while the case version
  test passes.
- **`A-05-23` — [[FND-046]] (plan handle `DSK-04-10`) will expose the actor's
  rights **and roles**.** `CanExecute` for command 6 needs `StaffRole.Engineer`,
  which is a role, not a `StaffAccessRight`. `GET /session/me` is specified to
  return "actor id, roles, rights, must-change-password flag"
  (`endpoint-map.md:33`), so the data exists. Confirmed by: reading
  [[FND-046]]'s delivered shape. Breaks if wrong: the Record-engineer-finding
  command cannot be disabled for a non-Engineer and every non-Engineer sees a
  server refusal instead.

## Execution placement

The six-question test from `docs/desktop/00-governance-and-workflow/README.md`
§ 3, answered for the case-command responsibility.

| Question | Answer | Evidence |
| --- | --- | --- |
| Shared authority — must several users see and update the same state? | **Yes** | Every command mutates the shared case and carries `expectedVersion`; three of them carry a second, task-level version. Lands in the gateway (`Pegasus.Web`, L-01). |
| Unattended execution — must it run with every desktop closed? | **No, for these nineteen.** | Each is an operator action. Related *automatic* work — the due-chaser sweep, sent-evidence polling — already runs in `Pegasus.Worker` (`DueWorkSweepFunction`, `SentEvidencePollFunction`; `reuse-map.md` § Pegasus.Worker) and stays there. Nothing moves. |
| Protected credentials — long-lived secret that must not sit on workstations? | **No** | No provider secret is involved; the edit-mode token is short-lived, per-case and in-memory ([[FEAT-005]], plan handle `DSK-05-05`). |
| Public callback — must an external service call a stable public endpoint? | **No** | No external party issues a case command. |
| Central enforcement — revocation, permissions, audit, invariant independent of the client? | **Yes** | The product invariants live here: never delete a case; reopen needs a reason and destination gates; principal and reference immutable. `ICaseWorkflowStore` (`src/Pegasus.Core/Workflow/CaseWorkflowContracts.cs:338-342`) makes version check, lease check, state change, idempotency and permanent action history **one atomic transaction**. The Engineer gate (`src/Pegasus.Core/Cases/CaseContracts.cs:309-316`) is enforced in Core. None of it can be trusted to a client — "the desktop hides or disables commands for usability only" (`vertical-slices.md` § Common to every slice). Lands in the gateway. |
| Measured operational advantage — measured evidence central is materially better? | **n/a** | No measurement exists either way and none is needed: questions 1 and 5 already place every command. The desktop builds and confirms; it computes nothing. |

**Placement:** the gateway executes, authorizes and audits all nineteen; the
desktop builds the request, collects the reason and confirms. Two "yes" answers,
both naming the gateway. No Azure resource is involved and no Azure write occurs.

## Implications

- **Eighteen reason dialogs, not "some".** Because `ValidateMutation` makes
  `Reason` mandatory on every `CaseMutationRequest`, the only command without a
  reason dialog is Add note. Building the command bar on "reason where the
  command needs one" and guessing which those are would produce a surface where
  most actions fail at the server.
- **One shared request envelope is the wrong abstraction, and the ticket body
  already says so.** The nineteen differ in real ways — task version on three,
  readiness on three (conditionally on one), an assessment enum on one, an
  approval triple on one, five chase fields on one. The body's step 4 —
  "Do not introduce a shared 'command' bag that hides which fields a given
  command needs" — is the right call, and the inventory above is the evidence.
- **Command 6 needs a role, not a right.** `CanExecute` must read
  `StaffRole.Engineer` from the session, and the gateway must translate the
  `InvalidOperationException` to 403. Both halves are needed; either alone leaves
  a hole.
- **The seven-case matrix from [[TEST-002]] (plan handle `DSK-08-02`) does not
  fit all nineteen unchanged.** "Stale version 409" needs a second variant for
  commands 14–16 (stale **task** version); "wrong right 403" needs the
  role-based variant for command 6; and Add note has neither a version nor a
  lease, so two of the seven cases are inapplicable to it. Say so rather than
  writing a theory that cannot pass.
- **`Created in error` is a closure outcome and a UI rule at once.**
  `CaseClosureOutcome.CreatedInError` (`CaseWorkflowContracts.cs:30`) is one of
  five; the screen spec (`screen-specs.md:204-205`) requires a case in that state
  to "show both references and no reopen control", and the approved sentence
  "Created in error cannot be reopened. Create and link the replacement case."
  (`docs/design/README.md:406`) is the only copy permitted to explain it.
- **There is no Delete, and its absence must be provable.** `AGENTS.md`
  § Product invariants — "Never delete a case." The strongest evidence is a
  view-model fact asserting the command collection contains no delete command at
  all, which the ticket body's step 10 already asks for.
- **`CaseCapabilityPagesTestSupport.cs` is the harness to reuse.** It exists for
  exactly these three page models (166 lines) and is the cheapest route to
  keeping the nineteen web handlers green while the `/api/v1` twins are added.

## Open questions

None that block the plan. `A-05-20` through `A-05-23` are settled by step 3's
reading of the delivered gateway and session contracts, and each has a named
consequence in the plan's *Risks / open questions* section. The upstream items
the Guardrails name — upstream `CASE-002` and `CASE-004`, future capabilities
with no fork ticket — are explicitly **not absorbed**, and the body's
instruction is that a slice needing one stops and raises a ticket; that is a
scope boundary recorded in the plan, not a question. No `open-questions`
document is created; the ticket body does not ask for one.
