# Plan — FND-002: Add the read-only `upstream` remote and land the first one-way sync from `upstream/main`

**Diff estimate: ~2 authored files, ~6 authored lines — plus a merge commit carrying the upstream range (32 commits at the 2026-08-23 planning baseline; re-derive it at execution).**

Derived from the measured inventory below. The distinction matters: this ticket
*authors* six lines and *carries* however many upstream files the merge brings.
A reviewer who reads the diff stat as this ticket's own work will reject a clean
sync; a reviewer who does not read it at all will miss an upstream change that
lands in a file the conversion plans already claim.

## Measured file-and-line inventory

`chore` owes no `files` document, so the surface area is measured here. Read on
2026-08-24 from the working tree at `origin/main`
`191ddf334208b8966dc5e32f4f597e434a086233`.

| Path | Measured today | What this ticket changes |
| --- | --- | --- |
| `docs/engineering.md` | 236 lines; § Branches and delivery is `:10-52` (heading at `:10`, next heading `## Markdown convention` at `:54`) | +1 bullet recording the one-way `upstream` sync — ~4 lines at the 78-column hard wrap |
| `docs/desktop/README.md` | 142 lines; "Planning baseline" paragraph at `:12-15`, currently naming upstream `7d6a948a` and "32 commits ahead" | +1 sentence with the SHA actually fetched and the date — ~2 lines |
| Git remotes | `origin` only — `git remote -v` on 2026-08-24 shows fetch and push URLs for `merceralex397-collab/pegasusDesktop` and nothing else | `upstream` added fetch-only |
| Everything else in the diff | — | comes from upstream, authored by upstream, reviewed but not written here |
| `docs/frd/frd-07-eva-and-external-engineering-handoff.md` | present in the fork | **must not be edited** — upstream `DOCS-013` owns it until the D-001 freeze |

## Approach

Add the remote and neuter its push URL in the same command sequence, then sync
through the ordinary route — task branch cut from `origin/dev`, merge
`upstream/main` into it, PR into `dev`, review, exact-SHA promotion. The
rejected alternative was merging `upstream/main` directly into `main`: it is
faster and it is the one thing `scripts/Test-MainBranchHistory.ps1:42-45`
exists to fail, because `main`'s head would no longer be contained in
`origin/dev` and the CI `changes` job (`.github/workflows/ci.yml:24-32`) runs
that guard on every push to `main`. The second rejected alternative was syncing
to the recorded SHA `7d6a948a`: under D-001 anything merged upstream after that
point and before the freeze is *lost*, not deferred, so the range is re-derived
from the live `upstream/main` head at execution.

Setting `--push` to a bogus URL rather than relying on discipline is deliberate.
`git remote set-url --push upstream DISABLED_NO_PUSH_TO_UPSTREAM` makes an
accidental `git push upstream` fail on an unresolvable transport instead of
succeeding against a live repository — the one failure in this ticket that
cannot be undone by the fork alone.

## Governing docs

`refs` is empty and `docs_todo: true` — confirm with `get_doc_gates FND-002`
before moving. No repository ADR governs this work today.

> **New ADR** — ADR-0100 (native WinUI 3 desktop client converted inside this
> fork), whose `## Consequences` carry the decided D-001, authored by
> [[FND-005]] (plan handle `DSK-00-05`); ADR-0100 is co-claimed by [[FND-026]]
> (plan handle `DSK-02-01`), so it is `authored by [[FND-005]]; see
> [[FND-005]]'s plan for the ownership reconciliation`. The D-001 text itself is
> written by [[FND-010]] (plan handle `DSK-00-10`), which also agrees the
> upstream freeze with that repository's owners.
> This plan is written to D-001 as recorded in `docs/desktop/README.md`
> § Locked decisions and open decisions and to
> `docs/desktop/00-governance-and-workflow/README.md` § 3 "Recommended branching
> flow" item 2; if the ADR lands differently this plan is revised before
> implementation.

Because `refs` is empty, the authorities that bind today are these:

