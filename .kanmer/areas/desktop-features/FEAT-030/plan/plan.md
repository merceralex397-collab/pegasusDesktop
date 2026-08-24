# Plan — FEAT-030: DSK-07-04 Desktop Operations screen

**Diff estimate: ~8 files, ~1,230 lines.**

Derived from the `files` document, not asserted. `src/Pegasus.Desktop`:
`OperationsViewModel` ~330 (five-state load model, two tables, the health row
collection, three commands, the `ObtainedAtUtc` discipline) and
`OperationsPage.xaml` ~280 plus ~40 of code-behind — the page is two
[[DUI-007]] data tables and a health list, not a bespoke grid.
`src/Pegasus.Desktop.Infrastructure`: ~120 for the typed calls into the
generated client. `tests/Pegasus.Desktop.ViewModelTests`: ~300 (the five fact
groups in the body's step 10 plus the disconnected and state-vocabulary facts).
`tests/Pegasus.Desktop.UITests`: ~120 for the `-Script operations` batch.
Documentation: ~40 lines of FRD-13 § Operations and one row edit in
`parity-matrix.md:72`.

## Approach

Build one view model over the **contracts** [[FEAT-027]] (plan handle
`DSK-07-01`) and [[FEAT-028]] (plan handle `DSK-07-02`) publish, and make its
whole design turn on a single rule copied from the web page: the obtained-at
timestamp is written **only** in the success branch, so a failed load keeps the
previous rows and labels them with their earlier time rather than blanking the
table. `Pages/Operations/Index.cshtml.cs` states that rule in its own remark
(`:41-45`) and enforces it by assigning at `:67`, after the await; reproducing
the *placement* rather than merely copying the field is what satisfies FRD-12
`:95-99`, the state contract at `docs/design/README.md:764-772`, and the
ticket's first acceptance criterion at once. The alternative considered and
rejected was the conventional WinUI shape — clear the collection, show a
progress ring, repopulate on success — which produces exactly the defect the
Guardrails name: "a blank screen after a failed load is a lie", and on this
screen a blank table reads as "nothing failed". The second decision is that
**retry uses a plain confirmation and revoke uses the [[DUI-009]] `ReasonDialog`**.
`docs/desktop/06-ui-design/screen-specs.md:392` says "Retry (reasoned)", but
`RetryExternalWorkCommand` (`src/Pegasus.Core/Operations/RequestOperations.cs:157-161`)
has no `Reason` member and the web handler collects none
(`Index.cshtml.cs:72-76`) while revoke does (`:117`). Collecting a reason the
gateway would discard is worse than not asking, so the trivial default is taken
and recorded here rather than raised as a question; a reason on retry would be a
Core command change and a different ticket.

## Governing docs

The ticket carries `refs: ["docs/frd/frd-12-operator-experience.md"]` and
`docs_todo: true` — confirmed in `get_doc_gates FEAT-030`, which reports
`governing-doc` **satisfied** at `leave-backlog`.

**Meets — `docs/frd/frd-12-operator-experience.md`.** `:95-99` requires that
"Every count and query exposes its last successful update time and current
refresh state" and that "`0`, loading, current, stale-with-last-good-time,
partial, unavailable, and failed are distinct outcomes. A refresh never replaces
a last-good value with a false zero" — steps 4 and 9 are that requirement in
code, and step 10's first two facts are its test. `:101-103` requires that
"Manual refresh reruns the same exact filtered query; it does not change policy
or create a business transition" — the refresh command re-issues the same reads
and mutates nothing. `:113` forbids inferring state from colour alone — step 7
and the step-12 scan. No FRD-12 text is modified.

> **New FRD** — `docs/frd/frd-13-desktop-operator-experience.md`, skeleton
> authored by [[FND-008]] (plan handle `DSK-00-08`), with its sections adopted
> from the screen specs by [[DUI-013]] (plan handle `DSK-06-13`). This ticket
> writes the § Operations section into that skeleton; [[FEAT-020]] (plan handle
> `DSK-05-20`) adds the retry and revoke command behaviour as a sub-heading
> inside it. If the skeleton has not landed, this ticket writes the section
> content into its `plan` and hands it to [[FND-008]] rather than creating the
> file itself.

> **New ADR** — ADR-0106 (Graph intake worker stays central: unattended
> execution, protected credentials), authored by [[FND-005]] (plan handle
> `DSK-00-05`).
> This plan is written to the decision as recorded in
> `docs/desktop/00-governance-and-workflow/README.md` § 3; if the ADR lands
> differently this plan is revised before implementation. Cited for step 7's
> rule that the screen reports on the central worker and does not drive it.

> **New ADR** — ADR-0107 (Box and DVLA/DVSA credentials stay behind the
> gateway; no long-lived provider secret in the package), authored by
> [[FND-005]]. Same condition. Cited for the health rows showing "a state and a
> last-good time, nothing more".

> **New ADR** — ADR-0104 (online-required, bounded local cache), authored by
> [[FND-005]]. Same condition. Cited for step 9: the disconnected state shows
> the last obtained values labelled with their time — a bounded in-memory
> last-good, not an offline replica.

**Ownership reconciliation.** `tests/Pegasus.Desktop.ViewModelTests` has two
claimant tickets: this body's step 10 names [[TEST-004]] (plan handle
`DSK-08-04`, "Scaffold `tests/Pegasus.Desktop.ViewModelTests`") and
[[FEAT-001]]'s `files` document names [[FND-038]] (plan handle `DSK-02-13`,
"Create `tests/Pegasus.Desktop.ViewModelTests` with fakes…"). This ticket
creates neither; see [[TEST-004]]'s plan for the ownership reconciliation and
write into whichever landed.

