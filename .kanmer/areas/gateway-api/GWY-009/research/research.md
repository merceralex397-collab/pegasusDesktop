# Research — GWY-009: Case tasks, notes, manual chasers and report-evidence links on `/api/v1`

## Question

Two questions, because this ticket carries two halves. (1) What exactly do the eight
`Cases/Tasks` page handlers call in Core, and which version does each one expect, so that the
`/api/v1` projection is an argument-mapper and nothing more? (2) Where do document-removal and
custody confirm/fail events land today, and what must change so each one appears exactly once on
the timeline the operator actually reads (upstream DOCS-012)?

## Current behaviour

`src/Pegasus.Web/Pages/Cases/Tasks.cshtml.cs` (248 lines) is the whole surface. It is a
`CaseMutationPageModel` (`src/Pegasus.Web/Pages/Cases/CaseMutationPageModel.cs`, 339 lines) and
carries eight `OnPost*Async` handlers:

| Handler | line | Core port | Request record | Versions it expects |
| --- | --- | --- | --- | --- |
| `OnPostAddNoteAsync` | `:33` | `IAddCaseNote` (`src/Pegasus.Core/Cases/CaseNotes.cs:20`) | `AddCaseNoteRequest(CaseId, Actor, OperationKey, Note)` (`CaseNotes.cs:14-18`) | **none** |
| `OnPostCreateTaskAsync` | `:61` | `ICreateCaseTask` (`src/Pegasus.Core/Tasks/CaseTaskContracts.cs:158`) | `CreateCaseTaskRequest` (`:36-45`) | case only (`ExpectedCaseVersion`) |
| `OnPostAssignTaskAsync` | `:89` | `IAssignCaseTask` (`:165`) | `AssignCaseTaskRequest` (`:57-73`) | **case and task** |
| `OnPostCompleteTaskAsync` | `:117` | `ICompleteCaseTask` (`:172`) | `CompleteCaseTaskRequest` (`:75-91`) | **case and task** |
| `OnPostCancelTaskAsync` | `:143` | `ICancelCaseTask` (`:179`) | `CancelCaseTaskRequest` (`:93-109`) | **case and task** |
| `OnPostRecordManualChaseAsync` | `:169` | `IRecordManualCaseChase` (`src/Pegasus.Core/Tasks/CaseWorkScheduling.cs:65`) | `ManualChaseRecord` (`CaseWorkScheduling.cs:31-42`) | case only |
| `OnPostLinkReportEvidenceAsync` | `:201` | `ILinkReportEvidence` (`src/Pegasus.Core/Workflow/CaseWorkflowContracts.cs:427`) | `LinkReportEvidenceRequest` | case only |
| `OnPostUnlinkReportEvidenceAsync` | `:225` | `IUnlinkReportEvidence` (`CaseWorkflowContracts.cs:441`) | `UnlinkReportEvidenceRequest` | case only |

Seven of the eight go through `CaseMutationPageModel.ExecuteCaseCommandAsync`
(`CaseMutationPageModel.cs:109`), which resolves the actor at `:149`, catches
`StaffAuthorizationException` at `:160`, and finishes with `RedirectToDetails` — the PRG/TempData
half that must not be reproduced. `OnPostAddNoteAsync` bypasses it entirely and calls
`TryGetActor` itself (`Tasks.cshtml.cs:39`).

Parity matrix row: **`PAR-11`** (`docs/desktop/01-inventory-and-parity/parity-matrix.md:56`) —
"13.3 Case lifecycle / FRD-01 / `Cases/Tasks.cshtml.cs` (248)", naming all eight handlers, with
the intended API column `~POST /api/v1/cases/{id}/notes`, `~.../tasks`,
`~.../tasks/{taskId}/assign|complete|cancel`, `~.../chases`, `~.../report-evidence`, status
`inventoried`. The matrix holds 46 rows
(`grep -c '^| PAR-' docs/desktop/01-inventory-and-parity/parity-matrix.md` → `46`).

The DOCS-012 half has no page handler at all — it is what the persistence layer does **after** an
operator acts:

- `src/Pegasus.Infrastructure/Persistence/EfDocumentCustodyStore.cs:419-461`
  (`ILogicallyRemoveDocument.ExecuteAsync`) sets `IsLogicallyRemoved`, `IsCurrent`,
  `RemovalReason`, `RemovalOperationKey`, calls `CaseMutationGuard.Complete(workflow)` and saves.
  It writes **no event of any kind**.
