# Plan — TOOL-003 (plan handle `DSK-12-03`): Add the vendored-skill hash check to the CI `changes` job

**Diff estimate: ~1 file, ~3 lines** (a three-line step inserted into
`.github/workflows/ci.yml`). Net diff at merge is exactly that. In flight the branch also
carries two transient commits — one that mutates a vendored `SKILL.md` by a single byte to
prove the gate bites, and one that reverts it — which cancel out and must both be on the
PR so the reviewer can see the red run and the green run.

## Approach

Put the check in the existing `changes` job as a peer of the two repository guards already
there, rather than creating a job for it. `.github/workflows/ci.yml:55-60` already runs
`./scripts/Test-TestShard.ps1` and `./scripts/Test-MigrationGrants.ps1` with `shell: pwsh`
on `ubuntu-latest`; a vendored-skill hash check is the same kind of thing — a cheap,
unconditional repository invariant — and the job already runs on every event with no `if:`
guard, so no path-detection plumbing is needed. The alternative considered and rejected was
a **new `windows-latest` job**: `verify-skills.ps1` is written to be Linux-clean precisely
so it can live here, and C-01 (2026-08-23) makes private-repository Windows runner minutes
bill at a 2× multiplier — paying that to hash 35 files would be a self-inflicted recurring
cost. A second rejected option was adding it to the `documentation` job
(`.github/workflows/ci.yml:71-87`); that job is deliberately `windows-latest` (its own
comment explains why: `Test-Path` case sensitivity differs and moving it would quietly
change the rule), so it is the wrong home for a check that has nothing to do with Markdown.

## Governing docs

The ticket carries `refs: []` and **`docs_todo: true`**.

> **New ADR** — ADR-0110 (agent-skill pinning and the invocation protocol), authored by
> [[TOOL-008]] (plan handle `DSK-12-08`), filename
> `docs/adr/0110-pin-agent-skills-and-invocation-protocol.md`. This plan is written to the
> decision as recorded in `docs/desktop/12-agent-tooling/README.md` § 3 and § 4 and in the
> reserved ADR block at `docs/desktop/00-governance-and-workflow/README.md` § 3. ADR-0110's
> own Verification section will name this CI step as its enforcement evidence, so if the ADR
> lands differently this plan is revised before implementation.

Programme-level authorities this plan meets:

| Authority | Requirement | Met by |
| --- | --- | --- |
| Proposal §21.2 (CI stages) | Skill-hash verification runs in CI | Step 4 |
| `docs/desktop/12-agent-tooling/README.md` § 4 (exit gate) | "`verify-skills.ps1` green locally **and in CI**" | Steps 2 and 9 |
| C-01 (2026-08-23) | Private Windows runner minutes bill at 2× | Step 4 keeps the step on `ubuntu-latest`; no new job |
| L-05 (locked) | The board is seeded from these plans; the plan names the `changes` job | Step 4 |
| `docs/engineering.md` § Branches and delivery | Task branch → PR into `dev`; never push this branch to `main` | Step 11 |

## Routing

Copied from the ticket body's `## Routing` block.

- **Subagent**: `pegasus-release-packager` — `.codex/agents/pegasus-release-packager.toml`
  (`sandbox_mode = "workspace-write"`, `model_reasoning_effort = "high"`).
