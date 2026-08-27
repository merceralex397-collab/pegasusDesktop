# Proof

## Merged revision

- Remote: configured `origin`
- `origin/main`: `ae2ce74a8eea31232203971415fe6b652c89ea84`
- `origin/dev`: `ae2ce74a8eea31232203971415fe6b652c89ea84`
- Promotion: documented atomic exact-SHA lease-guarded promotion; `origin/main` was confirmed an ancestor of `origin/dev`, and post-push read-back confirmed both refs equal the reviewed SHA.
- PR: #30, merged into `dev` at `ae2ce74a8eea31232203971415fe6b652c89ea84`'s resulting merge commit.
- Exact-head CI: run `33048018900` for `ff2fb8bf4e14bc3e58c22d4864ca83e33ec32448`; required changes, documentation, scripts, reference-data, browser, unit, all three SQL integration shards, and coverage jobs passed. Infrastructure was skipped as expected.

## Merged-main validation

Executed in a clean detached worktree at `origin/main`:

- `dotnet restore ./Pegasus.slnx --locked-mode` — passed.
- `dotnet build ./Pegasus.slnx --configuration Release --no-restore -nr:false` — passed; 0 warnings, 0 errors.
- `pwsh ./eng/api/Export-OpenApiDocument.ps1` — passed.
- `git diff --exit-code -- openapi/` — passed; regeneration produced no diff.
- Snapshot hashes — current and previous both `DF3761703FB4122C4E173D091BB6D654D49DA4EEF2895952F5180E8E998395E4`.
- `dotnet test ./tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj --configuration Release --no-restore --filter "Category=Contract" -nr:false` — 5/5 passed.
- `dotnet test ./tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj --configuration Release --no-restore --no-build -nr:false` — 5/5 passed.
- `dotnet test ./tests/Pegasus.Core.Tests/Pegasus.Core.Tests.csproj --configuration Release --no-restore --no-build -nr:false` — 935/935 passed.
- `dotnet test ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj --configuration Release --no-restore --no-build -nr:false` — 110/110 passed.
- Final proof worktree status — clean.

## Review and acceptance

- Independent reviewer Hilbert, who did not implement GWY-004, gave final `PASS — GWY-004 / PR #30 is merge-ready` after reviewing the final head, route/gate wiring, schemas, snapshot/exporter, prior-snapshot protection, test-host isolation, package/scope changes, and simplification dispositions.
- The snapshot is committed and regeneration is byte-identical.
- The generated document is gated by `Features:DesktopGateway`; gate-off contract coverage returns 404.
- Problem-details and paging schemas are covered by the contract suite.
- `PreviousSnapshotRemainsSatisfied` protects existing paths, operations, and required response properties.
- No deployment, cloud write, or external environment mutation was performed.

GWY-004 is ready for Kanmer verification and closeout.
