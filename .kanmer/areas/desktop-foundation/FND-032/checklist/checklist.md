# Checklist — FND-032

## Acceptance criteria

- [x] `App.xaml.cs` builds one generic host before any window is created and disposes it on exit.
- [x] Configuration is embedded and layered as base plus MSBuild-selected `local`, `pilot`, or `production` channel configuration.
- [x] Shipped desktop configuration contains no secret, token, or connection string.
- [x] Gateway and update options are bound with validation and fail at start when a required setting is missing.
- [x] Structured logs carry a per-launch session identifier and API correlation id, rotate within the explicit 10 MiB/five-file bounds, and redact bearer/password values.
- [x] The services required by a view model resolve in a test without a dispatcher.

## Validation evidence

- [x] `dotnet restore ./Pegasus.slnx --locked-mode` — passed.
- [x] `dotnet build ./Pegasus.slnx --configuration Release --no-restore` — passed, 0 warnings, 0 errors.
- [x] `dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release --no-restore` — 20 passed, 0 failed, 0 skipped.
- [x] `dotnet test ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj --configuration Release --no-restore` — 121 passed, 0 failed, 0 skipped.
- [x] Pilot-channel Release build and embedded-resource inspection — passed; only base plus pilot channel resource selected.
- [x] `BuildAndRun.ps1` packaged AUMID launch and cleanup — passed; diagnostics contained a session id.
- [x] `git diff --check` — passed.
- [x] Independent review by Descartes the 2nd — PASS on the exact implementation merged through PR #46/PR #49.

## Boundary evidence recorded separately

- [x] Clean-machine signed MSIX, install/uninstall, and certificate-trust evidence is assigned to packaging/release tickets, not claimed here.
- [x] D-003 pilot/production UNC feed authority is recorded as unresolved and is not guessed; feed provisioning remains an operator/release decision outside FND-032.
