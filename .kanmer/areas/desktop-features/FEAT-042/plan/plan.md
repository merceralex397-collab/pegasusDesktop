# Plan — FEAT-042: Report finalise endpoint — register the desktop-rendered PDF into custody with the report record and audit

**Diff estimate: ~14 files, ~1,180 lines.**

Derived from the files document: the three endpoints ~260 lines (the register handler carries the
readiness re-check, the server-side hash recomputation and the two-call store-then-approve
sequence; the content handler carries `Content-Length`, `ETag`, range, `nosniff` and a safe
filename), the `Pegasus.Contracts` DTOs ~140, the desktop Reports view and view-model ~300, the
desktop upload client call ~40, contract tests ~230 for the nine facts, the integration test ~120,
view-model tests ~150, the `winapp ui` reports-script additions ~20, and ~70 across the four
documentation files. **No new table** if assumption `A-07-16-1` holds — see the risk; no Azure
write; no Worker change.

## Approach

Make finalise a **case mutation that stores an ordinary document version and then approves that
stored identity**, over the Core concepts that already exist, rather than a report subsystem. The
rejected alternative was a durable report aggregate — a report/version table with generation state,
attempts, leases and failure rows, which is what upstream DOCS-001 (board [[DOCS-001]])'s research
records as *absent* today. It is rejected because Core already models finality exactly:
`ReportApprovalSubmission` (`src/Pegasus.Core/Workflow/CaseWorkflowContracts.cs:76-79`) binds an
approval to an artifact identity and a SHA-256, and `AddCaseDocumentResult.IsReplay`
(`src/Pegasus.Core/Documents/DocumentContracts.cs:66-84`) already gives idempotent versioning — so
a second finality concept would be two answers to one question, which `AGENTS.md` § Simplicity
rails forbids. It is also rejected on cost: a new table drags a runtime-role `Grant*` migration
checked by `scripts/Test-MigrationGrants.ps1` (PLAT-035), which the ticket's own Traps names.
The one thing this ticket genuinely adds is the **server-side readiness re-check on the register
path** — the gate L-03 left behind when it moved rendering to the client.

## Governing docs

`refs`: `docs/frd/frd-11-reports-correspondence-and-reviewed-proposals.md`.

| Ref | Requirement | Meets |
| --- | --- | --- |
| FRD-11 (reports, correspondence and reviewed proposals) | Correction and finality rules; what may be regenerated and when; that an approved report is not a sent report | Steps 6 and 8 enforce the existing rules server-side and surface regeneration as **named** conditions; step 11 keeps approved and sent separate. This ticket adds a desktop **behaviour** clause to FRD-11 and restates none of its rules — FRD-11 remains the owner |

The ticket carries **`docs_todo: true`**:

> **New ADR** — ADR-0108 (isolated, non-UI WebView2 HTML→PDF rendering; gateway renderer retained
> until golden-file parity), authored by [[FND-007]] (plan handle `DSK-00-07`); ADR-0108 has two
> claimants, so see [[FND-007]]'s plan for the ownership reconciliation — [[FEAT-038]] (plan handle
> `DSK-07-12`) owns the Phase 7 content and the acceptance flip.
> **New ADR** — ADR-0107 (Box and DVLA/DVSA credentials stay behind the gateway; no long-lived
> provider secret in the package), authored by [[FND-007]]'s sibling in the reserved block; recorded
> in `docs/desktop/00-governance-and-workflow/README.md` § 3.
> This plan is written to those decisions as recorded in
> `docs/desktop/00-governance-and-workflow/README.md` § 3 and in `docs/desktop/README.md`
> § Locked decisions (L-01, L-03); if either ADR lands differently this plan is revised before
> implementation.

`refs` carries no ADR, so the programme-level authorities that bind today, with the step that
satisfies each:

