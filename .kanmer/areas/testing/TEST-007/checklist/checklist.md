# Checklist — TEST-007 UI critical-path scripts

- [ ] Map each journey to an approved local Test/UAT fixture and stable AutomationIds.
- [ ] Implement wait-for UIA scripts for launch/session, read/write and conflict paths.
- [ ] Drive keyboard navigation explicitly and assert visible result/state after each command.
- [ ] Capture screenshot/recording and report step-level failure evidence.
- [ ] Verify: Every journey can run without sleep-based timing.
- [ ] Verify: Concurrency path asserts no silent overwrite.
- [ ] Verify: Keyboard journey covers the authority map relevant to the flow.
- [ ] Record exact test command/output, simplification pass and independent review.
