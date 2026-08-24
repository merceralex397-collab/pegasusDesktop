# Research — FEAT-003: the case workspace read path and its history

## Question

What does the web case page actually load on open, how is that composed in
Core, and what must the gateway's header plus per-section reads look like so a
lazily loaded eight-tab native workspace shows the same fields and the same
history rows as the web for the same case?

## Current behaviour

`src/Pegasus.Web/Pages/Cases/Details.cshtml.cs` (654 lines, verified `wc -l`),
with `Details.cshtml` (254 lines) and four partials under
`src/Pegasus.Web/Pages/Cases/Shared/` — `_CaseSummary.cshtml` (237),
`_CaseWorkflow.cshtml` (551), `_CaseDocuments.cshtml` (158),
`_CaseHistory.cshtml` (53).

`DetailsModel` takes eleven dependencies (`:18-29`), of which `IGetCase` is the
read; the other ten are the edit and lease commands that belong to
[[FEAT-005]] (plan handle `DSK-05-05`) and to [[FEAT-006]] (plan handle
`DSK-05-06`). `OnGetAsync` (`:110-154`) does the whole read:

1. resolve the actor, `Forbid()` if not (`:112-115`); `Guid.Empty` → `NotFound()`
   (`:116-119`);
2. `getCase.ExecuteAsync(new(id, actor), …)`; `null` → `NotFound()` (`:123-127`);
3. `imageIntakeQueries.ListForCaseAsync` and
   `caseEvidenceImageQueries.ListForCaseAsync` (`:128-129`) — **always**, on
   every tab;
4. only when `Tab == "evidence"`, a per-intake image list (`:130-140`);
5. `RestoreLeaseState`, `RestoreProposedValues`,
   `DescribeEditAuthorityHolderAsync` (`:141-143`) — the `TempData` machinery;
6. any unexpected exception → `QueryFailed` and **HTTP 503** (`:146-152`).

**The web has three tabs, not eight.** `TabFilter` binds from the query string
(`:55-56`) and `Tab` (`:58-64`) collapses to exactly `overview`, `evidence` or
`history`. The remarks at `:47-54` explain why: "the tab is in the query string
and the panels are server-rendered, so the screen works with no script and
every section is linkable."

Parity matrix row: **`PAR-08`** (13.3 Case lifecycle, FRD-01,
`Cases/Details.cshtml.cs` (654) with its six handlers, status `inventoried`) at
`docs/desktop/01-inventory-and-parity/parity-matrix.md:53`. This ticket owns the
**read path only**; the five `OnPost*` handlers named in the same row belong to
[[FEAT-005]]. The matrix holds 46 `PAR-` rows (`grep -c '^| PAR-' …` → `46`),
all keyed to page models under `src/Pegasus.Web/Pages/**`.

## Findings

### Facts

Verified at `HEAD` `bbd1c549` (2026-08-24). `git diff --stat 191ddf33..HEAD -- src tests`
is empty, so the plan set's line references still hold. **`bbd1c549` is the
revision characterized.**

- **The web loads everything on every case open.** `GetCase`
  (`src/Pegasus.Core/Cases/CaseQueries.cs:264-357`) composes **eight** ports in
  one call: `ICaseQueryStore`, `ICaseDataQueries`, `IVehicleEvidenceQueries`,
  `IEvaHandoffQueries`, `ICaseCustodyQueries`, `ICaseDueChaserQueries`,
  `ICaseTaskQueries`, `IStaffAccountQueries` (`:264-272`). It requires
  `StaffAccessRight.PerformCasework` (`:295`) and refuses `Guid.Empty` (`:297-300`).
  - Consequence, and it is the central one for this ticket: **lazy per-tab
    loading is a deliberate difference, not a port.** `CaseDetails`
    (`CaseQueries.cs:108-131`) is one composed record carrying `Summary`,
    `Workflow`, `ActiveEditLease`, `Documents`, `CustodyFolderRemoteId`,
    `CustodyState`, `RequestUploadLinks`, `AvailableReportSentEvidence`,
    `History`, plus `Data`, `Tasks`, `LatestChaser`, `VehicleEvidence`,
    `EvaHandoff`, `Custody` and `ReportApprovedByDisplayName` as `init`
    members. Splitting it into independently `ETag`ged section endpoints is
    work [[GWY-007]] (plan handle `DSK-03-07`) must do; the desktop cannot do
    it, and calling `GET /cases/{id}` per tab would be strictly worse than the
    web page.
