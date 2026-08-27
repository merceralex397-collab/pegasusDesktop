# Checklist — FEAT-031 Box broker endpoints

- [x] Inventory existing Box ports, Core use cases, custody/state queries, authorization, and audit/mutation conventions.
- [x] Add the minimal `/api/v1` broker contracts/routes for list, metadata, streamed download, bounded upload sessions, logical removal, and evidence confirmation.
- [x] Apply authorization, problem-details, correlation, no-provider-secret/no-reusable-URL/no-object-ID response constraints.
- [x] Add existing-project web contract coverage for allowed, denied, expired, failure, conditional-response, cache, upload quota, expiry, completion concurrency, and operation-key semantics.
- [x] Verify Release solution build (0 warnings/errors), 26 exact-head broker-focused tests, diff check, and changed production-scope secret/provider scan.
- [x] Keep the standalone contract-test scaffold out of the solution/CI and avoid direct desktop Box SDK calls.
- [x] Exercise existing LocalDB/Ef custody persistence through the gateway for canonical SHA/metadata, abandoned-session non-persistence, confirmation history, and operation-key replay.
- [x] Record the simplification pass and remediation of the initial independent review findings.
- [ ] Prove PLAT-039 token-age behaviour and PLAT-041 O(1)+N call-budget/export-gallery behaviour in this fork, or route the exact dependency through Kanmer; affected acceptance remains open and export/gallery stays unexposed.
- [ ] Obtain fresh independent review of exact head `3860d43f`.
