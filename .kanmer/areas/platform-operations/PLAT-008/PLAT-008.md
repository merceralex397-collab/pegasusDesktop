---
id: PLAT-008
type: ticket
title: >-
  DSK-10-08 · Dependency and vulnerability scanning in CI, with an SBOM
  published beside each package
status: backlog
area: platform-operations
assignee: ''
profile: chore
labels:
  - desktop-conversion
  - plan-10
  - phase-8
  - tier-1
groups:
  - EPIC-011
  - HZN-009
links: []
docs_todo: true
archived: false
created: '2026-08-24T08:10:26.559Z'
updated: '2026-08-24T08:10:26.559Z'
---

## What

Add `dotnet list package --vulnerable --include-transitive` and NuGet audit to CI so a known-high vulnerability fails the build, publish an SBOM alongside each desktop package artifact, and record the review rule that Windows App SDK major bumps are never taken automatically.

## Why

Proposal §17.1 `:1169` requires dependency and package vulnerability scanning, and §21.2 puts it in the CI stages. The desktop package is self-contained: it carries the .NET runtime and the Windows App SDK into the MSIX, so a vulnerable transitive package ships to every workstation over the D-003 UNC share and can only be fixed by a new signed release. `docs/desktop/02-architecture-and-foundation/README.md` § 7 also records that an unreviewed Windows App SDK bump has previously produced a silent XAML compiler failure, which is why major bumps are reviewed, never automatic. Operator-visible consequence: a workstation runs a package with a known-high CVE and nobody notices until the next manual audit. Siblings: [[DSK-10-03]] (package secret scan), [[DSK-10-01]] (register).

## Source of truth

- Plan row: `docs/desktop/10-security-observability-performance/README.md` § 5 — `DSK-10-08`
- Plan detail: same file § 4 (target state), § 6 (routing: package/CI controls)
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 17.1 `:1153-1172`; § 21.2 CI stages `:1451-1470`; § 22.2 Security tests `:1608-1621`
- Repository evidence:
  - `.github/workflows/ci.yml:11-70` — workflow `repository-check`, the `changes` job and the `shell: pwsh` step style; `:115-130` the `infrastructure` job as a small Windows job to copy
  - `Directory.Build.props` — repository-wide build properties (`TreatWarningsAsErrors`), the file `directory-build-organization` reasons about
  - `skills-lock.json` — the existing lockfile pattern in this repository
  - New: `Directory.Packages.props` and lock files from `DSK-02-02`; the desktop CI lanes from `DSK-09-05`; the SBOM step from `DSK-09-16`/`DSK-08-14`
- Binding decisions:
  - **C-01** (2026-08-23) — private-repository Windows runner minutes bill at 2×; the audit runs on the cheapest lane that already restores, not in a new Windows job.
  - **D-002 / D-003** — a shipped package cannot be unpublished from a UNC share the way a public release can be yanked; prevention is the only control.
- Depends on: `DSK-09-05` (CI desktop lanes), `DSK-09-16` (SBOM and vulnerability report per release).

## Routing

