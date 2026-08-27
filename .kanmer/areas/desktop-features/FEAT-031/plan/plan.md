# Plan — FEAT-031 Box broker endpoints

## Governing documents

- `docs/frd/frd-05-documents-extraction-and-custody.md` is the linked governing FRD.
- Group context: EPIC-008 and HZN-007.
- `docs_todo` remains true because seeded conversion documents are not being rewritten as part of this implementation; the linked FRD and ticket evidence govern this change.

## Scope and ownership

Deliver the gateway-only Box broker surface for list, metadata, bounded streaming download/upload sessions, logical removal, and third-party vehicle evidence confirmation. The endpoint layer translates to existing Core use cases and state/custody ports; it does not become a second business-policy owner.

Export and evidence-gallery paths remain unexposed until their current-fork call-budget implementation and measurement are proven. The inherited token-age acceptance also remains open until current-fork evidence exists. No upstream Pegasus synchronization is permitted, so neither gap may be closed by importing upstream work.

## Implementation completed

1. Inventory verified the existing Core ports and gateway composition root.
2. Implemented the following routes in `src/Pegasus.Web/Api/BoxDocumentBrokerEndpoints.cs`:
   - document list and metadata with workflow-version ETags and weak/list/wildcard conditional matching;
   - streamed document content with `private,no-store`;
   - bounded upload-session creation, chunk PUT, and completion;
   - logical document removal;
   - idempotent third-party vehicle-evidence confirmation by operation key.
3. Each operation performs case/document authorization/state checks immediately before the provider-facing Core call. Responses omit provider tokens, URLs, object IDs, secrets, and fabricated replay claims.
4. Upload sessions enforce `IntakeEnvelopeLimits.MaximumContentLength`, cap active sessions and buffered bytes, re-check expiry under the owner lock, dispose expired sessions safely, and serialize completion per session. Retry guidance uses numeric `Retry-After`.
5. Contract facts remain in the existing IntegrationTests project. The overlapping standalone contract-test scaffold was removed; no solution or CI changes were introduced.
6. A real-host LocalDB/Ef custody test now proves the gateway reaches the existing persistence/content adapter for a completed image upload, matching canonical SHA/metadata, no abandoned-session receipt/document or temporary file, confirmation action history, logical-removal action history with before/after snapshots, and exact operation-key replay.

## Exact branch history

- Initial implementation: `f31d5aefdb48575c9ee990a0515c0e68374f8d63`.
- Review remediation: `42250fd20fcf917081bb919c140a4797ce557150`, `8ff40b2720fef1e6a36b46a4124fe1578f6c7082`, `fcf5145c5bf14354aeaee87429f45e9c7826c591`.
- Final lifecycle/persistence evidence: `3860d43f`; removal audit remediation: `c3d06081`.
- Current exact head: `c3d06081a09a47798ac7e333dcf9e0afeac026a9` on `task/dsk-07-05-box-broker-endpoints`.

## Validation

Coordinator-owned exact-head checks at `c3d06081a09a47798ac7e333dcf9e0afeac026a9`:

- `dotnet build .\\Pegasus.slnx --configuration Release --no-restore -nr:false` — passed; 0 warnings, 0 errors.
- `dotnet test .\\tests\\Pegasus.IntegrationTests\\Pegasus.IntegrationTests.csproj --configuration Release --no-build --filter FullyQualifiedName~BoxDocumentBroker -nr:false` — passed; 26 passed, 0 failed, 0 skipped.
- The focused persistence test is included in those 26 tests and passed against the existing LocalDB/Ef custody path.
- `git diff --check` — passed before commit; the committed worktree is clean.
- Changed production-scope scan for `box.com`, `client-secret`, `access_token`, private-key, remote/object IDs, bearer/JWT leakage — no matches except the intentional `WWW-Authenticate: Bearer` challenge.
- The broader IntegrationTests profile was reported by the implementing agent before the final remediation as 925 passed, 2 skipped, 0 failed. It is not exact-head evidence and does not replace CI.

## Simplification pass — 2026-08-27

- Removed the standalone contract-test project rather than creating a competing test owner.
- Reused existing Core ports, gateway filters, and response/problem-details conventions; no new business-policy layer or compatibility path was added.
- Centralized upload-session bounds, quota accounting, owner-lock expiry cleanup/disposal, and per-session completion serialization in the minimal in-memory session owner required by this endpoint scope.
- Removed the unsupported replay field instead of inventing semantics that Core does not return.
- Kept export/gallery out of the diff because the required current-fork O(1)+N proof is absent.
- Added one bounded real-host persistence/custody test instead of duplicating the existing adapter suite.
- No unapplied behaviour-preserving simplification finding remains from the coordinator audit.

## Review and remaining acceptance gates

The first independent review of the initial implementation failed on the overlapping test scaffold, upload/session hardening, response-cache/ETag details, and missing adapter/persistence evidence. Those findings were remediated through `3860d43f`; the final removal-audit and persistence-evidence remediation is `c3d06081a09a47798ac7e333dcf9e0afeac026a9`. The review at `3860d43f` returned FAIL with findings on PLAT-039/041, removal audit history, persistence evidence strength, and exact replay assertion; those findings are addressed or remain explicitly blocked as described below.

The ticket is not acceptance-complete yet:

- PLAT-039: the required document-download and case-export success after more than one hour of gateway revision age has no current-fork proof. The referenced upstream IDs do not exist on this board, and upstream synchronization is prohibited.
- PLAT-041: export/evidence-gallery O(1)+N resolution and measurement have no current-fork implementation/proof, so those routes remain intentionally unexposed. The referenced upstream IDs do not exist on this board, and upstream synchronization is prohibited.

Next action: obtain fresh independent review of `c3d06081a09a47798ac7e333dcf9e0afeac026a9`; if supported scope passes, run exact-head CI and manage the PR. The two inherited acceptance conditions remain blockers for closing FEAT-031 until implemented/proven in this repository or explicitly resolved through a truthful Kanmer scope decision. Do not use upstream synchronization or cloud/deployment writes.

## Review remediation — 2026-08-27

The independent review of `3860d43f` returned FAIL. Its merge-blocking findings were that logical removal had no action-history row, the real-host evidence did not prove abandoned receipt preservation or all canonical metadata, and the confirmation replay response bodies were not compared. It also kept PLAT-039 token-age and PLAT-041 O(1)+N/export-gallery requirements open, because those are absent from this fork and cannot be satisfied by upstream synchronization.

Remediation committed at `c3d06081a09a47798ac7e333dcf9e0afeac026a9`: logical removal now persists a `document_logically_removed` action with reason, operation key, and serialized before/after document state, rejects an already-removed document missing its audit row, and validates same-key replay against the persisted outcome. The real-host LocalDB/Ef test now proves intake-receipt count is unchanged for an abandoned upload, checks the complete response/persisted canonical metadata projection, verifies removal audit state, and compares original/replayed mutation responses.

- `dotnet build ./Pegasus.slnx --configuration Release --no-restore -nr:false` passed with 0 warnings and 0 errors.
- `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --filter FullyQualifiedName~BoxDocumentBroker -nr:false` passed 26/26.
- `git diff --check` passed and the branch worktree is clean.

A fresh independent re-review of the exact current head is required. PLAT-039 token-age proof, PLAT-041 call-budget/export-gallery implementation and measurement, and the repository/cloud boundary evidence remain open acceptance blockers; no export/gallery route is exposed and no cloud/deployment read or write is being used under the current operator boundary.
