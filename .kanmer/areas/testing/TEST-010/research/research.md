# Research — TEST-010 Packaging update tests

## Question

Author eng/packaging/Test-Package.ps1 tests for install, upgrade, forced update, blocked client, signature failure, interruption, rollback, uninstall, no-admin and certificate trust.

## Verified context

- EPIC-009 requires the local production-mimicking Test/UAT stack: local gateway/Worker, Azurite, LocalDB and replay adapters; no Azure dev/test/staging environment is in scope.
- The desktop stays native WinUI 3, gateway-backed, and one Core business-policy owner remains authoritative.
- The run-tests skill requires platform/framework detection from global.json, project files and repository test configuration before choosing exact test flags.

## Ticket findings

- Expected work surface: eng/packaging/Test-Package.ps1.
- Expected work surface: MSIX/App Installer fixtures and development certificate setup.
- Expected work surface: Packaging evidence output.

## Implication

Self-managed certificate and UNC feed are real production mechanisms; do not substitute Azure/GitHub hosting.
