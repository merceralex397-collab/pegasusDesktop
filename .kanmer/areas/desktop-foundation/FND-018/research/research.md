# Research — FND-018: Parity rows for §13.5 vehicle, §13.9 assessment and reports, §13.10 administration

> **STATUS — INCOMPLETE. Do not move this ticket to Done yet.**

This is a `spike`. Its `research` document is the spike's **output**, not an input to it, and
its existence alone satisfies the `enter-done` gate (`get_doc_gates FND-018`: `enter-done`
needs `research` + `questions-resolved`, and it is this profile's only gated boundary). This
file was written **before the spike ran**, as the pre-work scaffold the authoring contract
requires. Everything under **Facts** is verified against the repository with the command that
produced it. Everything marked `NOT YET CAPTURED` is still owed and has an unticked box in this
ticket's `open-questions` document — those boxes, not this banner, block `enter-done`.

Baseline: `git rev-parse HEAD` → `bbd1c54959e8c3a361d3f73965b61d6e4aff59ec`, read 2026-08-24.

**Dependency, not an open question:** written against the confirmed skeleton of [[FND-014]]
(plan handle `DSK-01-01`). Read its `research` with `get_ticket_doc` before writing a cell.

**Read before writing any id:** the group document `HZN-001` / `board-conventions.md`
§ *Upstream ids versus board ids* (`get_group_doc HZN-001 board-conventions.md`). See F-9.

## Question

For the last un-inventoried capability groups — `PAR-14`, `PAR-15`, `PAR-18`, `PAR-27`,
`PAR-32`…`PAR-39`, `PAR-43`, `PAR-45`, `PAR-46` — what are the exact handlers, the right that
guards each administration surface, the Core owners, the real `pegasus_*` tool count, and which
upstream redesign ticket each native-screen cell must cite in full `upstream:<ID>` form?

## Current behaviour

Rows owned, with handler counts measured at `bbd1c549`:

| Row | §13.x | Entry point(s) | Handlers | Status today |
| --- | --- | --- | --- | --- |
| `PAR-14` | 13.5 | `Cases/Vehicle.cshtml.cs` (149) | 3 | `not inventoried` |
| `PAR-15` | 13.9 | `Cases/Assessment/Index.cshtml.cs` (740) | 7 | `inventoried` |
| `PAR-18` | 13.5 | `Cases/Eva/Download.cshtml.cs` (99) | 1 | `not inventoried` |
| `PAR-27` | 13.10 | `Operations/Index.cshtml.cs` (236) | 3 | `not inventoried` |
| `PAR-32` | 13.10 | `Administration/Index.cshtml.cs` (35) | 1 | `inventoried` |
| `PAR-33` | 13.10 | `Administration/Configuration.cshtml.cs` (128) | 2 | `not inventoried` |
| `PAR-34` | 13.10 | `Administration/MailCategories.cshtml.cs` (74) | 2 | `not inventoried` |
| `PAR-35` | 13.10 | `Administration/Mailboxes.cshtml.cs` (362) | 3 | `not inventoried` |
| `PAR-36` | 13.10 | `Administration/Access/Index.cshtml.cs` (102) | 2 | `not inventoried` |
| `PAR-37` | 13.10 | `Administration/Accounts/Index.cshtml.cs` (102), `Accounts/Edit.cshtml.cs` (96) | 2 + 2 | `not inventoried` |
| `PAR-38` | 13.10 | `Administration/Roles/Index.cshtml.cs` (135) | 2 | `not inventoried` |
| `PAR-39` | 13.10 | `Administration/Automation/Index.cshtml.cs` (260), `Automation/Activity.cshtml.cs` (73) | 6 + 1 | `not inventoried` |
| `PAR-43` | Web shell | `Error.cshtml.cs` (41), `StatusCode.cshtml.cs` (89) | 1 + 1 | `inventoried` |
| `PAR-45` | 13.10 (health) | `Program.cs:939`, `:945`, `:954` | n/a (route registrations) | `inventoried` |
| `PAR-46` | 13.10 (MCP reference) | `src/Pegasus.Web/Mcp/*McpTools.cs` | n/a (35 tools) | `inventoried` |

Today: vehicle lookup is a request→accept workflow whose live adapter is Worker-owned; EVA
bundles are generated and downloaded with a reason against a frozen schema; assessment runs
through a large Core policy and renders reports server-side with Playwright; the Operations
workspace retries external work and revokes upload links; administration is ten Razor pages
guarded either by an explicit `StaffAccessRight` check or by an `Administrator` policy
attribute; the shell error pages re-execute away from machine surfaces; health, version and the
MCP tool projection are non-Razor surfaces.

## Findings

- **The body's "eight rows against fifteen page models" is really eight rows against **ten**
  page models** — the other five are `PAR-40`/`PAR-41`, owned by [[FND-016]] — F-3.
- **Only five of the ten administration page models name a `StaffAccessRight` at the page;
  the other five guard with `[Authorize(Policy = StaffRoleNames.Administrator)]`** — F-4. The
  acceptance criterion "every administration row names the `StaffAccessRight` that guards it"
  needs the right found in the Core use case for those five.
- **`PAR-46`'s tool count is 35, and the body's summing command gives 42** — F-8. The
  difference is exactly the seven `[McpServerToolType]` class attributes.
- **`PAR-27` is also swept into [[FND-017]]'s row range** — F-2. This ticket has the explicit
  claim; confirm before either edits it.
- Every path, line and line-count the body cites is exact — F-1, F-5, F-6, F-7, F-10.

### Facts

Verified at `bbd1c549` on 2026-08-24, each with its command.

- **F-1 — 39 handlers across the owned surface.**
  `git grep -n "public .*On\(Get\|Post\)" -- 'src/Pegasus.Web/Pages/Administration' 'src/Pegasus.Web/Pages/Operations' 'src/Pegasus.Web/Pages/Cases/Vehicle.cshtml.cs' 'src/Pegasus.Web/Pages/Cases/Assessment' 'src/Pegasus.Web/Pages/Cases/Eva' 'src/Pegasus.Web/Pages/Error.cshtml.cs' 'src/Pegasus.Web/Pages/StatusCode.cshtml.cs'`,
  excluding the `Organizations/` and `Principals/` hits that belong to [[FND-016]], gives
  **39**:
  `Administration/Index` `OnGet :22`;
  `Administration/Configuration` `OnGetAsync :40`, `OnPostAsync :52`;
  `Administration/MailCategories` `OnGetAsync :24`, `OnPostSaveAsync :32`;
  `Administration/Mailboxes` `OnGetAsync :45`, `OnPostUpdateAsync :58`, `OnPostResolveFoldersAsync :167`;
  `Administration/Access/Index` `OnGetAsync :26`, `OnPostReviewAsync :37`;
  `Administration/Accounts/Index` `OnGetAsync :32`, `OnPostCreateAsync :43`;
  `Administration/Accounts/Edit` `OnGetAsync :22`, `OnPostDisableAsync :34`;
  `Administration/Roles/Index` `OnGetAsync :48`, `OnPostAssignAsync :59`;
  `Administration/Automation/Index` `OnGetAsync :45`, `OnPostSetEnabledAsync :57`,
  `OnPostSetSendToAiEnabledAsync :95`, `OnPostUpdateConnectorAsync :128`,
  `OnPostRotateChannelTokenAsync :168`, `OnPostClearChannelTokenAsync :207`;
  `Administration/Automation/Activity` `OnGetAsync :23`;
  `Operations/Index` `OnGetAsync :57`, `OnPostRetryExternalAsync :71`, `OnPostRevokeLinkAsync :112`;
  `Cases/Vehicle` `OnPostRequestVehicleLookupAsync :24`, `OnPostAcceptVehicleSuggestionAsync :46`,
  `OnPostGenerateEvaHandoffAsync :87`;
  `Cases/Assessment/Index` `OnPostSaveDamageAsync :184`, `OnGetAsync :246`,
  `OnPostGenerateReportDraftAsync :277`, `OnPostImportEstimateAsync :330`,
  `OnPostAcceptSpecificationAsync :476`, `OnPostSendAsync :583`, `OnPostReconcileAsync :628`;
  `Cases/Eva/Download` `OnPostAsync :21`;
  `Error` `OnGet :29`; `StatusCode` `OnGet(int code) :38`.
  **`Cases/Assessment/Index` has exactly seven handlers**, satisfying the body's second
  Verification item — and note the source order: `OnPostSaveDamageAsync` (`:184`) precedes
  `OnGetAsync` (`:246`).
- **F-2 — `PAR-27` is claimed by two ticket bodies.** [[FND-017]] (plan handle `DSK-01-04`)
  states its rows as `PAR-19`–`PAR-31`, which sweeps `PAR-27` in. But none of [[FND-017]]'s
  twelve steps mentions `PAR-27`, its step-11 `to locate` list omits it, and `PAR-27` is
  capability group **13.10**, not one of [[FND-017]]'s 13.4/13.7/13.8. **This ticket has the
  explicit claim**: `PAR-27` is in its *What*, has a dedicated step 6 and appears in its
  acceptance criteria. The determinate reading is that this ticket owns it. Both bodies are
  settled, so it is recorded rather than silently resolved — confirm with [[FND-017]] before
  either edits the row (open question U-11). Filling it twice, or not at all, is the failure.
- **F-3 — Eight rows cover *ten* administration page models, not fifteen.**
  `git ls-files 'src/Pegasus.Web/Pages/Administration/*.cshtml.cs' | wc -l` → **15** (the body's
  Verification item is right, including its note that the `**/` spelling returns **11** —
  measured and confirmed: **11**, because `**/` demands at least one directory below
  `Administration/`). But five of those fifteen — `Organizations/Index`, `Organizations/Edit`,
  `Principals/Index`, `Principals/Create`, `Principals/Replace` — are `PAR-40` and `PAR-41`,
  owned by [[FND-016]] (plan handle `DSK-01-03`). The remaining **ten** map onto `PAR-32`…`PAR-39`:

  | Row | Page model(s) |
  | --- | --- |
  | `PAR-32` | `Administration/Index.cshtml.cs` |
  | `PAR-33` | `Administration/Configuration.cshtml.cs` |
  | `PAR-34` | `Administration/MailCategories.cshtml.cs` |
  | `PAR-35` | `Administration/Mailboxes.cshtml.cs` |
  | `PAR-36` | `Administration/Access/Index.cshtml.cs` |
  | `PAR-37` | `Administration/Accounts/Index.cshtml.cs`, `Accounts/Edit.cshtml.cs` |
  | `PAR-38` | `Administration/Roles/Index.cshtml.cs` |
  | `PAR-39` | `Administration/Automation/Index.cshtml.cs`, `Automation/Activity.cshtml.cs` |

  10 page models on 8 rows, with no unmapped file. The acceptance criterion "all fifteen … each
  is named on exactly one of `PAR-32`–`PAR-39`, or recorded as a finding" is satisfied by naming
  ten here and **cross-referencing the other five to `PAR-40`/`PAR-41`** — do not fill a
  sibling's cells, and do not record the five as a finding, because they are covered.
- **F-3a — One `AdministrationPageModel` deriver lives outside `Administration/`.**
  `src/Pegasus.Web/Pages/Connect/Authorize.cshtml.cs:24` reads
  `public sealed class AuthorizeModel : AdministrationPageModel`. It is `PAR-42`, owned by
  [[FND-015]] and `legacy path retained`; it is not one of the fifteen and is not this ticket's.
  `AdministrationPageModel` itself is 7 lines and contributes only
  `IsOperationKeyValid(string)` (a `Guid.TryParseExact(value, "N", …)` check) — the operation-key
  *format* rule for every administration command.
- **F-4 — Only five of the ten administration page models name a `StaffAccessRight`; the other
  five guard by role policy.**
  `git grep -n "StaffAccessRight\." -- 'src/Pegasus.Web/Pages/Administration'` hits only:
  `Automation/Activity.cshtml.cs:32` and `Automation/Index.cshtml.cs:52,:64,:103,:135,:175,:214`
  → `StaffAuthorization.Require(actor, StaffAccessRight.ManageAutomationClients)` (`PAR-39`);
  `Configuration.cshtml.cs:47,:59` → `ManageWorkflowConfiguration` (`PAR-33`);
  `Index.cshtml.cs:32` → `ManageStaffAccounts` (`PAR-32`);
  `Mailboxes.cshtml.cs:52,:65,:174` → `ManageApprovedMailboxes` (`PAR-35`).
  The other five guard at the class level with
  `[Authorize(Policy = StaffRoleNames.Administrator)]`:
  `MailCategories.cshtml.cs:9`, `Access/Index.cshtml.cs:8`, `Accounts/Index.cshtml.cs:8`,
  `Accounts/Edit.cshtml.cs:8`, `Roles/Index.cshtml.cs:8`.
  So `PAR-34`, `PAR-36`, `PAR-37` and `PAR-38` have **no page-level `StaffAccessRight`**, and
  the matrix's guesses for them (`ManageApprovedOutlookCategories`, `ReviewStaffAccess`,
  `AssignStaffRoles`) must be located in the Core use case
  (`src/Pegasus.Core/Identity/StaffAccountAdministration.cs`,
  `src/Pegasus.Core/Intake/ApprovedOutlookCategories.cs`) rather than asserted from the page.
  The 12 rights themselves are `src/Pegasus.Core/Identity/StaffAuthorization.cs:9-20`, enum at
  `:7`, fail-closed by the file's own summary at `:23-26`.
