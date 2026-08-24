# Plan — GWY-010: Intake (received items) endpoints: detail, named commands, case linking and byte reads

**Diff estimate: ~13 files, ~3,250 lines** (~1,400 of them generated).

Derived from the files document, measured rather than asserted:

| File | Lines | How the number was reached |
| --- | --- | --- |
| `src/Pegasus.Contracts/Intake/IntakeResponses.cs` (new) | ~300 | `IntakeReceipt` has 35 positional members plus 4 derived (`IntakeContracts.cs:366-436`); the nested shapes `IntakeEvidence`, `InstructionReviewField`, `InstructionDraft`, `IntakeAssetRecord`, `ScannedPdfOcrCandidate`, `IntakeSourceIdentity` each need a DTO; plus `ReceivedItemSummaryDto` (11 members, `:511-521`) and `ReceivedItemPageDto` (4 members + `TotalPages`, `:744-752`) |
| `src/Pegasus.Contracts/Intake/IntakeCommands.cs` (new) | ~160 | 9 command records: 6 intake commands at 3–4 members each, 3 case-association commands at 5–6 members each (receipt version + case `expectedVersion` + `editLeaseToken` + `operationKey` + `reason`) |
| `src/Pegasus.Web/Api/IntakeEndpoints.cs` (new) | ~430 | 2 read routes (~35 lines each with paging validation and `ETag`), 9 command routes (~25 each), 3 byte routes (~50 each — headers, range, filename rule, media-type gate), plus the group builder |
| `/api/v1` problem mapper (edit, from [[GWY-002]], plan handle `DSK-03-02`) | +12 | One `IntakeArtifactIntegrityException` arm |
| `tests/Pegasus.IntegrationTests/DesktopGatewayIntakeTests.cs` (new) | ~950 | 9 commands × 7 cases = 63; paging bounds 3 (`pageSize=101`, `page=0`, undefined decision); detail `version` + `ETag` 2; byte facts 6 × 3 routes = 18. 86 facts at ~11 lines each after helpers borrowed from `IntakeWebTestSupport.cs` (866 lines) |
| `openapi/pegasus-v1.json` (regenerated, from [[GWY-004]], plan handle `DSK-03-04`) | ~+600 | 14 paths + ~20 schemas, the detail schema being the largest on the board so far |
| `src/Pegasus.Desktop.Infrastructure/Api/Generated/**` (regenerated, from [[FND-031]] / [[GWY-005]]) | ~+800, ~6 files | Kiota emits a request-builder per path segment plus a model per schema |

## Approach

Project the ten `Intake/*` handlers onto fourteen named `/api/v1` routes on one `received`
sub-group, mapping Core's own refusals into problem documents rather than re-implementing them, and
**sequence the snapshot commit behind the three imported intake tickets that change what the frozen
vocabulary must carry**.

The rejected alternative was to **commit the snapshot with the routes and let the three intake
tickets amend it later**. It is the obvious shape — endpoints and snapshot in one branch — and it
is wrong here for a mechanical reason rather than a stylistic one: [[GWY-004]] snapshot-tests
`openapi/pegasus-v1.json` and [[GWY-005]] generates and commits a client from it, so the moment
this branch merges, the intake vocabulary is a published contract. [[INTK-001]]'s decision-code
collapse, [[INTK-004]]'s `reevaluate` refusal shape and [[INTK-006]]'s new state would each stop
being an addition and become a versioned-contract change against a client already in the pilot
ring — and the area plan § 7 records that "removing a field is a contract-test failure by design".
Splitting the branch so the routes land first and the snapshot second was also rejected: the
snapshot test would fail on the intermediate commit, so the two must land together and the
sequencing must be a gate on the whole ticket rather than on one step.