`refs` names one FRD and no ADR, so the programme-level authorities that bind
today are tabulated for `kanmer-review` to check against the diff:

| Authority | Requirement | Met by |
| --- | --- | --- |
| Proposal § 13.10 | Integration health and failed-work review are parity capabilities | Steps 5 and 7 |
| Proposal § 16.1 | Explicit operation model: not started / running / succeeded / failed / cancelled | Step 4 |
| Proposal § 16.2 | The client shows when data is cached and when it was obtained | Steps 4 and 9 |
| Proposal § 14.8 | Notifications and errors carry one operator sentence and a reference | Step 8 |
| Proposal § 27 item 2 | An operator never needs the web app open to complete a task | The screen as a whole |
| L-01 | All data arrives through `/api/v1`, never a direct database or provider call | Step 2; the Guardrails' scope boundary |
| L-04 | Subagent, skills and MCP named on the ticket | § Routing below |
| ADR-0106 | The screen reports on the central Graph worker; it does not drive it | Step 7 |
| ADR-0107 | No secret and no raw provider payload reaches the client | Steps 5 and 7 |
| `docs/design/README.md:170`, `:412-420` | Operator copy carries no queue, lease, projection or intake terminology; CI does not enforce it — the reviewer is the only gate | Step 5's copy review; every string drawn from `OperatorLabels` or the eight approved web sentences |
| `docs/design/README.md:764-772` | The complete UI state contract for queries and mutations | Steps 4, 8, 9 |
| `docs/desktop/06-ui-design/screen-specs.md:390-398` | The table columns, the four AutomationIds, and "absent when not composed" for health rows | Steps 5 and 7 |
| `docs/desktop/06-ui-design/screen-specs.md:31-39` | AutomationId convention `<Screen>.<Region>.<Element>[.<Key>]`, 100% coverage | Step 5 |
| `docs/desktop/06-ui-design/screen-specs.md:417-427` | The desktop-specific states, including `disconnected (saves disabled, content visible)`, and the empty-state rule | Step 9 |
| `docs/desktop/06-ui-design/keyboard-and-accessibility.md:82`, `:88`, `:96` | No information by colour alone; forced-colours mapping; permanent consequences visible without hover | Steps 7 and 12 |
| `docs/desktop/03-gateway-api-and-data/README.md` § 3 "Contracts" | The desktop never hand-writes DTOs | Step 2 |
| `docs/desktop/03-gateway-api-and-data/README.md` § 3 "Retry" | Only idempotent `GET`s are retried automatically; commands never are | Step 6 |
| `docs/engineering.md` § Plan sizing | Diff estimate first; facts split from assumptions | This heading; `research` § Facts / Assumptions |
| `AGENTS.md` § Simplicity rails | One view model per screen; one operator vocabulary | Step 3 and the § Approach copy rule |
| `AGENTS.md` § Repository task workflow step 4 | Simplification pass over this branch's own diff before the PR | Step 12 |

