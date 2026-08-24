# Files — FEAT-003

Surface area of `DSK-05-03 · S3 Case detail read-only and history`. Paths that
do not exist at `HEAD` `bbd1c549` are marked with the ticket that creates them;
every other path was confirmed with `ls` or `wc -l`.

## Where the change lands

| Path | Why |
| --- | --- |
| `src/Pegasus.Contracts/` *(created by [[FND-029]] (plan handle `DSK-02-04`))* | The case-header DTO plus one DTO per section, each carrying its own `version` and `ETag` marker. History rows must expose actor **display name**, action, timestamp and reason — and **not** `BeforeVersion` / `AfterVersion`, which exist on `CaseHistoryEntry` (`src/Pegasus.Core/Cases/CaseQueries.cs:94-95`) and are forbidden on an operator surface. |
| `src/Pegasus.Web/` — the `/api/v1` cases **read** group only *(group by [[GWY-002]] (plan handle `DSK-03-02`); routes by [[GWY-007]] (plan handle `DSK-03-07`))* | `GET /api/v1/cases/{id}` (header + Overview) and the seven section routes, each calling the same Core ports `GetCase` composes. Risk: splitting the composed record loses `GetCase`'s cross-case integrity check (`CaseQueries.cs:316-322`) unless each section re-asserts its own case id. |
| `src/Pegasus.Desktop/` *(created by [[FND-030]] (plan handle `DSK-02-05`))* | `CaseWorkspaceViewModel` (header state plus one child view model per tab, each with its own Loading/Empty/Error/Loaded and its own refresh) and the workspace XAML: header, eight-tab sub-navigation, collapsible activity pane, History tab, Communications tab with the Queries group. |
| `src/Pegasus.Desktop.Infrastructure/` *(created by [[FND-031]] (plan handle `DSK-02-06`))* | Per-section `ETag` cache for the lifetime of the open case, and `If-None-Match` revalidation on manual refresh. ADR-0104 bounds it: this is a per-session cache, not offline replication. |
| `tests/Pegasus.Desktop.ViewModelTests/` *(created by [[FND-038]] (plan handle `DSK-02-13`))* | Lazy tab activation, per-tab error isolation, `ETag` revalidation, history paging, and the three Queries-group facts. |
| `tests/Pegasus.Api.ContractTests/` *(created by [[TEST-001]] (plan handle `DSK-08-01`))* | Header and every section: 200 with `version` and `ETag`, 304 on `If-None-Match`, 401, 403 without `PerformCasework`, 404 for an unknown case. |
| `tests/Pegasus.Desktop.UITests/` *(created by [[TEST-006]] (plan handle `DSK-08-06`))* | `ui-tests.ps1 -Script case-detail`: open a case from the list, cycle every tab by keyboard, assert the header stays stable. |
| `docs/desktop/01-inventory-and-parity/parity-matrix.md` | Row `PAR-08` (`:53`) — **read path only**; the five `OnPost*` handlers in the same row belong to [[FEAT-005]] (plan handle `DSK-05-05`). |
| `docs/desktop/06-ui-design/screen-specs.md` § `§13.8 Communications` | Record that Query-classified linked e-mails render as an identified read-only group headed `Queries`, with a truthful empty state and no create/reply/resolve control. [[DUI-013]] (plan handle `DSK-06-13`) carries it into FRD-13; [[FEAT-037]] (plan handle `DSK-07-11`) owns the payload half. |
| `docs/frd/frd-13-desktop-operator-experience.md` *(skeleton by [[FND-008]] (plan handle `DSK-00-08`))* | Case-workspace section, including the deliberate three-tabs→eight-tabs difference. |
| `docs/capabilities.md` | One `DSK` row for the case-workspace read path. |

## Context files

Read these before writing code. Each is here for one specific trap.