- **`GetCase` performs a cross-case integrity check and throws**
  `InvalidDataException("A composed case projection belongs to another case.")`
  when any composed part carries a different `CaseId` (`:316-322`), and
  `InvalidDataException("The accepted case is missing its typed data projection.")`
  when the typed data is absent (`:308-309`). A per-section gateway read that
  no longer composes the parts together loses this check unless each section
  endpoint re-asserts its own case id.
- **History actor names are resolved in Core, not in the page.**
  `CaseHistoryEntry` (`CaseQueries.cs:88-105`) is
  `(EventType, Actor, ActorKind, OccurredAtUtc, Reason, BeforeVersion, AfterVersion)`
  with an `init` property `ActorDisplayName` defaulting to
  `ActorDisplayNames.UnknownStaff` (`:104`). Its doc comment (`:97-103`) says the
  default exists "so a caller that forgets to populate it never renders the raw
  subject id". `GetCase` populates it from `IStaffAccountQueries` (`:325-332`).
  The row also carries `BeforeVersion` / `AfterVersion` — integers the screen
  spec forbids on an operator surface.
- **The eight-tab sub-navigation is the screen spec's, and it is new.**
  `docs/desktop/06-ui-design/screen-specs.md:180-183` fixes the order
  Overview · Vehicle · Assessment · Documents · Communications · Tasks ·
  Reports · History, against the web's three. `:206-216` describes each tab's
  contents, `:224` gives the keyboard map `Ctrl+1..8` / `Ctrl+S` / `Esc` /
  `Ctrl+W`, and `:225-227` fixes the AutomationIds `Case.Header.<Field>`,
  `Case.Actions.<Action>`, `Case.Tabs.<Tab>`, `Case.<Tab>.<Section>.<Element>`,
  `Case.Lease.Enter`, `Case.Lease.Renew`, `Case.Lease.Leave`.
- **The endpoint map already names the split.**
  `docs/desktop/03-gateway-api-and-data/endpoint-map.md:51` records
  `GET /cases/{id}` returning "case header + overview section" with
  `ETag + version`, and `:52` records
  `GET /cases/{id}/vehicle|/assessment|/documents|/communications|/tasks|/reports|/history`
  with "ETag per section" and a Phase column of "3 (history), 4–7 (others)".
  Auth right `PerformCasework` on both — matching `CaseQueries.cs:295` exactly.
  `:130` records the audit read: `GET /audit?actor&case&from&to&page`, right
  `ManageStaffAccounts` (full) / `PerformCasework` (own case history).
- **The Queries classification is a real gap in the current read.**
  `MailOperationalDestination` (`src/Pegasus.Core/Intake/Classification/MailOperationalDestinationPolicy.cs:7-15`;
  the enum members occupy `:8-15`, with `Queries` at `:10`) is the canonical
  classification. `MailOperationalDestinationResult` (`:17-22`) carries
  `Classification`, `PolicyKey` and `PolicyVersion` — the latter two must never
  reach an operator surface. The communications payload that carries this field
  is delivered by [[FEAT-037]] (plan handle `DSK-07-11`), whose step 9 and
  acceptance criterion both name it; this ticket **renders** it and adds no
  second read.
- **`CaseEvidenceImage` and `ImageIntakeSummary` are loaded unconditionally**
  by the web page (`Details.cshtml.cs:128-129`) even on the History tab. That
  is a cost the desktop's lazy loading removes; it is not behaviour to preserve.
- **`ProposedValues`, `ProposedValuesWereDropped`, `ProposedValuesWereShortened`,
  `LeaseToken`, the three operation keys and `CanRecoverLease`
  (`Details.cshtml.cs:78-105`) are all web mechanics.** They come from
  `CaseMutationPageModel`'s cookie `TempData` state machine
  (`src/Pegasus.Web/Pages/Cases/CaseMutationPageModel.cs:20-30`, budgets at
  `:38-39`, allow-list at `:46-88`). None of them is read-path behaviour and
  none is carried over here; the recovery experience is [[FEAT-008]] (plan
  handle `DSK-05-08`) and the lease session is [[FEAT-005]].
- **The 503 path is deliberate.** `Details.cshtml.cs:146-152` logs and returns
  503 rather than a blank page. The state contract at
  `docs/design/README.md:764-772` distinguishes "unavailable" from "empty"; the
  desktop must too.