## Routing

Copied from the ticket body's `## Routing` block.

- **Subagent**: `winui-dev` — `.codex/agents/winui-dev.toml`; verification by
  `pegasus-ui-verifier` — `.codex/agents/pegasus-ui-verifier.toml`
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `winui-design`
  (`.codex/skills/winui-design/SKILL.md`) → `winui-dev-workflow`
  (`.codex/skills/winui-dev-workflow/SKILL.md`) → `winui-ui-testing`
  (`.codex/skills/winui-ui-testing/SKILL.md`) → `winui-code-review`
  (`.codex/skills/winui-code-review/SKILL.md`) at review
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`,
  `set_ticket_doc`, `append_scratch`, `move_item`); Microsoft Learn
  (`microsoft_docs_search` for WinUI `ListView` virtualization and `InfoBar`
  semantics)
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` →
  `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (call
  `get_doc_gates <id>` before every move; a move crosses at most one gated
  boundary)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement
  (`AGENTS.md` § Repository task workflow step 5)

## Steps

These refine the ticket body's twelve implementation steps in the same order and
with the same ownership.

1. **Orient and take.** Read the plan row `DSK-07-04`
   (`docs/desktop/07-integrations/README.md` § 5), the Operations screen spec
   (`docs/desktop/06-ui-design/screen-specs.md:390-398`), the cross-cutting
   state contract (`:417-427`), and `docs/design/README.md` — specifically
   `:170` and `:412-420`, the operator-copy authority. **This screen is the most
   exposed on the board to the banned-word rule, because its subject matter is
   queue mechanics.** Call `get_doc_gates FEAT-030`, then `take_ticket` with
   branch `task/dsk-07-04-operations-screen` and a worktree cut from
   `origin/dev`.
2. **Confirm the contracts and regenerate the client.** Read
   `src/Pegasus.Contracts` for the types [[FEAT-027]] and [[FEAT-028]]
   published, and [[GWY-013]] (plan handle `DSK-03-13`)'s
   `GET /api/v1/operations` for the upload-links half. Run
   `pwsh ./eng/api/Generate-ApiClient.ps1` ([[GWY-005]], plan handle
   `DSK-03-05`) **twice**; expected: `git diff --exit-code` clean after the
   second. If a field the screen needs is absent, stop and raise it on the
   owning endpoint ticket — the desktop never hand-writes DTOs
   (`docs/desktop/03-gateway-api-and-data/README.md` § 3 Contracts).
3. **Add `OperationsViewModel` to `src/Pegasus.Desktop`** *(project created by
   [[FND-030]], plan handle `DSK-02-05`)* using `ObservableObject` and
   `[RelayCommand]` per `winui-code-review`'s MVVM checklist — no
   `SolidColorBrush`, `Visibility` or other UI type; the architecture test from
   [[FND-037]] (plan handle `DSK-02-12`) fails on one, as it does on any
   `src/Pegasus.Infrastructure` reference. **This ticket owns the type.** If
   [[FEAT-020]] (plan handle `DSK-05-20`) landed first it created it under
   exactly these members — extend it in place; a second view model for this
   screen is a stop condition (the Guardrails).
4. **Model the load state explicitly** per proposal § 16.1: `NotStarted`,
   `Running`, `Succeeded`, `Failed`, `Cancelled`. Set `ObtainedAtUtc` **only**
   in the success branch, reproducing the placement at `Index.cshtml.cs:67`
   (after the await), not merely the field at `:46`. On failure the view model
   keeps the previous rows, marks them previously-obtained with their earlier
   time, and raises the failure sentence — it does **not** clear the
   collections. The initial state is `NotStarted`, never a rendered empty table:
   the web page's own pre-load value is an empty projection with
   `LimitReached: false` (`:47-49`), and an empty table on a desktop screen
   reads as "nothing failed".
5. **Build `OperationsPage.xaml` from the screen spec.** An external-work table
   with columns kind, case, last failure, attempts, next action
   (`screen-specs.md:391-392`) carrying `Operations.External.Table` and a retry
   command carrying `Operations.External.Retry`; an upload-links table with
   `Operations.Links.Revoke`; one health row per dependency with
   `Operations.Health.<Dependency>`. Use the [[DUI-007]] (plan handle
   `DSK-06-07`) data-table pattern, not a bespoke grid. **Review every string
   against `docs/design/README.md:170` before committing**: draw each from
   `OperatorLabels` (`src/Pegasus.Web/Presentation/OperatorLabels.cs`, moving to
   `Pegasus.Contracts` under [[GWY-016]], plan handle `DSK-03-16`) or from the
   eight approved sentences the web handlers already produce (`:92-94`,
   `:100-102`, `:104-106`, `:128`, `:147`, `:157`, `:165`). Write none fresh.
   Render times through `OperatorLabels.OfficeTime` / `OfficeDate`
   (`:412`, `:426`, `Europe/London` at `:446`) rather than a second helper.
6. **Bind enablement to the gateway alone.** Retry is enabled iff the row's
   `canRetry` is true; revoke iff `canRevoke` is true. Both are **fields** of
   the Core projection (`RequestOperations.cs:51-52`) guarded by invariants at
   `:142` and `:149`. Never infer eligibility from `AttemptCount` — a client
   that guesses produces a refused command the operator cannot explain. Retry
   sends one `operationKey`; **revoke sends two** — one for the case edit lease
   and one for the revoke itself (`Index.cshtml.cs:132`, `:153`) — and a view
   model that generates one and reuses it will produce a refusal. Show the
   already-leased case honestly from `CaseEditLeaseState` /
   `CaseEditLeaseExpiresAtUtc` (`:54-55`) with the web page's own sentence
   (`:147`). Preserve the typed reason on refusal, as `PreserveReason`
   (`:218-235`) does. Commands are never retried automatically
   (`docs/desktop/03-gateway-api-and-data/README.md` § 3 Retry).
7. **Show failure and freshness as named figures, never as a rollup.** Render
   the two poison figures from [[FEAT-027]]'s contract as their own values, and
   the mailbox freshness state (`current` / `stale` / `unavailable`) with its
   last successful cycle time. Never collapse `unavailable` into "no failures" —
   the trap row "Poison-queue visibility lost behind a friendly status"
   (`docs/desktop/07-integrations/README.md` § 7) is exactly this. Mirror
   `StateLabel`'s refusal: the web page's default arm **throws**
   (`Index.cshtml.cs:185`), so the view model must surface an unrecognised state
   rather than degrading it to "Unknown". Every meaning-bearing state carries
   text as well as colour (`keyboard-and-accessibility.md:82`). Render the
   Box, DVLA/DVSA, update-feed and minimum-client-version rows **only when
   composed**: they come from [[PLAT-015]] (plan handle `DSK-10-15`)'s
   `GET /api/v1/admin/health`, which `endpoint-map.md:37` marks
   `ManageWorkflowConfiguration` and phase 8, so at phase 5 they are absent
   under the screen spec's own rule (`screen-specs.md:394-396`), not blank.
8. **Render refusals through the shared problem presentation** from
   [[DUI-010]] (plan handle `DSK-06-10`): one operator sentence plus a copyable
   Reference carrying the correlation id. Reasoned commands — revoke only — use
   the `ReasonDialog` from [[DUI-009]] (plan handle `DSK-06-09`). Retry uses a
   plain confirmation: `RetryExternalWorkCommand` has no `Reason` member, so
   collecting one would discard it (see § Approach). Distinguish replay from
   first effect in the operator's words, as `result.IsReplay` does at
   `Index.cshtml.cs:92-94`.
9. **Handle disconnected honestly.** `disconnected (saves disabled, content
   visible)` is a named contract state (`screen-specs.md:423`). When the gateway
   is unreachable the screen says so, keeps the last obtained values labelled
   with their time, disables the commands, and offers manual refresh. A blank
   table that implies "nothing failed" is a defect; so is a spinner that never
   resolves. The empty-state rule (`:425-427`) covers only three cases — an
   absent section, a legitimate `0`, and a search that returned nothing — and a
   failed load is none of them.
10. **View-model tests** in `tests/Pegasus.Desktop.ViewModelTests` with a fake
    API client: success sets `ObtainedAtUtc`; failure **preserves prior rows**
    and does not set it; retry is disabled when `canRetry` is false; revoke is
    disabled when `canRevoke` is false; a refused retry surfaces the problem
    sentence and its reference; a refused revoke preserves the typed reason;
    cancellation leaves the state `Cancelled`; the disconnected state keeps rows
    and disables commands; and an unrecognised operation state surfaces rather
    than rendering "Unknown".
11. **Build, launch and script the UI walk.** Run `.\BuildAndRun.ps1` from the
    `winui-dev-workflow` skill in async mode and capture the PID, then write and
    run a `winapp ui` batch per `winui-ui-testing` covering: both tables render,
    retry disabled then enabled, the reason dialog on revoke, keyboard-only
    traversal of both tables, and a screenshot of the disconnected state.
    Tier 7 obliges a **real run against the gateway**, not a mocked screenshot.
12. **Accessibility scan, FRD section, matrix row, simplification pass, PR.**
    Run the [[DUI-015]] (plan handle `DSK-06-15`) accessibility scan over the
    screen and attach the report — noting it is one of the ten recorded reviews
    (`keyboard-and-accessibility.md:115-147`), not all of them. Write the
    § Operations section into
    `docs/frd/frd-13-desktop-operator-experience.md` (skeleton by [[FND-008]],
    plan handle `DSK-00-08`), and move `PAR-27`
    (`docs/desktop/01-inventory-and-parity/parity-matrix.md:72`) from
    `not inventoried` to `implemented`, **filling its empty `Verification` cell**
    with the evidence produced here. Then run the simplification pass over this
    branch's own diff, record it under a dated `## Simplification pass` heading
    below, and open the PR into `dev`.

