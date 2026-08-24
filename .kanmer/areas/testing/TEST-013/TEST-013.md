---
id: TEST-013
type: ticket
title: >-
  DSK-08-13 · `ci.yml` lanes: `desktop-build`, `desktop-package`,
  `desktop-ui-smoke`, `packaging-tests`
status: preparing
area: testing
assignee: ''
profile: feature
stageEntered:
  preparing: '2026-08-24T21:34:15.384Z'
labels:
  - desktop-conversion
  - plan-08
  - phase-3
  - tier-1
groups:
  - EPIC-009
  - HZN-004
links: []
blocks:
  - DUI-015
  - TEST-014
  - TEST-019
docs_todo: true
archived: false
created: '2026-08-24T07:53:33.927Z'
updated: '2026-08-24T21:34:15.384Z'
---

## What

Add four lanes to `.github/workflows/ci.yml` — `desktop-build` (build plus view-model and contract tests), `desktop-package` (dev-certificate MSIX artifact), `desktop-ui-smoke` (install, `winapp ui`, axe — on the runner type [[DSK-08-12]] decided) and `packaging-tests` — without disturbing the existing `repository-check` jobs or the Linux publish path, and fix the checkout the existing `changes` and `documentation` jobs time out on (upstream DELIV-010).

## Why

Proposal §21.2 lists fifteen CI stages; the repository implements the first seven and has no publish, sign, package or UI stage at all. Until these lanes exist, every desktop test in this epic runs only where someone remembers to run it, and a PR can turn the desktop red without any check going red. The lanes are also what makes the cost question in [[DSK-08-19]] measurable rather than hypothetical. Consumes [[DSK-08-01]], [[DSK-08-04]], [[DSK-08-06]], [[DSK-08-10]] and the runner decision from [[DSK-08-12]]; overlaps deliberately with [[DSK-02-15]] and [[DSK-09-05]] — see Guardrails.

## Source of truth

