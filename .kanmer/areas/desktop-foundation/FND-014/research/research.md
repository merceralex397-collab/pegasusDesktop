# Research — FND-014: Re-derive the page-model inventory and confirm the parity-matrix skeleton

> **STATUS — INCOMPLETE. Do not move this ticket to Done yet.**

This is a `spike`. Its `research` document is the spike's **output**, not an input to it, and
its existence is what satisfies the `enter-done` gate (`get_doc_gates FND-014`:
`enter-done` needs `research` + `questions-resolved`; there is no other gated boundary). This
file was written **before the spike ran**, as the pre-work scaffold the authoring contract
requires. Everything under **Facts** is already verified against the repository with the
command that produced it. Everything marked `NOT YET CAPTURED` is the spike's real work and is
still owed; each one has an unticked box in this ticket's `open-questions` document, and those
boxes — not this banner — are what actually block `enter-done`.

Baseline for every measurement below: `git rev-parse HEAD` →
`bbd1c54959e8c3a361d3f73965b61d6e4aff59ec`, read 2026-08-24. The implementer re-runs step 2 of
the body and **re-stamps every number** against the head they actually work at; a number below
that has moved is a finding, not an error to hide.

## Question

Does the 46-row skeleton of `docs/desktop/01-inventory-and-parity/parity-matrix.md` — its
"Current entry point" and handler columns — still match the Razor page-model and handler
surface of `src/Pegasus.Web` at the current fork head, and where it does not, exactly which
page models, rows and handlers differ? Everything the sibling tickets [[FND-015]] (plan handle
`DSK-01-02`), [[FND-016]] (`DSK-01-03`), [[FND-017]] (`DSK-01-04`) and [[FND-018]]
(`DSK-01-05`) do is written against the answer.

## Current behaviour

**This ticket's subject is the matrix itself, so it does not sit on one `PAR-nn` row — it
covers all of them.** `grep -c '^| PAR-' docs/desktop/01-inventory-and-parity/parity-matrix.md`
returns **46**, keyed `PAR-01`…`PAR-46`, every row pointing at a page model under
`src/Pegasus.Web/Pages/**` except `PAR-45` (the non-Razor health and version endpoints in
`src/Pegasus.Web/Program.cs`) and `PAR-46` (the MCP tool projection in
`src/Pegasus.Web/Mcp/`).

How the web application presents that surface today:

- Razor Pages, conventional routing, one page model per screen under
  `src/Pegasus.Web/Pages/**`, each exposing `OnGet*`/`OnPost*` handlers. The command inventory
  *is* the handler-name list — that is the decision area 01 already recorded
  (`docs/desktop/01-inventory-and-parity/README.md:181-184`: keyed by page model + handler
  group, not by URL).
- Four shared base classes carry the cross-cutting behaviour: `src/Pegasus.Web/Pages/StaffPageModel.cs`
  (18 lines, actor resolution), `src/Pegasus.Web/Pages/Administration/AdministrationPageModel.cs`
  (7), `src/Pegasus.Web/Pages/Cases/CaseMutationPageModel.cs` (339, the PRG + TempData mutation
  wrapper) and `src/Pegasus.Web/Pages/UploadConfirmationPageModel.cs` (82).
- The non-Razor HTTP surface is three route registrations plus the MCP mount:
  `src/Pegasus.Web/Program.cs:939` `/health/live`, `:945` `/health/ready`, `:954`
  `GET /diagnostics/version`; `src/Pegasus.Web/Mcp/AutomationMcpExtensions.cs:134`
  `app.MapPost(AutomationMcp.TokenEndpointPath, …)` and `:137` `app.MapMcp(AutomationMcp.McpEndpointPath)`,
  whose literal paths are `"/connect/token"`, `"/authorize"` and `"/mcp"` at
  `src/Pegasus.Web/Mcp/AutomationMcp.cs:25-27`.

## Findings

- The 53/76 counts the ticket body expects are **correct at `bbd1c549`** — see F-2, F-3.
- The base-class map in the area plan is **right about three of its four numbers and wrong
  about one**, and its `StaffPageModel` path citation is wrong — see F-8, F-9.
