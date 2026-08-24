# Research — TEST-007 UI critical-path scripts

## Question

Script launch/update/login, case open/edit/save, concurrency message, logout and keyboard navigation journeys.

## Verified context

- EPIC-009 requires the local production-mimicking Test/UAT stack: local gateway/Worker, Azurite, LocalDB and replay adapters; no Azure dev/test/staging environment is in scope.
- The desktop stays native WinUI 3, gateway-backed, and one Core business-policy owner remains authoritative.
- The run-tests skill requires platform/framework detection from global.json, project files and repository test configuration before choosing exact test flags.

## Ticket findings

- Expected work surface: tests/Pegasus.Desktop.UITests/critical-paths/*.
- Expected work surface: Shared UI harness and route/test data setup.
- Expected work surface: AutomationIds from screen specs.

## Implication

Requires deterministic local data and must not mutate Outlook/Box; use replay/local copies only.