- **Skills**, in load order:
  1. `pegasus-desktop` — `.agents/skills/project/pegasus-desktop/SKILL.md`
  2. `authoring-github-workflows` — `.agents/skills/vendor/dotnet/authoring-github-workflows/`
     (from `dotnet/skills` `98f848512e9ee4877e399a0ae367bb5e4a193144`; created by
     [[TOOL-002]], plan handle `DSK-12-02`)
  3. `binlog-failure-analysis` — `.agents/skills/vendor/dotnet/binlog-failure-analysis/`
     — **only** if a run fails for a build reason; do not load it for form's sake
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`,
  `append_scratch`, `move_item`). **No Azure MCP.**
- **Kanmer pipeline** for profile `chore`: `kanmer-plan` → `kanmer-execute` →
  `kanmer-review` → `kanmer-verify` → `kanmer-closeout`. Gates confirmed by
  `get_doc_gates TOOL-003`: `leave-preparing` needs `plan` + `questions-resolved`;
  `enter-done` needs `proof` + `questions-resolved`. No `research`, `files` or `checklist`
  gate on a `chore`. Call `get_doc_gates TOOL-003` before every move.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement
  (`AGENTS.md` § Repository task workflow step 5).

## Steps

Refines the body's 11 steps in the same order.

1. **Orientation.** Read `EPIC-013/context.md` (`get_group_doc EPIC-013 context.md`), then
   the plan sections named in the body's **Source of truth**. `get_doc_gates TOOL-003`,
   then `take_ticket`.
2. **Confirm the prerequisite actually landed.** `test -f eng/skills/verify-skills.ps1 &&
   test -f eng/skills/skills.lock.json`, then `pwsh ./eng/skills/verify-skills.ps1` and
   record exit code 0. If it fails locally, stop and hand back to [[TOOL-002]] — a CI step
   that calls a broken script turns every PR red for a reason that is not the PR's fault.
3. **Load `authoring-github-workflows`** and follow its guidance on step placement and
   shell selection before touching the workflow.
4. **Insert the step.** In the `changes` job, immediately after
   `Migration runtime-grant check` (`.github/workflows/ci.yml:58-60`) and before
   `Azure deployment plan (Local)` (`:67-69`), in the exact style of its neighbours:

   ```yaml
         - name: Verify vendored agent skills
           shell: pwsh
           run: ./eng/skills/verify-skills.ps1
   ```

   Verified 2026-08-24: the `changes` job is `.github/workflows/ci.yml:12`,
   `runs-on: ubuntu-latest` at `:15`, `timeout-minutes: 5` at `:16`, and the workflow name
   is `repository-check` at `:1`, triggered on `pull_request` and `push` to `main`. Keep
   the step in `changes`. Do not move it to `documentation` and do not add a job.
5. **Check the job budget.** The job's ceiling is `timeout-minutes: 5`. Time the verifier
   locally (`Measure-Command { pwsh ./eng/skills/verify-skills.ps1 }`) and record the
   number. It hashes ~35 skill folders (~80 files), so seconds is the expected order —
   leave the timeout at 5 and record the measurement. Only raise it with the measured
   number in the commit message.
6. **Re-check Linux cleanliness before pushing**, because the job runs on `ubuntu-latest`
   and a Windows-only construct fails it for the wrong reason and burns a round trip. Read
   `eng/skills/verify-skills.ps1` for: backslash path literals, `Get-Acl`,
   `Get-AppxPackage`, registry access, `$env:USERPROFILE`, drive-letter assumptions, and
   any path comparison that assumes case-insensitivity (Linux filesystems are
   case-sensitive; the `documentation` job's own comment at
   `.github/workflows/ci.yml:71-76` records that this exact difference is why it stays on
   Windows). Do not edit the script to fix a problem — that is [[TOOL-002]]'s file; hand it
   back.
7. **Record the `Get-CiChangeFlags.ps1` decision so nobody re-opens it.**
   **The answer is: no change needed.** Reason, verified 2026-08-24: the `changes` job has
   no `if:` guard and runs on every `pull_request` and every `push` to `main`, so the new
   step executes unconditionally regardless of which paths changed. `$buildPattern` and
   `$infrastructurePattern` (`scripts/Get-CiChangeFlags.ps1:11-12`) only gate the
   *downstream* jobs that consume the `build` / `infrastructure` outputs; adding `.agents/`
   or `eng/` to `$buildPattern` would trigger a full application build whenever a vendored
   Markdown file changed — strictly worse, and a 2× multiplier cost under C-01. Write that
   sentence and its reason here under a dated heading. If a future change does edit the
   patterns, `scripts/Test-CiChangeFlags.ps1` must be updated in the same commit.
8. **Prove the check bites.** On the ticket branch, append one character to a vendored
   `SKILL.md` (for example `.agents/skills/vendor/windows/winui-design/SKILL.md`), commit,
   push, and observe the `changes` job fail **with the drifted path named in the log**.
   Capture the run URL. A red job whose log does not name the file is only half the
   evidence — the point of the gate is that it tells you which file drifted.
9. **Revert and prove green.** `git revert` the mutation (or `git checkout --` plus a new
   commit), push, observe `repository-check / changes` succeed. Capture the second run URL.
   Both URLs are the proof.
10. **Show the neighbours are undisturbed.** `pwsh ./scripts/Test-CiChangeFlags.ps1` and
    `pwsh ./scripts/Test-TestShard.ps1` locally — both expected to pass, unchanged.
11. **Open the PR into `dev`.** Never push this branch to `main`: on a push to `main` the
    same `changes` job runs `./scripts/Test-MainBranchHistory.ps1`
    (`.github/workflows/ci.yml:24-32`), and promotion to `main` is an exact-SHA atomic
    fast-forward requiring the literal `MERGE AUTH GRANTED`
    (`docs/engineering.md:10-52`). Record in the post-implementation report: the red run
    URL, the green run URL, the measured verifier duration from step 5, and the step 7
    decision.

## Verification

Evidence tier **1 — Static/build/architecture**, as the body states. The deliberate-drift
run is part of the acceptance precisely because a green run alone does not prove a gate
exists. `proof` is a `command-log` plus the two CI run URLs.

1. `pwsh ./eng/skills/verify-skills.ps1` → exit 0 locally on a clean tree.
2. `grep -n 'Verify vendored agent skills' -A 2 .github/workflows/ci.yml` → the step with
   `shell: pwsh` and `run: ./eng/skills/verify-skills.ps1`.
3. The recorded **red** CI run URL → `changes` job failed and the log names the mutated
   vendored path.
4. The recorded **green** CI run URL after the revert → `repository-check / changes`
   succeeded.
5. `pwsh ./scripts/Test-CiChangeFlags.ps1` → pass, unchanged.
6. The `changes` job duration on the green run → inside `timeout-minutes: 5`.

## Risks / open questions

| Risk | Mitigation |
| --- | --- |
| A Windows-only construct in `verify-skills.ps1` fails the ubuntu job for the wrong reason. | Step 6 reads for it before pushing; if found, hand back to [[TOOL-002]] rather than editing that file here (scope boundary). |
| The drift commit is merged by accident, leaving a mutated vendored file on `dev`. | Steps 8–9 are one PR; the reviewer checks the net diff is the three workflow lines only. `git diff origin/dev...HEAD --stat` on the final commit must show `.github/workflows/ci.yml` and nothing else. |
| The mutation makes the branch's own PR red and blocks merge. | Expected and correct. Push the revert before requesting review; the red run URL is already captured. |
| Adding paths to `$buildPattern` "to be safe" triggers full application builds on vendored-Markdown changes. | Step 7 records the decision *not* to, with the reason, so it is not re-litigated. |
| Verifier runtime grows as more skills are vendored and eats the 5-minute budget. | Step 5 records today's measured number so a future regression is visible rather than mysterious. |

Open questions: **none.** Nothing in this ticket is undecided — the job, the runner, the
step position and the `Get-CiChangeFlags.ps1` answer are all settled above, and no
`open-questions` document is created.

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over this
branch's own diff before the PR, recorded here under a dated heading. The net diff is three
YAML lines, so the honest disposition is likely "no findings" for all four lenses — but
record that explicitly rather than omitting the heading. This branch is not docs-only, so
`n/a — docs-only` is not available._