| Authority | Requirement | Met by |
| --- | --- | --- |
| `docs/engineering.md:16-33` | Promotion is an exact-SHA atomic fast-forward with an explicit lease on `dev`, read back after the push, authorised by the literal `MERGE AUTH GRANTED` | Step 11 |
| `docs/engineering.md:34-37` | A GitHub PR merge, rebase merge or squash merge is **not** a promotion | Step 11 (states it, and the read-back proves it) |
| `.github/workflows/ci.yml:24-32` + `scripts/Test-MainBranchHistory.ps1:36-50` | `main`'s head must be an append-only advance and contained in `origin/dev` | Steps 6, 10, 11 (the sync never touches `main` directly) |
| Proposal § 6.1–6.3 Repository strategy | The current web projects remain temporarily; no permanent second Pegasus repository | Steps 2–7 |
| Proposal § 29 item 1 | Freeze the baseline and take upstream from it | Steps 3–4 |
| Plan 00 § 3 item 2 | One-way sync `upstream/main` → fork `dev` through a merge PR, then promote, after each upstream release until cutover; never push fork → upstream | Steps 2, 6, 11, 13 |
| Plan 01 `upstream-kanmer-carryover.md:197-224` § Code drift and the first sync | The first-sync procedure and its "repeat after each upstream release until cutover" rule | Steps 4–12, and step 13's hand-off |
| D-001 (2026-08-23) | The fork becomes the single release source at the first production gateway change; upstream is merged one final time then frozen — what is unmerged at the freeze is lost | Steps 3, 5, 13 |
| L-01 | The gateway is `Pegasus.Web` evolved in place, which is why upstream web fixes still matter | Step 7 (merge them anyway) |
| C-01 (2026-08-23) | The repositories become private on completion | Step 2 (no anonymous-access assumption in the remote setup) |
| `docs/desktop/00-governance-and-workflow/README.md` § 7 | Check `docs/adr/README.md` after every sync; upstream keeps issuing numbers below ADR-0100 | Step 8 |

## Routing

Copied from the ticket body's `## Routing` block.

