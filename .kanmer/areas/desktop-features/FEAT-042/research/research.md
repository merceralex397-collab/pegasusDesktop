# Research — FEAT-042: Report finalise — registering a desktop-rendered PDF into custody

## Question

What does "final" already mean in this codebase, what enforces it, and where exactly did L-03 open
a hole by moving rendering to the client without moving the readiness gate with it — so that the
finalise endpoint binds to the existing Core concepts rather than inventing a parallel notion of a
final report?

## Current behaviour

The web application **renders a report draft and never stores it**.

- `src/Pegasus.Web/Pages/Cases/Assessment/Index.cshtml.cs` (740 lines) —
  `OnPostGenerateReportDraftAsync` at `:277`. Its own XML summary (`:270-276`) says readiness "is
  decided by `AssessmentReportProjection`, the same readiness rail rendered on this page; a case
  that is not ready returns to the page with every outstanding reason named rather than throwing".
  The handler checks the actor (`:282-285`), validates the operation key (`:286-290`), calls
  `generateReportDraft.ExecuteAsync` (`:294`), catches `ReportRenderRejectedException` and three
  I/O-shaped exceptions into one safe message (`:295-303`), and then switches on the outcome: on
  `NotReady` it joins every `reason.Requirement`/`reason.WhyOutstanding` pair into `TempData`
  (`:310-316`); otherwise it returns
  `File(assessmentPdf.Pdf, "application/pdf", assessmentPdf.SuggestedFileName)` (`:318-319`).
  **The bytes go straight to the browser. Nothing is persisted, nothing is approved, no custody
  record is written.** The fee note is not even returned.
- `OnPostSendAsync` at `:583` is a different concern — the outbound seam owned by [[FEAT-037]]
  (plan handle `DSK-07-11`).

**Parity-matrix row.** `docs/desktop/01-inventory-and-parity/parity-matrix.md:60` — **`PAR-15`**,
"13.9 Assessment and reporting", FRD-11/FRD-06, entry point `Cases/Assessment/Index.cshtml.cs`
(740). Its "API/data dependency" column already names "report draft via `IAssessmentReportRenderer`
(Playwright)" and its native-target column says "Assessment tab + report preview/finalise (Phase 7;
rendering local via WebView2 per L-03)". The indicative endpoints it lists include
`~POST .../reports` (register final). The matrix holds **46** `PAR-` rows
(`grep -c '^| PAR-' …/parity-matrix.md` → 46); `PAR-15` is the only one this ticket touches.

## Findings

- **Core already models "final" precisely, and it is an approval bound to an artifact identity and
  a hash — not a document flag.** `src/Pegasus.Core/Workflow/CaseWorkflowContracts.cs:65-70`
  defines `ReportApprovalEvidence(ApprovalId, ArtifactIdentity, ArtifactSha256, ApprovedBy,
  ApprovedAtUtc)`, and its summary at `:63` is one sentence long and load-bearing:
  **"A human approval of one immutable report artifact. It does not claim the report was sent."**
  `ReportApprovalSubmission` at `:76-79` carries only `ApprovalId`, `ArtifactIdentity` and
  `ArtifactSha256`; its summary at `:73-75` says the approving actor and approval time "are
  assigned by the authenticated mutation boundary" — so the client supplies **what** was approved
  and never **who** or **when**.
  - Consequence the ticket body draws in step 2 and this research confirms: the artifact must be
    **stored first**, then that stored identity approved. Approving bytes that were never stored
    leaves an `ArtifactIdentity` pointing at nothing.
- **The approval is gated on lifecycle state, and the replay path is the only way past it.**
  `src/Pegasus.Core/Lifecycle/CaseLifecycle.cs:160-180` — `RecordCaseReportApproval.ExecuteAsync`
  calls `CaseLifecycleRules.ValidateReportApproval(request)` (`:167`), then refuses unless
  `current.State == CaseLifecycleState.ReportPreparation` **or**
  `_store.HasOperationAsync(request.CaseId, request.OperationKey, …)` already knows the key, with
  the message "A report can be approved only while report preparation is active." (`:168-173`).
  **This is a constraint the plan must design around, not discover in a failing test**: a
  finalise-then-replay after the case has moved on works only because the operation key was already
  recorded. `ValidateReportApproval` itself is at `:448` and requires a non-empty `ApprovalId`.