The second rejected alternative was to **implement `?queue&state` as the endpoint-map writes it**,
by filtering `ListIntake`'s paged result in Web. Rejected because `ListIntakeQuery`
(`IntakeContracts.cs:738-742`) accepts only `IntakeDecision?`, the guardrail forbids touching
`src/Pegasus.Core/Intake/**`, and post-filtering a paged result would make
`IntakeListPage.TotalCount` (`:748`) disagree with what was returned. The endpoint exposes the
decision filter alone and records the divergence.

## Governing docs

The ticket's `refs` is **empty** and it carries `docs_todo: true` (confirmed in
`get_doc_gates GWY-010`: the `leave-backlog` `governing-doc` requirement shows `satisfied: true` on
that basis). So:

> **New ADR** — ADR-0103 (gateway, evolved `Pegasus.Web`, never direct database access from
> workstations), authored by [[FND-005]] (plan handle `DSK-00-05`).
> This plan is written to the decision as recorded in
> `docs/desktop/00-governance-and-workflow/README.md` § 3 (ADR table) and locked decision L-01 in
> `docs/desktop/README.md`; if the ADR lands differently this plan is revised before
> implementation.
> **ADR-0106** (Graph intake worker stays central — unattended execution, protected credentials)
> and **ADR-0107** (provider credentials stay behind the gateway) also bind and are likewise
> authored by [[FND-005]]; they are what the research document's *Execution placement* answers
> "yes" against for questions 2 and 3.

Because `refs` is empty, the programme-level authorities that bind today, with the step that
satisfies each:

| Authority | Requirement | Met by |
| --- | --- | --- |
| Proposal § 10.2 (API style) | Explicit named commands, never a dispatcher | Step 4 — fourteen named routes |
| Proposal § 12.1 (Graph intake) | The intake worker stays central and unattended; the desktop reads what it produced | Steps 3–4; nothing is placed on the desktop |
| Proposal § 13.4 (Intake) | Intake triage is a primary operator workflow the desktop must carry | Steps 3–8 |
| L-01 | Gateway is `Pegasus.Web` evolved in place | Step 4 |
| L-02 | Evidence from the local stack only; Azurite stands in for storage | Steps 9–10, 12 |
| Plan 03 § 3 row *Bytes & uploads* | Byte endpoints stream with `Content-Length`, range support and `ETag` | Step 6 |
| Plan 03 § 3 row *Compression* | Compression is JSON and problem responses only; bytes excluded | Step 7 |
| Plan 03 § 3 row *Paging/filter/sort* | `pageSize` bounded, newest-first default, filters explicit | Step 5 — with Core's lower cap of 100 winning |
| Plan 03 § 3 row *Idempotency* | `operationKey` an explicit body field | Step 3 |
| Plan 03 § 3 row *Concurrency* | `expectedVersion` and `editLeaseToken` explicit body fields | Steps 3 and 8 |
| Plan 03 § 7 trap *Two policy engines* | Rules stay in Core; an endpoint filter is a fail-fast boundary | Steps 5 and 13 |
| Plan 03 § 7 trap *Upstream drift* | Start intake work after the first upstream sync | Step 1 |
| Plan 03 § 7 trap *Pilot ring compatibility* | Contract changes must be additive until the minimum client version advances | Step 11 — the whole point of the sequencing |
| ADR-0018 / INTK-029 (recorded in `IntakeContracts.cs:417-435`) | No surface re-derives association provenance from raw fields | Step 3 |
| `AGENTS.md` § Repository task workflow step 4 | A recorded simplification pass over the branch diff | Step 13 |

## Routing

Copied from the ticket body's `## Routing` block.

- **Subagent**: `pegasus-gateway-dev` — `.codex/agents/pegasus-gateway-dev.toml`
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `dotnet-webapi` (`dotnet/skills`
  `98f84851`, plugin `dotnet-aspnetcore`) → `minimal-api-file-upload` (`dotnet/skills` `98f84851`,
  plugin `dotnet-aspnetcore`)
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`,
  `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search` for
  `ASP.NET Core Results.File range processing enableRangeProcessing`)
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute`
  → `kanmer-review` → `kanmer-verify` → `kanmer-closeout`
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement
  (`AGENTS.md` § Repository task workflow step 5)

