# Plan — FEAT-006: S6 Workflow, closure and tasks commands

**Diff estimate: ~14 files, ~3,100 lines.**

Derived from the files document: 3–4 `Pegasus.Contracts` files holding
**nineteen** request DTOs plus their responses (~620 lines — nineteen records
averaging thirty lines with their extra fields); 2–3 `/api/v1` endpoint files
(~700, nineteen named routes plus the Engineer-role translation); 3 desktop
files — `CaseCommandsViewModel` with nineteen command objects and their
`CanExecute` (~420), the command bar XAML (~260), the Tasks tab XAML (~300); 2
test files — contract tests running the seven-case matrix over nineteen
commands (~640, the largest single file in the S1–S8 set) and view-model tests
(~280); ~2 regenerated Kiota files (~350, generated); 3 documentation edits. The
596 lines of page model this replaces (227 + 121 + 248) are **not** in the diff.
The count is dominated by breadth, not depth: nineteen small, similar,
independently named things.

## Approach

Build one command object per handler and one endpoint per command, and let the
differences between them stay visible. The nineteen are not variations of a
theme — three carry a second, task-level version; three carry readiness
booleans, one of those only for a single destination; one carries an assessment
enum; one an approval triple; one five chase fields; and one carries neither a
version nor an edit-mode token at all. The rejected alternative was a shared
command envelope with optional members: it would compile, and it would hide
which fields a given command actually needs — which is exactly what the ticket
body's step 4 forbids and what makes `expectedTaskVersion` easy to drop. The
second rejected alternative was a single `POST /cases/{id}/commands` taking a
verb: forbidden outright by
`docs/desktop/03-gateway-api-and-data/README.md` § 3 and proposal §10.2, and it
would make the per-command authorization filter impossible to express.

## Governing docs

### Meets — the ticket's `refs`

`refs`: `docs/frd/frd-01-case-identity-and-lifecycle.md`.

| Requirement | Where | Met by |
| --- | --- | --- |
| "Every staff case mutation targets one identified case through a named Core action and requires the role permitted by the staff role access matrix." | `frd-01:84` | Steps 3–5 — nineteen named routes, one Core action each, with `CanExecute` derived from the actor's rights **and roles** |
| "Every save, transition, assignment, association, evidence change, and other staff mutation presents both the lease token and the Case version loaded by that editor." | `frd-01:84` | Step 4 (each DTO carries `expectedVersion` and `editLeaseToken` where Core requires them — eighteen of nineteen; Add note is the documented exception, `Tasks.cshtml.cs:28-32`) |
| "Core refuses a missing, expired, wrong-holder, or stale-version mutation without overwriting newer work." | `frd-01:86` | Step 9 (the seven-case matrix, including stale version 409 and the stale-**task**-version variant) |
| "There is no Administrator bypass, forced takeover, collaborative merge, bulk case mutation, queue-inline lifecycle edit…" | `frd-01:86` | The Out-of-scope boundary and step 5 — one control mutates one current case; no bulk action exists |
| "Lifecycle closure and correspondence" — the five terminal outcomes and the reopen gates | `frd-01:41-80` | Steps 6–7 (Close takes a named `CaseClosureOutcome`; Reopen takes a `CaseReopenDestination` **and** a reason, with readiness supplied only for `Review`) |
| "`Due by` comes from the inspection date or accepted equivalent deadline… Manual chasing remains a staff action in the alpha… The history records what was attempted, by whom, through which channel, against which party/address, when, and with what evidence." | `frd-01:92-96` | Step 8 (Record manual chase carries `attemptedAtUtc`, `channel`, `targetPartyOrAddress`, `outcome`, `note?` — exactly the five the FRD names) |
| "A recorded action is not proof of external delivery." | `frd-01:96` | Step 6 — no command's success copy claims delivery; the two report-evidence commands say "the exact retained report-Sent evidence was linked", not "sent" |

### New ADR

The ticket carries `docs_todo: true` (confirmed in `get_doc_gates FEAT-006`).

