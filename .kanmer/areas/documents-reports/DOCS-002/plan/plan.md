# Plan — DOCS-002: upstream:TICK-018 · DOC-02 — Store source emails, instruction documents, images, correspondence, and reports in Box

## Governing documents

- `docs/frd/frd-05-documents-extraction-and-custody.md`

## Chosen approach

Close the two remaining DOC-02 gaps, and only those two: **automatic Box retention of case correspondence when a later inbound e-mail is associated to a case**, and **automatic Box retention of outbound sent evidence**. Both are delivered as new durable custody work kinds keyed on the association row and the sent-evidence row, reusing the existing outbox convention, `BoxCaseCustody` and `BoxDocumentContentStore`. The ticket also settles one FRD-05 interpretation: whether DOC-02 requires per-attachment files in Box or the retained `.eml` satisfies it.

## Routing and constraints

- Future owner follows the ticket’s stated project boundary and repository task workflow. Reuse existing Core policy/ports before adding any abstraction.


## Ordered implementation steps

1. Orient. Read this body in full including the verbatim upstream ticket, then the upstream `research` document copied onto this ticket — it is the requirement and it enumerates exactly what is live and exactly what is missing. Read `docs/frd/frd-05-documents-extraction-and-custody.md` in full. Read [[DSK-07-16]] and confirm for yourself that the report half is closed there. Call `get_doc_gates <this ticket id>`, then `take_ticket` on branch `task/upstream-tick-018-correspondence-custody`.
2. Re-verify the copied research's "live" claims against the **fork tree**, not against upstream production, and record the result in `research`: acceptance enqueues `create_case_custody` (`EfCaseAcceptanceStore.cs:384-397`), replacement enqueues it too (`EfLinkedCaseReplacementStore.cs:212`), `EfQueuedCustodyProcessor` dispatches to `BoxCaseCustody`, and `AddProductionBoxCustody` composes `IDocumentContentStore` → `BoxDocumentContentStore`. Confirm by reading `EfIntakeMutationStore.LinkAsync` (`:273`) and `AutoLinkAsync` (`:434`) that association enqueues nothing — that is the defect.
3. **State the scope in `plan` before writing code.** This fork ticket carries the two named gaps only. Explicitly out of scope and recorded as such: the report half (owned by [[DSK-07-16]]), case custody roots and source-email retention (already live), and the image-initiated custody slice (its remaining item is a deploy-stage production verification, not code). Do not re-plan DOC-02 as a whole.
4. Add one new durable work kind for correspondence retention. Extend `ExternalWorkKinds` in `src/Pegasus.Core/Custody/ExternalWorkProcessing.cs` and add the matching member to `CustodyWorkKind` in `src/Pegasus.Core/Custody/CustodyContracts.cs`, following the existing `create_case_custody` / `create_image_case_custody` naming — name it in `plan` rather than guessing here. Extend the fail-closed dispatch at `ExternalWorkProcessing.cs:84-90`; an unknown persisted kind must still fail closed and must never be treated as custody by default.
5. Enqueue it from the association transactions themselves — `EfIntakeMutationStore.LinkAsync` and `AutoLinkAsync` — in the same transaction that records the association, with a deterministic operation key derived from the association row so a replayed association enqueues nothing new. Follow the exact shape used at `EfCaseAcceptanceStore.cs:384-397`. Done looks like: an integration test that links a retained mailbox message to a case and finds exactly one pending `ExternalWorkItems` row for the new kind.
6. Do the same for outbound sent evidence: enqueue a retention work item from the transaction that writes the `SentEmailEvidence` row (`PegasusDbContext.cs:46`, `:1241`), keyed on that row. Reuse the same outbox convention; do not add a second scheduling mechanism.
7. Implement the handler over the existing adapters — `BoxCaseCustody` for the case-folder placement and `BoxDocumentContentStore` for the versioned file — and give `LocalCaseCustody` / `LocalDocumentContentStore` the same behaviour so the DevelopmentOffline stack can prove it under **L-02**. Failure behaviour follows FRD-05 for case-scoped custody: an explicit named failure plus staff-initiated retry, not a silent automatic business retry.
8. **Re-expressed for the desktop.** FRD-05's staff retry lives today on a Razor page the conversion deletes. Keep the requirement and move it: the failed retention item and its Retry command surface through the operations projection that [[DSK-05-20]] and [[DSK-07-04]] already own (`Operations.External.Table`, `Operations.External.Retry`), so this ticket adds the work kind and its named failure reason to that projection rather than building a screen. Say in `plan` that you have done so.
9. **Operator step** — settle the FRD-05 interpretation the upstream research parks: does DOC-02 require each attachment of the accepted source exploded into Box as its own file, or does the retained `.eml` (which contains the attachments) satisfy it? FRD-05's wording reads as satisfied by the retained source for day one, but the operator may expect visible per-file content. Record the answer in this ticket's `open-questions` document and, if the answer is per-file, raise it as its own follow-up ticket rather than widening this one. Evidence the operator hands back: one sentence, and the FRD-05 clause it amends if any.
10. Test in the projects that exist on the fork: extend `tests/Pegasus.IntegrationTests/CustodyOutboxIntegrationTests.cs` and `DocumentCustodyDurabilityTests.cs` for enqueue-on-association, enqueue-on-sent-evidence, replay creating no second item, a dependency failure producing a named failure with a staff-retryable state, and a closed case remaining read-only. Add Core policy tests in `tests/Pegasus.Core.Tests` for the new kind's dispatch and failure-code classification.
11. **Operator step** — the live Box verification. Any live check writes only to the approved disposable subtree recorded at `docs/operations.md#approved-box-integration-test-target`; the `requires-live-approval` label stands. The operator hands back: the approval, the subtree used, and the folder listing showing the retained correspondence item and the retained sent-evidence item. Nothing outside that subtree is written.
12. Run the simplification pass over this branch diff, record it under a dated `## Simplification pass` heading in this ticket's `plan` document, update `docs/capabilities.md`'s DOC-02 row to the tier actually proved, and open the PR into `dev`.

