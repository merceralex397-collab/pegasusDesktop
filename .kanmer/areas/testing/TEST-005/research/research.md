# Research — TEST-005 View-model test catalogue

## Question

Create the view-model coverage catalogue for states, commands, cancellation, dirty state, validation, navigation, stale session and mandatory update.

## Verified context

- EPIC-009 requires the local production-mimicking Test/UAT stack: local gateway/Worker, Azurite, LocalDB and replay adapters; no Azure dev/test/staging environment is in scope.
- The desktop stays native WinUI 3, gateway-backed, and one Core business-policy owner remains authoritative.
- The run-tests skill requires platform/framework detection from global.json, project files and repository test configuration before choosing exact test flags.

## Ticket findings

- Expected work surface: tests/Pegasus.Desktop.ViewModelTests/*.
- Expected work surface: Desktop view-model command/state models.
- Expected work surface: Shared fake clock and gateway fixtures.

## Implication

Core policy is asserted through contracts/view-model outputs only; test code must not become a second rule owner.