- The matrix's `PAR-24` claim of "13 commands" behind `Triage/Details.OnPostActionAsync` is
  **off by one at this head**: there are 12 named commands plus a throwing `default:` — see
  F-10.
- A git pathspec subtlety silently drops page models from three of the sibling tickets'
  verification commands — see F-5. It is the single highest-value fact in this document,
  because a wrong count read as authoritative would mark rows `inventoried` against a surface
  that was never enumerated.

### Facts

Verified by reading the repository at `bbd1c549` on 2026-08-24. Each carries the command that
produced it.

- **F-1 — Baseline.** `git rev-parse HEAD` → `bbd1c54959e8c3a361d3f73965b61d6e4aff59ec`.
  Every row this ticket touches records this SHA (body step 2; area plan § 7 trap 2).
- **F-2 — 53 page models.** `git ls-files 'src/Pegasus.Web/**/*.cshtml.cs' | wc -l` → **53**.
  Matches the body's expectation exactly. All 53 live under `src/Pegasus.Web/Pages/`; there is
  no page model elsewhere in the project.
- **F-3 — 76 views.** `git ls-files 'src/Pegasus.Web/**/*.cshtml' | wc -l` → **76**. Matches
  the body's expectation exactly.
- **F-4 — 46 matrix rows.** `grep -c '^| PAR-' docs/desktop/01-inventory-and-parity/parity-matrix.md`
  → **46**, ids `PAR-01`…`PAR-46` with no gaps. 46 rows against 53 page models is *expected*,
  not a defect: several rows deliberately cover a folder (`PAR-20` covers the three
  `Intake/` byte routes, `PAR-25` covers `Unidentified/Index` + `Details`, `PAR-26` covers
  `ImageIntake/Index` + `Details`, `PAR-37` covers `Accounts/Index` + `Edit`, `PAR-40` covers
  `Organizations/Index` + `Edit`, `PAR-41` covers three `Principals/` pages, `PAR-43` covers
  `Error` + `StatusCode`, `PAR-39` covers `Automation/Index` + `Activity`), and two rows
  (`PAR-45`, `PAR-46`) are not page models at all.
- **F-5 — The git pathspec trap, and it bites the siblings.** `git ls-files` applies its
  pathspec **without** `:(glob)` magic by default, so `*` matches `/` and a literal `**/`
  therefore demands *at least one* directory level. Measured:

  | Command | Returns | Why |
  | --- | --- | --- |
  | `git ls-files 'src/Pegasus.Web/**/*.cshtml.cs' \| wc -l` | **53** ✓ | every page model is ≥1 level below `Pegasus.Web/` |
  | `git ls-files 'src/Pegasus.Web/Pages/**/*.cshtml.cs' \| wc -l` | **47** ✗ | drops the six models sitting directly in `Pages/`: `Error`, `Index`, `StatusCode`, `Upload`, `UploadGroupStatus`, `UploadStatus` |
  | `git ls-files 'src/Pegasus.Web/Pages/Cases/**/*.cshtml.cs' \| wc -l` | **4** ✗ | [[FND-016]]'s Verification expects `12`; it drops the eight models directly in `Cases/` |
  | `git ls-files 'src/Pegasus.Web/Pages/Cases/*.cshtml.cs' \| wc -l` | **12** ✓ | the spelling that answers the question |
  | `git ls-files 'src/Pegasus.Web/Pages/Administration/*.cshtml.cs' \| wc -l` | **15** ✓ | as [[FND-018]]'s Verification already records |
  | `git ls-files 'src/Pegasus.Web/Pages/Administration/**/*.cshtml.cs' \| wc -l` | **11** ✗ | as [[FND-018]]'s Verification already records |

  [[FND-018]] documents this for `Administration/`; [[FND-016]] does not, and its
  `Cases/**/*.cshtml.cs` command returns **4** where its body expects **12**. Hand that
  correction to [[FND-016]] as part of this ticket's output.
- **F-6 — 136 handler declarations.**
  `git grep -n "public .*On\(Get\|Post\)[A-Za-z]*" -- 'src/Pegasus.Web/Pages' | wc -l` → **136**
  lines. `git grep -c "public .*On\(Get\|Post\)" -- 'src/Pegasus.Web/Pages'` prints 53
  per-file counts — 52 of the 53 page models, **plus** the base class
  `Pages/UploadConfirmationPageModel.cs` (2 handlers), which is not one of the 53.