## Steps

These refine the ticket body's twelve implementation steps in the same order and with the same
ownership; step 13 splits the body's step 12 so the test run and the simplification pass are
separately tickable.

1. **Orient, and check the sync.** Read the five endpoint rows in
   `docs/desktop/03-gateway-api-and-data/endpoint-map.md` § Intake, the area README § 3 rows
   *Bytes & uploads* and *Compression*, and the bodies of [[INTK-001]] (upstream `INTK-002`),
   [[INTK-004]] (upstream `INTK-027`) and [[INTK-006]] (upstream `INTK-032`). Then
   `get_doc_gates GWY-010` and `take_ticket`. Confirm [[FND-023]] (plan handle `DSK-01-10`, the
   first one-way upstream sync) has landed — upstream `main` is ahead on intake paths (upstream
   `PLAT-039`, upstream `DOCS-010`) — and confirm that `src/Pegasus.Contracts/`,
   `src/Pegasus.Web/Api/`, `openapi/` and `src/Pegasus.Desktop.Infrastructure/Api/Generated/`
   exist. If any prerequisite is missing, stop and say which.
2. **Read the four page models in full and tabulate them.** Per handler: the Core command or port,
   the version it expects, whether `reason` is required, and — for the three byte handlers — the
   exact response headers set today. The starting table is in the research document's *Current
   behaviour*; the output of this step is the same table with the Core request record names filled
   in. Note the two byte behaviours that change: `IntakeArtifactIntegrityException` becomes a
   problem document rather than 409 `text/plain` (`Source.cshtml.cs:40-49`), and the filename rule
   refuses rather than sanitises.
3. **Add `src/Pegasus.Contracts/Intake/` response and command DTOs.** The detail DTO **projects**
   `IntakeReceipt`'s derived members (`CurrentCaseId` `:407`, `AssociationWasStaffDecision` `:417`,
   `UnlinkCancelsCase` `:429`, `CurrentCaseReference` `:432`) rather than recomputing them from
   `AcceptedCaseId`/`ManualLinkedCaseId` — those comments cite INTK-029 and say so.
   **The decision vocabulary is the `IntakeDecision` enum (`IntakeContracts.cs:77-86`), mapped
   through whatever single table [[INTK-001]] establishes — not hand-copied here.** Three copies
   already exist and disagree: `EfIntakeReceiptStore.ParseDecision` (`:1241-1252`, seven codes,
   throws), `EfOperationsStore.MapIntakeState` (`:563-569`, four codes, silently `Unknown`),
   `IntakeMcpTools` (`:82-87`, six codes). If [[INTK-001]] has not landed, record in this document
   which enumeration you read and that a fourth copy was deliberately avoided. Leave room for
   [[INTK-006]]'s new state member beside the OCR-required state, and for [[INTK-004]]'s
   `reevaluate` failure shape.
4. **Add `src/Pegasus.Web/Api/IntakeEndpoints.cs`** with a `received` sub-group carrying
   `.RequireStaffRight(StaffAccessRight.PerformCasework)` from [[GWY-003]] (plan handle
   `DSK-03-03`). Fourteen named routes: `GET /received`, `GET /received/{id}`; the six commands
   `retry-allocation`, `block`, `reevaluate`, `correct-draft`, `dismiss-suggestion`,
   `register-image-intake`; the three case-association commands `case-lease/claim`, `link-case`,
   `reverse-case-link`; the three byte routes `source`, `assets/{aid}`, `images/{iid}`.
