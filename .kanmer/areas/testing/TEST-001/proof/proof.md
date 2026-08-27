# Proof — TEST-001 API contract-test project

## Merged revision

- PR: https://github.com/merceralex397-collab/pegasusDesktop/pull/29
- PR state: MERGED on 2026-08-27
- Target: dev, then exact fast-forward promotion to main
- Merged main commit: c2939f7e7301b36d5c93eccff498550b76d9a87a
- The merged main commit is the same commit verified in the proof worktree at HEAD.

## Review and CI

- Independent reviewer: Hilbert (pegasus-desktop-reviewer), a non-implementing agent.
- Review verdict: PASS for PR #29 at implementation head ee9cba4d8c15e1a1e6c89b3f4941f84cc2c0f5e4.
- GitHub Actions: run 33040668468, attempt 2, green at that exact implementation head. All repository-check jobs passed, including unit, browser, all three SQL shards, SQL coverage, documentation, infrastructure, reference-data, local-development-scripts, and changes.
- The first attempt's isolated SQL post-login timeout was rerun as the failed job; the retry passed. No code change was made for that infrastructure failure.

## Merged-main validation

Executed in detached worktree C:\Users\PC\Documents\GitHub\pegasus-worktrees\test-001-proof at merged main commit c2939f7e7301b36d5c93eccff498550b76d9a87a:

- dotnet restore ./Pegasus.slnx --locked-mode — passed.
- dotnet build ./Pegasus.slnx --configuration Release --no-restore -nr:false — passed; 0 warnings, 0 errors.
- dotnet test ./tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj --configuration Release --no-build --filter "Category=Contract" -nr:false — passed; 1 passed, 0 failed, 0 skipped.
- dotnet test ./Pegasus.slnx --configuration Release --no-build --filter "Category=Contract" -nr:false — passed; Contract 1 passed, other projects had no matching tests.
- git diff --check — passed.
- The proof worktree was clean after validation.

## Acceptance boundary

The delivered slice is the single locked, solution-registered WebApplicationFactory contract-test scaffold and deterministic host-composition smoke fact. OpenAPI snapshot/export, Kiota generated-client freshness, endpoint authorization/failure matrices, and persistence cases remain with GWY-004, GWY-005, TEST-002, and TEST-003; they are not claimed by TEST-001.