- **F-7 — Exactly one page model declares no handler.** `src/Pegasus.Web/Pages/Account/AccessDenied.cshtml.cs`
  (7 lines) is absent from the per-file count of F-6. `PAR-04` already records it as
  `Account/AccessDenied.cshtml.cs (7)` with no handler list, so the row is consistent — but the
  reconciliation must state this explicitly rather than reading the missing count as a gap.
- **F-8 — The `StaffPageModel` path citation in the area plan is wrong, and the body is right
  about it.** `git ls-files | grep -i StaffPageModel` → `src/Pegasus.Web/Pages/StaffPageModel.cs`
  (18 lines). `docs/desktop/01-inventory-and-parity/README.md:48` says
  ``base classes `Pages/Shared/StaffPageModel` (18 lines, 18 pages)``. `ls src/Pegasus.Web/Pages/Shared/`
  contains only partial views (`_Layout.cshtml`, `_PageHeader.cshtml`, …) and no page-model
  file. The **line count and page count in that citation are correct; only the path is wrong.**
  Body step 10 fixes the citation at `README.md:48` and does not move the file.
- **F-9 — Base-class map, measured, against the plan's stated shape.**

  | Base class | Plan says | `git grep -l ": <Base>" -- 'src/Pegasus.Web/Pages'` | Reconciled | Verdict |
  | --- | --- | --- | --- | --- |
  | `StaffPageModel` | 18 pages | 21 files | 21 − 3 base classes (`Administration/AdministrationPageModel.cs`, `Cases/CaseMutationPageModel.cs`, `UploadConfirmationPageModel.cs`, which derive from it but are not `.cshtml.cs`) = **18 page models** | ✓ matches |
  | `AdministrationPageModel` | 16 pages | 16 files, all `.cshtml.cs` | **16** | ✓ matches, but see F-9a |
  | `CaseMutationPageModel` | 7 case pages | 8 files, all `.cshtml.cs` | **8** | ✗ **plan is short by one** |
  | `UploadConfirmationPageModel` | 2 pages | 2 files | **2** | ✓ matches |

  18 + 16 + 8 + 2 = **44**; the remaining **9** of the 53 derive from none of the four:
  `Account/AccessDenied`, `Account/SignIn`, `Account/SignOut`, `Error`, `ImageIntake/Index`,
  `Search/Index`, `StatusCode`, `Unidentified/Index`, `Uploads/Request`.
- **F-9a — One `AdministrationPageModel` deriver is not an administration page.**
  `src/Pegasus.Web/Pages/Connect/Authorize.cshtml.cs:24` reads
  `public sealed class AuthorizeModel : AdministrationPageModel`. `PAR-42` is the OpenIddict
  consent page for **external** MCP connectors, deliberately `legacy path retained` — yet it
  inherits the administration base. That is a fact about today's code, recorded here so
  [[FND-015]] (which owns `PAR-42`) states it rather than discovering it later; it is not a
  finding this ticket acts on.
- **F-9b — The eight `CaseMutationPageModel` derivers**, by path: `Cases/Closure.cshtml.cs`,
  `Cases/Custody.cshtml.cs`, `Cases/Details.cshtml.cs`, `Cases/Documents/Export.cshtml.cs`,
  `Cases/Eva/Download.cshtml.cs`, `Cases/Tasks.cshtml.cs`, `Cases/Vehicle.cshtml.cs`,
  `Cases/Workflow.cshtml.cs`. The plan's "7 case pages" (`README.md:50`) most likely omits
  `Documents/Export` or `Eva/Download`, both of which are `FileResult` pages that still take
  the mutation envelope. Report the actual number (**8**), per body step 8.
