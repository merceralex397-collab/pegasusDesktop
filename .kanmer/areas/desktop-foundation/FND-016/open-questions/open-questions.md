# Open questions — FND-016 (DSK-01-03)

The ticket body step 12 instructs: "tick every `open-questions/` item, then `get_doc_gates` and
move to Done." These are those items.

This is a `spike`. Its `research` document **is** the deliverable and writing it satisfies the
`enter-done` gate on its own — so the unticked boxes below are what actually keep an unfinished
spike from being closed. For a `spike` an unticked box blocks **`enter-done` and nothing else**
(`get_doc_gates FND-016` lists `enter-done` as this profile's only gated boundary); it does not
gate `leave-backlog`, and the ticket can move through Preparing, Implementing, Review and
Verifying with these open.

Each item is tickable when its output is recorded in `research`, replacing the matching
`NOT YET CAPTURED` block.

## Must be answered before this spike is Done

- [ ] **U-1 — The row-by-row citation table is written into `research`.** One table for
      `PAR-07`…`PAR-12`, `PAR-40`, `PAR-41`: entry point(s) → handlers (`path:line`) → command
      set expanded → Core owner (`path:line`) → FRD owner → test file or `gap:` → §4.1 placement
      → inventoried-at SHA (body step 11).
- [ ] **U-2 — Every handler maps to exactly one row, using the corrected glob.** Run
      `git ls-files 'src/Pegasus.Web/Pages/Cases/*.cshtml.cs' | wc -l` — expect **12**. **Do not
      run the `**/` spelling**: research F-2 measures it at **4**, and this ticket's own
      Verification item expects 12 from it. Then
      `git grep -n "public .*On\(Get\|Post\)" -- 'src/Pegasus.Web/Pages/Cases' 'src/Pegasus.Web/Pages/Administration/Organizations' 'src/Pegasus.Web/Pages/Administration/Principals'`
      and show all 47 + 9 handlers landing in exactly one `PAR` row, with the 19 owned by
      [[FND-017]] and [[FND-018]] cross-referenced by row id and left unfilled.
- [ ] **U-3 — `PAR-10` and `PAR-12` commands are listed individually and mapped to
      `CaseLifecycle`.** Seven workflow commands (hold, release hold, return to review, assign
      engineer, start work, record engineer finding, create linked replacement) and four closure
      commands (record report approval, close, reopen, archive), each citing its transition in
      `src/Pegasus.Core/Lifecycle/CaseLifecycle.cs` or `CaseCommandSeams.cs` by line. Handler
      names alone are a failed cell (area plan § 7 trap 1).
- [ ] **U-4 — `PAR-11` commands are mapped to `src/Pegasus.Core/Tasks/`.** Eight commands
      (note, create task, assign, complete, cancel, manual chase, link report evidence, unlink)
      against the five files research F-7a lists. State explicitly which are operator commands
      and that `RunDueChasers.cs` is unattended Worker work, not a desktop command.
- [ ] **U-5 — Assumption A-01-03-2 is settled for `PAR-09`.** Read
      `src/Pegasus.Web/Pages/Cases/Create.cshtml.cs:266` and the request type it builds; compare
      with `src/Pegasus.Core/Workflow/CaseWorkflowContracts.cs:182`. Does case creation use the
      six-field `CaseMutationRequest`, or a separate allocation request with an idempotency key
      instead of `ExpectedVersion`? Record the answer on `PAR-09` — area 03 shapes
      `POST /api/v1/cases` from it.
- [ ] **U-6 — Assumption A-01-03-3 is settled for the operation-key cap.** Open
      `src/Pegasus.Core/Cases/OrganizationAdministration.cs:274`,
      `src/Pegasus.Core/Identity/StaffAccountAdministration.cs:410`,
      `src/Pegasus.Core/Intake/DurableIntake.cs:256` and two of the bare-literal sites from
      research F-7 (`Lifecycle/CaseCommandSeams.cs:208`, `Lifecycle/CaseLifecycle.cs:233`). Is
      100 a uniform hard cap, and is there any single Core constant a client can reference?
      Record that there is none if that is the answer — it is a note for [[FND-029]] (plan
      handle `DSK-02-04`), not a change this ticket makes.
- [ ] **U-7 — Test evidence resolved for `PAR-10`, `PAR-11`, `PAR-12`, `PAR-40`, `PAR-41`.**
      Use research F-8's candidate list and F-9's **narrowed** principal grep
      (`git grep -rln "ReplacePrincipal\|PrincipalReplace\|Principals/Replace\|CreatePrincipal" tests/`,
      4 files) — **not** the body's `"Organization\|Principal"` grep, which research F-9
      measures at **68 files** because "Principal" also matches `ClaimsPrincipal`. Open each file
      before citing it. Where nothing asserts the behaviour, write `gap: <untested behaviour>`.
      Settles A-01-03-4.
- [ ] **U-8 — Every `gap:` line is copied into `research` for [[FND-025]].** Under a
      `### Gap list for DSK-01-12` heading (body step 9), consumable without re-derivation.
- [ ] **U-9 — The matrix edits are made and confined to the owned rows.** Eight rows to
      `inventoried`; the six-field envelope recorded **verbatim** on `PAR-08` from
      `CaseWorkflowContracts.cs:182-189` (research F-4 warns that `:178-181` ends a *different*
      record whose field is `LeaseToken`, not `EditLeaseToken` — anchor on `:182`); the
      inventoried-at SHA stamped on every touched row; no `~` endpoint name promoted, no UAT
      owner filled, no sibling row touched.
- [ ] **U-10 — The documentation gate passes.** `pwsh ./scripts/Test-DocumentationLinks.ps1` —
      exit 0, output attached as evidence.
- [ ] **U-11 — A-01-03-1 is confirmed against [[FND-014]]'s difference lists.** Read
      [[FND-014]]'s (plan handle `DSK-01-01`) research with `get_ticket_doc`. If its difference
      list (a) names a §13.3 or §13.6 page model with no `PAR` row, add that row here using the
      matrix's exact ten columns. If the list is empty, say so.

## Parked (explicitly deferred)

- **Whether the matrix gains an explicit cloud-placement column.** Research F-13 records that
  the matrix has ten columns and no placement column, while body step 10 and
  `docs/desktop/01-inventory-and-parity/README.md:37-38` speak of "the placement column".
  **Default taken, and recorded in `research` F-13:** write the proposal § 4.1 value as a
  leading `Placement: <value> (proposal §4.1)` clause inside the existing "Native screen/use
  case" cell — the same default [[FND-015]] took, so the four row-population tickets stay
  consistent. Deferred because a schema change would touch all 46 rows under four sets of hands;
  raise it at review, and if a column is wanted it is board hygiene for [[FND-052]]. The § 4.1
  values themselves need no decision: `Case workflow commands` → Split; `Central case data` →
  Cloud required; `Case/query presentation` → Split
  (`docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md:140-162`).
- **Splitting `PAR-08` or `PAR-09` into more rows.** They are the two largest page models (654
  and 689 lines). The body's Guardrails are explicit: "If the evidence does not fit one row, note
  the concern in the research document — do not split the plan row." Deferred on that
  instruction; if the cells genuinely overflow, record the concern and leave the split to a
  later grooming decision.
- **Introducing a single shared operation-key constant.** Research F-7 measures the 100-character
  cap as a repeated literal across eight-plus sites with three separate named constants. Changing
  that is a code change; this ticket is read-only over `src/` (Guardrails). Recorded as a note for
  [[FND-029]] (plan handle `DSK-02-04`, `src/Pegasus.Contracts`), which is where a shared envelope
  constant would belong.
- **Assigning a UAT owner.** The operator assigns one per capability group before any row passes
  `automated verification passed`; guessing is a defect, so nothing is asked.
