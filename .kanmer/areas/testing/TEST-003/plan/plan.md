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
