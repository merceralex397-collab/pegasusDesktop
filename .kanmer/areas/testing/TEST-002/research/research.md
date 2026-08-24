# Research — TEST-002 Authorization and failure-path template

## Question

Establish a reusable authorization and failure-path contract template for every api-v1 command.

## Verified context

- EPIC-009 requires the local production-mimicking Test/UAT stack: local gateway/Worker, Azurite, LocalDB and replay adapters; no Azure dev/test/staging environment is in scope.
- The desktop stays native WinUI 3, gateway-backed, and one Core business-policy owner remains authoritative.
- The run-tests skill requires platform/framework detection from global.json, project files and repository test configuration before choosing exact test flags.

## Ticket findings

- Expected work surface: tests/Pegasus.Api.ContractTests/* command test base.
- Expected work surface: Gateway endpoint inventory/contract fixtures.
- Expected work surface: Shared problem-details assertions.

## Implication

Keep only one fixture taxonomy and preserve the gateway/Core boundary.
