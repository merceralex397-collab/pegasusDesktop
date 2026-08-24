# Plan — FEAT-005: S5 Case edit with lease, version and completeness

**Diff estimate: ~17 files, ~2,400 lines.**

Derived from the files document: 4 `Pegasus.Contracts` DTO files (~280 lines —
the save request alone carries eighteen fields); 1–2 `/api/v1` command-endpoint
files (~300, five routes plus four typed 409 translations); 1
`Pegasus.Desktop.Infrastructure` file for `CaseEditSession` (~220 — claim,
renew timer, release, `LeaseLost`); 3 desktop files — edit state added to
`CaseWorkspaceViewModel` (~280), the edit-mode XAML on the Overview tab (~260),
the completeness command view (~120); 4 test files — ViewModel (~360), contract
(~300), the two-user LocalDB test (~220), UI script (~90); ~2 regenerated Kiota
files (~180, generated); 3 documentation edits. `src/Pegasus.Core` is expected
to be **untouched** — the `research` document found every completeness rule
already Core-owned — so no characterization move is budgeted.

## Approach

Put the lease in a dedicated session object and the edit state in the view
model, and let Core's own `CaseDataPolicy` do the field validation on both
sides. `CaseEditSession` (in `Pegasus.Desktop.Infrastructure`) owns claim,
renew and release, drives its timer from the `ExpiresAtUtc` the gateway returns,
and raises `LeaseLost` when a renew fails; the workspace view model owns dirty
state, the navigation guard and the deliberate `SaveCommand`. The rejected
alternative was keeping the lease inside the workspace view model: it makes the
timer a UI concern, gives every later editing slice a different copy of the
renew logic, and puts a secret-shaped 64-character token in the same object that
XAML binds to. The second rejected alternative was validating only server-side
and rendering the 400s: `CaseDataPolicy.Normalize`
(`src/Pegasus.Core/Cases/CaseDataOperations.cs:121-205`) is pure and lives in
`Pegasus.Core`, which the reuse-map boundary note explicitly permits the desktop
to reference, so running the same eighteen field rules locally costs nothing and
gives the operator an answer before a round trip — and the store re-checks them
inside the transaction anyway.

## Governing docs

### Meets — the ticket's `refs`

`refs`: `docs/frd/frd-01-case-identity-and-lifecycle.md`.

