# Checklist — FND-007

## Completed Phase 0 branch work

- [x] Read the governing plan, renderer ADRs, report port, and Microsoft Learn references.
- [x] Create ADR-0108 with valid frontmatter, `status: proposed`, the never-UI rule, the fixed `HWND_MESSAGE` controller, the gateway-retention gate, and the reversal condition.
- [x] Update the Phase 0 and Phase 7 source plans from host selection to packaged-controller validation.
- [x] Run documentation-link and Markdown-placement checks and `git diff --check`.
- [x] Record the review-normalised `related_frd: [frd-11]`.

## Remaining Phase 0 delivery

- [x] After [[FND-005]] merged, the branch was updated and PR #13 contained only the three FND-007 documentation files.
- [x] Open the scoped PR to `dev`, obtain independent review, and merge through the repository workflow (PR #13).
- [x] When that delivery reached `main`, write FND-007 proof of the merged proposed ADR, no ADR index row, review, and documentation checks; then close FND-007.

## Explicit Phase 7 hand-off

- [[FEAT-040]] validates the packaged controller and renderer.
- [[FEAT-041]] proves golden-file parity.
- [[FEAT-038]] alone accepts ADR-0108 and adds its index row. None of these is an FND-007 closeout task.
