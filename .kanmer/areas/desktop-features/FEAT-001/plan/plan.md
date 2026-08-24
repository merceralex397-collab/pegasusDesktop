# Plan — FEAT-001: S1 Dashboard and work queue

**Diff estimate: ~19 files, ~1,850 lines.**

Derived from the files document, not asserted: 2 new `Pegasus.Contracts` DTO
files (~120 lines); 1–2 `/api/v1` endpoint files in `Pegasus.Web` (~180); 4
desktop files — `DashboardViewModel` (~260), `DashboardPage.xaml` (~220), its
code-behind (~40), shell rail binding (~40); 1 `Pegasus.Desktop.Infrastructure`
client wrapper (~60); 3 test files — ViewModel (~320), contract (~280), UI
script (~90); ~3 regenerated Kiota client files (~200, generated); 3
documentation edits — parity row, FRD-13 section (~40), capabilities row.
The web dashboard (43 lines) and the rail filter (51 lines) are **not** in the
diff; the whole surface they replace is 94 lines, which is why the desktop-side
figure dominates.

## Approach

Replace the page filter's side channel with **one query contract**: the desktop
asks the gateway for the dashboard and for the rail counts, and never derives a
figure for itself. The two routes call the same `IGetOperationsSnapshot` and
`IDashboardQueries` the Razor page calls, so there is exactly one business
implementation of every count. The alternative considered and rejected was
deriving the rail badges on the desktop from the `/dashboard` payload — the
stage counts are all present there, so it would have saved a round trip. It is
rejected because `RailCountsPageFilter.cs:13-20` records that Inbox and Cases
have *no* figure to reuse "without inventing one", and a desktop that computed
badges locally would have to decide what to show for them; a separate endpoint
that simply omits what it cannot answer preserves the "absent renders nothing,
never a zero" rule as a property of the wire rather than of the client. The
second rejected alternative was a shared cached snapshot service on the desktop
feeding both surfaces: it re-creates the side channel this ticket exists to
remove and makes the freshness of the rail and the freshness of the tiles
diverge invisibly.

## Governing docs

### Meets — the ticket's `refs`

`refs`: `docs/frd/frd-12-operator-experience.md`.

| Requirement | Where | Met by |
| --- | --- | --- |
| "an authenticated office-wide dashboard with Europe/London day boundaries and Monday-to-Monday weeks" | `frd-12:8-9` | Steps 3–4 (the gateway returns the snapshot whose boundaries are computed at `OperationsSnapshot.cs:137-159`); Step 7 renders through the shared Europe/London vocabulary map |
| "clear counts that link to their exact filtered work and do not render stale zero placeholders" | `frd-12:13-14` | Steps 5 and 8 (nullable rail members; a count the gateway omits renders nothing); Step 7 (each tile is a link to its filtered queue) |
| "loading, empty, current, stale, unavailable, partial, failed, validation, conflict, and access-denied states" | `frd-12:22-23` | Step 6 (explicit VM states) and Step 10 (a fact per state) |
| "keyboard, pointer, screen-reader, 200% zoom, forced-colour, and reduced-motion support" | `frd-12:24-25` | Steps 7, 9 and 12 (`AutomationId` on every control; `F5`/`Ctrl+R`; keyboard-only `winapp ui` traversal plus the `axe-windows` scan) |
| "Every count and query exposes its last successful update time and current refresh state. `0`, loading, current, stale-with-last-good-time, partial, unavailable, and failed are distinct outcomes. A refresh never replaces a last-good value with a false zero" | `frd-12:95-99` | Steps 6 and 9 (`LastLoadedAt`, freshness state in the page header, last-good retained across a failed refresh) |
| "Manual refresh reruns the same exact filtered query; it does not change policy or create a business transition." | `frd-12:101-102` | Step 6 (`RefreshCommand` re-issues the identical request; there is no write path in this slice at all) |
| "`New cases today` counts every instructed Case created in the current Europe/London calendar day… It excludes Image-initiated Cases, Triage, Unidentified, and `Blocked intake`." | `frd-12:107` | Step 4 (the desktop performs no arithmetic on counts; `CaseActivityCounts.NewCasesToday` is rendered exactly as Core computed it) and Step 13 (the parity table proves equality) |
| "The UI never infers state from colour alone" | `frd-12:109-110` | Step 7 (status values render through the shared vocabulary list with text) |

