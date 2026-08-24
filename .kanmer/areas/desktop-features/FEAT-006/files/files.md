# Files — FEAT-006

Surface area of `DSK-05-06 · S6 Workflow, closure and tasks commands`. Paths
that do not exist at `HEAD` `bbd1c549` are marked with the ticket that creates
them; every other path was confirmed with `ls` or `wc -l`.

## Where the change lands

| Path | Why |
| --- | --- |
| `src/Pegasus.Contracts/` *(created by [[FND-029]] (plan handle `DSK-02-04`))* | **One request DTO per command — nineteen of them.** Each carries `operationKey`, `expectedVersion` and `editLeaseToken` where Core requires them, plus its own extra fields (the `research` inventory names them). Three carry `expectedTaskVersion`; three carry readiness booleans, one of those conditionally; one carries an `AuditAssessment`; one an approval triple; one five chase fields. Risk named by the ticket body: **no shared "command" bag** that hides which fields a command needs. Response DTOs matter too — Create linked replacement must return `isDuplicate` **and** the new reference. |
| `src/Pegasus.Web/` — the `/api/v1` cases **command** group only *(group by [[GWY-002]] (plan handle `DSK-03-02`); routes by [[GWY-008]] (plan handle `DSK-03-08`) and [[GWY-009]] (plan handle `DSK-03-09`))* | Nineteen named routes, each calling the same `src/Pegasus.Core/Lifecycle/` or `src/Pegasus.Core/Tasks/` command the Razor handler calls. Never a dispatcher taking an action string. The Engineer-role refusal on Record engineer finding must translate to **403**, not 400. |
| `src/Pegasus.Desktop/` *(created by [[FND-030]] (plan handle `DSK-02-05`))* | `CaseCommandsViewModel` — one command object per row, each with its own `CanExecute` derived from the loaded case state and the actor's rights **and roles** from [[FND-046]] (plan handle `DSK-04-10`) — plus the command bar in the case header from [[FEAT-003]] (plan handle `DSK-05-03`) and the Tasks tab. |
| `tests/Pegasus.Api.ContractTests/` *(created by [[TEST-001]] (plan handle `DSK-08-01`))* | The [[TEST-002]] (plan handle `DSK-08-02`) seven-case matrix per command, **with the three documented variants** the `research` document identifies: a stale-**task**-version 409 for commands 14–16, a role-based 403 for command 6, and two inapplicable cases for Add note. |
| `tests/Pegasus.Desktop.ViewModelTests/` *(created by [[FND-038]] (plan handle `DSK-02-13`))* | `CanExecute` gating per case state and per right/role; reason-required commands refusing an empty reason; and the invariant fact that **no delete command exists at all**. |
| `docs/desktop/01-inventory-and-parity/parity-matrix.md` | Rows `PAR-10` (`:55`, workflow), `PAR-11` (`:56`, tasks), `PAR-12` (`:57`, closure). |
| `docs/frd/frd-13-desktop-operator-experience.md` *(skeleton by [[FND-008]] (plan handle `DSK-00-08`))* | Command sections. |
| `docs/capabilities.md` | `DSK` rows for the workflow, closure and task commands. |

## Context files

Read these before writing code. Each is here for one specific trap.

