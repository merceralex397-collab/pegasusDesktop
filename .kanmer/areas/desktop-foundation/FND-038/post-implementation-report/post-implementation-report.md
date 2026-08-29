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

## Final handoff correction — 2026-08-29

This section supersedes earlier stale counts, commit references, and the earlier failed locked-restore/build claims. FND-038 is an intentionally partial handoff: the owned FND-031/current-infrastructure extension is complete and verified, while FND-032 host/options/log/fallback coverage is outstanding because `origin/dev` contains no `PegasusHost`, `DiagnosticsLoggerProvider`, `Host.CreateApplicationBuilder`, or `ValidateOnStart` matches in the desktop production paths.

Final scope and Git:
- Worktree: `C:/Users/PC/Documents/GitHub/pegasus-worktrees/desktop-viewmodel-tests`
- Branch: `task/desktop-viewmodel-tests`
- `origin/dev`: `ac8f4432`
- Exact head: `55e42c4c81443205be18093700a62f98e38e6286`
- FND-038 commits: `984b9f72`, `3ddfbf05`, `ad520f9d`, `55e42c4c`; merge `a0ab9bed` incorporates current `origin/dev`.
- PR diff against `origin/dev`: exactly three files under `tests/Pegasus.Desktop.ViewModelTests/**`.

Lock diagnosis:
- `dotnet restore ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj -r win-x64 --force-evaluate`: exit 0, but generated only unrelated changes to `src/Pegasus.Contracts/packages.lock.json` and `src/Pegasus.Core/packages.lock.json` (removed `net10.0/linux-x64`, newline normalization). No desktop test lock changed; the two generated files were restored to HEAD and no lock commit was made.
- `dotnet restore ./Pegasus.slnx --locked-mode`: exit 0.
- `dotnet build --configuration Release --no-restore`: exit 0, 0 warnings/errors, 36.43 seconds.
- Focused ViewModel command with TRX: exit 0, 18/18 passed, 0 skipped; TRX `artifacts/test-results/FND-038-handoff-viewmodel/PC_DESKTOP-S1M5C7P_2026-08-29_19_39_27_net10.0.trx`.
- Architecture command with TRX: exit 0, 121/121 passed, 0 skipped; TRX `artifacts/test-results/FND-038-handoff-architecture/PC_DESKTOP-S1M5C7P_2026-08-29_19_39_30_net10.0.trx`.
- `git diff --check`: passed. SQL shard verification is not applicable; no SQL/shard files changed.
- CI was not edited or run; the suite remains outside the current CI project list. This is explicitly outside FND-038 scope.

Review/simplification:
- The pending test adaptation was committed as `55e42c4c` and only changes the assertion boundary to the merged gateway-validation API.
- Simplification pass completed: reused TEST-004 support, kept one test/support file, and added no production code, CI change, duplicate fake/clock/host, UI thread, mock framework, or external service.
- Independent review is required before PR. The ticket must remain partial and must not move to Done because FND-032 host coverage is unavailable.

## Delivery and review outcome — 2026-08-29

