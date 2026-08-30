# Plan — TEST-003 Integration shard persistence paths

## Governing documents

Use the existing integration-test and local-stack conventions; this fix remains docs_todo until canonical desktop ownership is linked.

## Steps

1. Inventory current api-v1 persistence coverage and map each missing case to one existing shard.
2. Extend the shard using current LocalDB fixtures and shared endpoint contracts.
3. Run each shard plus VerifyPartition with the detected runner syntax.
4. Confirm no filter returns a false green zero-test result and record exact output.

## Verification

- [ ] All affected shards pass.
- [ ] VerifyPartition passes and each test is assigned once.
- [ ] No live Azure dependency is introduced.

## Read-only dependency audit — 2026-08-27

- Target commit: `origin/dev` at `ae66cbf6fccff7b7ac15805fec89c663bd25f730` (the same exact SHA currently on `origin/main`).
- `git grep -n '/api/v1' origin/dev -- ':!docs/**' ':!*.json' ':!*.md'` found only the gateway base-path constant and the contract-test catalogue. The committed OpenAPI snapshot has `"paths": {}`.
- `src/Pegasus.Web/Api/DesktopGatewayExtensions.cs` maps an empty `/api/v1` group; `GWY-002` is the completed composition skeleton, not a command implementation. There is no real POST, PUT, PATCH, or DELETE desktop command route in the target commit for an integration test to call.
- The live Kanmer route-producing tickets `GWY-007` through `GWY-015` are not done (and the related feature slices are not merged). Their work is the missing prerequisite, not a reason to invent test-only endpoints.
- Adding a zero-test placeholder or testing Razor/Core commands under this ticket would not satisfy the acceptance criteria: each new test must prove a real `/api/v1` persistence path, actor/history/outbox/version effects, and exactly-one shard assignment. The correct disposition is to hold TEST-003 until the first applicable command route is merged, then re-run this inventory and add only the routes that exist.

## Current disposition

Implementation is intentionally not started. The ticket remains in Preparing and unclaimed while the route dependency is absent. Next action: after a real `/api/v1` command ticket merges to `dev`, refresh the board and target SHA, re-audit the endpoint map, then take TEST-003 and add the corresponding LocalDB persistence tests and partition evidence.

## Live route recheck — 2026-08-30

- Refreshed the configured `origin` remote only; `origin/dev` is `69a88bbdc0f9a223f8c7da60e24277b664d4b495`.
- `FEAT-029` is merged and the current desktop gateway exposes these retained-mail writes: `POST /api/v1/mail/{messageId}/link-case/prepare`, `unlink-case/prepare`, `link-case`, `unlink-case`, `classification`, and `move-to-recommended-folder`.
- `TEST-002` is done. No other `/api/v1` route groups are present in this current checkout.
- This execution will add one self-contained `MailPersistenceTests` class for the landed mail writes only; it will not fabricate future cases, received, uploads, vehicle, assessment, or administration route tests. The older blocked disposition below is superseded for this execution by this measured route recheck; absent future routes remain outside scope.

## Simplification pass — 2026-08-30

- Reused the existing `IntakeWebApplicationFactory`, LocalDB template fixture, EF context factory, `ImageIntakeTestData.SeedCaseAsync`, retained-mail policy, and existing folder-mover contract; no second factory or production abstraction was introduced.
- Kept the change to one self-contained `MailPersistenceTests` class so the shard assigner owns it as one unit. The test class contains only the six landed retained-mail writes; absent future route groups remain unrepresented rather than receiving placeholders.
- Assertions read the persisted association, lease, classification, folder-move, mutation-history, and action-history rows and preserve the repository's actual actor/outcome representations. The recording folder mover is the smallest deterministic test seam needed for the successful move command.
- No simplification finding was left unapplied. No shard script, CI matrix, production code, documentation, Azure resource, or corpus file changed.
- Final validation: Release build passed with 0 warnings/0 errors; all three focused tests passed; final shards passed 329/329, 323/324 (one pre-existing skip), and 325/326 (one pre-existing skip); VerifyPartition covered all 979 enumerated tests exactly once. The eight exact disposable databases created by this final rerun were independently verified as offline test-owned residue and dropped; a subsequent exact-name census returned zero rows and no `Pegasus_Test_*.bak` files were present.

## Review findings disposition — 2026-08-30

- **Missing concurrency coverage:** fixed by adding `ConcurrentLinkCommandsCommitExactlyOneMutationAndReturnConflictToLoser`. It sends two API callers concurrently against one isolated LocalDB-backed factory, asserts exactly one `200` and one `409`, and verifies one association mutation plus the receipt/case version increments. The loser presents the post-commit version precondition so the domain contract is deterministic; an exploratory two-valid-request race exposed an existing SQL transient/deadlock path that returns generic 400 and is not silently treated as a passing 409 or changed in this tests-only ticket.
- **Missing link/unlink API audit assertions:** fixed. The sequential association test now queries `ActionHistory` by `mail_api` aggregate, message id, event kind, and operation correlation key, and asserts successful outcome, actor, and null failure reason for both operations.
- **Unticked evidence/checklists and area row:** fixed through Kanmer ticket-body checklist updates and the existing `docs/desktop/08-testing/README.md` DSK-08-03 row now records retained-mail persistence, lease, audit, and concurrent-conflict coverage.
- **Independent reproduction:** rerunning the exact final focused set and all three shards is in progress after these changes. The report will be replaced with the final commit SHA and literal results before the next review handoff.
