# Checklist — DUI-004 Authenticated shell

- [ ] Read screen-specs Shell and the existing foundation shell before selecting extension points.
- [ ] Implement the authority route order and fixed rail/title/status layout with ThemeResources and AutomationIds.
- [ ] Bind counts to the gateway rail-counts query and represent loading, stale and unavailable states explicitly.
- [ ] Consume the FND-033 environment-badge decision when available; do not invent a local label.
- [ ] Exercise keyboard rail navigation, count refresh and constrained-width layout.
- [ ] Verify: UI automation confirms route order and selection semantics.
- [ ] Verify: Gateway-backed count states never present unavailable/stale data as `0`.
- [ ] Verify: Keyboard and 200%/High Contrast screenshots pass.
- [ ] Record the simplification pass and independent review in the plan before merge.
