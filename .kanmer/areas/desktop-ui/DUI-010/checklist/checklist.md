# Checklist — DUI-010 ProblemInfoBar

- [ ] Inspect the API problem-details mapping and authority copy lists.
- [ ] Create the narrow problem-presentation model and InfoBar style using one sentence plus expandable/copyable Reference.
- [ ] Add guard tests for known mappings, banned words and raw-code leakage.
- [ ] Render representative retry, unavailable, denied and validation cases in a test host.
- [ ] Verify: View-model tests fail for a banned term or raw problem code.
- [ ] Verify: UIA exposes a copyable Reference only when supplied by the gateway.
- [ ] Verify: InfoBar state remains screen-local and never claims an external action succeeded.
- [ ] Record simplification and independent review evidence.
