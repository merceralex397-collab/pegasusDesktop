2026-08-27 read-only audit: origin/dev ae66cbf has the empty DesktopGateway /api/v1 group and OpenAPI paths {}. No real command route exists yet; GWY-007..015 are the live route-producing prerequisites. No test-only endpoints or zero-test placeholders added. Leaving TEST-003 Preparing and unclaimed pending the first merged command route.

2026-08-30 — Final implementation and validation checkpoint.

Added `tests/Pegasus.IntegrationTests/MailPersistenceTests.cs` only. The class exercises the six retained-mail writes currently landed on `origin/dev`: link-case prepare/link, unlink-case prepare/unlink, classification correction, and move-to-recommended-folder. It asserts HTTP result plus the persisted domain row, actor, operation/reason, lease operation where applicable, mutation/action history, and version increments. Each test creates its own `IntakeWebApplicationFactory`/LocalDB database; no test relies on another class's state.

Validation:
- `dotnet build ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --no-restore` — passed, 0 warnings, 0 errors.
- Exact fully-qualified association test — passed (1/1), including both `CaseEditLeaseOperations` rows.
- Exact three-test MailPersistenceTests filter — passed (3/3).
- Final shard 1 — passed 329/329 in 3m58s.
- Final shard 2 — passed 323, skipped 1 pre-existing (`CustodyOutboxIntegrationTests.AcceptedCaseRetainsEmbeddedPhotographsBesideTheSource`), total 324 in 4m57s.
- Final shard 3 — passed 325, skipped 1 pre-existing (`QdosMappingExtractionTests.MappedInstructionEmailExtractsItsDocumentedFieldSet`), total 326 in 6m35s.
- `pwsh ./scripts/Invoke-TestShard.ps1 -VerifyPartition -ArtifactRoot ./artifacts/test-shards -ShardCount 3` — passed: 3 shards covered all 979 enumerated tests exactly once.
- Final LocalDB census found eight exact disposable databases created during this rerun, with no testhost process active. They were explicitly verified by exact name and creation time, dropped through the normal ALTER SINGLE_USER/DROP path, and a subsequent census returned zero of those names; no `Pegasus_Test_*.bak` file was present.

The dated simplification pass is recorded in the plan. No production code, shard script, CI matrix, docs, Azure resource, or corpus file changed. Next: create the post-implementation report, pass the gated Review move, obtain independent review, open the PR to `dev`, and merge only after exact-head CI is green.

2026-08-30 — Review remediation checkpoint.

Independent review of commit `3c596803` failed on missing concurrency coverage, missing link/unlink API ActionHistory assertions, unticked ticket evidence, stale area-plan row, and lack of independent reproduction. Addressed all findings in commit `f85e5236`:
- Added API ActionHistory assertions for `mail_case_link` and `mail_case_unlink`.
- Added `ConcurrentLinkCommandsCommitExactlyOneMutationAndReturnConflictToLoser`, sending two callers concurrently and asserting one 200, one 409, one mutation, and one version increment. A fully-valid simultaneous race was also tried; it exposed an existing provider transient/deadlock returned as generic 400, so it is recorded as an honest out-of-scope production concern rather than claimed as a passing 409.
- Updated the existing `docs/desktop/08-testing/README.md` DSK-08-03 row.
- Checked all eight ticket acceptance/verification rows through Kanmer.

Final validation after remediation:
- Release build: 0 warnings/errors.
- Focused four-test filter: 4/4 passed.
- Shard 1: 330/330 passed.
- Shard 2: 323 passed, 1 pre-existing skip, 324 total.
- Shard 3: 325 passed, 1 pre-existing skip, 326 total.
- VerifyPartition: all 980 enumerated tests exactly once.
- Exact cleanup: 9 exact disposable databases (8 final-rerun plus 1 exploratory-run residue) dropped after testhost exit; follow-up exact-name census returned zero and no backup file was present.

Fresh post-implementation report version `db1fe6a0f80e17f8` now records commit `f85e5236` and the concurrency disposition. Ready for fresh independent review.
