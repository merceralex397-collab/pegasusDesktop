# Open questions — FND-018 (DSK-01-05)

The ticket body step 12 instructs: "tick `open-questions/`, then `get_doc_gates` and move to
Done." These are those items.

This is a `spike`. Its `research` document **is** the deliverable and writing it satisfies the
`enter-done` gate on its own — so the unticked boxes below are what actually keep an unfinished
spike from being closed. For a `spike` an unticked box blocks **`enter-done` and nothing else**
(`get_doc_gates FND-018` lists `enter-done` as this profile's only gated boundary); it does not
gate `leave-backlog`, and the ticket can move through Preparing, Implementing, Review and
Verifying with these open.

Each item is tickable when its output is recorded in `research`, replacing the matching
`NOT YET CAPTURED` block.

## Must be answered before this spike is Done

- [ ] **U-1 — The row-by-row citation table is written into `research`.** One table for the 15
      owned rows: entry point(s) → handlers (`path:line`) → guarding `StaffAccessRight`
      (`path:line`) → Core owner (`path:line`) → FRD owner → test file or `gap:` → upstream
      redesign id in full `upstream:<ID>` form → §4.1 placement → inventoried-at SHA.
- [ ] **U-2 — Every handler and every administration page model is accounted for.** Run the
      research F-1 grep (expect **39** handlers),
      `git ls-files 'src/Pegasus.Web/Pages/Administration/*.cshtml.cs' | wc -l` (expect **15** —
      **not** the `**/` spelling, measured at **11**), and
      `git grep -n "public .*On\(Get\|Post\)" -- 'src/Pegasus.Web/Pages/Cases/Assessment'`
      (expect **7**). Show **ten** administration page models on `PAR-32`…`PAR-39` per research
      F-3's map, and the other five (`Organizations/Index`, `Organizations/Edit`,
      `Principals/Index`, `Principals/Create`, `Principals/Replace`) **cross-referenced to
      `PAR-40`/`PAR-41`, which [[FND-016]] owns** — they are covered, so they are not a finding,
      and their cells are not filled here. The body's "eight rows against fifteen page models"
      is really eight rows against ten.
- [ ] **U-3 — `PAR-14` is filled.** Read `src/Pegasus.Web/Pages/Cases/Vehicle.cshtml.cs` (3
      handlers at `:24`, `:46`, `:87`) and `src/Pegasus.Core/Vehicle/`; record the request→accept
      workflow, that the live adapter is Worker-owned
      (`src/Pegasus.Infrastructure/Vehicle/DvlaDvsaProductionAdapter.cs`), and that provider
      outage must stay distinguishable from not-found (proposal § 16.2). Locate its test evidence
      with `git grep -rln "VehicleLookup\|Dvla\|Dvsa" tests/` — the matrix currently says
      `to locate`.
- [ ] **U-4 — `PAR-18` is filled.** Read `src/Pegasus.Web/Pages/Cases/Eva/Download.cshtml.cs`
      (`OnPostAsync :21`) and `src/Pegasus.Core/Eva/EvaBundleSchema.cs` (916 lines); record that
      revisions are frozen and that the download requires a reason, citing where each is enforced.
- [ ] **U-5 — `PAR-15` is filled and assumption A-01-05-4 is settled.** Record all seven handlers
      (research F-1 gives their lines, noting `OnPostSaveDamageAsync :184` precedes
      `OnGetAsync :246`), the policy owner
      `src/Pegasus.Core/Assessment/AssessmentPolicy.cs:19` (499 lines), the estimate importer, and
      today's rendering path `src/Pegasus.Infrastructure/Reports/PlaywrightAssessmentReportRenderer.cs`.
      Run `git grep -n "IAssessmentReportRenderer" src/Pegasus.Web src/Pegasus.Infrastructure` to
      confirm it is the only production path. Mark the native-screen cell explicitly "Phase 7;
      rendering local via WebView2 per L-03 / ADR-0108" — **record the decision, do not design it**.
- [ ] **U-6 — `PAR-27` is filled** (after U-11 confirms ownership). Read
      `src/Pegasus.Web/Pages/Operations/Index.cshtml.cs` — 3 handlers (`:57`, `:71`, `:112`),
      `[Authorize]` at `:13` and `[ValidateAntiForgeryToken]` at `:15` — and
      `src/Pegasus.Core/Operations/`; cite `upstream:PLAT-023` in full form.