- **F-5 — `PAR-27`'s attributes, exactly.** `src/Pegasus.Web/Pages/Operations/Index.cshtml.cs:13`
  `[Authorize(…)]` and `:15` `[ValidateAntiForgeryToken]` at class level, with the three handlers
  of F-1. The body's step 6 requirement to record `[ValidateAntiForgeryToken]` is met by citing
  `:15`.
- **F-6 — `PAR-14`/`PAR-18` Core and adapter owners exist.**
  `ls src/Pegasus.Infrastructure/Vehicle/` → `DvlaDvsaAdapters.cs`,
  `DvlaDvsaProductionAdapter.cs` — the live adapter the row must record as Worker-owned.
  `wc -l src/Pegasus.Core/Eva/EvaBundleSchema.cs` → **916**, matching the plan's frozen-revision
  claim.
- **F-7 — `PAR-15` owners exist.** `git grep -n "class AssessmentPolicy" src/Pegasus.Core` →
  `src/Pegasus.Core/Assessment/AssessmentPolicy.cs:19`; `wc -l` → **499**, matching the plan.
  `ls src/Pegasus.Infrastructure/Reports/` → `PlaywrightAssessmentReportRenderer.cs` — today's
  server-side rendering path, which is the **parity baseline** the row records. L-03 / ADR-0108
  (isolated non-UI WebView2 HTML→PDF, gateway renderer retained until golden-file parity) is the
  target and is cited, not designed here.