| Path | What it tells the implementer |
| --- | --- |
| `src/Pegasus.Web/Pages/Cases/Details.cshtml.cs` (654 lines) | The whole read path is `OnGetAsync` at `:110-154`; everything else on the page is edit or lease machinery. **The web has three tabs, not eight** — `Tab` at `:58-64` collapses to `overview` / `evidence` / `history`, and the remarks at `:47-54` say why. `ImageIntakes` and `EvidenceImages` load on **every** tab (`:128-129`); the per-intake gallery loads only on Evidence (`:130-140`). Unexpected failure → 503 (`:146-152`), never a blank page. |
| `src/Pegasus.Core/Cases/CaseQueries.cs:264-357` (`GetCase`) | The single read use case, composing **eight** ports (`:264-272`). Requires `PerformCasework` (`:295`); refuses `Guid.Empty` (`:297-300`); throws `InvalidDataException` when the typed data is missing (`:308-309`) or when a composed part belongs to another case (`:316-322`); resolves history actor display names from `IStaffAccountQueries` (`:325-332`). Splitting this into sections is [[GWY-007]]'s work, and the integrity check must survive the split. |
| `src/Pegasus.Core/Cases/CaseQueries.cs:88-131` | `CaseHistoryEntry` (`:88-105`) — note `ActorDisplayName` is an `init` property whose default is the honest `ActorDisplayNames.UnknownStaff` fallback (`:104`) so "a caller that forgets to populate it never renders the raw subject id", and note `BeforeVersion` / `AfterVersion` at `:94-95`, which must not reach the screen. `CaseDetails` (`:108-131`) is the composed record the section split has to take apart. |
| `src/Pegasus.Web/Pages/Cases/Shared/_CaseSummary.cshtml` (237 lines) | Which fields belong to the header versus Overview, and the settled label for each. This is the field-by-field oracle for the parity comparison at step 11. |
| `src/Pegasus.Web/Pages/Cases/Shared/_CaseHistory.cshtml` (53 lines) | What the web actually renders per history row — and, by omission, what it does not: no version integers, no GUIDs. |
| `src/Pegasus.Web/Pages/Cases/Shared/_CaseWorkflow.cshtml` (551 lines) | The largest partial, and almost all of it is command markup that belongs to [[FEAT-006]] (plan handle `DSK-05-06`). Read it to know what to leave out of the read-only shell. |
| `src/Pegasus.Web/Pages/Cases/Shared/_CaseDocuments.cshtml` (158 lines) | The web's Evidence-tab composition — documents, vehicle images and evidence photographs in one panel. The desktop separates them across Documents, Vehicle and Reports; this file is the evidence for that being a deliberate difference. |
| `src/Pegasus.Web/Pages/Cases/CaseMutationPageModel.cs` (339 lines) | Everything the read path must **not** carry over: the cookie `TempData` keys (`:20-30`), the 8 000 / 2 000-character budgets (`:38-39`), the `RetainableFormFields` allow-list (`:46-88`), the PRG redirect (`:176-177`). None is read-path behaviour. Its retirement for desktop paths is [[FEAT-024]] (plan handle `DSK-05-24`). |
| `src/Pegasus.Core/Intake/Classification/MailOperationalDestinationPolicy.cs:7-22` | The canonical classification. `MailOperationalDestination` members occupy `:8-15` with `Queries` at `:10`; `MailOperationalDestinationResult` (`:17-22`) also carries `PolicyKey` and `PolicyVersion`, which must **never** reach an operator surface. |
| `src/Pegasus.Core/Workflow/CaseEditAuthority.cs:68-92` | `CaseEditAuthorityHolder` (`:75-81`) and why a holder is disclosed by name and never by identifier: "the retained holder is a subject identifier and an identifier is never operator-facing". The read-only header shows the holder; the lease session itself is [[FEAT-005]]. |
| `src/Pegasus.Core/Identity/StaffAuthorization.cs` (77 lines) | Twelve rights (`:8-20`), fail-closed matrix (`:33-56`). `PerformCasework` admits Staff **or** Automation (`:39-41`), so a 403 fact must use an actor that genuinely lacks it. |
| `docs/desktop/06-ui-design/screen-specs.md:178-231` | The case workspace: the ASCII header sketch (`:180-186`), "Identity, state, actions and main content reachable without scrolling at 1280×800" (`:189-190`), the tab list and per-tab contents (`:206-216`), History as "read-only permanent action history: actor, time, outcome; no bodies, no telemetry noise" (`:215-216`), the state list (`:220-222`), the keyboard map `Ctrl+1..8` (`:224`), and the AutomationIds (`:225-227`). |
| `docs/desktop/06-ui-design/screen-specs.md:417-427` | The cross-cutting state contract and the empty-state rule: "a read-only section with nothing recorded and no available action is absent… 'No results' text appears only for a search the operator ran." The Queries one-line statement is the one sanctioned exception, and the ticket says so. |
| `docs/design/README.md:422-445` | The four hard rules as merge rules, including "Only populated, relevant sections render… A long page of empty panels is a defect, not a layout choice." |
| `docs/design/README.md:761` (§ Permanent history) | What the History panel may and may not show: "attributable staff or automated actor, caller, time, one affected Case or pre-case record, action/outcome, reason where required" — and **not** "message bodies, routine views, refresh/polling, retries, lease heartbeats, or adapter/Worker mechanics". |
| `docs/desktop/03-gateway-api-and-data/endpoint-map.md:51-52`, `:130` | The three rows this ticket consumes: the header/overview read, the seven section reads with "ETag per section", and the audit read with its `page` parameter and split auth rights. |
| `docs/frd/frd-01-case-identity-and-lifecycle.md:82-88` | The case-edit-authority section this ticket's `refs` names. `:84` — "Other authorised staff remain read-only and can see the holder and recovery state" — is what the read-only header must render. |
| `tests/Pegasus.IntegrationTests/CaseDetailsWebTests.cs` (1,286 lines) | The named parity oracle. Its scenarios are the source for the three fixture cases compared field by field at step 11. |
| `tests/Pegasus.IntegrationTests/IntakeWebTestSupport.cs:26` | The shared `WebApplicationFactory<Program>`. `Features:DesktopGateway` must be enabled explicitly, or every `/api/v1` route returns 404 and reads as a routing bug. |
| `docs/desktop/08-testing/test-uat-stack.md:22` | The Test/UAT configuration for the parity and navigation-budget runs. |

