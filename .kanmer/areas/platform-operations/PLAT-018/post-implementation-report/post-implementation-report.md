# Post-implementation report — PLAT-018

## Result

Implemented the composition-root runtime-grant architecture gate at `f171eadb2db862a3fb4ec279b08509b90ae30c21`.

The test derives registered EF stores, maps their model entities to tables, detects supported INSERT/DELETE writes, identifies the Web or Worker composition-root role, and compares each role/table/verb against the existing migration grant shapes. It also proves the three named historical regressions and a forward ungranted-table fixture. The current-architecture snapshot records the new coverage.

## Files

- `tests/Pegasus.ArchitectureTests/RuntimeGrantCompositionTests.cs`
- `docs/current-architecture.md`

No migration, grant script, source composition root, CI, cloud, upstream, or deployment file changed.

## Validation

- Release architecture build: passed, 0 warnings/errors.
- Focused tests: 6 passed, 0 failed.
- Full architecture suite: 117 passed, 0 failed, 0 skipped.
- `pwsh ./scripts/Test-MigrationGrants.ps1`: 71 migration files checked successfully.
- `git diff --check`: passed.
- Worktree clean; branch pushed to `origin/task/dsk-10-18-runtime-grant-composition-gate`.

## Simplification

The required pass covered reuse, simplification, efficiency, and altitude. One redundant `StartsWith('E')` precheck was removed; no other behavior-preserving change was justified.

## Remaining delivery

Independent review of final HEAD and the task PR are still required. No deployment or cloud proof is claimed.

## Remediation update — 2026-08-28

Final implementation HEAD is `b29466a87f44d6187e0fdf55f5dfc65d30e5a7f3`. The review findings were addressed without expanding the two-file scope: direct Web/Worker registrations and role-specific matching are covered; opt-out markers are applied by the evaluator and tested; historical regressions and the forward fixture use the real inference/evaluator path; grant scanning matches the existing whole-folder literal/tuple semantics; and the architecture wording matches the implemented INSERT/UPDATE/DELETE detection.

Final validation: Release build 0 warnings/errors; focused PLAT-018 tests 6 passed; full architecture suite 117 passed, 0 failed, 0 skipped; `Test-MigrationGrants.ps1` 71 migration files passed; `git diff --check` passed; worktree clean and branch pushed. Fresh independent review is required; PR #36 remains held from merge until it passes.

## Remediation 2 — 2026-08-28

Exact implementation HEAD `3a644ed5258d365fec8ce17c9ca743a9f86ac3ad` is pushed and clean. It addresses the second independent review's three acceptance gaps: EF model table metadata via `IModel`/`GetTableName()`, migration grant parsing aligned to the existing whole-folder literal/tuple check, and real registration/entity plus historical evaluator fixtures. Validation passed: Release architecture build with 0 warnings/errors; focused tests 6/6; full architecture suite 117/117; `Test-MigrationGrants.ps1` checked 71 migration files; `git diff --check`. Fresh independent review is pending; no merge or deployment proof is claimed.

## Independent re-review 2 — 2026-08-28

Review of exact HEAD `3a644ed5258d365fec8ce17c9ca743a9f86ac3ad` returned FAIL. The reviewer identified missing concrete-only store registration coverage, incomplete UPDATE inference for tracked/raw-SQL writes, a non-genuine forward fixture, incorrect tuple role attribution for a shared ImageIntake grant array, and overstated architecture documentation. PR #36 remains held; remediation and a fresh independent review are required.

## Remediation 3 — 2026-08-28

Exact HEAD `87933e0784cd2836dd043535b95346e30eaf4288` is clean and pushed. The five findings from the prior review were addressed within the assigned test and architecture-document files: concrete factory registrations, tracked/raw-SQL update coverage, inference-backed forward fixture, shared runtime-role tuple attribution, and truthful documentation. Reported validation is green (Release build 0 warnings/errors, focused 6/6, full architecture 117/117, migration scan 71/71, diff check). A new independent read-only review is pending; no merge or deployment proof is claimed.