### New ADR

The ticket carries `docs_todo: true` (confirmed in `get_doc_gates FEAT-001`,
`leave-backlog` → `governing-doc` `satisfied: true`), so no conversion ADR
exists yet.

> **New ADR** — ADR-0103 (gateway = evolved `Pegasus.Web`; never direct database
> access from a workstation) and ADR-0101 (local-execution / cloud-authority
> split and the six-question test), authored by [[FND-005]] (plan handle
> `DSK-00-05`).
> This plan is written to the decisions as recorded in
> `docs/desktop/00-governance-and-workflow/README.md` § 3 and to L-01 in
> `docs/desktop/README.md` § Locked decisions; if either ADR lands differently
> this plan is revised before implementation.

The reserved block spans ADR-0100…ADR-0110 across several areas. ADR-0100 has
more than one interested party through the no-split deviation recorded in
`docs/desktop/05-implementation-and-migration/README.md` § 3; it is authored by
[[FND-026]] (plan handle `DSK-02-01`); see [[FND-026]]'s plan for the ownership
reconciliation.

### Programme-level authorities that bind today

| Authority | Requirement | Met by |
| --- | --- | --- |
| L-01 (`docs/desktop/README.md` § Locked decisions) | Gateway is `Pegasus.Web` evolved in place; versioned `/api/v1` route groups beside Razor Pages; no new deployment unit | Steps 3–4 |
| L-02 (same) | Test/UAT is the local production-mimicking stack; never an Azure test resource | Steps 12–13 |
| L-04 (same) | Every ticket names its subagent, skills and MCP tools | § Routing below |
| `AGENTS.md` § Product invariants | `Pegasus.Core` owns business policy; duplicate business implementation is a stop condition | Step 4 (the endpoints call the same `IGetOperationsSnapshot` / `IDashboardQueries`) |
| `docs/engineering.md` § One Core owner | One policy owner per rule | Step 4 |
| `docs/engineering.md` § Required evidence tiers | Tier 5, 7 and 12 obligations stated on the ticket | Steps 11, 12, 13 |
| `docs/engineering.md` § Plan sizing | A plan states its diff estimate first, derived from the files document | First line of this document |
| `docs/design/README.md:396-445` | Banned words, closed necessary-copy list, four hard rules — merge rules, not CI checks | Step 7, and the reviewer at `enter-review` |
| `docs/desktop/00-governance-and-workflow/README.md` § 3 | Six-question cloud-justification test answered with evidence | `research` § Execution placement |
| Plan 05 § 7 (Risks and traps) | `/api/v1` gated off returns 404; tests must enable `Features:DesktopGateway` explicitly | Steps 11 and the Verification section |
| Proposal §14.3 | The dashboard answers five questions with actionable lists, no vanity charts | Step 7 |
| Proposal §14.9 | Keyboard: `F5` / `Ctrl+R` refresh | Step 9 |

## Routing

Copied from the ticket body's `## Routing` block.

- **Subagent**: `winui-dev` — `.codex/agents/winui-dev.toml` (screen and view
  model); `pegasus-gateway-dev` — `.codex/agents/pegasus-gateway-dev.toml` (any
  gap in the `/api/v1` dashboard group); `pegasus-test-engineer` —
  `.codex/agents/pegasus-test-engineer.toml` (view-model and contract tests);
  `pegasus-ui-verifier` — `.codex/agents/pegasus-ui-verifier.toml` (UI script
  and axe scan).
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `winui-dev-workflow`
  (`.codex/skills/winui-dev-workflow/SKILL.md`, win-dev-skills v0.5.0
  `f1028dd5`) → `winui-design` (`.codex/skills/winui-design/SKILL.md`) →
  `dotnet-webapi` (dotnet/skills `98f84851`,
  `plugins/dotnet-aspnetcore/skills/dotnet-webapi/SKILL.md`) →
  `code-testing-agent` and `run-tests` (dotnet/skills `98f84851`,
  `plugins/dotnet-test/skills/`) → `winui-ui-testing`
  (`.codex/skills/winui-ui-testing/SKILL.md`) → `winui-code-review`
  (`.codex/skills/winui-code-review/SKILL.md`) at review.
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`,
  `set_ticket_doc`, `append_scratch`, `move_item`); Microsoft Learn
  (`microsoft_docs_search`, `microsoft_docs_fetch`, `microsoft_code_sample_search`).
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` →
  `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout`
  (call `get_doc_gates <id>` before every move; a move crosses at most one
  gated boundary).
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement
  (`AGENTS.md` § Repository task workflow step 5).

