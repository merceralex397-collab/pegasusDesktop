# Research — FEAT-002: what the case list and search really do today

## Question

What exactly does the web Cases page query — which filters, which page size,
which sort, which Core method — and what does `GET /api/v1/cases` have to carry
so a server-paged, sortable, filtered native list returns identical result sets
and ordering to the web for the same database?

## Current behaviour

`src/Pegasus.Web/Pages/Cases/Index.cshtml.cs` (261 lines, verified `wc -l`).
`IndexModel` takes three dependencies — `ISearchCases`, `IImageIntakeQueries`,
`ILogger<IndexModel>` (`:14-17`) — and has one handler, `OnGetAsync` (`:71`).
Page size is a private constant, `ResultsPerPage = 25` (`:19`), not a query
parameter. Thirteen `[BindProperty(SupportsGet = true)]` filters bind from the
query string (`:21-60`):

| Bound property | Query name | Type |
| --- | --- | --- |
| `CaseReference` | `case` | `string?` |
| `Registration` | `registration` | `string?` |
| `Claimant` | `claimant` | `string?` |
| `ClaimNumber` | `claimNumber` | `string?` |
| `Principal` | `principal` | `string?` |
| `State` | `state` | `CaseLifecycleState?` |
| `EngineerId` | `engineerId` | `Guid?` |
| `ReceivedDate` | `receivedDate` | `DateOnly?` |
| `InstructionDate` | `instructionDate` | `DateOnly?` |
| `FromDate` | `fromDate` | `DateOnly?` |
| `ToDate` | `toDate` | `DateOnly?` |
| `Origin` | `origin` | `string?` |
| `Query` | `query` | `string?` |
| `RecordKindFilter` | `kind` | `string?` (`instructions` / `images`) |

`OnGetAsync` resolves the staff actor, refuses an unrecognised `kind` with
`NotFound()` (`:79-82`), optionally loads Image-intake rows
(`LoadImageIntakeResultsAsync`, `:143-203`), then calls
`searchCases.ExecuteAsync` with a `SearchCasesQuery` built from the thirteen
filters, the page number and `ResultsPerPage` (`:101-119`). An `ArgumentException`
becomes a model-state error (`:124`); any other exception sets `QueryFailed` and
**HTTP 503** (`:129-130`). `RouteValues` / `PageUrl` (`:208`, `:243`) rebuild
the whole query string for each page link — the page reload the desktop
replaces.

`src/Pegasus.Web/Pages/Search/Index.cshtml.cs` (29 lines) is a retired screen:
one `OnGet` returning `RedirectToPagePermanent("/Cases/Index", new { query = Query })`
(`:27-28`). Its remarks (`:10-19`) record why — Search and Cases ran the
identical Core query, and the two screens disagreed about a query failure
(Cases 503, Search nothing).

Parity matrix rows: **`PAR-07`** (13.3 Case lifecycle, FRD-01,
`Cases/Index.cshtml.cs` (261) — `OnGetAsync`, status `inventoried`) at
`docs/desktop/01-inventory-and-parity/parity-matrix.md:52`, and **`PAR-06`**
(13.2, the `Search/Index` redirect, status `inventoried`) at `:51`. The matrix
holds 46 `PAR-` rows (`grep -c '^| PAR-' …parity-matrix.md` → `46`), all keyed
to page models under `src/Pegasus.Web/Pages/**`.

## Findings

### Facts

Verified at `HEAD` `bbd1c549` (2026-08-24). `git diff --stat 191ddf33..HEAD -- src tests`
is empty, so the plan set's line references taken at `191ddf33` still hold.
**`bbd1c549` is the revision characterized** (parity-drift trap).

- **The Core query already supports sorting, and the web page never uses it.**
  `CaseSearchOrder` (`src/Pegasus.Core/Cases/CaseQueries.cs:31-43`) has ten
  members: `ReceivedDesc`, `ReceivedAsc`, `ReferenceAsc`, `ReferenceDesc`,
  `RegistrationAsc`, `RegistrationDesc`, `ClaimantAsc`, `ClaimantDesc`,
  `PrincipalAsc`, `PrincipalDesc`. `SearchCasesQuery` (`:45-50`) takes it as
  `Order = CaseSearchOrder.ReceivedDesc` by default, and
  `Index.cshtml.cs:101-119` passes only `(actor, filters, PageNumber, ResultsPerPage)`
  — never an order. Every web result set is newest-received first.
  - Consequence: sortable columns are **not** a behaviour to port. They are an
    existing Core capability the web never exposed, so the gateway can offer
    five sortable columns without new business logic and without a parity
    difference to explain — the default stays `ReceivedDesc`, which is what the
    design authority requires ("tables sort newest first",
    `docs/design/README.md:441-445`).