- Existing test evidence, located by `ls tests/Pegasus.IntegrationTests`:
  `tests/Pegasus.IntegrationTests/CaseDetailsWebTests.cs` (1,286 lines) — the
  named oracle; `CaseEditModeWebTests.cs` (126) and
  `CaseWorkflowPersistenceTests.cs` (2,194) cover the edit half and belong to
  [[FEAT-005]]; `Browser/OperatorJourneyTests.cs` (612).
- **Target projects do not exist yet.** `Pegasus.slnx` lists four production and
  three test projects. `grep -rn "DesktopGateway" src/ tests/` returns nothing —
  the gate is introduced by [[GWY-002]] (plan handle `DSK-03-02`).

### Assumptions

- **`A-05-08` — [[GWY-007]] (plan handle `DSK-03-07`) will split `CaseDetails`
  into a header/overview read plus seven independently `ETag`ged section
  reads.** `endpoint-map.md:51-52` says so; `GetCase` does not do it today
  (`CaseQueries.cs:264-357`). Confirmed by: reading [[GWY-007]]'s delivered
  route list and response shapes at step 3. Breaks if wrong: lazy loading is
  impossible, every tab activation re-fetches the whole composed record, and the
  ≤ 200 ms navigation budget at step 12 cannot be met — the fix belongs to
  [[GWY-007]], not to a desktop cache.
- **`A-05-09` — the section split preserves `GetCase`'s cross-case integrity
  check.** `GetCase` throws when a composed part belongs to another case
  (`CaseQueries.cs:316-322`). Confirmed by: a contract fact per section
  asserting the returned payload's case id. Breaks if wrong: a section endpoint
  could serve another case's rows and nothing would notice — the desktop cannot
  detect it, so this must be a gateway fact.
- **`A-05-10` — `GET /cases/{id}/history` is paged.** The endpoint map's audit
  row (`:130`) has a `page` parameter; the section row (`:52`) does not name
  one, and `CaseDetails.History` is an unbounded `IReadOnlyList<CaseHistoryEntry>`
  today. Confirmed by: [[GWY-007]]'s delivered history shape. Breaks if wrong:
  a long-lived case returns an unbounded payload and the History tab has no
  paging control to build — the ticket's step 6 requires "newest first, paged".
- **`A-05-11` — the communications payload from [[FEAT-037]] (plan handle
  `DSK-07-11`) will have landed before this tab renders the Queries group.**
  [[FEAT-037]] is Phase 5 and this ticket is Phase 3. Confirmed by: checking
  whether `GET /api/v1/cases/{caseId}/communications` carries the classification
  field before step 7. Breaks if wrong: the Communications tab ships without the
  Queries group and step 7 is deferred with a recorded reason — the group is not
  fabricated from a second read, which the ticket's Guardrails forbid.

## Execution placement

The six-question test from `docs/desktop/00-governance-and-workflow/README.md`
§ 3, answered for the case-read responsibility.

| Question | Answer | Evidence |
| --- | --- | --- |
| Shared authority — must several users see and update the same state? | **Yes** | The case is shared state; `CaseDetails.ActiveEditLease` (`CaseQueries.cs:111`) and `Workflow.Version` (`CaseWorkflowContracts.cs:112`) exist precisely because several operators read and edit the same record. Lands in the gateway (`Pegasus.Web`, L-01). |
| Unattended execution — must it run with every desktop closed? | **No** | A read on demand. The Worker's nine functions include no case-detail read (`reuse-map.md` § Pegasus.Worker). |
| Protected credentials — long-lived secret that must not sit on workstations? | **No** for the read path itself; **yes** for two tabs it will later host | The header/overview/history reads touch SQL only. Documents and Communications reach Box and Graph, whose credentials stay central under ADR-0107 and ADR-0106 — but those tab payloads are delivered by [[FEAT-014]] (plan handle `DSK-05-14`) and [[FEAT-037]] (plan handle `DSK-07-11`), not here. This ticket places no credential anywhere. |
| Public callback — must an external service call a stable public endpoint? | **No** | No external party reads a case. |
| Central enforcement — revocation, permissions, audit, invariant independent of the client? | **Yes** | `StaffAuthorization.Require(query.Actor, StaffAccessRight.PerformCasework)` at `CaseQueries.cs:295`, plus the cross-case integrity check at `:316-322` and the history actor-name resolution at `:325-332`, must hold whatever the client is. ADR-0103 forbids workstation database access. Lands in the gateway. |
| Measured operational advantage — measured evidence central is materially better? | **n/a** | No measurement exists either way, and none is needed: questions 1 and 5 already place the read. This ticket does measure something — the ≤ 200 ms cached-navigation budget at step 12 — but that measures the *desktop's* rendering after the header lands, not whether the query should be central. |