## Steps

These refine the ticket body's fourteen implementation steps in the same order
and with the same ownership; they add the *how* the body leaves out and change
no file assignment.

1. **Orient and take.** Read the plan row (`docs/desktop/05-implementation-and-migration/README.md`
   § 5, `DSK-05-01`), `vertical-slices.md` § `Common to every slice` and § S1,
   and `docs/design/README.md:422-445` with its banned-word list at `:412-420`.
   Then `get_doc_gates FEAT-001` and `take_ticket` with branch
   `task/dsk-05-01-dashboard` and worktree
   `../pegasus-worktrees/dsk-05-01-dashboard` created from `origin/dev`.
2. **Load the skills in order** and use `.codex/skills/winui-dev-workflow/BuildAndRun.ps1`
   for every local run — a plain `dotnet run` launches without package identity,
   and `AppInstance` single-instance behaviour from [[FND-035]] (plan handle
   `DSK-02-10`) silently differs without it. Use `winui-search.exe` under
   `.codex/skills/winui-design/` for control API lookups rather than guessing a
   WinUI type name.
3. **Record current behaviour.** The `research` document already carries this;
   confirm it is still true by re-running `git log -1 --format=%H` and
   `git diff --stat <recorded-sha>..HEAD -- src/Pegasus.Web/Pages/Index.cshtml.cs src/Pegasus.Web/Presentation/RailCountsPageFilter.cs src/Pegasus.Core/Operations`.
   If the upstream sync moved any of them, re-read and update `research` before
   continuing — that is the parity-drift trap, and the recorded SHA is
   `bbd1c549`.
4. **Verify and close the gateway contract.** Confirm [[GWY-006]] (plan handle
   `DSK-03-06`) returns all six page members plus `asOfUtc` and a weak `ETag`.
   Three specific checks, each from a research finding:
   - the endpoint filter requires **`PerformCasework`**, matching
     `src/Pegasus.Core/Operations/OperationsSnapshot.cs:96` — not the
     `AccessStaffApplication` recorded at
     `docs/desktop/03-gateway-api-and-data/endpoint-map.md:43-44`. Raise the
     endpoint-map correction on [[GWY-006]]; do not fix the map from here;
   - the `ETag` hashes the **payload**, not `asOfUtc` (which changes on every
     call, `OperationsSnapshot.cs:98`), or `If-None-Match` can never return 304;
   - the mail figure is on the wire as `unidentified`, from
     `MailActivityCounts.Unidentified` (`DashboardCounts.cs:48`), never as
     `needsSorting`.
   Where a field is missing, add it inside the `/api/v1` group gated by
   `Features:DesktopGateway`, calling the **same** `IGetOperationsSnapshot` /
   `IDashboardQueries` the Razor page calls. Done when a fact in
   `tests/Pegasus.Api.ContractTests` asserts every field with the gate enabled.
5. **Add the DTOs to `src/Pegasus.Contracts`** following [[FND-029]] (plan handle
   `DSK-02-04`) / [[GWY-001]] (plan handle `DSK-03-01`) conventions: paging
   envelope, no enum `ToString()` on the wire, `asOfUtc` as `DateTimeOffset`.
   **The rail-count members are nullable.** A record of three non-nullable
   `int`s forces `0` for Inbox and Cases and breaks the one rule step 8 exists
   to preserve. No ASP.NET, EF or WinUI type may appear in this project.
