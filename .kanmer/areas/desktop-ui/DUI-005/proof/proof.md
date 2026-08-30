# Proof — DUI-005

## Result

DUI-005 is verified on merged `main` at `6f5241666618f85db8994f582115b0e55c18adb9`. The desktop consumes the shared operator vocabulary through its presentation facade, with the implemented test and guard evidence recorded below.

## Delivery

- Exact final PR head: `ec6c080e65bd25e853fce85b581afb625394b87f`.
- Independent final-head review: PASS; no actionable findings. The review correction added the explicit `FeeNote` entry to the existing shared vocabulary owner.
- Exact-head repository-check run `33290083605`: success after rerunning SQL shard 3 job `99200136610` as `99202262331`; SQL shards 1–3, SQL coverage, unit, browser, documentation, changes, reference-data, and local-development-scripts passed. Infrastructure was skipped by the documented path filter.
- PR #54 merged into `dev` as `6f5241666618f85db8994f582115b0e55c18adb9`.
- Exact-SHA promotion moved `main` from `7c28cc812a89ad577e93a04c2b7e3f416bfa929e` to `6f5241666618f85db8994f582115b0e55c18adb9`; remote read-back confirmed `dev` and `main` both equal `6f524166`.

## Merged-main validation

Fresh detached verification worktree at `6f5241666618f85db8994f582115b0e55c18adb9`:

- `dotnet restore ./Pegasus.slnx --locked-mode` — exit 0.
- `dotnet build ./Pegasus.slnx --configuration Release --no-restore -nr:false --nologo` — exit 0; 0 warnings, 0 errors.
- `dotnet test tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release --no-restore --logger "console;verbosity=minimal"` — exit 0; 63 passed, 0 failed, 0 skipped.
- `rg -n "\\.ToString\\(\\)|ToLocalTime\\(\\)|DateTime\\.Now" src/Pegasus.Desktop --glob "!**/obj/**"` — no display-path matches. The three matches are diagnostic serialization calls in `Logging/DiagnosticsLoggerProvider.cs`, outside view/model presentation paths.
- Verification worktree `git status --short` — clean.

## Scope limit

This Tier 1 proof covers the shared vocabulary boundary, desktop formatting and guard tests, restore/build, exact-head CI, review, merge, and merged-main validation. It does not claim a running WinUI UI, MSIX install, clean-machine launch, or packaging acceptance; those belong to FND-039.
