# Research — TEST-015 Performance scripts

## Question

Measure startup, navigation, large list, heavy case, memory, slow network, provider timeout, ten users and report generation on the named Test/UAT workstation.

## Verified context

- EPIC-009 requires the local production-mimicking Test/UAT stack: local gateway/Worker, Azurite, LocalDB and replay adapters; no Azure dev/test/staging environment is in scope.
- The desktop stays native WinUI 3, gateway-backed, and one Core business-policy owner remains authoritative.
- The run-tests skill requires platform/framework detection from global.json, project files and repository test configuration before choosing exact test flags.

## Ticket findings

- Expected work surface: eng/performance/*.
- Expected work surface: tests/Pegasus.Desktop.UITests performance journeys.
- Expected work surface: artifacts/performance evidence.

## Implication

No Azure test estate; ten-user result needs controlled local/replay harness, not live operator data.