| Path | What it tells the implementer |
| --- | --- |
| `src/Pegasus.Core/Lifecycle/CaseLifecycle.cs:414-426` (`ValidateMutation`) | **The five things every case mutation must present**, including a **required `Reason` ≤ 500 characters** (`:420`). Eighteen of the nineteen derive from `CaseMutationRequest` and therefore need a reason — the ticket's "reason dialogs where Core requires one" resolves to eighteen, not "some". Operation key ≤ 100 (`:599`); lease token exactly 64 (`:421-425`); actor needs `PerformCasework` (`:605`). |
| `src/Pegasus.Core/Lifecycle/CaseLifecycle.cs:429-470` | The four extra validators: `ValidateReturnToReview` (readiness), `ValidateAssignment` (non-empty engineer id **and** readiness against the current `CaseWorkflowConfiguration`), `ValidateReportApproval` (approval id, artifact identity ≤ 200, SHA-256 exactly 64 hex), `ValidateReportEvidence` (non-empty evidence id). |
| `src/Pegasus.Core/Lifecycle/CaseLifecycle.cs:393-415` | `IsTerminal` — the five terminal states — and `TerminalStateNames()`, derived from `IsTerminal` "so the two cannot drift: a state that is terminal here but missing from a hand-written copy elsewhere is silently non-terminal for whatever that copy guards (INTK-029)". Do not restate the list in a desktop `CanExecute`; derive it. |
| `src/Pegasus.Core/Lifecycle/CaseLifecycle.cs:11-41` | The two state preconditions in plain form: "Only an open case can be held." (`:18`) and "Only a held case can be released." (`:36`). Each is checked **unless the operation key has already been applied** (`:16`, `:34`) — that is the replay path, and a `CanExecute` that mirrors the state check without the replay allowance will disable a legitimate retry. |
| `src/Pegasus.Core/Cases/CaseContracts.cs:300-319` | The one role gate: Record engineer finding requires `ActorKind.Staff`, `IsInRole(StaffRole.Engineer)` and a `Guid`-parseable `SubjectId`, and throws **`InvalidOperationException`** — not `StaffAuthorizationException`. An endpoint filter keyed on `StaffAccessRight` cannot express it, and the default mapping (`src/Pegasus.Web/Mcp/AutomationMcpErrors.cs:53-59`) would make it a 400. |
| `src/Pegasus.Core/Workflow/CaseWorkflowContracts.cs:25-39` | `CaseClosureOutcome` (five values, `:25-32`) and `CaseReopenDestination` (four values, `:34-39`). "Close with named outcome" means one of these five, never a generic Close. |
| `src/Pegasus.Core/Workflow/CaseWorkflowContracts.cs:182-188`, `:305-314` | `CaseMutationRequest` — the base every reasoned command derives from — and `ReopenCaseRequest`, whose `Readiness` is **nullable**. |
| `src/Pegasus.Core/Workflow/CaseWorkflowContracts.cs:338-342` (`ICaseWorkflowStore`) | "Each operation is one atomic transaction: optimistic-version and lease checks, case/due-work change, exact evidence link where supplied, idempotency, and permanent action history either all commit or all fail." |
| `src/Pegasus.Core/Tasks/CaseTaskContracts.cs:21-31` | `CaseTaskVersionConflictException(TaskId, ExpectedVersion, ActualVersion)` — the **second** version token. Commands 14–16 carry `expectedTaskVersion`; flattening it into the case version silently loses task-level concurrency. |
| `src/Pegasus.Web/Pages/Cases/Workflow.cshtml.cs` (227 lines) | Seven handlers at `:26`, `:42`, `:64`, `:98`, `:133`, `:156`, `:180`, and the class summary (`:9-13`) naming them. `OnPostCreateLinkedReplacementAsync` (`:180-226`) is hand-rolled and returns business content: `outcome.IsDuplicate` and `outcome.Identity.Reference` (`:207-211`). |
| `src/Pegasus.Web/Pages/Cases/Closure.cshtml.cs` (121 lines) | Four handlers at `:23`, `:52`, `:69`, `:106`. **Reopen supplies readiness only when the destination is `Review`** (`:98-105`) — a DTO that always sends it changes behaviour for the other three destinations. |
| `src/Pegasus.Web/Pages/Cases/Tasks.cshtml.cs` (248 lines) | Eight handlers at `:33`, `:61`, `:89`, `:117`, `:143`, `:169`, `:201`, `:225`. The remarks at `:28-32` are the exception to "every command needs a lease and a version": **Add note takes neither**, deliberately (CASE-017), "so it must not contend with an engineer editing the same case". |
| `src/Pegasus.Web/Pages/Cases/CaseMutationPageModel.cs:110-174` | `ExecuteCaseCommandAsync` / `ExecuteTransportCommandAsync` and the shared refusal sentences — the difference between "the case changed…" and "the item is unavailable…" is deliberate. Sixteen of the nineteen route through here; the PRG and `TempData` around it do not travel. |
| `src/Pegasus.Core/Identity/StaffAuthorization.cs` (77 lines) | Twelve rights (`:8-20`); the fail-closed matrix (`:33-56`). `PerformCasework` admits Staff **or** Automation (`:39-41`); `StaffRole.Engineer` is not a right and is not in this file's matrix. |
| `AGENTS.md` § Product invariants | The four this surface must uphold: never delete a case; reopen needs a reason and normal destination gates; principal and reference immutable after allocation, with `Created in error` plus a linked replacement and neither reference reused; duplicate business implementation is a stop condition. |
| `docs/design/README.md:400-409` | The closed necessary-copy list. Two sentences belong here: "Created in error cannot be reopened. Create and link the replacement case." and "Blocked — a reason is required." Nothing else may be written. |
| `docs/design/README.md:430-434` | "No how-it-works copy… no 'how this figure is calculated' prose, no introductory sentences under headings. The only exception is an individually approved consequence sentence from the closed necessary-copy list above." |
| `docs/desktop/06-ui-design/screen-specs.md:198-205` | The command vocabulary, fixed: the eleven named lifecycle actions, "never a generic Close", "every reasoned action through the `ReasonDialog` contract", and "`Created in error` shows both references and no reopen control". |
| `docs/desktop/06-ui-design/screen-specs.md:187-190`, `:214`, `:225-227` | The action bar ("one primary = the next permitted action; others default"), the Tasks tab contents, and the AutomationIds `Case.Actions.<Action>` and `Case.<Tab>.<Section>.<Element>`. |
| `docs/desktop/03-gateway-api-and-data/endpoint-map.md:57-62` | Every one of the nineteen routes as a named verb, with `PerformCasework` throughout, "(engineer finding: Engineer role)" called out at `:57`, and "reopen requires `reason`" at `:58`. |
| `docs/desktop/03-gateway-api-and-data/README.md` § 3 | "Commands are explicit verbs (`POST …/hold`), **never a generic action endpoint**." |
| `tests/Pegasus.IntegrationTests/CaseCapabilityPagesTestSupport.cs` (166 lines) | The shared harness built for exactly these three page models. The cheapest route to keeping the nineteen web handlers green while the `/api/v1` twins are added. Not named in the plan set. |
| `tests/Pegasus.IntegrationTests/CaseWorkflowWebTests.cs` (124), `CaseClosureWebTests.cs` (121), `CaseTasksWebTests.cs` (181) | The three route-level oracles, one per page model. |
| `tests/Pegasus.IntegrationTests/CaseWorkflowPersistenceTests.cs` (2,194) | The lifecycle persistence oracle; must stay green. |
| `tests/Pegasus.IntegrationTests/IntakeWebTestSupport.cs:26` | The shared `WebApplicationFactory<Program>`; `Features:DesktopGateway` must be enabled explicitly. |
| `docs/desktop/08-testing/test-uat-stack.md:22` | The Test/UAT configuration for the operator UAT run. |

