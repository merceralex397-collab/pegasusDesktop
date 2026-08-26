# Plan — INTK-008

## Objective

Make `GroupedImageIntakeConcurrencyTests.ConcurrentGroupMembersNeverSplitAcrossRepeatedRuns` faithfully retry only the SQL Server deadlock that the test intentionally provokes, when EF Core wraps it before it reaches the helper.

## Research evidence

- Exact-head PR #14 run `32959758190` failed `sql-integration (2)`; authorized failed-job rerun job `98152798225` failed identically.
- The failing test is `GroupedImageIntakeConcurrencyTests.ConcurrentGroupMembersNeverSplitAcrossRepeatedRuns`.
- The hosted stack is `InvalidOperationException` → `DbUpdateException` → `Microsoft.Data.SqlClient.SqlException` with error 1205, at `EfIntakeWorkStore.CompleteProcessingAsync`.
- The test helper `ProcessWithDeadlockRetryAsync` currently catches only `SqlException` directly. The 1205 therefore escapes the intended bounded queue-redelivery retry.
- The test is unchanged by DOCS-001's report-generation diff. Historical commit `777d2762` identifies the grouped-image race test as INTK-011; no current Kanmer ticket for INTK-011 exists, so this focused in-repository CI ticket owns only the test-harness correction.
- Local baseline focused run on `task/intk-008-deadlock-retry`, before edits: `dotnet restore ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --locked-mode` passed; `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --filter "FullyQualifiedName~GroupedImageIntakeConcurrencyTests" --no-restore` passed 2/2 in 56 seconds. This does not disprove the hosted intermittent failure.

## Approach

1. Update the existing helper's exception filter to traverse the actual exception chain and identify only `SqlException.Number == 1205`.
2. Preserve the existing maximum attempt bound and delay; non-deadlock exceptions remain unhandled and fail immediately.
3. Add no production retry policy and no new abstraction.
4. Run the focused test, the relevant repository validation, and the required Release build/test profile as proportionate.
5. Run the simplification pass over the one-file test diff and record the disposition.
6. Obtain independent `pegasus-desktop-reviewer` review of the test-only diff.
7. Open PR to `dev`; merge only when review passes and exact-head CI is green. Rerun PR #14's exact head after this fix is merged to `dev`; do not claim DOCS-001 clear until its own head's required checks are green.

## Acceptance criteria

- The helper retries an EF-wrapped SQL Server deadlock 1205 within its existing bounded attempt count.
- Other exception types and non-1205 SQL errors are not retried or swallowed.
- The grouped-image concurrency test passes its full two-test focused class locally.
- No production source file or runtime behavior changes.
- The fix is independently reviewed and exact-head CI is green before it is used to clear PR #14.

## Verification

- `dotnet restore ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --locked-mode`
- `dotnet build --configuration Release --no-restore`
- `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --filter "FullyQualifiedName~GroupedImageIntakeConcurrencyTests" --no-restore`
- `git diff --check`
- GitHub Actions exact-head checks for this PR and then PR #14 after the fix lands in `dev`.

## Simplification pass

To be recorded after implementation over this branch's own diff. The intended result is a single small exception-chain predicate local to the existing helper; no reusable cross-production abstraction is justified by one test caller.
