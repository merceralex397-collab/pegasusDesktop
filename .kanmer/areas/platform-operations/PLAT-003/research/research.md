# Research — PLAT-003

## Question

Add a CI step that unpacks the built MSIX and scans the package payload, the desktop configuration files and a generated diagnostics bundle for secrets — connection strings, Key Vault URI values, API keys, tokens, private keys — and fails the build on a match. The pattern list is the one maintained in the threat register ([[DSK-10-01]]).

## Findings

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

## Implications for this ticket

Proposal §17.1 `:1158-1159` requires that no production database credential and no long-lived Graph, Box, DVLA/DVSA or Azure secret ships in the desktop, and §22.2 lists "secret/log scanning" as a security test `:1617`. The desktop package is built by the CI desktop lane and by `scripts/Build-DesktopRelease.ps1`; nothing today would notice if a developer added a provider key to a desktop `appsettings.json`, and the estate has already shipped one plaintext credential in a server config (`src/Pegasus.Web/appsettings.json:8-14`, see [[DSK-10-02]]). Operator-visible consequence: a signed package distributed to workstations over the D-003 UNC share would carry a secret that cannot be recalled. Siblings: [[DSK-10-08]] (dependency vulnerabilities), [[DSK-10-09]] (log redaction).

## Boundaries and assumptions

- **Azure**: no write.
- **Scope boundary**: may create `eng/packaging/*`, edit the desktop packaging job in `.github/workflows/ci.yml`, and add fixtures under the packaging test project. Must not touch `src/Pegasus.Core`, `src/Pegasus.Web`, `infra/`, or the signing route. Board placement note: plan 00 § Kanmer board shape assigns no board area to plan 10, so this epic seeds into `platform-operations` (prefix `PLAT`).
- **Traps**: CI cost under C-01 — private-repository Windows minutes bill at 2×, so reuse the existing packaging job; a scanner that prints matched values turns its own log into the leak; a planted secret left in the tree is worse than the defect it tests for; the pattern list drifting from the threat register defeats both.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the ticket's `plan` document.

## Research conclusion

The existing ticket evidence identifies the implementation target, routing, and verification. This research does not create or link a planned canonical governing document; `docs_todo` remains accurate until such a document exists.
