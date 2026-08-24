# File map — PLAT-003

## Change surface

- `docs/desktop/10-security-observability-performance/README.md` § 8 — record the scanner and its location.
- `docs/desktop/10-security-observability-performance/threat-register.md` — mark the two rows as having an automated test ([[DSK-10-01]]).

## Context files and evidence

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

## Ripple effects and acceptance

- [ ] `eng/packaging/Test-PackageSecrets.ps1` exists, unpacks an MSIX, scans payload plus named extra paths, and exits non-zero on a match.
- [ ] The pattern list is copied from the threat register and names it as the source of truth.
- [ ] A negative test with a planted fake secret proves the scanner fails the build; a clean run proves it passes.
- [ ] The scanner runs as a step of the existing desktop packaging CI job — no new Windows job is added.
- [ ] Reported hits show `path:line:pattern-name` and never the matched value.

## Deliberately out of scope

- **Azure**: no write.
- **Scope boundary**: may create `eng/packaging/*`, edit the desktop packaging job in `.github/workflows/ci.yml`, and add fixtures under the packaging test project. Must not touch `src/Pegasus.Core`, `src/Pegasus.Web`, `infra/`, or the signing route. Board placement note: plan 00 § Kanmer board shape assigns no board area to plan 10, so this epic seeds into `platform-operations` (prefix `PLAT`).
- **Traps**: CI cost under C-01 — private-repository Windows minutes bill at 2×, so reuse the existing packaging job; a scanner that prints matched values turns its own log into the leak; a planted secret left in the tree is worse than the defect it tests for; the pattern list drifting from the threat register defeats both.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the ticket's `plan` document.
