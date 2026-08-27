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

## Live endpoint inventory — 2026-08-27

- Read the merged `origin/dev` host through `ContractTestWebApplicationFactory.Services` and inspected `EndpointDataSource.Endpoints`.
- The application currently exposes no `POST`, `PUT`, `PATCH`, or `DELETE` route under `/api/v1`; the command catalogue therefore returns zero endpoints.
- `GWY-003` and `GWY-021` remain unmerged, so there is no authoritative command authorization metadata or command handler to test yet.
- The harness records this zero state explicitly and makes the first future command endpoint fail the symmetric coverage guard until its literal row is reviewed and added.
- The derived-host probe test maps `POST /api/v1/__probe` through a test-only startup filter, then reads it from the catalogue/guard; it does not alter product routing.

## Review correction — 2026-08-27

The independent review correctly rejected treating a synthetic endpoint as equivalent to host registration. The probe has been changed to a derived `WebApplicationFactory<Program>` with a test-only `IStartupFilter` that maps `POST /api/v1/__probe`; the test now reads the resulting real `EndpointDataSource` and the focused contract suite confirms the route is reported as uncovered. This remains test-only and does not alter application routing.
