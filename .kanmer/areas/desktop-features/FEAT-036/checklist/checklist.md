# Checklist — FEAT-036 Desktop vehicle workflow

- [ ] Read FEAT-035 contract and design screen spec; identify existing validation use case.
- [ ] Implement input, request, result, explicit provider state and accept flow through the generated client.
- [ ] Reuse shared field/problem/provenance controls instead of local variants.
- [ ] Test invalid input, provider timeout/unavailable, accepted result, accessibility and keyboard path.
- [ ] Verify: Tests prove no request is sent for invalid input.
- [ ] Verify: Provider failure is explicit and provenance is exposed to UIA.
- [ ] Verify: No direct provider traffic or credential is packaged.
- [ ] Record simplification and independent review evidence.
