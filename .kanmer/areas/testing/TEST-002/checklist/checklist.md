# Checklist — TEST-002 Authorization and failure-path test template

- [x] Inventory the current `/api/v1` endpoint data source and record that no command endpoints are merged yet.
- [x] Add a parameterized row contract for unauthenticated, wrong-right, stale-version, invalid-request, and idempotent-replay assertions.
- [x] Derive command discovery from the real host endpoint data source; do not maintain a duplicate endpoint inventory.
- [x] Add the symmetric guard and a derived-host `POST /api/v1/__probe` red-path test without changing product routing.
- [x] Keep the current literal table empty because the live command inventory is empty; future command tickets must add concrete rows.
- [x] Add shared problem-details, bearer-challenge, exact-title, current-version, response-equality, and effect-snapshot assertions without recreating business policy.
- [x] Guard operation-key/replay and version/stale-row symmetry so incomplete future rows fail explicitly.
- [x] Document the extension rule in `docs/desktop/08-testing/README.md`.
- [x] Run locked restore, Release build, focused contract tests, and the repository non-corpus test suite.
- [x] Record the simplification pass and independent-review dispositions in the plan.
- [ ] Obtain a passing independent review of the remediated exact PR head.
- [ ] Obtain green exact-head CI, merge the reviewed PR to `dev`, promote the exact reviewed SHA to `main`, and write merged proof.
