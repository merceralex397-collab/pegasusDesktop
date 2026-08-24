# Plan — FEAT-003: S3 Case detail read-only and history

**Diff estimate: ~24 files, ~2,900 lines.**

Derived from the files document: 8 `Pegasus.Contracts` DTO files — header plus
seven sections (~420 lines total); 1–2 `/api/v1` read-endpoint files in
`Pegasus.Web` (~320, eight routes each with its own `ETag`); 8 desktop files —
`CaseWorkspaceViewModel` (~300), a child view model per rendered tab (Overview,
History, Communications ≈ 3 × ~150), `CaseWorkspacePage.xaml` (~380, header plus
tab strip plus activity pane), `HistoryView.xaml` (~150),
`CommunicationsView.xaml` (~180 including the Queries group), code-behind (~60);
1 `Pegasus.Desktop.Infrastructure` per-section `ETag` cache (~120); 3 test
files — ViewModel (~420), contract (~380, five facts × eight routes), UI script
(~110); ~3 regenerated Kiota files (~250, generated); 4 documentation edits
(parity row, screen-spec amendment, FRD-13 section ~50, capabilities row). The
web surface it replaces — 654 lines of page model plus 999 lines of `.cshtml`
and partials — is **not** in the diff. This is the largest ticket in the
Phase 3 set because it is the shell six later slices hang their tabs on.

## Approach

Build the workspace as a **header plus a bag of independent child view models**,
one per tab, each owning its own request, its own `ETag`, its own state and its
own error. The header is loaded once with `GET /api/v1/cases/{id}` and stays
stable; a tab's section endpoint is called on first activation and never before.
The alternative — mirroring the web and fetching the composed `CaseDetails` once
(`src/Pegasus.Core/Cases/CaseQueries.cs:264-357` composes eight ports in a
single call) — was rejected because it makes the ≤ 200 ms cached-navigation
budget unreachable, ties the whole workspace's availability to the slowest
dependency, and would mean one failing section blanks the case. The second
rejected alternative was a single view model with a `SelectedTab` switch: it
makes per-tab error isolation and per-section `ETag` revalidation into special
cases inside one state machine, and it gives the six later slices no seam to
plug into. The cost of the chosen shape is that the gateway must split the
composed record — work that belongs to [[GWY-007]] (plan handle `DSK-03-07`) and
that step 3 confirms before any desktop code is written.

## Governing docs

### Meets — the ticket's `refs`

`refs`: `docs/frd/frd-01-case-identity-and-lifecycle.md`.

| Requirement | Where | Met by |
| --- | --- | --- |
| "Every staff case mutation targets one identified case through a named Core action and requires the role permitted by the staff role access matrix." | `frd-01:84` | Step 3 and step 10 — this slice is read-only and exposes no mutation at all, and the 403 contract fact proves the read itself is behind `PerformCasework` |
| "Other authorised staff remain read-only and can see the holder and recovery state." | `frd-01:84` | Step 5 (the header renders the holder through `CaseEditAuthorityHolder`, `src/Pegasus.Core/Workflow/CaseEditAuthority.cs:75-81`, by name and never by identifier) |
| "A deliberate recovery or material denial/failure is attributable permanent history; routine renewal, expiry, heartbeat, polling, and adapter mechanics remain telemetry." | `frd-01:88` | Step 6 (the History tab renders permanent action history only — no lease heartbeats, no polling, no adapter mechanics; `docs/design/README.md:761` is the same rule stated as a UI constraint) |
| "The history records what was attempted, by whom, through which channel, against which party/address, when, and with what evidence." | `frd-01:96` | Step 6 (actor display name, action, Europe/London timestamp, reason where recorded) |
| "Principal and reference are immutable after allocation." | `frd-01:33-38` | Step 5 — read-only; the header renders reference and principal with no control that could change either |

### New ADR

The ticket carries `docs_todo: true` (confirmed in `get_doc_gates FEAT-003`).

