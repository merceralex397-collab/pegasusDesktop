# Plan — FEAT-031 Box broker endpoints

## Governing documents

- `docs/frd/frd-05-documents-extraction-and-custody.md` is the linked governing FRD.
- Group context: EPIC-008 and HZN-007.
- `docs_todo` remains true because the seeded conversion documents are not being rewritten as part of this implementation; the linked FRD and ticket evidence govern this change.

## Scope and ownership

Deliver the gateway-only Box broker surface for list, metadata, bounded streaming download/upload sessions, logical removal, and third-party evidence confirmation. The endpoint layer translates to existing Core use cases and state/custody ports; it does not become a second business-policy owner.

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
4. Upload sessions enforce `IntakeEnvelopeLimits.MaximumContentLength`, cap active sessions and buffered bytes, expire and dispose abandoned sessions, and serialize completion per session. Retry guidance uses numeric `Retry-After`.
5. Contract facts were added to the existing IntegrationTests project. The overlapping standalone contract-test scaffold was removed; no solution or CI changes were introduced.

## Validation

Coordinator-owned final-head checks at `fcf5145c5bf14354aeaee87429f45e9c7826c591`:

- `dotnet restore .\\Pegasus.slnx --locked-mode -nr:false` — passed; all projects up to date.
- `dotnet build .\\Pegasus.slnx --configuration Release -nr:false --no-restore` — passed; 0 warnings, 0 errors.
- `dotnet test .\\tests\\Pegasus.IntegrationTests\\Pegasus.IntegrationTests.csproj --configuration Release --no-build --filter FullyQualifiedName~BoxDocumentBroker -nr:false` — passed; 18 passed, 0 failed, 0 skipped.
- `git diff --check` — passed.
- Changed-scope scan found no provider secret/token/URL/object-ID response exposure, direct desktop Box SDK call, export/gallery route, or upstream-sync code.
- The implementing agent reported the broader IntegrationTests profile before the final comment/header-only change as 925 passed, 2 skipped, 0 failed; this is recorded as agent-reported evidence, not a substitute for exact-head CI.

## Simplification pass — 2026-08-27

- Removed the standalone contract-test project rather than creating a competing test owner.
- Reused existing Core ports, gateway filters, and response/problem-details conventions; no new business-policy layer or compatibility path was added.
- Centralized upload-session bounds, quota accounting, expiry cleanup, and per-session completion serialization in the minimal in-memory session owner required by this endpoint scope.
- Removed the unsupported replay field instead of inventing semantics that Core does not return.
- Kept export/gallery out of the diff because the required current-fork O(1)+N proof is absent.
- No unapplied behaviour-preserving simplification finding remains from the coordinator audit.

## Review and remaining acceptance gates

The first independent review of the initial implementation failed on the overlapping test scaffold, upload/session hardening, response-cache/ETag details, and missing evidence. Those findings were addressed in commits `42250fd20fcf917081bb919c140a4797ce557150`, `8ff40b2720fef1e6a36b46a4124fe1578f6c7082`, and `fcf5145c5bf14354aeaee87429f45e9c7826c591`. A fresh independent review is required on the exact final head.

The ticket is not acceptance-complete yet:

- PLAT-039: the required document-download and case-export success after more than one hour of gateway revision age has no current-fork proof.
- PLAT-041: export/evidence-gallery O(1)+N resolution and measurement have no current-fork implementation/proof, so those routes remain intentionally unexposed.

Next action: complete current-fork in-repo token-age/call-budget work if it is owned by this ticket, or record/route the exact dependency through Kanmer without weakening FEAT-031’s acceptance. Do not use upstream synchronization or cloud/deployment writes.
