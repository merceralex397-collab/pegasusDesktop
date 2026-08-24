# Plan — GWY-009: Case tasks, notes, manual chasers and report-evidence link endpoints

**Diff estimate: ~15 files, ~2,450 lines** (~1,100 of them generated).

Derived from the files document, measured rather than asserted:

| File | Lines | How the number was reached |
| --- | --- | --- |
| `src/Pegasus.Contracts/Cases/Commands/CaseTaskCommands.cs` (new) | ~120 | 4 request records (one 6-member, three 8-member) + 1 response record mirroring `CaseTaskRecord`'s 7 members (`src/Pegasus.Core/Tasks/CaseTaskContracts.cs:12-19`), at record-per-line style with XML doc on the two version fields |
| `src/Pegasus.Contracts/Cases/Commands/CaseNoteAndChaseCommands.cs` (new) | ~90 | `AddCaseNoteRequestDto` (2 members), `RecordManualChaseRequestDto` (9, mirroring `ManualChaseRecord`, `CaseWorkScheduling.cs:31-42`), `ReportEvidenceLinkRequestDto` (5), 2 response records |
| `src/Pegasus.Web/Api/CaseTaskEndpoints.cs` (new) | ~280 | 8 handlers × ~30 lines each (route, body binding, actor read, Core record construction, projection) + the group-attachment method |
| `/api/v1` problem mapper (edit, from [[GWY-002]], plan handle `DSK-03-02`) | +14 | One switch arm returning 409 `urn:pegasus:problem:version-conflict` with `taskId` / `expectedVersion` / `currentVersion` |
| `cases` sub-group registration (edit, from [[GWY-008]], plan handle `DSK-03-08`) | +2 | One `MapCaseTaskEndpoints(group)` call and its `using` |
| `EfDocumentCustodyStore.cs` (edit `:419-461`) | +22 | One inline `CaseWorkflowEvents.Add` in `EfCaseNoteStore.cs:48-63` shape; the workflow row is already loaded at `:446` |
| `EfQueuedCustodyProcessor.cs` (edit `:594-605`, `:661`) | ±34 | Two blocks of ~12 lines removed and ~17 added (the workflow-event shape carries 3 more members than `CaseHistoryEntity`) |
| `EfExternalWorkStore.cs` (edit `:450-456`, `:481-487`, `:608-614`, `:635-641`) | ±68 | Four blocks, same arithmetic |
| `tests/Pegasus.IntegrationTests/DesktopGatewayCaseTaskTests.cs` (new) | ~800 | 8 commands × 7 cases = 56 facts, plus 1 wrong-scope fact, plus 7 DOCS-012 round-trip facts and 1 negative = 65 facts. `CaseTaskArchivePersistenceTests.cs` (1,019 lines) carries a comparable count with shared arrange helpers, so ~12 lines per fact after helpers |
| `openapi/pegasus-v1.json` (regenerated, from [[GWY-004]], plan handle `DSK-03-04`) | ~+450 | 8 paths + 13 schemas at the density of an ASP.NET Core OpenAPI document |
| `src/Pegasus.Desktop.Infrastructure/Api/Generated/**` (regenerated, from [[FND-031]] / [[GWY-005]]) | ~+650, ~4 files | Kiota emits a request-builder class per path segment plus a model class per schema |

## Approach

Project the eight `Cases/Tasks` handlers one-to-one onto eight named `/api/v1` routes, each an
argument-mapper that constructs the same Core request record the page handler constructs and
returns the Core result — and, in the same branch, move the six custody events (plus one new
removal event) from `CaseHistory` onto `CaseWorkflowEvents`, the table the case history projection
actually reads.

The rejected alternative was to **land the DOCS-012 notes rule as a separate follow-up ticket**,
which is superficially cleaner: it is a persistence change with no API surface, and the guardrail
has to carve a named exception into `src/Pegasus.Infrastructure` to allow it. It was rejected
because the acceptance criterion is a round trip — "each event type appears once in
`GET /cases/{id}/history`" — and that round trip can only be asserted once both halves exist.
Splitting them would leave the first half untestable and the second half unowned; upstream
DOCS-012's own plan already deferred it once ("Real, but a separate ticket"), and this ticket is
that separate ticket. Landing them together also means one regeneration of the snapshot and the
client rather than two.

