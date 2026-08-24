---
id: TEST-013
type: ticket
title: >-
  DSK-08-13 · `ci.yml` lanes: `desktop-build`, `desktop-package`,
  `desktop-ui-smoke`, `packaging-tests`
status: backlog
area: testing
assignee: ''
profile: feature
labels:
  - desktop-conversion
  - plan-08
  - phase-3
  - tier-1
groups:
  - EPIC-009
  - HZN-004
links: []
docs_todo: true
archived: false
created: '2026-08-24T07:53:33.927Z'
updated: '2026-08-24T07:53:33.927Z'
---

## What

Add four lanes to `.github/workflows/ci.yml` — `desktop-build` (build plus view-model and contract tests), `desktop-package` (dev-certificate MSIX artifact), `desktop-ui-smoke` (install, `winapp ui`, axe — on the runner type [[DSK-08-12]] decided) and `packaging-tests` — without disturbing the existing `repository-check` jobs or the Linux publish path.

## Why

Proposal §21.2 lists fifteen CI stages; the repository implements the first seven and has no publish, sign, package or UI stage at all. Until these lanes exist, every desktop test in this epic runs only where someone remembers to run it, and a PR can turn the desktop red without any check going red. The lanes are also what makes the cost question in [[DSK-08-19]] measurable rather than hypothetical. Consumes [[DSK-08-01]], [[DSK-08-04]], [[DSK-08-06]], [[DSK-08-10]] and the runner decision from [[DSK-08-12]]; overlaps deliberately with [[DSK-02-15]] and [[DSK-09-05]] — see Guardrails.

## Source of truth

