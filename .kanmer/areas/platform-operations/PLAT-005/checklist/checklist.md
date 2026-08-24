# Checklist — PLAT-005

## Implementation

- [ ] 1. Orientation. Read the plan row, `docs/desktop/03-gateway-api-and-data/README.md` § 5 (rows `DSK-03-03`, `DSK-03-08`, `DSK-03-15`, `DSK-03-18`) and `docs/desktop/08-testing/README.md` § 5 row `DSK-08-02`. Call Kanmer `get_doc_gates` with this ticket's board id, then `take_ticket`.

- [ ] 2. Branch `task/dsk-10-05-authorization-direct-object-tests` from `dev`.

- [ ] 3. Build the endpoint inventory: enumerate every endpoint registered under `/api/v1` (read the route-group registrations added by `DSK-03-02`/`DSK-03-03` and the committed OpenAPI snapshot `openapi/pegasus-v1.json` from `DSK-03-04`). Write the list into the ticket's `files` document as a table of `method · route · required StaffAccessRight`. An endpoint whose required right cannot be named from the code is a finding — file it as an open question, do not guess.

- [ ] 4. Extend the authorization theory template from `DSK-08-02` in `tests/Pegasus.Api.ContractTests` so each endpoint gets: unauthenticated → 401; authenticated with the wrong `StaffAccessRight` → 403 with the documented problem type; authenticated with the right → success. Drive the theory from the inventory table so a new endpoint without a row fails the suite.

- [ ] 5. Add direct-object tests: for each endpoint that takes a case, document, intake or organization identifier, call it with an identifier the actor may not reach and assert the same refusal shape as an unknown identifier (never a different status that discloses existence). Assert the `SecurityEvent`/`ActionHistoryEntry` written for the refusal.

- [ ] 6. Add version-spoofing tests against the middleware from `DSK-04-06`: a request with `X-Pegasus-Client-Version` below the configured minimum is refused with `client-unsupported`; a request with a **missing** header is refused; a request with a malformed or absurdly high value is refused rather than trusted. Confirm the middleware covers the whole `/api/v1` group, not individual endpoints, and add the architecture-style assertion if `DSK-04-06` did not.

- [ ] 7. Add the automation-token test: a token issued to the Automation client is refused on `/api/v1` (proposal §8.3 and the ADR-0011 boundary already encoded in `StaffAuthorization.cs`).

- [ ] 8. For the package side, extend `eng/packaging/Test-Package.ps1` (created by `DSK-08-10`) with two scenarios: (a) flip one byte in the signed `.msix` and assert `Add-AppxPackage` fails with a signature error and installs nothing; (b) edit the `.appinstaller` XML after signing (change the `Uri` or the version) and assert App Installer refuses it. Use `microsoft_docs_search` for the exact failure codes before asserting on message text.

- [ ] 9. Run the package scenarios on the Test/UAT stack's local feed (`DSK-04-12`), not against the production UNC share. Capture the failure output as the proof artifact.

- [ ] 10. Run `dotnet test ./tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj --configuration Release` and `pwsh ./eng/packaging/Test-Package.ps1 -Scenario SignatureFailure,ManifestTampering`. Both green.

- [ ] 11. Load `test-gap-analysis` and produce the gap report: every command endpoint present in the OpenAPI snapshot must appear in the inventory table and in both an allow and a deny test. File each remaining gap as its own ticket rather than widening this one.

- [ ] 12. Update the threat register rows "accidental over-permission" and "compromised update package/feed" with the test names ([[DSK-10-01]]).

- [ ] 13. Record `## Simplification pass` with today's date over the branch diff in the ticket's `plan` document, open the PR into `dev`, and hand review to `pegasus-desktop-reviewer`.

## Verification

- [ ] `dotnet test ./tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj --configuration Release` — expected: all authorization theories pass and the suite fails if an endpoint row is removed.
- [ ] `pwsh ./eng/packaging/Test-Package.ps1 -Scenario SignatureFailure,ManifestTampering` — expected: both scenarios report the install refused, exit code 0 for the test run.
- [ ] `test-gap-analysis` report attached to the ticket — expected: no uncovered command endpoint.

## Progress notes

Record only factual progress here; unresolved decisions remain in `open-questions`.