> **New ADR** — ADR-0103 (gateway = evolved `Pegasus.Web`; never direct database
> access from a workstation) and ADR-0104 (online-required, bounded local cache
> only), authored by [[FND-005]] (plan handle `DSK-00-05`).
> This plan is written to the decisions as recorded in
> `docs/desktop/00-governance-and-workflow/README.md` § 3 and to L-01 in
> `docs/desktop/README.md` § Locked decisions; if either ADR lands differently
> this plan is revised before implementation. **ADR-0104 bounds step 4's cache
> explicitly: section payloads are cached for the lifetime of the open case and
> revalidated with `If-None-Match`; nothing is persisted for offline use.**

ADR-0100 has more than one interested party through the no-split deviation
recorded in `docs/desktop/05-implementation-and-migration/README.md` § 3; it is
authored by [[FND-026]] (plan handle `DSK-02-01`); see [[FND-026]]'s plan for
the ownership reconciliation.

### Programme-level authorities that bind today

| Authority | Requirement | Met by |
| --- | --- | --- |
| L-01 (`docs/desktop/README.md` § Locked decisions) | Gateway is `Pegasus.Web` evolved in place | Step 3 |
| L-02 (same) | Verification runs on the local Test/UAT stack | Steps 11–12 |
| L-04 (same) | Routing named on the ticket | § Routing below |
| `AGENTS.md` § Product invariants | Core owns business policy; duplicate business implementation is a stop condition | Step 3 (the section reads call the same Core ports `GetCase` composes) and the no-second-communications-read boundary |
| `docs/engineering.md` § Plan sizing | Diff estimate first, derived from the files document | First line |
| `docs/engineering.md` § Required evidence tiers | Tier 12 obliges a comparison against the persisted operator view, not a mock | Step 11 |
| `docs/design/README.md:432-439` | Only populated, relevant sections render; an empty-state panel is a defect | Step 5, with the single sanctioned Queries exception at step 7 |
| `docs/design/README.md:761` | The History panel excludes bodies, routine views, refresh/polling, retries, lease heartbeats and adapter mechanics | Step 6 |
| `docs/design/README.md:412-420` | Banned words — merge rule, not a CI check | Steps 5–7 and the reviewer at `enter-review` |
| `docs/desktop/00-governance-and-workflow/README.md` § 3 | Six-question test answered with evidence | `research` § Execution placement |
| Plan 05 § 7 | `/api/v1` gated off returns 404; tests enable `Features:DesktopGateway` explicitly | Step 10 |
| Proposal §14.5 | Case workspace: stable header, lazily loaded sections | Steps 4–5 |
| Proposal §15.1 | Cached-navigation budget | Step 12 |

## Routing

Copied from the ticket body's `## Routing` block.

- **Subagent**: `winui-dev` — `.codex/agents/winui-dev.toml`;
  `pegasus-gateway-dev` — `.codex/agents/pegasus-gateway-dev.toml`;
  `pegasus-test-engineer` — `.codex/agents/pegasus-test-engineer.toml`.
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `winui-dev-workflow`
  (`.codex/skills/winui-dev-workflow/SKILL.md`) → `winui-design`
  (`.codex/skills/winui-design/SKILL.md`) → `dotnet-webapi` (dotnet/skills
  `98f84851`, `plugins/dotnet-aspnetcore/skills/dotnet-webapi/SKILL.md`) →
  `run-tests` (dotnet/skills `98f84851`) → `winui-code-review` at review.
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`,
  `set_ticket_doc`, `append_scratch`, `move_item`); Microsoft Learn
  (`microsoft_docs_search`, `microsoft_code_sample_search`).
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` →
  `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout`
  (call `get_doc_gates <id>` before every move; a move crosses at most one
  gated boundary).
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement
  (`AGENTS.md` § Repository task workflow step 5).

## Steps

These refine the ticket body's fourteen steps in the same order and with the
same ownership.

1. **Orient and take.** Read the plan row (`docs/desktop/05-implementation-and-migration/README.md`
   § 5, `DSK-05-03`), `vertical-slices.md` § S3, and `docs/design/README.md:432-439`
   ("only populated, relevant sections render"). Then `get_doc_gates FEAT-003`
   and `take_ticket` with branch `task/dsk-05-03-case-detail`, worktree
   `../pegasus-worktrees/dsk-05-03-case-detail`, from `origin/dev`.