- **Subagent**: `pegasus-desktop-reviewer` —
  `.codex/agents/pegasus-desktop-reviewer.toml` (verified present). It reviews
  the **sync diff**; the merge is performed by the ticket owner and the
  promotion by the operator.
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `kanmer-plan`
  (`.grok/skills/kanmer-plan/SKILL.md`) → `kanmer-execute`
  (`.grok/skills/kanmer-execute/SKILL.md`) → `kanmer-review`
  (`.grok/skills/kanmer-review/SKILL.md`).
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`,
  `set_ticket_doc`, `append_scratch`, `move_item`).
- **Kanmer pipeline** for profile `chore`: `kanmer-tickets` → `kanmer-plan` →
  `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout`. Call
  `get_doc_gates FND-002` before every move; a move crosses at most one gated
  boundary.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement
  (`AGENTS.md` § Repository task workflow step 5).

## Steps

These refine the body's thirteen implementation steps; order, ownership and
paths are the body's.

1. **Orient.** Read the plan row and
   `docs/desktop/00-governance-and-workflow/README.md` § 3 item 2,
   `docs/engineering.md:1-52`, and
   `docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md:1-20`
   (which records at `:9-10` that `DSK-01-09` executes the dispositions and
   `DSK-01-10` performs the first code sync). Call `get_doc_gates FND-002`, then
   `take_ticket`. **Claim [[FND-023]] (plan handle `DSK-01-10`) as well, or
   agree with its owner who runs the sync** — the two tickets describe the same
   first sync from two plans.
2. **Add the remote, push-disabled, in one sequence:**
   ```
   git remote add upstream https://github.com/collisionengineers/pegasus.git
   git remote set-url --push upstream DISABLED_NO_PUSH_TO_UPSTREAM
   git remote -v
   ```
   Done looks like `upstream <real url> (fetch)` and
   `upstream DISABLED_NO_PUSH_TO_UPSTREAM (push)`. Measured precondition: on
   2026-08-24 `git remote -v` listed `origin` only, so `git remote add` will not
   collide.
3. **Record the upstream head as it is now**, not as it was recorded:
   ```
   git fetch upstream --no-tags
   git rev-parse upstream/main
   ```
   Write the SHA and the date into this plan. `7d6a948a` was the head read on
   2026-08-23 and `docs/desktop/README.md:13-14` still names it; treat that as a
   planning baseline. Syncing to a recorded SHA drops everything merged since,
   and under D-001 what is dropped is lost.
4. **Prove the fast-forward and count the real work:**
   ```
   git merge-base --is-ancestor 191ddf334208b8966dc5e32f4f597e434a086233 upstream/main; echo $?
   git rev-list --count 191ddf334208b8966dc5e32f4f597e434a086233..upstream/main
   ```
   Expected `0`, then the current count. The planning baseline said `32`
   (`docs/desktop/README.md:13-14`,
   `upstream-kanmer-carryover.md:199-201`); state the actual number plainly
   rather than reconciling it to 32. A non-zero first result means the histories
   diverged — stop and raise it; force nothing.
5. **Name what the sync brings that postdates the 2026-08-23 triage.**
   `git log --oneline --no-merges 191ddf33..upstream/main`, then list every
   upstream ticket id the commit subjects carry and tick each off the carry-over
   register in this plan. Record each known later item that did **not** arrive —
   `DOCS-013`, `ENG-014` and `ENG-015` are the known cases at planning time, and
   the fork's `docs/frd/frd-07-eva-and-external-engineering-handoff.md` still
   carries the invented-manifest mandate they would remove. Write these as
   `upstream DOCS-013`, `upstream ENG-014`, `upstream ENG-015`: a bare
   `<PREFIX>-<nnn>` on this board is a **fork board id**, and board `ENG-001` /
   `ENG-002` are the imported carry-overs of upstream `ENG-014` / `ENG-015`
   (Kanmer group document `HZN-001/board-conventions.md` § *Upstream ids versus
   board ids* holds the authoritative 19-row join). An unarrived row is lost at
   the D-001 freeze unless a fork ticket holds it; [[FND-023]] owns the FRD-07
   consequence — record the fact here and point at that ticket.
6. **Merge on a task branch cut from `origin/dev`:**
   ```
   git worktree add ../pegasus-worktrees/upstream-sync -b task/upstream-sync-2026-08 origin/dev
   git merge upstream/main
   ```
   Expected: a fast-forward, or a clean merge commit with no conflicts.
7. **Read the diff before proposing it:** `git diff origin/dev...HEAD --stat`
   and `git log --oneline origin/dev..HEAD`. Expect Razor and web changes the
   conversion eventually retires — **merge them anyway**; the web app is live
   and upstream owns them. Do not fix forward in the fork what upstream owns;
   raise it upstream.
8. **Check the ADR namespace after the merge:** `ls docs/adr/` and
   `grep -n '^| \[0' docs/adr/README.md`. Measured on 2026-08-24 the fork holds
   ADR-0001…ADR-0029 with 0017 never issued, and `docs/adr/README.md:18-19` is
   the accepted table with columns `ADR | Title | Related FRD`. Every upstream
   ADR must stay below `0100`; the reserved block ADR-0100…ADR-0110
   (`AGENTS.md:84-90`) must be untouched. A collision is a stop condition.
9. **Run the local gates the CI lanes mirror.** All four scripts exist under
   `scripts/` (verified 2026-08-24):
   ```
   pwsh ./scripts/Test-DocumentationLinks.ps1
   pwsh ./scripts/Test-TestMarkdownPlacement.ps1
   pwsh ./scripts/Test-MigrationGrants.ps1
   pwsh ./scripts/Test-TestShard.ps1
   ```
   All exit 0. The first two are the CI `documentation` job
   (`.github/workflows/ci.yml:84,87`); the last two run in the `changes` job
   (`:55-59`).
10. **Open the PR into `dev`** (`gh pr create --base dev`) and have
    `pegasus-desktop-reviewer` review the sync diff specifically for changes to
    files the conversion plans already claim — gateway composition in
    `src/Pegasus.Web/Program.cs`, contracts, and migrations. Merge only with
    `repository-check` green.
11. **Operator step — promote by exact SHA**, with the literal words
    `MERGE AUTH GRANTED` given immediately before the push:
    ```
    git fetch origin --prune
    git rev-parse origin/main origin/dev
    git merge-base --is-ancestor origin/main origin/dev
    git push --atomic --force-with-lease=refs/heads/dev:<reviewed-dev-sha> origin <reviewed-dev-sha>:refs/heads/main <reviewed-dev-sha>:refs/heads/dev
    git fetch origin --prune && git rev-parse origin/main origin/dev
    ```
    Both heads must read back equal to `<reviewed-dev-sha>`. This is
    `docs/engineering.md:16-33` verbatim in shape; a GitHub merge/squash/rebase
    is not a promotion and does not substitute.
12. **Confirm the sync landed.**
    `git fetch upstream --no-tags && git log --oneline dev..upstream/main` prints
    nothing, and `docs/operations.md` § Production environment (heading at
    `:280`; highest entry on the fork today is **Release 20** at `:336`,
    2026-08-22, source `05fe7a7f`) now carries the releases the merge brought —
    releases 21–24 at the planning baseline.
13. **Record the rules, and name the successor.** In this plan and in the proof:
    never `git push upstream` in any form; the sync is one-way until the D-001
    freeze, and the freeze itself is agreed with the upstream owners in
    [[FND-010]] (plan handle `DSK-00-10`). Record the actual upstream HEAD SHA
    and date synced, the real commit count, and the arrival list from step 5.
    Record explicitly that this ticket discharges the **first** sync only. The
    standing cadence up to the freeze already has an owner on the board:
    **[[FND-051]] (plan handle `DSK-01-13`, "Standing later upstream syncs up to
    the D-001 freeze")**, created 2026-08-24 — name it by id rather than
    describing a ticket to be created; the follow-up the body anticipated
    exists.

## Verification

Evidence tier 1 — Static/build/architecture (`docs/engineering.md:72-90`), as
the body states. The sync is proved by history containment, a green
`repository-check` run and the read-back promotion, not by application
behaviour.

The `proof` is a `command-log` holding:

| Command | Expected |
| --- | --- |
| `git rev-parse upstream/main` (before the merge) | the SHA recorded verbatim as the head actually synced |
| `git rev-list --count 191ddf33..upstream/main` | the real count, stated as measured |
| `git merge-base --is-ancestor 191ddf334208b8966dc5e32f4f597e434a086233 upstream/main; echo $?` | `0` |
| `git remote -v` | `upstream` push URL reads `DISABLED_NO_PUSH_TO_UPSTREAM` |
| `git log --oneline dev..upstream/main` | no output |
| `git rev-parse origin/main origin/dev` after promotion | two identical SHAs equal to the reviewed SHA |
| `pwsh ./scripts/Test-DocumentationLinks.ps1` | exits 0 |

Plus the step 5 arrival list and the unarrived list (`upstream DOCS-013`,
`upstream ENG-014`, `upstream ENG-015` at planning time). Proof is written on
merged `main`, after the merge.

## Risks / open questions

- **Syncing to a stale SHA.** The single most likely defect: `7d6a948a` and
  "32 commits" are recorded in two documents and are stale by construction.
  Mitigation: steps 3–4 re-derive both, and the acceptance criteria make the
  re-derivation itself checkable.
- **Histories diverge.** If step 4's ancestor check fails, someone has rewritten
  history on one side. Mitigation: stop and raise; never force, rebase or reset
  (`docs/engineering.md:13-14`).
- **ADR-number collision from upstream.** Mitigated by the reserved block and by
  step 8's post-merge check; a collision is a stop condition, and the same check
  is a standing obligation on [[FND-051]].
- **Duplicate execution with [[FND-023]].** A scope boundary, not a question:
  whoever runs the sync first records the evidence and the other cites it and
  closes. Step 1 makes claiming both the first action.
- **`docs/engineering.md` edit collision with [[FND-009]]** (release-tag
  convention in the same § Branches and delivery). A scope boundary owned by a
  named ticket: coordinate the two edits, or land whichever is second as a
  rebase onto the first. This ticket adds only the one-way-sync sentence.
- **FRD-07 must not be touched here.** `docs/frd/frd-07-eva-and-external-engineering-handoff.md`
  is owned by upstream `DOCS-013` until the freeze; [[FND-023]] owns the
  consequence if it never arrives.
- **Merging web changes the conversion will retire is correct, not a mistake.**
  Reviewers who flag them are reading the wrong ticket; the note is in the
  Guardrails for that reason.

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over
this branch's own diff before the PR, recorded here under a dated heading.
Expected outcome: `n/a — docs-only` if the branch carries only the merge and the
two documentation sentences — the merge's own content is upstream's work and is
not in scope for this pass._