## Acceptance conditions

- [ ] Associating a later inbound e-mail to a case enqueues exactly one durable retention work item in the same transaction as the association, and a replayed association enqueues none.
- [ ] Recording outbound sent evidence enqueues exactly one durable retention work item keyed on that evidence row.
- [ ] The retained correspondence item and the retained sent-evidence item appear in the case's Box folder through `BoxCaseCustody` / `BoxDocumentContentStore`, and in the local artifact root through the local pair.
- [ ] A retention failure is explicit and named, is staff-retryable through the operations projection of [[DSK-05-20]], and does not silently auto-retry as if it were image custody.
- [ ] The report half is untouched: no second path stores the finalised PDF, and [[DSK-07-16]]'s registration remains the only one.
- [ ] The per-attachment interpretation of step 9 is answered by the operator and recorded before the ticket leaves Preparing.
- [ ] No Box write occurs outside the approved test subtree, and no desktop holds a Box credential.

## Verification

- [ ] A task-level plan records the exact feature contract, caller, failure behavior, and required tests.
- [ ] The activation criteria have been satisfied or explicitly accepted before implementation begins.

## Risks and boundaries

- **Azure**: no write. ⚠ **Box write** (not Azure): the live verification of step 11 writes only inside the approved disposable subtree at `docs/operations.md#approved-box-integration-test-target` and needs exact-target operator approval per `docs/runbook.md` § Live operation approval matrix. The `requires-live-approval` label stands.
- **Scope boundary**: may touch `src/Pegasus.Core/Custody/**`, `src/Pegasus.Infrastructure/Custody/**`, `src/Pegasus.Infrastructure/Persistence/EfIntakeMutationStore.cs`, `EfQueuedCustodyProcessor.cs`, the sent-evidence store, any migration this needs, and the two test projects. Must **not** re-implement report retention (that is [[DSK-07-16]]), must **not** change `src/Pegasus.Worker`, must **not** widen into DOC-02 as a whole, and must **not** give any desktop client a Box credential — retention is server-side under **L-01**.
- **Blocks / blocked by**: this ticket **blocks** [[DSK-07-11]] (its outbound seam records sent evidence as an audit record and would sign off with no Box retention behind it) and [[DSK-05-14]] (the documents-and-custody slice would claim DOC-02 parity while case correspondence never reaches Box). It is **not** blocked by [[DSK-07-05]]; the Box broker endpoints serve the desktop browser, while this retention path is server-side and already has its adapters.
- **Traps**: blob is hot staging only, so "the message is in `IntakeAssets`" is not custody; the image-custody re-arm policy in `ImageCustodyRetryPolicy` is deliberately automatic and must **not** be copied onto case-scoped correspondence custody, which FRD-05 requires to fail explicitly with a staff retry; an unknown persisted work kind must keep failing closed; and a closed case stays read-only, so a late association to a closed case must be refused rather than written.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.