- `src/Pegasus.Infrastructure/Persistence/EfQueuedCustodyProcessor.cs:594-605` writes
  `custody_confirmed` into `context.Set<CaseHistoryEntity>()`; `:661` writes
  `audit_custody_confirmed` into `context.CaseHistory`.
- `src/Pegasus.Infrastructure/Persistence/EfExternalWorkStore.cs:450-456` and `:608-614` write
  `custody_failed`, `:481-487` and `:635-641` write `audit_custody_failed` — all four into
  `context.CaseHistory`.

## Findings

- The two version scopes are **not symmetric across the four task commands**, and the ticket body's
  phrasing ("task commands carry the task version") is a simplification the implementer must not
  take literally.
  - `CreateCaseTaskRequest` (`CaseTaskContracts.cs:36-45`) has `ExpectedCaseVersion` and **no**
    `ExpectedTaskVersion` — there is no task yet to be at a version. `AssignCaseTaskRequest`,
    `CompleteCaseTaskRequest` and `CancelCaseTaskRequest` (`:57-109`) all derive from
    `ExistingCaseTaskMutationRequest` (`:47-55`) and carry **both**.
  - So three routes take two version fields, one takes one, and the DTOs must mirror that shape
    rather than a uniform one.
- **A note takes neither an expected version nor an edit lease, by an explicit recorded decision.**
  `Tasks.cshtml.cs:28-32`: "A note takes no edit lease and no expected version: it adds to the
  case's record rather than changing the case, so it must not contend with an engineer editing the
  same case (CASE-017)." `AddCaseNoteRequest` (`CaseNotes.cs:14-18`) has four members and none is
  a version or a lease token.
  - This contradicts the endpoint-map row for `POST /cases/{id}/notes`, which says
    "`CaseMutationRequest` fields"
    (`docs/desktop/03-gateway-api-and-data/endpoint-map.md` § Cases, Tasks rows). The ticket body's
    step 5 settles it in the repository's favour — "construct the Core request records exactly as
    the page handlers do; the endpoint adds no rule of its own" — so the notes DTO carries
    `operationKey` and `note` only. Recorded in Open questions below; the plan writes it as a risk,
    not a change of scope.
- `AddCaseNote` (`CaseNotes.cs:33-81`) enforces three rules the endpoint must not duplicate:
  `StaffAuthorization.Require(actor, PerformCasework)` (`:48`); `Actor.Kind != ActorKind.Staff`
  throws (`:53-56`) — the Automation actor is barred from authoring a note; and
  `MaximumLength = 2000` (`:42`, thrown at `:70-75`). Its history event type is the constant
  `AddCaseNote.EventType = "operator_note"` (`:40`).
- `CaseTaskVersionConflictException` (`CaseTaskContracts.cs:21-30`) carries `TaskId`,
  `ExpectedVersion` and `ActualVersion` and derives from `InvalidOperationException` — so a
  problem-details mapper that only catches `CaseVersionConflictException` will fall through to the
  generic `InvalidOperationException` arm and return the wrong problem type. This is the concrete
  reason step 6 exists.
- `CaseTaskRecord` (`CaseTaskContracts.cs:12-19`) returns `Version` **and** `CaseVersion`, so a
  successful task command can tell the desktop both new versions in one response without a re-read
  — which is what step 8 asks for.
- Replay protection for the task family is
  `ICaseTaskStore.HasOperationAsync(caseId, operationKey, …)` (`CaseTaskContracts.cs:117-120`) —
  keyed on the **case**, not the task. A replayed key on a different task in the same case is
  therefore already a Core-level concern, not an endpoint one.
- `EfCaseNoteStore` (`src/Pegasus.Infrastructure/Persistence/EfCaseNoteStore.cs`, 66 lines) is the
  exact template for step 9. Its class comment (`:13-18`) records the defect this ticket is
  preventing: "It must be `CaseWorkflowEventEntity` specifically: the Notes tab reads
  `CaseWorkflowEvents` (`EfCaseQueryStore`), and the first version of this store wrote to
  `CaseHistory` instead … The note was persisted, the page reported success, and the timeline
  stayed empty." Its field set is at `:48-63`: `EventType`, `OperationKey`, `RequestHash`,
  `ActorKind`, `ActorSubjectId`, `ActorRolesJson`, `Reason`, `OccurredAtUtc`, `BeforeVersion`,
  `AfterVersion`. Its replay check is at `:35-44` — an `AnyAsync` on `(CaseId, OperationKey)`.
