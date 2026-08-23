# Vertical slices S1–S22

Each slice is written in the twelve-section shape of proposal §25 so it can
be pasted into a Kanmer ticket (plan document) with minimal editing. Route
group names are the planned names; [area 03](../03-gateway-api-and-data/endpoint-map.md)
is authoritative for exact paths. Screen specs are owned by
[area 06](../06-ui-design/screen-specs.md) and referenced by slice ID.
Ticket handles are `DSK-05-nn` (see the work breakdown in
[README.md](README.md)).

## Common to every slice

- **Placement default** (proposal §4, §4.1): interaction, immediate
  validation and local sorting/filtering of a loaded page run on the
  desktop; authoritative reads, writes, authorization, audit, secrets and
  shared state run in the gateway (`Pegasus.Web`, L-01). The six-question
  test is answered per slice below; "Shared authority = yes" and "Central
  enforcement = yes" hold for every write.
- **Concurrency and idempotency**: every command carries the Core
  `OperationKey` (caller-supplied idempotency key), `ExpectedVersion` and,
  for case edits, the `EditLeaseToken` (`CaseMutationRequest`,
  `src/Pegasus.Core/Workflow/CaseWorkflowContracts.cs:182`); conflicts are
  409 problems carrying the current version (area 03).
- **Authorization**: bearer token → `StaffActorFactory.TryCreate` →
  `StaffAccessRight` matrix (`src/Pegasus.Core/Identity/StaffAuthorization.cs`);
  the desktop hides or disables commands for usability only.
- **Design authority binding every slice** (`docs/design/README.md`):
  the four hard rules (a field is a label and a control; no how-it-works
  copy; only populated, relevant sections render; filters are dropdowns and
  tables sort newest first); banned operator words (`intake`, `bounded`,
  `projection`, `lease`, `opaque`, `ingress`, `composed`, `artifact`,
  `durable`, `aggregate`, `caller`, `correlation identifier`, `bytes`);
  the closed necessary-copy list; status vocabulary exactly as settled
  (`Audit`, `Triage`, `Unidentified`, `Blocked`, `Not ready`, `Review`,
  `Held`, `Completed`, `Stale`, `Created in error`, `Associated`,
  `Awaiting information`, `Ready for case allocation`,
  `Needs text extraction`, `Unsupported`, `Failed`,
  `Vehicle images registered`); every state value and every date/time
  through the shared vocabulary list (Europe/London); no colour-only state;
  provenance icon with one-word tooltip (`Staff · Extracted · AI · E-mail ·
  Lookup · Principal · Automatic`); `AutomationProperties.AutomationId` on
  every interactive control.
- **Implementation boundaries**: `Pegasus.Desktop` (views, view models,
  navigation), `Pegasus.Desktop.Infrastructure` (API client, cache,
  credential store), `Pegasus.Contracts` (DTOs), `Pegasus.Web` (`/api/v1`
  group + tests), `Pegasus.Core` (only when a rule is moved in with a
  characterization test). Forbidden from the desktop: `Pegasus.Infrastructure`,
  EF Core, Azure/Box/Graph SDKs, WebView hosting Pegasus UI.
- **Verification ladder** per slice: Core characterization/unit tests
  (tier 2) → contract tests for each endpoint incl. authorization and
  failure paths (tier 5) → view-model tests → `winapp ui` script and
  `axe-windows` scan (tier 7) → performance budget where stated (tier 10)
  → UAT script and parity comparison against the web page on the same data
  (tier 12) → parity-matrix row update.
- **Rollback/compatibility**: the Razor page stays live; the `/api/v1`
  group is additive and feature-gated; a slice is rolled back by
  withholding the desktop release (or the gateway flag), never by a
  schema rollback (expand/contract, area 09).
- **Routing default**: `winui-dev` (`winui-dev-workflow`, `winui-design`,
  `winui-code-review`) · `pegasus-gateway-dev` (`dotnet-webapi`,
  `microsoft-code-reference`) · `pegasus-test-engineer`
  (`code-testing-agent`, `run-tests`, `test-gap-analysis`) ·
  `pegasus-desktop-reviewer` (independent review) · MCP Microsoft Learn,
  Kanmer. Slice-specific additions are listed below.

## Phase 3 — first read-only vertical slice

### S1 · Dashboard and work queue (DSK-05-01)

- **User outcome**: on launch the operator sees what needs attention now —
  assigned work, new/unassigned, overdue, integration failures, recent
  cases — and opens any item in one action (proposal §13.2, §14.3).
- **Current behaviour**: `Pages/Index.cshtml.cs` (43 lines, `OnGetAsync`)
  over `IDashboardQueries`; rail counts injected by
  `Presentation/RailCountsPageFilter.cs`; operations snapshot read parts of
  `Pages/Operations/Index.cshtml.cs` (236, `OnGetAsync`). Evidence:
  `tests/Pegasus.IntegrationTests/Browser/OperatorJourneyTests.cs`,
  dashboard boundary tests in `tests/Pegasus.Core.Tests`.
- **Target behaviour**: actionable lists and counts, no vanity charts; rail
  counts come from a dashboard endpoint; refresh is coalesced and
  `F5`/`Ctrl+R` driven; freshness time shown. Deliberate difference: no
  page-filter side channel; counts and lists are one query contract.