- Plan row: `docs/desktop/08-testing/README.md` § 5 — `DSK-08-13`
- Plan detail: `docs/desktop/08-testing/README.md` § 1 (§21.2's fifteen stages mapped to jobs), § 4 exit gate items 1–2, and § 7 (CI minutes, checkout timeouts, LocalDB is Windows-only)
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 21.2 CI stages 3–12
- Repository evidence:
  - `.github/workflows/ci.yml` — workflow `repository-check`; jobs `changes` (ubuntu, path flags), `documentation`, `local-development-scripts`, `reference-data`, `infrastructure`, `unit`, `sql-integration` (matrix shard 1–3), `sql-integration-coverage` (ubuntu), `browser`; the `needs: changes` / `if: needs.changes.outputs.build == 'true'` gating pattern every build lane follows
  - `.github/actions/dotnet-build/action.yml` — `actions/setup-dotnet@v6` 10.0.x, NuGet cache keyed on `global.json` and every `packages.lock.json`, locked restore, Release build
  - `scripts/Get-CiChangeFlags.ps1` — the path classifier the `changes` job calls; new desktop paths must be classified there or the lanes never run
  - `.codex/skills/winui-packaging/SKILL.md` — the GitHub Actions packaging sample
- Binding decisions:
  - C-01 — private Windows runner minutes bill at 2×; every added Windows lane has a cost, which is why each must be path-gated and why [[DSK-08-19]] follows this ticket.
  - L-02 — no lane may require an Azure resource.
- Depends on: `DSK-08-12` — the hosted-versus-self-hosted decision the `desktop-ui-smoke` lane is built against. `DSK-02-03` — `Pegasus.Server.slnf`, the solution filter that lets the server lanes build without the desktop projects.

## Routing

- **Subagent**: `pegasus-release-packager` — `.codex/agents/pegasus-release-packager.toml`
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `authoring-github-workflows` (`dotnet/skills`, `.agents/skills/authoring-github-workflows/SKILL.md`) → `winui-packaging` (`.codex/skills/winui-packaging/SKILL.md`, `microsoft/win-dev-skills` v0.5.0 `f1028dd5`) for the packaging steps
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`)
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (call `get_doc_gates <id>` before every move; a move crosses at most one gated boundary)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Read `docs/desktop/08-testing/README.md` § 5 row `DSK-08-13`, § 4 and § 7, the research document of [[DSK-08-12]] (the runner decision), and `.github/workflows/ci.yml` in full. Call `get_doc_gates` on this ticket id, then `take_ticket`, and work in the ticket's own worktree and branch.
2. Check with [[DSK-02-15]] and [[DSK-09-05]] whether either has already added a `desktop-build` or packaging lane. If one exists, extend it rather than adding a second, and record that in the post-implementation report — three plans name overlapping lanes and only one may exist.
3. Load `pegasus-desktop`, then `authoring-github-workflows`. Extend `scripts/Get-CiChangeFlags.ps1` with a `desktop` flag covering `src/Pegasus.Desktop/**`, `src/Pegasus.Desktop.Infrastructure/**`, `src/Pegasus.Contracts/**`, `tests/Pegasus.Desktop.*/**`, `tests/Pegasus.Api.ContractTests/**` and `eng/packaging/**`, and add the matching output to the `changes` job. Update `scripts/Test-CiChangeFlags.ps1` so the classifier's own regression tests cover the new flag — that script is already a CI step.
4. Add job `desktop-build` on `windows-latest`, `needs: changes`, `if: needs.changes.outputs.desktop == 'true'`: checkout, `./.github/actions/dotnet-build`, then `dotnet test` for `tests/Pegasus.Desktop.ViewModelTests` and `tests/Pegasus.Api.ContractTests` chained with `&&` on one line — `pwsh` reports only the last command's exit code, which is why the existing `unit` job chains its two projects.
5. Add job `desktop-package` on `windows-latest`, `needs: desktop-build`: generate a development certificate with `winapp cert generate --if-exists skip --quiet`, package with `winapp package ./bin/x64/Release/ --cert ./devcert.pfx --quiet --self-contained`, and upload the `.msix` with `actions/upload-artifact@v6` and `if-no-files-found: error` (the existing shard job uses exactly that guard).
6. Add job `desktop-ui-smoke`, `needs: desktop-package`, on the runner type the [[DSK-08-12]] research document decided. It downloads the package artifact, trusts the certificate, installs with `Add-AppxPackage`, runs `tests/Pegasus.Desktop.UITests/Invoke-UiSuite.ps1 -IncludeAccessibility`, and uploads the results JSON, screenshots and axe output. Give it `timeout-minutes` no higher than the existing `browser` lane's 25 and `fail-fast: false` semantics if it uses a matrix.
7. Add job `packaging-tests`, `needs: desktop-package`, running `eng/packaging/Test-Package.ps1` against a feed folder created inside the job, and uploading the transcripts. If [[DSK-08-12]] concluded the hosted runner cannot install a package, this job and `desktop-ui-smoke` both move to the self-hosted runner label together — they must not be split across runner types.
8. Verify the Linux path is untouched: the `changes` and `sql-integration-coverage` jobs still run on `ubuntu-latest` and no new job depends on them. Confirm `docs/runbook.md`'s canonical solution commands still describe what CI runs.
9. Use a shallow `actions/checkout` (`fetch-depth: 1`) in every new job — only the history guard in `changes` needs full depth, and the 700 MB repository has been observed timing out in checkout at around five minutes (DELIV-010).
10. Open the PR and confirm on the run: the four new lanes appear, are skipped when no desktop path changed, and are green when one did. Record the wall-clock duration of each new lane in the post-implementation report — [[DSK-08-19]] needs those numbers.
11. Confirm `git diff` for this branch touches only `.github/workflows/ci.yml`, `scripts/Get-CiChangeFlags.ps1`, `scripts/Test-CiChangeFlags.ps1` and documentation; anything else means scope crept.
12. Update `docs/desktop/08-testing/README.md` status line to "lanes green", add the lanes to `docs/operations.md`, run the simplification pass over the branch diff and record it under a dated `## Simplification pass` heading before merging.

## Acceptance criteria

- [ ] Four lanes exist, are path-gated on the new `desktop` change flag, and are green on a PR that touches a desktop path.
- [ ] They are skipped on a documentation-only PR.
- [ ] The existing `repository-check` jobs are unchanged in behaviour and the Linux jobs still run on `ubuntu-latest`.
- [ ] Artifacts (MSIX, UI results, axe output, packaging transcripts) are uploaded with `if-no-files-found: error`.
- [ ] `git diff` is limited to the workflow, the two change-flag scripts and documentation.
- [ ] Each new lane's measured duration is recorded.

## Verification

- [ ] `pwsh ./scripts/Test-CiChangeFlags.ps1` — expected: exit 0 with the new `desktop` flag covered.
- [ ] A PR touching `src/Pegasus.Desktop/**` — expected: all four lanes run and pass; artifacts present on the run.
- [ ] A PR touching only `docs/**` — expected: the four lanes are skipped and `documentation` still runs.
- [ ] `git diff --name-only origin/dev...HEAD` — expected: only `.github/workflows/ci.yml`, `scripts/Get-CiChangeFlags.ps1`, `scripts/Test-CiChangeFlags.ps1` and documentation files.

## Evidence tier

Tier 1 — Static/build/architecture. It obliges a compiling, packaging, artifact-producing pipeline with its own regression test for the change classifier; it proves toolchain consistency, not application behaviour.

## Documentation changes

- `docs/operations.md` — record the four lanes and what each produces.
- `docs/runbook.md` — note which desktop commands CI runs and which stay local.
- `docs/desktop/README.md` status table and `docs/desktop/08-testing/README.md` — mark 08 as "lanes green".

## Guardrails

- **Azure**: no write. No lane authenticates to Azure or reads Azure state.
- **Scope boundary**: may edit `.github/workflows/ci.yml`, `scripts/Get-CiChangeFlags.ps1`, `scripts/Test-CiChangeFlags.ps1` and documentation. Must not change test code, must not add a second workflow file, and must not sign with anything other than a development certificate generated inside the run.
- **Traps**: three plans name an overlapping desktop CI lane ([[DSK-02-15]], [[DSK-09-05]], this ticket) — exactly one lane may exist; reconcile before adding. Every added Windows lane costs 2× once the repositories are private (C-01). `pwsh` reports only the last command's exit code — chain multi-command steps with `&&`. Shallow checkout everywhere except the history guard. LocalDB is Windows-only, so no desktop or integration lane can move to Linux to save minutes.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.

## Outcome

_Filled at closeout._
