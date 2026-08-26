# Plan — FND-001: Create the fork's `dev` branch from `main` and record the conversion baseline

**Diff estimate: ~1 file, ~3 lines.**

Derived from the measured inventory below, not asserted. This ticket's repository
diff is one sentence inside one existing paragraph; everything else it does is a
Git ref operation on `origin`, which produces no diff at all.

## Measured file-and-line inventory

`chore` owes no `files` document, so the surface area is measured here. Every
figure below was read on 2026-08-24 from the working tree at `origin/main`
`191ddf334208b8966dc5e32f4f597e434a086233`.

| Path | Measured today | What this ticket changes |
| --- | --- | --- |
| `docs/desktop/README.md` | 142 lines; the "Planning baseline" paragraph is `:12-15` (`fork merceralex397-collab/pegasusDesktop, branch main at 191ddf33, 2026-08-23 …`) | +1 sentence inside that paragraph — 2 lines at the file's 78-column hard wrap, plus 1 line of churn on the reflowed neighbour |
| `refs/heads/dev` on `origin` | absent — `git ls-remote --heads origin dev` printed nothing on 2026-08-24 | created at the baseline SHA; a ref, not a diff |
| `AGENTS.md` (334 lines), `docs/engineering.md` (236 lines) | unchanged | **not touched** — the body's step 8 forbids it |

Nothing else in the tree is in scope, so the estimate cannot honestly exceed
one file.

## Approach

Create the branch by pushing the baseline SHA straight to `refs/heads/dev` on
`origin` (`git push origin <sha>:refs/heads/dev`) rather than by checking a
branch out locally and pushing it. The push-a-SHA form cannot pick up an
accidental local commit, needs no working-tree switch, and leaves the current
worktree — which is on a task branch — untouched. The rejected alternative was
`git switch -c dev origin/main && git push -u origin dev`: it produces the same
ref but only if the local `main` is exactly at the baseline, and it silently
publishes whatever the local checkout happens to hold. Since the whole value of
this ticket is that `dev` starts at a *recorded* commit, the form that names the
commit explicitly is the correct one.

The documentation half is deliberately one sentence in an existing paragraph.
`scripts/Test-MarkdownPlacement.ps1:31` restricts new Markdown to
`docs/(prd|frd|adr|design|desktop)` (plus `workspaces/document-extraction`,
`.agents/skills`, `.design-sync`, `.grok`, `.stitch`,
`design/planning-and-old-designs`), and the CI `documentation` job
(`.github/workflows/ci.yml:71-87`, `windows-latest`) runs the placement
regression test and `Test-DocumentationLinks.ps1` on every change set — so a new
note file would be both unnecessary and a placement risk.

## Governing docs

The ticket's `refs` is empty and `docs_todo: true` — confirmed by
`get_doc_gates FND-001`, whose `leave-backlog` boundary is absent for the
`chore` profile and whose `docs_todo` field reads `true`. No repository ADR
governs this work today.

