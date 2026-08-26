# Checklist — PLAT-029

## Acceptance

- [x] Local occurrence reads serve retained source bytes through `OpenReadVersionAsync`.
- [x] Local occurrence reads serve retained attachment bytes at ordinal 2 or later.
- [x] Local occurrence reads serve folded image-case bytes from the existing `images` layout.
- [x] Existing managed `versionId` content remains readable through `OpenReadVersionAsync`.
- [x] SHA-256 and length verification remain enforced.
- [x] Missing content preserves `FileNotFoundException("The document content is unavailable.")`.
- [x] Corrupt content fails with `InvalidDataException`.
- [x] A real accepted/processed intake source is readable through the document download reader.
- [x] A real accepted/processed intake source is readable through the document export ZIP reader.
- [x] BoxDocumentContentStore, the Core interface default, and LocalCaseCustody write layout are unchanged.
- [x] Named Test/UAT component and Known gaps documentation are updated.
- [x] Intake-retained occurrence content is consumed by both the EVA handoff generator and the assessment report projection source; direct test `IntakeRetainedImageIsReadByEvaAndAssessmentReportProjection` passes.
- [ ] Required local Start/Smoke validation — blocked by the existing launcher process-path failure before readiness; see post-implementation-report.
- [ ] Operator-visible retained-content journey through the prescribed local Start/Smoke stack — not claimed while the stack launcher is unavailable.

## Delivery gates

- [x] Implementation committed at `a505175c` plus the reader-consumer coverage commit.
- [x] Branch pushed to `origin/plat-029-local-document-content`.
- [x] Task PR created — PR #25 against `dev`.
- [ ] Independent review passed — fresh review is required after the reader-consumer coverage update; the previous fresh review correctly failed on the Start/Smoke blocker and missing reader coverage.
- [ ] CI green and PR merged to `dev`.
- [ ] Merged-main proof written.
- [ ] Kanmer closeout completed.