> **New ADR** — ADR-0103 (gateway = evolved `Pegasus.Web`; never direct database
> access from a workstation) and ADR-0101 (local-execution / cloud-authority
> split and the six-question test), authored by [[FND-005]] (plan handle
> `DSK-00-05`).
> This plan is written to the decisions as recorded in
> `docs/desktop/00-governance-and-workflow/README.md` § 3 and to L-01 in
> `docs/desktop/README.md` § Locked decisions; if either ADR lands differently
> this plan is revised before implementation.

ADR-0100 has more than one interested party through the no-split deviation
recorded in `docs/desktop/05-implementation-and-migration/README.md` § 3; it is
authored by [[FND-026]] (plan handle `DSK-02-01`); see [[FND-026]]'s plan for
the ownership reconciliation.

### Programme-level authorities that bind today

| Authority | Requirement | Met by |
| --- | --- | --- |
| `AGENTS.md` § Product invariants | Never delete a case; reopen needs a reason and destination gates; principal and reference immutable, `Created in error` plus a linked replacement, neither reference reused | Steps 6–7 and step 10 (the no-delete fact) |
| `docs/engineering.md` § One Core owner | One policy owner per rule; a rule found only in a page model moves into Core with a test first | Step 3 |
| `docs/engineering.md` § Plan sizing | Diff estimate first, derived from the files document | First line |
| `docs/engineering.md` § Required evidence tiers | Tier 5 obliges route-level evidence per command including the correct action-history actor | Step 9 |
| `docs/desktop/03-gateway-api-and-data/README.md` § 3 | Commands are explicit verbs; never a generic action endpoint | Steps 3–4 |
| Proposal §10.2 | Same | Steps 3–4 |
| `docs/design/README.md:400-409` | Closed necessary-copy list — only two sentences belong to this surface | Step 6 |
| `docs/design/README.md:430-434` | No how-it-works copy; the only exception is an approved consequence sentence | Step 6 |
| `docs/desktop/06-ui-design/screen-specs.md:198-205` | The eleven named lifecycle actions; never a generic Close; every reasoned action through `ReasonDialog`; `Created in error` shows both references and no reopen control | Steps 6–7 |
| L-01 / L-02 / L-04 (`docs/desktop/README.md`) | Gateway owns commands, audit and authorization; verification on the local stack; routing named | Steps 3, 9, 11; § Routing |
| Plan 05 § 7 | `/api/v1` gated off returns 404; §13.11 scope creep — a slice needing an unabsorbed capability stops and raises a ticket | Step 9 and the Out-of-scope boundary |

## Routing

Copied from the ticket body's `## Routing` block.

- **Subagent**: `winui-dev` — `.codex/agents/winui-dev.toml`;
  `pegasus-gateway-dev` — `.codex/agents/pegasus-gateway-dev.toml`;
  `pegasus-test-engineer` — `.codex/agents/pegasus-test-engineer.toml`.
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `dotnet-webapi`
  (dotnet/skills `98f84851`, `plugins/dotnet-aspnetcore/skills/dotnet-webapi/SKILL.md`)
  → `winui-dev-workflow` (`.codex/skills/winui-dev-workflow/SKILL.md`) →
  `winui-design` (`.codex/skills/winui-design/SKILL.md`) → `code-testing-agent`
  (dotnet/skills `98f84851`) → `run-tests` → `winui-code-review` at review.
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`,
  `set_ticket_doc`, `append_scratch`, `move_item`); Microsoft Learn
  (`microsoft_docs_search`).
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` →
  `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout`
  (call `get_doc_gates <id>` before every move; a move crosses at most one
  gated boundary).
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement
  (`AGENTS.md` § Repository task workflow step 5).

## Steps

These refine the ticket body's twelve steps in the same order and with the same
ownership.

