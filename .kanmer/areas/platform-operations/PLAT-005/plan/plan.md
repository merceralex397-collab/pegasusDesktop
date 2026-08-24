# Plan — PLAT-005

## Objective

Give every `/api/v1` command an allow test and a deny test — role bypass, foreign case/document identifiers, and spoofed `X-Pegasus-Client-Version` — and prove that a tampered `.appinstaller` or package fails to install because its signature no longer validates.

## Chosen approach

Proposal §22.2 `:1613-1620` lists role bypass, direct-object access, update-manifest tampering and API version spoofing as security tests, and §22.3 `:1661` makes "every API command has authorization and failure-path tests" a coverage requirement rather than a percentage. The gateway's own boundary already fails closed (`src/Pegasus.Core/Identity/StaffAuthorization.cs`), but the desktop turns each Razor handler into an addressable endpoint, so an endpoint that forgets its `StaffAccessRight` filter is now reachable by anything holding a staff token. Operator-visible consequence: an operator without the right sees or changes a case they must not, or an old client is silently allowed past the minimum-version gate. Siblings: [[DSK-10-04]] (token path), [[DSK-10-06]] (uploads), [[DSK-10-01]] (register).

## Governing docs

- No canonical PRD/FRD/ADR is linked yet. `docs_todo: true` is intentionally retained: several desktop conversion decisions named by the ticket are planned canonical documents and must not be linked until they exist on `origin/dev`.
- Use the ticket's Source of truth and the owning desktop-area plan as the current planning authority; add a real governing-doc ref only through `link_doc` after the file exists.

## Routing

- **Subagents**: `pegasus-test-engineer` — `.codex/agents/pegasus-test-engineer.toml` (API side); `pegasus-release-packager` — `.codex/agents/pegasus-release-packager.toml` (package tampering side)
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `test-gap-analysis` (dotnet/skills `98f84851`, plugin `dotnet-test`) → `code-testing-agent` (same pin) → `winui-packaging` (`.codex/skills/winui-packaging/SKILL.md`, win-dev-skills v0.5.0 `f1028dd5`) for the signature-failure scenario
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search`) for App Installer signature validation and `Add-AppxPackage` failure codes
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout`
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Ordered implementation

1. Orientation. Read the plan row, `docs/desktop/03-gateway-api-and-data/README.md` § 5 (rows `DSK-03-03`, `DSK-03-08`, `DSK-03-15`, `DSK-03-18`) and `docs/desktop/08-testing/README.md` § 5 row `DSK-08-02`. Call Kanmer `get_doc_gates` with this ticket's board id, then `take_ticket`.
2. Branch `task/dsk-10-05-authorization-direct-object-tests` from `dev`.
3. Build the endpoint inventory: enumerate every endpoint registered under `/api/v1` (read the route-group registrations added by `DSK-03-02`/`DSK-03-03` and the committed OpenAPI snapshot `openapi/pegasus-v1.json` from `DSK-03-04`). Write the list into the ticket's `files` document as a table of `method · route · required StaffAccessRight`. An endpoint whose required right cannot be named from the code is a finding — file it as an open question, do not guess.
4. Extend the authorization theory template from `DSK-08-02` in `tests/Pegasus.Api.ContractTests` so each endpoint gets: unauthenticated → 401; authenticated with the wrong `StaffAccessRight` → 403 with the documented problem type; authenticated with the right → success. Drive the theory from the inventory table so a new endpoint without a row fails the suite.
5. Add direct-object tests: for each endpoint that takes a case, document, intake or organization identifier, call it with an identifier the actor may not reach and assert the same refusal shape as an unknown identifier (never a different status that discloses existence). Assert the `SecurityEvent`/`ActionHistoryEntry` written for the refusal.
6. Add version-spoofing tests against the middleware from `DSK-04-06`: a request with `X-Pegasus-Client-Version` below the configured minimum is refused with `client-unsupported`; a request with a **missing** header is refused; a request with a malformed or absurdly high value is refused rather than trusted. Confirm the middleware covers the whole `/api/v1` group, not individual endpoints, and add the architecture-style assertion if `DSK-04-06` did not.
7. Add the automation-token test: a token issued to the Automation client is refused on `/api/v1` (proposal §8.3 and the ADR-0011 boundary already encoded in `StaffAuthorization.cs`).
8. For the package side, extend `eng/packaging/Test-Package.ps1` (created by `DSK-08-10`) with two scenarios: (a) flip one byte in the signed `.msix` and assert `Add-AppxPackage` fails with a signature error and installs nothing; (b) edit the `.appinstaller` XML after signing (change the `Uri` or the version) and assert App Installer refuses it. Use `microsoft_docs_search` for the exact failure codes before asserting on message text.
9. Run the package scenarios on the Test/UAT stack's local feed (`DSK-04-12`), not against the production UNC share. Capture the failure output as the proof artifact.
10. Run `dotnet test ./tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj --configuration Release` and `pwsh ./eng/packaging/Test-Package.ps1 -Scenario SignatureFailure,ManifestTampering`. Both green.
11. Load `test-gap-analysis` and produce the gap report: every command endpoint present in the OpenAPI snapshot must appear in the inventory table and in both an allow and a deny test. File each remaining gap as its own ticket rather than widening this one.
12. Update the threat register rows "accidental over-permission" and "compromised update package/feed" with the test names ([[DSK-10-01]]).
13. Record `## Simplification pass` with today's date over the branch diff in the ticket's `plan` document, open the PR into `dev`, and hand review to `pegasus-desktop-reviewer`.

## Verification

- [ ] `dotnet test ./tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj --configuration Release` — expected: all authorization theories pass and the suite fails if an endpoint row is removed.
- [ ] `pwsh ./eng/packaging/Test-Package.ps1 -Scenario SignatureFailure,ManifestTampering` — expected: both scenarios report the install refused, exit code 0 for the test run.
- [ ] `test-gap-analysis` report attached to the ticket — expected: no uncovered command endpoint.

## Risks and constraints

- **Azure**: no write. Package scenarios run against the local Test/UAT feed (L-02), never against the production UNC share.
- **Scope boundary**: may add tests and packaging scenarios; may add the missing middleware-coverage assertion. Must not change endpoint authorization behaviour — a missing right is a new `fix` ticket. Board placement note: plan 00 § Kanmer board shape assigns no board area to plan 10, so this epic seeds into `platform-operations` (prefix `PLAT`).
- **Traps**: an inventory built by hand goes stale — drive the theories from the committed OpenAPI snapshot so the suite breaks when an endpoint is added; asserting on localized message text instead of a status/problem type produces a flaky test; the update-feed row is about SMB ACLs and signature validation under D-002/D-003, not public HTTPS.
- **Concern**: this row carries both an API test set and a packaging test set. Keep the two in one ticket as the plan states, but land them as two commits so the reviewer can read them separately.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the ticket's `plan` document.

## Simplification pass

Before the PR, independently review the branch diff for reuse, unnecessary abstraction, duplicated policy, and scope expansion; record findings and dispositions here.