- The read side of the round trip is `EfCaseQueryStore.cs:181-196`: it projects
  `CaseWorkflowEvents` filtered by `CaseId`, ordered `OccurredAtUtc` descending then `Id`
  descending, `Take(200)`, into `CaseHistoryEntry(EventType, ActorSubjectId, ActorKind,
  OccurredAtUtc, Reason, BeforeVersion, AfterVersion)`. Nothing in that projection reads
  `CaseHistory`. The `Take(200)` cap is the reason a test must assert the new entry is present
  rather than counting the whole list on a busy fixture.
- The system-written precedent to copy is `report_evidence_auto_linked`
  (`EfCaseWorkflowStore.cs:915`, inside the block beginning `:901`): it serialises before/after
  JSON, increments `workflow.Version`, calls `ClearLease(workflow)`, then `AddEvent(context,
  workflow, request.Actor, operationKey, request.Reason.Trim(), requestHash,
  "report_evidence_auto_linked", beforeVersion, workflow.Version, now, beforeJson, afterJson)`.
  Its replay arm is at `:855-872` and keys on `RequestHash`.
- The five operator labels at `src/Pegasus.Web/Presentation/OperatorLabels.cs:389-394` already
  exist and are correct: `report_evidence_auto_linked` → "Sent report linked automatically",
  `audit_custody_confirmed` → "Audit evidence stored", `audit_custody_failed` → "Audit evidence
  storage failed", `custody_confirmed` → "Document stored", `custody_failed` → "Document storage
  failed". Four of the five are unreachable today because the events they name are written into a
  table no surface projects. The ticket forbids rewording them; moving the writes is what makes
  them reachable.
- **No `Grant*` migration is needed for the DOCS-012 half, and this is verified rather than
  assumed.** `20260729199000_RuntimeRoleReconciliation.cs` grants
  `("CaseWorkflowEvents", "SELECT, INSERT")` to the **Web** role in `WebGrants` (`:94` list, entry
  at `:122`) and to the **Worker** role in `WorkerGrants` (`:166` list, entry at `:181`); the Web
  grant originates in `20260729176000_AzureSqlRuntimeLeastPrivilege.cs:59`. The pinned census in
  `tests/Pegasus.IntegrationTests/AzureSqlRuntimeRoleMigrationTests.cs` already records
  `CaseWorkflowEvents:SELECT,INSERT` at `:119` and `:177`, so it needs no edit either. This closes
  the area plan § 7 "runtime-role grants" trap for this ticket — the two Worker-side writers
  (`EfQueuedCustodyProcessor`, `EfExternalWorkStore`) can insert into `CaseWorkflowEvents` under
  the role they already run as.
- Existing test fixtures to mirror: `tests/Pegasus.IntegrationTests/CaseTasksWebTests.cs` (181
  lines) for the web-side behaviour that must stay green; `CaseNotePersistenceTests.cs` (154 lines)
  for the note round trip; `CaseTaskArchivePersistenceTests.cs` (1,019 lines) for the task version
  matrix; `DueChaserSweepPersistenceTests.cs` (426 lines) for the chase schedule.
- The projects this ticket writes into do **not exist yet**: `ls src` returns
  `Pegasus.Core`, `Pegasus.Infrastructure`, `Pegasus.Web`, `Pegasus.Worker` only. There is no
  `src/Pegasus.Contracts`, no `src/Pegasus.Web/Api`, no `openapi/` and no
  `src/Pegasus.Desktop.Infrastructure`. Every one of those is created by a named earlier ticket —
  see the files document.

### Facts

Everything under Findings above is read from the repository at the paths and lines given, on
2026-08-24, on branch `task/desktop-plan-segmentation`. The commands behind the counts:

- `wc -l src/Pegasus.Web/Pages/Cases/Tasks.cshtml.cs` → `248`
- `wc -l src/Pegasus.Infrastructure/Persistence/EfCaseNoteStore.cs` → `66`
- `grep -c '^| PAR-' docs/desktop/01-inventory-and-parity/parity-matrix.md` → `46`
- `grep -n "custody_failed\|audit_custody_failed\|CaseHistory" src/Pegasus.Infrastructure/Persistence/EfExternalWorkStore.cs`
  → writes at `:210`, `:450`, `:481`, `:608`, `:635`. The four this ticket owns are `:450`, `:481`,
  `:608`, `:635`. **`:210` is a different event and is out of scope** — the guardrail names four
  line ranges in this file, not five.