- **Validation bounds are Core's, and they are exact.** `SearchCases.ExecuteAsync`
  (`CaseQueries.cs:175-227`) requires `StaffAccessRight.PerformCasework`
  (`:184`), then refuses: `Page` outside `1…10_000` (`:186-189`); `PageSize`
  outside `1…100` (`:190-193`); `EngineerId == Guid.Empty` (`:194-197`); an
  undefined `State` (`:198-201`); an undefined `Order` (`:202-205`); `FromDate > ToDate`
  (`:206-211`). Each throws `ArgumentException` / `ArgumentOutOfRangeException`,
  which the page turns into a model-state error rather than a 400.
- **Filter normalization has per-field character caps** (`CaseQueries.cs:212-226`,
  helper at `:231-245`): `CaseReference` 100, `Claimant` 300, `ClaimNumber` 100,
  `Principal` 20 (**and upper-cased**, `:219`), `Origin` 100, `Query` 300.
  `Registration` is capped at 20 and then compacted through
  `CaseRegistration.Normalize` (`:246-259`, defined `:161-171` — letters and
  digits only, upper-cased); a registration that compacts to empty is refused
  with "The registration filter is invalid."
- **`SearchCasesResult` carries no total count.** It is
  `(Items, Page, PageSize, HasPreviousPage, HasNextPage)`
  (`CaseQueries.cs:69-74`). The list can say "there is a next page"; it cannot
  say "of 412". Any desktop control that needs a total — a scrollbar
  proportional to the whole set, "page 3 of 17" — has no source today.
- **The row shape is fixed by `CaseSearchItem`** (`CaseQueries.cs:52-67`):
  `CaseId`, `Reference`, `AuditReference`, `CaseType`, `Principal`, `State`,
  `EngineerId`, `Registration`, `Claimant`, `ClaimNumber`, `ReceivedAtUtc`,
  `InstructionDate`, `Origin`, `CreatedAtUtc`, `NextChaseAtUtc`. Fifteen
  members, and the screen spec's table (`screen-specs.md:167-172`) shows seven
  columns — Case/PO, Registration, Principal, Type, Stage chip, Due by/overdue
  chip, Updated. `EngineerId` is a `Guid?` and no name accompanies it, so an
  Engineer **column** needs a resolution the row does not carry; the Engineer
  *filter* is by id and is fine.
- **`Due by` is not on the row.** `CaseSearchItem` has `NextChaseAtUtc`, not a
  due date; `CaseDueWork` lives on the case, not the search item. The spec's
  "Due by/overdue chip" therefore needs either an added projection member on the
  gateway read or a documented omission — it is not derivable from what
  `ICaseQueryStore.SearchAsync` returns today.
- **The Image-intake lookup is additive and is not a case query.**
  `LoadImageIntakeResultsAsync` (`:143-203`) runs unless `kind == "instructions"`
  and searches `IImageIntakeQueries` by exact Image Intake Reference and by
  compacted registration, de-duplicating by `Id`; with `kind == "images"` and no
  search input it lists everything (`:194-201`). Its own comment (`:133-138`)
  states the rule: "Case-search schema is unchanged — an Image Intake Reference
  is not a Case reference." `ImageIntakeOutcomeLabel` (`:205-206`) yields
  `"Image intake registered"` or `"Associated with Case"`.
  - The vertical-slices plan assigns Unidentified and vehicle images to
    [[FEAT-012]] (plan handle `DSK-05-12`), and the screen spec puts them on the
    Queues screen (`screen-specs.md:148-159`). This slice's `kind` filter is the
    one place the two meet, and the design authority's UI-07 field list
    (`docs/design/README.md:745-757`) names "Image Intake Reference" as a search
    field of this screen. So the *field* is in scope; the vehicle-images
    *workspace* is not.
- **The UI-07 field list is twelve fields** (`docs/design/README.md:745-757`):
  Case/PO, Image Intake Reference, registration, claimant, claim number,
  principal, state, Engineer, received date, instruction date, date range,
  origin. That is the authority for what the filter pane offers, and it matches
  the thirteen bound properties once `kind` is counted separately.
