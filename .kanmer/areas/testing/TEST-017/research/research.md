# Research — TEST-017 Test/UAT stack lifecycle

## Question

Build TestStack lifecycle support in Invoke-LocalDevelopment.ps1 with doctor prerequisites, local feed and Publish-Feed.

## Verified context

- EPIC-009 requires the local production-mimicking Test/UAT stack: local gateway/Worker, Azurite, LocalDB and replay adapters; no Azure dev/test/staging environment is in scope.
- The desktop stays native WinUI 3, gateway-backed, and one Core business-policy owner remains authoritative.
- The run-tests skill requires platform/framework detection from global.json, project files and repository test configuration before choosing exact test flags.

## Ticket findings

- Expected work surface: scripts/Invoke-LocalDevelopment.ps1.
- Expected work surface: scripts/Invoke-Doctor.ps1.
- Expected work surface: local feed/Publish-Feed scripts and docs.

## Implication

LocalDB is Windows-only and scripts must avoid impacting mailbox/Box locations.