2. **Confirm the recorded behaviour.** The `research` document carries the field
   map, the three-tab fact (`Details.cshtml.cs:58-64`) and the composed-read
   fact (`CaseQueries.cs:264-357`). Re-verify with
   `git diff --stat bbd1c549..HEAD -- src/Pegasus.Web/Pages/Cases/Details.cshtml.cs src/Pegasus.Web/Pages/Cases/Shared src/Pegasus.Core/Cases/CaseQueries.cs`;
   if the upstream sync moved any of them, re-read and update `research` with
   the new SHA. The recorded SHA is `bbd1c549`. While re-reading the four
   partials, produce the header/Overview/per-tab field allocation the parity
   comparison at step 11 will use.
3. **Confirm the gateway split — this step gates the rest.** From [[GWY-007]]
   (plan handle `DSK-03-07`), confirm four things:
   - `GET /api/v1/cases/{id}` returns the header **plus** the Overview section
     with a `version` and a weak `ETag`;
   - the seven section endpoints (`/vehicle`, `/assessment`, `/documents`,
     `/communications`, `/tasks`, `/reports`, `/history`) each carry their own
     `ETag` so they load independently (assumption `A-05-08`);
   - each section endpoint re-asserts its own case id, preserving the
     cross-case integrity check `GetCase` performs at
     `src/Pegasus.Core/Cases/CaseQueries.cs:316-322` (assumption `A-05-09`);
   - `GET /api/v1/cases/{id}/history` is **paged** (assumption `A-05-10`);
     `CaseDetails.History` is unbounded today.
   History rows come from the action-history read ports in
   `src/Pegasus.Core/Identity/` — the same source the web uses. **If the split
   has not landed, stop and raise it on [[GWY-007]]**; do not fetch the composed
   record once per tab.
4. **`CaseWorkspaceViewModel`** in `src/Pegasus.Desktop`: header state plus one
   child view model per tab. A tab's data loads on **first activation**, not on
   case open; each child exposes its own Loading / Empty / Error / Loaded and
   can be refreshed independently. Cache section payloads by `ETag` for the
   lifetime of the open case in `Pegasus.Desktop.Infrastructure` and revalidate
   with `If-None-Match` on manual refresh. A tab whose section endpoint does not
   yet exist (Vehicle, Assessment, Documents, Reports in Phase 3) renders
   nothing and shows **no error** — that is a view-model fact, not a fallback.
5. **The workspace XAML.** A stable header showing reference, status, assignee,
   priority and save state, plus the command-bar **slot** (its contents arrive
   with [[FEAT-006]], plan handle `DSK-05-06`); the eight-tab sub-navigation in
   the order fixed at `docs/desktop/06-ui-design/screen-specs.md:182`; a
   collapsible right-side activity pane. Only populated sections render — a tab
   with nothing recorded and no available action shows no empty-state panel. The
   header discloses the edit-authority holder by **name** through
   `CaseEditAuthorityHolder` (`src/Pegasus.Core/Workflow/CaseEditAuthority.cs:75-81`),
   never by identifier. AutomationIds are fixed at `screen-specs.md:225-227`.
6. **The History tab** over `GET /api/v1/cases/{id}/history`: newest first,
   paged, each row rendering actor **display name**, action, timestamp
   (Europe/London through the shared vocabulary map) and reason where recorded.
   **No GUID, hash or version integer reaches the screen** — in particular
   `CaseHistoryEntry.BeforeVersion` / `AfterVersion`
   (`src/Pegasus.Core/Cases/CaseQueries.cs:94-95`) are not rendered. Never
   substitute `Actor` (a subject id) when `ActorDisplayName` is the honest
   `ActorDisplayNames.UnknownStaff` fallback (`:104`).