## Independent re-review 3 — 2026-08-28

Review of exact HEAD `87933e0784cd2836dd043535b95346e30eaf4288` returned FAIL. Role closure, structural mutation inference, forward and historical fixture authenticity, opt-out reason enforcement, and documentation truthfulness remain insufficient. PR #36 remains held; another bounded remediation and fresh independent review are required.

## Remediation 4 — 2026-08-28

Exact HEAD `16d96600a041ef3ae54a71d59dfb5ccb9b86596f` is clean and pushed. Reported validation is green: Release build 0 warnings/errors, focused 7/7, full architecture 118/118, migration scan 71/71, and diff check. The implementation remains test/docs-only and no merge or deployment proof is claimed pending fresh independent review.

## Independent re-review 4 — 2026-08-28

Review of exact HEAD `16d96600a041ef3ae54a71d59dfb5ccb9b86596f` returned FAIL. Transitive DI role closure, structural mutation inference, genuine forward/historical fixtures, exact tuple/parser parity, and documentation truthfulness remain unresolved. PR #36 remains held pending bounded remediation and fresh independent review.

## Remediation 5 — 2026-08-28

Exact HEAD `7b084329f3974acf6b4b47d92cdc6eff9a09243a` is clean and pushed with reported green validation (Release build, focused 7/7, full architecture 118/118, migration scan 71/71, diff check). PR #36 remains held pending fresh independent review.

## Remediation 6 status — 2026-08-28

HEAD `05b066df1613eff31d8e7d0b4e107a453c3e811a` is clean and locally green, but the implementer reports unresolved acceptance gaps in transitive/Core-mediated role closure, immutable historical registration fixtures, and differential tuple semantics. No independent review or merge is claimed.

## Scope correction — 2026-08-28

The ticket-owned test scope is expanded only as needed to make the acceptance evidence genuine: test-only syntax dependency metadata, immutable test fixtures, and test-only analyzer helpers are permitted. Production code, migrations, migration script, CI workflow, cloud, upstream, and deployment remain unchanged.

## Final remediation and combined validation — 2026-08-28

Exact implementation HEAD `2d069f0a6f7ea01564b6fdf3fac7efedbfad1f8b` is clean and pushed to `origin/task/dsk-10-18-runtime-grant-composition-gate`. The final bounded diff contains only the test package metadata, runtime-grant composition test/helper, immutable hashed fixtures, and the existing architecture snapshot. The final test helper normalizes CRLF to LF before hashing fixtures and the grant parser ignores prose “grant” text by requiring a real permission-bearing `GRANT ... ON` statement.

A temporary local combined validation tree merged exact PLAT-018 `2d069f0a` with exact PLAT-030 `c599a42b` without pushing or changing either task branch. It passed focused runtime-grant tests 8/8, the full architecture suite 119/119, `Test-MigrationGrants.ps1` for 72 migration files, `Test-AzureDeploymentPlan.ps1 -Mode Local`, and `git diff --check`. The focused branch-only test remains dependent on PLAT-030's grant migration; this dependency is explicit and not treated as missing evidence.

No cloud, deployment, credential, corpus, or upstream operation occurred. Fresh independent review is required before PR #36 can merge.

## Final lock and validation correction — 2026-08-28

Final implementation HEAD is `aaa025f41d9e60a6ed78c256a14832e014199c8c`, clean and pushed to `origin/task/dsk-10-18-runtime-grant-composition-gate`. The two Web/Infrastructure package lock files were refreshed after PR CI identified the locked-restore representation change caused by the centrally managed test-only Roslyn package.

A temporary local validation tree combined exact PLAT-018 `aaa025f4` with exact PLAT-030 `c599a42b` and passed locked restore, focused runtime-grant tests 8/8, full architecture tests 119/119, `Test-MigrationGrants.ps1` 72/72, `Test-AzureDeploymentPlan.ps1 -Mode Local`, and `git diff --check`. The temporary merge was not pushed. Fresh exact-head CI and independent review remain required; no merge or proof is claimed.
