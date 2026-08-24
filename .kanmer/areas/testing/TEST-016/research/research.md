# Research — TEST-016 End-to-end UAT scenarios

## Question

Create the fourteen business UAT scripts, each mapped to local Test/UAT or the production pilot ring with pass/fail recording.

## Verified context

- EPIC-009 requires the local production-mimicking Test/UAT stack: local gateway/Worker, Azurite, LocalDB and replay adapters; no Azure dev/test/staging environment is in scope.
- The desktop stays native WinUI 3, gateway-backed, and one Core business-policy owner remains authoritative.
- The run-tests skill requires platform/framework detection from global.json, project files and repository test configuration before choosing exact test flags.

## Ticket findings

- Expected work surface: docs/desktop/08-testing/uat-scenarios/*.
- Expected work surface: Test/UAT stack lifecycle/evidence format.
- Expected work surface: Pilot-ring boundary notes.

## Implication

This is UAT evidence, not a second functional specification; FRDs remain behaviour authority.