Final pushed head: `55e42c4c81443205be18093700a62f98e38e6286` on `task/desktop-viewmodel-tests`; remote branch `origin/task/desktop-viewmodel-tests` was created by the exact push command `git push --set-upstream origin task/desktop-viewmodel-tests`. The branch includes FND-038 commits `984b9f72`, `3ddfbf05`, `ad520f9d`, and `55e42c4c), plus merge `a0ab9bed` of current `origin/dev` `ac8f4432`.

The extension is necessarily partial. FND-031/current-infrastructure coverage is complete; FND-032 host/options/log/fallback coverage was not added because `origin/dev` has no matching `PegasusHost`, `DiagnosticsLoggerProvider`, `Host.CreateApplicationBuilder`, or `ValidateOnStart` APIs in the desktop paths. The ticket is not Done.

Final evidence in the pushed worktree:
- `dotnet restore ./Pegasus.slnx --locked-mode`: exit 0.
- `dotnet build --configuration Release --no-restore`: exit 0; 0 warnings, 0 errors; 36.43 seconds.
- `dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release --no-build --filter "Category!=Corpus" --logger trx --results-directory ./artifacts/test-results/FND-038-handoff-viewmodel`: exit 0; 18 passed, 0 failed, 0 skipped; TRX `artifacts/test-results/FND-038-handoff-viewmodel/PC_DESKTOP-S1M5C7P_2026-08-29_19_39_27_net10.0.trx`.
- `dotnet test ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj --configuration Release --no-build --filter "Category!=Corpus" --logger trx --results-directory ./artifacts/test-results/FND-038-handoff-architecture`: exit 0; 121 passed, 0 failed, 0 skipped; TRX `artifacts/test-results/FND-038-handoff-architecture/PC_DESKTOP-S1M5C7P_2026-08-29_19_39_30_net10.0.trx`.
- `git diff --check`: passed; pushed PR diff is exactly three test-project files.
- RID lock diagnosis: prescribed project restore exited 0 but only removed `net10.0/linux-x64` from unrelated `src/Pegasus.Contracts/packages.lock.json` and `src/Pegasus.Core/packages.lock.json` and normalized newlines. Those changes were restored and not committed; no desktop test lock change existed.
- CI/corpus/production/AGENTS.md were not changed; SQL shard verification is not applicable.

Independent review was not completed. The named reviewer invocation failed before inspection with exact Windows error `orchestrator_helper_launch_failed ... error=The filename or extension is too long. (os error 206)`. Therefore no PR was opened and no approval is claimed.

## Independent review — 2026-08-29

Bohr the 2nd independently reviewed exact head `55e42c4c81443205be18093700a62f98e38e6286` and returned PASS for the amended partial scope. The review confirmed that FND-038 reuses TEST-004's existing project, shared clock, baseline fakes, no-UI guard, solution registration, and architecture boundary; adds only the narrowly owned FND-031/current-infrastructure test extension; and makes no production, CI, corpus, AGENTS.md, solution, architecture-list, or unrelated-ticket changes.

The review confirmed the reported evidence: locked solution restore passed; Release build passed with 0 warnings and 0 errors; focused desktop tests passed 18/18 with 0 skipped; architecture tests passed 121/121 with 0 skipped; `git diff --check` passed; and the simplification record is consistent with the diff.

Review note: FND-032 host/options/log/fallback tests are explicitly deferred until FND-032's production host APIs merge. This is a partial handoff, not a Done approval. FND-038 still requires those host tests and its own post-merge proof before closeout.

## Prerequisite merge — 2026-08-29

PR #47 merged into `dev` after the exact reviewed head passed all applicable CI lanes.

- PR: https://github.com/merceralex397-collab/pegasusDesktop/pull/47
- Reviewed head: `55e42c4c81443205be18093700a62f98e38e6286`
- CI: run `33269301840` — completed successfully; browser, unit, all SQL shards, SQL integration coverage, changes, documentation, local-development-scripts, and reference-data succeeded; infrastructure was skipped by its documented path filter.
- Resulting `origin/dev`: `17f508dead86b5c739965905a274876a1aa8553b`

This is a prerequisite merge only. FND-038 remains open and partial: host/options/log/fallback tests owned by FND-038 must now be implemented against merged FND-032 APIs, independently reviewed, validated, merged, and proven before Done.

## FND-032 host coverage implementation — 2026-08-29

FND-032 is now merged to `dev`, so the deferred host coverage was implemented in this ticket's existing TEST-004-owned test project.

Added `tests/Pegasus.Desktop.ViewModelTests/Fnd032HostTests.cs` with:

- unpackaged `PegasusHost` start and service-resolution coverage for channel/options, HttpClient, credential store, bounded cache, and logging provider;
- startup validation failure when `Gateway:BaseAddress` is removed;
- diagnostics provider coverage for session ID and correlation ID, using the existing rolling writer to prove bearer-token redaction.

Validation at exact head `69d5803713422ccac9ef52fd924af80c5a5d1507`:

- `dotnet restore ./Pegasus.slnx --locked-mode` — exit 0;
- `dotnet build --configuration Release --no-restore` — exit 0, 0 warnings, 0 errors;
- `dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release --no-build` — 21 passed, 0 failed, 0 skipped;
- `dotnet test ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj --configuration Release --no-build` — 121 passed, 0 failed, 0 skipped;
- `git diff --check` — passed.

The required simplification pass is recorded in the plan. The branch is pushed and awaits independent review. FND-038 is still not Done: it requires review, PR CI, merge, merged-main proof, and Kanmer closeout.

## Review remediation and revalidation — 2026-08-29

The first independent review of head `69d5803713422ccac9ef52fd924af80c5a5d1507` was BLOCKED with concrete findings: fallback and host-configured bounded-writer coverage were absent, the logging test bypassed host DI, and the checklist remained inconsistent with the partial implementation.

Commit `f34d872aeac79460536a6a48f507f1dcbe739874` corrects those findings. The host test now exercises the host-resolved `ILoggerFactory` and `IDiagnosticsWriter`, verifies session/correlation/redaction in the actual serialized log, asserts the configured 10 MiB/five-file writer limits, and verifies the unpackaged process-specific temp fallback. The direct-provider duplicate test was removed.

Validation at the corrected exact head:

- focused FND-032 host tests — 2 passed, 0 failed, 0 skipped;
- full `Pegasus.Desktop.ViewModelTests` — 20 passed, 0 failed, 0 skipped;
- `Pegasus.ArchitectureTests` — 121 passed, 0 failed, 0 skipped;
- Release build — 0 warnings, 0 errors;
- locked solution restore and `git diff --check` — passed.

The earlier checklist note saying host coverage was outstanding is superseded by the appended coverage-reconciliation section. This is still awaiting a fresh independent review; no merge or Done claim is made.

## Independent review after remediation — 2026-08-29

Sagan the 2nd independently reviewed exact head `f34d872aeac79460536a6a48f507f1dcbe739874` after the prior BLOCKED review. The reviewer returned PASS.

The review confirmed that the prior findings are resolved:

- `ILoggerFactory` and `IDiagnosticsWriter` are resolved from the built host and exercised through host DI;
- the serialized host log carries a non-empty session ID, the scoped correlation ID, and redacts the bearer token;
- the resolved writer is asserted as the configured 10 MiB / five-file bounded writer;
- the unpackaged process-specific `%TEMP%/Pegasus.Desktop/<PID>` fallback is asserted;
- the checklist and plan now reconcile the corrected coverage.

The reviewer found no scope defect or unaddressed implementation omission. Evidence rechecked: locked restore passed; Release build passed with 0 warnings/errors; ViewModelTests 20/20 passed with 0 skipped; ArchitectureTests 121/121 passed with 0 skipped; `git diff --check` passed; exact diff is one test file and contains no production, CI, corpus, cloud, upstream, or unrelated changes.

This PASS authorizes the PR review boundary only. FND-038 remains not Done pending PR CI, merge to `dev`, merged-main proof, and Kanmer closeout.

## Corrected PR — 2026-08-29

After the independent PASS, PR #49 was opened against `dev` at exact head `f34d872aeac79460536a6a48f507f1dcbe739874`:

https://github.com/merceralex397-collab/pegasusDesktop/pull/49

The PR contains only `tests/Pegasus.Desktop.ViewModelTests/Fnd032HostTests.cs` relative to `dev`. Exact-head CI is running. No merge or Done claim is made until all applicable checks pass and merged-main proof is written.

## CI infrastructure blocker — 2026-08-29

PR #49 remains open at exact head `f34d872aeac79460536a6a48f507f1dcbe739874`. Exact-head CI run `33271318606` was attempted twice:

1. The initial attempt started the five heavy jobs at approximately 19:37. Browser, unit, and all three SQL jobs remained inside the shared `./.github/actions/dotnet-build` composite action for over an hour; their test steps never started, and GitHub provided no logs while the composite step was in progress.
2. The run was canceled and fully rerun. Fresh heavy jobs started at approximately 19:41 and reproduced the same condition: all five remained in `dotnet-build`, then were canceled. The coverage aggregator ended with failure because its required shard artifacts were canceled.

The lightweight checks (changes, documentation, local-development-scripts, and reference-data) passed; infrastructure was skipped by the documented path filter. This is a CI runner/action hang, not a source assertion failure. Local validation remains green, but exact-head CI is not green, so PR #49 must not merge. Smallest unblock action: restore a functioning `dotnet-build` runner/action execution, then rerun the failed heavy jobs at the unchanged exact head.