7. **The Queries group on the Communications tab** (upstream `CASE-009`). Read
   each linked e-mail's canonical classification from the communications payload
   [[FEAT-037]] (plan handle `DSK-07-11`) supplies, and render
   `Queries`-destination e-mails as their **own identified read-only group**
   within the tab — headed with the settled word `Queries`, never "Engineer
   queries". The group is read-only: no manual query-creation control, no
   **Raise a query** button, no reply, no resolve; raising, replying to and
   resolving a query stay out of scope (upstream `CASE-002`), and the desktop
   mutates no mailbox. When the tab has linked e-mails but none is
   Query-classified, the group states so truthfully in one line rather than
   vanishing — **that single line is the one stated exception to the
   only-populated-sections rule**, and it is recorded as such in the screen-spec
   amendment at step 14. Render nothing from the classification policy's
   `PolicyKey` or `PolicyVersion`
   (`src/Pegasus.Core/Intake/Classification/MailOperationalDestinationPolicy.cs:17-22`).
   If [[FEAT-037]]'s payload has not landed (assumption `A-05-11`), defer the
   group with a recorded reason rather than inventing a second read.
8. **Layout and focus.** The workspace is reachable without horizontal scrolling
   at the minimum supported window size from `screen-specs.md` § `Shell`
   (1280×800, `:189-190`), and focus order runs header → sub-navigation →
   content → activity pane.
9. **View-model tests** in `tests/Pegasus.Desktop.ViewModelTests` (project from
   [[FND-038]], plan handle `DSK-02-13`): lazy tab activation (an unvisited tab
   issues **no** request); per-tab error isolation (one failing section does not
   blank the workspace); `ETag` revalidation on refresh; history paging; a tab
   with no section endpoint rendering nothing without an error; and the three
   Queries facts — present and grouped when a Query-classified e-mail exists,
   truthful single line when linked e-mails exist but none is Query-classified,
   and **no creation, reply or resolve command in either case**.
10. **Contract tests** in `tests/Pegasus.Api.ContractTests` for the header and
    every section endpoint: 200 with `version` and `ETag`; 304 on
    `If-None-Match`; 401 without a token; 403 for an actor without
    `PerformCasework`; 404 for an unknown case. Enable `Features:DesktopGateway`
    explicitly in the factory.
11. **Parity comparison** against `tests/Pegasus.IntegrationTests/CaseDetailsWebTests.cs`
    scenarios: for three fixture cases, compare the web Details page against the
    desktop workspace **field by field** using the allocation produced at step 2,
    and compare history rows one to one. Record the table in `proof`. Record the
    three-tabs→eight-tabs difference as a **known deliberate difference**, not
    as a gap.
12. **Navigation budget.** First useful view ≤ 200 ms perceived after the header
    has loaded (cached navigation budget, proposal §15.1). Record the
    measurement method and the figures — and the workstation specification, as
    tier 12 evidence must come from a real run.
13. **UI script and scan.** Add `tests/Pegasus.Desktop.UITests/ui-tests.ps1 -Script case-detail`
    (harness from [[TEST-006]], plan handle `DSK-08-06`): open a case from the
    list, cycle every tab by keyboard using `wait-for` (never a sleep), and
    assert the header stays stable across tab changes. Then run the
    `axe-windows` scan from [[DUI-015]] (plan handle `DSK-06-15`) and attach both
    artefacts.
14. **Documentation and PR.** Update `docs/desktop/01-inventory-and-parity/parity-matrix.md`
    row `PAR-08` (`:53`) — **read path only**, the edit handlers stay with
    [[FEAT-005]] (plan handle `DSK-05-05`). Amend
    `docs/desktop/06-ui-design/screen-specs.md` § `§13.8 Communications` to
    record the Queries group, its truthful empty state and the absence of any
    create/reply/resolve control, so [[DUI-013]] (plan handle `DSK-06-13`)
    carries it into FRD-13. Add the case-workspace section to
    `docs/frd/frd-13-desktop-operator-experience.md` and a `DSK` row to
    `docs/capabilities.md`. Run the simplification pass over the branch diff
    (`AGENTS.md` step 4), record it under a dated `## Simplification pass`
    heading here, then open the PR into `dev`.

## Verification

Evidence tiers from the body: **tier 5** (Web/API/MCP caller), **tier 7**
(Browser/accessibility), **tier 12** (Integrated workflow).

