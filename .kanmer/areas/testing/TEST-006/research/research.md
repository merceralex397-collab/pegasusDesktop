# Research — TEST-006 Desktop UI-test harness

## Question

Create the ui-tests.ps1 harness around winapp ui and the AutomationId contract for the native desktop app.

## Verified context

- EPIC-009 requires the local production-mimicking Test/UAT stack: local gateway/Worker, Azurite, LocalDB and replay adapters; no Azure dev/test/staging environment is in scope.
- The desktop stays native WinUI 3, gateway-backed, and one Core business-policy owner remains authoritative.
- The run-tests skill requires platform/framework detection from global.json, project files and repository test configuration before choosing exact test flags.

## Ticket findings

- Expected work surface: tests/Pegasus.Desktop.UITests/ui-tests.ps1.
- Expected work surface: tests/Pegasus.Desktop.UITests/* batch scripts/fixtures.
- Expected work surface: AutomationId conventions in screen specs.

## Implication

UI tests mutate installed packages; run only on a dedicated workstation/runner and retain manual a11y review.
