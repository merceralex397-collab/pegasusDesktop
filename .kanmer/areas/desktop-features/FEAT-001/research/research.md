# Research — FEAT-001: the Dashboard and rail counts as one query contract

## Question

What does the web dashboard actually surface today, where do the rail badge
figures come from, and what must `GET /api/v1/dashboard` and
`GET /api/v1/dashboard/rail-counts` carry so the native Dashboard answers the
five §14.3 questions with counts that equal the web's for the same dataset?

## Current behaviour

The dashboard is `src/Pegasus.Web/Pages/Index.cshtml.cs` (43 lines, verified
`wc -l`). `IndexModel` is constructed with one dependency,
`IGetOperationsSnapshot` (`:13`), and its single handler `OnGetAsync`
(`:27-42`) resolves the staff actor through `StaffPageModel.TryGetActor`
(`:29`, defined at `src/Pegasus.Web/Pages/StaffPageModel.cs:11-15`), returns
`Forbid()` when it cannot (`:31`), and copies six members off the snapshot:

| Page member | Line | Type | Snapshot source |
| --- | --- | --- | --- |
| `Counts` | `:15` | `IntakeQueueCounts` | `snapshot.Intake` (`:36`) |
| `DueWork` | `:17` | `IReadOnlyList<CaseDueWork>` | `snapshot.DueWork` (`:37`) |
| `CaseStages` | `:19` | `CaseStageCounts` | `snapshot.CaseStages` (`:38`) |
| `CaseActivity` | `:21` | `CaseActivityCounts` | `snapshot.CaseActivity` (`:39`) |
| `MailActivity` | `:23` | `MailActivityCounts` | `snapshot.MailActivity` (`:40`) |
| `LoadedAtUtc` | `:25` | `DateTimeOffset` | `snapshot.AsOfUtc` (`:35`) |

Rail badges do not come from the page at all. `src/Pegasus.Web/Presentation/RailCountsPageFilter.cs`
(51 lines) is a global `IAsyncPageFilter` registered in
`src/Pegasus.Web/Program.cs:261` (`.AddMvcOptions(options => options.Filters.Add<…RailCountsPageFilter>())`).
On every authenticated request it calls `IDashboardQueries.GetCaseStageCountsAsync`
and writes a one-entry dictionary into `ViewData["RailCounts"]` (`:43-46`)
holding only `["Queues"] = counts.NotReady + counts.Review + counts.Held`
(`:45`). Its own remarks (`:13-20`) state the rule the desktop must preserve:
Inbox and Cases "have no established figure to reuse without inventing one, so
they are left absent from the dictionary — the layout already renders nothing
for a missing key, never a stale zero."

Parity matrix row: **`PAR-05`** ("13.2 Dashboard and work queues", FRD-12 owner,
current entry point `Index.cshtml.cs` (43) — `OnGetAsync`, status `inventoried`)
at `docs/desktop/01-inventory-and-parity/parity-matrix.md:50`. The matrix holds
46 `PAR-` rows (`grep -c '^| PAR-' docs/desktop/01-inventory-and-parity/parity-matrix.md`
→ `46`), all keyed to page models under `src/Pegasus.Web/Pages/**`. `PAR-06`
(`:51`) covers the retired `Search/Index` redirect and belongs to
[[FEAT-002]] (plan handle `DSK-05-02`), not to this ticket.

## Findings

### Facts

Verified by reading the fork at `HEAD` `bbd1c549` (2026-08-24). `git diff --stat 191ddf33..HEAD -- src tests`
is empty, so every line reference in the plan set — taken at the planning
baseline `191ddf33` — still holds unchanged at `bbd1c549`. **This is the
revision characterized** (ticket step 3, parity-drift trap).

- The snapshot carries **seven** members, and the page surfaces **six**.
  `OperationsSnapshot` (`src/Pegasus.Core/Operations/OperationsSnapshot.cs:45-52`)
  is `(AsOfUtc, Intake, TriageCount, DueWork, CaseStages, CaseActivity, MailActivity)`.
  `Index.cshtml.cs:34-40` never reads `TriageCount`; the query runs and the
  figure is discarded.
  - Consequence: the ticket body's six-member list (step 3) is exactly the web
    surface, and it is correct as parity. `TriageCount` is a seventh figure the
    same use case already computes at no extra cost — the Queues rail entry is
    the only place it could belong, and today the rail's Queues badge uses the
    *case-stage* sum instead. Do not silently add it to a tile.