| Authority | Requirement | Met by |
| --- | --- | --- |
| L-01 (index § Locked decisions) | The gateway is `Pegasus.Web` evolved in place; no new deployment unit | Steps 4, 5, 7 add routes to the existing `/api/v1` groups |
| L-03 / ADR-0108 (as recorded in 00 § 3) | The desktop renders; the gateway stores; the gateway renderer stays until golden-file parity | Steps 9 and 10 |
| ADR-0107 (as recorded in 00 § 3) | The PDF reaches Box through the gateway; no Box credential in the package | Step 5, and the package-scan Guardrail |
| Proposal § 12.5 | "Final output is uploaded to the canonical store and registered through the gateway" | Steps 4–6 |
| Proposal § 13.9 | PDF preview and finalisation; storage and retrieval of final reports; regeneration rules; audit | Steps 7, 8, 10 |
| Proposal § 23.2 | An isolated WebView2 only for a specific document render, never hosting Pegasus UI | Step 10 — Preview is a document viewer |
| Area 07 § 4, Target state, fourth bullet | Reports "finalised by uploading the PDF through the gateway into Box with the report record and audit" | Steps 5–6 |
| `docs/desktop/03-gateway-api-and-data/README.md:167` | One problem-type list; stable `urn:pegasus:problem:<slug>` URIs | Step 3's recorded slug decision — never a locally invented URN |
| `docs/desktop/03-gateway-api-and-data/endpoint-map.md:77-78` | `PerformCasework`; `yes (key)`; `CaseMutationRequest` concurrency tokens; phase 7 | Steps 4, 5, 7 |
| `docs/desktop/06-ui-design/screen-specs.md:379-386` | Generate/Preview/Finalise shape, the three AutomationIds, separate custody and sent columns, named regeneration conditions | Steps 8, 10, 11 |
| `docs/current-architecture.md:571` | Custody retry is human-only | Nothing here automates it |
| `docs/engineering.md:72-88` tier 5 | Idempotency and the action-history actor observable at the real route | Steps 5–6, verification |
| `docs/engineering.md:201-207` § Plan sizing | Diff estimate first; facts split from assumptions | This plan's first line; `research` § Facts / § Assumptions |
| `AGENTS.md` § Simplicity rails ("One list per concept") | One readiness rule, one finality concept, one problem-type list | Steps 3 and 6, and the Out-of-scope list |
| `AGENTS.md` § Repository task workflow steps 4–5 | Simplification pass before the PR; review by an agent that did not implement | Step 14; Routing |
| `HZN-001` / `board-conventions.md` § Upstream ids versus board ids | Upstream ids never written bare — **this ticket sits on the board's worst collision** | Every `upstream <ID> (board [[<board-id>]])` citation in these documents |

## Routing

Copied from the ticket body's `## Routing` block.

- **Subagent**: `pegasus-gateway-dev` — `.codex/agents/pegasus-gateway-dev.toml`; `winui-dev` —
  `.codex/agents/winui-dev.toml` for the preview and finalise surface
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `dotnet-webapi` (dotnet/skills `98f84851`)
  → `minimal-api-file-upload` (dotnet/skills `98f84851`) → `winui-design`
  (`.codex/skills/winui-design/SKILL.md`) → `run-tests`
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`,
  `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search` for streamed request
  bodies and `IFormFile` alternatives in minimal APIs)
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute`
  → `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (call `get_doc_gates <id>` before every
  move; a move crosses at most one gated boundary)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md`
  § Repository task workflow step 5)

## Steps

Refines the body's fourteen steps in the same order and with the same ownership. Nothing is
renumbered.

1. **Orient and take.** Read the plan row (`docs/desktop/07-integrations/README.md` § 5,
   `DSK-07-16`), the endpoint map's two Assessment report rows (`:77-78`), the Reports paragraph of
   the § 13.9 screen spec (`docs/desktop/06-ui-design/screen-specs.md:379-386`), and **FRD-11 in
   full** — it owns correction and finality and this ticket must not restate or contradict it. Read
   the two imported upstream tickets **by their board ids**: **upstream DOCS-001 (board
   [[DOCS-001]])** and **upstream TICK-208 (board [[DOCS-003]])**. Call `get_doc_gates FEAT-042` —
   it will report `leave-preparing` not passable while the open question stands, which is correct —
   then `take_ticket` on branch `task/dsk-07-16-report-finalise`.
