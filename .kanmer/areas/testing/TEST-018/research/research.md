# Research — TEST-018 Golden-file report parity lane

## Question

Run area-07 fixtures through the stack and compare desktop WebView2 output to gateway renderer within documented tolerances.

## Verified context

- EPIC-009 requires the local production-mimicking Test/UAT stack: local gateway/Worker, Azurite, LocalDB and replay adapters; no Azure dev/test/staging environment is in scope.
- The desktop stays native WinUI 3, gateway-backed, and one Core business-policy owner remains authoritative.
- The run-tests skill requires platform/framework detection from global.json, project files and repository test configuration before choosing exact test flags.

## Ticket findings

- Expected work surface: eng/reports/* or tests report parity project.
- Expected work surface: Governed Scriban/CSS fixtures.
- Expected work surface: artifacts/report-parity evidence.

## Implication

WebView2 runtime drift is recorded as environment evidence; no pixel-only or silent-baseline approach.