1. **Orient and take.** Read the plan row, `vertical-slices.md` § S6,
   `AGENTS.md` § Product invariants and `docs/design/README.md:400-409` (the
   closed consequence list). Then `get_doc_gates FEAT-006` and `take_ticket`
   with branch `task/dsk-05-06-case-commands`, worktree
   `../pegasus-worktrees/dsk-05-06-case-commands`, from `origin/dev`.
2. **Confirm the nineteen-row inventory.** The `research` document carries it in
   full — handler, Core use case, extra fields, gate. Re-verify with
   `git diff --stat bbd1c549..HEAD -- src/Pegasus.Web/Pages/Cases/Workflow.cshtml.cs src/Pegasus.Web/Pages/Cases/Closure.cshtml.cs src/Pegasus.Web/Pages/Cases/Tasks.cshtml.cs src/Pegasus.Core/Lifecycle src/Pegasus.Core/Tasks`;
   if the upstream sync moved any of them, re-read and update `research` with
   the new SHA. The recorded SHA is `bbd1c549`. Add the exception types each
   command can throw to the inventory before writing the contract theories.
3. **Confirm each row has its own named endpoint** in [[GWY-008]] (plan handle
   `DSK-03-08`) and [[GWY-009]] (plan handle `DSK-03-09`) — never a dispatcher
   taking an action string (assumption `A-05-20`). Where an endpoint is missing,
   add it to the `/api/v1` group calling the same `src/Pegasus.Core/Lifecycle/`
   or `src/Pegasus.Core/Tasks/` command the Razor handler calls. Two checks that
   are easy to miss:
   - the **Engineer-role** refusal on Record engineer finding
     (`src/Pegasus.Core/Cases/CaseContracts.cs:309-316`) throws
     `InvalidOperationException`, not `StaffAuthorizationException`, and must be
     translated to **403 `not-authorized`** rather than the default 400 shape
     (assumption `A-05-21`);
   - commands 14–16 carry **`expectedTaskVersion`** and their conflict is
     `CaseTaskVersionConflictException`
     (`src/Pegasus.Core/Tasks/CaseTaskContracts.cs:21-31`), a different type from
     the case version conflict (assumption `A-05-22`).
4. **One request DTO per command** in `src/Pegasus.Contracts`, each carrying
   `operationKey`, `expectedVersion` and `editLeaseToken` **where Core requires
   them**, and `reason` where Core requires it — which is **eighteen of the
   nineteen**, because `CaseLifecycleRules.ValidateMutation` makes `Reason`
   mandatory on `CaseMutationRequest` (`src/Pegasus.Core/Lifecycle/CaseLifecycle.cs:420`).
   Add note is the one exception (`src/Pegasus.Web/Pages/Cases/Tasks.cshtml.cs:28-32`,
   CASE-017). **Do not introduce a shared "command" bag** that hides which
   fields a given command needs. Two response shapes carry business content and
   must not be flattened to a version: Create linked replacement returns
   `isDuplicate` **and** the new reference (`Workflow.cshtml.cs:207-211`).
   Reopen's readiness is **nullable and supplied only for destination `Review`**
   (`Closure.cshtml.cs:98-105`).
5. **`CaseCommandsViewModel`** in `src/Pegasus.Desktop`: one command object per
   row, each with its own `CanExecute` derived from the loaded case state, the
   held edit-mode state from [[FEAT-005]] (plan handle `DSK-05-05`), and the
   actor's rights **and roles** from [[FND-046]] (plan handle `DSK-04-10`)
   (assumption `A-05-23`). Two rules for `CanExecute`:
   - derive terminal state from `CaseLifecycleRules.IsTerminal`
     (`CaseLifecycle.cs:393-399`), never from a restated list — the file itself
     warns that a hand-written copy drifts silently (INTK-029);
   - allow a command whose state precondition fails but whose operation key has
     already been applied, mirroring the replay allowance at
     `CaseLifecycle.cs:16` and `:34`; otherwise a legitimate retry is disabled.
   The desktop hides or disables **for usability only**; the gateway remains the
   enforcement point.