- **F-10 — `Triage/Details.OnPostActionAsync` dispatches 12 named commands, not 13.**
  `grep -n '^\s*case "' src/Pegasus.Web/Pages/Triage/Details.cshtml.cs` returns 12 labels, and
  `git grep -c 'case "' src/Pegasus.Web/Pages/Triage/Details.cshtml.cs` → `12`. The handler
  opens at `:85`; the `switch (actionName)` runs `:113-215` and ends in a `default:` at `:214`
  that throws `ArgumentException("The requested Triage action is not supported.")` — a guard,
  not a command. The 12, in source order with their lines:

  | # | `actionName` | Line | Core call |
  | --- | --- | --- | --- |
  | 1 | `assign` | 116 | `assign.ExecuteAsync` |
  | 2 | `unassign` | 127 | `unassign.ExecuteAsync(mutation, …)` |
  | 3 | `await_information` | 130 | `awaitInformation.ExecuteAsync(mutation, …)` |
  | 4 | `record_finding` | 133 | `recordFinding.ExecuteAsync` |
  | 5 | `supersede_finding` | 146 | `supersedeFinding.ExecuteAsync` |
  | 6 | `link_response` | 159 | `linkResponseEvidence.ExecuteAsync` (parses `responseCandidate`) |
  | 7 | `unlink_response` | 174 | `unlinkResponseEvidence.ExecuteAsync` |
  | 8 | `complete` | 185 | `complete.ExecuteAsync(mutation, …)` |
  | 9 | `cancel` | 188 | `cancel.ExecuteAsync(mutation, …)` |
  | 10 | `reopen` | 191 | `reopen.ExecuteAsync(mutation, …)` |
  | 11 | `link_case` | 194 | `ExecuteCaseAssociationAsync(linking: true, …)` |
  | 12 | `unlink_case` | 204 | `ExecuteCaseAssociationAsync(linking: false, …)` |

  `PAR-24`'s behaviour cell says "dispatches 13 commands"; the row's count must be corrected to
  **12** (with the SHA stamped) or the thirteenth must be produced. `PAR-24` itself belongs to
  [[FND-017]] — this ticket produces the command set, [[FND-017]] writes the cell (body step 7
  vs [[FND-017]] step 3).
- **F-10a — MCP/page asymmetry, for [[FND-017]]'s cross-check.**
  `git grep -oh 'pegasus_[a-z_]*' src/Pegasus.Web/Mcp/ | sort -u` lists 13 `pegasus_triage_*`
  tools, of which 10 mutate (`await_information`, `cancel`, `case_link`, `case_unlink`,
  `complete`, `record_finding`, `reopen`, `response_link`, `response_unlink`,
  `supersede_finding`) and 3 read (`get`, `list`, `source_download`). The page's `assign` and
  `unassign` have **no MCP counterpart**. That is exactly the "command exposed to MCP but
  missing from the page (or the reverse)" check [[FND-017]] step 3 asks for; the answer runs in
  the page→MCP direction.
- **F-11 — The MCP tool count is 35, and the obvious command says 42.**
  `git grep -oh 'pegasus_[a-z_]*' src/Pegasus.Web/Mcp/ | sort -u | wc -l` → **35**, matching
  `PAR-46`. `git grep -c "McpServerTool" src/Pegasus.Web/Mcp/` sums to **42** because it also
  counts the seven `[McpServerToolType]` class attributes, one per `*McpTools.cs` file:
  42 − 7 = 35. [[FND-018]] step 10 uses the summing command; hand it this reconciliation.
- **F-12 — Non-Razor surface confirmed.** `git grep -n "MapHealthChecks\|MapGet(\"/diagnostics" src/Pegasus.Web/Program.cs`
  → `:939` `/health/live`, `:945` `/health/ready`, `:954` `GET /diagnostics/version`.
  `git grep -n "SetTokenEndpointUris\|SetAuthorizationEndpointUris\|MapMcp" src/Pegasus.Web/Mcp/`
  → `AutomationMcpExtensions.cs:39`, `:40` (OpenIddict endpoint URIs) and `:137` (`MapMcp`);
  the token route itself is registered at `:134`. Literal paths at
  `src/Pegasus.Web/Mcp/AutomationMcp.cs:25-27`. `PAR-45` and `PAR-46` cover these; there is no
  orphan.
- **F-13 — The two `legacy path retained` rows still match the code (body step 9).**
  `src/Pegasus.Web/Pages/Uploads/Request.cshtml.cs` (222 lines, 2 handlers) is still the
  anonymous external request-link upload; `src/Pegasus.Web/Pages/Connect/Authorize.cshtml.cs`
  (177 lines, 3 handlers) is still the OpenIddict consent page (ADR-0027). Neither has become a
  staff surface. The base-class oddity in F-9a is a code-hygiene observation, not a change of
  audience.