- **Placement**: shared authority yes (shared queues); unattended no;
  secrets no; callback no; central enforcement yes (role-aware lists);
  measured advantage n/a → desktop renders, gateway queries.
- **Data/API impact**: `dashboard` group (summary, rail counts, recent
  cases); `operations` read endpoints for failures needing attention;
  server paging for lists; no writes.
- **UI specification**: area 06 spec S1 (dashboard, status bar, navigation
  rail order Dashboard → Inbox → Upload → Queues → Cases → Operations →
  Administration).
- **Routing**: default + `pegasus-ui-verifier` (`winui-ui-testing`).
- **Boundaries**: default.
- **Acceptance**: five §14.3 questions answered from live data; counts
  equal the web rail counts for the same dataset; keyboard-only traversal.
- **Verification**: VM tests (loading/empty/error/success states); contract
  tests; `winapp ui` script; parity table web vs. desktop counts.
- **Documentation**: parity rows for dashboard + rail counts; FRD-13
  dashboard section; `DSK` capability row.
- **Rollback/compatibility**: default.
- **Absorbs upstream**: none directly; informs PLAT-029 (IA restructure).

### S2 · Case list and search (DSK-05-02)

- **User outcome**: find and open cases fast — server-paged list, sortable
  columns, dropdown filters, global search (`Ctrl+K`), saved column layout
  (proposal §13.3, §14.4, §14.7).
- **Current behaviour**: `Pages/Cases/Index.cshtml.cs` (261, `OnGetAsync`)
  over `ICaseQueryStore` search/list; `Pages/Search/Index.cshtml.cs` (29)
  redirects into Cases (search merged into Cases per design authority).
  Evidence: `CaseDetailsWebTests.cs` list portions, browser journey tests.
- **Target behaviour**: virtualized list, newest-first default, filters as
  dropdowns, column chooser, Enter/double-click opens; global search
  grouped by case / party / vehicle / document metadata as the gateway
  supports; recent items local only. Deliberate difference: no full-page
  reloads; local sort of the loaded page only.
- **Placement**: shared authority yes; central enforcement yes; rest no →
  gateway queries with paging/filter/sort; desktop renders and locally
  sorts a loaded page.
- **Data/API impact**: `cases` list/search endpoints with paging, sort and
  filter contracts; ETag on reads; no writes.
- **UI specification**: area 06 spec S2 (list density 32 px rows, status
  badges with text, filter pane show/hide).
- **Routing**: default + `optimizing-ef-core-queries` for the query path.
- **Boundaries**: default.
- **Acceptance**: first page of ordinary results ≤ 1 s (excluding provider
  outage) on the baseline workstation; result sets equal the web for the
  same filters; keyboard traversal of results.
- **Verification**: paging/filter/sort contract tests; VM tests; perf
  measurement; `winapp ui`.
- **Documentation**: parity rows; FRD-13 list/search section.
- **Rollback/compatibility**: default.
- **Absorbs upstream**: PLAT-029 (information-architecture restructure —
  the desktop IA is the answer), UICASE-001 (case screen improvements, list
  part).

### S3 · Case detail read-only and history (DSK-05-03)

- **User outcome**: open a case and read identity, parties, key dates,
  next action and history without editing (proposal §13.3, §14.5).
- **Current behaviour**: `Pages/Cases/Details.cshtml.cs` (654) `OnGetAsync`
  (query, edit lease state, completeness); action history via
  `IActionHistoryWriter`-backed queries; partials under
  `Pages/Cases/Shared/`. Evidence: `tests/Pegasus.IntegrationTests/CaseDetailsWebTests.cs`
  (1,286 lines).
- **Target behaviour**: stable case header (reference, status, assignee,
  priority, save state, commands) with sub-navigation Overview · Vehicle ·
  Assessment · Documents · Communications · Tasks · Reports · History;
  sections load lazily; only populated sections render; collapsible
  right-side activity pane. Deliberate difference: one container reachable
  without scrolling, per design authority density rules.
- **Placement**: shared authority yes; central enforcement yes → gateway
  serves case sections and history; desktop renders.
- **Data/API impact**: `cases/{id}` header + per-section endpoints
  (overview, history/audit, tasks summary); ETag per section; no writes.
- **UI specification**: area 06 spec S3 (case workspace shell).
- **Routing**: default.
- **Boundaries**: default.
- **Acceptance**: first useful view ≤ 200 ms perceived after header load
  (cached navigation budget); history rows equal the web for the same case.
- **Verification**: VM tests; contract tests; parity comparison; `winapp
  ui`.
- **Documentation**: parity rows (Details GET, history).
- **Rollback/compatibility**: default.
- **Absorbs upstream**: CASE-012 (Redesign the Case page workspace —
  read-only shell part), CASE-020 (read the case header from the case, not
  the intake draft — must be true before parity), UICASE-001 (detail part).

## Phase 4 — case editing and concurrency

### S4 · Case create (DSK-05-04)

- **User outcome**: create a case from an instruction draft or from blank,
  settle the inspection address, see provenance beside every field, and
  obtain the allocated reference (proposal §13.3, §13.4).
