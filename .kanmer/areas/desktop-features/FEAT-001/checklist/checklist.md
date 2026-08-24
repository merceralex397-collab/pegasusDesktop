# Checklist — FEAT-001: S1 Dashboard and work queue

One box per plan step, in plan order. Tick with `set_ticket_doc`; append
progress notes below rather than rewriting.

- [ ] Read the plan row, `vertical-slices.md` § `Common to every slice` and § S1, and `docs/design/README.md:412-445`; run `get_doc_gates FEAT-001`; `take_ticket` on branch `task/dsk-05-01-dashboard`, worktree `../pegasus-worktrees/dsk-05-01-dashboard`, from `origin/dev`
- [ ] Load `pegasus-desktop` → `winui-dev-workflow` → `winui-design`, and confirm `.codex/skills/winui-dev-workflow/BuildAndRun.ps1` launches the packaged app with package identity
- [ ] Re-check parity drift: `git diff --stat bbd1c549..HEAD -- src/Pegasus.Web/Pages/Index.cshtml.cs src/Pegasus.Web/Presentation/RailCountsPageFilter.cs src/Pegasus.Core/Operations` is empty, or re-read and update the `research` document with the new SHA
- [ ] Confirm the `/api/v1/dashboard` endpoint filter requires `StaffAccessRight.PerformCasework` (matching `src/Pegasus.Core/Operations/OperationsSnapshot.cs:96`), and raise the `endpoint-map.md:43-44` `AccessStaffApplication` correction on [[GWY-006]]
- [ ] Confirm the `/api/v1/dashboard` weak `ETag` hashes the payload, not `asOfUtc`, by asserting two unchanged calls return the same `ETag`
- [ ] Confirm the mail figure is on the wire as `unidentified` (from `MailActivityCounts.Unidentified`, `DashboardCounts.cs:48`) and not as `needsSorting`
- [ ] Close any remaining gap in `GET /api/v1/dashboard` inside the `/api/v1` group, calling the same `IGetOperationsSnapshot` / `IDashboardQueries` the Razor page calls — no second query implementation
- [ ] Add `DashboardResponse` and `RailCountsResponse` (and nested count records) to `src/Pegasus.Contracts`, with **nullable** rail-count members and `asOfUtc` as `DateTimeOffset`, referencing no ASP.NET, EF or WinUI type
- [ ] Implement `DashboardViewModel` in `src/Pegasus.Desktop` with explicit `Loading` / `Empty` / `Error` / `Loaded` states
- [ ] Make `RefreshCommand` coalesce: a second invocation while one is in flight joins the first task and issues no second request
- [ ] Retain last-good values and mark them stale on a failed refresh; never blank the tiles
- [ ] Render `LastLoadedAt` as Europe/London through the shared operator vocabulary map, and cancel the in-flight request on navigation away
- [ ] Build the Dashboard XAML tiles listed at `docs/desktop/06-ui-design/screen-specs.md:131-140`, each linking to its filtered queue
- [ ] Render Recent cases only when there are entries, and the integration-failure list only when there are failures
- [ ] Give every interactive control an `AutomationId` per `screen-specs.md:145-147` (`Dashboard.Tile.<Metric>`, `Dashboard.Refresh`, `Dashboard.Recent.Row.<Ref>`)
- [ ] Confirm no banned word (`docs/design/README.md:412-420`), no field hint, no how-it-works copy and no chart reaches the screen, and that no state is signalled by colour alone
- [ ] Bind the shell rail counts to `GET /api/v1/dashboard/rail-counts` through the [[DUI-004]] shell view model, rendering nothing for an omitted count and inventing no figure for Inbox or Cases
- [ ] Bind `F5` and `Ctrl+R` to `RefreshCommand` and show current / stale / unavailable in the [[DUI-012]] page header control
- [ ] Write view-model tests: each of the four states, refresh coalescing, cancellation on navigate-away, error → `InfoBar` mapping, stale refresh retaining last-good, `null` rail count rendering nothing
- [ ] Write contract tests for both routes: gate off → 404, no token → 401, staff token → 200 with every field, `If-None-Match` → 304, and 403 (not 500) for an actor without `PerformCasework` — with `Features:DesktopGateway` enabled explicitly in the factory
- [ ] Add `tests/Pegasus.Desktop.UITests/ui-tests.ps1 -Script dashboard`: launch, `wait-for` the AutomationIds (no sleeps), traverse every list by keyboard only, open one item
- [ ] Run the `axe-windows` scan from [[DUI-015]] on the Dashboard and attach the report and a screenshot to the ticket proof
- [ ] Produce the parity table on the Test/UAT stack (`docs/desktop/08-testing/test-uat-stack.md:22`): web counts vs desktop counts per figure, on the same database, using the `DashboardCountersWebTests` / `RailCountsWebTests` fixtures
- [ ] Update `docs/desktop/01-inventory-and-parity/parity-matrix.md` row `PAR-05` to `implemented`, then to `automated verification passed` once the UI script is green, with evidence pointers
- [ ] Add the Dashboard section to `docs/frd/frd-13-desktop-operator-experience.md` and a `DSK` row to `docs/capabilities.md`
- [ ] Run the simplification pass over the branch diff and record it under a dated `## Simplification pass` heading in the `plan` document
- [ ] Verification run — `dotnet build ./Pegasus.slnx -c Release --no-restore`; `dotnet test` for ViewModelTests, Api.ContractTests, ArchitectureTests and IntegrationTests (`--filter "Category!=Corpus&Category!=Browser"`); `pwsh ./tests/Pegasus.Desktop.UITests/ui-tests.ps1 -Script dashboard`; then write `proof` with the command log, the UI/axe artefacts and the parity table
- [ ] Open the PR into `dev`

## Progress notes

(append with `set_ticket_doc(doc: "checklist", append: true)`)
