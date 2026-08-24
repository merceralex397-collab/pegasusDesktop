# Research — GWY-007: DSK-03-07 · Case read endpoints: paged list/search, sectioned detail, case history and audit

## Question

Add the read half of the case surface to `/api/v1`: a paged, sorted, filtered case list, a sectioned case detail whose sections load independently, and the case history/audit reads — all projecting the same Core query ports the Razor pages use, and sourcing the four case-identity facts from the case itself rather than the intake draft (upstream CASE-020).

## Evidence examined

- Plan row: `docs/desktop/03-gateway-api-and-data/README.md` § 5 — `DSK-03-07`
- Plan detail: same file § 3 — rows *Paging/filter/sort*, *Case detail*, *Concurrency*
- Plan detail: `docs/desktop/03-gateway-api-and-data/endpoint-map.md` § Cases (first three rows) and § Administration and audit (Audit row)
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 10.6 Query strategy, § 14.4 Work queue and case list, § 14.5 Case workspace
- Upstream carry-over: **upstream CASE-020** *Read the case header and list from the case, not the intake draft* (`fix`, `qdos26011`, `found-during-qa`, links upstream CASE-018 and upstream ENG-013) — **absorbed here; it was not imported, so it has no fork ticket and must never be written as a board wiki-link.** Its scope is exactly three lines: read the four fields from the case's own data; keep the intake draft as intake evidence on the receipt; re-check the search and sort paths when the value moves source. Its verification is "a case whose registration was corrected by staff shows the corrected value in the list, in the header band and in the Vehicle block — one value, three places, no disagreement". It records production as **not** currently affected (all three live cases carry draft rows that match their case fields), so this is latent, not live. Explicitly **not** in its scope: whether the header should repeat registration and claimant at all — that is an open operator decision under **upstream CASE-018** (also not imported, no fork ticket) and is not decided here. `docs/desktop/06-ui-design/screen-specs.md:230-231` wrongly lists CASE-020 among the carry-over rows "absorbed" by the case-workspace screen specification; a screen specification cannot deliver a query projection, and this ticket's § Documentation changes corrects that line.
- **Upstream-to-board join for the `CASE-` ids this body names — every one of them collides, so read this before writing any of them:**
  - **upstream CASE-020** — not imported, **no fork ticket**; delivered here.
  - **upstream CASE-021** — imported as board **[[CASE-001]]** (refuse Review for a case with no images).
  - **upstream CASE-022** — imported as board **[[CASE-002]]** (public upload links to the operator's accepted limits).
  - **upstream CASE-001** and **upstream CASE-002** are *different tickets again* — upstream CASE-001 is moot with the Razor front end and upstream CASE-002 is a post-alpha capability-allocation ticket; **both were dropped and neither was imported**, so board `CASE-001` and board `CASE-002` do not mean them. The `case-reference-workflow` area holds exactly two tickets and they are upstream CASE-021 and upstream CASE-022.
  - **upstream CASE-011, CASE-012, CASE-018, CASE-019 and ENG-013** — not imported, no fork tickets.
  Cite every id as `upstream <ID>` or, where a fork ticket exists, `upstream <ID> (board [[<board-id>]])`. [[DSK-01-09]] step 3 holds the full join table.
- Repository evidence:
  - `src/Pegasus.Core/Cases/CaseQueries.cs:45-50` — `SearchCasesQuery(ActionActor Actor, CaseSearchFilters Filters, int Page = 1, int PageSize = 25, CaseSearchOrder Order = CaseSearchOrder.ReceivedDesc)`
  - `src/Pegasus.Core/Cases/CaseQueries.cs:12-25` — `CaseSearchFilters(CaseReference, Registration, Claimant, ClaimNumber, Principal, State, EngineerId, ReceivedDate, InstructionDate, FromDate, ToDate, Origin, Query)`: the filter set the query parameters map onto
  - `src/Pegasus.Core/Cases/CaseQueries.cs:31-44` — `CaseSearchOrder` with `ReceivedDesc` first: newest received first is the default everywhere
  - `src/Pegasus.Core/Cases/CaseQueries.cs:69-74` — `SearchCasesResult(Items, Page, PageSize, HasPreviousPage, HasNextPage)`: **no total count exists**, so the paging envelope must not invent one
  - `src/Pegasus.Core/Cases/CaseQueries.cs:108-133` — `CaseDetails`, the fat record `Cases/Details` loads today; the sections carve it up
  - `src/Pegasus.Core/Cases/CaseQueries.cs:136-145` — `ICaseQueryStore.SearchAsync` / `GetAsync`
  - `src/Pegasus.Infrastructure/Persistence/EfCaseQueryStore.cs:224-252` — `SearchRows`, the projection CASE-020 names: it joins `IntakeReceiptEntity` to `InstructionDraftEntity` and sets `Registration = draft == null ? null : draft.VehicleRegistration`, `Claimant = draft == null ? null : draft.ClaimantName`, `ClaimNumber = draft == null ? null : draft.ClaimNumber` (`:244-246`) and `InstructionDate = draft == null ? null : draft.InstructionDate` (`:248`)
  - `src/Pegasus.Core/Cases/CaseDataContracts.cs:48-55` — `CaseField<T>(Fact, Suggestion, Confirmed)` with `Current => Confirmed ?? Fact ?? Suggestion`: the resolution rule the four fields must follow
  - `src/Pegasus.Core/Cases/CaseDataContracts.cs:70-92`, `:109-123`, `:166-169` — `CaseDataProjection.Claimant.Name`, `.Claim.Number`, `.Vehicle.Registration`, `.Instruction.InstructionDate`, and `ICaseDataQueries.GetAsync(Guid caseId, CancellationToken)`, the accepted source
  - `src/Pegasus.Infrastructure/Persistence/CaseDataModelConfiguration.cs:39-61` — the `CaseDataFields` table the projection must read instead
  - `tests/Pegasus.IntegrationTests/CasesIndexWebTests.cs`, `CaseDetailsWebTests.cs` — the existing web behaviour to compare against
- Binding decisions:
  - L-01 — endpoints evolve inside `Pegasus.Web`; the Razor pages keep working through coexistence.
  - L-02 — evidence comes from the local LocalDB stack, not an Azure test resource.
- Depends on: `DSK-03-03` for the `StaffAccessRight` filter and actor resolution.

## Scope and constraints

Proposal § 10.6 requires server paging/filtering/sorting and case detail loaded in sections "so the first useful view appears quickly"; § 14.4–14.5 make the work queue and case workspace the Phase 3 vertical slice. Operator-visible consequence: the case list opens as fast as the web list and shows the same rows in the same order, and a case workspace paints its header before the heavy sections arrive. Today `Cases/Details.cshtml.cs` loads one fat `CaseDetails` record; reproducing that on the desktop would put a multi-second wait in front of every case open. Upstream CASE-020 adds a second operator-visible consequence: the list and the header today read registration, claimant, claim number and instruction date from the origin receipt's `InstructionDraftEntity` while the rest of the case page reads `CaseDataFields`, so the same four facts have two independent sources that disagree the moment staff correct a case — a staff correction writes `CaseDataFields` and never the draft, so the list an operator searches would keep showing the superseded value indefinitely.

- Future owner: `pegasus-gateway-dev`; tests: `pegasus-test-engineer`; independent review: `pegasus-desktop-reviewer`.
- Use `dotnet-webapi`, `optimizing-ef-core-queries` where the ticket changes a query, and `run-tests` for the actual runner profile. The project decision overrides generic “service per endpoint” advice: route handlers translate to existing `Pegasus.Core` ports; no second policy/service layer is introduced.
- The shared EPIC context binds this to versioned `/api/v1` route groups in the existing `Pegasus.Web`, the existing rate-limiter mechanism, an OpenAPI snapshot, and no Azure write.

- Microsoft Learn (fetched 2026-08-24): [ASP.NET Core OpenAPI support](https://learn.microsoft.com/aspnet/core/fundamentals/openapi/overview?view=aspnetcore-10.0) confirms first-party OpenAPI generation. Use the repository’s planned committed snapshot and contract-test flow rather than adding a parallel API documentation path.

- **Azure**: no write.
- **Scope boundary**: may touch `src/Pegasus.Web/Api/**`, `src/Pegasus.Contracts/Cases/**`, `openapi/`, the generated client and the test projects. **Named exception for upstream CASE-020**: this ticket may also edit the `SearchRows` projection in `src/Pegasus.Infrastructure/Persistence/EfCaseQueryStore.cs` (`:224-252`) and nothing else in `src/Pegasus.Infrastructure` — the fix is a read projection, it is the one place the defect lives, and no other seeded ticket is permitted to make it. Must not modify `src/Pegasus.Web/Pages/Cases/**`, must not change any write path or the intake draft itself, and must not add a new Core query port without a recorded decision.
- **Traps**: do not port `Pages/Cases/CaseMutationPageModel.cs`'s TempData proposed-values/lease chaining — that is web-only state the desktop keeps in memory. Design authority: filters are dropdowns and tables sort newest first (`docs/design/README.md` § No explanatory copy and page economy). Contract changes must stay additive once the pilot ring exists. Upstream `main` is ahead of the fork; check for drift in the case pages after the first upstream sync ([[DSK-01-10]]). Upstream CASE-020 is **latent, not live** — all three production cases currently carry draft rows matching their case fields — so a "nothing changes in production" observation is expected and is not evidence the fix is unnecessary; the failure appears the first time staff correct a case. Whether the header should repeat registration and claimant at all is explicitly out of upstream CASE-020's scope (an open operator decision under **upstream CASE-018**, which has no fork ticket) — do not remove or restructure the header band here. **Upstream ids and fork board ids do not match, and every `CASE-` id in this body collides**: upstream CASE-020 was absorbed here and has **no fork ticket**; upstream CASE-021 is board [[CASE-001]] and upstream CASE-022 is board [[CASE-002]]; upstream `CASE-001` and upstream `CASE-002` are different tickets again and were both dropped, so board `CASE-001`/`CASE-002` never mean them; and upstream CASE-011, CASE-012, CASE-018, CASE-019 and ENG-013 have no fork tickets at all. Always write `upstream <ID>` or `upstream <ID> (board [[<board-id>]])`, never a bare `CASE-0nn`.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.

## Governing documents

- No canonical document is linked yet. Retain the ticket's existing `docs_todo` state; do not invent or link a proposed desktop ADR.

## Planning implication

Reuse the cited boundaries and revalidate the named sources against current `origin/dev` after the ticket is taken. Do not create a compatibility path, duplicate policy, or an unapproved external write.