- **Readiness has exactly one owner, and L-03 moved the renderer out from behind it.**
  `src/Pegasus.Core/Reports/AssessmentReportProjection.cs` (362 lines):
  `AssessmentReportDraftPreparation` at `:306-310` with `CanGenerate => Reasons.Count == 0`;
  the outcome enum `Generated` / `NotReady` / `NotFound` and
  `GenerateCaseAssessmentReportDraftResult` at `:312-322`; and `GenerateCaseAssessmentReportDraft`
  at `:331-362`, whose `ExecuteAsync` returns `NotFound` when the source yields nothing, returns
  `NotReady` with `projected.Reasons` unless `AssessmentReportProjection.Project(input).IsReady`,
  and only then renders. Its summary (`:323-330`) records that authorisation "is inherited from the
  composed `IAssessmentReportProjectionSource` (the same `StaffAuthorization` check the case-detail
  query already performs) — nothing new is invented here."
  - **The hole**: today rendering happens *inside* `ExecuteAsync`, so readiness and rendering are
    inseparable. Once the desktop renders locally, a client can produce a PDF for any case it can
    read and post it. Without a server-side re-check on the register path, an incomplete or
    unaccepted assessment gets a registered report. This is what the body means by "L-03 moved the
    rendering and not the gate", and it is why step 3 requires the check on **both** paths.
- **The custody path already gives idempotency and versioning for free.**
  `src/Pegasus.Core/Documents/DocumentContracts.cs:66-84`: `AddCaseDocumentCommand` carries
  `FileName`, `MediaType`, `Content`, `SemanticRole`, `Source`, `SourceOccurrenceIdentity`,
  `Actor`, `OperationKey`, `ExpectedCaseVersion` and `EditLeaseToken`; `AddCaseDocumentResult`
  returns `(Occurrence, Version, IsReplay)`. `IsReplay` is the idempotency signal the body's step 5
  reuses — there is no need for a bespoke replay table.
- **The artifact provenance the register call carries already exists.**
  `src/Pegasus.Core/Reports/AssessmentReportRendering.cs:272-278` —
  `RenderedReportArtifact(SuggestedFileName, Pdf, PageCount, Sha256, TemplateVersion,
  EngineVersion)`; `AssessmentReportContract.TemplateVersion` is `"rendererref1-v1"` (`:8`).
  `GenerateAssessmentReportDraft` (`:291-307`) already re-hashes and throws
  `ReportRenderRejectedException` (`:312`) on mismatch — client-side. The **server** must recompute
  the hash independently on receipt; a client-side check proves nothing about what arrived.
- **The endpoint pair is recorded as new, and the draft row already anticipates the switch.**
  `docs/desktop/03-gateway-api-and-data/endpoint-map.md:77` — `POST /cases/{id}/reports/draft`,
  replacing `OnPostGenerateReportDraftAsync`, with the Core column reading
  "`GenerateCaseAssessmentReportDraft` → `IAssessmentReportRenderer` (gateway-side until L-03
  parity; then the desktop renders and `POST /cases/{id}/reports` registers the final PDF)",
  `PerformCasework`, `yes (key)`, phase 7. `:78` — `POST /cases/{id}/reports` (register final) and
  `GET /cases/{id}/reports/{rid}/content`, Replaces column literally "— (new for L-03; today the
  web keeps the rendered draft server-side)", Returns "report id, version; bytes", phase 7.
- **The screen spec is explicit that Generate is an operator command and that Preview is not a
  WebView.** `docs/desktop/06-ui-design/screen-specs.md:371-386`, § 13.9. The Reports bullet
  (`:379-383`): "Generate report draft (local WebView2 render, L-03; progress in status bar;
  cancel), Preview (**PDF viewer in-app; the preview surface is a document viewer, not Pegasus UI
  in a WebView**), Finalise/Send (reasoned; idempotent), list of issued versions with custody and
  sent evidence shown **separately**; regeneration rules surfaced as enabled/disabled named
  conditions." AutomationIds at `:384-386`: `Case.Reports.Generate`, `Case.Reports.Preview`,
  `Case.Reports.Send`.
- **The problem-type vocabulary does not exist in `src/` yet.**
  `grep -rn "urn:pegasus:problem" src/ tests/` returns **nothing**. The slugs are specified in
  `docs/desktop/03-gateway-api-and-data/README.md:167` — `validation`, `not-authorized`,
  `version-conflict`, `lease-conflict`, `lease-expired`, `operation-conflict`,
  `client-unsupported`, `password-change-required`, `account-disabled`, `provider-unavailable`,
  `not-found`, `rate-limited`, `maintenance` — described there as a "Port of
  `AutomationMcpErrors.cs`", and are implemented by [[GWY-001]] (plan handle `DSK-03-01`) and
  [[GWY-002]] (plan handle `DSK-03-02`). `src/Pegasus.Web/Mcp/AutomationMcpErrors.cs` is the
  behavioural precedent it is ported from: it maps `StaffAuthorizationException`,
  `CaseEditLeaseExpiredException`, `CaseEditLeaseConflictException` and
  `CaseVersionConflictException` to safe messages that name the current case version, and collapses
  anything unexpected to a generic failure "so no infrastructure detail crosses the boundary"
  (`:7-16`). **There is no `not-ready` slug in that list** — see Implications.
