---
id: PLAT-003
type: ticket
title: >-
  DSK-10-03 · Package secret scan: fail the build when the MSIX, desktop config
  or logs carry a secret
status: preparing
area: platform-operations
assignee: ''
profile: feature
stageEntered:
  preparing: '2026-08-24T21:21:13.893Z'
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
created: '2026-08-24T08:05:04.702Z'
updated: '2026-08-24T21:21:13.893Z'
---

## What

Add a CI step that unpacks the built MSIX and scans the package payload, the desktop configuration files and a generated diagnostics bundle for secrets — connection strings, Key Vault URI values, API keys, tokens, private keys — and fails the build on a match. The pattern list is the one maintained in the threat register ([[DSK-10-01]]).

## Why

Proposal §17.1 `:1158-1159` requires that no production database credential and no long-lived Graph, Box, DVLA/DVSA or Azure secret ships in the desktop, and §22.2 lists "secret/log scanning" as a security test `:1617`. The desktop package is built by the CI desktop lane and by `scripts/Build-DesktopRelease.ps1`; nothing today would notice if a developer added a provider key to a desktop `appsettings.json`, and the estate has already shipped one plaintext credential in a server config (`src/Pegasus.Web/appsettings.json:8-14`, see [[DSK-10-02]]). Operator-visible consequence: a signed package distributed to workstations over the D-003 UNC share would carry a secret that cannot be recalled. Siblings: [[DSK-10-08]] (dependency vulnerabilities), [[DSK-10-09]] (log redaction).

## Source of truth

- Plan row: `docs/desktop/10-security-observability-performance/README.md` § 5 — `DSK-10-03`
- Plan detail: same file § 4 (target state: "the package contains no database, Box, Graph, DVLA/DVSA or Azure secret"), § 7 ("Secrets leaking through desktop logs or the diagnostics bundle")
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 17.1 `:1153-1172`; § 22.2 Security tests `:1608-1621`; § 21.2 CI stages `:1451-1470`
- Repository evidence:
  - `.github/workflows/ci.yml:1-70` — workflow name `repository-check`, the `changes` job and how `pwsh` steps are declared; `:71-88` the `documentation` job as the shape to copy for a new Windows job
  - `src/Pegasus.Web/appsettings.json:8-14` — the class of defect being prevented
  - `docs/operations.md:784-802` — how real secrets are held (Key Vault references and Container App secrets), so the scanner knows what a *reference* looks like versus a *value*
  - New desktop package and build script paths are established by `DSK-09-04` (`scripts/Build-DesktopRelease.ps1`) and `DSK-09-05` (the CI desktop lanes) — use whatever those tickets created; do not invent a different path
- Binding decisions:
  - **C-01** (2026-08-23) — the repositories become private; private Windows runner minutes bill at 2×, so this step must be cheap and must not add a second full build.
  - **D-002** / **D-003** — the package is signed in-house and published to a UNC share; a leaked secret cannot be revoked by unpublishing a public release.
- Depends on: `DSK-09-05` — the CI desktop lanes that build the MSIX this step scans.

## Routing

