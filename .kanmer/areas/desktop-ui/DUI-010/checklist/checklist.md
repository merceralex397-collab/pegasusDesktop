# Checklist — DUI-010 ProblemInfoBar

- [x] Inspect the API problem-details mapping and authority copy lists.
- [x] Create the narrow problem-presentation model and InfoBar style using one sentence plus expandable/copyable Reference.
- [x] Add guard tests for known mappings, banned words and raw-code leakage.
- [x] Render representative retry, unavailable, denied and validation cases in a test host.
- [x] Verify: View-model tests fail for a banned term or raw problem code.
- [x] Verify: UIA exposes a copyable Reference only when supplied by the gateway.
- [x] Verify: InfoBar state remains screen-local and never claims an external action succeeded.
- [x] Record simplification and independent review evidence.

## Evidence limitation

The repository has no `tests/Pegasus.Desktop.UITests/problem-tests.ps1` harness yet, so the implementation-agent direct UIA run is recorded in the post-implementation report and the scripted UI/theme sweep remains unclaimed.