- **The upstream evidence, cited with both namespaces as the board conventions require.**
  - **upstream DOCS-001 (board [[DOCS-001]])** — imported; the board id coincides with the upstream
    id, which `HZN-001` / `board-conventions.md` calls out as the trap in its join table. Its
    `open-questions` document records an operator selection and is **directly relevant to this
    ticket's binding open question** — see the `open-questions` document here.
  - **upstream TICK-208 (board [[DOCS-003]])** — imported; board `DOCS-003` is upstream TICK-208,
    **not** upstream `DOCS-003`, which is an unrelated post-alpha RPT-04 activation gate with no
    fork ticket. Board `DOCS-002` is upstream TICK-018 and is not involved. Confirmed against the
    join table in `HZN-001` / `board-conventions.md`.
- **Existing test precedents.** `tests/Pegasus.IntegrationTests/CaseReportApprovalWebTests.cs` (236
  lines) is the approval-path precedent step 13 follows;
  `tests/Pegasus.IntegrationTests/Reports/AssessmentReportDraftWebTests.cs` (259 lines) is the
  draft-path precedent. `tests/Pegasus.Api.ContractTests` does not exist yet — it is created by
  [[TEST-001]] (plan handle `DSK-08-01`) and [[GWY-004]] (plan handle `DSK-03-04`).

### Facts

Verified by reading the repository at fork `main` on 2026-08-24.

| Fact | Source |
| --- | --- |
| The draft handler returns bytes and stores nothing | `src/Pegasus.Web/Pages/Cases/Assessment/Index.cshtml.cs:318-319`; file is 740 lines |
| Readiness reasons are already enumerated pairwise into the operator message | same file `:310-316` |
| `ReportApprovalEvidence` — "does not claim the report was sent" | `src/Pegasus.Core/Workflow/CaseWorkflowContracts.cs:63`, record at `:65-70` |
| `ReportApprovalSubmission` carries only id + identity + hash; boundary assigns actor and time | same file `:73-79` |
| `RecordCaseReportApprovalRequest` is a `CaseMutationRequest` (version, key, reason, lease) | same file `:229-236` |
| `RecordReportApprovalAsync` on the store; `IRecordCaseReportApproval` use case | same file `:365`, `:420` |
| Approval refused unless state is `ReportPreparation` **or** the operation key is already known | `src/Pegasus.Core/Lifecycle/CaseLifecycle.cs:160-180`, guard at `:168-173` |
| `ValidateReportApproval` requires a non-empty `ApprovalId` | same file `:448` |
| One readiness owner; `NotReady` carries `AssessmentReadinessItem` reasons | `src/Pegasus.Core/Reports/AssessmentReportProjection.cs:306-362` |
| `AddCaseDocumentCommand` / `AddCaseDocumentResult.IsReplay` | `src/Pegasus.Core/Documents/DocumentContracts.cs:66-84` |
| `RenderedReportArtifact` provenance shape; `TemplateVersion = "rendererref1-v1"` | `src/Pegasus.Core/Reports/AssessmentReportRendering.cs:272-278`, `:8` |
| Endpoint map draft row and register/content row, both phase 7 | `docs/desktop/03-gateway-api-and-data/endpoint-map.md:77`, `:78` |
| Screen spec § 13.9 Reports bullet and the three AutomationIds | `docs/desktop/06-ui-design/screen-specs.md:371-386` |
| No `urn:pegasus:problem` string exists in `src/` or `tests/` today | `grep -rn "urn:pegasus:problem" src/ tests/` → no match |
| The thirteen problem slugs are specified in area 03 | `docs/desktop/03-gateway-api-and-data/README.md:167` |
| `AutomationMcpErrors` maps four domain exceptions to safe messages and collapses the rest | `src/Pegasus.Web/Mcp/AutomationMcpErrors.cs:7-16`, `:30-67` |
| `CaseReportApprovalWebTests.cs` is 236 lines; `AssessmentReportDraftWebTests.cs` is 259 | `wc -l` |
| `scripts/Test-MigrationGrants.ps1` exists | `ls scripts/` |
| Neither `src/Pegasus.Contracts`, `src/Pegasus.Desktop`, `src/Pegasus.Desktop.Infrastructure` nor `tests/Pegasus.Api.ContractTests` exists today | `ls src/`, `ls tests/` |

