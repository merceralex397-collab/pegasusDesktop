# Plan — FND-046: role-aware shell driven by `StaffAccessRight`

**Diff estimate: ~9 files, ~500 lines.**

Derived from the files document, not asserted: 4 new files
(`ICurrentActor.cs` ~25, `CurrentActor.cs` ~90,
`ShellRoleVisibilityTests.cs` ~170, `AdministrationAuthorizationTests.cs`
~110) and 5 edits (the session client ~15, `ShellViewModel.cs` ~55,
`ShellPage.xaml.cs` ~20, `NavigationService.cs` ~30, `App.xaml.cs` ~4).
`docs/engineering.md:201-203` § Plan sizing requires the estimate first.

## Approach

Compute rail-item and command visibility in the shell view model by calling
`StaffAuthorization.IsAuthorized(actor, right)` on a real `ActionActor` built
once per session by a new `ICurrentActor` service, and remove unauthorized
items from the collection the `NavigationView` binds to. The alternative
considered and rejected was to have the gateway return a "capabilities" list
the shell renders blindly — a `~GET /api/v1/session/capabilities` shape. It
was rejected for three reasons: it duplicates the matrix in a wire contract
that then needs its own versioning and OpenAPI snapshot; it adds a request to
the startup path for ten users; and it hides the fact that the *client* copy
is only an affordance, making it easier for a later reader to mistake it for
the boundary. Calling Core directly keeps exactly one owner of the matrix
(`src/Pegasus.Core/Identity/StaffAuthorization.cs:29-58`) and makes the
server-side test in step 10 obviously necessary rather than redundant.

## Governing docs

### Linked `refs`

| Ref | Requirement | Meets |
| --- | --- | --- |
| `docs/frd/frd-12-operator-experience.md` § Operator experience | Operator-visible navigation and its states are FRD-12's today; FRD-12 owns the current web rail behaviour this ticket reproduces natively | **Meets** — Steps 5–7 reproduce the web's absent-not-disabled Administration item (`src/Pegasus.Web/Pages/Shared/_Layout.cshtml:93-98`) in the native shell without changing which operator sees what. No FRD-12 text is modified. |

### `docs_todo: true`

This ticket carries `docs_todo: true`, so no conversion FRD governs it yet.

> **New FRD** — FRD-13 "Desktop operator experience", authored by [[FND-008]]
> (plan handle `DSK-00-08`).
> This plan is written to the role-aware navigation behaviour as recorded in
> `docs/desktop/06-ui-design/screen-specs.md:58-63` and the `DSK-06-04` row at
> `docs/desktop/06-ui-design/README.md:228`; if FRD-13 lands differently this
> plan is revised before implementation.

No ADR is authored here. The decision this plan depends on — that the gateway
is the authorization boundary — is proposal § 8.3 and is recorded in
**ADR-0103** (gateway, never direct database access), authored by
[[FND-005]] (plan handle `DSK-00-05`).

### Programme-level authorities that bind today

| Authority | Requirement | Met by |
| --- | --- | --- |
| Proposal § 8.3 Authorization | "the gateway must independently enforce authorization for every data query and command"; hiding is a usability affordance only | Step 10 (server-side refusal proved through the real route); step 8's guard is explicitly labelled non-security |
| Proposal § 14.2 Main shell | Rail order and admin-only Administration | Steps 5–6 |
| `docs/desktop/06-ui-design/screen-specs.md:27-30, :58-63` | Absent, not disabled | Step 5 (`MenuItems` removal, never `IsEnabled=false`) |
| `docs/desktop/06-ui-design/screen-specs.md:31-39, :80-82` | AutomationId convention and the shell id list | Step 7 |
| `docs/engineering.md` § Engineering invariants (one Core owner) | A business rule has exactly one implementation | Steps 2, 5 — the desktop calls `StaffAuthorization.IsAuthorized`; no matrix is copied |
| `docs/engineering.md:74, :78` (tiers 2 and 5) | Both tiers owed; neither substitutes for the other | Steps 9 and 10 |
| **L-01** (`docs/desktop/README.md` § Locked decisions) | The gateway is `Pegasus.Web` evolved in place; enforcement is the `/api/v1` endpoint filter, not a new policy engine | Step 10 targets the existing filter from [[GWY-021]] (plan handle `DSK-04-04`) / [[GWY-003]] (plan handle `DSK-03-03`) |
| `AGENTS.md` § Repository task workflow step 4 | Simplification pass over the branch's own diff, recorded under a dated heading in the plan | Step 12, and the `## Simplification pass` heading below |
| `AGENTS.md` § Repository task workflow step 5 | Review by an agent that did not implement | Routing → Reviewer |