| Requirement | Where | Met by |
| --- | --- | --- |
| "Entering edit mode acquires the case's one server-owned expiring lease." | `frd-01:84` | Step 4 (`CaseEditSession.Claim` on entering edit) |
| "Other authorised staff remain read-only and can see the holder and recovery state." | `frd-01:84` | Step 8 (the lease-taken state names the holder through `CaseEditAuthorityHolder`) |
| "Every save, transition, assignment, association, evidence change, and other staff mutation presents both the lease token and the Case version loaded by that editor." | `frd-01:84` | Step 7 (`expectedVersion` + `editLeaseToken` + a fresh `operationKey` per attempt) |
| "The holder may leave editing; an abandoned lease expires by server time and may then be reacquired." | `frd-01:86` | Step 4 (release on exit; the renew timer reads `ExpiresAtUtc`, never a client clock assumption) |
| "Core refuses a missing, expired, wrong-holder, or stale-version mutation without overwriting newer work." | `frd-01:86` | Steps 3 and 11 (the four typed refusals, and the two-user LocalDB test proving A's write is intact) |
| "The rejected editor keeps proposed values for comparison and must reload and reacquire rather than merge or force the save." | `frd-01:86` | Steps 5 and 8 (proposed values stay in the view model; the desktop clears its held token on a stale-version refusal too, matching `RequiresReacquisition`) |
| "There is no Administrator bypass, forced takeover, collaborative merge, bulk case mutation, queue-inline lifecycle edit, provider case-edit route, or direct external-system or adapter edit." | `frd-01:86` | The Out-of-scope boundary — no such control is built, and building one is a stop condition |
| "Web and MCP Automation Actor callers use the same guard." | `frd-01:88` | Step 3 (the endpoints call the same `IAcquireCaseEditLease` / `ISaveCase` / `IConfirmCompleteness` the Razor handlers call) |
| "A deliberate recovery or material denial/failure is attributable permanent history; routine renewal, expiry, heartbeat, polling, and adapter mechanics remain telemetry." | `frd-01:88` | Step 4 (the renew timer is telemetry, not history — the desktop raises no business event for a renewal) |

### New ADR

The ticket carries `docs_todo: true` (confirmed in `get_doc_gates FEAT-005`).

> **New ADR** — ADR-0103 (gateway = evolved `Pegasus.Web`; never direct database
> access from a workstation) and ADR-0104 (online-required, bounded local cache
> only), authored by [[FND-005]] (plan handle `DSK-00-05`).
> This plan is written to the decisions as recorded in
> `docs/desktop/00-governance-and-workflow/README.md` § 3 and to L-01 in
> `docs/desktop/README.md` § Locked decisions; if either ADR lands differently
> this plan is revised before implementation. **ADR-0104 bounds step 5: unsaved
> edits live in the view model, and any local draft is encrypted and bounded —
> it is not offline replication, and saves are never queued while offline
> (step 6).**

ADR-0100 has more than one interested party through the no-split deviation
recorded in `docs/desktop/05-implementation-and-migration/README.md` § 3; it is
authored by [[FND-026]] (plan handle `DSK-02-01`); see [[FND-026]]'s plan for
the ownership reconciliation.

### Programme-level authorities that bind today

| Authority | Requirement | Met by |
| --- | --- | --- |
| L-01 (`docs/desktop/README.md` § Locked decisions) | The gateway owns lease, version and audit | Step 3 |
| L-02 (same) | The two-user test runs against LocalDB in the local Test/UAT stack, never an Azure test resource | Steps 11–12 |
| L-04 (same) | Routing named on the ticket | § Routing below |
| `AGENTS.md` § Product invariants | Never delete a case; principal and reference immutable; duplicate business implementation is a stop condition | Out-of-scope boundary and step 3 |
| `docs/engineering.md` § One Core owner | One policy owner per rule | Step 9 (a completeness rule found only in the page model moves into Core with a test first — the expected count is zero) |
| `docs/engineering.md` § Plan sizing | Diff estimate first, derived from the files document | First line |
| `docs/engineering.md` § Required evidence tiers | Tier 4 obliges lease, stale-version and concurrency evidence against a **real LocalDB** with action-history atomicity | Step 11 |
| `docs/design/README.md:412-420` | `lease` is a banned operator word | Steps 4 and 8 — operator copy says "edit mode", reusing the settled sentences the web already ships |
| `docs/desktop/06-ui-design/screen-specs.md:191-197` | Reload / compare / reacquire are the only recovery actions; no forced takeover | Step 8 and the Out-of-scope boundary |
| `docs/desktop/00-governance-and-workflow/README.md` § 3 | Six-question test answered with evidence | `research` § Execution placement |
| Plan 05 § 7 | `/api/v1` gated off returns 404; tests enable `Features:DesktopGateway` explicitly | Step 11 |
| Proposal §10.4, §14.5, §14.9 | Detected concurrency; deliberate save; `Ctrl+S` | Steps 5–8 |

## Routing

Copied from the ticket body's `## Routing` block.

- **Subagent**: `winui-dev` — `.codex/agents/winui-dev.toml`;
  `pegasus-gateway-dev` — `.codex/agents/pegasus-gateway-dev.toml`;
  `pegasus-test-engineer` — `.codex/agents/pegasus-test-engineer.toml`;
  `pegasus-desktop-reviewer` — `.codex/agents/pegasus-desktop-reviewer.toml`
  (independent review of the concurrency behaviour).
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `winui-dev-workflow`
  (`.codex/skills/winui-dev-workflow/SKILL.md`) → `winui-design`
  (`.codex/skills/winui-design/SKILL.md`) → `dotnet-webapi` (dotnet/skills
  `98f84851`) → `test-gap-analysis` (dotnet/skills `98f84851`,
  `plugins/dotnet-test/skills/test-gap-analysis/SKILL.md`) → `run-tests` →
  `winui-code-review` at review.
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

These refine the ticket body's thirteen steps in the same order and with the
same ownership.

1. **Orient and take.** Read the plan row, `vertical-slices.md` § S5 and
   § `Common to every slice`, and
   `docs/frd/frd-01-case-identity-and-lifecycle.md:82-88`. Then
   `get_doc_gates FEAT-005` and `take_ticket` with branch
   `task/dsk-05-05-case-edit`, worktree
   `../pegasus-worktrees/dsk-05-05-case-edit`, from `origin/dev`.
2. **Confirm the business/mechanics split.** The `research` document carries it
   in two labelled sections. Re-verify with
   `git diff --stat bbd1c549..HEAD -- src/Pegasus.Web/Pages/Cases/Details.cshtml.cs src/Pegasus.Web/Pages/Cases/CaseMutationPageModel.cs src/Pegasus.Core/Workflow src/Pegasus.Core/Lifecycle src/Pegasus.Core/Cases/CaseDataOperations.cs`;
   if the upstream sync moved any of them, re-read and update `research` with the
   new SHA. The recorded SHA is `bbd1c549`. Only the business list is carried
   over.
3. **Confirm the gateway contract** from [[GWY-008]] (plan handle `DSK-03-08`).
   Five checks, each from a research finding:
   - the wire carries all five things `CaseLifecycleRules.ValidateMutation`
     requires (`src/Pegasus.Core/Lifecycle/CaseLifecycle.cs:414-426`) —
     `expectedVersion`, `operationKey` (≤ 100), **`reason` (required, ≤ 500)**,
     `editLeaseToken` (exactly 64), and an actor with `PerformCasework`;
   - a lease claim replayed with the same `operationKey` returns the **same
     token and expiry** (`ILeaseCaseForEdit`,
     `src/Pegasus.Core/Workflow/CaseWorkflowContracts.cs:323-336`);
   - a stale write returns a 409 problem carrying the **current version**
     (`CaseVersionConflictException.ActualVersion`, `:129`) (assumption
     `A-05-16`);
   - a lease conflict returns the **holder's display name**, resolved through
     `IDescribeCaseEditAuthorityHolder`
     (`src/Pegasus.Core/Workflow/CaseEditAuthority.cs:83-90`), never the subject
     id (assumption `A-05-17`);
   - the claim and renew responses carry `expiresAtUtc` (assumption `A-05-18`),
     and the completeness response carries **both** `Values` and `Evaluation`
     (`src/Pegasus.Core/Cases/CaseDataContracts.cs:105-107`) (assumption
     `A-05-19`).
   Lease tokens are 64 hex characters and must **never** be rendered to the
   operator.
4. **`CaseEditSession`** in `src/Pegasus.Desktop.Infrastructure`: claim on
   entering edit; renew on a timer at a fraction of the window derived from the
   returned `ExpiresAtUtc` — **never** from a hard-coded five minutes, because
   `EditLeaseDuration` lives in `src/Pegasus.Infrastructure/Persistence/EfCaseWorkflowStore.cs:20`
   and the desktop must not reference Infrastructure; release on exit; raise
   `LeaseLost` when a renew fails. The token is held **in memory only** — never
   written to disk, never to a log. Renewal is telemetry, not business history
   (`frd-01:88`), so it raises no operator-visible event.
5. **Edit state on `CaseWorkspaceViewModel`** from [[FEAT-003]] (plan handle
   `DSK-05-03`): an explicit dirty indicator; a deliberate `SaveCommand` — never
   an autosave; a navigation guard that warns before discarding unsaved work;
   and field-level validation that runs immediately against
   `CaseDataPolicy.Normalize` / `ValidateInspection`
   (`src/Pegasus.Core/Cases/CaseDataOperations.cs:121-190`) referenced directly
   from `Pegasus.Core`. **Bind inspection address and inspection mode as one
   control group** — `ValidateInspection` (`:163-190`) refuses one without the
   other, and a form that lets them diverge produces a refusal the operator
   cannot interpret.
6. **Keyboard and connectivity.** `Ctrl+S` → `SaveCommand` (proposal §14.9).
   Disable it while edit mode is not held or while the session is offline
   (see [[FND-047]] (plan handle `DSK-04-11`)) — **no silent queueing of saves.**
7. **The save request.** Send a `CaseMutationRequest`-shaped body with the
   `expectedVersion` the workspace loaded, the held `editLeaseToken`, **a reason
   collected through the `ReasonDialog` contract from [[DUI-009]] (plan handle
   `DSK-06-09`)** — Core requires one on every mutation
   (`CaseLifecycle.cs:420`), so a reason-free Save cannot succeed — and a fresh
   `operationKey` per user-initiated attempt, reused unchanged on a transport
   retry. On an uncertain outcome (timeout after send) **re-query the case
   rather than resending blind**.
8. **The three failure states, unambiguous and in settled vocabulary.** Version
   conflict (another member of staff changed the case); edit mode lost; edit
   mode held by a named holder. Reuse the sentences the web already ships —
   `src/Pegasus.Web/Pages/Cases/Details.cshtml.cs:196-197`, `:240-241`, `:280` —
   rather than writing new ones; **`lease` is a banned operator word**
   (`docs/design/README.md:412-420`), and none of those sentences uses it. On a
   stale-version refusal the desktop clears its held token too, matching
   `RequiresReacquisition` (`src/Pegasus.Web/Pages/Cases/CaseMutationPageModel.cs:313-314`):
   the operator reloads and re-enters edit mode rather than resubmitting. The
   full reload-compare-reapply pattern is [[FEAT-008]]'s (plan handle
   `DSK-05-08`); this slice must make the states unambiguous and never silently
   overwrite.