2. **Record in `research` (append) exactly what "final" means today**, with the line references:
   `ReportApprovalSubmission` binds an approval to an `ArtifactIdentity` and an `ArtifactSha256`
   and the authenticated boundary assigns the actor and time
   (`src/Pegasus.Core/Workflow/CaseWorkflowContracts.cs:73-79`). **Therefore store the artifact
   first, then approve that identity — never approve bytes that were not stored.** In the same
   step, settle assumption `A-07-16-1` by reading the persistence side of
   `RecordReportApprovalAsync` (`:365`) and the document store: if the report record genuinely needs
   a new table, **stop and re-plan**, because a new table drags a runtime-role `Grant*` migration
   (`scripts/Test-MigrationGrants.ps1`, PLAT-035) that this diff estimate does not carry.
3. **Close the readiness hole L-03 opened, on both paths, over the one existing Core rule.**
   `GenerateCaseAssessmentReportDraft` and `AssessmentReportProjection.Project`
   (`src/Pegasus.Core/Reports/AssessmentReportProjection.cs:306-362`) — never a second readiness
   implementation, never a client-side check.
   - `POST /api/v1/cases/{caseId}/reports/draft` returns a projection only for a complete, accepted
     assessment; otherwise it returns the named `NotReady` reasons with **each**
     `AssessmentReadinessItem`'s `Requirement` and `WhyOutstanding` enumerated as structured fields
     in the problem response — not joined into one string, and not collapsed into a generic
     refusal. The Razor handler already names every reason (`Index.cshtml.cs:310-316`); a generic
     API refusal would be a regression against the page it replaces.
   - `POST /api/v1/cases/{caseId}/reports` **re-runs that same rule itself** before storing. It does
     not trust that the client called draft first, and it holds no client-supplied readiness token:
     a case that became not-ready between render and finalise is refused with the same named
     reasons and stores nothing.
   - **Record the problem type here, in this document, under a dated heading.** The thirteen slugs
     at `docs/desktop/03-gateway-api-and-data/README.md:167` contain **no `not-ready`**
     (`grep -rn "urn:pegasus:problem" src/ tests/` → no match; the list is specification, not code
     yet). Either map the refusal onto `urn:pegasus:problem:validation` with the structured reasons
     in an extension member, or coordinate a new slug with [[GWY-001]] (plan handle `DSK-03-01`)
     — assumption `A-07-16-4`. **Do not invent a URN locally**; that list is a one-list-per-concept
     surface.
   - **This step must not be implemented until the binding open question in this ticket's
     `open-questions` document is resolved** — the body's Guardrails say so in as many words.
4. **Implement the register endpoint's request contract and hash verification.**
   `POST /api/v1/cases/{caseId}/reports` accepts the rendered PDF as a **streamed** body plus
   `fileName`, `sha256`, `pageCount`, `templateVersion`, `engineVersion`, `expectedVersion`,
   `editLeaseToken` and `operationKey`. Compute the SHA-256 of the received bytes **on the server**
   and compare with the client-declared `sha256`; on mismatch return
   `urn:pegasus:problem:validation` and **store nothing**. The client-side re-hash in
   `GenerateAssessmentReportDraft` (`src/Pegasus.Core/Reports/AssessmentReportRendering.cs:291-307`)
   runs on the far side of the wire once rendering is local and proves nothing about what arrived.
   Assert `templateVersion` against `AssessmentReportContract.TemplateVersion` (`:8`), the constant,
   never the literal `"rendererref1-v1"`.
5. **Store through the existing custody path**, not a special-cased blob: `IAddCaseDocument` /
   `AddCaseDocumentCommand` with `DocumentSemanticRole.EngineerReport` and
   `DocumentSource.Generated`, carrying the same `ExpectedCaseVersion`, `EditLeaseToken`,
   `OperationKey` and `Actor` (`src/Pegasus.Core/Documents/DocumentContracts.cs:66-84`). The upload
   itself goes through [[FEAT-031]] (plan handle `DSK-07-05`)'s Box broker — **no Box credential
   reaches the desktop** (ADR-0107). Reuse `AddCaseDocumentResult.IsReplay` for idempotency: a
   replayed `operationKey` returns the original report id and stores nothing new. Do not build a
   second replay mechanism.
