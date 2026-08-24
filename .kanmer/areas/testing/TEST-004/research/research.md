# Research — TEST-004 Desktop ViewModelTests project

## Question

Scaffold tests/Pegasus.Desktop.ViewModelTests targeting net10.0-windows10.0.26100.0 with no UI-thread requirement.

## Verified context

- EPIC-009 requires the local production-mimicking Test/UAT stack: local gateway/Worker, Azurite, LocalDB and replay adapters; no Azure dev/test/staging environment is in scope.
- The desktop stays native WinUI 3, gateway-backed, and one Core business-policy owner remains authoritative.
- The run-tests skill requires platform/framework detection from global.json, project files and repository test configuration before choosing exact test flags.

## Ticket findings

- Expected work surface: tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj.
- Expected work surface: Shared deterministic fake clock and gateway fakes.
- Expected work surface: Pegasus.slnx.

## Implication

Do not add another FixedTimeProvider or a desktop-specific business-rule copy.
