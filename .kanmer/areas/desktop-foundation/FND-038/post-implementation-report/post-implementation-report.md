# Post-implementation report — FND-038

## Status

The amended FND-038 implementation is complete for the current `origin/dev` target. It extends the TEST-004-owned project and does not recreate or re-register the scaffold.

FND-032 host fixture tests are precisely deferred: `origin/dev` has no `PegasusHost`, FND-032 host/options registrations, or `DiagnosticsLoggerProvider`. Testing those APIs would require production changes or pulling `task/desktop-host`, both outside this ticket. The current merged infrastructure boundary is covered through `AddPegasusApiClient`, `GatewayOptions`, `PegasusRequestHandler`, retry handling, and `RollingFileDiagnosticsWriter`.

## Changed files

Only these files changed in commit `984b9f7278f1ac151ba8fa0f923d4c3bce6fa86e2`:

- `tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj` — added the required direct reference to `Pegasus.Desktop.Infrastructure`.
- `tests/Pegasus.Desktop.ViewModelTests/Fnd031InfrastructureTests.cs` — 12 FND-031/current-infrastructure tests covering DPAPI round-trip/clear/protection, missing and corrupt credentials, root isolation, generated and caller-preserved headers/scopes, base-address options validation, GET retry/header preservation, POST no-retry, redaction/context, rotation, and retention.
- `tests/Pegasus.Desktop.ViewModelTests/Support/InfrastructureTestSupport.cs` — internal fixed client-version provider, recording HTTP handler, and recording logger.

TEST-004's existing `Support/FixedTimeProvider.cs`, baseline gateway/credential/navigation fakes, support tests, no-UI-thread guard, lock file, `Pegasus.slnx` entry, and architecture expected-list entry were reused untouched. No production, CI, corpus, AGENTS.md, solution, architecture-list, UI-test, packaging, or other-ticket files changed.

## Verification

Commands ran in `C:/Users/PC/Documents/GitHub/pegasus-worktrees/desktop-viewmodel-tests` on `task/desktop-viewmodel-tests`:

- `dotnet restore ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj -r win-x64 --force-evaluate` — exit 0.
- `dotnet restore ./Pegasus.slnx --locked-mode` — exit 0.
- `dotnet build --configuration Release --no-restore` — exit 0; 0 warnings, 0 errors; 27.15 seconds.
- `dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release --no-build --filter "Category!=Corpus" --logger trx --results-directory ./artifacts/test-results/FND-038-viewmodel` — exit 0; Passed 17, Failed 0, Skipped 0; 386 ms. TRX: `artifacts/test-results/FND-038-viewmodel/PC_DESKTOP-S1M5C7P_2026-08-29_19_06_38_net10.0.trx`.
- `dotnet test ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj --configuration Release --no-build --filter "Category!=Corpus" --logger trx --results-directory ./artifacts/test-results/FND-038-architecture` — exit 0; Passed 121, Failed 0, Skipped 0; 1 minute 2 seconds. TRX: `artifacts/test-results/FND-038-architecture/PC_DESKTOP-S1M5C7P_2026-08-29_19_07_50_net10.0.trx`.
- `git diff --check` and scope/support scans — passed; final post-commit worktree is clean and only the three owned files are in the commit.
- SQL shard partition verification — not applicable to this test-only desktop extension; no SQL/shard files or runtime were touched.
- CI — not run or edited; this project remains outside the current CI project list pending [[FND-040]].

The initial focused run exposed one incorrect rotation assertion (15 passed, 1 failed); the assertion was corrected to the current writer's bounded eviction semantics and the final run above passed 17/17.

## Simplification pass

Completed 2026-08-29. Reused all TEST-004 support and added only one test file plus one support file for three distinct current-pipeline helpers. Removed an unused import. No mocking framework, duplicate clock/fake/guard, host abstraction, UI thread, production dependency, or speculative compatibility path was introduced. No unapplied behaviour-preserving simplification finding remains.

## Delivery

- Branch: `task/desktop-viewmodel-tests`.
- Worktree: `C:/Users/PC/Documents/GitHub/pegasus-worktrees/desktop-viewmodel-tests`.
- Commit: `984b9f7278f1ac151ba8fa0f923d4c3bce6fa86e2`.
- PR: not opened; independent `pegasus-desktop-reviewer` review is required first.
- Merge: not performed.
- Azure/cloud writes and external service calls: none.
- Skills consulted: `pegasus-desktop`; pinned dotnet-test `scaffold-dotnet-test-project`, `code-testing-agent`, `run-tests`, `test-gap-analysis`, and `assertion-quality` route `98f84851`.

Remaining delivery gate: independent reviewer findings and disposition before any PR.
