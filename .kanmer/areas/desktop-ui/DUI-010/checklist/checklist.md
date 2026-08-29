# Checklist — DUI-010 ProblemInfoBar

- [x] Inspect the API problem-details mapping and authority copy lists.
- [x] Create the narrow problem-presentation model and InfoBar style using one sentence plus expandable/copyable Reference.
- [x] Add guard tests for known mappings, exact severities, banned words and raw-code leakage.
- [ ] Render representative retry, unavailable, denied and validation cases in the [[DUI-002]] non-production gallery/test host; ordinary startup must not show synthetic failure states.
- [x] Verify: View-model tests fail for a banned term or raw problem code.
- [x] Verify: UIA exposes a copyable Reference only when supplied by the gateway.
- [x] Verify: InfoBar state remains screen-local and never claims an external action succeeded.
- [ ] Record a fresh independent review after the remediation commit.

## Evidence limitation

The repository has no `tests/Pegasus.Desktop.UITests/problem-tests.ps1` harness yet, so scripted UI automation, the 200% scale check, and the Dark/High Contrast sweep remain unclaimed and are owned by [[TEST-006]]/[[DUI-002]]. The initial direct UIA clipboard check remains evidence for the prior control shape only; it is not a substitute for the missing full acceptance run.
