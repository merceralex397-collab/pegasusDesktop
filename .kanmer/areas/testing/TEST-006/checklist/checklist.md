# Checklist — TEST-006 Desktop UI-test harness

- [ ] Read winui-ui-testing guidance and the screen-spec AutomationId convention.
- [ ] Create the harness that builds/launches through the approved workflow and captures PID/build identity.
- [ ] Provide wait-for based UIA helpers, screenshots/records and no Start-Sleep polling.
- [ ] Add a smoke route and AutomationId audit hook for downstream suites.
- [ ] Verify: Harness launches through winapp tooling, never direct exe execution.
- [ ] Verify: A deterministic smoke script finds named controls and captures evidence.
- [ ] Verify: Missing AutomationId is detectable by the downstream audit.
- [ ] Record exact test command/output, simplification pass and independent review.