The second rejected alternative was a **shared `CaseWorkflowEventWriter` helper** in
`src/Pegasus.Infrastructure/Persistence/`, to avoid seven near-identical inline blocks. The
guardrail permits three named files "and nothing else in `src/Pegasus.Infrastructure`", so a new
file there is out of scope. Inline writes modelled on `EfCaseNoteStore.cs:48-63` are the intended
shape; the reviewer is told so here so the repetition is not read as a defect.

## Governing docs

The ticket's `refs` is **empty** and it carries `docs_todo: true` (confirmed in
`get_doc_gates GWY-009`: the `leave-backlog` `governing-doc` requirement shows `satisfied: true`
on that basis). So:

> **New ADR** — ADR-0103 (gateway, evolved `Pegasus.Web`, never direct database access from
> workstations), authored by [[FND-005]] (plan handle `DSK-00-05`).
> This plan is written to the decision as recorded in
> `docs/desktop/00-governance-and-workflow/README.md` § 3 (ADR table row ADR-0103) and locked
> decision L-01 in `docs/desktop/README.md`; if the ADR lands differently this plan is revised
> before implementation.
> ADR-0101 (local-execution / cloud-authority split and the six-question test) also binds, is
> likewise authored by [[FND-005]], and is what the research document's *Execution placement*
> section answers against.

Because `refs` is empty, the programme-level authorities that bind today, with the step that
satisfies each:

| Authority | Requirement | Met by |
| --- | --- | --- |
| Proposal § 10.2 (API style) | Commands are explicit auditable verbs, never a generic action endpoint | Step 4 — eight named routes, no dispatcher |
| Proposal § 13.3 (case lifecycle) | Tasks, notes and chasers are lifecycle surface the desktop must carry | Steps 3–8 |
| Proposal § 14.5 (case workspace) | The Tasks strip works natively — assign, complete, cancel, chase without the web app | Step 8 (responses carry the identifiers and both versions so no re-read is needed) |
| L-01 (`docs/desktop/README.md`) | The gateway is `Pegasus.Web` evolved in place; no new deployment unit | Step 4 — routes registered on the existing `cases` sub-group inside `Pegasus.Web` |
| L-02 | Evidence comes from the local production-mimicking stack only; no Azure test environment | Steps 10–12, 14 — `WebApplicationFactory<Program>` + LocalDB, no Azure call |
| Plan 03 § 3 row *Idempotency* | `operationKey` is an explicit body field, `desk:` prefix, ≤ 100 characters | Step 7 |
| Plan 03 § 3 row *Concurrency* | `expectedVersion` and `editLeaseToken` are explicit body fields; conflicts → 409 problem carrying the current version | Steps 3 and 6 |
| Plan 03 § 3 row *Problem details* | RFC 9457, stable `urn:pegasus:problem:<slug>` types, one mapping site | Step 6 |
| Plan 03 § 3 row *Projection style* | Endpoints are thin argument-mappers over Core ports; no business rule in Web | Step 5 |
| Plan 03 § 7 trap *Two policy engines* | Any rule appearing in an endpoint filter is a defect | Steps 5 and 15 (simplification pass) |
| Plan 03 § 7 trap *Runtime-role grants* | Any new write path needs its `Grant*` migration | Step 2 — verified **not** needed here; the grant already exists |
| Plan 03 § 7 trap *TempData semantics* | `CaseMutationPageModel`'s proposed-values/lease chaining must not be ported | Step 5 |
| Plan 03 § 4 exit gate | Every command endpoint has the seven-case matrix and the problem-details shape | Steps 10–11 |
| `AGENTS.md` § Repository task workflow step 4 | A simplification pass over the branch diff before the PR, recorded | Step 15 and the *Simplification pass* heading below |
| Operator answer of 2026-08-24 (quoted in the body from upstream DOCS-012) | Document changes are recorded as "notes (created by system same as other automatic notes)" | Step 9 |
| Operator decision 2026-08-19 (upstream CASE-004) | Future notes scope "does not authorize placeholder notes, fabricated content, or a hidden write path" | Step 3 — the notes DTO is the existing command at parity and nothing more |

## Routing

Copied from the ticket body's `## Routing` block.