## Verification

Evidence tier from the body: **7** — Browser/accessibility, read as the desktop
equivalent: a real authenticated workflow, keyboard, focus and error behaviour,
semantic labels, text-plus-colour states. Tier 7 obliges a real run against the
gateway; an automated scan does not replace the keyboard walk, and a mocked
screenshot does not satisfy it.

- `dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release`
  — expected: the freshness, enablement, refusal, disconnected, cancellation and
  state-vocabulary facts pass.
- `pwsh ./tests/Pegasus.Desktop.UITests/ui-tests.ps1 -AppPid <pid> -Script operations`
  — expected: every assertion passes; screenshots of the loaded, refused and
  disconnected states are attached. This is the tier-7 evidence.
- `AxeWindowsCLI` scan of the Operations screen — expected: no critical issue;
  the report is attached to the ticket proof.
- `pwsh ./eng/api/Generate-ApiClient.ps1` then `git diff --exit-code` — expected:
  clean, proving the committed generated client matches the contract.
- `git diff --stat origin/dev -- src/Pegasus.Web src/Pegasus.Core src/Pegasus.Infrastructure src/Pegasus.Worker`
  — expected: **empty output**. This single command proves the Guardrails' scope
  boundary and belongs in the proof verbatim.

## Risks / open questions

- **A blank table is the defect this screen exists to avoid.** The conventional
  WinUI load pattern clears the collection before refreshing; on this screen
  that reads as "nothing failed". Mitigation: step 4's rule that failure
  preserves rows, asserted by the second view-model fact at step 10.
