# Research — TEST-009 Accessibility lane

## Question

Add AxeWindowsCLI scanning and the recorded-review checklist to the desktop accessibility lane.

## Verified context

- EPIC-009 requires the local production-mimicking Test/UAT stack: local gateway/Worker, Azurite, LocalDB and replay adapters; no Azure dev/test/staging environment is in scope.
- The desktop stays native WinUI 3, gateway-backed, and one Core business-policy owner remains authoritative.
- The run-tests skill requires platform/framework detection from global.json, project files and repository test configuration before choosing exact test flags.

## Ticket findings

- Expected work surface: tests/Pegasus.Desktop.UITests accessibility scripts.
- Expected work surface: AxeWindowsCLI report capture.
- Expected work surface: artifacts/a11y evidence convention.

## Implication

Automation does not replace human review; no Azure test environment is added.
