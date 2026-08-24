---
id: PLAT-008
type: ticket
title: >-
  DSK-10-08 · Dependency and vulnerability scanning in CI, with an SBOM
  published beside each package
status: preparing
area: platform-operations
assignee: ''
profile: chore
stageEntered:
  preparing: '2026-08-24T21:21:14.608Z'
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
updated: '2026-08-24T21:21:14.608Z'
---

## What

Extend [[DSK-09-16]]'s desktop dependency-vulnerability gate across the whole repository at restore time — NuGet audit enabled in `Directory.Build.props` so a known-high advisory fails the build — assert that the SBOM [[DSK-09-16]] produces is published beside each desktop package artifact, and record the review rule that Windows App SDK major bumps are never taken automatically. The gate itself, its tool contract and its suppression register stay [[DSK-09-16]]'s.

## Why

Proposal §17.1 `:1169` requires dependency and package vulnerability scanning, and §21.2 puts it in the CI stages. The desktop package is self-contained: it carries the .NET runtime and the Windows App SDK into the MSIX, so a vulnerable transitive package ships to every workstation over the D-003 UNC share and can only be fixed by a new signed release. `docs/desktop/02-architecture-and-foundation/README.md` § 7 also records that an unreviewed Windows App SDK bump has previously produced a silent XAML compiler failure, which is why major bumps are reviewed, never automatic. Operator-visible consequence: a workstation runs a package with a known-high CVE and nobody notices until the next manual audit. Three plans name these controls — [[DSK-08-14]], [[DSK-09-16]] and this one — so ownership is pinned rather than negotiated at execution time: [[DSK-09-16]] owns the SBOM artefact, the vulnerability gate, its tool contract and its suppression register; [[DSK-08-14]] owns the `dependency-scan` job that runs them; this ticket consumes and extends them, adding the restore-time audit and the review rule and no second copy of anything. Siblings: [[DSK-10-03]] (package secret scan), [[DSK-10-01]] (register).

## Source of truth

- Plan row: `docs/desktop/10-security-observability-performance/README.md` § 5 — `DSK-10-08`
- Plan detail: same file § 4 (target state), § 6 (routing: package/CI controls)
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 17.1 `:1153-1172`; § 21.2 CI stages `:1451-1470`; § 22.2 Security tests `:1608-1621`
- Repository evidence:
  - `.github/workflows/ci.yml:11-70` — workflow `repository-check`, the `changes` job and the `shell: pwsh` step style; `:115-130` the `infrastructure` job as a small Windows job to copy
  - `Directory.Build.props` — repository-wide build properties (`TreatWarningsAsErrors`), the file `directory-build-organization` reasons about
  - `skills-lock.json` — the existing lockfile pattern in this repository
  - New: `Directory.Packages.props` and lock files from `DSK-02-02`; the desktop CI lanes from `DSK-09-05`; the SBOM artefact, the vulnerability gate, its tool manifest and its triage record from `DSK-09-16`; the `dependency-scan` job from `DSK-08-14`
- Binding decisions:
  - **C-01** (2026-08-23) — private-repository Windows runner minutes bill at 2×; the audit runs on the cheapest lane that already restores, not in a new Windows job.
  - **D-002 / D-003** — a shipped package cannot be unpublished from a UNC share the way a public release can be yanked; prevention is the only control.
- Depends on: `DSK-09-16` — owns the SBOM artefact, the vulnerability gate, its tool contract and its suppression register; this ticket gates on them and adds no second scanner, generator or register. `DSK-08-14` — owns the `dependency-scan` job those controls may run in; this ticket adds no job. `DSK-09-05` — the CI desktop lanes.

## Routing

