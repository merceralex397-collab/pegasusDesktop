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
