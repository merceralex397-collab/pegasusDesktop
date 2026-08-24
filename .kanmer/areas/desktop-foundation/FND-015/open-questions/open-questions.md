# Open questions — FND-015 (DSK-01-02)

The ticket body step 12 instructs: "tick every `open-questions/` item, then `get_doc_gates` and
move to Done." These are those items.

This is a `spike`. Its `research` document **is** the deliverable and writing it satisfies the
`enter-done` gate on its own — so the unticked boxes below are what actually keep an unfinished
spike from being closed. For a `spike` an unticked box blocks **`enter-done` and nothing else**
(`get_doc_gates FND-015` lists `enter-done` as this profile's only gated boundary); it does not
gate `leave-backlog`, and the ticket can move through Preparing, Implementing, Review and
Verifying with these open.

Each item is tickable when its output is recorded in `research`, replacing the matching
`NOT YET CAPTURED` block.

## Must be answered before this spike is Done

- [ ] **U-1 — The row-by-row citation table is written into `research`.** One table for
      `PAR-01`…`PAR-06`, `PAR-42`, `PAR-44`: entry point → handlers (`path:line`) → behaviour
      evidence (`path:line` only, never a paraphrase) → FRD owner → capability group → test file
      or `gap:` → §4.1 placement → inventoried-at SHA (body step 11).
- [ ] **U-2 — Every handler maps to exactly one row.** Run
      `git grep -n "public .*On\(Get\|Post\)" -- 'src/Pegasus.Web/Pages/Account' 'src/Pegasus.Web/Pages/Index.cshtml.cs' 'src/Pegasus.Web/Pages/Search' 'src/Pegasus.Web/Pages/Connect'`
      (research F-2 measures 11 handlers across 6 files) and show each landing in exactly one of
      this ticket's rows. State explicitly that `Account/AccessDenied.cshtml.cs` declares none.
- [ ] **U-3 — Test evidence is resolved for `PAR-01`, `PAR-02`, `PAR-03` and `PAR-44`.** Open
      every candidate research F-10 found — `StaffSignInSecurityTests.cs`,
      `ShellAndStatusPageWebTests.cs`, `AdministrationSearchAccountWebTests.cs`,
      `Browser/AccessibilityTests.cs`, and the four `StaffAccessRight` files — and decide per row
      whether a test asserts the behaviour the cell claims. Where none does, write
      `gap: <what is untested>`. **Do not write a test name that does not assert the behaviour.**
      This settles assumptions A-01-02-2 and A-01-02-3.
- [ ] **U-4 — Every `gap:` line is copied into `research` for [[FND-025]].** Under a
      `### Gap list for DSK-01-12` heading, in a form [[FND-025]] (plan handle `DSK-01-12`) can
      consume without re-deriving it (body step 5).
- [ ] **U-5 — The matrix edits are made and are confined to the owned rows.** `PAR-01`…`PAR-06`
      and `PAR-44` to `inventoried`; `PAR-42` stays `legacy path retained` with its one-sentence
      ADR-0027 reason; the inventoried-at SHA stamped on every touched row; **no** `~`-prefixed
      endpoint name promoted, **no** UAT owner filled, **no** sibling's row touched.
- [ ] **U-6 — The four corrections this research already identified are applied.**
      (a) `PAR-01` gains the per-client limit of 10/min
      (`StaffSessionPolicy.SignInAttemptsPerClientPerMinute`, `src/Pegasus.Core/Actors/StaffSessionPolicy.cs:12`,
      registered `src/Pegasus.Web/Program.cs:298-304`) alongside the global 100/min (`:324`,
      middleware `:797-817`).
      (b) The `lockoutOnFailure: false` citation moves from `:63` to `:64` (statement `:62-64`),
      paired with `Program.cs:270` `Lockout.AllowedForNewUsers = false`.
      (c) `PAR-44`'s `tests/Pegasus.ArchitectureTests`? guess is replaced — research F-10 shows
      no architecture test mentions `StaffAccessRight`.
      (d) `PAR-05` names `Presentation/RailCountsPageFilter.cs` **and** its global registration
      at `Program.cs:260-261`, so rail counts read as ambient rather than dashboard-scoped.
- [ ] **U-7 — Both documentation gates pass.** `pwsh ./scripts/Test-DocumentationLinks.ps1` and
      `pwsh ./scripts/Test-MarkdownPlacement.ps1` — both exit 0, output attached as evidence.
- [ ] **U-8 — Reviewer spot-check recorded.** Three cited `path:line` references from the changed
      rows opened; each line says what its cell claims (the ticket's fourth Verification item).
- [ ] **U-9 — A-01-02-1 is confirmed against [[FND-014]]'s difference lists.** Read
      [[FND-014]]'s (plan handle `DSK-01-01`) research with `get_ticket_doc`. If its difference
      list (a) names a §13.1 or §13.2 page model with no `PAR` row, add that row here using the
      matrix's exact ten columns (body step 2). If the list is empty, say so.

## Parked (explicitly deferred)

- **Should the matrix gain an eleventh, explicit cloud-placement column?** Research F-13
  measures the matrix header at ten columns with no placement column, while body step 7 and
  `docs/desktop/01-inventory-and-parity/README.md:37-38` both speak of "the placement column".
  **Default taken, and recorded in `research` F-13:** write the proposal § 4.1 value as a
  leading `Placement: <value> (proposal §4.1)` clause inside the existing "Native screen/use
  case" cell. Deferred because adding a column touches all 46 rows and would land in the middle
  of four concurrent row-population tickets ([[FND-015]], [[FND-016]], [[FND-017]],
  [[FND-018]]); the cost of deferring is one later edit pass, the cost of acting now is a schema
  change under four sets of hands. Raise it at review; if a column is wanted, it is board
  hygiene for [[FND-052]] or an explicit decision by the last row ticket to run, not a blocker
  here. The § 4.1 values themselves are settled and need no decision: `Native UI, navigation and
  state` → Desktop; `User login screen` → Split; `Case/query presentation` → Split
  (`docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md:140-162`).
- **Should `Connect/Authorize.cshtml.cs` inherit `AdministrationPageModel`?** Research F-9
  records that it does (`:24`), on an external-audience consent page. Deferred: this ticket is
  read-only over `src/` (Guardrails), the row is `legacy path retained`, and the inheritance
  changes no observable behaviour for the inventory. Stated on the row; if it ever matters it is
  a `fix` in `platform-operations`, not this spike.
- **Assigning a UAT owner.** The column stays blank by design; the operator assigns one owner
  per capability group before any row passes `automated verification passed`
  (`parity-matrix.md` § Notes, body step 10). Guessing one is a defect, so nothing is asked.