- `grep -n "CaseWorkflowEvents" src/Pegasus.Infrastructure/Persistence/Migrations/20260729199000_RuntimeRoleReconciliation.cs`
  → `:47`, `:122`, `:181`, `:228`, `:276` (runtime-table list, Web grants, Worker grants, and the
  two `Previous*` rollback lists).
- `ls src` → four projects, none of them `Pegasus.Contracts`.
- `grep -rn "RunDueChasers" src/Pegasus.Worker/` → `EmailEvidenceFunctions.cs:50`.

Documentation read: `docs/desktop/03-gateway-api-and-data/README.md` § 3 (Idempotency,
Concurrency, Problem details rows) and § 5 row `DSK-03-09`;
`docs/desktop/03-gateway-api-and-data/endpoint-map.md` § Cases (Tasks rows);
`docs/desktop/00-governance-and-workflow/README.md` § 3 (cloud-justification test).

### Assumptions

- **A-GWY-1** — the problem-details mapper created by [[GWY-002]] (plan handle `DSK-03-02`) is a
  single file with one exception-to-problem switch, so adding the
  `CaseTaskVersionConflictException` arm there is a one-place change. *Confirmed by:* reading that
  file once [[GWY-002]] has merged. *If wrong:* step 6 becomes "add the arm wherever the mapping
  lives", and the estimate for that file grows by the number of places; the rule that no second
  catch block appears in the endpoint file still holds.
- **A-GWY-2** — the `cases` sub-group and the `/api/v1` write rate-limit policy from [[GWY-008]]
  (plan handle `DSK-03-08`) are exposed as an extension point this file can attach to, rather than
  a closed builder. *Confirmed by:* reading the group registration [[GWY-008]] adds.
  *If wrong:* the endpoint file registers its own routes against the same group builder and the
  diff grows by the registration lines only.
- **A-GWY-3** — moving `custody_confirmed` / `custody_failed` (and the two audit variants) off
  `CaseHistory` breaks no existing assertion, because nothing operator-facing reads that table.
  *Confirmed by:* `grep -rn "CaseHistory" tests/ src/Pegasus.Web/` before the move, which the plan
  makes an explicit step. *If wrong:* an existing test asserts the old table and must be rewritten
  to assert the history projection instead — which is the same correction the ticket is making, so
  the fix is aligned, not a scope change.
- **A-GWY-4** — the two Worker-side writers hold a live `CaseWorkflowEntity` (or can load one) at
  each of the six write sites, so `BeforeVersion` / `AfterVersion` can be filled the way
  `EfCaseNoteStore.cs:61-62` fills them. *Confirmed by:* the code already reads `workflow.Version`
  into `beforeVersion` at every site — `EfQueuedCustodyProcessor.cs:594-605` and `:661`,
  `EfExternalWorkStore.cs:450-456`, `:481-487`, `:608-614`, `:635-641` — so this is very likely
  already true; the removal site (`EfDocumentCustodyStore.cs:419-461`) also loads the workflow at
  `:446`. *If wrong at one site:* that site loads the workflow row it is already updating; no new
  query shape.

## Execution placement

The six-question cloud-justification test from `docs/desktop/00-governance-and-workflow/README.md`
§ 3, answered for the responsibility this ticket places: *authoritative case-task/note mutation and
the automatic case-note write*.

| Question | Answer | Evidence |
| --- | --- | --- |
| Shared authority — must several users see and update the same state? | **yes** — lands in the gateway (`Pegasus.Web`), per L-01 | `CaseTaskRecord` carries both `Version` and `CaseVersion` (`CaseTaskContracts.cs:12-19`) and `CaseTaskVersionConflictException` (`:21-30`) exists precisely because two operators contend for one task; `CaseMutationGuard.Require` gates every write on the case edit lease |
| Unattended execution — must it run with every desktop closed? | **yes** — lands on the existing Worker Container App, which already hosts it; nothing new is placed | `RunDueChasers` is composed in `src/Pegasus.Worker/EmailEvidenceFunctions.cs:50`, and the `custody_confirmed` / `custody_failed` writes this ticket relocates are executed by `EfQueuedCustodyProcessor` and `EfExternalWorkStore` on the Worker's queue path, with no operator present |
| Protected credentials — long-lived secret that must not sit on workstations? | **no** | No provider call on any of the eight routes; the DOCS-012 half writes to SQL through the existing Worker connection. Box/Graph credentials belong to area 07 |
| Public callback — must an external service call a stable public endpoint? | **no** | All eight routes are bearer-authenticated staff routes under `/api/v1`; nothing calls back |
| Central enforcement — revocation, permissions, audit, invariant independent of the client? | **yes** — lands in Core, invoked by the gateway | `AddCaseNote.ExecuteAsync` enforces `PerformCasework` (`CaseNotes.cs:48`) and bars a non-Staff actor (`:53-56`); the task store owns "case/task concurrency checks, active edit-lease check, idempotent replay and permanent history write" (`CaseTaskContracts.cs:112-115`). A desktop that enforced these would be a second policy engine |
| Measured operational advantage — measured evidence central is materially better? | **no** | No measurement exists for this path. Area 10 owns the Phase 3 baseline; nothing is claimed here |