6. **Record the report record and approval in the same operation.** Call `IRecordCaseReportApproval`
   (`src/Pegasus.Core/Workflow/CaseWorkflowContracts.cs:420`) with the **stored** artifact's
   identity and hash, letting `CaseLifecycleRules.ValidateReportApproval`
   (`src/Pegasus.Core/Lifecycle/CaseLifecycle.cs:448`) enforce the rules. **Design around the
   lifecycle guard rather than discovering it**: `RecordCaseReportApproval.ExecuteAsync`
   (`:160-180`) refuses unless the case state is `ReportPreparation` **or**
   `HasOperationAsync` already knows the operation key (`:168-173`, "A report can be approved only
   while report preparation is active."). That `HasOperationAsync` branch is the only way a replay
   succeeds after the case has moved on, so step 12's replay test must drive it deliberately, not
   by luck. Add no second finality concept — no "final" flag on the document version; FRD-11 owns
   finality and the approval *is* it.
7. **Implement `GET /api/v1/cases/{caseId}/reports/{reportId}/content`** over the document content
   store with `Content-Length`, `ETag`, range support, `X-Content-Type-Options: nosniff` and a safe
   filename — the **same** guarantees as the existing document download endpoint, reached through
   the same port rather than a parallel reader.
8. **Surface regeneration rules as named conditions, not a disabled button.** The case's reports
   section response states, per FRD-11, whether regeneration is permitted and, when it is not,
   **why not** — as a named condition the desktop renders verbatim. The desktop does not compute it.
   This is `screen-specs.md:382-383`'s "enabled/disabled named conditions" made server-side.
9. **Keep the gateway renderer reachable behind its flag.**
   `POST /api/v1/cases/{caseId}/reports/draft` continues to return gateway-rendered bytes until
   [[FEAT-041]] (plan handle `DSK-07-15`)'s results table signs parity off. **Do not remove
   `AddPegasusReportRendering`** (`src/Pegasus.Infrastructure/DependencyInjection.cs:446`).
   **Record the flag name and who may flip it in this document** under a dated heading —
   [[FEAT-040]] (plan handle `DSK-07-14`) step 10 and [[FEAT-038]] step 9 both cite the same name,
   so it is written once, here or there, whichever lands first; cite the other if it already has.
10. **Build the desktop Reports flow in `src/Pegasus.Desktop`** ([[FND-030]], plan handle
    `DSK-02-05`): Generate — local render via [[FEAT-040]] with progress in the status bar and
    cancel; Preview — an **in-app PDF document viewer**, never Pegasus UI in a WebView (proposal
    § 23.2, ADR-0108, `screen-specs.md:380-381`; assumption `A-07-16-5`); Finalise — a reasoned,
    idempotent command that uploads and registers. AutomationIds exactly as the screen spec fixes
    them (`:384-386`): `Case.Reports.Generate`, `Case.Reports.Preview`, `Case.Reports.Send`. Render
    the server's named `NotReady` reasons as they arrive; **the desktop never decides readiness for
    itself.**
11. **Show issued versions with custody state and sent evidence as separate columns**, reading the
    append-only issued-version-to-Sent-evidence association from **upstream TICK-208 (board
    [[DOCS-003]])**. An approved report is not a sent report —
    `CaseWorkflowContracts.cs:63` says so in the code, and only retained Sent evidence proves a send
    ([[FEAT-037]], plan handle `DSK-07-11`). **While Core still carries one `ReportApprovalId` and
    one `ReportSentEvidenceId` per case this column pair cannot be honest, so it does not ship ahead
    of that ticket.** Sequencing recorded: board [[DOCS-003]] is already imported and must be
    **found by board id, not created** ([[FEAT-043]] (plan handle `DSK-07-17`) step 7); if it has
    not landed, this ticket ships steps 1–10 and 12–14 and holds step 11.
12. **Contract tests** in `tests/Pegasus.Api.ContractTests` ([[TEST-001]] (plan handle `DSK-08-01`),
    [[GWY-004]] (plan handle `DSK-03-04`)) — nine facts: success stores exactly one document version
    and one approval; a not-ready assessment refused by **both** the draft and the register endpoint
    with the named reasons, storing nothing; hash mismatch stores nothing; a replayed key returns
    the original id with no second version (driving the `HasOperationAsync` branch of
    `CaseLifecycle.cs:168-173` deliberately); unauthorised actor refused; stale `expectedVersion`
    → `409`; regeneration refused when FRD-11 forbids it, with the named condition returned; the
    content endpoint honours range and `ETag`. Match the refusal vocabulary that
    `src/Pegasus.Web/Mcp/AutomationMcpErrors.cs` establishes as ported by [[GWY-002]] (plan handle
    `DSK-03-02`) rather than inventing codes.
13. **Integration test** following `tests/Pegasus.IntegrationTests/CaseReportApprovalWebTests.cs`
    (236 lines) and the custody durability tests: a finalise on the local stack (L-02) produces
    **one** Box-custody document version, **one** report record, **one** approval row and **one**
    action-history entry — and an interrupted upload produces **none** of them, which is the
    observable form of assumption `A-07-16-2`. Reuse the not-ready arrangement from
    `tests/Pegasus.IntegrationTests/Reports/AssessmentReportDraftWebTests.cs` (259 lines) rather
    than building a second one.
14. **View-model tests, UI script, simplification pass, PR.** View-model tests
    ([[TEST-004]] (plan handle `DSK-08-04`), [[FND-038]] (plan handle `DSK-02-13`)) for
    generate/preview/finalise state, cancel during render, the not-ready reasons rendering as named
    requirements, and the disabled-with-named-reason regeneration case. Add the finalise assertions
    to the `winapp ui` reports script from [[TEST-008]] (plan handle `DSK-08-08`), on the harness
    [[TEST-006]] (plan handle `DSK-08-06`) creates. Regenerate `openapi/pegasus-v1.json`
    ([[GWY-004]]) and the Kiota client ([[GWY-005]], plan handle `DSK-03-05`). Then run the
    simplification pass over this branch's own diff, record it under a dated
    `## Simplification pass` heading below, and open the PR into `dev`.

## Verification

Evidence tier from the body: **Tier 5 — Web/API/MCP caller**
(`docs/engineering.md:72-88` item 5: actual routes reach Core; authentication, antiforgery,
validation, scope, **idempotency**, exception translation and the **action-history actor** are
observable). `proof` is the captured output of:

- `dotnet test ./tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj --configuration Release`
  — expected: the store-once, not-ready-on-**both**-paths, hash-mismatch, replay, authorization,
  conflict, regeneration-named-condition and content-range/`ETag` facts pass. The nine assertion
  names are the evidence, not the summary line.
- `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --filter "Category!=Corpus&Category!=Browser"`
  — expected: one version, one report record, one approval, one audit row; the interrupted-upload
  fact leaves none; a not-ready case leaves none; and the pre-existing
  `CaseReportApprovalWebTests` facts stay green.
- `dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release`
  — expected: generate, preview, cancel, not-ready-reasons and finalise-state facts pass.
- `pwsh ./tests/Pegasus.Desktop.UITests/ui-tests.ps1 -AppPid <pid> -Script reports`
  — expected: generate, preview and finalise assertions pass; screenshots attached.
- `git diff --exit-code openapi/pegasus-v1.json` after regeneration — expected clean, proving the
  committed snapshot matches the generated document.
- `git diff --stat origin/dev -- src/Pegasus.Worker` — expected **empty output**. Nothing in this
  ticket touches unattended work.
- `grep -rn "AddPegasusReportRendering" src/` — expected: still present at
  `src/Pegasus.Infrastructure/DependencyInjection.cs:446`. The observable form of the Guardrail
  "must not remove the gateway renderer registration".
- **A secret scan of the desktop package** showing no Box credential — the observable form of
  ADR-0107 and the Guardrail.

Behaviour to observe on the local stack (L-02): finalising the same case twice with one
`operationKey` produces one Box document version and one approval row; a case edited into a
not-ready state between render and finalise is refused at the **register** call with the same named
reasons the draft call gives.

## Risks / open questions

- **Risk — a new report table is needed after all** (`A-07-16-1` fails). It drags a runtime-role
  `Grant*` migration checked by `scripts/Test-MigrationGrants.ps1` (PLAT-035), which this diff
  estimate does not carry. Mitigation: step 2 settles it by reading the persistence side **before**
  any endpoint exists, and the instruction on failure is to stop and re-plan, not to add the table
  quietly.
- **Risk — the lifecycle guard refuses a legitimate finalise.** `CaseLifecycle.cs:168-173` permits
  approval only in `ReportPreparation` or on a known operation key. Mitigation: `A-07-16-3` is
  settled by the integration test finalising from a realistic case, and the replay test drives the
  `HasOperationAsync` branch deliberately.
- **Risk — approving bytes that were not stored.** The `ArtifactIdentity` binding is the whole
  point of `ReportApprovalSubmission`. Mitigation: step 2 fixes the order in this document before
  code exists, and step 13 asserts that an interruption leaves neither artefact nor approval.
- **Risk — a locally invented `not-ready` problem URN.** Mitigation: step 3 records the chosen slug
  in this document and coordinates any addition with [[GWY-001]], whose list is the single surface.
- **Risk — the readiness refusal is collapsed into one string** when ported from
  `Index.cshtml.cs:310-316`. Mitigation: step 3 requires structured `Requirement`/`WhyOutstanding`
  fields and step 12 asserts the named reasons on **both** paths.
- **Risk — the gateway renderer registration is removed as "dead code".** Mitigation: the explicit
  `grep` in verification, and step 9's instruction not to touch
  `DependencyInjection.cs:446`.
- **Risk — preview is built on a WebView because one is already in the solution** for
  [[FEAT-040]]'s renderer. Mitigation: `screen-specs.md:380-381` and [[FND-037]] (plan handle
  `DSK-02-12`)'s no-WebView architecture test with its single approved exception; `A-07-16-5`.
