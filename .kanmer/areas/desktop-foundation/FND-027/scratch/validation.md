## 2026-08-25 validation checkpoint

- `dotnet restore ./Pegasus.slnx --force-evaluate` — exit 0; all seven solution projects restored.
- `dotnet restore ./Pegasus.slnx --locked-mode` — exit 0; all seven solution projects restored from committed lock files.
- `dotnet build ./Pegasus.slnx --configuration Release --no-restore` — exit 0; 0 warnings, 0 errors.
- `dotnet test ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj --configuration Release --no-build` — 99 passed, 0 failed.
- `dotnet test ./tests/Pegasus.Core.Tests/Pegasus.Core.Tests.csproj --configuration Release --no-build` — 916 passed, 0 failed.
- Static checks — 8 solution project files, 36 central package versions, 7 lock files, one root `RestorePackagesWithLockFile=true`, one CI cache dependency match, one central Playwright property reference, one central Azure.Storage.Blobs entry, and no versioned `PackageReference` attributes in project files. The three `Version=` matches found by an initial broad scan are test-fixture XML strings in `DependencyDirectionTests.cs`, not project files; the project-file-only check returned no matches.
- Reviewer artifact `artifacts/fnd-027-cpm/convert-to-cpm.md` records the baseline/after package comparison, the planned Azure.Storage.Blobs alignment, nested evaluator boundary evidence, and prior focused evaluator validation (restore exit 0; 9/9 tests).
- Next: independent `pegasus-desktop-reviewer` review, then PR creation to `dev`; PR creation may be blocked by the repository collaborator permission already observed on other tickets, which will be recorded truthfully if repeated.
