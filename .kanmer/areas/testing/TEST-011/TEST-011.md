---
id: TEST-011
type: ticket
title: >-
  DSK-08-11 · Security test set: token lifecycle, disabled account, role bypass,
  direct-object access, malformed uploads, unsafe paths, manifest tampering,
  version spoofing, temp-file ACLs, secret and log scan
status: backlog
area: testing
assignee: ''
profile: feature
labels:
  - desktop-conversion
  - plan-08
  - phase-8
  - tier-9
  - needs-operator
groups:
  - EPIC-009
  - HZN-009
links: []
docs_todo: true
archived: false
created: '2026-08-24T07:51:10.298Z'
updated: '2026-08-24T07:51:10.298Z'
---

## What

Build the desktop-era security test set: xunit cases for the token, authorization and upload paths, and scripted checks for the ones a test cannot express — update-manifest tampering, client-version spoofing, desktop temporary-file permissions, and a secret scan over the package, the desktop configuration and the logs.

## Why

Proposal §22.2 ("Security tests") lists twelve items and §24 Phase 8 makes "no unresolved high-risk security item" an exit gate. The conversion changes the threat surface in ways the web app never had: a long-lived refresh token now sits on a workstation, an update manifest on a file share decides what code runs, and a client-supplied version header gates access. Each of those needs a test that fails first. Consumes the command coverage table of [[DSK-08-02]]; coordinates with [[DSK-10-04]]/[[DSK-10-05]]/[[DSK-10-03]], which own the controls this ticket proves, and with [[DSK-04-14]], which covers the token path from the auth side.

## Source of truth

- Plan row: `docs/desktop/08-testing/README.md` § 5 — `DSK-08-11`
- Plan detail: `docs/desktop/08-testing/README.md` § 4 (target state row "Security") and § 7 (`Category` traits)
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 22.2 "Security tests", § 17.1 required controls, § 17.3 threat model focus
- Repository evidence:
  - `tests/Pegasus.Api.ContractTests/CommandCoverage/**` — the endpoint catalogue and table from [[DSK-08-02]] that this ticket reuses rather than re-enumerating
  - `tests/Pegasus.IntegrationTests/IntakeWebTestSupport.cs:26` — the factory for the persisted-effect assertions
  - `docs/engineering.md` § Required evidence tiers, tier 9 — role matrix, secure cookies, transient authentication throttling, request forgery, denial before client construction, dependency and dynamic scanning, correlation, health, redaction, bounded failure metrics
  - `docs/desktop/09-release-update-and-distribution/appinstaller-template.md` — the manifest fields a tampering check must reject
- Binding decisions:
  - D-002 — packages are signed with the self-managed certificate; a tampered manifest or package must fail the signature check, and the private key never appears in CI.
  - L-02 — all of this runs locally; no Azure resource is probed.
- Depends on: `DSK-08-02` — the command coverage table. `DSK-04-02` and `DSK-04-04` — the OpenIddict public client and the `/api/v1` bearer authentication whose lifecycle these tests exercise.

## Routing