5. **Cap `pageSize` at 100 and map Core's refusals.** 100 is `ListIntake`'s own bound
   (`IntakeQueryUseCases.cs:23-28`), and it is lower than the board's global 200; the lower one
   wins. Map all three `ArgumentOutOfRangeException` throws (`:17-22` page, `:23-28` page size,
   `:29-34` undefined decision) to `urn:pegasus:problem:validation` rather than letting them become
   500s. **Expose the decision filter only.** The endpoint-map's `?queue&state` cannot be honoured:
   `ListIntakeQuery` (`IntakeContracts.cs:738-742`) accepts one filter, the guardrail forbids
   touching Core, and post-filtering a paged result would break `IntakeListPage.TotalCount`
   (`:748`). Record the divergence here rather than dropping the parameter silently.
6. **Implement the three byte routes.** `Results.File` / `TypedResults.File` with range processing
   enabled; set `Content-Length`, a weak `ETag`, `X-Content-Type-Options: nosniff`, and a
   `Content-Disposition` filename validated by the `AutomationMcpErrors.RequireFileName` rule
   (`:127-140`: at most 255 characters, equal to its own `Path.GetFileName`, not `.` or `..`).
   Keep the SHA-256 validation the existing handlers rely on, and carry across the asset route's
   media-type gate (`Asset.cshtml.cs:39-44` — a non-`image/*` asset is `NotFound`) and its
   `Cache-Control: private, no-store`. Add the `IntakeArtifactIntegrityException` arm to the
   problem mapper from [[GWY-002]], not a catch here.
7. **Exempt byte responses from response compression.** The area README § 3 row *Compression*
   limits compression to JSON and problem responses. [[GWY-017]] (plan handle `DSK-03-17`) adds the
   middleware; record the exemption requirement here so it survives until then.
8. **Wire the three case-association commands** to `IAcquireCaseEditLease`, `ILinkIntake`
   (`src/Pegasus.Core/Intake/DurableIntake.cs:1106`) and `IReverseIntakeLink` exactly as
   `Details.cshtml.cs:240`, `:274` and `:310` do, carrying **both** the receipt `expectedVersion`
   and the case `expectedVersion` + `editLeaseToken` in the body, per the endpoint-map row.
9. **Add `tests/Pegasus.IntegrationTests/DesktopGatewayIntakeTests.cs`**, reusing
   `IntakeWebTestSupport.cs` (866 lines) and the receipts in `MultiFormatIntakeWebTests.cs` (1,429
   lines). Seven-case matrix per command; paging bounds (`pageSize=101` refused as `validation`,
   `page=0` refused, an undefined decision refused); the detail's `version` and `ETag`.
10. **Add the byte-endpoint facts**, six per route: `Content-Length` present; `ETag` present; a
    `Range` request returns `206` with the correct slice; `X-Content-Type-Options: nosniff`
    present; a hostile filename is **refused** (not merely cleaned — the deliberate change from
    `SafeFileName` to `RequireFileName`); an unauthorised caller gets the `not-authorized` problem
    before any storage call is made. Add the asset route's media-type fact: a non-image asset id
    returns not-found rather than bytes.
11. **Record the three prerequisites, then regenerate and commit the snapshot.** Before running the
    export, write one dated line each into the *Pre-snapshot record* section below for
    [[INTK-001]], [[INTK-004]] and [[INTK-006]] — **landed** (with the commit or PR), or
    **deferred** (with the reason and who decided). A deferral is a legitimate outcome:
    [[INTK-006]] carries `docs_todo: true` and the `needs-operator` label because its
    operator-visible wording is still an open operator decision. Silence is not. Then regenerate
    `openapi/pegasus-v1.json` and run `eng/api/Generate-ApiClient.ps1` (from [[GWY-005]]), and
    commit both.
12. **Run the verification commands** in the section below and keep their output as the tier-5
    evidence.
13. **Run the simplification pass** over this branch's diff (`AGENTS.md` § Repository task workflow
    step 4) and record it under a dated `## Simplification pass` heading here. Look specifically
    for: a decision-code list written by hand (the fourth copy), a derived member recomputed in the
    DTO mapper, a rule that crept into the endpoint filter, a `queue`/`state` parameter that ended
    up filtering in Web, and any byte route that lost the media-type gate.

