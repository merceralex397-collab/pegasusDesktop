# Checklist — DUI-010 ProblemInfoBar

- [x] Inspect the API problem-details mapping and authority copy lists.
- [x] Create the reusable problem-presentation model and InfoBar with one sentence plus expandable/copyable Reference.
- [x] Add guard tests for known mappings, exact severities, banned words, gateway problem-type leakage and raw-code leakage.
- [ ] Render representative retry, unavailable, denied and validation cases in the [[DUI-002]] non-production gallery/test host; ordinary startup contains no synthetic failure states.
- [x] Verify: View-model tests fail for a banned term or raw problem code.
- [x] Verify: UIA copy logic places only the supplied Reference value on the clipboard; the earlier run is historical for the superseded sample page.
- [x] Verify: InfoBar state remains screen-local and never claims an external action succeeded.
- [ ] Verify runtime replacement announcements and stale-problem suppression in the UI harness owned by [[TEST-006]].
- [ ] Record a fresh independent review of remediation commit `681f6f16`.

## Evidence limitation

The repository has no `tests/Pegasus.Desktop.UITests/problem-tests.ps1` harness yet, so scripted UI automation, runtime announcement capture, the 200% scale check, keyboard walkthrough, and the Dark/High Contrast sweep remain unclaimed and are owned by [[TEST-006]]/[[DUI-002]].
