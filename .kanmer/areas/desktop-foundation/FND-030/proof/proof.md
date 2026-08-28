# Proof — FND-030

## Result

FND-030's WinUI 3 packaged x64 scaffold is merged to `main` and passed the required merged-main validation.

## Delivery and review

- PR #39: `https://github.com/merceralex397-collab/pegasusDesktop/pull/39`
- Implemented head: `d2fe4bd08f8e63c3655097e4f850a0f086072176`
- Independent review: Franklin reviewed the exact implementation head and passed the scaffold, package, identity, WebView2 asset control, solution integration, scope, accessibility, validation, and simplification lenses. The temporary review note was that exact-head main CI was still running; that condition is now cleared.
- PR #39 merged into `dev`; exact non-force SHA promotion moved `dev`/ `main` to merge commit `28ba13a4fcdb51270b24a48725d53b1de5bcae87` after confirming `origin/main` was an ancestor of `origin/dev`.
- No upstream synchronization, cloud write, deployment, signing, or certificate change was performed.

## Exact-main CI

- Repository-check run `33207170876`, attempt 2, exact head `28ba13a4fcdb51270b24a48725d53b1de5bcae87`: successful.
- All required jobs were successful: reference-data, changes, documentation, local-development-scripts, unit, browser, sql-integration shards 1/2/3, and sql-integration-coverage. Infrastructure was skipped as designed.
- The initial attempt's SQL shard 1 post-cleanup timeout was resolved by rerunning only that failed job on the unchanged SHA. Rerun job `98976295000` completed successfully in 9m05s.
- Coverage job `98978458346` passed and verified the enumerated non-browser integration tests were partitioned exactly once.

## Merged-main validation

Validation was run from the clean detached worktree `C:\Users\PC\Documents\GitHub\pegasus-worktrees\verify-fnd030-main-20260828` at `28ba13a4`.

- `dotnet restore ./Pegasus.slnx --locked-mode`: passed.
- `dotnet build ./Pegasus.slnx --configuration Release --no-restore -nr:false -p:UseSharedCompilation=false -p:BuildInParallel=false -p:NodeReuse=false`: passed with 0 warnings and 0 errors.
- `dotnet test ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj --configuration Release --no-build -nr:false -p:UseSharedCompilation=false -p:BuildInParallel=false -p:NodeReuse=false --verbosity minimal`: 121/121 passed.
- Merged-main desktop output scan: zero WebView2 runtime payload files; manifest identity `CollisionEngineers.Pegasus`; publisher `CN=Collision Engineers`.
- Retained launch artifact `artifacts/fnd-030/desktop-launch-desktop.png`; SHA-256 `C7838380DE4EACF053DFB0C9F7969F529DFAB76B11A8E4D1F5E1D5970A9B6159`.
- Merged-main verification worktree was clean.

## Required honesty limits

- A green `BuildAndRun.ps1` launch is not the same claim as a green solution `dotnet build`; both were run and are recorded separately.
- The current CI workflow has no dedicated desktop-specific CI job; that remains the scope of [[FND-040]]. The general exact-main CI passed.
- Linux support was not claimed or validated. Because [[FND-028]] has not landed, this proof does not assert that Linux is supported or that it is broken.

## Scope boundary

This ticket establishes the packaged WinUI 3 scaffold and repository integration only. It does not claim a signed MSIX, certificate possession, deployment, runtime gateway behavior, or a completed desktop feature surface.
