# Checklist — TEST-018 Golden-file report parity lane

- [ ] Confirm the governed fixture set and the permitted comparison tolerances before writing tests.
- [ ] Run gateway and isolated desktop renderer against the same inputs.
- [ ] Compare structural/text/render outputs with documented tolerance; never silently rebaseline.
- [ ] Record mismatches, environment/runtime version and disposition.
- [ ] Verify: Fixture provenance is immutable and comparison threshold documented.
- [ ] Verify: A deliberate output mismatch fails the lane.
- [ ] Verify: Gateway renderer remains until parity evidence passes.
- [ ] Record exact test command/output, simplification pass and independent review.