> **New ADR** — ADR-0100 (native WinUI 3 desktop client converted inside this
> fork), authored by [[FND-005]] (plan handle `DSK-00-05`); ADR-0100 is
> co-claimed by [[FND-026]] (plan handle `DSK-02-01`), so write
> `authored by [[FND-005]]; see [[FND-005]]'s plan for the ownership
> reconciliation` rather than asserting a single author.
> This plan is written to the decision as recorded in
> `docs/desktop/00-governance-and-workflow/README.md` § 3 ("Recommended
> branching flow", item 1) and to D-001 as recorded in `docs/desktop/README.md`
> § Locked decisions and open decisions; if the ADR lands differently this plan
> is revised before implementation. The D-001 consequence text itself is written
> into ADR-0100 by [[FND-010]] (plan handle `DSK-00-10`).

Because `refs` is empty, the authorities that actually bind today are these:

| Authority | Requirement | Met by |
| --- | --- | --- |
| `docs/engineering.md:10-15` § Branches and delivery | "Task branches are cut from `dev` and merge into `dev` through a PR. `main` is the active deployment…" — `dev` must exist for any conversion PR to follow the rule | Steps 4–5 |
| `docs/engineering.md:16-33` | Promotion is an exact-SHA atomic fast-forward requiring `git merge-base --is-ancestor origin/main origin/dev` and the literal `MERGE AUTH GRANTED` | Step 5 (proves the ancestor precondition holds from creation) |
| `AGENTS.md:266+` § Repository task workflow | Each task takes its own worktree cut from `origin/dev` | Step 7 |
| `.github/workflows/ci.yml:24-32` | The `changes` job fetches `refs/heads/dev` and runs `scripts/Test-MainBranchHistory.ps1 -ReleaseBranch origin/dev` on every push to `main`; `Test-MainBranchHistory.ps1:36-50` throws when `main`'s head is not contained in the release branch | Steps 4–5 make that guard satisfiable at all |
| `scripts/Test-MarkdownPlacement.ps1:31` | New Markdown only under the allowed roots | Step 7 (edits an existing file; creates none) |
| Proposal § 29 item 1 | "Choose the fork or protected conversion branch and freeze its baseline commit" | Steps 2–4 |
| L-05 (`docs/desktop/README.md` § Locked decisions) | The board is seeded from these plans, so every seeded ticket needs a `dev` to merge into | The whole ticket |
| D-001 (decided 2026-08-23) | The fork becomes the single release source at the first production gateway change — its `dev`/`main` pair is the conversion trunk | Step 6's default-branch reasoning |
| C-01 (2026-08-23) | The repositories become private on completion; no branch protection bought on a public plan may be assumed | Step 6 (records that no GitHub ruleset is relied on) |

## Routing

Copied from the ticket body's `## Routing` block, as
`docs/desktop/00-governance-and-workflow/README.md` § "Ticket template" requires
of the plan document specifically.

- **Subagent**: `winui-dev` — `.codex/agents/winui-dev.toml` (verified present).
  The plan row allows "operator or `winui-dev`"; step 4 is operator-performed
  either way.
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `kanmer-plan`
  (`.grok/skills/kanmer-plan/SKILL.md`) → `kanmer-execute`
  (`.grok/skills/kanmer-execute/SKILL.md`).
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`,
  `set_ticket_doc`, `append_scratch`, `move_item`).
- **Kanmer pipeline** for profile `chore`: `kanmer-tickets` → `kanmer-plan` →
  `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout`. Call
  `get_doc_gates FND-001` before every move; a move crosses at most one gated
  boundary.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement
  (`AGENTS.md` § Repository task workflow step 5).

## Steps

These refine the body's eleven implementation steps; the order, the ownership
and the file paths are the body's.

1. **Orient.** Read `docs/desktop/00-governance-and-workflow/README.md` § 5 row
   `DSK-00-01` and § 3 "Recommended branching flow" item 1, then
   `docs/engineering.md:1-52`. Call `get_doc_gates FND-001` — expect
   `leave-preparing: [plan, questions-resolved]` and `enter-done:
   [proof, questions-resolved]`, with no `leave-backlog` row. Then `take_ticket`,
   which records the worktree and branch.
2. **Re-verify the baseline.** `git fetch origin --prune`, then
   `git rev-parse origin/main`. Expected
   `191ddf334208b8966dc5e32f4f597e434a086233` — the value measured on
   2026-08-24 and the value recorded at `docs/desktop/README.md:12-13`. If it
   differs, stop: record the observed SHA in this plan and in
   `docs/desktop/README.md` before continuing. The baseline is a recorded fact,
   not a guess.
3. **Prove `dev` is absent.** `git ls-remote --heads origin dev` prints nothing;
   `git branch -a` lists `main`, `kanmer-board`, `remotes/origin/main`,
   `remotes/origin/HEAD -> origin/main` and any local task branch. Both were
   true on 2026-08-24. If `dev` already exists, this ticket becomes a
   verification: confirm it sits at the baseline SHA and skip to step 5.
4. **Operator step — create the branch from the exact SHA**, checking nothing
   out:
   ```
   git push origin 191ddf334208b8966dc5e32f4f597e434a086233:refs/heads/dev
   git ls-remote --heads origin dev
   ```
   Hand back both outputs. Do not substitute `git push -u origin dev` from a
   local checkout; the SHA form is what makes the ref provably the baseline.
5. **Verify the two heads agree.**
   ```
   git fetch origin --prune
   git rev-parse origin/dev origin/main
   git merge-base --is-ancestor origin/main origin/dev; echo $?
   ```
   Expected: two identical SHAs, then `0`. The third command is the exact
   precondition `docs/engineering.md:17` requires before any promotion and the
   containment `scripts/Test-MainBranchHistory.ps1:42-45` asserts in CI.
6. **Answer the default-branch question in this plan, not silently** — the body
   requires it and makes it an acceptance criterion.
   **Decision taken here (default, no operator input required):** the fork's
   GitHub default branch **stays `main`**. Measured today,
   `remotes/origin/HEAD -> origin/main`. Reasons: `docs/engineering.md:12-13`
   makes `main` "the active deployment and the sole revision eligible for an
   authorised release", which is what a default branch should present; leaving
   it alone means this ticket changes **no** GitHub setting, which is what its
   own Guardrails require; and under C-01 the repository becomes private, so no
   public landing-page consideration applies. The one hazard this leaves is that
   a bare `gh pr create` would default its base to `main`; it is closed by
   always passing `--base dev`, which step 10 and every conversion ticket's PR
   step already do. If the operator would rather the default were `dev`, that is
   a one-line GitHub setting change and belongs to them — record their answer
   here and change nothing without it. This is a recorded decision with a
   default taken, not an open question: it does not gate `leave-preparing`.
7. **Record the branch.** Create the task worktree the workflow expects
   (`git worktree add`, branch cut from `origin/dev` per `AGENTS.md`
   § Repository task workflow step 2), then edit the "Planning baseline"
   paragraph at `docs/desktop/README.md:12-15`, adding one sentence naming the
   `dev` branch, the SHA it was created at, and the date it happened. Match the
   file's existing hard wrap near 78 columns (`docs/engineering.md` § Markdown
   convention). Create no new Markdown file.
8. **Change nothing else.** `docs/engineering.md` § Branches and delivery is
   claimed by [[FND-009]] (plan handle `DSK-00-09`, release-tag convention) and
   [[FND-002]] (plan handle `DSK-00-02`, the one-way `upstream` sync sentence).
   Keep this diff to the single paragraph in `docs/desktop/README.md`.
9. **Run the documentation gates locally**, the same two the CI `documentation`
   job runs at `.github/workflows/ci.yml:84,87`:
   ```
   pwsh ./scripts/Test-DocumentationLinks.ps1
   pwsh ./scripts/Test-MarkdownPlacement.ps1 -Base origin/dev -Head HEAD
   ```
   Both exit 0. Note that `Test-MarkdownPlacement.ps1` takes `-Base` and `-Head`
   as **mandatory** parameters (`scripts/Test-MarkdownPlacement.ps1:2-5`) — a
   bare invocation fails on the missing argument, not on a placement violation.
   CI itself calls the regression wrapper `scripts/Test-TestMarkdownPlacement.ps1`,
   which takes none.
10. **Open the PR against `dev`** (`gh pr create --base dev`), take the
    independent review from `pegasus-desktop-reviewer`, and merge once
    `repository-check` is green.
11. **Write the proof** as a `command-log`: the `git rev-parse origin/dev
    origin/main` output showing equal SHAs, the `git ls-remote --heads origin dev`
    line, and the `git merge-base --is-ancestor` exit code.

## Verification

Evidence tier 1 — Static/build/architecture (`docs/engineering.md:72-90`
§ Required evidence tiers), as the ticket body states. Branch topology and
documentation consistency are the whole of the claim; nothing here proves
application behaviour.

The `proof` document is a `command-log` holding, verbatim:

| Command | Expected |
| --- | --- |
| `git ls-remote --heads origin dev` | one line ending `refs/heads/dev` |
| `git rev-parse origin/dev origin/main` | two identical SHAs |
| `git merge-base --is-ancestor origin/main origin/dev; echo $?` | `0` |
| `pwsh ./scripts/Test-DocumentationLinks.ps1` | exits 0, no broken relative link |
| `pwsh ./scripts/Test-MarkdownPlacement.ps1 -Base origin/dev -Head HEAD` | exits 0, no violation |

Proof is written on merged `main`, after review and the merge — never before
(`AGENTS.md` § Kanmer operating instructions).

## Risks / open questions

- **The baseline may have moved.** `origin/main` was
  `191ddf3342…` on 2026-08-24. If step 2 reads anything else, someone pushed to
  `main` outside the promotion procedure. Mitigation: stop and record the
  observed SHA in both this plan and `docs/desktop/README.md` before creating
  the branch; do not quietly retarget.
- **`dev` may already exist by the time this runs.** Creation is idempotent only
  if the existing ref is at the baseline. Mitigation: step 3 checks first; if it
  exists elsewhere, this ticket verifies and records rather than force-updating
  — `docs/engineering.md:13-14` forbids rebasing, resetting or force-pushing
  `dev` or `main`.
- **Default branch** — answered in step 6 with a default taken (stays `main`)
  and the reasoning recorded. Not an open question.
- **`docs/engineering.md` edit collision** — [[FND-009]] and [[FND-002]] both
  edit § Branches and delivery. This is a scope boundary, not a question: this
  ticket does not touch that file at all.
- **No branch protection.** `docs/engineering.md:34-37` records that GitHub
  protection and rulesets are intentionally out of scope on subscription
  grounds, so the main-push CI check is detective rather than preventive. This
  ticket adds no protection and must not be reviewed as though it should.

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over
this branch's own diff before the PR, recorded here under a dated heading.
Expected outcome for this ticket: `n/a — docs-only`._

## Execution result — 2026-08-25

The ticket was taken after the live board showed it unclaimed. Before any external write, git fetch origin --prune and the required ref checks were rerun in .worktrees/fnd-001:

- origin/main is 191ddf334208b8966dc5e32f4f597e434a086233, matching the recorded planning baseline.
- git ls-remote --heads origin dev reports 5770eb21c0d03620a6a6d99e0431bde91ec2ad6a refs/heads/dev; it is not the frozen baseline SHA.
- git merge-base --is-ancestor origin/main origin/dev exits 0.
- gh pr view 1 proves dev currently contains merged PR #1 (FND-005), merged at 2026-08-25T00:12:46Z, with merge commit 5770eb21c0d03620a6a6d99e0431bde91ec2ad6a.

The required initial ref state no longer exists. No force-update, reset, or GitHub setting change is permitted, and no repository file was changed. The ticket is therefore blocked on an external repository-state decision: whether the current descendant dev head is accepted as the recorded branch baseline, or an authorized owner defines a permitted corrective path. This ticket does not claim the frozen-SHA acceptance criterion.

## Operator decision — 2026-08-25

The operator accepts the current `origin/dev` descendant head as the effective recorded conversion baseline. `dev` was created from the frozen `origin/main` baseline, then advanced only by merged PR #1. This ticket therefore records the verified topology rather than force-updating either branch: `origin/main` remains an ancestor of `origin/dev`; `dev` is the branch from which new task branches are cut. The GitHub default branch remains `main`; no setting change is authorised or needed.

## Revised acceptance and verification

- Prove `origin/main` is `191ddf334208b8966dc5e32f4f597e434a086233` and is an ancestor of `origin/dev`.
- Prove the merge history records `5770eb21c0d03620a6a6d99e0431bde91ec2ad6a` as the initial integration advancement of `dev`.
- Record the current `dev` SHA and date in `docs/desktop/README.md`; do not assert that its current head equals `main`.

## Simplification pass — 2026-08-25

n/a — docs-only. The scoped change is one existing baseline paragraph; no new abstraction, compatibility path, or branch-management mechanism is introduced.

## Post-implementation report — 2026-08-25

The ticket's original branch-creation premise was superseded by the operator's accepted live topology. The repository diff is exactly one file: `docs/desktop/README.md` adds two lines recording `dev` at `5770eb21` and that the recorded `main` baseline is its ancestor. It does not edit code, workflows, settings, branches, cloud resources, or governing documents.

Live ref evidence from the task worktree:

- `origin/main`: `191ddf334208b8966dc5e32f4f597e434a086233`
- `origin/dev`: `5770eb21c0d03620a6a6d99e0431bde91ec2ad6a`
- `git merge-base --is-ancestor origin/main origin/dev`: exit 0
- `git show -s origin/dev`: merge commit `5770eb21`, `Merge pull request #1`

Validation rerun at PR #3 head `8d6fc34d`:

- `pwsh ./scripts/Test-DocumentationLinks.ps1` — passed: `All relative Markdown links resolve (232 files checked).`
- `pwsh ./scripts/Test-MarkdownPlacement.ps1 -Base origin/dev -Head HEAD` — passed.
- `git diff --check origin/dev...HEAD` — passed.

The resolved `chore` profile permits only `plan` and `scratch` documents, so this report is recorded in the plan rather than inventing an unsupported pipeline document.

## Independent review disposition — 2026-08-25

Independent reviewer: `pegasus-desktop-reviewer` (Herschel), who did not implement the change.

- **High — blocking:** GitHub has no reported checks for PR #3. `gh pr checks 3 --watch=false` returns no checks and `GET /repos/merceralex397-collab/pegasusDesktop/actions/workflows` returns `total_count: 0`, although `origin/dev` contains `.github/workflows/ci.yml`. Disposition: unresolved external CI-registration blocker; do not merge or change settings as a workaround.
- **Medium — evidence count:** the reviewer reported a non-reproduced `226` count. The exact command was rerun in the task worktree at PR head and returned `232`; the original recorded count is retained with this exact rerun evidence.
- **Medium — author report:** the reviewer found no separate report document. Disposition: resolved by this section in the profile-permitted plan document; `get_doc_gates FND-001` confirms that `post-implementation-report` is not an allowed document type for this `chore` ticket.

Review conclusion pending re-review of this reconciliation.

## Final independent re-review disposition — 2026-08-25

Meitner independently re-reviewed PR #3 at its updated head `1a78a16f`. The required owning area-plan correction is now implemented: `docs/desktop/00-governance-and-workflow/README.md` records the accepted topology `main=191ddf33`, `dev=5770eb21`, `main` as an ancestor, the trunk branching rule, and the corrected DSK-00-01 acceptance/verification. The prior stale ticket statements that CI was absent and `docs_todo` was true are superseded: live `get_doc_gates FND-001` reports `docs_todo:false`, and CI run `32849827677` is green with only expected conditional skips. No code, cloud, branch-protection, or release operation is involved. Independent review is now satisfied; merge remains subject to the updated PR checks/merge state and proof after merge.

## Closeout correction — 2026-08-26

The earlier CI-registration blocker is superseded. PR #3 merged into `dev` at `aa7339286416d29c9c65431886d7a072d92a1270`; final independent re-review recorded CI run `32849827677` green at head `1a78a16f`. The later in-repository boundary work records the accepted conversion trunk as `dev` and preserves the main-ancestor relationship; current `origin/main` contains the resulting documentation. No branch rewrite, upstream synchronization, cloud write, deployment, or GitHub-setting change was performed. Proof is now written for the merged delivery.