- **F-14 — Every test file the matrix cites exists** (checked by `test -f` over the 22 paths
  named in the matrix and in the area plan § 5): `Browser/OperatorJourneyTests.cs`,
  `Pegasus.Core.Tests/Operations/DashboardBoundaryTests.cs`, `CaseDetailsWebTests.cs`,
  `CaseWorkflowPersistenceTests.cs`, `QdosAllocationRecoveryTests.cs`, `QdosIntakeWebTests.cs`,
  `MultiFormatIntakeWebTests.cs`, `LocalIntakeAccessTests.cs`, `MailWorkspaceWebTests.cs`,
  `RetainedMailPersistenceTests.cs`, `CustodyOutboxIntegrationTests.cs`,
  `Browser/MailWorkspaceBrowserTests.cs`, `Browser/UploadDropzoneBrowserTests.cs`,
  `Browser/UploadRowsBrowserTests.cs`, `Reports/AssessmentReportDraftWebTests.cs`,
  `Reports/AssessmentReportRendererTests.cs`, `Browser/AssessmentReadinessSummaryBrowserTests.cs`,
  `ReadinessEndpointTests.cs`, `Browser/AccessibilityTests.cs`,
  `Browser/QdosAllocationRecoveryBrowserTests.cs`, `Browser/UploadCaseSearchBrowserTests.cs`,
  `IntakeStablePersistenceTests.cs`. No cited test path is stale; the `to locate` cells are
  genuinely unfilled, not broken.
- **F-15 — Both documentation gates exist and take no parameters.**
  `scripts/Test-DocumentationLinks.ps1:9` and `scripts/Test-MarkdownPlacement.ps1` are both
  `[CmdletBinding()] param()`. The body's Verification commands are runnable as written.

### Assumptions

- **A-01-1 — The 53 `.cshtml.cs` files are the complete staff surface.** Confirmed by: an
  enumeration that finds no page model outside `src/Pegasus.Web/Pages/`, and by the non-Razor
  surface of F-12 being fully covered by `PAR-45`/`PAR-46`. Breaks if a capability is reachable
  through a minimal-API route, an MVC controller or a Razor component not enumerated here — in
  which case a capability exists with no inventory row and the Phase 0 exit gate
  (`docs/desktop/01-inventory-and-parity/README.md:207-215` item 1) cannot honestly be claimed.
  **Confirm with** `git grep -n "MapGet(\|MapPost(\|MapPut(\|MapDelete(\|AddControllers" src/Pegasus.Web`
  before closing this spike — that command has not been run.
- **A-01-2 — 46 rows covering 53 page models is by design, not drift.** Confirmed by the
  folder-covering rows enumerated in F-4. Breaks if a page model is covered by *no* row; that
  is difference list (a) and is what the spike must produce. Do not resolve a mismatch by
  assuming coverage.
- **A-01-3 — Handler names are the stable command inventory.** This is area 01's recorded
  decision (`README.md:181-184`), not a discovery. It breaks wherever one handler dispatches
  many commands — F-10 is the canonical case and the plan's own trap 1
  (`README.md:225-228`). Every multi-command handler must be expanded before a row is trusted.
- **A-01-4 — Upstream has not moved the surface since the matrix was pre-populated.** The
  matrix was filled from `main` `191ddf33` on 2026-08-23 and the fork head is now `bbd1c549`;
  upstream `collisionengineers/pegasus` `main` was 32 commits ahead at `7d6a948a` on that date
  (`docs/desktop/README.md` planning baseline). The 53/76 counts still hold, so nothing in the
  *fork* has moved the surface — but a row inventoried at `bbd1c549` can go stale the moment
  [[FND-023]] (plan handle `DSK-01-10`) lands the first upstream sync. **This is why every row
  stamps its SHA**, and why re-verification after the sync is [[FND-051]] (`DSK-01-13`)'s
  standing job, not a silent assumption here.
