# Plan — FEAT-002: S2 Case list and search

**Diff estimate: ~18 files, ~2,000 lines.**

Derived from the files document: 2–3 `Pegasus.Contracts` DTO files (~160
lines); 1 `/api/v1` cases-read endpoint file in `Pegasus.Web` (~220, most of it
the twelve-filter binding and the `sort` mapping onto `CaseSearchOrder`'s ten
members); 5 desktop files — `CaseListViewModel` (~340), `CaseListPage.xaml`
(~300, the data table plus filter pane plus column chooser), its code-behind
(~60), the global-search results view (~140), shell search-slot binding (~40);
1 `Pegasus.Desktop.Infrastructure` file for the client call plus the persisted
column layout (~90); 3 test files — ViewModel (~360), contract (~330, one
theory per filter plus the six Core input bounds), UI script (~90); ~3
regenerated Kiota files (~200, generated); 3 documentation edits. The two web
page models it replaces total 290 lines and are **not** in the diff.

## Approach

Take the web page's thirteen bound filters as the parity floor, the design
authority's twelve UI-07 fields as the pane's contents, and Core's existing
`CaseSearchOrder` as the sort — so the gateway adds a `sort` parameter and no
new business logic. Every query goes to the server: filters, sort and paging
are all server-side, and the desktop's only local work is re-ordering the 25
rows it already holds. The rejected alternative was fetching a larger window
(say 500 rows) and filtering/sorting it on the desktop for responsiveness. It
is rejected on correctness, not speed: Core sorts over the whole set
(`CaseQueries.cs:31-43` feeds `ICaseQueryStore.SearchAsync`), so a client-side
sort of a window returns a *different* answer from the server's, and the ticket
requires "result sets equal the web page for the same filters". A second
alternative — reusing the dashboard's snapshot shape for recent cases as a
search surface — is rejected because `screen-specs.md:139-140` explicitly makes
Recent cases "a local convenience list, **not search authority**".

## Governing docs

### Meets — the ticket's `refs`

`refs`: `docs/frd/frd-01-case-identity-and-lifecycle.md`,
`docs/frd/frd-12-operator-experience.md`.

| Requirement | Where | Met by |
| --- | --- | --- |
| "actionable receiving, requests, Triage, case, query, and exception queues" | `frd-12:10` | Steps 5–6 (the case list as a virtualized, openable queue) |
| "intake-evidence filters with exact options `All`, `Instructions`, and `Images`" | `frd-12:11-12` | Step 6 (the `kind` filter as a dropdown with exactly those three options, mapping to `null` / `instructions` / `images` on the wire, matching `Index.cshtml.cs:79-82`) |
| "clear counts that link to their exact filtered work and do not render stale zero placeholders" | `frd-12:13-14` | Step 8 (an empty result set is "no results for a search the operator ran", never a false zero — `screen-specs.md:425-427`) |
| "list/detail journeys for intake, source evidence, Triage, cases, documents, history, and exports" | `frd-12:15-16` | Step 6 (Enter / double-click opens the case, the entry contract [[FEAT-003]] consumes) |
| "loading, empty, current, stale, unavailable, partial, failed, validation, conflict, and access-denied states" | `frd-12:22-23` | Step 8 and step 9 (a fact per state; 503 renders "unavailable", distinct from empty) |
| "Every actionable search result is a full-row keyboard-focusable link or button with visible action affordance." | `frd-12:28` | Step 6 (row-level `AutomationId`s, arrow-key navigation, Enter opens) |
| "At constrained desktop width, a long Case/PO, Image Intake Reference, or U-reference moves to a labelled second line instead of overlapping the received timestamp." | `frd-12:28` | Step 6 (the [[DUI-007]] data-table item template; verified in the `winapp ui` run at step 10) |
| "keyboard, pointer, screen-reader, 200% zoom, forced-colour, and reduced-motion support" | `frd-12:24-25` | Steps 6–7 and step 10 (`Ctrl+K` / `Ctrl+F`, arrow keys, Enter; accessible sort state on header cells) |
| "Principal and reference are immutable after allocation." | `frd-01:33-38` (§ Case identity and lifecycle) | Step 5 — this slice is read-only; it exposes no control that could edit either, which is the strongest form of meeting the invariant |
| "Image-initiated Cases are searchable using their VRM reference or registration" | `frd-12:113-117` | Steps 3 and 6 (the `kind` filter and the Image Intake Reference search field, mirroring `LoadImageIntakeResultsAsync`, `Index.cshtml.cs:143-203`) |

### New ADR

The ticket carries `docs_todo: true` (confirmed in `get_doc_gates FEAT-002`).

