# Checklist — DUI-015 Accessibility automation lane

- [ ] Confirm TEST-012/TEST-013 runner decision and existing UI harness contract.
- [ ] Implement AutomationId audit from winapp UI inspection with documented OS-chrome exclusions.
- [ ] Run AxeWindowsCLI against the real launched app, archive the report and fail critical findings.
- [ ] Feed results to the UI test lane while preserving manual-review handoff.
- [ ] Verify: An intentionally missing interactive AutomationId fails the audit.
- [ ] Verify: A critical Axe finding fails the lane and produces a report path.
- [ ] Verify: Evidence records app build identity, exact commands and scan output.
- [ ] Record simplification and independent review evidence.