- **A-01-5 — `PAR-24`'s "13" was a miscount, not a removed command.** The safe reading is that
  the pre-population counted the `default:` label or double-counted a case. Breaks if a
  thirteenth Triage command existed at `191ddf33` and was deleted since — which would be a
  behaviour regression, not a documentation error. **Confirm with**
  `git log -p --follow -- src/Pegasus.Web/Pages/Triage/Details.cshtml.cs` limited to the range
  `191ddf33..HEAD`; that command has not been run.

## Execution placement

**This ticket places no responsibility anywhere.** It is read-only inventory over
`src/Pegasus.Web` plus edits to two Markdown files under `docs/desktop/01-inventory-and-parity/`;
it starts no process, holds no credential, publishes no artefact and calls no Azure API (the
Guardrails say "no write … no Azure call at all"). The six-question cloud-justification test of
`docs/desktop/00-governance-and-workflow/README.md` § 3 is therefore not answered here.

The one placement it **assumes**: the enumeration runs on the developer workstation against a
local checkout, and its output is a repository document — no host, service or scheduled job is
implied by it. The *per-capability* placement answers the matrix rows will eventually carry are
owned by the ADRs of [[FND-005]] (plan handle `DSK-00-05`) and by area 11's
cloud-dependency records, not by this spike ([[FND-015]] step 7 says the same for its rows).

## Implications

1. **The skeleton is close but not clean.** Two of the plan's stated numbers are wrong (the
   `CaseMutationPageModel` count, F-9; the `PAR-24` command count, F-10) and one path citation
   is wrong (F-8). Each is a one-line correction, and each would have propagated into a sibling
   ticket's rows unchallenged.
2. **The pathspec trap (F-5) must be pushed to the siblings before they run.** [[FND-016]]'s
   Verification command returns 4 where its body expects 12. Correcting the *number* without
   correcting the *command* would leave the next agent with a command that silently under-counts.
3. **`PAR-24` and `PAR-46` are the two rows where a copied number is a defect.** Both ticket
   bodies already say "state the actual number"; F-10 and F-11 give the actual numbers and the
   arithmetic that explains the discrepancy, so neither sibling has to rediscover it.
4. **The `to locate` cells are real gaps, not broken links** (F-14). The siblings should write
   `gap: <untested behaviour>` where nothing exists and feed the line to [[FND-025]] (plan
   handle `DSK-01-12`), exactly as their bodies instruct — not hunt for a mis-typed path.
5. **Status columns stay untouched here** (body step 10). This ticket corrects entry points,
   handler lists and the inventoried-at SHA; advancing a row to `inventoried` belongs to
   [[FND-015]]…[[FND-018]].

---

## NOT YET CAPTURED — the spike's remaining work

Each block names the exact command and the question its output must answer. Each has one
unticked box in this ticket's `open-questions` document.

### NOT YET CAPTURED — U-1: raw per-file handler enumeration in the ticket scratch

**Command:** `git grep -n "public .*On\(Get\|Post\)[A-Za-z]*" -- 'src/Pegasus.Web/Pages'`
**Then:** `append_scratch` the raw 136-line output onto FND-014.
**Question its output must answer:** can the reviewer re-run the enumeration and get byte-identical
output to what the reconciliation was built from? (Body step 4 requires the raw output in scratch.)

### NOT YET CAPTURED — U-2: difference list (a) — page models with no `PAR` row

**Command:** join `git ls-files 'src/Pegasus.Web/**/*.cshtml.cs'` (53 paths) against the
"Current entry point" column of every `^| PAR-` row.
**Question its output must answer:** which of the 53 page models is named by no row? Expected
empty given F-4, but the join has not been run. Each entry needs one line of explanation.

### NOT YET CAPTURED — U-3: difference list (b) — `PAR` rows whose page model no longer exists

**Command:** for each of the 46 rows, `test -f src/Pegasus.Web/Pages/<cited path>`.
**Question its output must answer:** does every cited entry point still exist at `bbd1c549`?
`PAR-45` and `PAR-46` are exempt (they cite `Program.cs` and `Mcp/`, verified in F-12).

### NOT YET CAPTURED — U-4: difference list (c) — handlers in code missing from a row's handler list

**Command:** per-file diff of the F-6 output against each row's handler cell.
**Question its output must answer:** which handlers exist in code but are not listed on their
row, and which are listed but no longer exist? This is the list that decides whether
"every `OnGet*`/`OnPost*` handler appears exactly once across the matrix rows" (acceptance
criterion 2) can be claimed.

