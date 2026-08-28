# Plan — TEST-004 Desktop ViewModelTests project

## Governing documents

This ticket remains docs_todo: true until the planned desktop governing documents are authored. The local Test/UAT and locked-decision material is binding now; do not create a competing product document in this task.

## Chosen approach

Scaffold tests/Pegasus.Desktop.ViewModelTests targeting net10.0-windows10.0.26100.0 with no UI-thread requirement.

## Steps

1. Inspect existing test framework, target framework and package conventions.
2. Create the desktop view-model test project with no XAML dispatcher or UI-thread dependency.
3. Reuse one shared fake clock/date convention and generated/gateway fakes.
4. Add it to the solution and run focused tests.

## Verification

- The project targets the approved Windows TFM and runs headlessly.
- Tests do not require an installed MSIX or UI thread.
- Locked restore and Release build pass.

## Risks

Do not add another FixedTimeProvider or a desktop-specific business-rule copy.

Use the detected runner/framework and record exact command output when implementation begins. Complete a simplification pass and independent review before merge.

## Implementation checkpoint — 2026-08-28

Implemented in the ticket worktree C:/Users/PC/Documents/GitHub/pegasus-worktrees/desktop-viewmodel-tests from origin/dev.

- Added tests/Pegasus.Desktop.ViewModelTests targeting net10.0-windows10.0.26100.0, x64, unpackaged, with the existing centrally managed xUnit package set and a committed lock file.
- Added the single shared Support/FixedTimeProvider, transport-free hand-written gateway/credential/navigation support, and the NoUiThreadDependencyTests.PublicViewModelsDoNotReferenceDispatcherOrXamlTypes guard.
- Added the project to Pegasus.slnx and updated the existing solution-architecture expectation so the repository entry point remains tested.
- Added the focused Windows test command to docs/runbook.md and registered the ViewModel evidence profile in docs/operations.md.
- The production generated gateway client and the FND-031 credential port are not present on origin/dev; the support types therefore do not invent or duplicate those production contracts. FND-031 can extend the shared test home after its infrastructure change and its required behavior tests are ready.

Validation:
- dotnet restore ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj -r win-x64 --force-evaluate — passed.
- dotnet restore ./Pegasus.slnx --locked-mode — passed.
- dotnet build ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release --no-restore -nr:false -p:UseSharedCompilation=false -p:BuildInParallel=false -p:NodeReuse=false — passed, 0 warnings/errors.
- dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release --no-build --no-restore -nr:false -p:UseSharedCompilation=false -p:BuildInParallel=false -p:NodeReuse=false --verbosity minimal — Passed: 6, Failed: 0, Skipped: 0.
- dotnet build ./Pegasus.slnx --configuration Release --no-restore -nr:false -p:UseSharedCompilation=false -p:BuildInParallel=false -p:NodeReuse=false --verbosity minimal — passed, 0 warnings/errors.
- dotnet build ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj --configuration Release --no-restore -nr:false -p:UseSharedCompilation=false -p:BuildInParallel=false -p:NodeReuse=false --verbosity minimal — passed, 0 warnings/errors.
- dotnet test ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj --configuration Release --no-build --no-restore -nr:false -p:UseSharedCompilation=false -p:BuildInParallel=false -p:NodeReuse=false --verbosity minimal — Passed: 121, Failed: 0, Skipped: 0.
- The required negative guard probe temporarily added a public DispatcherQueue property to MainPageViewModel; the filtered guard run failed with the detected field/property, then the probe was removed and the focused suite returned to 6/6 green.

## Simplification pass — 2026-08-28

- Reused the repository's existing xUnit package and project conventions; no new test framework or mocking package was added.
- Kept the support layer to four small hand-written seams and one guard test. No UI thread, package identity, network, database, production contract, or CI workflow was introduced.
- Used one support test class for deterministic support behavior rather than duplicating one test file per fake; kept the required ViewModel trait on every test class.
- Updated the existing architecture expectation because solution registration is part of the actual project graph; no new architecture rule was added.
- The negative probe was temporary and is absent from the final diff.
