# Checklist — FEAT-030

One box per plan step, in plan order. The last box produces `proof`.

- [ ] Read the plan row `DSK-07-04`, the Operations screen spec (`docs/desktop/06-ui-design/screen-specs.md:390-398`), the cross-cutting state contract (`:417-427`) and `docs/design/README.md:170` and `:412-420`; call `get_doc_gates FEAT-030`; `take_ticket` on branch `task/dsk-07-04-operations-screen` from a worktree cut off `origin/dev`
- [ ] Confirm [[TEST-006]] (plan handle `DSK-08-06`) and [[DUI-015]] (plan handle `DSK-06-15`) have landed, so `winapp ui` and the accessibility scan are available — stop rather than substitute a screenshot if either is missing
- [ ] Read the [[FEAT-027]] (plan handle `DSK-07-01`), [[FEAT-028]] (plan handle `DSK-07-02`) and [[GWY-013]] (plan handle `DSK-03-13`) contracts in `src/Pegasus.Contracts`, and raise any missing field on the owning endpoint ticket rather than hand-writing a DTO
- [ ] Run `pwsh ./eng/api/Generate-ApiClient.ps1` twice and confirm `git diff --exit-code` is clean after the second
- [ ] Add `OperationsViewModel` to `src/Pegasus.Desktop` using `ObservableObject` and `[RelayCommand]`, with no `SolidColorBrush`, `Visibility` or other UI type — extending the existing type in place if [[FEAT-020]] (plan handle `DSK-05-20`) created it first
- [ ] Model the load state as `NotStarted` / `Running` / `Succeeded` / `Failed` / `Cancelled`, setting `ObtainedAtUtc` **only** in the success branch and after the last await
- [ ] Make failure preserve the previous rows, label them previously-obtained with their earlier time, and raise the failure sentence — clearing no collection
- [ ] Build `OperationsPage.xaml` with the external-work table (kind, case, last failure, attempts, next action), the upload-links table and the health rows, using the [[DUI-007]] (plan handle `DSK-06-07`) data-table pattern
- [ ] Apply the four AutomationIds from the spec exactly — `Operations.External.Table`, `Operations.External.Retry`, `Operations.Links.Revoke`, `Operations.Health.<Dependency>` — at 100% coverage
- [ ] Review every operator string against `docs/design/README.md:170`, sourcing each from `OperatorLabels` or the eight approved web sentences and rendering times through `OperatorLabels.OfficeTime` / `OfficeDate` — writing none fresh
- [ ] Bind retry enablement to `canRetry` and revoke enablement to `canRevoke` alone, inferring eligibility from no client-side value
- [ ] Send **two** distinct operation keys per revoke — one for the case edit lease, one for the revoke — and surface the already-leased case with the approved sentence from `Index.cshtml.cs:147`
- [ ] Preserve the typed reason on a refused revoke, as `PreserveReason` (`Index.cshtml.cs:218-235`) does
- [ ] Render the two poison figures as named values and the mailbox freshness state (`current` / `stale` / `unavailable`) with its last successful cycle time, collapsing `unavailable` into no success word
- [ ] Surface an unrecognised operation state rather than degrading it to "Unknown", mirroring the throwing default arm at `Index.cshtml.cs:185`
- [ ] Give every meaning-bearing state text as well as colour, and render the Box, DVLA/DVSA, update-feed and minimum-client-version health rows as **absent** until [[PLAT-015]] (plan handle `DSK-10-15`)'s endpoint exists
- [ ] Render refusals through the [[DUI-010]] (plan handle `DSK-06-10`) problem presentation — one operator sentence plus a copyable Reference carrying the correlation id — and use the [[DUI-009]] (plan handle `DSK-06-09`) `ReasonDialog` for revoke only, with a plain confirmation for retry
- [ ] Distinguish a replayed retry from a first effect in the operator's words, as `result.IsReplay` does at `Index.cshtml.cs:92-94`
- [ ] Implement the disconnected state: say so, keep the last obtained values labelled with their time, disable the commands, and offer manual refresh
- [ ] Add the view-model tests: success sets `ObtainedAtUtc`; failure preserves rows and does not; retry and revoke disabled on false flags; a refused retry surfaces sentence and reference; a refused revoke preserves the reason; cancellation leaves `Cancelled`; disconnected keeps rows and disables commands; an unrecognised state surfaces
- [ ] Launch with `.\BuildAndRun.ps1` in async mode, capture the PID, and write the `winapp ui` `-Script operations` batch covering both tables rendering, retry disabled then enabled, the reason dialog, keyboard-only traversal of both tables, and the disconnected-state screenshot
- [ ] Run the [[DUI-015]] accessibility scan over the screen and attach the report
- [ ] Write the § Operations section into `docs/frd/frd-13-desktop-operator-experience.md` (skeleton by [[FND-008]], plan handle `DSK-00-08`), leaving room for [[FEAT-020]]'s command sub-heading
- [ ] Move `PAR-27` (`docs/desktop/01-inventory-and-parity/parity-matrix.md:72`) from `not inventoried` to `implemented` and fill its empty `Verification` cell with the evidence produced here
- [ ] Run the simplification pass over this branch's own diff and record it under a dated `## Simplification pass` heading in the `plan` document
- [ ] Run the verification suite and capture its output as `proof`: `dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release`, `pwsh ./tests/Pegasus.Desktop.UITests/ui-tests.ps1 -AppPid <pid> -Script operations` against a **real** gateway, the `AxeWindowsCLI` scan report, and `git diff --stat origin/dev -- src/Pegasus.Web src/Pegasus.Core src/Pegasus.Infrastructure src/Pegasus.Worker` (expected: empty)

## Progress notes

(append with `set_ticket_doc(doc: "checklist", append: true)`)