- **Subagent**: `pegasus-release-packager` — `.codex/agents/pegasus-release-packager.toml`
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `directory-build-organization` (dotnet/skills `98f84851`, plugin `dotnet-msbuild`) → `authoring-github-workflows` (dotnet/skills `98f84851`, `.agents/skills/authoring-github-workflows/SKILL.md`)
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search`) for `dotnet list package --vulnerable` exit-code behaviour and NuGet audit (`NuGetAudit`, `NuGetAuditLevel`, `NuGetAuditMode`) properties
- **Kanmer pipeline** for profile `chore`: `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (leave-preparing needs `plan` + `questions-resolved`; enter-done needs `proof`)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Orientation. Read the plan row, `.github/workflows/ci.yml` end to end so the new step lands in an existing job, and `docs/desktop/09-release-update-and-distribution/README.md` § 5 rows `DSK-09-05`/`DSK-09-16`. Call Kanmer `get_doc_gates` with this ticket's board id, then `take_ticket`.
2. Branch `task/dsk-10-08-dependency-scanning` from `dev`.
3. Use `microsoft_docs_search` for `dotnet list package --vulnerable exit code` — the command reports findings on stdout and returns 0, so the step must parse the output. Record what the documentation says in the plan document before writing the parser.
4. Create `scripts/Test-DependencyVulnerabilities.ps1` with the repository's script header (`[CmdletBinding()] param([ValidateSet('Low','Moderate','High','Critical')][string] $FailAt = 'High', [string] $ProjectOrSolution = './Pegasus.slnx')`, `Set-StrictMode -Version Latest`, `$ErrorActionPreference = 'Stop'`). It runs `dotnet list $ProjectOrSolution package --vulnerable --include-transitive --format json`, parses the result, prints one line per finding as `package@version severity advisory-url`, and exits 1 when any finding is at or above `-FailAt`.
5. Add a triage file `docs/desktop/10-security-observability-performance/dependency-audit.md` holding accepted findings: package, version, advisory, why it is accepted, review date, and the ticket that will remove it. The script reads it and treats a listed advisory as non-fatal until its review date passes, then fails. Keep the file under `docs/desktop/` — anywhere else fails the CI `documentation` job.
6. Enable NuGet audit repository-wide in `Directory.Build.props`: `NuGetAudit=true`, `NuGetAuditMode=all`, `NuGetAuditLevel=high`. Restore once and confirm no existing project breaks under `TreatWarningsAsErrors`; if it does, add the narrowest `NoWarn` with a comment naming the advisory and add the row to the triage file rather than lowering the level.
7. Wire the script into `.github/workflows/ci.yml` as a step of an existing job that already restores — add it to the `unit` job (`:131-148`) or to the desktop lane from `DSK-09-05`, whichever restores the full solution — with `shell: pwsh` and `run: ./scripts/Test-DependencyVulnerabilities.ps1`. Do **not** create a new Windows job (C-01).
8. SBOM: confirm what `DSK-09-16`/`DSK-08-14` already produce. If an SBOM step exists, add only the assertion that the SBOM file is attached to the same artifact as the MSIX and is named for the package version. If it does not exist yet, note the dependency in the ticket and do not duplicate the step here.
9. Record the update-review rule in `docs/engineering.md` § Branches and delivery (or the nearest existing dependency section): dependency update PRs are reviewed like any other change, and a **major** Windows App SDK or .NET version bump is never merged automatically — it needs a build-and-launch check on a clean Windows 11 machine because an unreviewed bump has previously produced an `MSB3073` failure with no XAML diagnostic.
10. Prove the gate: temporarily add a package reference with a known-high advisory (find one via `microsoft_docs_search` or the NuGet advisory feed), run `pwsh ./scripts/Test-DependencyVulnerabilities.ps1` and expect exit 1 with the finding printed; remove the reference and expect exit 0. Capture both runs as the proof.
11. Update the threat register row "leaked service credential"/"administrator error" adjacency: add a dependency-vulnerability row naming this script and the triage file ([[DSK-10-01]]).
12. Record `## Simplification pass` with today's date over the branch diff in the ticket's `plan` document, open the PR into `dev`, and hand review to `pegasus-desktop-reviewer`.

## Acceptance criteria

- [ ] `scripts/Test-DependencyVulnerabilities.ps1` exists, parses `dotnet list package --vulnerable --include-transitive` output and exits non-zero at or above the configured severity.
- [ ] NuGet audit is enabled repository-wide in `Directory.Build.props` at `high`, with any exception narrowly scoped and recorded.
- [ ] The triage file records every accepted finding with a review date and an owning ticket.
- [ ] The check runs inside an existing CI job — no new Windows job is added.
- [ ] An SBOM is attached to the same artifact as each desktop package (verified, or the dependency on `DSK-09-16` is recorded).
- [ ] `docs/engineering.md` records that major Windows App SDK / .NET bumps are never automatic.

## Verification

- [ ] `pwsh ./scripts/Test-DependencyVulnerabilities.ps1` on a clean tree — expected: exit 0 and "no vulnerable packages" in the output.
- [ ] `pwsh ./scripts/Test-DependencyVulnerabilities.ps1` with the planted vulnerable reference — expected: exit 1 and the advisory line printed.
- [ ] CI run of `repository-check` on the PR — expected: green, with the new step visible in the chosen job's log.

## Evidence tier

Tier 1 — Static/build/architecture. Here that obliges a build-time gate proved in both directions (clean pass, planted failure); it proves consistency only, so the tier-9 evidence for what actually ships stays with [[DSK-10-03]] and the release record.

## Documentation changes

- `docs/desktop/10-security-observability-performance/dependency-audit.md` — new triage file.
- `docs/engineering.md` — the dependency-update review rule and the no-automatic-major-bump rule.
- `docs/desktop/10-security-observability-performance/threat-register.md` — add the dependency row.

## Guardrails

- **Azure**: no write.
- **Scope boundary**: may touch `scripts/`, `Directory.Build.props`, `.github/workflows/ci.yml`, `docs/engineering.md` and `docs/desktop/`. Must not change package versions to make the audit pass — an upgrade is its own ticket with its own review. Board placement note: plan 00 § Kanmer board shape assigns no board area to plan 10, so this epic seeds into `platform-operations` (prefix `PLAT`).
- **Traps**: `dotnet list package --vulnerable` returns 0 even when it reports findings — a step that trusts the exit code is a no-op gate; CI cost under C-01 forbids a new Windows job; `TreatWarningsAsErrors` turns a new audit warning into a build break across the whole repository, so enable it in one commit and fix the fallout in the same PR; an unreviewed Windows App SDK major bump has previously failed the XAML compiler with no diagnostic.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the ticket's `plan` document.

## Outcome

_Filled at closeout._
