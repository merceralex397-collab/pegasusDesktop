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