**Placement:** the gateway serves the header and each section, authorizes and
resolves display names; the desktop renders, caches by `ETag` for the lifetime
of the open case and revalidates on manual refresh. Two "yes" answers, both
naming the gateway. No Azure resource is involved and no Azure write occurs.

## Implications

- **The whole ticket depends on the gateway splitting the composed record.**
  Every other implication follows from `A-05-08`. Step 3 is therefore not a
  formality; if [[GWY-007]] has not split `CaseDetails`, this slice stops and
  raises it rather than caching a whole-record read per tab.
- **Eight tabs against the web's three is a deliberate difference to record.**
  It is not a parity gap. The web's Evidence tab collapses Documents, vehicle
  images and evidence photographs into one panel; the desktop's Documents,
  Vehicle and Reports tabs separate them. Record this in the parity row's
  "known deliberate difference" evidence and in the FRD-13 section, or a later
  reviewer will read it as missing behaviour.
- **Four tabs will be empty in Phase 3, and that is correct.** Vehicle,
  Assessment, Documents and Reports payloads arrive with [[FEAT-015]],
  [[FEAT-017]], [[FEAT-014]] and [[FEAT-018]]. The design authority's
  only-populated-sections rule (`docs/design/README.md:432-439`) means an
  unpopulated tab renders nothing — so the tab strip must tolerate a tab whose
  section endpoint does not yet exist without showing an error. That is a
  design decision this ticket makes, and it should be a view-model fact.
- **`BeforeVersion` and `AfterVersion` must not reach the screen.** They are on
  `CaseHistoryEntry` (`CaseQueries.cs:94-95`) and the ticket's acceptance
  criteria forbid a version integer on the surface. The History row renders
  actor display name, action, Europe/London timestamp and reason — nothing else.
- **`ActorDisplayName` must come from the gateway, never from the desktop.**
  Its default is the honest `ActorDisplayNames.UnknownStaff` fallback
  (`CaseQueries.cs:104`); a desktop that saw an empty name and substituted the
  raw `Actor` subject id would render an identifier, which
  `CaseEditAuthority.cs:68-73` and the design authority both forbid.
- **The Queries group's one-line empty statement is a sanctioned exception to a
  merge rule.** `docs/design/README.md:432-439` says an unpopulated read-only
  section is absent, not an empty-state panel. The ticket body's step 7 and its
  acceptance criteria make the truthful single line the one stated exception,
  required by upstream CASE-009. It must be recorded as such in
  `docs/desktop/06-ui-design/screen-specs.md` § `§13.8 Communications` (the
  ticket's own Documentation changes) or the next reviewer will read it as a
  breach.
- **Do not render `PolicyKey` or `PolicyVersion`.**
  `MailOperationalDestinationResult` carries both (`MailOperationalDestinationPolicy.cs:17-22`);
  [[FEAT-037]]'s step 9 already forbids exposing them, and this ticket must not
  reintroduce them through a tooltip or a diagnostic column.
- **Per-section error isolation is the desktop's own invention and needs a
  fact.** The web returns one 503 for the whole page. The desktop must let one
  failing section show unavailable while the rest of the workspace stays
  usable — the ticket's acceptance criterion "a failing section does not blank
  the workspace". No web behaviour tells the implementer this; the state
  contract at `docs/design/README.md:764-772` is the authority.

## Open questions

None that block the plan. `A-05-08` through `A-05-10` are settled by step 3's
reading of [[GWY-007]]'s delivered contract; `A-05-11` is settled by checking
[[FEAT-037]]'s delivered communications payload before step 7, with a recorded
deferral if it has not landed. The upstream dependency named in the ticket's
Guardrails — upstream `CASE-020` (read the case header from the case, not the
instruction draft), which has no fork ticket — must be true before `PAR-08`
reaches parity; the body's instruction is to **raise it rather than work around
it**, and it is recorded in the plan's *Risks / open questions* section as a
named external dependency. No `open-questions` document is created: the ticket
body does not ask for one, and nothing here is unsettled in a way a plan would
silently assume.