- **F-8 — `PAR-46` is 35 tools; the body's command sums to 42.**
  `git grep -c "McpServerTool" src/Pegasus.Web/Mcp/` prints seven per-file counts —
  `AssessmentMcpTools.cs:6`, `CaseMcpTools.cs:6`, `DocumentMcpTools.cs:4`,
  `IntakeMcpTools.cs:3`, `MailMcpTools.cs:4`, `TriageMcpTools.cs:14`,
  `UnidentifiedMcpTools.cs:5` — summing to **42**. Each file also carries one
  `[McpServerToolType]` class attribute (for example `AssessmentMcpTools.cs:144`), so
  42 − 7 = **35** `[McpServerTool(` method attributes. Independently:
  `git grep -oh 'pegasus_[a-z_]*' src/Pegasus.Web/Mcp/ | sort -u | wc -l` → **35**. The 35 names
  break down as 8 case (`case_edit_begin/end/renew`, `case_get`, `case_search`,
  `case_update_details`), 2 assessment, 3 document, 1 eva bundle + 1 eva handoff status,
  2 intake, 3 mail, 13 triage, 4 unidentified. **State 35, with the −7 arithmetic**, as the
  acceptance criterion requires ("the actual `pegasus_*` tool count produced by the grep, not
  the number copied from the plan").
- **F-8a — The MCP projection is not a complete mirror of the pages.** The 13
  `pegasus_triage_*` tools cover 10 of the page's 12 Triage commands; `assign` and `unassign`
  have no MCP counterpart ([[FND-014]] F-10a, [[FND-017]] F-4a). `PAR-46` is described in the
  matrix as "the reference projection for `/api/v1` shapes (area 03)" — record that it is a
  *reference*, not a complete surface, so area 03 does not infer the endpoint set from it alone.
- **F-9 — The id namespaces, confirmed against the board.** `get_group_doc HZN-001 board-conventions.md`
  § *Upstream ids versus board ids*: a bare `<PREFIX>-<nnn>` is a **fork board id**; an upstream
  id is always `upstream <ID>`. Verified with `search_items`, the plan handles this ticket's body
  names resolve as: `DSK-11-05` → board **`PLAT-023`** ("Resource-health, advisor and compliance
  read of the estate"); `DSK-11-07` → board **`PLAT-025`** ("Register refresh rule in the gateway
  and desktop release routes"); `DSK-11-08` → board **`PLAT-026`** ("Post-cutover deprovision
  checklist"); `DSK-11-09` → board **`PLAT-027`** ("Telemetry cap decision input"). Board
  `PLAT-028` is the imported **upstream `PLAT-032`** ("Simplification and duplicate-route sweep"),
  a different ticket from upstream `PLAT-028`. Meanwhile
  `docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md` carries the *upstream* rows
  this ticket must cite: `PLAT-023` (`:172`, Redesign the Operations workspace), `PLAT-025`
  (`:173`, Redesign workflow configurations), `PLAT-026` (`:174`, Redesign Approved Mailboxes
  administration), `PLAT-027` (`:175`, Consolidate Staff accounts, roles and access review),
  `PLAT-028` (`:176`, Redesign Organizations and Principals), `AUTO-006` (`:82`, Redesign the
  Automation workspace), `AUTO-007` (`:83`, Redesign AI Settings). **Every one of those seven is
  an upstream id and none is the fork board ticket of the same number.** Write them
  `upstream:PLAT-023`, `upstream:PLAT-025`, `upstream:PLAT-026`, `upstream:PLAT-027`,
  `upstream:AUTO-006`, `upstream:AUTO-007` in the matrix — never bare.
- **F-10 — `PAR-43` and `PAR-45` anchors are exact.**
  `src/Pegasus.Web/Program.cs:973-977` is the `IsMachineSurface(PathString path)` predicate —
  `/health`, `/diagnostics`, the MCP endpoint and the token endpoint — documented at `:969-971`
  as "Paths whose callers are programs, not people: they want a status code and a parsable body,
  and a re-executed HTML card would break them." That is precisely the "status-code re-execute
  away from machine surfaces" the row claims, and it is a desktop-relevant fact: the desktop is a
  program, so a problem-details body — not an HTML card — is what it must parse.
  `Program.cs:939` `/health/live`, `:945` `/health/ready`, `:954` `GET /diagnostics/version`;
  `tests/Pegasus.IntegrationTests/ReadinessEndpointTests.cs` exists.
- **F-11 — Named test files exist** (`test -f`):
  `tests/Pegasus.IntegrationTests/Reports/AssessmentReportDraftWebTests.cs`,
  `Reports/AssessmentReportRendererTests.cs`,
  `Browser/AssessmentReadinessSummaryBrowserTests.cs`, `ReadinessEndpointTests.cs`. Additional
  files that exist and are obvious candidates for the `to locate` cells:
  `tests/Pegasus.IntegrationTests/AssessmentDamageAndCopyWebTests.cs`,
  `AssessmentEstimateImportWebTests.cs`, `AssessmentPersistenceIntegrationTests.cs`,
  `AssessmentVehiclePrefillWebTests.cs`, `AutomaticVehicleLookupTests.cs`,
  `OrganizationAdministrationWebTests.cs`, `ShellAndStatusPageWebTests.cs`,
  `tests/Pegasus.Core.Tests/Operations/OperationsUseCaseTests.cs`,
  `tests/Pegasus.Core.Tests/Reports/AssessmentReportProjectionTests.cs`,
  `tests/Pegasus.Core.Tests/Reports/AssessmentReportRenderingTests.cs`,
  `tests/Pegasus.Core.Tests/Qdos/EvaBundleContractTests.cs`,
  `tests/Pegasus.Core.Tests/Qdos/EvaHandoffPolicyTests.cs`. **Open each before citing it.**
- **F-12 — The documentation gate takes no parameters.** `scripts/Test-DocumentationLinks.ps1:9`
  is `[CmdletBinding()] param()`, as the body's fourth Verification item says.

### Assumptions

- **A-01-05-1 — These 15 rows are the complete §13.5/§13.9/§13.10 surface.** Confirmed by F-1
  and F-3: 39 handlers plus two non-Razor rows, with nothing left over. Breaks if [[FND-014]]'s
  difference list (a) names an unmapped page model. **Confirm by reading [[FND-014]]'s
  difference lists**, not yet written.
- **A-01-05-2 — The four rights the matrix guesses for `PAR-34`, `PAR-36`, `PAR-37`, `PAR-38`
  are enforced in Core rather than at the page.** Based on F-4: those pages carry only the
  `Administrator` policy attribute. Breaks if the right is never checked at all for one of them —
  which would be a fail-open on an administration surface and an escalation, not a cell.
  **Confirm by reading** `src/Pegasus.Core/Identity/StaffAccountAdministration.cs` and
  `src/Pegasus.Core/Intake/ApprovedOutlookCategories.cs` for
  `StaffAuthorization.Require(..., ManageStaffAccounts | ReviewStaffAccess | AssignStaffRoles |
  ManageApprovedOutlookCategories)`.
- **A-01-05-3 — 35 is the tool count and 42 is an artefact of the attribute name.** Based on
  F-8's two independent measurements agreeing at 35. Breaks if a `[McpServerTool(` method is
  conditionally registered, or if a tool name is built rather than literal. **Confirm by
  comparing** the 35 distinct `pegasus_*` names against the 35 `[McpServerTool(` sites.
- **A-01-05-4 — `PlaywrightAssessmentReportRenderer` is the only production rendering path.**
  Based on `src/Pegasus.Infrastructure/Reports/` containing exactly one file. Breaks if a second
  renderer is composed behind a feature gate. **Confirm with**
  `git grep -n "IAssessmentReportRenderer" src/Pegasus.Web src/Pegasus.Infrastructure` — not yet
  run. `PAR-15`'s parity baseline must be the path production actually uses, since ADR-0108's
  golden-file comparison is measured against it.
- **A-01-05-5 — `Automation/Index.OnPostSetSendToAiEnabledAsync` (`:95`) is inventoried as an
  existing administration command, and nothing more.** Send to AI is a **recorded exclusion with
  a reactivation condition**, not an open conflict: `src/Pegasus.Web/AiWork/SendToAi.cs:12`
  defines `Features:SendToAi`, `:35-42` refuse to compose it outside the `DevelopmentOffline`
  runtime profile, and `src/Pegasus.Web/Program.cs:104-110` permits that profile only in
  Development, so it has never been operator-reachable in production. `PAR-39` records the
  handler as it is and cites `upstream:AUTO-007` for the redesign; it must **not** be turned into
  desktop scope, and **no `open-questions` item is created for it**. The reactivation condition is
  the separate non-preview transport decision named at `docs/capabilities.md:269`, and recording
  it belongs to [[FND-022]] (plan handle `DSK-01-09`) step 10.

## Execution placement

**This ticket places no responsibility anywhere.** It is read-only inspection of
`src/Pegasus.Web`, `src/Pegasus.Core`, `src/Pegasus.Infrastructure`, `tests/` and `infra/`, plus
edits to `docs/desktop/01-inventory-and-parity/parity-matrix.md` and possibly
`docs/open-decisions.md`. It renders no report, calls no DVLA/DVSA provider, changes no renderer
and makes no Azure call — the Guardrails say "no write", and live Azure verification is
[[FND-021]]'s (plan handle `DSK-01-08`) job. The six-question cloud-justification test of
`docs/desktop/00-governance-and-workflow/README.md` § 3 is therefore not answered here.

The one placement it **assumes**: the enumeration runs on a developer workstation against a local
checkout, and its output is a repository document.

Placements the rows **record** (decided elsewhere, cited not re-argued), from proposal § 4.1
(`docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md:140-162`):
`Interactive report generation` → **Mostly desktop** ("Generate and preview locally / Store final
record/document centrally"), which is L-03 and ADR-0108;
`DVLA/DVSA lookup` → **Split** ("Secret/rate-limit handling and shared result cache"), which is
ADR-0107; `Audit trail` → **Cloud required**; `Native UI, navigation and state` → **Desktop**.
Those ADRs are authored by [[FND-005]] (plan handle `DSK-00-05`) and [[FND-006]], and ADR-0108 is
authored by [[FND-007]]; the six-question tables belong to them. **The matrix has no placement
column** ([[FND-015]] F-13): this ticket takes the same default as [[FND-015]], [[FND-016]] and
[[FND-017]] — a leading `Placement: <value> (proposal §4.1)` clause inside the existing "Native
screen/use case" cell — and parks the schema question.

## Implications

1. **`PAR-15` is the evidence base for ADR-0108.** Recording today's Playwright path (F-7)
   precisely — which handler produces a draft (`:277`), which sends (`:583`), which reconciles
   (`:628`) — is what makes a golden-file parity comparison possible later. The row records the
   baseline; it does not design the replacement.
2. **The right-guard split (F-4) is a real conversion input.** A role-aware desktop shell
   ([[FND-046]], plan handle `DSK-04-10`) hides commands by `StaffAccessRight`; four of these
   rows have no page-level right to hide by, so the desktop must take the right from Core, not
   from the page. If A-01-05-2 fails for any of them, that is a fail-open finding.
3. **`PAR-46` is a reference, not a mirror (F-8a).** Area 03 must not derive the `/api/v1`
   endpoint set from the 35 tools alone: two Triage commands are absent from them.
4. **Every id in these cells is a collision risk (F-9).** Seven upstream ids, four of whose
   numbers are live fork board tickets with entirely different meanings, and one (`PLAT-028`)
   where the fork board ticket of that number is an imported *different* upstream ticket
   (upstream `PLAT-032`).
5. **`PAR-45` tells the desktop what to parse (F-10).** `IsMachineSurface` means the desktop
   receives status codes and parsable bodies from `/health` and `/diagnostics`, never a
   re-executed HTML card — that is exactly what the status bar and About surface consume.
6. **`PAR-27`'s double claim (F-2) must be settled** before either ticket edits the matrix.

---

## NOT YET CAPTURED — the spike's remaining work

Each block names the exact command and the question its output must answer; each has one
unticked box in `open-questions`.

### NOT YET CAPTURED — U-1: the row-by-row citation table

**Command:** none — assembly. One table for the 15 owned rows:
`PAR id → entry point(s) → handlers (path:line) → guarding StaffAccessRight (path:line) → Core
owner (path:line) → FRD owner → test file or gap: → upstream redesign id (upstream:<ID>) →
placement (§4.1) → inventoried-at SHA` (body step 12).

### NOT YET CAPTURED — U-2: handler-to-row mapping proof

**Commands:** the F-1 grep, and
`git ls-files 'src/Pegasus.Web/Pages/Administration/*.cshtml.cs' | wc -l` (expect **15**; the
`**/` spelling returns **11** and must not be used), and
`git grep -n "public .*On\(Get\|Post\)" -- 'src/Pegasus.Web/Pages/Cases/Assessment'` (expect
**7**).
**Question it must answer:** do all 39 handlers land in exactly one owned row, and are all
fifteen `Administration/` page models accounted for — ten on `PAR-32`…`PAR-39` and five
cross-referenced to `PAR-40`/`PAR-41` (F-3)?

### NOT YET CAPTURED — U-3: `PAR-14` filled

**Commands:** read `src/Pegasus.Web/Pages/Cases/Vehicle.cshtml.cs` and `src/Pegasus.Core/Vehicle/`;
`git grep -rln "VehicleLookup\|Dvla\|Dvsa" tests/`.
**Question it must answer:** what is the request→accept workflow, where is the Worker-owned live
adapter (`src/Pegasus.Infrastructure/Vehicle/DvlaDvsaProductionAdapter.cs`), how is provider
outage kept distinguishable from not-found (proposal § 16.2), and what tests exist?

### NOT YET CAPTURED — U-4: `PAR-18` filled

**Command:** read `src/Pegasus.Web/Pages/Cases/Eva/Download.cshtml.cs` and
`src/Pegasus.Core/Eva/EvaBundleSchema.cs` (916 lines).
**Question it must answer:** what makes revisions frozen, and where is the required reason
enforced?

### NOT YET CAPTURED — U-5: `PAR-15` filled, and A-01-05-4 settled

**Commands:** read all seven handlers of F-1; `git grep -n "class AssessmentPolicy" src/Pegasus.Core`;
`git grep -n "IAssessmentReportRenderer" src/Pegasus.Web src/Pegasus.Infrastructure`.
**Question it must answer:** is `PlaywrightAssessmentReportRenderer` the only production
rendering path, and does the row record today's behaviour as the parity baseline while citing
L-03 / ADR-0108 for the target without specifying the replacement?

### NOT YET CAPTURED — U-6: `PAR-27` filled (subject to U-11)

**Command:** read `src/Pegasus.Web/Pages/Operations/Index.cshtml.cs` (3 handlers, `[Authorize]`
`:13`, `[ValidateAntiForgeryToken]` `:15`) and `src/Pegasus.Core/Operations/`.
**Question it must answer:** what do retry-external and revoke-link require, and does the cell
cite `upstream:PLAT-023` in full form?

### NOT YET CAPTURED — U-7: `PAR-32`…`PAR-39` filled, and A-01-05-2 settled

**Commands:** the F-4 grep, plus reads of
`src/Pegasus.Core/Identity/StaffAccountAdministration.cs` and
`src/Pegasus.Core/Intake/ApprovedOutlookCategories.cs`.
**Question it must answer:** which `StaffAccessRight` guards each of `PAR-34`, `PAR-36`,
`PAR-37`, `PAR-38`, given that those pages carry only
`[Authorize(Policy = StaffRoleNames.Administrator)]`? If any is never checked, say so — it is a
fail-open finding, not a cell.

### NOT YET CAPTURED — U-8: `PAR-43`, `PAR-45`, `PAR-46` filled

**Commands:** `git grep -c "McpServerTool" src/Pegasus.Web/Mcp` (sums to 42) and
`git grep -oh 'pegasus_[a-z_]*' src/Pegasus.Web/Mcp/ | sort -u | wc -l` (35).
**Question it must answer:** does `PAR-46` state **35** with the −7 `[McpServerToolType]`
arithmetic shown, does `PAR-45` record that the desktop parses status codes and bodies rather
than HTML cards (`Program.cs:973-977`), and does `PAR-43` say it maps to the area 06 error and
empty-state catalogue rather than to a screen?

### NOT YET CAPTURED — U-9: `to locate` cells resolved

**Command:** `git grep -rln` over `tests/` per row, **opening each candidate from F-11 before
citing it**.
**Question it must answer:** for each row, is there a test that asserts the behaviour the cell
claims? Where none does, write `gap: <untested behaviour>` and copy the line into this document
for [[FND-025]] (plan handle `DSK-01-12`).

### NOT YET CAPTURED — U-10: the matrix edits and the documentation gate

**Commands:** the edit, then `pwsh ./scripts/Test-DocumentationLinks.ps1` — exit 0.
**Question it must answer:** does the diff change only the owned rows, with every upstream id in
full `upstream:<ID>` form, the SHA stamped, `~` names and blank UAT owners untouched — and does
the CI `documentation` job stay green?

## Open questions

Tracked as unticked items in this ticket's `open-questions` document.

- U-1 … U-10 above, plus:
- **U-11 — who owns `PAR-27`?** (F-2.) Settle with [[FND-017]] before either ticket edits it.
- **U-12 — is any of the four rights of A-01-05-2 never checked?** If so it is a fail-open on an
  administration surface and an escalation to the operator, not a matrix cell.

**Not open questions — scope boundaries owned by named tickets:**

- The confirmed skeleton and the three difference lists: [[FND-014]] (`DSK-01-01`).
- `PAR-40`, `PAR-41` and the five `Organizations/`+`Principals/` page models: [[FND-016]]
  (`DSK-01-03`). `PAR-13`, `PAR-16`, `PAR-17`, `PAR-19`…`PAR-26`, `PAR-28`…`PAR-31`:
  [[FND-017]] (`DSK-01-04`). `PAR-42`: [[FND-015]] (`DSK-01-02`).
- The report-rendering flow record behind `PAR-15`: [[FND-020]] (`DSK-01-07`). ADR-0108 itself:
  [[FND-007]].
- Live Azure verification of anything these rows touch: [[FND-021]] (`DSK-01-08`).
- Promoting a `~` endpoint name: area 03's endpoint map.
- The characterization-gap list: [[FND-025]] (`DSK-01-12`).
- **Send to AI / upstream `TICK-102`** — a recorded exclusion with a reactivation condition
  (A-01-05-5). No `open-questions` item is created for it on any ticket.
