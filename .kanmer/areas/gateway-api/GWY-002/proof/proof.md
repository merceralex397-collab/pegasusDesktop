# Proof — GWY-002

## Delivery identity

- PR #28 merged into `dev` at `2026-08-27T00:42:48Z`; merge commit: `6e14eae9ef6b682fdf4aa5a54287ae6113d274da`.
- The exact reviewed PR head was `920dad00427585a974208a7c95725edc4780d204`.
- The `920dad00` and `6e14eae9` trees are identical.
- The exact reviewed PR head had independent Hilbert review: **PASS**, no actionable findings.
- Exact-head GitHub Actions run `33026927409` completed successfully: changes, documentation, local-development-scripts, reference-data, unit, browser, SQL integration shards 1–3, and SQL integration coverage passed; infrastructure was expectedly skipped.

## Post-merge main verification

Executed in a clean detached worktree at `refs/remotes/origin/main`, resolving to `6e14eae9ef6b682fdf4aa5a54287ae6113d274da`:

- `dotnet build Pegasus.slnx --configuration Release -nr:false` — **Build succeeded; 0 Warning(s), 0 Error(s)**.
- `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --filter 'FullyQualifiedName~DesktopGateway' --no-build -nr:false` — **19 passed, 0 failed, 0 skipped**.
- `dotnet test ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj --configuration Release --no-build -nr:false` — **110 passed, 0 failed, 0 skipped**.
- Static checks — exactly one `AddProblemDetails(` registration, exactly one `"Features:DesktopGateway"` literal, exactly one `"/api/v1"` literal; `git diff --check` clean.
- The corrected shard-accounting check was exercised locally: shard 2 reported **302 assigned; 301 passed, 1 expected corpus skip, 0 failed; all 302 assigned tests ran**.

## Acceptance disposition

The feature gate leaves no `/api/v1` endpoint when absent/false; the enabled endpoint-free group, correlation boundary, problem translation, unmatched-404 problem response, host-level exception handling, and client-version extension point are covered by the merged integration tests. Existing host behavior is covered by the full exact-head CI matrix and the merged-main build/architecture checks. No Azure/cloud write or deployment was performed.