### Assumptions

- **`A-07-16-1` — a report record needs no new table.** The artifact becomes an ordinary case
  document version (`AddCaseDocumentCommand` with `EngineerReport` / `Generated`) and the report
  record is the existing approval row. *Confirmed by*: reading the persistence side of
  `RecordReportApprovalAsync` and the document store in plan step 2. *Breaks if wrong*: a new table
  drags a runtime-role `Grant*` migration checked by `scripts/Test-MigrationGrants.ps1` (PLAT-035),
  which the plan must then carry rather than meet in CI. The body's Traps names this exact risk.
- **`A-07-16-2` — the finalise operation can be one atomic unit across store-then-approve.** The
  document add and the approval are two Core calls sharing an `operationKey` and an
  `expectedVersion`. *Confirmed by*: an integration test asserting that an interrupted upload
  leaves no version, no approval and no action-history row (body step 13). *Breaks if wrong*: a
  reconciliation path is needed for a stored-but-unapproved artifact, which is scope this ticket
  does not carry — stop and report rather than inventing one.
- **`A-07-16-3` — `ReportPreparation` is the case state during a normal finalise.** The lifecycle
  guard at `CaseLifecycle.cs:168-173` makes any other state a refusal. *Confirmed by*: the
  integration test finalising from a realistic case; and the replay test, which must exercise the
  `HasOperationAsync` branch deliberately rather than by luck. *Breaks if wrong*: the endpoint's
  refusal vocabulary needs a state-specific problem type and the desktop needs a named condition
  for it.
- **`A-07-16-4` — a `not-ready` refusal maps onto an existing problem slug.** The thirteen slugs at
  `docs/desktop/03-gateway-api-and-data/README.md:167` contain no `not-ready`. *Confirmed by*:
  the plan's step 3 decision, recorded there. *Breaks if wrong*: [[GWY-001]] gains a slug, which is
  a one-line change to a list this ticket does not own — coordinate rather than invent locally.
- **`A-07-16-5` — the desktop can preview a PDF without a WebView.** Screen spec `:380-381`
  requires "a document viewer, not Pegasus UI in a WebView", and [[FND-037]] (plan handle
  `DSK-02-12`)'s no-WebView architecture test will enforce it with a single approved exception for
  [[FEAT-040]]'s renderer. *Confirmed by*: the viewer choice made in plan step 10. *Breaks if
  wrong*: the architecture test's exception list would have to widen, which is an ADR-0108 question
  and not this ticket's to answer.

## Execution placement

The six-question cloud-justification test from
`docs/desktop/00-governance-and-workflow/README.md` § 3 (`:166-178`), answered for the
responsibility this ticket places: **storing a final report artifact, approving it, and enforcing
readiness and regeneration rules.**

| Question | Answer | Evidence |
| --- | --- | --- |
| Shared authority — must several users see and update the same state? | **Yes** | A finalised report is case state every operator on that case sees. `AddCaseDocumentCommand` carries `ExpectedCaseVersion` and `EditLeaseToken` (`DocumentContracts.cs:66-84`) precisely because two operators can act on one case. Lands in the **gateway** (`Pegasus.Web`, L-01) over the existing SQL store — the same host that already owns case state. |
| Unattended execution — must it run with every desktop closed? | **No** | Finalise is operator-initiated. Note that the *rendering* moved to the desktop under L-03 and the *storing* did not; that split is what this ticket implements, and it is a placement decision, not an unattended one. |
| Protected credentials — long-lived secret that must not sit on workstations? | **Yes** | The PDF reaches Box through the gateway. ADR-0107 keeps Box credentials behind the gateway; `infra/modules/platform.bicep` holds them as Key Vault references on the Worker and Container App secrets on the Web. Lands with the **gateway**; the desktop gets none. The Guardrail "no Box credential is used by the desktop" is the observable form. |
| Public callback — must an external service call a stable public endpoint? | **No** | Nothing external calls in. Box is called outbound by the gateway. |
| Central enforcement — revocation, permissions, audit, invariant independent of the client? | **Yes**, and this is the decisive one | Readiness (`AssessmentReportProjection`), the lifecycle guard (`CaseLifecycle.cs:168-173`), the artifact-identity binding, FRD-11's regeneration rules and the action-history actor must all hold no matter what the client sends. Lands in **Core, enforced at the gateway boundary**. A client-side readiness check is not a gate — that is the hole L-03 opened. |
| Measured operational advantage — measured evidence central is materially better? | **No** | No measurement is claimed. The three "yes" answers above already place it; this row adds nothing and is not used to justify anything. |

