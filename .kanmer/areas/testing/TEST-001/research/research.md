# Research — TEST-001 API contract-test project

## Question

Scaffold tests/Pegasus.Api.ContractTests with xUnit 2.9.3 and WebApplicationFactory so api-v1 contracts are exercised against the gateway composition.

## Verified context

- EPIC-009 requires the local production-mimicking Test/UAT stack: local gateway/Worker, Azurite, LocalDB and replay adapters; no Azure dev/test/staging environment is in scope.
- The desktop stays native WinUI 3, gateway-backed, and one Core business-policy owner remains authoritative.
- The run-tests skill requires platform/framework detection from global.json, project files and repository test configuration before choosing exact test flags.

## Ticket findings

- Expected work surface: tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj.
- Expected work surface: tests/Pegasus.Api.ContractTests/* fixture and authentication helpers.
- Expected work surface: Pegasus.slnx and locked package/restore configuration.

## Implication

The project is a contract boundary; keep request setup and expected results shared with endpoint tests rather than creating a second policy engine.
