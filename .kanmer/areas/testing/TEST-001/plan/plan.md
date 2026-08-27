# Plan — TEST-001 API contract-test project

## Governing documents

This ticket remains docs_todo: true until the planned desktop governing documents are authored. The local Test/UAT and locked-decision material is binding now; do not create a competing product document in this task.

## Chosen approach

Scaffold tests/Pegasus.Api.ContractTests with xUnit 2.9.3 and WebApplicationFactory so api-v1 contracts are exercised against the gateway composition.

## Steps

1. Inspect existing test platform, global.json and package-management conventions before adding the project.
2. Create the minimal xUnit/WebApplicationFactory project and add it to the solution.
3. Reuse gateway fixture/auth test helpers where they exist; do not reproduce Core business policy.
4. Run locked restore, Release build and the focused project test.

## Verification

- The project appears once in Pegasus.slnx and builds with warnings-as-errors.
- Focused API contract test command passes using the detected test runner.
- Fixture does not require live Azure, Box or Graph.

## Risks

The project is a contract boundary; keep request setup and expected results shared with endpoint tests rather than creating a second policy engine.

Use the detected runner/framework and record exact command output when implementation begins. Complete a simplification pass and independent review before merge.

## Implementation and validation record — 2026-08-27

Scope was reconciled before implementation: TEST-001 owns the single contract-test project, its WebApplicationFactory baseline, solution registration, locked restore and discovery smoke fact. GWY-004 owns the OpenAPI export/snapshot contract; GWY-005 owns Kiota generation; later testing tickets own authorization, persistence and compatibility cases. No duplicate project, snapshot or generated client was created.

Implemented:
- Added `tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj` targeting `net10.0`, using centrally pinned xUnit 2.9.3, test SDK, collector and MVC testing packages.
- Added explicit references to `Pegasus.Web` and `Pegasus.Contracts`.
- Added `ContractTestWebApplicationFactory` with Development/`DevelopmentOffline` and `Features:DesktopGateway=true`.
- Added one `[Trait("Category", "Contract")]` real-host smoke fact. The unauthenticated request reaches the enabled `/api/v1` boundary and correctly returns 401 under the global authenticated-user policy.
- Registered the project once in `Pegasus.slnx`, generated its committed `packages.lock.json`, updated the canonical runbook/profile, and updated the exact solution-list architecture expectation.

Validation:
- `dotnet restore ./tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj`: passed.
- `dotnet restore ./Pegasus.slnx --locked-mode`: passed.
- `dotnet build ./tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj --configuration Release --no-restore`: passed, 0 warnings/errors.
- `dotnet build ./Pegasus.slnx --configuration Release --no-restore`: passed, 0 warnings/errors.
- `dotnet test ./tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj --configuration Release --no-build --filter "Category=Contract"`: passed 1, failed 0, skipped 0.
- `dotnet test ./Pegasus.slnx --configuration Release --no-build --filter "Category=Contract"`: passed 1, failed 0, skipped 0 and discovered the project through the solution.
- Canonical `dotnet test ./Pegasus.slnx --configuration Release --no-build --filter "Category!=Corpus"`: Core 935 passed; Architecture 110 passed; Contract 1 passed; Integration 957 passed, 2 skipped, 0 failed (959 total).

## Simplification pass — 2026-08-27

- Reused the existing central package versions, `Program` entry point, gateway constants and WebApplicationFactory host pattern; no new package, abstraction or policy engine was added.
- Kept the factory free of LocalDB and cloud/provider credentials because this scaffold only proves host composition; persistence belongs to TEST-003 and endpoint cases belong to later tickets.
- Added one deterministic smoke fact instead of placeholder template tests or duplicate OpenAPI/client tests owned by GWY-004/GWY-005.
- The only necessary adjacent update was the architecture test's exact solution inventory, which otherwise made the canonical suite fail after the intended project registration.
- `git diff --check`: passed. No unapplied behaviour-preserving simplification findings remain.
