# Research — TEST-011 Security test set

## Question

Build security tests for token lifecycle, disabled account, roles, direct-object access, malformed uploads, unsafe paths, tampered manifests, version spoofing, temp ACLs and secret/log scanning.

## Verified context

- EPIC-009 requires the local production-mimicking Test/UAT stack: local gateway/Worker, Azurite, LocalDB and replay adapters; no Azure dev/test/staging environment is in scope.
- The desktop stays native WinUI 3, gateway-backed, and one Core business-policy owner remains authoritative.
- The run-tests skill requires platform/framework detection from global.json, project files and repository test configuration before choosing exact test flags.

## Ticket findings

- Expected work surface: tests/Pegasus.Api.ContractTests security tests.
- Expected work surface: tests/Pegasus.Desktop.* security/packaging tests.
- Expected work surface: eng/security scripts.

## Implication

Security tests use synthetic technical fixtures only; no live credentials or unauthorized attack target.