## Routing

Copied from the ticket body's `## Routing` block; required in the plan
document by `docs/desktop/00-governance-and-workflow/README.md:264-276`
§ Ticket template, row 7.

- **Subagent**: `winui-dev` — `.codex/agents/winui-dev.toml`
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `winui-design`
  (`.codex/skills/winui-design/SKILL.md`, `microsoft/win-dev-skills` v0.5.0
  `f1028dd5`) → `winui-code-review`
  (`.codex/skills/winui-code-review/SKILL.md`) at review time
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`,
  `set_ticket_doc`, `append_scratch`, `move_item`); Microsoft Learn
  (`microsoft_docs_search`) for `NavigationView` item visibility semantics
- **Kanmer pipeline** for profile `feature`: `kanmer-research` →
  `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` →
  `kanmer-closeout` (call `get_doc_gates <id>` before every move; a move
  crosses at most one gated boundary)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement
  (`AGENTS.md` § Repository task workflow step 5)

All eight routing assets were confirmed present on 2026-08-24:
`ls .codex/agents/`, `ls .codex/skills/`, `ls .agents/skills/project/pegasus-desktop/`.

## Steps

These refine the ticket body's implementation steps 1–12 in the same order and
with the same ownership; they add the *how*, and change nothing the body
decided.

1. **Orientation.** Read
   `docs/desktop/04-auth-session-update-and-startup/README.md` § 5 row
   `DSK-04-10` and `docs/desktop/06-ui-design/screen-specs.md:41-82`. Call
   `get_doc_gates FND-046`, then `take_ticket` with the real branch and
   worktree (`AGENTS.md` § Repository task workflow steps 1–2: branch
   `task/<slug>` from `origin/dev`, worktree under
   `../pegasus-worktrees/<slug>`). Load `pegasus-desktop`, then `winui-design`.
2. **Read the Core boundary end to end.** `src/Pegasus.Core/Identity/StaffAuthorization.cs`
   (78 lines) and `src/Pegasus.Core/Identity/IdentityContracts.cs` (147 lines).
   Confirm three things before writing code: the Administrator family is the
   single switch arm at `:44-52`; the default arm at `:56` is `false`; and
   `IsAuthorized` throws `ArgumentNullException` on a null actor at `:31`, so
   "no session" must be handled by the caller. **Do not copy the matrix.** A
   second implementation of a business rule is a defect under
   `docs/engineering.md` § Engineering invariants.
3. **Build the actor from the session claims, exactly as the gateway does.**
   In `src/Pegasus.Desktop.Infrastructure/Session/`, add `CurrentActor` which,
   on each session transition, reads the subject id and the role-name claims
   from the current access token and calls
   `StaffActorFactory.TryCreate(subjectId, roleNames, out var actor)`. Mirror
   `src/Pegasus.Web/Pages/StaffPageModel.cs:11-15`: subject from
   `ClaimTypes.NameIdentifier`, roles from **all** `ClaimTypes.Role` claims.
   A `false` return is a **failed session**, not an anonymous one: raise the
   session-failure path from [[FND-043]] (plan handle `DSK-04-07`) and return
   to sign-in. Log one line at the diagnostics writer from [[FND-036]] (plan
   handle `DSK-02-11`) carrying the correlation id, the subject id and the
   *count* of unrecognised role names — never the token, never the claim values
   verbatim.
4. **Expose it as `ICurrentActor`, registered in the host.** One registration
   in `src/Pegasus.Desktop/App.xaml.cs` (host and DI from [[FND-032]], plan
   handle `DSK-02-07`), lifetime singleton, with a `Changed` event raised on
   sign-in, refresh and sign-out. On sign-out the held actor is set to `null`
   before the event is raised, so no subscriber can read a stale actor.
   **Never cache rights across a sign-out.**
5. **Filter the rail in the shell view model.** In [[FND-033]]'s (plan handle
   `DSK-02-08`) `ShellViewModel`, build the rail collection in the settled
   order — Dashboard → Inbox → Upload → Queues → Cases → Operations →
   Administration → user — then remove items whose right fails
   `StaffAuthorization.IsAuthorized(actor, right)`. Bind Administration to
   `StaffAccessRight.ManageStaffAccounts`
   (`StaffAuthorization.cs:44-52`, the Administrator-only family).
   **Remove from `NavigationView.MenuItems`; do not set `IsEnabled=false` and
   do not bind `Visibility`** — the screen spec requires absence
   (`screen-specs.md:27-30, :58-63`) and a collapsed element is still in the
   automation tree, which fails step 11's assertion.
6. **Apply the same rule to admin-only commands.** Sweep the title-bar user
   menu and any command-bar buttons the shell owns and give each the right it
   requires, resolved through the same call. Rebuild the collection from the
   ordered source on every `ICurrentActor.Changed`; never re-sort after a
   removal, or a role change reorders the rail.
7. **Preserve the AutomationIds.** Each present rail item keeps
   `AutomationProperties.AutomationId = Shell.Rail.<Route>` and the user group
   keeps `Shell.Title.User` (`screen-specs.md:80-82`). A hidden item has **no**
   AutomationId in the tree — that absence is precisely what step 11 asserts.
8. **Add the navigation guard.** In `NavigationService`, before navigating to
   any route by a means other than a rail click — deep link, restored window
   state, keyboard access key for a removed item — re-check
   `StaffAuthorization.IsAuthorized` and, on failure, route to the dashboard
   and surface the shell's standard `InfoBar` message. Add a code comment
   stating in one line that this is a usability guard and the security boundary
   is the `/api/v1` endpoint filter; the same sentence goes in the PR
   description.
9. **View-model tests** in `tests/Pegasus.Desktop.ViewModelTests` (project from
   [[FND-038]], plan handle `DSK-02-13`), using the fake credential store and
   clock that project supplies. Four independently failing cases:
   (a) an `Administrator` actor sees Administration; (b) an `Engineer` actor
   does not; (c) a `User` actor does not; (d) the rail order for a
   non-administrator equals the settled order with Administration elided and
   nothing else moved. Plus (e): a `TryCreate` failure — feed a role name that
   is not exactly one of `Administrator`/`Engineer`/`User`
   (`StaffActorFactory.cs:23-27` is `ignoreCase: false`) — produces the session
   failure state and **not** a shell with no rail items.
10. **The server-side half, as a real tier-5 test** in
    `tests/Pegasus.IntegrationTests`: authenticate as a non-administrator,
    obtain a genuine bearer token, call an Administrator-only `/api/v1`
    endpoint directly, and assert the response problem type is
    `urn:pegasus:problem:not-authorized`. This proves the gateway refuses the
    call whether or not the client hid the command. Place the class in exactly
    one shard — `scripts/Invoke-TestShard.ps1:8-10` assigns whole classes
    together — and keep the partition check green (see Verification).
    If no Administrator-only `/api/v1` route exists yet (assumption
    A-04-10-4), target the endpoint filter through a minimal test-only route
    registered in the integration-test host and record that substitution in the
    post-implementation report.
11. **Run it.** `.\BuildAndRun.ps1` from
    `.codex/skills/winui-dev-workflow/BuildAndRun.ps1`; sign in once as an
    Administrator and once as a non-administrator against the local stack
    (`pwsh ./scripts/Invoke-LocalDevelopment.ps1 -Action Start`), and capture
    `winapp ui inspect -a <pid> --interactive` plus a `winapp ui screenshot`
    for each. The non-administrator tree must contain **no**
    `Shell.Rail.Administration` node.
12. **Simplification pass** over this branch's diff (four lenses), recorded
    under a dated `## Simplification pass` heading in this document, then open
    the PR into `dev`.

