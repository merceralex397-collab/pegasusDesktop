# Checklist — TEST-010 Packaging update tests

- [ ] Read the D-002/D-003 signed-MSIX/UNC-feed decisions and existing packaging scripts.
- [ ] Implement isolated development-certificate/package lifecycle cases with explicit setup/cleanup.
- [ ] Assert gateway minimum-version block and rollback outcomes without touching production feed/certificate.
- [ ] Record exact package/build and result evidence per case.
- [ ] Verify: Every listed lifecycle case yields pass/fail evidence.
- [ ] Verify: No production PFX or trusted-root installation is used.
- [ ] Verify: Test runs are repeatable on the dedicated Test/UAT workstation.
- [ ] Record exact test command/output, simplification pass and independent review.