- **The endpoint map's parameter list is narrower than the page.**
  `docs/desktop/03-gateway-api-and-data/endpoint-map.md:49` records
  `GET /cases?page&pageSize&sort&stage&assignee&principal&q`. That is six
  filters against the page's thirteen and the authority's twelve. Auth right
  `PerformCasework` — which matches `CaseQueries.cs:184` exactly, unlike the
  dashboard rows.
- Existing test evidence, located by `ls tests/Pegasus.IntegrationTests` and
  `ls tests/Pegasus.Core.Tests/Cases`:
  `tests/Pegasus.IntegrationTests/CasesIndexWebTests.cs` (155 lines),
  `tests/Pegasus.Core.Tests/Cases/CaseSearchTests.cs`,
  `tests/Pegasus.IntegrationTests/Browser/UploadCaseSearchBrowserTests.cs`.
  The first two are more precise than the plan set's citation of the browser
  test alone and are the parity oracles for this slice.
- **Target projects do not exist yet.** `Pegasus.slnx` lists four production and
  three test projects; `src/Pegasus.Desktop`, `src/Pegasus.Desktop.Infrastructure`,
  `src/Pegasus.Contracts`, `tests/Pegasus.Desktop.ViewModelTests`,
  `tests/Pegasus.Api.ContractTests` and `tests/Pegasus.Desktop.UITests` are
  created by earlier tickets (see the files document).
  `grep -rn "DesktopGateway" src/ tests/` returns nothing — the gate is
  introduced by [[GWY-002]] (plan handle `DSK-03-02`).

### Assumptions

- **`A-05-04` — [[GWY-007]] (plan handle `DSK-03-07`) will expose all twelve
  UI-07 filters, not the six named in `endpoint-map.md:49`.** The ticket body's
  step 3 says "Where the contract is short of what the Razor page supports,
  extend the endpoint against the same `ICaseQueryStore` call". Confirmed by:
  reading [[GWY-007]]'s delivered parameter list at step 3. Breaks if wrong:
  the desktop cannot reach parity on filters, `PAR-07` cannot advance, and the
  gap is a defect on [[GWY-007]] rather than a desktop limitation.
- **`A-05-05` — a total count will not be added to the read.**
  `SearchCasesResult` has no total (`CaseQueries.cs:69-74`) and adding one means
  a second `COUNT(*)` per page on every keystroke-driven filter change.
  Confirmed by: the `optimizing-ef-core-queries` review at step 4 and
  [[GWY-007]]'s response shape. Breaks if wrong: the paging control must present
  "next / previous with accessible current-page context" (`screen-specs.md:172-174`)
  rather than "page n of m", which is what the screen spec already asks for —
  so this assumption being right is the cheaper outcome and the spec already
  assumes it.
- **`A-05-06` — the `Due by` chip needs a projection member the search result
  does not carry.** See the fact above. Confirmed by: reading [[GWY-007]]'s
  list DTO. Breaks if wrong (i.e. it is not added): the column is omitted with
  a recorded reason rather than computed on the desktop — a desktop-derived due
  date would be a second business implementation and a stop condition
  (`AGENTS.md` § Product invariants).
- **`A-05-07` — global search (`Ctrl+K`) is served by the same `/cases` route
  with `q`, not by a separate `/search` endpoint.**
  `endpoint-map.md:49` folds `Search/Index` into the `/cases` row's `Replaces`
  column, while `parity-matrix.md:51` names an indicative `~GET /api/v1/search`.
  Confirmed by: [[GWY-007]]'s route list. Breaks if wrong: `PAR-06`'s API column
  needs correcting, and the desktop binds a second client method — a small
  change, contained in step 7.

## Execution placement

The six-question test from `docs/desktop/00-governance-and-workflow/README.md`
§ 3, answered for the case list and search responsibility.

