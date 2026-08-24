# Open questions — FND-014 (DSK-01-01)

The ticket body step 12 instructs: "Record anything you could not settle as an item under the
ticket's `open-questions/`; every item must be ticked before `enter-done`."

This is a `spike`. Its `research` document **is** the deliverable and writing it satisfies the
`enter-done` gate on its own — so the unticked boxes below are what actually keep an unfinished
spike from being closed. For a `spike` an unticked box blocks **`enter-done` and nothing else**
(`get_doc_gates FND-014` lists `enter-done` as this profile's only gated boundary); it does not
gate `leave-backlog`, and the ticket can move through Preparing, Implementing, Review and
Verifying with these open.

Each item names the command that closes it. The item is tickable when its output is recorded in
`research`, replacing the matching `NOT YET CAPTURED` block.

## Must be answered before this spike is Done

- [ ] **U-1 — Raw handler enumeration is in the ticket scratch.** Run
      `git grep -n "public .*On\(Get\|Post\)[A-Za-z]*" -- 'src/Pegasus.Web/Pages'` (expect 136
      lines) and `append_scratch` the raw output onto FND-014, so the reviewer can re-run it.
- [ ] **U-2 — Difference list (a) is written: page models with no `PAR` row.** Join the 53
      paths from `git ls-files 'src/Pegasus.Web/**/*.cshtml.cs'` against the "Current entry
      point" column of every `^| PAR-` row. Empty list, or one line of explanation per entry.
- [ ] **U-3 — Difference list (b) is written: `PAR` rows whose page model no longer exists.**
      `test -f` each cited entry point for all 46 rows (`PAR-45`/`PAR-46` exempt — verified in
      research F-12). Empty list, or one line of explanation per entry.
- [ ] **U-4 — Difference list (c) is written: handlers in code missing from a row's handler
      list, and handlers listed on a row that no longer exist.** Per-file diff of the U-1
      output against each row's handler cell. This is the list that decides whether acceptance
      criterion 2 ("every handler appears exactly once across the matrix rows") can be claimed.
- [ ] **U-5 — Every multi-command handler is expanded into its command set.**
      `Triage/Details.OnPostActionAsync` is already done (research F-10, 12 commands). Still
      owed for the twelve other multi-handler page models listed in the U-5 block, so the
      matrix records command sets and never a bare handler name (area plan § 7 trap 1).
- [ ] **U-6 — The reconciled skeleton table is written into `research`.** One table of
      `PAR id → page model path → handlers (commands expanded) → base class → inventoried-at
      SHA`, covering all 46 rows, usable by [[FND-015]]…[[FND-018]] without re-running any
      enumeration (body step 11).
- [ ] **U-7 — A-01-1 is confirmed: no HTTP surface exists beyond the Razor pages and the four
      known registrations.** Run
      `git grep -n "MapGet(\|MapPost(\|MapPut(\|MapDelete(\|AddControllers" src/Pegasus.Web`.
      Any hit that is not `/health/live`, `/health/ready`, `/diagnostics/version`, the token
      endpoint or `MapMcp` is a capability with no inventory row, and the Phase 0 exit gate
      item 1 cannot be claimed until it has one.
- [ ] **U-8 — `PAR-24`'s "13 commands" is settled as a miscount or as a removed command.** Run
      `git log -p 191ddf33..HEAD -- src/Pegasus.Web/Pages/Triage/Details.cshtml.cs`. A removed
      command is a behaviour regression to escalate, not a documentation correction — say which
      it is, with the commit if there is one. (Research F-10 measures the current count as 12
      named commands plus a throwing `default:`.)
- [ ] **U-9 — Both documentation gates pass after the edits.**
      `pwsh ./scripts/Test-DocumentationLinks.ps1` and `pwsh ./scripts/Test-MarkdownPlacement.ps1`
      — both exit 0, output attached as the ticket evidence.
- [ ] **U-10 — The `README.md:50` base-class count correction is decided.** Research F-9b
      measures **8** `CaseMutationPageModel` derivers where
      `docs/desktop/01-inventory-and-parity/README.md:50` says 7. Body step 10 authorises only
      the `README.md:48` `StaffPageModel` **path** correction, so either extend the same PR to
      the count (recording the decision here) or hand the count to [[FND-052]] as a board-hygiene
      item. Do not silently leave the plan wrong.
- [ ] **U-11 — The pathspec correction is handed to [[FND-016]].** Research F-5 measures
      `git ls-files 'src/Pegasus.Web/Pages/Cases/**/*.cshtml.cs' | wc -l` → **4**, where
      [[FND-016]] (plan handle `DSK-01-03`) Verification expects **12**; the spelling that
      answers the question is `'src/Pegasus.Web/Pages/Cases/*.cshtml.cs'`. Record that the
      correction was passed on (a note in [[FND-016]]'s scratch, or in this ticket's proof
      naming it) so the next agent does not run a silently under-counting command.

## Parked (explicitly deferred)

- **Whether `Connect/Authorize.cshtml.cs` should inherit `AdministrationPageModel`** (research
  F-9a). It is an external-audience consent page that inherits the administration base. Deferred
  because this ticket is read-only over `src/` (Guardrails) and `PAR-42` is
  `legacy path retained` — the inheritance changes no observable behaviour for the parity
  inventory. Recorded so [[FND-015]] (which owns `PAR-42`) states it rather than rediscovering
  it; if it ever becomes a defect it is a `fix` ticket in `platform-operations`, not this spike.
- **Assigning UAT owners.** The matrix leaves the column blank by design; the operator assigns
  one owner per capability group before any row moves past `automated verification passed`
  (`parity-matrix.md` § Notes). Guessing an owner is a defect, so nothing is asked here.
- **Whether the matrix moves to `docs/features/`.** Owned and decided by [[FND-012]] (plan
  handle `DSK-00-12`); not an open question on this ticket.