## Verification

Evidence tier from the body: **Tier 2 — Core/domain** (the client-side right
computation over all three roles) **and Tier 5 — Web/API/MCP caller** (the
gateway refusal through the actual route with a real bearer token). The body is
explicit that the client test alone does not satisfy the ticket.

| Command | Expected | Becomes evidence as |
| --- | --- | --- |
| `dotnet test tests/Pegasus.Desktop.ViewModelTests` | `Passed!`, with cases (a)–(e) from step 9 green | TRX under `artifacts/test-results/`, summary into `proof` (test-output) |
| `dotnet test tests/Pegasus.IntegrationTests --filter FullyQualifiedName~Authorization` | `Passed!`; the non-administrator call returns problem type `urn:pegasus:problem:not-authorized` | TRX plus the asserted problem body, into `proof` (test-output) |
| `pwsh ./scripts/Invoke-TestShard.ps1 -VerifyPartition -ArtifactRoot ./artifacts/test-shards -ShardCount 3` | exit code `0`; the new class appears in exactly one shard | console transcript into `proof` (command-log) |
| `winapp ui inspect -a <pid> --interactive` signed in as a non-administrator | no `Shell.Rail.Administration` node in the tree | the inspect JSON plus the paired screenshots, into `proof` (visual) |

**Note on the third row.** The ticket body's Verification line reads
`pwsh ./scripts/Invoke-TestShard.ps1 -VerifyPartition`, which cannot run:
`-ShardCount` is declared `[Parameter(Mandatory)]` with no `ParameterSetName`
(`scripts/Invoke-TestShard.ps1:35-36`), so it is mandatory in the `Verify` set
too, and a bare invocation prompts or fails. The form above is the script's own
worked example (`:20`). Repairing the body is owned by [[FND-052]], which names
FND-046 among the four `-VerifyPartition` call sites to fix; nothing here
changes a ticket body.