- [ ] **U-7 — `PAR-32`…`PAR-39` are filled and assumption A-01-05-2 is settled.** Research F-4
      measures that only five of the ten administration page models name a `StaffAccessRight` at
      the page — `Administration/Index.cshtml.cs:32` (`ManageStaffAccounts`),
      `Configuration.cshtml.cs:47,:59` (`ManageWorkflowConfiguration`),
      `Mailboxes.cshtml.cs:52,:65,:174` (`ManageApprovedMailboxes`),
      `Automation/Index.cshtml.cs:52,:64,:103,:135,:175,:214` and `Automation/Activity.cshtml.cs:32`
      (`ManageAutomationClients`). The other five carry only
      `[Authorize(Policy = StaffRoleNames.Administrator)]`
      (`MailCategories.cshtml.cs:9`, `Access/Index.cshtml.cs:8`, `Accounts/Index.cshtml.cs:8`,
      `Accounts/Edit.cshtml.cs:8`, `Roles/Index.cshtml.cs:8`). Find the right for `PAR-34`,
      `PAR-36`, `PAR-37` and `PAR-38` in Core —
      `src/Pegasus.Core/Identity/StaffAccountAdministration.cs`,
      `src/Pegasus.Core/Intake/ApprovedOutlookCategories.cs` — and cite `path:line`. Also cite
      ADR-0022 and ADR-0024 on the `PAR-35` Mailboxes row, and `upstream:PLAT-025`,
      `upstream:PLAT-026`, `upstream:PLAT-027`, `upstream:AUTO-006`, `upstream:AUTO-007` on the
      rows they redesign.
- [ ] **U-8 — `PAR-43`, `PAR-45` and `PAR-46` are filled.** `PAR-46` must state **35**, produced
      by the grep and not copied from the plan, showing the arithmetic: research F-8 measures
      `git grep -c "McpServerTool" src/Pegasus.Web/Mcp/` summing to **42** across seven files,
      minus the seven `[McpServerToolType]` class attributes = **35**, independently confirmed by
      `git grep -oh 'pegasus_[a-z_]*' src/Pegasus.Web/Mcp/ | sort -u | wc -l` → 35. Record that the
      projection is a **reference**, not a complete mirror: `assign` and `unassign` exist on the
      Triage page with no MCP counterpart (research F-8a). `PAR-45` must record
      `src/Pegasus.Web/Program.cs:939`, `:945`, `:954`, cite
      `tests/Pegasus.IntegrationTests/ReadinessEndpointTests.cs`, note that
      `GET /api/v1/client-compatibility` is **new work owned by area 04, not an existing
      endpoint**, and record the `IsMachineSurface` predicate (`Program.cs:973-977`) — the desktop
      is a program and receives a status code and a parsable body, never a re-executed HTML card.
      `PAR-43` maps to the area 06 error and empty-state catalogue, not to a screen.
- [ ] **U-9 — Every `to locate` cell in the owned rows is resolved.** `git grep -rln` over
      `tests/` per row, **opening each candidate from research F-11 before citing it**. Where
      nothing asserts the behaviour, write `gap: <untested behaviour>` and copy the line into
      `research` under a `### Gap list for DSK-01-12` heading for [[FND-025]].
- [ ] **U-10 — The matrix edits are made and the documentation gate passes.** Rows to
      `inventoried`; the SHA stamped on every touched row; every upstream redesign cell written
      as `upstream:<ID>` and **never bare** — a bare `<PREFIX>-<nnn>` on this board is a fork
      board id, and research F-9 confirms that board `PLAT-023`/`PLAT-025`/`PLAT-026`/`PLAT-027`
      are the seeded conversion tickets `DSK-11-05`/`DSK-11-07`/`DSK-11-08`/`DSK-11-09` while
      board `PLAT-028` is the imported **upstream `PLAT-032`**, a different ticket entirely; no
      `~` name promoted; no UAT owner filled. Then
      `pwsh ./scripts/Test-DocumentationLinks.ps1` — exit 0.