6. **Implement `DashboardViewModel`** in `src/Pegasus.Desktop` with explicit
   `Loading` / `Empty` / `Error` / `Loaded` states, a single **coalesced**
   `RefreshCommand` — a second refresh while one is in flight joins the first
   `Task`, it does not queue a second request — `LastLoadedAt` rendered as
   Europe/London through the shared vocabulary map, and a `CancellationToken`
   cancelled on navigation away. A failed refresh **keeps the last-good values
   and marks them stale**; it never blanks the tiles (`frd-12:97-99`). It calls
   only the generated client from [[GWY-005]] (plan handle `DSK-03-05`) through
   `Pegasus.Desktop.Infrastructure`, and never references
   `Pegasus.Infrastructure`.
7. **Build the Dashboard XAML** for the five §14.3 questions as actionable
   lists and counts, using the tile list fixed at
   `docs/desktop/06-ui-design/screen-specs.md:131-140`: active cases Not ready |
   Review | Held; e-mail activity Received today | Unidentified | Blocked; New
   cases today; Sent to Engineer today/week; Reports sent today/week; recent
   cases (rendered **only** when there are entries); integration failures needing
   attention (absent when none). AutomationIds are `Dashboard.Tile.<Metric>`,
   `Dashboard.Refresh`, `Dashboard.Recent.Row.<Ref>` (`screen-specs.md:145-147`).
   Status values render through the shared vocabulary list with **text**, never
   colour alone. No field hints, no how-it-works copy, no charts.
8. **Wire the shell rail counts** to `GET /api/v1/dashboard/rail-counts` through
   the shell view model from [[DUI-004]] (plan handle `DSK-06-04`), preserving
   the semantics at `RailCountsPageFilter.cs:13-20` exactly: a count the gateway
   omits renders nothing, never a zero. Do not invent a figure for Inbox or
   Cases. Add a view-model fact that a `null` count produces an empty badge.
9. **Bind `F5` and `Ctrl+R`** to `RefreshCommand` (proposal §14.9) and show the
   freshness state — current / stale / unavailable — in the page header control
   from [[DUI-012]] (plan handle `DSK-06-12`).
10. **View-model tests** in `tests/Pegasus.Desktop.ViewModelTests` (project from
    [[FND-038]], plan handle `DSK-02-13`): each of the four states; refresh
    coalescing (two concurrent invocations issue one request); cancellation on
    navigate-away; an error response mapped to the `InfoBar` problem
    presentation from [[DUI-010]] (plan handle `DSK-06-10`); a stale refresh
    retaining last-good values; a `null` rail count rendering nothing.
11. **Contract tests** in `tests/Pegasus.Api.ContractTests` for both routes:
    gate off → 404; gate on + no token → 401; gate on + a staff token → 200 with
    every field; `If-None-Match` with the returned `ETag` → 304. Enable
    `Features:DesktopGateway` **explicitly** in the factory — a registered but
    gated-off endpoint returns 404 and otherwise reads as a routing bug
    (plan 05 § 7).
12. **UI script and accessibility scan.** Add `tests/Pegasus.Desktop.UITests/ui-tests.ps1 -Script dashboard`
    (harness from [[TEST-006]], plan handle `DSK-08-06`): launch the packaged
    app, wait on the dashboard AutomationIds — `wait-for`, never a sleep —
    traverse every list by keyboard only and open one item. Then run the
    `axe-windows` scan from [[DUI-015]] (plan handle `DSK-06-15`) on the screen
    and attach both artefacts to the ticket proof.
13. **Parity comparison.** Bring up the Test/UAT stack per
    `docs/desktop/08-testing/test-uat-stack.md:22`, sign in, and load the web
    dashboard and the desktop dashboard against the same database. Record web
    counts vs desktop counts **per figure**, using the fixtures behind
    `tests/Pegasus.IntegrationTests/DashboardCountersWebTests.cs` and
    `RailCountsWebTests.cs` so a disagreement is attributable to a figure rather
    than to a dataset. Any difference is a defect in this slice, not an accepted
    deviation.
14. **Documentation and PR.** Update `docs/desktop/01-inventory-and-parity/parity-matrix.md`
    row `PAR-05` (`:50`) to `implemented`, and to `automated verification passed`
    once step 12 is green, with evidence pointers. Add the Dashboard section to
    `docs/frd/frd-13-desktop-operator-experience.md` and a `DSK` row to
    `docs/capabilities.md`. Run the simplification pass over the branch diff
    (`AGENTS.md` § Repository task workflow step 4), record it under a dated
    `## Simplification pass` heading in this document, then open the PR into
    `dev`.

