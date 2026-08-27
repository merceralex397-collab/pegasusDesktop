# Proof — TEST-002 Authorization and failure-path test template

## Merged delivery

- Independent reviewer: Hilbert, exact PR head `3e0fe8c7c444bfab2427f83611459cc186cec3c8`, PASS after the six findings were remediated.
- PR: #31, merged into `dev` at `2026-08-27T17:47:25Z`.
- Merge commit: `ae66cbf6fccff7b7ac15805fec89c663bd25f730`.
- Exact-SHA promotion: `origin/dev` and `origin/main` both verified at `ae66cbf6fccff7b7ac15805fec89c663bd25f730`.

## CI evidence

Exact reviewed-head run `33098778132` passed:

- changes
- documentation
- local-development-scripts
- reference-data
- unit
- browser
- sql-integration (1)
- sql-integration (2)
- sql-integration (3)
- sql-integration-coverage

The infrastructure job was skipped by its path condition; no infrastructure path was changed.

## Merged-main validation

A detached worktree at the exact `origin/main` SHA was clean and verified with:

- `dotnet restore ./Pegasus.slnx --locked-mode` — passed.
- `dotnet build ./Pegasus.slnx --configuration Release --no-restore -nr:false --nologo` — passed with 0 warnings and 0 errors.
- `dotnet test ./tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj --configuration Release --no-build --no-restore -nr:false --logger "console;verbosity=minimal" --filter "Category=Contract"` — 12/12 passed.
- `git diff --check` — passed.

The branch validation before merge also passed the canonical non-corpus solution suite: 2,016 passed, 2 existing explicit skips, 0 failed; exact-head CI supplied the final post-remediation full lanes.

## Acceptance evidence

- The catalogue derives command routes from the real host `EndpointDataSource` and selects only POST/PUT/PATCH/DELETE routes under `/api/v1`.
- The merged host inventory has zero command endpoints because command routes are not yet merged. The literal row table is therefore intentionally empty; no authentication, command handler, or business rule was fabricated.
- The symmetric guard passes for the normal host and reports a test-only host-mapped `POST /api/v1/__probe` until a reviewed row exists.
- The five required row-driven theories enforce unauthenticated, wrong-right, stale-version/current-version, invalid-request exact-title, and operation-key replay contracts when future concrete rows exist. Rows must supply replay/version evidence symmetrically and expected post-replay state.
- Only the scoped contract-test harness and one testing-programme documentation line changed; no product endpoint, cloud, deployment, upstream, or corpus state changed.