9. **Completeness confirmation** as an explicit command with the reason dialog
   from [[DUI-009]]. The precondition rules stay in Core —
   `CaseDataPolicy.ValidateCompleteness` (`CaseDataOperations.cs:105-119`) and
   `CaseCompletenessPolicy.Evaluate` (`:59-94`) are already Core-owned, so the
   expected `src/Pegasus.Core` diff is **zero**; if a rule is nonetheless found
   only in the page model, move it into `src/Pegasus.Core/Cases/` with a
   characterization test in `tests/Pegasus.Core.Tests` **first**. Render both
   halves of the result: the confirmation was accepted, **and** whether it
   satisfies the current policy.
10. **View-model tests** in `tests/Pegasus.Desktop.ViewModelTests` (project from
    [[FND-038]], plan handle `DSK-02-13`): dirty state on edit; navigation
    guard; save disabled without edit mode and while offline; operation-key
    reuse on transport retry; `LeaseLost` handling; 409 mapped to the conflict
    state with the current version captured; a stale-version refusal clearing
    the held token; the address/mode control group refusing a half-set pair.
11. **Two-user integration test** in `tests/Pegasus.IntegrationTests` (LocalDB),
    driving the gateway directly: user A claims edit mode and saves at version
    N; user B saves at version N and receives a 409 carrying version N+1; **A's
    write is intact**. Add contract tests for claim/renew/release replay,
    expiry, and release by a non-holder. Enable `Features:DesktopGateway`
    explicitly. Keep the new facts in exactly one shard
    (`scripts/Invoke-TestShard.ps1 -VerifyPartition`).
