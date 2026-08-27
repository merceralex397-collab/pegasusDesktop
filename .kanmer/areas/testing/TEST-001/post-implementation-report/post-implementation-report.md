# Post-implementation report — TEST-001

## Result

The single `Pegasus.Api.ContractTests` project is present, registered once in `Pegasus.slnx`, restored with a committed lock file, and builds a real `WebApplicationFactory<Program>` host configured for the enabled `/api/v1` gateway. Its composition smoke fact is tagged `Category=Contract` and asserts the gateway options are registered by the real host. It intentionally does not claim that an endpoint exists: the gateway group is still empty and endpoint ownership remains with later gateway tickets.

## Changed files

- `tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj`
- `tests/Pegasus.Api.ContractTests/packages.lock.json`
- `tests/Pegasus.Api.ContractTests/ContractTestWebApplicationFactory.cs`
- `tests/Pegasus.Api.ContractTests/ContractTestHostTests.cs`
- `Pegasus.slnx`
- `tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs`
- `.github/workflows/ci.yml`
- `docs/runbook.md`
- `docs/operations.md`
- `docs/desktop/08-testing/README.md`

## Validation

Locked solution restore and Release solution build passed with zero warnings/errors before the review correction. The focused project and solution-level Contract selection each passed 1/1. The canonical non-corpus solution selection passed Core 935, Architecture 110, Contract 1, and Integration 957 with 2 skips and 0 failures before the review correction; the corrected smoke assertion requires a fresh local rerun and exact-head CI.

## Review corrections

Independent review correctly identified that a request to an unregistered probe path could receive 401 from the global fallback policy and therefore did not prove route mapping. The test now makes only the host-composition assertion. Review also identified that CI built but did not execute the new project; the existing unit chain now runs the focused Contract selection. The desktop testing plan now marks the scaffold existing while retaining downstream OpenAPI, Kiota, authorization, persistence and compatibility work as future work.

## Scope limits

OpenAPI snapshot/export, Kiota generated-client freshness, endpoint authorization/failure matrices and persistence cases remain with GWY-004, GWY-005, TEST-002 and TEST-003. No Azure/cloud/deployment/upstream/corpus operation was performed.

## Review state

A fresh independent review and post-correction exact-head CI run are required before merge.