- Plan row: `docs/desktop/08-testing/README.md` § 5 — `DSK-08-13`
- Plan detail: `docs/desktop/08-testing/README.md` § 1 (§21.2's fifteen stages mapped to jobs), § 4 exit gate items 1–2, and § 7 (CI minutes, checkout timeouts, LocalDB is Windows-only)
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 21.2 CI stages 3–12
- Repository evidence:
  - `.github/workflows/ci.yml` — workflow `repository-check`; jobs `changes` (ubuntu, path flags), `documentation`, `local-development-scripts`, `reference-data`, `infrastructure`, `unit`, `sql-integration` (matrix shard 1–3), `sql-integration-coverage` (ubuntu), `browser`; the `needs: changes` / `if: needs.changes.outputs.build == 'true'` gating pattern every build lane follows
  - `.github/workflows/ci.yml` — `changes` is `ubuntu-latest`, `timeout-minutes: 5`, `actions/checkout@v7` with `fetch-depth: 0`; `documentation` is `windows-latest`, `timeout-minutes: 10`, `actions/checkout@v7` with `fetch-depth: 0` — exactly the two jobs upstream DELIV-010 recorded being cancelled mid-fetch
  - `.github/actions/dotnet-build/action.yml` — `actions/setup-dotnet@v6` 10.0.x, NuGet cache keyed on `global.json` and every `packages.lock.json`, locked restore, Release build
  - `scripts/Get-CiChangeFlags.ps1` — the path classifier the `changes` job calls; new desktop paths must be classified there or the lanes never run. Line 11 is `$buildPattern` and it already matches `^(src|tests)/`; there is no `$desktopPattern` and the script emits only `Build` and `Infrastructure` today
  - `.codex/skills/winui-packaging/SKILL.md` — the GitHub Actions packaging sample
- Binding decisions:
  - C-01 — private Windows runner minutes bill at 2×; every added Windows lane has a cost, which is why each must be path-gated and why [[DSK-08-19]] follows this ticket. A flaky checkout that forces a re-run of the Windows `documentation` lane is billed twice.
  - L-02 — no lane may require an Azure resource.
- Depends on: `DSK-08-12` — the hosted-versus-self-hosted decision the `desktop-ui-smoke` lane is built against. `DSK-02-03` — `Pegasus.Server.slnf`, the solution filter that lets the server lanes build without the desktop projects. `DSK-09-05` — owns the single `desktop-package` job and pins the artifact literal `desktop-msix-unsigned`; this ticket extends that job and downloads that name. `DSK-02-15` — owns the `desktop-build` job; this ticket extends it and never redefines it.

## Routing

- **Subagent**: `pegasus-release-packager` — `.codex/agents/pegasus-release-packager.toml`
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `authoring-github-workflows` (`dotnet/skills`, `.agents/skills/authoring-github-workflows/SKILL.md`) → `winui-packaging` (`.codex/skills/winui-packaging/SKILL.md`, `microsoft/win-dev-skills` v0.5.0 `f1028dd5`) for the packaging steps
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`)
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (call `get_doc_gates <id>` before every move; a move crosses at most one gated boundary)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Read `docs/desktop/08-testing/README.md` § 5 row `DSK-08-13`, § 4 and § 7, the research document of [[DSK-08-12]] (the runner decision), and `.github/workflows/ci.yml` in full. Call `get_doc_gates` on this ticket id, then `take_ticket`, and work in the ticket's own worktree and branch.
2. `desktop-package` has exactly one owner: [[DSK-09-05]] adds the single `desktop-package` job, and this ticket extends that job and never adds a second. `desktop-build` is owned by [[DSK-02-15]], which proves build and tests only and packages nothing. Check whether either job already exists from its owner. If it does, extend it in place and change no existing step; if it has not landed, create it with exactly the shape its owner pins — [[DSK-09-05]] step 4 for the `desktop-package` job shape and [[DSK-09-05]] **step 9** for the artifact literal `desktop-msix-unsigned`, [[DSK-02-15]] steps 3, 5 and 6 for the `desktop-build` job shape — and record in the plan document and the post-implementation report which case applied. Three plans name overlapping lanes and only one of each may exist.
3. Load `pegasus-desktop`, then `authoring-github-workflows`. Extend `scripts/Get-CiChangeFlags.ps1` with a `desktop` flag covering `src/Pegasus.Desktop/**`, `src/Pegasus.Desktop.Infrastructure/**`, `src/Pegasus.Contracts/**`, `tests/Pegasus.Desktop.*/**`, `tests/Pegasus.Api.ContractTests/**` and `eng/packaging/**`, and add the matching output to the `changes` job. **This ticket owns the desktop change-flag contract**, because it is the only one of the overlapping-lane tickets that adds a `changes` job output and all three lanes it owns gate on it: [[DSK-02-15]] step 4 records that `$buildPattern` (line 11) already matches `^(src|tests)/` and deliberately edits nothing, and [[DSK-12-03]] defers on the same file. So the `desktop` flag must cover `eng/packaging/**` so [[DSK-09-05]]'s `desktop-package` lane gates on it, and [[DSK-09-05]] adds no second pattern for the same paths. Update `scripts/Test-CiChangeFlags.ps1` so the classifier's own regression tests cover the new flag, including a positive case for `eng/packaging/Test-AppInstaller.ps1` — that script is already a CI step.
4. `desktop-build` is owned by [[DSK-02-15]], so **extend** that job rather than adding a second one. If it has landed, add only `tests/Pegasus.Api.ContractTests` to its existing chained `dotnet test` line (the project [[DSK-08-01]] creates) and change no other step. If it has not landed, create it with exactly the shape [[DSK-02-15]] steps 3, 5 and 6 pin — `runs-on: windows-latest`, `needs: changes`, `if: needs.changes.outputs.build == 'true'`, `timeout-minutes: 30`, `actions/checkout@v7`, `actions/setup-dotnet@v6` with `dotnet-version: 10.0.x`, `dotnet restore ./Pegasus.slnx --locked-mode -r win-x64`, `dotnet build ./Pegasus.slnx --configuration Release --no-restore -p:Platform=x64`, then `dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release --no-build && dotnet test ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj --configuration Release --no-build` — and append the contract-test project to that same chained line. Chain with `&&` on one line: `pwsh` reports only the last command's exit code, which is why the existing `unit` job chains its two projects. Do not drop `tests/Pegasus.ArchitectureTests` and do not re-gate this lane on `needs.changes.outputs.desktop` — `desktop-build` keeps the `build` gate its owner pins, and only the three lanes this ticket owns gate on `desktop`. Record in the plan document which case applied.
5. Add job `desktop-package` on `windows-latest`, `needs: desktop-build`: generate a development certificate with `winapp cert generate --if-exists skip --quiet`, package with `winapp package ./bin/x64/Release/ --cert ./devcert.pfx --quiet --self-contained`, and upload the `.msix` with `actions/upload-artifact@v6`, `name: desktop-msix-unsigned` and `if-no-files-found: error` (the existing shard job uses exactly that guard). `desktop-msix-unsigned` is the literal [[DSK-09-05]] step 9 pins as the one name every consumer downloads: never rename it, never publish a second MSIX artifact under another name, and if the job already exists from [[DSK-09-05]] leave its upload step exactly as it stands.
6. Add job `desktop-ui-smoke`, `needs: desktop-package`, on the runner type the [[DSK-08-12]] research document decided. It downloads the package artifact with `actions/download-artifact@v6` and `name: desktop-msix-unsigned` — spelled exactly as [[DSK-09-05]] step 9 pins it, so a rename on either side shows up as a diff on both — then trusts the certificate, installs with `Add-AppxPackage`, runs `tests/Pegasus.Desktop.UITests/Invoke-UiSuite.ps1 -IncludeAccessibility`, and uploads the results JSON, screenshots and axe output. Give it `timeout-minutes` no higher than the existing `browser` lane's 25 and `fail-fast: false` semantics if it uses a matrix.
7. Add job `packaging-tests`, `needs: desktop-package`, running `eng/packaging/Test-Package.ps1` against a feed folder created inside the job, and uploading the transcripts. If [[DSK-08-12]] concluded the hosted runner cannot install a package, this job and `desktop-ui-smoke` both move to the self-hosted runner label together — they must not be split across runner types.
8. Verify the Linux path is untouched: the `changes` and `sql-integration-coverage` jobs still run on `ubuntu-latest` and no new job depends on them. Confirm `docs/runbook.md`'s canonical solution commands still describe what CI runs.
9. Use a shallow `actions/checkout` (`fetch-depth: 1`) in every new job — only the history guard in `changes` needs full depth, and the 700 MB repository has been observed timing out in checkout at around five minutes (DELIV-010).
10. Fix that checkout on the two existing jobs rather than leaving the recorded defect in place. Convert `changes` (`ubuntu-latest`, `timeout-minutes: 5`) and `documentation` (`windows-latest`, `timeout-minutes: 10`) to a **partial clone**: `actions/checkout@v7` with `filter: blob:none` and `fetch-depth: 0` **retained**, so `scripts/Test-MainBranchHistory.ps1` and the Markdown-placement history still resolve — a partial clone still fetches commits and trees and defers only blobs, which is why a shallow checkout cannot be used here. If the filter turns out to be unavailable or not to help, instead raise both jobs' `timeout-minutes` with the reason recorded in a comment above each job; do not do both. Then **measure**: ten consecutive `repository-check` runs must complete the `changes` job with no checkout cancellation, and the ten run URLs go in the post-implementation report. Upstream DELIV-010 recorded three of five runs cancelled mid-fetch on a ~680 MiB pack (PR #405 twice, PR #406 once, main run 32147904129), each failure blocking a release promotion until someone re-ran it, and under C-01 a re-run of the Windows `documentation` lane bills twice. Leave DELIV-010's second half alone — what makes the pack large and whether an asset belongs in LFS or out of history is an operator decision on history rewriting, not this ticket's.
11. Open the PR and confirm on the run: the four new lanes appear, are skipped when no desktop path changed, and are green when one did. Record the wall-clock duration of each new lane in the post-implementation report — [[DSK-08-19]] needs those numbers.
12. Confirm `git diff` for this branch touches only `.github/workflows/ci.yml`, `scripts/Get-CiChangeFlags.ps1`, `scripts/Test-CiChangeFlags.ps1` and documentation; anything else means scope crept.
13. Update `docs/desktop/08-testing/README.md` status line to "lanes green", add the lanes to `docs/operations.md`, run the simplification pass over the branch diff and record it under a dated `## Simplification pass` heading before merging.

## Acceptance criteria

- [ ] Four lanes exist and are green on a PR that touches a desktop path: the three this ticket owns (`desktop-package`, `desktop-ui-smoke`, `packaging-tests`) path-gated on the new `desktop` change flag, and `desktop-build` on the `needs.changes.outputs.build` gate [[DSK-02-15]] pins.
- [ ] They are skipped on a documentation-only PR.
- [ ] `scripts/Get-CiChangeFlags.ps1` carries exactly one pattern for the desktop paths — the `desktop` flag this ticket owns — and `eng/packaging/**` is classified by it and by no second pattern.
- [ ] The existing `repository-check` jobs are unchanged in behaviour apart from the `changes` and `documentation` checkout filter, whose effect is measured over ten consecutive runs with no checkout cancellation (upstream DELIV-010), and the Linux jobs still run on `ubuntu-latest`.
- [ ] Artifacts (MSIX, UI results, axe output, packaging transcripts) are uploaded with `if-no-files-found: error`, and the MSIX is uploaded and downloaded under the single literal `desktop-msix-unsigned`.
- [ ] `git diff` is limited to the workflow, the two change-flag scripts and documentation — the `changes` and `documentation` checkout change is inside `.github/workflows/ci.yml` and is expected there.
- [ ] Each new lane's measured duration is recorded.

## Verification

- [ ] `pwsh ./scripts/Test-CiChangeFlags.ps1` — expected: exit 0 with the new `desktop` flag covered.
- [ ] `pwsh ./scripts/Get-CiChangeFlags.ps1 -ChangedPath eng/packaging/Test-AppInstaller.ps1` — expected: `Desktop` is `True`.
- [ ] `grep -c 'desktop-msix-unsigned' .github/workflows/ci.yml` — expected: `2` — one upload in `desktop-package`, one download in `desktop-ui-smoke`, and no other artifact name for the MSIX.
- [ ] A PR touching `src/Pegasus.Desktop/**` — expected: all four lanes run and pass; artifacts present on the run.
- [ ] A PR touching only `docs/**` — expected: the four lanes are skipped and `documentation` still runs.
- [ ] Ten consecutive `repository-check` runs after the checkout change — expected: the `changes` job completes on every one with no cancellation during checkout; the ten run URLs recorded in the post-implementation report.
- [ ] `git diff --name-only origin/dev...HEAD` — expected: only `.github/workflows/ci.yml`, `scripts/Get-CiChangeFlags.ps1`, `scripts/Test-CiChangeFlags.ps1` and documentation files.

## Evidence tier

Tier 1 — Static/build/architecture. It obliges a compiling, packaging, artifact-producing pipeline with its own regression test for the change classifier; it proves toolchain consistency, not application behaviour.

## Documentation changes

- `docs/operations.md` — record the four lanes and what each produces, and the `changes`/`documentation` checkout filter with the ten-run measurement behind it.
- `docs/runbook.md` — note which desktop commands CI runs and which stay local.
- `docs/desktop/README.md` status table and `docs/desktop/08-testing/README.md` — mark 08 as "lanes green".

## Guardrails

- **Azure**: no write. No lane authenticates to Azure or reads Azure state.
- **Scope boundary**: may edit `.github/workflows/ci.yml`, `scripts/Get-CiChangeFlags.ps1`, `scripts/Test-CiChangeFlags.ps1` and documentation. Must not change test code, must not add a second workflow file, and must not sign with anything other than a development certificate generated inside the run. The `changes` and `documentation` edit is limited to the checkout step (or the timeout with its reason) — no step inside either job changes.
- **Traps**: three plans name an overlapping desktop CI lane ([[DSK-02-15]], [[DSK-09-05]], this ticket) — exactly one `desktop-package` job may exist and [[DSK-09-05]] owns it, and [[DSK-02-15]] owns `desktop-build` and packages nothing; this ticket extends those lanes and adds neither a second time. **One change-flag contract** — this ticket owns the `desktop` flag in `scripts/Get-CiChangeFlags.ps1` and the matching `changes` job output, and [[DSK-09-05]] gates `desktop-package` on it rather than adding a second pattern for `eng/packaging/**`; a second pattern covering the same paths, or one lane gated on two different `changes` outputs by two tickets, is a stop condition. Every added Windows lane costs 2× once the repositories are private (C-01). `pwsh` reports only the last command's exit code — chain multi-command steps with `&&`. Shallow checkout everywhere except the history guard, and the history guard keeps `fetch-depth: 0` even under `filter: blob:none` — dropping it breaks `Test-MainBranchHistory.ps1`. LocalDB is Windows-only, so no desktop or integration lane can move to Linux to save minutes.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.

## Outcome

_Filled at closeout._