12. **Operator step — two-user UAT.** Run the scripted two-user scenario on the
    local Test/UAT stack with two real workstations or two sessions, confirming
    the second writer sees the conflict, can reload and compare, and that no
    value was lost. Capture the operator's sign-off text and date in `proof`.
13. **Documentation and PR.** Update
    `docs/desktop/01-inventory-and-parity/parity-matrix.md` row `PAR-08` (`:53`)
    for the **edit handlers**; add the edit and edit-mode section to
    `docs/frd/frd-13-desktop-operator-experience.md` and a `DSK` row to
    `docs/capabilities.md`. Run the simplification pass over the branch diff
    (`AGENTS.md` step 4), record it under a dated `## Simplification pass`
    heading here, then open the PR into `dev`.

## Verification

Evidence tiers from the body: **tier 4** (LocalDB persistence), **tier 5**
(Web/API/MCP caller), **tier 7** (Browser/accessibility), **tier 12**
(Integrated workflow).

| Command | Expected | Evidence captured |
| --- | --- | --- |
| `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --no-build --filter "Category!=Corpus&Category!=Browser"` | The new two-user conflict facts pass; `CaseWorkflowPersistenceTests`, `ConcurrencyTokenPersistenceTests`, `CaseEditModeWebTests` and `CaseDetailsWebTests` stay green | Test summary — **tier 4 evidence**, against a real LocalDB with action-history atomicity |
| `dotnet test ./tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj --configuration Release --no-build` | Claim / renew / release replay, expiry, non-holder release and 409-with-current-version facts pass | Test summary — **tier 5 evidence** |
| `dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release --no-build` | Dirty state, navigation guard, operation-key, lease-lost and address/mode facts pass | Test summary |
| `dotnet build ./Pegasus.slnx --configuration Release --no-restore` | Succeeds under `TreatWarningsAsErrors=true` with no `WUI*` suppression | Build log tail |
| `pwsh ./tests/Pegasus.Desktop.UITests/ui-tests.ps1 -Script case-edit` | Edit, save and conflict-message assertions pass with no sleeps | Results JSON + screenshots — **tier 7 evidence** |
| `pwsh ./scripts/Invoke-TestShard.ps1 -VerifyPartition` | The new integration facts land in exactly one shard | Command output |
| Two-user UAT on the Test/UAT stack | The second writer sees the conflict, reloads, compares, and no value is lost | Named operator sign-off with date in `proof` — **tier 12 evidence** |

