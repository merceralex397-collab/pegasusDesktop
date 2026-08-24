# Checklist — FEAT-002: S2 Case list and search

One box per plan step, in plan order. Tick with `set_ticket_doc`; append
progress notes below rather than rewriting.

- [ ] Read the plan row, `vertical-slices.md` § S2, `docs/design/README.md:441-445` and the UI-07 field list at `:745-757`; run `get_doc_gates FEAT-002`; `take_ticket` on branch `task/dsk-05-02-case-list`, worktree `../pegasus-worktrees/dsk-05-02-case-list`, from `origin/dev`
- [ ] Re-check parity drift: `git diff --stat bbd1c549..HEAD -- src/Pegasus.Web/Pages/Cases/Index.cshtml.cs src/Pegasus.Web/Pages/Search/Index.cshtml.cs src/Pegasus.Core/Cases/CaseQueries.cs` is empty, or re-read and update `research` with the new SHA
- [ ] Confirm `GET /api/v1/cases` accepts all twelve UI-07 filters, not the six named at `endpoint-map.md:49`; extend the endpoint against the same `ISearchCases` call where it is short
- [ ] Confirm `sort` maps onto `CaseSearchOrder`'s ten members (`CaseQueries.cs:31-43`) and defaults to `ReceivedDesc`
- [ ] Confirm the gateway turns `ArgumentException` / `ArgumentOutOfRangeException` from `SearchCases.ExecuteAsync` (`CaseQueries.cs:186-211`) into 400 problems, not 500s
- [ ] Settle whether global search is `/cases?q=` or a `/search` route, and raise the `parity-matrix.md:51` `~GET /api/v1/search` correction on [[GWY-007]] if it disagrees
- [ ] Run the `optimizing-ef-core-queries` review over the gateway query path for N+1 and unbounded projections, and record the outcome plus the total-count and `Due by` decisions under a dated note in the `plan` document
- [ ] Add the list DTOs to `src/Pegasus.Contracts` mirroring `CaseSearchItem` (`CaseQueries.cs:52-67`) without enum names on the wire
- [ ] Implement `CaseListViewModel` with incremental server paging and a `SortDescriptor` that issues a server sort and resets to page 1
- [ ] Cancel the in-flight page request on a filter change, and reset to page 1
- [ ] Confine local re-ordering to the loaded page; never present it as a sort of the whole result set
- [ ] Build the list XAML on the [[DUI-007]] data-table pattern: 32 px rows, header cells as sort controls with accessible sort state, `ListView` virtualization
- [ ] Render every filter as a `ComboBox`, never a pill row, with the `kind` filter offering exactly `All` / `Instructions` / `Images`
- [ ] Persist the column-chooser layout locally per user through the [[FND-031]] settings abstraction — layout only, no result data (ADR-0104)
- [ ] Open the selected case on Enter and on double-click; give every control an `AutomationId` per `screen-specs.md:176-177`
- [ ] Render Stage and Type through the settled operator vocabulary (`OperatorLabels.cs:101-137`), never an enum name and never colour alone
- [ ] Bind `Ctrl+K` to the shell search slot and `Ctrl+F` to the in-list alias; group results by case / party / vehicle / document metadata as the gateway supports, keyboard traversable
- [ ] Label Recent items as a local convenience, never as search authority (`screen-specs.md:139-140`); the desktop never downloads the dataset
- [ ] Render loading, "No results" for a search the operator ran, and **unavailable** for a 503 as three distinct states, using the [[DUI-010]] `InfoBar` with a copyable Reference — not a modal
- [ ] Write view-model tests: first page, next page, end of set, sort toggle, filter reset to page 1, in-flight cancellation, empty vs unavailable, column-layout round-trip
- [ ] Write contract tests: paging, both sort directions for each of the five sortable columns, one theory per filter, 401, 403 without `PerformCasework`, `If-None-Match` → 304, and 400 problems for the six Core input bounds — with `Features:DesktopGateway` enabled explicitly
- [ ] Add `tests/Pegasus.Desktop.UITests/ui-tests.ps1 -Script case-list` asserting keyboard traversal and open-by-Enter with `wait-for`, no sleeps
- [ ] **Operator step** — measure first-page latency on the baseline Test/UAT workstation: ≤ 1 s cold and warm, provider outage excluded, with the hardware specification recorded
- [ ] Produce the parity table: identical result sets and ordering versus the web Cases page for the same filters and database, built from the `CasesIndexWebTests` fixtures
- [ ] Update `docs/desktop/01-inventory-and-parity/parity-matrix.md` rows `PAR-06` and `PAR-07` with status and evidence pointers
- [ ] Add the list and search section to `docs/frd/frd-13-desktop-operator-experience.md` and a `DSK` row to `docs/capabilities.md`
- [ ] Run the simplification pass over the branch diff and record it under a dated `## Simplification pass` heading in the `plan` document
- [ ] Verification run — `dotnet build ./Pegasus.slnx -c Release --no-restore`; `dotnet test` for ViewModelTests, Api.ContractTests and IntegrationTests (`--filter "Category!=Corpus&Category!=Browser"`); `pwsh ./tests/Pegasus.Desktop.UITests/ui-tests.ps1 -Script case-list`; then write `proof` with the command log, the UI artefacts, the performance record and the parity table
- [ ] Open the PR into `dev`

## Progress notes

(append with `set_ticket_doc(doc: "checklist", append: true)`)