- **Current behaviour**: `Pages/Cases/Create.cshtml.cs` (689; `OnGetAsync`,
  `OnPostCreateAsync`) — the one place typed draft values are editable;
  candidates and provenance shown beside each box; a keyed value becomes a
  staff-sourced candidate (`docs/current-architecture.md` § QDOS
  applicability and drafts). Evidence: `QdosIntakeWebTests.cs`,
  `QdosAllocationRecoveryTests.cs`.
- **Target behaviour**: native create form with immediate field validation,
  server validation next to the section, deliberate Save; provenance icons;
  no hint text. Deliberate difference: draft retention is in-memory VM
  state with optional encrypted local draft (§11.1), not TempData.
- **Placement**: shared authority yes (reference allocation); central
  enforcement yes (principal identity, fail-closed allocation) → gateway
  allocates; desktop validates locally using Core rules where deterministic.
- **Data/API impact**: `cases` create command (idempotent by operation
  key); draft read endpoint from `received`; address resolution endpoint.
- **UI specification**: area 06 spec S4.
- **Routing**: default.
- **Boundaries**: default + characterization tests in Core for the
  draft-to-case mapping before any page-model rule moves.
- **Acceptance**: allocation outcomes (created / withheld / failed) match
  the web for the fixture set; replay of the same operation key returns the
  same result; no field hints.
- **Verification**: Core characterization tests; contract tests; VM tests;
  UAT script with the genuine corpus (tier 8, local only).
- **Documentation**: parity row; FRD-13 create section.
- **Rollback/compatibility**: default.
- **Absorbs upstream**: CASE-001 (dropped TempData status passing).

### S5 · Case edit with lease, version and completeness (DSK-05-05)

- **User outcome**: edit case details safely — claim the edit lease,
  renew, save with the expected version, confirm completeness, release
  (proposal §10.4, §13.3, §14.5).
- **Current behaviour**: `Pages/Cases/Details.cshtml.cs` handlers
  `OnPostClaimLeaseAsync`, `OnPostRenewLeaseAsync`,
  `OnPostReleaseLeaseAsync`, `OnPostConfirmCompletenessAsync`,
  `OnPostSaveAsync` over `Pages/Cases/CaseMutationPageModel.cs` (339);
  Core `IAcquireCaseEditLease`/`IRenewCaseEditLease`/`IReleaseCaseEditLease`
  (`Workflow/CaseCommandContracts.cs:77-91`), `CaseEditAuthority`. Evidence:
  `CaseWorkflowPersistenceTests.cs` (2,194), `CaseDetailsWebTests.cs`.
- **Target behaviour**: explicit dirty-state indicator; deliberate Save;
  navigation warns before discarding; lease renewed on a timer while the
  editor is open; lease holder disclosed when taken. Deliberate
  difference: no TempData-retained proposed values — unsaved edits live in
  the view model and optional local encrypted draft.
- **Placement**: shared authority yes; central enforcement yes (lease,
  version, audit) → gateway; desktop validates fields locally.
- **Data/API impact**: `cases/{id}/lease` claim/renew/release;
  `cases/{id}` update with `ExpectedVersion` + `EditLeaseToken`;
  completeness confirm command; 409 problems with current version.
- **UI specification**: area 06 spec S5.
- **Routing**: default.
- **Boundaries**: default.
- **Acceptance**: two-user conflict test — second writer gets a conflict
  with the current version and can reload/compare/reapply; no silent
  overwrite; lease loss surfaces immediately.
- **Verification**: two-user integration test (LocalDB); contract tests;
  VM tests; UAT.
- **Documentation**: parity rows; FRD-13 edit section.
- **Rollback/compatibility**: default.
- **Absorbs upstream**: CASE-012 (edit part), UICASE-001, CASE-021
  (refuse Review for a case with no images — gateway rule, must be true
  before parity).

### S6 · Workflow, closure and tasks commands (DSK-05-06)

- **User outcome**: hold/release hold, return to review, assign engineer,
  start work, record engineer finding, create linked replacement; record
  report approval, close, reopen, archive; add note, create/assign/
  complete/cancel task, record manual chase, link/unlink report evidence
  (proposal §13.3).
- **Current behaviour**: `Pages/Cases/Workflow.cshtml.cs` (227, seven
  `OnPost*` handlers), `Pages/Cases/Closure.cshtml.cs` (121, four),
  `Pages/Cases/Tasks.cshtml.cs` (248, eight); all PRG through
  `CaseMutationPageModel`. Core `Lifecycle/CaseLifecycle.cs` (629),
  `Tasks/`, `Workflow/`.
