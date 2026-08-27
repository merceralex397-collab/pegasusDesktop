# Checklist — TEST-001 API contract-test project

- [x] Inspect existing test platform, global.json and package-management conventions before adding the project.
- [x] Create the minimal xUnit/WebApplicationFactory project and add it to the solution.
- [x] Reuse gateway fixture/auth test helpers where they exist; do not reproduce Core business policy.
- [x] Run locked restore, Release build and the focused project test.
- [x] Verify: The project appears once in Pegasus.slnx and builds with warnings-as-errors.
- [x] Verify: Focused API contract test command passes using the detected test runner.
- [x] Verify: Fixture does not require live Azure, Box or Graph.
- [ ] Record exact test command/output, simplification pass and independent review.