- **Subagent**: `pegasus-gateway-dev` — `.codex/agents/pegasus-gateway-dev.toml`
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `dotnet-webapi` (`dotnet/skills`
  `98f84851`, plugin `dotnet-aspnetcore`)
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`,
  `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search`)
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute`
  → `kanmer-review` → `kanmer-verify` → `kanmer-closeout`
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement
  (`AGENTS.md` § Repository task workflow step 5)

## Steps

These refine the ticket body's fourteen implementation steps in the same order and with the same
ownership. Step 2 and step 15 are the body's step 2 and step 14 with the *how* filled in; the
numbering below adds one step because the body's step 14 carries both the test run and the
simplification pass.

1. **Orient and confirm the prerequisites exist.** Read the four Tasks rows in
   `docs/desktop/03-gateway-api-and-data/endpoint-map.md` § Cases and § 3 of the area README, and
   the upstream DOCS-012 / upstream CASE-004 bodies named under *Source of truth*. Then
   `get_doc_gates GWY-009` and `take_ticket`. Before writing code, confirm on disk that
   `src/Pegasus.Contracts/`, `src/Pegasus.Web/Api/`, `openapi/pegasus-v1.json` and
   `src/Pegasus.Desktop.Infrastructure/Api/Generated/` exist — none of them does today, and each
   is created by a ticket named in the files document. If any is missing, stop: this ticket is
   not startable.
2. **Read `src/Pegasus.Web/Pages/Cases/Tasks.cshtml.cs` in full and record the version scope per
   handler**, then check the grant position. The scopes are in the research document's *Current
   behaviour* table and are **not uniform**: create takes the case version only, assign/complete/
   cancel take case **and** task, note takes neither, chase and both report-evidence commands take
   the case version. Then run
   `grep -n "CaseWorkflowEvents" src/Pegasus.Infrastructure/Persistence/Migrations/20260729199000_RuntimeRoleReconciliation.cs`
   and confirm entries at `:122` (Web role) and `:181` (Worker role) — both already
   `SELECT, INSERT`. This closes the area plan § 7 grant trap; no migration is written.
3. **Add the contract DTOs** in `src/Pegasus.Contracts/Cases/Commands/`. Two files, shapes as in
   the files document. The task field is named `taskExpectedVersion` and the case field
   `expectedVersion`, and only assign/complete/cancel carry both — create carries `expectedVersion`
   alone, because `CreateCaseTaskRequest` (`CaseTaskContracts.cs:36-45`) has no task version to
   expect. `AddCaseNoteRequestDto` carries `operationKey` and `note` **only**: no version, no lease
   token, per the CASE-017 comment at `Tasks.cshtml.cs:28-32`. The DTOs carry no EF, ASP.NET or
   WinUI reference — the architecture test from [[GWY-001]] (plan handle `DSK-03-01`) enforces it.
4. **Add `src/Pegasus.Web/Api/CaseTaskEndpoints.cs`** and attach it to the `cases` sub-group from
   [[GWY-008]]. Eight named routes, no dispatcher: `POST /{id}/notes`, `POST /{id}/tasks`,
   `POST /{id}/tasks/{taskId}/assign`, `/complete`, `/cancel`, `POST /{id}/chases/manual`,
   `POST /{id}/report-evidence/link`, `POST /{id}/report-evidence/unlink`.
5. **Resolve the actor from `HttpContext.Items` and build the Core records exactly as the page
   handlers do.** Compare each construction against the page handler line by line
   (`:46`, `:75-86`, `:103-114`, `:130-140`, `:156-166`, `:185-198`, `:213-222`, `:237-246`).
   The endpoint adds no rule: not the `PerformCasework` check (Core does it,
   `CaseNotes.cs:48`), not the Staff-actor bar (`:53-56`), not the 2,000-character note cap
   (`:42`). Nothing from `CaseMutationPageModel` — no TempData, no PRG, no proposed-value chaining.
6. **Map `CaseTaskVersionConflictException` in the problem middleware from [[GWY-002]], not here.**
   It derives from `InvalidOperationException` (`CaseTaskContracts.cs:21`), so without an explicit
   arm it silently falls through to the generic arm and returns a plausible wrong problem type.
   The arm returns 409 `urn:pegasus:problem:version-conflict` carrying `taskId`,
   `expectedVersion` and `currentVersion` from the exception's own properties (`:27-29`). Do not
   add a `catch` in the endpoint file.
