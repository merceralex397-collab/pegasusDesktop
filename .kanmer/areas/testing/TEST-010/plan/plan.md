# Plan — TEST-010 Packaging update tests

## Governing documents

This ticket remains docs_todo: true until the planned desktop governing documents are authored. The local Test/UAT and locked-decision material is binding now; do not create a competing product document in this task.

## Chosen approach

Author eng/packaging/Test-Package.ps1 tests for install, upgrade, forced update, blocked client, signature failure, interruption, rollback, uninstall, no-admin and certificate trust.

## Steps

1. Read the D-002/D-003 signed-MSIX/UNC-feed decisions and existing packaging scripts.
2. Implement isolated development-certificate/package lifecycle cases with explicit setup/cleanup.
3. Assert gateway minimum-version block and rollback outcomes without touching production feed/certificate.
4. Record exact package/build and result evidence per case.

## Verification

- Every listed lifecycle case yields pass/fail evidence.
- No production PFX or trusted-root installation is used.
- Test runs are repeatable on the dedicated Test/UAT workstation.

## Risks

Self-managed certificate and UNC feed are real production mechanisms; do not substitute Azure/GitHub hosting.

Use the detected runner/framework and record exact command output when implementation begins. Complete a simplification pass and independent review before merge.
