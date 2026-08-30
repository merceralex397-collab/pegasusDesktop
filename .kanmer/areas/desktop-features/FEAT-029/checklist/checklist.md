# Checklist — FEAT-029

One box per plan step, in plan order. A checked box means the evidence is recorded below or in the linked pipeline document.

- [x] Read the governing plan row, endpoint map, screen specification and FRD; called `get_doc_gates FEAT-029`; took the ticket on `task/dsk-07-03-mail-endpoints` from `origin/dev`.
- [x] Rechecked `GWY-012` and its gates before implementation; it remained Preparing, unclaimed, with no landed mail route implementation. Recorded the single-group ownership decision in `plan`.
- [x] Re-read the Razor mail handlers and recorded the implementation basis in `research`; the existing Core owners and operator consequences are preserved.
- [x] Implemented `GET /api/v1/mail` over retained-mail listing, mailbox listing and freshness, newest first, with weak ETag/version, page-size validation at 100, and destination/classification mutual exclusion.
- [x] Implemented `GET /api/v1/mail/{id}/preview` over `GetRetainedMail` with inert preview projection.
- [x] Implemented `GET /api/v1/mail/{id}` over `GetRetainedMail`, passing the search term through and projecting detail/version data.
- [x] Implemented `GET /api/v1/mail/deleted` over `SearchDeletedMail`, with its separate mailbox list, cap, state and truncation fields, and no write path.
- [x] Implemented link/unlink prepare routes over the existing case query and edit-lease ports, including version and eligibility checks.
- [x] Implemented link/unlink confirmation routes over `ILinkIntake` and `IReverseIntakeLink`, with prepared tuple, versions, lease, operation key, reason and unlink consequence text.
- [x] Implemented classification correction over `CorrectRetainedMailClassification`, canonical selection parsing, audit correlation and version-conflict mapping.
- [x] Implemented move-to-recommended-folder over `MoveRetainedMailFolder`, with version/policy/reason validation, bare-GUID key validation and distinct outcome messages.
- [x] Registered the ten implemented mail routes in the one existing `/api/v1` mail group behind the desktop feature flag and `PerformCasework` authorization filter.
- [x] Projected the complete Core-owned folder recommendation, suggested move and latest move without resolving a second Web-side provider authority.
- [x] Added API contract coverage for reads, paging, ETag, inert preview, prepare/confirm, classification, move, credential non-leakage and problem translation.
- [x] Added explicit move-key facts: a `desk:` key is rejected and a bare GUID succeeds.
- [x] Asserted both folder-mover compositions: capability present with a supplied mover and absent with the unavailable default.
- [x] Added gate-off, unauthenticated and wrong-role authorization facts for every implemented mail route.
- [x] Added the SQL-backed parity fact proving equivalent Razor/API link confirmation produces the same Core association and mutation-history version effects.
- [x] Reviewed response DTOs against the Core projections and asserted no provider credentials or raw provider payloads are exposed.
- [x] Corrected the endpoint-map mail rows for paging, classification idempotency, detail recommendation data and deleted search.
- [x] Added the desktop mail behavior clause to FRD-08.
- [x] Regenerated `openapi/pegasus-v1.json` and verified the committed snapshot. Kiota generation and generated-client commit are explicitly owned by [[GWY-005]]; that tree is not duplicated in this ticket while [[FND-031]]/[[GWY-004]] remain its documented prerequisites.
- [x] Ran the simplification pass and recorded the dated findings/dispositions in `plan`.
- [x] Ran the final verification: API contracts 62/62; MailWorkspaceWebTests 39/39; SQL Razor/API parity 1/1; broad non-browser integration 974 passed, 2 skipped, 0 failed, 976 total; full Release solution build 0 warnings/0 errors; guarded scope diff empty.

## Evidence notes

- Exact broad command: `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --filter "Category!=Corpus&Category!=Browser" --nologo` — 974 passed, 2 skipped, 0 failed, 976 total, 12m52s.
- No Azure/cloud write, upstream sync, corpus modification, or change to the Razor mail pages, Worker or Graph infrastructure was made.