| Question | Answer | Evidence |
| --- | --- | --- |
| Shared authority — must several users see and update the same state? | **Yes** | The list is a query over shared case rows; every operator sees the same set for the same filters, and `ICaseQueryStore.SearchAsync` (`CaseQueries.cs:135-138`) reads the one case store. Lands in the gateway (`Pegasus.Web`, L-01). |
| Unattended execution — must it run with every desktop closed? | **No** | A read on demand. No Worker function lists cases (`reuse-map.md` § Pegasus.Worker names all nine). |
| Protected credentials — long-lived secret that must not sit on workstations? | **No** | SQL only, behind the gateway. The connection string is central because of question 5 and ADR-0103, not because search needs a secret. |
| Public callback — must an external service call a stable public endpoint? | **No** | No external party searches cases. |
| Central enforcement — revocation, permissions, audit, invariant independent of the client? | **Yes** | `StaffAuthorization.Require(query.Actor, StaffAccessRight.PerformCasework)` at `CaseQueries.cs:184`, and the six input bounds at `:186-211`, must hold whatever the client is. A desktop that filtered locally would bypass all of them. Lands in the gateway. |
| Measured operational advantage — measured evidence central is materially better? | **Yes** | Paging is the measured advantage, and this ticket measures it: the ≤ 1 s first-page budget (step 11) is taken on the baseline workstation. Server paging keeps the transferred set at 25 rows regardless of database size; the alternative — download and filter locally — has no bound at all. Lands in the gateway; the *sort of the loaded page* stays on the desktop. |

**Placement:** the gateway queries, validates, authorizes and pages; the desktop
renders, sorts the loaded page and holds the column layout. Three "yes"
answers, all naming the gateway. No Azure resource is involved and no Azure
write occurs.

## Implications

- **Sortable columns are a gift, not a port.** `CaseSearchOrder`'s ten members
  map onto five sortable columns (Received, Reference, Registration, Claimant,
  Principal), each in both directions. The gateway maps a `sort` parameter onto
  the enum; nothing new is computed. Registration and Reference are the two the
  screen spec's column list needs; Claimant and Principal come free.
- **Local sort must be visibly of the loaded page only.** Because Core sorts
  server-side over the whole set and the desktop only holds 25 rows, a
  client-side re-sort of those rows produces a *different* answer from a
  server re-sort. The ticket body's step 5 wording — "local sorting of **only
  the loaded page** — never a client-side sort that implies the whole set is
  present" — is therefore a correctness requirement, not a performance one.
  The safest implementation is: a header click issues a **server** sort and
  resets to page 1; local sort is offered only where the whole set is known to
  fit on one page.
- **Filter change must reset to page 1 and cancel the in-flight page.** Core
  refuses `Page > 10_000` but not a page beyond the result set, which simply
  returns empty; leaving the page number after a filter change gives an empty
  list that looks like "no results". The ticket body's step 9 already names both
  facts.
- **Validation errors are `ArgumentException`, and the gateway must turn them
  into 400 problems.** The web turns them into model-state errors
  (`Index.cshtml.cs:124`); a desktop that received a 500 for "the start date
  cannot be after the end date" would be a regression. Raise this on
  [[GWY-007]] if its mapping does not already cover
  `ArgumentException`/`ArgumentOutOfRangeException` — the existing precedent is
  `src/Pegasus.Web/Mcp/AutomationMcpErrors.cs:53-59`, which passes their message
  through as a caller error.
- **The 503 behaviour is deliberate and should survive.** `Index.cshtml.cs:126-131`
  logs and returns 503 on an unexpected query failure rather than a blank page.
  The desktop's error state maps 503 to "unavailable" in the state contract
  (`docs/design/README.md:766`), which is exactly the distinction the retired
  Search screen got wrong.
- **`kind=images` is in scope as a filter and out of scope as a workspace.**
  The UI-07 field list makes "Image Intake Reference" a search field here; the
  Vehicle images list/detail belongs to [[FEAT-012]] (plan handle `DSK-05-12`).
  Surfacing the row and its outcome label is parity; opening a vehicle-images
  detail screen is not this ticket.
- **Page size is a constant today, and making it a parameter is a real change.**
  `ResultsPerPage = 25` is private (`:19`); Core accepts 1…100
  (`CaseQueries.cs:190-193`). Exposing `pageSize` on the endpoint is within
  Core's bounds and is what the endpoint map already names, but the desktop
  should default to 25 so the parity comparison at step 12 compares like with
  like.

## Open questions

None that block the plan. `A-05-04`, `A-05-06` and `A-05-07` are each settled by
step 3's reading of [[GWY-007]]'s delivered contract, and `A-05-05` by the
step 4 query review; each has a named consequence recorded in the plan's
*Risks / open questions* section. The performance measurement in step 11 is an
operator/verifier task on real hardware, not an unknown — it has a stated
threshold (≤ 1 s), a stated source (the baseline Test/UAT workstation) and a
stated exclusion (provider outage). No `open-questions` document is created.