## Risks / open questions

- **Risk: Save is built without a reason.** `CaseLifecycleRules.ValidateMutation`
  requires a non-empty `Reason` ≤ 500 characters on **every** mutation
  (`src/Pegasus.Core/Lifecycle/CaseLifecycle.cs:420`), which the ticket body's
  "deliberate Save" does not say. *Mitigation:* step 3's first check and step 7's
  explicit `ReasonDialog`; a view-model fact asserts Save is refused with an
  empty reason.
- **Risk: the renew timer hard-codes five minutes.** `EditLeaseDuration` is an
  Infrastructure constant (`src/Pegasus.Infrastructure/Persistence/EfCaseWorkflowStore.cs:20`)
  that the desktop must not reference and that can change without the desktop
  knowing. *Mitigation:* step 4 drives the timer from `ExpiresAtUtc`, and
  step 3's fifth check confirms the response carries it.
- **Risk: a stale-version refusal leaves the client believing it still holds
  edit mode.** `RequiresReacquisition` unions version conflict with lease loss
  (`CaseMutationPageModel.cs:313-314`) precisely because the rejected editor must
  reload and reacquire. *Mitigation:* step 8, with a view-model fact.
- **Risk: the lease token leaks.** 64 hex characters compared against a retained
  hash in fixed time (`CaseEditAuthority.cs:34-37`). *Mitigation:* step 4 keeps
  it in memory only; the acceptance criteria ban it from the UI **and** logs;
  the reviewer checks the log redaction from [[FND-032]] (plan handle
  `DSK-02-07`).
- **Risk: the word `lease` reaches the screen.** It is banned
  (`docs/design/README.md:412-420`) and nothing in CI catches it
  (`:417-420`). *Mitigation:* step 8 reuses the web's existing "Edit mode …"
  sentences verbatim, and the reviewer is the gate.
- **Risk: address and mode diverge in the form.** `ValidateInspection`
  (`CaseDataOperations.cs:163-190`) refuses one without the other and refuses the
  `ImageBasedAssessment` sentinel as a physical address. *Mitigation:* step 5
  binds them as one control group, with a view-model fact.
- **Risk: the completeness result is reported as a plain success.** A
  confirmation can be accepted while `SatisfiesPolicy` is false
  (`CaseDataContracts.cs:100-107`). *Mitigation:* step 3's fifth check and
  step 9's two-part rendering.
- **Scope boundary: the conflict-and-recovery pattern.** Reload / compare /
  reapply, the replayed-outcome presentation and the retry rule belong to
  [[FEAT-008]] (plan handle `DSK-05-08`). Inventing a conflict UX here would
  create the second pattern the design authority forbids.
- **Scope boundary, named board ticket: upstream `CASE-021` (board
  [[CASE-001]])** — "Refuse Review for a case with no images" — is a **gateway**
  rule that must be true before `PAR-08` reaches parity. It is an imported
  production defect on this board, not an upstream-only item; the join table in
  the `HZN-001` group document `board-conventions.md` records the mapping.
  Raise it there rather than compensating in the desktop.
- **Scope boundary: `CaseMutationPageModel` retirement** is [[FEAT-024]] (plan
  handle `DSK-05-24`), and the nineteen workflow/closure/task commands are
  [[FEAT-006]] (plan handle `DSK-05-06`).
- **Not an open question: the operator decisions are settled.** D-002, D-003 and
  D-004 do not touch this ticket, which performs no Azure write.

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over
this branch's own diff before the PR, recorded here under a dated heading._