7. **Apply the `operationKey` boundary rule and reuse the existing write limiter.** `desk:` prefix,
   ≤ 100 characters, no whitespace or control characters — validated at the boundary and rejected
   as `urn:pegasus:problem:validation`. Attach the `/api/v1` write rate-limit policy added in
   [[GWY-008]]; do not introduce a second limiter mechanism (area plan § 7).
8. **Return what the endpoint-map rows name**, so the desktop's task strip updates without a
   re-read: the note id and case version for `POST /{id}/notes`; the full `CaseTaskResponseDto`
   including both `version` and `caseVersion` for the four task routes (`CaseTaskRecord` already
   carries both, `CaseTaskContracts.cs:17-18`); the new case version for the chase and both
   report-evidence routes.
9. **Write the document changes as automatic case notes (upstream DOCS-012).** Seven write sites
   across three files, each writing **exactly one** `CaseWorkflowEventEntity` in the
   `EfCaseNoteStore.cs:48-63` shape — system actor kind, the operation key as replay protection
   and `RequestHash`, the reason carrying the operator-facing text, `BeforeVersion`/`AfterVersion`
   from the workflow row already in hand:
   - (a) `EfDocumentCustodyStore.cs:419-461` — **new** event on logical removal, reason from
     `command.Reason` (`DocumentContracts.cs:146-152`), replay key from `command.OperationKey`.
     Today this method writes only the four version flags at `:453-456`.
   - (b) `EfQueuedCustodyProcessor.cs:594-605` (`custody_confirmed`) and `:661`
     (`audit_custody_confirmed`) — **moved** off `CaseHistory`, not duplicated.
   - (c) `EfExternalWorkStore.cs:450-456` and `:608-614` (`custody_failed`), `:481-487` and
     `:635-641` (`audit_custody_failed`) — likewise moved. **`:210` in the same file is a
     different event and is not touched.**
   Keep the existing event-type strings so the labels at `OperatorLabels.cs:389-394` stay correct;
   do not reword them. Do not add a shared helper in `src/Pegasus.Infrastructure` — the guardrail
   forbids it, so the seven blocks are inline by design.
10. **Add `tests/Pegasus.IntegrationTests/DesktopGatewayCaseTaskTests.cs` with the seven-case
    matrix for each of the eight commands**: authorized success; unauthorized (wrong role);
    version conflict — **task** version for assign/complete/cancel, **case** version for the rest,
    and for notes the conflict case is instead "a note succeeds while another actor holds the edit
    lease", which is the CASE-017 behaviour; lease conflict; lease expired; replay of the same
    `operationKey` returning the same result; validation failure. Each conflict fact asserts the
    problem `type` URI, not merely the status code.
11. **Add the wrong-version-scope fact.** A task command sent with the *case* version in the
    `taskExpectedVersion` field (and vice versa) fails as a validation or conflict problem rather
    than silently succeeding. This is the specific confusion the two names exist to prevent, and
    the acceptance criteria call it out separately.
12. **Add the upstream DOCS-012 facts through the history projection.** After a document removal,
    `GET /cases/{id}/history` (from [[GWY-007]], plan handle `DSK-03-07`, backed by
    `EfCaseQueryStore.cs:181-196`) contains exactly one new entry for that removal carrying the
    operator's reason; likewise one entry after a custody confirm and one after a custody fail, for
    both the case and the audit variants — six positive facts plus the removal. Add the negative
    fact that `CaseHistory` gains no row for any of them. **A test that queries
    `CaseWorkflowEvents` directly does not satisfy this step**: the whole defect is that a row can
    exist where no surface reads it, and that is how Release 22 shipped.
13. **Regenerate and commit the OpenAPI snapshot and the generated client.** `dotnet build` to
    emit the document, export to `openapi/pegasus-v1.json`, then `eng/api/Generate-ApiClient.ps1`
    (from [[GWY-005]], plan handle `DSK-03-05`). CI runs `git diff --exit-code` after regeneration,
    so both must be committed.
14. **Run the three verification commands** in the *Verification* section below and keep their
    output as the tier-5 evidence.
15. **Run the simplification pass** over this branch's own diff (`AGENTS.md` § Repository task
    workflow step 4) and record it under a dated `## Simplification pass` heading in this
    document. Look specifically for: a rule that crept into an endpoint handler, a second catch
    block, a helper that consolidates the seven event writes (out of scope, and the reason is
    recorded in *Approach*), and any DTO field that is not on the Core record it mirrors.

