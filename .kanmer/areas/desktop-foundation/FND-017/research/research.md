# Research — FND-017: Parity rows for §13.4 intake, §13.7 documents and evidence, §13.8 communications

> **STATUS — INCOMPLETE. Do not move this ticket to Done yet.**

This is a `spike`. Its `research` document is the spike's **output**, not an input to it, and
its existence alone satisfies the `enter-done` gate (`get_doc_gates FND-017`: `enter-done`
needs `research` + `questions-resolved`, and it is this profile's only gated boundary). This
file was written **before the spike ran**, as the pre-work scaffold the authoring contract
requires. Everything under **Facts** is verified against the repository with the command that
produced it. Everything marked `NOT YET CAPTURED` is still owed and has an unticked box in this
ticket's `open-questions` document — those boxes, not this banner, block `enter-done`.

Baseline: `git rev-parse HEAD` → `bbd1c54959e8c3a361d3f73965b61d6e4aff59ec`, read 2026-08-24.

**Dependency, not an open question:** written against the confirmed skeleton of [[FND-014]]
(plan handle `DSK-01-01`). Read its `research` with `get_ticket_doc` before writing a cell.

## Question

For the intake, documents and communications rows — `PAR-13`, `PAR-16`, `PAR-17` and
`PAR-19`…`PAR-31` — what are the exact handlers, the command sets hidden behind them, the
envelope and integrity limits, the upstream redesign tickets each native-screen cell must cite,
and which `to locate` cells become a real test path versus an explicit `gap:`?

## Current behaviour

Rows owned, with handler counts measured at `bbd1c549`:

| Row | §13.x | Entry point(s) | Handlers | Status today |
| --- | --- | --- | --- | --- |
| `PAR-13` | 13.7 | `Cases/Custody.cshtml.cs` (270) | 6 | `inventoried` |
| `PAR-16` | 13.7 | `Cases/Documents/Download.cshtml.cs` (112) | 1 | `not inventoried` |
| `PAR-17` | 13.7 | `Cases/Documents/Export.cshtml.cs` (160) | 1 | `not inventoried` |
| `PAR-19` | 13.4 | `Intake/Details.cshtml.cs` (613) | 10 | `inventoried` |
| `PAR-20` | 13.4 | `Intake/Asset.cshtml.cs` (80), `Image.cshtml.cs` (79), `Source.cshtml.cs` (78) | 1 + 1 + 1 | `inventoried` |
| `PAR-21` | 13.8 | `Mail/Index.cshtml.cs` (428) | 2 | `inventoried` |
| `PAR-22` | 13.8 | `Mail/Message.cshtml.cs` (1,025) | 7 | `inventoried` |
| `PAR-23` | 13.4 (Triage) | `Triage/Index.cshtml.cs` (449) | 1 | `not inventoried` |
| `PAR-24` | 13.4 (Triage) | `Triage/Details.cshtml.cs` (496) | 2 (one hides 12 commands) | `not inventoried` |
| `PAR-25` | 13.4 (Unidentified) | `Unidentified/Index.cshtml.cs` (19), `Details.cshtml.cs` (180) | 1 + 2 | `not inventoried` |
| `PAR-26` | 13.4 (images) | `ImageIntake/Index.cshtml.cs` (85), `Details.cshtml.cs` (89) | 1 + 2 | `not inventoried` |
| `PAR-28` | 13.4 (manual) | `Upload.cshtml.cs` (183) | 2 | `inventoried` |
| `PAR-29` | 13.4 (manual) | `UploadStatus.cshtml.cs` (83) | 1 | `not inventoried` |
| `PAR-30` | 13.4 (manual) | `UploadGroupStatus.cshtml.cs` (225) | 3 | `not inventoried` |
| `PAR-31` | 13.4 (external) | `Uploads/Request.cshtml.cs` (222) | 2 | `legacy path retained` |

**`PAR-27` is listed in this ticket's range but is owned by [[FND-018]]** — see F-2. Do not
fill it here.

Today: received items arrive through the Worker's mailbox poll or the staff Upload form, are
staged with an opaque receipt, and are worked from `Intake/Details` with named commands that
each carry a server-derived actor plus expected version, case lease, operation key and reason.
Triage is a pre-case entity with its own lifecycle behind a single dispatching handler.
Retained mail is searched and read from `Mail/`, with link/unlink into a case under a case
lease. Documents are held in Box custody, downloaded as bytes with a no-sniff attachment
response, and exported as an archive.

## Findings

- **`PAR-27` is claimed by two tickets.** The overlap is a range-notation artefact; the
  capability groups settle it in [[FND-018]]'s favour — F-2. Confirm before either ticket
  writes the row.
- **`PAR-24`'s "13 commands" is 12 at this head** — F-4, and the twelve are named there.
- **`PAR-19`'s "ten named commands" is nine commands plus one read handler** — F-5.
- **`PAR-28`'s "one file" is wrong at this head: the Upload form binds `IFormFile[]` and
  accepts a batch of up to 20** — F-7. The body's step-6 numbers, and
  `docs/engineering.md:85`, still describe the superseded single-file envelope.
- **The MCP surface exposes 10 mutating Triage tools against the page's 12 commands** —
  `assign` and `unassign` have no MCP counterpart — F-4a. That is the answer to the body's
  step-3 cross-check.
- Every upstream redesign ticket the rows must cite exists in the carry-over table — F-9 — and
  three of the four ids collide with fork board ids, so the full `upstream:<ID>` form is
  mandatory.

### Facts

Verified at `bbd1c549` on 2026-08-24, each with its command.

- **F-1 — 47 handlers across the owned surface.**
  `git grep -n "public .*On\(Get\|Post\)" -- 'src/Pegasus.Web/Pages/Intake' 'src/Pegasus.Web/Pages/Triage' 'src/Pegasus.Web/Pages/Unidentified' 'src/Pegasus.Web/Pages/ImageIntake' 'src/Pegasus.Web/Pages/Mail' 'src/Pegasus.Web/Pages/Uploads' 'src/Pegasus.Web/Pages/Upload.cshtml.cs' 'src/Pegasus.Web/Pages/UploadStatus.cshtml.cs' 'src/Pegasus.Web/Pages/UploadGroupStatus.cshtml.cs'`
  → 39 handlers, plus `Cases/Custody.cshtml.cs` (6), `Cases/Documents/Download.cshtml.cs` (1)
  and `Cases/Documents/Export.cshtml.cs` (1) from the `Cases/` enumeration = **47** across the
  15 owned rows. Per-row counts are in the table above; per-handler lines:
  `Intake/Details` `OnGetAsync :95`, `OnPostRetryAllocationAsync :111`, `OnPostBlockAsync :157`,
  `OnPostReevaluateAsync :178`, `OnPostCorrectDraftAsync :192`, `OnPostClaimCaseLeaseAsync :240`,
  `OnPostLinkCaseAsync :274`, `OnPostReverseCaseLinkAsync :310`,
  `OnPostRegisterImageIntakeAsync :513`, `OnPostDismissSuggestionAsync :535`;
  `Intake/Asset :20`, `Intake/Image :20`, `Intake/Source :11`;
  `Mail/Index` `OnGetAsync :69`, `OnGetPreviewAsync :158`;
  `Mail/Message` `OnGetAsync :157`, `OnPostPrepareLinkCaseAsync :199`,
  `OnPostPrepareUnlinkCaseAsync :260`, `OnPostLinkCaseAsync :318`, `OnPostUnlinkCaseAsync :383`,
  `OnPostCorrectClassificationAsync :448`, `OnPostMoveToRecommendedFolderAsync :511`;
  `Triage/Index :199`; `Triage/Details` `OnGetAsync :56`, `OnPostActionAsync :85`;
  `Unidentified/Index :17` (`OnGet() =>` redirect-style expression body),
  `Unidentified/Details` `OnGetAsync :88`, `OnPostResolveAsync :93`;
  `ImageIntake/Index :27`, `ImageIntake/Details` `OnGetAsync :26`, `OnPostCloseAsync :48`;
  `Upload` `OnGet :43`, `OnPostAsync :48`; `UploadStatus` `OnGetAsync :56`;
  `UploadGroupStatus` `OnGetAsync :61`, `OnPostRegisterGroupAsync :64`,
  `OnPostAttachGroupAsync :130`; `Uploads/Request` `OnGetAsync :31`, `OnPostAsync :52`;
  `Cases/Custody` `:28`, `:74`, `:138`, `:162`, `:186`, `:237`;
  `Cases/Documents/Download :16`; `Cases/Documents/Export :18`.
- **F-2 — `PAR-27` is claimed by two ticket bodies; the capability groups settle it.**
  This ticket's *What* and acceptance criteria use the range `PAR-19`–`PAR-31` / `PAR-19`–`PAR-30`,
  which sweeps `PAR-27` in. But: (a) none of this ticket's twelve implementation steps mentions
  `PAR-27`, and its step-11 `to locate` list (`PAR-16`, `PAR-17`, `PAR-23`, `PAR-24`, `PAR-25`,
  `PAR-26`, `PAR-29`, `PAR-30`, `PAR-31`) omits it; (b) `PAR-27` is capability group
  **13.10 Administration and operations**, which is not one of this ticket's three groups
  (13.4, 13.7, 13.8); (c) [[FND-018]] (plan handle `DSK-01-05`) names `PAR-27` in its *What*,
  gives it a dedicated step 6 ("Fill `PAR-27` (Operations) … cite `upstream:PLAT-023`") and
  lists it in its acceptance criteria. **The determinate reading is that [[FND-018]] owns
  `PAR-27` and the range notation here is an artefact.** Both bodies are settled, so this is
  recorded rather than silently resolved: confirm with [[FND-018]] before either ticket writes
  the row (open question U-11). Filling it twice, or not at all, is the failure mode.
- **F-3 — Intake byte-route guards, cited precisely.** `PAR-20`'s three pages all set
  `Response.Headers.XContentTypeOptions = "nosniff"` —
  `Intake/Asset.cshtml.cs:48`, `Intake/Image.cshtml.cs:48`, `Intake/Source.cshtml.cs:30` — and all
  three go through `IDownloadIntakeSource` with a `DownloadIntakeSourceQuery(id, actor)`
  (`Image.cshtml.cs:17,:32`; `Source.cshtml.cs:8,:23`). The integrity check is in Core:
  `src/Pegasus.Core/Intake/DownloadIntakeSource.cs:40`
  `var actualHash = Convert.ToHexString(SHA256.HashData(content.Span));` — the retained hash is
  recomputed and compared on every read. Sibling recomputations exist at
  `Intake/DurableIntake.cs:533`, `Intake/InstructionEvidenceImages.cs:162`. The route-denial
  evidence is `tests/Pegasus.IntegrationTests/LocalIntakeAccessTests.cs` (exists).
- **F-4 — `Triage/Details.OnPostActionAsync` dispatches 12 named commands, not 13.**
  `git grep -c 'case "' src/Pegasus.Web/Pages/Triage/Details.cshtml.cs` → `12`. The handler opens
  at `:85`; the `switch (actionName)` ends in a `default:` at `:214` that throws
  `ArgumentException("The requested Triage action is not supported.")` — a guard, not a command.
  In source order with lines: `assign` (`:116`), `unassign` (`:127`), `await_information`
  (`:130`), `record_finding` (`:133`), `supersede_finding` (`:146`), `link_response` (`:159`),
  `unlink_response` (`:174`), `complete` (`:185`), `cancel` (`:188`), `reopen` (`:191`),
  `link_case` (`:194`), `unlink_case` (`:204`). `link_case`/`unlink_case` route through
  `ExecuteCaseAssociationAsync(linking: …)` rather than a direct use case. This ticket's own
  Verification item says "13 per the plan; **state the actual number**" — the actual number is
  **12**.
- **F-4a — The MCP cross-check (body step 3) answered in one direction.**
  `git grep -oh 'pegasus_[a-z_]*' src/Pegasus.Web/Mcp/ | sort -u` lists 13 `pegasus_triage_*`
  tools: 10 mutating (`await_information`, `cancel`, `case_link`, `case_unlink`, `complete`,
  `record_finding`, `reopen`, `response_link`, `response_unlink`, `supersede_finding`) and 3
  reading (`get`, `list`, `source_download`). **`assign` and `unassign` exist on the page and
  have no MCP counterpart.** Nothing is exposed to MCP that the page lacks. That asymmetry is a
  finding to record on `PAR-24` and `PAR-46`, not a defect to fix here.
- **F-5 — `PAR-19` has nine commands and one read handler, not "ten named commands".**
  F-1 shows `Intake/Details.cshtml.cs` with 10 handlers, of which `OnGetAsync` (`:95`) is the
  read. The nine commands the body enumerates — retry allocation, block, re-evaluate, correct
  draft, claim case lease, link case, reverse case link, register image intake, dismiss
  suggestion — are exactly the nine `OnPost*` handlers. Write nine commands and name the tenth
  handler as the read; "ten named commands" would leave the reader looking for a missing one.
- **F-6 — Mail handlers include a JSON read.** `Mail/Index.OnGetPreviewAsync` (`:158`) is a
  handler returning a preview payload, distinct from the page `OnGetAsync` (`:69`); `PAR-21`
  already records it. `Mail/Message` (1,025 lines) has 7 handlers (F-1), including the
  **two-phase** link/unlink pattern: `OnPostPrepareLinkCaseAsync` (`:199`) then
  `OnPostLinkCaseAsync` (`:318`), and the same for unlink (`:260`, `:383`). A desktop that
  models link as one call will miss the prepare step; record the pair explicitly.
- **F-7 — `PAR-28`'s envelope facts, measured — and the row and the plan are both stale.**
  `src/Pegasus.Core/Intake/IntakeContracts.cs:7` `public static class IntakeEnvelopeLimits`:
  - `:13` `MaximumContentLength = 10 * 1024 * 1024` — **10 MiB, per file**, documented as "One
    file uploaded through the staff form, which arrives inside one bounded multipart HTTP request."
  - `:33` `MaximumMailboxContentLength = 750L * 1024 * 1024` — **750 MiB** for one received
    message with envelope and all attachments, with a long rationale at `:17-32`: a received
    instruction carries "the covering message plus the 2–20+ documents and photographs of the
    job", and applying the one-file figure to the whole envelope "refused real QDOS instructions
    outright — a 16.69 MB forward was rejected as `message_too_large` on 2026-08-05 without ever
    being read." It is "deliberately permissive rather than a capacity claim".
  - `:42` `MaximumBatchFileCount = 20`.
  - `:49-50` `MaximumBatchContentLength = (MaximumBatchFileCount * MaximumContentLength) + MultipartOverhead`
    — 20 × 10 MiB + 64 KiB.
  - `:56` `MultipartOverhead = 64 * 1024`.
  `src/Pegasus.Web/Program.cs:525-530` sets `FormOptions.MultipartBodyLengthLimit =
  IntakeEnvelopeLimits.MaximumBatchContentLength`, with the comment "Bounded for a whole Upload
  batch, not one file".
  `src/Pegasus.Web/Pages/Upload.cshtml.cs:38` binds **`public IFormFile[] Upload`**, and `:35`,
  `:68`, `:72`, `:84` enforce `MaximumBatchFileCount` and the per-file `MaximumContentLength`.
  **Consequences:** the matrix's `PAR-28` cell "(`IFormFile`, 10 MiB, one file)" is stale; this
  ticket's step 6 phrase "one file, 10 MiB, 10 MiB plus 64 KiB multipart envelope" describes the
  superseded single-file form; and `docs/engineering.md:85` (tier 10) still says "the one-file
  10 MiB limit and 10 MiB-plus-64-KiB multipart envelope". `docs/index.md` § Authority says
  "Code plus passing tests beat any document about current state", and the body's own step-6
  command (`git grep -n "IntakeEnvelopeLimits" …`) is what produced these numbers — so record the
  measured values and flag the two documents rather than copying the old figure. The
  documentation correction itself is **not** this ticket's to make (open question U-10).
  The mailbox figure matters for `PAR-21`/`PAR-22`: the desktop's inbox must show a
  `message_too_large` state, and 750 MiB is the bound behind it.
- **F-8 — `PAR-31` still matches the code.** `Uploads/Request.cshtml.cs` is 222 lines with
  `OnGetAsync` (`:31`) and `OnPostAsync` (`:52`), anonymous with the request-link actor,
  antiforgery and PRG. It is the anonymous external audience and stays server-rendered
  (area 01 § 3, the recorded Deviation from proposal § 23's ladder). `legacy path retained`
  stands; write the one-sentence reason.
- **F-9 — Every upstream redesign ticket the rows must cite exists, and three of the four ids
  collide with fork board ids.** `docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md`
  rows: `INTK-019` (`:154`, "Replace Triage 'Assign to me' with Engineer selection", target
  06 Triage detail), `DOCS-011` (`:121`, evidence preview pane), `DOCS-012` (`:122`, Evidence
  tab), `CASE-022` (`:111`, "Make creating a public upload link findable", Documents tab
  commands). Per the group document `HZN-001` / `board-conventions.md` § *Upstream ids versus
  board ids*, a bare `<PREFIX>-<nnn>` on this board is a **fork board id**: board `CASE-002` is
  upstream `CASE-022`, board `INTK-001`…`INTK-007` are upstream `INTK-002`/`003`/`026`/`027`/
  `031`/`032`/`033` (no formula — read the table), and board `DOCS-001`/`DOCS-002`/`DOCS-003`
  are upstream `DOCS-001`/`TICK-018`/`TICK-208`. So the matrix cells must read
  `upstream:INTK-019`, `upstream:DOCS-011`, `upstream:DOCS-012`, `upstream:CASE-022` exactly,
  never bare (body step 10; acceptance criterion 5).
- **F-10 — `PAR-22`'s design input exists.**
  `ls docs/design/references/mockups/inbox-message-page/` → `Main.dc.html`, `Case.dc.html`,
  `CaseLinked.dc.html`, `Correcting.dc.html`, `Dialogs.dc.html`, `Filed.dc.html`,
  `FolderStates.dc.html`, `Moving.dc.html`, `README.md`, `canvas.json`. The directory the body
  requires be confirmed before citing (its third Verification item) is present, and the state
  names are themselves the row's native-screen evidence.
- **F-11 — Named test files that exist** (`test -f`):
  `tests/Pegasus.IntegrationTests/QdosIntakeWebTests.cs`, `MultiFormatIntakeWebTests.cs`,
  `LocalIntakeAccessTests.cs`, `IntakeStablePersistenceTests.cs`, `MailWorkspaceWebTests.cs`,
  `RetainedMailPersistenceTests.cs`, `CustodyOutboxIntegrationTests.cs`,
  `Browser/MailWorkspaceBrowserTests.cs`, `Browser/UploadDropzoneBrowserTests.cs`,
  `Browser/UploadRowsBrowserTests.cs`. None of the matrix's cited paths is stale, so every
  `to locate` cell is a genuine unfilled gap rather than a typo.
- **F-12 — The documentation gate takes no parameters.** `scripts/Test-DocumentationLinks.ps1:9`
  is `[CmdletBinding()] param()`.

### Assumptions

- **A-01-04-1 — The 15 owned rows are the complete §13.4/§13.7/§13.8 surface.** Confirmed by
  F-1: 47 handlers map with none left over. Breaks if [[FND-014]]'s difference list (a) names an
  intake, document or mail page model with no row. **Confirm by reading [[FND-014]]'s difference
  lists**, not yet written.
- **A-01-04-2 — `PAR-24`'s "13" was a miscount, not a removed command.** [[FND-014]] carries the
  same assumption (its A-01-5) and the confirming command
  `git log -p 191ddf33..HEAD -- src/Pegasus.Web/Pages/Triage/Details.cshtml.cs`. Breaks if a
  thirteenth command was deleted since the matrix was pre-populated — a behaviour regression, not
  a documentation error. **Do not duplicate the check**: read [[FND-014]]'s answer.
- **A-01-04-3 — The Upload form's batch behaviour is current and intended, not a half-landed
  change.** Based on F-7: the Core constants, the `FormOptions` wiring and the page binding all
  agree, and each carries an explanatory comment. Breaks if the batch path is feature-gated off
  in production. **Confirm with** `git grep -n "Features:" src/Pegasus.Web/Pages/Upload.cshtml.cs src/Pegasus.Core/Intake/GroupedIntake.cs`
  — not yet run. If it is gated, `PAR-28` must record the production-reachable behaviour, because
  parity is measured against live production behaviour.
- **A-01-04-4 — `IGroupedIntakeSubmission` is the current staff-upload use case** (it is what
  `UploadModel` takes at `Upload.cshtml.cs:28`). Breaks if `ReceiveIntake` — which the matrix's
  `PAR-28` cell names — is a different, still-live path. **Confirm by reading**
  `src/Pegasus.Core/Intake/GroupedIntake.cs` and whichever type declares `ReceiveIntake`.
- **A-01-04-5 — `PAR-17`'s export test really does arrive with the upstream sync.** The body
  says to leave the cell as `gap: arrives with upstream sync CASE-019` until then, and the matrix
  attributes the proof to upstream `CASE-019` at upstream commit `efbb2a9`. Breaks if the sync
  ([[FND-023]], plan handle `DSK-01-10`) lands without that test. **Confirm after the sync**, not
  now — and write the id as `upstream CASE-019`, never bare (F-9).

## Execution placement

**This ticket places no responsibility anywhere.** It is read-only inspection of
`src/Pegasus.Web`, `src/Pegasus.Core`, `tests/` and `infra/`, plus edits to
`docs/desktop/01-inventory-and-parity/parity-matrix.md` and possibly `docs/open-decisions.md`.
It runs no intake, starts no Worker, touches no `corpus/`, holds no credential and makes no
Azure call — the Guardrails say "no write, and no Azure read is needed either". The
six-question cloud-justification test of `docs/desktop/00-governance-and-workflow/README.md`
§ 3 is therefore not answered here.

The one placement it **assumes**: the enumeration runs on a developer workstation against a
local checkout, and its output is a repository document.

Two placements the rows **record** (they are decided elsewhere and are cited, not re-argued):
proposal § 4.1 (`docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md:140-162`) fixes
`Microsoft Graph intake/polling` → **Cloud required** ("Poll/deduplicate while desktops are
closed"), which ADR-0106 will carry, and `Box document browsing` → **Split**, which ADR-0107
will carry — both authored by [[FND-005]] (plan handle `DSK-00-05`) and [[FND-006]]. Record the
value; the six-question tables belong to those ADRs. **The matrix has no placement column**
([[FND-015]] F-13): this ticket takes the same default as [[FND-015]] and [[FND-016]] — a
leading `Placement: <value> (proposal §4.1)` clause inside the existing "Native screen/use case"
cell — and parks the schema question.

## Implications

1. **Three rows carry a number that must not be copied**: `PAR-24` (12, not 13 — F-4),
   `PAR-19` (nine commands plus a read — F-5), `PAR-28` (a batch of up to 20 files, not one —
   F-7). All three are exactly the "inventory by page count misses behaviour" trap the area plan
   names.
2. **The batch-upload finding reaches beyond this ticket.** The desktop transfer queue (area 05,
   §14.6) must model 20 files at 10 MiB each behind a ~200 MiB multipart budget, not a single
   10 MiB post. A slice built from `docs/engineering.md:85` would size it wrongly.
3. **Mail link/unlink is two-phase (F-6).** `~POST /api/v1/mail/{id}/link-case` in area 03 needs
   a prepare step or an equivalent confirmation contract.
4. **The Triage MCP asymmetry (F-4a) is a real parity input**, not trivia: `pegasus_*` tools are
   the reference projection for `/api/v1` shapes (`PAR-46`), and two commands are missing from
   it. Area 03 should decide deliberately whether `/api/v1` follows the page or the MCP surface.
5. **`PAR-27`'s double claim (F-2) must be settled before either ticket edits the matrix**,
   or the row is written twice or not at all.
6. **Every upstream id in these cells is a collision risk (F-9).** Three of the four prefixes
   exist as fork board ids with different meanings.

---

## NOT YET CAPTURED — the spike's remaining work

Each block names the exact command and the question its output must answer; each has one
unticked box in `open-questions`.

### NOT YET CAPTURED — U-1: the row-by-row citation table

**Command:** none — assembly. One table for the 15 owned rows:
`PAR id → entry point(s) → handlers (path:line) → command set expanded → Core owner (path:line)
→ FRD owner → test file or gap: → upstream redesign id (upstream:<ID>) → placement (§4.1) →
inventoried-at SHA` (body step 12).

### NOT YET CAPTURED — U-2: handler-to-row mapping proof

**Command:** the F-1 grep, plus `git grep -n "public .*On\(Get\|Post\)" -- 'src/Pegasus.Web/Pages/Cases/Custody.cshtml.cs' 'src/Pegasus.Web/Pages/Cases/Documents'`.
**Question it must answer:** does every one of the 47 handlers land in exactly one row this
ticket owns?

### NOT YET CAPTURED — U-3: `PAR-24` commands mapped to `TriageLifecycle`

**Command:** for each of the 12 commands in F-4, cite the transition in
`src/Pegasus.Core/Triage/TriageLifecycle.cs` (561 lines) by line, and re-run the MCP comparison
of F-4a against `src/Pegasus.Web/Mcp/TriageMcpTools.cs`.
**Question it must answer:** which `TriageLifecycle` transition backs each of the 12, and is the
`assign`/`unassign` MCP gap the only asymmetry in either direction? (Body step 3.)

### NOT YET CAPTURED — U-4: `PAR-19` commands with their envelope requirements

**Command:** read each of the nine `OnPost*` handlers of F-1 and record, per command, that the
actor is server-derived and which of expected version, case lease, operation key and reason it
requires.
**Question it must answer:** what exactly must a desktop command send for each of the nine?
(Body step 4.)

### NOT YET CAPTURED — U-5: A-01-04-3 and A-01-04-4 settled for `PAR-28`

**Commands:** `git grep -n "Features:" src/Pegasus.Web/Pages/Upload.cshtml.cs src/Pegasus.Core/Intake/GroupedIntake.cs`
and read `src/Pegasus.Core/Intake/GroupedIntake.cs` plus whatever declares `ReceiveIntake`.
**Question it must answer:** is the 20-file batch path production-reachable today, and is
`IGroupedIntakeSubmission` (not `ReceiveIntake`) the current staff-upload use case? Parity is
measured against live production behaviour, so a gated path is recorded as gated.

### NOT YET CAPTURED — U-6: `PAR-13`, `PAR-16`, `PAR-17` documents rows

**Command:** read `src/Pegasus.Web/Pages/Cases/Custody.cshtml.cs` (6 handlers at F-1's lines)
and the two `Cases/Documents/` pages; cite
`tests/Pegasus.IntegrationTests/CustodyOutboxIntegrationTests.cs`.
**Question it must answer:** what does each custody command require, and does `PAR-17` still
need `gap: arrives with upstream sync CASE-019`? (Body step 8; A-01-04-5.)

### NOT YET CAPTURED — U-7: `PAR-21`/`PAR-22` communications rows

**Command:** read `src/Pegasus.Web/Pages/Mail/Index.cshtml.cs` and `Mail/Message.cshtml.cs`;
cite `MailWorkspaceWebTests.cs`, `RetainedMailPersistenceTests.cs`,
`Browser/MailWorkspaceBrowserTests.cs`, and the mockup states of F-10.
**Question it must answer:** does the row record the two-phase link/unlink pair (F-6), the
`message_too_large` state and its 750 MiB bound (F-7), and the Deleted Items search cap of 100?

### NOT YET CAPTURED — U-8: `to locate` cells resolved

**Command:** `git grep -rln` searches over `tests/` for each of `PAR-16`, `PAR-17`, `PAR-23`,
`PAR-24`, `PAR-25`, `PAR-26`, `PAR-29`, `PAR-30`, `PAR-31`, **opening each candidate before
citing it**.
**Question it must answer:** for each row, is there a test that asserts the behaviour the cell
claims? Where none does, write `gap: <untested behaviour>` and copy the line into this document
for [[FND-025]] (plan handle `DSK-01-12`). (Body step 11.)

### NOT YET CAPTURED — U-9: the matrix edits

**Command:** none — the edit. Rows to `inventoried`, `PAR-31` to `legacy path retained` with its
reason, SHA stamped, `upstream:<ID>` cells written in full, `~` names and blank UAT owners
untouched, `PAR-27` left to [[FND-018]] once U-11 confirms it.

### NOT YET CAPTURED — U-10: the documentation gate

**Command:** `pwsh ./scripts/Test-DocumentationLinks.ps1` — exit 0.

## Open questions

Tracked as unticked items in this ticket's `open-questions` document.

- U-1 … U-10 above, plus:
- **U-11 — who owns `PAR-27`?** (F-2.) Settle with [[FND-018]] before either ticket edits the row.
- **U-12 — the stale one-file envelope in `docs/engineering.md:85`** (F-7). This ticket may
  record the measured values on `PAR-28`, but correcting a working-rules document is outside its
  editable set (Guardrails allow only `parity-matrix.md` and `docs/open-decisions.md`). Decide
  whether it becomes a one-line `docs/open-decisions.md` entry, a note to [[FND-052]], or a
  separate `fix` ticket — and do not leave a governing document contradicting the code silently.

**Not open questions — scope boundaries owned by named tickets:**

- The confirmed skeleton and the three difference lists: [[FND-014]] (`DSK-01-01`), which also
  owns the `PAR-24` 12-vs-13 history check (A-01-04-2).
- `PAR-07`…`PAR-12`, `PAR-40`, `PAR-41`: [[FND-016]] (`DSK-01-03`). `PAR-14`, `PAR-15`,
  `PAR-18`, `PAR-27`, `PAR-32`…`PAR-39`, `PAR-43`, `PAR-45`, `PAR-46`: [[FND-018]] (`DSK-01-05`).
- The Graph intake and Box custody flow records: [[FND-019]] (`DSK-01-06`) and [[FND-020]]
  (`DSK-01-07`).
- Promoting a `~` endpoint name: area 03's endpoint map.
- The upstream sync that brings upstream `CASE-019`'s proof: [[FND-023]] (`DSK-01-10`).
- The characterization-gap list: [[FND-025]] (`DSK-01-12`).
- **Send to AI / upstream `TICK-102` is a recorded exclusion with a reactivation condition, not
  an open question**, and no `open-questions` item is created for it on any ticket. The
  reactivation condition is the separate non-preview transport decision named at
  `docs/capabilities.md:269`. It is [[FND-022]]'s (`DSK-01-09`) step 10 to record, not this
  ticket's.
