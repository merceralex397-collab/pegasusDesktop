---
id: TOOL-003
type: ticket
title: DSK-12-03 · Add the vendored-skill hash check to the CI `changes` job
status: preparing
area: agent-tooling
assignee: ''
profile: chore
stageEntered:
  preparing: '2026-08-24T21:21:29.769Z'
labels:
  - desktop-conversion
  - plan-12
  - phase-0
  - tier-1
groups:
  - EPIC-013
  - HZN-001
links: []
docs_todo: true
archived: false
created: '2026-08-24T08:04:54.717Z'
updated: '2026-08-24T21:21:29.769Z'
---

## What

Add a `Verify vendored agent skills` step running `eng/skills/verify-skills.ps1` to the `changes` job of `.github/workflows/ci.yml`, and prove with a deliberate one-byte mutation that the job goes red and names the drifted file.

## Why

A lockfile nobody checks is a comment. Proposal §21.2 puts the skill-hash verification into the CI stages so that a vendored skill edited by hand — or a sync run against the wrong commit — is caught in the pull request rather than discovered when an agent follows guidance that no longer matches the pinned revision. `docs/desktop/12-agent-tooling/README.md` § 4 makes "`verify-skills.ps1` green locally and in CI" part of the area's exit gate. Without it, [[DSK-12-02]]'s lockfile decays silently and [[DSK-12-10]]'s bump procedure has nothing enforcing it.

## Source of truth

- Plan row: `docs/desktop/12-agent-tooling/README.md` § 5 — `DSK-12-03`
- Plan detail: `docs/desktop/12-agent-tooling/README.md` § 3 ("CI runs the verifier in the `changes` job of `.github/workflows/ci.yml`") and § 4 Exit gate
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 21.2 CI stages, § 20.2 Pinning and vendoring
- Repository evidence:
  - `.github/workflows/ci.yml:1` — workflow name `repository-check`; triggers `pull_request` and `push` to `main`
  - `.github/workflows/ci.yml:12-15` — job `changes`, `runs-on: ubuntu-latest`, `timeout-minutes: 5`; its comment records that it is path detection only and is not Linux-development evidence
  - `.github/workflows/ci.yml:55-60` — the two existing repository-guard steps in that job: `Test SQL shard assignment` (`./scripts/Test-TestShard.ps1`) and `Migration runtime-grant check` (`./scripts/Test-MigrationGrants.ps1`), both `shell: pwsh`
  - `.github/workflows/ci.yml:67-69` — `Azure deployment plan (Local)` (`./scripts/Test-AzureDeploymentPlan.ps1 -Mode Local`), the last step of the job
  - `.github/workflows/ci.yml:71-87` — the `documentation` job on `windows-latest`, which is the lane every change set runs
  - `scripts/Get-CiChangeFlags.ps1:11-12` — `$buildPattern` and `$infrastructurePattern`; neither mentions `.agents/` or `eng/`, and the `changes` job itself runs unconditionally
  - `scripts/Test-CiChangeFlags.ps1` — the regression test for those patterns
- Binding decisions:
  - **C-01** (2026-08-23) — private-repository Windows runners bill at a 2× multiplier; the `changes` job is `ubuntu-latest`, so putting the check there costs the least. Do not create a new `windows-latest` job for it.
  - **L-05** — the board is seeded from these plans; the plan places this check in the `changes` job by name.
- Depends on: `DSK-12-02` — `eng/skills/verify-skills.ps1` and `eng/skills/skills.lock.json` must exist and be Linux-clean before CI can call them.

## Routing

