# Post-implementation report — TICK-208 / DOCS-003

## Delivered slice

DOCS-001 was re-read at merged main SHA `80bbb6fe86916a0c499a70480121e278ab114e7a`. DOCS-003 reuses its `AssessmentReportVersion`, `AssessmentReportArtifact`, `AssessmentReportVersionStore`, predecessor chain, immutable artifact identity, and hash model. It also reuses the existing approved-mailbox Sent-evidence store, workflow commands, query projection, migration grant matrix, and current CASE-23 lifecycle.

The implementation adds only the missing version-specific custody association:

- Core approval, retained-evidence, link, auto-link, unlink, and projection contracts carry an immutable report-version identity.
- Persistence adds the report-version ledger, one current exact Sent-evidence pointer, append-only association history, artifact/version/hash/status fields, uniqueness/concurrency configuration, and the EF migration.
- Manual and Worker linking require an existing approved version and exact artifact identity/hash. Missing, stale, mismatched, duplicate, ambiguous, cross-case, or replayed associations fail closed or return the existing idempotent result.
- Correction/addendum versions start unsent. A predecessor's final Sent evidence remains on that predecessor; a new exact Sent item can bind only to the successor. Unlink preserves former case, actor, time, reason, and source evidence in association history.
- Existing Case projections show ordered issued versions, approval artifact, association status/reason, and final Sent evidence separately. Legacy rows remain explicit `Unresolved`.
- CASE-23 states/transitions, send operations, external delivery claims, mailbox mutation, and external APIs are unchanged.

## Migration and safety evidence

Migration `20260827231948_IssuedReportVersionEvidenceLedger`:

- creates the ledger/history structures and required indexes/constraints;
- marks pre-ledger approval and Sent-evidence rows `Unresolved` with reasons;
- creates ledger rows only from existing `AssessmentReportVersions`;
- does not infer a version from filenames, pointers, timing, or hashes and contains no evidence/artifact backfill;
- denies deletion of ledger/history data for the runtime roles and has the repository's down migration;
- was validated by the focused migration and runtime-role tests.

Read-only diff checks found no new external/cloud/mailbox API references in added product-source lines. No cloud, Azure, deployment, credential, Graph, Outlook, Box, or upstream operation was performed.

## Validation

- `dotnet restore ./Pegasus.slnx --locked-mode` — passed; locked assets unchanged.
- `dotnet build ./Pegasus.slnx --configuration Release --no-restore /nr:false` — passed; 0 warnings, 0 errors.
- Focused Core auto-link tests — passed.
- Focused persistence class — 30 passed, 0 failed.
- Focused migration/runtime-role tests — 17 passed, 0 failed.
- `dotnet test ./Pegasus.slnx --configuration Release --no-build --filter "Category!=Corpus" /nr:false` — Core 939 passed; API contract 12 passed; architecture 111 passed; integration 1,017 passed; 2 documented skips; 0 failed.
- Simplification pass completed 2026-08-28. Reuse, simplification, reliability, scope, and altitude findings are recorded in the plan; no finding is deferred.
- Current-state documentation was updated only for the merged-code/as-built evidence tier; no deployment or runtime claim is made.

## Delivery state

The implementation is complete on branch `task/upstream-tick-208-issued-version-ledger`, pending the required independent review, PR/CI, merge to `dev`, merged-main proof, and Kanmer closeout.

## Independent-review scope disposition — 2026-08-28

Curie identified that the plan text names a gateway/native Reports surface absent from this PR. Live board evidence resolves this as a documented ownership boundary, not an omitted DOCS-003 implementation: FEAT-042 (DSK-07-16) owns the `/api/v1` finalise/content contract and FEAT-018 (DSK-05-18) owns the native Reports flow; both are currently blocked by DOCS-003. DOCS-003's guardrail prohibits building those surfaces. This PR supplies the truthful Core/persistence/Case-projection ledger they require. No downstream surface is claimed as shipped here.

Curie also identified that unlink clears the retained row's mutable current-candidate association fields. That is the chosen model: `CaseReportAssociationHistory` is append-only and authoritative for former case, source identity, time, actor, roles, reason, and before/after report version; the retained row fields describe only its current candidate association. The plan now states this explicitly and the projection reads the authoritative history.
