# Proof — TEST-004

## Merged-main identity

- Verified checkout: `C:/Users/PC/Documents/GitHub/pegasus-worktrees/verify-test004-main-20260828`
- Git state: clean detached checkout at `66aa3eba08f7717b590812053695cc26f3170e7a`
- PR: #40, merged into `dev` at `66aa3eba08f7717b590812053695cc26f3170e7a`
- `main` was promoted to that exact non-force SHA under the recorded merge authorization.

## Validation

- `dotnet restore .\Pegasus.slnx --locked-mode` — passed.
- `dotnet build .\Pegasus.slnx --configuration Release --no-restore -nr:false -p:UseSharedCompilation=false -p:BuildInParallel=false -p:NodeReuse=false --verbosity minimal` — Build succeeded; 0 warnings, 0 errors.
- `dotnet test .\tests\Pegasus.Desktop.ViewModelTests\Pegasus.Desktop.ViewModelTests.csproj --configuration Release --no-build --no-restore -nr:false -p:UseSharedCompilation=false -p:BuildInParallel=false -p:NodeReuse=false --verbosity minimal` — Passed 6, Failed 0, Skipped 0.
- `dotnet test .\tests\Pegasus.ArchitectureTests\Pegasus.ArchitectureTests.csproj --configuration Release --no-build --no-restore -nr:false -p:UseSharedCompilation=false -p:BuildInParallel=false -p:NodeReuse=false --verbosity minimal` — Passed 121, Failed 0, Skipped 0.
- Negative guard probe temporarily added a DispatcherQueue property and the guard failed as designed; the property was removed and the focused suite returned to 6/6.

## Exact-head GitHub CI

Repository-check run [33218441215](https://github.com/merceralex397-collab/pegasusDesktop/actions/runs/33218441215) completed with conclusion `success` at the exact SHA `66aa3eba08f7717b590812053695cc26f3170e7a`. Required jobs passed: changes, reference-data, documentation, local-development-scripts, unit, sql-integration (1), sql-integration (2), sql-integration (3), browser, and sql-integration-coverage. The infrastructure job was skipped by its path condition.

The test project is now present on merged main and is available for FND-031's planned infrastructure behavior tests. No deployment or cloud write was performed.