6. **The command bar** in the case header from [[FEAT-003]] (plan handle
   `DSK-05-03`): a named verb per command, **never a generic "Close"**; one
   primary action = the next permitted action, others default
   (`docs/desktop/06-ui-design/screen-specs.md:187-190`). Permanent consequences
   visible without hover, using **only** the approved sentences — for this
   surface, "Created in error cannot be reopened. Create and link the
   replacement case." (`docs/design/README.md:406`). A case in `Created in error`
   shows both references and **no reopen control**
   (`screen-specs.md:204-205`). Every control carries an `AutomationId`
   (`Case.Actions.<Action>`).
7. **The reason dialog** from [[DUI-009]] (plan handle `DSK-06-09`) for every
   command Core requires a reason for — eighteen of the nineteen, reopen among
   them: named requirement, labelled reason field, verb-labelled primary button
   plus Cancel, initial focus on the reason field.
8. **The Tasks tab**: add note, create / assign / complete / cancel task, record
   manual chase, link and unlink report evidence — each an explicit action with
   its own operation key and the **task-level** `expectedVersion` where Core uses
   one. Record manual chase carries the five fields the FRD names —
   `attemptedAtUtc`, `channel`, `targetPartyOrAddress`, `outcome`, `note?`
   (`Tasks.cshtml.cs:169-198`, `docs/frd/frd-01-case-identity-and-lifecycle.md:96`).
   No success message may claim external delivery.
9. **Contract tests** in `tests/Pegasus.Api.ContractTests` running the
   [[TEST-002]] (plan handle `DSK-08-02`) seven-case matrix over every one of the
   nineteen commands: success, 401, wrong right 403, stale version 409, bad
   input 400 problem, replayed operation key returning the same result, and the
   Core-specific failure path. **Three documented variants** are required and
   must be recorded in the test file rather than silently skipped:
   - commands 14–16 additionally get a stale-**task**-version 409;
   - command 6's "wrong right 403" is role-based, not right-based;
   - Add note has neither a version nor an edit-mode token, so the stale-version
     and lease cases are inapplicable to it.
   Enable `Features:DesktopGateway` explicitly.
10. **View-model tests** in `tests/Pegasus.Desktop.ViewModelTests` (project from
    [[FND-038]], plan handle `DSK-02-13`): `CanExecute` gating per case state and
    per right/role; the replay allowance not disabling a legitimate retry;
    reason-required commands refusing to execute with an empty reason; a
    `Created in error` case exposing no reopen control; and the invariant fact
    that **no delete command exists at all** (`AGENTS.md` § Product invariants).
11. **Operator step — UAT of the primary case workflow**: hold / release,
    return to review, assign engineer, start work, record finding, create linked
    replacement, record report approval, close, reopen with reason, archive.
    Capture the operator's sign-off text and date in `proof`.
12. **Documentation and PR.** Update
    `docs/desktop/01-inventory-and-parity/parity-matrix.md` rows `PAR-10`
    (`:55`, workflow), `PAR-11` (`:56`, tasks) and `PAR-12` (`:57`, closure); add
    the command sections to `docs/frd/frd-13-desktop-operator-experience.md` and
    `DSK` rows to `docs/capabilities.md`. Run the simplification pass over the
    branch diff (`AGENTS.md` step 4), record it under a dated
    `## Simplification pass` heading here, then open the PR into `dev`.

## Verification

Evidence tiers from the body: **tier 5** (Web/API/MCP caller), **tier 7**
(Browser/accessibility).

| Command | Expected | Evidence captured |
| --- | --- | --- |
| `dotnet test ./tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj --configuration Release --no-build` | The seven-case matrix passes for each of the nineteen commands, plus the three documented variants | Test summary — **tier 5 evidence**, including the correct action-history actor per command |
| `dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release --no-build` | `CanExecute`, replay-allowance, reason-required, no-reopen-on-`Created in error` and no-delete facts pass | Test summary |
| `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --no-build --filter "Category!=Corpus&Category!=Browser"` | `CaseWorkflowWebTests`, `CaseClosureWebTests`, `CaseTasksWebTests`, `CaseReportApprovalWebTests`, `CaseNotePersistenceTests` and `CaseWorkflowPersistenceTests` stay green | Test summary (proves the web path was not disturbed) |
| `dotnet build ./Pegasus.slnx --configuration Release --no-restore` | Succeeds under `TreatWarningsAsErrors=true` with no `WUI*` suppression | Build log tail |
| UAT of the eleven lifecycle commands on the Test/UAT stack | Each command executes, is audited, and its consequence copy matches the approved list | Named operator sign-off with date in `proof` — **tier 7 evidence** |

