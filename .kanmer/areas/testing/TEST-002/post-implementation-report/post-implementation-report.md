# Post-implementation report — TEST-002

## Implemented

- Added a test-side `EndpointDataSource` catalogue for POST/PUT/PATCH/DELETE routes under `/api/v1`.
- Added a symmetric literal command coverage table and guard. The table is intentionally empty for the verified merged host because no command endpoint is currently present; a future command without a reviewed row fails the guard with its method and route.
- Added reusable row-driven theories for unauthenticated, wrong-right, stale-version, invalid-request, and idempotent-replay contracts. Their endpoint-specific requests and effect snapshots are supplied by future concrete rows; the zero-row placeholder is not part of the table and performs no HTTP call.
- Added a derived-host `POST /api/v1/__probe` guard test and shared problem/challenge/equality/effect assertions.
- Updated `docs/desktop/08-testing/README.md` to document TEST-002 and the future row-extension rule.
- No product endpoint, authentication pipeline, Core policy, database, cloud, deployment, upstream, or corpus change was made.

## Verified current applicability

The merged `origin/dev` host was inspected through the existing `ContractTestWebApplicationFactory` and `EndpointDataSource`: it has zero POST/PUT/PATCH/DELETE command endpoints under `/api/v1`. GWY-003 and GWY-021 are not merged, so no command-specific authorization or persistence behavior exists for this ticket to execute against. The harness therefore establishes the enforcement and theory shape without fabricating command rows or claiming runtime authorization coverage that cannot yet run.

## Validation

| Command | Result |
| --- | --- |
| `dotnet restore ./Pegasus.slnx --locked-mode` | Passed; packages up to date. |
| `dotnet build ./tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj --configuration Release --no-restore -nr:false --nologo` | Passed; 0 warnings, 0 errors. |
| `dotnet test ./tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj --configuration Release --no-build --no-restore -nr:false --logger "console;verbosity=minimal"` | Passed; 12/12. |
| `dotnet build ./Pegasus.slnx --configuration Release --no-restore -nr:false --nologo` | Passed; 0 warnings, 0 errors. |
| `dotnet test ./Pegasus.slnx --configuration Release --no-build --filter "Category!=Corpus"` | Passed; 2,016 passed, 2 skipped, 0 failed (Core 935, Architecture 110, Contract 12, Integration 959). |
| `git diff --check` | Passed. |

The two skipped integration tests are existing explicitly skipped cases; this ticket did not alter them. Independent review, PR CI, merge, exact-SHA main proof, and Kanmer closeout remain outstanding.

## Review remediation — 2026-08-27

The independent review identified six merge-readiness findings. All implementation findings are now addressed: the probe uses a derived real host and `EndpointDataSource`; versioned rows carry current-version evidence and stale tests assert it; the guard validates operation-key/replay and version/stale-row symmetry; problem assertions require exact titles; and replay asserts the row-supplied post-state plus one new history entry. The focused contract suite after remediation passes 12/12. The exact-head CI rerun remains the outstanding external validation until it is terminal and green.


## Final merged evidence — 2026-08-27

- Independent Hilbert review of exact head `3e0fe8c7c444bfab2427f83611459cc186cec3c8`: PASS after all six findings were remediated.
- PR #31 merged into `dev` as `ae66cbf6fccff7b7ac15805fec89c663bd25f730`.
- Exact-head repository-check run `33098778132` passed changes, documentation, local-development-scripts, reference-data, unit, browser, SQL integration shards 1–3, and SQL integration coverage; infrastructure was skipped by its path condition.
- Detached merged-main verification at `ae66cbf6fccff7b7ac15805fec89c663bd25f730`: locked restore passed; Release solution build passed with 0 warnings/0 errors; contract tests passed 12/12; `git diff --check` passed.