## Verification

Evidence tier from the body: **Tier 5 — Web/API/MCP caller.** It obliges evidence that the real
routes reach Core with authentication, validation, idempotency, exception translation and the
action-history actor observable. Local stack only (L-02): `WebApplicationFactory<Program>` over
LocalDB, no Azure call.

1. `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj -c Release --filter "FullyQualifiedName~DesktopGatewayCaseTaskTests"`
   — expected: all facts pass, at least seven per command, plus the wrong-scope fact, the removal
   and custody note round-trip facts, and the `CaseHistory`-gains-no-row negative. **This output is
   the `proof` document.**
2. `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj -c Release --filter "FullyQualifiedName~CaseTasksWebTests"`
   — expected: the existing Razor tests pass unchanged. A failure here means the persistence
   change altered page behaviour, which it must not.
3. `grep -rn "CaseHistoryEntity" src/Pegasus.Infrastructure/Persistence/EfQueuedCustodyProcessor.cs src/Pegasus.Infrastructure/Persistence/EfExternalWorkStore.cs`
   — expected: no remaining `custody_confirmed`, `custody_failed`, `audit_custody_confirmed` or
   `audit_custody_failed` write into `CaseHistory`. Note that `EfExternalWorkStore.cs:210` will
   still match, for a different event that is out of scope; the check is on the four event types,
   not on the absence of the type.

Observable behaviour to record alongside the command output: a task command replayed with the same
`operationKey` returns the same task record and the same version, and the case history shows one
entry, not two.

## Risks / open questions

- **The endpoint-map row for `POST /cases/{id}/notes` is stale.** It says the concurrency token is
  "`CaseMutationRequest` fields"; Core's `AddCaseNoteRequest` (`CaseNotes.cs:14-18`) has no version
  and no lease token, and `Tasks.cshtml.cs:28-32` records the CASE-017 decision that it must not.
  *Mitigation:* the body's step 5 binds the endpoint to the page handler's construction, so the
  repository wins and the notes DTO carries two fields. Recorded here so the reviewer sees a
  decision rather than an omission. Not an open question and no `open-questions` document — the
  body settles it.
- **The task version scopes are not uniform and the body's summary line simplifies them.**
  *Mitigation:* step 2 makes the per-handler table an explicit output, and step 11 tests the
  confusion case in both directions.
- **`CaseTaskVersionConflictException` fails quietly if unmapped.** *Mitigation:* step 6, plus the
  requirement in step 10 that each conflict fact asserts the problem `type` URI.
- **Seven write sites, not three files.** Missing one leaves an event invisible with nothing
  failing. *Mitigation:* step 9 enumerates every line range, and step 12 asserts each event type
  through the projection.
- **Moving an event could break an existing assertion on `CaseHistory`** (assumption A-GWY-3).
  *Mitigation:* run `grep -rn "CaseHistory" src/Pegasus.Web/ tests/` before the move. If a test
  asserts the old table, rewriting it to assert the history projection is the same correction this
  ticket is making, not a scope change.
- **Prerequisite tickets may not have merged.** `src/Pegasus.Contracts`, `src/Pegasus.Web/Api`,
  `openapi/` and `src/Pegasus.Desktop.Infrastructure` do not exist today. *Mitigation:* step 1
  stops the ticket rather than creating them. The blockers are [[GWY-001]], [[GWY-002]],
  [[GWY-004]], [[GWY-005]], [[GWY-007]], [[GWY-008]] and [[FND-031]].
- **Scope boundaries owned by named sibling tickets** — not open questions, recorded here as the
  authoring contract requires: the Evidence-tab half of upstream DOCS-012 belongs to [[FEAT-016]]
  (plan handle `DSK-05-16`); its glyph/design-authority half to [[DUI-003]] (plan handle
  `DSK-06-03`); the desktop caller to [[FEAT-006]] (plan handle `DSK-05-06`); the cross-endpoint
  authorization sweep to [[GWY-018]] (plan handle `DSK-03-18`).
- **Phase mismatch, by design.** The chase and report-evidence routes are Phase 5 in
  `endpoint-map.md` while this row is sequenced in Phase 4 (area README § 5, "08–09 (Phase 4)").
  They ship together; a Phase 5 desktop caller must not block the Phase 4 slice.

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over this branch's own
diff before the PR, recorded here under a dated heading._
