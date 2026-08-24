# Checklist — TEST-015 Performance scripts

- [ ] Confirm baseline workstation specification and budgets before measurement.
- [ ] Create repeatable scripts/traces for each named scenario using local Test/UAT fixtures.
- [ ] Capture startup/navigation/list/report metrics and resource traces with environmental metadata.
- [ ] Report regressions against budgets; do not tune blindly or manufacture concurrent load data.
- [ ] Verify: Each metric has machine/build/dataset context.
- [ ] Verify: Slow network/provider timeout demonstrates usable failure state.
- [ ] Verify: Results distinguish measured data from unrun pilot-ring checks.
- [ ] Record exact test command/output, simplification pass and independent review.