- **Subagent**: `pegasus-release-packager` — `.codex/agents/pegasus-release-packager.toml`
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `winui-packaging` (`.codex/skills/winui-packaging/SKILL.md`; win-dev-skills v0.5.0 `f1028dd5`) for MSIX layout and unpacking → `authoring-github-workflows` (dotnet/skills `98f84851`, `.agents/skills/authoring-github-workflows/SKILL.md`)
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search`, `microsoft_docs_fetch`) for MSIX package layout and `makeappx unpack` semantics
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (leave-backlog needs a governing doc — `docs_todo` is set because ADR-0105/ADR-0109 are not written yet)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Orientation. Read the plan row, the threat register section `## Secret and PII pattern list` created by [[DSK-10-01]], and the CI desktop lane added by `DSK-09-05` in `.github/workflows/ci.yml`. Call Kanmer `get_doc_gates` with this ticket's board id, then `take_ticket`.
2. Branch `task/dsk-10-03-package-secret-scan` from `dev`.
3. Create `eng/packaging/Test-PackageSecrets.ps1` with `[CmdletBinding()] param([Parameter(Mandatory)][string] $PackagePath, [string[]] $AdditionalPath, [switch] $ExpectMatch)`, `Set-StrictMode -Version Latest` and `$ErrorActionPreference = 'Stop'` — the same header shape every script in `scripts/` uses (see `scripts/Test-MigrationGrants.ps1:1-6`).
4. In that script, expand the package: use `makeappx unpack /p <PackagePath> /d <temp>` (search `microsoft_docs_search` for `makeappx unpack` if the switch names need confirming) and fall back to `Expand-Archive` only when `makeappx` is absent. Fail with a named error when neither is available — never silently scan nothing.
5. Define the pattern set as a single ordered hashtable in the script, copied verbatim from the threat register's pattern list: SQL connection strings (`Server=tcp:`, `Initial Catalog=`), `https://[a-z0-9-]+\.vault\.azure\.net/secrets/`, `InstrumentationKey=`, `APPLICATIONINSIGHTS_CONNECTION_STRING`, `AccountKey=`, `client_secret`, `-----BEGIN [A-Z ]*PRIVATE KEY-----`, `Bearer eyJ[A-Za-z0-9_-]+\.`, and the literal `Pegasus-UI-Verify`. Add a comment naming the register as the source of truth so the two never drift.
6. Scan every text-like file in the unpacked payload plus each `-AdditionalPath` (desktop `appsettings*.json`, the `.appinstaller`, and a diagnostics bundle produced by [[DSK-10-09]]). Skip binary payloads by extension, and report each hit as `path:line:pattern-name` without echoing the matched value. Exit 1 on any hit, 0 otherwise.
7. Add the negative test: `eng/packaging/Test-PackageSecrets.Tests.ps1` (or an xunit shim in `tests/Pegasus.Packaging.Tests`, whichever `DSK-08-10` established) that plants a fixture file containing a fake connection string, runs the scanner with `-ExpectMatch`, asserts exit code 1 and the reported pattern name, and then asserts a clean fixture exits 0. The planted secret must be an obvious fake (for example `Server=tcp:example.invalid;Password=NOT-A-REAL-SECRET`) and must live only in the fixture folder.
8. Wire the scanner into `.github/workflows/ci.yml` as a step of the existing desktop packaging job created by `DSK-09-05` — not as a new job — using `shell: pwsh` and `run: ./eng/packaging/Test-PackageSecrets.ps1 -PackagePath <the artifact path that job already produced>`. Adding a second Windows job would double the 2× private-runner cost (C-01).
9. Run the scanner locally against a dev-signed MSIX from `DSK-02-14`: expect exit 0 and a printed count of files scanned. Then re-run with the planted fixture: expect exit 1 and the pattern name in the output.
10. Remove the planted secret from the working tree before the PR and confirm with `git status --porcelain` that only the intended files remain. State in the post-implementation report that the fixture value is fake and was never a live credential.
11. Document the step in `docs/desktop/10-security-observability-performance/README.md` § 8 and record in the threat register that the "sensitive information in logs/temp files" and "leaked service credential" rows now have an automated test.
12. Record `## Simplification pass` with today's date over the branch diff in the ticket's `plan` document, open the PR into `dev`, and hand review to `pegasus-desktop-reviewer`.

## Acceptance criteria

- [ ] `eng/packaging/Test-PackageSecrets.ps1` exists, unpacks an MSIX, scans payload plus named extra paths, and exits non-zero on a match.
- [ ] The pattern list is copied from the threat register and names it as the source of truth.
- [ ] A negative test with a planted fake secret proves the scanner fails the build; a clean run proves it passes.
- [ ] The scanner runs as a step of the existing desktop packaging CI job — no new Windows job is added.
- [ ] Reported hits show `path:line:pattern-name` and never the matched value.

## Verification

- [ ] `pwsh ./eng/packaging/Test-PackageSecrets.ps1 -PackagePath <dev MSIX>` — expected: exit code 0 and a scanned-file count.
- [ ] `pwsh ./eng/packaging/Test-PackageSecrets.ps1 -PackagePath <fixture with planted secret> -ExpectMatch` — expected: exit code 1 and the pattern name printed.
- [ ] CI run of `repository-check` on the PR — expected: the desktop packaging job green with the new step visible in its log.

## Evidence tier

Tier 9 — Security/observability. Here that obliges a demonstrated denial: the evidence is the failing run with a planted secret, not only the passing run on a clean package.

## Documentation changes

- `docs/desktop/10-security-observability-performance/README.md` § 8 — record the scanner and its location.
- `docs/desktop/10-security-observability-performance/threat-register.md` — mark the two rows as having an automated test ([[DSK-10-01]]).

## Guardrails

- **Azure**: no write.
- **Scope boundary**: may create `eng/packaging/*`, edit the desktop packaging job in `.github/workflows/ci.yml`, and add fixtures under the packaging test project. Must not touch `src/Pegasus.Core`, `src/Pegasus.Web`, `infra/`, or the signing route. Board placement note: plan 00 § Kanmer board shape assigns no board area to plan 10, so this epic seeds into `platform-operations` (prefix `PLAT`).
- **Traps**: CI cost under C-01 — private-repository Windows minutes bill at 2×, so reuse the existing packaging job; a scanner that prints matched values turns its own log into the leak; a planted secret left in the tree is worse than the defect it tests for; the pattern list drifting from the threat register defeats both.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the ticket's `plan` document.

## Outcome

_Filled at closeout._
