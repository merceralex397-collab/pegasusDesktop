# Checklist — FEAT-003: S3 Case detail read-only and history

One box per plan step, in plan order. Tick with `set_ticket_doc`; append
progress notes below rather than rewriting.

- [ ] Read the plan row, `vertical-slices.md` § S3 and `docs/design/README.md:432-439`; run `get_doc_gates FEAT-003`; `take_ticket` on branch `task/dsk-05-03-case-detail`, worktree `../pegasus-worktrees/dsk-05-03-case-detail`, from `origin/dev`
- [ ] Re-check parity drift: `git diff --stat bbd1c549..HEAD -- src/Pegasus.Web/Pages/Cases/Details.cshtml.cs src/Pegasus.Web/Pages/Cases/Shared src/Pegasus.Core/Cases/CaseQueries.cs` is empty, or re-read and update `research` with the new SHA
- [ ] Read the four `Pages/Cases/Shared/` partials and record the header / Overview / per-tab field allocation the step-11 parity comparison will use
- [ ] Confirm `GET /api/v1/cases/{id}` returns the header **plus** Overview with a `version` and a weak `ETag`
- [ ] Confirm the seven section endpoints each carry their own `ETag` so they load independently — **stop and raise it on [[GWY-007]] if the split has not landed**
- [ ] Confirm each section endpoint re-asserts its own case id, preserving the cross-case integrity check at `src/Pegasus.Core/Cases/CaseQueries.cs:316-322`
- [ ] Confirm `GET /api/v1/cases/{id}/history` is paged, and that history rows come from the action-history read ports in `src/Pegasus.Core/Identity/`
- [ ] Implement `CaseWorkspaceViewModel` with header state plus one child view model per tab, each with its own Loading / Empty / Error / Loaded and its own refresh
- [ ] Load a tab's data on first activation only; an unvisited tab issues no request
- [ ] Cache section payloads by `ETag` for the lifetime of the open case and revalidate with `If-None-Match` on manual refresh — per-session only, nothing persisted for offline use (ADR-0104)
- [ ] Make a tab whose section endpoint does not yet exist render nothing and show no error
- [ ] Build the workspace XAML: stable header (reference, status, assignee, priority, save state) plus the command-bar slot, the eight-tab strip in `screen-specs.md:182` order, and the collapsible activity pane
- [ ] Render the edit-authority holder by name through `CaseEditAuthorityHolder` (`CaseEditAuthority.cs:75-81`), never by identifier
- [ ] Render only populated sections — no empty-state panels, with the single Queries exception below
- [ ] Give every control an `AutomationId` per `screen-specs.md:225-227`
- [ ] Implement the History tab: newest first, paged, rendering actor display name, action, Europe/London timestamp and reason where recorded
- [ ] Confirm no GUID, hash or version integer reaches the History row — in particular `CaseHistoryEntry.BeforeVersion` / `AfterVersion` (`CaseQueries.cs:94-95`)
- [ ] Never substitute the raw `Actor` subject id when `ActorDisplayName` is the `ActorDisplayNames.UnknownStaff` fallback (`CaseQueries.cs:104`)
- [ ] Render `Queries`-destination linked e-mails as their own identified read-only group on the Communications tab, headed `Queries`, from the [[FEAT-037]] payload — no second read
- [ ] Confirm the Queries group carries no create, reply or resolve control (upstream `CASE-002` is not activated here) and renders no `PolicyKey` or `PolicyVersion`
- [ ] Render the truthful one-line statement when linked e-mails exist but none is Query-classified, and record it as the one sanctioned exception to the only-populated-sections rule
- [ ] Confirm the workspace has no horizontal scrolling at 1280×800 and that focus order runs header → sub-navigation → content → activity pane
- [ ] Write view-model tests: lazy activation, per-tab error isolation, `ETag` revalidation, history paging, a tab with no endpoint rendering nothing, and the three Queries facts
- [ ] Write contract tests for the header and all seven sections: 200 with `version` and `ETag`, 304 on `If-None-Match`, 401, 403 without `PerformCasework`, 404 for an unknown case — with `Features:DesktopGateway` enabled explicitly
- [ ] Produce the parity table: field-by-field and history-row comparison against the web Details page for three `CaseDetailsWebTests` fixture cases, recording the three-tabs→eight-tabs difference as a known deliberate difference
- [ ] Measure the cached-navigation budget: first useful view ≤ 200 ms perceived after header load, with the method, figures and workstation specification recorded
- [ ] Add `tests/Pegasus.Desktop.UITests/ui-tests.ps1 -Script case-detail`: open a case from the list, cycle every tab by keyboard with `wait-for` (no sleeps), assert the header stays stable
- [ ] Run the `axe-windows` scan from [[DUI-015]] and attach the report and screenshots to the ticket proof
- [ ] Update `docs/desktop/01-inventory-and-parity/parity-matrix.md` row `PAR-08` for the **read path only**, leaving the edit handlers to [[FEAT-005]]
- [ ] Amend `docs/desktop/06-ui-design/screen-specs.md` § `§13.8 Communications` to record the Queries group, its truthful empty state and the absence of create/reply/resolve controls
- [ ] Add the case-workspace section to `docs/frd/frd-13-desktop-operator-experience.md` and a `DSK` row to `docs/capabilities.md`
- [ ] Run the simplification pass over the branch diff and record it under a dated `## Simplification pass` heading in the `plan` document
- [ ] Verification run — `dotnet build ./Pegasus.slnx -c Release --no-restore`; `dotnet test` for ViewModelTests, Api.ContractTests and IntegrationTests (`--filter "Category!=Corpus&Category!=Browser"`); `pwsh ./tests/Pegasus.Desktop.UITests/ui-tests.ps1 -Script case-detail`; then write `proof` with the command log, the UI/axe artefacts, the parity table and the navigation-budget record
- [ ] Open the PR into `dev`

## Progress notes

(append with `set_ticket_doc(doc: "checklist", append: true)`)