> **New ADR** — ADR-0103 (gateway = evolved `Pegasus.Web`; never direct database
> access from a workstation) and ADR-0104 (online-required, bounded local cache
> only), authored by [[FND-005]] (plan handle `DSK-00-05`).
> This plan is written to the decisions as recorded in
> `docs/desktop/00-governance-and-workflow/README.md` § 3 and to L-01 in
> `docs/desktop/README.md` § Locked decisions; if either ADR lands differently
> this plan is revised before implementation. ADR-0104 binds step 6's column
> layout: **layout is cached locally, result data is not.**

ADR-0100 has more than one interested party through the no-split deviation
recorded in `docs/desktop/05-implementation-and-migration/README.md` § 3; it is
authored by [[FND-026]] (plan handle `DSK-02-01`); see [[FND-026]]'s plan for
the ownership reconciliation.

### Programme-level authorities that bind today

| Authority | Requirement | Met by |
| --- | --- | --- |
| L-01 (`docs/desktop/README.md` § Locked decisions) | Gateway is `Pegasus.Web` evolved in place | Steps 3–4 |
| L-02 (same) | The performance budget is measured on the local Test/UAT workstation, never an Azure test environment | Step 11 |
| L-04 (same) | Routing named on the ticket | § Routing below |
| `AGENTS.md` § Product invariants | Core owns business policy; duplicate business implementation is a stop condition | Steps 3–4 (one `ISearchCases` call) and the "no desktop-computed Due by" boundary |
| `docs/engineering.md` § Plan sizing | Diff estimate first, derived from the files document | First line |
| `docs/engineering.md` § Required evidence tiers | Tier 10: "Do not invent a release latency threshold without an explicit decision" | Step 11 uses the ticket's stated ≤ 1 s, which is the explicit decision |
| `docs/design/README.md:441-445` | Filters are dropdowns; tables sort newest first; headers are sort controls | Step 6 |
| `docs/design/README.md:745-757` | The twelve UI-07 search fields | Steps 3 and 6 |
| `docs/design/README.md:412-420` | Banned words — merge rule, not a CI check | Step 6 and the reviewer at `enter-review` |
| `docs/desktop/00-governance-and-workflow/README.md` § 3 | Six-question test answered with evidence | `research` § Execution placement |
| Plan 05 § 7 | `/api/v1` gated off returns 404; tests enable `Features:DesktopGateway` explicitly | Step 10 |
| Proposal §14.4, §14.7 | Virtualized server-paged list; global search grouped by kind | Steps 5–7 |
| Proposal §15.1 | Performance budgets | Step 11 |

## Routing

Copied from the ticket body's `## Routing` block.

- **Subagent**: `winui-dev` — `.codex/agents/winui-dev.toml`;
  `pegasus-gateway-dev` — `.codex/agents/pegasus-gateway-dev.toml` (query path
  and paging); `pegasus-test-engineer` — `.codex/agents/pegasus-test-engineer.toml`.
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `winui-dev-workflow`
  (`.codex/skills/winui-dev-workflow/SKILL.md`) → `winui-design`
  (`.codex/skills/winui-design/SKILL.md`) → `dotnet-webapi` (dotnet/skills
  `98f84851`, `plugins/dotnet-aspnetcore/skills/dotnet-webapi/SKILL.md`) →
  `optimizing-ef-core-queries` (dotnet/skills `98f84851`,
  `plugins/dotnet-data/skills/optimizing-ef-core-queries/SKILL.md`) →
  `run-tests` → `winui-code-review` at review.
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

These refine the ticket body's thirteen steps in the same order and with the
same ownership.

1. **Orient and take.** Read the plan row (`docs/desktop/05-implementation-and-migration/README.md`
   § 5, `DSK-05-02`), `vertical-slices.md` § S2, and `docs/design/README.md:441-445`
   plus the UI-07 field list at `:745-757`. Then `get_doc_gates FEAT-002` and
   `take_ticket` with branch `task/dsk-05-02-case-list`, worktree
   `../pegasus-worktrees/dsk-05-02-case-list`, from `origin/dev`.
2. **Confirm the recorded behaviour.** The `research` document carries the full
   filter table, `ResultsPerPage = 25` (`Index.cshtml.cs:19`), the absent sort
   (`:101-119`) and the 503 path (`:129-130`). Re-verify with
   `git diff --stat bbd1c549..HEAD -- src/Pegasus.Web/Pages/Cases/Index.cshtml.cs src/Pegasus.Web/Pages/Search/Index.cshtml.cs src/Pegasus.Core/Cases/CaseQueries.cs`;
   if the upstream sync moved any of them, re-read and update `research` with
   the new SHA before writing code. The recorded SHA is `bbd1c549`.