- **Revoke needs two operation keys, and one is easy to miss.** The web handler
  generates a separate key for the lease (`Index.cshtml.cs:132`) before the
  revoke's own (`:153`). Mitigation: step 6 states it and the view-model test
  asserts two distinct keys leave the client.
- **The operator-copy ban list is enforced by the reviewer alone.**
  `docs/design/README.md:417-420` says CI does not check it, and this screen's
  subject matter is precisely the banned vocabulary — queue, lease, projection,
  intake. Mitigation: step 5's copy review, every string sourced from
  `OperatorLabels` or the eight approved web sentences, and the reviewer named
  in § Routing.
- **The screen spec says "Retry (reasoned)" and Core accepts no reason.**
  `screen-specs.md:392` versus `RequestOperations.cs:157-161`. Mitigation: the
  trivial default is taken and recorded (plain confirmation for retry,
  `ReasonDialog` for revoke); a reason on retry is a Core command change and a
  different ticket. Flagged to the reviewer as a deliberate deviation from the
  spec's wording.
- **Four of the five health rows have no phase-5 contract.** They come from
  [[PLAT-015]] (plan handle `DSK-10-15`)'s `GET /api/v1/admin/health`, which is
  `ManageWorkflowConfiguration` and phase 8 (`endpoint-map.md:37`), while this
  screen is `PerformCasework` and phase 5. Mitigation: render them absent under
  the spec's "absent when not composed" rule rather than blank. Answered by:
  [[PLAT-015]].
