# Checklist — DUI-012 Page header and manual refresh

- [ ] Model supported query states and timestamp semantics from the authority and gateway envelope.
- [ ] Implement header and refresh components with title, exact filter, last-good state and one primary action.
- [ ] Protect refresh from double submission and keep last-good data visible during failed/stale states.
- [ ] Add view-model and UI automation for same-filter rerun and keyboard access.
- [ ] Verify: A refresh reuses current filter/page rather than resetting it.
- [ ] Verify: Stale/unavailable data is explicitly labelled and never rendered as zero.
- [ ] Verify: No introductory or lede copy appears beneath a page title.
- [ ] Record simplification and independent review evidence.