3. **Confirm and close the gateway contract** from [[GWY-007]] (plan handle
   `DSK-03-07`). Four specific checks, each from a research finding:
   - the endpoint accepts all **twelve** UI-07 filters, not the six named at
     `docs/desktop/03-gateway-api-and-data/endpoint-map.md:49` (assumption
     `A-05-04`);
   - `sort` maps onto `CaseSearchOrder`'s ten members
     (`src/Pegasus.Core/Cases/CaseQueries.cs:31-43`) and defaults to
     `ReceivedDesc`;
   - `ArgumentException` and `ArgumentOutOfRangeException` from
     `SearchCases.ExecuteAsync` (`CaseQueries.cs:186-211`) become **400 problems**,
     not 500s — precedent `src/Pegasus.Web/Mcp/AutomationMcpErrors.cs:53-59`;
   - global search is served by `/cases?q=` or by a `/search` route, and
     whichever it is, `parity-matrix.md:51`'s indicative `~GET /api/v1/search`
     is corrected on [[GWY-007]] if it disagrees (assumption `A-05-07`).
   Where the contract is short, extend the endpoint against the **same**
   `ISearchCases` call — one implementation only.
4. **Query review.** Load `optimizing-ef-core-queries` and review the gateway
   query path for N+1 and unbounded projections; the list must project only the
   columns the table renders. Record the outcome, and the decision on a total
   count (assumption `A-05-05`) and on a projected `Due by` (assumption
   `A-05-06`), in this document under a dated note.
5. **`CaseListViewModel`** in `src/Pegasus.Desktop`: incremental server paging,
   a `SortDescriptor` that issues a **server** sort and resets to page 1, filter
   selections bound to `ComboBox` item sources, and local re-ordering of **only
   the loaded page**. A filter change cancels the in-flight page request before
   issuing the new one.
6. **The list XAML** on the data-table pattern from [[DUI-007]] (plan handle
   `DSK-06-07`): 32 px rows, header cells that are sort controls exposing an
   accessible sort state, filters as `ComboBox`es (not pill rows), a column
   chooser persisted locally per user through the [[FND-031]] settings
   abstraction (**layout only — ADR-0104 forbids caching result data**),
   `ListView` virtualization, Enter or double-click to open. AutomationIds are
   fixed at `docs/desktop/06-ui-design/screen-specs.md:176-177`: `Cases.Search`,
   `Cases.List.Table`, `Cases.List.Filter.<Name>`, `Cases.List.Row.<Ref>`,
   `Cases.New`. Stage and Type render through `OperatorLabels.CaseStage` /
   `CaseTypeName` (`src/Pegasus.Web/Presentation/OperatorLabels.cs:101-137`)
   vocabulary — never an enum name, never colour alone.
7. **Global search.** `Ctrl+K` focuses the title-area search slot from
   [[DUI-004]] (plan handle `DSK-06-04`); `Ctrl+F` is the in-list alias
   (`screen-specs.md:175`). Results are grouped by case / party / vehicle /
   document metadata **as the gateway supports** and are keyboard traversable.
   Search queries the gateway; it never downloads the dataset. Recent items are
   a local convenience only and are labelled as such, never presented as search
   authority (`screen-specs.md:139-140`).
8. **States.** Loading, empty and error per proposal §14.4 and the state
   contract at `docs/design/README.md:764-772`. Three distinctions matter and
   each has a fact in step 9: an empty *result set* for a search the operator
   ran shows "No results"; an absent section shows nothing; a 503 from the
   gateway shows **unavailable**, retaining the last-good page. The error state
   uses the `InfoBar` problem presentation from [[DUI-010]] (plan handle
   `DSK-06-10`) with a copyable Reference — not a modal.
9. **View-model tests** in `tests/Pegasus.Desktop.ViewModelTests` (project from
   [[FND-038]], plan handle `DSK-02-13`): first page, next page, end of set
   (`HasNextPage == false`); sort toggling issuing a server request and
   resetting to page 1; filter change resetting to page 1; cancellation of an
   in-flight page when the filter changes; empty vs unavailable rendered
   distinctly; column-layout persistence round-trip.
10. **Contract tests** in `tests/Pegasus.Api.ContractTests`: paging (first,
    middle, past-the-end), sort in both directions for each of the five sortable
    columns, one theory per filter, 401 without a token, 403 for an actor
    without `PerformCasework`, `If-None-Match` → 304, and **400 problems for the
    six Core input bounds** (`CaseQueries.cs:186-211`). Enable
    `Features:DesktopGateway` explicitly in the factory — a gated-off endpoint
    returns 404 and otherwise reads as a routing bug.
11. **Operator step — measure the performance budget** on the baseline Test/UAT
    workstation described in `docs/desktop/08-testing/test-uat-stack.md`. Time to
    first page of ordinary results must be **≤ 1 s**, excluding provider outage.
    Record cold and warm figures **and the hardware description**; the operator,
    or the `pegasus-ui-verifier` run on the real workstation, is the only
    acceptable source. A synthetic or developer-laptop figure does not satisfy
    tier 10.