## Verification

Evidence tier from the body: **Tier 5 — Web/API/MCP caller.** It obliges evidence that the real
routes reach Core with authentication, validation, idempotency and exception translation
observable, and that byte paths enforce their safety headers on the wire. Local stack only (L-02);
Azurite stands in for storage.

1. `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj -c Release --filter "FullyQualifiedName~DesktopGatewayIntakeTests"`
   — expected: all facts pass, including the `206` range fact and the `pageSize=101` refusal.
   **This output is the `proof` document.**
2. `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj -c Release --filter "FullyQualifiedName~MultiFormatIntakeWebTests"`
   — expected: the existing intake web tests pass unchanged.
3. **The *Pre-snapshot record* section below** — expected: one dated line each for [[INTK-001]],
   [[INTK-004]] and [[INTK-006]] stating landed or deferred-with-reason, written **before** the
   step 11 export was run. This is a document check, not a command, and it is a named verification
   in the ticket body.

Observable behaviour to record alongside: a hostile `Content-Disposition` filename is refused with
a `validation` problem rather than served with a cleaned name, and an asset id whose content is not
`image/*` returns not-found rather than bytes.

## Pre-snapshot record

_Written at implementation time, before step 11's export runs. One dated line each._

- [[INTK-001]] (upstream `INTK-002`, one decision-code table and the unknown-code behaviour) —
  _not yet recorded._
- [[INTK-004]] (upstream `INTK-027`, the `reevaluate` refusal contract) — _not yet recorded._
- [[INTK-006]] (upstream `INTK-032`, the unreadable-third-party-report state) —
  _not yet recorded._

## Risks / open questions

- **The freeze is the ticket's real risk.** Committing the snapshot before the three imported
  tickets settle publishes one of two disagreeing decision vocabularies as the contract.
  *Mitigation:* step 11's *Pre-snapshot record*, which is also a named verification, so a reviewer
  can see whether it was written before or after the export.
- **`?queue&state` is not implementable as written** — `ListIntakeQuery` accepts one filter and the
  guardrail forbids touching Core. *Mitigation:* step 5 exposes the decision filter and records
  the divergence. Not an open question: the guardrail settles it, and taking the default rather
  than asking is what the authoring contract requires.
- **Three page-size caps are in play** — 200 (board convention), 100 (`ListIntake`), 50
  (`IntakeMcpTools`, MCP only). *Mitigation:* step 5 states which one binds and tests the boundary
  at 101.
- **`ArgumentOutOfRangeException` becomes a 500 if unmapped.** *Mitigation:* step 5 maps all three
  throws; step 9 tests each.
- **Re-deriving `UnlinkCancelsCase` and friends in the DTO mapper** would reproduce exactly what
  INTK-029 removed. *Mitigation:* step 3 says project, and step 13's pass looks for it.
- **The byte routes change two behaviours deliberately** (problem document instead of 409
  `text/plain`; refuse instead of sanitise a filename). *Mitigation:* both have facts in step 10,
  and both are stated in step 2's output so a reviewer sees them as intended.
- **Upstream drift on intake paths.** *Mitigation:* step 1 checks [[FND-023]] first.
- **Scope boundaries owned by named sibling tickets** — not open questions: the upload side and the
  upload-status payload belong to [[GWY-011]] (plan handle `DSK-03-11`); the desktop caller to
  [[FEAT-009]] (plan handle `DSK-05-09`); the malformed-upload hardening to [[PLAT-006]] (plan
  handle `DSK-10-06`); the compression middleware to [[GWY-017]]; the authorization sweep to
  [[GWY-018]] (plan handle `DSK-03-18`).

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over this branch's own
diff before the PR, recorded here under a dated heading._
