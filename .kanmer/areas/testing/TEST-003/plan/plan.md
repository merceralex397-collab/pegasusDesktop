# Plan — TEST-003 Integration shard persistence paths

## Governing documents

Use the existing integration-test and local-stack conventions; this fix remains docs_todo until canonical desktop ownership is linked.

## Steps

1. Inventory current api-v1 persistence coverage and map each missing case to one existing shard.
2. Extend the shard using current LocalDB fixtures and shared endpoint contracts.
3. Run each shard plus VerifyPartition with the detected runner syntax.
4. Confirm no filter returns a false green zero-test result and record exact output.

## Verification

- [x] All affected shards pass.
- [x] VerifyPartition passes and each test is assigned once.
- [x] No live Azure dependency is introduced.

## Read-only dependency audit — 2026-08-27

- Target commit: `origin/dev` at `ae66cbf6fccff7b7ac15805fec89c663bd25f730` (the same exact SHA currently on `origin/main`).
- `git grep -n '/api/v1' origin/dev -- ':!docs/**' ':!*.json' ':!*.md'` found only the gateway base-path constant and the contract-test catalogue. The committed OpenAPI snapshot has `"paths": {}`.
- `src/Pegasus.Web/Api/DesktopGatewayExtensions.cs` maps an empty `/api/v1` group; `GWY-002` is the completed composition skeleton, not a command implementation. There is no real POST, PUT, PATCH, or DELETE desktop command route in the target commit for an integration test to call.
- The live Kanmer route-producing tickets `GWY-007` through `GWY-015` are not done (and the related feature slices are not merged). Their work is the missing prerequisite, not a reason to invent test-only endpoints.
- Adding a zero-test placeholder or testing Razor/Core commands under this ticket would not satisfy the acceptance criteria: each new test must prove a real `/api/v1` persistence path, actor/history/outbox/version effects, and exactly-one shard assignment. The correct disposition is to hold TEST-003 until the first applicable command route is merged, then re-run this inventory and add only the routes that exist.

## Current disposition

Implementation is complete on commit `f85e5236a27bfbda91278569ef46743776fc3160`. The ticket is in Review on its own branch and worktree. The live route inventory was re-run after `FEAT-029` merged; this ticket covers only the six retained-mail writes that exist on `origin/dev`, with no placeholders for absent future route groups. Final exact-head shard evidence was rerun after the commit and recorded below.

## Live route recheck — 2026-08-30

- Refreshed the configured `origin` remote only; `origin/dev` is `69a88bbdc0f9a223f8c7da60e24277b664d4b495`.
- `FEAT-029` is merged and the current desktop gateway exposes these retained-mail writes: `POST /api/v1/mail/{messageId}/link-case/prepare`, `unlink-case/prepare`, `link-case`, `unlink-case`, `classification`, and `move-to-recommended-folder`.
- `TEST-002` is done. No other `/api/v1` route groups are present in this current checkout.
- This execution added one self-contained `MailPersistenceTests` class for the landed mail writes only; it did not fabricate future cases, received, uploads, vehicle, assessment, or administration route tests. The older blocked disposition is superseded by this measured route recheck; absent future routes remain outside scope.

## Simplification pass — 2026-08-30

- Reused the existing `IntakeWebApplicationFactory`, LocalDB template fixture, EF context factory, `ImageIntakeTestData.SeedCaseAsync`, retained-mail policy, and existing folder-mover contract; no second factory or production abstraction was introduced.
- Kept the change to one self-contained `MailPersistenceTests` class so the shard assigner owns it as one unit. The class contains five tests for the six landed retained-mail writes plus the concurrent conflict path; absent future route groups remain unrepresented rather than receiving placeholders.
- Assertions read the persisted association, lease, classification, folder-move, mutation-history, and action-history rows and preserve the repository's actual actor/outcome representations. The recording folder mover is the smallest deterministic test seam needed for the successful move command.
- No simplification finding was left unapplied. No shard script, CI matrix, production code, Azure resource, or corpus file changed; the explicitly planned existing testing-area row was updated.
- Final exact-head validation after commit `f85e5236a27bfbda91278569ef46743776fc3160`: Release build passed with 0 warnings/0 errors; the four focused tests passed; shard 1 passed 330/330 in 5m02s, shard 2 passed 323/324 with one pre-existing skip in 4m39s, and shard 3 passed 325/326 with one pre-existing skip in 6m47s; VerifyPartition covered all 980 enumerated tests exactly once. The 11 exact disposable databases created by this post-commit rerun were independently verified after testhost exit and dropped; the older `Pegasus_Test_29a0ec4012034d2591bd9570dab5670f` database was not touched because it predates this run and remains protected by the runbook's one-day floor. A follow-up exact-name census for the 11 run databases returned zero rows.

## Review findings disposition — 2026-08-30

- **Missing concurrency coverage:** fixed by adding `ConcurrentLinkCommandsCommitExactlyOneMutationAndReturnConflictToLoser`. It sends two API callers concurrently against one isolated LocalDB-backed factory, asserts exactly one `200` and one `409`, and verifies one association mutation plus the receipt/case version increments. The loser presents the post-commit version precondition so the domain contract is deterministic; an exploratory two-valid-request race exposed an existing SQL transient/deadlock path that returns generic 400 and is not silently treated as a passing 409 or changed in this tests-only ticket.
- **Missing link/unlink API audit assertions:** fixed. The sequential association test now queries `ActionHistory` by `mail_api` aggregate, message id, event kind, and operation correlation key, and asserts successful outcome, actor, and null failure reason for both operations.
- **Unticked evidence/checklists and area row:** fixed through Kanmer ticket-body checklist updates and the existing `docs/desktop/08-testing/README.md` DSK-08-03 row now records retained-mail persistence, lease, audit, and concurrent-conflict coverage.
- **Independent reproduction:** the reviewer reproduced the corrected four-test focus and VerifyPartition result; the full three-shard commands were rerun after commit `f85e5236a27bfbda91278569ef46743776fc3160`, with literal results recorded in the post-implementation report. The remaining handoff is a fresh review of this exact commit.
