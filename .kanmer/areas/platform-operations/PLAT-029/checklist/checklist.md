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
- [ ] Required local Start/Smoke validation — blocked by repository-required SDK 10.0.302 unavailable on this workstation; see post-implementation-report.
- [ ] EVA handoff built from an intake-retained document in the local script stack — direct folded-image adapter coverage and existing EVA managed-layout suite pass, but the required local Start/Smoke environment is unavailable; do not claim this tier.

## Delivery gates

- [x] Implementation committed at `a505175c`.
- [x] Branch pushed to `origin/plat-029-local-document-content`.
- [ ] Task PR created — blocked by GitHub `GraphQL: must be a collaborator (createPullRequest)`.
- [ ] Independent review passed — latest reviewer returned FAIL on missing report/checklist and Start/Smoke blocker; refresh review after blockers change.
- [ ] CI green and PR merged to `dev`.
- [ ] Merged-main proof written.
- [ ] Kanmer closeout completed.