- **Subagent**: `pegasus-release-packager` — `.codex/agents/pegasus-release-packager.toml`
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `authoring-github-workflows` (`.agents/skills/vendor/dotnet/authoring-github-workflows/`, from `dotnet/skills` `98f84851`) → `binlog-failure-analysis` (`.agents/skills/vendor/dotnet/binlog-failure-analysis/`) only if a run fails for a build reason
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`). No Azure MCP.
- **Kanmer pipeline** for profile `chore`: `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout`. Gates: `leave-preparing` needs `plan` + `questions-resolved`; `enter-done` needs `proof` + `questions-resolved`. Call `get_doc_gates <this ticket's board id>` before every move.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Orientation. Read the plan row and the plan sections named under **Source of truth**, then `get_doc_gates <this ticket's board id>` and `take_ticket`.
2. Confirm the prerequisite actually landed: `test -f eng/skills/verify-skills.ps1 && test -f eng/skills/skills.lock.json`, then run `pwsh ./eng/skills/verify-skills.ps1` locally and record exit code 0. If it fails locally, stop and hand back to [[DSK-12-02]].
3. Load `authoring-github-workflows` and follow its guidance on step placement and shell selection before editing the workflow.
4. Edit `.github/workflows/ci.yml`: insert a step into the `changes` job immediately after `Migration runtime-grant check` (`.github/workflows/ci.yml:58-60`) and before `Azure deployment plan (Local)`, in the same style as its neighbours:

   ```yaml
         - name: Verify vendored agent skills
           shell: pwsh
           run: ./eng/skills/verify-skills.ps1
   ```

   Keep it in `changes` (`ubuntu-latest`). Do not move it to `documentation` (`windows-latest`) and do not add a job.
5. Check the job budget: `changes` has `timeout-minutes: 5` (`.github/workflows/ci.yml:16`). Time the verifier locally; if it runs in seconds, leave the timeout at 5 and record the measurement. Only raise it with the measured number in the commit message.
6. Confirm the verifier is genuinely cross-platform before pushing: run it on Linux paths in your head against `scripts`-style conventions — forward slashes, no `Get-Acl`/`Get-AppxPackage`/registry access, case-sensitive path comparisons. A Windows-only verifier fails the ubuntu job for the wrong reason.
7. **Decide and record** whether `scripts/Get-CiChangeFlags.ps1` needs a new path in `$buildPattern`. The `changes` job runs on every event with no `if:` guard, so the answer today is *no change needed*; write that sentence and its reason in the plan document so the next agent does not re-open it. If it is changed, `scripts/Test-CiChangeFlags.ps1` must be updated in the same commit.
8. Prove the check bites. On the ticket branch, add one character to a vendored `SKILL.md` (for example `.agents/skills/vendor/windows/winui-design/SKILL.md`), commit, push, and observe the `changes` job fail with the drifted path named in the log. Capture the run URL.
9. Revert the mutation (`git revert` or `git checkout --` plus a new commit), push, and observe the same job green. Capture the second run URL. Both URLs are the proof.
10. Run the neighbouring guards locally so the edit is shown not to have disturbed them: `pwsh ./scripts/Test-CiChangeFlags.ps1` and `pwsh ./scripts/Test-TestShard.ps1` — both expected to pass.
11. Open the PR into `dev`. Record in the post-implementation report the red run, the green run, the measured verifier duration, and the step 7 decision.

## Acceptance criteria

- [ ] `.github/workflows/ci.yml` `changes` job contains a `Verify vendored agent skills` step running `./eng/skills/verify-skills.ps1` with `shell: pwsh`.
- [ ] The step sits on `ubuntu-latest`; no new job and no `windows-latest` runner was added (C-01).
- [ ] A pushed one-byte mutation of a vendored skill turns the `changes` job red and the log names the file.
- [ ] After reverting, the same job is green.
- [ ] The `Get-CiChangeFlags.ps1` decision is recorded with its reason in the plan document.
- [ ] The `changes` job still completes inside its `timeout-minutes`.

## Verification

- [ ] `pwsh ./eng/skills/verify-skills.ps1` — expected: exit code 0 locally on a clean tree.
- [ ] `grep -n 'Verify vendored agent skills' -A 2 .github/workflows/ci.yml` — expected: the step with `shell: pwsh` and `run: ./eng/skills/verify-skills.ps1`.
- [ ] The recorded red CI run URL — expected: `changes` job failed, log names the mutated vendored path.
- [ ] The recorded green CI run URL after revert — expected: `repository-check / changes` succeeded.
- [ ] `pwsh ./scripts/Test-CiChangeFlags.ps1` — expected: pass, unchanged.

## Evidence tier

Tier 1 — Static/build/architecture. It obliges CI run evidence for both the failing and the passing state; a green run alone does not prove a gate exists, which is why the deliberate-drift run is part of the acceptance.

## Documentation changes

- `None.` The procedure text belongs in `docs/runbook.md` and is [[DSK-12-10]]'s work; adding it here would create a second copy.

## Guardrails

- **Azure**: no write. This ticket touches no Azure resource; note that the neighbouring `Azure deployment plan (Local)` step is `-Mode Local` and needs no credentials — do not change it.
- **Scope boundary**: may edit `.github/workflows/ci.yml` only, plus a transient mutation to one vendored file that must be reverted in the same PR. Must not edit `eng/skills/*.ps1` (that is [[DSK-12-02]]), `scripts/Get-CiChangeFlags.ps1` unless step 7 decides otherwise, or any file under `src/` or `tests/`.
- **Traps**: the `changes` job is `ubuntu-latest`, so a Windows-only PowerShell construct fails it for the wrong reason; a push to `main` in that job also runs `scripts/Test-MainBranchHistory.ps1`, so never push this branch straight to `main`; branch → PR into `dev` → exact-SHA promotion with the literal `MERGE AUTH GRANTED`.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.

## Outcome

_Filled at closeout._