| Command | Expected | Evidence captured |
| --- | --- | --- |
| `dotnet build ./Pegasus.slnx --configuration Release --no-restore` | Succeeds under `TreatWarningsAsErrors=true` with no `WUI*` suppression | Build log tail |
| `dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release --no-build` | Lazy-load, error-isolation, revalidation, paging and the three Queries facts pass | Test summary |
| `dotnet test ./tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj --configuration Release --no-build` | Header and section 200 / 304 / 401 / 403 / 404 facts pass | Test summary — **tier 5 evidence** |
| `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --no-build --filter "Category!=Corpus&Category!=Browser"` | `CaseDetailsWebTests` unchanged and green | Test summary (proves the web path was not disturbed) |
| `pwsh ./tests/Pegasus.Desktop.UITests/ui-tests.ps1 -Script case-detail` | Keyboard tab cycle passes; the header stays stable; no sleep-based waits | Results JSON + screenshots + `axe-windows` report — **tier 7 evidence** |
| Parity table on the Test/UAT stack | Field-by-field and history-row equality against the web Details page for three fixture cases | The table in `proof` — **tier 12 evidence**; a mocked path does not satisfy it |
| Navigation-budget run | First useful view ≤ 200 ms perceived after header load | Method, figures and workstation specification in `proof` |

## Risks / open questions

- **Risk, and the biggest one: the gateway has not split `CaseDetails`.**
  `GetCase` composes eight ports in one call (`CaseQueries.cs:264-272`).
  *Mitigation:* step 3 gates the whole ticket on it, and the correct action if
  the split is missing is to **stop and raise it on [[GWY-007]]** (plan handle
  `DSK-03-07`) — a scope boundary a named sibling owns. Fetching the composed
  record once per tab is a defect, not a workaround.
- **Risk: the section split silently loses the cross-case integrity check.**
  `GetCase` throws when a composed part belongs to another case
  (`CaseQueries.cs:316-322`); seven independent endpoints have no such
  composition point. *Mitigation:* step 3's third check and a contract fact per
  section asserting the returned case id.
- **Risk: a version integer or a subject id reaches the History row.**
  `CaseHistoryEntry` carries `BeforeVersion`, `AfterVersion` and a raw `Actor`
  (`CaseQueries.cs:88-105`). *Mitigation:* step 6 states the rendered field set
  explicitly and a view-model fact asserts the raw `Actor` is never substituted
  for a missing display name.
- **Risk: the empty-state exception is read as a breach.** The Queries group's
  truthful single line contradicts `docs/design/README.md:432-439` on its face.
  *Mitigation:* step 14 records it in the screen spec as the one sanctioned
  exception, with upstream `CASE-009` named as its source.
- **Risk: [[FEAT-037]]'s communications payload has not landed.** It is a
  Phase 5 ticket and this is Phase 3 (assumption `A-05-11`). *Mitigation:*
  step 7 defers the group with a recorded reason rather than adding a second
  read — which the Guardrails forbid outright.
- **External dependency, not an open question: upstream `CASE-020`** (read the
  case header from the case, not the instruction draft) has **no fork ticket**
  and must be true before `PAR-08` reaches parity. The ticket body's instruction
  is binding: **raise it rather than working around it.** It arrives through the
  one-way upstream sync described in
  `docs/desktop/00-governance-and-workflow/README.md` § Recommended branching
  flow, item 2. Recorded here so the implementer stops rather than compensating
  in the header view model.
- **Scope boundary: upstream `CASE-002`** (the query lifecycle — raise, reply,
  resolve) is not activated here. A **Raise a query** control, a reply or a
  resolve on the Communications tab is a stop condition.
- **Scope boundary: four tabs render nothing in Phase 3.** Vehicle, Assessment,
  Documents and Reports arrive with [[FEAT-015]], [[FEAT-017]], [[FEAT-014]] and
  [[FEAT-018]]. That is correct behaviour under the only-populated-sections
  rule, and step 9 has a fact for it.
- **Not an open question: the operator decisions are settled.** D-002, D-003 and
  D-004 do not touch this ticket, which performs no Azure write.

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over
this branch's own diff before the PR, recorded here under a dated heading._
