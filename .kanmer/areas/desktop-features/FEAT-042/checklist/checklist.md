# Checklist — FEAT-042

One box per plan step, in plan order. The last box produces `proof`.

**Before ticking anything from step 3 onward:** the unticked item in this ticket's `open-questions`
document must be answered by the operator. The body's Guardrails require it, and it correctly holds
`leave-preparing` shut until then.

- [ ] Read `docs/desktop/07-integrations/README.md` § 5 row `DSK-07-16`, `endpoint-map.md:77-78`, `screen-specs.md:379-386`, and `docs/frd/frd-11-reports-correspondence-and-reviewed-proposals.md` in full.
- [ ] Read the two imported upstream tickets by board id: **upstream DOCS-001 (board [[DOCS-001]])** and **upstream TICK-208 (board [[DOCS-003]])**.
- [ ] Call `get_doc_gates FEAT-042` and `take_ticket` on branch `task/dsk-07-16-report-finalise`.
- [ ] Append to `research` the definition of "final" with line references — `ReportApprovalSubmission` binds identity + SHA-256 and the boundary assigns actor and time (`CaseWorkflowContracts.cs:73-79`) — and record that the artifact is stored **before** it is approved.
- [ ] Settle assumption `A-07-16-1` by reading the persistence side of `RecordReportApprovalAsync` (`CaseWorkflowContracts.cs:365`) and the document store; if a new table is genuinely needed, **stop and re-plan** rather than adding it.
- [ ] Obtain the operator's answer to the binding open question and tick it in `open-questions` with the decision recorded.
- [ ] Implement the draft endpoint's `NotReady` response with **each** `AssessmentReadinessItem`'s `Requirement` and `WhyOutstanding` as structured fields, over `AssessmentReportProjection` (`src/Pegasus.Core/Reports/AssessmentReportProjection.cs:306-362`) — no second readiness rule.
- [ ] Implement the register endpoint's independent server-side readiness re-check, holding no client-supplied readiness token, refusing a case that became not-ready between render and finalise with the same named reasons.
- [ ] Record the chosen problem-type slug for the readiness refusal under a dated heading in the `plan` document, chosen from `docs/desktop/03-gateway-api-and-data/README.md:167` or coordinated with [[GWY-001]] — never invented locally.
- [ ] Implement `POST /api/v1/cases/{caseId}/reports` accepting the streamed PDF plus `fileName`, `sha256`, `pageCount`, `templateVersion`, `engineVersion`, `expectedVersion`, `editLeaseToken` and `operationKey`.
- [ ] Recompute the SHA-256 server-side and refuse a mismatch with `urn:pegasus:problem:validation`, storing nothing; assert `templateVersion` against the `AssessmentReportContract.TemplateVersion` constant.
- [ ] Store through `IAddCaseDocument` with `DocumentSemanticRole.EngineerReport` and `DocumentSource.Generated`, via [[FEAT-031]]'s Box broker, reusing `AddCaseDocumentResult.IsReplay` for idempotency.
- [ ] Call `IRecordCaseReportApproval` with the **stored** artifact's identity and hash, and confirm the `ReportPreparation`-or-known-key guard at `src/Pegasus.Core/Lifecycle/CaseLifecycle.cs:168-173` is satisfied by design rather than by luck.
- [ ] Implement `GET /api/v1/cases/{caseId}/reports/{reportId}/content` with `Content-Length`, `ETag`, range support, `nosniff` and a safe filename, over the existing document content store port.
- [ ] Return regeneration permission as a **named condition** from the reports-section response per FRD-11, so the desktop renders it and does not compute it.
- [ ] Confirm `AddPegasusReportRendering` (`src/Pegasus.Infrastructure/DependencyInjection.cs:446`) is untouched and the draft route still returns gateway-rendered bytes.
- [ ] Record the parity flag's name and who may flip it under a dated heading in the `plan` document, or cite [[FEAT-040]] step 10 / [[FEAT-038]] step 9 if one has already named it.
- [ ] Build the desktop Reports tab in `src/Pegasus.Desktop` with Generate (progress + cancel), Preview (**in-app PDF document viewer, never a WebView**) and Finalise (reasoned, idempotent), using AutomationIds `Case.Reports.Generate`, `Case.Reports.Preview`, `Case.Reports.Send`.
- [ ] Make the desktop render the server's named `NotReady` reasons verbatim and decide readiness nowhere in client code.
- [ ] Add the issued-versions list with custody state and sent evidence in **separate** columns over **upstream TICK-208 (board [[DOCS-003]])**'s ledger — or hold this box and record the sequencing if that ticket has not landed.
- [ ] Write the nine contract-test facts in `tests/Pegasus.Api.ContractTests`, including not-ready on **both** paths and a replay that drives the `HasOperationAsync` branch deliberately.
- [ ] Write the integration test following `CaseReportApprovalWebTests.cs`: one version, one report record, one approval, one action-history entry — and an interrupted upload leaving none.
- [ ] Write the view-model tests for generate/preview/finalise state, cancel during render, not-ready reasons as named requirements, and regeneration disabled with a named reason.
- [ ] Add the finalise assertions to the `winapp ui` reports script owned by [[TEST-008]].
- [ ] Update `endpoint-map.md:77-78`, FRD-11's desktop behaviour clause, `docs/current-architecture.md` and the `DSK` row in `docs/capabilities.md`.
- [ ] Regenerate `openapi/pegasus-v1.json` ([[GWY-004]]) and the Kiota client ([[GWY-005]]).
- [ ] Run the simplification pass over this branch's own diff and record it under a dated `## Simplification pass` heading in the `plan` document.
- [ ] Run the full verification set and capture its output as `proof`: the contract-test run, the integration run, the view-model run, the `ui-tests.ps1 -Script reports` run with screenshots, `git diff --exit-code openapi/pegasus-v1.json`, `git diff --stat origin/dev -- src/Pegasus.Worker` (expected empty), `grep -rn "AddPegasusReportRendering" src/` (expected still present), and the desktop-package secret scan showing no Box credential. Then open the PR into `dev`.

## Progress notes

(append with `set_ticket_doc(doc: "checklist", append: true)`)