## Ripple effects

- **Generated client and OpenAPI snapshot.** Nineteen new command shapes.
  [[GWY-005]] (plan handle `DSK-03-05`) commits Kiota output with a CI no-op
  check; [[TEST-001]] (plan handle `DSK-08-01`) fails the snapshot test on an
  undeclared change. This is the largest single addition to both in the S1–S8
  set.
- **The [[TEST-002]] template gains three documented variants.** The seven-case
  matrix (plan handle `DSK-08-02`) needs a stale-task-version case, a role-based
  403 case, and a recorded exemption for Add note's two inapplicable cases.
  Coordinate rather than silently skipping coverage — that template "fails when a
  command lacks coverage".
- **[[FEAT-003]]'s command-bar slot is filled here.** The header layout is
  already built; this ticket populates it.
- **[[FEAT-005]]'s edit session gates most of these commands.** Eighteen of the
  nineteen require a 64-character `editLeaseToken`, which `CaseEditSession`
  holds. `CanExecute` must therefore read edit-mode state as well as case state.
- **[[FEAT-008]] (plan handle `DSK-05-08`) renders every refusal.** The four
  problem types plus `CaseTaskVersionConflictException` all surface through its
  one recovery pattern; a bespoke conflict message on any of the nineteen would
  be a second pattern.
- **[[FND-046]] (plan handle `DSK-04-10`) must expose roles, not only rights.**
  Command 6's `CanExecute` needs `StaffRole.Engineer`.
- **Existing web tests must stay green.** Nothing here touches
  `Workflow.cshtml.cs`, `Closure.cshtml.cs` or `Tasks.cshtml.cs`, so
  `CaseWorkflowWebTests`, `CaseClosureWebTests`, `CaseTasksWebTests`,
  `CaseReportApprovalWebTests`, `CaseNotePersistenceTests` and
  `CaseWorkflowPersistenceTests` must pass unchanged.
- **Downstream tickets.** `FEAT-006` blocks `FEAT-022` and `FEAT-025`.
- **Documentation link check.** `scripts/Test-DocumentationLinks.ps1` runs over
  repository documentation, so a broken relative link in the new FRD-13 sections
  fails CI.

## Out of scope

Recorded so the reviewer sees each was a decision.

- **`Pages/Cases/Workflow.cshtml.cs`, `Closure.cshtml.cs` and `Tasks.cshtml.cs`
  are not modified.** They stay live until `PAR-10`–`PAR-12` reach `cut over`;
  the cut is [[FEAT-026]] (plan handle `DSK-05-26`).
- **No generic execute endpoint, and no action-string dispatcher.** Forbidden by
  `docs/desktop/03-gateway-api-and-data/README.md` § 3 and proposal §10.2.
- **No shared "command" request bag.** Each command names the fields it needs.
- **No Delete command of any kind.** `AGENTS.md` § Product invariants — "Never
  delete a case." Its absence is asserted by a view-model fact.
- **No new consequence copy.** Only the closed approved list at
  `docs/design/README.md:400-409`.
- **Upstream `CASE-002` and `CASE-004` are not absorbed.** They are future
  capabilities with no fork ticket; a slice that needs one **stops and raises a
  ticket** (ticket Guardrails, and plan 05 § 7's §13.11 scope-creep rule).
- **No lease session work.** Claim, renew, release and the dirty-state machinery
  are [[FEAT-005]] (plan handle `DSK-05-05`).
- **No conflict-and-recovery UX.** That single pattern is [[FEAT-008]] (plan
  handle `DSK-05-08`).
- **No assessment, report or custody commands.** Those belong to [[FEAT-017]],
  [[FEAT-018]] and [[FEAT-014]] respectively.
- **No Azure write.** Enabling `Features:DesktopGateway` in production is
  [[PLAT-024]] (plan handle `DSK-11-06`).