12. **Parity comparison.** Apply the same filters on the web Cases page and on
    the desktop list against the same database; result sets and ordering must be
    identical. Build the comparison from the fixtures behind
    `tests/Pegasus.IntegrationTests/CasesIndexWebTests.cs` so a disagreement is
    attributable to a filter rather than to a dataset. Record it in `proof`.
13. **Documentation and PR.** Update `docs/desktop/01-inventory-and-parity/parity-matrix.md`
    rows `PAR-06` (`:51`) and `PAR-07` (`:52`); add the list/search section to
    `docs/frd/frd-13-desktop-operator-experience.md` and a `DSK` row to
    `docs/capabilities.md`; run the simplification pass over the branch diff
    (`AGENTS.md` step 4) and record it under a dated `## Simplification pass`
    heading here; then open the PR into `dev`.

## Verification

Evidence tiers from the body: **tier 5** (Web/API/MCP caller), **tier 7**
(Browser/accessibility), **tier 10** (Performance/concurrency).

| Command | Expected | Evidence captured |
| --- | --- | --- |
| `dotnet build ./Pegasus.slnx --configuration Release --no-restore` | Succeeds under `TreatWarningsAsErrors=true` with no `WUI*` suppression | Build log tail |
| `dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release --no-build` | Paging, sort, filter, cancellation and state facts pass | Test summary |
| `dotnet test ./tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj --configuration Release --no-build` | Paging / sort / per-filter / 401 / 403 / 304 / six-bound-400 facts pass | Test summary — **tier 5 evidence** |
| `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --no-build --filter "Category!=Corpus&Category!=Browser"` | `CasesIndexWebTests` unchanged and green | Test summary (proves the web path was not disturbed) |
| `pwsh ./tests/Pegasus.Desktop.UITests/ui-tests.ps1 -Script case-list` | Keyboard traversal and open-by-Enter pass with no sleep-based waits | Results JSON + screenshot — **tier 7 evidence** |
| Performance run on the baseline workstation | First page of ordinary results ≤ 1 s, cold and warm, provider outage excluded | The figures **plus the workstation specification** in `proof` — **tier 10 evidence**; a developer-laptop figure does not satisfy it |
| Parity table on the Test/UAT stack | Identical result sets and ordering versus the web page for each filter combination | The table itself, in `proof` |

## Risks / open questions

- **Risk: the gateway serves only six filters.** `endpoint-map.md:49` names six;
  the page has thirteen and the authority twelve. *Mitigation:* step 3 checks it
  first and extends the endpoint against the same `ISearchCases` call. The gap,
  if it exists, is a defect **owned by [[GWY-007]]** (plan handle `DSK-03-07`) —
  a scope boundary raised from here, not an open question.
- **Risk: Core's input refusals arrive as 500s.** `SearchCases.ExecuteAsync`
  throws `ArgumentException`/`ArgumentOutOfRangeException` for six distinct bad
  inputs (`CaseQueries.cs:186-211`), and the web turns them into model-state
  errors, not HTTP codes. *Mitigation:* step 3's third check and the six 400
  facts in step 10.
- **Risk: local sort silently disagrees with the server.** Core sorts the whole
  set; the desktop holds 25 rows. *Mitigation:* step 5 makes a header click a
  **server** sort; local re-ordering is confined to the loaded page and is never
  presented as a sort of the whole result.
- **Risk: `Due by` computed on the desktop.** `CaseSearchItem` carries
  `NextChaseAtUtc`, not a due date (`CaseQueries.cs:52-67`). *Mitigation:*
  step 4 records the decision — either [[GWY-007]] projects it or the column is
  omitted with a reason. Deriving it client-side is a stop condition.
- **Risk: the perf figure is taken on the wrong machine.** Tier 10 obliges a
  measurement "against the stated concurrency and volume assumptions".
  *Mitigation:* step 11 records the hardware description alongside the figure,
  and the operator label `needs-operator` on this ticket exists for exactly this
  step.
- **Risk: parity drift.** *Mitigation:* step 2 re-checks the three source files
  against `bbd1c549` before code is written and records the SHA actually
  characterized.
- **Scope boundary: the vehicle-images workspace.** The `kind=images` filter and
  its outcome label are parity here; the Unidentified and Vehicle-images
  list/detail screens belong to [[FEAT-012]] (plan handle `DSK-05-12`).
- **Scope boundary: create.** `Ctrl+N` occupies the primary-command slot; the
  create flow itself is [[FEAT-004]] (plan handle `DSK-05-04`).
- **Not an open question: the operator decisions are settled.** D-002, D-003 and
  D-004 do not touch this ticket, which performs no Azure write and ships no
  package.

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over
this branch's own diff before the PR, recorded here under a dated heading._