- The single Core entry point is `GetOperationsSnapshot.ExecuteAsync`
  (`OperationsSnapshot.cs:61-124`). It composes five reads —
  `IIntakeReceiptQueries.GetCountsAsync`, `IListTriage.ExecuteAsync` (page 1,
  size 1, for the total only), `ICaseDueWorkQueries.GetDueAsync`,
  `IDashboardQueries.GetCaseStageCountsAsync`, `GetCaseActivityCountsAsync`,
  `GetMailActivityCountsAsync` — and bounds due work at
  `MaximumDueWork = 20` (`:68`, applied at `:107`).
- **The authorization right is `PerformCasework`, not `AccessStaffApplication`.**
  `OperationsSnapshot.cs:96` is `StaffAuthorization.Require(actor, StaffAccessRight.PerformCasework);`.
  `docs/desktop/03-gateway-api-and-data/endpoint-map.md:43-44` records
  `AccessStaffApplication` for both dashboard rows.
  - `PerformCasework` is strictly narrower in one direction and wider in
    another: `StaffAuthorization.IsAuthorized`
    (`src/Pegasus.Core/Identity/StaffAuthorization.cs:33-56`) grants
    `AccessStaffApplication` to `ActorKind.Staff` only, and `PerformCasework`
    to `ActorKind.Staff` **or** `ActorKind.Automation`. An endpoint filter that
    checks only `AccessStaffApplication` would admit a staff actor whom Core
    then refuses — a 500-shaped `StaffAuthorizationException` instead of a 403.
- Day and week boundaries are the office's, not UTC's.
  `OfficeTimeZoneId = "Europe/London"` (`:78`); `OfficeBoundaries` (`:137-159`)
  starts the week on Monday and falls back to UTC when the platform carries no
  IANA database. `LoadedAtUtc` is UTC and the operator surface must render it
  through the Europe/London vocabulary map.
- `IDashboardQueries` (`src/Pegasus.Core/Operations/DashboardCounts.cs:55-67`)
  has exactly three methods: `GetCaseStageCountsAsync` (`:57`),
  `GetCaseActivityCountsAsync` (`:59`), `GetMailActivityCountsAsync` (`:64`).
  Its doc comment (`:50-53`) states the rule the tiles inherit: "Every member
  returns a real number or the tile that would have shown it is not rendered —
  there is no placeholder value."
- `MailActivityCounts` (`DashboardCounts.cs:45-51`) carries a compatibility
  alias: `Unidentified` is an `init` property defaulting to `NeedsSorting`
  (`:48`). The design authority's banned/settled vocabulary makes `Unidentified`
  the operator word; the wire DTO must carry `unidentified` and must not carry
  `needsSorting`.
- `CaseStageCounts` is `(NotReady, Review, Held)` (`:18`); `CaseActivityCounts`
  is `(NewCasesToday, SentToEngineerToday, SentToEngineerThisWeek, ReportsSentToday, ReportsSentThisWeek)`
  (`:30-34`). Together with `MailActivityCounts.ReceivedToday`/`Unidentified`
  and the `Blocked` figure inside `IntakeQueueCounts`, these are precisely the
  metric tiles the screen spec lists at
  `docs/desktop/06-ui-design/screen-specs.md:131-137`.
- **None of the target projects exists yet.** `Pegasus.slnx` lists four
  production projects (`Pegasus.Core`, `Pegasus.Infrastructure`,
  `Pegasus.Web`, `Pegasus.Worker`) and three test projects
  (`ArchitectureTests`, `Core.Tests`, `IntegrationTests`). `src/Pegasus.Desktop`,
  `src/Pegasus.Desktop.Infrastructure`, `src/Pegasus.Contracts`,
  `tests/Pegasus.Desktop.ViewModelTests`, `tests/Pegasus.Api.ContractTests` and
  `tests/Pegasus.Desktop.UITests` are all created by earlier tickets (see the
  files document).