Three "yes", and each one names the host that already carries it: `Pegasus.Web` for shared
authority (L-01), the existing Worker Container App for unattended execution, Core for central
enforcement. No new placement, no Azure resource, no Azure write.

## Implications

1. **The DTOs are not uniform and must not be made uniform.** Create takes the case version only;
   assign/complete/cancel take both; note takes neither. The ticket's instruction to name the task
   field distinctly stands and is what prevents the confusion — but four distinct DTO shapes, not
   one.
2. **The notes endpoint is the thinnest of the eight and the easiest to over-build.** Core's
   `AddCaseNoteRequest` has four members. Adding `expectedVersion` or `editLeaseToken` to the
   DTO to "match the family" would reintroduce exactly the contention CASE-017 removed, and would
   also drift toward the notes capability upstream CASE-004 keeps closed.
3. **Step 6 is load-bearing and easy to skip.** `CaseTaskVersionConflictException` derives from
   `InvalidOperationException`, so an unmapped arm does not fail loudly — it returns a plausible
   generic problem. The test for it must assert the problem `type` URI, not just the 409.
4. **The DOCS-012 half is a persistence change with a guardrail that forbids a shared helper.**
   The guardrail permits changes to three named files "and nothing else in
   `src/Pegasus.Infrastructure`", so the obvious refactor — one `CaseWorkflowEventWriter` helper —
   is out of scope. Each write site carries its own inline `CaseWorkflowEvents.Add`, modelled on
   `EfCaseNoteStore.cs:48-63`. The plan says so explicitly so a reviewer does not read the
   repetition as a defect.
5. **Six relocated write sites plus one new one, not three files' worth.** `custody_confirmed` ×1,
   `audit_custody_confirmed` ×1, `custody_failed` ×2, `audit_custody_failed` ×2 — six existing
   sites to move — plus the document removal, which today writes nothing and gains a new event.
   Counting them as "three files" is how one gets missed.
6. **The proof must go through the projection.** A test that queries `CaseWorkflowEvents` directly
   passes while reproducing the Release 22 defect. Every DOCS-012 fact asserts through
   `GET /cases/{id}/history` (from [[GWY-007]], plan handle `DSK-03-07`, backed by
   `EfCaseQueryStore.cs:181-196`) and adds the negative that `CaseHistory` gained no row.
7. **Everything this ticket writes into is created by an earlier ticket.** Nothing under
   `src/Pegasus.Contracts`, `src/Pegasus.Web/Api` or `openapi/` exists on disk today, so the
   implementer must confirm those tickets have merged before starting rather than creating the
   projects themselves.
8. **The grant trap does not bite here.** Verified above: both runtime roles already hold
   `SELECT, INSERT` on `CaseWorkflowEvents` and the census test already pins it. No migration, no
   `Invoke-AzureDatabaseBootstrap.ps1` change, no census edit.

## Open questions

- The endpoint-map row for `POST /cases/{id}/notes` says the concurrency token is
  "`CaseMutationRequest` fields", while Core's `AddCaseNoteRequest` has no version and no lease and
  `Tasks.cshtml.cs:28-32` records the CASE-017 decision that it must not. **Not left open**: the
  ticket body's step 5 binds the endpoint to the page handler's construction, so the notes DTO
  carries `operationKey` and `note` only, and the endpoint-map row is the stale text. Taken as a
  default rather than asked, per the authoring contract; recorded here and as a risk in the plan so
  the reviewer sees it was a decision. No `open-questions` document is created — the body does not
  instruct one and this is settled by the body itself.
- Nothing else is unresolved. The one question this research opened at the outset — whether the
  DOCS-012 half needs a runtime-role grant — is answered under Facts and needs no operator.