- **Subagent**: `pegasus-release-packager` — `.codex/agents/pegasus-release-packager.toml`
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `directory-build-organization` (dotnet/skills `98f84851`, plugin `dotnet-msbuild`) → `authoring-github-workflows` (dotnet/skills `98f84851`, `.agents/skills/authoring-github-workflows/SKILL.md`)
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search`) for `dotnet list package --vulnerable` exit-code behaviour and NuGet audit (`NuGetAudit`, `NuGetAuditLevel`, `NuGetAuditMode`) properties
- **Kanmer pipeline** for profile `chore`: `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (leave-preparing needs `plan` + `questions-resolved`; enter-done needs `proof`)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Orientation. Read the plan row, `.github/workflows/ci.yml` end to end so the new properties land beside an existing job, `docs/desktop/09-release-update-and-distribution/README.md` § 5 rows `DSK-09-05` and `DSK-09-16`, and `docs/desktop/08-testing/README.md` § 5 row `DSK-08-14`. Call Kanmer `get_doc_gates` with this ticket's board id, then `take_ticket`.
2. Branch `task/dsk-10-08-dependency-scanning` from `dev`.
3. **Check first.** Check whether the desktop dependency-vulnerability gate, its tool contract and its triage record already exist from [[DSK-09-16]], which owns the SBOM artefact, the vulnerability gate, its tool contract and its suppression register. If they do, extend them in place — this ticket adds only the repository-wide restore-time NuGet audit (step 5) and the placement assertion (step 6) — and change no rule inside the gate. If [[DSK-09-16]] has not landed, create them with exactly the shape its steps 2, 3 and 5 pin: the SBOM generator chosen by a recorded comparison of `Microsoft.Sbom.DotNetTool` (SPDX output) against `CycloneDX.DotNet` and pinned to an exact version in a `dotnet-tools.json` manifest restored with `dotnet tool restore`; `dotnet list ./src/Pegasus.Desktop/Pegasus.Desktop.csproj package --vulnerable --include-transitive` written to `vulnerability-report.txt`, with the `-FailAt` severity parameter [[DSK-09-16]] pins deciding what fails — `[ValidateSet('Low','Moderate','High','Critical')]`, defaulting to `High`, so any row at or above `-FailAt` fails the build while `Moderate` and `Low` are reported without failing; and one explicit `-AcceptVulnerabilities <reason>` escape that records the triage text into `desktop-release-manifest.json` rather than silently continuing. Record in the plan document which case applied. Never a second scanner, never a second SBOM generator and never a second suppression register.
4. Use `microsoft_docs_search` for `dotnet list package --vulnerable exit code` and for the `NuGetAudit`, `NuGetAuditMode` and `NuGetAuditLevel` properties. `dotnet list package --vulnerable` reports findings on stdout and returns 0, so any parser written under step 3's second case must read the output rather than trust the exit code. Record what the documentation says in the plan document before writing anything.
5. Enable NuGet audit repository-wide in `Directory.Build.props`: `NuGetAudit=true`, `NuGetAuditMode=all`, `NuGetAuditLevel=high`. This is the restore-time half of the control and it adds no scanner script, no report file and no register of its own. Restore once and confirm no existing project breaks under `TreatWarningsAsErrors`; if one does, add the narrowest `NoWarn` with a comment naming the advisory and record the accepted finding in [[DSK-09-16]]'s triage record — do not lower the level and do not open a second triage file.
6. Assert placement rather than adding a lane: confirm the gate runs in a CI job that already restores — the job [[DSK-09-16]]'s CI-wiring step routes it to, the step headed **Wire CI without adding a second step of anything**, which is step 8 of [[DSK-09-16]] as that ticket stands on 2026-08-24. Cite that step by its heading rather than by its number: the number has already moved once and this pointer went stale when it did. That step sends the vulnerability gate to the `dependency-scan` job [[DSK-08-14]] owns on `ubuntu-latest` and deliberately adds no vulnerability step to the `desktop-package` lane, so the job to look for is that one whichever of the two tickets landed first. Record in the plan document which job carried it. Do **not** create a new Windows job (C-01), a second lane, or a second invocation of the same scan.
7. SBOM: assert only that the SBOM [[DSK-09-16]] produces is attached to the same artifact as the `.msix` and is named for the package version. Add no generator and no second SBOM step here: [[DSK-09-16]]'s **Decide and record the generator** step owns the generator choice, and its separate step that pins the chosen tool to an exact version in a `dotnet-tools.json` manifest restored with `dotnet tool restore` owns the tool-manifest pin — two different steps of that ticket, cited by their content because its step numbers move.
8. Record the update-review rule in `docs/engineering.md` § Branches and delivery (or the nearest existing dependency section): dependency update PRs are reviewed like any other change, and a **major** Windows App SDK or .NET version bump is never merged automatically — it needs a build-and-launch check on a clean Windows 11 machine because an unreviewed bump has previously produced an `MSB3073` failure with no XAML diagnostic.
9. Prove the restore-time gate in both directions: temporarily add a package reference with a known-high advisory (find one via `microsoft_docs_search` or the NuGet advisory feed), run `dotnet restore ./Pegasus.slnx --locked-mode` and expect the build to fail with `NU1903`/`NU1904` naming the package; remove the reference and expect a clean restore. Capture both runs as the proof, together with one run of [[DSK-09-16]]'s gate reporting the same package.
10. Update the threat register ([[DSK-10-01]]): add one dependency-vulnerability row naming [[DSK-09-16]]'s gate and triage record as the control, with this ticket's repository-wide NuGet audit as its restore-time extension — one row, one control, not two.
11. Record `## Simplification pass` with today's date over the branch diff in the ticket's `plan` document, open the PR into `dev`, and hand review to `pegasus-desktop-reviewer`.