- **`Features:DesktopGateway` does not exist in code today.**
  `grep -rn "DesktopGateway" src/ tests/` returns nothing; every hit is in
  `docs/desktop/`. The gate is introduced by [[GWY-002]] (plan handle
  `DSK-03-02`). The three flags that do exist are `Features:SendToAi`
  (`src/Pegasus.Web/AiWork/SendToAi.cs:12`), `Features:AutomationMcp`
  (`src/Pegasus.Web/Mcp/AutomationMcp.cs:12`) and `Features:LocalIntake` /
  `Features:LocalDocumentCustody` (`src/Pegasus.Web/Program.cs:112`, `:202`) —
  the last two are refused outside the `DevelopmentOffline` runtime profile.
- Existing test evidence, located by `ls tests/Pegasus.IntegrationTests`:
  `tests/Pegasus.IntegrationTests/DashboardCountersWebTests.cs` (74 lines),
  `tests/Pegasus.IntegrationTests/RailCountsWebTests.cs` (121 lines),
  `tests/Pegasus.Core.Tests/Operations/DashboardBoundaryTests.cs` (141 lines),
  `tests/Pegasus.IntegrationTests/Browser/OperatorJourneyTests.cs` (612 lines).
  The first two are more precise than the plan set's citation of
  `OperatorJourneyTests` alone and are the parity oracles this slice compares
  against.
- The design authority's four hard rules live at `docs/design/README.md:422-445`,
  the banned-word list at `:412-420` (`intake`, `bounded`, `projection`,
  `lease`, `opaque`, `ingress`, `composed`, `artifact`, `durable`, `aggregate`,
  `caller`, `correlation identifier`, `bytes`), and the closed necessary-copy
  list at `:400-409`. `:417-420` states plainly that nothing in CI enforces the
  ban — it is a review rule, so the reviewer is the only gate.
