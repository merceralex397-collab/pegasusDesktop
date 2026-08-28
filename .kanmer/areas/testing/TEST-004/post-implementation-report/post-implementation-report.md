# Post-implementation report — TEST-004

## Scope delivered

Implemented the Windows-only, unpackaged desktop view-model test home at tests/Pegasus.Desktop.ViewModelTests. It is registered in Pegasus.slnx and uses the existing centrally managed xUnit package set without adding a mocking framework or a CI workflow change.

The project contains one shared deterministic FixedTimeProvider, a transport-free FakeGatewayClient with recorded requests and all thirteen current PegasusProblemTypes supported as queued failures, FakeCredentialStore, FakeNavigationService, and the public-view-model guard test. The support types intentionally do not invent production ports that are not yet present on origin/dev; FND-031 and later desktop tickets can adapt them when those ports land.

The existing architecture-test expectation, runbook focused command, and ViewModel evidence-profile row were updated because they are required consumers of the new test project.

## Verification

- dotnet restore ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj -r win-x64 --force-evaluate — passed.
- dotnet restore ./Pegasus.slnx --locked-mode — passed.
- dotnet build ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release --no-restore -nr:false -p:UseSharedCompilation=false -p:BuildInParallel=false -p:NodeReuse=false — passed, 0 warnings/errors.
- dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release --no-build --no-restore -nr:false -p:UseSharedCompilation=false -p:BuildInParallel=false -p:NodeReuse=false --verbosity minimal — Passed: 6, Failed: 0, Skipped: 0.
- dotnet build ./Pegasus.slnx --configuration Release --no-restore -nr:false -p:UseSharedCompilation=false -p:BuildInParallel=false -p:NodeReuse=false --verbosity minimal — passed, 0 warnings/errors.
- dotnet build ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj --configuration Release --no-restore -nr:false -p:UseSharedCompilation=false -p:BuildInParallel=false -p:NodeReuse=false --verbosity minimal — passed, 0 warnings/errors.
- dotnet test ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj --configuration Release --no-build --no-restore -nr:false -p:UseSharedCompilation=false -p:BuildInParallel=false -p:NodeReuse=false --verbosity minimal — Passed: 121, Failed: 0, Skipped: 0.
- The negative guard check temporarily introduced a DispatcherQueue property into MainPageViewModel and the filtered guard test failed naming the field/property; the temporary production edit was removed and the final focused suite passed 6/6.
- rg FixedTimeProvider tests/Pegasus.Desktop.ViewModelTests shows one definition in Support/FixedTimeProvider.cs.
- dotnet sln ./Pegasus.slnx list includes the new test project.

## Simplification pass

Completed and recorded in the Kanmer plan on 2026-08-28. The diff reuses existing package and test conventions, uses hand-written seams, adds no production dependency or CI lane, and keeps one support test class plus one guard test. No speculative UI-thread, network, database, package-identity, or production-port compatibility path was added.

## Branch

Commit c7f6f689 on task/desktop-viewmodel-tests, pushed to origin/pegasusDesktop. Independent pegasus-desktop-reviewer review and the PR merge remain required before verification and closeout.
