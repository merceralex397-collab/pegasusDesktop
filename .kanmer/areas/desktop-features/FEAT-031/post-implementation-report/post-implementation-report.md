# Post-implementation report — FEAT-031

## Exact implementation head

`c3d06081a09a47798ac7e333dcf9e0afeac026a9` on branch `task/dsk-07-05-box-broker-endpoints`.

## Delivered scope

The gateway broker routes, contracts, auth/problem-details/correlation integration, bounded upload-session handling with owner-lock expiry enforcement, streaming download headers, conditional ETags, logical removal with audited before/after state, operation-key evidence confirmation, and existing-project contract tests are implemented. A real-host test reaches the existing LocalDB/Ef custody/content adapter for completed upload, confirmation, and removal. No export/evidence-gallery route is exposed.

## Validation evidence

- `dotnet build .\\Pegasus.slnx --configuration Release --no-restore -nr:false` passed with 0 warnings and 0 errors.
- `dotnet test .\\tests\\Pegasus.IntegrationTests\\Pegasus.IntegrationTests.csproj --configuration Release --no-build --filter FullyQualifiedName~BoxDocumentBroker -nr:false` passed: 26 passed, 0 failed, 0 skipped.
- The persistence test proved canonical SHA/metadata, unchanged intake-receipt count plus no abandoned-session document or temporary file, confirmation action history, logical-removal action history with before/after snapshots, and exact operation-key replay including equal response bodies.
- `git diff --check` passed before commit; the branch worktree is clean.
- Changed production-scope scan found no provider-secret/token/URL/object-ID response exposure or direct desktop Box SDK call; the only bearer match is the intentional 401 challenge.
- Broader profile evidence reported by the implementing agent before this final remediation was 925 passed, 2 skipped, 0 failed; exact-head CI has not run.

## Open acceptance conditions

- The independent review of exact head `3860d43f` returned FAIL; its removal-audit, persistence-evidence, and replay-assertion findings were remediated at `c3d06081a09a47798ac7e333dcf9e0afeac026a9`. A fresh independent review of the current exact head is pending.
- PLAT-039 token-age success after more than one hour has not been proven in this fork; the referenced upstream ticket is absent from this board and upstream synchronization is prohibited.
- PLAT-041 current-fork O(1)+N call-budget implementation/measurement for export/evidence-gallery has not been proven; those routes remain unexposed and the referenced upstream ticket is absent from this board.

This report does not claim the ticket is done. The open acceptance conditions must be resolved before Kanmer closeout.

## Review remediation update — 2026-08-27

The current branch head is `c3d06081a09a47798ac7e333dcf9e0afeac026a9`. The coordinator added a `document_logically_removed` history row with reason, operation key, and before/after snapshots; the LocalDB/Ef test now checks receipt preservation, full canonical metadata, removal audit state, and equal original/replay response bodies. The Release build passed with 0 warnings/errors and the broker-focused test suite passed 26/26.

A new independent review is required at this exact head. PLAT-039 token-age evidence, PLAT-041 O(1)+N/export-gallery implementation and measurement, and the boundary evidence restricted by the current no-cloud/deployment operator instruction remain open; this report does not claim done.

## Boundary evidence update — 2026-08-27

Local read-only package/source checks at `c3d06081a09a47798ac7e333dcf9e0afeac026a9` found `Box.Sdk.Gen` only in `src/Pegasus.Infrastructure/Pegasus.Infrastructure.csproj`; `src/Pegasus.Desktop.Infrastructure` is absent; the changed gateway marker scan found only the intentional `WWW-Authenticate: Bearer` challenge. The 26 focused route tests cover response leakage.

The repository Bicep file shows the intended Container App secret and Key Vault-reference placement, but the required live Key Vault names-only read was not performed because the current operator instruction forbids cloud/deployment operations until the full refactor is complete. That acceptance evidence remains open.