Four "no" is not the outcome here and should not be forced into one. Three "yes" answers place the
**storage, approval and enforcement** with the gateway and Core, while rendering and preview stay on
the desktop under L-03. **No answer places anything in Azure beyond what already runs there**: the
gateway is the existing `Pegasus.Web` Container App (L-01, no new deployment unit) and the area
plan's § 3 records that this area requires no ⚠ Azure write. The ticket's own Guardrail says
"Azure: no write."

## Implications

1. **Store first, then approve that identity.** Non-negotiable, and it falls straight out of
   `ReportApprovalSubmission`'s shape (`:76-79`). The plan orders the two Core calls accordingly and
   the integration test asserts that an interruption leaves neither.
2. **The server recomputes the SHA-256; the client's declared `sha256` is a claim to be checked.**
   Core's own re-hash (`AssessmentReportRendering.cs:291-307`) happens on the client side of the
   wire once rendering is local, so it proves nothing about the bytes that arrived.
3. **Readiness is re-checked on the register path independently, not inherited from a prior draft
   call.** The body is explicit: "a case that became not-ready between render and finalise is
   refused with the same named reasons". That means calling the same Core rule again at register
   time, never trusting a client-held token or a prior success.
4. **The refusal must enumerate every `AssessmentReadinessItem`, not collapse.** The Razor handler
   already does this (`:310-316`); the API version must keep the `Requirement` /`WhyOutstanding`
   pairs structured in the problem response so the desktop can render them as named conditions. A
   generic "not ready" would be a regression against the page it replaces.
5. **The lifecycle state guard is a design input, not a surprise.** `CaseLifecycle.cs:168-173`
   permits approval only in `ReportPreparation` or on a known operation key. The replay test must
   drive the `HasOperationAsync` branch deliberately.
6. **Reuse `AddCaseDocumentResult.IsReplay` rather than building idempotency.** One less concept,
   and it is the same mechanism every other document write uses.
7. **`not-ready` has no slug yet.** The plan must name which of the thirteen existing problem types
   at `docs/desktop/03-gateway-api-and-data/README.md:167` carries the readiness refusal, or record
   that [[GWY-001]] gains one — and must not invent a URN locally, because that list is a
   one-list-per-concept surface.
8. **Approved is not sent, and the desktop cannot assert a send.** `CaseWorkflowContracts.cs:63`
   says it in the code. The separately-shown issued-version and Sent-evidence columns depend on
   **upstream TICK-208 (board [[DOCS-003]])**'s append-only ledger, because Core carries one
   `ReportApprovalId` and one `ReportSentEvidenceId` per case today; the body forbids faking that
   pair over the single slots, so step 11 does not ship ahead of that ticket.
9. **There is one binding open question and the body orders it recorded.** See below.

## Open questions

**One, and the ticket body binds it.** The body's Guardrails require it to be "resolved and recorded
in this ticket's `open-questions` document before step 3 is implemented": **upstream DOCS-001 (board
[[DOCS-001]]) records report generation as automatic — "detects a complete, accepted assessment,
invokes the integrated renderer" — while `docs/desktop/06-ui-design/screen-specs.md:379-386` and
this ticket make Generate an operator-initiated `Case.Reports.Generate` command.**

This research adds one piece of evidence the operator should see when answering, and deliberately
stops short of answering it: board [[DOCS-001]]'s own `open-questions` document states that "the
operator has selected automatic generation when all required assessment details are accepted,
immutable version/hash/custody, idempotent replay, append-only correction versions, **human approval
before issue**, and no separate renderer runtime." The phrase "human approval before issue" is
compatible with more than one reading of what "automatic" governs. **That observation is not the
answer**, and it is not taken as one here: the body says in as many words "do not invent a hybrid,
and do not implement automatic generation on the strength of the upstream wording alone." It is
recorded so the operator is shown their own prior decision rather than asked a fresh question.

The question is written as an unticked item in this ticket's `open-questions` document. It blocks
`leave-preparing`, `enter-review` and `enter-done` — which is the intended behaviour and the reason
the body ordered it recorded. It does **not** gate `leave-backlog`.

Nothing else is unsettled: every assumption above is closed by a check inside the plan's own steps.