## Ripple effects

- **Generated client.** [[GWY-005]] (plan handle `DSK-03-05`) commits Kiota
  output with a CI no-op check; eight new read shapes regenerate it and the
  regenerated files belong in this diff.
- **OpenAPI snapshot.** [[TEST-001]] (plan handle `DSK-08-01`) fails on an
  undeclared change; all eight routes and their schemas land in the snapshot in
  the same commit.
- **Architecture tests.** `tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs`
  (520 lines), extended by [[FND-037]] (plan handle `DSK-02-12`), fails on an
  ASP.NET/EF/WinUI type inside `Pegasus.Contracts` and on any
  `Pegasus.Infrastructure` reference from the desktop.
- **Screen spec change ripples into FRD-13.** The Communications amendment this
  ticket makes to `docs/desktop/06-ui-design/screen-specs.md` § `§13.8` is
  carried into FRD-13 by [[DUI-013]] (plan handle `DSK-06-13`), whose ticket
  adopts the spec blocks as FRD sections. Edit the spec, not FRD-13's copy.
- **[[FEAT-037]] (plan handle `DSK-07-11`) owns the payload half** of the same
  Queries change. Its step 9 adds the classification field to
  `GET /api/v1/cases/{caseId}/communications`; this ticket renders it. Neither
  side adds a second read.
- **Every later slice hangs its tab here.** [[FEAT-005]] adds edit state to this
  view model; [[FEAT-006]] fills the command bar slot; [[FEAT-014]], [[FEAT-015]],
  [[FEAT-017]] and [[FEAT-018]] fill Documents, Vehicle, Assessment and Reports.
  A change to `CaseWorkspaceViewModel`'s child-view-model contract is a change
  to six later tickets' foundation.
- **Existing web tests must stay green.** Nothing here touches
  `Pages/Cases/Details.cshtml.cs`, its partials or `CaseMutationPageModel.cs`,
  so `CaseDetailsWebTests.cs` must pass unchanged. A diff touching them is a
  scope breach.
- **Downstream tickets.** `FEAT-003` blocks `FEAT-004`, `FEAT-005`, `FEAT-007`,
  `FEAT-022`, `FEAT-025`, `TEST-007` and `TEST-016`.
- **Documentation link check.** `scripts/Test-DocumentationLinks.ps1` runs over
  repository documentation, so a broken relative link in the new FRD-13 section
  or in the screen-spec amendment fails CI.

## Out of scope

Recorded so the reviewer sees each was a decision.

- **`Pages/Cases/Details.cshtml.cs`, its four `Shared/` partials and
  `CaseMutationPageModel.cs` are not modified.** They stay live until `PAR-08`
  reaches `cut over`; the cut is [[FEAT-026]] (plan handle `DSK-05-26`) and the
  page-model retirement is [[FEAT-024]] (plan handle `DSK-05-24`).
- **No editing, no lease, no commands.** The five `OnPost*` handlers are
  [[FEAT-005]] (plan handle `DSK-05-05`); the command bar is a **slot** here and
  its contents are [[FEAT-006]] (plan handle `DSK-05-06`).
- **No second communications read.** The classification field comes from
  [[FEAT-037]] (plan handle `DSK-07-11`); this ticket renders it and adds no
  query of its own.
- **No query lifecycle.** No **Raise a query** control, no reply, no resolve, no
  manual association, no mailbox mutation. Raising, replying to and resolving a
  query are upstream `CASE-002` and are not activated here — building one is a
  stop condition.
- **No `PolicyKey` or `PolicyVersion` on any surface**, including tooltips and
  diagnostic columns.
- **No `BeforeVersion` / `AfterVersion`, GUID or hash on any surface.**
- **No Vehicle, Assessment, Documents or Reports payload.** Those tabs exist in
  the strip and render nothing until [[FEAT-015]], [[FEAT-017]], [[FEAT-014]] and
  [[FEAT-018]] deliver them.
- **No `TempData["CaseDetailsStatus"]`-style status passing.** Upstream
  `CASE-001` asked to show-or-drop it and it is dropped for the desktop
  (`docs/desktop/05-implementation-and-migration/README.md` § 3).
- **No Azure write.** Enabling `Features:DesktopGateway` in production is
  [[PLAT-024]] (plan handle `DSK-11-06`).
