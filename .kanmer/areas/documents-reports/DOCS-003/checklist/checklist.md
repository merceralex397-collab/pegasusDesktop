# Checklist — TICK-208

- [x] After SIMPLI-014 and DOCS-001 merge, re-read their exact merged report-version contracts/persistence/proof; name the reused types/stores and either narrow TICK-208 to missing Sent association work or close no-code if all acceptance is already met.
- [x] Add the minimal Core version-specific approval/Sent association and ordered history contract using DOCS-001 immutable report identities and existing exact approved-mailbox evidence types, without a second report aggregate or CASE-23 states.
- [x] Add append-only persistence/configuration/migration for version→approval→Sent evidence and reasoned association history, preserving all legacy rows with explicit unresolved provenance and no fabricated artifact match.
- [x] Make staff link/reassociate and Worker auto-link version-aware, chronological, idempotent and fail-closed on missing/ambiguous/mismatched/duplicate evidence while preserving former associations.
- [x] Prove correction/addendum creates an unsent successor, preserves the predecessor's final Sent evidence/time, and binds a new exact Sent item only to the successor without changing CASE-23 lifecycle.
- [x] Adapt existing Case/detail/report-evidence projections and mutations to expose exact version history/current status and explicit legacy ambiguity, adding no send operation or external-delivery claim.
- [x] Add focused Core/integration/migration/concurrency tests covering retention, lineage, replay, stale version, ambiguity/hash mismatch, reassociation history, conservative legacy migration, and staff/Worker races.
- [x] Run locked restore, Release build, focused/full tests, migration inspection and negative source checks; run the simplification pass, update current-state docs only at the proved tier, and write the post-implementation report.

## Progress notes

2026-08-28: Implementation and validation complete on the DOCS-003 task branch. Reused DOCS-001 `AssessmentReportVersion`, `AssessmentReportArtifact`, `AssessmentReportVersionStore`, the existing approved-mailbox evidence store, workflow commands, query projection, and role bootstrap matrix. Added only the missing version/evidence ledger and history slice. Legacy rows remain explicit `Unresolved`; no version is inferred.

Validation:
- `dotnet restore ./Pegasus.slnx --locked-mode` — passed; locked assets unchanged.
- `dotnet build ./Pegasus.slnx --configuration Release --no-restore /nr:false` — passed; 0 warnings, 0 errors.
- `dotnet test ./Pegasus.slnx --configuration Release --no-build --filter "Category!=Corpus" /nr:false` — Core 939 passed; API 12 passed; architecture 111 passed; integration 1,017 passed; 2 documented skips; 0 failures.
- Focused persistence/migration/grant tests and Core auto-link tests — passed before the full suite; the full suite revalidated the final branch.
- Simplification pass — completed 2026-08-28; reuse, simplification, reliability, scope, and altitude findings are recorded in the plan; no finding deferred.
- No mailbox, Graph, Box, cloud, deployment, credential, or upstream write was performed.