### NOT YET CAPTURED — U-5: command-set expansion for every multi-command handler

**Command:** read the dispatch body of each handler that hides more than one command and list
the commands by name. `Triage/Details.OnPostActionAsync` is **already done** (F-10). Still owed,
at minimum: `Cases/Workflow.cshtml.cs` (7 handlers), `Cases/Tasks.cshtml.cs` (8),
`Cases/Custody.cshtml.cs` (6), `Cases/Details.cshtml.cs` (6),
`Cases/Assessment/Index.cshtml.cs` (7), `Intake/Details.cshtml.cs` (10),
`Mail/Message.cshtml.cs` (7), `Administration/Automation/Index.cshtml.cs` (6),
`Operations/Index.cshtml.cs` (3), `Administration/Mailboxes.cshtml.cs` (3),
`Cases/Vehicle.cshtml.cs` (3), `UploadGroupStatus.cshtml.cs` (3), `Connect/Authorize.cshtml.cs` (3).
**Question its output must answer:** does any *other* handler hide a command set the way
`OnPostActionAsync` does — i.e. is F-10 the only dispatcher, or one of several?
(Counts above are from the F-6 per-file output; a handler count > 1 is a candidate, not proof.)

### NOT YET CAPTURED — U-6: the reconciled skeleton table

**Command:** none — this is the assembly step. One table of
`PAR id → page model path → handlers (commands expanded) → base class → inventoried-at SHA`,
covering all 46 rows, written into this document (body step 11).
**Question its output must answer:** can [[FND-015]]…[[FND-018]] start from this table alone,
without re-running any enumeration?

### NOT YET CAPTURED — U-7: A-01-1's confirming command

**Command:** `git grep -n "MapGet(\|MapPost(\|MapPut(\|MapDelete(\|AddControllers" src/Pegasus.Web`
**Question its output must answer:** is there any HTTP surface in `Pegasus.Web` beyond the
Razor pages and the four registrations of F-12? A hit that is not health, version, the token
endpoint or `MapMcp` is a capability with no inventory row.

### NOT YET CAPTURED — U-8: A-01-5's confirming command

**Command:** `git log -p 191ddf33..HEAD -- src/Pegasus.Web/Pages/Triage/Details.cshtml.cs`
**Question its output must answer:** was a thirteenth Triage command removed since the matrix
was pre-populated (a behaviour regression to escalate), or was "13" always a miscount (a
documentation correction)?

### NOT YET CAPTURED — U-9: the two documentation gates

**Commands:** `pwsh ./scripts/Test-DocumentationLinks.ps1` and
`pwsh ./scripts/Test-MarkdownPlacement.ps1`, both expected to exit 0 after the matrix and
README edits.
**Question its output must answer:** do the edits keep the CI `documentation` job green?

## Open questions

Recorded as unticked items in this ticket's `open-questions` document (body step 12); every one
must be ticked before `enter-done`.

- U-1 … U-9 above.
- **Is `PAR-24`'s "13" a miscount or a removed command?** (U-8). If removed, this stops being a
  documentation ticket and becomes an escalation to [[FND-052]] / the operator.
- **Does `Cases/Documents/Export` or `Cases/Eva/Download` account for the plan's missing
  eighth `CaseMutationPageModel` deriver** (F-9b), and should the plan's `README.md:50` count
  be corrected in the same PR as the `README.md:48` path citation? Body step 10 authorises only
  the path citation, so the count correction needs an explicit decision.

**Not open questions — scope boundaries owned by named tickets:**

- Advancing any row's Status: [[FND-015]] (`DSK-01-02`), [[FND-016]] (`DSK-01-03`),
  [[FND-017]] (`DSK-01-04`), [[FND-018]] (`DSK-01-05`).
- Whether the matrix later moves to `docs/features/`: [[FND-012]] (plan handle `DSK-00-12`).
- Re-verifying rows after the first upstream sync: [[FND-023]] (`DSK-01-10`) and the standing
  [[FND-051]] (`DSK-01-13`).
- The characterization-gap list the `gap:` cells feed: [[FND-025]] (`DSK-01-12`).
