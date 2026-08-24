# Research — TEST-008 Document, vehicle and report UI scripts

## Question

Script document upload, vehicle lookup, report preview and report finalise on the native stack.

## Verified context

- EPIC-009 requires the local production-mimicking Test/UAT stack: local gateway/Worker, Azurite, LocalDB and replay adapters; no Azure dev/test/staging environment is in scope.
- The desktop stays native WinUI 3, gateway-backed, and one Core business-policy owner remains authoritative.
- The run-tests skill requires platform/framework detection from global.json, project files and repository test configuration before choosing exact test flags.

## Ticket findings

- Expected work surface: tests/Pegasus.Desktop.UITests/integration-flows/*.
- Expected work surface: Local Test/UAT replay fixtures and report test data.
- Expected work surface: AutomationIds for document/vehicle/report surfaces.

## Implication

No live provider credentials, provider direct calls or fabricated corpus artifacts.
