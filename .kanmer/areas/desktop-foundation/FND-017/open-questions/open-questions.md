# Open questions — FND-017 (DSK-01-04)

The ticket body step 12 instructs: "tick `open-questions/`, then `get_doc_gates` and move to
Done." These are those items.

This is a `spike`. Its `research` document **is** the deliverable and writing it satisfies the
`enter-done` gate on its own — so the unticked boxes below are what actually keep an unfinished
spike from being closed. For a `spike` an unticked box blocks **`enter-done` and nothing else**
(`get_doc_gates FND-017` lists `enter-done` as this profile's only gated boundary); it does not
gate `leave-backlog`, and the ticket can move through Preparing, Implementing, Review and
Verifying with these open.

Each item is tickable when its output is recorded in `research`, replacing the matching
`NOT YET CAPTURED` block.

## Must be answered before this spike is Done

- [ ] **U-1 — The row-by-row citation table is written into `research`.** One table for the 15
      owned rows: entry point(s) → handlers (`path:line`) → command set expanded → Core owner
      (`path:line`) → FRD owner → test file or `gap:` → upstream redesign id in full
      `upstream:<ID>` form → §4.1 placement → inventoried-at SHA (body step 12).
- [ ] **U-2 — Every handler maps to exactly one row.** Run the research F-1 grep plus
      `git grep -n "public .*On\(Get\|Post\)" -- 'src/Pegasus.Web/Pages/Cases/Custody.cshtml.cs' 'src/Pegasus.Web/Pages/Cases/Documents'`
      and show all **47** handlers landing in exactly one owned row.
- [ ] **U-3 — `PAR-24`'s command set is written and mapped.** List the **12** commands research
      F-4 measured (`assign`, `unassign`, `await_information`, `record_finding`,
      `supersede_finding`, `link_response`, `unlink_response`, `complete`, `cancel`, `reopen`,
      `link_case`, `unlink_case`) one per line, each mapped to its transition in
      `src/Pegasus.Core/Triage/TriageLifecycle.cs` by line. **State 12, not the 13 the plan
      says** — the ticket's own Verification item requires the actual number. Re-run the MCP
      cross-check of research F-4a against `src/Pegasus.Web/Mcp/TriageMcpTools.cs` and record
      the `assign`/`unassign` gap as a finding (body step 3).
- [ ] **U-4 — `PAR-19`'s nine commands are written with their envelope requirements.** Read each
      of the nine `OnPost*` handlers (research F-1 lists their lines) and record, per command,
      that the actor is server-derived and which of expected version, case lease, operation key
      and reason it requires. Name the tenth handler, `OnGetAsync` (`:95`), as the read — the
      body's "ten named commands" is nine commands plus one read (research F-5).
- [ ] **U-5 — Assumptions A-01-04-3 and A-01-04-4 are settled for `PAR-28`.** Run
      `git grep -n "Features:" src/Pegasus.Web/Pages/Upload.cshtml.cs src/Pegasus.Core/Intake/GroupedIntake.cs`
      and read `src/Pegasus.Core/Intake/GroupedIntake.cs` plus whatever declares `ReceiveIntake`.
      Is the 20-file batch path production-reachable today, and is `IGroupedIntakeSubmission`
      (not `ReceiveIntake`) the current staff-upload use case? Parity is measured against live
      production behaviour, so a gated path must be recorded as gated.
- [ ] **U-6 — `PAR-13`, `PAR-16`, `PAR-17` are filled.** Read `Cases/Custody.cshtml.cs` (6
      handlers) and the two `Cases/Documents/` pages; cite
      `tests/Pegasus.IntegrationTests/CustodyOutboxIntegrationTests.cs`. Decide whether `PAR-17`
      still carries `gap: arrives with upstream sync CASE-019` — writing the id as
      `upstream CASE-019`, never bare (body step 8).
- [ ] **U-7 — `PAR-21` and `PAR-22` are filled.** Record the two-phase link/unlink pair
      (research F-6: `OnPostPrepareLinkCaseAsync :199` → `OnPostLinkCaseAsync :318`, and the
      unlink equivalents `:260` → `:383`), the `message_too_large` state with its 750 MiB bound
      (`src/Pegasus.Core/Intake/IntakeContracts.cs:33`), and the Deleted Items search cap. Cite
      the mockup states in `docs/design/references/mockups/inbox-message-page/` (research F-10
      confirms the directory and its eight state files exist).
- [ ] **U-8 — Every `to locate` cell in the owned rows is resolved.** `git grep -rln` over
      `tests/` for `PAR-16`, `PAR-17`, `PAR-23`, `PAR-24`, `PAR-25`, `PAR-26`, `PAR-29`,
      `PAR-30`, `PAR-31`, **opening each candidate before citing it**. Where nothing asserts the
      behaviour, write `gap: <untested behaviour>` and copy the line into `research` under a
      `### Gap list for DSK-01-12` heading for [[FND-025]] (body step 11).
- [ ] **U-9 — The matrix edits are made and confined to the owned rows.** Rows to
      `inventoried`; `PAR-31` stays `legacy path retained` with its one-sentence reason
      (anonymous external audience served by the gateway host, the recorded Deviation from
      proposal § 23's ladder); SHA stamped on every touched row; every upstream redesign cell
      written as `upstream:INTK-019`, `upstream:DOCS-011`, `upstream:DOCS-012`,
      `upstream:CASE-022` — **never bare**, because a bare `<PREFIX>-<nnn>` on this board is a
      fork board id (`HZN-001` / `board-conventions.md`); no `~` name promoted; no UAT owner
      filled.
- [ ] **U-10 — The documentation gate passes.** `pwsh ./scripts/Test-DocumentationLinks.ps1` —
      exit 0, output attached as evidence.
- [ ] **U-11 — Ownership of `PAR-27` is settled with [[FND-018]] before either ticket edits the
      row.** Research F-2 records the double claim: this ticket's *What* uses the range
      `PAR-19`–`PAR-31`, which sweeps `PAR-27` in, but none of this ticket's twelve steps
      mentions it, its step-11 `to locate` list omits it, `PAR-27` is capability group **13.10**
      (not one of this ticket's 13.4/13.7/13.8), and [[FND-018]] (plan handle `DSK-01-05`) names
      it in its *What*, gives it a dedicated step 6 and lists it in its acceptance criteria. The
      determinate reading is that **[[FND-018]] owns `PAR-27`**. Confirm and record the
      agreement; do not fill it here on the strength of the range alone, and do not leave it
      unfilled by both.
- [ ] **U-12 — The stale one-file envelope in `docs/engineering.md:85` is routed somewhere.**
      Research F-7 measures the code: `MaximumContentLength = 10 MiB` per file
      (`src/Pegasus.Core/Intake/IntakeContracts.cs:13`), `MaximumBatchFileCount = 20` (`:42`),
      `MaximumBatchContentLength = 20 × 10 MiB + 64 KiB` (`:49-50`),
      `MultipartOverhead = 64 KiB` (`:56`), wired at `src/Pegasus.Web/Program.cs:525-530`, with
      `Upload.cshtml.cs:38` binding `IFormFile[]`. `docs/engineering.md:85` still says "the
      one-file 10 MiB limit and 10 MiB-plus-64-KiB multipart envelope", and the matrix's
      `PAR-28` cell says "one file". Record the measured values on `PAR-28`
      (`docs/index.md` § Authority: code plus passing tests beat any document about current
      state), then decide how the *document* is corrected: a one-line
      `docs/open-decisions.md` entry, a note to [[FND-052]], or a separate `fix` ticket. This
      ticket's Guardrails allow it to edit only `parity-matrix.md` and `docs/open-decisions.md`,
      so it must not rewrite `docs/engineering.md` itself. Do not close this spike leaving a
      working-rules document silently contradicting the code.

## Parked (explicitly deferred)

- **Whether the matrix gains an explicit cloud-placement column.** [[FND-015]] F-13 measures the
  matrix at ten columns with no placement column, while body step 7 of [[FND-015]] and
  `docs/desktop/01-inventory-and-parity/README.md:37-38` speak of "the placement column".
  **Default taken, recorded in `research` § Execution placement:** write the proposal § 4.1 value
  as a leading `Placement: <value> (proposal §4.1)` clause inside the existing "Native
  screen/use case" cell — the same default [[FND-015]] and [[FND-016]] took, so the four
  row-population tickets stay consistent. Deferred because a schema change would touch all 46
  rows under four sets of hands; raise it at review, and if a column is wanted it is board
  hygiene for [[FND-052]].
- **Whether `/api/v1` should follow the page's 12 Triage commands or the MCP surface's 10.**
  Research F-4a records the asymmetry (`assign` and `unassign` have no MCP tool). This ticket
  records it; deciding the endpoint set is area 03's endpoint map, and the `~` names in the
  matrix stay indicative until it is approved (`parity-matrix.md` § Notes).
- **Whether `PAR-24`'s "13" was a miscount or a removed command.** Owned by [[FND-014]] (plan
  handle `DSK-01-01`), whose open question U-8 runs
  `git log -p 191ddf33..HEAD -- src/Pegasus.Web/Pages/Triage/Details.cshtml.cs`. Read its answer
  rather than duplicating the check.
- **Assigning a UAT owner.** The operator assigns one per capability group before any row passes
  `automated verification passed`; guessing is a defect.

## Explicitly not an open question

- **Send to AI (upstream `TICK-102` / capability `AI-09`).** It is a **recorded exclusion with a
  reactivation condition**, not an unresolved conflict: `src/Pegasus.Web/AiWork/SendToAi.cs:12`
  defines `Features:SendToAi`, `:35-42` refuse to compose it outside the `DevelopmentOffline`
  runtime profile, and `src/Pegasus.Web/Program.cs:104-110` permits that profile only in
  Development — so it has never been reachable by an operator in production and there is nothing
  to reach parity with. `docs/desktop/05-implementation-and-migration/reuse-map.md:38` marking
  `AiWork/` "gated, out of parity scope" is correct. The reactivation condition is the separate
  non-preview transport decision named at `docs/capabilities.md:269`. Recording it belongs to
  [[FND-022]] (plan handle `DSK-01-09`) step 10. **No `open-questions` item is created for it on
  any ticket.**