- **Subagent**: `pegasus-test-engineer` — `.codex/agents/pegasus-test-engineer.toml`
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `code-testing-agent` (`dotnet/skills` `98f84851`, plugin `dotnet-test`)
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search`) for DPAPI and file-ACL semantics
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (call `get_doc_gates <id>` before every move; a move crosses at most one gated boundary)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Read `docs/desktop/08-testing/README.md` § 5 row `DSK-08-11`, `docs/desktop/10-security-observability-performance/README.md` § 5 rows `DSK-10-03` to `DSK-10-07`, and `docs/desktop/04-auth-session-update-and-startup/README.md` § 5 row `DSK-04-14`. Agree the split with those tickets before writing anything, and record it in the ticket research document: this ticket owns the *tests and scripted checks*, those own the controls.
2. Call `get_doc_gates` on this ticket id, then `take_ticket`, and work in the ticket's own worktree and branch. Load `pegasus-desktop`, then `code-testing-agent`.
3. Add `tests/Pegasus.Api.ContractTests/Security/TokenLifecycleTests.cs` — `[Trait("Category", "Security")]`: access-token expiry rejected, refresh rotation issues a new refresh token and invalidates the old one, a replayed old refresh token is rejected, logout revokes, and a token issued before a password change is rejected.
4. Add `Security/DisabledAccountTests.cs`: an account disabled after a token was issued is rejected on the next `/api/v1` call — assert the per-request `IsEnabled`/security-stamp check, not only the login path.
5. Add `Security/RoleBypassTests.cs` and `Security/DirectObjectAccessTests.cs` driven from the [[DSK-08-02]] command table: for every command, a token without the required right is refused, and a valid token cannot reach another operator's case, document or upload session by identifier. Assert no side effect occurred.
6. Add `Security/MalformedUploadTests.cs` and `Security/UnsafePathTests.cs` on the upload-session endpoints: over-size single file, over-size multipart envelope, mismatched declared content type, path traversal in the supplied file name, and a name that normalises to a reserved Windows device name. Take the limits from the existing `IntakeEnvelopeLimits` rather than restating numbers.
7. Add `Security/ClientVersionSpoofTests.cs`: an `X-Pegasus-Client-Version` header below the minimum is refused; a malformed or absent header is refused rather than defaulted; a header above the current release does not bypass the gate.
8. Add `eng/security/Test-PackageSecrets.ps1`: unpack the built MSIX, and scan it plus the desktop configuration files and a captured log directory for connection strings, Key Vault URI values, API keys and bearer tokens using a pattern list kept in one file beside the script. Exit non-zero on any hit, printing the file and the matched pattern name (never the matched secret).
9. Add `eng/security/Test-ManifestTampering.ps1`: take a signed package and its `.appinstaller`, modify one manifest field, and assert the install is refused; then assert the validator from [[DSK-09-03]] also rejects the tampered `.appinstaller`.
10. Add `eng/security/Test-TempFileAcl.ps1`: after a desktop session, enumerate the desktop's temporary, cache and credential-store paths and assert each grants access only to the current user — no `Everyone`, no `Users`, no inherited broad grant. Use `microsoft_docs_search` for the current DPAPI `CurrentUser` scope semantics before asserting what "bound to the user" means.
11. Run `dotnet test ./tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj --configuration Release --no-build --filter "Category=Security"` and each script. Done when every check has been seen to fail against a deliberately weakened input and then pass against the real one — record both runs.
12. Record the `Security` trait in `docs/operations.md` § Evidence profiles and the scripts in `docs/runbook.md`, then run the simplification pass over the branch diff before opening the PR.

## Acceptance criteria

- [ ] Every item in proposal §22.2 "Security tests" that applies to the desktop has a failing-then-passing test or scripted check.
- [ ] Role-bypass and direct-object tests are generated from the command table, so a new command is covered automatically.
- [ ] The secret scan covers the package, the desktop configuration and the logs, and never prints a matched secret.
- [ ] Manifest tampering is refused by both the installer and the `.appinstaller` validator.
- [ ] Temporary, cache and credential-store paths grant access to the current user only.

## Verification

- [ ] `dotnet test ./tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj --configuration Release --no-build --filter "Category=Security"` — expected: `Passed!` with a non-zero total.
- [ ] `pwsh ./eng/security/Test-PackageSecrets.ps1 -PackagePath <msix>` — expected: exit 0 and a printed count of scanned files; planting a dummy connection string makes it exit 1 naming the file.
- [ ] `pwsh ./eng/security/Test-ManifestTampering.ps1` — expected: exit 0 with both refusals observed.
- [ ] `pwsh ./eng/security/Test-TempFileAcl.ps1` — expected: exit 0, per-path ACL summary showing the current user only.

## Evidence tier

Tier 9 — Security/observability. It obliges the role matrix, transient authentication throttling, denial before client construction, redaction and dependency scanning to be observed through the real caller; App Insights, Container App probes and Key Vault behaviour stay pilot-ring checks under L-02.

## Documentation changes

- `docs/operations.md` § Evidence profiles — register the `Security` trait for the desktop era.
- `docs/runbook.md` — add the three `eng/security/` scripts.
- `docs/desktop/08-testing/README.md` § 4 — mark the security row as covered.

## Guardrails

- **Azure**: no write, and no read of a production secret. The scan never prints a matched value.
- **Scope boundary**: may create `tests/Pegasus.Api.ContractTests/Security/**` and `eng/security/**`. Must not change authentication or authorization code — a failing expectation is a finding for `pegasus-gateway-dev` and a ticket in area 10, never a relaxed test.
- **Traps**: the desktop security controls are owned by area 10 ([[DSK-10-03]]–[[DSK-10-07]]) and the token flow by [[DSK-04-14]] — agree the split first or two tickets will write the same tests. `TreatWarningsAsErrors=true` applies. Never fabricate domain data, and never commit a real or realistic secret as a scan fixture — use an obviously synthetic pattern.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.

## Outcome

_Filled at closeout._