- **Target behaviour**: each command is an explicit, audited action with a
  reason dialog where the command requires one; never a generic "Close";
  permanent consequences visible without hover (e.g. "Created in error
  cannot be reopened. Create and link the replacement case.").
- **Placement**: shared authority yes; central enforcement yes → gateway
  commands; desktop builds and confirms.
- **Data/API impact**: `cases/{id}/workflow/*`, `cases/{id}/closure/*`,
  `cases/{id}/tasks/*` explicit commands, all with operation key, version,
  lease where applicable; never a generic execute endpoint.
- **UI specification**: area 06 spec S6 (command bar, reason dialog
  contract).
- **Routing**: default.
- **Boundaries**: default.
- **Acceptance**: every command has authorization and failure-path tests;
  product invariants hold (never delete a case; reopen needs a reason;
  principal/reference immutable).
- **Verification**: contract tests per command; VM tests; UAT.
- **Documentation**: parity rows for nineteen handlers.
- **Rollback/compatibility**: default.
- **Absorbs upstream**: CASE-002/CASE-004 remain future capabilities (not
  absorbed).

### S7 · Parties and reference data (DSK-05-07)

- **User outcome**: administrators maintain organizations and principals
  (create, update, replace a principal) and see provider reference data
  (proposal §13.6).
- **Current behaviour**: `Pages/Administration/Organizations/Index.cshtml.cs`
  (126), `Organizations/Edit` (146), `Principals/Index` (31),
  `Principals/Create` (137), `Principals/Replace` (199); Core
  `Cases/` organization/principal administration (operation key ≤ 100,
  `Cases/OrganizationAdministration.cs:274`), `ReferenceData/` catalogue.
- **Target behaviour**: admin-only native screens; principal replacement is
  an explicit command with consequence copy; reference data read-only.
- **Placement**: shared authority yes; central enforcement yes
  (`ManageOrganizationsAndPrincipals` right) → gateway.
- **Data/API impact**: `administration/organizations`,
  `administration/principals` commands; reference catalogue read endpoint.
- **UI specification**: area 06 spec S7.
- **Routing**: default.
- **Boundaries**: default.
- **Acceptance**: non-administrators receive 403 problems; replace never
  reuses a reference.
- **Verification**: authorization tests; contract tests; VM tests.
- **Documentation**: parity rows.
- **Rollback/compatibility**: default.
- **Absorbs upstream**: PLAT-028 (Redesign Organizations and Principals —
  desktop screen spec), TICK-034 (DATA-02) stays backlog.

### S8 · Concurrency UX (DSK-05-08)

- **User outcome**: when another user changed the case, the lease was
  lost, or a command was replayed, the operator understands what happened
  and can reload, compare and deliberately reapply (proposal §10.4, §16.1).
- **Current behaviour**: web surfaces `CaseVersionConflictException`,
  `CaseOperationConflictException`, lease-lost through page errors and
  retained proposed values (`CaseMutationPageModel.cs:36-80`).
- **Target behaviour**: one conflict-and-recovery pattern reused by every
  editor; idempotent replay shows the original outcome; uncertain outcomes
  (timeout after send) are resolved by re-query, never by blind retry.
- **Placement**: central enforcement yes → gateway returns typed problems;
  desktop implements the recovery UX.
- **Data/API impact**: problem types for version conflict (with current
  version), lease conflict (with holder), operation conflict, replayed;
  read-after-conflict endpoints.
- **UI specification**: area 06 spec S8 (UI state contract: stale version,
  lease lost, conflict-and-recovery, idempotent/replayed).
- **Routing**: default.
- **Boundaries**: default.
- **Acceptance**: two-user scripted scenario passes; no retry of
  non-idempotent commands without a fresh operation key decision by the
  operator.
- **Verification**: contract tests for each problem type; VM tests; UAT
  two-user script.
- **Documentation**: FRD-13 conflict section; parity matrix note.
- **Rollback/compatibility**: default.
- **Absorbs upstream**: KANMER-005 (exclusive leases between staff and
  Automation Actors — gateway rule, absorbed here).

## Phase 5 — intake and communications

### S9 · Received items — intake detail, actions and bytes (DSK-05-09)

- **User outcome**: review a received item (instruction or image), its
  classification evidence, field suggestions and extracted text; retry
  allocation, block, re-evaluate, correct the draft, claim the case lease,
  link or reverse-link a case, register vehicle images, dismiss a
  suggestion; open the source, assets and images (proposal §13.4, §13.7).
- **Current behaviour**: `Pages/Intake/Details.cshtml.cs` (613; ten
  handlers), `Pages/Intake/Asset.cshtml.cs` (80), `Image.cshtml.cs` (79),
  `Source.cshtml.cs` (78) returning bytes through Core
  `DownloadIntakeSource`; Core `Intake/` (`IGetIntake`, `IAllocateIntake`,
  `ILinkIntake`, `IReverseIntakeLink`, mutation commands). Evidence:
  `QdosIntakeWebTests.cs`, `IntakeStablePersistenceTests.cs`,
  `MultiFormatIntakeWebTests.cs` (1,429), `LocalIntakeAccessTests.cs`.
- **Target behaviour**: native review surface; operator vocabulary
  "Received item" (never "intake"); read-only typed draft here, editable
  only on create (S4); bytes streamed with progress. Deliberate difference:
  no separate status page round-trips.
- **Placement**: shared authority yes; central enforcement yes; secrets
  (artifact store) yes → gateway; desktop renders and streams.
- **Data/API impact**: `received/{id}` detail, ten explicit commands,
  `received/{id}/source|assets/{n}|images/{n}` byte endpoints with range
  and no-sniff; operation keys ≤ 100/200 as in Core.
- **UI specification**: area 06 spec S9.
- **Routing**: default + `minimal-api-file-upload` (byte handling).
- **Boundaries**: default.
- **Acceptance**: outcomes equal the web for the fixture set; blocked and
  withheld states carry the approved necessary copy only.
- **Verification**: Core characterization tests (link/unlink integrity,
  re-evaluation); contract tests; VM tests; genuine-corpus run locally
  (tier 8).
- **Documentation**: parity rows (ten handlers + three byte pages).
- **Rollback/compatibility**: default.
- **Absorbs upstream**: INTK-001 (honest queued upload status), INTK-027
  (re-evaluation after transient staging cleanup — gateway/worker fix),
  INTK-033 (stranded triage-request email — worker fix), INTK-004 (labels
  reconciled with code).

### S10 · Mail workspace (DSK-05-10)

- **User outcome**: browse retained mail by mailbox/folder with freshness,
  preview and open a message, link or unlink a case with confirmation,
  correct classification, move to the recommended folder; search Deleted
  Items (proposal §13.4, §13.8).
- **Current behaviour**: `Pages/Mail/Index.cshtml.cs` (428; `OnGetAsync`,
  `OnGetPreviewAsync` JSON at `:176`), `Pages/Mail/Message.cshtml.cs`
  (1,025; seven handlers) over Core `ListRetainedMail`, `GetRetainedMail`,
  `GetRetainedMailFreshness`, `SearchDeletedMail`, `ILinkIntake`,
  `IReverseIntakeLink`, classification correction, folder move (provider
  absent by default). Evidence: `MailWorkspaceWebTests.cs` (2,045),
  `RetainedMailPersistenceTests.cs` (1,696), `MailWorkspaceBrowserTests.cs`.
- **Target behaviour**: list + message pane; link/unlink confirmations
  carry "Unlinking this email cancels case <reference>." exactly; Deleted
  Items search capped at 100 newest via the gateway. Split into S10a
  list/preview, S10b message/link-unlink, S10c classify/move.
- **Placement**: shared authority yes; unattended (poll) yes — Worker;
  secrets (Graph) yes → gateway/worker; desktop renders.
- **Data/API impact**: `mail` list/preview/message/search endpoints;
  commands link/unlink/correct-classification/move; versions for
  classification/recommendation/mailbox; reason required on move.
- **UI specification**: area 06 spec S10 (approved `/Inbox/{id}` mockups
  under `docs/design/references/mockups/inbox-message-page/`).
- **Routing**: default.
- **Boundaries**: default.
- **Acceptance**: web and desktop show the same retained messages for the
  same scope; link/unlink outcomes identical; move control absent when the
  provider is absent.
- **Verification**: contract tests; VM tests; `winapp ui` dialog tests;
  parity vs `MailWorkspaceWebTests.cs` scenarios.
- **Documentation**: parity rows; FRD-13 mail section cites FRD-08.
- **Rollback/compatibility**: default.
- **Absorbs upstream**: AUTO-003 (expose completed email-workspace actions
  through the Automation Actor — same Core use cases; gateway side),
  MAIL-011/MAIL-012 fixes arrive via upstream sync.

### S11 · Triage (DSK-05-11)

- **User outcome**: work the triage queue — list, detail, source download,
  and every triage action (await information, record/supersede finding,
  response link/unlink, complete, cancel, reopen, case link/unlink and the
  remaining commands) with evidence (proposal §13.4).
- **Current behaviour**: `Pages/Triage/Index.cshtml.cs` (449),
  `Pages/Triage/Details.cshtml.cs` (496; `OnPostActionAsync` dispatching
  thirteen commands); Core `Triage/` (lifecycle 561 lines); MCP
  `TriageMcpTools.cs` names ten mutations.
- **Target behaviour**: explicit per-action commands (no dispatcher
  string); `Triage` keeps its settled meaning; "Assign to me" replaced by
  Engineer selection (upstream INTK-019).
- **Placement**: shared authority yes; central enforcement yes → gateway.
- **Data/API impact**: `triage` list/detail/source + thirteen commands with
  `ExpectedVersion`; `TriageVersionConflictException` → 409.
- **UI specification**: area 06 spec S11.
- **Routing**: default.
- **Boundaries**: default + characterization of the action matrix in Core.
- **Acceptance**: every action has contract and authorization tests; the
  three commands not named by MCP are enumerated and covered.
- **Verification**: Core tests; contract tests; VM tests; UAT.
- **Documentation**: parity rows.
- **Rollback/compatibility**: default.
- **Absorbs upstream**: INTK-019.

### S12 · Unidentified and vehicle images (DSK-05-12)

- **User outcome**: list and resolve Unidentified items; list and close
  vehicle-image registrations with VRM suggestions and candidate cases
  (proposal §13.4, §13.5).
- **Current behaviour**: `Pages/Unidentified/Index.cshtml.cs` (19),
  `Details.cshtml.cs` (180; `OnPostResolveAsync`);
  `Pages/ImageIntake/Index.cshtml.cs` (85), `Details.cshtml.cs` (89;
  `OnPostCloseAsync`); Core `Intake/Unidentified/` (operation key ≤ 200,
  `UnidentifiedContracts.cs:398`), `ImageIntake/`, `IImageIntakeQueries`.
- **Target behaviour**: vocabulary "Unidentified" (never "Needs sorting")
  and "Vehicle images" / "Image reference"; resolve and close are explicit
  reasoned commands.
- **Placement**: shared authority yes → gateway.
- **Data/API impact**: `unidentified` list/detail/resolve/source;
  `image-intake` list/detail/close.
- **UI specification**: area 06 spec S12.
- **Routing**: default.
- **Boundaries**: default.
- **Acceptance**: counts exclude receipts that produced a case, as today.
- **Verification**: contract tests; VM tests.
- **Documentation**: parity rows.
- **Rollback/compatibility**: default.
- **Absorbs upstream**: none.

### S13 · Uploads — manual, status, groups (DSK-05-13)

- **User outcome**: upload one file (≤ 10 MiB; `.eml .pdf .docx .doc .msg
  .jpg .jpeg .png`) by drag-and-drop or picker, follow its status, and
  register/attach upload groups (proposal §13.4, §13.7).
- **Current behaviour**: `Pages/Upload.cshtml.cs` (183; `OnPostAsync`),
  `Pages/UploadStatus.cshtml.cs` (83), `Pages/UploadGroupStatus.cshtml.cs`
  (225; register/attach group); limits from `IntakeEnvelopeLimits`
  (`Program.cs:525-530`); `Pages/Uploads/Request.cshtml.cs` stays web.
- **Target behaviour**: upload queue with progress and cancel; client-side
  limit check mirrors server; status polled, never assumed complete.
- **Placement**: shared authority yes; secrets (artifact store) yes →
  gateway; desktop streams.
- **Data/API impact**: `uploads` stage (multipart), status, group
  register/attach; server enforces limits before Core.
- **UI specification**: area 06 spec S13.
- **Routing**: default + `minimal-api-file-upload`, `winui-ui-testing`
  (file picker automation).
- **Boundaries**: default.
- **Acceptance**: 10 MiB + 64 KiB envelope enforced; receipt token replay
  returns the existing receipt.
- **Verification**: contract tests incl. limits; VM tests; `winapp ui` file
  picker script.
- **Documentation**: parity rows.
- **Rollback/compatibility**: default.
- **Absorbs upstream**: INTK-001 (status honesty) shared with S9.

## Phase 6 — documents, Box and vehicle services

### S14 · Documents and custody (DSK-05-14)

- **User outcome**: browse the case's Box-backed documents, upload with
  progress/cancel/retry, preview where safe, export, remove per
  permissions, retry failed custody, create/revoke request-upload links
  (proposal §12.2, §13.7, §14.6).
- **Current behaviour**: `Pages/Cases/Custody.cshtml.cs` (270; six
  handlers), `Pages/Cases/Documents/Download.cshtml.cs` (112),
  `Documents/Export.cshtml.cs` (160); Core `Custody/`, `Documents/`,
  `ICaseCustody`, `IDocumentContentStore`; Infrastructure
  `BoxCaseCustody.cs`. Evidence: `CustodyOutboxIntegrationTests.cs`
  (1,796).
- **Target behaviour**: native folder/file list; transfer queue; clear
  local-temporary vs. canonical-Box distinction; no hidden overwrite;
  direct transfer only if Box issues short-lived constrained URLs (area
  07 decides from `Box.Sdk.Gen`), otherwise stream through the gateway.
- **Placement**: shared authority yes; secrets (Box) yes; central
  enforcement yes → gateway brokers; desktop transfers and previews.
- **Data/API impact**: `cases/{id}/documents` list/download/export/upload
  session/remove; custody retry; request-link create/revoke; ETag/range on
  downloads.
- **UI specification**: area 06 spec S14.
- **Routing**: default + `pegasus-ui-verifier`.
- **Boundaries**: default; temporary files with per-user ACLs and bounded
  retention (area 10).
- **Acceptance**: large and interrupted transfers recover safely; no Box
  secret in the package; evidence gallery reads document records.
- **Verification**: contract tests; VM tests; transfer failure tests; perf
  (transfer does not block navigation).
- **Documentation**: parity rows; ADR-0107 consumed.
- **Rollback/compatibility**: default.
- **Absorbs upstream**: DOCS-011 (preview evidence with paging/download),
  DOCS-012 (evidence on Evidence tab, not custody ledger), PLAT-039 and
  PLAT-041 (gateway-side Box fixes via upstream sync).

### S15 · Vehicle lookup and EVA handoff (DSK-05-15)

- **User outcome**: request a DVLA/DVSA lookup, accept a suggestion with
  source and timestamp, and generate/download the EVA handoff bundle
  (proposal §12.3, §13.5).
- **Current behaviour**: `Pages/Cases/Vehicle.cshtml.cs` (149; three
  handlers), `Pages/Cases/Eva/Download.cshtml.cs` (99); Core `Vehicle/`,
  `Eva/EvaBundleSchema.cs`; Infrastructure DVLA/DVSA adapters incl. replay;
  Worker reconciliation sweep enqueues lookups.
- **Target behaviour**: normalized registration input; provider failure
  distinct from not-found; cached-lookup freshness shown; EVA bundle
  generate/download as explicit commands.
- **Placement**: secrets (DVLA/DVSA keys) yes; shared cache yes → gateway;
  desktop triggers and displays.
- **Data/API impact**: `vehicles/{registration}/lookup` request/accept;
  `cases/{id}/eva/handoff` generate/download.
- **UI specification**: area 06 spec S15.
- **Routing**: default.
- **Boundaries**: default.
- **Acceptance**: provider states distinguishable; no provider secret in
  the package; replay adapter works in the Test/UAT stack.
- **Verification**: contract tests with replay adapter; VM tests.
- **Documentation**: parity rows; ADR-0107.
- **Rollback/compatibility**: default.
- **Absorbs upstream**: ENG-013 (already upstream); ENG-009 (Cazana
  valuation from workbench) stays backlog.

### S16 · Images and gallery (DSK-05-16)

- **User outcome**: a reusable image gallery across image-bearing screens
  with progressive thumbnails that never block navigation (proposal §13.7,
  §15.2).
- **Current behaviour**: `Pages/Shared/_ImageGallery.cshtml`,
  `Presentation/GalleryImage.cs`, receipt-asset image endpoints.
- **Target behaviour**: decode to display size; bounded thumbnail cache;
  dispose promptly; keyboard traversal; alt text from metadata.
- **Placement**: desktop renders; gateway serves bytes.
- **Data/API impact**: image byte endpoints with size hints and ETag.
- **UI specification**: area 06 spec S16.
- **Routing**: default + `pegasus-ui-verifier` (`analyzing-dotnet-performance`).
- **Boundaries**: default.
- **Acceptance**: thumbnail display progressive; memory steady after
  repeated navigation.
- **Verification**: VM tests; perf/memory measurement; `winapp ui`.
- **Documentation**: parity rows.
- **Rollback/compatibility**: default.
- **Absorbs upstream**: CASE-011 (reusable image gallery viewer).

## Phase 7 — assessment and reports

### S17 · Assessment workbench (DSK-05-17)

- **User outcome**: record damage, import an estimate, accept the repair
  specification, reconcile, with mileage/source prefilled from lookup
  evidence (proposal §13.9).
- **Current behaviour**: `Pages/Cases/Assessment/Index.cshtml.cs` (740;
  `OnPostSaveDamageAsync`, `OnPostImportEstimateAsync`,
  `OnPostAcceptSpecificationAsync`, `OnPostReconcileAsync`, plus report
  handlers in S18); Core `Assessment/AssessmentPolicy.cs` (499),
  `ICaseAssessmentStore`; Infrastructure `AudatexEstimatePdfParser.cs`.
- **Target behaviour**: native workbench split S17a damage, S17b estimate
  import/accept, S17c reconcile; deterministic calculations run locally
  through Core with server recheck on write.
- **Placement**: interactive calculation desktop; authoritative write and
  estimate parsing gateway.
- **Data/API impact**: `cases/{id}/assessment` get/save/import (multipart)/
  accept/reconcile with versions and operation keys.
- **UI specification**: area 06 spec S17.
- **Routing**: default.
- **Boundaries**: default + characterization tests on assessment rules.
- **Acceptance**: figures equal the web for the fixture set; engineer-only
  confirmations enforced server-side.
- **Verification**: Core tests; contract tests; VM tests; UAT by an
  Engineer.
- **Documentation**: parity rows; FRD-13 assessment section (cites FRD-06,
  FRD-11).
- **Rollback/compatibility**: default.
- **Absorbs upstream**: none in parity scope (UI-15 workbench stays
  backlog).

### S18 · Report generation, preview, finalise, send (DSK-05-18)

- **User outcome**: generate the assessment report and fee note locally,
  preview, finalise (canonical copy registered centrally), send with
  idempotency (proposal §12.5, §13.9; L-03).
- **Current behaviour**: `Pages/Cases/Assessment/Index.cshtml.cs`
  `OnPostGenerateReportDraftAsync`, `OnPostSendAsync`; Core
  `Reports/AssessmentReportProjection.cs`, `AssessmentReportRendering.cs`
  (`IAssessmentReportRenderer`); Infrastructure
  `PlaywrightAssessmentReportRenderer.cs`. Evidence:
  `tests/Pegasus.IntegrationTests/Reports/AssessmentReportRendererTests.cs`,
  `AssessmentReportDraftWebTests.cs`.
- **Target behaviour**: projection from the gateway; render on the desktop
  through the isolated WebView2 HTML→PDF path (area 07, ADR-0108) with the
  same Scriban templates and `report.css`; PDFsharp post-processing;
  preview; finalise uploads the canonical PDF and registers it; gateway
  renderer retained until golden-file parity passes.
- **Placement**: interactive generation desktop (measured advantage,
  §4.1); canonical storage, audit and send gateway.
- **Data/API impact**: `cases/{id}/reports` projection, finalise (upload +
  register), send (idempotency key, audited provider message id).
- **UI specification**: area 06 spec S18.
- **Routing**: default + `microsoft-code-reference` (WebView2
  `PrintToPdfAsync`).
- **Boundaries**: `Pegasus.Desktop.Infrastructure` hosts the renderer;
  WebView2 never hosts Pegasus UI (architecture test).
- **Acceptance**: golden-file comparison against the Playwright output for
  the fixture catalogue; WebView2 runtime absent → guided message and
  gateway fallback.
- **Verification**: golden-file tests; contract tests; VM tests;
  performance on baseline hardware.
- **Documentation**: ADR-0108; parity rows; FRD-11 cross-reference.
- **Rollback/compatibility**: gateway renderer stays until parity; feature
  flag selects the renderer.
- **Absorbs upstream**: DOCS-001, TICK-206/208/216, TICK-081/096/097/100 as
  report-decision inputs (area 07).

## Phase 8 — administration and hardening

### S19 · Administration (DSK-05-19)

- **User outcome**: administrators manage configuration, mail categories,
  approved mailboxes (update, resolve folders), access review, staff
  accounts (create, disable), roles, automation clients (enable, Send-to-AI
  toggle, connector, channel token rotate/clear) and view automation
  activity (proposal §13.10).
- **Current behaviour**: sixteen `Pages/Administration/*` models
  (`Configuration` 128, `MailCategories` 74, `Mailboxes` 362,
  `Access/Index` 102, `Accounts/Index` 102, `Accounts/Edit` 96,
  `Roles/Index` 135, `Automation/Index` 260, `Automation/Activity` 73, …)
  on `AdministrationPageModel`; Core `Identity/` administration,
  workflow configuration, approved mailboxes (ADR-0022/0024).
- **Target behaviour**: admin-only native screens; every mutation audited;
  consolidated accounts/roles/access review where upstream asked.
- **Placement**: central enforcement yes (`ManageStaffAccounts`,
  `ReviewStaffAccess`, `AssignStaffRoles`, `ManageWorkflowConfiguration`,
  `ManageApprovedMailboxes`, `ManageApprovedOutlookCategories`,
  `ManageAutomationClients`) → gateway.
- **Data/API impact**: `administration/*` commands and reads.
- **UI specification**: area 06 spec S19.
- **Routing**: default.
- **Boundaries**: default.
- **Acceptance**: 403 for non-administrators on every endpoint; audit
  rows for sensitive operations.
- **Verification**: authorization tests; contract tests; VM tests; UAT.
- **Documentation**: parity rows (sixteen models).
- **Rollback/compatibility**: default.
- **Absorbs upstream**: PLAT-025 (workflow configurations), PLAT-026
  (approved mailboxes), PLAT-027 (consolidate accounts/roles/access),
  AUTO-006 (automation workspace), AUTO-007 (AI settings), PR-026
  (Outlook category administration reconciliation).

### S20 · Operations and integration health (DSK-05-20)

- **User outcome**: see retryable external work, active upload links,
  integration health (Graph worker last cycle, Box, DVLA/DVSA, feed,
  minimum client version), retry or revoke (proposal §13.10, §18.3).
- **Current behaviour**: `Pages/Operations/Index.cshtml.cs` (236;
  `OnPostRetryExternalAsync`, `OnPostRevokeLinkAsync`); Core `Operations/`;
  health endpoints `/health/live`, `/health/ready`.
- **Target behaviour**: native operations screen; health described without
  secrets; retries explicit and audited.
- **Placement**: shared authority yes → gateway; desktop displays.
- **Data/API impact**: `operations` snapshot, retry-external, revoke-link;
  admin health endpoint (area 10).
- **UI specification**: area 06 spec S20.
- **Routing**: default.
- **Boundaries**: default.
- **Acceptance**: failures visible and recoverable in the E2E scenario 13.
- **Verification**: contract tests; VM tests.
- **Documentation**: parity rows.
- **Rollback/compatibility**: default.
- **Absorbs upstream**: PLAT-023 (Redesign the Operations workspace).

### S21 · Password change and account lifecycle (DSK-05-21)

- **User outcome**: change password (including must-change-on-next-login),
  understand a disabled account, sign out (proposal §8.4, §13.1).
- **Current behaviour**: `Pages/Account/PasswordChange.cshtml.cs` (189),
  `SignOut.cshtml.cs` (21), `MustChangePassword` middleware
  (`Program.cs:875-899`); Core `Identity/` password change.
- **Target behaviour**: native flow driven by the session problem types
  from area 04; refresh tokens invalidated on change.
- **Placement**: central enforcement yes → gateway.
- **Data/API impact**: `session` password-change and logout endpoints.
- **UI specification**: area 06 spec S21.
- **Routing**: default.
- **Boundaries**: default.
- **Acceptance**: change invalidates other sessions; disabled account
  message exact.
- **Verification**: contract tests; security tests (area 10); VM tests.
- **Documentation**: parity rows.
- **Rollback/compatibility**: default.
- **Absorbs upstream**: none.

### S22 · Hardening sweep (DSK-05-22)

- **User outcome**: every slice meets the accessibility, performance and
  security baselines on the baseline workstation (proposal §14.9, §15,
  §17, Phase 8).
- **Current behaviour**: browser lane accessibility tests
  (`Browser/AccessibilityTests.cs`) cover the web only.
- **Target behaviour**: `axe-windows` scan + `winapp ui` suite + keyboard
  walkthrough + Narrator smoke + 200 % scale + high contrast per screen;
  performance regression report; security checklist.
- **Placement**: desktop.
- **Data/API impact**: none.
- **UI specification**: area 06 keyboard-and-accessibility.
- **Routing**: `pegasus-ui-verifier` (`winui-ui-testing`,
  `analyzing-dotnet-performance`) · `pegasus-desktop-reviewer`
  (`winui-code-review`) · `pegasus-test-engineer`.
- **Boundaries**: fixes land in the owning slice's projects.
- **Acceptance**: zero critical accessibility issues; budgets met; no
  unresolved high-risk security finding.
- **Verification**: scan reports and perf report attached to the ticket
  proof.
- **Documentation**: performance baseline in area 10; parity matrix
  complete.
- **Rollback/compatibility**: n/a.
- **Absorbs upstream**: PLAT-015 (operator copy aligned with the design
  authority), PLAT-005 (screenshots from a local run — desktop
  screenshots via `winapp ui screenshot`).
