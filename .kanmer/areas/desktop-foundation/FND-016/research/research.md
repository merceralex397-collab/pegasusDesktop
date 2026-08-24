# Research — FND-016: Parity rows for §13.3 case lifecycle and §13.6 parties and reference data

> **STATUS — INCOMPLETE. Do not move this ticket to Done yet.**

This is a `spike`. Its `research` document is the spike's **output**, not an input to it, and
its existence alone satisfies the `enter-done` gate (`get_doc_gates FND-016`: `enter-done`
needs `research` + `questions-resolved`, and it is this profile's only gated boundary). This
file was written **before the spike ran**, as the pre-work scaffold the authoring contract
requires. Everything under **Facts** is verified against the repository with the command that
produced it. Everything marked `NOT YET CAPTURED` is still owed and has an unticked box in this
ticket's `open-questions` document — those boxes, not this banner, block `enter-done`.

Baseline: `git rev-parse HEAD` → `bbd1c54959e8c3a361d3f73965b61d6e4aff59ec`, read 2026-08-24.

**Dependency, not an open question:** written against the confirmed skeleton of [[FND-014]]
(plan handle `DSK-01-01`); the board records that edge. Read [[FND-014]]'s `research` with
`get_ticket_doc` before writing a cell.

## Question

For `PAR-07`…`PAR-12` (case lifecycle) and `PAR-40`/`PAR-41` (organizations, principals): what
are the exact handlers, the command sets behind them, the Core policy owners with `path:line`,
the concurrency envelope the desktop must reproduce, and which test files really cover them —
so eight rows can move to `inventoried` without a single invented citation?

## Current behaviour

The rows this ticket owns, with the status each carries today:

| Row | §13.x | Entry point | Handlers | Status today |
| --- | --- | --- | --- | --- |
| `PAR-07` | 13.3 | `Cases/Index.cshtml.cs` (261) | 1 | `inventoried` |
| `PAR-08` | 13.3 | `Cases/Details.cshtml.cs` (654) | 6 | `inventoried` |
| `PAR-09` | 13.3 | `Cases/Create.cshtml.cs` (689) | 2 | `inventoried` |
| `PAR-10` | 13.3 | `Cases/Workflow.cshtml.cs` (227) | 7 | `inventoried` |
| `PAR-11` | 13.3 | `Cases/Tasks.cshtml.cs` (248) | 8 | `inventoried` |
| `PAR-12` | 13.3 | `Cases/Closure.cshtml.cs` (121) | 4 | `inventoried` |
| `PAR-40` | 13.6 | `Administration/Organizations/Index.cshtml.cs` (126) + `Edit.cshtml.cs` (146) | 2 + 2 | `not inventoried` |
| `PAR-41` | 13.6 | `Administration/Principals/Index.cshtml.cs` (31), `Create.cshtml.cs` (137), `Replace.cshtml.cs` (199) | 1 + 2 + 2 | `not inventoried` |

Today the case workspace is a set of Razor pages sharing one PRG + TempData wrapper,
`src/Pegasus.Web/Pages/Cases/CaseMutationPageModel.cs` (339 lines), and one Core mutation
envelope. Every mutating command carries `CaseId`, `ExpectedVersion`, `Actor`, `OperationKey`,
`Reason` and `EditLeaseToken`; the actor is server-derived, never sent by the client. Lease
claim/renew/release are separate commands, and a save that does not present the live lease is
refused. Lifecycle transitions live in `src/Pegasus.Core/Lifecycle/CaseLifecycle.cs` (629
lines) behind named use cases in `src/Pegasus.Core/Lifecycle/CaseCommandSeams.cs`. Organizations
and principals are administration pages over `src/Pegasus.Core/Cases/OrganizationAdministration.cs`.

## Findings

- **`git ls-files 'src/Pegasus.Web/Pages/Cases/**/*.cshtml.cs' | wc -l` returns `4`, not the
  `12` this ticket's Verification expects** — F-2. The command, not the expectation, is wrong.
- The 47 `Cases/**` handlers split cleanly across `PAR-07`…`PAR-18`, of which this ticket owns
  28 — F-3. No handler is orphaned and none is double-claimed.
- The three lease ports are declared in `Workflow/CaseCommandContracts.cs`, **not** in
  `Lifecycle/` where the body's grep points — F-5.
- The 64-hex lease token is a **column-width** constraint with a stated reason — F-6. That is
  the sentence `PAR-08` needs, not the number alone.
- **The 100-character operation-key cap is a repeated literal, not one constant** — F-7. Eight
  call sites, three named constants, no single source. A desktop client has nothing to
  reference.
- The body's step-9 organization/principal grep matches **68 files** and is useless as a
  locator; a narrowed grep finds the four that matter — F-9.
- Several `to locate` cells are already resolvable from a plain search — F-8, F-9.

### Facts

Verified at `bbd1c549` on 2026-08-24, each with its command.

- **F-1 — Row set and current statuses**: the table under *Current behaviour*. Six of the eight
  rows already read `inventoried`; treat that as "already drafted", never "already verified"
  ([[FND-015]] found two wrong cells among its four such rows).
- **F-2 — The Verification glob under-counts by two thirds.** `git ls-files` applies its
  pathspec without `:(glob)` magic, so `*` matches `/` and a literal `**/` demands at least one
  directory level:

  | Command | Returns |
  | --- | --- |
  | `git ls-files 'src/Pegasus.Web/Pages/Cases/**/*.cshtml.cs' \| wc -l` | **4** — only `Assessment/Index`, `Documents/Download`, `Documents/Export`, `Eva/Download` |
  | `git ls-files 'src/Pegasus.Web/Pages/Cases/*.cshtml.cs' \| wc -l` | **12** ✓ — the eight directly in `Cases/` plus those four |

  This ticket's first Verification item expects `12` from the first spelling. Use the second.
  [[FND-018]]'s body already documents the identical trap for `Administration/`;
  [[FND-014]] F-5 records it for the whole board.
- **F-3 — 47 handlers across `Cases/**`, split across twelve rows, 28 of them owned here.**
  `git grep -n "public .*On\(Get\|Post\)" -- 'src/Pegasus.Web/Pages/Cases'` → 47 lines:

  | Row | Page model | Handlers (line) | Owner |
  | --- | --- | --- | --- |
  | `PAR-07` | `Cases/Index.cshtml.cs` | `OnGetAsync` (`:71`) | **this ticket** |
  | `PAR-08` | `Cases/Details.cshtml.cs` | `OnGetAsync` (`:110`), `OnPostClaimLeaseAsync` (`:156`), `OnPostRenewLeaseAsync` (`:203`), `OnPostReleaseLeaseAsync` (`:250`), `OnPostConfirmCompletenessAsync` (`:293`), `OnPostSaveAsync` (`:324`) | **this ticket** |
  | `PAR-09` | `Cases/Create.cshtml.cs` | `OnGetAsync` (`:210`), `OnPostCreateAsync` (`:266`) | **this ticket** |
  | `PAR-10` | `Cases/Workflow.cshtml.cs` | `OnPostHoldAsync` (`:26`), `OnPostReleaseHoldAsync` (`:42`), `OnPostReturnToReviewAsync` (`:64`), `OnPostAssignEngineerAsync` (`:98`), `OnPostStartWorkAsync` (`:133`), `OnPostRecordEngineerFindingAsync` (`:156`), `OnPostCreateLinkedReplacementAsync` (`:180`) | **this ticket** |
  | `PAR-11` | `Cases/Tasks.cshtml.cs` | `OnPostAddNoteAsync` (`:33`), `OnPostCreateTaskAsync` (`:61`), `OnPostAssignTaskAsync` (`:89`), `OnPostCompleteTaskAsync` (`:117`), `OnPostCancelTaskAsync` (`:143`), `OnPostRecordManualChaseAsync` (`:169`), `OnPostLinkReportEvidenceAsync` (`:201`), `OnPostUnlinkReportEvidenceAsync` (`:225`) | **this ticket** |
  | `PAR-12` | `Cases/Closure.cshtml.cs` | `OnPostRecordReportApprovalAsync` (`:23`), `OnPostCloseAsync` (`:52`), `OnPostReopenAsync` (`:69`), `OnPostArchiveAsync` (`:106`) | **this ticket** |
  | `PAR-13` | `Cases/Custody.cshtml.cs` | 6 (`:28`, `:74`, `:138`, `:162`, `:186`, `:237`) | [[FND-017]] |
  | `PAR-14` | `Cases/Vehicle.cshtml.cs` | 3 (`:24`, `:46`, `:87`) | [[FND-018]] |
  | `PAR-15` | `Cases/Assessment/Index.cshtml.cs` | 7 (`:184`, `:246`, `:277`, `:330`, `:476`, `:583`, `:628`) | [[FND-018]] |
  | `PAR-16` | `Cases/Documents/Download.cshtml.cs` | `OnGetAsync` (`:16`) | [[FND-017]] |
  | `PAR-17` | `Cases/Documents/Export.cshtml.cs` | `OnPostAsync` (`:18`) | [[FND-017]] |
  | `PAR-18` | `Cases/Eva/Download.cshtml.cs` | `OnPostAsync` (`:21`) | [[FND-018]] |

  1+6+2+7+8+4+6+3+7+1+1+1 = **47**. This ticket owns **28**; the acceptance criterion "every
  handler of the twelve `Cases/**` page models is accounted for in exactly one matrix row
  (including the rows owned by sibling tickets, cross-referenced by id)" is satisfiable from
  this table — record the split, do not fill a sibling's cells.
- **F-3a — Organizations and principals: 9 handlers across 5 page models.**
  `git grep -n "public .*On\(Get\|Post\)" -- 'src/Pegasus.Web/Pages/Administration/Organizations' 'src/Pegasus.Web/Pages/Administration/Principals'` →
  `Organizations/Index` `OnGetAsync` (`:34`), `OnPostCreateAsync` (`:46`);
  `Organizations/Edit` `OnGetAsync` (`:33`), `OnPostUpdateAsync` (`:45`);
  `Principals/Index` `OnGetAsync` (`:18`);
  `Principals/Create` `OnGetAsync` (`:32`), `OnPostCreateAsync` (`:43`);
  `Principals/Replace` `OnGetAsync` (`:38`), `OnPostReplaceAsync` (`:58`).
- **F-4 — The six-field mutation envelope, verbatim.**
  `src/Pegasus.Core/Workflow/CaseWorkflowContracts.cs:182-189`:

  ```csharp
  public abstract record CaseMutationRequest(
      Guid CaseId,
      long ExpectedVersion,
      ActionActor Actor,
      string OperationKey,
      string Reason,
      string EditLeaseToken);
  ```

  This is the contract area 03 must reproduce on `PUT /api/v1/cases/{id}` and it belongs
  verbatim in `PAR-08`'s behaviour cell (body step 3; acceptance criterion 3). **Trap:** the
  same file at `:178-181` ends a *different* record with `ActionActor Actor, string OperationKey,
  string LeaseToken` — note `LeaseToken`, not `EditLeaseToken`. Copying the wrong four lines is
  an easy mistake; anchor on `:182`.
- **F-5 — The lease ports are in `Workflow/`, not `Lifecycle/`.** Body step 4 says
  `git grep -n "IAcquireCaseEditLease\|CaseEditAuthority" src/Pegasus.Core`. Run, it gives:
  interfaces `IAcquireCaseEditLease` `src/Pegasus.Core/Workflow/CaseCommandContracts.cs:77`,
  `IRenewCaseEditLease` `:84`, `IReleaseCaseEditLease` `:91`; implementations
  `AcquireCaseEditLease` `src/Pegasus.Core/Lifecycle/CaseCommandSeams.cs:6`,
  `RenewCaseEditLease` `:20`, `ReleaseCaseEditLease` `:34`, each over the port
  `ILeaseCaseForEdit`. Cite the interface for the contract and the seam for the behaviour.
- **F-6 — The 64-hex lease token, with the reason.**
  `src/Pegasus.Core/Workflow/CaseEditAuthority.cs:12` `public static class CaseEditAuthority`;
  `:18` `public const int LeaseTokenLength = 64;`, documented at `:14-17`: *"Edit lease tokens are
  issued as 64 hexadecimal characters and retained in a column of that exact width, so a longer
  presented value can never round-trip and is refused as invalid."* Also in that file:
  `IsHeld(leaseExpiresAtUtc, nowUtc)` at `:25` — *"An abandoned lease expires without a sweeper,
  so every projection and guard asks this one question"* — and `RequireVersion` at `:27`, which
  throws `CaseVersionConflictException`. Those three are the whole concurrency story for a
  desktop client: fixed-width token, expiry-by-time with no sweeper, version compare on write.
  Enforcement is echoed in `src/Pegasus.Core/Intake/DurableIntake.cs:1213-1217`, which refuses a
  presented token longer than `CaseEditAuthority.LeaseTokenLength`.
- **F-7 — The 100-character operation-key cap is a repeated literal, and that is a finding.**
  `git grep -rn "OperationKey.*100\|MaximumOperationKeyLength" src/Pegasus.Core` returns **eight
  or more call sites** passing a bare `100`:
  `Cases/CreateLinkedReplacement.cs:34`, `Custody/CustodyContracts.cs:435`,
  `Intake/ApprovedOutlookCategories.cs:59`, `Lifecycle/CaseCommandSeams.cs:208`, `:217`, `:234`,
  `Lifecycle/CaseLifecycle.cs:233` — against only three named constants:
  `Cases/OrganizationAdministration.cs:274 MaximumOperationKeyLength = 100`,
  `Identity/StaffAccountAdministration.cs:410 MaximumOperationKeyLength = 100`,
  `Intake/DurableIntake.cs:256 private const int MaximumOperationKeyLength = 100`.
  **There is no single Core constant a desktop client can reference**, and the plan's phrase
  "operation key ≤ 100 characters" understates this: the cap is enforced independently in at
  least eight places. `PAR-08`, `PAR-10`, `PAR-11`, `PAR-12`, `PAR-40` and `PAR-41` all inherit
  it. Record the number and the fact that it is not centralised.
- **F-7a — `src/Pegasus.Core/Tasks/` contents.** `ls src/Pegasus.Core/Tasks/` → 5 files:
  `CaseTaskContracts.cs`, `CaseTaskUseCases.cs`, `CaseWorkScheduling.cs`,
  `RecordManualCaseChase.cs`, `RunDueChasers.cs`. `PAR-11` (body step 6) cites these; note that
  `RunDueChasers.cs` is *unattended* work — it is a Worker concern, not a desktop command, and
  the row should say so rather than implying the desktop schedules chases.
- **F-8 — Lifecycle and closure test evidence exists; the matrix's `to locate` is stale.**
  `git grep -rln "OnPostHold\|ReturnToReview\|AssignEngineer\|Reopen\|Archive" tests/` → 14
  files, the load-bearing ones being
  `tests/Pegasus.IntegrationTests/CaseWorkflowWebTests.cs` (handler-level, `PAR-10`),
  `tests/Pegasus.IntegrationTests/CaseClosureWebTests.cs` (`PAR-12`),
  `tests/Pegasus.Core.Tests/Lifecycle/AssignCaseEngineerTests.cs` (`PAR-10`, Core policy),
  `tests/Pegasus.IntegrationTests/CaseWorkflowPersistenceTests.cs` (already cited),
  `tests/Pegasus.IntegrationTests/CaseTaskArchivePersistenceTests.cs` (`PAR-11`).
  Other hits (`Qdos/EvaBundleContractTests.cs`, `Triage/TriageReplayTests.cs`,
  `AutomationAssessmentIngressTests.cs`, `IntakePersistenceIntegrationTests.cs`,
  `MultiFormatIntakeWebTests.cs`, `QdosCustodialWebTests.cs`, `QdosTriageReplayIntegrationTests.cs`,
  `AutomationDocumentIngressTests.cs`, `Core.Tests/Qdos/EvaHandoffPolicyTests.cs`) match on
  words like `Archive`/`Reopen` used in other contexts — **open each before citing it**.
  Additional named files that exist and are obvious candidates:
  `tests/Pegasus.IntegrationTests/CaseCreateWebTests.cs` (`PAR-09`),
  `tests/Pegasus.IntegrationTests/CaseNotePersistenceTests.cs` (`PAR-11`),
  `tests/Pegasus.IntegrationTests/CaseReportApprovalWebTests.cs` (`PAR-12`),
  `tests/Pegasus.Core.Tests/Lifecycle/CaseEditLeaseTests.cs` (`PAR-08`).
- **F-9 — The body's organization/principal grep is unusable; a narrowed one works.**
  `git grep -rln "Organization\|Principal" tests/Pegasus.Core.Tests tests/Pegasus.IntegrationTests | wc -l`
  → **68 files**, because "Principal" also matches `ClaimsPrincipal` and the intake domain's
  principal vocabulary. The narrowed search
  `git grep -rln "ReplacePrincipal\|PrincipalReplace\|Principals/Replace\|CreatePrincipal" tests/`
  returns **4**: `tests/Pegasus.Core.Tests/Cases/OrganizationAdministrationTests.cs`,
  `tests/Pegasus.IntegrationTests/OrganizationAdministrationPersistenceTests.cs`,
  `tests/Pegasus.IntegrationTests/OrganizationAdministrationWebTests.cs`,
  `tests/Pegasus.IntegrationTests/CaseWorkflowPersistenceTests.cs`. `ls tests/Pegasus.IntegrationTests/ | grep -i 'organization\|principal'`
  confirms only the two `OrganizationAdministration*` files. So `PAR-40` has real evidence, and
  `PAR-41`'s dedicated evidence is thin — likely a `gap:` for the *replace-not-edit* rule.
- **F-10 — The product invariants that constrain `PAR-12` and `PAR-41`, verbatim.**
  `AGENTS.md:235-257` § Product invariants:
  `:243` — *"Never delete a case. Reopening needs a reason and normal destination gates."*
  `:240-242` — *"Principal and reference are immutable after allocation. Wrong-principal work
  closes as `Created in error` with a reason and linked replacement; neither reference is reused
  and the original never reopens."*
  The second explains why `Cases/Workflow.OnPostCreateLinkedReplacementAsync` (`:180`, `PAR-10`)
  and `Principals/Replace` (`PAR-41`) exist at all, and why there is no principal *edit*
  handler. A desktop that offers a silent delete, or an editable principal, is a defect against
  these rows (body step 7).
- **F-11 — `CaseLifecycle.cs` is 629 lines** (`wc -l src/Pegasus.Core/Lifecycle/CaseLifecycle.cs`),
  matching the plan; `CaseCommandSeams.cs` holds the named use-case seams. ADR-0018 exists at
  `docs/adr/0018-provider-inspection-mode-database-setting.md` for `PAR-41`'s provider
  inspection mode (body step 8).
- **F-12 — FRD owners exist.** This ticket's `refs` (`get_doc_gates FND-016`) are
  `docs/frd/frd-01-case-identity-and-lifecycle.md`,
  `docs/frd/frd-04-parties-accounts-and-access.md` and
  `docs/frd/frd-09-provider-and-intermediary-routes.md`; all three are tracked.
- **F-13 — Proposal § 4.1 values for these rows** (`docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md:140-162`),
  verbatim: `Case workflow commands` → **Split**; `Central case data` → **Cloud required**;
  `Case/query presentation` → **Split**. Body step 10 requires the §4.1 value per row.
  **The matrix has no placement column** (ten columns, no placement) — [[FND-015]] F-13 measures
  this and takes the default of recording it in-cell as
  `Placement: <value> (proposal §4.1)` inside "Native screen/use case". **This ticket takes the
  same default**, so the four row-population tickets stay consistent; the schema question is
  parked in `open-questions`.

### Assumptions

- **A-01-03-1 — `PAR-07`…`PAR-12` and `PAR-40`/`PAR-41` are the complete §13.3/§13.6 surface.**
  Confirmed by F-3 and F-3a: 28 + 9 handlers map with none left over, and the remaining 19
  `Cases/**` handlers belong to named sibling rows. Breaks if [[FND-014]]'s difference list (a)
  finds a §13.3/§13.6 page model with no row. **Confirm by reading [[FND-014]]'s difference
  lists**, not yet written.
- **A-01-03-2 — Every mutating case command uses `CaseMutationRequest`.** Based on F-4 plus the
  `CaseMutationPageModel` PRG wrapper being shared by eight page models ([[FND-014]] F-9b).
  Breaks wherever a handler builds its own request type — `OnPostCreateAsync` (`PAR-09`) almost
  certainly does, since a case being created has no `CaseId` or `ExpectedVersion` yet.
  **Confirm by reading** `src/Pegasus.Web/Pages/Cases/Create.cshtml.cs:266` and the request type
  it constructs. If create is exempt, say so on `PAR-09` — area 03 needs to know which endpoint
  carries an idempotency key instead of a version.
- **A-01-03-3 — 100 is a hard cap everywhere, not a default.** Based on F-7's call sites all
  passing `100` to a `RequireText`-style guard. Breaks if any site treats it as advisory or uses
  a different number. **Confirm by opening** the three named constants and two of the literal
  sites. If it is genuinely uniform, the row can state one number; if not, the desktop cannot
  assume one.
- **A-01-03-4 — `PAR-41`'s dedicated test evidence is thin or absent.** Based on F-9's narrowed
  search returning only organization-named files. Breaks if principal replacement is tested
  inside one of them under a different name. **Confirm by opening**
  `OrganizationAdministrationWebTests.cs` and `OrganizationAdministrationPersistenceTests.cs`.
  If nothing asserts replace-not-edit, that is a `gap:` on a product invariant (F-10) and
  [[FND-025]] should rank it high.
- **A-01-03-5 — The six already-`inventoried` rows need completing, not rewriting.** Breaks
  where a cell is actively wrong; [[FND-015]] found two such cells among four rows, so verify
  rather than assume.

## Execution placement

**This ticket places no responsibility anywhere.** It is read-only inspection of
`src/Pegasus.Web`, `src/Pegasus.Core` and `tests/`, plus edits to
`docs/desktop/01-inventory-and-parity/parity-matrix.md` and possibly `docs/open-decisions.md`.
It starts no process, holds no credential, publishes no artefact and makes no Azure call
(Guardrails: "no write. This ticket makes no Azure call."). The six-question
cloud-justification test of `docs/desktop/00-governance-and-workflow/README.md` § 3 is
therefore not answered here.

The one placement it **assumes**: the enumeration runs on a developer workstation against a
local checkout, and its output is a repository document.

The rows' own placement values are **recorded, not decided** (F-13) — read verbatim from
proposal § 4.1. The six-question tables belong to the ADRs authored by [[FND-005]] (plan handle
`DSK-00-05`), and `Central case data` → *Cloud required* is exactly the answer ADR-0103
(gateway, never direct database access from workstations) will carry.

## Implications

1. **`PAR-08` is the concurrency contract of the whole conversion.** F-4 (six-field envelope),
   F-5 (three lease ports), F-6 (64-hex token, expiry without a sweeper, version compare) and
   F-7 (100-char key) together are what area 03 must reproduce on `/api/v1` and what area 04's
   session client must respect. Recorded properly, that row saves the slice tickets a rediscovery.
2. **The operation-key cap has no single home (F-7).** Worth stating on the rows and worth a
   note to [[FND-029]] (plan handle `DSK-02-04`, `src/Pegasus.Contracts` envelopes), which is
   where a shared constant would naturally live.
3. **`RunDueChasers` is unattended work (F-7a)** — the one part of `PAR-11`'s Core folder that
   is not a desktop command. Calling it out prevents a slice ticket from trying to schedule
   chases from the client.
4. **Two of the body's search commands need correcting before use** — the `Cases/**` glob (F-2)
   and the organization/principal grep (F-9). Both would otherwise produce a wrong count or an
   unusable 68-file result.
5. **`PAR-41` is where a real gap is likely** (A-01-03-4), and it sits on a product invariant.
6. **`PAR-08` and `PAR-09` are large rows** (654 and 689 lines). Body Guardrails say: record the
   concern in this research if the evidence does not fit one row; **do not split the plan row**.

---

## NOT YET CAPTURED — the spike's remaining work

Each block names the exact command and the question its output must answer; each has one
unticked box in `open-questions`.

### NOT YET CAPTURED — U-1: the row-by-row citation table

**Command:** none — assembly. One table for the eight owned rows:
`PAR id → entry point(s) → handlers (path:line) → command set expanded → Core owner (path:line)
→ FRD owner → test file or gap: → placement (§4.1) → inventoried-at SHA` (body step 11).
**Question it must answer:** can the Phase 3/4 slice tickets build the case workspace from this
table without reopening the page models?

### NOT YET CAPTURED — U-2: the full-surface handler map, with the corrected glob

**Commands:** `git ls-files 'src/Pegasus.Web/Pages/Cases/*.cshtml.cs' | wc -l` (expect **12** —
**not** the `**/` spelling, which returns 4; see F-2) and
`git grep -n "public .*On\(Get\|Post\)" -- 'src/Pegasus.Web/Pages/Cases' 'src/Pegasus.Web/Pages/Administration/Organizations' 'src/Pegasus.Web/Pages/Administration/Principals'`.
**Question it must answer:** does every one of the 47 + 9 handlers land in exactly one `PAR` row,
with the 19 sibling-owned ones cross-referenced by row id and left unfilled?

### NOT YET CAPTURED — U-3: `PAR-10` and `PAR-12` command lists mapped to `CaseLifecycle`

**Command:** for each of the 7 + 4 handlers in F-3, read the Core call it makes and cite the
transition in `src/Pegasus.Core/Lifecycle/CaseLifecycle.cs` (629 lines) or
`CaseCommandSeams.cs` by line.
**Question it must answer:** which `CaseLifecycle` transition backs each command — so the matrix
records commands, never handler names (area plan § 7 trap 1; body step 5)?

### NOT YET CAPTURED — U-4: `PAR-11` command list mapped to `src/Pegasus.Core/Tasks/`

**Command:** for each of the 8 handlers, cite the use case in the five files of F-7a.
**Question it must answer:** which of the eight are operator commands and which touch unattended
work (`RunDueChasers.cs`)? (Body step 6.)

### NOT YET CAPTURED — U-5: A-01-03-2 settled for `PAR-09`

**Command:** read `src/Pegasus.Web/Pages/Cases/Create.cshtml.cs:266` and the request type it
builds; compare against `CaseWorkflowContracts.cs:182`.
**Question it must answer:** does case creation use `CaseMutationRequest` or a separate
allocation request with an idempotency key instead of `ExpectedVersion`? Area 03 needs the
answer to shape `POST /api/v1/cases`.

### NOT YET CAPTURED — U-6: A-01-03-3 settled for the operation-key cap

**Command:** open `Cases/OrganizationAdministration.cs:274`,
`Identity/StaffAccountAdministration.cs:410`, `Intake/DurableIntake.cs:256` and two literal
sites from F-7.
**Question it must answer:** is 100 a uniform hard cap in every site, and is there any single
Core constant a client could reference? (If not, say so — it is a note for [[FND-029]].)

### NOT YET CAPTURED — U-7: test evidence resolved for `PAR-10`, `PAR-11`, `PAR-12`, `PAR-40`, `PAR-41`

**Commands:** the F-8 and F-9 candidate lists, each file **opened** before it is cited; the
narrowed principal grep of F-9 rather than the body's 68-file one.
**Question it must answer:** for each row, is there a test that asserts the behaviour the cell
claims? Where none does, write `gap: <untested behaviour>` — never a plausible-looking test
name. Settles A-01-03-4.

### NOT YET CAPTURED — U-8: `gap:` lines handed to [[FND-025]]

**Command:** none — copy each `gap:` line into this document under a
`### Gap list for DSK-01-12` heading (body step 9).
**Question it must answer:** does [[FND-025]] (plan handle `DSK-01-12`) receive every gap in a
consumable form?

### NOT YET CAPTURED — U-9: the matrix edits

**Command:** none — the edit. Advance the eight rows to `inventoried`, record the F-4 envelope
verbatim on `PAR-08`, stamp the SHA on every touched row, leave every `~` endpoint name and
blank UAT owner untouched, touch no sibling row.
**Question it must answer:** does the diff change only the eight owned rows?

### NOT YET CAPTURED — U-10: the documentation gate

**Command:** `pwsh ./scripts/Test-DocumentationLinks.ps1` — exit 0.
**Question it must answer:** do the edits keep the CI `documentation` job green?

## Open questions

Tracked as unticked items in this ticket's `open-questions` document.

- U-1 … U-10 above.
- **Is principal replace-not-edit tested at all?** (A-01-03-4 / U-7.) It is a product invariant
  (`AGENTS.md:240-242`); an untested invariant is worth ranking high in [[FND-025]].
- **Does case creation share the mutation envelope?** (A-01-03-2 / U-5.) The answer changes the
  shape of `POST /api/v1/cases` in area 03.

**Not open questions — scope boundaries owned by named tickets:**

- The confirmed skeleton and the three difference lists: [[FND-014]] (plan handle `DSK-01-01`).
- `PAR-13`, `PAR-16`, `PAR-17`: [[FND-017]] (`DSK-01-04`). `PAR-14`, `PAR-15`, `PAR-18`:
  [[FND-018]] (`DSK-01-05`). Cross-reference their rows; do not fill their cells (body step 2).
- Promoting a `~` endpoint name: area 03's endpoint map (`parity-matrix.md` § Notes).
- A shared operation-key constant: [[FND-029]] (`DSK-02-04`, `src/Pegasus.Contracts`), if anyone
  wants one — this ticket only records that there is none.
- Assigning a UAT owner: the operator, per capability group.
- The characterization-gap list: [[FND-025]] (`DSK-01-12`).
- Whether the matrix moves to `docs/features/`: [[FND-012]] (`DSK-00-12`).