## Risks / open questions

- **Risk: the reason requirement is under-applied.** Eighteen of the nineteen
  commands need a reason because `ValidateMutation` makes it mandatory on
  `CaseMutationRequest` (`src/Pegasus.Core/Lifecycle/CaseLifecycle.cs:420`);
  reading the ticket's "where Core requires one" as "some" would produce a
  surface where most actions fail at the server. *Mitigation:* the `research`
  inventory states it per row, step 4 restates it, and step 10 has a fact.
- **Risk: `expectedTaskVersion` is flattened into the case version.** Commands
  14–16 have a second version token whose conflict is a different type
  (`src/Pegasus.Core/Tasks/CaseTaskContracts.cs:21-31`). *Mitigation:* step 3's
  second check, step 4's per-command DTO, and the stale-task-version variant in
  step 9.
- **Risk: the Engineer-role refusal arrives as a 400.** It throws
  `InvalidOperationException` (`src/Pegasus.Core/Cases/CaseContracts.cs:314-316`),
  which the existing mapping passes through as a caller error
  (`src/Pegasus.Web/Mcp/AutomationMcpErrors.cs:53-59`). *Mitigation:* step 3's
  first check and step 9's role-based 403 fact. If [[GWY-008]] cannot express it
  in a `StaffAccessRight` filter, raise it there — the endpoint map already
  flags it (`endpoint-map.md:57`).
- **Risk: `CanExecute` disables a legitimate retry.** Core allows a command
  whose state precondition fails **when the operation key has already been
  applied** (`CaseLifecycle.cs:16`, `:34`). *Mitigation:* step 5's second rule,
  with a view-model fact.
- **Risk: a restated terminal-state list drifts.**
  `CaseLifecycleRules.TerminalStateNames()`'s own remarks
  (`CaseLifecycle.cs:400-408`) record that this already happened once
  (INTK-029). *Mitigation:* step 5's first rule — derive, never restate.
- **Risk: reopen always sends readiness.** The web sends it only for destination
  `Review` (`src/Pegasus.Web/Pages/Cases/Closure.cshtml.cs:98-105`).
  *Mitigation:* step 4, with a contract fact per destination.
- **Risk: new consequence copy is written.** The list is closed
  (`docs/design/README.md:400-409`) and nothing in CI enforces it (`:417-420`).
  *Mitigation:* step 6 names the one sentence this surface may use; the reviewer
  is the gate.
- **Scope boundary: upstream `CASE-002` and `CASE-004`** are future
  capabilities with **no fork ticket** and are explicitly **not absorbed** here
  (`vertical-slices.md` § S6). A slice that needs one **stops and raises a
  ticket** rather than building a partial surface.
- **Scope boundary: [[TEST-002]]'s template gains three variants.** The
  seven-case matrix (plan handle `DSK-08-02`) "fails when a command lacks
  coverage", so the two inapplicable cases for Add note must be recorded as
  exemptions in that template rather than silently skipped. Coordinate with
  [[TEST-002]].
- **Scope boundary: the conflict-and-recovery UX** for every refusal above is
  [[FEAT-008]] (plan handle `DSK-05-08`); a bespoke conflict message on any of
  the nineteen would be a second pattern.
- **Not an open question: the operator decisions are settled.** D-002, D-003 and
  D-004 do not touch this ticket, which performs no Azure write.

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over
this branch's own diff before the PR, recorded here under a dated heading._
