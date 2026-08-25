# Post-implementation report — INTK-003

## Delivered

- `CaseDataPolicy.Normalize` now converts named kilometre input to canonical miles with factor `0.6213711922` and midpoint-away-from-zero rounding.
- Missing units are treated as miles; only the existing named `Miles` and `Kilometres` vocabulary is accepted, and unknown/numeric nonblank values fail closed.
- The submitted kilometre value is retained as typed provenance in `CaseVehicleData.OriginalMileageKilometres` and the existing EAV store.
- Existing case DTO, Web case-details, MCP save path, and case summary carry/display the marker; no new endpoint or client conversion was added.
- The EAV field whitelist is updated by generated migration `20260825202208_CanonicalCaseMileageProvenance`; it changes only the check constraint and does not transform existing data.
- FRD-06 and `docs/capabilities.md` record the current requirement. EVA bundle field names remain unchanged.

## Validation

- `dotnet test tests/Pegasus.Core.Tests/Pegasus.Core.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~CaseDataOperationsTests"`: 12 passed.
- `dotnet test tests/Pegasus.Core.Tests/Pegasus.Core.Tests.csproj --configuration Release --no-restore`: 926 passed.
- `dotnet test tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~CaseDataCompletenessPersistenceTests"`: 5 passed.
- `dotnet build --configuration Release`: passed with 0 warnings and 0 errors.
- `pwsh ./scripts/Test-MigrationGrants.ps1`: 66 migrations checked; passed.
- `git diff --check`: passed; only line-ending normalization warnings were reported.

## Known boundary

The ticket is not yet merged or proven on `main`. Independent review and the configured-origin PR/CI/merge sequence are next. No upstream, cloud, deployment, or external write was performed.

## Review correction — 2026-08-25

The first independent review failed on duplicated conversion ownership and incomplete provenance validation. Those findings were fixed by delegating to `VehicleMileagePolicy.ToMiles`, rejecting negative provenance, and clearing provenance when mileage is cleared. Final corrected-source validation is 927/927 Core tests, 5/5 case-data persistence tests, a Release build with 0 warnings/errors, and a passing 66-migration grant check.

## Review correction — 2026-08-25 (corrected head)

The corrected changes are committed as `52b00c52` and pushed to the configured `pegasusDesktop` remote. The earlier review findings were addressed: conversion now has one owner in `VehicleMileagePolicy.ToMiles`, negative kilometre provenance is rejected, and provenance is cleared when mileage is cleared. Corrected-source validation is 36/36 focused Core/VehicleWorkflow tests, 927/927 full Core tests, 5/5 case-data persistence tests, Release build with 0 warnings/errors, 66-migration grant check passed, and `git diff --check` passed.

## Review correction — 2026-08-25 (vehicle workflow writer)

The second independent review found that vehicle-suggestion correction wrote raw kilometre mileage/unit directly through `EfVehicleWorkflowStore`, bypassing canonical case-data normalization and provenance handling. The corrected branch now delegates the pair to `CaseDataPolicy.Normalize`, writes canonical miles with unit `Miles`, stores `vehicle_mileage_kilometres` for kilometre corrections, and clears that marker when mileage is explicitly replaced or cleared.

Validation after this correction:

- `dotnet test tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --no-restore --filter FullyQualifiedName~VehicleWorkflowTerminalTests.KilometreCorrectionIsStoredAsCanonicalMilesWithOriginalReading`: 1 passed.
- `dotnet test tests/Pegasus.Core.Tests/Pegasus.Core.Tests.csproj --no-restore`: 927 passed.
- `dotnet build Pegasus.slnx --configuration Release --no-restore`: passed with 0 warnings and 0 errors.
- `pwsh ./scripts/Test-MigrationGrants.ps1`: 66 migration files checked; passed.
- `git diff --check`: passed; only line-ending normalization warnings were reported.

## CI correction — 2026-08-25

Exact-head run `32899041711` failed in `sql-integration (3)` because `CommittedMigrationCreatesTheSqlServerSchema` expected the pre-ticket migration list and omitted `20260825202208_CanonicalCaseMileageProvenance`; the actual database applied the migration correctly. Updated `tests/Pegasus.IntegrationTests/IntakePersistenceIntegrationTests.cs` to include that generated migration. Local rerun of the failing test passed 1/1. The correction is not merge-ready until a new exact-head CI run is green.

## CI concurrency blocker — 2026-08-25

After the migration-list correction, exact-head run `32900431792` failed twice in `sql-integration (3)` on the existing `GroupedImageIntakeConcurrencyTests.ConcurrentGroupMembersNeverSplitAcrossRepeatedRuns` test. Both failures were SQL Server deadlock-victim errors (1205) at `EfIntakeWorkStore.CompleteProcessingAsync`; the second attempt passed 290/291 tests. This is unrelated to the ticket's changed files. CI is not green and the PR is not merge-ready; no bypass or false green claim is allowed.