- **The upload-links half depends on a contract this ticket does not own.**
  [[GWY-013]] (plan handle `DSK-03-13`) publishes `GET /api/v1/operations`
  (assumption A-07-04-2). If `canRevoke` and the link fields are absent, the
  revoke table is deferred and the gap raised there — not filled with a second
  query here. Answered by: [[GWY-013]].
- **Revoke needs a case edit lease the desktop must acquire** through
  [[GWY-008]] (plan handle `DSK-03-08`)'s endpoints (assumption A-07-04-4). If
  they have not landed, retry ships alone — a smaller screen, not a broken one.
  Answered by: [[GWY-008]].
- **Two tickets claim `tests/Pegasus.Desktop.ViewModelTests`.** [[TEST-004]]
  (plan handle `DSK-08-04`) per this body and [[FND-038]] (plan handle
  `DSK-02-13`) per [[FEAT-001]]'s files document. Mitigation: this ticket
  creates neither and writes into whichever landed; see [[TEST-004]]'s plan for
  the ownership reconciliation.
- **`winapp ui` and `AxeWindowsCLI` must be available** (assumption A-07-04-5)
  or the tier-7 evidence cannot be produced. Mitigation: confirm [[TEST-006]]
  (plan handle `DSK-08-06`) and [[DUI-015]] (plan handle `DSK-06-15`) have
  landed at step 1; if not, the ticket stops rather than substituting a
  screenshot.
- **`OperatorLabels` moves under this ticket's feet.** [[GWY-016]] (plan handle
  `DSK-03-16`) and [[FEAT-023]] (plan handle `DSK-05-23`) relocate it to
  `Pegasus.Contracts`. Mitigation: consume it, never fork it; expect a namespace
  change and coordinate rather than writing a second office-time helper.

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over
this branch's own diff before the PR, recorded here under a dated heading._