## Risks / open questions

- **Risk: the shell exposes rail items as static XAML, not a bound
  collection.** Mitigation: assumption A-04-10-3 in the research document; if
  it holds false, rebuild `NavigationView.MenuItems` in code-behind from the
  same ordered view-model source. The assertion is unchanged.
  Answered by reading [[FND-033]]'s implementation when it lands.
- **Risk: a role added server-side without a desktop release denies the whole
  session.** `StaffActorFactory.TryCreate` returns `false` for **any**
  unrecognised role name (`:23-27`), so a new `StaffRole` value shipped to the
  gateway first logs a failed session for every user who holds it — it does not
  degrade gracefully. Recorded here as the body's Traps section requires.
  Mitigation is procedural, not code: a `StaffRole` addition is a Core change
  that must ship to the desktop in the same release train, and the minimum
  client version gate from [[GWY-023]] (plan handle `DSK-04-06`) is the lever
  that enforces it. **This is a scope boundary owned by [[GWY-023]] and
  [[FND-045]] (plan handle `DSK-04-09`), not an open question.**
- **Risk: no Administrator-only `/api/v1` route exists when this ticket runs.**
  Owned by [[GWY-015]] (plan handle `DSK-03-15`) and [[GWY-003]]. Step 10
  carries the recorded fallback; a scope boundary, not an open question.
- **Risk: a reviewer reads the step-8 guard as a security control.** Mitigation:
  the one-line comment required by step 8 plus the same sentence in the PR
  description; `winui-code-review` at review time is asked to check it.
- **Question — who answers:** whether `Shell.Rail.Administration`'s absence
  becomes a standing case in `tests/Pegasus.Desktop.UITests/ui-tests.ps1`.
  Answered by [[TEST-006]] (plan handle `DSK-08-06`), which owns that harness,
  or by [[DUI-015]] (plan handle `DSK-06-15`)'s AutomationId coverage audit.
  This ticket captures the evidence manually and does not create the harness.

No `open-questions` document is created: the ticket body does not instruct one,
and every unknown above is owned by a named sibling ticket, which
`docs/desktop/00-governance-and-workflow/README.md` § 3 makes a scope boundary
rather than a question.

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass
over this branch's own diff before the PR, recorded here under a dated
heading._