## Acceptance criteria

- [ ] Exactly one desktop vulnerability gate, one SBOM generator and one suppression register exist across [[DSK-08-14]], [[DSK-09-16]] and this ticket — [[DSK-09-16]] owns all three — and the plan document records whether that ticket had landed or this one supplied them to its pinned shape.
- [ ] NuGet audit is enabled repository-wide in `Directory.Build.props` at `high`, with any exception narrowly scoped and its accepted finding recorded in [[DSK-09-16]]'s triage record.
- [ ] No second triage or suppression register is created anywhere under `docs/desktop/10-security-observability-performance/`.
- [ ] The check runs inside an existing CI job — no new Windows job and no second lane is added — and the lane that carried it is recorded.
- [ ] The SBOM [[DSK-09-16]] produces is attached to the same artifact as each desktop package and named for the package version; this ticket adds no generator.
- [ ] `docs/engineering.md` records that major Windows App SDK / .NET bumps are never automatic.

## Verification

- [ ] `dotnet restore ./Pegasus.slnx --locked-mode` on a clean tree — expected: exit 0 with no `NU1901`–`NU1904` warning at or above `high`.
- [ ] `dotnet restore ./Pegasus.slnx --locked-mode` with the planted vulnerable reference — expected: the build fails with `NU1903` or `NU1904` naming the package and its advisory.
- [ ] `rg -n -- "--vulnerable" .github/workflows/ci.yml scripts/` — expected: exactly one invocation site, the gate [[DSK-09-16]] owns; no second scanner script exists.
- [ ] CI run of `repository-check` on the PR — expected: green, with the audit visible in the chosen job's log.

## Evidence tier

Tier 1 — Static/build/architecture. Here that obliges a build-time gate proved in both directions (clean pass, planted failure); it proves consistency only, so the tier-9 evidence for what actually ships stays with [[DSK-10-03]] and the release record.

## Documentation changes

- `docs/desktop/10-security-observability-performance/README.md` § 5 row `DSK-10-08` — record that the SBOM artefact, the vulnerability gate, its tool contract and its suppression register belong to `DSK-09-16`, and that this row delivers the restore-time NuGet audit, the placement assertion and the update-review rule.
- `docs/engineering.md` — the dependency-update review rule and the no-automatic-major-bump rule.
- `docs/desktop/10-security-observability-performance/threat-register.md` — add the dependency row, naming `DSK-09-16`'s gate and triage record as the control.

## Guardrails

- **Azure**: no write.
- **Scope boundary**: may touch `Directory.Build.props`, `docs/engineering.md` and `docs/desktop/10-security-observability-performance/`. It does **not** own the SBOM artefact, the vulnerability gate, its tool manifest or its suppression register — [[DSK-09-16]] owns all four, and step 3 supplies them only if that ticket has not landed, to exactly the shape it pins. Must not create a second scanner script, a second SBOM generator, a second triage or suppression register, a new lane or job in `.github/workflows/ci.yml`, or a new Windows job. Must not change package versions to make the audit pass — an upgrade is its own ticket with its own review. Board placement note: plan 00 § Kanmer board shape assigns no board area to plan 10, so this epic seeds into `platform-operations` (prefix `PLAT`).
- **Traps**: one dependency-scan and SBOM contract — three plans name the desktop dependency-scan and SBOM controls ([[DSK-08-14]], [[DSK-09-16]], [[DSK-10-08]]), and exactly one SBOM generator, one vulnerability gate and one suppression register may exist: [[DSK-09-16]] owns the SBOM, the gate, its tool contract and its triage record, [[DSK-08-14]] owns the `dependency-scan` job that runs them, and this ticket adds the restore-time NuGet audit only — a second copy of any of the three is a stop condition. Cite [[DSK-09-16]]'s steps by their content, not their numbers: its CI-wiring step moved from 7 to 8 during the amendment pass and left this ticket pointing at the transitive-flag note. `dotnet list package --vulnerable` returns 0 even when it reports findings — a step that trusts the exit code is a no-op gate; CI cost under C-01 forbids a new Windows job; `TreatWarningsAsErrors` turns a new audit warning into a build break across the whole repository, so enable it in one commit and fix the fallout in the same PR; an unreviewed Windows App SDK major bump has previously failed the XAML compiler with no diagnostic.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the ticket's `plan` document.

## Outcome

_Filled at closeout._
