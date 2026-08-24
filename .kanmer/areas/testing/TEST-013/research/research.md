# Research — TEST-013 Desktop CI lanes

## Question

Add desktop-build, desktop-package, desktop-ui-smoke and packaging-tests CI lanes without regressing Linux jobs.

## Verified context

- EPIC-009 requires the local production-mimicking Test/UAT stack: local gateway/Worker, Azurite, LocalDB and replay adapters; no Azure dev/test/staging environment is in scope.
- The desktop stays native WinUI 3, gateway-backed, and one Core business-policy owner remains authoritative.
- The run-tests skill requires platform/framework detection from global.json, project files and repository test configuration before choosing exact test flags.

## Ticket findings

- Expected work surface: .github/workflows/ci.yml.
- Expected work surface: GitHub build actions/scripts.
- Expected work surface: Existing test and packaging commands.

## Implication

Private Windows runner time is a cost constraint; do not add duplicate lanes or a new CI platform.