- `docs/frd/frd-12-operator-experience.md:93-99` ("Dashboard freshness and
  reconciliation") already requires exactly the freshness contract this ticket
  builds: "`0`, loading, current, stale-with-last-good-time, partial,
  unavailable, and failed are distinct outcomes. A refresh never replaces a
  last-good value with a false zero…". `:107` fixes `New cases today` to the
  Europe/London calendar day and excludes Image-initiated cases, Triage,
  Unidentified and Blocked intake.

### Assumptions

- **`A-05-01` — [[GWY-006]] (plan handle `DSK-03-06`) will expose both routes
  under `/api/v1/dashboard` and `/api/v1/dashboard/rail-counts`.** The ticket
  body and `endpoint-map.md:43-44` both say `dashboard/rail-counts`; the parity
  matrix row `PAR-05` (`:50`) writes the indicative form `~GET /api/v1/rail-counts`.
  Confirmed by: reading [[GWY-006]]'s delivered route table before step 4.
  Breaks if wrong: the client generated by [[GWY-005]] (plan handle `DSK-03-05`)
  binds a method name that does not match, and the parity-matrix API column
  needs correcting instead of the desktop.
- **`A-05-02` — the gateway will re-run `GetOperationsSnapshot` per request
  rather than caching it.** `RailCountsPageFilter`'s remarks (`:21-26`) argue
  the stage-count query is "a single grouped aggregate query with no row
  projection… so running it once more per request from the shell stays cheap",
  which is the only measured statement in the repository about this cost.
  Confirmed by: the perf figures [[FEAT-002]] records on the same stack.
  Breaks if wrong: coalesced refresh on the desktop is not enough and the
  gateway needs a short server-side cache with its own freshness field —
  a change to [[GWY-006]], not to this ticket.
- **`A-05-03` — the weak `ETag` for `/dashboard` can be derived from
  `AsOfUtc`.** `AsOfUtc` comes from `TimeProvider.GetUtcNow()` (`:98`), so it
  changes on every call and an `ETag` derived from it never matches. The ETag
  must therefore hash the *payload*, not the timestamp. Confirmed by: a
  contract fact that two calls with unchanged data return the same `ETag`.
  Breaks if wrong: `If-None-Match` never yields 304 and the ticket's fourth
  contract fact (step 11) cannot pass.

## Execution placement

The six-question cloud-justification test from
`docs/desktop/00-governance-and-workflow/README.md` § 3, answered for the
dashboard responsibility.

| Question | Answer | Evidence |
| --- | --- | --- |
| Shared authority — must several users see and update the same state? | **Yes** | The counts are office-wide queues over shared rows: `GetCaseStageCountsAsync` aggregates every case, not the viewer's (`DashboardCounts.cs:57`); `FRD-12:5-7` calls it "an authenticated office-wide dashboard". Lands in the gateway (`Pegasus.Web`, L-01). |
| Unattended execution — must it run with every desktop closed? | **No** | The dashboard is a read on demand. Nothing schedules it; the Worker's nine functions (`reuse-map.md` § Pegasus.Worker) contain no dashboard job. |
| Protected credentials — long-lived secret that must not sit on workstations? | **No** | The read path touches SQL only, through `IDashboardQueries` → `EfDashboardQueries` (`src/Pegasus.Infrastructure/DependencyInjection.cs:244`). The connection string is a gateway concern because of question 5, not because the dashboard needs a secret of its own. |
| Public callback — must an external service call a stable public endpoint? | **No** | No external party calls the dashboard. |
| Central enforcement — revocation, permissions, audit, invariant independent of the client? | **Yes** | `StaffAuthorization.Require(actor, StaffAccessRight.PerformCasework)` at `OperationsSnapshot.cs:96` is a Core boundary that must hold whatever the client is; ADR-0103 forbids workstation database access. Lands in the gateway. |
| Measured operational advantage — measured evidence central is materially better? | **n/a** | No measurement exists either way, and none is needed: questions 1 and 5 already place the query. Answering "yes" without a figure would be the invention this test exists to catch. |

**Placement:** the desktop renders, sorts and refreshes; the gateway
(`Pegasus.Web` evolved in place, L-01) queries, authorizes and shapes. Two
"yes" answers, both naming the gateway as the host — not a new service, not a
new Azure resource. No Azure write anywhere in this ticket.

## Implications

- **Rail counts must stay a sparse map, not a fixed record.** Preserving
  "absent renders nothing, never a zero" (`RailCountsPageFilter.cs:13-20`) means
  the wire shape has to be able to omit an entry. A DTO of three non-nullable
  `int`s would force a zero for Inbox and Cases and silently break the one
  behaviour the ticket's step 8 names. Nullable members or a keyed map both
  work; nullable members serialize more predictably through the generated
  client and are the recommendation.
- **The gateway endpoint filter must check `PerformCasework`.** Fact 3 above
  makes the `AccessStaffApplication` row in `endpoint-map.md:43-44` wrong for
  the dashboard pair. This is a defect in the endpoint map, owned by
  [[GWY-006]]; this ticket's step 4 must confirm the filter matches Core, and
  the correction belongs in that ticket's documentation change, not silently in
  this one's code.
- **The `ETag` must hash the payload.** See `A-05-03`. State this in the
  gateway contract review so the 304 fact is achievable.
- **`TriageCount` is present and unused.** It is not parity to surface it and
  not parity to remove the query. Leave the behaviour as it is and record the
  observation; adding a Triage tile would be new scope under proposal §13.11's
  discipline and belongs to [[FEAT-011]] (plan handle `DSK-05-11`) or the Queues
  screen, not here.
- **Coalescing is a real requirement, not a nicety.** The web re-runs the
  stage-count query on *every authenticated request* through the page filter.
  A desktop that fires a second `/dashboard` while one is in flight would be
  strictly worse than the page it replaces on the same database. The ticket's
  step 6 "a second refresh joins the first, it does not queue" is the correct
  shape.
- **Parity is testable against two existing web tests, not one.**
  `DashboardCountersWebTests.cs` and `RailCountsWebTests.cs` are the oracles;
  the parity table in step 13 should be built from the same fixtures they use so
  a disagreement is attributable.

## Open questions

None that block the plan. The three assumptions above are each settled by a
step in the plan (`A-05-01` and `A-05-02` at step 3, `A-05-03` at step 8), and
the `PerformCasework` correction is a named hand-off to [[GWY-006]] recorded in
the plan's *Risks / open questions* section — a scope boundary a sibling ticket
owns, not an unresolved question. No `open-questions` document is created.
