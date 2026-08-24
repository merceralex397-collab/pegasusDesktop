---
id: PLAT-005
type: ticket
title: >-
  DSK-10-05 · Authorization and direct-object tests for every /api/v1 command,
  plus manifest and version tampering
status: backlog
area: platform-operations
assignee: ''
profile: feature
labels:
  - desktop-conversion
  - plan-10
  - phase-8
  - tier-9
groups:
  - EPIC-011
  - HZN-009
links: []
docs_todo: true
archived: false
created: '2026-08-24T08:05:04.733Z'
updated: '2026-08-24T08:05:04.733Z'
---

## What

Give every `/api/v1` command an allow test and a deny test — role bypass, foreign case/document identifiers, and spoofed `X-Pegasus-Client-Version` — and prove that a tampered `.appinstaller` or package fails to install because its signature no longer validates.

## Why

Proposal §22.2 `:1613-1620` lists role bypass, direct-object access, update-manifest tampering and API version spoofing as security tests, and §22.3 `:1661` makes "every API command has authorization and failure-path tests" a coverage requirement rather than a percentage. The gateway's own boundary already fails closed (`src/Pegasus.Core/Identity/StaffAuthorization.cs`), but the desktop turns each Razor handler into an addressable endpoint, so an endpoint that forgets its `StaffAccessRight` filter is now reachable by anything holding a staff token. Operator-visible consequence: an operator without the right sees or changes a case they must not, or an old client is silently allowed past the minimum-version gate. Siblings: [[DSK-10-04]] (token path), [[DSK-10-06]] (uploads), [[DSK-10-01]] (register).

## Source of truth

- Plan row: `docs/desktop/10-security-observability-performance/README.md` § 5 — `DSK-10-05`
- Plan detail: same file § 2 (Facts — staff rights matrix fails closed), § 4 (target state)
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 8.3 Authorization `:446-453`; § 9 Forced updates and compatibility `:467-525`; § 22.2 Security tests `:1608-1621`; § 22.3 Coverage policy `:1655-1665`
- Repository evidence:
  - `src/Pegasus.Core/Identity/StaffAuthorization.cs:1-40` — the `StaffAccessRight` enum and the fail-closed switch every deny test must line up with
  - `src/Pegasus.Core/Identity/IdentityContracts.cs:98-137` — the audit records a refusal must write
  - `src/Pegasus.Web/Program.cs:517-522` — the fallback authorization policy that must not be the only defence on `/api/v1`
  - `tests/Pegasus.IntegrationTests/AutomationConnectorAuthorizationTests.cs` — an existing authorization test shape to copy
  - New: the `/api/v1` route groups and the per-group `StaffAccessRight` endpoint filter from `DSK-03-03`; the command endpoints from `DSK-03-08` and `DSK-03-15`; the client-version middleware from `DSK-04-06`; the `.appinstaller` template and validator from `DSK-09-03`
- Binding decisions:
  - **L-01** — the gateway is `Pegasus.Web` evolved in place; these tests run against that host, not a new deployment unit.
  - **ADR-0105** (to be authored) — signed MSIX/App Installer distribution with a gateway minimum-version gate; the two layers are tested here as one set.
  - **D-002 / D-003** — signature validation is against the self-managed certificate trusted in `LocalMachine\TrustedPeople`, and the feed is a UNC share; a tampering test must use that path, not a public HTTPS feed.
- Depends on: `DSK-03-03` (staff bearer actor resolution and per-group rights filter), `DSK-03-08` (case command endpoints), `DSK-03-15` (administration endpoints).

## Routing

- **Subagents**: `pegasus-test-engineer` — `.codex/agents/pegasus-test-engineer.toml` (API side); `pegasus-release-packager` — `.codex/agents/pegasus-release-packager.toml` (package tampering side)
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `test-gap-analysis` (dotnet/skills `98f84851`, plugin `dotnet-test`) → `code-testing-agent` (same pin) → `winui-packaging` (`.codex/skills/winui-packaging/SKILL.md`, win-dev-skills v0.5.0 `f1028dd5`) for the signature-failure scenario
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search`) for App Installer signature validation and `Add-AppxPackage` failure codes
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout`
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

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

## Acceptance criteria

- [ ] Every `/api/v1` endpoint in the committed OpenAPI snapshot appears in the inventory table with its required `StaffAccessRight`.
- [ ] Every endpoint has an allow test and a deny test; a new endpoint added without a row fails the suite.
- [ ] Foreign-identifier access is refused with the same shape as an unknown identifier and writes an audit record.
- [ ] Below-minimum, missing and malformed `X-Pegasus-Client-Version` values are all refused.
- [ ] A tampered package and a tampered `.appinstaller` both fail to install, with the failure captured.

## Verification

- [ ] `dotnet test ./tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj --configuration Release` — expected: all authorization theories pass and the suite fails if an endpoint row is removed.
- [ ] `pwsh ./eng/packaging/Test-Package.ps1 -Scenario SignatureFailure,ManifestTampering` — expected: both scenarios report the install refused, exit code 0 for the test run.
- [ ] `test-gap-analysis` report attached to the ticket — expected: no uncovered command endpoint.

## Evidence tier

Tier 9 — Security/observability. Here that obliges denial before the call reaches Core, an observable audit record for each refusal, and a real install refusal for the tampering cases rather than a unit-level assertion.

## Documentation changes

- `docs/desktop/10-security-observability-performance/threat-register.md` — record the test names against the over-permission and update-feed rows.
- `docs/desktop/08-testing/README.md` § 5 — cross-reference this ticket from row `DSK-08-11` if the reviewer finds the two lists have diverged; otherwise `None.`

## Guardrails

- **Azure**: no write. Package scenarios run against the local Test/UAT feed (L-02), never against the production UNC share.
- **Scope boundary**: may add tests and packaging scenarios; may add the missing middleware-coverage assertion. Must not change endpoint authorization behaviour — a missing right is a new `fix` ticket. Board placement note: plan 00 § Kanmer board shape assigns no board area to plan 10, so this epic seeds into `platform-operations` (prefix `PLAT`).
- **Traps**: an inventory built by hand goes stale — drive the theories from the committed OpenAPI snapshot so the suite breaks when an endpoint is added; asserting on localized message text instead of a status/problem type produces a flaky test; the update-feed row is about SMB ACLs and signature validation under D-002/D-003, not public HTTPS.
- **Concern**: this row carries both an API test set and a packaging test set. Keep the two in one ticket as the plan states, but land them as two commits so the reviewer can read them separately.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the ticket's `plan` document.

## Outcome

_Filled at closeout._