- [ ] **U-11 — Ownership of `PAR-27` is settled with [[FND-017]] before either ticket edits the
      row.** Research F-2 records the double claim: [[FND-017]] (plan handle `DSK-01-04`) states
      its rows as `PAR-19`–`PAR-31`, which sweeps `PAR-27` in, but none of its twelve steps
      mentions it, its step-11 `to locate` list omits it, and `PAR-27` is capability group
      **13.10** — not one of [[FND-017]]'s 13.4/13.7/13.8. **This ticket has the explicit claim**
      (its *What*, its step 6 and its acceptance criteria). Confirm and record the agreement;
      do not leave the row unfilled by both, and do not let it be filled twice.
- [ ] **U-12 — A-01-05-2's fail-open case is checked.** If any of `ManageApprovedOutlookCategories`,
      `ReviewStaffAccess`, `AssignStaffRoles` or `ManageStaffAccounts` turns out never to be
      checked for its page — neither at the page nor in the Core use case — that is a fail-open
      on an administration surface. Say so plainly and escalate to the operator; it is not a
      matrix cell, and it must not be papered over by writing the guessed right into the row.
- [ ] **U-13 — A-01-05-1 is confirmed against [[FND-014]]'s difference lists.** Read
      [[FND-014]]'s (plan handle `DSK-01-01`) research with `get_ticket_doc`. If its difference
      list (a) names a §13.5, §13.9 or §13.10 page model with no `PAR` row, add that row here
      using the matrix's exact ten columns. If the list is empty, say so.

## Parked (explicitly deferred)

- **Whether the matrix gains an explicit cloud-placement column.** [[FND-015]] F-13 measures the
  matrix at ten columns with no placement column, while
  `docs/desktop/01-inventory-and-parity/README.md:37-38` speaks of "the placement column".
  **Default taken, recorded in `research` § Execution placement:** write the proposal § 4.1 value
  as a leading `Placement: <value> (proposal §4.1)` clause inside the existing "Native screen/use
  case" cell — the same default [[FND-015]], [[FND-016]] and [[FND-017]] took, so all four
  row-population tickets stay consistent. Deferred because a schema change would touch all 46 rows
  under four sets of hands; raise it at review, and if a column is wanted it is board hygiene for
  [[FND-052]]. The § 4.1 values for these rows need no decision: `Interactive report generation`
  → Mostly desktop; `DVLA/DVSA lookup` → Split; `Audit trail` → Cloud required; `Native UI,
  navigation and state` → Desktop
  (`docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md:140-162`).
- **Whether `/api/v1` should follow the pages or the 35-tool MCP projection.** Research F-8a
  records that the projection is a reference and not a mirror. Deciding the endpoint set is area
  03's endpoint map; the `~` names stay indicative until it is approved
  (`parity-matrix.md` § Notes).
- **The `AdministrationPageModel` inheritance of `Connect/Authorize.cshtml.cs`** (research
  F-3a). That page is `PAR-42`, owned by [[FND-015]] and `legacy path retained`; it is not one of
  the fifteen and not this ticket's to act on.
- **Assigning a UAT owner.** The operator assigns one per capability group before any row passes
  `automated verification passed`; guessing is a defect.

## Explicitly not an open question

- **Send to AI (upstream `TICK-102` / capability `AI-09`), reached through
  `Administration/Automation/Index.OnPostSetSendToAiEnabledAsync` (`:95`) on `PAR-39`.** It is a
  **recorded exclusion with a reactivation condition**, not an unresolved conflict:
  `src/Pegasus.Web/AiWork/SendToAi.cs:12` defines `Features:SendToAi`, `:35-42` refuse to compose
  it outside the `DevelopmentOffline` runtime profile, and `src/Pegasus.Web/Program.cs:104-110`
  permits that profile only in Development — so it has never been operator-reachable in
  production and there is nothing to reach parity with.
  `docs/desktop/05-implementation-and-migration/reuse-map.md:38` marking `AiWork/` "gated, out of
  parity scope" is correct. `PAR-39` inventories the handler as it stands and cites
  `upstream:AUTO-007` for the redesign; it must not be turned into desktop scope. The reactivation
  condition is the separate non-preview transport decision named at `docs/capabilities.md:269`,
  and recording it belongs to [[FND-022]] (plan handle `DSK-01-09`) step 10. **No
  `open-questions` item is created for it on any ticket.**
