# Research — PLAT-005

## Question

Give every `/api/v1` command an allow test and a deny test — role bypass, foreign case/document identifiers, and spoofed `X-Pegasus-Client-Version` — and prove that a tampered `.appinstaller` or package fails to install because its signature no longer validates.

## Findings

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

## Implications for this ticket

Proposal §22.2 `:1613-1620` lists role bypass, direct-object access, update-manifest tampering and API version spoofing as security tests, and §22.3 `:1661` makes "every API command has authorization and failure-path tests" a coverage requirement rather than a percentage. The gateway's own boundary already fails closed (`src/Pegasus.Core/Identity/StaffAuthorization.cs`), but the desktop turns each Razor handler into an addressable endpoint, so an endpoint that forgets its `StaffAccessRight` filter is now reachable by anything holding a staff token. Operator-visible consequence: an operator without the right sees or changes a case they must not, or an old client is silently allowed past the minimum-version gate. Siblings: [[DSK-10-04]] (token path), [[DSK-10-06]] (uploads), [[DSK-10-01]] (register).

## Boundaries and assumptions

- **Azure**: no write. Package scenarios run against the local Test/UAT feed (L-02), never against the production UNC share.
- **Scope boundary**: may add tests and packaging scenarios; may add the missing middleware-coverage assertion. Must not change endpoint authorization behaviour — a missing right is a new `fix` ticket. Board placement note: plan 00 § Kanmer board shape assigns no board area to plan 10, so this epic seeds into `platform-operations` (prefix `PLAT`).
- **Traps**: an inventory built by hand goes stale — drive the theories from the committed OpenAPI snapshot so the suite breaks when an endpoint is added; asserting on localized message text instead of a status/problem type produces a flaky test; the update-feed row is about SMB ACLs and signature validation under D-002/D-003, not public HTTPS.
- **Concern**: this row carries both an API test set and a packaging test set. Keep the two in one ticket as the plan states, but land them as two commits so the reviewer can read them separately.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the ticket's `plan` document.

## Research conclusion

The existing ticket evidence identifies the implementation target, routing, and verification. This research does not create or link a planned canonical governing document; `docs_todo` remains accurate until such a document exists.