## Verification

Evidence tiers from the body: **tier 5** (Web/API/MCP caller), **tier 7**
(Browser/accessibility), **tier 12** (Integrated workflow). Commands, and which
output becomes the evidence in `proof`:

| Command | Expected | Evidence captured |
| --- | --- | --- |
| `dotnet build ./Pegasus.slnx --configuration Release --no-restore` | Succeeds under `TreatWarningsAsErrors=true` with no `WUI*` suppression | Build log tail |
| `dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release --no-build` | All dashboard view-model facts pass | Test summary (tier 5 support) |
| `dotnet test ./tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj --configuration Release --no-build` | Gate-off 404, 401, 200-with-every-field and 304 facts pass for both routes | Test summary — **tier 5 evidence** |
| `dotnet test ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj --configuration Release --no-build` | Dependency-direction facts stay green | Test summary |
| `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --no-build --filter "Category!=Corpus&Category!=Browser"` | `DashboardCountersWebTests` and `RailCountsWebTests` unchanged and green | Test summary (proves the web path was not disturbed) |
| `pwsh ./tests/Pegasus.Desktop.UITests/ui-tests.ps1 -Script dashboard` | Passes with no sleep-based waits | Results JSON + screenshot + `axe-windows` report — **tier 7 evidence** |
| Parity table, run by hand on the Test/UAT stack | Every desktop count equals the web count for the same database | The table itself, in `proof` — **tier 12 evidence** (a mocked path does not satisfy it) |

## Risks / open questions

- **Risk: the endpoint filter is written to `AccessStaffApplication`.**
  `endpoint-map.md:43-44` says so and Core says `PerformCasework`
  (`OperationsSnapshot.cs:96`). A staff actor would pass the filter and then
  throw inside Core. *Mitigation:* step 4 checks it explicitly, and a contract
  fact asserts 403 (not 500) for an actor without `PerformCasework`. The
  endpoint-map correction is **owned by [[GWY-006]]** (plan handle `DSK-03-06`)
  — a scope boundary, raised from here, not fixed here.
- **Risk: `ETag` derived from `asOfUtc` never yields 304.** *Mitigation:*
  step 4 and assumption `A-05-03`; the 304 contract fact is the proof.
- **Risk: rail counts serialized as non-nullable `int`s.** That silently turns
  "absent" into `0` and violates `frd-12:13-14` and
  `RailCountsPageFilter.cs:13-20`. *Mitigation:* step 5 fixes nullability at the
  DTO, step 8 adds the view-model fact, step 13's parity table would catch it.
- **Risk: refresh coalescing regresses under load.** The web runs the stage
  count once per authenticated request through a global filter
  (`Program.cs:261`); a queueing desktop refresh would be worse than that.
  *Mitigation:* step 6's join-the-in-flight-task shape and its view-model fact.
- **Risk: parity drift.** Upstream keeps fixing the web app. *Mitigation:*
  step 3 re-checks the three source files against the recorded SHA `bbd1c549`
  before any code is written, and records the SHA actually characterized.
- **Scope boundary, not an open question: the ViewModelTests project has two
  claimants.** The body names [[FND-038]] (plan handle `DSK-02-13`), and area 08
  also lists it as `DSK-08-04` ([[TEST-004]]). The body is settled and outranks
  this plan, so [[FND-038]] is the creator; whoever runs second finds the
  project already present. Recorded here so the reviewer sees it was noticed.
- **Scope boundary: `TriageCount`.** Computed by `GetOperationsSnapshot` and
  discarded by the web page. Not surfaced here. If a Triage figure is wanted on
  the rail it belongs to the Queues screen, [[FEAT-011]] (plan handle
  `DSK-05-11`).
- **Not an open question: the operator decisions are settled.** No question is
  raised about pilot approval, signing or feed hosting (D-002, D-003, D-004);
  none of them touches this ticket, which performs no Azure write.

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over
this branch's own diff before the PR, recorded here under a dated heading._