- **Risk — an id-namespace error in this area specifically.** Board `DOCS-003` is upstream TICK-208;
  upstream `DOCS-003` is an unrelated post-alpha RPT-04 gate with no fork ticket; board `DOCS-002`
  is upstream TICK-018; board `DOCS-001` matches its upstream id by **coincidence**. Mitigation:
  every citation in these documents is written `upstream <ID> (board [[<board-id>]])`, per
  `HZN-001` / `board-conventions.md`.
- **Scope boundary, not an open question** — the append-only issued-version ledger is **upstream
  TICK-208 (board [[DOCS-003]])**'s, already imported, to be found by board id and not created; the
  renderer is [[FEAT-040]]'s; the parity gate is [[FEAT-041]]'s; the ADR file and its acceptance
  flip are [[FEAT-038]]'s; the custody upload path is [[FEAT-031]]'s; the outbound send seam is
  [[FEAT-037]]'s; the slice is [[FEAT-018]] (plan handle `DSK-05-18`)'s.
- **One open question, and it is binding.** The ticket body's Guardrails require it to be resolved
  and recorded in this ticket's `open-questions` document **before step 3 is implemented**:
  automatic versus operator-initiated report generation as the **desktop** contract. It is recorded
  there as an unticked item, which correctly holds `leave-preparing`, `enter-review` and
  `enter-done` shut until an operator answers. It does **not** gate `leave-backlog`. The body
  forbids resolving it by inventing a hybrid or by acting on the upstream wording alone, so it is
  not resolved here.

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over this branch's own
diff before the PR, recorded here under a dated heading._
